using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Metadata registration delegates normal weather behavior to the engine; only faults are injected here.
    public sealed class IndependentWeatherDef : Def, IWeatherDefinition
    {
        private WeatherTimelineDeclaration _declaration;
        internal string Fault;
        public IReadOnlyList<Code> ConditionIds => _declaration.ConditionIds;
        protected override void ReadCore(JsonReader reader) => _declaration = WeatherTimelineDeclaration.Read(reader);
        public void ValidateBinding(World world) => _declaration.ValidateBinding(world);
        public WeatherObservation Observe(WeatherContext context)
        {
            if (Fault == "throw") throw new InvalidOperationException("proof.weather.original");
            if (Fault == "pause") ((GameClock)context.World.Clock).Pause();
            if (Fault == "map") ((TileMap)context.World.Space).Layer<ByteLayer>("network.line").Set(new TileCoord(10,28),0);
            if (Fault == "recursive") context.World.Services.Get<IWeatherReader>().Read(Id,new PlayerId(0),out _);
            if (Fault == "malformed") return default;
            if (Fault == "unknown") return new WeatherObservation(new Code("weather.phase"),new Code("weather.undeclared"),0,1,Code.None);
            return _declaration.Observe(context);
        }
    }
    public sealed class WeatherProofModule : GameModule
    {
        private IndependentWeatherDef _definition;
        private readonly InitializeProbe _initialize;
        public WeatherProofModule(InitializeProbe initialize) { _initialize = initialize; }
        public override string Id => "proof.weather";
        public override ModuleVersion Version => new ModuleVersion(1,0,0);
        public override void DeclareContent(ContentTypes types) => types.Section<IndependentWeatherDef>("independent_weather_timelines");
        public override void Compose(WorldComposer composer)
        {
            var actual = composer.Content.Get<IWeatherDefinition>("weather.local");
            if (!composer.Content.Owns(actual)) throw new ContentException("proof weather must be its actual immutable alias");
            _definition = actual as IndependentWeatherDef;
            composer.AddSystem(_initialize,12345);
        }
        public void Arm(string fault)
        { if (_definition == null) throw new InvalidOperationException("negative weather requires selected independent provider"); _definition.Fault = fault; }
    }
}
