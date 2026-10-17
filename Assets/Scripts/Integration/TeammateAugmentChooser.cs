using System;
using System.Collections.Generic;
using System.Reflection;
using ExpiryGame.Flow;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ExpiryGame.Integration
{
    // 팀원 선택 화면(AugmentManager + AugmentUI)을 IAugmentChooser로 감싼다. 팀원 파일은 수정하지 않는다.
    //
    // 팀원 쪽에 "선택 완료" 이벤트와 "후보 지정" 함수가 없어서 두 가지를 우회한다 (docs/meetings.md D79):
    //  1) 후보 뽑기는 우리가 한다. 뽑은 후보만 팀원 매니저의 allAugments(private)에 넣고 OpenAugmentSelection()을 부르면
    //     팀원 화면이 정확히 그 후보들을 보여 준다. (리플렉션은 ManagerAccess 한 곳에 모았다 — 필드 이름이 바뀌면 Available=false로
    //     에러 로그를 남기고 대체 보상으로 넘어가므로 게임은 멈추지 않는다)
    //  2) 완료는 매 프레임(Tick) 확인한다: 후보 중 하나의 GetAugmentLevel이 올랐으면 그 증강을 고른 것,
    //     화면(panelRoot)이 후보 선택 없이 꺼졌으면 "선택 없이 닫힘"이다. Tick은 Update에서 부르므로 timeScale 0에서도 돈다.
    // 후보 규칙(팀원 규칙 그대로): 최대 레벨에 닿은 증강 제외, 스킬 증강은 스킬 최대 레벨도 확인, 한 화면에 최대 choiceCount개, 중복 없음.
    public sealed class TeammateAugmentChooser : IAugmentChooser
    {
        private readonly AugmentManager manager;
        private readonly AugmentState state;
        private readonly GameObject panelRoot;
        private readonly IReadOnlyList<AugmentSO> all;
        private readonly TeammateAugmentCatalog catalog;
        private readonly IAugmentEffects effects;
        private readonly int choiceCount;
        private readonly ManagerAccess access;

        private readonly List<AugmentSO> picks = new List<AugmentSO>();
        private readonly Dictionary<AugmentSO, int> levelsAtOpen = new Dictionary<AugmentSO, int>();
        private Action<AugmentDefinition> onResult;
        private IReadOnlyList<string> forced = Array.Empty<string>();

        public TeammateAugmentChooser(AugmentManager manager, AugmentState state, GameObject panelRoot, IReadOnlyList<AugmentSO> all,
            TeammateAugmentCatalog catalog, IAugmentEffects effects, int choiceCount)
        {
            this.manager = manager;
            this.state = state;
            this.panelRoot = panelRoot;
            this.all = all;
            this.catalog = catalog;
            this.effects = effects;
            this.choiceCount = Mathf.Max(1, choiceCount);
            access = new ManagerAccess(manager);
        }

        public bool IsOpen => onResult != null;
        public bool Available => access.Available;

        public AugmentDefinition FirstCandidate => picks.Count > 0 ? catalog.Get(picks[0]) : null;

        public IReadOnlyList<string> ForcedIds => forced;

        public void ForceNext(IReadOnlyList<string> ids) => forced = ids ?? Array.Empty<string>();

        // 지금 뽑을 수 있는 증강 (팀원 규칙 + 우리 기록의 최대 중첩)
        public List<AugmentSO> Eligible()
        {
            var list = new List<AugmentSO>();
            foreach (var augment in all)
            {
                var definition = catalog.Get(augment);
                if (definition == null || !effects.CanApply(definition) || list.Contains(augment)) continue;
                if (manager.GetAugmentLevel(augment) >= Mathf.Max(1, augment.maxLevel)) continue;
                if (augment.augmentType == AugmentType.Skill && state.GetSkillLevel(augment.skill) >= augment.skill.maxLevel) continue;
                list.Add(augment);
            }
            return list;
        }

        public bool Prepare()
        {
            picks.Clear();
            if (!access.Available) return false;

            var eligible = Eligible();
            if (forced.Count > 0)
            {
                var wanted = forced;
                forced = Array.Empty<string>(); // 한 번 쓰면 풀린다
                foreach (var id in wanted)
                {
                    var found = eligible.Find(a => a.name == id);
                    if (found != null && !picks.Contains(found) && picks.Count < choiceCount) picks.Add(found);
                    else if (found == null) Debug.LogWarning($"[Augment] 강제 지정한 '{id}' 는 지금 뽑을 수 없어(최대 중첩 등) 건너뜁니다.");
                }
                return picks.Count > 0;
            }

            picks.AddRange(CandidateDrawer.Draw(eligible, choiceCount, Random.Range));
            return picks.Count > 0;
        }

        public void Open(Action<AugmentDefinition> result)
        {
            if (!access.Available) throw new InvalidOperationException("팀원 AugmentManager 구조가 바뀌어 선택 화면을 열 수 없습니다.");

            levelsAtOpen.Clear();
            foreach (var augment in picks) levelsAtOpen[augment] = manager.GetAugmentLevel(augment);

            access.ResetSelecting();
            access.SetPool(picks);
            manager.OpenAugmentSelection();
            if (!access.IsSelecting) throw new InvalidOperationException("팀원 AugmentManager가 선택 화면을 열지 않았습니다.");

            onResult = result;
        }

        // 매 프레임 (timeScale 0에서도 도는 Update에서). 열려 있는 동안 선택/닫힘을 확인한다
        public void Tick()
        {
            if (onResult == null) return;

            foreach (var augment in picks)
            {
                if (manager.GetAugmentLevel(augment) <= levelsAtOpen[augment]) continue;
                Finish(catalog.Get(augment));
                return;
            }

            if (panelRoot != null && !panelRoot.activeInHierarchy) Finish(null);
        }

        public void Close()
        {
            onResult = null;
            if (panelRoot != null && panelRoot.activeSelf) panelRoot.SetActive(false);
            access.ResetSelecting();
            access.RestorePool(all);
        }

        private void Finish(AugmentDefinition chosen)
        {
            var callback = onResult;
            onResult = null;
            access.ResetSelecting();
            access.RestorePool(all);
            callback?.Invoke(chosen);
        }

        // 팀원 AugmentManager의 private 멤버 접근. 리플렉션은 여기에만 둔다.
        private sealed class ManagerAccess
        {
            private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
            private readonly AugmentManager manager;
            private readonly FieldInfo pool;
            private readonly FieldInfo selecting;
            private readonly FieldInfo choices;

            public bool Available { get; }

            public ManagerAccess(AugmentManager manager)
            {
                this.manager = manager;
                var type = typeof(AugmentManager);
                pool = type.GetField("allAugments", Flags);
                selecting = type.GetField("_isSelecting", Flags);
                choices = type.GetField("_currentChoices", Flags);
                Available = manager != null && pool != null && selecting != null && choices != null
                            && pool.FieldType == typeof(List<AugmentSO>) && selecting.FieldType == typeof(bool)
                            && choices.FieldType == typeof(HashSet<AugmentSO>);
                if (!Available)
                    Debug.LogError("[Augment] 팀원 AugmentManager의 allAugments/_isSelecting/_currentChoices 를 찾지 못했습니다(필드 이름이 바뀌었나요?). " +
                                   "증강 선택 대신 대체 보상으로 진행합니다. docs/connect_todo.md 참고.");
            }

            public bool IsSelecting => Available && (bool)selecting.GetValue(manager);

            public void SetPool(IReadOnlyList<AugmentSO> list)
            {
                if (!Available) return;
                var target = (List<AugmentSO>)pool.GetValue(manager);
                if (target == null) { target = new List<AugmentSO>(); pool.SetValue(manager, target); }
                target.Clear();
                target.AddRange(list);
            }

            public void RestorePool(IReadOnlyList<AugmentSO> list) => SetPool(list);

            // 화면이 중간에 꺼져 팀원 매니저가 "선택 중" 상태로 남으면 다음 OpenAugmentSelection이 영원히 무시되므로 풀어 준다
            public void ResetSelecting()
            {
                if (!Available) return;
                selecting.SetValue(manager, false);
                (choices.GetValue(manager) as HashSet<AugmentSO>)?.Clear();
            }
        }
    }
}
