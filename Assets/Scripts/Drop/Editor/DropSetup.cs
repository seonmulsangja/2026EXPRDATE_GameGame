using ExpiryGame.Combat;
using ExpiryGame.Visual;
using ExpiryGame.Visual.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ExpiryGame.Drop.Editor
{
    // 열린 씬에 드롭 시스템을 설치한다: DropTable/연출 SO, 월드 드롭 프리팹, DropSpawner, 테스트용 적 더미 2마리.
    // 여러 번 실행해도 중복 생성하지 않고, 이미 있는 SO의 수치는 건드리지 않는다.
    public static class DropSetup
    {
        public const string DropTablePath = DataDir + "/DropTable_Default.asset";

        private const string DataDir = "Assets/ShelfLife/Data/Drop";
        private const string PrefabDir = "Assets/ShelfLife/Prefabs";
        private const string FeelPath = DataDir + "/DropFeelSettings.asset";
        private const string FlashMaterialPath = "Assets/ShelfLife/Data/Combat/SpriteFlash.mat";
        private const string SpawnerName = "DropSpawner";
        private const string EnemyLayerName = "Enemy";

        [MenuItem("Tools/ShelfLife/Install Drop To Open Scene")]
        public static void Install()
        {
            var inventory = Object.FindFirstObjectByType<FoodInventory>();
            var clock = Object.FindFirstObjectByType<GameClock>();
            var tracker = Object.FindFirstObjectByType<ShelfLifeSystem>();
            var factory = Object.FindFirstObjectByType<FoodFactory>();
            if (inventory == null || clock == null || tracker == null || factory == null)
            {
                Debug.LogError("[ShelfLife] 씬에 Player(FoodInventory)/GameSystems가 없습니다. ShelfLifeTest 계열 씬을 연 상태에서 실행하세요.");
                return;
            }

            EnsureFolder("Assets/ShelfLife", "Data");
            EnsureFolder("Assets/ShelfLife/Data", "Drop");
            FoodSpriteLinker.Link();

            var table = LoadOrCreateTable();
            var feel = LoadOrCreateFeel();
            var theme = ArtAssets.LoadOrCreateTheme();
            var pickupPrefab = CreateFoodDropPrefab(theme);

            InstallSpawner(pickupPrefab, feel, inventory, clock, tracker, factory);
            InstallDummies(table);

            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log("[ShelfLife] 드롭 설치 완료. 적 더미(DropDummy_Weak/Strong)를 때려 보세요.");
        }

        private static DropTable LoadOrCreateTable()
        {
            var table = AssetDatabase.LoadAssetAtPath<DropTable>(DropTablePath);
            if (table != null) return table;

            table = ScriptableObject.CreateInstance<DropTable>();
            AssetDatabase.CreateAsset(table, DropTablePath);

            var foods = FoodSpriteLinker.LoadAll();
            var so = new SerializedObject(table);
            var entries = so.FindProperty("entries");
            entries.arraySize = foods.Length;
            for (var i = 0; i < foods.Length; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("food").objectReferenceValue = foods[i];
                entry.FindPropertyRelative("weight").floatValue = 1f;
                entry.FindPropertyRelative("minShelfLife01").floatValue = 0.4f;
                entry.FindPropertyRelative("maxShelfLife01").floatValue = 1f;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return table;
        }

        private static DropFeelSettings LoadOrCreateFeel()
        {
            var feel = AssetDatabase.LoadAssetAtPath<DropFeelSettings>(FeelPath);
            if (feel == null)
            {
                feel = ScriptableObject.CreateInstance<DropFeelSettings>();
                AssetDatabase.CreateAsset(feel, FeelPath);
            }
            if (feel.sparkleSprite == null)
            {
                feel.sparkleSprite = ArtAssets.Load("fx", "particle_spark");
                EditorUtility.SetDirty(feel);
            }
            return feel;
        }

        private static DroppedFood CreateFoodDropPrefab(ArtTheme theme)
        {
            // 이미 있으면 그대로 쓴다 (프리팹을 손으로 다듬은 경우 덮어쓰지 않음. 다시 만들려면 프리팹 삭제 후 실행)
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/FoodDrop.prefab");
            if (existing != null) return existing.GetComponent<DroppedFood>();

            var root = new GameObject("FoodDrop");

            var shadow = DarkArtSceneInstaller.GetOrCreateChild(root.transform, "Shadow");
            shadow.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            shadow.transform.localScale = Vector3.one * 0.55f;
            var shadowRenderer = shadow.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = ArtAssets.Load("player", "shadow");
            shadowRenderer.sortingOrder = 0;
            DarkArtSceneInstaller.EnsureLit(shadowRenderer);

            var body = DarkArtSceneInstaller.GetOrCreateChild(root.transform, "Body");
            body.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            var bodyRenderer = body.AddComponent<SpriteRenderer>();
            bodyRenderer.sortingOrder = 1;
            DarkArtSceneInstaller.EnsureLit(bodyRenderer);
            var visual = body.AddComponent<FoodVisual>();
            DarkArtSceneInstaller.SetObject(visual, "theme", theme);
            DarkArtSceneInstaller.SetObject(visual, "body", bodyRenderer);

            var sparkleGo = DarkArtSceneInstaller.GetOrCreateChild(root.transform, "Sparkle");
            sparkleGo.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            var sparkleRenderer = sparkleGo.AddComponent<SpriteRenderer>();
            sparkleRenderer.sortingOrder = 3;
            sparkleRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(ArtAssets.UnlitMaterialPath);
            var sparkle = sparkleGo.AddComponent<DropSparkle>();

            var arc = root.AddComponent<DropArc>();
            DarkArtSceneInstaller.SetObject(arc, "body", body.transform);
            DarkArtSceneInstaller.SetObject(arc, "shadow", shadow.transform);

            var collector = root.AddComponent<DropCollector>();

            var dropped = root.AddComponent<DroppedFood>();
            DarkArtSceneInstaller.SetObject(dropped, "visual", visual);
            DarkArtSceneInstaller.SetObject(dropped, "arc", arc);
            DarkArtSceneInstaller.SetObject(dropped, "collector", collector);
            DarkArtSceneInstaller.SetObject(dropped, "sparkle", sparkle);

            var saved = PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/FoodDrop.prefab");
            Object.DestroyImmediate(root);
            return saved.GetComponent<DroppedFood>();
        }

        private static void InstallSpawner(DroppedFood pickupPrefab, DropFeelSettings feel, FoodInventory inventory,
            GameClock clock, ShelfLifeSystem tracker, FoodFactory factory)
        {
            var host = GameObject.Find(SpawnerName) ?? new GameObject(SpawnerName);
            var spawner = DarkArtSceneInstaller.GetOrAdd<DropSpawner>(host);
            DarkArtSceneInstaller.SetObject(spawner, "factory", factory);
            DarkArtSceneInstaller.SetObject(spawner, "tracker", tracker);
            DarkArtSceneInstaller.SetObject(spawner, "collector", inventory);
            DarkArtSceneInstaller.SetObject(spawner, "clock", clock);
            DarkArtSceneInstaller.SetObject(spawner, "pickupPrefab", pickupPrefab);
            DarkArtSceneInstaller.SetObject(spawner, "settings", feel);
        }

        private static void InstallDummies(DropTable table)
        {
            var enemyLayer = LayerMask.NameToLayer(EnemyLayerName);
            var prefab = CreateDummyPrefab(table, enemyLayer);
            PlaceDummy(prefab, "DropDummy_Weak", new Vector3(-3f, 4f), 0.05f, new Color(0.75f, 0.9f, 1f));
            PlaceDummy(prefab, "DropDummy_Strong", new Vector3(3f, 4f), 0.95f, new Color(1f, 0.5f, 0.5f));
        }

        private static GameObject CreateDummyPrefab(DropTable table, int enemyLayer)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/DropDummy.prefab");
            if (existing != null) return existing;

            var go = new GameObject("DropDummy", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(Rigidbody2D));
            go.layer = enemyLayer;

            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = ArtAssets.Load("player", "player_down_idle");
            renderer.sortingOrder = 2;
            DarkArtSceneInstaller.EnsureLit(renderer);

            var circle = go.GetComponent<CircleCollider2D>();
            circle.radius = 0.3f;
            circle.offset = new Vector2(0f, 0.3f);

            var body = go.GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.linearDamping = 10f;

            var dummy = go.AddComponent<DropTestDummy>();
            DarkArtSceneInstaller.SetObject(dummy, "table", table);

            go.AddComponent<KnockbackReceiver>();
            var flash = go.AddComponent<FlashOnHit>();
            DarkArtSceneInstaller.SetObject(flash, "flashMaterial", AssetDatabase.LoadAssetAtPath<Material>(FlashMaterialPath));

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/DropDummy.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static void PlaceDummy(GameObject prefab, string name, Vector3 position, float threat01, Color tint)
        {
            var instance = GameObject.Find(name);
            if (instance == null)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = name;
                instance.transform.position = position;
            }

            instance.GetComponent<SpriteRenderer>().color = tint;
            var so = new SerializedObject(instance.GetComponent<DropTestDummy>());
            so.FindProperty("threat01").floatValue = threat01;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}")) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
