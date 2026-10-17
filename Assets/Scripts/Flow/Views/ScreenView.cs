using UnityEngine;

namespace ExpiryGame.Flow
{
    // 화면 하나 = 전체 화면 CanvasGroup 하나. 보이기/숨기기만 안다 (무슨 일을 할지는 컨트롤러가 정한다).
    public abstract class ScreenView : MonoBehaviour
    {
        private CanvasGroup group;

        protected CanvasGroup Group
        {
            get
            {
                if (group == null)
                {
                    group = gameObject.GetComponent<CanvasGroup>();
                    if (group == null) group = gameObject.AddComponent<CanvasGroup>();
                }
                return group;
            }
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            Group.alpha = 1f;
            Group.blocksRaycasts = true;
            Group.interactable = true;
        }

        public virtual void Hide()
        {
            Group.alpha = 0f;
            Group.blocksRaycasts = false;
            Group.interactable = false;
            gameObject.SetActive(false);
        }
    }
}
