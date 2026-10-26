using System;

namespace Beep.SimEngine.Kernel
{
    public enum SpatialInteractionStatus : byte
    {
        WithinReach = 0, Reachable = 1, Pending = 2, Unreachable = 3, Unsupported = 4
    }
    /// <summary>Current physical contact only; OutsideReach says nothing about a future route.</summary>
    public enum SpatialProximityStatus : byte { WithinReach = 0, OutsideReach = 1, Unsupported = 2 }

    /// <summary>
    /// Optional selected-space capability for physical interaction and approach. The provider owns
    /// coordinates, reach, travel cost and navigation. Approach requests movement; it never teleports.
    /// </summary>
    public interface ISpatialInteraction
    {
        bool TryEstimateCost(World world, Entity actor, Entity target, out long cost);
        SpatialProximityStatus CheckProximity(World world, Entity actor, Entity target);
        SpatialInteractionStatus Check(World world, Entity actor, Entity target);
        SpatialInteractionStatus Approach(World world, Entity actor, Entity target);
    }

    /// <summary>One engine dispatch boundary. Nonspatial machines may work on themselves, not distant entities.</summary>
    public static class SpatialInteraction
    {
        private static bool Valid(World world, Entity actor, Entity target)
            => world != null && actor != null && target != null && actor.Alive && target.Alive
                && ReferenceEquals(world.Entities.Get(actor.Id), actor) && ReferenceEquals(world.Entities.Get(target.Id), target);

        private static bool Local(Entity actor, Entity target)
            => ReferenceEquals(actor, target) && actor.Get<ISpatialPresence>() == null;

        public static SpatialProximityStatus CheckProximity(World world, Entity actor, Entity target)
        {
            world?.RequireHealthyUpdate();
            if (!Valid(world, actor, target)) return SpatialProximityStatus.Unsupported;
            if (Local(actor, target)) return SpatialProximityStatus.WithinReach;
            if (!(world.Space is ISpatialInteraction provider)) return SpatialProximityStatus.Unsupported;
            var targetState = EntityObservation.CaptureProvider(world, target, target.Sync);
            var value = EntityObservation.ReadProvider(world, provider, actor, () => {
                var result = provider.CheckProximity(world, actor, target);
                if ((byte)result > (byte)SpatialProximityStatus.Unsupported)
                    throw new InvalidOperationException("spatial proximity returned an invalid status");
                return result;
            }, world.Space, observeTargetState: true);
            targetState.Verify();
            return value;
        }

        public static bool TryEstimateCost(World world, Entity actor, Entity target, out long cost)
        {
            cost = 0;
            if (!Valid(world, actor, target)) return false;
            if (Local(actor, target)) return true;
            if (!(world.Space is ISpatialInteraction provider) || !provider.TryEstimateCost(world, actor, target, out cost)) return false;
            if (cost < 0) throw new InvalidOperationException("spatial interaction cost must be nonnegative");
            return true;
        }

        public static SpatialInteractionStatus Check(World world, Entity actor, Entity target)
        {
            if (!Valid(world, actor, target)) return SpatialInteractionStatus.Unsupported;
            if (Local(actor, target)) return SpatialInteractionStatus.WithinReach;
            return world.Space is ISpatialInteraction provider ? provider.Check(world, actor, target) : SpatialInteractionStatus.Unsupported;
        }

        public static SpatialInteractionStatus Approach(World world, Entity actor, Entity target)
        {
            if (!Valid(world, actor, target)) return SpatialInteractionStatus.Unsupported;
            if (Local(actor, target)) return SpatialInteractionStatus.WithinReach;
            return world.Space is ISpatialInteraction provider ? provider.Approach(world, actor, target) : SpatialInteractionStatus.Unsupported;
        }

        public static bool CanApproach(SpatialInteractionStatus status)
            => status == SpatialInteractionStatus.WithinReach || status == SpatialInteractionStatus.Reachable || status == SpatialInteractionStatus.Pending;
    }
}
