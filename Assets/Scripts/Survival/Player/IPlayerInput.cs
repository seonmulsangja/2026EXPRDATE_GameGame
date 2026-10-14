using System;
using UnityEngine;

namespace ExpiryGame
{
    public interface IPlayerInput
    {
        Vector2 Move { get; }

        event Action EatPerformed;
        event Action AttackPerformed;
        event Action NextFoodPerformed;
    }
}
