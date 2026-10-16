using UnityEngine;

public class AugmentUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private AugmentSlotUI[] slots;

    private AugmentManager _manager;
    private float _previousTimeScale = 1f;

    private void Awake()
    {
        panel.SetActive(false);
    }

    public void Initialize(AugmentManager manager)
    {
        _manager = manager;
    }

    public int GetAugmentLevel(AugmentSO augment)
    {
        return _manager.GetAugmentLevel(augment);
    }

    public void ShowAugments(AugmentSO[] augments)
    {
        _previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        panel.SetActive(true);

        for (int i = 0; i < slots.Length; i++)
        {
            bool hasAugment = i < augments.Length;

            slots[i].gameObject.SetActive(hasAugment);

            if (hasAugment)
            {
                slots[i].Setup(augments[i], this);
            }
        }
    }

    public void SelectAugment(AugmentSO augment)
    {
        if (!_manager.ChooseAugment(augment))
            return;

        panel.SetActive(false);
        Time.timeScale = _previousTimeScale;
    }
}