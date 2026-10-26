using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public sealed class AutomationModule : GameModule
    {
        public const string ModuleId = "sim.automation";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(OperationsModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<AuthoredControlRuleDef>("control_rules");
            types.Section<AuthoredControlInputRecoveryPolicyDef>("control_input_recovery_policies");
            types.Section<OperationControlActionPolicyDef>("operation_control_actions");
            types.Section<OutletReserveControlActionPolicyDef>("outlet_reserve_control_actions");
            types.Section<StockControlActionPolicyDef>("stock_control_actions");
            types.Section<CompanyAlertControlActionPolicyDef>("company_alert_control_actions");
        }
        public override void Compose(WorldComposer composer)
        {
            var rules = new AutomationSystem(composer.World);
            composer.Services.Bind<IAutomationRules>(rules);
            composer.AddSystem(rules, 290);
            composer.Commands.Register("sim.configure-automation", new ConfigureAutomationHandler());
            composer.Commands.Register("sim.remove-automation", new RemoveAutomationHandler());
        }
    }
}
