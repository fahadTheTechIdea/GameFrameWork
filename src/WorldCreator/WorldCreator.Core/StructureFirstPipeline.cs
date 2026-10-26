using System.Security.Cryptography;
using System.Text.Json;
using WorldCreator.Core.Planning;

namespace WorldCreator.Core;

/// <summary>Owns structure-first jobs, scope coverage and exact instance assembly.
/// Host agents execute image jobs; generated concepts never supply object placements.</summary>
public sealed partial class StructureFirstPipeline
{
    public static string WorldDigest(WorldDocument world) => Convert.ToHexStringLower(
        SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(world, WorldJson.CreateOptions())));
    public static string JobId(string definition, string variant) => definition + ":" + variant;

    public StructureFirstSpecification Plan(WorldDocument world, CancellationToken token = default)
    {
        var structures = Structures(world, token);
        var design = world.Request.Design!;
        var scopes = new List<StructureVisualScope> { new("world", null, VisualScopeKind.World, null, null, []) };
        var rules = design.SettlementRules.ToDictionary(r => r.Id, StringComparer.Ordinal);
        var groups = structures.GroupBy(o => rules[o.DesignRuleId].SettlementId).OrderBy(g => g.Key, StringComparer.Ordinal);
        int index = 0;
        foreach (var group in groups)
        {
            string id = "settlement-" + index++;
            var classes = group.Select(o => rules[o.DesignRuleId].Settlement!.Class).Distinct().ToArray();
            if (classes.Length != 1) throw new ArgumentException("STRUCTURE_SETTLEMENT_CLASS_CONFLICT");
            var regions = group.Select(o => o.VisualSource!.RegionId).Distinct().Order(StringComparer.Ordinal).ToArray();
            bool city = classes[0] == SettlementClass.City;
            scopes.Add(new(id, "world", VisualScopeKind.Settlement, group.Key, classes[0], city ? [] : regions, city));
            if (city)
            {
                // Reuse authored districts; never invent a grid partition or relocate sites.
                int districtIndex = 0;
                foreach (var district in group.GroupBy(o => rules[o.DesignRuleId].Siting?.DistrictId ?? "sample").OrderBy(g => g.Key, StringComparer.Ordinal))
                    scopes.Add(new(id + "-district-" + districtIndex++, id, VisualScopeKind.District, group.Key, null,
                        district.Select(o => o.VisualSource!.RegionId).Distinct().Order(StringComparer.Ordinal).ToArray()));
            }
        }
        var art = structures.Select(o => (o.VisualSource!.BuildingDefinitionId!, o.VisualSource.SizeVariantId!))
            .Distinct().OrderBy(x => x.Item1, StringComparer.Ordinal).ThenBy(x => x.Item2, StringComparer.Ordinal)
            .Select(x => new StructureArtEntry(x.Item1, x.Item2)).ToArray();
        return new(1, design.Blueprint.MockupId + ".structures-first", WorldDigest(world),
            design.Blueprint.ArtStyleId, "Use the saved GameArts camera/profile; do not infer camera approval from a draft.",
            64, scopes, art);
    }

    public async Task<StructureFirstResult> BuildAsync(WorldDocument world, StructureFirstSpecification specification,
        string trustedAssetRoot, CancellationToken token = default)
    {
        var structures = Structures(world, token);
        var design = world.Request.Design!;
        ValidateSpecification(specification, world);
        var library = new Dictionary<string, StructureArtEntry>(StringComparer.Ordinal);
        foreach (var entry in specification.Art)
        {
            token.ThrowIfCancellationRequested();
            if (entry is null || !SettlementCatalogRegistry.ValidId(entry.DefinitionId) ||
                !SettlementCatalogRegistry.ValidId(entry.VariantId) || !library.TryAdd(JobId(entry.DefinitionId, entry.VariantId), entry))
                throw new ArgumentException("STRUCTURE_LIBRARY_DUPLICATE_OR_INVALID");
            if (entry.Master is not null) _ = await ReadImageAsync(entry.Master, trustedAssetRoot, token);
        }
        var scopeMap = specification.Scopes.ToDictionary(s => s.Id, StringComparer.Ordinal);
        var concepts = new HashSet<string>(StringComparer.Ordinal);
        foreach (var concept in specification.Concepts ?? [])
        {
            if (concept is null || !scopeMap.ContainsKey(concept.ScopeId) || !concepts.Add(concept.ScopeId) ||
                string.IsNullOrWhiteSpace(concept.MockupId) || string.IsNullOrWhiteSpace(concept.Revision) || concept.Image is null)
                throw new ArgumentException("STRUCTURE_CONCEPT_INVALID");
            _ = await ReadImageAsync(concept.Image, trustedAssetRoot, token);
        }
        var leaves = specification.Scopes.Where(s => !specification.Scopes.Any(child => child.ParentId == s.Id)).ToArray();
        var rules = design.SettlementRules.ToDictionary(r => r.Id, StringComparer.Ordinal);
        var instances = new List<StructureFirstInstance>();
        foreach (var item in structures)
        {
            token.ThrowIfCancellationRequested();
            var source = item.VisualSource!;
            string job = JobId(source.BuildingDefinitionId!, source.SizeVariantId!);
            if (!library.TryGetValue(job, out var art)) throw new ArgumentException("STRUCTURE_LIBRARY_MISSING:" + job);
            var owners = leaves.Where(s => s.RegionIds.Contains(source.RegionId, StringComparer.Ordinal)).ToArray();
            if (owners.Length != 1) throw new ArgumentException("STRUCTURE_SCOPE_OWNERSHIP:" + item.Id);
            var owner = owners[0]; var cursor = owner;
            while (cursor.Kind != VisualScopeKind.Settlement && cursor.ParentId is { } parent) cursor = scopeMap[parent];
            if (cursor.Kind != VisualScopeKind.Settlement || cursor.SettlementId != rules[item.DesignRuleId].SettlementId ||
                cursor.SettlementClass != rules[item.DesignRuleId].Settlement!.Class)
                throw new ArgumentException("STRUCTURE_SCOPE_SETTLEMENT_MISMATCH:" + item.Id);
            instances.Add(new(item, owner.Id, job, art));
        }
        if (!library.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(instances.Select(i => i.StructureJobId)))
            throw new ArgumentException("STRUCTURE_LIBRARY_UNUSED_ENTRY");
        var definitions = design.BuildingCatalog.ToDictionary(d => d.Id, StringComparer.Ordinal);
        var jobs = new List<StructureFirstJob>();
        foreach (var group in instances.GroupBy(i => i.StructureJobId).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            var first = group.First(); var definition = definitions[first.Art.DefinitionId];
            var variant = definition.SizeVariants!.Single(v => v.Id == first.Art.VariantId);
            string prompt = $"Create ONE {definition.Function} structure master for {definition.Id}/{variant.Id}. " +
                $"Saved art style: {specification.ArtStyleId}. Camera: {specification.CameraProfile}. " +
                $"Ground footprint {variant.WidthMeters} x {variant.DepthMeters} meters; physical height {variant.HeightMeters} meters. " +
                $"Composition scope: {definition.Composition!.GeometryScope}. " +
                "Use the actual reviewed style reference. Separate movable/interactive parts; use canonical GameArts source masks, components and measured atlases. " +
                "Do not generate a city or a sheet. No approval or measured pivot may be invented.";
            jobs.Add(new(group.Key, definition.Id, variant.Id, group.Count(), variant.WidthMeters, variant.DepthMeters,
                definition.Function, definition.Composition, prompt, first.Art.Master is null ? "AwaitingStructureMaster" : "CandidateVerifiedNotApproved"));
        }
        var coverage = new List<StructureScopeCoverage>();
        var mockups = new List<StructureMockupJob>();
        foreach (var scope in specification.Scopes.OrderBy(s => s.Id, StringComparer.Ordinal))
        {
            token.ThrowIfCancellationRequested();
            bool Under(string leaf)
            { for (var s = scopeMap[leaf]; ; s = scopeMap[s.ParentId!]) { if (s.Id == scope.Id) return true; if (s.ParentId is null) return false; } }
            var members = instances.Where(i => Under(i.ScopeId)).ToArray();
            var children = specification.Scopes.Where(s => s.ParentId == scope.Id).ToArray();
            var issues = new List<string>();
            if (scope.IsSample) issues.Add("PARTIAL_SETTLEMENT_SAMPLE:" + scope.Id);
            if (children.Length == 0 && scope.RegionIds.Count == 0) issues.Add("UNPLANNED_SCOPE:" + scope.Id);
            if (scope.Kind == VisualScopeKind.Settlement && scope.SettlementClass == SettlementClass.City &&
                children.Count(s => s.Kind == VisualScopeKind.District) < 2) issues.Add("CITY_MULTIPLE_DISTRICT_SCOPES_REQUIRED:" + scope.Id);
            if (children.Length == 0 && members.Length > specification.MaximumStructuresPerMockup)
                issues.Add("MOCKUP_SCOPE_REQUIRES_AUTHORED_SUBDIVISION:" + scope.Id);
            coverage.Add(new(scope.Id, members.Length, issues.Count == 0, issues));
            if (members.Length == 0 && children.Length == 0 && scope.RegionIds.Count == 0) continue;
            string role = children.Length > 0 ? "OverviewContextOnly" : scope.Kind.ToString();
            string prompt = $"Generate a {role} mockup for {scope.Id}, {scope.SettlementClass?.ToString() ?? scope.Kind.ToString()}. " +
                $"This image covers {members.Length} planned structures in this scope, not an inferred complete city. " +
                (scope.IsSample ? "This is a SAMPLE district; remaining settlement coverage is unresolved. " : "") +
                "Use the verified structure master reference board and the engine layout for this scope. Preserve shared style, boundary connections and scale between scopes. " +
                "The C# instance manifest owns IDs/counts/positions. Concept pixels cannot replace it. " +
                "Build reference boards through canonical GameArts tools if the host image-reference limit is exceeded.";
            string status = members.Any(i => i.Art.Master is null) ? "AwaitingStructureMasters" :
                issues.Count > 0 ? "ScopePlanningRequired" : "ReadyForAgentImageCall";
            if (concepts.Contains(scope.Id))
            {
                if (members.Any(i => i.Art.Master is null)) throw new ArgumentException("STRUCTURE_CONCEPT_BEFORE_MASTERS");
                status = "ConceptCandidateVerifiedNotApproved";
            }
            mockups.Add(new(scope.Id, role, members.Select(i => i.Placement.Id).ToArray(),
                members.Select(i => i.StructureJobId).Distinct().Order(StringComparer.Ordinal).ToArray(), prompt, status));
        }
        // Completeness rolls up the whole authored hierarchy. An overview never fills missing districts.
        foreach (var scope in specification.Scopes.OrderByDescending(s => s.Kind))
        {
            token.ThrowIfCancellationRequested();
            int i = coverage.FindIndex(c => c.ScopeId == scope.Id);
            if (specification.Scopes.Where(s => s.ParentId == scope.Id).Any(child => !coverage.Single(c => c.ScopeId == child.Id).Complete))
                coverage[i] = coverage[i] with { Complete = false };
        }
        var productionIssues = coverage.SelectMany(c => c.Issues).Concat(instances.Where(i => i.Art.Master is null)
            .Select(i => "STRUCTURE_MASTER_MISSING:" + i.StructureJobId)).Concat(instances.Where(i => i.Art.Production is null)
            .Select(i => "STRUCTURE_PRODUCTION_ART_MISSING:" + i.StructureJobId))
            .Concat(leaves.Where(s => !concepts.Contains(s.Id)).Select(s => "STRUCTURE_SCOPE_CONCEPT_MISSING:" + s.Id))
            .Distinct(StringComparer.Ordinal).ToArray();
        return new(new(1, WorldDigest(world), world.Design!.Id, instances), jobs, mockups, coverage, productionIssues);
    }

    private static PlannedWorldObject[] Structures(WorldDocument world, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(world); token.ThrowIfCancellationRequested();
        if (world.SchemaVersion != 1 || world.Design is null || world.Request?.Design is null)
            throw new ArgumentException("STRUCTURE_WORLD_DESIGN_REQUIRED");
        var validation = new WorldDesignValidator().Validate(world.Design, token);
        if (!validation.IsValid) throw new WorldPlanException(validation.Issues);
        var compiled = new WorldPlanCompiler().Compile(world.Design, token);
        if (JsonSerializer.Serialize(compiled, WorldJson.CreateOptions()) != JsonSerializer.Serialize(world.Objects, WorldJson.CreateOptions()))
            throw new ArgumentException("STRUCTURE_WORLD_PLAN_OBJECT_MISMATCH");
        var result = world.Design.Objects.Where(o => o.VisualSource?.BuildingDefinitionId is not null).OrderBy(o => o.Id, StringComparer.Ordinal).ToArray();
        if (result.Length is 0 or > 10000 || result.Any(o => o.VisualSource?.SizeVariantId is null || o.VisualSource.Footprint is null))
            throw new ArgumentException("STRUCTURE_WORLD_INVENTORY_INVALID");
        return result;
    }

    private static void ValidateSpecification(StructureFirstSpecification spec, WorldDocument world)
    {
        if (spec is null || spec.SchemaVersion != 1 || !SettlementCatalogRegistry.ValidId(spec.Id) ||
            spec.WorldSha256 != WorldDigest(world) || spec.ArtStyleId != world.Request.Design!.Blueprint.ArtStyleId ||
            string.IsNullOrWhiteSpace(spec.CameraProfile) || spec.MaximumStructuresPerMockup is < 1 or > 1000 ||
            spec.Scopes is null || spec.Scopes.Count is 0 or > 512 || spec.Art is null || spec.Art.Count > 1000 || spec.Concepts?.Count > 512)
            throw new ArgumentException("STRUCTURE_SPECIFICATION_INVALID_OR_STALE");
        var scopes = new Dictionary<string, StructureVisualScope>(StringComparer.Ordinal);
        var regions = world.Request.Design!.Blueprint.Regions.Select(r => r.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var scope in spec.Scopes)
            if (scope is null || !SettlementCatalogRegistry.ValidId(scope.Id) || !scopes.TryAdd(scope.Id, scope) || !Enum.IsDefined(scope.Kind) ||
                scope.RegionIds is null || scope.RegionIds.Distinct(StringComparer.Ordinal).Count() != scope.RegionIds.Count ||
                scope.RegionIds.Any(r => !regions.Contains(r)) || scope.SettlementClass is { } c && !Enum.IsDefined(c) ||
                scope.Kind == VisualScopeKind.Settlement && (scope.SettlementClass is null || string.IsNullOrWhiteSpace(scope.SettlementId)))
                throw new ArgumentException("STRUCTURE_SCOPE_INVALID");
        if (scopes.Values.Count(s => s.ParentId is null && s.Kind == VisualScopeKind.World) != 1)
            throw new ArgumentException("STRUCTURE_SCOPE_ROOT_INVALID");
        foreach (var scope in scopes.Values)
        {
            if (scope.RegionIds.Count > 0 && scopes.Values.Any(s => s.ParentId == scope.Id)) throw new ArgumentException("STRUCTURE_SCOPE_PARENT_INVALID");
            if (scope.ParentId is null) { if (scope.Kind != VisualScopeKind.World) throw new ArgumentException("STRUCTURE_SCOPE_PARENT_INVALID"); continue; }
            if (!scopes.TryGetValue(scope.ParentId, out var parent) || (int)scope.Kind != (int)parent.Kind + 1 ||
                scope.RegionIds.Count > 0 && scopes.Values.Any(s => s.ParentId == scope.Id))
                throw new ArgumentException("STRUCTURE_SCOPE_PARENT_INVALID");
        }
    }
}
