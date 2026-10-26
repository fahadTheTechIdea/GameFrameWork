using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Standard
{
    /// <summary>Immutable tile facility geometry. Existing jobs own claims and existing presence owns coordinates.</summary>
    public interface ITileWorkDockDefinition : IWorkDockDefinition
    { bool TryTile(TileSpaceGeometry geometry, TileCoord origin, Code slot, out TileCoord tile); }

    public sealed class TileWorkDockDef : ComponentDef<TileWorkDock>, ITileWorkDockDefinition, IWorkDockRolloutDefinition, ITilePlacementClearance, ITileNavigationOriginConstraint
    {
        private readonly Dictionary<Code, TileCoord> _offsets = new Dictionary<Code, TileCoord>();
        private readonly List<WorkDockSlotDeclaration> _slots = new List<WorkDockSlotDeclaration>();
        private string[] _roles, _resources;
        private ResolvedContent _content;
        private IReadOnlyList<WorkDockRolloutDeclaration> _rollouts;
        public IReadOnlyList<WorkDockRollout> Rollouts { get; private set; }
        public IReadOnlyList<WorkDockSlot> Slots { get; private set; }
        public IReadOnlyList<WorkerRoleId> WorkRoles { get; private set; }
        public IReadOnlyList<ResourceId> Resources { get; private set; }
        public ITileSpaceDefinition SpaceDefinition { get; private set; }
        public TileCoord Offset(Code slot) => _offsets.TryGetValue(slot, out var value) ? value : throw new ArgumentException("unknown tile dock slot", nameof(slot));
        protected override void ReadCore(JsonReader reader)
        {
            if (!reader.Has("work_roles") || !reader.Has("resources")) throw reader.Error("explicit work_roles/resources arrays are required");
            _roles = reader.Strings("work_roles"); _resources = reader.Strings("resources");
            _rollouts = WorkDockRolloutDeclaration.Read(reader);
            var rows = reader.Array("slots") ?? throw reader.Error("explicit slots are required");
            var positions = new HashSet<TileCoord>();
            for (int i = 0; i < rows.Count; i++)
            {
                var row = new JsonReader(rows[i], reader.Where + ".slots[" + i + "]");
                var declaration = WorkDockSlotDeclaration.Read(row); var offset = new TileCoord(row.Int("x"), row.Int("y")); row.End();
                var code = declaration.Id;
                if (_offsets.ContainsKey(code) || !positions.Add(offset)) throw row.Error("slot identities and relative tiles must be unique");
                _offsets.Add(code, offset);
                _slots.Add(declaration);
            }
        }
        protected override void ResolveCore(ContentResolver content)
        {
            _content = content.Content;
            var roles = new WorkerRoleId[_roles.Length]; var roleSet = new HashSet<WorkerRoleId>();
            for (int i = 0; i < roles.Length; i++)
            { roles[i] = content.Def<IWorkerRoleDefinition>(_roles[i]).Role; if (!roleSet.Add(roles[i])) throw content.Fail("duplicate tile dock work role"); }
            WorkRoles = Array.AsReadOnly(roles);
            var resources = new ResourceId[_resources.Length]; var resourceSet = new HashSet<ResourceId>();
            for (int i = 0; i < resources.Length; i++)
            { resources[i] = content.Resource(_resources[i]); if (!resourceSet.Add(resources[i])) throw content.Fail("duplicate tile dock resource"); }
            Resources = Array.AsReadOnly(resources);
            Slots = Array.AsReadOnly(_slots.ConvertAll(slot => slot.Resolve(content)).ToArray());
            var rollouts = new List<WorkDockRollout>(); foreach (var lane in _rollouts) rollouts.Add(lane.Resolve(content)); Rollouts = rollouts.AsReadOnly();
        }
        protected override void ValidateCore(EntityDef owner)
        {
            WorkDockRules.ValidateDefinition(owner);
            WorkDockRules.ValidateRollouts(_content, owner);
            foreach (var lane in Rollouts) foreach (var output in lane.Outputs)
                if (output.Component<ITilePresenceDefinition>()?.Movable != true || !output.HasCapability<IMobility>() || output.Component<IMobilityDefinition>() == null)
                    throw new ContentException(owner.Id + ": tile launch outputs require actual movable tile presence and mobility metadata");
            var presence = owner.Component<ITilePresenceDefinition>();
            if (presence == null || !owner.HasCapability<ITilePresence>() || presence.Movable && !owner.HasCapability<IMotion>())
                throw new ContentException(owner.Id + ": tile dock requires tile presence and an actual motion capability when movable");
            WorkDockRules.ValidateResourceService(owner, _content, Resources);
            WorkDockRules.ValidateRequests(this, WorkRoles, Resources);
            SpaceDefinition = _content.Get<ITileSpaceDefinition>(presence.Space);
            foreach (var offset in _offsets.Values)
            {
                bool fits = false;
                foreach (TileSpaceGeometry geometry in SpaceDefinition.Geometries)
                    if (Math.Abs((long)offset.X) < geometry.Width && Math.Abs((long)offset.Y) < geometry.Height) { fits = true; break; }
                if (!fits)
                    throw new ContentException(owner.Id + ": tile dock offset cannot fit its authored space");
                if (presence.Blocks && offset.X >= 0 && offset.Y >= 0 && offset.X < presence.Width && offset.Y < presence.Height)
                    throw new ContentException(owner.Id + ": blocking facility footprint cannot contain a physical dock slot");
            }
        }
        public bool TryTile(TileSpaceGeometry geometry, TileCoord origin, Code slot, out TileCoord tile)
        {
            TileCoord offset = Offset(slot); long x = (long)origin.X + offset.X, y = (long)origin.Y + offset.Y;
            if (x < 0 || y < 0 || x >= geometry.Width || y >= geometry.Height) { tile = default; return false; }
            tile = new TileCoord((int)x, (int)y); return true;
        }
        public Code CheckOrigin(TileSpaceGeometry geometry, TileCoord origin)
        { foreach (var slot in Slots) if (!TryTile(geometry, origin, slot.Id, out _)) return Codes.OutOfBounds; return Code.None; }
        public Code CheckNavigationOrigin(World world, TileCoord origin)
            => world?.Space?.Id == SpaceDefinition.Id
                ? CheckOrigin(SpatialSpaceContracts.ReadGeometry(SpaceDefinition, world.Settings.Scenario), origin) : Codes.InvalidArgument;
        public Code CheckTilePlacement(World world, EntityDef definition, TilePlacement placement, Entity replacing)
        {
            if (world?.Space is not ITileNavigationSpace map || definition == null || placement == null || placement.Space != map.Id || map.Id != SpaceDefinition.Id ||
                !ReferenceEquals(world.Content.Find<EntityDef>(definition?.Id), definition) || !ReferenceEquals(definition.Component<TileWorkDockDef>(), this)) return Codes.InvalidArgument;
            TileSpaceGeometry geometry = SpatialSpaceContracts.ReadGeometry(SpaceDefinition, world.Settings.Scenario);
            Code refusal = CheckOrigin(geometry, placement.Origin); if (!refusal.IsNone) return refusal;
            foreach (var slot in Slots)
            {
                TryTile(geometry, placement.Origin, slot.Id, out var tile); Entity occupant = map.OccupantAt(tile);
                if (occupant != null && !ReferenceEquals(occupant, replacing) && occupant.Get<ITilePresence>()?.Movable != true) return Codes.Blocked;
            }
            return Code.None;
        }
        public Code CheckTileObstruction(World world, EntityDef definition, TilePlacement placement, TileRect obstruction)
        {
            if (world?.Space is not ITileNavigationSpace map || definition == null || placement == null || placement.Space != map.Id || map.Id != SpaceDefinition.Id ||
                !ReferenceEquals(world.Content.Find<EntityDef>(definition.Id), definition) || !ReferenceEquals(definition.Component<TileWorkDockDef>(), this) ||
                obstruction.W < 1 || obstruction.H < 1) return Codes.InvalidArgument;
            TileSpaceGeometry geometry = SpatialSpaceContracts.ReadGeometry(SpaceDefinition, world.Settings.Scenario);
            Code refusal = CheckOrigin(geometry, placement.Origin); if (!refusal.IsNone) return refusal;
            foreach (var slot in Slots)
            {
                TryTile(geometry, placement.Origin, slot.Id, out var tile);
                if (obstruction.Contains(tile)) return Codes.Blocked;
            }
            return Code.None;
        }
    }

    /// <summary>Native geometry implementation only: no queue, worker/stock claims, route, progress or clock.</summary>
    public sealed class TileWorkDock : Component<TileWorkDockDef>, IWorkDock, IEntityInitialization, IEntityCheckpointValidation
    {
        public IReadOnlyList<WorkDockSlot> Slots => Def.Slots;
        public SpatialSnapshot CaptureSlot(World world, Code slot)
        {
            var presence = Entity.Get<ITilePresence>();
            if (world == null || !ReferenceEquals(world.Entities.Get(Entity.Id), Entity) || presence?.Placed != true ||
                presence.Space != world.Space.Id || !Def.TryTile(SpatialSpaceContracts.ReadGeometry(Def.SpaceDefinition, world.Settings.Scenario),
                    presence.Tile, slot, out var tile))
                throw new InvalidOperationException("invalid actual tile dock geometry");
            var point = FixVec2.Centre(tile);
            return new TileSpatialSnapshot(presence.Space, tile, new TileRect(tile, 1, 1), point, point, FixVec2.Zero, false, false);
        }
        private bool Live(World world, Entity actor)
        {
            if (world?.Space is not ITileNavigationSpace map || actor == null || !Entity.Alive || !actor.Alive ||
                !ReferenceEquals(world.Entities.Get(Entity.Id), Entity) || !ReferenceEquals(world.Entities.Get(actor.Id), actor) || map.Id != Def.SpaceDefinition.Id) return false;
            var facility = Entity.Get<ITilePresence>(); var presence = actor.Get<ITilePresence>(); var mobility = actor.Get<IMobility>();
            return facility?.Placed == true && presence?.Placed == true && facility.Space == map.Id && presence.Space == map.Id &&
                presence.Movable && mobility != null && ReferenceEquals(mobility.Entity, actor);
        }
        private bool Goal(World world, Entity actor, Code slot, out TileRect goal)
        {
            goal = default;
            if (!Live(world, actor) || !Def.TryTile(SpatialSpaceContracts.ReadGeometry(Def.SpaceDefinition, world.Settings.Scenario),
                Entity.Get<ITilePresence>().Tile, slot, out var tile)) return false;
            goal = new TileRect(tile, 1, 1); return true;
        }
        public Code CheckRequest(World world, Entity carrier, WorkDockRequest request)
        {
            if (!request.IsValid || !Live(world, carrier)) return WorkDockCodes.Incompatible;
            if (request.IsWork) { foreach (var role in Def.WorkRoles) if (role == request.Role) return Code.None; }
            else foreach (var resource in Def.Resources) if (resource == request.Resource) return Code.None;
            return WorkDockCodes.Incompatible;
        }
        public Code CheckVacancy(World world, Entity carrier, Code slot)
        {
            if (!Goal(world, carrier, slot, out var goal)) return WorkDockCodes.Incompatible;
            ITilePresence shape = carrier.Get<ITilePresence>();
            var footprint = new TileRect(goal.Origin, shape.Footprint.W, shape.Footprint.H);
            if (!((ITileSpatialSpace)world.Space).InBounds(footprint)) return WorkDockCodes.Incompatible;
            foreach (Entity occupant in world.Entities.All)
            {
                if (!occupant.Alive || ReferenceEquals(occupant, carrier)) continue;
                var presence = occupant.Get<ITilePresence>();
                if (presence?.Placed == true && presence.Space == world.Space.Id && presence.Footprint.Overlaps(footprint) &&
                    (presence.Blocks || occupant.Has<IWorker>() || occupant.Has<ITransporter>())) return WorkDockCodes.Occupied;
            }
            return Code.None;
        }
        public bool TryEstimateCost(World world, Entity carrier, Code slot, SpatialSnapshot origin, out long cost)
        {
            cost = 0; if (!Goal(world, carrier, slot, out var goal)) return false;
            if (origin != null && (origin is not TileSpatialSnapshot || origin.Space != world.Space.Id)) return false;
            cost = goal.StepsTo(origin is TileSpatialSnapshot point ? point.Tile : carrier.Get<ITilePresence>().Tile); return true;
        }
        public bool IsAtSlot(World world, Entity carrier, Code slot)
            => Goal(world, carrier, slot, out var goal) && goal.Contains(carrier.Get<ITilePresence>().Tile);
        public bool IsApproachingSlot(World world, Entity carrier, Code slot)
            => Goal(world, carrier, slot, out var goal) && carrier.Get<IMobility>().Status == MoveStatus.Moving && carrier.Get<IMobility>().Goal == goal;
        public SpatialInteractionStatus Check(World world, Entity carrier, Code slot)
        {
            if (!Goal(world, carrier, slot, out var goal)) return SpatialInteractionStatus.Unsupported;
            if (IsAtSlot(world, carrier, slot)) return SpatialInteractionStatus.WithinReach;
            PathSearchResult route = carrier.Get<IMobility>().CanReach(world, goal);
            return route == PathSearchResult.Found ? SpatialInteractionStatus.Reachable : route == PathSearchResult.BudgetExceeded ? SpatialInteractionStatus.Pending :
                route == PathSearchResult.Unreachable ? SpatialInteractionStatus.Unreachable : SpatialInteractionStatus.Unsupported;
        }
        public SpatialInteractionStatus Approach(World world, Entity carrier, Code slot)
        {
            if (!Goal(world, carrier, slot, out var goal)) return SpatialInteractionStatus.Unsupported;
            var mobility = carrier.Get<IMobility>();
            if (goal.Contains(carrier.Get<ITilePresence>().Tile) && mobility.Status != MoveStatus.Moving) return SpatialInteractionStatus.WithinReach;
            if (mobility.Status == MoveStatus.Moving && mobility.Goal == goal) return SpatialInteractionStatus.Reachable;
            SpatialInteractionStatus status = Check(world, carrier, slot);
            if (!SpatialInteraction.CanApproach(status)) return status;
            mobility.GoTo(goal); return status == SpatialInteractionStatus.Pending ? status : SpatialInteractionStatus.Reachable;
        }
        public Code CheckRemoval(World world) => world.Services.Get<WorkBoard>().CheckDockRemoval(Entity);
        public void Initialize(World world) => ValidateCheckpoint();
        public void ValidateCheckpoint()
        {
            var presence = Entity.Get<ITilePresence>();
            TileSpaceGeometry geometry = SpatialSpaceContracts.ReadGeometry(Def.SpaceDefinition, Entity.Scenario);
            if (presence?.Placed != true || presence.Space != Def.SpaceDefinition.Id ||
                !geometry.Contains(presence.Footprint) || !Def.CheckOrigin(geometry, presence.Tile).IsNone)
                throw new CorruptStateException("tile dock presence disagrees with its authored facility geometry");
        }
    }
}
