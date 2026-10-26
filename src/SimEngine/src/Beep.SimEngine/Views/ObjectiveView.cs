using System.Collections.Generic;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Views
{
    public sealed class ObjectiveView
    {
        private readonly World _world; private readonly PlayerId _viewer;
        internal ObjectiveView(World world, PlayerId viewer) { _world = world; _viewer = viewer; }
        public void CopyObjectives(IList<ObjectiveSnapshot> destination)
        { _world.RequireHealthyUpdate(); _world.Services.Get<IObjectives>().CopyObjectives(_viewer, destination); }
        public ObjectiveObservation Observe(string objective)
        { _world.RequireHealthyUpdate(); return _world.Services.Get<IObjectives>().Observe(_viewer, objective); }
    }
}
