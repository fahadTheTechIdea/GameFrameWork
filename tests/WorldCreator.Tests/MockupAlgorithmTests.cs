using WorldCreator.Catalogs.Modern;
using System.Diagnostics;
using System.Text.Json;
using WorldCreator.Core;
using WorldCreator.Core.Geometry;
using WorldCreator.Core.Planning;
using WorldCreator.Worlds;

internal static class MockupAlgorithmTests
{
    public static async Task RunAsync(string? cliPath)
    {
        var input = MockupGenerationFixture.Create();
        var engine = new DesertWorldGenerator(TestCatalogs.Registry);
        var world = await engine.GenerateAsync(input);
        Check(world.Design is not null && world.Objects.All(o => o.VisualSource is {
            MockupId:"synthetic-industrial-mockup",MockupRevision:"simulated-layout-r1" }),"all objects preserve mockup lineage");
        Check(world.Cells.All(c => c.SourceVisualRegionId is not null && c.DesignRuleId is not null),"all cells preserve source rules");
        Check(world.SiteFeatures!.Any(f => f.Kind == SiteFeatureKind.LoadingYard) &&
            world.SiteFeatures!.Any(f => f.Kind == SiteFeatureKind.AccessWalkway) &&
            world.SiteFeatures!.Any(f => f.Kind == SiteFeatureKind.GreenBuffer),"industrial surroundings are designed");
        Check(world.Vegetation!.Count > 0 && world.Vegetation.All(p => world.Landscape!.Single(z => z.Id == p.ZoneId)
            .Boundary.ContainsPoint(p.PositionMeters)),"planting stays in designed green zones");
        Check(world.Vegetation.All(p => !world.SiteFeatures!.Where(s => s.Kind == SiteFeatureKind.LoadingYard)
            .Any(s => s.Boundary.ContainsPoint(p.PositionMeters))),"no greenery fills loading yards");
        Check(JsonSerializer.Serialize(world) == JsonSerializer.Serialize(await engine.GenerateAsync(input)),"deterministic complete world");
        await Reject(input with { Design = null },"MOCKUP_DESIGN_REQUIRED");
        await Reject(input with { Design = input.Design! with { TerrainRules = [] } },"TERRAIN_COVERAGE_GAP");
        await Reject(input with { Design = input.Design! with { Blueprint = input.Design.Blueprint with { ApprovedRevision = "" } } },
            "MOCKUP_APPROVAL_OR_STYLE_MISSING");
        await Reject(input with { Design = input.Design! with { RoadRules = input.Design.RoadRules.Select(r => r with {
            Profile = new(1,1,0,0,0,0) }).ToArray() } },"BUILDING_ACCESS_ROAD_UNDERSIZED");
        await Reject(input with { Design = input.Design! with { Blueprint = input.Design.Blueprint with {
            Regions = input.Design.Blueprint.Regions.Select(r => r.Id == "industrial-0" ? r with {
                Outline = new([r.Outline.Vertices[0],r.Outline.Vertices[1],r.Outline.Vertices[2]]) } : r).ToArray()
        } } },"SETTLEMENT_REGION_SHAPE_UNSUPPORTED");
        await Reject(input with { Design = input.Design! with { Paths = input.Design.Paths.Select(p => p.Id == "service-road-0" || p.Id.StartsWith("warehouse-row-0-",StringComparison.Ordinal) ? p with {
            Centerline = new([p.Centerline.Points[0],p.Centerline.Points[1].Offset(0,4)]) } : p).ToArray()
        } },"SETTLEMENT_FRONTAGE_NOT_ON_REGION_EDGE");

        var overlay = MockupGenerationFixture.Rect(144,0,188,12);
        var masked = input with { Design = input.Design! with {
            Blueprint = input.Design.Blueprint with { Regions = [..input.Design.Blueprint.Regions,new("rock-focal-region",overlay,"rock-silhouette")] },
            TerrainRules = [..input.Design.TerrainRules,new("rock-focal-region",CellKind.Rock,1,12,"approved-rock-mask")]
        } };
        var maskedWorld = await engine.GenerateAsync(masked);
        Check(maskedWorld.At(40,1).Kind == CellKind.Rock && maskedWorld.At(40,1).HeightMeters == 12 &&
            maskedWorld.At(20,1).Kind == CellKind.Sand,"terrain follows authored regions and elevation");
        var reversed = await engine.GenerateAsync(masked with { Design = masked.Design! with {
            TerrainRules = masked.Design.TerrainRules.Reverse().ToArray(), RoadRules = masked.Design.RoadRules.Reverse().ToArray(),
            SettlementRules = masked.Design.SettlementRules.Reverse().ToArray(), PublicRealmPatterns = masked.Design.PublicRealmPatterns.Reverse().ToArray()
        } });
        Check(JsonSerializer.Serialize(maskedWorld.Cells) == JsonSerializer.Serialize(reversed.Cells) &&
            JsonSerializer.Serialize(maskedWorld.Design) == JsonSerializer.Serialize(reversed.Design),"input enumeration cannot change generation");
        await Reject(masked with { Design = masked.Design! with { TerrainRules = masked.Design.TerrainRules.Select(r => r with { Priority = 0 }).ToArray() } },
            "TERRAIN_PRIORITY_AMBIGUOUS");

        var shifted = input with { Design = input.Design! with {
            Blueprint = input.Design.Blueprint with { Regions = input.Design.Blueprint.Regions.Select(r => r.Id == "industrial-0"
                ? r with { Outline = new(r.Outline.Vertices.Select(p => p.Offset(0,4)).ToArray()) } : r).ToArray() },
            Paths = input.Design.Paths.Select(p => p.Id == "service-road-0" || p.Id.StartsWith("warehouse-row-0-",StringComparison.Ordinal) ? p with {
                Centerline = new(p.Centerline.Points.Select(q => q.Offset(0,4)).ToArray()) } : p).ToArray()
        } };
        shifted=shifted with { Design=shifted.Design! with { Siting=shifted.Design.Siting! with { Sources=shifted.Design.Siting.Sources.Select(s=>s.Id.StartsWith("warehouse-row-0-",StringComparison.Ordinal)?s with { Port=s.Port.Offset(0,4) }:s).ToArray() } } };
        var shiftedWorld = await engine.GenerateAsync(shifted);
        foreach (var building in world.Objects.Where(o => o.Kind == "warehouse"))
        {
            var changed = shiftedWorld.Objects.Single(o => o.Id == building.Id);
            double delta = building.VisualSource!.RegionId == "industrial-0" ? 4 : 0;
            Check(changed.VisualSource!.PositionMeters == building.VisualSource.PositionMeters.Offset(0,delta),
                "mockup edits move dependent buildings only");
        }
        var duplicateRegion = input.Design!.SettlementRules[1] with { RegionId = "industrial-0",FrontagePathId = "service-road-0",SettlementId=input.Design.SettlementRules[0].SettlementId,Siting=input.Design.SettlementRules[0].Siting };
        await Reject(input with { Design = input.Design with { SettlementRules = [..input.Design.SettlementRules,duplicateRegion with { Id="duplicate-region" }] } },
            "ROOT_FOOTPRINT_CONFLICT");
        await Reject(input with { Design = input.Design with { VegetationClearances = [] } },"VEGETATION_ASSET_DIMENSIONS_MISSING");
        await Reject(input with { Design = input.Design with { PublicRealmPatterns = input.Design.PublicRealmPatterns.Select(p =>
            p with { NormalOffsetMeters = 0 }).ToArray() } },"PUBLIC_REALM_CLEARANCE_CONFLICT");

        var park = MockupGenerationFixture.Rect(80,4,136,12);
        var withPark = input with { Design = input.Design with {
            Blueprint = input.Design.Blueprint with { Regions = [..input.Design.Blueprint.Regions,new("town-park",park,"public-green-space")] },
            LandscapeZones = [new("town-park-lawn","park",park,"town-park","authored-park-composition")],
            VegetationGrammars = [new("park-shrubs","town-park-lawn","synthetic-shrub",VegetationPattern.BorderRow,8,2,"frame the park edge")]
        } };
        var parkWorld = await engine.GenerateAsync(withPark);
        Check(parkWorld.At(25,1).Kind == CellKind.Grass && parkWorld.Vegetation!.Any(p => p.ZoneId == "town-park-lawn"),
            "public park and private buffers have distinct intentional distribution");
        var badPark = withPark with { Design = withPark.Design! with { LandscapeZones = [withPark.Design.LandscapeZones![0] with {
            Boundary = MockupGenerationFixture.Rect(80,28,100,36),SourceVisualRegionId = "world" }] } };
        await Reject(badPark,"GREEN_ZONE_USE_CONFLICT");

        var catalog = new ModernCatalog().Structures;
        var stadium = catalog.Single(d => d.Id == "football-stadium");
        var parcel = new BuildableParcel("site","block",MockupGenerationFixture.Rect(0,0,400,400),new(200,0),"test-frontage");
        var stadiumRules = new BuildingEnvelopeRules(stadium.Id,40,40,20,1,1);
        var sizes = new BuildingSizePlanner();
        var local = sizes.Plan(parcel,stadiumRules,stadium,settlement:new(SettlementClass.Town,SettlementRole.Mixed,2000));
        var town = sizes.Plan(parcel,stadiumRules,stadium,settlement:new(SettlementClass.Town,SettlementRole.Mixed,10000));
        var city = sizes.Plan(parcel,stadiumRules,stadium,settlement:new(SettlementClass.City,SettlementRole.Mixed,200000));
        Check(local.Valid && town.Valid && city.Valid && local.Variant!.Id == "local-ground" && town.Variant!.Id == "town-ground" &&
            city.Variant!.Id == "major-city" && local.Footprint!.SignedArea() < city.Footprint!.SignedArea(),"stadium scale follows town/city demand");
        var townProgram = new BuildingProgramPlanner([stadium],new ModernDemandPolicy()).Plan(new(SettlementClass.Town,SettlementRole.Mixed,2000));
        var cityProgram = new BuildingProgramPlanner([stadium],new ModernDemandPolicy()).Plan(new(SettlementClass.City,SettlementRole.Mixed,200000));
        Check(townProgram.Needs.Single().SizeVariantId == "local-ground" && cityProgram.Needs.Single().SizeVariantId == "major-city" &&
            townProgram.Needs.Single().Count == 1 && cityProgram.Needs.Single().Count == 1,
            "demand planning selects appropriate venue scale instead of many house-sized stadiums");
        var undersized = sizes.Plan(parcel with { Boundary = MockupGenerationFixture.Rect(0,0,40,40),StreetAccess = new(20,0) },
            stadiumRules with { FrontSetbackMeters = 2,RearSetbackMeters = 2,SideSetbackMeters = 2 },stadium,
            settlement:new(SettlementClass.City,SettlementRole.Mixed,200000));
        Check(!undersized.Valid,"stadium is never shrunk to house scale");
        foreach (var type in new[] { "factory","warehouse" })
        {
            var definition = catalog.Single(d => d.Id == type);
            var small = sizes.Plan(parcel,new(type,10,10,10,1,1),definition,"small",1,new(SettlementClass.Town,SettlementRole.Industrial,2000));
            var large = sizes.Plan(parcel,new(type,10,10,10,1,1),definition,"large",1,new(SettlementClass.City,SettlementRole.Industrial,100000));
            Check(small.Valid && large.Valid && small.Footprint!.SignedArea() < large.Footprint!.SignedArea(),type+" has multiple unscaled physical sizes");
            Check(!sizes.Plan(parcel,new(type,10,10,10,1,1),definition,"small",1,
                new(SettlementClass.Town,SettlementRole.Residential,2000)).Valid,"industrial facilities obey settlement context");
        }
        var custom = stadium with { Id = "custom-exhibition-center",ScaleDemandPerResident = 0,
            SizeVariants = [new("authored",42,26,18,1000)] };
        Check(sizes.Plan(parcel,stadiumRules with { BuildingType = custom.Id },custom,requiredCapacity:800).Valid,
            "custom building types and dimensions require no engine enum or code change");

        var stadiumWorld = await engine.GenerateAsync(Landmark("football-stadium",new(SettlementClass.City,SettlementRole.Mixed,200000)));
        var houseWorld = await engine.GenerateAsync(Landmark("village-house",new(SettlementClass.Village,SettlementRole.Residential,25)));
        Check(stadiumWorld.Objects.Single(o => o.Kind == "football-stadium").VisualSource!.Footprint!.SignedArea() >
            houseWorld.Objects.Single(o => o.Kind == "village-house").VisualSource!.Footprint!.SignedArea()*100,
            "compiled stadium retains physical scale relative to a house");
        Check(stadiumWorld.SiteFeatures!.Any(s => s.Kind == SiteFeatureKind.Forecourt) &&
            stadiumWorld.SiteFeatures!.Any(s => s.Kind == SiteFeatureKind.Parking) &&
            houseWorld.SiteFeatures!.Any(s => s.Kind == SiteFeatureKind.Garden),"archetypes produce different surroundings");
        await Reject(Landmark("football-stadium",new(SettlementClass.City,SettlementRole.Mixed,200000)) with {
            Design = Landmark("football-stadium",new(SettlementClass.City,SettlementRole.Mixed,200000)).Design! with {
                RoadRules = [new("approach",RoadClass.Local,new(3,2,0,0,0,0))] } },"BUILDING_ACCESS_ROAD_UNDERSIZED");

        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        try { await engine.GenerateAsync(input,cancelled.Token); throw new Exception("Cancelled generation completed."); }
        catch (OperationCanceledException) { }
        var roundtrip = JsonSerializer.Deserialize<WorldRequest>(JsonSerializer.Serialize(input,WorldJson.CreateOptions()),WorldJson.CreateOptions())!;
        Check(JsonSerializer.Serialize(world.Cells) == JsonSerializer.Serialize((await engine.GenerateAsync(roundtrip)).Cells),"request catalog JSON roundtrip");
        if (cliPath is not null) await CheckCli(cliPath,input);
        else Console.WriteLine("NOT RUN: mockup generation CLI process checks (supply --cli <dll>).");
        Console.WriteLine("PASS: mockup generation, contextual building sizes, surroundings, green zoning and negative algorithm cases.");

        async Task Reject(WorldRequest request,string code)
        {
            try { await engine.GenerateAsync(request); }
            catch (WorldGenerationException ex) when (ex.Issues.Any(i => i.StartsWith(code,StringComparison.Ordinal))) { return; }
            throw new InvalidOperationException("Expected generation rejection: "+code);
        }
    }

    private static WorldRequest Landmark(string type,SettlementDemand context)
    {
        var region = MockupGenerationFixture.Rect(32,32,480,480);
        var definition = new ModernCatalog().Structures.Single(d => d.Id == type);
        return TestCatalogs.Contextualize(new(512,512,42,Biome.Desert,1,new(1,
            new(1,"synthetic-landmark-layout","simulated-r1","synthetic-style",
                [new("world",MockupGenerationFixture.Rect(0,0,512,512),"world"),new("landmark-site",region,"civic-composition")],[]),1,
            [new("approach","world",new([new(8,32),new(504,32)]),"road")],
            [new("approach",RoadClass.Arterial,new(6,2,0,0,0,0))],
            [new("landmark","landmark-site","settlement","approach","serve the settlement's explicit facility program",448,448,
                new(type,40,40,20,1,1),[],Settlement:context)],
            [new("world",CellKind.Sand,0,0,"authored-base")],[],[definition])));
    }

    private static async Task CheckCli(string cliPath,WorldRequest request)
    {
        var root = Path.Combine(Path.GetTempPath(),"gfw-mockup-cli-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string json = Path.Combine(root,"request.json"),output = Path.Combine(root,"world");
            await File.WriteAllTextAsync(json,JsonSerializer.Serialize(request,WorldJson.CreateOptions()));
            var good = await Run("generate-world",json,output);
            Check(good.Code == 0,"CLI generates mockup world");
            var original = await File.ReadAllBytesAsync(Path.Combine(output,"world.json"));
            var document = JsonSerializer.Deserialize<WorldDocument>(original,WorldJson.CreateOptions())!;
            Check(document.Objects.All(o => o.VisualSource is not null) && document.SiteFeatures!.Count > 0,"CLI exports full placement provenance and site geometry");
            await File.WriteAllTextAsync(json,JsonSerializer.Serialize(request with { Design = null },WorldJson.CreateOptions()));
            var failed = await Run("generate-world",json,output);
            Check(failed.Code == 1 && failed.Error.Contains("MOCKUP_DESIGN_REQUIRED"),"CLI rejects missing design");
            var preserved = await File.ReadAllBytesAsync(Path.Combine(output,"world.json"));
            Check(original.SequenceEqual(preserved),"invalid design preserves previous exported world");
            var retired = await Run("desert",Path.Combine(root,"invalid-command"),"42");
            Check(retired.Code == 1 && retired.Error.Contains("Supported:") && !Directory.Exists(Path.Combine(root,"invalid-command")),
                "CLI cannot silently use the retired prototype");
            var log = await File.ReadAllTextAsync(Path.Combine(root,".gameframework","operations","audit.jsonl"));
            Check(log.Contains("world.generate.succeeded") && log.Contains("world.generate.failed"),"generation audit retains success and rejection");
            Console.WriteLine("PASS: mockup-driven CLI generation and rejected-output preservation.");
        }
        finally { Directory.Delete(root,recursive:true); }

        async Task<(int Code,string Error)> Run(params string[] arguments)
        {
            var start = new ProcessStartInfo("dotnet") { WorkingDirectory = root,UseShellExecute = false,
                RedirectStandardOutput = true,RedirectStandardError = true,CreateNoWindow = true };
            start.ArgumentList.Add(cliPath);
            foreach (var argument in arguments) start.ArgumentList.Add(argument);
            using var process = Process.Start(start)!;
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try { await process.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException) { process.Kill(entireProcessTree:true); throw; }
            await stdout;
            return (process.ExitCode,await stderr);
        }
    }

    private static void Check(bool valid,string scenario)
    {
        if (!valid) throw new InvalidOperationException("Mockup algorithm regression failed: "+scenario);
    }
}
