using ExpiryGame.Combat;
using NUnit.Framework;
using UnityEngine;

namespace ExpiryGame.Tests
{
    // 넉백(거리 상한 / 감쇠 / 장애물 정지 / 경직 / 속도 제한 / 안전지대 분리)과 먹기(길게 누르기 / 취소 / 안내) 회귀 테스트
    public class KnockbackEatTests
    {
        private const float Dt = 1f / 60f;

        private KnockbackProfile hit;
        private KnockbackProfile safeZone;
        private EatSettings eat;
        private FoodBalanceConfig balance;

        [SetUp]
        public void SetUp()
        {
            hit = ScriptableObject.CreateInstance<KnockbackProfile>();
            safeZone = ScriptableObject.CreateInstance<KnockbackProfile>();
            KnockbackProfile.ApplySafeZoneDefaults(safeZone);
            eat = ScriptableObject.CreateInstance<EatSettings>();
            balance = ScriptableObject.CreateInstance<FoodBalanceConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(hit);
            Object.DestroyImmediate(safeZone);
            Object.DestroyImmediate(eat);
            Object.DestroyImmediate(balance);
        }

        // 끝날 때까지 돌려서 총 이동 거리와 걸린 시간, 프레임당 최대 속도를 돌려준다
        private static float Run(KnockbackMotion motion, System.Func<Vector2, float, float> clip, out float seconds, out float peakSpeed)
        {
            var moved = Vector2.zero;
            seconds = 0f;
            peakSpeed = 0f;
            while (motion.IsMoving && seconds < 10f)
            {
                var delta = motion.Step(Dt, clip);
                moved += delta;
                peakSpeed = Mathf.Max(peakSpeed, delta.magnitude / Dt);
                seconds += Dt;
            }
            return moved.magnitude;
        }

        // ---------- 거리 ----------

        [Test]
        public void DefaultHit_IsWeak_AndStrongestStaysWithinOneTile()
        {
            Assert.That(hit.DistanceFor(3f), Is.InRange(0.3f, 0.6f), "약한 타격");
            Assert.That(hit.DistanceFor(25f), Is.InRange(0.3f, 0.7f), "가장 센 타격도 약하게");
            Assert.LessOrEqual(hit.DistanceFor(100000f), 1f, "어떤 데미지여도 1타일 이내");
            Assert.AreEqual(0.1f, hit.duration, 1e-4f, "기본 지속시간 0.1초");
        }

        [Test]
        public void DistanceCap_HoldsEvenWhenTheProfileAsksForMore()
        {
            hit.baseDistance = 5f;
            hit.bonusDistanceAtMaxDamage = 5f;
            Assert.LessOrEqual(hit.DistanceFor(25f), hit.maxDistance);

            var motion = new KnockbackMotion();
            var applied = motion.Apply(Vector2.right, 50f, 0f, hit, 0f);
            Assert.LessOrEqual(applied, hit.maxDistance + 1e-4f);
            Assert.LessOrEqual(Run(motion, null, out _, out _), hit.maxDistance + 1e-3f);
        }

        [Test]
        public void Travel_MatchesRequestedDistance_InAboutTheProfileDuration()
        {
            var motion = new KnockbackMotion();
            motion.Apply(Vector2.right, 0.5f, 0f, hit, 0f);
            var moved = Run(motion, null, out var seconds, out _);

            Assert.AreEqual(0.5f, moved, 0.01f);
            Assert.That(seconds, Is.InRange(hit.duration - Dt, hit.duration + 2 * Dt));
        }

        [Test]
        public void EaseOut_MovesMostOfTheDistanceEarly()
        {
            var motion = new KnockbackMotion();
            motion.Apply(Vector2.right, 0.6f, 0f, hit, 0f);
            var half = Vector2.zero;
            for (var i = 0; i < 3; i++) half += motion.Step(Dt, null); // 약 절반 시간 (0.05s)
            Assert.Greater(half.magnitude, 0.6f * 0.6f, "절반 시간에 60% 넘게 이동 (감속 커브)");
        }

        // ---------- 연속 피격 감쇠 / 동시 속도 ----------

        [Test]
        public void RepeatedHits_InsideWindow_PushLessEachTime_AndResetAfterWindow()
        {
            var motion = new KnockbackMotion();
            var first = motion.Apply(Vector2.right, 0.6f, 0f, hit, 0f);
            var second = motion.Apply(Vector2.right, 0.6f, 0f, hit, 0.2f);
            var third = motion.Apply(Vector2.right, 0.6f, 0f, hit, 0.4f);
            Assert.Less(second, first);
            Assert.Less(third, second);
            Assert.GreaterOrEqual(third, 0.6f * hit.minComboMultiplier - 1e-4f);

            var later = motion.Apply(Vector2.right, 0.6f, 0f, hit, 0.4f + hit.comboWindowSeconds + 1f);
            Assert.AreEqual(first, later, 1e-4f, "윈도우가 지나면 다시 첫 타 배율");
        }

        [Test]
        public void SimultaneousHits_NeverAddUpPastTheCap()
        {
            var motion = new KnockbackMotion();
            hit.comboFalloff = 1f; // 감쇠 없이 쌓이는 최악의 경우
            for (var i = 0; i < 10; i++) motion.Apply(Vector2.right, 1f, 0f, hit, i * 0.01f);
            Assert.LessOrEqual(motion.RemainingDistance, hit.maxDistance + 1e-4f);
            Assert.LessOrEqual(Run(motion, null, out _, out _), hit.maxDistance + 1e-3f);
        }

        [Test]
        public void SpeedLimit_StretchesTheTime_InsteadOfExceedingMaxSpeed()
        {
            hit.maxSpeed = 5f;
            hit.maxDistance = 2f;
            var motion = new KnockbackMotion();
            motion.Apply(Vector2.right, 1f, 0f, hit, 0f);
            var moved = Run(motion, null, out var seconds, out var peak);

            Assert.AreEqual(1f, moved, 0.02f, "거리는 유지");
            Assert.LessOrEqual(peak, hit.maxSpeed * 1.15f, "순간 속도는 상한 이내 (프레임 이산화 오차 포함)");
            Assert.Greater(seconds, hit.duration, "시간이 늘어난다");
        }

        // ---------- 장애물 ----------

        [Test]
        public void Obstacle_StopsTheKnockbackThere()
        {
            var motion = new KnockbackMotion();
            motion.Apply(Vector2.right, 0.8f, 0f, hit, 0f);
            const float wall = 0.2f;
            var travelled = 0f;
            System.Func<Vector2, float, float> clip = (dir, want) => Mathf.Max(0f, Mathf.Min(want, wall - travelled));

            var steps = 0;
            while (motion.IsMoving && steps++ < 600) travelled += motion.Step(Dt, clip).magnitude;

            Assert.AreEqual(wall, travelled, 1e-3f, "벽 앞에서 멈춘다 (뚫고 지나가지 않는다)");
            Assert.IsTrue(motion.WasBlocked);
            Assert.IsFalse(motion.IsMoving);
            Assert.IsTrue(motion.IsStaggered, "막혀도 경직은 유지");
        }

        // ---------- 경직 / 저항 ----------

        [Test]
        public void Stagger_OutlastsTheShortMovement_ThenEnds()
        {
            var motion = new KnockbackMotion();
            motion.Apply(Vector2.right, 0.4f, 0f, hit, 0f);
            Run(motion, null, out var seconds, out _);
            Assert.IsFalse(motion.IsMoving);
            Assert.IsTrue(motion.IsStaggered, "움직임이 끝나도 경직은 남는다 (0.1초 이동 < 0.15초 경직)");
            Assert.That(hit.staggerSeconds, Is.InRange(0.1f, 0.2f));

            var left = hit.staggerSeconds - seconds;
            for (var t = 0f; t < left + 0.05f; t += Dt) motion.Step(Dt, null);
            Assert.IsFalse(motion.IsActive);
        }

        [Test]
        public void Resistance_ScalesDistance_AndFullResistanceNeverMovesOrStaggers()
        {
            var half = new KnockbackMotion().Apply(Vector2.right, 0.6f, 0.5f, hit, 0f);
            Assert.AreEqual(0.3f, half, 1e-4f);

            var immune = new KnockbackMotion();
            Assert.AreEqual(0f, immune.Apply(Vector2.right, 0.6f, 1f, hit, 0f));
            Assert.IsFalse(immune.IsActive);

            var none = new KnockbackMotion().Apply(Vector2.right, 0.6f, 0f, hit, 0f);
            Assert.AreEqual(0.6f, none, 1e-4f, "저항 값이 없으면 0");
        }

        // ---------- 모닥불 밀어내기는 타격과 별개 ----------

        [Test]
        public void SafeZoneRepel_IsSlowerAndGentlerThanAHit()
        {
            var a = new KnockbackMotion();
            a.Apply(Vector2.right, 1.5f, 0f, safeZone, 0f);
            var movedSafe = Run(a, null, out var safeSeconds, out var safePeak);

            var b = new KnockbackMotion();
            b.Apply(Vector2.right, 0.6f, 0f, hit, 0f);
            Run(b, null, out var hitSeconds, out var hitPeak);

            Assert.AreEqual(1.5f, movedSafe, 0.03f, "경계 밖으로 나갈 거리는 확보");
            Assert.Less(safePeak, hitPeak * 0.5f, "순간 속도가 훨씬 느리다");
            Assert.Greater(safeSeconds, hitSeconds * 4f, "천천히 이동");
            Assert.AreEqual(0f, safeZone.staggerSeconds, "밀어내기는 경직 없음");
            Assert.AreNotSame(KnockbackProfile.ResolveHit(null), KnockbackProfile.ResolveSafeZone(null));
        }

        [Test]
        public void ShippedAssets_Resolve_AndKeepTheWeakDefaults()
        {
            var shippedHit = KnockbackProfile.ResolveHit(null);
            Assert.IsNotNull(shippedHit);
            Assert.LessOrEqual(shippedHit.DistanceFor(100000f), 1f);
            Assert.IsNotNull(KnockbackProfile.ResolveSafeZone(null));
            Assert.AreEqual(0.4f, EatSettings.Resolve(null).holdSeconds, 1e-4f, "먹기 길게 누르기 기본 0.4초");
        }

        // ---------- 먹기 ----------

        [Test]
        public void Eat_FillsWhileHeld_AndCompletesAtHoldSeconds()
        {
            var timer = new EatHoldTimer();
            Assert.AreEqual(0.4f, eat.holdSeconds, 1e-4f);
            Assert.AreEqual(EatHoldEvent.Started, timer.Tick(Dt, true, true, eat.holdSeconds));

            var completed = false;
            var seconds = 0f;
            while (!completed && seconds < 2f)
            {
                completed = timer.Tick(Dt, true, true, eat.holdSeconds) == EatHoldEvent.Completed;
                seconds += Dt;
            }
            Assert.IsTrue(completed);
            Assert.That(seconds, Is.InRange(0.4f - 2 * Dt, 0.4f + 2 * Dt));
            Assert.IsFalse(timer.IsActive);
        }

        [Test]
        public void Eat_ReleasingEarly_CancelsImmediately_WithNoProgressLeft()
        {
            var timer = new EatHoldTimer();
            timer.Tick(Dt, true, true, eat.holdSeconds);
            timer.Tick(0.2f, true, true, eat.holdSeconds);
            Assert.Greater(timer.Progress01, 0.3f);

            Assert.AreEqual(EatHoldEvent.Canceled, timer.Tick(Dt, false, true, eat.holdSeconds));
            Assert.IsFalse(timer.IsActive);
            Assert.AreEqual(0f, timer.Progress01);
            Assert.AreEqual(EatHoldEvent.None, timer.Tick(Dt, false, true, eat.holdSeconds));
        }

        [Test]
        public void Eat_LosingTheFood_Cancels_AndNeedsAReleaseBeforeTheNextEat()
        {
            var timer = new EatHoldTimer();
            timer.Tick(Dt, true, true, eat.holdSeconds);
            Assert.AreEqual(EatHoldEvent.Canceled, timer.Tick(Dt, true, false, eat.holdSeconds), "대상이 사라지면 취소");

            var fresh = new EatHoldTimer();
            fresh.Tick(Dt, true, true, 0.1f);
            Assert.AreEqual(EatHoldEvent.Completed, fresh.Tick(0.2f, true, true, 0.1f));
            Assert.AreEqual(EatHoldEvent.None, fresh.Tick(Dt, true, true, 0.1f), "누른 채로는 다음 음식을 연달아 먹지 않는다");
            fresh.Tick(Dt, false, true, 0.1f);
            Assert.AreEqual(EatHoldEvent.Started, fresh.Tick(Dt, true, true, 0.1f), "놓았다 다시 누르면 시작");
        }

        [Test]
        public void Eat_ZeroHoldSeconds_EatsOnPress()
        {
            Assert.AreEqual(EatHoldEvent.Completed, new EatHoldTimer().Tick(Dt, true, true, 0f));
        }

        [Test]
        public void EatAdvisor_ExplainsFullHealthAndRottenFood()
        {
            var calculator = new FoodStatCalculator(balance);
            var data = ScriptableObject.CreateInstance<FoodData>();
            data.shelfLifeSeconds = 10f;
            var item = new FoodItem(data, calculator.Calculate(data.shelfLifeSeconds));

            Assert.AreEqual(EatWarning.FullHealth, EatAdvisor.Evaluate(item, 100f, 100f));
            Assert.AreEqual(EatWarning.None, EatAdvisor.Evaluate(item, 60f, 100f));
            Assert.AreEqual(eat.fullHealthNotice, EatAdvisor.NoticeFor(EatWarning.FullHealth, item, eat));

            item.Tick(20f);
            Assert.IsTrue(item.IsRotten);
            Assert.AreEqual(EatWarning.Rotten, EatAdvisor.Evaluate(item, 100f, 100f), "썩은 음식이 체력 가득보다 우선");
            Assert.IsNotEmpty(EatAdvisor.NoticeFor(EatWarning.Rotten, item, eat));
            Assert.AreEqual(EatWarning.None, EatAdvisor.Evaluate(null, 100f, 100f));
            Object.DestroyImmediate(data);
        }
    }
}
