using UnityEngine;

namespace ExpiryGame.Combat
{
    // 스윙 모션/판정 수치. 준비(windup) → 빠른 스윙(strike) → 여운(recovery)
    [CreateAssetMenu(menuName = "ShelfLife/Swing Profile", fileName = "SwingProfile")]
    public class SwingProfile : ScriptableObject
    {
        [Header("Windup (살짝 뒤로 당김)")]
        [Min(0.01f)] public float windupSeconds = 0.09f;
        [Tooltip("스윙 시작 각도보다 더 뒤로 당기는 각도")]
        public float windupDegrees = 25f;
        public AnimationCurve windupEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Strike (빠른 스윙)")]
        [Min(0.01f)] public float strikeSeconds = 0.09f;
        [Tooltip("호의 전체 각도. 조준 방향 기준 좌우로 절반씩")]
        [Range(20f, 240f)] public float arcDegrees = 130f;
        public AnimationCurve strikeEase = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0.6f), new Keyframe(1f, 1f, 2.2f, 0f));

        [Header("Recovery (여운, 천천히 복귀)")]
        [Min(0.01f)] public float recoverySeconds = 0.22f;
        [Tooltip("스윙이 끝난 뒤 더 흘러가는 각도")]
        public float followThroughDegrees = 18f;
        public AnimationCurve recoveryEase = new AnimationCurve(
            new Keyframe(0f, 0f, 3f, 3f), new Keyframe(1f, 1f, 0f, 0f));

        [Header("Cooldown / Direction")]
        [Tooltip("공격 시작부터 다음 공격까지. 스윙 총 시간보다 길게 두면 여유가 생긴다")]
        [Min(0f)] public float cooldownSeconds = 0.45f;
        [Tooltip("true면 휘두를 때마다 방향이 번갈아 바뀐다")]
        public bool alternateDirection = true;

        [Header("Weapon visual")]
        [Min(0.1f)] public float weaponScale = 1.3f;
        [Tooltip("손 피벗에서 스프라이트까지 거리")]
        public float weaponHandOffset = 0.1f;
        [Range(0f, 1f)] public float fadeInPortionOfWindup = 0.6f;
        [Range(0f, 1f)] public float fadeOutPortionOfRecovery = 0.5f;

        [Header("Hit detection (호 판정)")]
        [Min(0.1f)] public float reach = 1.6f;
        [Tooltip("대상 폭과 별개로 더해지는 각도 여유")]
        [Min(0f)] public float angleTolerance = 8f;

        [Header("Recoil (스윙 순간 플레이어가 앞으로 밀림)")]
        [Min(0f)] public float recoilDistance = 0.18f;

        public float TotalSeconds => windupSeconds + strikeSeconds + recoverySeconds;
    }
}
