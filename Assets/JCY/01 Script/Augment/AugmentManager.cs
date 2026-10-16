using System.Collections.Generic;
using UnityEngine;

public class AugmentManager : MonoBehaviour
{
    [Header("Augment Data")]
    [SerializeField] private List<AugmentSO> allAugments;

    [Header("References")]
    [SerializeField] private AugmentUI augmentUI;
    [SerializeField] private AugmentState playerState;

    [Header("Settings")]
    [SerializeField] private int choiceCount = 3;

    // 증강별 현재 레벨
    private readonly Dictionary<AugmentSO, int> _augmentLevels = new();

    // 현재 선택 가능한 증강
    private readonly HashSet<AugmentSO> _currentChoices = new();

    private bool _isSelecting;

    private void Awake()
    {
        augmentUI.Initialize(this);
    }

    public void OpenAugmentSelection()
    {
        if (_isSelecting)
            return;

        List<AugmentSO> candidates = new();

       
        foreach (AugmentSO augment in allAugments)
        {
            if (augment == null)
                continue;

            int currentLevel = GetAugmentLevel(augment);
            int maxLevel = Mathf.Max(1, augment.maxLevel);

            // 증강 자체의 최대 레벨 확인
            if (currentLevel >= maxLevel)
                continue;

            // 스킬 증강은 SkillSO도 확인
            if (augment.augmentType == AugmentType.Skill)
            {
                if (augment.skill == null)
                    continue;

                if (playerState.GetSkillLevel(augment.skill)
                    >= augment.skill.maxLevel)
                    continue;
            }

            // 중복 등록 방지
            if (!candidates.Contains(augment))
                candidates.Add(augment);
        }


        if (candidates.Count == 0)
        {
            Debug.Log("더 이상 획득할 수 있는 증강이 없습니다.");
            return;
        }

        // 후보 섞기
        for (int i = 0; i < candidates.Count; i++)
        {
            int randomIndex = Random.Range(i, candidates.Count);

            (candidates[i], candidates[randomIndex]) =
                (candidates[randomIndex], candidates[i]);
        }

        int count = Mathf.Min(choiceCount, candidates.Count);
        AugmentSO[] selected = candidates.GetRange(0, count).ToArray();

        _currentChoices.Clear();

        foreach (AugmentSO augment in selected)
        {
            _currentChoices.Add(augment);
        }

        _isSelecting = true;
        augmentUI.ShowAugments(selected);
    }

    public bool ChooseAugment(AugmentSO augment)
    {
        if (!_isSelecting || !_currentChoices.Contains(augment))
            return false;

        int currentLevel = GetAugmentLevel(augment);
        int maxLevel = Mathf.Max(1, augment.maxLevel);

        if (currentLevel >= maxLevel)
            return false;

        // 스킬 증강은 스킬 레벨을 먼저 확인
        if (augment.augmentType == AugmentType.Skill)
        {
            if (augment.skill == null)
                return false;

            if (playerState.GetSkillLevel(augment.skill)
                >= augment.skill.maxLevel)
                return false;

            if (!playerState.AcquireSkill(augment.skill))
                return false;
        }
        else
        {
            playerState.ApplyAugment(augment);
        }

        _augmentLevels[augment] = currentLevel + 1;

        _isSelecting = false;
        _currentChoices.Clear();

        return true;
    }

    public int GetAugmentLevel(AugmentSO augment)
    {
        return _augmentLevels.TryGetValue(augment, out int level)
            ? level
            : 0;
    }
}