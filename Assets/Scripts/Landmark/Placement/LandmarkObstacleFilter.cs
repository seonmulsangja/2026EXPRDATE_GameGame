using UnityEngine;
using ExpiryGame.MapGen;

namespace ExpiryGame.Landmark
{
    // 랜드마크 주변(크기 + 여유)은 장애물을 비운다. ChunkStreamer의 장애물 필터로 등록된다.
    public class LandmarkObstacleFilter : MonoBehaviour, IObstacleFilter
    {
        [SerializeField] private LandmarkSettings settings;
        [SerializeField] private ChunkStreamer streamer;

        private LandmarkPlanner planner;

        private void Awake()
        {
            planner = new LandmarkPlanner(settings);
            streamer.SetObstacleFilter(this);
        }

        public bool IsBlocked(Vector2 worldPosition)
        {
            return planner.IsInClearArea(worldPosition);
        }
    }
}
