using System.IO;
using ExpiryGame.Landmark;
using ExpiryGame.Landmark.Editor;
using ExpiryGame.MapGen.Editor;
using ExpiryGame.UI.Editor;
using ExpiryGame.Visual.Editor;
using ExpiryGame.World;
using ExpiryGame.World.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ExpiryGame.DebugTools.Editor
{
    // 아트 v2 + UI + 낮밤 + 랜드마크를 한 번에 설치하는 올인원 메뉴. 원본 씬(DropMapTest)은 건드리지 않고 복사본에 설치한다.
    public static class ArtV2Setup
    {
        private const string SourceScenePath = "Assets/ShelfLife/Scenes/DropMapTest.unity";
        private const string TestScenePath = "Assets/ShelfLife/Scenes/ArtV2Test.unity";
        private const string DebugMenuName = "DebugMenu";

        [MenuItem("Tools/ShelfLife/Setup Art V2 Test (All In One)")]
        public static void SetupAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            AssetDatabase.Refresh();
            ArtImporter.ApplyAll();

            if (!File.Exists(SourceScenePath))
            {
                Debug.LogError($"[ShelfLife] {SourceScenePath} 가 없습니다. 먼저 Tools > ShelfLife > Setup Drop + Infinite Map Test (All In One)을 실행하세요.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(scene, TestScenePath, true))
            {
                Debug.LogError($"[ShelfLife] {TestScenePath} 복사 저장 실패.");
                return;
            }
            EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);

            MapGenSetup.Install();
            WorldSetup.Install();
            LandmarkSetup.Install();
            UISetup.Install(false);
            InstallDebugMenu();

            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[ShelfLife] 아트 v2 테스트 씬 준비 완료 (ArtV2Test). Play 후 F1로 디버그 메뉴를 열어 보세요.");
        }

        [MenuItem("Tools/ShelfLife/Install Debug Menu To Open Scene")]
        public static void InstallDebugMenu()
        {
            var inventory = Object.FindFirstObjectByType<FoodInventory>();
            var clock = Object.FindFirstObjectByType<GameClock>();
            var factory = Object.FindFirstObjectByType<FoodFactory>();
            var locator = Object.FindFirstObjectByType<LandmarkLocator>();
            if (inventory == null || clock == null || factory == null || locator == null)
            {
                Debug.LogError("[ShelfLife] 씬에 Player/GameClock/FoodFactory/랜드마크가 필요합니다.");
                return;
            }

            var host = GameObject.Find(DebugMenuName) ?? new GameObject(DebugMenuName);
            var menu = DarkArtSceneInstaller.GetOrAdd<DebugMenu>(host);
            DarkArtSceneInstaller.SetObject(menu, "clock", clock);
            DarkArtSceneInstaller.SetObject(menu, "dayNightProfile", WorldSetup.LoadOrCreateProfile());
            DarkArtSceneInstaller.SetObject(menu, "inventory", inventory);
            DarkArtSceneInstaller.SetObject(menu, "factory", factory);
            DarkArtSceneInstaller.SetObject(menu, "locator", locator);
            DarkArtSceneInstaller.SetObject(menu, "player", inventory.transform);

            var so = new SerializedObject(menu);
            var foods = FoodSpriteLinker.LoadAll();
            var property = so.FindProperty("foods");
            property.arraySize = foods.Length;
            for (var i = 0; i < foods.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = foods[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkAllScenesDirty();
        }
    }
}
