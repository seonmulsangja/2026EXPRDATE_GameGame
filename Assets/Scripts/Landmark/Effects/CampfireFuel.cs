using System;
using UnityEngine;
using ExpiryGame.World;

namespace ExpiryGame.Landmark
{
    // 모닥불의 연료 "상태"만 담당한다 (줄고/차고/꺼지고/다시 켜지는 것). 빛/안전지대/회복/외형/게이지는 이 컴포넌트의
    // Ratio/Stage/Strength를 읽어서 각자 반응한다. 상태는 CampfireStateStore(랜드마크 ID 키)에 보관돼 언로드/로드에도 이어진다.
    // CampfireFuelProfile이 비어 있으면 연료 없이 항상 켜진 모닥불(이전 동작)처럼 보인다.
    [RequireComponent(typeof(LandmarkZone))]
    public class CampfireFuel : MonoBehaviour, ILandmarkPart
    {
        private LandmarkZone zone;
        private LandmarkContext context;
        private CampfireFuelProfile profile;
        private CampfireFuelModel model;
        private CampfireStage lastStage;

        public event Action<CampfireStage, CampfireStage> StageChanged;

        public bool HasProfile => model != null;
        public CampfireFuelProfile Profile => profile;
        public bool IsLit => model == null || model.Lit;
        public float Ratio => model != null ? model.Ratio : 1f;
        public float Fuel => model != null ? model.Fuel : 0f;
        public CampfireStage Stage => model != null ? model.Stage : CampfireStage.Full;

        // 켜져 있으면 1(가득) ~ atEmpty(바닥)로 보간한 배율, 꺼져 있으면 0. 연료가 없는(프로필 없는) 모닥불은 항상 1
        public float Strength(float atEmpty)
        {
            return model != null ? model.Strength(atEmpty) : 1f;
        }

        public bool CanAfford(float amount)
        {
            return model == null || model.CanAfford(amount);
        }

        public bool TryConsume(float amount)
        {
            if (model == null) return true;
            var ok = model.TryConsume(amount);
            if (ok) AfterChange();
            return ok;
        }

        private void Awake()
        {
            zone = GetComponent<LandmarkZone>();
        }

        public void Bind(LandmarkContext ctx)
        {
            context = ctx;
            profile = ctx.Data.fuelProfile;
            model = null;
            if (profile == null) return;

            CampfireState saved = null;
            if (ctx.Store != null && ctx.Id != null) ctx.Store.TryGet(ctx.Id, out saved);
            model = new CampfireFuelModel(profile, saved);
            // 언로드돼 있던 동안(= 플레이어가 멀리 있던 동안) 회복된 만큼 따라잡는다
            if (saved != null) model.CatchUpAway(Mathf.Max(0f, Time.time - saved.SavedAt));
            lastStage = model.Stage;
            Save();
        }

        private void Update()
        {
            if (model == null) return;
            var night = DayNightState.Instance != null ? DayNightState.Instance.Current.Night01 : 0f;
            Advance(Time.deltaTime, zone != null && zone.IsInside, night);
        }

        // 한 틱 진행 (Update가 부르고, 테스트는 시간을 마음대로 넘겨 부른다)
        public void Advance(float dt, bool playerInside, float night01)
        {
            if (model == null) return;
            model.Tick(dt, playerInside, night01);
            AfterChange();
        }

        private void AfterChange()
        {
            var stage = model.Stage;
            if (stage == lastStage) return;
            var previous = lastStage;
            lastStage = stage;
            Save();
            StageChanged?.Invoke(previous, stage);
            context?.Events?.CampfireStageChanged(new CampfireStageInfo(transform.position, previous, stage));
        }

        // 현재 상태를 저장소에 기록한다 (언로드 직전/단계 전환 때)
        public void Save()
        {
            if (model == null || context == null || context.Store == null || context.Id == null) return;
            context.Store.Save(context.Id, model.Snapshot(Time.time));
        }

        private void OnDisable()
        {
            Save();
        }
    }
}
