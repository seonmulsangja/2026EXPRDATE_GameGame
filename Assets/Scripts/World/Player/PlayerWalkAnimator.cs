using UnityEngine;

namespace ExpiryGame.World
{
    // 이동 입력이 있으면 walk_a → idle → walk_b → idle 루프, 멈추면 idle. 방향은 마지막 이동 방향(IFacing).
    // 기존 PlayerSpriteFacing(방향만 고르던 컴포넌트)을 대체한다.
    public class PlayerWalkAnimator : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerFacing facing;
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private WalkAnimationProfile profile;

        private float elapsed;

        private void LateUpdate()
        {
            var frames = profile.For(((IFacing)facing).FacingDirection);
            var moving = ((IPlayerInput)input).Move.sqrMagnitude > Mathf.Epsilon;

            Sprite pick;
            if (moving)
            {
                elapsed += Time.deltaTime;
                var step = (int)(elapsed / profile.frameSeconds) % 4;
                pick = step == 0 ? frames.walkA : step == 2 ? frames.walkB : frames.idle;
            }
            else
            {
                elapsed = 0f;
                pick = frames.idle;
            }

            if (pick != null) body.sprite = pick;
        }
    }
}
