namespace WorldCreator.Core.Planning;

public enum StructureRepresentation { SingleStructure, ContextDependentSite, CompoundSite, InfrastructureAssembly }
public enum CatalogPartKind { Structure, GroundArea, Infrastructure, Equipment, Interior }
public sealed record CatalogPartRole(string Role, CatalogPartKind Kind, IReadOnlyList<string> CandidateDefinitionIds);
/// <summary>Authored inventory review, not proof that suggested parts exist in a generated world.
/// A single-building archetype may still participate in a user-requested compound program.</summary>
public sealed record StructureCompositionAssessment(StructureRepresentation Representation,
    string GeometryScope, string ReviewBasis, IReadOnlyList<CatalogPartRole> PartRoles)
{
    public void Validate()
    {
        if (!Enum.IsDefined(Representation) || string.IsNullOrWhiteSpace(GeometryScope) || string.IsNullOrWhiteSpace(ReviewBasis) ||
            PartRoles is null || PartRoles.Count > 128 || PartRoles.Any(p => p is null || !Enum.IsDefined(p.Kind) ||
                !SettlementCatalogRegistry.ValidId(p.Role) || p.CandidateDefinitionIds is null ||
                p.CandidateDefinitionIds.Any(id => !SettlementCatalogRegistry.ValidId(id))) ||
            PartRoles.Select(p => p.Role).Distinct(StringComparer.Ordinal).Count() != PartRoles.Count)
            throw new ArgumentException("CATALOG_COMPOSITION_ASSESSMENT_INVALID");
    }
}
