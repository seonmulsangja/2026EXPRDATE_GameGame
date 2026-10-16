using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ExpiryGame.Landmark
{
    // 가까이 가서 키를 눌러 쓰는 대상 (편의점 등)
    public interface ILandmarkInteractable
    {
        Vector2 Position { get; }
        float InteractRange { get; }
        bool CanInteract { get; }
        string PromptText { get; }
        void Interact();
    }

    // UI가 "지금 누를 수 있는 상호작용"을 구독하는 계약. 대상이 없으면 null.
    public interface IInteractionPrompt
    {
        event Action<string> PromptChanged;
        string CurrentPrompt { get; }
    }

    public static class InteractableRegistry
    {
        private static readonly List<ILandmarkInteractable> items = new List<ILandmarkInteractable>();

        public static IReadOnlyList<ILandmarkInteractable> Items => items;

        public static void Register(ILandmarkInteractable item)
        {
            if (!items.Contains(item)) items.Add(item);
        }

        public static void Unregister(ILandmarkInteractable item)
        {
            items.Remove(item);
        }
    }
}
