using System.Text;
using System.Text.Json.Nodes;
using Counter.GameLayer;
using ScenarioEngine.Contracts;
using ScenarioEngine.Core;

namespace ScenarioEngine.Tests;

public sealed class ContentTests
{
    [Fact]
    public void HostCanConsumeAuthoredWorldWithoutReparsingSource()
    {
        var scenario = new Harness().Scenario();
        Assert.Equal("sandbox", scenario.Definition.StartZoneId);
        var zone = Assert.Single(scenario.Definition.Zones);
        Assert.Equal("counter_zone", zone.SceneRole);
        Assert.Equal(new PositionMm(0, 0, 0), Assert.Single(zone.Anchors).Position);
    }

    [Fact]
    public void ValidPackRegistersAndAnotherScenarioNeedsNoSourceChange()
    {
        var h = new Harness();
        var first = h.Scenario();
        var pack = Harness.Pack();
        pack["manifest"]!["packId"] = "examples.second";
        pack["documents"]!["scenario.json"]!["id"] = "second_operation";
        pack["manifest"]!["entryScenarioIds"]![0] = "second_operation";
        Assert.True(h.Load(pack).IsReady);
        Assert.Equal(2, h.Content.ListPlayable().Length);
        Assert.True(h.Content.TryResolve(first.Identity, out _));
    }

    [Fact]
    public void ContentIdentityConflictsNeverReplaceRunningDefinitions()
    {
        var h = new Harness();
        var original = h.Scenario();
        var changed = Harness.Pack();
        changed["documents"]!["scenario.json"]!["layerData"]!["initialValue"] = 1;
        var refused = h.Load(changed);
        Assert.False(refused.IsReady);
        Assert.Contains(refused.Report.Diagnostics, d => d.Code == "PACK_ID_CONFLICT");
        Assert.Equal(original, h.Content.ListPlayable().Select(s => h.Content.TryResolve(s.Identity, out var resolved) ? resolved : null).Single());
    }

    [Theory]
    [InlineData("unknown_fact", "CONDITION_FACT")]
    [InlineData("wrong_type", "CONDITION_TYPE")]
    [InlineData("cycle", "OBJECTIVE_CYCLE")]
    [InlineData("missing_zone", "PACK_REFERENCE")]
    [InlineData("negative_stock", "SCHEMA_VALIDATION")]
    [InlineData("unknown_effect", "EFFECT_UNKNOWN")]
    [InlineData("unknown_layer", "LAYER_UNAVAILABLE")]
    [InlineData("unsafe_path", "SCHEMA_VALIDATION")]
    public void InvalidContentIsNeverPlayable(string mutation, string expectedCode)
    {
        var h = new Harness();
        var pack = Harness.Pack();
        switch (mutation)
        {
            case "unknown_fact": pack["documents"]!["scenario.json"]!["closingCondition"]!["fact"] = "test_counter.unknown"; break;
            case "wrong_type": pack["documents"]!["scenario.json"]!["closingCondition"]!["value"] = true; break;
            case "cycle": pack["documents"]!["objective.json"]!["dependencies"] = JsonNode.Parse("""[{"objectiveId":"count_three","acceptedStatuses":["Completed"]}]"""); break;
            case "missing_zone": pack["documents"]!["scenario.json"]!["startZoneId"] = "absent"; break;
            case "negative_stock": pack["documents"]!["scenario.json"]!["layerData"]!["initialValue"] = -1; break;
            case "unknown_effect": pack["documents"]!["objective.json"]!["successEffects"] = JsonNode.Parse("""[{"type":"arbitrary.execute","key":"bad","arguments":{}}]"""); break;
            case "unknown_layer": pack["manifest"]!["gameLayerId"] = "unknown_layer"; break;
            case "unsafe_path": pack["manifest"]!["files"]![0]!["path"] = "../escape.json"; break;
        }
        var result = h.Load(pack);
        Assert.False(result.IsReady);
        Assert.Contains(result.Report.Diagnostics, d => d.Code == expectedCode);
        Assert.Empty(h.Content.ListPlayable());
    }

    [Fact]
    public void DuplicateJsonPropertiesAndUnsupportedApiAreRefused()
    {
        var h = new Harness();
        var duplicate = """{"manifest":{},"manifest":{},"documents":{}}""";
        Assert.Contains(h.Content.LoadNormalized(Encoding.UTF8.GetBytes(duplicate), TestContext.Current.CancellationToken).Report.Diagnostics, d => d.Code == "PACK_JSON");
        var pack = Harness.Pack();
        pack["manifest"]!["contentApiVersion"] = 2;
        Assert.False(h.Load(pack).IsReady);
    }

    [Fact]
    public void RegistryRejectsDuplicatePluginsAndUsesExactVersions()
    {
        Assert.Throws<ArgumentException>(() => new GameLayerRegistry([new CounterLayer(), new CounterLayer()]));
        var registry = new GameLayerRegistry([new CounterLayer()]);
        Assert.True(registry.TryResolve("test_counter", "1.0.0", out _));
        Assert.False(registry.TryResolve("test_counter", "2.0.0", out _));
        Assert.False(registry.TryResolve("Test_Counter", "1.0.0", out _));
    }

    [Fact]
    public void CoreAssembliesDoNotDependOnDomainOrGodot()
    {
        foreach (var assembly in new[] { typeof(IGame).Assembly, typeof(GameBase).Assembly })
        {
            Assert.DoesNotContain(assembly.GetReferencedAssemblies(),
                name => name.Name!.Contains("Godot", StringComparison.OrdinalIgnoreCase) ||
                        name.Name.Contains("Counter", StringComparison.OrdinalIgnoreCase) ||
                        name.Name.Contains("LostSoldier", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void UnavailableLostSoldierPackDoesNotPretendToRun()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ScenarioEngine.slnx"))) directory = directory.Parent;
        Assert.NotNull(directory);
        var bytes = File.ReadAllBytes(Path.Combine(directory.FullName, "docs/sdd/examples/river_crossing.json"));
        var h = new Harness();
        var result = h.Content.LoadNormalized(bytes, TestContext.Current.CancellationToken);
        Assert.False(result.IsReady);
        Assert.Contains(result.Report.Diagnostics, d => d.Code == "LAYER_UNAVAILABLE");
    }
}
