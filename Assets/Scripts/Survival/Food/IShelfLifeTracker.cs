namespace ExpiryGame
{
    public interface IShelfLifeTracker
    {
        void Register(FoodItem item);
        void Unregister(FoodItem item);
    }
}
