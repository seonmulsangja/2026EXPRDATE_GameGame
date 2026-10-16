using System.Collections.Generic;
using ExpiryGame.Landmark;
using ExpiryGame.MapGen;
using ExpiryGame.World;
using NUnit.Framework;
using UnityEngine;

namespace ExpiryGame.Tests
{
    public class ArtV2Tests
    {
        private sealed class FixedModifier : IDecayRateModifier
        {
            public float Multiplier { get; set; }
            public float GetDecayRateMultiplier(Vector2 worldPosition) => worldPosition.x < 10f ? Multiplier : 1f;
        }

        private static LandmarkSettings NewSettings(int seed = 777)
        {
            var map = ScriptableObject.CreateInstance<MapGenSettings>();
            map.seed = seed;
            var settings = ScriptableObject.CreateInstance<LandmarkSettings>();
            settings.map = map;
            var a = ScriptableObject.CreateInstance<LandmarkData>();
            a.kind = LandmarkKind.Fridge;
            a.size = new Vector2(2f, 2.5f);
            var b = ScriptableObject.CreateInstance<LandmarkData>();
            b.kind = LandmarkKind.Dump;
            b.size = new Vector2(2.5f, 2f);
            var start = ScriptableObject.CreateInstance<LandmarkData>();
            start.kind = LandmarkKind.Campfire;
            start.size = new Vector2(1.5f, 1.5f);
            settings.landmarks = new[] { a, b };
            settings.startLandmark = start;
            return settings;
        }

        [Test]
        public void SameSeedGivesSameLandmarks()
        {
            var first = new LandmarkPlanner(NewSettings());
            var second = new LandmarkPlanner(NewSettings());
            for (var y = -4; y <= 4; y++)
            for (var x = -4; x <= 4; x++)
            {
                var region = new Vector2Int(x, y);
                var hasFirst = first.TryGet(region, out var p1);
                var hasSecond = second.TryGet(region, out var p2);
                Assert.AreEqual(hasFirst, hasSecond);
                if (hasFirst)
                {
                    Assert.AreEqual(p1.Position, p2.Position);
                    Assert.AreEqual(p1.Data.kind, p2.Data.kind);
                }
            }
        }

        [Test]
        public void StartRegionAlwaysHasGuaranteedLandmarkNearStart()
        {
            for (var seed = 0; seed < 50; seed++)
            {
                var settings = NewSettings(seed);
                var planner = new LandmarkPlanner(settings);
                Assert.IsTrue(planner.TryGet(planner.RegionOf(Vector2.zero), out var placement));
                Assert.AreEqual(LandmarkKind.Campfire, placement.Data.kind);
                Assert.Less(Vector2.Distance(placement.Position, Vector2.zero), 30f);
            }
        }

        [Test]
        public void LandmarksStayInsideRegionMargin()
        {
            var settings = NewSettings();
            var planner = new LandmarkPlanner(settings);
            var size = planner.RegionSizeTiles;
            for (var y = -5; y <= 5; y++)
            for (var x = -5; x <= 5; x++)
            {
                if (!planner.TryGet(new Vector2Int(x, y), out var p)) continue;
                Assert.GreaterOrEqual(p.Position.x, x * size + settings.regionMargin - 0.001f);
                Assert.LessOrEqual(p.Position.x, (x + 1) * size - settings.regionMargin + 0.001f);
                Assert.GreaterOrEqual(p.Position.y, y * size + settings.regionMargin - 0.001f);
                Assert.LessOrEqual(p.Position.y, (y + 1) * size - settings.regionMargin + 0.001f);
            }
        }

        [Test]
        public void ObstaclesAreClearedAroundLandmark()
        {
            var planner = new LandmarkPlanner(NewSettings());
            Assert.IsTrue(planner.TryGet(planner.RegionOf(Vector2.zero), out var placement));
            Assert.IsTrue(planner.IsInClearArea(placement.Position + Vector2.up * 0.5f));
            Assert.IsFalse(planner.IsInClearArea(placement.Position + Vector2.right * 20f));
        }

        [Test]
        public void DecayRegistryMultipliesModifiers()
        {
            var a = new FixedModifier { Multiplier = 0.5f };
            var b = new FixedModifier { Multiplier = 0.4f };
            Assert.AreEqual(1f, DecayRateRegistry.Combined(Vector2.zero), 0.0001f);
            DecayRateRegistry.Register(a);
            DecayRateRegistry.Register(b);
            Assert.AreEqual(0.2f, DecayRateRegistry.Combined(Vector2.zero), 0.0001f);
            Assert.AreEqual(1f, DecayRateRegistry.Combined(new Vector2(50f, 0f)), 0.0001f, "반경 밖 위치는 배율 없음");
            DecayRateRegistry.Unregister(a);
            DecayRateRegistry.Unregister(b);
            Assert.AreEqual(1f, DecayRateRegistry.Combined(Vector2.zero), 0.0001f);
        }

        [Test]
        public void DayNightProfileCoversWholeDayWithoutJumps()
        {
            var profile = ScriptableObject.CreateInstance<DayNightProfile>();
            profile.ApplyDefaults();

            var previous = profile.Evaluate(0f).Night01;
            for (var i = 1; i <= 1000; i++)
            {
                var sample = profile.Evaluate(i / 1000f);
                Assert.LessOrEqual(Mathf.Abs(sample.Night01 - previous), 0.1f, $"밤 진행도가 {i / 1000f}에서 튐");
                previous = sample.Night01;
            }
            Assert.AreEqual(DayPart.Dawn, profile.Evaluate(0.01f).Part);
            Assert.AreEqual(DayPart.Day, profile.Evaluate(profile.TimeOfDayAt(DayPart.Day, 0.5f)).Part);
            Assert.AreEqual(DayPart.Night, profile.Evaluate(profile.TimeOfDayAt(DayPart.Night, 0.5f)).Part);
            Assert.AreEqual(1f, profile.Evaluate(profile.TimeOfDayAt(DayPart.Night, 0.5f)).Night01, 0.0001f);
            Assert.AreEqual(0f, profile.Evaluate(profile.TimeOfDayAt(DayPart.Day, 0.5f)).Night01, 0.0001f);
        }
        [Test]
        public void DecorIsDeterministicAndClearOfStartAndObstacles()
        {
            var map = ScriptableObject.CreateInstance<MapGenSettings>();
            map.obstacles = new[] { new ObstacleDefinition { name = "A", weight = 1f, colliderSize = Vector2.one } };
            map.decor = new[]
            {
                new DecorDefinition { name = "W", weight = 2f, nightTint = Color.white },
                new DecorDefinition { name = "P", weight = 1f, nightTint = Color.white },
            };
            var generator = new ChunkGenerator(map, new SingleBiomeProvider(new DefaultBiome(map)));
            var total = 0;
            for (var y = -2; y <= 2; y++)
            for (var x = -2; x <= 2; x++)
            {
                var a = generator.Generate(new Vector2Int(x, y));
                var b = generator.Generate(new Vector2Int(x, y));
                Assert.AreEqual(a.Decor.Count, b.Decor.Count);
                for (var i = 0; i < a.Decor.Count; i++)
                {
                    Assert.AreEqual(a.Decor[i].Position, b.Decor[i].Position);
                    Assert.GreaterOrEqual((a.Decor[i].Position - map.startPoint).magnitude, map.startClearRadius - 0.001f);
                    foreach (var o in a.Obstacles)
                        Assert.GreaterOrEqual((a.Decor[i].Position - o.Position).magnitude, map.decorObstacleClearance - 0.001f);
                }
                total += a.Decor.Count;
            }
            Assert.Greater(total, 0, "장식이 하나도 생성되지 않음");
        }

        [Test]
        public void GrassFloorVariantAppears()
        {
            var map = ScriptableObject.CreateInstance<MapGenSettings>();
            map.grassStart = 0.5f;
            var generator = new ChunkGenerator(map, new SingleBiomeProvider(new DefaultBiome(map)));
            var found = false;
            foreach (var v in generator.Generate(Vector2Int.zero).FloorVariants) found |= v == 3;
            Assert.IsTrue(found);
        }
    }
}
