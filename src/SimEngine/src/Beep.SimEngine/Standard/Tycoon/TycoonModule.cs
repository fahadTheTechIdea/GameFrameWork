using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    /// <summary>Optional providers; resolved game configuration is the sole enablement authority.</summary>
    public sealed class TycoonModule : GameModule
    {
        public const string ModuleId = "sim.tycoon";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<FixedResourceValuesDef>("fixed_resource_values");
            types.Section<FixedResourceValueDef>("resource_values");
            types.Section<BusinessAccountPolicyDef>("business_accounts");
            types.Section<PrimaryProductionIncomeDef>("primary_production_income");
            types.Section<WorkCostRequirementDef>("work_cost_requirements");
            types.Section<ActivityCostBindingDef>("activity_cost_bindings");
            types.Section<EntityOperatingCostDef>("entity_operating_costs");
            types.Section<RunningOperationCostConditionDef>("operation_running_cost_conditions");
            types.Section<TransformationCostBindingDef>("transformation_cost_bindings");
            types.Section<ContentActionChargeDef>("content_action_charges");
            types.Section<ContentActionActivityBindingDef>("content_action_activity_bindings");
            types.Section<ResourceSaleDef>("resource_sales");
            types.Section<BusinessSalesReportDef>("business_sales_reports");
            types.Component<SupplierDef>("supplier", 4);
        }
        public override void Compose(WorldComposer composer)
        {
            var rules = new BusinessModeRules(composer.World.GameMode);
            JsonReader settings = composer.Scenario.SettingsFor(Id);
            string valuationId = ReadIdentity(settings, "valuation_policy");
            string accountsId = ReadIdentity(settings, "account_policy");
            string incomeId = ReadIdentity(settings, "income_policy");
            settings?.End(); // Unknown keys refuse; there is no old mode/accounts reader.

            // Never advertise capabilities whose transaction/provider integration is not installed.
            if (rules.TycoonEnabled) throw new ContentException("Tycoon requires an actual advanced economic provider; fixed defaults are not a substitute");

            BusinessAccounts accounts = null;
            if (rules.MoneyEnabled)
            {
                if (accountsId == null) throw new ContentException("enabled money requires an explicit account_policy");
                IBusinessAccountDefinition definition = composer.Content.Get<IBusinessAccountDefinition>(accountsId);
                if (definition.OpeningCash < Fix.Zero || definition.CurrencyUnit?.Dimension != "currency" ||
                    !definition.CurrencyUnit.Unit.IsValid || definition.CurrencyUnit.Discrete)
                    throw new ContentException("accounts require nonnegative opening cash and an authored continuous currency");
                accounts = new BusinessAccounts(composer.World.Players.Count, definition.OpeningCash, definition.CurrencyUnit.Unit, definition.Id);
            }
            IResourceValuation valuation = null;
            if (rules.ValuesEnabled)
            {
                if (valuationId == null) throw new ContentException("enabled values require an explicit valuation_policy");
                IBusinessValuePolicyDefinition definition = composer.Content.Get<IBusinessValuePolicyDefinition>(valuationId);
                if (definition.Mode != BusinessMode.FixedDefaults || definition.CurrencyUnit?.Dimension != "currency" ||
                    !definition.CurrencyUnit.Unit.IsValid || definition.CurrencyUnit.Discrete)
                    throw new ContentException("selected valuation must implement enabled fixed-default rules and an authored currency");
                valuation = definition.Create(composer.World) ?? throw new ContentException("valuation policy returned no provider");
                if (valuation.Policy != definition.Id || valuation.Mode != definition.Mode || valuation.Currency != definition.CurrencyUnit.Unit)
                    throw new ContentException("valuation provider differs from its selected policy/mode/currency");
                if (accounts != null && accounts.Currency != valuation.Currency)
                    throw new ContentException("selected accounts and valuation use different currencies");
            }
            if (rules.ProductionIncomeEnabled)
            {
                if (incomeId == null || accounts == null || valuation == null)
                    throw new ContentException("enabled production income requires an explicit income_policy, accounts and valuation");
                IProductionIncomePolicyDefinition definition = composer.Content.Get<IProductionIncomePolicyDefinition>(incomeId);
                IProductionIncomePolicy income = definition.Create(composer.World)
                    ?? throw new ContentException("income policy returned no provider");
                if (income.Id != definition.Id) throw new ContentException("income provider differs from its selected definition");
                accounts.ConfigureIncome(composer.World, valuation, income);
            }
            if (rules.TradingEnabled) accounts.ConfigurePurchasing(rules);
            // Validate every selected provider before publishing service aliases.
            composer.Services.Bind<IBusinessModeRules>(rules);
            composer.Services.Bind<IResourceTradingRules>(rules);
            if (accounts != null) { composer.Services.Bind<IEconomy>(accounts); composer.Services.Bind<IBusinessAccounts>(accounts); composer.Services.Bind<IMonetaryBudgets>(accounts); }
            if (valuation != null) composer.Services.Bind<IResourceValuation>(valuation);
            if (rules.ProductionIncomeEnabled) composer.Services.Bind<IResourceProductionSettlement>(accounts);
            if (rules.TradingEnabled) composer.Services.Bind<IResourcePurchaseSettlement>(accounts);
            var operatingCosts = new EntityOperatingCostSystem(composer.World);
            composer.Services.Bind<IBusinessContentCharges>(new ContentActionCharges(composer.World, accounts));
            composer.Services.Bind<IBusinessOperatingCosts>(operatingCosts); composer.AddSystem(operatingCosts, 601);
            var sales = new ResourceSalesSystem(composer.World, accounts);
            composer.Services.Bind<IBusinessSales>(sales); composer.AddSystem(sales, 599);
            if (rules.TradingEnabled) composer.Services.Bind<IResourceSaleSettlement>(accounts);
            composer.Events.Register<BusinessSaleFact>(BusinessSaleFact.EventId, 1);
            composer.AddViewFeature(Id, 11, (world, viewer) => new BusinessView(world, viewer));
        }
        private static string ReadIdentity(JsonReader settings, string key)
        {
            if (settings == null) return null;
            JsonValue value = settings.Value(key);
            if (value == null || value.Kind == JsonKind.Null) return null;
            if (value.Kind != JsonKind.String) throw settings.Error(key + " must be a policy identity or null");
            try { ContractIdentity.ValidateId(value.Text, key); }
            catch (ArgumentException error) { throw settings.Error(error.Message); }
            return value.Text;
        }
    }
}
