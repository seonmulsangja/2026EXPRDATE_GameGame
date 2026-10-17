using UnityEngine;
using UnityEngine.Tilemaps;

namespace ExpiryGame.World
{
    // 같은 좌표에 겹쳐 둔 night Tilemap의 알파를 Night01로 크로스페이드한다. (day Tilemap은 항상 불투명)
    public class NightTilemapFader : MonoBehaviour
    {
        [SerializeField] private DayNightDirector director;
        [SerializeField] private Tilemap nightTilemap;

        private float applied = -1f;

        private void OnEnable()
        {
            ((IDayNightState)director).Changed += OnChanged;
            Apply(((IDayNightState)director).Current.Night01);
        }

        private void OnDisable()
        {
            ((IDayNightState)director).Changed -= OnChanged;
        }

        private void OnChanged(DayNightSample sample)
        {
            Apply(sample.Night01);
        }

        private void Apply(float night01)
        {
            if (Mathf.Abs(night01 - applied) < 0.002f) return;
            applied = night01;
            nightTilemap.color = new Color(1f, 1f, 1f, night01);
        }
    }
}
