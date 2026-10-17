using System;
using ExpiryGame.Combat;
using ExpiryGame.Drop;
using ExpiryGame.Flow;
using UnityEngine;

namespace ExpiryGame.Integration
{
    // 팀원의 Enemy(JCY)를 이 프로젝트의 계약에 맞추는 어댑터. Enemy.cs는 수정하지 않는다.
    //  - IDamageable      : 휘두르기/타격감이 때릴 수 있게 (체력은 여기서 관리, 팀원 Enemy에는 피격 처리가 없음)
    //  - IDropSource      : 죽으면 DropSpawner가 DropTable로 음식을 떨군다
    //  - IEnemyActor      : 처치 통계 / BGM 위험도
    //  - 넉백 중 이동 정지 : KnockbackReceiver가 있으면 그 동안 Enemy가 덮어쓴 속도를 0으로 되돌린다
    //  - 접촉 데미지 반복 : Enemy.OnCollisionEnter2D가 첫 1회를 주고, 붙어 있으면 contactInterval마다 다시 준다
    [RequireComponent(typeof(Enemy))]
    public class EnemyAdapter : MonoBehaviour, IDamageable, IDropSource, IEnemyActor
    {
        private Enemy enemy;
        private Rigidbody2D body;
        private IKnockbackable knockback;
        private EnemyIntegrationSettings settings;
        private Action<EnemyAdapter> release;

        private float health;
        private bool dead;
        private float nextContactTime;

        public Vector2Int HomeChunk { get; private set; }
        public float Threat01 { get; private set; }
        public DropTable Table => settings != null ? settings.dropTable : null;
        public Vector3 DropPosition => transform.position;
        public Vector3 Position => transform.position;

        public event Action<IDropSource> Died;
        public event Action<IEnemyActor> Killed;

        private void Awake()
        {
            enemy = GetComponent<Enemy>();
            body = GetComponent<Rigidbody2D>();
            knockback = GetComponent<IKnockbackable>();
        }

        private void OnEnable()
        {
            DropSourceRegistry.Register(this);
            EnemyRegistry.Register(this);
        }

        private void OnDisable()
        {
            DropSourceRegistry.Unregister(this);
            EnemyRegistry.Unregister(this);
        }

        // 스폰(또는 풀에서 재사용)할 때 호출. Enemy.OnEnable이 SO를 뽑아 기본값을 채운 "뒤"에 불러야 한다.
        public void Setup(EnemyIntegrationSettings integration, IDifficultyService difficulty, Transform target,
            Vector2Int homeChunk, Action<EnemyAdapter> onRelease)
        {
            settings = integration;
            release = onRelease;
            HomeChunk = homeChunk;
            dead = false;
            nextContactTime = 0f;

            enemy.targetTrm = target;
            Threat01 = ComputeThreat();

            health = Mathf.Max(1f, enemy._health * integration.healthScale * difficulty.HealthMultiplier);
            enemy._speed *= difficulty.SpeedMultiplier;
            enemy._damage = Mathf.Max(1, Mathf.RoundToInt(enemy._damage * integration.damageScale * difficulty.DamageMultiplier));
        }

        public void TakeDamage(float amount)
        {
            if (dead || amount <= 0f) return;
            health -= amount;
            if (health > 0f) return;

            dead = true;
            Died?.Invoke(this);
            Killed?.Invoke(this);
            Release();
        }

        // 청크 언로드 등으로 조용히 치울 때 (드롭/통계 없음)
        public void Despawn()
        {
            if (!dead) Release();
        }

        private void LateUpdate()
        {
            // Enemy.Update가 매 프레임 속도를 덮어써서 넉백(MovePosition)과 싸우므로, 넉백 중에는 이동 입력을 지운다
            if (knockback != null && knockback.IsKnockedBack && body != null) body.linearVelocity = Vector2.zero;
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (settings != null && other.gameObject.CompareTag("Player")) nextContactTime = Time.time + settings.contactInterval;
        }

        private void OnCollisionStay2D(Collision2D other)
        {
            if (dead || settings == null || Time.time < nextContactTime) return;
            if (!other.gameObject.CompareTag("Player")) return;
            if (!other.gameObject.TryGetComponent<Health>(out var playerHealth)) return;

            playerHealth.TakeDamage(enemy._damage);
            nextContactTime = Time.time + settings.contactInterval;
        }

        private void Release()
        {
            if (release != null) release(this);
            else Destroy(gameObject);
        }

        // EnemySO들 중 상대적으로 얼마나 강한 적인지 (체력/속도/데미지 가중 평균)
        private float ComputeThreat()
        {
            var pool = EnemyPooling.Instance;
            if (pool == null || pool.enemyDatas == null || pool.enemyDatas.Count == 0) return 0.5f;

            float maxHealth = 1f, maxSpeed = 1f, maxDamage = 1f;
            foreach (var data in pool.enemyDatas)
            {
                if (data == null) continue;
                maxHealth = Mathf.Max(maxHealth, data.health);
                maxSpeed = Mathf.Max(maxSpeed, data.speed);
                maxDamage = Mathf.Max(maxDamage, data.damage);
            }

            var weights = settings.threatHealthWeight + settings.threatSpeedWeight + settings.threatDamageWeight;
            if (weights <= 0f) return 0.5f;
            var score = settings.threatHealthWeight * (enemy._health / maxHealth)
                        + settings.threatSpeedWeight * (enemy._speed / maxSpeed)
                        + settings.threatDamageWeight * (enemy._damage / maxDamage);
            return Mathf.Clamp01(score / weights);
        }
    }
}
