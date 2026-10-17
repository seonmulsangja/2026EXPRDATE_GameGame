using ExpiryGame.Drop;
using UnityEngine;

namespace ExpiryGame.Integration
{
    // 팀원 적 시스템을 이 프로젝트에 붙이는 데 필요한 수치 전부. 팀원의 EnemySO(체력 3~15, 데미지 1)는 건드리지 않고
    // 여기 배율로 이 프로젝트의 체력/데미지 단위(플레이어 체력 100)에 맞춘다.
    [CreateAssetMenu(menuName = "ShelfLife/Integration/Enemy Integration Settings", fileName = "EnemyIntegrationSettings")]
    public class EnemyIntegrationSettings : ScriptableObject
    {
        [Header("적 프리팹 / 드롭")]
        public GameObject enemyPrefab;
        public DropTable dropTable;

        [Header("단위 변환 (팀원 EnemySO → 이 프로젝트)")]
        [Tooltip("EnemySO.health × 이 값 = 기본 체력 (음식 무기 데미지가 10~30 수준)")]
        [Min(0.1f)] public float healthScale = 6f;
        [Tooltip("EnemySO.damage × 이 값 = 기본 접촉 데미지 (플레이어 체력 100)")]
        [Min(0.1f)] public float damageScale = 5f;
        [Tooltip("계속 붙어 있을 때 접촉 데미지를 다시 주는 간격 (초)")]
        [Min(0.1f)] public float contactInterval = 0.8f;

        [Header("위협도 (강한 음식 드롭 가중치에 쓰임)")]
        [Range(0f, 1f)] public float threatHealthWeight = 0.5f;
        [Range(0f, 1f)] public float threatSpeedWeight = 0.3f;
        [Range(0f, 1f)] public float threatDamageWeight = 0.2f;

        [Header("청크 스폰")]
        [Tooltip("난이도 배율 1일 때 청크 하나당 목표 적 수")]
        [Min(0f)] public float enemiesPerChunk = 1.2f;
        [Tooltip("살아 있는 적 수 상한 (난이도 배율을 곱한 뒤 hardCap으로 한 번 더 제한)")]
        [Min(1)] public int maxAlive = 24;
        [Min(1)] public int hardCap = 60;
        [Tooltip("플레이어에게서 이 거리 안에는 스폰하지 않는다 (화면 밖에서 나타나도록)")]
        [Min(1f)] public float minSpawnDistance = 11f;
        [Tooltip("부족한 청크를 채우는 주기 (초)")]
        [Min(0.2f)] public float topUpInterval = 2.5f;
        [Tooltip("한 번의 보충에서 새로 내보내는 적 수 상한 (한꺼번에 몰려 나오지 않게)")]
        [Min(1)] public int spawnsPerTick = 3;
        [Min(1)] public int spawnAttempts = 6;
        [Min(0f)] public float edgeMargin = 1f;
        [Min(0f)] public float obstacleCheckRadius = 0.5f;

        [Header("넉백")]
        [Tooltip("모닥불 안전지대가 이미 있는 적을 밀어낼 때 쓰는 수치 (타격 넉백과 별개). 비우면 Resources/ShelfLife/Knockback_SafeZone")]
        public ExpiryGame.Combat.KnockbackProfile safeZoneKnockback;

        [Header("위험 구역 (쓰레기 더미)")]
        [Tooltip("위험 구역 안에서 청크 목표 적 수에 곱하는 배율")]
        [Min(1f)] public float dangerSpawnMultiplier = 2f;
        [Tooltip("위험 구역에 들어간 순간 주변에 추가로 내보내는 적 수 (난이도 배율 적용)")]
        [Min(0)] public int dangerBurstCount = 4;
        [Min(1f)] public float dangerBurstRadius = 5f;
    }
}
