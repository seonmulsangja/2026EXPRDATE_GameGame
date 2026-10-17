using UnityEngine;

namespace ExpiryGame.Landmark
{
    // 모닥불: F로 현재 선택된 슬롯의 음식을 강화한다 (규칙/수치는 EnhanceProfile, 판단은 FoodEnhancer).
    // 가능 여부와 상관없이 범위 안에서는 항상 상호작용 대상이라, 불가능할 때는 프롬프트에 사유가 뜬다.
    public class CampfireEnhanceEffect : MonoBehaviour, ILandmarkPart, ILandmarkInteractable
    {
        private readonly CooldownTimer cooldown = new CooldownTimer();

        private LandmarkContext context;
        private FoodEnhancer enhancer;

        // 프롬프트 문자열은 상태가 바뀔 때만 다시 만든다 (Interactor가 매 프레임 읽는다)
        private string prompt = string.Empty;
        private FoodItem promptItem;
        private EnhanceBlock promptBlock = (EnhanceBlock)(-1);
        private int promptSecond = -1;
        private bool promptDirty = true;

        private EnhanceProfile Profile => context != null ? context.Data.enhanceProfile : null;

        public Vector2 Position => transform.position;
        public float InteractRange => context != null ? context.Data.interactRange : 0f;
        public bool CanInteract => Profile != null && context.Inventory != null;

        public string PromptText
        {
            get
            {
                RefreshPrompt();
                return prompt;
            }
        }

        public void Bind(LandmarkContext ctx)
        {
            context = ctx;
            enhancer = ctx.Data.enhanceProfile != null ? new FoodEnhancer(ctx.Data.enhanceProfile) : null;
            promptDirty = true;
        }

        private void OnEnable()
        {
            InteractableRegistry.Register(this);
        }

        private void OnDisable()
        {
            InteractableRegistry.Unregister(this);
        }

        public void Interact()
        {
            if (!CanInteract) return;

            var item = context.Inventory.Selected;
            var check = enhancer.Check(item, cooldown.Remaining(Time.time));
            if (!check.CanEnhance || !enhancer.Apply(item)) return;

            cooldown.Start(Time.time, Profile.cooldownSeconds);
            promptDirty = true;
            context.Events?.FoodEnhanced(new FoodEnhancedInfo(item, Position, check.HealBefore, check.HealAfter));
        }

        private void RefreshPrompt()
        {
            if (!CanInteract)
            {
                prompt = string.Empty;
                return;
            }

            var item = context.Inventory.Selected;
            var left = cooldown.Remaining(Time.time);
            var check = enhancer.Check(item, left);
            var second = Mathf.CeilToInt(left);
            if (!promptDirty && item == promptItem && check.Block == promptBlock && second == promptSecond) return;

            promptDirty = false;
            promptItem = item;
            promptBlock = check.Block;
            promptSecond = second;
            prompt = Profile.PromptFor(check);
        }
    }
}
