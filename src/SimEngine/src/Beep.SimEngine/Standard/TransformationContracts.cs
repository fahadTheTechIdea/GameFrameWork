using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public enum TransformationOutputHome : byte { Unassigned = 1, Producer = 2, Required = 3 }
    public enum TransformationMode : byte { Batch, Continuous }
    /// <summary>Admission and verified queue mutations; actual queues remain in named ITransformer providers.</summary>
    public interface ITransformationQueue
    {
        Code CheckEnqueue(PlayerId company, EntityId entity, Code capability, string transformation, int count, bool repeat, EntityId home);
        Code Enqueue(PlayerId company, EntityId entity, Code capability, string transformation, int count, bool repeat, EntityId home);
        Code CheckCancel(PlayerId company, EntityId entity, Code capability, int index);
        Code Cancel(PlayerId company, EntityId entity, Code capability, int index);
    }
    /// <summary>Engine-owned accepted work uses physical eligibility, never player metadata visibility. Same queue authority.</summary>
    public interface ITransformationOperationQueue
    {
        Code CheckEnqueueOperation(PlayerId company, EntityId entity, Code capability, string transformation, int count, bool repeat, EntityId home);
        Code EnqueueOperation(PlayerId company, EntityId entity, Code capability, string transformation, int count, bool repeat, EntityId home);
    }
    public interface ITransformationDefinition : IContentDefinition
    {
        string Category { get; }
        TransformationMode Mode { get; }
        IReadOnlyList<IResourceRate> InputRates { get; }
        IReadOnlyList<IResourceRate> OutputRates { get; }
        Bill Inputs { get; }
        Bill Tools { get; }
        Bill ToolConsumption { get; }
        IReadOnlyList<IResourceRate> ToolConsumptionRates { get; }
        Bill StartResources { get; }
        Bill Outputs { get; }
        IReadOnlyList<IResourceOutputQualityRule> OutputQualities { get; }
        IReadOnlyList<ResourcePortRoute> OutputPorts { get; }
        IReadOnlyList<ResourcePortRoute> InputPorts { get; }
        IReadOnlyList<ResourcePortRoute> ToolPorts { get; }
        IReadOnlyList<ResourcePortRoute> OperatingPorts { get; }
        IReadOnlyList<IResourceRate> OperatingInputs { get; }
        EntityDef EntityOutput { get; }
        TransformationOutputHome? OutputHome { get; }
        GameDuration Duration { get; }
        Fix Cash { get; }
        Code CancelPolicy { get; }
    }
    public interface ITransformerDefinition : IOperationDefinition
    {
        IReadOnlyList<ITransformationDefinition> Transformations { get; }
        bool AutomaticSupply { get; }
    }
    public interface ITransformerWipDefinition : ITransformerDefinition
    {
        ICapacityPolicy WipCapacity { get; }
        ResolvedContent Content { get; }
        EntityStateBindings States { get; }
        ITransformationDefinition IdleTransformation { get; }
    }
    /// <summary>Actual provider-owned raw material buffer; views expose only detached WIP facts.</summary>
    public interface ITransformerWip : ITransformer
    {
        IResourceOutlet WorkInProgress { get; }
        ReservationOwner WorkInProgressOwner { get; }
    }
    /// <summary>Actual reusable material commitments, owned by the existing execution and endpoint book.</summary>
    public interface ITransformerTools : ITransformerWip
    {
        IReadOnlyList<TransformationPortLink> ToolSources { get; }
    }
    public interface ITransformer : IControllableOperation, IEntityRemovalGuard
    {
        TransformationSnapshot ReadTransformation(World world);
        Code CheckEnqueue(ITransformationDefinition transformation, int count, bool repeat, EntityId home);
        /// <summary>Append the exact resolved home supplied by the shared engine queue authority, not a default-home request.</summary>
        void Enqueue(ITransformationDefinition transformation, int count, bool repeat, EntityId home);
        Code CheckCancel(int index);
        void Cancel(World world, int index);
        void Advance(TransformationExecutionContext context);
    }
    /// <summary>Optional engine-side physical/utility/worker gate; no host callback.</summary>
    public interface ITransformationGate : INamedEntityCapability
    {
        ExecutionGateSnapshot ReadGate(World world, ITransformer processor, ITransformationDefinition transformation);
    }
    /// <summary>Optional gate authoring declares selected engine services without requiring them for unstaffed recipes.</summary>
    public interface ITransformationCompositionRequirement
    {
        void ValidateComposition(World world, EntityDef owner);
    }
    public sealed class TransformationExecutionContext
    {
        internal TransformationExecutionContext(World world, TickInfo tick) { World = world; Tick = tick; }
        public World World { get; }
        public TickInfo Tick { get; }
        public Code CheckGates(ITransformer processor, ITransformationDefinition transformation)
        {
            return TransformationContracts.ReadGates(World, processor, transformation).Blocker;
        }
    }
    public readonly struct TransformationQueueEntry
    {
        public TransformationQueueEntry(long execution, string transformation, bool repeat, EntityId home)
        {
            ContractIdentity.ValidateId(transformation, "queued transformation");
            if (execution <= 0) throw new ArgumentOutOfRangeException(nameof(execution));
            Execution = execution; Transformation = transformation; Repeat = repeat; Home = home;
        }
        public long Execution { get; }
        public string Transformation { get; }
        public bool Repeat { get; }
        public EntityId Home { get; }
    }
    /// <summary>Detached provider facts, never a queue/progress execution handle.</summary>
    public readonly struct TransformationSnapshot
    {
        public TransformationSnapshot(TransformationPhase phase, WorkProgress progress, Code blocker,
            IReadOnlyList<TransformationQueueEntry> queue, ITransformationDefinition head, ISimulationTime time,
            TransformationWipSnapshot wip, TransformationToolsSnapshot tools, UtilityDemandSnapshot operating, TransformationFlowSnapshot flow, long nextExecution)
        {
            if (queue == null || time == null) throw new ArgumentNullException();
            if (nextExecution <= 0) throw new ArgumentOutOfRangeException(nameof(nextExecution));
            NextExecution = nextExecution;
            var copy = new TransformationQueueEntry[queue.Count];
            for (int i = 0; i < copy.Length; i++) copy[i] = queue[i];
            Queue = Array.AsReadOnly(copy); Phase = phase; Progress = progress; Blocker = blocker;
            Transformation = head?.Id; EntityOutput = head?.EntityOutput?.Id;
            Required = head == null ? 0 : time.Ticks(head.Duration);
            Wip = wip;
            Tools = tools;
            Operating = operating;
            Flow = flow; Mode = head?.Mode;
        }
        public TransformationPhase Phase { get; }
        public WorkProgress Progress { get; }
        public long Required { get; }
        public Code Blocker { get; }
        public IReadOnlyList<TransformationQueueEntry> Queue { get; }
        public int QueueCount => Queue.Count;
        public long NextExecution { get; }
        public string Transformation { get; }
        public string EntityOutput { get; }
        public TransformationWipSnapshot Wip { get; }
        public TransformationToolsSnapshot Tools { get; }
        public UtilityDemandSnapshot Operating { get; }
        public TransformationFlowSnapshot Flow { get; }
        public TransformationMode? Mode { get; }
    }
    public readonly struct TransformationWipSnapshot
    {
        public static TransformationWipSnapshot None => new TransformationWipSnapshot(default, 0, 0, Bill.Empty, Array.Empty<TransformationPortLink>());
        public ResourceEndpointId Endpoint { get; }
        public long Execution { get; }
        public int Version { get; }
        public Bill Stock { get; }
        public IReadOnlyList<TransformationPortLink> Sources { get; }
        public TransformationWipSnapshot(ResourceEndpointId endpoint, long execution, int version, Bill stock, IReadOnlyList<TransformationPortLink> sources)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            Endpoint = endpoint; Execution = execution; Version = version; Stock = stock;
            Sources = new List<TransformationPortLink>(sources).AsReadOnly();
        }
    }
    public readonly struct TransformationToolsSnapshot
    {
        public static TransformationToolsSnapshot None => new TransformationToolsSnapshot(
            Array.Empty<TransformationPortLink>(), Array.Empty<ResourceReservation>(), Bill.Empty);
        public IReadOnlyList<TransformationPortLink> Sources { get; }
        public IReadOnlyList<ResourceReservation> Receipts { get; }
        public Bill Held { get; }
        public TransformationToolsSnapshot(IReadOnlyList<TransformationPortLink> sources,
            IReadOnlyList<ResourceReservation> receipts, Bill held)
        {
            if (sources == null || receipts == null || held == null) throw new ArgumentNullException();
            Sources = new List<TransformationPortLink>(sources).AsReadOnly();
            Receipts = new List<ResourceReservation>(receipts).AsReadOnly(); Held = held;
        }
    }
    public static partial class TransformationContracts
    {
        /// <summary>One pure retirement rule for native and independent processors. Active requests,
        /// including material return, must settle through their existing queue before retirement.</summary>
        public static Code CheckRemoval(World world, ITransformer provider)
        {
            SupplySource(world, provider);
            TransformationSnapshot state = Read(world, provider);
            // Read validates the actual WIP stock, receipts, tools and execution ownership too;
            // an orphan buffer is corruption, never permission to silently discard material.
            return state.QueueCount != 0 ? TransformerManager.BlockerActive : Code.None;
        }

        internal static ExecutionGateEvaluation ReadGates(World world, ITransformer processor, ITransformationDefinition transformation)
        {
            if (transformation == null || !ReferenceEquals(world.Content.Find<ITransformationDefinition>(transformation.Id), transformation))
                throw new InvalidOperationException("foreign transformation gate definition");
            SupplySource(world, processor);
            return ExecutionGateContracts.Read<ITransformationGate>(world, processor.Entity, gate => gate.ReadGate(world, processor, transformation));
        }

        internal static LogisticsDemandId SupplySource(World world, ITransformer provider)
        {
            Entity entity = provider?.Entity;
            if (entity == null || !entity.Alive || !ReferenceEquals(world.Entities.Get(entity.Id), entity) ||
                provider.CapabilityId.IsNone || !ReferenceEquals(entity.Get<ITransformer>(provider.CapabilityId), provider) ||
                !ReferenceEquals(world.Content.Find<EntityDef>(entity.Def.Id), entity.Def) ||
                Definition(provider).CapabilityId != provider.CapabilityId)
                throw new ArgumentException("supply requires the actual live named transformer of this world", nameof(provider));
            return new LogisticsDemandId(TransformationModule.ModuleId, entity.Id.Value, provider.CapabilityId);
        }
        internal static ITransformerDefinition Definition(ITransformer provider)
            => provider?.Entity?.Def.Component<ITransformerDefinition>(provider.CapabilityId)
                ?? throw new InvalidOperationException("transformer requires its actual named definition metadata");
        internal static TransformationSnapshot Read(World world, ITransformer provider)
            => EntityObservation.ReadProvider(world, provider, null, () => ReadCore(world, provider),
                space: world.Space, observeTargetState: true, faultPhase: "transformations.read");
        private static TransformationSnapshot ReadCore(World world, ITransformer provider)
        {
            int version = provider.Entity.Version;
            TransformationSnapshot snapshot = provider.ReadTransformation(world);
            if (version != provider.Entity.Version) throw new InvalidOperationException("transformer read mutated its entity");
            if (snapshot.Queue == null) throw new InvalidOperationException("transformer returned malformed queue");
            if (snapshot.Phase < TransformationPhase.Idle || snapshot.Phase > TransformationPhase.Returning)
                throw new CorruptStateException("transformer returned invalid phase");
            ITransformationDefinition head = null;
            var executions = new HashSet<long>();
            for (int i = 0; i < snapshot.Queue.Count; i++)
            {
                if (snapshot.Queue[i].Execution <= 0 || snapshot.Queue[i].Execution >= snapshot.NextExecution || !executions.Add(snapshot.Queue[i].Execution))
                    throw new CorruptStateException("invalid or duplicate queue execution identity");
                ITransformationDefinition definition = world.Content.Find<ITransformationDefinition>(snapshot.Queue[i].Transformation);
                if (definition == null || !Supports(provider, definition)) throw new CorruptStateException("unsupported saved transformation");
                if (definition.Mode == TransformationMode.Continuous && snapshot.Queue[i].Repeat) throw new CorruptStateException("continuous stream cannot repeat");
                EntityId home = snapshot.Queue[i].Home;
                if (!IsResolvedOutputHome(definition, home) || home.IsValid && home.Value >= world.Entities.NextIdentity.Value)
                    throw new CorruptStateException("queued output home is not an actual resolved worker-home intent");
                if (i == 0) head = definition;
            }
            if (snapshot.Wip.Execution > 0 && (snapshot.QueueCount == 0 || snapshot.Wip.Execution != snapshot.Queue[0].Execution))
                throw new CorruptStateException("WIP does not belong to its actual queue execution");
            if (snapshot.Transformation != head?.Id || snapshot.EntityOutput != head?.EntityOutput?.Id || snapshot.Mode != head?.Mode
                || snapshot.Required != (head == null ? 0 : world.Time.Ticks(head.Duration)))
                throw new InvalidOperationException("transformer returned mismatched authored head facts");
            if (head == null && (snapshot.Phase != TransformationPhase.Idle || !snapshot.Progress.Equals(WorkProgress.Zero))
                || head != null && head.Mode == TransformationMode.Batch && (snapshot.Progress.WholeTicks > snapshot.Required
                    || snapshot.Progress.WholeTicks == snapshot.Required && snapshot.Progress.Fraction != 0)
                || (snapshot.Phase == TransformationPhase.Idle || snapshot.Phase == TransformationPhase.Supplying) && !snapshot.Progress.Equals(WorkProgress.Zero)
                || snapshot.Phase == TransformationPhase.Blocked && (head?.Mode != TransformationMode.Batch || !snapshot.Progress.Complete(snapshot.Required)))
                throw new CorruptStateException("invalid transformation progress/phase checkpoint");
            ValidateWip(world, provider, snapshot, head);
            ValidateTools(world, provider, snapshot, head);
            ValidateOperatingInputs(world, provider, snapshot, head);
            ValidateFlow(world, provider, snapshot, head);
            return snapshot;
        }
        private static void ValidateWip(World world, ITransformer provider, TransformationSnapshot snapshot, ITransformationDefinition head)
        {
            TransformationWipSnapshot facts = snapshot.Wip;
            if (facts.Stock == null || facts.Sources == null) throw new CorruptStateException("missing explicit WIP facts");
            if (!(provider is ITransformerWip wip))
            {
                if (facts.Endpoint.IsValid || facts.Execution != 0 || facts.Version != 0 || !facts.Stock.IsEmpty || facts.Sources.Count != 0)
                    throw new CorruptStateException("provider without WIP reports material state");
                return;
            }
            int entityVersion = provider.Entity.Version; IResourceOutlet stock = wip.WorkInProgress;
            ReservationOwner owner = wip.WorkInProgressOwner;
            bool active = snapshot.Phase == TransformationPhase.Working || snapshot.Phase == TransformationPhase.Blocked || snapshot.Phase == TransformationPhase.Returning;
            if (stock == null || stock.Endpoint != TransformerManager.WipEndpoint(wip) || facts.Endpoint != stock.Endpoint ||
                stock.Company != provider.Entity.Owner || facts.Version != stock.Version || stock.Version < 0 ||
                active != (facts.Execution > 0) || active && (!owner.IsValid || owner.Identity != facts.Execution ||
                    owner.Scope != TransformerManager.WipOwnerScope(stock.Endpoint)) || !active && owner.IsValid)
                throw new CorruptStateException("invalid WIP endpoint/execution ownership");
            Bill expected = active ? head.Inputs : Bill.Empty;
            int sourceCount = active && head.Mode == TransformationMode.Batch ? head.InputPorts.Count : 0;
            if (facts.Sources.Count != sourceCount) throw new CorruptStateException("WIP source count differs from active authored inputs");
            for (int i = 0; i < sourceCount; i++)
            {
                var source = facts.Sources[i]; var route = head.InputPorts[i];
                if (source.Resource != route.Resource || source.Port != route.Port || !source.Endpoint.IsValid || source.Endpoint == stock.Endpoint)
                    throw new CorruptStateException("WIP original source differs from its authored input route");
            }
            if (facts.Stock.Count != expected.Count || stock.Reservations.Count != expected.Count)
                throw new CorruptStateException("WIP stock/claim count differs from active inputs");
            foreach (var definition in world.Content.All<IResourceContentDefinition>())
                if (stock.Amount(definition.Resource) != expected.AmountOf(definition.Resource) ||
                    facts.Stock.AmountOf(definition.Resource) != expected.AmountOf(definition.Resource))
                    throw new CorruptStateException("WIP contains missing, duplicated or unrelated inputs");
            var resources = new HashSet<ResourceId>();
            foreach (ResourceReservation record in stock.Reservations)
                if (!record.Owner.Equals(owner) || record.Direction != ReservationDirection.Outbound ||
                    record.Token.Endpoint != stock.Endpoint || !resources.Add(record.Resource) ||
                    expected.AmountOf(record.Resource) != record.Amount || !stock.CheckReservedOut(owner, record.Token).Ready)
                    throw new CorruptStateException("WIP material is not protected by its actual execution");
            if (provider.Entity.Version != entityVersion || stock.Version != facts.Version)
                throw new InvalidOperationException("WIP provider mutated a pure material read");
        }
        internal static void ValidateDefinition(ResolvedContent content, EntityDef owner, ITransformerDefinition processor)
        {
            Type runtime = processor.ComponentType;
            if (!typeof(ITransformer).IsAssignableFrom(runtime) || processor.Transformations == null || processor.Transformations.Count == 0)
                throw new ContentException(owner.Id + ": explicit transformer metadata and runtime required");
            if (typeof(ITransformerWip).IsAssignableFrom(runtime) && !(processor is ITransformerWipDefinition))
                throw new ContentException(owner.Id + ": WIP runtime requires its declared metadata contract");
            var wip = processor as ITransformerWipDefinition;
            if (wip != null && (!typeof(ITransformerWip).IsAssignableFrom(runtime) || wip.WipCapacity == null
                || !ReferenceEquals(wip.Content, content) || wip.States == null
                || !ReferenceEquals(content.Find<ICapacityPolicyDefinition>(wip.WipCapacity.Id), wip.WipCapacity)))
                throw new ContentException(owner.Id + ": WIP requires actual owned capacity, context, states and runtime metadata");
            if (wip?.IdleTransformation != null && !Supports(processor, wip.IdleTransformation))
                throw new ContentException(owner.Id + ": idle processing requires its actual supported recipe");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (ITransformationDefinition transformation in processor.Transformations)
            {
                if (transformation == null || !ReferenceEquals(content.Find<ITransformationDefinition>(transformation.Id), transformation)
                    || !ids.Add(transformation.Id))
                    throw new ContentException(owner.Id + ": foreign or duplicate transformation catalog alias");
                if (!transformation.Tools.IsEmpty && !typeof(ITransformerTools).IsAssignableFrom(runtime))
                    throw new ContentException(owner.Id + ": reusable tools require the actual ITransformerTools capability");
                if (transformation.OperatingInputs.Count != 0 && !typeof(IUtilityConsumer).IsAssignableFrom(runtime))
                    throw new ContentException(owner.Id + ": operating inputs require IUtilityConsumer");
                if (transformation.Mode == TransformationMode.Continuous && !typeof(ITransformerFlow).IsAssignableFrom(runtime))
                    throw new ContentException(owner.Id + ": continuous transformations require ITransformerFlow");
                if (wip != null)
                {
                    if (!CapacityPolicyContracts.Check(wip.WipCapacity, Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>(), transformation.Inputs.Lines).Fits)
                        throw new ContentException(owner.Id + ": WIP must hold complete supported native inputs");
                    foreach (ResourceAmount line in transformation.Inputs.Lines)
                        if (CapacityPolicyContracts.Quantum(wip.WipCapacity, line.Resource).Floor(line.Amount) != line.Amount)
                            throw new ContentException(owner.Id + ": WIP input violates its native packing quantum");
                }
                ValidateRecipePorts(content, owner, transformation);
            }
        }
        private static void ValidateRecipePorts(ResolvedContent content, EntityDef owner, ITransformationDefinition transformation)
        {
            ResourcePortContracts.ValidateDefinitionBindings(content, owner, transformation.OperatingPorts, Bill.Empty, transformation.OperatingInputs, false);
            ResourcePortContracts.ValidateDefinitionBindings(content, owner, transformation.ToolPorts, transformation.Tools, Array.Empty<IResourceRate>(), true);
            foreach (var rate in transformation.ToolConsumptionRates)
            {
                foreach (var route in transformation.ToolPorts)
                    if (route.Resource == rate.Resource.Resource &&
                        CapacityPolicyContracts.Quantum(owner.Component<IResourcePortDefinition>(route.Port).CapacityPolicy, route.Resource).Floor(rate.Quantity) != rate.Quantity)
                        throw new ContentException(owner.Id + ": continuous tool consumption violates selected native packing quantum");
            }
            foreach (var route in transformation.ToolPorts)
            {
                Fix consumption = transformation.ToolConsumption.AmountOf(route.Resource);
                var policy = owner.Component<IResourcePortDefinition>(route.Port).CapacityPolicy;
                if (CapacityPolicyContracts.Quantum(policy, route.Resource).Floor(consumption) != consumption)
                    throw new ContentException(owner.Id + ": completion tool consumption violates selected native packing quantum");
            }
            ResourcePortContracts.ValidateDefinitionBindings(content, owner, transformation.InputPorts, transformation.Inputs, transformation.InputRates, false);
            ResourcePortContracts.ValidateDefinitionBindings(content, owner, transformation.OutputPorts, transformation.Outputs, transformation.OutputRates, false);
        }
        internal static bool Supports(ITransformer provider, ITransformationDefinition definition)
        {
            return Supports(Definition(provider), definition);
        }
        internal static bool Supports(ITransformerDefinition metadata, ITransformationDefinition definition)
        {
            foreach (ITransformationDefinition item in metadata.Transformations)
                if (ReferenceEquals(item, definition)) return true;
            return false;
        }
        internal static Code CheckEnqueue(World world, ITransformer provider, ITransformationDefinition definition, int count, bool repeat, EntityId home)
            => EntityObservation.ReadProvider(world, provider, null, () => provider.CheckEnqueue(definition, count, repeat, home),
                space: world.Space, observeTargetState: true, faultPhase: "transformations.admission");
        internal static Code CheckCancel(World world, ITransformer provider, int index)
            => EntityObservation.ReadProvider(world, provider, null, () => provider.CheckCancel(index),
                space: world.Space, observeTargetState: true, faultPhase: "transformations.cancel-admission");
    }
}
