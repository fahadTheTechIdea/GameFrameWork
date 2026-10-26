using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    public class TechnologyModule : GameModule
    {
        public const string ModuleId = "sim.technology";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] {
            new ModuleRequirement(WorkModule.ModuleId, Version), new ModuleRequirement(CompanyKnowledgeModule.ModuleId, Version) });
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<TechnologyDef>("technologies");
            types.Section<EntityTechnologyUnlockDef>("technology_entity_unlocks");
            types.Section<ActivityTechnologyUnlockDef>("technology_activity_unlocks");
            types.Section<TransformationTechnologyUnlockDef>("technology_transformation_unlocks");
            types.Section<LineTechnologyUnlockDef>("technology_line_unlocks");
            types.Section<TechnologyCompletionDef>("technology_completion_effects");
            types.Section<TechnologyRequirementDef>("technology_requirements");
        }
        public override void Compose(WorldComposer composer)
        {
            var knowledge = new TechnologyService(composer.World); composer.Services.Bind(knowledge);
            composer.Services.Bind<ITechnologyKnowledge>(knowledge);
            composer.AddViewFeature("sim.technology", 2, (world, viewer) => new TechnologyView(world, viewer));
        }
    }
}
