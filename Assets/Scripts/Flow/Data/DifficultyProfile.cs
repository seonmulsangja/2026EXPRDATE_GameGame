using UnityEngine;

namespace ExpiryGame.Flow
{
    // 일차별 난이도 곡선. x축 = 경과 일수 (0 = 1일차 새벽, 1 = 2일차 새벽, ...).
    //  - "배율 곡선"(spawn/health/damage): 하루 안에서도 다음 날 값으로 부드럽게 이어진다. 1 = 기준
    //  - "일차별 값"(speedRatio/speedCapRatio/maxAlive/contactInterval/detectRadius): 일차 번호로만 읽는다 (그날은 값이 고정)
    [CreateAssetMenu(menuName = "ShelfLife/Flow/Difficulty Profile", fileName = "DifficultyProfile")]
    public class DifficultyProfile : ScriptableObject
    {
        private static readonly float[] DefaultSpawn = { 0.6f, 0.8f, 1f, 1.25f, 1.5f, 1.75f };
        private static readonly float[] DefaultHealth = { 1f, 1.3f, 1.7f, 2.2f, 2.8f, 3.5f };
        private static readonly float[] DefaultDamage = { 1f, 1.2f, 1.5f, 1.9f, 2.4f, 3f };
        private static readonly float[] DefaultSpeedRatio = { 0.65f, 0.7f, 0.75f, 0.8f, 0.84f, 0.86f };
        private static readonly float[] DefaultSpeedCap = { 0.7f, 0.75f, 0.8f, 0.85f, 0.85f, 0.85f };
        private static readonly float[] DefaultMaxAlive = { 3f, 4f, 6f, 8f, 10f, 12f };
        private static readonly float[] DefaultContactInterval = { 1.2f, 1.1f, 1f, 0.9f, 0.85f, 0.8f };
        private static readonly float[] DefaultDetectRadius = { 9f, 10f, 11f, 12f, 13f, 14f };

        [Header("배율 곡선 (1일차는 튜토리얼처럼 쉽게)")]
        [Tooltip("청크당 목표 적 수에 곱하는 밀도 배율")]
        public AnimationCurve spawn = Curve(DefaultSpawn);
        [Tooltip("적 체력 배율 (EnemyIntegrationSettings.healthScale에 곱해진다)")]
        public AnimationCurve health = Curve(DefaultHealth);
        [Tooltip("접촉 피해 배율 (EnemyIntegrationSettings.damageScale에 곱해진다. 1일차 1 = 플레이어 최대 체력의 약 5%)")]
        public AnimationCurve damage = Curve(DefaultDamage);

        [Header("일차별 값 (일차 번호로 읽는다: 1일차 = x 0)")]
        [Tooltip("일반 적의 이동 속도 ÷ 플레이어 이동 속도. 1일차 0.6~0.7, 5일차 0.85 이하")]
        public AnimationCurve speedRatio = Curve(DefaultSpeedRatio);
        [Tooltip("종류별 보정(빠른 종류)을 곱한 뒤에도 이 비율을 넘지 못한다. '빠른 적 슬롯'을 얻은 적만 예외")]
        public AnimationCurve speedCapRatio = Curve(DefaultSpeedCap);
        [Tooltip("동시에 살아 있을 수 있는 적 수")]
        public AnimationCurve maxAlive = Curve(DefaultMaxAlive);
        [Tooltip("계속 붙어 있을 때 접촉 피해를 다시 주는 간격 (초). 첫 타격 지연은 EnemyIntegrationSettings")]
        public AnimationCurve contactInterval = Curve(DefaultContactInterval);
        [Tooltip("이 거리 안에 플레이어가 들어오면 추격을 시작한다. 밖에서는 느리게 배회하거나 멈춘다 (타일)")]
        public AnimationCurve detectRadius = Curve(DefaultDetectRadius);

        [Header("밤 추가 배율 (낮은 1)")]
        [Min(0f)] public float nightSpawn = 1.4f;
        [Min(0f)] public float nightHealth = 1.15f;
        [Tooltip("일반 적 속도 비율에 곱한다. 5일차 85% 이하 규칙 때문에 기본 1")]
        [Min(0f)] public float nightSpeed = 1f;
        [Min(0f)] public float nightDamage = 1.1f;
        [Tooltip("동시 적 수 상한에 곱한다. 1일차 상한 3을 밤에도 지키려고 기본 1")]
        [Min(0f)] public float nightMaxAlive = 1f;

        [Header("빠른 적 (플레이어보다 빠를 수 있는 적)")]
        [Tooltip("이 일차부터 허용 (3 = 3일차)")]
        [Min(1)] public int fastEnemyUnlockDay = 3;
        [Tooltip("동시에 허용하는 빠른 적 수")]
        [Min(0)] public int fastEnemySlots = 1;
        [Tooltip("빠른 적의 속도 상한 (플레이어 속도 대비)")]
        [Min(0.1f)] public float fastSpeedRatio = 1.1f;

        [Header("초반 스폰")]
        [Tooltip("게임 시작(플레이 시간) 후 이 시간 동안은 적이 나오지 않는다 (초)")]
        [Min(0f)] public float spawnGraceSeconds = 25f;
        [Tooltip("이 시간(플레이 시간)이 지나기 전에는 동시에 1마리만 나온다 (초)")]
        [Min(0f)] public float singleEnemySeconds = 40f;

        [Header("플레이어 피격")]
        [Tooltip("어떤 적에게든 맞으면 이 시간 동안 무적 (깜빡임). 이 안에서는 여러 마리가 닿아도 피해 1번 (초)")]
        [Min(0f)] public float playerInvulnerableSeconds = 0.8f;

        [Header("방어")]
        [Tooltip("곡선 값이 이보다 작아도 스폰 배율은 이 값 밑으로 내려가지 않는다 (비어 있는 곡선/0 배율로 적이 영구히 안 나오는 사고 방지)")]
        [Min(0.05f)] public float minSpawnMultiplier = 0.2f;

        private void OnValidate()
        {
            EnsureCurves();
        }

        // 곡선이 비어 있으면(키 0개) Evaluate가 항상 0을 돌려줘서 적이 영구히 안 나온다. 기본값으로 채운다. 바꿨으면 true
        public bool EnsureCurves()
        {
            var changed = false;
            changed |= Fill(ref spawn, DefaultSpawn);
            changed |= Fill(ref health, DefaultHealth);
            changed |= Fill(ref damage, DefaultDamage);
            changed |= Fill(ref speedRatio, DefaultSpeedRatio);
            changed |= Fill(ref speedCapRatio, DefaultSpeedCap);
            changed |= Fill(ref maxAlive, DefaultMaxAlive);
            changed |= Fill(ref contactInterval, DefaultContactInterval);
            changed |= Fill(ref detectRadius, DefaultDetectRadius);
            return changed;
        }

        // 곡선의 모든 키(값/기울기)에 배율을 곱한다. 디버그 슬라이더 값을 굽는 데 쓴다
        public static void ScaleCurve(AnimationCurve curve, float factor)
        {
            if (curve == null || curve.length == 0) return;
            var keys = curve.keys;
            for (var i = 0; i < keys.Length; i++)
            {
                keys[i].value *= factor;
                keys[i].inTangent *= factor;
                keys[i].outTangent *= factor;
            }
            curve.keys = keys;
        }

        private static bool Fill(ref AnimationCurve curve, float[] defaults)
        {
            if (curve != null && curve.length > 0) return false;
            curve = Curve(defaults);
            return true;
        }

        private static AnimationCurve Curve(params float[] valuesPerDay)
        {
            var keys = new Keyframe[valuesPerDay.Length];
            for (var i = 0; i < keys.Length; i++) keys[i] = new Keyframe(i, valuesPerDay[i]);
            var curve = new AnimationCurve(keys);
            for (var i = 0; i < keys.Length; i++) curve.SmoothTangents(i, 0f);
            return curve;
        }
    }
}
