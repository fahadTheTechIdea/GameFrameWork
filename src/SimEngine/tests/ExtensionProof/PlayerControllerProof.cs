using System;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.ExtensionProof
{
    /// <summary>External engine-side provider; public contracts, ordinary commands and no scheduler changes.</summary>
    public class PlayerControllerProofModule : PlayerControllersModule
    {
        public override void DeclareContent(ContentTypes types)
        {
            base.DeclareContent(types); types.Section<ReplyControllerDef>("reply_controllers");
            types.Section<DelayedPlanSitesDef>("delayed_plan_sites");
            types.Section<RecoveryRelayDef>("recovery_relays");
        }
    }
    /// <summary>Independent contract policy; JSON selects the existing authored native rule.</summary>
    public sealed class RecoveryRelayDef : Def, IPlanRecoveryPolicy
    {
        private string _policyId, _mode;
        private PendingPlanRecoveryDef _policy;
        protected override void ReadCore(JsonReader reader)
        {
            _policyId = reader.String("policy"); _mode = reader.String("mode");
            if (_mode != "normal" && _mode != "throw") throw reader.Error("explicit recovery proof mode required");
        }
        protected override void ResolveCore(ContentResolver resolver) => _policy = resolver.Def<PendingPlanRecoveryDef>(_policyId);
        public void ValidateBinding(World world, PlayerId company) => _policy.ValidateBinding(world, company);
        public PlanRecoveryCheck Observe(PlanRecoveryContext context)
        {
            if (_mode == "throw") throw new InvalidOperationException("proof.recovery-fault");
            return _policy.Observe(context);
        }
    }
    /// <summary>Public pure source: authored sites become known after explicit game time.</summary>
    public sealed class DelayedPlanSitesDef : Def, IPlanSiteSource
    {
        private string _sitesId;
        private AuthoredPlanSitesDef _sites;
        private GameDuration _after;
        protected override void ReadCore(JsonReader reader)
        {
            _sitesId = reader.String("site_set");
            _after = (reader.Child("after") ?? throw reader.Error("explicit duration required")).RationalDuration();
        }
        protected override void ResolveCore(ContentResolver content) => _sites = content.Def<AuthoredPlanSitesDef>(_sitesId);
        public void ValidateBinding(World world, PlayerId company, PlanTemplate template, string group)
        {
            _sites.ValidateBinding(world, company, template, group);
            try { _ = world.Time.Ticks(_after); }
            catch (OverflowException) { throw new ContentException(Id + ": site availability exceeds session time"); }
        }
        public PlanSiteObservation Observe(PlayerView view, Func<IGameCondition, Code> observe)
            => view.Tick < view.SimulationTime.Ticks(_after)
                    ? new PlanSiteObservation(new Code("proof.sites-undiscovered"), Array.Empty<PlanSiteCandidate>()) : _sites.Observe(view, observe);
    }
    public sealed class ReplyControllerDef : PlayerControllerDef
    {
        private string _mode;
        protected override void ReadCore(JsonReader reader)
        {
            base.ReadCore(reader); _mode = reader.String("mode");
            if (_mode != "refusal" && _mode != "budget" && _mode != "expired") throw reader.Error("unknown reply proof mode");
        }
        public override IPlayerController Create() => new ReplyController(_mode);
        private sealed class ReplyController : PlayerController, IPlayerControllerState
        {
            private readonly string _mode;
            private int _pending;
            private bool _answered;
            private PlayerDecisionContext _previous;
            public ReplyController(string mode) { _mode = mode; }
            public int StateSchemaVersion => 1;
            public override void Decide(PlayerDecisionContext context)
            {
                if (_answered) return;
                if (_mode == "expired")
                {
                    if (_previous != null) _previous.Submit(new SetControllerCommand { Controller = Controller.Human });
                    _previous = context; return;
                }
                if (_pending == 0)
                {
                    _pending = context.Submit(new SetControllerCommand { Controller = (Controller)200 });
                    if (_mode == "budget") context.Submit(new SetControllerCommand { Controller = Controller.Human });
                    return;
                }
                foreach (CommandResult answer in context.Answers)
                    if (answer.Sequence == _pending)
                    { _pending = 0; _answered = true; context.ReportBlocker(answer.Refusal); return; }
                context.ReportBlocker(new Code("proof.awaiting-reply"));
            }
            public void Sync(IStateSync sync)
            {
                sync.Int(ref _pending); sync.Bool(ref _answered);
                if (_pending < 0 || _answered && _pending != 0) throw new CorruptStateException("invalid reply proof state");
            }
        }
    }
}
