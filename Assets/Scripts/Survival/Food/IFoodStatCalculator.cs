namespace ExpiryGame
{
    public interface IFoodStatCalculator
    {
        FoodStats Calculate(float shelfLifeSeconds);
    }
}
