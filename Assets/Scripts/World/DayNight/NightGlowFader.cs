using UnityEngine;

namespace ExpiryGame.World
{
    // 밤에만 켜지는 발광 스프라이트(*_glow, 플레이어 주변 글로우 등). Additive 머티리얼의 알파를 Night01로 페이드 인.
    // 프리팹에 붙는 컴포넌트라 씬 참조 대신 DayNightState.Instance를 쓴다.
    [RequireComponent(typeof(SpriteRenderer))]
    public class NightGlowFader : MonoBehaviour
    {
        [Tooltip("밤 최대 알파에 곱하는 배율 (DayNightProfile.glowMaxAlpha × 이 값)")]
        [SerializeField, Range(0f, 1f)] private float strength = 1f;
        [Tooltip("낮에도 남는 최소 알파 (모닥불처럼 낮에도 약하게 보여야 할 때)")]
        [SerializeField, Range(0f, 1f)] private float dayAlpha;

        private SpriteRenderer sr;
        private float applied = -1f;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            applied = -1f;
        }

        private void Update()
        {
            var state = DayNightState.Instance;
            var night = state != null ? state.Current.Night01 : 0f;
            var max = state != null && state.Profile != null ? state.Profile.glowMaxAlpha : 1f;
            var alpha = Mathf.Lerp(dayAlpha, max * strength, night);
            if (Mathf.Abs(alpha - applied) < 0.002f) return;
            applied = alpha;
            var color = sr.color;
            color.a = alpha;
            sr.color = color;
        }
    }
}
