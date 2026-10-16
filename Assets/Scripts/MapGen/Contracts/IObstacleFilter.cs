using UnityEngine;

namespace ExpiryGame.MapGen
{
    // 생성된 장애물 배치를 월드에 올리기 직전에 걸러내는 계약 (예: 랜드마크 주변 비우기).
    // 판정은 위치만으로 결정적이어야 한다 (청크가 다시 로드돼도 같은 결과).
    public interface IObstacleFilter
    {
        bool IsBlocked(Vector2 worldPosition);
    }
}
