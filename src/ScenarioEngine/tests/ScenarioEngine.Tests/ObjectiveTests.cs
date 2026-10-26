using System.Text.Json.Nodes;
using ScenarioEngine.Contracts;

namespace ScenarioEngine.Tests;

public sealed class ObjectiveTests
{
    [Fact]
    public void ObjectiveEffectsApplyOnceAndReprojectBeforeEnding()
    {
        var h = new Harness();
        var pack = Harness.Pack();
        pack["documents"]!["objective.json"]!["completeWhen"]!["value"] = 1;
        pack["documents"]!["objective.json"]!["successEffects"] = JsonNode.Parse("""[{"type":"test_counter.bonus","key":"bonus","arguments":{"amount":2}}]""");
        using var sim = h.Start(h.Scenario(pack));
        sim.Enqueue(Harness.Increment());
        var step = sim.Step(Harness.Input(1));
        Assert.True(step.Committed);
        Assert.Equal(3, Harness.Value(sim.Snapshot));
        Assert.Equal(SessionPhase.Ended, sim.Snapshot.Phase);
        Assert.Single(sim.Snapshot.AppliedEffects);
        Assert.Contains(step.Events, e => e.Kind == "core.ending_reached");
    }

    [Fact]
    public void CompletedObjectivesNeverReapplyEffects()
    {
        var h = new Harness();
        var pack = Harness.Pack();
        pack["documents"]!["scenario.json"]!["closingCondition"]!["value"] = 100;
        pack["documents"]!["objective.json"]!["completeWhen"]!["value"] = 1;
        pack["documents"]!["objective.json"]!["successEffects"] = JsonNode.Parse("""[{"type":"test_counter.bonus","key":"bonus","arguments":{"amount":1}}]""");
        using var sim = h.Start(h.Scenario(pack));
        sim.Enqueue(Harness.Increment());
        Assert.True(sim.Step(Harness.Input(1)).Committed);
        Assert.True(sim.Step(Harness.Input(2)).Committed);
        Assert.Equal(2, Harness.Value(sim.Snapshot));
        Assert.Single(sim.Snapshot.AppliedEffects);
    }

    [Fact]
    public void FailureWinsOverSimultaneousSuccess()
    {
        var h = new Harness();
        var pack = Harness.Pack();
        pack["documents"]!["objective.json"]!["completeWhen"]!["value"] = 0;
        pack["documents"]!["objective.json"]!["failWhen"]!["op"] = "gte";
        using var sim = h.Start(h.Scenario(pack));
        Assert.True(sim.Step(Harness.Input(1)).Committed);
        Assert.Equal(ObjectiveStatus.Failed, sim.Snapshot.Objectives["count_three"]);
    }

    [Theory]
    [InlineData(true, "ENDING_AMBIGUOUS")]
    [InlineData(false, "ENDING_UNCOVERED")]
    public void InvalidEndingResolutionRollsBackWholeTick(bool ambiguous, string expectedCode)
    {
        var h = new Harness();
        var scenario = h.Scenario();
        var definition = scenario.Definition;
        definition = ambiguous
            ? definition with { Endings = definition.Endings.Add(definition.Endings[0] with { Id = "other" }) }
            : definition with { Endings = [definition.Endings[0] with { Condition = new CompareCondition("test_counter.value", "lt", FactValue.From(0L)) }] };
        using var sim = h.Start(scenario with { Definition = definition });
        sim.Enqueue(Harness.Increment(amount: 3));
        var step = sim.Step(Harness.Input(1));
        Assert.False(step.Committed);
        Assert.Equal(expectedCode, step.Code);
        Assert.Equal(0, Harness.Value(sim.Snapshot));
        Assert.Equal(ObjectiveStatus.Locked, sim.Snapshot.Objectives["count_three"]);
        Assert.Empty(step.Events);
    }

    [Fact]
    public void EndAuditOutageCannotPublishFalseEnding()
    {
        var h = new Harness();
        using var sim = h.Start();
        h.Reporter.FailAudit = true;
        sim.Enqueue(Harness.Increment(amount: 3));
        var step = sim.Step(Harness.Input(1));
        Assert.False(step.Committed);
        Assert.Null(sim.Snapshot.EndingId);
        Assert.Empty(step.Events);
        Assert.Equal(0, sim.Snapshot.Tick);
    }
}
