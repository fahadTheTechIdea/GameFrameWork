using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public sealed class ProofResourceQualityDef : Def, IResourceQualityDefinition
    {
        private ResourceQualityDeclaration _declaration;
        public ResourceId Resource => _declaration.Resource;
        public IUnitDefinition Unit => _declaration.Unit;
        public Fix Minimum => _declaration.Minimum;
        public Fix Maximum => _declaration.Maximum;
        protected override void ReadCore(JsonReader reader) => _declaration = ResourceQualityDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    public sealed class ProofResourceLotDef : Def, IResourceLotDefinition
    {
        private ResourceLotDeclaration _declaration;
        public ResourceId Resource => _declaration.Resource;
        public IReadOnlyDictionary<string, Fix> Qualities => _declaration.Qualities;
        protected override void ReadCore(JsonReader reader) => _declaration = ResourceLotDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    public sealed class ProofResourceLotSelectionDef : Def, IResourceLotSelectionDefinition
    {
        private ResourceLotSelectionDeclaration _declaration;
        public ResourceId Resource => _declaration.Resource;
        public Code Lot => _declaration.Lot;
        public IReadOnlyList<ResourceQualityRange> Qualities => _declaration.Qualities;
        protected override void ReadCore(JsonReader reader) => _declaration = ResourceLotSelectionDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    /// <summary>Authoring registration only; no inventory, transfer or game rule implementation.</summary>
    public sealed class IndependentResourceFlowQueryDef : Def, IResourceFlowQueryDefinition
    {
        private ResourceFlowQueryDeclaration _declaration;
        public ResourceId Resource => _declaration.Resource;
        public IReadOnlyList<ResourceFlowKind> Kinds => _declaration.Kinds;
        public GameDuration Window => _declaration.Window;
        public DurationUnit Per => _declaration.Per;
        public IResourceLotSelectionDefinition Selection => _declaration.Selection;
        protected override void ReadCore(JsonReader reader) => _declaration = ResourceFlowQueryDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) => _declaration.Resolve(content);
    }
    public sealed class ResourceLotProofModule : GameModule
    {
        public override string Id => "proof.resource-lots";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(InventoryModule.ModuleId, Version) };
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<ProofResourceQualityDef>("proof_resource_qualities");
            types.Section<ProofResourceLotDef>("proof_resource_lots");
            types.Section<ProofResourceLotSelectionDef>("proof_resource_selections");
            types.Section<IndependentTransformationDef>("proof_lot_recipes");
            types.Section<IndependentResourceFlowQueryDef>("proof_flow_queries");
        }
    }
}
