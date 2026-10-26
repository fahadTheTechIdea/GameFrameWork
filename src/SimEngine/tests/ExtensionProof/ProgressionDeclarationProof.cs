using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Registration/alias assertions only. Original work/knowledge/objective services own gameplay.
    public sealed class ProgressionDeclarationModule : GameModule
    {
        private readonly InitializeProbe _initialize;
        public ProgressionDeclarationModule(InitializeProbe initialize) { _initialize = initialize; }
        public override string Id => "proof.progression-declarations";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] {
            new ModuleRequirement(TechnologyModule.ModuleId, Version), new ModuleRequirement(ObjectiveModule.ModuleId, Version) });
        public bool ActualAliases { get; private set; }
        public override void DeclareContent(ContentTypes content)
        {
            content.Section<IndependentTechnologyDef>("independent_technologies");
            content.Section<IndependentObjectiveDef>("independent_objectives");
        }
        public override void Compose(WorldComposer composer)
        {
            var content = composer.Content;
            var research = content.Get<ITechnologyDefinition>("decl.external");
            var objective = content.Get<IObjectiveDefinition>("decl.external-earned");
            ActualAliases = !(research is TechnologyDef) && !(objective is ObjectiveDef)
                && ReferenceEquals(research, content.Get<IndependentTechnologyDef>(research.Id))
                && ReferenceEquals(objective, content.Get<IndependentObjectiveDef>(objective.Id));
            if (!ActualAliases) throw new ContentException("progression declaration proof requires actual independent/native aliases");
            composer.AddSystem(_initialize, 301);
        }
    }
}
