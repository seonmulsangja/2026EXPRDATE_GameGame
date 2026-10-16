using System;
using UnityEngine;

namespace ExpiryGame.World
{
    public enum DayPart
    {
        Dawn,
        Day,
        Dusk,
        Night
    }

    // 한 시점의 낮밤 상태 (DayNightProfile.Evaluate 결과). 이 값을 소비하는 쪽은 다시 계산하지 않는다.
    public readonly struct DayNightSample
    {
        public readonly DayPart Part;
        public readonly float PartProgress01;
        public readonly Color AmbientColor;
        public readonly float AmbientIntensity;
        // 0 = 완전한 낮, 1 = 완전한 밤. 밤 타일 알파 / 발광 / 비네트가 모두 이 값을 쓴다.
        public readonly float Night01;

        public DayNightSample(DayPart part, float partProgress01, Color ambientColor, float ambientIntensity, float night01)
        {
            Part = part;
            PartProgress01 = partProgress01;
            AmbientColor = ambientColor;
            AmbientIntensity = ambientIntensity;
            Night01 = night01;
        }
    }

    [Serializable]
    public class DayPartSettings
    {
        [Tooltip("하루 중 이 구간의 비율 (4개 구간 합으로 정규화됨)")]
        [Min(0.001f)] public float length = 0.25f;
        [Tooltip("구간 시작(0) → 끝(1) 동안의 전역 조명 색")]
        public Gradient ambientColor = new Gradient();
        [Tooltip("구간 시작 → 끝 전역 조명 밝기")]
        public AnimationCurve ambientIntensity = AnimationCurve.Constant(0f, 1f, 1f);
        [Tooltip("구간 시작 → 끝 밤 진행도 (0 = 낮, 1 = 밤). 밤 타일 알파/발광/비네트의 기준")]
        public AnimationCurve night01 = AnimationCurve.Constant(0f, 1f, 0f);
    }

    // 하루 구간(새벽/낮/황혼/밤) 비율과 구간별 조명을 전부 담는 SO.
    // TimeOfDay01 = 0 에서 새벽으로 시작하고, 구간은 새벽→낮→황혼→밤 순으로 이어진다 (밤이 끝나면 다시 새벽).
    [CreateAssetMenu(menuName = "ShelfLife/Day Night Profile", fileName = "DayNightProfile")]
    public class DayNightProfile : ScriptableObject
    {
        public DayPartSettings dawn = new DayPartSettings();
        public DayPartSettings day = new DayPartSettings();
        public DayPartSettings dusk = new DayPartSettings();
        public DayPartSettings night = new DayPartSettings();

        [Header("밤 연출 (Night01 로 day→night 보간)")]
        [Range(0f, 1f)] public float vignetteAtDay = 0.2f;
        [Range(0f, 1f)] public float vignetteAtNight = 0.5f;
        [Tooltip("*_glow / 모닥불 빛번짐 등 발광 레이어의 최대 알파")]
        [Range(0f, 1f)] public float glowMaxAlpha = 1f;
        [Tooltip("플레이어 주변 소프트 글로우 알파 (낮, 밤)")]
        [Range(0f, 1f)] public float playerGlowAtDay = 0f;
        [Range(0f, 1f)] public float playerGlowAtNight = 0.35f;

        public DayNightSample Evaluate(float timeOfDay01)
        {
            var t = Mathf.Repeat(timeOfDay01, 1f);
            var total = dawn.length + day.length + dusk.length + night.length;
            var cursor = t * total;

            if (TryPick(dawn, DayPart.Dawn, ref cursor, out var sample)) return sample;
            if (TryPick(day, DayPart.Day, ref cursor, out sample)) return sample;
            if (TryPick(dusk, DayPart.Dusk, ref cursor, out sample)) return sample;
            return Build(night, DayPart.Night, cursor / night.length);
        }

        // 해당 구간의 progress(0~1) 지점에 해당하는 TimeOfDay01 (디버그 점프 등에서 구간 비율을 다시 계산하지 않게)
        public float TimeOfDayAt(DayPart part, float progress01)
        {
            var total = dawn.length + day.length + dusk.length + night.length;
            var start = 0f;
            DayPartSettings target;
            switch (part)
            {
                case DayPart.Dawn: target = dawn; break;
                case DayPart.Day: start = dawn.length; target = day; break;
                case DayPart.Dusk: start = dawn.length + day.length; target = dusk; break;
                default: start = dawn.length + day.length + dusk.length; target = night; break;
            }
            return (start + target.length * Mathf.Clamp01(progress01)) / total;
        }

        private static bool TryPick(DayPartSettings part, DayPart kind, ref float cursor, out DayNightSample sample)
        {
            if (cursor < part.length)
            {
                sample = Build(part, kind, cursor / part.length);
                return true;
            }
            cursor -= part.length;
            sample = default;
            return false;
        }

        private static DayNightSample Build(DayPartSettings part, DayPart kind, float progress)
        {
            progress = Mathf.Clamp01(progress);
            return new DayNightSample(kind, progress, part.ambientColor.Evaluate(progress),
                part.ambientIntensity.Evaluate(progress), Mathf.Clamp01(part.night01.Evaluate(progress)));
        }

        // 기본값 (docs/art.md 의 다크 톤 팔레트). 에셋 생성 시 한 번만 호출되고 이후 수치는 에셋에서 조절한다.
        public void ApplyDefaults()
        {
            dawn = Part(0.10f, new[] { Hex("#6F7C92"), Hex("#9AA7BD") }, new[] { 0.7f, 0.9f }, 1f, 0f);
            day = Part(0.35f, new[] { Hex("#9AA7BD"), Hex("#9AA7BD") }, new[] { 0.9f, 0.9f }, 0f, 0f);
            dusk = Part(0.10f, new[] { Hex("#9AA7BD"), Hex("#7A6A78"), Hex("#232A44") }, new[] { 0.9f, 0.5f, 0.3f }, 0f, 1f);
            night = Part(0.45f, new[] { Hex("#232A44"), Hex("#1A2036") }, new[] { 0.25f, 0.25f }, 1f, 1f);
        }

        private static DayPartSettings Part(float length, Color[] colors, float[] intensities, float nightFrom, float nightTo)
        {
            var gradient = new Gradient();
            var keys = new GradientColorKey[colors.Length];
            for (var i = 0; i < colors.Length; i++)
                keys[i] = new GradientColorKey(colors[i], i / (float)(colors.Length - 1));
            gradient.SetKeys(keys, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });

            var intensity = new AnimationCurve();
            for (var i = 0; i < intensities.Length; i++)
                intensity.AddKey(i / (float)(intensities.Length - 1), intensities[i]);

            return new DayPartSettings
            {
                length = length,
                ambientColor = gradient,
                ambientIntensity = intensity,
                night01 = AnimationCurve.Linear(0f, nightFrom, 1f, nightTo),
            };
        }

        private static Color Hex(string value)
        {
            return ColorUtility.TryParseHtmlString(value, out var color) ? color : Color.white;
        }
    }
}
