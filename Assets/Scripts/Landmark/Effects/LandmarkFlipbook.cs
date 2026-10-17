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

        public Sprite[] Frames => frames;

        // 다른 프레임 묶음으로 갈아끼운다 (모닥불 연료 단계 등)
        public void SetFrames(Sprite[] newFrames)
        {
            if (newFrames == null || newFrames.Length == 0) return;
            frames = newFrames;
            elapsed = 0f;
        }

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
