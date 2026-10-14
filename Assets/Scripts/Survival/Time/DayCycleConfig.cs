using UnityEngine;

namespace ExpiryGame
{
    [CreateAssetMenu(menuName = "ShelfLife/Day Cycle Config", fileName = "DayCycleConfig")]
    public class DayCycleConfig : ScriptableObject
    {
        [Min(1f)] public float dayLengthSeconds = 60f;
        [Range(0.01f, 0.99f)] public float nightStartNormalized = 0.5f;
        [Range(0f, 0.99f)] public float startTimeNormalized = 0f;
    }
}
