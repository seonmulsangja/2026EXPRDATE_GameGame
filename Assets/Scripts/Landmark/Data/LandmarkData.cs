using UnityEngine;
using ExpiryGame.Drop;

namespace ExpiryGame.Landmark
{
    public enum LandmarkKind
    {
        Fridge,
        Campfire,
        Store,
        Dump
    }

    // 랜드마크 한 종류의 모든 수치. 효과(냉장고/모닥불/편의점/쓰레기)는 각각 독립 컴포넌트가 이 SO에서 자기 값만 읽는다.
    [CreateAssetMenu(menuName = "ShelfLife/Landmark Data", fileName = "LandmarkData")]
    public class LandmarkData : ScriptableObject
    {
        [Header("공통")]
        public string displayName = "랜드마크";
        public LandmarkKind kind;
        [Tooltip("월드에 놓일 프리팹 (피벗 = 바닥 중앙)")]
        public GameObject prefab;
        [Tooltip("지역 안에서 이 종류가 뽑힐 상대 가중치")]
        [Min(0f)] public float weight = 1f;
        [Tooltip("차지하는 크기 (타일). 이 영역 + 여유만큼은 장애물을 비운다")]
        public Vector2 size = new Vector2(2f, 2f);
        [Tooltip("효과가 적용되는 반경 (타일)")]
        [Min(0f)] public float effectRadius = 4f;
        public Color ringTint = Color.white;
        [Tooltip("효과 반경 바깥 이 거리 안으로 들어오면 fx_zone_ring이 나타난다")]
        [Min(0f)] public float ringRevealDistance = 3f;

        [Header("냉장고: 반경 안 유통기한 감소 속도 배율 (0.3 = 30% 속도)")]
        [Tooltip("끄면 냉장고는 유통기한을 늦추지 않는다 (드롭만 한다)")]
        public bool decayEnabled = true;
        [Range(0f, 1f)] public float decayMultiplier = 0.3f;

        [Header("냉장고: 상호작용 1회 드롭 (편의점과 별도 수치. 재방문 시 비어 있음)")]
        public DropTable fridgeTable;
        [Range(0f, 1f)] public float fridgeThreat01 = 0f;
        [Tooltip("표를 몇 번 굴릴지. 한 번에 나오는 개수는 DropTable의 min~maxCount")]
        [Min(1)] public int fridgeRolls = 1;
        public string fridgeEmptyPrompt = "비어 있음";

        [Header("모닥불: 반경 안 체력 회복")]
        [Min(0f)] public float healPerSecond = 2f;
        [Min(0.05f)] public float healTickSeconds = 0.5f;

        [Header("모닥불: 음식 강화(F) / 안전지대")]
        public EnhanceProfile enhanceProfile;
        public SafeZoneProfile safeZone;
        [Tooltip("비어 있으면 연료 없이 항상 켜져 있는 모닥불 (이전 동작)")]
        public CampfireFuelProfile fuelProfile;

        [Header("편의점: 상호작용 1회 드롭 (재방문 시 비어 있음)")]
        public DropTable lootTable;
        [Range(0f, 1f)] public float lootThreat01 = 0.4f;
        [Min(1)] public int lootRolls = 3;

        [Header("상호작용 공통 (편의점/냉장고/모닥불): F로 쓸 수 있는 거리와 기본 문구")]
        [Min(0.5f)] public float interactRange = 2f;
        public string interactPrompt = "열기";

        [Header("쓰레기 더미: 썩은 음식 위주 드롭 + 위험 이벤트")]
        public DropTable dumpTable;
        [Range(0f, 1f)] public float dumpThreat01 = 0.2f;
        [Min(1)] public int dumpRolls = 2;

        // 링은 안전지대가 있으면 그 반경으로, 없으면 효과 반경으로 그린다
        public float RingRadius => safeZone != null && safeZone.radius > 0f ? safeZone.radius : effectRadius;
    }
}
