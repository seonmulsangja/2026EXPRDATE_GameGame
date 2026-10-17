using System.Collections.Generic;
using ExpiryGame.Flow;
using ExpiryGame.Landmark;
using ExpiryGame.MapGen;
using UnityEngine;

namespace ExpiryGame.Integration
{
    // 청크 이벤트(OnChunkLoaded/Unloaded)에 연결한 적 스포너. 팀원의 EnemySpawner_JCY/EnemyPooling 풀은 쓰지 않는다
    // (고정 스폰 포인트 + 풀 push가 서로 얽혀 있어서). 대신 팀원 Enemy 프리팹만 가져와 청크 단위로 내보내고 치운다.
    //  - 플레이어 주변에 로드된 청크에서만 스폰, 언로드되면 그 청크 출신 적은 조용히 치운다 (드롭/통계 없음)
    //  - 난이도 배율(IDifficultyService)이 목표 수를 정하고, 위험 구역 안에서는 더 늘린다
    //  - 위험 구역 진입 순간에는 플레이어 주변으로 추가 인원을 내보낸다
    //  - 모닥불 안전지대(SafeZoneProfile.radius) 안에는 스폰하지 않는다. 청크가 생길 때 "이미 정해진 랜드마크 배치"로 판단한다
    public class EnemyChunkSpawner : MonoBehaviour, ISpawnDebug
    {
        private sealed class ChunkState
        {
            public Bounds Bounds;
            public SafeZoneSet Safe = SafeZoneSet.Empty;
            public readonly List<EnemyAdapter> Alive = new List<EnemyAdapter>();
        }

        [SerializeField] private ChunkStreamer streamer;
        [SerializeField] private EnemyIntegrationSettings settings;
        [SerializeField] private DifficultyService difficulty;
        [SerializeField] private GameFlowRoot flow;
        [SerializeField] private Transform player;
        [SerializeField] private DangerZoneEvents dangerEvents;
        [Tooltip("모닥불 안전지대 판단용 (배치 정보만 읽는다). 비어 있으면 씬에서 찾는다")]
        [SerializeField] private LandmarkLocator landmarks;
        [Tooltip("적 오브젝트의 부모. 비어 있으면 자기 자신")]
        [SerializeField] private Transform root;

        private readonly Dictionary<Vector2Int, ChunkState> chunks = new Dictionary<Vector2Int, ChunkState>();
        private readonly Stack<EnemyAdapter> pool = new Stack<EnemyAdapter>();
        private readonly List<Vector2Int> order = new List<Vector2Int>();
        private readonly List<EnemyAdapter> buffer = new List<EnemyAdapter>();
        private readonly Collider2D[] overlap = new Collider2D[4];
        private readonly List<LandmarkInfo> nearbyLandmarks = new List<LandmarkInfo>();
        private readonly SpawnDiagnostics diagnostics = new SpawnDiagnostics();
        private int activeCount;
        private float timer;
        private bool wired;

        private bool Playing => flow != null && flow.Machine.Current == GameState.Playing;
        public int ActiveEnemies => activeCount;
        public int MaxEnemies => wired ? Cap() : 0;
        public int LoadedChunks => chunks.Count;
        public bool Paused { get; set; }
        public float SpawnMultiplier => difficulty != null ? difficulty.SpawnMultiplier : 0f;
        public SpawnDiagnostics Diagnostics => diagnostics;

        private bool InDanger => dangerEvents != null && dangerEvents.IsPlayerInDanger;

        private void Awake()
        {
            if (landmarks == null) landmarks = FindFirstObjectByType<LandmarkLocator>();
        }

        private void Start()
        {
            wired = ValidateWiring();

            // 팀원 Enemy.OnEnable이 EnemyPooling.Instance의 SO 목록을 읽는다 (씬에 EnemyPooling이 있어야 함).
            // Awake 순서에 의존하지 않도록 모든 Awake가 끝난 Start에서 확인한다.
            if (EnemyPooling.Instance == null)
            {
                Debug.LogError("[ShelfLife] 씬에 EnemyPooling이 없습니다. Tools > ShelfLife > Install Enemies To Open Scene 을 실행하세요.", this);
                enabled = false;
            }
        }

        // 필수 참조가 하나라도 비면 스폰이 조용히 아무것도 안 한다. 무엇이 비었는지 한 번에 알려 준다.
        private bool ValidateWiring()
        {
            var missing = new List<string>();
            if (streamer == null) missing.Add("streamer(ChunkStreamer)");
            if (settings == null) missing.Add("settings(EnemyIntegrationSettings)");
            else if (settings.enemyPrefab == null) missing.Add("settings.enemyPrefab");
            if (difficulty == null) missing.Add("difficulty(DifficultyService)");
            if (flow == null) missing.Add("flow(GameFlowRoot)");
            if (player == null) missing.Add("player");
            if (missing.Count == 0) return true;

            Debug.LogError("[ShelfLife] EnemyChunkSpawner 참조가 비어 있어 스폰하지 않습니다: " + string.Join(", ", missing)
                           + "  -> Tools > ShelfLife > Setup Game (All In One) 또는 Install Enemies To Open Scene 을 실행하세요.", this);
            return false;
        }

        private void OnEnable()
        {
            if (streamer == null) return;
            var events = (IChunkEvents)streamer;
            events.OnChunkLoaded += OnChunkLoaded;
            events.OnChunkUnloaded += OnChunkUnloaded;
            if (dangerEvents != null) ((IDangerZoneEvents)dangerEvents).OnDangerZoneEntered += OnDangerEntered;

            // 구독 전에 이미 로드된 청크(시작 지역)를 소급 처리한다. 이벤트만 기다리면 그 청크는 영영 비어 있다.
            events.ForEachLoaded(AddChunk);
        }

        private void OnDisable()
        {
            if (streamer == null) return;
            var events = (IChunkEvents)streamer;
            events.OnChunkLoaded -= OnChunkLoaded;
            events.OnChunkUnloaded -= OnChunkUnloaded;
            if (dangerEvents != null) ((IDangerZoneEvents)dangerEvents).OnDangerZoneEntered -= OnDangerEntered;
        }

        private void Update()
        {
            if (!wired) return;

            // 일시정지(timeScale 0)에서도 진단이 돌도록 실제 시간으로 센다. 스폰 여부는 Tick 안에서 상태를 보고 정한다
            timer -= Time.unscaledDeltaTime;
            if (timer > 0f) return;
            timer = settings.topUpInterval;
            Tick();
        }

        private void OnChunkLoaded(Vector2Int coord, Bounds bounds, ChunkRandom rng)
        {
            AddChunk(coord, bounds);
        }

        // 목표 수 채우기는 Tick이 한다 (Playing이 아닐 때 로드된 청크도 시작하면 곧 채워진다)
        private void AddChunk(Vector2Int coord, Bounds bounds)
        {
            if (chunks.ContainsKey(coord)) return;
            chunks[coord] = new ChunkState { Bounds = bounds, Safe = FindSafeZones(bounds) };
            if (Playing) timer = Mathf.Min(timer, 0.1f);
        }

        private void OnChunkUnloaded(Vector2Int coord)
        {
            if (!chunks.TryGetValue(coord, out var state)) return;
            buffer.Clear();
            buffer.AddRange(state.Alive);
            foreach (var enemy in buffer) enemy.Despawn();
            chunks.Remove(coord);
        }

        private void OnDangerEntered(DangerZoneInfo info)
        {
            if (!wired || Paused || !Playing || settings.dangerBurstCount <= 0) return;

            var count = Mathf.Max(1, Mathf.RoundToInt(settings.dangerBurstCount * difficulty.SpawnMultiplier));
            for (var i = 0; i < count && activeCount < Cap(); i++)
            {
                var angle = Random.value * Mathf.PI * 2f;
                var distance = settings.minSpawnDistance + Random.value * settings.dangerBurstRadius;
                var position = (Vector2)player.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                if (TryFindChunk(position, out var coord) && !chunks[coord].Safe.Contains(position) && IsFree(position))
                {
                    Spawn(position, coord);
                    diagnostics.RecordSpawn("위험 구역 진입");
                }
            }
        }

        // 한 번의 보충. 막히면 이유를 세고 돌아간다 (어디서 막히는지가 진단의 전부)
        private void Tick()
        {
            diagnostics.RecordTick();
            if (Paused) { diagnostics.RecordBlocked(SpawnBlockReason.Paused); return; }
            if (!Playing) { diagnostics.RecordBlocked(SpawnBlockReason.NotPlaying, flow.Machine.Current.ToString()); return; }
            if (settings.enemyPrefab == null) { diagnostics.RecordBlocked(SpawnBlockReason.NoPrefab); return; }
            if (chunks.Count == 0) { diagnostics.RecordBlocked(SpawnBlockReason.NoChunks); return; }

            var multiplier = difficulty.SpawnMultiplier * (InDanger ? settings.dangerSpawnMultiplier : 1f);
            var perChunk = settings.enemiesPerChunk * multiplier;
            if (perChunk <= 0f) { diagnostics.RecordBlocked(SpawnBlockReason.DifficultyZero, $"x{multiplier:0.##}"); return; }
            if (activeCount >= Cap()) { diagnostics.RecordBlocked(SpawnBlockReason.Cap, $"{activeCount}/{Cap()}"); return; }

            TopUp(perChunk);
        }

        private void TopUp(float perChunk)
        {
            var cap = Cap();
            var budget = settings.spawnsPerTick;

            // 매번 같은 청크부터 채우지 않도록 순서를 섞는다
            order.Clear();
            order.AddRange(chunks.Keys);
            for (var i = order.Count - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            foreach (var coord in order)
            {
                if (budget <= 0 || activeCount >= cap) break;
                var state = chunks[coord];
                var target = SpawnMath.ChunkTarget(perChunk, Random.value);
                if (state.Alive.Count >= target) continue;
                if (TrySpawnIn(coord, state)) budget--;
                else diagnostics.RecordBlocked(SpawnBlockReason.NoCandidate, $"청크 {coord}");
            }
        }

        private int Cap()
        {
            return SpawnMath.Cap(settings.maxAlive, settings.hardCap, difficulty.SpawnMultiplier);
        }

        private bool TrySpawnIn(Vector2Int coord, ChunkState state)
        {
            var bounds = state.Bounds;
            var margin = settings.edgeMargin;
            for (var i = 0; i < settings.spawnAttempts; i++)
            {
                var position = new Vector2(
                    Random.Range(bounds.min.x + margin, bounds.max.x - margin),
                    Random.Range(bounds.min.y + margin, bounds.max.y - margin));
                if (((Vector2)player.position - position).sqrMagnitude < settings.minSpawnDistance * settings.minSpawnDistance)
                {
                    diagnostics.RecordBlocked(SpawnBlockReason.TooClose);
                    continue;
                }
                if (state.Safe.Contains(position)) { diagnostics.RecordBlocked(SpawnBlockReason.SafeZone); continue; }
                if (!IsFree(position)) { diagnostics.RecordBlocked(SpawnBlockReason.Obstacle); continue; }

                Spawn(position, coord);
                diagnostics.RecordSpawn($"청크 {coord}");
                return true;
            }
            return false;
        }

        // 청크 주변의 모닥불 배치를 안전 구역으로 모아 둔다 (로케이터가 이웃 지역까지 찾아 준다)
        private SafeZoneSet FindSafeZones(Bounds bounds)
        {
            if (landmarks == null) return SafeZoneSet.Empty;
            ((ILandmarkLocator)landmarks).GetNearby(bounds.center, nearbyLandmarks);
            return SafeZoneSet.From(nearbyLandmarks);
        }

        private bool IsFree(Vector2 position)
        {
            return Physics2D.OverlapCircle(position, settings.obstacleCheckRadius, ContactFilter2D.noFilter, overlap) == 0;
        }

        private bool TryFindChunk(Vector2 position, out Vector2Int coord)
        {
            foreach (var pair in chunks)
            {
                var bounds = pair.Value.Bounds;
                if (position.x >= bounds.min.x && position.x < bounds.max.x && position.y >= bounds.min.y && position.y < bounds.max.y)
                {
                    coord = pair.Key;
                    return true;
                }
            }
            coord = default;
            return false;
        }

        // 디버그: 플레이어 근처에 한 마리. 상태/정지/상한/안전지대는 무시하고 프리팹과 청크만 필요하다
        public bool ForceSpawnNearPlayer()
        {
            if (!wired || settings.enemyPrefab == null) { diagnostics.RecordBlocked(SpawnBlockReason.NoPrefab, "강제 스폰"); return false; }
            if (chunks.Count == 0) { diagnostics.RecordBlocked(SpawnBlockReason.NoChunks, "강제 스폰"); return false; }

            for (var i = 0; i < 12; i++)
            {
                var angle = Random.value * Mathf.PI * 2f;
                var position = (Vector2)player.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(3f, 5f);
                if (!TryFindChunk(position, out var coord) || !IsFree(position)) continue;
                Spawn(position, coord);
                diagnostics.RecordSpawn("강제 스폰");
                return true;
            }
            diagnostics.RecordBlocked(SpawnBlockReason.NoCandidate, "강제 스폰");
            return false;
        }

        private void Spawn(Vector2 position, Vector2Int coord)
        {
            EnemyAdapter adapter;
            if (pool.Count > 0)
            {
                adapter = pool.Pop();
                adapter.transform.position = position;
                adapter.gameObject.SetActive(true);
            }
            else
            {
                var instance = Instantiate(settings.enemyPrefab, position, Quaternion.identity, root != null ? root : transform);
                adapter = instance.GetComponent<EnemyAdapter>();
            }

            // Enemy.OnEnable(SO 뽑기)이 끝난 뒤에 배율을 적용한다
            adapter.Setup(settings, difficulty, player, coord, Release);
            chunks[coord].Alive.Add(adapter);
            activeCount++;
        }

        private void Release(EnemyAdapter adapter)
        {
            if (chunks.TryGetValue(adapter.HomeChunk, out var state)) state.Alive.Remove(adapter);
            activeCount = Mathf.Max(0, activeCount - 1);
            adapter.gameObject.SetActive(false);
            pool.Push(adapter);
        }
    }
}
