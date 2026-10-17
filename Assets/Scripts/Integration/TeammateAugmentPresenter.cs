using System;
using System.Collections.Generic;
using ExpiryGame.Flow;
using UnityEngine;

namespace ExpiryGame.Integration
{
    // 팀원의 증강 선택 화면을 IAugmentSelectionPresenter로 여는 어댑터. GameFlowRoot.augmentPresenter 슬롯에 끼운다.
    //  레벨업 → (게임은 이미 AugmentSelect로 멈춤) RequestSelection → 팀원 화면 열기 → 선택 → 효과 적용 → onComplete(재개)
    // 후보 부족/에러/선택 없이 닫힘 처리는 GuardedAugmentPresenter(Flow)가, 팀원 화면 접근은 TeammateAugmentChooser가 맡고
    // 이 클래스는 둘을 이어 붙이고 Update에서 선택 완료를 확인(Tick)할 뿐이다.
    public class TeammateAugmentPresenter : MonoBehaviour, IAugmentSelectionPresenter, IAugmentPresenterConsumer, IAugmentDebug
    {
        [Header("팀원 증강 시스템 (씬 참조)")]
        [SerializeField] private AugmentManager manager;
        [SerializeField] private AugmentUI augmentUI;
        [Tooltip("팀원 AugmentManager가 요구하는 상태 보관소. 플레이어가 아닌 별도 오브젝트에 둔다 (팀원 AugmentState는 이동 속도/공격 스탯을 직접 덮어써서 우리 능력치 파이프라인과 충돌한다)")]
        [SerializeField] private AugmentState state;
        [Tooltip("AugmentUI.panel 과 같은 오브젝트. 선택 없이 꺼졌는지 확인하는 데 쓴다")]
        [SerializeField] private GameObject panelRoot;
        [Tooltip("스킬 증강이 실행되는 플레이어의 PlayerSkillController")]
        [SerializeField] private PlayerSkillController skills;
        [SerializeField] private List<AugmentSO> augments = new List<AugmentSO>();
        [SerializeField] private AugmentIntegrationSettings settings;
        [Tooltip("한 화면에 보여 줄 후보 수 (팀원 AugmentManager.choiceCount와 같게)")]
        [SerializeField, Min(1)] private int choiceCount = 3;

        private TeammateAugmentChooser chooser;
        private GuardedAugmentPresenter guarded;
        private readonly List<AugmentDefinition> definitions = new List<AugmentDefinition>();

        public IReadOnlyList<AugmentDefinition> AllAugments => definitions;
        public IReadOnlyList<string> ForcedIds => chooser != null ? chooser.ForcedIds : Array.Empty<string>();

        public void Bind(IAugmentEffects effects, IAugmentSelectionPresenter fallback)
        {
            if (manager == null || augmentUI == null || state == null || settings == null)
            {
                Debug.LogError("[Augment] TeammateAugmentPresenter 참조가 비어 있어 대체 보상만 씁니다. Tools > ShelfLife > Install Augments To Open Scene 을 실행하세요.", this);
                guarded = new GuardedAugmentPresenter(new NoChooser(), effects, fallback);
                return;
            }

            if (skills == null) skills = FindFirstObjectByType<PlayerSkillController>();
            var catalog = new TeammateAugmentCatalog(settings, skills);
            definitions.Clear();
            foreach (var augment in augments)
            {
                var definition = catalog.Get(augment);
                if (definition != null) definitions.Add(definition);
            }

            chooser = new TeammateAugmentChooser(manager, state, panelRoot, augments, catalog, effects, choiceCount);
            guarded = new GuardedAugmentPresenter(chooser, effects, fallback);

            // 선택 화면이 HUD와 흐름 화면(레벨업 배너 등) 위에 그려지게 한다
            var canvas = augmentUI.GetComponentInParent<Canvas>(true);
            if (canvas != null)
            {
                canvas.sortingOrder = settings.augmentCanvasSortingOrder;
                // 팀원 씬은 AugmentCanvas 루트를 꺼 둔 채로 저장돼 있다. 꺼져 있으면 AugmentUI.Awake(패널 숨김)가 돌지 않고 패널을 켜도 보이지 않는다
                if (!canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
            }
        }

        public void RequestSelection(int level, Action onComplete)
        {
            if (guarded == null)
            {
                Debug.LogError("[Augment] Bind되기 전에 선택이 요청되었습니다. 선택을 건너뛰고 재개합니다.", this);
                onComplete?.Invoke();
                return;
            }
            guarded.RequestSelection(level, onComplete);
        }

        public void Cancel() => guarded?.Cancel();

        // 팀원 화면은 완료 이벤트가 없어서 매 프레임 확인한다. Update는 timeScale 0에서도 돈다
        private void Update() => chooser?.Tick();

        public void ForceNextCandidates(IReadOnlyList<string> ids) => chooser?.ForceNext(ids);

        // Bind가 안 된 경우(참조 누락)에도 게임이 멈추지 않도록 항상 "후보 없음"으로 답하는 자리 채움
        private sealed class NoChooser : IAugmentChooser
        {
            public bool Prepare() => false;
            public AugmentDefinition FirstCandidate => null;
            public void Open(Action<AugmentDefinition> onResult) { }
            public void Close() { }
        }
    }
}
