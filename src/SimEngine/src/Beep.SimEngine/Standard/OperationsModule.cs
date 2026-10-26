using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    public sealed class OperationsModule : GameModule
    {
        public const string ModuleId = "sim.operations";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<AutomationSignalDef>("signals");
            types.Section<ResourceAutomationSignalDef>("resource_signals");
        }
        public override void Compose(WorldComposer composer)
        {
            AutomationSignalContracts.ValidateComposition(composer.World);
            composer.Commands.Register("sim.configure-operation", new ConfigureOperationHandler());
            composer.AddViewFeature("sim.operations", 14, (world, viewer) => new OperationsView(world, viewer));
        }
    }
}
