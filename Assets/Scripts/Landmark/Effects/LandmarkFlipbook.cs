using UnityEngine;

namespace ExpiryGame.Landmark
{
    // 모닥불처럼 프레임을 루프하는 스프라이트 애니메이션 (프레임 간격은 인스펙터/프리팹 값)
    [RequireComponent(typeof(SpriteRenderer))]
    public class LandmarkFlipbook : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames = new Sprite[0];
        [SerializeField, Min(0.02f)] private float frameSeconds = 0.15f;

        private SpriteRenderer sr;
        private float elapsed;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (frames.Length == 0) return;
            elapsed += Time.deltaTime;
            sr.sprite = frames[(int)(elapsed / frameSeconds) % frames.Length];
        }
    }
}
