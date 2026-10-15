using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExpiryGame.Drop
{
    // IDropSource 등록소. 적이 DropSpawner 참조를 몰라도 되게 하는 얇은 연결점.
    public static class DropSourceRegistry
    {
        private static readonly List<IDropSource> sources = new List<IDropSource>();

        public static IReadOnlyList<IDropSource> Sources => sources;

        public static event Action<IDropSource> Registered;
        public static event Action<IDropSource> Unregistered;

        public static void Register(IDropSource source)
        {
            if (source == null || sources.Contains(source)) return;
            sources.Add(source);
            Registered?.Invoke(source);
        }

        public static void Unregister(IDropSource source)
        {
            if (!sources.Remove(source)) return;
            Unregistered?.Invoke(source);
        }

        // 도메인 리로드를 끈 에디터 설정에서도 이전 플레이의 잔재가 남지 않게 한다
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            sources.Clear();
            Registered = null;
            Unregistered = null;
        }
    }
}
