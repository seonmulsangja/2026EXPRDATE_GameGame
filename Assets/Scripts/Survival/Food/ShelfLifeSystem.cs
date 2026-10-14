using System.Collections.Generic;
using UnityEngine;

namespace ExpiryGame
{
    public class ShelfLifeSystem : MonoBehaviour, IShelfLifeTracker
    {
        [SerializeField] private GameClock clock;

        private readonly List<FoodItem> items = new List<FoodItem>();
        private readonly List<FoodItem> snapshot = new List<FoodItem>();

        private void OnEnable()
        {
            clock.Ticked += OnTicked;
        }

        private void OnDisable()
        {
            clock.Ticked -= OnTicked;
        }

        public void Register(FoodItem item)
        {
            if (!items.Contains(item)) items.Add(item);
        }

        public void Unregister(FoodItem item)
        {
            items.Remove(item);
        }

        private void OnTicked(float deltaSeconds)
        {
            snapshot.Clear();
            snapshot.AddRange(items);
            foreach (var item in snapshot) item.Tick(deltaSeconds);
        }
    }
}
