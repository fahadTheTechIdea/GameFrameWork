using Counter.GameLayer;
using ScenarioEngine.Contracts;

namespace ScenarioEngine.Tests;

public sealed class GameLifecycleEnhancementTests
{
    [Fact]
    public void FailedInitializationDisposesCandidateAndAllowsRetry()
    {
        var h = new Harness();
        var scenario = h.Scenario();
        var factory = new RetryFactory(h.Factory);
        using var game = new CounterGame(factory);
        var start = new GameStart(scenario, Harness.Options(), Harness.Frame(0));
        Assert.Throws<InvalidOperationException>(() => game.Init(start));
        Assert.True(factory.CandidateDisposed);
        game.Init(start);
        Assert.Equal(SessionPhase.Running, game.Snapshot.Phase);
    }
    private sealed class RetryFactory(IScenarioSessionFactory inner) : IScenarioSessionFactory
    {
        private int _attempt;
        public bool CandidateDisposed { get; private set; }
        public IScenarioSimulation Create(ValidatedScenario scenario, ScenarioStartOptions options, WorldFrame world)
        {
            var simulation = inner.Create(scenario, options, world);
            return ++_attempt == 1 ? new StartFailure(simulation, () => CandidateDisposed = true) : simulation;
        }
        public IScenarioSimulation Restore(ValidatedScenario scenario, SessionSnapshot saved, WorldFrame world) => inner.Restore(scenario, saved, world);
    }
    private sealed class StartFailure(IScenarioSimulation inner, Action disposed) : IScenarioSimulation
    {
        public SessionSnapshot Snapshot => inner.Snapshot;
        public void Start() => throw new InvalidOperationException("Injected startup failure.");
        public void Pause() => inner.Pause();
        public void Resume() => inner.Resume();
        public AdmissionResult Enqueue(CommandEnvelope command) => inner.Enqueue(command);
        public AdmissionResult CancelQueued(string controller, string command) => inner.CancelQueued(controller, command);
        public StepResult Step(TickInput input) => inner.Step(input);
        public void Dispose() { inner.Dispose(); disposed(); }
    }
}
