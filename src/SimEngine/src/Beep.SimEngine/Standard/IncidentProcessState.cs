using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    /// <summary>One embedded local interval, not the incident, clock or material owner.</summary>
    public sealed class IncidentProcessState
    {
        private readonly IIncidentProcess _owner;
        private int _episode;
        private long _elapsed;
        private long _visited = -1; // Transient duplicate-visit permission coordinate, not another game clock.
        private bool _updating;
        private bool _observing;
        public IncidentProcessState(IIncidentProcess owner) { _owner = owner ?? throw new ArgumentNullException(nameof(owner)); }
        public IncidentProcessSnapshot Read(World world)
        {
            if (_observing) { var error = new InvalidOperationException("incident process observation cannot reenter"); world.LatchFault("incident.process", error); throw error; }
            _observing = true;
            try
            {
                var facts = IncidentProcessContracts.Observe(world, _owner); long elapsed = _episode == facts.Incident.Episode ? _elapsed : 0;
                if (elapsed < 0 || elapsed > facts.Required) throw new CorruptStateException("invalid observed incident interval");
                Code blocker = facts.Blocker;
                if (blocker.IsNone) blocker = elapsed < facts.Required ? IncidentProcessCodes.Interval : IncidentProcessContracts.CheckEffect(world, _owner, facts);
                return new IncidentProcessSnapshot(facts.Incident.Episode, elapsed, facts.Required, facts.Blocker.IsNone, blocker);
            }
            finally { _observing = false; }
        }
        public void Update(World world, TickInfo tick)
        {
            world.RequireHealthyUpdate();
            if (!world.Stepping || _updating || _visited == tick.Tick || tick.Tick != world.Clock.StepIndex || tick.Span != 1 || !ReferenceEquals(tick.Time, world.Time))
            {
                var error = new InvalidOperationException("incident process requires one original incident-system visit per game update");
                if (world.Stepping || _updating || _observing) world.LatchFault("incident.process", error);
                throw error;
            }
            _updating = true; _visited = tick.Tick;
            try
            {
                var facts = IncidentProcessContracts.Observe(world, _owner);
                if (_episode != facts.Incident.Episode) { _episode = facts.Incident.Episode; _elapsed = 0; _owner.Entity.Touch(); }
                if (!facts.Blocker.IsNone)
                {
                    if ((!facts.Incident.Active || facts.Profile.IneligibleProgress == IncidentProcessProgressPolicy.Reset) && _elapsed != 0)
                    { _elapsed = 0; _owner.Entity.Touch(); }
                    return;
                }
                if (_elapsed < 0 || _elapsed > facts.Required) throw new CorruptStateException("incident process interval exceeds its authored duration");
                if (_elapsed < facts.Required) { _elapsed = checked(_elapsed + 1); _owner.Entity.Touch(); }
                if (_elapsed != facts.Required || !IncidentProcessContracts.CheckEffect(world, _owner, facts).IsNone) return;
                var preparation = IncidentProcessContracts.Prepare(world, _owner, facts) ?? throw new InvalidOperationException("incident effect returned no preparation");
                if (!preparation.Ready) return;
                preparation.Commit(); _elapsed = 0; _owner.Entity.Touch();
            }
            catch (Exception error) { world.LatchFault("incident.process", error); throw; }
            finally { _updating = false; }
        }
        public void ValidateLoaded(World world)
        {
            var facts = IncidentProcessContracts.Observe(world, _owner);
            if (_episode < 0 || _episode > facts.Incident.Episode || _elapsed < 0 || _elapsed > facts.Required || _episode == 0 && _elapsed != 0)
                throw new CorruptStateException("invalid incident process episode provenance/interval");
        }
        public void Sync(IStateSync sync)
        {
            int episode = _episode; long elapsed = _elapsed; sync.Int(ref episode); sync.Long(ref elapsed);
            if (episode < 0 || elapsed < 0 || episode == 0 && elapsed != 0) throw new CorruptStateException("invalid incident process interval/provenance");
            if (sync.Reading) { if (_updating) throw new CorruptStateException("cannot restore an active incident process"); _episode = episode; _elapsed = elapsed; _visited = -1; }
        }
    }
    public sealed class IncidentProcessDef : ComponentDef<IncidentProcess>, IIncidentProcessDefinition
    {
        private IncidentProcessBindingDeclaration _declaration;
        public Code CapabilityId => _declaration.CapabilityId;
        public IIncidentProcessProfile Profile => _declaration.Profile;
        protected override void ReadCore(JsonReader reader) => _declaration = IncidentProcessBindingDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        public void ValidateBinding(World world) => IncidentProcessContracts.ValidateDefinition(world, this);
    }
    public sealed class IncidentProcess : Component, IIncidentProcess
    {
        private readonly IncidentProcessState _state;
        public IncidentProcess() { _state = new IncidentProcessState(this); }
        public IIncidentProcessDefinition Definition => (IIncidentProcessDefinition)Def;
        public Code CapabilityId => Definition.CapabilityId;
        public IncidentProcessSnapshot ReadProcess(World world) => _state.Read(world);
        public void UpdateProcess(World world, TickInfo tick) => _state.Update(world, tick);
        public void ValidateProcessCheckpoint(World world) => _state.ValidateLoaded(world);
        public override void Sync(IStateSync sync) => _state.Sync(sync);
    }
    public sealed class IncidentProcessModule : GameModule
    {
        public const string ModuleId = "sim.incident-processes";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override System.Collections.Generic.IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(IncidentResponseModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        { types.Section<IncidentProcessProfileDef>("incident_process_profiles"); types.Component<IncidentProcessDef>("incident_process", 1); }
    }
}
