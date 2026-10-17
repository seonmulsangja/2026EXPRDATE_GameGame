using System.IO;
using System.Linq;
using ExpiryGame.Flow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ExpiryGame.Integration.Editor
{
    // 팀원 증강 시스템(JCY)을 열린 씬에 붙인다. 팀원 코드/SO/씬은 수정하지 않는다.
    //  - 팀원 선택 화면은 Test_JCY.unity 안의 씬 오브젝트(AugmentCanvas)라서, 읽기만 해서 우리 쪽 프리팹으로 복사한다
    //  - 이미 씬에 있는 것(AugmentUI/AugmentManager/어댑터)은 다시 만들지 않고 참조만 다시 채운다
    // 어셈블리 경계: 팀원 코드가 Assembly-CSharp라서 이 폴더도 asmdef 없이 같은 어셈블리에 둔다.
    public static class AugmentIntegrationInstaller
    {
        public const string MenuPath = "Tools/ShelfLife/Install Augments To Open Scene";
        private const string TeamScenePath = "Assets/JCY/Test_JCY.unity";
        private const string TeamCanvasName = "AugmentCanvas";
        private const string TeamAugmentDir = "Assets/JCY/02 SO/Augment";
        private const string PrefabPath = "Assets/ShelfLife/Prefabs/AugmentCanvas.prefab";
        private const string DataDir = "Assets/ShelfLife/Data/Integration";
        private const string SettingsPath = DataDir + "/AugmentIntegrationSettings.asset";
        private const string BackendName = "AugmentBackend (JCY)";

        [MenuItem(MenuPath)]
        public static void Install()
        {
            var flow = Object.FindFirstObjectByType<GameFlowRoot>();
            var inventory = Object.FindFirstObjectByType<FoodInventory>();
            if (flow == null || inventory == null)
            {
                Debug.LogError("[ShelfLife] 씬에 GameFlow/Player가 필요합니다. 먼저 Install Game Flow To Open Scene을 실행하세요.");
                return;
            }

            var settings = LoadOrCreateSettings();
            var augments = LoadAugments();
            if (augments.Length == 0)
            {
                Debug.LogError($"[ShelfLife] {TeamAugmentDir} 에 증강 SO가 없습니다. origin/JCY를 병합했는지 확인하세요.");
                return;
            }

            var ui = Object.FindFirstObjectByType<AugmentUI>(FindObjectsInactive.Include);
            if (ui == null)
            {
                var prefab = EnsureCanvasPrefab();
                if (prefab == null) return;
                var canvasObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                ui = canvasObject.GetComponentInChildren<AugmentUI>(true);
            }
            var canvas = ui.GetComponentInParent<Canvas>(true);
            if (canvas != null)
            {
                canvas.sortingOrder = settings.augmentCanvasSortingOrder; // HUD와 흐름 화면(100) 위
                canvas.gameObject.SetActive(true); // 팀원 씬은 AugmentCanvas 루트를 꺼 둔 채 저장돼 있다 (켜져 있어야 AugmentUI.Awake가 패널을 숨긴다)
            }

            WireSlots(ui);
            var uiSo = new SerializedObject(ui);
            var panel = uiSo.FindProperty("panel").objectReferenceValue as GameObject;

            // 팀원 매니저/상태 보관소는 플레이어가 아닌 별도 오브젝트에 둔다 (AugmentState가 이동 속도/공격 스탯을 직접 덮어쓰기 때문)
            var backend = GameObject.Find(BackendName) ?? new GameObject(BackendName);
            var state = Object.FindFirstObjectByType<AugmentState>(FindObjectsInactive.Include);
            if (state == null) state = backend.AddComponent<AugmentState>();
            else if (state.gameObject == inventory.gameObject)
                Debug.LogWarning("[ShelfLife] AugmentState가 플레이어에 붙어 있습니다. 이동 속도/공격 스탯을 직접 덮어써 능력치 파이프라인과 충돌하니 떼어 주세요.", state);

            var manager = Object.FindFirstObjectByType<AugmentManager>(FindObjectsInactive.Include);
            if (manager == null) manager = backend.AddComponent<AugmentManager>();
            var managerSo = new SerializedObject(manager);
            SetList(managerSo.FindProperty("allAugments"), augments);
            managerSo.FindProperty("augmentUI").objectReferenceValue = ui;
            managerSo.FindProperty("playerState").objectReferenceValue = state;
            managerSo.ApplyModifiedPropertiesWithoutUndo();

            var player = inventory.gameObject;
            var skills = player.GetComponent<PlayerSkillController>() ?? player.AddComponent<PlayerSkillController>();

            var presenter = flow.GetComponent<TeammateAugmentPresenter>() ?? flow.gameObject.AddComponent<TeammateAugmentPresenter>();
            var presenterSo = new SerializedObject(presenter);
            presenterSo.FindProperty("manager").objectReferenceValue = manager;
            presenterSo.FindProperty("augmentUI").objectReferenceValue = ui;
            presenterSo.FindProperty("state").objectReferenceValue = state;
            presenterSo.FindProperty("panelRoot").objectReferenceValue = panel;
            presenterSo.FindProperty("skills").objectReferenceValue = skills;
            presenterSo.FindProperty("settings").objectReferenceValue = settings;
            presenterSo.FindProperty("choiceCount").intValue = Mathf.Max(1, managerSo.FindProperty("choiceCount").intValue);
            SetList(presenterSo.FindProperty("augments"), augments);
            presenterSo.ApplyModifiedPropertiesWithoutUndo();

            var flowSo = new SerializedObject(flow);
            flowSo.FindProperty("augmentPresenter").objectReferenceValue = presenter;
            flowSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log($"[ShelfLife] 증강 통합 설치 완료 (증강 {augments.Length}종). 수치: {SettingsPath}");
        }

        // Test_JCY 씬의 AugmentCanvas를 읽어서 프리팹으로 복사한다 (원본 씬은 저장하지 않고 닫는다)
        private static GameObject EnsureCanvasPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null) return existing;

            if (!File.Exists(TeamScenePath))
            {
                Debug.LogError($"[ShelfLife] {TeamScenePath} 가 없어 증강 선택 화면을 만들 수 없습니다. origin/JCY를 병합했는지 확인하세요.");
                return null;
            }

            EnsureFolder(Path.GetDirectoryName(PrefabPath).Replace('\\', '/'));
            var scene = EditorSceneManager.OpenScene(TeamScenePath, OpenSceneMode.Additive);
            try
            {
                var source = scene.GetRootGameObjects().FirstOrDefault(go => go.name == TeamCanvasName);
                if (source == null)
                {
                    Debug.LogError($"[ShelfLife] {TeamScenePath} 에서 '{TeamCanvasName}' 를 찾지 못했습니다.");
                    return null;
                }
                // 팀원 씬은 루트가 꺼져 있고 AugmentUI.slots가 비어 있다 (슬롯이 연결되지 않으면 클릭해도 아무 일도 일어나지 않는다).
                // 복사본(프리팹)을 만들기 전에 고쳐 둔다 — 원본 씬은 저장하지 않고 닫으므로 팀원 파일은 그대로다.
                source.SetActive(true);
                var teamUi = source.GetComponentInChildren<AugmentUI>(true);
                if (teamUi != null) WireSlots(teamUi);
                return PrefabUtility.SaveAsPrefabAsset(source, PrefabPath);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        // AugmentUI.slots(private)를 캔버스 안의 AugmentSlotUI들로 채운다 (계층 순서). 이미 맞으면 그대로 둔다
        private static void WireSlots(AugmentUI ui)
        {
            var found = ui.GetComponentsInChildren<AugmentSlotUI>(true);
            var so = new SerializedObject(ui);
            var slots = so.FindProperty("slots");
            var same = slots.arraySize == found.Length;
            for (var i = 0; same && i < found.Length; i++) same = slots.GetArrayElementAtIndex(i).objectReferenceValue == found[i];
            if (same) return;

            slots.arraySize = found.Length;
            for (var i = 0; i < found.Length; i++) slots.GetArrayElementAtIndex(i).objectReferenceValue = found[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static AugmentSO[] LoadAugments()
        {
            return AssetDatabase.FindAssets("t:AugmentSO", new[] { TeamAugmentDir })
                .Select(guid => AssetDatabase.LoadAssetAtPath<AugmentSO>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(a => a != null)
                .OrderBy(a => a.name)
                .ToArray();
        }

        private static void SetList(SerializedProperty list, AugmentSO[] items)
        {
            list.arraySize = items.Length;
            for (var i = 0; i < items.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        private static AugmentIntegrationSettings LoadOrCreateSettings()
        {
            EnsureFolder(DataDir);
            var settings = AssetDatabase.LoadAssetAtPath<AugmentIntegrationSettings>(SettingsPath);
            if (settings != null) return settings;

            settings = ScriptableObject.CreateInstance<AugmentIntegrationSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
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
