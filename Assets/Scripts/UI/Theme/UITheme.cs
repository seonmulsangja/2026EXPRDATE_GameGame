using System;
using TMPro;
using UnityEngine;

namespace ExpiryGame.UI
{
    public enum TextRole
    {
        Title,
        Body,
        Small,
        Number
    }

    [Serializable]
    public struct TextStyle
    {
        public int size;
        public Color color;
    }

    [Serializable]
    public class UISprites
    {
        public Sprite panel;
        public Sprite panelAccent;
        public Sprite slot;
        public Sprite slotSelected;
        public Sprite barFrame;
        public Sprite barFill;
        public Sprite barGhost;
        public Sprite heart;
        public Sprite healIcon;
        public Sprite weaponIcon;
        public Sprite shelfLifeIcon;
        public Sprite sun;
        public Sprite moon;
        public Sprite pipFull;
        public Sprite pipEmpty;
        public Sprite arrow;
    }

    // UI 폰트/색/간격/문구/스프라이트를 한 곳에서 관리한다.
    // 화면 모서리에 붙은 HUD가 차지하는 영역 (캔버스 px). 화살표/라벨 같은 떠 있는 UI는 이 밖에만 놓는다.
    [Serializable]
    public struct HudZone
    {
        public string name;
        [Tooltip("화면 모서리/가장자리 기준점 (0,0)=좌하 (1,1)=우상 (0.5,0)=하단 중앙")]
        public Vector2 corner;
        public Vector2 size;
    }

    // 한글 픽셀 폰트를 바꾸려면 이 에셋의 font 하나만 교체하면 된다 (모든 텍스트는 ThemedText가 여기서 폰트를 받는다).
    [CreateAssetMenu(menuName = "ShelfLife/UI Theme", fileName = "UITheme")]
    public class UITheme : ScriptableObject
    {
        [Header("폰트 (유일한 지정 지점)")]
        public TMP_FontAsset font;

        [Header("해상도")]
        [Tooltip("원본 도트 1px = 캔버스 몇 px (1080p 기준 3이면 정수 배율이라 픽셀이 깨지지 않는다)")]
        [Min(1)] public int pixelScale = 2;
        [Tooltip("텍스트 크기는 pixelScale 3 기준으로 적혀 있다. 이 값으로 비율을 맞춘다.")]
        public float TextScale => pixelScale / 3f;

        [Header("텍스트 스타일")]
        public TextStyle title = new TextStyle { size = 36, color = Color.white };
        public TextStyle body = new TextStyle { size = 28, color = Color.white };
        public TextStyle small = new TextStyle { size = 20, color = new Color(0.62f, 0.66f, 0.72f) };
        public TextStyle number = new TextStyle { size = 26, color = Color.white };

        [Header("색")]
        public Color accent = new Color(0.878f, 0.475f, 0.227f);   // #E0793A
        public Color healthFill = new Color(0.85f, 0.2f, 0.2f);
        public Color timeFill = new Color(0.878f, 0.475f, 0.227f);
        public Color healText = new Color(0.35f, 0.85f, 0.4f);
        public Color shelfText = new Color(0.95f, 0.78f, 0.25f);
        public Color dangerText = new Color(0.95f, 0.3f, 0.3f);
        public Color ghostHeal = new Color(0.35f, 0.9f, 0.4f, 0.9f);
        public Color ghostDamage = new Color(1f, 0.3f, 0.3f, 0.9f);
        public Color landmarkLabel = new Color(0.45f, 0.9f, 1f);
        public Gradient freshness = new Gradient();   // x: 0 = 썩기 직전(빨강) → 1 = 신선(초록)

        [Header("HUD 안전 영역 (UISetup이 실제 패널 크기로 채운다)")]
        [Tooltip("이 영역들 밖으로만 랜드마크 화살표/라벨을 배치한다")]
        public HudZone[] hudZones = new HudZone[0];
        [Tooltip("점유 영역 바깥으로 더 띄우는 여백 (캔버스 px)")]
        public float hudZoneGap = 12f;
        [Tooltip("DetailPanel 미리보기 표시 시간 (초). Tab으로 고정 표시를 켜고 끈다.")]
        [Min(0f)] public float detailPreviewSeconds = 2f;

        [Header("간격 (원본 도트 px, pixelScale 곱해서 사용)")]
        public int screenMargin = 8;
        public int slotSpacing = 4;

        [Header("문구")]
        public string dayFormat = "{0}일차";
        public string dayLabel = "낮";
        public string nightLabel = "밤";
        public string hpFormat = "{0} / {1}";
        public string previewFormat = "{0} 예상";
        public string eatLine = "먹으면 HP {0}";
        public string weaponLine = "무기 데미지 {0}";
        public string usesLine = "남은 사용 {0}/{1}";
        public string shelfLine = "유통기한 {0}";
        public string rottenLabel = "썩음";
        public string controlsHint = "E(길게): 먹기 · 좌클릭: 휘두르기";
        public string landmarkFormat = "{0} · {1}m";

        public UISprites sprites = new UISprites();

        // 중심 원점 좌표계의 점유 영역 목록 (캔버스 크기 기준)
        public void GetZoneRects(Vector2 canvasSize, System.Collections.Generic.List<Rect> result)
        {
            result.Clear();
            foreach (var zone in hudZones)
            {
                var anchor = new Vector2((zone.corner.x - 0.5f) * canvasSize.x, (zone.corner.y - 0.5f) * canvasSize.y);
                var min = anchor - new Vector2(zone.corner.x * zone.size.x, zone.corner.y * zone.size.y);
                result.Add(new Rect(min, zone.size));
            }
        }

        public TextStyle StyleFor(TextRole role)
        {
            switch (role)
            {
                case TextRole.Title: return title;
                case TextRole.Small: return small;
                case TextRole.Number: return number;
                default: return body;
            }
        }

        public Color FreshnessColor(float shelfLife01)
        {
            return freshness.Evaluate(Mathf.Clamp01(shelfLife01));
        }

        // 신선도 바 기본: 초록 > 노랑 > 빨강
        public void ApplyDefaults()
        {
            var g = new Gradient();
            g.SetKeys(new[]
            {
                new GradientColorKey(new Color(0.9f, 0.25f, 0.22f), 0f),
                new GradientColorKey(new Color(0.95f, 0.75f, 0.2f), 0.45f),
                new GradientColorKey(new Color(0.35f, 0.8f, 0.35f), 1f),
            }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            freshness = g;
        }
    }
}
