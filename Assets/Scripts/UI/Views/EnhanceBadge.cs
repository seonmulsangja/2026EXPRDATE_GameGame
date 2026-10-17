using UnityEngine;
using UnityEngine.UI;

namespace ExpiryGame.UI
{
    // 강화된 음식 표시용 작은 배지. UITheme.sprites.enhanceBadge가 있으면 그 스프라이트를, 없으면 임시 도형(마름모)을 쓴다.
    // 스프라이트가 생기면 UITheme 한 곳만 채우면 된다 (필요한 아트: docs/art_needed.md).
    public static class EnhanceBadge
    {
        public static Image Create(RectTransform parent, UITheme theme, Vector2 anchor, Vector2 offset, float size)
        {
            var go = new GameObject("EnhancedBadge", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = offset;
            rect.sizeDelta = new Vector2(size, size);

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = theme.enhancedBadgeColor;
            if (theme.sprites.enhanceBadge != null) image.sprite = theme.sprites.enhanceBadge;
            else rect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            go.SetActive(false);
            return image;
        }
    }
}
