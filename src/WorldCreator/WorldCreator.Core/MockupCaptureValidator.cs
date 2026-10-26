using WorldCreator.Core.Geometry;

namespace WorldCreator.Core.Capture;

public static class MockupCaptureValidator
{
    public static CaptureReport Validate(MockupCaptureDocument capture, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(capture);
        token.ThrowIfCancellationRequested();
        var issues = new List<CaptureIssue>();
        void Fail(string code, string id, string message) => issues.Add(new(code, id, message));
        var source = capture.Source;
        if (capture.SchemaVersion != 1 || source is null || source.Width is <= 0 or > 32768 || source.Height is <= 0 or > 32768 ||
            new[] { source.MockupId, source.Revision, source.File, source.ArtStyleId, source.Camera, source.LifecycleManifest }.Any(string.IsNullOrWhiteSpace) ||
            source.Sha256 is null || source.Sha256.Length != 64 || !source.Sha256.All(Uri.IsHexDigit))
            Fail("CAPTURE_SOURCE_INVALID", "source", "Current schema, identity, PNG hash, dimensions, camera, style and lifecycle reference required.");
        if (!Enum.IsDefined(capture.Coverage) || capture.Unresolved is null || capture.Unresolved.Any(string.IsNullOrWhiteSpace) ||
            (capture.Coverage == CaptureCoverage.Partial && capture.Unresolved.Count == 0) ||
            (capture.Coverage == CaptureCoverage.Complete && capture.Unresolved.Count != 0))
            Fail("CAPTURE_COVERAGE_INVALID", "coverage", "Partial capture must enumerate gaps; complete capture cannot contain unresolved items.");
        if (capture.Features is null || capture.Features.Count is < 1 or > 2000 || capture.Junctions is null || capture.Junctions.Count > 4000)
            Fail("CAPTURE_INVENTORY_INVALID", "features", "Require 1..2000 features and at most 4000 explicit junctions.");
        if (issues.Count != 0) return new(false, "invalid", issues, []);
        bool InBounds(CapturePixel p) => double.IsFinite(p.X) && double.IsFinite(p.Y) && p.X >= 0 && p.Y >= 0 && p.X <= source!.Width && p.Y <= source.Height;
        var ids = new Dictionary<string, CaptureFeature>(StringComparer.Ordinal);
        var bindings = new HashSet<string>(StringComparer.Ordinal);
        int segments = 0;
        foreach (var f in capture.Features!)
        {
            token.ThrowIfCancellationRequested();
            if (f is null) { Fail("CAPTURE_FEATURE_INVALID", "features", "Null feature."); continue; }
            if (string.IsNullOrWhiteSpace(f.Id) || !ids.TryAdd(f.Id, f) || string.IsNullOrWhiteSpace(f.Semantic) || string.IsNullOrWhiteSpace(f.Notes) ||
                !Enum.IsDefined(f.Shape) || !Enum.IsDefined(f.Evidence) || !Enum.IsDefined(f.Review) || !double.IsFinite(f.UncertaintyPixels) || f.UncertaintyPixels < 0 || f.UncertaintyPixels > Math.Max(source!.Width, source.Height))
                Fail("CAPTURE_FEATURE_INVALID", f.Id, "Unique ID, semantic, evidence, review, uncertainty and notes required.");
            if (f.Points is null || f.Points.Count > 4096 || f.Points.Any(p => !InBounds(p)))
            { Fail("CAPTURE_COORDINATES_INVALID", f.Id, "Finite source-pixel coordinates must stay within the original image."); continue; }
            segments += Math.Max(0, f.Points.Count - 1);
            try
            {
                if (f.Shape == CaptureShape.Point && f.Points.Count != 1) throw new ArgumentException();
                if (f.Shape == CaptureShape.Polyline) new WorldPolyline(f.Points.Select(P).ToArray()).Validate();
                if (f.Shape == CaptureShape.Polygon) new WorldPolygon(f.Points.Select(P).ToArray()).Validate();
            }
            catch (ArgumentException) { Fail("CAPTURE_GEOMETRY_INVALID", f.Id, "Point, open polyline or simple polygon required; do not repeat the closing vertex."); }
            if (f.Semantic == "road.centerline" && (f.Road is null || f.Shape != CaptureShape.Polyline))
                Fail("CAPTURE_ROAD_INVALID", f.Id, "Road centerlines require polyline geometry and explicit road metadata.");
            if (f.Road is { } road && (f.Semantic != "road.centerline" || !double.IsFinite(road.WidthPixels) || road.WidthPixels <= 0 ||
                road.WidthPixels > Math.Max(source!.Width, source.Height) || !Enum.IsDefined(road.WidthKind) || road.Level is < -10 or > 10 ||
                road.AccessModes is null || road.AccessModes.Count == 0 || road.AccessModes.Any(string.IsNullOrWhiteSpace) || road.AccessModes.Distinct().Count() != road.AccessModes.Count))
                Fail("CAPTURE_ROAD_INVALID", f.Id, "Positive observed width, width meaning, optional level and explicit access modes required.");
            if (f.Binding is { } binding && (binding.Kind is not ("path" or "region" or "anchor") || string.IsNullOrWhiteSpace(binding.TargetId) ||
                !bindings.Add(binding.Kind + ":" + binding.TargetId) ||
                (binding.Kind == "path" && f.Shape != CaptureShape.Polyline) || (binding.Kind == "region" && f.Shape != CaptureShape.Polygon) ||
                (binding.Kind == "anchor" && f.Shape != CaptureShape.Point)))
                Fail("CAPTURE_BINDING_INVALID", f.Id, "Unique target and matching path/region/anchor shape required.");
            if (f.Binding is not null && (f.Semantic == "building.roof" || f.Evidence == CaptureEvidence.Unknown))
                Fail("CAPTURE_BINDING_UNGROUNDED", f.Id, "Roof silhouettes and unknown geometry cannot define engine ground geometry.");
        }
        if (issues.Count != 0) return new(false,"invalid",issues,[]);
        foreach (var f in ids.Values)
        {
            var parents = new HashSet<string> { f.Id }; var current = f;
            while (current.ParentId is { } parent)
            {
                if (!ids.TryGetValue(parent, out var next) || !parents.Add(parent))
                { Fail("CAPTURE_PARENT_INVALID", f.Id, "Parent must exist and parent graph must be acyclic."); break; }
                current = next;
            }
            if (f.VegetationPlacement is { } placement)
            {
                if (f.Shape != CaptureShape.Point || !f.Semantic.StartsWith("vegetation.",StringComparison.Ordinal) ||
                    !double.IsFinite(placement.GroundRadiusPixels) || placement.GroundRadiusPixels <= 0 || placement.GroundRadiusPixels > 256 ||
                    f.ParentId is null || !ids.TryGetValue(f.ParentId,out var ground) || ground.Shape != CaptureShape.Polygon || !ground.Semantic.StartsWith("landscape.",StringComparison.Ordinal) ||
                    placement.ProtectedFeatureIds is null || placement.ProtectedFeatureIds.Count > 32 || placement.ProtectedFeatureIds.Distinct().Count() != placement.ProtectedFeatureIds.Count ||
                    placement.ProtectedFeatureIds.Any(id=>id is null || id==f.Id || id==f.ParentId || !ids.TryGetValue(id,out var other) ||
                        (other.Shape!=CaptureShape.Polygon && other.Road is null)))
                { Fail("CAPTURE_VEGETATION_PLACEMENT_INVALID",f.Id,"Point vegetation requires a planted-ground parent, finite ground radius and existing protected polygon/road references."); continue; }
                if (f.Points.Count != 1 || ground.Points is null || ground.Points.Count < 3) continue; // Prior geometry diagnostics own malformed inputs.
                var p=P(f.Points[0]);var polygon=new WorldPolygon(ground.Points.Select(P).ToArray());
                if (!polygon.ContainsPoint(p) || DistanceToEdges(p,polygon.Vertices)+1e-8<placement.GroundRadiusPixels)
                    Fail("CAPTURE_VEGETATION_OUTSIDE_GROUND",f.Id,"Revise the independently traced ground boundary; never move or drop the captured tree.");
                foreach (var id in placement.ProtectedFeatureIds)
                {
                    token.ThrowIfCancellationRequested();
                    var protectedFeature=ids[id];
                    double distance=protectedFeature.Road is { } road
                        ? Distance(f.Points[0],protectedFeature.Points)-road.WidthPixels/2
                        : new WorldPolygon(protectedFeature.Points.Select(P).ToArray()).DistanceToPoint(p);
                    if (distance<=placement.GroundRadiusPixels+1e-8)
                        Fail("CAPTURE_VEGETATION_BOUNDARY_CONFLICT",f.Id,"Tree ground footprint conflicts with protected feature "+id+"; revise the design, not its position.");
                }
            }
        }
        if (segments > 4096) Fail("CAPTURE_SEGMENT_LIMIT", "features", "At most 4096 segments per capture; split larger regions into separate documents.");
        if (capture.InspectionAreas is { } areas)
        {
            var areaIds = new HashSet<string>(StringComparer.Ordinal);
            if (areas.Count is < 1 or > 256) Fail("CAPTURE_INSPECTION_INVALID", "inspectionAreas", "Require 1..256 inspection areas when supplied.");
            foreach (var area in areas)
            {
                token.ThrowIfCancellationRequested();
                if (area is null || string.IsNullOrWhiteSpace(area.Id) || !areaIds.Add(area.Id) || !Enum.IsDefined(area.Status) ||
                    area.Bounds is not { X: >= 0, Y: >= 0, Width: > 0, Height: > 0 } b ||
                    (long)b.X + b.Width > source!.Width || (long)b.Y + b.Height > source.Height ||
                    area.FeatureIds is null || area.FeatureIds.Distinct().Count() != area.FeatureIds.Count ||
                    area.FeatureIds.Any(id => id is null || !ids.ContainsKey(id)) ||
                    area.RemainingWork is null || area.RemainingWork.Any(string.IsNullOrWhiteSpace) ||
                    (area.Status == CaptureInspectionStatus.Complete ? area.RemainingWork.Count != 0 || area.FeatureIds.Count == 0 : area.RemainingWork.Count == 0) ||
                    (capture.Coverage == CaptureCoverage.Complete && area.Status != CaptureInspectionStatus.Complete))
                    Fail("CAPTURE_INSPECTION_INVALID", area?.Id ?? "inspectionAreas", "Unique area, source bounds, existing feature references and honest status/work remaining required.");
                else if (area.FeatureIds.Any(id => !ids[id].Points.Any(p => p.X >= b.X && p.Y >= b.Y && p.X <= (long)b.X + b.Width && p.Y <= (long)b.Y + b.Height)))
                    Fail("CAPTURE_INSPECTION_LOCATION_INVALID", area.Id, "Each referenced feature must have a source coordinate inside its inspection area.");
            }
        }
        if (capture.SurfaceChecks is { } checks)
        {
            if (checks.Count is < 1 or > 4096) Fail("CAPTURE_SURFACE_CHECK_INVALID","surfaceChecks","Require 1..4096 source-reviewed samples when supplied.");
            var samples=new HashSet<(string,double,double)>();
            foreach(var check in checks)
            {
                token.ThrowIfCancellationRequested();
                if(check is null || string.IsNullOrWhiteSpace(check.FeatureId) || !InBounds(check.Pixel) || string.IsNullOrWhiteSpace(check.Notes) ||
                    !ids.TryGetValue(check.FeatureId,out var target) || target.Shape!=CaptureShape.Polygon ||
                    !samples.Add((check.FeatureId,check.Pixel.X,check.Pixel.Y)))
                { Fail("CAPTURE_SURFACE_CHECK_INVALID",check?.FeatureId??"surfaceChecks","Unique, bounded source samples must reference existing polygons and state their evidence.");continue; }
                if(new WorldPolygon(target.Points.Select(P).ToArray()).ContainsPoint(P(check.Pixel))!=check.Inside)
                    Fail("CAPTURE_SURFACE_CHECK_FAILED",check.FeatureId,"Contour contradicts source-reviewed "+(check.Inside?"interior":"exterior")+" sample: "+check.Notes);
            }
        }
        if (capture.CompilationScope is { } scope && (string.IsNullOrWhiteSpace(scope.Id) || string.IsNullOrWhiteSpace(scope.Purpose) ||
            scope.FeatureIds is null || scope.FeatureIds.Count == 0 || scope.FeatureIds.Distinct().Count() != scope.FeatureIds.Count ||
            scope.FeatureIds.Any(id => id is null || !ids.TryGetValue(id,out var f) || f.Binding is null) ||
            scope.ExcludedContent is null || scope.ExcludedContent.Count == 0 || scope.ExcludedContent.Any(string.IsNullOrWhiteSpace)))
            Fail("CAPTURE_COMPILATION_SCOPE_INVALID", "scope", "A named, explicit set of existing bound features, purpose and exclusions is required.");
        if (issues.Count != 0) return new(false, "invalid", issues, []);
        var junctionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var j in capture.Junctions!)
        {
            if (j is null || string.IsNullOrWhiteSpace(j.Id) || !junctionIds.Add(j.Id) || !InBounds(j.Position) || !Enum.IsDefined(j.Kind) ||
                !Enum.IsDefined(j.Review) || string.IsNullOrWhiteSpace(j.Notes) || j.RoadIds is null || j.RoadIds.Count < 2 || j.RoadIds.Distinct().Count() != j.RoadIds.Count ||
                j.RoadIds.Any(id => !ids.TryGetValue(id, out var f) || f.Road is null))
            { Fail("CAPTURE_JUNCTION_INVALID", j?.Id ?? "junctions", "Unique junction, position, review and at least two existing roads required."); continue; }
            if (j.RoadIds.Any(id => Distance(j.Position, ids[id].Points) > 0.5))
                Fail("CAPTURE_JUNCTION_OFF_ROAD", j.Id, "Junction must lie on each captured centerline within 0.5 source pixels.");
            var levels = j.RoadIds.Select(id => ids[id].Road!.Level).ToArray();
            if ((j.Kind == CaptureJunctionKind.AtGrade && (levels.Any(l => l is null) || levels.Distinct().Count() != 1)) ||
                (j.Kind == CaptureJunctionKind.GradeSeparated && (levels.Any(l => l is null) || levels.Distinct().Count() < 2)))
                Fail("CAPTURE_JUNCTION_LEVEL_INVALID", j.Id, "At-grade requires one known level; grade-separated requires distinct known levels.");
        }
        if (capture.Calibration is { } c)
        {
            ValidateCalibration(c, InBounds, Fail);
            if (issues.Count == 0 && ids.Values.SelectMany(f => f.Points).Select(p => Transform(c, p)).Any(p => !double.IsFinite(p.X) || !double.IsFinite(p.Y)))
                Fail("CAPTURE_CALIBRATION_OVERFLOW", "calibration", "All transformed feature coordinates must remain finite.");
        }
        if (issues.Count != 0) return new(false, "invalid", issues, []);
        var crossings = new List<CaptureCrossing>();
        var roads = ids.Values.Where(f => f.Road is not null).OrderBy(f => f.Id, StringComparer.Ordinal).ToArray();
        for (int i = 0; i < roads.Length; i++)
        for (int j = i + 1; j < roads.Length; j++)
        {
            token.ThrowIfCancellationRequested();
            var a = roads[i]; var b = roads[j]; var hits = new List<CapturePixel>();
            for (int ai = 1; ai < a.Points.Count; ai++)
            for (int bi = 1; bi < b.Points.Count; bi++)
            {
                token.ThrowIfCancellationRequested();
                var hit = PlanarIntersections.Intersect(P(a.Points[ai - 1]), P(a.Points[ai]), P(b.Points[bi - 1]), P(b.Points[bi]));
                if (hit.Kind == IntersectionKind.CollinearOverlap) Fail("CAPTURE_ROAD_OVERLAP", a.Id, "Overlapping road centerlines: " + b.Id);
                if (hit.Point is { } p && !hits.Any(h => P(h).DistanceTo(p) < 0.01)) hits.Add(new(p.X, p.Y));
            }
            foreach (var p in hits)
            {
                var matches = capture.Junctions!.Where(k => k.RoadIds.Contains(a.Id) && k.RoadIds.Contains(b.Id) && P(k.Position).DistanceTo(P(p)) <= 0.5).ToArray();
                if (matches.Length > 1) Fail("CAPTURE_JUNCTION_AMBIGUOUS", a.Id, "Multiple junction records at one crossing with " + b.Id);
                var junction = matches.FirstOrDefault();
                crossings.Add(new(a.Id, b.Id, p, junction?.Id, junction?.Kind ?? CaptureJunctionKind.Unresolved));
            }
        }
        // Missing/unresolved crossings remain review blockers; projected crossings never imply a connection.
        return new(issues.Count == 0, issues.Count != 0 ? "invalid" : "provisional-review", issues, crossings);
    }

    internal static WorldPoint P(CapturePixel p) => new(p.X, p.Y);
    private static double DistanceToEdges(WorldPoint p,IReadOnlyList<WorldPoint> points)
    {
        double distance=double.PositiveInfinity;
        for(int i=0;i<points.Count;i++)
        {
            var a=points[i];var b=points[(i+1)%points.Count];double dx=b.X-a.X,dy=b.Y-a.Y;
            double t=Math.Clamp(((p.X-a.X)*dx+(p.Y-a.Y)*dy)/(dx*dx+dy*dy),0,1);
            distance=Math.Min(distance,p.DistanceTo(new(a.X+t*dx,a.Y+t*dy)));
        }
        return distance;
    }
    private static double Distance(CapturePixel p, IReadOnlyList<CapturePixel> points) => Enumerable.Range(1, points.Count - 1).Min(i =>
    {
        var a = P(points[i - 1]); var b = P(points[i]); var q = P(p);
        double dx = b.X - a.X, dy = b.Y - a.Y;
        double t = Math.Clamp(((q.X - a.X) * dx + (q.Y - a.Y) * dy) / (dx * dx + dy * dy), 0, 1);
        return q.DistanceTo(new(a.X + dx * t, a.Y + dy * t));
    });
    private static void ValidateCalibration(CaptureCalibration c, Func<CapturePixel, bool> inBounds, Action<string, string, string> fail)
    {
        if (new[] { c.A, c.B, c.C, c.D, c.E, c.F, c.ToleranceMeters }.Any(v => !double.IsFinite(v)) ||
            !double.IsFinite(c.A * c.E - c.B * c.D) || Math.Abs(c.A * c.E - c.B * c.D) < 1e-12 || c.ToleranceMeters <= 0 || string.IsNullOrWhiteSpace(c.Basis) ||
            c.Controls is null || c.Controls.Count is < 3 or > 64 || c.Controls.Any(p => p is null || !inBounds(p.Pixel) || !double.IsFinite(p.WorldX) || !double.IsFinite(p.WorldY)))
        { fail("CAPTURE_CALIBRATION_INVALID", "calibration", "Finite invertible affine transform, basis, tolerance and 3..64 control points required."); return; }
        var first = c.Controls[0].Pixel;
        if (!c.Controls.Skip(1).Any(a => c.Controls.Skip(1).Any(b => Math.Abs((a.Pixel.X - first.X) * (b.Pixel.Y - first.Y) - (a.Pixel.Y - first.Y) * (b.Pixel.X - first.X)) > 1e-6)))
            fail("CAPTURE_CALIBRATION_DEGENERATE", "calibration", "Control pixels must not be collinear.");
        if (c.Controls.Any(p => !double.IsFinite(Transform(c,p.Pixel).X) || !double.IsFinite(Transform(c,p.Pixel).Y) || Transform(c, p.Pixel).DistanceTo(new(p.WorldX, p.WorldY)) > c.ToleranceMeters))
            fail("CAPTURE_CALIBRATION_RESIDUAL", "calibration", "Control points do not match the explicit transform within tolerance.");
    }
    public static WorldPoint Transform(CaptureCalibration c, CapturePixel p) => new(c.A * p.X + c.B * p.Y + c.C, c.D * p.X + c.E * p.Y + c.F);
}
