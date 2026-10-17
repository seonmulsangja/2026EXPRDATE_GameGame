using System;
using System.Collections.Generic;

namespace ExpiryGame.Flow
{
    // 후보 풀에서 중복 없이 count개를 무작위로 뽑는다 (부분 피셔-예이츠). 난수는 밖에서 받아서 테스트가 고정할 수 있다.
    public static class CandidateDrawer
    {
        // range(min, maxExclusive): [min, maxExclusive) 정수 난수
        public static List<T> Draw<T>(IReadOnlyList<T> pool, int count, Func<int, int, int> range)
        {
            var copy = new List<T>(pool);
            var take = Math.Max(0, Math.Min(count, copy.Count));
            for (var i = 0; i < take; i++)
            {
                var j = range(i, copy.Count);
                (copy[i], copy[j]) = (copy[j], copy[i]);
            }
            return copy.GetRange(0, take);
        }
    }
}
