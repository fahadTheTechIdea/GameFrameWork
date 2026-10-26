using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    // Financial history belongs to the existing account owner, never the domain or another ledger.
    internal sealed partial class BusinessAccounts : IResourceSaleSettlement
    {
        private World _salesWorld;
        private IReadOnlyDictionary<string, IResourceSaleDefinition> _saleDefinitions;
        private IBusinessSalesReportDefinition _saleReport;
        private Fix[] _saleRevenue, _saleCurrent, _salePrevious;
        private long _lastSaleClosing = -1;
        private long _salesReportPeriod, _salesReportOffset;
        internal void ConfigureSales(World world, IReadOnlyDictionary<string, IResourceSaleDefinition> definitions, IBusinessSalesReportDefinition report)
        {
            if (_salesWorld != null || world == null || definitions == null || report == null)
                throw new ContentException("sales account configures once with actual reporting metadata");
            _salesWorld = world; _saleDefinitions = definitions; _saleReport = report;
            var schedule = EntityObservation.ReadProvider(world, report, null,
                () => (world.Time.ExactTicks(report.Period), world.Time.ExactTicks(report.Offset)));
            _salesReportPeriod = schedule.Item1; _salesReportOffset = schedule.Item2;
            if (_salesReportPeriod <= 0 || _salesReportOffset < 0 || _salesReportOffset >= _salesReportPeriod)
                throw new ContentException("sales report requires exact positive period and bounded offset");
            _saleRevenue = new Fix[_balances.Length]; _saleCurrent = new Fix[_balances.Length]; _salePrevious = new Fix[_balances.Length];
        }
        internal Code ReadSales(PlayerId company, out BusinessSalesSnapshot snapshot)
        {
            snapshot = default;
            if (_saleReport == null) return new Code("business.no-sales");
            if (!IsAccount(company)) return Codes.InvalidArgument;
            int seat = company.Value;
            snapshot = new BusinessSalesSnapshot(_saleReport.Id, Currency, _saleRevenue[seat], _saleCurrent[seat], _salePrevious[seat]);
            return Code.None;
        }
        public Code PrepareSale(ResourceInventoryIntent intent, out IResourceSaleCommit commit)
        {
            commit = null;
            if (_salesWorld == null || _trading == null || _trading.CheckTrading() != ResourceTransferStatus.Ready)
                return new Code("business.trading-disabled");
            if (intent.Kind != ResourceFlowKind.Sold || !IsAccount(intent.Company) || intent.Quantities == null || intent.Quantities.IsEmpty
                || !_saleDefinitions.TryGetValue(intent.Cause.Id, out var definition)) return Codes.InvalidArgument;
            Entity entity = _salesWorld.Entities.Get(intent.Endpoint);
            var metadata = EntityObservation.ReadProvider(_salesWorld, definition, entity,
                () => (definition.Target, definition.Prices, definition.Eligibility));
            if (entity == null || entity.Owner != intent.Company || !ReferenceEquals(entity.Def, metadata.Item1)) return Codes.NotOwned;
            Code refusal = EntityObservation.ReadProvider(_salesWorld, metadata.Item3, entity,
                () => metadata.Item3.Check(_salesWorld, entity), observeTargetState: true);
            if (!refusal.IsNone) return refusal;
            int seat = intent.Company.Value;
            try
            {
                var facts = new List<BusinessSaleFact>(); Fix payout = Fix.Zero;
                foreach (var line in intent.Quantities.Lines)
                {
                    Fix price = metadata.Item2.AmountOf(line.Resource);
                    if (!price.IsPositive || !line.Amount.IsPositive) return Codes.InvalidArgument;
                    Fix value = line.Amount * price; payout += value;
                    facts.Add(new BusinessSaleFact(intent.Company, entity.Id, line.Resource, line.Amount, value, Currency));
                }
                Fix balance = _balances[seat], revenue = _saleRevenue[seat], current = _saleCurrent[seat];
                commit = new SaleCommit(this, seat, balance, balance + payout, revenue, revenue + payout, current, current + payout, facts);
                return Code.None;
            }
            catch (OverflowException) { return new Code("business.sale-range"); }
        }
        private sealed class SaleCommit : IResourceSaleCommit
        {
            private readonly BusinessAccounts _owner;
            private readonly int _seat;
            private readonly Fix _balance, _after, _revenue, _revenueAfter, _current, _currentAfter;
            private readonly IReadOnlyList<BusinessSaleFact> _facts;
            private bool _committed;
            internal SaleCommit(BusinessAccounts owner, int seat, Fix balance, Fix after, Fix revenue, Fix revenueAfter, Fix current, Fix currentAfter, IReadOnlyList<BusinessSaleFact> facts)
            { _owner = owner; _seat = seat; _balance = balance; _after = after; _revenue = revenue; _revenueAfter = revenueAfter; _current = current; _currentAfter = currentAfter; _facts = facts; }
            public void Commit()
            {
                if (_committed || _owner._balances[_seat] != _balance || _owner._saleRevenue[_seat] != _revenue || _owner._saleCurrent[_seat] != _current)
                    throw new InvalidOperationException("sale settlement duplicate or changed after inventory admission");
                _owner._balances[_seat] = _after; _owner._saleRevenue[_seat] = _revenueAfter; _owner._saleCurrent[_seat] = _currentAfter; _committed = true;
                foreach (var fact in _facts) _owner._salesWorld.Events.Publish(fact);
            }
        }
        internal void CloseSalesPeriod(long tick)
        {
            if (_saleReport == null) return;
            long period = _salesReportPeriod, offset = _salesReportOffset;
            if (tick < offset || (tick - offset) % period != 0 || tick == _lastSaleClosing) return;
            for (int i = 0; i < _balances.Length; i++) { _salePrevious[i] = _saleCurrent[i]; _saleCurrent[i] = Fix.Zero; }
            _lastSaleClosing = tick;
        }
        private void SyncSales(IStateSync sync)
        {
            string policy = _saleReport?.Id; sync.String(ref policy);
            if (policy != _saleReport?.Id) throw new CorruptStateException("saved sales report differs from selected policy");
            long closing = _lastSaleClosing; sync.Long(ref closing);
            if (closing < -1 || policy == null && closing != -1 || _salesWorld != null && closing > _salesWorld.Tick)
                throw new CorruptStateException("invalid sales closing boundary");
            if (closing >= 0)
            {
                long period = _salesReportPeriod, offset = _salesReportOffset;
                if (closing < offset || (closing - offset) % period != 0) throw new CorruptStateException("saved sales closing is not an authored boundary");
            }
            if (sync.Reading) _lastSaleClosing = closing;
            if (policy == null) return;
            for (int i = 0; i < _balances.Length; i++)
            {
                Fix total = _saleRevenue[i], current = _saleCurrent[i], previous = _salePrevious[i];
                sync.Fix(ref total); sync.Fix(ref current); sync.Fix(ref previous);
                if (total < Fix.Zero || current < Fix.Zero || previous < Fix.Zero || current > total || previous > total - current)
                    throw new CorruptStateException("invalid saved sales revenue");
                if (sync.Reading) { _saleRevenue[i] = total; _saleCurrent[i] = current; _salePrevious[i] = previous; }
            }
        }
    }
}
