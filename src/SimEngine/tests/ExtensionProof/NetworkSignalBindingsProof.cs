using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentNetworkSignalDef : ComponentDef<NetworkSignal>, INetworkSignalDefinition
    {
        private NetworkSignalDeclaration _declaration;
        internal Action ObserveNetwork;
        public string Fault { get; private set; }
        public Code CapabilityId => _declaration.CapabilityId;
        public INetworkDefinition Network { get { ObserveNetwork?.Invoke(); return Fault == "network-null" ? null : _declaration.Network; } }
        public int SignalSchemaVersion => Fault == "schema-version" ? 3 : 2;
        public IReadOnlyList<AutomationMetricSchema> SignalBindings => Fault == "bindings-null" ? null
            : Fault == "bindings-empty" ? Array.Empty<AutomationMetricSchema>() : NetworkSignalDeclaration.Metrics;
        protected override void ReadCore(JsonReader reader)
        { Fault = reader.String("metadata_fault"); _declaration = NetworkSignalDeclaration.Read(reader); }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateCore(EntityDef owner) => _declaration.ValidateOwner(owner);
    }
    public sealed class UnboundNetworkSignalDef : ComponentDef<NetworkSignal>, IAutomationSignalDefinition
    {
        private NetworkSignalDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        public int SignalSchemaVersion => 2;
        public IReadOnlyList<AutomationMetricSchema> SignalBindings => NetworkSignalDeclaration.Metrics;
        protected override void ReadCore(JsonReader reader) => _declaration = NetworkSignalDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    // Immutable authored definitions and negative getter installation only. No gameplay execution.
    public sealed class NetworkSignalBindingsModule : GameModule
    {
        private readonly InitializeProbe _initialize;
        public NetworkSignalBindingsModule(InitializeProbe initialize) { _initialize = initialize; }
        public override string Id => "proof.network-signal-bindings";
        public override ModuleVersion Version => new ModuleVersion(1,0,0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] {new ModuleRequirement(InfrastructureModule.ModuleId,Version),new ModuleRequirement(TileNavigationModule.ModuleId,Version)};
        public bool ActualBinding { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            types.Component<IndependentNetworkSignalDef>("independent_network_signal",1);
            types.Component<UnboundNetworkSignalDef>("unbound_network_signal",1);
            types.Component<ExposurePrivateProbeDef>("network_signal_private_probe",1);
        }
        public override void Compose(WorldComposer composer)
        {
            var definition=composer.Content.Get<EntityDef>("binding.source").Component<INetworkSignalDefinition>(new Code("pool"));
            ActualBinding=definition is IndependentNetworkSignalDef && definition.ComponentType==typeof(NetworkSignal)
                && ReferenceEquals(definition.Network,composer.Content.Get<INetworkDefinition>("binding.main"));
            composer.AddSystem(_initialize,303);composer.AddSystem(new ProbeInstaller(),304);
        }
        private sealed class ProbeInstaller : GameSystem
        {
            public override string Id=>"proof.network-signal-binding-faults";
            public override int StateSchemaVersion=>1;
            public override void Update(World world,TickInfo time) { }
            public override void Initialize(World world)
            {
                foreach(var owner in world.Content.All<EntityDef>())
                    foreach(var part in owner.Components)
                        if(part is IndependentNetworkSignalDef definition && definition.Fault.StartsWith("live-",StringComparison.Ordinal))
                            definition.ObserveNetwork=()=>{
                                Entity source=null;foreach(var entity in world.Entities.All)if(ReferenceEquals(entity.Def,owner)){source=entity;break;}
                                if(source==null||world.Clock.StepIndex==0)return;
                                if(definition.Fault=="live-pause"||definition.Fault=="live-pause-throw")((GameClock)world.Clock).Pause();
                                if(definition.Fault=="live-touch"||definition.Fault=="live-touch-throw")source.Touch();
                                if(definition.Fault=="live-private"||definition.Fault=="live-private-throw")source.Get<ExposurePrivateProbe>().Corrupt();
                                if(definition.Fault=="live-throw"||definition.Fault.EndsWith("-throw",StringComparison.Ordinal))
                                    throw new InvalidOperationException("proof.network-signal.network");
                            };
            }
        }
    }
}
