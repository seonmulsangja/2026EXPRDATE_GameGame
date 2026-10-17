using System;
using System.Collections.Generic;
using ExpiryGame.Flow;
using NUnit.Framework;
using UnityEngine;

namespace ExpiryGame.Tests
{
    // 초반 난이도 완화(무적 시간 / 속도 규칙 / 스폰 유예) + 경험치·레벨업 흐름
    public class DifficultyAndExperienceTests
    {
        private sealed class FakeTable : IXpTable
        {
            public int Value = 10;
            public int RequiredFor(int level) => Value;
        }

        private sealed class FakePresenter : IAugmentSelectionPresenter
        {
            public readonly List<int> Requested = new List<int>();
            public readonly List<Action> Completions = new List<Action>();
            public int Cancels;

            public void RequestSelection(int level, Action onComplete)
            {
                Requested.Add(level);
                Completions.Add(onComplete);
            }

            public void Cancel() => Cancels++;
            public void CompleteLast() => Completions[Completions.Count - 1].Invoke();
        }

        private sealed class RewardEnemy : IEnemyActor, IExperienceReward
        {
            public Vector3 Position => Vector3.zero;
            public float Threat01 => 0.5f;
            public int ExperienceReward { get; set; }
            public event Action<IEnemyActor> Killed;
            public void Kill() => Killed?.Invoke(this);
        }

        private sealed class PlainEnemy : IEnemyActor
        {
            public Vector3 Position => Vector3.zero;
            public float Threat01 => 0.5f;
            public event Action<IEnemyActor> Killed;
            public void Kill() => Killed?.Invoke(this);
        }

        private sealed class FakeSink : IExperienceSink
        {
            public int Total;
            public void AddExperience(int amount) => Total += amount;
        }

        private sealed class FakeClock : IGameClock
        {
            public float DayLengthSeconds => 60f;
            public float TimeOfDay01 => 0f;
            public int DayCount { get; set; } = 1;
            public DayPhase Phase => DayPhase.Day;
            public event Action<float> Ticked { add { } remove { } }
            public event Action<DayPhase> PhaseChanged { add { } remove { } }
            public event Action<int> DayStarted { add { } remove { } }
            public float SecondsToDays(float seconds) => seconds / DayLengthSeconds;
        }

        private readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in created) if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
            created.Clear();
            foreach (var enemy in new List<IEnemyActor>(EnemyRegistry.Enemies)) EnemyRegistry.Unregister(enemy);
        }

        private DifficultyEvaluator NewEvaluator(out DifficultyProfile profile, out DifficultyTuning tuning)
        {
            profile = ScriptableObject.CreateInstance<DifficultyProfile>();
            created.Add(profile);
            tuning = new DifficultyTuning();
            return new DifficultyEvaluator(profile, tuning);
        }

        // ---------- 피격 무적 ----------

        [Test]
        public void Invulnerability_SecondHitInsideWindowIsBlocked()
        {
            var gate = new HitInvulnerability();
            Assert.IsTrue(gate.TryPass(10f, 0.8f));
            Assert.IsFalse(gate.TryPass(10.5f, 0.8f), "0.8초 안의 두 번째 피격은 막힌다");
            Assert.IsTrue(gate.TryPass(10.81f, 0.8f), "창이 끝나면 다시 맞는다");
        }

        [Test]
        public void Invulnerability_BlockedHitDoesNotExtendTheWindow()
        {
            var gate = new HitInvulnerability();
            gate.TryPass(0f, 0.8f);
            gate.TryPass(0.7f, 0.8f); // 막힌 시도
            Assert.IsTrue(gate.TryPass(0.81f, 0.8f), "막힌 시도가 무적을 연장하면 안 된다");
        }

        [Test]
        public void Invulnerability_SeveralEnemiesAtOnceDamageOnlyOnce()
        {
            var gate = new HitInvulnerability();
            var damage = 0f;
            for (var enemy = 0; enemy < 5; enemy++)
                if (gate.TryPass(3f, 0.8f)) damage += 5f;
            Assert.AreEqual(5f, damage, "다섯 마리가 같은 순간에 닿아도 피해는 1번");
        }

        [Test]
        public void Invulnerability_ZeroDurationNeverBlocks_AndResetClears()
        {
            var gate = new HitInvulnerability();
            Assert.IsTrue(gate.TryPass(1f, 0f));
            Assert.IsTrue(gate.TryPass(1f, 0f));
            gate.TryPass(2f, 0.8f);
            gate.Reset();
            Assert.IsTrue(gate.TryPass(2.1f, 0.8f));
        }

        // ---------- 일차별 값 ----------

        [Test]
        public void Evaluator_MaxAliveStepsPerDay_AndStaysFlatInsideTheDay()
        {
            var evaluator = NewEvaluator(out _, out _);
            var expected = new[] { 3, 4, 6, 8, 10 };
            for (var day = 0; day < expected.Length; day++)
            {
                evaluator.SetTime(day, false);
                Assert.AreEqual(expected[day], evaluator.MaxAlive, $"{day + 1}일차 새벽");
                evaluator.SetTime(day + 0.97f, false);
                Assert.AreEqual(expected[day], evaluator.MaxAlive, $"{day + 1}일차 끝 무렵에도 같은 값");
            }
        }

        [Test]
        public void Evaluator_FirstNightKeepsDayOneCap()
        {
            var evaluator = NewEvaluator(out _, out _);
            evaluator.SetTime(0.6f, true);
            Assert.AreEqual(3, evaluator.MaxAlive, "1일차 밤에도 동시 3마리");
        }

        [Test]
        public void Evaluator_NormalEnemySpeed_StaysInTheRequestedBands()
        {
            var evaluator = NewEvaluator(out _, out _);
            evaluator.SetTime(0f, false);
            Assert.That(evaluator.EnemySpeedRatio, Is.InRange(0.6f, 0.7f), "1일차 일반 적은 플레이어의 60~70%");
            evaluator.SetTime(4f, true);
            Assert.LessOrEqual(Mathf.Min(evaluator.EnemySpeedRatio, evaluator.NormalSpeedCapRatio), 0.85f, "5일차 밤에도 85% 이하");
        }

        [Test]
        public void Evaluator_ContactIntervalStartsAtLeast1_2_AndNeverIncreases()
        {
            var evaluator = NewEvaluator(out _, out _);
            evaluator.SetTime(0f, false);
            var previous = evaluator.ContactInterval;
            Assert.GreaterOrEqual(previous, 1.2f);
            for (var day = 1; day < 5; day++)
            {
                evaluator.SetTime(day, false);
                Assert.LessOrEqual(evaluator.ContactInterval, previous, "주기는 일차가 갈수록 줄어든다");
                previous = evaluator.ContactInterval;
            }
        }

        [Test]
        public void Evaluator_DamageGrowsByDay_AndHealthToo()
        {
            var evaluator = NewEvaluator(out _, out _);
            evaluator.SetTime(0f, false);
            var damage1 = evaluator.DamageMultiplier;
            var health1 = evaluator.HealthMultiplier;
            evaluator.SetTime(4f, false);
            Assert.Greater(evaluator.DamageMultiplier, damage1);
            Assert.Greater(evaluator.HealthMultiplier, health1);
        }

        [Test]
        public void Evaluator_FastEnemySlotsOpenOnDayThree()
        {
            var evaluator = NewEvaluator(out _, out _);
            evaluator.SetTime(1.5f, false);
            Assert.AreEqual(0, evaluator.FastEnemySlots, "2일차까지는 없음");
            evaluator.SetTime(2f, false);
            Assert.AreEqual(1, evaluator.FastEnemySlots, "3일차부터 동시에 1마리");
        }

        [Test]
        public void Tuning_OverridesAndResetWork()
        {
            var evaluator = NewEvaluator(out var profile, out var tuning);
            evaluator.SetTime(0f, false);
            var baseSpawn = evaluator.SpawnMultiplier;
            var baseDamage = evaluator.DamageMultiplier;

            tuning.SpawnScale = 2f;
            tuning.DamageScale = 0.5f;
            tuning.MaxAliveOverride = 7;
            tuning.InvulnerableOverride = 1.5f;
            Assert.AreEqual(baseSpawn * 2f, evaluator.SpawnMultiplier, 0.0001f);
            Assert.AreEqual(baseDamage * 0.5f, evaluator.DamageMultiplier, 0.0001f);
            Assert.AreEqual(7, evaluator.MaxAlive);
            Assert.AreEqual(1.5f, evaluator.PlayerInvulnerableSeconds, 0.0001f);
            Assert.IsTrue(tuning.HasChanges);

            tuning.Reset();
            Assert.IsFalse(tuning.HasChanges);
            Assert.AreEqual(3, evaluator.MaxAlive);
            Assert.AreEqual(profile.playerInvulnerableSeconds, evaluator.PlayerInvulnerableSeconds, 0.0001f);
        }

        [Test]
        public void ScaleCurve_ScalesEveryKeyValue()
        {
            var curve = new AnimationCurve(new Keyframe(0f, 2f), new Keyframe(1f, 4f));
            DifficultyProfile.ScaleCurve(curve, 0.5f);
            Assert.AreEqual(1f, curve.Evaluate(0f), 0.0001f);
            Assert.AreEqual(2f, curve.Evaluate(1f), 0.0001f);
        }

        // ---------- 속도 규칙 (빠른 적 슬롯) ----------

        [Test]
        public void SpeedPolicy_FastKindsAreCappedBeforeDayThree()
        {
            var evaluator = NewEvaluator(out _, out _);
            evaluator.SetTime(0f, false);
            var policy = evaluator.SpeedPolicy;

            var normal = policy.Resolve(1f, "normal");
            var fast = policy.Resolve(2f, "fast");
            Assert.AreEqual(evaluator.EnemySpeedRatio, normal, 0.0001f, "기준 종류는 일반 비율 그대로");
            Assert.AreEqual(evaluator.NormalSpeedCapRatio, fast, 0.0001f, "1일차에는 빠른 종류도 상한에 걸린다");
            Assert.Less(fast, 1f, "플레이어보다 빠른 적은 없다");
        }

        [Test]
        public void SpeedPolicy_OnlyOneFastEnemyMayExceedTheCap_AndSlotIsReturned()
        {
            var evaluator = NewEvaluator(out _, out _);
            evaluator.SetTime(2f, false); // 3일차
            var policy = evaluator.SpeedPolicy;
            var cap = evaluator.NormalSpeedCapRatio;

            var first = policy.Resolve(2f, "A");
            var second = policy.Resolve(2f, "B");
            Assert.Greater(first, cap, "첫 번째 빠른 적은 슬롯을 얻는다");
            Assert.AreEqual(cap, second, 0.0001f, "두 번째는 상한에 걸린다 (동시 1마리)");
            Assert.AreEqual(first, policy.Resolve(2f, "A"), 0.0001f, "슬롯 주인은 계속 빠르다");

            policy.Release("A");
            Assert.Greater(policy.Resolve(2f, "B"), cap, "슬롯이 비면 다음 적이 얻는다");
        }

        [Test]
        public void SpeedPolicy_FastEnemyNeverExceedsFastLimit()
        {
            var evaluator = NewEvaluator(out var profile, out _);
            evaluator.SetTime(4f, false);
            var value = evaluator.SpeedPolicy.Resolve(5f, "very fast");
            Assert.LessOrEqual(value, profile.fastSpeedRatio + 0.0001f);
        }

        // ---------- 스폰 유예 ----------

        [Test]
        public void EarlyCap_NoSpawnDuringGrace_ThenOneAtATime_ThenDayCap()
        {
            Assert.AreEqual(0, SpawnMath.EarlyCap(0f, 25f, 40f, 3));
            Assert.AreEqual(0, SpawnMath.EarlyCap(24.9f, 25f, 40f, 3), "유예 25초 안에는 0");
            Assert.AreEqual(1, SpawnMath.EarlyCap(25f, 25f, 40f, 3), "유예가 끝나면 1마리");
            Assert.AreEqual(1, SpawnMath.EarlyCap(39.9f, 25f, 40f, 3), "40초 전까지는 동시에 1마리");
            Assert.AreEqual(3, SpawnMath.EarlyCap(40f, 25f, 40f, 3), "그 뒤에는 일차 상한");
            Assert.AreEqual(0, SpawnMath.EarlyCap(30f, 25f, 40f, 0), "일차 상한이 0이면 1마리도 안 나온다");
        }

        [Test]
        public void IsOnScreen_UsesCameraRectPlusMargin()
        {
            // 카메라 중심 (0,0), 절반 크기 12.4 x 7, 여유 1.5
            Assert.IsTrue(SpawnMath.IsOnScreen(10f, 0f, 0f, 0f, 12.4f, 7f, 1.5f));
            Assert.IsTrue(SpawnMath.IsOnScreen(13.5f, 0f, 0f, 0f, 12.4f, 7f, 1.5f), "여유 안쪽은 화면으로 친다");
            Assert.IsFalse(SpawnMath.IsOnScreen(14f, 0f, 0f, 0f, 12.4f, 7f, 1.5f));
            Assert.IsFalse(SpawnMath.IsOnScreen(0f, 9f, 0f, 0f, 12.4f, 7f, 1.5f), "위아래도 같은 규칙");
            Assert.IsTrue(SpawnMath.IsOnScreen(-12f, -8f, 0f, 0f, 12.4f, 7f, 1.5f));
        }

        // ---------- 경험치 ----------

        [Test]
        public void Experience_AccumulatesWithoutLevelUpBelowRequirement()
        {
            var service = new ExperienceService(new FakeTable { Value = 10 });
            var levelUps = 0;
            service.OnLevelUp += _ => levelUps++;

            service.AddExperience(4);
            service.AddExperience(5);
            Assert.AreEqual(0, levelUps);
            Assert.AreEqual(1, service.Level);
            Assert.AreEqual(9, service.Current);
        }

        [Test]
        public void Experience_ReachingRequirementLevelsUpAndCarriesOver()
        {
            var service = new ExperienceService(new FakeTable { Value = 10 });
            var levels = new List<int>();
            service.OnLevelUp += levels.Add;

            service.AddExperience(14);
            CollectionAssert.AreEqual(new[] { 2 }, levels);
            Assert.AreEqual(4, service.Current, "남은 경험치는 새 레벨로 이월");
            Assert.AreEqual(2, service.Level);
        }

        [Test]
        public void Experience_ManyLevelsAtOnceRaiseEventsInOrder()
        {
            var service = new ExperienceService(new FakeTable { Value = 10 });
            var levels = new List<int>();
            var changed = new List<(int, int)>();
            service.OnLevelUp += levels.Add;
            service.OnExperienceChanged += (current, required) => changed.Add((current, required));

            service.AddExperience(35);
            CollectionAssert.AreEqual(new[] { 2, 3, 4 }, levels, "오른 순서대로 한 번씩");
            Assert.AreEqual(5, service.Current);
            Assert.AreEqual(1, changed.Count, "경험치 변경 이벤트는 한 번에 한 번, 최종 값으로");
            Assert.AreEqual((5, 10), changed[0]);
        }

        [Test]
        public void Experience_IgnoresZeroAndNegative_AndSurvivesBrokenTable()
        {
            var table = new FakeTable { Value = 0 };
            var service = new ExperienceService(table);
            service.AddExperience(0);
            service.AddExperience(-5);
            Assert.AreEqual(0, service.Current);

            Assert.DoesNotThrow(() => service.AddExperience(5), "필요량 0짜리 표가 있어도 무한 루프에 빠지지 않는다");
            Assert.GreaterOrEqual(service.Required, 1);
        }

        [Test]
        public void Experience_ResetReturnsToLevelOne()
        {
            var service = new ExperienceService(new FakeTable { Value = 10 });
            service.AddExperience(25);
            (int, int)? last = null;
            service.OnExperienceChanged += (current, required) => last = (current, required);

            service.Reset();
            Assert.AreEqual(1, service.Level);
            Assert.AreEqual(0, service.Current);
            Assert.AreEqual(0, service.TotalGained);
            Assert.AreEqual((0, 10), last, "HUD가 초기 값으로 돌아가도록 변경 이벤트를 낸다");
        }

        [Test]
        public void XpCurve_DefaultsRiseAndFirstLevelIsReachableInTime()
        {
            var curve = ScriptableObject.CreateInstance<XpCurve>();
            created.Add(curve);

            Assert.AreEqual(40, curve.RequiredFor(1));
            for (var level = 1; level < 14; level++)
                Assert.GreaterOrEqual(curve.RequiredFor(level + 1), curve.RequiredFor(level), "필요량은 줄지 않는다");
            Assert.GreaterOrEqual(curve.RequiredFor(999), 1);

            // 1레벨 필요량 40 = 기본 지급량 8인 적 5마리. 유예 25초 뒤 한 마리씩 나오는 초반에 60~90초 안에 잡을 수 있는 수준
            Assert.AreEqual(5, Mathf.CeilToInt(curve.RequiredFor(1) / 8f));
        }

        [Test]
        public void KillBridge_PassesRewardOfKilledEnemies_AndIgnoresPlainOnes_AndStopsAfterDispose()
        {
            var sink = new FakeSink();
            var bridge = new KillExperienceBridge(sink);
            var early = new RewardEnemy { ExperienceReward = 8 };
            EnemyRegistry.Register(early);
            early.Kill();

            var plain = new PlainEnemy();
            EnemyRegistry.Register(plain);
            plain.Kill();

            var late = new RewardEnemy { ExperienceReward = 12 };
            EnemyRegistry.Register(late);
            late.Kill();
            Assert.AreEqual(20, sink.Total);

            bridge.Dispose();
            early.Kill();
            Assert.AreEqual(20, sink.Total, "Dispose 뒤에는 구독이 끊긴다");
        }

        [Test]
        public void Stats_ReportsReachedLevel()
        {
            var service = new ExperienceService(new FakeTable { Value = 10 });
            var tracker = new StatsTracker(new FakeClock(), null, service);
            Assert.AreEqual(1, tracker.Snapshot().LevelReached);
            service.AddExperience(25);
            Assert.AreEqual(3, tracker.Snapshot().LevelReached);
            tracker.Dispose();
        }

        // ---------- 레벨업 → 증강 선택 ----------

        private static GameStateMachine PlayingMachine() => new GameStateMachine(GameState.Playing);

        [Test]
        public void LevelUp_PausesGameAndAsksForSelection()
        {
            var machine = PlayingMachine();
            var service = new ExperienceService(new FakeTable { Value = 10 });
            var presenter = new FakePresenter();
            using (new LevelUpCoordinator(machine, service, presenter))
            {
                service.AddExperience(10);

                Assert.AreEqual(GameState.AugmentSelect, machine.Current, "선택 대기 상태로 멈춘다");
                CollectionAssert.AreEqual(new[] { 2 }, presenter.Requested);
            }
        }

        [Test]
        public void LevelUp_DoesNotResumeUntilSelectionCompletes()
        {
            var machine = PlayingMachine();
            var service = new ExperienceService(new FakeTable { Value = 10 });
            var presenter = new FakePresenter();
            using (new LevelUpCoordinator(machine, service, presenter))
            {
                service.AddExperience(10);

                Assert.IsFalse(machine.Resume(), "일시정지 해제로는 풀 수 없다");
                Assert.IsFalse(machine.Pause());
                Assert.AreEqual(GameState.AugmentSelect, machine.Current);

                presenter.CompleteLast();
                Assert.AreEqual(GameState.Playing, machine.Current, "선택이 끝나야 재개");
            }
        }

        [Test]
        public void LevelUp_MultipleLevelsAreQueuedAndHandledOneByOne()
        {
            var machine = PlayingMachine();
            var service = new ExperienceService(new FakeTable { Value = 10 });
            var presenter = new FakePresenter();
            using (var coordinator = new LevelUpCoordinator(machine, service, presenter))
            {
                service.AddExperience(35); // 2, 3, 4레벨

                CollectionAssert.AreEqual(new[] { 2 }, presenter.Requested, "첫 선택만 열려 있다");
                Assert.AreEqual(2, coordinator.PendingCount, "나머지는 큐에서 기다린다");

                presenter.CompleteLast();
                CollectionAssert.AreEqual(new[] { 2, 3 }, presenter.Requested);
                Assert.AreEqual(GameState.AugmentSelect, machine.Current, "다음 선택이 이어지는 동안 재개되지 않는다");

                presenter.CompleteLast();
                CollectionAssert.AreEqual(new[] { 2, 3, 4 }, presenter.Requested);

                presenter.CompleteLast();
                Assert.AreEqual(GameState.Playing, machine.Current, "마지막 선택이 끝나면 재개");
                Assert.AreEqual(0, coordinator.PendingCount);
                Assert.IsFalse(coordinator.IsSelecting);
            }
        }

        [Test]
        public void LevelUp_DuplicateCompletionDoesNotSkipTheNextSelection()
        {
            var machine = PlayingMachine();
            var service = new ExperienceService(new FakeTable { Value = 10 });
            var presenter = new FakePresenter();
            using (new LevelUpCoordinator(machine, service, presenter))
            {
                service.AddExperience(25); // 2, 3레벨
                var firstCompletion = presenter.Completions[0];
                firstCompletion();
                Assert.AreEqual(2, presenter.Requested.Count);

                firstCompletion(); // 같은 콜백을 또 부른다
                Assert.AreEqual(GameState.AugmentSelect, machine.Current, "두 번째 선택은 아직 끝나지 않았다");
                Assert.AreEqual(2, presenter.Requested.Count);
            }
        }

        [Test]
        public void LevelUp_WhilePausedWaitsUntilPlaying()
        {
            var machine = PlayingMachine();
            var service = new ExperienceService(new FakeTable { Value = 10 });
            var presenter = new FakePresenter();
            using (new LevelUpCoordinator(machine, service, presenter))
            {
                machine.Pause();
                service.AddExperience(10);
                Assert.AreEqual(0, presenter.Requested.Count, "일시정지 중에는 열지 않는다");

                machine.Resume();
                CollectionAssert.AreEqual(new[] { 2 }, presenter.Requested, "재개하면 대기하던 선택이 열린다");
            }
        }

        [Test]
        public void LevelUp_ResetClearsQueueAndStateAndIgnoresStaleCompletion()
        {
            var machine = PlayingMachine();
            var service = new ExperienceService(new FakeTable { Value = 10 });
            var presenter = new FakePresenter();
            using (var coordinator = new LevelUpCoordinator(machine, service, presenter))
            {
                service.AddExperience(35);
                var stale = presenter.Completions[0];

                coordinator.Reset();
                service.Reset();

                Assert.AreEqual(0, coordinator.PendingCount, "큐가 비워진다");
                Assert.IsFalse(coordinator.IsSelecting);
                Assert.AreEqual(GameState.Playing, machine.Current, "대기 상태가 풀린다");
                Assert.AreEqual(1, presenter.Cancels, "열려 있던 선택 화면은 접는다");
                Assert.AreEqual(1, service.Level);

                stale();
                Assert.AreEqual(1, presenter.Requested.Count, "늦게 도착한 옛 콜백은 아무 일도 일으키지 않는다");
                Assert.AreEqual(GameState.Playing, machine.Current);
            }
        }

        [Test]
        public void StateMachine_AugmentSelectOnlyFromPlayingAndBackToPlaying()
        {
            var machine = new GameStateMachine(GameState.Title);
            Assert.IsFalse(machine.BeginAugmentSelect());
            machine.StartGame();
            Assert.IsTrue(machine.BeginAugmentSelect());
            Assert.IsFalse(machine.Win(), "선택 중에는 클리어/일시정지 같은 전환이 막힌다");
            Assert.IsFalse(machine.Pause());
            Assert.IsFalse(machine.Resume(), "선택이 끝나기 전에는 재개할 수 없다");
            Assert.IsTrue(machine.EndAugmentSelect());
            Assert.AreEqual(GameState.Playing, machine.Current);
            Assert.IsFalse(machine.EndAugmentSelect());
        }
    }
}
