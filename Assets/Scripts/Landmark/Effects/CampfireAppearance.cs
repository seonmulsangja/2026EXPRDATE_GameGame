using UnityEngine;

namespace ExpiryGame.Landmark
{
    // 모닥불 외형: 연료 단계에 맞는 스프라이트로 바꾸고(전환 때 짧은 페이드), 꺼지면 빛번짐(Glow)을 끈다.
    // 가득 = 프리팹의 기존 루프(landmark_campfire_0~2), 나머지 단계 스프라이트는 아래 필드(LandmarkSetup이 채운다).
    [RequireComponent(typeof(CampfireFuel))]
    public class CampfireAppearance : MonoBehaviour
    {
        [SerializeField] private Sprite[] weakFrames = new Sprite[0];
        [SerializeField] private Sprite[] embersFrames = new Sprite[0];
        [SerializeField] private Sprite[] outFrames = new Sprite[0];

        private CampfireFuel fuel;
        private SpriteRenderer body;
        private LandmarkFlipbook flipbook;
        private Transform glow;
        private Vector3 glowBaseScale = Vector3.one;
        private float glowFollow = 1f;
        private Sprite[] fullFrames;
        private SpriteRenderer fade;
        private float fadeLeft;
        private float fadeTotal;
        private bool applied;
        private CampfireStage appliedStage;

        private void Awake()
        {
            fuel = GetComponent<CampfireFuel>();
            var bodyTransform = transform.Find("Body");
            if (bodyTransform != null)
            {
                body = bodyTransform.GetComponent<SpriteRenderer>();
                flipbook = bodyTransform.GetComponent<LandmarkFlipbook>();
                if (flipbook != null) fullFrames = flipbook.Frames;
            }
            glow = transform.Find("Glow");
            if (glow != null) glowBaseScale = glow.localScale;
        }

        private void Update()
        {
            if (body == null || !fuel.HasProfile) return;

            var stage = fuel.Stage;
            if (!applied || stage != appliedStage) Apply(stage, applied);
            UpdateFade();
            UpdateGlow();
        }

        private void Apply(CampfireStage stage, bool animate)
        {
            if (animate && fuel.Profile.stageFadeSeconds > 0f) StartFade();
            applied = true;
            appliedStage = stage;

            var frames = FramesFor(stage);
            if (frames == null || frames.Length == 0) return;
            if (flipbook != null) flipbook.SetFrames(frames);
            // 플립북은 다음 Update에 스프라이트를 바꾸므로, 한 프레임 동안 옛 스프라이트가 남지 않게 바로 맞춘다
            body.sprite = frames[0];
        }

        private Sprite[] FramesFor(CampfireStage stage)
        {
            switch (stage)
            {
                case CampfireStage.Weak: return weakFrames.Length > 0 ? weakFrames : fullFrames;
                case CampfireStage.Embers: return embersFrames.Length > 0 ? embersFrames : (weakFrames.Length > 0 ? weakFrames : fullFrames);
                case CampfireStage.Out: return outFrames.Length > 0 ? outFrames : (embersFrames.Length > 0 ? embersFrames : fullFrames);
                default: return fullFrames;
            }
        }

        // 바뀌기 직전 스프라이트를 위에 겹쳐 두고 서서히 투명하게
        private void StartFade()
        {
            if (fade == null)
            {
                var go = new GameObject("StageFade");
                go.transform.SetParent(body.transform, false);
                fade = go.AddComponent<SpriteRenderer>();
                fade.sharedMaterial = body.sharedMaterial;
                fade.sortingLayerID = body.sortingLayerID;
                fade.sortingOrder = body.sortingOrder + 1;
            }
            fade.sprite = body.sprite;
            fadeTotal = fadeLeft = fuel.Profile.stageFadeSeconds;
            fade.color = Color.white;
            fade.enabled = true;
        }

        private void UpdateFade()
        {
            if (fade == null || !fade.enabled) return;
            fadeLeft -= Time.deltaTime;
            if (fadeLeft <= 0f)
            {
                fade.enabled = false;
                return;
            }
            fade.color = new Color(1f, 1f, 1f, fadeLeft / fadeTotal);
        }

        // 빛번짐은 꺼지면 사라지고, 연료가 줄면 작아진다 (색 알파는 NightGlowFader가 그대로 맡는다)
        private void UpdateGlow()
        {
            if (glow == null) return;
            var target = fuel.Strength(fuel.Profile.lightRadiusAtEmpty);
            glowFollow = Mathf.MoveTowards(glowFollow, target, fuel.Profile.lightFollowSpeed * Time.deltaTime);
            glow.localScale = glowBaseScale * glowFollow;
            var visible = glowFollow > 0.001f;
            if (glow.gameObject.activeSelf != visible) glow.gameObject.SetActive(visible);
        }
    }
}
