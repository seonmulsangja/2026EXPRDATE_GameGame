using UnityEngine;

public class AugmentChest : MonoBehaviour
{
    private AugmentManager _augmentManager;
    private bool _isOpened;

    private void Awake()
    {
        _augmentManager = FindFirstObjectByType<AugmentManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isOpened)
            return;

        if (!other.CompareTag("Player"))
            return;

        if (_augmentManager == null)
        {
            Debug.LogWarning("AugmentManager를 찾을 수 없습니다.");
            return;
        }

        _isOpened = true;
        _augmentManager.OpenAugmentSelection();
    }
}