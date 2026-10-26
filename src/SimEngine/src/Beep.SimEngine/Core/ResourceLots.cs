using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine
{
    /// <summary>A physical partition of a resource. None means untracked, never an invented quality.</summary>
    public readonly struct ResourceLotAmount
    {
        public ResourceId Resource { get; }
        public Code Lot { get; }
        public Fix Amount { get; }
        /// <summary>Runtime composition, not another catalog or stock ledger. Null resolves an authored lot.</summary>
        public IReadOnlyDictionary<string, Fix> GeneratedQualities { get; }
        public string Production { get; }
        public ResourceLotAmount(ResourceId resource, Code lot, Fix amount)
        { Resource = resource; Lot = lot; Amount = amount; GeneratedQualities = null; Production = null; }
        private ResourceLotAmount(ResourceId resource, Fix amount, IReadOnlyDictionary<string, Fix> qualities, string production)
        {
            if (string.IsNullOrWhiteSpace(production)) throw new ArgumentException("produced lots require explicit execution identity");
            Resource = resource; Amount = amount; Production = production;
            var values = new SortedDictionary<string, Fix>(StringComparer.Ordinal);
            foreach (var pair in qualities) values.Add(pair.Key, pair.Value);
            if (values.Count == 0) throw new ArgumentException("generated composition requires quality values");
            GeneratedQualities = new ReadOnlyDictionary<string, Fix>(values);
            // Execution + actual receipt state + composition identify a produced lot, without another sequence owner.
            var identity = new StringBuilder(resource.Value.ToString(CultureInfo.InvariantCulture)).Append('|').Append(production.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(production);
            foreach (var pair in values) identity.Append('|').Append(pair.Key.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(pair.Key).Append(':').Append(pair.Value.Raw.ToString(CultureInfo.InvariantCulture));
            using var hash = SHA256.Create();
            Lot = new Code("produced." + BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(identity.ToString()))).Replace("-", "").ToLowerInvariant());
        }
        public static ResourceLotAmount Produced(ResourceId resource, Fix amount, IReadOnlyDictionary<string, Fix> qualities, string production)
            => new ResourceLotAmount(resource, amount, qualities, production);
        private ResourceLotAmount(ResourceLotAmount original, Fix amount)
        { Resource = original.Resource; Lot = original.Lot; Amount = amount; GeneratedQualities = original.GeneratedQualities; Production = original.Production; }
        public ResourceLotAmount WithAmount(Fix amount) => new ResourceLotAmount(this, amount);
        public IReadOnlyDictionary<string, Fix> Qualities(ResolvedContent content)
            => GeneratedQualities ?? (Lot.IsNone ? null : content.Find<IResourceLotDefinition>(Lot.Id)?.Qualities);
        public bool ValidMetadata(ResolvedContent content)
        {
            if (GeneratedQualities == null) return Lot.IsNone || content.Find<IResourceLotDefinition>(Lot.Id)?.Resource == Resource;
            if (content.Find<IResourceLotDefinition>(Lot.Id) != null || Produced(Resource, Amount, GeneratedQualities, Production).Lot != Lot) return false;
            foreach (var pair in GeneratedQualities)
            {
                var axis = content.Find<IResourceQualityDefinition>(pair.Key);
                if (axis == null || axis.Resource != Resource || pair.Value < axis.Minimum || pair.Value > axis.Maximum ||
                    UnitConversion.ValidateAmount(axis.Unit, pair.Value) != UnitConversionStatus.Converted) return false;
            }
            return true;
        }
        public static ResourceLotAmount Sync(IStateSync sync, ResourceLotAmount value)
        {
            ResourceId resource = value.Resource; string id = value.Lot.Id; Fix amount = value.Amount;
            sync.Resource(ref resource); sync.String(ref id); sync.Fix(ref amount);
            string production = value.Production; sync.String(ref production);
            int count = sync.Count(value.GeneratedQualities?.Count ?? 0);
            var pairs = sync.Reading ? new SortedDictionary<string, Fix>(StringComparer.Ordinal) : null;
            if (sync.Reading)
            {
                string previous = null;
                for (int i = 0; i < count; i++)
                {
                    string key = null; Fix quality = Fix.Zero; sync.String(ref key); sync.Fix(ref quality);
                    if (string.IsNullOrWhiteSpace(key) || previous != null && string.CompareOrdinal(previous, key) >= 0) throw new CorruptStateException("invalid composition quality vector");
                    pairs.Add(key, quality); previous = key;
                }
                if ((count == 0) != (production == null)) throw new CorruptStateException("produced lot lost its execution identity or composition");
                var result = count == 0 ? new ResourceLotAmount(resource, id == null ? Code.None : new Code(id), amount) : Produced(resource, amount, pairs, production);
                if (result.Lot.Id != id) throw new CorruptStateException("composition identity mismatch");
                return result;
            }
            if (value.GeneratedQualities != null) foreach (var pair in value.GeneratedQualities)
            { string key = pair.Key; Fix quality = pair.Value; sync.String(ref key); sync.Fix(ref quality); }
            return value;
        }
    }
    public interface IResourceLotEndpoint : IResourceOutlet
    {
        IReadOnlyList<ResourceLotAmount> Lots { get; }
        IReadOnlyList<ResourceLotAmount> SelectLots(ReservationOwner owner, ReservationToken token, Fix amount);
        ReservationToken ReserveLotsOut(ReservationOwner owner, ResourceId resource, IReadOnlyList<ResourceLotAmount> lots, Code selection);
        void IncreaseLotsReservedOut(ReservationOwner owner, ReservationToken token, IReadOnlyList<ResourceLotAmount> lots);
        void InitializeLots(IReadOnlyList<ResourceLotAmount> lots);
        Fix ReceiveLotsReserved(ResolvedContent content, ReservationOwner owner, ReservationToken token, IReadOnlyList<ResourceLotAmount> lots);
    }
}
