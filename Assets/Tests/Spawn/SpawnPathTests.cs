using System;
using System.Collections.Generic;
using ExpiryGame.Flow;
using ExpiryGame.Landmark;
using ExpiryGame.MapGen;
using NUnit.Framework;
using UnityEngine;

namespace ExpiryGame.Tests
{
    // 적 스폰 경로 회귀 테스트: 시작 청크 소급 처리 / 난이도 0 방어 / 안전지대 판정 / 진단 카운터
    public class SpawnPathTests
    {
        private sealed class FakeChunkEvents : IChunkEvents
        {
            public readonly List<Vector2Int> Loaded = new List<Vector2Int>();
            public int Size = 16;
            public event Action<Vector2Int, Bounds, ChunkRandom> OnChunkLoaded { add { } remove { } }
            public event Action<Vector2Int> OnChunkUnloaded { add { } remove { } }
            public Vector2Int CurrentChunk => Vector2Int.zero;
            public IReadOnlyCollection<Vector2Int> LoadedChunks => Loaded;

            public Bounds BoundsOf(Vector2Int coord) =>
                new Bounds(new Vector3(coord.x * Size + Size * 0.5f, coord.y * Size + Size * 0.5f, 0f), new Vector3(Size, Size, 0f));
        }

        private readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in created) if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
            created.Clear();
        }

        private T Make<T>() where T : ScriptableObject
        {
            var obj = ScriptableObject.CreateInstance<T>();
            created.Add(obj);
            return obj;
        }

        // 시작 청크 소급 처리

        [Test]
        public void ForEachLoaded_ReplaysChunksLoadedBeforeSubscribing()
        {
            var events = new FakeChunkEvents();
            events.Loaded.Add(new Vector2Int(0, 0));
            events.Loaded.Add(new Vector2Int(-1, 2));

            var seen = new Dictionary<Vector2Int, Bounds>();
            var count = events.ForEachLoaded((coord, bounds) => seen[coord] = bounds);

            Assert.AreEqual(2, count);
            Assert.AreEqual(new Vector3(8f, 8f, 0f), seen[new Vector2Int(0, 0)].center, "시작 청크가 소급되어야 한다");
            Assert.AreEqual(new Vector3(-8f, 40f, 0f), seen[new Vector2Int(-1, 2)].center);
        }

        [Test]
        public void ForEachLoaded_SurvivesCollectionChangingInsideCallback()
        {
            var events = new FakeChunkEvents();
            events.Loaded.Add(Vector2Int.zero);
            Assert.DoesNotThrow(() => events.ForEachLoaded((coord, bounds) => events.Loaded.Add(coord + Vector2Int.right)));
        }

        [Test]
        public void ForEachLoaded_NothingLoaded_DoesNotCall()
        {
            var called = false;
            Assert.AreEqual(0, new FakeChunkEvents().ForEachLoaded((c, b) => called = true));
            Assert.IsFalse(called);
        }

        // 난이도 0 방어

        [Test]
        public void DifficultyProfile_EmptyCurves_AreRefilledSoMultiplierIsNeverZero()
        {
            var profile = Make<DifficultyProfile>();
            profile.spawn = new AnimationCurve();
            profile.health = null;
            Assert.AreEqual(0f, profile.spawn.Evaluate(0f), "전제: 빈 곡선은 0을 돌려준다");

            Assert.IsTrue(profile.EnsureCurves());
            Assert.Greater(profile.spawn.Evaluate(0f), 0f);
            Assert.Greater(profile.health.Evaluate(0f), 0f);
            Assert.IsFalse(profile.EnsureCurves(), "정상 곡선은 건드리지 않는다");
        }

        [Test]
        public void DifficultyProfile_HasPositiveSpawnFloor()
        {
            Assert.Greater(Make<DifficultyProfile>().minSpawnMultiplier, 0f);
        }

        [Test]
        public void DifficultyProfile_DefaultSpawnIsPositiveEveryDay()
        {
            var profile = Make<DifficultyProfile>();
            for (var day = 0f; day < 8f; day += 0.25f) Assert.Greater(profile.spawn.Evaluate(day), 0f, $"day {day}");
        }

        [Test]
        public void SpawnMath_FirstDayStillTargetsEnemies()
        {
            var perChunk = 1.2f * 0.35f; // enemiesPerChunk x 1일차 배율
            Assert.AreEqual(1, SpawnMath.ChunkTarget(perChunk, 0.0f), "굴림이 낮으면 1마리");
            Assert.AreEqual(0, SpawnMath.ChunkTarget(perChunk, 0.99f));
            Assert.AreEqual(0, SpawnMath.ChunkTarget(0f, 0f), "배율 0이면 절대 안 나온다 (그래서 막는다)");
            Assert.AreEqual(3, SpawnMath.ChunkTarget(2.5f, 0.1f));
        }

        [Test]
        public void SpawnMath_CapRespectsHardCapAndMinimumScale()
        {
            Assert.AreEqual(12, SpawnMath.Cap(24, 60, 0.35f), "배율이 낮아도 절반 밑으로는 안 내려간다");
            Assert.AreEqual(60, SpawnMath.Cap(24, 60, 9f));
        }

        // 안전지대 판정

        private LandmarkInfo Campfire(Vector2 position, SafeZoneProfile safe)
        {
            var data = Make<LandmarkData>();
            data.kind = LandmarkKind.Campfire;
            data.safeZone = safe;
            return new LandmarkInfo(data, position, 0f);
        }

        [Test]
        public void SafeZone_DefaultRadiusDoesNotForbidTheWholeStartChunk()
        {
            var safe = Make<SafeZoneProfile>();
            var chunk = new Bounds(new Vector3(8f, 8f, 0f), new Vector3(16f, 16f, 0f));
            var set = SafeZoneSet.From(new[] { Campfire(chunk.center, safe) });

            Assert.IsTrue(set.Contains(chunk.center));
            var free = 0;
            const int steps = 16;
            for (var y = 0; y < steps; y++)
            for (var x = 0; x < steps; x++)
            {
                var p = new Vector2(chunk.min.x + (x + 0.5f), chunk.min.y + (y + 0.5f));
                if (!set.Contains(p)) free++;
            }
            Assert.Greater(free, steps * steps / 2, "모닥불이 청크 한가운데 있어도 절반 이상은 스폰 가능해야 한다");
        }

        [Test]
        public void SafeZone_IsOutsideAtCampfireRadius_AndUsesWorldPositions()
        {
            var safe = Make<SafeZoneProfile>();
            safe.radius = 4.5f;
            var set = SafeZoneSet.From(new[] { Campfire(new Vector2(100f, -40f), safe) });

            Assert.IsTrue(set.Contains(new Vector2(104f, -40f)));
            Assert.IsFalse(set.Contains(new Vector2(104.6f, -40f)), "반경 밖은 스폰 가능 (판정 방향이 뒤집히지 않았다)");
            Assert.IsFalse(set.Contains(new Vector2(0f, 0f)), "월드 원점은 영향 없음 (좌표계 혼동 없음)");
        }

        // 진단 카운터

        [Test]
        public void Diagnostics_CountsBlocksPerReason_AndRemembersLastResult()
        {
            var d = new SpawnDiagnostics();
            d.RecordBlocked(SpawnBlockReason.NotPlaying, "Title");
            d.RecordBlocked(SpawnBlockReason.NotPlaying);
            d.RecordBlocked(SpawnBlockReason.SafeZone);
            Assert.AreEqual(2, d.Blocked(SpawnBlockReason.NotPlaying));
            Assert.AreEqual(1, d.Blocked(SpawnBlockReason.SafeZone));
            Assert.AreEqual(0, d.Blocked(SpawnBlockReason.Cap));
            Assert.AreEqual(3, d.TotalBlocked);
            StringAssert.Contains("안전지대", d.LastResult);

            d.RecordSpawn("청크");
            Assert.AreEqual(1, d.Spawned);
            StringAssert.Contains("성공", d.LastResult);

            d.Reset();
            Assert.AreEqual(0, d.TotalBlocked);
            Assert.AreEqual(0, d.Spawned);
        }

        [Test]
        public void Diagnostics_LabelsEveryReason()
        {
            foreach (SpawnBlockReason reason in Enum.GetValues(typeof(SpawnBlockReason)))
                Assert.IsNotEmpty(SpawnDiagnostics.Label(reason), reason.ToString());
        }
    }
}
