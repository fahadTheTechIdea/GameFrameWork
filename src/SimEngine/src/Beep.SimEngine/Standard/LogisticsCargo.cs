using System;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Current stock disposition only. Actual inventory, policies and receipt books remain authoritative.</summary>
    public sealed class CargoDispositionSnapshot
    {
        public EntityId Worker { get; }
        public ResourceEndpointId Endpoint { get; }
        public Bill Retained { get; }
        public Bill Committed { get; }
        public Bill Unassigned { get; }
        public bool HasResidualCargo => !Committed.IsEmpty || !Unassigned.IsEmpty;
        internal CargoDispositionSnapshot(EntityId worker, ResourceEndpointId endpoint, Bill retained, Bill committed, Bill unassigned)
        { Worker = worker; Endpoint = endpoint; Retained = retained; Committed = committed; Unassigned = unassigned; }
    }
    public sealed partial class LogisticsService
    {
        private Fix ProtectedStock(IOutletPort store, ResourceId resource, OutletPolicySettings settings, bool emergency)
        {
            bool release = emergency && settings?.ReserveAccess == LogisticsReserveAccess.EmergencyOnly;
            return Fix.Max(DemandOf(store.Entity.Id, resource, release), release ? Fix.Zero : settings?.Reserve.AmountOf(resource) ?? Fix.Zero);
        }

        public CargoDispositionSnapshot ReadCargoDisposition(EntityId workerId)
        {
            Entity worker = _world.Entities.Get(workerId);
            if (worker == null || !worker.Alive || worker.Get<ITransporter>() == null)
                throw new ArgumentException("cargo disposition requires an actual live transporter");
            return EntityObservation.ReadProvider(_world, worker.Get<ITransporter>(), worker, () =>
            {
                var cargo = TransporterContracts.ReadCargo(_world, worker);
                return EntityObservation.ReadProvider(_world, cargo, cargo.Entity, () =>
                {
                    if (!cargo.Endpoint.IsValid || cargo.Company != worker.Owner)
                        throw new InvalidOperationException("cargo disposition requires its actual owned endpoint");
                    var settings = LogisticsPolicyRules.Read(_world, cargo.Entity);
                    var retained = new List<ResourceAmount>(); var committed = new List<ResourceAmount>(); var unassigned = new List<ResourceAmount>();
                    var stock = cargo.Stock ?? throw new InvalidOperationException("cargo disposition requires actual stock rows");
                    var resources = new HashSet<ResourceId>();
                    foreach (var line in stock)
                    {
                        if (!line.Resource.IsValid || !resources.Add(line.Resource) || !line.Amount.IsPositive)
                            throw new InvalidOperationException("cargo disposition requires unique positive resource stock rows");
                        var unit = ((Content.IResourceCatalog)_world.Content).Get(line.Resource).QuantityUnit;
                        if (UnitConversion.ValidateAmount(unit, line.Amount) != UnitConversionStatus.Converted)
                            throw new InvalidOperationException("cargo stock violates its actual resource unit");
                        Fix available = cargo.Available(line.Resource), promised = cargo.ReservedOut(line.Resource);
                        if (UnitConversion.ValidateAmount(unit, available) != UnitConversionStatus.Converted ||
                            UnitConversion.ValidateAmount(unit, promised) != UnitConversionStatus.Converted)
                            throw new InvalidOperationException("cargo partition violates its actual resource unit");
                        if (cargo.Amount(line.Resource) != line.Amount || available < Fix.Zero || promised < Fix.Zero || available + promised != line.Amount)
                            throw new InvalidOperationException("cargo disposition disagrees with actual available stock and outbound receipts");
                        Fix keep = Fix.Min(available, ProtectedStock(cargo, line.Resource, settings, false));
                        Fix spare = available - keep;
                        if (keep.IsPositive) retained.Add(new ResourceAmount(line.Resource, keep));
                        if (promised.IsPositive) committed.Add(new ResourceAmount(line.Resource, promised));
                        if (spare.IsPositive) unassigned.Add(new ResourceAmount(line.Resource, spare));
                    }
                    return new CargoDispositionSnapshot(worker.Id, cargo.Endpoint, new Bill(retained), new Bill(committed), new Bill(unassigned));
                }, observeTargetState: true);
            }, space: _world.Space, observeTargetState: true);
        }
    }
}
