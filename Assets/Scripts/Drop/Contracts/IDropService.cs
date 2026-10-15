using System;
using UnityEngine;

namespace ExpiryGame.Drop
{
    public interface IDropService
    {
        // 사망 이벤트를 거치지 않고 직접 떨구고 싶을 때 (상자 등)
        void Drop(DropTable table, float threat01, Vector3 position);

        event Action<FoodItem, Vector3> FoodDropped;
        event Action<FoodItem> FoodCollected;
    }
}
