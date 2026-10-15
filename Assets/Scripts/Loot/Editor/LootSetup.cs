using ExpiryGame.Drop;
using ExpiryGame.Drop.Editor;
using ExpiryGame.MapGen;
using ExpiryGame.MapGen.Editor;
using ExpiryGame.Visual.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ExpiryGame.Loot.Editor
{
    // 음식 상자 설치 + 드롭/무한 맵/상자를 한 번에 설치하는 올인원 메뉴.
    public static class LootSetup
    {
        private const string SourceScenePath = "Assets/ShelfLife/Scenes/ShelfLifeTest.unity";
        private const string TestScenePath = "Assets/ShelfLife/Scenes/DropMapTest.unity";
        private const string SettingsPath = "Assets/ShelfLife/Data/Drop/LootCrateSettings.asset";
        private const string SpawnerName = "LootCrateSpawner";
        private const string CrateRootName = "LootCrates";
        private const string EnemyLayerName = "Enemy";

        [MenuItem("Tools/ShelfLife/Setup Drop + Infinite Map Test (All In One)")]
        public static void SetupAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // 원본 테스트 씬은 건드리지 않고 복사본에 설치한다
            var scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(scene, TestScenePath, true))
            {
                Debug.LogError($"[ShelfLife] {TestScenePath} 복사 저장 실패. ShelfLifeTest 씬이 있는지 확인하세요 (Tools > ShelfLife > Setup Combat Test).");
                return;
            }
            EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);

            DropSetup.Install();
            MapGenSetup.Install();
            Install();
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[ShelfLife] 드롭 + 무한 맵 테스트 씬 준비 완료 (DropMapTest). Play 후 적 더미/상자를 때려 보세요.");
        }

        [MenuItem("Tools/ShelfLife/Install Loot Crates To Open Scene")]
        public static void Install()
        {
            var streamer = Object.FindFirstObjectByType<ChunkStreamer>();
            if (streamer == null || Object.FindFirstObjectByType<DropSpawner>() == null)
            {
                Debug.LogError("[ShelfLife] 먼저 'Install Drop To Open Scene'과 'Install Infinite Map To Open Scene'을 실행하세요.");
                return;
            }

            var mapSettings = AssetDatabase.LoadAssetAtPath<MapGenSettings>(MapGenSetup.SettingsPath);
            var settings = LoadOrCreateSettings();

            var crateRoot = GameObject.Find(CrateRootName) ?? new GameObject(CrateRootName);
            var host = GameObject.Find(SpawnerName) ?? new GameObject(SpawnerName);
            var spawner = DarkArtSceneInstaller.GetOrAdd<LootCrateSpawner>(host);
            DarkArtSceneInstaller.SetObject(spawner, "streamer", streamer);
            DarkArtSceneInstaller.SetObject(spawner, "mapSettings", mapSettings);
            DarkArtSceneInstaller.SetObject(spawner, "settings", settings);
            DarkArtSceneInstaller.SetObject(spawner, "root", crateRoot.transform);

            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log("[ShelfLife] 음식 상자 설치 완료.");
        }

        private static LootCrateSettings LoadOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<LootCrateSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<LootCrateSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            // 비어 있는 참조만 채운다 (사용자가 바꾼 값은 유지)
            if (settings.table == null) settings.table = AssetDatabase.LoadAssetAtPath<DropTable>(DropSetup.DropTablePath);
            if (settings.sprite == null) settings.sprite = ArtAssets.Load("obstacles", "obstacle_crate");
            if (settings.material == null) settings.material = AssetDatabase.LoadAssetAtPath<Material>(ArtAssets.LitMaterialPath);
            if (settings.layer == 0)
            {
                var layer = LayerMask.NameToLayer(EnemyLayerName);
                settings.layer = layer >= 0 ? layer : 0;
            }
            EditorUtility.SetDirty(settings);
            return settings;
        }
    }
}
