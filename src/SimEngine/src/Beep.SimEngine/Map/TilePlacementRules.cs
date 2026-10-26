using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Maps
{
    /// <summary>One pure tile placement preflight; selected providers retain their actual occupancy and native presence owns coordinates.</summary>
    public static class TilePlacementRules
    {
        public static Code CheckConstructionDefinition(ITileSpatialSpace space, EntityDef target, EntityDef site)
        {
            var finished = target?.Component<ITilePresenceDefinition>(); var pending = site?.Component<ITilePresenceDefinition>();
            return space == null || finished == null || pending == null || finished.Space != space.Id || pending.Space != space.Id ||
                finished.Movable || pending.Movable || !pending.Blocks || finished.Width != pending.Width || finished.Height != pending.Height
                ? Codes.InvalidArgument : Code.None;
        }
        public static Code Check(World world, ITileNavigationSpace space, EntityDef target, SpatialPlacement placement, Entity replacing)
        {
            world?.RequireHealthyUpdate();
            if (world == null || space == null || !ReferenceEquals(world.Space, space) || target == null ||
                !ReferenceEquals(world.Content.Find<EntityDef>(target.Id), target) || placement is not TilePlacement tile || tile.Space != space.Id)
                return Codes.InvalidArgument;
            if (replacing != null && !ReferenceEquals(world.Entities.Get(replacing.Id), replacing)) return Codes.NotFound;
            var presence = target.Component<ITilePresenceDefinition>();
            if (presence == null || presence.Space != space.Id) return Codes.NotAllowed;
            if (!space.InBounds(new TileRect(tile.Origin, presence.Width, presence.Height))) return Codes.OutOfBounds;
            if (presence.Movable && tile.Position.Tile != tile.Origin) return Codes.InvalidArgument;
            foreach (var component in target.Components)
                if (component is ITilePlacementConstraint constraint) {
                    Code refusal = EntityObservation.ReadProvider(world, constraint, replacing,
                        () => constraint.CheckTilePlacement(world, target, tile, replacing), space, observeTargetState: replacing != null);
                    if (!refusal.IsNone) return refusal;
                }
            if (presence.Blocks)
                for (int y = tile.Origin.Y; y < tile.Origin.Y + presence.Height; y++)
                    for (int x = tile.Origin.X; x < tile.Origin.X + presence.Width; x++) {
                        Entity occupant = space.OccupantAt(new TileCoord(x, y));
                        if (occupant != null && !ReferenceEquals(occupant, replacing)) return Codes.Blocked;
                    }
            // Existing AND pending facilities keep their authored future access. A
            // permanent neighbour must not be admitted where finalization later
            // discovers it has destroyed a service/exit/parking/rollout tile.
            if (presence.Blocks && !presence.Movable)
            {
                var obstruction = new TileRect(tile.Origin, presence.Width, presence.Height);
                foreach (Entity existing in world.Entities.All)
                {
                    if (!existing.Alive || ReferenceEquals(existing, replacing)) continue;
                    ITilePresence occupied = existing.Get<ITilePresence>();
                    if (occupied == null) continue;
                    TilePlacement anchor = EntityObservation.ReadProvider(world, occupied, existing, () =>
                        occupied.Placed && !occupied.Movable && occupied.Space == space.Id
                            ? new TilePlacement(space.Id, occupied.Tile, occupied.Position) : null,
                        space, observeTargetState: true);
                    if (anchor == null) continue;
                    EntityDef reserved = existing.Def;
                    IConstructionSite site = existing.Get<IConstructionSite>();
                    if (site != null)
                        reserved = EntityObservation.ReadProvider(world, site, existing, () => {
                            EntityDef result = site.Target;
                            if (!ReferenceEquals(site.Entity, existing) || result == null ||
                                !ReferenceEquals(world.Content.Find<EntityDef>(result.Id), result) ||
                                !CheckConstructionDefinition(space, result, existing.Def).IsNone)
                                throw new InvalidOperationException("pending clearance requires its actual compatible construction target");
                            return result;
                        }, space, observeTargetState: true);
                    foreach (var component in reserved.Components)
                        if (component is ITilePlacementClearance clearance)
                        {
                            Code refusal = EntityObservation.ReadProvider(world, clearance, existing,
                                () => clearance.CheckTileObstruction(world, reserved, anchor, obstruction), space, observeTargetState: true);
                            if (!refusal.IsNone) return refusal;
                        }
                }
            }
            return Code.None;
        }
    }
}
