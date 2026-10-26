using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    public sealed class CompanyKnowledgeModule : GameModule
    {
        public const string ModuleId = "sim.knowledge";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override void DeclareContent(ContentTypes types)
        { types.Section<KnowledgeFactDef>("knowledge_facts"); types.Section<KnowledgeEffectDef>("knowledge_effects"); types.Section<TileReadingDef>("tile_readings");
            types.Section<AuthoredKnowledgeSharingDef>("knowledge_sharing_policies");
            types.Section<AuthoredKnowledgeForgettingDef>("knowledge_forgetting_policies"); }
        public override void Compose(WorldComposer composer)
        {
            var knowledge = new CompanyKnowledgeService(composer.World); composer.Services.Bind<ICompanyKnowledge>(knowledge);
            composer.Services.Bind<IKnowledgeFreshness>(knowledge);
            composer.Services.Bind<IKnowledgeSharing>(knowledge);
            composer.Services.Bind<IKnowledgeForgetting>(knowledge);
            composer.Services.Bind(new AuthoredKnowledgeProvider(composer.World));
            composer.Commands.Register("sim.share-knowledge", new ShareKnowledgeHandler());
            composer.Commands.Register("sim.forget-knowledge", new ForgetKnowledgeHandler());
            composer.AddViewFeature(Id, 5, (world, viewer) => new KnowledgeView(world, viewer));
        }
    }
}
