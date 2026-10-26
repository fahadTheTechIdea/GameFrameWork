namespace Beep.SimEngine.Kernel
{
    /// <summary>Pure current-route duration from the actual motion owner; no route or time authority.</summary>
    public interface IMotionEstimate : IMotion
    {
        MotionTravelEstimate EstimateRemaining(World world, Fix speedModifier);
    }

    /// <summary>A current-route estimate or an explicit unknown reason, never a promised arrival.</summary>
    public readonly struct MotionTravelEstimate
    {
        public long? RemainingTicks { get; }
        public Code Blocker { get; }
        public MotionTravelEstimate(long? remainingTicks, Code blocker)
        { RemainingTicks = remainingTicks; Blocker = blocker; }
    }
}
