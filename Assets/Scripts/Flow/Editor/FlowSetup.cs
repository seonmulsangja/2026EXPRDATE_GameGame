using System.IO;
using ExpiryGame.Combat;
using ExpiryGame.Drop;
using ExpiryGame.UI;
using ExpiryGame.UI.Editor;
using ExpiryGame.Visual.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ExpiryGame.Flow.Editor
{
    // 게임 흐름(타이틀~게임오버/클리어) 설치 + 제출용 올인원. 원본 테스트 씬(ArtV2Test)은 건드리지 않고 Game.unity 복사본에 설치한다.
    public static class FlowSetup
    {
        public const string DataDir = "Assets/ShelfLife/Data/Flow";
        private const string SourceScenePath = "Assets/ShelfLife/Scenes/ArtV2Test.unity";
        private const string GameScenePath = "Assets/ShelfLife/Scenes/Game.unity";
        private const string RootName = "GameFlow";
        private const string EnemyInstallMenu = "Tools/ShelfLife/Install Enemies To Open Scene";

        // 테스트용으로 깔린 것들. 게임 씬에서는 치운다.
        private static readonly string[] TestObjectPrefixes = { "CombatDummy", "Scarecrow", "DropDummy", "EnemyDummy", "Pickup_", "DebugHud" };

        [MenuItem("Tools/ShelfLife/Setup Game (All In One)")]
        public static void SetupAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (!File.Exists(SourceScenePath))
            {
                Debug.Log("[ShelfLife] ArtV2Test 씬이 없어 먼저 Art V2 올인원을 실행합니다.");
                ExpiryGame.DebugTools.Editor.ArtV2Setup.SetupAll();
            }
            if (!File.Exists(SourceScenePath))
            {
                Debug.LogError($"[ShelfLife] {SourceScenePath} 를 만들지 못했습니다. Tools > ShelfLife > Setup Art V2 Test (All In One)의 오류를 먼저 확인하세요.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(scene, GameScenePath, true))
            {
                Debug.LogError($"[ShelfLife] {GameScenePath} 복사 저장 실패.");
                return;
            }
            EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

            RemoveTestObjects();
            Install();
            if (!EditorApplication.ExecuteMenuItem(EnemyInstallMenu))
                Debug.LogError($"[ShelfLife] '{EnemyInstallMenu}' 메뉴를 실행하지 못했습니다 (컴파일 에러 확인).");

            SubmissionSettings.Apply();
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[ShelfLife] 게임 씬 준비 완료 (Game.unity). Play하면 타이틀 화면부터 시작합니다.");
        }

        [MenuItem("Tools/ShelfLife/Install Game Flow To Open Scene")]
        public static void Install()
        {
            var clock = Object.FindFirstObjectByType<GameClock>();
            var inventory = Object.FindFirstObjectByType<FoodInventory>();
            var dropSpawner = Object.FindFirstObjectByType<DropSpawner>();
            if (clock == null || inventory == null || dropSpawner == null)
            {
                Debug.LogError("[ShelfLife] 씬에 GameClock/Player(FoodInventory)/DropSpawner가 필요합니다. 먼저 Setup Art V2 Test (All In One)을 실행하세요.");
                return;
            }

            var player = inventory.gameObject;
            var health = player.GetComponent<Health>();
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(UISetup.ThemePath);
            if (theme == null) Debug.LogWarning("[ShelfLife] UITheme이 없어 기본 글꼴로 표시됩니다. Install HUD를 먼저 실행하세요.");

            // 적이 플레이어를 알아보는 태그 (팀원 Enemy.OnCollisionEnter2D가 CompareTag("Player")를 쓴다)
            player.tag = "Player";
            InstallPlayerFlash(player);

            var profile = LoadOrCreate<DifficultyProfile>("DifficultyProfile");
            var host = GameObject.Find(RootName) ?? new GameObject(RootName);

            var difficulty = DarkArtSceneInstaller.GetOrAdd<DifficultyService>(host);
            DarkArtSceneInstaller.SetObject(difficulty, "clock", clock);
            DarkArtSceneInstaller.SetObject(difficulty, "profile", profile);

            var root = DarkArtSceneInstaller.GetOrAdd<GameFlowRoot>(host);
            DarkArtSceneInstaller.SetObject(root, "theme", theme);
            DarkArtSceneInstaller.SetObject(root, "settings", LoadOrCreate<FlowSettings>("FlowSettings"));
            DarkArtSceneInstaller.SetObject(root, "goal", LoadOrCreate<SurvivalGoal>("SurvivalGoal"));
            DarkArtSceneInstaller.SetObject(root, "scoreRules", LoadOrCreate<ScoreRules>("ScoreRules"));
            DarkArtSceneInstaller.SetObject(root, "loadout", LoadOrCreateLoadout());
            DarkArtSceneInstaller.SetObject(root, "audioCatalog", AudioCatalogBuilder.LoadOrCreate(DataDir));
            DarkArtSceneInstaller.SetObject(root, "clock", clock);
            DarkArtSceneInstaller.SetObject(root, "playerHealth", health);
            DarkArtSceneInstaller.SetObject(root, "inventory", inventory);
            DarkArtSceneInstaller.SetObject(root, "consumer", player.GetComponent<FoodConsumer>());
            DarkArtSceneInstaller.SetObject(root, "input", player.GetComponent<PlayerInputReader>());
            DarkArtSceneInstaller.SetObject(root, "cameraShake", Camera.main != null ? Camera.main.GetComponent<CameraShake>() : null);
            DarkArtSceneInstaller.SetObject(root, "hitDetector", player.GetComponentInChildren<SwingHitDetector>(true));
            DarkArtSceneInstaller.SetObject(root, "dropSpawner", dropSpawner);

            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log("[ShelfLife] 게임 흐름 설치 완료 (GameFlow).");
        }

        public static T LoadOrCreate<T>(string assetName) where T : ScriptableObject
        {
            EnsureFolder(DataDir);
            var path = $"{DataDir}/{assetName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        // 시작 음식: 통조림 300초, 빵 120초, 생선 45초 (생선 FoodData는 30초라 복사본으로 45초를 지급한다)
        private static StartingLoadout LoadOrCreateLoadout()
        {
            var loadout = LoadOrCreate<StartingLoadout>("StartingLoadout");
            if (loadout.entries != null && loadout.entries.Length > 0) return loadout;

            loadout.entries = new[]
            {
                new LoadoutEntry { food = LoadFood("Food_Canned_Bean"), shelfLifeSeconds = 300f },
                new LoadoutEntry { food = LoadFood("Food_Bread"), shelfLifeSeconds = 120f },
                new LoadoutEntry { food = LoadFood("Food_Fresh_Fish"), shelfLifeSeconds = 45f },
            };
            EditorUtility.SetDirty(loadout);
            return loadout;
        }

        private static FoodData LoadFood(string assetName)
        {
            var food = AssetDatabase.LoadAssetAtPath<FoodData>($"Assets/ShelfLife/Data/{assetName}.asset");
            if (food == null) Debug.LogWarning($"[ShelfLife] {assetName} 를 찾지 못했습니다. StartingLoadout을 직접 채워 주세요.");
            return food;
        }

        // 피격 플래시: 플레이어 본체 SpriteRenderer만 번쩍이게 (그림자/손에 든 음식은 제외)
        private static void InstallPlayerFlash(GameObject player)
        {
            var flash = DarkArtSceneInstaller.GetOrAdd<FlashOnHit>(player);
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/ShelfLife/Data/Combat/SpriteFlash.mat");
            DarkArtSceneInstaller.SetObject(flash, "flashMaterial", material);

            var body = player.GetComponent<SpriteRenderer>();
            if (body == null) return;
            var so = new SerializedObject(flash);
            var renderers = so.FindProperty("renderers");
            renderers.arraySize = 1;
            renderers.GetArrayElementAtIndex(0).objectReferenceValue = body;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RemoveTestObjects()
        {
            foreach (var go in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go == null || go.parent != null) continue;
                foreach (var prefix in TestObjectPrefixes)
                {
                    if (!go.name.StartsWith(prefix)) continue;
                    Object.DestroyImmediate(go.gameObject);
                    break;
                }
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
