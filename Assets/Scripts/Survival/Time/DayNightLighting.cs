using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ExpiryGame
{
    public class DayNightLighting : MonoBehaviour
    {
        [SerializeField] private GameClock clock;
        [SerializeField] private Light2D globalLight;
        [SerializeField] private Gradient colorOverDay;

        private void OnEnable()
        {
            clock.Ticked += OnTicked;
        }

        private void OnDisable()
        {
            clock.Ticked -= OnTicked;
        }

        private void OnTicked(float _)
        {
            globalLight.color = colorOverDay.Evaluate(clock.TimeOfDay01);
        }
    }
}
