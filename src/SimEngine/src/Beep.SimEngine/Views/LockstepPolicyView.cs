using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Views
{
    public sealed class LockstepPolicyView
    {
        internal LockstepPolicyView(World world)
        {
            world.RequireHealthyUpdate();
            var policy = world.Services.Get<ILockstepPolicy>();
            var settings = LockstepPolicyContracts.Read(world, policy);
            Policy = policy.Id; MaximumAhead = settings.MaximumAhead;
            MaximumCommandsPerFrame = settings.MaximumCommandsPerFrame;
        }
        public string Policy { get; }
        public long? MaximumAhead { get; }
        public long? MaximumCommandsPerFrame { get; }
    }
}
