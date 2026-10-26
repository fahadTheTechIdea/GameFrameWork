using System.Collections.Immutable;
using ScenarioEngine.Contracts;

namespace ScenarioEngine.Tests;

public sealed class EnhancementRegressionTests
{
    [Fact]
    public void ControllerIdentityIsOrdinalRegardlessOfCallerCollectionComparer()
    {
        var h = new Harness();
        var options = Harness.Options() with { Controllers = ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, "player") };
        using var sim = h.Factory.Create(h.Scenario(), options, Harness.Frame(0));
        sim.Start();
        Assert.Equal("COMMAND_DENIED", sim.Enqueue(Harness.Increment(controller: "PLAYER")).Code);
    }

    [Theory]
    [InlineData("operation")]
    [InlineData("controller")]
    [InlineData("payload")]
    [InlineData("fingerprint")]
    [InlineData("duplicate_sequence")]
    [InlineData("orphan_receipt")]
    public void RestoreRefusesTamperedCommands(string mutation)
    {
        var h = new Harness();
        var scenario = h.Scenario();
        using var sim = h.Start(scenario);
        sim.Enqueue(Harness.Increment());
        var state = sim.Snapshot;
        var pending = state.PendingCommands[0];
        state = mutation switch
        {
            "operation" => state with { PendingCommands = [pending with { Command = pending.Command with { Operation = "unregistered.execute" } }] },
            "controller" => state with { PendingCommands = [pending with { Command = pending.Command with { ControllerId = "intruder" } }] },
            "payload" => state with { PendingCommands = [pending with { Command = pending.Command with { Payload = ContractJson.Element(new { amount = -2 }) } }] },
            "fingerprint" => state with { Receipts = state.Receipts.SetItem("one", state.Receipts["one"] with { Fingerprint = "wrong" }) },
            "duplicate_sequence" => state with { PendingCommands = [pending, pending] },
            "orphan_receipt" => state with { PendingCommands = [] },
            _ => throw new InvalidOperationException()
        };
        var exception = Assert.Throws<ScenarioContractException>(() => h.Factory.Restore(scenario, state, Harness.Frame(0)));
        Assert.Equal("SNAPSHOT_COMMANDS", exception.Code);
    }

    [Fact]
    public void DirectScenarioDefinitionsCannotApplyTheSameEffectKeyTwice()
    {
        var h = new Harness();
        var scenario = h.Scenario();
        var objective = scenario.Definition.Objectives[0];
        var effect = new EffectDefinition("test_counter.bonus", "duplicate", ContractJson.Element(new { amount = 1 }));
        scenario = scenario with { Definition = scenario.Definition with
        {
            Objectives = [objective with { SuccessEffects = [effect, effect] }]
        }};
        var exception = Assert.Throws<ScenarioContractException>(() => h.Factory.Create(scenario, Harness.Options(), Harness.Frame(0)));
        Assert.Equal("EFFECT_DUPLICATE_KEY", exception.Code);
    }

    [Fact]
    public void UndefinedSessionPhaseIsRejectedOnRestore()
    {
        var h = new Harness();
        var scenario = h.Scenario();
        using var sim = h.Start(scenario);
        var exception = Assert.Throws<ScenarioContractException>(() => h.Factory.Restore(
            scenario, sim.Snapshot with { Phase = (SessionPhase)1234 }, Harness.Frame(0)));
        Assert.Equal("SNAPSHOT_INVALID", exception.Code);
    }
}
