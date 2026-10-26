using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Independent public-only content policy; contributes no queue, job, claims or age store.</summary>
    public sealed class ShipmentDispatchProfileDef : DispatchProfileDef, IWorkerDispatchPreemptionPolicy
    {
        private static readonly IReadOnlyList<string> Scopes = new List<string> { "proof.shipments", "sim.work", "sim.logistics" }.AsReadOnly();
        private GameDuration _work, _shipment;
        private string _fault;
        private bool _preempt;
        private WorkerDispatchOrdering _ordering;
        public override WorkerDispatchOrdering Ordering => _ordering;
        public override IReadOnlyList<string> DispatchScopes => Scopes;
        protected override void ReadCore(JsonReader reader)
        {
            _preempt = reader.Bool("preempt_emergencies");
            _ordering = DispatchPolicyDeclaration.ReadOrdering(reader);
            _work = reader.Child("work_after").RationalDuration();
            _shipment = reader.Child("shipment_after").RationalDuration();
            _fault = reader.String("fault");
            if (_work.IsZero || _shipment.IsZero || _fault != "none" && _fault != "zero" && _fault != "negative" && _fault != "undeclared")
                throw reader.Error("explicit positive thresholds and named proof fault required");
        }
        public override void ValidateTime(ISimulationTime time)
        {
            if (time.Ticks(_work) <= 0 || time.Ticks(_shipment) <= 0) throw new ContentException("positive session thresholds required");
        }
        public bool CanPreempt(WorkerDispatchOffer challenger, WorkExecutionFacts incumbent) => _preempt;
        public override long? StarvationAfter(ISimulationTime time, WorkerDispatchOffer offer)
        {
            if (offer.AgeTicks >= 2 && _fault == "undeclared" && offer.Scope == LogisticsModule.ModuleId) return 1;
            if (offer.Scope != WorkModule.ModuleId && offer.Scope != "proof.shipments") return null;
            if (offer.AgeTicks >= 2 && _fault == "zero") return 0;
            if (offer.AgeTicks >= 2 && _fault == "negative") return -1;
            return time.Ticks(offer.Scope == WorkModule.ModuleId ? _work : _shipment);
        }
    }
}
