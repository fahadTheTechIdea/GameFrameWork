using System;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public enum BusinessMode : byte { FixedDefaults = 0, Tycoon = 1, Unavailable = 255 }
    public enum ResourceValueStatus : byte { Unavailable, Ready, UnknownResource, InvalidQuantity, OutOfRange, Unrepresentable }

    /// <summary>A value quote, not a sale, resource mutation or production-income commitment.</summary>
    public readonly struct ResourceValueQuote
    {
        public ResourceValueStatus Status { get; }
        public string Policy { get; }
        public BusinessMode Mode { get; }
        public ResourceId Resource { get; }
        public UnitId QuantityUnit { get; }
        public UnitId Currency { get; }
        public Fix Quantity { get; }
        public Fix UnitValue { get; }
        public Fix Value { get; }
        public ResourceValueQuote(ResourceValueStatus status, string policy, BusinessMode mode,
            ResourceId resource, UnitId quantityUnit, UnitId currency, Fix quantity, Fix unitValue, Fix value)
        { Status = status; Policy = policy; Mode = mode; Resource = resource; QuantityUnit = quantityUnit;
          Currency = currency; Quantity = quantity; UnitValue = unitValue; Value = value; }
    }

    /// <summary>The one selected business valuation policy. Quotes must be pure and deterministic.</summary>
    public interface IResourceValuation
    {
        string Policy { get; }
        BusinessMode Mode { get; }
        UnitId Currency { get; }
        ResourceValueQuote Quote(ResourceId resource, Fix nativeQuantity);
    }

    /// <summary>Open content contract: a mod can author another policy without changing the module.</summary>
    public interface IBusinessValuePolicyDefinition : IContentDefinition
    {
        BusinessMode Mode { get; }
        IUnitDefinition CurrencyUnit { get; }
        IResourceValuation Create(World world);
    }
    public interface IFixedResourceValueDefinition : IContentDefinition
    {
        string Catalog { get; }
        IResourceDefinition Resource { get; }
        IUnitDefinition CurrencyUnit { get; }
        Fix PerUnit { get; }
    }
    /// <summary>One authoring protocol for native and independently defined fixed-value entries.</summary>
    public sealed class FixedResourceValueDeclaration
    {
        private string _resource, _unit, _currency;
        public string Catalog { get; private set; }
        public IResourceDefinition Resource => _definition;
        private IResourceContentDefinition _definition;
        public IUnitDefinition CurrencyUnit { get; private set; }
        public Fix PerUnit { get; private set; }
        public void Read(JsonReader r)
        {
            Catalog = r.String("catalog"); _resource = r.String("resource"); _unit = r.String("unit");
            _currency = r.String("currency"); PerUnit = r.Fix("value");
            if (string.IsNullOrWhiteSpace(Catalog) || PerUnit < Fix.Zero)
                throw r.Error("value requires a catalog identity and nonnegative amount");
        }
        public void Resolve(ContentResolver c)
        {
            _definition = c.Def<IResourceContentDefinition>(_resource);
            IUnitContentDefinition unit = c.Def<IUnitContentDefinition>(_unit);
            CurrencyUnit = c.Def<IUnitContentDefinition>(_currency);
            if (unit.Unit != _definition.QuantityUnit.Unit) throw c.Fail("value unit must be the resource's native quantity unit: " + _resource);
            if (CurrencyUnit.Dimension != "currency" || CurrencyUnit.Discrete)
                throw c.Fail("currency must be an explicitly authored continuous currency unit");
        }
    }

    /// <summary>One independently authored value entry; the existing definition registry owns it.</summary>
    public sealed class FixedResourceValueDef : Def, IFixedResourceValueDefinition
    {
        private readonly FixedResourceValueDeclaration _declaration = new FixedResourceValueDeclaration();
        public string Catalog => _declaration.Catalog;
        public IResourceDefinition Resource => _declaration.Resource;
        public IUnitDefinition CurrencyUnit => _declaration.CurrencyUnit;
        public Fix PerUnit => _declaration.PerUnit;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    /// <summary>Single fixed catalog resolution/quote algorithm shared by native and independent policies.</summary>
    public sealed class FixedResourceValuesDeclaration
    {
        private string _currency, _catalog;
        private IUnitContentDefinition _currencyUnit;
        private IReadOnlyList<IFixedResourceValueDefinition> _values;
        public IUnitDefinition CurrencyUnit => _currencyUnit;
        public IReadOnlyList<IFixedResourceValueDefinition> Values => _values;

        public void Read(JsonReader r)
        {
            _currency = r.String("currency");
            _catalog = r.String("catalog");
            if (string.IsNullOrWhiteSpace(_catalog)) throw r.Error("catalog identity must be explicitly authored");
        }
        public void Resolve(ContentResolver c)
        {
            _currencyUnit = c.Def<IUnitContentDefinition>(_currency);
            if (_currencyUnit.Dimension != "currency" || _currencyUnit.Discrete)
                throw c.Fail("currency must be an explicitly authored continuous currency unit");
        }
        public void ValidateResolved(ContentResolver c)
        {
            var values = new List<IFixedResourceValueDefinition>();
            foreach (IFixedResourceValueDefinition entry in c.Content.All<IFixedResourceValueDefinition>())
                if (entry.Catalog == _catalog)
                {
                    if (entry.CurrencyUnit.Unit != CurrencyUnit.Unit) throw c.Fail("catalog currency disagrees with selected policy");
                    values.Add(entry);
                }
            values.Sort((a, b) => a.Resource.Resource.CompareTo(b.Resource.Resource));
            for (int i = 1; i < values.Count; i++)
                if (values[i - 1].Resource.Resource == values[i].Resource.Resource)
                    throw c.Fail("duplicate value for resource '" + values[i].Resource.Id + "'");
            _values = values.AsReadOnly();
            foreach (IResourceContentDefinition resource in c.Content.Resources)
                if (!TryValue(resource.Resource, out _)) throw c.Fail("missing explicit resource value for '" + resource.Id + "'");
        }
        private bool TryValue(ResourceId resource, out IFixedResourceValueDefinition value)
        {
            for (int i = 0; i < _values.Count; i++)
                if (_values[i].Resource.Resource == resource) { value = _values[i]; return true; }
            value = default; return false;
        }
        public IResourceValuation Create(World world, IBusinessValuePolicyDefinition definition)
        {
            if (world == null || !(definition is Def actual) || !actual.IsFrozen ||
                !ReferenceEquals(world.Content.Find<IBusinessValuePolicyDefinition>(definition.Id), definition))
                throw new InvalidOperationException("valuation requires the frozen selected session policy");
            return new FixedValuation(definition, this);
        }
        private sealed class FixedValuation : IResourceValuation
        {
            private readonly IBusinessValuePolicyDefinition _definition;
            private readonly FixedResourceValuesDeclaration _declaration;
            public FixedValuation(IBusinessValuePolicyDefinition definition, FixedResourceValuesDeclaration declaration)
            { _definition = definition; _declaration = declaration; }
            public string Policy => _definition.Id;
            public BusinessMode Mode => _definition.Mode;
            public UnitId Currency => _definition.CurrencyUnit.Unit;
            public ResourceValueQuote Quote(ResourceId resource, Fix nativeQuantity)
            {
                if (!_declaration.TryValue(resource, out IFixedResourceValueDefinition entry))
                    return Result(ResourceValueStatus.UnknownResource);
                if (UnitConversion.ValidateAmount(entry.Resource.QuantityUnit, nativeQuantity) != UnitConversionStatus.Converted)
                    return Result(ResourceValueStatus.InvalidQuantity);
                BigInteger product = (BigInteger)nativeQuantity.Raw * entry.PerUnit.Raw;
                BigInteger raw = BigInteger.DivRem(product, Fix.One.Raw, out BigInteger remainder);
                if (raw > long.MaxValue) return Result(ResourceValueStatus.OutOfRange);
                if (!remainder.IsZero) return Result(ResourceValueStatus.Unrepresentable);
                return Result(ResourceValueStatus.Ready, Fix.FromRaw((long)raw));

                ResourceValueQuote Result(ResourceValueStatus status, Fix amount = default)
                    => new ResourceValueQuote(status, Policy, Mode, resource,
                        entry?.Resource.QuantityUnit.Unit ?? default, Currency, nativeQuantity, entry?.PerUnit ?? default, amount);
            }
        }
    }
    /// <summary>Master-listed fixed defaults. Explicit zero values are valid; absent values are not.</summary>
    public sealed class FixedResourceValuesDef : Def, IBusinessValuePolicyDefinition
    {
        private readonly FixedResourceValuesDeclaration _declaration = new FixedResourceValuesDeclaration();
        public BusinessMode Mode => BusinessMode.FixedDefaults;
        public IUnitDefinition CurrencyUnit => _declaration.CurrencyUnit;
        public IReadOnlyList<IFixedResourceValueDefinition> Values => _declaration.Values;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        protected override void ValidateResolvedCore(ContentResolver content) => _declaration.ValidateResolved(content);
        public IResourceValuation Create(World world) => _declaration.Create(world, this);
    }
}
