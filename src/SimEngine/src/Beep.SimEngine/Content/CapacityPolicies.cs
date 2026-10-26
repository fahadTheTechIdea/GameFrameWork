using System;
using System.Collections.Generic;
using System.Numerics;

namespace Beep.SimEngine.Content
{
    /// <summary>Single explicit JSON limits reader and exact admission calculation. No stock, promises or clock.</summary>
    public sealed class CapacityPolicyDeclaration
    {
        private IResourceCatalog _catalog;
        private readonly List<KeyValuePair<string, Fix>> _authored = new List<KeyValuePair<string, Fix>>();
        private IReadOnlyList<CapacityLimit> _limits;
        public IReadOnlyList<CapacityLimit> Limits => _limits ?? throw new InvalidOperationException("unresolved capacity declaration");

        public void Read(JsonReader r)
        {
            JsonReader limits = r.Child("limits") ?? throw r.Error("capacity limits must be authored");
            if (limits.Raw.Count == 0) throw limits.Error("at least one capacity limit is required");
            for (int i = 0; i < limits.Raw.Count; i++)
            {
                string unit = limits.Raw.KeyAt(i); Fix amount = limits.Fix(unit);
                if (string.IsNullOrWhiteSpace(unit)) throw limits.Error("capacity limit unit identity must not be empty");
                if (amount < Fix.Zero) throw limits.Error("capacity limit cannot be negative: " + unit);
                _authored.Add(new KeyValuePair<string, Fix>(unit, amount));
            }
            limits.End();
        }

        public void Resolve(ContentResolver c)
        {
            var limits = new CapacityLimit[_authored.Count];
            for (int i = 0; i < limits.Length; i++)
            {
                string path = ".limits['" + _authored[i].Key + "']";
                IUnitContentDefinition unit = c.Content.Find<IUnitContentDefinition>(_authored[i].Key);
                if (unit == null) throw c.Fail(path + ": unknown IUnitContentDefinition '" + _authored[i].Key + "'");
                UnitConversionStatus status = UnitConversion.ValidateAmount(unit, _authored[i].Value);
                if (status != UnitConversionStatus.Converted) throw c.Fail(path + ": invalid capacity limit: " + status);
                for (int k = 0; k < i; k++)
                    if (limits[k].Unit.Dimension == unit.Dimension)
                        throw c.Fail(path + ": repeated capacity dimension '" + unit.Dimension + "'");
                limits[i] = new CapacityLimit(unit, _authored[i].Value);
            }
            Array.Sort(limits, (a, b) => a.Unit.Unit.CompareTo(b.Unit.Unit));
            _limits = Array.AsReadOnly(limits); _catalog = c.Content;
        }

        public CapacityCheck Check(IReadOnlyList<ResourceAmount> stock,
            IReadOnlyList<ResourceAmount> promised, IReadOnlyList<ResourceAmount> incoming,
            long instances, Func<IResourceDefinition, CapacityStatus> validateResource)
        {
            if (_catalog == null) throw new InvalidOperationException("unresolved capacity declaration");
            if (validateResource == null) throw new ArgumentNullException(nameof(validateResource));
            if (stock == null) throw new ArgumentNullException(nameof(stock));
            if (promised == null) throw new ArgumentNullException(nameof(promised));
            if (incoming == null) throw new ArgumentNullException(nameof(incoming));
            if (instances <= 0) return new CapacityCheck(CapacityStatus.InvalidRequest);
            // Local calculation only. The catalog and the inventory vectors are never mutated/cached.
            var used = new BigInteger[3, Limits.Count];
            var native = new Dictionary<ResourceId, BigInteger>();
            CapacityCheck failure;
            if (!Measure(stock, 0, used, native, validateResource, out failure) || !Measure(promised, 1, used, native, validateResource, out failure) ||
                !Measure(incoming, 2, used, native, validateResource, out failure)) return failure;
            var facts = new CapacityUsage[Limits.Count];
            UnitId exceeded = default;
            for (int i = 0; i < facts.Length; i++)
            {
                BigInteger projected = used[0, i] + used[1, i] + used[2, i];
                BigInteger limit = (BigInteger)Limits[i].Amount.Raw * instances;
                if (projected > long.MaxValue || limit > long.MaxValue) return new CapacityCheck(CapacityStatus.OutOfRange, unit: Limits[i].Unit.Unit);
                Fix total = Fix.FromRaw((long)projected);
                Fix maximum = Fix.FromRaw((long)limit);
                facts[i] = new CapacityUsage(Limits[i].Unit.Unit, maximum,
                    Fix.FromRaw((long)used[0, i]), Fix.FromRaw((long)used[1, i]), Fix.FromRaw((long)used[2, i]), total);
                if (!exceeded.IsValid && total > maximum) exceeded = Limits[i].Unit.Unit;
            }
            return new CapacityCheck(exceeded.IsValid ? CapacityStatus.LimitExceeded : CapacityStatus.Fits,
                unit: exceeded, usage: facts);
        }

        public CapacityQuantum Quantum(ICapacityPolicy policy, ResourceId resource)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            CapacityCheck check = CapacityPolicyContracts.Check(policy, Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>(),
                new[] { new ResourceAmount(resource, Fix.Zero) });
            if (!check.Fits) return new CapacityQuantum(check.Status);
            IResourceDefinition definition = _catalog.Get(resource);
            BigInteger quantum = definition.QuantityUnit.Discrete ? Fix.One.Raw : BigInteger.One;
            foreach (CapacityLimit limit in Limits)
            {
                IUnitDefinition target = limit.Unit; ResourceCapacityMeasure measure = default;
                foreach (ResourceCapacityMeasure candidate in definition.CapacityMeasures)
                    if (candidate.Unit.Dimension == target.Dimension) { measure = candidate; break; }
                BigInteger a = (BigInteger)measure.PerQuantity.Raw * measure.Unit.ScaleNumerator * target.ScaleDenominator;
                BigInteger b = (BigInteger)Fix.One.Raw * measure.Unit.ScaleDenominator * target.ScaleNumerator;
                quantum = Lcm(quantum, b / BigInteger.GreatestCommonDivisor(a, b));
                if (measure.Unit.Discrete)
                {
                    BigInteger whole = (BigInteger)Fix.One.Raw * Fix.One.Raw;
                    quantum = Lcm(quantum, whole / BigInteger.GreatestCommonDivisor(measure.PerQuantity.Raw, whole));
                }
                if (target.Discrete)
                {
                    BigInteger whole = b * Fix.One.Raw;
                    quantum = Lcm(quantum, whole / BigInteger.GreatestCommonDivisor(a, whole));
                }
            }
            return quantum > long.MaxValue ? new CapacityQuantum(CapacityStatus.OutOfRange)
                : new CapacityQuantum(CapacityStatus.Fits, (long)quantum);
        }

        public CapacityOffer Offer(ICapacityPolicy policy, IReadOnlyList<ResourceAmount> stock, IReadOnlyList<ResourceAmount> promised,
            ResourceId resource, Fix ceiling, long instances = 1)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            if (ceiling < Fix.Zero) return new CapacityOffer(CapacityStatus.InvalidAmount);
            CapacityCheck current = CapacityPolicyContracts.Check(policy, stock, promised, new[] { new ResourceAmount(resource, Fix.Zero) }, instances);
            if (!current.Fits) return new CapacityOffer(current.Status);
            IResourceDefinition definition = _catalog.Get(resource);
            BigInteger upper = ceiling.Raw;
            BigInteger nativeHeld = BigInteger.Zero;
            foreach (ResourceAmount entry in stock) if (entry.Resource == resource) nativeHeld += entry.Amount.Raw;
            foreach (ResourceAmount entry in promised) if (entry.Resource == resource) nativeHeld += entry.Amount.Raw;
            upper = BigInteger.Min(upper, long.MaxValue - nativeHeld);
            CapacityQuantum quantum = CapacityPolicyContracts.Quantum(policy, resource);
            if (quantum.Status != CapacityStatus.Fits || quantum.Raw <= 0) return new CapacityOffer(quantum.Status);
            for (int i = 0; i < Limits.Count; i++)
            {
                IUnitDefinition target = Limits[i].Unit; ResourceCapacityMeasure measure = default;
                foreach (ResourceCapacityMeasure candidate in definition.CapacityMeasures)
                    if (candidate.Unit.Dimension == target.Dimension) { measure = candidate; break; }
                BigInteger a = (BigInteger)measure.PerQuantity.Raw * measure.Unit.ScaleNumerator * target.ScaleDenominator;
                BigInteger b = (BigInteger)Fix.One.Raw * measure.Unit.ScaleDenominator * target.ScaleNumerator;
                CapacityUsage usage = current.Usage[i];
                BigInteger room = (BigInteger)(usage.Limit.Raw - usage.Projected.Raw) * b / a;
                upper = BigInteger.Min(upper, room);
            }
            BigInteger raw = upper / quantum.Raw * quantum.Raw;
            if (raw == 0) return new CapacityOffer(ceiling.IsZero ? CapacityStatus.Fits : CapacityStatus.LimitExceeded);
            Fix amount = Fix.FromRaw((long)raw);
            // Specialized providers must approve the same intention; never silently bypass their Check.
            CapacityCheck check = CapacityPolicyContracts.Check(policy, stock, promised, new[] { new ResourceAmount(resource, amount) }, instances);
            return new CapacityOffer(check.Status, check.Fits ? amount : Fix.Zero);
        }

        private static BigInteger Lcm(BigInteger a, BigInteger b) => a / BigInteger.GreatestCommonDivisor(a, b) * b;

        private bool Measure(IReadOnlyList<ResourceAmount> values, int lane, BigInteger[,] used,
            Dictionary<ResourceId, BigInteger> native, Func<IResourceDefinition, CapacityStatus> validateResource, out CapacityCheck failure)
        {
            for (int r = 0; r < values.Count; r++)
            {
                ResourceAmount entry = values[r]; IResourceDefinition resource;
                try { resource = _catalog.Get(entry.Resource); }
                catch (ContentException) { failure = new CapacityCheck(CapacityStatus.UnknownResource, entry.Resource); return false; }
                CapacityStatus status = FromConversion(UnitConversion.ValidateAmount(resource.QuantityUnit, entry.Amount));
                if (status != CapacityStatus.Fits)
                { failure = new CapacityCheck(status, entry.Resource, resource.QuantityUnit.Unit); return false; }
                native.TryGetValue(entry.Resource, out BigInteger held);
                if (held + entry.Amount.Raw > long.MaxValue)
                { failure = new CapacityCheck(CapacityStatus.OutOfRange, entry.Resource, resource.QuantityUnit.Unit); return false; }
                native[entry.Resource] = held + entry.Amount.Raw;
                status = validateResource(resource);
                if (status != CapacityStatus.Fits)
                { failure = new CapacityCheck(status, entry.Resource); return false; }
                for (int i = 0; i < Limits.Count; i++)
                {
                    IUnitDefinition target = Limits[i].Unit;
                    ResourceCapacityMeasure measure = default; bool found = false;
                    for (int k = 0; k < resource.CapacityMeasures.Count; k++)
                        if (resource.CapacityMeasures[k].Unit.Dimension == target.Dimension)
                        { measure = resource.CapacityMeasures[k]; found = true; break; }
                    if (!found)
                    { failure = new CapacityCheck(CapacityStatus.MissingMeasure, entry.Resource, target.Unit); return false; }
                    // Both authored unit precision constraints apply to the measured quantity. Do not
                    // round a product into the source unit before converting it to the target unit.
                    BigInteger product = (BigInteger)entry.Amount.Raw * measure.PerQuantity.Raw;
                    if (measure.Unit.Discrete && product % ((BigInteger)Fix.One.Raw * Fix.One.Raw) != 0)
                    { failure = new CapacityCheck(CapacityStatus.FractionalDiscrete, entry.Resource, measure.Unit.Unit); return false; }
                    BigInteger numerator = product * measure.Unit.ScaleNumerator * target.ScaleDenominator;
                    BigInteger denominator = (BigInteger)Fix.One.Raw * measure.Unit.ScaleDenominator * target.ScaleNumerator;
                    BigInteger raw = BigInteger.DivRem(numerator, denominator, out BigInteger remainder);
                    if (remainder != 0)
                    { failure = new CapacityCheck(CapacityStatus.Unrepresentable, entry.Resource, target.Unit); return false; }
                    if (raw > long.MaxValue || used[lane, i] + raw > long.MaxValue)
                    { failure = new CapacityCheck(CapacityStatus.OutOfRange, entry.Resource, target.Unit); return false; }
                    if (target.Discrete && raw % Fix.One.Raw != 0)
                    { failure = new CapacityCheck(CapacityStatus.FractionalDiscrete, entry.Resource, target.Unit); return false; }
                    used[lane, i] += raw;
                }
            }
            failure = default; return true;
        }

        private static CapacityStatus FromConversion(UnitConversionStatus status)
        {
            switch (status)
            {
                case UnitConversionStatus.Converted: return CapacityStatus.Fits;
                case UnitConversionStatus.InvalidAmount: return CapacityStatus.InvalidAmount;
                case UnitConversionStatus.FractionalDiscrete: return CapacityStatus.FractionalDiscrete;
                default: return CapacityStatus.InvalidUnit;
            }
        }
    }

    /// <summary>Native authoring kind; independent definitions implement the same contract and share the declaration.</summary>
    public abstract class CapacityPolicyDef : Def, ICapacityPolicyDefinition
    {
        private readonly CapacityPolicyDeclaration _declaration = new CapacityPolicyDeclaration();
        public IReadOnlyList<CapacityLimit> Limits => _declaration.Limits;
        protected sealed override void ReadCore(JsonReader reader)
        {
            if (string.IsNullOrWhiteSpace(Id)) throw reader.Error("capacity policy identity must not be empty");
            _declaration.Read(reader); ReadProperties(reader);
        }
        protected abstract void ReadProperties(JsonReader reader);
        protected virtual void ResolveProperties(ContentResolver content) { }
        protected sealed override void ResolveCore(ContentResolver content)
        { _declaration.Resolve(content); ResolveProperties(content); }
        protected virtual CapacityStatus ValidateResource(IResourceDefinition resource) => CapacityStatus.Fits;
        public virtual CapacityCheck Check(IReadOnlyList<ResourceAmount> stock, IReadOnlyList<ResourceAmount> promised,
            IReadOnlyList<ResourceAmount> incoming, long instances = 1)
            => _declaration.Check(stock, promised, incoming, instances, ValidateResource);
        public virtual CapacityQuantum Quantum(ResourceId resource) => _declaration.Quantum(this, resource);
        public virtual CapacityOffer Offer(IReadOnlyList<ResourceAmount> stock, IReadOnlyList<ResourceAmount> promised,
            ResourceId resource, Fix ceiling, long instances = 1)
            => _declaration.Offer(this, stock, promised, resource, ceiling, instances);
    }

    public sealed class BasicCapacityPolicyDef : CapacityPolicyDef
    {
        protected override void ReadProperties(JsonReader r) { }
    }
}
