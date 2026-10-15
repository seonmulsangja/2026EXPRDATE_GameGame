using System.Collections.Generic;
using ExpiryGame.MapGen;
using UnityEngine;

namespace ExpiryGame.Loot
{
    // OnChunkLoaded를 구독해 청크마다 확률로 상자를 하나 놓고, OnChunkUnloaded에서 풀로 되돌린다.
    // 위치/확률은 청크 난수에서 Fork해서 정하므로 같은 청크는 항상 같은 자리에 상자가 생긴다.
    // 이번 플레이 중 부순 상자는 기억해서 청크를 다시 로드해도 되살리지 않는다 (저장은 안 함).
    public class LootCrateSpawner : MonoBehaviour
    {
        private const int Salt = 0xC4A7E;

        [SerializeField] private ChunkStreamer streamer;
        [SerializeField] private MapGenSettings mapSettings;
        [SerializeField] private LootCrateSettings settings;
        [SerializeField] private Transform root;

        private readonly Dictionary<Vector2Int, LootCrate> active = new Dictionary<Vector2Int, LootCrate>();
        private readonly HashSet<Vector2Int> opened = new HashSet<Vector2Int>();
        private readonly Stack<LootCrate> free = new Stack<LootCrate>();
        private readonly Collider2D[] overlapBuffer = new Collider2D[4];

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

        private void OnChunkLoaded(Vector2Int coord, Bounds bounds, ChunkRandom rng)
        {
            if (opened.Contains(coord) || active.ContainsKey(coord)) return;

            var forked = rng.Fork(Salt);
            if (forked.Value() > settings.chancePerChunk) return;

            Physics2D.SyncTransforms(); // 방금 켠 장애물 콜라이더가 겹침 검사에 잡히게
            for (var i = 0; i < settings.placementAttempts; i++)
            {
                var position = new Vector2(
                    forked.Range(bounds.min.x + settings.edgeMargin, bounds.max.x - settings.edgeMargin),
                    forked.Range(bounds.min.y + settings.edgeMargin, bounds.max.y - settings.edgeMargin));
                if (IsBlocked(position)) continue;

                Place(coord, position);
                return;
            }
        }

        private void OnChunkUnloaded(Vector2Int coord)
        {
            if (!active.TryGetValue(coord, out var crate)) return;
            active.Remove(coord);
            Release(crate);
        }

        private bool IsBlocked(Vector2 position)
        {
            if ((position - mapSettings.startPoint).magnitude < settings.startClearRadius) return true;
            return Physics2D.OverlapCircle(position, settings.overlapRadius, ContactFilter2D.noFilter, overlapBuffer) > 0;
        }

        private void Place(Vector2Int coord, Vector2 position)
        {
            var crate = free.Count > 0 ? free.Pop() : Create();
            crate.transform.position = position;
            crate.Setup(settings, coord);
            crate.gameObject.SetActive(true);
            active.Add(coord, crate);
        }

        private void OnCrateBroke(LootCrate crate)
        {
            opened.Add(crate.Chunk);
            active.Remove(crate.Chunk);
            Release(crate);
        }

        private void Release(LootCrate crate)
        {
            crate.gameObject.SetActive(false);
            free.Push(crate);
        }

        private LootCrate Create()
        {
            var go = new GameObject("LootCrate") { layer = settings.layer };
            go.transform.SetParent(root != null ? root : transform, false);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = settings.sprite;
            renderer.color = settings.tint;
            renderer.sortingOrder = settings.sortingOrder;
            if (settings.material != null) renderer.sharedMaterial = settings.material;

            var box = go.AddComponent<BoxCollider2D>();
            box.size = settings.colliderSize;
            box.offset = settings.colliderOffset;

            var crate = go.AddComponent<LootCrate>();
            crate.Broke += OnCrateBroke;
            return crate;
        }
    }
}
