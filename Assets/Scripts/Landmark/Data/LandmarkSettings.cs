using UnityEngine;
using ExpiryGame.MapGen;

namespace ExpiryGame.Landmark
{
    // 랜드마크 배치 수치. 청크 크기/월드 seed는 MapGenSettings에서 가져온다.
    [CreateAssetMenu(menuName = "ShelfLife/Landmark Settings", fileName = "LandmarkSettings")]
    public class LandmarkSettings : ScriptableObject
    {
        public MapGenSettings map;

        [Header("지역")]
        [Tooltip("한 지역의 한 변 (청크 수). 지역마다 랜드마크 0~1개")]
        [Min(1)] public int chunksPerRegion = 6;
        [Tooltip("지역 경계에서 랜드마크 중심까지 최소 거리 (타일). 이웃 지역 랜드마크와 겹치지 않게 하는 여유")]
        [Min(0f)] public float regionMargin = 8f;
        [Tooltip("지역에 랜드마크가 생길 확률")]
        [Range(0f, 1f)] public float spawnChance = 0.75f;
        [Tooltip("랜드마크 주변 장애물을 비우는 여유 (타일)")]
        [Min(0f)] public float obstacleClearPadding = 2f;
        public int salt = 0x1A2D;

        [Header("종류 (지역 추첨에 쓰는 목록)")]
        public LandmarkData[] landmarks = new LandmarkData[0];

        [Header("시작 지점 보장")]
        [Tooltip("시작 지점 근처에 반드시 하나 놓이는 랜드마크")]
        public LandmarkData startLandmark;
        [Tooltip("MapGenSettings.startPoint 기준 오프셋. 지역 경계 여유 안으로 보정된다")]
        public Vector2 startOffset = new Vector2(10f, 8f);
    }
}
