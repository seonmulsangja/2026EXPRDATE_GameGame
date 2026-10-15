using System;
using UnityEngine;

namespace ExpiryGame.Combat
{
    // 가벼운 스프라이트 파티클 1개: 감속하며 날아가다 페이드/축소 후 풀로 반환
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteParticle : MonoBehaviour
    {
        private SpriteRenderer sr;
        private Vector2 velocity;
        private float drag;
        private float lifetime;
        private float age;
        private float spin;
        private float startScale;
        private Action<SpriteParticle> onFinished;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        public void Launch(Sprite sprite, Vector2 position, Vector2 launchVelocity, float dragPerSecond,
            float seconds, float spinDegrees, float scale, Action<SpriteParticle> finished)
        {
            sr.sprite = sprite;
            sr.color = Color.white;
            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.value * 360f);
            transform.localScale = Vector3.one * scale;
            velocity = launchVelocity;
            drag = dragPerSecond;
            lifetime = seconds;
            spin = spinDegrees;
            startScale = scale;
            age = 0f;
            onFinished = finished;
            enabled = true;
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            age += dt;
            if (age >= lifetime)
            {
                enabled = false;
                onFinished?.Invoke(this);
                return;
            }

            transform.position += (Vector3)(velocity * dt);
            velocity *= Mathf.Exp(-drag * dt);
            transform.Rotate(0f, 0f, spin * dt);

            var t = age / lifetime;
            var fade = 1f - Mathf.Clamp01((t - 0.5f) / 0.5f);
            sr.color = new Color(1f, 1f, 1f, fade);
            transform.localScale = Vector3.one * (startScale * Mathf.Lerp(1f, 0.5f, t));
        }
    }
}
