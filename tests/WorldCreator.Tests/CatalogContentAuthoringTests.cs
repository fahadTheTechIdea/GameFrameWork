using System.Diagnostics;
using System.Text.Json;
using WorldCreator.Catalogs.Modern;
using WorldCreator.Core;
using WorldCreator.Core.Planning;
using WorldCreator.Worlds;

internal static class CatalogContentAuthoringTests
{
    internal static CatalogContentManifest Content()
    {
        var modern = new ModernCatalog();
        var office = modern.Structures.Single(d => d.Id == "office-building");
        var added = office with { Id = "user.garden-office", ServiceCapacity = 1, AutomaticDemand = false,
            SizeVariants = [new("compact", 6, 8, 4, 1, 1)], Composition = new(StructureRepresentation.SingleStructure,
                "one small garden office building", "synthetic user-request authoring fixture", [new("workspace", CatalogPartKind.Interior, [])]) };
        FacilityFrontageGrammar layout = new(new(4, 2, 0, 0, 0, 0), 8, 8, 8, 4, 8, 8);
        FacilityProgramPart Part(string role, string id, int count = 1) => new(role, id,
            modern.Structures.Single(d => d.Id == id).SizeVariants!.OrderBy(v => v.WidthMeters * v.DepthMeters).ThenBy(v => v.Id, StringComparer.Ordinal).First().Id,
            "user-authored " + role, count);
        return new(1, "user.requested-compounds", "modern", "test.requested-parent-parts", [added], [
            // Deliberately ordered before its dependencies to exercise topological compilation.
            new("user.learning-and-care", "Learning and care site", "test.combined-program", layout, [],
                [new("learning", "user.school-program", "teaching precinct"), new("care", "user.care-program", "care precinct")]),
            new("user.school-program", "Requested school and library", "test.school-request", layout,
                [Part("teaching", "primary-school"), Part("library", "library")], []),
            new("user.care-program", "Requested care site", "test.care-request", layout,
                [Part("care", "clinic", 2)], []),
            new("user.home-program", "Requested home with two garden offices", "test.home-request", layout,
                [Part("home", "bungalow"), new("garden-office", added.Id, "compact", "requested detached garden office", 2)], [])
        ], []);
    }

    public static async Task RunAsync(string? cli = null)
    {
        var modern = new ModernCatalog(); var definitions = modern.Structures;
        Check(definitions.Count == 197 && definitions.All(d => d.Composition is not null), "all 197 current families reviewed");
        var known = definitions.Select(d => d.Id).ToHashSet(StringComparer.Ordinal);
        Check(definitions.SelectMany(d => d.Composition!.PartRoles).SelectMany(r => r.CandidateDefinitionIds).All(known.Contains), "review candidate IDs exist");
        foreach (var kind in Enum.GetValues<StructureRepresentation>()) Check(definitions.Any(d => d.Composition!.Representation == kind), "review distinguishes " + kind);
        // Exercise the same authoring algorithm across every frontage family, not just example parents.
        var allPrograms = definitions.Where(d => d.PlacementKind == BuildingPlacementKind.FrontageSite).Select(d => {
            var variant = d.SizeVariants!.OrderBy(v => v.WidthMeters * v.DepthMeters).ThenBy(v => v.Id, StringComparer.Ordinal).First();
            return new FacilityProgramDefinition("audit." + d.Id, "Audit " + d.Id, "test.full-inventory-authoring",
                new(new(4, 2, 0, 0, 0, 0), 8, 8, 8, 4, 8, 8), [new("primary", d.Id, variant.Id, "explicit primary member")], []);
        }).ToArray();
        var audit = CatalogContentAuthoring.Apply([modern], [new(1, "test.inventory", "modern", "test.inventory", [], allPrograms, [])]);
        Check(new SettlementCatalogRegistry(audit).Resolve(ModernCatalog.Selection).Facilities.Count == 3 + 187, "all 187 frontage families author/register a reusable program");
        foreach (var infrastructure in definitions.Where(d => d.PlacementKind != BuildingPlacementKind.FrontageSite))
            Reject(() => CatalogContentAuthoring.CompileProgram(allPrograms[0] with { Parts = [new("part", infrastructure.Id, infrastructure.SizeVariants![0].Id, "infrastructure is not a building slot")] }, definitions, []), "CATALOG_FACILITY_PART_GEOMETRY_UNSUPPORTED");

        var content = Content(); var providers = CatalogContentAuthoring.Apply([modern], [content]);
        var partsFile = content with { Id = "z.shared-parts", Programs = [] };
        var parentFile = content with { Id = "a.requested-parent", Structures = [], Programs = [content.Programs.Single(p => p.Id == "user.home-program")] };
        var split = new SettlementCatalogRegistry(CatalogContentAuthoring.Apply([modern], [parentFile, partsFile]));
        var reverseSplit = new SettlementCatalogRegistry(CatalogContentAuthoring.Apply([modern], [partsFile, parentFile]));
        Check(split.Resolve(ModernCatalog.Selection).Provenance.ContentSha256 == reverseSplit.Resolve(ModernCatalog.Selection).Provenance.ContentSha256,
            "cross-file part dependencies resolve independently of filename/input order");
        var registry = new SettlementCatalogRegistry(providers); var registered = registry.Resolve(ModernCatalog.Selection);
        Check(registered.Structures.Count == 198 && registered.Facilities.Count == 7, "new part definition and four reusable programs added to source catalog");
        Check(registered.Provenance.AuthoredContentDigests![content.Id].Length == 64, "authored content identity exported");
        string pluginPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../WorldCreator.TestCatalog/bin/Release/net10.0/WorldCreator.TestCatalog.dll"));
        using var plugin = new SettlementCatalogPlugin(pluginPath);
        var external = plugin.Providers.Single();
        var externalPart = external.Structures.Single() with { Id = "test.requested-annex" };
        var externalContent = new CatalogContentManifest(1, "test.requested-content", external.Descriptor.Id, "test.independent-source-request",
            [externalPart], [new("test.requested-program", "Requested independent program", "test.independent-source-request",
                content.Programs[1].Layout, [new("annex", externalPart.Id, "small", "requested specialist annex", 2)], [])], []);
        var externalPack = new SettlementCatalogRegistry(CatalogContentAuthoring.Apply([external], [externalContent]))
            .Resolve(new(external.Descriptor.Id, external.Descriptor.Capabilities));
        Check(externalPack.Structures.Count == 2 && externalPack.Facilities.Count == 1 && !externalPack.Descriptor.Capabilities.Any(c => c.StartsWith("modern.", StringComparison.Ordinal)),
            "same authoring API extends an independent compiled pack without Modern inheritance");
        var engine = new DesertWorldGenerator(registry);
        foreach (var pair in new[] { ("user.school-program", 2, 1), ("user.care-program", 2, 1), ("user.home-program", 3, 1), ("user.learning-and-care", 4, 3) })
        {
            var request = FacilityCompositionTests.Create(pair.Item1, selectedRegistry: registry);
            var world = await engine.GenerateAsync(request);
            Check(world.Objects.Count(o => o.VisualSource?.BuildingDefinitionId is not null) == pair.Item2 && world.Facilities!.Count == pair.Item3 &&
                world.Siting!.Status == SitingEvaluationStatus.Passed, "actual unrelated requested program generates: " + pair.Item1);
        }
        var referenceRequest = FacilityCompositionTests.Create("user.home-program", selectedRegistry: registry);
        var references = referenceRequest.Design! with { BuildingCatalogIds = ["bungalow", "user.garden-office"] };
        var referencedWorld = await engine.GenerateAsync(referenceRequest with { Design = references });
        Check(referencedWorld.Request.Design!.BuildingCatalogIds is null && referencedWorld.Request.Design.BuildingCatalog.Count == 2 &&
            JsonSerializer.Serialize(referencedWorld.Design) == JsonSerializer.Serialize((await engine.GenerateAsync(referenceRequest)).Design),
            "registered definition references materialize once and preserve the generated layout");
        Reject(() => CatalogContentAuthoring.BindReferences(references with { BuildingCatalogIds = ["missing.part"] }, registry), "CATALOG_STRUCTURE_REFERENCE_UNKNOWN");
        Reject(() => CatalogContentAuthoring.BindReferences(references with { BuildingCatalogIds = ["bungalow", "bungalow"] }, registry), "CATALOG_STRUCTURE_REFERENCE_INVALID");
        Reject(() => CatalogContentAuthoring.BindReferences(references with { BuildingCatalog = [registered.Structures[0]] }, registry), "CATALOG_STRUCTURE_REFERENCE_AMBIGUOUS");
        Reject(() => CatalogContentAuthoring.Apply(providers, [content]), "CATALOG_CONTENT_STRUCTURE_CONFLICT");
        Reject(() => CatalogContentAuthoring.Apply([modern], [content with { CatalogId = "unknown" }]), "CATALOG_CONTENT_TARGET_UNKNOWN");
        Reject(() => CatalogContentAuthoring.Apply([modern], [content with { Structures = [content.Structures[0] with { Composition = null }] }]), "CATALOG_CONTENT_COMPOSITION_REVIEW_REQUIRED");
        var invalid = content with { Structures = [], Programs = [content.Programs[1] with { Parts = [new("unknown", "missing.part", "small", "must add missing catalog part first")] }] };
        Reject(() => CatalogContentAuthoring.Apply([modern], [invalid]), "CATALOG_FACILITY_PART_UNKNOWN");
        Reject(() => CatalogContentAuthoring.CompileProgram(content.Programs[1] with { Parts = [content.Programs[1].Parts[0] with { Kind = CatalogPartKind.Equipment }] }, definitions, []), "CATALOG_FACILITY_PART_KIND_UNSUPPORTED");
        var replacement = content with { Structures = [definitions.Single(d => d.Id == "clinic") with { AutomaticDemand = false }], Programs = [] };
        Reject(() => CatalogContentAuthoring.Apply([modern], [replacement]), "CATALOG_CONTENT_STRUCTURE_CONFLICT");
        Check(CatalogContentAuthoring.Apply([modern], [replacement with { ReplaceStructureIds = new HashSet<string> { "clinic" } }]).Count == 1, "explicit replacement supported");
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        try { CatalogContentAuthoring.Apply([modern], [content], cancelled.Token); throw new Exception("expected cancellation"); } catch (OperationCanceledException) { }

        string root = Path.Combine(Path.GetTempPath(), "worldcreator-content-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try
        {
            string catalogDir = Path.Combine(root, ".gameframework", "catalogs");
            var saved = await CatalogContentAuthoring.SaveManifestAsync(content, catalogDir);
            var restored = CatalogContentAuthoring.ReadManifest(saved);
            var reloaded = new SettlementCatalogRegistry(CatalogContentAuthoring.Apply([modern], [restored]));
            Check(reloaded.Resolve(ModernCatalog.Selection).Provenance.ContentSha256 == registered.Provenance.ContentSha256, "persisted content reconstructs same current catalog");
            try { await CatalogContentAuthoring.SaveManifestAsync(content, catalogDir); throw new Exception("unrequested overwrite"); } catch (IOException) { }
            string before = await File.ReadAllTextAsync(saved);
            try { await CatalogContentAuthoring.SaveManifestAsync(content, catalogDir, true, cancelled.Token); throw new Exception("expected save cancellation"); } catch (OperationCanceledException) { }
            Check(before == await File.ReadAllTextAsync(saved), "cancelled update preserves authoritative catalog");
            await CatalogContentAuthoring.SaveManifestAsync(content, catalogDir, true);
            if (cli is not null)
            {
                var listing = JsonDocument.Parse(await Run("list-buildings", "modern"));
                Check(listing.RootElement.GetProperty("FamilyCount").GetInt32() == 198, "normal CLI reloads saved project catalog without a per-invocation flag");
                string requestFile = Path.Combine(root, "request.json");
                await File.WriteAllTextAsync(requestFile, JsonSerializer.Serialize(FacilityCompositionTests.Create("user.home-program", selectedRegistry: registry), WorldJson.CreateOptions()));
                await Run("generate-world", requestFile, Path.Combine(root, "world"));
                var generated = JsonSerializer.Deserialize<WorldDocument>(await File.ReadAllTextAsync(Path.Combine(root, "world", "world.json")), WorldJson.CreateOptions())!;
                Check(generated.Objects.Count(o => o.VisualSource?.BuildingDefinitionId == "user.garden-office") == 2, "fresh CLI uses newly authored parent and added part");
                await Run("save-catalog-content", saved, catalogDir, "--replace");
                Check((await File.ReadAllTextAsync(Path.Combine(root, ".gameframework", "operations", "audit.jsonl"))).Contains("world.catalog.content.saved", StringComparison.Ordinal), "catalog additions use shared audit owner");
                var splitRoot = Path.Combine(root, "cross-file-project");
                var splitDirectory = Path.Combine(splitRoot, ".gameframework", "catalogs");
                await CatalogContentAuthoring.SaveManifestAsync(parentFile, splitDirectory);
                await CatalogContentAuthoring.SaveManifestAsync(partsFile, splitDirectory);
                using var splitListing = JsonDocument.Parse(await RunAt(splitRoot, "list-facilities", "modern"));
                Check(splitListing.RootElement.GetProperty("Facilities").EnumerateArray().Any(p => p.GetProperty("Id").GetString() == "user.home-program"),
                    "project discovery resolves a parent whose shared-part file sorts later");
                async Task<string> Run(params string[] args)
                    => await RunAt(root, args);
                async Task<string> RunAt(string workingDirectory, params string[] args)
                {
                    var info = new ProcessStartInfo("dotnet") { WorkingDirectory = workingDirectory, RedirectStandardOutput = true, RedirectStandardError = true };
                    info.ArgumentList.Add(cli); foreach (var arg in args) info.ArgumentList.Add(arg);
                    using var process = Process.Start(info)!; var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
                    try { await process.WaitForExitAsync(timeout.Token); } catch (OperationCanceledException) { process.Kill(true); throw; }
                    var output = await stdout; var error = await stderr;
                    Check(process.ExitCode == 0, "CLI authoring " + args[0] + ": " + output + error); return output;
                }
            }
            await File.WriteAllTextAsync(saved, "{\"SchemaVersion\":1,\"Unknown\":true}");
            try { CatalogContentAuthoring.ReadManifest(saved); throw new Exception("invalid manifest accepted"); } catch (JsonException) { }
        }
        finally { Directory.Delete(root, true); }
        Console.WriteLine("PASS: all 197 family reviews; all 187 frontage families author/register through common grammar; 10 infrastructure families reject building substitution; arbitrary new parts, unrelated/nested requested programs, persistence/automatic reload, explicit updates, cancellation, audit and actual CLI generation.");
    }
    private static void Reject(Action action, string code)
    { try { action(); } catch (ArgumentException ex) when (ex.Message.StartsWith(code, StringComparison.Ordinal)) { return; } throw new Exception("Expected authoring rejection " + code); }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception("Catalog authoring: " + message); }
}
