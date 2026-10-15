using UnityEngine;

namespace ExpiryGame.Combat
{
    public interface IAimSource
    {
        // origin 기준으로 조준(휘두를) 방향. 항상 정규화된 값.
        Vector2 GetAimDirection(Vector2 origin);
    }
}
