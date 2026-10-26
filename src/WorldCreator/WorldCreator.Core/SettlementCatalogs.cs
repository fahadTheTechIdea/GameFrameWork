using System.Collections.Frozen;
using System.Security.Cryptography;
using System.Text.Json;

namespace WorldCreator.Core.Planning;

public sealed record SettlementCatalogSelection(string CatalogId, IReadOnlySet<string> EnabledCapabilities);
public sealed record CatalogVocabulary(IReadOnlySet<string> Functions, IReadOnlySet<string> Categories,
    IReadOnlySet<string> Districts, IReadOnlySet<string> Technologies, IReadOnlySet<string> Resources);
public sealed record CatalogCompositionProvenance(IReadOnlyDictionary<string,string> CatalogDigests,
    IReadOnlyDictionary<string,string> StructureOrigins, IReadOnlyDictionary<string,string> FacilityOrigins);
public sealed record SettlementCatalogDescriptor(string Id, string Name, IReadOnlySet<string> Capabilities,
    IReadOnlySet<string> RequiredCatalogs, CatalogVocabulary Vocabulary,
    CatalogCompositionProvenance? Composition = null, IReadOnlyDictionary<string,string>? AuthoredContentDigests = null);
public sealed record ResolvedSettlementCatalog(string Id, string Name, string ContentSha256,
    IReadOnlySet<string> EnabledCapabilities, string PolicyId, IReadOnlyDictionary<string,string> ImplementationIdentities, CatalogCompositionProvenance? Composition = null, IReadOnlyDictionary<string,string>? AuthoredContentDigests = null);

/// <summary>Implemented by independently registered packs. Pack code must be deterministic and side-effect free.</summary>
public interface ISettlementCatalogProvider
{
    SettlementCatalogDescriptor Descriptor { get; }
    IReadOnlyList<BuildingDefinition> Structures { get; }
    ISettlementCatalogPolicy Policy { get; }
    ISettlementDemandPolicy Demand { get; }
    IInfrastructureDesignStrategy Infrastructure { get; }
    IReadOnlyList<FacilityDefinition> Facilities { get; }
}
public interface ISettlementDemandPolicy
{
    BuildingProgram Plan(IReadOnlyList<BuildingDefinition> catalog, SettlementDemand settlement);
}
public interface ISettlementCatalogPolicy
{
    string Id { get; }
    BuildingDefinition Resolve(BuildingDefinition definition, string technology);
    IReadOnlySet<string> RequiredCapabilities(BuildingDefinition definition, string technology);
    IReadOnlySet<string> RequiredCapabilities(MockupWorldDesign design);
    IReadOnlyList<string> ValidateSupply(MockupWorldDesign design, BuildingDefinition definition, StructureResourceRequirement requirement,
        SitingResourceSource source, StructureSitingContext context, CancellationToken token=default);
    bool RequiresNeighborPolicy(IReadOnlySet<string> firstTags, IReadOnlySet<string> secondTags);
}
public interface ISettlementCatalogRegistry
{
    IReadOnlyList<SettlementCatalogDescriptor> Catalogs { get; }
    RegisteredSettlementCatalog Resolve(SettlementCatalogSelection selection);
}
public sealed record RegisteredSettlementCatalog(SettlementCatalogDescriptor Descriptor,
    IReadOnlyList<BuildingDefinition> Structures, ISettlementCatalogPolicy Policy, ISettlementDemandPolicy Demand, IInfrastructureDesignStrategy Infrastructure,
    ResolvedSettlementCatalog Provenance, IReadOnlyList<FacilityDefinition> Facilities);

/// <summary>One current contract. No implicit catalog, load-order replacement or compatibility fallback.</summary>
public sealed class SettlementCatalogRegistry : ISettlementCatalogRegistry
{
    private readonly FrozenDictionary<string,RegisteredSettlementCatalog> catalogs;
    public IReadOnlyList<SettlementCatalogDescriptor> Catalogs { get; }
    public SettlementCatalogRegistry(IEnumerable<ISettlementCatalogProvider> providers, CancellationToken token=default)
    {
        ArgumentNullException.ThrowIfNull(providers);
        var registered=new Dictionary<string,RegisteredSettlementCatalog>(StringComparer.Ordinal);
        foreach (var provider in providers)
        {
            token.ThrowIfCancellationRequested();
            if (registered.Count>=256) throw new ArgumentException("CATALOG_PROVIDER_BUDGET_EXCEEDED");
            if (provider is null) throw new ArgumentException("CATALOG_PROVIDER_INVALID");
            var d=provider.Descriptor;
            if (d is null || !ValidId(d.Id) || string.IsNullOrWhiteSpace(d.Name) || d.Capabilities is null ||
                d.Capabilities.Any(c=>!ValidId(c)) || d.RequiredCatalogs is null || d.RequiredCatalogs.Any(c=>!ValidId(c)) ||
                d.Vocabulary is null || provider.Policy is null || !ValidId(provider.Policy.Id) || provider.Demand is null || provider.Infrastructure is null ||
                provider.Structures is not { Count: >0 and <=10000 }) throw new ArgumentException("CATALOG_PROVIDER_INVALID");
            if (d.Composition is { } composition && (composition.CatalogDigests is null || composition.StructureOrigins is null || composition.FacilityOrigins is null))
                throw new ArgumentException("CATALOG_IMPORT_PROVENANCE_INVALID");
            if (d.AuthoredContentDigests?.Any(p=>!ValidId(p.Key) || p.Value is null || p.Value.Length!=64 || p.Value.Any(c=>!char.IsAsciiHexDigit(c)))==true)
                throw new ArgumentException("CATALOG_AUTHORED_CONTENT_PROVENANCE_INVALID");
            var v=d.Vocabulary;
            IReadOnlySet<string> Freeze(IReadOnlySet<string> values)
            {
                if (values is null || values.Any(x=>!ValidId(x))) throw new ArgumentException("CATALOG_VOCABULARY_INVALID");
                return values.ToFrozenSet(StringComparer.Ordinal);
            }
            d=d with { Capabilities=Freeze(d.Capabilities),RequiredCatalogs=Freeze(d.RequiredCatalogs),
                AuthoredContentDigests=d.AuthoredContentDigests?.OrderBy(p=>p.Key,StringComparer.Ordinal).ToFrozenDictionary(StringComparer.Ordinal),
                Composition=d.Composition is null?null:new(
                    d.Composition.CatalogDigests.OrderBy(p=>p.Key,StringComparer.Ordinal).ToFrozenDictionary(StringComparer.Ordinal),
                    d.Composition.StructureOrigins.OrderBy(p=>p.Key,StringComparer.Ordinal).ToFrozenDictionary(StringComparer.Ordinal),
                    d.Composition.FacilityOrigins.OrderBy(p=>p.Key,StringComparer.Ordinal).ToFrozenDictionary(StringComparer.Ordinal)),
                Vocabulary=new(Freeze(v.Functions),Freeze(v.Categories),Freeze(v.Districts),Freeze(v.Technologies),Freeze(v.Resources)) };
            var definitions=provider.Structures.Select(FreezeDefinition).OrderBy(s=>s.Id,StringComparer.Ordinal).ToArray();
            if (definitions.Select(s=>s.Id).Distinct(StringComparer.Ordinal).Count()!=definitions.Length)
                throw new ArgumentException("CATALOG_DUPLICATE_STRUCTURE");
            foreach (var definition in definitions)
            {
                token.ThrowIfCancellationRequested();
                if (definition.Composition is null) throw new ArgumentException("CATALOG_COMPOSITION_REVIEW_REQUIRED:"+definition.Id);
                ValidateDefinition(d,definition);
                foreach (var technology in definition.SitingProfile!.AllowedTechnologies)
                {
                    var resolved=provider.Policy.Resolve(definition,technology);
                    ValidateDefinition(d,resolved);
                    if (resolved.Id!=definition.Id || provider.Policy.RequiredCapabilities(definition,technology).Any(c=>!d.Capabilities.Contains(c)))
                        throw new ArgumentException("CATALOG_POLICY_CONTRACT_INVALID:"+definition.Id);
                }
            }
            var facilities=FacilityCompositionPlanner.Snapshot(provider.Facilities,definitions,token);
            var implementations=new[] { provider.GetType().Assembly,provider.Policy.GetType().Assembly,provider.Demand.GetType().Assembly,
                    provider.Infrastructure.GetType().Assembly,typeof(ISettlementCatalogProvider).Assembly }
                .Distinct().OrderBy(a=>a.FullName,StringComparer.Ordinal).ToDictionary(a=>a.GetName().Name!,a=>
                    string.IsNullOrEmpty(a.Location)?"module:"+a.ManifestModule.ModuleVersionId.ToString("N"):
                    "sha256:"+Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(a.Location))),StringComparer.Ordinal).ToFrozenDictionary(StringComparer.Ordinal);
            string digest=Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(
                new { Descriptor=d,Structures=definitions,Facilities=facilities,Policy=provider.Policy.Id,Implementations=implementations.OrderBy(p=>p.Key,StringComparer.Ordinal) },WorldJson.CreateOptions())));
            if (!registered.TryAdd(d.Id,new(d,Array.AsReadOnly(definitions),provider.Policy,provider.Demand,provider.Infrastructure,
                new(d.Id,d.Name,digest,d.Capabilities,provider.Policy.Id,implementations,d.Composition,d.AuthoredContentDigests),facilities))) throw new ArgumentException("CATALOG_DUPLICATE_ID:"+d.Id);
        }
        var active=new HashSet<string>(StringComparer.Ordinal); var done=new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in registered.Keys.Order(StringComparer.Ordinal)) Visit(id);
        void Visit(string id)
        {
            token.ThrowIfCancellationRequested();
            if (!registered.ContainsKey(id)) throw new ArgumentException("CATALOG_DEPENDENCY_MISSING:"+id);
            if (done.Contains(id)) return;
            if (!active.Add(id)) throw new ArgumentException("CATALOG_DEPENDENCY_CYCLE:"+id);
            if (active.Count>256) throw new ArgumentException("CATALOG_DEPENDENCY_DEPTH_EXCEEDED");
            foreach (var dependency in registered[id].Descriptor.RequiredCatalogs.Order(StringComparer.Ordinal)) Visit(dependency);
            if (registered[id].Descriptor.Composition is { } composition)
            {
                if (!registered[id].Descriptor.RequiredCatalogs.SetEquals(composition.CatalogDigests.Keys) ||
                    !registered[id].Structures.Select(s=>s.Id).ToHashSet(StringComparer.Ordinal).SetEquals(composition.StructureOrigins.Keys) ||
                    !registered[id].Facilities.Select(f=>f.Id).ToHashSet(StringComparer.Ordinal).SetEquals(composition.FacilityOrigins.Keys))
                    throw new ArgumentException("CATALOG_IMPORT_PROVENANCE_INVALID:"+id);
                foreach (var import in composition.CatalogDigests)
                    if (!registered.TryGetValue(import.Key,out var source) || source.Provenance.ContentSha256!=import.Value)
                        throw new ArgumentException("CATALOG_IMPORT_DIGEST_MISMATCH:"+import.Key);
                foreach (var origin in composition.StructureOrigins)
                    if (!composition.CatalogDigests.ContainsKey(origin.Value) || !registered[origin.Value].Structures.Any(s=>s.Id==origin.Key))
                        throw new ArgumentException("CATALOG_IMPORT_ORIGIN_INVALID:"+origin.Key);
                foreach (var origin in composition.FacilityOrigins)
                    if (!composition.CatalogDigests.ContainsKey(origin.Value) || !registered[origin.Value].Facilities.Any(f=>f.Id==origin.Key))
                        throw new ArgumentException("CATALOG_IMPORT_ORIGIN_INVALID:"+origin.Key);
            }
            active.Remove(id); done.Add(id);
        }
        catalogs=registered.ToFrozenDictionary(StringComparer.Ordinal);
        Catalogs=Array.AsReadOnly(registered.Values.Select(c=>c.Descriptor).OrderBy(c=>c.Id,StringComparer.Ordinal).ToArray());
    }
    public RegisteredSettlementCatalog Resolve(SettlementCatalogSelection selection)
    {
        if (selection is null || !ValidId(selection.CatalogId) || selection.EnabledCapabilities is null)
            throw new ArgumentException("CATALOG_SELECTION_REQUIRED");
        if (!catalogs.TryGetValue(selection.CatalogId,out var catalog)) throw new ArgumentException("CATALOG_NOT_REGISTERED:"+selection.CatalogId);
        if (selection.EnabledCapabilities.Any(c=>!catalog.Descriptor.Capabilities.Contains(c)))
            throw new ArgumentException("CATALOG_CAPABILITY_UNKNOWN");
        return catalog with { Provenance=catalog.Provenance with {
            EnabledCapabilities=selection.EnabledCapabilities.ToFrozenSet(StringComparer.Ordinal) } };
    }
    public static void ValidateDefinition(SettlementCatalogDescriptor descriptor,BuildingDefinition definition)
    {
        definition.Validate();
        var v=descriptor.Vocabulary;
        var p=definition.SitingProfile ?? throw new ArgumentException("CATALOG_SITING_PROFILE_REQUIRED:"+definition.Id);
        if (!v.Functions.Contains(definition.Function) || !v.Categories.Contains(definition.Category) ||
            p.AllowedDistricts.Any(d=>!v.Districts.Contains(d)) || p.AllowedTechnologies.Any(t=>!v.Technologies.Contains(t)) ||
            p.Requirements.Any(r=>!v.Resources.Contains(r.Kind)) || p.Provides?.Any(r=>!v.Resources.Contains(r))==true)
            throw new ArgumentException("CATALOG_VOCABULARY_UNREGISTERED:"+definition.Id);
    }
    public static bool ValidId(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length<=160 &&
        value.All(c=>char.IsAsciiLetterOrDigit(c) || c is '.' or '-' or '_' or ':');
    private static BuildingDefinition FreezeDefinition(BuildingDefinition d)
    {
        ArgumentNullException.ThrowIfNull(d); d.Validate();
        var p=d.SitingProfile ?? throw new ArgumentException("CATALOG_SITING_PROFILE_REQUIRED");
        return d with { Composition=d.Composition is null?null:d.Composition with { PartRoles=Array.AsReadOnly(d.Composition.PartRoles.Select(r=>r with { CandidateDefinitionIds=Array.AsReadOnly(r.CandidateDefinitionIds.ToArray()) }).ToArray()) },
            AllowedClasses=d.AllowedClasses.ToFrozenSet(),FavoredRoles=d.FavoredRoles.ToFrozenSet(),
            AllowedRoles=d.AllowedRoles?.ToFrozenSet(),SiteRules=Array.AsReadOnly((d.SiteRules ?? []).ToArray()),
            SizeVariants=Array.AsReadOnly((d.SizeVariants ?? []).Select(v=>v with { AllowedClasses=v.AllowedClasses?.ToFrozenSet() }).ToArray()),
            SitingProfile=p with { ResearchRuleIds=Array.AsReadOnly(p.ResearchRuleIds.ToArray()),SourceIds=Array.AsReadOnly(p.SourceIds.ToArray()),
                AllowedDistricts=p.AllowedDistricts.ToFrozenSet(StringComparer.Ordinal),AllowedTechnologies=p.AllowedTechnologies.ToFrozenSet(StringComparer.Ordinal),
                Tags=p.Tags.ToFrozenSet(StringComparer.Ordinal),RequiredSurroundings=p.RequiredSurroundings.ToFrozenSet(),
                Provides=p.Provides?.ToFrozenSet(StringComparer.Ordinal),Requirements=Array.AsReadOnly(p.Requirements.Select(r=>r with {
                    RequiredQualities=r.RequiredQualities?.ToFrozenSet(StringComparer.Ordinal),WhenTechnologies=r.WhenTechnologies?.ToFrozenSet(StringComparer.Ordinal) }).ToArray()) } };
    }
}
