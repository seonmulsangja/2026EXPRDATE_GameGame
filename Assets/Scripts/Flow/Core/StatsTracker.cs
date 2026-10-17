using System;
using System.Collections.Generic;
using ExpiryGame.Drop;

namespace ExpiryGame.Flow
{
    // 기존 이벤트(적 처치 / 먹기)를 구독해 한 판의 통계를 모은다. 다른 시스템은 건드리지 않는다.
    public class StatsTracker : IStatsSource, IDisposable
    {
        private readonly IGameClock clock;
        private readonly FoodConsumer consumer;
        private readonly HashSet<IEnemyActor> subscribed = new HashSet<IEnemyActor>();

        private int kills;
        private int eaten;
        private int rottenEaten;
        private float playSeconds;
        private int frozenDays = -1;

        public StatsTracker(IGameClock clock, FoodConsumer consumer)
        {
            this.clock = clock;
            this.consumer = consumer;

            if (consumer != null) consumer.FoodEaten += OnFoodEaten;
            EnemyRegistry.Registered += Subscribe;
            EnemyRegistry.Unregistered += Unsubscribe;
            foreach (var enemy in EnemyRegistry.Enemies) Subscribe(enemy);
        }

        // 플레이 중에만 시간 누적 (Playing 상태일 때 매 프레임 호출)
        public void AddPlayTime(float seconds)
        {
            playSeconds += seconds;
        }

        // 게임 오버/클리어 시점의 일수를 고정한다
        public void Freeze()
        {
            frozenDays = CurrentDays();
        }

        public GameStats Snapshot()
        {
            return new GameStats
            {
                DaysSurvived = frozenDays >= 0 ? frozenDays : CurrentDays(),
                Kills = kills,
                FoodEaten = eaten,
                RottenEaten = rottenEaten,
                PlaySeconds = playSeconds,
            };
        }

        public void Dispose()
        {
            if (consumer != null) consumer.FoodEaten -= OnFoodEaten;
            EnemyRegistry.Registered -= Subscribe;
            EnemyRegistry.Unregistered -= Unsubscribe;
            foreach (var enemy in subscribed) enemy.Killed -= OnKilled;
            subscribed.Clear();
        }

        private int CurrentDays() => Math.Max(0, clock.DayCount - 1);

        private void Subscribe(IEnemyActor enemy)
        {
            if (subscribed.Add(enemy)) enemy.Killed += OnKilled;
        }

        private void Unsubscribe(IEnemyActor enemy)
        {
            if (subscribed.Remove(enemy)) enemy.Killed -= OnKilled;
        }

        private void OnKilled(IEnemyActor enemy) => kills++;

        private void OnFoodEaten(FoodItem item)
        {
            eaten++;
            if (item.IsRotten) rottenEaten++;
        }
    }
}
