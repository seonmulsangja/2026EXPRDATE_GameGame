using ExpiryGame.MapGen;
using ExpiryGame.MapGen.Editor;
using ExpiryGame.Visual;
using ExpiryGame.Visual.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace ExpiryGame.World.Editor
{
    // 아트 v2 월드 부분 설치: 걷기 애니메이션, 낮/밤 바닥(Tilemap 2장 크로스페이드), 낮밤 조명, 발광, 비네트.
    // 에셋(SO/머티리얼/타일)은 이미 있으면 수치를 건드리지 않는다.
    public static class WorldSetup
    {
        public const string DataDir = "Assets/ShelfLife/Data/World";
        public const string ProfilePath = DataDir + "/DayNightProfile.asset";
        public const string AdditiveMaterialPath = DataDir + "/SpriteAdditive.mat";

        private const string WalkPath = DataDir + "/WalkAnimationProfile.asset";
        private const string VolumeProfilePath = DataDir + "/NightVolumeProfile.asset";
        private const string TileDir = "Assets/ShelfLife/Data/MapGen/Tiles";
        private const string DirectorName = "DayNight";
        private const string NightGroundName = "GroundNight";

        public static DayNightProfile LoadOrCreateProfile()
        {
            EnsureFolder("Assets/ShelfLife/Data", "World");
            var profile = AssetDatabase.LoadAssetAtPath<DayNightProfile>(ProfilePath);
            if (profile != null) return profile;
            profile = ScriptableObject.CreateInstance<DayNightProfile>();
            profile.ApplyDefaults();
            AssetDatabase.CreateAsset(profile, ProfilePath);
            return profile;
        }

        public static Material LoadOrCreateAdditiveMaterial()
        {
            EnsureFolder("Assets/ShelfLife/Data", "World");
            var material = AssetDatabase.LoadAssetAtPath<Material>(AdditiveMaterialPath);
            if (material != null) return material;
            var shader = Shader.Find("ShelfLife/SpriteAdditive");
            if (shader == null)
            {
                Debug.LogError("[ShelfLife] ShelfLife/SpriteAdditive 셰이더를 찾지 못했습니다. 컴파일/임포트가 끝난 뒤 다시 실행하세요.");
                return null;
            }
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, AdditiveMaterialPath);
            return material;
        }

        [MenuItem("Tools/ShelfLife/Install Day-Night + Walk Anim To Open Scene")]
        public static void Install()
        {
            var inventory = Object.FindFirstObjectByType<FoodInventory>();
            var clock = Object.FindFirstObjectByType<GameClock>();
            if (inventory == null || clock == null)
            {
                Debug.LogError("[ShelfLife] 씬에 Player/GameClock이 없습니다. 테스트 씬을 연 상태에서 실행하세요.");
                return;
            }

            var profile = LoadOrCreateProfile();
            var additive = LoadOrCreateAdditiveMaterial();
            var walk = LoadOrCreateWalkProfile();
            var settings = AssetDatabase.LoadAssetAtPath<MapGenSettings>(MapGenSetup.SettingsPath);

            InstallPlayer(inventory, walk, additive);
            var director = InstallDirector(clock, profile);
            InstallNightGround(settings, director);
            InstallVignette(director);

            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log("[ShelfLife] 낮밤 시각 차이 + 걷기 애니메이션 설치 완료.");
        }

        // ---------- 에셋 ----------

        public static WalkAnimationProfile LoadOrCreateWalkProfile()
        {
            EnsureFolder("Assets/ShelfLife/Data", "World");
            var profile = AssetDatabase.LoadAssetAtPath<WalkAnimationProfile>(WalkPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<WalkAnimationProfile>();
                AssetDatabase.CreateAsset(profile, WalkPath);
            }
            profile.down = Frames("down");
            profile.up = Frames("up");
            profile.left = Frames("left");
            profile.right = Frames("right");
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static DirectionalFrames Frames(string direction)
        {
            return new DirectionalFrames
            {
                idle = ArtAssets.Load("player", $"player_{direction}_idle"),
                walkA = ArtAssets.Load("player", $"player_{direction}_walk_a"),
                walkB = ArtAssets.Load("player", $"player_{direction}_walk_b"),
            };
        }

        // 낮/밤 바닥 타일을 만들고 MapGenSettings에 연결한다 (floor_a/b/c/d → _day / _night)
        public static void LinkFloorTiles(MapGenSettings settings)
        {
            if (settings == null) return;
            var names = new[] { "a", "b", "c", "d" };
            System.Array.Resize(ref settings.floorTiles, names.Length);
            System.Array.Resize(ref settings.nightFloorTiles, names.Length);
            for (var i = 0; i < names.Length; i++)
            {
                settings.floorTiles[i] = CreateTile($"Floor_{names[i].ToUpper()}_Day", $"floor_{names[i]}_day");
                settings.nightFloorTiles[i] = CreateTile($"Floor_{names[i].ToUpper()}_Night", $"floor_{names[i]}_night");
            }
            LinkDecor(settings);
            EditorUtility.SetDirty(settings);
        }

        // 장식 종류를 비어 있을 때만 채운다 (이미 손본 가중치/밀도는 유지). weed/puddle은 day/night 쌍, 나머지는 공용(밤 틴트)
        private static void LinkDecor(MapGenSettings settings)
        {
            if (settings.decor != null && settings.decor.Length > 0) return;

            var nightTint = new Color(0.5f, 0.56f, 0.8f, 1f);
            settings.decor = new[]
            {
                Decor("Weed", "decor_weed_day", "decor_weed_night", 4f, Color.white),
                Decor("Puddle", "decor_puddle_day", "decor_puddle_night", 1.5f, Color.white),
                Decor("Pebbles", "decor_pebbles", null, 3f, nightTint),
                Decor("Fishbone", "decor_fishbone", null, 1f, nightTint),
                Decor("Trash", "decor_trash", null, 1f, nightTint),
            };
            settings.decorMaterial = AssetDatabase.LoadAssetAtPath<Material>(ArtAssets.LitMaterialPath);
        }

        private static DecorDefinition Decor(string name, string day, string night, float weight, Color nightTint)
        {
            return new DecorDefinition
            {
                name = name,
                day = ArtAssets.Load("decor", day),
                night = night != null ? ArtAssets.Load("decor", night) : null,
                weight = weight,
                nightTint = nightTint,
            };
        }

        private static TileBase CreateTile(string assetName, string spriteName)
        {
            var path = $"{TileDir}/{assetName}.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.colliderType = Tile.ColliderType.None;
                AssetDatabase.CreateAsset(tile, path);
            }
            tile.sprite = ArtAssets.Load("tiles", spriteName);
            EditorUtility.SetDirty(tile);
            return tile;
        }

        // ---------- 씬 ----------

        private static void InstallPlayer(FoodInventory inventory, WalkAnimationProfile walk, Material additive)
        {
            var player = inventory.gameObject;
            var body = player.GetComponent<SpriteRenderer>();
            body.sprite = walk.down.idle;

            // 방향만 고르던 v1 컴포넌트는 걷기 애니메이터가 대체한다
            var legacy = player.GetComponent<PlayerSpriteFacing>();
            if (legacy != null) Object.DestroyImmediate(legacy);

            var animator = DarkArtSceneInstaller.GetOrAdd<PlayerWalkAnimator>(player);
            DarkArtSceneInstaller.SetObject(animator, "input", player.GetComponent<PlayerInputReader>());
            DarkArtSceneInstaller.SetObject(animator, "facing", player.GetComponent<PlayerFacing>());
            DarkArtSceneInstaller.SetObject(animator, "body", body);
            DarkArtSceneInstaller.SetObject(animator, "profile", walk);

            // 밤에 플레이어 주변에 은은히 번지는 빛 (Additive 스프라이트. Light 2D 플레이어 라이트는 기존 그대로)
            var glow = DarkArtSceneInstaller.GetOrCreateChild(player.transform, "NightGlow");
            glow.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            glow.transform.localScale = Vector3.one * 1.6f;
            var renderer = DarkArtSceneInstaller.GetOrAdd<SpriteRenderer>(glow);
            renderer.sprite = ArtAssets.Load("fx", "fx_glow_soft");
            renderer.sharedMaterial = additive;
            renderer.color = new Color(1f, 0.85f, 0.6f, 0f);
            renderer.sortingOrder = 1;
            var fader = DarkArtSceneInstaller.GetOrAdd<NightGlowFader>(glow);
            SetFloat(fader, "strength", 0.35f);
        }

        private static DayNightDirector InstallDirector(GameClock clock, DayNightProfile profile)
        {
            Light2D global = null;
            foreach (var light in Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None))
                if (light.lightType == Light2D.LightType.Global) global = light;

            // 같은 전역 라이트를 건드리는 기존 컨트롤러는 끈다 (DayNightDirector가 대체)
            foreach (var legacy in Object.FindObjectsByType<AmbientLightController>(FindObjectsSortMode.None)) legacy.enabled = false;
            foreach (var legacy in Object.FindObjectsByType<DayNightLighting>(FindObjectsSortMode.None)) legacy.enabled = false;

            var host = GameObject.Find(DirectorName) ?? new GameObject(DirectorName);
            var director = DarkArtSceneInstaller.GetOrAdd<DayNightDirector>(host);
            DarkArtSceneInstaller.SetObject(director, "clock", clock);
            DarkArtSceneInstaller.SetObject(director, "profile", profile);
            DarkArtSceneInstaller.SetObject(director, "globalLight", global);
            return director;
        }

        private static void InstallNightGround(MapGenSettings settings, DayNightDirector director)
        {
            var streamer = Object.FindFirstObjectByType<ChunkStreamer>();
            var grid = GameObject.Find("MapGrid");
            if (settings == null || streamer == null || grid == null)
            {
                Debug.LogWarning("[ShelfLife] 무한 맵(MapGrid/ChunkStreamer)이 없어 밤 바닥은 건너뜁니다. Install Infinite Map To Open Scene을 먼저 실행하세요.");
                return;
            }

            LinkFloorTiles(settings);

            var night = DarkArtSceneInstaller.GetOrCreateChild(grid.transform, NightGroundName);
            var tilemap = DarkArtSceneInstaller.GetOrAdd<Tilemap>(night);
            var renderer = DarkArtSceneInstaller.GetOrAdd<TilemapRenderer>(night);
            renderer.sortingOrder = settings.floorSortingOrder + 1;
            var lit = AssetDatabase.LoadAssetAtPath<Material>(ArtAssets.LitMaterialPath);
            if (lit != null) renderer.sharedMaterial = lit;
            tilemap.color = new Color(1f, 1f, 1f, 0f);

            DarkArtSceneInstaller.SetObject(streamer, "nightTilemap", tilemap);

            var fader = DarkArtSceneInstaller.GetOrAdd<NightTilemapFader>(night);
            DarkArtSceneInstaller.SetObject(fader, "director", director);
            DarkArtSceneInstaller.SetObject(fader, "nightTilemap", tilemap);

            var decor = DarkArtSceneInstaller.GetOrAdd<DecorNightController>(director.gameObject);
            DarkArtSceneInstaller.SetObject(decor, "director", director);
        }

        private static void InstallVignette(DayNightDirector director)
        {
            var volumeProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (volumeProfile == null)
            {
                volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
                var vignette = volumeProfile.Add<Vignette>(true);
                vignette.intensity.Override(0.2f);
                vignette.smoothness.Override(0.5f);
                vignette.color.Override(Color.black);
                AssetDatabase.CreateAsset(volumeProfile, VolumeProfilePath);
            }

            var host = GameObject.Find("NightVolume") ?? new GameObject("NightVolume");
            var volume = DarkArtSceneInstaller.GetOrAdd<Volume>(host);
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = volumeProfile;

            var vignetteComponent = DarkArtSceneInstaller.GetOrAdd<NightVignette>(host);
            DarkArtSceneInstaller.SetObject(vignetteComponent, "director", director);
            DarkArtSceneInstaller.SetObject(vignetteComponent, "volume", volume);

            var camera = Camera.main;
            if (camera != null) camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        }

        private static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}")) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
