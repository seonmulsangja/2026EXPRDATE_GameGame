using UnityEngine;

namespace ExpiryGame.Landmark
{
    // 쓰레기 더미: 처음 반경에 들어오면 썩은 음식 위주로 쏟아내고, 들어올 때마다 위험 이벤트를 발행한다.
    // 적 쪽 반응은 IDangerZoneEvents.OnDangerZoneEntered를 구독하는 팀원 코드가 맡는다.
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
            zone.Exited += OnExited;
        }

        private void OnDisable()
        {
            zone.Entered -= OnEntered;
            zone.Exited -= OnExited;
            OnExited();
        }

        public void Bind(LandmarkContext ctx)
        {
            context = ctx;
        }

        private void OnEntered()
        {
            if (context == null) return;

            announced = true;
            context.Danger?.Enter(Info());

            if (spilled) return;
            spilled = true;
            var origin = (Vector3)(Vector2)transform.position + Vector3.up * 0.6f;
            for (var i = 0; i < context.Data.dumpRolls; i++)
                context.Drops.Drop(context.Data.dumpTable, context.Data.dumpThreat01, origin);
        }

        private void OnExited()
        {
            if (!announced || context == null) return;
            announced = false;
            context.Danger?.Exit(Info());
        }

        private DangerZoneInfo Info()
        {
            return new DangerZoneInfo(context.Data.kind, transform.position, context.Data.effectRadius);
        }
    }
}
