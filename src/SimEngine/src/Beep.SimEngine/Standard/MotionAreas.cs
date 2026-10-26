using System;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Optional pure constraint implemented by the actual motion owner.</summary>
    public interface IMotionAreaConstraint : IMotion
    {
        Code CheckArea(World world, ISpatialRegion region);
        MotionRouteCheck CheckRoute(World world, ISpatialRegion region, SpatialSnapshot origin, MotionDestination destination, Fix speedModifier);
    }
    public enum MotionDestinationKind : byte { Exact = 0, Interaction = 1 }
    /// <summary>Detached explicit arrival intent, not a second goal owner.</summary>
    public sealed class MotionDestination
    {
        public SpatialSnapshot Geometry { get; }
        public MotionDestinationKind Kind { get; }
        public MotionDestination(SpatialSnapshot geometry, MotionDestinationKind kind)
        {
            if (geometry == null || (byte)kind > (byte)MotionDestinationKind.Interaction) throw new ArgumentException("route needs explicit geometry and arrival kind");
            Geometry = geometry; Kind = kind;
        }
    }
    /// <summary>Prospective structural/policy admission. Arrival is a proven detached geometry, never a reserved route.</summary>
    public sealed class MotionRouteCheck
    {
        public Code Blocker { get; }
        public SpatialSnapshot Arrival { get; }
        /// <summary>Conditional movement exposure at the current controlled rate, not queue/service time or a route reservation.</summary>
        public MotionTravelEstimate? Travel { get; }
        public MotionRouteCheck(Code blocker, SpatialSnapshot arrival, MotionTravelEstimate? travel)
        { Blocker = blocker; Arrival = arrival; Travel = travel; }
    }
    public static class MotionAreaCodes
    {
        public static readonly Code Outside = new Code("motion.area-outside"), Unsupported = new Code("motion.area-unsupported"),
            Pending = new Code("motion.area-route-pending");
        public static bool IsWait(Code code) => code == Outside || code == Unsupported || code == Pending;
    }
    public sealed class MotionAreaEvaluation
    {
        public string Area { get; }
        public Code Blocker { get; }
        internal MotionAreaEvaluation(string area, Code blocker) { Area = area; Blocker = blocker; }
    }
    /// <summary>Stateless admission only. Actual policy, route and geometry owners retain all state.</summary>
    public static class MotionAreaContracts
    {
        public static MotionRouteCheck Plan(World world, Entity entity, ISpatialRegion region, SpatialSnapshot origin, MotionDestination destination)
        {
            MoveStatus status = MotionExecutionContracts.ReadStatus(world, entity);
            if (origin == null || destination == null || world.Space == null || region != null && region.Space != world.Space.Id ||
                origin.Space != world.Space.Id || destination.Geometry.Space != world.Space.Id)
                throw new ArgumentException("prospective route needs actual session region and compatible detached geometry");
            var provider = entity.Get<IMotionAreaConstraint>();
            if (provider == null) return new MotionRouteCheck(MotionAreaCodes.Unsupported, null, null);
            if (!ReferenceEquals(provider, entity.Get<IMotion>()) || !ReferenceEquals(provider.Entity, entity))
                throw new InvalidOperationException("prospective constraint must alias actual motion owner");
            var rates = MotionExecutionContracts.ReadRates(world, entity);
            Code durationBlocker = entity.Get<IWorker>() == null ? Code.None : WorkerExecutionContracts.Read(world, entity).Gates.Blocker;
            if (durationBlocker.IsNone) durationBlocker = !rates.Gates.Blocker.IsNone ? rates.Gates.Blocker :
                !rates.Rate.IsPositive ? MotionExecutionCodes.ZeroRate : Code.None;
            MotionRouteCheck result = EntityObservation.ReadProvider(world, provider, entity,
                () => provider.CheckRoute(world, region, origin, destination, rates.Rate), space: world.Space);
            if (MotionExecutionContracts.ReadStatus(world, entity) != status || result == null ||
                result.Blocker.IsNone != (result.Arrival != null) || result.Blocker.IsNone != result.Travel.HasValue ||
                !result.Blocker.IsNone && !MotionAreaCodes.IsWait(result.Blocker))
                throw new InvalidOperationException("motion returned mutating or contradictory prospective admission");
            if (result.Arrival != null)
            {
                var travel = result.Travel.Value;
                if (travel.RemainingTicks.HasValue ? travel.RemainingTicks.Value < 0 || !travel.Blocker.IsNone : travel.Blocker.IsNone)
                    throw new InvalidOperationException("motion returned contradictory prospective duration");
                var admitted = region == null ? SpatialRegionStatus.Inside : EntityObservation.Read(world, entity, () => region.Check(result.Arrival));
                if (result.Arrival.Space != world.Space.Id || admitted != SpatialRegionStatus.Inside)
                    throw new InvalidOperationException("prospective arrival is outside actual area geometry");
            }
            return result.Blocker.IsNone && result.Travel.Value.RemainingTicks != 0 && !durationBlocker.IsNone
                ? new MotionRouteCheck(Code.None, result.Arrival, new MotionTravelEstimate(null, durationBlocker)) : result;
        }
        public static MotionAreaEvaluation Read(World world, Entity entity)
        {
            ISpatialRegion region = WorkPolicyRules.MotionRegion(world, entity);
            if (region == null) return new MotionAreaEvaluation(null, Code.None);
            MoveStatus status = MotionExecutionContracts.ReadStatus(world, entity);
            if (status != MoveStatus.Moving) return new MotionAreaEvaluation(region.Id, Code.None);
            var provider = entity.Get<IMotionAreaConstraint>();
            if (provider == null) return new MotionAreaEvaluation(region.Id, MotionAreaCodes.Unsupported);
            if (!ReferenceEquals(provider, entity.Get<IMotion>()) || !ReferenceEquals(provider.Entity, entity))
                throw new InvalidOperationException("area constraint must alias actual motion owner");
            Code blocker = EntityObservation.ReadProvider(world, provider, entity, () => provider.CheckArea(world, region), space: world.Space);
            if (MotionExecutionContracts.ReadStatus(world, entity) != status || !blocker.IsNone && !MotionAreaCodes.IsWait(blocker))
                throw new InvalidOperationException("motion returned mutating or invalid area admission");
            return new MotionAreaEvaluation(region.Id, blocker);
        }
        public static SpatialRegionStatus CheckTravel(ISpatialRegion region, SpatialSnapshot from, SpatialSnapshot to)
        {
            if (region is not ISpatialTravelRegion travel) return SpatialRegionStatus.Unsupported;
            if (from == null || to == null || from.Space != region.Space || to.Space != region.Space) return SpatialRegionStatus.Unsupported;
            SpatialRegionStatus result = travel.CheckTravel(from, to);
            if ((byte)result > (byte)SpatialRegionStatus.Unsupported) throw new InvalidOperationException("region returned invalid sweep admission");
            return result;
        }
    }
}
