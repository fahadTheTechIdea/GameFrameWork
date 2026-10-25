using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;
using GameFrameWork.Core.Planning;

namespace GameFrameWork.Core.Capture;

// Source-image coordinates are deliberately distinct from world meters.
public readonly record struct CapturePixel(double X, double Y);
public sealed record CaptureSource(string MockupId, string Revision, string File, string Sha256,
    int Width, int Height, string ArtStyleId, string Camera, string LifecycleManifest);
public enum CaptureShape { Point, Polyline, Polygon }
public enum CaptureEvidence { Observed, Inferred, Authored, Unknown }
public enum CaptureReview { Pending, Accepted, Rejected }
public enum CaptureCoverage { Partial, Complete }
public enum CaptureJunctionKind { AtGrade, GradeSeparated, Unresolved }
public enum CaptureWidthKind { Carriageway, FullCorridor }
public sealed record CaptureRoad(double WidthPixels, CaptureWidthKind WidthKind, int? Level,
    IReadOnlyList<string> AccessModes);
public sealed record CaptureBinding(string Kind, string TargetId);
/// <summary>Ground footprint, independent of the crown. ParentId identifies its planted-ground polygon.</summary>
public sealed record CaptureVegetationPlacement(double GroundRadiusPixels, IReadOnlyList<string> ProtectedFeatureIds);
public sealed record CaptureFeature(string Id, string Semantic, CaptureShape Shape,
    IReadOnlyList<CapturePixel> Points, CaptureEvidence Evidence, CaptureReview Review,
    double UncertaintyPixels, bool Occluded, string Notes, string? ParentId = null,
    CaptureRoad? Road = null, CaptureBinding? Binding = null, CaptureVegetationPlacement? VegetationPlacement = null);
public sealed record CaptureJunction(string Id, CapturePixel Position,
    IReadOnlyList<string> RoadIds, CaptureJunctionKind Kind, CaptureReview Review, string Notes);
public sealed record CaptureControlPoint(CapturePixel Pixel, double WorldX, double WorldY);
/// <summary>WorldX=A*x+B*y+C, WorldY=D*x+E*y+F. This does not correct elevated roofs or perspective.</summary>
public sealed record CaptureCalibration(double A, double B, double C, double D, double E, double F,
    string Basis, double ToleranceMeters, IReadOnlyList<CaptureControlPoint> Controls);
public sealed record CaptureCompilationScope(string Id, IReadOnlyList<string> FeatureIds,
    string Purpose, IReadOnlyList<string> ExcludedContent);
public enum CaptureInspectionStatus { Uninspected, Partial, Complete }
/// <summary>Explicit inspection work remaining; annotation count is not a completeness measure.</summary>
public sealed record CaptureInspectionArea(string Id, CaptureCrop Bounds, CaptureInspectionStatus Status,
    IReadOnlyList<string> FeatureIds, IReadOnlyList<string> RemainingWork);
/// <summary>Source-reviewed interior/exterior sample; not a claim of pixel segmentation accuracy.</summary>
public sealed record CaptureSurfaceCheck(string FeatureId, CapturePixel Pixel, bool Inside, string Notes);
public sealed record MockupCaptureDocument(int SchemaVersion, CaptureSource Source,
    CaptureCoverage Coverage, IReadOnlyList<string> Unresolved,
    IReadOnlyList<CaptureFeature> Features, IReadOnlyList<CaptureJunction> Junctions,
    CaptureCalibration? Calibration = null, CaptureCompilationScope? CompilationScope = null,
    IReadOnlyList<CaptureInspectionArea>? InspectionAreas = null,
    IReadOnlyList<CaptureSurfaceCheck>? SurfaceChecks = null);
public sealed record CaptureIssue(string Code, string FeatureId, string Message);
public sealed record CaptureCrossing(string FirstRoadId, string SecondRoadId,
    CapturePixel Position, string? JunctionId, CaptureJunctionKind Kind);
public sealed record CaptureReport(bool Valid, string Status, IReadOnlyList<CaptureIssue> Issues,
    IReadOnlyList<CaptureCrossing> Crossings);
public sealed record CaptureWorldFeature(string FeatureId, string Semantic, CaptureBinding Binding,
    CaptureShape Shape, IReadOnlyList<Geometry.WorldPoint> Points, CaptureRoad? Road);
public sealed record CaptureGeometryDraft(string Status, CaptureSource Source,
    IReadOnlyList<CaptureWorldFeature> Features, IReadOnlyList<string> RemainingGates);
public sealed record CaptureVegetationDraft(string Status, CaptureSource Source, string PlacementPolicy,
    IReadOnlyList<PlannedVegetation> Plants, IReadOnlyList<string> RemainingGates);

public static class MockupCaptureJson
{
    public static JsonNode Schema()
    {
        var schema = Options().GetJsonSchemaAsNode(typeof(MockupCaptureDocument));
        schema["type"] = "object";
        schema["properties"]!["schemaVersion"]!["const"] = 1;
        schema["$schema"] = "https://json-schema.org/draft/2020-12/schema";
        schema["title"] = "GameFrameWork mockup capture";
        schema["description"] = "Single current source-pixel capture contract. Runtime validation additionally enforces geometry, bounds, references, calibration and review gates. No lifecycle approval is conveyed.";
        return schema;
    }
    public static JsonSerializerOptions Options() => new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };
}
