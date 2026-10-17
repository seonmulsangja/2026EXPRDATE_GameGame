using System;
using System.Collections.Generic;

namespace ExpiryGame.Flow
{
    // 플레이어 능력치 파이프라인의 중심: 기본값 + 수정자 → 최종값 (순수 클래스).
    // 최종값 = (기본값 + 더하기 수정자 합) × (1 + 곱하기 수정자 합). 수정자는 (source, key, op)당 하나만 유지한다.
    public sealed class PlayerStatSheet : IPlayerStats, IStatModifierRegistry
    {
        private readonly Dictionary<StatKey, float> baseValues = new Dictionary<StatKey, float>();
        private readonly List<IStatModifier> modifiers = new List<IStatModifier>();

        public event Action Changed;

        public int ModifierCount => modifiers.Count;

        public void SetBase(StatKey key, float value)
        {
            baseValues[key] = value;
            Changed?.Invoke();
        }

        public float Get(StatKey key)
        {
            baseValues.TryGetValue(key, out var value);
            var add = 0f;
            var multiply = 0f;
            foreach (var modifier in modifiers)
            {
                if (modifier.Key != key) continue;
                if (modifier.Op == StatOp.Add) add += modifier.Value;
                else multiply += modifier.Value;
            }
            return (value + add) * (1f + multiply);
        }

        public void Set(IStatModifier modifier)
        {
            for (var i = 0; i < modifiers.Count; i++)
            {
                var existing = modifiers[i];
                if (existing.Source != modifier.Source || existing.Key != modifier.Key || existing.Op != modifier.Op) continue;
                modifiers[i] = modifier;
                Changed?.Invoke();
                return;
            }
            modifiers.Add(modifier);
            Changed?.Invoke();
        }

        public bool RemoveSource(string source)
        {
            var removed = modifiers.RemoveAll(m => m.Source == source) > 0;
            if (removed) Changed?.Invoke();
            return removed;
        }

        public void Clear()
        {
            if (modifiers.Count == 0) return;
            modifiers.Clear();
            Changed?.Invoke();
        }
    }

    public sealed class StatModifier : IStatModifier
    {
        public string Source { get; }
        public StatKey Key { get; }
        public StatOp Op { get; }
        public float Value { get; }

        public StatModifier(string source, StatKey key, StatOp op, float value)
        {
            Source = source;
            Key = key;
            Op = op;
            Value = value;
        }
    }
}
