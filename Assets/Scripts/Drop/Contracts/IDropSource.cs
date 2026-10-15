using System;
using UnityEngine;

namespace ExpiryGame.Drop
{
    // 음식을 떨굴 수 있는 쪽(적)이 구현하는 계약. 구현 후 OnEnable에서 DropSourceRegistry.Register(this),
    // OnDisable에서 Unregister(this)만 하면 DropSpawner가 알아서 구독한다.
    public interface IDropSource
    {
        DropTable Table { get; }
        // 0 = 약한 적, 1 = 강한 적. 강할수록 유통기한 짧은(강한) 음식 가중치가 올라간다 (계산은 DropTable 한 곳)
        float Threat01 { get; }
        Vector3 DropPosition { get; }

        event Action<IDropSource> Died;
    }
}
