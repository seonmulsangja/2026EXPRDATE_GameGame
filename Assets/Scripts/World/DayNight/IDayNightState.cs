using System;

namespace ExpiryGame.World
{
    // 낮밤 진행도를 읽는 쪽(타일 크로스페이드, 발광, 비네트, 랜드마크 등)이 구독하는 계약.
    public interface IDayNightState
    {
        DayNightSample Current { get; }
        DayNightProfile Profile { get; }
        event Action<DayNightSample> Changed;
    }

    // 프리팹(씬 참조를 가질 수 없음)이 현재 씬의 IDayNightState를 찾는 지점. DayNightDirector가 등록한다.
    public static class DayNightState
    {
        public static IDayNightState Instance { get; internal set; }
        public static float Night01 => Instance != null ? Instance.Current.Night01 : 0f;
    }
}
