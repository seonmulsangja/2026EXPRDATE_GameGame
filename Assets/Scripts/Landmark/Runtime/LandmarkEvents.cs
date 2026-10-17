using System;
using UnityEngine;

namespace ExpiryGame.Landmark
{
    public readonly struct FoodEnhancedInfo
    {
        public readonly FoodItem Food;
        public readonly Vector2 Position;
        public readonly float HealBefore;
        public readonly float HealAfter;

        public FoodEnhancedInfo(FoodItem food, Vector2 position, float healBefore, float healAfter)
        {
            Food = food;
            Position = position;
            HealBefore = healBefore;
            HealAfter = healAfter;
        }
    }

    public readonly struct FridgeOpenedInfo
    {
        public readonly Vector2 Position;

        public FridgeOpenedInfo(Vector2 position)
        {
            Position = position;
        }
    }

    public readonly struct CampfireStageInfo
    {
        public readonly Vector2 Position;
        public readonly CampfireStage From;
        public readonly CampfireStage To;

        public CampfireStageInfo(Vector2 position, CampfireStage from, CampfireStage to)
        {
            Position = position;
            From = from;
            To = to;
        }
    }

    // 소리/파티클/UI가 구독하는 쪽 (구독자는 파일이나 연출이 없어도 상관없다)
    public interface ILandmarkEvents
    {
        event Action<FoodEnhancedInfo> OnFoodEnhanced;
        event Action<FridgeOpenedInfo> OnFridgeOpened;
        event Action<CampfireStageInfo> OnCampfireStageChanged;
    }

    // 효과 컴포넌트가 발행하는 쪽
    public interface ILandmarkEventSink
    {
        void FoodEnhanced(FoodEnhancedInfo info);
        void FridgeOpened(FridgeOpenedInfo info);
        void CampfireStageChanged(CampfireStageInfo info);
    }

    // 씬에 1개 (DangerZoneEvents와 같은 방식)
    public class LandmarkEvents : MonoBehaviour, ILandmarkEvents, ILandmarkEventSink
    {
        public event Action<FoodEnhancedInfo> OnFoodEnhanced;
        public event Action<FridgeOpenedInfo> OnFridgeOpened;
        public event Action<CampfireStageInfo> OnCampfireStageChanged;

        void ILandmarkEventSink.FoodEnhanced(FoodEnhancedInfo info) => OnFoodEnhanced?.Invoke(info);
        void ILandmarkEventSink.FridgeOpened(FridgeOpenedInfo info) => OnFridgeOpened?.Invoke(info);
        void ILandmarkEventSink.CampfireStageChanged(CampfireStageInfo info) => OnCampfireStageChanged?.Invoke(info);
    }
}
