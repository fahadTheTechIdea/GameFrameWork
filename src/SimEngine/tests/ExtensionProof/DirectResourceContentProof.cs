using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent metadata-only resource kind. No native ResourceDef, catalog, stock, scheduler or clock.
    public sealed class DirectResourceContentDef : Def, IResourceContentDefinition
    {
        private ResourceDeclaration _value;
        public string Fault { get; private set; }
        public int ResolutionCount { get; private set; }
        public ResourceId Resource => Fault == "identity" ? new ResourceId(int.MaxValue) : _value.Resource;
        public string Category => Fault == "category" ? null : _value.Category;
        public IUnitDefinition QuantityUnit => Fault == "unit" ? new ForeignUnit(_value.QuantityUnit) : _value.QuantityUnit;
        public Code Phase => Fault == "phase" ? Code.None : _value.Phase;
        public IReadOnlyList<string> Traits => Fault == "traits-null" ? null :
            Fault == "traits-duplicate" ? new[] { "proof.direct", "proof.direct" } : _value.Traits;
        public IReadOnlyList<ResourceCapacityMeasure> CapacityMeasures => Fault == "measures-null" ? null :
            Fault == "measures-empty" ? Array.Empty<ResourceCapacityMeasure>() :
            Fault == "measure-default" ? new[] { default(ResourceCapacityMeasure) } :
            Fault == "ambiguous" ? new[] { _value.CapacityMeasures[0], _value.CapacityMeasures[0] } : _value.CapacityMeasures;
        public bool HasTrait(string trait) => Fault != "trait-lookup" && _value.HasTrait(trait);
        protected override void ReadCore(JsonReader reader)
        {
            Fault = reader.String("fault");
            if (Fault != "none" && Fault != "identity" && Fault != "category" && Fault != "unit" && Fault != "phase" &&
                Fault != "traits-null" && Fault != "traits-duplicate" && Fault != "trait-lookup" &&
                Fault != "measures-null" && Fault != "measures-empty" && Fault != "measure-default" && Fault != "ambiguous")
                throw reader.Error("unknown direct resource proof metadata");
            _value = ResourceDeclaration.Read(Id, reader);
        }
        protected override void ResolveCore(ContentResolver resolver)
        { ResolutionCount++; _value.Resolve(resolver); }
        private sealed class ForeignUnit : IUnitContentDefinition
        {
            private readonly IUnitDefinition _value;
            internal ForeignUnit(IUnitDefinition value) { _value = value; }
            public string Id => _value.Unit.Id;
            public string Module => "proof.direct-resources";
            public UnitId Unit => _value.Unit;
            public string Dimension => _value.Dimension;
            public long ScaleNumerator => _value.ScaleNumerator;
            public long ScaleDenominator => _value.ScaleDenominator;
            public bool Discrete => _value.Discrete;
        }
    }
    // Wrong contract negative registration only, not another resource runtime owner.
    public sealed class WrongResourceContentDef : Def, IResourceDefinition
    {
        private ResourceDeclaration _value;
        public ResourceId Resource => _value.Resource;
        public string Category => _value.Category;
        public IUnitDefinition QuantityUnit => _value.QuantityUnit;
        public Code Phase => _value.Phase;
        public IReadOnlyList<string> Traits => _value.Traits;
        public IReadOnlyList<ResourceCapacityMeasure> CapacityMeasures => _value.CapacityMeasures;
        public bool HasTrait(string trait) => _value.HasTrait(trait);
        protected override void ReadCore(JsonReader reader) => _value = ResourceDeclaration.Read(Id, reader);
        protected override void ResolveCore(ContentResolver resolver) => _value.Resolve(resolver);
    }
    public sealed class DirectResourceContentModule : GameModule
    {
        public const string ModuleId = "proof.direct-resources";
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(WorkModule.ModuleId, Version) };
        public bool Composed { get; private set; }
        public bool ActualAliases { get; private set; }
        public bool StableProjection { get; private set; }
        public int CatalogCount { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<DirectResourceContentDef>("contract_resources");
            types.Section<WrongResourceContentDef>("wrong_resource_contracts");
        }
        public override void Compose(WorldComposer composer)
        {
            Composed = true;
            IResourceCatalog catalog = composer.Content;
            var direct = composer.Content.Get<DirectResourceContentDef>("supply");
            ActualAliases = !((object)direct is ResourceDef) && direct.ResolutionCount == 1 &&
                ReferenceEquals(composer.Content.Get<IResourceContentDefinition>("supply"), direct) &&
                ReferenceEquals(composer.Content.Resource(direct.Resource), direct) &&
                ReferenceEquals(catalog.Get(direct.Resource), direct) &&
                composer.Content.ResourceId("supply") == direct.Resource;
            CatalogCount = composer.Content.ResourceCount;
            StableProjection = ReferenceEquals(composer.Content.Resources, composer.Content.Resources) &&
                ReferenceEquals(catalog.Definitions, composer.Content.Resources) && catalog.Definitions.Count == CatalogCount;
            foreach (IResourceContentDefinition resource in composer.Content.Resources)
                ActualAliases &= ReferenceEquals(composer.Content.Get<IResourceContentDefinition>(resource.Id), resource) &&
                    ReferenceEquals(composer.Content.Resource(resource.Resource), resource) &&
                    ReferenceEquals(catalog.Get(resource.Resource), resource) && catalog.TryResolve(resource.Id, out var id) && id == resource.Resource &&
                    (!(resource is DirectResourceContentDef independent) || independent.ResolutionCount == 1);
        }
    }
}
