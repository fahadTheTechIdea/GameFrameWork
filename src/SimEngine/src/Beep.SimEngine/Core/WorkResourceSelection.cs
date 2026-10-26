using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Beep.SimEngine
{
    public enum WorkResourceUse : byte { Consume = 0, Hold = 1, Continuous = 2, Elapsed = 3 }
    internal static class WorkResourceUses
    {
        internal static bool Running(WorkResourceUse use) => use == WorkResourceUse.Continuous || use == WorkResourceUse.Elapsed;
    }
    public readonly struct WorkResourceSource
    {
        public ResourceEndpointId Endpoint { get; }
        public long Cost { get; }
        public WorkResourceSource(ResourceEndpointId endpoint, long cost) { Endpoint = endpoint; Cost = cost; }
    }
    public readonly struct WorkResourceCandidate
    {
        public WorkResourceChoice Choice { get; }
        public WorkResourceSource Source { get; }
        public WorkRequirementResult Result { get; }
        public IReadOnlyList<ResourceClaimAvailability> Availability { get; }
        public WorkResourceCandidate(WorkResourceChoice choice, WorkResourceSource source, WorkRequirementResult result, IReadOnlyList<ResourceClaimAvailability> availability)
        { Choice = choice; Source = source; Result = result; Availability = new List<ResourceClaimAvailability>(availability).AsReadOnly(); }
    }
    /// <summary>Pure selected source/intent, not an endpoint handle or a promise.</summary>
    public sealed class WorkResourceChoice
    {
        public IReadOnlyList<string> Path { get; }
        public string Requirement { get; }
        public ResourceEndpointId Endpoint { get; }
        public Bill Quantities { get; }
        public IReadOnlyList<Content.IResourceLotSelectionDefinition> Selections { get; }
        public WorkResourceUse Use { get; }
        public IReadOnlyList<IResourceRate> Rates { get; }
        public WorkResourceChoice(IEnumerable<string> path, string requirement, ResourceEndpointId endpoint, Bill quantities, WorkResourceUse use,
            IEnumerable<IResourceRate> rates, IEnumerable<Content.IResourceLotSelectionDefinition> selections = null)
        {
            Path = WorkRequirementPath.Copy(path);
            if (string.IsNullOrWhiteSpace(requirement) || !endpoint.IsValid || quantities == null || quantities.IsEmpty || use > WorkResourceUse.Elapsed)
                throw new ArgumentException("resource choice requires an authored occurrence, endpoint and native bill/use");
            Requirement = requirement; Endpoint = endpoint; Quantities = quantities; Use = use;
            Rates = CopyRates(rates, quantities, use);
            Selections = selections == null ? Array.Empty<Content.IResourceLotSelectionDefinition>() : new List<Content.IResourceLotSelectionDefinition>(selections).AsReadOnly();
        }
        internal static IReadOnlyList<IResourceRate> CopyRates(IEnumerable<IResourceRate> rates, Bill quantities, WorkResourceUse use)
        {
            var detached = new List<IResourceRate>(); var seen = new HashSet<ResourceId>();
            foreach (var rate in rates ?? throw new ArgumentNullException(nameof(rates)))
            {
                if (rate?.Resource == null || !seen.Add(rate.Resource.Resource) || quantities.AmountOf(rate.Resource.Resource) != rate.Quantity)
                    throw new ArgumentException("resource rates must match the same unique native reference bill");
                detached.Add(new ResourceRate(rate.Resource, rate.Quantity, rate.PeriodNumerator, rate.PeriodDenominator, rate.PeriodUnit));
            }
            if (use == WorkResourceUse.Elapsed ? detached.Count != quantities.Count : detached.Count != 0)
                throw new ArgumentException("elapsed use requires explicit rates; other uses have no rate intent");
            detached.Sort((a, b) => a.Resource.Resource.CompareTo(b.Resource.Resource));
            return detached.AsReadOnly();
        }
        internal bool At(IReadOnlyList<string> path) => WorkRequirementPath.Matches(Path, path);
        internal ReservationOwner Owner(int order, string node)
            => WorkRequirementPath.Owner("sim.work.resources.", order, node, Path);
    }
    /// <summary>Existing node-owned links/facts; actual live promises remain in endpoint books.</summary>
    public sealed class WorkResourceBinding
    {
        public WorkResourceChoice Choice { get; }
        public IReadOnlyList<ResourceReservation> Receipts { get; }
        public bool Spent { get; }
        public WorkResourceBinding(WorkResourceChoice choice, IEnumerable<ResourceReservation> receipts, bool spent)
        { Choice = choice ?? throw new ArgumentNullException(nameof(choice)); Receipts = new List<ResourceReservation>(receipts).AsReadOnly(); Spent = spent; }
    }
    /// <summary>Detached running-input accounting derived from node work/exposure, never a stock ledger.</summary>
    public sealed class WorkResourceUsage
    {
        public IReadOnlyList<string> Path { get; }
        public string Requirement { get; }
        public ResourceEndpointId Endpoint { get; }
        public WorkResourceUse Use { get; }
        /// <summary>Whole-work budget for Continuous; quantities per explicit resource rate period for Elapsed.</summary>
        public Bill ReferenceQuantity { get; }
        public IReadOnlyList<IResourceRate> Rates { get; }
        public Bill Consumed { get; }
        /// <summary>Only bounded work-normalized inputs have a remaining budget. Elapsed returns null.</summary>
        public Bill RemainingBudget { get; }
        internal WorkResourceUsage(IEnumerable<string> path, string requirement, ResourceEndpointId endpoint, WorkResourceUse use, Bill total,
            IReadOnlyList<IResourceRate> rates, Bill consumed, Bill remaining)
        {
            Path = WorkRequirementPath.Copy(path); Requirement = requirement; Endpoint = endpoint; Use = use; ReferenceQuantity = total; Consumed = consumed;
            RemainingBudget = remaining;
            Rates = WorkResourceChoice.CopyRates(rates, total, use);
        }
    }
    internal static class WorkRequirementPath
    {
        internal static ReservationOwner Owner(string prefix, int order, string node, IReadOnlyList<string> path)
        {
            var scope = new StringBuilder(prefix); Append(node); foreach (var segment in path) Append(segment);
            return new ReservationOwner(scope.ToString(), order);
            void Append(string part) => scope.Append(part.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(part);
        }
        internal static IReadOnlyList<string> Copy(IEnumerable<string> path)
        {
            var segments = new List<string>(path ?? throw new ArgumentNullException(nameof(path)));
            if (segments.Count == 0) throw new ArgumentException("requirement occurrence needs a path");
            foreach (var segment in segments) if (string.IsNullOrWhiteSpace(segment)) throw new ArgumentException("path needs stable identities");
            return segments.AsReadOnly();
        }
        internal static bool Matches(IReadOnlyList<string> left, IReadOnlyList<string> right)
        {
            if (left.Count != right.Count) return false;
            for (int i = 0; i < left.Count; i++) if (left[i] != right[i]) return false;
            return true;
        }
    }
}
