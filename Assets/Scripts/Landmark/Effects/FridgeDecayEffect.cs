using UnityEngine;

namespace ExpiryGame.Landmark
{
    // 냉장고: 반경 안에 놓인 음식(가방은 플레이어 위치, 바닥 음식은 자기 위치)의 유통기한 감소에 배율을 건다.
    // 감소 로직(ShelfLifeSystem)은 IDecayRateModifier만 읽는다.
    public class FridgeDecayEffect : MonoBehaviour, IDecayRateModifier, ILandmarkPart
    {
        private LandmarkData data;

        public void Bind(LandmarkContext context)
        {
            data = context.Data;
        }

        private void OnEnable()
        {
            DecayRateRegistry.Register(this);
        }

        private void OnDisable()
        {
            DecayRateRegistry.Unregister(this);
        }

        public float GetDecayRateMultiplier(Vector2 worldPosition)
        {
            if (data == null) return 1f;
            return Vector2.Distance(worldPosition, transform.position) <= data.effectRadius ? data.decayMultiplier : 1f;
        }
    }
}
