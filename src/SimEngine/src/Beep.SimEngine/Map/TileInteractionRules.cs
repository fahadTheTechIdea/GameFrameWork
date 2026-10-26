using System;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Maps
{
    /// <summary>One tile approach policy. Providers own geometry/occupancy; native motion owns route and time.</summary>
    public static class TileInteractionRules
    {
        private static bool TryInteraction(World world, ITileNavigationSpace space, Entity actor, Entity target,
            out ITilePresence presence, out TileRect reach)
        {
            world?.RequireHealthyUpdate();
            presence = actor?.Get<ITilePresence>(); var destination = target?.Get<ITilePresence>(); reach = default;
            if (world == null || space == null || !ReferenceEquals(world.Space, space) || actor == null || target == null || !actor.Alive || !target.Alive ||
                !ReferenceEquals(world.Entities.Get(actor.Id), actor) || !ReferenceEquals(world.Entities.Get(target.Id), target) ||
                presence == null || destination == null || !presence.Placed || !destination.Placed ||
                presence.Space != space.Id || destination.Space != space.Id) return false;
            reach = space.InteractionGoal(destination.Footprint, presence.Footprint.W, presence.Footprint.H); return true;
        }
        public static bool TryEstimateCost(World world, ITileNavigationSpace space, Entity actor, Entity target, out long cost)
        {
            cost = 0;
            if (!TryInteraction(world, space, actor, target, out var presence, out var reach)) return false;
            cost = reach.StepsTo(presence.Tile); return true;
        }
        public static SpatialInteractionStatus Check(World world, ITileNavigationSpace space, Entity actor, Entity target)
        {
            if (!TryInteraction(world, space, actor, target, out var presence, out var reach)) return SpatialInteractionStatus.Unsupported;
            if (reach.Contains(presence.Tile)) return SpatialInteractionStatus.WithinReach;
            var mobility = actor.Get<IMobility>(); if (mobility == null) return SpatialInteractionStatus.Unreachable;
            var route = mobility.CanReach(world, reach);
            return route == PathSearchResult.Found ? SpatialInteractionStatus.Reachable
                : route == PathSearchResult.BudgetExceeded ? SpatialInteractionStatus.Pending : SpatialInteractionStatus.Unreachable;
        }
        public static SpatialProximityStatus CheckProximity(World world, ITileNavigationSpace space, Entity actor, Entity target)
        {
            if (!TryInteraction(world, space, actor, target, out var presence, out var reach)) return SpatialProximityStatus.Unsupported;
            return reach.Contains(presence.Tile) ? SpatialProximityStatus.WithinReach : SpatialProximityStatus.OutsideReach;
        }
        public static SpatialInteractionStatus Approach(World world, ITileNavigationSpace space, Entity actor, Entity target)
        {
            if (!TryInteraction(world, space, actor, target, out var presence, out var reach)) return SpatialInteractionStatus.Unsupported;
            var mobility = actor.Get<IMobility>();
            if (reach.Contains(presence.Tile) && (mobility == null || mobility.Status != MoveStatus.Moving)) return SpatialInteractionStatus.WithinReach;
            if (mobility != null && mobility.Status == MoveStatus.Moving && mobility.Goal == reach) return SpatialInteractionStatus.Reachable;
            var status = Check(world, space, actor, target);
            if (!SpatialInteraction.CanApproach(status) || mobility == null) return status;
            if (mobility.Status != MoveStatus.Moving || mobility.Goal != reach) mobility.GoTo(reach);
            return status == SpatialInteractionStatus.Pending ? status : SpatialInteractionStatus.Reachable;
        }
    }
}
