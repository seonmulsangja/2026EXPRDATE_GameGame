using UnityEngine;
using UnityEngine.Tilemaps;

namespace ExpiryGame.MapGen
{
    // 청크 단위로 SetTilesBlock. 언로드는 같은 영역을 null로 덮는다.
    // night Tilemap이 있으면 같은 좌표에 night 타일을 동시에 채운다 (낮밤 크로스페이드용).
    public class ChunkTilemapView
    {
        private readonly Tilemap tilemap;
        private readonly Tilemap nightTilemap;
        private readonly TileBase[] floorTiles;
        private readonly TileBase[] nightTiles;
        private readonly TileBase[] nightBuffer;
        private readonly TileBase[] tileBuffer;
        private readonly TileBase[] emptyBuffer;
        private readonly int size;

        public ChunkTilemapView(Tilemap tilemap, MapGenSettings settings, Tilemap nightTilemap = null)
        {
            this.tilemap = tilemap;
            this.nightTilemap = nightTilemap;
            floorTiles = settings.floorTiles;
            nightTiles = settings.nightFloorTiles;
            size = settings.chunkSize;
            tileBuffer = new TileBase[size * size];
            nightBuffer = new TileBase[size * size];
            emptyBuffer = new TileBase[size * size];
        }

        public void Show(ChunkData chunk)
        {
            var block = BlockFor(chunk.Origin);
            for (var i = 0; i < chunk.FloorVariants.Length; i++)
                tileBuffer[i] = floorTiles[Mathf.Clamp(chunk.FloorVariants[i], 0, floorTiles.Length - 1)];
            tilemap.SetTilesBlock(block, tileBuffer);

            if (nightTilemap == null || nightTiles == null || nightTiles.Length == 0) return;
            for (var i = 0; i < chunk.FloorVariants.Length; i++)
                nightBuffer[i] = nightTiles[Mathf.Clamp(chunk.FloorVariants[i], 0, nightTiles.Length - 1)];
            nightTilemap.SetTilesBlock(block, nightBuffer);
        }

        public void Hide(Vector2Int chunkCoord)
        {
            var block = BlockFor(chunkCoord * size);
            tilemap.SetTilesBlock(block, emptyBuffer);
            if (nightTilemap != null) nightTilemap.SetTilesBlock(block, emptyBuffer);
        }

        private BoundsInt BlockFor(Vector2Int origin)
        {
            return new BoundsInt(origin.x, origin.y, 0, size, size, 1);
        }
    }
}
