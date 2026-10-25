using GameFrameWork.Core.Geometry;

namespace GameFrameWork.Core.Planning;

/// <summary>Validates the structured design boundary before any generation or raster allocation.</summary>
public sealed class MockupDesignValidator(ISettlementCatalogRegistry catalogs)
{
    public IReadOnlyList<WorldDesignIssue> Validate(WorldRequest request, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        token.ThrowIfCancellationRequested();
        request.Validate();
        var design = request.Design;
        if (design is null) return [new("MOCKUP_DESIGN_REQUIRED", "", "Provide a structured mockup design; no placement fallback exists.")];
        if (design.SchemaVersion != 1 || design.Blueprint is null ||
            !double.IsFinite(design.MetersPerCell) || design.MetersPerCell <= 0 ||
            !double.IsFinite(design.MetersPerCell * Math.Max(request.Width, request.Height)) ||
            design.Paths is null || design.RoadRules is null || design.SettlementRules is null ||
            design.TerrainRules is null || design.PublicRealmPatterns is null || design.BuildingCatalog is null)
            return [new("MOCKUP_DESIGN_INVALID", "", "The current schema, positive scale and complete design collections are required.")];
        RegisteredSettlementCatalog pack;
        try { pack=catalogs.Resolve(design.Catalog!); }
        catch (ArgumentException ex) { return [new(ex.Message,"","Explicit registered catalog selection is required.")]; }
        var issues = new List<WorldDesignIssue>();
        if (design.Paths.Count > 10000 || design.SettlementRules.Count > 10000 || design.PublicRealmPatterns.Count > 10000 ||
            design.BuildingCatalog.Count > 10000 || design.TerrainRules.Count > 10000)
            return [new("MOCKUP_DESIGN_BUDGET_EXCEEDED","","Split this design into smaller planned regions before generation.")];
        void Fail(string code, string id) => issues.Add(new(code, id, "Revise the authored design; no relocation or approximation is performed."));
        var blueprint = design.Blueprint;
        if (blueprint.Regions is null || blueprint.Anchors is null || blueprint.Regions.Any(r => r is null || r.Outline is null) ||
            blueprint.Anchors.Any(a => a is null))
            return [new("MOCKUP_DESIGN_INVALID", "", "Visual regions and anchors must be complete.")];
        issues.AddRange(new VisualWorldPlanner().Plan(blueprint, [], "mockup-validation",
            new(request.Width, request.Height), design.MetersPerCell,token).Issues);
        if (issues.Count != 0) return issues;
        var regions = blueprint.Regions.ToDictionary(r => r.Id, StringComparer.Ordinal);
        foreach (var region in regions.Values)
            if (region.Outline.Vertices.Any(p => p.X < 0 || p.Y < 0 ||
                p.X > request.Width * design.MetersPerCell || p.Y > request.Height * design.MetersPerCell))
                Fail("MOCKUP_REGION_OUTSIDE_WORLD", region.Id);
        var paths = new Dictionary<string, VisualDesignPath>(StringComparer.Ordinal);
        foreach (var path in design.Paths)
        {
            token.ThrowIfCancellationRequested();
            if (path is null || string.IsNullOrWhiteSpace(path.Id) || string.IsNullOrWhiteSpace(path.RegionId) ||
                string.IsNullOrWhiteSpace(path.Role) || path.Centerline is null || !paths.TryAdd(path.Id, path))
            { Fail("MOCKUP_PATH_INVALID", path?.Id ?? ""); continue; }
            if (!regions.TryGetValue(path.RegionId, out var region))
            { Fail("MOCKUP_PATH_REGION_MISSING", path.Id); continue; }
            try
            {
                path.Centerline.Validate();
                // A convex parent guarantees each segment stays inside its region.
                if (!ConvexParcelGeometry.IsConvex(region.Outline)) Fail("MOCKUP_PATH_REGION_NOT_CONVEX", path.Id);
                if (path.Centerline.Points.Any(p => !region.Outline.ContainsPoint(p) || p.X < 0 || p.Y < 0 ||
                    p.X >= request.Width * design.MetersPerCell || p.Y >= request.Height * design.MetersPerCell))
                    Fail("MOCKUP_PATH_OUTSIDE_REGION", path.Id);
            }
            catch (ArgumentException) { Fail("MOCKUP_PATH_GEOMETRY_INVALID", path.Id); }
        }
        if (issues.Count != 0) return issues;
        if (design.Capture is not null)
        {
            try { Capture.MockupCapturePipeline.ValidateDesignTrace(design, token); }
            catch (ArgumentException ex) { return [new("MOCKUP_CAPTURE_TRACE_INVALID", "capture", ex.Message)]; }
        }
        var roads = new HashSet<string>(StringComparer.Ordinal);
        foreach (var rule in design.RoadRules)
        {
            token.ThrowIfCancellationRequested();
            if (rule is null || string.IsNullOrWhiteSpace(rule.PathId) || !roads.Add(rule.PathId) ||
                !paths.TryGetValue(rule.PathId, out var path) || !Enum.IsDefined(rule.Class) || rule.Profile is null)
            { Fail("MOCKUP_ROAD_RULE_INVALID", rule?.PathId ?? ""); continue; }
            try { rule.Profile.Validate(); }
            catch (ArgumentException) { Fail("MOCKUP_ROAD_PROFILE_INVALID", rule.PathId); }
            if (issues.Count == 0)
                foreach (var surface in StreetSurfaceBuilder.Build(new(path.Id,path.Centerline,rule.Profile)))
                    if (surface.Polygon.Any(p => !regions[path.RegionId].Outline.ContainsPoint(p)))
                        Fail("MOCKUP_ROAD_EXTENT_OUTSIDE_REGION", rule.PathId);
        }
        if (issues.Count != 0) return issues;
        var catalog = new Dictionary<string, BuildingDefinition>(StringComparer.Ordinal);
        foreach (var definition in design.BuildingCatalog)
        {
            token.ThrowIfCancellationRequested();
            if (definition is null || string.IsNullOrWhiteSpace(definition.Id) || !catalog.TryAdd(definition.Id, definition))
            { Fail("MOCKUP_BUILDING_CATALOG_INVALID", definition?.Id ?? ""); continue; }
            try { definition.Validate(); }
            catch (ArgumentException) { Fail("MOCKUP_BUILDING_CATALOG_INVALID", definition.Id); }
        }
        if (issues.Count != 0) return issues;
        var grammars = new HashSet<string>(StringComparer.Ordinal);
        var settlements = new HashSet<string>(StringComparer.Ordinal);
        var contexts = new Dictionary<string,SettlementDemand>(StringComparer.Ordinal);
        foreach (var rule in design.SettlementRules)
        {
            token.ThrowIfCancellationRequested();
            if (rule is null || string.IsNullOrWhiteSpace(rule.Id) || !grammars.Add(rule.Id) ||
                string.IsNullOrWhiteSpace(rule.SettlementId) || string.IsNullOrWhiteSpace(rule.RegionId) ||
                !regions.ContainsKey(rule.RegionId) || string.IsNullOrWhiteSpace(rule.Purpose) ||
                string.IsNullOrWhiteSpace(rule.FrontagePathId) || !roads.Contains(rule.FrontagePathId) ||
                rule.Envelope is null || rule.Components is null ||
                !double.IsFinite(rule.TargetFrontageMeters) || rule.TargetFrontageMeters <= 0 ||
                !double.IsFinite(rule.MinimumFrontageMeters) || rule.MinimumFrontageMeters <= 0 ||
                rule.MinimumFrontageMeters > rule.TargetFrontageMeters)
            { Fail("MOCKUP_SETTLEMENT_RULE_INVALID", rule?.Id ?? ""); continue; }
            settlements.Add(rule.SettlementId);
            try { rule.Envelope.Validate(); }
            catch (ArgumentException) { Fail("MOCKUP_BUILDING_ENVELOPE_INVALID", rule.Id); }
            if (!catalog.ContainsKey(rule.Envelope.BuildingType) || rule.RequiredCapacity < 0)
                Fail("MOCKUP_BUILDING_DEFINITION_MISSING_OR_INVALID", rule.Id);
            try { rule.Settlement?.Validate(); }
            catch (ArgumentException) { Fail("MOCKUP_SETTLEMENT_CONTEXT_INVALID", rule.Id); }
            if (rule.Settlement is { } context && !contexts.TryAdd(rule.SettlementId,context) && contexts[rule.SettlementId] != context)
                Fail("MOCKUP_SETTLEMENT_CONTEXT_CONFLICT",rule.SettlementId);
            var components = new HashSet<string>(StringComparer.Ordinal);
            foreach (var part in rule.Components)
                if (part is null || string.IsNullOrWhiteSpace(part.Id) || !components.Add(part.Id) ||
                    string.IsNullOrWhiteSpace(part.Kind) || string.IsNullOrWhiteSpace(part.Purpose) ||
                    string.IsNullOrWhiteSpace(part.State))
                    Fail("MOCKUP_COMPONENT_RULE_INVALID", rule.Id);
        }
        var terrain = new HashSet<string>(StringComparer.Ordinal);
        foreach (var rule in design.TerrainRules)
            if (rule is null || string.IsNullOrWhiteSpace(rule.RegionId) || !regions.ContainsKey(rule.RegionId) ||
                !terrain.Add(rule.RegionId) || string.IsNullOrWhiteSpace(rule.RuleId) ||
                rule.Kind is not (CellKind.Sand or CellKind.Grass or CellKind.Rock or CellKind.Water) ||
                !double.IsFinite(rule.ElevationMeters) || !double.IsFinite(rule.SlopeX) || !double.IsFinite(rule.SlopeY) ||
                (rule.SurfaceMaterialId is { } material && (string.IsNullOrWhiteSpace(material) || material.Length>128 || material.Any(char.IsControl))))
                Fail("MOCKUP_TERRAIN_RULE_INVALID", rule?.RegionId ?? "");
        foreach (var pattern in design.PublicRealmPatterns)
            if (pattern is null || string.IsNullOrWhiteSpace(pattern.PathId) || !roads.Contains(pattern.PathId))
                Fail("MOCKUP_PUBLIC_PATH_INVALID", pattern?.Id ?? "");
        var zones = new HashSet<string>(StringComparer.Ordinal);
        foreach (var zone in design.LandscapeZones ?? [])
        {
            if (zone is null || string.IsNullOrWhiteSpace(zone.Id) || !zones.Add(zone.Id) || zone.Boundary is null ||
                string.IsNullOrWhiteSpace(zone.SourceVisualRegionId) || !regions.TryGetValue(zone.SourceVisualRegionId, out var region) ||
                string.IsNullOrWhiteSpace(zone.DesignRuleId) || string.IsNullOrWhiteSpace(zone.Kind) || !Enum.IsDefined(zone.Role))
            { Fail("MOCKUP_LANDSCAPE_ZONE_INVALID", zone?.Id ?? ""); continue; }
            try
            {
                zone.Boundary.Validate();
                if (!ConvexParcelGeometry.IsConvex(region.Outline) ||
                    zone.Boundary.Vertices.Any(p => !region.Outline.ContainsPoint(p)))
                    Fail("MOCKUP_LANDSCAPE_OUTSIDE_REGION", zone.Id);
            }
            catch (ArgumentException) { Fail("MOCKUP_LANDSCAPE_ZONE_INVALID", zone.Id); }
        }
        if (design.VegetationGrammars?.Any(g => g is null) == true)
            Fail("MOCKUP_VEGETATION_GRAMMAR_INVALID", "");
        foreach (var rule in design.InfrastructureRules ?? [])
        {
            if (rule is null || string.IsNullOrWhiteSpace(rule.SettlementId) || rule.Settlement is null) continue;
            settlements.Add(rule.SettlementId);
            if (!contexts.TryAdd(rule.SettlementId,rule.Settlement) && contexts[rule.SettlementId] != rule.Settlement)
                Fail("MOCKUP_SETTLEMENT_CONTEXT_CONFLICT",rule.SettlementId);
        }
        // Infrastructure can be the only designed facility in a settlement.
        if (settlements.Count != request.SettlementCount) Fail("MOCKUP_SETTLEMENT_COUNT_MISMATCH", "");
        if (issues.Count == 0)
            issues.AddRange(pack.Infrastructure.Plan(design,token).Issues.Select(i =>
                new WorldDesignIssue(i.Split(':')[0],i.Contains(':') ? i[(i.IndexOf(':')+1)..] : "", "Revise the infrastructure design.")));
        if (issues.Count==0) issues.AddRange(SurfaceClearanceValidator.Validate(design,token:token));
        if (issues.Count==0) issues.AddRange(new StructureSitingValidator(catalogs).Validate(design,token));
        return issues;
    }
}
