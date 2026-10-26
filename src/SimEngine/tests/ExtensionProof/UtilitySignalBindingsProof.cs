using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentUtilitySignalDef : ComponentDef<UtilitySignal>, IUtilitySignalDefinition
    {
        private UtilitySignalDeclaration _declaration;
        internal Action ObserveConsumer;
        public string Fault { get; private set; }
        public Code CapabilityId => _declaration.CapabilityId;
        public Code Consumer { get { ObserveConsumer?.Invoke(); return Fault == "consumer-none" ? Code.None : _declaration.Consumer; } }
        public int SignalSchemaVersion => Fault == "schema-version" ? 2 : 1;
        public IReadOnlyList<AutomationMetricSchema> SignalBindings => Fault == "bindings-null" ? null
            : Fault == "bindings-empty" ? Array.Empty<AutomationMetricSchema>() : UtilitySignalDeclaration.Metrics;
        protected override void ReadCore(JsonReader reader)
        { Fault = reader.String("metadata_fault"); _declaration = UtilitySignalDeclaration.Read(reader); }
        protected override void ValidateCore(EntityDef owner) => _declaration.ValidateOwner(owner);
    }
    public sealed class UnboundUtilitySignalDef : ComponentDef<UtilitySignal>, IAutomationSignalDefinition
    {
        private UtilitySignalDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        public int SignalSchemaVersion => 1;
        public IReadOnlyList<AutomationMetricSchema> SignalBindings => UtilitySignalDeclaration.Metrics;
        protected override void ReadCore(JsonReader reader) => _declaration = UtilitySignalDeclaration.Read(reader);
    }
    // No second utility owner: only authored definitions and negative getter installation.
    public sealed class UtilitySignalBindingsModule : GameModule
    {
        public override string Id => "proof.utility-signal-bindings";
        public override ModuleVersion Version => new ModuleVersion(1,0,0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] {new ModuleRequirement(UtilitySignalsModule.ModuleId,Version)};
        public bool ActualBinding { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            types.Component<IndependentUtilitySignalDef>("independent_utility_signal",1);
            types.Component<UnboundUtilitySignalDef>("unbound_utility_signal",1);
            types.Component<ExposurePrivateProbeDef>("utility_signal_private_probe",1);
        }
        public override void Compose(WorldComposer composer)
        {
            var definition=composer.Content.Get<EntityDef>("contract.factory").Component<IUtilitySignalDefinition>(new Code("utility"));
            ActualBinding=definition is IndependentUtilitySignalDef && definition.ComponentType==typeof(UtilitySignal)
                && definition.Consumer==new Code("contract.process");
            composer.AddSystem(new ProbeInstaller(),304);
        }
        private sealed class ProbeInstaller : GameSystem
        {
            public override string Id=>"proof.utility-signal-binding-faults";
            public override int StateSchemaVersion=>1;
            public override void Update(World world,TickInfo time) { } // No gameplay.
            public override void Initialize(World world)
            {
                foreach(var owner in world.Content.All<EntityDef>())
                    foreach(var part in owner.Components)
                        if(part is IndependentUtilitySignalDef definition && definition.Fault.StartsWith("live-",StringComparison.Ordinal))
                            definition.ObserveConsumer=()=>{
                                Entity source=null;foreach(var entity in world.Entities.All)if(ReferenceEquals(entity.Def,owner)){source=entity;break;}
                                if(source==null||world.Clock.StepIndex==0)return;
                                if(definition.Fault=="live-pause"||definition.Fault=="live-pause-throw")((GameClock)world.Clock).Pause();
                                if(definition.Fault=="live-touch"||definition.Fault=="live-touch-throw")source.Touch();
                                if(definition.Fault=="live-private"||definition.Fault=="live-private-throw")source.Get<ExposurePrivateProbe>().Corrupt();
                                if(definition.Fault=="live-throw"||definition.Fault.EndsWith("-throw",StringComparison.Ordinal))
                                    throw new InvalidOperationException("proof.utility-signal.consumer");
                            };
            }
        }
    }
}
