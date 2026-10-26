using WorldCreator.Core.Cities;
using WorldCreator.Core.Geometry;

namespace WorldCreator.Worlds;

/// <summary>Partitions blocks into street-facing bands, frontage parcels and explicitly reserved interior/access space.</summary>
public sealed class CityFrontageParcelPlanner
{
    public CityParcelLayout Plan(CityBlock block,CityRectangle size,string typeId,string purpose,
        ProceduralCityRequest request,CityParcelRules? configured,CancellationToken token=default)
    {
        var rules=configured??new CityParcelRules([CityFrontage.North,CityFrontage.South]);
        var remainder=block.Bounds;var bands=new List<(CityFrontage Side,CityRectangle Bounds)>();
        int horizontal=rules.Frontages.Count(s=>s is CityFrontage.North or CityFrontage.South);
        int vertical=rules.Frontages.Count-horizontal;
        double need=size.Height+2*request.SetbackMeters;
        foreach(var side in rules.Frontages)
        {
            token.ThrowIfCancellationRequested();
            bool ns=side is CityFrontage.North or CityFrontage.South;
            int count=ns?horizontal:vertical;
            double span=ns?remainder.Height:remainder.Width;
            // Reserve enough room for later orthogonal frontages instead of consuming the entire block first.
            double natural=span/((ns?vertical:horizontal)>0?count+1:count);
            double depth=Math.Min(rules.MaximumDepthMeters??natural,natural);
            if(depth+1e-7<need)throw new ArgumentException("CITY_FRONTAGE_DEPTH_DOES_NOT_FIT:"+block.Id+":"+side);
            CityRectangle band;
            switch(side)
            {
                case CityFrontage.North:band=new(remainder.X,remainder.Y,remainder.Width,depth);remainder=new(remainder.X,remainder.Y+depth,remainder.Width,remainder.Height-depth);break;
                case CityFrontage.South:band=new(remainder.X,remainder.Bottom-depth,remainder.Width,depth);remainder=new(remainder.X,remainder.Y,remainder.Width,remainder.Height-depth);break;
                case CityFrontage.West:band=new(remainder.X,remainder.Y,depth,remainder.Height);remainder=new(remainder.X+depth,remainder.Y,remainder.Width-depth,remainder.Height);break;
                case CityFrontage.East:band=new(remainder.Right-depth,remainder.Y,depth,remainder.Height);remainder=new(remainder.X,remainder.Y,remainder.Width-depth,remainder.Height);break;
                default:throw new ArgumentException("CITY_FRONTAGE_INVALID");
            }
            bands.Add((side,band));if(ns)horizontal--;else vertical--;
        }
        var reserves=new List<CityOpenSpace>();var lots=new List<CityFrontageLot>();
        bool interior=remainder.Width>1e-7 && remainder.Height>1e-7;
        if(interior && (remainder.Width<rules.InteriorAccessWidthMeters || remainder.Height<rules.InteriorAccessWidthMeters))throw new ArgumentException("CITY_INTERIOR_CLEARANCE");
        WorldPolyline? innerAccess=null;string? innerRoad=null;
        for(int bandIndex=0;bandIndex<bands.Count;bandIndex++)
        {
            var (side,band)=bands[bandIndex];var slices=new List<CityRectangle>{band};
            if(interior && bandIndex==0)
            {
                double width=rules.InteriorAccessWidthMeters;
                CityRectangle lane;
                if(side is CityFrontage.North or CityFrontage.South)
                {
                    double x=remainder.Center.X;
                    lane=new(x-width/2,band.Y,width,band.Height);
                    slices=[new(band.X,band.Y,lane.X-band.X,band.Height),new(lane.Right,band.Y,band.Right-lane.Right,band.Height)];
                }
                else
                {
                    double y=remainder.Center.Y;
                    lane=new(band.X,y-width/2,band.Width,width);
                    slices=[new(band.X,band.Y,band.Width,lane.Y-band.Y),new(band.X,lane.Bottom,band.Width,band.Bottom-lane.Bottom)];
                }
                if(!band.Contains(lane) || slices.Any(s=>s.Width<=0 || s.Height<=0))throw new ArgumentException("CITY_INTERIOR_PASSAGE_DOES_NOT_FIT");
                var gate=side switch
                {
                    CityFrontage.North=>new WorldPoint(lane.Center.X,band.Y),CityFrontage.South=>new WorldPoint(lane.Center.X,band.Bottom),
                    CityFrontage.West=>new WorldPoint(band.X,lane.Center.Y),_=>new WorldPoint(band.Right,lane.Center.Y)
                };
                var road=RoadPoint(gate,side,block.Frontages.Bounds);innerRoad=RoadId(side,block.Frontages);
                innerAccess=new([road,gate,remainder.Center]);
                reserves.Add(new(block.Id+"-interior-passage",block.Id,lane,"Reserved connection to "+rules.InteriorPurpose,"passage",innerAccess,innerRoad,width));
            }
            for(int sliceIndex=0;sliceIndex<slices.Count;sliceIndex++)
            {
                token.ThrowIfCancellationRequested();var slice=slices[sliceIndex];var frame=new CityFrontageFrame(slice,side);
                var local=new CityRectangle(0,0,frame.Width,frame.Depth);
                double minimum=size.Width+2*request.SetbackMeters;
                var divided=FrontageParcelPlanner.Subdivide(new(block.Id+"-"+side.ToString().ToLowerInvariant()+"-"+sliceIndex,local.Polygon.Vertices,local.Width*local.Height),Math.Max(request.TargetParcelFrontageMeters,minimum),minimum);
                if(divided.Diagnostics.Count!=0)throw new ArgumentException("CITY_PARCEL_SUBDIVISION_FAILED:"+block.Id);
                foreach(var parcel in divided.Parcels)
                {
                    var envelope=BuildingEnvelopePlanner.Plan(parcel,new(typeId,request.SetbackMeters,request.SetbackMeters,request.SetbackMeters,size.Width,size.Height));
                    if(!envelope.IsFeasible)throw new ArgumentException("CITY_BUILDING_ENVELOPE_FAILED:"+block.Id);
                    var a=envelope.Footprint!.Vertices;
                    var body=new CityRectangle((a.Min(p=>p.X)+a.Max(p=>p.X)-size.Width)/2,a.Min(p=>p.Y),size.Width,size.Height);
                    var localGate=parcel.StreetAccess;var gate=frame.ToWorld(localGate);var road=RoadPoint(gate,side,block.Frontages.Bounds);
                    var localRoad=new WorldPoint(localGate.X,-request.StreetProfile.TotalWidthMeters/2);
                    var boundary=parcel.Boundary.Vertices;var localParcel=new CityRectangle(boundary.Min(p=>p.X),boundary.Min(p=>p.Y),boundary.Max(p=>p.X)-boundary.Min(p=>p.X),boundary.Max(p=>p.Y)-boundary.Min(p=>p.Y));
                    lots.Add(new(new(parcel.Id,block.Id,block.DistrictId,typeId,frame.ToWorld(localParcel),RoadId(side,block.Frontages),road,gate,purpose,side),frame,body,new([localRoad,localGate,new(body.Center.X,body.Y)])));
                }
            }
        }
        if(interior)reserves.Add(new(block.Id+"-interior",block.Id,remainder,rules.InteriorPurpose,"courtyard",innerAccess,innerRoad,rules.InteriorAccessWidthMeters));
        return new(lots,reserves);
    }
    private static string RoadId(CityFrontage side,CityRegion region)=>side switch{CityFrontage.North=>region.NorthRoadId,CityFrontage.East=>region.EastRoadId,CityFrontage.South=>region.SouthRoadId,CityFrontage.West=>region.WestRoadId,_=>throw new ArgumentException("CITY_FRONTAGE_INVALID")};
    private static WorldPoint RoadPoint(WorldPoint gate,CityFrontage side,CityRectangle bounds)=>side switch{CityFrontage.North=>new(gate.X,bounds.Y),CityFrontage.East=>new(bounds.Right,gate.Y),CityFrontage.South=>new(gate.X,bounds.Bottom),CityFrontage.West=>new(bounds.X,gate.Y),_=>throw new ArgumentException("CITY_FRONTAGE_INVALID")};
}
