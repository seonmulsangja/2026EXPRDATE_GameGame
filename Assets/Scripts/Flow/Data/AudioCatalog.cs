using UnityEngine;

namespace ExpiryGame.Flow
{
    // 비어 있는 클립은 그냥 건너뛴다. 파일이 생기면 여기에 끌어다 놓기만 하면 된다.
    [CreateAssetMenu(menuName = "ShelfLife/Flow/Audio Catalog", fileName = "AudioCatalog")]
    public class AudioCatalog : ScriptableObject
    {
        [Header("BGM")]
        public AudioClip bgmDay;
        public AudioClip bgmNight;
        public AudioClip bgmDanger;
        public AudioClip bgmGameOver;
        [Range(0f, 1f)] public float bgmVolume = 0.6f;
        [Min(0.1f)] public float crossfadeSeconds = 1.5f;

        [Header("위험 BGM (플레이어 주변 적 수 기준으로 낮/밤 BGM을 덮어쓴다)")]
        [Min(1f)] public float dangerRadius = 8f;
        [Min(1)] public int dangerEnemyCount = 4;

        [Header("효과음")]
        public AudioClip sfxEat;
        public AudioClip sfxHit;
        public AudioClip sfxRot;
        public AudioClip sfxPickup;
        public AudioClip sfxBreak;
        [Tooltip("모닥불에서 음식을 강화했을 때 (비어 있으면 무음)")]
        public AudioClip sfxEnhance;
        [Tooltip("냉장고를 열었을 때 (비어 있으면 무음)")]
        public AudioClip sfxFridgeOpen;
        [Range(0f, 1f)] public float sfxVolume = 0.8f;
    }
}
