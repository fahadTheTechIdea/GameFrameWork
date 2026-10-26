using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    public sealed class WeatherModule : GameModule
    {
        public const string ModuleId = "sim.weather";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override void DeclareContent(ContentTypes types) => types.Section<WeatherTimelineDef>("weather_timelines");
        public override void Compose(WorldComposer composer)
        {
            composer.Services.Bind<IWeatherReader>(new WeatherReader(composer.World));
            composer.AddViewFeature(Id, 1, (world, viewer) => new WeatherView(world, viewer));
        }
    }
}
