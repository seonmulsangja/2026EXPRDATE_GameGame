using System.Collections.Generic;
using UnityEngine;

namespace ExpiryGame.Landmark
{
    public readonly struct LandmarkInfo
    {
        public readonly LandmarkData Data;
        public readonly Vector2 Position;
        public readonly float Distance;

        // 종류+위치로 정해지는 ID (CampfireStateStore의 키)
        public string Id => LandmarkId.Of(Data, Position);

        public LandmarkInfo(LandmarkData data, Vector2 position, float distance)
        {
            Data = data;
            Position = position;
            Distance = distance;
        }
    }

    // 로드 여부와 상관없이 가까운 랜드마크를 거리순으로 알려준다 (UI 화살표, 디버그 텔레포트용).
    public interface ILandmarkLocator
    {
        // from 기준 가까운 순으로 results를 채운다 (results는 비운 뒤 채움)
        void GetNearby(Vector2 from, List<LandmarkInfo> results);
    }

    public class LandmarkLocator : MonoBehaviour, ILandmarkLocator
    {
        [SerializeField] private LandmarkSettings settings;
        [Tooltip("중심 지역 기준 몇 지역까지 찾을지 (2 = 5x5 지역)")]
        [SerializeField, Min(1)] private int searchRing = 2;

        private readonly List<LandmarkPlacement> placements = new List<LandmarkPlacement>();
        private LandmarkPlanner planner;

        private void Awake()
        {
            planner = new LandmarkPlanner(settings);
        }

        public void GetNearby(Vector2 from, List<LandmarkInfo> results)
        {
            results.Clear();
            placements.Clear();
            planner.CollectAround(from, searchRing, placements);
            foreach (var placement in placements)
                results.Add(new LandmarkInfo(placement.Data, placement.Position, Vector2.Distance(from, placement.Position)));
            results.Sort((a, b) => a.Distance.CompareTo(b.Distance));
        }
    }
}
