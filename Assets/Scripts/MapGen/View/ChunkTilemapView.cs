using UnityEngine;
using UnityEngine.Tilemaps;

namespace ExpiryGame.MapGen
{
    // 단일 Tilemap에 청크 단위로 SetTilesBlock. 언로드는 같은 영역을 null로 덮는다.
    public class ChunkTilemapView
    {
        private readonly Tilemap tilemap;
        private readonly TileBase[] floorTiles;
        private readonly TileBase[] tileBuffer;
        private readonly TileBase[] emptyBuffer;
        private readonly int size;

        public ChunkTilemapView(Tilemap tilemap, MapGenSettings settings)
        {
            this.tilemap = tilemap;
            floorTiles = settings.floorTiles;
            size = settings.chunkSize;
            tileBuffer = new TileBase[size * size];
            emptyBuffer = new TileBase[size * size];
        }

        public void Show(ChunkData chunk)
        {
            for (var i = 0; i < chunk.FloorVariants.Length; i++)
                tileBuffer[i] = floorTiles[Mathf.Clamp(chunk.FloorVariants[i], 0, floorTiles.Length - 1)];
            tilemap.SetTilesBlock(BlockFor(chunk.Origin), tileBuffer);
        }

        public void Hide(Vector2Int chunkCoord)
        {
            tilemap.SetTilesBlock(BlockFor(chunkCoord * size), emptyBuffer);
        }

        private BoundsInt BlockFor(Vector2Int origin)
        {
            return new BoundsInt(origin.x, origin.y, 0, size, size, 1);
        }
    }
}
