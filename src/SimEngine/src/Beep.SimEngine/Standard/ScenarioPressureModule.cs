using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    public class ScenarioPressureModule : GameModule
    {
        public const string ModuleId = "sim.scenario-pressure";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<ScenarioPressureProfileDef>("scenario_pressure_profiles");
            types.Section<IncidentPressureSourceDef>("incident_pressure_sources");
        }
        public override void Compose(WorldComposer composer)
        {
            JsonReader settings = composer.Scenario.SettingsFor(Id) ?? throw new ContentException("selected pressure needs explicit company bindings");
            JsonValue companies = settings.Array("companies") ?? throw settings.Error("explicit companies array required"); settings.End();
            var bindings = new SortedDictionary<int, IScenarioPressureProfile>();
            for (int i = 0; i < companies.Count; i++)
            {
                var row = new JsonReader(companies[i], settings.Where + ".companies[" + i + "]");
                int company = row.Int("company"); string id = row.String("profile"); row.End();
                IScenarioPressureProfile profile = composer.Content.Find<IScenarioPressureProfile>(id);
                if (company < 0 || bindings.ContainsKey(company) || profile == null) throw row.Error("invalid/duplicate pressure company or missing profile");
                bindings.Add(company, profile);
            }
            var system = new ScenarioPressureSystem(composer.World, bindings);
            composer.Services.Bind<IScenarioPressure>(system); composer.AddSystem(system, 850);
            composer.AddViewFeature(Id, 2, (world, viewer) => new ScenarioPressureView(world, viewer));
        }
    }
}
