using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Independent metadata only; all normal authoring/selectors use the shared engine declaration.</summary>
    public sealed class IndependentNetworkDef : Def, INetworkDefinition
    {
        private NetworkDeclaration _declaration;
        public string Fault { get; private set; }
        internal Action ObserveRate;
        public ILineDefinition Line => Fault == "line-null" ? null : Fault == "foreign-line" ? new LineDef() : _declaration.Line;
        public ICapacityPolicyDefinition CapacityPolicy => Fault == "capacity-null" ? null : _declaration.CapacityPolicy;
        public bool Public => Fault == "public-throw" ? throw new ContentException(Id + ": invalid proof access metadata") : _declaration.Public;
        public IReadOnlyList<IResourceRate> Rates => Fault == "rates-null" ? null : Fault == "rates-empty" ? Array.Empty<IResourceRate>()
            : Fault == "rates-duplicate" ? Array.AsReadOnly(new[] { _declaration.Rates[0], _declaration.Rates[0] }) : _declaration.Rates;
        public IReadOnlyList<ResourceId> Resources => Fault == "resources-null" ? null : Fault == "resources-empty" ? Array.Empty<ResourceId>()
            : Fault == "resources-duplicate" ? Array.AsReadOnly(new[] { _declaration.Resources[0], _declaration.Resources[0] }) : _declaration.Resources;
        public INetworkFlowPolicy FlowPolicy => Fault == "flow-null" ? null : _declaration.FlowPolicy;
        public INetworkSolver Solver => Fault == "solver-null" ? null : _declaration.Solver;
        public INetworkDemandPolicy DemandPolicy => Fault == "demand-null" ? null : _declaration.DemandPolicy;
        protected override void ReadCore(JsonReader reader)
        { Fault = reader.String("metadata_fault"); _declaration = NetworkDeclaration.Read(reader); }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public bool Carries(ResourceId resource) => Fault == "carries-extra" || _declaration.Carries(resource);
        public IResourceRate Rate(ResourceId resource)
        {
            ObserveRate?.Invoke();
            var rate = _declaration.Rate(resource);
            if (Fault == "rate-extra" && rate == null) return _declaration.Rates[0];
            if (Fault == "rate-copy" && rate != null) return new ResourceRate(rate.Resource, rate.Quantity, rate.PeriodNumerator, rate.PeriodDenominator, rate.PeriodUnit);
            return rate;
        }
        // Public-contract compilation proof: copy actual detached facts, never calculate or own flow state.
        public static NetworkFlowSnapshot CopyFlowFact(NetworkFlowSnapshot value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return new NetworkFlowSnapshot(value.Entity, value.Resource, value.Pull, value.Source, value.Destination,
                value.ElapsedTicks, value.LastTick, value.NextAllowance, value.Blocker);
        }
    }
    // Metadata/fault instrumentation, never a network graph, transfer algorithm or gameplay scheduler.
    public sealed class NetworkDefinitionContractsModule : GameModule
    {
        public override string Id => "proof.network-definition-contracts";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(InfrastructureModule.ModuleId, Version) };
        public bool ActualContract { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<IndependentNetworkDef>("independent_networks");
            types.Section<LimitedNetworkSolverDef>("limited_network_solvers");
        }
        public override void Compose(WorldComposer composer)
        {
            var network = composer.Content.Get<INetworkDefinition>("binding.main");
            ActualContract = network is IndependentNetworkDef
                && ReferenceEquals(network, composer.Content.Find<INetworkDefinition>(network.Id))
                && ReferenceEquals(network.CapacityPolicy, composer.Content.Get<ICapacityPolicyDefinition>("network.shared"));
            composer.AddSystem(new ProbeInstaller(), 307);
        }
        private sealed class ProbeInstaller : GameSystem
        {
            public override string Id => "proof.network-definition-faults";
            public override int StateSchemaVersion => 1;
            public override void Update(World world, TickInfo time) { }
            public override void Initialize(World world)
            {
                foreach (var network in world.Content.All<IndependentNetworkDef>())
                    if (network.Fault.StartsWith("live-", StringComparison.Ordinal))
                        network.ObserveRate = () =>
                        {
                            if (world.Clock.StepIndex == 0) return;
                            Entity source = null;
                            foreach (var entity in world.Entities.All) if (entity.Def.Id == "binding.source") { source = entity; break; }
                            if (network.Fault == "live-throw") throw new InvalidOperationException("proof.network-definition.rate");
                            if (network.Fault == "live-pause") ((GameClock)world.Clock).Pause();
                            if (network.Fault == "live-touch") source.Touch();
                            if (network.Fault == "live-private") source.Get<ExposurePrivateProbe>().Corrupt();
                            if (network.Fault == "live-private-throw")
                            { source.Get<ExposurePrivateProbe>().Corrupt(); throw new InvalidOperationException("proof.network-definition.original-after-mutation"); }
                            if (network.Fault == "live-pause-throw")
                            { ((GameClock)world.Clock).Pause(); throw new InvalidOperationException("proof.network-definition.original-after-mutation"); }
                        };
            }
        }
    }
}
