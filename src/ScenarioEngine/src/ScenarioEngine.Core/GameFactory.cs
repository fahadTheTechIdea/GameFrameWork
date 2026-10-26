using ScenarioEngine.Contracts;

namespace ScenarioEngine.Core;

/// <summary>Creates the common game lifecycle for any explicitly registered layer.</summary>
public sealed class GameFactory(IScenarioSessionFactory sessions, IGameLayerRegistry layers) : IGameFactory
{
    public IGame Create(ValidatedScenario scenario)
    {
        var identity = scenario.Identity;
        if (!layers.TryResolve(identity.LayerId, identity.LayerVersion, out _))
            throw new ScenarioContractException("LAYER_UNAVAILABLE", "Required layer/version is not registered.");
        return new RegisteredGame(sessions, new(identity.LayerId, identity.LayerVersion));
    }
    private sealed class RegisteredGame(IScenarioSessionFactory sessions, LayerIdentity identity) : GameBase(sessions)
    {
        protected override LayerIdentity GameLayer => identity;
    }
}
