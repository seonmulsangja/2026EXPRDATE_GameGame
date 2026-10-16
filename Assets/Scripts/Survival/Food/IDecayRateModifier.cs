namespace ExpiryGame
{
    // 유통기한 감소 속도 배율을 제공하는 쪽(예: 냉장고 구역)이 구현한다. 1 = 그대로, 0.5 = 절반 속도.
    // 배율은 음식이 놓인 위치마다 다르다 (반경 안의 음식만 느려진다).
    public interface IDecayRateModifier
    {
        float GetDecayRateMultiplier(UnityEngine.Vector2 worldPosition);
    }
}
