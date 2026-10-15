using System.Collections.Generic;
using UnityEngine;

namespace ExpiryGame.Combat
{
    // 피격 시 스프라이트를 잠깐 흰색으로. flashMaterial(ShelfLife/SpriteFlash)로 교체했다가 원복한다.
    // 실시간(unscaled) 기준이라 히트스톱 중에도 정확히 해당 시간만 번쩍인다.
    public class FlashOnHit : MonoBehaviour, IFlashable
    {
        [SerializeField] private Material flashMaterial;
        [Tooltip("비어 있으면 자식의 모든 SpriteRenderer")]
        [SerializeField] private SpriteRenderer[] renderers;

        private readonly List<Material> originals = new List<Material>();
        private float endRealtime;
        private bool flashing;

        private void Awake()
        {
            if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        public void Flash(float seconds)
        {
            if (flashMaterial == null) return;
            endRealtime = Time.unscaledTime + seconds;
            if (flashing) return;

            flashing = true;
            originals.Clear();
            foreach (var r in renderers)
            {
                originals.Add(r.sharedMaterial);
                r.sharedMaterial = flashMaterial;
            }
        }

        private void Update()
        {
            if (flashing && Time.unscaledTime >= endRealtime) Restore();
        }

        private void OnDisable()
        {
            if (flashing) Restore();
        }

        private void Restore()
        {
            flashing = false;
            for (var i = 0; i < renderers.Length && i < originals.Count; i++)
                if (renderers[i] != null) renderers[i].sharedMaterial = originals[i];
        }
    }
}
