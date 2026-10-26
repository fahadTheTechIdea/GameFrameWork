using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independently authored contract providers, not native objective inheritance.
    public sealed class IndependentObjectiveDef : Def, IObjectiveDefinition
    {
        private readonly ObjectiveDeclaration _declaration = new ObjectiveDeclaration();
        public IReadOnlyList<string> Dependencies => _declaration.Dependencies;
        public IObjectivePredicate Predicate => _declaration.Predicate;
        public GameDuration? Deadline => _declaration.Deadline;
        public IObjectiveEffect OnComplete => _declaration.OnComplete;
        public IObjectiveEffect OnFailure => _declaration.OnFailure;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    // Passes an existing authored fact through the native captured predicate DAG.
    public sealed class ForwardObjectivePredicateDef : Def, IObjectivePredicate
    {
        public IReadOnlyList<IContentDefinition> ObservationContent => Array.Empty<IContentDefinition>();
        public IReadOnlyList<string> Dependencies { get; private set; }
        protected override void ReadCore(JsonReader reader) => Dependencies = Array.AsReadOnly(new[] { reader.String("child") });
        protected override void ResolveCore(ContentResolver content) => content.Def<IObjectivePredicate>(Dependencies[0]);
        public void ValidateBinding(World world) { }
        public ObjectiveObservation Observe(ObjectiveContext context, IReadOnlyDictionary<string, ObjectiveObservation> children)
            => children[Dependencies[0]];
    }
    // Content-owned behavior only; no state, clock or copied lifecycle.
    public sealed class IndependentObjectiveEffectDef : Def, IObjectiveEffect
    {
        private string _gate;
        public IObjectivePredicate Gate { get; private set; }
        protected override void ReadCore(JsonReader reader) => _gate = reader.String("gate");
        protected override void ResolveCore(ContentResolver content)
        {
            Gate = content.Def<IObjectivePredicate>(_gate);
            if (Gate.Dependencies.Count != 0) throw content.Fail("effect gate requires an authored leaf predicate");
        }
        public void ValidateBinding(World world) => Gate.ValidateBinding(world);
        public Code Check(ObjectiveContext context)
        {
            var observation = Gate.Observe(context, new Dictionary<string, ObjectiveObservation>());
            return observation.Satisfied ? Code.None : observation.Blocker;
        }
        public void Apply(ObjectiveContext context)
            => context.World.Notify(new Code("proof.objective-contract-effect"), context.Company, subject: context.Objective);
    }
    public sealed class ObjectiveContractModule : ObjectiveProofModule
    {
        private readonly InitializeProbe _initialize;
        public ObjectiveContractModule(InitializeProbe initialize) { _initialize = initialize; }
        public bool IndependentAliases { get; private set; }
        public bool NativeLinks { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            base.DeclareContent(types);
            types.Section<IndependentObjectiveDef>("independent_objectives");
            types.Section<ForwardObjectivePredicateDef>("forward_objective_predicates");
            types.Section<IndependentObjectiveEffectDef>("independent_objective_effects");
        }
        public override void Compose(WorldComposer composer)
        {
            base.Compose(composer);
            var content = composer.Content;
            var definition = content.Get<IObjectiveDefinition>("field.build");
            var predicate = content.Get<IObjectivePredicate>("objective.forward-tower");
            var effect = content.Get<IObjectiveEffect>("objective.wait-two");
            IndependentAliases = !(definition is ObjectiveDef) && !(predicate is ObjectivePredicateDef) && !(effect is ObjectiveEffectDef)
                && content.Owns(definition) && content.Owns(predicate) && content.Owns(effect)
                && ReferenceEquals(definition.OnComplete, effect);
            NativeLinks = content.Get<IObjectiveDefinition>("field.win").Dependencies[0] == definition.Id
                && content.Get<IObjectivePredicate>("objective.established").Dependencies[0] == predicate.Id
                && predicate.Dependencies[0] == "objective.has-tower";
            composer.AddSystem(_initialize, 301);
        }
    }
}
