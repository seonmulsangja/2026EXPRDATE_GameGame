using UnityEngine;

namespace ExpiryGame.Landmark
{
    // 편의점: 상호작용 키로 DropTable을 1회 드롭. 이미 열었으면 더 이상 상호작용 대상이 아니다.
    // (청크가 다시 로드되면 새 인스턴스라 리셋된다)
    public class StoreLootEffect : MonoBehaviour, ILandmarkPart, ILandmarkInteractable
    {
        private LandmarkContext context;
        private bool opened;

        public Vector2 Position => transform.position;
        public float InteractRange => context != null ? context.Data.interactRange : 0f;
        public bool CanInteract => context != null && !opened;
        public string PromptText => context != null ? context.Data.interactPrompt : string.Empty;

        public void Bind(LandmarkContext ctx)
        {
            context = ctx;
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
            opened = true;
            var origin = (Vector3)Position + Vector3.up * 0.8f;
            for (var i = 0; i < context.Data.lootRolls; i++)
                context.Drops.Drop(context.Data.lootTable, context.Data.lootThreat01, origin);
        }
    }
}
