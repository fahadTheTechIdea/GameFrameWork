namespace Beep.SimEngine
{
    /// <summary>Whole verified generation intent, not a history event or a second stock authority.</summary>
    public readonly struct ResourceGenerationContext
    {
        public ReservationOwner Owner { get; }
        public PlayerId Company { get; }
        public Code Cause { get; }
        public Bill Consumed { get; }
        public Bill Produced { get; }
        public ResourceGenerationContext(ReservationOwner owner, PlayerId company, Code cause, Bill consumed, Bill produced)
        { Owner = owner; Company = company; Cause = cause; Consumed = consumed; Produced = produced; }
    }
    /// <summary>Optional engine-owned commit participant; absent in games without generation income.</summary>
    public interface IResourceProductionSettlement
    {
        Code CheckGeneration(ResourceGenerationContext context);
        Code PrepareGeneration(ResourceGenerationContext context, out IResourceProductionCommit commit);
    }
    /// <summary>Ephemeral admission, valid only in the same synchronous verified resource commit.</summary>
    public interface IResourceProductionCommit : IResourceInventoryCommit { }
}
