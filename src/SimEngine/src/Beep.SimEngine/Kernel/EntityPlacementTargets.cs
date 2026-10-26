using System;
using Beep.SimEngine.Content;

namespace Beep.SimEngine.Kernel
{
    /// <summary>Exact stateless lookup over actual entities and selected placement contracts. No spatial store.</summary>
    public static class EntityPlacementTargets
    {
        public static readonly Code Ambiguous = new Code("core.placement-target-ambiguous");
        public static readonly Code Unsupported = new Code("core.placement-target-unsupported");
        public static Code Find(World world, PlayerId company, EntityDef prototype, JsonValue arguments, out EntityId target)
        {
            target = EntityId.None;
            if (world == null || prototype == null || !ReferenceEquals(world.Content.Find<EntityDef>(prototype.Id), prototype)) return Codes.UnknownDef;
            var observation = EntityObservation.Capture(world);
            try { return FindCurrent(world, company, prototype, arguments, out target); }
            finally { observation.Verify(); }
        }
        private static Code FindCurrent(World world, PlayerId company, EntityDef prototype, JsonValue arguments, out EntityId target)
        {
            target = EntityId.None;
            if (!world.Players.IsSeat(company)) return Codes.NotSeated;
            if (!(world.Space is ISpatialPlacementIntent provider)) return Unsupported;
            SpatialPlacement expected;
            try { expected = provider.ReadPlacement(arguments); }
            catch (ContentException) { return Codes.InvalidArgument; }
            if (expected == null || expected.Space != world.Space.Id) return Codes.InvalidArgument;
            string identity = provider.PlacementIdentity(expected);
            if (string.IsNullOrWhiteSpace(identity)) throw new InvalidOperationException("placement provider returned an empty identity");
            Entity found = null;
            foreach (Entity entity in world.Entities.All)
            {
                if (!entity.Alive || !ReferenceEquals(entity.Def, prototype)) continue;
                var observation = EntityObservation.Capture(world, entity); bool match;
                try
                {
                    SpatialPlacement actual = provider.CapturePlacement(entity);
                    match = actual != null && actual.Space == world.Space.Id && provider.PlacementIdentity(actual) == identity;
                }
                finally { observation.Verify(); }
                if (!match) continue;
                if (found != null) return Ambiguous;
                found = entity;
            }
            if (found == null) return Codes.NotFound;
            if (found.Owner != company) return Codes.NotOwned;
            target = found.Id; return Code.None;
        }
    }
}
