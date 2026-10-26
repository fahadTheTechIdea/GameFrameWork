using System.Text.Json;
using WorldCreator.Core;
using WorldCreator.Core.Cities;
using WorldCreator.Core.Geometry;
using WorldCreator.Core.Metadata;

namespace WorldCreator.Worlds;

public sealed partial class ProceduralCityGenerator
{
    public WorldMetadataDocument CreateMetadata(ProceduralCityDocument city,CancellationToken token=default)
    {
        var nodes=new List<WorldMetadataNode>();var boxes=new List<MetadataMapBox>();var links=new List<WorldMetadataConnection>();
        void Node(string id,string kind,string? parent,string purpose,CityRectangle bounds,string color,bool hideLabel=false,string? typeId=null,bool footprint=false,string? label=null,int rotation=0)
        {
            token.ThrowIfCancellationRequested();
            var properties=JsonSerializer.Deserialize<Dictionary<string,JsonElement>>(JsonSerializer.Serialize(new{schematic=new{label=label??typeId??kind,color,hideLabel,footprint},planning_rule="engine-procedural-city",type_id=typeId,rotation_degrees=rotation} ))!;
            nodes.Add(new(id,typeId??kind,kind,parent,purpose,properties,[bounds.Center.X,bounds.Center.Y],typeId is not null && city.SourceStructures.ContainsKey(typeId)?typeId:null));
            boxes.Add(new(id,bounds.X,bounds.Y,bounds.Width,bounds.Height));
        }
        Node(city.Id,"city",null,"Procedurally generated city",new(0,0,city.Request.WidthMeters,city.Request.HeightMeters),"#e2ede4",true);
        string District(string id)=>city.Id+"/district/"+id;
        string Block(string id)=>city.Id+"/block/"+id;
        string Parcel(string id)=>city.Id+"/parcel/"+id;
        string Structure(string id)=>city.Id+"/structure/"+id;
        string Junction(string id)=>city.Id+"/junction/"+id;
        string[] colors=["#d8e6d8","#dde4ed","#ebdfca","#e1d7ce","#d7e4cb","#e1dff0"];
        int colorIndex=0;
        foreach(var d in city.Districts)Node(District(d.Id),"district",city.Id,d.Purpose,d.Bounds,colors[colorIndex++%colors.Length],label:d.Id.Replace('-',' '));
        foreach(var b in city.Blocks)Node(Block(b.Id),"block",District(b.DistrictId),"Street-bounded development block",b.Bounds,"#e9eee6",true);
        foreach(var p in city.Parcels)Node(Parcel(p.Id),"parcel",Block(p.BlockId),p.Purpose,p.Bounds,"#edf1e8",true);
        var placementIds=city.Structures.Select(s=>s.Id).ToHashSet(StringComparer.Ordinal);
        foreach(var s in city.Structures)
            Node(Structure(s.Id),s.Kind,placementIds.Contains(s.ParentId)?Structure(s.ParentId):Parcel(s.ParcelId),s.Purpose,s.Bounds,
                s.IsOpenSpace?"#b7d39d":s.IsCompound?"#ded4bd":"#adc4ce",s.IsCompound,s.TypeId,rotation:s.RotationDegrees);
        foreach(var space in city.OpenSpaces)Node(city.Id+"/green/"+space.Id,space.Kind,Block(space.BlockId),space.Purpose,space.Bounds,space.Kind=="park"?"#b7d39d":space.Kind=="passage"?"#d0d3ca":"#dbe3c8",space.Kind=="passage");
        foreach(var junction in city.StreetGraph.Nodes)
            Node(Junction(junction.Id),"junction",city.Id,"Connected street junction or frontage terminal",new(junction.Location.X-0.5,junction.Location.Y-0.5,1,1),"#76878b",true);
        foreach(var edge in city.StreetGraph.Edges)
            links.Add(new(city.Id+"/road-edge/"+edge.Id,"street",Junction(edge.FromId),Junction(edge.ToId),true,edge.CorridorId,new Dictionary<string,JsonElement>()));
        foreach(var p in city.Parcels)
        {
            var terminal=city.StreetGraph.Nodes.Single(n=>n.Location.DistanceTo(p.RoadAccess)<1e-6);
            links.Add(new(city.Id+"/access/"+p.Id,"frontage-access",Junction(terminal.Id),Parcel(p.Id),true,p.FrontageRoadId,new Dictionary<string,JsonElement>()));
        }
        foreach(var space in city.OpenSpaces.Where(s=>s.Access is not null))
        {
            var terminal=city.StreetGraph.Nodes.Single(n=>n.Location.DistanceTo(space.Access!.Points[0])<1e-6);
            links.Add(new(city.Id+"/reserve-access/"+space.Id,"interior-access",Junction(terminal.Id),city.Id+"/green/"+space.Id,true,space.FrontageRoadId!,new Dictionary<string,JsonElement>()));
        }
        var segments=city.Streets.SelectMany(r=>r.Centerline.Points.Zip(r.Centerline.Points.Skip(1),(a,b)=>
            new MetadataMapRoad(r.Id,a.X,a.Y,b.X,b.Y,r.Profile.TotalWidthMeters))).ToList();
        foreach(var s in city.Structures)
            segments.AddRange(s.Access.Points.Zip(s.Access.Points.Skip(1),(a,b)=>new MetadataMapRoad("access:"+s.Id,a.X,a.Y,b.X,b.Y,1.5)));
        foreach(var space in city.OpenSpaces.Where(s=>s.Access is not null))
            segments.AddRange(space.Access!.Points.Zip(space.Access.Points.Skip(1),(a,b)=>new MetadataMapRoad("reserve:"+space.Id,a.X,a.Y,b.X,b.Y,space.AccessWidthMeters)));
        return new(1,city.Id,"procedural-city","city",city.Request.Coverage,city.Request.RemainingScopes??[],new(["x","y"],"meters"),
            city.RequestSha256,city.CatalogSha256,nodes.AsReadOnly(),links.AsReadOnly(),city.SourceStructures,
            new("C# procedural city generator","meters",city.Request.WidthMeters,city.Request.HeightMeters,boxes,segments));
    }

    public async Task<ProceduralCityDocument> GenerateFilesAsync(string requestFile,string catalogFile,string newDirectory,CancellationToken token=default)
    {
        var request=await WorldMetadataGenerator.ReadAsync<ProceduralCityRequest>(requestFile,token);
        var catalog=await WorldMetadataGenerator.ReadAsync<ProceduralCityCatalog>(catalogFile,token);
        var city=Generate(request,catalog,token);var metadata=CreateMetadata(city,token);
        string target=Path.GetFullPath(newDirectory);string parent=Path.GetDirectoryName(target)??throw new ArgumentException("CITY_OUTPUT_INVALID");
        if(Directory.Exists(target)||File.Exists(target))throw new IOException("CITY_OUTPUT_EXISTS");
        Directory.CreateDirectory(parent);string stage=Path.Combine(parent,".city-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(stage);
        try
        {
            await Write("request.json",request);await Write("catalog.json",catalog);await Write("city.json",city);await Write("world-metadata.json",metadata);
            await Write("summary.json",Summary(city));
            // The generic viewer already links to recipe.json; city requests are the recipe for this adapter.
            await Write("recipe.json",request);
            await new WorldMetadataSchematicExporter().WriteAsync(metadata,stage,token);
            await Write("package-manifest.json",await ManifestAsync(city,stage,token));
            token.ThrowIfCancellationRequested();Directory.Move(stage,target);return city;
            async Task Write<T>(string name,T value)=>await File.WriteAllTextAsync(Path.Combine(stage,name),JsonSerializer.Serialize(value,WorldJson.CreateOptions(true)),token);
        }
        finally
        {
            if(Directory.Exists(stage) && Path.GetDirectoryName(Path.GetFullPath(stage))==parent && Path.GetFileName(stage).StartsWith(".city-",StringComparison.Ordinal))Directory.Delete(stage,true);
        }
    }
}
