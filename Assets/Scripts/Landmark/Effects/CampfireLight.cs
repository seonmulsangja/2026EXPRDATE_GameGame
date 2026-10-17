using UnityEngine;
using UnityEngine.Rendering.Universal;
using ExpiryGame.World;

namespace ExpiryGame.Landmark
{
    // 모닥불 Light 2D: 낮엔 약하게, 밤엔 강하게 + 불꽃처럼 깜빡임.
    [RequireComponent(typeof(Light2D))]
    public class CampfireLight : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float intensityAtDay = 0.5f;
        [SerializeField, Min(0f)] private float intensityAtNight = 1.4f;
        [SerializeField, Range(0f, 0.5f)] private float flicker = 0.15f;
        [SerializeField, Min(0.1f)] private float flickerSpeed = 7f;

        private Light2D lightSource;
        private CampfireFuel fuel;
        private float seed;
        private float baseOuter;
        private float baseInner;
        private float radiusFollow = 1f;
        private float intensityFollow = 1f;

        private void Awake()
        {
            lightSource = GetComponent<Light2D>();
            fuel = GetComponentInParent<CampfireFuel>();
            baseOuter = lightSource.pointLightOuterRadius;
            baseInner = lightSource.pointLightInnerRadius;
            seed = Random.value * 100f;
        }

        private void Update()
        {
            var baseIntensity = Mathf.Lerp(intensityAtDay, intensityAtNight, DayNightState.Night01);
            var noise = Mathf.PerlinNoise(seed, Time.time * flickerSpeed) * 2f - 1f;
            var intensity = baseIntensity * (1f + noise * flicker);

            // 연료 비율에 따라 빛 반경/밝기를 줄이고(부드럽게 따라간다), 꺼지면 빛을 끈다
            if (fuel != null && fuel.HasProfile)
            {
                var profile = fuel.Profile;
                var step = profile.lightFollowSpeed * Time.deltaTime;
                radiusFollow = Mathf.MoveTowards(radiusFollow, fuel.Strength(profile.lightRadiusAtEmpty), step);
                intensityFollow = Mathf.MoveTowards(intensityFollow, fuel.Strength(profile.lightIntensityAtEmpty), step);
                lightSource.pointLightOuterRadius = baseOuter * radiusFollow;
                lightSource.pointLightInnerRadius = Mathf.Min(baseInner, baseOuter * radiusFollow);
                intensity *= intensityFollow;
                var on = intensityFollow > 0.001f;
                if (lightSource.enabled != on) lightSource.enabled = on;
            }
            lightSource.intensity = intensity;
        }
    }
}
