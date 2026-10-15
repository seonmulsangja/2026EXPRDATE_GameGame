using NUnit.Framework;
using UnityEngine;

namespace ExpiryGame.Tests
{
    public class FoodTests
    {
        private FoodBalanceConfig balance;
        private FoodStatCalculator calculator;

        [SetUp]
        public void SetUp()
        {
            balance = ScriptableObject.CreateInstance<FoodBalanceConfig>();
            calculator = new FoodStatCalculator(balance);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(balance);
        }

        [Test]
        public void ShortShelfLife_HealsAndHitsHarder_ButBreaksSooner()
        {
            var fresh = calculator.Calculate(balance.shortestShelfLifeSeconds);
            var durable = calculator.Calculate(balance.longestShelfLifeSeconds);

            Assert.Greater(fresh.Heal, durable.Heal);
            Assert.Greater(fresh.Damage, durable.Damage);
            Assert.Less(fresh.MaxDurability, durable.MaxDurability);
        }

        [Test]
        public void ShelfLifeOutsideRange_IsClamped()
        {
            var below = calculator.Calculate(0.001f);
            var shortest = calculator.Calculate(balance.shortestShelfLifeSeconds);
            Assert.AreEqual(shortest.Heal, below.Heal, 1e-4f);
        }

        [Test]
        public void MidShelfLife_InterpolatesLinearly()
        {
            var mid = (balance.shortestShelfLifeSeconds + balance.longestShelfLifeSeconds) * 0.5f;
            var expected = (balance.heal.atLongestShelfLife + balance.heal.atShortestShelfLife) * 0.5f;
            Assert.AreEqual(expected, calculator.Calculate(mid).Heal, 1e-3f);
        }

        [Test]
        public void FoodItem_RotsAtZero_AndSwitchesToRottenStats()
        {
            var data = ScriptableObject.CreateInstance<FoodData>();
            data.shelfLifeSeconds = 10f;
            var item = new FoodItem(data, calculator.Calculate(data.shelfLifeSeconds));
            var rotted = 0;
            item.Rotted += _ => rotted++;

            item.Tick(9f);
            Assert.IsFalse(item.IsRotten);
            Assert.AreEqual(item.Stats.Heal, item.CurrentHeal);

            item.Tick(5f);
            item.Tick(5f);
            Assert.IsTrue(item.IsRotten);
            Assert.AreEqual(1, rotted);
            Assert.AreEqual(item.Stats.RottenHeal, item.CurrentHeal);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void FoodItem_BreaksWhenDurabilityRunsOut()
        {
            var data = ScriptableObject.CreateInstance<FoodData>();
            var item = new FoodItem(data, calculator.Calculate(data.shelfLifeSeconds));
            var broken = false;
            item.Broken += _ => broken = true;

            for (var i = 0; i < item.Stats.MaxDurability; i++) item.UseDurability();

            Assert.IsTrue(broken);
            Assert.IsTrue(item.IsBroken);
            Object.DestroyImmediate(data);
        }
    }
}
