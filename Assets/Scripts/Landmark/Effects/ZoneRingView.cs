using UnityEngine;

namespace ExpiryGame.Landmark
{
    // 효과 반경을 fx_zone_ring(점선 원)으로 표시. 가까이 갔을 때만 보이고 종류별 틴트를 쓴다.
    [RequireComponent(typeof(LandmarkZone))]
    public class ZoneRingView : MonoBehaviour
    {
        // fx_zone_ring.png는 128px / PPU 32 = 4 유닛 지름
        private const float RingSpriteDiameter = 4f;

        [SerializeField] private SpriteRenderer ring;

        private LandmarkZone zone;
        private float applied = -1f;
        private float appliedRadius = -1f;

        private void Awake()
        {
            zone = GetComponent<LandmarkZone>();
        }

        private void Update()
        {
            var data = zone.Data;
            if (data == null) return;

            var radius = data.RingRadius;
            if (!Mathf.Approximately(appliedRadius, radius))
            {
                appliedRadius = radius;
                ring.transform.position = transform.position;
                ring.transform.localScale = Vector3.one * (radius * 2f / RingSpriteDiameter);
            }

            var reveal = Mathf.Max(0.01f, data.ringRevealDistance);
            var alpha = 1f - Mathf.Clamp01((zone.PlayerDistance - radius) / reveal);
            if (Mathf.Abs(alpha - applied) < 0.01f) return;
            applied = alpha;
            var color = data.ringTint;
            color.a = alpha * data.ringTint.a;
            ring.color = color;
        }
    }
}
