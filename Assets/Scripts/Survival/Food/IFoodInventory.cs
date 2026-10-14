using System;
using System.Collections.Generic;

namespace ExpiryGame
{
    public interface IFoodInventory
    {
        IReadOnlyList<FoodItem> Items { get; }
        FoodItem Selected { get; }
        int Capacity { get; }

        bool TryAdd(FoodItem item);
        bool TryAdd(FoodData data);
        bool Remove(FoodItem item);
        void SelectNext();

        event Action Changed;
    }
}
