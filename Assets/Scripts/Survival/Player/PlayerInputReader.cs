using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ExpiryGame
{
    public class PlayerInputReader : MonoBehaviour, IPlayerInput
    {
        [SerializeField] private InputActionReference move;
        [SerializeField] private InputActionReference eat;
        [SerializeField] private InputActionReference attack;
        [SerializeField] private InputActionReference nextFood;

        public Vector2 Move => move.action.ReadValue<Vector2>();

        public event Action EatPerformed;
        public event Action AttackPerformed;
        public event Action NextFoodPerformed;

        private void OnEnable()
        {
            Bind(move, null);
            Bind(eat, OnEat);
            Bind(attack, OnAttack);
            Bind(nextFood, OnNextFood);
        }

        private void OnDisable()
        {
            Unbind(move, null);
            Unbind(eat, OnEat);
            Unbind(attack, OnAttack);
            Unbind(nextFood, OnNextFood);
        }

        private void OnEat(InputAction.CallbackContext context) => EatPerformed?.Invoke();
        private void OnAttack(InputAction.CallbackContext context) => AttackPerformed?.Invoke();
        private void OnNextFood(InputAction.CallbackContext context) => NextFoodPerformed?.Invoke();

        private static void Bind(InputActionReference reference, Action<InputAction.CallbackContext> handler)
        {
            if (handler != null) reference.action.performed += handler;
            reference.action.Enable();
        }

        private static void Unbind(InputActionReference reference, Action<InputAction.CallbackContext> handler)
        {
            if (handler != null) reference.action.performed -= handler;
            reference.action.Disable();
        }
    }
}
