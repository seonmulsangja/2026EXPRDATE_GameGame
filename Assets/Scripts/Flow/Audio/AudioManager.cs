using UnityEngine;

namespace ExpiryGame.Flow
{
    // BGM 크로스페이드 + 효과음 재생. 어떤 소리를 낼지는 모르고(상태만 보고 BGM을 고르는 것 제외),
    // 효과음 연결은 AudioEventBinder가 한다. 클립이 비어 있으면 조용히 건너뛴다.
    public class AudioManager : MonoBehaviour
    {
        private const float EvaluateInterval = 0.25f;

        private AudioCatalog catalog;
        private IGameClock clock;
        private IGameStateMachine machine;
        private Transform listener;

        private readonly AudioSource[] bgm = new AudioSource[2];
        private AudioSource sfx;
        private int current;
        private bool fading;
        private float fadeProgress;
        private float nextEvaluate;

        public void Initialize(AudioCatalog audioCatalog, IGameClock gameClock, IGameStateMachine stateMachine, Transform player)
        {
            catalog = audioCatalog;
            clock = gameClock;
            machine = stateMachine;
            listener = player;

            for (var i = 0; i < bgm.Length; i++) bgm[i] = CreateSource("Bgm" + i, true);
            sfx = CreateSource("Sfx", false);
        }

        public void PlaySfx(AudioClip clip)
        {
            if (clip == null || sfx == null || catalog == null) return;
            sfx.PlayOneShot(clip, catalog.sfxVolume);
        }

        private void Update()
        {
            if (catalog == null) return;
            var dt = Time.unscaledDeltaTime;

            if (fading) AdvanceFade(dt);
            else if (Time.unscaledTime >= nextEvaluate)
            {
                nextEvaluate = Time.unscaledTime + EvaluateInterval;
                var desired = ChooseClip();
                if (desired != null && desired != bgm[current].clip) BeginFade(desired);
            }
        }

        private AudioClip ChooseClip()
        {
            switch (machine.Current)
            {
                case GameState.GameOver:
                    return catalog.bgmGameOver;
                case GameState.Playing:
                case GameState.Paused:
                    if (catalog.bgmDanger != null && listener != null &&
                        EnemyRegistry.CountWithin(listener.position, catalog.dangerRadius) >= catalog.dangerEnemyCount)
                        return catalog.bgmDanger;
                    return clock.Phase == DayPhase.Night && catalog.bgmNight != null ? catalog.bgmNight : catalog.bgmDay;
                default:
                    return catalog.bgmDay;
            }
        }

        private void BeginFade(AudioClip clip)
        {
            var next = 1 - current;
            var source = bgm[next];
            source.clip = clip;
            source.loop = clip != catalog.bgmGameOver;
            source.volume = 0f;
            source.Play();
            fading = true;
            fadeProgress = 0f;
        }

        private void AdvanceFade(float dt)
        {
            fadeProgress = Mathf.Clamp01(fadeProgress + dt / catalog.crossfadeSeconds);
            var next = 1 - current;
            bgm[next].volume = fadeProgress * catalog.bgmVolume;
            bgm[current].volume = (1f - fadeProgress) * catalog.bgmVolume;
            if (fadeProgress < 1f) return;

            bgm[current].Stop();
            bgm[current].clip = null;
            current = next;
            fading = false;
        }

        private AudioSource CreateSource(string sourceName, bool loop)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
            return source;
        }
    }
}
