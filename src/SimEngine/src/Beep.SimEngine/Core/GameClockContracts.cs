namespace Beep.SimEngine
{
    /// <summary>Read-only facts from the session's sole game clock. StepIndex is its exact time coordinate.</summary>
    public interface IGameClock
    {
        long StepIndex { get; }
        ISimulationTime Time { get; }
        GameDuration TotalGameTime { get; }
        GameDuration StepDuration { get; }
        CalendarReading Date { get; }
        bool Paused { get; }
        int Speed { get; }
    }
}
