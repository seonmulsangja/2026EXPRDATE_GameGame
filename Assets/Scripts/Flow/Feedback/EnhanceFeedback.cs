using System;
using ExpiryGame.Combat;
using ExpiryGame.Landmark;
using UnityEngine;

namespace ExpiryGame.Flow
{
    // 모닥불에서 음식을 강화하면 기존 스파크 파티클(IHitFx)로 불꽃을 튀긴다. 이벤트만 구독하고 모닥불 코드는 연출을 모른다.
    // (효과음은 AudioEventBinder가 같은 이벤트로 처리한다)
    public class EnhanceFeedback : IDisposable
    {
        private readonly ILandmarkEvents events;
        private readonly IHitFx fx;
        private readonly FlowSettings settings;

        public EnhanceFeedback(ILandmarkEvents events, IHitFx fx, FlowSettings settings)
        {
            this.events = events;
            this.fx = fx;
            this.settings = settings;
            if (events != null) events.OnFoodEnhanced += OnEnhanced;
        }

        public void Dispose()
        {
            if (events != null) events.OnFoodEnhanced -= OnEnhanced;
        }

        private void OnEnhanced(FoodEnhancedInfo info)
        {
            if (fx == null || settings.enhanceSparkCount <= 0) return;
            var origin = info.Position + Vector2.up * settings.enhanceSparkHeight;
            fx.BurstParticles(ParticleKind.Spark, origin, Vector2.up, settings.enhanceSparkCount,
                settings.enhanceSparkSpread, settings.enhanceSparkSpeedScale);
        }
    }
}
