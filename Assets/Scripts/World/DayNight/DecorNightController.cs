using UnityEngine;
using ExpiryGame.MapGen;

namespace ExpiryGame.World
{
    // 낮밤 진행도(Night01)를 맵 장식(MapGen)에 전달한다. 장식 수백 개가 매 틱 갱신되지 않도록 눈에 띄는 변화(1%)만 넘긴다.
    public class DecorNightController : MonoBehaviour
    {
        [SerializeField] private DayNightDirector director;
        [SerializeField, Range(0.001f, 0.1f)] private float step = 0.01f;

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
            var atEdge = (night01 <= 0f || night01 >= 1f) && !Mathf.Approximately(night01, applied);
            if (!atEdge && Mathf.Abs(night01 - applied) < step) return;
            applied = night01;
            DecorLighting.Set(night01);
        }
    }
}
