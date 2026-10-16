using UnityEngine;

namespace ExpiryGame.MapGen
{
    // 기본 바이옴: 퍼린 노이즈 바닥 + 퍼린 밀도 장애물
    public class DefaultBiome : IBiome
    {
        public string Name => "Default";
        public IFloorRule Floor { get; }
        public IObstacleRule Obstacles { get; }
        public IDecorRule Decor { get; }

        public DefaultBiome(MapGenSettings settings)
        {
            Floor = new PerlinFloorRule(settings);
            Obstacles = new PerlinObstacleRule(settings);
            Decor = new PerlinDecorRule(settings);
        }
    }

    public class SingleBiomeProvider : IBiomeProvider
    {
        private readonly IBiome biome;

        public SingleBiomeProvider(IBiome biome)
        {
            this.biome = biome;
        }

        public IBiome GetBiome(Vector2Int chunkCoord)
        {
            return biome;
        }
    }
}
