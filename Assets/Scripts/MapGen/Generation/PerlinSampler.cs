using UnityEngine;

namespace ExpiryGame.MapGen
{
    // seed + 채널별로 노이즈 오프셋을 달리해, 바닥/장애물 노이즈가 서로 같은 무늬가 되지 않게 한다.
    // 좌표는 큰 양수 오프셋을 더해 Mathf.PerlinNoise의 음수 좌표 영역을 피한다.
    public static class PerlinSampler
    {
        public const int FloorChannel = 1;
        public const int ObstacleChannel = 2;

        private const float PositiveShift = 50000f;

        public static float Sample(float tileX, float tileY, float scale, int worldSeed, int channel)
        {
            var h = ChunkSeed.For(worldSeed, Vector2Int.zero, channel);
            var offsetX = (h & 0xFFFF) * 0.1f;
            var offsetY = ((h >> 16) & 0xFFFF) * 0.1f;
            return Mathf.PerlinNoise((tileX + PositiveShift + offsetX) * scale, (tileY + PositiveShift + offsetY) * scale);
        }
    }
}
