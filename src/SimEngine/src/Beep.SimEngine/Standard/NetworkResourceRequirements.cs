using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    /// <summary>Pure source metadata and selection. NetworkService and existing work books retain all stock/claims.</summary>
    public sealed class NetworkResourceSourceDeclaration
    {
        private readonly string _network;
        public INetworkDefinition Network { get; private set; }
        private NetworkResourceSourceDeclaration(JsonReader reader) { _network=reader.String("network"); }
        public static NetworkResourceSourceDeclaration Read(JsonReader reader)
            =>new NetworkResourceSourceDeclaration(reader??throw new ArgumentNullException(nameof(reader)));
        public void Resolve(ContentResolver content,IResourceWorkRequirement requirement)
        {
            if(Network!=null)throw new InvalidOperationException("network resource source resolves once");
            Network=content.Def<INetworkDefinition>(_network);
            foreach(var quantity in requirement.Quantities.Lines)
                if(!Network.Carries(quantity.Resource))throw content.Fail("network resource requirement needs an actually carried resource");
        }
        public void ValidateBinding(World world,IResourceWorkRequirement requirement)
        {
            if(!world.Services.Has<INetworkDiagnostics>()||Network.Line.Layer.Space.Id!=world.Space?.Id)
                throw new ContentException(requirement.Id+": network resource requirement needs its selected spatial topology");
        }
        private Code Connection(WorkRequirementContext context,Entity source,out NetworkConnectionSnapshot snapshot)
        {
            snapshot=default;
            if(source?.Alive!=true)return WorkResourceCodes.MissingSource;
            return context.World.Services.Get<INetworkDiagnostics>().ReadConnection(source.Id,Network.Id,context.Owner,out snapshot);
        }
        public WorkRequirementResult Evaluate(WorkRequirementContext context,Entity source)
        {
            Code refusal=Connection(context,source,out var connection);
            if(refusal.IsNone&&context.CommittedSource.HasValue&&context.CommittedSource.Value!=connection.Endpoint)
                refusal=WorkResourceCodes.MissingSource;
            return refusal.IsNone?WorkRequirementResult.Satisfied():WorkRequirementResult.Pending(refusal);
        }
        public Code CopySources(WorkRequirementContext context,Entity source,IList<WorkResourceSource> destination)
        {
            destination.Clear();
            Code refusal=Connection(context,source,out var connection);
            if(!refusal.IsNone)return refusal;
            if(context.CommittedSource.HasValue&&context.CommittedSource.Value!=connection.Endpoint)
                return WorkResourceCodes.MissingSource;
            destination.Add(new WorkResourceSource(connection.Endpoint,0));return Code.None;
        }
        public WorkRequirementResult CheckSource(WorkRequirementContext context,Entity source,ResourceEndpointId endpoint)
        {
            Code refusal=Connection(context,source,out var connection);
            return refusal.IsNone&&connection.Endpoint==endpoint?WorkRequirementResult.Satisfied():
                WorkRequirementResult.Pending(refusal.IsNone?WorkResourceCodes.MissingSource:refusal);
        }
    }
    public abstract class NetworkResourceRequirementDef : ResourceWorkRequirementDef,IWorkRequirementBindingValidator
    {
        public override IReadOnlyList<IContentDefinition> ContentDependencies
        {
            get
            {
                var references = new List<IContentDefinition>(base.ContentDependencies) { Network, Network.Line, Network.Line.Layer, Network.Line.Layer.Space };
                return references.AsReadOnly();
            }
        }
        private NetworkResourceSourceDeclaration _source;
        public INetworkDefinition Network=>_source.Network;
        protected abstract Entity Source(WorkRequirementContext context);
        protected override void ReadCore(JsonReader reader)
        { base.ReadCore(reader);_source=NetworkResourceSourceDeclaration.Read(reader); }
        protected override void ResolveCore(ContentResolver content)
        { base.ResolveCore(content);_source.Resolve(content,this); }
        public void ValidateBinding(World world)=>_source.ValidateBinding(world,this);
        public override WorkRequirementResult Evaluate(WorkRequirementContext context)=>_source.Evaluate(context,Source(context));
        public override Code CopySources(WorkRequirementContext context,IList<WorkResourceSource> destination)
            =>_source.CopySources(context,Source(context),destination);
        public override WorkRequirementResult CheckSource(WorkRequirementContext context,ResourceEndpointId endpoint)
            =>_source.CheckSource(context,Source(context),endpoint);
    }
    public sealed class TargetNetworkResourceRequirementDef : NetworkResourceRequirementDef
    { protected override Entity Source(WorkRequirementContext context)=>context.Target; }
    public sealed class WorkerNetworkResourceRequirementDef : NetworkResourceRequirementDef,IWorkerWorkRequirement
    { protected override Entity Source(WorkRequirementContext context)=>context.Worker; }
}
