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

    // 충돌 없는 바닥 장식 한 종류. night 스프라이트가 있으면 day/night 쌍(알파 크로스페이드), 없으면 공용(밤에 틴트).
    [Serializable]
    public struct DecorDefinition
    {
        public string name;
        public Sprite day;
        [Tooltip("비어 있으면 공용 장식: 밤에 day 스프라이트에 nightTint를 곱한다")]
        public Sprite night;
        [Min(0f)] public float weight;
        public Color nightTint;
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
        [Tooltip("순서: floor_a, floor_b, floor_c, floor_d(풀)")]
        public TileBase[] floorTiles = new TileBase[4];
        [Tooltip("아트 v2 밤 바닥. 순서는 floorTiles와 같다. 비어 있으면 낮 바닥만 쓴다 (night Tilemap은 같은 좌표에 겹쳐 알파 크로스페이드)")]
        public TileBase[] nightFloorTiles = new TileBase[4];
        public int floorSortingOrder = -100;
        [Tooltip("풀(floor_d) 노이즈. 이 값 이상인 타일이 풀이 된다 (1에 가까울수록 드묾)")]
        [Min(0.001f)] public float grassNoiseScale = 0.05f;
        [Range(0f, 1f)] public float grassStart = 0.68f;

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

        [Header("바닥 장식 (충돌 없음)")]
        public DecorDefinition[] decor = Array.Empty<DecorDefinition>();
        [Tooltip("청크당 배치 시도 횟수 (상한)")]
        [Min(0)] public int decorAttemptsPerChunk = 40;
        [Min(0.001f)] public float decorNoiseScale = 0.06f;
        [Tooltip("밀도 노이즈가 0 / 1일 때 시도가 채택될 확률")]
        [Range(0f, 1f)] public float decorDensityAtLow = 0.15f;
        [Range(0f, 1f)] public float decorDensityAtHigh = 0.6f;
        [Tooltip("장식끼리 최소 거리 (타일)")]
        [Min(0f)] public float minDecorSpacing = 1.2f;
        [Tooltip("장애물과 최소 거리 (타일)")]
        [Min(0f)] public float decorObstacleClearance = 1f;
        public Material decorMaterial;
        [Tooltip("바닥(-100) 위, 효과 반경 링 아래")]
        public int decorSortingOrder = -95;

        private void OnValidate()
        {
            floorCStart = Mathf.Max(floorBStart, floorCStart);
            if (floorTiles == null || floorTiles.Length != 4) Array.Resize(ref floorTiles, 4);
            if (nightFloorTiles == null || nightFloorTiles.Length != 4) Array.Resize(ref nightFloorTiles, 4);
        }
    }
}
