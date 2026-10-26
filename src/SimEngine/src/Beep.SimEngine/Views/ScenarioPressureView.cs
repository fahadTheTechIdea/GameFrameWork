using System.Collections.Generic;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Views
{
    public sealed class ScenarioPressureView
    {
        private readonly World _world;
        private readonly PlayerId _viewer;
        internal ScenarioPressureView(World world, PlayerId viewer) { _world = world; _viewer = viewer; }
        public void CopyPressure(IList<ScenarioPressureSnapshot> destination)
            => _world.Services.Get<IScenarioPressure>().CopyPressure(_viewer, destination);
        public bool CopyCandidates(PlayerId company, string source, IList<PressureCandidateSnapshot> destination)
            => _world.Services.Get<IScenarioPressure>().CopyCandidates(_viewer, company, source, destination);
    }
}
