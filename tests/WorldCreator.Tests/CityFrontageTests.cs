using System.Text.Json;
using WorldCreator.Core;
using WorldCreator.Core.Cities;
using WorldCreator.Core.Geometry;
using WorldCreator.Worlds;

internal static class CityFrontageTests
{
    public static ProceduralCityRequest Example()=>ProceduralCityTests.Request(width:720,height:520) with
    {
        Id="four-frontage-city",ParcelRules=new([CityFrontage.North,CityFrontage.East,CityFrontage.South,CityFrontage.West],32),
        Districts=[new("homes","Four-frontage neighborhoods",1,"rectilinear-grid",["house"],5)]
    };
    public static async Task WriteExamplesAsync(string directory)
    {
        Directory.CreateDirectory(directory);
        var mixed=ProceduralCityTests.Request("recursive-neighborhoods",1) with{Id="mixed-frontage-city",ParcelRules=Example().ParcelRules,
            Districts=ProceduralCityTests.Request("recursive-neighborhoods",1).Districts.Select(d=>d.Id=="production"?d with{ParcelRules=new([CityFrontage.North,CityFrontage.South])}:d).ToArray()};
        foreach(var (name,request) in new[]{("four-frontages.request.json",Example()),("mixed-frontages.request.json",mixed)})
        {
            await using var stream=new FileStream(Path.Combine(directory,name),FileMode.CreateNew,FileAccess.Write);
            await JsonSerializer.SerializeAsync(stream,request,WorldJson.CreateOptions(true));
        }
    }
    public static async Task RunAsync()
    {
        var generator=ProceduralCityTests.Generator();var catalog=ProceduralCityTests.Catalog();
        string Json<T>(T x)=>JsonSerializer.Serialize(x,WorldJson.CreateOptions());
        ProceduralCityDocument? first=null;
        var orientations=Enum.GetValues<CityFrontage>();
        for(int start=0;start<4;start++)
        {
            var rules=new CityParcelRules(orientations.Skip(start).Concat(orientations.Take(start)).ToArray(),32);
            var request=Example() with{ParcelRules=rules};
            var city=generator.Generate(request,catalog);first??=city;
            Check(Json(city)==Json(generator.Generate(request,catalog)),"deterministic cardinal frame "+start);
            Check(city.Parcels.Select(p=>p.Frontage).Distinct().Count()==4 && generator.Validate(city).Count==0,"all four actual connected frontages "+start);
            foreach(var p in city.Parcels)
            {
                double depth=p.Frontage is CityFrontage.North or CityFrontage.South?p.Bounds.Height:p.Bounds.Width;
                Check(depth<=32+1e-7,"maximum parcel depth");
                var body=city.Structures.Single(s=>s.ParentId==p.Id);
                Check(body.RotationDegrees==(int)p.Frontage!*90,"explicit source-aligned orientation");
                Check(p.Frontage is CityFrontage.East or CityFrontage.West?body.Bounds.Width==12 && body.Bounds.Height==14:body.Bounds.Width==14 && body.Bounds.Height==12,"footprint rotates with frontage");
            }
            foreach(var block in city.Blocks)
            {
                var pieces=city.Parcels.Where(p=>p.BlockId==block.Id).Select(p=>p.Bounds).Concat(city.OpenSpaces.Where(s=>s.BlockId==block.Id).Select(s=>s.Bounds)).ToArray();
                Check(Math.Abs(pieces.Sum(p=>Math.Abs(p.Polygon.SignedArea()))-Math.Abs(block.Bounds.Polygon.SignedArea()))<1e-5,"complete parcel/reserve land allocation");
                for(int i=0;i<pieces.Length;i++)for(int j=0;j<i;j++)
                    Check(TerrainCoverageTopology.IntersectionArea(pieces[i].Polygon,pieces[j].Polygon)<1e-7,"independent nonoverlapping land-use polygons");
            }
            Check(city.OpenSpaces.Any(s=>s.Kind=="courtyard") && city.OpenSpaces.Any(s=>s.Kind=="passage"),"explicit internal purpose and access reserve");
            Check(city.OpenSpaces.Where(s=>s.Access is not null).All(s=>city.StreetGraph.Nodes.Any(n=>n.Location.DistanceTo(s.Access!.Points[0])<1e-6)),"courtyard access joins actual road graph");
            Console.WriteLine($"PASS four-frontage orientation {start}: {city.Parcels.Count} parcels, {city.OpenSpaces.Count(s=>s.Kind=="courtyard")} courtyards, {city.OpenSpaces.Count(s=>s.Kind=="passage")} reserved passages.");
        }
        var quad=first!;
        var court=quad.OpenSpaces.First(s=>s.Kind=="courtyard");
        Check(generator.Validate(quad with{OpenSpaces=quad.OpenSpaces.Where(s=>s.Id!=court.Id).ToArray()}).Any(i=>i.StartsWith("BLOCK_LAND_USE_INCOMPLETE")),"missing interior rejected");
        Check(generator.Validate(quad with{OpenSpaces=quad.OpenSpaces.Select(s=>s.Id==court.Id?s with{Access=null}:s).ToArray()}).Any(i=>i.StartsWith("RESERVE_ACCESS_MISSING")),"missing access rejected");
        var passage=quad.OpenSpaces.First(s=>s.Kind=="passage");
        Check(generator.Validate(quad with{OpenSpaces=quad.OpenSpaces.Select(s=>s.Id==passage.Id?s with{Bounds=s.Bounds with{Width=s.Bounds.Width/2}}:s).ToArray()}).Any(i=>i.StartsWith("RESERVE_ACCESS_UNRESERVED")),"full access width must fit declared reserve");
        var parcel=quad.Parcels.First();
        Check(generator.Validate(quad with{Parcels=quad.Parcels.Select(p=>p.Id==parcel.Id?p with{Frontage=CityFrontage.West}:p).ToArray()}).Any(i=>i.StartsWith("PARCEL_FRONTAGE_ORIENTATION_INVALID")),"wrong frontage metadata rejected");
        var nestedCatalog=catalog with{Structures=catalog.Structures.Append(new("campus","campus","Nested rotated facilities",new(0,0,116,60),[new("wings","compound",2,"Detached subfacilities")])).ToArray()};
        var nestedRequest=Example() with{WidthMeters=1600,HeightMeters=1400,TargetBlockWidthMeters=600,TargetBlockDepthMeters=600,
            ParcelRules=new(orientations,80),Districts=[new("campus","Multi-level constituent programs",1,"rectilinear-grid",["campus"])]};
        var nested=generator.Generate(nestedRequest,nestedCatalog);
        Check(nested.Structures.Any(s=>s.Id.Contains("/wings-0/workspaces-0")) && nested.Structures.Select(s=>s.RotationDegrees).Distinct().Count()==4 && generator.Validate(nested).Count==0,"rotated recursive compound sites and access on every side");
        foreach(var rules in new CityParcelRules[]{new([],32),new([CityFrontage.North,CityFrontage.North],32),new([(CityFrontage)99],32),new(orientations,double.NaN),new(orientations,32,0)})
            Reject(()=>generator.Generate(Example() with{ParcelRules=rules},catalog),"CITY_PARCEL_POLICY_INVALID");
        Reject(()=>generator.Generate(Example() with{ParcelRules=new(orientations,10)},catalog),"CITY_FRONTAGE_DEPTH_DOES_NOT_FIT");
        string root=Path.Combine(Path.GetTempPath(),"city-frontage-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(root,"request.json"),Json(Example()));await File.WriteAllTextAsync(Path.Combine(root,"catalog.json"),Json(catalog));
            var saved=await generator.GenerateFilesAsync(Path.Combine(root,"request.json"),Path.Combine(root,"catalog.json"),Path.Combine(root,"out"));
            var restored=await WorldCreator.Core.Metadata.WorldMetadataGenerator.ReadAsync<ProceduralCityDocument>(Path.Combine(root,"out/city.json"));
            Check(Json(saved)==Json(restored) && generator.Validate(restored).Count==0,"four-sided city persists full frontage/reserve geometry");
            var metadata=generator.CreateMetadata(restored);
            Check(metadata.Nodes.Any(n=>n.Kind=="courtyard") && metadata.Connections.Any(c=>c.Kind=="interior-access"),"actual reserve ownership/access in spatial metadata");
        }
        finally{if(Path.GetFullPath(root).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase)&&Path.GetFileName(root).StartsWith("city-frontage-"))Directory.Delete(root,true);}
        Console.WriteLine("PASS: four-sided frontages, bounded depths, complete land allocation, reserved swept-width interior access, recursive rotations, corrupt/missing geometry rejection and persistence.");
    }
    private static void Check(bool ok,string message){if(!ok)throw new Exception("City frontages: "+message);}
    private static void Reject(Action act,string code){try{act();}catch(ArgumentException ex)when(ex.Message.StartsWith(code,StringComparison.Ordinal)){return;}throw new Exception("Expected "+code);}
}
