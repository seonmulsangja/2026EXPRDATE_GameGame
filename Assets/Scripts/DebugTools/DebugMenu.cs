#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ExpiryGame.Flow;
using ExpiryGame.Landmark;
using ExpiryGame.World;

namespace ExpiryGame.DebugTools
{
    // 테스트용 디버그 메뉴 (F1로 열고 닫기). 시간 빨리감기 / 낮밤 즉시 전환 / 가까운 랜드마크로 텔레포트 / 음식 지급.
    // 게임 로직은 건드리지 않고 공개 API(GameClock, IFoodInventory, ILandmarkLocator)만 호출한다. 출시 전 삭제 대상.
    public class DebugMenu : MonoBehaviour
    {
        [SerializeField] private GameClock clock;
        [SerializeField] private DayNightProfile dayNightProfile;
        [SerializeField] private FoodInventory inventory;
        [SerializeField] private FoodFactory factory;
        [SerializeField] private LandmarkLocator locator;
        [SerializeField] private Transform player;
        [SerializeField] private FoodData[] foods = new FoodData[0];
        [SerializeField] private float[] speeds = { 1f, 4f, 16f, 64f };
        [Tooltip("텔레포트 시 랜드마크 아래쪽으로 비켜 서는 거리 (타일)")]
        [SerializeField] private float teleportOffset = 2.5f;
        [Tooltip("안내 문구 순환 표시에서 문구를 바꾸는 간격 (초)")]
        [SerializeField, Min(0.3f)] private float promptSeconds = 1.5f;

        // 안내 박스 점검용 문구: 짧은 것 / 긴 것 / 여러 줄이 될 만큼 긴 것 / 영문 / 숫자 / 기호
        private static readonly string[] PromptSamples =
        {
            "F  열기",
            "F  굽기 불가 (이미 강화됨)",
            "F  굽기 (회복 +12 → +16)",
            "F  굽기 불가 (꺼짐)",
            "F  굽기 불가 (연료 부족)",
            "F  굽기 불가 (쿨다운 10초) 지금은 다른 음식도 구울 수 없습니다 잠시 후 다시 시도하세요",
            "F  Cook (Heal +12 -> +16) Already Enhanced",
            "F  1234567890 / 9999 / 100%",
            "F  WWWWWWWWWWWWWWWWWWWWWWWWWWWWWW",
        };

        private readonly List<LandmarkInfo> nearby = new List<LandmarkInfo>();
        private ISpawnDebug spawn;
        private float nextSpawnLookup;
        private bool open;
        private Vector2 scroll;
        private LandmarkInteractor interactor;
        private bool promptCycle;
        private int promptIndex;
        private float nextPromptTime;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame) open = !open;
            UpdatePromptCycle();
        }

        private void OnDisable()
        {
            clock.SpeedMultiplier = 1f;
            ShowPrompt(null);
        }

        private void OnGUI()
        {
            if (!open)
            {
                GUI.Label(new Rect(10, Screen.height - 24, 240, 22), "F1: 디버그 메뉴");
                return;
            }

            GUILayout.BeginArea(new Rect(10, 10, 280, Screen.height - 20), GUI.skin.box);
            scroll = GUILayout.BeginScrollView(scroll);

            GUILayout.Label($"시간 배속 (현재 x{clock.SpeedMultiplier:0.#})");
            GUILayout.BeginHorizontal();
            foreach (var speed in speeds)
                if (GUILayout.Button($"x{speed:0.#}")) clock.SpeedMultiplier = speed;
            GUILayout.EndHorizontal();

            GUILayout.Label("낮밤 즉시 전환");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("새벽")) Jump(DayPart.Dawn, 0f);
            if (GUILayout.Button("낮")) Jump(DayPart.Day, 0.5f);
            if (GUILayout.Button("황혼")) Jump(DayPart.Dusk, 0.5f);
            if (GUILayout.Button("밤")) Jump(DayPart.Night, 0.5f);
            GUILayout.EndHorizontal();

            GUILayout.Label("랜드마크");
            locator.GetNearby(player.position, nearby);
            if (nearby.Count == 0) GUILayout.Label("(주변에 없음)");
            else
            {
                var nearest = nearby[0];
                if (GUILayout.Button($"가장 가까운 곳으로: {nearest.Data.displayName} ({nearest.Distance:0}m)")) Teleport(nearest);
            }

            DrawPromptPanel();
            DrawSpawnPanel();

            GUILayout.Label("음식 지급 (가방에 추가)");
            foreach (var food in foods)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(food.displayName)) Give(food, 1f);
                if (GUILayout.Button("썩음", GUILayout.Width(50))) Give(food, 0f);
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // 안내 문구 순환 표시: 켜면 PromptSamples를 일정 간격으로 돌려 띄워 박스 안에 들어오는지 확인한다
        private void DrawPromptPanel()
        {
            GUILayout.Label("안내 문구 박스 점검");
            var wasCycling = promptCycle;
            promptCycle = GUILayout.Toggle(promptCycle, promptCycle ? "순환 표시 중 (눌러서 끄기)" : "안내 문구 순환 표시");
            if (promptCycle != wasCycling)
            {
                nextPromptTime = 0f;
                if (!promptCycle) ShowPrompt(null);
            }
            if (!promptCycle) return;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("이전")) StepPrompt(-1);
            if (GUILayout.Button("다음")) StepPrompt(1);
            GUILayout.EndHorizontal();
            GUILayout.Label($"{promptIndex + 1}/{PromptSamples.Length}: {PromptSamples[promptIndex]}");
        }

        private void UpdatePromptCycle()
        {
            if (!promptCycle || Time.unscaledTime < nextPromptTime) return;
            nextPromptTime = Time.unscaledTime + promptSeconds;
            ShowPrompt(PromptSamples[promptIndex]);
            promptIndex = (promptIndex + 1) % PromptSamples.Length;
        }

        private void StepPrompt(int delta)
        {
            promptIndex = (promptIndex + delta + PromptSamples.Length) % PromptSamples.Length;
            ShowPrompt(PromptSamples[promptIndex]);
            nextPromptTime = Time.unscaledTime + promptSeconds;
        }

        private void ShowPrompt(string text)
        {
            if (interactor == null) interactor = FindFirstObjectByType<LandmarkInteractor>();
            if (interactor != null) interactor.SetDebugPrompt(text);
        }

        // 스포너는 Assembly-CSharp에 있어 직접 참조할 수 없으므로 ISpawnDebug 계약으로 찾는다 (1초마다 재탐색)
        private void FindSpawn()
        {
            if (spawn != null && !(spawn is Object o && o == null)) return;
            spawn = null;
            if (Time.unscaledTime < nextSpawnLookup) return;
            nextSpawnLookup = Time.unscaledTime + 1f;
            foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (behaviour is ISpawnDebug found) { spawn = found; return; }
        }

        private void DrawSpawnPanel()
        {
            GUILayout.Label("적 스폰 상태");
            FindSpawn();
            if (spawn == null)
            {
                GUILayout.Label("(EnemyChunkSpawner가 씬에 없음 → Setup Game 실행 필요)");
                return;
            }

            var d = spawn.Diagnostics;
            GUILayout.Label($"활성 적 {spawn.ActiveEnemies} / 최대 {spawn.MaxEnemies}   로드된 청크 {spawn.LoadedChunks}");
            GUILayout.Label($"스폰 배율 x{spawn.SpawnMultiplier:0.##} (일차/낮밤 반영)");
            GUILayout.Label($"직전 시도: {d.LastResult}");
            GUILayout.Label($"시도 {d.Ticks}회 / 성공 {d.Spawned}회 / 막힘 {d.TotalBlocked}회");
            foreach (var pair in d.All())
                if (pair.Value > 0) GUILayout.Label($"  · {SpawnDiagnostics.Label(pair.Key)}: {pair.Value}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("적 강제 스폰(플레이어 근처)")) spawn.ForceSpawnNearPlayer();
            if (GUILayout.Button("카운터 초기화", GUILayout.Width(100))) d.Reset();
            GUILayout.EndHorizontal();
            spawn.Paused = GUILayout.Toggle(spawn.Paused, spawn.Paused ? "스폰 정지 중 (눌러서 재개)" : "스폰 전체 정지");
        }

        private void Jump(DayPart part, float progress)
        {
            clock.JumpToTimeOfDay(dayNightProfile.TimeOfDayAt(part, progress));
        }

        private void Teleport(LandmarkInfo target)
        {
            var destination = target.Position + Vector2.down * teleportOffset;
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null) body.position = destination;
            player.position = destination;
            Physics2D.SyncTransforms();
        }

        private void Give(FoodData data, float shelfLife01)
        {
            var item = ((IFoodFactory)factory).Create(data);
            item.SetShelfLife01(shelfLife01);
            if (!((IFoodInventory)inventory).TryAdd(item)) Debug.Log("[Debug] 가방이 가득 찼습니다.");
        }
    }
}
#else
using UnityEngine;

namespace ExpiryGame.DebugTools
{
    // 릴리스 빌드에서는 디버그 기능이 컴파일되지 않는다. 씬에 남아 있는 컴포넌트가 Missing Script가 되지 않도록 빈 껍데기만 둔다.
    public class DebugMenu : MonoBehaviour
    {
    }
}
#endif
