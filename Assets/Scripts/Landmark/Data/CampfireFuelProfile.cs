using UnityEngine;

namespace ExpiryGame.Landmark
{
    // 모닥불 연료의 모든 수치. 연료 상태(CampfireFuel)는 이 값만 읽고, 빛/안전지대/회복/외형은 연료 "비율"을 읽어 각자 반응한다.
    [CreateAssetMenu(menuName = "ShelfLife/Landmark/Campfire Fuel Profile", fileName = "CampfireFuelProfile")]
    public class CampfireFuelProfile : ScriptableObject
    {
        [Header("연료")]
        [Min(1f)] public float maxFuel = 100f;
        [Tooltip("플레이어가 효과 반경 안에 있을 때 초당 줄어드는 양")]
        [Min(0f)] public float drainPerSecondInside = 1.2f;
        [Tooltip("플레이어가 반경 밖일 때 초당 회복량 (언로드된 동안도 경과 시간만큼 따라잡는다)")]
        [Min(0f)] public float regenPerSecondOutside = 0.4f;
        [Tooltip("밤에 감소 속도에 곱하는 배율 (낮 1배 → 밤 이 값)")]
        [Min(0f)] public float nightDrainMultiplier = 1.6f;
        [Tooltip("꺼진 뒤 이 비율 이상으로 차면 자동으로 다시 켜진다")]
        [Range(0.01f, 1f)] public float relightThreshold01 = 0.25f;
        [Tooltip("음식 굽기(F) 1회에 드는 연료. 부족하면 굽기 불가")]
        [Min(0f)] public float enhanceFuelCost = 15f;

        [Header("단계 경계 (연료 비율). 0 초과 ~ 약함 미만 = 잉걸불, 꺼짐은 연료 0 → 재점화 임계값 전")]
        [Range(0f, 1f)] public float fullMin01 = 0.6f;
        [Range(0f, 1f)] public float weakMin01 = 0.3f;

        [Header("연료가 바닥(0%)일 때의 배율 (가득 = 1). 꺼지면 효과 자체가 0")]
        [Range(0f, 1f)] public float lightRadiusAtEmpty = 0.35f;
        [Range(0f, 1f)] public float lightIntensityAtEmpty = 0.25f;
        [Range(0f, 1f)] public float safeRadiusAtEmpty = 0.4f;
        [Range(0f, 1f)] public float healAtEmpty = 0.3f;
        [Tooltip("빛 반경/밝기가 목표 배율을 따라가는 속도 (배율/초)")]
        [Min(0.1f)] public float lightFollowSpeed = 1.5f;

        [Header("외형")]
        [Min(0f)] public float stageFadeSeconds = 0.4f;

        [Header("연료 게이지 (월드 스프라이트)")]
        [Range(0f, 1f)] public float gaugeShowBelow01 = 0.5f;
        [Range(0f, 1f)] public float gaugeFlashBelow01 = 0.2f;
        [Min(0.5f)] public float gaugeFlashHz = 3f;
        [Tooltip("모닥불 발밑 기준 높이 (타일)")]
        [Min(0f)] public float gaugeHeight = 2.1f;
        [Min(0.2f)] public float gaugeWidth = 1.3f;

        public float Ratio(float fuel)
        {
            return maxFuel > 0f ? Mathf.Clamp01(fuel / maxFuel) : 0f;
        }

        public CampfireStage StageFor(float ratio01, bool lit)
        {
            if (!lit) return CampfireStage.Out;
            if (ratio01 >= fullMin01) return CampfireStage.Full;
            if (ratio01 >= weakMin01) return CampfireStage.Weak;
            return CampfireStage.Embers;
        }
    }
}
