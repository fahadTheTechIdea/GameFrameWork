using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // Independent authored source-policy proof; existing ports/control/book/WorkBoard own all state.
    public sealed class CommittedResourceSourceModule : GameModule
    {
        public override string Id => "proof.committed-sources";
        public override ModuleVersion Version => new ModuleVersion(1,0,0);
        public override IReadOnlyList<ModuleRequirement> Requires => new[] {
            new ModuleRequirement(WorkModule.ModuleId,Version),new ModuleRequirement(OperationsModule.ModuleId,Version),
            new ModuleRequirement("proof.output-ports",Version) };
        public override void DeclareContent(ContentTypes types) {
            types.Section<GuardedResourceSourceDef>("guarded_resource_sources");
            types.Component<AutomationProbeDef>("source_operation",1);
        }
    }
    public sealed class GuardedResourceSourceDef : ResourceWorkRequirementDef
    {
        private Code _first,_second,_operation,_refusal;
        private string _mode;
        protected override void ReadCore(JsonReader reader) {
            base.ReadCore(reader);_first=new Code(reader.String("first"));_second=new Code(reader.String("second"));
            _operation=new Code(reader.String("operation"));_refusal=new Code(reader.String("refusal"));_mode=reader.String("mode");
            if(_first.IsNone||_second.IsNone||_first==_second||_operation.IsNone||_refusal.IsNone)throw reader.Error("explicit distinct ports/control/refusal required");
            switch(_mode) {
                case "pending":case "blocked":case "missing":case "omit":case "unavailable":
                case "unavailable-candidates":case "duplicate":case "negative-cost":case "invalid-endpoint":
                case "throw-enumerate":case "touch-enumerate":case "clock-enumerate":case "invalid-check":case "throw-check":break;
                default:throw reader.Error("unknown source-policy probe");
            }
        }
        private bool Enabled(WorkRequirementContext context) =>
            context.Target.Get<IControllableOperation>(_operation).ReadControl().Enabled;
        private ResourceEndpointId Endpoint(WorkRequirementContext context,Code port) {
            var provider=context.Target.Get<IResourcePort>(port);
            var resolution=provider.ResolveStore(context.World);
            if(!resolution.Ready)throw new InvalidOperationException("authored proof source unexpectedly unavailable "+resolution.Blocker);
            return resolution.Store.Endpoint;
        }
        public override Code CopySources(WorkRequirementContext context,IList<WorkResourceSource> destination) {
            destination.Clear();
            if(_mode=="throw-enumerate")throw new InvalidOperationException("proof.original-source-enumeration-error");
            if(_mode=="touch-enumerate")context.Target.Touch();
            if(_mode=="clock-enumerate")((Hosting.GameClock)context.World.Clock).Pause();
            if(_mode=="unavailable")return _refusal;
            if(_mode!="omit"||Enabled(context))destination.Add(new WorkResourceSource(
                _mode=="invalid-endpoint"?default:Endpoint(context,_first),_mode=="negative-cost"?-1:0));
            destination.Add(new WorkResourceSource(Endpoint(context,_second),1));
            if(_mode=="duplicate")destination.Add(destination[0]);
            return _mode=="unavailable-candidates"?_refusal:Code.None;
        }
        public override WorkRequirementResult CheckSource(WorkRequirementContext context,ResourceEndpointId endpoint) {
            if(context.CommittedSource.HasValue&&context.CommittedSource.Value!=endpoint)
                throw new InvalidOperationException("proof.committed-alternative-must-not-be-probed");
            if(_mode=="invalid-check")return default;
            if(_mode=="throw-check")throw new InvalidOperationException("proof.original-source-check-error");
            if(!Enabled(context))return _mode=="blocked"?WorkRequirementResult.Blocked(_refusal)
                :WorkRequirementResult.Pending(_mode=="missing"?WorkResourceCodes.MissingSource:_refusal);
            return WorkRequirementResult.Satisfied();
        }
    }
}
