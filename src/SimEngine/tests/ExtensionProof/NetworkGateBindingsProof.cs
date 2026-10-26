using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentNetworkGateDef : ComponentDef<SignalNetworkGate>, ISignalNetworkGateDefinition
    {
        private SignalNetworkGateDeclaration _declaration;
        internal Action ObserveNetworks;
        public string Fault { get; private set; }
        public Code CapabilityId => _declaration.CapabilityId;
        public IReadOnlyList<INetworkDefinition> Networks { get { ObserveNetworks?.Invoke(); return Fault=="networks-null"?null:Fault=="networks-empty"?Array.Empty<INetworkDefinition>():Fault=="networks-duplicate"?Array.AsReadOnly(new[]{_declaration.Networks[0],_declaration.Networks[0]}):_declaration.Networks; } }
        public IReadOnlyList<SignalGateCondition> Conditions => Fault=="conditions-null"?null:Fault=="conditions-empty"?Array.Empty<SignalGateCondition>():Fault=="conditions-duplicate"?Array.AsReadOnly(new[]{_declaration.Conditions[0],_declaration.Conditions[0]}):_declaration.Conditions;
        protected override void ReadCore(JsonReader reader)
        {Fault=reader.String("metadata_fault");_declaration=SignalNetworkGateDeclaration.Read(reader);}
        protected override void ResolveCore(ContentResolver content)=>_declaration.Resolve(content);
        protected override void ValidateCore(EntityDef owner)=>_declaration.ValidateOwner(owner);
    }
    public sealed class UnboundNetworkGateDef : ComponentDef<SignalNetworkGate>, INamedCapabilityDefinition
    {
        private SignalNetworkGateDeclaration _declaration;
        public Code CapabilityId=>_declaration.CapabilityId;
        protected override void ReadCore(JsonReader reader)=>_declaration=SignalNetworkGateDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content)=>_declaration.Resolve(content);
    }
    public sealed class WrongCodecNetworkGateDef : ComponentDef<SignalNetworkGate>, ISignalNetworkGateDefinition
    {
        private SignalNetworkGateDeclaration _declaration;
        public Code CapabilityId=>_declaration.CapabilityId;
        public IReadOnlyList<INetworkDefinition> Networks=>_declaration.Networks;
        public IReadOnlyList<SignalGateCondition> Conditions=>_declaration.Conditions;
        protected override void ReadCore(JsonReader reader)=>_declaration=SignalNetworkGateDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content)=>_declaration.Resolve(content);
    }
    // Metadata plus explicit negative injection only. Flow/reserve decisions remain in engine gates.
    public sealed class NetworkGateBindingsModule : GameModule
    {
        public override string Id=>"proof.network-gate-bindings";
        public override ModuleVersion Version=>new ModuleVersion(1,0,0);
        public override IReadOnlyList<ModuleRequirement> Requires=>new[]{new ModuleRequirement(InfrastructureModule.ModuleId,Version)};
        public bool ActualBinding {get;private set;}
        public override void DeclareContent(ContentTypes types)
        {
            types.Component<IndependentNetworkGateDef>("independent_network_gate",1);
            types.Component<UnboundNetworkGateDef>("unbound_network_gate",1);
            types.Component<WrongCodecNetworkGateDef>("wrong_codec_network_gate",2);
        }
        public override void Compose(WorldComposer composer)
        {
            var gate=composer.Content.Get<EntityDef>("binding.source").Component<ISignalNetworkGateDefinition>(new Code("safety"));
            ActualBinding=gate is IndependentNetworkGateDef && gate.ComponentType==typeof(SignalNetworkGate)
                && ReferenceEquals(gate.Networks[0],composer.Content.Get<INetworkDefinition>("binding.main"));
            composer.AddSystem(new ProbeInstaller(),305);
        }
        private sealed class ProbeInstaller : GameSystem
        {
            public override string Id=>"proof.network-gate-binding-faults";
            public override int StateSchemaVersion=>1;
            public override void Update(World world,TickInfo time) { }
            public override void Initialize(World world)
            {
                foreach(var owner in world.Content.All<EntityDef>())foreach(var part in owner.Components)
                    if(part is IndependentNetworkGateDef definition && definition.Fault.StartsWith("live-",StringComparison.Ordinal))
                        definition.ObserveNetworks=()=>{
                            Entity source=null;foreach(var entity in world.Entities.All)if(ReferenceEquals(entity.Def,owner)){source=entity;break;}
                            if(source==null||world.Clock.StepIndex==0)return;
                            if(definition.Fault=="live-throw")throw new InvalidOperationException("proof.network-gate.networks");
                            if(definition.Fault=="live-pause")((GameClock)world.Clock).Pause();
                            if(definition.Fault=="live-touch")source.Touch();
                            if(definition.Fault=="live-private")source.Get<ExposurePrivateProbe>().Corrupt();
                        };
            }
        }
    }
}
