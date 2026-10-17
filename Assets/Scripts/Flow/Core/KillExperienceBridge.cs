using System;
using System.Collections.Generic;

namespace ExpiryGame.Flow
{
    // 적 처치 이벤트를 경험치로 옮긴다. StatsTracker와 같은 방식으로 등록소를 보고, 죽인 적이 IExperienceReward면 그 양을 준다.
    // 경험치를 얼마 주는지는 적(어댑터)이 정하고, 이 클래스는 전달만 한다.
    public sealed class KillExperienceBridge : IDisposable
    {
        private readonly IExperienceSink sink;
        private readonly HashSet<IEnemyActor> subscribed = new HashSet<IEnemyActor>();

        public KillExperienceBridge(IExperienceSink sink)
        {
            this.sink = sink;
            EnemyRegistry.Registered += Subscribe;
            EnemyRegistry.Unregistered += Unsubscribe;
            foreach (var enemy in EnemyRegistry.Enemies) Subscribe(enemy);
        }

        public void Dispose()
        {
            EnemyRegistry.Registered -= Subscribe;
            EnemyRegistry.Unregistered -= Unsubscribe;
            foreach (var enemy in subscribed) enemy.Killed -= OnKilled;
            subscribed.Clear();
        }

        private void Subscribe(IEnemyActor enemy)
        {
            if (subscribed.Add(enemy)) enemy.Killed += OnKilled;
        }

        private void Unsubscribe(IEnemyActor enemy)
        {
            if (subscribed.Remove(enemy)) enemy.Killed -= OnKilled;
        }

        private void OnKilled(IEnemyActor enemy)
        {
            if (enemy is IExperienceReward reward) sink.AddExperience(reward.ExperienceReward);
        }
    }
}
