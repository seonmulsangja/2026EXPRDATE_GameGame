using System.Collections.Generic;
using UnityEngine;

namespace ExpiryGame.Landmark
{
    public enum CampfireStage
    {
        Full,
        Weak,
        Embers,
        Out
    }

    // 청크가 언로드돼도 남겨 두는 모닥불 하나의 상태
    public sealed class CampfireState
    {
        public float Fuel;
        public bool Lit;
        // 저장 시각 (Time.time). 다시 로드될 때 그동안 회복된 만큼 따라잡는다
        public float SavedAt;
    }

    // 연료 규칙만 담당하는 순수 클래스 (씬/시간/입력을 모른다). 수치는 CampfireFuelProfile.
    public sealed class CampfireFuelModel
    {
        private readonly CampfireFuelProfile profile;

        public CampfireFuelModel(CampfireFuelProfile profile)
        {
            this.profile = profile;
            Fuel = profile.maxFuel;
            Lit = true;
        }

        public CampfireFuelModel(CampfireFuelProfile profile, CampfireState saved) : this(profile)
        {
            if (saved == null) return;
            Fuel = Mathf.Clamp(saved.Fuel, 0f, profile.maxFuel);
            Lit = saved.Lit;
        }

        public float Fuel { get; private set; }
        public bool Lit { get; private set; }
        public float Ratio => profile.Ratio(Fuel);
        public CampfireStage Stage => profile.StageFor(Ratio, Lit);

        // 켜져 있을 때 비율에 따라 1(가득) ~ atEmpty(바닥)로 보간한 배율, 꺼져 있으면 0
        public float Strength(float atEmpty)
        {
            return Lit ? Mathf.Lerp(atEmpty, 1f, Ratio) : 0f;
        }

        // 반경 안이면 줄고(밤엔 더 빨리), 밖이면 조금씩 회복. 0이 되면 꺼지고, 재점화 임계값 이상으로 차야 다시 켜진다.
        public void Tick(float dt, bool playerInside, float night01)
        {
            if (dt <= 0f) return;
            if (playerInside)
            {
                var drain = profile.drainPerSecondInside * Mathf.Lerp(1f, profile.nightDrainMultiplier, Mathf.Clamp01(night01));
                Fuel = Mathf.Max(0f, Fuel - drain * dt);
            }
            else
            {
                Fuel = Mathf.Min(profile.maxFuel, Fuel + profile.regenPerSecondOutside * dt);
            }
            UpdateLit();
        }

        // 플레이어가 멀리 있던 시간만큼 회복을 따라잡는다 (언로드 기간)
        public void CatchUpAway(float seconds)
        {
            Tick(seconds, false, 0f);
        }

        public bool CanAfford(float amount)
        {
            return Lit && Fuel >= amount;
        }

        public bool TryConsume(float amount)
        {
            if (!CanAfford(amount)) return false;
            Fuel = Mathf.Max(0f, Fuel - amount);
            UpdateLit();
            return true;
        }

        public CampfireState Snapshot(float now)
        {
            return new CampfireState { Fuel = Fuel, Lit = Lit, SavedAt = now };
        }

        private void UpdateLit()
        {
            if (Fuel <= 0f) Lit = false;
            else if (!Lit && Ratio >= profile.relightThreshold01) Lit = true;
        }
    }

    // 랜드마크 ID로 키를 잡는 상태 저장소. 청크가 언로드/로드돼도 남고, LandmarkSpawner가 씬마다 새로 만들므로 씬 리로드(다시하기) 때 초기화된다.
    public sealed class CampfireStateStore
    {
        private readonly Dictionary<string, CampfireState> states = new Dictionary<string, CampfireState>();

        // 청크 생성 때 "아직 로드되지 않은 모닥불"의 현재 상태를 물을 수 있도록 현재 저장소를 노출 (SafeZoneRegistry와 같은 방식)
        public static CampfireStateStore Active { get; set; }

        public int Count => states.Count;

        public bool TryGet(string id, out CampfireState state)
        {
            return states.TryGetValue(id, out state);
        }

        public void Save(string id, CampfireState state)
        {
            states[id] = state;
        }

        public void Clear()
        {
            states.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Active = null;
        }
    }

    // 랜드마크 종류+위치로 만든 안정적인 ID (같은 seed면 로드/언로드와 무관하게 항상 같다)
    public static class LandmarkId
    {
        public static string Of(LandmarkData data, Vector2 position)
        {
            var kind = data != null ? data.kind.ToString() : "None";
            return $"{kind}@{Mathf.RoundToInt(position.x * 100f)},{Mathf.RoundToInt(position.y * 100f)}";
        }
    }
}
