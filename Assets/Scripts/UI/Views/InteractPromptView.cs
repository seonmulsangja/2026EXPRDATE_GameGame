using TMPro;
using UnityEngine;
using ExpiryGame.Landmark;

namespace ExpiryGame.UI
{
    // 상호작용 가능한 랜드마크 근처에서만 "F  열기" 안내를 보여준다 (IInteractionPrompt 구독).
    public class InteractPromptView : MonoBehaviour
    {
        [SerializeField] private LandmarkInteractor interactor;
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text label;

        private void OnEnable()
        {
            ((IInteractionPrompt)interactor).PromptChanged += OnPromptChanged;
            OnPromptChanged(((IInteractionPrompt)interactor).CurrentPrompt);
        }

        private void OnDisable()
        {
            ((IInteractionPrompt)interactor).PromptChanged -= OnPromptChanged;
        }

        private void OnPromptChanged(string prompt)
        {
            root.SetActive(!string.IsNullOrEmpty(prompt));
            if (!string.IsNullOrEmpty(prompt)) label.text = prompt;
        }
    }
}
