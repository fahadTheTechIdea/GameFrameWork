using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Views
{
    public sealed class EnvironmentalMetricsView
    {
        private readonly World _world;
        private readonly PlayerId _viewer;
        internal EnvironmentalMetricsView(World world, PlayerId viewer) { _world = world; _viewer = viewer; }
        public EnvironmentalObservation Read(PlayerId company, string metric)
        {
            _world.RequireHealthyUpdate();
            var query = new EnvironmentalQuery(company, metric);
            return _viewer != PlayerId.None && company != _viewer ? EnvironmentalObservation.Refused(query, Codes.NotOwned)
                : _world.Services.Get<IEnvironmentalMetricSource>().Read(query);
        }
    }
}
