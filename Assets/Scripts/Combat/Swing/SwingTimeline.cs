using UnityEngine;

namespace ExpiryGame.Combat
{
    public enum SwingPhase { Windup, Strike, Recovery, Done }

    // 스윙 모션 순수 계산 (씬/컴포넌트와 무관).
    // rel 각도: 조준 방향 기준 상대각. 실제 각도 = aim + sign * rel
    public readonly struct SwingTimeline
    {
        private readonly SwingProfile p;

        public SwingTimeline(SwingProfile profile)
        {
            p = profile;
        }

        public float Total => p.TotalSeconds;
        public float StrikeStartRel => -p.arcDegrees * 0.5f;
        public float StrikeEndRel => p.arcDegrees * 0.5f;

        public SwingPhase Evaluate(float elapsed, out float rel, out float phase01)
        {
            var windupRel = StrikeStartRel - p.windupDegrees;
            if (elapsed < p.windupSeconds)
            {
                phase01 = elapsed / p.windupSeconds;
                rel = Mathf.LerpUnclamped(StrikeStartRel, windupRel, p.windupEase.Evaluate(phase01));
                return SwingPhase.Windup;
            }

            elapsed -= p.windupSeconds;
            if (elapsed < p.strikeSeconds)
            {
                phase01 = elapsed / p.strikeSeconds;
                rel = Mathf.LerpUnclamped(windupRel, StrikeEndRel, p.strikeEase.Evaluate(phase01));
                return SwingPhase.Strike;
            }

            elapsed -= p.strikeSeconds;
            if (elapsed < p.recoverySeconds)
            {
                phase01 = elapsed / p.recoverySeconds;
                rel = Mathf.LerpUnclamped(StrikeEndRel, StrikeEndRel + p.followThroughDegrees, p.recoveryEase.Evaluate(phase01));
                return SwingPhase.Recovery;
            }

            phase01 = 1f;
            rel = StrikeEndRel + p.followThroughDegrees;
            return SwingPhase.Done;
        }
    }
}
