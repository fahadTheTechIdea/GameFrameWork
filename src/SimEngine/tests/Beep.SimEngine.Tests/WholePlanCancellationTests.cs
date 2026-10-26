using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class WholePlanCancellationTests
    {
        internal const string Case = "Plans: whole cancellation, grouped rival recovery and saved physical execution";
        private static readonly ReplayLimits Limits = new ReplayLimits(16 * 1024 * 1024, 12000);
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory() => ConstructionPlanTests.Points(
            additionalModule: new PlayerControllerProofModule(),
            moduleDeclaration: "{\"id\":\"sim.player-controllers\",\"version\":\"1.0.0\",\"documents\":[\"playbook-group-recovery.json\"]}",
            moduleDocument: FrameworkProofContent.Read("playbook-group-recovery.json"),
            scenarioEdit: scenario => {
                scenario.Get("modules").Add(JsonValue.Of(PlayerControllersModule.ModuleId));
                scenario.Get("settings").Set(PlayerControllersModule.ModuleId, Content.Json.Parse(
                    "{\"companies\":[{\"company\":0,\"profile\":\"group.controller\"},{\"company\":1,\"profile\":\"group.controller\"}]}"));
            }, startupProfiles: new[] { "playbook-group-recovery.start" });
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static WorkView Work(SimGame game, int company = 1) => game.Seat(company).View.Feature<WorkView>();
        private static List<PlanView> Plans(SimGame game, int company = 1)
        { var rows = new List<PlanView>(); Work(game, company).CopyPlans(rows); return rows; }
        private static PlaybookStepSnapshot Claim(SimGame game, int company = 1)
        {
            var rows = new List<PlaybookStepSnapshot>();
            if (!game.Seat(company).View.Feature<PlayerControllersView>().CopyPlaybook(new PlayerId(company), rows))
                throw new InvalidOperationException("missing grouped recovery");
            return rows.Single();
        }
        private static Fix Stock(SimGame game, int company)
        {
            Fix stock = Fix.Zero; var resource = game.World.Content.ResourceId("supply");
            foreach (var entity in game.World.Entities.All)
                if (entity.Alive && entity.Owner == new PlayerId(company) && entity.Get<IInletPort>() is IInletPort inlet)
                    stock += inlet.Amount(resource);
            return stock;
        }
        private static JsonValue At(int position) => Content.Json.Parse("{\"space\":\"field.points\",\"position\":" + position + "}");
        private static PlanIntent Intent(int position, string group, bool paused, IEnumerable<long> after = null)
        {
            var args = JsonValue.NewObject(); args.Set("placement", At(position));
            return new PlanIntent("build:point.tower", args, group, 70, paused, after);
        }
        private static CommandResult Order(Check c, SimGame game, int company, Command command)
        {
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(company).Preview(command), "normal player command currently ready");
            c.Equal(hash, game.Hash(), "preview performs no cancellation or physical work");
            var answer = TestKit.Order(game, company, command); c.That(answer.Accepted, "normal command accepted: " + answer.Refusal);
            if (!answer.Accepted) throw new InvalidOperationException("command refused: " + answer.Refusal);
            return answer;
        }
        private static void Configure(Check c, SimGame game)
        {
            for (int company = 0; company < 2; company++)
            {
                var worker = game.Seat(company).Snapshot.Entities.Single(e => e.Def == "machine" && e.Owner == new PlayerId(company)).Id;
                Order(c, game, company, new ConfigureWorkPolicyCommand { Worker = worker, Settings = new WorkPolicySettings(
                    true, 0, new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All) });
            }
        }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.RunTicks(1); twin?.RunTicks(1);
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("whole cancellation time=" + game.Tick + "/fault=" + game.Fault);
        }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 2000 && !done(); i++) Step(game, twin);
            if (!done())
            {
                var jobs = new List<WorkJobView>(); Work(game).CopyJobs(jobs);
                var nodes = new List<string>();
                var movement = new List<string>(); var motion = game.Observer.Feature<MotionView>();
                foreach (var entity in game.Snapshot.Entities.Where(entity => entity.Def == "machine"))
                {
                    Code execution = motion.ReadExecution(entity.Id, out var moving);
                    Code estimate = motion.ReadArrival(entity.Id, out var arrival);
                    movement.Add(entity.Id + "/" + entity.Owner + "/at=" + (entity.Spatial as PointSnapshot)?.Position +
                        "/execution=" + execution + "/" + moving?.Status + "/" + moving?.Blocker + "/dock=" + moving?.DockBlocker +
                        "/arrival=" + estimate + "/" + arrival?.Blocker + "/remaining=" + arrival?.RemainingTicks);
                }
                foreach (var plan in Plans(game).Where(plan => plan.Order > 0))
                {
                    var rows = new List<ActivityNodeView>(); Work(game).TryCopyNodes(plan.Order, rows);
                    nodes.AddRange(rows.Select(node => plan.Id + ":" + node.Id + "/" + node.State + "/" + node.Blocker + "/job=" + node.JobId + "/worker=" + node.Worker + "/progress=" + node.Progress.WholeTicks));
                }
                throw new InvalidOperationException(stage + "/time=" + game.Tick + "/plans=" +
                    string.Join(";", Plans(game).Select(p => p.Id + "/" + p.State + "/" + p.Blocker)) +
                    "/claim=" + Claim(game).Status + "/" + Claim(game).RecoveryAttempts + "/nodes=" + string.Join(";", nodes) +
                    "/jobs=" + string.Join(";", jobs.Select(job => job.Id + "/" + job.State + "/" + job.Blocker + "/worker=" + job.Worker + "/target=" + job.Target + "/toil=" + job.ToilIndex)) +
                    "/movement=" + string.Join(";", movement));
            }
        }
        private static CancelPlanCommand Cancel(bool pendingOnly, params long[] ids)
            => new CancelPlanCommand { Selection = new PlanCancellationSelection(pendingOnly, ids) };
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current whole cancellation build");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "one revised command/playbook reader");
            Selection(c); WholeRefusal(c); Physical(c); Stale(c); GroupUser(c);
        }
        private static void Selection(Check c)
        {
            var values = new List<long> { 9, 2, 7 }; var selection = new PlanCancellationSelection(true, values); values.Clear();
            c.That(selection.PendingOnly && selection.Plans.SequenceEqual(new long[] { 2, 7, 9 }), "explicit selection canonicalizes detached exact IDs");
            c.Throws<NotSupportedException>(() => ((IList<long>)selection.Plans).Clear(), "selection immutable");
            c.Throws<ArgumentException>(() => new PlanCancellationSelection(true, Array.Empty<long>()), "empty selection invalid");
            c.Throws<ArgumentException>(() => new PlanCancellationSelection(true, new long[] { 1, 1 }), "duplicates invalid");
            c.Throws<ArgumentException>(() => new PlanCancellationSelection(false, new long[] { 0 }), "zero identity invalid");
            c.Throws<ArgumentNullException>(() => new PlanCancellationSelection(false, null), "missing selection has no inferred target");
        }
        private static void WholeRefusal(Check c)
        {
            var factory = Factory(); var game = New(factory);
            long a = Order(c, game, 0, new SubmitPlanCommand { Intent = Intent(10, "owner", true) }).Arg0;
            long b = Order(c, game, 0, new SubmitPlanCommand { Intent = Intent(16, "owner", true) }).Arg0;
            long foreign = Order(c, game, 1, new SubmitPlanCommand { Intent = Intent(22, "foreign", true) }).Arg0;
            foreach (var tuple in new[] {
                (selection: new PlanCancellationSelection(false, new[] { foreign, a }), expected: Codes.NotOwned),
                (selection: new PlanCancellationSelection(false, new[] { long.MaxValue, a }), expected: Codes.NotFound),
                (selection: new PlanCancellationSelection(true, new[] { b, a }), expected: PlanCodes.Paused) })
            {
                ulong hash = game.Hash(); var view = Work(game, 0).PreviewPlanCancel(tuple.selection);
                c.That(view.Blocker == tuple.expected && view.Entries.Count == 2 && view.Entries[0].Plan == a,
                    "whole private admission includes stable per-entry blockers");
                c.Equal(tuple.expected, game.Seat(0).Preview(new CancelPlanCommand { Selection = tuple.selection }), "command and view use same whole check");
                c.Equal(hash, game.Hash(), "whole refusal preview preserves hash");
                var answer = TestKit.Order(game, 0, new CancelPlanCommand { Selection = tuple.selection });
                c.That(!answer.Accepted && answer.Refusal == tuple.expected && Plans(game, 0).All(p => p.State == PlanState.Pending),
                    "actual whole refusal never cancels ready prefix");
            }
            c.Equal(Codes.InvalidArgument, game.Seat(0).Preview(new CancelPlanCommand()), "missing command selection explicitly refuses");
            Order(c, game, 0, Cancel(false, b));
            var ended = new PlanCancellationSelection(false, new[] { b, a });
            c.Equal(Codes.AlreadyDone, Work(game, 0).PreviewPlanCancel(ended).Blocker, "terminal member refuses whole ordinary set");
            var failed = TestKit.Order(game, 0, new CancelPlanCommand { Selection = ended });
            c.That(!failed.Accepted && Plans(game, 0).Single(p => p.Id == a).State == PlanState.Pending, "terminal later member leaves pending prefix intact");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "refused and cancelled native history saves");
        }
        private static void Physical(Check c)
        {
            var factory = Factory(); var game = New(factory); Configure(c, game);
            // The authored one-dimensional map has no passing lane. Each company's finite
            // source and fleet start on its own side; no test-side movement or occupancy bypass.
            c.That(game.Seat(1).Snapshot.Entities.Where(entity => entity.Owner == new PlayerId(1) &&
                (entity.Def == "source" || entity.Def == "machine")).Count(entity =>
                    entity.Spatial is PointSnapshot point && point.Position == Fix.FromInt(40)) == 2,
                "authored rival source and machine have a physically separate operating interval");
            c.Equal(Fix.FromInt(20), Stock(game, 0), "player starts with unchanged finite20");
            c.Equal(Fix.FromInt(20), Stock(game, 1), "rival starts with unchanged finite20");
            using (var recorder = game.RecordReplay(Limits))
            {
                Order(c, game, 1, new SetControllerCommand { Controller = Controller.Autopilot });
                c.That(Claim(game).PendingCommand > 0 && Plans(game).Count == 0, "rival submits one ordinary grouped intent");
                Order(c, game, 0, new ConstructCommand { Def = "point.tower", Placement = At(30), Priority = 70 });
                var old = Plans(game).OrderBy(p => p.Id).ToArray(); long first = old[0].Id;
                c.That(old.Length == 2 && old.All(p => p.State == PlanState.Pending) &&
                    old[0].Blocker == Codes.Blocked && old[1].Blocker == PlanCodes.DependencyPending,
                    "site race leaves whole accepted group unstarted");
                Order(c, game, 1, new ConfigurePlanCommand { Plan = first + 1, Group = "player.split", Priority = 70, Paused = false });
                var twin = factory.Load(game.Save());
                Until(game, () => Claim(game).CancellationPlans.Count == 2, "whole exact cancellation queued", twin);
                var fact = Claim(game);
                c.That(fact.CancellationPlans.SequenceEqual(new[] { first, first + 1 }) && fact.FirstPlan == first &&
                    fact.RecoveryAttempts == 1 && Plans(game).All(p => p.State == PlanState.Pending),
                    "one budgeted normal command captures actual range despite player regrouping");
                c.Throws<NotSupportedException>(() => ((IList<long>)fact.CancellationPlans).Clear(), "cancellation diagnostics immutable");
                var privateRows = new List<PlaybookStepSnapshot> { fact };
                c.That(!game.Seat(0).View.Feature<PlayerControllersView>().CopyPlaybook(new PlayerId(1), privateRows) && privateRows.Count == 0,
                    "exact rival cancellation IDs do not leak to another company");
                twin = factory.Load(game.Save()); Step(game, twin);
                c.That(Plans(game).Where(p => p.Id >= first && p.Id < first + 2).All(p => p.State == PlanState.Cancelled) &&
                    Claim(game).CancellationPlans.Count == 0 && Claim(game).FirstPlan == 0 && Claim(game).PendingCommand > 0,
                    "both actual members cancel before real answer releases link and proposes alternative");
                c.That(fact.CancellationPlans.Count == 2, "retained in-flight facts do not change");
                c.Equal(Fix.FromInt(20), Stock(game, 1), "whole pending cancellation grants/spends no resources");
                twin = factory.Load(game.Save()); Step(game, twin);
                var next = Plans(game).Where(p => p.Id > first + 1).OrderBy(p => p.Id).ToArray();
                c.That(next.Length == 2 && next[0].Arguments.Get("placement").Get("position").Text == "60" &&
                    next[1].Arguments.Get("placement").Get("position").Text == "66" &&
                    next.All(p => p.Group == "group.claim.recovery.1"), "existing current selector places whole alternate authored template");
                twin = factory.Load(game.Save());
                Until(game, () => Claim(game).Status == PlaybookStepStatus.Done &&
                    game.Seat(0).Snapshot.Entities.Any(e => e.Def == "point.tower" && e.Owner == new PlayerId(0)), "physical alternate group completes", twin);
                c.Equal(Fix.FromInt(4), Stock(game, 1), "two rival structures physically consume16 of20");
                c.Equal(Fix.FromInt(12), Stock(game, 0), "player structure physically consumes8 of20");
                c.That(Plans(game).Count == 4 && Claim(game).RecoveryAttempts == 1 && !Claim(game).Recovering,
                    "old native cancellation history and new completed plans remain distinct, no duplicate gameplay");
                var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(),
                    "whole cancellation/grouped physical saves replay: " + replay.Failure + "/" + replay.Detail);
            }
        }
        private static void Stale(Check c)
        {
            var factory = Factory(); var game = New(factory); Configure(c, game);
            Order(c, game, 0, new ConstructCommand { Def = "point.tower", Placement = At(30), Priority = 70 });
            long a = Order(c, game, 1, new SubmitPlanCommand { Intent = Intent(30, "guard", false) }).Arg0;
            long b = Order(c, game, 1, new SubmitPlanCommand { Intent = Intent(36, "guard", false, new[] { a }) }).Arg0;
            var cancel = Cancel(true, b, a);
            c.Equal(Code.None, game.Seat(1).Preview(cancel), "initial whole pending selection currently ready");
            Order(c, game, 1, new ConfigurePlanDependenciesCommand { Changes = new PlanDependencyChanges(new[] {
                new PlanDependencyChange(b, Array.Empty<long>()) }) });
            c.That(Plans(game).Single(p => p.Id == b).State == PlanState.Active, "normal player dependency edit activates later member physically");
            var check = Work(game).PreviewPlanCancel(cancel.Selection); ulong hash = game.Hash();
            c.That(check.Blocker == PlanCodes.NotPending && check.Entries[0].Blocker.IsNone &&
                check.Entries[1].Blocker == PlanCodes.NotPending, "stale whole selection refuses active later member before pending prefix");
            c.Equal(hash, game.Hash(), "fresh whole guard is pure");
            game.Seat(1).Submit(cancel); var twin = factory.Load(game.Save()); Step(game, twin);
            c.That(Plans(game).Single(p => p.Id == a).State == PlanState.Pending &&
                Plans(game).Single(p => p.Id == b).State == PlanState.Active, "queued saved refusal cancels no prefix and preserves active work");
            Until(game, () => Plans(game).Single(p => p.Id == b).State == PlanState.Done, "refused whole command leaves physical project completable", twin);
            c.Equal(Fix.FromInt(12), Stock(game, 1), "protected active project consumes only its own8");
        }
        private static void GroupUser(Check c)
        {
            var factory = Factory(); var game = New(factory);
            Order(c, game, 0, new SubmitPlanCommand { Intent = Intent(10, "user.group", true) });
            Order(c, game, 0, new SubmitPlanCommand { Intent = Intent(16, "user.group", true) });
            c.That(Work(game, 0).PreviewPlanGroupCancel("user.group").Ready, "normal group preview delegates same whole owner");
            Order(c, game, 0, new CancelPlanGroupCommand { Group = "user.group" });
            c.That(Plans(game, 0).All(p => p.State == PlanState.Cancelled), "ordinary user group cancellation permits explicitly selected paused intents");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "group cancellation uses one native saved history");
        }
    }
}
