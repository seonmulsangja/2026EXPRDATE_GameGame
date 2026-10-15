using ExpiryGame.Visual.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ExpiryGame.MapGen.Editor
{
    // 열린 씬에 무한 맵을 설치한다: 타일/MapGenSettings 에셋, Grid+Tilemap, ChunkStreamer, 카메라 추적, 디버그 오버레이.
    // 기존 고정 아레나(Floor/Walls/Obstacles)는 삭제하지 않고 비활성화만 한다.
    public static class MapGenSetup
    {
        public const string SettingsPath = DataDir + "/MapGenSettings.asset";

        private const string DataDir = "Assets/ShelfLife/Data/MapGen";
        private const string TileDir = DataDir + "/Tiles";
        private const string GridName = "MapGrid";
        private const string GroundName = "Ground";
        private const string ObstacleRootName = "MapObstacles";
        private const string StreamerName = "MapGen";

        private static readonly string[] LegacyArenaRoots = { "Floor", "Walls", "Obstacles" };

        [MenuItem("Tools/ShelfLife/Install Infinite Map To Open Scene")]
        public static void Install()
        {
            var player = Object.FindFirstObjectByType<FoodInventory>();
            if (player == null)
            {
                Debug.LogError("[ShelfLife] 씬에 Player(FoodInventory)가 없습니다. ShelfLifeTest 계열 씬을 연 상태에서 실행하세요.");
                return;
            }

            EnsureFolder("Assets/ShelfLife", "Data");
            EnsureFolder("Assets/ShelfLife/Data", "MapGen");
            EnsureFolder(DataDir, "Tiles");

            var settings = LoadOrCreateSettings();
            DisableLegacyArena();
            var tilemap = InstallTilemap(settings);
            var streamer = InstallStreamer(settings, player.transform, tilemap);
            InstallCamera(player.transform);
            InstallOverlay(streamer);

            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log("[ShelfLife] 무한 맵 설치 완료. Play 후 WASD로 이동해 보세요 (화면 오른쪽 위에 청크 좌표 표시).");
        }

        private static MapGenSettings LoadOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<MapGenSettings>(SettingsPath);
            if (settings != null) return settings;

            settings = ScriptableObject.CreateInstance<MapGenSettings>();
            settings.floorTiles = new TileBase[]
            {
                CreateTile("Floor_A", "floor_a"),
                CreateTile("Floor_B", "floor_b"),
                CreateTile("Floor_C", "floor_c"),
            };
            settings.obstacles = new[]
            {
                Obstacle("Crate", "obstacle_crate", 3f),
                Obstacle("Barrel", "obstacle_barrel", 2f),
                Obstacle("Rubble", "obstacle_rubble", 4f),
            };
            settings.obstacleMaterial = AssetDatabase.LoadAssetAtPath<Material>(ArtAssets.LitMaterialPath);
            AssetDatabase.CreateAsset(settings, SettingsPath);
            return settings;
        }

        private static ObstacleDefinition Obstacle(string name, string spriteName, float weight)
        {
            return new ObstacleDefinition
            {
                name = name,
                sprite = ArtAssets.Load("obstacles", spriteName),
                weight = weight,
                colliderSize = new Vector2(0.8f, 0.6f),
                colliderOffset = new Vector2(0f, 0.3f),
            };
        }

        private static TileBase CreateTile(string assetName, string spriteName)
        {
            var path = $"{TileDir}/{assetName}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile != null) return tile;

            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = ArtAssets.Load("tiles", spriteName);
            tile.colliderType = Tile.ColliderType.None;
            AssetDatabase.CreateAsset(tile, path);
            return tile;
        }

        private static void DisableLegacyArena()
        {
            foreach (var name in LegacyArenaRoots)
            {
                var root = GameObject.Find(name);
                if (root != null) root.SetActive(false);
            }
        }

        private static Tilemap InstallTilemap(MapGenSettings settings)
        {
            var grid = GameObject.Find(GridName) ?? new GameObject(GridName);
            var gridComponent = DarkArtSceneInstaller.GetOrAdd<Grid>(grid);
            gridComponent.cellSize = Vector3.one;

            var ground = DarkArtSceneInstaller.GetOrCreateChild(grid.transform, GroundName);
            var tilemap = DarkArtSceneInstaller.GetOrAdd<Tilemap>(ground);
            var renderer = DarkArtSceneInstaller.GetOrAdd<TilemapRenderer>(ground);
            renderer.sortingOrder = settings.floorSortingOrder;
            var lit = AssetDatabase.LoadAssetAtPath<Material>(ArtAssets.LitMaterialPath);
            if (lit != null) renderer.sharedMaterial = lit;
            return tilemap;
        }

        private static ChunkStreamer InstallStreamer(MapGenSettings settings, Transform player, Tilemap tilemap)
        {
            var obstacleRoot = GameObject.Find(ObstacleRootName) ?? new GameObject(ObstacleRootName);
            var host = GameObject.Find(StreamerName) ?? new GameObject(StreamerName);
            var streamer = DarkArtSceneInstaller.GetOrAdd<ChunkStreamer>(host);
            DarkArtSceneInstaller.SetObject(streamer, "settings", settings);
            DarkArtSceneInstaller.SetObject(streamer, "target", player);
            DarkArtSceneInstaller.SetObject(streamer, "tilemap", tilemap);
            DarkArtSceneInstaller.SetObject(streamer, "obstacleRoot", obstacleRoot.transform);
            return streamer;
        }

        private static void InstallCamera(Transform player)
        {
            var camera = Camera.main;
            if (camera == null) return;
            var follow = DarkArtSceneInstaller.GetOrAdd<CameraFollow2D>(camera.gameObject);
            DarkArtSceneInstaller.SetObject(follow, "target", player);
        }

        private static void InstallOverlay(ChunkStreamer streamer)
        {
            var overlay = DarkArtSceneInstaller.GetOrAdd<MapDebugOverlay>(streamer.gameObject);
            DarkArtSceneInstaller.SetObject(overlay, "streamer", streamer);
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}")) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
