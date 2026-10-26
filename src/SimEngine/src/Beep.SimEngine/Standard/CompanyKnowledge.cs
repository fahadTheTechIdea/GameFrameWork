using System;
using System.Collections.Generic;
using System.Text;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public enum KnowledgeRetention { FirstLearned = 1, LatestObservation = 2 }
    /// <summary>A remembered observation, not live world state or a resource balance.</summary>
    public readonly struct KnowledgeObservation
    {
        public PlayerId Company { get; }
        public string Provider { get; }
        public string Subject { get; }
        public int SchemaVersion { get; }
        public long LearnedTick { get; }
        public long ObservedTick { get; }
        public JsonValue Data { get; }
        internal KnowledgeObservation(PlayerId company, string provider, string subject, int schema, long learned, long observed, JsonValue data)
        { Company = company; Provider = provider; Subject = subject; SchemaVersion = schema; LearnedTick = learned; ObservedTick = observed; Data = data; }
    }
    /// <summary>Declared engine-side semantic validator; no independent learned table or codec.</summary>
    public interface IKnowledgeProvider
    {
        string Id { get; }
        int SchemaVersion { get; }
        World World { get; }
        /// <summary>Pure, explicit retention for a validated subject; no implicit default.</summary>
        KnowledgeRetention Retention(string subject);
        Code CheckObservation(string subject, JsonValue data);
        void ValidateKnowledge(IReadOnlyList<KnowledgeObservation> observations);
    }
    public interface ICompanyKnowledge
    {
        Code CheckRecord(PlayerId company, string provider, string subject, JsonValue data);
        Code Record(PlayerId company, string provider, string subject, JsonValue data);
        bool TryRead(PlayerId viewer, PlayerId company, string provider, string subject, out KnowledgeObservation observation);
        void CopyKnown(PlayerId viewer, IList<KnowledgeObservation> destination);
    }
    /// <summary>Optional explicit metadata on the actual observation provider; null is authored permanence.</summary>
    public interface IKnowledgeFreshnessProvider : IKnowledgeProvider
    {
        GameDuration? FreshFor(string subject);
    }
    public interface IKnowledgeFreshness
    {
        Code ReadFreshness(PlayerId viewer, PlayerId company, string provider, string subject, out KnowledgeFreshnessSnapshot snapshot);
    }
    /// <summary>Detached historical reading plus freshness derived from the sole game clock.</summary>
    public sealed class KnowledgeFreshnessSnapshot
    {
        public KnowledgeObservation Observation { get; }
        public GameDuration? FreshFor { get; }
        public long? FreshSteps { get; }
        public long AgeSteps { get; }
        public GameDuration Age { get; }
        public bool IsFresh => !FreshSteps.HasValue || AgeSteps < FreshSteps.Value;
        internal KnowledgeFreshnessSnapshot(KnowledgeObservation observation, GameDuration? freshFor, long? freshSteps, long age, ISimulationTime time)
        { Observation = observation; FreshFor = freshFor; FreshSteps = freshSteps; AgeSteps = age; Age = GameDuration.FromSteps(age, time); }
    }
    public static class KnowledgeCodes
    {
        public static readonly Code Known = new Code("knowledge.already-known");
        public static readonly Code MissingProvider = new Code("knowledge.missing-provider");
        public static readonly Code UnsupportedFreshness = new Code("knowledge.no-freshness-policy");
    }
    public sealed class CompanyKnowledgeService : ICompanyKnowledge, IKnowledgeFreshness, IKnowledgeSharing, IKnowledgeForgetting, IPersistentService, ICompositionValidator
    {
        private readonly World _world;
        private List<KnowledgeObservation> _known = new List<KnowledgeObservation>();
        private bool _checking;
        public CompanyKnowledgeService(World world) { _world = world ?? throw new ArgumentNullException(nameof(world)); }
        public string Id => CompanyKnowledgeModule.ModuleId;
        public World World => _world;
        public int StateSchemaVersion => 3;
        private IReadOnlyList<IKnowledgeProvider> Providers() => _world.Services.Providers<IKnowledgeProvider>();
        private T Observe<T>(object provider, Func<T> read)
        {
            _world.RequireHealthyUpdate();
            if (_checking) throw new InvalidOperationException("knowledge provider cannot re-enter ledger mutation");
            _checking = true;
            try { return EntityObservation.ReadProvider(_world, provider, null, read); }
            finally { _checking = false; }
        }
        private IReadOnlyList<KnowledgeSubjectKey> ReadSubjects(IKnowledgeSubjectPolicy policy)
        {
            KnowledgeContentContracts.ValidateAlias(_world, policy);
            var subjects = policy.Subjects; KnowledgeSubjectKeys.Validate(subjects, policy.Id);
            return new List<KnowledgeSubjectKey>(subjects).AsReadOnly();
        }
        public void ValidateComposition(World world)
        {
            if (!ReferenceEquals(world, _world)) throw new ContentException("foreign knowledge session");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (IKnowledgeProvider provider in Providers())
            {
                Observe(provider, () => {
                    try { ContractIdentity.Validate(provider.Id, provider.SchemaVersion, "knowledge provider"); }
                    catch (ArgumentException error) { throw new ContentException(error.Message); }
                    if (!ReferenceEquals(provider.World, world) || !ids.Add(provider.Id)) throw new ContentException("duplicate/foreign knowledge provider");
                    return true;
                });
            }
            foreach (IKnowledgeSubjectPolicy policy in world.Content.All<IKnowledgeSubjectPolicy>())
            {
                var subjects = Observe(policy, () => { var keys = ReadSubjects(policy); policy.ValidateBinding(world); return keys; });
                ValidateKnowledgePolicy(policy, subjects);
            }
        }
        private IKnowledgeProvider Provider(string id)
        { foreach (IKnowledgeProvider provider in Providers()) if (Observe(provider, () => provider.Id) == id) return provider; return null; }
        private static KnowledgeRetention Retention(IKnowledgeProvider provider, string subject)
        {
            KnowledgeRetention retention = provider.Retention(subject);
            if (retention != KnowledgeRetention.FirstLearned && retention != KnowledgeRetention.LatestObservation)
                throw new InvalidOperationException("knowledge provider returned invalid retention: " + provider.Id + "/" + subject);
            return retention;
        }
        internal static string Canonical(JsonValue data)
        { var text = new StringBuilder(); data.WriteCanonical(text); return text.ToString(); }
        private static JsonValue Capture(JsonValue data)
        { if (data == null) return null; JsonValue captured = data.Clone(); captured.Freeze(); return captured; }
        private static int Compare(KnowledgeObservation a, KnowledgeObservation b)
        { int c = a.Company.Value.CompareTo(b.Company.Value); if (c != 0) return c;
            c = string.CompareOrdinal(a.Provider, b.Provider); return c != 0 ? c : string.CompareOrdinal(a.Subject, b.Subject); }
        private KnowledgeObservation? Find(PlayerId company, string provider, string subject)
        { foreach (KnowledgeObservation fact in _known) if (fact.Company == company && fact.Provider == provider && fact.Subject == subject) return fact; return null; }
        public Code CheckRecord(PlayerId company, string provider, string subject, JsonValue data)
        {
            _world.RequireHealthyUpdate();
            if (_checking) throw new InvalidOperationException("knowledge checks cannot re-enter recording");
            if (!_world.Players.IsSeat(company)) return Codes.NotSeated;
            try { ContractIdentity.ValidateId(provider, "knowledge provider"); ContractIdentity.ValidateId(subject, "knowledge subject"); }
            catch (ArgumentException) { return Codes.InvalidArgument; }
            IKnowledgeProvider validator = Provider(provider); if (validator == null) return KnowledgeCodes.MissingProvider;
            JsonValue captured = Capture(data); if (captured == null) return Codes.InvalidArgument;
            return Observe(validator, () =>
            {
                Code check = validator.CheckObservation(subject, captured); if (!check.IsNone) return check;
                KnowledgeRetention retention = Retention(validator, subject);
                KnowledgeObservation? known = Find(company, provider, subject);
                return !known.HasValue || retention == KnowledgeRetention.LatestObservation || Canonical(known.Value.Data) == Canonical(captured)
                    ? Code.None : KnowledgeCodes.Known;
            });
        }
        public Code Record(PlayerId company, string provider, string subject, JsonValue data)
        {
            _world.RequireStateMutation();
            JsonValue captured = Capture(data); Code check = CheckRecord(company, provider, subject, captured);
            if (!check.IsNone) return check;
            IKnowledgeProvider validator = Provider(provider);
            KnowledgeObservation? known = Find(company, provider, subject);
            if (known.HasValue && (Observe(validator, () => Retention(validator, subject)) == KnowledgeRetention.FirstLearned
                || known.Value.ObservedTick == _world.Tick && Canonical(known.Value.Data) == Canonical(captured))) return Code.None;
            var candidate = new List<KnowledgeObservation>(_known);
            if (known.HasValue) candidate.RemoveAll(fact => fact.Company == company && fact.Provider == provider && fact.Subject == subject);
            int schema = Observe(validator, () => validator.SchemaVersion);
            candidate.Add(new KnowledgeObservation(company, provider, subject, schema,
                known?.LearnedTick ?? _world.Tick, _world.Tick, captured));
            candidate.Sort(Compare); Publish(candidate); return Code.None;
        }
        public bool TryRead(PlayerId viewer, PlayerId company, string provider, string subject, out KnowledgeObservation observation)
        {
            observation = default;
            _world.RequireHealthyUpdate();
            if (!_world.Players.IsSeat(company) || viewer != PlayerId.None && (!_world.Players.IsSeat(viewer) || viewer != company)) return false;
            KnowledgeObservation? found = Find(company, provider, subject); if (!found.HasValue) return false;
            observation = found.Value; return true;
        }
        public Code ReadFreshness(PlayerId viewer, PlayerId company, string provider, string subject, out KnowledgeFreshnessSnapshot snapshot)
        {
            snapshot = null;
            _world.RequireHealthyUpdate();
            if (!_world.Players.IsSeat(company) || viewer != PlayerId.None && (!_world.Players.IsSeat(viewer) || viewer != company)) return Codes.NotFound;
            try { ContractIdentity.ValidateId(provider, "knowledge provider"); ContractIdentity.ValidateId(subject, "knowledge subject"); }
            catch (ArgumentException) { return Codes.InvalidArgument; }
            if (!TryRead(viewer, company, provider, subject, out KnowledgeObservation observation)) return Codes.NotFound;
            IKnowledgeProvider actual = Provider(provider);
            if (!(actual is IKnowledgeFreshnessProvider policy)) return KnowledgeCodes.UnsupportedFreshness;
            if (_checking) throw new InvalidOperationException("knowledge freshness provider cannot re-enter its ledger");
            var observed = Observe(actual, () => {
                GameDuration? duration = policy.FreshFor(subject); KnowledgeRetention retention = Retention(actual, subject);
                if (duration.HasValue && (duration.Value.IsZero || retention != KnowledgeRetention.LatestObservation))
                    throw new InvalidOperationException("finite freshness requires positive duration and refreshable retention");
                long? steps = duration.HasValue ? _world.Time.Ticks(duration.Value) : (long?)null;
                long age = checked(_world.Clock.StepIndex - observation.ObservedTick);
                if (age < 0) throw new InvalidOperationException("future knowledge observation");
                return new KnowledgeFreshnessSnapshot(observation, duration, steps, age, _world.Time);
            });
            _world.RequireHealthyUpdate(); snapshot = observed;
            return Code.None;
        }
        public void CopyKnown(PlayerId viewer, IList<KnowledgeObservation> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination)); destination.Clear();
            _world.RequireHealthyUpdate();
            if (viewer != PlayerId.None && !_world.Players.IsSeat(viewer)) return;
            foreach (KnowledgeObservation observation in _known) if (viewer == PlayerId.None || viewer == observation.Company) destination.Add(observation);
        }
        private void ValidateKnowledgePolicy(IKnowledgeSubjectPolicy policy, IReadOnlyList<KnowledgeSubjectKey> subjects)
        {
            if (!(policy is IKnowledgeSharingPolicy) && !(policy is IKnowledgeForgettingPolicy)) throw new ContentException(policy.Id + ": unknown knowledge policy capability");
            foreach (var key in subjects)
            {
                IKnowledgeProvider provider = Provider(key.Provider);
                if (provider == null || policy is IKnowledgeSharingPolicy && !(provider is IKnowledgeSharingProvider)
                    || policy is IKnowledgeForgettingPolicy && !(provider is IKnowledgeForgettingProvider)) throw new ContentException(policy.Id + ": missing actual knowledge policy provider: " + key.Provider);
                Code check = Observe(provider, () =>
                {
                    Code result = Code.None;
                    if (policy is IKnowledgeSharingPolicy) result = ((IKnowledgeSharingProvider)provider).CheckSharingSubject(key.Subject);
                    if (result.IsNone && policy is IKnowledgeForgettingPolicy) result = ((IKnowledgeForgettingProvider)provider).CheckForgettingSubject(key.Subject);
                    return result;
                });
                if (!check.IsNone) throw new ContentException(policy.Id + ": unsupported knowledge policy subject: " + key.Subject + "/" + check);
            }
        }
        public Code PreviewShare(PlayerId sender, PlayerId recipient, string policyId, out KnowledgeShareSnapshot snapshot)
        {
            snapshot = null;
            _world.RequireHealthyUpdate();
            if (_checking) throw new InvalidOperationException("knowledge sharing checks cannot re-enter ledger mutation");
            if (!_world.Players.IsSeat(sender) || !_world.Players.IsSeat(recipient)) return Codes.NotSeated;
            if (sender == recipient) return Codes.InvalidArgument;
            try { ContractIdentity.ValidateId(policyId, "knowledge sharing policy"); }
            catch (ArgumentException) { return Codes.InvalidArgument; }
            IKnowledgeSharingPolicy policy = _world.Content.Find<IKnowledgeSharingPolicy>(policyId);
            if (policy == null || !_world.ViewFor(sender).CanSeeContent(policy)) return Codes.NotFound;
            var facts = Observe(policy, () => (Subjects: ReadSubjects(policy), Allowed: policy.Allows(_world, sender, recipient)));
            if (!facts.Allowed) return Codes.NotAllowed;
            var entries = new List<KnowledgeSubjectSnapshot>(); Code blocker = Code.None;
            foreach (var key in facts.Subjects)
            {
                KnowledgeObservation? outgoing = Find(sender, key.Provider, key.Subject); Code check = Code.None;
                if (!(Provider(key.Provider) is IKnowledgeSharingProvider provider)) check = KnowledgeSharingCodes.Unsupported;
                else if (!outgoing.HasValue) check = Codes.NotFound;
                else
                {
                    check = Observe(provider, () => provider.CheckShare(outgoing.Value, recipient));
                }
                entries.Add(new KnowledgeSubjectSnapshot(key, outgoing, check));
                if (blocker.IsNone && !check.IsNone) blocker = check;
            }
            _world.RequireHealthyUpdate(); snapshot = new KnowledgeShareSnapshot(sender, recipient, policyId, entries); return blocker;
        }
        public Code Share(PlayerId sender, PlayerId recipient, string policy)
        {
            _world.RequireStateMutation();
            Code check = PreviewShare(sender, recipient, policy, out KnowledgeShareSnapshot snapshot);
            if (!check.IsNone) return check;
            var candidate = new List<KnowledgeObservation>(_known);
            foreach (var entry in snapshot.Entries)
            {
                KnowledgeObservation outgoing = entry.Observation.Value;
                KnowledgeObservation? known = Find(recipient, entry.Key.Provider, entry.Key.Subject);
                IKnowledgeProvider provider = Provider(entry.Key.Provider);
                KnowledgeRetention retention = Observe(provider, () => Retention(provider, entry.Key.Subject));
                if (known.HasValue && (retention == KnowledgeRetention.FirstLearned || known.Value.ObservedTick >= outgoing.ObservedTick)) continue;
                candidate.RemoveAll(fact => fact.Company == recipient && fact.Provider == entry.Key.Provider && fact.Subject == entry.Key.Subject);
                candidate.Add(new KnowledgeObservation(recipient, entry.Key.Provider, entry.Key.Subject, outgoing.SchemaVersion,
                    known?.LearnedTick ?? _world.Tick, outgoing.ObservedTick, outgoing.Data));
            }
            candidate.Sort(Compare); Publish(candidate); return Code.None;
        }
        public Code PreviewForget(PlayerId company, string policyId, out KnowledgeForgetSnapshot snapshot)
        {
            snapshot = null;
            _world.RequireHealthyUpdate();
            if (_checking) throw new InvalidOperationException("knowledge forgetting checks cannot re-enter ledger mutation");
            if (!_world.Players.IsSeat(company)) return Codes.NotSeated;
            try { ContractIdentity.ValidateId(policyId, "knowledge forgetting policy"); }
            catch (ArgumentException) { return Codes.InvalidArgument; }
            IKnowledgeForgettingPolicy policy = _world.Content.Find<IKnowledgeForgettingPolicy>(policyId);
            if (policy == null || !_world.ViewFor(company).CanSeeContent(policy)) return Codes.NotFound;
            var facts = Observe(policy, () => (Subjects: ReadSubjects(policy), Allowed: policy.Allows(_world, company)));
            if (!facts.Allowed) return Codes.NotAllowed;
            var removed = new SortedDictionary<string, List<KnowledgeObservation>>(StringComparer.Ordinal);
            foreach (var key in facts.Subjects)
            {
                if (!removed.TryGetValue(key.Provider, out var list)) { list = new List<KnowledgeObservation>(); removed.Add(key.Provider, list); }
                KnowledgeObservation? fact = Find(company, key.Provider, key.Subject); if (fact.HasValue) list.Add(fact.Value);
            }
            var refusals = new Dictionary<string, Code>(StringComparer.Ordinal); Code blocker = Code.None;
            foreach (var group in removed)
            {
                Code check = KnowledgeForgettingCodes.Unsupported;
                if (Provider(group.Key) is IKnowledgeForgettingProvider provider)
                {
                    var retained = _known.FindAll(fact => fact.Company == company && fact.Provider == group.Key
                        && !group.Value.Exists(removedFact => removedFact.Subject == fact.Subject));
                    check = Observe(provider, () => provider.CheckForget(company, group.Value.AsReadOnly(), retained.AsReadOnly()));
                }
                refusals.Add(group.Key, check); if (blocker.IsNone && !check.IsNone) blocker = check;
            }
            var entries = new List<KnowledgeSubjectSnapshot>();
            foreach (var key in facts.Subjects) entries.Add(new KnowledgeSubjectSnapshot(key, Find(company, key.Provider, key.Subject), refusals[key.Provider]));
            _world.RequireHealthyUpdate(); snapshot = new KnowledgeForgetSnapshot(company, policyId, entries); return blocker;
        }
        public Code Forget(PlayerId company, string policy)
        {
            _world.RequireStateMutation();
            Code check = PreviewForget(company, policy, out KnowledgeForgetSnapshot snapshot); if (!check.IsNone) return check;
            var candidate = new List<KnowledgeObservation>(_known);
            foreach (var entry in snapshot.Entries) candidate.RemoveAll(fact => fact.Company == company && fact.Provider == entry.Key.Provider && fact.Subject == entry.Key.Subject);
            Publish(candidate); return Code.None;
        }
        private void Publish(List<KnowledgeObservation> candidate)
        {
            _world.RequireStateMutation();
            Validate(candidate);
            _world.RequireStateMutation();
            _known = candidate;
        }
        private void Validate(List<KnowledgeObservation> candidate)
        {
            _world.RequireHealthyUpdate();
            if (_checking) throw new InvalidOperationException("knowledge validator re-entered");
            KnowledgeObservation? previous = null;
            foreach (KnowledgeObservation observation in candidate)
            {
                try { ContractIdentity.ValidateId(observation.Provider, "knowledge provider"); ContractIdentity.ValidateId(observation.Subject, "knowledge subject"); }
                catch (ArgumentException) { throw new CorruptStateException("invalid knowledge key"); }
                IKnowledgeProvider provider = Provider(observation.Provider);
                if (provider == null || !_world.Players.IsSeat(observation.Company) || observation.LearnedTick < 0
                    || observation.LearnedTick > _world.Tick || observation.ObservedTick < 0 || observation.ObservedTick > _world.Tick
                    || observation.Data == null || !observation.Data.Frozen || previous.HasValue && Compare(previous.Value, observation) >= 0)
                    throw new CorruptStateException("invalid knowledge identity/schema/data/time");
                Observe(provider, () => {
                    if (!ReferenceEquals(provider.World, _world) || observation.SchemaVersion != provider.SchemaVersion
                        || !provider.CheckObservation(observation.Subject, observation.Data).IsNone)
                        throw new CorruptStateException("invalid knowledge provider/schema/data");
                    _ = Retention(provider, observation.Subject); return true;
                });
                previous = observation;
            }
            foreach (IKnowledgeProvider provider in Providers())
                Observe(provider, () => { provider.ValidateKnowledge(candidate.FindAll(fact => fact.Provider == provider.Id).AsReadOnly()); return true; });
        }
        public void Sync(IStateSync sync)
        {
            if (sync.Reading) _world.RequireStateMutation();
            else Validate(_known);
            var candidate = sync.Reading ? new List<KnowledgeObservation>() : _known;
            int count = sync.Count(_known.Count);
            for (int i = 0; i < count; i++)
            {
                KnowledgeObservation fact = sync.Reading ? default : _known[i]; PlayerId company = fact.Company;
                string provider = fact.Provider, subject = fact.Subject, data = sync.Reading ? null : Canonical(fact.Data);
                int schema = fact.SchemaVersion; long learned = fact.LearnedTick, observed = fact.ObservedTick;
                sync.Player(ref company); sync.String(ref provider); sync.String(ref subject); sync.Int(ref schema);
                sync.Long(ref learned); sync.Long(ref observed); sync.String(ref data);
                if (sync.Reading)
                {
                    JsonValue value; try { value = Capture(Json.Parse(data)); } catch (Exception error) when (error is ContentException || error is ArgumentException || error is NullReferenceException)
                    { throw new CorruptStateException("invalid knowledge payload"); }
                    candidate.Add(new KnowledgeObservation(company, provider, subject, schema, learned, observed, value));
                }
            }
            if (sync.Reading) Publish(candidate);
        }
    }
    public interface IKnowledgeSubjectDefinition : IContentDefinition
    {
        KnowledgeRetention Retention { get; }
        GameDuration? FreshFor { get; }
        Code CheckTarget(World world, Entity target);
        JsonValue Observe(World world, Entity target);
        Code CheckObservation(World world, JsonValue data);
    }
    /// <summary>Actual immutable authored subject/policy metadata; no second knowledge catalog.</summary>
    public static class KnowledgeContentContracts
    {
        public static void ValidateAlias(World world, IContentDefinition definition)
        {
            if (!(definition is Def actual) || !actual.IsFrozen || !world.Content.Owns(definition)
                || definition is IPersistentService)
                throw new ContentException("knowledge content requires actual frozen immutable metadata");
            bool alias = definition is IKnowledgeSubjectDefinition subject
                ? ReferenceEquals(world.Content.Find<IKnowledgeSubjectDefinition>(definition.Id), subject)
                : definition is IKnowledgeSubjectPolicy policy &&
                    ReferenceEquals(world.Content.Find<IKnowledgeSubjectPolicy>(definition.Id), policy);
            if (!alias) throw new ContentException(definition.Id + ": invalid knowledge contract alias");
        }
        public static void ValidateSubject(World world, IKnowledgeSubjectDefinition subject)
        {
            ValidateAlias(world, subject);
            EntityObservation.ReadProvider(world, subject, null, () =>
            {
                KnowledgeRetention retention = subject.Retention;
                GameDuration? fresh = subject.FreshFor;
                if (retention != KnowledgeRetention.FirstLearned && retention != KnowledgeRetention.LatestObservation)
                    throw new ContentException(subject.Id + ": invalid knowledge retention");
                if (fresh.HasValue && (fresh.Value.IsZero || retention != KnowledgeRetention.LatestObservation))
                    throw new ContentException(subject.Id + ": finite freshness requires positive duration and latest retention");
                if (fresh.HasValue)
                    try { _ = world.Time.Ticks(fresh.Value); }
                    catch (OverflowException) { throw new ContentException(subject.Id + ": freshness exceeds selected session time representation"); }
                if (subject is ISpatialKnowledgeSubjectDefinition spatial) SpatialReadingDeclaration.ValidateBinding(world, spatial);
                return true;
            });
        }
    }
    public static class KnowledgeRetentionPolicy
    {
        public static GameDuration? ReadFreshness(JsonReader reader, KnowledgeRetention retention)
        {
            JsonValue value = reader.Value("fresh_for") ?? throw reader.Error("explicit fresh_for duration or null required");
            if (value.Kind == JsonKind.Null) return null;
            GameDuration duration = new JsonReader(value, reader.Where + ".fresh_for").RationalDuration();
            if (duration.IsZero || retention != KnowledgeRetention.LatestObservation)
                throw reader.Error("finite freshness requires a positive duration and latest retention");
            return duration;
        }
        public static KnowledgeRetention Read(JsonReader reader)
        {
            string policy = reader.String("retention");
            return policy == "first" ? KnowledgeRetention.FirstLearned
                : policy == "latest" ? KnowledgeRetention.LatestObservation : throw reader.Error("explicit first/latest retention required");
        }
    }
    public sealed class KnowledgeFactDef : Def, IKnowledgeSubjectDefinition
    {
        public JsonValue Value { get; private set; }
        public KnowledgeRetention Retention => KnowledgeRetention.FirstLearned;
        public GameDuration? FreshFor => null;
        public Code CheckTarget(World world, Entity target) => Code.None;
        public JsonValue Observe(World world, Entity target) => Value;
        public Code CheckObservation(World world, JsonValue data)
            => CompanyKnowledgeService.Canonical(Value) == CompanyKnowledgeService.Canonical(data) ? Code.None : Codes.InvalidArgument;
        protected override void ReadCore(JsonReader reader)
        { KnowledgeRetentionPolicy.ReadFreshness(reader, Retention);
            Value = (reader.Child("value") ?? throw reader.Error("fact requires explicit value object")).Raw.Clone(); Value.Freeze(); }
    }
    internal sealed class AuthoredKnowledgeProvider : IKnowledgeFreshnessProvider, IKnowledgeSharingProvider, IKnowledgeForgettingProvider, ICompositionValidator
    {
        public const string ProviderId = "sim.knowledge.facts";
        public AuthoredKnowledgeProvider(World world) { World = world; }
        public string Id => ProviderId;
        public int SchemaVersion => 3;
        public World World { get; }
        public KnowledgeRetention Retention(string subject) => World.Content.Get<IKnowledgeSubjectDefinition>(subject).Retention;
        public GameDuration? FreshFor(string subject) => World.Content.Get<IKnowledgeSubjectDefinition>(subject).FreshFor;
        public void ValidateComposition(World world)
        {
            if (!ReferenceEquals(world, World)) throw new ContentException("foreign authored knowledge session");
            foreach (IKnowledgeSubjectDefinition subject in world.Content.All<IKnowledgeSubjectDefinition>())
                KnowledgeContentContracts.ValidateSubject(world, subject);
        }
        public Code CheckObservation(string subject, JsonValue data)
        { IKnowledgeSubjectDefinition definition = World.Content.Find<IKnowledgeSubjectDefinition>(subject);
            return definition == null ? Codes.UnknownDef : definition.CheckObservation(World, data); }
        public void ValidateKnowledge(IReadOnlyList<KnowledgeObservation> observations) { }
        public Code CheckSharingSubject(string subject)
            => World.Content.Find<IKnowledgeSubjectDefinition>(subject) == null ? Codes.UnknownDef : Code.None;
        public Code CheckForgettingSubject(string subject) => CheckSharingSubject(subject);
        public Code CheckForget(PlayerId company, IReadOnlyList<KnowledgeObservation> removed, IReadOnlyList<KnowledgeObservation> retained)
        {
            if (!World.Players.IsSeat(company)) return Codes.NotSeated;
            foreach (var fact in removed) if (fact.Company != company || fact.Provider != Id || !CheckObservation(fact.Subject, fact.Data).IsNone) return Codes.InvalidArgument;
            return Code.None;
        }
        public Code CheckShare(KnowledgeObservation outgoing, PlayerId recipient)
            => !World.Players.IsSeat(recipient) ? Codes.NotSeated
                : outgoing.Provider != Id || outgoing.SchemaVersion != SchemaVersion ? Codes.InvalidArgument
                : CheckObservation(outgoing.Subject, outgoing.Data);
    }
    public sealed class KnowledgeEffectDef : Def, IPhysicalWorkEffect
    {
        public System.Collections.Generic.IReadOnlyList<IContentDefinition> ContentDependencies => System.Array.AsReadOnly(new IContentDefinition[] { Subject });
        private string _subject;
        public IKnowledgeSubjectDefinition Subject { get; private set; }
        protected override void ReadCore(JsonReader reader) => _subject = reader.String("subject");
        protected override void ResolveCore(ContentResolver resolver) => Subject = resolver.Def<IKnowledgeSubjectDefinition>(_subject);
        public void RequireCapabilities(World world)
        { if (!world.Services.Has<ICompanyKnowledge>()) throw new ContentException("knowledge effect requires selected knowledge owner"); }
        public Code CheckSubject(World world, Entity target, Order order, IActivityNodeDefinition node)
        {
            Code check = Subject.CheckTarget(world, target); if (!check.IsNone) return check;
            return world.Services.Get<ICompanyKnowledge>().CheckRecord(order.Owner, AuthoredKnowledgeProvider.ProviderId, Subject.Id, Subject.Observe(world, target));
        }
        public Code CommitSubject(World world, Entity target, Order order, IActivityNodeDefinition node)
        {
            Code check = Subject.CheckTarget(world, target); if (!check.IsNone) return check;
            return world.Services.Get<ICompanyKnowledge>().Record(order.Owner, AuthoredKnowledgeProvider.ProviderId, Subject.Id, Subject.Observe(world, target));
        }
    
        public Entity ResolveSubject(World world, Order order, IActivityNodeDefinition node) => world.Entities.Get(order.Target);
        public void ValidatePlan(World world, IActivityDefinition plan, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.ValidatePlan(world, this, plan, node);
        public Code Check(World world, Order order, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.Check(world, this, order, node);
        public WorkEffectResult Apply(World world, Order order, IActivityNodeDefinition node)
            => PhysicalWorkEffectContracts.Apply(world, this, order, node);
    }
}
