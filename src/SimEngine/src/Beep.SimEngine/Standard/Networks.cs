using System;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine.Content;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Standard
{
    // ======================================================================== content

    /// <summary>Native JSON registration for the shared physical line declaration, never a required extension base.</summary>
    public sealed class LineDef : Def, ILineDefinition
    {
        private LineDeclaration _declaration;
        public bool AutomaticSupply => _declaration.AutomaticSupply;
        public IReadOnlyList<IContentDefinition> DisclosureContent => _declaration.DisclosureContent;
        public IActivityDefinition Workflow => _declaration.Workflow;
        public string LayerId => _declaration.LayerId;
        public string SegmentNode => _declaration.SegmentNode;
        public Bill SupplyCapacity => _declaration.SupplyCapacity;
        public EntityDef WorkMarker => _declaration.WorkMarker;
        public ISpatialLayerDefinition Layer => _declaration.Layer;
        public ISpatialLayerDefinition OwnerLayer => _declaration.OwnerLayer;
        public Fix Value => _declaration.Value;
        public bool Owned => _declaration.Owned;
        public ILinePlacementPolicy PlacementPolicy => _declaration.PlacementPolicy;
        public string SegmentEffect => _declaration.SegmentEffect;
        public string FinishEffect => _declaration.FinishEffect;
        public string PlanKey => _declaration.PlanKey;
        public IActivityGraph Graph => _declaration.Graph;
        public string OnComplete => _declaration.OnComplete;
        protected override void ReadCore(JsonReader reader) => _declaration = LineDeclaration.Read(reader, this);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateResolvedCore(ContentResolver content) => _declaration.ValidateResolved(content);
        public string IntentIdentity(World world, JsonValue arguments) => _declaration.IntentIdentity(world, arguments);
        public System.Collections.Generic.IReadOnlyList<EntityId> ReadEntityReferences(World world, JsonValue arguments) => _declaration.ReadEntityReferences(world, arguments);
        public Code CheckEntityReplacement(World world, PlayerId company, JsonValue arguments, Entity source, EntityDef destination) => _declaration.CheckEntityReplacement(world, company, arguments, source, destination);
        public JsonValue ReplaceEntityReference(World world, JsonValue arguments, EntityId source, EntityId destination) => _declaration.ReplaceEntityReference(world, arguments, source, destination);
        public IActivityDefinition BindExecution(World world, EntityId target) => _declaration.BindExecution(world, target);
        public string ExecutionIdentity(World world, Order order) => _declaration.ExecutionIdentity(world, order);
        public Code CheckSubmission(World world, PlayerId company, JsonValue arguments) => _declaration.CheckSubmission(world, company, arguments);
        public Code CheckActivation(World world, PlayerId company, JsonValue arguments) => _declaration.CheckActivation(world, company, arguments);
        public Order Activate(World world, PlayerId company, JsonValue arguments, int priority) => _declaration.Activate(world, company, arguments, priority);
        public PlanIntent CreateIntent(JsonValue path, string group, int priority, bool paused) => _declaration.CreateIntent(path, group, priority, paused);
        public void ValidateTemplate(World world, JsonValue arguments) => _declaration.ValidateTemplate(world, arguments);
        public void ValidateTemplateAnchor(World world, JsonValue arguments, JsonValue anchor) => _declaration.ValidateTemplateAnchor(world, arguments, anchor);
        public Code CaptureTemplate(World world, PlayerId company, JsonValue arguments, JsonValue origin, out JsonValue template)
            => _declaration.CaptureTemplate(world, company, arguments, origin, out template);
        public Code PlaceTemplate(World world, PlayerId company, JsonValue arguments, JsonValue anchor, out JsonValue intent)
            => _declaration.PlaceTemplate(world, company, arguments, anchor, out intent);
    }

    /// <summary>Native JSON registration for the shared network declaration; not a required extension base.</summary>
    public sealed class NetworkDef : Def, INetworkDefinition
    {
        private NetworkDeclaration _declaration;
        public ILineDefinition Line => _declaration.Line;
        public IReadOnlyList<ResourceId> Resources => _declaration.Resources;
        public ICapacityPolicyDefinition CapacityPolicy => _declaration.CapacityPolicy;
        public bool Public => _declaration.Public;
        public IReadOnlyList<IResourceRate> Rates => _declaration.Rates;
        public INetworkFlowPolicy FlowPolicy => _declaration.FlowPolicy;
        public INetworkSolver Solver => _declaration.Solver;
        public INetworkDemandPolicy DemandPolicy => _declaration.DemandPolicy;
        protected override void ReadCore(JsonReader reader) => _declaration = NetworkDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public bool Carries(ResourceId resource) => _declaration.Carries(resource);
        public IResourceRate Rate(ResourceId resource) => _declaration.Rate(resource);
    }

    public readonly struct PortSpec
    {
        public INetworkDefinition Network { get; }
        public IReadOnlyList<ResourceId> Push { get; }
        public IReadOnlyList<ResourceId> Pull { get; }

        public PortSpec(INetworkDefinition network, IEnumerable<ResourceId> push, IEnumerable<ResourceId> pull)
        {
            Network = network ?? throw new ArgumentNullException(nameof(network));
            Push = new List<ResourceId>(push ?? Array.Empty<ResourceId>()).AsReadOnly();
            Pull = new List<ResourceId>(pull ?? Array.Empty<ResourceId>()).AsReadOnly();
        }
    }

    /// <summary>Native JSON registration for the shared connection declaration, not an extension base.</summary>
    public sealed class PortsDef : ComponentDef<Ports>, INetworkPortsDefinition
    {
        private NetworkPortsDeclaration _declaration;
        public IReadOnlyList<PortSpec> Connections => _declaration.Connections;
        protected override void ReadCore(JsonReader reader) => _declaration = NetworkPortsDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateCore(EntityDef owner) => _declaration.ValidateOwner(owner);
    }

    public sealed class Ports : Component, INetworkPorts
    {
        public new INetworkPortsDefinition Def => (INetworkPortsDefinition)base.Def;
        public IReadOnlyList<PortSpec> Connections => Def.Connections;
    }

    // ======================================================================== line laying

    /// <summary>A line being laid: its path, how far the machine has got, and its order.</summary>
    public sealed class LineWork : Component, ILineWork
    {
        private LinePlan _plan;
        public ILineExecutionPlan ExecutionPlan(World world)
        {
            ILineDefinition definition = world.Content.Find<ILineDefinition>(Line);
            if (definition == null || Path == null || Path.Count == 0) throw new CorruptStateException("line execution lacks its authored definition/path");
            if (_plan == null) _plan = new LinePlan(definition, Path.Count);
            if (_plan.PlanKey != definition.PlanKey || _plan.Graph.Nodes.Count != checked(Path.Count * definition.Workflow.Graph.Nodes.Count))
                throw new CorruptStateException("line execution graph differs from actual cursor content");
            return _plan;
        }
        public bool RetainsOrder(Order order) => Entity.Alive && order != null && order.Target == Entity.Id && order.Id == Order;
        private string _line;
        private JsonValue _path;
        private int _order;
        public string Line => _line;
        public JsonValue Path => _path;
        public int Order => _order;
        public void BindPath(World world, ILineDefinition line, IReadOnlyList<SpatialPlacement> path)
        {
            if (world == null || line == null || path == null || path.Count == 0 || _line != null || _order != 0 ||
                !ReferenceEquals(world.Entities.Get(Entity.Id), Entity) || !Entity.Alive ||
                !ReferenceEquals(world.Content.Find<ILineDefinition>(line.Id), line) || !ReferenceEquals(line.WorkMarker, Entity.Def))
                throw new InvalidOperationException("line path initializes its actual authored worksite once");
            JsonValue canonical = LineDeclaration.WritePath(world, line.Layer.Space.Id, path);
            _line = line.Id; _path = canonical; Entity.Touch();
        }
        public void BindOrder(World world, Order order)
        {
            if (world == null || order == null || _order != 0 ||
                !ReferenceEquals(world.Services.Get<WorkBoard>().Order(order.Id), order) ||
                order.Target != Entity.Id || order.Owner != Entity.Owner || !ReferenceEquals(order.Plan, ExecutionPlan(world)))
                throw new InvalidOperationException("line worksite binds its actual graph order once");
            _order = order.Id; Entity.Touch();
        }

        public override void Sync(IStateSync s)
        {
            if (s.Reading) _plan = null;
            s.String(ref _line);
            string path = _path?.ToString(); s.String(ref path);
            if (s.Reading) {
                try {
                    _path = Json.Parse(path);
                    if (!_path.IsArray || _path.Count == 0 || _path.ToString() != path) throw new ContentException("canonical corridor path required");
                    _path.Freeze();
                }
                catch (Exception error) { throw new CorruptStateException("invalid corridor placement path: " + error.Message); }
            }
            s.Int(ref _order);
            // Order zero is the real transient BindPath -> WorkBoard.Post -> BindOrder stage.
            // Stable checkpoints/loads require the actual positive order in ValidateMarkers.
            if (string.IsNullOrWhiteSpace(Line) || Path == null || !Path.IsArray || Path.Count == 0 || Order < 0)
                throw new CorruptStateException("line work requires a line, path and nonnegative order binding");
        }
    }

    public sealed class LineWorkDef : ComponentDef<LineWork>, ILineWorkDefinition
    {
        protected override void ReadCore(JsonReader r) { }
        public void ValidateBinding(EntityDef owner) => LineWorkContracts.ValidateBinding(owner);
        protected override void ValidateCore(EntityDef owner) => ValidateBinding(owner);
    }

    /// <summary>Geometry binding of the authored segment DAG through the sole activity compiler.</summary>
    internal sealed class LinePlan : ILineExecutionPlan
    {
        private readonly ILineDefinition _line;
        private readonly int _segments;
        public IActivityDefinition Workflow => _line.Workflow;
        public bool AutomaticSupply => Workflow.AutomaticSupply;
        public IReadOnlyList<IContentDefinition> DisclosureContent => _line.DisclosureContent;

        public LinePlan(ILineDefinition line, int segments)
        {
            if (segments <= 0) throw new ArgumentOutOfRangeException(nameof(segments));
            _line = line; _segments = segments;
            PlanKey = line.PlanKey;
            var nodes = new List<IActivityNodeDefinition>(checked(segments * Workflow.Graph.Nodes.Count));
            for (int i = 0; i < segments; i++)
                foreach (IActivityNodeDefinition node in Workflow.Graph.Nodes)
                {
                    var dependencies = new List<string>();
                    foreach (string dependency in node.Dependencies) dependencies.Add(NodeId(i, dependency));
                    if (i > 0 && dependencies.Count == 0) dependencies.Add(NodeId(i - 1, line.SegmentNode));
                    nodes.Add(ActivityNodeDef.Make(NodeId(i, node.Id), node.Executor, node.Roles, node.Duration,
                        dependencies.ToArray(), node.Effect, node.Requirement, node.RequirementMultiplier));
                }
            Graph = new ActivityGraph(nodes, PlanKey);
            OnComplete = line.FinishEffect;
        }
        internal static string NodeId(int index, string authored) => "segment." + index.ToString(System.Globalization.CultureInfo.InvariantCulture) + "." + authored;
        /// <summary>Derived from the actual terminal states. No second laid-progress counter.</summary>
        public int CompletedSegments(Order order)
        {
            int completed = 0;
            for (int i = 0; i < _segments; i++)
            {
                ActivityNodeExecution terminal = order.Node(NodeId(i, _line.SegmentNode));
                if (terminal == null) throw new CorruptStateException("line segment final node is missing");
                if (terminal.State != NodeState.Done) break;
                completed++;
            }
            return completed;
        }
        public void ValidateOrder(Order order)
        {
            if (!ReferenceEquals(order.Plan, this) || order.Nodes.Count != Graph.Nodes.Count)
                throw new CorruptStateException("line order differs from its actual bound graph");
            for (int i = 0; i < Graph.Nodes.Count; i++)
                if (order.Nodes[i].Id != Graph.Nodes[i].Id) throw new CorruptStateException("line node identities differ");
            int completed = CompletedSegments(order);
            for (int segment = 0; segment < _segments; segment++)
                foreach (IActivityNodeDefinition node in Workflow.Graph.Nodes)
                {
                    ActivityNodeExecution actual = order.Node(NodeId(segment, node.Id));
                    if (segment < completed && actual.State != NodeState.Done ||
                        segment > completed && (actual.State != NodeState.Waiting &&
                            !(order.State == OrderState.Cancelled && actual.State == NodeState.Cancelled) &&
                            !(order.State == OrderState.Failed && actual.State == NodeState.Failed) || actual.JobId != 0 ||
                            actual.Progress.WholeTicks != 0 || actual.Progress.Fraction != 0))
                        throw new CorruptStateException("line completed/active stages are not a segment prefix");
                }
        }
        public string PlanKey { get; }
        public IActivityGraph Graph { get; }
        public string OnComplete { get; }
    }

    /// <summary>
    /// Selected spatial graph topology and timed resource flow. The original pool/transaction
    /// algorithms conserve real stock through splits/joins; spatial providers never own balances.
    /// </summary>
    public sealed partial class NetworkService : IPersistentService, ICompositionValidator, INetworkTopology, IResourceEndpointProvider, ILineSimulation, IEntityReplacementReferenceOwner
    {
        public int StateSchemaVersion => 7;
        public World World => _world;
        public string EndpointScope => "sim.infrastructure.pool";
        public static readonly Code OccupiedNetwork = new Code("sim.network.occupied");
        public static readonly Code NetworkCapacity = new Code("sim.network.insufficient-capacity");
        public static readonly Code NoteLineBlocked = new Code("note.line-blocked");

        private readonly World _world;
        private readonly INetworkGraphGeometry _geometry;
        private readonly Dictionary<string, NetState> _nets = new Dictionary<string, NetState>(StringComparer.Ordinal);
        private readonly List<NetState> _netList = new List<NetState>();
        private long _nextEndpoint = 1;
        private List<TopologyChange> _committing;

        internal sealed class NetState
        {
            public INetworkDefinition Def;
            public Dictionary<string, int> ComponentOf;
            public NetworkService Owner;
            // Layer versions are transient invalidation cursors, not saved game state.
            public int ObservedGeometryVersion = -1, ObservedOwnershipVersion = -1;
            public bool RestoredPendingGeometry;
            public NetworkGeometryVersion Version => Owner.ReadVersion(Def.Line);
            public bool Dirty => ComponentOf == null || RestoredPendingGeometry || Version.Geometry != ObservedGeometryVersion || Version.Ownership != ObservedOwnershipVersion;
            public readonly List<NetComponent> Components = new List<NetComponent>();
            public readonly List<FlowCursor> Flow = new List<FlowCursor>();
        }

        public sealed class NetComponent
        {
            internal readonly List<FlowTurn> FlowTurns = new List<FlowTurn>();
            public int MemberCount => Members.Count;
            public PlayerId Owner { get; }
            public IReadOnlyList<string> Members { get; }
            public ResourceInventoryState Inventory { get; }
            public ResourceEndpointId Endpoint => Inventory.Endpoint;
            public int Version => _version;
            internal int _version;
            public IReadOnlyList<ResourceAmount> Pool => Inventory.Stock;
            public Fix Amount(ResourceId resource) => Inventory.Amount(resource);
            internal NetComponent(World world, INetworkDefinition network, ResourceEndpointId endpoint, PlayerId owner, List<string> members)
            {
                Owner = owner; Members = members.AsReadOnly();
                var capacity = new InstancedCapacityPolicy(network.CapacityPolicy, members.Count);
                Inventory = new ResourceInventoryState(endpoint, () => Owner, () => _version,
                    () => _version = checked(_version + 1), world.Content, () => capacity, resource => NetworkDefinitionContracts.Read(world, network, null, () => network.Carries(resource)),
                    network.Public, false);
            }
        }

        public NetworkService(World world,INetworkGraphGeometry geometry)
        {
            _world=world??throw new ArgumentNullException(nameof(world));_geometry=geometry??throw new ArgumentNullException(nameof(geometry));
            foreach(var definition in world.Content.All<INetworkDefinition>()) {
                if(definition.Line.Layer.Space.Id!=world.Space?.Id)continue;
                var state=new NetState{Def=definition,Owner=this};_nets.Add(definition.Id,state);_netList.Add(state);
            }
        }
        private T ReadGeometry<T>(Entity target,Func<T> read)
            =>EntityObservation.ReadProvider(_world,_world.Space,target,
                ()=>EntityObservation.ReadProvider(_world,_geometry,target,read,observeTargetState:true),observeTargetState:true);
        private NetworkGeometryVersion ReadVersion(ILineDefinition line)
            =>ReadGeometry(null,()=>_geometry.ReadVersion(_world,line));
        private List<string> Neighbours(ILineDefinition line,string identity)
            =>ReadGeometry(null,()=>{
                var result=new List<string>();_geometry.CopyNeighbours(_world,line,identity,result);
                var seen=new HashSet<string>(StringComparer.Ordinal);
                foreach(var next in result)if(string.IsNullOrWhiteSpace(next)||next==identity||!seen.Add(next)||!_geometry.ValidateIdentity(_world,line,next))
                    throw new InvalidOperationException("graph provider returned invalid potential neighbour");
                result.Sort(StringComparer.Ordinal);return result;
            });

        public string Id => "sim.lines";
        public IResourceEndpoint Resolve(ResourceEndpointId endpoint)
        {
            if (endpoint.Scope != EndpointScope || !endpoint.IsValid) return null;
            foreach (NetState state in _netList)
                foreach (NetComponent component in state.Components)
                    if (component.Endpoint == endpoint) return component.Inventory;
            if (_committing != null)
                foreach (TopologyChange change in _committing)
                    foreach (TopologyPart part in change.Parts)
                        if (part.Live != null && part.Live.Endpoint == endpoint) return part.Live.Inventory;
            return null;
        }
        public void InspectReservations(Action<ResourceEndpointId, IReadOnlyList<ResourceReservation>> inspect)
        {
            foreach (NetState state in _netList)
                foreach (NetComponent component in state.Components) inspect(component.Inventory.Endpoint, component.Inventory.Reservations);
        }
        public void ValidateComposition(World world)
        {
            if(!ReferenceEquals(world,_world)||_geometry.Space!=world.Space?.Id||!ReferenceEquals(world.Services.Get<INetworkGraphGeometry>(),_geometry))
                throw new ContentException("network graph requires its actual selected spatial provider");
            foreach(var state in _netList)ReadGeometry(null,()=>{_geometry.ValidateNetwork(world,state.Def);return true;});
            // Every loaded policy, including unused solver content, is admitted before Initialize.
            NetworkDefinitionContracts.ValidateComposition(world);
            NetworkPolicyContracts.ValidateComposition(world);
            NetworkPortsContracts.ValidateComposition(world);

        }

        // ------------------------------------------------------------------ laying

        public Code CheckAddition(PlayerId player,ILineDefinition line,IReadOnlyList<SpatialPlacement> path)
        {
            if(line==null||!ReferenceEquals(_world.Content.Find<ILineDefinition>(line.Id),line))return Codes.UnknownDef;
            if(line.Layer.Space.Id!=_world.Space?.Id)return LineCodes.WrongSpace;
            if(!_world.Players.IsSeat(player)||path==null||path.Count==0)return Codes.InvalidArgument;
            var proposed=new List<SpatialPlacement>();
            var geometry=_world.Services.Get<ILineGeometry>();
            foreach(var placement in path) {
                var check=EntityObservation.ReadProvider(_world,geometry,null,()=>{
                    Code refusal=geometry.CheckSegment(_world,player,line,placement,out bool laid);
                    return (Refusal:refusal,Laid:laid);
                },_world.Space);
                if(!check.Refusal.IsNone)return check.Refusal;
                Code policy=LinePlacementContracts.Check(_world,line.PlacementPolicy,player,placement);if(!policy.IsNone)return policy;
                if(!check.Laid)proposed.Add(placement);
            }
            foreach(var state in _netList) {
                if(state.Def.Line.Layer.Id!=line.Layer.Id)continue;
                var additions=ReadGeometry(null,()=>{
                    var vertices=new List<NetworkVertex>();var seen=new HashSet<string>(StringComparer.Ordinal);
                    foreach(var placement in proposed) {
                        string identity=_geometry.VertexIdentity(_world,state.Def.Line,placement);
                        if(string.IsNullOrWhiteSpace(identity)||!_geometry.ValidateIdentity(_world,state.Def.Line,identity)||!seen.Add(identity))
                            throw new InvalidOperationException("graph provider returned invalid addition identity");
                        vertices.Add(new NetworkVertex(identity,state.Def.Line.Owned?player:PlayerId.None));
                    }
                    return vertices;
                });
                Code refusal=Redistribute(BuildTopology(state,null,additions));if(!refusal.IsNone)return refusal;
            }
            return Code.None;
        }
        public Code CheckRemoval(PlayerId player,ILineDefinition line,IReadOnlyList<SpatialPlacement> path)
            =>EvaluateRemoval(player,line,path,out _);
        public Code Remove(PlayerId player,ILineDefinition line,IReadOnlyList<SpatialPlacement> path)
        {
            Code refusal=EvaluateRemoval(player,line,path,out var changes);if(!refusal.IsNone)return refusal;
            ApplyTopology(changes,()=>_geometry.Erase(_world,player,line,path));return Code.None;
        }

        private sealed class TopologyChange
        {
            public NetState State;
            public NetworkGeometryVersion CommittedVersion;
            public Dictionary<string, int> Map;
            public readonly List<TopologyPart> Parts = new List<TopologyPart>();
            public readonly List<TopologyLeg> Legs = new List<TopologyLeg>();
        }
        private sealed class TopologyPart
        {
            public PlayerId Owner;
            public readonly List<string> Members = new List<string>();
            // Temporary preflight allocation, never a live balance or persisted pool.
            public readonly List<ResourceAmount> Allocation = new List<ResourceAmount>();
            public NetComponent Live;
        }
        private readonly struct TopologyLeg
        {
            public readonly int From, To;
            public readonly ResourceAmount Quantity;
            public TopologyLeg(int from, int to, ResourceAmount quantity) { From = from; To = to; Quantity = quantity; }
        }

        private Code EvaluateRemoval(PlayerId player,ILineDefinition line,IReadOnlyList<SpatialPlacement> path,out List<TopologyChange> changes)
        {
            changes=new List<TopologyChange>();
            if(line==null||!ReferenceEquals(_world.Content.Find<ILineDefinition>(line.Id),line))return Codes.UnknownDef;
            if(line.Layer.Space.Id!=_world.Space?.Id)return LineCodes.WrongSpace;
            if(!_world.Players.IsSeat(player)||path==null||path.Count==0)return Codes.InvalidArgument;
            Code refusal=ReadGeometry(null,()=>_geometry.CheckErase(_world,player,line,path));if(!refusal.IsNone)return refusal;
            var removed=ReadGeometry(null,()=>{
                var result=new HashSet<string>(StringComparer.Ordinal);
                foreach(var placement in path){string identity=_geometry.VertexIdentity(_world,line,placement);
                    if(string.IsNullOrWhiteSpace(identity)||!_geometry.ValidateIdentity(_world,line,identity)||!result.Add(identity))
                        throw new InvalidOperationException("graph provider returned invalid removal identity");}
                return result;
            });
            foreach(var state in _netList){if(state.Def.Line.Layer.Id!=line.Layer.Id)continue;
                var candidate=BuildTopology(state,removed);refusal=Redistribute(candidate);if(!refusal.IsNone)return refusal;changes.Add(candidate);}
            return Code.None;
        }

        /// <summary>
        /// Rebuilds the components of networks whose line changed. Called only from the line system, at a
        /// fixed point of the tick, so a network's contents are redistributed at the same moment in every
        /// copy of the game (saving or hashing never triggers it).
        /// </summary>
        public void RebuildDirty()
        {
            var changes = new List<TopologyChange>();
            for (int i = 0; i < _netList.Count; i++)
                if (_netList[i].Dirty || _netList[i].ComponentOf == null)
                    changes.Add(PrepareTopology(_netList[i]));
            ApplyTopology(changes);
        }

        // ------------------------------------------------------------------ networks

        public INetworkDefinition Network(string id) => _nets.TryGetValue(id, out NetState s) ? s.Def : null;

        /// <summary>The connected component a tile belongs to in a network (-1: none).</summary>
        public int ComponentAt(string network,string identity)
        {
            var state=State(network);
            return ReadGeometry(null,()=>_geometry.ValidateIdentity(_world,state.Def.Line,identity))&&state.ComponentOf.TryGetValue(identity,out int index)?index:-1;
        }
        public int ComponentOf(string network,Entity entity,EntityDef footprint)
        {
            if(entity?.Alive!=true||!ReferenceEquals(_world.Entities.Get(entity.Id),entity)||footprint==null||
                !ReferenceEquals(_world.Content.Find<EntityDef>(footprint.Id),footprint))
                throw new ArgumentException("network contact requires actual anchor and catalog footprint");
            var state=State(network);
            for(int i=0;i<state.Components.Count;i++){
                var component=state.Components[i];if(component.Owner.IsValid&&component.Owner!=entity.Owner)continue;
                foreach(string identity in component.Members)
                    if(ReadGeometry(entity,()=>_geometry.Touches(_world,state.Def.Line,entity,footprint,identity)))return i;
            }
            return -1;
        }

        public NetComponent Component(string network, int index) => State(network).Components[index];
        public int ComponentCount(string network) => State(network).Components.Count;

        internal NetState State(string network)
        {
            NetState state = _nets[network];
            if (state.ComponentOf == null) throw new InvalidOperationException("network graph has not initialized: " + network);
            return state;
        }

        internal IReadOnlyList<NetState> All => _netList;

        private TopologyChange PrepareTopology(NetState state)
        {
            TopologyChange candidate = BuildTopology(state, null);
            Code refusal = Redistribute(candidate);
            if (!refusal.IsNone) throw new InvalidOperationException("network topology changed without valid disposition: " + state.Def.Id + ": " + refusal.Id);
            return candidate;
        }

        private void ApplyTopology(List<TopologyChange> changes, Action geometry = null)
        {
            try { ApplyTopologyCore(changes, geometry); }
            catch (Exception error) { _world.LatchFault("network.topology.commit", error); throw; }
        }
        private readonly struct TopologyResourceObservation
        {
            private readonly NetComponent _owner;
            private readonly int _version;
            private readonly EntityObservation.ProviderScope _inventory;
            public TopologyResourceObservation(World world, NetComponent owner)
            {
                _owner = owner; _version = owner.Version;
                _inventory = EntityObservation.CaptureProvider(world, owner.Inventory, owner.Inventory.Sync, "network.topology.commit");
            }
            public void Verify()
            {
                if (_owner.Version != _version)
                    throw new InvalidOperationException("network topology geometry mutated an endpoint's original version");
                _inventory.Verify();
            }
        }
        private List<TopologyResourceObservation> ObserveTopologyResources(List<TopologyChange> changes)
        {
            var seen = new HashSet<NetComponent>();
            var observations = new List<TopologyResourceObservation>();
            // Membership lookup only uses hashing; capture/verification order follows stable graph order.
            foreach (TopologyChange change in changes)
            {
                foreach (NetComponent owner in change.State.Components)
                    if (seen.Add(owner)) observations.Add(new TopologyResourceObservation(_world, owner));
                foreach (TopologyPart part in change.Parts)
                    if (seen.Add(part.Live)) observations.Add(new TopologyResourceObservation(_world, part.Live));
            }
            return observations;
        }
        private void ApplyTopologyCore(List<TopologyChange> changes, Action geometry)
        {
            if (_committing != null) throw new InvalidOperationException("network topology commit is already in progress");
            long additions = 0;
            foreach (TopologyChange change in changes)
                foreach (TopologyPart part in change.Parts) if (part.Live == null) additions = checked(additions + 1);
            _ = checked(_nextEndpoint + additions);
            // These are the new actual owners, not stock copies. They resolve only during this commit.
            _committing = changes;
            foreach (TopologyChange change in changes)
                foreach (TopologyPart part in change.Parts)
                    if (part.Live == null)
                    {
                        var endpoint = new ResourceEndpointId(EndpointScope, _nextEndpoint);
                        _nextEndpoint = checked(_nextEndpoint + 1);
                        part.Live = new NetComponent(_world, change.State.Def, endpoint, part.Owner, part.Members);
                    }
            var grouped = new Dictionary<(NetComponent From, NetComponent To), List<ResourceAmount>>();
            foreach (TopologyChange change in changes) foreach (TopologyLeg leg in change.Legs)
            {
                var key = (From: change.State.Components[leg.From], To: change.Parts[leg.To].Live);
                if (!grouped.TryGetValue(key, out var amounts)) grouped.Add(key, amounts = new List<ResourceAmount>());
                amounts.Add(leg.Quantity);
            }
            var movements = new List<ResourceMovementIntent>();
            foreach (var group in grouped)
                movements.Add(new ResourceMovementIntent(new ReservationOwner("sim.infrastructure.topology", group.Key.From.Endpoint.Identity),
                    group.Key.From.Owner, group.Key.From.Endpoint, group.Key.To.Endpoint, new Bill(group.Value), Array.Empty<ResourceReservation>(),
                    Array.Empty<ResourceReservation>(), Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain));
            if (movements.Count != 0 && !_world.Services.Get<IResourceTransaction>().CommitMovements(movements).Committed)
                throw new InvalidOperationException("complete network redistribution changed after successful preflight");
            VerifyTopologyResources(changes);
            var facts = EntityObservation.Capture(_world);
            var resources = ObserveTopologyResources(changes);
            if (geometry != null)
            {
                // Geometry may change, but time, identity, randomness and admitted stock may not.
                geometry();
                facts.Verify();
                _world.RequireHealthyUpdate();
                VerifyTopologyResources(changes);
                foreach (TopologyChange change in changes)
                {
                    TopologyChange actual = BuildTopology(change.State, null);
                    if (!SameTopology(change, actual))
                        throw new InvalidOperationException("network erase did not commit its admitted physical topology");
                }
            }
            // Read provider-owned stamps before the final guard, never during publication.
            foreach (TopologyChange change in changes) change.CommittedVersion = change.State.Version;
            facts.Verify();
            _world.RequireHealthyUpdate();
            VerifyTopologyResources(changes);
            foreach (var resource in resources) resource.Verify();
            foreach (TopologyChange change in changes)
            {
                change.State.Components.Clear();
                foreach (TopologyPart part in change.Parts) change.State.Components.Add(part.Live);
                change.State.ComponentOf = change.Map;
                change.State.ObservedGeometryVersion = change.CommittedVersion.Geometry;
                change.State.ObservedOwnershipVersion = change.CommittedVersion.Ownership;
                change.State.RestoredPendingGeometry = false;
            }
            // Do not clear this on failure: an interrupted commit must never serialize as healthy.
            _committing = null;
        }

        private static void VerifyTopologyResources(List<TopologyChange> changes)
        {
            foreach (TopologyChange change in changes)
            {
                foreach (NetComponent old in change.State.Components)
                {
                    bool retained = false;
                    foreach (TopologyPart part in change.Parts) if (ReferenceEquals(part.Live, old)) retained = true;
                    if (!retained && (old.Pool.Count != 0 || old.Inventory.Reservations.Count != 0))
                        throw new InvalidOperationException("retired network endpoint still owns stock or receipts");
                }
                foreach (TopologyPart part in change.Parts)
                {
                    if (part.Live.Pool.Count != part.Allocation.Count)
                        throw new InvalidOperationException("network allocation differs from committed owner");
                    for (int i = 0; i < part.Allocation.Count; i++)
                        if (part.Live.Pool[i].Resource != part.Allocation[i].Resource || part.Live.Pool[i].Amount != part.Allocation[i].Amount)
                            throw new InvalidOperationException("network allocation differs from committed native stock");
                }
            }
        }

        private static bool SameTopology(TopologyChange expected, TopologyChange actual)
        {
            if (expected.Parts.Count != actual.Parts.Count || expected.Map.Count != actual.Map.Count) return false;
            for (int i = 0; i < expected.Parts.Count; i++)
                if (expected.Parts[i].Owner != actual.Parts[i].Owner) return false;
            foreach (var vertex in expected.Map)
                if (!actual.Map.TryGetValue(vertex.Key, out int part) || part != vertex.Value) return false;
            return true;
        }

        private TopologyChange BuildTopology(NetState state,HashSet<string> removed,IReadOnlyList<NetworkVertex> additions=null)
        {
            // One transient admission read-set, not another persisted topology or provider cache.
            var graph=ReadGeometry(null,()=>{
                var result=new List<NetworkVertex>();_geometry.CopyVertices(_world,state.Def.Line,result);
                var actual=new Dictionary<string,NetworkVertex>(StringComparer.Ordinal);
                var edges=new Dictionary<string,List<string>>(StringComparer.Ordinal);
                foreach(var vertex in result) {
                    if(string.IsNullOrWhiteSpace(vertex.Identity)||actual.ContainsKey(vertex.Identity)||!_geometry.ValidateIdentity(_world,state.Def.Line,vertex.Identity)||
                        (state.Def.Line.Owned?!_world.Players.IsSeat(vertex.Company):vertex.Company!=PlayerId.None))
                        throw new InvalidOperationException("graph provider returned invalid or duplicate actual vertex/company");
                    actual.Add(vertex.Identity,vertex);
                }
                if(additions!=null)foreach(var vertex in additions) {
                    if(actual.ContainsKey(vertex.Identity))throw new InvalidOperationException("proposed graph vertex is already occupied");
                    actual.Add(vertex.Identity,vertex);result.Add(vertex);
                }
                result.Sort((a,b)=>string.CompareOrdinal(a.Identity,b.Identity));
                foreach(var vertex in result) {
                    var neighbours=new List<string>();_geometry.CopyNeighbours(_world,state.Def.Line,vertex.Identity,neighbours);
                    var seen=new HashSet<string>(StringComparer.Ordinal);
                    foreach(string next in neighbours)
                        if(string.IsNullOrWhiteSpace(next)||next==vertex.Identity||!seen.Add(next)||!_geometry.ValidateIdentity(_world,state.Def.Line,next))
                            throw new InvalidOperationException("graph provider returned invalid potential neighbour");
                    neighbours.Sort(StringComparer.Ordinal);edges.Add(vertex.Identity,neighbours);
                }
                // Check even edges into visited vertices, other companies and proposed removals.
                // Physical graph meaning cannot depend on traversal order or the requested mutation.
                foreach(var vertex in result)foreach(string next in edges[vertex.Identity])
                    if(actual.ContainsKey(next)&&!edges[next].Contains(vertex.Identity))
                        throw new InvalidOperationException("network graph requires reciprocal connected edges");
                return (Vertices:result,Actual:actual,Edges:edges);
            });
            var candidate=new TopologyChange{State=state,Map=new Dictionary<string,int>(StringComparer.Ordinal)};
            var stack=new Stack<string>();
            foreach(var vertex in graph.Vertices) {
                string identity=vertex.Identity;
                if(candidate.Map.ContainsKey(identity)||removed!=null&&removed.Contains(identity))continue;
                int index=candidate.Parts.Count;var part=new TopologyPart{Owner=vertex.Company};candidate.Parts.Add(part);
                candidate.Map.Add(identity,index);stack.Push(identity);
                while(stack.Count>0) {
                    string current=stack.Pop();part.Members.Add(current);
                    foreach(string next in graph.Edges[current]) {
                        if(!graph.Actual.TryGetValue(next,out var neighbour)||candidate.Map.ContainsKey(next)||
                            removed!=null&&removed.Contains(next)||neighbour.Company!=vertex.Company)continue;
                        candidate.Map.Add(next,index);stack.Push(next);
                    }
                }
                part.Members.Sort(StringComparer.Ordinal);
                if(state.ComponentOf!=null&&state.ComponentOf.TryGetValue(part.Members[0],out int previous)) {
                    var old=state.Components[previous];bool unchanged=old.Owner==part.Owner&&old.MemberCount==part.Members.Count;
                    foreach(string member in part.Members)if(!state.ComponentOf.TryGetValue(member,out int oldIndex)||oldIndex!=previous)unchanged=false;
                    if(unchanged)part.Live=old;
                }
            }
            return candidate;
        }

        private Code Redistribute(TopologyChange candidate)
            => NetworkDefinitionContracts.Read(_world, candidate.State.Def, null, () => RedistributeCore(candidate));
        private Code RedistributeCore(TopologyChange candidate)
        {
            NetState state = candidate.State; INetworkDefinition network = state.Def;
            foreach (NetComponent component in state.Components) ValidatePool(network, component);
            var retained = new bool[state.Components.Count];
            foreach (TopologyPart part in candidate.Parts)
                if (part.Live != null)
                {
                    part.Allocation.AddRange(part.Live.Pool);
                    retained[state.Components.IndexOf(part.Live)] = true;
                }
            for (int i = 0; i < state.Components.Count; i++)
                if (!retained[i] && state.Components[i].Inventory.Reservations.Count != 0) return ResourceTransactions.ActivePromises;
            if (state.ComponentOf == null) return Code.None;
            var overlaps = new Dictionary<int, int>[state.Components.Count];
            var orders = new List<int>[state.Components.Count];
            var shared = new int[state.Components.Count];
            foreach (KeyValuePair<string, int> tile in candidate.Map)
            {
                if (!state.ComponentOf.TryGetValue(tile.Key, out int oldIndex) || retained[oldIndex] || state.Components[oldIndex].Pool.Count == 0) continue;
                int nextIndex = tile.Value;
                if (state.Components[oldIndex].Owner != candidate.Parts[nextIndex].Owner) return LineCodes.ForeignLine;
                if (overlaps[oldIndex] == null) { overlaps[oldIndex] = new Dictionary<int, int>(); orders[oldIndex] = new List<int>(); }
                if (!overlaps[oldIndex].ContainsKey(nextIndex)) { overlaps[oldIndex][nextIndex] = 0; orders[oldIndex].Add(nextIndex); }
                overlaps[oldIndex][nextIndex]++; shared[oldIndex]++;
            }
            for (int oldIndex = 0; oldIndex < state.Components.Count; oldIndex++)
            {
                if (retained[oldIndex] || state.Components[oldIndex].Pool.Count == 0) continue;
                if (shared[oldIndex] == 0) return OccupiedNetwork;
                List<int> order = orders[oldIndex]; order.Sort();
                int largest = order[0];
                foreach (int index in order) if (overlaps[oldIndex][index] > overlaps[oldIndex][largest]) largest = index;
                foreach (ResourceAmount stock in state.Components[oldIndex].Pool)
                {
                    var capacity = NetworkDefinitionContracts.Read(_world, network, null, () => network.CapacityPolicy);
                    CapacityQuantum quantum = NetworkPolicyContracts.Read(_world, capacity, null, () => CapacityPolicyContracts.Quantum(capacity, stock.Resource));
                    if (quantum.Status != CapacityStatus.Fits || quantum.Raw <= 0 || quantum.Floor(stock.Amount) != stock.Amount)
                        throw new InvalidOperationException("network topology capacity returned an invalid native quantum");
                    Fix remaining = stock.Amount;
                    foreach (int index in order)
                    {
                        if (index == largest) continue;
                        Fix proportional = quantum.Floor(Fix.FromRaw((long)((BigInteger)stock.Amount.Raw * overlaps[oldIndex][index] / shared[oldIndex])));
                        remaining -= Allocate(candidate, oldIndex, index, stock.Resource, proportional);
                    }
                    remaining -= Allocate(candidate, oldIndex, largest, stock.Resource, remaining);
                    foreach (int index in order)
                        if (index != largest && remaining.IsPositive)
                            remaining -= Allocate(candidate, oldIndex, index, stock.Resource, remaining);
                    if (remaining.IsPositive) return NetworkCapacity;
                }
            }
            return Code.None;
        }

        private Fix Allocate(TopologyChange candidate, int from, int to, ResourceId resource, Fix ceiling)
        {
            TopologyPart receiving = candidate.Parts[to];
            var capacity = NetworkDefinitionContracts.Read(_world, candidate.State.Def, null, () => candidate.State.Def.CapacityPolicy);
            CapacityOffer offer = NetworkPolicyContracts.Read(_world, capacity, null, () => CapacityPolicyContracts.Offer(capacity, receiving.Allocation,
                Array.Empty<ResourceAmount>(), resource, ceiling, receiving.Members.Count));
            Fix amount = offer.Amount;
            if (!amount.IsPositive) return Fix.Zero;
            AddAllocation(receiving.Allocation, resource, amount);
            candidate.Legs.Add(new TopologyLeg(from, to, new ResourceAmount(resource, amount)));
            return amount;
        }

        private void ValidatePool(INetworkDefinition network, NetComponent component)
            => NetworkDefinitionContracts.Read(_world, network, null, () => { ValidatePoolCore(network, component); return true; });
        private void ValidatePoolCore(INetworkDefinition network, NetComponent component)
        {
            int previous = 0;
            foreach (ResourceAmount line in component.Pool)
            {
                var capacity = NetworkDefinitionContracts.Read(_world, network, null, () => network.CapacityPolicy);
                CapacityQuantum quantum = NetworkPolicyContracts.Read(_world, capacity, null, () => CapacityPolicyContracts.Quantum(capacity, line.Resource));
                if (!NetworkDefinitionContracts.Read(_world, network, null, () => network.Carries(line.Resource)) || !line.Amount.IsPositive || line.Resource.Value <= previous ||
                    quantum.Status != CapacityStatus.Fits || quantum.Raw <= 0 || quantum.Floor(line.Amount) != line.Amount)
                    throw new CorruptStateException("invalid network native stock: " + network.Id);
                previous = line.Resource.Value;
            }
            var policy = NetworkDefinitionContracts.Read(_world, network, null, () => network.CapacityPolicy);
            if (!NetworkPolicyContracts.Read(_world, policy, null, () => CapacityPolicyContracts.Check(component.Inventory.CapacityPolicy,
                component.Pool, component.Inventory.InboundPromises, Array.Empty<ResourceAmount>())).Fits)
                throw new CorruptStateException("network stock/promises exceed component capacity: " + network.Id);
            foreach (ResourceAmount promise in component.Inventory.OutboundPromises)
                if (promise.Amount > component.Amount(promise.Resource)) throw new CorruptStateException("network pickup promises exceed real stock");
        }

        // Candidate allocation only. Live pools never use list mutation or a second balance path.
        private static void AddAllocation(List<ResourceAmount> allocation, ResourceId resource, Fix amount)
        {
            if (!amount.IsPositive) return;
            for (int i = 0; i < allocation.Count; i++)
                if (allocation[i].Resource == resource)
                { allocation[i] = new ResourceAmount(resource, allocation[i].Amount + amount); return; }
            int at = 0; while (at < allocation.Count && allocation[at].Resource.Value < resource.Value) at++;
            allocation.Insert(at, new ResourceAmount(resource, amount));
        }

        private bool Transfer(IResourceOutlet source, IResourceInlet destination, PlayerId company,
            ResourceId resource, Fix amount, ReservationOwner owner)
        {
            var intent = new ResourceMovementIntent(owner, company, source.Endpoint, destination.Endpoint,
                new Bill(new[] { new ResourceAmount(resource, amount) }), Array.Empty<ResourceReservation>(), Array.Empty<ResourceReservation>(),
                Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain);
            return _world.Services.Get<IResourceTransaction>().CommitMovement(intent).Committed;
        }

        public void Sync(IStateSync sync)
        {
            bool observation = sync is IStateObservationSync probe && probe.IsObservation && !sync.Reading;
            if (_committing != null && !observation) throw new InvalidOperationException("cannot checkpoint an interrupted network topology commit");
            if (_flowOperation.Active && !observation) throw new InvalidOperationException("cannot checkpoint an interrupted network flow update");
            // A lookup may observe redistribution before geometry/publication. Fingerprint its
            // actual staged pools through their ORIGINAL codecs; never make them checkpointable.
            if (observation)
            {
                bool flowing = _flowOperation.Active; sync.Bool(ref flowing);
                int changes = _committing?.Count ?? -1; sync.Int(ref changes);
                if (_committing != null)
                    foreach (TopologyChange change in _committing)
                    {
                        string network = change.State.Def.Id; sync.String(ref network);
                        sync.Count(change.Parts.Count);
                        foreach (TopologyPart part in change.Parts)
                        {
                            PlayerId owner = part.Owner; sync.Player(ref owner);
                            sync.Count(part.Members.Count);
                            foreach (string member in part.Members) { string value = member; sync.String(ref value); }
                            bool live = part.Live != null; sync.Bool(ref live);
                            if (!live) continue;
                            long identity = part.Live.Endpoint.Identity; int version = part.Live.Version;
                            sync.Long(ref identity); sync.Int(ref version);
                            part.Live.Inventory.Sync(sync);
                            SyncFlowTurns(sync, change.State.Def, part.Live);
                        }
                    }
            }
            sync.Long(ref _nextEndpoint);
            if (_nextEndpoint <= 0) throw new CorruptStateException("invalid next network endpoint identity");
            if (sync.Count(_netList.Count) != _netList.Count) throw new CorruptStateException("network count differs from the save");
            var identities = new HashSet<long>();
            foreach (NetState state in _netList)
            {
                string id = state.Def.Id; sync.String(ref id);
                if (id != state.Def.Id) throw new CorruptStateException("network identity differs from selected content");
                bool dirty = state.Dirty; sync.Bool(ref dirty);
                if (sync.Reading)
                {
                    state.RestoredPendingGeometry = dirty;
                    state.ObservedGeometryVersion = state.Version.Geometry;
                    state.ObservedOwnershipVersion = state.Version.Ownership;
                }
                // Composition legitimately precedes OnNewGame topology initialization. Hash the actual
                // lifecycle state for pure observation; it is never an empty fabricated live graph.
                bool initialized = state.ComponentOf != null; sync.Bool(ref initialized);
                if (!initialized)
                {
                    if (sync.Mode != SyncMode.Hash) throw new CorruptStateException("uninitialized network is not a stable checkpoint");
                    if (state.Components.Count != 0 || state.Flow.Count != 0)
                        throw new InvalidOperationException("uninitialized network retains actual components or flow");
                    continue;
                }
                int count = sync.Count(state.Components.Count);
                if (sync.Reading) { state.Components.Clear(); state.ComponentOf = new Dictionary<string, int>(StringComparer.Ordinal); }
                for (int i = 0; i < count; i++)
                {
                    NetComponent component = sync.Reading ? null : state.Components[i];
                    long identity = component?.Endpoint.Identity ?? 0;
                    int version = component?.Version ?? 0;
                    PlayerId owner = component?.Owner ?? PlayerId.None;
                    sync.Long(ref identity); sync.Int(ref version); sync.Player(ref owner);
                    if (identity <= 0 || identity >= _nextEndpoint || !identities.Add(identity) || version < 0 ||
                        state.Def.Line.Owned && !_world.Players.IsSeat(owner) || !state.Def.Line.Owned && owner != PlayerId.None)
                        throw new CorruptStateException("invalid network endpoint identity/version/owner");
                    int members = sync.Count(component?.MemberCount ?? 0);
                    if (members <= 0) throw new CorruptStateException("network component requires tile membership");
                    var loadedMembers = sync.Reading ? new List<string>() : null;
                    string previous = null;
                    for (int m = 0; m < members; m++)
                    {
                        string tile = sync.Reading ? null : component.Members[m]; sync.String(ref tile);
                        if (!ReadGeometry(null,()=>_geometry.ValidateIdentity(_world,state.Def.Line,tile)) || m > 0 && string.CompareOrdinal(previous,tile) >= 0)
                            throw new CorruptStateException("invalid network component tile order");
                        if (sync.Reading)
                        {
                            if (state.ComponentOf.ContainsKey(tile)) throw new CorruptStateException("duplicate network component tile");
                            state.ComponentOf.Add(tile, i); loadedMembers.Add(tile);
                        }
                        previous = tile;
                    }
                    if (sync.Reading)
                    {
                        component = new NetComponent(_world, state.Def, new ResourceEndpointId(EndpointScope, identity), owner, loadedMembers)
                        { _version = version };
                        state.Components.Add(component);
                    }
                    component.Inventory.Sync(sync); ValidatePool(state.Def, component);
                    SyncFlowTurns(sync, state.Def, component);
                }
                ValidateGraph(state);
                SyncFlow(sync, state);
            }
        }

        private void ValidateGraph(NetState state)
        {
            int members = 0; string previous = null;
            for (int i = 0; i < state.Components.Count; i++)
            {
                NetComponent component = state.Components[i];
                if (i > 0 && string.CompareOrdinal(previous, component.Members[0]) >= 0)
                    throw new CorruptStateException("invalid network component order");
                previous = component.Members[0]; members = checked(members + component.MemberCount);
                var visited = new HashSet<string>(StringComparer.Ordinal); var pending = new Stack<string>();
                pending.Push(component.Members[0]); visited.Add(component.Members[0]);
                while (pending.Count > 0)
                {
                    string at = pending.Pop();
                    foreach(string next in Neighbours(state.Def.Line,at))
                        if(state.ComponentOf.TryGetValue(next,out int index)&&index==i&&visited.Add(next))pending.Push(next);
                }
                if (visited.Count != component.MemberCount) throw new CorruptStateException("disconnected saved network component");
                foreach (string tile in component.Members)
                    if (!state.ComponentOf.TryGetValue(tile, out int index) || index != i)
                        throw new CorruptStateException("network membership index differs");
            }
            if (members != state.ComponentOf.Count) throw new CorruptStateException("network membership count differs");
            if (!state.Dirty)
            {
                TopologyChange actual = BuildTopology(state, null);
                if (actual.Parts.Count != state.Components.Count) throw new CorruptStateException("settled network graph differs from current geometry");
                for (int i = 0; i < actual.Parts.Count; i++)
                    if (!ReferenceEquals(actual.Parts[i].Live, state.Components[i]))
                        throw new CorruptStateException("settled network ownership/membership differs from current geometry");
            }
        }
    }

    /// <summary>Cleans ended markers, maintains topology, and advances physical flow each Update.</summary>
    public sealed class LineSystem : GameSystem
    {
        public override int StateSchemaVersion => 1;
        public override string Id => "sim.lines";
        public override void OnNewGame(World world) { if (world.Services.TryGet<ILineSimulation>(out var topology)) topology.RebuildDirty(); }
        public override void OnLoaded(World world) => world.Services.Get<ILineConstruction>().ValidateMarkers();
        public override void Update(World world, TickInfo tick)
        {
            world.Services.Get<ILineConstruction>().Advance();
            if (world.Services.TryGet<ILineSimulation>(out var topology)) { topology.RebuildDirty(); topology.Flow(); }
        }
    }



    internal sealed class LayLineHandler : CommandHandler<LayLineCommand>
    {
        private static PlanIntent Intent(World world, ILineDefinition line, LayLineCommand command)
            => LineDefinitionContracts.Read(world, line, null, () =>
                line.CreateIntent(TilePlacement.PathArguments(line.Layer.Space.Id, command.Tiles), null, 45, false));

        protected override Code Check(World world, LayLineCommand c)
        {
            ILineDefinition definition = world.Content.Find<ILineDefinition>(c.Line);
            return definition == null ? Codes.UnknownDef : world.Services.Get<IPlanBoard>().PreviewPlan(c.Player, Intent(world, definition, c));
        }

        protected override void Apply(World world, LayLineCommand c, CommandContext ctx)
        {
            var line = world.Content.Get<ILineDefinition>(c.Line);
            ctx.Arg0 = world.Services.Get<IPlanBoard>().SubmitPlan(c.Player, Intent(world, line, c));
        }
    }
}
