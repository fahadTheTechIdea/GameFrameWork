using System.Diagnostics;
using System.Text.Json;
using WorldCreator.Catalogs.Modern;
using WorldCreator.Core;
using WorldCreator.Core.Geometry;
using WorldCreator.Core.Planning;
using WorldCreator.Worlds;

internal static class FacilityCompositionTests
{
    internal static WorldRequest Create(string facilityId, double scale = 1, ISettlementCatalogRegistry? selectedRegistry = null)
    {
        var registry = selectedRegistry ?? TestCatalogs.Registry;
        var pack = registry.Resolve(ModernCatalog.Selection);
        var template = pack.Facilities.Single(f => f.Id == facilityId);
        int width = (int)Math.Ceiling((template.WidthMeters + 40) / scale);
        int height = (int)Math.Ceiling((template.DepthMeters + 40) / scale);
        var outline = MockupGenerationFixture.Rect(0, 0, width * scale, height * scale);
        var contexts = new Dictionary<string, StructureSitingContext>(StringComparer.Ordinal);
        Add(template, "");
        void Add(FacilityDefinition current, string prefix)
        {
            foreach (var member in current.Structures) contexts.Add(prefix + member.Id, new("pending", "pending", []));
            foreach (var child in current.Children) Add(pack.Facilities.Single(f => f.Id == child.DefinitionId), prefix + child.Id + ":");
        }
        var input = new MockupWorldDesign(1,
            new(1, "synthetic-facility-mockup", "authored-r1", "synthetic-style", [new("world", outline, "campus-design")],
                [new("campus-origin", "world", new(20, 20), "campus-top-left")]), scale,
            [new("arrival", "world", new([new(4, 28), new(20, 28)]), "road")],
            [new("arrival", RoadClass.Local, new(4, 2, 0, 0, 0, 0))], [],
            [new("world", CellKind.Sand, 0, 0, "synthetic-ground")], [], [], Catalog: ModernCatalog.Selection,
            FacilityPlacements: [new("campus", facilityId, "world", "campus-origin", "settlement", "authored campus composition",
                new(SettlementClass.Town, SettlementRole.Mixed, 1), contexts)]);
        var expanded = new FacilityCompositionPlanner(registry).Expand(input);
        var services = TestCatalogs.Contextualize(new(width, height, 42, Biome.Desert, 1, expanded)).Design!;
        foreach (var rule in services.SettlementRules) contexts[rule.Id["campus:".Length..]] = rule.Siting!;
        return new(width, height, 42, Biome.Desert, 1, input with {
            Paths = input.Paths.Concat(services.Paths.Where(p => p.Role != "road")).ToArray(),
            Siting = services.Siting! with { Separations = [
                new("authored-impact-buffer", "heavy-impact", "sensitive", 8, "authored game clearance", "synthetic-game-design"),
                new("authored-freight-buffer", "freight-intensive", "sensitive", 8, "authored game clearance", "synthetic-game-design")] }
        });
    }

    public static async Task RunAsync(string? cli = null)
    {
        var modern = new ModernCatalog(); var registry = TestCatalogs.Registry;
        var engine = new DesertWorldGenerator(registry);
        foreach (var id in new[] { "modern.football-club", "modern.factory-campus" })
        {
            var request = Create(id); var world = await engine.GenerateAsync(request);
            var buildings = world.Objects.Where(o => o.VisualSource?.BuildingDefinitionId is not null).ToArray();
            Check(buildings.Length == (id == "modern.football-club" ? 4 : 5), "each constituent is a real generated structure: " + id);
            Check(world.Facilities!.Count == (id == "modern.football-club" ? 2 : 1), "nested ownership retained");
            Check(buildings.All(b => b.VisualSource!.Facility is not null), "per-member template/owner provenance");
            Check(world.Siting!.Status == SitingEvaluationStatus.Passed && buildings.All(b => world.Siting.Allocations.Any(a => a.ObjectId == b.Id)), "every child consumes actual contextual services");
            Check(buildings.All(b => world.SiteFeatures!.Any(s => s.BuildingId == b.Id && s.Kind == SiteFeatureKind.AccessWalkway)), "each child has planned access");
            if (id == "modern.football-club") Check(world.Facilities.Single(f => f.TemplateId == "modern.training-block").ParentFacilityId == "campus" &&
                buildings.Count(b => b.VisualSource!.BuildingDefinitionId == "football-training-ground") == 2, "repeated nested first-team/academy training grounds");
            var again = await engine.GenerateAsync(request with { Seed = 99 });
            Check(JsonSerializer.Serialize(world.Design) == JsonSerializer.Serialize(again.Design), "positions follow grammar, not seed");
            var restored = JsonSerializer.Deserialize<WorldDocument>(JsonSerializer.Serialize(world, WorldJson.CreateOptions()), WorldJson.CreateOptions())!;
            Check(restored.Facilities!.Count == world.Facilities.Count && restored.Objects.Count(o => o.VisualSource?.Facility is not null) == world.Objects.Count(o => o.VisualSource?.Facility is not null), "ownership JSON roundtrip");
            var regenerated = await engine.GenerateAsync(restored.Request);
            Check(JsonSerializer.Serialize(regenerated.Design) == JsonSerializer.Serialize(world.Design), "compiled request validates and regenerates without duplicate expansion");
            await Rejected(engine, restored.Request with { Design = restored.Request.Design! with {
                PlannedFacilities = restored.Request.Design.PlannedFacilities!.Select((p, i) => i == 0 ? p with { MockupRevision = "forged" } : p).ToArray() } }, "FACILITY_COMPILED_PROVENANCE_INVALID");
            await Rejected(engine, request with { Design = request.Design! with { FacilityPlacements = request.Design.FacilityPlacements!.Select(p => p with {
                MemberContexts = new Dictionary<string, StructureSitingContext>() }).ToArray() } }, "FACILITY_MEMBER_CONTEXT_REQUIRED");
            await Rejected(engine, request with { Design = request.Design! with { Blueprint = request.Design.Blueprint with {
                Anchors = [new("campus-origin", "world", new(request.Width * request.Design.MetersPerCell - 1, 20), "campus")] } } }, "FACILITY_OUTSIDE_PARENT");
            await Rejected(engine, request with { Design = request.Design! with { Siting = request.Design.Siting! with {
                Sources = request.Design.Siting.Sources.Select(s => s with { Capacity = 0.00001 }).ToArray() } } }, "SITING_DEPENDENCY_FAILED");
        }
        await Rejected(engine, Create("modern.football-club", 2), "BUILDING_GREEN_RASTER_CONFLICT");
        var club = Create("modern.football-club");
        var shifted = club with { Design = club.Design! with { Blueprint = club.Design.Blueprint with {
            Anchors = [new("campus-origin", "world", new(22, 20), "campus")] } } };
        // Expansion moves descendants exactly with the anchor. Services are deliberately not
        // moved here: source/route feasibility remains an independent generation gate.
        var first = new FacilityCompositionPlanner(registry).Expand(club.Design!);
        var second = new FacilityCompositionPlanner(registry).Expand(shifted.Design!);
        Check(first.PlannedFacilities!.Zip(second.PlannedFacilities!).All(p => p.Second.Boundary.Vertices[0].X - p.First.Boundary.Vertices[0].X == 2), "nested geometry follows source anchor exactly");
        Reject(() => new FacilityCompositionPlanner(registry).Expand(club.Design! with {
            FacilityPlacements = Enumerable.Range(0, 257).Select(_ => club.Design!.FacilityPlacements![0]).ToArray() }), "FACILITY_PLACEMENT_BUDGET");
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        try { new FacilityCompositionPlanner(registry).Expand(club.Design!, cancelled.Token); throw new Exception("expected cancellation"); } catch (OperationCanceledException) { }
        var template = modern.Facilities.Single(f => f.Id == "modern.football-club");
        Reject(() => new SettlementCatalogRegistry([new Pack(modern, [template with { Children = [new("loop", template.Id, new(0, 0), "invalid loop")] }])]), "FACILITY_CYCLE");
        Reject(() => new SettlementCatalogRegistry([new Pack(modern, [template])]), "FACILITY_CHILD_UNKNOWN");
        var row = modern.Facilities.Single(f => f.Id == "modern.factory-campus");
        Reject(() => new SettlementCatalogRegistry([new Pack(modern, [row with { Structures = row.Structures.Append(row.Structures[0] with { Id = "overlap" }).ToArray() }])]), "FACILITY_SITE_CONFLICT");
        await CatalogComposition(modern, cli);
        Console.WriteLine("PASS: explicit catalog imports/policy dispatch/provenance; nested football-club and factory campuses through real world, access, siting and resource gates; JSON, conflicts, missing contexts, capacities, anchor motion and cancellation.");
    }

    private static async Task CatalogComposition(ModernCatalog modern, string? cli)
    {
        string pluginPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../WorldCreator.TestCatalog/bin/Release/net10.0/WorldCreator.TestCatalog.dll"));
        using var plugin = new SettlementCatalogPlugin(pluginPath);
        var external = plugin.Providers.Single();
        var imports = new CatalogImport[] { new("modern", ["office-building", "clinic", "football-training-ground", "warehouse", "factory", "workshop", "road-bridge"],
            modern.Facilities.Select(f => f.Id).ToArray()), new(external.Descriptor.Id, ["test.arcane-hall"], []) };
        var definition = new CatalogCompositionDefinition("test.composed", "Explicit composed setting", imports);
        var providers = SettlementCatalogComposition.Register([modern, external], [definition]);
        var registry = new SettlementCatalogRegistry(providers);
        var descriptor = registry.Catalogs.Single(c => c.Id == definition.Id);
        var selection = new SettlementCatalogSelection(definition.Id, descriptor.Capabilities);
        var pack = registry.Resolve(selection);
        Check(pack.Structures.Count == 8 && pack.Facilities.Count == 3 && pack.Provenance.Composition!.CatalogDigests.Count == 2, "only explicit imports with source digests");
        var reversed = SettlementCatalogComposition.Register([external, modern], [definition with { Imports = imports.Reverse().Select(i => i with { StructureIds = i.StructureIds.Reverse().ToArray(), FacilityIds = i.FacilityIds.Reverse().ToArray() }).ToArray() }]);
        Check(new SettlementCatalogRegistry(reversed).Resolve(selection).Provenance.ContentSha256 == pack.Provenance.ContentSha256, "composition ordering independent");
        var request = Create("modern.football-club");
        var world = await new DesertWorldGenerator(registry).GenerateAsync(request with { Design = request.Design! with { Catalog = selection } });
        Check(world.Catalog!.Id == definition.Id && world.Siting!.Status == SitingEvaluationStatus.Passed, "composed selection drives actual Modern facility policies");
        Check(world.Catalog.Composition!.StructureOrigins["factory"] == "modern" && world.Catalog.Composition.StructureOrigins["test.arcane-hall"] == "test.arcane", "exported per-definition source ownership");
        var mixed = Mixed(request, pack, selection);
        var mixedWorld = await new DesertWorldGenerator(registry).GenerateAsync(mixed);
        Check(mixedWorld.Objects.Any(o => o.Kind == "test.arcane-hall") && mixedWorld.Objects.Any(o => o.Kind == "clinic") &&
            mixedWorld.Siting!.Allocations.Any(a => a.RequirementId == "mana"), "one world actually compiles two imported packs and resource policies");
        await Rejected(new(registry), mixed with { Design = mixed.Design! with { Siting = mixed.Design.Siting! with {
            Sources = mixed.Design.Siting.Sources.Select(s => s.Id == "arcane-mana" ? s with { Qualities = new HashSet<string>() } : s).ToArray() } } }, "SITING_DEPENDENCY_FAILED");
        await Rejected(new(registry), mixed with { Design = mixed.Design! with { Catalog = selection with {
            EnabledCapabilities = selection.EnabledCapabilities.Where(c => c != "test.mana").ToHashSet() } } }, "CATALOG_CAPABILITY_REQUIRED");
        var bridge = InfrastructureAlgorithmTests.Create("road-bridge");
        var bridgeWorld = await new DesertWorldGenerator(registry).GenerateAsync(bridge with { Design = bridge.Design! with { Catalog = selection } });
        Check(bridgeWorld.Infrastructure!.Single().DefinitionId == "road-bridge", "imported infrastructure planner executes");
        var demand = new BuildingProgramPlanner(pack.Structures, pack.Demand).Plan(new(SettlementClass.Town, SettlementRole.Mixed, 1));
        Check(demand.Needs.Any(n => n.DefinitionId == "test.arcane-hall") && demand.Needs.All(n => pack.Structures.Any(d => d.Id == n.DefinitionId)), "demand dispatch stays inside explicit imports");
        var externalDefinition = pack.Structures.Single(d => d.Id == "test.arcane-hall");
        Check(pack.Policy.RequiredCapabilities(externalDefinition, "test.leycraft").SetEquals(new[] { "test.mana" }), "external policy dispatch, no Modern inheritance");
        Check(pack.Policy.ValidateSupply(request.Design!, externalDefinition, externalDefinition.SitingProfile!.Requirements.Single(),
            new("test", "test.mana", SitingResourceOrigin.UtilityNetwork, "world", new(1, 1), 10, "game-service-units", new HashSet<string>(), "test"),
            new("district", "test.leycraft", [])).Single() == "test.ward-requires-attuned-mana", "external supply code actually executes");
        Reject(() => SettlementCatalogComposition.Register([modern], [definition]), "CATALOG_COMPOSITION_DEPENDENCY_MISSING_OR_CYCLE");
        Reject(() => SettlementCatalogComposition.Register([modern], [new("a", "a", [new("b", ["x"], [])]), new("b", "b", [new("a", ["x"], [])])]), "CATALOG_COMPOSITION_DEPENDENCY_MISSING_OR_CYCLE");
        Reject(() => new SettlementCatalogComposition(definition with { Imports = [new("modern", ["factory", "factory"], [])] }, TestCatalogs.Registry), "CATALOG_IMPORT_STRUCTURE_CONFLICT");
        Reject(() => new SettlementCatalogComposition(definition with { Imports = [new("modern", ["unknown"], [])] }, TestCatalogs.Registry), "CATALOG_IMPORT_STRUCTURE_UNKNOWN");
        Reject(() => new SettlementCatalogComposition(definition with { Imports = [new("modern", ["clinic"], ["modern.football-club"])] }, TestCatalogs.Registry), "FACILITY_STRUCTURE_UNKNOWN");
        var provider = providers.Single(p => p.Descriptor.Id == definition.Id);
        Reject(() => new SettlementCatalogRegistry([new Pack(modern, modern.Facilities, modern.Descriptor with { Name = "changed source content" }), external, provider]), "CATALOG_IMPORT_DIGEST_MISMATCH");
        var limitedProviders = SettlementCatalogComposition.Register([modern], [new("test.limited", "Limited imports", [new("modern", ["office-building"], [])])]);
        var limitedRegistry = new SettlementCatalogRegistry(limitedProviders);
        var unimported = SitingAlgorithmTests.Create("clinic");
        await Rejected(new(limitedRegistry), unimported with { Design = unimported.Design! with {
            Catalog = new("test.limited", ModernCapabilities.All) } }, "CATALOG_COMPOSITION_STRUCTURE_NOT_IMPORTED");
        var manifest = new CatalogCompositionManifest(1, [definition]);
        if (cli is not null) await Cli(cli, pluginPath, manifest, request with { Design = request.Design! with { Catalog = selection } });
    }

    private static WorldRequest Mixed(WorldRequest request, RegisteredSettlementCatalog pack, SettlementCatalogSelection selection)
    {
        var design = request.Design!; double y = (request.Height - 8) * design.MetersPerCell;
        int height = request.Height + 40;
        var region = new VisualRegion("arcane-site", MockupGenerationFixture.Rect(20, y, 50, y + 28), "authored custom pack precinct");
        var path = new VisualDesignPath("arcane-arrival", "world", new([new(4, y), new(50, y)]), "road");
        var structure = pack.Structures.Single(d => d.Id == "test.arcane-hall");
        return request with { Height = height, Design = design with { Catalog = selection,
            Blueprint = design.Blueprint with { Regions = [new("world", MockupGenerationFixture.Rect(0, 0, request.Width * design.MetersPerCell, height * design.MetersPerCell), "mixed composition"), region] },
            BuildingCatalog = [structure],
            Paths = design.Paths.Concat([path, path with { Id = "arcane-mana-path", Role = "utility" }, new("arcane-connector", "world", new([new(4, 28), new(4, y)]), "road")]).ToArray(),
            RoadRules = design.RoadRules.Concat([new("arcane-arrival", RoadClass.Local, new(4, 2, 0, 0, 0, 0)), new("arcane-connector", RoadClass.Local, new(4, 2, 0, 0, 0, 0))]).ToArray(),
            SettlementRules = [new("arcane-hall", region.Id, "settlement", path.Id, "authored custom ritual hall", 30, 30,
                new(structure.Id, 4, 4, 2, 8, 8), [], "small", 5, new(SettlementClass.Town, SettlementRole.Mixed, 1), new("arcane-district", "test.leycraft", [new("mana", "arcane-mana", ["arcane-mana-link"])]))],
            Siting = design.Siting! with {
                Districts = design.Siting.Districts.Append(new("arcane-district", region.Id, "settlement", "test.sanctum", "synthetic-mixed-pack-rule")).ToArray(),
                Sources = design.Siting.Sources.Append(new("arcane-mana", "test.mana", SitingResourceOrigin.UtilityNetwork, "world", path.Centerline.Points[0], 1000, "game-service-units", new HashSet<string> { "attuned" }, "synthetic-authored-rule")).ToArray(),
                Links = design.Siting.Links.Append(new("arcane-mana-link", "arcane-mana-path", SitingNetworkMode.Utility, 1000, "game-service-units", "synthetic-authored-rule", Services: new HashSet<string> { "test.mana" })).ToArray()
            }
        } };
    }

    private static async Task Cli(string cli, string plugin, CatalogCompositionManifest manifest, WorldRequest request)
    {
        string root = Path.Combine(Path.GetTempPath(), "worldcreator-composition-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try
        {
            string file = Path.Combine(root, "catalog.json"), input = Path.Combine(root, "world.json");
            await File.WriteAllTextAsync(file, JsonSerializer.Serialize(manifest, WorldJson.CreateOptions()));
            await File.WriteAllTextAsync(input, JsonSerializer.Serialize(request, WorldJson.CreateOptions()));
            Check(SettlementCatalogComposition.ReadManifest(file).Catalogs.Count == 1, "data manifest contract");
            await Run("list-facilities", "test.composed");
            await Run("generate-world", input, Path.Combine(root, "generated"));
            Check(File.Exists(Path.Combine(root, "generated", "world.json")), "CLI generated actual composed facility world");
            string before = await File.ReadAllTextAsync(Path.Combine(root, "generated", "world.json"));
            await File.WriteAllTextAsync(input, JsonSerializer.Serialize(request with { Design = request.Design! with {
                FacilityPlacements = request.Design.FacilityPlacements!.Select(p => p with { MemberContexts = new Dictionary<string, StructureSitingContext>() }).ToArray() } }, WorldJson.CreateOptions()));
            await RunInvalid("generate-world", input, Path.Combine(root, "generated"));
            Check(before == await File.ReadAllTextAsync(Path.Combine(root, "generated", "world.json")), "rejected facility input preserves existing export");
            Check((await File.ReadAllTextAsync(Path.Combine(root, ".gameframework", "operations", "audit.jsonl"))).Contains("world.generate.failed", StringComparison.Ordinal), "composition failures use shared audit owner");
            await File.WriteAllTextAsync(file, "{\"SchemaVersion\":1,\"Catalogs\":[],\"Unknown\":true}");
            try { SettlementCatalogComposition.ReadManifest(file); throw new Exception("unknown manifest member accepted"); } catch (JsonException) { }
            async Task RunInvalid(params string[] args) => await Process(args, false);
            async Task Run(params string[] args)
                => await Process(args, true);
            async Task Process(string[] args, bool accepted)
            {
                var info = new ProcessStartInfo("dotnet") { WorkingDirectory = root, RedirectStandardOutput = true, RedirectStandardError = true };
                info.ArgumentList.Add(cli); foreach (var arg in args) info.ArgumentList.Add(arg);
                info.ArgumentList.Add("--catalog-plugin"); info.ArgumentList.Add(plugin);
                info.ArgumentList.Add("--catalog-composition"); info.ArgumentList.Add(file);
                using var process = System.Diagnostics.Process.Start(info)!; var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
                try { await process.WaitForExitAsync(timeout.Token); } catch (OperationCanceledException) { process.Kill(true); throw; }
                var output = await stdout; var error = await stderr;
                Check((process.ExitCode == 0) == accepted, "CLI " + args[0] + ": " + output + error);
            }
        }
        finally { Directory.Delete(root, true); }
    }
    private static async Task Rejected(DesertWorldGenerator engine, WorldRequest request, string code)
    {
        try { await engine.GenerateAsync(request); } catch (WorldGenerationException ex) when (ex.Issues.Any(i => i.StartsWith(code, StringComparison.Ordinal))) { return; }
        throw new Exception("Expected facility rejection " + code);
    }
    private static void Reject(Action action, string code)
    { try { action(); } catch (ArgumentException ex) when (ex.Message.StartsWith(code, StringComparison.Ordinal)) { return; } throw new Exception("Expected composition rejection " + code); }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception("Composition: " + message); }
    private sealed class Pack(ISettlementCatalogProvider inner, IReadOnlyList<FacilityDefinition> facilities, SettlementCatalogDescriptor? descriptor = null) : ISettlementCatalogProvider
    {
        public SettlementCatalogDescriptor Descriptor => descriptor ?? inner.Descriptor;
        public IReadOnlyList<BuildingDefinition> Structures => inner.Structures;
        public IReadOnlyList<FacilityDefinition> Facilities => facilities;
        public ISettlementCatalogPolicy Policy => inner.Policy;
        public ISettlementDemandPolicy Demand => inner.Demand;
        public IInfrastructureDesignStrategy Infrastructure => inner.Infrastructure;
    }
}
