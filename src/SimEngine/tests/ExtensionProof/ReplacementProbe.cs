using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    // A non-friend engine-side contract/fault probe. No rule is implemented in a host.
    public sealed class ReplacementProbeDef : ComponentDef<ReplacementProbe>
    {
        private string[] _targets;
        private EntityDef[] _resolved;
        public bool FailInitialization { get; private set; }
        public string CheckFault { get; private set; }
        protected override void ReadCore(JsonReader reader)
        {
            JsonValue targets = reader.Array("allowed_targets") ?? throw reader.Error("allowed_targets is required");
            _targets = new string[targets.Count];
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].Kind != JsonKind.String || string.IsNullOrWhiteSpace(targets[i].Text))
                    throw reader.Error("allowed_targets entries must be nonempty identities");
                _targets[i] = targets[i].Text;
            }
            FailInitialization = reader.Bool("fail_initialization");
            CheckFault = reader.String("check_fault", "none");
            bool late = CheckFault.StartsWith("retirement-", StringComparison.Ordinal);
            string fault = late ? CheckFault.Substring("retirement-".Length) : CheckFault;
            if (fault != "none" && fault != "payload" && fault != "payload-deny" &&
                fault != "clock" && fault != "space" && fault != "throw" && !(late && fault == "deny"))
                throw reader.Error("unknown replacement check probe");
        }
        protected override void ResolveCore(ContentResolver content)
        {
            _resolved = new EntityDef[_targets.Length];
            for (int i = 0; i < _targets.Length; i++) _resolved[i] = content.Def<EntityDef>(_targets[i]);
        }
        public bool Allows(EntityDef target)
        {
            foreach (EntityDef allowed in _resolved) if (ReferenceEquals(allowed, target)) return true;
            return false;
        }
        protected override void ValidateCore(EntityDef owner)
        {
            if (CheckFault.StartsWith("retirement-", StringComparison.Ordinal) && owner.Component<SingleResourcePortDef>() == null)
                throw new ContentException(owner.Id + ": late replacement probe requires its actual independent inventory");
        }
    }

    public sealed class ReplacementProbe : Component<ReplacementProbeDef>, IReplacementGuard
    {
        public static readonly Code Denied = new Code("proof.replacement-denied");
        public override void OnCreate()
        { if (Def.FailInitialization) throw new InvalidOperationException("authored initialization fault probe"); }
        public Code CheckReplacement(World world, EntityDef target)
        {
            // Negative extension instrumentation only; authoritative data lives in
            // the companion port, clock and selected space, never in this probe.
            string fault = Def.CheckFault;
            if (fault.StartsWith("retirement-", StringComparison.Ordinal))
            {
                // Negative instrumentation activates only after the REAL source stock
                // has relocated. Positive stock-bearing preflight remains admissible.
                if (Entity.Get<IInventory>().Stock.Count != 0) return Def.Allows(target) ? Code.None : Denied;
                fault = fault.Substring("retirement-".Length);
            }
            if (fault == "payload" || fault == "payload-deny")
                Entity.Get<SingleResourcePort>().FaultServicePayload();
            if (fault == "clock") ((Hosting.GameClock)world.Clock).Pause();
            if (fault == "space") { var space = (PointSpace)world.Space; space.SetAccess(!space.AccessOpen); }
            if (fault == "throw") throw new InvalidOperationException(Def.CheckFault.StartsWith("retirement-", StringComparison.Ordinal)
                ? "authored replacement retirement exception" : "authored replacement preflight exception");
            return fault == "payload-deny" || fault == "deny" || !Def.Allows(target) ? Denied : Code.None;
        }
    }
}
