using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Engine-side negative callback probe; no driver access to session internals or balances.
    public sealed class TopologyCapacityDef : Def, ICapacityPolicyDefinition
    {
        private readonly CapacityPolicyDeclaration _declaration = new CapacityPolicyDeclaration();
        private string _fault;
        private bool _queryOnly, _query;
        internal bool HasFault => _fault != "none";
        internal World Session;
        public IReadOnlyList<CapacityLimit> Limits => _declaration.Limits;
        protected override void ReadCore(JsonReader r) { _declaration.Read(r); _fault = r.String("fault"); _queryOnly = r.Bool("query_only", false); }
        protected override void ResolveCore(ContentResolver c) => _declaration.Resolve(c);
        internal void Query(bool active) { _query = active; }
        internal void BindNegativeSession(World world)
        {
            if (!HasFault) return;
            if (Session != null) throw new ContentException("negative capacity probe requires its own single-session factory");
            Session = world;
        }
        private bool Active(string phase) => Session?.Clock.Paused == true && (!_queryOnly || _query) && _fault.EndsWith("-" + phase, StringComparison.Ordinal);
        public CapacityCheck Check(IReadOnlyList<ResourceAmount> stock, IReadOnlyList<ResourceAmount> promised,
            IReadOnlyList<ResourceAmount> incoming, long instances = 1)
        {
            if (Active("check"))
            {
                if (_fault == "unknown-check") return new CapacityCheck((CapacityStatus)255);
                if (_fault == "throw-check") throw new InvalidOperationException("proof.original-capacity-check-error");
                if (_fault == "clock-check") ((GameClock)Session.Clock).Resume();
                if (_fault == "mutate-stock-check" && stock.Count > 0) ((IList<ResourceAmount>)stock).Clear();
                if (_fault == "mutate-projected-check" && incoming.Count > 0 && incoming[0].Amount.IsPositive) ((IList<ResourceAmount>)stock).Add(new ResourceAmount(incoming[0].Resource, Fix.One));
                if (_fault == "mutate-incoming-check" && incoming.Count > 0 && incoming[0].Amount.IsPositive) ((IList<ResourceAmount>)incoming)[0] = new ResourceAmount(incoming[0].Resource, Fix.Zero);
            }
            return _declaration.Check(stock, promised, incoming, instances, _ => CapacityStatus.Fits);
        }
        public CapacityQuantum Quantum(ResourceId resource)
        {
            if (Active("quantum"))
            {
                if (_fault == "throw-quantum") throw new InvalidOperationException("proof.original-capacity-quantum-error");
                if (_fault == "clock-quantum") ((GameClock)Session.Clock).Resume();
                if (_fault == "zero-quantum") return new CapacityQuantum(CapacityStatus.Fits, 0);
                if (_fault == "unknown-quantum") return new CapacityQuantum((CapacityStatus)255);
                if (_fault == "failure-raw-quantum") return new CapacityQuantum(CapacityStatus.ResourceRejected, Fix.One.Raw);
            }
            return _declaration.Quantum(this, resource);
        }
        public CapacityOffer Offer(IReadOnlyList<ResourceAmount> stock, IReadOnlyList<ResourceAmount> promised,
            ResourceId resource, Fix ceiling, long instances = 1)
        {
            if (Active("offer"))
            {
                if (_fault == "throw-offer") throw new InvalidOperationException("proof.original-capacity-offer-error");
                if (_fault == "clock-offer") ((GameClock)Session.Clock).Resume();
                if (_fault == "mutate-offer") ((IList<ResourceAmount>)stock).Clear();
                if (_fault == "over-offer") return new CapacityOffer(CapacityStatus.Fits, ceiling + Fix.One);
                if (_fault == "fractional-offer") return new CapacityOffer(CapacityStatus.Fits, Fix.FromRatio(3, 2));
                if (_fault == "refusal-offer") return new CapacityOffer(CapacityStatus.LimitExceeded, Fix.One);
                if (_fault == "unknown-offer") return new CapacityOffer((CapacityStatus)255);
                if (_fault == "negative-offer") return new CapacityOffer(CapacityStatus.Fits, -Fix.One);
                if (_fault == "inconsistent-offer") return new CapacityOffer(CapacityStatus.Fits, ceiling);
            }
            return _declaration.Offer(this, stock, promised, resource, ceiling, instances);
        }
    }
    public sealed class TopologyCapacityProofModule : GameModule
    {
        public const string ModuleId = "proof.topology-capacity";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires =>
            new[] { new ModuleRequirement(InfrastructureModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes t) => t.Section<TopologyCapacityDef>("topology_capacity_policies");
        public override void Compose(WorldComposer c)
        {
            foreach (var policy in c.Content.All<TopologyCapacityDef>())
                policy.BindNegativeSession(c.World);
        }
    }
}
