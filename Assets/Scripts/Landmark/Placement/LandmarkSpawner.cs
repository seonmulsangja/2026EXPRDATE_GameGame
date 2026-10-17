using System.Collections.Generic;
using UnityEngine;
using ExpiryGame.Drop;
using ExpiryGame.MapGen;

namespace ExpiryGame.Landmark
{
    // 청크가 로드될 때, 랜드마크 기준점이 그 청크 안에 있으면 프리팹을 세우고 언로드되면 치운다.
    // 어디에 무엇을 놓을지는 LandmarkPlanner가, 효과는 프리팹 안의 독립 컴포넌트가 맡는다 (여기는 수명 관리뿐).
    public class LandmarkSpawner : MonoBehaviour
    {
        [SerializeField] private LandmarkSettings settings;
        [SerializeField] private ChunkStreamer streamer;
        [SerializeField] private Transform player;
        [SerializeField] private Health playerHealth;
        [SerializeField] private DropSpawner drops;
        [SerializeField] private DangerZoneEvents danger;
        [SerializeField] private FoodInventory inventory;
        [SerializeField] private LandmarkEvents events;
        [Tooltip("스폰된 랜드마크의 부모. 비어 있으면 자기 자신")]
        [SerializeField] private Transform root;

        private readonly Dictionary<Vector2Int, GameObject> spawned = new Dictionary<Vector2Int, GameObject>();
        private LandmarkPlanner planner;
        private CampfireStateStore store;

        private void Awake()
        {
            // 상태 저장소는 이 스포너와 수명이 같다: 청크가 언로드돼도 남고, 씬을 다시 불러오면(다시하기) 새로 시작한다
            store = new CampfireStateStore();
            CampfireStateStore.Active = store;
            planner = new LandmarkPlanner(settings);
            // 이전 버전 씬에는 새 참조가 비어 있을 수 있어 한 번 찾아 둔다 (설치 메뉴를 다시 실행하면 채워진다)
            if (inventory == null) inventory = FindFirstObjectByType<FoodInventory>();
            if (events == null) events = FindFirstObjectByType<LandmarkEvents>();
        }

        private void OnDestroy()
        {
            if (CampfireStateStore.Active == store) CampfireStateStore.Active = null;
        }

        private void OnEnable()
        {
            var events = (IChunkEvents)streamer;
            events.OnChunkLoaded += OnChunkLoaded;
            events.OnChunkUnloaded += OnChunkUnloaded;
        }

        private void OnDisable()
        {
            var events = (IChunkEvents)streamer;
            events.OnChunkLoaded -= OnChunkLoaded;
            events.OnChunkUnloaded -= OnChunkUnloaded;
        }

        private void OnChunkLoaded(Vector2Int chunk, Bounds bounds, ChunkRandom rng)
        {
            if (spawned.ContainsKey(chunk)) return;
            if (!planner.TryGet(planner.RegionOf(bounds.center), out var placement)) return;
            if (!bounds.Contains(new Vector3(placement.Position.x, placement.Position.y, bounds.center.z))) return;
            if (placement.Data.prefab == null) return;

            var instance = Instantiate(placement.Data.prefab, placement.Position, Quaternion.identity, root != null ? root : transform);
            instance.name = $"Landmark_{placement.Data.kind}";

            var context = new LandmarkContext
            {
                Data = placement.Data,
                Id = LandmarkId.Of(placement.Data, placement.Position),
                Store = store,
                Player = player,
                PlayerHealth = playerHealth,
                Drops = drops,
                Danger = danger,
                Inventory = inventory,
                Events = events,
            };
            foreach (var part in instance.GetComponentsInChildren<ILandmarkPart>(true)) part.Bind(context);
            spawned.Add(chunk, instance);
        }

        private void OnChunkUnloaded(Vector2Int chunk)
        {
            if (!spawned.TryGetValue(chunk, out var instance)) return;
            spawned.Remove(chunk);
            if (instance != null) Destroy(instance);
        }
    }
}
