using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ExpiryGame.MapGen
{
    [Serializable]
    public struct ObstacleDefinition
    {
        public string name;
        public Sprite sprite;
        [Min(0f)] public float weight;
        public Vector2 colliderSize;
        public Vector2 colliderOffset;
    }

    // 맵 생성/스트리밍 수치 전부.
    [CreateAssetMenu(menuName = "ShelfLife/Map Gen Settings", fileName = "MapGenSettings")]
    public class MapGenSettings : ScriptableObject
    {
        [Header("월드")]
        public int seed = 12345;

        [Header("청크 스트리밍")]
        [Min(4)] public int chunkSize = 16;
        [Tooltip("플레이어 청크 기준 반경. 2 = 5x5 청크")]
        [Min(1)] public int loadRadius = 2;
        [Tooltip("로드 반경 밖으로 이만큼 더 나가야 언로드 (0 = 바로 언로드, 경계 왕복 시 재로드가 잦아지면 1로)")]
        [Min(0)] public int unloadMargin = 0;
        [Tooltip("프레임당 로드할 청크 수 (끊김 방지)")]
        [Min(1)] public int chunksPerFrame = 1;

        [Header("바닥 (노이즈 값 < B시작 → floor_a, < C시작 → floor_b, 그 이상 → floor_c)")]
        [Min(0.001f)] public float floorNoiseScale = 0.07f;
        [Range(0f, 1f)] public float floorBStart = 0.45f;
        [Range(0f, 1f)] public float floorCStart = 0.62f;
        [Tooltip("순서: floor_a, floor_b, floor_c")]
        public TileBase[] floorTiles = new TileBase[3];
        public int floorSortingOrder = -100;

        [Header("장애물 밀도")]
        [Min(0.001f)] public float obstacleNoiseScale = 0.04f;
        [Tooltip("청크당 배치 시도 횟수 (상한). 실제 개수는 밀도/간격 때문에 이보다 적다")]
        [Min(0)] public int obstacleAttemptsPerChunk = 48;
        [Tooltip("밀도 노이즈가 0일 때 시도가 채택될 확률")]
        [Range(0f, 1f)] public float densityAtLow = 0.02f;
        [Tooltip("밀도 노이즈가 1일 때 시도가 채택될 확률")]
        [Range(0f, 1f)] public float densityAtHigh = 0.45f;
        [Tooltip("장애물 사이 최소 거리 (타일). 청크 경계에서도 지켜진다")]
        [Min(1f)] public float minObstacleSpacing = 3f;
        [Tooltip("이 지점 주변은 장애물을 비운다 (플레이어 시작 지점)")]
        public Vector2 startPoint = Vector2.zero;
        [Min(0f)] public float startClearRadius = 6f;

        [Header("장애물 종류")]
        public ObstacleDefinition[] obstacles = Array.Empty<ObstacleDefinition>();
        public Material obstacleMaterial;
        public int obstacleSortingOrder = 1;

        private void OnValidate()
        {
            floorCStart = Mathf.Max(floorBStart, floorCStart);
            if (floorTiles == null || floorTiles.Length != 3) Array.Resize(ref floorTiles, 3);
        }
    }
}
