using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public static class LineCodes
    {
        public static readonly Code NotContiguous = new Code("core.path-not-contiguous");
        public static readonly Code AlreadyLaid = new Code("core.already-laid");
        public static readonly Code ForeignLine = new Code("core.foreign-line");
        public static readonly Code DurationRange = new Code("core.duration-range");
        public static readonly Code WrongSpace = new Code("core.line-wrong-space");
        public static readonly Code Done = new Code("note.line-done");
    }

    /// <summary>Sole corridor execution owner; durable path/order and progress remain in LineWork/WorkBoard.</summary>
    public sealed class LineConstructionService : ILineConstruction, ILineWorkEffects, ILineRemoval, IActivitySource, ICompositionValidator
    {
        private readonly World _world;
        private readonly ILineGeometry _geometry;
        private readonly ILineRemovalGeometry _removal;
        public LineConstructionService(World world, ILineGeometry geometry)
        { _world = world ?? throw new ArgumentNullException(nameof(world)); _geometry = geometry ?? throw new ArgumentNullException(nameof(geometry)); _removal = geometry as ILineRemovalGeometry; }
        private T ReadGeometry<T>(Entity target, Func<T> read)
            => EntityObservation.ReadProvider(_world,_world.Space,target,
                ()=>EntityObservation.ReadProvider(_world,_geometry,target,read,observeTargetState:true),observeTargetState:true);
        private IReadOnlyList<SpatialPlacement> Path(ILineWork work)
            => LineDeclaration.ReadPath(_world, _world.Content.Get<ILineDefinition>(work.Line).Layer.Space.Id, work.Path);
        public void ValidateComposition(World world)
        {
            if (!ReferenceEquals(world,_world) || !ReferenceEquals(world.Services.Get<ILineGeometry>(),_geometry))
                throw new ContentException("corridor geometry requires its actual session binding");
            if (_removal != null && (!ReferenceEquals(world.Services.Get<ILineRemovalGeometry>(),_removal) ||
                !ReferenceEquals(world.Services.Get<ILineRemoval>(),this)))
                throw new ContentException("corridor removal requires its original geometry and execution aliases");
            LineDefinitionContracts.ValidateComposition(world);
            LinePlacementContracts.ValidateComposition(world);
            ReadGeometry(null,()=> {
                if (_geometry.Space != world.Space?.Id) throw new ContentException("corridor geometry differs from selected space");
                foreach (ILineDefinition line in world.Content.All<ILineDefinition>())
                    if (line.Layer.Space.Id == world.Space.Id) _geometry.ValidateLine(world,line);
                return true;
            });
            foreach (ILineDefinition line in world.Content.All<ILineDefinition>())
                if (line.Layer.Space.Id == world.Space.Id) world.Services.Get<WorkBoard>().ValidatePlan(line);
        }
        public Code CheckIntent(PlayerId player, ILineDefinition line, IReadOnlyList<SpatialPlacement> path)
            => line == null ? Codes.UnknownDef : LineDefinitionContracts.Read(_world,line,null,()=>CheckIntentCore(player,line,path));
        private Code CheckIntentCore(PlayerId player, ILineDefinition line, IReadOnlyList<SpatialPlacement> path)
        {
            if (!ReferenceEquals(_world.Content.Find<ILineDefinition>(line.Id),line)) return Codes.UnknownDef;
            if (!_world.Players.IsSeat(player)) return Codes.NotSeated;
            if (_world.Space == null || line.Layer.Space.Id != _world.Space.Id) return LineCodes.WrongSpace;
            if (path == null || path.Count == 0) return Codes.InvalidArgument;
            foreach (SpatialPlacement placement in path)
                if (placement == null || placement.Space != line.Layer.Space.Id) return LineCodes.WrongSpace;
            try {
                if (!CapacityPolicyContracts.Check(line.WorkMarker.Component<IInventoryDefinition>().CapacityPolicy, Array.Empty<ResourceAmount>(),
                    Array.Empty<ResourceAmount>(),line.SupplyCapacity.Lines).Fits) return Codes.InvalidArgument;
            }
            catch (ContentException) { return Codes.InvalidArgument; }
            try {
                checked {
                    int nodes = line.Workflow.Graph.Nodes.Count * path.Count;
                    long ticks = 0;
                    foreach (IActivityNodeDefinition node in line.Workflow.Graph.Nodes) ticks += _world.Time.Ticks(node.Duration);
                    long duration = ticks * path.Count;
                }
            }
            catch (OverflowException) { return LineCodes.DurationRange; }
            return ReadGeometry(null,()=>_geometry.CheckPath(_world,line,path));
        }
        private Code CheckPlacement(PlayerId player, ILineDefinition line, SpatialPlacement placement, out bool alreadyLaid)
        {
            bool laid = false;
            Code check = ReadGeometry(null,()=>_geometry.CheckSegment(_world,player,line,placement,out laid));
            alreadyLaid = laid;
            return check.IsNone ? LinePlacementContracts.Check(_world,line.PlacementPolicy,player,placement) : check;
        }
        public Code Check(PlayerId player, ILineDefinition line, IReadOnlyList<SpatialPlacement> path)
            => line == null ? Codes.UnknownDef : LineDefinitionContracts.Read(_world,line,null,()=> {
                Code refusal = CheckIntentCore(player,line,path); if (!refusal.IsNone) return refusal;
                bool anyNew = false;
                foreach (SpatialPlacement placement in path) {
                    refusal = CheckPlacement(player,line,placement,out bool laid);
                    if (!refusal.IsNone) return refusal; anyNew |= !laid;
                }
                if (!anyNew) return LineCodes.AlreadyLaid;
                return _world.PrepareCreation(line.WorkMarker,player,EntityId.None,path[0],out _);
            });
        public Order Begin(PlayerId player, ILineDefinition line, IReadOnlyList<SpatialPlacement> path, int priority)
        {
            Code refusal = Check(player,line,path);
            if (!refusal.IsNone) throw new ArgumentException("line work refused: " + refusal,nameof(path));
            refusal = _world.PrepareCreation(line.WorkMarker,player,EntityId.None,path[0],out IEntityCreation creation);
            if (!refusal.IsNone) throw new InvalidOperationException("line worksite placement refused: " + refusal);
            Entity marker = _world.CommitCreation(creation); ILineWork work = marker.Get<ILineWork>();
            work.BindPath(_world,line,path);
            Order order = _world.Services.Get<WorkBoard>().Post(LineWorkContracts.ReadPlan(_world,work),player,marker.Id,priority);
            work.BindOrder(_world,order); return order;
        }
        public IActivityDefinition BindExecution(ILineDefinition line, EntityId target)
        {
            Entity cursor = _world.Entities.Get(target); ILineWork work = cursor?.Get<ILineWork>();
            if (line == null || !ReferenceEquals(_world.Content.Find<ILineDefinition>(line.Id),line) ||
                work == null || work.Line != line.Id || !ReferenceEquals(cursor.Def,line.WorkMarker))
                throw new CorruptStateException("line plan does not bind its actual authored cursor");
            return LineWorkContracts.ReadPlan(_world,work);
        }
        public string ExecutionIdentity(ILineDefinition line, Order order)
        {
            if (order == null || !ReferenceEquals(order.Plan,BindExecution(line,order.Target)))
                throw new CorruptStateException("line order graph differs from its cursor");
            ILineWork work = _world.Entities.Get(order.Target).Get<ILineWork>();
            if (work.Order != order.Id) throw new CorruptStateException("line cursor names another order");
            return LineDeclaration.PathIdentity(_world,line.Layer.Space.Id,Path(work));
        }
        public IReadOnlyList<IActivityDefinition> ActivityDefinitions {
            get {
                var plans = new List<IActivityDefinition>();
                foreach (ILineDefinition line in _world.Content.All<ILineDefinition>())
                    if (line.Layer.Space.Id == _world.Settings.Space) plans.Add(line);
                return plans.AsReadOnly();
            }
        }
        public void Advance()
        {
            var works = _world.Entities.Components<ILineWork>(); var board = _world.Services.Get<WorkBoard>();
            for (int i=0;i<works.Count;i++) {
                ILineWork work = works[i]; if (!work.Entity.Alive) continue;
                Order order = board.Order(work.Order);
                if (order == null || !order.IsActive) RemoveMarker(work.Entity,order);
            }
        }
        public void ValidateMarkers()
        {
            var works = _world.Entities.Components<ILineWork>();
            for (int i=0;i<works.Count;i++) {
                ILineWork work = works[i];
                Order order = _world.Services.Get<WorkBoard>().Order(work.Order);
                ILineDefinition line = _world.Content.Find<ILineDefinition>(work.Line);
                if (line == null || !ReferenceEquals(work.Entity.Def,line.WorkMarker) || order == null ||
                    order.Target != work.Entity.Id || order.Owner != work.Entity.Owner || order.PlanKey != line.PlanKey ||
                    _world.Space?.Id != line.Layer.Space.Id)
                    throw new CorruptStateException("line marker content/order binding differs");
                IReadOnlyList<SpatialPlacement> path;
                try { path = Path(work); }
                catch (ContentException error) { throw new CorruptStateException("invalid corridor placement path: " + error.Message); }
                if (!ReadGeometry(work.Entity,()=>_geometry.CheckPath(_world,line,path)).IsNone)
                    throw new CorruptStateException("invalid line segment path");
                ILineExecutionPlan plan = LineWorkContracts.ReadPlan(_world,work); plan.ValidateOrder(order);
                int completed = plan.CompletedSegments(order);
                if (!ReadGeometry(work.Entity,()=>_geometry.IsCursorAt(_world,work.Entity,path[Math.Min(completed,path.Count-1)])))
                    throw new CorruptStateException("line cursor differs from completed stage prefix");
            }
        }
        private bool ActualOrder(Entity marker, Order order)
            => marker != null && order != null && marker.Alive && ReferenceEquals(_world.Entities.Get(marker.Id),marker) &&
                ReferenceEquals(_world.Services.Get<WorkBoard>().Order(order.Id),order) && order.Target == marker.Id && order.Owner == marker.Owner;
        private Code ReadRemovalPath(PlayerId company, ILineDefinition line, Entity target, out IReadOnlyList<SpatialPlacement> path)
        {
            path = null;
            if (_removal == null) throw new InvalidOperationException("selected corridor geometry has no removal capability");
            if (line == null || !ReferenceEquals(_world.Content.Find<ILineDefinition>(line.Id),line)) return Codes.UnknownDef;
            if (!_world.Players.IsSeat(company)) return Codes.NotSeated;
            if (target == null || !target.Alive || !ReferenceEquals(_world.Entities.Get(target.Id),target)) return Codes.NotFound;
            if (target.Owner != company) return Codes.NotOwned;
            if (line.Layer.Space.Id != _world.Space?.Id) return LineCodes.WrongSpace;
            IReadOnlyList<SpatialPlacement> selected = null;
            Code refusal = ReadGeometry(target,()=>_removal.ReadRemovalPath(_world,line,target,out selected));
            if (!refusal.IsNone) return refusal;
            if (selected == null || selected.Count == 0) throw new InvalidOperationException("removal geometry returned no actual placements");
            var detached = new List<SpatialPlacement>();
            foreach (var placement in selected) {
                if (placement == null || placement.Space != line.Layer.Space.Id)
                    throw new InvalidOperationException("removal geometry returned foreign or missing placement");
                detached.Add(placement);
            }
            path = detached.AsReadOnly();
            // Use the original selected decoder/inverse contract; no second path codec or tile cast.
            _ = LineDeclaration.WritePath(_world,line.Layer.Space.Id,path);
            return Code.None;
        }
        Code ILineRemoval.CheckIntent(PlayerId company, ILineDefinition line, Entity target)
            => line == null ? Codes.UnknownDef : LineDefinitionContracts.Read(_world,line,target,()=>ReadRemovalPath(company,line,target,out _));
        Code ILineRemoval.Check(PlayerId company, ILineDefinition line, Entity target)
            => line == null ? Codes.UnknownDef : LineDefinitionContracts.Read(_world,line,target,()=> {
                Code refusal = ReadRemovalPath(company,line,target,out var path);
                return !refusal.IsNone ? refusal : ReadGeometry(target,()=>_removal.CheckRemoval(_world,company,line,path));
            });
        Code ILineRemoval.Remove(PlayerId company, ILineDefinition line, Entity target)
        {
            Code refusal = ReadRemovalPath(company,line,target,out var path);
            if (refusal.IsNone) refusal = ReadGeometry(target,()=>_removal.CheckRemoval(_world,company,line,path));
            return !refusal.IsNone ? refusal : _removal.Remove(_world,company,line,path);
        }
        public Code CheckSegment(Entity marker, Order order, IActivityNodeDefinition step)
        {
            if (!ActualOrder(marker,order)) return WorkEffectCodes.NotComplete;
            ILineWork work = marker.Get<ILineWork>();
            if (work == null || work.Order != order.Id || step == null) return WorkEffectCodes.NotComplete;
            ILineExecutionPlan plan = LineWorkContracts.ReadPlan(_world,work);
            if (!ReferenceEquals(order.Plan,plan) || !ReferenceEquals(plan.Graph.Find(step.Id),step)) return WorkEffectCodes.NotComplete;
            int index = plan.CompletedSegments(order); var path = Path(work);
            ILineDefinition line = _world.Content.Get<ILineDefinition>(work.Line);
            if (index >= path.Count || step.Id != LinePlan.NodeId(index,line.SegmentNode) ||
                !order.Node(step.Id).Progress.Complete(_world.Time.Ticks(step.Duration))) return WorkEffectCodes.NotComplete;
            foreach (IActivityNodeDefinition node in line.Workflow.Graph.Nodes)
                if (node.Id != line.SegmentNode && order.Node(LinePlan.NodeId(index,node.Id)).State != NodeState.Done)
                    return WorkEffectCodes.NotComplete;
            if (!ReadGeometry(marker,()=>_geometry.IsCursorAt(_world,marker,path[index]))) return Codes.InvalidArgument;
            Code refusal = CheckPlacement(order.Owner,line,path[index],out _);
            if(refusal.IsNone&&_world.Services.TryGet<INetworkTopology>(out var topology))
                refusal=EntityObservation.ReadProvider(_world,topology,marker,
                    ()=>topology.CheckAddition(order.Owner,line,new[]{path[index]}),_world.Space,observeTargetState:true);
            return !refusal.IsNone ? refusal : index+1 < path.Count ? CheckMarkerRemoval(marker) : Code.None;
        }
        public Code CompleteSegment(Entity marker, Order order, IActivityNodeDefinition step)
        {
            Code refusal = CheckSegment(marker,order,step); if (!refusal.IsNone) return refusal;
            ILineWork work = marker.Get<ILineWork>(); int index = LineWorkContracts.ReadPlan(_world,work).CompletedSegments(order);
            var path = Path(work);
            if (index+1 < path.Count && marker.Get<IInventory>().Stock.Count > 0 &&
                !_world.Services.Get<IResourceRecovery>().Evacuate(_world,marker).IsNone)
                throw new InvalidOperationException("line segment recovery changed after preflight");
            refusal = _geometry.Stamp(_world,marker.Owner,_world.Content.Get<ILineDefinition>(work.Line),path[index]);
            if (!refusal.IsNone) throw new InvalidOperationException("line segment geometry changed after preflight: " + refusal);
            if (index+1 < path.Count) _geometry.MoveCursor(_world,marker,path[index+1]);
            marker.Touch(); return Code.None;
        }
        private Code CheckMarkerRemoval(Entity marker)
        {
            IInventory stock = marker.Get<IInventory>();
            if (stock == null || stock.Stock.Count == 0) return _world.CheckDespawn(marker);
            return _world.Services.TryGet<IResourceRecovery>(out var recovery)
                ? recovery.CheckEvacuation(_world,marker) : ResourceTransactions.MissingRecovery;
        }
        private Code RemoveMarker(Entity marker, Order order)
        {
            Code refusal = CheckMarkerRemoval(marker);
            if (refusal.IsNone && marker.Get<IInventory>()?.Stock.Count > 0)
                refusal = _world.Services.Get<IResourceRecovery>().Evacuate(_world,marker);
            if (refusal.IsNone) _world.Despawn(marker);
            else if (order != null && order.Blocker != refusal) {
                order.Blocker = refusal; marker.Touch(); _world.Notify(refusal,marker.Owner,marker.Id,order.PlanKey);
            }
            return refusal;
        }
        public Code CheckFinish(Entity marker, Order order, IActivityNodeDefinition step)
        {
            if (!ActualOrder(marker,order)) return WorkEffectCodes.NotComplete;
            ILineWork work = marker.Get<ILineWork>();
            return work == null || work.Order != order.Id || step != null ||
                !ReferenceEquals(order.Plan,LineWorkContracts.ReadPlan(_world,work)) || order.NodesDone != order.Nodes.Count ||
                LineWorkContracts.ReadPlan(_world,work).CompletedSegments(order) != work.Path.Count
                ? WorkEffectCodes.NotComplete : CheckMarkerRemoval(marker);
        }
        public Code Finish(Order order)
        {
            if (order == null) return WorkEffectCodes.NotComplete;
            Entity marker = _world.Entities.Get(order.Target); if (marker == null) return Codes.NotFound;
            Code refusal = CheckFinish(marker,order,null); if (!refusal.IsNone) return refusal;
            ILineWork work = marker.Get<ILineWork>(); string line = work.Line; int count = work.Path.Count;
            refusal = RemoveMarker(marker,order);
            if (!refusal.IsNone) throw new InvalidOperationException("line recovery changed after successful preflight");
            _world.Notify(LineCodes.Done,order.Owner,EntityId.None,line,Fix.FromInt(count)); return Code.None;
        }
    }
}
