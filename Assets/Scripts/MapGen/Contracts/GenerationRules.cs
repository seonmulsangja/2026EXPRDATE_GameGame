using System.Collections.Generic;
using UnityEngine;

namespace ExpiryGame.MapGen
{
    // 바닥 규칙: 청크의 모든 타일에 변형 번호를 채운다
    public interface IFloorRule
    {
        void Fill(in ChunkInfo chunk, int[] floorVariants);
    }

    // 장애물 규칙: 청크 안의 장애물 배치를 만든다
    public interface IObstacleRule
    {
        void Generate(in ChunkInfo chunk, List<ObstaclePlacement> output);
    }

    // 바이옴 = 바닥 규칙 + 장애물 규칙 묶음. 새 바이옴은 이 인터페이스만 구현하면 된다.
    public interface IBiome
    {
        string Name { get; }
        IFloorRule Floor { get; }
        IObstacleRule Obstacles { get; }
    }

    // 청크 좌표 → 바이옴. 지금은 하나뿐이지만, 나중에 노이즈로 바이옴을 나누려면 이것만 교체한다.
    public interface IBiomeProvider
    {
        IBiome GetBiome(Vector2Int chunkCoord);
    }
}
