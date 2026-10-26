using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Diagnostic instrumentation only; every operation delegates to the original actual endpoint.
    public sealed class InventoryCapacityQueryDef : ComponentDef<InventoryCapacityQuery>
    {
        private string _resource, _policy;
        public string Operation { get; private set; }
        public Fix Quantity { get; private set; }
        public ResourceId Resource { get; private set; }
        public TopologyCapacityDef Policy { get; private set; }
        protected override void ReadCore(JsonReader r)
        {
            Operation = r.String("operation"); _resource = r.String("resource"); _policy = r.String("policy"); Quantity = r.Fix("quantity");
            if (!Quantity.IsPositive || Operation != "incoming" && Operation != "outgoing" && Operation != "conversion" && Operation != "offer")
                throw r.Error("explicit original endpoint query and positive quantity required");
        }
        protected override void ResolveCore(ContentResolver c) { Resource = c.Resource(_resource); Policy = c.Def<TopologyCapacityDef>(_policy); }
        protected override void ValidateCore(EntityDef owner)
        {
            if (!ReferenceEquals(owner.Component<IStorageDefinition>()?.CapacityPolicy, Policy))
                throw new ContentException("inventory query requires its actual storage capacity binding");
        }
    }
    public sealed class InventoryCapacityQuery : Component<InventoryCapacityQueryDef>, IInspectable
    {
        public void Inspect(World world, PlayerId viewer, IList<Facet> into)
        {
            if (!world.Clock.Paused || !Def.Policy.HasFault || viewer.IsValid && viewer != Entity.Owner) return;
            IStorage stock = Entity.Get<IStorage>();
            var vector = new[] { new ResourceAmount(Def.Resource, Def.Quantity) };
            Def.Policy.Query(true);
            try
            {
                if (Def.Operation == "incoming") stock.CheckIncoming(vector);
                if (Def.Operation == "outgoing") stock.CanReserveOut(new ReservationOwner("proof.inventory-query", Entity.Id.Value), Def.Resource, Def.Quantity);
                if (Def.Operation == "conversion") ((IResourceConversionEndpoint)stock).CheckConversion(vector, vector);
                if (Def.Operation == "offer") stock.Offer(Def.Resource, Def.Quantity);
            }
            finally { Def.Policy.Query(false); }
        }
    }
    public sealed class InventoryCapacityProofModule : GameModule
    {
        public const string ModuleId = "proof.inventory-capacity";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires =>
            new[] { new ModuleRequirement(InventoryModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes t)
        {
            t.Section<TopologyCapacityDef>("inventory_capacity_policies");
            t.Component<InventoryCapacityQueryDef>("inventory_capacity_query", 1);
        }
        public override void Compose(WorldComposer c)
        {
            foreach (var policy in c.Content.All<TopologyCapacityDef>()) policy.BindNegativeSession(c.World);
        }
    }
}
