using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using Counter.GameLayer;
using ScenarioEngine.Contracts;
using ScenarioEngine.Core;

namespace ScenarioEngine.Tests;

public sealed class EngineTests
{
    [Fact]
    public void MotherGameInitializesUpdatesPausesAndCleansUp()
    {
        var h = new Harness();
        using var game = new CounterGame(h.Factory);
        Assert.Throws<InvalidOperationException>(() => game.Update(Harness.Input(1)));
        var start = new GameStart(h.Scenario(), Harness.Options(), Harness.Frame(0));
        game.Init(start);
        Assert.Equal(SessionPhase.Running, game.Snapshot.Phase);
        Assert.Throws<InvalidOperationException>(() => game.Init(start));
        game.Pause();
        Assert.False(game.Update(Harness.Input(1)).Committed);
        Assert.True(game.Submit(Harness.Increment()).Accepted);
        game.Resume();
        Assert.True(game.Update(Harness.Input(1)).Committed);
        Assert.Equal(1, Harness.Value(game.Snapshot));
        game.Dispose();
        Assert.Equal(SessionPhase.Disposed, game.Snapshot.Phase);
    }

    [Fact]
    public void RegisteredUnrelatedLayerReachesEndingThroughOrders()
    {
        var h = new Harness();
        using var simulation = h.Start();
        for (var tick = 1; tick <= 3; tick++)
        {
            Assert.True(simulation.Enqueue(Harness.Increment(tick, "cmd-" + tick)).Accepted);
            Assert.True(simulation.Step(Harness.Input(tick)).Committed);
        }
        Assert.Equal("goal_reached", simulation.Snapshot.EndingId);
        Assert.Equal(ObjectiveStatus.Completed, simulation.Snapshot.Objectives["count_three"]);
        Assert.False(simulation.Step(Harness.Input(4)).Committed);
        Assert.Single(h.Reporter.Audits, a => a.Operation == "ScenarioEnded");
    }

    [Fact]
    public void DuplicateCommandDoesNotSpendTwiceAndConflictsAreRejected()
    {
        var h = new Harness();
        using var sim = h.Start();
        var command = Harness.Increment();
        var first = sim.Enqueue(command);
        Assert.Equal(first, sim.Enqueue(command));
        Assert.Equal("COMMAND_ID_CONFLICT", sim.Enqueue(command with { Payload = ContractJson.Element(new { amount = 2 }) }).Code);
        Assert.True(sim.Step(Harness.Input(1)).Committed);
        Assert.Equal(1, Harness.Value(sim.Snapshot));
        Assert.Equal("APPLIED", sim.Enqueue(command).Code);
        Assert.True(sim.Step(Harness.Input(2)).Committed);
        Assert.Equal(1, Harness.Value(sim.Snapshot));
    }

    [Theory]
    [InlineData("intruder", 1, 1, "COMMAND_DENIED")]
    [InlineData("player", 0, 1, "COMMAND_LATE")]
    [InlineData("player", 9, 1, "COMMAND_TOO_EARLY")]
    [InlineData("player", 1, -1, "COMMAND_INVALID")]
    [InlineData("player", 1, 11, "COMMAND_INVALID")]
    public void CommandAdmissionEnforcesContracts(string controller, long tick, int amount, string code)
    {
        using var sim = new Harness().Start();
        Assert.Equal(code, sim.Enqueue(Harness.Increment(tick, amount: amount, controller: controller)).Code);
        Assert.Equal(0, Harness.Value(sim.Snapshot));
        Assert.Empty(sim.Snapshot.PendingCommands);
    }

    [Fact]
    public void CancelQueuedChecksOwnerAndIsIdempotent()
    {
        using var sim = new Harness().Start();
        sim.Pause();
        sim.Enqueue(Harness.Increment());
        Assert.Equal("COMMAND_DENIED", sim.CancelQueued("other", "one").Code);
        Assert.True(sim.CancelQueued("player", "one").Accepted);
        Assert.True(sim.CancelQueued("player", "one").Accepted);
        sim.Resume();
        Assert.True(sim.Step(Harness.Input(1)).Committed);
        Assert.Equal(0, Harness.Value(sim.Snapshot));
    }

    [Fact]
    public void QueueAndPerTickLimitsAreEnforced()
    {
        using var sim = new Harness().Start();
        for (var i = 0; i < 32; i++) Assert.True(sim.Enqueue(Harness.Increment(1, "cmd-" + i)).Accepted);
        Assert.Equal("COMMAND_QUEUE_FULL", sim.Enqueue(Harness.Increment(1, "extra")).Code);
    }

    [Fact]
    public void RejectedDomainCommandDoesNotChangeState()
    {
        var h = new Harness();
        var scenario = h.Scenario() with
        {
            Definition = h.Scenario().Definition with
            {
                ClosingCondition = new CompareCondition("core.tick", "gt", FactValue.From(100L)),
                LayerData = ContractJson.Element(new { initialValue = 100 })
            }
        };
        using var sim = h.Start(scenario);
        // Authoritative native input can put the domain at its limit; the command then fails atomically.
        sim.Enqueue(Harness.Increment());
        var frame = new WorldFrame(0, 1, [new(1, "test_counter.adjustment", null, ContractJson.Element(new { delta = 100 }))]);
        for (var i = 2; i <= 9; i++)
            frame = frame with { Observations = frame.Observations.Add(new(i, "test_counter.adjustment", null, ContractJson.Element(new { delta = 100 }))) };
        var step = sim.Step(new(1, frame));
        Assert.True(step.Committed);
        Assert.Equal(1000, Harness.Value(sim.Snapshot));
        Assert.Equal(CommandStatus.Rejected, sim.Snapshot.Receipts["one"].Status);
    }

    [Fact]
    public void LayerExceptionRollsBackCandidateEventsRandomAndTick()
    {
        var failure = new InvalidOperationException("Injected layer failure after mutation.");
        var h = new Harness(new DecoratingLayer((result, context) =>
        {
            context.Random.NextUInt32("temporary");
            throw failure;
        }));
        using var sim = h.Start();
        sim.Enqueue(Harness.Increment());
        var step = sim.Step(Harness.Input(1));
        Assert.False(step.Committed);
        Assert.Equal(SessionPhase.Faulted, sim.Snapshot.Phase);
        Assert.Equal(0, sim.Snapshot.Tick);
        Assert.Equal(0, Harness.Value(sim.Snapshot));
        Assert.Empty(sim.Snapshot.RandomStreams);
        Assert.Empty(step.Events);
        Assert.Same(failure, Assert.Single(h.Reporter.Notices, n => n.Severity == Severity.Error).Exception);
    }

    [Fact]
    public void ReentrantUpdateIsRefusedWithoutMutatingOuterTick()
    {
        IScenarioSimulation? sim = null;
        StepResult? nested = null;
        var h = new Harness(new DecoratingLayer((result, _) =>
        {
            nested = sim!.Step(Harness.Input(1));
            return result;
        }));
        using var owned = h.Start();
        sim = owned;
        Assert.True(sim.Step(Harness.Input(1)).Committed);
        Assert.Equal("ENGINE_BUSY", nested!.Code);
        Assert.Equal(1, sim.Snapshot.Tick);
    }

    [Fact]
    public void BadWorldSequenceAndGenerationDoNotAdvanceSession()
    {
        using var sim = new Harness().Start();
        var wrongSequence = new WorldFrame(0, 1, [new(2, "test_counter.adjustment", null, ContractJson.Element(new { delta = 1 }))]);
        Assert.Equal("WORLD_FRAME_GAP", sim.Step(new(1, wrongSequence)).Code);
        Assert.Equal("WORLD_GENERATION", sim.Step(new(1, Harness.Frame(1, 1))).Code);
        Assert.Equal(SessionPhase.Running, sim.Snapshot.Phase);
        Assert.Equal(0, sim.Snapshot.Tick);
    }

    [Fact]
    public void InitialWorldIsValidatedBeforeGameStarts()
    {
        var h = new Harness();
        var bad = new WorldFrame(0, 0, [new(4, "test_counter.adjustment", null, ContractJson.Element(new { delta = 1 }))]);
        var exception = Assert.Throws<ScenarioContractException>(() => h.Factory.Create(h.Scenario(), Harness.Options(), bad));
        Assert.Equal("WORLD_FRAME_GAP", exception.Code);
    }

    [Fact]
    public void SameInputsProduceIdenticalHashAndChangedObservationDiverges()
    {
        var h = new Harness();
        var scenario = h.Scenario();
        using var a = h.Start(scenario);
        using var b = h.Start(scenario);
        using var changed = h.Start(scenario);
        a.Enqueue(Harness.Increment()); b.Enqueue(Harness.Increment()); changed.Enqueue(Harness.Increment());
        Assert.Equal(a.Step(Harness.Input(1)).RuleStateHash, b.Step(Harness.Input(1)).RuleStateHash);
        var frame = new WorldFrame(0, 1, [new(1, "test_counter.adjustment", null, ContractJson.Element(new { delta = 1 }))]);
        Assert.NotEqual(ScenarioEngine.Core.ScenarioEngine.StateHash(a.Snapshot), changed.Step(new(1, frame)).RuleStateHash);
    }

    [Fact]
    public void PcgMatchesPublishedReferenceVector()
    {
        var state = PcgRandomStreams.Initialize(42, 54);
        uint[] expected = [0xa15c02b7, 0x7b47f409, 0xba1d3330, 0x83d2f293, 0xbfa4784b, 0xcbed606e];
        foreach (var value in expected) Assert.Equal(value, PcgRandomStreams.Next(state, out state));
        Assert.Equal(6, state.DrawCount);
    }

    [Fact]
    public void PayloadSurvivesCallerJsonDocumentDisposal()
    {
        using var sim = new Harness().Start();
        using (var doc = JsonDocument.Parse("""{"amount":1}"""))
            Assert.True(sim.Enqueue(Harness.Increment() with { Payload = doc.RootElement }).Accepted);
        Assert.True(sim.Step(Harness.Input(1)).Committed);
    }
}
