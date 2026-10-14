using UnityEngine;

namespace ExpiryGame
{
    [CreateAssetMenu(menuName = "ShelfLife/Food Data", fileName = "FoodData")]
    public class FoodData : ScriptableObject
    {
        public string displayName;
        public Sprite icon;
        [Min(1f)] public float shelfLifeSeconds = 120f;
    }
}
