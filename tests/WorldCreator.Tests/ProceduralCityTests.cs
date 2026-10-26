using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using WorldCreator.Catalogs.Modern;
using WorldCreator.Core;
using WorldCreator.Core.Cities;
using WorldCreator.Core.Geometry;
using WorldCreator.Core.Metadata;
using WorldCreator.Worlds;

internal static class ProceduralCityTests
{
    public static ProceduralCityGenerator Generator(bool sources=false)=>new([new RectilinearCityLayout(),new SubdivisionCityLayout()],sources?TestCatalogs.Registry:null);
    public static ProceduralCityCatalog Catalog()=>new(1,"game-city-catalog",[
        new("house","house","Residential home",new(0,0,14,12),[]),
        new("shop","shop","Neighborhood trade",new(0,0,18,14),[]),
        new("civic","civic-building","Public services",new(0,0,24,18),[]),
        new("workshop","workshop","Production building",new(0,0,20,18),[]),
        new("compound","facility","Reusable compound site",new(0,0,52,36),[
            new("workspaces","workshop",2,"Separate production structures")])]);
    public static ProceduralCityRequest Request(string layout="rectilinear-grid",int variation=0,double width=1200,double height=800)=>
        new(1,"procedural-city",width,height,new(3,2,1.5,1.5,0.25,0.25),180,170,120,26,3,[
            new("neighborhoods","Homes and local shops",2,layout,["house","house","shop"],5),
            new("center","Civic services and trade",1,layout,["civic","shop"],4),
            new("production","Employment and production",1,layout,["workshop","compound"],4)],variation);
    public static async Task WriteExamplesAsync(string directory)
    {
        Directory.CreateDirectory(directory);
        foreach(var (name,value) in new (string,object)[]{("catalog.json",Catalog()),("grid.request.json",Request()),
            ("subdivision.request.json",Request("recursive-neighborhoods",3)),("mixed.request.json",Request("recursive-neighborhoods",1) with{Districts=[
                new("residential","Residential neighborhoods",2,"recursive-neighborhoods",["house","shop"],4),
                new("center","Formal civic center",1,"rectilinear-grid",["civic","shop"],3),
                new("production","Compound employment sites",1,"recursive-neighborhoods",["compound","workshop"],4)]})})
        {
            await using var file=new FileStream(Path.Combine(directory,name),FileMode.CreateNew,FileAccess.Write);
            await JsonSerializer.SerializeAsync(file,value,value.GetType(),WorldJson.CreateOptions(true));
        }
    }
    public static async Task RunAsync(string? cli)
    {
        var generator=Generator();var catalog=Catalog();
        string Json<T>(T value)=>JsonSerializer.Serialize(value,WorldJson.CreateOptions());
        var grid=generator.Generate(Request(),catalog);var subdivision=generator.Generate(Request("recursive-neighborhoods",3),catalog);
        Check(Json(grid)==Json(generator.Generate(Request(),catalog)),"determinism");
        Check(Json(grid.Streets)!=Json(subdivision.Streets) && Json(grid.Blocks.Select(b=>b.Bounds))!=Json(subdivision.Blocks.Select(b=>b.Bounds)),"different engine layouts calculate different street/block geometry");
        foreach(var layout in new[]{"rectilinear-grid","recursive-neighborhoods"})
        foreach(var (width,height,variation) in new[]{(1200d,800d,0),(1200d,800d,2),(900d,700d,4)})
        {
            var city=generator.Generate(Request(layout,variation,width,height),catalog);
            Check(city.Districts.Count==3 && city.Structures.Count>50 && city.Parcels.Count>30 && city.OpenSpaces.Count>0,"actual city generated from rules "+layout);
            Check(city.StreetGraph.IsConnected() && generator.Validate(city).Count==0,"connected streets/access and valid geometry");
            double roadWidth=city.Request.StreetProfile.TotalWidthMeters;
            Check(Math.Abs(city.Districts.Sum(d=>d.Bounds.Width*d.Bounds.Height)-(width-roadWidth)*(height-roadWidth))<1e-5,"district partition area");
            foreach(var solid in city.Structures.Where(s=>!s.IsCompound))
            foreach(var road in city.Streets)
                foreach(var surface in StreetSurfaceBuilder.Build(road))
                    Check(TerrainCoverageTopology.IntersectionArea(solid.Bounds.Polygon,new(surface.Polygon))<1e-7,"independent polygon street/footprint clearance");
            Check(city.Structures.Any(s=>s.IsCompound) && city.Structures.Where(s=>s.ParentId!=s.ParcelId).All(s=>city.Structures.Single(p=>p.Id==s.ParentId).Bounds.Contains(s.Bounds)),"constituent structure ownership");
            var metadata=generator.CreateMetadata(city);
            Check(metadata.MapGeometry is not null && metadata.MapGeometry.Boxes.Count==metadata.Nodes.Count && metadata.Nodes.All(n=>n.Position is not null),"exact engine geometry handed to JavaScript");
            Console.WriteLine($"PASS city profile {layout}, {width}x{height}, variation {variation}: {city.Streets.Count} streets, {city.Blocks.Count} blocks, {city.Parcels.Count} parcels, {city.Structures.Count} structures, {city.OpenSpaces.Count} parks.");
        }
        Check(Json(grid.Streets)!=Json(generator.Generate(Request(variation:2),catalog).Streets),"variation changes rule-based geometry");
        var sparse=generator.Generate(Request() with{TargetParcelFrontageMeters=60},catalog);
        Check(sparse.Parcels.Count<grid.Parcels.Count,"frontage parameter controls parcel density");
        var nestedCatalog=catalog with{Structures=catalog.Structures.Append(new("campus","campus","Nested facilities",new(0,0,116,60),[new("wings","compound",2,"Independent nested facilities")])).ToArray()};
        var nested=generator.Generate(Request() with{TargetBlockWidthMeters=400,TargetBlockDepthMeters=300,Districts=[new("campus-area","Nested sites",1,"rectilinear-grid",["campus"])]},nestedCatalog);
        Check(nested.Structures.Any(s=>s.Id.Contains("/wings-0/workspaces-0",StringComparison.Ordinal)) && generator.Validate(nested).Count==0,"recursive compound programs generate multiple ownership levels with clear access");
        Reject(()=>generator.Generate(Request() with{WidthMeters=double.NaN},catalog),"CITY_INPUT_INVALID");
        Reject(()=>generator.Generate(Request() with{MaximumBlocks=1},catalog),"CITY_");
        Reject(()=>generator.Generate(Request() with{MaximumParcels=1},catalog),"CITY_PARCEL_BUDGET");
        Reject(()=>generator.Generate(Request() with{MaximumStructures=1},catalog),"CITY_STRUCTURE_BUDGET");
        var custom=new ProceduralCityGenerator([new EmptyLayout()]);
        Reject(()=>custom.Generate(Request() with{Districts=[new("empty","Invalid registered generator",1,"empty-layout",["house"])]},catalog),"CITY_LAYOUT_RESULT_INVALID");
        Reject(()=>generator.Generate(Request() with{Districts=[new("d","Unsupported layout",1,"missing",["house"])]},catalog),"CITY_DISTRICT_RULE_INVALID");
        Reject(()=>generator.Generate(Request(),catalog with{Structures=catalog.Structures.Select(t=>t.Id=="compound"?t with{Footprint=new(0,0,10,10)}:t).ToArray()}),"CITY_COMPOUND_PARTS_DO_NOT_FIT");
        Reject(()=>generator.Generate(Request(),catalog with{Structures=catalog.Structures.Select(t=>t.Id=="house"?t with{Parts=[new("cycle","house",1,"bad cycle")]}:t).ToArray()}),"CITY_CATALOG_CYCLE_OR_DEPTH");
        Check(generator.Validate(grid with{Streets=grid.Streets.Skip(1).ToArray()}).Count>0,"missing actual road rejected even when cached graph was connected");
        Check(generator.Validate(grid with{Structures=grid.Structures.Skip(1).ToArray()}).Count>0,"missing parcel structure rejected");
        var obstructed=grid.Structures[0] with{Access=new([grid.Structures[0].Access.Points[0],grid.Structures[0].Bounds.Center])};
        Check(generator.Validate(grid with{Structures=grid.Structures.Select(s=>s.Id==obstructed.Id?obstructed:s).ToArray()}).Any(i=>i.StartsWith("ACCESS_BLOCKED",StringComparison.Ordinal)),"route entering a solid footprint is rejected");
        var referenced=new ProceduralCityCatalog(1,"referenced",[new("warehouse","warehouse","Registered warehouse",null,[],CatalogReference:new(ModernCatalog.Selection,"warehouse","small"))]);
        var sourceRequest=Request() with{Districts=[new("d","Catalog-based industrial sites",1,"rectilinear-grid",["warehouse"])]};
        Reject(()=>generator.Generate(sourceRequest,referenced),"CITY_SOURCE_CATALOG_REQUIRED");
        var sourceCity=Generator(true).Generate(sourceRequest,referenced);
        Check(sourceCity.SourceStructures["warehouse"].Variant.WidthMeters==sourceCity.Structures[0].Bounds.Width,"registered source dimensions reused");
        Reject(()=>Generator(true).Generate(sourceRequest with{StreetProfile=new(0.5,1,0,0,0,0)},referenced),"CITY_SOURCE_ROAD_UNDERSIZED");
        using(var cancel=new CancellationTokenSource()){cancel.Cancel();try{generator.Generate(Request(),catalog,cancel.Token);throw new Exception("Cancellation absent");}catch(OperationCanceledException){}}
        string root=Path.Combine(Path.GetTempPath(),"worldcreator-city-tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try
        {
            await WriteExamplesAsync(root);string request=Path.Combine(root,"grid.request.json"),cat=Path.Combine(root,"catalog.json"),output=Path.Combine(root,"output");
            await generator.GenerateFilesAsync(request,cat,output);
            var restored=await WorldMetadataGenerator.ReadAsync<ProceduralCityDocument>(Path.Combine(output,"city.json"));
            Check(Json(restored)==Json(grid),"city persistence roundtrip");
            string hash=Hash(Path.Combine(output,"city.json"));
            try{await generator.GenerateFilesAsync(request,cat,output);throw new Exception("Overwrite allowed");}catch(IOException){}
            Check(Hash(Path.Combine(output,"city.json"))==hash && !Directory.GetDirectories(root,".city-*").Any(),"atomic failure preserves outputs");
            using(var cancelled=new CancellationTokenSource())
            {
                cancelled.Cancel();string cancelledOutput=Path.Combine(root,"cancelled");
                try{await generator.GenerateFilesAsync(request,cat,cancelledOutput,cancelled.Token);throw new Exception("Export cancellation absent");}catch(OperationCanceledException){}
                Check(!Directory.Exists(cancelledOutput),"cancelled file export leaves no published city");
            }
            if(cli is not null)
            {
                Directory.CreateDirectory(Path.Combine(root,".gameframework/catalogs"));
                await File.WriteAllTextAsync(Path.Combine(root,".gameframework/catalogs/unrelated.json"),"invalid unrelated catalog manifest");
                Check(await Run(request,cat,Path.Combine(root,"cli"))==0,"actual fresh CLI city generation");
                Check(await Run(request,cat,Path.Combine(root,"cli"))==1,"CLI existing destination refusal");
                string audit=await File.ReadAllTextAsync(Path.Combine(root,".gameframework/operations/audit.jsonl"));
                Check(audit.Contains("world.city.succeeded")&&audit.Contains("world.city.failed")&&!Directory.Exists(Path.Combine(root,".gamearts")),"shared city audit and no image dependency");
                async Task<int> Run(params string[] args)
                {
                    var start=new ProcessStartInfo("dotnet"){WorkingDirectory=root,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true};
                    start.ArgumentList.Add(cli);start.ArgumentList.Add("generate-city");foreach(var arg in args)start.ArgumentList.Add(arg);
                    using var process=Process.Start(start)!;var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();
                    using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(60));
                    try{await process.WaitForExitAsync(timeout.Token);}catch(OperationCanceledException){process.Kill(true);throw;}
                    string message=await stdout+await stderr;if(process.ExitCode!=0 && !message.Contains("CITY_OUTPUT_EXISTS"))Console.WriteLine(message);return process.ExitCode;
                }
            }
        }
        finally{if(Path.GetFullPath(root).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase)&&Path.GetFileName(root).StartsWith("worldcreator-city-tests-",StringComparison.Ordinal))Directory.Delete(root,true);}
        Console.WriteLine("PASS: procedural engine city generation, two layout strategies, dimensions/variation/density, nested structures, actual street/parcel geometry, constraints, source catalog, negatives, cancellation, persistence and CLI/audit.");
    }
    private static string Hash(string file)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
    private static void Reject(Action action,string code){try{action();}catch(ArgumentException ex)when(ex.Message.StartsWith(code,StringComparison.Ordinal)){return;}throw new Exception("Expected "+code);}
    private static void Check(bool valid,string message){if(!valid)throw new Exception("Procedural city: "+message);}
    private sealed class EmptyLayout : ICityStreetLayout
    {
        public string Id=>"empty-layout";
        public CityLayoutResult Generate(CityLayoutContext context,CancellationToken token=default)=>new([],[]);
    }
}
