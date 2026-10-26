using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Views
{
    public readonly struct LogisticsDemandView
    {
        public LogisticsDemandId Source { get; }
        public Code Port { get; }
        public ResourceEndpointId Endpoint { get; }
        public EntityId Destination { get; }
        public ResourceId Resource { get; }
        public Fix Quantity { get; }
        public LogisticsDemandSettings Settings { get; }
        public bool Replenishing { get; }
        public int Priority { get; }
        public long CreatedTick { get; }
        public long AgeTicks { get; }
        public long LastDispatchTick { get; }
        public LogisticsDemandSourceCheck Check { get; }
        internal LogisticsDemandView(LogisticsService.Demand demand, long tick, LogisticsDemandSourceCheck check)
        { Source = demand.Source; Destination = demand.Store; Port = demand.Port; Endpoint = demand.Endpoint; Resource = demand.Resource; Settings = demand.Settings; Quantity = demand.Settings.Desired;
            Replenishing = demand.Replenishing; Priority = demand.Settings.Priority; CreatedTick = demand.CreatedTick; AgeTicks = checked(tick - demand.CreatedTick); LastDispatchTick = demand.LastDispatchTick; Check = check; }
    }
    public readonly struct RouteView
    {
        public int Id { get; }
        public EntityId From { get; }
        public EntityId To { get; }
        public Code SourcePort { get; }
        public Code DestinationPort { get; }
        public ResourceId Resource { get; }
        public LogisticsRouteSettings Settings { get; }
        public long CreatedTick { get; }
        public long AgeTicks { get; }
        public long LastDispatchTick { get; }
        internal RouteView(LogisticsService.Route route, long tick)
        { Id = route.Id; From = route.From; To = route.To; SourcePort = route.SourcePort; DestinationPort = route.DestinationPort; Resource = route.Resource; Settings = route.Settings;
            CreatedTick = route.CreatedTick; AgeTicks = checked(tick - route.CreatedTick); LastDispatchTick = route.LastDispatchTick; }
    }

    public readonly struct CargoReturnView
    {
        public ResourceId Resource { get; }
        public Fix Stock { get; }
        public Fix Available { get; }
        public HaulPreview Return { get; }
        internal CargoReturnView(ResourceAmount stock, Fix available, HaulPreview preview)
        { Resource = stock.Resource; Stock = stock.Amount; Available = available; Return = preview; }
    }

    public sealed class LogisticsView
    {
        private readonly World _world;
        private readonly PlayerId _viewer;
        internal LogisticsView(World world, PlayerId viewer) { _world = world; _viewer = viewer; }
        private bool Sees(PlayerId owner) => _viewer == PlayerId.None || _world.Players.IsSeat(_viewer) && owner == _viewer;
        private bool ObservesReference(EntityId id) => !id.IsValid || _world.Entities.Get(id) is Entity entity && entity.Alive && _world.ViewFor(_viewer).CanSee(entity);
        private bool Discloses(CargoDispositionSnapshot cargo)
            => _world.ViewFor(_viewer).CanSeeResources(cargo.Retained.Lines)
                && _world.ViewFor(_viewer).CanSeeResources(cargo.Committed.Lines)
                && _world.ViewFor(_viewer).CanSeeResources(cargo.Unassigned.Lines);
        private bool Discloses(HaulPreview haul)
        {
            if (!ResourceLotSelection.Visible(_world, _viewer, haul.Selection)) return false;
            if (!ObservesReference(haul.Source) || !ObservesReference(haul.Destination) || !ObservesReference(haul.Carrier)) return false;
            if (haul.Resource.IsValid && !_world.ViewFor(_viewer).CanSeeContent(_world.Content.Resource(haul.Resource))) return false;
            if (!HaulTripPolicyContracts.CanDisclose(_world, _viewer, haul.TripBudget)) return false;
            if (!RecurringPlanContracts.CanDisclose(_world, _viewer, haul.ServiceInputs)) return false;
            if (haul.LoadPolicyInputs != null)
                foreach (var settings in haul.LoadPolicyInputs)
                    if (!HaulLoadPolicyContracts.CanDisclose(_world, _viewer, settings)) return false;
            if (haul.HaulDemand.IsValid)
            {
                Entity carrier = _world.Entities.Get(haul.Carrier);
                if (carrier == null || !_world.Services.Get<ILogisticsDemandSources>()
                    .CheckSourceDisclosure(_viewer, haul.HaulDemand, carrier.Owner, haul.Destination).IsNone) return false;
            }
            return !haul.Destination.IsValid || !haul.Resource.IsValid || _world.Services.Get<ILogisticsDemandSources>()
                .CheckDemandDisclosure(_viewer, haul.Destination, haul.Resource,
                    haul.DestinationEndpoint.IsValid ? haul.DestinationEndpoint : (ResourceEndpointId?)null).IsNone;
        }
        private bool Discloses(LogisticsDispatchOffer offer)
        {
            if (!ResourceLotSelection.Visible(_world, _viewer, offer.Selection)) return false;
            if (!Sees(offer.Company) || !ObservesReference(offer.Destination) || !ObservesReference(offer.RecoveryOrigin) || !Discloses(offer.Haul)) return false;
            if (!_world.ViewFor(_viewer).CanSeeContent(_world.Content.Resource(offer.Resource)) || offer.Destination.IsValid &&
                !_world.Services.Get<ILogisticsDemandSources>().CheckDemandDisclosure(_viewer, offer.Destination, offer.Resource,
                    offer.DestinationEndpoint.IsValid ? offer.DestinationEndpoint : (ResourceEndpointId?)null).IsNone) return false;
            var route = offer.Route == 0 ? null : _world.Services.Get<LogisticsService>().FindRoute(offer.Route);
            return offer.Route == 0 || route.HasValue && ObservesReference(route.Value.From) && ObservesReference(route.Value.To);
        }
        private bool Discloses(LogisticsOperationalSnapshot value)
        {
            if (value == null) return true;
            foreach (var offer in value.Offers) if (!Discloses(offer)) return false;
            foreach (var shipment in value.Shipments)
                if (!ObservesReference(shipment.Source) || !ObservesReference(shipment.Worker)) return false;
            foreach (var carrier in value.Carriers)
                if (!ObservesReference(carrier.Worker) || !Discloses(carrier.Cargo) || carrier.Return.HasValue && !Discloses(carrier.Return.Value)) return false;
            return true;
        }
        public Code ReadDiagnostics(EntityId target, ResourceId resource, out LogisticsOperationalSnapshot value,
            Code port = default, ResourceEndpointId? endpoint = null)
        {
            value = null; _world.RequireHealthyUpdate(); Entity entity = _world.Entities.Get(target);
            if (entity == null || !entity.Alive || !_world.ViewFor(_viewer).CanSee(entity)) return Codes.NotFound;
            if (!Sees(entity.Owner)) return Codes.NotOwned;
            if (!_world.Services.Get<ILogisticsDemandSources>().CheckDemandDisclosure(_viewer, target, resource, endpoint).IsNone) return Codes.NotFound;
            Code refusal = _world.Services.Get<ILogisticsDiagnostics>().ReadDiagnostics(entity.Owner, target, resource, out var facts, port, endpoint);
            _world.RequireHealthyUpdate(); if (!Discloses(facts)) return Codes.NotFound;
            value = facts; return refusal;
        }
        public Code ReadIncomingThroughput(EntityId target, ResourceId resource, long fromStep, long throughStep,
            out LogisticsThroughputSnapshot value, Code port = default, ResourceEndpointId? endpoint = null)
        {
            value = null; _world.RequireHealthyUpdate(); Entity entity = _world.Entities.Get(target);
            if (entity == null || !entity.Alive || !_world.ViewFor(_viewer).CanSee(entity)) return Codes.NotFound;
            if (!Sees(entity.Owner)) return Codes.NotOwned;
            Code refusal = _world.Services.Get<ILogisticsDiagnostics>().ReadIncomingThroughput(entity.Owner, target, resource, fromStep, throughStep, out var facts, port, endpoint);
            _world.RequireHealthyUpdate(); value = facts; return refusal;
        }
        public void CopyQueue(IList<LogisticsDispatchOffer> destination)
        {
            destination.Clear(); _world.RequireHealthyUpdate();
            var queue = _world.Services.Get<ILogisticsDispatchQueue>();
            var facts = EntityObservation.ReadProvider(_world, queue, null, () => {
                var rows = new List<LogisticsDispatchOffer>(); queue.CopyQueue(_viewer, rows); return rows;
            });
            _world.RequireHealthyUpdate();
            foreach (var fact in facts) if (Discloses(fact)) destination.Add(fact);
        }
        public Code ReadOutletPolicy(EntityId source, out OutletPolicySettings settings, Code port = default)
        {
            settings = null; _world.RequireHealthyUpdate(); Entity entity = _world.Entities.Get(source); if (entity == null) return Codes.NotFound;
            if (_viewer != PlayerId.None && !_world.Players.IsSeat(_viewer)) return Codes.NotFound;
            if (!_world.ViewFor(_viewer).CanObserveOwned(entity)) return Codes.NotFound;
            IOutletPolicy policy = entity.Get<IOutletPolicy>(); if (policy == null) return LogisticsPolicyCodes.NoPolicy;
            var facts = LogisticsPolicyRules.Read(_world, entity, port);
            _world.RequireHealthyUpdate(); settings = facts; return settings == null ? LogisticsPolicyCodes.NoPolicy : Code.None;
        }
        public Code PreviewOutletPolicy(EntityId source, OutletPolicyChange change, Code port = default)
        {
            _world.RequireHealthyUpdate();
            Entity entity = _world.Entities.Get(source);
            Code refusal = LogisticsPolicyRules.CheckChange(_world, _viewer == PlayerId.None ? entity?.Owner ?? _viewer : _viewer, entity, change, out _, port);
            _world.RequireHealthyUpdate(); return refusal;
        }
        // Grouped controls use the same selected port and original complete-set owner, not named fleets.
        public OutletPolicySelectionPreview PreviewOutletPolicyGroup(IReadOnlyList<EntityId> sources, OutletPolicyChange change, Code port = default)
            => LogisticsPolicyRules.PreviewGroup(_world, _viewer, sources, change, port);
        public void CopyDemands(IList<LogisticsDemandView> destination)
        {
            destination.Clear(); _world.RequireHealthyUpdate();
            LogisticsService logistics = _world.Services.Get<LogisticsService>();
            var facts = EntityObservation.ReadProvider(_world, logistics, null, () => {
                var rows = new List<LogisticsDemandView>();
                foreach (LogisticsService.Demand demand in logistics.Demands)
                    if (Sees(demand.Owner) && _world.ViewFor(_viewer).CanSeeContent(_world.Content.Resource(demand.Resource)) &&
                        logistics.CheckSourceDisclosure(_viewer, demand.Source, demand.Owner, demand.Store).IsNone && ResourceLotSelection.Visible(_world, _viewer, demand.Settings.Selection))
                        rows.Add(new LogisticsDemandView(demand, _world.Tick, logistics.CheckSource(demand)));
                return rows;
            });
            _world.RequireHealthyUpdate();
            foreach (var fact in facts) destination.Add(fact);
        }

        public Code CopyCargoReturns(EntityId worker, IList<CargoReturnView> destination)
        {
            destination.Clear(); _world.RequireHealthyUpdate(); Entity entity = _world.Entities.Get(worker);
            if (!_world.ViewFor(_viewer).CanObserveOwned(entity)) return Codes.NotFound;
            ITransporter transporter = entity.Get<ITransporter>();
            if (transporter == null) return Codes.NotAllowed;
            var facts = EntityObservation.ReadProvider(_world, transporter, entity, () => {
                IInventory cargo = TransporterContracts.ReadCargo(_world, entity);
                return EntityObservation.ReadProvider(_world, cargo, cargo.Entity, () => {
                    var stock = cargo.Stock;
                    if (!_world.ViewFor(_viewer).CanSeeResources(stock)) return null;
                    var rows = new List<CargoReturnView>();
                    IHaulExecution hauling = _world.Services.Get<IHaulExecution>();
                    foreach (ResourceAmount line in stock)
                    {
                        HaulPreview preview = hauling.PreviewReturnCargo(worker, line.Resource);
                        if (!Discloses(preview)) return null;
                        rows.Add(new CargoReturnView(line, cargo.Available(line.Resource), preview));
                    }
                    return rows;
                });
            });
            _world.RequireHealthyUpdate();
            if (facts == null) return Codes.NotFound;
            foreach (var fact in facts) destination.Add(fact);
            return Code.None;
        }

        public Code ReadCargoDisposition(EntityId worker, out CargoDispositionSnapshot value)
        {
            value = null; _world.RequireHealthyUpdate(); Entity entity = _world.Entities.Get(worker);
            if (!_world.ViewFor(_viewer).CanObserveOwned(entity)) return Codes.NotFound;
            if (entity.Get<ITransporter>() == null) return Codes.NotAllowed;
            var facts = _world.Services.Get<IHaulExecution>().ReadCargoDisposition(worker);
            _world.RequireHealthyUpdate();
            if (!Discloses(facts)) return Codes.NotFound;
            value = facts; return Code.None;
        }

        public Code ReadStockPolicy(EntityId store, ResourceId resource, out LogisticsDemandSettings settings, Code port = default, ResourceEndpointId? endpoint = null)
        {
            settings = null; _world.RequireHealthyUpdate(); Entity entity = _world.Entities.Get(store); if (entity == null) return Codes.NotFound;
            if (_viewer != PlayerId.None && !_world.Players.IsSeat(_viewer)) return Codes.NotFound;
            if (!_world.ViewFor(_viewer).CanObserveOwned(entity)) return Codes.NotFound;
            bool found = _world.Services.Get<ILogisticsStockPolicies>().TryReadStockPolicy(store, resource, out var facts, port, endpoint);
            if (found && !ResourceLotSelection.Visible(_world, _viewer, facts.Selection)) return Codes.NotFound;
            _world.RequireHealthyUpdate(); settings = facts;
            return found ? Code.None : LogisticsPolicyCodes.NoStockPolicy;
        }
        public Code PreviewStockPolicy(EntityId store, ResourceId resource, LogisticsDemandChange change, Code port = default, ResourceEndpointId? endpoint = null)
        {
            _world.RequireHealthyUpdate();
            Entity entity = _world.Entities.Get(store);
            Code refusal = Beep.SimEngine.Commands.ConfigureStockPolicyHandler.Check(_world, _viewer == PlayerId.None ? entity?.Owner ?? _viewer : _viewer, store, resource, change, port, endpoint);
            _world.RequireHealthyUpdate(); return refusal;
        }
        public Code ReadHaulLoadPolicy(EntityId worker, out HaulLoadPolicySnapshot value)
        {
            value = null; _world.RequireHealthyUpdate(); Entity entity = _world.Entities.Get(worker); if (entity == null) return Codes.NotFound;
            if (!_world.ViewFor(_viewer).CanObserveOwned(entity)) return Codes.NotFound;
            var profile = HaulLoadPolicyContracts.Read(_world, entity); if (profile == null) return HaulLoadPolicyCodes.NotPolicy;
            if (!HaulLoadPolicyContracts.CanDisclose(_world, _viewer, profile)) return Codes.NotFound;
            var facts = EntityObservation.Read(_world, entity, () => new HaulLoadPolicySnapshot(profile, _world.Time));
            _world.RequireHealthyUpdate(); value = facts; return Code.None;
        }
        public Code PreviewHaulLoadPolicy(EntityId worker, HaulLoadSettings settings)
        {
            _world.RequireHealthyUpdate();
            Entity entity = _world.Entities.Get(worker);
            Code refusal = HaulLoadPolicyContracts.CheckConfigure(_world, _viewer == PlayerId.None ? entity?.Owner ?? _viewer : _viewer, entity, settings);
            _world.RequireHealthyUpdate(); return refusal;
        }

        public Code ReadHaulTripPolicy(EntityId worker, out HaulTripSettings value)
        {
            value = null; _world.RequireHealthyUpdate(); Entity entity = _world.Entities.Get(worker); if (entity == null) return Codes.NotFound;
            if (!_world.ViewFor(_viewer).CanObserveOwned(entity)) return Codes.NotFound;
            var facts = HaulTripPolicyContracts.Read(_world, entity);
            _world.RequireHealthyUpdate();
            if (!HaulTripPolicyContracts.CanDisclose(_world, _viewer, facts)) return Codes.NotFound;
            value = facts;
            return value == null ? HaulTripPolicyCodes.NotPolicy : Code.None;
        }
        public Code PreviewHaulTripPolicy(EntityId worker, HaulTripSettings settings)
        {
            _world.RequireHealthyUpdate();
            Entity entity = _world.Entities.Get(worker);
            Code refusal = HaulTripPolicyContracts.CheckConfigure(_world, _viewer == PlayerId.None ? entity?.Owner ?? _viewer : _viewer, entity, settings);
            _world.RequireHealthyUpdate(); return refusal;
        }
        public HaulLoadSelectionPreview PreviewHaulLoadPolicyGroup(IReadOnlyList<EntityId> workers, HaulLoadSettings settings)
            => HaulLoadPolicyContracts.PreviewGroup(_world, _viewer, workers, settings);
        public HaulTripSelectionPreview PreviewHaulTripPolicyGroup(IReadOnlyList<EntityId> workers, HaulTripSettings settings)
            => HaulTripPolicyContracts.PreviewGroup(_world, _viewer, workers, settings);
        public Code ReadHaulTripServiceSources(EntityId worker, out IReadOnlyList<Code> value)
        {
            value = null; _world.RequireHealthyUpdate(); Code refusal = ReadHaulTripPolicy(worker, out HaulTripSettings settings);
            if (!refusal.IsNone) return refusal;
            var profile = _world.Content.Get<IHaulTripProfile>(settings.Profile);
            var facts = HaulTripPolicyContracts.ReadServiceSources(_world, _world.Entities.Get(worker), profile, requireCompatible: true, out _);
            foreach (Code source in facts)
                if (!RecurringPlanContracts.CanDisclose(_world, _viewer, _world.Entities.Get(worker).Get<IRecurringPlanSource>(source))) return Codes.NotFound;
            _world.RequireHealthyUpdate(); value = facts;
            return Code.None;
        }
        public Code ReadHaulTripServiceWindow(EntityId worker, out ServiceMovementWindow value)
        {
            value = null; _world.RequireHealthyUpdate(); Code refusal = ReadHaulTripPolicy(worker, out _);
            if (!refusal.IsNone) return refusal;
            var facts = HaulTripPolicyContracts.ReadServiceWindow(_world, _world.Entities.Get(worker));
            if (!RecurringPlanContracts.CanDisclose(_world, _viewer, facts?.SourceInputs)) return Codes.NotFound;
            _world.RequireHealthyUpdate(); value = facts;
            return Code.None;
        }

        public Code ReadHaulTripBudget(EntityId worker, out HaulTripEvaluation value)
        {
            value = null; _world.RequireHealthyUpdate(); Entity entity = _world.Entities.Get(worker); if (entity == null) return Codes.NotFound;
            if (!_world.ViewFor(_viewer).CanObserveOwned(entity)) return Codes.NotFound;
            if (entity.Get<IHaulTripPolicy>() == null) return HaulTripPolicyCodes.NotPolicy;
            var facts = _world.Services.Get<IHaulExecution>().ReadHaulTripBudget(worker);
            _world.RequireHealthyUpdate();
            if (!HaulTripPolicyContracts.CanDisclose(_world, _viewer, facts)) return Codes.NotFound;
            value = facts;
            return value == null ? HaulTripPolicyCodes.NotHauling : Code.None;
        }

        public HaulPreview PreviewHaul(EntityId source, EntityId destination, ResourceId resource, Fix ceiling, bool purchase = false, Code sourcePort = default, Code destinationPort = default, EntityId worker = default, int priority = 0, Code selection = default)
        {
            _world.RequireHealthyUpdate();
            Entity to = _world.Entities.Get(destination);
            var facts = _world.Services.Get<IHaulPlayerControls>().PreviewPlayerHaul(_viewer == PlayerId.None ? to?.Owner ?? _viewer : _viewer, source, destination, resource, ceiling, purchase, worker, sourcePort, destinationPort, priority, selection);
            _world.RequireHealthyUpdate(); return facts;
        }

        public Code PreviewRoute(EntityId source, EntityId destination, ResourceId resource, LogisticsRouteSettings settings, Code sourcePort = default, Code destinationPort = default)
        {
            _world.RequireHealthyUpdate();
            Entity entity = _world.Entities.Get(source);
            Code refusal = _world.Services.Get<ILogisticsRoutes>().CheckRoute(_viewer == PlayerId.None ? entity?.Owner ?? _viewer : _viewer, source, destination, resource, settings, sourcePort, destinationPort);
            _world.RequireHealthyUpdate(); return refusal;
        }
        public Code PreviewRouteSettings(int route, LogisticsRouteSettings settings)
        {
            _world.RequireHealthyUpdate();
            var current = _world.Services.Get<LogisticsService>().FindRoute(route);
            return _world.Services.Get<ILogisticsRoutes>().CheckRouteSettings(_viewer == PlayerId.None ? current?.Owner ?? _viewer : _viewer, route, settings);
        }
        public Code PreviewRemoveRoute(int route)
        {
            _world.RequireHealthyUpdate();
            var current = _world.Services.Get<LogisticsService>().FindRoute(route);
            return _world.Services.Get<ILogisticsRoutes>().CheckRemoveRoute(_viewer == PlayerId.None ? current?.Owner ?? _viewer : _viewer, route);
        }
        public void CopyRoutes(IList<RouteView> dst)
        {
            dst.Clear(); _world.RequireHealthyUpdate();
            var facts = new List<RouteView>();
            IReadOnlyList<LogisticsService.Route> routes = _world.Services.Get<LogisticsService>().Routes;
            for (int i = 0; i < routes.Count; i++)
                if (Sees(routes[i].Owner) && ObservesReference(routes[i].From) && ObservesReference(routes[i].To) &&
                    _world.ViewFor(_viewer).CanSeeContent(_world.Content.Resource(routes[i].Resource)) && ResourceLotSelection.Visible(_world, _viewer, routes[i].Settings.Selection)) facts.Add(new RouteView(routes[i], _world.Tick));
            _world.RequireHealthyUpdate();
            foreach (var fact in facts) dst.Add(fact);
        }

        /// <summary>Actual visible stock intent; unavailable is never substituted with zero demand.</summary>
        public Code ReadStockTarget(EntityId store, ResourceId resource, out Fix value)
        {
            value = Fix.Zero; _world.RequireHealthyUpdate();
            Entity e = _world.Entities.Get(store);
            if (!_world.ViewFor(_viewer).CanObserveOwned(e)) return Codes.NotFound;
            if (_world.Content.Resource(resource) == null) return Codes.InvalidArgument;
            var logistics = _world.Services.Get<LogisticsService>();
            if (!logistics.CheckDemandDisclosure(_viewer, store, resource).IsNone) return Codes.NotFound;
            value = EntityObservation.ReadProvider(_world, logistics, e, () => logistics.DemandOf(store, resource));
            return Code.None;
        }

    }
}
