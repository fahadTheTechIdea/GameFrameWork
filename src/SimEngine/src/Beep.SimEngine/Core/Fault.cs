namespace Beep.SimEngine
{
    /// <summary>
    /// Why a game stopped: an engine operation failed. Immutable and latched; a faulted game never
    /// completes another game update. Tick is a coordinate of the sole game clock, not another timer.
    /// The text is for a
    /// developer's log (it is an exception message), never for a player.
    /// </summary>
    public sealed class WorldFault
    {
        internal WorldFault(long tick, string phase, System.Exception exception)
        {
            Tick = tick;
            Phase = phase;
            Message = exception.GetType().Name + ": " + exception.Message;
            Detail = exception.ToString();
        }

        public long Tick { get; }
        public string Phase { get; }
        public string Message { get; }
        public string Detail { get; }
        public override string ToString() => "game update " + Tick + " in " + Phase + ": " + Message;
    }
}
