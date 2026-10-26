using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Independent engine-side requirement; sources are existing real target/worker ports.</summary>
    public sealed class DualInputRequirementDef : ResourceWorkRequirementDef, IWorkerWorkRequirement
    {
        private long _targetCost, _workerCost;
        protected override void ReadCore(JsonReader reader)
        {
            base.ReadCore(reader);
            if (reader.Value("target_cost")?.Kind != JsonKind.Number || reader.Value("worker_cost")?.Kind != JsonKind.Number)
                throw reader.Error("both input source costs must be explicitly authored numbers");
            _targetCost = reader.Long("target_cost"); _workerCost = reader.Long("worker_cost");
            if (_targetCost < 0 || _workerCost < 0) throw reader.Error("input source costs must be nonnegative");
        }
        public override Code CopySources(WorkRequirementContext context, IList<WorkResourceSource> destination)
        {
            destination.Clear();
            IOutletPort target = context.Target?.Get<IOutletPort>(), worker = context.Worker?.Get<IOutletPort>();
            if (target != null) destination.Add(new WorkResourceSource(target.Endpoint, _targetCost));
            if (worker != null && worker.Endpoint != target?.Endpoint) destination.Add(new WorkResourceSource(worker.Endpoint, _workerCost));
            return destination.Count == 0 ? WorkResourceCodes.MissingSource : Code.None;
        }
        public override WorkRequirementResult CheckSource(WorkRequirementContext context, ResourceEndpointId endpoint)
            => context.Target?.Get<IOutletPort>()?.Endpoint == endpoint || context.Worker?.Get<IOutletPort>()?.Endpoint == endpoint
                ? WorkRequirementResult.Satisfied() : WorkRequirementResult.Pending(WorkResourceCodes.MissingSource);
    }
}
