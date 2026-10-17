using ExpiryGame.Landmark;
using ExpiryGame.Visual.Editor;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ExpiryGame.UI.Editor
{
    // preview_ui.png 레이아웃의 HUD를 열린 씬에 만든다 (Canvas Scaler 1920x1080, Pixel Perfect).
    // 좌표는 미리보기(1280x720) 기준 위치를 1.5배 해서 1920x1080에 옮긴 것이고, 도트 1px = pixelScale(3) 캔버스 px.
    // 다시 실행하면 HUD 캔버스를 지우고 새로 만든다 (손으로 고친 레이아웃은 사라진다).
    public static class UISetup
    {
        public const string ThemePath = "Assets/ShelfLife/UI/UITheme.asset";

        private const string UiDir = "Assets/ShelfLife/UI";
        private const string FontDir = UiDir + "/Fonts";
        private const string HudName = "HUD";
        private const int BarBorder = 8;
        private const int MaxPips = 8;

        private static UITheme theme;

        // 레이아웃 좌표는 pixelScale 3 기준으로 적혀 있다. 실제 배율(기본 2)에 맞춰 정수로 줄인다.
        private static float P(float value)
        {
            return Mathf.Round(value * scale / 3f);
        }
        private static int scale;

        [MenuItem("Tools/ShelfLife/Install HUD To Open Scene")]
        public static void Install()
        {
            Install(true);
        }

        // confirmOverwrite: 이미 HUD가 있으면 덮어쓸지 묻는다 (올인원처럼 새 씬에 설치할 때는 false)
        public static void Install(bool confirmOverwrite)
        {
            var inventory = Object.FindFirstObjectByType<FoodInventory>();
            var clock = Object.FindFirstObjectByType<GameClock>();
            var weapon = Object.FindFirstObjectByType<FoodWeapon>();
            var locator = Object.FindFirstObjectByType<LandmarkLocator>();
            var interactor = Object.FindFirstObjectByType<LandmarkInteractor>();
            if (inventory == null || clock == null || weapon == null || locator == null || interactor == null)
            {
                Debug.LogError("[ShelfLife] 씬에 Player/GameClock/FoodWeapon/랜드마크가 필요합니다. Install Landmarks To Open Scene을 먼저 실행하세요.");
                return;
            }

            var existing = GameObject.Find(HudName);
            if (existing != null && confirmOverwrite &&
                !EditorUtility.DisplayDialog("HUD 다시 설치",
                    "씬에 이미 HUD 캔버스가 있습니다. 지우고 새로 만들면 손으로 바꾼 레이아웃은 사라집니다. 계속할까요?",
                    "덮어쓰기", "취소"))
            {
                Debug.Log("[ShelfLife] HUD 설치를 취소했습니다. 기존 HUD는 그대로입니다.");
                return;
            }

            theme = LoadOrCreateTheme();
            scale = theme.pixelScale;
            if (existing != null) Object.DestroyImmediate(existing);

            var canvas = CreateCanvas();
            var health = inventory.GetComponent<Health>();

            BuildHealth(canvas, health, inventory);
            BuildTime(canvas, clock);
            var bar = BuildSlots(canvas, inventory, weapon, clock);
            BuildDetail(canvas, inventory, weapon, clock, bar);
            BuildArrow(canvas, locator, inventory.transform);
            BuildPrompt(canvas, interactor);
            RecordHudZones(canvas, bar);

            // 임시 OnGUI HUD는 새 HUD로 대체되므로 끈다
            foreach (var old in Object.FindObjectsByType<DebugHud>(FindObjectsSortMode.None)) old.enabled = false;

            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log("[ShelfLife] HUD 설치 완료. 폰트는 Assets/ShelfLife/UI/UITheme.asset 의 font 한 곳에서 교체하세요.");
        }

        // ---------- 테마 ----------

        public static UITheme LoadOrCreateTheme()
        {
            EnsureFolder("Assets/ShelfLife", "UI");
            var asset = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<UITheme>();
                asset.ApplyDefaults();
                AssetDatabase.CreateAsset(asset, ThemePath);
            }

            var s = asset.sprites;
            s.panel = Ui("ui_panel");
            s.panelAccent = Ui("ui_panel_accent");
            s.slot = Ui("ui_slot");
            s.slotSelected = Ui("ui_slot_selected");
            s.barFrame = Ui("ui_bar_frame");
            s.barFill = Ui("ui_bar_fill");
            s.barGhost = Ui("ui_bar_ghost");
            s.heart = Ui("icon_heart");
            s.healIcon = Ui("icon_heal");
            s.weaponIcon = Ui("icon_weapon");
            s.shelfLifeIcon = Ui("icon_shelflife");
            s.sun = Ui("icon_sun");
            s.moon = Ui("icon_moon");
            s.pipFull = Ui("ui_pip_full");
            s.pipEmpty = Ui("ui_pip_empty");
            s.arrow = Ui("ui_arrow");

            if (asset.font == null) asset.font = LoadOrCreateFont();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static Sprite Ui(string name)
        {
            return ArtAssets.Load("ui", name);
        }

        // 프로젝트에 한글 폰트 파일이 없을 때의 임시 상태. 이미 UITheme.font가 연결돼 있으면 건드리지 않는다.
        // 폰트 파일을 프로젝트에 넣은 뒤 Tools > ShelfLife > UI > Create TMP Font From Project Font 로 교체한다.
        private static TMP_FontAsset LoadOrCreateFont()
        {
            Debug.LogWarning("[ShelfLife] UITheme.font가 비어 있어 TMP 기본 폰트를 씁니다 (한글이 네모로 보일 수 있음). 한글 폰트 파일을 넣고 Create TMP Font From Project Font 메뉴를 실행하세요.");
            return TMP_Settings.defaultFontAsset;
        }

        // ---------- 캔버스 ----------

        private static RectTransform CreateCanvas()
        {
            var go = new GameObject(HudName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            canvas.referencePixelsPerUnit = 32f;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 32f;
            return (RectTransform)go.transform;
        }

        // ---------- 좌상단: 체력 ----------

        private static void BuildHealth(RectTransform canvas, Health health, FoodInventory inventory)
        {
            var panel = Panel("HealthPanel", canvas, theme.sprites.panel, new Vector2(0f, 1f), new Vector2(P(36f), P(-36f)), new Vector2(P(570f), P(138f)));
            Icon("Heart", panel, theme.sprites.heart, new Vector2(0f, 1f), new Vector2(P(36f), P(-42f)), new Vector2(20f * scale, 18f * scale));

            var frame = BarFrame("HpBar", panel, new Vector2(P(118f), P(-30f)), new Vector2(P(366f), P(54f)));
            var fill = BarFill("Fill", frame, theme.sprites.barFill, theme.healthFill);
            var ghost = BarFill("Ghost", frame, theme.sprites.barGhost, theme.ghostHeal);
            ghost.GetComponent<Image>().type = Image.Type.Tiled;

            var hp = Text("HpText", panel, TextRole.Number, TextAlignmentOptions.Left, new Vector2(P(120f), P(-111f)), new Vector2(P(180f), P(40f)), true);
            var preview = Text("PreviewText", panel, TextRole.Body, TextAlignmentOptions.Left, new Vector2(P(306f), P(-111f)), new Vector2(P(240f), P(40f)), true);

            var view = panel.gameObject.AddComponent<HealthBarView>();
            Set(view, "theme", theme);
            Set(view, "health", health);
            Set(view, "inventory", inventory);
            Set(view, "fill", fill);
            Set(view, "ghost", ghost);
            Set(view, "ghostImage", ghost.GetComponent<Image>());
            Set(view, "hpText", hp);
            Set(view, "previewText", preview);
        }

        // ---------- 우상단: 일차 / 낮밤 ----------

        private static void BuildTime(RectTransform canvas, GameClock clock)
        {
            var panel = Panel("TimePanel", canvas, theme.sprites.panel, new Vector2(1f, 1f), new Vector2(P(-36f), P(-36f)), new Vector2(P(444f), P(171f)));
            var icon = Icon("PhaseIcon", panel, theme.sprites.sun, new Vector2(0f, 1f), new Vector2(P(27f), P(-30f)), new Vector2(20f * scale, 20f * scale));

            var day = Text("DayText", panel, TextRole.Title, TextAlignmentOptions.Left, new Vector2(P(120f), P(-42f)), new Vector2(P(300f), P(50f)), true);
            var phase = Text("PhaseText", panel, TextRole.Small, TextAlignmentOptions.Left, new Vector2(P(120f), P(-90f)), new Vector2(P(300f), P(36f)), true);

            var frame = BarFrame("TimeBar", panel, new Vector2(P(39f), P(-117f)), new Vector2(P(366f), P(48f)));
            var fill = BarFill("Fill", frame, theme.sprites.barFill, theme.timeFill);
            fill.anchorMax = new Vector2(0f, 1f);

            var view = panel.gameObject.AddComponent<TimeHudView>();
            Set(view, "theme", theme);
            Set(view, "clock", clock);
            Set(view, "phaseIcon", icon.GetComponent<Image>());
            Set(view, "dayText", day);
            Set(view, "phaseText", phase);
            Set(view, "barFill", fill);
        }

        // ---------- 하단 중앙: 음식 슬롯 ----------

        private static FoodSlotBarView BuildSlots(RectTransform canvas, FoodInventory inventory, FoodWeapon weapon, GameClock clock)
        {
            var bar = Rect("SlotBar", canvas, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(P(1000f), P(200f)));
            var capacity = Mathf.Max(1, inventory.Capacity);
            var slots = new Object[capacity];
            var pitch = P(165f);

            for (var i = 0; i < capacity; i++)
            {
                var x = (i - (capacity - 1) * 0.5f) * pitch;
                slots[i] = BuildSlot(bar, i, new Vector2(x, 48f));
            }

            var view = bar.gameObject.AddComponent<FoodSlotBarView>();
            Set(view, "inventory", inventory);
            Set(view, "weapon", weapon);
            Set(view, "clock", clock);
            SetArray(view, "slots", slots);
            return view;
        }

        private static FoodSlotView BuildSlot(RectTransform parent, int index, Vector2 position)
        {
            var slot = Rect($"Slot{index + 1}", parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), position, new Vector2(48f * scale, 48f * scale));
            var frame = slot.gameObject.AddComponent<Image>();
            frame.sprite = theme.sprites.slot;
            frame.raycastTarget = false;

            var icon = Icon("Icon", slot, null, new Vector2(0.5f, 0.5f), new Vector2(0f, P(8f)), new Vector2(32f * scale, 32f * scale));
            var number = Text("Number", slot, TextRole.Small, TextAlignmentOptions.TopLeft, new Vector2(P(8f), P(-4f)), new Vector2(P(40f), P(28f)), false);
            var pips = PipRow("Pips", slot, new Vector2(0f, 0f), new Vector2(P(12f), P(12f)));

            var freshness = Rect("Freshness", slot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, P(-6f)), new Vector2(P(132f), P(12f)));
            var back = freshness.gameObject.AddComponent<Image>();
            back.sprite = theme.sprites.barFill;
            back.color = new Color(0.05f, 0.06f, 0.08f, 0.9f);
            back.raycastTarget = false;
            var fill = BarFill("Fill", freshness, theme.sprites.barFill, Color.white);
            fill.anchorMax = new Vector2(0f, 1f);

            var view = slot.gameObject.AddComponent<FoodSlotView>();
            Set(view, "theme", theme);
            Set(view, "frame", frame);
            Set(view, "number", number);
            Set(view, "icon", icon.GetComponent<Image>());
            Set(view, "pips", pips);
            Set(view, "freshFill", fill);
            Set(view, "freshFillImage", fill.GetComponent<Image>());
            return view;
        }

        // ---------- 선택 슬롯 위 상세 패널 ----------

        private static void BuildDetail(RectTransform canvas, FoodInventory inventory, FoodWeapon weapon, GameClock clock, FoodSlotBarView bar)
        {
            // 우하단 구석 (슬롯 바 옆). 플레이어(화면 중앙)와 그 아래를 가리지 않는다. Tab 또는 2초 미리보기로만 보인다.
            var panel = Panel("DetailPanel", canvas, theme.sprites.panelAccent, new Vector2(1f, 0f), new Vector2(P(-36f), P(36f)), new Vector2(P(488f), P(360f)));
            panel.gameObject.SetActive(false);

            var icon = Icon("Icon", panel, null, new Vector2(0f, 1f), new Vector2(P(30f), P(-24f)), new Vector2(32f * scale, 32f * scale));
            var nameText = Text("Name", panel, TextRole.Title, TextAlignmentOptions.Left, new Vector2(P(140f), P(-48f)), new Vector2(P(330f), P(50f)), true);
            var hint = Text("Hint", panel, TextRole.Small, TextAlignmentOptions.Left, new Vector2(P(140f), P(-92f)), new Vector2(P(340f), P(30f)), true);

            Icon("EatIcon", panel, theme.sprites.healIcon, new Vector2(0f, 1f), new Vector2(P(30f), P(-153f)), new Vector2(16f * scale, 16f * scale), centered: true);
            var eat = Text("EatText", panel, TextRole.Body, TextAlignmentOptions.Left, new Vector2(P(88f), P(-153f)), new Vector2(P(380f), P(40f)), true);
            Icon("WeaponIcon", panel, theme.sprites.weaponIcon, new Vector2(0f, 1f), new Vector2(P(30f), P(-204f)), new Vector2(20f * scale, 20f * scale), centered: true);
            var damage = Text("DamageText", panel, TextRole.Body, TextAlignmentOptions.Left, new Vector2(P(88f), P(-204f)), new Vector2(P(380f), P(40f)), true);
            var uses = Text("UsesText", panel, TextRole.Body, TextAlignmentOptions.Left, new Vector2(P(88f), P(-252f)), new Vector2(P(240f), P(40f)), true);
            var pips = PipRow("UsePips", panel, new Vector2(0f, 1f), new Vector2(P(330f), P(-246f)));
            Icon("ShelfIcon", panel, theme.sprites.shelfLifeIcon, new Vector2(0f, 1f), new Vector2(P(30f), P(-300f)), new Vector2(16f * scale, 16f * scale), centered: true);
            var shelf = Text("ShelfText", panel, TextRole.Body, TextAlignmentOptions.Left, new Vector2(P(88f), P(-300f)), new Vector2(P(380f), P(40f)), true);

            // 패널 자체는 숨겨졌다 켜지므로, 구독을 유지할 컴포넌트는 캔버스 쪽 별도 오브젝트에 둔다
            var host = Rect("DetailPanelController", canvas, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            host.offsetMin = host.offsetMax = Vector2.zero;
            var view = host.gameObject.AddComponent<FoodDetailPanelView>();
            Set(view, "theme", theme);
            Set(view, "inventory", inventory);
            Set(view, "weapon", weapon);
            Set(view, "clock", clock);
            Set(view, "panel", panel);
            Set(view, "icon", icon.GetComponent<Image>());
            Set(view, "nameText", nameText);
            Set(view, "hintText", hint);
            Set(view, "eatText", eat);
            Set(view, "damageText", damage);
            Set(view, "usesText", uses);
            Set(view, "pips", pips);
            Set(view, "shelfText", shelf);
        }

        // 화살표/라벨이 피할 HUD 점유 영역을 실제 패널 크기에서 읽어 UITheme에 기록한다 (패널 크기를 바꾸고 재설치하면 따라간다)
        private static void RecordHudZones(RectTransform canvas, FoodSlotBarView bar)
        {
            var zones = new System.Collections.Generic.List<HudZone>();
            AddZone(zones, canvas, "HealthPanel", new Vector2(0f, 1f));
            AddZone(zones, canvas, "TimePanel", new Vector2(1f, 1f));
            AddZone(zones, canvas, "DetailPanel", new Vector2(1f, 0f));
            zones.Add(new HudZone
            {
                name = "SlotBar",
                corner = new Vector2(0.5f, 0f),
                size = new Vector2(bar.SlotCount * P(165f), P(48f) + 48f * scale + P(18f))
            });
            theme.hudZones = zones.ToArray();
            EditorUtility.SetDirty(theme);
        }

        private static void AddZone(System.Collections.Generic.List<HudZone> zones, RectTransform canvas, string name, Vector2 corner)
        {
            var rect = (RectTransform)canvas.Find(name);
            zones.Add(new HudZone
            {
                name = name,
                corner = corner,
                size = rect.sizeDelta + new Vector2(Mathf.Abs(rect.anchoredPosition.x), Mathf.Abs(rect.anchoredPosition.y))
            });
        }

        // ---------- 랜드마크 화살표 / 상호작용 안내 ----------

        private static void BuildArrow(RectTransform canvas, LandmarkLocator locator, Transform player)
        {
            var area = Rect("LandmarkArrowArea", canvas, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            area.offsetMin = area.offsetMax = Vector2.zero;

            var arrow = Icon("Arrow", area, theme.sprites.arrow, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20f * scale, 20f * scale), centered: true);
            var label = Text("Label", area, TextRole.Body, TextAlignmentOptions.Center, Vector2.zero, new Vector2(P(320f), P(40f)), true);
            label.color = theme.landmarkLabel;
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            label.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            var view = area.gameObject.AddComponent<LandmarkArrowView>();
            Set(view, "theme", theme);
            Set(view, "locator", locator);
            Set(view, "player", player);
            Set(view, "viewCamera", Camera.main);
            Set(view, "area", area);
            Set(view, "arrow", arrow);
            Set(view, "label", label);
        }

        private static void BuildPrompt(RectTransform canvas, LandmarkInteractor interactor)
        {
            var panel = Panel("InteractPrompt", canvas, theme.sprites.panel, new Vector2(0.5f, 0.5f), new Vector2(0f, P(-110f)), new Vector2(P(260f), P(66f)));
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = new Vector2(0f, P(-110f));
            var label = Text("Label", panel, TextRole.Body, TextAlignmentOptions.Center, Vector2.zero, new Vector2(P(240f), P(50f)), true);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            label.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            // 패널이 꺼져도 구독이 유지되도록 컨트롤러는 캔버스 쪽에 둔다
            var host = Rect("InteractPromptController", canvas, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            host.offsetMin = host.offsetMax = Vector2.zero;
            var view = host.gameObject.AddComponent<InteractPromptView>();
            Set(view, "interactor", interactor);
            Set(view, "root", panel.gameObject);
            Set(view, "label", label);
        }

        // ---------- 생성 도우미 ----------

        private static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        // 9-slice 패널. 앵커/피벗은 모서리 기준 (corner = (0,1) 좌상단, (1,1) 우상단, (0.5,0) 하단 중앙 …)
        private static RectTransform Panel(string name, RectTransform parent, Sprite sprite, Vector2 corner, Vector2 position, Vector2 size)
        {
            var rect = Rect(name, parent, corner, corner, corner, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f / scale;
            image.raycastTarget = false;
            return rect;
        }

        private static RectTransform BarFrame(string name, RectTransform parent, Vector2 topLeftOffset, Vector2 size)
        {
            var rect = Rect(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(topLeftOffset.x, topLeftOffset.y), size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = theme.sprites.barFrame;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 4f / BarBorder;   // 보더 4px → 캔버스 BarBorder px
            image.raycastTarget = false;

            // 채움 영역: 프레임 안쪽 (보더 두께만큼 안으로)
            var inner = Rect("Inner", rect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            inner.offsetMin = new Vector2(BarBorder, BarBorder);
            inner.offsetMax = new Vector2(-BarBorder, -BarBorder);
            return inner;
        }

        // 앵커 x 범위로 채움 길이를 조절하는 바 (HealthBarView/TimeHudView/FoodSlotView가 anchorMax.x를 바꾼다)
        private static RectTransform BarFill(string name, RectTransform parent, Sprite sprite, Color color)
        {
            var rect = Rect(name, parent, Vector2.zero, Vector2.one, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static RectTransform Icon(string name, RectTransform parent, Sprite sprite, Vector2 anchor, Vector2 position, Vector2 size, bool centered = false)
        {
            // centered: position이 아이콘 중심(좌상단 앵커 기준), 아니면 아이콘의 좌상단/앵커 기준
            var pivot = centered ? new Vector2(0.5f, 0.5f) : (anchor.y >= 1f ? new Vector2(anchor.x, 1f) : new Vector2(0.5f, 0.5f));
            var rect = Rect(name, parent, anchor, anchor, pivot, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            image.preserveAspect = true;
            return rect;
        }

        private static PipRow PipRow(string name, RectTransform parent, Vector2 anchor, Vector2 position)
        {
            var row = Rect(name, parent, anchor, anchor, new Vector2(0f, anchor.y >= 1f ? 1f : 0f), position, new Vector2(P(MaxPips * 15f), P(12f)));
            var pips = new Object[MaxPips];
            for (var i = 0; i < MaxPips; i++)
            {
                var pip = Rect($"Pip{i}", row, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(P(i * 15f), 0f), new Vector2(P(12f), P(12f)));
                var image = pip.gameObject.AddComponent<Image>();
                image.sprite = theme.sprites.pipFull;
                image.raycastTarget = false;
                pips[i] = image;
            }
            var component = row.gameObject.AddComponent<PipRow>();
            Set(component, "theme", theme);
            SetArray(component, "pips", pips);
            return component;
        }

        private static TMP_Text Text(string name, RectTransform parent, TextRole role, TextAlignmentOptions alignment, Vector2 position, Vector2 size, bool topLeft)
        {
            // position은 좌상단 앵커 기준 텍스트 영역의 (왼쪽 가운데) 위치. topLeft=false면 패널 좌상단 모서리에 글자 영역을 붙인다.
            var rect = Rect(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, topLeft ? 0.5f : 1f), position, size);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            rect.gameObject.AddComponent<ThemedText>().Setup(theme, role, false);
            return text;
        }

        private static void Set(Object target, string field, Object value)
        {
            DarkArtSceneInstaller.SetObject(target, field, value);
        }

        private static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}")) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
