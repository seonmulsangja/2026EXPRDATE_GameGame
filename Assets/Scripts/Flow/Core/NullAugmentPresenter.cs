using System;

namespace ExpiryGame.Flow
{
    // 팀원의 증강 선택 화면이 붙기 전까지 쓰는 임시 구현: "레벨 업!" 배너 + 체력 소량 회복 후 자동 재개.
    // 팀원 화면은 IAugmentSelectionPresenter를 구현해 GameFlowRoot의 augmentPresenter 슬롯에 끼우면 이것 대신 쓰인다.
    public sealed class NullAugmentPresenter : IAugmentSelectionPresenter
    {
        private readonly Health health;
        private readonly LevelUpBannerView banner;
        private readonly FlowSettings settings;

        public NullAugmentPresenter(Health health, LevelUpBannerView banner, FlowSettings settings)
        {
            this.health = health;
            this.banner = banner;
            this.settings = settings;
        }

        public void RequestSelection(int level, Action onComplete)
        {
            if (health != null && !health.IsDead && settings.levelUpHealRatio > 0f)
                health.Heal(health.Max * settings.levelUpHealRatio);

            if (banner == null)
            {
                onComplete?.Invoke();
                return;
            }
            banner.Play(level, settings.levelUpBannerSeconds, onComplete);
        }

        public void Cancel()
        {
            if (banner != null) banner.Cancel();
        }
    }
}
