using System;

namespace ExpiryGame
{
    public interface IGameClock
    {
        float DayLengthSeconds { get; }
        float TimeOfDay01 { get; }
        int DayCount { get; }
        DayPhase Phase { get; }

        event Action<float> Ticked;
        event Action<DayPhase> PhaseChanged;
        event Action<int> DayStarted;

        float SecondsToDays(float seconds);
    }
}
