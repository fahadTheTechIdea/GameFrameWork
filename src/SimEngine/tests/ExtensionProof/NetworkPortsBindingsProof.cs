using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
namespace Beep.SimEngine.ExtensionProof
{
    // Independent metadata delegates sole engine authoring; negative getters contain no normal gameplay.
    public sealed class IndependentNetworkPortsDef : ComponentDef<Ports>, INetworkPortsDefinition
    {
        private NetworkPortsDeclaration _declaration;
        private ResourceId _unsupported;
        public string Fault { get; private set; }
        internal Action ObserveConnections;
        public IReadOnlyList<PortSpec> Connections
        {
            get
            {
                ObserveConnections?.Invoke();
                var rows = _declaration.Connections;
                if (Fault == "connections-null") return null;
                if (Fault == "connection-default") return Array.AsReadOnly(new[] { default(PortSpec) });
                if (Fault == "duplicate-network") return Array.AsReadOnly(new[] { rows[0], rows[0] });
                if (Fault == "foreign-network") return Array.AsReadOnly(new[] { new PortSpec(new NetworkDef(), rows[0].Push, rows[0].Pull) });
                if (Fault == "resource-invalid") return Array.AsReadOnly(new[] { new PortSpec(rows[0].Network, new[] { ResourceId.None }, rows[0].Pull) });
                if (Fault == "resource-unsupported") return Array.AsReadOnly(new[] { new PortSpec(rows[0].Network, new[] { _unsupported }, rows[0].Pull) });
                if (Fault == "resource-duplicate") return Array.AsReadOnly(new[] { new PortSpec(rows[0].Network, new[] { rows[0].Push[0], rows[0].Push[0] }, rows[0].Pull) });
                if (Fault == "metadata-throw") throw new ContentException("proof invalid port metadata");
                return rows;
            }
        }
        protected override void ReadCore(JsonReader reader)
        { Fault = reader.String("metadata_fault"); _declaration = NetworkPortsDeclaration.Read(reader); }
        protected override void ResolveCore(ContentResolver content)
        { _declaration.Resolve(content); _unsupported = content.Content.ResourceId("water"); }
        protected override void ValidateCore(EntityDef owner) => _declaration.ValidateOwner(owner);
    }
    public sealed class UnboundOriginalPortsDef : ComponentDef<Ports>
    {
        private NetworkPortsDeclaration _declaration;
        protected override void ReadCore(JsonReader reader) => _declaration = NetworkPortsDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    public sealed class InvalidNetworkPortsRuntimeDef : ComponentDef<InvalidNetworkPortsRuntime>, INetworkPortsDefinition
    {
        private NetworkPortsDeclaration _declaration;
        public IReadOnlyList<PortSpec> Connections => _declaration.Connections;
        protected override void ReadCore(JsonReader reader) => _declaration = NetworkPortsDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    public sealed class InvalidNetworkPortsRuntime : Component { }

    // Direct custom provider proves contract admission without a native metadata base or transfer algorithm.
    public sealed class IndependentCustomPortsDef : ComponentDef<IndependentCustomPorts>, INetworkPortsDefinition
    {
        private NetworkPortsDeclaration _declaration;
        public string Fault { get; private set; }
        public IReadOnlyList<PortSpec> Connections => _declaration.Connections;
        protected override void ReadCore(JsonReader reader)
        { Fault = reader.String("metadata_fault"); _declaration = NetworkPortsDeclaration.Read(reader); }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateCore(EntityDef owner) => _declaration.ValidateOwner(owner);
    }
    public sealed class IndependentCustomPorts : Component<IndependentCustomPortsDef>, INetworkPorts
    {
        internal Action ObserveConnections;
        internal Func<bool> FaultActive;
        public IReadOnlyList<PortSpec> Connections
        { get { ObserveConnections?.Invoke(); return Def.Fault == "live-copy" && FaultActive?.Invoke() == true ? new List<PortSpec>(Def.Connections).AsReadOnly() : Def.Connections; } }
    }
    public sealed class NetworkPortsBindingsModule : GameModule
    {
        public override string Id => "proof.network-ports-bindings";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(InfrastructureModule.ModuleId, Version) };
        public bool ActualBinding { get; private set; }
        public bool CustomBinding { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            types.Component<IndependentNetworkPortsDef>("independent_network_ports", 1);
            types.Component<IndependentCustomPortsDef>("independent_custom_ports", 1);
            types.Component<UnboundOriginalPortsDef>("unbound_original_ports", 1);
            types.Component<InvalidNetworkPortsRuntimeDef>("invalid_network_ports_runtime", 1);

        }
        public override void Compose(WorldComposer composer)
        {
            var owner = composer.Content.Get<EntityDef>("binding.source");
            var definition = owner.Component<INetworkPortsDefinition>();
            ActualBinding = definition is IndependentNetworkPortsDef && definition.ComponentType == typeof(Ports)
                && ReferenceEquals(definition.Connections[0].Network, composer.Content.Get<INetworkDefinition>("binding.main"));
            CustomBinding = definition is IndependentCustomPortsDef && definition.ComponentType == typeof(IndependentCustomPorts);
            composer.AddSystem(new ProbeInstaller(), 308);
        }
        private sealed class ProbeInstaller : GameSystem
        {
            public override string Id => "proof.network-ports-faults";
            public override int StateSchemaVersion => 1;
            // Startup spawn entries are flushed after OnNewGame; first Update sees actual runtime providers.
            public override void Update(World world, TickInfo time) => InstallFaults(world);
            public override void OnLoaded(World world) => InstallFaults(world);
            public override void Initialize(World world) => InstallFaults(world);
            private static void InstallFaults(World world)
            {
                foreach (var owner in world.Content.All<EntityDef>())
                    foreach (var part in owner.Components)
                    {
                        string fault = part is IndependentNetworkPortsDef original ? original.Fault
                            : part is IndependentCustomPortsDef custom ? custom.Fault : "none";
                        if (!fault.StartsWith("live-", StringComparison.Ordinal)) continue;
                        Action injection = () =>
                        {
                            if (world.Clock.StepIndex == 0) return;
                            Entity source = null; foreach (var entity in world.Entities.All) if (ReferenceEquals(entity.Def, owner)) { source = entity; break; }
                            if (source == null) return;
                            if (fault == "live-throw") throw new InvalidOperationException("proof.network-ports.connections");
                            if (fault == "live-pause") ((GameClock)world.Clock).Pause();
                            if (fault == "live-touch") source.Touch();
                            if (fault == "live-private") source.Get<ExposurePrivateProbe>().Corrupt();
                        };
                        if (part is IndependentNetworkPortsDef metadata) metadata.ObserveConnections = injection;
                        else foreach (var entity in world.Entities.All)
                            if (ReferenceEquals(entity.Def, owner) && entity.Get<INetworkPorts>() is IndependentCustomPorts provider)
                            { provider.ObserveConnections = injection; provider.FaultActive = () => world.Clock.StepIndex > 0; }
                    }
            }
        }
    }
}
