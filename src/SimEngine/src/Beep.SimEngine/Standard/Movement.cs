using Beep.SimEngine.Maps;
using System;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface ITileMoverDefinition : IMobilityDefinition, IMotionNavigationDefinition
    {
        ITileSpaceDefinition SpaceDefinition { get; }
        EntityStateBindings States { get; }
    }
    public interface ITileMover : IMobility, IMotionEstimate, IMotionAreaConstraint, IMotionNavigation,
        IEntityStateSource, IEntityCheckpointValidation, ITileMotionClearance { }

    /// <summary>Sole native tile mover declaration; contains no live position, route or time.</summary>
    public sealed class MoverDeclaration
    {
        private ResolvedContent _content;
        public ITileSpaceDefinition SpaceDefinition { get; private set; }
        public string MovementClass { get; }
        public IReadOnlyList<string> NavigationClasses { get; }
        public Fix TilesPerHour { get; }
        public EntityStateBindings States { get; private set; }
        private MoverDeclaration(JsonReader reader)
        {
            MovementClass = reader.String("class");
            if (!reader.Has("classes")) throw reader.Error("explicit allowed navigation classes required");
            var classes = reader.Strings("classes"); Array.Sort(classes, StringComparer.Ordinal);
            NavigationClasses = Array.AsReadOnly(classes);
            MotionNavigationContracts.ValidateChoices(NavigationClasses, MovementClass);
            TilesPerHour = reader.Fix("speed");
            if (!TilesPerHour.IsPositive) throw reader.Error("speed must be positive");
            States = EntityStateBindings.Read(reader, "moving", "stopped");
        }
        public static MoverDeclaration Read(JsonReader reader)
            => new MoverDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content)
        { _content = content.Content; States = States.Resolve(content); }
        public void ValidateBinding(EntityDef owner)
        {
            var presence = owner.Component<ITilePresenceDefinition>();
            if (presence == null || !presence.Movable || !owner.HasCapability<ITilePresence>())
                throw new ContentException(owner.Id + ": standard mover requires movable tile-presence contracts");
            SpaceDefinition = _content.Get<ITileSpaceDefinition>(presence.Space);
        }
    }
    public sealed class MoverDef : ComponentDef<Mover>, ITileMoverDefinition
    {
        private MoverDeclaration _declaration;
        public ITileSpaceDefinition SpaceDefinition => _declaration.SpaceDefinition;
        public string MovementClass => _declaration.MovementClass;
        public string InitialNavigation => MovementClass;
        public IReadOnlyList<string> NavigationClasses => _declaration.NavigationClasses;
        public Fix TilesPerHour => _declaration.TilesPerHour;
        public EntityStateBindings States => _declaration.States;
        protected override void ReadCore(JsonReader reader) => _declaration = MoverDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateCore(EntityDef owner) => _declaration.ValidateBinding(owner);
    }

    /// <summary>Where a vehicle is going and how far along its route it is. Moved by <see cref="MovementSystem"/>.</summary>
    public sealed class Mover : Component, ITileMover
    {
        public new ITileMoverDefinition Def => (ITileMoverDefinition)base.Def;
        private List<TileCoord> _path = new List<TileCoord>();
        private IReadOnlyList<TileCoord> _pathView;
        public Mover() { _pathView = _path.AsReadOnly(); }
        public MoveStatus Status { get; private set; }
        public TileRect Goal { get; private set; }
        public IReadOnlyList<TileCoord> Path => _pathView;
        public int PathIndex { get; private set; }
        public FixVec2 RouteOrigin { get; private set; }
        /// <summary>Game ticks this mover has spent moving since its last order (for telemetry and ETAs).</summary>
        public long TicksMoving { get; private set; }

        public TileRect? ReadMotionClearance()
        {
            if (Status != MoveStatus.Moving || PathIndex < 0 || PathIndex >= Path.Count) return null;
            FixVec2 from = PathIndex == 0 ? RouteOrigin : FixVec2.Centre(Path[PathIndex - 1]);
            FixVec2 position = Entity.Get<ITilePresence>().Position;
            TileCoord next = Path[PathIndex];
            if (position == from || position == FixVec2.Centre(next)) return null;
            TileCoord start = from.Tile;
            TileRect shape = Entity.Get<ITilePresence>().Footprint;
            return new TileRect(Math.Min(start.X, next.X), Math.Min(start.Y, next.Y),
                checked(Math.Abs(start.X - next.X) + shape.W), checked(Math.Abs(start.Y - next.Y) + shape.H));
        }

        public bool IsMoving => Status == MoveStatus.Moving;
        private string _movementClass;
        public override void OnCreate() => _movementClass = Def.InitialNavigation;
        public string MovementClass => _movementClass;
        public string ReadNavigation() => _movementClass;
        public Code CheckNavigation(World world, string profile)
        {
            if (world == null || !Entity.Alive || !ReferenceEquals(world.Entities.Get(Entity.Id), Entity) || world.Space?.Id != Def.SpaceDefinition.Id)
                return Codes.NotFound;
            if (!System.Linq.Enumerable.Contains(Def.NavigationClasses, profile)) return MotionNavigationCodes.NotAllowed;
            return world.Services.Get<NavService>().Find(profile) == null ? MotionNavigationCodes.Missing : Code.None;
        }
        public void ConfigureNavigation(World world, string profile)
        {
            Code refusal = CheckNavigation(world, profile); if (!refusal.IsNone) throw new InvalidOperationException("native navigation selection refused: " + refusal);
            if (_movementClass == profile) return;
            _ = checked(Entity.Version + 1); _movementClass = profile;
            if (Status == MoveStatus.Moving || Status == MoveStatus.Unreachable)
            {
                _path.Clear(); PathIndex = -1; RouteOrigin = Entity.Get<ITilePresence>().Position;
                Status = MoveStatus.Moving;
            }
            Entity.Touch();
        }
        MoveStatus IMotion.Status => Status;
        TileRect IMobility.Goal => Goal;
        public PathSearchResult CanReach(World world, TileRect goal)
            => world == null || !Entity.Alive || !ReferenceEquals(world.Entities.Get(Entity.Id), Entity) ||
                world.Space?.Id != Def.SpaceDefinition.Id || !ValidGoal(goal) ? PathSearchResult.InvalidRequest :
                world.Services.Get<NavService>().Grid(MovementClass).CanReach(Entity.Get<ITilePresence>().Tile, goal, actor: Entity);

        public IEntityStateDefinition GetState(EntityUpdateContext context)
            => Status == MoveStatus.Moving ? Def.States["moving"] : Status == MoveStatus.Unreachable ? Def.States["stopped"] : null;

        /// <summary>Sends the mover to any tile of <paramref name="goal"/>. The path is planned on its next move.</summary>
        public void GoTo(TileRect goal)
        {
            if (!ValidGoal(goal) || Entity.Get<ITilePresence>()?.Placed != true)
                throw new ArgumentException("native motion needs an in-bounds goal and actual placed presence", nameof(goal));
            _ = checked(Entity.Version + 1);
            Goal = goal;
            Status = MoveStatus.Moving;
            _path.Clear();
            PathIndex = -1;
            RouteOrigin = Entity.Get<ITilePresence>().Position;
            TicksMoving = 0;
            Entity.Touch();
        }

        public void Stop()
        {
            if (Status == MoveStatus.Idle) return;
            _ = checked(Entity.Version + 1);
            Status = MoveStatus.Idle;
            Goal = default;
            _path.Clear();
            PathIndex = 0;
            RouteOrigin = FixVec2.Zero;
            Entity.Touch();
        }

        private TileSpatialSnapshot At(FixVec2 position)
            => new TileSpatialSnapshot(Def.SpaceDefinition.Id, position.Tile, new TileRect(position.Tile,
                Entity.Get<ITilePresence>().Footprint.W, Entity.Get<ITilePresence>().Footprint.H), position, position, FixVec2.Zero, Entity.Get<ITilePresence>().Blocks, true);
        private SpatialRegionStatus Sweep(ISpatialRegion region, FixVec2 from, FixVec2 to)
            => MotionAreaContracts.CheckTravel(region, At(from), At(to));
        private bool PermitsStep(ISpatialRegion region, TileCoord from, TileCoord to)
            => Sweep(region, FixVec2.Centre(from), FixVec2.Centre(to)) == SpatialRegionStatus.Inside;
        private bool CurrentRouteAllowed(ISpatialRegion region)
        {
            if (PathIndex < 0) return false;
            FixVec2 from = Entity.Get<ITilePresence>().Position;
            for (int i = PathIndex; i < Path.Count; i++)
            {
                FixVec2 to = FixVec2.Centre(Path[i]);
                if (Sweep(region, from, to) != SpatialRegionStatus.Inside) return false;
                from = to;
            }
            return Goal.Contains(from.Tile);
        }
        public Code CheckArea(World world, ISpatialRegion region)
        {
            if (world == null || region == null || !Entity.Alive || !ReferenceEquals(world.Entities.Get(Entity.Id), Entity) ||
                world.Space?.Id != Def.SpaceDefinition.Id || Status != MoveStatus.Moving)
                throw new ArgumentException("route-area admission needs actual active session motion");
            FixVec2 from = Entity.Get<ITilePresence>().Position;
            var initial = Sweep(region, from, from);
            if (initial != SpatialRegionStatus.Inside) return initial == SpatialRegionStatus.Outside ? MotionAreaCodes.Outside : MotionAreaCodes.Unsupported;
            if (CurrentRouteAllowed(region)) return Code.None;
            var result = world.Services.Get<NavService>().Grid(MovementClass).CanReach(from.Tile, Goal,
                permitsStep: (a, b) => PermitsStep(region, a, b), actor: Entity);
            return result == PathSearchResult.Found ? Code.None : result == PathSearchResult.BudgetExceeded ? MotionAreaCodes.Pending : MotionAreaCodes.Outside;
        }
        public MotionRouteCheck CheckRoute(World world, ISpatialRegion region, SpatialSnapshot origin, MotionDestination destination, Fix speedModifier)
        {
            if (world == null || destination == null || speedModifier < Fix.Zero || !Entity.Alive || !ReferenceEquals(world.Entities.Get(Entity.Id), Entity) ||
                world.Space?.Id != Def.SpaceDefinition.Id) throw new ArgumentException("prospective route requires actual native session motion");
            MotionRouteCheck Refuse(Code code) => new MotionRouteCheck(code, null, null);
            if (origin is not TileSpatialSnapshot from || destination.Geometry is not TileSpatialSnapshot to ||
                from.Space != Def.SpaceDefinition.Id || to.Space != Def.SpaceDefinition.Id ||
                from.Footprint.W != Entity.Get<ITilePresence>().Footprint.W || from.Footprint.H != Entity.Get<ITilePresence>().Footprint.H ||
                from.Position.Tile != from.Tile || !InBounds(from.Tile) || !ValidGoal(to.Footprint)) return Refuse(MotionAreaCodes.Unsupported);
            SpatialRegionStatus initial = region == null ? SpatialRegionStatus.Inside : Sweep(region, from.Position, from.Position);
            if (initial != SpatialRegionStatus.Inside) return Refuse(initial == SpatialRegionStatus.Outside ? MotionAreaCodes.Outside : MotionAreaCodes.Unsupported);
            TileRect goal = destination.Kind == MotionDestinationKind.Exact ? to.Footprint :
                ((ITileNavigationSpace)world.Space).InteractionGoal(to.Footprint, from.Footprint.W, from.Footprint.H);
            if (destination.Kind == MotionDestinationKind.Exact && (goal.W != 1 || goal.H != 1)) return Refuse(MotionAreaCodes.Unsupported);
            bool unsupported = false;
            bool Permit(TileCoord a, TileCoord b)
            {
                var status = Sweep(region, a == from.Tile ? from.Position : FixVec2.Centre(a), FixVec2.Centre(b));
                unsupported |= status == SpatialRegionStatus.Unsupported; return status == SpatialRegionStatus.Inside;
            }
            NavGrid grid = world.Services.Get<NavService>().Grid(MovementClass);
            MotionRouteCheck ReadPath(IReadOnlyList<TileCoord> path, int first, bool retained)
            {
                var reached = At(first == path.Count ? from.Position : FixVec2.Centre(path[path.Count - 1]));
                MotionTravelEstimate travel;
                try
                {
                    FixVec2 position = from.Position; BigInteger remaining = BigInteger.Zero;
                    for (int i = first; i < path.Count; i++)
                    {
                        if (retained && region != null && Sweep(region, position, FixVec2.Centre(path[i])) != SpatialRegionStatus.Inside)
                            return null;
                        if (!TrySegment(tile => grid.StructuralCostFor(Entity, tile), position, path[i], out var target, out _, out Fix needed))
                            return null;
                        remaining += needed.Raw; position = target;
                    }
                    travel = first == path.Count ? new MotionTravelEstimate(0, Code.None) : MotionArrivalContracts.FromBudget(remaining, TickBudget(world, speedModifier));
                }
                catch (OverflowException)
                {
                    // A retained suffix is not a complete feasibility proof until every live
                    // segment is checked. Let the original search prove it before an unknown ETA.
                    if (retained) return null;
                    travel = new MotionTravelEstimate(null, MotionArrivalCodes.Overflow);
                }
                return new MotionRouteCheck(Code.None, reached, travel);
            }
            // The sole mover already owns this physical path. Reuse no hypothetical origin,
            // goal or cached cost: re-read current structural origin/segments/corners and sweeps.
            if (Status == MoveStatus.Moving && PathIndex >= 0 && PathIndex < Path.Count && goal.Equals(Goal) &&
                from.Position == Entity.Get<ITilePresence>().Position && grid.StructuralCostFor(Entity, from.Tile) > 0)
            {
                MotionRouteCheck current = ReadPath(Path, PathIndex, true);
                if (current != null && Goal.Contains(((TileSpatialSnapshot)current.Arrival).Tile)) return current;
            }
            var path = new List<TileCoord>();
            PathSearchResult result = grid.ProbeStructuralRoute(Entity, from.Tile, goal, out TileCoord arrival, region == null ? null : Permit, path: path);
            if (result != PathSearchResult.Found)
                return Refuse(result == PathSearchResult.BudgetExceeded ? MotionAreaCodes.Pending : unsupported ? MotionAreaCodes.Unsupported : MotionAreaCodes.Outside);
            return ReadPath(path, 0, false) ?? Refuse(MotionAreaCodes.Outside);
        }

        private Fix TickBudget(World world, Fix speedModifier)
            => world.Time.RateFor(Def.TilesPerHour * speedModifier, DurationUnit.Hour);
        private bool TrySegment(Func<TileCoord, int> costFor, FixVec2 pos, TileCoord next, out FixVec2 target, out Fix factor, out Fix needed)
        {
            target = default; factor = needed = Fix.Zero;
            TileCoord current = pos.Tile;
            int cost = costFor(next);
            bool blockedCorner = next.X != current.X && next.Y != current.Y
                && (costFor(new TileCoord(next.X, current.Y)) <= 0 || costFor(new TileCoord(current.X, next.Y)) <= 0);
            if (cost <= 0 || blockedCorner) return false;
            target = FixVec2.Centre(next); factor = Fix.FromRatio(cost, 10);
            BigInteger charge = new BigInteger((target - pos).LengthCeiling.Raw) * factor.Raw;
            needed = Fix.FromRaw(checked((long)((charge + (BigInteger.One << Fix.FractionalBits) - 1) >> Fix.FractionalBits)));
            return true;
        }

        public MotionTravelEstimate EstimateRemaining(World world, Fix speedModifier)
        {
            if (Status != MoveStatus.Moving) throw new InvalidOperationException("only an actual moving route can be estimated");
            if (PathIndex < 0) return new MotionTravelEstimate(null, MotionArrivalCodes.RoutePending);
            if (PathIndex >= Path.Count) throw new CorruptStateException("invalid current motion path index");
            ISpatialRegion region = WorkPolicyRules.MotionRegion(world, Entity);
            if (region != null && !CurrentRouteAllowed(region)) return new MotionTravelEstimate(null, MotionArrivalCodes.RouteChanged);
            NavGrid grid = world.Services.Get<NavService>().Grid(MovementClass);
            FixVec2 pos = Entity.Get<ITilePresence>().Position;
            BigInteger remaining = BigInteger.Zero;
            for (int i = PathIndex; i < Path.Count; i++)
            {
                if (!TrySegment(tile => grid.CostFor(Entity, tile), pos, Path[i], out var target, out _, out Fix needed))
                    return new MotionTravelEstimate(null, MotionArrivalCodes.RouteChanged);
                remaining += needed.Raw;
                pos = target;
            }
            if (!Goal.Contains(pos.Tile)) return new MotionTravelEstimate(null, MotionArrivalCodes.RouteChanged);
            return MotionArrivalContracts.FromBudget(remaining, TickBudget(world, speedModifier));
        }

        public void Advance(World world, TickInfo tick, Fix speedModifier)
        {
            if (world == null || !Entity.Alive || !ReferenceEquals(world.Entities.Get(Entity.Id), Entity) ||
                world.Space?.Id != Def.SpaceDefinition.Id || Status != MoveStatus.Moving || !speedModifier.IsPositive)
                throw new ArgumentException("native motion advances only actual active session state");
            ValidatePresence();
            ISpatialRegion region = WorkPolicyRules.MotionRegion(world, Entity);
            if (region != null && !CheckArea(world, region).IsNone) return;
            if (PathIndex < 0 && Goal.Contains(Entity.Get<ITilePresence>().Tile)) { FinishRoute(MoveStatus.Arrived); return; }
            long nextAge = checked(TicksMoving + 1);
            Fix budget = TickBudget(world, speedModifier);
            NavGrid grid = world.Services.Get<NavService>().Grid(MovementClass);
            if (region != null && PathIndex >= 0 && !CurrentRouteAllowed(region))
            { PathIndex = -1; _path.Clear(); RouteOrigin = Entity.Get<ITilePresence>().Position; }
            if (PathIndex < 0)
            {
                RouteOrigin = Entity.Get<ITilePresence>().Position;
                PathSearchResult result = grid.FindPath(Entity.Get<ITilePresence>().Tile, Goal, _path,
                    permitsStep: region == null ? null : (a, b) => PermitsStep(region, a, b), actor: Entity);
                if (result == PathSearchResult.BudgetExceeded) return;
                if (result == PathSearchResult.Unreachable)
                {
                    // Traffic is temporary, not a changed destination. Keep the original goal and
                    // physical origin pending while a structural route still exists (or its bounded
                    // proof is pending). Static topology/area failure remains genuinely unreachable.
                    PathSearchResult structural = grid.ProbeStructuralRoute(Entity, Entity.Get<ITilePresence>().Tile,
                        Goal, out _, region == null ? null : (a, b) => PermitsStep(region, a, b));
                    if (structural == PathSearchResult.Unreachable) FinishRoute(MoveStatus.Unreachable);
                    else if (structural != PathSearchResult.Found && structural != PathSearchResult.BudgetExceeded)
                        throw new InvalidOperationException("active native route failed structural request validation");
                    return;
                }
                PathIndex = 0;
            }
            TicksMoving = nextAge;
            FixVec2 pos = Entity.Get<ITilePresence>().Position;
            while (budget > Fix.Zero && PathIndex < Path.Count)
            {
                if (region != null && Sweep(region, pos, FixVec2.Centre(Path[PathIndex])) != SpatialRegionStatus.Inside)
                    throw new InvalidOperationException("admitted route-area sweep changed during physical advance");
                if (!TrySegment(tile => grid.CostFor(Entity, tile), pos, Path[PathIndex], out var target, out Fix factor, out Fix needed))
                {
                    // Keep physical right of way instead of both crossing movers
                    // repeatedly choosing the same detour. This probe permits WAITING
                    // only; the unchanged live check above must pass before any step.
                    if (TrySegment(tile => grid.WaitingTrafficCostFor(Entity, tile), pos, Path[PathIndex], out _, out _, out _)) break;
                    // A changed route is replanned from the current physical position next tick.
                    PathIndex = -1; _path.Clear(); RouteOrigin = pos; break;
                }
                if (budget >= needed) { pos = target; budget -= needed; PathIndex++; }
                else { pos = FixVec2.MoveTowards(pos, target, budget / factor); budget = Fix.Zero; }
            }
            Entity.Get<ITilePresence>().Move(world, pos);
            if (PathIndex >= 0 && PathIndex >= Path.Count && Goal.Contains(Entity.Get<ITilePresence>().Tile))
            {
                FinishRoute(MoveStatus.Arrived);
            }
        }

        private void FinishRoute(MoveStatus status)
        { Status = status; _path.Clear(); PathIndex = 0; RouteOrigin = FixVec2.Zero; }
        private TileSpaceGeometry Geometry => SpatialSpaceContracts.ReadGeometry(Def.SpaceDefinition, Entity.Scenario);
        private bool InBounds(TileCoord tile) => Geometry.Contains(new TileRect(tile,
            Entity.Get<ITilePresence>().Footprint.W, Entity.Get<ITilePresence>().Footprint.H));
        private bool ValidGoal(TileRect goal) => Geometry.Contains(goal);
        private static bool Adjacent(TileCoord a, TileCoord b) => a != b && System.Math.Abs((long)a.X - b.X) <= 1 && System.Math.Abs((long)a.Y - b.Y) <= 1;
        private void ValidatePayload(MoveStatus status, TileRect goal, IReadOnlyList<TileCoord> path, int index, long age, FixVec2 origin)
        {
            if ((byte)status > (byte)MoveStatus.Unreachable || age < 0 ||
                (status == MoveStatus.Idle ? goal != default : !ValidGoal(goal)))
                throw new CorruptStateException("invalid native motion status, age or authored goal bounds");
            if (status != MoveStatus.Moving)
            {
                if (path.Count != 0 || index != 0 || origin != FixVec2.Zero) throw new CorruptStateException("ended motion owns an active route");
                return;
            }
            if (!InBounds(origin.Tile) || index < -1 || index == -1 && path.Count != 0 || index >= 0 && (path.Count == 0 || index >= path.Count))
                throw new CorruptStateException("invalid active native route origin/index/phase");
            TileCoord previous = origin.Tile;
            for (int i = 0; i < path.Count; i++)
            {
                if (!InBounds(path[i]) || !Adjacent(previous, path[i])) throw new CorruptStateException("native route has out-of-bounds or disconnected steps");
                previous = path[i];
            }
            if (path.Count > 0 && !goal.Contains(previous)) throw new CorruptStateException("native route ends outside its actual goal");
        }
        public void ValidateCheckpoint()
        {
            ValidatePayload(Status, Goal, Path, PathIndex, TicksMoving, RouteOrigin);
            ValidatePresence();
        }
        private void ValidatePresence()
        {
            ITilePresence presence = Entity.Get<ITilePresence>();
            if (presence?.Placed != true || !presence.Movable || presence.Space != Def.SpaceDefinition.Id ||
                !InBounds(presence.Tile) || presence.Position.Tile != presence.Tile)
                throw new CorruptStateException("native motion disagrees with actual placed mobile presence");
            if (Status == MoveStatus.Arrived && !Goal.Contains(presence.Tile)) throw new CorruptStateException("arrived motion is not physically at its goal");
            if (Status != MoveStatus.Moving) return;
            if (PathIndex == -1)
            {
                if (RouteOrigin != presence.Position) throw new CorruptStateException("pending route origin differs from actual position");
                return;
            }
            FixVec2 from = PathIndex == 0 ? RouteOrigin : FixVec2.Centre(Path[PathIndex - 1]), to = FixVec2.Centre(Path[PathIndex]);
            if (presence.Position.X < Fix.Min(from.X, to.X) || presence.Position.X > Fix.Max(from.X, to.X) ||
                presence.Position.Y < Fix.Min(from.Y, to.Y) || presence.Position.Y > Fix.Max(from.Y, to.Y))
                throw new CorruptStateException("actual position is outside its current native route segment");
        }
        public override void Sync(IStateSync s)
        {
            string selected = _movementClass; s.String(ref selected);
            if (!System.Linq.Enumerable.Contains(Def.NavigationClasses, selected)) throw new CorruptStateException("saved native navigation class is not declared");
            if (!s.Reading) ValidateCheckpoint();
            int rawStatus = (int)Status; s.Int(ref rawStatus);
            if (rawStatus < 0 || rawStatus > (int)MoveStatus.Unreachable) throw new CorruptStateException("invalid native motion status encoding");
            MoveStatus status = (MoveStatus)rawStatus;
            int gx = Goal.X, gy = Goal.Y, gw = Goal.W, gh = Goal.H;
            s.Int(ref gx); s.Int(ref gy); s.Int(ref gw); s.Int(ref gh); var goal = new TileRect(gx, gy, gw, gh);
            int n = s.Count(_path.Count); var candidate = s.Reading ? new List<TileCoord>() : _path;
            for (int i = 0; i < n; i++)
            { TileCoord tile = s.Reading ? default : _path[i]; s.Tile(ref tile); if (s.Reading) candidate.Add(tile); }
            int index = PathIndex; long age = TicksMoving; FixVec2 origin = RouteOrigin;
            s.Int(ref index); s.Long(ref age); s.Vec(ref origin);
            ValidatePayload(status, goal, candidate, index, age, origin);
            if (s.Reading)
            { _movementClass = selected; Status = status; Goal = goal; _path = candidate; _pathView = _path.AsReadOnly(); PathIndex = index; TicksMoving = age; RouteOrigin = origin; }
        }
    }

    /// <summary>
    /// Advances every active motion owner once per tick. Its provider owns coordinates, routes and
    /// physical travel. Authored execution capabilities own readiness and speed multipliers.
    /// </summary>
    public sealed class MovementSystem : GameSystem, ICompositionValidator
    {
        public override int StateSchemaVersion => 6;
        public override string Id => "sim.movement";

        private World _world;
        public void ValidateComposition(World world) => MotionExecutionContracts.ValidateDefinitions(world);
        public override void Initialize(World world) { _world = world; }
        public override void Sync(IStateSync sync) { if (!sync.Reading) MotionExecutionContracts.ValidateLive(_world); }
        public override void OnLoaded(World world)
        {
            try { MotionExecutionContracts.ValidateLive(world); }
            catch (ArgumentException error) { throw new CorruptStateException(error.Message); }
            catch (InvalidOperationException error) { throw new CorruptStateException(error.Message); }
        }

        public override void Update(World world, TickInfo tick)
        {
            MotionExecutionContracts.ValidateLive(world);
            ComponentList<IMotion> movers = world.Entities.Components<IMotion>();
            for (int i = 0; i < movers.Count; i++)
            {
                IMotion m = movers[i];
                if (m.Status != MoveStatus.Moving || !m.Entity.Alive) continue;
                var value = MotionExecutionContracts.Read(world, m.Entity);
                if (!value.Blocker.IsNone) continue;
                if (!OperatingInputsContracts.Commit(world, m.Entity, OperatingPhase.Movement)) continue;
                m.Advance(world, tick, value.Rates.Rate);
            }
        }

    }
}
