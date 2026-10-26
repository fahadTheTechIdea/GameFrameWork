using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    public class PlayerControllersModule : GameModule
    {
        public const string ModuleId = "sim.player-controllers";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<PlanPlaybookDef>("plan_playbooks");
            types.Section<AuthoredPlanSitesDef>("plan_site_sets");
            types.Section<PendingPlanRecoveryDef>("pending_plan_recovery_policies");
        }
        public override void Compose(WorldComposer composer)
        {
            JsonReader settings = composer.Scenario.SettingsFor(Id) ?? throw new ContentException("selected controllers need explicit company bindings");
            string defaultId = settings.NullableString("default_profile");
            JsonValue rows = settings.Array("companies") ?? throw settings.Error("companies required"); settings.End();
            var profiles = new SortedDictionary<int, IPlayerControllerDefinition>();
            for (int i = 0; i < rows.Count; i++)
            {
                var row = new JsonReader(rows[i], settings.Where + ".companies[" + i + "]");
                int company = row.Int("company"); string id = row.String("profile"); row.End();
                IPlayerControllerDefinition profile = composer.Content.Find<IPlayerControllerDefinition>(id);
                if (company < 0 || profiles.ContainsKey(company) || profile == null) throw row.Error("invalid company or missing/duplicate controller profile");
                profiles.Add(company, profile);
            }
            if (defaultId != null)
            {
                IPlayerControllerDefinition profile = composer.Content.Find<IPlayerControllerDefinition>(defaultId)
                    ?? throw new ContentException("default controller profile is missing: " + defaultId);
                for (int company = 0; company < composer.World.Players.Count; company++)
                    if (!profiles.ContainsKey(company)) profiles.Add(company, profile);
            }
            var system = new PlayerControllersSystem(composer.World, profiles);
            composer.Services.Bind<IPlayerControllers>(system); composer.AddSystem(system, 900);
            composer.AddViewFeature(Id, 7, (world, viewer) => new PlayerControllersView(world, viewer));
        }
    }
}
