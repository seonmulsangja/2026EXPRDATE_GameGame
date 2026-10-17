using UnityEditor;
using UnityEngine;

namespace ExpiryGame.Flow.Editor
{
    // 제출용 빌드 설정: Build Settings 씬 목록(게임 씬 하나), 해상도/전체화면.
    public static class SubmissionSettings
    {
        private const string GameScenePath = "Assets/ShelfLife/Scenes/Game.unity";

        [MenuItem("Tools/ShelfLife/Prepare Submission Settings")]
        public static void Apply()
        {
            if (System.IO.File.Exists(GameScenePath))
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(GameScenePath, true) };
            else
                Debug.LogWarning($"[ShelfLife] {GameScenePath} 가 없어 Build Settings 씬 목록을 바꾸지 않았습니다. Setup Game (All In One)을 먼저 실행하세요.");

            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.allowFullscreenSwitch = true;
            Debug.Log("[ShelfLife] 제출 설정 적용: 씬 목록 = Game, 1920x1080, 전체화면 창(Alt+Enter로 전환).");
        }
    }
}
