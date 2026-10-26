using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Independent engine-side timed entity emitter, not a subclass/proxy of Transformer.</summary>
    public sealed class PulseTransformerDef : ComponentDef<PulseTransformer>, ITransformerDefinition
    {
        private string[] _ids;
        public string QueueFault { get; private set; }
        public string SnapshotRandomFault { get; private set; }
        public int OperationSchemaVersion => 1;
        public OperationSettings ControlSettings { get; private set; }
        public Code CapabilityId => ControlSettings.Id;
        public IReadOnlyList<ITransformationDefinition> Transformations { get; private set; }
        public bool AutomaticSupply => false;
        public EntityStateBindings States { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            ControlSettings = OperationSettings.Read(reader.Child("operation"));
            QueueFault = reader.String("queue_fault", "none");
            SnapshotRandomFault = reader.String("snapshot_random_fault", "none");
            if (SnapshotRandomFault != "none" && SnapshotRandomFault != "rng" && SnapshotRandomFault != "rng-restore" && SnapshotRandomFault != "rng-same")
                throw reader.Error("unknown snapshot random probe");
            if (QueueFault != "none" && QueueFault != "cancel-pause" && QueueFault != "cancel-speed" &&
                QueueFault != "cancel-create" && QueueFault != "cancel-throw" && QueueFault != "cancel-exposure" &&
                QueueFault != "cancel-reduce-exposure" && QueueFault != "append-exposure" && QueueFault != "append-charge" &&
                QueueFault != "cancel-charge" && QueueFault != "cancel-credit" && QueueFault != "append-admission-charge" &&
                QueueFault != "cancel-admission-charge") throw reader.Error("unknown queue context probe");
            _ids = reader.Strings("transformations");
            if (_ids.Length == 0) throw reader.Error("explicit pulse transformations required");
            if (ControlSettings.Initial.Unit != new Code("unit.work-rate") || !ControlSettings.Initial.Minimum.IsPositive)
                throw reader.Error("pulse requires positive work-rate controls");
            States = EntityStateBindings.Read(reader, "working", "stopped");
        }
        protected override void ResolveCore(ContentResolver content)
        {
            var definitions = new List<ITransformationDefinition>();
            foreach (string id in _ids)
            {
                ITransformationDefinition definition = content.Def<ITransformationDefinition>(id);
                if (!definition.Inputs.IsEmpty || !definition.Outputs.IsEmpty || definition.EntityOutput == null || definition.Cash.IsPositive)
                    throw content.Fail("pulse accepts only explicitly free entity-emission transformations");
                definitions.Add(definition);
            }
            Transformations = definitions.AsReadOnly(); States = States.Resolve(content);
        }
    }
    public sealed class PulseTransformer : Component<PulseTransformerDef>, ITransformer, IUtilityConsumer, IEntityStateSource, ITransformationGate, ITransformationStaffingLifecycle
    {
        private readonly List<TransformationQueueEntry> _queue = new List<TransformationQueueEntry>();
        private WorkProgress _progress;
        private long _operatingExposure;
        private World _session;
        private long _nextExecution = 1;
        private TransformationPhase _phase;
        private Code _blocker;
        private bool _enabled;
        private Fix _target;
        public int FaultMode;
        // Explicit transient fault/entry instrumentation; no gameplay or codec state.
        public Action<TransformationExecutionContext> BeforeAdvanceProbe, AfterAdvanceProbe;
        public Action<World, ITransformer> StaffingProbe;
        public Action<World, string> PureReadProbe;
        public void PrepareStaffing(World world, ITransformer processor) => StaffingProbe?.Invoke(world, processor);
        public Code CapabilityId => Def.CapabilityId;
        public Code CheckRemoval(World world) => TransformationContracts.CheckRemoval(world, this);
        public override void OnCreate() { _enabled = Def.ControlSettings.Initial.Enabled; _target = Def.ControlSettings.Initial.Target; }
        public OperationControlSnapshot ReadControl() => Def.ControlSettings.Snapshot(_enabled, _target);
        public Code CheckControl(bool enabled, Fix target) => Def.ControlSettings.Check(target);
        public void Configure(bool enabled, Fix target)
        {
            if (!CheckControl(enabled, target).IsNone) throw new InvalidOperationException("invalid pulse control");
            _enabled = enabled; _target = target; Entity.Touch();
        }
        public OperationSnapshot ReadOperation(World world)
            => !_enabled ? new OperationSnapshot(OperationStatus.Disabled, OperationCodes.Disabled)
                : new OperationSnapshot(_queue.Count > 0 ? OperationStatus.Running : OperationStatus.Idle, _blocker);
        public TransformationSnapshot ReadTransformation(World world)
        {
            _session = world;
            PureReadProbe?.Invoke(world, "snapshot");
            if (world.Clock.Paused && world.Clock.StepIndex > 0)
            {
                if (Def.SnapshotRandomFault == "rng") world.Random.NextULong();
                if (Def.SnapshotRandomFault == "rng-restore") { ulong saved = world.Random.State; world.Random.NextULong(); world.Random.State = saved; }
                if (Def.SnapshotRandomFault == "rng-same") world.Random.State = world.Random.State;
            }
            if (FaultMode == 1) Entity.Touch();
            if (FaultMode == 9) _enabled = !_enabled; // Negative hidden persisted mutation probe.
            return new TransformationSnapshot(_phase, _progress, _blocker, _queue,
                _queue.Count == 0 ? null : world.Content.Get<ITransformationDefinition>(_queue[0].Transformation), world.Time,
                TransformationWipSnapshot.None, TransformationToolsSnapshot.None, ReadUtilityDemand(world), TransformationFlowSnapshot.None, _nextExecution);
        }
        public IResourcePort ResolveUtilityPort(World world, ResourceId resource) => null;
        public UtilityDemandSnapshot ReadUtilityDemand(World world)
            => new UtilityDemandSnapshot(CapabilityId, _operatingExposure, Array.Empty<ResourcePortSnapshot>(), false,
                Bill.Empty, Bill.Empty, Bill.Empty, Code.None);
        public Code CheckEnqueue(ITransformationDefinition transformation, int count, bool repeat, EntityId home)
        {
            PureReadProbe?.Invoke(_session, "enqueue");
            if (Def.QueueFault == "append-admission-charge" && _operatingExposure > 0 && _session.Clock.Paused)
                _session.Services.Get<IEconomy>().Charge(Entity.Owner, Fix.One, new Code("proof.illegal-queue-cost"));
            if (FaultMode == 2) Entity.Touch();
            if (FaultMode == 8) _enabled = !_enabled; // Negative pure-admission mutation probe.
            if (count < 1 || (long)_queue.Count + (repeat ? 1 : count) > int.MaxValue) return Codes.InvalidArgument;
            foreach (ITransformationDefinition definition in Def.Transformations) if (ReferenceEquals(definition, transformation)) return Code.None;
            return Codes.UnknownDef;
        }
        public void Enqueue(ITransformationDefinition transformation, int count, bool repeat, EntityId home)
        {
            if (FaultMode == 13) return; // Negative missing append probe.
            if (!CheckEnqueue(transformation, count, repeat, home).IsNone) throw new InvalidOperationException("pulse admission changed");
            for (int i = 0; i < (repeat ? 1 : count); i++) _queue.Add(new TransformationQueueEntry(_nextExecution++, transformation.Id, repeat, home));
            if (Def.QueueFault == "append-exposure" && _operatingExposure > 0) _operatingExposure = checked(_operatingExposure + 1);
            if (Def.QueueFault == "append-charge" && _operatingExposure > 0)
                _session.Services.Get<IEconomy>().Charge(Entity.Owner, Fix.One, new Code("proof.illegal-queue-cost"));
            if (FaultMode == 10) _enabled = !_enabled;
            if (FaultMode == 11) _queue.Add(new TransformationQueueEntry(_nextExecution++, transformation.Id, repeat, home));
            if (FaultMode == 12 && _queue.Count > 1) _queue[0] = new TransformationQueueEntry(_queue[0].Execution, transformation.Id, true, home);
            if (FaultMode == 15) Entity.Get<IControllableOperation>(new Code("transformation")).Configure(false, Fix.One);
            Entity.Touch();
            if (FaultMode == 14) throw new InvalidOperationException("negative interrupted committed append");
        }
        public Code CheckCancel(int index)
        {
            PureReadProbe?.Invoke(_session, "cancel");
            if (Def.QueueFault == "cancel-admission-charge" && _operatingExposure > 0 && _session.Clock.Paused)
                _session.Services.Get<IEconomy>().Charge(Entity.Owner, Fix.One, new Code("proof.illegal-queue-cost"));
            if (FaultMode == 16) _enabled = !_enabled; // Negative hidden cancellation-admission mutation.
            return index < 0 || index >= _queue.Count ? Codes.InvalidArgument : Code.None;
        }
        public ExecutionGateSnapshot ReadGate(World world, ITransformer processor, ITransformationDefinition transformation)
        {
            if (FaultMode == 4) Entity.Touch();
            if (FaultMode == 5) return null;
            if (FaultMode == 6) return new ExecutionGateSnapshot(new Code("wrong-gate"), true, Code.None, Array.Empty<ExecutionGateConditionSnapshot>());
            if (FaultMode == 7) return new ExecutionGateSnapshot(CapabilityId, false, new Code("pulse.invalid-inapplicable"), Array.Empty<ExecutionGateConditionSnapshot>());
            return new ExecutionGateSnapshot(CapabilityId, true, _enabled ? Code.None : new Code("pulse.utility-offline"),
                Array.Empty<ExecutionGateConditionSnapshot>());
        }
        public void Cancel(World world, int index)
        {
            if (!CheckCancel(index).IsNone) throw new InvalidOperationException("pulse cancellation changed");
            if (Def.QueueFault == "cancel-charge") world.Services.Get<IEconomy>().Charge(Entity.Owner, Fix.One, new Code("proof.illegal-queue-cost"));
            if (Def.QueueFault == "cancel-credit") world.Services.Get<IEconomy>().Credit(Entity.Owner, Fix.One, new Code("proof.illegal-queue-refund"));
            if (FaultMode == 17) return;
            _queue.RemoveAt(index); if (index == 0) { _progress = WorkProgress.Zero; _operatingExposure = 0; _phase = TransformationPhase.Idle; _blocker = Code.None; }
            if (index > 0 && Def.QueueFault == "cancel-exposure") _operatingExposure = checked(_operatingExposure + 1);
            if (index > 0 && Def.QueueFault == "cancel-reduce-exposure" && _operatingExposure > 1) _operatingExposure--;
            if (FaultMode == 18 && _queue.Count > 0) _queue.RemoveAt(0);
            if (FaultMode == 19 && _queue.Count > 0)
            {
                var entry = _queue[0]; _queue[0] = new TransformationQueueEntry(entry.Execution, entry.Transformation, !entry.Repeat, entry.Home);
            }
            if (FaultMode == 20) _enabled = !_enabled;
            if (FaultMode == 22) Entity.Get<IControllableOperation>(new Code("transformation")).Configure(false, Fix.One);
            if (FaultMode == 23 && index > 0) _progress = WorkProgress.Zero;
            Entity.Touch();
            if (FaultMode == 21) throw new InvalidOperationException("negative interrupted committed cancellation");
            if (Def.QueueFault == "cancel-pause") ((Hosting.GameClock)world.Clock).Pause();
            if (Def.QueueFault == "cancel-speed") ((Hosting.GameClock)world.Clock).Speed = 2;
            if (Def.QueueFault == "cancel-create") {
                Code refusal = world.PrepareCreation(world.Content.Get<EntityDef>("token"), Entity.Owner, Entity.Id, null, out var creation);
                if (!refusal.IsNone) throw new InvalidOperationException("queue identity probe creation refused " + refusal);
                var result = world.Services.Get<IResourceTransaction>().CommitConversion(new ResourceConversionIntent(
                    new ReservationOwner("proof.queue-context",Entity.Id.Value),Entity.Owner,
                    Array.Empty<ResourceConversionInput>(),Array.Empty<ResourceProductionIntent>(),new Code("pulse")),creation);
                if (!result.Committed) throw new InvalidOperationException("queue identity probe failed actual creation");
            }
            if (Def.QueueFault == "cancel-throw") throw new InvalidOperationException("proof.original-queue-cancellation-error");
        }
        public void Advance(TransformationExecutionContext context)
        {
            BeforeAdvanceProbe?.Invoke(context);
            AdvanceCurrent(context);
            AfterAdvanceProbe?.Invoke(context);
        }
        private void AdvanceCurrent(TransformationExecutionContext context)
        {
            if (FaultMode == 3) throw new InvalidOperationException("declared pulse execution fault");
            if (!_enabled) { _blocker = OperationCodes.Disabled; return; }
            _blocker = Code.None;
            if (_queue.Count == 0) { _phase = TransformationPhase.Idle; return; }
            ITransformationDefinition transformation = context.World.Content.Get<ITransformationDefinition>(_queue[0].Transformation);
            _blocker = context.CheckGates(this, transformation); if (!_blocker.IsNone) return;
            _phase = TransformationPhase.Working;
            if (!_progress.Complete(context.World.Time.Ticks(transformation.Duration)))
            {
                _progress = _progress.Advance(_target, context.World.Time.Ticks(transformation.Duration));
                _operatingExposure = checked(_operatingExposure + 1); Entity.Touch();
            }
            if (!_progress.Complete(context.World.Time.Ticks(transformation.Duration))) return;
            // Real result creation is engine-side and timed; no harness-produced completion counter.
            _blocker = TransformationContracts.CheckOutputHome(context.World, this, transformation, _queue[0].Home);
            if (!_blocker.IsNone) { _phase = TransformationPhase.Blocked; return; }
            _blocker = EntityRollout.Find(context.World, Entity, transformation.EntityOutput, out SpatialPlacement placement);
            if (!_blocker.IsNone) { _phase = TransformationPhase.Blocked; return; }
            _blocker = context.World.PrepareCreation(transformation.EntityOutput, Entity.Owner, Entity.Id, placement, out IEntityCreation creation);
            if (!_blocker.IsNone) { _phase = TransformationPhase.Blocked; return; }
            var conversion = new ResourceConversionIntent(new ReservationOwner("proof.pulse", Entity.Id.Value), Entity.Owner,
                Array.Empty<ResourceConversionInput>(), Array.Empty<ResourceProductionIntent>(), new Code(transformation.Id));
            ResourceInventoryResult completed = context.World.Services.Get<IResourceTransaction>().CommitConversion(conversion, creation);
            if (!completed.Committed) { _blocker = completed.Blocker; _phase = TransformationPhase.Blocked; return; }
            Entity result = context.World.Entities.Get(completed.CreatedEntity);
            TransformationContracts.BindOutputHome(context.World, this, transformation, _queue[0].Home, result);
            context.World.Events.Publish(new EntityTransformed { Entity = result.Id, Transformer = Entity.Id, Owner = Entity.Owner });
            TransformationQueueEntry done = _queue[0]; _queue.RemoveAt(0); if (done.Repeat) _queue.Add(new TransformationQueueEntry(_nextExecution++, done.Transformation, done.Repeat, done.Home));
            _progress = WorkProgress.Zero; _operatingExposure = 0; _phase = TransformationPhase.Idle;
        }
        public IEntityStateDefinition GetState(EntityUpdateContext context)
            => !_enabled ? Def.States["stopped"] : _phase == TransformationPhase.Working ? Def.States["working"] : null;
        public override void Sync(IStateSync sync)
        {
            sync.Bool(ref _enabled); sync.Fix(ref _target); sync.Work(ref _progress); sync.Enum(ref _phase);
            sync.Long(ref _operatingExposure);
            string blocker = _blocker.Id; sync.String(ref blocker); if (sync.Reading) _blocker = blocker == null ? Code.None : new Code(blocker);
            if (!CheckControl(_enabled, _target).IsNone) throw new CorruptStateException("invalid pulse control state");
            sync.Long(ref _nextExecution);
            int count = sync.Count(_queue.Count); if (sync.Reading) _queue.Clear();
            for (int i = 0; i < count; i++)
            {
                string id = sync.Reading ? null : _queue[i].Transformation;
                bool repeat = !sync.Reading && _queue[i].Repeat; EntityId home = sync.Reading ? default : _queue[i].Home;
                long execution = sync.Reading ? 0 : _queue[i].Execution; sync.Long(ref execution);
                sync.String(ref id); sync.Bool(ref repeat); sync.Entity(ref home);
                if (sync.Reading) _queue.Add(new TransformationQueueEntry(execution, id, repeat, home));
            }
        }
    }
    public sealed class TransformerProofModule : GameModule
    {
        public override string Id => "proof.transformer";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(TransformationModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types) => types.Component<PulseTransformerDef>("pulse_transformer", 3);
    }
}
