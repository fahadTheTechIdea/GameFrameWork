using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Standard
{
    /// <summary>Optional tile routing and mover provider; no work or economy dependency.</summary>
    public sealed class TileNavigationModule : GameModule
    {
        public const string ModuleId = "sim.tile-navigation";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[]
        {
            new ModuleRequirement(MotionModule.ModuleId, Version)
        });
        public override void DeclareContent(ContentTypes types)
        {
            types.Component<MoverDef>("mover", 4);
            types.Section<UniformTileNavigationDef>("uniform_tile_navigation");
            types.Section<LayeredTileNavigationDef>("layered_tile_navigation");
        }
        public override void Compose(WorldComposer composer)
        {
            if (!(composer.World.Space is ITileNavigationSpace))
                throw new ContentException("selected tile navigation requires an authored tile-space provider");
            var navigation = new NavService(composer.World); composer.Services.Bind(navigation);
            foreach (ITileNavigationRule rule in composer.Content.All<ITileNavigationRule>())
                if (EntityObservation.ReadProvider(composer.World, rule, null, () => rule.Space, composer.World.Space) == composer.World.Space.Id)
                    navigation.Define(rule);
            composer.Commands.Register("sim.move-vehicle", new MoveVehicleHandler());
        }
    }
}
