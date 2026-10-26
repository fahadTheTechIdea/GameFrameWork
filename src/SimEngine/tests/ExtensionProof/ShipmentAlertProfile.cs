using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>Public-only content extension. It contributes no queue, state owner or scheduler.</summary>
    public sealed class ShipmentAlertProfileDef : CompanyAlertProfileDef
    {
        private static readonly IReadOnlyList<string> Scopes = new List<string> { "proof.shipments" }.AsReadOnly();
        private int _normal, _urgent, _emergency;
        public override IReadOnlyList<string> DispatchScopes => Scopes;
        protected override void ReadCore(JsonReader reader)
        { _normal = reader.Int("normal"); _urgent = reader.Int("urgent"); _emergency = reader.Int("emergency"); }
        public override long PriorityAdjustment(CompanyAlertLevel level, WorkerDispatchOffer offer)
            => offer.Scope != "proof.shipments" || offer.Emergency ? 0 : level == CompanyAlertLevel.Normal ? _normal
                : level == CompanyAlertLevel.Urgent ? _urgent : _emergency;
    }
}
