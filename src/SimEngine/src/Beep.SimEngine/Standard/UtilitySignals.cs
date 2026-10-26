using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Native registration; independent authored bindings share the same declaration/runtime.</summary>
    public sealed class UtilitySignalDef : ComponentDef<UtilitySignal>, IUtilitySignalDefinition
    {
        private UtilitySignalDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        public Code Consumer => _declaration.Consumer;
        public int SignalSchemaVersion => 1;
        public IReadOnlyList<AutomationMetricSchema> SignalBindings => UtilitySignalDeclaration.Metrics;
        protected override void ReadCore(JsonReader reader) => _declaration = UtilitySignalDeclaration.Read(reader);
        protected override void ValidateCore(EntityDef owner) => _declaration.ValidateOwner(owner);
    }
    public sealed class UtilitySignal : Component, IAutomationSignalSource
    {
        private IUtilitySignalDefinition Binding => base.Def as IUtilitySignalDefinition
            ?? throw new InvalidOperationException("utility signal requires its actual metadata contract");
        public Code CapabilityId => Binding.CapabilityId;
        public AutomationSignalSnapshot ReadSignal(World world, IAutomationSignalContent signal)
        {
            if (world == null || signal == null || Entity == null || !Entity.Alive || !ReferenceEquals(world.Entities.Get(Entity.Id), Entity) ||
                !ReferenceEquals(Entity.Get<IAutomationSignalSource>(CapabilityId), this) ||
                !ReferenceEquals(world.Content.Find<IAutomationSignalContent>(signal.Id), signal))
                throw new ArgumentException("utility signal requires its actual live session/authored signal");
            return UtilitySignalBindingContracts.Observe(world, Binding, Entity, () =>
            {
                UtilityDemandSnapshot facts = UtilityContracts.Read(world, Entity.Get<IUtilityConsumer>(Binding.Consumer));
                if (signal.Metric.Id == "utility.ready")
                    return !facts.Drawing ? signal.Unavailable(UtilityCodes.Inactive) : !facts.NextBlocker.IsNone
                        ? signal.Unavailable(facts.NextBlocker) : signal.Boolean(facts.Satisfied);
                var definition = ResourceQuantitySignals.Definition(world, signal);
                if (!facts.Drawing) return signal.Unavailable(UtilityCodes.Inactive);
                bool declared = false; foreach (ResourceId resource in facts.Resources) if (resource == definition.Resource.Resource) declared = true;
                if (!declared) return signal.Unavailable(ResourceSignalCodes.Incompatible);
                if (!facts.NextBlocker.IsNone) return signal.Unavailable(facts.NextBlocker);
                Fix amount;
                switch (signal.Metric.Id)
                {
                    case "utility.requested": amount = facts.NextInputs.AmountOf(definition.Resource.Resource); break;
                    case "utility.available": amount = facts.Available.AmountOf(definition.Resource.Resource); break;
                    case "utility.consumed": amount = facts.Consumed.AmountOf(definition.Resource.Resource); break;
                    default: return signal.Unavailable(OperationCodes.SignalUnavailable);
                }
                return ResourceQuantitySignals.ReadNative(world, signal, amount);
            });
        }
    }
    /// <summary>Optional stateless signal registration; consumption/update/state stay in selected providers.</summary>
    public sealed class UtilitySignalsModule : GameModule
    {
        public const string ModuleId = "sim.utility-signals";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] { new ModuleRequirement(OperationsModule.ModuleId, Version) });
        public override void DeclareContent(ContentTypes types) => types.Component<UtilitySignalDef>("utility_signal", 1);
        public override void Compose(WorldComposer composer) => UtilitySignalBindingContracts.ValidateComposition(composer.World);
    }
}
