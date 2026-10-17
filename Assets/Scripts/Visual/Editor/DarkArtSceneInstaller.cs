using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ExpiryGame.Visual.Editor
{
    // 열려 있는 씬에 다크 아트(실제 스프라이트)를 적용한다. 여러 번 실행해도 중복 생성하지 않는다.
    public static class DarkArtSceneInstaller
    {
        private const string HeldFoodName = "HeldFood";
        private const string PlayerLightName = "PlayerLight";
        private const string ShadowName = "Shadow";
        private const string FloorName = "Floor";
        private const string WallsName = "Walls";
        private const string ObstaclesName = "Obstacles";
        private const string AmbientName = "AmbientLighting";

        private const float ArenaHalfWidth = 13f;
        private const float ArenaHalfHeight = 8f;
        private const float HeldFoodScale = 0.6f;
        private static readonly Vector3 HeldFoodOffset = new Vector3(0f, 1.35f, 0f);

        [MenuItem("Tools/ShelfLife/Apply Dark Art To Open Scene")]
        public static void Apply()
        {
            var theme = ArtAssets.LoadOrCreateTheme();
            FoodSpriteLinker.Link();

            var clock = Object.FindFirstObjectByType<GameClock>();
            if (clock == null)
            {
                Debug.LogError("[ShelfLife] 씬에 GameClock이 없습니다. ShelfLifeTest 씬을 연 상태에서 실행하세요.");
                return;
            }

            InstallCamera(theme);
            InstallAmbient(theme, clock);
            InstallFloor();
            InstallWalls();
            InstallObstacles();
            InstallActors(theme, clock);
            InstallPickups(theme);

            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("[ShelfLife] 다크 아트 적용 완료. 씬을 저장(Ctrl+S)하세요.");
        }

        private static void InstallCamera(ArtTheme theme)
        {
            var camera = Camera.main;
            if (camera == null) return;
            var backdrop = GetOrAdd<CameraBackdrop>(camera.gameObject);
            SetObject(backdrop, "theme", theme);
            camera.backgroundColor = theme.backgroundColor;
        }

        private static void InstallAmbient(ArtTheme theme, GameClock clock)
        {
            Light2D global = null;
            foreach (var light in Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None))
                if (light.lightType == Light2D.LightType.Global) global = light;

            if (global == null)
            {
                global = new GameObject("Global Light 2D").AddComponent<Light2D>();
                global.lightType = Light2D.LightType.Global;
            }

            // 기존 DayNightLighting이 같은 라이트의 색을 덮어쓰지 않도록 끈다
            foreach (var legacy in Object.FindObjectsByType<DayNightLighting>(FindObjectsSortMode.None))
                legacy.enabled = false;

            var host = GameObject.Find(AmbientName) ?? new GameObject(AmbientName);
            var controller = GetOrAdd<AmbientLightController>(host);
            SetObject(controller, "theme", theme);
            SetObject(controller, "clock", clock);
            SetObject(controller, "globalLight", global);
        }

        private static void InstallFloor()
        {
            var floor = GameObject.Find(FloorName) ?? new GameObject(FloorName);
            var renderer = GetOrAdd<SpriteRenderer>(floor);
            renderer.sprite = ArtAssets.Load("tiles", "floor_a");
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(ArenaHalfWidth * 2f + 2f, ArenaHalfHeight * 2f + 2f);
            renderer.sortingOrder = -100;
            EnsureLit(renderer);
        }

        private static void InstallWalls()
        {
            var root = GameObject.Find(WallsName) ?? new GameObject(WallsName);
            var sprite = ArtAssets.Load("tiles", "wall");
            var w = ArenaHalfWidth + 1f;
            var h = ArenaHalfHeight + 1f;
            BuildWall(root.transform, "Wall_Top", sprite, new Vector2(0f, h), new Vector2(w * 2f + 1f, 1f));
            BuildWall(root.transform, "Wall_Bottom", sprite, new Vector2(0f, -h), new Vector2(w * 2f + 1f, 1f));
            BuildWall(root.transform, "Wall_Left", sprite, new Vector2(-w, 0f), new Vector2(1f, h * 2f - 1f));
            BuildWall(root.transform, "Wall_Right", sprite, new Vector2(w, 0f), new Vector2(1f, h * 2f - 1f));
        }

        private static void BuildWall(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size)
        {
            var go = GetOrCreateChild(parent, name);
            go.transform.position = position;
            var renderer = GetOrAdd<SpriteRenderer>(go);
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = size;
            renderer.sortingOrder = 3;
            EnsureLit(renderer);
            var box = GetOrAdd<BoxCollider2D>(go);
            box.size = size;
        }

        private static void InstallObstacles()
        {
            var root = GameObject.Find(ObstaclesName) ?? new GameObject(ObstaclesName);
            PlaceObstacle(root.transform, "Crate_1", "obstacle_crate", new Vector2(-8f, 4f));
            PlaceObstacle(root.transform, "Crate_2", "obstacle_crate", new Vector2(8f, -5f));
            PlaceObstacle(root.transform, "Barrel_1", "obstacle_barrel", new Vector2(8f, 5f));
            PlaceObstacle(root.transform, "Barrel_2", "obstacle_barrel", new Vector2(-9f, -5f));
            PlaceObstacle(root.transform, "Rubble_1", "obstacle_rubble", new Vector2(0f, 6f));
            PlaceObstacle(root.transform, "Rubble_2", "obstacle_rubble", new Vector2(1f, -6f));
        }

        private static void PlaceObstacle(Transform parent, string name, string spriteName, Vector2 position)
        {
            var go = GetOrCreateChild(parent, name);
            go.transform.position = position;
            var renderer = GetOrAdd<SpriteRenderer>(go);
            renderer.sprite = ArtAssets.Load("obstacles", spriteName);
            renderer.sortingOrder = 1;
            EnsureLit(renderer);
            var box = GetOrAdd<BoxCollider2D>(go);
            box.size = new Vector2(0.8f, 0.6f);
            box.offset = new Vector2(0f, 0.3f);
        }

        private static void InstallActors(ArtTheme theme, GameClock clock)
        {
            foreach (var health in Object.FindObjectsByType<Health>(FindObjectsSortMode.None))
            {
                var inventory = health.GetComponent<FoodInventory>();
                if (inventory == null) continue; // 적은 전투 셋업이 담당

                var renderer = health.GetComponent<SpriteRenderer>();
                renderer.color = Color.white;
                renderer.sprite = ArtAssets.Load("player", "player_down_idle");
                renderer.sortingOrder = 2;
                health.transform.localScale = Vector3.one;
                EnsureLit(renderer);

                var circle = health.GetComponent<CircleCollider2D>();
                if (circle != null)
                {
                    circle.radius = 0.28f;
                    circle.offset = new Vector2(0f, 0.3f);
                }

                InstallPlayerParts(health.gameObject, inventory, renderer, theme, clock);
            }
        }

        private static void InstallPlayerParts(GameObject player, FoodInventory inventory, SpriteRenderer body,
            ArtTheme theme, GameClock clock)
        {
            var shadow = GetOrCreateChild(player.transform, ShadowName);
            var shadowRenderer = GetOrAdd<SpriteRenderer>(shadow);
            shadowRenderer.sprite = ArtAssets.Load("player", "shadow");
            shadowRenderer.sortingOrder = 0;
            EnsureLit(shadowRenderer);

            var facing = GetOrAdd<PlayerSpriteFacing>(player);
            SetObject(facing, "facing", player.GetComponent<PlayerFacing>());
            SetObject(facing, "body", body);
            SetObject(facing, "down", ArtAssets.Load("player", "player_down_idle"));
            SetObject(facing, "up", ArtAssets.Load("player", "player_up_idle"));
            SetObject(facing, "left", ArtAssets.Load("player", "player_left_idle"));
            SetObject(facing, "right", ArtAssets.Load("player", "player_right_idle"));

            var lightHost = GetOrCreateChild(player.transform, PlayerLightName);
            lightHost.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            var light = GetOrAdd<Light2D>(lightHost);
            light.lightType = Light2D.LightType.Point;
            var playerLight = GetOrAdd<PlayerLight>(lightHost);
            SetObject(playerLight, "theme", theme);
            SetObject(playerLight, "clock", clock);
            SetObject(playerLight, "playerLight", light);

            var heldHost = GetOrCreateChild(player.transform, HeldFoodName);
            heldHost.transform.localPosition = HeldFoodOffset;
            heldHost.transform.localScale = Vector3.one * HeldFoodScale;
            var heldBody = GetOrAdd<SpriteRenderer>(heldHost);
            heldBody.sortingOrder = 5;
            EnsureLit(heldBody);

            var visual = GetOrAdd<FoodVisual>(heldHost);
            SetObject(visual, "theme", theme);
            SetObject(visual, "clock", clock);
            SetObject(visual, "body", heldBody);

            var held = GetOrAdd<HeldFoodVisual>(heldHost);
            SetObject(held, "inventory", inventory);
            SetObject(held, "visual", visual);
        }

        private static void InstallPickups(ArtTheme theme)
        {
            foreach (var pickup in Object.FindObjectsByType<FoodPickup>(FindObjectsSortMode.None))
            {
                var food = new SerializedObject(pickup).FindProperty("food").objectReferenceValue as FoodData;
                var renderer = pickup.GetComponent<SpriteRenderer>();
                if (food == null || renderer == null) continue;

                renderer.color = Color.white;
                renderer.sprite = food.sprite;
                renderer.sortingOrder = 1;
                pickup.transform.localScale = Vector3.one;
                EnsureLit(renderer);

                var visual = GetOrAdd<FoodVisual>(pickup.gameObject);
                SetObject(visual, "theme", theme);
                SetObject(visual, "body", renderer);
                SetObject(visual, "staticFood", food);
            }
        }

        public static void EnsureLit(SpriteRenderer renderer)
        {
            var lit = AssetDatabase.LoadAssetAtPath<Material>(ArtAssets.LitMaterialPath);
            if (lit != null) renderer.sharedMaterial = lit;
        }

        public static GameObject GetOrCreateChild(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null) return existing.gameObject;
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        public static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        public static void SetObject(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[ShelfLife] {target.GetType().Name}.{field} 필드를 찾지 못했습니다.");
                return;
            }
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
