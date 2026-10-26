using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Independent stateless declared demand; its existing inventory owns all actual stock.</summary>
    public sealed class ProofUtilityConsumerDef : ComponentDef<ProofUtilityConsumer>, IResourcePortDefinition
    {
        public bool BindsEntityInventory => true;
        public ICapacityPolicy CapacityPolicy => Owner.Component<IInventoryDefinition>().CapacityPolicy;
        public bool Accepts(ResolvedContent content, ResourceId resource)
            => Inputs.AmountOf(resource).IsPositive && Owner.Component<IInventoryDefinition>().Accepts(content, resource);
        private BillSpec _inputs;
        public Code CapabilityId { get; private set; }
        public Bill Inputs { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            CapabilityId = new Code(reader.String("id")); if (CapabilityId.IsNone) throw reader.Error("explicit utility identity required");
            _inputs = BillSpec.Read(reader, "inputs"); if (_inputs.IsEmpty) throw reader.Error("explicit utility demand required");
        }
        protected override void ResolveCore(ContentResolver content) => Inputs = _inputs.Resolve(content);
        protected override void ValidateCore(EntityDef owner)
        { if (owner.Component<IInventoryDefinition>() == null) throw new ContentException("proof utility requires an actual inventory"); }
    }
    public sealed class ProofUtilityConsumer : Component<ProofUtilityConsumerDef>, IUtilityConsumer, IResourcePort
    {
        public int Fault; // Observation-only fault injection, not gameplay/save state.
        public Code CapabilityId => Def.CapabilityId;
        public ResourcePortResolution ResolveStore(World world, ResourceEndpointId? original = null)
            => original.HasValue && original.Value != ResourceEndpointId.ForEntity(Entity.Id)
                ? ResourcePortResolution.Unavailable(ResourcePortCodes.OriginalUnavailable):ResourcePortResolution.ReadyStore(Entity.Get<IInventory>());
        public IResourcePort ResolveUtilityPort(World world, ResourceId resource) => Def.Inputs.AmountOf(resource).IsPositive ? this : null;
        public UtilityDemandSnapshot ReadUtilityDemand(World world)
        {
            if (Fault == 1) Entity.Touch();
            var ports = new List<ResourcePortSnapshot>(); var available = new List<ResourceAmount>(); var store = (IResourceOutlet)ResolveStore(world).Store;
            foreach (ResourceAmount line in Def.Inputs.Lines)
            {
                Fix held = store.Amount(line.Resource);
                ports.Add(new ResourcePortSnapshot(line.Resource, CapabilityId, store.Endpoint, store.Version,
                    held.IsPositive ? new Bill(new[] { new ResourceAmount(line.Resource, held) }) : Bill.Empty,Code.None));
                Fix contribution = Fault == 2 ? Fix.Zero : Fix.Min(line.Amount, store.Available(line.Resource));
                if (contribution.IsPositive) available.Add(new ResourceAmount(line.Resource, contribution));
            }
            return new UtilityDemandSnapshot(Fault == 3 ? new Code("wrong") : CapabilityId,
                Fault == 4 ? checked(world.Tick + 2) : 0, ports, true, Bill.Empty, Def.Inputs, new Bill(available),
                Fault == 5 ? new Code("proof.utility-range") : Code.None);
        }
    }
    public sealed class UtilityConsumerProofModule : GameModule
    {
        public override string Id => "proof.utility-consumer";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(InventoryModule.ModuleId, Version), new ModuleRequirement(UtilitySignalsModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types) => types.Component<ProofUtilityConsumerDef>("proof_utility_consumer", 1);
    }
}
