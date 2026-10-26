using WorldCreator.Catalogs.Modern;
using WorldCreator.Core;
using WorldCreator.Core.Planning;
using WorldCreator.Worlds;
using System.Diagnostics;
using System.Text.Json;

internal static class CatalogContractTests
{
    public static async Task RunAsync(string? cliPath=null)
    {
        var modern=new ModernCatalog(); var registry=new SettlementCatalogRegistry([modern]);
        Check(registry.Resolve(ModernCatalog.Selection).Structures.Count==197,"Modern inventory");
        Check(registry.Catalogs.Single().Name=="Modern" && modern.Structures.Sum(d=>d.SizeVariants!.Count)==585,"Modern identity and variants");
        Reject(()=>registry.Resolve(null!),"CATALOG_SELECTION_REQUIRED");
        Reject(()=>registry.Resolve(new("unknown",new HashSet<string>())),"CATALOG_NOT_REGISTERED");
        Reject(()=>registry.Resolve(new("modern",new HashSet<string> { "unknown" })),"CATALOG_CAPABILITY_UNKNOWN");
        Reject(()=>new SettlementCatalogRegistry([modern,modern]),"CATALOG_DUPLICATE_ID");
        Reject(()=>new SettlementCatalogRegistry([new Decorated(modern,modern.Descriptor with {
            RequiredCatalogs=new HashSet<string> { "missing" } })]),"CATALOG_DEPENDENCY_MISSING");
        Reject(()=>new SettlementCatalogRegistry([new Decorated(modern,modern.Descriptor with {
            RequiredCatalogs=new HashSet<string> { "modern" } })]),"CATALOG_DEPENDENCY_CYCLE");
        Reject(()=>new SettlementCatalogRegistry([new Decorated(modern,modern.Descriptor with {
            Vocabulary=modern.Descriptor.Vocabulary with { Resources=new HashSet<string>() } })]),"CATALOG_VOCABULARY_UNREGISTERED");
        Reject(()=>new SettlementCatalogRegistry([new Decorated(modern,modern.Descriptor with {
            Capabilities=new HashSet<string>() })]),"CATALOG_POLICY_CONTRACT_INVALID");
        var request=SitingAlgorithmTests.Create(); var engine=new DesertWorldGenerator(registry);
        await Rejected(engine,request with { Design=request.Design! with { Catalog=null } },"CATALOG_SELECTION_REQUIRED");
        await Rejected(engine,request with { Design=request.Design! with { Catalog=new("modern",ModernCapabilities.All.Where(c=>c!=ModernCapabilities.Electricity).ToHashSet()) } },"CATALOG_CAPABILITY_REQUIRED");
        await Rejected(engine,request with { Design=request.Design! with { SchemaVersion=2 } },"MOCKUP_DESIGN_INVALID");
        await Rejected(engine,request with { Design=request.Design! with { Siting=null } },"SITING_PLAN_REQUIRED");
        var world=await engine.GenerateAsync(request);
        Check(world.Catalog!.Id=="modern" && world.Catalog.EnabledCapabilities.SetEquals(ModernCapabilities.All) && world.Catalog.ContentSha256.Length==64,"exported catalog provenance");

        string pluginPath=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../WorldCreator.TestCatalog/bin/Release/net10.0/WorldCreator.TestCatalog.dll"));
        using var plugin=new SettlementCatalogPlugin(pluginPath);
        Reject(()=>new SettlementCatalogPlugin(pluginPath+".missing"),"CATALOG_PLUGIN_PATH_INVALID");
        Reject(()=>new SettlementCatalogPlugin(typeof(ISettlementCatalogProvider).Assembly.Location),"CATALOG_PLUGIN_PROVIDER_MISSING");
        Check(plugin.Providers.Count==1 && plugin.Providers[0].GetType().Assembly!=typeof(ModernCatalog).Assembly,"independent plugin assembly");
        var external=plugin.Providers.Single();
        var plugins=new SettlementCatalogRegistry([modern,external]);
        var reverse=new SettlementCatalogRegistry([external,modern]);
        Check(plugins.Resolve(ModernCatalog.Selection).Provenance.ContentSha256==reverse.Resolve(ModernCatalog.Selection).Provenance.ContentSha256,"registration order independence");
        var selection=new SettlementCatalogSelection(external.Descriptor.Id,external.Descriptor.Capabilities);
        var registered=plugins.Resolve(selection);
        Check(new BuildingProgramPlanner(registered.Structures,registered.Demand).Plan(new(SettlementClass.Town,SettlementRole.Mixed,1)).Needs.Single().DefinitionId=="test.arcane-hall","fixed external demand policy");
        var arcane=Arcane(request,external,selection);
        var arcaneEngine=new DesertWorldGenerator(plugins);
        var arcaneWorld=await arcaneEngine.GenerateAsync(arcane);
        Check(arcaneWorld.Siting!.Status==SitingEvaluationStatus.Passed && arcaneWorld.Siting.Allocations.Single().RequirementId=="mana" &&
            arcaneWorld.Catalog!.Id=="test.arcane" && !arcaneWorld.Catalog.EnabledCapabilities.Any(c=>c.StartsWith("modern.",StringComparison.Ordinal)),"custom vocabulary and fixed code without Modern services");
        var invalid=arcane with { Design=arcane.Design! with { Siting=arcane.Design.Siting! with { Sources=arcane.Design.Siting.Sources.Select(s=>s with { Qualities=new HashSet<string>() }).ToArray() } } };
        await Rejected(arcaneEngine,invalid,"SITING_DEPENDENCY_FAILED");
        await Rejected(arcaneEngine,arcane with { Design=arcane.Design! with { Catalog=selection with { EnabledCapabilities=new HashSet<string>() } } },"CATALOG_CAPABILITY_REQUIRED");
        using var cancellation=new CancellationTokenSource(); cancellation.Cancel();
        try { new SettlementCatalogRegistry([modern],cancellation.Token); throw new Exception("Missing cancellation"); } catch (OperationCanceledException) { }
        if (cliPath is not null) await Cli(cliPath,pluginPath,arcane,invalid);
        Console.WriteLine("PASS: catalog contracts: Modern 197/585; explicit capability flags; independent dynamic plugin with custom vocabulary, fixed demand/siting code and actual world; no implicit catalog or legacy schema; CLI/plugin failures and audit.");
    }
    private static WorldRequest Arcane(WorldRequest request,ISettlementCatalogProvider pack,SettlementCatalogSelection selection)
    {
        var d=request.Design!; var path=d.Paths.Single(p=>p.Id=="service-power") with { Id="mana-line" };
        return request with { Design=d with { Catalog=selection,BuildingCatalog=pack.Structures,Paths=d.Paths.Append(path).ToArray(),
            SettlementRules=d.SettlementRules.Select(r=>r with { Envelope=r.Envelope with { BuildingType="test.arcane-hall" },SizeVariantId="small",RequiredCapacity=5,
                Siting=new("district","test.leycraft",[new("mana","mana",["mana-link"])]) }).ToArray(),
            Siting=new(1,[new("district","site","settlement","test.sanctum","test.authored")],
                [new("mana","test.mana",SitingResourceOrigin.UtilityNetwork,"world",path.Centerline.Points[0],10000,"game-service-units",new HashSet<string> { "attuned" },"test.authored")],
                [new("mana-link",path.Id,SitingNetworkMode.Utility,10000,"game-service-units","test.authored",Services:new HashSet<string> { "test.mana" })],[],[]) } };
    }
    private static async Task Cli(string cli,string plugin,WorldRequest accepted,WorldRequest rejected)
    {
        string root=Path.Combine(Path.GetTempPath(),"gfw-pack-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try
        {
            var list=await Run("list-catalogs","--catalog-plugin",plugin);
            Check(list.Code==0 && list.Output.Contains("test.arcane"),"CLI plugin discovery");
            Check((await Run("list-buildings")).Code==1,"CLI requires explicit catalog");
            Check((await Run("list-buildings","missing")).Code==1,"CLI rejects missing pack");
            string input=Path.Combine(root,"input.json"),output=Path.Combine(root,"world");
            await File.WriteAllTextAsync(input,JsonSerializer.Serialize(accepted,WorldJson.CreateOptions()));
            Check((await Run("generate-world",input,output)).Code==1,"missing plugin does not fall back to Modern");
            Check((await Run("generate-world",input,output,"--catalog-plugin",plugin)).Code==0,"CLI generates through external fixed policy");
            var before=await File.ReadAllBytesAsync(Path.Combine(output,"world.json"));
            await File.WriteAllTextAsync(input,JsonSerializer.Serialize(rejected,WorldJson.CreateOptions()));
            var failed=await Run("generate-world",input,output,"--catalog-plugin",plugin);
            var after=await File.ReadAllBytesAsync(Path.Combine(output,"world.json"));
            Check(failed.Code==1 && before.SequenceEqual(after),"plugin rejection preserves export");
            string audit=await File.ReadAllTextAsync(Path.Combine(root,".gameframework","operations","audit.jsonl"));
            Check(audit.Contains("world.catalog.succeeded") && audit.Contains("world.catalog.failed") && audit.Contains("world.generate.catalog.selected") && audit.Contains("world.generate.failed"),"catalog and plugin audit");
            async Task<(int Code,string Output)> Run(params string[] args)
            {
                var info=new ProcessStartInfo("dotnet") { WorkingDirectory=root,UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true };
                info.ArgumentList.Add(cli); foreach (var arg in args) info.ArgumentList.Add(arg);
                using var process=Process.Start(info)!; var stdout=process.StandardOutput.ReadToEndAsync(); var stderr=process.StandardError.ReadToEndAsync();
                using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(60));
                try { await process.WaitForExitAsync(timeout.Token); } catch (OperationCanceledException) { process.Kill(true); throw; }
                await stderr; return (process.ExitCode,await stdout);
            }
        }
        finally { Directory.Delete(root,true); }
    }
    private static async Task Rejected(DesertWorldGenerator engine,WorldRequest request,string code)
    {
        try { await engine.GenerateAsync(request); } catch (WorldGenerationException ex) when (ex.Issues.Any(i=>i.StartsWith(code,StringComparison.Ordinal))) { return; }
        throw new Exception("Expected catalog rejection: "+code);
    }
    private static void Reject(Action action,string code)
    { try { action(); } catch (ArgumentException ex) when (ex.Message.StartsWith(code,StringComparison.Ordinal)) { return; } throw new Exception("Expected "+code); }
    private static void Check(bool condition,string message) { if (!condition) throw new Exception("Catalog contracts: "+message); }
    private sealed class Decorated(ISettlementCatalogProvider inner,SettlementCatalogDescriptor descriptor) : ISettlementCatalogProvider
    {
        public SettlementCatalogDescriptor Descriptor => descriptor;
        public IReadOnlyList<BuildingDefinition> Structures => inner.Structures;
        public IReadOnlyList<FacilityDefinition> Facilities => inner.Facilities;
        public ISettlementCatalogPolicy Policy => inner.Policy;
        public ISettlementDemandPolicy Demand => inner.Demand;
        public IInfrastructureDesignStrategy Infrastructure => inner.Infrastructure;
    }
}
