using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Detached facts from a declared local signal; no source/provider handle.</summary>
    public readonly struct ExecutionGateConditionSnapshot
    {
        public string Id { get; }
        public Code Source { get; }
        public AutomationSignalSnapshot Signal { get; }
        public bool Ready { get; }
        public Code Blocker { get; }
        public ExecutionGateConditionSnapshot(string id, Code source, AutomationSignalSnapshot signal, bool ready, Code blocker)
        { Id = id; Source = source; Signal = signal; Ready = ready; Blocker = blocker; }
    }
    public sealed class ExecutionGateSnapshot
    {
        public Code Capability { get; }
        public bool Applies { get; }
        public Code Blocker { get; }
        public IReadOnlyList<ExecutionGateConditionSnapshot> Conditions { get; }
        public ExecutionGateSnapshot(Code capability, bool applies, Code blocker, IEnumerable<ExecutionGateConditionSnapshot> conditions)
        {
            Capability = capability; Applies = applies; Blocker = blocker;
            if (conditions == null) throw new ArgumentNullException(nameof(conditions));
            var copy = new List<ExecutionGateConditionSnapshot>(conditions);
            copy.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id)); Conditions = copy.AsReadOnly();
        }
    }
    public sealed class ExecutionGateEvaluation
    {
        public Code Blocker { get; }
        public IReadOnlyList<ExecutionGateSnapshot> Gates { get; }
        // Only the gate owner publishes this exclusively owned, complete facts list.
        // Unlike the public snapshot input above, no caller-owned collection crosses this boundary.
        internal ExecutionGateEvaluation(Code blocker, List<ExecutionGateSnapshot> gates)
        { Blocker = blocker; Gates = gates.AsReadOnly(); }
    }
    internal static class ExecutionGateContracts
    {
        internal static ComponentDef ValidateReusable(World world, INamedCapabilityDefinition definition, Type runtime,
            int codecVersion, Entity entity)
        {
            if (!(definition is ComponentDef part) || !part.IsFrozen || !world.Content.Owns(part.Owner) ||
                part.ComponentType != runtime || part.StateCodec.SchemaVersion != codecVersion ||
                !ReferenceEquals(part.StateCodec, world.Content.ComponentDeclaration(part.Key)?.Metadata) || definition.CapabilityId.IsNone ||
                !ReferenceEquals(part.Owner.Component<INamedCapabilityDefinition>(definition.CapabilityId), definition) ||
                entity != null && !ReferenceEquals(entity.Def, part.Owner))
                throw new ContentException("gate binding requires actual immutable owner, original runtime and registered codec");
            return part;
        }
        internal static readonly ExecutionGateEvaluation Open = new ExecutionGateEvaluation(Code.None, new List<ExecutionGateSnapshot>());
        internal static ExecutionGateEvaluation Read<TGate>(World world, Entity entity, Func<TGate, ExecutionGateSnapshot> read)
            where TGate : class, INamedEntityCapability
        {
            if (entity == null || !entity.Alive || !ReferenceEquals(world.Entities.Get(entity.Id), entity))
                throw new InvalidOperationException("foreign execution gate entity");
            List<(Code Identity, TGate Provider)> gates = null;
            foreach (var component in entity.Components)
                if (component is TGate gate)
                {
                    if (gates == null) gates = new List<(Code, TGate)>();
                    Code identity = EntityObservation.ReadProvider(world, gate, entity, () => gate.CapabilityId);
                    gates.Add((identity, gate));
                }
            if (gates == null) return Open;
            gates.Sort((a, b) => string.CompareOrdinal(a.Identity.Id, b.Identity.Id));
            var facts = new List<ExecutionGateSnapshot>(gates.Count); Code blocker = Code.None;
            foreach (var entry in gates)
            {
                TGate gate = entry.Provider; Code identity = entry.Identity;
                var snapshot = EntityObservation.ReadProvider(world, gate, entity, () =>
                {
                    ContractIdentity.ValidateId(identity.Id, "execution gate capability");
                    if (gate.CapabilityId != identity || !ReferenceEquals(gate.Entity, entity) || !ReferenceEquals(entity.Get<TGate>(identity), gate))
                        throw new InvalidOperationException("execution gate is not the actual named entity capability");
                    var value = read(gate);
                    if (gate.CapabilityId != identity || value == null || value.Capability != identity || value.Conditions == null ||
                        !value.Applies && (!value.Blocker.IsNone || value.Conditions.Count != 0))
                        throw new InvalidOperationException("execution gate returned malformed identity/applicability facts");
                    string previous = null; Code conditionBlocker = Code.None;
                    foreach (var condition in value.Conditions)
                    {
                        ContractIdentity.ValidateId(condition.Id, "gate condition");
                        if (condition.Source.IsNone || previous != null && string.CompareOrdinal(previous, condition.Id) >= 0 ||
                            condition.Ready != condition.Blocker.IsNone || condition.Ready && !condition.Signal.Available)
                            throw new InvalidOperationException("execution gate returned malformed condition facts");
                        IAutomationSignalContent signal = world.Content.Find<IAutomationSignalContent>(condition.Signal.Signal);
                        if (signal == null) throw new InvalidOperationException("gate reports an unknown signal");
                        var source = entity.Get<IAutomationSignalSource>(condition.Source);
                        if (!AutomationSignalContracts.Accepts(world, signal, source) ||
                            !condition.Signal.Available && condition.Blocker != condition.Signal.Blocker)
                            throw new InvalidOperationException("gate condition disagrees with actual named source or unavailable signal blocker");
                        signal.Validate(condition.Signal);
                        if (conditionBlocker.IsNone && !condition.Ready) conditionBlocker = condition.Blocker;
                        previous = condition.Id;
                    }
                    if (value.Conditions.Count > 0 && value.Blocker != conditionBlocker)
                        throw new InvalidOperationException("gate aggregate disagrees with complete condition facts");
                    return value;
                });
                facts.Add(snapshot);
                if (blocker.IsNone && !snapshot.Blocker.IsNone) blocker = snapshot.Blocker;
            }
            return new ExecutionGateEvaluation(blocker, facts);
        }
    }
}
