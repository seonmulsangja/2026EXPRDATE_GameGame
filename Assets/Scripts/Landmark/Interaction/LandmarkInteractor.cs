using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ExpiryGame.Landmark
{
    // 플레이어 쪽: 범위 안 가장 가까운 상호작용 대상을 찾아 프롬프트로 알리고, 키가 눌리면 실행한다.
    // 키는 기존 입력 에셋을 건드리지 않도록 이 컴포넌트가 InputAction을 직접 가진다.
    public class LandmarkInteractor : MonoBehaviour, IInteractionPrompt
    {
        [SerializeField] private Transform player;
        [SerializeField] private string binding = "<Keyboard>/f";
        [Tooltip("프롬프트 앞에 붙는 키 표기")]
        [SerializeField] private string keyLabel = "F";

        private InputAction action;
        private ILandmarkInteractable target;

        public string CurrentPrompt { get; private set; }
        public event Action<string> PromptChanged;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private string debugPrompt;

        // 디버그 메뉴 전용: 박스에 글자가 들어오는지 보려고 안내 문구를 강제로 띄운다 (null이면 끔)
        public void SetDebugPrompt(string prompt)
        {
            debugPrompt = prompt;
        }
#endif

        private void OnEnable()
        {
            action = new InputAction("LandmarkInteract", InputActionType.Button, binding);
            action.performed += OnPerformed;
            action.Enable();
        }

        private void OnDisable()
        {
            action.performed -= OnPerformed;
            action.Dispose();
            SetPrompt(null);
        }

        private void Update()
        {
            target = FindNearest();
            var prompt = target != null ? $"{keyLabel}  {target.PromptText}" : null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (debugPrompt != null) prompt = debugPrompt;
#endif
            SetPrompt(prompt);
        }

        private void OnPerformed(InputAction.CallbackContext context)
        {
            if (target != null && target.CanInteract) target.Interact();
        }

        private ILandmarkInteractable FindNearest()
        {
            ILandmarkInteractable best = null;
            var bestDistance = float.MaxValue;
            var items = InteractableRegistry.Items;
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (!item.CanInteract) continue;
                var distance = Vector2.Distance(player.position, item.Position);
                if (distance > item.InteractRange || distance >= bestDistance) continue;
                best = item;
                bestDistance = distance;
            }
            return best;
        }

        private void SetPrompt(string prompt)
        {
            if (prompt == CurrentPrompt) return;
            CurrentPrompt = prompt;
            PromptChanged?.Invoke(prompt);
        }
    }
}
