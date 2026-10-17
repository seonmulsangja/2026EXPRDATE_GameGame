using System.Collections.Generic;
using UnityEngine;

namespace ExpiryGame.Flow
{
    // 시작 음식을 인벤토리 슬롯 1, 2, 3... 순서로 지급한다. 스탯은 FoodFactory가 유통기한에서 자동 계산한다.
    public class LoadoutGiver
    {
        private readonly List<FoodData> clones = new List<FoodData>();

        public void Give(IFoodInventory inventory, StartingLoadout loadout)
        {
            if (loadout == null) return;
            foreach (var entry in loadout.entries)
            {
                if (entry.food == null) continue;
                if (!inventory.TryAdd(Resolve(entry))) Debug.LogWarning($"[ShelfLife] 가방이 가득 차 시작 음식 {entry.food.name}을 지급하지 못했습니다.");
            }
        }

        // 유통기한을 덮어쓴 항목은 FoodData 복사본을 쓴다 (원본 SO는 드롭 표와 공유하므로 건드리지 않는다)
        private FoodData Resolve(LoadoutEntry entry)
        {
            if (entry.shelfLifeSeconds <= 0f || Mathf.Approximately(entry.shelfLifeSeconds, entry.food.shelfLifeSeconds)) return entry.food;
            var copy = Object.Instantiate(entry.food);
            copy.name = entry.food.name;
            copy.shelfLifeSeconds = entry.shelfLifeSeconds;
            clones.Add(copy);
            return copy;
        }

        public void Release()
        {
            foreach (var clone in clones)
            {
                if (clone == null) continue;
                if (Application.isPlaying) Object.Destroy(clone);
                else Object.DestroyImmediate(clone);
            }
            clones.Clear();
        }
    }
}
