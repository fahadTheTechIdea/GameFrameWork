using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using WorldCreator.Core;
using WorldCreator.Core.Cities;
using WorldCreator.Core.Geometry;
using WorldCreator.Core.Metadata;
using WorldCreator.Core.Planning;

namespace WorldCreator.Worlds;

/// <summary>Rule-driven, headless city generation. Hosts supply layout strategies; no mockup or renderer is required.</summary>
public sealed partial class ProceduralCityGenerator
{
    private readonly IReadOnlyDictionary<string,ICityStreetLayout> layouts;
    private readonly ISettlementCatalogRegistry? sourceCatalogs;
    public ProceduralCityGenerator(IEnumerable<ICityStreetLayout> layouts,ISettlementCatalogRegistry? sourceCatalogs=null)
    {
        this.layouts=layouts.ToDictionary(x=>x.Id,StringComparer.Ordinal);
        if(this.layouts.Count==0 || this.layouts.Keys.Any(k=>!SettlementCatalogRegistry.ValidId(k)))throw new ArgumentException("CITY_LAYOUT_REGISTRY_INVALID");
        this.sourceCatalogs=sourceCatalogs;
    }
    public ProceduralCityDocument Generate(ProceduralCityRequest request,ProceduralCityCatalog catalog,CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested();
        var types=ValidateInputs(request,catalog,token);
        var sizes=new Dictionary<string,CityRectangle>(StringComparer.Ordinal);
        var facts=new Dictionary<string,MetadataStructureDefinition>(StringComparer.Ordinal);
        foreach(var type in types.Values)
        {
            if(type.CatalogReference is {} reference)
            {
                if(sourceCatalogs is null)throw new ArgumentException("CITY_SOURCE_CATALOG_REQUIRED");
                var pack=sourceCatalogs.Resolve(reference.Catalog);
                var definition=pack.Structures.SingleOrDefault(d=>d.Id==reference.DefinitionId)??throw new ArgumentException("CITY_SOURCE_STRUCTURE_UNKNOWN");
                var variant=definition.SizeVariants?.SingleOrDefault(v=>v.Id==reference.VariantId)??throw new ArgumentException("CITY_SOURCE_VARIANT_UNKNOWN");
                sizes.Add(type.Id,new(0,0,variant.WidthMeters,variant.DepthMeters));facts.Add(type.Id,new(pack.Provenance,definition,variant));
            }
            else sizes.Add(type.Id,type.Footprint!);
        }
        var streets=new List<StreetCorridor>();var districts=new List<CityDistrict>();var blocks=new List<CityBlock>();
        var parcels=new List<CityParcel>();var structures=new List<CityStructurePlacement>();var openSpaces=new List<CityOpenSpace>();
        double half=request.StreetProfile.TotalWidthMeters/2;
        var domain=new CityRectangle(half,half,request.WidthMeters-2*half,request.HeightMeters-2*half);
        var root=new CityRegion(request.Id,domain,"city-north","city-east","city-south","city-west");
        Road(root.NorthRoadId,new(domain.X,domain.Y),new(domain.Right,domain.Y));
        Road(root.EastRoadId,new(domain.Right,domain.Y),new(domain.Right,domain.Bottom));
        Road(root.SouthRoadId,new(domain.X,domain.Bottom),new(domain.Right,domain.Bottom));
        Road(root.WestRoadId,new(domain.X,domain.Y),new(domain.X,domain.Bottom));
        int districtRoad=0;
        PartitionDistricts(root,request.Districts.ToArray());
        var terminals=parcels.Select(p=>p.RoadAccess).Concat(openSpaces.Where(s=>s.Access is not null).Select(s=>s.Access!.Points[0])).ToArray();
        var topology=StreetTopologyBuilder.Build(streets,terminals,token);
        if(topology.Diagnostics.Count!=0 || !topology.IsConnected())throw new ArgumentException("CITY_STREET_TOPOLOGY_INVALID:"+string.Join(',',topology.Diagnostics));
        var result=new ProceduralCityDocument(1,request.Id,request,Digest(request),Digest(catalog),districts.AsReadOnly(),streets.AsReadOnly(),topology,
            blocks.AsReadOnly(),parcels.AsReadOnly(),structures.AsReadOnly(),openSpaces.AsReadOnly(),new ReadOnlyDictionary<string,MetadataStructureDefinition>(facts));
        var issues=Validate(result,token);
        if(issues.Count!=0)throw new ArgumentException("CITY_RESULT_INVALID:"+string.Join(',',issues.Take(8)));
        return result;

        void Road(string id,WorldPoint a,WorldPoint b)=>streets.Add(new(id,new([a,b]),request.StreetProfile));
        void PartitionDistricts(CityRegion region,CityDistrictRule[] rules)
        {
            token.ThrowIfCancellationRequested();
            if(rules.Length==1){District(region with{Id=rules[0].Id},rules[0]);return;}
            int middle=rules.Length/2;double ratio=rules.Take(middle).Sum(r=>r.AreaWeight)/rules.Sum(r=>r.AreaWeight);
            var b=region.Bounds;string id="district-route-"+districtRoad++;
            if(b.Width>=b.Height)
            {
                double cut=b.X+b.Width*ratio;Road(id,new(cut,b.Y),new(cut,b.Bottom));
                PartitionDistricts(region with{Bounds=new(b.X,b.Y,cut-b.X,b.Height),EastRoadId=id},rules[..middle]);
                PartitionDistricts(region with{Bounds=new(cut,b.Y,b.Right-cut,b.Height),WestRoadId=id},rules[middle..]);
            }
            else
            {
                double cut=b.Y+b.Height*ratio;Road(id,new(b.X,cut),new(b.Right,cut));
                PartitionDistricts(region with{Bounds=new(b.X,b.Y,b.Width,cut-b.Y),SouthRoadId=id},rules[..middle]);
                PartitionDistricts(region with{Bounds=new(b.X,cut,b.Width,b.Bottom-cut),NorthRoadId=id},rules[middle..]);
            }
        }
        void District(CityRegion region,CityDistrictRule rule)
        {
            if(region.Bounds.Width<request.MinimumBlockSpanMeters || region.Bounds.Height<request.MinimumBlockSpanMeters)throw new ArgumentException("CITY_DISTRICT_TOO_SMALL:"+rule.Id);
            districts.Add(new(rule.Id,rule.Purpose,rule.LayoutId,region.Bounds));
            var layout=layouts[rule.LayoutId].Generate(new(region,request,request.MaximumBlocks-blocks.Count),token);
            if(layout.Blocks is null || layout.Streets is null || layout.Streets.Count>4096 || layout.Blocks.Count==0 || layout.Blocks.Count>request.MaximumBlocks-blocks.Count)
                throw new ArgumentException("CITY_LAYOUT_RESULT_INVALID");
            double sum=0;
            for(int i=0;i<layout.Blocks.Count;i++)
            {
                var b=layout.Blocks[i];
                if(b is null || !ValidRectangle(b.Bounds) || b.Bounds.Width<request.MinimumBlockSpanMeters || b.Bounds.Height<request.MinimumBlockSpanMeters || !region.Bounds.Contains(b.Bounds) || layout.Blocks.Take(i).Any(p=>p.Bounds.Overlaps(b.Bounds)))throw new ArgumentException("CITY_LAYOUT_PARTITION_INVALID");
                sum+=b.Bounds.Width*b.Bounds.Height;
            }
            if(Math.Abs(sum-region.Bounds.Width*region.Bounds.Height)>1e-6*Math.Max(1,sum))throw new ArgumentException("CITY_LAYOUT_PARTITION_INCOMPLETE");
            streets.AddRange(layout.Streets);
            for(int index=0;index<layout.Blocks.Count;index++)
            {
                token.ThrowIfCancellationRequested();var seed=layout.Blocks[index];var inside=seed.Bounds.Inset(half);
                if(!ValidRectangle(inside))throw new ArgumentException("CITY_BLOCK_HAS_NO_INTERIOR");
                bool green=rule.OpenSpaceEveryBlocks>0 && (index+1)%rule.OpenSpaceEveryBlocks==0;
                blocks.Add(new(seed.Id,rule.Id,inside,seed,green));
                if(green){openSpaces.Add(new(seed.Id+"-green",seed.Id,inside,rule.Purpose+": planned public open space"));continue;}
                var type=types[rule.StructureCycle[(index+request.Variation)%rule.StructureCycle.Count]];
                var parcelPlan=new CityFrontageParcelPlanner().Plan(blocks[^1],sizes[type.Id],type.Id,rule.Purpose+": "+type.Purpose,request,rule.ParcelRules??request.ParcelRules,token);
                openSpaces.AddRange(parcelPlan.Reserves);
                foreach(var lot in parcelPlan.Lots)
                {
                    if(parcels.Count>=request.MaximumParcels)throw new ArgumentException("CITY_PARCEL_BUDGET");
                    parcels.Add(lot.Parcel);
                    Place(type,lot.Parcel.Id+"-structure",lot.Parcel.Id,lot.Parcel.Id,lot.LocalFootprint,lot.LocalAccess,lot.Parcel.Purpose,0,lot.Frame);
                }
            }
        }
        void Place(CityStructureType type,string id,string parent,string parcelId,CityRectangle bounds,WorldPolyline access,string purpose,int depth,CityFrontageFrame frame)
        {
            token.ThrowIfCancellationRequested();
            if(depth>16 || structures.Count>=request.MaximumStructures)throw new ArgumentException("CITY_STRUCTURE_BUDGET");
            if(facts.TryGetValue(type.Id,out var fact) && request.StreetProfile.LaneWidthMeters*request.StreetProfile.LaneCount+1e-7<Math.Max(fact.Definition.MinimumRoadWidthMeters,fact.Variant.MinimumRoadWidthMeters))
                throw new ArgumentException("CITY_SOURCE_ROAD_UNDERSIZED:"+type.Id);
            structures.Add(new(id,type.Id,type.Kind,parent,parcelId,frame.ToWorld(bounds),type.Parts.Count>0,type.IsOpenSpace,purpose,"frontage-and-compound-grammar",new(access.Points.Select(frame.ToWorld).ToArray()),frame.RotationDegrees));
            var members=type.Parts.SelectMany(p=>Enumerable.Range(0,p.Count).Select(i=>(Part:p,Index:i,Type:types[p.StructureId]))).ToArray();
            if(members.Length==0)return;
            const double padding=4,gap=4;
            double width=members.Max(p=>sizes[p.Type.Id].Width),height=members.Max(p=>sizes[p.Type.Id].Height);
            int columns=(int)Math.Floor((bounds.Width-2*padding+gap)/(width+gap));
            int rows=columns<=0?int.MaxValue:(int)Math.Ceiling((double)members.Length/columns);
            if(columns<=0 || rows*height+(rows-1)*gap>bounds.Height-2*padding+1e-7)throw new ArgumentException("CITY_COMPOUND_PARTS_DO_NOT_FIT:"+type.Id);
            var entrance=access.Points[^1];double laneX=bounds.X+padding/2;
            double gateY=Math.Abs(entrance.Y-bounds.Bottom)<1e-7?bounds.Bottom-padding/2:bounds.Y+padding/2;
            for(int i=0;i<members.Length;i++)
            {
                var member=members[i];var size=sizes[member.Type.Id];
                var b=new CityRectangle(bounds.X+padding+(i%columns)*(width+gap),bounds.Y+padding+(i/columns)*(height+gap),size.Width,size.Height);
                var path=new[]{entrance,new WorldPoint(entrance.X,gateY),new WorldPoint(laneX,gateY),
                    new WorldPoint(laneX,b.Y-gap/2),new WorldPoint(b.Center.X,b.Y-gap/2),new WorldPoint(b.Center.X,b.Y)}.Distinct().ToArray();
                Place(member.Type,id+"/"+member.Part.Id+"-"+member.Index,id,parcelId,b,new(path),member.Part.Purpose,depth+1,frame);
            }
        }
    }
    private static CityRectangle Bounds(WorldPolygon p)=>new(p.Vertices.Min(v=>v.X),p.Vertices.Min(v=>v.Y),p.Vertices.Max(v=>v.X)-p.Vertices.Min(v=>v.X),p.Vertices.Max(v=>v.Y)-p.Vertices.Min(v=>v.Y));
    private static bool ValidRectangle(CityRectangle? b)=>b is not null && new[]{b.X,b.Y,b.Width,b.Height,b.Right,b.Bottom}.All(double.IsFinite) && b.Width>0 && b.Height>0;
    private static string Digest<T>(T value)=>Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value,WorldJson.CreateOptions())));
}
