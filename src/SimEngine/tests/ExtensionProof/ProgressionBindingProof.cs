using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Negative-only binding instrumentation. These unused definitions never execute gameplay.
    public interface IProgressionBindingDependency { }
    public abstract class ProgressionBindingProbeDef : Def
    {
        private Fix _minimum, _maximum;
        private string _mode;
        internal Action Validated;
        protected override void ReadCore(JsonReader reader)
        {
            _minimum = reader.Fix("minimum"); _maximum = reader.Fix("maximum"); _mode = reader.String("mode");
            if (_mode != "valid" && _mode != "provider" && _mode != "throw" && _mode != "clock")
                throw reader.Error("explicit binding probe mode required");
        }
        public void ValidateBinding(World world)
        {
            Validated?.Invoke();
            if (_minimum < Fix.Zero || _maximum > Fix.One || _minimum > _maximum)
                throw new ContentException(Id + ": declared probe interval must lie within [0,1]");
            if (_mode == "provider" && !world.Services.Has<IProgressionBindingDependency>())
                throw new ContentException(Id + ": required binding dependency missing");
            if (_mode == "throw") throw new InvalidOperationException("proof.progression.binding.first");
            if (_mode == "clock") ((GameClock)world.Clock).Pause();
        }
    }
    public sealed class ProgressionBindingPredicateDef : ProgressionBindingProbeDef, IObjectivePredicate
    {
        public IReadOnlyList<string> Dependencies => Array.Empty<string>();
        public IReadOnlyList<IContentDefinition> ObservationContent => Array.Empty<IContentDefinition>();
        public ObjectiveObservation Observe(ObjectiveContext context, IReadOnlyDictionary<string, ObjectiveObservation> children)
            => throw new InvalidOperationException("unused binding probe must never observe gameplay");
    }
    public sealed class ProgressionBindingEffectDef : ProgressionBindingProbeDef, IObjectiveEffect
    {
        public Code Check(ObjectiveContext context) => throw new InvalidOperationException("unused binding probe must never check gameplay");
        public void Apply(ObjectiveContext context) => throw new InvalidOperationException("unused binding probe must never apply gameplay");
    }
    public sealed class ProgressionBindingModule : GameModule
    {
        public override string Id => "proof.progression-bindings";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(ObjectiveModule.ModuleId, Version) };
        public int PredicateValidations { get; private set; }
        public int EffectValidations { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<ProgressionBindingPredicateDef>("progression_binding_predicates");
            types.Section<ProgressionBindingEffectDef>("progression_binding_effects");
        }
        public override void Compose(WorldComposer composer)
        {
            composer.Content.Get<ProgressionBindingPredicateDef>("binding.unused-predicate").Validated = () => PredicateValidations++;
            composer.Content.Get<ProgressionBindingEffectDef>("binding.unused-effect").Validated = () => EffectValidations++;
        }
    }
}
