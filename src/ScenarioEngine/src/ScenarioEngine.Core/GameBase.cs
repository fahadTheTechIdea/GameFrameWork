using ScenarioEngine.Contracts;

namespace ScenarioEngine.Core;

/// <summary>The common game lifecycle. Domain behavior is supplied by a registered game layer.</summary>
public abstract class GameBase(IScenarioSessionFactory sessions) : IGame
{
    private readonly object _lifecycleGate = new();
    private volatile IScenarioSimulation? _simulation;
    private bool _disposed;
    private bool _initializing;
    protected abstract LayerIdentity GameLayer { get; }
    public SessionSnapshot Snapshot => Simulation.Snapshot;
    private IScenarioSimulation Simulation => _simulation ?? throw new InvalidOperationException("Call Init first.");

    public void Init(GameStart start) => Initialize(start.Scenario, () => sessions.Create(start.Scenario, start.Options, start.InitialWorld));
    public void Restore(ValidatedScenario scenario, SessionSnapshot saved, WorldFrame restoredWorld) =>
        Initialize(scenario, () => sessions.Restore(scenario, saved, restoredWorld));

    private void Initialize(ValidatedScenario scenario, Func<IScenarioSimulation> create)
    {
        lock (_lifecycleGate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_simulation is not null || _initializing) throw new InvalidOperationException("Game is already initializing or initialized.");
            if (scenario.Identity.LayerId != GameLayer.Id || scenario.Identity.LayerVersion != GameLayer.Version)
                throw new ScenarioContractException("GAME_LAYER_MISMATCH", "Selected scenario does not belong to this game.");
            _initializing = true;
            IScenarioSimulation? candidate = null;
            try
            {
                candidate = create();
                if (candidate.Snapshot.Phase == SessionPhase.Ready) candidate.Start();
                _simulation = candidate;
                candidate = null; // Ownership transfers only after successful startup.
            }
            finally
            {
                _initializing = false;
                candidate?.Dispose();
            }
        }
    }
    public StepResult Update(TickInput input) => Simulation.Step(input);
    public AdmissionResult Submit(CommandEnvelope command) => Simulation.Enqueue(command);
    public AdmissionResult CancelQueued(string controllerId, string commandId) => Simulation.CancelQueued(controllerId, commandId);
    public void Pause() => Simulation.Pause();
    public void Resume() => Simulation.Resume();
    public void Dispose()
    {
        lock (_lifecycleGate)
        {
            if (_disposed) return;
            if (_initializing) throw new ScenarioContractException("GAME_BUSY", "Cannot dispose during initialization.");
            _simulation?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
