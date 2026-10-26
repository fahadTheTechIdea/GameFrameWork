using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Maps
{
    /// <summary>Sole stateless tile rollout policy; selected providers retain placement and occupancy.</summary>
    public static class TileRolloutRules
    {
        public static Code CheckDefinition(World world, ITileNavigationSpace space, EntityDef producer, EntityDef output)
        {
            if (world == null || space == null || !ReferenceEquals(world.Space, space) || space is not ISpatialPlacement) return EntityRollout.Unsupported;
            ITilePresenceDefinition source = producer.Component<ITilePresenceDefinition>();
            ITilePresenceDefinition destination = output.Component<ITilePresenceDefinition>();
            if (source?.Space != space.Id || destination?.Space != space.Id) return EntityRollout.Unsupported;
            if (destination.Width > space.Width || destination.Height > space.Height) return Codes.OutOfBounds;
            if (destination.Movable)
            {
                IMobilityDefinition mobility = output.Component<IMobilityDefinition>();
                if (!output.HasCapability<IMobility>() || mobility == null
                    || !world.Services.TryGet<NavService>(out NavService navigation) || navigation.Find(mobility.MovementClass) == null)
                    return EntityRollout.Unsupported;
                if (producer.Component<ITileWorkDockDefinition>() == null) return EntityRollout.Unsupported;
                WorkDockRules.ValidateRollouts(world.Content, producer);
                bool supported = false; foreach (var lane in WorkDockRules.RolloutsFor(producer, output)) supported = true;
                if (!supported) return EntityRollout.Unsupported;
            }
            return Code.None;
        }

        public static Code Find(World world, ITileNavigationSpace space, EntityRolloutRequest request, out SpatialPlacement placement)
        {
            placement = null;
            EntityDef output = request.Output;
            Code refusal = CheckDefinition(world, space, request.Producer, output);
            if (!refusal.IsNone) return refusal;
            if (!(request.Origin is TilePlacement origin) || origin.Space != space.Id) return Codes.InvalidArgument;
            ITilePresenceDefinition source = request.Producer.Component<ITilePresenceDefinition>();
            TileRect footprint = new TileRect(origin.Origin, source.Width, source.Height);
            ITilePresenceDefinition destination = output.Component<ITilePresenceDefinition>();
            if (destination.Movable) return FindMobileRollout(world, space, request, out placement);
            long maxRing = System.Math.Max(System.Math.Max((long)footprint.X, footprint.Y),
                System.Math.Max((long)space.Width - footprint.Right, (long)space.Height - footprint.Bottom));
            for (long ring = 1; ring <= maxRing; ring++)
            {
                long left = System.Math.Max(0L, (long)footprint.X - ring), top = System.Math.Max(0L, (long)footprint.Y - ring);
                long right = System.Math.Min(space.Width, (long)footprint.Right + ring), bottom = System.Math.Min(space.Height, (long)footprint.Bottom + ring);
                if (right <= left || bottom <= top) continue;
                // Sole deterministic tile policy: bottom/right/top/left through the authored boundary.
                long y = bottom - 1;
                for (long x = left; x < right; x++) if (Try(x, y, out placement)) return Code.None;
                long edge = right - 1;
                for (y = top; y < bottom; y++) if (Try(edge, y, out placement)) return Code.None;
                y = top;
                for (long x = left; x < right; x++) if (Try(x, y, out placement)) return Code.None;
                edge = left;
                for (y = top; y < bottom; y++) if (Try(edge, y, out placement)) return Code.None;
            }
            return Codes.Blocked;

            bool Try(long x, long y, out SpatialPlacement candidate)
            {
                candidate = null;
                var tile = new TileCoord((int)x, (int)y);
                var intent = new TilePlacement(space.Id, tile, FixVec2.Centre(tile));
                if (!CheckPlacement(world, space, request, intent).IsNone) return false;
                candidate = intent;
                return true;
            }
        }

        public static Code CheckPlacement(World world, ITileNavigationSpace space, EntityRolloutRequest request, SpatialPlacement placement)
        {
            if (world == null || !ReferenceEquals(world.Space, space) || space is not ISpatialPlacement placementProvider) return Codes.InvalidArgument;
            Code refusal = placementProvider.CheckPlacement(world, request.Output, placement, null);
            if (!refusal.IsNone) return refusal;
            var tile = (TilePlacement)placement;
            var destination = request.Output.Component<ITilePresenceDefinition>();
            var area = new TileRect(tile.Origin, destination.Width, destination.Height);
            // Non-blocking mobile outputs still cannot be born inside occupied or planned geometry.
            for (int y = area.Y; y < area.Bottom; y++)
                for (int x = area.X; x < area.Right; x++)
                    if (space.OccupantAt(new TileCoord(x, y)) != null) return Codes.Blocked;
            foreach (var occupied in request.Unavailable)
            {
                if (!(occupied.Placement is TilePlacement other) || other.Space != space.Id) return Codes.InvalidArgument;
                ITilePresenceDefinition definition = occupied.Definition.Component<ITilePresenceDefinition>();
                if (definition == null || definition.Space != space.Id) return Codes.InvalidArgument;
                if (area.Overlaps(new TileRect(other.Origin, definition.Width, definition.Height))) return Codes.Blocked;
            }
            return destination.Movable ? CheckMobileRollout(world, space, request, tile) : Code.None;
        }

        private static Code FindMobileRollout(World world, ITileNavigationSpace space, EntityRolloutRequest request, out SpatialPlacement placement)
        {
            placement = null; bool pending = false;
            var layout = request.Producer.Component<ITileWorkDockDefinition>(); var origin = (TilePlacement)request.Origin;
            TileSpaceGeometry geometry = SpatialSpaceContracts.ReadGeometry(world.Content.Get<ITileSpaceDefinition>(space.Id), world.Settings.Scenario);
            foreach (var lane in WorkDockRules.RolloutsFor(request.Producer, request.Output)) foreach (Code start in lane.Starts)
            {
                if (!layout.TryTile(geometry, origin.Origin, start, out TileCoord at)) continue;
                var candidate = new TilePlacement(space.Id, at, FixVec2.Centre(at));
                Code refusal = CheckPlacement(world, space, request, candidate);
                if (refusal.IsNone) { placement = candidate; return Code.None; }
                pending |= refusal == EntityRollout.Pending;
            }
            return pending ? EntityRollout.Pending : Codes.Blocked;
        }
        private static Code CheckMobileRollout(World world, ITileNavigationSpace space, EntityRolloutRequest request, TilePlacement placement)
        {
            if (request.Origin is not TilePlacement origin || origin.Space != space.Id || request.Producer.Component<ITileWorkDockDefinition>() is not ITileWorkDockDefinition layout)
                return Codes.InvalidArgument;
            TileSpaceGeometry geometry = SpatialSpaceContracts.ReadGeometry(world.Content.Get<ITileSpaceDefinition>(space.Id), world.Settings.Scenario);
            var unavailable = new List<TileRect>();
            foreach (var occupied in request.Unavailable)
            {
                if (occupied.Placement is not TilePlacement at || occupied.Definition.Component<ITilePresenceDefinition>() is not ITilePresenceDefinition shape) return Codes.InvalidArgument;
                unavailable.Add(new TileRect(at.Origin, shape.Width, shape.Height));
            }
            NavGrid grid = world.Services.Get<NavService>().Grid(request.Output.Component<IMobilityDefinition>().MovementClass);
            bool pending = false;
            foreach (var lane in WorkDockRules.RolloutsFor(request.Producer, request.Output))
            {
                bool selected = false;
                foreach (Code start in lane.Starts) if (layout.TryTile(geometry, origin.Origin, start, out TileCoord tile) && tile == placement.Origin) selected = true;
                if (!selected) continue;
                foreach (Code exit in lane.Exits)
                {
                    if (!layout.TryTile(geometry, origin.Origin, exit, out TileCoord goal)) continue;
                    if (goal == placement.Origin) throw new InvalidOperationException("launch and exit must occupy distinct actual geometry");
                    PathSearchResult route = grid.ProbeDefinitionRoute(request.Output, placement.Origin, goal, unavailable, lane.MaxExpanded);
                    if (route == PathSearchResult.Found) return Code.None;
                    if (route == PathSearchResult.InvalidRequest) throw new InvalidOperationException("authored rollout route rejected its actual prototype/context");
                    pending |= route == PathSearchResult.BudgetExceeded;
                }
            }
            return pending ? EntityRollout.Pending : Codes.Blocked;
        }

    }
}
