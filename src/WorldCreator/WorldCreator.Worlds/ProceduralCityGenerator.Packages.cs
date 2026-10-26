using System.Security.Cryptography;
using System.Text.Json;
using WorldCreator.Core;
using WorldCreator.Core.Cities;
using WorldCreator.Core.Metadata;

namespace WorldCreator.Worlds;

public sealed partial class ProceduralCityGenerator
{
    private static readonly string[] PackageFiles=["request.json","recipe.json","catalog.json","city.json","world-metadata.json","summary.json","schematic.html","schematic.js"];
    private const long PackageFileLimit=16*1024*1024;

    private static async Task<string> FileDigestAsync(string path,CancellationToken token)
    {
        await using var stream=File.OpenRead(path);
        return Convert.ToHexStringLower(await SHA256.HashDataAsync(stream,token));
    }
    private static async Task<IReadOnlyDictionary<string,string>> BuildsAsync(CancellationToken token)
    {
        var builds=new SortedDictionary<string,string>(StringComparer.Ordinal);
        foreach(var assembly in new[]{typeof(ProceduralCityGenerator).Assembly,typeof(WorldMetadataDocument).Assembly})
        {
            if(string.IsNullOrEmpty(assembly.Location))throw new NotSupportedException("CITY_ENGINE_BUILD_ID_UNAVAILABLE");
            builds.Add(assembly.GetName().Name!,await FileDigestAsync(assembly.Location,token));
        }
        return builds;
    }
    private static async Task<CityPackageManifest> ManifestAsync(ProceduralCityDocument city,string root,CancellationToken token)
    {
        var files=new SortedDictionary<string,CityPackageFile>(StringComparer.Ordinal);
        foreach(var name in PackageFiles)
        {
            string path=Path.Combine(root,name);var info=new FileInfo(path);
            if(!info.Exists || info.Length is <=0 or >PackageFileLimit || (info.Attributes&FileAttributes.ReparsePoint)!=0)throw new IOException("CITY_PACKAGE_FILE_INVALID:"+name);
            files.Add(name,new(await FileDigestAsync(path,token),info.Length));
        }
        return new(1,city.Id,city.RequestSha256,city.CatalogSha256,await BuildsAsync(token),files);
    }

    public async Task<CityPackageVerification> VerifyFilesAsync(string directory,CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested();var issues=new List<string>();string root=Path.GetFullPath(directory);
        try
        {
            if(!Directory.Exists(root) || (File.GetAttributes(root)&FileAttributes.ReparsePoint)!=0)return new(["CITY_PACKAGE_ROOT_INVALID"]);
            var manifestPath=Path.Combine(root,"package-manifest.json");
            if(!File.Exists(manifestPath) || (File.GetAttributes(manifestPath)&FileAttributes.ReparsePoint)!=0)return new(["CITY_PACKAGE_MANIFEST_MISSING_OR_LINKED"]);
            var manifest=await WorldMetadataGenerator.ReadAsync<CityPackageManifest>(manifestPath,token);
            bool Hash(string? s)=>s is {Length:64} && s.All(char.IsAsciiHexDigit);
            if(manifest.SchemaVersion!=1 || manifest.Files is null || manifest.EngineBuilds is null || !Hash(manifest.RequestSha256) || !Hash(manifest.CatalogSha256) ||
                manifest.Files.Count!=PackageFiles.Length || !PackageFiles.All(manifest.Files.ContainsKey) || manifest.EngineBuilds.Count!=2 ||
                manifest.Files.Any(p=>p.Value is null || !Hash(p.Value.Sha256) || p.Value.Length is <=0 or >PackageFileLimit))return new(["CITY_PACKAGE_MANIFEST_INVALID"]);
            foreach(var name in PackageFiles)
            {
                token.ThrowIfCancellationRequested();var info=new FileInfo(Path.Combine(root,name));
                if(!info.Exists || (info.Attributes&FileAttributes.ReparsePoint)!=0 || info.Length!=manifest.Files[name].Length)
                {issues.Add("CITY_PACKAGE_FILE_MISSING_OR_CHANGED:"+name);continue;}
                if(await FileDigestAsync(info.FullName,token)!=manifest.Files[name].Sha256)issues.Add("CITY_PACKAGE_HASH_MISMATCH:"+name);
            }
            var builds=await BuildsAsync(token);
            if(builds.Any(p=>!manifest.EngineBuilds.TryGetValue(p.Key,out string? recorded) || p.Value!=recorded))issues.Add("CITY_PACKAGE_ENGINE_BUILD_MISMATCH");
            if(issues.Count>0)return new(issues);
            var request=await WorldMetadataGenerator.ReadAsync<ProceduralCityRequest>(Path.Combine(root,"request.json"),token);
            var catalog=await WorldMetadataGenerator.ReadAsync<ProceduralCityCatalog>(Path.Combine(root,"catalog.json"),token);
            var saved=await WorldMetadataGenerator.ReadAsync<ProceduralCityDocument>(Path.Combine(root,"city.json"),token);
            var generated=Generate(request,catalog,token);
            if(manifest.CityId!=generated.Id || manifest.RequestSha256!=generated.RequestSha256 || manifest.CatalogSha256!=generated.CatalogSha256)issues.Add("CITY_PACKAGE_INPUT_PROVENANCE_MISMATCH");
            string Serialize<T>(T value)=>JsonSerializer.Serialize(value,WorldJson.CreateOptions(true));
            if(Serialize(saved)!=Serialize(generated))issues.Add("CITY_PACKAGE_GEOMETRY_NOT_REPRODUCIBLE");
            var metadata=CreateMetadata(generated,token);
            if(await File.ReadAllTextAsync(Path.Combine(root,"world-metadata.json"),token)!=Serialize(metadata))issues.Add("CITY_PACKAGE_METADATA_NOT_REPRODUCIBLE");
            if(await File.ReadAllTextAsync(Path.Combine(root,"recipe.json"),token)!=await File.ReadAllTextAsync(Path.Combine(root,"request.json"),token))issues.Add("CITY_PACKAGE_RECIPE_MISMATCH");
            if(await File.ReadAllTextAsync(Path.Combine(root,"summary.json"),token)!=Serialize(Summary(generated)))issues.Add("CITY_PACKAGE_SUMMARY_MISMATCH");
            var view=await new WorldMetadataSchematicExporter().CreateAsync(metadata,token);
            if(await File.ReadAllTextAsync(Path.Combine(root,"schematic.html"),token)!=view.Html || await File.ReadAllTextAsync(Path.Combine(root,"schematic.js"),token)!=view.JavaScript)issues.Add("CITY_PACKAGE_VIEWER_MISMATCH");
            return new(issues);
        }
        catch(Exception ex)when(ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException or NotSupportedException)
        {return new(["CITY_PACKAGE_VERIFICATION_FAILED:"+ex.GetType().Name]);}
    }
    private static object Summary(ProceduralCityDocument city)=>new{city.Id,Districts=city.Districts.Count,Streets=city.Streets.Count,Blocks=city.Blocks.Count,Parcels=city.Parcels.Count,
        Structures=city.Structures.Count,OpenSpaces=city.OpenSpaces.Count,LandUse=city.OpenSpaces.GroupBy(s=>s.Kind).OrderBy(g=>g.Key,StringComparer.Ordinal).ToDictionary(g=>g.Key,g=>g.Count()),
        StreetGraphConnected=city.StreetGraph.IsConnected(),city.Request.Coverage,city.RequestSha256,city.CatalogSha256,GeometryOwner="C# engine; JavaScript renders exact generated meter coordinates"};
}
