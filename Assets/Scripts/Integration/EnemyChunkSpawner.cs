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
    public class EnemyChunkSpawner : MonoBehaviour
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
        private int activeCount;
        private float timer;

        private bool Playing => flow != null && flow.Machine.Current == GameState.Playing;
        private bool InDanger => dangerEvents != null && dangerEvents.IsPlayerInDanger;

        private void Awake()
        {
            if (landmarks == null) landmarks = FindFirstObjectByType<LandmarkLocator>();
        }

        private void Start()
        {
            // 팀원 Enemy.OnEnable이 EnemyPooling.Instance의 SO 목록을 읽는다 (씬에 EnemyPooling이 있어야 함).
            // Awake 순서에 의존하지 않도록 모든 Awake가 끝난 Start에서 확인한다.
            if (EnemyPooling.Instance == null)
            {
                Debug.LogError("[ShelfLife] 씬에 EnemyPooling이 없습니다. Tools > ShelfLife > Install Enemies To Open Scene 을 실행하세요.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            var events = (IChunkEvents)streamer;
            events.OnChunkLoaded += OnChunkLoaded;
            events.OnChunkUnloaded += OnChunkUnloaded;
            if (dangerEvents != null) ((IDangerZoneEvents)dangerEvents).OnDangerZoneEntered += OnDangerEntered;
        }

        private void OnDisable()
        {
            var events = (IChunkEvents)streamer;
            events.OnChunkLoaded -= OnChunkLoaded;
            events.OnChunkUnloaded -= OnChunkUnloaded;
            if (dangerEvents != null) ((IDangerZoneEvents)dangerEvents).OnDangerZoneEntered -= OnDangerEntered;
        }

        private void Update()
        {
            if (!Playing) return;

            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = settings.topUpInterval;
            TopUp();
        }

        private void OnChunkLoaded(Vector2Int coord, Bounds bounds, ChunkRandom rng)
        {
            // 목표 수 채우기는 TopUp이 한다 (Playing이 아닐 때 로드된 청크도 시작하면 곧 채워진다)
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
            if (!Playing || settings.dangerBurstCount <= 0) return;

            var count = Mathf.Max(1, Mathf.RoundToInt(settings.dangerBurstCount * difficulty.SpawnMultiplier));
            for (var i = 0; i < count && activeCount < Cap(); i++)
            {
                var angle = Random.value * Mathf.PI * 2f;
                var distance = settings.minSpawnDistance + Random.value * settings.dangerBurstRadius;
                var position = (Vector2)player.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                if (TryFindChunk(position, out var coord) && !chunks[coord].Safe.Contains(position) && IsFree(position)) Spawn(position, coord);
            }
        }

        private void TopUp()
        {
            var multiplier = difficulty.SpawnMultiplier * (InDanger ? settings.dangerSpawnMultiplier : 1f);
            var perChunk = settings.enemiesPerChunk * multiplier;
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
                var target = Mathf.FloorToInt(perChunk) + (Random.value < perChunk - Mathf.Floor(perChunk) ? 1 : 0);
                if (state.Alive.Count >= target) continue;
                if (TrySpawnIn(coord, state)) budget--;
            }
        }

        private int Cap()
        {
            var scaled = Mathf.CeilToInt(settings.maxAlive * Mathf.Max(0.5f, difficulty.SpawnMultiplier));
            return Mathf.Min(settings.hardCap, scaled);
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
                if (((Vector2)player.position - position).sqrMagnitude < settings.minSpawnDistance * settings.minSpawnDistance) continue;
                if (state.Safe.Contains(position) || !IsFree(position)) continue;

                Spawn(position, coord);
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
