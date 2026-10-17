using System.Collections;
using System.Linq;
using ExpiryGame.Flow;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace ExpiryGame.Tests
{
    // 실제 Game.unity를 불러 팀원 증강 선택 화면까지 이어 보는 플레이 모드 테스트 (Window > General > Test Runner > PlayMode).
    // 레벨업 → 정지/입력 차단 → 팀원 화면의 버튼 클릭 → 효과 적용 → 재개를 진짜 UI로 확인한다.
    // 전제: Tools > ShelfLife > Setup Game (All In One) 으로 Game.unity에 증강이 설치돼 있어야 한다 (없으면 건너뜀).
    public class AugmentPlayTests
    {
        private GameFlowRoot root;

        private IEnumerator LoadGame()
        {
            SceneManager.LoadScene("Game");
            yield return null; yield return null; yield return null;
            root = Object.FindFirstObjectByType<GameFlowRoot>();
            Assert.IsNotNull(root, "Game 씬에 GameFlowRoot가 있어야 한다");
            if (GameObject.Find("AugmentCanvas") == null) Assert.Ignore("Game.unity에 증강이 설치되지 않았습니다. Setup Game (All In One)을 먼저 실행하세요.");
            if (root.Machine.Current == GameState.Title) root.Machine.StartGame();
            yield return null;
            Assert.AreEqual(GameState.Playing, root.Machine.Current);
        }

        private static Button[] SlotButtons()
        {
            var canvas = GameObject.Find("AugmentCanvas");
            return canvas.GetComponentsInChildren<Button>(false).Where(b => b.gameObject.activeInHierarchy).ToArray();
        }

        private static IEnumerator Frames(int count)
        {
            for (var i = 0; i < count; i++) yield return null;
        }

        [UnityTest]
        public IEnumerator LevelUp_StopsGame_BlocksInput_ThenSelectionResumes()
        {
            yield return LoadGame();
            var xp = root.Experience;
            var input = Object.FindFirstObjectByType<PlayerInputReader>();

            root.ExperienceSink.AddExperience(xp.Required - xp.Current);
            yield return Frames(2);

            Assert.AreEqual(GameState.AugmentSelect, root.Machine.Current);
            Assert.AreEqual(0f, Time.timeScale, "게임 정지");
            Assert.IsFalse(input.enabled, "이동/휘두르기/먹기 입력 차단");
            var augmentCanvas = GameObject.Find("AugmentCanvas").GetComponent<Canvas>();
            var flowCanvas = GameObject.Find("FlowCanvas").GetComponent<Canvas>();
            Assert.Greater(augmentCanvas.sortingOrder, flowCanvas.sortingOrder, "선택 화면이 HUD/흐름 화면 위에 그려진다");
            Assert.AreEqual(3, SlotButtons().Length, "후보 3개");

            // 아무것도 안 해도 자동 선택/재개되지 않는다
            var start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < 2.5f) yield return null;
            Assert.AreEqual(GameState.AugmentSelect, root.Machine.Current);
            Assert.AreEqual(0, root.AugmentHistory.Records.Count);

            SlotButtons()[0].onClick.Invoke();
            yield return Frames(2);

            Assert.AreEqual(GameState.Playing, root.Machine.Current, "고르면 재개");
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsTrue(input.enabled, "입력 복구");
            Assert.AreEqual(1, root.AugmentHistory.Records.Count);
            Assert.AreEqual(0, SlotButtons().Length, "선택 화면 접힘");
        }

        [UnityTest]
        public IEnumerator MultipleLevelUps_ShowOneByOne_WithoutResumingInBetween()
        {
            yield return LoadGame();
            var xp = root.Experience;
            var before = xp.Level;
            root.ExperienceSink.AddExperience(xp.Required * 12);
            var gained = xp.Level - before;
            Assert.GreaterOrEqual(gained, 3);
            yield return null;

            for (var i = 0; i < gained; i++)
            {
                Assert.AreEqual(GameState.AugmentSelect, root.Machine.Current, $"{i + 1}/{gained}번째 선택 중에는 재개되지 않는다");
                Assert.Greater(SlotButtons().Length, 0);
                SlotButtons()[0].onClick.Invoke();
                yield return Frames(2);
            }
            Assert.AreEqual(GameState.Playing, root.Machine.Current);
            Assert.AreEqual(gained, root.AugmentHistory.Records.Sum(r => r.Level), "선택 횟수 = 레벨업 횟수");
        }

        [UnityTest]
        public IEnumerator PlayerDeathDuringSelection_EndsGame_AndClosesScreen()
        {
            yield return LoadGame();
            var xp = root.Experience;
            root.ExperienceSink.AddExperience(xp.Required - xp.Current);
            yield return Frames(2);
            Assert.AreEqual(GameState.AugmentSelect, root.Machine.Current);

            Object.FindFirstObjectByType<PlayerInputReader>().GetComponent<Health>().TakeDamage(99999f);
            yield return Frames(2);

            Assert.AreEqual(GameState.GameOver, root.Machine.Current);
            Assert.AreEqual(0, SlotButtons().Length, "선택 화면 접힘");
        }

        [UnityTest]
        public IEnumerator AllAugmentsMaxed_FallsBackToReward_AndGameKeepsRunning()
        {
            yield return LoadGame();
            var xp = root.Experience;
            var total = root.AugmentDebug.AllAugments.Sum(a => a.MaxLevel);
            var picks = 0;
            for (var i = 0; i < total + 5; i++)
            {
                root.ExperienceSink.AddExperience(xp.Required - xp.Current);
                yield return Frames(2);
                if (root.Machine.Current == GameState.AugmentSelect)
                {
                    if (SlotButtons().Length > 0) { SlotButtons()[0].onClick.Invoke(); picks++; }
                    else
                    {
                        // 대체 보상 배너: 실시간으로 잠깐 뜬 뒤 자동 재개
                        var start = Time.realtimeSinceStartup;
                        while (root.Machine.Current == GameState.AugmentSelect && Time.realtimeSinceStartup - start < 3f) yield return null;
                    }
                    yield return Frames(2);
                }
                Assert.AreEqual(GameState.Playing, root.Machine.Current, $"레벨업 {i + 1}번째 뒤에도 게임이 멈추지 않는다");
            }
            Assert.AreEqual(total, picks, "모든 증강이 최대 중첩이 될 때까지만 화면이 열린다");
            Assert.IsTrue(root.AugmentHistory.Records.All(r => root.Augments.LevelOf(r.Id) == r.Level));
        }

        [UnityTest]
        public IEnumerator ForcedCandidates_AreOneShot_AndRetryResetsEverything()
        {
            yield return LoadGame();
            var xp = root.Experience;
            root.AugmentDebug.ForceNextCandidates(new[] { "DamageUp" });
            root.ExperienceSink.AddExperience(xp.Required - xp.Current);
            yield return Frames(2);
            Assert.AreEqual(1, SlotButtons().Length, "강제 지정한 후보만 뜬다");
            SlotButtons()[0].onClick.Invoke();
            yield return Frames(2);
            Assert.AreEqual("DamageUp", root.AugmentHistory.Records[0].Id);
            Assert.AreEqual(1f, root.PlayerStats.Get(StatKey.AttackPower), "효과가 능력치에 반영된다");
            Assert.AreEqual(0, root.AugmentDebug.ForcedIds.Count, "강제 지정은 한 번 쓰면 풀린다");

            // 다시하기와 같은 경로: 씬 리로드
            FlowLaunch.Restart();
            yield return Frames(4);
            root = Object.FindFirstObjectByType<GameFlowRoot>();
            Assert.AreEqual(0, root.AugmentHistory.Records.Count, "선택 기록 초기화");
            Assert.AreEqual(0f, root.PlayerStats.Get(StatKey.AttackPower), "수정자 초기화");
            Assert.AreEqual(1, root.Experience.Level);
        }
    }
}
