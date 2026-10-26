using System.Collections.Generic;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Views
{
    public sealed class MotionView
    {
        private readonly World _world;
        private readonly PlayerId _viewer;
        internal MotionView(World world, PlayerId viewer) { _world = world; _viewer = viewer; }
        private Entity Observed(EntityId id)
        {
            _world.RequireHealthyUpdate(); Entity entity = _world.Entities.Get(id);
            return _world.ViewFor(_viewer).CanObserveOwned(entity) ? entity : null;
        }
        public MotionNavigationSelectionPreview PreviewNavigationGroup(IReadOnlyList<EntityId> targets, string profile)
            => MotionNavigationContracts.PreviewGroup(_world, _viewer, targets, profile);
        public Code ReadNavigation(EntityId id, out MotionNavigationSnapshot value)
        {
            value = null; var entity = Observed(id);
            if (entity == null) return Codes.NotFound;
            value = MotionNavigationContracts.Read(_world, entity);
            return value == null ? MotionNavigationCodes.Unsupported : Code.None;
        }
        public Code PreviewNavigation(EntityId id, string profile)
        {
            _world.RequireHealthyUpdate();
            var entity = _world.Entities.Get(id);
            return MotionNavigationContracts.CheckConfigure(_world, _viewer == PlayerId.None ? entity?.Owner ?? _viewer : _viewer, entity, profile);
        }
        public Code ReadExecution(EntityId id, out MotionExecutionView value)
        {
            value = null; var entity = Observed(id);
            if (entity == null) return Codes.NotFound;
            if (entity.Get<IMotion>() == null) return Codes.InvalidArgument;
            var facts = MotionExecutionContracts.Read(_world, entity);
            if (!OperatingInputsContracts.CanDisclose(_world, _viewer, facts.Operating) ||
                !HaulTripPolicyContracts.CanDisclose(_world, _viewer, facts.TripBudget)) return Codes.NotFound;
            value = facts; return Code.None;
        }
        public Code ReadArrival(EntityId id, out MotionArrivalView value)
        {
            value = null; var entity = Observed(id);
            if (entity == null) return Codes.NotFound;
            if (entity.Get<IMotion>() == null) return Codes.InvalidArgument;
            value = MotionArrivalContracts.Read(_world, entity); return Code.None;
        }
        public Code ReadOperatingInputs(EntityId id, long intervals, out OperatingInputEvaluation value)
        {
            value = null; var entity = Observed(id);
            if (entity == null) return Codes.NotFound;
            if (entity.Get<IMotion>() == null || intervals < 0) return Codes.InvalidArgument;
            var facts = OperatingInputsContracts.Read(_world, entity, OperatingPhase.Movement, intervals);
            if (!OperatingInputsContracts.CanDisclose(_world, _viewer, facts)) return Codes.NotFound;
            value = facts; return Code.None;
        }
        public Code ReadRoute(EntityId id, SpatialSnapshot origin, MotionDestination destination, out MotionRouteCheck value)
        {
            value = null; var entity = Observed(id);
            if (entity == null) return Codes.NotFound;
            if (entity.Get<IMotion>() == null || origin == null || destination == null || _world.Space == null ||
                origin.Space != _world.Space.Id || destination.Geometry.Space != _world.Space.Id) return Codes.InvalidArgument;
            value = MotionAreaContracts.Plan(_world, entity, WorkPolicyRules.MotionRegion(_world, entity), origin, destination);
            return Code.None;
        }
    }
}
