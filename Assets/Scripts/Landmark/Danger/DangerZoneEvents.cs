using System;
using UnityEngine;

namespace ExpiryGame.Landmark
{
    public readonly struct DangerZoneInfo
    {
        public readonly LandmarkKind Source;
        public readonly Vector2 Center;
        public readonly float Radius;

        public DangerZoneInfo(LandmarkKind source, Vector2 center, float radius)
        {
            Source = source;
            Center = center;
            Radius = radius;
        }
    }

    // 팀원(적 AI)용: 위험 구역 진입/이탈을 구독한다. 적이 어떻게 반응할지는 구독하는 쪽이 정한다.
    public interface IDangerZoneEvents
    {
        bool IsPlayerInDanger { get; }
        event Action<DangerZoneInfo> OnDangerZoneEntered;
        event Action<DangerZoneInfo> OnDangerZoneExited;
    }

    // 위험 구역을 만드는 쪽(쓰레기 더미 등)이 호출하는 쪽
    public interface IDangerZoneSink
    {
        void Enter(DangerZoneInfo info);
        void Exit(DangerZoneInfo info);
    }

    // 씬에 1개. 겹친 구역을 세어서 IsPlayerInDanger를 유지한다.
    public class DangerZoneEvents : MonoBehaviour, IDangerZoneEvents, IDangerZoneSink
    {
        private int active;

        public bool IsPlayerInDanger => active > 0;

        public event Action<DangerZoneInfo> OnDangerZoneEntered;
        public event Action<DangerZoneInfo> OnDangerZoneExited;

        void IDangerZoneSink.Enter(DangerZoneInfo info)
        {
            active++;
            OnDangerZoneEntered?.Invoke(info);
        }

        void IDangerZoneSink.Exit(DangerZoneInfo info)
        {
            active = Mathf.Max(0, active - 1);
            OnDangerZoneExited?.Invoke(info);
        }
    }
}
