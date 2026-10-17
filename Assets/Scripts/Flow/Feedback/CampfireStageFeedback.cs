using System;
using ExpiryGame.Combat;
using ExpiryGame.Landmark;
using UnityEngine;

namespace ExpiryGame.Flow
{
    // 모닥불 연료 단계가 바뀔 때 불꽃/연기 느낌의 파티클을 낸다 (기존 스파크 파티클 IHitFx 재사용).
    // 이벤트만 구독하고 모닥불 코드는 연출을 모른다. 페이드는 CampfireAppearance가 맡는다.
    public class CampfireStageFeedback : IDisposable
    {
        private readonly ILandmarkEvents events;
        private readonly IHitFx fx;
        private readonly FlowSettings settings;

        public CampfireStageFeedback(ILandmarkEvents events, IHitFx fx, FlowSettings settings)
        {
            this.events = events;
            this.fx = fx;
            this.settings = settings;
            if (events != null) events.OnCampfireStageChanged += OnStageChanged;
        }

        public void Dispose()
        {
            if (events != null) events.OnCampfireStageChanged -= OnStageChanged;
        }

        private void OnStageChanged(CampfireStageInfo info)
        {
            if (fx == null) return;

            // 다시 타오를 때는 더 많이 튀고, 사그라들 때는 적게 (꺼질 때는 한 줌의 연기 대용)
            var lighting = info.To < info.From;
            var count = lighting ? settings.campfireRelightSparkCount : settings.campfireDimSparkCount;
            if (count <= 0) return;
            var origin = info.Position + Vector2.up * settings.enhanceSparkHeight;
            fx.BurstParticles(ParticleKind.Spark, origin, Vector2.up, count, settings.campfireStageSparkSpread, settings.campfireStageSparkSpeedScale);
        }
    }
}
