using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface IMotionExecutionDefinition : INamedCapabilityDefinition { }
    public interface IMotionExecution : INamedEntityCapability { ExecutionRateSnapshot ReadExecution(World world); }
    public static class MotionExecutionCodes { public static readonly Code ZeroRate = new Code("motion.zero-rate"); }
    public sealed class MotionExecutionView
    {
        public EntityId Entity { get; }
        public MoveStatus Status { get; }
        public ExecutionRateEvaluation Rates { get; }
        public ExecutionGateEvaluation WorkerGates { get; }
        public OperatingInputEvaluation Operating { get; }
        public MotionAreaEvaluation Area { get; }
        public HaulTripEvaluation TripBudget { get; }
        public Code DockBlocker { get; }
        public Code Blocker { get; }
        internal MotionExecutionView(EntityId entity, MoveStatus status, ExecutionRateEvaluation rates, ExecutionGateEvaluation workerGates, OperatingInputEvaluation operating, MotionAreaEvaluation area, HaulTripEvaluation trip, Code dockBlocker)
        {
            Entity = entity; Status = status; Rates = rates; WorkerGates = workerGates; Operating = operating; Area = area; TripBudget = trip; DockBlocker = dockBlocker;
            Blocker = !dockBlocker.IsNone ? dockBlocker : !workerGates.Blocker.IsNone ? workerGates.Blocker : !rates.Gates.Blocker.IsNone ? rates.Gates.Blocker :
                !rates.Rate.IsPositive ? MotionExecutionCodes.ZeroRate : !area.Blocker.IsNone ? area.Blocker :
                trip != null && !trip.Blocker.IsNone ? trip.Blocker : operating.Blocker;
        }
    }
    public static class MotionExecutionContracts
    {
        public static MoveStatus ReadStatus(World world, Entity entity)
        {
            var motion = entity?.Get<IMotion>();
            if (world == null || motion == null || !entity.Alive || !ReferenceEquals(world.Entities.Get(entity.Id), entity))
                throw new ArgumentException("motion execution requires an actual live session motion provider");
            var status = EntityObservation.ReadProvider(world, motion, entity, () => motion.Status);
            if ((byte)status > (byte)MoveStatus.Unreachable) throw new InvalidOperationException("motion returned invalid status");
            return status;
        }
        /// <summary>End the actual provider's movement intent, without another route/status owner.</summary>
        public static void Stop(World world, Entity entity)
        {
            if (entity?.Get<IMotion>() == null) return;
            if (ReadStatus(world, entity) == MoveStatus.Idle) return;
            PlayerId owner = entity.Owner; long tick = world.Tick; EntityId next = world.Entities.NextIdentity;
            entity.Get<IMotion>().Stop();
            if (ReadStatus(world, entity) != MoveStatus.Idle || entity.Owner != owner || world.Tick != tick || world.Entities.NextIdentity != next)
                throw new InvalidOperationException("motion provider did not stop its actual intent");
        }
        internal static ExecutionRateEvaluation ReadRates(World world, Entity entity)
            => ExecutionRateContracts.Read<IMotionExecution, IMotionExecutionDefinition>(world, entity, Fix.One, provider => provider.ReadExecution(world));
        public static MotionExecutionView Read(World world, Entity entity)
            => Capture(world, entity, world.Services.TryGet<IHaulExecution>(out var haul) && entity?.Get<IHaulTripPolicy>() != null
                ? haul.ReadHaulTripBudget(entity.Id) : null);
        // Candidate assignment and dock FIFO read physics only, not the incumbent job's funding.
        // Otherwise queue admission -> job check -> trip selector would recurse.
        internal static MotionExecutionView ReadPhysical(World world, Entity entity) => Capture(world, entity, null);
        private static MotionExecutionView Capture(World world, Entity entity, HaulTripEvaluation trip)
        {
            var status = ReadStatus(world, entity);
            var rates = ReadRates(world, entity);
            var workerGates = entity.Get<IWorker>() == null ? ExecutionGateContracts.Open
                : WorkerExecutionContracts.Read(world, entity).Gates;
            Code dockBlocker = Code.None;
            if (entity.Get<IWorkDock>() != null) {
                var admission = world.Services.Get<IWorkDockAdmission>();
                dockBlocker = EntityObservation.ReadProvider(world, admission, entity, () => admission.CheckEndpointMotion(entity));
            }
            return new MotionExecutionView(entity.Id, status, rates, workerGates, OperatingInputsContracts.Read(world, entity, OperatingPhase.Movement), MotionAreaContracts.Read(world, entity), trip, dockBlocker);
        }
        internal static void ValidateDefinitions(World world)
        {
            ExecutionRateContracts.ValidateDefinitions<IMotionExecution, IMotionExecutionDefinition, IMotion>(world);
            MotionNavigationContracts.ValidateDefinitions(world);
        }
        internal static void ValidateLive(World world)
        { foreach (Entity entity in world.Entities.All) if (entity.Alive && entity.Get<IMotion>() != null) { Read(world, entity); MotionNavigationContracts.Read(world, entity); } }
    }
    public sealed class MotionExecutionDef : ControlledExecutionDef<MotionExecution>, IMotionExecutionDefinition
    {
        protected override Code MultiplierUnit => new Code("unit.speed-multiplier");
        protected override bool HasTarget(EntityDef owner) => owner.HasCapability<IMotion>();
    }
    public sealed class MotionExecution : ControlledExecution<MotionExecutionDef>, IMotionExecution { }
}
