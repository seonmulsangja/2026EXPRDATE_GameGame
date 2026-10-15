using UnityEngine;

namespace ExpiryGame.MapGen
{
    // 청크 좌표 + 월드 seed → 청크 seed. 플랫폼/버전과 무관하게 같은 값이 나오도록 정수 해시만 쓴다.
    public static class ChunkSeed
    {
        public static int For(int worldSeed, Vector2Int coord, int salt = 0)
        {
            unchecked
            {
                var h = (uint)worldSeed;
                h = Mix(h ^ (uint)coord.x * 0x9E3779B1u);
                h = Mix(h ^ (uint)coord.y * 0x85EBCA6Bu);
                h = Mix(h ^ (uint)salt * 0xC2B2AE35u);
                return (int)h;
            }
        }

        private static uint Mix(uint h)
        {
            unchecked
            {
                h ^= h >> 16;
                h *= 0x7FEB352Du;
                h ^= h >> 15;
                h *= 0x846CA68Bu;
                h ^= h >> 16;
                return h;
            }
        }
    }

    // 결정적 난수 (xorshift32). struct라서 복사하면 같은 지점부터 독립적으로 이어서 뽑는다.
    public struct ChunkRandom
    {
        private uint state;

        public ChunkRandom(int seed)
        {
            state = seed == 0 ? 0x6D2B79F5u : (uint)seed;
        }

        public float Value()
        {
            return (Next() >> 8) * (1f / 16777216f);
        }

        public float Range(float min, float max)
        {
            return min + (max - min) * Value();
        }

        // [minInclusive, maxExclusive)
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            return minInclusive + (int)(Value() * (maxExclusive - minInclusive));
        }

        // 현재 상태에서 갈라져 나온 독립 난수. salt는 용도별로 다르게 (예: 적 스폰 1, 상자 2)
        public ChunkRandom Fork(int salt)
        {
            return new ChunkRandom(ChunkSeed.For((int)state, Vector2Int.zero, salt));
        }

        private uint Next()
        {
            var x = state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            state = x;
            return x;
        }
    }

    public static class ChunkMath
    {
        public static Vector2Int WorldToChunk(Vector2 world, int chunkSize)
        {
            return new Vector2Int(Mathf.FloorToInt(world.x / chunkSize), Mathf.FloorToInt(world.y / chunkSize));
        }

        // 체비쇼프 거리 (5x5 = 반경 2)
        public static int ChebyshevDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }
    }
}
