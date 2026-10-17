using System;
using UnityEngine;

namespace ExpiryGame.Combat
{
    // 넉백 한 번의 움직임 계산 (순수 클래스, 물리/씬을 모른다 → EditMode 테스트가 된다).
    //  - 거리: 요청 거리 × (1 - 저항) × 연속 피격 배율, profile.maxDistance 상한
    //  - 동시에 여러 번 맞아도 속도를 더하지 않는다: 남은 거리 + 새 거리를 상한 안에서 합쳐 한 번의 움직임으로 만든다
    //  - 속도: 순간 속도가 profile.maxSpeed를 넘지 않도록 시간을 늘린다
    //  - 장애물: 매 스텝 clip(방향, 원하는 거리) → 허용 거리를 받아 그만큼만 움직이고, 막히면 거기서 멈춘다
    //  - 경직: 움직임이 끝나도 staggerSeconds 동안은 IsStaggered
    public sealed class KnockbackMotion
    {
        private const int CurveSamples = 32;

        private KnockbackProfile profile;
        private Vector2 direction;
        private float total;
        private float traveled;
        private float elapsed;
        private float duration;
        private float staggerLeft;
        private float lastHitTime = float.NegativeInfinity;
        private int chain;

        public bool IsMoving { get; private set; }
        public bool IsStaggered => staggerLeft > 0f;
        public bool IsActive => IsMoving || IsStaggered;
        public bool WasBlocked { get; private set; }

        // 이번 움직임에서 남은 거리 / 계획된 총 거리 / 계획된 시간 (진단·테스트용)
        public float RemainingDistance => IsMoving ? Mathf.Max(0f, total - traveled) : 0f;
        public float PlannedDistance => total;
        public float PlannedDuration => duration;

        // 반환: 실제로 적용된 거리 (저항 100%면 0, 경직도 걸지 않는다)
        public float Apply(Vector2 dir, float requestedDistance, float resistance, KnockbackProfile settings, float now)
        {
            if (dir.sqrMagnitude <= 0f || requestedDistance <= 0f || settings == null) return 0f;
            resistance = Mathf.Clamp01(resistance);
            if (resistance >= 1f) return 0f;

            chain = now - lastHitTime <= settings.comboWindowSeconds ? chain + 1 : 0;
            lastHitTime = now;

            var distance = requestedDistance * (1f - resistance) * settings.ComboMultiplier(chain);
            distance = Mathf.Min(distance, settings.maxDistance);
            if (distance <= 0.0001f) return 0f;

            dir.Normalize();
            if (IsMoving && total - traveled > 0f)
            {
                var remaining = total - traveled;
                var blended = this.direction * remaining + dir * distance;
                distance = Mathf.Min(remaining + distance, settings.maxDistance);
                if (blended.sqrMagnitude > 0.0001f) dir = blended.normalized;
            }

            profile = settings;
            direction = dir;
            total = distance;
            traveled = 0f;
            elapsed = 0f;
            duration = Mathf.Max(settings.duration, distance * PeakSlope(settings.easeOut) / settings.maxSpeed);
            IsMoving = true;
            WasBlocked = false;
            staggerLeft = Mathf.Max(staggerLeft, settings.staggerSeconds);
            return distance;
        }

        // 이번 프레임에 움직일 변위. clip이 null이면 막힘 없음.
        public Vector2 Step(float dt, Func<Vector2, float, float> clip)
        {
            staggerLeft = Mathf.Max(0f, staggerLeft - dt);
            if (!IsMoving) return Vector2.zero;

            elapsed += dt;
            var t = Mathf.Clamp01(elapsed / duration);
            var target = t >= 1f ? total : total * Mathf.Clamp01(profile.easeOut.Evaluate(t));
            var want = Mathf.Max(0f, target - traveled);

            var allowed = want;
            if (clip != null && want > 0f)
            {
                allowed = Mathf.Clamp(clip(direction, want), 0f, want);
                if (allowed < want - 0.0001f)
                {
                    WasBlocked = true;
                    IsMoving = false; // 장애물에 부딪히면 거기서 멈춘다
                }
            }

            traveled += allowed;
            if (t >= 1f) IsMoving = false;
            return direction * allowed;
        }

        public void Cancel()
        {
            IsMoving = false;
            staggerLeft = 0f;
            chain = 0;
            lastHitTime = float.NegativeInfinity;
        }

        // 곡선의 가장 가파른 기울기 (총 거리 대비 순간 속도 배율). ease-out이면 1보다 크다.
        public static float PeakSlope(AnimationCurve curve)
        {
            var peak = 1f;
            var previous = 0f;
            for (var i = 1; i <= CurveSamples; i++)
            {
                var value = curve.Evaluate(i / (float)CurveSamples);
                peak = Mathf.Max(peak, (value - previous) * CurveSamples);
                previous = value;
            }
            return peak;
        }
    }
}
