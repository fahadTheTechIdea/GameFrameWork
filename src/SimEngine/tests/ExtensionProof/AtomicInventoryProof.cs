using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent engine-side endpoint, not a native component subclass or separate stock/book implementation.
    public sealed class AtomicInventoryDef : ComponentDef<AtomicInventory>, IStorageDefinition
    {
        private StorageDeclaration _storage;
        public string Behavior { get; private set; }
        public string CargoReadFault { get; private set; }
        public string LifecycleRead { get; private set; }
        public string LifecycleFault { get; private set; }
        public string LifecycleCommitFault { get; private set; }
        public string TransferReceiveFault { get; private set; }
        public ResolvedContent Content => _storage.Content;
        public ICapacityPolicy CapacityPolicy => _storage.CapacityPolicy;
        public bool Public => _storage.Public;
        public bool Evacuate => _storage.Evacuate;
        public Bill InitialStock => _storage.InitialStock;
        public IReadOnlyList<ResourceLotAmount> InitialLots => _storage.InitialLots;
        public bool Accepts(ResolvedContent content, ResourceId resource) => _storage.Accepts(content, resource);
        protected override void ReadCore(JsonReader reader)
        {
            Behavior = reader.String("conversion_behavior");
            CargoReadFault = reader.String("cargo_read_fault", "none");
            LifecycleRead = reader.String("lifecycle_read", "none");
            LifecycleFault = reader.String("lifecycle_fault", "none");
            LifecycleCommitFault = reader.String("lifecycle_commit_fault", "none");
            TransferReceiveFault = reader.String("transfer_receive_fault", "none");
            if (TransferReceiveFault != "none" && TransferReceiveFault != "second-short" && TransferReceiveFault != "second-throw")
                throw reader.Error("unknown transfer receive fault probe");
            if (LifecycleCommitFault != "none" && LifecycleCommitFault != "clock" && LifecycleCommitFault != "noop" &&
                LifecycleCommitFault != "short" && LifecycleCommitFault != "throw-before" && LifecycleCommitFault != "throw-after" &&
                LifecycleCommitFault != "promises" && LifecycleCommitFault != "amount-throw" && LifecycleCommitFault != "stock-clock" &&
                LifecycleCommitFault != "reservations-throw" && LifecycleCommitFault != "late-amount-throw") throw reader.Error("unknown lifecycle commit probe");
            if (LifecycleRead != "none" && LifecycleRead != "accepts" && LifecycleRead != "capacity" &&
                LifecycleRead != "available" && LifecycleRead != "incoming" && LifecycleRead != "amount") throw reader.Error("unknown lifecycle read probe");
            if (LifecycleFault != "none" && LifecycleFault != "private" && LifecycleFault != "clock" && LifecycleFault != "throw" &&
                LifecycleFault != "negative-amount" && LifecycleFault != "late-negative-amount" && LifecycleFault != "late-fractional-amount")
                throw reader.Error("unknown lifecycle fault probe");
            if ((LifecycleFault == "negative-amount" || LifecycleFault == "late-negative-amount" || LifecycleFault == "late-fractional-amount") &&
                LifecycleRead != "amount") throw reader.Error("invalid quantity probe requires an amount read");
            if ((LifecycleRead == "none") != (LifecycleFault == "none")) throw reader.Error("lifecycle probe requires both read and fault");
            if (CargoReadFault != "none" && CargoReadFault != "null" && CargoReadFault != "duplicate" &&
                CargoReadFault != "unknown" && CargoReadFault != "amount" && CargoReadFault != "throw" &&
                CargoReadFault != "mutate" && CargoReadFault != "clock" &&
                CargoReadFault != "fractional-total" && CargoReadFault != "fractional-partition")
                throw reader.Error("unknown authored cargo read fault");
            if (Behavior != "healthy" && Behavior != "unknown-status" && Behavior != "mutate-check" && Behavior != "clock-check" &&
                Behavior != "throw-check" && Behavior != "noop-commit" && Behavior != "extra-stock" && Behavior != "clock-commit" && Behavior != "throw-commit")
                throw reader.Error("explicit supported conversion_behavior required");
            _storage = StorageDeclaration.Read(reader);
        }
        protected override void ResolveCore(ContentResolver content) => _storage.Resolve(content);
    }
    public sealed class AtomicInventory : Component<AtomicInventoryDef>, IStorage, IResourceConversionEndpoint, IEntityInitialization
    {
        private ResourceInventoryState _state;
        private World _world;
        private int _lifecycleMarker;
        private bool _lifecycleCommitted; // Transient fault-probe phase only; never simulation progress.
        public int LifecycleCommitCalls { get; private set; }
        public int PostCommitAmountReads { get; private set; }
        private bool CommitProbe => _world?.Clock.Paused == true && Def.LifecycleCommitFault != "none";
        // Explicit per-instance protocol instrumentation, never authored gameplay or saved state.
        public Action BeforeLifecycleCommit, AfterLifecycleCommit;
        private Fix CommitLifecycle(ResourceId resource, Fix amount, bool adding)
        {
            BeforeLifecycleCommit?.Invoke();
            if (!CommitProbe)
            {
                Fix actual = adding ? State.Add(resource, amount) : State.Remove(resource, amount);
                AfterLifecycleCommit?.Invoke();
                return actual;
            }
            LifecycleCommitCalls++;
            if (Def.LifecycleCommitFault == "throw-before") throw new InvalidOperationException("proof.original-lifecycle-commit-before");
            if (Def.LifecycleCommitFault == "noop") return amount;
            Fix changed = adding ? State.Add(resource, amount) : State.Remove(resource, amount);
            _lifecycleCommitted = true;
            PostCommitAmountReads = 0;
            if (Def.LifecycleCommitFault == "clock") ((GameClock)_world.Clock).Speed = 2;
            if (Def.LifecycleCommitFault == "throw-after") throw new InvalidOperationException("proof.original-lifecycle-commit-after");
            if (Def.LifecycleCommitFault == "promises") State.ReserveOut(new ReservationOwner("proof.unrelated-commit", Entity.Id.Value), resource, Fix.One);
            return Def.LifecycleCommitFault == "short" ? changed / Fix.FromInt(2) : changed;
        }
        public int LifecycleCalls { get; private set; } // Transient callback diagnostics, not simulation state.
        private void ReadLifecycle(string operation)
        {
            if (_world?.Clock.Paused != true || Def.LifecycleRead != operation) return;
            LifecycleCalls++;
            if (Def.LifecycleFault == "private") _lifecycleMarker++; // Deliberately no Entity.Touch.
            if (Def.LifecycleFault == "clock") ((GameClock)_world.Clock).Speed = 2;
            if (Def.LifecycleFault == "throw") throw new InvalidOperationException("proof.original-lifecycle-read-error");
        }
        public ResourceEndpointId Endpoint => ResourceEndpointId.ForEntity(Entity.Id);
        public void WriteResourceState(IStateSync writer) => State.WriteResourceState(writer);
        public PlayerId Company => Entity.Owner;
        public int Version => Entity.Version;
        private ResourceInventoryState State => _state ?? (_state = new ResourceInventoryState(Endpoint, () => Company, () => Version,
            Entity.Touch, Def.Content, () => Def.CapacityPolicy, resource => Def.Accepts(Def.Content, resource), Def.Public, Def.Evacuate));
        internal void BindSession(World world) { _world = world; }
        public void Initialize(World world) { BindSession(world); ResourceTransactions.InitializeStock(world, Entity); }
        public Code CheckRemoval(World world) => ResourceTransactions.CheckInventoryRemoval(this);
        public IReadOnlyList<ResourceAmount> Stock
        {
            get
            {
                if (CommitProbe && _lifecycleCommitted && Def.LifecycleCommitFault == "stock-clock") ((GameClock)_world.Clock).Speed = 2;
                if (_world?.Clock.Paused == true && Def.CargoReadFault != "none")
                {
                    var stock = State.Stock;
                    if (Def.CargoReadFault == "null") return null;
                    if (Def.CargoReadFault == "duplicate") return new[] { stock[0], stock[0] };
                    if (Def.CargoReadFault == "unknown") return new[] { new ResourceAmount(new ResourceId(int.MaxValue), Fix.One) };
                    if (Def.CargoReadFault == "amount") return new[] { new ResourceAmount(stock[0].Resource, stock[0].Amount + Fix.One) };
                    if (Def.CargoReadFault == "fractional-total") return new[] { new ResourceAmount(stock[0].Resource, stock[0].Amount - Fix.One / Fix.FromInt(2)) };
                    if (Def.CargoReadFault == "throw") throw new InvalidOperationException("proof.original-cargo-read-error");
                    if (Def.CargoReadFault == "mutate") State.Take(stock[0].Resource, Fix.One);
                    if (Def.CargoReadFault == "clock") ((GameClock)_world.Clock).Speed = 2;
                }
                return State.Stock;
            }
        }
        public IReadOnlyList<ResourceAmount> InboundPromises => State.InboundPromises;
        public IReadOnlyList<ResourceAmount> OutboundPromises => State.OutboundPromises;
        public IReadOnlyList<ResourceReservation> Reservations
        {
            get {
                if (CommitProbe && _lifecycleCommitted && Def.LifecycleCommitFault == "reservations-throw")
                    throw new InvalidOperationException("proof.original-lifecycle-reservations");
                return State.Reservations;
            }
        }
        public ICapacityPolicy CapacityPolicy { get { ReadLifecycle("capacity"); return State.CapacityPolicy; } }
        public bool Public => State.Public;
        public bool Evacuate => State.Evacuate;
        public bool Accepts(ResolvedContent content, ResourceId resource) { ReadLifecycle("accepts"); return State.Accepts(content, resource); }
        public CapacityCheck CheckIncoming(IReadOnlyList<ResourceAmount> incoming) { ReadLifecycle("incoming"); return State.CheckIncoming(incoming); }
        public Fix Offer(ResourceId resource, Fix ceiling) => State.Offer(resource, ceiling);
        public Fix Receive(ResolvedContent content, ResourceId resource, Fix amount) => State.Receive(content, resource, amount);
        public Fix Take(ResourceId resource, Fix amount) => State.Take(resource, amount);
        public Fix Amount(ResourceId resource)
        {
            ReadLifecycle("amount");
            if (_world?.Clock.Paused == true && Def.CargoReadFault == "fractional-total") return State.Amount(resource) - Fix.One / Fix.FromInt(2);
            if (_world?.Clock.Paused == true && Def.LifecycleRead == "amount" && Def.LifecycleFault == "negative-amount") return -Fix.One;
            if (_world?.Clock.Paused == true && LifecycleCalls > 1 && Def.LifecycleRead == "amount")
            {
                if (Def.LifecycleFault == "late-negative-amount") return -Fix.One;
                if (Def.LifecycleFault == "late-fractional-amount") return State.Amount(resource) - Fix.One / Fix.FromInt(2);
            }
            if (CommitProbe && _lifecycleCommitted) {
                PostCommitAmountReads++;
                if (Def.LifecycleCommitFault == "late-amount-throw" && PostCommitAmountReads > 1)
                    throw new InvalidOperationException("proof.unverified-publication-read");
            }
            if (CommitProbe && _lifecycleCommitted && Def.LifecycleCommitFault == "amount-throw")
                throw new InvalidOperationException("proof.original-lifecycle-amount");
            return State.Amount(resource);
        }
        public Fix ReservedIn(ResourceId resource) => State.ReservedIn(resource);
        public Fix ReservedOut(ResourceId resource) => _world?.Clock.Paused == true && Def.CargoReadFault == "fractional-partition"
            ? State.ReservedOut(resource) + Fix.One / Fix.FromInt(2) : State.ReservedOut(resource);
        public Fix Available(ResourceId resource)
        {
            ReadLifecycle("available");
            return _world?.Clock.Paused == true && (Def.CargoReadFault == "fractional-total" || Def.CargoReadFault == "fractional-partition")
                ? State.Available(resource) - Fix.One / Fix.FromInt(2) : State.Available(resource);
        }
        public bool Holds(Bill bill) => State.Holds(bill);
        public bool HoldsUnpromised(Bill bill) => State.HoldsUnpromised(bill);
        public Fix Add(ResourceId resource, Fix amount) => CommitLifecycle(resource, amount, true);
        public Fix Remove(ResourceId resource, Fix amount) => CommitLifecycle(resource, amount, false);
        public bool CanReserveIn(ReservationOwner owner, ResourceId resource, Fix amount) => State.CanReserveIn(owner, resource, amount);
        public ReservationToken ReserveIn(ReservationOwner owner, ResourceId resource, Fix amount) => State.ReserveIn(owner, resource, amount);
        public bool CanReserveOut(ReservationOwner owner, ResourceId resource, Fix amount) => State.CanReserveOut(owner, resource, amount);
        public ReservationToken ReserveOut(ReservationOwner owner, ResourceId resource, Fix amount) => State.ReserveOut(owner, resource, amount);
        public void ReleaseIn(ReservationOwner owner, ReservationToken token) => State.ReleaseIn(owner, token);
        public void ReleaseOut(ReservationOwner owner, ReservationToken token) => State.ReleaseOut(owner, token);
        public ResourceReceiptCheck CheckReservedIn(ResolvedContent content, ReservationOwner owner, ReservationToken token) => State.CheckReservedIn(content, owner, token);
        public ResourceReceiptCheck CheckReservedOut(ReservationOwner owner, ReservationToken token) => State.CheckReservedOut(owner, token);
        private int _transferReceives; // Negative callback diagnostics only, not simulation state.
        public Fix ReceiveReserved(ResolvedContent content, ReservationOwner owner, ReservationToken token, Fix amount)
        {
            Fix actual=State.ReceiveReserved(content,owner,token,amount);
            if (Def.TransferReceiveFault != "none" && ++_transferReceives == 2)
            {
                if (Def.TransferReceiveFault == "second-throw") throw new InvalidOperationException("authored second receive failure");
                return actual / Fix.FromInt(2);
            }
            return actual;
        }
        public Fix TakeReserved(ReservationOwner owner, ReservationToken token, Fix amount) => State.TakeReserved(owner, token, amount);
        public void IncreaseReservedOut(ReservationOwner owner, ReservationToken token, Fix amount) => State.IncreaseReservedOut(owner, token, amount);
        public override void Sync(IStateSync sync) { State.Sync(sync); sync.Int(ref _lifecycleMarker); }
        public CapacityCheck CheckConversion(IReadOnlyList<ResourceAmount> consumed, IReadOnlyList<ResourceAmount> produced)
        {
            if (Def.Behavior == "unknown-status") return new CapacityCheck((CapacityStatus)255);
            if (Def.Behavior == "mutate-check") State.Take(consumed[0].Resource, Fix.One);
            if (Def.Behavior == "clock-check") ((GameClock)_world.Clock).Pause();
            if (Def.Behavior == "throw-check") throw new InvalidOperationException("authored atomic capacity callback failure");
            return State.CheckConversion(consumed, produced);
        }
        public void ConvertReserved(ReservationOwner owner, IReadOnlyList<ResourceConversionInput> inputs, Bill produced, IReadOnlyList<ResourceLotAmount> lots = null)
        {
            if (Def.Behavior == "noop-commit") return;
            if (Def.Behavior == "throw-commit") throw new InvalidOperationException("authored atomic commit callback failure");
            State.ConvertReserved(owner, inputs, produced, lots);
            if (Def.Behavior == "extra-stock") State.Take(inputs[0].Receipt.Resource, Fix.One);
            if (Def.Behavior == "clock-commit") ((GameClock)_world.Clock).Pause();
        }
    }
}

