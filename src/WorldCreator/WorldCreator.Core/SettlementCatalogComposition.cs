using System.Collections.Frozen;
using System.Text.Json;

namespace WorldCreator.Core.Planning;

public sealed record CatalogImport(string CatalogId, IReadOnlyList<string> StructureIds, IReadOnlyList<string> FacilityIds);
public sealed record CatalogCompositionDefinition(string Id, string Name, IReadOnlyList<CatalogImport> Imports);
public sealed record CatalogCompositionManifest(int SchemaVersion, IReadOnlyList<CatalogCompositionDefinition> Catalogs);

/// <summary>Explicit positive imports. IDs have one owner; collisions never select a winner.
/// Policies stay with their defining packs, including supply, demand and infrastructure code.</summary>
public sealed class SettlementCatalogComposition : ISettlementCatalogProvider
{
    public SettlementCatalogDescriptor Descriptor { get; }
    public IReadOnlyList<BuildingDefinition> Structures { get; }
    public IReadOnlyList<FacilityDefinition> Facilities { get; }
    public ISettlementCatalogPolicy Policy { get; }
    public ISettlementDemandPolicy Demand { get; }
    public IInfrastructureDesignStrategy Infrastructure { get; }

    public SettlementCatalogComposition(CatalogCompositionDefinition definition, ISettlementCatalogRegistry registry,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (definition is null || !SettlementCatalogRegistry.ValidId(definition.Id) || string.IsNullOrWhiteSpace(definition.Name) ||
            definition.Imports is not { Count: > 0 and <= 256 }) throw new ArgumentException("CATALOG_COMPOSITION_INVALID");
        var packs = new List<RegisteredSettlementCatalog>();
        var structures = new Dictionary<string, BuildingDefinition>(StringComparer.Ordinal);
        var owners = new Dictionary<string, RegisteredSettlementCatalog>(StringComparer.Ordinal);
        var facilities = new Dictionary<string, FacilityDefinition>(StringComparer.Ordinal);
        var facilityOwners = new Dictionary<string,string>(StringComparer.Ordinal);
        foreach (var import in definition.Imports.OrderBy(i => i?.CatalogId, StringComparer.Ordinal))
        {
            token.ThrowIfCancellationRequested();
            if (import is null || import.StructureIds is null || import.FacilityIds is null ||
                import.StructureIds.Count + import.FacilityIds.Count == 0 || import.StructureIds.Count > 10000 || import.FacilityIds.Count > 1000 ||
                packs.Any(p => p.Descriptor.Id == import.CatalogId)) throw new ArgumentException("CATALOG_IMPORT_INVALID");
            var descriptor = registry.Catalogs.SingleOrDefault(d => d.Id == import.CatalogId)
                ?? throw new ArgumentException("CATALOG_IMPORT_MISSING:" + import.CatalogId);
            var pack = registry.Resolve(new(descriptor.Id, descriptor.Capabilities)); packs.Add(pack);
            foreach (var id in import.StructureIds.Order(StringComparer.Ordinal))
            {
                var structure = pack.Structures.SingleOrDefault(s => s.Id == id) ?? throw new ArgumentException("CATALOG_IMPORT_STRUCTURE_UNKNOWN:" + id);
                if (!structures.TryAdd(id, structure)) throw new ArgumentException("CATALOG_IMPORT_STRUCTURE_CONFLICT:" + id);
                owners.Add(id, pack);
            }
            foreach (var id in import.FacilityIds.Order(StringComparer.Ordinal))
            {
                var facility = pack.Facilities.SingleOrDefault(f => f.Id == id) ?? throw new ArgumentException("CATALOG_IMPORT_FACILITY_UNKNOWN:" + id);
                if (!facilities.TryAdd(id, facility)) throw new ArgumentException("CATALOG_IMPORT_FACILITY_CONFLICT:" + id);
                facilityOwners.Add(id,pack.Descriptor.Id);
            }
        }
        IReadOnlySet<string> Union(Func<SettlementCatalogDescriptor, IReadOnlySet<string>> values) =>
            packs.SelectMany(p => values(p.Descriptor)).ToFrozenSet(StringComparer.Ordinal);
        Descriptor = new(definition.Id, definition.Name, Union(d => d.Capabilities),
            packs.Select(p => p.Descriptor.Id).ToFrozenSet(StringComparer.Ordinal),
            new(Union(d => d.Vocabulary.Functions), Union(d => d.Vocabulary.Categories), Union(d => d.Vocabulary.Districts),
                Union(d => d.Vocabulary.Technologies), Union(d => d.Vocabulary.Resources)),
            new(packs.ToFrozenDictionary(p => p.Descriptor.Id, p => p.Provenance.ContentSha256, StringComparer.Ordinal),
                owners.OrderBy(p=>p.Key,StringComparer.Ordinal).ToFrozenDictionary(p=>p.Key,p=>p.Value.Descriptor.Id,StringComparer.Ordinal),
                facilityOwners.OrderBy(p=>p.Key,StringComparer.Ordinal).ToFrozenDictionary(StringComparer.Ordinal)));
        Structures = Array.AsReadOnly(structures.Values.OrderBy(s => s.Id, StringComparer.Ordinal).ToArray());
        Facilities = FacilityCompositionPlanner.Snapshot(facilities.Values.ToArray(), Structures, token);
        var dispatch = new Dispatch(definition.Id, owners.ToFrozenDictionary(StringComparer.Ordinal), packs.ToArray());
        Policy = dispatch; Demand = dispatch; Infrastructure = dispatch;
    }

    public static CatalogCompositionManifest ReadManifest(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length is <= 0 or > 1048576) throw new ArgumentException("CATALOG_COMPOSITION_MANIFEST_SIZE");
        var options=WorldJson.CreateOptions();
        options.UnmappedMemberHandling=System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow;
        options.RespectRequiredConstructorParameters=true;
        var manifest = JsonSerializer.Deserialize<CatalogCompositionManifest>(stream, options)
            ?? throw new ArgumentException("CATALOG_COMPOSITION_MANIFEST_INVALID");
        if (manifest.SchemaVersion != 1 || manifest.Catalogs is not { Count: > 0 and <= 256 })
            throw new ArgumentException("CATALOG_COMPOSITION_MANIFEST_INVALID");
        return manifest;
    }

    public static IReadOnlyList<ISettlementCatalogProvider> Register(IEnumerable<ISettlementCatalogProvider> providers,
        IEnumerable<CatalogCompositionDefinition> definitions, CancellationToken token = default)
    {
        var result = providers.ToList(); var pending = definitions.ToList();
        if (pending.Count > 256 || pending.Any(d => d is null || !SettlementCatalogRegistry.ValidId(d.Id)) ||
            pending.Select(d => d.Id).Distinct(StringComparer.Ordinal).Count() != pending.Count ||
            pending.Any(d => result.Any(p => p.Descriptor.Id == d.Id))) throw new ArgumentException("CATALOG_COMPOSITION_ID_CONFLICT");
        var registry = new SettlementCatalogRegistry(result, token);
        while (pending.Count > 0)
        {
            token.ThrowIfCancellationRequested();
            var next = pending.OrderBy(d => d.Id, StringComparer.Ordinal).FirstOrDefault(d => d.Imports is not null &&
                d.Imports.All(i => i is not null && registry.Catalogs.Any(c => c.Id == i.CatalogId)));
            if (next is null) throw new ArgumentException("CATALOG_COMPOSITION_DEPENDENCY_MISSING_OR_CYCLE");
            result.Add(new SettlementCatalogComposition(next, registry, token)); pending.Remove(next);
            registry = new SettlementCatalogRegistry(result, token);
        }
        return Array.AsReadOnly(result.ToArray());
    }

    private sealed class Dispatch(string id, FrozenDictionary<string, RegisteredSettlementCatalog> owners,
        IReadOnlyList<RegisteredSettlementCatalog> packs) : ISettlementCatalogPolicy, ISettlementDemandPolicy, IInfrastructureDesignStrategy
    {
        public string Id => id + ":composition";
        private RegisteredSettlementCatalog Owner(BuildingDefinition d) => owners.TryGetValue(d.Id, out var pack) ? pack
            : throw new ArgumentException("CATALOG_COMPOSITION_STRUCTURE_NOT_IMPORTED:" + d.Id);
        public BuildingDefinition Resolve(BuildingDefinition definition, string technology) => Owner(definition).Policy.Resolve(definition, technology);
        public IReadOnlySet<string> RequiredCapabilities(BuildingDefinition definition, string technology) => Owner(definition).Policy.RequiredCapabilities(definition, technology);
        public IReadOnlySet<string> RequiredCapabilities(MockupWorldDesign design) => packs.Where(p => Filter(design, p).BuildingCatalog.Count > 0)
            .SelectMany(p => p.Policy.RequiredCapabilities(Filter(design, p))).ToFrozenSet(StringComparer.Ordinal);
        public IReadOnlyList<string> ValidateSupply(MockupWorldDesign design, BuildingDefinition definition,
            StructureResourceRequirement requirement, SitingResourceSource source, StructureSitingContext context, CancellationToken token = default) =>
            Owner(definition).Policy.ValidateSupply(design, definition, requirement, source, context, token);
        public bool RequiresNeighborPolicy(IReadOnlySet<string> firstTags, IReadOnlySet<string> secondTags) => packs.Any(p => p.Policy.RequiresNeighborPolicy(firstTags, secondTags));
        public BuildingProgram Plan(IReadOnlyList<BuildingDefinition> catalog, SettlementDemand settlement)
        {
            var results = packs.Select(p => (Pack: p, Definitions: catalog.Where(d => Owner(d).Descriptor.Id == p.Descriptor.Id).ToArray()))
                .Where(p => p.Definitions.Length > 0).Select(p => (p.Definitions, Program: p.Pack.Demand.Plan(p.Definitions, settlement))).ToArray();
            if (results.Any(p => p.Program.Needs.Any(n => !p.Definitions.Any(d => d.Id == n.DefinitionId))))
                throw new ArgumentException("CATALOG_COMPOSITION_DEMAND_NOT_IMPORTED");
            return new(results.SelectMany(r => r.Program.Needs).OrderBy(n => n.DefinitionId, StringComparer.Ordinal).ToArray(),
                results.SelectMany(r => r.Program.Diagnostics).Order(StringComparer.Ordinal).ToArray());
        }
        public InfrastructureDesignResult Plan(MockupWorldDesign design, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            if (design.InfrastructureRules is not { Count: > 0 }) return new([], []);
            var results = new List<InfrastructureDesignResult>();
            foreach (var pack in packs)
            {
                token.ThrowIfCancellationRequested(); var filtered = Filter(design, pack);
                if (filtered.InfrastructureRules is { Count: > 0 }) results.Add(pack.Infrastructure.Plan(filtered, token));
            }
            return new(results.SelectMany(r => r.Structures).OrderBy(s => s.Id, StringComparer.Ordinal).ToArray(),
                results.SelectMany(r => r.Issues).Order(StringComparer.Ordinal).ToArray());
        }
        private MockupWorldDesign Filter(MockupWorldDesign design, RegisteredSettlementCatalog pack) => design with {
            BuildingCatalog = design.BuildingCatalog.Where(d => Owner(d).Descriptor.Id == pack.Descriptor.Id).ToArray(),
            SettlementRules = design.SettlementRules.Where(r => owners.TryGetValue(r.Envelope.BuildingType, out var owner) && owner.Descriptor.Id == pack.Descriptor.Id).ToArray(),
            InfrastructureRules = (design.InfrastructureRules ?? []).Where(r => owners.TryGetValue(r.DefinitionId, out var owner) && owner.Descriptor.Id == pack.Descriptor.Id).ToArray()
        };
    }
}
