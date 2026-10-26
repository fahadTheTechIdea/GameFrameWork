using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;
using WorldCreator.Core;
using WorldCreator.Core.Capture;
using WorldCreator.Core.Geometry;
using WorldCreator.Core.Planning;

internal static class MockupCaptureTests
{
    public static async Task RunAsync(string? cliPath = null)
    {
        var root = Path.Combine(Path.GetTempPath(), "gameframework-capture-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var fixture = Path.Combine(Environment.CurrentDirectory, "tests/fixtures/mockup-capture-source.png");
            var bytes = await File.ReadAllBytesAsync(fixture);
            await File.WriteAllBytesAsync(Path.Combine(root, "source.png"), bytes);
            var capture = Create(Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
            Check(MockupCaptureValidator.Validate(capture).Valid, "reviewed synthetic fixture");
            var interior=new CaptureSurfaceCheck("box",new(20,20),true,"Reviewed source ground interior");
            var exterior=new CaptureSurfaceCheck("box",new(40,20),false,"Reviewed source walkway interior");
            Check(MockupCaptureValidator.Validate(capture with { SurfaceChecks=[interior,exterior] }).Valid,"independent source interior/exterior checks");
            Invalid(capture with { SurfaceChecks=[interior with { Inside=false }] },"CAPTURE_SURFACE_CHECK_FAILED");
            Invalid(capture with { SurfaceChecks=[exterior with { Inside=true }] },"CAPTURE_SURFACE_CHECK_FAILED");
            Invalid(capture with { SurfaceChecks=[interior,interior] },"CAPTURE_SURFACE_CHECK_INVALID");
            Invalid(capture with { SurfaceChecks=[interior with { FeatureId="r1" }] },"CAPTURE_SURFACE_CHECK_INVALID");
            Invalid(capture with { SurfaceChecks=[interior with { Pixel=new(101,0) }] },"CAPTURE_SURFACE_CHECK_INVALID");
            var area = new CaptureInspectionArea("source",new(0,0,100,100),CaptureInspectionStatus.Complete,["box"],[]);
            Check(MockupCaptureValidator.Validate(capture with { InspectionAreas = [area] }).Valid,"explicit complete inspection area");
            Invalid(capture with { InspectionAreas = [area with { FeatureIds = ["absent"] }] },"CAPTURE_INSPECTION_INVALID");
            Invalid(capture with { InspectionAreas = [area with { Bounds = new(99,0,int.MaxValue,100) }] },"CAPTURE_INSPECTION_INVALID");
            Invalid(capture with { InspectionAreas = [area with { RemainingWork = ["untraced trees"] }] },"CAPTURE_INSPECTION_INVALID");
            Invalid(capture with { InspectionAreas = [area with { Status = CaptureInspectionStatus.Partial, RemainingWork = ["trees"] }] },"CAPTURE_INSPECTION_INVALID");
            Invalid(capture with { InspectionAreas = [area with { Bounds = new(70,70,20,20) }] },"CAPTURE_INSPECTION_LOCATION_INVALID");
            var zone = new LandscapeZone("grove","Park",new([new(0,0),new(80,0),new(80,80),new(0,80)]),"scene","source-anchors");
            var grammar = new VegetationGrammar("trees","grove","tree",VegetationPattern.Anchored,1,0,"Same position",["tree-a","tree-b"]);
            VisualAnchor[] treeAnchors = [new("tree-a","scene",new(12.25,37.75),"vegetation.tree"),new("tree-b","scene",new(44,63),"vegetation.tree")];
            var anchored = VegetationCompositionPlanner.ComposeAnchors(grammar,zone,treeAnchors);
            Check(anchored.IsValid && anchored.Plants.Select(p=>p.PositionMeters).SequenceEqual(treeAnchors.Select(a=>a.PositionMeters)),"anchors preserve exact non-grid tree positions");
            Check(!VegetationCompositionPlanner.ComposeAnchors(grammar,zone,[treeAnchors[0]]).IsValid,"missing tree anchor rejects without relocation");
            Check(!VegetationCompositionPlanner.ComposeAnchors(grammar,zone,[treeAnchors[0],treeAnchors[1] with { PositionMeters=new(100,100) }]).IsValid,"out of zone trees reject without relocation");
            Check(!VegetationCompositionPlanner.ComposeAnchors(grammar with { AnchorIds=["tree-a","tree-a"] },zone,treeAnchors).IsValid,"duplicate tree anchor rejected");
            var treeCapture = capture with { Features=capture.Features.Concat([new CaptureFeature("tree","vegetation.tree",CaptureShape.Point,[new(23.25,67.5)],CaptureEvidence.Observed,CaptureReview.Pending,3,false,"Provisional crown center")]).ToArray() };
            var treeDraft = MockupCapturePipeline.VegetationDraft(treeCapture)!;
            Check(treeDraft.Plants.Single().PositionMeters==new WorldPoint(46.5,135) && treeDraft.Status.Contains("not-collision-accepted"),"source tree draft uses engine planner and authored calibration without claiming clearance approval");
            var ownedTree=treeCapture.Features.Last() with { Points=[new(20,20)], ParentId="box", VegetationPlacement=new(2,["r1","r2"]) };
            var planted=treeCapture with { Features=capture.Features.Select(f=>f.Id=="box"?f with {Semantic="landscape.planted-ground"}:f).Append(ownedTree).ToArray() };
            Check(MockupCaptureValidator.Validate(planted).Valid,"owned tree footprint stays inside planting and clear of protected road widths");
            Invalid(planted with { Features=planted.Features.Select(f=>f.Id=="tree"?f with { Points=[new(10.5,20)] }:f).ToArray() },"CAPTURE_VEGETATION_OUTSIDE_GROUND");
            Invalid(planted with { Features=planted.Features.Select(f=>f.Id=="tree"?f with { Points=[new(50,50)] }:f).ToArray() },"CAPTURE_VEGETATION_BOUNDARY_CONFLICT");
            Invalid(planted with { Features=planted.Features.Select(f=>f.Id=="tree"?f with { VegetationPlacement=new(2,["missing"]) }:f).ToArray() },"CAPTURE_VEGETATION_PLACEMENT_INVALID");
            Check((await MockupCapturePipeline.ReadSourceAsync(capture, root)).SequenceEqual(bytes), "real source bytes");
            var report = MockupCaptureValidator.Validate(capture);
            Check(report.Crossings.Count == 1 && report.Crossings[0].Kind == CaptureJunctionKind.AtGrade, "explicit at-grade crossing");
            var noJunction = MockupCaptureValidator.Validate(capture with { Junctions = [] });
            Check(noJunction.Valid && noJunction.Crossings.Single().Kind == CaptureJunctionKind.Unresolved, "projected crossings do not imply connectivity");
            var separated = capture with { Features = [capture.Features[0], capture.Features[1] with { Road = capture.Features[1].Road! with { Level = 1 } }, capture.Features[2]],
                Junctions = [capture.Junctions[0] with { Kind = CaptureJunctionKind.GradeSeparated }] };
            Check(MockupCaptureValidator.Validate(separated).Crossings.Single().Kind == CaptureJunctionKind.GradeSeparated, "grade separated crossing retained");
            Invalid(separated with { Junctions = capture.Junctions }, "CAPTURE_JUNCTION_LEVEL_INVALID");
            Invalid(capture with { Junctions = [capture.Junctions[0] with { Position = new(40, 50) }] }, "CAPTURE_JUNCTION_OFF_ROAD");
            Invalid(capture with { Junctions = [capture.Junctions[0] with { RoadIds = ["missing", "r2"] }] }, "CAPTURE_JUNCTION_INVALID");
            Invalid(capture with { Features = [capture.Features[0] with { Points = [new(-1, 0), new(3, 3)] }] }, "CAPTURE_COORDINATES_INVALID");
            Invalid(capture with { Features = [capture.Features[0] with { Points = [new(1, 1), new(1, 1)] }] }, "CAPTURE_GEOMETRY_INVALID");
            Invalid(capture with { Features = [capture.Features[0] with { Road = null }] }, "CAPTURE_ROAD_INVALID");
            Invalid(capture with { Features = [capture.Features[0] with { UncertaintyPixels = 1e200 }] }, "CAPTURE_FEATURE_INVALID");
            Invalid(capture with { Features = [capture.Features[2] with { Points = [new(10,10),new(30,30),new(10,30),new(30,10)] }] }, "CAPTURE_GEOMETRY_INVALID");
            Invalid(capture with { Features = [capture.Features[2] with { Semantic = "building.roof" }] }, "CAPTURE_BINDING_UNGROUNDED");
            Invalid(capture with { Features = [capture.Features[2] with { ParentId = "box" }] }, "CAPTURE_PARENT_INVALID");
            Invalid(capture with { Coverage = CaptureCoverage.Partial }, "CAPTURE_COVERAGE_INVALID");
            Invalid(capture with { Calibration = capture.Calibration! with { A = 0, E = 0 } }, "CAPTURE_CALIBRATION_INVALID");
            Invalid(capture with { Calibration = capture.Calibration! with { C = 40 } }, "CAPTURE_CALIBRATION_RESIDUAL");
            Invalid(capture with { Calibration = capture.Calibration! with { A = double.MaxValue, E = double.MaxValue } }, "CAPTURE_CALIBRATION_INVALID");
            Invalid(capture with { Calibration = capture.Calibration! with { Controls = [new(new(1,1),2,2),new(new(2,2),4,4),new(new(3,3),6,6)] } }, "CAPTURE_CALIBRATION_DEGENERATE");
            Throws(() => MockupCapturePipeline.GeometryDraft(capture with { Calibration = null }), "CAPTURE_CALIBRATION_REQUIRED");
            Throws(() => MockupCapturePipeline.GeometryDraft(capture with { Coverage = CaptureCoverage.Partial, Unresolved = ["missing sidewalks"] }), "CAPTURE_COVERAGE_INCOMPLETE");
            Throws(() => MockupCapturePipeline.GeometryDraft(capture with { Features = capture.Features.Select(f => f with { Review = CaptureReview.Pending }).ToArray() }), "CAPTURE_REVIEW_REQUIRED");
            Throws(() => MockupCapturePipeline.GeometryDraft(capture with { Junctions = [] }), "CAPTURE_REVIEW_REQUIRED");
            Throws(() => MockupCapturePipeline.GeometryDraft(capture with { Features = capture.Features.Select(f => f.Road is null ? f : f with { Road = f.Road with { Level = null } }).ToArray(), Junctions = [] }), "CAPTURE_REVIEW_REQUIRED");
            var draft = MockupCapturePipeline.GeometryDraft(capture);
            Check(draft.Features[0].Points[0] == new WorldPoint(20,20) && draft.Status.Contains("not-production"), "explicit affine calibration without promoting production");
            var template = SitingAlgorithmTests.Create().Design!;
            var pathId = template.Paths[0].Id;
            var bound = capture with { Features = [capture.Features[0] with { Binding = new("path", pathId) }, capture.Features[1] with { Binding = null }, capture.Features[2] with { Binding = null }] };
            template = template with { Blueprint = template.Blueprint with { MockupId = capture.Source.MockupId, ApprovedRevision = capture.Source.Revision, ArtStyleId = capture.Source.ArtStyleId } };
            var applied = MockupCapturePipeline.ApplyToDesign(bound, template);
            Check(applied.Paths[0].Centerline.Points[1] == new WorldPoint(200,100) && applied.Siting == template.Siting && applied.Catalog == template.Catalog && applied.Capture == bound, "actual engine contract receives geometry, source lineage and preserves authored rules");
            MockupCapturePipeline.ValidateDesignTrace(applied);
            Throws(() => MockupCapturePipeline.ValidateDesignTrace(applied with { Paths = template.Paths }), "CAPTURE_DESIGN_GEOMETRY_MISMATCH");
            Throws(() => MockupCapturePipeline.ApplyToDesign(bound, template with { Paths = [] }), "CAPTURE_DESIGN_BINDING_MISSING");
            Throws(() => MockupCapturePipeline.ApplyToDesign(bound, template with { Blueprint = template.Blueprint with { ApprovedRevision = "stale" } }), "CAPTURE_DESIGN_SOURCE_MISMATCH");
            var raised = bound with { Features = bound.Features.Select(f => f.Road is null ? f : f with { Road = f.Road with { Level = 1 } }).ToArray() };
            Throws(() => MockupCapturePipeline.ApplyToDesign(raised, template), "CAPTURE_DESIGN_INFRASTRUCTURE_RULE_REQUIRED");
            var generationRequest = SitingAlgorithmTests.Create();
            var generationTemplate = generationRequest.Design!;
            generationTemplate = generationTemplate with { Blueprint = generationTemplate.Blueprint with {
                MockupId = capture.Source.MockupId, ApprovedRevision = capture.Source.Revision, ArtStyleId = capture.Source.ArtStyleId } };
            var engineCapture = capture with { Features = [capture.Features[0] with {
                Points = generationTemplate.Paths[0].Centerline.Points.Select(p => new CapturePixel(p.X/4,p.Y/4)).ToArray(),
                Binding = new("path",generationTemplate.Paths[0].Id), Evidence = CaptureEvidence.Authored,
                Notes = "Synthetic authored geometry for engine handoff; not proof of image localization." }], Junctions = [],
                Calibration = new(4,0,0,0,4,0,"Synthetic authored game scale",0.01,[new(new(0,0),0,0),new(new(100,0),400,0),new(new(0,100),0,400)]) };
            var generationDesign = MockupCapturePipeline.ApplyToDesign(engineCapture, generationTemplate);
            var generated = await new WorldCreator.Worlds.DesertWorldGenerator(TestCatalogs.Registry).GenerateAsync(generationRequest with { Design = generationDesign });
            Check(generated.Request.Design!.Capture!.Source.Sha256 == capture.Source.Sha256 && generated.Siting!.Status == SitingEvaluationStatus.Passed,
                "complete actual engine world preserves capture lineage and passes existing contextual siting");
            var staleDesign = generationDesign with { Paths = generationDesign.Paths.Select((p,i) => i == 0 ? p with { Centerline = new(p.Centerline.Points.Select(q => q.Offset(0,1)).ToArray()) } : p).ToArray() };
            Check(new WorldCreator.Core.Planning.MockupDesignValidator(TestCatalogs.Registry).Validate(generationRequest with { Design = staleDesign }).Any(i => i.Code == "MOCKUP_CAPTURE_TRACE_INVALID"),
                "actual engine boundary rejects geometry edited without updating capture");
            var svg = MockupCapturePipeline.Overlay(capture, bytes);
            Check(XDocument.Parse(svg).Root!.Name.LocalName == "svg" && svg.Contains(Convert.ToBase64String(bytes)), "overlay embeds exact verified image");
            var prompts = MockupCapturePipeline.Prompts(capture);
            Check(prompts.Tasks.Count == 3 && prompts.Tasks.All(t => t.Status == "draft-blocked-for-production" && t.Crop.Width > 0 && t.Crop.Height > 0), "bounded crop and blocked prompt drafts");
            Check(prompts.Tasks.Where(t => t.FeatureId.StartsWith("r", StringComparison.Ordinal)).All(t => t.AssetClass == "terrain" && t.MasterPrompt.Contains("ROAD KIT")), "road prompts request a topology-aware terrain kit");
            var html = MockupCapturePipeline.ReviewHtml(capture, bytes);
            Check(html.Contains("Show original") && html.Contains("data-feature") && html.Contains("application/json"), "review viewer source toggle and feature inspection");
            Check(html.Contains("Inspection region") && html.Contains("Feature layer") && html.Contains("Unaltered source crop") && html.Contains("Count does not measure completeness"),"detailed capture filters, original crop and honest coverage");
            var atlasDirectory = Path.Combine(Environment.CurrentDirectory,"tests/fixtures/mockup-atlas/built");
            Check((await new GameArtsAtlasValidator().ValidateAsync(Path.Combine(atlasDirectory,"synthetic-door.atlas.json"), Path.Combine(atlasDirectory,"synthetic-door.png"))).Count == 0,
                "actual canonical GameArts builder fixture accepted by C# atlas validator");
            var escaped = capture with { Features = [capture.Features[2] with { Id = "<script>alert(1)</script>", Binding = null }] , Junctions = [] };
            var escapedSvg = MockupCapturePipeline.Overlay(escaped, bytes);
            Check(!escapedSvg.Contains("<script>") && escapedSvg.Contains("&lt;script&gt;"), "overlay text XML escaped");
            await RejectAsync(() => MockupCapturePipeline.ReadSourceAsync(capture with { Source = capture.Source with { Sha256 = new string('0',64) } }, root), "CAPTURE_SOURCE_HASH_MISMATCH");
            await RejectAsync(() => MockupCapturePipeline.ReadSourceAsync(capture with { Source = capture.Source with { Width = 101 } }, root), "CAPTURE_SOURCE_PNG_DIMENSIONS_MISMATCH");
            Throws(() => MockupCapturePipeline.ResolveSourcePath(root, "../escape.png"), "CAPTURE_SOURCE_PATH_INVALID");
            Throws(() => MockupCapturePipeline.ResolveSourcePath(root, "source.png:stream"), "CAPTURE_SOURCE_PATH_INVALID");
            var json = JsonSerializer.Serialize(capture, MockupCaptureJson.Options());
            Check(JsonSerializer.Serialize(JsonSerializer.Deserialize<MockupCaptureDocument>(json, MockupCaptureJson.Options()), MockupCaptureJson.Options()) == json, "strict JSON roundtrip");
            ThrowsJson(() => JsonSerializer.Deserialize<MockupCaptureDocument>(json.Replace("\"schemaVersion\": 1", "\"schemaVersion\": 1, \"oldField\": true"), MockupCaptureJson.Options()));
            ThrowsJson(() => JsonSerializer.Deserialize<MockupCaptureDocument>(json.Replace("\"Complete\"", "2"), MockupCaptureJson.Options()));
            using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
            try { MockupCaptureValidator.Validate(capture, cancelled.Token); throw new Exception("Cancellation ignored"); } catch (OperationCanceledException) { }
            var schemaFile = Path.Combine(Environment.CurrentDirectory, "docs/schemas/mockup-capture.schema.json");
            Check(JsonSerializer.Serialize(JsonDocument.Parse(await File.ReadAllTextAsync(schemaFile)).RootElement) == JsonSerializer.Serialize(JsonDocument.Parse(MockupCaptureJson.Schema().ToJsonString()).RootElement), "schema snapshot matches live contract");
            if (cliPath is not null)
            {
                await File.WriteAllTextAsync(Path.Combine(root, "capture.json"), json);
                var review = await Run("review-capture", "capture.json", root, "review");
                Check(review.Code == 0 && File.Exists(Path.Combine(root,"review/overlay.svg")) && File.Exists(Path.Combine(root,"review/prompts.json")), "actual CLI review export");
                var original = await File.ReadAllTextAsync(Path.Combine(root,"review/overlay.svg"));
                var exists = await Run("review-capture", "capture.json", root, "review");
                Check(exists.Code == 1 && exists.Error.Contains("CAPTURE_OUTPUT_EXISTS") && await File.ReadAllTextAsync(Path.Combine(root,"review/overlay.svg")) == original, "existing output preserved");
                var compile = await Run("compile-capture", "capture.json", root, "geometry");
                Check(compile.Code == 0 && File.Exists(Path.Combine(root,"geometry/geometry-draft.json")), "actual CLI calibrated draft");
                await File.WriteAllTextAsync(Path.Combine(root,"capture.json"), JsonSerializer.Serialize(capture with { Junctions = [] }, MockupCaptureJson.Options()));
                var rejected = await Run("compile-capture", "capture.json", root, "rejected");
                Check(rejected.Code == 1 && rejected.Error.Contains("CAPTURE_REVIEW_REQUIRED") && !Directory.Exists(Path.Combine(root,"rejected")), "unresolved topology never publishes draft");
                await File.AppendAllTextAsync(Path.Combine(root,"source.png"), "tampered");
                var stale = await Run("review-capture", "capture.json", root, "stale");
                Check(stale.Code == 1 && stale.Error.Contains("CAPTURE_SOURCE_HASH_MISMATCH") && !Directory.Exists(Path.Combine(root,"stale")), "stale source publishes nothing");
                foreach (var sink in new[] { "log.jsonl", "audit.jsonl", "notifications.jsonl" })
                {
                    var events = await File.ReadAllTextAsync(Path.Combine(root,".gameframework/operations",sink));
                    Check(events.Contains("world.capture.succeeded") && events.Contains("world.capture.failed") && events.Contains("CorrelationId"), "shared operational reporting " + sink);
                }
            }
            Console.WriteLine($"PASS: mockup source capture, geometry/calibration/topology/review gates, prompt drafts, schema, engine handoff, canonical atlas fixture; CLI {(cliPath is null ? "NOT RUN" : "PASS")} (synthetic inputs; no visual approval).");
        }
        finally { Directory.Delete(root, true); }

        async Task<(int Code, string Error)> Run(params string[] args)
        {
            var start = new ProcessStartInfo("dotnet") { WorkingDirectory = root, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            start.ArgumentList.Add(cliPath!); foreach (var arg in args) start.ArgumentList.Add(arg);
            using var process = Process.Start(start)!; var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            try { await process.WaitForExitAsync(timeout.Token); } catch (OperationCanceledException) { process.Kill(true); throw; }
            _ = await stdout; return (process.ExitCode, await stderr);
        }
    }
    private static MockupCaptureDocument Create(string hash) => new(1,
        new("synthetic", hash, "source.png", hash, 100,100,"test", "orthographic", ".gamearts/outputs/manifests/mockups.json"),
        CaptureCoverage.Complete, [],
        [new("r1","road.centerline",CaptureShape.Polyline,[new(0,50),new(100,50)],CaptureEvidence.Observed,CaptureReview.Accepted,1,false,"Synthetic horizontal road", Road:new(10,CaptureWidthKind.Carriageway,0,["vehicle"]),Binding:new("path","p1")),
         new("r2","road.centerline",CaptureShape.Polyline,[new(50,0),new(50,100)],CaptureEvidence.Observed,CaptureReview.Accepted,1,false,"Synthetic vertical road",Road:new(10,CaptureWidthKind.Carriageway,0,["vehicle"]),Binding:new("path","p2")),
         new("box","building.footprint",CaptureShape.Polygon,[new(10,10),new(30,10),new(30,30),new(10,30)],CaptureEvidence.Authored,CaptureReview.Accepted,1,false,"Synthetic authored ground boundary",Binding:new("region","box"))],
        [new("cross",new(50,50),["r1","r2"],CaptureJunctionKind.AtGrade,CaptureReview.Accepted,"Synthetic reviewed at-grade")],
        new(2,0,0,0,2,0,"Synthetic game-authored scale, not measured real scene",0.01,[new(new(0,0),0,0),new(new(100,0),200,0),new(new(0,100),0,200)]));
    private static void Invalid(MockupCaptureDocument capture, string code) => Check(MockupCaptureValidator.Validate(capture).Issues.Any(i => i.Code == code), code);
    private static void Check(bool condition, string text) { if (!condition) throw new Exception("Capture regression: " + text); }
    private static void Throws(Action action, string code) { try { action(); } catch (ArgumentException e) when(e.Message.Contains(code)) { return; } throw new Exception("Expected " + code); }
    private static async Task RejectAsync(Func<Task<byte[]>> action, string code) { try { await action(); } catch (ArgumentException e) when(e.Message.Contains(code)) { return; } throw new Exception("Expected " + code); }
    private static void ThrowsJson(Action action) { try { action(); } catch (JsonException) { return; } throw new Exception("Strict JSON accepted invalid contract"); }
}
