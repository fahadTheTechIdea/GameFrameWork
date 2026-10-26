using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Views
{
    public sealed class WeatherView
    {
        private readonly World _world;
        private readonly PlayerId _viewer;
        internal WeatherView(World world, PlayerId viewer) { _world = world; _viewer = viewer; }
        public Code Read(string provider, out WeatherSnapshot snapshot)
            => _world.Services.Get<IWeatherReader>().Read(provider, _viewer, out snapshot);
    }
}
