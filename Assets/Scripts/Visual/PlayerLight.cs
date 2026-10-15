using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ExpiryGame.Visual
{
    // 플레이어 주변광(Point Light 2D). 밝기는 시간대 커브를 따라 밤에 강해진다.
    public class PlayerLight : MonoBehaviour
    {
        [SerializeField] private ArtTheme theme;
        [SerializeField] private GameClock clock;
        [SerializeField] private Light2D playerLight;

        private void OnEnable()
        {
            playerLight.lightType = Light2D.LightType.Point;
            playerLight.color = theme.playerLightColor;
            playerLight.pointLightInnerRadius = theme.playerLightInnerRadius;
            playerLight.pointLightOuterRadius = theme.playerLightOuterRadius;
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
            playerLight.intensity = theme.EvaluatePlayerLightIntensity(((IGameClock)clock).TimeOfDay01);
        }
    }
}
