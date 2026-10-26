using System;
using System.Collections;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class ForwardGameConditionDef : Def, IGameCondition, IGameConditionDependencies
    {
        private IReadOnlyList<string> _conditionIds;
        private string _observationStage, _observationFault;
        private Action _observe;
        public int ObservationCalls { get; private set; }
        public GameConditionContext LastObservedContext { get; private set; }
        public IReadOnlyList<string> ConditionIds { get { Before("dependencies"); return _conditionIds; } }
        public IGameCondition Child { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            if (reader.Raw.Get("conditions")?.Kind != JsonKind.Array) throw reader.Error("explicit condition dependencies required");
            var ids = reader.Strings("conditions");
            if (ids.Length != 1) throw reader.Error("forwarding content requires exactly one child");
            _conditionIds = Array.AsReadOnly(ids);
            _observationStage = reader.String("observation_stage", null);
            _observationFault = reader.String("observation_fault", null);
            if (_observationStage == null ? _observationFault != null :
                _observationStage != "dependencies" && _observationStage != "enumeration" && _observationStage != "binding" && _observationStage != "check"
                || _observationFault != "none" && _observationFault != "throw" && _observationFault != "pause" && _observationFault != "pause-throw")
                throw reader.Error("explicit observation stage/fault pair required");
            if (_observationStage == "enumeration") _conditionIds = new ObservedIds(_conditionIds, () => Before("enumeration"));
        }
        protected override void ResolveCore(ContentResolver content) => Child = content.Def<IGameCondition>(ConditionIds[0]);
        public void ValidateBinding(World world, PlayerId company) { Before("binding"); Child.ValidateBinding(world, company); }
        public Code Check(GameConditionContext context)
        {
            if (_observationStage == "check") LastObservedContext = context;
            Before("check"); return context.Check(Child);
        }
        // Negative instrumentation only: no probe changes ordinary condition decisions or enters saved state.
        internal void InstallObservationProbe(World world)
        {
            if (_observationStage == null) return;
            ObservationCalls = 0; LastObservedContext = null;
            _observe = () => {
                ObservationCalls++;
                if (_observationFault == "pause" || _observationFault == "pause-throw") ((GameClock)world.Clock).Pause();
                if (_observationFault == "throw" || _observationFault == "pause-throw")
                    throw new InvalidOperationException("proof.condition-observation." + _observationStage);
            };
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
    // Engine-side extension condition observes original private plans, never creates progress/state.
    public sealed class CompletedPlanGameConditionDef : Def, IGameCondition
    {
        private string _group;
        protected override void ReadCore(JsonReader reader)
        {
            _group = reader.String("group");
            if (string.IsNullOrWhiteSpace(_group)) throw reader.Error("explicit nonempty plan group required");
        }
        public void ValidateBinding(World world, PlayerId company)
        {
            if (!world.Players.IsSeat(company) || world.ViewFor(company).Feature<WorkView>() == null)
                throw new ContentException(Id + ": selected private work view required");
        }
        public Code Check(GameConditionContext context)
        {
            var rows = new List<PlanView>(); context.View.Feature<WorkView>().CopyPlans(rows);
            foreach (var plan in rows)
                if (plan.Group == _group && plan.State == PlanState.Done) return Code.None;
            return new Code("condition.plan-incomplete");
        }
    }
    public sealed class ConditionContractModule : ConditionCallProofModule
    {
        private readonly InitializeProbe _initialize;
        public ConditionContractModule(InitializeProbe initialize) { _initialize = initialize; }
        public bool IndependentAliases { get; private set; }
        public bool NativeLinks { get; private set; }
        public ConditionCallProbeDef AliasProbe { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            base.DeclareContent(types);
            types.Section<ForwardGameConditionDef>("forward_game_conditions");
            types.Section<CompletedPlanGameConditionDef>("completed_plan_conditions");
        }
        public override void Compose(WorldComposer composer)
        {
            foreach (var condition in composer.Content.All<ForwardGameConditionDef>()) condition.InstallObservationProbe(composer.World);
            base.Compose(composer);
            var content = composer.Content;
            var forwarded = content.Get<IGameCondition>("contract.risk");
            var completed = content.Get<IGameCondition>("contract.completed");
            IndependentAliases = !(forwarded is GameConditionDef) && !(completed is GameConditionDef)
                && content.Owns(forwarded) && content.Owns(completed)
                && ReferenceEquals(((ForwardGameConditionDef)forwarded).Child, content.Get<IGameCondition>("strategy.risk"));
            NativeLinks = content.Get<CompositeGameConditionDef>("contract.allowed").ConditionIds[1] == forwarded.Id
                && ReferenceEquals(content.Get<ConditionObjectivePredicateDef>("contract.plan-predicate").Condition, completed);
            AliasProbe = content.Get<ConditionCallProbeDef>("contract.alias");
            composer.AddSystem(_initialize, 301);
        }
    }
}
