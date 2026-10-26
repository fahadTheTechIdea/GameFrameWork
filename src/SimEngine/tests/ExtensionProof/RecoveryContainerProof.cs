using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent registration delegates engine authoring; no stock, packing or work implementation.
    public sealed class IndependentRecoveryContainerDef : Def, IResourceRecoveryDefinition
    {
        private ResourceRecoveryDeclaration _declaration;
        private string _fault;
        public int Resolutions { get; private set; }
        public string Space => _fault == "foreign-space" ? "missing" : _declaration.Space;
        public EntityDef Container => _fault == "null-container" ? null : _declaration.Container;
        protected override void ReadCore(JsonReader reader)
        { _fault = reader.Has("metadata_fault") ? reader.String("metadata_fault") : null; _declaration = ResourceRecoveryDeclaration.Read(reader); }
        protected override void ResolveCore(ContentResolver resolver) { _declaration.Resolve(resolver); Resolutions++; }
        protected override void ValidateResolvedCore(ContentResolver resolver) => _declaration.Validate(resolver);
    }

    public sealed class RecoveryContainerProofModule : GameModule
    {
        private readonly InitializeProbe _initialize;
        public RecoveryContainerProofModule(InitializeProbe initialize) { _initialize = initialize; }
        public override string Id => "proof.recovery-containers";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] { new ModuleRequirement(ConstructionModule.ModuleId, Version) };
        public bool ActualAliases { get; private set; }
        public override void DeclareContent(ContentTypes types)
            => types.Section<IndependentRecoveryContainerDef>("independent_recovery_containers");
        public override void Compose(WorldComposer composer)
        {
            var owner = composer.Services.Get<IResourceRecovery>();
            var selected = composer.Content.Get<IResourceRecoveryDefinition>("container.contract");
            ActualAliases = ReferenceEquals(owner, composer.Services.Get<IConstructionProjects>())
                && ReferenceEquals(owner.Definition, selected)
                && ReferenceEquals(selected.Container, composer.Content.Get<EntityDef>("point.recovery-container"))
                && !composer.Services.Has<ConstructionService>();
            if (selected is IndependentRecoveryContainerDef direct) ActualAliases &= direct.Resolutions == 1;
            composer.AddSystem(_initialize, 303);
        }
    }
}
