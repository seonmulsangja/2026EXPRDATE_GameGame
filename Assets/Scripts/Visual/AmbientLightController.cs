using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ExpiryGame.Visual
{
    // 시간대(TimeOfDay01)에 맞춰 글로벌 라이트 색/밝기를 연속적으로 바꾼다.
    // 기존 DayNightLighting과 같은 글로벌 라이트를 쓰므로 씬에서는 둘 중 하나만 켠다.
    public class AmbientLightController : MonoBehaviour
    {
        [SerializeField] private ArtTheme theme;
        [SerializeField] private GameClock clock;
        [SerializeField] private Light2D globalLight;

        private void OnEnable()
        {
            ((IGameClock)clock).Ticked += OnTicked;
            Apply();
        }

        private void OnDisable()
        {
            ((IGameClock)clock).Ticked -= OnTicked;
        }

        private void OnTicked(float _)
        {
            Apply();
        }

        private void Apply()
        {
            var time = ((IGameClock)clock).TimeOfDay01;
            globalLight.color = theme.EvaluateAmbientColor(time);
            globalLight.intensity = theme.EvaluateAmbientIntensity(time);
        }
    }
}
