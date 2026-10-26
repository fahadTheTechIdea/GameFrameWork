using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Views
{
    public sealed class TechnologyView
    {
        private readonly World _world; private readonly PlayerId _viewer;
        internal TechnologyView(World world, PlayerId viewer) { _world = world; _viewer = viewer; }
        public Code ReadTechnology(string technology, out TechnologySnapshot snapshot)
            => ReadTechnology(_viewer, technology, out snapshot);
        public Code ReadTechnology(PlayerId company, string technology, out TechnologySnapshot snapshot)
        {
            snapshot = default; _world.RequireHealthyUpdate();
            return _world.Services.Get<ITechnologyKnowledge>().ReadTechnology(_viewer, company, technology, out snapshot);
        }
        public void CopyTechnologies(IList<TechnologySnapshot> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear(); _world.RequireHealthyUpdate();
            _world.Services.Get<ITechnologyKnowledge>().CopyTechnologies(_viewer, destination);
        }
        public Code RouteTo(string technology, IList<string> destination)
        { _world.RequireHealthyUpdate(); return _world.Services.Get<ITechnologyKnowledge>().CopyRoute(_viewer, technology, destination); }
        public Code PreviewResearch(string technology, EntityId target, int priority)
        { _world.RequireHealthyUpdate(); return _world.Services.Get<WorkBoard>().CheckPlayerActivity(_viewer, "activity:" + technology, target, priority); }
    }
}
