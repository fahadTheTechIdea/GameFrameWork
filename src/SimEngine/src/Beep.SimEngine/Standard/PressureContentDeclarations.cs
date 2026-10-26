using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface IScenarioPressureProfile : IContentDefinition
    {
        Fix Capacity { get; }
        Fix Initial { get; }
        Fix Refill { get; }
        GameDuration RefillPeriod { get; }
        GameDuration Cadence { get; }
        GameDuration Cooldown { get; }
        IReadOnlyList<IPressureSource> Sources { get; }
    }
    public sealed class PressureSourceDeclaration
    {
        public Fix Cost { get; }
        public int Priority { get; }
        private PressureSourceDeclaration(Fix cost, int priority) { Cost = cost; Priority = priority; }
        public static PressureSourceDeclaration Read(JsonReader reader)
        {
            if (reader == null) throw new ContentException("explicit pressure source required");
            Fix cost = reader.Fix("cost"); int priority = reader.Int("priority");
            if (cost <= Fix.Zero) throw reader.Error("pressure cost must be positive");
            return new PressureSourceDeclaration(cost, priority);
        }
    }
    /// <summary>One explicit JSON reader and reference resolver, shared by all profile implementations.</summary>
    public sealed class ScenarioPressureDeclaration
    {
        private readonly string[] _sourceIds;
        public Fix Capacity { get; }
        public Fix Initial { get; }
        public Fix Refill { get; }
        public GameDuration RefillPeriod { get; }
        public GameDuration Cadence { get; }
        public GameDuration Cooldown { get; }
        private ScenarioPressureDeclaration(Fix capacity, Fix initial, Fix refill,
            GameDuration period, GameDuration cadence, GameDuration cooldown, string[] sources)
        { Capacity = capacity; Initial = initial; Refill = refill; RefillPeriod = period;
            Cadence = cadence; Cooldown = cooldown; _sourceIds = sources; }
        public static ScenarioPressureDeclaration Read(JsonReader reader)
        {
            if (reader == null) throw new ContentException("explicit pressure profile required");
            Fix capacity = reader.Fix("capacity"), initial = reader.Fix("initial"), refill = reader.Fix("refill");
            GameDuration period = (reader.Child("refill_period") ?? throw reader.Error("explicit refill period required")).RationalDuration();
            GameDuration cadence = (reader.Child("cadence") ?? throw reader.Error("explicit cadence required")).RationalDuration();
            GameDuration cooldown = (reader.Child("cooldown") ?? throw reader.Error("explicit cooldown required")).RationalDuration();
            if (capacity <= Fix.Zero || initial < Fix.Zero || initial > capacity || refill < Fix.Zero || period.IsZero || cadence.IsZero)
                throw reader.Error("invalid pressure points/timing");
            if (reader.Array("sources") == null) throw reader.Error("explicit pressure sources required");
            string[] sources = reader.Strings("sources");
            if (sources.Length == 0) throw reader.Error("pressure profile needs a source");
            Array.Sort(sources, StringComparer.Ordinal);
            for (int i = 0; i < sources.Length; i++)
            {
                try { ContractIdentity.ValidateId(sources[i], "pressure source"); }
                catch (ArgumentException error) { throw reader.Error(error.Message); }
                if (i > 0 && sources[i] == sources[i - 1]) throw reader.Error("duplicate pressure source");
            }
            return new ScenarioPressureDeclaration(capacity, initial, refill, period, cadence, cooldown, sources);
        }
        public IReadOnlyList<IPressureSource> Resolve(ContentResolver content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            var sources = new List<IPressureSource>();
            foreach (string id in _sourceIds) sources.Add(content.Def<IPressureSource>(id));
            return sources.AsReadOnly();
        }
    }
    internal readonly struct PressureSourceSettings
    {
        internal readonly IPressureSource Definition;
        internal readonly Fix Cost;
        internal readonly int Priority;
        internal PressureSourceSettings(IPressureSource definition, Fix cost, int priority)
        { Definition = definition; Cost = cost; Priority = priority; }
    }
    /// <summary>Detached, validated binding configuration, not another pacing ledger or definition registry.</summary>
    internal sealed class PressureProfileSettings
    {
        internal readonly IScenarioPressureProfile Definition;
        internal readonly Fix Capacity, Initial, Refill;
        internal readonly long Period, Cadence, Cooldown;
        internal readonly IReadOnlyList<PressureSourceSettings> Sources;
        internal PressureProfileSettings(IScenarioPressureProfile definition, Fix capacity, Fix initial, Fix refill,
            long period, long cadence, long cooldown, List<PressureSourceSettings> sources)
        { Definition = definition; Capacity = capacity; Initial = initial; Refill = refill;
            Period = period; Cadence = cadence; Cooldown = cooldown; Sources = sources.AsReadOnly(); }
    }
    internal static class PressureContentContracts
    {
        internal static PressureSourceSettings Read(World world, IPressureSource source)
        {
            if (source == null || !world.Content.Owns(source) || !ReferenceEquals(world.Content.Find<IPressureSource>(source.Id), source))
                throw new ContentException("pressure source must alias actual content");
            return EntityObservation.ReadProvider(world, source, null, () =>
            {
                Fix cost = source.Cost; int priority = source.Priority;
                if (cost <= Fix.Zero) throw new ContentException(source.Id + ": pressure cost must be positive");
                return new PressureSourceSettings(source, cost, priority);
            });
        }
        internal static PressureProfileSettings Read(World world, IScenarioPressureProfile profile)
        {
            if (profile == null || !world.Content.Owns(profile) || !ReferenceEquals(world.Content.Find<IScenarioPressureProfile>(profile.Id), profile))
                throw new ContentException("pressure profile must alias actual content");
            return EntityObservation.ReadProvider(world, profile, null, () =>
            {
                Fix capacity = profile.Capacity, initial = profile.Initial, refill = profile.Refill;
                GameDuration period = profile.RefillPeriod, cadence = profile.Cadence, cooldown = profile.Cooldown;
                var declared = profile.Sources;
                if (capacity <= Fix.Zero || initial < Fix.Zero || initial > capacity || refill < Fix.Zero || declared == null || declared.Count == 0)
                    throw new ContentException(profile.Id + ": invalid pressure points/sources");
                long periodSteps, cadenceSteps, cooldownSteps;
                try
                {
                    periodSteps = world.Time.Ticks(period); cadenceSteps = world.Time.Ticks(cadence); cooldownSteps = world.Time.Ticks(cooldown);
                    if (periodSteps <= 0 || cadenceSteps <= 0 || cooldownSteps < 0) throw new ArgumentException("invalid pressure timing");
                }
                catch (Exception error) when (error is ArgumentException || error is OverflowException)
                { throw new ContentException(profile.Id + ": pressure duration cannot resolve against selected game time: " + error.Message); }
                var sources = new List<PressureSourceSettings>(); var identities = new HashSet<string>(StringComparer.Ordinal);
                foreach (IPressureSource source in declared)
                {
                    PressureSourceSettings settings = Read(world, source);
                    if (!identities.Add(source.Id) || settings.Cost > capacity)
                        throw new ContentException(profile.Id + ": duplicate source or source exceeds profile capacity");
                    sources.Add(settings);
                }
                sources.Sort((a, b) => { int order = b.Priority.CompareTo(a.Priority);
                    return order != 0 ? order : string.CompareOrdinal(a.Definition.Id, b.Definition.Id); });
                return new PressureProfileSettings(profile, capacity, initial, refill, periodSteps, cadenceSteps, cooldownSteps, sources);
            });
        }
    }
}
