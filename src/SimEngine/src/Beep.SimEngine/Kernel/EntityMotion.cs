namespace Beep.SimEngine.Kernel
{
    /// <summary>Physical motion facts independent of the provider's coordinate or route representation.</summary>
    public enum MoveStatus : byte
    {
        Idle = 0,
        Moving = 1,
        Arrived = 2,
        Unreachable = 3
    }

    /// <summary>
    /// One entity's physical motion owner. Provider-specific contracts set destinations. The declared
    /// movement phase advances it once per tick; only Entity.Update commits the entity's state.
    /// </summary>
    public interface IMotion : IEntityCapability
    {
        MoveStatus Status { get; }
        void Stop();
        void Advance(World world, TickInfo tick, Fix speedModifier);
    }
}
