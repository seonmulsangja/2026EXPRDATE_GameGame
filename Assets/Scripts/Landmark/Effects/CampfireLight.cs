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
        private float seed;

        private void Awake()
        {
            lightSource = GetComponent<Light2D>();
            seed = Random.value * 100f;
        }

        private void Update()
        {
            var baseIntensity = Mathf.Lerp(intensityAtDay, intensityAtNight, DayNightState.Night01);
            var noise = Mathf.PerlinNoise(seed, Time.time * flickerSpeed) * 2f - 1f;
            lightSource.intensity = baseIntensity * (1f + noise * flicker);
        }
    }
}
