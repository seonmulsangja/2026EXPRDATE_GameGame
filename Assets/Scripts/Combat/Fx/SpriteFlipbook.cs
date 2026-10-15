using System;
using UnityEngine;

namespace ExpiryGame.Combat
{
    // 스프라이트 프레임 재생 후 완료 콜백(풀 반환). 실시간 기준이라 히트스톱 중에도 재생된다.
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFlipbook : MonoBehaviour
    {
        private SpriteRenderer sr;
        private Sprite[] frames;
        private float frameSeconds;
        private float elapsed;
        private Action<SpriteFlipbook> onFinished;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        public void Play(Sprite[] sequence, float totalSeconds, Action<SpriteFlipbook> finished)
        {
            frames = sequence;
            frameSeconds = totalSeconds / Mathf.Max(1, sequence.Length);
            elapsed = 0f;
            onFinished = finished;
            sr.sprite = frames[0];
            enabled = true;
        }

        private void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            var index = (int)(elapsed / frameSeconds);
            if (index >= frames.Length)
            {
                enabled = false;
                onFinished?.Invoke(this);
                return;
            }
            sr.sprite = frames[index];
        }
    }
}
