using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Hosting
{
    public interface ILockstepPolicy : IContentDefinition
    {
        long? MaximumAhead { get; }
        long? MaximumCommandsPerFrame { get; }
    }
    /// <summary>Detached policy facts, not a second queue or session state.</summary>
    public readonly struct LockstepPolicySettings
    {
        public readonly long? MaximumAhead, MaximumCommandsPerFrame;
        public LockstepPolicySettings(long? ahead, long? commands)
        { MaximumAhead = ahead; MaximumCommandsPerFrame = commands; }
    }
    public static class LockstepPolicyContracts
    {
        public static LockstepPolicySettings Read(World world, ILockstepPolicy policy)
            => EntityObservation.ReadProvider(world, policy, null, () => {
                if (policy == null || !world.Content.Owns(policy) ||
                    !ReferenceEquals(world.Content.Find<ILockstepPolicy>(policy.Id), policy))
                    throw new ContentException("lockstep policy requires its actual immutable catalog alias");
                long? ahead = policy.MaximumAhead, commands = policy.MaximumCommandsPerFrame;
                if (ahead.HasValue && ahead.Value <= 0 || commands.HasValue && commands.Value <= 0)
                    throw new ContentException(policy.Id + ": lockstep limits must be positive or explicit null");
                return new LockstepPolicySettings(ahead, commands);
            });
    }
}
