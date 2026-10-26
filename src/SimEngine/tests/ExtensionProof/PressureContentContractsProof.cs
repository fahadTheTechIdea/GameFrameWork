using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentPressureProfileDef : Def, IScenarioPressureProfile
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
    /// <summary>Immutable authored negative callback proof, not a pacing strategy or alternate simulator.</summary>
    public sealed class PressureCallbackProbeDef : Def, IPressureSource, IPressureSourceCandidates
    {
        private PressureSourceDeclaration _declaration;
        private string _phase, _fault, _target;
        private EntityDef _definition;
        public Fix Cost => _declaration.Cost;
        public int Priority => _declaration.Priority;
        public string Marker => "proof.pressure.callback." + _phase + "." + _fault;
        protected override void ReadCore(JsonReader reader)
        {
            _declaration = PressureSourceDeclaration.Read(reader);
            _phase = reader.String("phase"); _fault = reader.String("fault"); _target = reader.String("entity");
            if (_phase == "validate" ? _fault != "throw" && _fault != "pause-throw" && _fault != "pause"
                : _phase == "evaluate" ? _fault != "throw"
                : _phase == "candidates" ? _fault != "throw" && _fault != "prefix-throw" && _fault != "null" && _fault != "duplicate" && _fault != "reverse"
                : true) throw reader.Error("explicit known callback phase/fault required");
        }
        protected override void ResolveCore(ContentResolver content) => _definition = content.Def<EntityDef>(_target);
        public void ValidateBinding(World world, PlayerId company)
        {
            if (!world.Content.Owns(_definition)) throw new ContentException("callback probe requires actual target metadata");
            if (_phase != "validate") return;
            if (_fault == "pause" || _fault == "pause-throw") ((GameClock)world.Clock).Pause();
            if (_fault != "pause") throw new InvalidOperationException(Marker);
        }
        public PressureObservation Evaluate(PressureContext context)
        {
            if (_phase == "evaluate") throw new InvalidOperationException(Marker);
            return PressureObservation.Wait(PressureCodes.NoTarget);
        }
        public void CopyCandidates(PressureContext context, IList<PressureCandidateSnapshot> destination)
        {
            if (_phase != "candidates") return;
            if (_fault == "throw") throw new InvalidOperationException(Marker);
            var entities = new List<EntityView>(); context.View.CopyEntities(context.Company, entities);
            entities.Sort((a, b) => a.Id.CompareTo(b.Id)); var facts = new List<PressureCandidateSnapshot>();
            foreach (var entity in entities)
                if (entity.Def == _definition.Id) facts.Add(new PressureCandidateSnapshot(entity.Id, Array.Empty<ExecutionGateConditionSnapshot>()));
            if (facts.Count < 2) throw new InvalidOperationException("callback proof requires two actual private targets");
            if (_fault == "reverse") { destination.Add(facts[1]); destination.Add(facts[0]); return; }
            destination.Add(facts[0]);
            if (_fault == "prefix-throw") throw new InvalidOperationException(Marker);
            destination.Add(_fault == "null" ? null : facts[0]);
        }
    }
    // Original pressure owner; only authoring and diagnostic initialization are extended here.
    public sealed class PressureContractModule : ScenarioPressureModule
    {
        private readonly InitializeProbe _initialize;
        private readonly Action<World> _composed;
        public PressureContractModule(InitializeProbe initialize, Action<World> composed = null) { _initialize = initialize; _composed = composed; }
        public bool IndependentAlias { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            base.DeclareContent(types);
            types.Section<IndependentPressureProfileDef>("independent_pressure_profiles");
            types.Section<WorkerPolicyPressureDef>("worker_policy_pressure_sources");
            types.Section<PressureCallbackProbeDef>("pressure_callback_probes");
        }
        public override void Compose(WorldComposer composer)
        {
            base.Compose(composer);
            var profile = composer.Content.Get<IScenarioPressureProfile>("pressure.incidents");
            var source = composer.Content.Get<IPressureSource>("pressure.worker");
            IndependentAlias = !(profile is ScenarioPressureProfileDef) && !(source is PressureSourceDef)
                && composer.Content.Owns(profile) && composer.Content.Owns(source);
            composer.AddSystem(_initialize, 301);
            _composed?.Invoke(composer.World);
        }
    }
}
