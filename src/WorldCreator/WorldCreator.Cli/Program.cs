using WorldCreator.Catalogs.Modern;
using WorldCreator.Core;
using WorldCreator.Core.Operations;
using WorldCreator.Core.Planning;
using WorldCreator.Worlds;
using WorldCreator.Godot;
using System.Text.Json;
using WorldCreator.Core.Capture;
using WorldCreator.Cli;
using WorldCreator.Core.Metadata;
using WorldCreator.Core.Cities;

if (args.Length == 0 || args[0] is "--help" or "-h")
{
    Console.WriteLine("Usage: dotnet run --project src/WorldCreator/WorldCreator.Cli -- generate-world <request.json> [output-dir] [--capture <capture.json> --asset-root <trusted-root>]");
    Console.WriteLine("       dummy-village [output-dir]");
    Console.WriteLine("       verify-world <world.json> <manifest-directory> <trusted-asset-root>");
    Console.WriteLine("       list-catalogs [--catalog-plugin <trusted-assembly.dll>] ");
    Console.WriteLine("       list-buildings <catalog-id> [category]");
    Console.WriteLine("       save-catalog-content <manifest.json> <catalog-directory> [--replace]");
    Console.WriteLine("       --catalog-content <manifest.json> extends a source catalog before composition.");
    Console.WriteLine("       list-facilities <catalog-id> [--catalog-composition <manifest.json>] ");
    Console.WriteLine("       --catalog-composition <manifest.json> registers explicit catalog imports for listing/generation.");
    Console.WriteLine("       review-capture <capture.json> <trusted-image-root> <new-output-dir>");
    Console.WriteLine("       compile-capture <capture.json> <trusted-image-root> <new-output-dir> (calibrated geometry draft)");
    Console.WriteLine("       capture-schema (current capture JSON Schema)");
    Console.WriteLine("       structure-pipeline plan <world.json> <new-library.json>");
    Console.WriteLine("       generate-metadata <recipe.json> <catalog.json> <new-output-dir> (mockup-free, engine-independent)");
    Console.WriteLine("       generate-city <request.json> <catalog.json> <new-output-dir> (engine procedural streets/blocks/parcels/structures)");
    Console.WriteLine("       verify-city <package-directory> (hashes, engine build, reproduced geometry and viewer)");
    Console.WriteLine("       structure-pipeline build <world.json> <library.json> <trusted-asset-root> <new-output-dir>");
    Console.WriteLine("verify-world checks dependency integrity; lifecycle approval and gameplay readiness require separate gates.");
    return;
}
var operation = args[0] switch { "verify-city"=>"world.city.verify", "generate-city" => "world.city", "generate-metadata" => "world.metadata", "structure-pipeline" => "world.structure-pipeline", "verify-world" => "world.verify", "review-capture" or "compile-capture" or "capture-schema" => "world.capture", "list-buildings" or "list-catalogs" or "list-facilities" or "save-catalog-content" => "world.catalog", _ => "world.generate" };
var id = Guid.NewGuid().ToString("N");
var eventRoot = Path.Combine(Environment.CurrentDirectory, ".gameframework", "operations");
var targetName = args[0] == "generate-world" ? args.Length > 2 ? Path.GetFileName(args[2]) : "world"
    : args.Length > 1 ? Path.GetFileName(args[1]) : null;
var reporter = new WorldOperationReporter(
    new JsonLineOperationStore(Path.Combine(eventRoot, "log.jsonl")),
    new JsonLineOperationStore(Path.Combine(eventRoot, "audit.jsonl")),
    new ConsoleAndFileNotificationService(new JsonLineOperationStore(Path.Combine(eventRoot, "notifications.jsonl"))));
OperationEvent Entry(string eventId, OperationSeverity severity, string message, string outcome) =>
    new(eventId, DateTimeOffset.UtcNow, severity, "WorldCreator.Cli", operation, id, message, targetName, "local-cli", outcome);
using var cancellation = new CancellationTokenSource();
ConsoleCancelEventHandler cancelHandler = (_, e) => { e.Cancel = true; cancellation.Cancel(); };
Console.CancelKeyPress += cancelHandler;
var loadedCatalogPlugins=new List<SettlementCatalogPlugin>();
var compositionDefinitions=new List<CatalogCompositionDefinition>();
var contentFiles=new List<string>();
try
{
    var commandArguments=args.ToList();
    string? captureInput = null, captureAssetRoot = null;
    for (int index=1;index<commandArguments.Count;)
    {
        if (commandArguments[index] is not ("--catalog-plugin" or "--catalog-composition" or "--catalog-content")) { index++; continue; }
        if (index+1>=commandArguments.Count) throw new ArgumentException("Expected a value for "+commandArguments[index]);
        if (commandArguments[index]=="--catalog-content") contentFiles.Add(Path.GetFullPath(commandArguments[index+1]));
        else if (commandArguments[index]=="--catalog-composition")
            compositionDefinitions.AddRange(SettlementCatalogComposition.ReadManifest(commandArguments[index+1]).Catalogs);
        else loadedCatalogPlugins.Add(new SettlementCatalogPlugin(commandArguments[index+1],cancellation.Token));
        commandArguments.RemoveRange(index,2);
    }
    if (commandArguments[0]=="generate-world")
    {
        for (int index=1;index<commandArguments.Count;)
        {
            var flag=commandArguments[index];
            if (flag is not ("--capture" or "--asset-root")) { index++; continue; }
            if (index+1>=commandArguments.Count) throw new ArgumentException("Capture options require a value.");
            if (flag=="--capture") { if(captureInput is not null) throw new ArgumentException("Duplicate --capture."); captureInput=commandArguments[index+1]; }
            else { if(captureAssetRoot is not null) throw new ArgumentException("Duplicate --asset-root."); captureAssetRoot=commandArguments[index+1]; }
            commandArguments.RemoveRange(index,2);
        }
        if ((captureInput is null)!=(captureAssetRoot is null)) throw new ArgumentException("--capture and --asset-root must be supplied together.");
    }
    args=commandArguments.ToArray();
    if(args[0]=="verify-city")
    {
        if(args.Length!=2)throw new ArgumentException("Expected verify-city <package-directory>.");
        string cityCatalogPath=Path.Combine(args[1],"catalog.json");
        if((File.GetAttributes(args[1])&FileAttributes.ReparsePoint)!=0 || (File.GetAttributes(cityCatalogPath)&FileAttributes.ReparsePoint)!=0)throw new ArgumentException("CITY_PACKAGE_LINKED_INPUT");
        var cityCatalog=await WorldMetadataGenerator.ReadAsync<ProceduralCityCatalog>(cityCatalogPath,cancellation.Token);
        if(cityCatalog.Structures is not null && cityCatalog.Structures.All(t=>t is not null && t.CatalogReference is null) &&
            loadedCatalogPlugins.Count==0 && compositionDefinitions.Count==0 && contentFiles.Count==0)
        {await VerifyCityAsync(null);return;}
    }
    if(args[0]=="generate-city")
    {
        if(args.Length!=4)throw new ArgumentException("Expected generate-city <request.json> <catalog.json> <new-output-dir>.");
        var cityCatalog=await WorldMetadataGenerator.ReadAsync<ProceduralCityCatalog>(args[2],cancellation.Token);
        if(cityCatalog.Structures is not null && cityCatalog.Structures.All(t=>t is not null && t.CatalogReference is null) &&
            loadedCatalogPlugins.Count==0 && compositionDefinitions.Count==0 && contentFiles.Count==0)
        {await GenerateCityAsync(null);return;}
    }
    if (args[0] == "generate-metadata")
    {
        if (args.Length != 4) throw new ArgumentException("Expected generate-metadata <recipe.json> <catalog.json> <new-output-dir>.");
        var metadataCatalog = await WorldMetadataGenerator.ReadAsync<WorldMetadataCatalog>(args[2], cancellation.Token);
        if (metadataCatalog.Archetypes is not null && metadataCatalog.Archetypes.All(a => a is not null && a.Structure is null) &&
            loadedCatalogPlugins.Count == 0 && compositionDefinitions.Count == 0 && contentFiles.Count == 0)
        {
            // Generic metadata must not depend on unrelated settlement manifests in the workspace.
            await GenerateMetadataAsync(null);
            return;
        }
    }
    if (args[0] == "structure-pipeline")
    {
        await reporter.ReportAsync(Entry("world.structure-pipeline.started", OperationSeverity.Information, "Structure pipeline started.", "started"));
        string summary = await StructureFirstCommand.RunAsync(args, cancellation.Token);
        await reporter.ReportAsync(Entry("world.structure-pipeline.succeeded", OperationSeverity.Information, summary, "succeeded"), notify: true);
        Console.WriteLine(summary);
        return;
    }
    if (args[0] == "capture-schema")
    {
        if (args.Length != 1) throw new ArgumentException("Expected capture-schema.");
        Console.WriteLine(MockupCaptureJson.Schema().ToJsonString(MockupCaptureJson.Options()));
        return;
    }
    if (args[0] is "review-capture" or "compile-capture")
    {
        if (args.Length != 4) throw new ArgumentException("Expected capture command <capture.json> <trusted-image-root> <new-output-dir>.");
        await reporter.ReportAsync(Entry("world.capture.started", OperationSeverity.Information, "Source-pixel capture verification started.", "started"));
        if (new FileInfo(args[1]).Length > 16777216) throw new ArgumentException("CAPTURE_DOCUMENT_SIZE_LIMIT");
        await using var captureStream = File.OpenRead(args[1]);
        var capture = await JsonSerializer.DeserializeAsync<MockupCaptureDocument>(captureStream, MockupCaptureJson.Options(), cancellation.Token)
            ?? throw new ArgumentException("CAPTURE_DOCUMENT_REQUIRED");
        var bytes = await MockupCapturePipeline.ReadSourceAsync(capture, args[2], cancellation.Token);
        var report = MockupCaptureValidator.Validate(capture, cancellation.Token);
        var overlay = MockupCapturePipeline.Overlay(capture, bytes, cancellation.Token);
        var html = MockupCapturePipeline.ReviewHtml(capture, bytes, cancellation.Token);
        var prompts = MockupCapturePipeline.Prompts(capture, cancellation.Token);
        var vegetation = MockupCapturePipeline.VegetationDraft(capture, cancellation.Token);
        var geometry = args[0] == "compile-capture" ? MockupCapturePipeline.GeometryDraft(capture, cancellation.Token) : null;
        var destination = Path.GetFullPath(args[3]);
        if (Directory.Exists(destination) || File.Exists(destination)) throw new ArgumentException("CAPTURE_OUTPUT_EXISTS");
        var parent = Path.GetDirectoryName(destination) ?? throw new ArgumentException("CAPTURE_OUTPUT_INVALID");
        Directory.CreateDirectory(parent);
        var staging = Path.Combine(parent, ".capture-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(staging, "capture.json"), JsonSerializer.Serialize(capture, MockupCaptureJson.Options()), cancellation.Token);
            await File.WriteAllTextAsync(Path.Combine(staging, "report.json"), JsonSerializer.Serialize(report, MockupCaptureJson.Options()), cancellation.Token);
            await File.WriteAllTextAsync(Path.Combine(staging, "overlay.svg"), overlay, cancellation.Token);
            await File.WriteAllTextAsync(Path.Combine(staging, "index.html"), html, cancellation.Token);
            await File.WriteAllTextAsync(Path.Combine(staging, "prompts.json"), JsonSerializer.Serialize(prompts, MockupCaptureJson.Options()), cancellation.Token);
            if (vegetation is not null) await File.WriteAllTextAsync(Path.Combine(staging, "vegetation-layout.json"), JsonSerializer.Serialize(vegetation, MockupCaptureJson.Options()), cancellation.Token);
            if (geometry is not null) await File.WriteAllTextAsync(Path.Combine(staging, "geometry-draft.json"), JsonSerializer.Serialize(geometry, MockupCaptureJson.Options()), cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            Directory.Move(staging, destination);
        }
        finally { if (Directory.Exists(staging)) Directory.Delete(staging, true); }
        await reporter.ReportAsync(Entry("world.capture.succeeded", OperationSeverity.Information,
            "Source verified; capture overlay and prompt drafts exported. No art approval or production world granted.", "succeeded"), notify: true);
        Console.WriteLine($"Capture review exported to {destination}; {report.Crossings.Count(c => c.Kind == CaptureJunctionKind.Unresolved)} unresolved projected crossings.");
        return;
    }
    var projectCatalogDirectory=Path.Combine(Environment.CurrentDirectory,".gameframework","catalogs");
    if (Directory.Exists(projectCatalogDirectory)) contentFiles.AddRange(Directory.GetFiles(projectCatalogDirectory,"*.json",SearchOption.TopDirectoryOnly));
    var contents=contentFiles.Select(Path.GetFullPath).Distinct(OperatingSystem.IsWindows()?StringComparer.OrdinalIgnoreCase:StringComparer.Ordinal)
        .Order(StringComparer.Ordinal).Select(CatalogContentAuthoring.ReadManifest).ToList();
    CatalogContentManifest? savedContent=null;
    if (args[0]=="save-catalog-content")
    {
        if (args.Length is <3 or >4 || args.Length==4 && args[3]!="--replace") throw new ArgumentException("Expected save-catalog-content <manifest> <directory> [--replace].");
        savedContent=CatalogContentAuthoring.ReadManifest(args[1]);
        contents.RemoveAll(m=>m.Id==savedContent.Id && m.CatalogId==savedContent.CatalogId);
        contents.Add(savedContent);
    }
    var authoredProviders=CatalogContentAuthoring.Apply(new ISettlementCatalogProvider[] { new ModernCatalog() }.Concat(loadedCatalogPlugins.SelectMany(p=>p.Providers)),contents,cancellation.Token);
    var catalogProviders=SettlementCatalogComposition.Register(authoredProviders,compositionDefinitions,cancellation.Token).ToArray();
    var catalogRegistry=new SettlementCatalogRegistry(catalogProviders,cancellation.Token);
    if(args[0]=="generate-city"){await GenerateCityAsync(catalogRegistry);return;}
    if(args[0]=="verify-city"){await VerifyCityAsync(catalogRegistry);return;}
    if(args[0]=="generate-metadata")
    {
        await GenerateMetadataAsync(catalogRegistry);
        return;
    }
    if (savedContent is not null)
    {
        var savedPath=await CatalogContentAuthoring.SaveManifestAsync(savedContent,args[2],args.Length==4,cancellation.Token);
        await reporter.ReportAsync(Entry("world.catalog.content.saved",OperationSeverity.Information,
            $"Saved reusable catalog content {savedContent.Id} for {savedContent.CatalogId}.","succeeded"));
        Console.WriteLine(savedPath);
        return;
    }
    foreach (var content in contents)
        await reporter.ReportAsync(Entry("world.catalog.content.registered",OperationSeverity.Information,
            $"Content {content.Id} for {content.CatalogId}; sha256 {catalogRegistry.Catalogs.Single(d=>d.Id==content.CatalogId).AuthoredContentDigests![content.Id]}.","registered"));
    if (args[0]=="list-catalogs")
    {
        if (args.Length!=1) throw new ArgumentException("Expected list-catalogs");
        Console.WriteLine(JsonSerializer.Serialize(catalogRegistry.Catalogs,WorldJson.CreateOptions()));
        await reporter.ReportAsync(Entry("world.catalog.succeeded",OperationSeverity.Information,"Registered catalogs listed.","succeeded"));
        return;
    }
    if (args[0]=="list-facilities")
    {
        if (args.Length!=2) throw new ArgumentException("Expected list-facilities <catalog-id>.");
        var descriptor=catalogRegistry.Catalogs.SingleOrDefault(d=>d.Id==args[1]) ?? throw new ArgumentException("CATALOG_NOT_REGISTERED:"+args[1]);
        var catalog=catalogRegistry.Resolve(new(args[1],descriptor.Capabilities));
        Console.WriteLine(JsonSerializer.Serialize(new { Catalog=catalog.Provenance, Facilities=catalog.Facilities },WorldJson.CreateOptions()));
        await reporter.ReportAsync(Entry("world.catalog.succeeded",OperationSeverity.Information,"Facility catalog listed.","succeeded"));
        return;
    }
    if (args[0] == "list-buildings")
    {
        if (args.Length is <2 or >3) throw new ArgumentException("Expected list-buildings <catalog-id> [category].");
        var descriptor=catalogRegistry.Catalogs.SingleOrDefault(d=>d.Id==args[1]) ?? throw new ArgumentException("CATALOG_NOT_REGISTERED:"+args[1]);
        var catalog=catalogRegistry.Resolve(new(args[1],descriptor.Capabilities));
        string? selected=args.Length==3?args[2]:null;
        if (selected is not null && !descriptor.Vocabulary.Categories.Contains(selected)) throw new ArgumentException("Unknown building category.");
        await reporter.ReportAsync(Entry("world.catalog.started",OperationSeverity.Information,"Building catalog read started.","started"));
        var definitions = catalog.Structures.Where(d => selected is null || d.Category == selected)
            .OrderBy(d => d.Id,StringComparer.Ordinal).ToArray();
        var payload = new { Catalog=catalog.Provenance, FamilyCount = definitions.Length,
            VariantCount = definitions.Sum(d => d.SizeVariants?.Count ?? 0),
            Categories = definitions.Select(d => d.Category).Distinct().Order().ToArray(), Definitions = definitions };
        cancellation.Token.ThrowIfCancellationRequested();
        Console.WriteLine(JsonSerializer.Serialize(payload,WorldJson.CreateOptions()));
        await reporter.ReportAsync(Entry("world.catalog.succeeded",OperationSeverity.Information,
            $"Building catalog read ({payload.FamilyCount} families, {payload.VariantCount} variants).","succeeded"));
        return;
    }
    if (args[0] == "verify-world")
    {
        if (args.Length != 4)
            throw new ArgumentException("Expected verify-world <world.json> <manifest-directory> <trusted-asset-root>.");
        await reporter.ReportAsync(Entry("world.verify.started", OperationSeverity.Information,
            "Portable world integrity audit started.", "started"));
        var verified = await new PortableWorldPackageVerifier(new FileSystemArtRegistry(args[2]))
            .VerifyAsync(args[1], args[3], cancellation.Token);
        await reporter.ReportAsync(Entry(verified.Valid ? "world.verify.succeeded" : "world.verify.failed",
            verified.Valid ? OperationSeverity.Information : OperationSeverity.Warning,
            verified.Valid ? "Portable world dependency integrity verified."
                : $"Portable world integrity audit rejected ({verified.Issues.Count} issues).",
            verified.Valid ? "succeeded" : "failed"), notify: true);
        Console.WriteLine(JsonSerializer.Serialize(verified));
        Environment.ExitCode = verified.Valid ? 0 : 1;
        return;
    }
    if (args[0] == "dummy-village")
    {
        if (args.Length > 2) throw new ArgumentException("Expected dummy-village [output-dir].");
        var destination = args.Length > 1 ? args[1] : "generated/dummy-village";
        await reporter.ReportAsync(Entry("world.generate.started", OperationSeverity.Information,
            "Simulation village export started.", "started"));
        await new DummyVillageAssembler().ExportAsync(destination, cancellation.Token);
        await reporter.ReportAsync(Entry("world.generate.succeeded", OperationSeverity.Information,
            "Simulation village exported.", "succeeded"), notify: true);
        Console.WriteLine($"Generated test Godot village at {Path.GetFullPath(destination)}");
        return;
    }
    if (args[0] != "generate-world") throw new ArgumentException("Supported: generate-world, dummy-village, verify-world, list-catalogs, list-buildings, list-facilities, save-catalog-content, review-capture or compile-capture.");
    if (args.Length is < 2 or > 3) throw new ArgumentException("Expected generate-world <request.json> [output-dir].");
    var output = args.Length > 2 ? args[2] : "generated/world";
    await reporter.ReportAsync(Entry("world.generate.started", OperationSeverity.Information, "Mockup-driven world generation started.", "started"));
    await using var requestStream = File.OpenRead(args[1]);
    byte[]? captureSourcePng = null;
    var request = await JsonSerializer.DeserializeAsync<WorldRequest>(requestStream, WorldJson.CreateOptions(),cancellation.Token)
        ?? throw new ArgumentException("World generation request is empty.");
    if (captureInput is not null)
    {
        if (new FileInfo(captureInput).Length>16777216) throw new ArgumentException("CAPTURE_DOCUMENT_SIZE_LIMIT");
        await using var captureStream = File.OpenRead(captureInput);
        var capture = await JsonSerializer.DeserializeAsync<MockupCaptureDocument>(captureStream,MockupCaptureJson.Options(),cancellation.Token)
            ?? throw new ArgumentException("CAPTURE_DOCUMENT_REQUIRED");
        captureSourcePng = await MockupCapturePipeline.ReadSourceAsync(capture,captureAssetRoot!,cancellation.Token);
        request=request with { Design=MockupCapturePipeline.ApplyToDesign(capture,request.Design ?? throw new ArgumentException("MOCKUP_DESIGN_REQUIRED"),cancellation.Token) };
        await reporter.ReportAsync(Entry("world.generate.capture.applied",OperationSeverity.Information,
            $"Verified capture {capture.Source.MockupId}; source {capture.Source.Sha256}; scope {capture.CompilationScope?.Id ?? "complete"}.","applied"));
    }
    var world = await new DesertWorldGenerator(catalogRegistry).GenerateAsync(request, cancellation.Token);
    await reporter.ReportAsync(Entry("world.generate.catalog.selected",OperationSeverity.Information,
        $"Catalog {world.Catalog!.Id}; policy {world.Catalog.PolicyId}; content {world.Catalog.ContentSha256}; capabilities {string.Join(',',world.Catalog.EnabledCapabilities.Order(StringComparer.Ordinal))}.","selected"));
    await reporter.ReportAsync(Entry("world.generate.siting.passed",OperationSeverity.Information,"Contextual siting rules and capacity evaluated.","passed"));
    if(world.Request.Design!.SurfaceClearances is {Count:>0} spacing)
        await reporter.ReportAsync(Entry("world.generate.surface-clearance.passed",OperationSeverity.Information,
            $"Validated {spacing.Count} authored surface-spacing rules against continuous road/sport/building geometry and fixed tree bases.","passed"));
    var palette=catalogProviders.Where(p=>p.Descriptor.Id==world.Catalog!.Id).OfType<IWorldPlanPalette>().SingleOrDefault();
    var planRenderer=new WorldPlanSvgRenderer(palette);
    var capturePreview=captureSourcePng is null?null:new CaptureWorldPreviewRenderer(planRenderer).Render(world,captureSourcePng,cancellation.Token);
    var captureDetails=captureSourcePng is null?null:MockupCapturePipeline.ReviewHtml(world.Request.Design!.Capture!,captureSourcePng,cancellation.Token);
    var capturePlants=captureSourcePng is null?null:MockupCapturePipeline.VegetationDraft(world.Request.Design!.Capture!,cancellation.Token);
    await new GodotWorldExporter(planRenderer).ExportAsync(world, output, cancellation.Token);
    if(captureSourcePng is not null)
    {
        await File.WriteAllTextAsync(Path.Combine(output,"capture-review.html"),capturePreview,cancellation.Token);
        await File.WriteAllTextAsync(Path.Combine(output,"source-details.html"),captureDetails,cancellation.Token);
        if(capturePlants is not null)await File.WriteAllTextAsync(Path.Combine(output,"vegetation-layout.json"),JsonSerializer.Serialize(capturePlants,MockupCaptureJson.Options()),cancellation.Token);
    }
    await reporter.ReportAsync(Entry("world.generate.succeeded", OperationSeverity.Information, "Mockup-driven world data and skeleton scene exported.", "succeeded"), notify: true);
    Console.WriteLine($"Generated {world.Width}x{world.Height} mockup-driven world at {Path.GetFullPath(output)}");
}
catch (Exception ex) when (ex is not OutOfMemoryException)
{
    bool cancelled = ex is OperationCanceledException && cancellation.IsCancellationRequested;
    var failure = Entry(operation + (cancelled ? ".cancelled" : ".failed"),
        cancelled ? OperationSeverity.Warning : OperationSeverity.Error,
        $"World operation {(cancelled ? "cancelled" : "failed")} ({ex.GetType().Name}).",
        cancelled ? "cancelled" : "failed");
    try { await reporter.ReportAsync(failure, notify: true); }
    catch (Exception reportingException)
    {
        Console.Error.WriteLine($"Operational reporting also failed: {reportingException.GetType().Name}");
    }
    Console.Error.WriteLine($"World operation failed: {ex.Message}");
    Environment.ExitCode = cancelled ? 130 : 1;
}
finally { Console.CancelKeyPress -= cancelHandler;
    foreach (var plugin in loadedCatalogPlugins) plugin.Dispose(); }

async Task GenerateMetadataAsync(ISettlementCatalogRegistry? registry)
{
    await reporter.ReportAsync(Entry("world.metadata.started", OperationSeverity.Information, "Metadata world generation started.", "started"));
    var metadata = await new WorldMetadataGenerator(registry).GenerateFilesAsync(args[1], args[2], args[3], cancellation.Token);
    string summary = $"Generated {metadata.Nodes.Count} metadata nodes and {metadata.Connections.Count} connections; scope: {metadata.Coverage}.";
    await reporter.ReportAsync(Entry("world.metadata.succeeded", OperationSeverity.Information, summary, "succeeded"), notify: true);
    Console.WriteLine(summary);
}

async Task GenerateCityAsync(ISettlementCatalogRegistry? registry)
{
    await reporter.ReportAsync(Entry("world.city.started",OperationSeverity.Information,"Procedural city generation started.","started"));
    var city=await ProceduralCityCommand.RunAsync(args,registry,cancellation.Token);
    string summary=$"Generated procedural city: {city.Districts.Count} districts, {city.Streets.Count} streets, {city.Blocks.Count} blocks, {city.Parcels.Count} parcels, {city.Structures.Count} structure instances; connected street graph.";
    await reporter.ReportAsync(Entry("world.city.succeeded",OperationSeverity.Information,summary,"succeeded"),notify:true);
    Console.WriteLine(summary);
}

async Task VerifyCityAsync(ISettlementCatalogRegistry? registry)
{
    await reporter.ReportAsync(Entry("world.city.verify.started",OperationSeverity.Information,"City package verification started.","started"));
    var result=await ProceduralCityCommand.VerifyAsync(args[1],registry,cancellation.Token);
    if(!result.Valid)throw new ArgumentException("CITY_PACKAGE_INVALID:"+string.Join(',',result.Issues));
    await reporter.ReportAsync(Entry("world.city.verify.succeeded",OperationSeverity.Information,"City package hashes, engine build, generated geometry and viewer verified.","succeeded"),notify:true);
    Console.WriteLine("City package verified: file integrity, matching engine build, reproducible geometry and derived outputs.");
}
