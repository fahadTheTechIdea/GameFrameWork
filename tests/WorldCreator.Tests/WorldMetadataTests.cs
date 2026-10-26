using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using WorldCreator.Core;
using WorldCreator.Core.Metadata;
using WorldCreator.Catalogs.Modern;

internal static class WorldMetadataTests
{
    private static IReadOnlyDictionary<string,JsonElement> Props(object? value=null)=>value is null
        ? new Dictionary<string,JsonElement>() : JsonSerializer.Deserialize<Dictionary<string,JsonElement>>(JsonSerializer.Serialize(value))!;
    private static WorldMetadataArchetype Type(string id,string kind,MetadataPart[]? parts=null,MetadataConnectionRule[]? links=null,
        MetadataChildRequirement[]? requirements=null,bool connected=false,object? properties=null) =>
        new(id,kind,"Game-authored "+kind+" program",Props(properties),parts??[],links??[],requirements??[],connected);

    public static (WorldMetadataRecipe Recipe,WorldMetadataCatalog Catalog) Example(string name)
    {
        MetadataSpace space=new([],"none");double[]? origin=null; bool positions=false;
        string root="root";WorldMetadataArchetype[] types;
        var parameters=new Dictionary<string,int>(); MetadataExplicitConnection[] extra=[];
        switch(name)
        {
            case "town":
                parameters=new(){{"districts",3},{"sites",5}};
                types=[Type("root","settlement",[new("districts","district",0,"Distinct town districts","districts")],
                    [new("streets","street-network",MetadataConnectionPattern.Chain,"districts")],[new("district",1,12)],true),
                    Type("district","district",[new("sites","site",0,"Planned neighborhood sites","sites")],
                    [new("access","local-access",MetadataConnectionPattern.Chain,"sites")],[new("location",1,100)],true),
                    Type("site","location",properties:new{ gameplay_role="configurable-site", enabled=true })];break;
            case "dungeon":
                parameters=new(){{"floors",2},{"rooms",6}};
                types=[Type("root","dungeon",[new("floors","floor",0,"Exploration floors","floors")],
                    [new("stairs","floor-transition",MetadataConnectionPattern.Chain,"floors")],connected:true),
                    Type("floor","floor",[new("rooms","room",0,"Connected encounter rooms","rooms")],
                    [new("corridors","corridor",MetadataConnectionPattern.Chain,"rooms")],[new("room",2,64)],true),
                    Type("room","room",properties:new{ encounter_budget=3, visibility="unexplored" })];
                extra=[new("stairs-room-link","stairs","dungeon/floors-0/rooms-5","dungeon/floors-1/rooms-0")];break;
            case "platformer":
                parameters=new(){{"levels",4}};space=new(["x","y"],"meters");origin=[0,0];positions=true;
                types=[Type("root","campaign",[new("levels","level",0,"Authored level progression","levels",new([0,0],[200,0]))],
                    [new("progression","next-level",MetadataConnectionPattern.Chain,"levels",Bidirectional:false)],connected:true),
                    Type("level","level",[new("sections","section",3,"Side-scrolling sections",Layout:new([0,0],[40,0]))],
                    [new("jumps","jump-path",MetadataConnectionPattern.Chain,"sections",Bidirectional:false,Properties:Props(new{ minimum_jump_meters=2.5 }))],connected:true),
                    Type("section","section",properties:new{ camera="side-scrolling", theme="game-defined" })];break;
            case "space":
                parameters=new(){{"systems",2},{"planets",3}};space=new(["x","y","z"],"game-units");origin=[0,0,0];positions=true;
                types=[Type("root","universe",[new("systems","system",0,"Named systems","systems",new([100,0,0],[1000,0,100]))],
                    [new("lanes","hyperspace-lane",MetadataConnectionPattern.Chain,"systems")],connected:true),
                    Type("system","star-system",[new("planets","planet",0,"System destinations","planets",new([50,0,0],[50,0,0]))],
                    [new("routes","system-travel",MetadataConnectionPattern.Hub,"self","planets")],[new("planet",1,12)],true),
                    Type("planet","planet",properties:new{ exploration="unvisited", orbital_simulation="consumer-defined" })];break;
            default:throw new ArgumentException("Unknown fixture");
        }
        var catalog=new WorldMetadataCatalog(1,name+"-catalog",types);
        return (new(1,name,name+"-game",catalog.Id,root,name,MetadataCoverage.CompleteDeclaredScope,[],parameters,space,origin,positions,
            AdditionalConnections:extra),catalog);
    }

    public static async Task WriteExamplesAsync(string directory)
    {
        Directory.CreateDirectory(directory);
        foreach(string name in new[]{"town","dungeon","platformer","space"})
        {
            var fixture=Example(name);
            foreach(var item in new[]{("recipe",JsonSerializer.Serialize(fixture.Recipe,WorldJson.CreateOptions(true))),
                ("catalog",JsonSerializer.Serialize(fixture.Catalog,WorldJson.CreateOptions(true)))})
            {
                string file=Path.Combine(directory,name+"."+item.Item1+".json");
                await using var stream=new FileStream(file,FileMode.CreateNew,FileAccess.Write);
                await using var writer=new StreamWriter(stream);await writer.WriteAsync(item.Item2);
            }
        }
    }

    public static async Task RunAsync(string? cli=null)
    {
        var generator=new WorldMetadataGenerator();
        var expected=new Dictionary<string,(int Nodes,int Links)>{{"town",(19,14)},{"dungeon",(15,12)},{"platformer",(17,11)},{"space",(9,7)}};
        foreach(var (name,counts) in expected)
        {
            var f=Example(name);var world=generator.Generate(f.Recipe,f.Catalog);
            Check(world.Nodes.Count==counts.Nodes && world.Connections.Count==counts.Links,"exact "+name+" graph without mockups");
            Check(JsonSerializer.Serialize(world,WorldJson.CreateOptions())==JsonSerializer.Serialize(generator.Generate(f.Recipe,f.Catalog),WorldJson.CreateOptions()),"determinism "+name);
            var ids=world.Nodes.Select(n=>n.Id).ToHashSet();
            Check(ids.Count==world.Nodes.Count && world.Nodes.All(n=>n.ParentId is null || ids.Contains(n.ParentId)) &&
                world.Connections.All(l=>ids.Contains(l.FromId)&&ids.Contains(l.ToId)),"valid ownership/graph references "+name);
            Check(world.StructureDefinitions.Count==0,"no forced settlement/art dependency "+name);
            if(name=="platformer")
            {
                Check(world.Nodes.Single(n=>n.Id=="platformer/levels-3/sections-2").Position!.SequenceEqual(new[]{680d,0d}),"authored local line grammar composes positions");
                Check(world.Connections.All(l=>!l.Bidirectional)&&world.Connections.Any(l=>l.Properties.ContainsKey("minimum_jump_meters")),"one-way typed traversal properties");
            }
            if(name=="town" || name=="dungeon")Check(world.Nodes.All(n=>n.Position is null),"topological worlds need no made-up coordinates");
        }
        var town=Example("town");
        var pairedCatalog=new WorldMetadataCatalog(1,"graph-catalog",[
            Type("root","hub",[new("left","leaf",2,"Left destinations"),new("right","leaf",2,"Right destinations")],
                [new("pairs","paired-portals",MetadataConnectionPattern.Pairs,"left","right"),
                 new("routes","travel",MetadataConnectionPattern.AllPairs,"left","right")],connected:true),Type("leaf","destination")]);
        var pairedRecipe=town.Recipe with{CatalogId=pairedCatalog.Id};
        var paired=generator.Generate(pairedRecipe,pairedCatalog);
        Check(paired.Nodes.Count==5 && paired.Connections.Count(l=>l.Kind=="paired-portals")==2 &&
            paired.Connections.Count(l=>l.Kind=="travel")==4,"pair and cartesian grammars expand distinct typed connections");
        Reject(()=>generator.Generate(pairedRecipe with{MaximumConnections=5},pairedCatalog),"METADATA_CONNECTION_BUDGET");
        var unequal=pairedCatalog with{Archetypes=[pairedCatalog.Archetypes[0] with{Parts=[new("left","leaf",1,"Left"),new("right","leaf",2,"Right")]},pairedCatalog.Archetypes[1]]};
        Reject(()=>generator.Generate(pairedRecipe,unequal),"METADATA_PAIR_COUNT_MISMATCH");
        var cityRecipe=town.Recipe with { Scale="city",Coverage=MetadataCoverage.PartialScope,RemainingScopes=["outer-districts"],
            Parameters=new Dictionary<string,int>{{"districts",5},{"sites",5}} };
        var city=generator.Generate(cityRecipe,town.Catalog);
        Check(city.Nodes.Count==31 && city.Coverage==MetadataCoverage.PartialScope && city.RemainingScopes.Count==1,"scale parameters retain incomplete-city intent");
        Reject(()=>generator.Generate(cityRecipe with{RemainingScopes=[]},town.Catalog),"METADATA_RECIPE_OR_CATALOG_INVALID");
        Reject(()=>generator.Generate(town.Recipe with{MaximumNodes=5},town.Catalog),"METADATA_PART_COUNT_OR_NODE_BUDGET");
        Reject(()=>generator.Generate(town.Recipe with{MaximumConnections=1},town.Catalog),"METADATA_CONNECTION_BUDGET");
        Reject(()=>generator.Generate(town.Recipe with{Parameters=new Dictionary<string,int>()},town.Catalog),"METADATA_PARAMETER_MISSING");
        Reject(()=>generator.Generate(town.Recipe with{RootArchetypeId="missing"},town.Catalog),"METADATA_ROOT_UNKNOWN");
        Reject(()=>generator.Generate(town.Recipe,town.Catalog with{Archetypes=town.Catalog.Archetypes.Append(town.Catalog.Archetypes[0]).ToArray()}),"METADATA_ARCHETYPE_INVALID");
        var cycle=town.Catalog with{Archetypes=town.Catalog.Archetypes.Select(a=>a.Id=="site"?a with{Parts=[new("cycle","root",1,"invalid cycle")]}:a).ToArray()};
        Reject(()=>generator.Generate(town.Recipe,cycle),"METADATA_ARCHETYPE_CYCLE_OR_DEPTH");
        var disconnected=town.Catalog with{Archetypes=town.Catalog.Archetypes.Select(a=>a.Id=="district"?a with{Connections=[]}:a).ToArray()};
        Reject(()=>generator.Generate(town.Recipe,disconnected),"METADATA_CHILDREN_DISCONNECTED");
        Reject(()=>generator.Generate(town.Recipe with{Parameters=new Dictionary<string,int>{{"districts",0},{"sites",5}}},town.Catalog),"METADATA_CHILD_REQUIREMENT");
        var platform=Example("platformer");
        Reject(()=>generator.Generate(platform.Recipe with{Origin=null},platform.Catalog),"METADATA_ORIGIN_INVALID");
        Reject(()=>generator.Generate(town.Recipe with{AdditionalConnections=[new("bad","portal","missing","town")]},town.Catalog),"METADATA_EXPLICIT_CONNECTION_INVALID");
        using(var cancel=new CancellationTokenSource()) {cancel.Cancel();try{generator.Generate(town.Recipe,town.Catalog,cancel.Token);throw new Exception("Cancellation absent");}catch(OperationCanceledException){}}
        var linked=town.Catalog with{Archetypes=town.Catalog.Archetypes.Select(a=>a.Id=="site"?a with{Structure=new(ModernCatalog.Selection,"warehouse","small")}:a).ToArray()};
        Reject(()=>generator.Generate(town.Recipe,linked),"METADATA_STRUCTURE_CATALOG_REQUIRED");
        var referenced=new WorldMetadataGenerator(TestCatalogs.Registry).Generate(town.Recipe,linked);
        Check(referenced.StructureDefinitions.Count==1 && referenced.Nodes.Count(n=>n.StructureKey is not null)==15 &&
            referenced.StructureDefinitions.Single().Value.Variant.WidthMeters==12,"existing catalog dimensions/provenance reused once, no duplicate structure facts");
        var invalidReference=linked with{Archetypes=linked.Archetypes.Select(a=>a.Structure is null?a:a with{Structure=a.Structure with{Catalog=null!}}).ToArray()};
        Reject(()=>generator.Generate(town.Recipe,invalidReference),"METADATA_STRUCTURE_REFERENCE_INVALID");

        string root=Path.Combine(Path.GetTempPath(),"worldcreator-metadata-tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        try
        {
            await WriteExamplesAsync(root);
            string recipe=Path.Combine(root,"town.recipe.json"),catalog=Path.Combine(root,"town.catalog.json"),output=Path.Combine(root,"world");
            var exported=await generator.GenerateFilesAsync(recipe,catalog,output);
            var restored=await WorldMetadataGenerator.ReadAsync<WorldMetadataDocument>(Path.Combine(output,"world-metadata.json"));
            Check(restored.Nodes.Count==19 && File.Exists(Path.Combine(output,"review.html")),"versioned JSON and review export");
            Check(File.Exists(Path.Combine(output,"schematic.js")) && File.Exists(Path.Combine(output,"schematic.html")),"JavaScript schematic packaged atomically with metadata");
            var dangerousCatalog=town.Catalog with{Archetypes=town.Catalog.Archetypes.Select(a=>a with{Purpose="</script><script>alert('bad')</script>"}).ToArray()};
            var dangerousWorld=generator.Generate(town.Recipe,dangerousCatalog);
            string safeDirectory=Path.Combine(root,"escaped");Directory.CreateDirectory(safeDirectory);
            await new WorldMetadataSchematicExporter().WriteAsync(dangerousWorld,safeDirectory);
            string safeHtml=await File.ReadAllTextAsync(Path.Combine(safeDirectory,"schematic.html"));
            Check(!safeHtml.Contains("</script><script>alert",StringComparison.Ordinal) && !safeHtml.Contains("__WORLD_METADATA_JSON__",StringComparison.Ordinal),"embedded schematic data escapes script delimiters");
            var resume=generator.Generate(await WorldMetadataGenerator.ReadAsync<WorldMetadataRecipe>(Path.Combine(output,"recipe.json")),
                await WorldMetadataGenerator.ReadAsync<WorldMetadataCatalog>(Path.Combine(output,"catalog.json")));
            Check(JsonSerializer.Serialize(exported)==JsonSerializer.Serialize(resume),"saved recipes/catalogs reproduce exact metadata");
            string hash=Hash(Path.Combine(output,"world-metadata.json"));
            try{await generator.GenerateFilesAsync(recipe,catalog,output);throw new Exception("Overwrite allowed");}catch(IOException){}
            using(var cancel=new CancellationTokenSource())
            {cancel.Cancel();try{await generator.GenerateFilesAsync(recipe,catalog,Path.Combine(root,"cancelled"),cancel.Token);throw new Exception("Cancellation absent");}catch(OperationCanceledException){}}
            Check(Hash(Path.Combine(output,"world-metadata.json"))==hash&&!Directory.Exists(Path.Combine(root,"cancelled")),"failure/cancellation preserve prior output");
            string invalid=Path.Combine(root,"invalid.recipe.json");
            var invalidRecipe=System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllTextAsync(recipe))!;
            invalidRecipe["UnknownSetting"]=true;
            await File.WriteAllTextAsync(invalid,invalidRecipe.ToJsonString());
            try{await generator.GenerateFilesAsync(invalid,catalog,Path.Combine(root,"invalid"));throw new Exception("Unknown field accepted");}catch(JsonException){}
            Check(!Directory.Exists(Path.Combine(root,"invalid")),"unknown recipe fields reject before publication");
            if(cli is not null)
            {
                string unrelatedCatalogs=Path.Combine(root,".gameframework/catalogs");Directory.CreateDirectory(unrelatedCatalogs);
                await File.WriteAllTextAsync(Path.Combine(unrelatedCatalogs,"unrelated.json"),"invalid unrelated settlement manifest");
                foreach(string name in expected.Keys)
                    Check(await Run("generate-metadata",Path.Combine(root,name+".recipe.json"),Path.Combine(root,name+".catalog.json"),Path.Combine(root,"cli-"+name))==0,"fresh mockup-free CLI "+name);
                Check(await Run("generate-metadata",recipe,catalog,Path.Combine(root,"cli-town"))==1,"CLI rejects existing output");
                string audit=await File.ReadAllTextAsync(Path.Combine(root,".gameframework/operations/audit.jsonl"));
                Check(audit.Contains("world.metadata.succeeded")&&audit.Contains("world.metadata.failed")&&!Directory.Exists(Path.Combine(root,".gamearts")),"metadata audit and zero GameArts setup dependency");
                async Task<int> Run(params string[] arguments)
                {
                    var info=new ProcessStartInfo("dotnet"){WorkingDirectory=root,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true};
                    info.ArgumentList.Add(cli);foreach(var argument in arguments)info.ArgumentList.Add(argument);
                    using var process=Process.Start(info)!;var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();
                    using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(60));
                    try{await process.WaitForExitAsync(timeout.Token);}catch(OperationCanceledException){process.Kill(true);throw;}
                    _=await stdout;_=await stderr;return process.ExitCode;
                }
            }
        }
        finally
        {if(Path.GetFullPath(root).StartsWith(Path.GetFullPath(Path.GetTempPath()),StringComparison.OrdinalIgnoreCase)&&Path.GetFileName(root).StartsWith("worldcreator-metadata-tests-",StringComparison.Ordinal))Directory.Delete(root,true);}
        Console.WriteLine("PASS: mockup-free town, dungeon, platformer and space metadata; exact hierarchy/counts/connections/positions, reusable catalogs, scaling/partial scope, constraints, negatives, cancellation, JSON persistence and actual CLI/audit.");
    }
    private static string Hash(string file)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
    private static void Reject(Action action,string code){try{action();}catch(ArgumentException ex)when(ex.Message.StartsWith(code,StringComparison.Ordinal)){return;}throw new Exception("Expected "+code);}
    private static void Check(bool ok,string message){if(!ok)throw new Exception("Metadata: "+message);}
}
