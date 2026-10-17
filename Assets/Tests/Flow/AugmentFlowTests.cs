using System;
using System.Collections.Generic;
using ExpiryGame.Flow;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace ExpiryGame.Tests
{
    // 레벨업 → 증강 선택 → 효과 적용 → 재개 흐름. 팀원 타입은 쓰지 않고 Flow의 계약(IAugmentChooser 등)만 가짜로 채워 검증한다.
    public class AugmentFlowTests
    {
        private sealed class FakeTable : IXpTable
        {
            public int RequiredFor(int level) => 10;
        }

        // 선택 화면을 대신하는 가짜: 열면 대기하다가 테스트가 Choose/CloseWithoutChoice로 끝낸다
        private sealed class FakeChooser : IAugmentChooser
        {
            public List<AugmentDefinition> Candidates = new List<AugmentDefinition>();
            public bool ThrowOnOpen;
            public int Opens;
            public int Closes;
            private Action<AugmentDefinition> pending;

            public bool Prepare() => Candidates.Count > 0;
            public AugmentDefinition FirstCandidate => Candidates.Count > 0 ? Candidates[0] : null;

            public void Open(Action<AugmentDefinition> onResult)
            {
                Opens++;
                if (ThrowOnOpen) throw new InvalidOperationException("화면 에러");
                pending = onResult;
            }

            public void Close() => Closes++;

            public void Choose(AugmentDefinition chosen)
            {
                var callback = pending;
                pending = null;
                callback(chosen);
            }
        }

        private sealed class FakeFallback : IAugmentSelectionPresenter
        {
            public int Requests;
            public int Cancels;
            public bool AutoComplete = true;
            public void RequestSelection(int level, Action onComplete)
            {
                Requests++;
                if (AutoComplete) onComplete();
            }
            public void Cancel() => Cancels++;
        }

        private static AugmentDefinition Stat(string id, StatKey key, float perLevel, int max = 5)
            => new AugmentDefinition(id, id, "", max, new[] { new StatEffect(key, StatOp.Add, perLevel) });

        private sealed class Rig
        {
            public readonly GameStateMachine Machine = new GameStateMachine(GameState.Playing);
            public readonly ExperienceService Experience = new ExperienceService(new FakeTable());
            public readonly PlayerStatSheet Sheet = new PlayerStatSheet();
            public readonly AugmentEffectService Effects;
            public readonly FakeChooser Chooser = new FakeChooser();
            public readonly FakeFallback Fallback = new FakeFallback();
            public readonly GuardedAugmentPresenter Presenter;
            public readonly LevelUpCoordinator Coordinator;
            public readonly List<GameState> States = new List<GameState>();

            public Rig()
            {
                Sheet.SetBase(StatKey.MoveSpeed, 5f);
                Effects = new AugmentEffectService(Sheet);
                Presenter = new GuardedAugmentPresenter(Chooser, Effects, Fallback);
                Coordinator = new LevelUpCoordinator(Machine, Experience, Presenter);
                Machine.StateChanged += (from, to) => States.Add(to);
            }
        }

        // ── 레벨업 → 선택 → 재개 ──

        [Test]
        public void LevelUp_OpensSelection_StaysStoppedUntilChosen_ThenResumes()
        {
            var rig = new Rig();
            var speed = Stat("speed", StatKey.MoveSpeed, 0.5f);
            rig.Chooser.Candidates.Add(speed);

            rig.Experience.AddExperience(10);

            Assert.AreEqual(GameState.AugmentSelect, rig.Machine.Current, "레벨업하면 멈춘다");
            Assert.AreEqual(1, rig.Chooser.Opens);
            Assert.IsFalse(rig.Machine.Resume(), "선택 전에는 재개할 수 없다");
            Assert.AreEqual(GameState.AugmentSelect, rig.Machine.Current);

            rig.Chooser.Choose(speed);

            Assert.AreEqual(GameState.Playing, rig.Machine.Current, "고르면 재개된다");
            Assert.AreEqual(1, rig.Effects.LevelOf("speed"));
            Assert.AreEqual(5.5f, rig.Sheet.Get(StatKey.MoveSpeed), 0.0001f, "효과가 능력치에 반영된다");
        }

        [Test]
        public void MultipleLevelUps_OpenOneByOne_WithoutResumingInBetween()
        {
            var rig = new Rig();
            var speed = Stat("speed", StatKey.MoveSpeed, 0.5f);
            rig.Chooser.Candidates.Add(speed);

            rig.Experience.AddExperience(30); // 레벨 1 → 4: 레벨업 3번

            Assert.AreEqual(1, rig.Chooser.Opens, "첫 선택만 열리고 나머지는 큐에서 기다린다");
            Assert.AreEqual(2, rig.Coordinator.PendingCount);

            rig.Chooser.Choose(speed);
            Assert.AreEqual(2, rig.Chooser.Opens, "다음 선택이 곧바로 이어진다");
            Assert.AreEqual(GameState.AugmentSelect, rig.Machine.Current, "중간에 Playing으로 돌아가지 않는다");

            rig.Chooser.Choose(speed);
            Assert.AreEqual(3, rig.Chooser.Opens);
            Assert.AreEqual(GameState.AugmentSelect, rig.Machine.Current);

            rig.Chooser.Choose(speed);
            Assert.AreEqual(GameState.Playing, rig.Machine.Current, "마지막 선택이 끝나야 재개된다");
            Assert.AreEqual(3, rig.Effects.LevelOf("speed"));
            CollectionAssert.DoesNotContain(rig.States.GetRange(1, rig.States.Count - 2), GameState.Playing, "처음 멈춘 뒤 마지막 재개 전까지 Playing 전환이 없다");
        }

        // ── 효과 적용/해제/중첩 ──

        [Test]
        public void Effects_StackPerLevel_ThenRefuseAtMaxLevel_AndRevoke()
        {
            var sheet = new PlayerStatSheet();
            sheet.SetBase(StatKey.AttackPower, 0f);
            var effects = new AugmentEffectService(sheet);
            var damage = Stat("damage", StatKey.AttackPower, 2f, max: 3);

            Assert.IsTrue(effects.Apply(damage));
            Assert.AreEqual(2f, sheet.Get(StatKey.AttackPower));
            Assert.IsTrue(effects.Apply(damage));
            Assert.IsTrue(effects.Apply(damage));
            Assert.AreEqual(6f, sheet.Get(StatKey.AttackPower), "레벨 3이면 perLevel × 3");
            Assert.AreEqual(1, sheet.ModifierCount, "같은 증강의 중첩은 수정자를 늘리지 않고 값을 올린다");

            Assert.IsFalse(effects.CanApply(damage));
            Assert.IsFalse(effects.Apply(damage), "최대 중첩이면 거절한다");
            Assert.AreEqual(6f, sheet.Get(StatKey.AttackPower));

            Assert.IsTrue(effects.Revoke(damage));
            Assert.AreEqual(4f, sheet.Get(StatKey.AttackPower), "해제하면 한 단계 내려간다");
            Assert.IsTrue(effects.Revoke(damage));
            Assert.IsTrue(effects.Revoke(damage));
            Assert.AreEqual(0f, sheet.Get(StatKey.AttackPower));
            Assert.AreEqual(0, sheet.ModifierCount);
            Assert.AreEqual(0, effects.Records.Count, "0레벨이 되면 기록에서도 빠진다");
            Assert.IsFalse(effects.Revoke(damage));
        }

        [Test]
        public void Effects_DifferentAugmentsOnSameStat_Add_AndMultiplyAppliesAfterAdd()
        {
            var sheet = new PlayerStatSheet();
            sheet.SetBase(StatKey.MoveSpeed, 5f);
            var effects = new AugmentEffectService(sheet);
            effects.Apply(Stat("a", StatKey.MoveSpeed, 1f));
            effects.Apply(Stat("b", StatKey.MoveSpeed, 0.5f));
            effects.Apply(new AugmentDefinition("pct", "pct", "", 1, new[] { new StatEffect(StatKey.MoveSpeed, StatOp.Multiply, 0.1f) }));

            Assert.AreEqual((5f + 1f + 0.5f) * 1.1f, sheet.Get(StatKey.MoveSpeed), 0.0001f);
        }

        [Test]
        public void SpecialEffect_ReceivesNewLevelEachTime_AndFailureDoesNotBreakApply()
        {
            var effects = new AugmentEffectService(new PlayerStatSheet());
            var levels = new List<int>();
            var skill = new AugmentDefinition("skill", "스킬", "", 3, null, level => levels.Add(level));
            effects.Apply(skill);
            effects.Apply(skill);
            CollectionAssert.AreEqual(new[] { 1, 2 }, levels);

            var broken = new AugmentDefinition("broken", "고장", "", 2, null, level => throw new InvalidOperationException("스킬 부여 실패"));
            LogAssert.Expect(UnityEngine.LogType.Exception, new System.Text.RegularExpressions.Regex("스킬 부여 실패"));
            Assert.IsTrue(effects.Apply(broken), "특수 효과가 실패해도 선택은 기록된다");
            Assert.AreEqual(1, effects.LevelOf("broken"));
        }

        [Test]
        public void StatApplier_WritesMoveSpeedToMover_AndRestoresBaseOnReset()
        {
            var go = new UnityEngine.GameObject("MoverTest");
            try
            {
                var mover = go.AddComponent<PlayerMover>();
                mover.MoveSpeed = 5f;
                var sheet = new PlayerStatSheet();
                using (new PlayerStatApplier(sheet, mover, null))
                {
                    var effects = new AugmentEffectService(sheet);
                    effects.Apply(Stat("speed", StatKey.MoveSpeed, 1.5f));
                    Assert.AreEqual(6.5f, mover.MoveSpeed, 0.0001f, "증강을 고르면 플레이어 이동 속도에 반영된다");

                    effects.Reset();
                    Assert.AreEqual(5f, mover.MoveSpeed, 0.0001f, "초기화하면 기본 이동 속도로 돌아간다");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        // ── 재시작 초기화 / 기록 ──

        [Test]
        public void Reset_ClearsModifiersLevelsAndHistory()
        {
            var sheet = new PlayerStatSheet();
            sheet.SetBase(StatKey.AttackPower, 1f);
            var effects = new AugmentEffectService(sheet);
            effects.Apply(Stat("damage", StatKey.AttackPower, 2f));
            effects.Apply(Stat("damage", StatKey.AttackPower, 2f));
            effects.Apply(Stat("speed", StatKey.MoveSpeed, 1f));
            Assert.AreEqual(2, effects.Records.Count);

            effects.Reset();

            Assert.AreEqual(1f, sheet.Get(StatKey.AttackPower), "수정자가 모두 사라지고 기본값만 남는다");
            Assert.AreEqual(0, sheet.ModifierCount);
            Assert.AreEqual(0, effects.LevelOf("damage"));
            Assert.AreEqual(0, effects.Records.Count);
            Assert.IsTrue(effects.CanApply(Stat("damage", StatKey.AttackPower, 2f)), "초기화 뒤에는 다시 쌓을 수 있다");
        }

        [Test]
        public void History_KeepsPickOrderAndFinalLevel_AndFeedsStatsLines()
        {
            var effects = new AugmentEffectService(new PlayerStatSheet());
            var damage = Stat("damage", StatKey.AttackPower, 1f);
            var speed = Stat("speed", StatKey.MoveSpeed, 1f);
            effects.Apply(damage);
            effects.Apply(speed);
            effects.Apply(damage);

            var records = effects.Records;
            Assert.AreEqual(2, records.Count);
            Assert.AreEqual("damage", records[0].Id, "처음 고른 순서");
            Assert.AreEqual(2, records[0].Level);
            Assert.AreEqual("speed", records[1].Id);
            Assert.AreEqual(1, records[1].Level);
        }

        [Test]
        public void CoordinatorReset_DropsPendingSelectionAndCancelsScreen()
        {
            var rig = new Rig();
            rig.Chooser.Candidates.Add(Stat("speed", StatKey.MoveSpeed, 0.5f));
            rig.Experience.AddExperience(20);
            Assert.AreEqual(GameState.AugmentSelect, rig.Machine.Current);

            rig.Coordinator.Reset();
            rig.Effects.Reset();

            Assert.AreEqual(GameState.Playing, rig.Machine.Current);
            Assert.AreEqual(0, rig.Coordinator.PendingCount);
            Assert.AreEqual(1, rig.Chooser.Closes, "열려 있던 화면을 접는다");
            Assert.AreEqual(1, rig.Fallback.Cancels);
        }

        // ── 예외 처리 ──

        [Test]
        public void NoCandidates_UsesFallbackReward_AndGameResumes()
        {
            var rig = new Rig(); // 후보 없음 = 모든 증강이 최대 중첩
            rig.Experience.AddExperience(10);

            Assert.AreEqual(1, rig.Fallback.Requests, "대체 보상으로 처리한다");
            Assert.AreEqual(0, rig.Chooser.Opens, "선택 화면은 열지 않는다");
            Assert.AreEqual(GameState.Playing, rig.Machine.Current, "게임이 멈추지 않는다");
        }

        [Test]
        public void AllAugmentsAtMaxStack_ChooserHasNoCandidates_FallbackRuns()
        {
            var rig = new Rig();
            var only = Stat("only", StatKey.AttackPower, 1f, max: 1);
            rig.Effects.Apply(only);
            // 팀원 규칙과 같이, 최대 중첩에 닿은 증강은 후보에서 빠진다
            rig.Chooser.Candidates = rig.Effects.CanApply(only) ? new List<AugmentDefinition> { only } : new List<AugmentDefinition>();

            rig.Experience.AddExperience(10);

            Assert.AreEqual(1, rig.Fallback.Requests);
            Assert.AreEqual(GameState.Playing, rig.Machine.Current);
        }

        [Test]
        public void ScreenError_LogsError_AppliesFirstCandidate_AndResumes()
        {
            var rig = new Rig();
            var first = Stat("first", StatKey.MoveSpeed, 1f);
            rig.Chooser.Candidates.Add(first);
            rig.Chooser.Candidates.Add(Stat("second", StatKey.MoveSpeed, 2f));
            rig.Chooser.ThrowOnOpen = true;
            LogAssert.Expect(UnityEngine.LogType.Error, new System.Text.RegularExpressions.Regex("에러가 났습니다"));
            LogAssert.Expect(UnityEngine.LogType.Exception, new System.Text.RegularExpressions.Regex("화면 에러"));

            rig.Experience.AddExperience(10);

            Assert.AreEqual(1, rig.Effects.LevelOf("first"), "첫 번째 후보가 적용된다");
            Assert.AreEqual(0, rig.Effects.LevelOf("second"));
            Assert.AreEqual(GameState.Playing, rig.Machine.Current, "복구하고 재개한다");
            Assert.AreEqual(0, rig.Fallback.Requests);
        }

        [Test]
        public void ClosedWithoutChoice_LogsError_AppliesFirstCandidate_AndResumes()
        {
            var rig = new Rig();
            var first = Stat("first", StatKey.AttackPower, 1f);
            rig.Chooser.Candidates.Add(first);
            LogAssert.Expect(UnityEngine.LogType.Error, new System.Text.RegularExpressions.Regex("선택 없이 닫혔습니다"));

            rig.Experience.AddExperience(10);
            Assert.AreEqual(GameState.AugmentSelect, rig.Machine.Current, "자동 선택 타이머 없이 기다린다");
            rig.Chooser.Choose(null);

            Assert.AreEqual(1, rig.Effects.LevelOf("first"));
            Assert.AreEqual(GameState.Playing, rig.Machine.Current);
        }

        [Test]
        public void ScreenError_WithoutAnyCandidate_FallsBackToReward()
        {
            var rig = new Rig();
            rig.Chooser.Candidates.Add(Stat("x", StatKey.AttackPower, 1f, max: 1));
            rig.Effects.Apply(rig.Chooser.Candidates[0]); // 첫 후보가 이미 최대라 적용할 수 없다
            rig.Chooser.ThrowOnOpen = true;
            LogAssert.Expect(UnityEngine.LogType.Error, new System.Text.RegularExpressions.Regex("에러가 났습니다"));
            LogAssert.Expect(UnityEngine.LogType.Exception, new System.Text.RegularExpressions.Regex("화면 에러"));

            rig.Experience.AddExperience(10);

            Assert.AreEqual(1, rig.Fallback.Requests);
            Assert.AreEqual(GameState.Playing, rig.Machine.Current);
        }

        [Test]
        public void PlayerDeathDuringSelection_EndsGame_AndClosesScreen()
        {
            var rig = new Rig();
            rig.Chooser.Candidates.Add(Stat("speed", StatKey.MoveSpeed, 0.5f));
            rig.Experience.AddExperience(20);
            Assert.AreEqual(GameState.AugmentSelect, rig.Machine.Current);

            Assert.IsTrue(rig.Machine.Fail(), "선택 중 사망도 게임 오버로 끝난다");

            Assert.AreEqual(GameState.GameOver, rig.Machine.Current);
            Assert.AreEqual(0, rig.Coordinator.PendingCount, "남은 레벨업은 버린다");
            Assert.IsFalse(rig.Coordinator.IsSelecting);
            Assert.AreEqual(1, rig.Chooser.Closes, "열려 있던 선택 화면을 접는다");

            rig.Chooser.Choose(rig.Chooser.Candidates[0]); // 늦은 클릭은 무시
            Assert.AreEqual(0, rig.Effects.LevelOf("speed"));
            Assert.AreEqual(GameState.GameOver, rig.Machine.Current);
        }

        // ── 후보 뽑기 ──

        [Test]
        public void CandidateDrawer_PicksDistinctUpToCount_AndShortPoolReturnsAll()
        {
            var pool = new List<string> { "a", "b", "c", "d", "e" };
            var picked = CandidateDrawer.Draw(pool, 3, (min, max) => max - 1);
            Assert.AreEqual(3, picked.Count);
            CollectionAssert.AllItemsAreUnique(picked);

            var shortPool = CandidateDrawer.Draw(new List<string> { "a", "b" }, 3, (min, max) => min);
            CollectionAssert.AreEquivalent(new[] { "a", "b" }, shortPool, "후보가 모자라면 있는 만큼만");
            Assert.AreEqual(0, CandidateDrawer.Draw(new List<string>(), 3, (min, max) => min).Count);
            Assert.AreEqual(5, pool.Count, "원본 풀은 바뀌지 않는다");
        }
    }
}
