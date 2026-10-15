using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExpiryGame.MapGen
{
    // 팀원용: 적 스폰 등이 청크 로드/언로드를 구독하는 계약.
    // OnChunkLoaded의 rng는 값 복사로 전달되어 구독자마다 독립적이다. 같은 청크는 항상 같은 난수열이므로
    // 서로 다른 시스템이 같은 값을 뽑지 않게 rng.Fork(고유 salt)를 쓰는 것을 권장한다.
    public interface IChunkEvents
    {
        event Action<Vector2Int, Bounds, ChunkRandom> OnChunkLoaded;
        event Action<Vector2Int> OnChunkUnloaded;

        Vector2Int CurrentChunk { get; }
        IReadOnlyCollection<Vector2Int> LoadedChunks { get; }
    }
}
