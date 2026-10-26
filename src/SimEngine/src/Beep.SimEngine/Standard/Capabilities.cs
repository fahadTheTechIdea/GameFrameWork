using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Authored receiving compatibility, available before an entity is created.</summary>
    public interface IInletPortDefinition : IComponentDefinition
    {
        bool Accepts(ResolvedContent content, ResourceId resource);
    }

    /// <summary>Authored stock capacity/compatibility used before state-preserving replacement.</summary>
    public interface IInventoryDefinition : IInletPortDefinition
    {
        ICapacityPolicy CapacityPolicy { get; }
        /// <summary>Authored physical visibility and evacuation disposition, available before creation/replacement.</summary>
        bool Public { get; }
        bool Evacuate { get; }
        /// <summary>Authored creation stock. Restore/replacement never grants this bill again.</summary>
        Bill InitialStock { get; }
    }

    /// <summary>Configuration for the reusable stock owner; Content is the actual frozen session catalog, not a World.</summary>
    public interface IStorageDefinition : IInventoryDefinition
    {
        ResolvedContent Content { get; }
        IReadOnlyList<ResourceLotAmount> InitialLots { get; }
    }

    /// <summary>Reusable configured inventory; stock and promises remain in its single existing owner.</summary>
    public interface IStorage : IInventory { }

    /// <summary>Authored tile-navigation facts for production rollout and travel previews.</summary>
    public interface IMobilityDefinition : IComponentDefinition
    {
        string MovementClass { get; }
        Fix TilesPerHour { get; }
    }

    /// <summary>Physical reserved supply, separate from its optional pricing and account settlement.</summary>
    public interface ISupplier : IEntityCapability, IEntityRemovalGuard, IResourceEndpoint
    {
        ResourceSupplyKind SupplyKind { get; }
        /// <summary>The existing stock owner for finite supply; null only for explicit authored creation.</summary>
        IOutletPort SupplyStock { get; }
        /// <summary>Side-effect-free offer, between zero and requested. Unlimited authored sources return requested.</summary>
        Fix Offer(ResourceId resource, Fix requested);
        CapacityQuantum SupplyQuantum(ResourceId resource);
        IReadOnlyList<ResourceReservation> Reservations { get; }
        bool CanReserveSupply(ReservationOwner owner, ResourceId resource, Fix amount);
        ReservationToken ReserveSupply(ReservationOwner owner, ResourceId resource, Fix amount);
        void ReleaseSupply(ReservationOwner owner, ReservationToken token);
        /// <summary>Pure exact physical receipt preflight. Payment belongs to IResourcePurchaseSettlement.</summary>
        ResourceReceiptCheck CheckSupply(World world, PlayerId buyer, ReservationOwner owner, ReservationToken token, Fix amount);
        /// <summary>Transaction-only physical callback. Keep the same receipt's unspent remainder; never charge money.</summary>
        Fix SupplyReserved(World world, PlayerId buyer, ReservationOwner owner, ReservationToken token, Fix amount, ResourceSupplyExecution execution);
    }

    /// <summary>Receiving, compatibility and promised capacity of a physical endpoint.</summary>
    public interface IInletPort : IEntityCapability, IEntityRemovalGuard, IResourceInlet { }

    /// <summary>Every exporting endpoint is also a receiving endpoint; compatibility remains explicit.</summary>
    public interface IOutletPort : IInletPort, IResourceOutlet { }

    /// <summary>One inventory authority, shared by endpoint, project, production and cargo consumers.</summary>
    public interface IInventory : IOutletPort
    {
        IReadOnlyList<ResourceAmount> Stock { get; }
        Fix Add(ResourceId resource, Fix amount);
        Fix Remove(ResourceId resource, Fix amount);
        bool Holds(Bill bill);
        bool HoldsUnpromised(Bill bill);
    }

    /// <summary>Optional engine-owned physical disposition of current entity stock, with pure preflight.</summary>
    public interface IResourceRecovery
    {
        IResourceRecoveryDefinition Definition { get; }
        Code CheckEvacuation(World world, Entity source);
        Code Evacuate(World world, Entity source);
        Code CheckYield(World world, PlayerId company, SpatialPlacement placement, Bill resources, ReservationOwner requester, Code cause);
        Code ReleaseYield(World world, PlayerId company, SpatialPlacement placement, Bill resources, ReservationOwner requester, Code cause);
    }

    public interface IResourceRecoveryDefinition : IContentDefinition
    {
        string Space { get; }
        EntityDef Container { get; }
    }

    /// <summary>Immutable authored machine rating and roles; runtime modifiers use execution contracts.</summary>
    public interface IWorkerDefinition : IComponentDefinition
    {
        Fix WorkRate { get; }
        IReadOnlyList<WorkerRoleId> Roles { get; }
    }

    /// <summary>Authored modes required by the reusable physical worker state source.</summary>
    public interface IStatefulWorkerDefinition : IWorkerDefinition
    {
        EntityStateBindings States { get; }
    }

    public interface IWorkerRoles : IEntityCapability
    {
        IReadOnlyList<WorkerRoleId> Roles { get; }
        bool Can(WorkerRoleId role);
    }

    public interface IWorker : IWorkerRoles
    {
        int Job { get; set; }
        /// <summary>Sole home intent. A changed setter touches its entity and changes no other state; None clears.</summary>
        EntityId Home { get; set; }
        bool Held { get; set; }
        bool HeldArrived { get; set; }
        bool Idle { get; }
    }

    /// <summary>Physical worker whose authored working/loading/stopped modes participate in entity Update.</summary>
    public interface IStatefulWorker : IWorker, IEntityStateSource { }

    /// <summary>Tile-navigation capability of the optional standard module, not a mandatory world model.</summary>
    public interface IMobility : IMotion
    {
        string MovementClass { get; }
        TileRect Goal { get; }
        PathSearchResult CanReach(World world, TileRect goal);
        void GoTo(TileRect goal);
    }

    /// <summary>Authored handling profile for an actual physical transporter.</summary>
    public interface ITransporterDefinition : IComponentDefinition
    {
        IResourceHandlingProfile Handling { get; }
        /// <summary>Pure validation of this provider's authored cargo/presence requirements, not native layout assumptions.</summary>
        void ValidateBinding(EntityDef owner);
    }

    /// <summary>Explicit transport capability; actual cargo may have a separate inventory owner.
    /// Removal must protect that binding through the same entity removal contract.</summary>
    public interface ITransporter : IEntityCapability, IEntityRemovalGuard
    {
        IInventory Cargo { get; }
        IResourceHandlingProfile Handling { get; }
    }

    public interface INetworkPorts : IEntityCapability
    {
        IReadOnlyList<PortSpec> Connections { get; }
    }

    public interface INetworkPortsDefinition : IComponentDefinition
    {
        IReadOnlyList<PortSpec> Connections { get; }
    }

    /// <summary>Pure named connection readiness; network pools and timed flow keep their existing owner.</summary>
    public interface INetworkFlowGate : INamedEntityCapability
    {
        ExecutionGateSnapshot ReadGate(World world, INetworkDefinition network);
    }

    /// <summary>Selected spatial-network execution; physical work owns when removal is invoked.</summary>
    public interface INetworkTopology
    {
        /// <summary>Pure proposed geometry admission; active affected receipts must settle before stamping.</summary>
        Code CheckAddition(PlayerId player, ILineDefinition line, IReadOnlyList<SpatialPlacement> path);
        Code CheckRemoval(PlayerId player, ILineDefinition line, IReadOnlyList<SpatialPlacement> path);
        Code Remove(PlayerId player, ILineDefinition line, IReadOnlyList<SpatialPlacement> path);
    }

    /// <summary>Sole transporter JSON reference reader/resolver and capability binding validator.</summary>
    public sealed class TransporterDeclaration
    {
        private readonly string _handling;
        public IResourceHandlingProfile Handling { get; private set; }
        private TransporterDeclaration(JsonReader reader)
        {
            _handling = reader.String("handling");
            try { ContractIdentity.ValidateId(_handling, "handling profile"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
        }
        public static TransporterDeclaration Read(JsonReader reader)
            => new TransporterDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content) => Handling = content.Def<IResourceHandlingProfile>(_handling);
        public static void ValidateBinding(EntityDef owner)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (!owner.HasCapability<IInventory>() || !owner.HasCapability<ISpatialPresence>())
                throw new ContentException(owner.Id + ": transporter requires inventory and spatial-presence capabilities; its space provider controls physical reach");
        }
    }

    /// <summary>Sole actual cargo binding observation and transporter removal admission.</summary>
    public static class TransporterContracts
    {
        public static IInventory ReadCargo(World world, Entity worker)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            world.RequireHealthyUpdate();
            var transporter = worker?.Get<ITransporter>();
            if (worker == null || !worker.Alive || !ReferenceEquals(world.Entities.Get(worker.Id), worker) || transporter == null)
                throw new ArgumentException("cargo observation requires an actual live transporter");
            return EntityObservation.ReadProvider(world, transporter, worker, () => {
                if (!ReferenceEquals(transporter.Entity, worker))
                    throw new InvalidOperationException("transporter differs from its actual worker binding");
                IInventory cargo = transporter.Cargo;
                if (cargo == null || cargo.Entity == null || !cargo.Entity.Alive || cargo.Entity.Owner != worker.Owner ||
                    !ReferenceEquals(world.Entities.Get(cargo.Entity.Id), cargo.Entity) || !ReferenceEquals(cargo.Entity.Get<IInventory>(), cargo))
                    throw new InvalidOperationException("transporter returned foreign or non-actual cargo inventory");
                return cargo;
            });
        }
        public static Code CheckRemoval(World world, ITransporter transporter)
        {
            if (transporter == null) throw new ArgumentNullException(nameof(transporter));
            Entity worker = transporter.Entity;
            if (worker == null || !ReferenceEquals(worker.Get<ITransporter>(), transporter))
                throw new ArgumentException("removal requires its actual transporter capability");
            IInventory cargo = ReadCargo(world, worker);
            // The worker's actual inventory already participates in World's guard loop. Its
            // original recovery owner may explicitly resolve that same inventory guard.
            if (ReferenceEquals(cargo.Entity, worker)) return Code.None;
            return EntityObservation.ReadProvider(world, cargo, cargo.Entity,
                () => ResourceTransactions.CheckInventoryRemoval(cargo));
        }
    }

    public sealed class TransporterDef : ComponentDef<Transporter>, ITransporterDefinition
    {
        private TransporterDeclaration _declaration;
        public IResourceHandlingProfile Handling => _declaration.Handling;
        protected override void ReadCore(JsonReader reader) => _declaration = TransporterDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public void ValidateBinding(EntityDef owner) => TransporterDeclaration.ValidateBinding(owner);
        protected override void ValidateCore(EntityDef owner) => ValidateBinding(owner);
    }

    public sealed class Transporter : Component, ITransporter
    {
        public new ITransporterDefinition Def => (ITransporterDefinition)base.Def;
        public IInventory Cargo => Entity.Get<IInventory>();
        public IResourceHandlingProfile Handling => Def.Handling;
        public Code CheckRemoval(World world) => TransporterContracts.CheckRemoval(world, this);
    }
}
