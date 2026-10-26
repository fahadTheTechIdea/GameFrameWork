using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Optional authored initialization through the sole existing world-generation pipeline.</summary>
    public sealed class WorldSetupModule : GameModule
    {
        public const string ModuleId = "sim.world-setup";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<WorldSetupProfileDef>("world_setup_profiles");
            types.Section<EntitySetupDef>("entity_setups"); types.Section<TileLayerSetupDef>("tile_layer_setups");
        }
        public override void Compose(WorldComposer composer)
        {
            JsonReader settings = composer.Scenario.SettingsFor(Id) ?? throw new ContentException("selected world setup needs explicit profile");
            string profile = settings.String("profile"); settings.End();
            composer.AddWorldGen(new WorldSetupStage(composer.Content.Get<IWorldSetupProfileDefinition>(profile)), 100);
        }
    }
}
