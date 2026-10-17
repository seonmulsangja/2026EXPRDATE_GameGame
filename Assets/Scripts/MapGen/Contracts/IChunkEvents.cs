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

        // 청크 좌표의 월드 경계 (OnChunkLoaded로 받은 Bounds와 같은 값). 구독 시점 이전에 로드된 청크를 소급 처리할 때 쓴다
        Bounds BoundsOf(Vector2Int coord);
    }

    public static class ChunkEventsExtensions
    {
        // 구독하기 "전에" 이미 로드된 청크에 대해 onLoaded를 한 번씩 부른다. 이벤트만 구독하면 시작 지역이 영구히 비는 문제를 막는다.
        // 호출 중에 컬렉션이 바뀌어도 안전하도록 복사본을 순회한다.
        public static int ForEachLoaded(this IChunkEvents events, Action<Vector2Int, Bounds> onLoaded)
        {
            var snapshot = new List<Vector2Int>(events.LoadedChunks);
            foreach (var coord in snapshot) onLoaded(coord, events.BoundsOf(coord));
            return snapshot.Count;
        }
    }
}
