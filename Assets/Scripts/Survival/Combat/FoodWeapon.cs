using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExpiryGame
{
    public class FoodWeapon : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private FoodInventory inventory;
        [SerializeField] private PlayerFacing facing;
        [SerializeField] private LayerMask targetMask;
        [SerializeField, Min(0f)] private float reach = 1f;
        [SerializeField, Min(0.01f)] private float radius = 0.7f;
        [SerializeField, Min(0f)] private float cooldownSeconds = 0.4f;
        [Tooltip("(선택) IAttackDriver를 구현한 컴포넌트(SwingController 등). 있으면 판정/연출을 위임하고, 없으면 즉발 원형 판정을 쓴다.")]
        [SerializeField] private MonoBehaviour attackDriver;

        private readonly List<Collider2D> hits = new List<Collider2D>();
        private readonly HashSet<IDamageable> targets = new HashSet<IDamageable>();
        private float nextAttackTime;

        public event Action<FoodItem, int> WeaponUsed;

        private void OnEnable()
        {
            ((IPlayerInput)input).AttackPerformed += Attack;
        }

        private void OnDisable()
        {
            ((IPlayerInput)input).AttackPerformed -= Attack;
        }

        private void Attack()
        {
            var item = ((IFoodInventory)inventory).Selected;
            if (item == null || Time.time < nextAttackTime) return;

            var driver = attackDriver as IAttackDriver;
            if (driver == null)
            {
                nextAttackTime = Time.time + cooldownSeconds;
                CollectTargets();
                foreach (var target in targets) target.TakeDamage(item.CurrentDamage);
                Commit(item, targets.Count);
                return;
            }

            if (driver.IsBusy) return;
            nextAttackTime = Time.time + driver.CooldownSeconds;
            driver.Begin(item, hitCount => Commit(item, hitCount));
        }

        private void Commit(FoodItem item, int hitCount)
        {
            item.UseDurability();
            if (item.IsBroken) ((IFoodInventory)inventory).Remove(item);
            WeaponUsed?.Invoke(item, hitCount);
        }

        private void CollectTargets()
        {
            var center = (Vector2)transform.position + ((IFacing)facing).FacingDirection * reach;
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = targetMask, useTriggers = false };

            targets.Clear();
            Physics2D.OverlapCircle(center, radius, filter, hits);
            foreach (var hit in hits)
            {
                var target = hit.GetComponentInParent<IDamageable>();
                if (target != null) targets.Add(target);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (facing == null) return;
            Gizmos.DrawWireSphere((Vector2)transform.position + ((IFacing)facing).FacingDirection * reach, radius);
        }
    }
}
