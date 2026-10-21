using System.Text.Json;
using GameFrameWork.Core;
using GameFrameWork.Core.Operations;
using GameFrameWork.Core.Geometry;
using GameFrameWork.Core.Planning;
using GameFrameWork.Worlds;
using GameFrameWork.Godot;
var generator = new DesertWorldGenerator();
var req = new WorldRequest(32, 32, 123, Biome.Desert);
var first = await generator.GenerateAsync(req);
var second = await generator.GenerateAsync(req);
Assert(JsonSerializer.Serialize(first) == JsonSerializer.Serialize(second), "Determinism");
Assert(first.Cells.Count == 1024, "Grid dimensions");
Assert(first.Cells.Any(c => c.Kind == CellKind.Road), "Roads");
Assert(first.Objects.Any(o => o.Kind == "gate"), "Gate metadata");
var dir = Path.Combine(Path.GetTempPath(), "gameframework-test-" + Guid.NewGuid().ToString("N"));
try
{
    await new GodotWorldExporter().ExportAsync(first, dir);
    Assert(File.Exists(Path.Combine(dir, "GeneratedWorld.tscn")) && File.Exists(Path.Combine(dir, "world.json")), "Export outputs");
}
finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }

var blueprint = new WorldBlueprint(1, "test-city", req, new[]
{
    new AssetRequirement("warehouse", "building", 8, 6, new[] { "S", "N" }, new[]
    {
        new ComponentRequirement("door", "warehouse", "door-hinge", true, AnimationTechnique.Frames,
            new[] { "closed", "opening", "open", "closing" })
    })
});
var empty = new InMemoryRegistry(null);
var missing = await new WorldBuildGate(empty).CheckAsync(blueprint);
Assert(!missing.Ready && missing.Issues.Any(i => i.Code == "ART_MISSING"), "Missing GameArts asset rejection");
const string hash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
var manifest = new ApprovedArtManifest(1, "warehouse", "approved-r1", "7.5",
    ArtApprovalState.Approved, new[]
    {
        new SpriteSheetManifest("door-s", "door_s.png", "door_s.json", hash, hash, "door", new[] { "S" })
    },
    new[]
    {
        new ComponentAttachment("door", "warehouse", "door-hinge", 0.4, 0.2, new Dictionary<string, PixelAnchor>
        {
            ["S"] = new(8, 13)
        })
    });
var incomplete = await new WorldBuildGate(new InMemoryRegistry(manifest)).CheckAsync(blueprint);
Assert(!incomplete.Ready && incomplete.Issues.Any(i => i.Code == "ANIMATION_SHEET_MISSING"), "Incomplete direction sheet rejected");
Assert(incomplete.Issues.Any(i => i.Code == "DIRECTION_ANCHOR_MISSING"), "Missing direction attachment rejected");
var full = manifest with
{
    Sheets = new[]
    {
        manifest.Sheets[0],
        new SpriteSheetManifest("door-n", "door_n.png", "door_n.json", hash, hash, "door", new[] { "N" })
    },
    Attachments = new[]
    {
        manifest.Attachments[0] with
        {
            DirectionAnchors = new Dictionary<string, PixelAnchor> { ["S"] = new(8, 13), ["N"] = new(8, 12) }
        }
    }
};
var approved = await new WorldBuildGate(new InMemoryRegistry(full)).CheckAsync(blueprint);
Assert(approved.Ready, "Structurally complete art manifest accepted; byte/atlas verification remains separate");
var revoked = await new WorldBuildGate(new InMemoryRegistry(full with { ApprovalState = ArtApprovalState.Revoked })).CheckAsync(blueprint);
Assert(!revoked.Ready && revoked.Issues.Any(i => i.Code == "NOT_APPROVED"), "Revoked art rejected");


// Spatial foundation: no arbitrary parcel position or disconnected street frontage.
var planned = new SettlementPlanner().Plan(new WorldRequest(48, 48, 42, Biome.Desert, 2));
Assert(planned.Diagnostics.Count == 0, "Planner reports no infeasible parcels for fixture");
var roadCells = planned.Roads.SelectMany(r => r.Cells()).ToHashSet();
Assert(planned.Roads.Any(r => r.Class == RoadClass.Collector), "Regional road spine");
Assert(planned.Buildings.Count == 8, "Building requirements met");
Assert(planned.Buildings.Select(b => b.Site).Distinct().Count() == planned.Buildings.Count, "Footprints non-overlapping");
Assert(planned.Buildings.All(b => roadCells.Contains(b.AccessRoadCell)), "Every building road frontage is valid");
Assert(planned.Buildings.All(b => !roadCells.Contains(b.Site)), "No building blocks a road");
Assert(planned.Buildings.All(b => !string.IsNullOrWhiteSpace(b.Purpose) &&
    !string.IsNullOrWhiteSpace(b.ConstraintId)), "Every placement is explainable");
var plannedAgain = new SettlementPlanner().Plan(new WorldRequest(48, 48, 42, Biome.Desert, 2));
Assert(JsonSerializer.Serialize(planned) == JsonSerializer.Serialize(plannedAgain), "Deterministic spatial plan");
// All roads connect to one collector, and no structure overwrites the road raster.
var plannedWorld = await generator.GenerateAsync(new WorldRequest(48, 48, 42, Biome.Desert, 2));
Assert(plannedWorld.Objects.Count(o => o.Kind == "warehouse") == 8, "World projects all planned buildings");
Assert(planned.Buildings.All(b => plannedWorld.At(b.AccessRoadCell.X, b.AccessRoadCell.Y).Kind == CellKind.Road),
    "Rasterized access road is preserved");


// Deliberately invalid plans must fail structural validation, not silently export.
var isolatedRoads = new[]
{
    new RoadSegment("west", new GridPoint(1, 1), new GridPoint(3, 1), RoadClass.Local),
    new RoadSegment("east", new GridPoint(10, 10), new GridPoint(12, 10), RoadClass.Local)
};
var invalidRoad = SpatialValidation.Validate(new GridBounds(16, 16), isolatedRoads,
    Array.Empty<PlacementDecision>());
Assert(invalidRoad.Any(d => d.Code == "DISCONNECTED_ROAD_NETWORK"), "Isolated roads rejected");
var invalidFrontage = new[]
{
    new PlacementDecision("test-building", "warehouse", new GridPoint(3, 3),
        "workshop capacity", "settlement-0", "road-frontage-v1", new GridPoint(1, 1))
};
var invalidParcels = SpatialValidation.Validate(new GridBounds(16, 16),
    new[] { isolatedRoads[0] }, invalidFrontage);
Assert(invalidParcels.Any(d => d.Code == "FRONTAGE_NOT_ADJACENT"), "Non-adjacent frontage rejected");
var frontageRule = new RoadFrontageConstraint();
Assert(!frontageRule.IsSatisfied(new GridPoint(3, 3), new GridPoint(1, 1),
    new HashSet<GridPoint> { new(1, 1) }, new HashSet<GridPoint>(),
    new GridBounds(16, 16)), "Constraint rejects bad frontage");
Assert(SpatialValidation.Validate(new GridBounds(48, 48), planned.Roads,
    planned.Buildings).Count == 0, "Planned roads and lots validate");


// Continuous world-meter geometry checks
var crossSection = new RoadCrossSection(3.5, 2, 2.0, 2.0, .15, .15);
var street = new StreetCorridor("main-street",
    new WorldPolyline(new[] { new WorldPoint(0, 0), new WorldPoint(10, 0) }), crossSection);
Assert(Math.Abs(crossSection.TotalWidthMeters - 11.3) < 1e-9, "Street cross-section width");
Assert(Math.Abs(street.Centerline.LengthMeters() - 10) < 1e-9, "World-meter centerline length");
var streetEdges = street.SegmentEdges();
Assert(streetEdges.Count == 1, "Road segment edges generated");
Assert(Math.Abs(streetEdges[0].LeftStart.Y - 5.65) < 1e-9, "Left road offset");
Assert(Math.Abs(streetEdges[0].RightEnd.Y + 5.65) < 1e-9, "Right road offset");
Assert(street.Bounds().Min.X < 0 && street.Bounds().Max.X > 10, "Road bounds include full cross-section");
AssertThrows<ArgumentException>(() => new WorldPolyline(new[] { new WorldPoint(1,1), new WorldPoint(1,1) }).Validate(),
    "Duplicate centerline points rejected");
AssertThrows<ArgumentOutOfRangeException>(() => new RoadCrossSection(-1, 2, 2, 2, 0, 0).Validate(),
    "Negative lane width rejected");
AssertThrows<ArgumentException>(() => new RoadCrossSection(3, 2, double.NaN, 2, 0, 0).Validate(),
    "Nonfinite sidewalk width rejected");



// Parametric urban surface bands are derived from the same street profile.
var bands = StreetSurfaceBuilder.Build(street);
Assert(bands.Count == 5, "Road has carriageway, two curbs and two sidewalks");
Assert(bands.All(b => b.Polygon.Count == 4), "Band polygons have four vertices");
Assert(bands.All(b => Math.Abs(StreetSurfaceBuilder.SignedArea(b.Polygon)) > 1e-8),
    "Generated surface bands have nonzero area");
var lane = bands.Single(b => b.Kind == StreetSurfaceKind.Carriageway);
Assert(Math.Abs(Math.Abs(StreetSurfaceBuilder.SignedArea(lane.Polygon)) - 70.0) < 1e-8,
    "Two lane carriageway area is 7m by 10m");
var walks = bands.Where(b => b.Kind is StreetSurfaceKind.LeftSidewalk or StreetSurfaceKind.RightSidewalk);
Assert(walks.All(b => Math.Abs(Math.Abs(StreetSurfaceBuilder.SignedArea(b.Polygon)) - 20.0) < 1e-8),
    "Each sidewalk is 2m by 10m");
var curvedBands = StreetSurfaceBuilder.Build(new StreetCorridor("bent",
    new WorldPolyline(new[] { new WorldPoint(0,0), new WorldPoint(10,0), new WorldPoint(10,10) }), crossSection));
Assert(curvedBands.Count == 10, "Both road segments receive independent bands");

// Continuous street intersection regression scenarios
var crossing = PlanarIntersections.Intersect(new(0, 0), new(10, 0), new(5, -5), new(5, 5));
Assert(crossing.Kind == IntersectionKind.Point && crossing.Point is { X: 5, Y: 0 }, "Orthogonal road intersection");
Assert(PlanarIntersections.Intersect(new(0, 0), new(10, 0), new(0, 2), new(10, 2)).Kind == IntersectionKind.None,
    "Parallel roads do not intersect");
Assert(PlanarIntersections.Intersect(new(0, 0), new(10, 0), new(5, 0), new(15, 0)).Kind ==
    IntersectionKind.CollinearOverlap, "Overlapping road centerlines detected");
Assert(PlanarIntersections.Intersect(new(0, 0), new(5, 0), new(5, 0), new(5, 5)).Kind ==
    IntersectionKind.Point, "Endpoint joins are intersections");
var crossingNetwork = StreetNetworkAnalyzer.Analyze(new[]
{
    new StreetCorridor("east-west", new WorldPolyline(new[] { new WorldPoint(0, 0), new WorldPoint(10, 0) }), crossSection),
    new StreetCorridor("north-south", new WorldPolyline(new[] { new WorldPoint(5, -5), new WorldPoint(5, 5) }), crossSection)
});
Assert(crossingNetwork.Junctions.Count == 1 && crossingNetwork.Junctions[0].Corridors.Count == 2,
    "Intersection carries both road IDs");
Assert(crossingNetwork.Diagnostics.Count == 0, "Valid crossing has no overlap warnings");


// Street topology must split true crossing centerlines and reject disconnected networks.
var connectedGraph = StreetTopologyBuilder.Build(new[]
{
    new StreetCorridor("horizontal", new WorldPolyline(new[] { new WorldPoint(0, 0), new WorldPoint(10, 0) }), crossSection),
    new StreetCorridor("vertical", new WorldPolyline(new[] { new WorldPoint(5, -5), new WorldPoint(5, 5) }), crossSection)
});
Assert(connectedGraph.Nodes.Count == 5 && connectedGraph.Edges.Count == 4, "Crossing forms four split edges and five nodes");
Assert(connectedGraph.IsConnected() && connectedGraph.Diagnostics.Count == 0, "Crossing graph connected");
var disconnectedGraph = StreetTopologyBuilder.Build(new[]
{
    new StreetCorridor("a", new WorldPolyline(new[] { new WorldPoint(0, 0), new WorldPoint(2, 0) }), crossSection),
    new StreetCorridor("b", new WorldPolyline(new[] { new WorldPoint(10, 0), new WorldPoint(12, 0) }), crossSection)
});
Assert(!disconnectedGraph.IsConnected() && disconnectedGraph.Diagnostics.Contains("DISCONNECTED_STREET_GRAPH"),
    "Separated streets are rejected as disconnected");


// Buildable block extraction from four orthogonal road corridors.
StreetCorridor RectRoad(string id, double ax, double ay, double bx, double by) =>
    new(id, new WorldPolyline(new[] { new WorldPoint(ax, ay), new WorldPoint(bx, by) }), crossSection);
var enclosed = UrbanBlockExtractor.Extract(new[]
{
    RectRoad("north", 0, 0, 100, 0), RectRoad("south", 0, 80, 100, 80),
    RectRoad("west", 0, 0, 0, 80), RectRoad("east", 100, 0, 100, 80)
});
Assert(enclosed.Blocks.Count == 1 && enclosed.Diagnostics.Count == 0, "Enclosed road rectangle yields one block");
Assert(Math.Abs(enclosed.Blocks[0].AreaSquareMeters - (100 - 11.3) * (80 - 11.3)) < 1e-7,
    "Block interior accounts for road corridor width");
Assert(UrbanBlockExtractor.Extract(new[]
{
    RectRoad("north", 0, 0, 100, 0), RectRoad("south", 0, 80, 100, 80),
    RectRoad("west", 0, 0, 0, 80)
}).Blocks.Count == 0, "Missing road edge cannot form a closed block");


// Polygon validity and minimum-frontage parcel subdivision
var parcels = FrontageParcelPlanner.Subdivide(enclosed.Blocks[0], 20, 8);
Assert(parcels.Diagnostics.Count == 0 && parcels.Parcels.Count == 4,
    "Block subdivides into four purpose-defined frontages");
Assert(parcels.Parcels.All(p => p.Boundary.SignedArea() > 0), "Parcel geometry valid");
Assert(Math.Abs(parcels.Parcels.Sum(p => p.Boundary.SignedArea()) -
    enclosed.Blocks[0].AreaSquareMeters) < 1e-7, "Parcel coverage conserves buildable area");
Assert(parcels.Parcels.All(p => p.StreetAccess.Y == enclosed.Blocks[0].Boundary[0].Y),
    "Every parcel has a street-facing anchor");
AssertThrows<ArgumentException>(() => new WorldPolygon(new[]
{
    new WorldPoint(0,0), new WorldPoint(10,10), new WorldPoint(0,10), new WorldPoint(10,0)
}).Validate(), "Bow-tie polygon rejected");


// Convex irregular parcels can be split without losing area.
var irregular = new WorldPolygon(new[]
{
    new WorldPoint(0, 0), new WorldPoint(30, 0),
    new WorldPoint(25, 20), new WorldPoint(5, 20)
});
Assert(ConvexParcelGeometry.IsConvex(irregular), "Trapezoidal parcel is convex");
var (western, eastern) = ConvexParcelGeometry.SplitVertical(irregular, 15);
western.Validate(); eastern.Validate();
Assert(Math.Abs(Math.Abs(western.SignedArea()) + Math.Abs(eastern.SignedArea()) -
    Math.Abs(irregular.SignedArea())) < 1e-7, "Irregular parcel split conserves area");
AssertThrows<ArgumentOutOfRangeException>(() => ConvexParcelGeometry.SplitVertical(irregular, 45),
    "Outside split rejected");


// Building rules are explicit and must not force-fit infeasible structures.
var parcel = parcels.Parcels[0];
var houseRules = new BuildingEnvelopeRules("house", 4, 3, 2, 8, 10);
var house = BuildingEnvelopePlanner.Plan(parcel, houseRules);
Assert(house.IsFeasible && house.Footprint is not null, "Residential envelope feasible");
Assert(house.Footprint!.SignedArea() > 0, "House buildable area positive");
var tower = BuildingEnvelopePlanner.Plan(parcel,
    new BuildingEnvelopeRules("tower", 8, 8, 5, 40, 40));
Assert(!tower.IsFeasible && tower.Diagnostics.Contains("INSUFFICIENT_BUILDABLE_ENVELOPE"),
    "Tower denied on undersized town parcel");
AssertThrows<ArgumentException>(() => new BuildingEnvelopeRules("house", -1, 1, 1, 8, 8).Validate(),
    "Negative setbacks rejected");


// Settlement identity selects functional building programs, not arbitrary sprites.
var programPlanner = new BuildingProgramPlanner(ExampleBuildingCatalog.Create());
var villageProgram = programPlanner.Plan(new(SettlementClass.Village, SettlementRole.Agricultural, 200));
var townProgram = programPlanner.Plan(new(SettlementClass.Town, SettlementRole.Commercial, 200));
var cityProgram = programPlanner.Plan(new(SettlementClass.City, SettlementRole.Industrial, 200));
Assert(villageProgram.Diagnostics.Count == 0 && townProgram.Diagnostics.Count == 0 &&
    cityProgram.Diagnostics.Count == 0, "Baseline settlement profiles have housing");
Assert(villageProgram.Needs.Any(n => n.DefinitionId == "village-house"), "Village houses present");
Assert(!villageProgram.Needs.Any(n => n.DefinitionId == "apartment"), "No city apartments in village");
Assert(townProgram.Needs.Any(n => n.DefinitionId == "townhouse"), "Town housing selected");
Assert(cityProgram.Needs.Any(n => n.DefinitionId == "apartment") &&
    cityProgram.Needs.Any(n => n.DefinitionId == "factory"), "Industrial city program selected");
Assert(!townProgram.Needs.Any(n => n.DefinitionId == "factory"), "No unrelated city factory in commercial town");
Assert(JsonSerializer.Serialize(programPlanner.Plan(new(SettlementClass.Village, SettlementRole.Agricultural, 200))) ==
    JsonSerializer.Serialize(villageProgram), "Building demand reproducible");


// Public-realm features are anchored to actual streets, never uniformly scattered.
var publicStreet = new StreetCorridor("public-main",
    new WorldPolyline(new[] { new WorldPoint(0, 0), new WorldPoint(100, 0) }), crossSection);
var publicRules = new PublicRealmRules(25, 1, 1.5);
var publicPlan = new PublicRealmPlanner().Plan(new[] { publicStreet }, publicRules);
Assert(publicPlan.Diagnostics.Count == 0, "Street accessories have legal clearances");
Assert(publicPlan.Features.Count == 5, "Street endpoints and internal light positions derived from spacing");
Assert(publicPlan.Features.Count(f => f.Kind == PublicFeatureKind.Streetlight) == 3,
    "Internal illumination fixtures spaced along corridor");
Assert(publicPlan.Features.All(f => f.ParentId == "public-main" && f.Position.Y > 0 &&
    !string.IsNullOrWhiteSpace(f.Purpose)), "Public features anchored with purpose");
var reserved = new SpatialReservationIndex();
Assert(reserved.TryReserve(new WorldPoint(0, 0), 2), "First reservation succeeds");
Assert(!reserved.TryReserve(new WorldPoint(1, 0), 2), "Overlapping accessory rejected");
Assert(reserved.TryReserve(new WorldPoint(5, 0), 2), "Separated accessory allowed");
Assert(JsonSerializer.Serialize(new PublicRealmPlanner().Plan(new[] { publicStreet }, publicRules)) ==
    JsonSerializer.Serialize(publicPlan), "Public realm deterministic");


var requiredDesign = new WorldDesignPlan("planned-world", new GridBounds(16, 16), new[]
{
    new PlannedWorldObject("depot", "warehouse", new GridPoint(3, 4),
        "service goods transport", "industrial-program-v1", "planned-world"),
    new PlannedWorldObject("depot-door", "door", new GridPoint(3, 4),
        "accessible depot entry", "door-component-v1", "planned-world", "depot", "closed", "door-open")
});
var constructed = new WorldPlanCompiler().Compile(requiredDesign);
Assert(constructed.Count == 2, "Valid design compiles parent and component");
var missingPurpose = requiredDesign with { Objects = new[]
{
    requiredDesign.Objects[0] with { Purpose = "" }
}};
AssertThrows<WorldPlanException>(() => new WorldPlanCompiler().Compile(missingPurpose),
    "Missing design purpose blocks construction");
var missingParent = requiredDesign with { Objects = new[]
{
    requiredDesign.Objects[1] with { ParentObjectId = "unknown" }
}};
Assert(new WorldDesignValidator().Validate(missingParent).Issues.Any(i => i.Code == "PARENT_NOT_FOUND"),
    "Orphan component rejected");
var overlapping = requiredDesign with { Objects = new[]
{
    requiredDesign.Objects[0],
    requiredDesign.Objects[0] with { Id = "unrelated" }
}};
Assert(new WorldDesignValidator().Validate(overlapping).Issues.Any(i => i.Code == "ROOT_FOOTPRINT_CONFLICT"),
    "Two independent objects cannot occupy identical root position");


var coordinator = new WorldPlanCoordinator();
var disciplines = new[]
{
    new DesignDiscipline("objects", new[] { "parcels", "transport" }),
    new DesignDiscipline("transport", new[] { "terrain" }),
    new DesignDiscipline("parcels", new[] { "transport" }),
    new DesignDiscipline("terrain", Array.Empty<string>())
};
var coordinated = coordinator.Coordinate(disciplines);
Assert(coordinated.IsValid &&
    string.Join(",", coordinated.ExecutionOrder) == "terrain,transport,parcels,objects",
    "Design dependencies control execution order");
var missingDesign = coordinator.Coordinate(new[]
{
    new DesignDiscipline("lighting", new[] { "street-plan" })
});
Assert(!missingDesign.IsValid && missingDesign.Issues.Any(i => i.Code == "DESIGN_DEPENDENCY_MISSING"),
    "Missing street design blocks lighting");
var circularDesign = coordinator.Coordinate(new[]
{
    new DesignDiscipline("terrain", new[] { "roads" }),
    new DesignDiscipline("roads", new[] { "terrain" })
});
Assert(!circularDesign.IsValid && circularDesign.Issues.Any(i => i.Code == "DESIGN_DEPENDENCY_CYCLE"),
    "Circular design dependencies rejected");


var integrated = new SettlementDesignPipeline().Build(new WorldRequest(48, 48, 42, Biome.Desert, 2));
Assert(integrated.Settlement.Buildings.Count == 8, "Integrated settlement has planned building demand");
Assert(integrated.Streets.Count == integrated.Settlement.Roads.Count, "Street geometry follows transport plan");
Assert(integrated.PublicRealm.Features.All(f => integrated.Streets.Any(s => s.Id == f.ParentId)),
    "Public realm features reference planned streets");
Assert(new WorldDesignValidator().Validate(integrated.Design).IsValid, "Integrated object plan validates");
Assert(new WorldPlanCompiler().Compile(integrated.Design).Count == 16,
    "Integrated world compiles buildings with attached gates");
Assert(JsonSerializer.Serialize(integrated) ==
    JsonSerializer.Serialize(new SettlementDesignPipeline().Build(new WorldRequest(48,48,42,Biome.Desert,2))),
    "Integrated settlement deterministic");

// Durable operational service contract test: log, audit and user-facing notification are separate sinks.
var operationsDir = Path.Combine(Path.GetTempPath(), "gameframework-ops-" + Guid.NewGuid().ToString("N"));
try
{
    var logPath = Path.Combine(operationsDir, "log.jsonl");
    var auditPath = Path.Combine(operationsDir, "audit.jsonl");
    var notificationPath = Path.Combine(operationsDir, "notifications.jsonl");
    var reporter = new WorldOperationReporter(new JsonLineOperationStore(logPath),
        new JsonLineOperationStore(auditPath),
        new ConsoleAndFileNotificationService(new JsonLineOperationStore(notificationPath)));
    var evt = new OperationEvent("world.validation.failed", DateTimeOffset.UtcNow,
        OperationSeverity.Warning, "tests", "world.validate", "correlation-test",
        "Invalid placement rejected.", "test-world", "test-runner", "failed");
    await reporter.ReportAsync(evt, notify: true);
    foreach (var output in new[] { logPath, auditPath, notificationPath })
    {
        Assert(File.Exists(output), "Operation sink file created");
        var stored = File.ReadAllText(output);
        Assert(stored.Contains("world.validation.failed") && stored.Contains("correlation-test"), "Correlated operation persisted");
    }
}
finally { if (Directory.Exists(operationsDir)) Directory.Delete(operationsDir, true); }
Console.WriteLine("PASS: world model and art contract structural tests (not PNG/atlas/Godot runtime verification).");
static void AssertThrows<T>(Action action, string message) where T : Exception
{
    try { action(); } catch (T) { return; }
    throw new Exception("Failed: " + message);
}
static void Assert(bool pass, string message)
{
    if (!pass) throw new Exception("Failed: " + message);
}
sealed class InMemoryRegistry(ApprovedArtManifest? manifest) : IApprovedArtRegistry
{
    public Task<ApprovedArtManifest?> ResolveAsync(string assetId, CancellationToken cancellationToken = default)
        => Task.FromResult(manifest);
}
