using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    public enum PlaybookStepStatus : byte { Ready, Waiting, CommandPending, Executing, Done, Blocked, Recovering }
    public sealed class PlaybookStepSnapshot
    {
        public string Id { get; }
        public IReadOnlyList<string> Dependencies { get; }
        public int Priority { get; }
        public PlaybookStepStatus Status { get; }
        public Code Blocker { get; }
        public int PendingCommand { get; }
        public long FirstPlan { get; }
        public int PlanCount { get; }
        public IReadOnlyList<PlaybookSiteSnapshot> Sites { get; }
        public PlanSiteCandidate SelectedSite { get; }
        public long BlockedSince { get; }
        public int RecoveryAttempts { get; }
        public bool Recovering { get; }
        public IReadOnlyList<long> CancellationPlans { get; }
        public PlanRecoveryCheck Recovery { get; }
        public PlaybookStepSnapshot(string id, IReadOnlyList<string> dependencies, int priority, PlaybookStepStatus status,
            Code blocker, int pending, long first, int count, IEnumerable<PlaybookSiteSnapshot> sites, PlanSiteCandidate selected,
            long blockedSince, int attempts, bool recovering, IEnumerable<long> cancellation, PlanRecoveryCheck recovery)
        {
            ContractIdentity.ValidateId(id, "playbook step");
            if (dependencies == null || priority < 0 || !Enum.IsDefined(typeof(PlaybookStepStatus), status) || pending < 0 || first < 0 || count <= 0)
                throw new ArgumentException("invalid playbook step facts");
            Id = id; Dependencies = new List<string>(dependencies).AsReadOnly(); Priority = priority; Status = status;
            Blocker = blocker; PendingCommand = pending; FirstPlan = first; PlanCount = count;
            var copy = new List<PlaybookSiteSnapshot>(sites ?? throw new ArgumentNullException(nameof(sites)));
            if (selected != null && (status != PlaybookStepStatus.Ready || !copy.Exists(s => ReferenceEquals(s.Site, selected) && s.Ready)))
                throw new ArgumentException("selected site must be an actual ready candidate");
            Sites = copy.AsReadOnly(); SelectedSite = selected;
            if (blockedSince < -1 || attempts < 0 || cancellation == null) throw new ArgumentException("invalid recovery facts");
            BlockedSince = blockedSince; RecoveryAttempts = attempts; Recovering = recovering;
            CancellationPlans = PlanIntent.CaptureDependencies(cancellation); Recovery = recovery;
            if (recovering != (CancellationPlans.Count > 0)) throw new ArgumentException("cancellation facts require actual in-flight range");
        }
    }
    public interface IPlanPlaybookDiagnostics
    {
        void CopySteps(PlayerView view, Func<IGameCondition, Code> observe,
            Func<IPlanSiteSource, PlanSiteObservation> sites,
            Func<IPlanRecoveryPolicy, PlanRecoveryContext, PlanRecoveryCheck> recovery, IList<PlaybookStepSnapshot> destination);
    }
    public interface IPlayerControllerStateValidation
    {
        void ValidateState(PlayerView view, IReadOnlyList<int> awaiting, IReadOnlyList<CommandResult> answers);
    }
}
