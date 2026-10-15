using UnityEngine;

namespace ExpiryGame.Visual
{
    // 음식 스프라이트 + 신선도 틴트. 스프라이트 위에 같은 모양의 반투명 오버레이를 얹어
    // 시들면 회색으로 탈색, 썩으면 탁한 녹색으로 덮는다. FoodItem은 읽기/구독만 한다.
    public class FoodVisual : MonoBehaviour
    {
        private const string OverlayName = "FreshnessOverlay";

        [SerializeField] private ArtTheme theme;
        [SerializeField] private GameClock clock;
        [SerializeField] private SpriteRenderer body;
        [Tooltip("월드 픽업처럼 FoodItem 없이 항상 신선하게 보여줄 음식 (선택)")]
        [SerializeField] private FoodData staticFood;

        private SpriteRenderer overlay;
        private FoodItem bound;
        private bool subscribed;

        private SpriteRenderer Overlay => overlay != null ? overlay : (overlay = EnsureOverlay());

        private void Awake()
        {
            if (staticFood != null) Show(staticFood);
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Show(FoodData data)
        {
            Unbind();
            SetSprite(data != null ? data.sprite : null);
            SetOverlay(Color.clear);
        }

        public void Bind(FoodItem item)
        {
            if (item == bound) return;
            Unbind();
            bound = item;
            Subscribe();
            Refresh();
        }

        public void Clear()
        {
            Unbind();
            SetSprite(null);
            SetOverlay(Color.clear);
        }

        private void Unbind()
        {
            Unsubscribe();
            bound = null;
        }

        private void Subscribe()
        {
            if (subscribed || bound == null) return;
            subscribed = true;
            bound.Rotted += OnRotted;
            if (clock != null) ((IGameClock)clock).Ticked += OnTicked;
        }

        private void Unsubscribe()
        {
            if (!subscribed) return;
            subscribed = false;
            bound.Rotted -= OnRotted;
            if (clock != null) ((IGameClock)clock).Ticked -= OnTicked;
        }

        private void OnTicked(float _)
        {
            Refresh();
        }

        private void OnRotted(FoodItem _)
        {
            Refresh();
        }

        // clock 참조 없이 쓰는 프리팹(월드 드롭)이 직접 갱신 시점을 정할 수 있게 공개
        public void Refresh()
        {
            if (bound == null) return;
            var rottenSprite = bound.Data.spriteRotten;
            SetSprite(bound.Data.SpriteFor(bound.IsRotten));
            // 썩음 전용 스프라이트가 있으면 그것으로 교체하고 오버레이는 끈다 (신선도 틴트는 신선 상태에서만)
            if (bound.IsRotten && rottenSprite != null) SetOverlay(Color.clear);
            else SetOverlay(theme.EvaluateFoodOverlay(bound.ShelfLife01, bound.IsRotten));
        }

        private void SetSprite(Sprite sprite)
        {
            if (body.sprite == sprite && Overlay.sprite == sprite) return;
            body.sprite = sprite;
            Overlay.sprite = sprite;
        }

        private void SetOverlay(Color color)
        {
            Overlay.color = color;
        }

        private SpriteRenderer EnsureOverlay()
        {
            var existing = transform.Find(OverlayName);
            var go = existing != null ? existing.gameObject : new GameObject(OverlayName);
            go.transform.SetParent(transform, false);

            var renderer = go.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = go.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = body.sharedMaterial;
            renderer.sortingLayerID = body.sortingLayerID;
            renderer.sortingOrder = body.sortingOrder + 1;
            renderer.color = Color.clear;
            return renderer;
        }
    }
}
