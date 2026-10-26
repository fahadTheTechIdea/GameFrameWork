using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent authored metadata reuses the original schedule/template codec and plan owner.
    public sealed class IndependentRecurringActivityDef : ComponentDef<RecurringActivitySource>, IRecurringActivitySourceDefinition
    {
        private RecurringActivityDeclaration _declaration;
        internal Action ObserveConditions;
        public string Fault { get; private set; }
        public Code CapabilityId => _declaration.CapabilityId;
        public RecurringCadenceBasis Cadence => Fault == "cadence-invalid" ? (RecurringCadenceBasis)255 : _declaration.Cadence;
        public RecurringTriggerMode TriggerMode => _declaration.TriggerMode;
        public RecurringWorkerDispatchMode WorkerDispatch => _declaration.WorkerDispatch;
        public string PlanKey => _declaration.PlanKey;
        public IPlanTemplateContent Template => _declaration.Template;
        public RecurringPlanSettings Initial => Fault == "initial-null" ? null : _declaration.Initial;
        public RecurringPlanBinding Binding => Fault == "binding-null" ? null : _declaration.Binding;
        public string Group => _declaration.Group;
        public Code OperationPolicy => _declaration.OperationPolicy;
        public IReadOnlyList<SignalGateCondition> Conditions
        { get { ObserveConditions?.Invoke(); return Fault == "conditions-null" ? null : _declaration.Conditions; } }
        protected override void ReadCore(JsonReader reader)
        { Fault = reader.String("metadata_fault"); _declaration = RecurringActivityDeclaration.Read(reader); }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public void ValidateBinding(World world)
        {
            RecurringActivityBindingContracts.ValidateBinding(world, this);
            if (Fault == "validation-throw") throw new InvalidOperationException("proof.recurring.validation");
            if (Fault == "validation-pause") ((GameClock)world.Clock).Pause();
            if (Fault == "validation-reenter") world.Services.Get<WorkBoard>().ValidateComposition(world);
        }
    }
    public sealed class UnboundRecurringActivityDef : ComponentDef<RecurringActivitySource>, INamedCapabilityDefinition
    {
        private RecurringActivityDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        protected override void ReadCore(JsonReader reader) => _declaration = RecurringActivityDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    public sealed class RecurringActivityBindingsModule : ControlContractModule
    {
        public bool ActualBinding { get; private set; }
        public RecurringActivityBindingsModule(InitializeProbe initialize) : base(initialize) { }
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(WorkModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        {
            base.DeclareContent(types);
            types.Component<IndependentRecurringActivityDef>("independent_recurring", 8);
            types.Component<UnboundRecurringActivityDef>("unbound_recurring", 8);
            types.Component<ExposurePrivateProbeDef>("recurring_private_probe", 1);
        }
        public override void Compose(WorldComposer composer)
        {
            base.Compose(composer);
            var binding = composer.Content.Get<EntityDef>("field.routine").Component<IRecurringActivitySourceDefinition>(new Code("routine"));
            ActualBinding = binding is IndependentRecurringActivityDef && binding.ComponentType == typeof(RecurringActivitySource)
                && binding.PlanKey == "activity:field.routine-work" && binding.Template == null;
            composer.AddSystem(new ProbeInstaller(), 302);
        }
        private sealed class ProbeInstaller : GameSystem
        {
            public override string Id => "proof.recurring-bindings";
            public override int StateSchemaVersion => 1;
            public override void Update(World world, TickInfo time) { } // Negative observation installation only.
            public override void Initialize(World world)
            {
            foreach (var owner in world.Content.All<EntityDef>())
                foreach (var part in owner.Components)
                    if (part is IndependentRecurringActivityDef definition && definition.Fault.StartsWith("live-", StringComparison.Ordinal))
                        definition.ObserveConditions = () => {
                            Entity source = null;
                            foreach (var entity in world.Entities.All) if (ReferenceEquals(entity.Def, owner)) { source = entity; break; }
                            if (source == null || world.Clock.StepIndex == 0) return;
                            if (definition.Fault == "live-throw") throw new InvalidOperationException("proof.recurring.conditions");
                            if (definition.Fault == "live-pause") ((GameClock)world.Clock).Pause();
                            if (definition.Fault == "live-touch") source.Touch();
                            if (definition.Fault == "live-private") source.Get<ExposurePrivateProbe>().Corrupt();
                        };
            }
        }
    }
}
