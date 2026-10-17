using UnityEngine;
using UnityEngine.UI;

namespace ExpiryGame.UI
{
    // 남은 사용 횟수 점 (ui_pip_full / ui_pip_empty). 최대 개수는 Image 배열 길이까지.
    public class PipRow : MonoBehaviour
    {
        [SerializeField] private UITheme theme;
        [SerializeField] private Image[] pips = new Image[0];

        private int shownRemaining = -1;
        private int shownMax = -1;

        public void Set(int remaining, int max)
        {
            if (remaining == shownRemaining && max == shownMax) return;
            shownRemaining = remaining;
            shownMax = max;

            for (var i = 0; i < pips.Length; i++)
            {
                var visible = i < max;
                pips[i].gameObject.SetActive(visible);
                if (visible) pips[i].sprite = i < remaining ? theme.sprites.pipFull : theme.sprites.pipEmpty;
            }
        }

        public void Hide()
        {
            shownRemaining = shownMax = -1;
            foreach (var pip in pips) pip.gameObject.SetActive(false);
        }
    }
}
