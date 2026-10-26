using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>One objective lifecycle owner, not a resource/work/progression store.</summary>
    public sealed class ObjectiveSystem : GameSystem, IObjectives, ICompositionValidator
    {
        private sealed class Entry
        {
            internal PlayerId Company;
            internal IObjectiveDefinition Definition;
            internal ObjectiveState State = ObjectiveState.Locked;
            internal long Activated = -1, Settled = -1;
            internal ObjectiveObservation Observation = ObjectiveObservation.Wait(ObjectiveCodes.Prerequisite);
            internal Code Blocker = ObjectiveCodes.Prerequisite, Failure;
        }
        private sealed class Decision
        {
            internal Entry Entry;
            internal ObjectiveState State;
            internal ObjectiveObservation Observation;
            internal Code Failure;
            internal bool Activate;
        }
        private readonly World _world;
        private readonly List<Entry> _entries = new List<Entry>();
        private readonly DependencyGraph<IObjectivePredicate> _predicates;
        public override string Id => ObjectiveModule.ModuleId;
        public override int StateSchemaVersion => 1;
        public ObjectiveSystem(World world, IReadOnlyDictionary<int, IReadOnlyList<string>> bindings)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _predicates = new DependencyGraph<IObjectivePredicate>(world.Content.All<IObjectivePredicate>(), Id + ".predicates");
            _ = new DependencyGraph<IObjectiveDefinition>(world.Content.All<IObjectiveDefinition>(), Id + ".objectives");
            foreach (var company in bindings)
                foreach (string id in company.Value)
                    _entries.Add(new Entry { Company = new PlayerId(company.Key), Definition = world.Content.Get<IObjectiveDefinition>(id) });
            _entries.Sort((a, b) => { int c = a.Company.Value.CompareTo(b.Company.Value); return c != 0 ? c : string.CompareOrdinal(a.Definition.Id, b.Definition.Id); });
        }
        private Entry Find(PlayerId company, string id)
        {
            _world.RequireHealthyUpdate();
            foreach (Entry entry in _entries)
                if (entry.Company == company && EntityObservation.ReadProvider(_world, entry.Definition, null, () => entry.Definition.Id == id)) return entry;
            return null;
        }
        private HashSet<string> Reachable(IObjectivePredicate root)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal); var pending = new Stack<string>(); pending.Push(root.Id);
            while (pending.Count > 0)
            {
                string id = pending.Pop(); if (!ids.Add(id)) continue;
                foreach (string dependency in _predicates.Dependencies(id)) pending.Push(dependency);
            }
            return ids;
        }
        public void ValidateComposition(World world)
        {
            if (!ReferenceEquals(world, _world)) throw new ContentException("objectives belong to another session");
            foreach (IObjectivePredicate predicate in _predicates.Nodes)
                EntityObservation.ReadProvider(world, predicate, null, () => {
                    RequireAlias(world, predicate, "predicate");
                    IReadOnlyList<IContentDefinition> content = predicate.ObservationContent;
                    if (content == null) throw new ContentException(predicate.Id + ": explicit observation content required");
                    var identities = new HashSet<string>(StringComparer.Ordinal);
                    foreach (IContentDefinition reference in content)
                    {
                        RequireAlias(world, reference, "observation content");
                        if (!(reference is Def actual) || !actual.IsFrozen || reference is IPersistentService || reference.Id != actual.Id || !identities.Add(reference.Id))
                            throw new ContentException(predicate.Id + ": observation content requires unique actual immutable aliases");
                    }
                    predicate.ValidateBinding(world);
                    return true;
                });
            foreach (IObjectiveEffect effect in world.Content.All<IObjectiveEffect>())
                EntityObservation.ReadProvider(world, effect, null, () => {
                    RequireAlias(world, effect, "effect");
                    effect.ValidateBinding(world);
                    return true;
                });
            foreach (IObjectiveDefinition definition in world.Content.All<IObjectiveDefinition>())
                EntityObservation.ReadProvider(world, definition, null, () => {
                    RequireAlias(world, definition, "definition");
                    RequireAlias(world, definition.Predicate, "predicate");
                    if (definition.OnComplete != null) RequireAlias(world, definition.OnComplete, "completion effect");
                    if (definition.OnFailure != null) RequireAlias(world, definition.OnFailure, "failure effect");
                    if (definition.Deadline.HasValue && world.Time.Ticks(definition.Deadline.Value) <= 0)
                        throw new ContentException(definition.Id + ": objective deadline must advance game time");
                    return true;
                });
            var identities = new HashSet<(int, string)>();
            foreach (Entry entry in _entries)
            {
                if (!world.Players.IsSeat(entry.Company) || !identities.Add((entry.Company.Value, entry.Definition.Id))) throw new ContentException("duplicate/unknown objective beneficiary");
                foreach (string prerequisite in entry.Definition.Dependencies)
                    if (Find(entry.Company, prerequisite) == null) throw new ContentException(entry.Definition.Id + ": beneficiary must explicitly enroll its prerequisite " + prerequisite);
            }
        }
        private static void RequireAlias<T>(World world, T definition, string kind) where T : class, IContentDefinition
        {
            if (!(definition is Def actual) || !actual.IsFrozen || definition is IPersistentService ||
                definition.Id != actual.Id || definition.Module != actual.Module || !world.Content.Owns(definition) ||
                !ReferenceEquals(world.Content.Find<T>(definition.Id), definition))
                throw new ContentException("objective " + kind + " must be an unambiguous actual content alias");
        }
        public ObjectiveObservation Observe(PlayerId company, string objective)
        {
            _world.RequireHealthyUpdate();
            if (!_world.Players.IsSeat(company)) return ObjectiveObservation.Invalid(Codes.NotSeated);
            Entry entry = Find(company, objective); if (entry == null) return ObjectiveObservation.Invalid(Codes.NotFound);
            if (!CanDisclose(company, entry.Definition)) return ObjectiveObservation.Invalid(Codes.NotFound);
            return Evaluate(entry);
        }
        private bool CanDisclose(PlayerId viewer, IObjectiveDefinition root)
        {
            _world.RequireHealthyUpdate();
            var view = _world.ViewFor(viewer);
            var pending = new Stack<IObjectiveDefinition>(); var visited = new HashSet<string>(StringComparer.Ordinal);
            pending.Push(root);
            while (pending.Count > 0)
            {
                IObjectiveDefinition definition = pending.Pop();
                bool visible = EntityObservation.ReadProvider(_world, definition, null, () => {
                    if (!visited.Add(definition.Id)) return true;
                    if (!view.CanSeeContent(definition)) return false;
                    var needed = Reachable(definition.Predicate);
                    foreach (IObjectivePredicate predicate in _predicates.Nodes)
                        if (needed.Contains(predicate.Id) && !EntityObservation.ReadProvider(_world, predicate, null, () => {
                            if (!view.CanSeeContent(predicate)) return false;
                            foreach (IContentDefinition content in predicate.ObservationContent) if (!view.CanSeeContent(content)) return false;
                            return true;
                        })) return false;
                    foreach (string prerequisite in definition.Dependencies) pending.Push(_world.Content.Get<IObjectiveDefinition>(prerequisite));
                    return true;
                });
                if (!visible) return false;
            }
            return true;
        }
        private ObjectiveObservation Evaluate(Entry entry)
            => !CanDisclose(entry.Company, entry.Definition) ? ObjectiveObservation.Wait(ObjectiveCodes.Undisclosed)
                : EntityObservation.ReadProvider(_world, entry.Definition, null, () => EvaluateCore(entry));
        private ObjectiveObservation EvaluateCore(Entry entry)
        {
            var values = new Dictionary<string, ObjectiveObservation>(StringComparer.Ordinal);
            var context = new ObjectiveContext(_world, entry.Company, entry.Definition.Id); var needed = Reachable(entry.Definition.Predicate);
            foreach (IObjectivePredicate predicate in _predicates.Nodes)
            {
                if (!needed.Contains(predicate.Id)) continue;
                var inputs = new Dictionary<string, ObjectiveObservation>(StringComparer.Ordinal);
                foreach (string child in _predicates.Dependencies(predicate.Id)) inputs.Add(child, values[child]);
                ObjectiveObservation observation = EntityObservation.ReadProvider(_world, predicate, null, () => {
                    ObjectiveObservation observed = predicate.Observe(context, new ReadOnlyDictionary<string, ObjectiveObservation>(inputs));
                    if (observed.Progress < Fix.Zero || observed.Progress > Fix.One ||
                        observed.Satisfied && (!observed.Valid || observed.Progress != Fix.One || !observed.Blocker.IsNone) ||
                        !observed.Satisfied && observed.Blocker.IsNone) throw new InvalidOperationException(predicate.Id + ": invalid objective observation");
                    return observed;
                });
                values.Add(predicate.Id, observation);
            }
            return values[entry.Definition.Predicate.Id];
        }
        private long? Deadline(Entry entry) => entry.Activated < 0 || !entry.Definition.Deadline.HasValue ? (long?)null
            : checked(entry.Activated + _world.Time.Ticks(entry.Definition.Deadline.Value));
        public override void Update(World world, TickInfo tick)
        {
            var decisions = new List<Decision>();
            // Read the entire pass before any lifecycle/effect commit, including prerequisite state.
            foreach (Entry entry in _entries)
            {
                if (entry.State == ObjectiveState.Completed || entry.State == ObjectiveState.Failed) continue;
                var decision = new Decision { Entry = entry, State = entry.State, Observation = entry.Observation, Failure = entry.Failure };
                if (entry.State == ObjectiveState.Locked)
                {
                    bool ready = true, failed = false;
                    foreach (string prerequisite in entry.Definition.Dependencies)
                    { ObjectiveState state = Find(entry.Company, prerequisite).State; ready &= state == ObjectiveState.Completed; failed |= state == ObjectiveState.Failed; }
                    if (failed) { decision.State = ObjectiveState.Failing; decision.Failure = ObjectiveCodes.DependencyFailed; }
                    else if (ready) { decision.State = ObjectiveState.Active; decision.Activate = true; }
                }
                if (decision.State == ObjectiveState.Active)
                {
                    long? deadline = decision.Activate ? (entry.Definition.Deadline.HasValue ? checked(tick.Tick + world.Time.Ticks(entry.Definition.Deadline.Value)) : (long?)null) : Deadline(entry);
                    if (deadline.HasValue && tick.Tick >= deadline.Value)
                    { decision.State = ObjectiveState.Failing; decision.Failure = ObjectiveCodes.Deadline; }
                    else
                    {
                        decision.Observation = Evaluate(entry);
                        if (!decision.Observation.Valid) { decision.State = ObjectiveState.Failing; decision.Failure = decision.Observation.Blocker; }
                        else if (decision.Observation.Satisfied) decision.State = ObjectiveState.Completing;
                    }
                }
                decisions.Add(decision);
            }
            foreach (Decision decision in decisions)
            {
                Entry entry = decision.Entry; entry.State = decision.State; entry.Observation = decision.Observation; entry.Failure = decision.Failure;
                if (decision.Activate) entry.Activated = tick.Tick;
                entry.Blocker = entry.State == ObjectiveState.Failing ? entry.Failure : entry.Observation.Blocker;
                if (entry.State != ObjectiveState.Completing && entry.State != ObjectiveState.Failing) continue;
                IObjectiveEffect effect = entry.State == ObjectiveState.Completing ? entry.Definition.OnComplete : entry.Definition.OnFailure;
                var context = new ObjectiveContext(world, entry.Company, entry.Definition.Id);
                Code check = Code.None;
                if (effect != null)
                {
                    var scope = EntityObservation.CaptureProvider(world, effect);
                    try { check = effect.Check(context); }
                    finally { scope.Verify(); }
                }
                if (!check.IsNone) { entry.Blocker = check; continue; }
                effect?.Apply(context);
                entry.State = entry.State == ObjectiveState.Completing ? ObjectiveState.Completed : ObjectiveState.Failed;
                entry.Settled = tick.Tick; entry.Blocker = entry.State == ObjectiveState.Completed ? Code.None : entry.Failure;
            }
        }
        public void CopyObjectives(PlayerId viewer, IList<ObjectiveSnapshot> destination)
        {
            _world.RequireHealthyUpdate();
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            var facts = new List<ObjectiveSnapshot>();
            if (viewer != PlayerId.None && !_world.Players.IsSeat(viewer)) { destination.Clear(); return; }
            foreach (Entry entry in _entries)
                if (viewer == PlayerId.None || viewer == entry.Company)
                    EntityObservation.ReadProvider(_world, entry.Definition, null, () => {
                        if (!CanDisclose(viewer, entry.Definition)) return false;
                        facts.Add(new ObjectiveSnapshot(entry.Company, entry.Definition, entry.State, entry.Activated, entry.Settled,
                            Deadline(entry), entry.Observation, entry.Blocker, entry.Failure));
                        return true;
                    });
            _world.RequireHealthyUpdate();
            destination.Clear(); foreach (ObjectiveSnapshot fact in facts) destination.Add(fact);
        }
        private void ValidateState()
        {
            foreach (Entry entry in _entries)
            {
                bool terminal = entry.State == ObjectiveState.Completed || entry.State == ObjectiveState.Failed;
                bool failing = entry.State == ObjectiveState.Failing || entry.State == ObjectiveState.Failed;
                if (entry.State < ObjectiveState.Locked || entry.State > ObjectiveState.Failed || entry.Activated < -1 || entry.Activated > _world.Tick ||
                    entry.Settled < -1 || entry.Settled > _world.Tick || terminal != (entry.Settled >= 0) ||
                    entry.Activated >= 0 && terminal && entry.Settled < entry.Activated || failing == entry.Failure.IsNone ||
                    entry.State == ObjectiveState.Locked && entry.Activated != -1 ||
                    entry.Activated < 0 && entry.State != ObjectiveState.Locked && !(failing && entry.Failure == ObjectiveCodes.DependencyFailed) ||
                    (entry.State == ObjectiveState.Completing || entry.State == ObjectiveState.Completed) && !entry.Observation.Satisfied ||
                    entry.State == ObjectiveState.Completed && !entry.Blocker.IsNone ||
                    !entry.Observation.Satisfied && entry.Observation.Blocker.IsNone || entry.Observation.Progress < Fix.Zero || entry.Observation.Progress > Fix.One)
                    throw new CorruptStateException("invalid objective lifecycle/timestamps/observation");
                _ = Deadline(entry);
                if (entry.State == ObjectiveState.Locked) continue;
                bool failedDependency = false;
                foreach (string prerequisite in entry.Definition.Dependencies)
                {
                    Entry dependency = Find(entry.Company, prerequisite); failedDependency |= dependency.State == ObjectiveState.Failed;
                    if (entry.Failure != ObjectiveCodes.DependencyFailed &&
                        (dependency.State != ObjectiveState.Completed || dependency.Settled > entry.Activated)) throw new CorruptStateException("objective precedes its required completion");
                }
                if (entry.Failure == ObjectiveCodes.DependencyFailed && (!failedDependency || entry.Activated != -1)) throw new CorruptStateException("objective dependency failure has no failed prerequisite");
                if (entry.Failure == ObjectiveCodes.Deadline && (!Deadline(entry).HasValue || (terminal ? entry.Settled : _world.Tick) < Deadline(entry).Value)) throw new CorruptStateException("objective deadline failure precedes expiry");
            }
        }
        public override void Sync(IStateSync sync)
        {
            if (!sync.Reading) ValidateState();
            if (sync.Count(_entries.Count) != _entries.Count) throw new CorruptStateException("saved objective enrollment differs");
            foreach (Entry entry in _entries)
            {
                PlayerId company = entry.Company; string id = entry.Definition.Id; sync.Player(ref company); sync.String(ref id);
                if (company != entry.Company || id != entry.Definition.Id) throw new CorruptStateException("saved objective binding differs");
                sync.Enum(ref entry.State); sync.Long(ref entry.Activated); sync.Long(ref entry.Settled); SyncCode(sync, ref entry.Blocker); SyncCode(sync, ref entry.Failure);
                bool valid = entry.Observation.Valid, satisfied = entry.Observation.Satisfied; Fix progress = entry.Observation.Progress; Code blocker = entry.Observation.Blocker;
                sync.Bool(ref valid); sync.Bool(ref satisfied); sync.Fix(ref progress); SyncCode(sync, ref blocker);
                if (sync.Reading)
                    try { entry.Observation = new ObjectiveObservation(valid, satisfied, progress, blocker); }
                    catch (ArgumentException e) { throw new CorruptStateException(e.Message); }
            }
            if (sync.Reading) ValidateState();
        }
        public override void OnLoaded(World world) => ValidateState();
        private static void SyncCode(IStateSync sync, ref Code code)
        {
            string id = code.Id; sync.String(ref id);
            try { if (id != null) ContractIdentity.ValidateId(id, "objective blocker"); if (sync.Reading) code = id == null ? Code.None : new Code(id); }
            catch (ArgumentException e) { throw new CorruptStateException(e.Message); }
        }
    }
}
