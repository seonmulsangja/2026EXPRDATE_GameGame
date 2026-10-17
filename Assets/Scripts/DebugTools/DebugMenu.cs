#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
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

        private readonly List<LandmarkInfo> nearby = new List<LandmarkInfo>();
        private bool open;
        private Vector2 scroll;

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame) open = !open;
        }

        private void OnDisable()
        {
            clock.SpeedMultiplier = 1f;
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
