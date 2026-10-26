using WorldCreator.Core;
using WorldCreator.Core.Cities;
using WorldCreator.Core.Geometry;
using WorldCreator.Core.Metadata;
using WorldCreator.Core.Planning;

namespace WorldCreator.Worlds;

public sealed partial class ProceduralCityGenerator
{
    private Dictionary<string,CityStructureType> ValidateInputs(ProceduralCityRequest q,ProceduralCityCatalog c,CancellationToken token)
    {
        bool Id(string? id)=>SettlementCatalogRegistry.ValidId(id);
        if(q is null || c is null || q.SchemaVersion!=1 || c.SchemaVersion!=1 || !Id(q.Id) || !Id(c.Id) ||
            !double.IsFinite(q.WidthMeters) || !double.IsFinite(q.HeightMeters) || q.WidthMeters is <100 or >30000 || q.HeightMeters is <100 or >30000 ||
            q.StreetProfile is null || q.Districts is null || q.Districts.Count is <1 or >32 ||
            new[]{q.TargetBlockWidthMeters,q.TargetBlockDepthMeters,q.MinimumBlockSpanMeters,q.TargetParcelFrontageMeters,q.SetbackMeters}.Any(v=>!double.IsFinite(v)) ||
            q.MinimumBlockSpanMeters<30 || q.TargetBlockWidthMeters<q.MinimumBlockSpanMeters || q.TargetBlockDepthMeters<q.MinimumBlockSpanMeters ||
            q.TargetParcelFrontageMeters<4 || q.SetbackMeters<1 || q.SetbackMeters>100 || q.Variation is <0 or >1000000 ||
            q.MaximumBlocks is <1 or >256 || q.MaximumParcels is <1 or >3000 || q.MaximumStructures is <1 or >5000 ||
            !Enum.IsDefined(q.Coverage) || q.RemainingScopes?.Count>128 || q.RemainingScopes?.Any(s=>string.IsNullOrWhiteSpace(s) || s.Length>2048)==true ||
            (q.Coverage==MetadataCoverage.PartialScope ? q.RemainingScopes is not {Count:>0}:q.RemainingScopes?.Count>0) ||
            c.Structures is null || c.Structures.Count is <1 or >1000)throw new ArgumentException("CITY_INPUT_INVALID");
        q.StreetProfile.Validate();
        void ParcelPolicy(CityParcelRules? p)
        {
            if(p is null)return;
            if(p.Frontages is null || p.Frontages.Count is <1 or >4 || p.Frontages.Any(f=>!Enum.IsDefined(f)) || p.Frontages.Distinct().Count()!=p.Frontages.Count ||
                p.MaximumDepthMeters is {} depth && (!double.IsFinite(depth) || depth<=0 || depth>30000) || !double.IsFinite(p.InteriorAccessWidthMeters) ||
                p.InteriorAccessWidthMeters is <1 or >20 || string.IsNullOrWhiteSpace(p.InteriorPurpose) || p.InteriorPurpose.Length>2048)
                throw new ArgumentException("CITY_PARCEL_POLICY_INVALID");
        }
        ParcelPolicy(q.ParcelRules);
        if(q.StreetProfile.TotalWidthMeters>q.MinimumBlockSpanMeters/3)throw new ArgumentException("CITY_ROAD_TOO_WIDE");
        var types=new Dictionary<string,CityStructureType>(StringComparer.Ordinal);int totalParts=0;
        foreach(var t in c.Structures)
        {
            token.ThrowIfCancellationRequested();
            if(t is null || !Id(t.Id) || !Id(t.Kind) || !types.TryAdd(t.Id,t) || string.IsNullOrWhiteSpace(t.Purpose) || t.Purpose.Length>2048 ||
                t.Parts is null || t.Parts.Count>64 || t.IsOpenSpace && t.Parts.Count>0 ||
                (t.CatalogReference is null ? !ValidRectangle(t.Footprint) || t.Footprint!.X!=0 || t.Footprint.Y!=0 || t.Footprint.Width>1000 || t.Footprint.Height>1000 : t.Footprint is not null))
                throw new ArgumentException("CITY_STRUCTURE_TYPE_INVALID");
            if(t.CatalogReference is {} reference && (reference.Catalog is null || !Id(reference.Catalog.CatalogId) || reference.Catalog.EnabledCapabilities is null || !Id(reference.DefinitionId) || !Id(reference.VariantId)))throw new ArgumentException("CITY_STRUCTURE_REFERENCE_INVALID");
            var ids=new HashSet<string>();
            foreach(var p in t.Parts)
                if(p is null || !Id(p.Id) || !ids.Add(p.Id) || !Id(p.StructureId) || p.Count is <1 or >128 || string.IsNullOrWhiteSpace(p.Purpose) || p.Purpose.Length>2048)throw new ArgumentException("CITY_STRUCTURE_PART_INVALID");
            totalParts+=t.Parts.Sum(p=>p.Count);if(totalParts>10000)throw new ArgumentException("CITY_CATALOG_PART_BUDGET");
        }
        var districtIds=new HashSet<string>(StringComparer.Ordinal);
        foreach(var d in q.Districts)
        {
            if(d is null || !Id(d.Id) || !districtIds.Add(d.Id) || string.IsNullOrWhiteSpace(d.Purpose) || d.Purpose.Length>2048 ||
                !double.IsFinite(d.AreaWeight) || d.AreaWeight is <0.1 or >100 || !Id(d.LayoutId) || !layouts.ContainsKey(d.LayoutId) ||
                d.StructureCycle is null || d.StructureCycle.Count is <1 or >128 || d.StructureCycle.Any(id=>id is null || !types.ContainsKey(id)) ||
                d.OpenSpaceEveryBlocks is <0 or >256)throw new ArgumentException("CITY_DISTRICT_RULE_INVALID");
            ParcelPolicy(d.ParcelRules);
        }
        var active=new HashSet<string>();var done=new HashSet<string>();
        foreach(var id in types.Keys)Visit(id,0);
        void Visit(string id,int depth)
        {
            token.ThrowIfCancellationRequested();
            if(!types.ContainsKey(id))throw new ArgumentException("CITY_PART_TYPE_UNKNOWN");
            if(done.Contains(id))return;
            if(depth>16 || !active.Add(id))throw new ArgumentException("CITY_CATALOG_CYCLE_OR_DEPTH");
            foreach(var part in types[id].Parts)Visit(part.StructureId,depth+1);
            active.Remove(id);done.Add(id);
        }
        return types;
    }

    public IReadOnlyList<string> Validate(ProceduralCityDocument city,CancellationToken token=default)
    {
        var issues=new List<string>();var q=city.Request;
        var domain=new CityRectangle(0,0,q.WidthMeters,q.HeightMeters);
        var roads=city.Streets.ToDictionary(r=>r.Id,StringComparer.Ordinal);
        var parcels=city.Parcels.ToDictionary(p=>p.Id,StringComparer.Ordinal);
        var blocks=city.Blocks.ToDictionary(b=>b.Id,StringComparer.Ordinal);
        var structures=city.Structures.ToDictionary(s=>s.Id,StringComparer.Ordinal);
        foreach(var road in city.Streets)
        {
            token.ThrowIfCancellationRequested();road.Centerline.Validate();road.Profile.Validate();
            var bounds=road.Bounds();
            if(!domain.Contains(new(bounds.Min.X,bounds.Min.Y,bounds.Max.X-bounds.Min.X,bounds.Max.Y-bounds.Min.Y)))issues.Add("ROAD_OUTSIDE_CITY:"+road.Id);
        }
        foreach(var p in city.Parcels)
        {
            token.ThrowIfCancellationRequested();
            if(!blocks.TryGetValue(p.BlockId,out var block) || !block.Bounds.Contains(p.Bounds))issues.Add("PARCEL_OUTSIDE_BLOCK:"+p.Id);
            if(!roads.TryGetValue(p.FrontageRoadId,out var road) || !OnRoad(p.RoadAccess,road))issues.Add("PARCEL_FRONTAGE_MISSING:"+p.Id);
            if(p.Frontage is {} side && block is not null)
            {
                var b=block.Frontages.Bounds;
                var expectedRoad=side switch{CityFrontage.North=>block.Frontages.NorthRoadId,CityFrontage.South=>block.Frontages.SouthRoadId,CityFrontage.East=>block.Frontages.EastRoadId,_=>block.Frontages.WestRoadId};
                var expectedGate=side switch{CityFrontage.North=>new WorldPoint(p.Bounds.Center.X,p.Bounds.Y),CityFrontage.South=>new WorldPoint(p.Bounds.Center.X,p.Bounds.Bottom),CityFrontage.East=>new WorldPoint(p.Bounds.Right,p.Bounds.Center.Y),_=>new WorldPoint(p.Bounds.X,p.Bounds.Center.Y)};
                var expectedAccess=side switch{CityFrontage.North=>new WorldPoint(expectedGate.X,b.Y),CityFrontage.South=>new WorldPoint(expectedGate.X,b.Bottom),CityFrontage.East=>new WorldPoint(b.Right,expectedGate.Y),_=>new WorldPoint(b.X,expectedGate.Y)};
                if(!Enum.IsDefined(side) || expectedRoad!=p.FrontageRoadId || expectedGate.DistanceTo(p.FrontageAccess)>1e-7 || expectedAccess.DistanceTo(p.RoadAccess)>1e-7)
                    issues.Add("PARCEL_FRONTAGE_ORIENTATION_INVALID:"+p.Id);
            }
            if(!city.StreetGraph.Nodes.Any(n=>n.Location.DistanceTo(p.RoadAccess)<1e-6))issues.Add("PARCEL_ACCESS_GRAPH_MISSING:"+p.Id);
            if(city.Structures.Count(s=>s.ParcelId==p.Id && s.ParentId==p.Id)!=1)issues.Add("PARCEL_ROOT_STRUCTURE_MISSING:"+p.Id);
        }
        foreach(var block in city.Blocks)
        {
            var values=city.Parcels.Where(p=>p.BlockId==block.Id).Select(p=>p.Bounds).Concat(city.OpenSpaces.Where(s=>s.BlockId==block.Id).Select(s=>s.Bounds)).ToArray();
            if(values.Any(b=>!ValidRectangle(b) || !block.Bounds.Contains(b)))issues.Add("BLOCK_LAND_USE_OUTSIDE:"+block.Id);
            for(int i=0;i<values.Length;i++)for(int j=0;j<i;j++)if(values[i].Overlaps(values[j]))issues.Add("BLOCK_LAND_USE_OVERLAP:"+block.Id);
            double covered=values.Sum(b=>b.Width*b.Height);
            if(Math.Abs(covered-block.Bounds.Width*block.Bounds.Height)>1e-5)issues.Add("BLOCK_LAND_USE_INCOMPLETE:"+block.Id);
        }
        for(int i=0;i<city.Structures.Count;i++)
        {
            token.ThrowIfCancellationRequested();var s=city.Structures[i];
            if(!parcels.TryGetValue(s.ParcelId,out var parcel) || !parcel.Bounds.Contains(s.Bounds))issues.Add("STRUCTURE_OUTSIDE_PARCEL:"+s.Id);
            if(s.ParentId!=s.ParcelId && (!structures.TryGetValue(s.ParentId,out var parent) || !parent.IsCompound || !parent.Bounds.Contains(s.Bounds)))issues.Add("COMPOUND_OWNERSHIP_INVALID:"+s.Id);
            if(s.RotationDegrees is not (0 or 90 or 180 or 270) || parcel?.Frontage is {} facing && s.RotationDegrees!=(int)facing*90)issues.Add("STRUCTURE_ROTATION_INVALID:"+s.Id);
            s.Access.Validate();
            if(!s.Bounds.Polygon.ContainsPoint(s.Access.Points[^1]))issues.Add("STRUCTURE_ENTRANCE_MISSING:"+s.Id);
            if(s.ParentId==s.ParcelId && (parcel is null || s.Access.Points[0].DistanceTo(parcel.RoadAccess)>1e-7))issues.Add("STRUCTURE_FRONTAGE_ACCESS_MISSING:"+s.Id);
            if(s.IsCompound)continue;
            for(int j=0;j<i;j++)if(!city.Structures[j].IsCompound && s.Bounds.Overlaps(city.Structures[j].Bounds))issues.Add("STRUCTURE_OVERLAP:"+s.Id);
            foreach(var road in city.Streets)
            {
                var b=road.Bounds();if(s.Bounds.Overlaps(new(b.Min.X,b.Min.Y,b.Max.X-b.Min.X,b.Max.Y-b.Min.Y)))issues.Add("STRUCTURE_ROAD_OVERLAP:"+s.Id);
            }
        }
        var solids=city.Structures.Where(s=>!s.IsCompound && !s.IsOpenSpace).ToArray();
        foreach(var placement in city.Structures)
        {
            token.ThrowIfCancellationRequested();
            foreach(var solid in solids)
                for(int i=1;i<placement.Access.Points.Count;i++)
                    if(CrossesInterior(placement.Access.Points[i-1],placement.Access.Points[i],solid.Bounds))issues.Add("ACCESS_BLOCKED:"+placement.Id+":"+solid.Id);
        }
        foreach(var space in city.OpenSpaces)
        {
            token.ThrowIfCancellationRequested();
            if(!blocks.ContainsKey(space.BlockId))issues.Add("RESERVE_BLOCK_MISSING:"+space.Id);
            if(space.Kind is not ("park" or "courtyard" or "passage"))issues.Add("RESERVE_KIND_INVALID:"+space.Id);
            if(space.Kind=="park")continue;
            if(space.Access is not {Points.Count:>=2} path || space.FrontageRoadId is null || !roads.TryGetValue(space.FrontageRoadId,out var road) || !OnRoad(path.Points[0],road) ||
                !double.IsFinite(space.AccessWidthMeters) || space.AccessWidthMeters<1)
            {issues.Add("RESERVE_ACCESS_MISSING:"+space.Id);continue;}
            path.Validate();
            var roadBounds=road.Bounds();
            var reserved=city.OpenSpaces.Where(s=>s.BlockId==space.BlockId).Select(s=>s.Bounds).Append(new CityRectangle(roadBounds.Min.X,roadBounds.Min.Y,roadBounds.Max.X-roadBounds.Min.X,roadBounds.Max.Y-roadBounds.Min.Y)).ToArray();
            for(int i=1;i<path.Points.Count;i++)
            {
                var a=path.Points[i-1];var b=path.Points[i];double half=space.AccessWidthMeters/2;
                CityRectangle? sweep=Math.Abs(a.X-b.X)<1e-7?new(a.X-half,Math.Min(a.Y,b.Y),2*half,Math.Abs(a.Y-b.Y)):
                    Math.Abs(a.Y-b.Y)<1e-7?new(Math.Min(a.X,b.X),a.Y-half,Math.Abs(a.X-b.X),2*half):null;
                if(sweep is null || Math.Abs(reserved.Sum(r=>TerrainCoverageTopology.IntersectionArea(sweep.Polygon,r.Polygon))-sweep.Width*sweep.Height)>1e-5)
                    issues.Add("RESERVE_ACCESS_UNRESERVED:"+space.Id);
            }
            if(!city.StreetGraph.Nodes.Any(n=>n.Location.DistanceTo(path.Points[0])<1e-6))issues.Add("RESERVE_ACCESS_GRAPH_MISSING:"+space.Id);
            if(space.Kind=="courtyard" && !space.Bounds.Polygon.ContainsPoint(path.Points[^1]))issues.Add("RESERVE_ACCESS_DESTINATION_INVALID:"+space.Id);
            foreach(var solid in solids)
                for(int i=1;i<path.Points.Count;i++)
                    if(CrossesInterior(path.Points[i-1],path.Points[i],solid.Bounds,space.AccessWidthMeters/2))issues.Add("RESERVE_ACCESS_BLOCKED:"+space.Id);
        }
        foreach(var block in city.Blocks.Where(b=>b.IsOpenSpace))
            if(city.OpenSpaces.Count(s=>s.BlockId==block.Id && s.Bounds==block.Bounds)!=1 || city.Parcels.Any(p=>p.BlockId==block.Id))issues.Add("OPEN_SPACE_ALLOCATION_INVALID:"+block.Id);
        var rebuilt=StreetTopologyBuilder.Build(city.Streets,city.Parcels.Select(p=>p.RoadAccess).Concat(city.OpenSpaces.Where(s=>s.Access is not null).Select(s=>s.Access!.Points[0])).ToArray(),token);
        if(System.Text.Json.JsonSerializer.Serialize(rebuilt)!=System.Text.Json.JsonSerializer.Serialize(city.StreetGraph))issues.Add("STREET_GRAPH_STALE");
        if(city.StreetGraph.Diagnostics.Count>0 || !city.StreetGraph.IsConnected())issues.Add("STREET_GRAPH_DISCONNECTED");
        return issues;
        static bool OnRoad(WorldPoint point,StreetCorridor road)
        {
            for(int i=1;i<road.Centerline.Points.Count;i++)
            {
                var a=road.Centerline.Points[i-1];var b=road.Centerline.Points[i];
                if(Math.Abs(a.DistanceTo(point)+point.DistanceTo(b)-a.DistanceTo(b))<1e-7)return true;
            }
            return false;
        }
        static bool CrossesInterior(WorldPoint a,WorldPoint b,CityRectangle r,double halfWidth=0)
        {
            if(Math.Abs(a.X-b.X)<1e-7)return a.X+halfWidth>r.X+1e-7 && a.X-halfWidth<r.Right-1e-7 && Math.Min(Math.Max(a.Y,b.Y),r.Bottom)-Math.Max(Math.Min(a.Y,b.Y),r.Y)>1e-7;
            if(Math.Abs(a.Y-b.Y)<1e-7)return a.Y+halfWidth>r.Y+1e-7 && a.Y-halfWidth<r.Bottom-1e-7 && Math.Min(Math.Max(a.X,b.X),r.Right)-Math.Max(Math.Min(a.X,b.X),r.X)>1e-7;
            return true;
        }
    }
}
