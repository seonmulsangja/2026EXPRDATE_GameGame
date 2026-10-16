using System;
using UnityEngine;

namespace ExpiryGame.World
{
    [Serializable]
    public struct DirectionalFrames
    {
        public Sprite idle;
        public Sprite walkA;
        public Sprite walkB;
    }

    // 플레이어 걷기 애니메이션 수치/스프라이트. 프레임 간격은 여기서 조절한다.
    [CreateAssetMenu(menuName = "ShelfLife/Walk Animation Profile", fileName = "WalkAnimationProfile")]
    public class WalkAnimationProfile : ScriptableObject
    {
        [Tooltip("프레임 하나가 보이는 시간(초). 아트 매니페스트 권장 0.12~0.15")]
        [Min(0.02f)] public float frameSeconds = 0.13f;
        public DirectionalFrames down;
        public DirectionalFrames up;
        public DirectionalFrames left;
        public DirectionalFrames right;

        public DirectionalFrames For(Vector2 direction)
        {
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y)) return direction.x > 0f ? right : left;
            return direction.y > 0f ? up : down;
        }
    }
}
