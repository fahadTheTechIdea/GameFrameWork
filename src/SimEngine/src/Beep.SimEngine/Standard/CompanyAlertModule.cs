using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    public sealed class CompanyAlertModule : GameModule
    {
        public const string ModuleId = "sim.company-alerts";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(WorkModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types) => types.Section<AuthoredCompanyAlertProfileDef>("company_alert_profiles");
        public override void Compose(WorldComposer composer)
        {
            JsonReader settings = composer.Scenario.SettingsFor(ModuleId) ?? throw new ContentException("selected company-alerts needs explicit scenario bindings");
            JsonValue companies = settings.Array("companies") ?? throw settings.Error("companies must be explicit"); settings.End();
            var initial = new SortedDictionary<int, CompanyAlertSettings>();
            for (int i = 0; i < companies.Count; i++)
            {
                var row = new JsonReader(companies[i], settings.Where + ".companies[" + i + "]");
                int company = row.Int("company"); CompanyAlertSettings value = CompanyAlertSettings.Read(row); row.End();
                if (company < 0 || company >= composer.Scenario.Seats || initial.ContainsKey(company)) throw row.Error("duplicate/unknown scenario company");
                if (composer.Content.Find<ICompanyAlertProfileDefinition>(value.Profile) == null) throw row.Error("unknown company alert profile");
                initial.Add(company, value);
            }
            composer.Services.Require<WorkBoard>();
            var policies = new CompanyAlertPolicies(composer.World, initial);
            composer.Services.Bind<ICompanyAlertPolicy>(policies); composer.Services.Bind<ICompanyWorkerReservePolicy>(policies);
            composer.Commands.Register("sim.configure-company-alert", new Commands.ConfigureCompanyAlertHandler());
            composer.AddViewFeature(ModuleId, 4, (world, viewer) => new CompanyAlertView(world, viewer));
        }
    }
}
