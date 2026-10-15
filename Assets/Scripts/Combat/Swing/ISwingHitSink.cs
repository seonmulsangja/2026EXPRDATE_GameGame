using UnityEngine;

namespace ExpiryGame.Combat
{
    // SwingController가 판정기에 알리는 계약
    public interface ISwingHitSink
    {
        void BeginSwing(SwingInfo info);
        void Sweep(Vector2 origin, float fromAngle, float toAngle);
        int EndSwing();
    }
}
