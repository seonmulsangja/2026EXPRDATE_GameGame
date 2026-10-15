using UnityEngine;

namespace ExpiryGame.Visual
{
    [RequireComponent(typeof(Camera))]
    public class CameraBackdrop : MonoBehaviour
    {
        [SerializeField] private ArtTheme theme;

        private void Awake()
        {
            var cam = GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = theme.backgroundColor;
        }
    }
}
