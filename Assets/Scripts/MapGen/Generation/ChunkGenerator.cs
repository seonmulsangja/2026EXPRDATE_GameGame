using UnityEngine;

namespace ExpiryGame.MapGen
{
    // 청크 좌표 → ChunkData (순수 계산). 같은 seed/좌표/규칙이면 항상 같은 결과라서 저장이 필요 없다.
    public class ChunkGenerator
    {
        private readonly MapGenSettings settings;
        private readonly IBiomeProvider biomes;

        public ChunkGenerator(MapGenSettings settings, IBiomeProvider biomes)
        {
            this.settings = settings;
            this.biomes = biomes;
        }

        public ChunkData Generate(Vector2Int coord)
        {
            var info = new ChunkInfo(coord, settings.chunkSize, settings.seed);
            var data = new ChunkData(info);
            var biome = biomes.GetBiome(coord);
            biome.Floor.Fill(info, data.FloorVariants);
            biome.Obstacles.Generate(info, data.Obstacles);
            biome.Decor?.Generate(info, data.Obstacles, data.Decor);
            return data;
        }
    }
}
