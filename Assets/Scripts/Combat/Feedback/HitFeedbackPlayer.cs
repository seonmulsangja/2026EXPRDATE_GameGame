using UnityEngine;

namespace ExpiryGame.Combat
{
    // 타격감 총괄. 적중(HitLanded) / 스윙 / 무기 파괴(FoodItem.Broken)를 구독해서
    // 데미지→강도를 한 번 계산하고, 각 효과 컴포넌트에 위임만 한다.
    public class HitFeedbackPlayer : MonoBehaviour
    {
        [SerializeField] private HitFeelProfile feel;
        [SerializeField] private SwingProfile swing;
        [Tooltip("타격 넉백 수치. 비우면 Resources/ShelfLife/Knockback_Hit")]
        [SerializeField] private KnockbackProfile hitKnockback;
        [SerializeField] private SwingController swingController;
        [SerializeField] private SwingHitDetector hitDetector;
        [Tooltip("IHitStop 구현 (HitStop)")]
        [SerializeField] private MonoBehaviour hitStop;
        [Tooltip("ICameraShaker 구현 (CameraShake, 카메라에 부착)")]
        [SerializeField] private MonoBehaviour cameraShake;
        [Tooltip("IHitFx 구현 (HitFxSpawner)")]
        [SerializeField] private MonoBehaviour fx;
        [Tooltip("스윙 반동을 받을 대상 (IKnockbackable, 보통 플레이어 자신의 KnockbackReceiver)")]
        [SerializeField] private MonoBehaviour recoilReceiver;

        private FoodItem subscribedItem;

        private void OnEnable()
        {
            hitDetector.HitLanded += OnHit;
            swingController.SwingStarted += OnSwingStarted;
            swingController.StrikeStarted += OnStrikeStarted;
            swingController.SwingEnded += OnSwingEnded;
        }

        private void OnDisable()
        {
            hitDetector.HitLanded -= OnHit;
            swingController.SwingStarted -= OnSwingStarted;
            swingController.StrikeStarted -= OnStrikeStarted;
            swingController.SwingEnded -= OnSwingEnded;
            Unsubscribe();
        }

        private void OnSwingStarted(SwingInfo info)
        {
            Unsubscribe();
            subscribedItem = info.Item;
            subscribedItem.Broken += OnBroken;
        }

        private void OnSwingEnded(SwingInfo info)
        {
            Unsubscribe();
        }

        private void Unsubscribe()
        {
            if (subscribedItem == null) return;
            subscribedItem.Broken -= OnBroken;
            subscribedItem = null;
        }

        private void OnStrikeStarted(SwingInfo info)
        {
            var hand = info.Origin;
            ((IHitFx)fx).PlaySlash(hand + info.AimDirection * 0.5f, info.AimAngle);

            if (recoilReceiver != null)
                ((IKnockbackable)recoilReceiver).ApplyKnockback(info.AimDirection, swing.recoilDistance);
        }

        private void OnHit(HitInfo hit)
        {
            var intensity = feel.IntensityFor(hit.Damage); // 데미지→강도 변환은 여기서 한 번만

            ((IHitStop)hitStop).Play(feel.hitStopSeconds.Evaluate(intensity));
            ((ICameraShaker)cameraShake).Shake(hit.Direction, feel.shakeAmplitude.Evaluate(intensity), feel.shakeSeconds.Evaluate(intensity));

            var knockable = hit.Target.GetComponentInParent<IKnockbackable>();
            if (knockable != null)
            {
                var knockback = KnockbackProfile.ResolveHit(hitKnockback);
                knockable.ApplyKnockback(hit.Direction, knockback.DistanceFor(hit.Damage), knockback);
            }

            var flashable = hit.Target.GetComponentInParent<IFlashable>();
            flashable?.Flash(feel.flashSeconds);

            var hitFx = (IHitFx)fx;
            hitFx.PlaySpark(hit.Point);
            hitFx.BurstParticles(feel.ParticleKindFor(hit.Weapon), hit.Point, hit.Direction,
                Mathf.RoundToInt(feel.particleCount.Evaluate(intensity)), feel.particleSpreadDegrees,
                feel.particleSpeedScale.Evaluate(intensity));
        }

        private void OnBroken(FoodItem item)
        {
            var position = swingController.WeaponTip;
            var kind = feel.ParticleKindFor(item);
            var hitFx = (IHitFx)fx;

            hitFx.BurstParticles(kind, position, swingController.Current.AimDirection, feel.breakParticleCount, 180f, 1.3f);
            hitFx.BurstParticles(ParticleKind.Spark, position, swingController.Current.AimDirection, feel.breakParticleCount / 3, 180f, 1.6f);
            hitFx.PlaySpark(position);
            ((ICameraShaker)cameraShake).Shake(Vector2.zero, feel.breakShakeAmplitude, feel.breakShakeSeconds);
            ((IHitStop)hitStop).Play(feel.breakHitStopSeconds);
        }
    }
}
