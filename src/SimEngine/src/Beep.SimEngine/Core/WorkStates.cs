namespace Beep.SimEngine
{
    public enum OrderState : byte { Active = 0, Done = 1, Failed = 2, Cancelled = 3, Completing = 4 }

    public enum NodeState : byte
    {
        Waiting = 0,
        Ready = 1,
        Working = 2,
        Done = 3,
        Failed = 4,
        Paused = 5,
        Cancelled = 6,
        Completing = 7
    }
}
