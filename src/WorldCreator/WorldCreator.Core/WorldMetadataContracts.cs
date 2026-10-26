using System.Text.Json;
using WorldCreator.Core.Planning;

namespace WorldCreator.Core.Metadata;

public enum MetadataCoverage { CompleteDeclaredScope, PartialScope }
public enum MetadataConnectionPattern { Chain, Hub, Pairs, AllPairs }
public sealed record MetadataSpace(IReadOnlyList<string> Axes, string Unit);
public sealed record MetadataLineLayout(IReadOnlyList<double> Origin, IReadOnlyList<double> Step);
public sealed record MetadataPart(string Id, string ArchetypeId, int Count, string Purpose,
    string? CountParameter = null, MetadataLineLayout? Layout = null);
public sealed record MetadataConnectionRule(string Id, string Kind, MetadataConnectionPattern Pattern,
    string FromPart, string? ToPart = null, bool Bidirectional = true,
    IReadOnlyDictionary<string,JsonElement>? Properties = null);
public sealed record MetadataExplicitConnection(string Id,string Kind,string FromId,string ToId,
    bool Bidirectional = true,IReadOnlyDictionary<string,JsonElement>? Properties = null);
public sealed record MetadataChildRequirement(string Kind, int Minimum, int Maximum);
public sealed record MetadataStructureReference(SettlementCatalogSelection Catalog, string DefinitionId, string VariantId);
public sealed record WorldMetadataArchetype(string Id, string Kind, string Purpose,
    IReadOnlyDictionary<string, JsonElement> Properties, IReadOnlyList<MetadataPart> Parts,
    IReadOnlyList<MetadataConnectionRule> Connections, IReadOnlyList<MetadataChildRequirement> ChildRequirements,
    bool RequireWeaklyConnectedChildren = false, MetadataStructureReference? Structure = null);
public sealed record WorldMetadataCatalog(int SchemaVersion, string Id, IReadOnlyList<WorldMetadataArchetype> Archetypes);
public sealed record WorldMetadataRecipe(int SchemaVersion, string Id, string GameProfile, string CatalogId,
    string RootArchetypeId, string Scale, MetadataCoverage Coverage, IReadOnlyList<string> RemainingScopes,
    IReadOnlyDictionary<string, int> Parameters, MetadataSpace Space,
    IReadOnlyList<double>? Origin = null, bool RequireAllPositions = false,
    int MaximumNodes = 10000, int MaximumConnections = 50000,
    IReadOnlyList<MetadataExplicitConnection>? AdditionalConnections = null);
public sealed record WorldMetadataNode(string Id, string ArchetypeId, string Kind, string? ParentId,
    string Purpose, IReadOnlyDictionary<string, JsonElement> Properties,
    IReadOnlyList<double>? Position, string? StructureKey);
public sealed record WorldMetadataConnection(string Id, string Kind, string FromId, string ToId,
    bool Bidirectional, string SourceRuleId,IReadOnlyDictionary<string,JsonElement> Properties);
public sealed record MetadataStructureDefinition(ResolvedSettlementCatalog Catalog,
    BuildingDefinition Definition, BuildingSizeVariant Variant);
public sealed record MetadataMapBox(string NodeId,double X,double Y,double Width,double Height);
public sealed record MetadataMapRoad(string Id,double X1,double Y1,double X2,double Y2,double Width);
public sealed record MetadataMapGeometry(string Source,string Unit,double Width,double Height,
    IReadOnlyList<MetadataMapBox> Boxes,IReadOnlyList<MetadataMapRoad> Roads);
public sealed record WorldMetadataDocument(int SchemaVersion, string Id, string GameProfile, string Scale,
    MetadataCoverage Coverage, IReadOnlyList<string> RemainingScopes, MetadataSpace Space,
    string RecipeSha256, string CatalogSha256, IReadOnlyList<WorldMetadataNode> Nodes,
    IReadOnlyList<WorldMetadataConnection> Connections,
    IReadOnlyDictionary<string, MetadataStructureDefinition> StructureDefinitions,MetadataMapGeometry? MapGeometry=null);
public interface IWorldMetadataGenerator
{
    WorldMetadataDocument Generate(WorldMetadataRecipe recipe, WorldMetadataCatalog catalog, CancellationToken token = default);
}
