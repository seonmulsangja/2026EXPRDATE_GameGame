using System.Collections.Generic;
using ExpiryGame.Drop;
using ExpiryGame.MapGen;
using ExpiryGame.MapGen.Editor;
using ExpiryGame.Visual.Editor;
using ExpiryGame.World;
using ExpiryGame.World.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ExpiryGame.Landmark.Editor
{
    // 랜드마크 4종(냉장고/모닥불/편의점/쓰레기 더미)의 SO·프리팹을 만들고 열린 씬에 스포너 등을 설치한다.
    // 이미 있는 에셋의 수치/프리팹은 덮어쓰지 않는다 (프리팹을 다시 만들려면 삭제 후 실행).
    public static class LandmarkSetup
    {
        public const string SettingsPath = DataDir + "/LandmarkSettings.asset";

        private const string DataDir = "Assets/ShelfLife/Data/Landmark";
        private const string PrefabDir = "Assets/ShelfLife/Prefabs/Landmarks";
        private const string HostName = "Landmarks";
        private const string RootName = "LandmarkInstances";

        [MenuItem("Tools/ShelfLife/Install Landmarks To Open Scene")]
        public static void Install()
        {
            var inventory = Object.FindFirstObjectByType<FoodInventory>();
            var streamer = Object.FindFirstObjectByType<ChunkStreamer>();
            var drops = Object.FindFirstObjectByType<DropSpawner>();
            if (inventory == null || streamer == null || drops == null)
            {
                Debug.LogError("[ShelfLife] 씬에 Player / 무한 맵(ChunkStreamer) / DropSpawner가 필요합니다. Setup Drop + Infinite Map Test를 먼저 실행하세요.");
                return;
            }

            var settings = CreateAssets();
            var player = inventory.transform;

            var host = GameObject.Find(HostName) ?? new GameObject(HostName);
            var root = DarkArtSceneInstaller.GetOrCreateChild(host.transform, RootName);

            var danger = DarkArtSceneInstaller.GetOrAdd<DangerZoneEvents>(host);

            var spawner = DarkArtSceneInstaller.GetOrAdd<LandmarkSpawner>(host);
            DarkArtSceneInstaller.SetObject(spawner, "settings", settings);
            DarkArtSceneInstaller.SetObject(spawner, "streamer", streamer);
            DarkArtSceneInstaller.SetObject(spawner, "player", player);
            DarkArtSceneInstaller.SetObject(spawner, "playerHealth", player.GetComponent<Health>());
            DarkArtSceneInstaller.SetObject(spawner, "drops", drops);
            DarkArtSceneInstaller.SetObject(spawner, "danger", danger);
            DarkArtSceneInstaller.SetObject(spawner, "root", root.transform);

            var filter = DarkArtSceneInstaller.GetOrAdd<LandmarkObstacleFilter>(host);
            DarkArtSceneInstaller.SetObject(filter, "settings", settings);
            DarkArtSceneInstaller.SetObject(filter, "streamer", streamer);

            var locator = DarkArtSceneInstaller.GetOrAdd<LandmarkLocator>(host);
            DarkArtSceneInstaller.SetObject(locator, "settings", settings);

            var interactor = DarkArtSceneInstaller.GetOrAdd<LandmarkInteractor>(host);
            DarkArtSceneInstaller.SetObject(interactor, "player", player);

            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log("[ShelfLife] 랜드마크 설치 완료. 시작 지점 근처에 모닥불이 하나 보장됩니다.");
        }

        // ---------- 에셋 ----------

        public static LandmarkSettings CreateAssets()
        {
            EnsureFolder("Assets/ShelfLife/Data", "Landmark");
            EnsureFolder("Assets/ShelfLife/Prefabs", "Landmarks");

            var additive = WorldSetup.LoadOrCreateAdditiveMaterial();
            var storeTable = LoadOrCreateTable("DropTable_Store", StoreTable);
            var dumpTable = LoadOrCreateTable("DropTable_Dump", DumpTable);

            var fridge = LoadOrCreateData("Landmark_Fridge", d =>
            {
                d.displayName = "냉장고";
                d.kind = LandmarkKind.Fridge;
                d.weight = 1f;
                d.size = new Vector2(2f, 2.5f);
                d.effectRadius = 5f;
                d.ringTint = new Color(0.45f, 0.85f, 1f, 0.9f);
                d.decayMultiplier = 0.3f;
            });
            var campfire = LoadOrCreateData("Landmark_Campfire", d =>
            {
                d.displayName = "모닥불";
                d.kind = LandmarkKind.Campfire;
                d.weight = 1f;
                d.size = new Vector2(1.5f, 1.5f);
                d.effectRadius = 4.5f;
                d.ringTint = new Color(1f, 0.6f, 0.25f, 0.9f);
                d.healPerSecond = 2f;
                d.healTickSeconds = 0.5f;
            });
            var store = LoadOrCreateData("Landmark_Store", d =>
            {
                d.displayName = "편의점";
                d.kind = LandmarkKind.Store;
                d.weight = 0.8f;
                d.size = new Vector2(3f, 2.5f);
                d.effectRadius = 3.5f;
                d.ringTint = new Color(1f, 0.9f, 0.4f, 0.9f);
                d.lootTable = storeTable;
                d.lootThreat01 = 0.4f;
                d.lootRolls = 3;
                d.interactRange = 2.5f;
                d.interactPrompt = "열기";
            });
            var dump = LoadOrCreateData("Landmark_Dump", d =>
            {
                d.displayName = "쓰레기 더미";
                d.kind = LandmarkKind.Dump;
                d.weight = 0.8f;
                d.size = new Vector2(2.5f, 2f);
                d.effectRadius = 5f;
                d.ringTint = new Color(0.8f, 0.3f, 0.9f, 0.9f);
                d.dumpTable = dumpTable;
                d.dumpThreat01 = 0.2f;
                d.dumpRolls = 2;
            });

            BuildPrefab(fridge, "landmark_fridge", additive, root =>
            {
                root.AddComponent<FridgeDecayEffect>();
            });
            BuildPrefab(campfire, "landmark_campfire_0", additive, root =>
            {
                root.AddComponent<CampfireHealEffect>();
                AddCampfireVisuals(root, additive);
            });
            BuildPrefab(store, "landmark_store", additive, root => root.AddComponent<StoreLootEffect>());
            BuildPrefab(dump, "landmark_dump", additive, root => root.AddComponent<DumpDangerEffect>());

            var settings = AssetDatabase.LoadAssetAtPath<LandmarkSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<LandmarkSettings>();
                settings.landmarks = new[] { fridge, campfire, store, dump };
                settings.startLandmark = campfire;
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }
            settings.map = AssetDatabase.LoadAssetAtPath<MapGenSettings>(MapGenSetup.SettingsPath);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            return settings;
        }

        private static LandmarkData LoadOrCreateData(string name, System.Action<LandmarkData> init)
        {
            var path = $"{DataDir}/{name}.asset";
            var data = AssetDatabase.LoadAssetAtPath<LandmarkData>(path);
            if (data != null) return data;
            data = ScriptableObject.CreateInstance<LandmarkData>();
            init(data);
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        private static DropTable LoadOrCreateTable(string name, System.Action<SerializedObject, FoodData[]> fill)
        {
            var path = $"{DataDir}/{name}.asset";
            var table = AssetDatabase.LoadAssetAtPath<DropTable>(path);
            if (table != null) return table;

            table = ScriptableObject.CreateInstance<DropTable>();
            AssetDatabase.CreateAsset(table, path);
            fill(new SerializedObject(table), FoodSpriteLinker.LoadAll());
            return table;
        }

        // 편의점: 모든 음식이 균등, 대체로 신선 (한 번 열면 2~3개)
        private static void StoreTable(SerializedObject so, FoodData[] foods)
        {
            WriteEntries(so, foods, new[] { (1f, 0.5f, 1f) });
            so.FindProperty("dropChance").floatValue = 1f;
            so.FindProperty("minCount").intValue = 2;
            so.FindProperty("maxCount").intValue = 3;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 쓰레기 더미: 같은 음식을 "이미 썩음(가중치 4)"과 "아직 먹을 만함(가중치 1)" 두 항목으로 둔다 → 썩은 음식 위주
        private static void DumpTable(SerializedObject so, FoodData[] foods)
        {
            WriteEntries(so, foods, new[] { (4f, 0f, 0f), (1f, 0.2f, 0.6f) });
            so.FindProperty("dropChance").floatValue = 1f;
            so.FindProperty("minCount").intValue = 1;
            so.FindProperty("maxCount").intValue = 2;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WriteEntries(SerializedObject so, FoodData[] foods, (float weight, float min, float max)[] variants)
        {
            var entries = so.FindProperty("entries");
            entries.arraySize = foods.Length * variants.Length;
            var index = 0;
            foreach (var food in foods)
            foreach (var variant in variants)
            {
                var entry = entries.GetArrayElementAtIndex(index++);
                entry.FindPropertyRelative("food").objectReferenceValue = food;
                entry.FindPropertyRelative("weight").floatValue = variant.weight;
                entry.FindPropertyRelative("minShelfLife01").floatValue = variant.min;
                entry.FindPropertyRelative("maxShelfLife01").floatValue = variant.max;
            }
        }

        // ---------- 프리팹 ----------

        private static void BuildPrefab(LandmarkData data, string spriteName, Material additive, System.Action<GameObject> addEffects)
        {
            var path = $"{PrefabDir}/{data.name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                var root = new GameObject(data.name);

                var body = new GameObject("Body");
                body.transform.SetParent(root.transform, false);
                var bodyRenderer = body.AddComponent<SpriteRenderer>();
                bodyRenderer.sprite = ArtAssets.Load("landmarks", spriteName);
                bodyRenderer.sortingOrder = 1;
                DarkArtSceneInstaller.EnsureLit(bodyRenderer);

                // 발밑 절반만 막는다 (위쪽은 지나가면서 가려 보이게)
                var collider = root.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(data.size.x * 0.85f, data.size.y * 0.4f);
                collider.offset = new Vector2(0f, data.size.y * 0.2f);

                var glowName = spriteName.EndsWith("_0") ? "fx_glow_soft" : spriteName + "_glow";
                AddGlow(root, glowName, additive, data.kind == LandmarkKind.Campfire);

                root.AddComponent<LandmarkZone>();
                AddRing(root);
                addEffects(root);

                PrefabUtility.SaveAsPrefabAsset(root, path);
                Object.DestroyImmediate(root);
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            data.prefab = prefab;
            EditorUtility.SetDirty(data);
        }

        private static void AddGlow(GameObject root, string spriteName, Material additive, bool campfire)
        {
            var glow = new GameObject("Glow");
            glow.transform.SetParent(root.transform, false);
            var renderer = glow.AddComponent<SpriteRenderer>();
            renderer.sprite = ArtAssets.Load(campfire ? "fx" : "landmarks", spriteName);
            renderer.sharedMaterial = additive;
            renderer.sortingOrder = 2;
            renderer.color = new Color(1f, 1f, 1f, 0f);

            var fader = glow.AddComponent<NightGlowFader>();
            if (!campfire) return;

            // 모닥불 빛번짐: 낮에도 은은하게, 밤에 크게
            glow.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            glow.transform.localScale = Vector3.one * 1.3f;
            renderer.color = new Color(1f, 0.6f, 0.25f, 0f);
            var so = new SerializedObject(fader);
            so.FindProperty("strength").floatValue = 0.7f;
            so.FindProperty("dayAlpha").floatValue = 0.2f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddRing(GameObject root)
        {
            var ring = new GameObject("ZoneRing");
            ring.transform.SetParent(root.transform, false);
            var renderer = ring.AddComponent<SpriteRenderer>();
            renderer.sprite = ArtAssets.Load("fx", "fx_zone_ring");
            renderer.sortingOrder = -90;
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(ArtAssets.UnlitMaterialPath);
            renderer.color = new Color(1f, 1f, 1f, 0f);

            var view = root.AddComponent<ZoneRingView>();
            DarkArtSceneInstaller.SetObject(view, "ring", renderer);
        }

        // 모닥불: 3프레임 루프 + Light 2D
        private static void AddCampfireVisuals(GameObject root, Material additive)
        {
            var body = root.transform.Find("Body").gameObject;
            var flipbook = body.AddComponent<LandmarkFlipbook>();
            var so = new SerializedObject(flipbook);
            var frames = so.FindProperty("frames");
            frames.arraySize = 3;
            for (var i = 0; i < 3; i++)
                frames.GetArrayElementAtIndex(i).objectReferenceValue = ArtAssets.Load("landmarks", $"landmark_campfire_{i}");
            so.ApplyModifiedPropertiesWithoutUndo();

            var lightHost = new GameObject("Light");
            lightHost.transform.SetParent(root.transform, false);
            lightHost.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            var light = lightHost.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = new Color(1f, 0.65f, 0.3f);
            light.pointLightInnerRadius = 0.5f;
            light.pointLightOuterRadius = 6f;
            lightHost.AddComponent<CampfireLight>();
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}")) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
