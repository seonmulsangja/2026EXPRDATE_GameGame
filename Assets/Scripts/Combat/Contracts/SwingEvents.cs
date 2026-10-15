using UnityEngine;

namespace ExpiryGame.Combat
{
    public readonly struct SwingInfo
    {
        public readonly FoodItem Item;
        public readonly Vector2 Origin;
        public readonly Vector2 AimDirection;
        public readonly float AimAngle;
        public readonly int Sign; // +1 반시계, -1 시계

        public SwingInfo(FoodItem item, Vector2 origin, Vector2 aimDirection, int sign)
        {
            Item = item;
            Origin = origin;
            AimDirection = aimDirection;
            AimAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            Sign = sign;
        }
    }

    public readonly struct HitInfo
    {
        public readonly GameObject Target;
        public readonly Vector2 Point;
        public readonly Vector2 Direction; // 정규화. 넉백/스파크 방향
        public readonly float Damage;
        public readonly FoodItem Weapon;

        public HitInfo(GameObject target, Vector2 point, Vector2 direction, float damage, FoodItem weapon)
        {
            Target = target;
            Point = point;
            Direction = direction;
            Damage = damage;
            Weapon = weapon;
        }
    }
}
