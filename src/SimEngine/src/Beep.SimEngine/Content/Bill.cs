using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;

namespace Beep.SimEngine
{
    /// <summary>An amount of one resource.</summary>
    public readonly struct ResourceAmount : IEquatable<ResourceAmount>
    {
        public readonly ResourceId Resource;
        public readonly Fix Amount;
        public ResourceAmount(ResourceId resource, Fix amount) { Resource = resource; Amount = amount; }
        public bool Equals(ResourceAmount o) => Resource == o.Resource && Amount == o.Amount;
        public override bool Equals(object o) => o is ResourceAmount s && Equals(s);
        public override int GetHashCode() => Resource.Value * 31 + Amount.GetHashCode();
        public override string ToString() => Amount + " " + Resource;
    }

    /// <summary>
    /// An immutable list of resource amounts (a construction bill, a recipe's inputs, a starting stock),
    /// in canonical resource order so two bills with the same contents compare and hash the same.
    /// </summary>
    public sealed class Bill
    {
        public static readonly Bill Empty = new Bill(new ResourceAmount[0]);

        private readonly ResourceAmount[] _lines;

        public Bill(IEnumerable<ResourceAmount> lines)
        {
            var list = new List<ResourceAmount>();
            foreach (ResourceAmount s in lines)
            {
                if (!s.Resource.IsValid) throw new ArgumentException("a bill line needs an resource");
                if (s.Amount <= Fix.Zero) throw new ArgumentException("a bill line needs a positive amount");
                int at = list.FindIndex(x => x.Resource == s.Resource);
                if (at >= 0) list[at] = new ResourceAmount(s.Resource, list[at].Amount + s.Amount);
                else list.Add(s);
            }
            list.Sort((a, b) => a.Resource.Value.CompareTo(b.Resource.Value));
            _lines = list.ToArray();
        }

        public int Count => _lines.Length;
        public ResourceAmount this[int i] => _lines[i];
        public bool IsEmpty => _lines.Length == 0;
        public IReadOnlyList<ResourceAmount> Lines => Array.AsReadOnly(_lines);

        /// <summary>Actual authored resource aliases for this native bill; stock and quantities are not copied.</summary>
        public IReadOnlyList<IContentDefinition> ContentReferences(ResolvedContent content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            var references = new IContentDefinition[_lines.Length];
            for (int i = 0; i < _lines.Length; i++) references[i] = content.Resource(_lines[i].Resource);
            return Array.AsReadOnly(references);
        }

        public Fix AmountOf(ResourceId resource)
        {
            for (int i = 0; i < _lines.Length; i++) if (_lines[i].Resource == resource) return _lines[i].Amount;
            return Fix.Zero;
        }

        /// <summary>Detached quantity vector by authored unit, never a mixed-dimension scalar.</summary>
        public void CopyQuantities(IResourceCatalog catalog, IList<UnitQuantity> destination)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            destination.Clear();
            var amounts = new SortedDictionary<UnitId, Fix>();
            for (int i = 0; i < _lines.Length; i++)
            {
                UnitId unit = catalog.Get(_lines[i].Resource).QuantityUnit.Unit;
                amounts.TryGetValue(unit, out Fix held);
                amounts[unit] = held + _lines[i].Amount;
            }
            foreach (KeyValuePair<UnitId, Fix> amount in amounts) destination.Add(new UnitQuantity(amount.Key, amount.Value));
        }

        /// <summary>This bill with every amount multiplied by <paramref name="factor"/> (lines that reach zero drop).</summary>
        public Bill Scale(Fix factor)
        {
            var lines = new List<ResourceAmount>(_lines.Length);
            for (int i = 0; i < _lines.Length; i++)
            {
                Fix a = _lines[i].Amount * factor;
                if (a > Fix.Zero) lines.Add(new ResourceAmount(_lines[i].Resource, a));
            }
            return new Bill(lines);
        }
    }
}

namespace Beep.SimEngine.Content
{
    /// <summary>
    /// A bill as authored — {"steel": 20, "cement": 12} — read in <c>Read</c> and turned into a
    /// <see cref="Bill"/> of interned resources in <c>Resolve</c>.
    /// </summary>
    public sealed class BillSpec
    {
        private readonly List<KeyValuePair<string, Fix>> _lines = new List<KeyValuePair<string, Fix>>();
        private string _where;

        public static BillSpec Read(JsonReader r, string key)
        {
            var spec = new BillSpec { _where = r.Where + "." + key };
            JsonValue obj = r.Object(key);
            if (obj == null) return spec;
            var inner = new JsonReader(obj, r.Where + "." + key);
            for (int i = 0; i < obj.Count; i++)
            {
                string resource = obj.KeyAt(i);
                Fix amount = inner.Fix(resource);
                if (amount <= Fix.Zero) throw inner.Error("amount of '" + resource + "' must be positive");
                spec._lines.Add(new KeyValuePair<string, Fix>(resource, amount));
            }
            inner.End();
            return spec;
        }

        public bool IsEmpty => _lines.Count == 0;

        /// <summary>Resolves against content directly (for settings read outside a def).</summary>
        public Bill Resolve(ResolvedContent content, string where)
        {
            if (_lines.Count == 0) return Bill.Empty;
            var stacks = new List<ResourceAmount>(_lines.Count);
            for (int i = 0; i < _lines.Count; i++)
            {
                if (!content.TryResource(_lines[i].Key, out ResourceId resource)) throw new ContentException(where + ": unknown resource '" + _lines[i].Key + "'");
                ValidateQuantity(content.Resource(resource), _lines[i].Value, where);
                stacks.Add(new ResourceAmount(resource, _lines[i].Value));
            }
            return new Bill(stacks);
        }

        public Bill Resolve(ContentResolver c)
        {
            if (_lines.Count == 0) return Bill.Empty;
            var stacks = new List<ResourceAmount>(_lines.Count);
            for (int i = 0; i < _lines.Count; i++)
            {
                ResourceId resource = c.Resource(_lines[i].Key);
                ValidateQuantity(c.Content.Resource(resource), _lines[i].Value, _where + "['" + _lines[i].Key + "']");
                stacks.Add(new ResourceAmount(resource, _lines[i].Value));
            }
            return new Bill(stacks);
        }

        private static void ValidateQuantity(IResourceDefinition resource, Fix amount, string where)
        {
            UnitConversionStatus status = UnitConversion.ValidateAmount(resource.QuantityUnit, amount);
            if (status != UnitConversionStatus.Converted)
                throw new ContentException(where + ": quantity of '" + resource.Id + "' is invalid for " + resource.QuantityUnit.Unit + ": " + status);
        }
    }
}
