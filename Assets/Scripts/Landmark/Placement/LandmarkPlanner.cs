using System.Collections.Generic;
using UnityEngine;
using ExpiryGame.MapGen;

namespace ExpiryGame.Landmark
{
    public readonly struct LandmarkPlacement
    {
        public readonly Vector2Int Region;
        public readonly LandmarkData Data;
        // 바닥 중앙 (프리팹 피벗)
        public readonly Vector2 Position;

        public LandmarkPlacement(Vector2Int region, LandmarkData data, Vector2 position)
        {
            Region = region;
            Data = data;
            Position = position;
        }

        // 장애물을 비울 영역 (크기 + 여유)
        public Rect ClearRect(float padding)
        {
            return new Rect(Position.x - Data.size.x * 0.5f - padding, Position.y - padding,
                Data.size.x + padding * 2f, Data.size.y + padding * 2f);
        }
    }

    // 월드를 지역(chunksPerRegion x chunksPerRegion 청크)으로 나눠 seed 기반으로 랜드마크 0~1개를 결정한다.
    // 순수 계산이라 같은 seed면 항상 같은 결과이고, 로드/언로드와 무관하게 어느 지역이든 물어볼 수 있다.
    public class LandmarkPlanner
    {
        private readonly LandmarkSettings settings;
        private readonly Dictionary<Vector2Int, LandmarkPlacement?> cache = new Dictionary<Vector2Int, LandmarkPlacement?>();
        private readonly Vector2 startPosition;
        private readonly Vector2Int startRegion;
        private readonly float totalWeight;

        public LandmarkPlanner(LandmarkSettings settings)
        {
            this.settings = settings;
            foreach (var data in settings.landmarks)
                if (data != null) totalWeight += data.weight;

            startRegion = RegionOf(settings.map.startPoint + settings.startOffset);
            startPosition = ClampToRegion(startRegion, settings.map.startPoint + settings.startOffset);
        }

        public float RegionSizeTiles => settings.chunksPerRegion * settings.map.chunkSize;

        public Vector2Int RegionOf(Vector2 world)
        {
            var size = RegionSizeTiles;
            return new Vector2Int(Mathf.FloorToInt(world.x / size), Mathf.FloorToInt(world.y / size));
        }

        public bool TryGet(Vector2Int region, out LandmarkPlacement placement)
        {
            if (!cache.TryGetValue(region, out var cached))
            {
                cached = Compute(region);
                cache.Add(region, cached);
            }
            placement = cached ?? default;
            return cached.HasValue;
        }

        // 중심 지역 기준 ring 반경(체비쇼프)의 모든 랜드마크
        public void CollectAround(Vector2 world, int ring, List<LandmarkPlacement> results)
        {
            var center = RegionOf(world);
            for (var dy = -ring; dy <= ring; dy++)
            for (var dx = -ring; dx <= ring; dx++)
                if (TryGet(center + new Vector2Int(dx, dy), out var placement)) results.Add(placement);
        }

        // 이 위치가 어떤 랜드마크의 비움 영역 안인가 (마진 >= 비움 영역이라 자기 지역만 보면 된다)
        public bool IsInClearArea(Vector2 world)
        {
            return TryGet(RegionOf(world), out var placement) &&
                   placement.ClearRect(settings.obstacleClearPadding).Contains(world);
        }

        private LandmarkPlacement? Compute(Vector2Int region)
        {
            if (settings.startLandmark != null && region == startRegion)
                return new LandmarkPlacement(region, settings.startLandmark, startPosition);

            if (totalWeight <= 0f) return null;
            var rng = new ChunkRandom(ChunkSeed.For(settings.map.seed, region, settings.salt));
            if (rng.Value() > settings.spawnChance) return null;

            var data = Pick(rng.Value() * totalWeight);
            if (data == null) return null;
            var size = RegionSizeTiles;
            var margin = Mathf.Min(settings.regionMargin, size * 0.5f);
            var origin = (Vector2)region * size;
            var position = new Vector2(origin.x + rng.Range(margin, size - margin), origin.y + rng.Range(margin, size - margin));
            return new LandmarkPlacement(region, data, position);
        }

        private LandmarkData Pick(float roll)
        {
            LandmarkData last = null;
            foreach (var data in settings.landmarks)
            {
                if (data == null || data.weight <= 0f) continue;
                last = data;
                roll -= data.weight;
                if (roll <= 0f) return data;
            }
            return last;
        }

        private Vector2 ClampToRegion(Vector2Int region, Vector2 position)
        {
            var size = RegionSizeTiles;
            var margin = Mathf.Min(settings.regionMargin, size * 0.5f);
            var origin = (Vector2)region * size;
            return new Vector2(Mathf.Clamp(position.x, origin.x + margin, origin.x + size - margin),
                Mathf.Clamp(position.y, origin.y + margin, origin.y + size - margin));
        }
    }
}
