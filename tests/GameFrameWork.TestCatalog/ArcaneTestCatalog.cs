using GameFrameWork.Core.Planning;

namespace GameFrameWork.TestCatalog;

/// <summary>Independent synthetic contract fixture, not a production Fantasy pack.</summary>
public sealed class ArcaneTestCatalog : ISettlementCatalogProvider
{
    private static IReadOnlySet<string> Set(params string[] ids) => new HashSet<string>(ids,StringComparer.Ordinal);
    public SettlementCatalogDescriptor Descriptor => new("test.arcane","Synthetic Arcane",Set("test.mana"),Set(),
        new(Set("test.ritual"),Set("test.magic"),Set("test.sanctum"),Set("test.leycraft"),Set("test.mana")));
    public IReadOnlyList<BuildingDefinition> Structures => [new("test.arcane-hall","test.ritual",5,
        new HashSet<SettlementClass> { SettlementClass.Town },new HashSet<SettlementRole>(),
        [new("small",8,8,10,5)],SiteRules:[new("arrival",SiteFeatureKind.Forecourt,SiteBand.Front,2)],
        Category:"test.magic",AutomaticDemand:false,SitingProfile:new("test.hall-siting",1,["test.rule"],["test.authored-mechanics"],
            Set("test.sanctum"),Set("test.leycraft"),Set(),[new("mana","test.mana",SitingNetworkMode.Utility)],
            new HashSet<SiteFeatureKind> { SiteFeatureKind.Forecourt,SiteFeatureKind.AccessWalkway }))];
    public ISettlementCatalogPolicy Policy { get; } = new ArcanePolicy();
    public ISettlementDemandPolicy Demand { get; } = new ArcaneDemand();
    public IInfrastructureDesignStrategy Infrastructure { get; } = new ArcaneInfrastructure();
    private sealed class ArcanePolicy : ISettlementCatalogPolicy
    {
        public string Id => "test.ward-algorithm";
        public BuildingDefinition Resolve(BuildingDefinition definition,string technology) => definition;
        public IReadOnlySet<string> RequiredCapabilities(BuildingDefinition definition,string technology) => Set("test.mana");
        public IReadOnlySet<string> RequiredCapabilities(MockupWorldDesign design) => Set();
        public IReadOnlyList<string> ValidateSupply(MockupWorldDesign design,StructureResourceRequirement requirement,
            SitingResourceSource source,StructureSitingContext context,CancellationToken token=default) => source.Qualities.Contains("attuned")?[]:["test.ward-requires-attuned-mana"];
        public bool RequiresNeighborPolicy(IReadOnlySet<string> first,IReadOnlySet<string> second) => false;
    }
    private sealed class ArcaneDemand : ISettlementDemandPolicy
    {
        public BuildingProgram Plan(IReadOnlyList<BuildingDefinition> catalog,SettlementDemand settlement) =>
            new([new("test.arcane-hall",1,"Authored arcane precinct demand.")],[]);
    }
    private sealed class ArcaneInfrastructure : IInfrastructureDesignStrategy
    {
        public InfrastructureDesignResult Plan(MockupWorldDesign design,CancellationToken token=default)
        { token.ThrowIfCancellationRequested(); return new([],design.InfrastructureRules?.Count>0?["TEST_INFRASTRUCTURE_UNSUPPORTED"]:[]); }
    }
}
