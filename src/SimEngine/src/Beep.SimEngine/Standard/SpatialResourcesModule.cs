using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public class SpatialResourcesModule : GameModule
    {
        public const string ModuleId = "sim.spatial-resources";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] { new ModuleRequirement(WorkModule.ModuleId, Version) });
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<TileResourceCellDef>("tile_resource_cells");
            types.Section<SpatialResourceTransferEffectDef>("spatial_resource_transfer_effects");
        }
        public override void Compose(WorldComposer composer)
        {
            composer.Services.Require<IResourceTransaction>();
            composer.Services.Bind<ISpatialResourceEndpoints>(new SpatialResourceEndpoints(composer.World));
            foreach(var effect in composer.Content.All<ISpatialResourceTransferEffectDefinition>())
                SpatialResourceTransferContracts.RequireCapabilities(composer.World,effect);
        }
    }
}
