using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    public static class PressureCodes
    {
        public static readonly Code NoTarget = new Code("pressure.no-target");
        public static readonly Code Budget = new Code("pressure.budget");
        public static readonly Code Awaiting = new Code("pressure.awaiting-command");
        public static readonly Code Cooldown = new Code("pressure.cooldown");
    }
    /// <summary>Private observation only: neither a mutable world nor a submission/execution handle.</summary>
    public readonly struct PressureContext
    {
        public PlayerId Company { get; }
        public PlayerView View { get; }
        public long Tick { get; }
        public ISimulationTime Time { get; }
        internal PressureContext(World world, PlayerId company)
        { Company = company; View = world.ViewFor(company); Tick = world.Tick; Time = world.Time; }
    }
    public readonly struct PressureObservation
    {
        private PressureObservation(Command command, Code blocker) { Command = command; Blocker = blocker; }
        public Command Command { get; }
        public Code Blocker { get; }
        public static PressureObservation Propose(Command command)
            => new PressureObservation(command ?? throw new ArgumentNullException(nameof(command)), Code.None);
        public static PressureObservation Wait(Code blocker)
        {
            if (blocker.IsNone) throw new ArgumentException("waiting pressure needs an explicit blocker", nameof(blocker));
            return new PressureObservation(null, blocker);
        }
    }
    public interface IPressureSource : IContentDefinition
    {
        Fix Cost { get; }
        int Priority { get; }
        void ValidateBinding(World world, PlayerId company);
        PressureObservation Evaluate(PressureContext context);
    }
    /// <summary>One common authored cost/priority contract; public sources add semantics, not scheduler branches.</summary>
    public abstract class PressureSourceDef : Def, IPressureSource
    {
        private PressureSourceDeclaration _declaration;
        public Fix Cost => _declaration.Cost;
        public int Priority => _declaration.Priority;
        protected sealed override void ReadCore(JsonReader reader)
        {
            _declaration = PressureSourceDeclaration.Read(reader);
            ReadSource(reader);
        }
        protected abstract void ReadSource(JsonReader reader);
        public abstract void ValidateBinding(World world, PlayerId company);
        public abstract PressureObservation Evaluate(PressureContext context);
    }
    public sealed class PressureCandidateSnapshot
    {
        public EntityId Entity { get; }
        public Code Blocker { get; }
        public bool Ready => Blocker.IsNone;
        public IReadOnlyList<ExecutionGateConditionSnapshot> Conditions { get; }
        public PressureCandidateSnapshot(EntityId entity, IEnumerable<ExecutionGateConditionSnapshot> conditions)
        {
            if (!entity.IsValid) throw new ArgumentException("pressure candidate requires actual entity identity", nameof(entity));
            if (conditions == null) throw new ArgumentNullException(nameof(conditions));
            Entity = entity; var facts = new List<ExecutionGateConditionSnapshot>(conditions);
            facts.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id)); string previous = null;
            Code blocker = Code.None;
            foreach (var fact in facts)
            {
                ContractIdentity.ValidateId(fact.Id, "pressure condition");
                if (fact.Source.IsNone || previous == fact.Id || fact.Ready != fact.Blocker.IsNone || fact.Ready && !fact.Signal.Available)
                    throw new ArgumentException("malformed pressure candidate conditions", nameof(conditions));
                if (blocker.IsNone && !fact.Ready) blocker = fact.Blocker;
                previous = fact.Id;
            }
            Conditions = facts.AsReadOnly(); Blocker = blocker;
        }
    }
    /// <summary>Optional pure detached candidates; never a command, provider or retained admission handle.</summary>
    public interface IPressureSourceCandidates
    {
        void CopyCandidates(PressureContext context, IList<PressureCandidateSnapshot> destination);
    }
    public sealed class IncidentPressureSourceDef : PressureSourceDef, IPressureSourceCandidates
    {
        private string _target;
        private EntityDef _definition;
        private Code _source, _kind;
        public IReadOnlyList<SignalGateCondition> Conditions { get; private set; }
        protected override void ReadSource(JsonReader reader)
        {
            _target = reader.String("entity"); _source = new Code(reader.String("source")); _kind = new Code(reader.String("kind"));
            try { ContractIdentity.ValidateId(_source.Id, "incident source"); ContractIdentity.ValidateId(_kind.Id, "incident kind"); }
            catch (ArgumentException error) { throw reader.Error(error.Message); }
            Conditions = SignalGateCondition.ReadAll(reader, allowEmpty: true);
        }
        protected override void ResolveCore(ContentResolver content)
        {
            _definition = content.Def<EntityDef>(_target);
            foreach (var condition in Conditions) condition.Resolve(content);
        }
        public override void ValidateBinding(World world, PlayerId company)
        {
            if (!world.Content.Owns(_definition) || !_definition.HasCapability<IIncident>(_source) ||
                !world.Commands.TryGet(typeof(RaiseIncidentCommand), out _) || !world.Services.Has<WorkBoard>())
                throw new ContentException(Id + ": incident pressure needs actual incident capability/module/command");
            IIncidentDefinition definition = _definition.Component<IIncidentDefinition>(_source);
            if (definition == null || !definition.Supports(_kind)) throw new ContentException(Id + ": missing named incident definition or unsupported authored kind");
            if (Conditions.Count > 0 && world.ViewFor(company).Feature<OperationsView>() == null)
                throw new ContentException(Id + ": conditioned pressure needs selected operation signal views");
            foreach (var condition in Conditions) condition.Validate(_definition);
        }
        public override PressureObservation Evaluate(PressureContext context)
        {
            var candidates = new List<PressureCandidateSnapshot>(); CopyCandidates(context, candidates);
            Code blocker = Code.None;
            foreach (var candidate in candidates)
            {
                if (candidate.Ready) return PressureObservation.Propose(new RaiseIncidentCommand { Target = candidate.Entity, Source = _source, Kind = _kind });
                if (blocker.IsNone) blocker = candidate.Blocker;
            }
            return PressureObservation.Wait(blocker.IsNone ? PressureCodes.NoTarget : blocker);
        }
        public void CopyCandidates(PressureContext context, IList<PressureCandidateSnapshot> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination)); destination.Clear();
            var entities = new List<EntityView>(); context.View.CopyEntities(context.Company, entities);
            entities.Sort((a, b) => a.Id.CompareTo(b.Id));
            IncidentsView incidents = context.View.Feature<IncidentsView>();
            OperationsView operations = Conditions.Count == 0 ? null : context.View.Feature<OperationsView>();
            foreach (EntityView entity in entities)
                if (entity.Def == _definition.Id && incidents.TryGetIncident(entity.Id, _source, out IncidentSnapshot snapshot) && !snapshot.Active)
                {
                    destination.Add(new PressureCandidateSnapshot(entity.Id,
                        Conditions.Count == 0 ? Array.Empty<ExecutionGateConditionSnapshot>() : SignalGateCondition.ReadFromView(operations, entity.Id, Conditions)));
                }
        }
    }
    public sealed class ScenarioPressureProfileDef : Def, IScenarioPressureProfile
    {
        private ScenarioPressureDeclaration _declaration;
        public Fix Capacity => _declaration.Capacity;
        public Fix Initial => _declaration.Initial;
        public Fix Refill => _declaration.Refill;
        public GameDuration RefillPeriod => _declaration.RefillPeriod;
        public GameDuration Cadence => _declaration.Cadence;
        public GameDuration Cooldown => _declaration.Cooldown;
        public IReadOnlyList<IPressureSource> Sources { get; private set; }
        protected override void ReadCore(JsonReader reader) => _declaration = ScenarioPressureDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => Sources = _declaration.Resolve(content);
    }
    public readonly struct ScenarioPressureSnapshot
    {
        public PlayerId Company { get; }
        public string Profile { get; }
        public Fix Available { get; }
        public Fix Reserved { get; }
        public Fix Spent { get; }
        public long Decisions { get; }
        public long Accepted { get; }
        public long Refused { get; }
        public long LastDecision { get; }
        public long NextDecision { get; }
        public long LastRefill { get; }
        public long CooldownUntil { get; }
        public int PendingSequence { get; }
        public string PendingSource { get; }
        public Code Blocker { get; }
        internal ScenarioPressureSnapshot(PlayerId company, string profile, Fix available, Fix reserved, Fix spent,
            long decisions, long accepted, long refused, long last, long next, long refill, long cooldown, int pending, string source, Code blocker)
        { Company = company; Profile = profile; Available = available; Reserved = reserved; Spent = spent;
            Decisions = decisions; Accepted = accepted; Refused = refused; LastDecision = last; NextDecision = next;
            LastRefill = refill; CooldownUntil = cooldown; PendingSequence = pending; PendingSource = source; Blocker = blocker; }
    }
    public interface IScenarioPressure
    {
        void CopyPressure(PlayerId viewer, IList<ScenarioPressureSnapshot> destination);
        bool CopyCandidates(PlayerId viewer, PlayerId company, string source, IList<PressureCandidateSnapshot> destination);
    }
}
