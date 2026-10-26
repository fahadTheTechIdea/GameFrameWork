using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class IndependentCapacityDef : Def, ICapacityPolicyDefinition
    {
        private readonly CapacityPolicyDeclaration _declaration = new CapacityPolicyDeclaration();
        private string _trait;
        public IReadOnlyList<CapacityLimit> Limits => _declaration.Limits;
        protected override void ReadCore(JsonReader reader)
        {
            _declaration.Read(reader);
            JsonValue trait = reader.Value("required_trait") ?? throw reader.Error("required_trait must be authored");
            if (trait.Kind != JsonKind.Null && trait.Kind != JsonKind.String) throw reader.Error("required_trait must be null or a string");
            _trait = trait.Kind == JsonKind.Null ? null : trait.Text;
            if (_trait != null && string.IsNullOrWhiteSpace(_trait)) throw reader.Error("required_trait is null or an explicit trait");
        }
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
        private CapacityStatus Compatible(IResourceDefinition resource)
            => _trait == null || resource.HasTrait(_trait) ? CapacityStatus.Fits : CapacityStatus.ResourceRejected;
        public CapacityCheck Check(IReadOnlyList<ResourceAmount> stock, IReadOnlyList<ResourceAmount> promised,
            IReadOnlyList<ResourceAmount> incoming, long instances = 1)
            => _declaration.Check(stock, promised, incoming, instances, Compatible);
        public CapacityQuantum Quantum(ResourceId resource) => _declaration.Quantum(this, resource);
        public CapacityOffer Offer(IReadOnlyList<ResourceAmount> stock, IReadOnlyList<ResourceAmount> promised,
            ResourceId resource, Fix ceiling, long instances = 1)
            => _declaration.Offer(this, stock, promised, resource, ceiling, instances);
    }

    // Composition and detached diagnostics only. Original inventories/WIP/physical coordinators execute.
    public sealed class CapacityContractModule : GameModule
    {
        public const string ModuleId = "proof.capacity-contracts";
        private readonly InitializeProbe _initialize;
        public CapacityContractModule(InitializeProbe initialize) { _initialize = initialize; }
        public override string Id => ModuleId;
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires
            => new[] { new ModuleRequirement(InventoryModule.ModuleId, Version) };
        public bool IndependentAlias { get; private set; }
        public bool OriginalBindings { get; private set; }
        public bool NativeParity { get; private set; }
        public CapacityCheck Mixed { get; private set; }
        public CapacityOffer MixedOffer { get; private set; }
        public CapacityStatus RejectedOffer { get; private set; }
        public CapacityStatus RejectedQuantum { get; private set; }
        public CapacityQuantum FractionalQuantum { get; private set; }
        public CapacityStatus FractionalCheck { get; private set; }
        public CapacityOffer FractionalOffer { get; private set; }
        public CapacityStatus ZeroInstances { get; private set; }
        public CapacityStatus WideInstances { get; private set; }
        public CapacityStatus Closed { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<IndependentCapacityDef>("independent_capacity_policies");
            types.Section<TraitCapacityPolicyDef>("trait_capacity_policies");
        }
        public override void Compose(WorldComposer composer)
        {
            var content = composer.Content;
            var combined = content.Get<ICapacityPolicyDefinition>("contract.combined");
            IndependentAlias = combined is IndependentCapacityDef && !(combined is CapacityPolicyDef)
                && content.Owns(combined) && ReferenceEquals(content.Find<ICapacityPolicyDefinition>(combined.Id), combined);
            OriginalBindings = content.Get<EntityDef>("machine").Component<IInventoryDefinition>().CapacityPolicy is IndependentCapacityDef
                && content.Get<EntityDef>("contract.factory").Component<IInventoryDefinition>().CapacityPolicy is IndependentCapacityDef;
            var empty = Array.Empty<ResourceAmount>();
            var bulk = content.ResourceId("contract.bulk"); var fluid = content.ResourceId("contract.fluid");
            var heavy = content.ResourceId("contract.heavy");
            var stock = new[] { new ResourceAmount(bulk, Fix.FromInt(2)), new ResourceAmount(fluid, Fix.One) };
            var promised = new[] { new ResourceAmount(heavy, Fix.One) };
            Mixed = combined.Check(stock, promised, empty);
            MixedOffer = combined.Offer(stock, promised, bulk, Fix.FromInt(100));
            var native = content.Get<ICapacityPolicyDefinition>("contract.native-combined");
            NativeParity = native.Check(stock, promised, empty).Status == Mixed.Status
                && native.Offer(stock, promised, bulk, Fix.FromInt(100)).Amount == MixedOffer.Amount
                && native.Quantum(bulk).Raw == combined.Quantum(bulk).Raw;
            var trait = content.Get<ICapacityPolicyDefinition>("contract.bulk-only");
            var nativeTrait = content.Get<ICapacityPolicyDefinition>("contract.native-bulk");
            RejectedOffer = trait.Offer(empty, empty, fluid, Fix.One).Status;
            RejectedQuantum = trait.Quantum(fluid).Status;
            NativeParity &= nativeTrait.Offer(empty, empty, fluid, Fix.One).Status == RejectedOffer
                && nativeTrait.Quantum(fluid).Status == RejectedQuantum;
            var fractional = content.ResourceId("contract.fractional");
            var slots = content.Get<ICapacityPolicyDefinition>("contract.slots");
            FractionalQuantum = slots.Quantum(fractional);
            FractionalCheck = slots.Check(empty, empty, new[] { new ResourceAmount(fractional, Fix.One) }).Status;
            FractionalOffer = slots.Offer(empty, empty, fractional, Fix.FromInt(3));
            ZeroInstances = combined.Check(empty, empty, empty, 0).Status;
            WideInstances = combined.Check(empty, empty, empty, long.MaxValue).Status;
            Closed = content.Get<ICapacityPolicyDefinition>("contract.closed").Offer(empty, empty, heavy, Fix.One).Status;
            composer.AddSystem(_initialize, 301);
        }
    }
}
