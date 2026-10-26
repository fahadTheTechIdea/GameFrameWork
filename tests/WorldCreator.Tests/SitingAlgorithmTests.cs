using WorldCreator.Catalogs.Modern;
using System.Diagnostics;
using System.Text.Json;
using WorldCreator.Core;
using WorldCreator.Core.Geometry;
using WorldCreator.Core.Planning;
using WorldCreator.Worlds;

internal static class SitingAlgorithmTests
{
    public static async Task RunAsync(string? cliPath=null)
    {
        var catalog=new ModernCatalog().Structures;
        int technologies=0;
        foreach (var definition in catalog)
        {
            Check(definition.SitingProfile is not null,"profile for "+definition.Id);
            definition.Validate();
            foreach (var technology in definition.SitingProfile!.AllowedTechnologies)
            { ModernSitingProfiles.Resolve(definition,technology).Validate(); technologies++; }
        }
        var engine=new DesertWorldGenerator(TestCatalogs.Registry);
        var cases=new[] { "warehouse","factory","primary-school","hospital","fire-station","football-stadium",
            "hotel","data-center","aquaculture-facility","solar-farm","wind-farm","water-treatment-plant",
            "wastewater-treatment-plant","desalination-plant","observatory","detached-house","supermarket","bank","mosque" };
        foreach (var id in cases)
        {
            var world=await engine.GenerateAsync(Create(id));
            Check(world.Siting?.Status==SitingEvaluationStatus.Passed && world.Siting.Allocations.Count>0,"complete contextual generation: "+id);
            Check(world.Siting!.Evaluations.All(e=>e.SourceIds.Count>0),"research provenance: "+id);
            var roundtrip=JsonSerializer.Deserialize<WorldDocument>(JsonSerializer.Serialize(world,WorldJson.CreateOptions()),WorldJson.CreateOptions())!;
            Check(roundtrip.Siting!.Allocations.Count==world.Siting.Allocations.Count,"assessment/allocations roundtrip");
        }
        foreach (var technology in new[] { FacilityTechnology.NuclearOnceThrough,FacilityTechnology.NuclearRecirculating,FacilityTechnology.NuclearAtmospheric })
        {
            var world=await engine.GenerateAsync(Create("power-station",technology));
            Check(world.Siting!.Status==SitingEvaluationStatus.Passed && world.SiteFeatures!.Any(f=>f.Kind==SiteFeatureKind.CoolingPlant) &&
                world.SiteFeatures!.Any(f=>f.Kind==SiteFeatureKind.SecurityCompound),"nuclear conditional services and surroundings");
            Check(world.Siting.Allocations.Any(a=>a.RequirementId=="shutdown-heat"),"shutdown heat is a separate dependency");
            if (technology==FacilityTechnology.NuclearAtmospheric)
                Check(!world.Siting.Allocations.Any(a=>a.RequirementId is "cooling-intake" or "cooling-makeup"),"atmospheric technology does not inherit water cooling");
        }
        var infrastructure=catalog.Where(d=>d.PlacementKind!=BuildingPlacementKind.FrontageSite).ToArray();
        foreach (var definition in infrastructure)
        {
            var world=await engine.GenerateAsync(ContextualInfrastructure(definition.Id));
            Check(world.Siting!.Status==SitingEvaluationStatus.Passed && world.Infrastructure!.Count==1,"contextual infrastructure: "+definition.Id);
        }
        var baseline=Create();
        await Reject(baseline with { Design=baseline.Design! with { Siting=null } },"SITING_PLAN_REQUIRED");
        await Reject(ChangeDistrict(baseline,DistrictUse.Residential),"SITING_DISTRICT_INCOMPATIBLE");
        await Reject(ChangeDistrict(Create("factory",FacilityTechnology.CleanLight),DistrictUse.Residential),"SITING_DISTRICT_INCOMPATIBLE");
        Check((await engine.GenerateAsync(ChangeDistrict(Create("factory",FacilityTechnology.CleanLight),DistrictUse.MixedUse))).Siting!.Status==SitingEvaluationStatus.Passed,
            "clean light activity is allowed in compatible mixed use");
        await Reject(ChangeDistrict(Create("factory",FacilityTechnology.HeavyProcess),DistrictUse.MixedUse),"SITING_DISTRICT_INCOMPATIBLE");
        await Reject(Context(baseline,c=>c with { Dependencies=c.Dependencies.Where(d=>d.RequirementId!="power").ToArray() }),"SITING_DEPENDENCY_UNRESOLVED");
        await Reject(Context(Create("power-station"),c=>c with { Technology=FacilityTechnology.Standard }),"SITING_TECHNOLOGY_UNRESOLVED");
        await Reject(Sources(baseline,s=>s.Kind==SitingResourceKind.PotableWater?s with { Qualities=new HashSet<string> { "reliable","marine" } }:s),"SITING_DEPENDENCY_FAILED");
        await Reject(Sources(baseline,s=>s.Kind==SitingResourceKind.Electricity?s with { Kind=SitingResourceKind.Fuel }:s),"SITING_DEPENDENCY_FAILED");
        await Reject(Sources(baseline,s=>s with { Unit="wrong-units" }),"SITING_DEPENDENCY_FAILED");
        await Reject(Sources(baseline,s=>s with { Capacity=.01 }),"SITING_DEPENDENCY_FAILED");
        var insufficientLink=baseline with { Design=baseline.Design! with { Siting=baseline.Design.Siting! with {
            Links=baseline.Design.Siting.Links.Select(l=>l with { Capacity=.01 }).ToArray() } } };
        await Reject(insufficientLink,"SITING_DEPENDENCY_FAILED");
        var wrongCommodity=baseline with { Design=baseline.Design! with { Siting=baseline.Design.Siting! with {
            Links=baseline.Design.Siting.Links.Select(l=>l with { Services=new HashSet<string> { SitingResourceKind.Fuel } }).ToArray() } } };
        await Reject(wrongCommodity,"SITING_DEPENDENCY_FAILED");
        var disconnected=baseline with { Design=baseline.Design! with { Paths=baseline.Design.Paths.Select(p=>p.Role=="utility"?
            p with { Centerline=new([new(32,20),new(64,20)]) }:p).ToArray() } };
        await Reject(disconnected,"SITING_DEPENDENCY_FAILED");
        var narrow=baseline with { Design=baseline.Design! with { RoadRules=[baseline.Design.RoadRules[0] with { Profile=new(3,1,0,0,0,0) }] } };
        await Reject(narrow,"BUILDING_ACCESS_ROAD_UNDERSIZED");
        var narrowRoute=baseline with { Design=baseline.Design! with {
            Paths=baseline.Design.Paths.Append(new("narrow-upstream","world",new([new(32,16),new(32,64)]),"road")).ToArray(),
            RoadRules=baseline.Design.RoadRules.Append(new("narrow-upstream",RoadClass.Local,new(3,1,0,0,0,0))).ToArray(),
            SettlementRules=baseline.Design.SettlementRules.Select(r=>r with { Siting=r.Siting! with {
                Dependencies=r.Siting.Dependencies.Select(b=>b.RequirementId=="freight"?b with { LinkIds=b.LinkIds.Append("narrow-link").ToArray() }:b).ToArray() } }).ToArray(),
            Siting=baseline.Design.Siting! with { Sources=baseline.Design.Siting.Sources.Select(s=>s.Id=="freight"?s with { Port=new(32,16) }:s).ToArray(),
                Links=baseline.Design.Siting.Links.Append(new("narrow-link","narrow-upstream",SitingNetworkMode.Road,10000,"game-service-units","narrow-trunk",
                    Services:new HashSet<string> { SitingResourceKind.Freight })).ToArray() } } };
        await Reject(narrowRoute,"SITING_DEPENDENCY_FAILED");
        var nuclear=Create("power-station",FacilityTechnology.NuclearOnceThrough);
        await Reject(Context(nuclear,c=>c with { Dependencies=c.Dependencies.Where(d=>d.RequirementId!="shutdown-heat").ToArray() }),"SITING_DEPENDENCY_UNRESOLVED");
        await Reject(Sources(nuclear,s=>s.Kind==SitingResourceKind.CoolingWater?s with { Origin=SitingResourceOrigin.UtilityNetwork }:s),"SITING_DEPENDENCY_FAILED");
        var decorative=nuclear with { Design=nuclear.Design! with { TerrainRules=nuclear.Design.TerrainRules.Select(t=>t.RegionId=="water"?t with { Kind=CellKind.Sand }:t).ToArray() } };
        await Reject(decorative,"SITING_SOURCE_WATER_REGION_INVALID");
        var intake=Create("water-treatment-plant");
        var outfall=new SitingResourceSource("upstream-outfall",SitingResourceKind.WaterOutfall,SitingResourceOrigin.UtilityNetwork,"world",new(32,64),1000,"game-service-units",new HashSet<string>(),"authored-upstream-discharge");
        var contaminated=intake with { Design=intake.Design! with { Siting=intake.Design.Siting! with {
            Sources=intake.Design.Siting.Sources.Select(s=>s.Id=="source-water"?s with { UpstreamSourceIds=[outfall.Id] }:s).Append(outfall).ToArray() } } };
        await Reject(contaminated,"SITING_DEPENDENCY_FAILED");
        var exclusion=Create("hospital");
        exclusion=exclusion with { Design=exclusion.Design! with { Siting=exclusion.Design.Siting! with {
            Exclusions=[new("hazard","site",new HashSet<string> { "sensitive" },"authored-hazard-overlay")] } } };
        await Reject(exclusion,"SITING_EXCLUSION_CONFLICT");
        var publicGreen=Create("detached-house");
        publicGreen=publicGreen with { Design=publicGreen.Design! with { Siting=publicGreen.Design.Siting! with {
            Exclusions=[new("public-green-hazard","site",new HashSet<string> { "public-green" },"authored-green-hazard")] } } };
        await Reject(publicGreen,"SITING_GREEN_EXCLUSION_CONFLICT");
        var mixed=Mixed();
        var mixedWorld=await engine.GenerateAsync(mixed);
        Check(mixedWorld.Siting!.Evaluations.Any(e=>e.RuleId=="SITING_SEPARATION_industry-neighbor" && e.Status==SitingEvaluationStatus.Passed),
            "separate compatible residential and industrial districts");
        await Reject(mixed with { Design=mixed.Design! with { Siting=mixed.Design.Siting! with { Separations=[] } } },"SITING_SEPARATION_UNRESOLVED");
        var tooClose=mixed with { Design=mixed.Design! with { Siting=mixed.Design.Siting! with {
            Separations=[mixed.Design.Siting.Separations[0] with { MinimumMeters=1000 }] } } };
        await Reject(tooClose,"SITING_SEPARATION_industry-neighbor");
        await Reject(tooClose with { Design=tooClose.Design! with { SettlementRules=tooClose.Design.SettlementRules.Reverse().ToArray() } },"SITING_SEPARATION_industry-neighbor");
        var homeObject=mixedWorld.Design!.Objects.Single(o=>o.Kind=="detached-house");
        var fakeProducer=Sources(mixed,s=>s.Id=="power"?s with { ProviderObjectId=homeObject.Id,Port=homeObject.VisualSource!.PositionMeters }:s);
        await Reject(fakeProducer,"SITING_PROVIDER_EXPORT_INVALID");
        var missingProducer=Sources(mixed,s=>s.Id=="power"?s with { ProviderObjectId="missing-provider" }:s);
        await Reject(missingProducer,"SITING_PROVIDER_EXPORT_INVALID");
        var suppliedFactory=SuppliedFactory();
        var suppliedWorld=await engine.GenerateAsync(suppliedFactory);
        Check(suppliedWorld.Siting!.Status==SitingEvaluationStatus.Passed && suppliedWorld.Siting.Allocations.Any(a=>
            a.ObjectId.StartsWith("secondary-",StringComparison.Ordinal) && a.SourceId=="secondary-power"),"actual power station supplies a factory");
        var secondaryObject=suppliedWorld.Design!.Objects.Single(o=>o.Kind=="factory");
        var primaryObject=suppliedWorld.Design.Objects.Single(o=>o.Kind=="power-station");
        var secondaryPort=new WorldPoint(secondaryObject.VisualSource!.PositionMeters.X,secondaryObject.VisualSource.Footprint!.Vertices.Min(p=>p.Y));
        var primaryEnd=suppliedFactory.Design!.Paths.Single(p=>p.Id=="primary-service-power").Centerline.Points[^1];
        var cyclic=suppliedFactory with { Design=suppliedFactory.Design with {
            BuildingCatalog=suppliedFactory.Design.BuildingCatalog.Select(d=>d.Id=="factory"?d with { SitingProfile=ModernSitingProfiles.Resolve(d,FacilityTechnology.HeavyProcess).SitingProfile! with {
                Id="explicit-test-generation-factory",Provides=new HashSet<string> { SitingResourceKind.ParentFacility,SitingResourceKind.Electricity } } }:d).ToArray(),
            Paths=suppliedFactory.Design.Paths.Select(p=>p.Id=="primary-service-power"?p with { RegionId="combined-world",
                Centerline=new([secondaryPort,new(secondaryPort.X,64),primaryEnd]) }:p).ToArray(),
            Siting=suppliedFactory.Design.Siting! with { Sources=suppliedFactory.Design.Siting.Sources.Select(s=>s.Id=="primary-power"?
                s with { Origin=SitingResourceOrigin.Facility,RegionId="combined-world",ProviderObjectId=secondaryObject.Id,
                    Port=secondaryPort,Capacity=secondaryObject.VisualSource.ServiceCapacity!.Value }:s).ToArray() } } };
        await Reject(cyclic,"SITING_PROVIDER_DEPENDENCY_CYCLE");
        var multiple=Create("warehouse",copies:2);
        var first=await engine.GenerateAsync(multiple);
        double quantity=first.Siting!.Allocations.First(a=>a.RequirementId=="power").Quantity;
        await Reject(Sources(multiple,s=>s.Id=="power"?s with { Capacity=quantity*1.5 }:s),"SITING_DEPENDENCY_FAILED");
        var reorder=baseline with { Design=baseline.Design! with { Paths=baseline.Design.Paths.Reverse().ToArray(),
            Siting=baseline.Design.Siting! with { Sources=baseline.Design.Siting.Sources.Reverse().ToArray(),Links=baseline.Design.Siting.Links.Reverse().ToArray() } } };
        Check(JsonSerializer.Serialize((await engine.GenerateAsync(baseline)).Siting,WorldJson.CreateOptions())==
            JsonSerializer.Serialize((await engine.GenerateAsync(reorder)).Siting,WorldJson.CreateOptions()),"deterministic allocation/evaluation order");
        await Reject(MockupGenerationFixture.Create() with { Design=MockupGenerationFixture.Create().Design! with { Siting=null } },"SITING_PLAN_REQUIRED");
        using var cancellation=new CancellationTokenSource(); cancellation.Cancel();
        try { await engine.GenerateAsync(baseline,cancellation.Token); throw new Exception("Missing cancellation"); }
        catch (OperationCanceledException) { }
        if (cliPath is not null) await Cli(cliPath,baseline,ChangeDistrict(baseline,DistrictUse.Residential));
        Console.WriteLine($"PASS: siting: {catalog.Count} catalog profiles / {technologies} technology resolutions; {cases.Length+3} building + {infrastructure.Length} infrastructure + 2 mixed/provider contextual worlds; typed supplies, capacity, districts, provider cycles, nuclear cooling, surroundings, JSON, negatives and cancellation.");

        async Task Reject(WorldRequest request,string code)
        {
            try { await engine.GenerateAsync(request); throw new Exception("Expected siting rejection: "+code); }
            catch (WorldGenerationException ex) when (ex.Issues.Any(i=>i.StartsWith(code,StringComparison.Ordinal))) { }
        }
    }

    private static WorldRequest SuppliedFactory()
    {
        var primary=Create("power-station",FacilityTechnology.ThermalAirCooled);
        var secondary=Create("factory",FacilityTechnology.HeavyProcess);
        double shift=primary.Width+32;
        int width=(int)shift+secondary.Width,height=Math.Max(primary.Height,secondary.Height);
        var provider=new SettlementDesignPipeline(TestCatalogs.Registry).Build(primary).Design.Objects.Single(o=>o.Kind=="power-station");
        var providerPort=new WorldPoint(provider.VisualSource!.PositionMeters.X,provider.VisualSource.Footprint!.Vertices.Min(p=>p.Y));
        var regions=new List<VisualRegion> { new("combined-world",MockupGenerationFixture.Rect(0,0,width,height),"combined-utility-industrial-design") };
        var paths=new List<VisualDesignPath>(); var roads=new List<MockupRoadRule>(); var rules=new List<SettlementRegionRule>();
        var terrain=new List<MockupTerrainRule> { new("combined-world",CellKind.Sand,-1,2,"combined-base") };
        var districts=new List<SitingDistrict>(); var sources=new List<SitingResourceSource>(); var links=new List<SitingNetworkLink>();
        foreach (var (request,prefix,dx) in new[] { (primary,"primary-",0d),(secondary,"secondary-",shift) })
        {
            var design=request.Design!;
            regions.AddRange(design.Blueprint.Regions.Select(r=>r with { Id=prefix+r.Id,
                Outline=new(r.Outline.Vertices.Select(v=>v.Offset(dx,0)).ToArray()) }));
            paths.AddRange(design.Paths.Select(p=>p with { Id=prefix+p.Id,RegionId=prefix+p.RegionId,
                Centerline=new(p.Centerline.Points.Select(v=>v.Offset(dx,0)).ToArray()) }));
            roads.AddRange(design.RoadRules.Select(r=>r with { PathId=prefix+r.PathId }));
            rules.AddRange(design.SettlementRules.Select(r=>r with { Id=prefix+r.Id,RegionId=prefix+r.RegionId,FrontagePathId=prefix+r.FrontagePathId,
                Settlement=new(SettlementClass.Town,SettlementRole.Mixed,1000),
                Siting=r.Siting! with { DistrictId=prefix+r.Siting.DistrictId,Dependencies=r.Siting.Dependencies.Select(b=>b with {
                    SourceId=prefix+b.SourceId,LinkIds=b.LinkIds.Select(i=>prefix+i).ToArray() }).ToArray() } }));
            terrain.AddRange(design.TerrainRules.Select(t=>t with { RegionId=prefix+t.RegionId,Kind=CellKind.Sand,RuleId=prefix+t.RuleId }));
            districts.AddRange(design.Siting!.Districts.Select(d=>d with { Id=prefix+d.Id,RegionId=prefix+d.RegionId }));
            sources.AddRange(design.Siting.Sources.Select(s=>s with { Id=prefix+s.Id,RegionId=prefix+s.RegionId,Port=s.Port.Offset(dx,0) }));
            links.AddRange(design.Siting.Links.Select(l=>l with { Id=prefix+l.Id,PathId=prefix+l.PathId }));
        }
        var from=paths.Single(p=>p.Id=="primary-approach").Centerline.Points[^1];
        var to=paths.Single(p=>p.Id=="secondary-approach").Centerline.Points[0];
        paths.Add(new("district-connector","combined-world",new([from,to]),"road"));
        roads.Add(new("district-connector",RoadClass.Collector,new(3,4,2,2,0,0)));
        int powerIndex=sources.FindIndex(s=>s.Id=="secondary-power");
        sources[powerIndex]=sources[powerIndex] with { Origin=SitingResourceOrigin.Facility,RegionId="combined-world",
            ProviderObjectId="primary-"+provider.Id,Port=providerPort,Capacity=provider.VisualSource.ServiceCapacity!.Value };
        int pathIndex=paths.FindIndex(p=>p.Id=="secondary-service-power");
        paths[pathIndex]=paths[pathIndex] with { RegionId="combined-world",
            Centerline=new([providerPort,new(providerPort.X,64),paths[pathIndex].Centerline.Points[^1]]) };
        return new(width,height,42,Biome.Desert,1,new(1,new(1,"synthetic-provider-layout","r1","synthetic-style",regions,[]),1,
            paths,roads,rules,terrain,[],primary.Design!.BuildingCatalog.Concat(secondary.Design!.BuildingCatalog).ToArray(),
            Catalog:ModernCatalog.Selection,Siting:new(1,districts,sources,links,[],[])));
    }

    private static WorldRequest ContextualInfrastructure(string id) => InfrastructureAlgorithmTests.Create(id);

    public static WorldRequest WithInfrastructureContext(WorldRequest request)
    {
        var design=request.Design!; var rule=design.InfrastructureRules![0]; var definition=design.BuildingCatalog[0];
        var profile=definition.SitingProfile!;
        var obstacle=design.Blueprint.Regions.Single(r=>r.Id==rule.ObstacleRegionId);
        var points=design.Paths.Single(p=>p.Id==rule.AlignmentPathId).Centerline.Points;
        var paths=design.Paths.ToList(); var sources=new List<SitingResourceSource>();
        var links=new List<SitingNetworkLink>(); var bindings=new List<SitingDependencyBinding>();
        foreach (var requirement in profile.Requirements)
        {
            string approach=requirement.Id=="maintenance" && rule.ServicePathId is not null?rule.ServicePathId:rule.StartConnectionPathId;
            var path=design.Paths.Single(p=>p.Id==approach);
            var sourcePoint=requirement.RequiredOrigin==SitingResourceOrigin.SurfaceWater && requirement.Mode!=SitingNetworkMode.Waterway?
                new WorldPoint(obstacle.Outline.Vertices.Average(p=>p.X),obstacle.Outline.Vertices.Average(p=>p.Y)):path.Centerline.Points[0];
            WorldPoint target=requirement.Id=="maintenance"?path.Centerline.Points[^1]:points[0];
            string pathId=path.Id;
            if (requirement.Mode==SitingNetworkMode.SiteInterface)
            { pathId="interface-"+requirement.Id; paths.Add(new(pathId,"world",new([sourcePoint,target]),"site-interface")); }
            string sourceId="source-"+requirement.Id,linkId="link-"+requirement.Id;
            sources.Add(new(sourceId,requirement.Kind,requirement.RequiredOrigin ?? SitingResourceOrigin.UtilityNetwork,
                requirement.RequiredOrigin==SitingResourceOrigin.SurfaceWater?obstacle.Id:"world",sourcePoint,1000,"game-service-units",
                (requirement.RequiredQualities ?? new HashSet<string>()).ToHashSet(StringComparer.Ordinal),"authored-infrastructure-supply"));
            links.Add(new(linkId,pathId,requirement.Mode,1000,"game-service-units","authored-infrastructure-link",Services:new HashSet<string> { requirement.Kind }));
            bindings.Add(new(requirement.Id,sourceId,[linkId],target));
        }
        return request with { Design=design with { SchemaVersion=1,Catalog=ModernCatalog.Selection,Paths=paths,
            InfrastructureRules=[rule with { Siting=new("district",FacilityTechnology.Standard,bindings) }],
            Siting=new(1,[new("district","world",rule.SettlementId,profile.AllowedDistricts.Order().First(),"infrastructure-district")],sources,links,[],[]) } };
    }

    private static WorldRequest Mixed()
    {
        var request=Create("factory",FacilityTechnology.HeavyProcess);
        var design=request.Design!; var home=new ModernCatalog().Structures.Single(d=>d.Id=="detached-house");
        double oldRight=design.Blueprint.Regions.Single(r=>r.Id=="site").Outline.Vertices.Max(v=>v.X);
        double homeLeft=oldRight+24,homeRight=homeLeft+132,bottom=design.Blueprint.Regions.Single(r=>r.Id=="site").Outline.Vertices.Max(v=>v.Y);
        int width=(int)homeRight+32;
        var homeBoundary=MockupGenerationFixture.Rect(homeLeft,64,homeRight,bottom);
        var regions=design.Blueprint.Regions.Select(r=>r.Id=="world"?r with { Outline=MockupGenerationFixture.Rect(0,0,width,request.Height) }:r)
            .Append(new("homes",homeBoundary,"housing-neighborhood")).ToArray();
        var paths=design.Paths.Select(p=>p with { Centerline=new(p.Centerline.Points.Select((v,i)=>
            i==p.Centerline.Points.Count-1 ? new WorldPoint(homeRight+16,v.Y) : v).ToArray()) })
            .Append(new("home-walk","world",new([new(32,64),new(homeRight+16,64)]),"pedestrian")).ToArray();
        var dependencies=home.SitingProfile!.Requirements.Select(r=>new SitingDependencyBinding(r.Id,
            r.Kind==SitingResourceKind.PedestrianCatchment?"home-access":design.Siting!.Sources.First(s=>s.Kind==r.Kind).Id,
            [r.Kind==SitingResourceKind.PedestrianCatchment?"home-access-link":design.Siting!.Links.First(l=>l.Services!.Contains(r.Kind)).Id])).ToArray();
        var rule=new SettlementRegionRule("homes","homes","settlement","approach","neighborhood beside a separately planned employment district",132,132,
            new(home.Id,20,20,20,1,1),[],"small",1,design.SettlementRules[0].Settlement,new("homes",FacilityTechnology.Standard,dependencies));
        return request with { Width=width,Design=design with { Blueprint=design.Blueprint with { Regions=regions },Paths=paths,
            BuildingCatalog=design.BuildingCatalog.Append(home).ToArray(),SettlementRules=design.SettlementRules.Append(rule).ToArray(),
            Siting=design.Siting! with { Districts=design.Siting.Districts.Append(new("homes","homes","settlement",DistrictUse.Residential,"residential-district")).ToArray(),
                Sources=design.Siting.Sources.Append(new("home-access",SitingResourceKind.PedestrianCatchment,SitingResourceOrigin.UtilityNetwork,
                    "world",new(32,64),10000,"game-service-units",new HashSet<string>(),"neighborhood-users")).ToArray(),
                Links=design.Siting.Links.Append(new("home-access-link","home-walk",SitingNetworkMode.Pedestrian,10000,"game-service-units","neighborhood-walk",
                    Services:new HashSet<string> { SitingResourceKind.PedestrianCatchment })).ToArray(),
                Separations=[new("industry-neighbor","heavy-impact","sensitive",20,"explicit synthetic game parameter; not regulation","R05")] } } };
    }

    public static WorldRequest Create(string id="warehouse",string? selected=null,int copies=1)
    {
        var definition=new ModernCatalog().Structures.Single(d=>d.Id==id);
        var technology=selected ?? (id=="warehouse"?FacilityTechnology.GeneralStorage:definition.SitingProfile!.AllowedTechnologies.Order().First());
        var resolved=ModernSitingProfiles.Resolve(definition,technology);
        var variant=definition.SizeVariants![0];
        double right=64+(variant.WidthMeters+120)*copies,bottom=64+variant.DepthMeters+120;
        int width=(int)Math.Ceiling(right+32),height=(int)Math.Ceiling(bottom+32);
        var regions=new List<VisualRegion> { new("world",MockupGenerationFixture.Rect(0,0,width,height),"synthetic-world"),
            new("site",MockupGenerationFixture.Rect(64,64,right,bottom),"authored-facility-site"),
            new("water",MockupGenerationFixture.Rect(0,0,16,height),"authored-water-source") };
        var paths=new List<VisualDesignPath> { new("approach","world",new([new(32,64),new(right+16,64)]),"road") };
        var sources=new List<SitingResourceSource>(); var links=new List<SitingNetworkLink>(); var bindings=new List<SitingDependencyBinding>();
        foreach (var requirement in resolved.SitingProfile!.Requirements.Where(r=>r.WhenTechnologies is null || r.WhenTechnologies.Contains(technology)))
        {
            var origin=requirement.RequiredOrigin ?? SitingResourceOrigin.UtilityNetwork;
            var port=origin==SitingResourceOrigin.SurfaceWater ? new WorldPoint(8,64) : new WorldPoint(32,64);
            if (requirement.Id=="water-outfall" && origin==SitingResourceOrigin.SurfaceWater) port=new(8,80);
            string sourceRegion=requirement.SourceContainsSite?"site":origin==SitingResourceOrigin.SurfaceWater?"water":"world";
            if (requirement.SourceContainsSite) port=new(64,64);
            sources.Add(new(requirement.Id,requirement.Kind,origin,sourceRegion,port,100000,"game-service-units",
                (requirement.RequiredQualities ?? new HashSet<string>()).ToHashSet(StringComparer.Ordinal),"synthetic-resource:"+requirement.Id));
            if (requirement.SourceContainsSite) { bindings.Add(new(requirement.Id,requirement.Id,[])); continue; }
            string pathId="service-"+requirement.Id;
            string role=requirement.Mode switch { SitingNetworkMode.Road=>"road",SitingNetworkMode.Pedestrian=>"pedestrian",
                SitingNetworkMode.Railway=>"railway",SitingNetworkMode.Waterway=>"waterway",SitingNetworkMode.SiteInterface=>"site-interface",_=>"utility" };
            if (requirement.Mode==SitingNetworkMode.Road) pathId="approach";
            else paths.Add(new(pathId,"world",port.Y==64 ? new([port,new(right+16,64)]) :
                new([port,new(24,port.Y),new(24,64),new(right+16,64)]),role));
            string linkId="link-"+requirement.Id;
            links.Add(new(linkId,pathId,requirement.Mode,100000,requirement.Unit,"synthetic-link:"+requirement.Id,
                Services:new HashSet<string> { requirement.Kind }));
            bindings.Add(new(requirement.Id,requirement.Id,[linkId]));
        }
        var context=new StructureSitingContext("district",technology,bindings);
        return new(width,height,42,Biome.Desert,1,new(1,new(1,"synthetic-siting-layout","layout-r1","synthetic-style",regions,[]),1,
            paths,[new("approach",RoadClass.Collector,new(3,4,2,2,0,0))],
            [new("facility","site","settlement","approach","serve the explicit contextual "+id+" program",
                (right-64)/copies,(right-64)/copies,new(id,20,20,20,1,1),[],variant.Id,variant.ServiceCapacity,
                new(SettlementClass.Town,definition.AllowedRoles?.Order().First() ?? SettlementRole.Mixed,1000),context)],
            [new("world",CellKind.Sand,0,2,"authored-base"),new("water",CellKind.Water,1,0,"authored-water")],[],[definition],
            Catalog:ModernCatalog.Selection,Siting:new(1,[new("district","site","settlement",resolved.SitingProfile.AllowedDistricts.Order().First(),"synthetic-district")],sources,links,[],[])));
    }

    private static WorldRequest ChangeDistrict(WorldRequest request,string use) => request with { Design=request.Design! with {
        Siting=request.Design.Siting! with { Districts=request.Design.Siting.Districts.Select(d=>d with { Use=use }).ToArray() } } };
    private static WorldRequest Context(WorldRequest request,Func<StructureSitingContext,StructureSitingContext> change) => request with { Design=request.Design! with {
        SettlementRules=request.Design.SettlementRules.Select(r=>r with { Siting=change(r.Siting!) }).ToArray() } };
    private static WorldRequest Sources(WorldRequest request,Func<SitingResourceSource,SitingResourceSource> change) => request with { Design=request.Design! with {
        Siting=request.Design.Siting! with { Sources=request.Design.Siting.Sources.Select(change).ToArray() } } };
    private static async Task Cli(string cliPath,WorldRequest accepted,WorldRequest rejected)
    {
        string root=Path.Combine(Path.GetTempPath(),"gfw-siting-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try
        {
            string requestPath=Path.Combine(root,"request.json"),output=Path.Combine(root,"world");
            await File.WriteAllTextAsync(requestPath,JsonSerializer.Serialize(accepted,WorldJson.CreateOptions()));
            Check(await Run()==0,"contextual CLI success");
            string before=await File.ReadAllTextAsync(Path.Combine(output,"world.json"));
            await File.WriteAllTextAsync(requestPath,JsonSerializer.Serialize(rejected,WorldJson.CreateOptions()));
            Check(await Run()==1 && await File.ReadAllTextAsync(Path.Combine(output,"world.json"))==before,"CLI rejection preserves prior export");
            string audit=await File.ReadAllTextAsync(Path.Combine(root,".gameframework","operations","audit.jsonl"));
            Check(audit.Contains("world.generate.succeeded") && audit.Contains("world.generate.failed"),"contextual CLI audits both outcomes");
            async Task<int> Run()
            {
                var info=new ProcessStartInfo("dotnet") { WorkingDirectory=root,UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true };
                foreach (var arg in new[] { cliPath,"generate-world",requestPath,output }) info.ArgumentList.Add(arg);
                using var process=Process.Start(info)!; var stdout=process.StandardOutput.ReadToEndAsync(); var stderr=process.StandardError.ReadToEndAsync();
                using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(60));
                try { await process.WaitForExitAsync(timeout.Token); } catch (OperationCanceledException) { process.Kill(true); throw; }
                await stdout; await stderr; return process.ExitCode;
            }
        }
        finally { Directory.Delete(root,true); }
    }
    private static void Check(bool condition,string message) { if (!condition) throw new Exception("Siting: "+message); }
}
