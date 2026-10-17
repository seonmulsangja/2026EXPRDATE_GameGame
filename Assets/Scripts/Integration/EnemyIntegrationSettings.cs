using System;
using ExpiryGame.Drop;
using UnityEngine;

namespace ExpiryGame.Integration
{
    [Serializable]
    public struct EnemyExperience
    {
        [Tooltip("EnemySO.enemyName과 같은 이름")]
        public string enemyName;
        [Min(0)] public int experience;
    }

    // 팀원 적 시스템을 이 프로젝트에 붙이는 데 필요한 수치. 팀원의 EnemySO(체력 3~15, 데미지 1, 속도 3~10)는 건드리지 않고
    // 여기 배율로 이 프로젝트의 단위(플레이어 체력 100, 플레이어 속도 5)에 맞춘다.
    // 일차에 따라 변하는 값(속도 비율, 체력/피해 배율, 동시 적 수, 접촉 주기, 감지 반경)은 DifficultyProfile에 있다.
    [CreateAssetMenu(menuName = "ShelfLife/Integration/Enemy Integration Settings", fileName = "EnemyIntegrationSettings")]
    public class EnemyIntegrationSettings : ScriptableObject
    {
        [Header("적 프리팹 / 드롭")]
        public GameObject enemyPrefab;
        public DropTable dropTable;

        [Header("단위 변환 (팀원 EnemySO → 이 프로젝트)")]
        [Tooltip("EnemySO.health × 이 값 × DifficultyProfile.health = 체력. 시작 무기(데미지 약 18~24)로 1일차 일반 적을 1~2타에 잡는 값")]
        [Min(0.1f)] public float healthScale = 2f;
        [Tooltip("EnemySO.damage × 이 값 × DifficultyProfile.damage = 접촉 피해. 5 = 플레이어 최대 체력(100)의 5%")]
        [Min(0.1f)] public float damageScale = 5f;

        [Header("접촉 공격")]
        [Tooltip("적이 플레이어에 닿은 뒤 첫 타격까지의 지연 (초). 그동안 적은 멈추고 번쩍인다. 이후 주기는 DifficultyProfile.contactInterval")]
        [Min(0f)] public float firstHitDelay = 0.4f;
        [Tooltip("닿은 순간 적이 번쩍이는 시간 (초)")]
        [Min(0f)] public float telegraphFlashSeconds = 0.15f;

        [Header("속도")]
        [Tooltip("이 속도(EnemySO.speed)인 종류가 기준(1배)이다. 가장 흔한 '괴물'의 5")]
        [Min(0.1f)] public float referenceEnemySpeed = 5f;
        [Tooltip("종류 보정 = (EnemySO.speed ÷ 기준)의 이 거듭제곱. 1이면 SO 속도 차이를 그대로, 0.5면 절반쯤만 반영 (SO 속도 3~10을 0.77~1.41배로 좁힌다)")]
        [Range(0f, 1f)] public float kindSpeedExponent = 0.5f;

        [Header("추격 / 배회")]
        [Tooltip("추격을 멈추는 반경 = 감지 반경 × 이 값 (경계에서 왔다 갔다 하지 않게)")]
        [Min(1f)] public float loseRadiusMultiplier = 1.3f;
        [Tooltip("감지 반경 밖에서 배회할 때의 속도 (추격 속도 대비)")]
        [Range(0f, 1f)] public float wanderSpeedRatio = 0.3f;
        [Min(0.1f)] public float wanderMoveMinSeconds = 1f;
        [Min(0.1f)] public float wanderMoveMaxSeconds = 2f;
        [Tooltip("멈춰 서 있는 시간 (초)")]
        [Min(0f)] public float wanderPauseMinSeconds = 1.5f;
        [Min(0f)] public float wanderPauseMaxSeconds = 3.5f;

        [Header("경험치 (처치 시 지급)")]
        [Min(0)] public int defaultExperience = 8;
        [Tooltip("종류별 지급량. 목록에 없는 종류는 defaultExperience")]
        public EnemyExperience[] experienceByEnemy = new EnemyExperience[0];

        [Header("위협도 (강한 음식 드롭 가중치에 쓰임)")]
        [Range(0f, 1f)] public float threatHealthWeight = 0.5f;
        [Range(0f, 1f)] public float threatSpeedWeight = 0.3f;
        [Range(0f, 1f)] public float threatDamageWeight = 0.2f;

        [Header("청크 스폰")]
        [Tooltip("난이도 배율 1일 때 청크 하나당 목표 적 수 (동시 적 수 상한은 DifficultyProfile.maxAlive)")]
        [Min(0f)] public float enemiesPerChunk = 1.2f;
        [Min(1)] public int hardCap = 60;
        [Tooltip("플레이어에게서 이 거리 안에는 스폰하지 않는다. 실제 하한은 화면 밖 규칙(offscreenMargin)이 정하고 이 값은 안전망이다 (화면 위아래 반높이 7 + 여유 = 8.5)")]
        [Min(1f)] public float minSpawnDistance = 8f;
        [Tooltip("카메라가 보는 영역에서 이만큼 더 바깥에만 스폰한다 (타일)")]
        [Min(0f)] public float offscreenMargin = 1.5f;
        [Tooltip("플레이어에게서 이보다 먼 곳에는 스폰하지 않는다. 너무 멀면 만날 일이 없다")]
        [Min(5f)] public float maxSpawnDistance = 22f;
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

        public int ExperienceFor(string enemyName)
        {
            if (experienceByEnemy != null)
            {
                foreach (var entry in experienceByEnemy)
                    if (entry.enemyName == enemyName) return Mathf.Max(0, entry.experience);
            }
            return Mathf.Max(0, defaultExperience);
        }
    }
}
