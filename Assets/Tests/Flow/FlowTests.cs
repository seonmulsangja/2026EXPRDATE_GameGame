using System;
using System.Collections.Generic;
using ExpiryGame.Flow;
using NUnit.Framework;
using UnityEngine;

namespace ExpiryGame.Tests
{
    public class FlowTests
    {
        private sealed class FakeEnemy : IEnemyActor
        {
            public Vector3 Position { get; set; }
            public float Threat01 => 0.5f;
            public event Action<IEnemyActor> Killed;
            public void Kill() => Killed?.Invoke(this);
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

        private sealed class FakeInventory : IFoodInventory
        {
            public readonly List<FoodData> Added = new List<FoodData>();
            public IReadOnlyList<FoodItem> Items => new List<FoodItem>();
            public FoodItem Selected => null;
            public int Capacity => 5;
            public event Action Changed { add { } remove { } }
            public bool TryAdd(FoodItem item) => true;
            public bool TryAdd(FoodData data) { Added.Add(data); return true; }
            public bool Remove(FoodItem item) => true;
            public void SelectNext() { }
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var enemy in new List<IEnemyActor>(EnemyRegistry.Enemies)) EnemyRegistry.Unregister(enemy);
        }

        [Test]
        public void StateMachine_AllowsOnlyValidTransitions()
        {
            var machine = new GameStateMachine(GameState.Title);
            Assert.IsFalse(machine.Pause());
            Assert.IsFalse(machine.Fail());
            Assert.IsTrue(machine.StartGame());
            Assert.IsFalse(machine.StartGame());
            Assert.IsTrue(machine.Pause());
            Assert.IsFalse(machine.Win());
            Assert.IsTrue(machine.Resume());
            Assert.IsTrue(machine.Fail());
            Assert.AreEqual(GameState.GameOver, machine.Current);
            Assert.IsFalse(machine.Resume());
            Assert.IsFalse(machine.Win());
        }

        [Test]
        public void StateMachine_RaisesEventWithPreviousAndNewState()
        {
            var machine = new GameStateMachine(GameState.Playing);
            (GameState from, GameState to)? seen = null;
            machine.StateChanged += (from, to) => seen = (from, to);
            machine.Win();
            Assert.AreEqual((GameState.Playing, GameState.Clear), seen);
        }

        [Test]
        public void SurvivalGoal_ClearsWhenTargetDaysCompleted()
        {
            var goal = ScriptableObject.CreateInstance<SurvivalGoal>();
            goal.surviveDays = 5;
            Assert.IsFalse(goal.IsCleared(5), "5일차 새벽은 4일을 버텼을 뿐");
            Assert.IsTrue(goal.IsCleared(6));
            UnityEngine.Object.DestroyImmediate(goal);
        }

        [Test]
        public void Score_UsesRules_AndNeverGoesNegative()
        {
            var rules = ScriptableObject.CreateInstance<ScoreRules>();
            var stats = new GameStats { Kills = 3, DaysSurvived = 2, FoodEaten = 4, RottenEaten = 1 };
            var expected = 3 * rules.perKill + 2 * rules.perDaySurvived + 4 * rules.perFoodEaten + rules.perRottenEaten;
            Assert.AreEqual(expected, ScoreCalculator.Calculate(stats, rules, false));
            Assert.AreEqual(expected + rules.clearBonus, ScoreCalculator.Calculate(stats, rules, true));

            var bad = new GameStats { RottenEaten = 100 };
            Assert.AreEqual(0, ScoreCalculator.Calculate(bad, rules, false));
            UnityEngine.Object.DestroyImmediate(rules);
        }

        [Test]
        public void DifficultyProfile_FirstDayIsEasierThanLater()
        {
            var profile = ScriptableObject.CreateInstance<DifficultyProfile>();
            Assert.Less(profile.spawn.Evaluate(0f), profile.spawn.Evaluate(2f));
            Assert.Less(profile.health.Evaluate(0f), profile.health.Evaluate(4f));
            Assert.Less(profile.spawn.Evaluate(0f), 1f);
            UnityEngine.Object.DestroyImmediate(profile);
        }

        [Test]
        public void StatsTracker_CountsKillsOfRegisteredEnemies_AndStopsAfterDispose()
        {
            var clock = new FakeClock { DayCount = 3 };
            var tracker = new StatsTracker(clock, null);
            var early = new FakeEnemy();
            EnemyRegistry.Register(early);
            early.Kill();

            var late = new FakeEnemy();
            EnemyRegistry.Register(late);
            late.Kill();

            Assert.AreEqual(2, tracker.Snapshot().Kills);
            Assert.AreEqual(2, tracker.Snapshot().DaysSurvived);

            tracker.Dispose();
            early.Kill();
            Assert.AreEqual(2, tracker.Snapshot().Kills, "Dispose 뒤에는 구독이 끊겨야 한다 (씬 리로드 대비)");
        }

        [Test]
        public void StatsTracker_FreezeKeepsDaysAtGameEnd()
        {
            var clock = new FakeClock { DayCount = 4 };
            var tracker = new StatsTracker(clock, null);
            tracker.Freeze();
            clock.DayCount = 9;
            Assert.AreEqual(3, tracker.Snapshot().DaysSurvived);
            tracker.Dispose();
        }

        [Test]
        public void EnemyRegistry_CountsOnlyEnemiesWithinRadius()
        {
            EnemyRegistry.Register(new FakeEnemy { Position = new Vector3(1f, 0f) });
            EnemyRegistry.Register(new FakeEnemy { Position = new Vector3(5f, 0f) });
            Assert.AreEqual(1, EnemyRegistry.CountWithin(Vector2.zero, 2f));
            Assert.AreEqual(2, EnemyRegistry.CountWithin(Vector2.zero, 6f));
        }

        [Test]
        public void LoadoutGiver_OverridesShelfLifeWithACopy_AndKeepsOriginalAsset()
        {
            var fish = ScriptableObject.CreateInstance<FoodData>();
            fish.shelfLifeSeconds = 30f;
            var bread = ScriptableObject.CreateInstance<FoodData>();
            bread.shelfLifeSeconds = 120f;

            var loadout = ScriptableObject.CreateInstance<StartingLoadout>();
            loadout.entries = new[]
            {
                new LoadoutEntry { food = bread, shelfLifeSeconds = 120f },
                new LoadoutEntry { food = fish, shelfLifeSeconds = 45f },
            };

            var inventory = new FakeInventory();
            var giver = new LoadoutGiver();
            giver.Give(inventory, loadout);

            Assert.AreEqual(2, inventory.Added.Count);
            Assert.AreSame(bread, inventory.Added[0], "같은 값이면 원본을 그대로 쓴다");
            Assert.AreEqual(45f, inventory.Added[1].shelfLifeSeconds);
            Assert.AreEqual(30f, fish.shelfLifeSeconds, "원본 SO는 바뀌면 안 된다");

            giver.Release();
            UnityEngine.Object.DestroyImmediate(fish);
            UnityEngine.Object.DestroyImmediate(bread);
            UnityEngine.Object.DestroyImmediate(loadout);
        }
    }
}
