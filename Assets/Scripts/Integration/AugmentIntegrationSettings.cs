using UnityEngine;

namespace ExpiryGame.Integration
{
    // 팀원 증강 SO의 수치를 우리 능력치로 옮길 때의 변환 배율. 팀원 SO(AugmentSO.value)는 건드리지 않고 여기서만 조절한다.
    // 레벨당 변화량 = AugmentSO.value × 아래 배율. (팀원 설명 문구에 맞춘 기본값: 이동 속도 레벨 × 0.5, 공격 속도 레벨 × 0.025)
    [CreateAssetMenu(menuName = "ShelfLife/Integration/Augment Integration Settings", fileName = "AugmentIntegrationSettings")]
    public class AugmentIntegrationSettings : ScriptableObject
    {
        [Header("증강 value → 레벨당 능력치 변화량")]
        [Tooltip("공격력 강화: 공격 한 번에 더해지는 피해. 팀원 설명 '레벨 × 1'")]
        public float attackPowerPerValue = 1f;
        [Tooltip("이동 속도 증가: 팀원 설명 '레벨 × 0.5'. (SO의 value는 1이라 0.5를 곱한다)")]
        public float moveSpeedPerValue = 0.5f;
        [Tooltip("공격 속도 증가: 쿨다운에서 빼는 초. 팀원 설명 '레벨 × 0.025'. (SO의 value는 1이라 0.025를 곱한다)")]
        public float attackCooldownReductionPerValue = 0.025f;

        [Header("선택 화면")]
        [Tooltip("팀원 증강 캔버스의 정렬 순서. HUD(0대)와 흐름 화면(GameFlow 캔버스 100)보다 위에 그려지도록 더 크게 둔다")]
        public int augmentCanvasSortingOrder = 150;
    }
}
