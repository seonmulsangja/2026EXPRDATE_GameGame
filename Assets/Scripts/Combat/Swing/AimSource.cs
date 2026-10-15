using UnityEngine;
using UnityEngine.InputSystem;

namespace ExpiryGame.Combat
{
    // 마우스 방향 우선, 마우스가 없거나 꺼져 있으면 이동 방향(IFacing)
    public class AimSource : MonoBehaviour, IAimSource
    {
        [SerializeField] private PlayerFacing facing;
        [SerializeField] private bool preferMouse = true;
        [SerializeField] private Camera viewCamera;

        public Vector2 GetAimDirection(Vector2 origin)
        {
            var mouse = Mouse.current;
            var cam = viewCamera != null ? viewCamera : Camera.main;
            if (preferMouse && mouse != null && cam != null)
            {
                var screen = mouse.position.ReadValue();
                var world = (Vector2)cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
                var toMouse = world - origin;
                if (toMouse.sqrMagnitude > 0.0001f) return toMouse.normalized;
            }
            return facing != null ? ((IFacing)facing).FacingDirection.normalized : Vector2.down;
        }
    }
}
