using GameFrameWork.Catalogs.Modern;
using GameFrameWork.Core;
using GameFrameWork.Core.Geometry;
using GameFrameWork.Core.Planning;
using GameFrameWork.Worlds;
using System.Diagnostics;
using System.Text.Json;

internal static class CatalogExpansionTests
{
    public static async Task RunAsync(string? cliPath)
    {
        var catalog = new ModernCatalog().Structures;
        var variantCount = catalog.Sum(d => d.SizeVariants!.Count);
        Check(catalog.Count >= 190 && variantCount >= 500,"broad inventory rather than a renamed starter list");
        Check(catalog.Select(d => d.Id).Distinct(StringComparer.Ordinal).Count() == catalog.Count,"unique family identities");
        foreach (var category in BuildingCategory.Values.Where(c => c != BuildingCategory.Unspecified))
            Check(catalog.Count(d => d.Category == category) >= 3,"coverage: "+category);
        foreach (var required in new[] { "detached-house","villa","residential-tower","supermarket","shopping-mall",
            "cafe","hotel","bank","office-tower","primary-school","university-campus","library","hospital","pharmacy",
            "police-station","fire-station","courthouse","mosque","church","temple","synagogue","museum","theatre",
            "indoor-arena","swimming-center","gym","bus-station","railway-station","airport-terminal","aircraft-hangar",
            "refinery","chemical-plant","cold-storage-warehouse","greenhouse","barn","grain-silo","water-treatment-plant",
            "electrical-substation","data-center","cell-tower","public-toilet","playground","road-bridge","dam","quay" })
            Check(catalog.Any(d => d.Id == required),"missing important family: "+required);

        var resolver = new BuildingSizePlanner();
        int verifiedVariants = 0,unsupportedFamilies = 0;
        foreach (var definition in catalog)
        {
            definition.Validate();
            var variants = definition.SizeVariants ?? throw new InvalidOperationException("Missing sizes: "+definition.Id);
            Check(definition.Category != BuildingCategory.Unspecified && !string.IsNullOrWhiteSpace(definition.CapacityUnit),
                "category and capacity units: "+definition.Id);
            Check(variants.Count >= 2,"physical size family: "+definition.Id);
            Check(variants.Max(v => v.WidthMeters*v.DepthMeters) >
                variants.Min(v => v.WidthMeters*v.DepthMeters),"size variants differ: "+definition.Id);
            foreach (var variant in variants)
            {
                variant.Validate();
                var context = Context(definition,variant);
                var parcel = new BuildableParcel("catalog-site","catalog-block",
                    MockupGenerationFixture.Rect(0,0,variant.WidthMeters+400,variant.DepthMeters+400),
                    new((variant.WidthMeters+400)/2,0),"catalog-validation-frontage");
                var resolved = resolver.Plan(parcel,new(definition.Id,100,100,100,1,1),definition,
                    variant.Id,variant.ServiceCapacity,context);
                if (definition.PlacementKind != BuildingPlacementKind.FrontageSite)
                {
                    Check(!resolved.Valid && resolved.Issues.Contains("BUILDING_PLACEMENT_KIND_UNSUPPORTED"),
                        "do not misplace specialized infrastructure: "+definition.Id);
                    continue;
                }
                Check(resolved.Valid,"size fits generous test site: "+definition.Id+":"+variant.Id);
                var points = resolved.Footprint!.Vertices;
                Check(Math.Abs(points.Max(p => p.X)-points.Min(p => p.X)-variant.WidthMeters) < 1e-8 &&
                    Math.Abs(points.Max(p => p.Y)-points.Min(p => p.Y)-variant.DepthMeters) < 1e-8,
                    "unscaled dimensions: "+definition.Id+":"+variant.Id);
                var site = new BuildingSitePlanner().Plan("facility","visual-site","access-road",parcel,resolved.Footprint,definition);
                Check(site.Issues.Count == 0 && site.Features.Any(f => f.Kind == SiteFeatureKind.AccessWalkway),
                    "site program resolves: "+definition.Id+":"+variant.Id);
                Check(site.Features.All(f => f.Boundary.Vertices.All(parcel.Boundary.ContainsPoint)),
                    "surroundings stay in parcel: "+definition.Id);
                verifiedVariants++;
            }
            if (definition.PlacementKind != BuildingPlacementKind.FrontageSite) unsupportedFamilies++;
        }

        var program = new BuildingProgramPlanner(catalog,new ModernDemandPolicy()).Plan(new(SettlementClass.Town,SettlementRole.Mixed,2000));
        Check(program.Needs.All(n => catalog.Single(d => d.Id == n.DefinitionId).AutomaticDemand),
            "catalog breadth does not spawn every specialized facility");
        Check(!program.Needs.Any(n => n.DefinitionId is "airport-terminal" or "refinery" or "cathedral" or "data-center"),
            "specialized facilities need explicit composition demand");
        Check(catalog.Single(d => d.Id == "bus-stop").SiteRules!.All(s => s.Kind != SiteFeatureKind.LoadingYard) &&
            catalog.Single(d => d.Id == "hospital").SiteRules!.Any(s => s.Id == "patient-dropoff") &&
            catalog.Single(d => d.Id == "mosque").SiteRules!.Any(s => s.Id == "gathering-court") &&
            catalog.Single(d => d.Id == "food-processing-plant").SiteRules!.Any(s => s.Kind == SiteFeatureKind.LoadingYard),
            "site rules reflect facility purpose");
        Check(catalog.Single(d => d.Id == "hardware-store").Function == BuildingFunction.Retail &&
            catalog.Single(d => d.Id == "restaurant").Function == BuildingFunction.FoodService &&
            catalog.Single(d => d.Id == "mosque").Function == BuildingFunction.Worship &&
            catalog.Single(d => d.Id == "data-center").Function == BuildingFunction.Communications,
            "facility semantics remain distinct");

        var representatives = new[] { "detached-house","supermarket","hotel","bank","primary-school","hospital",
            "police-station","mosque","indoor-arena","bus-depot","food-processing-plant","greenhouse","data-center","park-pavilion" };
        var engine = new DesertWorldGenerator(TestCatalogs.Registry);
        foreach (var id in representatives)
        {
            var definition = catalog.Single(d => d.Id == id);
            var request = Request(definition);
            var world = await engine.GenerateAsync(request);
            var facility = world.Objects.Single(o => o.Kind == id);
            Check(facility.VisualSource!.SizeVariantId == "small" && facility.VisualSource.Footprint is not null &&
                facility.VisualSource.HeightMeters == definition.SizeVariants![0].HeightMeters &&
                world.SiteFeatures!.Count > 0,"complete generation: "+id);
        }
        var bridge = catalog.Single(d => d.Id == "road-bridge");
        try
        {
            await engine.GenerateAsync(Request(bridge));
            throw new InvalidOperationException("Bridge was incorrectly generated as a frontage building.");
        }
        catch (WorldGenerationException ex) when (ex.Issues.Any(i => i.StartsWith("BUILDING_PLACEMENT_KIND_UNSUPPORTED",StringComparison.Ordinal))) { }
        if (cliPath is not null) await CheckCli(cliPath,catalog.Count,variantCount);
        else Console.WriteLine("NOT RUN: catalog CLI discovery checks (supply --cli <dll>).");
        Console.WriteLine($"PASS: catalog coverage: {catalog.Count} families / {variantCount} variants / {catalog.Select(d => d.Category).Distinct().Count()} categories; " +
            $"{verifiedVariants} frontage variants sized/site-checked; {unsupportedFamilies} infrastructure families explicitly guarded; {representatives.Length} complete generation cases.");
    }

    private static async Task CheckCli(string cliPath,int families,int variants)
    {
        var root = Path.Combine(Path.GetTempPath(),"gfw-catalog-cli-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var all = await Run("list-buildings","modern");
            using (var json = JsonDocument.Parse(all.Output))
                Check(all.Code == 0 && json.RootElement.GetProperty("FamilyCount").GetInt32() == families &&
                    json.RootElement.GetProperty("VariantCount").GetInt32() == variants,"full CLI inventory");
            var filtered = await Run("list-buildings","modern","Healthcare");
            using (var json = JsonDocument.Parse(filtered.Output))
            {
                Check(filtered.Code == 0 && json.RootElement.GetProperty("FamilyCount").GetInt32() >= 10,"filtered CLI inventory");
                Check(json.RootElement.GetProperty("Definitions").EnumerateArray().All(d =>
                    d.GetProperty("Category").GetString() == "Healthcare"),"category filter excludes unrelated facilities");
            }
            Check((await Run("list-buildings","modern","unknown-category")).Code == 1,"invalid catalog category is rejected");
            var audit = await File.ReadAllTextAsync(Path.Combine(root,".gameframework","operations","audit.jsonl"));
            Check(audit.Contains("world.catalog.succeeded") && audit.Contains("world.catalog.failed"),"catalog reads and errors are audited");
            Console.WriteLine("PASS: catalog CLI inventory, category filter, invalid input and audit checks.");
        }
        finally { Directory.Delete(root,recursive:true); }

        async Task<(int Code,string Output)> Run(params string[] arguments)
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
            await stderr;
            return (process.ExitCode,await stdout);
        }
    }

    private static SettlementDemand Context(BuildingDefinition definition,BuildingSizeVariant variant)
    {
        var selectedClass = definition.AllowedClasses.OrderBy(c => c).First(c => variant.AllowedClasses is null || variant.AllowedClasses.Contains(c));
        var role = definition.AllowedRoles?.OrderBy(r => r).First() ?? SettlementRole.Mixed;
        return new(selectedClass,role,0);
    }

    private static WorldRequest Request(BuildingDefinition definition)
    {
        var variant = definition.SizeVariants![0];
        double right = 32+variant.WidthMeters+200,bottom = 32+variant.DepthMeters+200;
        int width = (int)Math.Ceiling(right+32),height = (int)Math.Ceiling(bottom+32);
        var region = MockupGenerationFixture.Rect(32,32,right,bottom);
        return TestCatalogs.Contextualize(new(width,height,42,Biome.Desert,1,new(1,
            new(1,"synthetic-catalog-layout","simulated-r1","synthetic-profile",
                [new("world",MockupGenerationFixture.Rect(0,0,width,height),"world"),new("facility-site",region,"facility-composition")],[]),1,
            [new("approach","world",new([new(16,32),new(right+16,32)]),"road")],
            [new("approach",RoadClass.Local,new(6,2,0,0,0,0))],
            [new("facility","facility-site","settlement","approach","serve the explicit "+definition.Id+" world program",
                right-32,right-32,new(definition.Id,40,40,40,1,1),[],variant.Id,variant.ServiceCapacity,Context(definition,variant))],
            [new("world",CellKind.Sand,0,0,"authored-base")],[],[definition])));
    }

    private static void Check(bool pass,string scenario)
    {
        if (!pass) throw new InvalidOperationException("Catalog expansion regression failed: "+scenario);
    }
}
