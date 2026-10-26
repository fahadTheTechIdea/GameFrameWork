using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    public class IndependentTransformationDef : Def, ITransformationDefinition
    {
        protected TransformationDeclaration Declaration;
        public virtual IReadOnlyList<ResourcePortRoute> OutputPorts => Declaration.OutputPorts;
        public virtual IReadOnlyList<ResourcePortRoute> InputPorts => Declaration.InputPorts;
        public virtual IReadOnlyList<ResourcePortRoute> ToolPorts => Declaration.ToolPorts;
        public virtual IReadOnlyList<ResourcePortRoute> OperatingPorts => Declaration.OperatingPorts;
        public TransformationMode Mode => Declaration.Mode;
        public virtual IReadOnlyList<IResourceRate> InputRates => Declaration.InputRates;
        public virtual IReadOnlyList<IResourceRate> OutputRates => Declaration.OutputRates;
        public virtual Bill Inputs => Declaration.Inputs;
        public Bill Tools => Declaration.Tools;
        public virtual Bill ToolConsumption => Declaration.ToolConsumption;
        public virtual IReadOnlyList<IResourceRate> ToolConsumptionRates => Declaration.ToolConsumptionRates;
        public virtual Bill StartResources => Declaration.StartResources;
        public virtual Bill Outputs => Declaration.Outputs;
        public IReadOnlyList<IResourceOutputQualityRule> OutputQualities => Declaration.OutputQualities;
        public virtual IReadOnlyList<IResourceRate> OperatingInputs => Declaration.OperatingInputs;
        public virtual EntityDef EntityOutput => Declaration.EntityOutput;
        public virtual TransformationOutputHome? OutputHome => Declaration.OutputHome;
        public GameDuration Duration => Declaration.Duration;
        public virtual Fix Cash => Declaration.Cash;
        public virtual string Category => Declaration.Category;
        public Code CancelPolicy => Declaration.CancelPolicy;
        public int Resolutions { get; private set; }
        protected override void ReadCore(JsonReader reader) => Declaration = TransformationDeclaration.Read(reader);
        protected override void ResolveCore(ContentResolver content) { Declaration.Resolve(content); Resolutions++; }
    }
    // Deliberately invalid immutable metadata; not an alternate recipe reader or gameplay system.
    public sealed class InvalidTransformationDef : IndependentTransformationDef
    {
        public override string Category => " ";
    }
    public sealed class TransformationContractModule : GameModule
    {
        public override string Id => "proof.transformation-contracts";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override IReadOnlyList<ModuleRequirement> Requires
            => new[] { new ModuleRequirement(TransformationModule.ModuleId, Version) };
        public bool ActualAliases { get; private set; }
        public bool OriginalNativeBinding { get; private set; }
        public override void DeclareContent(ContentTypes types)
        {
            types.Section<IndependentTransformationDef>("independent_transformations");
            types.Section<InvalidTransformationDef>("invalid_transformations");
        }
        public override void Compose(WorldComposer composer)
        {
            ActualAliases = true;
            foreach (var recipe in composer.Content.All<IndependentTransformationDef>())
                ActualAliases &= recipe.Resolutions == 1 && composer.Content.Owns(recipe)
                    && ReferenceEquals(recipe, composer.Content.Find<ITransformationDefinition>(recipe.Id));
            var bound = composer.Content.Get<EntityDef>("contract.factory").Component<ITransformerDefinition>(new Code("contract.process"));
            OriginalNativeBinding = bound.Transformations.Count == 1 && bound.Transformations[0] is IndependentTransformationDef
                && ReferenceEquals(bound.Transformations[0], composer.Content.Get<ITransformationDefinition>("contract.make"));
        }
    }
}
