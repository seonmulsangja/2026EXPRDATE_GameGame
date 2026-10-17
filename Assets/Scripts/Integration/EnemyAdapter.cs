using System;
using ExpiryGame.Combat;
using ExpiryGame.Drop;
using ExpiryGame.Flow;
using ExpiryGame.Landmark;
using UnityEngine;

namespace ExpiryGame.Integration
{
    // 팀원의 Enemy(JCY)를 이 프로젝트의 계약에 맞추는 어댑터. Enemy.cs는 수정하지 않는다.
    //  - IDamageable      : 휘두르기/타격감이 때릴 수 있게 (체력은 여기서 관리, 팀원 Enemy에는 피격 처리가 없음)
    //  - IDropSource      : 죽으면 DropSpawner가 DropTable로 음식을 떨군다
    //  - IEnemyActor      : 처치 통계 / BGM 위험도
    //  - 넉백 중 이동 정지 : KnockbackReceiver가 있으면 그 동안(밀림 + 경직) Enemy가 덮어쓴 속도를 0으로 되돌리고 접촉 데미지도 주지 않는다
    //  - 넉백 저항 : knockbackResistance(0~1)를 EnemySO(같은 이름의 필드가 있을 때) → 프리팹 값 순으로 읽어 리시버에 넣는다. 없으면 0
    //  - 안전지대 : 모닥불 반경 안으로 들어온 적을 KnockbackReceiver로 밀어낸다 (SafeZoneProfile.repelEnemies로 끈다)
    //  - 접촉 데미지 반복 : Enemy.OnCollisionEnter2D가 첫 1회를 주고, 붙어 있으면 contactInterval마다 다시 준다
    [RequireComponent(typeof(Enemy))]
    public class EnemyAdapter : MonoBehaviour, IDamageable, IDropSource, IEnemyActor
    {
        private const string ResistanceFieldName = "knockbackResistance";

        [Tooltip("0~1. 1이면 넉백 면역. EnemySO에 knockbackResistance 필드가 있으면 그 값이 우선")]
        [Range(0f, 1f)] [SerializeField] private float knockbackResistance;

        private Enemy enemy;
        private Rigidbody2D body;
        private IKnockbackable knockback;
        private EnemyIntegrationSettings settings;
        private Action<EnemyAdapter> release;

        private float health;
        private bool dead;
        private float nextContactTime;
        private float nextRepelTime;

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
            if (knockback != null) knockback.Resistance = ResolveKnockbackResistance();
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

        private void Update()
        {
            RepelFromSafeZone();
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
            if (knockback != null && knockback.IsKnockedBack) return; // 맞아서 경직 중에는 바로 반격하지 못한다
            if (!other.gameObject.CompareTag("Player")) return;
            if (!other.gameObject.TryGetComponent<Health>(out var playerHealth)) return;

            playerHealth.TakeDamage(enemy._damage);
            nextContactTime = Time.time + settings.contactInterval;
        }

        // 안전지대 안이면 중심 반대 방향으로 민다. 경계에서 얼마나 안쪽인지만큼 더 밀어 확실히 밖으로 내보낸다.
        private void RepelFromSafeZone()
        {
            if (dead || knockback == null || Time.time < nextRepelTime) return;
            if (!SafeZoneRegistry.TryFind(transform.position, out var zone) || !zone.RepelEnemies) return;

            nextRepelTime = Time.time + zone.RepelIntervalSeconds;
            if (knockback.IsKnockedBack) return;

            var away = (Vector2)transform.position - zone.Center;
            var depth = zone.Radius - away.magnitude;
            if (away.sqrMagnitude < 0.0001f) away = UnityEngine.Random.insideUnitCircle;
            var repel = KnockbackProfile.ResolveSafeZone(settings != null ? settings.safeZoneKnockback : null);
            knockback.ApplyKnockback(away, Mathf.Max(zone.RepelDistance, depth + 0.25f), repel);
        }

        // EnemySO(팀원 파일, 수정하지 않음)에 knockbackResistance 필드가 생기면 읽고, 없으면 프리팹 값(기본 0)
        private float ResolveKnockbackResistance()
        {
            var pool = EnemyPooling.Instance;
            if (pool != null && pool.enemyDatas != null)
            {
                foreach (var data in pool.enemyDatas)
                {
                    if (data == null || data.enemyName != enemy._enemyName) continue;
                    var field = data.GetType().GetField(ResistanceFieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (field != null && field.FieldType == typeof(float)) return Mathf.Clamp01((float)field.GetValue(data));
                    break;
                }
            }
            return Mathf.Clamp01(knockbackResistance);
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
