using System.IO;
using ExpiryGame.Combat;
using ExpiryGame.Drop;
using ExpiryGame.Flow;
using ExpiryGame.Landmark;
using ExpiryGame.MapGen;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ExpiryGame.Integration.Editor
{
    // 팀원 적(JCY)을 열린 씬에 붙인다. 팀원 프리팹/코드/SO는 수정하지 않고, 프리팹 "변형(Variant)"에 어댑터 등을 얹는다.
    // 어셈블리 경계: 팀원 코드가 Assembly-CSharp라서 이 폴더도 asmdef 없이 같은 어셈블리에 둔다.
    public static class EnemyIntegrationInstaller
    {
        private const string TeamPrefabPath = "Assets/JCY/03 Prefab/Enemy.prefab";
        private const string TeamSoDir = "Assets/JCY/02 SO";
        private const string VariantPath = "Assets/ShelfLife/Prefabs/Enemy_Integrated.prefab";
        private const string DataDir = "Assets/ShelfLife/Data/Integration";
        private const string SettingsPath = DataDir + "/EnemyIntegrationSettings.asset";
        private const string KnockbackSafeZonePath = "Assets/Resources/ShelfLife/Knockback_SafeZone.asset";
        private const string DropTablePath = "Assets/ShelfLife/Data/Drop/DropTable_Default.asset";
        private const string FlashMaterialPath = "Assets/ShelfLife/Data/Combat/SpriteFlash.mat";
        private const string PoolingName = "EnemyPooling (JCY)";
        private const string SpawnerName = "EnemyChunkSpawner";
        private const string EnemyRootName = "Enemies";

        [MenuItem("Tools/ShelfLife/Install Enemies To Open Scene")]
        public static void Install()
        {
            var streamer = Object.FindFirstObjectByType<ChunkStreamer>();
            var inventory = Object.FindFirstObjectByType<FoodInventory>();
            var difficulty = Object.FindFirstObjectByType<DifficultyService>();
            var flow = Object.FindFirstObjectByType<GameFlowRoot>();
            if (streamer == null || inventory == null || difficulty == null || flow == null)
            {
                Debug.LogError("[ShelfLife] 씬에 ChunkStreamer/Player/GameFlow가 필요합니다. 먼저 Install Infinite Map과 Install Game Flow To Open Scene을 실행하세요.");
                return;
            }

            var prefab = LoadTeamPrefab();
            if (prefab == null) return;

            inventory.gameObject.tag = "Player";
            var settings = LoadOrCreateSettings();
            settings.enemyPrefab = CreateVariant(prefab);
            if (settings.enemyPrefab == null) { Debug.LogError("[ShelfLife] 적 프리팹 변형을 만들지 못했습니다. 위 오류를 확인하세요."); return; }
            EditorUtility.SetDirty(settings);

            InstallPooling(inventory.transform);

            var enemyRoot = GameObject.Find(EnemyRootName) ?? new GameObject(EnemyRootName);
            var host = GameObject.Find(SpawnerName) ?? new GameObject(SpawnerName);
            var spawner = host.GetComponent<EnemyChunkSpawner>() ?? host.AddComponent<EnemyChunkSpawner>();
            var so = new SerializedObject(spawner);
            so.FindProperty("streamer").objectReferenceValue = streamer;
            so.FindProperty("settings").objectReferenceValue = settings;
            so.FindProperty("difficulty").objectReferenceValue = difficulty;
            so.FindProperty("flow").objectReferenceValue = flow;
            so.FindProperty("player").objectReferenceValue = inventory.transform;
            so.FindProperty("dangerEvents").objectReferenceValue = Object.FindFirstObjectByType<DangerZoneEvents>();
            so.FindProperty("root").objectReferenceValue = enemyRoot.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log("[ShelfLife] 적 통합 설치 완료 (EnemyChunkSpawner). 수치: Assets/ShelfLife/Data/Integration/EnemyIntegrationSettings.asset");
        }

        private static GameObject LoadTeamPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TeamPrefabPath);
            if (prefab == null) Debug.LogError($"[ShelfLife] {TeamPrefabPath} 가 없습니다. origin/JCY를 병합했는지 확인하세요.");
            return prefab;
        }

        // 팀원 프리팹을 건드리지 않고 변형을 만들어 어댑터/넉백/플래시를 얹는다 (같은 경로에 다시 만들면 GUID가 유지된다)
        private static GameObject CreateVariant(GameObject teamPrefab)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(teamPrefab);
            try
            {
                if (instance.GetComponent<EnemyAdapter>() == null) instance.AddComponent<EnemyAdapter>();
                if (instance.GetComponent<KnockbackReceiver>() == null) instance.AddComponent<KnockbackReceiver>();

                var flash = instance.GetComponent<FlashOnHit>() ?? instance.AddComponent<FlashOnHit>();
                var so = new SerializedObject(flash);
                so.FindProperty("flashMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(FlashMaterialPath);
                so.ApplyModifiedPropertiesWithoutUndo();

                EnsureFolder(Path.GetDirectoryName(VariantPath).Replace('\\', '/'));
                return PrefabUtility.SaveAsPrefabAsset(instance, VariantPath);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        // 팀원 Enemy.OnEnable이 EnemyPooling.Instance.enemyDatas에서 SO를 뽑는다. 풀링/스폰 로직은 쓰지 않으므로 풀 크기 0.
        private static void InstallPooling(Transform player)
        {
            var pooling = Object.FindFirstObjectByType<EnemyPooling>();
            if (pooling == null) pooling = new GameObject(PoolingName).AddComponent<EnemyPooling>();

            var so = new SerializedObject(pooling);
            var datas = so.FindProperty("enemyDatas");
            var guids = AssetDatabase.FindAssets("t:EnemySO", new[] { TeamSoDir });
            datas.arraySize = guids.Length;
            for (var i = 0; i < guids.Length; i++)
                datas.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<EnemySO>(AssetDatabase.GUIDToAssetPath(guids[i]));
            so.FindProperty("poolSize").intValue = 0;
            so.FindProperty("_payerTrm").objectReferenceValue = player;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static EnemyIntegrationSettings LoadOrCreateSettings()
        {
            EnsureFolder(DataDir);
            var settings = AssetDatabase.LoadAssetAtPath<EnemyIntegrationSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<EnemyIntegrationSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }
            if (settings.dropTable == null) settings.dropTable = AssetDatabase.LoadAssetAtPath<DropTable>(DropTablePath);
            if (settings.safeZoneKnockback == null) settings.safeZoneKnockback = AssetDatabase.LoadAssetAtPath<KnockbackProfile>(KnockbackSafeZonePath);
            return settings;
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
