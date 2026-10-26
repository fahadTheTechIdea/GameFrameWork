using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public enum ControlRuleArbitration : byte { Exclusive = 1 }
    public enum ControlTargetKind : byte { Entity = 1, Company = 2 }

    public readonly struct ControlBinding : IEquatable<ControlBinding>
    {
        public ControlBinding(EntityId entity, Code capability)
            : this(ControlTargetKind.Entity, entity, capability) { }
        private ControlBinding(ControlTargetKind kind, EntityId entity, Code capability)
        {
            if (capability.IsNone || (kind == ControlTargetKind.Entity ? !entity.IsValid : kind != ControlTargetKind.Company || entity != EntityId.None))
                throw new ArgumentException("explicit typed control target and named capability required");
            ContractIdentity.ValidateId(capability.Id, "automation capability");
            Kind = kind; Entity = entity; Capability = capability;
        }
        /// <summary>The rule beneficiary is the actual company, never a second independently redirected seat.</summary>
        public static ControlBinding ForCompany(Code capability) => new ControlBinding(ControlTargetKind.Company, EntityId.None, capability);
        public ControlTargetKind Kind { get; }
        public EntityId Entity { get; }
        public Code Capability { get; }
        public bool Equals(ControlBinding other) => Kind == other.Kind && Entity == other.Entity && Capability == other.Capability;
        public override bool Equals(object obj) => obj is ControlBinding other && Equals(other);
        public override int GetHashCode() => Kind.GetHashCode() ^ Entity.GetHashCode() ^ Capability.GetHashCode();
        internal static ControlBinding Sync(IStateSync sync, ControlBinding binding)
        {
            ControlTargetKind kind = binding.Kind; EntityId entity = binding.Entity; string capability = binding.Capability.Id;
            sync.Enum(ref kind); sync.Entity(ref entity); sync.String(ref capability);
            try { return sync.Reading ? new ControlBinding(kind, entity, new Code(capability)) : binding; }
            catch (ArgumentException ex) { throw new CorruptStateException("invalid control binding: " + ex.Message); }
        }
    }

    public readonly struct ControlAction : IEquatable<ControlAction>
    {
        public ControlAction(bool enabled, Fix target, Code unit)
        {
            if (unit.IsNone) throw new ArgumentException("control action requires an explicit unit");
            ContractIdentity.ValidateId(unit.Id, "control action unit");
            Enabled = enabled; Target = target; Unit = unit;
        }
        public bool Enabled { get; }
        public Fix Target { get; }
        public Code Unit { get; }
        public bool Equals(ControlAction other) => Enabled == other.Enabled && Target == other.Target && Unit == other.Unit;
        public override bool Equals(object obj) => obj is ControlAction other && Equals(other);
        public override int GetHashCode() => Enabled.GetHashCode() ^ Target.GetHashCode() ^ Unit.GetHashCode();
        internal static ControlAction Sync(IStateSync sync, ControlAction current)
        {
            bool enabled = current.Enabled; Fix target = current.Target; string unit = current.Unit.Id;
            sync.Bool(ref enabled); sync.Fix(ref target); sync.String(ref unit);
            if (!sync.Reading) return current;
            try { return new ControlAction(enabled, target, new Code(unit)); }
            catch (ArgumentException ex) { throw new CorruptStateException("invalid saved control action: " + ex.Message); }
        }
        public static ControlAction Read(JsonReader reader)
        {
            if (reader == null) throw new ContentException("explicit control action required");
            JsonValue enabled = reader.Value("enabled");
            if (enabled == null || enabled.Kind != JsonKind.Bool || !reader.Has("target"))
                throw reader.Error("enabled boolean and target must be explicit");
            Fix target = reader.Fix("target"); Code unit = new Code(reader.String("unit")); reader.End();
            try { return new ControlAction(enabled.Bool, target, unit); }
            catch (ArgumentException ex) { throw reader.Error(ex.Message); }
        }
    }

    public sealed class ControlSignalInput
    {
        public ControlSignalInput(string id, IAutomationSignalContent signal)
        {
            ContractIdentity.ValidateId(id, "rule input");
            Id = id; Signal = signal ?? throw new ArgumentNullException(nameof(signal));
        }
        public string Id { get; }
        public IAutomationSignalContent Signal { get; }
    }

    public interface IControlRule : IContentDefinition
    {

        IControlInputRecoveryPolicy InputRecovery { get; }
        IControlActionPolicy ActionPolicy { get; }
        IReadOnlyList<ControlSignalInput> Inputs { get; }
        IReadOnlyDictionary<string, IControlThresholdDefinition> Thresholds { get; }
        IReadOnlyDictionary<string, bool> InitialState { get; }
        bool InitialLatch { get; }
        GameDuration Dwell { get; }
        GameDuration Cooldown { get; }
        ControlAction WhenTrue { get; }
        ControlAction WhenFalse { get; }
        ControlRuleArbitration Arbitration { get; }
        ControlEvaluation Evaluate(ControlEvaluationContext context);
    }

    /// <summary>Detached typed inputs and named prior hysteresis facts; no mutable World or providers.</summary>
    public sealed class ControlEvaluationContext
    {
        internal ControlEvaluationContext(IReadOnlyDictionary<string, AutomationSignalSnapshot> signals,
            IReadOnlyDictionary<string, bool> previous, IReadOnlyDictionary<string, ControlThresholdBand> thresholds)
        { Signals = AutomationFacts.Copy(signals); PreviousState = AutomationFacts.Copy(previous); Thresholds = AutomationFacts.Copy(thresholds); }
        public IReadOnlyDictionary<string, AutomationSignalSnapshot> Signals { get; }
        public IReadOnlyDictionary<string, bool> PreviousState { get; }
        public IReadOnlyDictionary<string, ControlThresholdBand> Thresholds { get; }
    }

    public sealed class ControlEvaluation
    {
        public ControlEvaluation(bool condition, IReadOnlyDictionary<string, bool> state, Code blocker)
        { Condition = condition; State = AutomationFacts.Copy(state); Blocker = blocker; }
        public bool Condition { get; }
        public IReadOnlyDictionary<string, bool> State { get; }
        public Code Blocker { get; }
    }

    internal static class AutomationFacts
    {
        internal static IReadOnlyDictionary<string, T> Copy<T>(IReadOnlyDictionary<string, T> values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            var copy = new SortedDictionary<string, T>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, T> value in values)
            {
                ContractIdentity.ValidateId(value.Key, "automation state/input");
                copy.Add(value.Key, value.Value);
            }
            return new ReadOnlyDictionary<string, T>(copy);
        }
    }

    public sealed class AutomationSettings
    {
        public AutomationSettings(string profile, bool enabled, ControlBinding target,
            IReadOnlyDictionary<string, ControlBinding> inputs, IReadOnlyDictionary<string, ControlThresholdBand> thresholds,
            ControlAction whenTrue, ControlAction whenFalse)
        {
            ContractIdentity.ValidateId(profile, "control rule profile");
            if (target.Capability.IsNone || (target.Kind == ControlTargetKind.Entity ? !target.Entity.IsValid : target.Kind != ControlTargetKind.Company || target.Entity != EntityId.None))
                throw new ArgumentException("explicit typed control target required");
            Profile = profile; Enabled = enabled; Target = target; Inputs = AutomationFacts.Copy(inputs);
            Thresholds = AutomationFacts.Copy(thresholds);
            if (whenTrue.Unit.IsNone || whenFalse.Unit.IsNone) throw new ArgumentException("both action branches must be explicit");
            WhenTrue = whenTrue; WhenFalse = whenFalse;
            foreach (ControlBinding input in Inputs.Values)
                if (input.Kind != ControlTargetKind.Entity || !input.Entity.IsValid || input.Capability.IsNone) throw new ArgumentException("explicit entity signal bindings required");
        }
        public string Profile { get; }
        public bool Enabled { get; }
        public ControlBinding Target { get; }
        public IReadOnlyDictionary<string, ControlBinding> Inputs { get; }
        public IReadOnlyDictionary<string, ControlThresholdBand> Thresholds { get; }
        public ControlAction WhenTrue { get; }
        public ControlAction WhenFalse { get; }
        // Binding resolution only; EntityStore owns identities and EntityObservation owns scopes/deduplication.
        internal IEnumerable<Entity> ResolveParticipants(World world)
        {
            if (Target.Kind == ControlTargetKind.Entity) yield return world.Entities.Get(Target.Entity);
            foreach (ControlBinding input in Inputs.Values) yield return world.Entities.Get(input.Entity);
        }
        internal static AutomationSettings Sync(IStateSync sync, AutomationSettings current)
        {
            if (!sync.Reading && current == null) throw new ArgumentNullException(nameof(current), "automation settings are required for a command or saved record");
            string profile = current?.Profile; bool enabled = current?.Enabled ?? false;
            sync.String(ref profile); sync.Bool(ref enabled);
            ControlBinding target = ControlBinding.Sync(sync, current?.Target ?? default);
            int count = sync.Count(current?.Inputs.Count ?? 0);
            if (!sync.Reading)
            {
                foreach (KeyValuePair<string, ControlBinding> input in current.Inputs)
                {
                    string key = input.Key; sync.String(ref key); ControlBinding.Sync(sync, input.Value);
                }
                SyncThresholds(sync, current.Thresholds);
                ControlAction.Sync(sync, current.WhenTrue); ControlAction.Sync(sync, current.WhenFalse);
                return current;
            }
            var inputs = new SortedDictionary<string, ControlBinding>(StringComparer.Ordinal); string previous = null;
            for (int i = 0; i < count; i++)
            {
                string key = null; sync.String(ref key);
                if (key == null || previous != null && string.CompareOrdinal(previous, key) >= 0)
                    throw new CorruptStateException("automation inputs must have unique canonical identities");
                inputs.Add(key, ControlBinding.Sync(sync, default)); previous = key;
            }
            var thresholds = SyncThresholds(sync, null);
            var whenTrue = ControlAction.Sync(sync, default); var whenFalse = ControlAction.Sync(sync, default);
            try { return new AutomationSettings(profile, enabled, target, inputs, thresholds, whenTrue, whenFalse); }
            catch (ArgumentException ex) { throw new CorruptStateException("invalid automation settings: " + ex.Message); }
        }
        private static IReadOnlyDictionary<string, ControlThresholdBand> SyncThresholds(IStateSync sync,
            IReadOnlyDictionary<string, ControlThresholdBand> current)
        {
            int count = sync.Count(current?.Count ?? 0);
            if (!sync.Reading)
            {
                foreach (var row in current) { string key = row.Key; sync.String(ref key); ControlThresholdBand.Sync(sync, row.Value); }
                return current;
            }
            var values = new SortedDictionary<string, ControlThresholdBand>(StringComparer.Ordinal); string previous = null;
            for (int i = 0; i < count; i++)
            {
                string key = null; sync.String(ref key);
                if (key == null || previous != null && string.CompareOrdinal(previous, key) >= 0)
                    throw new CorruptStateException("automation thresholds must have unique canonical identities");
                values.Add(key, ControlThresholdBand.Sync(sync, default)); previous = key;
            }
            try { return AutomationFacts.Copy(values); }
            catch (ArgumentException ex) { throw new CorruptStateException("invalid threshold identity: " + ex.Message); }
        }
    }

    public sealed class AutomationRuleSnapshot
    {
        internal AutomationRuleSnapshot(PlayerId company, string id, AutomationSettings settings,
            IReadOnlyDictionary<string, bool> state, bool latch, ControlAction? appliedAction, bool? candidate, long since,
            long cooldown, bool? pending, long due, long lastApplied, Code blocker, string waitingInput)
        {
            Company = company; Id = id; Settings = settings; State = AutomationFacts.Copy(state);
            Latch = latch; LastAppliedAction = appliedAction; Candidate = candidate; CandidateSince = since;
            CooldownUntil = cooldown; PendingDecision = pending; PendingAt = due; LastAppliedAt = lastApplied; Blocker = blocker;
            WaitingInput = waitingInput;
        }
        public PlayerId Company { get; }
        public string Id { get; }
        public AutomationSettings Settings { get; }
        public IReadOnlyDictionary<string, bool> State { get; }
        public bool EffectiveEnabled => Settings.Enabled && Blocker.IsNone;
        public string WaitingInput { get; }
        public bool WaitingForInputs => WaitingInput != null;
        public bool Suspended => Settings.Enabled && !Blocker.IsNone && !WaitingForInputs;
        public bool Latch { get; }
        public bool HasApplied => LastAppliedAction.HasValue;
        public ControlAction? LastAppliedAction { get; }
        public bool? Candidate { get; }
        public long CandidateSince { get; }
        public long CooldownUntil { get; }
        public bool? PendingDecision { get; }
        public long PendingAt { get; }
        public long LastAppliedAt { get; }
        public Code Blocker { get; }
    }

    public interface IAutomationRules
    {
        World World { get; }
        Code CheckConfigure(PlayerId company, string id, AutomationSettings settings);
        void Configure(PlayerId company, string id, AutomationSettings settings);
        Code CheckRemove(PlayerId company, string id);
        void Remove(PlayerId company, string id);
        bool TryGet(PlayerId company, string id, out AutomationRuleSnapshot snapshot);
        void CopyRules(PlayerId viewer, IList<AutomationRuleSnapshot> destination);
    }

    public static class AutomationCodes
    {
        public static readonly Code ReplacementBinding = new Code("automation.replacement-binding");
        public static readonly Code InvalidSettings = new Code("automation.invalid-settings");
        public static readonly Code UnitMismatch = new Code("automation.unit-mismatch");
        public static readonly Code Conflict = new Code("automation.target-conflict");
        public static readonly Code NotSignalSource = new Code("automation.not-signal-source");
    }
}
