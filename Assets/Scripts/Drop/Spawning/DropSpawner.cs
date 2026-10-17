using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

namespace ExpiryGame.Drop
{
    // 드롭 총괄: IDropSource의 사망을 받아 DropTable을 굴리고, 결과를 월드 드롭(풀링)으로 만든다.
    // 음식 생성은 IFoodFactory, 유통기한 감소는 IShelfLifeTracker, 획득은 IFoodInventory에 위임한다.
    public class DropSpawner : MonoBehaviour, IDropService
    {
        [SerializeField] private FoodFactory factory;
        [SerializeField] private ShelfLifeSystem tracker;
        [SerializeField] private FoodInventory collector;
        [SerializeField] private GameClock clock;
        [SerializeField] private DroppedFood pickupPrefab;
        [SerializeField] private DropFeelSettings settings;

        private readonly List<DropResult> rolled = new List<DropResult>();
        private readonly HashSet<IDropSource> subscribed = new HashSet<IDropSource>();
        private ObjectPool<DroppedFood> pool;
        private DropContext context;

        public event Action<FoodItem, Vector3> FoodDropped;
        public event Action<FoodItem> FoodCollected;

        private void Awake()
        {
            context = new DropContext
            {
                Settings = settings,
                Inventory = collector,
                Collector = collector.transform,
                Clock = clock,
            };
            pool = new ObjectPool<DroppedFood>(CreatePickup, OnGet, OnRelease, OnDestroyPickup,
                collectionCheck: false, defaultCapacity: 16, maxSize: 64);
        }

        private void OnEnable()
        {
            DropSourceRegistry.Registered += Subscribe;
            DropSourceRegistry.Unregistered += Unsubscribe;
            foreach (var source in DropSourceRegistry.Sources) Subscribe(source);
        }

        private void OnDisable()
        {
            DropSourceRegistry.Registered -= Subscribe;
            DropSourceRegistry.Unregistered -= Unsubscribe;
            foreach (var source in subscribed) source.Died -= OnSourceDied;
            subscribed.Clear();
        }

        public void Drop(DropTable table, float threat01, Vector3 position)
        {
            if (table == null) return;

            rolled.Clear();
            table.Roll(threat01, rolled);
            foreach (var result in rolled) Spawn(result, position);
        }

        private void Spawn(DropResult result, Vector3 origin)
        {
            var item = ((IFoodFactory)factory).Create(result.Food);
            item.SetShelfLife01(result.ShelfLife01);
            ((IShelfLifeTracker)tracker).Register(item);

            var offset = Random.insideUnitCircle.normalized * Random.Range(settings.scatterMinDistance, settings.scatterMaxDistance);
            var target = origin + new Vector3(offset.x, offset.y, 0f);

            var pickup = pool.Get();
            pickup.Launch(item, origin, target, context);
            item.Anchor = pickup.transform;
            FoodDropped?.Invoke(item, origin);
        }

        private void Subscribe(IDropSource source)
        {
            if (subscribed.Add(source)) source.Died += OnSourceDied;
        }

        private void Unsubscribe(IDropSource source)
        {
            if (subscribed.Remove(source)) source.Died -= OnSourceDied;
        }

        private void OnSourceDied(IDropSource source)
        {
            Drop(source.Table, source.Threat01, source.DropPosition);
        }

        private void OnPickupCollected(DroppedFood pickup)
        {
            var item = pickup.Item;
            pool.Release(pickup);
            FoodCollected?.Invoke(item);
        }

        private void OnPickupExpired(DroppedFood pickup)
        {
            ((IShelfLifeTracker)tracker).Unregister(pickup.Item);
            pool.Release(pickup);
        }

        private DroppedFood CreatePickup()
        {
            var pickup = Instantiate(pickupPrefab, transform);
            pickup.Collected += OnPickupCollected;
            pickup.Expired += OnPickupExpired;
            return pickup;
        }

        private static void OnGet(DroppedFood pickup)
        {
            pickup.gameObject.SetActive(true);
        }

        private static void OnRelease(DroppedFood pickup)
        {
            pickup.Cleanup();
            pickup.gameObject.SetActive(false);
        }

        private static void OnDestroyPickup(DroppedFood pickup)
        {
            if (pickup != null) Destroy(pickup.gameObject);
        }
    }
}
