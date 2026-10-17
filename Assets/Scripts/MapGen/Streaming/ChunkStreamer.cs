using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ExpiryGame.MapGen
{
    // 플레이어 주변 청크만 로드/언로드. 로드는 프레임당 chunksPerFrame개씩 분산한다.
    // 생성은 ChunkGenerator, 타일 반영은 ChunkTilemapView, 장애물은 ObstaclePool이 맡고
    // 이 클래스는 "어느 청크를 언제 올릴지"와 이벤트 발행만 한다.
    public class ChunkStreamer : MonoBehaviour, IChunkEvents
    {
        [SerializeField] private MapGenSettings settings;
        [SerializeField] private Transform target;
        [SerializeField] private Tilemap tilemap;
        [Tooltip("낮밤 크로스페이드용 night 바닥. 같은 Grid에 겹쳐 둔 Tilemap (비어 있으면 낮 바닥만)")]
        [SerializeField] private Tilemap nightTilemap;
        [Tooltip("장애물 오브젝트의 부모. 비어 있으면 자기 자신")]
        [SerializeField] private Transform obstacleRoot;

        private readonly Dictionary<Vector2Int, List<PooledObstacle>> loaded = new Dictionary<Vector2Int, List<PooledObstacle>>();
        private readonly Dictionary<Vector2Int, List<DecorInstance>> loadedDecor = new Dictionary<Vector2Int, List<DecorInstance>>();
        private readonly List<Vector2Int> pending = new List<Vector2Int>();
        private readonly List<Vector2Int> unloadBuffer = new List<Vector2Int>();

        private ChunkGenerator generator;
        private ChunkTilemapView view;
        private ObstaclePool obstacles;
        private DecorPool decor;
        private IObstacleFilter obstacleFilter;
        private bool hasCenter;

        public Vector2Int CurrentChunk { get; private set; }
        public IReadOnlyCollection<Vector2Int> LoadedChunks => loaded.Keys;

        public Bounds BoundsOf(Vector2Int coord)
        {
            return new Bounds(
                new Vector3(coord.x * settings.chunkSize + settings.chunkSize * 0.5f, coord.y * settings.chunkSize + settings.chunkSize * 0.5f, 0f),
                new Vector3(settings.chunkSize, settings.chunkSize, 0f));
        }

        public event Action<Vector2Int, Bounds, ChunkRandom> OnChunkLoaded;
        public event Action<Vector2Int> OnChunkUnloaded;

        private void Awake()
        {
            generator = new ChunkGenerator(settings, new SingleBiomeProvider(new DefaultBiome(settings)));
            view = new ChunkTilemapView(tilemap, settings, nightTilemap);
            obstacles = new ObstaclePool(settings, obstacleRoot != null ? obstacleRoot : transform);
            decor = new DecorPool(settings, obstacleRoot != null ? obstacleRoot : transform);
        }

        // 바이옴을 추가할 때: 새 IBiomeProvider를 만들어 여기에 넘긴다 (이미 로드된 청크는 그대로)
        public void SetBiomeProvider(IBiomeProvider provider)
        {
            generator = new ChunkGenerator(settings, provider);
        }

        // 랜드마크처럼 "이 위치는 비워 달라"는 쪽이 등록한다 (이미 로드된 청크는 그대로)
        public void SetObstacleFilter(IObstacleFilter filter)
        {
            obstacleFilter = filter;
        }

        private void Update()
        {
            var center = ChunkMath.WorldToChunk(target.position, settings.chunkSize);
            if (!hasCenter || center != CurrentChunk)
            {
                hasCenter = true;
                CurrentChunk = center;
                UnloadFarChunks();
                QueueMissingChunks();
            }
            LoadPending();
        }

        private void UnloadFarChunks()
        {
            var limit = settings.loadRadius + settings.unloadMargin;
            unloadBuffer.Clear();
            foreach (var coord in loaded.Keys)
                if (ChunkMath.ChebyshevDistance(coord, CurrentChunk) > limit) unloadBuffer.Add(coord);
            foreach (var coord in unloadBuffer) Unload(coord);
        }

        private void QueueMissingChunks()
        {
            pending.Clear();
            var r = settings.loadRadius;
            for (var dy = -r; dy <= r; dy++)
            for (var dx = -r; dx <= r; dx++)
            {
                var coord = CurrentChunk + new Vector2Int(dx, dy);
                if (!loaded.ContainsKey(coord)) pending.Add(coord);
            }
            // 가까운 청크부터 (플레이어 발밑이 먼저 채워지도록)
            pending.Sort((a, b) => (a - CurrentChunk).sqrMagnitude.CompareTo((b - CurrentChunk).sqrMagnitude));
        }

        private void LoadPending()
        {
            var budget = settings.chunksPerFrame;
            while (budget > 0 && pending.Count > 0)
            {
                var coord = pending[0];
                pending.RemoveAt(0);
                if (loaded.ContainsKey(coord)) continue;
                if (ChunkMath.ChebyshevDistance(coord, CurrentChunk) > settings.loadRadius) continue;
                Load(coord);
                budget--;
            }
        }

        private void Load(Vector2Int coord)
        {
            var data = generator.Generate(coord);
            view.Show(data);

            var spawned = new List<PooledObstacle>(data.Obstacles.Count);
            foreach (var placement in data.Obstacles)
            {
                if (obstacleFilter != null && obstacleFilter.IsBlocked(placement.Position)) continue;
                spawned.Add(obstacles.Get(placement));
            }
            loaded.Add(coord, spawned);

            // 장식은 장애물과 같은 필터(랜드마크 주변 비움)를 거친다
            var decorSpawned = new List<DecorInstance>(data.Decor.Count);
            foreach (var placement in data.Decor)
            {
                if (obstacleFilter != null && obstacleFilter.IsBlocked(placement.Position)) continue;
                decorSpawned.Add(decor.Get(placement));
            }
            loadedDecor.Add(coord, decorSpawned);

            var info = new ChunkInfo(coord, settings.chunkSize, settings.seed);
            OnChunkLoaded?.Invoke(coord, data.WorldBounds, new ChunkRandom(ChunkSeed.For(info.WorldSeed, coord, 0x5EED)));
        }

        private void Unload(Vector2Int coord)
        {
            if (!loaded.TryGetValue(coord, out var spawned)) return;
            foreach (var obstacle in spawned) obstacles.Release(obstacle);
            loaded.Remove(coord);
            if (loadedDecor.TryGetValue(coord, out var decorSpawned))
            {
                foreach (var item in decorSpawned) decor.Release(item);
                loadedDecor.Remove(coord);
            }
            view.Hide(coord);
            OnChunkUnloaded?.Invoke(coord);
        }
    }
}
