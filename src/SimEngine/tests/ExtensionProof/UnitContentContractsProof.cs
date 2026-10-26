using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentUnitDef : Def, IUnitContentDefinition
    {
        private UnitDeclaration _declaration;
        public UnitId Unit => _declaration.Unit;
        public string Dimension => _declaration.Dimension;
        public long ScaleNumerator => _declaration.ScaleNumerator;
        public long ScaleDenominator => _declaration.ScaleDenominator;
        public bool Discrete => _declaration.Discrete;
        public int Resolutions { get; private set; }
        protected override void ReadCore(JsonReader reader) => _declaration = UnitDeclaration.Read(Id, reader);
        protected override void ResolveCore(ContentResolver content) { Resolutions++; }
    }
    // Deliberately malformed immutable provider metadata; no state writer or world access.
    public sealed class InvalidUnitDef : Def, IUnitContentDefinition
    {
        private UnitDeclaration _declaration;
        private string _mode;
        public UnitId Unit => _mode == "identity" ? default : _declaration.Unit;
        public string Dimension => _mode == "dimension" ? " " : _declaration.Dimension;
        public long ScaleNumerator => _mode == "numerator" ? 0 : _mode == "unreduced" ? 2 : _declaration.ScaleNumerator;
        public long ScaleDenominator => _mode == "denominator" ? 0 : _mode == "unreduced" ? 2 : _declaration.ScaleDenominator;
        public bool Discrete => _declaration.Discrete;
        protected override void ReadCore(JsonReader reader)
        {
            _declaration = UnitDeclaration.Read(Id, reader); _mode = reader.String("mode");
            if (_mode != "identity" && _mode != "dimension" && _mode != "numerator" && _mode != "denominator" && _mode != "unreduced")
                throw reader.Error("explicit invalid unit metadata mode required");
        }
    }
    // Public content composition and detached facts only; no alternate conversion, inventory or workflow.
    public sealed class UnitContractModule : GameModule
    {
        public const string ModuleId = "proof.unit-contracts";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires
            => new[] { new ModuleRequirement(InventoryModule.ModuleId, Version) };
        public bool OwnedAliases { get; private set; }
        public bool OriginalBindings { get; private set; }
        public long NormalizedNumerator { get; private set; }
        public long NormalizedDenominator { get; private set; }
        public IReadOnlyList<UnitConversionResult> Conversions { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<IndependentUnitDef>("independent_units");
            types.Section<InvalidUnitDef>("invalid_units");
        }
        public override void Compose(WorldComposer composer)
        {
            var content = composer.Content; OwnedAliases = true;
            foreach (var unit in content.All<IndependentUnitDef>())
                OwnedAliases &= unit.Resolutions == 1 && content.Owns(unit)
                    && ReferenceEquals(content.Find<IUnitContentDefinition>(unit.Id), unit)
                    && !(unit is IUnitDefinition && (object)unit is UnitDef);
            var supply = content.Resource(content.ResourceId("supply"));
            OriginalBindings = supply.QuantityUnit is IndependentUnitDef
                && ReferenceEquals(supply.QuantityUnit, content.Get<IUnitContentDefinition>("unit.supply"));
            foreach (var measure in supply.CapacityMeasures) OriginalBindings &= measure.Unit is IndependentUnitDef;
            foreach (var limit in content.Get<ICapacityPolicyDefinition>("contract.combined").Limits)
                OriginalBindings &= limit.Unit is IndependentUnitDef;
            var mass = content.Get<IUnitContentDefinition>("unit.capacity.mass");
            var grams = content.Get<IUnitContentDefinition>("unit.contract.g");
            var third = content.Get<IUnitContentDefinition>("unit.contract.third");
            var count = content.Get<IUnitContentDefinition>("unit.capacity.count");
            var pair = content.Get<IUnitContentDefinition>("unit.contract.pair");
            var huge = content.Get<IUnitContentDefinition>("unit.contract.huge");
            var volume = content.Get<IUnitContentDefinition>("unit.capacity.volume");
            NormalizedNumerator = grams.ScaleNumerator; NormalizedDenominator = grams.ScaleDenominator;
            Conversions = Array.AsReadOnly(new[] {
                UnitConversion.Convert(Fix.One, mass, grams),
                UnitConversion.Convert(Fix.FromInt(3), mass, third),
                UnitConversion.Convert(Fix.One, mass, third),
                UnitConversion.Convert(Fix.One, count, pair),
                UnitConversion.Convert(Fix.FromInt(2), count, pair),
                UnitConversion.Convert(Fix.One, huge, mass),
                UnitConversion.Convert(Fix.One, huge, huge),
                UnitConversion.Convert(Fix.One, mass, volume),
                UnitConversion.Convert(-Fix.One, mass, grams),
                UnitConversion.Convert(Fix.Half, count, pair)
            });
        }
    }
}
