using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public enum CompanyAlertLevel : byte { Normal = 1, Urgent = 2, Emergency = 3 }

    public sealed class CompanyAlertSettings
    {
        public string Profile { get; }
        public CompanyAlertLevel Level { get; }
        public IReadOnlyList<WorkerReserveSetting> WorkerReserves { get; }
        public CompanyAlertSettings(string profile, CompanyAlertLevel level, IEnumerable<WorkerReserveSetting> workerReserves)
        {
            if (string.IsNullOrWhiteSpace(profile) || level < CompanyAlertLevel.Normal || level > CompanyAlertLevel.Emergency)
                throw new ArgumentException("explicit alert profile and valid level required");
            if (workerReserves == null) throw new ArgumentException("worker reserves must be explicit, including an empty list");
            var reserves = new List<WorkerReserveSetting>(workerReserves);
            reserves.Sort((a, b) => string.CompareOrdinal(a.Role.Value, b.Role.Value));
            for (int i = 0; i < reserves.Count; i++)
                if (!reserves[i].Role.IsValid || reserves[i].MinimumReady < 0 || i > 0 && reserves[i].Role == reserves[i - 1].Role)
                    throw new ArgumentException("worker reserve roles must be valid and unique with nonnegative counts");
            Profile = profile; Level = level; WorkerReserves = reserves.AsReadOnly();
        }
        internal bool Matches(CompanyAlertSettings other)
        {
            if (other == null || Profile != other.Profile || Level != other.Level || WorkerReserves.Count != other.WorkerReserves.Count) return false;
            for (int i = 0; i < WorkerReserves.Count; i++)
                if (WorkerReserves[i].Role != other.WorkerReserves[i].Role || WorkerReserves[i].MinimumReady != other.WorkerReserves[i].MinimumReady) return false;
            return true;
        }
        public static CompanyAlertSettings Read(JsonReader reader)
        {
            string profile = reader.String("profile"), level = reader.String("level");
            JsonValue values = reader.Array("worker_reserves") ?? throw reader.Error("worker_reserves must be explicit, including []");
            var reserves = new List<WorkerReserveSetting>();
            try
            {
                for (int i = 0; i < values.Count; i++)
                {
                    var row = new JsonReader(values[i], reader.Where + ".worker_reserves[" + i + "]");
                    var value = new WorkerReserveSetting(new WorkerRoleId(row.String("role")), row.Int("minimum_ready")); row.End(); reserves.Add(value);
                }
                return new CompanyAlertSettings(profile, ParseLevel(level), reserves);
            }
            catch (ArgumentException ex) { throw reader.Error(ex.Message); }
        }
        public static CompanyAlertLevel ParseLevel(string value)
            => value == "normal" ? CompanyAlertLevel.Normal : value == "urgent" ? CompanyAlertLevel.Urgent : value == "emergency"
                ? CompanyAlertLevel.Emergency : throw new ArgumentException("alert level must be normal, urgent or emergency");
        public static CompanyAlertSettings Sync(IStateSync sync, CompanyAlertSettings current)
        {
            if (!sync.Reading && current == null) throw new CorruptStateException("missing company alert settings");
            string profile = current?.Profile; CompanyAlertLevel level = current?.Level ?? 0;
            sync.String(ref profile); sync.Enum(ref level);
            int count = sync.Count(current?.WorkerReserves.Count ?? 0); var reserves = sync.Reading ? new List<WorkerReserveSetting>() : null;
            string previous = null;
            for (int i = 0; i < count; i++)
            {
                string role = sync.Reading ? null : current.WorkerReserves[i].Role.Value; int minimum = sync.Reading ? 0 : current.WorkerReserves[i].MinimumReady;
                sync.String(ref role); sync.Int(ref minimum);
                if (string.IsNullOrWhiteSpace(role) || minimum < 0 || previous != null && string.CompareOrdinal(previous, role) >= 0)
                    throw new CorruptStateException("invalid or noncanonical worker reserve settings");
                previous = role;
                if (sync.Reading)
                {
                    try { reserves.Add(new WorkerReserveSetting(new WorkerRoleId(role), minimum)); }
                    catch (ArgumentException ex) { throw new CorruptStateException("invalid reserve role: " + ex.Message); }
                }
            }
            try { return sync.Reading ? new CompanyAlertSettings(profile, level, reserves) : current; }
            catch (ArgumentException ex) { throw new CorruptStateException("invalid company alert settings: " + ex.Message); }
        }
    }
    public readonly struct CompanyAlertSnapshot
    {
        public PlayerId Company { get; }
        public CompanyAlertSettings Settings { get; }
        public int Version { get; }
        public long ChangedTick { get; }
        internal CompanyAlertSnapshot(PlayerId company, CompanyAlertSettings settings, int version, long changedTick)
        { Company = company; Settings = settings; Version = version; ChangedTick = changedTick; }
    }
    public interface ICompanyAlertPolicy
    {
        World World { get; }
        CompanyAlertSnapshot ReadAlert(PlayerId company);
        Code CheckAlert(PlayerId company, CompanyAlertSettings settings);
        void ConfigureAlert(PlayerId company, CompanyAlertSettings settings);
        long EffectivePriority(WorkerDispatchOffer offer);
    }
    public interface ICompanyAlertProfileDefinition : IContentDefinition
    {
        IReadOnlyList<string> DispatchScopes { get; }
        long PriorityAdjustment(CompanyAlertLevel level, WorkerDispatchOffer offer);
    }
    /// <summary>Open content base; existing ResolvedContent indexes alias independently authored subtypes.</summary>
    public abstract class CompanyAlertProfileDef : Def, ICompanyAlertProfileDefinition
    {
        public abstract IReadOnlyList<string> DispatchScopes { get; }
        public abstract long PriorityAdjustment(CompanyAlertLevel level, WorkerDispatchOffer offer);
    }
    public readonly struct CompanyAlertRule
    {
        public string Scope { get; }
        public bool Emergency { get; }
        public int PriorityAdjustment { get; }
        public CompanyAlertRule(string scope, bool emergency, int priorityAdjustment)
        {
            if (scope != null) ContractIdentity.ValidateId(scope, "alert dispatch scope");
            Scope = scope; Emergency = emergency; PriorityAdjustment = priorityAdjustment;
        }
    }
    public sealed class AuthoredCompanyAlertProfileDef : CompanyAlertProfileDef
    {
        private CompanyAlertProfileDeclaration _declaration;
        public override IReadOnlyList<string> DispatchScopes => _declaration.DispatchScopes;
        protected override void ReadCore(JsonReader reader) => _declaration = CompanyAlertProfileDeclaration.Read(reader);
        public override long PriorityAdjustment(CompanyAlertLevel level, WorkerDispatchOffer offer)
            => _declaration.PriorityAdjustment(level, offer);
    }
    /// <summary>Sole native JSON alert rules and stateless adjustment; live settings belong to CompanyAlertPolicies.</summary>
    public sealed class CompanyAlertProfileDeclaration
    {
        private IReadOnlyList<CompanyAlertRule> _normal, _urgent, _emergency;
        private IReadOnlyList<string> _scopes;
        public IReadOnlyList<string> DispatchScopes => _scopes;
        private CompanyAlertProfileDeclaration() { }
        public static CompanyAlertProfileDeclaration Read(JsonReader reader)
        { var declaration = new CompanyAlertProfileDeclaration(); declaration.ReadCore(reader); return declaration; }
        private void ReadCore(JsonReader reader)
        {
            var scopes = new SortedSet<string>(StringComparer.Ordinal);
            _normal = ReadRules(reader, "normal", scopes); _urgent = ReadRules(reader, "urgent", scopes); _emergency = ReadRules(reader, "emergency", scopes);
            _scopes = new List<string>(scopes).AsReadOnly();
        }
        private static IReadOnlyList<CompanyAlertRule> ReadRules(JsonReader reader, string level, ISet<string> scopes)
        {
            JsonValue values = reader.Array(level) ?? throw reader.Error(level + " rules must be explicit, including empty arrays");
            var rules = new List<CompanyAlertRule>(); var identities = new HashSet<(string, bool)>();
            for (int i = 0; i < values.Count; i++)
            {
                var row = new JsonReader(values[i], reader.Where + "." + level + "[" + i + "]");
                JsonValue scope = row.Value("scope"), emergency = row.Value("emergency");
                if (scope == null || scope.Kind != JsonKind.Null && scope.Kind != JsonKind.String || emergency == null || emergency.Kind != JsonKind.Bool)
                    throw row.Error("explicit scope identity/null and emergency boolean required");
                string id = scope.Kind == JsonKind.Null ? null : scope.Text; int adjustment = row.Int("priority_adjustment"); row.End();
                try
                {
                    var rule = new CompanyAlertRule(id, emergency.Bool, adjustment);
                    if (!identities.Add((id, rule.Emergency))) throw row.Error("duplicate alert rule identity");
                    rules.Add(rule); if (id != null) scopes.Add(id);
                }
                catch (ArgumentException ex) { throw row.Error(ex.Message); }
            }
            rules.Sort((a, b) => { int scope = string.CompareOrdinal(a.Scope, b.Scope); return scope != 0 ? scope : a.Emergency.CompareTo(b.Emergency); });
            return rules.AsReadOnly();
        }
        public long PriorityAdjustment(CompanyAlertLevel level, WorkerDispatchOffer offer)
        {
            IReadOnlyList<CompanyAlertRule> rules = level == CompanyAlertLevel.Normal ? _normal : level == CompanyAlertLevel.Urgent ? _urgent
                : level == CompanyAlertLevel.Emergency ? _emergency : throw new ArgumentException("invalid company alert level");
            long adjustment = 0;
            foreach (CompanyAlertRule rule in rules)
                if ((rule.Scope == null || rule.Scope == offer.Scope) && rule.Emergency == offer.Emergency) adjustment = checked(adjustment + rule.PriorityAdjustment);
            return adjustment;
        }
    }
    public static class CompanyAlertCodes
    {
        public static readonly Code InvalidSettings = new Code("work.invalid-company-alert"), NotCompany = new Code("work.alert-company-not-found");
    }
    /// <summary>Company response settings only; jobs, claims, readiness counts and rankings stay derived.</summary>
    internal sealed partial class CompanyAlertPolicies : ICompanyAlertPolicy, ICompanyWorkerReservePolicy, IPersistentService, ICompositionValidator
    {
        private readonly CompanyAlertSnapshot[] _companies;
        public World World { get; }
        public string Id => CompanyAlertModule.ModuleId;
        public int StateSchemaVersion => 3;
        internal CompanyAlertPolicies(World world, IReadOnlyDictionary<int, CompanyAlertSettings> initial)
        {
            World = world; _companies = new CompanyAlertSnapshot[world.Players.Count];
            for (int i = 0; i < _companies.Length; i++)
            {
                if (!initial.TryGetValue(i, out CompanyAlertSettings settings)) throw new ContentException("every selected company needs an explicit alert binding");
                _companies[i] = new CompanyAlertSnapshot(new PlayerId(i), settings, 0, 0);
            }
        }
        private bool ValidCompany(PlayerId company) => World.Players.IsSeat(company) && company.Value < _companies.Length;
        public CompanyAlertSnapshot ReadAlert(PlayerId company)
            => ValidCompany(company) ? _companies[company.Value] : throw new ArgumentException("unknown alert company");
        public Code CheckAlert(PlayerId company, CompanyAlertSettings settings)
        {
            if (!ValidCompany(company)) return CompanyAlertCodes.NotCompany;
            ICompanyAlertProfileDefinition profile = settings == null ? null : World.Content.Find<ICompanyAlertProfileDefinition>(settings.Profile);
            if (profile == null) return CompanyAlertCodes.InvalidSettings;
            foreach (WorkerReserveSetting reserve in settings.WorkerReserves)
                if (World.Content.Find<IWorkerRoleDefinition>(reserve.Role.Value)?.Role != reserve.Role) return CompanyAlertCodes.InvalidSettings;
            return EntityObservation.ReadProvider(World, profile, null, () => CheckProfile(profile));
        }
        private Code CheckProfile(ICompanyAlertProfileDefinition profile)
        {
            if (!World.Content.Owns(profile) || !ReferenceEquals(World.Content.Find<ICompanyAlertProfileDefinition>(profile.Id), profile)
                || profile.DispatchScopes == null) return CompanyAlertCodes.InvalidSettings;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string scope in profile.DispatchScopes)
            {
                try { ContractIdentity.ValidateId(scope, "alert dispatch scope"); }
                catch (ArgumentException) { return CompanyAlertCodes.InvalidSettings; }
                if (!seen.Add(scope)) return CompanyAlertCodes.InvalidSettings;
                bool found = scope == WorkModule.ModuleId;
                foreach (IWorkerDispatchSource source in World.Services.Providers<IWorkerDispatchSource>()) if (source.DispatchScope == scope) found = true;
                if (!found) return CompanyAlertCodes.InvalidSettings;
            }
            return Code.None;
        }
        public void ValidateComposition(World world)
        {
            if (!ReferenceEquals(world, World)) throw new ContentException("company alerts belong to another session");
            foreach (var profile in World.Content.All<ICompanyAlertProfileDefinition>())
                if (!EntityObservation.ReadProvider(World, profile, null, () => CheckProfile(profile)).IsNone)
                    throw new ContentException("invalid company alert profile identity/dispatch scopes: " + profile.Id);
            foreach (CompanyAlertSnapshot company in _companies)
                if (!CheckAlert(company.Company, company.Settings).IsNone) throw new ContentException("unknown profile/dispatch scope in initial company alert");
        }
        public void ConfigureAlert(PlayerId company, CompanyAlertSettings settings)
        {
            Code refusal = CheckAlert(company, settings); if (!refusal.IsNone) throw new InvalidOperationException("company alert refused: " + refusal);
            CompanyAlertSnapshot current = ReadAlert(company);
            if (current.Settings.Matches(settings)) return;
            _companies[company.Value] = new CompanyAlertSnapshot(company, settings, checked(current.Version + 1), World.Tick);
            World.Services.Get<WorkBoard>().Dirty = true; // Next shared pass, never mutation of acquired jobs/claims/cargo.
        }
        public long EffectivePriority(WorkerDispatchOffer offer)
        {
            CompanyAlertSnapshot company = ReadAlert(offer.Company);
            ICompanyAlertProfileDefinition profile = World.Content.Get<ICompanyAlertProfileDefinition>(company.Settings.Profile);
            return EntityObservation.ReadProvider(World, profile, null,
                () => checked((long)offer.Priority + profile.PriorityAdjustment(company.Settings.Level, offer)));
        }
        public void Sync(IStateSync sync)
        {
            int count = sync.Count(_companies.Length);
            if (count != _companies.Length || count != World.Players.Count) throw new CorruptStateException("company alert seat declaration differs");
            var restored = sync.Reading ? new CompanyAlertSnapshot[count] : null;
            for (int i = 0; i < count; i++)
            {
                CompanyAlertSnapshot current = _companies[i]; PlayerId company = current.Company; sync.Player(ref company);
                CompanyAlertSettings settings = CompanyAlertSettings.Sync(sync, current.Settings);
                int version = current.Version; long changed = current.ChangedTick; sync.Int(ref version); sync.Long(ref changed);
                if (company.Value != i || version < 0 || changed < 0 || changed > World.Tick || version == 0 && changed != 0 || !CheckAlert(company, settings).IsNone)
                    throw new CorruptStateException("invalid saved company alert policy");
                if (sync.Reading) restored[i] = new CompanyAlertSnapshot(company, settings, version, changed);
            }
            if (sync.Reading) Array.Copy(restored, _companies, count);
        }
    }
}
