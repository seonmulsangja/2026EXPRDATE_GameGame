using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ExpiryGame.UI
{
    // 화면 우하단 구석의 상세 패널(ui_panel_accent). 평소엔 숨기고, Tab으로 고정 표시를 켜고 끈다.
    // 선택이 바뀌거나 선택 음식이 썩으면 theme.detailPreviewSeconds 동안만 미리 보여준다. 값은 FoodItem이 이미 현재 신선도를 반영해 가진 것을 읽는다
    // (CurrentHeal / CurrentDamage / RemainingDurability / RemainingShelfLife). 유통기한 텍스트는 초가 바뀔 때만 갱신.
    public class FoodDetailPanelView : MonoBehaviour
    {
        [SerializeField] private UITheme theme;
        [SerializeField] private FoodInventory inventory;
        [SerializeField] private FoodWeapon weapon;
        [SerializeField] private GameClock clock;
        [SerializeField] private RectTransform panel;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text hintText;
        [SerializeField] private TMP_Text eatText;
        [SerializeField] private TMP_Text damageText;
        [SerializeField] private TMP_Text usesText;
        [SerializeField] private PipRow pips;
        [SerializeField] private TMP_Text shelfText;

        private FoodItem watched;
        private int shownSecond = -1;
        private bool pinned;
        private float previewLeft;

        private void OnEnable()
        {
            ((IFoodInventory)inventory).Changed += OnInventoryChanged;
            weapon.WeaponUsed += OnWeaponUsed;
            ((IGameClock)clock).Ticked += OnTicked;
            hintText.text = theme.controlsHint;
            OnInventoryChanged();
        }

        private void OnDisable()
        {
            ((IFoodInventory)inventory).Changed -= OnInventoryChanged;
            weapon.WeaponUsed -= OnWeaponUsed;
            ((IGameClock)clock).Ticked -= OnTicked;
            Watch(null);
        }

        private void OnInventoryChanged()
        {
            var foods = (IFoodInventory)inventory;
            var previous = watched;
            Watch(foods.Selected);
            if (watched == null)
            {
                previewLeft = 0f;
                SetVisible(false);
                return;
            }

            RefreshAll();
            // 같은 음식의 개수/상태 변화(먹기, 줍기 등)에는 미리보기를 다시 켜지 않는다
            if (watched != previous) StartPreview();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame && watched != null)
            {
                pinned = !pinned;
                if (pinned) previewLeft = 0f;
            }
            if (previewLeft > 0f) previewLeft -= Time.unscaledDeltaTime;
            SetVisible(watched != null && (pinned || previewLeft > 0f));
        }

        private void StartPreview()
        {
            previewLeft = theme.detailPreviewSeconds;
            SetVisible(true);
        }

        private void SetVisible(bool visible)
        {
            if (panel.gameObject.activeSelf != visible) panel.gameObject.SetActive(visible);
        }

        private void OnWeaponUsed(FoodItem item, int hitCount)
        {
            if (item == watched) RefreshUses();
        }

        private void OnRotted(FoodItem item)
        {
            RefreshAll();
            StartPreview();
        }

        private void OnTicked(float _)
        {
            if (watched != null) RefreshShelfLife();
        }

        private void Watch(FoodItem item)
        {
            if (watched != null) watched.Rotted -= OnRotted;
            watched = item;
            if (watched != null) watched.Rotted += OnRotted;
        }

        private void RefreshAll()
        {
            icon.sprite = watched.Data.icon != null ? watched.Data.icon : watched.Data.sprite;
            nameText.text = watched.Data.displayName;

            var heal = Mathf.RoundToInt(watched.CurrentHeal);
            eatText.color = heal >= 0 ? theme.healText : theme.dangerText;
            eatText.text = string.Format(theme.eatLine, heal >= 0 ? $"+{heal}" : heal.ToString());
            damageText.text = string.Format(theme.weaponLine, Mathf.RoundToInt(watched.CurrentDamage));

            RefreshUses();
            shownSecond = -1;
            RefreshShelfLife();
        }

        private void RefreshUses()
        {
            usesText.text = string.Format(theme.usesLine, watched.RemainingDurability, watched.Stats.MaxDurability);
            pips.Set(watched.RemainingDurability, watched.Stats.MaxDurability);
        }

        private void RefreshShelfLife()
        {
            var second = watched.IsRotten ? -2 : Mathf.CeilToInt(watched.RemainingShelfLife);
            if (second == shownSecond) return;
            shownSecond = second;
            if (watched.IsRotten)
            {
                shelfText.color = theme.dangerText;
                shelfText.text = string.Format(theme.shelfLine, theme.rottenLabel);
                return;
            }
            shelfText.color = theme.shelfText;
            shelfText.text = string.Format(theme.shelfLine, $"{second / 60}:{second % 60:00}");
        }
    }
}
