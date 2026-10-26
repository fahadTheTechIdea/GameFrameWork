namespace Beep.SimEngine.Kernel
{
    /// <summary>Several distinct operations may share an entity, with one owner per authored identity.</summary>
    public interface INamedEntityCapability : IEntityCapability
    {
        Code CapabilityId { get; }
    }
    /// <summary>A capability belongs to one entity; it is not itself a world entity.</summary>
    public interface IEntityCapability
    {
        Entity Entity { get; }
    }
}
