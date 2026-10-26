using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public enum IncidentProcessProgressPolicy : byte { Reset = 1, Retain = 2 }
    public interface IIncidentProcessProfile : IContentDependencyDefinition
    {
        Code Source { get; }
        IIncidentWorkflowDefinition Workflow { get; }
        IReadOnlyList<string> StopPhases { get; }
        GameDuration Interval { get; }
        IncidentProcessProgressPolicy IneligibleProgress { get; }
        IIncidentExposureEffect Effect { get; }
    }
    public interface IIncidentProcessDefinition : INamedCapabilityDefinition
    {
        IIncidentProcessProfile Profile { get; }
        void ValidateBinding(World world);
    }
    public interface IIncidentProcess : INamedEntityCapability
    {
        IIncidentProcessDefinition Definition { get; }
        IncidentProcessSnapshot ReadProcess(World world);
        void UpdateProcess(World world, TickInfo tick);
        void ValidateProcessCheckpoint(World world);
    }
    public readonly struct IncidentProcessSnapshot
    {
        public int Episode { get; }
        public long ElapsedTicks { get; }
        public long RequiredTicks { get; }
        public bool PhaseEligible { get; }
        public bool Due => ElapsedTicks == RequiredTicks && RequiredTicks > 0;
        public Code Blocker { get; }
        internal IncidentProcessSnapshot(int episode, long elapsed, long required, bool eligible, Code blocker)
        { Episode = episode; ElapsedTicks = elapsed; RequiredTicks = required; PhaseEligible = eligible; Blocker = blocker; }
    }
    public static class IncidentProcessCodes
    {
        public static readonly Code Inactive = new Code("incident.process.inactive");
        public static readonly Code PhaseComplete = new Code("incident.process.phase-complete");
        public static readonly Code Interval = new Code("incident.process.interval");
    }
    public sealed class IncidentProcessProfileDeclaration
    {
        private readonly string _workflow, _effect;
        public Code Source { get; }
        public IIncidentWorkflowDefinition Workflow { get; private set; }
        public IReadOnlyList<string> StopPhases { get; }
        public GameDuration Interval { get; }
        public IncidentProcessProgressPolicy IneligibleProgress { get; }
        public IIncidentExposureEffect Effect { get; private set; }
        private IncidentProcessProfileDeclaration(JsonReader reader)
        {
            string source = reader.String("source"); _workflow = reader.String("workflow"); _effect = reader.String("effect");
            try { foreach (string id in new[] { source, _workflow, _effect }) ContractIdentity.ValidateId(id, "incident process reference"); Source = new Code(source); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            Interval = reader.Child("interval")?.RationalDuration() ?? throw reader.Error("explicit process interval required");
            if (Interval.IsZero) throw reader.Error("incident process interval must be positive");
            if (reader.Array("until_phases_complete") == null) throw reader.Error("explicit until_phases_complete array required");
            string[] phases = reader.Strings("until_phases_complete"); Array.Sort(phases, StringComparer.Ordinal);
            if (phases.Length == 0) throw reader.Error("incident process requires at least one stop phase");
            for (int i = 0; i < phases.Length; i++)
                if (string.IsNullOrWhiteSpace(phases[i]) || i > 0 && phases[i] == phases[i - 1]) throw reader.Error("invalid/duplicate incident stop phase");
            StopPhases = Array.AsReadOnly(phases);
            string policy = reader.String("ineligible_progress");
            IneligibleProgress = policy == "reset" ? IncidentProcessProgressPolicy.Reset : policy == "retain" ? IncidentProcessProgressPolicy.Retain :
                throw reader.Error("ineligible_progress must be reset or retain");
        }
        public static IncidentProcessProfileDeclaration Read(JsonReader reader) => new IncidentProcessProfileDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content)
        {
            if (Workflow != null) throw new InvalidOperationException("incident process profile resolves once");
            Workflow = content.Def<IIncidentWorkflowDefinition>(_workflow); Effect = content.Def<IIncidentExposureEffect>(_effect);
        }
    }
    public sealed class IncidentProcessProfileDef : Def, IIncidentProcessProfile
    {
        private IncidentProcessProfileDeclaration _declaration;
        public Code Source => _declaration.Source;
        public IIncidentWorkflowDefinition Workflow => _declaration.Workflow;
        public IReadOnlyList<string> StopPhases => _declaration.StopPhases;
        public GameDuration Interval => _declaration.Interval;
        public IncidentProcessProgressPolicy IneligibleProgress => _declaration.IneligibleProgress;
        public IIncidentExposureEffect Effect => _declaration.Effect;
        public IReadOnlyList<IContentDefinition> ContentDependencies => Array.AsReadOnly(new IContentDefinition[] { Workflow, Effect });
        protected override void ReadCore(JsonReader reader) => _declaration = IncidentProcessProfileDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    public sealed class IncidentProcessBindingDeclaration
    {
        private readonly string _profile;
        public Code CapabilityId { get; }
        public IIncidentProcessProfile Profile { get; private set; }
        private IncidentProcessBindingDeclaration(JsonReader reader)
        {
            string id = reader.String("id"); _profile = reader.String("profile");
            try { ContractIdentity.ValidateId(id, "incident process capability"); ContractIdentity.ValidateId(_profile, "incident process profile"); CapabilityId = new Code(id); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
        }
        public static IncidentProcessBindingDeclaration Read(JsonReader reader) => new IncidentProcessBindingDeclaration(reader ?? throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content)
        {
            if (Profile != null) throw new InvalidOperationException("incident process binding resolves once");
            Profile = content.Def<IIncidentProcessProfile>(_profile);
        }
    }
    public static class IncidentProcessContracts
    {
        public static void ValidateProfile(World world, IIncidentProcessProfile profile)
        {
            if (profile is not Def actual || !actual.IsFrozen || !world.Content.Owns(profile) ||
                !ReferenceEquals(world.Content.Find<IIncidentProcessProfile>(profile.Id), profile) || profile.Source.IsNone ||
                profile.StopPhases == null || profile.StopPhases.Count == 0 || profile.Interval.IsZero ||
                profile.IneligibleProgress != IncidentProcessProgressPolicy.Reset && profile.IneligibleProgress != IncidentProcessProgressPolicy.Retain)
                throw new ContentException("incident process requires actual immutable profile metadata");
            IncidentBindingContracts.Workflow(world, profile.Workflow);
            var phases = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in profile.StopPhases)
                if (!phases.Add(id) || IncidentPhases.Find(profile.Workflow, id) == null) throw new ContentException(profile.Id + ": missing/duplicate authored stop phase");
            try { if (world.Time.ExactTicks(profile.Interval) <= 0) throw new ContentException(profile.Id + ": positive process interval required"); }
            catch (Exception error) when (error is ArgumentException || error is OverflowException) { throw new ContentException(profile.Id + ": interval does not fit the session quantum: " + error.Message); }
            if (profile.Effect is not Def effect || !effect.IsFrozen || !world.Content.Owns(profile.Effect) ||
                !ReferenceEquals(world.Content.Find<IIncidentExposureEffect>(effect.Id), profile.Effect))
                throw new ContentException(profile.Id + ": process requires its actual immutable exposure effect");
            EntityObservation.ReadProvider(world, profile.Effect, null, () => { profile.Effect.RequireCapabilities(world); return true; }, space: world.Space);
        }
        public static void ValidateDefinition(World world, IIncidentProcessDefinition definition, Entity entity = null)
        {
            var part = IncidentBindingContracts.Actual(world, definition, typeof(IIncidentProcess), entity);
            if (part.StateCodec.SchemaVersion != 1) throw new ContentException("incident process requires its original interval codec1");
            EntityObservation.ReadProvider(world, definition.Profile, entity, () => { ValidateProfile(world, definition.Profile); return true; }, space: world.Space);
            var source = part.Owner.Component<IPhasedIncidentDefinition>(definition.Profile.Source);
            if (source is not ComponentDef sourcePart || !typeof(IPhasedIncidentResponse).IsAssignableFrom(sourcePart.ComponentType) ||
                !ReferenceEquals(source.Workflow, definition.Profile.Workflow)) throw new ContentException(part.Owner.Id + ": process needs its exact named incident/workflow");
        }
        internal static void ValidateComposition(World world)
        {
            foreach (var profile in world.Content.All<IIncidentProcessProfile>())
                EntityObservation.ReadProvider(world, profile, null, () => { ValidateProfile(world, profile); return true; }, space: world.Space);
            foreach (var owner in world.Content.All<EntityDef>()) foreach (var part in owner.Components)
                if (part is IIncidentProcessDefinition definition)
                    EntityObservation.ReadProvider(world, part, null, () => { ValidateDefinition(world, definition); definition.ValidateBinding(world); return true; }, space: world.Space);
                else if (typeof(IIncidentProcess).IsAssignableFrom(part.ComponentType)) throw new ContentException(owner.Id + ": process runtime requires its metadata contract");
        }
        internal readonly struct Facts
        {
            internal readonly IIncidentProcessProfile Profile;
            internal readonly IncidentLifecycleSnapshot Incident;
            internal readonly long Required;
            internal readonly Code Blocker;
            internal Facts(IIncidentProcessProfile profile, IncidentLifecycleSnapshot incident, long required, Code blocker)
            { Profile = profile; Incident = incident; Required = required; Blocker = blocker; }
        }
        internal static Facts Observe(World world, IIncidentProcess process)
        {
            if (process?.Entity == null || !ReferenceEquals(world.Entities.Get(process.Entity.Id), process.Entity) ||
                !ReferenceEquals(process.Entity.Get<IIncidentProcess>(process.CapabilityId), process)) throw new InvalidOperationException("incident process must alias its actual session capability");
            ValidateDefinition(world, process.Definition, process.Entity);
            var profile = process.Definition.Profile; var response = process.Entity.Get<IPhasedIncidentResponse>(profile.Source);
            if (response == null || !ReferenceEquals(response.Workflow, profile.Workflow)) throw new InvalidOperationException("incident process source changed");
            var incident = IncidentPhases.ReadLifecycle(world, response, out var order); long required = world.Time.ExactTicks(profile.Interval);
            if (!incident.Active) return new Facts(profile, incident, required, IncidentProcessCodes.Inactive);
            bool complete = order != null;
            foreach (string id in profile.StopPhases) { var phase = IncidentPhases.Find(profile.Workflow, id); complete &= IncidentPhases.CompletedNodes(phase, order) == phase.Nodes.Count; }
            return new Facts(profile, incident, required, complete ? IncidentProcessCodes.PhaseComplete : Code.None);
        }
        internal static Code CheckEffect(World world, IIncidentProcess process, Facts facts)
            => EntityObservation.ReadProvider(world, facts.Profile.Effect, process.Entity,
                () => facts.Profile.Effect.CheckExposure(new IncidentPropagationContext(world, process.Entity, facts.Incident), process.Entity),
                space: world.Space, observeTargetState: true);
        internal static IncidentExposurePreparation Prepare(World world, IIncidentProcess process, Facts facts)
            => EntityObservation.ReadProvider(world, facts.Profile.Effect, process.Entity,
                () => facts.Profile.Effect.PrepareExposure(new IncidentPropagationContext(world, process.Entity, facts.Incident), process.Entity),
                space: world.Space, observeTargetState: true);
    }
}
