using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent JSON metadata, sharing the engine's sole authoring and execution algorithms.
    public sealed class IndependentBusinessValueDef : Def, IBusinessValuePolicyDefinition
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
    public sealed class IndependentResourceValueDef : Def, IFixedResourceValueDefinition
    {
        private readonly FixedResourceValueDeclaration _declaration = new FixedResourceValueDeclaration();
        public string Catalog => _declaration.Catalog;
        public IResourceDefinition Resource => _declaration.Resource;
        public IUnitDefinition CurrencyUnit => _declaration.CurrencyUnit;
        public Fix PerUnit => _declaration.PerUnit;
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    public sealed class IndependentProductionIncomeDef : Def, IProductionIncomePolicyDefinition
    {
        private readonly PrimaryProductionIncomeDeclaration _declaration = new PrimaryProductionIncomeDeclaration();
        protected override void ReadCore(JsonReader reader) => _declaration.Read(reader);
        public IProductionIncomePolicy Create(World world) => _declaration.Create(world, this);
    }
}
