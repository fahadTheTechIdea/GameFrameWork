using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Immutable physical corridor metadata and its original engine-owned plan/template binding.</summary>
    public interface ILineDefinition : IContentDefinition, IPlanTemplateDefinition, IActivityWorkflowBinding
    {
        string LayerId { get; }
        string SegmentNode { get; }
        Bill SupplyCapacity { get; }
        EntityDef WorkMarker { get; }
        ISpatialLayerDefinition Layer { get; }
        ISpatialLayerDefinition OwnerLayer { get; }
        Fix Value { get; }
        bool Owned { get; }
        ILinePlacementPolicy PlacementPolicy { get; }
        string SegmentEffect { get; }
        string FinishEffect { get; }
        PlanIntent CreateIntent(JsonValue path, string group, int priority, bool paused);
    }

    public sealed partial class LineDeclaration
    {
        private readonly ILineDefinition _owner;
        private LineDeclaration(ILineDefinition owner) { _owner = owner ?? throw new ArgumentNullException(nameof(owner)); }
        public static LineDeclaration Read(JsonReader reader, ILineDefinition owner)
        {
            var declaration = new LineDeclaration(owner);
            declaration.ReadCore(reader ?? throw new ArgumentNullException(nameof(reader)));
            return declaration;
        }
        public bool AutomaticSupply => Workflow.AutomaticSupply;
        public IReadOnlyList<IContentDefinition> DisclosureContent => OwnerLayer == null
            ? Array.AsReadOnly(new IContentDefinition[] { _owner, Layer, PlacementPolicy, WorkMarker, (IContentDefinition)Workflow })
            : Array.AsReadOnly(new IContentDefinition[] { _owner, Layer, OwnerLayer, PlacementPolicy, WorkMarker, (IContentDefinition)Workflow });
        private string _activity;
        public IActivityDefinition Workflow { get; private set; }
        public string SegmentNode { get; private set; }
        public Bill SupplyCapacity { get; private set; }
        private string _layerId;
        public string LayerId => _layerId;
        private string _workMarker;
        public EntityDef WorkMarker { get; private set; }
        private string _ownerLayerId;
        public ISpatialLayerDefinition Layer { get; private set; }
        public ISpatialLayerDefinition OwnerLayer { get; private set; }
        public Fix Value { get; private set; }
        /// <summary>Owned lines connect only to their owner's structures and never cross another owner's.</summary>
        public bool Owned { get; private set; }
        private string _placementPolicy;
        public ILinePlacementPolicy PlacementPolicy { get; private set; }
        public string SegmentEffect => Workflow.Graph.Find(SegmentNode).Effect;
        public string FinishEffect { get; private set; }

        private void ReadCore(JsonReader r)
        {
            _activity = r.String("activity");
            SegmentNode = r.String("segment_node");
            _workMarker = r.String("work_marker");
            _layerId = r.String("layer");
            _ownerLayerId = r.String("owner_layer", null);
            Value = r.Fix("value");
            if (!Value.IsPositive) throw r.Error("explicit positive line value required");
            Owned = r.Bool("owned", true);
            _placementPolicy = r.String("placement_policy");
            if (Owned && string.IsNullOrWhiteSpace(_ownerLayerId)) throw r.Error("owned lines require an explicit owner_layer spatial layer");
            if (!Owned && !string.IsNullOrEmpty(_ownerLayerId)) throw r.Error("owner_layer is only valid when owned is true");
            FinishEffect = r.String("finish_effect");
            try
            {
                foreach (string id in new[] { _activity, SegmentNode, _workMarker, _layerId, FinishEffect, _placementPolicy })
                    ContractIdentity.ValidateId(id, "line reference");
                if (Owned) ContractIdentity.ValidateId(_ownerLayerId, "line owner layer");
            }
            catch (ArgumentException error) { throw r.Error(error.Message); }
        }

        public void Resolve(ContentResolver c)
        {
            if (Workflow != null) throw new InvalidOperationException("line metadata resolves once");
            Layer = c.Def<ISpatialLayerDefinition>(_layerId);
            if (Owned)
            {
                OwnerLayer = c.Def<ISpatialLayerDefinition>(_ownerLayerId);
                if (OwnerLayer.ValueType != SpatialLayerValueType.Int32) throw c.Fail("line owner layer '" + OwnerLayer.Id + "' must use int values");
                if (!ReferenceEquals(Layer.Space, OwnerLayer.Space)) throw c.Fail("line and owner layers must belong to the same space");
            }
            Workflow = c.Def<IAuthoredActivityDefinition>(_activity);
            PlacementPolicy = c.Def<ILinePlacementPolicy>(_placementPolicy);
            WorkMarker = c.Def<EntityDef>(_workMarker);
            if (c.Def<ILineEffectDefinition>(FinishEffect).Operation != LineEffectOperation.Finish)
                throw c.Fail("line final effect must declare the finish operation");
        }

        public void ValidateResolved(ContentResolver c)
        {
            SupplyCapacity = WorkBoard.SupplyCapacity(Workflow.Graph, c.Content);
            LineDefinitionContracts.ValidateMetadata(_owner, c);
            _prototype = new LinePlan(_owner, 1);
        }
    }

    /// <summary>Actual authored aliases and segment rules; no alternate builder, catalog or progress state.</summary>
    public static class LineDefinitionContracts
    {
        private static void RequireAlias<T>(T definition, ContentResolver c) where T : class, IContentDefinition
        {
            if (!(definition is Def actual) || string.IsNullOrWhiteSpace(actual.Id) || string.IsNullOrWhiteSpace(actual.Module) ||
                !c.Content.Owns(definition) || definition.Id != actual.Id || definition.Module != actual.Module ||
                !ReferenceEquals(c.Content.Find<T>(actual.Id), definition))
                throw c.Fail("line requires its actual catalog " + typeof(T).Name + " alias");
        }
        public static void ValidateMetadata(ILineDefinition line, ContentResolver c)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            RequireAlias(line, c);
            foreach (string id in new[] { line.LayerId, line.SegmentNode, line.FinishEffect })
                try { ContractIdentity.ValidateId(id, "line reference"); }
                catch (ArgumentException error) { throw c.Fail(error.Message); }
            RequireAlias(line.Layer, c);
            if (line.Layer.ResourceMeasure == SpatialResourceMeasure.Quantity)
                throw c.Fail("line topology cannot overwrite a resource-quantity layer");
            if (line.Layer.Id != line.LayerId || !line.Value.IsPositive ||
                line.Layer.ValueType == SpatialLayerValueType.Byte &&
                    (line.Value.Raw % Fix.One.Raw != 0 || line.Value.Raw / Fix.One.Raw > byte.MaxValue) ||
                line.Layer.ValueType == SpatialLayerValueType.Int32 &&
                    (line.Value.Raw % Fix.One.Raw != 0 || line.Value.Raw / Fix.One.Raw > int.MaxValue))
                throw c.Fail("line requires its actual layer and representable positive value");
            LinePlacementContracts.ValidateMetadata(line.PlacementPolicy, line.Layer.Space, c);
            if (line.Owned)
            {
                RequireAlias(line.OwnerLayer, c);
                if (line.OwnerLayer.ResourceMeasure == SpatialResourceMeasure.Quantity)
                    throw c.Fail("line ownership cannot overwrite a resource-quantity layer");
                if (line.OwnerLayer.ValueType != SpatialLayerValueType.Int32 || !ReferenceEquals(line.Layer.Space, line.OwnerLayer.Space))
                    throw c.Fail("owned line requires an actual integer owner layer in the same space");
            }
            else if (line.OwnerLayer != null) throw c.Fail("unowned line cannot declare an owner layer");
            if (!(line.Workflow is IAuthoredActivityDefinition profile)) throw c.Fail("line requires an actual authored workflow");
            RequireAlias(profile, c);
            if (line.AutomaticSupply != profile.AutomaticSupply || line.PlanKey != "line:" + line.Id ||
                line.OnComplete != line.FinishEffect)
                throw c.Fail("line plan identity, final effect and supply mode must match its declaration");
            if (c.Def<ILineEffectDefinition>(line.FinishEffect).Operation != LineEffectOperation.Finish)
                throw c.Fail("line final effect must declare the finish operation");
            if (profile.PlayerOrderable || profile.TargetTags.Count != 0 || profile.OnComplete != null)
                throw c.Fail("bound segment activity must be non-orderable, unrestricted and have no standalone final effect");
            IActivityNodeDefinition terminal = line.Workflow.Graph.Find(line.SegmentNode);
            if (terminal == null || !line.Workflow.Graph.HasMachineWork(line.SegmentNode))
                throw c.Fail("segment_node requires authored physical machine work in its dependency ancestry");
            if (c.Def<ILineEffectDefinition>(terminal.Effect).Operation != LineEffectOperation.Segment)
                throw c.Fail("segment_node must declare the segment stamping effect");
            var ancestors = new HashSet<string>(StringComparer.Ordinal);
            var pending = new Stack<string>(); pending.Push(line.SegmentNode);
            while (pending.Count > 0)
            {
                string id = pending.Pop(); if (!ancestors.Add(id)) continue;
                foreach (string dependency in line.Workflow.Graph.Find(id).Dependencies) pending.Push(dependency);
            }
            if (ancestors.Count != line.Workflow.Graph.Nodes.Count)
                throw c.Fail("every segment node must precede segment_node");
            foreach (IActivityNodeDefinition node in line.Workflow.Graph.Nodes)
                if (node.Id != line.SegmentNode && c.Content.Find<IWorkEffect>(node.Effect) is ILineEffectDefinition)
                    throw c.Fail("only segment_node may alter the native line cursor");
            Bill envelope = WorkBoard.SupplyCapacity(line.Workflow.Graph, c.Content);
            if (line.SupplyCapacity == null || line.SupplyCapacity.Lines.Count != envelope.Lines.Count)
                throw c.Fail("line supply capacity differs from its authored material envelope");
            for (int i = 0; i < envelope.Lines.Count; i++)
                if (!line.SupplyCapacity.Lines[i].Equals(envelope.Lines[i]))
                    throw c.Fail("line supply capacity differs from its authored material envelope");
            if (line.SegmentEffect != terminal.Effect) throw c.Fail("line segment effect differs from its terminal");
            RequireAlias(line.WorkMarker, c);
            ILineWorkDefinition cursor = line.WorkMarker.Component<ILineWorkDefinition>();
            if (line.WorkMarker.Kind != EntityKind.Marker || cursor == null || !line.WorkMarker.HasCapability<ILineWork>())
                throw c.Fail("work_marker requires an authored marker with an actual line-work capability");
            cursor.ValidateBinding(line.WorkMarker);
            ISpatialPresenceDefinition presence = line.WorkMarker.Component<ISpatialPresenceDefinition>();
            IInventoryDefinition inventory = line.WorkMarker.Component<IInventoryDefinition>();
            if (presence.Space != line.Layer.Space.Id)
                throw c.Fail("work_marker requires its actual line space");
            if (inventory.InitialStock == null || !inventory.InitialStock.IsEmpty || inventory.CapacityPolicy == null ||
                !CapacityPolicyContracts.Check(inventory.CapacityPolicy, Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>(), line.SupplyCapacity.Lines).Fits)
                throw c.Fail("work_marker inventory must begin empty and hold the authored segment material envelope");
        }
        public static void Validate(ILineDefinition line, ContentResolver content)
        {
            ValidateMetadata(line, content);
            IActivityGraph graph = line.Graph;
            if (graph == null || graph.Nodes.Count != line.Workflow.Graph.Nodes.Count)
                throw content.Fail("line prototype requires its complete one-segment graph");
            foreach (var source in line.Workflow.Graph.Nodes)
            {
                var bound = graph.Find(LinePlan.NodeId(0, source.Id));
                if (bound == null || !ReferenceEquals(bound.Executor, source.Executor) || !ReferenceEquals(bound.Requirement, source.Requirement) ||
                    !bound.Duration.Equals(source.Duration) || bound.RequirementMultiplier != source.RequirementMultiplier ||
                    bound.Effect != source.Effect || bound.Roles.Count != source.Roles.Count || bound.Dependencies.Count != source.Dependencies.Count)
                    throw content.Fail("line prototype must preserve its actual authored node metadata");
                for (int i = 0; i < source.Roles.Count; i++)
                    if (bound.Roles[i] != source.Roles[i]) throw content.Fail("line prototype changes an authored role");
                for (int i = 0; i < source.Dependencies.Count; i++)
                    if (bound.Dependencies[i] != LinePlan.NodeId(0, source.Dependencies[i]))
                        throw content.Fail("line prototype changes an authored dependency");
            }
        }
        internal static T Read<T>(World world, ILineDefinition line, Entity entity, Func<T> read)
            => NetworkPolicyContracts.Read(world, line, entity, read, observeTargetState: true);
        internal static void ValidateComposition(World world)
        {
            foreach (var line in world.Content.All<ILineDefinition>())
                Read(world, line, null, () => { Validate(line, new ContentResolver(world.Content, (Def)line)); return true; });
        }
    }
}
