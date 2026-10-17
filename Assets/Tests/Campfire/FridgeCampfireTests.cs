using System;
using System.Collections.Generic;
using ExpiryGame.Drop;
using ExpiryGame.Landmark;
using NUnit.Framework;
using UnityEngine;

namespace ExpiryGame.Tests
{
    public class FridgeCampfireTests
    {
        private sealed class FakeInventory : IFoodInventory
        {
            public FoodItem SelectedItem;
            public IReadOnlyList<FoodItem> Items => new List<FoodItem>();
            public FoodItem Selected => SelectedItem;
            public int Capacity => 5;
            public event Action Changed { add { } remove { } }
            public bool TryAdd(FoodItem item) => true;
            public bool TryAdd(FoodData data) => true;
            public bool Remove(FoodItem item) => true;
            public void SelectNext() { }
        }

        private sealed class FakeDrops : IDropService
        {
            public readonly List<DropTable> Dropped = new List<DropTable>();
            public event Action<FoodItem, Vector3> FoodDropped { add { } remove { } }
            public event Action<FoodItem> FoodCollected { add { } remove { } }
            public void Drop(DropTable table, float threat01, Vector3 position) => Dropped.Add(table);
        }

        private sealed class FakeEvents : ILandmarkEventSink
        {
            public int Enhanced;
            public int FridgeOpened;
            public int StageChanges;
            void ILandmarkEventSink.CampfireStageChanged(CampfireStageInfo info) => StageChanges++;
            void ILandmarkEventSink.FoodEnhanced(FoodEnhancedInfo info) => Enhanced++;
            void ILandmarkEventSink.FridgeOpened(FridgeOpenedInfo info) => FridgeOpened++;
        }

        private sealed class FakeSafeZone : ISafeZone
        {
            public Vector2 Center { get; set; }
            public float Radius { get; set; }
            public bool RepelEnemies { get; set; } = true;
            public float RepelDistance => 1.5f;
            public float RepelIntervalSeconds => 0.25f;
        }

        private readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();
        private EnhanceProfile profile;

        [SetUp]
        public void SetUp()
        {
            profile = Make<EnhanceProfile>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var item in created)
                if (item != null) UnityEngine.Object.DestroyImmediate(item);
            created.Clear();
        }

        private T Make<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            created.Add(asset);
            return asset;
        }

        private FoodItem NewFood(float shelfLifeSeconds = 120f, float heal = 31f, float damage = 20f)
        {
            var data = Make<FoodData>();
            data.shelfLifeSeconds = shelfLifeSeconds;
            return new FoodItem(data, new FoodStats(heal, damage, 5, heal * -0.5f, damage * 0.5f));
        }

        // ---------- 모닥불: 강화 ----------

        [Test]
        public void Enhance_AppliesOnlyOncePerFood()
        {
            var enhancer = new FoodEnhancer(profile);
            var food = NewFood();

            Assert.IsTrue(enhancer.Check(food, 0f).CanEnhance);
            Assert.IsTrue(enhancer.Apply(food));
            Assert.AreEqual(EnhanceBlock.AlreadyEnhanced, enhancer.Check(food, 0f).Block);
            Assert.IsFalse(enhancer.Apply(food));

            // 두 번째 시도가 배율을 또 곱하지 않는다
            Assert.AreEqual(31f * 1.3f, food.CurrentHeal, 0.001f);
            Assert.AreEqual(20f * 1.3f, food.CurrentDamage, 0.001f);
        }

        [Test]
        public void Enhance_OnlyMultipliesExistingStats_DoesNotChangeConversion()
        {
            var food = NewFood();
            new FoodEnhancer(profile).Apply(food);

            Assert.AreEqual(31f, food.Stats.Heal, 0.0001f);
            Assert.AreEqual(1.3f, food.HealMultiplier, 0.0001f);
            Assert.IsTrue(food.IsEnhanced);
        }

        [Test]
        public void Enhance_RejectsRottenFood()
        {
            var enhancer = new FoodEnhancer(profile);
            var food = NewFood();
            food.Tick(1000f);

            Assert.IsTrue(food.IsRotten);
            Assert.AreEqual(EnhanceBlock.Rotten, enhancer.Check(food, 0f).Block);
            Assert.IsFalse(enhancer.Apply(food));
            Assert.IsFalse(food.IsEnhanced);
            Assert.AreEqual(food.Stats.RottenHeal, food.CurrentHeal, 0.0001f);
        }

        [Test]
        public void Enhance_RejectsEmptySlot()
        {
            var enhancer = new FoodEnhancer(profile);
            Assert.AreEqual(EnhanceBlock.NoFood, enhancer.Check(null, 0f).Block);
            Assert.IsFalse(enhancer.Apply(null));
        }

        [Test]
        public void Enhance_ExtendsShelfLifeAndClampsToMax()
        {
            var enhancer = new FoodEnhancer(profile);

            var half = NewFood(120f);
            half.SetShelfLife01(0.5f);
            enhancer.Apply(half);
            Assert.AreEqual(90f, half.RemainingShelfLife, 0.001f);

            var nearlyFull = NewFood(120f);
            nearlyFull.SetShelfLife01(0.9f);
            enhancer.Apply(nearlyFull);
            Assert.AreEqual(120f, nearlyFull.RemainingShelfLife, 0.001f);
        }

        [Test]
        public void Enhance_CooldownBlocksUntilItEnds()
        {
            var enhancer = new FoodEnhancer(profile);
            var timer = new CooldownTimer();
            Assert.IsTrue(timer.IsReady(0f));

            timer.Start(10f, profile.cooldownSeconds);
            var blocked = enhancer.Check(NewFood(), timer.Remaining(15f));
            Assert.AreEqual(EnhanceBlock.Cooldown, blocked.Block);
            Assert.AreEqual(profile.cooldownSeconds - 5f, blocked.CooldownLeft, 0.001f);

            Assert.IsTrue(timer.IsReady(10f + profile.cooldownSeconds));
            Assert.IsTrue(enhancer.Check(NewFood(), timer.Remaining(10f + profile.cooldownSeconds)).CanEnhance);
        }

        [Test]
        public void EnhancePrompt_ShowsChangeOrReason()
        {
            var enhancer = new FoodEnhancer(profile);
            var food = NewFood(heal: 31f);
            Assert.AreEqual("굽기 (회복 +31 → +40)", profile.PromptFor(enhancer.Check(food, 0f)));

            Assert.AreEqual("굽기 불가 (쿨다운 4초)", profile.PromptFor(enhancer.Check(food, 3.2f)));

            enhancer.Apply(food);
            Assert.AreEqual("굽기 불가 (이미 강화됨)", profile.PromptFor(enhancer.Check(food, 0f)));

            var rotten = NewFood();
            rotten.Tick(1000f);
            Assert.AreEqual("굽기 불가 (썩음)", profile.PromptFor(enhancer.Check(rotten, 0f)));
        }

        [Test]
        public void CampfireEffect_EnhancesOnceThenCooldownBlocksNextFood()
        {
            var data = Make<LandmarkData>();
            data.kind = LandmarkKind.Campfire;
            data.enhanceProfile = profile;
            var inventory = new FakeInventory();
            var events = new FakeEvents();

            var go = new GameObject("Campfire");
            created.Add(go);
            var effect = go.AddComponent<CampfireEnhanceEffect>();
            effect.Bind(new LandmarkContext { Data = data, Inventory = inventory, Events = events });

            var first = NewFood();
            var second = NewFood();
            inventory.SelectedItem = first;
            effect.Interact();
            effect.Interact();
            Assert.IsTrue(first.IsEnhanced);
            Assert.AreEqual(1, events.Enhanced);

            inventory.SelectedItem = second;
            effect.Interact();
            Assert.IsFalse(second.IsEnhanced, "쿨다운 중에는 다른 음식도 강화되지 않는다");
            StringAssert.Contains("쿨다운", effect.PromptText);
            Assert.AreEqual(1, events.Enhanced);
        }

        // ---------- 냉장고 ----------

        [Test]
        public void Fridge_OpensOnlyOnce_ThenShowsEmptyAndTurnsGlowOff()
        {
            var data = Make<LandmarkData>();
            data.kind = LandmarkKind.Fridge;
            data.fridgeTable = Make<DropTable>();
            data.fridgeRolls = 2;
            var drops = new FakeDrops();
            var events = new FakeEvents();

            var go = new GameObject("Fridge");
            created.Add(go);
            var glow = new GameObject("Glow");
            glow.transform.SetParent(go.transform, false);
            var effect = go.AddComponent<FridgeLootEffect>();
            effect.Bind(new LandmarkContext { Data = data, Drops = drops, Events = events });

            Assert.AreEqual(data.interactPrompt, effect.PromptText);
            effect.Interact();
            effect.Interact();

            Assert.AreEqual(2, drops.Dropped.Count, "fridgeRolls번만 굴리고 다시는 안 나온다");
            Assert.AreSame(data.fridgeTable, drops.Dropped[0]);
            Assert.AreEqual(1, events.FridgeOpened);
            Assert.AreEqual(data.fridgeEmptyPrompt, effect.PromptText);
            Assert.IsFalse(glow.activeSelf);
        }

        [Test]
        public void Fridge_DecayMultiplierCanBeToggledInLandmarkData()
        {
            var data = Make<LandmarkData>();
            data.kind = LandmarkKind.Fridge;
            data.effectRadius = 5f;
            data.decayMultiplier = 0.3f;

            var go = new GameObject("Fridge");
            created.Add(go);
            var effect = go.AddComponent<FridgeDecayEffect>();
            effect.Bind(new LandmarkContext { Data = data });

            Assert.AreEqual(0.3f, effect.GetDecayRateMultiplier(new Vector2(1f, 0f)), 0.0001f);
            data.decayEnabled = false;
            Assert.AreEqual(1f, effect.GetDecayRateMultiplier(new Vector2(1f, 0f)), 0.0001f);
        }

        // ---------- 모닥불: 안전지대 ----------

        private LandmarkInfo Info(LandmarkKind kind, Vector2 position, SafeZoneProfile safe)
        {
            var data = Make<LandmarkData>();
            data.kind = kind;
            data.safeZone = safe;
            return new LandmarkInfo(data, position, 0f);
        }

        [Test]
        public void SafeZoneSet_ExcludesPositionsInsideCampfireRadius()
        {
            var safe = Make<SafeZoneProfile>();
            safe.radius = 4f;
            var set = SafeZoneSet.From(new[]
            {
                Info(LandmarkKind.Campfire, new Vector2(10f, 10f), safe),
                Info(LandmarkKind.Fridge, new Vector2(50f, 50f), safe),
                Info(LandmarkKind.Campfire, new Vector2(80f, 80f), null),
            });

            Assert.AreEqual(1, set.Count, "안전지대는 SafeZoneProfile이 있는 모닥불만");
            Assert.IsTrue(set.Contains(new Vector2(10f, 10f)));
            Assert.IsTrue(set.Contains(new Vector2(13.9f, 10f)));
            Assert.IsFalse(set.Contains(new Vector2(14.1f, 10f)));
            Assert.IsFalse(set.Contains(new Vector2(50f, 50f)), "냉장고는 안전지대가 아니다");
            Assert.IsFalse(set.Contains(new Vector2(80f, 80f)));
            Assert.IsFalse(SafeZoneSet.Empty.Contains(Vector2.zero));
        }

        [Test]
        public void SafeZoneRegistry_FindsZoneAndGivesSafetyPriority()
        {
            var zone = new FakeSafeZone { Center = new Vector2(5f, 5f), Radius = 4f };
            SafeZoneRegistry.Register(zone);
            try
            {
                Assert.IsTrue(SafeZoneRegistry.IsSafe(new Vector2(6f, 5f)));
                Assert.IsFalse(SafeZoneRegistry.IsSafe(new Vector2(20f, 5f)));
                Assert.IsTrue(SafeZoneRegistry.TryFind(new Vector2(6f, 5f), out var found));
                Assert.IsTrue(found.RepelEnemies);
            }
            finally
            {
                SafeZoneRegistry.Unregister(zone);
            }
            Assert.IsFalse(SafeZoneRegistry.IsSafe(new Vector2(6f, 5f)));
        }

        [Test]
        public void CampfireSafeZone_ReadsRadiusAndRepelToggleFromProfile()
        {
            var safe = Make<SafeZoneProfile>();
            safe.radius = 6f;
            var data = Make<LandmarkData>();
            data.kind = LandmarkKind.Campfire;
            data.safeZone = safe;

            var go = new GameObject("Campfire");
            created.Add(go);
            var zone = go.AddComponent<CampfireSafeZone>();
            zone.Bind(new LandmarkContext { Data = data });

            Assert.AreEqual(6f, zone.Radius);
            Assert.IsTrue(zone.RepelEnemies, "기본값은 ON");
            safe.repelEnemies = false;
            Assert.IsFalse(zone.RepelEnemies);
            Assert.AreEqual(6f, data.RingRadius);
        }
    }
}
