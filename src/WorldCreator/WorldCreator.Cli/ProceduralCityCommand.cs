using WorldCreator.Core.Cities;
using WorldCreator.Core.Planning;
using WorldCreator.Worlds;

namespace WorldCreator.Cli;

/// <summary>CLI composition boundary; calculations stay in the engine.</summary>
internal static class ProceduralCityCommand
{
    private static ProceduralCityGenerator Generator(ISettlementCatalogRegistry? registry)=>new([new RectilinearCityLayout(),new SubdivisionCityLayout()],registry);
    public static Task<ProceduralCityDocument> RunAsync(string[] args,ISettlementCatalogRegistry? registry,CancellationToken token)
    {
        if(args.Length!=4)throw new ArgumentException("Expected generate-city <request.json> <catalog.json> <new-output-dir>.");
        return Generator(registry).GenerateFilesAsync(args[1],args[2],args[3],token);
    }
    public static Task<CityPackageVerification> VerifyAsync(string directory,ISettlementCatalogRegistry? registry,CancellationToken token)=>Generator(registry).VerifyFilesAsync(directory,token);
}
