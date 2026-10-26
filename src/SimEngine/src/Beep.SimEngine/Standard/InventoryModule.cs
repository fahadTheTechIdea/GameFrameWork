using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    public sealed class InventoryModule : GameModule
    {
        public const string ModuleId = "sim.inventory";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override void DeclareContent(ContentTypes types)
        {
            types.Component<StorageDef>("storage", 5);
            types.Component<InventoryPortDef>("inventory_port", 1);
            types.Component<OperatingInputsDef>("operating_inputs", 1);
            types.Section<ResourceQualityDef>("resource_qualities");
            types.Section<ResourceLotDef>("resource_lots");
            types.Section<ResourceLotSelectionDef>("resource_selections");
        }
        public override void Compose(WorldComposer composer)
        {
            ResourceLotContentContracts.Validate(composer.World);
            composer.Events.Register<ResourceFlowFact>(ResourceFlowFact.EventId, 7);
            composer.Services.Bind<IResourceTransaction>(new ResourceTransactions(composer.World));
            composer.Commands.Register("sim.select-resource-port",new Commands.SelectResourcePortHandler());
            composer.AddViewFeature("sim.inventory", 13, (world, viewer) => new InventoryView(world, viewer));
        }
    }
}
