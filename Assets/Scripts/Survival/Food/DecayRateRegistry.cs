using System.Collections.Generic;

namespace ExpiryGame
{
    // IDecayRateModifier 등록소. ShelfLifeSystem은 이것만 읽고, 모디파이어를 만드는 쪽은 ShelfLifeSystem을 몰라도 된다.
    // (DropSourceRegistry와 같은 방식)
    public static class DecayRateRegistry
    {
        private static readonly List<IDecayRateModifier> modifiers = new List<IDecayRateModifier>();

        public static void Register(IDecayRateModifier modifier)
        {
            if (!modifiers.Contains(modifier)) modifiers.Add(modifier);
        }

        public static void Unregister(IDecayRateModifier modifier)
        {
            modifiers.Remove(modifier);
        }

        // 해당 위치에서 등록된 배율의 곱. 없으면 1.
        public static float Combined(UnityEngine.Vector2 worldPosition)
        {
            var result = 1f;
            for (var i = 0; i < modifiers.Count; i++) result *= modifiers[i].GetDecayRateMultiplier(worldPosition);
            return result;
        }
    }
}
