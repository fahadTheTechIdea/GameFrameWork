using WorldCreator.Core.Geometry;
using WorldCreator.Core.Metadata;
using System.Text.Json.Serialization;

namespace WorldCreator.Core.Cities;

public sealed record CityRectangle(double X, double Y, double Width, double Height)
{
    [JsonIgnore] public double Right => X + Width;
    [JsonIgnore] public double Bottom => Y + Height;
    [JsonIgnore] public WorldPoint Center => new(X + Width / 2, Y + Height / 2);
    [JsonIgnore] public WorldPolygon Polygon => new([new(X,Y),new(Right,Y),new(Right,Bottom),new(X,Bottom)]);
    public CityRectangle Inset(double amount) => new(X+amount,Y+amount,Width-2*amount,Height-2*amount);
    public bool Contains(CityRectangle other) => other.X>=X-1e-7 && other.Y>=Y-1e-7 && other.Right<=Right+1e-7 && other.Bottom<=Bottom+1e-7;
    public bool Overlaps(CityRectangle other) => Math.Min(Right,other.Right)-Math.Max(X,other.X)>1e-7 && Math.Min(Bottom,other.Bottom)-Math.Max(Y,other.Y)>1e-7;
}
public sealed record CityStructurePart(string Id,string StructureId,int Count,string Purpose);
public sealed record CityStructureType(string Id,string Kind,string Purpose,CityRectangle? Footprint,
    IReadOnlyList<CityStructurePart> Parts,bool IsOpenSpace=false,MetadataStructureReference? CatalogReference=null);
public sealed record ProceduralCityCatalog(int SchemaVersion,string Id,IReadOnlyList<CityStructureType> Structures);
public enum CityFrontage { North, East, South, West }
public sealed record CityParcelRules(IReadOnlyList<CityFrontage> Frontages,double? MaximumDepthMeters=null,
    double InteriorAccessWidthMeters=3,string InteriorPurpose="Shared block interior");
public sealed record CityDistrictRule(string Id,string Purpose,double AreaWeight,string LayoutId,
    IReadOnlyList<string> StructureCycle,int OpenSpaceEveryBlocks=0,CityParcelRules? ParcelRules=null);
public sealed record ProceduralCityRequest(int SchemaVersion,string Id,double WidthMeters,double HeightMeters,
    RoadCrossSection StreetProfile,double TargetBlockWidthMeters,double TargetBlockDepthMeters,
    double MinimumBlockSpanMeters,double TargetParcelFrontageMeters,double SetbackMeters,
    IReadOnlyList<CityDistrictRule> Districts,int Variation=0,int MaximumBlocks=256,int MaximumParcels=3000,
    int MaximumStructures=5000,MetadataCoverage Coverage=MetadataCoverage.CompleteDeclaredScope,IReadOnlyList<string>? RemainingScopes=null,
    CityParcelRules? ParcelRules=null);
public sealed record CityRegion(string Id,CityRectangle Bounds,string NorthRoadId,string EastRoadId,string SouthRoadId,string WestRoadId);
public sealed record CityLayoutContext(CityRegion Region,ProceduralCityRequest Request,int BlockBudget);
public sealed record CityLayoutResult(IReadOnlyList<CityRegion> Blocks,IReadOnlyList<StreetCorridor> Streets);
public interface ICityStreetLayout
{
    string Id { get; }
    CityLayoutResult Generate(CityLayoutContext context,CancellationToken token=default);
}
public sealed record CityDistrict(string Id,string Purpose,string LayoutId,CityRectangle Bounds);
public sealed record CityBlock(string Id,string DistrictId,CityRectangle Bounds,CityRegion Frontages,bool IsOpenSpace);
public sealed record CityParcel(string Id,string BlockId,string DistrictId,string StructureTypeId,CityRectangle Bounds,
    string FrontageRoadId,WorldPoint RoadAccess,WorldPoint FrontageAccess,string Purpose,CityFrontage? Frontage=null);
public sealed record CityStructurePlacement(string Id,string TypeId,string Kind,string ParentId,string ParcelId,
    CityRectangle Bounds,bool IsCompound,bool IsOpenSpace,string Purpose,string PlanningRule,WorldPolyline Access,int RotationDegrees=0);
public sealed record CityOpenSpace(string Id,string BlockId,CityRectangle Bounds,string Purpose,string Kind="park",
    WorldPolyline? Access=null,string? FrontageRoadId=null,double AccessWidthMeters=0);
public sealed record CityFrontageFrame(CityRectangle Band,CityFrontage Side)
{
    [JsonIgnore] public double Width => Side is CityFrontage.North or CityFrontage.South?Band.Width:Band.Height;
    [JsonIgnore] public double Depth => Side is CityFrontage.North or CityFrontage.South?Band.Height:Band.Width;
    [JsonIgnore] public int RotationDegrees => (int)Side*90;
    public WorldPoint ToWorld(WorldPoint p)=>Side switch
    {
        CityFrontage.North=>new(Band.X+p.X,Band.Y+p.Y),
        CityFrontage.East=>new(Band.Right-p.Y,Band.Y+p.X),
        CityFrontage.South=>new(Band.Right-p.X,Band.Bottom-p.Y),
        CityFrontage.West=>new(Band.X+p.Y,Band.Bottom-p.X),
        _=>throw new ArgumentException("CITY_FRONTAGE_INVALID")
    };
    public CityRectangle ToWorld(CityRectangle b)
    {
        var a=ToWorld(new WorldPoint(b.X,b.Y));var z=ToWorld(new WorldPoint(b.Right,b.Bottom));
        bool quarter=Side is CityFrontage.East or CityFrontage.West;
        return new(Math.Min(a.X,z.X),Math.Min(a.Y,z.Y),quarter?b.Height:b.Width,quarter?b.Width:b.Height);
    }
}
public sealed record CityFrontageLot(CityParcel Parcel,CityFrontageFrame Frame,CityRectangle LocalFootprint,WorldPolyline LocalAccess);
public sealed record CityParcelLayout(IReadOnlyList<CityFrontageLot> Lots,IReadOnlyList<CityOpenSpace> Reserves);
public sealed record ProceduralCityDocument(int SchemaVersion,string Id,ProceduralCityRequest Request,string RequestSha256,
    string CatalogSha256,IReadOnlyList<CityDistrict> Districts,IReadOnlyList<StreetCorridor> Streets,StreetTopology StreetGraph,
    IReadOnlyList<CityBlock> Blocks,IReadOnlyList<CityParcel> Parcels,IReadOnlyList<CityStructurePlacement> Structures,IReadOnlyList<CityOpenSpace> OpenSpaces,
    IReadOnlyDictionary<string,MetadataStructureDefinition> SourceStructures);
public sealed record CityPackageFile(string Sha256,long Length);
public sealed record CityPackageManifest(int SchemaVersion,string CityId,string RequestSha256,string CatalogSha256,
    IReadOnlyDictionary<string,string> EngineBuilds,IReadOnlyDictionary<string,CityPackageFile> Files);
public sealed record CityPackageVerification(IReadOnlyList<string> Issues)
{
    [JsonIgnore] public bool Valid=>Issues.Count==0;
}
