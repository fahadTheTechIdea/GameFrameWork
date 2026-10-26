using System.Collections.Immutable;
using System.Text.Json;

namespace ScenarioEngine.Contracts;

/// <summary>Shared format invariants for runtime creation and save decoding.</summary>
public static class SessionSnapshotRules
{
    public static void Validate(SessionSnapshot state)
    {
        if (state is null || state.Identity is null || state.Layer is null ||
            state.Controllers is null || state.Objectives is null || state.AppliedEffects is null ||
            state.Receipts is null || state.RandomStreams is null || state.PendingCommands.IsDefault ||
            !Enum.IsDefined(state.Phase) || state.Tick < 0 || state.Tick > long.MaxValue - 8 ||
            state.WorldGeneration < 0 || state.LastObservationSequence < 0 ||
            state.LastEventSequence < 0 || state.LastIngressSequence < 0 ||
            !Token(state.SessionId) || state.Controllers.Count is < 1 or > 16 ||
            state.Controllers.Any(c => !Token(c)) || state.Layer.Data.ValueKind != JsonValueKind.Object)
            throw new ScenarioContractException("SNAPSHOT_INVALID", "Invalid required state, phase, counters or controllers.");
        var identity = state.Identity;
        if (new[] { identity.PackId, identity.PackVersion, identity.ContentDigest, identity.ScenarioId, identity.LayerId, identity.LayerVersion }.Any(v => !Token(v)))
            throw new ScenarioContractException("SNAPSHOT_INVALID", "Missing content identity.");
        if (state.Objectives.Any(p => !Token(p.Key) || !Enum.IsDefined(p.Value)))
            throw new ScenarioContractException("SNAPSHOT_INVALID", "Invalid objective status.");
        if ((state.Phase == SessionPhase.Ended) != (state.EndingId is not null && state.OutcomeId is not null) ||
            (state.EndingId is null) != (state.OutcomeId is null))
            throw new ScenarioContractException("SNAPSHOT_INVALID", "Ending identity does not match phase.");
        if (state.RandomStreams.Count > 4096 || state.RandomStreams.Any(p =>
            string.IsNullOrWhiteSpace(p.Key) || p.Key.Length > 160 ||
            p.Value is null || (p.Value.Increment & 1) == 0 || p.Value.DrawCount < 0))
            throw new ScenarioContractException("SNAPSHOT_RANDOM", "Invalid or excessive random streams.");

        if (state.Receipts.Count > 100000 || state.LastIngressSequence != state.Receipts.Count ||
            state.Receipts.Values.Select(r => r?.Sequence).Distinct().Count() != state.Receipts.Count)
            throw new ScenarioContractException("SNAPSHOT_COMMANDS", "Invalid receipt count or ingress sequence.");
        foreach (var pair in state.Receipts)
        {
            var receipt = pair.Value;
            if (receipt is null || pair.Key != receipt.CommandId || !Token(pair.Key) ||
                !Enum.IsDefined(receipt.Status) || receipt.Sequence is < 1 ||
                receipt.Sequence > state.LastIngressSequence || !Token(receipt.Code) ||
                !state.Controllers.Any(c => StringComparer.Ordinal.Equals(c, receipt.ControllerId)) ||
                receipt.Fingerprint is null || receipt.Fingerprint.Length != 64 ||
                receipt.Fingerprint.Any(c => !Uri.IsHexDigit(c)))
                throw new ScenarioContractException("SNAPSHOT_COMMANDS", "Invalid command receipt.");
        }
        if (state.PendingCommands.Any(q => q is null || q.Command is null) ||
            state.PendingCommands.Length > 128 ||
            state.PendingCommands.Select(q => q.Command.CommandId).Distinct(StringComparer.Ordinal).Count() != state.PendingCommands.Length ||
            state.PendingCommands.Select(q => q.Sequence).Distinct().Count() != state.PendingCommands.Length ||
            state.PendingCommands.GroupBy(q => q.Command.TargetTick).Any(g => g.Count() > 32))
            throw new ScenarioContractException("SNAPSHOT_COMMANDS", "Duplicate or excessive pending commands.");
        foreach (var queued in state.PendingCommands)
        {
            var command = queued.Command;
            if (command is null || !Token(command.CommandId) || !Token(command.ControllerId) ||
                command.WorldGeneration != state.WorldGeneration ||
                command.TargetTick <= state.Tick || command.TargetTick > state.Tick + 8 ||
                !state.Receipts.TryGetValue(command.CommandId, out var receipt) ||
                receipt.Sequence != queued.Sequence || receipt.Status != CommandStatus.Queued ||
                receipt.ControllerId != command.ControllerId || !Token(command.Operation) ||
                !WireJsonRules.Validate(command.Payload, requireObject: true).IsValid ||
                ContractJson.Hash(command) != receipt.Fingerprint)
                throw new ScenarioContractException("SNAPSHOT_COMMANDS", "Pending order differs from its admission receipt.");
        }
        var pendingIds = state.PendingCommands.Select(q => q.Command.CommandId).ToHashSet(StringComparer.Ordinal);
        if (state.Receipts.Any(p => (p.Value.Status == CommandStatus.Queued) != pendingIds.Contains(p.Key)))
            throw new ScenarioContractException("SNAPSHOT_COMMANDS", "Queued receipt has no matching pending command.");
        if (state.Phase == SessionPhase.Ended && !state.PendingCommands.IsEmpty)
            throw new ScenarioContractException("SNAPSHOT_COMMANDS", "Ended sessions cannot keep queued orders.");
    }
    private static bool Token(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 128;
}
