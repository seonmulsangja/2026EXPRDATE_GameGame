using UnityEngine;

namespace ExpiryGame.Landmark
{
    // 쓰레기 더미: 처음 반경에 들어오면 썩은 음식 위주로 쏟아내고, 들어올 때마다 위험 이벤트를 발행한다.
    // 적 쪽 반응은 IDangerZoneEvents.OnDangerZoneEntered를 구독하는 팀원 코드가 맡는다.
    // 플레이어가 모닥불 안전지대 안에 있으면 위험 구역과 겹쳐도 안전이 우선한다 (위험 이벤트를 내지 않는다).
    [RequireComponent(typeof(LandmarkZone))]
    public class DumpDangerEffect : MonoBehaviour, ILandmarkPart
    {
        private LandmarkZone zone;
        private LandmarkContext context;
        private bool spilled;
        private bool announced;

        private void Awake()
        {
            zone = GetComponent<LandmarkZone>();
        }

        private void OnEnable()
        {
            zone.Entered += OnEntered;
        }

        private void OnDisable()
        {
            zone.Entered -= OnEntered;
            Announce(false);
        }

        public void Bind(LandmarkContext ctx)
        {
            context = ctx;
        }

        private void Update()
        {
            if (context == null || context.Player == null) return;
            Announce(zone.IsInside && !SafeZoneRegistry.IsSafe(context.Player.position));
        }

        private void OnEntered()
        {
            if (context == null || spilled) return;
            spilled = true;
            var origin = (Vector3)(Vector2)transform.position + Vector3.up * 0.6f;
            for (var i = 0; i < context.Data.dumpRolls; i++)
                context.Drops.Drop(context.Data.dumpTable, context.Data.dumpThreat01, origin);
        }

        // 위험 구역 안에 있고 안전지대 밖일 때만 "위험"으로 알린다 (상태가 바뀔 때 한 번씩)
        private void Announce(bool dangerous)
        {
            if (context == null || dangerous == announced) return;
            announced = dangerous;
            if (dangerous) context.Danger?.Enter(Info());
            else context.Danger?.Exit(Info());
        }

        private DangerZoneInfo Info()
        {
            return new DangerZoneInfo(context.Data.kind, transform.position, context.Data.effectRadius);
        }
    }
}
