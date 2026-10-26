using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    public sealed class IncidentResponseModule : GameModule
    {
        public const string ModuleId = "sim.incidents";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] { new ModuleRequirement(WorkModule.ModuleId, Version) });
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<IncidentResponseEffectDef>("response_effects");
            types.Section<IncidentWorkflowDef>("incident_workflows");
            types.Component<IncidentDef>("incident", 2);
            types.Component<IncidentSignalDef>("incident_signal", 1);
        }
        public override void Compose(WorldComposer composer)
        {
            ResponseWork.ValidateComposition(composer.World);
            composer.Commands.Register("sim.raise-incident", new RaiseIncidentHandler());
            composer.AddSystem(new IncidentResponseSystem(), 390);
            composer.AddViewFeature("sim.incidents", 9, (world, viewer) => new IncidentsView(world, viewer));
        }
    }
}
