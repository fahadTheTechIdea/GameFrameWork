using WorldCreator.Catalogs.Modern;
using System.Text.Json;
using System.Xml.Linq;
using System.Diagnostics;
using System.Security.Cryptography;
using WorldCreator.Core;
using WorldCreator.Core.Capture;
using WorldCreator.Core.Geometry;
using WorldCreator.Core.Planning;
using WorldCreator.Worlds;

internal static class CurvedRoadTests
{
    public static async Task RunAsync(string? cliPath=null)
    {
        WorldPolygon Box(double x,double y,double r,double b)=>new([new(x,y),new(r,y),new(r,b),new(x,b)]);
        Check(Math.Abs(SurfaceClearanceValidator.Distance(Box(0,0,10,10),Box(11,0,21,10))-1)<1e-9,"continuous boundary distance preserves exactly one meter");
        Check(SurfaceClearanceValidator.Distance(Box(0,4,10,6),Box(4,0,6,10))==0,"crossing edges detect overlap even when vertices lie outside");
        var concave=new WorldPolygon([new(0,0),new(10,0),new(10,3),new(3,3),new(3,10),new(0,10)]);
        Check(Math.Abs(SurfaceClearanceValidator.Distance(concave,Box(5,5,7,7))-2)<1e-9,"concave boundary separation is measured without bounding-box approximation");
        // Every slope/octant, reversal, single-cell segment and exact corner must remain four-connected.
        for(int x=-9;x<=9;x++) for(int y=-9;y<=9;y++)
        {
            var a=new GridPoint(12,12);var b=new GridPoint(12+x,12+y);
            var cells=new RoadSegment("test",a,b,RoadClass.Local).Cells().ToHashSet();
            Check(cells.SetEquals(new RoadSegment("test",b,a,RoadClass.Local).Cells()),"reversal invariant");
            Check(cells.Contains(a)&&cells.Contains(b)&&Connected(cells),"four-connected supercover");
        }
        var section=new RoadCrossSection(3,2,1,2,.2,.2);
        var bent=new StreetCorridor("curve",new([new(0,0),new(20,0),new(35,12),new(40,30)]),section);
        var surfaces=StreetSurfaceBuilder.Build(bent);
        foreach(var surface in surfaces) new WorldPolygon(surface.Polygon).Validate();
        Check(surfaces.Any(s=>s.SegmentIndex<0)&&surfaces.Select(s=>(s.SegmentIndex,s.Kind)).Distinct().Count()==surfaces.Count,"bend joins have unique structural IDs");
        Check(surfaces.Any(s=>s.SegmentIndex<0&&s.Kind==StreetSurfaceKind.Carriageway&&new WorldPolygon(s.Polygon).ContainsPoint(new(20.5,-2.8))),"outside bend wedge filled");
        var graph=StreetTopologyBuilder.Build([bent,new("cross",new([new(10,-10),new(10,10)]),section)]);
        Check(graph.IsConnected()&&graph.Diagnostics.Count==0,"continuous diagonal/bent topology");
        using var cancelled=new CancellationTokenSource();cancelled.Cancel();
        try{StreetTopologyBuilder.Build([bent],token:cancelled.Token);throw new Exception("Cancellation ignored");}catch(OperationCanceledException){}
        var baseline=SitingAlgorithmTests.Create();
        var extra=new VisualDesignPath("curved-entry","world",new([new(32,64),new(28,48),new(28.1,47.9),new(26,36),new(30,20)]),"road");
        var curved=baseline with {Design=baseline.Design! with {Paths=[..baseline.Design!.Paths,extra],RoadRules=[..baseline.Design.RoadRules,new(extra.Id,RoadClass.Local,new(3,2,0,0,0,0))]}};
        var engine=new DesertWorldGenerator(TestCatalogs.Registry);
        var generated=await engine.GenerateAsync(curved);
        Check(generated.Request.Design!.Paths.Single(p=>p.Id==extra.Id).Centerline.Points.SequenceEqual(extra.Centerline.Points),"curved/dense source samples preserved in complete world");
        Check(JsonSerializer.Serialize(generated)==JsonSerializer.Serialize(await engine.GenerateAsync(curved)),"curve generation deterministic");
        var svg=new WorldPlanSvgRenderer().Render(generated);
        Check(XDocument.Parse(svg).Root!.Name.LocalName=="svg"&&svg.Contains("curved-entry"),"actual generated geometry map");
        var disconnected=curved with {Design=curved.Design! with {Paths=curved.Design.Paths.Select(p=>p.Id==extra.Id?p with {Centerline=new([new(10,10),new(12,12)])}:p).ToArray()}};
        try{await engine.GenerateAsync(disconnected);throw new Exception("Disconnected curve accepted");}
        catch(WorldGenerationException e) when(e.Issues.Any(i=>i.Contains("ROAD_TOPOLOGY_INVALID",StringComparison.Ordinal))){}
        await CityAcceptance(cliPath);
        Console.WriteLine("PASS: diagonal supercover octants/reversal, bend joins, dense curves, continuous topology, capture scopes and actual Modern city generation.");
    }

    private static async Task CityAcceptance(string? cliPath)
    {
        string path=Path.Combine(Environment.CurrentDirectory,".gameframework/worlds/modern-city");
        var capture=JsonSerializer.Deserialize<MockupCaptureDocument>(await File.ReadAllTextAsync(Path.Combine(path,"mockup-capture.json")),MockupCaptureJson.Options())!;
        var report=MockupCaptureValidator.Validate(capture);
        Check(report.Valid&&report.Crossings.Count==51&&report.Crossings.All(c=>c.Kind==CaptureJunctionKind.AtGrade&&c.JunctionId is not null),"all city crossings explicitly resolved");
        Check(capture.Coverage==CaptureCoverage.Partial&&capture.CompilationScope is not null&&capture.Calibration!.Controls.Count>=4,"scoped completeness does not fabricate full visual inventory");
        await MockupCapturePipeline.ReadSourceAsync(capture,Environment.CurrentDirectory);
        var request=JsonSerializer.Deserialize<WorldRequest>(await File.ReadAllTextAsync(Path.Combine(path,"world-request.json")),WorldJson.CreateOptions())!;
        string contentPath=Path.Combine(path,"catalogs","modern-city.layout.json");
        var cityRegistry=new SettlementCatalogRegistry(CatalogContentAuthoring.Apply([new ModernCatalog()],[CatalogContentAuthoring.ReadManifest(contentPath)]));
        Check(request.Design!.BuildingCatalog.Count==0 && request.Design.BuildingCatalogIds?.Count==6,"city definitions have one reusable catalog owner");
        var applied=CatalogContentAuthoring.BindReferences(MockupCapturePipeline.ApplyToDesign(capture,request.Design!),cityRegistry);
        var world=await new DesertWorldGenerator(cityRegistry).GenerateAsync(request with {Design=applied});
        foreach(var sample in new[]{(815d,510d,"modern.planted-ground"),(800d,568d,"modern.park-paving"),(915d,520d,"modern.football-pitch"),(915d,585d,"modern.basketball-court")})
        {
            var p=MockupCaptureValidator.Transform(capture.Calibration!,new(sample.Item1,sample.Item2));
            var cell=world.At((int)(p.X/applied.MetersPerCell),(int)(p.Y/applied.MetersPerCell));
            Check(cell.SurfaceMaterialId==sample.Item3,"actual compiled source location keeps material "+sample.Item3);
            var restored=JsonSerializer.Deserialize<WorldCell>(JsonSerializer.Serialize(cell,WorldJson.CreateOptions()),WorldJson.CreateOptions())!;
            Check(restored==cell,"surface material and source provenance survive cell JSON roundtrip");
        }
        Check(world.Landscape!.Single(z=>z.Id=="captured-park-trees").Role==LandscapeZoneRole.PlacementScope,"tree ownership envelope does not repaint captured ground");
        Check(applied.TerrainRules.Count(r=>r.SurfaceMaterialId=="modern.planted-ground")==6&&capture.CompilationScope!.FeatureIds.Contains("park-court"),"all six planted polygons and sports surfaces compile from capture bindings");
        var badMaterial=applied with {Capture=null,TerrainRules=applied.TerrainRules.Select((r,i)=>i==0?r with {SurfaceMaterialId=" "}:r).ToArray()};
        Check(new MockupDesignValidator(cityRegistry).Validate(request with {Design=badMaterial}).Any(i=>i.Code=="MOCKUP_TERRAIN_RULE_INVALID"),"blank surface material rejected");
        var badRole=applied with {Capture=null,LandscapeZones=applied.LandscapeZones!.Select(z=>z with {Role=(LandscapeZoneRole)99}).ToArray()};
        Check(new MockupDesignValidator(cityRegistry).Validate(request with {Design=badRole}).Any(i=>i.Code=="MOCKUP_LANDSCAPE_ZONE_INVALID"),"undefined placement-scope role rejected");
        var buildings=world.Objects.Where(o=>o.VisualSource?.BuildingDefinitionId is not null).ToArray();
        Check(buildings.Length==43&&world.Siting!.Status==SitingEvaluationStatus.Passed&&world.Siting.Allocations.Count==174,"complete real city uses all contextual engine gates");
        var stadium=buildings.Single(o=>o.Kind=="football-stadium").VisualSource!;
        var factory=buildings.Single(o=>o.Kind=="factory").VisualSource!;
        Check(Size(stadium.Footprint!)==(180d,140d)&&stadium.ServiceCapacity==12000,"regional stadium dimensions/capacity preserved");
        Check(Size(factory.Footprint!)==(140d,90d)&&factory.ServiceCapacity==3000,"large heavy factory dimensions/capacity preserved");
        var sourceParkTrees=capture.Features.Where(f=>f.Id.StartsWith("detail-tree-park-",StringComparison.Ordinal)).ToArray();
        Check(world.Vegetation!.Count==44&&world.Request.Design!.Capture!.Source.Sha256==capture.Source.Sha256,"city captured park planting and source lineage");
        Check(sourceParkTrees.All(f=>world.Vegetation.Any(p=>p.Id.EndsWith(f.Id,StringComparison.Ordinal)&&p.PositionMeters==MockupCaptureValidator.Transform(capture.Calibration!,f.Points[0]))),"every park tree stays at its exact source position in actual engine world");
        var trees=MockupCapturePipeline.VegetationDraft(capture)!;
        Check(trees.Plants.Count==capture.Features.Count(f=>f.Semantic is "vegetation.tree" or "vegetation.palm")&&trees.Plants.Count==432,"complete currently annotated tree layout retains all exact positions");
        Check(capture.InspectionAreas!.Count==8&&capture.InspectionAreas.All(a=>a.Status==CaptureInspectionStatus.Partial&&a.RemainingWork.Count>0),"all inspected regions retain explicit smaller-detail gaps");
        var plantedGround=capture.Features.Where(f=>f.Semantic=="landscape.planted-ground"&&f.Evidence==CaptureEvidence.Observed).Select(f=>new WorldPolygon(f.Points.Select(p=>new WorldPoint(p.X,p.Y)).ToArray())).ToArray();
        Check(plantedGround.Length==6&&plantedGround.Any(p=>p.ContainsPoint(new(815,510))),"park planted ground has independently traced surface islands");
        Check(new[]{new WorldPoint(742,548),new(915,520),new(915,585)}.All(q=>plantedGround.All(p=>!p.ContainsPoint(q))),"park ground excludes gazebo paving, soccer pitch and basketball court");
        Check(plantedGround.Any(p=>p.ContainsPoint(new(800,552)))&&plantedGround.All(p=>!p.ContainsPoint(new(969,520))&&!p.ContainsPoint(new(800,567))),"refined ground retains the planted bed and excludes east-pitch/south walkways using independent source witness points");
        Check(sourceParkTrees.All(t=>t.VegetationPlacement is { GroundRadiusPixels:1.5 }&&t.ParentId is not null),"all park trees own a constrained planted-ground area rather than being dropped at its border");
        Check(capture.SurfaceChecks is {Count:118}&&capture.SurfaceChecks.Count(c=>!c.FeatureId.StartsWith("design-clearance-",StringComparison.Ordinal))==59,"source and adjusted planting retain independent source samples");
        Check(applied.SurfaceClearances is {Count:6}&&applied.SurfaceClearances.All(r=>r.MinimumGapMeters==1),"current city config declares one-meter paved gaps");
        var ground=new MockupTerrainComposer(cityRegistry).Compose(request with {Design=applied});
        var bandPoint=MockupCaptureValidator.Transform(capture.Calibration!,new(991.5,520));
        var bandCell=ground.Cells[(int)(bandPoint.Y/applied.MetersPerCell)*request.Width+(int)(bandPoint.X/applied.MetersPerCell)];
        Check(ground.Issues.Count==0&&bandCell.SurfaceMaterialId=="modern.park-paving"&&bandCell.DesignRuleId!.StartsWith("spacing-band:",StringComparison.Ordinal),"minimum space compiles a paved band beyond the former park envelope");
        var gapFailures=SurfaceClearanceValidator.Validate(applied with {SurfaceClearances=applied.SurfaceClearances!.Select(r=>r with {MinimumGapMeters=1000}).ToArray()});
        Check(gapFailures.Any(i=>i.Code=="SURFACE_CLEARANCE_TOO_SMALL"&&i.Message.Contains("measured gap",StringComparison.Ordinal)),"spacing violations preserve measured distances");
        Check(SurfaceClearanceValidator.Validate(applied with {SurfaceClearances=[applied.SurfaceClearances![0] with {RegionId="missing"}]}).Any(i=>i.Code=="SURFACE_CLEARANCE_RULE_INVALID"),"invalid surface references reject");
        Check(SurfaceClearanceValidator.Validate(applied with {SurfaceClearances=applied.SurfaceClearances!.Select(r=>r with {VegetationRadiusMeters=1000}).ToArray()}).Any(i=>i.Code=="SURFACE_CLEARANCE_TREE_OUTSIDE"),"setbacks cannot remove fixed tree footprints");
        Check(applied.Paths.Where(p=>p.Id.StartsWith("r",StringComparison.Ordinal)).Any(p=>p.Centerline.Points.Count>2),"captured curves are retained");
        var missing=capture with {Junctions=capture.Junctions.Skip(1).ToArray()};
        try{MockupCapturePipeline.GeometryDraft(missing);throw new Exception("Missing scoped crossing accepted");}catch(ArgumentException e) when(e.Message.Contains("CAPTURE_REVIEW_REQUIRED",StringComparison.Ordinal)){}
        var malformed=capture with {CompilationScope=capture.CompilationScope! with {FeatureIds=["missing"]}};
        Check(!MockupCaptureValidator.Validate(malformed).Valid,"malformed scope rejected");
        var utility=applied with {Paths=applied.Paths.Select(p=>p with {AdditionalNetworkModes=null}).ToArray(),Capture=null};
        Check(new StructureSitingValidator(cityRegistry).Validate(utility).Any(i=>i.Code=="SITING_LINK_MODE_MISMATCH"),"utility sharing requires explicit path contract");
        if(cliPath is not null)
        {
            string repository=Environment.CurrentDirectory;
            string temp=Path.Combine(Path.GetTempPath(),"city-capture-cli-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
            try
            {
                var result=await Run("generate-world",Path.Combine(path,"world-request.json"),"world","--capture",Path.Combine(path,"mockup-capture.json"),"--asset-root",repository);
                Check(result.Code==0&&File.Exists(Path.Combine(temp,"world/plan.svg")),"actual capture-to-engine CLI exports generated map");
                string output=Path.Combine(temp,"world/world.json");string before=Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(output)));
                var tightRequest=request with {Design=applied with {SurfaceClearances=applied.SurfaceClearances!.Select(r=>r with {MinimumGapMeters=1000}).ToArray()}};
                await File.WriteAllTextAsync(Path.Combine(temp,"tight.json"),JsonSerializer.Serialize(tightRequest,WorldJson.CreateOptions()));
                var tooTight=await Run("generate-world","tight.json","world","--capture",Path.Combine(path,"mockup-capture.json"),"--asset-root",repository);
                Check(tooTight.Code==1&&tooTight.Error.Contains("SURFACE_CLEARANCE_TOO_SMALL")&&Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(output)))==before,"actual CLI rejects impossible spacing and preserves successful output");
                await File.WriteAllTextAsync(Path.Combine(temp,"unresolved.json"),JsonSerializer.Serialize(missing,MockupCaptureJson.Options()));
                var rejected=await Run("generate-world",Path.Combine(path,"world-request.json"),"world","--capture","unresolved.json","--asset-root",repository);
                Check(rejected.Code==1&&rejected.Error.Contains("CAPTURE_REVIEW_REQUIRED")&&Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(output)))==before,"failed capture generation preserves current city output");
                var options=await Run("generate-world",Path.Combine(path,"world-request.json"),"absent","--capture","unresolved.json");
                Check(options.Code==1&&options.Error.Contains("must be supplied together")&&!Directory.Exists(Path.Combine(temp,"absent")),"capture requires explicit trusted image root");
                var events=await File.ReadAllTextAsync(Path.Combine(temp,".gameframework/operations/audit.jsonl"));
                Check(events.Contains("world.generate.capture.applied")&&events.Contains("world.generate.succeeded")&&events.Contains("world.generate.failed"),"capture generation reuses shared audit boundary");
            }
            finally{Directory.Delete(temp,true);}
            async Task<(int Code,string Error)> Run(params string[] arguments)
            {
                var start=new ProcessStartInfo("dotnet"){WorkingDirectory=temp,RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false,CreateNoWindow=true};
                start.ArgumentList.Add(Path.GetFullPath(cliPath));foreach(var arg in arguments)start.ArgumentList.Add(arg);
                start.ArgumentList.Add("--catalog-content");start.ArgumentList.Add(contentPath);
                using var process=Process.Start(start)!;var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();
                using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(45));
                try{await process.WaitForExitAsync(timeout.Token);}catch(OperationCanceledException){process.Kill(true);throw;}
                _=await stdout;return(process.ExitCode,await stderr);
            }
        }
        static (double,double) Size(WorldPolygon p)=>(Math.Round(p.Vertices.Max(v=>v.X)-p.Vertices.Min(v=>v.X),6),Math.Round(p.Vertices.Max(v=>v.Y)-p.Vertices.Min(v=>v.Y),6));
    }
    private static bool Connected(HashSet<GridPoint> cells)
    {
        var reached=new HashSet<GridPoint>();var queue=new Queue<GridPoint>();queue.Enqueue(cells.First());
        while(queue.TryDequeue(out var p))if(reached.Add(p))foreach(var d in new[]{new GridPoint(1,0),new(-1,0),new(0,1),new(0,-1)})
        {var n=new GridPoint(p.X+d.X,p.Y+d.Y);if(cells.Contains(n)&&!reached.Contains(n))queue.Enqueue(n);}
        return reached.SetEquals(cells);
    }
    private static void Check(bool valid,string name){if(!valid)throw new Exception("Curved-road regression: "+name);}
}
