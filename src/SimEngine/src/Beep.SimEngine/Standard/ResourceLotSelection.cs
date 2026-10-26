using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public static class ResourceLotSelection
    {
        public static bool Visible(World world, PlayerId viewer, Code selection)
        {
            if (selection.IsNone || viewer == PlayerId.None) return true;
            var definition = world.Content.Find<IResourceLotSelectionDefinition>(selection.Id); var view = world.ViewFor(viewer);
            if (definition == null || !view.CanSeeContent(definition) || !view.CanSeeContent(world.Content.Resource(definition.Resource))) return false;
            if (!definition.Lot.IsNone && !LotVisible(world, viewer, new ResourceLotAmount(definition.Resource, definition.Lot, Fix.One))) return false;
            foreach (var range in definition.Qualities) if (!view.CanSeeContent(range.Quality) || !view.CanSeeContent((IContentDefinition)range.Quality.Unit)) return false;
            return true;
        }
        public static bool LotVisible(World world, PlayerId viewer, ResourceLotAmount lot)
        {
            var view = world.ViewFor(viewer);
            if (!view.CanSeeContent(world.Content.Resource(lot.Resource))) return false;
            if (lot.Lot.IsNone) return true;
            if (lot.GeneratedQualities == null && !view.CanSeeContent(world.Content.Find<IResourceLotDefinition>(lot.Lot.Id))) return false;
            var qualities = lot.Qualities(world.Content);
            if (qualities == null) return false;
            foreach (var pair in qualities)
            {
                var axis = world.Content.Find<IResourceQualityDefinition>(pair.Key);
                if (axis == null || !view.CanSeeContent(axis) || !view.CanSeeContent((IContentDefinition)axis.Unit)) return false;
            }
            return true;
        }
        internal static Fix EligibleAmount(ResolvedContent content, IReadOnlyList<ResourceLotAmount> lots, ResourceId resource, IResourceLotSelectionDefinition selection)
        {
            Fix result = Fix.Zero;
            foreach (var lot in lots) if (lot.Resource == resource && Matches(content, lot, selection)) result += lot.Amount;
            return result;
        }
        public static bool Matches(ResolvedContent content, ResourceLotAmount lot, IResourceLotSelectionDefinition selection)
        {
            if (selection == null) return true;
            if (lot.Resource != selection.Resource || lot.Lot.IsNone || !selection.Lot.IsNone && selection.Lot != lot.Lot) return false;
            var qualities = lot.Qualities(content);
            if (!lot.ValidMetadata(content) || qualities == null) throw new InvalidOperationException("missing actual lot content");
            foreach (var range in selection.Qualities)
                if (!qualities.TryGetValue(range.Quality.Id, out Fix value) || value < range.Minimum || value > range.Maximum) return false;
            return true;
        }
        /// <summary>Ephemeral admission vector, derived from the sole physical buffer and original reservation records.</summary>
        internal static ResourceLotBuffer Unclaimed(IReadOnlyList<ResourceLotAmount> lots, IReadOnlyList<ResourceReservation> records)
        {
            var free = new ResourceLotBuffer(); foreach (var lot in lots) free.Add(lot);
            foreach (var record in records) if (record.Direction == ReservationDirection.Outbound)
            {
                if (record.Lots.Count == 0) throw new InvalidOperationException("lot-capable outlet must bind every outbound promise to partitions");
                free.Remove(record.Lots);
            }
            return free;
        }
        internal static IReadOnlyList<ResourceLotAmount> Allocate(ResolvedContent content, IReadOnlyList<ResourceLotAmount> free,
            ResourceId resource, Fix amount, IResourceLotSelectionDefinition selection)
        {
            var result = new List<ResourceLotAmount>();
            foreach (var lot in free) if (lot.Resource == resource && Matches(content, lot, selection) && amount.IsPositive)
            {
                Fix take = Fix.Min(lot.Amount, amount); result.Add(lot.WithAmount(take)); amount -= take;
            }
            return amount.IsPositive ? null : result.AsReadOnly();
        }
        public static bool ValidRange(ResolvedContent content, Code quality, Fix minimum, Fix maximum)
        {
            var axis = quality.IsNone ? null : content.Find<IResourceQualityDefinition>(quality.Id);
            return axis != null && minimum <= maximum && minimum >= axis.Minimum && maximum <= axis.Maximum &&
                UnitConversion.ValidateAmount(axis.Unit, minimum) == UnitConversionStatus.Converted &&
                UnitConversion.ValidateAmount(axis.Unit, maximum) == UnitConversionStatus.Converted;
        }
        /// <summary>On-hand quality candidates, not a reservation or a promise of future availability.</summary>
        public static IReadOnlyList<ResourceLotAmount> Select(ResolvedContent content, IReadOnlyList<ResourceLotAmount> lots,
            Code quality, Fix minimum, Fix maximum)
        {
            var axis = content.Find<IResourceQualityDefinition>(quality.Id);
            if (!ValidRange(content, quality, minimum, maximum))
                throw new ArgumentException("quality selection requires an actual axis and exact bounded range");
            var selected = new List<ResourceLotAmount>();
            foreach (var lot in lots)
            {
                if (lot.Resource != axis.Resource || lot.Lot.IsNone) continue;
                var qualities = lot.Qualities(content);
                if (!lot.ValidMetadata(content) || qualities == null) throw new InvalidOperationException("lot lost its actual content identity");
                if (qualities.TryGetValue(quality.Id, out Fix value) && value >= minimum && value <= maximum) selected.Add(lot);
            }
            return selected.AsReadOnly();
        }
    }
}
