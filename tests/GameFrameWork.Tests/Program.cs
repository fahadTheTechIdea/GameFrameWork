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


var integratedGate = new SettlementDesignGate();
Assert(integratedGate.Validate(integrated).Count == 0, "Integrated design passes cross-discipline validation");
Assert(integratedGate.Compile(integrated).Count == integrated.Design.Objects.Count,
    "Validated integrated design compiles without inventing objects");
var brokenPublicRealm = integrated with
{
    PublicRealm = integrated.PublicRealm with
    {
        Features = new[]
        {
            new PublicFeature("orphan-sign", PublicFeatureKind.StreetNameSign,
                new WorldPoint(10, 10), "missing-road", "road naming", "signage-v1", 1)
        }
    }
};
Assert(integratedGate.Validate(brokenPublicRealm).Any(d => d.StartsWith("ORPHAN_PUBLIC_FEATURE")),
    "Orphan street signs rejected");
AssertThrows<InvalidOperationException>(() => integratedGate.Compile(brokenPublicRealm),
    "Cannot compile a design containing orphan features");
var incompleteIntegrated = integrated with { Diagnostics = new[] { "UNMET_DESIGN_REQUIREMENT" } };
AssertThrows<InvalidOperationException>(() => integratedGate.Compile(incompleteIntegrated),
    "Unresolved stage diagnostics block compilation");


var visual = new ApprovedVisualWorldBlueprint(1, "town-mockup", "approved-r1", "pixel-cozy",
    new[] { new VisualRegion("square", new WorldPolygon(new[]
    {
        new WorldPoint(0,0), new WorldPoint(64,0), new WorldPoint(64,64), new WorldPoint(0,64)
    }), "town-square") },
    new[] { new VisualAnchor("shop-entry", "square", new WorldPoint(24,32), "shop") });
var visualDecisions = new[] { new VisualObjectDecision("shop", "building", "shop-entry",
    "anchor the storefront in the approved square", "mockup-shop-v1") };
var visualPlan = new VisualWorldPlanner().Plan(visual, visualDecisions,
    "approved-town-design", new GridBounds(16,16), 4);
Assert(visualPlan.IsValid && visualPlan.Design!.Objects[0].Site == new GridPoint(6,8),
    "Approved mockup anchor compiles into planned game location");
Assert(new WorldPlanCompiler().Compile(visualPlan.Design!).Count == 1,
    "Approved visual world plan compiles");
var absentAnchor = new VisualWorldPlanner().Plan(visual,
    new[] { visualDecisions[0] with { AnchorId = "missing" } },
    "approved-town-design", new GridBounds(16,16), 4);
Assert(!absentAnchor.IsValid && absentAnchor.Issues.Any(i => i.Code == "VISUAL_ANCHOR_MISSING"),
    "No guessing placement for missing visual anchors");
var unapprovedMockup = new VisualWorldPlanner().Plan(visual with { ApprovedRevision = "" },
    visualDecisions, "approved-town-design", new GridBounds(16,16), 4);
Assert(!unapprovedMockup.IsValid, "No world build without approved mockup revision");


var pattern = new RegionPattern("garden-hedge", "square", "hedge", "define garden boundary", 8, 4, "approved-garden-row-v1");
var repeated = new VisualRegionPatternGenerator().Generate(visual, pattern);
Assert(repeated.Issues.Count == 0 && repeated.Decisions.Count == 8,
    "Approved visual region produces repeated planned hedge elements");
var mergedVisual = visual with { Anchors = visual.Anchors.Concat(repeated.GeneratedAnchors).ToArray() };
var repeatedWorld = new VisualWorldPlanner().Plan(mergedVisual, repeated.Decisions,
    "hedge-plan", new GridBounds(16,16), 4);
Assert(repeatedWorld.IsValid, "Generated grammar anchors compile through visual world planning");
Assert(new VisualRegionPatternGenerator().Generate(visual, pattern with { RegionId = "unknown" })
    .Issues.Any(i => i.Code == "REGION_MISSING"), "Unknown region blocks pattern");


var designedPath = new VisualDesignPath("garden-border", "square",
    new WorldPolyline(new[]
    {
        new WorldPoint(4, 4), new WorldPoint(36, 4), new WorldPoint(36, 36)
    }), "garden-edge");
var pathGrammar = new PathPattern("garden-lamps", "garden-border",
    "decorative-lamp", "mark the authored garden route", "mockup-path-v1", 8, 4, 4);
var pathGenerated = new VisualPathPatternGenerator().Generate(visual, new[] { designedPath }, pathGrammar);
Assert(pathGenerated.Issues.Count == 0 && pathGenerated.Anchors.Count == 8,
    "Path geometry determines decorative fixture count");
Assert(pathGenerated.Anchors[0].PositionMeters == new WorldPoint(8, 4) &&
    pathGenerated.Anchors[4].PositionMeters == new WorldPoint(36, 8),
    "Path samples follow authored turns in order");
Assert(new VisualPathPatternGenerator().Generate(visual, new[] { designedPath },
    pathGrammar with { PathId = "unknown" }).Issues.Any(i => i.Code == "VISUAL_PATH_MISSING"),
    "Missing visual path cannot trigger arbitrary placements");
Assert(JsonSerializer.Serialize(pathGenerated) == JsonSerializer.Serialize(
    new VisualPathPatternGenerator().Generate(visual, new[] { designedPath }, pathGrammar)),
    "Path placements reproducible");


var composed = new VisualCompositionGenerator().Generate(
    new VisualCompositionRequest(visual,
        new[] { pattern },
        new[] { designedPath },
        new[] { pathGrammar },
        visualDecisions),
    "composed-game-town", new GridBounds(16,16), 4);
Assert(composed.IsValid, "Approved mockup combines distinct building, region and path grammars");
Assert(composed.Design!.Objects.Count == 17, "Explicit object and sixteen pattern elements combined");
Assert(new WorldPlanCompiler().Compile(composed.Design).Count == 17,
    "Compiled objects preserve planned composition");
var badComposition = new VisualCompositionGenerator().Generate(
    new VisualCompositionRequest(visual,
        new[] { pattern, pattern },
        Array.Empty<VisualDesignPath>(),
        Array.Empty<PathPattern>(),
        Array.Empty<VisualObjectDecision>()),
    "duplicate-grammar", new GridBounds(16,16), 4);
Assert(!badComposition.IsValid &&
    badComposition.Issues.Any(i => i.Code == "COMPOSITION_ANCHOR_COLLISION"),
    "Duplicate design grammar IDs cannot silently overwrite artwork");


var artBound = await new WorldArtAssembler(new InMemoryRegistry(full)).AssembleAsync(
    requiredDesign with { Objects = new[] { requiredDesign.Objects[0] } },
    new[] { new WorldArtBinding("depot", "warehouse", "door", "S", 2) });
Assert(artBound.IsValid && artBound.Objects.Count == 1 &&
    artBound.Objects[0].Art.RenderLayer == 2, "Approved art binding preserves planned object and render layer");
var wrongFacing = await new WorldArtAssembler(new InMemoryRegistry(full)).AssembleAsync(
    requiredDesign with { Objects = new[] { requiredDesign.Objects[0] } },
    new[] { new WorldArtBinding("depot", "warehouse", "door", "E", 2) });
Assert(!wrongFacing.IsValid && wrongFacing.Issues.Any(i => i.Code == "ART_DIRECTION_UNAVAILABLE"),
    "Unapproved facing cannot be substituted");
var missingArtBinding = await new WorldArtAssembler(new InMemoryRegistry(full)).AssembleAsync(
    requiredDesign, Array.Empty<WorldArtBinding>());
Assert(!missingArtBinding.IsValid && missingArtBinding.Issues.Count(i => i.Code == "ART_BINDING_MISSING") == 2,
    "No constructed visual objects without explicit asset bindings");


var artDir = Path.Combine(Path.GetTempPath(), "gameframework-art-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(artDir);
try
{
    // Tiny valid 1x1 opaque PNG fixture and structured atlas file with actual measured bytes.
    var pngBytes = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/3ioAAAAASUVORK5CYII=");
    var pngPath = Path.Combine(artDir, "fixture.png");
    var atlasPath = Path.Combine(artDir, "fixture.json");
    await File.WriteAllBytesAsync(pngPath, pngBytes);
    await File.WriteAllTextAsync(atlasPath, "{\"frames\":[]}");
    static string Sha256Hex(byte[] bytes) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
    var fileSheet = new SpriteSheetManifest("fixture", "fixture.png", "fixture.json",
        Sha256Hex(pngBytes), Sha256Hex(await File.ReadAllBytesAsync(atlasPath)), "door", new[] { "S" });
    var fileManifest = full with { Sheets = new[] { fileSheet } };
    var verifier = new GameArtsFileVerifier(artDir);
    var goodFiles = await verifier.VerifyAsync(fileManifest);
    Assert(goodFiles.IsValid, "Approved asset files match real PNG and atlas hashes");
    await File.WriteAllTextAsync(atlasPath, "{\"frames\":[1]}");
    var mismatched = await verifier.VerifyAsync(fileManifest);
    Assert(mismatched.Issues.Any(i => i.Code == "ASSET_HASH_MISMATCH"),
        "Modified atlas fails hash validation");
    var traversal = fileManifest with
    {
        Sheets = new[] { fileSheet with { ImagePath = "../outside.png" } }
    };
    var denied = await verifier.VerifyAsync(traversal);
    Assert(denied.Issues.Any(i => i.Code == "ASSET_PATH_INVALID"),
        "Traversal path rejected");
}
finally { Directory.Delete(artDir, recursive: true); }


var bundleRoot = Path.Combine(Path.GetTempPath(), "gamearts-native-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(bundleRoot);
try
{
    var bundlePath = Path.Combine(bundleRoot, "sample.bundle.json");
    var nativeJson = """
    {
      "schema_version":"1.0","asset_id":"gate-asset","directions":["S","E"],
      "components":[
        {"id":"base","parent":null,"mount":null,
         "atlases":{"S":"base_s.atlas.json","E":"base_e.atlas.json"}},
        {"id":"door","parent":"base","mount":"hinge",
         "atlases":{"S":"door_s.atlas.json","E":"door_e.atlas.json"}}
      ]
    }
    """;
    await File.WriteAllTextAsync(bundlePath, nativeJson);
    var nativeResult = await new GameArtsBundleReader().ReadAsync(bundlePath);
    Assert(nativeResult.IsValid && nativeResult.Bundle!.Components.Count == 2,
        "Native GameArts component bundle parsed");
    // Parent atlas exists, but must provide the child's named hinge on every direction.
    var parentAtlas = """
    {"schema_version":"1.0","image":"base.png","image_size":[1,1],
     "mask_color":"#FF00FF","background_alpha":255,
     "frames":[{"rect":[0,0,1,1],"pivot":[0,0],"anchors":{}}]}
    """;
    await File.WriteAllTextAsync(Path.Combine(bundleRoot, "base_s.atlas.json"), parentAtlas);
    await File.WriteAllTextAsync(Path.Combine(bundleRoot, "base_e.atlas.json"), parentAtlas);
    var mountErrors = await new GameArtsBundleFileValidator().ValidateAsync(bundlePath);
    Assert(mountErrors.Any(e => e == "PARENT_MOUNT_MISSING:door:S:0") &&
        mountErrors.Any(e => e == "PARENT_MOUNT_MISSING:door:E:0"),
        "Every parent direction/frame must provide the child mount");
    var bundleFiles = await new GameArtsBundleFileValidator().ValidateAsync(bundlePath);
    Assert(bundleFiles.Any(e => e.StartsWith("ATLAS_MISSING")),
        "Bundle rejects missing atlas files instead of treating references as verified");
    await File.WriteAllTextAsync(bundlePath, nativeJson.Replace("\"E\":\"door_e.atlas.json\"", "\"bad\":\"door_e.atlas.json\""));
    var missingDirection = await new GameArtsBundleReader().ReadAsync(bundlePath);
    Assert(!missingDirection.IsValid && missingDirection.Errors.Any(e => e.StartsWith("INCOMPLETE_COMPONENT_DIRECTIONS")),
        "Native bundle missing component direction rejected");
}
finally { Directory.Delete(bundleRoot, recursive: true); }


var atlasTestDir = Path.Combine(Path.GetTempPath(), "gamearts-atlas-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(atlasTestDir);
try
{
    var pngFile = Path.Combine(atlasTestDir, "sprite.png");
    var atlasFile = Path.Combine(atlasTestDir, "sprite.atlas.json");
    await File.WriteAllBytesAsync(pngFile, Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/3ioAAAAASUVORK5CYII="));
    var validAtlas = """
    {"schema_version":"1.0","image":"sprite.png","image_size":[1,1],
     "mask_color":"#FF00FF","background_alpha":255,
     "frames":[{"rect":[0,0,1,1],"pivot":[0,0],"anchors":{"mount":[0,0]}}]}
    """;
    await File.WriteAllTextAsync(atlasFile, validAtlas);
    Assert((await new GameArtsAtlasValidator().ValidateAsync(atlasFile, pngFile)).Count == 0,
        "Real PNG and measured atlas geometry accepted");
    await File.WriteAllTextAsync(atlasFile, validAtlas.Replace("[0,0,1,1]", "[0,0,2,1]"));
    Assert((await new GameArtsAtlasValidator().ValidateAsync(atlasFile, pngFile))
        .Any(e => e.StartsWith("FRAME_OUT_OF_BOUNDS")),
        "Atlas rectangles must fit actual sheet");
    await File.WriteAllTextAsync(atlasFile, validAtlas.Replace("\"pivot\":[0,0]", "\"pivot\":[2,0]"));
    Assert((await new GameArtsAtlasValidator().ValidateAsync(atlasFile, pngFile))
        .Any(e => e.StartsWith("PIVOT_OUT_OF_BOUNDS")),
        "Atlas pivot must be cell-local and in bounds");
}
finally { Directory.Delete(atlasTestDir, true); }


var motionFolder = Path.Combine(Path.GetTempPath(), "gamearts-motion-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(motionFolder);
try
{
    var file = Path.Combine(motionFolder, "sample.bundle.json");
    var motionJson = """
    {"schema_version":"1.0","asset_id":"gate","directions":["S"],
     "component_order":["base"],
     "components":[{"id":"base","parent":null,"atlases":{"S":"base_s.atlas.json"}}],
     "sampled_poses":[{"direction":"S","normalized_time":0.5,
       "nodes":{"base":{"frame_index":0,"world_matrix_2d":[[1,0,0],[0,1,0],[0,0,1]]}}}]}
    """;
    await File.WriteAllTextAsync(file, motionJson);
    Assert((await new GameArtsMotionValidator().ValidateAsync(file)).Count == 0,
        "Finite sampled component pose accepted");
    await File.WriteAllTextAsync(file, motionJson.Replace("\"normalized_time\":0.5", "\"normalized_time\":1.5"));
    Assert((await new GameArtsMotionValidator().ValidateAsync(file))
        .Contains("POSE_DIRECTION_OR_TIME_INVALID"), "Out-of-range normalized pose time rejected");
    await File.WriteAllTextAsync(file, motionJson.Replace("\"frame_index\":0", "\"frame_index\":-1"));
    Assert((await new GameArtsMotionValidator().ValidateAsync(file))
        .Contains("POSE_FRAME_INDEX_INVALID:base"), "Negative animation frame rejected");
}
finally { Directory.Delete(motionFolder, true); }


var approvalRoot = Path.Combine(Path.GetTempPath(), "gamearts-gate-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(approvalRoot);
try
{
    var bundlePath = Path.Combine(approvalRoot, "empty.bundle.json");
    await File.WriteAllTextAsync(bundlePath, """
    {"schema_version":"1.0","asset_id":"sample","directions":["S"],
     "components":[{"id":"base","parent":null,"atlases":{"S":"missing.atlas.json"}}],
     "sampled_poses":[{"direction":"S","normalized_time":0,
     "nodes":{"base":{"frame_index":0,"world_matrix_2d":[[1,0,0],[0,1,0],[0,0,1]]}}}]}
    """);
    var gate = new GameArtsProductionGate(new TestApprovalAuthority(true));
    var result = await gate.CheckAsync(bundlePath, "r1");
    Assert(!result.Ready && result.Approved && result.Issues.Any(i => i.StartsWith("ATLAS_MISSING")),
        "Approved but incomplete bundles cannot pass readiness");
    var unapproved = await new GameArtsProductionGate(new TestApprovalAuthority(false))
        .CheckAsync(bundlePath, "r1");
    Assert(!unapproved.Ready && unapproved.Issues.Contains("LIFECYCLE_APPROVAL_NOT_VERIFIED"),
        "Technical checks cannot bypass approval authority");
}
finally { Directory.Delete(approvalRoot, true); }

var villageDir = Path.Combine(Path.GetTempPath(), "gameframework-village-" + Guid.NewGuid().ToString("N"));
try
{
    await new DummyVillageAssembler().ExportAsync(villageDir);
    foreach (var name in new[] { "project.godot", "Village.tscn", "Village.gd", "design.json" })
        Assert(File.Exists(Path.Combine(villageDir,name)), "Dummy village emits " + name);
    using var village = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(villageDir, "design.json")));
    Assert(village.RootElement.GetProperty("Objects").GetArrayLength() == 7,
        "Dummy village creates two homes, separate doors, two trees and a sign");
    var gd = await File.ReadAllTextAsync(Path.Combine(villageDir, "Village.gd"));
    Assert(gd.Contains("door_open") && gd.Contains("for hx in [3, 12]"),
        "Dummy scene draws separate animated doors on planned buildings");
}
finally { if (Directory.Exists(villageDir)) Directory.Delete(villageDir, true); }


var packageBuilder = new WorldAssemblyPackageBuilder();
var (portable, portableReport) = packageBuilder.Build(visual,
    requiredDesign with { Objects = new[] { requiredDesign.Objects[0] } },
    new[] { new WorldArtBinding("depot", "warehouse", "door", "S", 2) });
Assert(portableReport.Valid && portable is not null && portable.Objects.Count == 1,
    "Approved visual design creates portable engine-independent package");
var (unboundPackage, unboundReport) = packageBuilder.Build(visual,
    requiredDesign, Array.Empty<WorldArtBinding>());
Assert(unboundPackage is null && unboundReport.Errors.Any(e => e.StartsWith("ART_REFERENCE_MISSING")),
    "World package rejects missing visual art bindings");
var portableDir = Path.Combine(Path.GetTempPath(), "world-package-" + Guid.NewGuid().ToString("N"));
try
{
    var packagePath = Path.Combine(portableDir, "world.json");
    await packageBuilder.WriteAsync(portable!, packagePath);
    using var json = JsonDocument.Parse(await File.ReadAllTextAsync(packagePath));
    Assert(json.RootElement.GetProperty("MockupId").GetString() == "town-mockup",
        "World assembly retains approved mockup identity");
}
finally { if (Directory.Exists(portableDir)) Directory.Delete(portableDir, true); }


var semanticBuilder = new SemanticWorldBuilder();
var squareShape = visual.Regions[0].Outline;
var semantic = semanticBuilder.Build(portable!, visual,
    new[] { new TerrainRegion("ground-square","grass",squareShape,"square","approved-terrain-v1") },
    new[] { new PlannedWorldPath("walkway","pedestrian",
        new WorldPolyline(new[] { new WorldPoint(4,4),new WorldPoint(24,4) }),
        "mockup-main-walkway","approved-path-v1") },
    new[] { new LandscapeZone("garden","flowerbed",new WorldPolygon(new[]
    { new WorldPoint(8,8),new WorldPoint(16,8),new WorldPoint(16,16),new WorldPoint(8,16) }),
        "square","approved-garden-v1") });
Assert(semantic.Valid && semantic.Package!.Terrain.Count == 1 &&
    semantic.Package.Paths.Count == 1 && semantic.Package.Landscape.Count == 1,
    "Semantic world includes terrain pathways and landscaping");
var badSemantic = semanticBuilder.Build(portable!, visual,
    new[] { new TerrainRegion("ground-square","grass",squareShape,"not-approved","approved-terrain-v1") },
    Array.Empty<PlannedWorldPath>(),Array.Empty<LandscapeZone>());
Assert(!badSemantic.Valid && badSemantic.Errors.Any(e => e.StartsWith("UNKNOWN_VISUAL_REGION")),
    "Semantic terrain cannot originate from an unknown visual region");


var gardenZone = semantic.Package!.Landscape.Single();
var gardenStyle = new LandscapeBandStyle("cozy-flower-rows", "pink-flowers", 1, 1, 1);
var gardenComposition = new LandscapeBandComposer().Compose(gardenZone, gardenStyle);
Assert(gardenComposition.Diagnostics.Count == 0 && gardenComposition.Bands.Count == 3,
    "Approved garden region yields three intentional planted rows");
Assert(gardenComposition.Bands.All(b => b.ParentZoneId == "garden" &&
    b.MaterialId == "pink-flowers" && b.Shape.SignedArea() > 0),
    "Garden bands preserve style and source zone");
Assert(JsonSerializer.Serialize(gardenComposition) ==
    JsonSerializer.Serialize(new LandscapeBandComposer().Compose(gardenZone, gardenStyle)),
    "Landscape composition is deterministic");
Assert(new LandscapeBandComposer().Compose(gardenZone,
    new LandscapeBandStyle("huge", "flowers", 10, 1, 1)).Diagnostics
    .Contains("LANDSCAPE_BORDER_EXCEEDS_ZONE:garden"),
    "Insufficient garden space reports failure instead of placing elsewhere");


var irregularGarden = new LandscapeZone("angled-garden", "flowerbed",
    new WorldPolygon(new[]
    {
        new WorldPoint(0,0), new WorldPoint(30,0),
        new WorldPoint(25,20), new WorldPoint(5,20)
    }), "square", "approved-angled-garden-v1");
var insetStyle = new LandscapeInsetStyle("cozy-border", "flowers", 2);
var angledInset = new ConvexLandscapeInsetComposer().Compose(irregularGarden, insetStyle);
Assert(angledInset.Diagnostics.Count == 0 && angledInset.Interior is not null,
    "Convex irregular garden supports designed interior inset");
Assert(Math.Abs(angledInset.Interior!.SignedArea()) < Math.Abs(irregularGarden.Boundary.SignedArea()),
    "Inset garden interior remains smaller than parent region");
var excessiveInset = new ConvexLandscapeInsetComposer().Compose(irregularGarden,
    insetStyle with { InsetMeters = 100 });
Assert(excessiveInset.Interior is null && excessiveInset.Diagnostics.Count != 0,
    "Oversized landscape inset fails without fallback placement");


var concaveGarden = new WorldPolygon(new[]
{
    new WorldPoint(0,0), new WorldPoint(6,0), new WorldPoint(6,2),
    new WorldPoint(2,2), new WorldPoint(2,6), new WorldPoint(0,6)
});
var pieces = LandscapePolygonTriangulator.Triangulate(concaveGarden);
Assert(pieces.Count == 4, "Six-vertex concave garden decomposes into four triangles");
Assert(pieces.All(p => p.Vertices.Count == 3 && p.SignedArea() > 0),
    "Triangulated areas retain original winding and remain non-degenerate");
Assert(Math.Abs(pieces.Sum(p => p.SignedArea()) - concaveGarden.SignedArea()) < 1e-8,
    "Concave garden triangulation conserves authored area");
Assert(JsonSerializer.Serialize(pieces) ==
    JsonSerializer.Serialize(LandscapePolygonTriangulator.Triangulate(concaveGarden)),
    "Concave garden triangulation deterministic");


var concaveVisual = visual with { Regions = new[] {
    new VisualRegion("forest-edge", concaveGarden, "woodland-clearing")
}};
var terrainStyles = new Dictionary<string, TerrainCompositionStyle> {
    ["forest-edge"] = new("cozy-woodland", "forest-ground", "approved-woodland-fill-v1")
};
var styledTerrain = new StyledTerrainComposer().Compose(concaveVisual,terrainStyles);
Assert(styledTerrain.Diagnostics.Count == 0 && styledTerrain.Pieces.Count == 4,
    "Approved concave region creates four exact terrain fill pieces");
Assert(Math.Abs(styledTerrain.Pieces.Sum(p => Math.Abs(p.Geometry.SignedArea())) -
    Math.Abs(concaveGarden.SignedArea())) < 1e-8,
    "Styled terrain preserves authored region area");
Assert(styledTerrain.Pieces.All(p => p.SourceRegionId == "forest-edge" &&
    p.MaterialId == "forest-ground"), "Terrain keeps material and region provenance");
Assert(new StyledTerrainComposer().Compose(concaveVisual,
    new Dictionary<string, TerrainCompositionStyle>()).Diagnostics
    .Contains("REGION_STYLE_MISSING:forest-edge"), "Missing style cannot trigger arbitrary terrain");


var terrainBindings = new[] { new TerrainMaterialBinding("forest-ground","warehouse","door","S") };
var boundTerrain = await new TerrainArtAssembler(new InMemoryRegistry(full))
    .AssembleAsync(styledTerrain, terrainBindings);
Assert(boundTerrain.IsValid && boundTerrain.Pieces.Count == styledTerrain.Pieces.Count,
    "Approved terrain material preserves every styled polygon");
Assert(boundTerrain.Pieces.All(piece => piece.Material.MaterialId == "forest-ground"),
    "Terrain assembly retains original design material");
var unavailableTerrain = await new TerrainArtAssembler(new InMemoryRegistry(null))
    .AssembleAsync(styledTerrain, terrainBindings);
Assert(!unavailableTerrain.IsValid &&
    unavailableTerrain.Issues.Any(issue => issue.StartsWith("TERRAIN_ART_NOT_APPROVED")),
    "Missing GameArts terrain material fails without substitute");
var unboundTerrain = await new TerrainArtAssembler(new InMemoryRegistry(full))
    .AssembleAsync(styledTerrain, Array.Empty<TerrainMaterialBinding>());
Assert(!unboundTerrain.IsValid &&
    unboundTerrain.Issues.Any(issue => issue.StartsWith("TERRAIN_MATERIAL_UNBOUND")),
    "Engine does not choose arbitrary terrain textures");


var landscapeWorldPlan = new LandscapeWorldPlanner().Generate(semantic.Package!,
    new[] { new LandscapeDesignGrammar("garden", "cozy-rows", "pink-flowers", 1, 1, 1) });
Assert(landscapeWorldPlan.IsValid && landscapeWorldPlan.Features.Count == 3,
    "Semantic world generates planned garden rows rather than scattered plants");
Assert(landscapeWorldPlan.Features.All(x => x.ZoneId == "garden" &&
    x.VisualRegionId == "square" && x.MaterialId == "pink-flowers"),
    "Garden detail retains approved visual region and material");
Assert(JsonSerializer.Serialize(landscapeWorldPlan) ==
    JsonSerializer.Serialize(new LandscapeWorldPlanner().Generate(semantic.Package!,
        new[] { new LandscapeDesignGrammar("garden", "cozy-rows", "pink-flowers", 1, 1, 1) })),
    "Landscape generation deterministic");
var missingGardenRules = new LandscapeWorldPlanner().Generate(semantic.Package!,
    Array.Empty<LandscapeDesignGrammar>());
Assert(!missingGardenRules.IsValid &&
    missingGardenRules.Diagnostics.Contains("GARDEN_GRAMMAR_MISSING:garden"),
    "Missing garden design refuses arbitrary placement");

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
sealed class TestApprovalAuthority(bool approved) : IGameArtsApprovalAuthority
{
    public Task<bool> IsApprovedAsync(string assetId, string revision,
        CancellationToken token = default) => Task.FromResult(approved);
}
sealed class InMemoryRegistry(ApprovedArtManifest? manifest) : IApprovedArtRegistry
{
    public Task<ApprovedArtManifest?> ResolveAsync(string assetId, CancellationToken cancellationToken = default)
        => Task.FromResult(manifest);
}
