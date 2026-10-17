using UnityEngine;

namespace ExpiryGame.Landmark
{
    // 모닥불을 안전지대로 등록한다. 수치는 LandmarkData.safeZone(SO)에서 읽고, 판단은 SafeZoneRegistry를 읽는 쪽이 한다.
    public class CampfireSafeZone : MonoBehaviour, ILandmarkPart, ISafeZone
    {
        private SafeZoneProfile profile;
        private CampfireFuel fuel;
        private float current;
        private bool snapPending = true;

        public Vector2 Center => transform.position;
        // 연료 비율로 최소~최대 반경 사이를 오가고, 꺼지면 0. 변할 때는 초당 radiusChangePerSecond 이하로만 움직여 적이 갑자기 튕겨나지 않는다.
        public float Radius => snapPending ? TargetRadius() : current;
        public bool RepelEnemies => profile != null && profile.repelEnemies;
        public float RepelDistance => profile != null ? profile.repelDistance : 0f;
        public float RepelIntervalSeconds => profile != null ? profile.repelIntervalSeconds : 1f;

        public void Bind(LandmarkContext context)
        {
            profile = context.Data.safeZone;
            fuel = GetComponent<CampfireFuel>();
            snapPending = true;
        }

        private void Update()
        {
            if (profile == null) return;
            var target = TargetRadius();
            // 첫 프레임(연료가 불러온 상태 그대로)과 꺼질 때는 곧바로 맞춘다. 꺼지면 밀어낼 영역 자체가 없어 튕길 일이 없다.
            if (snapPending || target <= 0f)
            {
                current = target;
                snapPending = false;
                return;
            }
            current = Mathf.MoveTowards(current, target, profile.radiusChangePerSecond * Time.deltaTime);
        }

        private float TargetRadius()
        {
            if (profile == null) return 0f;
            if (fuel == null || !fuel.HasProfile) return profile.radius;
            return profile.radius * fuel.Strength(fuel.Profile.safeRadiusAtEmpty);
        }

        private void OnEnable()
        {
            SafeZoneRegistry.Register(this);
        }

        private void OnDisable()
        {
            SafeZoneRegistry.Unregister(this);
        }
    }
}
