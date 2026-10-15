using System;
using UnityEngine;

namespace ExpiryGame.Drop
{
    // 포물선 + 작은 바운스. 루트는 지면 위치를 이동하고, body만 높이만큼 위로 띄운다(그림자는 지면에 남음).
    public class DropArc : MonoBehaviour
    {
        [SerializeField] private Transform body;
        [SerializeField] private Transform shadow;

        private DropFeelSettings settings;
        private Vector3 from;
        private Vector3 to;
        private float elapsed;
        private bool playing;
        private bool touchedDown;
        private Vector3 bodyRest;
        private Vector3 shadowRest;

        public bool IsPlaying => playing;

        public event Action Touchdown;
        public event Action Settled;

        private void Awake()
        {
            bodyRest = body.localPosition;
            shadowRest = shadow != null ? shadow.localScale : Vector3.one;
        }

        public void Play(Vector3 start, Vector3 end, DropFeelSettings feel)
        {
            settings = feel;
            from = start;
            to = end;
            elapsed = 0f;
            touchedDown = false;
            playing = true;
            transform.position = start;
            Apply(0f);
        }

        public void Cancel()
        {
            playing = false;
            if (body != null) body.localPosition = bodyRest;
            if (shadow != null) shadow.localScale = shadowRest;
        }

        private void Update()
        {
            if (!playing) return;
            elapsed += Time.deltaTime;

            var arc = settings.arcDuration;
            var bounceEnabled = settings.bounceHeightRatio > 0f;
            var total = arc + (bounceEnabled ? settings.bounceDuration : 0f);

            if (elapsed < arc)
            {
                var t = elapsed / arc;
                transform.position = Vector3.Lerp(from, to, t);
                Apply(settings.arcHeight * 4f * t * (1f - t));
                return;
            }

            transform.position = to;
            if (!touchedDown)
            {
                touchedDown = true;
                Touchdown?.Invoke();
            }

            if (bounceEnabled && elapsed < total)
            {
                var t = (elapsed - arc) / settings.bounceDuration;
                Apply(settings.arcHeight * settings.bounceHeightRatio * 4f * t * (1f - t));
                return;
            }

            Apply(0f);
            playing = false;
            Settled?.Invoke();
        }

        private void Apply(float height)
        {
            body.localPosition = bodyRest + new Vector3(0f, height, 0f);
            if (shadow == null) return;
            var lift01 = settings.arcHeight <= 0f ? 0f : Mathf.Clamp01(height / settings.arcHeight);
            shadow.localScale = shadowRest * Mathf.Lerp(1f, settings.shadowMinScale, lift01);
        }
    }
}
