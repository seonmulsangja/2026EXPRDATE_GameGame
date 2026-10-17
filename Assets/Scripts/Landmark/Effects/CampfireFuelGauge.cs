using UnityEngine;

namespace ExpiryGame.Landmark
{
    // 모닥불 위에 뜨는 작은 연료 게이지 (SpriteRenderer만 사용). 플레이어가 가까이 있거나 연료가 적을 때만 보이고,
    // 더 적어지면 깜빡인다. 연료 값은 CampfireFuel에서 읽기만 한다.
    [RequireComponent(typeof(CampfireFuel))]
    public class CampfireFuelGauge : MonoBehaviour
    {
        private static readonly Color BackColor = new Color(0.05f, 0.05f, 0.08f, 0.85f);
        private static readonly Color FullColor = new Color(1f, 0.72f, 0.25f);
        private static readonly Color LowColor = new Color(0.95f, 0.25f, 0.2f);
        private static readonly Color OutColor = new Color(0.45f, 0.5f, 0.6f);
        private static Sprite whiteSprite;

        [Tooltip("비워 두면 기본 스프라이트 머티리얼 (빛 영향 없음)")]
        [SerializeField] private Material material;
        [SerializeField, Min(0.02f)] private float barThickness = 0.14f;
        [SerializeField] private int sortingOrder = 6;

        private CampfireFuel fuel;
        private LandmarkZone zone;
        private Transform root;
        private SpriteRenderer back;
        private SpriteRenderer fill;
        private SpriteRenderer marker;
        private float width;

        private void Awake()
        {
            fuel = GetComponent<CampfireFuel>();
            zone = GetComponent<LandmarkZone>();
        }

        private void Update()
        {
            var profile = fuel.Profile;
            if (!fuel.HasProfile || profile == null)
            {
                SetVisible(false);
                return;
            }

            var show = zone.IsInside || fuel.Ratio < profile.gaugeShowBelow01 || !fuel.IsLit;
            if (!show)
            {
                SetVisible(false);
                return;
            }

            EnsureBuilt(profile);
            SetVisible(true);

            var ratio = fuel.Ratio;
            var fillWidth = Mathf.Max(0.0001f, width * ratio);
            fill.transform.localScale = new Vector3(fillWidth, barThickness * 0.7f, 1f);
            fill.transform.localPosition = new Vector3(-width * 0.5f + fillWidth * 0.5f, 0f, 0f);
            var color = fuel.IsLit ? Color.Lerp(LowColor, FullColor, ratio) : OutColor;

            // 연료가 적을 때만 깜빡인다 (꺼진 뒤에는 고정)
            var alpha = 1f;
            if (fuel.IsLit && ratio < profile.gaugeFlashBelow01)
                alpha = Mathf.Lerp(0.25f, 1f, Mathf.PingPong(Time.time * profile.gaugeFlashHz * 2f, 1f));
            color.a = alpha;
            fill.color = color;
            var backColor = BackColor;
            backColor.a *= alpha;
            back.color = backColor;
            marker.transform.localPosition = new Vector3(-width * 0.5f + width * profile.relightThreshold01, 0f, 0f);
            marker.color = new Color(1f, 1f, 1f, 0.7f * alpha);
        }

        private void SetVisible(bool visible)
        {
            if (root != null && root.gameObject.activeSelf != visible) root.gameObject.SetActive(visible);
        }

        private void EnsureBuilt(CampfireFuelProfile profile)
        {
            if (root != null) return;
            width = profile.gaugeWidth;
            root = new GameObject("FuelGauge").transform;
            root.SetParent(transform, false);
            root.localPosition = new Vector3(0f, profile.gaugeHeight, 0f);

            back = MakePart("Back", sortingOrder);
            back.transform.localScale = new Vector3(width + barThickness * 0.5f, barThickness, 1f);
            fill = MakePart("Fill", sortingOrder + 1);
            marker = MakePart("RelightMark", sortingOrder + 2);
            marker.transform.localScale = new Vector3(barThickness * 0.25f, barThickness * 1.2f, 1f);
        }

        private SpriteRenderer MakePart(string partName, int order)
        {
            var go = new GameObject(partName);
            go.transform.SetParent(root, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = WhiteSprite();
            sr.sortingOrder = order;
            if (material != null) sr.sharedMaterial = material;
            return sr;
        }

        // 1x1 흰색 스프라이트 (1유닛 크기라 localScale이 곧 크기)
        private static Sprite WhiteSprite()
        {
            if (whiteSprite != null) return whiteSprite;
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            whiteSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            whiteSprite.hideFlags = HideFlags.HideAndDontSave;
            return whiteSprite;
        }
    }
}
