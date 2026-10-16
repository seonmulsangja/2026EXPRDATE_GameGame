using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExpiryGame
{
    public class FoodInventory : MonoBehaviour, IFoodInventory
    {
        [SerializeField] private ShelfLifeSystem tracker;
        [SerializeField] private FoodFactory factory;
        [SerializeField] private PlayerInputReader input;
        [SerializeField, Min(1)] private int capacity = 5;

        private readonly List<FoodItem> items = new List<FoodItem>();
        private int selectedIndex;

        public IReadOnlyList<FoodItem> Items => items;
        public int Capacity => capacity;
        public FoodItem Selected => items.Count == 0 ? null : items[selectedIndex];

        public event Action Changed;

        private void OnEnable()
        {
            ((IPlayerInput)input).NextFoodPerformed += SelectNext;
        }

        private void OnDisable()
        {
            ((IPlayerInput)input).NextFoodPerformed -= SelectNext;
        }

        public bool TryAdd(FoodData data)
        {
            if (items.Count >= capacity) return false;
            return TryAdd(((IFoodFactory)factory).Create(data));
        }

        public bool TryAdd(FoodItem item)
        {
            if (items.Count >= capacity) return false;
            items.Add(item);
            item.Anchor = transform;
            ((IShelfLifeTracker)tracker).Register(item);
            Changed?.Invoke();
            return true;
        }

        public bool Remove(FoodItem item)
        {
            if (!items.Remove(item)) return false;
            item.Anchor = null;
            ((IShelfLifeTracker)tracker).Unregister(item);
            selectedIndex = items.Count == 0 ? 0 : Mathf.Min(selectedIndex, items.Count - 1);
            Changed?.Invoke();
            return true;
        }

        public void SelectNext()
        {
            if (items.Count == 0) return;
            selectedIndex = (selectedIndex + 1) % items.Count;
            Changed?.Invoke();
        }
    }
}
