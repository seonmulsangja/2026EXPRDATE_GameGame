namespace ExpiryGame.MapGen
{
    // 바닥 변형을 노이즈 값의 구간으로 나눈다: 0=floor_a, 1=floor_b, 2=floor_c
    public class PerlinFloorRule : IFloorRule
    {
        private readonly MapGenSettings settings;

        public PerlinFloorRule(MapGenSettings settings)
        {
            this.settings = settings;
        }

        public void Fill(in ChunkInfo chunk, int[] floorVariants)
        {
            var origin = chunk.Origin;
            for (var y = 0; y < chunk.Size; y++)
            for (var x = 0; x < chunk.Size; x++)
            {
                var n = PerlinSampler.Sample(origin.x + x, origin.y + y, settings.floorNoiseScale,
                    chunk.WorldSeed, PerlinSampler.FloorChannel);
                floorVariants[y * chunk.Size + x] = n < settings.floorBStart ? 0 : n < settings.floorCStart ? 1 : 2;
            }
        }
    }
}
