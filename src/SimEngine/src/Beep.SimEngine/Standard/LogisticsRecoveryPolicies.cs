using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public enum LogisticsRecoveryKind : byte { Evacuation, CargoReturn }

    /// <summary>Immutable authored dispatch intent, never another queue or time owner.</summary>
    public sealed class LogisticsRecoverySettings
    {
        public bool Enabled { get; }
        public int Priority { get; }
        public int AttemptsPerPass { get; }
        public IReadOnlyList<Code> RerouteBlockers { get; }
        public string DestinationPolicy { get; }
        public LogisticsRecoverySettings(bool enabled, int priority, int attemptsPerPass, IEnumerable<Code> rerouteBlockers, string destinationPolicy)
        {
            ContractIdentity.ValidateId(destinationPolicy, "recovery destination policy"); DestinationPolicy = destinationPolicy;
            if (priority < 0 || attemptsPerPass < 1) throw new ArgumentException("nonnegative priority and positive attempts per pass required");
            Enabled = enabled; Priority = priority; AttemptsPerPass = attemptsPerPass;
            var blockers = new List<Code>();
            foreach (Code blocker in rerouteBlockers ?? throw new ArgumentNullException(nameof(rerouteBlockers)))
            {
                if (blocker.IsNone || string.IsNullOrWhiteSpace(blocker.Id) || blockers.Contains(blocker))
                    throw new ArgumentException("reroute blockers require unique explicit identities");
                blockers.Add(blocker);
            }
            RerouteBlockers = blockers.AsReadOnly();
        }
        public static LogisticsRecoverySettings Read(JsonReader reader)
        {
            if (reader == null) throw new ContentException("explicit recovery dispatch settings required");
            bool enabled = reader.Bool("enabled"); int priority = reader.Int("priority"), attempts = reader.Int("attempts_per_pass");
            if (priority < 0 || attempts < 1) throw reader.Error("nonnegative priority and positive attempts_per_pass required");
            var blockers = new List<Code>();
            JsonValue declared = reader.Value("reroute_blockers");
            if (declared == null || declared.Kind != JsonKind.Array) throw reader.Error("explicit reroute_blockers array required");
            for (int i = 0; i < declared.Count; i++)
            {
                if (declared[i].Kind != JsonKind.String) throw reader.Error("reroute blockers require string identities");
                string id = declared[i].Text;
                if (string.IsNullOrWhiteSpace(id)) throw reader.Error("reroute blockers require unique explicit identities");
                var blocker = new Code(id);
                if (blockers.Contains(blocker)) throw reader.Error("reroute blockers require unique explicit identities");
                blockers.Add(blocker);
            }
            string destination = reader.String("destination_policy");
            reader.End();
            try { return new LogisticsRecoverySettings(enabled, priority, attempts, blockers, destination); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
        }
    }
    public interface ILogisticsRecoveryPolicy : IContentDefinition
    {
        LogisticsRecoverySettings Read(LogisticsRecoveryKind kind);
    }
    public sealed class AuthoredLogisticsRecoveryProfileDef : Def, ILogisticsRecoveryPolicy
    {
        private LogisticsRecoveryProfileDeclaration _declaration;
        public LogisticsRecoverySettings Read(LogisticsRecoveryKind kind)
            => _declaration.ReadSettings(kind);
        protected override void ReadCore(JsonReader reader) => _declaration = LogisticsRecoveryProfileDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    /// <summary>One explicit two-kind JSON declaration, with no live recovery state.</summary>
    public sealed class LogisticsRecoveryProfileDeclaration
    {
        private readonly LogisticsRecoverySettings _evacuation, _cargoReturn;
        private LogisticsRecoveryProfileDeclaration(LogisticsRecoverySettings evacuation, LogisticsRecoverySettings cargoReturn)
        { _evacuation = evacuation; _cargoReturn = cargoReturn; }
        public static LogisticsRecoveryProfileDeclaration Read(JsonReader reader)
        {
            if (reader == null) throw new ContentException("explicit recovery profile required");
            return new LogisticsRecoveryProfileDeclaration(LogisticsRecoverySettings.Read(reader.Child("evacuation")),
                LogisticsRecoverySettings.Read(reader.Child("cargo_return")));
        }
        public LogisticsRecoverySettings ReadSettings(LogisticsRecoveryKind kind)
            => kind == LogisticsRecoveryKind.Evacuation ? _evacuation : kind == LogisticsRecoveryKind.CargoReturn
                ? _cargoReturn : throw new ArgumentOutOfRangeException(nameof(kind));
        public void Resolve(ContentResolver content) => Resolve(content, ReadSettings);
        public static void Resolve(ContentResolver content, Func<LogisticsRecoveryKind, LogisticsRecoverySettings> read)
        {
            if (read == null) throw new ArgumentNullException(nameof(read));
            foreach (LogisticsRecoveryKind kind in new[] { LogisticsRecoveryKind.Evacuation, LogisticsRecoveryKind.CargoReturn })
            {
                var settings = read(kind) ?? throw content.Fail("recovery profile must explicitly define both kinds");
                content.Def<IRecoveryDestinationPolicy>(settings.DestinationPolicy);
            }
        }
    }
    /// <summary>Selected immutable content observation; no policy/chronology/settings mirror.</summary>
    internal static class LogisticsRecoveryContracts
    {
        internal static LogisticsRecoverySettings Read(World world, ILogisticsRecoveryPolicy policy, LogisticsRecoveryKind kind)
        {
            if (policy == null) return null;
            if (!world.Content.Owns(policy) || !ReferenceEquals(world.Content.Find<ILogisticsRecoveryPolicy>(policy.Id), policy))
                throw new ContentException("recovery policy must alias selected immutable content");
            return EntityObservation.ReadProvider(world, policy, null, () =>
            {
                var settings = policy.Read(kind) ?? throw new ContentException("recovery policy must explicitly supply both dispatch kinds");
                if (world.Content.Find<IRecoveryDestinationPolicy>(settings.DestinationPolicy) == null)
                    throw new ContentException("unknown recovery destination policy '" + settings.DestinationPolicy + "'");
                return settings;
            });
        }
    }
    /// <summary>Optional policy; the existing LogisticsService owns both recovery kinds' chronology.</summary>
    public sealed class LogisticsRecoveryPolicyModule : GameModule
    {
        public const string ModuleId = "sim.logistics-recovery";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(LogisticsModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<AuthoredLogisticsRecoveryProfileDef>("logistics_recovery_profiles");
            types.Section<AuthoredRecoveryDestinationPolicyDef>("recovery_destination_policies");
        }
        public override void Compose(WorldComposer composer)
        {
            var settings = composer.Scenario.SettingsFor(ModuleId) ?? throw new ContentException("logistics recovery needs explicit profile settings");
            string id = settings.String("profile"); settings.End();
            foreach (var profile in composer.Content.All<ILogisticsRecoveryPolicy>())
                foreach (LogisticsRecoveryKind kind in new[] { LogisticsRecoveryKind.Evacuation, LogisticsRecoveryKind.CargoReturn })
                    LogisticsRecoveryContracts.Read(composer.World, profile, kind);
            composer.Services.Bind<ILogisticsRecoveryPolicy>(composer.Content.Get<ILogisticsRecoveryPolicy>(id));
        }
    }
}
