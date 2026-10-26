using WorldCreator.Catalogs.Modern;
using System.Text.Json;
using WorldCreator.Core;
using WorldCreator.Worlds;

internal static class ProductionIntegrityTests
{
    public static async Task RunAsync(string root, string exportedPath,
        IReadOnlyDictionary<string, ApprovedArtManifest> manifests,
        CoordinatedSettlementResult composition, ProductionWorldRequest inputs, string? cliPath)
    {
        var registry = new MappedRegistry(manifests);
        var verifier = new PortableWorldPackageVerifier(registry);
        var originalBytes = await File.ReadAllBytesAsync(exportedPath);
        var world = JsonSerializer.Deserialize<DummyVillageWorldDocument>(originalBytes)!;
        var home = world.AssetDependencies!.Single(x => x.AssetId == "fixture-home");
        var malformed = new (string Name, DummyVillageWorldDocument World, string Issue)[]
        {
            ("null-base", world with { SemanticWorld = world.SemanticWorld with { Base = null! } },
                "WORLD_DEPENDENCY_STRUCTURE_INVALID"),
            ("null-bindings", world with { SemanticWorld = world.SemanticWorld with {
                Base = world.SemanticWorld.Base with { ArtBindings = null! } } },
                "WORLD_DEPENDENCY_STRUCTURE_INVALID"),
            ("null-binding", world with { SemanticWorld = world.SemanticWorld with {
                Base = world.SemanticWorld.Base with { ArtBindings = [null!] } } },
                "WORLD_ART_ASSET_REFERENCE_INVALID"),
            ("blank-art", world with { SemanticWorld = world.SemanticWorld with {
                Base = world.SemanticWorld.Base with { ArtBindings = [
                    world.SemanticWorld.Base.ArtBindings[0] with { AssetId = " " }] } } },
                "WORLD_ART_ASSET_REFERENCE_INVALID"),
            ("blank-terrain", world with { TerrainMaterials = [world.TerrainMaterials[0] with { MaterialId = "" }] },
                "WORLD_TERRAIN_ASSET_REFERENCE_INVALID"),
            ("null-garden", world with { GardenBeds = [null!] }, "WORLD_GARDEN_ASSET_REFERENCE_INVALID"),
            ("blank-species", world with { Vegetation = [world.Vegetation[0] with { SpeciesAssetId = " " }] },
                "WORLD_VEGETATION_ASSET_REFERENCE_INVALID"),
            ("null-evidence", world with { AssetDependencies = [null!] },
                "WORLD_DEPENDENCY_DUPLICATE_OR_INVALID:"),
            ("null-sheet-evidence", WithHome(home with { Files = [null!] }),
                "WORLD_DEPENDENCY_HASH_EVIDENCE_MISMATCH:fixture-home"),
            ("duplicate-evidence", world with { AssetDependencies = [.. world.AssetDependencies!, home] },
                "WORLD_DEPENDENCY_DUPLICATE_OR_INVALID:fixture-home"),
            ("wrong-hash", WithHome(home with { Files = [home.Files[0] with { ImageSha256 = new string('0', 64) }] }),
                "WORLD_DEPENDENCY_HASH_EVIDENCE_MISMATCH:fixture-home"),
            ("unexpected-evidence", world with { AssetDependencies = [.. world.AssetDependencies!, home with { AssetId = "unused" }] },
                "WORLD_DEPENDENCY_UNEXPECTED:unused")
        };
        foreach (var sample in malformed)
        {
            var path = Path.Combine(root, sample.Name + ".json");
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(sample.World));
            var rejected = await verifier.VerifyAsync(path, root);
            Check(!rejected.Valid && rejected.Issues.Contains(sample.Issue), sample.Name);
        }

        var invalidManifests = new[]
        {
            manifests["fixture-home"] with { Sheets = [null!] },
            manifests["fixture-home"] with { Sheets = [manifests["fixture-home"].Sheets[0] with { Id = "" }] },
            manifests["fixture-home"] with { Sheets = [manifests["fixture-home"].Sheets[0], manifests["fixture-home"].Sheets[0]] },
            manifests["fixture-home"] with { GameArtsRevision = "" },
            manifests["fixture-home"] with { ApprovalState = ArtApprovalState.Revoked }
        };
        foreach (var manifest in invalidManifests)
        {
            var changed = manifests.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
            changed["fixture-home"] = manifest;
            var changedRegistry = new MappedRegistry(changed);
            var rejected = await new PortableWorldPackageVerifier(changedRegistry).VerifyAsync(exportedPath, root);
            Check(!rejected.Valid && rejected.Issues.Contains(
                "WORLD_DEPENDENCY_APPROVAL_OR_REVISION_MISMATCH:fixture-home"), "malformed registry manifest");
            var evidence = await new WorldAssetDependencyManifestBuilder(changedRegistry).BuildAsync(["fixture-home"]);
            Check(!evidence.Valid && evidence.Dependencies.Count == 0, "incomplete evidence is never exported");
            var rejectedProduction = await new GameArtsNonVegetationGate(changedRegistry,
                new TestApprovalAuthority(true)).VerifyAsync(world, root, inputs.ProductionBundles);
            Check(rejectedProduction.Contains("NONVEGETATION_MANIFEST_NOT_APPROVED:fixture-home"),
                "production rejects malformed manifests before reading sheets");
        }

        var registryRoot = Path.Combine(root, "broken-registry");
        Directory.CreateDirectory(registryRoot);
        await File.WriteAllTextAsync(Path.Combine(registryRoot, "fixture-home.json"), "{");
        var unreadable = await new PortableWorldPackageVerifier(new FileSystemArtRegistry(registryRoot))
            .VerifyAsync(exportedPath, root);
        Check(!unreadable.Valid && unreadable.Issues.Contains(
            "WORLD_DEPENDENCY_MANIFEST_UNREADABLE:fixture-home:JsonException"), "unreadable registry diagnostic");

        // Every bundle here already passed the full positive fixture. Substituting a
        // different, valid bundle must still fail even when the authority approves both.
        var other = inputs.ProductionBundles.First(x => x.AssetId != "fixture-home");
        var wrongBundles = inputs.ProductionBundles.Select(x => x.AssetId == "fixture-home"
            ? x with { BundlePath = other.BundlePath } : x).ToArray();
        var exporter = new CoordinatedProductionWorldExporter(registry, new TestApprovalAuthority(true));
        var wrongAsset = await exporter.ExportAsync(new(composition, inputs with {
            ProductionBundles = wrongBundles }), exportedPath);
        Check(!wrongAsset.Produced && wrongAsset.Issues.Contains(
            "NONVEGETATION_BUNDLE_ASSET_MISMATCH:fixture-home"), "cross-asset bundle substitution");
        var preservedBytes = await File.ReadAllBytesAsync(exportedPath);
        Check(originalBytes.SequenceEqual(preservedBytes),
            "rejected export preserves accepted output");

        var brokenBundlePath = Path.Combine(root, "broken.bundle.json");
        await File.WriteAllTextAsync(brokenBundlePath, "{");
        var brokenBundles = inputs.ProductionBundles.Select(x => x.AssetId == "fixture-home"
            ? x with { BundlePath = "broken.bundle.json" } : x).ToArray();
        var unreadableBundle = await new GameArtsNonVegetationGate(registry, new TestApprovalAuthority(true))
            .VerifyAsync(world, root, brokenBundles);
        Check(unreadableBundle.Contains("NONVEGETATION_BUNDLE_UNREADABLE:fixture-home"),
            "malformed bundle diagnostic");
        var invalidInventory = await new GameArtsNonVegetationGate(registry, new TestApprovalAuthority(true))
            .VerifyAsync(malformed.Single(x => x.Name == "blank-terrain").World, root, inputs.ProductionBundles);
        Check(invalidInventory.Contains("WORLD_TERRAIN_ASSET_REFERENCE_INVALID"),
            "production and audit share reference validation");

        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        try
        {
            await verifier.VerifyAsync(exportedPath, root, cancelled.Token);
            throw new InvalidOperationException("Cancelled audit completed.");
        }
        catch (OperationCanceledException) { }
        if (cliPath is not null)
            await CliIntegrityTests.RunAsync(cliPath, root, exportedPath, manifests);
        else
            Console.WriteLine("NOT RUN: CLI process checks (supply --cli <WorldCreator.Cli.dll>).");
        Console.WriteLine("PASS: production integrity regressions (synthetic GameArts fixtures).");

        DummyVillageWorldDocument WithHome(WorldAssetDependency replacement) => world with {
            AssetDependencies = world.AssetDependencies!.Select(x => x.AssetId == "fixture-home" ? replacement : x).ToArray()
        };
    }

    private static void Check(bool pass, string scenario)
    {
        if (!pass) throw new InvalidOperationException("Production integrity regression failed: " + scenario);
    }
}
