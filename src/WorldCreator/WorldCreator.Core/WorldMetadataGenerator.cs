using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using WorldCreator.Core.Planning;

namespace WorldCreator.Core.Metadata;

/// <summary>Expands reusable game-defined programs into world metadata. No image, biome,
/// asset registry, raster, renderer or game engine is required. No prompt text is executed.</summary>
public sealed partial class WorldMetadataGenerator(ISettlementCatalogRegistry? structureCatalogs = null) : IWorldMetadataGenerator
{
    public WorldMetadataDocument Generate(WorldMetadataRecipe recipe, WorldMetadataCatalog catalog, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        var types = Validate(recipe, catalog, token);
        var nodes = new List<WorldMetadataNode>(); var connections = new List<WorldMetadataConnection>();
        var structures = new SortedDictionary<string, MetadataStructureDefinition>(StringComparer.Ordinal); long propertyBytes=0;
        Visit(recipe.RootArchetypeId, recipe.Id, null, types[recipe.RootArchetypeId].Purpose, recipe.Origin?.ToArray(), 0);
        var nodeIds=nodes.Select(n=>n.Id).ToHashSet(StringComparer.Ordinal);var explicitIds=new HashSet<string>(StringComparer.Ordinal);
        foreach(var link in recipe.AdditionalConnections ?? [])
        {
            token.ThrowIfCancellationRequested();
            if(link is null || !SettlementCatalogRegistry.ValidId(link.Id) || !explicitIds.Add(link.Id) ||
                !SettlementCatalogRegistry.ValidId(link.Kind) || !nodeIds.Contains(link.FromId) || !nodeIds.Contains(link.ToId) || link.FromId==link.ToId)
                throw new ArgumentException("METADATA_EXPLICIT_CONNECTION_INVALID");
            if(connections.Count>=recipe.MaximumConnections)throw new ArgumentException("METADATA_CONNECTION_BUDGET");
            connections.Add(new(recipe.Id+"/explicit/"+link.Id,link.Kind,link.FromId,link.ToId,link.Bidirectional,"recipe:"+link.Id,Properties(link.Properties)));
        }
        return new(1, recipe.Id, recipe.GameProfile, recipe.Scale, recipe.Coverage, recipe.RemainingScopes.ToArray(),
            new(recipe.Space.Axes.ToArray(),recipe.Space.Unit), Digest(recipe), Digest(catalog), nodes.AsReadOnly(), connections.AsReadOnly(),
            new ReadOnlyDictionary<string,MetadataStructureDefinition>(structures));

        void Visit(string typeId, string id, string? parentId, string purpose, double[]? position, int depth)
        {
            token.ThrowIfCancellationRequested();
            if (depth > 32 || id.Length>2048 || nodes.Count >= recipe.MaximumNodes) throw new ArgumentException("METADATA_NODE_OR_DEPTH_BUDGET");
            if (recipe.RequireAllPositions && position is null) throw new ArgumentException("METADATA_POSITION_REQUIRED:" + id);
            var type = types[typeId]; string? structureKey = null;
            if (type.Structure is { } reference)
            {
                if (structureCatalogs is null) throw new ArgumentException("METADATA_STRUCTURE_CATALOG_REQUIRED");
                var pack = structureCatalogs.Resolve(reference.Catalog);
                var definition = pack.Structures.SingleOrDefault(d=>d.Id==reference.DefinitionId) ?? throw new ArgumentException("METADATA_STRUCTURE_UNKNOWN");
                var variant = definition.SizeVariants?.SingleOrDefault(v=>v.Id==reference.VariantId) ?? throw new ArgumentException("METADATA_STRUCTURE_VARIANT_UNKNOWN");
                structureKey=pack.Descriptor.Id+"/"+definition.Id+"/"+variant.Id;
                structures.TryAdd(structureKey,new(pack.Provenance,definition,variant));
            }
            nodes.Add(new(id,type.Id,type.Kind,parentId,purpose,Properties(type.Properties),position,structureKey));
            var slots = new Dictionary<string,List<string>>(StringComparer.Ordinal) { ["self"] = [id] };
            var childKinds = new List<string>();
            foreach (var part in type.Parts.OrderBy(p=>p.Id,StringComparer.Ordinal))
            {
                token.ThrowIfCancellationRequested();
                int count=part.CountParameter is { } parameter ? recipe.Parameters.TryGetValue(parameter,out int n) ? n :
                    throw new ArgumentException("METADATA_PARAMETER_MISSING:"+parameter) : part.Count;
                if(count<0 || count>recipe.MaximumNodes || (long)nodes.Count+count>recipe.MaximumNodes)
                    throw new ArgumentException("METADATA_PART_COUNT_OR_NODE_BUDGET");
                var members = new List<string>();slots.Add(part.Id,members);
                for(int i=0;i<count;i++)
                {
                    string child=id+"/"+part.Id+"-"+i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    double[]? childPosition=null;
                    if(part.Layout is { } layout)
                    {
                        if(position is null) throw new ArgumentException("METADATA_PARENT_POSITION_REQUIRED:"+id);
                        childPosition=position.Select((p,axis)=>p+layout.Origin[axis]+i*layout.Step[axis]).ToArray();
                        if(childPosition.Any(p=>!double.IsFinite(p)))throw new ArgumentException("METADATA_POSITION_NONFINITE");
                    }
                    members.Add(child);childKinds.Add(types[part.ArchetypeId].Kind);
                    Visit(part.ArchetypeId,child,id,part.Purpose,childPosition,depth+1);
                }
            }
            foreach(var requirement in type.ChildRequirements)
            {
                int count=childKinds.Count(k=>k==requirement.Kind);
                if(count<requirement.Minimum || count>requirement.Maximum)
                    throw new ArgumentException("METADATA_CHILD_REQUIREMENT:"+id+":"+requirement.Kind);
            }
            var localEdges=new List<(string From,string To)>();
            foreach(var rule in type.Connections.OrderBy(r=>r.Id,StringComparer.Ordinal))
            {
                var from=slots[rule.FromPart]; var to=rule.ToPart is null ? [] : slots[rule.ToPart]; int index=0;
                switch(rule.Pattern)
                {
                    case MetadataConnectionPattern.Chain:
                        for(int i=1;i<from.Count;i++)Add(from[i-1],from[i]);break;
                    case MetadataConnectionPattern.Hub:
                        if(to.Count==0)break;
                        if(from.Count!=1)throw new ArgumentException("METADATA_HUB_REQUIRES_ONE_SOURCE:"+id);
                        foreach(var target in to)Add(from[0],target);break;
                    case MetadataConnectionPattern.Pairs:
                        if(from.Count!=to.Count)throw new ArgumentException("METADATA_PAIR_COUNT_MISMATCH:"+id);
                        for(int i=0;i<from.Count;i++)Add(from[i],to[i]);break;
                    case MetadataConnectionPattern.AllPairs:
                        if((long)from.Count*to.Count>recipe.MaximumConnections-connections.Count)throw new ArgumentException("METADATA_CONNECTION_BUDGET");
                        foreach(var a in from)foreach(var b in to)Add(a,b);break;
                    default:throw new ArgumentException("METADATA_CONNECTION_PATTERN_INVALID");
                }
                void Add(string a,string b)
                {
                    token.ThrowIfCancellationRequested();
                    if(a==b)throw new ArgumentException("METADATA_CONNECTION_SELF_LINK");
                    if(connections.Count>=recipe.MaximumConnections)throw new ArgumentException("METADATA_CONNECTION_BUDGET");
                    connections.Add(new(id+"/link-"+rule.Id+"-"+index++,rule.Kind,a,b,rule.Bidirectional,type.Id+":"+rule.Id,Properties(rule.Properties)));
                    localEdges.Add((a,b));
                }
            }
            if(type.RequireWeaklyConnectedChildren && childKinds.Count>1)
            {
                var children=slots.Where(s=>s.Key!="self").SelectMany(s=>s.Value).ToArray();
                var seen=new HashSet<string>(StringComparer.Ordinal);var queue=new Queue<string>();queue.Enqueue(children[0]);
                var graph=new Dictionary<string,List<string>>(StringComparer.Ordinal);
                foreach(var edge in localEdges)
                {
                    if(!graph.TryGetValue(edge.From,out var a))graph[edge.From]=a=[];a.Add(edge.To);
                    if(!graph.TryGetValue(edge.To,out var b))graph[edge.To]=b=[];b.Add(edge.From);
                }
                while(queue.TryDequeue(out string? next))
                {token.ThrowIfCancellationRequested();if(!seen.Add(next))continue;if(graph.TryGetValue(next,out var linked))foreach(var neighbor in linked)queue.Enqueue(neighbor);}
                if(children.Any(c=>!seen.Contains(c)))throw new ArgumentException("METADATA_CHILDREN_DISCONNECTED:"+id);
            }
        }
        IReadOnlyDictionary<string,JsonElement> Properties(IReadOnlyDictionary<string,JsonElement>? values)
        {
            if(values is null)return new ReadOnlyDictionary<string,JsonElement>(new Dictionary<string,JsonElement>());
            ValidateProperties(values);
            propertyBytes+=JsonSerializer.SerializeToUtf8Bytes(values).Length;
            if(propertyBytes>67108864)throw new ArgumentException("METADATA_EXPANDED_PROPERTIES_BUDGET");
            var snapshot=new SortedDictionary<string,JsonElement>(StringComparer.Ordinal);
            foreach(var value in values)snapshot.Add(value.Key,value.Value.Clone());
            return new ReadOnlyDictionary<string,JsonElement>(snapshot);
        }
    }

    private static Dictionary<string,WorldMetadataArchetype> Validate(WorldMetadataRecipe r,WorldMetadataCatalog c,CancellationToken token)
    {
        bool Id(string? id)=>SettlementCatalogRegistry.ValidId(id) && !id!.Contains('/');
        if(r is null || c is null || r.SchemaVersion!=1 || c.SchemaVersion!=1 || !Id(r.Id) || !Id(r.GameProfile) || !Id(r.Scale) ||
            !Id(c.Id) || r.CatalogId!=c.Id || !Id(r.RootArchetypeId) || !Enum.IsDefined(r.Coverage) ||
            r.RemainingScopes is null || r.RemainingScopes.Count>512 || r.RemainingScopes.Any(s=>string.IsNullOrWhiteSpace(s) || s.Length>2048) ||
            (r.Coverage==MetadataCoverage.PartialScope ? r.RemainingScopes.Count==0 : r.RemainingScopes.Count!=0) ||
            r.Parameters is null || r.Parameters.Count>256 || r.Parameters.Any(p=>!Id(p.Key) || p.Value<0 || p.Value>100000) ||
            r.Space is null || r.Space.Axes is null || r.Space.Axes.Count>8 || r.Space.Axes.Any(a=>!Id(a)) ||
            r.Space.Axes.Distinct(StringComparer.Ordinal).Count()!=r.Space.Axes.Count || !Id(r.Space.Unit) ||
            r.MaximumNodes is < 1 or > 100000 || r.MaximumConnections is < 0 or > 500000 ||
            r.AdditionalConnections?.Count>50000 ||
            c.Archetypes is null || c.Archetypes.Count is < 1 or > 1000)
            throw new ArgumentException("METADATA_RECIPE_OR_CATALOG_INVALID");
        bool Vector(IReadOnlyList<double>? v)=>v is not null && r.Space.Axes.Count>0 && v.Count==r.Space.Axes.Count && v.All(double.IsFinite);
        if(r.Origin is not null && !Vector(r.Origin) || r.RequireAllPositions && r.Origin is null)
            throw new ArgumentException("METADATA_ORIGIN_INVALID");
        var types=new Dictionary<string,WorldMetadataArchetype>(StringComparer.Ordinal);int parts=0;
        foreach(var a in c.Archetypes)
        {
            token.ThrowIfCancellationRequested();
            if(a is null || !Id(a.Id) || !types.TryAdd(a.Id,a) || !Id(a.Kind) || string.IsNullOrWhiteSpace(a.Purpose) || a.Purpose.Length>2048 ||
                a.Properties is null || a.Properties.Count>256 || a.Properties.Any(p=>string.IsNullOrWhiteSpace(p.Key) || p.Key.Length>160 || p.Value.ValueKind==JsonValueKind.Undefined) ||
                JsonSerializer.SerializeToUtf8Bytes(a.Properties).Length>65536 || a.Parts is null || a.Parts.Count>256 ||
                a.Connections is null || a.Connections.Count>256 || a.ChildRequirements is null || a.ChildRequirements.Count>256)
                throw new ArgumentException("METADATA_ARCHETYPE_INVALID");
            if(a.Structure is { } reference && (reference.Catalog is null || !Id(reference.Catalog.CatalogId) ||
                reference.Catalog.EnabledCapabilities is null || !Id(reference.DefinitionId) || !Id(reference.VariantId)))
                throw new ArgumentException("METADATA_STRUCTURE_REFERENCE_INVALID");
            parts+=a.Parts.Count;if(parts>10000)throw new ArgumentException("METADATA_CATALOG_PART_BUDGET");
            var ids=new HashSet<string>(StringComparer.Ordinal){"self"};
            foreach(var p in a.Parts)
                if(p is null || !Id(p.Id) || !ids.Add(p.Id) || !Id(p.ArchetypeId) || string.IsNullOrWhiteSpace(p.Purpose) || p.Purpose.Length>2048 || p.Count<0 || p.Count>100000 ||
                    p.CountParameter is { } parameter && !Id(parameter) || p.Layout is { } l && (!Vector(l.Origin) || !Vector(l.Step)))
                    throw new ArgumentException("METADATA_PART_INVALID");
            var ruleIds=new HashSet<string>(StringComparer.Ordinal);
            foreach(var l in a.Connections)
                if(l is null || !Id(l.Id) || !ruleIds.Add(l.Id) || !Id(l.Kind) || !Enum.IsDefined(l.Pattern) || !ids.Contains(l.FromPart) ||
                    (l.Pattern==MetadataConnectionPattern.Chain ? l.ToPart is not null : l.ToPart is null || !ids.Contains(l.ToPart)))
                    throw new ArgumentException("METADATA_CONNECTION_RULE_INVALID");
            foreach(var rule in a.Connections)if(rule.Properties is not null)ValidateProperties(rule.Properties);
            var requirements=new HashSet<string>(StringComparer.Ordinal);
            foreach(var q in a.ChildRequirements)
                if(q is null || !Id(q.Kind) || !requirements.Add(q.Kind) || q.Minimum<0 || q.Maximum<q.Minimum || q.Maximum>100000)
                    throw new ArgumentException("METADATA_CHILD_REQUIREMENT_INVALID");
        }
        if(!types.ContainsKey(r.RootArchetypeId))throw new ArgumentException("METADATA_ROOT_UNKNOWN");
        var active=new HashSet<string>(StringComparer.Ordinal);var done=new HashSet<string>(StringComparer.Ordinal);
        foreach(var id in types.Keys.Order(StringComparer.Ordinal))Visit(id,0);
        void Visit(string id,int depth)
        {
            token.ThrowIfCancellationRequested();
            if(!types.ContainsKey(id))throw new ArgumentException("METADATA_ARCHETYPE_UNKNOWN:"+id);
            if(done.Contains(id))return;
            if(depth>32 || !active.Add(id))throw new ArgumentException("METADATA_ARCHETYPE_CYCLE_OR_DEPTH");
            foreach(var part in types[id].Parts)Visit(part.ArchetypeId,depth+1);
            active.Remove(id);done.Add(id);
        }
        return types;
    }
    private static void ValidateProperties(IReadOnlyDictionary<string,JsonElement> values)
    {
        if(values.Count>256 || values.Any(p=>string.IsNullOrWhiteSpace(p.Key) || p.Key.Length>160 || p.Value.ValueKind==JsonValueKind.Undefined) ||
            JsonSerializer.SerializeToUtf8Bytes(values).Length>65536)throw new ArgumentException("METADATA_PROPERTIES_INVALID");
    }
    private static string Digest<T>(T value)=>Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value,WorldJson.CreateOptions())));
}
