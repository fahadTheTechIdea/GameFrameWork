using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // An engine-side proof of another spatial representation, not host/harness gameplay.
    public sealed class PointSpaceDef : Def, ISpatialSpaceDefinition
    {
        public Fix Extent { get; private set; }
        public bool InitialAccessOpen { get; private set; }
        public Fix RolloutOffset { get; private set; }
        public Fix InteractionReach { get; private set; }
        public Fix? NetworkStep { get; private set; }
        public Fix? NetworkReach { get; private set; }
        public string NetworkFault { get; private set; }
        public string NetworkEraseFault { get; private set; }
        public string SnapshotFault { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            Extent = reader.Fix("extent");
            InitialAccessOpen = reader.Bool("access_open");
            RolloutOffset = reader.Fix("rollout_offset");
            InteractionReach = reader.Fix("interaction_reach", Fix.Zero);
            SnapshotFault = reader.String("snapshot_fault", null);
            if (SnapshotFault != null && SnapshotFault != "null" && SnapshotFault != "foreign" && SnapshotFault != "blank" &&
                SnapshotFault != "negative" && SnapshotFault != "throw" && SnapshotFault != "mutate") throw reader.Error("unknown world capture probe");
            JsonValue graph=reader.Value("network_graph");
            if(graph!=null) {
                var authored=new JsonReader(graph,reader.Where+".network_graph");
                NetworkStep=authored.Fix("step");NetworkReach=authored.Fix("reach");NetworkFault=authored.String("fault",null);
                NetworkEraseFault=authored.String("erase_fault",null);
                if(NetworkEraseFault!=null&&NetworkEraseFault!="noop"&&NetworkEraseFault!="restore"&&NetworkEraseFault!="extra"&&NetworkEraseFault!="throw")
                    throw authored.Error("unknown physical erase probe");
                authored.End();
                if(NetworkFault!=null&&NetworkFault!="backward-only"&&NetworkFault!="duplicate-neighbour"&&NetworkFault!="self"&&
                    NetworkFault!="invalid-neighbour"&&NetworkFault!="duplicate-vertex"&&NetworkFault!="invalid-owner"&&NetworkFault!="throw"&&NetworkFault!="mutate-layer")
                    throw reader.Error("unknown authored graph fault");
                if(!NetworkStep.Value.IsPositive||NetworkStep.Value>Extent||NetworkReach.Value<Fix.Zero)
                    throw reader.Error("explicit point network step/reach invalid");
            }
            if (!Extent.IsPositive) throw reader.Error("point-space extent must be positive");
            if (!RolloutOffset.IsPositive || RolloutOffset > Extent) throw reader.Error("rollout offset must be positive and within the space extent");
            if (InteractionReach < Fix.Zero || InteractionReach > Extent) throw reader.Error("interaction reach must be nonnegative and within the space extent");
        }
        public void Compose(WorldComposer composer)
        {
            var space = new PointSpace(this);
            foreach (ISpatialLayerDefinition layer in composer.Content.All<ISpatialLayerDefinition>())
                if (ReferenceEquals(layer.Space, this)) space.RegisterLayer(layer);
            composer.Services.Bind<ISpatialSpace>(space);
            composer.Services.Bind<ILineGeometry>(space);
            composer.Services.Bind<ILineRemovalGeometry>(space);
            composer.Services.Bind<INetworkGraphGeometry>(space);
            SpatialRegionChangeView<PointRegion>.Compose(composer, space, "point-changes." + Id);
            composer.AddViewFeature("points.layers." + Id, 4, (world, viewer) => new PointLayerView(space, viewer));
        }
    }

    public sealed class PointPresenceDef : ComponentDef<PointPresence>, ISpatialPresenceDefinition
    {
        private string _space;
        public PointSpaceDef SpaceDefinition { get; private set; }
        public string Space => SpaceDefinition.Id;
        public Fix InitialPosition { get; private set; }
        public string SnapshotFault { get; private set; }
        public Fix SnapshotFaultAt { get; private set; }
        public string ComparisonFault { get; private set; }
        public Fix ComparisonFaultAt { get; private set; }
        public void ValidateComposition(World world, EntityDef entity)
        {
            if (!ReferenceEquals(entity.Component<ISpatialPresenceDefinition>(), this) ||
                !(world.Space is PointSpace point) || point.Id != Space)
                throw new ContentException(entity.Id + ": point presence requires its actual selected point-space provider");
        }
        protected override void ReadCore(JsonReader reader)
        {
            _space = reader.String("space"); InitialPosition = reader.Fix("position");SnapshotFault=reader.String("snapshot_fault",null);
            ComparisonFault=reader.String("comparison_fault",null);
            if(ComparisonFault!=null) {
                if(ComparisonFault!="normal"&&ComparisonFault!="nonreflexive"&&ComparisonFault!="asymmetric"&&
                    ComparisonFault!="stateful"&&ComparisonFault!="throw"&&ComparisonFault!="mutate"&&ComparisonFault!="shape-transition")
                    throw reader.Error("unknown snapshot comparison probe");
                if(reader.Value("comparison_fault_at")?.Kind!=JsonKind.Number)throw reader.Error("comparison probe position must be explicitly authored");
                ComparisonFaultAt=reader.Fix("comparison_fault_at");
            }
            if(SnapshotFault!=null) {
                if(SnapshotFault!="null"&&SnapshotFault!="foreign"&&SnapshotFault!="blank"&&SnapshotFault!="throw"&&SnapshotFault!="mutate")
                    throw reader.Error("unknown entity capture probe");
                if(reader.Value("snapshot_fault_at")?.Kind!=JsonKind.Number)throw reader.Error("capture probe position must be explicitly authored");
                SnapshotFaultAt=reader.Fix("snapshot_fault_at");
            }
        }
        protected override void ResolveCore(ContentResolver content)
        {
            SpaceDefinition = content.Def<PointSpaceDef>(_space);
            if (InitialPosition < Fix.Zero || InitialPosition > SpaceDefinition.Extent)
                throw content.Fail("point position is outside its authored space");
            if(SnapshotFault!=null&&(SnapshotFaultAt<Fix.Zero||SnapshotFaultAt>SpaceDefinition.Extent))
                throw content.Fail("capture probe position is outside its authored space");
            if(ComparisonFault!=null&&(ComparisonFaultAt<Fix.Zero||ComparisonFaultAt>SpaceDefinition.Extent))
                throw content.Fail("comparison probe position is outside its authored space");
        }
    }

    public class PointSnapshot : SpatialSnapshot
    {
        public PointSnapshot(string space, Fix position, Fix previous) : base(space)
        { Position = position; Previous = previous; }
        public Fix Position { get; }
        public Fix Previous { get; }
        public override bool Matches(SpatialSnapshot other)
            => other is PointSnapshot value && Space == value.Space && Position == value.Position && Previous == value.Previous;
    }
    public sealed class PointPlacement : SpatialPlacement
    {
        public PointPlacement(string space, Fix position) : base(space) { Position = position; }
        public Fix Position { get; }
    }
    // Negative-only detached comparison instrumentation; existing point values remain the facts.
    public sealed class PointComparisonSnapshot : PointSnapshot
    {
        private readonly PointPresence _source;
        private readonly string _mode;
        private readonly Fix _at;
        private bool _crossRead;
        public PointSnapshot Value { get; }
        public PointComparisonSnapshot(PointSnapshot value,PointPresence source,string mode,Fix at):base(value.Space,value.Position,value.Previous)
        { Value=value;_source=source;_mode=mode;_at=at; }
        public override bool Matches(SpatialSnapshot other) {
            if(!(other is PointComparisonSnapshot right))throw new InvalidOperationException("cross-shape comparison must not invoke callback");
            if(Value.Position>=_at) {
                if(_mode=="nonreflexive")return false;
                if(_mode=="throw")throw new InvalidOperationException("proof.original-snapshot-comparison-error");
                if(_mode=="mutate")_source.Entity.Touch();
                if(ReferenceEquals(this,other))return true;
                if(_mode=="asymmetric")return Value.Position>right.Value.Position;
                if(_mode=="stateful"){bool previous=_crossRead;_crossRead=!_crossRead;return previous;}
            }
            return Value.Matches(right.Value);
        }
    }
    public sealed class PointOtherComparisonSnapshot : PointSnapshot
    {
        public PointSnapshot Value { get; }
        public PointOtherComparisonSnapshot(PointSnapshot value):base(value.Space,value.Position,value.Previous){Value=value;}
        public override bool Matches(SpatialSnapshot other) => other is PointOtherComparisonSnapshot right
            ? Value.Matches(right.Value) : throw new InvalidOperationException("cross-shape comparison must not invoke callback");
    }
    public sealed class PointWorldSnapshot : SpatialWorldSnapshot
    {
        public PointWorldSnapshot(string space, int version, Fix extent, bool open) : base(space, version)
        { Extent = extent; AccessOpen = open; }
        public Fix Extent { get; }
        public bool AccessOpen { get; }
    }

    public sealed partial class PointPresence : Component<PointPresenceDef>, ISpatialPresence
    {
        private Fix _position, _previous;
        private bool _configured;
        public string Space => Def.Space;
        public Fix Position => _position;
        public override void OnCreate() { if (!_configured) Place(Def.InitialPosition); }
        public void Place(Fix position)
        {
            if (_configured) throw new InvalidOperationException("point presence is already configured");
            if (position < Fix.Zero || position > Def.SpaceDefinition.Extent) throw new ArgumentOutOfRangeException(nameof(position));
            _position = _previous = position; _configured = true;
        }
        public void BeginTick() => _previous = _position;
        public SpatialSnapshot Capture() {
            if (ProbeVisibilityCapture(out var observed)) return observed;
            if(Def.SnapshotFault!=null&&_position>=Def.SnapshotFaultAt) {
                if(Def.SnapshotFault=="null")return null;
                if(Def.SnapshotFault=="foreign")return new PointSnapshot("foreign.space",_position,_previous);
                if(Def.SnapshotFault=="blank")return new PointSnapshot("",_position,_previous);
                if(Def.SnapshotFault=="throw")throw new InvalidOperationException("proof.original-entity-snapshot-error");
                if(Def.SnapshotFault=="mutate")Entity.Touch();
            }
            var value=new PointSnapshot(Space,_position,_previous);
            if(Def.ComparisonFault=="shape-transition"&&_position>=Def.ComparisonFaultAt)return new PointOtherComparisonSnapshot(value);
            return Def.ComparisonFault==null ? value : new PointComparisonSnapshot(value,this,Def.ComparisonFault,Def.ComparisonFaultAt);
        }
        public void Move(World world, Fix position)
        {
            if (!(world.Space is PointSpace space) || space.Id != Space || !ReferenceEquals(world.Entities.Get(Entity.Id), Entity))
                throw new InvalidOperationException("point motion requires its selected live space");
            if (position < Fix.Zero || position > Def.SpaceDefinition.Extent) throw new ArgumentOutOfRangeException(nameof(position));
            if (_position != position) { _position = position; Entity.Touch(); }
        }
        public override void Sync(IStateSync sync)
        {
            sync.Fix(ref _position); sync.Fix(ref _previous);
            if (_position < Fix.Zero || _position > Def.SpaceDefinition.Extent || _previous < Fix.Zero || _previous > Def.SpaceDefinition.Extent)
                throw new CorruptStateException("point presence is outside its space");
        }
    }

    public sealed class PointMotionDef : ComponentDef<PointMotion>
    {
        public Fix UnitsPerHour { get; private set; }
        public bool IgnoreStop { get; private set; }
        public string EstimateFault { get; private set; }
        public string AreaFault { get; private set; }
        public EntityStateBindings States { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            UnitsPerHour = reader.Fix("rate");
            IgnoreStop = reader.Bool("ignore_stop", false);
            EstimateFault = reader.String("estimate_fault", null);
            AreaFault = reader.String("area_fault", null);
            if (!UnitsPerHour.IsPositive) throw reader.Error("motion rate must be positive");
            States = EntityStateBindings.Read(reader, "moving", "stopped");
        }
        protected override void ResolveCore(ContentResolver content) => States = States.Resolve(content);
        protected override void ValidateCore(EntityDef owner)
        {
            if (owner.Component<PointPresenceDef>() == null) throw new ContentException(owner.Id + ": point motion requires point presence");
        }
    }

    public sealed class PointMotion : Component<PointMotionDef>, IMotionEstimate, IMotionAreaConstraint, IEntityStateSource
    {
        private MoveStatus _status;
        private Fix _goal;
        private long _ticksMoving;
        public MoveStatus Status => _status;
        public Fix Goal => _goal;
        public long TicksMoving => _ticksMoving;
        public void GoTo(Fix goal)
        {
            if (goal < Fix.Zero || goal > Entity.Get<PointPresence>().Def.SpaceDefinition.Extent)
                throw new ArgumentOutOfRangeException(nameof(goal));
            if (_status == MoveStatus.Moving && _goal == goal) return;
            _ = checked(Entity.Version + 1);
            _goal = goal; _status = MoveStatus.Moving; _ticksMoving = 0;
            Entity.Touch();
        }
        public void Stop() { if (!Def.IgnoreStop && _status != MoveStatus.Idle) { _ = checked(Entity.Version + 1); _status = MoveStatus.Idle; Entity.Touch(); } }
        public Code CheckArea(World world, ISpatialRegion region)
        {
            if (world == null || region == null || !Entity.Alive || !ReferenceEquals(world.Entities.Get(Entity.Id), Entity) ||
                world.Space?.Id != Entity.Get<PointPresence>().Space || _status != MoveStatus.Moving)
                throw new ArgumentException("point route-area admission needs actual active session motion");
            if (Def.AreaFault == "touch") Entity.Touch();
            if (Def.AreaFault == "status") _status = MoveStatus.Idle;
            if (Def.AreaFault == "invalid") return new Code("proof.invalid-area-result");
            PointPresence presence = Entity.Get<PointPresence>();
            var result = MotionAreaContracts.CheckTravel(region, presence.Capture(), new PointSnapshot(presence.Space, _goal, _goal));
            return result == SpatialRegionStatus.Inside ? Code.None : result == SpatialRegionStatus.Outside ? MotionAreaCodes.Outside : MotionAreaCodes.Unsupported;
        }
        public MotionRouteCheck CheckRoute(World world, ISpatialRegion region, SpatialSnapshot origin, MotionDestination destination, Fix speedModifier)
        {
            if (world == null || speedModifier < Fix.Zero || !Entity.Alive || !ReferenceEquals(world.Entities.Get(Entity.Id), Entity) ||
                world.Space?.Id != Entity.Get<PointPresence>().Space) throw new ArgumentException("prospective route needs actual point session motion");
            if (Def.AreaFault == "route-touch" || Def.AreaFault == "route-touch-in-area" && region != null) Entity.Touch();
            if (Def.AreaFault == "route-status") _status = _status == MoveStatus.Moving ? MoveStatus.Idle : MoveStatus.Moving;
            if (Def.AreaFault == "route-null") return null;
            if (Def.AreaFault == "route-reason") return new MotionRouteCheck(new Code("proof.route-reason"), null, null);
            if (Def.AreaFault == "route-space") return new MotionRouteCheck(Code.None, new PointSnapshot("foreign", Fix.Zero, Fix.Zero), new MotionTravelEstimate(1, Code.None));
            if (Def.AreaFault == "route-empty") return new MotionRouteCheck(Code.None, null, new MotionTravelEstimate(1, Code.None));
            if (Def.AreaFault == "route-outside") return new MotionRouteCheck(Code.None, new PointSnapshot(world.Space.Id, Fix.FromInt(90), Fix.FromInt(90)), new MotionTravelEstimate(1, Code.None));
            if (origin is not PointSnapshot from || destination?.Geometry is not PointSnapshot to || from.Space != world.Space.Id || to.Space != world.Space.Id)
                return new MotionRouteCheck(MotionAreaCodes.Unsupported, null, null);
            // Test-only callback witnesses: selected/needed routes still use the ordinary
            // point implementation. No route, claim, stock or elapsed-time mirror.
            if (Def.AreaFault == "departure-far" && to.Position == Fix.FromInt(17) ||
                Def.AreaFault == "departure-tie" && to.Position == Fix.FromInt(9))
                throw new InvalidOperationException("proof.dominated-departure-was-evaluated");
            if (Def.AreaFault == "departure-blocked" && to.Position == Fix.FromInt(13))
                return new MotionRouteCheck(MotionAreaCodes.Outside, null, null);
            if (Def.AreaFault == "departure-all-blocked" || Def.AreaFault == "departure-pending")
            {
                if (to.Position == Fix.FromInt(13))
                    return new MotionRouteCheck(Def.AreaFault == "departure-pending" ? MotionAreaCodes.Pending : MotionAreaCodes.Unsupported, null, null);
                if (to.Position == Fix.FromInt(17)) return new MotionRouteCheck(MotionAreaCodes.Outside, null, null);
            }
            if (Def.AreaFault == "service-far" && to.Position == Fix.FromInt(14) ||
                Def.AreaFault == "service-tie" && to.Position == Fix.FromInt(9))
                throw new InvalidOperationException("proof.losing-service-was-evaluated");
            if (to.Position == Fix.FromInt(11) && Def.AreaFault == "service-pending")
                return new MotionRouteCheck(MotionAreaCodes.Pending, null, null);
            if (Def.AreaFault == "service-order" && (to.Position == Fix.FromInt(11) || to.Position == Fix.FromInt(14)))
                return new MotionRouteCheck(to.Position == Fix.FromInt(11) ? MotionAreaCodes.Unsupported : MotionAreaCodes.Outside, null, null);
            if (to.Position == Fix.FromInt(11) && (Def.AreaFault == "service-blocked" || Def.AreaFault == "service-all-blocked") ||
                to.Position == Fix.FromInt(14) && (Def.AreaFault == "service-all-blocked" || Def.AreaFault == "service-pending"))
                return new MotionRouteCheck(MotionAreaCodes.Outside, null, null);
            var result = region == null ? SpatialRegionStatus.Inside : MotionAreaContracts.CheckTravel(region, from, to);
            if (result != SpatialRegionStatus.Inside) return new MotionRouteCheck(result == SpatialRegionStatus.Unsupported ? MotionAreaCodes.Unsupported : MotionAreaCodes.Outside, null, null);
            if (!(world.Space as PointSpace).AccessOpen && from.Position != to.Position) return new MotionRouteCheck(MotionAreaCodes.Outside, null, null);
            MotionTravelEstimate travel;
            try { travel = from.Position == to.Position ? new MotionTravelEstimate(0, Code.None) : MotionArrivalContracts.FromBudget(Fix.Abs(to.Position - from.Position).Raw, TickBudget(world, speedModifier)); }
            catch (OverflowException) { travel = new MotionTravelEstimate(null, MotionArrivalCodes.Overflow); }
            if (Def.AreaFault == "route-duration-negative") travel = new MotionTravelEstimate(-1, Code.None);
            if (Def.AreaFault == "route-duration-empty") travel = new MotionTravelEstimate(null, Code.None);
            if (Def.AreaFault == "route-duration-both") travel = new MotionTravelEstimate(1, MotionArrivalCodes.Unsupported);
            if (Def.AreaFault == "route-duration-unsupported") travel = new MotionTravelEstimate(null, MotionArrivalCodes.Unsupported);
            return new MotionRouteCheck(Code.None, new PointSnapshot(to.Space, to.Position, to.Position), travel);
        }
        private Fix TickBudget(World world, Fix speedModifier) => world.Time.RateFor(Def.UnitsPerHour * speedModifier, DurationUnit.Hour);
        public MotionTravelEstimate EstimateRemaining(World world, Fix speedModifier)
        {
            if (Def.EstimateFault == "touch") Entity.Touch();
            if (Def.EstimateFault == "status") _status = MoveStatus.Idle;
            if (Def.EstimateFault == "empty") return new MotionTravelEstimate(null, Code.None);
            if (Def.EstimateFault == "negative") return new MotionTravelEstimate(-1, Code.None);
            if (Def.EstimateFault == "zero") return new MotionTravelEstimate(0, Code.None);
            if (Def.EstimateFault == "both") return new MotionTravelEstimate(1, MotionArrivalCodes.RoutePending);
            if (Def.EstimateFault == "overflow") return new MotionTravelEstimate(long.MaxValue, Code.None);
            if (!(world.Space as PointSpace).AccessOpen) return new MotionTravelEstimate(null, MotionArrivalCodes.Unreachable);
            Fix remaining = Fix.Abs(_goal - Entity.Get<PointPresence>().Position);
            return MotionArrivalContracts.FromBudget(remaining.Raw, TickBudget(world, speedModifier));
        }
        public void Advance(World world, TickInfo tick, Fix speedModifier)
        {
            if (_status != MoveStatus.Moving) return;
            var region = WorkPolicyRules.MotionRegion(world, Entity);
            if (region != null && !CheckArea(world, region).IsNone) return;
            PointSpace space = world.Space as PointSpace ?? throw new InvalidOperationException("point space is required");
            if (!space.AccessOpen) { _status = MoveStatus.Unreachable; return; }
            _ticksMoving = checked(_ticksMoving + 1);
            PointPresence presence = Entity.Get<PointPresence>();
            Fix distance = TickBudget(world, speedModifier);
            Fix remaining = _goal - presence.Position;
            Fix next = Fix.Abs(remaining) <= distance ? _goal : presence.Position + (remaining > Fix.Zero ? distance : -distance);
            // Actual point-space occupancy governs every movement, including ordinary return-home
            // motion. Do not cross or overlap another physical machine between update positions.
            if (!space.CheckMobileVacancy(world, Entity, presence.Position, next).IsNone) return;
            presence.Move(world, next);
            if (next == _goal) _status = MoveStatus.Arrived;
        }
        public IEntityStateDefinition GetState(EntityUpdateContext context)
            => _status == MoveStatus.Moving ? Def.States["moving"] : _status == MoveStatus.Unreachable ? Def.States["stopped"] : null;
        public override void Sync(IStateSync sync)
        {
            sync.Enum(ref _status); sync.Fix(ref _goal); sync.Long(ref _ticksMoving);
            PointSpaceDef space = Entity.Get<PointPresence>().Def.SpaceDefinition;
            if (_status > MoveStatus.Unreachable || _goal < Fix.Zero || _goal > space.Extent || _ticksMoving < 0)
                throw new CorruptStateException("invalid point-motion state");
        }
    }

    // Public-contract proof that motion does not require a forecast capability or coordinates.
    public sealed class UnestimatedMotionDef : ComponentDef<UnestimatedMotion>
    {
        public long Ticks { get; private set; }
        protected override void ReadCore(JsonReader reader)
        { Ticks = reader.Long("ticks"); if (Ticks <= 0) throw reader.Error("motion duration must be positive"); }
    }
    public sealed class UnestimatedMotion : Component<UnestimatedMotionDef>, IMotion
    {
        private long _remaining;
        private MoveStatus _status;
        public MoveStatus Status => _status;
        public override void OnCreate() { _remaining = Def.Ticks; _status = MoveStatus.Moving; }
        public void Stop() => _status = MoveStatus.Idle;
        public void Advance(World world, TickInfo tick, Fix speedModifier)
        { if (_status == MoveStatus.Moving && --_remaining == 0) _status = MoveStatus.Arrived; }
        public override void Sync(IStateSync sync)
        { sync.Long(ref _remaining); sync.Enum(ref _status); if (_remaining < 0 || _remaining > Def.Ticks || _status > MoveStatus.Unreachable) throw new CorruptStateException("invalid proof motion"); }
    }

    public sealed partial class PointSpace : ISpatialOverlap, ISpatialInteraction, ISpatialRollout, ISpatialLayerProvider, ISpatialVisibilityInvalidation, ISpatialRegionChangeSource<PointRegion>
    {
        private readonly PointSpaceDef _definition;
        private bool _accessOpen;
        public PointSpace(PointSpaceDef definition)
        { _definition = definition; _accessOpen = definition.InitialAccessOpen; }
        public string Id => _definition.Id;
        public int StateSchemaVersion => 5;
        public int ObservationVersion { get { ObserveRevision?.Invoke(); return _observerVersion; } }
        public bool AccessOpen => _accessOpen;
        public Code CheckMobileVacancy(World world, Entity carrier, Fix from, Fix to)
        {
            if (!ReferenceEquals(world?.Space, this) || carrier == null || !ReferenceEquals(world.Entities.Get(carrier.Id), carrier) ||
                carrier.Get<PointPresence>()?.Space != Id) return WorkDockCodes.Incompatible;
            return MobileVacancy(world, carrier, from, to);
        }
        private Code MobileVacancy(World world, Entity carrier, Fix from, Fix to)
        {
            foreach (Entity occupant in world.Entities.All)
                if (occupant.Alive && occupant != carrier && (occupant.Has<IMotion>() || occupant.Has<ITransporter>() || occupant.Has<IWorker>()) &&
                    occupant.Get<PointPresence>() is PointPresence presence && presence.Space == Id)
                {
                    Fix point = presence.Position;
                    bool occupied = from == to ? point == to : to > from ? point > from && point <= to : point < from && point >= to;
                    if (occupied) return WorkDockCodes.Occupied;
                }
            return Code.None;
        }
        // Fault instrumentation only; not authored gameplay or another state owner.
        public Action ObserveSync, ObserveRevision;
        public bool MutatePlacementSource, MutateRolloutSource;
        public Action<string, Entity> GeometryObservationFault;
        // Fault probes only, never a second gameplay policy or persistence owner.
        public SpatialOverlapStatus? OverlapFault;
        public Entity MutateOverlapParticipant;
        public bool MutateOnlyCoincidentOverlap;
        public SpatialOverlapStatus CheckOverlap(World world, SpatialSnapshot left, SpatialSnapshot right)
        {
            if (!MutateOnlyCoincidentOverlap || !ReferenceEquals(left, right) && left is PointSnapshot from && right is PointSnapshot to && from.Position == to.Position)
                MutateOverlapParticipant?.Touch();
            if (OverlapFault.HasValue) return OverlapFault.Value;
            if (!ReferenceEquals(world?.Space, this) || !(left is PointSnapshot a) || !(right is PointSnapshot b) || a.Space != Id || b.Space != Id)
                return SpatialOverlapStatus.Unsupported;
            return a.Position == b.Position ? SpatialOverlapStatus.Overlapping : SpatialOverlapStatus.Disjoint;
        }
        public void ValidateComposition(World world)
        {
            foreach (EntityDef entity in world.Content.All<EntityDef>())
            {
                ISpatialPresenceDefinition presence = entity.Component<ISpatialPresenceDefinition>();
                if (presence != null && presence.Space == Id && !(presence is PointPresenceDef))
                    throw new ContentException(entity.Id + ": incompatible point-space presence");
            }
            _layersFrozen = true;
            _world = world;
        }
        public void OnSpawn(Entity entity)
        {
            GeometryObservationFault?.Invoke("spawn", entity);
            ISpatialPresence presence = entity.Get<ISpatialPresence>();
            if (presence != null && (!(presence is PointPresence) || presence.Space != Id))
                throw new ContentException("point spawn uses the wrong selected space");
        }
        public void OnDespawn(Entity entity) { GeometryObservationFault?.Invoke("despawn", entity); }
        public void OnLoaded(World world) { foreach (Entity entity in world.Entities.All) OnSpawn(entity); ResetSpatialVersions(); }
        public SpatialWorldSnapshot Capture(PlayerId viewer) {
            if(!_accessOpen&&_definition.SnapshotFault!=null) {
                if(_definition.SnapshotFault=="null")return null;
                if(_definition.SnapshotFault=="foreign")return new PointWorldSnapshot("foreign.space",VersionFor(viewer),_definition.Extent,_accessOpen);
                if(_definition.SnapshotFault=="blank")return new PointWorldSnapshot("",VersionFor(viewer),_definition.Extent,_accessOpen);
                if(_definition.SnapshotFault=="negative")return new InvalidHeaderSnapshot(Id,-1);
                if(_definition.SnapshotFault=="throw")throw new InvalidOperationException("proof.original-world-snapshot-error");
                if(_definition.SnapshotFault=="mutate")SetAccess(true);
            }
            return new PointWorldSnapshot(Id,VersionFor(viewer),_definition.Extent,_accessOpen);
        }
        private sealed class InvalidHeaderSnapshot : SpatialWorldSnapshot
        { internal InvalidHeaderSnapshot(string space,int version):base(space,version){} }
        public void Sync(IStateSync sync) { ObserveSync?.Invoke(); sync.Bool(ref _accessOpen); SyncLayers(sync); }
        public SpatialPlacement CapturePlacement(Entity source)
        {
            PlacementCaptureFault?.Invoke();
            return Presence(source, out PointPresence presence) ? new PointPlacement(Id, presence.Position) : null;
        }
        public Code CheckPlacement(World world, EntityDef target, SpatialPlacement placement, Entity replacing)
        {
            GeometryObservationFault?.Invoke("placement", replacing);
            if (MutatePlacementSource && world.Entities.All.Count > 0) world.Entities.All[0].Touch();
            if (!ReferenceEquals(world.Space, this) || !ReferenceEquals(world.Content.Find<EntityDef>(target.Id), target)
                || !(placement is PointPlacement point) || point.Space != Id) return Codes.InvalidArgument;
            if (replacing != null && !ReferenceEquals(world.Entities.Get(replacing.Id), replacing)) return Codes.NotFound;
            if (target.Component<PointPresenceDef>()?.Space != Id) return Codes.NotAllowed;
            if (point.Position < Fix.Zero || point.Position > _definition.Extent) return Codes.OutOfBounds;
            if (target.Kind == EntityKind.Structure)
                foreach (Entity occupant in world.Entities.All)
                    if (occupant.Alive && !ReferenceEquals(occupant, replacing) && occupant.Def.Kind == EntityKind.Structure &&
                        occupant.Get<PointPresence>() is PointPresence presence && presence.Space == Id && presence.Position == point.Position)
                        return Codes.Blocked;
            return Code.None;
        }
        public void ConfigurePlacement(Entity entity, SpatialPlacement placement)
        {
            GeometryObservationFault?.Invoke("configure", entity);
            if (!(placement is PointPlacement point) || point.Space != Id) throw new ArgumentException("incompatible point placement");
            entity.Get<PointPresence>().Place(point.Position);
        }
        public Code CheckRolloutDefinition(World world, EntityDef transformer, EntityDef output)
        {
            GeometryObservationFault?.Invoke("rollout-definition", null);
            if (!ReferenceEquals(world.Space, this) || transformer.Component<PointPresenceDef>()?.Space != Id || output.Component<PointPresenceDef>()?.Space != Id)
                return EntityRollout.Unsupported;
            if (!output.HasCapability<IMotion>()) return Code.None;
            WorkDockRules.ValidateRollouts(world.Content, transformer);
            if (transformer.Component<IPointDockDefinition>() != null)
                foreach (var lane in WorkDockRules.RolloutsFor(transformer, output)) return Code.None;
            return EntityRollout.Unsupported;
        }
        public Code FindRollout(World world, EntityRolloutRequest request, out SpatialPlacement placement)
        {
            GeometryObservationFault?.Invoke("rollout", request.Source);
            if (MutateRolloutSource) request.Source?.Touch();
            placement = null;
            Code refusal = CheckRolloutDefinition(world, request.Producer, request.Output);
            if (!refusal.IsNone) return refusal;
            if (!_accessOpen) return Codes.Blocked;
            if (!(request.Origin is PointPlacement originPlacement) || originPlacement.Space != Id) return Codes.InvalidArgument;
            Fix origin = originPlacement.Position;
            if (request.Output.HasCapability<IMotion>())
            {
                var layout = request.Producer.Component<IPointDockDefinition>();
                foreach (var lane in WorkDockRules.RolloutsFor(request.Producer, request.Output)) foreach (Code start in lane.Starts)
                {
                    var candidate = new PointPlacement(Id, origin + layout.Offset(start));
                    refusal = CheckRolloutPlacement(world, request, candidate);
                    if (refusal.IsNone) { placement = candidate; return Code.None; }
                }
                return Codes.Blocked;
            }
            if (_definition.Extent - origin < _definition.RolloutOffset) return Codes.OutOfBounds;
            for (Fix position = origin + _definition.RolloutOffset; position <= _definition.Extent;)
            {
                var candidate = new PointPlacement(Id, position);
                refusal = CheckRolloutPlacement(world, request, candidate);
                if (refusal.IsNone) { placement = candidate; return Code.None; }
                if (refusal != Codes.Blocked) return refusal;
                if (_definition.Extent - position < _definition.RolloutOffset) break;
                position += _definition.RolloutOffset;
            }
            return Codes.Blocked;
        }
        public Code CheckRolloutPlacement(World world, EntityRolloutRequest request, SpatialPlacement placement)
        {
            Code refusal = CheckPlacement(world, request.Output, placement, null);
            if (!refusal.IsNone) return refusal;
            var point = (PointPlacement)placement;
            foreach (var occupied in request.Unavailable)
            {
                if (!(occupied.Placement is PointPlacement other) || other.Space != Id) return Codes.InvalidArgument;
                if (other.Position == point.Position) return Codes.Blocked;
            }
            return request.Output.HasCapability<IMotion>() ? CheckMobileRollout(world, request, point) : Code.None;
        }
        private bool Presence(Entity entity, out PointPresence presence)
        { presence = entity.Get<PointPresence>(); return presence != null && presence.Space == Id; }
        public bool TryEstimateCost(World world, Entity actor, Entity target, out long cost)
        {
            cost = 0;
            if (!Presence(actor, out PointPresence from) || !Presence(target, out PointPresence to)) return false;
            cost = Fix.Abs(to.Position - from.Position).Raw;
            return true;
        }
        // Test-only callback observation hooks, never gameplay state or a replacement algorithm.
        public Action BeforeProximity;
        public Action BeforeReachability;
        public SpatialProximityStatus CheckProximity(World world, Entity actor, Entity target)
        {
            BeforeProximity?.Invoke();
            if (!Presence(actor, out PointPresence from) || !Presence(target, out PointPresence to)) return SpatialProximityStatus.Unsupported;
            return Fix.Abs(from.Position - to.Position) <= _definition.InteractionReach ? SpatialProximityStatus.WithinReach : SpatialProximityStatus.OutsideReach;
        }
        public SpatialInteractionStatus Check(World world, Entity actor, Entity target)
        {
            BeforeReachability?.Invoke();
            var proximity = CheckProximity(world, actor, target);
            if (proximity == SpatialProximityStatus.Unsupported) return SpatialInteractionStatus.Unsupported;
            if (proximity == SpatialProximityStatus.WithinReach) return SpatialInteractionStatus.WithinReach;
            return _accessOpen && actor.Has<PointMotion>() ? SpatialInteractionStatus.Reachable : SpatialInteractionStatus.Unreachable;
        }
        public SpatialInteractionStatus Approach(World world, Entity actor, Entity target)
        {
            SpatialInteractionStatus status = Check(world, actor, target);
            if (status == SpatialInteractionStatus.Reachable)
            {
                Fix from = actor.Get<PointPresence>().Position, to = target.Get<PointPresence>().Position;
                actor.Get<PointMotion>().GoTo(from < to ? to - _definition.InteractionReach : to + _definition.InteractionReach);
            }
            return status;
        }
    }

    public sealed class SetPointAccessCommand : Command
    {
        public bool Open;
        public override void Sync(IStateSync sync) => sync.Bool(ref Open);
    }
    public sealed class SetPointAccessHandler : CommandHandler<SetPointAccessCommand>
    {
        protected override Code Check(World world, SetPointAccessCommand command)
            => world.Space is PointSpace ? Code.None : Codes.InvalidArgument;
        protected override void Apply(World world, SetPointAccessCommand command, CommandContext context)
            => ((PointSpace)world.Space).SetAccess(command.Open);
    }
    public sealed class PointModule : GameModule
    {
        public override string Id => "points";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(WorkModule.ModuleId, new ModuleVersion(1, 0, 0)), new ModuleRequirement(TransformationModule.ModuleId, new ModuleVersion(1, 0, 0)), new ModuleRequirement(LogisticsModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes content)
        {
            content.Section<PointSpaceDef>("point_spaces");
            content.Section<PointIntervalRegionDef>("point_interval_regions");
            content.Section<PointStationsRegionDef>("point_station_regions");
            content.Component<PointPresenceDef>("point_presence", 1);
            content.Component<PointMotionDef>("point_motion", 2);
            content.Component<UnestimatedMotionDef>("unestimated_motion", 1);
            content.Component<ContractWorkerDef>("contract_worker", 1);
            content.Component<UndeclaredWorkerDef>("undeclared_worker", 1);
            content.Component<NonWorkerSpecificationDef>("non_worker_specification", 1);
            content.Component<SingleResourcePortDef>("point_stock", 4);
            content.Component<ReplacementProbeDef>("replacement_probe", 1);
            content.Component<ContractOutletPolicyDef>("contract_outlet_policy", 2);
            content.Component<PointDockDef>("point_dock", 7);
            content.Component<WorkPermitDef>("work_permit", 1);
            content.Component<ActivityRecordDef>("activity_record", 1);
            content.Component<ProofWorkerExecutionDef>("proof_worker_execution", 1);
            content.Component<ProofMotionExecutionDef>("proof_motion_execution", 1);
            content.Component<ProofOperatingInputsDef>("proof_operating_inputs", 2);
            content.Component<ReserveTransporterDef>("reserve_transporter", 1);
            content.Component<ContractHaulLoadDef>("contract_haul_load_policy", 2);
            content.Section<OddHaulLoadProfileDef>("contract_haul_load_profiles");
        }
        public override void Compose(WorldComposer composer)
        {
            composer.Commands.Register("points.set-access", new SetPointAccessHandler());
            composer.Commands.Register("points.probe-direct-capture", new DirectPointCaptureHandler());
            composer.Commands.Register("points.write-layer", new WritePointLayerHandler());
            composer.Commands.Register("points.assign-layer", new AssignPointLayerHandler());
        }
    }

    public static class PointGame
    {
        public static string Definitions => FrameworkProofContent.Read("points.json");
        public static GameFactory Factory(string definitions = null, string layerDefinitions = null,
            GameModule additionalModule = null, string moduleDeclaration = null, string moduleDocument = null, string alertProfiles = null, string handlingProfiles = null, string flowDefinitions = null, string retryProfiles = null, string dispatchProfiles = null, string constructionDefinitions = null, string recoveryDefinitions = null, string templateDefinitions = null, string knowledgeDefinitions = null,
            IReadOnlyList<GameModule> extraModules = null, string extraDeclarations = null, IReadOnlyDictionary<string, string> extraDocuments = null,
            IReadOnlyDictionary<string, string> additionalDocuments = null, IReadOnlyList<string> startupProfiles = null)
        {
            var modules = new List<GameModule> { new InventoryModule(), new ResourceFlowsModule(), new OperationsModule(), new ContentAccessModule(), new TransformationModule(), new MotionModule(), new TileNavigationModule(), new WorkModule(), new LogisticsModule(), new ConstructionModule(), new InfrastructureModule(), new IncidentResponseModule(), new WorldSetupModule(), new PointModule() };
            if (additionalModule != null) modules.Add(additionalModule);
            if (extraModules != null) foreach (GameModule module in extraModules) modules.Add(module);
            modules.Add(new CompanyKnowledgeModule());
            if (alertProfiles != null) modules.Add(new CompanyAlertModule());
            if (dispatchProfiles != null) modules.Add(new DispatchPolicyModule());
            var definition = new GameDefinition("point-proof", modules);
            JsonValue authored = Json.Parse(definitions ?? Definitions);
            if (startupProfiles != null) foreach (string profile in startupProfiles) StartupContentAuthoring.Include(authored, profile);
            var files = new PreloadedFileSource().Add("framework/access.json", FrameworkProofContent.Access)
                .Add("manifest.json", FrameworkProofContent.Read("point-master.json"))
                .Add("points.json", authored.ToString()).Add("flows.json", flowDefinitions ?? FrameworkProofContent.Read("resource-flows.json"));
            if (layerDefinitions != null)
            {
                files.Add("point-layers.json", layerDefinitions);
            }
            if (additionalModule != null)
            {
                JsonValue manifest = Json.Parse(files.ReadText("manifest.json"));
                JsonValue declaration = Json.Parse(moduleDeclaration);
                manifest.Get("modules").Add(declaration);
                var text = new System.Text.StringBuilder(); manifest.WriteCanonical(text);
                files.Add("manifest.json", text.ToString());
                files.Add(declaration.Get("documents")[0].Text, moduleDocument);
            }
            if (alertProfiles != null)
            {
                JsonValue manifest = Json.Parse(files.ReadText("manifest.json"));
                manifest.Get("modules").Add(Json.Parse("{\"id\":\"sim.company-alerts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"company-alerts.json\"]}"));
                var text = new System.Text.StringBuilder(); manifest.WriteCanonical(text);
                files.Add("manifest.json", text.ToString()).Add("company-alerts.json", alertProfiles);
            }
            if (dispatchProfiles != null)
            {
                JsonValue manifest = Json.Parse(files.ReadText("manifest.json"));
                manifest.Get("modules").Add(Json.Parse("{\"id\":\"sim.dispatch-policy\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"dispatch-policy.json\"]}"));
                var text = new System.Text.StringBuilder(); manifest.WriteCanonical(text);
                files.Add("manifest.json", text.ToString()).Add("dispatch-policy.json", dispatchProfiles);
            }
            JsonValue finalManifest = Json.Parse(files.ReadText("manifest.json"));
            if (extraDeclarations != null)
            {
                JsonValue declarations = Json.Parse(extraDeclarations);
                for (int i = 0; i < declarations.Count; i++) finalManifest.Get("modules").Add(declarations[i]);
            }
            finalManifest.Get("modules").Add(Json.Parse("{\"id\":\"sim.knowledge\",\"version\":\"1.0.0\",\"documents\":"
                + (knowledgeDefinitions == null ? "[]" : "[\"knowledge.json\"]") + "}"));
            for (int i = 0; i < finalManifest.Get("modules").Count; i++)
            {
                if (finalManifest.Get("modules")[i].Get("id").Text == LogisticsModule.ModuleId)
                    finalManifest.Get("modules")[i].Get("documents").Add(JsonValue.Of("point-handling.json"));
                if (finalManifest.Get("modules")[i].Get("id").Text == WorkModule.ModuleId)
                    finalManifest.Get("modules")[i].Get("documents").Add(JsonValue.Of("work-retry.json"));
                if (templateDefinitions != null && finalManifest.Get("modules")[i].Get("id").Text == WorkModule.ModuleId)
                    finalManifest.Get("modules")[i].Get("documents").Add(JsonValue.Of("plan-templates.json"));
                if (layerDefinitions != null && finalManifest.Get("modules")[i].Get("id").Text == "points")
                    finalManifest.Get("modules")[i].Get("documents").Add(JsonValue.Of("point-layers.json"));
                if (constructionDefinitions != null && finalManifest.Get("modules")[i].Get("id").Text == "points")
                    finalManifest.Get("modules")[i].Get("documents").Add(JsonValue.Of("point-construction.json"));
                if (recoveryDefinitions != null && finalManifest.Get("modules")[i].Get("id").Text == "points")
                    finalManifest.Get("modules")[i].Get("documents").Add(JsonValue.Of("point-recovery.json"));
                if (additionalDocuments != null && finalManifest.Get("modules")[i].Get("id").Text == "points")
                    foreach (var document in additionalDocuments) finalManifest.Get("modules")[i].Get("documents").Add(JsonValue.Of(document.Key));
            }
            var finalText = new System.Text.StringBuilder(); finalManifest.WriteCanonical(finalText);
            files.Add("manifest.json", finalText.ToString()).Add("point-handling.json", handlingProfiles ?? FrameworkProofContent.Read("point-handling.json"));
            files.Add("work-retry.json", retryProfiles ?? FrameworkProofContent.Read("work-retry.json"));
            if (constructionDefinitions != null) files.Add("point-construction.json", constructionDefinitions);
            if (recoveryDefinitions != null) files.Add("point-recovery.json", recoveryDefinitions);
            if (templateDefinitions != null) files.Add("plan-templates.json", templateDefinitions);
            if (knowledgeDefinitions != null) files.Add("knowledge.json", knowledgeDefinitions);
            if (extraDocuments != null) foreach (KeyValuePair<string, string> document in extraDocuments) files.Add(document.Key, document.Value);
            if (additionalDocuments != null) foreach (KeyValuePair<string, string> document in additionalDocuments) files.Add(document.Key, document.Value);
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
    }
}
