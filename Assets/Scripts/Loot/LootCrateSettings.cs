using ExpiryGame.Drop;
using UnityEngine;

namespace ExpiryGame.Loot
{
    // 맵에 흩어지는 음식 상자(탐험 보상) 수치.
    [CreateAssetMenu(menuName = "ShelfLife/Loot Crate Settings", fileName = "LootCrateSettings")]
    public class LootCrateSettings : ScriptableObject
    {
        [Tooltip("청크가 로드될 때 상자가 생길 확률")]
        [Range(0f, 1f)] public float chancePerChunk = 0.35f;
        [Tooltip("시작 지점 주변은 비움 (시작 지점은 MapGenSettings 기준)")]
        [Min(0f)] public float startClearRadius = 8f;
        [Tooltip("청크 가장자리에서 이만큼 안쪽에만 배치 (타일)")]
        [Min(0f)] public float edgeMargin = 2f;
        [Tooltip("주변 장애물과 겹침 검사 반경")]
        [Min(0.1f)] public float overlapRadius = 0.7f;
        [Min(1)] public int placementAttempts = 6;

        [Header("상자")]
        public DropTable table;
        [Range(0f, 1f)] public float threat01 = 0.3f;
        [Min(1f)] public float health = 15f;
        public Sprite sprite;
        public Color tint = new Color(1f, 0.85f, 0.45f, 1f);
        public Vector2 colliderSize = new Vector2(0.8f, 0.6f);
        public Vector2 colliderOffset = new Vector2(0f, 0.3f);
        public int sortingOrder = 1;
        public Material material;
        [Tooltip("스윙 대상 레이어 (FoodWeapon/SwingHitDetector targetMask에 포함된 레이어, 보통 Enemy)")]
        public int layer;
    }
}
