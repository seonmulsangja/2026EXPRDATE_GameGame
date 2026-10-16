using UnityEngine;

namespace ExpiryGame.MapGen
{
    // 청크 하나를 만들 때 규칙들이 공통으로 받는 입력
    public readonly struct ChunkInfo
    {
        public readonly Vector2Int Coord;
        public readonly int Size;
        public readonly int WorldSeed;

        public ChunkInfo(Vector2Int coord, int size, int worldSeed)
        {
            Coord = coord;
            Size = size;
            WorldSeed = worldSeed;
        }

        // 청크 왼쪽 아래 타일 좌표
        public Vector2Int Origin => Coord * Size;
        public int Seed => ChunkSeed.For(WorldSeed, Coord);
    }

    public readonly struct ObstaclePlacement
    {
        public readonly int TypeIndex;
        public readonly Vector2 Position;

        public ObstaclePlacement(int typeIndex, Vector2 position)
        {
            TypeIndex = typeIndex;
            Position = position;
        }
    }

    // 충돌 없는 바닥 장식 배치. TypeIndex는 MapGenSettings.decor 의 인덱스.
    public readonly struct DecorPlacement
    {
        public readonly int TypeIndex;
        public readonly Vector2 Position;

        public DecorPlacement(int typeIndex, Vector2 position)
        {
            TypeIndex = typeIndex;
            Position = position;
        }
    }

    // 생성 결과(순수 데이터). 타일맵/게임오브젝트 반영은 View가 한다.
    public sealed class ChunkData
    {
        public readonly Vector2Int Coord;
        public readonly Vector2Int Origin;
        public readonly int Size;
        // 타일 (x, y)의 바닥 변형 번호 = [y * Size + x]. 번호→타일 매핑은 MapGenSettings.floorTiles
        public readonly int[] FloorVariants;
        public readonly System.Collections.Generic.List<ObstaclePlacement> Obstacles =
            new System.Collections.Generic.List<ObstaclePlacement>();

        public readonly System.Collections.Generic.List<DecorPlacement> Decor =
            new System.Collections.Generic.List<DecorPlacement>();

        public ChunkData(ChunkInfo info)
        {
            Coord = info.Coord;
            Origin = info.Origin;
            Size = info.Size;
            FloorVariants = new int[info.Size * info.Size];
        }

        public Bounds WorldBounds => new Bounds(
            new Vector3(Origin.x + Size * 0.5f, Origin.y + Size * 0.5f, 0f),
            new Vector3(Size, Size, 0f));
    }
}
