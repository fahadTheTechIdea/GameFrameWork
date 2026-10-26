using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Views
{
    /// <summary>Detached selected event policy metadata; no live queue or mutable settings escape.</summary>
    public sealed class EventDispatchPolicyView
    {
        internal EventDispatchPolicyView(World world)
        {
            world.RequireHealthyUpdate();
            Policy = world.Services.Get<IEventDispatchPolicy>().Id;
            MaximumEventsPerDispatch = world.Events.MaximumEventsPerDispatch;
        }
        public string Policy { get; }
        public long? MaximumEventsPerDispatch { get; }
    }
}
