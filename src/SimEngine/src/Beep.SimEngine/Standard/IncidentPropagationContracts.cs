using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Standard
{
    public readonly struct IncidentExposureObservation
    {
        public bool Eligible { get; }
        public Code Blocker { get; }
        public IncidentExposureObservation(bool eligible, Code blocker)
        {
            if (eligible != blocker.IsNone) throw new ArgumentException("exposure requires exact eligibility/blocker facts");
            Eligible = eligible; Blocker = blocker;
        }
        public static IncidentExposureObservation Ready => new IncidentExposureObservation(true, Code.None);
        public static IncidentExposureObservation Refused(Code blocker) => new IncidentExposureObservation(false, blocker);
    }
    public readonly struct IncidentPropagationContext
    {
        public World World { get; }
        public Entity Source { get; }
        public IncidentLifecycleSnapshot Incident { get; }
        internal IncidentPropagationContext(World world, Entity source, IncidentLifecycleSnapshot incident)
        { World = world; Source = source; Incident = incident; }
    }
    public interface IIncidentPropagationRule : IContentDefinition
    {
        GameDuration Interval { get; }
        GameDuration Delay { get; }
        Code TargetSource { get; }
        Code TargetKind { get; }
        bool CrossCompany { get; }
        void ValidateBinding(World world);
        void ValidateSource(EntityDef source);
        IncidentExposureObservation Observe(IncidentPropagationContext context, Entity target);
    }
    /// <summary>Optional authored change at successful delayed exposure, not another propagation system.</summary>
    public interface IIncidentExposureEffectRule : IIncidentPropagationRule
    {
        IIncidentExposureEffect ExposureEffect { get; }
    }
    public interface IIncidentExposureEffect : IContentDependencyDefinition
    {
        void RequireCapabilities(World world);
        Code CheckExposure(IncidentPropagationContext context, Entity target);
        IncidentExposurePreparation PrepareExposure(IncidentPropagationContext context, Entity target);
    }
    /// <summary>Transient, single-use prepared publication; never saved or a second incident/terrain ledger.</summary>
    public sealed class IncidentExposurePreparation
    {
        private Action _commit;
        private bool _used;
        public Code Blocker { get; }
        public bool Ready => Blocker.IsNone;
        public IncidentExposurePreparation(Code blocker)
        { if(blocker.IsNone)throw new ArgumentException("refused exposure preparation requires a blocker");Blocker=blocker; }
        public IncidentExposurePreparation(Action commit)
        { _commit=commit??throw new ArgumentNullException(nameof(commit)); }
        internal void Commit()
        {
            if(!Ready||_used)throw new InvalidOperationException("exposure publication requires its unused successful preparation");
            _used=true;var commit=_commit;_commit=null;commit();
        }
    }
    public interface IIncidentPropagationDefinition : INamedCapabilityDefinition
    {
        Code Source { get; }
        IIncidentPropagationRule Rule { get; }
        void ValidateBinding(World world);
    }
    public interface IIncidentPropagationEmitter : INamedEntityCapability
    {
        Code Source { get; }
        IIncidentPropagationRule Rule { get; }
    }
    public static class IncidentPropagationCodes
    {
        public static readonly Code ReplacementBinding = new Code("incident.propagation.replacement-binding");
        public static readonly Code Inactive = new Code("incident.propagation.inactive");
        public static readonly Code SourceEnded = new Code("incident.propagation.source-ended");
        public static readonly Code TargetGone = new Code("incident.propagation.target-gone");
        public static readonly Code TargetChanged = new Code("incident.propagation.target-changed");
        public static readonly Code TargetActive = new Code("incident.propagation.target-active");
        public static readonly Code Company = new Code("incident.propagation.company");
        public static readonly Code Presence = new Code("incident.propagation.presence");
        public static readonly Code Distance = new Code("incident.propagation.distance");
        public static readonly Code SourceTerrain = new Code("incident.propagation.source-terrain");
        public static readonly Code TargetTerrain = new Code("incident.propagation.target-terrain");
    }
    /// <summary>Optional tile eligibility over the shared explicit policy; not a required mod base.</summary>
    public sealed class TileIncidentPropagationRuleDef : Def, IIncidentExposureEffectRule, IContentDependencyDefinition
    {
        private TileIncidentPropagationDeclaration _declaration;
        public ITileSpaceDefinition Space => _declaration.Space;
        public long Radius => _declaration.Radius;
        public GameDuration Interval => _declaration.Interval;
        public GameDuration Delay => _declaration.Delay;
        public Code TargetSource => _declaration.TargetSource;
        public Code TargetKind => _declaration.TargetKind;
        public bool CrossCompany => _declaration.CrossCompany;
        public IReadOnlyList<SignalGateCondition> SourceConditions => _declaration.SourceConditions;
        public IReadOnlyList<SignalGateCondition> TargetConditions => _declaration.TargetConditions;
        public TileFootprintConditionDeclaration SourceTiles => _declaration.SourceTiles;
        public TileFootprintConditionDeclaration TargetTiles => _declaration.TargetTiles;
        public IIncidentExposureEffect ExposureEffect => _declaration.ExposureEffect;
        public IReadOnlyList<IContentDefinition> ContentDependencies => ExposureEffect==null ? Array.Empty<IContentDefinition>() : Array.AsReadOnly(new IContentDefinition[]{ExposureEffect});
        protected override void ReadCore(JsonReader reader) => _declaration = TileIncidentPropagationDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateResolvedCore(ContentResolver content) => _declaration.Validate(content);
        public void ValidateSource(EntityDef source) => _declaration.ValidateSource(source);
        public void ValidateBinding(World world) => _declaration.ValidateBinding(world, Id);
        public IncidentExposureObservation Observe(IncidentPropagationContext context, Entity target)
            => _declaration.Observe(context, target, Id);
    }
    public sealed class IncidentPropagationEmitterDef : ComponentDef<IncidentPropagationEmitter>, IIncidentPropagationDefinition
    {
        private IncidentPropagationBindingDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        public Code Source => _declaration.Source;
        public IIncidentPropagationRule Rule => _declaration.Rule;
        protected override void ReadCore(JsonReader reader) => _declaration = IncidentPropagationBindingDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateCore(EntityDef owner) => _declaration.ValidateOwner(owner);
        public void ValidateBinding(World world) => IncidentPropagationContracts.ValidateBinding(world, this);
    }
    public sealed class IncidentPropagationEmitter : Component, IIncidentPropagationEmitter
    {
        public new IIncidentPropagationDefinition Def => (IIncidentPropagationDefinition)base.Def;
        public Code CapabilityId => Def.CapabilityId;
        public Code Source => Def.Source;
        public IIncidentPropagationRule Rule => Def.Rule;
    }
    public readonly struct PendingExposureSnapshot
    {
        public EntityId Target { get; }
        public int ExpectedEpisode { get; }
        public long ScheduledTick { get; }
        public long DueTick { get; }
        internal PendingExposureSnapshot(EntityId target, int episode, long scheduled, long due)
        { Target = target; ExpectedEpisode = episode; ScheduledTick = scheduled; DueTick = due; }
    }
    public sealed class IncidentPropagationSnapshot
    {
        public EntityId Entity { get; }
        public Code Capability { get; }
        public string Rule { get; }
        public int ObservedEpisode { get; }
        public long NextCycleTick { get; }
        public bool Scanning { get; }
        public EntityId ScanAfter { get; }
        public EntityId ScanThrough { get; }
        public Code Blocker { get; }
        public IReadOnlyList<PendingExposureSnapshot> Pending { get; }
        internal IncidentPropagationSnapshot(EntityId entity, Code capability, string rule, int episode, long next,
            bool scanning, EntityId after, EntityId through, Code blocker, IEnumerable<PendingExposureSnapshot> pending)
        { Entity = entity; Capability = capability; Rule = rule; ObservedEpisode = episode; NextCycleTick = next;
            Scanning = scanning; ScanAfter = after; ScanThrough = through; Blocker = blocker;
            Pending = new List<PendingExposureSnapshot>(pending).AsReadOnly(); }
    }
    /// <summary>Detached current eligibility, not a scheduled exposure or an arrival guarantee.</summary>
    public sealed class IncidentEligibilitySnapshot
    {
        public EntityId Source { get; }
        public Code Capability { get; }
        public EntityId Target { get; }
        public string Rule { get; }
        public int SourceEpisode { get; }
        public long ObservedUpdate { get; }
        public long AuthoredDelaySteps { get; }
        public IncidentExposureObservation Observation { get; }
        internal IncidentEligibilitySnapshot(EntityId source, Code capability, EntityId target, string rule,
            int episode, long observed, long delay, IncidentExposureObservation observation)
        { Source = source; Capability = capability; Target = target; Rule = rule; SourceEpisode = episode;
            ObservedUpdate = observed; AuthoredDelaySteps = delay; Observation = observation; }
    }

    public interface IIncidentPropagationState
    {
        Code ReadEligibility(EntityId source, Code capability, EntityId target, PlayerId viewer, out IncidentEligibilitySnapshot snapshot);
        bool TryRead(EntityId entity, Code capability, PlayerId viewer, out IncidentPropagationSnapshot snapshot);
    }
}
