using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Deliberately inconsistent, unused immutable content. Shares the original recipe reader;
    // no simulation system, stock owner or alternate recipe catalog.
    public sealed class TransformationMetadataDef : IndependentTransformationDef
    {
        private string _fault;
        private IResourceDefinition _other;
        protected override void ReadCore(JsonReader reader)
        {
            _fault = reader.String("fault");
            base.ReadCore(reader);
        }
        protected override void ResolveCore(ContentResolver content)
        {
            base.ResolveCore(content); _other = content.Content.Resource(content.Resource("contract.bulk"));
        }
        public override Bill StartResources => _fault == "startup-missing" ? Bill.Empty
            : _fault == "startup-extra" ? new Bill(new[] { new ResourceAmount(_other.Resource, Fix.One) })
            : _fault == "startup-quantity" ? new Bill(new[] { new ResourceAmount(Declaration.Tools[0].Resource, Fix.One) }) : base.StartResources;
        public override Bill ToolConsumption => _fault == "tool-overdraw"
            ? new Bill(new[] { new ResourceAmount(Declaration.Tools[0].Resource, Fix.FromInt(2)) })
            : _fault == "tool-unheld" ? new Bill(new[] { new ResourceAmount(_other.Resource, Fix.One) }) : base.ToolConsumption;
        public override Bill Inputs => _fault == "continuous-bill" ? Declaration.Tools : base.Inputs;
        public override IReadOnlyList<IResourceRate> InputRates => Rates("input", base.InputRates);
        public override IReadOnlyList<IResourceRate> OutputRates => Rates("output", base.OutputRates);
        public override IReadOnlyList<IResourceRate> OperatingInputs => Rates("operating", base.OperatingInputs);
        public override IReadOnlyList<IResourceRate> ToolConsumptionRates => _fault == "batch-rate" ? base.OperatingInputs
            : _fault == "tool-rate-unheld" ? new IResourceRate[] { new ResourceRate(_other, Fix.One, 1, 1, DurationUnit.Minute) }
            : Rates("tool", base.ToolConsumptionRates);
        private IReadOnlyList<IResourceRate> Rates(string direction, IReadOnlyList<IResourceRate> original)
        {
            if (_fault == direction + "-duplicate-rate") return new[] { original[0], original[0] };
            if (_fault.StartsWith("rate-", StringComparison.Ordinal) && direction == "operating") return new IResourceRate[] { new BrokenRate(original[0], _rateFault) };
            if (_fault != direction + "-invalid-rate") return original;
            return new IResourceRate[] { new BrokenRate(original[0], _rateFault) };
        }
        private string _rateFault => _fault.StartsWith("rate-", StringComparison.Ordinal) ? _fault.Substring(5) : "zero";
        public override IReadOnlyList<ResourcePortRoute> InputPorts => Routes("input", base.InputPorts);
        public override IReadOnlyList<ResourcePortRoute> OutputPorts => Routes("output", base.OutputPorts);
        public override IReadOnlyList<ResourcePortRoute> ToolPorts => Routes("tool", base.ToolPorts);
        public override IReadOnlyList<ResourcePortRoute> OperatingPorts => Routes("operating", base.OperatingPorts);
        private IReadOnlyList<ResourcePortRoute> Routes(string direction, IReadOnlyList<ResourcePortRoute> original)
        {
            if (_fault == direction + "-missing-route") return Array.Empty<ResourcePortRoute>();
            if (_fault == direction + "-duplicate-route") return new[] { original[0], original[0] };
            if (_fault == direction + "-unrelated-route") return new[] { new ResourcePortRoute(_other.Resource, original[0].Port) };
            if (_fault == direction + "-invalid-route") return new[] { new ResourcePortRoute(original[0].Resource, new Code(" ")) };
            if (_fault == direction + "-none-route") return new[] { new ResourcePortRoute(original[0].Resource, Code.None) };
            return original;
        }
        // Malformed rate metadata bypasses native construction deliberately; validator must still
        // apply the original ResourceRate invariant and actual content ownership.
        private sealed class BrokenRate : IResourceRate
        {
            private readonly IResourceRate _original; private readonly string _fault;
            internal BrokenRate(IResourceRate original, string fault) { _original = original; _fault = fault; }
            public IResourceDefinition Resource => _fault == "null" ? null : _fault == "foreign" ? new ForeignResource(_original.Resource) : _original.Resource;
            public Fix Quantity => _fault == "zero" ? Fix.Zero : _fault == "negative" ? -Fix.One : _fault == "fraction" ? Fix.FromRaw(1) : _original.Quantity;
            public long PeriodNumerator => _fault == "numerator" ? 0 : _original.PeriodNumerator;
            public long PeriodDenominator => _fault == "denominator" ? 0 : _original.PeriodDenominator;
            public DurationUnit PeriodUnit => _fault == "unit" ? (DurationUnit)255 : _original.PeriodUnit;
        }
        private sealed class ForeignResource : IResourceDefinition
        {
            private readonly IResourceDefinition _original;
            internal ForeignResource(IResourceDefinition original) { _original = original; }
            public string Id => _original.Id;
            public ResourceId Resource => _original.Resource;
            public string Category => _original.Category;
            public IUnitDefinition QuantityUnit => _original.QuantityUnit;
            public Code Phase => _original.Phase;
            public IReadOnlyList<string> Traits => _original.Traits;
            public IReadOnlyList<ResourceCapacityMeasure> CapacityMeasures => _original.CapacityMeasures;
            public bool HasTrait(string trait) => _original.HasTrait(trait);
        }
    }
    public sealed class TransformationMetadataModule : GameModule
    {
        public override string Id => "proof.transformation-metadata";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(TransformationModule.ModuleId, Version) };
        public bool ActualAliases { get; private set; }
        public override void DeclareContent(ContentTypes types) => types.Section<TransformationMetadataDef>("metadata_transformations");
        public override void Compose(WorldComposer composer)
        {
            ActualAliases = true;
            foreach (var recipe in composer.Content.All<TransformationMetadataDef>())
                ActualAliases &= recipe.Resolutions == 1 && composer.Content.Owns(recipe)
                    && ReferenceEquals(recipe, composer.Content.Find<ITransformationDefinition>(recipe.Id));
        }
    }
}
