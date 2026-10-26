using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Standard
{
    public interface IPlanRecoveryPolicy : IContentDefinition
    {
        void ValidateBinding(World world, PlayerId company);
        PlanRecoveryCheck Observe(PlanRecoveryContext context);
    }
    public abstract class PlanRecoveryPolicyDef : Def, IPlanRecoveryPolicy
    {
        public abstract void ValidateBinding(World world, PlayerId company);
        public abstract PlanRecoveryCheck Observe(PlanRecoveryContext context);
    }
    /// <summary>Detached actual linked plans and controller chronology, never mutable execution.</summary>
    public sealed class PlanRecoveryContext
    {
        public PlayerView View { get; }
        public IReadOnlyList<PlanView> Plans { get; }
        public long BlockedSince { get; }
        public int Attempts { get; }
        public PlanRecoveryContext(PlayerView view, IEnumerable<PlanView> plans, long blockedSince, int attempts)
        {
            View = view ?? throw new ArgumentNullException(nameof(view));
            if (blockedSince < -1 || blockedSince > view.Tick || attempts < 0) throw new ArgumentException("invalid recovery chronology");
            var copy = new List<PlanView>(plans ?? throw new ArgumentNullException(nameof(plans)));
            var ids = new HashSet<long>();
            foreach (var plan in copy)
                if (plan.Id <= 0 || plan.Intent == null || !ids.Add(plan.Id)) throw new ArgumentException("recovery needs distinct actual linked plans");
            Plans = copy.AsReadOnly(); BlockedSince = blockedSince; Attempts = attempts;
        }
    }
    public readonly struct PlanRecoveryCheck
    {
        public bool Eligible { get; }
        public bool Ready { get; }
        public Code Blocker { get; }
        public PlanRecoveryCheck(bool eligible, bool ready, Code blocker)
        {
            if (ready && (!eligible || !blocker.IsNone)) throw new ArgumentException("ready recovery requires eligibility without blocker");
            if (!blocker.IsNone) ContractIdentity.ValidateId(blocker.Id, "recovery blocker");
            Eligible = eligible; Ready = ready; Blocker = blocker;
        }
    }
    /// <summary>Pure authored policy for continuously blocked, unpaused, wholly unstarted plans.</summary>
    public sealed class PendingPlanRecoveryDef : PlanRecoveryPolicyDef
    {
        private GameDuration _delay;
        private int _maximum;
        private readonly HashSet<string> _blockers = new HashSet<string>(StringComparer.Ordinal);
        protected override void ReadCore(JsonReader reader)
        {
            _delay = (reader.Child("delay") ?? throw reader.Error("explicit recovery delay required")).RationalDuration();
            _maximum = reader.Int("maximum_recoveries");
            if (_delay.IsZero || _maximum <= 0 || reader.Array("blockers") == null) throw reader.Error("positive delay/maximum and explicit blockers required");
            foreach (string id in reader.Strings("blockers"))
            {
                try { ContractIdentity.ValidateId(id, "recovery blocker"); }
                catch (ArgumentException error) { throw reader.Error(error.Message); }
                if (!_blockers.Add(id)) throw reader.Error("duplicate recovery blocker");
            }
            if (_blockers.Count == 0) throw reader.Error("nonempty recovery blockers required");
        }
        public override void ValidateBinding(World world, PlayerId company)
        {
            if (!world.Players.IsSeat(company) || !world.Services.Has<IPlanBoard>() || !world.Commands.TryGet(typeof(CancelPlanCommand), out _))
                throw new ContentException(Id + ": recovery needs actual seat and native plan cancellation");
            try { _ = world.Time.Ticks(_delay); }
            catch (OverflowException) { throw new ContentException(Id + ": recovery delay exceeds session representation"); }
        }
        public override PlanRecoveryCheck Observe(PlanRecoveryContext context)
        {
            bool blocked = false;
            foreach (var plan in context.Plans)
            {
                if (plan.State != PlanState.Pending || plan.Paused) return new PlanRecoveryCheck(false, false, PlanCodes.NotPending);
                blocked |= _blockers.Contains(plan.Blocker.Id ?? "");
            }
            if (!blocked) return new PlanRecoveryCheck(false, false, Code.None);
            if (context.Attempts >= _maximum) return new PlanRecoveryCheck(false, false, new Code("controller.recovery-exhausted"));
            long delay = context.View.SimulationTime.Ticks(_delay);
            bool ready = context.BlockedSince >= 0 && context.View.Tick - context.BlockedSince >= delay;
            return new PlanRecoveryCheck(true, ready, ready ? Code.None : new Code("controller.recovery-wait"));
        }
    }
    internal static class PlanRecoveryContracts
    {
        internal static PlanRecoveryCheck Read(World world, IPlanRecoveryPolicy policy, PlanRecoveryContext context)
        {
            if (policy == null || !world.Content.Owns(policy) || !ReferenceEquals(world.Content.Find<IPlanRecoveryPolicy>(policy.Id), policy))
                throw new ArgumentException("recovery policy must be an unambiguous actual session content alias");
            if (context == null || context.View.IsObserver || !world.Players.IsSeat(context.View.Viewer))
                return new PlanRecoveryCheck(false, false, Codes.NotSeated);
            if (!context.View.CanSeeContent(policy)) return new PlanRecoveryCheck(false, false, Codes.UnknownDef);
            return EntityObservation.ReadProvider(world, policy, null, () => policy.Observe(context));
        }
    }
}
