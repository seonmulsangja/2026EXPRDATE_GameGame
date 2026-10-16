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

        // 기본적으로 증강 SO 정보 사용
        Sprite icon = augment.icon;
        string displayName = augment.augmentName;
        string displayDescription = augment.description;

        // 스킬 증강이면 SkillSO 정보 사용
        if (augment.augmentType == AugmentType.Skill
            && augment.skill != null)
        {
            SkillSO skill = augment.skill;

            icon = skill.icon;
            displayName = skill.skillName;
            displayDescription = skill.description;
        }

        iconImage.sprite = icon;
        nameText.text = displayName;
        descriptionText.text = displayDescription;

        int currentLevel = _augmentUI.GetAugmentLevel(augment);

        if (levelText != null)
        {
            if (augment.augmentType == AugmentType.Skill
                && augment.skill != null)
            {
                int skillLevel = currentLevel;

                levelText.text = skillLevel > 0
                    ? $"Lv.{skillLevel} → Lv.{skillLevel + 1}"
                    : "NEW";
            }
            else
            {
                levelText.text = currentLevel > 0
                    ? $"Lv.{currentLevel} → Lv.{currentLevel + 1}"
                    : "NEW";
            }
        }

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnSelect);
    }

    private void OnSelect()
    {
        _augmentUI.SelectAugment(_augment);
    }
}