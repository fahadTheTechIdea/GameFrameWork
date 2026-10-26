using System;
using System.Numerics;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public static class MotionArrivalCodes
    {
        public static readonly Code NotMoving = new Code("motion.not-moving"), Unsupported = new Code("motion.estimate-unsupported"),
            RoutePending = new Code("motion.route-pending"), RouteChanged = new Code("motion.route-changed"),
            Unreachable = new Code("motion.unreachable"), ZeroBudget = new Code("motion.travel-budget-zero"),
            Overflow = new Code("motion.estimate-overflow");
    }

    public sealed class MotionArrivalView
    {
        public EntityId Entity { get; }
        public int EntityVersion { get; }
        public long ObservedTick { get; }
        public MoveStatus Status { get; }
        public long? RemainingTicks { get; }
        public long? ArrivalTick { get; }
        public Code Blocker { get; }
        /// <summary>Current-stock funding of this actual remaining route; null when no duration is known. Not a reservation or arrival guarantee.</summary>
        public OperatingInputEvaluation OperatingBudget { get; }
        internal MotionArrivalView(Entity entity, long tick, MoveStatus status, MotionTravelEstimate estimate, OperatingInputEvaluation operatingBudget)
        {
            Entity = entity.Id; EntityVersion = entity.Version; ObservedTick = tick; Status = status;
            RemainingTicks = estimate.RemainingTicks; Blocker = estimate.Blocker;
            ArrivalTick = RemainingTicks.HasValue ? checked(tick + RemainingTicks.Value) : (long?)null;
            OperatingBudget = operatingBudget;
        }
    }

    /// <summary>One stateless estimate/gate/checked-duration protocol; movement remains provider-owned.</summary>
    public static class MotionArrivalContracts
    {
        /// <summary>Combine conditional movement exposure, never queue/service time or a partial known prefix.</summary>
        public static MotionTravelEstimate Combine(MotionTravelEstimate first, MotionTravelEstimate second)
        {
            void Validate(MotionTravelEstimate value)
            {
                if (value.RemainingTicks.HasValue ? value.RemainingTicks.Value < 0 || !value.Blocker.IsNone : value.Blocker.IsNone)
                    throw new ArgumentException("travel total requires explicit known or unknown duration");
            }
            Validate(first); Validate(second);
            if (!first.RemainingTicks.HasValue) return first;
            if (!second.RemainingTicks.HasValue) return second;
            try { return new MotionTravelEstimate(checked(first.RemainingTicks.Value + second.RemainingTicks.Value), Code.None); }
            catch (OverflowException) { return new MotionTravelEstimate(null, MotionArrivalCodes.Overflow); }
        }
        public static MotionTravelEstimate FromBudget(BigInteger remainingRaw, Fix tickBudget)
        {
            if (remainingRaw.Sign < 0 || tickBudget.Raw < 0) throw new ArgumentOutOfRangeException("travel quantities must be nonnegative");
            // Even an already reached moving goal needs its next physical phase to settle Arrived.
            if (remainingRaw.IsZero) return new MotionTravelEstimate(1, Code.None);
            if (tickBudget.Raw == 0) return new MotionTravelEstimate(null, MotionArrivalCodes.ZeroBudget);
            BigInteger ticks = (remainingRaw + tickBudget.Raw - 1) / tickBudget.Raw;
            return ticks > long.MaxValue ? new MotionTravelEstimate(null, MotionArrivalCodes.Overflow) : new MotionTravelEstimate((long)ticks, Code.None);
        }
        public static MotionArrivalView Read(World world, Entity entity)
        {
            MoveStatus status = MotionExecutionContracts.ReadStatus(world, entity);
            MotionArrivalView View(MotionTravelEstimate value)
            {
                if (value.RemainingTicks.HasValue && value.RemainingTicks.Value > long.MaxValue - world.Tick)
                    value = new MotionTravelEstimate(null, MotionArrivalCodes.Overflow);
                var budget = value.RemainingTicks.HasValue
                    ? OperatingInputsContracts.Read(world, entity, OperatingPhase.Movement, value.RemainingTicks.Value) : null;
                return new MotionArrivalView(entity, world.Tick, status, value, budget);
            }
            if (status == MoveStatus.Arrived) return View(new MotionTravelEstimate(0, Code.None));
            if (status != MoveStatus.Moving) return View(new MotionTravelEstimate(null,
                status == MoveStatus.Unreachable ? MotionArrivalCodes.Unreachable : MotionArrivalCodes.NotMoving));
            MotionExecutionView execution = MotionExecutionContracts.Read(world, entity);
            if (!execution.Blocker.IsNone) return View(new MotionTravelEstimate(null, execution.Blocker));
            var provider = entity.Get<IMotionEstimate>();
            if (provider == null) return View(new MotionTravelEstimate(null, MotionArrivalCodes.Unsupported));
            if (!ReferenceEquals(provider.Entity, entity) || !ReferenceEquals(provider, entity.Get<IMotion>()))
                throw new InvalidOperationException("arrival estimate must alias the actual motion owner");
            MotionTravelEstimate estimate = EntityObservation.ReadProvider(world, provider, entity,
                () => provider.EstimateRemaining(world, execution.Rates.Rate), space: world.Space);
            if (MotionExecutionContracts.ReadStatus(world, entity) != status ||
                estimate.RemainingTicks.HasValue && (estimate.RemainingTicks.Value <= 0 || !estimate.Blocker.IsNone) ||
                !estimate.RemainingTicks.HasValue && estimate.Blocker.IsNone)
                throw new InvalidOperationException("motion returned invalid or mutating arrival facts");
            return View(estimate);
        }
    }
}
