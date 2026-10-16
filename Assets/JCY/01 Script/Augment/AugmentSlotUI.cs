using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AugmentSlotUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Button selectButton;

    private AugmentSO _augment;
    private AugmentUI _augmentUI;

    public void Setup(AugmentSO augment, AugmentUI augmentUI)
    {
        _augment = augment;
        _augmentUI = augmentUI;

        iconImage.sprite = augment.icon;
        nameText.text = augment.augmentName;
        descriptionText.text = augment.description;

        int currentLevel = _augmentUI.GetAugmentLevel(augment);

        if (levelText != null)
        {
            levelText.text = currentLevel > 0
                ? $"Lv.{currentLevel} → Lv.{currentLevel + 1}"
                : "NEW";
        }

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnSelect);
    }

    private void OnSelect()
    {
        _augmentUI.SelectAugment(_augment);
    }
}