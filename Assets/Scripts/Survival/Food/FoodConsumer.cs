using System;
using UnityEngine;

namespace ExpiryGame
{
    // 먹기: E를 holdSeconds(EatSettings) 동안 누르면 선택 음식을 먹는다. 중간에 놓거나 선택 음식이 바뀌면 즉시 취소.
    // 진행도/안내는 이벤트와 프로퍼티로만 알려주고, 어떻게 그릴지는 모른다 (EatProgressView가 구독).
    public class FoodConsumer : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private FoodInventory inventory;
        [SerializeField] private Health health;
        [Tooltip("비워두면 Resources/ShelfLife/EatSettings, 그것도 없으면 코드 기본값(0.4초)")]
        [SerializeField] private EatSettings settings;

        private readonly EatHoldTimer timer = new EatHoldTimer();
        private FoodItem target;

        public event Action<FoodItem> FoodEaten;
        public event Action<FoodItem> EatStarted;
        public event Action EatCanceled;

        public bool IsEating => timer.IsActive;
        public float Progress01 => timer.Progress01;
        public FoodItem Target => target;
        public EatSettings Settings => settings = EatSettings.Resolve(settings);

        // 체력 가득 / 썩은 음식 같은 "먹어도 아쉬운 이유" (먹기를 막지는 않는다)
        public EatWarning CurrentWarning => EatAdvisor.Evaluate(target, health.Current, health.Max);

        private void Update()
        {
            var selected = ((IFoodInventory)inventory).Selected;
            if (timer.IsActive && selected != target) selected = null; // 먹는 도중 다른 음식으로 바꾸면 취소

            var held = ((IPlayerInput)input).EatHeld;
            switch (timer.Tick(Time.deltaTime, held, selected != null, Settings.holdSeconds))
            {
                case EatHoldEvent.Started:
                    target = selected;
                    EatStarted?.Invoke(target);
                    break;
                case EatHoldEvent.Canceled:
                    target = null;
                    EatCanceled?.Invoke();
                    break;
                case EatHoldEvent.Completed:
                    Eat(selected);
                    target = null;
                    break;
            }
        }

        private void OnDisable()
        {
            if (!timer.IsActive) return;
            timer.Tick(0f, false, false, Settings.holdSeconds);
            target = null;
            EatCanceled?.Invoke();
        }

        private void Eat(FoodItem item)
        {
            var amount = item.CurrentHeal;
            if (amount >= 0f) ((IHealable)health).Heal(amount);
            else ((IDamageable)health).TakeDamage(-amount);

            ((IFoodInventory)inventory).Remove(item);
            FoodEaten?.Invoke(item);
        }
    }
}
