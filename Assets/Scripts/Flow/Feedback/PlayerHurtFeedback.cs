using System;
using UnityEngine;
using ExpiryGame.Combat;

namespace ExpiryGame.Flow
{
    // 플레이어 체력이 줄어들 때 연출을 모아 건다: 붉은 비네트 + 짧은 카메라 흔들림 + 플레이어 플래시.
    // 체력 감소의 원인(적 접촉, 썩은 음식 등)은 가리지 않는다. 연출 각각은 기존 컴포넌트에 위임한다.
    public class PlayerHurtFeedback : IDisposable
    {
        private readonly Health health;
        private readonly FlowSettings settings;
        private readonly HurtVignetteView vignette;
        private readonly ICameraShaker shaker;
        private readonly IFlashable flashable;

        public PlayerHurtFeedback(Health health, FlowSettings settings, HurtVignetteView vignette, ICameraShaker shaker)
        {
            this.health = health;
            this.settings = settings;
            this.vignette = vignette;
            this.shaker = shaker;
            flashable = health.GetComponentInChildren<IFlashable>();
            health.HealthChanged += OnHealthChanged;
            lastCurrent = health.Current;
        }

        private float lastCurrent;

        public void Dispose()
        {
            if (health != null) health.HealthChanged -= OnHealthChanged;
        }

        private void OnHealthChanged(float current, float max)
        {
            var lost = lastCurrent - current;
            lastCurrent = current;
            if (lost <= 0f) return;

            var strength = Mathf.Clamp01(lost / (max * settings.hurtFullIntensityRatio));
            vignette.Pulse(Mathf.Max(0.35f, strength));
            shaker?.Shake(UnityEngine.Random.insideUnitCircle, settings.hurtShakeAmplitude * Mathf.Lerp(0.5f, 1f, strength), settings.hurtShakeSeconds);
            flashable?.Flash(settings.hurtFlashSeconds);
        }
    }
}
