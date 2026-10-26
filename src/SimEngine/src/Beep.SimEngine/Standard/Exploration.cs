using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface IExplorationDefinition : INamedCapabilityDefinition
    {
        void ValidateBinding(World world);
    }
    public interface IExplorationSource : INamedEntityCapability
    {
        ExplorationSnapshot ReadExploration(World world);
        void ValidateExploration(World world);
        void AdvanceExploration(ExplorationUpdateContext context);
    }
    /// <summary>Source-specific permission issued by the engine update, not another current clock.</summary>
    public readonly struct ExplorationUpdateContext
    {
        private readonly IExplorationSource _source;
        private readonly long _issued;
        internal ExplorationUpdateContext(World world, IExplorationSource source)
        { World = world; _source = source; _issued = world.Clock.StepIndex; }
        public World World { get; }
        public IGameClock Clock => World.Clock;
        public long Step => _issued;
        public bool IsCurrent(IExplorationSource source) => World != null && World.Stepping && !World.UpdatingEntities
            && ReferenceEquals(_source, source) && World.Clock.StepIndex == _issued && source.Entity.Alive
            && ReferenceEquals(World.Entities.Get(source.Entity.Id), source.Entity);
    }
    public sealed class ExplorationEntrySnapshot
    {
        public string Id { get; }
        public string Region { get; }
        public string Requirement { get; }
        public string Provider { get; }
        public string Subject { get; }
        public GameDuration Duration { get; }
        public long RequiredSteps { get; }
        public WorkProgress Progress { get; }
        public KnowledgeFreshnessSnapshot Knowledge { get; }
        public bool Known => Knowledge != null;
        public bool NeedsObservation => Knowledge == null || !Knowledge.IsFresh;
        public Code Blocker { get; }
        public ExplorationEntrySnapshot(string id, string region, string requirement, string provider, string subject, GameDuration duration,
            long requiredSteps, WorkProgress progress, KnowledgeFreshnessSnapshot knowledge, Code blocker)
        { Id = id; Region = region; Requirement = requirement; Provider = provider; Subject = subject; Duration = duration;
            RequiredSteps = requiredSteps; Progress = progress; Knowledge = knowledge; Blocker = blocker; }
    }
    public sealed class ExplorationSnapshot
    {
        public EntityId Entity { get; }
        public Code Source { get; }
        public IReadOnlyList<ExplorationEntrySnapshot> Entries { get; }
        public ExplorationSnapshot(EntityId entity, Code source, IReadOnlyList<ExplorationEntrySnapshot> entries)
        { Entity = entity; Source = source; Entries = new List<ExplorationEntrySnapshot>(entries ?? throw new ArgumentNullException(nameof(entries))).AsReadOnly(); }
    }
    public static class ExplorationCodes
    {
        public static readonly Code Unsupported = new Code("exploration.no-source"), Outside = new Code("exploration.outside-contact-region"),
            UnsupportedRegion = new Code("exploration.unsupported-region"), NoSensor = new Code("exploration.no-current-sensor-coverage");
    }
    public static class ExplorationContracts
    {
        public static ExplorationSnapshot Read(World world, IExplorationSource source)
        {
            Entity entity = source?.Entity;
            if (world == null || entity == null || !entity.Alive || !ReferenceEquals(world.Entities.Get(entity.Id), entity)
                || !ReferenceEquals(entity.Get<IExplorationSource>(source.CapabilityId), source)) throw new ArgumentException("actual exploration source required");
            world.RequireHealthyUpdate();
            return EntityObservation.ReadProvider(world, source, entity, () => {
            ExplorationSnapshot value = source.ReadExploration(world);
            if (value == null || value.Entity != entity.Id || value.Source != source.CapabilityId) throw new InvalidOperationException("invalid exploration snapshot identity");
            string previous = null;
            foreach (var entry in value.Entries)
            {
                if (entry == null || entry.Duration.IsZero || entry.RequiredSteps != world.Time.Ticks(entry.Duration)
                    || entry.Progress.WholeTicks >= entry.RequiredSteps || entry.Progress.Fraction != 0
                    || previous != null && string.CompareOrdinal(previous, entry.Id) >= 0) throw new InvalidOperationException("invalid exploration progress/order");
                ContractIdentity.ValidateId(entry.Id, "exploration entry"); ContractIdentity.ValidateId(entry.Region, "exploration region");
                ContractIdentity.ValidateId(entry.Requirement, "exploration requirement");
                ContractIdentity.ValidateId(entry.Provider, "knowledge provider"); ContractIdentity.ValidateId(entry.Subject, "knowledge subject");
                if (entry.Knowledge != null && (entry.Knowledge.Observation.Company != entity.Owner
                    || entry.Knowledge.Observation.Provider != entry.Provider || entry.Knowledge.Observation.Subject != entry.Subject))
                    throw new InvalidOperationException("exploration knowledge must identify its actual company provider and subject");
                previous = entry.Id;
            }
            return value;
            }, observeTargetState: true);
        }
    }
    public sealed class AuthoredExplorationEntry
    {
        private readonly string _region, _subject, _requirement;
        public string Id { get; }
        public ISpatialRegionDefinition Region { get; private set; }
        public IExplorationRequirementDefinition Requirement { get; private set; }
        public IKnowledgeSubjectDefinition Subject { get; private set; }
        public GameDuration Duration { get; }
        public bool RetainInterrupted { get; }
        internal AuthoredExplorationEntry(JsonReader reader)
        {
            Id = reader.String("id"); _region = reader.String("region"); _subject = reader.String("subject");
            _requirement = reader.String("requirement");
            Duration = (reader.Child("duration") ?? throw reader.Error("explicit exploration duration required")).RationalDuration();
            string interruption = reader.String("interruption"); reader.End();
            if (Duration.IsZero || interruption != "retain" && interruption != "reset") throw reader.Error("positive duration and explicit retain/reset interruption required");
            try { ContractIdentity.ValidateId(Id, "exploration entry"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            RetainInterrupted = interruption == "retain";
        }
        internal void Resolve(ContentResolver content)
        { Region = content.Def<ISpatialRegionDefinition>(_region); Subject = content.Def<IKnowledgeSubjectDefinition>(_subject);
            Requirement = content.Def<IExplorationRequirementDefinition>(_requirement); }
    }
    public interface IAuthoredExplorationProfileDefinition : IContentDefinition
    {
        IReadOnlyList<AuthoredExplorationEntry> Entries { get; }
        EntityStateBindings States { get; }
    }
    public interface IAuthoredExplorationDefinition : IExplorationDefinition
    {
        Code Sensor { get; }
        IAuthoredExplorationProfileDefinition Profile { get; }
    }
    /// <summary>Sole JSON declaration for unfinished contact observation, not a knowledge owner.</summary>
    public sealed class AuthoredExplorationProfileDeclaration
    {
        public IReadOnlyList<AuthoredExplorationEntry> Entries { get; }
        public EntityStateBindings States { get; private set; }
        private AuthoredExplorationProfileDeclaration(JsonReader reader)
        {
            States = EntityStateBindings.Read(reader, "sampling");
            var array = reader.Array("entries") ?? throw reader.Error("explicit exploration entries required");
            if (array.Count == 0) throw reader.Error("exploration entries cannot be empty");
            var entries = new List<AuthoredExplorationEntry>();
            for (int i = 0; i < array.Count; i++) entries.Add(new AuthoredExplorationEntry(new JsonReader(array[i], reader.Where + ".entries[" + i + "]")));
            entries.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            for (int i = 1; i < entries.Count; i++) if (entries[i].Id == entries[i - 1].Id) throw reader.Error("duplicate exploration entry");
            Entries = entries.AsReadOnly();
        }
        public static AuthoredExplorationProfileDeclaration Read(JsonReader reader)
            => new AuthoredExplorationProfileDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content)
        { States = States.Resolve(content); foreach (var entry in Entries) entry.Resolve(content); }
    }
    public sealed class AuthoredExplorationProfileDef : Def, IAuthoredExplorationProfileDefinition
    {
        private AuthoredExplorationProfileDeclaration _declaration;
        public IReadOnlyList<AuthoredExplorationEntry> Entries => _declaration.Entries;
        public EntityStateBindings States => _declaration.States;
        protected override void ReadCore(JsonReader reader) => _declaration = AuthoredExplorationProfileDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    /// <summary>Sole JSON binding to the actual profile and named sensor on the original entity.</summary>
    public sealed class AuthoredExplorationDeclaration
    {
        private readonly string _profile;
        public Code CapabilityId { get; }
        public Code Sensor { get; }
        public IAuthoredExplorationProfileDefinition Profile { get; private set; }
        private AuthoredExplorationDeclaration(JsonReader reader)
        {
            string id = reader.String("id"), sensor = reader.String("sensor"); _profile = reader.String("profile");
            try { ContractIdentity.ValidateId(id, "exploration source"); ContractIdentity.ValidateId(sensor, "sensor"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            CapabilityId = new Code(id); Sensor = new Code(sensor);
        }
        public static AuthoredExplorationDeclaration Read(JsonReader reader)
            => new AuthoredExplorationDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content) => Profile = content.Def<IAuthoredExplorationProfileDefinition>(_profile);
        public void ValidateOwner(EntityDef owner)
        {
            if (owner == null || Profile == null || Profile.Entries == null)
                throw new ContentException("authored exploration requires an explicit actual profile entry list");
            var sensor = owner.Component<ISpatialSensorDefinition>(Sensor); var presence = owner.Component<ISpatialPresenceDefinition>();
            if (sensor == null || presence == null || sensor.Space != presence.Space
                || !typeof(ISpatialSensor).IsAssignableFrom(((ComponentDef)sensor).ComponentType)) throw new ContentException(owner.Id + ": exploration requires actual compatible presence/sensor");
            foreach (var entry in Profile.Entries)
            {
                if (entry.Region.Space != presence.Space)
                    throw new ContentException(owner.Id + ": authored exploration requires same-space regions");
                AuthoredExplorationContracts.ValidateSubjectPolicy(entry.Subject);
            }
        }
    }
    public sealed class AuthoredExplorationDef : ComponentDef<AuthoredExploration>, IAuthoredExplorationDefinition
    {
        private AuthoredExplorationDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        public Code Sensor => _declaration.Sensor;
        public IAuthoredExplorationProfileDefinition Profile => _declaration.Profile;
        protected override void ReadCore(JsonReader reader) => _declaration = AuthoredExplorationDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateCore(EntityDef owner) => _declaration.ValidateOwner(owner);
        public void ValidateBinding(World world) => AuthoredExplorationContracts.ValidateBinding(world, this);
    }
    public static class AuthoredExplorationContracts
    {
        internal static void ValidateSubjectPolicy(IKnowledgeSubjectDefinition subject)
        {
            if (subject.Retention != KnowledgeRetention.FirstLearned
                && (subject.Retention != KnowledgeRetention.LatestObservation || !subject.FreshFor.HasValue))
                throw new ContentException(subject.Id + ": automatic exploration requires first-learned or finite-freshness latest subjects");
        }
        // Fresh call facts reference the actual immutable declarations, never another live profile.
        internal sealed class ProfileFacts
        {
            internal readonly IReadOnlyList<AuthoredExplorationEntry> Entries;
            internal readonly IEntityStateDefinition Sampling;
            internal ProfileFacts(IReadOnlyList<AuthoredExplorationEntry> entries, IEntityStateDefinition sampling)
            { Entries = entries; Sampling = sampling; }
        }
        private static T Observe<T>(World world, object provider, Entity entity, Func<T> read)
            => EntityObservation.ReadProvider(world, provider, entity, read, observeTargetState: true);
        public static void ValidateProfile(World world, IAuthoredExplorationProfileDefinition profile)
            => ReadProfile(world, profile, null);
        private static ProfileFacts ReadProfile(World world, IAuthoredExplorationProfileDefinition profile, Entity entity)
            => Observe(world, profile, entity, () => {
                if (!(profile is Def actual) || !actual.IsFrozen || profile is IPersistentService ||
                    !world.Content.Owns(profile) || !ReferenceEquals(world.Content.Find<IAuthoredExplorationProfileDefinition>(profile.Id), profile))
                    throw new ContentException("authored exploration needs an actual frozen profile contract alias");
                var entries = profile.Entries;
                var states = profile.States;
                if (entries == null || entries.Count == 0 || states == null || states.Definitions.Count != 1 ||
                    !states.Definitions.TryGetValue("sampling", out var sampling) || !world.Content.Owns(sampling) ||
                    !ReferenceEquals(world.Content.Find<IEntityStateDefinition>(sampling.Id), sampling))
                    throw new ContentException(profile.Id + ": explicit entries and actual sampling state required");
                var facts = new List<AuthoredExplorationEntry>(); string previous = null;
                foreach (var entry in entries)
                {
                    if (entry == null || previous != null && string.CompareOrdinal(previous, entry.Id) >= 0 || entry.Duration.IsZero ||
                        entry.Region == null || !world.Content.Owns(entry.Region) ||
                        !ReferenceEquals(world.Content.Find<ISpatialRegionDefinition>(entry.Region.Id), entry.Region))
                        throw new ContentException(profile.Id + ": canonical entries and actual region references required");
                    SpatialRegionContracts.ValidateAlias(world, entry.Region);
                    EntityObservation.ReadProvider(world, entry.Requirement, entity, () => {
                        if (!(entry.Requirement is Def actualRequirement) || !actualRequirement.IsFrozen
                            || entry.Requirement is IPersistentService || !world.Content.Owns(entry.Requirement)
                            || !ReferenceEquals(world.Content.Find<IExplorationRequirementDefinition>(entry.Requirement.Id), entry.Requirement))
                            throw new ContentException(entry.Id + ": actual frozen exploration requirement required");
                        entry.Requirement.ValidateTarget(world, entry.Region); return true;
                    }, space: world.Space, observeTargetState: true);
                    KnowledgeContentContracts.ValidateSubject(world, entry.Subject);
                    ValidateSubjectPolicy(entry.Subject);
                    try { _ = world.Time.Ticks(entry.Duration); }
                    catch (OverflowException) { throw new ContentException(entry.Id + ": exploration duration exceeds selected session time representation"); }
                    previous = entry.Id; facts.Add(entry);
                }
                return new ProfileFacts(facts.AsReadOnly(), sampling);
            });
        public static void ValidateBinding(World world, IAuthoredExplorationDefinition definition)
            => ReadBinding(world, definition, null);
        internal static ProfileFacts ReadBinding(World world, IAuthoredExplorationDefinition definition, Entity entity)
            => Observe(world, definition, entity, () => {
                if (!(definition is ComponentDef part) || !part.IsFrozen || !world.Content.Owns(part.Owner) ||
                    !ReferenceEquals(part.Owner.Component<IAuthoredExplorationDefinition>(definition.CapabilityId), definition) ||
                    !typeof(IExplorationSource).IsAssignableFrom(part.ComponentType) || !world.Services.Has<ICompanyKnowledge>())
                    throw new ContentException("authored exploration needs its actual source/content/knowledge owners");
                var sensor = part.Owner.Component<ISpatialSensorDefinition>(definition.Sensor);
                var presence = part.Owner.Component<ISpatialPresenceDefinition>();
                if (sensor == null || presence == null || sensor.Space != presence.Space ||
                    !typeof(ISpatialSensor).IsAssignableFrom(((ComponentDef)sensor).ComponentType))
                    throw new ContentException(part.Owner.Id + ": exploration requires actual compatible presence/sensor");
                var facts = ReadProfile(world, definition.Profile, entity);
                foreach (var entry in facts.Entries)
                    if (entry.Region.Space != presence.Space || world.Space?.Id != entry.Region.Space)
                        throw new ContentException(part.Owner.Id + ": foreign exploration region/subject/space");
                return facts;
            });
    }
    /// <summary>Only unfinished authored observation lives here; learned membership remains in the sole ledger.</summary>
    public sealed class AuthoredExploration : Component, IExplorationSource, IEntityStateSource
    {
        public new IAuthoredExplorationDefinition Def => (IAuthoredExplorationDefinition)base.Def;
        private WorkProgress[] _progress;
        private bool _advancing;
        private long _lastAdvance = -1; // Transient consumed permission coordinate, not a clock or saved observation.
        public Code CapabilityId => Def.CapabilityId;
        public override void OnCreate() => _progress = new WorkProgress[Def.Profile.Entries.Count];
        private void RequireActual(World world)
        {
            if (world == null || !Entity.Alive || !ReferenceEquals(world.Entities.Get(Entity.Id), Entity)
                || !ReferenceEquals(Entity.Get<IExplorationSource>(CapabilityId), this)) throw new ArgumentException("exploration requires its actual session source");
        }
        private ExplorationEntrySnapshot Current(World world, int index, AuthoredExplorationEntry entry)
        {
            Code reading = world.Services.Get<IKnowledgeFreshness>().ReadFreshness(Entity.Owner, Entity.Owner,
                AuthoredKnowledgeProvider.ProviderId, entry.Subject.Id, out var knowledge);
            if (!reading.IsNone && reading != Codes.NotFound)
                throw new InvalidOperationException("exploration freshness refused: " + reading);
            Code blocker = Code.None;
            if (knowledge == null || !knowledge.IsFresh)
            {
                if (!world.Players.IsSeat(Entity.Owner)) blocker = Codes.NotSeated;
                else
                {
                    var sensor = Entity.Get<ISpatialSensor>(Def.Sensor);
                    blocker = EntityObservation.ReadProvider(world, entry.Requirement, Entity,
                        () => { Code result = entry.Requirement.CheckTarget(world, Entity, sensor, entry.Region); world.RequireHealthyUpdate(); return result; },
                        space: world.Space, observeTargetState: true);
                    if (blocker.IsNone)
                        blocker = EntityObservation.ReadProvider(world, entry.Subject, Entity, () => entry.Subject.CheckTarget(world, Entity), observeTargetState: true);
                }
            }
            return new ExplorationEntrySnapshot(entry.Id, entry.Region.Id, entry.Requirement.Id, AuthoredKnowledgeProvider.ProviderId, entry.Subject.Id,
                entry.Duration, world.Time.Ticks(entry.Duration), _progress[index], knowledge, blocker);
        }
        public ExplorationSnapshot ReadExploration(World world)
        {
            RequireActual(world); var profile = AuthoredExplorationContracts.ReadBinding(world, Def, Entity);
            var entries = new List<ExplorationEntrySnapshot>();
            for (int i = 0; i < profile.Entries.Count; i++) entries.Add(Current(world, i, profile.Entries[i]));
            return new ExplorationSnapshot(Entity.Id, CapabilityId, entries);
        }
        public void ValidateExploration(World world)
        {
            RequireActual(world); ValidateProgress(world, AuthoredExplorationContracts.ReadBinding(world, Def, Entity));
        }
        private void ValidateProgress(World world, AuthoredExplorationContracts.ProfileFacts profile)
        {
            if (_progress == null || _progress.Length != profile.Entries.Count) throw new CorruptStateException("invalid exploration progress count");
            for (int i = 0; i < _progress.Length; i++)
                if (_progress[i].Fraction != 0 || _progress[i].Complete(world.Time.Ticks(profile.Entries[i].Duration))) throw new CorruptStateException("invalid unfinished exploration progress");
        }
        public void AdvanceExploration(ExplorationUpdateContext context)
        {
            if (!context.IsCurrent(this) || _advancing || _lastAdvance == context.Step) throw new InvalidOperationException("exploration requires one current engine-issued source update");
            _advancing = true; _lastAdvance = context.Step;
            try
            {
                var world = context.World; RequireActual(world);
                var profile = AuthoredExplorationContracts.ReadBinding(world, Def, Entity); ValidateProgress(world, profile);
                for (int i = 0; i < profile.Entries.Count; i++)
                {
                    var entry = profile.Entries[i]; var current = Current(world, i, entry); WorkProgress next = _progress[i]; bool recorded = false;
                    if (!current.NeedsObservation || !current.Blocker.IsNone && !entry.RetainInterrupted) next = WorkProgress.Zero;
                    else if (current.Blocker.IsNone)
                    {
                        next = next.Advance(Fix.One, current.RequiredSteps);
                        if (next.Complete(current.RequiredSteps))
                        {
                            JsonValue data = EntityObservation.ReadProvider(world, entry.Subject, Entity, () => entry.Subject.Observe(world, Entity), observeTargetState: true);
                            Code refusal = world.Services.Get<ICompanyKnowledge>().Record(Entity.Owner, AuthoredKnowledgeProvider.ProviderId, entry.Subject.Id, data);
                            if (!refusal.IsNone) throw new InvalidOperationException("completed exploration recording refused: " + refusal);
                            next = WorkProgress.Zero; recorded = true;
                        }
                    }
                    if (recorded || !next.Equals(_progress[i])) { _progress[i] = next; Entity.Touch(); }
                }
            }
            finally { _advancing = false; }
        }
        public IEntityStateDefinition GetState(EntityUpdateContext context)
        {
            RequireActual(context.World); var profile = AuthoredExplorationContracts.ReadBinding(context.World, Def, Entity);
            for (int i = 0; i < profile.Entries.Count; i++)
            { var entry = Current(context.World, i, profile.Entries[i]); if (entry.NeedsObservation && entry.Blocker.IsNone) return profile.Sampling; }
            return null;
        }
        public override void Sync(IStateSync sync)
        {
            int count = sync.Count(Def.Profile.Entries.Count); if (count != Def.Profile.Entries.Count) throw new CorruptStateException("exploration entry count differs from authoring");
            var candidate = sync.Reading ? new WorkProgress[count] : _progress;
            for (int i = 0; i < count; i++)
            {
                string id = Def.Profile.Entries[i].Id; sync.String(ref id);
                if (id != Def.Profile.Entries[i].Id) throw new CorruptStateException("exploration entry identity/order differs from authoring");
                var value = sync.Reading ? WorkProgress.Zero : candidate[i]; sync.Work(ref value);
                if (value.Fraction != 0) throw new CorruptStateException("authored observation requires whole actual intervals");
                if (sync.Reading) candidate[i] = value;
            }
            if (sync.Reading) _progress = candidate;
        }
    }
}
