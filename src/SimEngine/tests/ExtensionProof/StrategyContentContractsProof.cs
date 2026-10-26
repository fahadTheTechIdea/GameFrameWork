using System;
using System.Collections;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent authored factory metadata; native playbook retains every decision/state rule.
    public sealed class IndependentControllerProfileDef : Def, IPlayerControllerDefinition, IDependencyNode
    {
        private string _profile, _factory, _observationStage, _observationFault;
        private GameDuration _cadence;
        private int _commandsPerDecision;
        private IReadOnlyList<string> _dependencies;
        private Action _observe;
        public int ObservationCalls { get; private set; }
        public GameDuration Cadence { get { Before("cadence"); return _cadence; } }
        public int CommandsPerDecision { get { Before("budget"); return _commandsPerDecision; } }
        public IReadOnlyList<string> Dependencies { get { Before("dependencies"); return _dependencies; } }
        public IPlayerControllerDefinition Delegate { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            _profile = reader.String("profile"); _factory = reader.String("factory");
            if (_factory != "delegate" && _factory != "null") throw reader.Error("explicit factory policy required");
            _cadence = (reader.Child("cadence") ?? throw reader.Error("explicit cadence required")).RationalDuration();
            _commandsPerDecision = reader.Int("commands_per_decision");
            _dependencies = Array.AsReadOnly(new[] { _profile });
            _observationStage = reader.String("observation_stage", null);
            _observationFault = reader.String("observation_fault", null);
            if (_observationStage == null ? _observationFault != null :
                _observationStage != "cadence" && _observationStage != "budget" && _observationStage != "dependencies"
                && _observationStage != "enumeration" && _observationStage != "factory" && _observationStage != "binding"
                || _observationFault != "none" && _observationFault != "throw" && _observationFault != "pause" && _observationFault != "pause-throw" && _observationFault != "overflow")
                throw reader.Error("explicit controller observation stage/fault pair required");
            if (_observationStage == "enumeration") _dependencies = new ObservedIds(_dependencies, () => Before("enumeration"));
        }
        protected override void ResolveCore(ContentResolver content) => Delegate = content.Def<IPlayerControllerDefinition>(_profile);
        public IPlayerController Create() { Before("factory"); return _factory == "null" ? null : Delegate.Create(); }
        public void ValidateBinding(World world, PlayerId company) { Before("binding"); Delegate.ValidateBinding(world, company); }
        // Adversarial framework instrumentation only. Never normal gameplay, persisted policy or progress.
        internal void InstallObservationProbe(World world)
        {
            if (_observationStage == null) return;
            ObservationCalls = 0;
            _observe = () => {
                ObservationCalls++;
                if (_observationFault == "overflow") throw new OverflowException("proof.controller-observation." + _observationStage);
                if (_observationFault == "pause" || _observationFault == "pause-throw") ((GameClock)world.Clock).Pause();
                if (_observationFault == "throw" || _observationFault == "pause-throw")
                    throw new ApplicationException("proof.controller-observation." + _observationStage);
            };
        }
        public void ArmObservationFault(string fault)
        {
            if (_observe == null || fault != "throw" && fault != "pause" && fault != "pause-throw")
                throw new ArgumentException("installed explicit observation fault required", nameof(fault));
            ObservationCalls = 0; _observationFault = fault;
        }
        private void Before(string stage) { if (_observationStage == stage) _observe?.Invoke(); }
        private sealed class ObservedIds : IReadOnlyList<string>
        {
            private readonly IReadOnlyList<string> _ids;
            private readonly Action _before;
            internal ObservedIds(IReadOnlyList<string> ids, Action before) { _ids = ids; _before = before; }
            public int Count => _ids.Count;
            public string this[int index] => _ids[index];
            public IEnumerator<string> GetEnumerator() { _before(); return _ids.GetEnumerator(); }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
    public sealed class IndependentPlanTemplateDef : Def, IPlanTemplateContent
    {
        public PlanTemplate Template { get; private set; }
        protected override void ReadCore(JsonReader reader) => Template = PlanTemplate.Read(reader);
    }
    public sealed class StrategyContractModule : PlayerControllerProofModule
    {
        private readonly InitializeProbe _initialize;
        private World _observationWorld;
        public StrategyContractModule(InitializeProbe initialize) { _initialize = initialize; }
        public bool IndependentAliases { get; private set; }
        public bool NativeLinks { get; private set; }
        public string ObservationFaultPhase => _observationWorld?.Fault?.Phase;
        public override void DeclareContent(ContentTypes types)
        {
            base.DeclareContent(types);
            types.Section<IndependentControllerProfileDef>("independent_controller_profiles");
            types.Section<IndependentPlanTemplateDef>("independent_plan_templates");
        }
        public override void Compose(WorldComposer composer)
        {
            _observationWorld = composer.World;
            foreach (var definition in composer.Content.All<IndependentControllerProfileDef>()) definition.InstallObservationProbe(composer.World);
            base.Compose(composer);
            var content = composer.Content;
            var profile = content.Get<IPlayerControllerDefinition>("contract.controller");
            var template = content.Get<IPlanTemplateContent>("recovery.tower");
            var sites = content.Get<IPlanSiteSource>("contract.sites");
            var recovery = content.Get<IPlanRecoveryPolicy>("recovery.external");
            IndependentAliases = !(profile is PlayerControllerDef) && !(template is PlanTemplateDef)
                && !(sites is PlanSiteSourceDef) && !(recovery is PlanRecoveryPolicyDef)
                && content.Owns(profile) && content.Owns(template) && content.Owns(sites) && content.Owns(recovery);
            NativeLinks = ReferenceEquals(((IndependentControllerProfileDef)profile).Delegate, content.Get<IPlayerControllerDefinition>("recovery.controller"))
                && template.Template.Entries.Count == 1 && template.Template.Entries[0].Definition == "build:point.tower";
            composer.AddSystem(_initialize, 301);
        }
    }
}
