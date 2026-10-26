using Beep.SimEngine.Content;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // An unrelated game-side preference provider; engine still owns all safety and selection.
    public sealed class ProofFacilityPolicyDef : Def, IActivityFacilityPolicy
    {
        private string _node;
        public string Node => _node;
        public bool PreferLater { get; private set; }
        public int Fault { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            _node = reader.String("node"); PreferLater = reader.Bool("prefer_later"); Fault = reader.Int("fault");
            if (Fault < 0 || Fault > 2) throw reader.Error("explicit facility proof fault range required");
        }
        public long? Rank(ActivityFacilityFacts facts) => Fault == 1 ? -1 : Fault == 2 ? (long?)null :
            PreferLater ? int.MaxValue - (long)facts.Target.Value : facts.Target.Value;
    }
}
