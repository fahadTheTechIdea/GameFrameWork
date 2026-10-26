using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Only selected Tycoon sales execute; no stock or money owned by this interpreter.</summary>
    internal sealed class ResourceSalesSystem : GameSystem, IBusinessSales, ICompositionValidator
    {
        private readonly World _world;
        private readonly BusinessAccounts _accounts;
        private readonly IReadOnlyList<IResourceSaleDefinition> _definitions;
        private readonly IBusinessSalesReportDefinition _report;
        private readonly Dictionary<string, Schedule> _schedules = new Dictionary<string, Schedule>(StringComparer.Ordinal);
        public override string Id => "sim.tycoon-resource-sales";
        public override int StateSchemaVersion => 1;
        internal ResourceSalesSystem(World world, BusinessAccounts accounts)
        {
            _world = world; _accounts = accounts;
            var definitions = new List<IResourceSaleDefinition>(world.Content.All<IResourceSaleDefinition>());
            definitions.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id)); _definitions = definitions.AsReadOnly();
            var reports = world.Content.All<IBusinessSalesReportDefinition>();
            if (definitions.Count != 0 && reports.Count != 1 || definitions.Count == 0 && reports.Count != 0)
                throw new ContentException("sales require one explicit reporting policy, absent when no sale definitions");
            _report = reports.Count == 1 ? reports[0] : null;
            var byId = new Dictionary<string, IResourceSaleDefinition>(StringComparer.Ordinal);
            foreach (var definition in definitions) byId.Add(definition.Id, definition);
            if (_report != null && accounts != null) accounts.ConfigureSales(world, byId, _report);
        }
        public void ValidateComposition(World world)
        {
            if (!ReferenceEquals(world, _world)) throw new ContentException("foreign sales session");
            var targets = new HashSet<string>(StringComparer.Ordinal);
            foreach (var d in _definitions) EntityObservation.ReadProvider(world, d, null, () => {
                if (!world.Content.Owns(d) || !ReferenceEquals(world.Content.Find<IResourceSaleDefinition>(d.Id), d)
                    || !world.Content.Owns(d.Target) || !d.Target.HasCapability<IInventory>() || d.Prices == null || d.Prices.IsEmpty
                    || d.Reason.IsNone || d.Eligibility == null || !world.Content.Owns(d.Eligibility) || !targets.Add(d.Target.Id))
                    throw new ContentException("sale requires actual unique target/price/eligibility metadata");
                ValidateSchedule(d.Period, d.Offset);
                _schedules[d.Id] = Schedule.Every(world.Time, d.Period, world.Time.ExactTicks(d.Offset));
                d.Eligibility.ValidateTarget(d.Target);
                foreach (var line in d.Prices.Lines)
                    if (!line.Amount.IsPositive || world.Content.Resource(line.Resource) == null) throw new ContentException("sale requires actual resource and positive price");
                if (world.Services.Get<IBusinessModeRules>().TradingEnabled && _accounts == null) throw new ContentException("enabled sale requires actual account");
                return true;
            });
            if (_report != null) EntityObservation.ReadProvider(world, _report, null, () => {
                if (!world.Content.Owns(_report) || !ReferenceEquals(world.Content.Find<IBusinessSalesReportDefinition>(_report.Id), _report))
                    throw new ContentException("sales reporting requires actual immutable metadata");
                ValidateSchedule(_report.Period, _report.Offset); return true;
            });
            void ValidateSchedule(GameDuration period, GameDuration offset)
            {
                long p = world.Time.ExactTicks(period), o = world.Time.ExactTicks(offset);
                if (p <= 0 || o < 0 || o >= p) throw new ContentException("sale/report schedule requires positive period and offset within period");
            }
        }
        public override void Update(World world, TickInfo tick)
        {
            if (_definitions.Count == 0 || !world.Services.Get<IBusinessModeRules>().TradingEnabled) return;
            IResourceTransaction resources = world.Services.Get<IResourceTransaction>();
            foreach (var d in _definitions)
            {
                if (!_schedules[d.Id].IsDue(tick.Tick)) continue;
                var metadata = EntityObservation.ReadProvider(world, d, null, () =>
                    (world.Time.ExactTicks(d.Period), world.Time.ExactTicks(d.Offset), d.Target, d.Eligibility, d.Prices, d.Id));
                long p = metadata.Item1, o = metadata.Item2;
                if (tick.Tick < o || (tick.Tick - o) % p != 0) continue;
                foreach (Entity entity in world.Entities.All)
                {
                    if (!entity.Alive || !entity.Owner.IsValid || !ReferenceEquals(entity.Def, metadata.Item3)) continue;
                    if (!EntityObservation.ReadProvider(world, metadata.Item4, entity, () => metadata.Item4.Check(world, entity), observeTargetState: true).IsNone) continue;
                    IInventory inventory = entity.Get<IInventory>();
                    foreach (var price in metadata.Item5.Lines)
                    {
                        Fix quantity = EntityObservation.ReadProvider(world, inventory, entity, () => inventory.Available(price.Resource), observeTargetState: true);
                        if (!quantity.IsPositive) continue;
                        var intent = new ResourceInventoryIntent(new ReservationOwner(Id, entity.Id.Value), entity.Owner, entity.Id,
                            new Bill(new[] { new ResourceAmount(price.Resource, quantity) }), ResourceFlowKind.Sold, new Code(metadata.Item6));
                        var result = resources.CommitInventory(intent);
                        if (!result.Committed) throw new InvalidOperationException("authored sale refused: " + result.Status + "/" + result.Blocker);
                    }
                }
            }
            _accounts?.CloseSalesPeriod(tick.Tick);
        }
        public Code ReadSales(PlayerId company, out BusinessSalesSnapshot snapshot)
        { snapshot = default; _world.RequireHealthyUpdate(); return _accounts == null ? new Code("business.no-account") : _accounts.ReadSales(company, out snapshot); }
        public Code CopyPolicies(EntityId target, PlayerId viewer, IList<ResourceSaleView> destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            destination.Clear(); _world.RequireHealthyUpdate(); Entity entity = _world.Entities.Get(target);
            if (!_world.ViewFor(viewer).CanObserveOwned(entity)) return Codes.NotFound;
            var staged = new List<ResourceSaleView>();
            foreach (var d in _definitions)
            {
                var metadata = EntityObservation.ReadProvider(_world, d, entity, () => (d.Target, d.Eligibility, new ResourceSaleView(d, Code.None)));
                if (!ReferenceEquals(metadata.Item1, entity.Def)) continue;
                Code blocker = _world.Services.Get<IBusinessModeRules>().TradingEnabled
                    ? EntityObservation.ReadProvider(_world, metadata.Item2, entity, () => metadata.Item2.Check(_world, entity), observeTargetState: true)
                    : new Code("business.trading-disabled");
                staged.Add(EntityObservation.ReadProvider(_world, d, entity, () => new ResourceSaleView(d, blocker)));
            }
            foreach (var item in staged) destination.Add(item); return Code.None;
        }
    }
}
