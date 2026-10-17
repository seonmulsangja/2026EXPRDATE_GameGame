using UnityEngine;

namespace ExpiryGame.Landmark
{
    // 모닥불을 안전지대로 등록한다. 수치는 LandmarkData.safeZone(SO)에서 읽고, 판단은 SafeZoneRegistry를 읽는 쪽이 한다.
    public class CampfireSafeZone : MonoBehaviour, ILandmarkPart, ISafeZone
    {
        private SafeZoneProfile profile;

        public Vector2 Center => transform.position;
        public float Radius => profile != null ? profile.radius : 0f;
        public bool RepelEnemies => profile != null && profile.repelEnemies;
        public float RepelDistance => profile != null ? profile.repelDistance : 0f;
        public float RepelIntervalSeconds => profile != null ? profile.repelIntervalSeconds : 1f;

        public void Bind(LandmarkContext context)
        {
            profile = context.Data.safeZone;
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
