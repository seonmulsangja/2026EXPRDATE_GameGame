using UnityEngine;

namespace ExpiryGame.Flow
{
    // 레벨별 필요 경험치 곡선. x = 현재 레벨, y = 다음 레벨까지 필요한 경험치.
    // 기본값은 첫 레벨업이 60~90초 안에, 이후 40~60초 간격으로 오도록 잡은 것 (docs/balance.md의 추정 근거 참고).
    [CreateAssetMenu(menuName = "ShelfLife/Flow/Xp Curve", fileName = "XpCurve")]
    public class XpCurve : ScriptableObject, IXpTable
    {
        // 레벨 1~12의 기본 필요량
        private static readonly int[] DefaultRequired = { 40, 55, 72, 92, 115, 140, 168, 198, 230, 265, 302, 342 };

        [Tooltip("x = 현재 레벨 (1부터), y = 그 레벨에서 다음 레벨로 가는 데 필요한 경험치")]
        public AnimationCurve requiredByLevel = DefaultCurve();

        [Tooltip("곡선이 주는 값의 하한 (0이나 음수로 무한 레벨업이 일어나는 사고 방지)")]
        [Min(1)] public int minimumRequired = 1;

        public int RequiredFor(int level)
        {
            if (requiredByLevel == null || requiredByLevel.length == 0) return Mathf.Max(minimumRequired, DefaultRequired[Mathf.Clamp(level - 1, 0, DefaultRequired.Length - 1)]);
            return Mathf.Max(minimumRequired, Mathf.RoundToInt(requiredByLevel.Evaluate(Mathf.Max(1, level))));
        }

        private static AnimationCurve DefaultCurve()
        {
            var keys = new Keyframe[DefaultRequired.Length];
            for (var i = 0; i < keys.Length; i++) keys[i] = new Keyframe(i + 1, DefaultRequired[i]);
            var curve = new AnimationCurve(keys);
            for (var i = 0; i < keys.Length; i++) curve.SmoothTangents(i, 0f);
            // 레벨 12 이후는 마지막 값을 유지한다 (필요량이 줄거나 0이 되지 않게)
            curve.postWrapMode = WrapMode.ClampForever;
            return curve;
        }
    }
}
