using System.Collections.Generic;
using UnityEngine;

namespace ExpiryGame.MapGen
{
    // 가중치 + 밀도 노이즈 + 최소 간격 + 시작 지점 비움.
    // 청크 가장자리에서 minSpacing/2 만큼 안쪽에만 배치하므로, 이웃 청크 정보 없이도 경계에서 간격이 지켜진다.
    public class PerlinObstacleRule : IObstacleRule
    {
        private const int Salt = 0x0B57;

        private readonly MapGenSettings settings;

        public PerlinObstacleRule(MapGenSettings settings)
        {
            this.settings = settings;
        }

        public void Generate(in ChunkInfo chunk, List<ObstaclePlacement> output)
        {
            var definitions = settings.obstacles;
            var totalWeight = TotalWeight(definitions);
            if (totalWeight <= 0f) return;

            var margin = Mathf.Min(Mathf.CeilToInt(settings.minObstacleSpacing * 0.5f), chunk.Size / 2 - 1);
            var origin = chunk.Origin;
            var rng = new ChunkRandom(ChunkSeed.For(chunk.WorldSeed, chunk.Coord, Salt));
            var spacingSqr = settings.minObstacleSpacing * settings.minObstacleSpacing;
            var clearSqr = settings.startClearRadius * settings.startClearRadius;

            for (var attempt = 0; attempt < settings.obstacleAttemptsPerChunk; attempt++)
            {
                var tx = rng.Range(origin.x + margin, origin.x + chunk.Size - margin);
                var ty = rng.Range(origin.y + margin, origin.y + chunk.Size - margin);
                var roll = rng.Value();
                var typeRoll = rng.Value() * totalWeight;

                var position = new Vector2(tx + 0.5f, ty + 0.5f);
                var density = Mathf.Lerp(settings.densityAtLow, settings.densityAtHigh,
                    PerlinSampler.Sample(tx, ty, settings.obstacleNoiseScale, chunk.WorldSeed, PerlinSampler.ObstacleChannel));

                if (roll > density) continue;
                if ((position - settings.startPoint).sqrMagnitude < clearSqr) continue;
                if (TooClose(output, position, spacingSqr)) continue;

                output.Add(new ObstaclePlacement(PickType(definitions, typeRoll), position));
            }
        }

        private static bool TooClose(List<ObstaclePlacement> placed, Vector2 position, float spacingSqr)
        {
            foreach (var other in placed)
                if ((other.Position - position).sqrMagnitude < spacingSqr) return true;
            return false;
        }

        private static float TotalWeight(ObstacleDefinition[] definitions)
        {
            var total = 0f;
            foreach (var d in definitions) total += Mathf.Max(0f, d.weight);
            return total;
        }

        private static int PickType(ObstacleDefinition[] definitions, float roll)
        {
            for (var i = 0; i < definitions.Length; i++)
            {
                roll -= Mathf.Max(0f, definitions[i].weight);
                if (roll <= 0f) return i;
            }
            return definitions.Length - 1;
        }
    }
}
