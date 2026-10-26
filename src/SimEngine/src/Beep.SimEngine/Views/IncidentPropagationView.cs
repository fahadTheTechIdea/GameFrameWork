using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Views
{
    public sealed class IncidentPropagationView
    {
        private readonly World _world;
        private readonly PlayerId _viewer;
        internal IncidentPropagationView(World world, PlayerId viewer) { _world = world; _viewer = viewer; }
        public Code ReadEligibility(EntityId source, Code emitter, EntityId target, out IncidentEligibilitySnapshot snapshot)
            => _world.Services.Get<IIncidentPropagationState>().ReadEligibility(source, emitter, target, _viewer, out snapshot);
        public bool TryRead(EntityId entity, Code emitter, out IncidentPropagationSnapshot snapshot)
            => _world.Services.Get<IIncidentPropagationState>().TryRead(entity, emitter, _viewer, out snapshot);
    }
}
