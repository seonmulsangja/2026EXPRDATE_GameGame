using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExpiryGame.Flow
{
    // 고른 증강을 능력치 파이프라인에 연결한다.
    //  - 레벨(중첩)을 센다. 레벨 N이면 효과는 perLevel × N — 같은 증강을 또 고르면 수정자 값이 올라가고, 최대 레벨이면 거절한다
    //  - 능력치 효과는 수정자(source = "augment:{id}")로 등록하고, 스킬 같은 특수 효과는 정의의 OnLevel로 넘긴다
    //  - 고른 순서대로 기록을 남긴다 (결과 화면). Reset은 수정자/레벨/기록을 모두 비운다 (다시하기)
    public sealed class AugmentEffectService : IAugmentEffects, IAugmentHistory
    {
        public const string SourcePrefix = "augment:";

        private readonly IStatModifierRegistry registry;
        private readonly Dictionary<string, int> levels = new Dictionary<string, int>();
        private readonly List<AugmentDefinition> order = new List<AugmentDefinition>();

        public event Action<AugmentDefinition, int> Applied;

        public AugmentEffectService(IStatModifierRegistry registry)
        {
            this.registry = registry;
        }

        public int LevelOf(string id) => levels.TryGetValue(id, out var level) ? level : 0;

        public bool CanApply(AugmentDefinition definition) => definition != null && LevelOf(definition.Id) < definition.MaxLevel;

        public bool Apply(AugmentDefinition definition)
        {
            if (!CanApply(definition)) return false;

            var level = LevelOf(definition.Id) + 1;
            if (level == 1) order.Add(definition);
            levels[definition.Id] = level;
            Register(definition, level);

            if (definition.OnLevel != null)
            {
                // 특수 효과가 실패해도 게임은 계속된다 (능력치 효과와 기록은 이미 반영됨)
                try { definition.OnLevel(level); }
                catch (Exception e) { Debug.LogException(e); }
            }
            Applied?.Invoke(definition, level);
            return true;
        }

        public bool Revoke(AugmentDefinition definition)
        {
            if (definition == null) return false;
            var level = LevelOf(definition.Id);
            if (level <= 0) return false;

            level--;
            if (level == 0)
            {
                levels.Remove(definition.Id);
                order.RemoveAll(d => d.Id == definition.Id);
                registry.RemoveSource(SourcePrefix + definition.Id);
            }
            else
            {
                levels[definition.Id] = level;
                Register(definition, level);
            }
            return true;
        }

        public void Reset()
        {
            foreach (var definition in order) registry.RemoveSource(SourcePrefix + definition.Id);
            levels.Clear();
            order.Clear();
        }

        public IReadOnlyList<AugmentRecord> Records
        {
            get
            {
                var records = new List<AugmentRecord>(order.Count);
                foreach (var definition in order) records.Add(new AugmentRecord(definition.Id, definition.Name, LevelOf(definition.Id)));
                return records;
            }
        }

        private void Register(AugmentDefinition definition, int level)
        {
            var source = SourcePrefix + definition.Id;
            // 한 정의 안에서 같은 (키, 연산)이 여러 번 나오면 합쳐서 하나로 등록한다 (수정자는 (source, key, op)당 하나)
            var merged = new Dictionary<(StatKey, StatOp), float>();
            foreach (var effect in definition.Stats)
            {
                var key = (effect.Key, effect.Op);
                merged.TryGetValue(key, out var sum);
                merged[key] = sum + effect.PerLevel;
            }
            foreach (var pair in merged)
                registry.Set(new StatModifier(source, pair.Key.Item1, pair.Key.Item2, pair.Value * level));
        }
    }
}
