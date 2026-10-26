using WorldCreator.Catalogs.Modern;
using WorldCreator.Core;
using WorldCreator.Core.Geometry;
using WorldCreator.Core.Planning;
using WorldCreator.Worlds;
using System.Text.Json;

internal static class InfrastructureAlgorithmTests
{
    public static async Task RunAsync()
    {
        var catalog = new ModernCatalog().Structures.Where(d=>d.PlacementKind!=BuildingPlacementKind.FrontageSite).ToArray();
        var engine = new DesertWorldGenerator(TestCatalogs.Registry);
        int generated=0;
        foreach (var definition in catalog)
        foreach (var variant in definition.SizeVariants!)
        {
            var request=Create(definition.Id,variant.Id);
            WorldDocument world;
            try { world=await engine.GenerateAsync(request); }
            catch (WorldGenerationException ex) { throw new Exception(definition.Id+"/"+variant.Id+": "+ex.Message,ex); }
            var structure=world.Infrastructure!.Single();
            Check(structure.Variant.Id==variant.Id && structure.Variant.WidthMeters==variant.WidthMeters &&
                Math.Abs(Math.Abs(structure.Footprint.SignedArea())-variant.WidthMeters*structure.LengthMeters)<1e-7,
                "variant geometry retained: "+definition.Id+"/"+variant.Id);
            Check(structure.Connections.Count>=1 && structure.Reservations.Count>=2 && world.Objects.Any(o=>o.Id==structure.Id),
                "compiled connections and surroundings: "+definition.Id);
            Check(world.InfrastructureLayers!.Any(c=>c.UnderlyingTerrain.Kind is CellKind.Water or CellKind.Rock),
                "underlying obstacle retained: "+definition.Id);
            if (structure.CarriesRoute)
            {
                var p=request.Design!.Paths.Single(p=>p.Id==structure.AlignmentPathId).Centerline.Points;
                Check(Reachable(world,p[0],p[1]),"crossing is traversable: "+definition.Id);
                Check(world.Cells.Any(c=>c.BlocksMovement && c.Kind==(definition.Id=="road-tunnel"?CellKind.Rock:CellKind.Water)),
                    "adjacent obstacle remains blocked: "+definition.Id);
            }
            if (definition.Id is "quay" or "ferry-landing" or "jetty")
            {
                Check(world.Cells.Any(c=>c.DesignRuleId==structure.Id && !c.BlocksMovement),"accessible waterfront deck");
                Check(Reachable(world,structure.Connections[0].PositionMeters,structure.VisualSource.PositionMeters),"connected arrival and waterfront deck");
                if (definition.Id=="quay") Check(structure.AccessLinks.Count==1,"quay service-to-deck walkway");
            }
            if (definition.Id=="canal-lock")
                Check(world.Cells.Any(c=>c.DesignRuleId==structure.Id && c.Kind==CellKind.Water) && structure.PassageMode=="waterway",
                    "lock retains vessel channel and land exclusion");
            var serialized=JsonSerializer.Serialize(world,WorldJson.CreateOptions());
            var restored=JsonSerializer.Deserialize<WorldDocument>(serialized,WorldJson.CreateOptions())!;
            Check(restored.Infrastructure!.Single().Connections.Count==structure.Connections.Count &&
                restored.InfrastructureLayers!.Count==world.InfrastructureLayers!.Count,"infrastructure JSON roundtrip");
            generated++;
            var maximum=Create(definition.Id,variant.Id,variant.DepthMeters);
            var maximumPlan=new ModernInfrastructureDesignStrategy().Plan(maximum.Design!);
            Check(maximumPlan.Issues.Count==0 && maximumPlan.Structures.Single().LengthMeters==variant.DepthMeters,
                "maximum authored span: "+definition.Id+"/"+variant.Id+": "+string.Join(';',maximumPlan.Issues));
            await Reject(Create(definition.Id,variant.Id,variant.DepthMeters+1),"INFRASTRUCTURE_SIZE_OR_CAPACITY_DOES_NOT_FIT");
        }
        // Automatic scaling is actual engine behavior; class alone does not invent demand.
        foreach (var (population,expected) in new[] { (1000,"small"),(8000,"medium"),(50000,"large") })
        {
            var request=Create("road-bridge",expected);
            request=Rule(request,r=>r with { SizeVariantId=null,RequiredCapacity=0,
                Settlement=new(population>=50000?SettlementClass.City:population>=8000?SettlementClass.Town:SettlementClass.Village,
                    SettlementRole.Mixed,population) });
            var scaled=(await engine.GenerateAsync(request)).Infrastructure!.Single();
            Check(scaled.Variant.Id==expected && scaled.RequiredCapacity==Math.Max(10,(int)Math.Ceiling(population*.005)),"contextual road bridge size/capacity");
        }
        var baseline=Create("road-bridge","small");
        await Reject(Rule(baseline,r=>r with { EndConnectionPathId="missing" }),"INFRASTRUCTURE_CONNECTION_MISSING_OR_WRONG_MODE");
        await Reject(Rule(baseline,r=>r with { RequiredCapacity=1000 }),"INFRASTRUCTURE_SIZE_OR_CAPACITY_DOES_NOT_FIT");
        await Reject(Rule(baseline,r=>r with { ObstacleRegionId="world" }),"INFRASTRUCTURE_OBSTACLE_KIND_INVALID");
        await Reject(Rule(baseline,r=>r with { SizeVariantId="unknown" }),"INFRASTRUCTURE_VARIANT_UNKNOWN");
        await Reject(Rule(baseline,r=>r with { ClearanceMeters=double.NaN }),"INFRASTRUCTURE_RULE_INVALID");
        await Reject(Rule(baseline,r=>r with { Settlement=new(SettlementClass.Town,SettlementRole.Mixed,-1) }),"INFRASTRUCTURE_CONTEXT_INVALID");
        var wrongMode=baseline with { Design=baseline.Design! with { Paths=baseline.Design.Paths.Select(p=>p.Id=="approach-a"?p with { Role="pedestrian" }:p).ToArray() } };
        await Reject(wrongMode,"INFRASTRUCTURE_CONNECTION_MISSING_OR_WRONG_MODE");
        var narrow=baseline with { Design=baseline.Design! with { RoadRules=baseline.Design.RoadRules.Select(r=>r.PathId=="approach-a"?r with { Profile=new(2,1,0,0,0,0) }:r).ToArray() } };
        await Reject(narrow,"INFRASTRUCTURE_ACCESS_UNDERSIZED");
        var cityNarrow=Rule(baseline,r=>r with { SizeVariantId=null,RequiredCapacity=0,
            Settlement=new(SettlementClass.City,SettlementRole.Mixed,50000) });
        await Reject(cityNarrow,"INFRASTRUCTURE_ACCESS_UNDERSIZED");
        var mismatched=baseline with { Design=baseline.Design! with { RoadRules=baseline.Design.RoadRules.Select(r=>r.PathId=="approach-a"?
            r with { Profile=new(6,1,1,0,0,0) }:r).ToArray() } };
        await Reject(mismatched,"INFRASTRUCTURE_APPROACH_PROFILE_MISMATCH");
        var shifted=baseline with { Design=baseline.Design! with { Paths=baseline.Design.Paths.Select(p=>p.Id=="approach-a"?
            p with { Centerline=new([new(8,60),new(32,60)]) }:p).ToArray() } };
        await Reject(shifted,"INFRASTRUCTURE_CONNECTION_MISSING_OR_WRONG_MODE");
        var angled=baseline with { Design=baseline.Design! with { Paths=baseline.Design.Paths.Select(p=>p.Id=="approach-a"?
            p with { Centerline=new([new(32,20),new(32,64)]) }:p).ToArray() } };
        await Reject(angled,"INFRASTRUCTURE_APPROACH_ALIGNMENT_INVALID");
        var green=baseline with { Design=baseline.Design! with { LandscapeZones=[new("bad-green","park",
            MockupGenerationFixture.Rect(34,69,36,70),"world","park-rule")] } };
        await Reject(green,"GREEN_ZONE_USE_CONFLICT");
        var overlap=baseline with { Design=baseline.Design! with { InfrastructureRules=[baseline.Design.InfrastructureRules![0],
            baseline.Design.InfrastructureRules[0] with { Id="overlapping-bridge" }] } };
        await Reject(overlap,"INFRASTRUCTURE_RESERVATION_CONFLICT");
        var lockRequest=Create("canal-lock","small");
        await Reject(Rule(lockRequest,r=>r with { ServicePathId=null }),"INFRASTRUCTURE_MAINTENANCE_ACCESS_REQUIRED");
        var brokenCanal=lockRequest with { Design=lockRequest.Design! with { Paths=lockRequest.Design.Paths.Select(p=>p.Id=="approach-a"?
            p with { Centerline=new([new(8,20),new(32,64)]) }:p).ToArray() } };
        await Reject(brokenCanal,"INFRASTRUCTURE_WATERWAY_APPROACH_INVALID");
        var interruptedCanal=lockRequest with { Design=lockRequest.Design! with {
            Blueprint=lockRequest.Design.Blueprint with { Regions=lockRequest.Design.Blueprint.Regions.Append(new("land-sliver",
                MockupGenerationFixture.Rect(10,50,10.1,75),"land")).ToArray() },
            TerrainRules=lockRequest.Design.TerrainRules.Append(new("land-sliver",CellKind.Sand,2,2,"land-interruption")).ToArray() } };
        await Reject(interruptedCanal,"INFRASTRUCTURE_WATERWAY_APPROACH_INVALID");
        await Reject(Rule(Create("quay","small"),r=>r with { ObstacleRegionId="world" }),"INFRASTRUCTURE_OBSTACLE_KIND_INVALID");
        var inland=Create("quay","small");
        inland=inland with { Design=inland.Design! with { Blueprint=inland.Design.Blueprint with {
            Regions=inland.Design.Blueprint.Regions.Select(r=>r.Id=="obstacle"?r with { Outline=MockupGenerationFixture.Rect(0,100,128,128) }:r).ToArray() } } };
        await Reject(inland,"INFRASTRUCTURE_QUAY_SHORELINE_INVALID");
        // A second water mask outside the named obstacle must never be silently bridged.
        var foreignWater=baseline with { Design=baseline.Design! with {
            Blueprint=baseline.Design.Blueprint with { Regions=baseline.Design.Blueprint.Regions.Append(new("other-water",
                MockupGenerationFixture.Rect(33,58,34,70),"water")).ToArray() },
            TerrainRules=baseline.Design.TerrainRules.Append(new("other-water",CellKind.Water,2,0,"foreign-water")).ToArray() } };
        await Reject(foreignWater,"INFRASTRUCTURE_UNEXPECTED_TERRAIN");
        var reordered=baseline with { Design=baseline.Design! with { Paths=baseline.Design.Paths.Reverse().ToArray(),
            RoadRules=baseline.Design.RoadRules.Reverse().ToArray(),TerrainRules=baseline.Design.TerrainRules.Reverse().ToArray() } };
        Check(JsonSerializer.Serialize((await engine.GenerateAsync(baseline)).Design,WorldJson.CreateOptions())==
              JsonSerializer.Serialize((await engine.GenerateAsync(reordered)).Design,WorldJson.CreateOptions()),"order-independent infrastructure design");
        var rotated=Rotate(baseline);
        var rotatedWorld=await engine.GenerateAsync(rotated);
        Check(rotatedWorld.Infrastructure!.Single().LengthMeters==baseline.Design!.Paths[0].Centerline.LengthMeters(),
            "vertical crossing orientation");
        var home=new ModernCatalog().Structures.Single(d=>d.Id=="detached-house");
        var mixed=baseline with { Design=baseline.Design! with {
            Blueprint=baseline.Design.Blueprint with { Regions=baseline.Design.Blueprint.Regions.Append(new("homes",
                MockupGenerationFixture.Rect(8,8,24,40),"housing")).ToArray() },
            Paths=baseline.Design.Paths.Concat(new VisualDesignPath[] {
                new("home-street","world",new([new(4,8),new(24,8)]),"road"),
                new("home-collector","world",new([new(4,8),new(4,64),new(8,64)]),"road") }).ToArray(),
            RoadRules=baseline.Design.RoadRules.Concat(new MockupRoadRule[] {
                new("home-street",RoadClass.Local,new(3,2,0,0,0,0)),
                new("home-collector",RoadClass.Collector,new(3,2,0,0,0,0)) }).ToArray(),
            BuildingCatalog=baseline.Design.BuildingCatalog.Append(home).ToArray(),
            SettlementRules=[new("homes","homes","settlement","home-street","bank neighborhood",16,16,
                new("detached-house",4,2,2,12,10),[],"small",1,baseline.Design.InfrastructureRules![0].Settlement)] } };
        mixed=TestCatalogs.Contextualize(mixed with { Design=mixed.Design! with { Siting=null } });
        var mixedWorld=await engine.GenerateAsync(mixed);
        Check(mixedWorld.Objects.Any(o=>o.Kind=="detached-house") && mixedWorld.Infrastructure!.Count==1 &&
            mixedWorld.Landscape!.Count>0,"housing, gardens, streets and bridge coexist in one settlement");
        using var cancellation=new CancellationTokenSource(); cancellation.Cancel();
        try { await engine.GenerateAsync(baseline,cancellation.Token); throw new Exception("Cancellation not observed"); }
        catch (OperationCanceledException) { }
        Console.WriteLine($"PASS: infrastructure algorithms: {catalog.Length} families / {generated} variant worlds, contextual sizes, connected passage, reserved surroundings, terrain layers, JSON, invalid designs and cancellation.");

        async Task Reject(WorldRequest request,string code)
        {
            try { await engine.GenerateAsync(request); throw new Exception("Expected rejection: "+code); }
            catch (WorldGenerationException ex) when (ex.Issues.Any(i=>i.StartsWith(code,StringComparison.Ordinal))) { }
        }
    }

    public static WorldRequest Create(string id="road-bridge",string variantId="small",double? authoredLength=null)
    {
        var definition=new ModernCatalog().Structures.Single(d=>d.Id==id);
        var variant=definition.SizeVariants!.Single(v=>v.Id==variantId);
        double length=authoredLength ?? Math.Min(variant.DepthMeters*.6,40), y=64, x=32;
        int width=Math.Max(256,(int)Math.Ceiling((length+80)/.5));
        double worldWidth=width*.5;
        var a=new WorldPoint(x,y); var b=new WorldPoint(x+length,y);
        var regions=new List<VisualRegion> { new("world",MockupGenerationFixture.Rect(0,0,worldWidth,128),"synthetic-infrastructure-composition") };
        bool crossing=id is "road-bridge" or "railway-bridge" or "pedestrian-bridge" or "road-tunnel";
        bool bank=crossing || id=="dam";
        string mode=id=="railway-bridge"?"railway":id=="pedestrian-bridge"?"pedestrian":"road";
        double roadWidth=Math.Max(definition.MinimumRoadWidthMeters,variant.MinimumRoadWidthMeters);
        var profile=new RoadCrossSection(roadWidth,1,0,0,0,0);
        var paths=new List<VisualDesignPath> { new("alignment","world",new([a,b]),bank?mode:id=="canal-lock"?"waterway":"structure") };
        var roads=new List<MockupRoadRule>();
        string? end=null,service=null;
        if (bank)
        {
            regions.Add(new("obstacle",MockupGenerationFixture.Rect(x+length*.3,0,x+length*.7,128),id=="road-tunnel"?"rock":"water"));
            paths.Add(new("approach-a","world",new([new(8,y),a]),mode));
            paths.Add(new("approach-b","world",new([b,new(worldWidth-8,y)]),mode));
            roads.Add(new("approach-a",RoadClass.Local,profile)); roads.Add(new("approach-b",RoadClass.Local,profile));
            roads.Add(new("alignment",RoadClass.Local,profile));
            end="approach-b";
        }
        else if (id=="canal-lock")
        {
            double waterHalf=variant.WidthMeters/2-1;
            regions.Add(new("obstacle",MockupGenerationFixture.Rect(0,y-waterHalf,worldWidth,y+waterHalf),"canal"));
            paths.Add(new("approach-a","world",new([new(8,y),a]),"waterway"));
            paths.Add(new("approach-b","world",new([b,new(worldWidth-8,y)]),"waterway"));
            paths.Add(new("service","world",new([new(x+length/2,8),new(x+length/2,y-variant.WidthMeters/2)]),"road"));
            roads.Add(new("service",RoadClass.Local,profile));
            end="approach-b"; service="service";
        }
        else if (id=="quay")
        {
            regions.Add(new("obstacle",MockupGenerationFixture.Rect(0,y+.25,worldWidth,128),"shore-water"));
            paths.Add(new("approach-a","world",new([new(x+length/2,8),new(x+length/2,y-variant.WidthMeters/2)]),"road"));
            roads.Add(new("approach-a",RoadClass.Local,profile));
        }
        else
        {
            regions.Add(new("obstacle",MockupGenerationFixture.Rect(x+length*.2,0,worldWidth,128),"coastal-water"));
            paths.Add(new("approach-a","world",new([new(8,y),a]),"road"));
            roads.Add(new("approach-a",RoadClass.Local,profile));
        }
        return TestCatalogs.Contextualize(new(width,256,42,Biome.Desert,1,new(1,new(1,"synthetic-infrastructure","layout-r1","synthetic-pixel",regions,[]),
            .5,paths,roads,[],[new("world",CellKind.Sand,0,2,"base"),
                new("obstacle",id=="road-tunnel"?CellKind.Rock:CellKind.Water,1,0,"obstacle-mask")],[],[definition],
            InfrastructureRules:[new("structure","world","settlement",id,"alignment","obstacle",
                "connect the authored settlement infrastructure",new(SettlementClass.Town,SettlementRole.Mixed,1000),
                "approach-a",end,service,variantId,1)])));
    }

    private static WorldRequest Rule(WorldRequest request,Func<InfrastructureDesignRule,InfrastructureDesignRule> change) =>
        request with { Design=request.Design! with { InfrastructureRules=request.Design.InfrastructureRules!.Select(change).ToArray() } };
    private static WorldRequest Rotate(WorldRequest request) => request with { Design=request.Design! with {
        Siting=request.Design.Siting! with { Sources=request.Design.Siting.Sources.Select(s=>s with { Port=new(s.Port.Y,s.Port.X) }).ToArray() },
        InfrastructureRules=request.Design.InfrastructureRules!.Select(r=>r with { Siting=r.Siting! with { Dependencies=r.Siting.Dependencies.Select(d=>d.ServicePointMeters is { } p?d with { ServicePointMeters=new(p.Y,p.X) }:d).ToArray() } }).ToArray(),
        Paths=request.Design.Paths.Select(p=>p with { Centerline=new(p.Centerline.Points.Select(v=>new WorldPoint(v.Y,v.X)).ToArray()) }).ToArray(),
        Blueprint=request.Design.Blueprint with { Regions=request.Design.Blueprint.Regions.Select(r=>r with {
            Outline=new(r.Outline.Vertices.Select(v=>new WorldPoint(v.Y,v.X)).Reverse().ToArray()) }).ToArray() } } };
    private static bool Reachable(WorldDocument world,WorldPoint from,WorldPoint to)
    {
        double scale=world.Request.Design!.MetersPerCell;
        var a=new GridPoint((int)(from.X/scale),(int)(from.Y/scale)); var b=new GridPoint((int)(to.X/scale),(int)(to.Y/scale));
        var queue=new Queue<GridPoint>(); var visited=new HashSet<GridPoint> { a }; queue.Enqueue(a);
        while (queue.TryDequeue(out var current))
        {
            if (current==b) return true;
            foreach (var next in new[] { new GridPoint(current.X+1,current.Y),new(current.X-1,current.Y),
                new(current.X,current.Y+1),new(current.X,current.Y-1) })
                if (next.X>=0 && next.Y>=0 && next.X<world.Width && next.Y<world.Height &&
                    !world.At(next.X,next.Y).BlocksMovement && visited.Add(next)) queue.Enqueue(next);
        }
        return false;
    }
    private static void Check(bool value,string message) { if (!value) throw new Exception("Infrastructure: "+message); }
}

