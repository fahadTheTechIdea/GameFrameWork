using System.Text.Json;
using System.Security.Cryptography;
using WorldCreator.Core;
using WorldCreator.Core.Cities;
using WorldCreator.Core.Metadata;
using WorldCreator.Catalogs.Modern;

internal static class CityPackageTests
{
    public static async Task RunAsync()
    {
        var generator=ProceduralCityTests.Generator();
        string root=Path.Combine(Path.GetTempPath(),"city-package-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        string Json<T>(T x)=>JsonSerializer.Serialize(x,WorldJson.CreateOptions(true));
        try
        {
            await File.WriteAllTextAsync(Path.Combine(root,"request.json"),Json(CityFrontageTests.Example()));
            await File.WriteAllTextAsync(Path.Combine(root,"catalog.json"),Json(ProceduralCityTests.Catalog()));
            string package=Path.Combine(root,"packet");await generator.GenerateFilesAsync(Path.Combine(root,"request.json"),Path.Combine(root,"catalog.json"),package);
            Check((await generator.VerifyFilesAsync(package)).Valid,"fresh package verifies with regeneration and viewer checks");
            string manifestPath=Path.Combine(package,"package-manifest.json"),cityPath=Path.Combine(package,"city.json"),jsPath=Path.Combine(package,"schematic.js");
            string originalManifest=await File.ReadAllTextAsync(manifestPath),originalCity=await File.ReadAllTextAsync(cityPath),originalJs=await File.ReadAllTextAsync(jsPath);
            await File.WriteAllTextAsync(cityPath,originalCity+" ");
            Check(!(await generator.VerifyFilesAsync(package)).Valid,"byte tampering rejected");
            await File.WriteAllTextAsync(cityPath,originalCity);
            var city=await WorldMetadataGenerator.ReadAsync<ProceduralCityDocument>(cityPath);
            await File.WriteAllTextAsync(cityPath,Json(city with{Structures=city.Structures.Select((s,i)=>i==0?s with{Bounds=s.Bounds with{X=s.Bounds.X+1}}:s).ToArray()}));
            await Rehash("city.json");
            Check((await generator.VerifyFilesAsync(package)).Issues.Contains("CITY_PACKAGE_GEOMETRY_NOT_REPRODUCIBLE"),"geometry tampering rejected even after manifest rehash");
            await File.WriteAllTextAsync(cityPath,originalCity);await File.WriteAllTextAsync(manifestPath,originalManifest);
            await File.WriteAllTextAsync(jsPath,originalJs+"\n/* forged viewer */");await Rehash("schematic.js");
            Check((await generator.VerifyFilesAsync(package)).Issues.Contains("CITY_PACKAGE_VIEWER_MISMATCH"),"modified viewer rejected even after manifest rehash");
            await File.WriteAllTextAsync(jsPath,originalJs);await File.WriteAllTextAsync(manifestPath,originalManifest);
            var manifest=await WorldMetadataGenerator.ReadAsync<CityPackageManifest>(manifestPath);
            var unsafeFiles=manifest.Files.ToDictionary(p=>p.Key,p=>p.Value);unsafeFiles["../outside.json"]=unsafeFiles["city.json"];unsafeFiles.Remove("city.json");
            await File.WriteAllTextAsync(manifestPath,Json(manifest with{Files=unsafeFiles}));
            Check((await generator.VerifyFilesAsync(package)).Issues.Contains("CITY_PACKAGE_MANIFEST_INVALID"),"manifest traversal rejected before file lookup");
            await File.WriteAllTextAsync(manifestPath,originalManifest);
            var builds=manifest.EngineBuilds.ToDictionary(p=>p.Key,_=>new string('0',64));
            await File.WriteAllTextAsync(manifestPath,Json(manifest with{EngineBuilds=builds}));
            Check((await generator.VerifyFilesAsync(package)).Issues.Contains("CITY_PACKAGE_ENGINE_BUILD_MISMATCH"),"stale engine build rejected");
            await File.WriteAllTextAsync(manifestPath,originalManifest);
            using(var cancel=new CancellationTokenSource()){cancel.Cancel();try{await generator.VerifyFilesAsync(package,cancel.Token);throw new Exception("Cancellation absent");}catch(OperationCanceledException){}}
            Check((await generator.VerifyFilesAsync(package)).Valid && await File.ReadAllTextAsync(manifestPath)==originalManifest,"verification remains read-only");
            var sourceCatalog=new ProceduralCityCatalog(1,"source-catalog",[new("warehouse","warehouse","Registered structure",null,[],CatalogReference:new(ModernCatalog.Selection,"warehouse","small"))]);
            var sourceRequest=ProceduralCityTests.Request() with{Districts=[new("source-sites","Registered source dimensions",1,"rectilinear-grid",["warehouse"])]};
            await File.WriteAllTextAsync(Path.Combine(root,"source.request.json"),Json(sourceRequest));await File.WriteAllTextAsync(Path.Combine(root,"source.catalog.json"),Json(sourceCatalog));
            var sourceGenerator=ProceduralCityTests.Generator(true);
            string sourcePackage=Path.Combine(root,"source-packet");
            await sourceGenerator.GenerateFilesAsync(Path.Combine(root,"source.request.json"),Path.Combine(root,"source.catalog.json"),sourcePackage);
            Check((await sourceGenerator.VerifyFilesAsync(sourcePackage)).Valid && !(await generator.VerifyFilesAsync(sourcePackage)).Valid,"source package reproduces only with required catalog registry");
            async Task Rehash(string name)
            {
                var m=await WorldMetadataGenerator.ReadAsync<CityPackageManifest>(manifestPath);var files=m.Files.ToDictionary(p=>p.Key,p=>p.Value);
                byte[] bytes=await File.ReadAllBytesAsync(Path.Combine(package,name));files[name]=new(Convert.ToHexStringLower(SHA256.HashData(bytes)),bytes.LongLength);
                await File.WriteAllTextAsync(manifestPath,Json(m with{Files=files}));
            }
        }
        finally{if(Path.GetFullPath(root).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase)&&Path.GetFileName(root).StartsWith("city-package-"))Directory.Delete(root,true);}
        Console.WriteLine("PASS: city package manifests, byte hashes, regenerated geometry/metadata/viewer, forged hashes, path traversal, engine identity, cancellation and read-only verification.");
    }
    private static void Check(bool ok,string message){if(!ok)throw new Exception("City packages: "+message);}
}
