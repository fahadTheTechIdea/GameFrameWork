using System.Linq;
using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent immutable content. Positive behavior delegates to the engine's sole algorithms.
    public sealed class IndependentNetworkRankingDef : Def, INetworkFlowPolicy
    {
        public string Fault { get; private set; }
        internal Action Probe;
        protected override void ReadCore(JsonReader reader) => Fault = reader.String("fault");
        public long Rank(NetworkFlowCandidate candidate, EntityId lastServed)
        { Probe?.Invoke(); return RoundRobinNetworkRanking.Rank(candidate, lastServed); }
    }
    public sealed class IndependentNetworkDemandDef : Def, INetworkDemandPolicy
    {
        public string Fault { get; private set; }
        internal Action Probe;
        internal NetworkDemandContext Observed;
        protected override void ReadCore(JsonReader reader) => Fault = reader.String("fault");
        public NetworkDemandSnapshot Read(NetworkDemandContext context)
        { Observed = context; Probe?.Invoke(); return CapacityNetworkDemand.Read(context); }
    }
    public sealed class IndependentNetworkTransferDef : Def, INetworkSolver
    {
        public string Fault { get; private set; }
        internal Action Probe;
        internal NetworkExecutionContext Observed;
        internal Action<ISimulationTime> TimeProbe;
        protected override void ReadCore(JsonReader reader) => Fault = reader.String("fault");
        public void ValidateTime(ISimulationTime time)
        {
            ResourceTransferNetworkSolver.ValidateTime(time);
            if (Fault == "invalid-time") throw new ContentException(Id + ": explicit invalid proof time");
            TimeProbe?.Invoke(time);
        }
        public void Advance(NetworkExecutionContext context)
        { Observed = context; Probe?.Invoke(); ResourceTransferNetworkSolver.Advance(context); }
    }
    public sealed class NetworkPolicyContractsModule : GameModule
    {
        public override string Id => "proof.network-policy-contracts";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(InfrastructureModule.ModuleId, Version) };
        public bool ActualContracts { get; private set; }
        public NetworkDemandContext DemandContext { get; private set; }
        public NetworkExecutionContext ExecutionContext { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<IndependentNetworkRankingDef>("independent_network_rankings");
            types.Section<IndependentNetworkDemandDef>("independent_network_demands");
            types.Section<IndependentNetworkTransferDef>("independent_network_transfers");
        }
        public override void Compose(WorldComposer composer)
        {
            var network = composer.Content.Get<INetworkDefinition>("binding.main");
            ActualContracts = network.FlowPolicy is IndependentNetworkRankingDef
                && network.DemandPolicy is IndependentNetworkDemandDef && network.Solver is IndependentNetworkTransferDef;
            // Negative instrumentation only; never computes normal gameplay or mutates proof-owned balances.
            foreach (var solver in composer.Content.All<IndependentNetworkTransferDef>())
                if (solver.Fault == "validate-pause") solver.TimeProbe = time => ((GameClock)composer.World.Clock).Pause();
            composer.AddSystem(new ProbeInstaller(this), 306);
        }
        private sealed class ProbeInstaller : GameSystem
        {
            private readonly NetworkPolicyContractsModule _module;
            internal ProbeInstaller(NetworkPolicyContractsModule module) { _module = module; }
            public override string Id => "proof.network-policy-faults";
            public override int StateSchemaVersion => 1;
            public override void Initialize(World world)
            {
                foreach (var policy in world.Content.All<IndependentNetworkRankingDef>()) policy.Probe = Fault(world, policy.Fault);
                foreach (var policy in world.Content.All<IndependentNetworkDemandDef>()) policy.Probe = Fault(world, policy.Fault);
                foreach (var solver in world.Content.All<IndependentNetworkTransferDef>()) solver.Probe = Fault(world, solver.Fault);
            }
            private static Action Fault(World world, string fault)
            {
                if (fault == "none" || fault == "invalid-time" || fault == "validate-pause") return null;
                return () =>
                {
                    if (world.Clock.StepIndex == 0) return;
                    Entity source = null, peer = null;
                    foreach (var entity in world.Entities.All)
                    { if (entity.Def.Id == "binding.source") source = entity; if (entity.Def.Id == "binding.watcher") peer = entity; }
                    if (fault == "throw") throw new InvalidOperationException("proof.network-policy");
                    if (fault == "pause") ((GameClock)world.Clock).Pause();
                    if (fault == "touch") source.Touch();
                    if (fault == "private") source.Get<ExposurePrivateProbe>().Corrupt();
                    if (fault == "peer-private") peer.Get<ExposurePrivateProbe>().Corrupt();
                    if (fault == "pool")
                        world.Services.Providers<NetworkService>().Single().Component("binding.main", 0).Inventory.Remove(world.Content.ResourceId("steel"), Fix.One);
                };
            }
            public override void Update(World world, TickInfo time) { }
            public override void CompleteUpdate(World world)
            {
                _module.DemandContext = (world.Content.Find<INetworkDemandPolicy>("policy.capacity") as IndependentNetworkDemandDef)?.Observed;
                _module.ExecutionContext = (world.Content.Find<INetworkSolver>("policy.transfer") as IndependentNetworkTransferDef)?.Observed;
            }
        }
    }
}
