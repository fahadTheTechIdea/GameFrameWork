using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    public sealed class IncidentPropagationModule : GameModule
    {
        public const string ModuleId = "sim.incident-propagation";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(IncidentResponseModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<TileIncidentPropagationRuleDef>("tile_incident_propagation_rules");
            types.Section<NetworkIncidentPropagationRuleDef>("network_incident_propagation_rules");
            types.Component<IncidentPropagationEmitterDef>("incident_propagation", 1);
        }
        public override void Compose(WorldComposer composer)
        {
            JsonReader settings = composer.Scenario.SettingsFor(Id) ?? throw new ContentException("selected propagation needs explicit budget settings");
            int budget = settings.Int("budget"); settings.End(); if (budget <= 0) throw settings.Error("propagation budget must be positive");
            var propagation = new IncidentPropagationSystem(composer.World, budget);
            composer.Services.Bind<IIncidentPropagationState>(propagation); composer.AddSystem(propagation, 380);
            composer.AddViewFeature(Id, 2, (world, viewer) => new IncidentPropagationView(world, viewer));
        }
    }
}
