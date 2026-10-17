using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using ExpiryGame.UI;

namespace ExpiryGame.Flow
{
    // 흐름 화면(타이틀/일시정지/결과/배너)을 코드로 만드는 작은 도우미. 폰트/색은 UITheme에서만 받는다.
    public static class FlowUi
    {
        public static readonly Color Dim = new Color(0.02f, 0.02f, 0.05f, 0.78f);
        public static readonly Color ButtonColor = new Color(0.16f, 0.17f, 0.22f, 0.95f);

        public static RectTransform CreateCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return (RectTransform)go.transform;
        }

        // 버튼 클릭을 받으려면 EventSystem이 필요하다. 씬에 없으면 만든다.
        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        public static RectTransform Stretch(string name, Transform parent)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Fill(string name, Transform parent, Color color, bool raycast)
        {
            var rt = Stretch(name, parent);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        // 중앙 기준 앵커의 고정 크기 요소
        public static RectTransform Box(string name, Transform parent, Vector2 anchoredPosition, Vector2 size)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;
            return rt;
        }

        public static TextMeshProUGUI Text(UITheme theme, Transform parent, string content, int size, Color color,
            Vector2 anchoredPosition, Vector2 boxSize, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var rt = Box("Text", parent, anchoredPosition, boxSize);
            var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (theme != null && theme.font != null) text.font = theme.font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        public static Button Button(UITheme theme, Transform parent, string label, Vector2 anchoredPosition, Action onClick)
        {
            var rt = Box("Button_" + label, parent, anchoredPosition, new Vector2(420f, 78f));
            var image = rt.gameObject.AddComponent<Image>();
            image.color = ButtonColor;
            var outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = theme != null ? theme.accent : Color.white;
            outline.effectDistance = new Vector2(2f, -2f);

            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.25f, 1.1f, 0.9f, 1f);
            colors.pressedColor = new Color(0.8f, 0.7f, 0.6f, 1f);
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(() => onClick());

            Text(theme, rt, label, 40, Color.white, Vector2.zero, rt.sizeDelta);
            return button;
        }

        public static string FormatTime(float seconds)
        {
            var total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return (total / 60) + ":" + (total % 60).ToString("00");
        }
    }
}
