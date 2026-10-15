using System.Collections.Generic;
using ExpiryGame.Drop;
using ExpiryGame.MapGen;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ExpiryGame.Tests
{
    public class DropMapTests
    {
        private static MapGenSettings NewMapSettings()
        {
            var settings = ScriptableObject.CreateInstance<MapGenSettings>();
            settings.obstacles = new[]
            {
                new ObstacleDefinition { name = "A", weight = 3f, colliderSize = Vector2.one },
                new ObstacleDefinition { name = "B", weight = 2f, colliderSize = Vector2.one },
                new ObstacleDefinition { name = "C", weight = 4f, colliderSize = Vector2.one },
            };
            return settings;
        }

        private static ChunkGenerator NewGenerator(MapGenSettings settings)
        {
            return new ChunkGenerator(settings, new SingleBiomeProvider(new DefaultBiome(settings)));
        }

        [Test]
        public void SameChunkIsAlwaysIdentical()
        {
            var generator = NewGenerator(NewMapSettings());
            var a = generator.Generate(new Vector2Int(3, -2));
            var b = generator.Generate(new Vector2Int(3, -2));

            CollectionAssert.AreEqual(a.FloorVariants, b.FloorVariants);
            Assert.AreEqual(a.Obstacles.Count, b.Obstacles.Count);
            for (var i = 0; i < a.Obstacles.Count; i++)
            {
                Assert.AreEqual(a.Obstacles[i].Position, b.Obstacles[i].Position);
                Assert.AreEqual(a.Obstacles[i].TypeIndex, b.Obstacles[i].TypeIndex);
            }
        }

        [Test]
        public void DifferentSeedsGiveDifferentFloors()
        {
            var settings = NewMapSettings();
            var generator = NewGenerator(settings);
            var before = new List<int>();
            for (var i = 0; i < 6; i++) before.AddRange(generator.Generate(new Vector2Int(i * 3, 5)).FloorVariants);
            settings.seed += 1;
            var after = new List<int>();
            for (var i = 0; i < 6; i++) after.AddRange(generator.Generate(new Vector2Int(i * 3, 5)).FloorVariants);
            CollectionAssert.AreNotEqual(before, after);
        }

        [Test]
        public void ObstaclesKeepMinimumSpacingEvenAcrossChunkBorders()
        {
            var settings = NewMapSettings();
            settings.obstacleAttemptsPerChunk = 400;
            settings.densityAtLow = 1f;
            settings.densityAtHigh = 1f;
            var generator = NewGenerator(settings);

            var all = new List<ObstaclePlacement>();
            for (var y = 4; y <= 5; y++)
            for (var x = 4; x <= 5; x++)
                all.AddRange(generator.Generate(new Vector2Int(x, y)).Obstacles);

            Assert.Greater(all.Count, 20);
            var spacingSqr = settings.minObstacleSpacing * settings.minObstacleSpacing;
            for (var i = 0; i < all.Count; i++)
            for (var j = i + 1; j < all.Count; j++)
                Assert.GreaterOrEqual((all[i].Position - all[j].Position).sqrMagnitude, spacingSqr - 0.0001f);
        }

        [Test]
        public void StartAreaStaysClear()
        {
            var settings = NewMapSettings();
            settings.obstacleAttemptsPerChunk = 400;
            settings.densityAtLow = 1f;
            settings.densityAtHigh = 1f;
            var generator = NewGenerator(settings);

            for (var y = -1; y <= 0; y++)
            for (var x = -1; x <= 0; x++)
                foreach (var placement in generator.Generate(new Vector2Int(x, y)).Obstacles)
                    Assert.GreaterOrEqual((placement.Position - settings.startPoint).magnitude, settings.startClearRadius);
        }

        [Test]
        public void StrongerEnemiesFavorShortShelfLifeFoods()
        {
            var fish = ScriptableObject.CreateInstance<FoodData>();
            fish.shelfLifeSeconds = 30f;
            var can = ScriptableObject.CreateInstance<FoodData>();
            can.shelfLifeSeconds = 300f;

            var table = ScriptableObject.CreateInstance<DropTable>();
            var so = new SerializedObject(table);
            so.FindProperty("dropChance").floatValue = 1f;
            so.FindProperty("minCount").intValue = 1;
            so.FindProperty("maxCount").intValue = 1;
            var entries = so.FindProperty("entries");
            entries.arraySize = 2;
            SetEntry(entries.GetArrayElementAtIndex(0), fish);
            SetEntry(entries.GetArrayElementAtIndex(1), can);
            so.ApplyModifiedPropertiesWithoutUndo();

            Assert.Greater(FishShare(table, fish, 1f), 0.8f);
            Assert.Less(FishShare(table, fish, 0f), 0.6f);
            Assert.Greater(FishShare(table, fish, 0f), 0.4f);
        }

        private static void SetEntry(SerializedProperty entry, FoodData food)
        {
            entry.FindPropertyRelative("food").objectReferenceValue = food;
            entry.FindPropertyRelative("weight").floatValue = 1f;
            entry.FindPropertyRelative("minShelfLife01").floatValue = 0.4f;
            entry.FindPropertyRelative("maxShelfLife01").floatValue = 1f;
        }

        private static float FishShare(DropTable table, FoodData fish, float threat01)
        {
            const int Rolls = 3000;
            var results = new List<DropResult>();
            var fishCount = 0;
            for (var i = 0; i < Rolls; i++)
            {
                results.Clear();
                table.Roll(threat01, results);
                foreach (var r in results)
                {
                    Assert.GreaterOrEqual(r.ShelfLife01, 0.4f);
                    Assert.LessOrEqual(r.ShelfLife01, 1f);
                    if (r.Food == fish) fishCount++;
                }
            }
            return fishCount / (float)Rolls;
        }
    }
}
