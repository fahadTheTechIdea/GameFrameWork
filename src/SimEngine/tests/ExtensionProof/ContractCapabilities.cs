using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Independent one-dimensional vehicle behavior; the standard movement phase invokes it.</summary>
    public sealed class LaneMobilityDef : ComponentDef<LaneMobility>, IMobilityDefinition
    {
        public string MovementClass { get; private set; }
        public Fix TilesPerHour { get; private set; }
        public EntityStateBindings States { get; private set; }
        public IEntityStateDefinition MovingState => States["moving"];
        public IEntityStateDefinition StoppedState => States["stopped"];
        protected override void ReadCore(JsonReader r)
        {
            MovementClass = r.String("class"); TilesPerHour = r.Fix("speed");
            if (TilesPerHour <= Fix.Zero) throw r.Error("speed must be positive");
            States = EntityStateBindings.Read(r, "moving", "stopped");
        }
        protected override void ResolveCore(ContentResolver c)
        {
            States = States.Resolve(c);
        }
    }

    public sealed class LaneMobility : Component<LaneMobilityDef>, IMobility, IEntityStateSource
    {
        private MoveStatus _status;
        private TileRect _goal;
        public string MovementClass => Def.MovementClass;
        public MoveStatus Status => _status;
        public TileRect Goal => _goal;
        public PathSearchResult CanReach(World world, TileRect goal)
        {
            if (goal.Y > Entity.Get<ITilePresence>().Tile.Y || goal.Bottom <= Entity.Get<ITilePresence>().Tile.Y)
                return PathSearchResult.Unreachable;
            return world.Services.Get<NavService>().Grid(MovementClass).CanReach(Entity.Get<ITilePresence>().Tile, goal, actor: Entity);
        }
        public void GoTo(TileRect goal) { _goal = goal; _status = MoveStatus.Moving; }
        public void Stop() => _status = MoveStatus.Idle;
        public void Advance(World world, TickInfo tick, Fix speedModifier)
        {
            if (_status != MoveStatus.Moving || !Entity.Alive) return;
            if (CanReach(world, _goal) == PathSearchResult.Unreachable) { _status = MoveStatus.Unreachable; return; }
            ITilePresence presence = Entity.Get<ITilePresence>();
            int x = Math.Max(_goal.X, Math.Min(presence.Tile.X, _goal.Right - 1));
            var target = FixVec2.Centre(new TileCoord(x, presence.Tile.Y));
            presence.Move(world, FixVec2.MoveTowards(presence.Position, target,
                world.Time.RateFor(Def.TilesPerHour * speedModifier, DurationUnit.Hour)));
            if (_goal.Contains(presence.Tile)) _status = MoveStatus.Arrived;
        }
        public IEntityStateDefinition GetState(EntityUpdateContext context)
            => _status == MoveStatus.Moving ? Def.MovingState : _status == MoveStatus.Unreachable ? Def.StoppedState : null;
        public override void Sync(IStateSync s)
        {
            s.Enum(ref _status);
            int x = _goal.X, y = _goal.Y, w = _goal.W, h = _goal.H;
            s.Int(ref x); s.Int(ref y); s.Int(ref w); s.Int(ref h); _goal = new TileRect(x, y, w, h);
        }
    }

    /// <summary>Paid supply policy over the entity's one independently authored inventory.</summary>
    public sealed class FiniteSupplierDef : ComponentDef<FiniteSupplier>
    {
        private string _resource;
        public string ReceiptFault { get; private set; }
        public ResourceId Resource { get; private set; }
        public Fix UnitPrice { get; private set; }
        protected override void ReadCore(JsonReader r)
        {
            _resource = r.String("resource"); UnitPrice = r.Fix("price");
            ReceiptFault = r.String("receipt_fault", null);
            if (ReceiptFault != null && ReceiptFault != "no_stock_delta") throw r.Error("unknown finite supplier receipt fault");
            if (UnitPrice <= Fix.Zero) throw r.Error("price must be positive");
        }
        protected override void ResolveCore(ContentResolver c) => Resource = c.Resource(_resource);
        protected override void ValidateCore(EntityDef owner)
        {
            if (!owner.HasCapability<IInventory>()) throw new ContentException(owner.Id + ": finite supplier requires inventory");
        }
    }

    public sealed class FiniteSupplier : Component<FiniteSupplierDef>, ISupplier, IResourceSupplyPrice
    {
        // Explicit per-instance negative protocol instrumentation; never gameplay or saved state.
        public Action BeforeSupplyClaim, AfterSupplyClaim, BeforeSupplyRelease, AfterSupplyRelease;
        public ResourceEndpointId Endpoint => ResourceEndpointId.ForEntity(Entity.Id);
        public void WriteResourceState(IStateSync writer) => Stock.WriteResourceState(writer);
        public PlayerId Company => Entity.Owner;
        public int Version => Entity.Version;
        public Fix UnitPrice(ResourceId resource) => resource == Def.Resource ? Def.UnitPrice : Fix.Zero;
        public Code CheckRemoval(World world) => Reservations.Count != 0 ? ResourceTransactions.ActivePromises : Code.None;
        public ResourceSupplyKind SupplyKind => ResourceSupplyKind.ExistingStock;
        public IOutletPort SupplyStock => Stock;
        private IInventory Stock => Entity.Get<IInventory>();
        public IReadOnlyList<ResourceReservation> Reservations => Stock.Reservations;
        public CapacityQuantum SupplyQuantum(ResourceId resource) => resource == Def.Resource
            ? Stock.CapacityPolicy.Quantum(resource) : new CapacityQuantum(CapacityStatus.ResourceRejected);
        public Fix Offer(ResourceId resource, Fix requested)
            => SupplyQuantum(resource).Floor(Fix.Min(requested, Stock.Available(resource)));
        public bool CanReserveSupply(ReservationOwner owner, ResourceId resource, Fix amount)
            => resource == Def.Resource && Stock.CanReserveOut(owner, resource, amount);
        public ReservationToken ReserveSupply(ReservationOwner owner, ResourceId resource, Fix amount)
        {
            if (!CanReserveSupply(owner, resource, amount)) throw new InvalidOperationException("invalid finite supply promise");
            BeforeSupplyClaim?.Invoke();
            ReservationToken token = Stock.ReserveOut(owner, resource, amount);
            AfterSupplyClaim?.Invoke();
            return token;
        }
        public void ReleaseSupply(ReservationOwner owner, ReservationToken token)
        {
            BeforeSupplyRelease?.Invoke();
            Stock.ReleaseOut(owner, token);
            AfterSupplyRelease?.Invoke();
        }
        public ResourceReceiptCheck CheckSupply(World world, PlayerId buyer, ReservationOwner owner, ReservationToken token, Fix amount)
        {
            ResourceReceiptCheck check = Stock.CheckReservedOut(owner, token);
            if (!check.Ready) return check;
            if (check.Record.Resource != Def.Resource) return new ResourceReceiptCheck(ResourceTransferStatus.Incompatible, check.Record);
            if (!amount.IsPositive || amount > check.Record.Amount || SupplyQuantum(check.Record.Resource).Floor(amount) != amount)
                return new ResourceReceiptCheck(ResourceTransferStatus.InvalidReceipt, check.Record);
            return check;
        }
        public Fix SupplyReserved(World world, PlayerId buyer, ReservationOwner owner, ReservationToken token, Fix amount, ResourceSupplyExecution execution)
        {
            if (execution == null) throw new InvalidOperationException("finite fulfillment requires the transaction owner");
            ResourceReceiptCheck check = CheckSupply(world, buyer, owner, token, amount);
            if (!check.Ready) throw new InvalidOperationException("finite supply refused: " + check.Status);
            ResourceReservation record = check.Record;
            execution.RequireSupply(Endpoint, owner, token, amount);
            if (Def.ReceiptFault == "no_stock_delta")
            {
                // Deliberately invalid engine-side provider: consumes the receipt and touches the owner,
                // but lies about stock consumption. The transaction owner must reject its claimed success.
                Stock.ReleaseOut(owner, token); return amount;
            }
            Fix delivered = Stock.TakeReserved(owner, token, amount);
            if (delivered != amount) throw new InvalidOperationException("finite supply violated exact receipt");
            return delivered;
        }
    }

    // Invalid-content probe: a second authored inlet declaration with no second runtime port.
    public sealed class InletDeclarationDef : ComponentDef<InletDeclaration>, IInletPortDefinition
    {
        protected override void ReadCore(JsonReader r) { }
        public bool Accepts(ResolvedContent content, ResourceId resource) => false;
    }
    public sealed class InletDeclaration : Component<InletDeclarationDef> { }

    // Independent engine content, not host-side scheduling. The standard WorkSystem executes it.
    public sealed class ContractWorkerDef : ComponentDef<ContractWorker>, IWorkerDefinition
    {
        private string _roleId;
        private IReadOnlyList<WorkerRoleId> _roles;
        private Fix _rate;
        public string SpecificationFault { get; private set; }
        public IReadOnlyList<WorkerRoleId> Roles => SpecificationFault == "metadata-null" ? null :
            SpecificationFault == "metadata-empty" ? Array.AsReadOnly(Array.Empty<WorkerRoleId>()) :
            SpecificationFault == "metadata-duplicate" ? Array.AsReadOnly(new[] { _roles[0], _roles[0] }) :
            SpecificationFault == "metadata-unknown" ? Array.AsReadOnly(new[] { new WorkerRoleId("missing") }) : _roles;
        public Fix WorkRate => SpecificationFault == "metadata-zero" ? Fix.Zero : _rate;
        public string HomeFault { get; private set; }
        public EntityStateBindings States { get; private set; }
        public IEntityStateDefinition WorkingState => States["working"];
        public IEntityStateDefinition StoppedState => States["stopped"];
        protected override void ReadCore(JsonReader r)
        {
            _roleId = r.String("role");
            _rate = r.Fix("rate");
            SpecificationFault = r.String("specification_fault", null);
            if (SpecificationFault != null && SpecificationFault != "metadata-zero" && SpecificationFault != "metadata-null" &&
                SpecificationFault != "metadata-empty" && SpecificationFault != "metadata-duplicate" && SpecificationFault != "metadata-unknown" &&
                SpecificationFault != "runtime-null" && SpecificationFault != "runtime-mismatch" && SpecificationFault != "runtime-can-none" &&
                SpecificationFault != "runtime-can-false" && SpecificationFault != "runtime-can-extra" && SpecificationFault != "runtime-touch")
                throw r.Error("unknown worker specification fault probe");
            HomeFault = r.String("home_fault", null);
            if (_rate <= Fix.Zero) throw r.Error("rate must be positive");
            States = EntityStateBindings.Read(r, "working", "stopped");
        }
        protected override void ValidateCore(EntityDef owner)
        {
            if (!owner.HasCapability<IMotion>()) throw new ContentException(owner.Id + ": worker requires motion");
        }
        protected override void ResolveCore(ContentResolver c)
        {
            _roles = Array.AsReadOnly(new[] { c.Def<IWorkerRoleDefinition>(_roleId).Role });
            States = States.Resolve(c);
        }
    }

    public sealed class ContractWorker : Component<ContractWorkerDef>, IWorker, IEntityStateSource
    {
        // Negative observation instrumentation only; never persisted gameplay or positive execution.
        public Action BeforeCan;
        private int _job;
        private EntityId _home;
        private bool _held, _arrived;
        public IReadOnlyList<WorkerRoleId> Roles
        {
            get
            {
                if (Def.SpecificationFault == "runtime-touch") Entity.Touch();
                return Def.SpecificationFault == "runtime-null" ? null :
                    Def.SpecificationFault == "runtime-mismatch" ? Array.AsReadOnly(new[] { WorkerRoleId.None }) : Def.Roles;
            }
        }
        public bool Can(WorkerRoleId role)
        {
            BeforeCan?.Invoke();
            return Def.SpecificationFault == "runtime-can-false" ? false :
                Def.SpecificationFault == "runtime-can-extra" ? role.IsValid :
                role == Def.Roles[0] || Def.SpecificationFault == "runtime-can-none" && !role.IsValid;
        }
        public int Job { get => _job; set => _job = value; }
        public EntityId Home
        {
            get { if (Def.HomeFault == "read-touch") Entity.Touch(); return _home; }
            set
            {
                if (Def.HomeFault == "commit-ignore" || _home == value) return;
                if (Def.HomeFault != "commit-no-touch") Entity.Touch();
                _home = value;
                if (Def.HomeFault == "commit-job") _job = 99;
            }
        }
        public bool Held { get => _held; set => _held = value; }
        public bool HeldArrived { get => _arrived; set => _arrived = value; }
        public bool Idle => _job == 0;
        public IEntityStateDefinition GetState(EntityUpdateContext context)
        {
            Job job = context.World.Services.Get<WorkBoard>().Job(_job);
            if (job != null && (job.State == JobState.Paused ||
                (job.Blocker == WorkDockCodes.Waiting || job.Blocker == WorkDockCodes.Occupied || job.Blocker == WorkDockCodes.DepartureBlocked) &&
                Entity.Get<IMotion>()?.Status != MoveStatus.Moving)) return Def.StoppedState;
            return job != null && job.State == JobState.Assigned && job.ToilIndex < job.Toils.Count
                && job.Toils[job.ToilIndex].Executor == WorkPhases.Productive ? Def.WorkingState : null;
        }
        public override void Sync(IStateSync s)
        {
            int job = _job; EntityId home = _home; bool held = _held, arrived = _arrived;
            s.Int(ref job); s.Entity(ref home); s.Bool(ref held); s.Bool(ref arrived);
            if (job < 0 || home.Value < 0) throw new CorruptStateException("invalid saved contract worker");
            if (s.Reading) { _job = job; _home = home; _held = held; _arrived = arrived; }
        }
    }

    // Declaration faults only: reuse the actual worker type, or declare metadata on a non-worker.
    // These introduce no alternate worker, scheduler, rate or runtime state authority.
    public sealed class UndeclaredWorkerDef : ComponentDef<ContractWorker>
    { protected override void ReadCore(JsonReader reader) { } }
    public sealed class NonWorkerSpecificationDef : ComponentDef<NonWorkerSpecification>, IWorkerDefinition
    {
        public Fix WorkRate => Fix.One;
        public IReadOnlyList<WorkerRoleId> Roles => Array.AsReadOnly(new[] { new WorkerRoleId("inspect") });
        protected override void ReadCore(JsonReader reader) { }
    }
    public sealed class NonWorkerSpecification : Component<NonWorkerSpecificationDef> { }

    // A specialized one-resource endpoint owns its own buffer. It is not Storage or a second ledger
    // beside Storage on the entity, and exposes only the physical endpoint capabilities it needs.
    public sealed class SingleResourcePortDef : ComponentDef<SingleResourcePort>, IInventoryDefinition
    {
        private string _resource, _policy;
        internal ResolvedContent Content { get; private set; }
        public ResourceId Resource { get; private set; }
        public ICapacityPolicy CapacityPolicy { get; private set; }
        public Fix Initial { get; private set; }
        public Bill InitialStock { get; private set; }
        public bool Public { get; private set; }
        public bool Evacuate { get; private set; }
        /// <summary>Authored hostile-provider probe; absent for normal content. Owns no extra stock or receipts.</summary>
        public string ReceiptFault { get; private set; }
        public string ReplacementFault { get; private set; }
        protected override void ReadCore(JsonReader r)
        {
            _resource = r.String("resource"); _policy = r.String("capacity_policy"); Initial = r.Fix("initial");
            Public = r.Bool("public", true); Evacuate = r.Bool("evacuate", false);
            ReceiptFault = r.String("receipt_fault", null);
            ReplacementFault = r.String("replacement_fault", null);
            if (ReplacementFault != null && ReplacementFault != "clone_stock" && ReplacementFault != "mutate_source" &&
                ReplacementFault != "prepared-target" && ReplacementFault != "prepared-target-deny" &&
                ReplacementFault != "prepared-source" && ReplacementFault != "prepared-clock" &&
                ReplacementFault != "prepared-space" && ReplacementFault != "prepared-throw" &&
                ReplacementFault != "retain-source" && ReplacementFault != "retain-clock" && ReplacementFault != "retain-space" &&
                ReplacementFault != "retain-source-throw" && ReplacementFault != "retain-clock-throw" && ReplacementFault != "retain-space-throw" &&
                ReplacementFault != "retain-throw" && ReplacementFault != "retain-target")
                throw r.Error("unknown replacement fault probe");
            if (ReceiptFault != null && ReceiptFault != "partial_take" && ReceiptFault != "retain_take" &&
                ReceiptFault != "partial_receive" && ReceiptFault != "mutating_check" &&
                ReceiptFault != "committed_check" && ReceiptFault != "silent_version" &&
                ReceiptFault != "partial_remove" && ReceiptFault != "partial_add" && ReceiptFault != "refuse_in" && ReceiptFault != "bad_growth" && ReceiptFault != "ignore_release")
                throw r.Error("unknown receipt fault probe");
            if (Initial < Fix.Zero) throw r.Error("initial stock cannot be negative");
        }
        protected override void ResolveCore(ContentResolver c)
        {
            Content = c.Content; Resource = c.Resource(_resource); CapacityPolicy = c.Def<ICapacityPolicyDefinition>(_policy);
            InitialStock = Initial.IsPositive ? new Bill(new[] { new ResourceAmount(Resource, Initial) }) : Bill.Empty;
            if (Initial.IsPositive && (UnitConversion.ValidateAmount(Content.Resource(Resource).QuantityUnit, Initial) != UnitConversionStatus.Converted ||
                CapacityPolicy.Quantum(Resource).Floor(Initial) != Initial)) throw c.Fail("initial stock violates native unit or exact quantum");
            if (!CapacityPolicy.Check(Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>(),
                new[] { new ResourceAmount(Resource, Initial) }).Fits) throw c.Fail("initial stock violates authored capacity policy");
        }
        public bool Accepts(ResolvedContent content, ResourceId resource) => ReferenceEquals(content, Content) && resource == Resource;
    }

    public sealed class SingleResourcePort : Component<SingleResourcePortDef>, IInventory, IInspectable, IEntityInitialization, IEntityReplacementState
    {
        // Transient public-contract fault instrumentation over this actual book, never saved gameplay.
        public Action AfterReserveOut;
        // Hostile observation probes only; no gameplay state, extra stock owner or saved settings.
        public int SignalFault;
        public Action InboundCheckProbe; // Non-authoritative reentrancy/mutation fault injection only.
        public ResourceEndpointId Endpoint => ResourceEndpointId.ForEntity(Entity.Id);
        public void WriteResourceState(IStateSync writer)
        { ResourceStateEncoding.RequireWriter(writer); SyncResources(writer); }
        public PlayerId Company => Entity.Owner;
        public int Version => Entity.Version;
        private Fix _amount;
        private int _serviceCount;
        public int ServiceCount => _serviceCount;
        // Hostile provider probe only: change this actual persisted payload without version invalidation.
        public void FaultServicePayload() => _serviceCount = checked(_serviceCount + 1);
        public void RecordService() { _serviceCount = checked(_serviceCount + 1); Entity.Touch(); }
        public void RestoreReplacementState(World world, Component source)
        {
            if (!(source is SingleResourcePort previous)) throw new InvalidOperationException("incompatible service state");
            _serviceCount = previous._serviceCount;
            if (Def.ReplacementFault == "retain-target") FaultServicePayload(); // Legitimate candidate-local change.
            if (Def.ReplacementFault == "retain-source" || Def.ReplacementFault == "retain-source-throw") previous.FaultServicePayload();
            if (Def.ReplacementFault == "retain-clock" || Def.ReplacementFault == "retain-clock-throw") ((Beep.SimEngine.Hosting.GameClock)world.Clock).Pause();
            if (Def.ReplacementFault == "retain-space" || Def.ReplacementFault == "retain-space-throw")
            { var space=(PointSpace)world.Space; space.SetAccess(!space.AccessOpen); }
            if (Def.ReplacementFault == "retain-throw" || Def.ReplacementFault == "retain-source-throw" ||
                Def.ReplacementFault == "retain-clock-throw" || Def.ReplacementFault == "retain-space-throw")
                throw new InvalidOperationException("authored local retention failure: " + Def.ReplacementFault);
            if (Def.ReplacementFault == "clone_stock") Add(Def.Resource, Fix.One);
            if (Def.ReplacementFault == "mutate_source") previous.Remove(previous.Def.Resource, Fix.One);
            if (Def.ReplacementFault != null && Def.ReplacementFault.StartsWith("prepared-", StringComparison.Ordinal))
                InboundCheckProbe = () => {
                    if (Def.ReplacementFault == "prepared-target" || Def.ReplacementFault == "prepared-target-deny") FaultServicePayload();
                    if (Def.ReplacementFault == "prepared-source") previous.FaultServicePayload();
                    if (Def.ReplacementFault == "prepared-clock") ((Beep.SimEngine.Hosting.GameClock)world.Clock).Pause();
                    if (Def.ReplacementFault == "prepared-space") { var space=(PointSpace)world.Space; space.SetAccess(!space.AccessOpen); }
                    if (Def.ReplacementFault == "prepared-throw") throw new InvalidOperationException("authored detached admission failure");
                };
        }
        private readonly ResourceReservationBook _reservations = new ResourceReservationBook();
        public bool Public => Def.Public;
        public bool Evacuate => Def.Evacuate;
        public ICapacityPolicy CapacityPolicy => Def.CapacityPolicy;
        private IReadOnlyList<ResourceAmount> Vector(Fix amount) => Array.AsReadOnly(amount.IsPositive
            ? new[] { new ResourceAmount(Def.Resource, amount) } : Array.Empty<ResourceAmount>());
        public IReadOnlyList<ResourceAmount> Stock => Vector(_amount);
        public IReadOnlyList<ResourceAmount> InboundPromises => _reservations.Promises(ReservationDirection.Inbound);
        public IReadOnlyList<ResourceAmount> OutboundPromises => _reservations.Promises(ReservationDirection.Outbound);
        public IReadOnlyList<ResourceReservation> Reservations => _reservations.Records;
        public bool Accepts(ResolvedContent content, ResourceId resource) => Def.Accepts(content, resource);
        public CapacityCheck CheckIncoming(IReadOnlyList<ResourceAmount> incoming)
        {
            if (SignalFault == 3) Entity.Touch();
            if (SignalFault == 4) return new CapacityCheck((CapacityStatus)255);
            if (SignalFault == 5) return new CapacityCheck(CapacityStatus.OutOfRange);
            if (incoming == null) throw new ArgumentNullException(nameof(incoming));
            foreach (ResourceAmount entry in incoming)
                if (entry.Resource != Def.Resource) return new CapacityCheck(CapacityStatus.ResourceRejected, entry.Resource);
            return CapacityPolicy.Check(Stock, InboundPromises, incoming);
        }
        public Fix Offer(ResourceId resource, Fix ceiling)
        {
            if (SignalFault == 10) Entity.Touch();
            if (SignalFault == 11) return -Fix.One;
            if (SignalFault == 12) return Fix.Parse("0.5");
            if (SignalFault == 13) return Fix.FromInt(100);
            return resource == Def.Resource ? CapacityPolicy.Offer(Stock, InboundPromises, resource, ceiling).Amount : Fix.Zero;
        }
        public Fix Amount(ResourceId resource)
        {
            if (SignalFault == 1) Entity.Touch();
            return SignalFault == 2 ? -Fix.One : resource == Def.Resource ? _amount : Fix.Zero;
        }
        public Fix Available(ResourceId resource) => Amount(resource) - ReservedOut(resource) + (SignalFault == 8 ? Fix.One : Fix.Zero);
        public Fix ReservedIn(ResourceId resource)
        {
            if (SignalFault == 9) Entity.Touch();
            return SignalFault == 7 ? Fix.Parse("0.5") : _reservations.Amount(resource, ReservationDirection.Inbound);
        }
        public Fix ReservedOut(ResourceId resource) => SignalFault == 6 ? -Fix.One : _reservations.Amount(resource, ReservationDirection.Outbound);
        public void Initialize(World world) => ResourceTransactions.InitializeStock(world, Entity);
        public Code CheckRemoval(World world) => ResourceTransactions.CheckInventoryRemoval(this);
        private bool Valid(ResourceId resource, Fix amount) => resource == Def.Resource && amount.IsPositive &&
            UnitConversion.ValidateAmount(Def.Content.Resource(resource).QuantityUnit, amount) == UnitConversionStatus.Converted;
        public Fix Add(ResourceId resource, Fix amount)
        {
            if (!Valid(resource, amount)) return Fix.Zero;
            Fix added = Offer(resource, amount);
            if (added.IsPositive) { _amount += added; Entity.Touch(); }
            return Def.ReceiptFault == "partial_add" ? added / Fix.FromInt(2) : added;
        }
        public Fix Remove(ResourceId resource, Fix amount)
        {
            if (!Valid(resource, amount)) return Fix.Zero;
            Fix removed = CapacityPolicy.Offer(Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>(),
                resource, Fix.Min(amount, Available(resource))).Amount;
            if (removed.IsPositive) { _amount -= removed; Entity.Touch(); }
            return Def.ReceiptFault == "partial_remove" ? removed / Fix.FromInt(2) : removed;
        }
        public bool Holds(Bill bill)
        {
            foreach (ResourceAmount line in bill.Lines) if (Amount(line.Resource) < line.Amount) return false;
            return true;
        }
        public bool HoldsUnpromised(Bill bill)
        {
            foreach (ResourceAmount line in bill.Lines) if (Available(line.Resource) < line.Amount) return false;
            return true;
        }
        public Fix Receive(ResolvedContent content, ResourceId resource, Fix amount) => Accepts(content, resource) ? Add(resource, amount) : Fix.Zero;
        public Fix Take(ResourceId resource, Fix amount) => Remove(resource, amount);
        public bool CanReserveIn(ReservationOwner owner, ResourceId resource, Fix amount)
        {
            InboundCheckProbe?.Invoke();
            return Def.ReceiptFault != "refuse_in" && _reservations.CanReserve(owner, resource, ReservationDirection.Inbound) && Valid(resource, amount)
                && CheckIncoming(new[] { new ResourceAmount(resource, amount) }).Fits;
        }
        public ReservationToken ReserveIn(ReservationOwner owner, ResourceId resource, Fix amount)
        {
            if (!CanReserveIn(owner, resource, amount)) throw new InvalidOperationException("invalid inbound port commitment");
            ReservationToken token = _reservations.Add(Endpoint, owner, resource, amount, ReservationDirection.Inbound);
            Entity.Touch(); return token;
        }
        public bool CanReserveOut(ReservationOwner owner, ResourceId resource, Fix amount)
            => _reservations.CanReserve(owner, resource, ReservationDirection.Outbound) && Valid(resource, amount)
                && amount <= Available(resource) && CapacityPolicy.Check(Array.Empty<ResourceAmount>(),
                    Array.Empty<ResourceAmount>(), new[] { new ResourceAmount(resource, amount) }).Fits;
        public ReservationToken ReserveOut(ReservationOwner owner, ResourceId resource, Fix amount)
        {
            if (!CanReserveOut(owner, resource, amount)) throw new InvalidOperationException("invalid outbound port commitment");
            ReservationToken token = _reservations.Add(Endpoint, owner, resource, amount, ReservationDirection.Outbound);
            Entity.Touch(); AfterReserveOut?.Invoke(); return token;
        }
        public void ReleaseIn(ReservationOwner owner, ReservationToken token)
        { _reservations.Release(Endpoint, owner, token, ReservationDirection.Inbound); Entity.Touch(); }
        public void ReleaseOut(ReservationOwner owner, ReservationToken token)
        {
            if (Def.ReceiptFault == "ignore_release") return; // Hostile proof only; the engine must reject fake tool release.
            _reservations.Release(Endpoint, owner, token, ReservationDirection.Outbound); Entity.Touch();
        }
        public ResourceReceiptCheck CheckReservedIn(ResolvedContent content, ReservationOwner owner, ReservationToken token)
        {
            ResourceReceiptCheck check = _reservations.Check(Endpoint, owner, token, ReservationDirection.Inbound);
            if (!check.Ready) return check;
            ResourceReservation record = check.Record;
            if (!Accepts(content, record.Resource)) return new ResourceReceiptCheck(ResourceTransferStatus.Incompatible, record);
            return CapacityPolicy.Check(Stock, _reservations.Promises(ReservationDirection.Inbound, token),
                new[] { new ResourceAmount(record.Resource, record.Amount) }).Fits
                ? check : new ResourceReceiptCheck(ResourceTransferStatus.NoCapacity, record);
        }
        public ResourceReceiptCheck CheckReservedOut(ReservationOwner owner, ReservationToken token)
        {
            if (Def.ReceiptFault == "mutating_check") Entity.Touch(); // Deliberate contract violation: the engine must fault.
            ResourceReceiptCheck check = _reservations.Check(Endpoint, owner, token, ReservationDirection.Outbound);
            if (!check.Ready) return check;
            if (Def.ReceiptFault == "committed_check") return new ResourceReceiptCheck(ResourceTransferStatus.Committed, check.Record);
            ResourceReservation record = check.Record;
            if (!Accepts(Def.Content, record.Resource)) return new ResourceReceiptCheck(ResourceTransferStatus.Incompatible, record);
            if (record.Amount > _amount || ReservedOut(record.Resource) > _amount)
                return new ResourceReceiptCheck(ResourceTransferStatus.NoStock, record);
            return CapacityPolicy.Check(Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>(),
                new[] { new ResourceAmount(record.Resource, record.Amount) }).Fits
                ? check : new ResourceReceiptCheck(ResourceTransferStatus.Incompatible, record);
        }
        public Fix ReceiveReserved(ResolvedContent content, ReservationOwner owner, ReservationToken token, Fix amount)
        {
            ResourceReceiptCheck check = CheckReservedIn(content, owner, token);
            if (!check.Ready) throw new InvalidOperationException("reserved port admission refused: " + check.Status);
            ResourceReservation record = check.Record;
            CheckReceiptAmount(record, amount);
            _reservations.Consume(Endpoint, owner, token, ReservationDirection.Inbound, amount);
            _amount += amount; Entity.Touch();
            return Def.ReceiptFault == "partial_receive" ? amount / Fix.FromInt(2) : amount;
        }
        public Fix TakeReserved(ReservationOwner owner, ReservationToken token, Fix amount)
        {
            ResourceReceiptCheck check = CheckReservedOut(owner, token);
            if (!check.Ready) throw new InvalidOperationException("reserved port pickup refused: " + check.Status);
            ResourceReservation record = check.Record;
            CheckReceiptAmount(record, amount);
            if (Def.ReceiptFault == "retain_take") return amount; // Deliberately lies about consuming the owned receipt.
            _reservations.Consume(Endpoint, owner, token, ReservationDirection.Outbound, amount);
            _amount -= amount;
            if (Def.ReceiptFault != "silent_version") Entity.Touch();
            return Def.ReceiptFault == "partial_take" ? amount / Fix.FromInt(2) : amount;
        }

        public void IncreaseReservedOut(ReservationOwner owner, ReservationToken token, Fix amount)
        {
            ResourceReservation record = _reservations.Get(Endpoint, owner, token, ReservationDirection.Outbound);
            Fix total = record.Amount + amount;
            if (!Valid(record.Resource, amount) || Available(record.Resource) < amount || CapacityPolicy.Quantum(record.Resource).Floor(total) != total)
                throw new InvalidOperationException("invalid native port promise growth");
            _reservations.Increase(Endpoint, owner, token, Def.ReceiptFault == "bad_growth" ? amount + amount : amount); Entity.Touch();
        }
        private void CheckReceiptAmount(ResourceReservation record, Fix amount)
        {
            if (!Valid(record.Resource, amount) || amount > record.Amount || CapacityPolicy.Quantum(record.Resource).Floor(amount) != amount)
                throw new InvalidOperationException("point receipt quantity must be native and bounded");
        }

        private void SyncResources(IStateSync s)
        { s.Fix(ref _amount); _reservations.Sync(s, Endpoint); }
        public override void Sync(IStateSync s)
        {
            s.Int(ref _serviceCount);
            if (_serviceCount < 0) throw new CorruptStateException("negative service count");
            SyncResources(s);
            if (!s.Reading) return;
            foreach (ResourceReservation record in Reservations)
                if (!Valid(record.Resource, record.Amount)) throw new CorruptStateException("invalid native port commitment");
            if (_amount < Fix.Zero || ReservedOut(Def.Resource) > _amount ||
                !CapacityPolicy.Check(Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>(), OutboundPromises).Fits ||
                !CapacityPolicy.Check(Stock, InboundPromises, Array.Empty<ResourceAmount>()).Fits)
                throw new CorruptStateException("invalid saved measured port");
        }
        public void Inspect(World world, PlayerId viewer, IList<Facet> into)
        {
            if ((!viewer.IsValid || viewer == Entity.Owner) && world.ViewFor(viewer).TryFindResource(world.Content.ResourceName(Def.Resource), out _))
                into.Add(new Facet(Storage.FacetStock, _amount, world.Content.ResourceName(Def.Resource)));
        }
    }
}
