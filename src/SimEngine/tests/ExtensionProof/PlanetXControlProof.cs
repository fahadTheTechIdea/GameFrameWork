using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Authored stateless safety query; knowledge and incident state remain in their existing owners.</summary>
    public sealed class PlanetSafetySignalDef : ComponentDef<PlanetSafetySignal>, IAutomationSignalDefinition
    {
        private string _technology;
        public Code CapabilityId { get; private set; }
        public Code Source { get; private set; }
        public ITechnologyDefinition Technology { get; private set; }
        public int SignalSchemaVersion => 1;
        private static readonly IReadOnlyList<AutomationMetricSchema> Metrics = Array.AsReadOnly(new[]
        { new AutomationMetricSchema(new Code("planet.safety-ready"), AutomationSignalKind.Boolean, new Code("unit.boolean")) });
        public IReadOnlyList<AutomationMetricSchema> SignalBindings => Metrics;
        protected override void ReadCore(JsonReader reader)
        {
            string id = reader.String("id"), source = reader.String("source"); _technology = reader.String("technology");
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(source))
                throw reader.Error("explicit named safety and incident identities required");
            CapabilityId = new Code(id); Source = new Code(source);
        }
        protected override void ResolveCore(ContentResolver content) => Technology = content.Def<ITechnologyDefinition>(_technology);
        protected override void ValidateCore(EntityDef owner)
        {
            if (owner.Component<IIncidentDefinition>(Source) == null)
                throw new ContentException(owner.Id + ": safety query needs its actual named incident definition");
        }
    }

    public sealed class PlanetSafetySignal : Component<PlanetSafetySignalDef>, IAutomationSignalSource
    {
        public Code CapabilityId => Def.CapabilityId;
        public AutomationSignalSnapshot ReadSignal(World world, IAutomationSignalContent signal)
        {
            if (signal.Metric != new Code("planet.safety-ready")) return signal.Unavailable(OperationCodes.SignalUnavailable);
            IIncident incident = Entity.Get<IIncident>(Def.Source);
            if (incident == null) return signal.Unavailable(IncidentCodes.NotResponse);
            Code knowledge = world.Services.Get<ITechnologyKnowledge>().CheckKnown(Entity.Owner, Def.Technology.Id);
            if (!knowledge.IsNone && knowledge != TechnologyCodes.Locked) return signal.Unavailable(knowledge);
            return signal.Boolean(knowledge.IsNone && !ResponseWork.Read(incident).Active);
        }
    }

    /// <summary>Optional ordinary game-content module; no scheduler, saved safety latch or completion owner.</summary>
    public sealed class PlanetXControlModule : GameModule
    {
        public override string Id => "planet-x-control";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[]
        {
            new ModuleRequirement("planet-x", Version), new ModuleRequirement(TechnologyModule.ModuleId, Version),
            new ModuleRequirement(AutomationModule.ModuleId, Version), new ModuleRequirement(IncidentResponseModule.ModuleId, Version),
            new ModuleRequirement(ScenarioPressureModule.ModuleId, Version)
        };
        public override void DeclareContent(ContentTypes types) => types.Component<PlanetSafetySignalDef>("planet_safety_signal", 1);
    }
}

