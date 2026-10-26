using System.Collections.Immutable;
using System.Text.Json;

namespace ScenarioEngine.Contracts;

public enum Severity { Information, Warning, Error }
public sealed record Diagnostic(string Code, string Message, string File = "", string Pointer = "", Severity Severity = Severity.Error);
public sealed record ValidationReport(ImmutableArray<Diagnostic> Diagnostics)
{
    public bool IsValid => !Diagnostics.Any(d => d.Severity == Severity.Error);
    public static ValidationReport Valid { get; } = new([]);
}
public enum FactKind { Boolean, Integer, String }
public sealed record FactValue(FactKind Kind, bool Boolean = false, long Integer = 0, string? Text = null)
{
    public static FactValue From(bool value) => new(FactKind.Boolean, Boolean: value);
    public static FactValue From(long value) => new(FactKind.Integer, Integer: value);
    public static FactValue From(string value) => new(FactKind.String, Text: value);
}
public sealed record FactDefinition(FactKind Kind, long? Minimum = null, long? Maximum = null);
public abstract record Condition;
public sealed record AllCondition(ImmutableArray<Condition> Children) : Condition;
public sealed record AnyCondition(ImmutableArray<Condition> Children) : Condition;
public sealed record NotCondition(Condition Child) : Condition;
public sealed record CompareCondition(string Fact, string Operation, FactValue Value) : Condition;
public enum ObjectiveStatus { Locked, Active, Completed, Failed, Abandoned }
public sealed record ObjectiveDependency(string ObjectiveId, ImmutableArray<ObjectiveStatus> AcceptedStatuses);
public sealed record EffectDefinition(string Type, string Key, JsonElement Arguments);
public sealed record ObjectiveDefinition(
    string Id, ImmutableArray<ObjectiveDependency> Dependencies, Condition ActivateWhen,
    Condition CompleteWhen, Condition FailWhen, ImmutableArray<EffectDefinition> SuccessEffects,
    ImmutableArray<EffectDefinition> FailureEffects);
public sealed record EndingDefinition(string Id, int Priority, Condition Condition, ImmutableArray<string> OutcomeTags);
public sealed record ScenarioIdentity(string PackId, string PackVersion, string ContentDigest,
    string ScenarioId, string LayerId, string LayerVersion);
public sealed record AnchorDefinition(string Id, PositionMm Position);
public sealed record ExitDefinition(string Id, string TargetZoneId, string AnchorId, Condition Condition, int DurationTicks);
public sealed record ZoneDefinition(string Id, string TitleKey, string SceneRole,
    ImmutableArray<AnchorDefinition> Anchors, ImmutableArray<ExitDefinition> Exits);
public sealed record ScenarioDefinition(
    string Id, Condition ClosingCondition, ImmutableArray<ObjectiveDefinition> Objectives,
    ImmutableArray<EndingDefinition> Endings, JsonElement LayerData,
    string TitleKey, string StartZoneId, ImmutableArray<ZoneDefinition> Zones);
public sealed record ValidatedScenario(ScenarioIdentity Identity, ScenarioDefinition Definition);
public sealed record ScenarioSummary(ScenarioIdentity Identity);
public sealed record LayerCapabilities(
    ImmutableDictionary<string, FactDefinition> Facts,
    ImmutableDictionary<string, JsonElement> Operations,
    ImmutableDictionary<string, JsonElement> Effects,
    ImmutableDictionary<string, JsonElement> Observations,
    JsonElement ContentSchema);
public interface IContractValidator
{
    ValidationReport Validate(JsonElement schema, JsonElement value, string file = "");
}
public interface IGameLayerRegistry
{
    bool TryResolve(string id, string exactVersion, out IGameLayer layer);
    ImmutableArray<LayerIdentity> ListInstalled();
}
public sealed record LayerIdentity(string Id, string Version);
public interface IScenarioCatalog
{
    ImmutableArray<ScenarioSummary> ListPlayable();
    bool TryResolve(ScenarioIdentity identity, out ValidatedScenario scenario);
}
public interface IScenarioSessionFactory
{
    IScenarioSimulation Create(ValidatedScenario scenario, ScenarioStartOptions options, WorldFrame initialWorld);
    IScenarioSimulation Restore(ValidatedScenario scenario, SessionSnapshot saved, WorldFrame restoredWorld);
}
