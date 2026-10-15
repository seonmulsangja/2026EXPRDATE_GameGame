using UnityEngine;

namespace ExpiryGame
{
    public class PlayerFacing : MonoBehaviour, IFacing
    {
        [SerializeField] private PlayerInputReader input;

        public Vector2 FacingDirection { get; private set; } = Vector2.down;

        private void Update()
        {
            var move = ((IPlayerInput)input).Move;
            if (move.sqrMagnitude > Mathf.Epsilon) FacingDirection = move.normalized;
        }
    }
}
