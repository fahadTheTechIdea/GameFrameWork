using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public enum OperatingPhase : byte { Movement = 1, Idle = 2, Standby = 3 }
    public interface IOperatingInputsDefinition : IResourcePortDefinition
    {
        OperatingPhase Phase { get; }
        IReadOnlyList<IResourceRate> Inputs { get; }
    }
    /// <summary>Metadata for the reusable phase exposure component and its blocked entity state.</summary>
    public interface IStatefulOperatingInputsDefinition : IOperatingInputsDefinition
    {
        EntityStateBindings States { get; }
    }
    public interface IOperatingInputs : IUtilityConsumer, IResourcePort
    {
        OperatingExposureSnapshot ReadExposure();
        void AdvanceExposure(OperatingInputsContext context);
    }
    public interface IStatefulOperatingInputs : IOperatingInputs, IEntityStateSource { }
    public readonly struct OperatingExposureSnapshot
    {
        public long ElapsedTicks { get; }
        public long UpdatedTick { get; }
        public OperatingExposureSnapshot(long elapsedTicks, long updatedTick) { ElapsedTicks = elapsedTicks; UpdatedTick = updatedTick; }
    }
    /// <summary>Engine-issued admission. Only the provider owns exposure; the actual inventory owns resources.</summary>
    public readonly struct OperatingInputsContext
    {
        public World World { get; }
        public Entity Entity { get; }
        public Code Source { get; }
        public long Tick { get; }
        public OperatingPhase Phase { get; }
        public OperatingExposureSnapshot Before { get; }
        public OperatingExposureSnapshot After { get; }
        internal OperatingInputsContext(World world, Entity entity, Code source, OperatingPhase phase, OperatingExposureSnapshot before)
        { World = world; Entity = entity; Source = source; Phase = phase; Tick = world.Tick; Before = before; After = new OperatingExposureSnapshot(checked(before.ElapsedTicks + 1), world.Tick); }
    }
    public readonly struct OperatingUtilitySnapshot
    {
        public OperatingExposureSnapshot Exposure { get; }
        public UtilityDemandSnapshot Demand { get; }
        internal OperatingUtilitySnapshot(OperatingExposureSnapshot exposure, UtilityDemandSnapshot demand)
        { Exposure = exposure; Demand = demand; }
    }
    public sealed class OperatingInputEvaluation
    {
        public IReadOnlyList<OperatingUtilitySnapshot> Providers { get; }
        public long Intervals { get; }
        public Bill RequiredInputs { get; }
        public Code Blocker { get; }
        internal OperatingInputEvaluation(List<OperatingUtilitySnapshot> providers, long intervals, Bill required, Code blocker)
        { Providers = providers.AsReadOnly(); Intervals = intervals; RequiredInputs = required; Blocker = blocker; }
    }
    public static class OperatingInputCodes
    {
        public static readonly Code Inputs = new Code("operating.inputs"), Range = new Code("operating.range"),
            Cause = new Code("operating.inputs");
    }
    /// <summary>One admission/commit path. No resource balance, rounding state, route or work queue.</summary>
    public static class OperatingInputsContracts
    {
        internal static bool CanDisclose(World world, PlayerId viewer, OperatingInputEvaluation value)
        {
            world.RequireHealthyUpdate();
            if (!world.ViewFor(viewer).CanSeeResources(value.RequiredInputs.Lines)) return false;
            foreach (OperatingUtilitySnapshot provider in value.Providers)
                if (!UtilityContracts.CanDisclose(world, viewer, provider.Demand)) return false;
            return true;
        }

        public static bool IsValidPhase(OperatingPhase phase) => phase == OperatingPhase.Movement || phase == OperatingPhase.Idle || phase == OperatingPhase.Standby;
        internal static bool HasInputs(Entity entity)
        { foreach (var part in entity.Components) if (part is IOperatingInputs) return true; return false; }
        public static OperatingPhase? CurrentWorkerPhase(World world, Entity entity)
        {
            if (world == null || entity == null || !entity.Alive || !ReferenceEquals(world.Entities.Get(entity.Id), entity) || !entity.Has<IWorker>())
                throw new ArgumentException("worker operating phase requires the actual live session worker");
            return EntityObservation.Read(world, entity, () =>
            {
                if (entity.Get<IWorker>().Job != 0 || entity.Has<IMotion>() && MotionExecutionContracts.ReadStatus(world, entity) == MoveStatus.Moving) return (OperatingPhase?)null;
                var policy = WorkPolicyRules.Read(entity);
                return policy != null && (!policy.Enabled || policy.AssignmentMode != WorkAssignmentMode.All) ? OperatingPhase.Standby : OperatingPhase.Idle;
            });
        }
        private static bool Active(World world, Entity entity, OperatingPhase phase)
            => phase == OperatingPhase.Movement ? entity.Has<IMotion>() && MotionExecutionContracts.ReadStatus(world, entity) == MoveStatus.Moving : CurrentWorkerPhase(world, entity) == phase;
        public static bool IsStructurallyValid(OperatingExposureSnapshot value)
            => value.ElapsedTicks >= 0 && value.UpdatedTick >= -1 && (value.ElapsedTicks == 0) == (value.UpdatedTick == -1) &&
                (value.ElapsedTicks <= value.UpdatedTick || value.ElapsedTicks - value.UpdatedTick <= 1);

        /// <summary>Actual inputs for the next accepted interval of the declared phase. Never advances game time or consumes stock.</summary>
        public static UtilityDemandSnapshot ReadDemand(World world, IOperatingInputs provider)
        {
            var entity = provider?.Entity;
            if (world == null || entity == null || !entity.Alive ||
                !ReferenceEquals(world.Entities.Get(entity.Id), entity))
                throw new ArgumentException("operating utility requires its actual named live session provider");
            return EntityObservation.ReadProvider(world, provider, entity, () =>
            {
                var definition = entity.Def.Component<IOperatingInputsDefinition>(provider.CapabilityId);
                if (provider.CapabilityId.IsNone || !ReferenceEquals(entity.Get<IOperatingInputs>(provider.CapabilityId), provider) ||
                    definition == null || !definition.ComponentType.IsInstanceOfType(provider))
                    throw new InvalidOperationException("operating utility lost its actual named metadata/provider alias");
                return CalculateDemand(world, provider, provider.ReadExposure());
            });
        }

        private static UtilityDemandSnapshot CalculateDemand(World world, IOperatingInputs provider, OperatingExposureSnapshot exposure)
        {
            var entity = provider.Entity;
            var definition = entity.Def.Component<IOperatingInputsDefinition>(provider.CapabilityId);
            var store = entity.Get<IInventory>();
            if (definition?.Inputs == null || !IsValidPhase(definition.Phase) || store == null)
                throw new InvalidOperationException("operating inputs require actual named metadata and inventory");
            ValidateRates(world, definition.Inputs, store.CapacityPolicy);
            if (!IsStructurallyValid(exposure) || exposure.UpdatedTick > world.Tick)
                throw new InvalidOperationException("invalid operating exposure chronology");
            Bill consumed = ResourceRates.Accumulated(definition.Inputs, world.Time, store.CapacityPolicy,
                new WorkProgress(exposure.ElapsedTicks), ResourceRateRounding.Ceiling);
            Bill next = Bill.Empty; Code blocker = Code.None;
            try
            {
                next = DrawIntervals(world, provider, exposure, 1);
            }
            catch (OverflowException) { blocker = OperatingInputCodes.Range; }
            var resources = new List<ResourceId>(); var available = new List<ResourceAmount>();
            foreach (var rate in definition.Inputs)
            {
                ResourceId resource = rate.Resource.Resource; resources.Add(resource);
                if (!ResourceQuantitySignals.Capture(world, store, resource, out var quantity).IsNone)
                    throw new InvalidOperationException("operating utility lost actual endpoint compatibility");
                Fix amount = Fix.Min(next.AmountOf(resource), quantity.Available);
                if (amount > Fix.Zero) available.Add(new ResourceAmount(resource, amount));
            }
            var bindings = UtilityContracts.Bindings(world, provider, resources);
            foreach (var binding in bindings)
                if(!binding.Ready){blocker=ResourcePortContracts.Blocker(bindings);available.RemoveAll(value=>value.Resource==binding.Route.Resource);}
                else if (!ReferenceEquals(binding.Store, store)) throw new InvalidOperationException("phase operating port lost its actual local owner");
            return new UtilityDemandSnapshot(provider.CapabilityId, exposure.ElapsedTicks,
                ResourcePortContracts.Facts(bindings), resources.Count > 0, consumed, next, new Bill(available), blocker);
        }

        private static bool SameBill(Bill actual, Bill expected)
        {
            if (actual.Count != expected.Count) return false;
            for (int i = 0; i < actual.Count; i++) if (!actual[i].Equals(expected[i])) return false;
            return true;
        }

        private static void ValidateDemand(UtilityDemandSnapshot actual, UtilityDemandSnapshot expected)
        {
            if (actual.ElapsedTicks != expected.ElapsedTicks || actual.Drawing != expected.Drawing || actual.NextBlocker != expected.NextBlocker ||
                actual.Resources.Count != expected.Resources.Count || !SameBill(actual.Consumed, expected.Consumed) ||
                !SameBill(actual.NextInputs, expected.NextInputs))
                throw new InvalidOperationException("operating utility facts differ from actual authored rates/exposure");
            for (int i = 0; i < actual.Resources.Count; i++) if (actual.Resources[i] != expected.Resources[i])
                throw new InvalidOperationException("operating utility declared resources differ from actual authored rates");
        }

        private static Bill DrawIntervals(World world, IOperatingInputs provider, OperatingExposureSnapshot exposure, long intervals)
        {
            var definition = provider.Entity.Def.Component<IOperatingInputsDefinition>(provider.CapabilityId);
            return ResourceRates.Between(definition.Inputs, world.Time, provider.Entity.Get<IInventory>().CapacityPolicy,
                new WorkProgress(exposure.ElapsedTicks), new WorkProgress(checked(exposure.ElapsedTicks + intervals)), ResourceRateRounding.Ceiling);
        }

        /// <summary>Pure input horizon from actual accepted phase exposure. Never advances/claims future game time or stock.</summary>
        public static OperatingInputEvaluation Read(World world, Entity entity, OperatingPhase phase, long intervals = 1)
        {
            if (intervals < 0) throw new ArgumentOutOfRangeException(nameof(intervals));
            if (world == null || entity == null || !entity.Alive || !IsValidPhase(phase) || !ReferenceEquals(world.Entities.Get(entity.Id), entity) ||
                (phase == OperatingPhase.Movement ? !entity.Has<IMotion>() : !entity.Has<IWorker>()))
                throw new ArgumentException("operating inputs require the actual live phase target");
            var facts = new List<OperatingUtilitySnapshot>(); var amounts = new List<ResourceAmount>(); Code blocker = Code.None;
            ExecutionGateContracts.Read<IOperatingInputs>(world, entity, provider =>
            {
                var definition = entity.Def.Component<IOperatingInputsDefinition>(provider.CapabilityId);
                if (definition == null || !IsValidPhase(definition.Phase)) throw new InvalidOperationException("operating inputs lost phase metadata");
                if (definition.Phase != phase) return new ExecutionGateSnapshot(provider.CapabilityId, false, Code.None, Array.Empty<ExecutionGateConditionSnapshot>());
                var demand = UtilityContracts.Read(world, provider);
                var exposure = provider.ReadExposure();
                ValidateDemand(demand, CalculateDemand(world, provider, exposure));
                if (intervals > 0 && !demand.NextBlocker.IsNone) blocker = demand.NextBlocker;
                try { amounts.AddRange(intervals == 1 ? demand.NextInputs.Lines : DrawIntervals(world, provider, exposure, intervals).Lines); }
                catch (OverflowException) { blocker = OperatingInputCodes.Range; }
                facts.Add(new OperatingUtilitySnapshot(exposure, demand));
                return new ExecutionGateSnapshot(provider.CapabilityId, true, Code.None, Array.Empty<ExecutionGateConditionSnapshot>());
            });
            Bill draw;
            try { draw = blocker == OperatingInputCodes.Range ? Bill.Empty : new Bill(amounts); }
            catch (OverflowException) { draw = Bill.Empty; blocker = OperatingInputCodes.Range; }
            if (facts.Count > 0 && blocker.IsNone)
            {
                var status = EntityObservation.Read(world, entity, () => world.Services.Get<IResourceTransaction>().EvaluateInventory(Intent(entity, draw)).Status);
                if (status == ResourceTransferStatus.NoStock) blocker = OperatingInputCodes.Inputs;
                else if (status != ResourceTransferStatus.Ready) throw new InvalidOperationException("operating input inventory refused: " + status);
            }
            return new OperatingInputEvaluation(facts, intervals, draw, blocker);
        }
        private static ResourceInventoryIntent Intent(Entity entity, Bill draw)
            => new ResourceInventoryIntent(new ReservationOwner("operating.inputs", entity.Id.Value), entity.Owner, entity.Id,
                draw, ResourceFlowKind.Consumed, OperatingInputCodes.Cause);

        private static void ValidateRates(World world, IReadOnlyList<IResourceRate> rates, ICapacityPolicy capacity)
        {
            if (rates == null || capacity == null) throw new InvalidOperationException("operating input metadata requires actual rates and capacity");
            foreach (var rate in rates)
                if (rate?.Resource == null || !ReferenceEquals(world.Content.Resource(rate.Resource.Resource), rate.Resource))
                    throw new InvalidOperationException("operating input rate is not actual session resource content");
            ResourceRates.Accumulated(rates, world.Time, capacity, WorkProgress.Zero, ResourceRateRounding.Ceiling);
        }

        internal static bool Commit(World world, Entity entity, OperatingPhase phase)
        {
            if (!Active(world, entity, phase)) return false;
            var value = Read(world, entity, phase);
            if (!value.Blocker.IsNone) return false;
            if (value.Providers.Count == 0) return true;
            foreach (var fact in value.Providers)
                if (fact.Exposure.UpdatedTick == world.Tick) throw new InvalidOperationException("operating inputs already advanced this tick");
            if (!world.Services.Get<IResourceTransaction>().CommitInventory(Intent(entity, value.RequiredInputs)).Committed)
                throw new InvalidOperationException("whole operating input draw changed after admission");
            var store = entity.Get<IInventory>(); var stock = new List<ResourceAmount>(store.Stock);
            var reservations = new List<ResourceReservation>(store.Reservations);
            foreach (var fact in value.Providers)
            {
                var provider = entity.Get<IOperatingInputs>(fact.Demand.Consumer);
                var context = new OperatingInputsContext(world, entity, fact.Demand.Consumer, phase, fact.Exposure);
                var owner = entity.Owner; var nextIdentity = world.Entities.NextIdentity; int version = entity.Version;
                provider.AdvanceExposure(context);
                var after = EntityObservation.Read(world, entity, provider.ReadExposure);
                if (after.ElapsedTicks != context.After.ElapsedTicks || after.UpdatedTick != context.After.UpdatedTick ||
                    entity.Version <= version || entity.Owner != owner || world.Tick != context.Tick || world.Entities.NextIdentity != nextIdentity ||
                    !ReferenceEquals(world.Entities.Get(entity.Id), entity) || !ReferenceEquals(entity.Get<IOperatingInputs>(fact.Demand.Consumer), provider))
                    throw new InvalidOperationException("operating provider did not commit exact admitted exposure");
                if (store.Stock.Count != stock.Count || store.Reservations.Count != reservations.Count)
                    throw new InvalidOperationException("operating exposure changed inventory");
                for (int i = 0; i < stock.Count; i++)
                    if (!store.Stock[i].Equals(stock[i])) throw new InvalidOperationException("operating exposure changed resource stock");
                for (int i = 0; i < reservations.Count; i++)
                {
                    var a = store.Reservations[i]; var b = reservations[i];
                    if (!a.Token.Equals(b.Token) || !a.Owner.Equals(b.Owner) || a.Resource != b.Resource || a.Amount != b.Amount || a.Direction != b.Direction)
                        throw new InvalidOperationException("operating exposure changed native promises");
                }
            }
            return true;
        }
        internal static void ValidateWorkers(World world)
        {
            foreach (Entity entity in world.Entities.All)
                if (entity.Alive && entity.Has<IWorker>() && HasInputs(entity))
                {
                    Read(world, entity, OperatingPhase.Idle); Read(world, entity, OperatingPhase.Standby);
                }
        }
        internal static void ValidateDefinitions(World world)
        {
            ExecutionRateContracts.ValidateDefinitions<IOperatingInputs, IOperatingInputsDefinition, IInventory>(world);
            foreach (var entity in world.Content.All<EntityDef>()) foreach (var part in entity.Components)
                if (part is IOperatingInputsDefinition definition)
                {
                    EntityObservation.ReadProvider(world, definition, null, () =>
                    {
                        bool stateful = typeof(IStatefulOperatingInputs).IsAssignableFrom(part.ComponentType);
                        if (stateful != (definition is IStatefulOperatingInputsDefinition))
                            throw new ContentException(entity.Id + ": reusable operating inputs require paired stateful metadata/runtime contracts");
                        if (definition is IStatefulOperatingInputsDefinition statesDefinition)
                        {
                            var states = statesDefinition.States;
                            if (states == null || states.Definitions.Count != 1 || !states.Definitions.TryGetValue("disabled", out var state) ||
                                state == null || !world.Content.Owns(state) || !ReferenceEquals(world.Content.Find<IEntityStateDefinition>(state.Id), state))
                                throw new ContentException(entity.Id + ": operating disabled mode must reference the actual owned state catalog");
                        }
                    bool motion = definition.Phase == OperatingPhase.Movement;
                    if (!IsValidPhase(definition.Phase) || (motion ? !entity.HasCapability<IMotion>() : !entity.HasCapability<IWorker>()) ||
                        !motion && !world.Services.TryGet<IWorkerDispatch>(out _))
                        throw new ContentException(entity.Id + ": operating phase requires its actual execution target/module");
                    var inventory = entity.Component<IInventoryDefinition>();
                    if (!entity.HasCapability<IInventory>() || inventory == null || !world.Services.TryGet<IResourceTransaction>(out _))
                        throw new ContentException(entity.Id + ": operating inputs require actual inventory metadata and transaction service");
                    try
                    {
                        ValidateRates(world, definition.Inputs, inventory.CapacityPolicy);
                        foreach (var rate in definition.Inputs)
                            if (!inventory.Accepts(world.Content, rate.Resource.Resource)) throw new ContentException(entity.Id + ": inventory refuses authored operating input");
                    }
                    catch (ArgumentException error) { throw new ContentException(entity.Id + ": " + error.Message); }
                    catch (InvalidOperationException error) { throw new ContentException(entity.Id + ": " + error.Message); }
                        return true;
                    });
                }
        }
    }
    /// <summary>Sole JSON declaration for reusable operating inputs. Contains no stock or exposure.</summary>
    public sealed class OperatingInputsDeclaration
    {
        private readonly ResourceRateSpec _inputs;
        public Code CapabilityId { get; }
        public OperatingPhase Phase { get; }
        public IReadOnlyList<IResourceRate> Inputs { get; private set; }
        public EntityStateBindings States { get; private set; }
        private OperatingInputsDeclaration(JsonReader reader)
        {
            string id = reader.String("id");
            try { ContractIdentity.ValidateId(id, "operating input capability"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            CapabilityId = new Code(id); string phase = reader.String("phase");
            Phase = phase == "movement" ? OperatingPhase.Movement : phase == "idle" ? OperatingPhase.Idle : phase == "standby" ? OperatingPhase.Standby : throw reader.Error("phase must explicitly be movement, idle or standby");
            _inputs = ResourceRateSpec.Read(reader, "inputs"); States = EntityStateBindings.Read(reader, "disabled");
        }
        public static OperatingInputsDeclaration Read(JsonReader reader)
            => new OperatingInputsDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content) { Inputs = _inputs.Resolve(content); States = States.Resolve(content); }
        public void ValidateBinding(EntityDef owner)
        {
            if (owner == null || !owner.HasCapability<IInventory>() || owner.Component<IInventoryDefinition>() == null ||
                (Phase == OperatingPhase.Movement ? !owner.HasCapability<IMotion>() : !owner.HasCapability<IWorker>()))
                throw new ContentException(owner?.Id + ": operating inputs require actual phase target and inventory");
        }
    }
    public sealed class OperatingInputsDef : ComponentDef<OperatingInputs>, IStatefulOperatingInputsDefinition
    {
        public bool BindsEntityInventory => true;
        public ICapacityPolicy CapacityPolicy => Owner.Component<IInventoryDefinition>().CapacityPolicy;
        public bool Accepts(ResolvedContent content, ResourceId resource)
        {
            foreach (var rate in Inputs) if (rate.Resource.Resource == resource) return Owner.Component<IInventoryDefinition>().Accepts(content, resource);
            return false;
        }
        private OperatingInputsDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        public OperatingPhase Phase => _declaration.Phase;
        public IReadOnlyList<IResourceRate> Inputs => _declaration.Inputs;
        public EntityStateBindings States => _declaration.States;
        protected override void ReadCore(JsonReader reader) => _declaration = OperatingInputsDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateCore(EntityDef owner) => _declaration.ValidateBinding(owner);
    }
    /// <summary>Accepted exposure for one authored phase only. Actual resources stay in IInventory.</summary>
    public sealed class OperatingInputs : Component, IStatefulOperatingInputs
    {
        public new IStatefulOperatingInputsDefinition Def => (IStatefulOperatingInputsDefinition)base.Def;
        private long _elapsed, _updated = -1;
        public Code CapabilityId => Def.CapabilityId;
        public ResourcePortResolution ResolveStore(World world, ResourceEndpointId? original = null)
            => original.HasValue && original.Value != ResourceEndpointId.ForEntity(Entity.Id)
                ? ResourcePortResolution.Unavailable(ResourcePortCodes.OriginalUnavailable):ResourcePortResolution.ReadyStore(Entity.Get<IInventory>());
        public IResourcePort ResolveUtilityPort(World world, ResourceId resource)
        {
            foreach (var rate in Def.Inputs) if (rate.Resource.Resource == resource) return this;
            return null;
        }
        public UtilityDemandSnapshot ReadUtilityDemand(World world) => OperatingInputsContracts.ReadDemand(world, this);
        public OperatingExposureSnapshot ReadExposure() => new OperatingExposureSnapshot(_elapsed, _updated);
        public void AdvanceExposure(OperatingInputsContext context)
        {
            if (!ReferenceEquals(context.Entity, Entity) || context.Source != CapabilityId || context.World == null || context.Tick != context.World.Tick ||
                context.Phase != Def.Phase || context.Before.ElapsedTicks != _elapsed || context.Before.UpdatedTick != _updated || _updated == context.Tick)
                throw new InvalidOperationException("operating exposure requires current engine admission");
            _elapsed = context.After.ElapsedTicks; _updated = context.After.UpdatedTick; Entity.Touch();
        }
        public IEntityStateDefinition GetState(EntityUpdateContext context)
        {
            bool active = Def.Phase == OperatingPhase.Movement ? Entity.Has<IMotion>() && MotionExecutionContracts.ReadStatus(context.World, Entity) == MoveStatus.Moving :
                OperatingInputsContracts.CurrentWorkerPhase(context.World, Entity) == Def.Phase;
            return !active || OperatingInputsContracts.Read(context.World, Entity, Def.Phase).Blocker.IsNone ? null : Def.States["disabled"];
        }
        public override void Sync(IStateSync sync)
        {
            long elapsed = _elapsed, updated = _updated; sync.Long(ref elapsed); sync.Long(ref updated);
            if (!OperatingInputsContracts.IsStructurallyValid(new OperatingExposureSnapshot(elapsed, updated)))
                throw new CorruptStateException("invalid saved operating exposure");
            if (sync.Reading) { _elapsed = elapsed; _updated = updated; }
        }
    }
}
