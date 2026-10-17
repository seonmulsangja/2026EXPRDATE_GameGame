using System.Collections.Generic;
using UnityEngine;

namespace ExpiryGame.MapGen
{
    // 가중치 + 밀도 노이즈 + 장식 간 최소 간격 + 장애물/시작 지점 비움. 같은 청크는 항상 같은 결과.
    // 랜드마크 주변 비움은 배치 결과를 월드에 올릴 때 IObstacleFilter가 맡는다 (ChunkStreamer).
    public class PerlinDecorRule : IDecorRule
    {
        private const int Salt = 0x0DEC;

        private readonly MapGenSettings settings;

        public PerlinDecorRule(MapGenSettings settings)
        {
            this.settings = settings;
        }

        public void Generate(in ChunkInfo chunk, IReadOnlyList<ObstaclePlacement> obstacles, List<DecorPlacement> output)
        {
            var definitions = settings.decor;
            var totalWeight = 0f;
            foreach (var d in definitions) totalWeight += Mathf.Max(0f, d.weight);
            if (totalWeight <= 0f) return;

            var origin = chunk.Origin;
            var rng = new ChunkRandom(ChunkSeed.For(chunk.WorldSeed, chunk.Coord, Salt));
            var spacingSqr = settings.minDecorSpacing * settings.minDecorSpacing;
            var obstacleSqr = settings.decorObstacleClearance * settings.decorObstacleClearance;
            var startSqr = settings.startClearRadius * settings.startClearRadius;

            for (var attempt = 0; attempt < settings.decorAttemptsPerChunk; attempt++)
            {
                var tx = rng.Range(origin.x, origin.x + chunk.Size);
                var ty = rng.Range(origin.y, origin.y + chunk.Size);
                var roll = rng.Value();
                var typeRoll = rng.Value() * totalWeight;

                var position = new Vector2(tx + 0.5f, ty + 0.5f);
                var density = Mathf.Lerp(settings.decorDensityAtLow, settings.decorDensityAtHigh,
                    PerlinSampler.Sample(tx, ty, settings.decorNoiseScale, chunk.WorldSeed, PerlinSampler.DecorChannel));

                if (roll > density) continue;
                if ((position - settings.startPoint).sqrMagnitude < startSqr) continue;
                if (NearObstacle(obstacles, position, obstacleSqr)) continue;
                if (NearDecor(output, position, spacingSqr)) continue;

                output.Add(new DecorPlacement(PickType(definitions, typeRoll), position));
            }
        }

        private static bool NearObstacle(IReadOnlyList<ObstaclePlacement> obstacles, Vector2 position, float sqr)
        {
            for (var i = 0; i < obstacles.Count; i++)
                if ((obstacles[i].Position - position).sqrMagnitude < sqr) return true;
            return false;
        }

        private static bool NearDecor(List<DecorPlacement> placed, Vector2 position, float sqr)
        {
            foreach (var other in placed)
                if ((other.Position - position).sqrMagnitude < sqr) return true;
            return false;
        }

        private static int PickType(DecorDefinition[] definitions, float roll)
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
