using System;
using System.Collections.Generic;
using System.Numerics;

namespace Beep.SimEngine.Content
{
    /// <summary>Sole strict unit authoring declaration. Exact runtime conversions stay in UnitConversion.</summary>
    public sealed class UnitDeclaration
    {
        public UnitId Unit { get; private set; }
        public string Dimension { get; private set; }
        public long ScaleNumerator { get; private set; }
        public long ScaleDenominator { get; private set; }
        public bool Discrete { get; private set; }
        /// <summary>Validate the actual definition before any consumer resolves, not a substituted unit value.</summary>
        public static void Validate(IUnitContentDefinition unit, ContentResolver content)
        {
            if (unit == null || content == null) throw new ArgumentNullException();
            try
            {
                if (!content.Content.Owns(unit) || !ReferenceEquals(content.Content.Find<IUnitContentDefinition>(unit.Id), unit)
                    || unit.Unit != new UnitId(unit.Id)
                    || UnitConversion.ValidateAmount(unit, Fix.Zero) != UnitConversionStatus.Converted
                    || BigInteger.GreatestCommonDivisor(unit.ScaleNumerator, unit.ScaleDenominator) != BigInteger.One)
                    throw content.Fail("unit requires its actual owned identity and valid canonical metadata");
            }
            catch (Exception error) when (!(error is ContentException))
            { throw content.Fail("invalid unit metadata: " + error.Message); }
        }

        private UnitDeclaration() { }
        public static UnitDeclaration Read(string id, JsonReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            var declaration = new UnitDeclaration(); declaration.ReadCore(id, reader); return declaration;
        }
        private void ReadCore(string id, JsonReader r)
        {
            try { Unit = new UnitId(id); }
            catch (ArgumentException error) { throw r.Error(error.Message); }
            Dimension = r.String("dimension");
            if (string.IsNullOrWhiteSpace(Dimension)) throw r.Error("dimension is required");
            JsonReader scale = r.Child("scale") ?? throw r.Error("scale is required");
            long numerator = scale.Long("numerator"), denominator = scale.Long("denominator"); scale.End();
            if (numerator <= 0 || denominator <= 0) throw r.Error("unit scale must be positive");
            long gcd = (long)BigInteger.GreatestCommonDivisor(numerator, denominator);
            ScaleNumerator = numerator / gcd; ScaleDenominator = denominator / gcd;
            Discrete = r.Bool("discrete");
        }
    }

    /// <summary>Native units kind; independent definitions share the same authoring/contract.</summary>
    public sealed class UnitDef : Def, IUnitContentDefinition
    {
        private UnitDeclaration _declaration;
        public UnitId Unit => _declaration.Unit;
        public string Dimension => _declaration.Dimension;
        public long ScaleNumerator => _declaration.ScaleNumerator;
        public long ScaleDenominator => _declaration.ScaleDenominator;
        public bool Discrete => _declaration.Discrete;
        protected override void ReadCore(JsonReader reader) => _declaration = UnitDeclaration.Read(Id, reader);
    }

    public interface IResourceCatalog
    {
        IResourceDefinition Get(ResourceId id);
        bool TryResolve(string stableId, out ResourceId id);
        IReadOnlyList<IResourceDefinition> Definitions { get; }
    }
    public interface IResourceDefinition
    {
        string Id { get; }
        ResourceId Resource { get; }
        string Category { get; }
        IUnitDefinition QuantityUnit { get; }
        Code Phase { get; }
        IReadOnlyList<string> Traits { get; }
        IReadOnlyList<ResourceCapacityMeasure> CapacityMeasures { get; }
        bool HasTrait(string trait);
    }
    /// <summary>Actual authored resource metadata; ResolvedContent alone owns compact identities.</summary>
    public interface IResourceContentDefinition : IContentDefinition, IResourceDefinition
    {
        new string Id { get; }
    }
    public readonly struct ResourceCapacityMeasure
    {
        public IUnitDefinition Unit { get; }
        public Fix PerQuantity { get; }
        public ResourceCapacityMeasure(IUnitDefinition unit, Fix perQuantity)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (UnitConversion.ValidateAmount(unit, Fix.Zero) != UnitConversionStatus.Converted)
                throw new ArgumentException("valid capacity measure unit metadata required", nameof(unit));
            // A coefficient is not an amount in the measure unit. Fractional discrete coefficients
            // remain legal; original capacity quantum math determines representable native amounts.
            if (perQuantity <= Fix.Zero) throw new ArgumentOutOfRangeException(nameof(perQuantity));
            Unit = unit; PerQuantity = perQuantity;
        }
    }

    /// <summary>Sole strict resource metadata reader/resolver; no catalog, numeric ID allocator or stock.</summary>
    public sealed class ResourceDeclaration
    {
        private string _id;
        private string _unit;
        private readonly List<KeyValuePair<string, Fix>> _measures = new List<KeyValuePair<string, Fix>>();
        private IUnitDefinition _quantityUnit;
        private IReadOnlyList<ResourceCapacityMeasure> _capacityMeasures;
        private bool _resolutionAttempted;
        public ResourceId Resource { get; private set; }
        public string Category { get; private set; }
        public IUnitDefinition QuantityUnit => _quantityUnit ?? throw new InvalidOperationException("unresolved resource declaration");
        public Code Phase { get; private set; }
        public IReadOnlyList<string> Traits { get; private set; }
        public IReadOnlyList<ResourceCapacityMeasure> CapacityMeasures => _capacityMeasures ?? throw new InvalidOperationException("unresolved resource declaration");

        private ResourceDeclaration() { }
        public static ResourceDeclaration Read(string id, JsonReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            var declaration = new ResourceDeclaration(); declaration.ReadCore(id, reader); return declaration;
        }
        private void ReadCore(string id, JsonReader r)
        {
            if (string.IsNullOrWhiteSpace(id)) throw r.Error("resource identity must not be empty");
            _id = id;
            _unit = r.String("unit");
            Category = r.String("category"); string phase = r.String("phase");
            if (string.IsNullOrWhiteSpace(_unit) || string.IsNullOrWhiteSpace(Category) || string.IsNullOrWhiteSpace(phase))
                throw r.Error("resource unit, category and phase are required");
            Phase = new Code(phase);
            if (r.Raw.Get("traits") == null) throw r.Error("traits must be authored, including an explicit empty array");
            string[] traits = r.Strings("traits");
            for (int i = 0; i < traits.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(traits[i])) throw r.Error("trait identity must not be empty");
                for (int k = 0; k < i; k++) if (traits[k] == traits[i]) throw r.Error("duplicate trait '" + traits[i] + "'");
            }
            Traits = Array.AsReadOnly(traits);
            JsonReader capacity = r.Child("capacity") ?? throw r.Error("capacity measures are required");
            if (capacity.Raw.Count == 0) throw capacity.Error("at least one explicit capacity measure is required");
            for (int i = 0; i < capacity.Raw.Count; i++)
            {
                string unit = capacity.Raw.KeyAt(i); Fix amount = capacity.Fix(unit);
                if (string.IsNullOrWhiteSpace(unit)) throw capacity.Error("capacity measure unit identity must not be empty");
                if (amount <= Fix.Zero) throw capacity.Error("capacity measure must be positive: " + unit);
                _measures.Add(new KeyValuePair<string, Fix>(unit, amount));
            }
            capacity.End();
        }
        public void Resolve(ContentResolver c)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            if (_resolutionAttempted) throw new InvalidOperationException("resource declaration resolution may run only once");
            _resolutionAttempted = true;
            Resource = c.Resource(_id);
            IUnitContentDefinition quantityUnit = c.Def<IUnitContentDefinition>(_unit);
            var measures = new ResourceCapacityMeasure[_measures.Count];
            for (int i = 0; i < measures.Length; i++)
            {
                IUnitContentDefinition unit = c.Def<IUnitContentDefinition>(_measures[i].Key);
                for (int k = 0; k < i; k++) if (measures[k].Unit.Dimension == unit.Dimension)
                    throw c.Fail("capacity declares ambiguous measures for dimension '" + unit.Dimension + "'");
                measures[i] = new ResourceCapacityMeasure(unit, _measures[i].Value);
            }
            Array.Sort(measures, (a, b) => a.Unit.Unit.CompareTo(b.Unit.Unit));
            _quantityUnit = quantityUnit; _capacityMeasures = Array.AsReadOnly(measures);
        }
        public bool HasTrait(string trait)
            => HasTrait(Traits, trait);

        /// <summary>One lookup on authored or detached trait facts; never invokes a live resource provider.</summary>
        public static bool HasTrait(IReadOnlyList<string> traits, string trait)
        {
            if (traits == null) return false;
            for (int i = 0; i < traits.Count; i++) if (traits[i] == trait) return true;
            return false;
        }
        /// <summary>Admit every actual resource before any capacity/stock/recipe consumer resolves.</summary>
        public static void Validate(IResourceContentDefinition resource, ContentResolver resolver)
        {
            if (resource == null || resolver == null) throw new ArgumentNullException();
            try
            {
                ResolvedContent content = resolver.Content;
                if (!(resource is Def actual) || resource.Id != actual.Id ||
                    ((IContentDefinition)resource).Id != actual.Id || ((IResourceDefinition)resource).Id != actual.Id ||
                    resource.Module != actual.Module ||
                    !content.Owns(resource) || !ReferenceEquals(content.Find<IResourceContentDefinition>(actual.Id), resource) ||
                    resource.Resource != content.ResourceId(actual.Id) || !ReferenceEquals(content.Resource(resource.Resource), resource) ||
                    string.IsNullOrWhiteSpace(resource.Category) || resource.Phase.IsNone ||
                    !(resource.QuantityUnit is IUnitContentDefinition quantityUnit))
                    throw resolver.Fail("resource requires its actual catalog identity and explicit valid metadata");
                UnitDeclaration.Validate(quantityUnit, resolver);
                IReadOnlyList<string> traits = resource.Traits;
                IReadOnlyList<ResourceCapacityMeasure> measures = resource.CapacityMeasures;
                if (traits == null || measures == null || measures.Count == 0)
                    throw resolver.Fail("resource requires explicit traits and capacity measures");
                var seenTraits = new HashSet<string>(StringComparer.Ordinal);
                foreach (string trait in traits)
                    if (string.IsNullOrWhiteSpace(trait) || !seenTraits.Add(trait) || !resource.HasTrait(trait))
                        throw resolver.Fail("resource traits must be unique explicit supported identities");
                var dimensions = new HashSet<string>(StringComparer.Ordinal);
                foreach (ResourceCapacityMeasure measure in measures)
                {
                    if (!(measure.Unit is IUnitContentDefinition unit) || !measure.PerQuantity.IsPositive)
                        throw resolver.Fail("resource capacity requires actual valid unit and positive coefficient");
                    UnitDeclaration.Validate(unit, resolver);
                    if (!dimensions.Add(unit.Dimension))
                        throw resolver.Fail("resource capacity has ambiguous measures for one dimension");
                }
            }
            catch (Exception error) when (!(error is ContentException))
            { throw resolver.Fail("invalid resource metadata: " + error.Message); }
        }
    }
    /// <summary>Native resource kinds delegate all common metadata; specialized hooks cannot bypass it.</summary>
    public abstract class ResourceDef : Def, IResourceContentDefinition
    {
        private ResourceDeclaration _declaration;
        public ResourceId Resource => _declaration.Resource;
        public string Category => _declaration.Category;
        public IUnitDefinition QuantityUnit => _declaration.QuantityUnit;
        public Code Phase => _declaration.Phase;
        public IReadOnlyList<string> Traits => _declaration.Traits;
        public IReadOnlyList<ResourceCapacityMeasure> CapacityMeasures => _declaration.CapacityMeasures;
        protected sealed override void ReadCore(JsonReader reader)
        { _declaration = ResourceDeclaration.Read(Id, reader); ReadProperties(reader); }
        protected abstract void ReadProperties(JsonReader reader);
        protected sealed override void ResolveCore(ContentResolver content)
        { _declaration.Resolve(content); ResolveProperties(content); }
        protected virtual void ResolveProperties(ContentResolver content) { }
        public bool HasTrait(string trait) => _declaration.HasTrait(trait);
    }
    /// <summary>Built-in resources section; specialized kinds register their own derived definition section.</summary>
    public sealed class BasicResourceDef : ResourceDef
    {
        protected override void ReadProperties(JsonReader r) { }
    }
}
