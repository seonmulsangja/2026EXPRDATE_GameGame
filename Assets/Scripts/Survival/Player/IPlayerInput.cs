using System;
using UnityEngine;

namespace ExpiryGame
{
    public interface IPlayerInput
    {
        Vector2 Move { get; }
        // E를 누르고 있는 동안 true (길게 누르기 시간은 FoodConsumer가 EatSettings로 직접 잰다)
        bool EatHeld { get; }

        event Action EatPerformed;
        event Action AttackPerformed;
        event Action NextFoodPerformed;
    }
}
