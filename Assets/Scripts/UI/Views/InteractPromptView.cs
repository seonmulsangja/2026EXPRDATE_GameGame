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
        [Tooltip("비어 있으면 라벨의 ThemedText에서 가져온다")]
        [SerializeField] private UITheme theme;

        private AutoSizePanel fitter;

        private void Awake()
        {
            // HUD를 다시 설치하지 않은 이전 씬에서도 박스가 글자에 맞춰 늘어나도록 필요하면 직접 붙인다
            var rect = (RectTransform)root.transform;
            fitter = root.GetComponent<AutoSizePanel>();
            if (fitter == null) fitter = root.AddComponent<AutoSizePanel>();
            if (theme == null) theme = label.GetComponent<ThemedText>()?.Theme;
            fitter.Setup(theme, rect, label);
        }

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
            if (string.IsNullOrEmpty(prompt)) return;
            if (theme != null) fitter.SetText(prompt);
            else label.text = prompt;
        }
    }
}
