using System.Collections.Generic;
using UnityEngine;

namespace ExpiryGame.Combat
{
    // 스파크/슬래시/파티클 생성과 풀링만 담당. 언제 무엇을 얼마나 낼지는 HitFeedbackPlayer가 정한다.
    public class HitFxSpawner : MonoBehaviour, IHitFx
    {
        [Header("Sprites")]
        [SerializeField] private Sprite[] slashFrames = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite[] hitSparkFrames = System.Array.Empty<Sprite>();
        [SerializeField] private Sprite crumb;
        [SerializeField] private Sprite juice;
        [SerializeField] private Sprite rot;
        [SerializeField] private Sprite spark;

        [Header("Look")]
        [Tooltip("빛 영향을 받지 않는 스프라이트 머티리얼 (Sprite-Unlit-Default 권장)")]
        [SerializeField] private Material fxMaterial;
        [SerializeField] private int sortingOrder = 20;
        [Min(0.1f)] [SerializeField] private float slashScale = 2f;
        [Min(0.1f)] [SerializeField] private float sparkScale = 1.2f;
        [Min(0.01f)] [SerializeField] private float slashSeconds = 0.12f;
        [Min(0.01f)] [SerializeField] private float sparkSeconds = 0.16f;

        [Header("Particles")]
        [Min(0.1f)] [SerializeField] private float particleBaseSpeed = 4.5f;
        [Min(0f)] [SerializeField] private float particleDrag = 5f;
        [Min(0.05f)] [SerializeField] private Vector2 particleLifetime = new Vector2(0.3f, 0.55f);
        [Min(0.1f)] [SerializeField] private float particleScale = 1.2f;

        private readonly Stack<SpriteFlipbook> flipbookPool = new Stack<SpriteFlipbook>();
        private readonly Stack<SpriteParticle> particlePool = new Stack<SpriteParticle>();

        public void PlaySpark(Vector2 position)
        {
            if (hitSparkFrames.Length == 0) return;
            var fx = GetFlipbook();
            fx.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, Random.value * 360f));
            fx.transform.localScale = Vector3.one * sparkScale;
            fx.Play(hitSparkFrames, sparkSeconds, ReleaseFlipbook);
        }

        public void PlaySlash(Vector2 position, float angleDegrees)
        {
            if (slashFrames.Length == 0) return;
            var fx = GetFlipbook();
            fx.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angleDegrees));
            fx.transform.localScale = Vector3.one * slashScale;
            fx.Play(slashFrames, slashSeconds, ReleaseFlipbook);
        }

        public void BurstParticles(ParticleKind kind, Vector2 position, Vector2 direction, int count, float spreadDegrees, float speedScale)
        {
            var sprite = SpriteFor(kind);
            if (sprite == null) return;

            var baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            for (var i = 0; i < count; i++)
            {
                var angle = (baseAngle + Random.Range(-spreadDegrees, spreadDegrees)) * Mathf.Deg2Rad;
                var speed = particleBaseSpeed * speedScale * Random.Range(0.5f, 1.1f);
                var velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;

                var p = GetParticle();
                p.Launch(sprite, position, velocity, particleDrag, Random.Range(particleLifetime.x, particleLifetime.y),
                    Random.Range(-360f, 360f), particleScale * Random.Range(0.7f, 1.2f), ReleaseParticle);
            }
        }

        private Sprite SpriteFor(ParticleKind kind)
        {
            switch (kind)
            {
                case ParticleKind.Crumb: return crumb;
                case ParticleKind.Juice: return juice;
                case ParticleKind.Rot: return rot;
                default: return spark;
            }
        }

        private SpriteFlipbook GetFlipbook()
        {
            if (flipbookPool.Count > 0)
            {
                var pooled = flipbookPool.Pop();
                pooled.gameObject.SetActive(true);
                return pooled;
            }
            return CreateFx<SpriteFlipbook>("Fx_Flipbook");
        }

        private SpriteParticle GetParticle()
        {
            if (particlePool.Count > 0)
            {
                var pooled = particlePool.Pop();
                pooled.gameObject.SetActive(true);
                return pooled;
            }
            return CreateFx<SpriteParticle>("Fx_Particle");
        }

        private void ReleaseFlipbook(SpriteFlipbook fx)
        {
            fx.gameObject.SetActive(false);
            flipbookPool.Push(fx);
        }

        private void ReleaseParticle(SpriteParticle fx)
        {
            fx.gameObject.SetActive(false);
            particlePool.Push(fx);
        }

        private T CreateFx<T>(string objectName) where T : Component
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            if (fxMaterial != null) sr.sharedMaterial = fxMaterial;
            sr.sortingOrder = sortingOrder;
            return go.AddComponent<T>();
        }
    }
}
