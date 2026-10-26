using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public class KnowledgeVisibilityModule : GameModule
    {
        public const string ModuleId = "sim.knowledge-visibility";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] { new ModuleRequirement(CompanyKnowledgeModule.ModuleId, Version) });
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<KnowledgeVisibilityPolicyDef>("knowledge_visibility_policies");
            types.Section<EntityKnowledgeVisibilityDef>("entity_knowledge_visibility");
            types.Section<EntityPublicVisibilityDef>("entity_public_visibility");
            types.Section<KnowledgeVisibilityRuleBindingDef>("knowledge_visibility_rule_bindings");
            types.Section<ActivityKnowledgeVisibilityDef>("activity_knowledge_visibility");
            types.Section<TransformationKnowledgeVisibilityDef>("transformation_knowledge_visibility");
            types.Section<LayerKnowledgeVisibilityDef>("layer_knowledge_visibility");
            types.Section<ObjectiveKnowledgeVisibilityDef>("objective_knowledge_visibility");
            types.Section<ObjectivePredicateKnowledgeVisibilityDef>("objective_predicate_knowledge_visibility");
            types.Section<LayerRegionKnowledgeVisibilityDef>("layer_region_knowledge_visibility");
            types.Section<LayerFreshKnowledgeVisibilityDef>("layer_fresh_knowledge_visibility");
            types.Section<LayerSensorVisibilityDef>("layer_sensor_visibility");
            types.Section<EntitySensorVisibilityDef>("entity_sensor_visibility");
            types.Component<TileSensorDef>("tile_sensor", 1);
        }
        public override void Compose(WorldComposer composer)
        {
            JsonReader settings = composer.Scenario.SettingsFor(Id) ?? throw new ContentException("selected visibility needs explicit policy");
            string id = settings.String("policy"); settings.End();
            IKnowledgeVisibilityDefinition policy = composer.Content.Find<IKnowledgeVisibilityDefinition>(id) ?? throw new ContentException("missing visibility policy: " + id);
            composer.Services.Bind<IVisibility>(new KnowledgeVisibility(composer.World, policy));
        }
    }
}
