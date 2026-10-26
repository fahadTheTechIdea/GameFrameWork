using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Sole rule/intent state owner, also exposed through its engine-side policy contract.</summary>
    internal sealed class AutomationSystem : GameSystem, IAutomationRules, ICompositionValidator, IEntityReplacementReferenceOwner
    {
        private sealed class Record
        {
            internal PlayerId Company;
            internal string Id;
            internal AutomationSettings Settings;
            internal IReadOnlyDictionary<string, bool> State;
            internal bool Latch;
            internal ControlAction? AppliedAction;
            internal bool HasApplied => AppliedAction.HasValue;
            internal bool? Candidate, Pending;
            internal long Since = -1, Cooldown, Due = -1, AppliedAt = -1;
            internal Code Blocker;
            internal string WaitingInput;
            internal bool Waiting => WaitingInput != null;
        }
        private readonly SortedDictionary<(int, string), Record> _rules =
            new SortedDictionary<(int, string), Record>(Comparer<(int, string)>.Create((a, b) =>
                a.Item1 != b.Item1 ? a.Item1.CompareTo(b.Item1) : string.CompareOrdinal(a.Item2, b.Item2)));
        public World World { get; }
        public override string Id => AutomationModule.ModuleId;
        public override int StateSchemaVersion => 5;
        internal AutomationSystem(World world) { World = world; }
        public Code CheckReferenceReplacement(World world, Entity source, EntityDef destination)
        {
            if (!ReferenceEquals(world, World)) throw new ArgumentException("automation belongs to another world", nameof(world));
            foreach (Record record in _rules.Values)
            {
                IControlRule rule = Definition(record.Settings);
                if (record.Settings.Target.Kind == ControlTargetKind.Entity && record.Settings.Target.Entity == source.Id)
                {
                    if (record.Company != source.Owner) return Codes.NotOwned;
                    foreach (var action in new[] { record.Settings.WhenTrue, record.Settings.WhenFalse })
                    {
                        Code refusal = EntityObservation.ReadProvider(world, rule.ActionPolicy, source,
                            () => rule.ActionPolicy.CheckReplacement(world, record.Company, record.Settings.Target, source, destination, action),
                            observeTargetState: true);
                        if (!refusal.IsNone) return refusal;
                    }
                }
                foreach (var input in rule.Inputs)
                {
                    var binding = record.Settings.Inputs[input.Id];
                    if (binding.Entity != source.Id) continue;
                    if (record.Company != source.Owner) return Codes.NotOwned;
                    var current = source.Get<IAutomationSignalSource>(binding.Capability) as Component;
                    var next = destination.Component<IAutomationSignalDefinition>(binding.Capability) as ComponentDef;
                    var schema = AutomationSignalContracts.Schema(destination, binding.Capability);
                    if (current == null || next == null || current.Def.Key != next.Key || !Upgrades.CanRetainState(current.Def, next) ||
                        schema == null || !input.Signal.Accepts(schema)) return AutomationCodes.ReplacementBinding;
                }
            }
            return Code.None;
        }
        public void ReplaceEntityReferences(World world, Entity source, Entity destination)
        {
            if (!CheckReferenceReplacement(world, source, destination.Def).IsNone)
                throw new InvalidOperationException("automation replacement binding changed after admission");
            var prepared = new List<(Record Record, AutomationSettings Settings)>();
            foreach (Record record in _rules.Values)
            {
                var settings = record.Settings; var target = settings.Target; bool changed = false;
                if (target.Kind == ControlTargetKind.Entity && target.Entity == source.Id)
                {
                    var policy = Definition(settings).ActionPolicy;
                    target = EntityObservation.ReadProvider(world, policy, destination,
                        () => policy.RebindTarget(world, record.Company, settings.Target, source, destination), observeTargetState: true);
                    if (target.Kind != ControlTargetKind.Entity || target.Entity != destination.Id || target.Capability.IsNone)
                        throw new InvalidOperationException("action replacement redirected the actual target");
                    changed = true;
                }
                var inputs = new SortedDictionary<string, ControlBinding>(StringComparer.Ordinal);
                foreach (var input in settings.Inputs)
                {
                    var binding = input.Value;
                    if (binding.Entity == source.Id)
                    {
                        binding = new ControlBinding(destination.Id, binding.Capability); changed = true;
                        var declared = Definition(settings).Inputs;
                        foreach (var signal in declared)
                            if (signal.Id == input.Key && !AutomationSignalContracts.Accepts(world, signal.Signal,
                                destination.Get<IAutomationSignalSource>(binding.Capability)))
                                throw new InvalidOperationException("replacement lost actual automation signal binding");
                    }
                    inputs.Add(input.Key, binding);
                }
                if (changed) prepared.Add((record, new AutomationSettings(settings.Profile, settings.Enabled, target, inputs,
                    settings.Thresholds, settings.WhenTrue, settings.WhenFalse)));
            }
            // Only live bindings change. Configure would erase pending decisions, dwell and recovery state.
            foreach (var item in prepared) item.Record.Settings = item.Settings;
        }

        public void ValidateComposition(World world)
        {
            if (!ReferenceEquals(world, World)) throw new ContentException("automation belongs to another world");
            var nodes = new SortedDictionary<string, ControlContentNode>(StringComparer.Ordinal);
            foreach (IControlInputRecoveryPolicy recovery in world.Content.All<IControlInputRecoveryPolicy>())
            {
                if (!world.Content.Owns(recovery) || !ReferenceEquals(world.Content.Find<IControlInputRecoveryPolicy>(recovery.Id), recovery))
                    throw new ContentException("actual unambiguous recovery policy required");
                AddNode(recovery);
            }
            foreach (IControlActionPolicy policy in world.Content.All<IControlActionPolicy>())
            {
                if (!world.Content.Owns(policy) || !ReferenceEquals(world.Content.Find<IControlActionPolicy>(policy.Id), policy))
                    throw new ContentException("actual unambiguous action policy required");
                AddNode(policy);
            }
            foreach (IControlRule rule in world.Content.All<IControlRule>())
            {
                if (!world.Content.Owns(rule) || !ReferenceEquals(world.Content.Find<IControlRule>(rule.Id), rule))
                    throw new ContentException("actual unambiguous control rule required");
                AddNode(rule);
            }
            // Declared delegating cycles reject before recursive metadata/evaluation callbacks.
            _ = new DependencyGraph<ControlContentNode>(nodes.Values, AutomationModule.ModuleId);
            foreach (IControlActionPolicy policy in world.Content.All<IControlActionPolicy>())
            {
                EntityObservation.ReadProvider(world, policy, null, () => {
                    ControlActionDispatch.Validate(world, policy); return true;
                }, space: world.Space);
            }
            foreach (IControlRule rule in world.Content.All<IControlRule>())
            {
                if (!world.Content.Owns(rule) || !ReferenceEquals(world.Content.Find<IControlRule>(rule.Id), rule))
                    throw new ContentException("actual unambiguous control rule required");
                EntityObservation.ReadProvider(world, rule, null, () => {
                if (rule.Inputs == null || rule.Inputs.Count == 0 || rule.Thresholds == null || rule.InitialState == null || rule.InitialState.Count == 0
                    || rule.Arbitration != ControlRuleArbitration.Exclusive || rule.WhenTrue.Unit.IsNone
                    || rule.WhenTrue.Unit != rule.WhenFalse.Unit)
                    throw new ContentException(rule.Id + ": invalid declared control rule contract");
                if (rule.InputRecovery == null || !world.Content.Owns(rule.InputRecovery) ||
                    !ReferenceEquals(world.Content.Find<IControlInputRecoveryPolicy>(rule.InputRecovery.Id), rule.InputRecovery))
                    throw new ContentException(rule.Id + ": explicit catalog-owned input recovery policy required");
                if (rule.ActionPolicy == null || !world.Content.Owns(rule.ActionPolicy) ||
                    !ReferenceEquals(world.Content.Find<IControlActionPolicy>(rule.ActionPolicy.Id), rule.ActionPolicy))
                    throw new ContentException(rule.Id + ": explicit catalog-owned control action policy required");
                var inputs = new HashSet<string>(StringComparer.Ordinal);
                foreach (ControlSignalInput input in rule.Inputs)
                    if (input == null || !inputs.Add(input.Id) || !ReferenceEquals(world.Content.Find<IAutomationSignalContent>(input.Signal.Id), input.Signal))
                        throw new ContentException(rule.Id + ": duplicate/missing/foreign signal input");
                foreach (var threshold in rule.Thresholds)
                {
                    ControlSignalInput source = null;
                    foreach (var input in rule.Inputs) if (input.Id == threshold.Value?.Input) source = input;
                    if (!ValidId(threshold.Key) || threshold.Value == null || source == null
                        || source.Signal.Kind != AutomationSignalKind.Number || source.Signal.Unit != threshold.Value.Initial.Unit)
                        throw new ContentException(rule.Id + ": invalid numeric threshold declaration");
                }
                if (!ControlThresholds.Check(rule, ControlThresholds.Initial(rule)).IsNone)
                    throw new ContentException(rule.Id + ": invalid initial numeric threshold bands");
                try { AutomationFacts.Copy(rule.InitialState); world.Time.Ticks(rule.Dwell); world.Time.Ticks(rule.Cooldown); }
                catch (Exception ex) when (ex is ArgumentException || ex is OverflowException)
                { throw new ContentException(rule.Id + ": invalid named state/duration: " + ex.Message); }
                return true;
                }, space: world.Space);
            }

            void AddNode(IContentDefinition definition)
            {
                if (nodes.TryGetValue(definition.Id, out var previous))
                {
                    if (!ReferenceEquals(previous.Definition, definition)) throw new ContentException("ambiguous control declaration identity");
                    return;
                }
                EntityObservation.ReadProvider(world, definition, null, () => {
                    nodes.Add(definition.Id, new ControlContentNode(definition)); return true;
                }, space: world.Space);
            }
        }
        private sealed class ControlContentNode : IDependencyNode
        {
            internal IContentDefinition Definition { get; }
            public string Id => Definition.Id;
            public IReadOnlyList<string> Dependencies { get; }
            internal ControlContentNode(IContentDefinition definition)
            {
                Definition = definition;
                var dependencies = new SortedSet<string>(StringComparer.Ordinal);
                if (definition is IDependencyNode declared)
                {
                    if (declared.Dependencies == null) throw new ContentException("explicit control dependencies required");
                    foreach (string dependency in declared.Dependencies)
                        if (!dependencies.Add(dependency)) throw new ContentException("duplicate control dependency");
                }
                if (definition is IControlRule rule)
                {
                    IControlInputRecoveryPolicy recovery = rule.InputRecovery;
                    IControlActionPolicy action = rule.ActionPolicy;
                    if (recovery != null) dependencies.Add(recovery.Id);
                    if (action != null) dependencies.Add(action.Id);
                }
                Dependencies = new List<string>(dependencies).AsReadOnly();
            }
        }

        private IControlRule Definition(AutomationSettings settings) => settings == null ? null : World.Content.Find<IControlRule>(settings.Profile);
        private static bool ValidId(string id)
        { try { ContractIdentity.ValidateId(id, "live automation rule"); return true; } catch (ArgumentException) { return false; } }
        private Code CheckShape(PlayerId company, string id, AutomationSettings settings)
        {
            if (!World.Players.IsSeat(company)) return Codes.NotSeated;
            if (!ValidId(id) || settings == null) return AutomationCodes.InvalidSettings;
            IControlRule rule = Definition(settings);
            if (rule == null) return Codes.UnknownDef;
            if (settings.Target.Kind != rule.ActionPolicy.TargetKind) return AutomationCodes.InvalidSettings;
            if (settings.Inputs.Count != rule.Inputs.Count) return AutomationCodes.InvalidSettings;
            if (settings.WhenTrue.Unit != rule.WhenTrue.Unit || settings.WhenFalse.Unit != rule.WhenTrue.Unit) return AutomationCodes.UnitMismatch;
            foreach (ControlSignalInput input in rule.Inputs)
                if (!settings.Inputs.ContainsKey(input.Id)) return AutomationCodes.InvalidSettings;
            return ControlThresholds.Check(rule, settings.Thresholds);
        }

        private Code CheckReferences(PlayerId company, AutomationSettings settings)
        {
            IControlRule rule = Definition(settings);
            Code actionRefusal = ControlActionDispatch.CheckBinding(World, company, settings, settings.WhenTrue);
            if (!actionRefusal.IsNone) return actionRefusal;
            actionRefusal = ControlActionDispatch.CheckBinding(World, company, settings, settings.WhenFalse);
            if (!actionRefusal.IsNone) return actionRefusal;
            foreach (ControlSignalInput input in rule.Inputs)
            {
                ControlBinding binding = settings.Inputs[input.Id]; Entity entity = World.Entities.Get(binding.Entity);
                if (entity == null) return Codes.NotFound;
                if (entity.Owner != company) return Codes.NotOwned;
                IAutomationSignalSource source = entity.Get<IAutomationSignalSource>(binding.Capability);
                if (source == null) return AutomationCodes.NotSignalSource;
                if (!AutomationSignalContracts.Accepts(World, input.Signal, source))
                    return OperationCodes.SignalUnavailable;
            }
            return Code.None;
        }

        public Code CheckConfigure(PlayerId company, string id, AutomationSettings settings)
        {
            World.RequireHealthyUpdate();
            if (!World.Players.IsSeat(company)) return Codes.NotSeated;
            if (settings == null) return AutomationCodes.InvalidSettings;
            IControlRule rule = Definition(settings);
            if (rule == null) return Codes.UnknownDef;
            // Admission includes custom disclosure/shape metadata, not just the nested action calls.
            // Observe the actual bindings before the first callback, including refusal branches.
            return EntityObservation.ReadParticipants(World, rule, settings.ResolveParticipants(World), () => {
                    if (settings != null && World.Players.IsSeat(company) && !AutomationProfileSnapshot.VisibleTo(World, company, Definition(settings)))
                        return Codes.UnknownDef;
                    Code refusal = CheckShape(company, id, settings);
                    if (!refusal.IsNone) return refusal;
                    refusal = CheckReferences(company, settings); if (!refusal.IsNone) return refusal;
                    refusal = ControlActionDispatch.Prepare(World, company, settings, settings.WhenTrue, out _, out _);
                    if (!refusal.IsNone) return refusal;
                    refusal = ControlActionDispatch.Prepare(World, company, settings, settings.WhenFalse, out _, out _);
                    if (!refusal.IsNone) return refusal;
                    if (settings.Enabled)
                        foreach (Record other in _rules.Values)
                            if (other.Company == company && other.Id != id && other.Settings.Enabled && (other.Blocker.IsNone || other.Waiting)
                                && other.Settings.Target.Equals(settings.Target)) return AutomationCodes.Conflict;
                    return Code.None;
                }, space: World.Space);
        }

        public void Configure(PlayerId company, string id, AutomationSettings settings)
        {
            Code refusal = CheckConfigure(company, id, settings);
            if (!refusal.IsNone) throw new InvalidOperationException("automation configure refused: " + refusal);
            var key = (company.Value, id);
            if (!_rules.TryGetValue(key, out Record record) || !SameBindings(record.Settings, settings))
            {
                IControlRule rule = Definition(settings);
                record = EntityObservation.ReadParticipants(World, rule, settings.ResolveParticipants(World),
                    () => new Record { Company = company, Id = id, Latch = rule.InitialLatch, State = AutomationFacts.Copy(rule.InitialState) },
                    space: World.Space);
                _rules[key] = record;
            }
            record.Settings = settings; record.Blocker = Code.None; record.Candidate = record.Pending = null;
            record.Since = record.Due = -1;
            record.WaitingInput = null;
        }
        private static bool SameBindings(AutomationSettings left, AutomationSettings right)
        {
            if (left.Profile != right.Profile || !left.Target.Equals(right.Target) || left.Inputs.Count != right.Inputs.Count) return false;
            foreach (KeyValuePair<string, ControlBinding> input in left.Inputs)
                if (!right.Inputs.TryGetValue(input.Key, out ControlBinding binding) || !binding.Equals(input.Value)) return false;
            return true;
        }
        public Code CheckRemove(PlayerId company, string id)
        {
            World.RequireHealthyUpdate();
            return !World.Players.IsSeat(company) ? Codes.NotSeated : !ValidId(id) || !_rules.ContainsKey((company.Value, id)) ? Codes.NotFound : Code.None;
        }
        public void Remove(PlayerId company, string id)
        {
            Code refusal = CheckRemove(company, id);
            if (!refusal.IsNone) throw new InvalidOperationException("automation removal refused: " + refusal);
            _rules.Remove((company.Value, id));
        }
        private static AutomationRuleSnapshot Snapshot(Record record)
            => new AutomationRuleSnapshot(record.Company, record.Id, record.Settings, record.State, record.Latch,
                record.AppliedAction, record.Candidate, record.Since, record.Cooldown, record.Pending, record.Due, record.AppliedAt, record.Blocker, record.WaitingInput);
        public bool TryGet(PlayerId company, string id, out AutomationRuleSnapshot snapshot)
        {
            snapshot = null; World.RequireHealthyUpdate();
            if (id == null || !_rules.TryGetValue((company.Value, id), out Record record)) return false;
            snapshot = Snapshot(record); return true;
        }
        public void CopyRules(PlayerId viewer, IList<AutomationRuleSnapshot> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear();
            World.RequireHealthyUpdate();
            if (viewer != PlayerId.None && !World.Players.IsSeat(viewer)) return;
            var values = new List<AutomationRuleSnapshot>();
            foreach (Record record in _rules.Values)
                if (viewer == PlayerId.None || viewer == record.Company) values.Add(Snapshot(record));
            foreach (var value in values) destination.Add(value);
        }

        private static void Suspend(Record record, Code blocker)
        {
            if (blocker.IsNone) throw new ArgumentException("suspension needs a typed blocker");
            record.Blocker = blocker; record.Candidate = record.Pending = null; record.Since = record.Due = -1;
            record.WaitingInput = null;
        }
        private ControlInputRecoveryAction ReadRecovery(Record record, ControlInputFailure failure)
        {
            var policy = Definition(record.Settings).InputRecovery;
            if (policy == null || !World.Content.Owns(policy) ||
                !ReferenceEquals(World.Content.Find<IControlInputRecoveryPolicy>(policy.Id), policy))
                throw new InvalidOperationException("input recovery policy lost its actual catalog identity");
            return EntityObservation.ReadParticipants(World, policy, record.Settings.ResolveParticipants(World), () => {
                ControlInputRecoveryAction action = policy.Decide(failure);
                if (!Enum.IsDefined(typeof(ControlInputRecoveryAction), action))
                    throw new InvalidOperationException("input recovery provider returned an invalid action");
                return action;
            }, space: World.Space);
        }
        private void FailInput(Record record, Code blocker, ControlInputFailure? failure)
        {
            ControlInputRecoveryAction action = failure.HasValue ? ReadRecovery(record, failure.Value) : ControlInputRecoveryAction.Suspend;
            Suspend(record, blocker);
            if (action == ControlInputRecoveryAction.Wait)
            { record.WaitingInput = failure.Value.Input; }
        }
        private Code Capture(Record record, out ControlEvaluationContext context, out ControlInputFailure? failure)
        {
            context = null; failure = null; Code refusal = CheckReferences(record.Company, record.Settings);
            if (!refusal.IsNone) return refusal;
            var signals = new SortedDictionary<string, AutomationSignalSnapshot>(StringComparer.Ordinal);
            foreach (ControlSignalInput input in Definition(record.Settings).Inputs)
            {
                ControlBinding binding = record.Settings.Inputs[input.Id];
                AutomationSignalSnapshot signal = input.Signal.ReadFrom(World, World.Entities.Get(binding.Entity).Get<IAutomationSignalSource>(binding.Capability));
                if (!signal.Available)
                {
                    failure = new ControlInputFailure(input.Id, new Code(input.Signal.Id), signal.Blocker);
                    return signal.Blocker;
                }
                signals.Add(input.Id, signal);
            }
            context = new ControlEvaluationContext(signals, record.State, record.Settings.Thresholds); return Code.None;
        }

        public override void Update(World world, TickInfo tick)
        {
            // Check all due inputs before any old intent changes an operation. An offline source
            // cancels its stale intent, while cross-rule feedback sees one shared pre-commit phase.
            var due = new List<Record>();
            var failed = new HashSet<Record>();
            foreach (Record record in _rules.Values)
            {
                if (!record.Settings.Enabled || !record.Blocker.IsNone || !record.Pending.HasValue) continue;
                if (record.Due > tick.Tick) continue;
                Code refusal = Capture(record, out _, out var failure);
                if (!refusal.IsNone) { FailInput(record, refusal, failure); failed.Add(record); } else due.Add(record);
            }
            // Old intents commit through the normal command owner; never apply new decisions here.
            foreach (Record record in due)
            {
                Code refusal = CheckReferences(record.Company, record.Settings);
                IControlRule rule = Definition(record.Settings);
                CommandRegistry.Entry handler = null; Command command = null;
                bool decision = record.Pending.Value;
                ControlAction action = decision ? record.Settings.WhenTrue : record.Settings.WhenFalse;
                if (refusal.IsNone) refusal = ControlActionDispatch.Prepare(world, record.Company, record.Settings,
                    action, out handler, out command);
                if (!refusal.IsNone) { Suspend(record, refusal); continue; }
                long cooldown = EntityObservation.ReadParticipants(world, rule, record.Settings.ResolveParticipants(world),
                    () => checked(tick.Tick + world.Time.Ticks(rule.Cooldown)), space: world.Space);
                handler.Handler.Apply(world, command, new CommandContext());
                world.RequireHealthyUpdate();
                record.Latch = decision; record.AppliedAction = action; record.AppliedAt = tick.Tick;
                record.Cooldown = cooldown;
                record.Pending = null; record.Due = -1; record.Candidate = null; record.Since = -1;
            }
            // Capture the entire input set before any rule evaluates; no evaluator receives mutable engine state.
            var pass = new List<(Record Record, ControlEvaluationContext Context)>();
            foreach (Record record in _rules.Values)
            {
                if (!record.Settings.Enabled || !record.Blocker.IsNone && !record.Waiting || failed.Contains(record)) continue;
                Code refusal = Capture(record, out ControlEvaluationContext context, out var failure);
                if (!refusal.IsNone) FailInput(record, refusal, failure);
                else
                {
                    record.Blocker = Code.None; record.WaitingInput = null;
                    pass.Add((record, context));
                }
            }
            foreach (var item in pass)
            {
                Record record = item.Record; IControlRule rule = Definition(record.Settings);
                var prepared = EntityObservation.ReadParticipants(world, rule,
                    record.Settings.ResolveParticipants(world), () => {
                        ControlEvaluation value = rule.Evaluate(item.Context);
                        ValidateEvaluation(rule, value);
                        bool? candidate = record.Candidate, pending = record.Pending;
                        long since = record.Since, due = record.Due;
                        // Metadata is provider code too. Admit the entire decision before writing
                        // any original rule fields, including state, dwell candidate and next-step time.
                        if (value.Blocker.IsNone && !pending.HasValue)
                        {
                            if (record.HasApplied && value.Condition == record.Latch && record.AppliedAction.Value.Equals(
                                value.Condition ? record.Settings.WhenTrue : record.Settings.WhenFalse))
                            { candidate = null; since = -1; }
                            else
                            {
                                if (!candidate.HasValue || candidate.Value != value.Condition)
                                { candidate = value.Condition; since = tick.Tick; }
                                if (tick.Tick >= record.Cooldown && tick.Tick - since >= world.Time.Ticks(rule.Dwell))
                                {
                                    pending = value.Condition; due = checked(tick.Tick + 1);
                                    candidate = null; since = -1;
                                }
                            }
                        }
                        return (Result: value, Candidate: candidate, Since: since, Pending: pending, Due: due);
                    }, space: world.Space);
                ControlEvaluation result = prepared.Result;
                record.State = result.State;
                if (!result.Blocker.IsNone) { Suspend(record, result.Blocker); continue; }
                record.Candidate = prepared.Candidate; record.Since = prepared.Since;
                record.Pending = prepared.Pending; record.Due = prepared.Due;
            }
        }

        private static void ValidateEvaluation(IControlRule rule, ControlEvaluation result)
        {
            if (result == null || result.State.Count != rule.InitialState.Count)
                throw new InvalidOperationException("control rule returned malformed named state: " + rule.Id);
            foreach (string key in rule.InitialState.Keys)
                if (!result.State.ContainsKey(key)) throw new InvalidOperationException("control rule returned different state identities: " + rule.Id);
        }

        private void ValidateRecord(Record record, bool observation = false)
        {
            Code shape = CheckShape(record.Company, record.Id, record.Settings);
            if (!shape.IsNone) throw new CorruptStateException("invalid saved automation binding: " + shape);
            IControlRule rule = Definition(record.Settings);
            if (record.AppliedAction.HasValue && record.AppliedAction.Value.Unit != rule.WhenTrue.Unit)
                throw new CorruptStateException("invalid saved last applied action unit");
            if (record.State == null || record.State.Count != rule.InitialState.Count)
                throw new CorruptStateException("invalid saved automation state count");
            foreach (string key in rule.InitialState.Keys)
                if (!record.State.ContainsKey(key)) throw new CorruptStateException("invalid saved automation state identity");
            if (record.Waiting)
            {
                ControlSignalInput input = null;
                foreach (var declared in rule.Inputs) if (declared.Id == record.WaitingInput) input = declared;
                if (!record.Settings.Enabled || record.Blocker.IsNone || input == null ||
                    ReadRecovery(record, new ControlInputFailure(input.Id, new Code(input.Signal.Id), record.Blocker)) != ControlInputRecoveryAction.Wait)
                    throw new CorruptStateException("invalid saved automation input wait");
            }
            bool inUpdateObservation = observation && World.Stepping;
            if (record.Candidate.HasValue != (record.Since >= 0) || record.Candidate.HasValue &&
                    (inUpdateObservation ? record.Since > World.Tick : record.Since >= World.Tick)
                || record.Since < -1 || record.Pending.HasValue != (record.Due >= 0) || record.Due < -1
                || record.Pending.HasValue && (record.Due == 0 || record.Due != World.Tick &&
                    (!inUpdateObservation || record.Due != checked(World.Tick + 1)))
                || record.Candidate.HasValue && record.Pending.HasValue
                || (!record.Settings.Enabled || !record.Blocker.IsNone) && (record.Pending.HasValue || record.Candidate.HasValue)
                || record.HasApplied != (record.AppliedAt >= 0) || record.AppliedAt < -1 ||
                    (inUpdateObservation ? record.AppliedAt > World.Tick : record.AppliedAt >= World.Tick)
                || !record.HasApplied && (record.Cooldown != 0 || record.Latch != rule.InitialLatch)
                || record.HasApplied && record.Cooldown != checked(record.AppliedAt + World.Time.Ticks(rule.Cooldown)))
                throw new CorruptStateException("invalid saved automation timing/phase");
        }
        public override void OnLoaded(World world) => ValidateState();
        private void ValidateState(bool observation = false)
        {
            var targets = new HashSet<(PlayerId, ControlBinding)>();
            foreach (Record record in _rules.Values)
            {
                ValidateRecord(record, observation);
                if (record.Settings.Enabled && (record.Blocker.IsNone || record.Waiting) && !targets.Add((record.Company, record.Settings.Target)))
                    throw new CorruptStateException("conflicting saved automation target owners");
            }
        }

        public override void Sync(IStateSync sync)
        {
            if (!sync.Reading) ValidateState(sync is IStateObservationSync observation && observation.IsObservation);
            int count = sync.Count(_rules.Count);
            if (sync.Reading)
            {
                _rules.Clear(); (int, string)? previous = null;
                for (int i = 0; i < count; i++)
                {
                    var record = new Record(); SyncRecord(sync, record);
                    var key = (record.Company.Value, record.Id);
                    if (previous.HasValue && _rules.Comparer.Compare(previous.Value, key) >= 0)
                        throw new CorruptStateException("automation rules must have unique canonical identities");
                    _rules.Add(key, record); previous = key;
                }
            }
            else foreach (Record record in _rules.Values) SyncRecord(sync, record);
        }
        private static void SyncDecision(IStateSync sync, ref bool? decision)
        {
            byte state = decision.HasValue ? (byte)(decision.Value ? 2 : 1) : (byte)0;
            sync.Byte(ref state);
            if (state > 2) throw new CorruptStateException("invalid automation decision");
            if (sync.Reading) decision = state == 0 ? (bool?)null : state == 2;
        }
        private static void SyncRecord(IStateSync sync, Record record)
        {
            sync.Player(ref record.Company); sync.String(ref record.Id);
            record.Settings = AutomationSettings.Sync(sync, record.Settings);
            int count = sync.Count(record.State?.Count ?? 0);
            if (sync.Reading)
            {
                var state = new SortedDictionary<string, bool>(StringComparer.Ordinal); string previous = null;
                for (int i = 0; i < count; i++)
                {
                    string key = null; bool value = false; sync.String(ref key); sync.Bool(ref value);
                    if (key == null || previous != null && string.CompareOrdinal(previous, key) >= 0)
                        throw new CorruptStateException("automation state must have unique canonical identities");
                    state.Add(key, value); previous = key;
                }
                try { record.State = AutomationFacts.Copy(state); }
                catch (ArgumentException ex) { throw new CorruptStateException("invalid automation state identity: " + ex.Message); }
            }
            else foreach (KeyValuePair<string, bool> fact in record.State)
            { string key = fact.Key; bool value = fact.Value; sync.String(ref key); sync.Bool(ref value); }
            sync.Bool(ref record.Latch); bool applied = record.HasApplied; sync.Bool(ref applied);
            if (applied) record.AppliedAction = ControlAction.Sync(sync, record.AppliedAction ?? default);
            else if (sync.Reading) record.AppliedAction = null;
            SyncDecision(sync, ref record.Candidate);
            sync.Long(ref record.Since); sync.Long(ref record.Cooldown); SyncDecision(sync, ref record.Pending);
            sync.Long(ref record.Due); sync.Long(ref record.AppliedAt);
            string blocker = record.Blocker.Id; sync.String(ref blocker);
            sync.String(ref record.WaitingInput);
            if (sync.Reading)
            {
                try
                {
                    if (blocker != null) ContractIdentity.ValidateId(blocker, "automation blocker");
                    record.Blocker = blocker == null ? Code.None : new Code(blocker);
                }
                catch (ArgumentException ex) { throw new CorruptStateException("invalid automation blocker: " + ex.Message); }
            }
        }
    }
}
