using System.Collections.Generic;
using ExpiryGame.Landmark;
using NUnit.Framework;
using UnityEngine;

namespace ExpiryGame.Tests
{
    // 모닥불 연료: 감소/회복, 단계 전환, 꺼짐 중 효과 중단, 재점화, 언로드 후 상태 유지, 다시하기 초기화
    public class CampfireFuelTests
    {
        private sealed class FakeEvents : ILandmarkEventSink
        {
            public readonly List<CampfireStageInfo> Stages = new List<CampfireStageInfo>();
            void ILandmarkEventSink.FoodEnhanced(FoodEnhancedInfo info) { }
            void ILandmarkEventSink.FridgeOpened(FridgeOpenedInfo info) { }
            void ILandmarkEventSink.CampfireStageChanged(CampfireStageInfo info) => Stages.Add(info);
        }

        private sealed class FakeInventory : IFoodInventory
        {
            public IReadOnlyList<FoodItem> Items => new List<FoodItem>();
            public FoodItem Selected => null;
            public int Capacity => 5;
            public event System.Action Changed { add { } remove { } }
            public bool TryAdd(FoodItem item) => true;
            public bool TryAdd(FoodData data) => true;
            public bool Remove(FoodItem item) => true;
            public void SelectNext() { }
        }

        private readonly List<Object> created = new List<Object>();
        private CampfireFuelProfile profile;

        [SetUp]
        public void SetUp()
        {
            profile = Make<CampfireFuelProfile>();
            profile.maxFuel = 100f;
            profile.drainPerSecondInside = 2f;
            profile.regenPerSecondOutside = 1f;
            profile.nightDrainMultiplier = 2f;
            profile.relightThreshold01 = 0.25f;
            profile.enhanceFuelCost = 10f;
            profile.fullMin01 = 0.6f;
            profile.weakMin01 = 0.3f;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var item in created)
                if (item != null) Object.DestroyImmediate(item);
            created.Clear();
        }

        private T Make<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            created.Add(asset);
            return asset;
        }

        private LandmarkData CampfireData()
        {
            var data = Make<LandmarkData>();
            data.kind = LandmarkKind.Campfire;
            data.fuelProfile = profile;
            data.safeZone = Make<SafeZoneProfile>();
            data.safeZone.radius = 10f;
            data.safeZone.radiusChangePerSecond = 2f;
            data.healPerSecond = 2f;
            data.enhanceProfile = Make<EnhanceProfile>();
            return data;
        }

        private CampfireFuel NewFuel(LandmarkData data, CampfireStateStore store, string id, FakeEvents events = null)
        {
            var go = new GameObject("Campfire");
            created.Add(go);
            var fuel = go.AddComponent<CampfireFuel>();
            fuel.Bind(new LandmarkContext { Data = data, Id = id, Store = store, Events = events });
            return fuel;
        }

        // ---------- 감소 / 회복 ----------

        [Test]
        public void Fuel_DrainsWhilePlayerInside_AndNightDrainsFaster()
        {
            var day = new CampfireFuelModel(profile);
            day.Tick(10f, true, 0f);
            Assert.AreEqual(80f, day.Fuel, 0.001f, "낮: 초당 2 × 10초");

            var night = new CampfireFuelModel(profile);
            night.Tick(10f, true, 1f);
            Assert.AreEqual(60f, night.Fuel, 0.001f, "밤: 배율 2 → 초당 4 × 10초");
        }

        [Test]
        public void Fuel_RegeneratesOutside_AndClampsToMax()
        {
            var model = new CampfireFuelModel(profile, new CampfireState { Fuel = 50f, Lit = true });
            model.Tick(10f, false, 0f);
            Assert.AreEqual(60f, model.Fuel, 0.001f);

            model.Tick(1000f, false, 0f);
            Assert.AreEqual(100f, model.Fuel, 0.001f);
        }

        // ---------- 단계 ----------

        [Test]
        public void Stage_FollowsRatioBoundaries()
        {
            var model = new CampfireFuelModel(profile);
            Assert.AreEqual(CampfireStage.Full, model.Stage);

            model.Tick(21f, true, 0f);                 // 100 → 58
            Assert.AreEqual(CampfireStage.Weak, model.Stage);

            model.Tick(14f, true, 0f);                 // 58 → 30 : 경계(0.3)는 약함
            Assert.AreEqual(CampfireStage.Weak, model.Stage);

            model.Tick(1f, true, 0f);                  // 28
            Assert.AreEqual(CampfireStage.Embers, model.Stage);

            model.Tick(100f, true, 0f);                // 0
            Assert.AreEqual(CampfireStage.Out, model.Stage);
        }

        [Test]
        public void StageChange_RaisesEventOncePerTransition()
        {
            var events = new FakeEvents();
            var fuel = NewFuel(CampfireData(), new CampfireStateStore(), "A", events);

            fuel.Advance(1f, true, 0f);                // 98 : 그대로 가득
            Assert.AreEqual(0, events.Stages.Count);

            fuel.Advance(20f, true, 0f);               // 58 : 약함
            Assert.AreEqual(1, events.Stages.Count);
            Assert.AreEqual(CampfireStage.Full, events.Stages[0].From);
            Assert.AreEqual(CampfireStage.Weak, events.Stages[0].To);

            fuel.Advance(1f, true, 0f);                // 56 : 변화 없음
            Assert.AreEqual(1, events.Stages.Count);
        }

        // ---------- 꺼짐 / 재점화 ----------

        [Test]
        public void Out_StopsEffects_LightHealSafeZoneAndEnhance()
        {
            var data = CampfireData();
            var go = new GameObject("Campfire");
            created.Add(go);
            var fuel = go.AddComponent<CampfireFuel>();
            var safe = go.AddComponent<CampfireSafeZone>();
            var enhance = go.AddComponent<CampfireEnhanceEffect>();
            var context = new LandmarkContext { Data = data, Id = "A", Store = new CampfireStateStore(), Inventory = new FakeInventory() };
            fuel.Bind(context);
            safe.Bind(context);
            enhance.Bind(context);

            Assert.AreEqual(10f, safe.Radius, 0.001f, "가득이면 최대 반경");
            Assert.AreEqual(1f, fuel.Strength(profile.healAtEmpty), 0.001f);

            fuel.Advance(100f, true, 0f);
            Assert.IsFalse(fuel.IsLit);
            Assert.AreEqual(0f, fuel.Strength(profile.lightIntensityAtEmpty), "꺼지면 빛 배율 0");
            Assert.AreEqual(0f, fuel.Strength(profile.healAtEmpty), "꺼지면 회복 배율 0");
            Assert.AreEqual(0f, safe.Radius, 0.001f, "꺼지면 안전지대 없음");
            Assert.IsTrue(enhance.CanInteract, "꺼져도 프롬프트는 계속 보인다");
            StringAssert.Contains("꺼짐", enhance.PromptText);
        }

        [Test]
        public void Relight_AutomaticallyOnceFuelPassesThreshold()
        {
            var model = new CampfireFuelModel(profile);
            model.Tick(100f, true, 0f);
            Assert.IsFalse(model.Lit);

            model.Tick(20f, false, 0f);                // 20% : 아직 임계값 미만
            Assert.IsFalse(model.Lit);
            Assert.AreEqual(CampfireStage.Out, model.Stage);

            model.Tick(5f, false, 0f);                 // 25% : 임계값
            Assert.IsTrue(model.Lit);
            Assert.AreNotEqual(CampfireStage.Out, model.Stage);
        }

        [Test]
        public void Relight_PlayerStayingInsideDoesNotRelight()
        {
            var model = new CampfireFuelModel(profile);
            model.Tick(100f, true, 0f);
            model.Tick(500f, true, 0f);
            Assert.IsFalse(model.Lit);
            Assert.AreEqual(0f, model.Fuel, 0.001f);
        }

        // ---------- 굽기 연료 ----------

        [Test]
        public void Enhance_ConsumesFuel_AndBlocksWhenNotEnough()
        {
            var model = new CampfireFuelModel(profile, new CampfireState { Fuel = 15f, Lit = true });
            Assert.IsTrue(model.TryConsume(profile.enhanceFuelCost));
            Assert.AreEqual(5f, model.Fuel, 0.001f);
            Assert.IsFalse(model.CanAfford(profile.enhanceFuelCost), "연료 부족");
            Assert.IsFalse(model.TryConsume(profile.enhanceFuelCost));
            Assert.AreEqual(5f, model.Fuel, 0.001f);
        }

        // ---------- 반응(보간) ----------

        [Test]
        public void Strength_InterpolatesBetweenAtEmptyAndFull()
        {
            var model = new CampfireFuelModel(profile, new CampfireState { Fuel = 50f, Lit = true });
            Assert.AreEqual(0.7f, model.Strength(0.4f), 0.001f, "0.4 ~ 1 의 중간");
            Assert.AreEqual(1f, new CampfireFuelModel(profile).Strength(0.4f), 0.001f);
        }

        // ---------- 언로드 / 로드 / 다시하기 ----------

        [Test]
        public void State_SurvivesUnloadAndReload_ByLandmarkId()
        {
            var data = CampfireData();
            var store = new CampfireStateStore();

            var first = NewFuel(data, store, "Campfire@100,200");
            first.Advance(30f, true, 0f);              // 100 → 40
            first.Save();
            Object.DestroyImmediate(first.gameObject);

            var reloaded = NewFuel(data, store, "Campfire@100,200");
            Assert.AreEqual(40f, reloaded.Fuel, 0.5f, "언로드된 시간만큼 조금 회복돼 있을 수 있다");
            Assert.AreEqual(CampfireStage.Weak, reloaded.Stage);

            var other = NewFuel(data, store, "Campfire@999,999");
            Assert.AreEqual(100f, other.Fuel, 0.001f, "다른 모닥불은 영향 없음");
        }

        [Test]
        public void State_OutStaysOutAfterReload()
        {
            var data = CampfireData();
            var store = new CampfireStateStore();
            var first = NewFuel(data, store, "A");
            first.Advance(100f, true, 0f);
            first.Save();
            Object.DestroyImmediate(first.gameObject);

            var reloaded = NewFuel(data, store, "A");
            Assert.IsFalse(reloaded.IsLit);
            Assert.AreEqual(CampfireStage.Out, reloaded.Stage);
        }

        [Test]
        public void NewStore_StartsFull_LikeSceneReload()
        {
            var data = CampfireData();
            var oldStore = new CampfireStateStore();
            var first = NewFuel(data, oldStore, "A");
            first.Advance(100f, true, 0f);
            first.Save();
            Assert.AreEqual(1, oldStore.Count);

            // 다시하기 = 씬 리로드 = LandmarkSpawner가 새 저장소를 만든다
            var freshStore = new CampfireStateStore();
            var again = NewFuel(data, freshStore, "A");
            Assert.AreEqual(100f, again.Fuel, 0.001f);
            Assert.IsTrue(again.IsLit);
        }

        [Test]
        public void CatchUp_RegeneratesForTheTimeAwayWhileUnloaded()
        {
            var model = new CampfireFuelModel(profile, new CampfireState { Fuel = 10f, Lit = true });
            model.CatchUpAway(30f);
            Assert.AreEqual(40f, model.Fuel, 0.001f);
        }

        // ---------- 안전지대 집합 (청크 생성 때 판단) ----------

        [Test]
        public void SafeZoneSet_UsesCurrentFuel_OutCampfireIsNotSafe()
        {
            var data = CampfireData();
            var store = new CampfireStateStore();
            var position = new Vector2(10f, 10f);
            var info = new LandmarkInfo(data, position, 0f);

            Assert.IsTrue(SafeZoneSet.From(new[] { info }, store).Contains(position + new Vector2(9f, 0f)), "상태 없음 = 가득 = 반경 10");

            store.Save(info.Id, new CampfireFuelModel(profile, new CampfireState { Fuel = 0f, Lit = false }).Snapshot(0f));
            Assert.AreEqual(0, SafeZoneSet.From(new[] { info }, store).Count, "꺼진 모닥불은 스폰을 막지 않는다");

            store.Save(info.Id, new CampfireState { Fuel = 50f, Lit = true });
            var half = SafeZoneSet.From(new[] { info }, store);   // 0.4 ~ 1 의 중간 0.7 → 반경 7
            Assert.IsTrue(half.Contains(position + new Vector2(6f, 0f)));
            Assert.IsFalse(half.Contains(position + new Vector2(8f, 0f)));
        }

        [Test]
        public void NoFuelProfile_BehavesLikeAlwaysLit()
        {
            var data = CampfireData();
            data.fuelProfile = null;
            var fuel = NewFuel(data, new CampfireStateStore(), "A");
            fuel.Advance(1000f, true, 1f);
            Assert.IsTrue(fuel.IsLit);
            Assert.AreEqual(1f, fuel.Strength(0.1f));
            Assert.IsTrue(fuel.TryConsume(999f));
        }
    }
}
