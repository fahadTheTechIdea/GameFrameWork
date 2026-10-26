using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Immutable economic interpretation, not a ledger, provider catalog or loop.</summary>
    public interface IBusinessModeRules
    {
        bool ValuesEnabled { get; }
        bool MoneyEnabled { get; }
        bool ProductionIncomeEnabled { get; }
        bool ConstructionCostsEnabled { get; }
        bool ManufacturingCostsEnabled { get; }
        bool RunningCostsEnabled { get; }
        bool TradingEnabled { get; }
        bool TycoonEnabled { get; }
        bool CostEnabled(MonetaryCostCategory category);
    }
    public sealed class BusinessModeRules : IBusinessModeRules, IResourceTradingRules
    {
        public bool ValuesEnabled { get; }
        public bool MoneyEnabled { get; }
        public bool ProductionIncomeEnabled { get; }
        public bool ConstructionCostsEnabled { get; }
        public bool ManufacturingCostsEnabled { get; }
        public bool RunningCostsEnabled { get; }
        public bool TradingEnabled { get; }
        public bool TycoonEnabled { get; }
        public BusinessModeRules(IGameModeConfiguration configuration)
        {
            if (configuration == null) throw new ContentException("business requires an explicitly authored game-mode configuration");
            ValuesEnabled = Read("business.values.enabled"); MoneyEnabled = Read("business.money.enabled");
            ProductionIncomeEnabled = Read("business.production_income.enabled");
            ConstructionCostsEnabled = Read("business.construction_costs.enabled");
            ManufacturingCostsEnabled = Read("business.manufacturing_costs.enabled");
            RunningCostsEnabled = Read("business.running_costs.enabled");
            TradingEnabled = Read("business.trading.enabled"); TycoonEnabled = Read("business.tycoon.enabled");
            if (!MoneyEnabled && (ProductionIncomeEnabled || ConstructionCostsEnabled || ManufacturingCostsEnabled || RunningCostsEnabled || TradingEnabled || TycoonEnabled))
                throw new ContentException("enabled monetary capabilities require business.money.enabled");
            if (!ValuesEnabled && (ProductionIncomeEnabled || TycoonEnabled))
                throw new ContentException("production income and advanced valuation require business.values.enabled");

            bool Read(string id) => ReadProperty(configuration, id);
        }
        public bool CostEnabled(MonetaryCostCategory category)
        {
            switch (category)
            {
                case MonetaryCostCategory.Construction: return ConstructionCostsEnabled;
                case MonetaryCostCategory.Manufacturing: return ManufacturingCostsEnabled;
                case MonetaryCostCategory.Running: return RunningCostsEnabled;
                default: throw new ContentException("a monetary cost requires an explicit supported category");
            }
        }
        private static string Property(MonetaryCostCategory category)
        {
            switch (category)
            {
                case MonetaryCostCategory.Construction: return "business.construction_costs.enabled";
                case MonetaryCostCategory.Manufacturing: return "business.manufacturing_costs.enabled";
                case MonetaryCostCategory.Running: return "business.running_costs.enabled";
                default: throw new ContentException("a monetary cost requires an explicit supported category");
            }
        }
        private static bool ReadProperty(IGameModeConfiguration configuration, string id)
        {
            if (configuration == null || !configuration.TryGet(new Code(id), out GameModeValue value) || value.Kind != GameModeValueKind.Boolean)
                throw new ContentException("monetary content requires an explicitly authored Boolean property '" + id + "'");
            foreach (GameModePropertyView property in configuration.Properties)
                if (property.Property.Id == id && property.Capability.Id != TycoonModule.ModuleId)
                    throw new ContentException("business property '" + id + "' has an incorrect capability owner");
            return value.Boolean;
        }
        /// <summary>One gate for work, recipes and views. Disabled costs require no accounts;
        /// native economic authoring still requires its declaring TycoonModule.</summary>
        public static MonetaryCostDecision EvaluateCost(World world, MonetaryCostCategory category, Fix amount)
        {
            if (world == null) throw new ArgumentNullException(nameof(world));
            string property = Property(category);
            if (amount < Fix.Zero) throw new ContentException("monetary costs cannot be negative");
            if (amount == Fix.Zero) return new MonetaryCostDecision(category, amount, MonetaryCostApplicability.NotAuthored);
            bool enabled = world.Services.TryGet<IBusinessModeRules>(out var rules) ? rules.CostEnabled(category) : ReadProperty(world.GameMode, property);
            if (!enabled) return new MonetaryCostDecision(category, amount, MonetaryCostApplicability.DisabledByMode);
            if (!ReadProperty(world.GameMode, "business.money.enabled")) throw new ContentException("enabled monetary costs require enabled money");
            return new MonetaryCostDecision(category, amount, world.Services.Has<IEconomy>()
                ? MonetaryCostApplicability.Enabled : MonetaryCostApplicability.UnavailableCapability);
        }
        internal static bool CanPay(IEconomy economy, PlayerId company, Fix amount)
        {
            Fix before = economy.Cash(company); bool can = economy.CanAfford(company, amount);
            if (economy.Cash(company) != before) throw new InvalidOperationException("economy mutated monetary cost admission");
            return can;
        }
        public static readonly Code UnavailableCostCapability = new Code("business.cost-capability-unavailable");
        public static readonly Code LedgerPurchase = new Code("ledger.purchase");
        public ResourceTransferStatus CheckTrading() => !TradingEnabled ? ResourceTransferStatus.TradingDisabled
            : MoneyEnabled ? ResourceTransferStatus.Ready : ResourceTransferStatus.UnavailableCapability;
    }
}
