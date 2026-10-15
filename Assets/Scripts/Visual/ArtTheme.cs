using System;
using UnityEngine;

namespace ExpiryGame.Visual
{
    [CreateAssetMenu(menuName = "ShelfLife/Art Theme", fileName = "ArtTheme")]
    public class ArtTheme : ScriptableObject
    {
        [Header("Pixel")]
        [Min(1)] public int pixelsPerUnit = 32;

        [Header("Palette")]
        public Color backgroundColor;

        [Header("Food freshness (x: 0=갓 생산 → 1=썩기 직전, RGB=덮는 색, A=덮는 세기)")]
        public Gradient witherOverlay = new Gradient();
        [Tooltip("썩은 뒤의 덮는 색 (A=세기)")]
        public Color rottenOverlay;

        [Header("Day / Night (x: TimeOfDay01)")]
        public Gradient ambientColorOverDay = new Gradient();
        public AnimationCurve ambientIntensityOverDay = new AnimationCurve();

        [Header("Player light")]
        public Color playerLightColor;
        [Min(0f)] public float playerLightInnerRadius;
        [Min(0f)] public float playerLightOuterRadius;
        public AnimationCurve playerLightIntensityOverDay = new AnimationCurve();

        public Color EvaluateFoodOverlay(float shelfLife01, bool isRotten)
        {
            if (isRotten) return rottenOverlay;
            return witherOverlay.Evaluate(Mathf.Clamp01(1f - shelfLife01));
        }

        public Color EvaluateAmbientColor(float timeOfDay01)
        {
            return ambientColorOverDay.Evaluate(timeOfDay01);
        }

        public float EvaluateAmbientIntensity(float timeOfDay01)
        {
            return ambientIntensityOverDay.Evaluate(timeOfDay01);
        }

        public float EvaluatePlayerLightIntensity(float timeOfDay01)
        {
            return playerLightIntensityOverDay.Evaluate(timeOfDay01);
        }

        [ContextMenu("Reset To Dark Defaults")]
        public void ApplyDarkDefaults()
        {
            pixelsPerUnit = 32;

            backgroundColor = Hex("14161C");

            witherOverlay = MakeGradient(
                new[] { (Hex("8A93A3"), 0f), (Hex("7C8390"), 1f) },
                new[] { (0f, 0f), (0.15f, 0.45f), (0.65f, 1f) });
            rottenOverlay = Hex("5C6B48", 0.8f);

            ambientColorOverDay = MakeGradient(
                new[]
                {
                    (Hex("6F7C92"), 0f), (Hex("9AA7BD"), 0.15f), (Hex("9AA7BD"), 0.4f),
                    (Hex("7A6A78"), 0.5f), (Hex("232A44"), 0.62f), (Hex("1A2036"), 0.9f), (Hex("6F7C92"), 1f),
                },
                new[] { (1f, 0f), (1f, 1f) });
            ambientIntensityOverDay = new AnimationCurve(
                new Keyframe(0f, 0.7f), new Keyframe(0.15f, 0.9f), new Keyframe(0.4f, 0.9f),
                new Keyframe(0.55f, 0.5f), new Keyframe(0.65f, 0.25f), new Keyframe(0.9f, 0.25f), new Keyframe(1f, 0.7f));

            playerLightColor = Hex("FFD9A0");
            playerLightInnerRadius = 1.5f;
            playerLightOuterRadius = 7f;
            playerLightIntensityOverDay = new AnimationCurve(
                new Keyframe(0f, 0.2f), new Keyframe(0.4f, 0.2f), new Keyframe(0.55f, 0.8f),
                new Keyframe(0.65f, 1.2f), new Keyframe(0.9f, 1.2f), new Keyframe(1f, 0.2f));
        }

        private static Color Hex(string rgb, float alpha = 1f)
        {
            ColorUtility.TryParseHtmlString("#" + rgb, out var color);
            color.a = alpha;
            return color;
        }

        private static Gradient MakeGradient((Color color, float time)[] colors, (float alpha, float time)[] alphas)
        {
            var colorKeys = new GradientColorKey[colors.Length];
            for (var i = 0; i < colors.Length; i++) colorKeys[i] = new GradientColorKey(colors[i].color, colors[i].time);
            var alphaKeys = new GradientAlphaKey[alphas.Length];
            for (var i = 0; i < alphas.Length; i++) alphaKeys[i] = new GradientAlphaKey(alphas[i].alpha, alphas[i].time);
            var gradient = new Gradient();
            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }
    }
}
