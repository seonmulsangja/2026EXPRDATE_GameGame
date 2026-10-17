using UnityEngine;

namespace ExpiryGame.Flow
{
    // 디버그 메뉴가 실시간으로 건드리는 임시 값. DifficultyProfile 에셋은 건드리지 않고 이 값만 바꾼다.
    // 마음에 들면 BakeInto로 프로필에 굽는다 (에디터 전용 저장).
    public sealed class DifficultyTuning
    {
        public const int NoOverride = -1;

        public float SpeedScale { get; set; } = 1f;
        public float DamageScale { get; set; } = 1f;
        public float SpawnScale { get; set; } = 1f;
        // 0 미만이면 프로필 값을 쓴다
        public int MaxAliveOverride { get; set; } = NoOverride;
        public float InvulnerableOverride { get; set; } = -1f;

        public bool HasChanges =>
            !Mathf.Approximately(SpeedScale, 1f) || !Mathf.Approximately(DamageScale, 1f) || !Mathf.Approximately(SpawnScale, 1f)
            || MaxAliveOverride >= 0 || InvulnerableOverride >= 0f;

        public void Reset()
        {
            SpeedScale = 1f;
            DamageScale = 1f;
            SpawnScale = 1f;
            MaxAliveOverride = NoOverride;
            InvulnerableOverride = -1f;
        }

        // 현재 값을 프로필에 반영하고 임시 값은 초기화한다. currentDayIndex: 최대 동시 적 수 override를 어느 일차 기준으로 굽는지
        public void BakeInto(DifficultyProfile profile, int currentDayIndex)
        {
            if (profile == null) return;

            if (!Mathf.Approximately(SpeedScale, 1f))
            {
                // 최종 속도 = min(비율 x 종류, 상한) x 배율 이므로 세 값을 같이 키우면 같은 결과가 된다
                DifficultyProfile.ScaleCurve(profile.speedRatio, SpeedScale);
                DifficultyProfile.ScaleCurve(profile.speedCapRatio, SpeedScale);
                profile.fastSpeedRatio *= SpeedScale;
            }
            if (!Mathf.Approximately(DamageScale, 1f)) DifficultyProfile.ScaleCurve(profile.damage, DamageScale);
            if (!Mathf.Approximately(SpawnScale, 1f)) DifficultyProfile.ScaleCurve(profile.spawn, SpawnScale);
            if (MaxAliveOverride >= 0)
            {
                var now = profile.maxAlive.Evaluate(currentDayIndex);
                if (now > 0.01f) DifficultyProfile.ScaleCurve(profile.maxAlive, MaxAliveOverride / now);
            }
            if (InvulnerableOverride >= 0f) profile.playerInvulnerableSeconds = InvulnerableOverride;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(profile);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
            Reset();
        }
    }
}
