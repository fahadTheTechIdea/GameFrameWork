using System;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public enum WorkerHomeState : byte { Unassigned = 0, Working = 1, Held = 2, Ready = 3, Returning = 4, AtHome = 5, Blocked = 6 }

    /// <summary>Detached intent and derived readiness. Never another home, route or progress owner.</summary>
    public sealed class WorkerHomeView
    {
        public EntityId Worker { get; }
        public EntityId Home { get; }
        public int EntityVersion { get; }
        public int Job { get; }
        public WorkerHomeState State { get; }
        public Code Blocker { get; }
        internal WorkerHomeView(Entity entity, EntityId home, int job, WorkerHomeState state, Code blocker)
        { Worker = entity.Id; Home = home; EntityVersion = entity.Version; Job = job; State = state; Blocker = blocker; }
    }

    /// <summary>Sole stateless home binding/control protocol over actual IWorker and spatial owners.</summary>
    public static partial class WorkerHomeContracts
    {
        public static readonly Code Unsupported = new Code("work.home-unsupported");
        private readonly struct Binding
        {
            public readonly EntityId Home;
            public readonly int Job;
            public readonly bool Held, HeldArrived;
            public Binding(IWorker worker)
            { Home = worker.Home; Job = worker.Job; Held = worker.Held; HeldArrived = worker.HeldArrived; }
        }
        private static Binding ReadBinding(World world, Entity entity)
        {
            var worker = entity?.Get<IWorker>();
            if (world == null || worker == null || !entity.Alive || !ReferenceEquals(world.Entities.Get(entity.Id), entity) || !ReferenceEquals(worker.Entity, entity))
                throw new ArgumentException("home requires the actual live worker in its session");
            var observation = EntityObservation.CaptureProvider(world, worker);
            try
            {
                var value = new Binding(worker);
                if (value.Home.Value < 0 || value.Home.IsValid && value.Home.Value >= world.Entities.NextIdentity.Value ||
                    value.Job < 0 || worker.Idle != (value.Job == 0)) throw new CorruptStateException("invalid worker home or assignment facts");
                return value;
            }
            finally { observation.Verify(); }
        }
        private static T PureTarget<T>(World world, Entity worker, Entity target, Func<T> read)
            => EntityObservation.Read(world, worker, () => EntityObservation.Read(world, target, read));
        /// <summary>Observe the sole worker-owned home intent, including while working or manually held.</summary>
        public static EntityId ReadHome(World world, Entity worker) => ReadBinding(world, worker).Home;
        private static SpatialInteractionStatus Geometry(World world, Entity worker, Entity target)
        {
            if (world.Services.Get<WorkBoard>().ReadHomeParking(target, worker, out _, out var parking, out _)) return parking;
            var status = PureTarget(world, worker, target, () => SpatialInteraction.Check(world, worker, target));
            if ((byte)status > (byte)SpatialInteractionStatus.Unsupported) throw new InvalidOperationException("home interaction returned an invalid status");
            return status;
        }
        private static Code Target(World world, Entity worker, EntityId homeId, out Entity home)
        {
            home = null;
            if (!homeId.IsValid) return homeId == EntityId.None ? Code.None : Codes.InvalidArgument;
            if (homeId == worker.Id) return Codes.InvalidArgument;
            home = world.Entities.Get(homeId);
            if (home == null) return Codes.NotFound;
            if (home.Owner != worker.Owner) return Codes.NotOwned;
            return worker.Get<IMotion>() == null ? Unsupported : Code.None;
        }
        internal static Code CheckOutputDefinition(World world, Beep.SimEngine.Content.EntityDef worker, Entity home, PlayerId owner)
        {
            if (home == null) return Codes.NotFound;
            if (home.Owner != owner) return Codes.NotOwned;
            return EntityObservation.Read(world, home, () =>
            {
                var presence = worker.Component<ISpatialPresenceDefinition>();
                var target = home.Get<ISpatialPresence>();
                if (!worker.HasCapability<IWorker>() || !worker.HasCapability<IMotion>() || presence == null || target == null ||
                    presence.Space != target.Space || world.Space == null || target.Space != world.Space.Id) return Unsupported;
                if (world.Space is ISpatialInteraction) return Code.None;
                var dock = home.Def.Component<IWorkDockDefinition>();
                if (dock != null) foreach (var slot in dock.Slots) if (slot.Kind == WorkDockSlotKind.Parking) return Code.None;
                return Unsupported;
            });
        }
        internal static void BindProduced(World world, Entity worker, EntityId home)
        {
            Code refusal = CheckBinding(world, worker, home);
            if (!refusal.IsNone) throw new InvalidOperationException("produced worker home differs from complete admission: " + refusal);
            if (ReadHome(world, worker) != EntityId.None) throw new InvalidOperationException("new worker already has an unrelated home binding");
            CommitHome(world, worker, world.Entities.Get(home));
        }
        public static Code CheckHome(World world, PlayerId player, EntityId workerId, EntityId homeId)
        {
            world.RequireHealthyUpdate();
            Entity worker = world.Entities.Get(workerId);
            if (worker == null || worker.Owner != player || !world.ViewFor(player).CanObserveOwned(worker)) return Codes.NotFound;
            if (homeId.IsValid && homeId != workerId && !world.ViewFor(player).CanObserveOwned(world.Entities.Get(homeId))) return Codes.NotFound;
            return CheckBinding(world, worker, homeId);
        }
        // Physical eligibility belongs to the same home owner, not player visibility or another policy/state.
        internal static Code CheckBinding(World world, Entity worker, EntityId homeId)
        {
            world.RequireHealthyUpdate();
            if (worker?.Alive != true || !ReferenceEquals(world.Entities.Get(worker.Id), worker)) return Codes.NotFound;
            if (worker.Get<IWorker>() == null) return StandardCodes.NotWorker;
            ReadBinding(world, worker);
            Code refusal = Target(world, worker, homeId, out Entity home);
            if (!refusal.IsNone || home == null) return refusal;
            return Geometry(world, worker, home) == SpatialInteractionStatus.Unsupported ? Unsupported : Code.None;
        }
        public static void Configure(World world, PlayerId player, EntityId workerId, EntityId homeId)
        {
            Code refusal = CheckHome(world, player, workerId, homeId);
            if (!refusal.IsNone) throw new InvalidOperationException("home intent refused: " + refusal);
            Entity entity = world.Entities.Get(workerId), home = world.Entities.Get(homeId);
            if (!CommitHome(world, entity, home)) return;
            Binding after = ReadBinding(world, entity);
            // Recording home cannot commandeer acquired work or held movement.
            if (after.Job == 0 && !after.Held) UpdateIdle(world, entity);
        }
        private static bool CommitHome(World world, Entity entity, Entity home)
        {
            EntityId workerId = entity.Id, homeId = home?.Id ?? EntityId.None;
            Binding before = ReadBinding(world, entity);
            if (before.Home == homeId) return false;
            IMotion motion = entity.Get<IMotion>();
            MoveStatus? status = motion == null ? null : MotionExecutionContracts.ReadStatus(world, entity);
            int version = entity.Version, targetVersion = home?.Version ?? 0;
            PlayerId owner = entity.Owner; long tick = world.Tick; EntityId next = world.Entities.NextIdentity;
            entity.Get<IWorker>().Home = homeId;
            Binding after = ReadBinding(world, entity);
            MoveStatus? afterStatus = motion == null ? null : MotionExecutionContracts.ReadStatus(world, entity);
            if (after.Home != homeId || after.Job != before.Job || after.Held != before.Held || after.HeldArrived != before.HeldArrived ||
                !ReferenceEquals(world.Entities.Get(workerId), entity) || entity.Owner != owner || entity.Version <= version ||
                world.Tick != tick || world.Entities.NextIdentity != next || afterStatus != status ||
                home != null && (!ReferenceEquals(world.Entities.Get(homeId), home) || home.Version != targetVersion || home.Owner != owner))
                throw new InvalidOperationException("worker home setter did not make an exact isolated intent commit");
            world.Services.Get<WorkBoard>().Dirty = true;
            return true;
        }
        internal static Code CheckHomeReferences(World world, Entity source, Beep.SimEngine.Content.EntityDef destination)
        {
            var workers = world.Entities.Components<IWorker>();
            for (int i = 0; i < workers.Count; i++)
            {
                Entity entity = workers[i].Entity;
                if (!entity.Alive || ReferenceEquals(entity, source) || ReadHome(world, entity) != source.Id) continue;
                if (entity.Owner != source.Owner) return Codes.NotOwned;
                Code refusal = CheckBinding(world, entity, source.Id);
                if (!refusal.IsNone) return refusal;
                var presence = destination.Component<ISpatialPresenceDefinition>();
                if (presence == null || presence.Space != source.Get<ISpatialPresence>()?.Space) return Unsupported;
            }
            return Code.None;
        }
        internal static void ReplaceHomeReferences(World world, Entity source, Entity destination)
        {
            var workers = world.Entities.Components<IWorker>();
            for (int i = 0; i < workers.Count; i++)
            {
                Entity entity = workers[i].Entity;
                if (!entity.Alive || ReferenceEquals(entity, source) || ReadHome(world, entity) != source.Id) continue;
                Code refusal = CheckBinding(world, entity, destination.Id);
                if (!refusal.IsNone) throw new InvalidOperationException("replacement home binding refused: " + refusal);
                CommitHome(world, entity, destination);
            }
        }
        private static Code Policy(World world, Entity worker, Entity home)
            => PureTarget(world, worker, home, () => WorkPolicyRules.CheckDestination(world, worker, home));
        internal static Code ReadCurrent(World world, PlayerId viewer, Entity entity, out WorkerHomeView value)
        {
            value = null; world.RequireHealthyUpdate();
            if (!world.ViewFor(viewer).CanObserveOwned(entity)) return Codes.NotFound;
            if (entity.Get<IWorker>() == null) return StandardCodes.NotWorker;
            EntityId home = ReadHome(world, entity);
            Entity target = world.Entities.Get(home);
            // A missing home is an owned worker's blocker, not a hidden foreign endpoint.
            if (home.IsValid && target != null && !world.ViewFor(viewer).CanObserveOwned(target)) return Codes.NotFound;
            value = Read(world, entity); return Code.None;
        }
        public static WorkerHomeView Read(World world, Entity entity)
        {
            Binding binding = ReadBinding(world, entity);
            WorkerHomeView View(WorkerHomeState state, Code blocker = default) => new WorkerHomeView(entity, binding.Home, binding.Job, state, blocker);
            if (binding.Job != 0) return View(WorkerHomeState.Working);
            if (binding.Held) return View(WorkerHomeState.Held);
            if (binding.Home == EntityId.None) return View(WorkerHomeState.Unassigned);
            Code refusal = Target(world, entity, binding.Home, out Entity home);
            if (!refusal.IsNone) return View(WorkerHomeState.Blocked, refusal);
            if (world.Services.Get<WorkBoard>().ReadHomeParking(home, entity, out _, out var parking, out var parkingBlocker))
            {
                if (!parkingBlocker.IsNone) return View(WorkerHomeState.Blocked, parkingBlocker);
                if (parking == SpatialInteractionStatus.WithinReach) return View(WorkerHomeState.AtHome);
                var execution = MotionExecutionContracts.Read(world, entity);
                if (!execution.Blocker.IsNone) return View(WorkerHomeState.Blocked, execution.Blocker);
                return View(execution.Status == MoveStatus.Moving ? WorkerHomeState.Returning : WorkerHomeState.Ready);
            }
            refusal = Policy(world, entity, home);
            if (!refusal.IsNone) return View(WorkerHomeState.Blocked, refusal);
            var geometry = Geometry(world, entity, home);
            if (geometry == SpatialInteractionStatus.Unsupported) return View(WorkerHomeState.Blocked, Unsupported);
            if (geometry == SpatialInteractionStatus.WithinReach) return View(WorkerHomeState.AtHome);
            if (geometry == SpatialInteractionStatus.Unreachable) return View(WorkerHomeState.Blocked, WorkSystem.BlockerUnreachable);
            var motion = MotionExecutionContracts.Read(world, entity);
            if (!motion.Blocker.IsNone) return View(WorkerHomeState.Blocked, motion.Blocker);
            return View(motion.Status == MoveStatus.Moving ? WorkerHomeState.Returning : WorkerHomeState.Ready);
        }
        internal static void UpdateIdle(World world, Entity entity)
        {
            Binding binding = ReadBinding(world, entity);
            if (binding.Job != 0 || binding.Held) return;
            var motion = entity.Get<IMotion>();
            Code refusal = Target(world, entity, binding.Home, out Entity home);
            if (refusal.IsNone && home != null && world.Services.Get<WorkBoard>().ReadHomeParking(home, entity, out var slot, out var parking, out var parkingBlocker))
            {
                if (!parkingBlocker.IsNone) { MotionExecutionContracts.Stop(world, entity); return; }
                if (parking == SpatialInteractionStatus.WithinReach)
                {
                    // Arrival may be selected while another home goal is still moving. Intent changes
                    // must stop that goal before the movement phase, without relocating the worker.
                    if (motion != null && MotionExecutionContracts.ReadStatus(world, entity) == MoveStatus.Moving)
                        MotionExecutionContracts.Stop(world, entity);
                }
                else
                    world.Services.Get<WorkBoard>().ApproachHomeParking(home, entity, slot);
                return;
            }
            if (refusal.IsNone && home != null) refusal = Policy(world, entity, home);
            if (!refusal.IsNone || home == null)
            { MotionExecutionContracts.Stop(world, entity); return; }
            // The provider retains unchanged goals/paths; changed or moving endpoints replan physically.
            var status = EntityObservation.Read(world, home, () => SpatialInteraction.Approach(world, entity, home));
            if ((byte)status > (byte)SpatialInteractionStatus.Unsupported) throw new InvalidOperationException("home approach returned an invalid status");
            if (status == SpatialInteractionStatus.WithinReach || !SpatialInteraction.CanApproach(status))
                if (motion != null && MotionExecutionContracts.ReadStatus(world, entity) != MoveStatus.Arrived) MotionExecutionContracts.Stop(world, entity);
        }
        internal static void ValidateLive(World world)
        {
            ComponentList<IWorker> workers = world.Entities.Components<IWorker>();
            for (int i = 0; i < workers.Count; i++) if (workers[i].Entity.Alive) ReadBinding(world, workers[i].Entity);
        }
    }
}
