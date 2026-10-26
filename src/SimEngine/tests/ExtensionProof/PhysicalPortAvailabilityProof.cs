using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Incoming-only capability over the existing ResourceInventoryState stock/book/codec.
    public sealed class ReceivingOnlyDef : ComponentDef<ReceivingOnly>, IInletPortDefinition
    {
        private string _policy, _resource;
        public ICapacityPolicy Policy { get; private set; }
        public ResourceId Resource { get; private set; }
        public ResolvedContent Content { get; private set; }
        public bool Public { get; private set; }
        protected override void ReadCore(JsonReader reader) { _policy = reader.String("capacity_policy"); _resource = reader.String("resource"); Public = reader.Bool("public"); }
        protected override void ResolveCore(ContentResolver content) { Content = content.Content; Policy = content.Def<ICapacityPolicyDefinition>(_policy); Resource = content.Resource(_resource); }
        public bool Accepts(ResolvedContent content, ResourceId resource) => ReferenceEquals(content, Content) && resource == Resource;
    }
    public sealed class ReceivingOnly : Component<ReceivingOnlyDef>, IInletPort
    {
        private ResourceInventoryState _state;
        private ResourceInventoryState State => _state ?? (_state = new ResourceInventoryState(Endpoint, () => Company, () => Version,
            Entity.Touch, Def.Content, () => Def.Policy, resource => Def.Accepts(Def.Content, resource), Def.Public, false));
        public ResourceEndpointId Endpoint => ResourceEndpointId.ForEntity(Entity.Id);
        public void WriteResourceState(IStateSync writer) => State.WriteResourceState(writer);
        public PlayerId Company => Entity.Owner;
        public int Version => Entity.Version;
        public bool Public => Def.Public;
        public ICapacityPolicy CapacityPolicy => State.CapacityPolicy;
        public IReadOnlyList<ResourceAmount> InboundPromises => State.InboundPromises;
        public IReadOnlyList<ResourceReservation> Reservations => State.Reservations;
        public Fix Amount(ResourceId resource) => State.Amount(resource);
        public Fix ReservedIn(ResourceId resource) => State.ReservedIn(resource);
        public bool Accepts(ResolvedContent content, ResourceId resource) => State.Accepts(content, resource);
        public Fix Offer(ResourceId resource, Fix ceiling) => State.Offer(resource, ceiling);
        public CapacityCheck CheckIncoming(IReadOnlyList<ResourceAmount> incoming) => State.CheckIncoming(incoming);
        public bool CanReserveIn(ReservationOwner owner, ResourceId resource, Fix amount) => State.CanReserveIn(owner, resource, amount);
        public ReservationToken ReserveIn(ReservationOwner owner, ResourceId resource, Fix amount) => State.ReserveIn(owner, resource, amount);
        public void ReleaseIn(ReservationOwner owner, ReservationToken token) => State.ReleaseIn(owner, token);
        public ResourceReceiptCheck CheckReservedIn(ResolvedContent content, ReservationOwner owner, ReservationToken token) => State.CheckReservedIn(content, owner, token);
        public Fix ReceiveReserved(ResolvedContent content, ReservationOwner owner, ReservationToken token, Fix amount) => State.ReceiveReserved(content, owner, token, amount);
        public Fix Receive(ResolvedContent content, ResourceId resource, Fix amount) => State.Receive(content, resource, amount);
        public Code CheckRemoval(World world) => State.Reservations.Count != 0 ? ResourceTransactions.ActivePromises : State.Stock.Count != 0 ? ResourceTransactions.UndisposedStock : Code.None;
        public override void Sync(IStateSync sync) => State.Sync(sync);
    }
    // Authored hostile or explicitly unavailable named binding, no additional live stock owner.
    public sealed class PhysicalAvailabilityProbeDef : ComponentDef<PhysicalAvailabilityProbe>, IResourcePortDefinition
    {
        public bool BindsEntityInventory => false;
        private string _pool;
        public string Mode { get; private set; }
        public EndpointPoolDef Pool { get; private set; }
        public Code CapabilityId { get; private set; }
        public ICapacityPolicy CapacityPolicy => Pool.Policy;
        protected override void ReadCore(JsonReader reader) {
            CapabilityId = new Code(reader.String("id")); _pool = reader.String("pool"); Mode = reader.String("mode");
            if (Mode != "unavailable" && Mode != "malformed" && Mode != "inlet-only" && Mode != "mutate" && Mode != "clock" && Mode != "throw") throw reader.Error("unknown physical availability probe");
        }
        protected override void ResolveCore(ContentResolver content) => Pool = content.Def<EndpointPoolDef>(_pool);
        public bool Accepts(ResolvedContent content, ResourceId resource) => Pool.Accepts(resource);
    }
    public sealed class PhysicalAvailabilityProbe : Component<PhysicalAvailabilityProbeDef>, IResourcePort
    {
        public static readonly Code Closed = new Code("proof.physical-port-closed");
        public Code CapabilityId => Def.CapabilityId;
        public ResourcePortResolution ResolveStore(World world, ResourceEndpointId? original = null) {
            if (Def.Mode == "unavailable") return ResourcePortResolution.Unavailable(Closed);
            if (Def.Mode == "malformed") return default;
            var state = world.Services.Get<EndpointPools>().Get(Def.Pool.Identity);
            if (Def.Mode == "inlet-only") return ResourcePortResolution.ReadyStore(new ClaimInlet(state));
            if (Def.Mode == "mutate") Entity.Touch();
            if (Def.Mode == "clock") ((Hosting.GameClock)world.Clock).Pause();
            if (Def.Mode == "throw") throw new InvalidOperationException("proof.physical-port-original-error");
            return ResourcePortResolution.ReadyStore(state);
        }
    }
}

