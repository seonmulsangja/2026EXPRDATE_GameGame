using UnityEngine;

namespace ExpiryGame.Drop
{
    // 착지 순간의 짧은 반짝임. 스프라이트 하나를 키우면서 투명하게.
    [RequireComponent(typeof(SpriteRenderer))]
    public class DropSparkle : MonoBehaviour
    {
        private SpriteRenderer sprite;
        private DropFeelSettings settings;
        private float elapsed;
        private bool playing;

        private SpriteRenderer Sprite => sprite != null ? sprite : (sprite = GetComponent<SpriteRenderer>());

        private void Awake()
        {
            Sprite.enabled = false;
        }

        public void Play(DropFeelSettings feel)
        {
            settings = feel;
            elapsed = 0f;
            playing = feel.sparkleSprite != null;
            if (!playing) return;
            Sprite.sprite = feel.sparkleSprite;
            Sprite.enabled = true;
            Apply(0f);
        }

        public void Stop()
        {
            playing = false;
            Sprite.enabled = false;
        }

        private void Update()
        {
            if (!playing) return;
            elapsed += Time.deltaTime;
            var t = elapsed / settings.sparkleDuration;
            if (t >= 1f)
            {
                Stop();
                return;
            }
            Apply(t);
        }

        private void Apply(float t)
        {
            var color = settings.sparkleColor;
            color.a *= 1f - t;
            Sprite.color = color;
            transform.localScale = Vector3.one * Mathf.Lerp(settings.sparkleStartScale, settings.sparkleEndScale, t);
        }
    }
}
