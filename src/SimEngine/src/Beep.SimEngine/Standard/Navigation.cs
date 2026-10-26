using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Content;

namespace Beep.SimEngine.Standard
{
    public enum PathSearchResult : byte
    {
        Found = 0,
        Unreachable = 1,
        BudgetExceeded = 2,
        InvalidRequest = 3
    }

    /// <summary>
    /// How one movement class (land vehicles, vessels) sees the map: a cost per tile to enter it, where
    /// 10 is ordinary ground, less is faster (a road), more is slower (forest, mud) and 0 is impassable.
    /// The game supplies the cost rule; the framework searches a bounded, sparse frontier.
    /// </summary>
    public sealed class NavGrid
    {
        public const int DefaultExpansionBudget = 200000;
        private readonly World _world;
        private readonly Func<TileCoord, int> _cost;
        private readonly ITileNavigationRule _rule;

        private readonly PathSearch _search;
        private ITileNavigationSpace Map
        {
            get {
                _world.RequireHealthyUpdate();
                return _world.Space as ITileNavigationSpace
                    ?? throw new InvalidOperationException("navigation requires its selected occupancy/geometry contract");
            }
        }
        private Entity Occupant(Entity actor, TileCoord tile)
        {
            var map = Map;
            if (!map.InBounds(tile)) return null;
            return EntityObservation.ReadProvider(_world, map, actor, () => OccupantCore(map, tile), map);
        }
        private Entity OccupantCore(ITileNavigationSpace map, TileCoord tile)
        {
            Entity occupant = map.OccupantAt(tile);
            if (occupant != null && (!occupant.Alive || !ReferenceEquals(_world.Entities.Get(occupant.Id), occupant) ||
                occupant.Get<ITilePresence>() is not ITilePresence presence || !presence.Placed || !presence.Blocks ||
                presence.Space != map.Id || !presence.Footprint.Contains(tile)))
                throw new InvalidOperationException("navigation occupancy must identify its actual live blocking presence");
            return occupant;
        }
        private int EntryCost(Entity actor, TileCoord tile, bool structural, bool waitingProbe)
        {
            var map = Map;
            if (!map.InBounds(tile) || !OriginAllowed(actor, tile)) return 0;
            object provider = _rule ?? (_cost.Target ?? (object)_cost);
            // One rule/actor/selected-space scope covers occupancy and terrain together.
            return EntityObservation.ReadProvider(_world, provider, actor, (this, map, actor, tile, structural, waitingProbe), ReadEntry, map);
        }
        private static readonly Func<(NavGrid Grid, ITileNavigationSpace Map, Entity Actor, TileCoord Tile, bool Structural, bool WaitingProbe), int> ReadEntry = ReadEntryCore;
        private static int ReadEntryCore((NavGrid Grid, ITileNavigationSpace Map, Entity Actor, TileCoord Tile, bool Structural, bool WaitingProbe) context)
        {
            NavGrid grid = context.Grid; var map = context.Map; Entity actor = context.Actor; TileCoord tile = context.Tile;
            ITilePresence shape = actor?.Get<ITilePresence>();
            int width = shape?.Footprint.W ?? 1, height = shape?.Footprint.H ?? 1, maximum = 0;
            if (!map.InBounds(new TileRect(tile, width, height))) return 0;
            for (int y = tile.Y; y < (long)tile.Y + height; y++) for (int x = tile.X; x < (long)tile.X + width; x++)
            {
                var cell = new TileCoord(x, y);
                Entity occupant = grid.OccupantCore(map, cell);
                if (occupant != null && !ReferenceEquals(occupant, actor) &&
                    !(context.Structural && occupant.Get<ITilePresence>()?.Movable == true) &&
                    !(context.WaitingProbe && grid.YieldingTraffic(actor, occupant))) return 0;
                if (!context.Structural && grid.MotionClearanceBlocked(map, actor, cell, context.WaitingProbe)) return 0;
                int value = grid._rule != null ? TileNavigationContracts.ReadCostCore((grid._world, grid._rule, grid.MinCost, cell)) : grid._cost(cell);
                if (value < 0 || value > 0 && value < grid.MinCost) throw new InvalidOperationException("navigation callback violates its declared cost minimum");
                if (value == 0) return 0;
                maximum = Math.Max(maximum, value);
            }
            return maximum;
        }

        private bool YieldingTraffic(Entity actor, Entity other)
            => actor != null && other.Id.Value > actor.Id.Value && other.Get<ITilePresence>()?.Movable == true &&
                other.Get<IMotion>() != null && MotionExecutionContracts.ReadStatus(_world, other) == MoveStatus.Moving &&
                MotionExecutionContracts.Read(_world, other).Blocker.IsNone;

        private bool MotionClearanceBlocked(ITileNavigationSpace map, Entity actor, TileCoord tile, bool waitingProbe)
        {
            // An adjacent segment extends the complete actual footprint by at most one cell per axis.
            // Its occupancy is therefore within this neighbourhood even for wide actors.
            for (int y = Math.Max(0, tile.Y - 1); y <= Math.Min(map.Height - 1, tile.Y + 1); y++)
                for (int x = Math.Max(0, tile.X - 1); x <= Math.Min(map.Width - 1, tile.X + 1); x++)
                {
                    Entity other = OccupantCore(map, new TileCoord(x, y));
                    if (other == null || ReferenceEquals(other, actor) || other.Get<ITileMotionClearance>() is not ITileMotionClearance motion) continue;
                    TileRect? clearance = EntityObservation.ReadProvider(_world, motion, other, () => {
                        if (!ReferenceEquals(motion.Entity, other)) throw new InvalidOperationException("motion clearance must belong to its actual occupant");
                        TileRect? value = motion.ReadMotionClearance();
                        if (value is TileRect area)
                        {
                            ITilePresence presence = other.Get<ITilePresence>();
                            TileRect footprint = presence.Footprint;
                            if (motion.Status != MoveStatus.Moving || !presence.Movable ||
                                area.W < footprint.W || (long)area.W > (long)footprint.W + 1 ||
                                area.H < footprint.H || (long)area.H > (long)footprint.H + 1 ||
                                !map.InBounds(new TileCoord(area.X, area.Y)) || !map.InBounds(new TileCoord(area.X + area.W - 1, area.Y + area.H - 1)) ||
                                !area.Contains(presence.Tile) || !area.Contains(new TileCoord(footprint.Right - 1, footprint.Bottom - 1)))
                                throw new InvalidOperationException("motion clearance requires an active in-bounds adjacent segment containing its actual mobile occupant");
                        }
                        return value;
                    }, map, observeTargetState: true);
                    if (clearance?.Contains(tile) == true && (!waitingProbe || !YieldingTraffic(actor, other))) return true;
                }
            return false;
        }

        internal NavGrid(World world, PathSearch search, string id, int minCost, Func<TileCoord, int> cost, ITileNavigationRule rule)
        {
            if (minCost < 1) throw new ArgumentOutOfRangeException(nameof(minCost), "the cheapest tile costs at least 1");
            _world = world;
            _search = search;
            Id = id;
            MinCost = minCost;
            _cost = cost;
            _rule = rule;
        }

        public string Id { get; }

        /// <summary>The cheapest a tile can cost: keeps the search heuristic admissible.</summary>
        public int MinCost { get; }

        public int Cost(TileCoord t)
        {
            var map = Map;
            if (!map.InBounds(t)) return 0;
            // Contract registrations already use this gateway's actual provider/space/clock scope.
            // Do not wrap a stateless forwarding closure in a second identical spatial scope.
            if (_rule != null) return TileNavigationContracts.ReadCost(_world, _rule, MinCost, t);
            return EntityObservation.ReadProvider(_world, _cost.Target ?? (object)_cost, null, () => {
                int value = _cost(t);
                if (value < 0 || value > 0 && value < MinCost)
                    throw new InvalidOperationException("navigation callback violates its declared cost minimum");
                return value;
            }, map);
        }

        public bool Passable(TileCoord t) => Cost(t) > 0;
        private static readonly Func<(World World, ITileNavigationOriginConstraint Constraint, TileCoord Tile), Code> ReadOrigin =
            context => context.Constraint.CheckNavigationOrigin(context.World, context.Tile);
        private bool OriginAllowed(Entity actor, TileCoord tile)
        {
            if (actor == null) return true;
            return OriginAllowed(actor.Def, actor, tile);
        }
        private bool OriginAllowed(EntityDef definition, Entity actor, TileCoord tile)
        {
            // Frozen component order is authoritative. Indexing avoids a boxed array
            // enumerator per searched tile; constraints still use the same guard.
            var parts = definition.Components;
            for (int i = 0; i < parts.Count; i++)
                if (parts[i] is ITileNavigationOriginConstraint constraint && !EntityObservation.ReadProvider(_world, constraint, actor,
                    (_world, constraint, tile), ReadOrigin).IsNone) return false;
            return true;
        }
        /// <summary>The same terrain/static-occupancy rule used by prospective structural search and travel weighting.</summary>
        internal int StructuralCostFor(Entity actor, TileCoord tile)
            => EntryCost(actor, tile, true, false);
        /// <summary>Classify an already blocked segment for waiting, NEVER permission to enter it.</summary>
        internal int WaitingTrafficCostFor(Entity actor, TileCoord tile)
            => EntryCost(actor, tile, false, true);
        /// <summary>Current structural feasibility, not future traffic vacancy or a physical movement permission.</summary>
        public PathSearchResult ProbeStructuralRoute(Entity actor, TileCoord from, TileRect goal, out TileCoord arrival,
            Func<TileCoord, TileCoord, bool> permitsStep, int maxExpanded = DefaultExpansionBudget, List<TileCoord> path = null)
        {
            var map = Map; TileCoord reached = default;
            arrival = default;
            path?.Clear();
            if (actor == null || !actor.Alive || !ReferenceEquals(_world.Entities.Get(actor.Id), actor) ||
                goal.X < 0 || goal.Y < 0 || goal.W <= 0 || goal.H <= 0 || (long)goal.X + goal.W > map.Width || (long)goal.Y + goal.H > map.Height)
                return PathSearchResult.InvalidRequest;
            PathSearchResult result = _search.Find(map, from, goal, tile => StructuralCostFor(actor, tile), MinCost, true, maxExpanded, path, permitsStep, tile => reached = tile);
            arrival = reached; return result;
        }

        /// <summary>Current entry cost for a moving entity, including blocking occupants.</summary>
        public int CostFor(Entity actor, TileCoord tile)
            => EntryCost(actor, tile, false, false);

        /// <summary>
        /// Searches only the reachable frontier, rather than allocating or scanning a width×height region
        /// table. The explicit expansion budget bounds work; false means no path was found within it.
        /// </summary>
        public PathSearchResult CanReach(TileCoord from, TileRect goal, int maxExpanded = DefaultExpansionBudget,
            Func<TileCoord, TileCoord, bool> permitsStep = null, Entity actor = null)
        {
            var map = Map;
            actor = actor ?? Occupant(null, from);
            return _search.Find(map, from, goal, tile => CostFor(actor, tile), MinCost, true, maxExpanded, null, permitsStep);
        }

        /// <summary>
        /// Octile A* from a tile to any tile of <paramref name="goal"/>. Fills <paramref name="path"/> with
        /// the tiles to step onto (excluding the start). Deterministic: equal total costs prefer greater
        /// progress, then lower row/column, rather than expanding a broad equal-cost plateau.
        /// </summary>
        public PathSearchResult FindPath(TileCoord from, TileRect goal, List<TileCoord> path, int maxExpanded = DefaultExpansionBudget,
            Func<TileCoord, TileCoord, bool> permitsStep = null, Entity actor = null)
        {
            path.Clear();
            if (!Map.InBounds(from)) return PathSearchResult.Unreachable;
            var map = Map;
            actor = actor ?? Occupant(null, from);
            return _search.Find(map, from, goal, tile => CostFor(actor, tile), MinCost, true, maxExpanded, path, permitsStep);
        }

        /// <summary>Same physical search for an unborn actual prototype, excluding the complete planned prefix.
        /// No temporary entity, saved route, reservation, map-sized allocation or second pathfinder.</summary>
        public PathSearchResult ProbeDefinitionRoute(EntityDef definition, TileCoord from, TileCoord exit,
            IReadOnlyList<TileRect> unavailable, int maxExpanded)
        {
            _world.RequireHealthyUpdate(); var map = Map;
            var presence = definition?.Component<ITilePresenceDefinition>();
            if (definition == null || !ReferenceEquals(_world.Content.Find<EntityDef>(definition.Id), definition) ||
                presence?.Space != map.Id || !presence.Movable || definition.Component<IMobilityDefinition>()?.MovementClass != Id || unavailable == null || maxExpanded < 1)
                return PathSearchResult.InvalidRequest;
            var forbidden = new TileRect[unavailable.Count];
            for (int i = 0; i < forbidden.Length; i++)
            {
                var area = unavailable[i];
                if (area.X < 0 || area.Y < 0 || area.W < 1 || area.H < 1 || (long)area.X + area.W > map.Width || (long)area.Y + area.H > map.Height)
                    return PathSearchResult.InvalidRequest;
                forbidden[i] = area;
            }
            return _search.Find(map, from, new TileRect(exit, 1, 1), Cost, MinCost, true, maxExpanded, null);

            int Cost(TileCoord tile)
            {
                if (tile.X < 0 || tile.Y < 0 || (long)tile.X + presence.Width > map.Width || (long)tile.Y + presence.Height > map.Height ||
                    !OriginAllowed(definition, null, tile)) return 0;
                var area = new TileRect(tile, presence.Width, presence.Height);
                foreach (var blocked in forbidden) if (area.Overlaps(blocked)) return 0;
                int cost = 0;
                for (int y = area.Y; y < area.Bottom; y++) for (int x = area.X; x < area.Right; x++)
                {
                    int current = EntryCost(null, new TileCoord(x, y), false, false);
                    if (current <= 0) return 0;
                    cost = Math.Max(cost, current);
                }
                return cost;
            }
        }

    }

    /// <summary>The navigation service: one grid per movement class the game defines.</summary>
    public sealed class NavService : ICompositionValidator
    {
        private readonly World _world;
        private readonly List<NavGrid> _grids = new List<NavGrid>();
        private bool _frozen;

        public NavService(World world) { _world = world; }

        /// <summary>The one router: movement classes and line planning both search through it.</summary>
        internal PathSearch Search { get; } = new PathSearch();

        /// <summary>
        /// Defines a movement class with its tile cost rule (10 = ordinary, 0 = impassable). The rule is
        /// evaluated against current session state; no cached full-world region table is maintained.
        /// </summary>
        public NavGrid Define(ITileNavigationRule rule)
        {
            int minimum = EntityObservation.ReadProvider(_world, rule, null, () => {
                TileNavigationContracts.RequireContext(rule, _world); return rule.MinCost;
            }, _world.Space);
            return Register(rule.Id, minimum, null, rule);
        }
        public NavGrid Define(string id, int minCost, Func<TileCoord, int> cost)
            => Register(id, minCost, cost ?? throw new ArgumentNullException(nameof(cost)), null);
        private NavGrid Register(string id, int minCost, Func<TileCoord, int> cost, ITileNavigationRule rule)
        {
            if (_frozen) throw new InvalidOperationException("movement classes are declared during composition");
            if (!(_world.Space is ITileNavigationSpace)) throw new ContentException("tile navigation requires a selected tile-space provider");
            if (Find(id) != null) throw new InvalidOperationException("movement class '" + id + "' defined twice");
            var g = new NavGrid(_world, Search, id, minCost, cost, rule);
            _grids.Add(g);
            return g;
        }

        public NavGrid Find(string id)
        {
            for (int i = 0; i < _grids.Count; i++) if (_grids[i].Id == id) return _grids[i];
            return null;
        }

        public NavGrid Grid(string id) => Find(id) ?? throw new InvalidOperationException("no movement class '" + id + "'");

        public void ValidateComposition(World world)
        {
            if (!ReferenceEquals(world, _world) || !(world.Space is ITileNavigationSpace))
                throw new ContentException("tile navigation belongs to its actual selected session and tile space");
            _frozen = true;
        }
    }

    /// <summary>
    /// The framework's one A*: octile steps over a per-tile entry cost (10 ordinary, 0 blocked), toward any
    /// tile of a goal rectangle. Scratch is reused across searches. Deterministic: ties break on tile index.
    /// </summary>
    internal sealed class PathSearch
    {
        // Navigation profiles share this one algorithm. A provider can synchronously query
        // another profile, so each active invocation must retain its own transient scratch.
        private sealed class Scratch
        {
            internal readonly Dictionary<TileCoord, long> Distance = new Dictionary<TileCoord, long>();
            internal readonly Dictionary<TileCoord, TileCoord> Parent = new Dictionary<TileCoord, TileCoord>();
            internal readonly HashSet<TileCoord> Closed = new HashSet<TileCoord>();
            // Pure costs are cached only within this invocation, never across routes/updates.
            internal readonly Dictionary<TileCoord, int> EntryCosts = new Dictionary<TileCoord, int>();
            internal readonly PathHeap Open = new PathHeap();
        }
        private readonly List<Scratch> _scratch = new List<Scratch> { new Scratch() };
        private int _depth;

        /// <summary>
        /// Fills <paramref name="path"/> with the tiles to step onto (excluding the start). <paramref name="minCost"/>
        /// keeps the heuristic admissible; with <paramref name="cornerRule"/> a diagonal may not cut a blocked corner.
        /// </summary>
        public PathSearchResult Find(ITileSpatialSpace map, TileCoord from, TileRect goal, Func<TileCoord, int> cost,
            int minCost, bool cornerRule, int maxExpanded, List<TileCoord> path, Func<TileCoord, TileCoord, bool> permitsStep = null,
            Action<TileCoord> reached = null)
        {
            if (_depth == _scratch.Count) _scratch.Add(new Scratch());
            Scratch scratch = _scratch[_depth++];
            try { return FindCore(scratch, map, from, goal, cost, minCost, cornerRule, maxExpanded, path, permitsStep, reached); }
            finally { _depth--; }
        }
        private PathSearchResult FindCore(Scratch scratch, ITileSpatialSpace map, TileCoord from, TileRect goal, Func<TileCoord, int> cost,
            int minCost, bool cornerRule, int maxExpanded, List<TileCoord> path, Func<TileCoord, TileCoord, bool> permitsStep,
            Action<TileCoord> reached)
        {
            path?.Clear();
            if (!map.InBounds(from)) return PathSearchResult.Unreachable;
            scratch.EntryCosts.Clear();
            if (EntryCost(from) <= 0) return PathSearchResult.Unreachable;
            if (maxExpanded < 1) return PathSearchResult.BudgetExceeded;
            // An exact impassable destination is already a complete refusal proof.
            // Use the same guarded, query-local cost read; never scan larger goal areas.
            if (goal.W == 1 && goal.H == 1 && map.InBounds(new TileCoord(goal.X, goal.Y)) &&
                EntryCost(new TileCoord(goal.X, goal.Y)) <= 0) return PathSearchResult.Unreachable;
            scratch.Distance.Clear();
            scratch.Parent.Clear();
            scratch.Closed.Clear();
            scratch.Open.Clear();
            scratch.Distance.Add(from, 0);
            scratch.Open.Push(new PathHeap.Entry(from, Heuristic(from, goal, minCost), 0));
            int expanded = 0;
            while (scratch.Open.Count > 0)
            {
                PathHeap.Entry entry = scratch.Open.Pop();
                TileCoord current = entry.Tile;
                if (!scratch.Distance.TryGetValue(current, out long currentDistance) || currentDistance != entry.Distance || !scratch.Closed.Add(current)) continue;
                if (goal.Contains(current))
                {
                    reached?.Invoke(current);
                    if (path != null)
                    {
                        for (TileCoord at = current; at != from; at = scratch.Parent[at]) path.Add(at);
                        path.Reverse();
                    }
                    return PathSearchResult.Found;
                }
                if (expanded >= maxExpanded) return PathSearchResult.BudgetExceeded;
                expanded++;
                for (int n = 0; n < 8; n++)
                {
                    var next = new TileCoord(current.X + TileSpatialGeometry.Neighbours[n].X, current.Y + TileSpatialGeometry.Neighbours[n].Y);
                    if (!map.InBounds(next) || scratch.Closed.Contains(next)) continue;
                    if (permitsStep != null && !permitsStep(current, next)) continue;
                    int c = EntryCost(next);
                    if (c <= 0) continue;
                    long stepCost = c;
                    if (n >= 4)
                    {
                        if (cornerRule && (EntryCost(new TileCoord(next.X, current.Y)) <= 0 || EntryCost(new TileCoord(current.X, next.Y)) <= 0)) continue;
                        stepCost = (long)c * 14 / 10;
                    }
                    long distance = checked(currentDistance + stepCost);
                    if (scratch.Distance.TryGetValue(next, out long known) && distance >= known) continue;
                    scratch.Distance[next] = distance;
                    scratch.Parent[next] = current;
                    scratch.Open.Push(new PathHeap.Entry(next, AddSaturated(distance, Heuristic(next, goal, minCost)), distance));
                }
            }
            return PathSearchResult.Unreachable;

            int EntryCost(TileCoord tile)
            {
                if (scratch.EntryCosts.TryGetValue(tile, out int value)) return value;
                value = cost(tile); // The original guarded callback, including typed refusal/fault.
                scratch.EntryCosts.Add(tile, value);
                return value;
            }
        }

        private static long AddSaturated(long a, long b) => a > long.MaxValue - b ? long.MaxValue : a + b;

        private static long Heuristic(TileCoord t, TileRect goal, int minCost)
        {
            int cx = t.X < goal.X ? goal.X : t.X >= goal.Right ? goal.Right - 1 : t.X;
            int cy = t.Y < goal.Y ? goal.Y : t.Y >= goal.Bottom ? goal.Bottom - 1 : t.Y;
            long dx = System.Math.Abs((long)t.X - cx), dy = System.Math.Abs((long)t.Y - cy);
            long diagonal = System.Math.Min(dx, dy), straight = System.Math.Max(dx, dy) - diagonal;
            // Search rounds EACH actual diagonal edge down. Rounding once after
            // summing distance can overestimate low authored costs and lose the
            // optimal route. Use the same per-edge lower bound, then sum it.
            long diagonalCost = (long)minCost * 14 / 10;
            long diagonalTotal = diagonal > long.MaxValue / diagonalCost ? long.MaxValue : diagonal * diagonalCost;
            long straightTotal = straight > long.MaxValue / minCost ? long.MaxValue : straight * minCost;
            return AddSaturated(diagonalTotal, straightTotal);
        }
    }

    /// <summary>A deterministic A* frontier; equal total costs prefer greater progress, then row/column.</summary>
    internal sealed class PathHeap
    {
        internal readonly struct Entry
        {
            public Entry(TileCoord tile, long priority, long distance) { Tile = tile; Priority = priority; Distance = distance; }
            public TileCoord Tile { get; }
            public long Priority { get; }
            public long Distance { get; }
        }
        private Entry[] _items = new Entry[256];
        public int Count { get; private set; }
        public void Clear() => Count = 0;

        public void Push(Entry v)
        {
            if (Count == _items.Length) Array.Resize(ref _items, _items.Length * 2);
            int i = Count++;
            _items[i] = v;
            while (i > 0)
            {
                int p = (i - 1) >> 1;
                if (Compare(_items[p], _items[i]) <= 0) break;
                Entry t = _items[p]; _items[p] = _items[i]; _items[i] = t;
                i = p;
            }
        }

        public Entry Pop()
        {
            Entry top = _items[0];
            _items[0] = _items[--Count];
            int i = 0;
            while (true)
            {
                int l = 2 * i + 1, r = l + 1, m = i;
                if (l < Count && Compare(_items[l], _items[m]) < 0) m = l;
                if (r < Count && Compare(_items[r], _items[m]) < 0) m = r;
                if (m == i) break;
                Entry t = _items[m]; _items[m] = _items[i]; _items[i] = t;
                i = m;
            }
            return top;
        }

        private static int Compare(Entry a, Entry b)
        {
            int c = a.Priority.CompareTo(b.Priority);
            if (c != 0) return c;
            // For equal f=g+h, greater g means smaller remaining h. This changes
            // only equal-cost exploration order, never optimality or the budget.
            c = b.Distance.CompareTo(a.Distance);
            if (c != 0) return c;
            c = a.Tile.Y.CompareTo(b.Tile.Y);
            return c != 0 ? c : a.Tile.X.CompareTo(b.Tile.X);
        }
    }
}
