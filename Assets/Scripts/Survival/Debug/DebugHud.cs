using System.Text;
using UnityEngine;

namespace ExpiryGame
{
    // 임시 디버그용: UI 담당 HUD가 완성되면 삭제
    public class DebugHud : MonoBehaviour
    {
        [SerializeField] private GameClock clock;
        [SerializeField] private Health player;
        [SerializeField] private FoodInventory inventory;
        [SerializeField] private Camera viewCamera;

        private Health[] actors;
        private readonly StringBuilder builder = new StringBuilder();

        private void Start()
        {
            actors = FindObjectsByType<Health>(FindObjectsSortMode.None);
        }

        private void OnGUI()
        {
            DrawStatus();
            DrawActorHealth();
        }

        private void DrawStatus()
        {
            var foods = (IFoodInventory)inventory;
            builder.Clear();
            builder.AppendLine($"Day {clock.DayCount}  {clock.Phase}  {clock.TimeOfDay01:P0}");
            builder.AppendLine($"HP {player.Current:0}/{player.Max:0}");
            foreach (var item in foods.Items)
            {
                var mark = item == foods.Selected ? ">" : " ";
                var life = item.IsRotten ? "ROTTEN" : $"{item.RemainingShelfLife:0}s ({clock.SecondsToDays(item.RemainingShelfLife):0.00}d)";
                builder.AppendLine($"{mark} {item.Data.displayName}  {life}  heal {item.CurrentHeal:0.#}  dmg {item.CurrentDamage:0.#}  dur {item.RemainingDurability}/{item.Stats.MaxDurability}");
            }
            GUI.Label(new Rect(10, 10, 800, 300), builder.ToString());
        }

        private void DrawActorHealth()
        {
            foreach (var actor in actors)
            {
                if (actor == null || actor == player) continue;
                var screen = viewCamera.WorldToScreenPoint(actor.transform.position);
                GUI.Label(new Rect(screen.x - 30, Screen.height - screen.y - 50, 120, 24), $"HP {actor.Current:0}");
            }
        }
    }
}
