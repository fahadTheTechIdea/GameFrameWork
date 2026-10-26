using System.Collections.Generic;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Views
{
    public sealed class PlayerControllersView
    {
        private readonly World _world;
        private readonly PlayerId _viewer;
        internal PlayerControllersView(World world, PlayerId viewer) { _world = world; _viewer = viewer; }
        public void CopyControllers(IList<PlayerControllerFacts> destination)
        {
            if (destination == null) throw new System.ArgumentNullException(nameof(destination));
            destination.Clear(); _world.RequireHealthyUpdate();
            _world.Services.Get<IPlayerControllers>().CopyControllers(_viewer, destination);
        }
        public Code PreviewSelection(Controller selection)
        { _world.RequireHealthyUpdate(); return _world.Services.Get<IPlayerControllers>().CheckSelection(_viewer, selection); }
        public bool CopyPlaybook(PlayerId company, IList<PlaybookStepSnapshot> destination)
        {
            if (destination == null) throw new System.ArgumentNullException(nameof(destination));
            destination.Clear(); _world.RequireHealthyUpdate();
            return _world.Services.Get<IPlayerControllers>().CopyPlaybook(_viewer, company, destination);
        }
    }
}
