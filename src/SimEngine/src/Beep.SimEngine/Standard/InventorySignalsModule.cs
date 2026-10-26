using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Optional metadata-only bridge; inventories, signals and controls keep their existing owners.</summary>
    public sealed class InventorySignalsModule : GameModule
    {
        public const string ModuleId = "sim.inventory-signals";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[]
        {
            new ModuleRequirement(InventoryModule.ModuleId, Version),
            new ModuleRequirement(OperationsModule.ModuleId, Version)
        });
        public override void DeclareContent(ContentTypes types) => types.Component<InventorySignalDef>("inventory_signal", 1);
        public override void Compose(WorldComposer composer) => InventorySignalBindingContracts.ValidateComposition(composer.World);
    }
}
