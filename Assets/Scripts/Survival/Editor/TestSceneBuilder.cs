using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace ExpiryGame.Editor
{
    public static class TestSceneBuilder
    {
        private const string Root = "Assets/ShelfLife";
        private const string DataDir = Root + "/Data";
        private const string PrefabDir = Root + "/Prefabs";
        private const string SceneDir = Root + "/Scenes";
        private const string ScenePath = SceneDir + "/ShelfLifeTest.unity";
        private const string InputAssetPath = "Assets/InputSystem_Actions.inputactions";
        private const string EnemyLayerName = "Enemy";

        private readonly struct FoodSpec
        {
            public readonly string Name;
            public readonly float ShelfLife;
            public readonly Color Tint;

            public FoodSpec(string name, float shelfLife, Color tint)
            {
                Name = name;
                ShelfLife = shelfLife;
                Tint = tint;
            }
        }

        private static readonly FoodSpec[] FoodSpecs =
        {
            new FoodSpec("Fresh Fish", 30f, new Color(0.4f, 0.7f, 1f)),
            new FoodSpec("Bread", 120f, new Color(0.9f, 0.75f, 0.4f)),
            new FoodSpec("Canned Bean", 300f, new Color(0.5f, 0.85f, 0.4f)),
        };

        [MenuItem("Tools/ShelfLife/Build Test Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var enemyLayer = EnsureLayer(EnemyLayerName);
            EnsureFolders();

            // 씬 교체가 메모리의 에셋을 언로드하므로, 에셋은 반드시 새 씬을 만든 뒤에 생성/로드한다
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var dayConfig = LoadOrCreate<DayCycleConfig>(DataDir + "/DayCycleConfig.asset");
            var balance = LoadOrCreate<FoodBalanceConfig>(DataDir + "/FoodBalanceConfig.asset");
            var foods = FoodSpecs.Select(spec => CreateFood(spec)).ToArray();

            var playerPrefab = CreatePlayerPrefab(enemyLayer);
            var enemyPrefab = CreateEnemyPrefab(enemyLayer);
            var pickupPrefab = CreatePickupPrefab();

            var camera = CreateCamera();
            var light = CreateGlobalLight();
            var systems = CreateSystems(dayConfig, balance);
            CreateDayNightLighting(systems.clock, light);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = Vector3.zero;
            var inventory = player.GetComponent<FoodInventory>();
            SetObject(inventory, "tracker", systems.shelfLife);
            SetObject(inventory, "factory", systems.factory);

            PlaceEnemy(enemyPrefab, new Vector3(4f, 2f));
            PlaceEnemy(enemyPrefab, new Vector3(4f, -2f));
            for (var i = 0; i < foods.Length; i++)
                PlacePickup(pickupPrefab, foods[i], FoodSpecs[i].Tint, new Vector3(-4f, 2f - 2f * i));

            CreateDebugHud(systems.clock, player.GetComponent<Health>(), inventory, camera);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ShelfLife] 테스트 씬 생성 완료: {ScenePath}");
        }

        private static (GameClock clock, ShelfLifeSystem shelfLife, FoodFactory factory) CreateSystems(
            DayCycleConfig dayConfig, FoodBalanceConfig balance)
        {
            var go = new GameObject("GameSystems");
            var clock = go.AddComponent<GameClock>();
            var shelfLife = go.AddComponent<ShelfLifeSystem>();
            var factory = go.AddComponent<FoodFactory>();
            SetObject(clock, "config", dayConfig);
            SetObject(shelfLife, "clock", clock);
            SetObject(factory, "balance", balance);
            return (clock, shelfLife, factory);
        }

        private static void CreateDayNightLighting(GameClock clock, Light2D light)
        {
            var go = new GameObject("DayNightLighting");
            var lighting = go.AddComponent<DayNightLighting>();
            SetObject(lighting, "clock", clock);
            SetObject(lighting, "globalLight", light);

            var so = new SerializedObject(lighting);
            so.FindProperty("colorOverDay").gradientValue = BuildDayGradient();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Gradient BuildDayGradient()
        {
            var dusk = new Color(0.9f, 0.85f, 0.8f);
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(dusk, 0f),
                    new GradientColorKey(Color.white, 0.15f),
                    new GradientColorKey(Color.white, 0.4f),
                    new GradientColorKey(new Color(1f, 0.6f, 0.4f), 0.5f),
                    new GradientColorKey(new Color(0.2f, 0.2f, 0.45f), 0.62f),
                    new GradientColorKey(new Color(0.15f, 0.15f, 0.35f), 0.9f),
                    new GradientColorKey(dusk, 1f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }

        private static GameObject CreatePlayerPrefab(int enemyLayer)
        {
            var go = new GameObject("Player", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(Rigidbody2D));
            Style(go, new Color(1f, 1f, 1f), 0.8f);

            var body = go.GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            go.GetComponent<CircleCollider2D>().radius = 0.4f;

            var input = go.AddComponent<PlayerInputReader>();
            SetObject(input, "move", FindInputReference("Player/Move"));
            SetObject(input, "eat", FindInputReference("Player/Interact"));
            SetObject(input, "attack", FindInputReference("Player/Attack"));
            SetObject(input, "nextFood", FindInputReference("Player/Next"));

            var mover = go.AddComponent<PlayerMover>();
            SetObject(mover, "input", input);

            var facing = go.AddComponent<PlayerFacing>();
            SetObject(facing, "input", input);

            var health = go.AddComponent<Health>();

            var inventory = go.AddComponent<FoodInventory>();
            SetObject(inventory, "input", input);

            var consumer = go.AddComponent<FoodConsumer>();
            SetObject(consumer, "input", input);
            SetObject(consumer, "inventory", inventory);
            SetObject(consumer, "health", health);

            var weapon = go.AddComponent<FoodWeapon>();
            SetObject(weapon, "input", input);
            SetObject(weapon, "inventory", inventory);
            SetObject(weapon, "facing", facing);
            var maskProperty = new SerializedObject(weapon).FindProperty("targetMask");
            maskProperty.serializedObject.Update();
            maskProperty.intValue = 1 << enemyLayer;
            maskProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(go, "Player");
        }

        private static GameObject CreateEnemyPrefab(int enemyLayer)
        {
            var go = new GameObject("EnemyDummy", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(Health));
            Style(go, new Color(0.9f, 0.3f, 0.3f), 1f);
            go.layer = enemyLayer;
            go.GetComponent<CircleCollider2D>().radius = 0.5f;
            return SavePrefab(go, "EnemyDummy");
        }

        private static GameObject CreatePickupPrefab()
        {
            var go = new GameObject("FoodPickup", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(FoodPickup));
            Style(go, Color.white, 0.5f);
            go.GetComponent<CircleCollider2D>().isTrigger = true;
            return SavePrefab(go, "FoodPickup");
        }

        private static void PlaceEnemy(GameObject prefab, Vector3 position)
        {
            var enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            enemy.transform.position = position;
        }

        private static void PlacePickup(GameObject prefab, FoodData food, Color tint, Vector3 position)
        {
            var pickup = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            pickup.name = $"Pickup_{food.displayName}";
            pickup.transform.position = position;
            pickup.GetComponent<SpriteRenderer>().color = tint;
            SetObject(pickup.GetComponent<FoodPickup>(), "food", food);
        }

        private static void CreateDebugHud(GameClock clock, Health player, FoodInventory inventory, Camera camera)
        {
            var hud = new GameObject("DebugHud").AddComponent<DebugHud>();
            SetObject(hud, "clock", clock);
            SetObject(hud, "player", player);
            SetObject(hud, "inventory", inventory);
            SetObject(hud, "viewCamera", camera);
        }

        private static Camera CreateCamera()
        {
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            go.AddComponent<UniversalAdditionalCameraData>();
            go.transform.position = new Vector3(0f, 0f, -10f);

            var camera = go.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 7f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
            return camera;
        }

        private static Light2D CreateGlobalLight()
        {
            var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            return light;
        }

        private static FoodData CreateFood(FoodSpec spec)
        {
            var food = LoadOrCreate<FoodData>($"{DataDir}/Food_{spec.Name.Replace(' ', '_')}.asset");
            food.displayName = spec.Name;
            food.shelfLifeSeconds = spec.ShelfLife;
            food.icon = GetPlaceholderSprite();
            EditorUtility.SetDirty(food);
            return food;
        }

        private static void Style(GameObject go, Color color, float scale)
        {
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = GetPlaceholderSprite();
            renderer.color = color;
            go.transform.localScale = Vector3.one * scale;
        }

        private static Sprite GetPlaceholderSprite()
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        }

        private static GameObject SavePrefab(GameObject instance, string name)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, $"{PrefabDir}/{name}.prefab");
            UnityEngine.Object.DestroyImmediate(instance);
            return prefab;
        }

        private static InputActionReference FindInputReference(string actionPath)
        {
            var reference = AssetDatabase.LoadAllAssetsAtPath(InputAssetPath)
                .OfType<InputActionReference>()
                .FirstOrDefault(r => r.name == actionPath);
            if (reference == null)
                throw new InvalidOperationException($"{InputAssetPath}에서 InputActionReference '{actionPath}'를 찾지 못했습니다.");
            return reference;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void SetObject(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field)
                ?? throw new MissingFieldException($"{target.GetType().Name}.{field}");
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolders()
        {
            foreach (var (parent, name) in new[] { ("Assets", "ShelfLife"), (Root, "Data"), (Root, "Prefabs"), (Root, "Scenes") })
                if (!AssetDatabase.IsValidFolder($"{parent}/{name}"))
                    AssetDatabase.CreateFolder(parent, name);
        }

        private static int EnsureLayer(string layerName)
        {
            var existing = LayerMask.NameToLayer(layerName);
            if (existing >= 0) return existing;

            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            for (var i = 8; i < layers.arraySize; i++)
            {
                var slot = layers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(slot.stringValue)) continue;
                slot.stringValue = layerName;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }
            throw new InvalidOperationException("빈 레이어 슬롯이 없습니다.");
        }
    }
}
