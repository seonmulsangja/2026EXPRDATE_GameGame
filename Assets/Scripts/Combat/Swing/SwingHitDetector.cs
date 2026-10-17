using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExpiryGame.Combat
{
    // 스윙 호(각도 + 거리) 판정. 스윙 한 번에 대상당 한 번만 데미지.
    public class SwingHitDetector : MonoBehaviour, ISwingHitSink
    {
        [SerializeField] private SwingProfile profile;
        [SerializeField] private HitFeelProfile feel;
        [SerializeField] private LayerMask targetMask;

        private readonly List<Collider2D> overlaps = new List<Collider2D>();
        private readonly HashSet<IDamageable> hitOnce = new HashSet<IDamageable>();
        private SwingInfo current;

        public event Action<HitInfo> HitLanded;

        public void BeginSwing(SwingInfo info)
        {
            current = info;
            hitOnce.Clear();
        }

        public int EndSwing()
        {
            return hitOnce.Count;
        }

        public void Sweep(Vector2 origin, float fromAngle, float toAngle)
        {
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = targetMask, useTriggers = false };
            overlaps.Clear();
            Physics2D.OverlapCircle(origin, profile.reach, filter, overlaps);

            var mid = (fromAngle + toAngle) * 0.5f;
            var half = Mathf.Abs(toAngle - fromAngle) * 0.5f;

            foreach (var col in overlaps)
            {
                var target = col.GetComponentInParent<IDamageable>();
                if (target == null || hitOnce.Contains(target)) continue;

                var center = (Vector2)col.bounds.center;
                var toCenter = center - origin;
                var dist = Mathf.Max(toCenter.magnitude, 0.01f);
                var width = Mathf.Max(col.bounds.extents.x, col.bounds.extents.y);
                var widthAngle = Mathf.Asin(Mathf.Clamp01(width / Mathf.Max(dist, width + 0.01f))) * Mathf.Rad2Deg;
                var bearing = Mathf.Atan2(toCenter.y, toCenter.x) * Mathf.Rad2Deg;

                if (Mathf.Abs(Mathf.DeltaAngle(mid, bearing)) > half + widthAngle + profile.angleTolerance) continue;

                hitOnce.Add(target);
                var point = col.ClosestPoint(origin);
                var direction = Vector2.Lerp(current.AimDirection, toCenter / dist, feel.knockbackRadialBlend).normalized;
                var damage = current.Item.CurrentDamage + current.DamageBonus;

                target.TakeDamage(damage);
                HitLanded?.Invoke(new HitInfo(col.gameObject, point, direction, damage, current.Item));
            }
        }
    }
}
