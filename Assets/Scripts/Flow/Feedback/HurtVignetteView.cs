using UnityEngine;
using UnityEngine.UI;

namespace ExpiryGame.Flow
{
    // 화면 가장자리가 붉게 번쩍였다가 사라지는 비네트. 중앙은 투명한 방사형 텍스처를 한 장 만들어 쓴다.
    public class HurtVignetteView : MonoBehaviour
    {
        private const int TextureSize = 128;

        private FlowSettings settings;
        private Image image;
        private Texture2D texture;
        private Sprite sprite;
        private float intensity;

        public void Build(FlowSettings flowSettings)
        {
            settings = flowSettings;
            texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var half = (TextureSize - 1) * 0.5f;
            for (var y = 0; y < TextureSize; y++)
            for (var x = 0; x < TextureSize; x++)
            {
                var distance = new Vector2((x - half) / half, (y - half) / half).magnitude;
                var alpha = Mathf.Clamp01(Mathf.InverseLerp(0.45f, 1.25f, distance));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
            }
            texture.Apply();
            sprite = Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize), new Vector2(0.5f, 0.5f));

            image = FlowUi.Fill("Vignette", transform, new Color(settings.hurtColor.r, settings.hurtColor.g, settings.hurtColor.b, 0f), false);
            image.sprite = sprite;
        }

        public void Pulse(float strength01)
        {
            intensity = Mathf.Max(intensity, Mathf.Clamp01(strength01));
        }

        private void Update()
        {
            if (image == null) return;
            intensity = Mathf.MoveTowards(intensity, 0f, Time.unscaledDeltaTime / settings.hurtFadeSeconds);
            var color = settings.hurtColor;
            color.a = intensity * settings.hurtMaxAlpha;
            image.color = color;
        }

        private void OnDestroy()
        {
            if (sprite != null) Destroy(sprite);
            if (texture != null) Destroy(texture);
        }
    }
}
