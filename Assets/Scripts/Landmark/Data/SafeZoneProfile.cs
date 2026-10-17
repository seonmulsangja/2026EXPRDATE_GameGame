using UnityEngine;

namespace ExpiryGame.Landmark
{
    // 모닥불 안전지대 수치. 반경 안에서는 적이 새로 생기지 않고, (켜져 있으면) 들어온 적은 밀려난다.
    [CreateAssetMenu(menuName = "ShelfLife/Landmark/Safe Zone Profile", fileName = "SafeZoneProfile")]
    public class SafeZoneProfile : ScriptableObject
    {
        [Tooltip("SafeZoneRadius: 이 반경(타일) 안에는 적이 스폰되지 않는다. 반경 링(fx_zone_ring)도 이 값으로 그려진다")]
        [Min(0f)] public float radius = 4.5f;

        [Tooltip("연료 변화로 반경이 늘거나 줄 때 초당 최대 변화량 (타일). 적이 갑자기 튕겨나지 않게 한다")]
        [Min(0.1f)] public float radiusChangePerSecond = 1f;

        [Header("이미 있는 적 밀어내기 (EnemyAdapter가 KnockbackReceiver로)")]
        public bool repelEnemies = true;
        [Tooltip("한 번 밀 때 최소 거리 (타일). 경계 안쪽 깊이만큼 더 민다")]
        [Min(0f)] public float repelDistance = 1.5f;
        [Tooltip("한 적을 다시 미는 최소 간격 (초)")]
        [Min(0.05f)] public float repelIntervalSeconds = 0.25f;
    }
}
