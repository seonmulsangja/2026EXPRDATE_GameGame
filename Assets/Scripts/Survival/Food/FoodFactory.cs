using UnityEngine;

namespace ExpiryGame
{
    public class FoodFactory : MonoBehaviour, IFoodFactory
    {
        [SerializeField] private FoodBalanceConfig balance;

        private IFoodStatCalculator calculator;

        private void Awake()
        {
            calculator = new FoodStatCalculator(balance);
        }

        public FoodItem Create(FoodData data)
        {
            return new FoodItem(data, calculator.Calculate(data.shelfLifeSeconds));
        }
    }
}
