using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Optional authored policy selection only. WorkBoard remains lifecycle/update/state owner.</summary>
    public class PlanHistoryModule : GameModule
    {
        public const string ModuleId = "sim.plan-history";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => Array.AsReadOnly(new[] { new ModuleRequirement(WorkModule.ModuleId, Version) });
        public override void DeclareContent(ContentTypes types) => types.Section<AuthoredPlanHistoryPolicyDef>("plan_history_policies");
        public override void Compose(WorldComposer composer)
        {
            JsonReader settings = composer.Scenario.SettingsFor(Id) ?? throw new ContentException("selected plan history module needs an explicit policy");
            string id = settings.String("policy"); settings.End();
            foreach (var policy in composer.Content.All<IPlanHistoryPolicy>())
                PlanHistoryPolicyContracts.Read(composer.World, policy, 0);
            composer.Services.Get<WorkBoard>().SelectPlanHistoryPolicy(composer.Content.Get<IPlanHistoryPolicy>(id));
        }
    }
}
