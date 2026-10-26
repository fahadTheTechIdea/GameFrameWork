using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Standard
{
    /// <summary>Authored strategic dependencies over actual native plans; not a second plan executor.</summary>
    public sealed class PlanPlaybookDef : PlayerControllerDef
    {
        private sealed class Step : IDependencyNode
        {
            public string Id { get; set; }
            public IReadOnlyList<string> Dependencies { get; set; }
            public int Priority;
            public string TemplateId, Group, SiteSourceId;
            public string[] ConditionIds;
            public IPlanTemplateContent Template;
            public IReadOnlyList<IGameCondition> Conditions;
            public IPlanSiteSource SiteSource;
            public string RecoveryId;
            public IPlanRecoveryPolicy Recovery;
        }
        private DependencyGraph<Step> _graph;
        private IReadOnlyList<Step> _priority;
        protected override void ReadCore(JsonReader reader)
        {
            base.ReadCore(reader);
            JsonValue rows = reader.Array("steps") ?? throw reader.Error("steps required");
            if (rows.Count == 0) throw reader.Error("playbook needs explicit nonempty steps");
            var steps = new List<Step>(); var groups = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < rows.Count; i++)
            {
                var row = new JsonReader(rows[i], reader.Where + ".steps[" + i + "]");
                if (row.Array("after") == null || row.Array("conditions") == null) throw row.Error("explicit after and conditions arrays required");
                var step = new Step { Id = row.String("id"), TemplateId = row.String("template"), Group = row.String("group"),
                    Priority = row.Int("priority"), Dependencies = Array.AsReadOnly(row.Strings("after")),
                    ConditionIds = row.Strings("conditions"), SiteSourceId = row.String("sites") };
                JsonValue recovery = row.Value("recovery") ?? throw row.Error("explicit recovery null/profile required");
                if (recovery.Kind != JsonKind.Null && recovery.Kind != JsonKind.String) throw row.Error("recovery must be null or profile ID");
                step.RecoveryId = recovery.Kind == JsonKind.Null ? null : row.String("recovery");
                row.End();
                try { ContractIdentity.ValidateId(step.Id, "playbook step"); ContractIdentity.ValidateId(step.Group, "playbook group"); }
                catch (ArgumentException error) { throw row.Error(error.Message); }
                if (step.Priority < 0 || !groups.Add(step.Group)) throw row.Error("invalid priority or duplicate playbook group");
                Array.Sort(step.ConditionIds, StringComparer.Ordinal);
                for (int j = 0; j < step.ConditionIds.Length; j++)
                    if (string.IsNullOrEmpty(step.ConditionIds[j]) || j > 0 && step.ConditionIds[j] == step.ConditionIds[j - 1])
                        throw row.Error("strategy conditions must have distinct explicit identities");
                steps.Add(step);
            }
            _graph = new DependencyGraph<Step>(steps, Id);
            steps.Sort((a, b) => { int priority = b.Priority.CompareTo(a.Priority); return priority != 0 ? priority : string.CompareOrdinal(a.Id, b.Id); });
            _priority = steps.AsReadOnly();
        }
        protected override void ResolveCore(ContentResolver resolver)
        {
            foreach (Step step in _graph.Nodes)
            {
                step.Template = resolver.Def<IPlanTemplateContent>(step.TemplateId);
                step.SiteSource = resolver.Def<IPlanSiteSource>(step.SiteSourceId);
                if (step.RecoveryId != null) step.Recovery = resolver.Def<IPlanRecoveryPolicy>(step.RecoveryId);
                var conditions = new List<IGameCondition>();
                foreach (string id in step.ConditionIds) conditions.Add(resolver.Def<IGameCondition>(id));
                step.Conditions = conditions.AsReadOnly(); step.ConditionIds = null;
            }
        }
        public override void ValidateBinding(World world, PlayerId company)
        {
            if (!world.Services.TryGet<IPlanBoard>(out var board) || !world.Commands.TryGet(typeof(PlacePlanTemplateCommand), out _)
                || !world.ViewFeatures.ContainsKey(typeof(WorkView)))
                throw new ContentException("plan playbook requires the selected native plan board/command");
            foreach (Step step in _graph.Nodes)
            {
                if (step.Template.Template.Space != world.Space?.Id) throw new ContentException("playbook template belongs to an unselected space");
                EntityObservation.ReadProvider(world, step.SiteSource, null,
                    () => { step.SiteSource.ValidateBinding(world, company, step.Template.Template, step.Group); return 0; });
                foreach (var condition in step.Conditions) GameConditionContracts.ValidateBinding(world, company, condition);
                if (step.Recovery != null)
                {
                    EntityObservation.ReadProvider(world, step.Recovery, null,
                        () => { step.Recovery.ValidateBinding(world, company); return 0; });
                }
            }
        }
        public override IPlayerController Create() => new Playbook(this);
        private sealed class Playbook : PlayerController, IPlayerControllerState, IPlanHistoryRetention,
            IPlanPlaybookDiagnostics, IPlayerControllerStateValidation
        {
            private sealed class Execution
            {
                public Step Definition;
                public int Pending;
                public long First;
                public long BlockedSince = -1;
                public int RecoveryAttempts;
                public bool Recovering;
                public int Count => Definition.Template.Template.Entries.Count;
            }
            private readonly PlanPlaybookDef _definition;
            private readonly SortedDictionary<string, Execution> _executions = new SortedDictionary<string, Execution>(StringComparer.Ordinal);
            public Playbook(PlanPlaybookDef definition)
            {
                _definition = definition;
                foreach (var step in definition._graph.Nodes) _executions.Add(step.Id, new Execution { Definition = step });
            }
            public int StateSchemaVersion => 5;
            public bool RetainsPlan(World world, PlayerId company, long plan)
            {
                foreach (var execution in _executions.Values)
                    if (execution.First > 0 && plan >= execution.First && plan - execution.First < execution.Count) return true;
                return false;
            }
            public override void Decide(PlayerDecisionContext context)
            {
                var refused = new HashSet<string>(StringComparer.Ordinal); Code firstRefusal = Code.None;
                foreach (var execution in _executions.Values)
                    if (execution.Pending > 0)
                        foreach (CommandResult answer in context.Answers)
                            if (answer.Sequence == execution.Pending)
                            {
                                string expected = execution.Recovering ? "sim.cancel-plan" : "sim.place-plan-template";
                                if (answer.Player != context.Id || answer.Command != expected)
                                    throw new InvalidOperationException("playbook received mismatched actual command answer");
                                if (!answer.Accepted)
                                { refused.Add(execution.Definition.Id); if (firstRefusal.IsNone) firstRefusal = answer.Refusal; }
                                else if (execution.Recovering)
                                {
                                    var linked = Linked(execution, Plans(context.View));
                                    if (answer.Arg0 != execution.First || answer.Arg1 != execution.Count ||
                                        linked.Count != execution.Count || !linked.TrueForAll(p => p.State == PlanState.Cancelled))
                                        throw new InvalidOperationException("cancellation answer differs from actual whole linked plans");
                                    execution.First = 0;
                                }
                                else
                                {
                                    if (answer.Arg0 <= 0 || answer.Arg1 != execution.Count || answer.Arg0 > long.MaxValue - (execution.Count - 1))
                                        throw new InvalidOperationException("accepted playbook command returned invalid execution identities");
                                    execution.First = answer.Arg0;
                                }
                                if (execution.Recovering) execution.BlockedSince = -1;
                                execution.Pending = 0; execution.Recovering = false; break;
                            }
                var plans = Plans(context.View);
                foreach (var execution in _executions.Values)
                {
                    if (execution.Pending > 0 || execution.First == 0 || execution.Definition.Recovery == null) continue;
                    var linked = Linked(execution, plans);
                    PlanRecoveryCheck check = linked.Count == execution.Count
                        ? context.ObservePlanRecovery(execution.Definition.Recovery,
                            new PlanRecoveryContext(context.View, linked, execution.BlockedSince, execution.RecoveryAttempts))
                        : new PlanRecoveryCheck(false, false, Codes.NotFound);
                    if (!check.Eligible) execution.BlockedSince = -1;
                    else if (execution.BlockedSince < 0) execution.BlockedSince = context.Tick;
                }
                var facts = Read(context.View, context.ObserveCondition, context.ObservePlanSites, context.ObservePlanRecovery);
                int submitted = 0; Code blocker = firstRefusal;
                foreach (var step in _definition._priority)
                {
                    PlaybookStepSnapshot fact = facts[step.Id];
                    var execution = _executions[step.Id];
                    if (fact.Recovery.Ready && execution.Pending == 0 && !refused.Contains(step.Id))
                    {
                        var cancel = new CancelPlanCommand { Selection = new PlanCancellationSelection(true, Range(execution)) };
                        Code refusal = context.Preview(cancel);
                        if (!refusal.IsNone) { if (blocker.IsNone) blocker = refusal; continue; }
                        int attempts = checked(execution.RecoveryAttempts + 1);
                        execution.Pending = context.Submit(cancel); execution.Recovering = true;
                        execution.RecoveryAttempts = attempts; submitted++;
                        if (submitted == _definition.CommandsPerDecision) break;
                        continue;
                    }
                    if (fact.Status == PlaybookStepStatus.Ready && !refused.Contains(step.Id))
                    {
                        var command = new PlacePlanTemplateCommand { Selection = PlanTemplateSelection.Snapshot(step.Template.Template), Anchor = fact.SelectedSite.Anchor, Group = Group(execution) };
                        Code refusal = context.Preview(command);
                        if (!refusal.IsNone) { if (blocker.IsNone) blocker = refusal; continue; }
                        _executions[step.Id].Pending = context.Submit(command);
                        if (++submitted == _definition.CommandsPerDecision) break;
                    }
                    else if (blocker.IsNone && !fact.Blocker.IsNone) blocker = fact.Blocker;
                }
                if (submitted == 0 && !blocker.IsNone) context.ReportBlocker(blocker);
            }
            private static Dictionary<long, PlanView> Plans(PlayerView view)
            {
                var values = new List<PlanView>(); view.Feature<WorkView>().CopyPlans(values);
                var plans = new Dictionary<long, PlanView>();
                foreach (var plan in values) plans.Add(plan.Id, plan);
                return plans;
            }
            private static List<PlanView> Linked(Execution execution, Dictionary<long, PlanView> plans)
            {
                var linked = new List<PlanView>();
                for (int i = 0; i < execution.Count; i++)
                    if (plans.TryGetValue(execution.First + i, out var plan)) linked.Add(plan);
                return linked;
            }
            private static IReadOnlyList<long> Range(Execution execution)
            {
                var plans = new List<long>();
                for (int i = 0; i < execution.Count; i++) plans.Add(checked(execution.First + i));
                return plans.AsReadOnly();
            }
            private static string Group(Execution execution)
                => execution.RecoveryAttempts == 0 ? execution.Definition.Group : execution.Definition.Group +
                    ".recovery." + execution.RecoveryAttempts.ToString(System.Globalization.CultureInfo.InvariantCulture);
            private Dictionary<string, PlaybookStepSnapshot> Read(PlayerView view, Func<IGameCondition, Code> observe,
                Func<IPlanSiteSource, PlanSiteObservation> sites,
                Func<IPlanRecoveryPolicy, PlanRecoveryContext, PlanRecoveryCheck> recovery)
            {
                var plans = Plans(view); var result = new Dictionary<string, PlaybookStepSnapshot>(StringComparer.Ordinal);
                foreach (var step in _definition._graph.Nodes)
                {
                    var execution = _executions[step.Id]; Code blocker = Code.None; PlaybookStepStatus status;
                    var candidates = new List<PlaybookSiteSnapshot>(); PlanSiteCandidate selected = null;
                    PlanRecoveryCheck recoveryCheck = default;
                    if (execution.Pending > 0)
                    { status = execution.Recovering ? PlaybookStepStatus.Recovering : PlaybookStepStatus.CommandPending; blocker = new Code("controller.command-pending"); }
                    else if (execution.First > 0)
                    {
                        status = PlaybookStepStatus.Done;
                        for (int i = 0; i < execution.Count; i++)
                        {
                            if (!plans.TryGetValue(execution.First + i, out var plan))
                            { status = PlaybookStepStatus.Blocked; blocker = Codes.NotFound; break; }
                            if (plan.State == PlanState.Failed || plan.State == PlanState.Cancelled)
                            { status = PlaybookStepStatus.Blocked; blocker = plan.Blocker.IsNone ? Codes.Blocked : plan.Blocker; break; }
                            if (plan.State != PlanState.Done)
                            { status = PlaybookStepStatus.Executing; if (blocker.IsNone) blocker = plan.Blocker.IsNone ? new Code("controller.plan-pending") : plan.Blocker; }
                        }
                        if (step.Recovery != null && !execution.Recovering)
                        {
                            var linked = Linked(execution, plans);
                            recoveryCheck = linked.Count != execution.Count ? new PlanRecoveryCheck(false, false, Codes.NotFound)
                                : recovery(step.Recovery, new PlanRecoveryContext(view, linked, execution.BlockedSince, execution.RecoveryAttempts));
                        }
                        if (execution.Recovering)
                        { status = PlaybookStepStatus.Recovering; blocker = new Code("controller.recovery-in-progress"); }
                    }
                    else
                    {
                        status = PlaybookStepStatus.Ready;
                        foreach (string dependency in _definition._graph.Dependencies(step.Id))
                            if (result[dependency].Status != PlaybookStepStatus.Done)
                            { status = PlaybookStepStatus.Waiting; blocker = new Code("controller.dependencies"); break; }
                        if (status == PlaybookStepStatus.Ready)
                        {
                            foreach (var condition in step.Conditions)
                            { blocker = observe(condition); if (!blocker.IsNone) { status = PlaybookStepStatus.Blocked; break; } }
                            if (status == PlaybookStepStatus.Ready)
                            {
                                var observation = sites(step.SiteSource); blocker = observation.Blocker;
                                foreach (var candidate in observation.Sites)
                                {
                                    var check = view.Feature<WorkView>().PreviewPlanTemplateActivation(
                                        PlanTemplateSelection.Snapshot(step.Template.Template), candidate.Anchor, Group(execution));
                                    var fact = new PlaybookSiteSnapshot(candidate, check);
                                    candidates.Add(fact);
                                    if (selected == null && fact.Ready) selected = candidate;
                                    if (blocker.IsNone && !fact.Ready) blocker = fact.Blocker;
                                }
                                if (selected != null) blocker = Code.None;
                                else if (blocker.IsNone) blocker = new Code("controller.no-sites");
                                if (!blocker.IsNone) status = PlaybookStepStatus.Blocked;
                            }
                        }
                    }
                    result.Add(step.Id, new PlaybookStepSnapshot(step.Id, _definition._graph.Dependencies(step.Id), step.Priority, status,
                        blocker, execution.Pending, execution.First, execution.Count, candidates, selected,
                        execution.BlockedSince, execution.RecoveryAttempts, execution.Recovering,
                        execution.Recovering ? Range(execution) : Array.Empty<long>(), recoveryCheck));
                }
                return result;
            }
            public void CopySteps(PlayerView view, Func<IGameCondition, Code> observe,
                Func<IPlanSiteSource, PlanSiteObservation> sites,
                Func<IPlanRecoveryPolicy, PlanRecoveryContext, PlanRecoveryCheck> recovery, IList<PlaybookStepSnapshot> destination)
            {
                if (destination == null) throw new ArgumentNullException(nameof(destination)); destination.Clear();
                var facts = Read(view, observe, sites, recovery);
                foreach (string id in _executions.Keys) destination.Add(facts[id]);
            }
            public void ValidateState(PlayerView view, IReadOnlyList<int> awaiting, IReadOnlyList<CommandResult> answers)
            {
                var plans = Plans(view);
                foreach (var execution in _executions.Values)
                {
                    if (execution.BlockedSince > view.Tick) throw new CorruptStateException("recovery timestamp exceeds the sole clock");
                    if (execution.Pending > 0)
                    {
                        bool found = false;
                        foreach (int ticket in awaiting) if (ticket == execution.Pending) found = true;
                        foreach (var answer in answers)
                            if (answer.Sequence == execution.Pending && answer.Player == view.Viewer &&
                                answer.Command == (execution.Recovering ? "sim.cancel-plan" : "sim.place-plan-template")) found = true;
                        if (!found) throw new CorruptStateException("playbook pending node lacks actual controller command/mail");
                    }
                    if (execution.First > 0)
                        for (int i = 0; i < execution.Count; i++)
                            if (!plans.TryGetValue(execution.First + i, out var plan) || plan.Definition != execution.Definition.Template.Template.Entries[i].Definition)
                                throw new CorruptStateException("playbook execution does not match actual own template plans");
                    if (execution.Pending > 0 || execution.First > 0)
                        foreach (string dependency in _definition._graph.Dependencies(execution.Definition.Id))
                        {
                            var prerequisite = _executions[dependency];
                            if (prerequisite.First == 0) throw new CorruptStateException("started playbook node has unissued prerequisite");
                            for (int i = 0; i < prerequisite.Count; i++)
                                if (!plans.TryGetValue(prerequisite.First + i, out var plan) || plan.State != PlanState.Done)
                                    throw new CorruptStateException("started playbook node has unfinished prerequisite");
                        }
                }
            }
            public void Sync(IStateSync sync)
            {
                if (sync.Count(_executions.Count) != _executions.Count) throw new CorruptStateException("playbook node count differs from content");
                var decoded = new List<(Execution execution, int pending, long first, long since, int attempts, bool recovering)>();
                var tickets = new HashSet<int>(); var ranges = new List<(long first, long last)>();
                foreach (var execution in _executions.Values)
                {
                    string id = execution.Definition.Id; int pending = execution.Pending; long first = execution.First;
                    long since = execution.BlockedSince;
                    int attempts = execution.RecoveryAttempts; bool recovering = execution.Recovering;
                    sync.String(ref id); sync.Int(ref pending); sync.Long(ref first);
                    sync.Long(ref since); sync.Int(ref attempts); sync.Bool(ref recovering);
                    if (since < -1 || attempts < 0 ||
                        execution.Definition.Recovery == null && (since != -1 || attempts != 0 || recovering) ||
                        first == 0 && (since != -1 || recovering) ||
                        recovering && (attempts == 0 || since < 0 || pending == 0) ||
                        id != execution.Definition.Id || pending < 0 || first < 0 || pending > 0 && first > 0 && !recovering ||
                        pending > 0 && !tickets.Add(pending) || first > 0 && first > long.MaxValue - (execution.Count - 1))
                        throw new CorruptStateException("invalid playbook stable node/execution link");
                    if (first > 0)
                    {
                        long last = first + execution.Count - 1;
                        foreach (var range in ranges) if (first <= range.last && last >= range.first) throw new CorruptStateException("playbook nodes share actual execution identities");
                        ranges.Add((first, last));
                    }
                    if (sync.Reading) decoded.Add((execution, pending, first, since, attempts, recovering));
                }
                if (sync.Reading) foreach (var row in decoded)
                {
                    row.execution.Pending = row.pending; row.execution.First = row.first;
                    row.execution.BlockedSince = row.since; row.execution.RecoveryAttempts = row.attempts;
                    row.execution.Recovering = row.recovering;
                }
            }
        }
    }
}
