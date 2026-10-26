using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Views
{
    public sealed class BusinessView
    {
        private readonly World _world;
        private readonly IBusinessAccounts _accounts;
        private readonly IResourceValuation _valuation;
        private readonly IMonetaryBudgets _budgets;
        private readonly PlayerId _viewer;
        private readonly IBusinessModeRules _rules;
        private readonly bool _hasGenerationIncome, _hasPurchasing;
        internal BusinessView(World world, PlayerId viewer)
        { _world = world; world.Services.TryGet(out _accounts); world.Services.TryGet(out _valuation); world.Services.TryGet(out _budgets); _viewer = viewer;
          _rules = world.Services.Get<IBusinessModeRules>();
          _hasGenerationIncome = world.Services.Has<IResourceProductionSettlement>();
          _hasPurchasing = world.Services.Has<IResourcePurchaseSettlement>(); }
        private T Read<T>(object provider, Func<T> read)
        {
            _world.RequireHealthyUpdate();
            return provider == null ? read() : EntityObservation.ReadProvider(_world, provider, null, read);
        }
        public bool HasAccounts => Read<bool>(null, () => _accounts != null);
        public bool HasValuation => Read<bool>(null, () => _valuation != null);
        public IBusinessModeRules Rules => Read(_rules, () => _rules);
        public bool HasGenerationIncome => Read<bool>(null, () => _hasGenerationIncome);
        public bool HasPurchasing => Read<bool>(null, () => _hasPurchasing);
        public bool HasBudgets => Read<bool>(null, () => _budgets != null);
        public Code CopyOperatingCosts(EntityId target, IList<EntityOperatingCostView> destination)
            => _world.Services.Get<IBusinessOperatingCosts>().CopyCosts(target, _viewer, destination);
        public Code CopyBudgets(PlayerId player, IList<MonetaryBudgetReceipt> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear(); _world.RequireHealthyUpdate();
            if (_budgets == null) return new Code("business.no-account");
            if (_viewer.IsValid && _viewer != player) return Codes.NotOwned;
            if (Read(_accounts, () => _accounts.ReadAccount(player, out _)) != Code.None) return Codes.InvalidArgument;
            var staged = Read(_budgets, () =>
            {
                var records = new List<MonetaryBudgetReceipt>();
                _budgets.InspectBudgets(items => { foreach (var item in items) if (item.Company == player) records.Add(item); });
                return records;
            });
            foreach (var record in staged) destination.Add(record);
            return Code.None;
        }
        public string Policy => Read(_valuation, () => _valuation?.Policy);
        public BusinessMode Mode => Read(_valuation, () => _valuation?.Mode ?? BusinessMode.Unavailable);
        public UnitId Currency => _valuation == null
            ? Read(_accounts, () => _accounts?.Currency ?? default)
            : Read(_valuation, () => _valuation.Currency);
        public ResourceValueQuote Quote(ResourceId resource, Fix nativeQuantity) => Read(_valuation, () => _valuation == null
            ? new ResourceValueQuote(ResourceValueStatus.Unavailable, null, BusinessMode.Unavailable, resource, default, default, nativeQuantity, Fix.Zero, Fix.Zero)
            : _valuation.Quote(resource, nativeQuantity));
        public Code ReadAccount(PlayerId player, out BusinessAccountSnapshot account)
        {
            account = default;
            _world.RequireHealthyUpdate();
            if (_accounts == null) return new Code("business.no-account");
            if (_viewer.IsValid && _viewer != player) return Codes.NotOwned;
            var result = Read(_accounts, () =>
            {
                Code refusal = _accounts.ReadAccount(player, out var value); return (refusal, value);
            });
            account = result.Item2; return result.Item1;
        }
        public Code CopySpending(PlayerId company, IList<BusinessExpenseSnapshot> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear(); _world.RequireHealthyUpdate();
            if (_accounts == null) return new Code("business.no-account");
            if (_viewer.IsValid && _viewer != company) return Codes.NotOwned;
            var staged = Read(_accounts, () =>
            {
                var rows = new List<BusinessExpenseSnapshot>();
                Code refusal = _accounts.CopySpending(company, rows); return (refusal, rows);
            });
            if (staged.Item1.IsNone) foreach (var row in staged.Item2) destination.Add(row);
            return staged.Item1;
        }
        public Code ReadActionCharge(PlayerId company, Code action, string target, out ContentActionChargeView facts)
        {
            facts = default; _world.RequireHealthyUpdate();
            if (_viewer.IsValid && _viewer != company) return Codes.NotOwned;
            if (!_world.ViewFor(_viewer).CanSeeContent(_world.Content.Find<Beep.SimEngine.Content.IContentDefinition>(target))) return Codes.UnknownDef;
            return _world.Services.Get<IBusinessContentCharges>().Read(company, action, target, out facts);
        }
        public Code ReadSales(PlayerId company, out BusinessSalesSnapshot snapshot)
        {
            snapshot = default; _world.RequireHealthyUpdate();
            if (_viewer.IsValid && _viewer != company) return Codes.NotOwned;
            return _world.Services.Get<IBusinessSales>().ReadSales(company, out snapshot);
        }
        public Code CopySalesPolicies(EntityId target, IList<ResourceSaleView> destination)
            => _world.Services.Get<IBusinessSales>().CopyPolicies(target, _viewer, destination);
    }
}
