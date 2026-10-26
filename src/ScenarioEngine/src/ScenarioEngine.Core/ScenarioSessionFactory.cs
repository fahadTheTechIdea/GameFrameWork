using ScenarioEngine.Contracts;

namespace ScenarioEngine.Core;

public sealed class ScenarioSessionFactory(IGameLayerRegistry layers, IContractValidator validator,
    IEngineReporter reporter) : IScenarioSessionFactory
{
    public IScenarioSimulation Create(ValidatedScenario scenario, ScenarioStartOptions options, WorldFrame initialWorld)
    {
        var layer = Resolve(scenario);
        return new ScenarioEngine(scenario, options, initialWorld, layer, validator, reporter, null);
    }
    public IScenarioSimulation Restore(ValidatedScenario scenario, SessionSnapshot saved, WorldFrame restoredWorld)
    {
        if (scenario.Identity != saved.Identity)
            throw new ScenarioContractException("SAVE_CONTENT_MISMATCH", "Save content identity does not match.");
        if (saved.Phase is SessionPhase.Faulted or SessionPhase.Disposed or SessionPhase.Transitioning)
            throw new ScenarioContractException("SAVE_PHASE", "This snapshot cannot be restored.");
        return new ScenarioEngine(scenario, new(saved.SessionId, saved.Seed, saved.Controllers),
            restoredWorld, Resolve(scenario), validator, reporter, saved);
    }
    private IGameLayer Resolve(ValidatedScenario scenario)
    {
        if (!layers.TryResolve(scenario.Identity.LayerId, scenario.Identity.LayerVersion, out var layer))
            throw new ScenarioContractException("LAYER_UNAVAILABLE", "Required game layer is not registered.");
        return layer;
    }
}
