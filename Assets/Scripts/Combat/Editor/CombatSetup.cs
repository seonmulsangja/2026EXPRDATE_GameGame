using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ExpiryGame.Visual.Editor;

namespace ExpiryGame.Combat.Editor
{
    // 휘두르기 + 타격감 설치 도구. 열린 씬의 플레이어에 컴포넌트를 붙이고, 테스트 적 더미/허수아비 프리팹을 만든다.
    public static class CombatSetup
    {
        private const string DataDir = "Assets/ShelfLife/Data";
        private const string CombatDir = DataDir + "/Combat";
        private const string PrefabDir = "Assets/ShelfLife/Prefabs";
        private const string SwingProfilePath = CombatDir + "/SwingProfile.asset";
        private const string HitFeelProfilePath = CombatDir + "/HitFeelProfile.asset";
        private const string FlashMaterialPath = CombatDir + "/SpriteFlash.mat";
        private const string FxRootName = "CombatFx";
        private const string EnemyLayerName = "Enemy";

        [MenuItem("Tools/ShelfLife/Setup Combat Test (All In One)")]
        public static void SetupAll()
        {
            ArtImporter.ApplyAll();
            ExpiryGame.Editor.TestSceneBuilder.Build();
            Install();
            DarkArtSceneInstaller.Apply();
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[ShelfLife] 전투 테스트 씬 준비 완료. Play 후 마우스 좌클릭으로 휘두르세요.");
        }

        [MenuItem("Tools/ShelfLife/Install Combat To Open Scene")]
        public static void Install()
        {
            var inventory = Object.FindFirstObjectByType<FoodInventory>();
            if (inventory == null)
            {
                Debug.LogError("[ShelfLife] 씬에 플레이어(FoodInventory)가 없습니다. ShelfLifeTest 씬을 연 상태에서 실행하세요.");
                return;
            }

            FoodSpriteLinker.Link();
            EnsureFolder(DataDir, "Combat");

            var swing = LoadOrCreate<SwingProfile>(SwingProfilePath);
            var feel = LoadOrCreateFeel();
            var flashMaterial = LoadOrCreateFlashMaterial();
            var unlit = AssetDatabase.LoadAssetAtPath<Material>(ArtAssets.UnlitMaterialPath);
            var enemyLayer = LayerMask.NameToLayer(EnemyLayerName);

            var fx = InstallFxRoot(unlit);
            var hitStop = DarkArtSceneInstaller.GetOrAdd<HitStop>(fx.gameObject);
            var shake = DarkArtSceneInstaller.GetOrAdd<CameraShake>(Camera.main.gameObject);

            InstallPlayer(inventory.gameObject, swing, feel, enemyLayer, fx, hitStop, shake);
            ReplaceEnemies(flashMaterial, enemyLayer);
            PlaceExtraPickups();

            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log("[ShelfLife] 전투(휘두르기/타격감) 설치 완료.");
        }

        private static HitFxSpawner InstallFxRoot(Material unlit)
        {
            var root = GameObject.Find(FxRootName) ?? new GameObject(FxRootName);
            var spawner = DarkArtSceneInstaller.GetOrAdd<HitFxSpawner>(root);
            SetArray(spawner, "slashFrames", ArtAssets.LoadFrames("fx", "fx_slash", 5));
            SetArray(spawner, "hitSparkFrames", ArtAssets.LoadFrames("fx", "fx_hitspark", 5));
            DarkArtSceneInstaller.SetObject(spawner, "crumb", ArtAssets.Load("fx", "particle_crumb"));
            DarkArtSceneInstaller.SetObject(spawner, "juice", ArtAssets.Load("fx", "particle_juice"));
            DarkArtSceneInstaller.SetObject(spawner, "rot", ArtAssets.Load("fx", "particle_rot"));
            DarkArtSceneInstaller.SetObject(spawner, "spark", ArtAssets.Load("fx", "particle_spark"));
            DarkArtSceneInstaller.SetObject(spawner, "fxMaterial", unlit);
            return spawner;
        }

        private static void InstallPlayer(GameObject player, SwingProfile swing, HitFeelProfile feel, int enemyLayer,
            HitFxSpawner fx, HitStop hitStop, CameraShake shake)
        {
            var facing = player.GetComponent<PlayerFacing>();

            var aim = DarkArtSceneInstaller.GetOrAdd<AimSource>(player);
            DarkArtSceneInstaller.SetObject(aim, "facing", facing);

            var hand = DarkArtSceneInstaller.GetOrCreateChild(player.transform, "HandPivot");
            hand.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            var weaponGo = DarkArtSceneInstaller.GetOrCreateChild(hand.transform, "Weapon");
            var weapon = DarkArtSceneInstaller.GetOrAdd<SpriteRenderer>(weaponGo);
            weapon.sortingOrder = 6;
            weapon.enabled = false;
            DarkArtSceneInstaller.EnsureLit(weapon);

            var detector = DarkArtSceneInstaller.GetOrAdd<SwingHitDetector>(player);
            DarkArtSceneInstaller.SetObject(detector, "profile", swing);
            DarkArtSceneInstaller.SetObject(detector, "feel", feel);
            SetLayerMask(detector, "targetMask", 1 << enemyLayer);

            var controller = DarkArtSceneInstaller.GetOrAdd<SwingController>(player);
            DarkArtSceneInstaller.SetObject(controller, "profile", swing);
            DarkArtSceneInstaller.SetObject(controller, "aimSource", aim);
            DarkArtSceneInstaller.SetObject(controller, "hitSink", detector);
            DarkArtSceneInstaller.SetObject(controller, "handPivot", hand.transform);
            DarkArtSceneInstaller.SetObject(controller, "weaponRenderer", weapon);
            var held = player.transform.Find("HeldFood");
            SetArray(controller, "hideWhileSwinging", held != null ? new Object[] { held.gameObject } : new Object[0]);

            var knockback = DarkArtSceneInstaller.GetOrAdd<KnockbackReceiver>(player);

            var feedback = DarkArtSceneInstaller.GetOrAdd<HitFeedbackPlayer>(player);
            DarkArtSceneInstaller.SetObject(feedback, "feel", feel);
            DarkArtSceneInstaller.SetObject(feedback, "swing", swing);
            DarkArtSceneInstaller.SetObject(feedback, "swingController", controller);
            DarkArtSceneInstaller.SetObject(feedback, "hitDetector", detector);
            DarkArtSceneInstaller.SetObject(feedback, "hitStop", hitStop);
            DarkArtSceneInstaller.SetObject(feedback, "cameraShake", shake);
            DarkArtSceneInstaller.SetObject(feedback, "fx", fx);
            DarkArtSceneInstaller.SetObject(feedback, "recoilReceiver", knockback);

            var weaponComponent = player.GetComponent<FoodWeapon>();
            DarkArtSceneInstaller.SetObject(weaponComponent, "attackDriver", controller);
        }

        private static void ReplaceEnemies(Material flashMaterial, int enemyLayer)
        {
            // Build Test Scene이 놓은 임시 EnemyDummy는 제거하고 전투 테스트용으로 교체한다
            foreach (var health in Object.FindObjectsByType<Health>(FindObjectsSortMode.None))
                if (health.GetComponent<FoodInventory>() == null && health.name.StartsWith("EnemyDummy"))
                    Object.DestroyImmediate(health.gameObject);

            var dummy = CreateTargetPrefab("CombatDummy", "player_down_idle", new Color(1f, 0.6f, 0.6f), false, flashMaterial, enemyLayer);
            var scarecrow = CreateTargetPrefab("Scarecrow", "obstacle_barrel", Color.white, true, flashMaterial, enemyLayer);

            PlaceIfMissing(dummy, "CombatDummy_1", new Vector3(4f, 2f));
            PlaceIfMissing(dummy, "CombatDummy_2", new Vector3(4f, -2f));
            PlaceIfMissing(scarecrow, "Scarecrow_1", new Vector3(6.5f, 0f));
        }

        private static GameObject CreateTargetPrefab(string name, string spriteName, Color tint, bool invulnerable,
            Material flashMaterial, int enemyLayer)
        {
            var go = new GameObject(name, typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(Rigidbody2D));
            go.layer = enemyLayer;

            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = ArtAssets.Load(spriteName == "obstacle_barrel" ? "obstacles" : "player", spriteName);
            renderer.color = tint;
            renderer.sortingOrder = 2;
            DarkArtSceneInstaller.EnsureLit(renderer);

            var circle = go.GetComponent<CircleCollider2D>();
            circle.radius = 0.3f;
            circle.offset = new Vector2(0f, 0.3f);

            var body = go.GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.linearDamping = 10f;

            var target = go.AddComponent<TrainingTarget>();
            var targetSo = new SerializedObject(target);
            targetSo.FindProperty("invulnerable").boolValue = invulnerable;
            targetSo.ApplyModifiedPropertiesWithoutUndo();

            go.AddComponent<KnockbackReceiver>();
            var flash = go.AddComponent<FlashOnHit>();
            DarkArtSceneInstaller.SetObject(flash, "flashMaterial", flashMaterial);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/{name}.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static void PlaceIfMissing(GameObject prefab, string name, Vector3 position)
        {
            if (GameObject.Find(name) != null) return;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = name;
            instance.transform.position = position;
        }

        private static void PlaceExtraPickups()
        {
            var pickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/FoodPickup.prefab");
            if (pickupPrefab == null) return;

            var column = 0;
            foreach (var food in FoodSpriteLinker.LoadAll())
            {
                var pickupName = $"Pickup_{food.displayName}";
                if (GameObject.Find(pickupName) != null) continue;

                var pickup = (GameObject)PrefabUtility.InstantiatePrefab(pickupPrefab);
                pickup.name = pickupName;
                pickup.transform.position = new Vector3(-7f, 3.5f - column * 1.1f);
                DarkArtSceneInstaller.SetObject(pickup.GetComponent<FoodPickup>(), "food", food);
                column++;
            }
        }

        private static HitFeelProfile LoadOrCreateFeel()
        {
            var created = AssetDatabase.LoadAssetAtPath<HitFeelProfile>(HitFeelProfilePath) == null;
            var feel = LoadOrCreate<HitFeelProfile>(HitFeelProfilePath);
            if (created || feel.weaponFx == null || feel.weaponFx.Length == 0)
            {
                feel.weaponFx = FoodSpriteLinker.LoadAll()
                    .Select(food => new WeaponFxEntry
                    {
                        food = food,
                        kind = FoodSpriteLinker.IsJuicy(food) ? ParticleKind.Juice : ParticleKind.Crumb,
                    })
                    .ToArray();
                EditorUtility.SetDirty(feel);
            }
            return feel;
        }

        private static Material LoadOrCreateFlashMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(FlashMaterialPath);
            if (material != null) return material;

            var shader = Shader.Find("ShelfLife/SpriteFlash");
            if (shader == null)
            {
                Debug.LogError("[ShelfLife] ShelfLife/SpriteFlash 셰이더를 찾지 못했습니다. Console의 셰이더 컴파일 에러를 확인하세요.");
                return null;
            }
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, FlashMaterialPath);
            return material;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}")) AssetDatabase.CreateFolder(parent, name);
        }

        private static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var property = so.FindProperty(field);
            property.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetLayerMask(Object target, string field, int mask)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).intValue = mask;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
