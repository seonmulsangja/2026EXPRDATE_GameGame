using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ExpiryGame.World
{
    // 화면 가장자리 비네트(URP Volume)를 밤에 강하게. 세기는 DayNightProfile(낮/밤 값) 사이를 Night01로 보간.
    public class NightVignette : MonoBehaviour
    {
        [SerializeField] private DayNightDirector director;
        [SerializeField] private Volume volume;

        private Vignette vignette;

        private void OnEnable()
        {
            if (volume.profile.TryGet(out vignette)) vignette.active = true;
            else Debug.LogWarning("[ShelfLife] Volume 프로필에 Vignette 오버라이드가 없습니다.", this);

            ((IDayNightState)director).Changed += OnChanged;
            OnChanged(((IDayNightState)director).Current);
        }

        private void OnDisable()
        {
            ((IDayNightState)director).Changed -= OnChanged;
        }

        private void OnChanged(DayNightSample sample)
        {
            if (vignette == null) return;
            var profile = ((IDayNightState)director).Profile;
            vignette.intensity.Override(Mathf.Lerp(profile.vignetteAtDay, profile.vignetteAtNight, sample.Night01));
        }
    }
}
