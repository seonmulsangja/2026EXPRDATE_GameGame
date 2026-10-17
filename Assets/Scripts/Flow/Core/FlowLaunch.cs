using UnityEngine;
using UnityEngine.SceneManagement;

namespace ExpiryGame.Flow
{
    // 다시하기/타이틀로는 씬 리로드로 처리한다 (살아 있는 객체·이벤트 구독을 전부 새로 만드는 가장 확실한 초기화).
    // 리로드 사이에 살아남는 값은 "타이틀을 건너뛸지" 한 비트뿐이다.
    public static class FlowLaunch
    {
        private static bool skipTitleOnce;

        public static bool ConsumeSkipTitle()
        {
            var value = skipTitleOnce;
            skipTitleOnce = false;
            return value;
        }

        public static void Restart() => Reload(true);

        public static void ToTitle() => Reload(false);

        private static void Reload(bool skipTitle)
        {
            skipTitleOnce = skipTitle;
            Time.timeScale = 1f;

            var scene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
            // Build Settings에 없는 씬을 에디터에서 바로 실행한 경우 buildIndex가 -1이다
            if (scene.buildIndex < 0)
            {
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
                return;
            }
#endif
            SceneManager.LoadScene(scene.buildIndex);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            skipTitleOnce = false;
        }
    }
}
