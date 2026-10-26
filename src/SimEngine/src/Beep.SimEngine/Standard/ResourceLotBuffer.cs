using System;
using System.Collections;
using System.Collections.Generic;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Sole physical stock collection. Aggregate vectors are derived snapshots, never another ledger.</summary>
    internal sealed class ResourceLotBuffer : IReadOnlyList<ResourceAmount>
    {
        private readonly List<ResourceLotAmount> _lots = new List<ResourceLotAmount>();
        public IReadOnlyList<ResourceLotAmount> Lots => new List<ResourceLotAmount>(_lots).AsReadOnly();
        public List<ResourceAmount> Aggregate()
            => new List<ResourceAmount>(this);
        public int Count
        {
            get { int count = 0; ResourceId previous = ResourceId.None; foreach (var lot in _lots) if (lot.Resource != previous) { count++; previous = lot.Resource; } return count; }
        }
        public ResourceAmount this[int index]
        {
            get { if (index < 0) throw new ArgumentOutOfRangeException(nameof(index)); foreach (var line in this) if (index-- == 0) return line; throw new ArgumentOutOfRangeException(nameof(index)); }
        }
        public IEnumerator<ResourceAmount> GetEnumerator()
        {
            int at = 0;
            while (at < _lots.Count)
            {
                ResourceId resource = _lots[at].Resource; Fix amount = Fix.Zero;
                while (at < _lots.Count && _lots[at].Resource == resource) amount += _lots[at++].Amount;
                yield return new ResourceAmount(resource, amount);
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public Fix Amount(ResourceId resource)
        { Fix total = Fix.Zero; foreach (var lot in _lots) if (lot.Resource == resource) total += lot.Amount; return total; }
        private static int Compare(ResourceLotAmount a, ResourceLotAmount b)
        { int order = a.Resource.CompareTo(b.Resource); return order != 0 ? order : string.CompareOrdinal(a.Lot.Id, b.Lot.Id); }
        public void Add(ResourceLotAmount value)
        {
            for (int i = 0; i < _lots.Count; i++) if (_lots[i].Resource == value.Resource && _lots[i].Lot == value.Lot)
            { _lots[i] = value.WithAmount(_lots[i].Amount + value.Amount); return; }
            _lots.Add(value); _lots.Sort(Compare);
        }
        public void Remove(IReadOnlyList<ResourceLotAmount> selected)
        {
            foreach (var take in selected)
            {
                int at = _lots.FindIndex(lot => lot.Resource == take.Resource && lot.Lot == take.Lot);
                if (at < 0 || !take.Amount.IsPositive || _lots[at].Amount < take.Amount) throw new InvalidOperationException("lot removal exceeds actual partition");
                var lot = _lots[at];
                if (lot.Amount == take.Amount) _lots.RemoveAt(at);
                else _lots[at] = lot.WithAmount(lot.Amount - take.Amount);
            }
        }
        public void Tag(ResourceLotAmount value)
        {
            ResourceId resource = value.Resource; Fix amount = value.Amount;
            for (int i = 0; i < _lots.Count; i++) if (_lots[i].Resource == resource && _lots[i].Lot.IsNone && _lots[i].Amount >= amount)
            {
                Fix rest = _lots[i].Amount - amount; _lots.RemoveAt(i);
                if (rest.IsPositive) Add(new ResourceLotAmount(resource, Code.None, rest));
                Add(value); return;
            }
            throw new InvalidOperationException("lot tagging requires actual untracked material");
        }
        public void Clear() => _lots.Clear();
        /// <summary>Detached exact lifecycle delta, not another stock owner.</summary>
        internal static IReadOnlyList<ResourceLotAmount> Difference(IReadOnlyList<ResourceLotAmount> before,
            IReadOnlyList<ResourceLotAmount> after, ResourceId resource, bool adds)
        {
            var delta = new ResourceLotBuffer();
            foreach (var lot in adds ? after : before) if (lot.Resource == resource) delta.Add(lot);
            foreach (var lot in adds ? before : after) if (lot.Resource == resource) delta.Remove(new[] { lot });
            return delta.Lots;
        }
    }
}
