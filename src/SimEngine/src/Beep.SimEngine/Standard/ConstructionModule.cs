using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    public sealed class ConstructionModule : GameModule
    {
        public const string ModuleId = "sim.construction";
        public const int CleanupOrder = 405;
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[]
        {
            new ModuleRequirement(WorkModule.ModuleId, Version),
            new ModuleRequirement(InventoryModule.ModuleId, Version)
        });
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<ConstructionEffectDef>("construction_effects");
            types.Section<ResourceRecoveryDef>("resource_recovery_profiles");
            types.Component<ConstructibleDef>("construction", 1);
            types.Component<SiteDef>("site", 1);
        }
        public override void Compose(WorldComposer composer)
        {
            if (!(composer.World.Space is ISpatialConstructionPlacement)) throw new ContentException("selected construction requires a construction placement provider");
            composer.Services.Require<WorkBoard>();
            JsonReader settings = composer.Scenario.SettingsFor(ModuleId) ?? throw new ContentException("selected construction needs explicit recovery selection");
            JsonValue recovery = settings.Value("recovery"); settings.End();
            if (recovery == null || recovery.Kind != JsonKind.Null && recovery.Kind != JsonKind.String)
                throw new ContentException("construction recovery must explicitly name a profile or be null");
            var construction = new ConstructionService(composer.World, recovery.Kind == JsonKind.Null ? null : composer.Content.Get<IResourceRecoveryDefinition>(recovery.Text));
            composer.Services.Bind<IConstructionProjects>(construction);
            composer.Services.Bind<IResourceRecovery>(construction);
            composer.AddSystem(new ConstructionCleanupSystem(), CleanupOrder);
            composer.Events.Register<StructureBuilt>("sim.structure-built", 1);
            composer.Events.Register<StructureRemoved>("sim.structure-removed", 1);
            composer.Commands.Register("sim.construct", new ConstructHandler());
            composer.Commands.Register("sim.cancel-construction", new CancelConstructionHandler());
            composer.Commands.Register("sim.demolish", new DemolishHandler());
            composer.AddViewFeature("sim.construction", 5, (world, viewer) => new ConstructionView(world, viewer));
        }
    }

    /// <summary>Retries disposition of inactive sites through their existing order, stock and claims.</summary>
    public sealed class ConstructionCleanupSystem : GameSystem
    {
        public override string Id => "sim.construction.cleanup";
        public override int StateSchemaVersion => 1;
        public override void Update(World world, TickInfo tick)
        {
            ComponentList<IConstructionSite> sites = world.Entities.Components<IConstructionSite>();
            WorkBoard board = world.Services.Get<WorkBoard>();
            for (int i = 0; i < sites.Count; i++)
            {
                IConstructionSite site = sites[i];
                if (site.Entity.Alive && !(board.Order(site.Order)?.IsActive ?? false))
                    world.Services.Get<IConstructionProjects>().Abandon(world, site.Entity);
            }
        }
    }
}
