using System;
using System.Collections.Generic;

namespace ExpiryGame.Flow
{
    // 증강 하나가 바꾸는 능력치. 레벨마다 perLevel만큼씩 쌓인다 (레벨 3이면 perLevel × 3)
    public readonly struct StatEffect
    {
        public readonly StatKey Key;
        public readonly StatOp Op;
        public readonly float PerLevel;

        public StatEffect(StatKey key, StatOp op, float perLevel)
        {
            Key = key;
            Op = op;
            PerLevel = perLevel;
        }
    }

    // 팀원의 증강 데이터(AugmentSO)를 우리 흐름이 이해하는 말로 옮긴 것. Flow는 팀원 타입을 모르고 이것만 본다.
    public sealed class AugmentDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public int MaxLevel { get; }
        public IReadOnlyList<StatEffect> Stats { get; }
        // 능력치로 표현되지 않는 효과(스킬 부여 등). 새 레벨을 받아 적용한다. 없으면 null
        public Action<int> OnLevel { get; }

        public AugmentDefinition(string id, string name, string description, int maxLevel,
            IReadOnlyList<StatEffect> stats, Action<int> onLevel = null)
        {
            Id = id;
            Name = name;
            Description = description;
            MaxLevel = Math.Max(1, maxLevel);
            Stats = stats ?? Array.Empty<StatEffect>();
            OnLevel = onLevel;
        }
    }

    // 고른 증강 기록 한 줄 (결과 화면 표시용)
    public readonly struct AugmentRecord
    {
        public readonly string Id;
        public readonly string Name;
        public readonly int Level;

        public AugmentRecord(string id, string name, int level)
        {
            Id = id;
            Name = name;
            Level = level;
        }
    }
}
