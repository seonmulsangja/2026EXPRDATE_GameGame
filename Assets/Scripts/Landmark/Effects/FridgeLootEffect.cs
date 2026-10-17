using UnityEngine;

namespace ExpiryGame.Landmark
{
    // 냉장고: F로 전용 DropTable(편의점과 별도 SO)을 1회 드롭. 열고 나면 "비어 있음"으로 표시하고 글로우를 끈다.
    // (청크가 다시 로드되면 새 인스턴스라 리셋된다 — 편의점과 동일)
    public class FridgeLootEffect : MonoBehaviour, ILandmarkPart, ILandmarkInteractable
    {
        private const string GlowName = "Glow";

        private LandmarkContext context;
        private bool opened;

        public bool IsOpened => opened;
        public Vector2 Position => transform.position;
        public float InteractRange => context != null ? context.Data.interactRange : 0f;
        public bool CanInteract => context != null && context.Data.fridgeTable != null;

        public string PromptText
        {
            get
            {
                if (context == null) return string.Empty;
                return opened ? context.Data.fridgeEmptyPrompt : context.Data.interactPrompt;
            }
        }

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
            if (!CanInteract || opened) return;
            opened = true;

            var origin = (Vector3)Position + Vector3.up * 0.8f;
            for (var i = 0; i < context.Data.fridgeRolls; i++)
                context.Drops.Drop(context.Data.fridgeTable, context.Data.fridgeThreat01, origin);

            var glow = transform.Find(GlowName);
            if (glow != null) glow.gameObject.SetActive(false);
            context.Events?.FridgeOpened(new FridgeOpenedInfo(Position));
        }
    }
}
