using UnityEngine;

namespace ExpiryGame
{
    [RequireComponent(typeof(Collider2D))]
    public class FoodPickup : MonoBehaviour
    {
        [SerializeField] private FoodData food;

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var inventory = other.GetComponentInParent<IFoodInventory>();
            if (inventory != null && inventory.TryAdd(food)) Destroy(gameObject);
        }
    }
}
