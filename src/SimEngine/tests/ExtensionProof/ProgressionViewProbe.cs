using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Negative observation instrumentation only. Native owners retain all healthy gameplay.
    public sealed class ProgressionViewPredicate : Def, IObjectivePredicate
    {
        internal Action<World> Probe;
        internal bool Malformed;
        public IReadOnlyList<string> Dependencies => Array.Empty<string>();
        public IReadOnlyList<IContentDefinition> ObservationContent => Array.Empty<IContentDefinition>();
        protected override void ReadCore(JsonReader reader) { }
        public void ValidateBinding(World world) { }
        public ObjectiveObservation Observe(ObjectiveContext context, IReadOnlyDictionary<string, ObjectiveObservation> children)
        { Probe?.Invoke(context.World); return Malformed ? default : ObjectiveObservation.Wait(new Code("proof.wait")); }
    }
    public sealed class ProgressionViewProbeModule : GameModule
    {
        private ProgressionViewPredicate _predicate;
        public string Hidden;
        public string FailingFamily;
        public bool Pause;
        public int Reads;
        public override string Id => "proof.progression-views";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] {
            new ModuleRequirement(TechnologyModule.ModuleId, Version), new ModuleRequirement(ObjectiveModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types) => types.Section<ProgressionViewPredicate>("progression_view_predicates");
        public override void Compose(WorldComposer composer)
        {
            _predicate = composer.Content.Get<ProgressionViewPredicate>("probe.progression");
            composer.Services.Bind<IVisibility>(new Visibility(this));
        }
        public void ArmPredicate(string mode)
        {
            _predicate.Malformed = mode == "malformed";
            _predicate.Probe = world => {
                Reads++;
                if (mode == "throw") throw new InvalidOperationException("proof.progression.predicate.first");
                if (mode == "clock") ((GameClock)world.Clock).Pause();
            };
        }
        private sealed class Visibility : IVisibility
        {
            private readonly ProgressionViewProbeModule _probe;
            internal Visibility(ProgressionViewProbeModule probe) => _probe = probe;
            public bool CanSee(World world, PlayerId viewer, Entity entity) => viewer == PlayerId.None || viewer == entity.Owner;
            public bool CanSeeContent(World world, PlayerId viewer, IContentDefinition definition)
            {
                bool selected = _probe.FailingFamily == "technology" && definition is ITechnologyDefinition
                    || _probe.FailingFamily == "objective" && definition is IObjectiveDefinition;
                if (selected && ++_probe.Reads == 2)
                {
                    if (_probe.Pause) ((GameClock)world.Clock).Pause();
                    else throw new InvalidOperationException("proof.progression.visibility.first");
                }
                return definition.Id != _probe.Hidden;
            }
        }
    }
}
