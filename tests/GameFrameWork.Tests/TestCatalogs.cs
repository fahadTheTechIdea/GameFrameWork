using GameFrameWork.Catalogs.Modern;
using GameFrameWork.Core;
using GameFrameWork.Core.Planning;

internal static class TestCatalogs
{
    public static ISettlementCatalogRegistry Registry { get; } = new SettlementCatalogRegistry([new ModernCatalog()]);

    // Explicit authored synthetic services for geometric regression fixtures. Not a runtime fallback.
    public static WorldRequest Contextualize(WorldRequest request)
    {
        var design=request.Design! with { Catalog=ModernCatalog.Selection };
        if (design.Siting is not null) return request with { Design=design };
        if (design.InfrastructureRules?.Count>0)
        {
            if (design.SettlementRules.Count==0) return SitingAlgorithmTests.WithInfrastructureContext(request with { Design=design });
            var world=design.Blueprint.Regions.Single(r=>r.Id=="world").Outline;
            var infraDesign=design with { SettlementRules=[],Blueprint=design.Blueprint with { Regions=design.Blueprint.Regions.Append(
                new("bridge-district",MockupGenerationFixture.Rect(24,40,world.Vertices.Max(p=>p.X),world.Vertices.Max(p=>p.Y)),"authored-bridge-district")).ToArray() },
                InfrastructureRules=design.InfrastructureRules.Select(r=>r with { RegionId="bridge-district" }).ToArray() };
            var infra=SitingAlgorithmTests.WithInfrastructureContext(request with { Design=infraDesign }).Design!;
            infra=infra with { Siting=infra.Siting! with { Districts=infra.Siting.Districts.Select(d=>d with { RegionId="bridge-district" }).ToArray() } };
            var homes=Contextualize(request with { Design=design with { InfrastructureRules=[] } }).Design!;
            return request with { Design=homes with { Blueprint=infra.Blueprint,InfrastructureRules=infra.InfrastructureRules,
                Paths=homes.Paths.Concat(infra.Paths.Where(p=>!homes.Paths.Any(h=>h.Id==p.Id))).ToArray(),
                Siting=homes.Siting! with { Districts=homes.Siting.Districts.Concat(infra.Siting!.Districts).ToArray(),
                    Sources=homes.Siting.Sources.Concat(infra.Siting.Sources).ToArray(),Links=homes.Siting.Links.Concat(infra.Siting.Links).ToArray() } } };
        }
        var paths=design.Paths.ToList(); var sources=new List<SitingResourceSource>(); var links=new List<SitingNetworkLink>();
        var districts=new List<SitingDistrict>(); var rules=new List<SettlementRegionRule>();
        foreach (var rule in design.SettlementRules)
        {
            var definition=design.BuildingCatalog.Single(d=>d.Id==rule.Envelope.BuildingType);
            string technology=definition.Id switch { "warehouse"=>FacilityTechnology.GeneralStorage,"factory"=>FacilityTechnology.HeavyProcess,
                "workshop"=>FacilityTechnology.CleanLight,_=>definition.SitingProfile!.AllowedTechnologies.Order(StringComparer.Ordinal).First() };
            var profile=new ModernCatalogPolicy().Resolve(definition,technology).SitingProfile!;
            string districtId=rule.Id+"-district";
            districts.Add(new(districtId,rule.RegionId,rule.SettlementId,profile.AllowedDistricts.Order(StringComparer.Ordinal).First(),"authored-test-district"));
            var bindings=new List<SitingDependencyBinding>(); var frontage=design.Paths.Single(p=>p.Id==rule.FrontagePathId);
            foreach (var r in profile.Requirements.Where(r=>r.WhenTechnologies is null || r.WhenTechnologies.Contains(technology)))
            {
                string sourceId=rule.Id+"-"+r.Id,linkId=sourceId+"-link",pathId=sourceId+"-path";
                var port=r.SourceContainsSite?design.Blueprint.Regions.Single(p=>p.Id==rule.RegionId).Outline.Vertices[0]:frontage.Centerline.Points[0];
                sources.Add(new(sourceId,r.Kind,r.RequiredOrigin ?? (r.SourceContainsSite?SitingResourceOrigin.ResourceArea:SitingResourceOrigin.UtilityNetwork),
                    r.SourceContainsSite?rule.RegionId:"world",port,1e9,r.Unit,(r.RequiredQualities ?? new HashSet<string>()).ToHashSet(),"authored-test-service"));
                if (r.SourceContainsSite) { bindings.Add(new(r.Id,sourceId,[])); continue; }
                if (r.Mode==SitingNetworkMode.Road) pathId=frontage.Id;
                else paths.Add(frontage with { Id=pathId,Role=r.Mode==SitingNetworkMode.Pedestrian?"pedestrian":"utility" });
                links.Add(new(linkId,pathId,r.Mode,1e9,r.Unit,"authored-test-service",Services:new HashSet<string> { r.Kind }));
                bindings.Add(new(r.Id,sourceId,[linkId]));
            }
            rules.Add(rule with { Siting=new(districtId,technology,bindings) });
        }
        return request with { Design=design with { Paths=paths,SettlementRules=rules,Siting=new(1,districts,sources,links,[],[]) } };
    }
}
