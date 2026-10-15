using UnityEngine;

namespace ExpiryGame.Visual
{
    // 바라보는 방향(IFacing)에 맞춰 플레이어 4방향 스프라이트를 고른다.
    public class PlayerSpriteFacing : MonoBehaviour
    {
        [SerializeField] private PlayerFacing facing;
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private Sprite down;
        [SerializeField] private Sprite up;
        [SerializeField] private Sprite left;
        [SerializeField] private Sprite right;

        private void LateUpdate()
        {
            var dir = ((IFacing)facing).FacingDirection;
            Sprite pick;
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y)) pick = dir.x > 0f ? right : left;
            else pick = dir.y > 0f ? up : down;
            if (pick != null) body.sprite = pick;
        }
    }
}
