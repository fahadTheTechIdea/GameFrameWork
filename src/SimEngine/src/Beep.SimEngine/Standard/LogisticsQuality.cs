using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    // The existing logistics owner derives decisions from actual stock/receipts. No new ledger or scheduler.
    public sealed partial class LogisticsService
    {
        public static readonly Code QualityIncompatible = new Code("logistics.quality-incompatible");
        internal static bool ValidSelection(ResolvedContent content, ResourceId resource, Code selection)
        {
            if (selection.IsNone) return true;
            var definition = content.Find<IResourceLotSelectionDefinition>(selection.Id);
            return definition != null && definition.Resource == resource &&
                ResourceLotContentContracts.ValidSelection(content, resource, definition.Lot, definition.Qualities);
        }
        private IReadOnlyList<IResourceLotSelectionDefinition> SelectedClaims(ResourceId resource, Code selection)
        {
            if (selection.IsNone) return Array.Empty<IResourceLotSelectionDefinition>();
            if (!ValidSelection(_world.Content, resource, selection)) throw new InvalidOperationException("haul lost its actual quality selection");
            return Array.AsReadOnly(new[] { _world.Content.Find<IResourceLotSelectionDefinition>(selection.Id) });
        }
        private Fix QualifiedAvailable(IResourceInlet inlet, ResourceId resource, Code selection)
        {
            if (selection.IsNone) return Unpromised(inlet, resource);
            if (!(inlet is IResourceLotEndpoint source)) return Fix.Zero;
            return EntityObservation.ReadProvider(_world, source, null, () => ResourceLotSelection.EligibleAmount(_world.Content,
                ResourceLotSelection.Unclaimed(source.Lots, source.Reservations).Lots, resource, SelectedClaims(resource, selection)[0]));
        }
        private bool HasQualityDemand(ResourceEndpointId endpoint, ResourceId resource)
        { foreach (var row in _demands) if (row.Endpoint == endpoint && row.Resource == resource && !row.Settings.Selection.IsNone && DemandActive(row)) return true; return false; }

        // Project physical on-hand and actually committed incoming partitions once, then allocate them
        // to active demand groups in stable priority order. Incompatible stock never fills a quality gap.
        private Dictionary<Code, Fix> QualityGaps(IResourceInlet inlet, ResourceId resource, bool emergencyOnly, bool includeIncoming = true)
        {
            var projected = inlet is IResourceLotEndpoint lots
                ? EntityObservation.ReadProvider(_world, lots, null, () => ResourceLotSelection.Unclaimed(lots.Lots, lots.Reservations))
                : new ResourceLotBuffer();
            if (!(inlet is IResourceLotEndpoint) && Unpromised(inlet, resource).IsPositive)
                projected.Add(new ResourceLotAmount(resource, Code.None, Unpromised(inlet, resource)));
            Fix represented = Fix.Zero;
            if (includeIncoming) foreach (var job in _world.Services.Get<WorkBoard>().Jobs)
            {
                if (!job.IsLive || !job.IsHaul || job.DestinationEndpoint != inlet.Endpoint || job.Resource != resource) continue;
                var receipt = inlet.Reservations;
                bool incoming = false; foreach (var record in receipt) if (record.Token.Equals(job.InReservation) && record.Direction == ReservationDirection.Inbound) incoming = true;
                if (!incoming) continue;
                AddReceipt(ReadHaulEndpoint(job, true).Store, job.OutReservation);
                if (_world.Entities.Get(job.Worker) is Entity worker && worker.Alive) AddReceipt(TransporterContracts.ReadCargo(_world, worker), job.CargoOutReservation);
            }
            void AddReceipt(IResourceInlet source, ReservationToken token)
            {
                if (source == null || !token.IsValid) return;
                EntityObservation.ReadProvider(_world, source, null, () => {
                    foreach (var record in source.Reservations) if (record.Token.Equals(token) && record.Direction == ReservationDirection.Outbound)
                        foreach (var lot in record.Lots) { projected.Add(lot); represented += lot.Amount; }
                    return true;
                });
            }
            Fix unknown = includeIncoming ? inlet.ReservedIn(resource) - represented : Fix.Zero;
            if (unknown < Fix.Zero) throw new InvalidOperationException("quality projection exceeds actual inbound commitments");
            if (unknown.IsPositive) projected.Add(new ResourceLotAmount(resource, Code.None, unknown));
            var groups = new Dictionary<Code, (Demand First, Fix Target)>();
            foreach (var row in _demands)
                if (row.Endpoint == inlet.Endpoint && row.Resource == resource && DemandActive(row) &&
                    (row.Source.Scope != StockDemandScope || row.Replenishing || !includeIncoming) && (!emergencyOnly || CheckSource(row).Emergency))
                {
                    if (groups.TryGetValue(row.Settings.Selection, out var group))
                        groups[row.Settings.Selection] = (CompareDemand(row, group.First) < 0 ? row : group.First, group.Target + row.Settings.Desired);
                    else groups.Add(row.Settings.Selection, (row, row.Settings.Desired));
                }
            var ordered = new List<(Demand First, Fix Target)>(groups.Values);
            ordered.Sort((a, b) => { int quality = a.First.Settings.Selection.IsNone.CompareTo(b.First.Settings.Selection.IsNone); return quality != 0 ? quality : CompareDemand(a.First, b.First); });
            var result = new Dictionary<Code, Fix>();
            foreach (var group in ordered)
            {
                Code selected = group.First.Settings.Selection;
                var filter = selected.IsNone ? null : SelectedClaims(resource, selected)[0];
                Fix available = ResourceLotSelection.EligibleAmount(_world.Content, projected.Lots, resource, filter);
                Fix covered = Fix.Min(group.Target, available);
                if (covered.IsPositive) projected.Remove(ResourceLotSelection.Allocate(_world.Content, projected.Lots, resource, covered, filter));
                result.Add(selected, group.Target - covered);
            }
            return result;
        }
        private Fix OnHandQualityShortage(IResourceInlet inlet, ResourceId resource)
        { Fix result = Fix.Zero; foreach (var gap in QualityGaps(inlet, resource, false, includeIncoming: false).Values) result += gap; return result; }
    }
}
