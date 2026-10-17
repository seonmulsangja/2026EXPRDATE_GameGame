using System;
using UnityEngine;

namespace ExpiryGame.Flow
{
    [Serializable]
    public struct LoadoutEntry
    {
        public FoodData food;
        [Tooltip("0이면 FoodData의 유통기한을 그대로 쓴다. 값을 넣으면 이 유통기한(초)을 최대치로 한 복사본을 지급한다 (스탯은 이 값으로 자동 계산)")]
        [Min(0f)] public float shelfLifeSeconds;
    }

    // 시작 시 슬롯 1, 2, 3 ... 순서로 지급하는 음식. 항상 신선도 100%로 시작한다.
    [CreateAssetMenu(menuName = "ShelfLife/Flow/Starting Loadout", fileName = "StartingLoadout")]
    public class StartingLoadout : ScriptableObject
    {
        public LoadoutEntry[] entries = Array.Empty<LoadoutEntry>();
    }
}
