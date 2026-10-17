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
    //  - IExperienceReward: 처치 경험치 (종류별 지급량은 EnemyIntegrationSettings)
    //  - 속도 : 플레이어 속도 대비 비율(DifficultyProfile)로 매 프레임 정한다. 빠른 종류는 "빠른 적 슬롯"을 얻어야 상한을 넘는다
    //  - 추격 : 감지 반경 안에서만 플레이어를 쫓고, 밖에서는 느리게 배회하거나 멈춘다 (Enemy.targetTrm을 비우면 팀원 이동이 멈춘다)
    //  - 접촉 : 닿으면 firstHitDelay 동안 멈추고 번쩍인 뒤 첫 타격, 이후 contactInterval마다. 피해는 플레이어의 무적 시간을 지킨다
    //           팀원 Enemy.OnCollisionEnter2D는 닿자마자 _damage를 주므로 _damage를 0으로 만들어 무력화하고(Health는 0 이하를 무시) 피해는 여기서 준다
    //  - 넉백 중 이동 정지 : KnockbackReceiver가 있으면 그 동안(밀림 + 경직) Enemy가 덮어쓴 속도를 0으로 되돌리고 접촉 데미지도 주지 않는다
    //  - 넉백 저항 : knockbackResistance(0~1)를 EnemySO(같은 이름의 필드가 있을 때) → 프리팹 값 순으로 읽어 리시버에 넣는다. 없으면 0
    //  - 안전지대 : 모닥불 반경 안으로 들어온 적을 KnockbackReceiver로 밀어낸다 (SafeZoneProfile.repelEnemies로 끈다)
    [RequireComponent(typeof(Enemy))]
    public class EnemyAdapter : MonoBehaviour, IDamageable, IDropSource, IEnemyActor, IExperienceReward
    {
        private const string ResistanceFieldName = "knockbackResistance";
        private const float DefaultPlayerSpeed = 5f;

        [Tooltip("0~1. 1이면 넉백 면역. EnemySO에 knockbackResistance 필드가 있으면 그 값이 우선")]
        [Range(0f, 1f)] [SerializeField] private float knockbackResistance;

        private Enemy enemy;
        private Rigidbody2D body;
        private IKnockbackable knockback;
        private IFlashable flashable;
        private EnemyIntegrationSettings settings;
        private IDifficultyService difficulty;
        private Action<EnemyAdapter> release;
        private Transform target;
        private PlayerMover playerMover;

        private float health;
        private bool dead;
        private float baseContactDamage;
        private float kindSpeedFactor = 1f;
        private int experienceReward;
        private float nextRepelTime;

        // 접촉
        private bool touching;
        private GameObject touchedObject;
        private float nextContactTime;
        private float telegraphUntil;

        // 추격 / 배회
        private bool chasing;
        private bool wanderMoving;
        private Vector2 wanderDirection;
        private float wanderChangeAt;

        public Vector2Int HomeChunk { get; private set; }
        public float Threat01 { get; private set; }
        public int ExperienceReward => experienceReward;
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
            flashable = GetComponent<IFlashable>();
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
            difficulty?.SpeedPolicy?.Release(this);
            touching = false;
        }

        // 스폰(또는 풀에서 재사용)할 때 호출. Enemy.OnEnable이 SO를 뽑아 기본값을 채운 "뒤"에 불러야 한다.
        public void Setup(EnemyIntegrationSettings integration, IDifficultyService difficultyService, Transform playerTarget,
            Vector2Int homeChunk, Action<EnemyAdapter> onRelease)
        {
            settings = integration;
            difficulty = difficultyService;
            release = onRelease;
            target = playerTarget;
            playerMover = playerTarget != null ? playerTarget.GetComponent<PlayerMover>() : null;
            HomeChunk = homeChunk;
            dead = false;
            touching = false;
            touchedObject = null;
            nextContactTime = 0f;
            telegraphUntil = 0f;
            chasing = false;
            wanderMoving = false;
            wanderChangeAt = Time.time + UnityEngine.Random.Range(0f, integration.wanderPauseMaxSeconds);

            if (knockback != null) knockback.Resistance = ResolveKnockbackResistance();
            Threat01 = ComputeThreat();

            health = Mathf.Max(1f, enemy._health * integration.healthScale * difficultyService.HealthMultiplier);
            // 접촉 피해는 맞는 순간의 난이도 배율로 계산한다 (디버그 슬라이더가 바로 반영되고, 오래 산 적도 그날 값을 쓴다)
            baseContactDamage = Mathf.Max(0.1f, enemy._damage * integration.damageScale);
            enemy._damage = 0;
            kindSpeedFactor = Mathf.Pow(Mathf.Max(0.1f, enemy._speed / integration.referenceEnemySpeed), integration.kindSpeedExponent);
            experienceReward = integration.ExperienceFor(enemy._enemyName);

            enemy.targetTrm = null; // 감지 반경에 들어오면 Update가 켠다
            enemy._speed = ComputeSpeed();
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

        // 청크 언로드 등으로 조용히 치울 때 (드롭/통계/경험치 없음)
        public void Despawn()
        {
            if (!dead) Release();
        }

        private void Update()
        {
            if (dead || settings == null || difficulty == null) return;

            RepelFromSafeZone();
            enemy._speed = ComputeSpeed();
            UpdateChase();
            TickContact();
        }

        private void LateUpdate()
        {
            // Enemy.Update가 매 프레임 속도를 덮어써서 넉백(MovePosition)과 싸우므로, 넉백 중에는 이동 입력을 지운다.
            // 접촉 예고 중에도 멈춰 서 있는다
            if (body == null) return;
            var frozen = Time.time < telegraphUntil;
            if (frozen || (knockback != null && knockback.IsKnockedBack)) body.linearVelocity = Vector2.zero;
        }

        // ---------- 속도 ----------

        private float ComputeSpeed()
        {
            var playerSpeed = playerMover != null ? playerMover.MoveSpeed : DefaultPlayerSpeed;
            var policy = difficulty.SpeedPolicy;
            var ratio = policy != null ? policy.Resolve(kindSpeedFactor, this) : difficulty.EnemySpeedRatio * kindSpeedFactor;
            return playerSpeed * ratio * difficulty.SpeedMultiplier;
        }

        // ---------- 추격 / 배회 ----------

        private void UpdateChase()
        {
            if (target == null) return;

            var sqr = ((Vector2)target.position - (Vector2)transform.position).sqrMagnitude;
            var detect = difficulty.DetectRadius;
            var lose = detect * settings.loseRadiusMultiplier;
            if (!chasing && sqr <= detect * detect) chasing = true;
            else if (chasing && sqr > lose * lose) chasing = false;

            var frozen = Time.time < telegraphUntil;
            enemy.targetTrm = chasing && !frozen ? target : null;
            if (!chasing && !frozen) Wander();
        }

        // 걷다가 멈추기를 반복한다. 팀원 Enemy는 targetTrm이 비면 속도를 건드리지 않으므로 여기서 직접 정한다
        private void Wander()
        {
            if (body == null) return;

            if (Time.time >= wanderChangeAt)
            {
                wanderMoving = !wanderMoving;
                if (wanderMoving)
                {
                    wanderDirection = UnityEngine.Random.insideUnitCircle.normalized;
                    wanderChangeAt = Time.time + UnityEngine.Random.Range(settings.wanderMoveMinSeconds, settings.wanderMoveMaxSeconds);
                }
                else
                {
                    wanderChangeAt = Time.time + UnityEngine.Random.Range(settings.wanderPauseMinSeconds, settings.wanderPauseMaxSeconds);
                }
            }

            body.linearVelocity = wanderMoving ? wanderDirection * (enemy._speed * settings.wanderSpeedRatio) : Vector2.zero;

            if (wanderMoving && Mathf.Abs(wanderDirection.x) > 0.01f)
            {
                var scale = transform.localScale;
                scale.x = wanderDirection.x > 0f ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
                transform.localScale = scale;
            }
        }

        // ---------- 접촉 공격 ----------

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (!IsPlayer(other)) return;
            BeginTouch(other.gameObject);
        }

        private void OnCollisionStay2D(Collision2D other)
        {
            // 활성화된 채로 이미 겹쳐 있던 경우 등, Enter를 놓쳤을 때를 위한 보험
            if (!touching && !dead && IsPlayer(other)) BeginTouch(other.gameObject);
        }

        private void OnCollisionExit2D(Collision2D other)
        {
            if (!IsPlayer(other)) return;
            touching = false;
            touchedObject = null;
            telegraphUntil = 0f; // 떨어지면 예고를 접고 다시 쫓는다. 다음에 닿으면 처음부터
        }

        private static bool IsPlayer(Collision2D other) => other.gameObject.CompareTag("Player");

        private void BeginTouch(GameObject player)
        {
            if (settings == null || dead) return;
            touching = true;
            touchedObject = player;
            nextContactTime = Time.time + settings.firstHitDelay;
            telegraphUntil = nextContactTime;
            if (flashable != null && settings.telegraphFlashSeconds > 0f) flashable.Flash(settings.telegraphFlashSeconds);
        }

        // 물리 Stay 콜백은 정지한 물체에서 끊길 수 있어서 시간 기준은 Update에서 본다
        private void TickContact()
        {
            if (!touching || Time.time < nextContactTime) return;
            if (touchedObject == null) { touching = false; return; }
            if (knockback != null && knockback.IsKnockedBack) return; // 맞아서 경직 중에는 바로 반격하지 못한다

            nextContactTime = Time.time + difficulty.ContactInterval; // 무적에 막혀도 공격은 한 것으로 본다
            var damage = Mathf.Max(1f, baseContactDamage * difficulty.DamageMultiplier);

            if (touchedObject.TryGetComponent<IContactDamageReceiver>(out var receiver)) receiver.TryHit(damage);
            else if (touchedObject.TryGetComponent<Health>(out var playerHealth)) playerHealth.TakeDamage(damage);
        }

        // ---------- 안전지대 / 기타 ----------

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
            difficulty?.SpeedPolicy?.Release(this);
            touching = false;
            if (release != null) release(this);
            else Destroy(gameObject);
        }

        // EnemySO들 중 상대적으로 얼마나 강한 적인지 (체력/속도/데미지 가중 평균). Setup에서 _damage를 0으로 만들기 전에 계산한다
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
