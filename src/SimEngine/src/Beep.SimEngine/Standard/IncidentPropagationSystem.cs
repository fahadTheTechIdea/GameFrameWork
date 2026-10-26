using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Sole exposure timing/backlog owner. Incidents and native response work retain their own state.</summary>
    internal sealed class IncidentPropagationSystem : GameSystem, ICompositionValidator, IIncidentPropagationState, IEntityReplacementReferenceOwner
    {
        private sealed class Record
        {
            internal EntityId Entity;
            internal Code Capability;
            internal string Rule;
            internal int Episode;
            internal long Next = -1;
            internal bool Scanning;
            internal EntityId After, Through;
            internal Code Blocker = IncidentPropagationCodes.Inactive;
            internal readonly List<PendingExposureSnapshot> Pending = new List<PendingExposureSnapshot>();
        }
        private sealed class Binding
        {
            internal Record Record;
            internal IIncidentPropagationEmitter Emitter;
            internal IIncidentPropagationRule Rule;
            internal IncidentLifecycleSnapshot Incident;
        }
        private readonly World _world;
        private readonly int _budget;
        private List<Record> _records = new List<Record>();
        private EntityId _cursor;
        private Code _cursorCapability;
        private bool _validating, _observing; // Transient callback guards, not simulation timing/state.
        public override string Id => IncidentPropagationModule.ModuleId;
        public override int StateSchemaVersion => 1;
        internal IncidentPropagationSystem(World world, int budget) { _world = world; _budget = budget; }
        private static int Compare(EntityId a, Code ac, EntityId b, Code bc)
        { int order = a.CompareTo(b); return order != 0 ? order : string.CompareOrdinal(ac.Id, bc.Id); }
        private static int ComparePending(PendingExposureSnapshot a, PendingExposureSnapshot b)
        { int order = a.DueTick.CompareTo(b.DueTick); return order != 0 ? order : a.Target.CompareTo(b.Target); }
        private IIncidentPropagationEmitter Emitter(Record record)
            => _world.Entities.Get(record.Entity)?.Get<IIncidentPropagationEmitter>(record.Capability);
        public void ValidateComposition(World world)
        {
            if (!ReferenceEquals(world, _world) || _budget <= 0) throw new ContentException("invalid propagation owner/budget");
            if (_validating || _observing) throw new InvalidOperationException("propagation callbacks cannot recursively compose");
            _validating = true;
            try { IncidentPropagationContracts.ValidateComposition(world); }
            finally { _validating = false; }
        }
        private Record Find(EntityId entity, Code capability)
        { foreach (var record in _records) if (record.Entity == entity && record.Capability == capability) return record; return null; }
        private static bool RetainsIncident(Entity source, EntityDef destination, Code capability)
        {
            var current = source.Get<IPhasedIncidentResponse>(capability);
            var next = destination.Component<IPhasedIncidentDefinition>(capability);
            return current is Component component && next is ComponentDef definition &&
                component.Def.Key == definition.Key && Upgrades.CanRetainState(component.Def, definition) &&
                ReferenceEquals(current.Workflow, next.Workflow);
        }
        public Code CheckReferenceReplacement(World world, Entity source, EntityDef destination)
        {
            if (!ReferenceEquals(world, _world)) throw new ArgumentException("propagation belongs to another world", nameof(world));
            foreach (Record record in _records)
            {
                if (record.Entity == source.Id)
                {
                    var emitter = source.Get<IIncidentPropagationEmitter>(record.Capability);
                    var next = destination.Component<IIncidentPropagationDefinition>(record.Capability);
                    if (emitter is not Component component || next is not ComponentDef definition ||
                        component.Def.Key != definition.Key || !Upgrades.CanRetainState(component.Def, definition) ||
                        emitter.Source != next.Source || !ReferenceEquals(emitter.Rule, next.Rule) ||
                        !RetainsIncident(source, destination, emitter.Source)) return IncidentPropagationCodes.ReplacementBinding;
                }
                foreach (var pending in record.Pending)
                    if (pending.Target == source.Id)
                    {
                        var emitter = Emitter(record);
                        if (emitter == null || !RetainsIncident(source, destination, emitter.Rule.TargetSource))
                            return IncidentPropagationCodes.ReplacementBinding;
                    }
            }
            return Code.None;
        }
        private void VerifyRetainedIncident(Entity source, Entity destination, Code capability)
        {
            var old = source.Get<IPhasedIncidentResponse>(capability);
            var next = destination.Get<IPhasedIncidentResponse>(capability);
            if (old == null || next == null) throw new InvalidOperationException("replacement lost a propagation incident binding");
            var before = IncidentPhases.ReadLifecycle(_world, old, out _);
            var after = IncidentPhases.ReadLifecycle(_world, next, out _);
            if (before.Kind != after.Kind || before.Episode != after.Episode ||
                before.RaisedTick != after.RaisedTick || before.ResolvedTick != after.ResolvedTick)
                throw new InvalidOperationException("replacement changed a propagation incident episode");
        }
        public void ReplaceEntityReferences(World world, Entity source, Entity destination)
        {
            if (!CheckReferenceReplacement(world, source, destination.Def).IsNone)
                throw new InvalidOperationException("replacement changed a propagation binding after admission");
            // Verify actual retained component state before modifying the sole exposure backlog.
            foreach (Record record in _records)
            {
                if (record.Entity == source.Id)
                    VerifyRetainedIncident(source, destination, source.Get<IIncidentPropagationEmitter>(record.Capability).Source);
                foreach (var pending in record.Pending)
                    if (pending.Target == source.Id) VerifyRetainedIncident(source, destination, Emitter(record).Rule.TargetSource);
            }
            foreach (Record record in _records)
            {
                for (int i = 0; i < record.Pending.Count; i++)
                {
                    var pending = record.Pending[i];
                    if (pending.Target == source.Id)
                        record.Pending[i] = new PendingExposureSnapshot(destination.Id, pending.ExpectedEpisode, pending.ScheduledTick, pending.DueTick);
                }
                record.Pending.Sort(ComparePending);
                if (record.Entity == source.Id) record.Entity = destination.Id;
            }
            _records.Sort((a, b) => Compare(a.Entity, a.Capability, b.Entity, b.Capability));
            // After/Through and the round-robin cursor are ordering positions, not live bindings.
            // Keep their original values: restarting a scan would repeat/skew admission work.
            Validate(_records, _cursor, _cursorCapability);
        }
        private List<Binding> Bindings()
        {
            var bindings = new List<Binding>(); var live = new HashSet<Record>();
            foreach (Entity entity in _world.Entities.All)
                if (entity.Alive)
                    foreach (Component component in entity.Components)
                        if (component is IIncidentPropagationEmitter emitter)
                        {
                            var rule = IncidentPropagationContracts.ReadEmitter(_world, emitter, entity);
                            IPhasedIncidentResponse source = entity.Get<IPhasedIncidentResponse>(emitter.Source);
                            if (source == null) throw new InvalidOperationException("propagation source is missing");
                            IncidentLifecycleSnapshot snapshot = IncidentPhases.ReadLifecycle(_world, source, out _);
                            Record record = Find(entity.Id, emitter.CapabilityId);
                            if (record == null) { record = new Record { Entity = entity.Id, Capability = emitter.CapabilityId, Rule = rule.Id }; _records.Add(record); }
                            if (record.Episode != snapshot.Episode)
                            {
                                record.Pending.Clear(); record.Scanning = false; record.After = record.Through = EntityId.None;
                                record.Episode = snapshot.Episode;
                                record.Next = snapshot.Active ? checked(snapshot.RaisedTick + _world.Time.ExactTicks(rule.Interval)) : -1;
                                record.Blocker = snapshot.Active ? Code.None : IncidentPropagationCodes.Inactive;
                            }
                            if (!snapshot.Active)
                            {
                                record.Blocker = record.Episode == 0 ? IncidentPropagationCodes.Inactive : IncidentPropagationCodes.SourceEnded;
                                record.Pending.Clear(); record.Next = -1; record.Scanning = false; record.After = record.Through = EntityId.None;
                            }
                            bindings.Add(new Binding { Record = record, Emitter = emitter, Rule = rule, Incident = snapshot }); live.Add(record);
                        }
            _records.RemoveAll(record => !live.Contains(record));
            _records.Sort((a, b) => Compare(a.Entity, a.Capability, b.Entity, b.Capability));
            bindings.Sort((a, b) => Compare(a.Record.Entity, a.Record.Capability, b.Record.Entity, b.Record.Capability));
            return bindings;
        }
        public override void Update(World world, TickInfo tick)
        {
            // Capture all source admission before any delayed Raise; new waves wait until the next Update.
            List<Binding> bindings = Bindings();
            for (int unit = 0; unit < _budget; unit++)
            {
                Binding first = null, selected = null;
                foreach (var binding in bindings)
                {
                    Record record = binding.Record;
                    bool due = binding.Incident.Active && (record.Pending.Count > 0 && record.Pending[0].DueTick <= world.Tick || record.Next <= world.Tick);
                    if (!due) continue;
                    if (first == null) first = binding;
                    if (Compare(record.Entity, record.Capability, _cursor, _cursorCapability) > 0) { selected = binding; break; }
                }
                selected = selected ?? first; if (selected == null) break;
                _cursor = selected.Record.Entity; _cursorCapability = selected.Record.Capability;
                Advance(selected);
            }
        }
        private IncidentExposureObservation Observe(Binding binding, Entity target)
        {
            if (_observing) throw new InvalidOperationException("propagation eligibility cannot recursively observe");
            _observing = true;
            try
            {
                Entity source = binding.Emitter.Entity;
                return EntityObservation.ReadProvider(_world, binding.Rule, target, () => {
                    var sourceScope = EntityObservation.CaptureProvider(_world, source, source.Sync);
                    var targetScope = EntityObservation.CaptureProvider(_world, target, target.Sync);
                    var exposureScope = EntityObservation.CaptureProvider(_world, this);
                    try
                    {
                        var observation = binding.Rule.Observe(new IncidentPropagationContext(_world, source, binding.Incident), target);
                        _ = new IncidentExposureObservation(observation.Eligible, observation.Blocker);
                        var effect=IncidentPropagationContracts.ReadExposureEffect(_world,binding.Rule);
                        if(observation.Eligible&&effect!=null){
                            Code refusal=effect.CheckExposure(new IncidentPropagationContext(_world,source,binding.Incident),target);
                            if(!refusal.IsNone)observation=IncidentExposureObservation.Refused(refusal);
                        }
                        exposureScope.Verify(); targetScope.Verify(); sourceScope.Verify();
                        return observation;
                    }
                    catch (Exception error) { _world.LatchFault("incident.propagation.observe", error); throw; }
                },_world.Space);
            }
            finally { _observing = false; }
        }
        private Code Target(Binding binding, EntityId id, out Entity target, out IPhasedIncidentResponse incident, out IncidentLifecycleSnapshot snapshot)
        {
            target = _world.Entities.Get(id); incident = null; snapshot = default;
            if (target == null || !target.Alive) return IncidentPropagationCodes.TargetGone;
            if (!binding.Rule.CrossCompany && target.Owner != binding.Emitter.Entity.Owner) return IncidentPropagationCodes.Company;
            incident = target.Get<IPhasedIncidentResponse>(binding.Rule.TargetSource);
            if (incident == null) return IncidentCodes.NotResponse;
            snapshot = IncidentPhases.ReadLifecycle(_world, incident, out _);
            if (snapshot.Kind != binding.Rule.TargetKind) return IncidentPropagationCodes.TargetChanged;
            return snapshot.Active ? IncidentPropagationCodes.TargetActive : Code.None;
        }
        private void Advance(Binding binding)
        {
            Record record = binding.Record; IIncidentPropagationRule rule = binding.Rule;
            if (record.Pending.Count > 0 && record.Pending[0].DueTick <= _world.Tick)
            {
                PendingExposureSnapshot pending = record.Pending[0]; record.Pending.RemoveAt(0);
                var current = IncidentPhases.ReadLifecycle(_world, binding.Emitter.Entity.Get<IPhasedIncidentResponse>(binding.Emitter.Source), out _);
                if (!current.Active || current.Episode != record.Episode) { record.Blocker = IncidentPropagationCodes.SourceEnded; return; }
                record.Blocker = Target(binding, pending.Target, out Entity target, out IPhasedIncidentResponse incident, out var snapshot);
                if (!record.Blocker.IsNone) return;
                if (snapshot.Episode != pending.ExpectedEpisode) { record.Blocker = IncidentPropagationCodes.TargetChanged; return; }
                var observation = Observe(binding, target); record.Blocker = observation.Blocker;
                if (!observation.Eligible) return;
                var effect=IncidentPropagationContracts.ReadExposureEffect(_world,rule);
                IncidentExposurePreparation prepared=null;
                if(effect!=null){
                    prepared=EntityObservation.ReadProvider(_world,effect,target,()=>{
                        Entity source=binding.Emitter.Entity;
                        var sourceScope=EntityObservation.CaptureProvider(_world,source,source.Sync);
                        var candidate=effect.PrepareExposure(new IncidentPropagationContext(_world,source,current),target);
                        sourceScope.Verify();return candidate;
                    },_world.Space,observeTargetState:true);
                    if(prepared==null)throw new InvalidOperationException("exposure effect omitted its preparation");
                    if(!prepared.Ready){record.Blocker=prepared.Blocker;return;}
                }
                if (!incident.Raise(_world, rule.TargetKind)) throw new InvalidOperationException("eligible incident provider refused propagation Raise");
                var raised = IncidentPhases.ReadLifecycle(_world, incident, out _);
                if (!raised.Active || raised.Episode != checked(snapshot.Episode + 1) || raised.RaisedTick != _world.Tick)
                    throw new InvalidOperationException("incident provider violated exact propagation episode receipt");
                prepared?.Commit();
                return;
            }
            if (!record.Scanning)
            {
                record.Scanning = true; record.After = EntityId.None;
                record.Through = new EntityId(checked(_world.Entities.NextIdentity.Value - 1));
            }
            Entity candidate = null;
            foreach (Entity entity in _world.Entities.All)
                if (entity.Alive && entity.Id.Value > record.After.Value && entity.Id.Value <= record.Through.Value &&
                    (candidate == null || entity.Id.Value < candidate.Id.Value)) candidate = entity;
            if (candidate == null)
            {
                record.Scanning = false; record.After = record.Through = EntityId.None;
                record.Next = checked(record.Next + _world.Time.ExactTicks(rule.Interval)); return;
            }
            record.After = candidate.Id;
            if (candidate.Id == record.Entity) return;
            foreach (var pending in record.Pending) if (pending.Target == candidate.Id) return;
            record.Blocker = Target(binding, candidate.Id, out Entity targetEntity, out _, out var targetSnapshot);
            if (!record.Blocker.IsNone) return;
            var eligible = Observe(binding, targetEntity); record.Blocker = eligible.Blocker;
            if (!eligible.Eligible) return;
            record.Pending.Add(new PendingExposureSnapshot(candidate.Id, targetSnapshot.Episode, _world.Tick,
                checked(_world.Tick + _world.Time.ExactTicks(rule.Delay))));
            record.Pending.Sort(ComparePending);
        }
        public Code ReadEligibility(EntityId sourceId, Code capability, EntityId targetId, PlayerId viewer, out IncidentEligibilitySnapshot snapshot)
        {
            snapshot = null; _world.RequireHealthyUpdate();
            if (capability.IsNone) return Codes.InvalidArgument;
            Entity source = _world.Entities.Get(sourceId), target = _world.Entities.Get(targetId);
            if (source == null || !source.Alive || target == null || !target.Alive ||
                viewer.IsValid && (source.Owner != viewer || target.Owner != viewer)) return Codes.NotFound;
            var emitter = source.Get<IIncidentPropagationEmitter>(capability);
            if (emitter == null) return Codes.NotFound;
            var rule = IncidentPropagationContracts.ReadEmitter(_world, emitter, source);
            var incident = IncidentPhases.ReadLifecycle(_world, source.Get<IPhasedIncidentResponse>(emitter.Source), out _);
            IncidentExposureObservation observation;
            if (!incident.Active)
                observation = IncidentExposureObservation.Refused(incident.Episode == 0
                    ? IncidentPropagationCodes.Inactive : IncidentPropagationCodes.SourceEnded);
            else
            {
                var binding = new Binding { Emitter = emitter, Rule = rule, Incident = incident };
                Code blocker = Target(binding, targetId, out _, out _, out _);
                observation = blocker.IsNone ? Observe(binding, target) : IncidentExposureObservation.Refused(blocker);
            }
            snapshot = new IncidentEligibilitySnapshot(sourceId, capability, targetId, rule.Id, incident.Episode,
                _world.Clock.StepIndex, _world.Time.ExactTicks(rule.Delay), observation);
            return Code.None;
        }
        public bool TryRead(EntityId entity, Code capability, PlayerId viewer, out IncidentPropagationSnapshot snapshot)
        {
            _world.RequireHealthyUpdate();
            snapshot = null; Entity owner = _world.Entities.Get(entity);
            if (owner == null || !(viewer == PlayerId.None || owner.Owner == viewer)) return false;
            Record record = Find(entity, capability); if (record == null) return false;
            var pending = new List<PendingExposureSnapshot>();
            foreach (var item in record.Pending)
            {
                Entity target = _world.Entities.Get(item.Target);
                if (viewer == PlayerId.None || target != null && target.Owner == viewer) pending.Add(item);
            }
            snapshot = new IncidentPropagationSnapshot(record.Entity, record.Capability, record.Rule, record.Episode, record.Next,
                record.Scanning, viewer == PlayerId.None ? record.After : EntityId.None,
                viewer == PlayerId.None ? record.Through : EntityId.None, record.Blocker, pending); return true;
        }
        private void Validate(List<Record> records, EntityId cursor, Code cursorCapability)
        {
            if (cursor.Value < 0 || cursor.Value >= _world.Entities.NextIdentity.Value || cursor.IsValid == cursorCapability.IsNone)
                throw new CorruptStateException("invalid propagation round-robin cursor");
            if (!cursorCapability.IsNone) ContractIdentity.ValidateId(cursorCapability.Id, "propagation cursor");
            Record previous = null;
            foreach (var record in records)
            {
                if (!record.Entity.IsValid || record.Entity.Value >= _world.Entities.NextIdentity.Value || record.Capability.IsNone ||
                    previous != null && Compare(previous.Entity, previous.Capability, record.Entity, record.Capability) >= 0 ||
                    record.Episode < 0 || record.Next < -1 || record.After.Value < 0 || record.Through.Value < 0 ||
                    record.Through.Value >= _world.Entities.NextIdentity.Value || record.After.Value > record.Through.Value ||
                    record.Scanning && (record.Next < 0 || !record.Through.IsValid) ||
                    !record.Scanning && (record.After.IsValid || record.Through.IsValid)) throw new CorruptStateException("invalid propagation record ordering/timing/cursor");
                ContractIdentity.ValidateId(record.Capability.Id, "propagation capability");
                ContractIdentity.ValidateId(record.Rule, "propagation rule");
                if (record.Episode == 0 && (record.Next != -1 || record.Pending.Count > 0 || record.Scanning))
                    throw new CorruptStateException("unraised source cannot contain propagation work");
                if (!record.Blocker.IsNone) ContractIdentity.ValidateId(record.Blocker.Id, "propagation blocker");
                IIncidentPropagationEmitter emitter = Emitter(record);
                if (emitter != null && emitter.Rule.Id != record.Rule) throw new CorruptStateException("propagation rule binding differs");
                var seen = new HashSet<EntityId>(); PendingExposureSnapshot? last = null;
                foreach (var pending in record.Pending)
                {
                    if (!pending.Target.IsValid || pending.Target == record.Entity || pending.Target.Value >= _world.Entities.NextIdentity.Value ||
                        pending.ExpectedEpisode < 0 || pending.ScheduledTick < 0 || pending.ScheduledTick > _world.Tick || pending.DueTick <= pending.ScheduledTick ||
                        last.HasValue && ComparePending(last.Value, pending) >= 0 || !seen.Add(pending.Target)) throw new CorruptStateException("invalid/duplicate pending exposure");
                    if (emitter != null && pending.DueTick != checked(pending.ScheduledTick + _world.Time.ExactTicks(emitter.Rule.Delay)))
                        throw new CorruptStateException("pending exposure delay differs from authored rule");
                    last = pending;
                }
                previous = record;
            }
        }
        private static void CodeSync(IStateSync sync, ref Code code)
        {
            string id = code.Id; sync.String(ref id);
            if (sync.Reading)
            {
                try { code = id == null ? Code.None : new Code(id); }
                catch (ArgumentException error) { throw new CorruptStateException(error.Message); }
            }
        }
        public override void Sync(IStateSync sync)
        {
            if (!sync.Reading) Validate(_records, _cursor, _cursorCapability);
            EntityId cursor = _cursor; Code cursorCapability = _cursorCapability;
            sync.Entity(ref cursor); CodeSync(sync, ref cursorCapability);
            int count = sync.Count(_records.Count); var records = sync.Reading ? new List<Record>() : _records;
            for (int i = 0; i < count; i++)
            {
                Record record = sync.Reading ? new Record() : records[i];
                sync.Entity(ref record.Entity); CodeSync(sync, ref record.Capability); sync.String(ref record.Rule);
                sync.Int(ref record.Episode); sync.Long(ref record.Next); sync.Bool(ref record.Scanning);
                sync.Entity(ref record.After); sync.Entity(ref record.Through); CodeSync(sync, ref record.Blocker);
                int pendingCount = sync.Count(record.Pending.Count);
                for (int p = 0; p < pendingCount; p++)
                {
                    var pending = sync.Reading ? default : record.Pending[p]; EntityId target = pending.Target;
                    int episode = pending.ExpectedEpisode; long scheduled = pending.ScheduledTick, due = pending.DueTick;
                    sync.Entity(ref target); sync.Int(ref episode); sync.Long(ref scheduled); sync.Long(ref due);
                    if (sync.Reading) record.Pending.Add(new PendingExposureSnapshot(target, episode, scheduled, due));
                }
                if (sync.Reading) records.Add(record);
            }
            if (sync.Reading)
            {
                try { Validate(records, cursor, cursorCapability); }
                catch (Exception error) when (error is ArgumentException || error is OverflowException) { throw new CorruptStateException(error.Message); }
                _records = records; _cursor = cursor; _cursorCapability = cursorCapability;
            }
        }
        public override void OnLoaded(World world)
        {
            Validate(_records, _cursor, _cursorCapability);
            foreach (Record record in _records)
            {
                var emitter = Emitter(record); if (emitter == null) continue;
                var source = emitter.Entity.Get<IPhasedIncidentResponse>(emitter.Source);
                var lifecycle = IncidentPhases.ReadLifecycle(world, source, out _);
                if (record.Episode > lifecycle.Episode || record.Episode == 0 && (record.Next != -1 || record.Pending.Count > 0))
                    throw new CorruptStateException("propagation observed a future or unraised source episode");
                foreach (var pending in record.Pending)
                {
                    var target = world.Entities.Get(pending.Target)?.Get<IPhasedIncidentResponse>(emitter.Rule.TargetSource);
                    if (target != null && pending.ExpectedEpisode > IncidentPhases.ReadLifecycle(world, target, out _).Episode)
                        throw new CorruptStateException("propagation expects a future target episode");
                }
            }
        }
    }
}
