using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExpiryGame.Flow
{
    // 처치 통계/BGM 위험도가 읽는 "살아 있는 적" 계약. 팀원 적 코드는 어댑터가 이걸 구현한다.
    public interface IEnemyActor
    {
        Vector3 Position { get; }
        // 0 = 약한 적, 1 = 강한 적
        float Threat01 { get; }
        // 플레이어에게 죽었을 때만 발행 (청크 언로드로 사라지는 건 발행하지 않는다)
        event Action<IEnemyActor> Killed;
    }

    // DropSourceRegistry와 같은 방식의 얇은 등록소. 어댑터가 OnEnable/OnDisable에서 등록/해제한다.
    public static class EnemyRegistry
    {
        private static readonly List<IEnemyActor> enemies = new List<IEnemyActor>();

        public static IReadOnlyList<IEnemyActor> Enemies => enemies;

        public static event Action<IEnemyActor> Registered;
        public static event Action<IEnemyActor> Unregistered;

        public static void Register(IEnemyActor enemy)
        {
            if (enemy == null || enemies.Contains(enemy)) return;
            enemies.Add(enemy);
            Registered?.Invoke(enemy);
        }

        public static void Unregister(IEnemyActor enemy)
        {
            if (!enemies.Remove(enemy)) return;
            Unregistered?.Invoke(enemy);
        }

        public static int CountWithin(Vector2 center, float radius)
        {
            var sqr = radius * radius;
            var count = 0;
            for (var i = 0; i < enemies.Count; i++)
                if (((Vector2)enemies[i].Position - center).sqrMagnitude <= sqr) count++;
            return count;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            enemies.Clear();
            Registered = null;
            Unregistered = null;
        }
    }
}
