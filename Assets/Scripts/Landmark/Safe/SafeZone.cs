using System.Collections.Generic;
using UnityEngine;

namespace ExpiryGame.Landmark
{
    // 안전지대(모닥불). 적 스폰/쓰레기 더미 위험 구역이 "여기가 안전한가"를 물을 때 쓰는 계약.
    public interface ISafeZone
    {
        Vector2 Center { get; }
        float Radius { get; }
        bool RepelEnemies { get; }
        float RepelDistance { get; }
        float RepelIntervalSeconds { get; }
    }

    // 로드된 안전지대 등록소 (InteractableRegistry와 같은 방식). 적 어댑터/위험 구역이 읽는다.
    public static class SafeZoneRegistry
    {
        private static readonly List<ISafeZone> zones = new List<ISafeZone>();

        public static IReadOnlyList<ISafeZone> Zones => zones;

        public static void Register(ISafeZone zone)
        {
            if (!zones.Contains(zone)) zones.Add(zone);
        }

        public static void Unregister(ISafeZone zone)
        {
            zones.Remove(zone);
        }

        public static bool TryFind(Vector2 position, out ISafeZone found)
        {
            for (var i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                if (zone.Radius <= 0f || (position - zone.Center).sqrMagnitude > zone.Radius * zone.Radius) continue;
                found = zone;
                return true;
            }
            found = null;
            return false;
        }

        public static bool IsSafe(Vector2 position)
        {
            return TryFind(position, out _);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            zones.Clear();
        }
    }

    // 배치 정보(LandmarkInfo)만으로 만든 안전 구역 모음. 청크가 생길 때 "이미 정해진 랜드마크 위치"로 판단하므로
    // 랜드마크 프리팹이 아직 로드되지 않았어도 같은 답을 준다.
    public sealed class SafeZoneSet
    {
        public static readonly SafeZoneSet Empty = new SafeZoneSet(new List<KeyValuePair<Vector2, float>>());

        private readonly List<KeyValuePair<Vector2, float>> circles;

        private SafeZoneSet(List<KeyValuePair<Vector2, float>> circles)
        {
            this.circles = circles;
        }

        public int Count => circles.Count;

        public static SafeZoneSet From(IEnumerable<LandmarkInfo> landmarks)
        {
            var circles = new List<KeyValuePair<Vector2, float>>();
            foreach (var info in landmarks)
            {
                var data = info.Data;
                if (data == null || data.kind != LandmarkKind.Campfire || data.safeZone == null || data.safeZone.radius <= 0f) continue;
                circles.Add(new KeyValuePair<Vector2, float>(info.Position, data.safeZone.radius));
            }
            return circles.Count == 0 ? Empty : new SafeZoneSet(circles);
        }

        public bool Contains(Vector2 position)
        {
            for (var i = 0; i < circles.Count; i++)
            {
                var radius = circles[i].Value;
                if ((position - circles[i].Key).sqrMagnitude <= radius * radius) return true;
            }
            return false;
        }
    }
}
