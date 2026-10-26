using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Non-friend, metadata-only extension. No systems, stock, work, receipts, clock or host rules.
    public sealed class NetworkResourceSourceModule : GameModule
    {
        public override string Id=>"proof.network-sources";
        public override ModuleVersion Version=>new ModuleVersion(1,0,0);
        public override IReadOnlyList<ModuleRequirement> Requires=>new[]{new ModuleRequirement(InfrastructureModule.ModuleId,Version)};
        public override void DeclareContent(ContentTypes types)=>types.Section<IndependentNetworkResourceRequirementDef>("independent_network_resources");
    }
    public sealed class IndependentNetworkResourceRequirementDef : Def,IResourceWorkRequirement,IWorkRequirementBindingValidator
    {
        public IReadOnlyList<IContentDefinition> ContentDependencies
        {
            get
            {
                var references = new List<IContentDefinition>(_resources.ContentDependencies) { _source.Network, _source.Network.Line, _source.Network.Line.Layer, _source.Network.Line.Layer.Space };
                return references.AsReadOnly();
            }
        }
        private ResourceRequirementDeclaration _resources;
        private NetworkResourceSourceDeclaration _source;
        public Bill Quantities=>_resources.Quantities;
        public IReadOnlyList<IResourceLotSelectionDefinition> Selections=>_resources.Selections;
        public WorkResourceUse Use=>_resources.Use;
        public IReadOnlyList<IResourceRate> Rates=>_resources.Rates;
        public IReadOnlyList<string> Dependencies=>System.Array.Empty<string>();
        protected override void ReadCore(JsonReader reader)
        { _resources=ResourceRequirementDeclaration.Read(reader);_source=NetworkResourceSourceDeclaration.Read(reader); }
        protected override void ResolveCore(ContentResolver content)
        { _resources.Resolve(content);_source.Resolve(content,this); }
        public void ValidateBinding(World world)=>_source.ValidateBinding(world,this);
        public WorkRequirementResult Evaluate(WorkRequirementContext context)=>_source.Evaluate(context,context.Target);
        public Code CopySources(WorkRequirementContext context,IList<WorkResourceSource> destination)=>_source.CopySources(context,context.Target,destination);
        public WorkRequirementResult CheckSource(WorkRequirementContext context,ResourceEndpointId endpoint)=>_source.CheckSource(context,context.Target,endpoint);
    }
}
