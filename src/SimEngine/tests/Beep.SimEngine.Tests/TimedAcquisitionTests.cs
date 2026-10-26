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
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Ordinary authored composition/player inputs and detached observations, never work decisions.
    internal static class TimedAcquisitionTests
    {
        internal const string Case = "Timed acquisition: authored all-any timed and staffed frontiers share atomic branch commitments";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static WorkView Work(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<WorkView>();
        private static List<ActivityNodeView> Nodes(SimGame game, int order)
        { var rows = new List<ActivityNodeView>(); Work(game).TryCopyNodes(order, rows); return rows; }
        private static LogisticsOperationalSnapshot Supply(SimGame game)
        {
            if (!game.Seat(0).View.TryFindResource("supply", out var resource)) throw new InvalidOperationException("missing authored supply");
            var code = game.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(Entity(game, "source"), resource, out var value);
            if (!code.IsNone || value == null) throw new InvalidOperationException("source diagnostics " + code); return value;
        }
        private static CommandResult Result(SimGame game, int sequence)
        { long cursor = 0; var rows = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, rows); return rows.Single(x => x.Sequence == sequence); }
        private static int Queue(Check c, SimGame game, Command command)
        { ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary command preview"); c.Equal(hash, game.Hash(), "preview pure"); return game.Seat(0).Submit(command); }
        private static CommandResult Submit(Check c, SimGame game, Command command)
        { int sequence = Queue(c, game, command); Step(game); var result = Result(game, sequence); c.That(result.Accepted, "ordinary player input accepted"); return result; }
        private static bool Complete(SimGame game, string plan)
            => Work(game).TryReadActivityCompletion("activity:" + plan, Entity(game, "source"), out _);
        private static void Reach(SimGame game, SimGame twin, Func<bool> done, string stage)
        {
            var trail = new Queue<string>();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 512 && watch.ElapsedMilliseconds < 20000 && !done(); i++)
            {
                var orders = new List<OrderView>(); Work(game).CopyOrders(orders);
                trail.Enqueue(game.Clock.StepIndex + ":" + string.Join(";", orders.Select(o => o.Id + "/" + o.State + "/" + string.Join(",", Nodes(game, o.Id).Select(n => n.Id + "/" + n.State + "/" + n.Progress.WholeTicks + "/" + n.Blocker)))));
                if (trail.Count > 16) trail.Dequeue(); Step(game, twin);
            }
            if (!done()) throw new InvalidOperationException(stage + " exceeded512Updates/20s stock=" + Supply(game).Stock + " promised=" + Supply(game).OutboundPromised + " trail=" + string.Join(" | ", trail));
        }
        private static GameFactory Factory(bool direct, bool half, bool mixed)
        {
            var module = new RequirementAcquisitionModule(); var data = Json.Parse(FrameworkProofContent.Read("timed-acquisition.json"));
            if (!direct) foreach (var pair in new[] { ("independent_timed_executors", "timed_activity_executors"),
                ("independent_staffed_executors", "staffed_activity_executors"), ("independent_requirement_groups", "work_requirement_groups"),
                ("independent_target_resources", "target_resource_requirements") })
            { data.Set(pair.Item2, data.Get(pair.Item1)); data.Remove(pair.Item1); }
            if (mixed)
            {
                var node = data.Get("activities")[0].Get("nodes")[0]; node.Set("executor", JsonValue.Of("timed.staffed"));
                node.Set("roles", Json.Parse("[\"inspect\"]"));
            }
            return ConstructionPlanTests.Points(extraModules: new[] { module },
                extraDeclarations: "[{\"id\":\"proof.requirement-acquisition\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"timed-acquisition.json\"]}]",
                extraDocuments: new Dictionary<string, string> { { "timed-acquisition.json", data.ToString() } },
                scenarioEdit: scenario => scenario.Get("modules").Add(JsonValue.Of(module.Id)), worldEdit: root =>
                {
                    root.Get("entities")[0].Get("components").Get("point_stock").Set("initial", JsonValue.Number("4"));
                    StartupContentAuthoring.Position(root, "machine", JsonValue.Number("4"));
                    if (half) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
                });
        }
        private static void Bound(Check c, SimGame game, int order, bool mixed)
        {
            var rows = Nodes(game, order).Where(x => x.Id != "join").ToArray();
            c.That(rows.Length == 2 && rows.All(x => x.State == NodeState.Working && x.Progress.WholeTicks > 0), "timed/staffed frontier earns simultaneous progress");
            c.That(rows.All(x => x.Choices.Count == 1 && x.Choices[0].Entry == "z.feasible"), "whole-set evaluator commits jointly feasible any branches");
            c.That(rows.All(x => x.Resources.Count == 2 && x.Resources.Count(r => r.Choice.Use == WorkResourceUse.Hold && !r.Spent && r.Receipts.Count == 1) == 1
                && x.Resources.Count(r => r.Choice.Use == WorkResourceUse.Consume && r.Spent) == 1), "every material/tool occurrence owns its exact native receipt/spent state");
            c.That(rows.SelectMany(x => x.Resources).Select(x => x.Choice.Endpoint).Distinct().Count() == 1, "both native sets share one actual scarce target");
            c.That(Supply(game).Stock == Fix.FromInt(2) && Supply(game).OutboundPromised == Fix.FromInt(2), "both materials spent, both tools protected");
            c.That(rows.Count(x => x.Worker.IsValid) == (mixed ? 1 : 0) && rows.Count(x => x.JobId != 0) == (mixed ? 1 : 0), "timed node never fabricates worker/job, mixed row owns a real machine");
            c.That(Nodes(game, order).Single(x => x.Id == "join").Progress.WholeTicks == 0, "dependent join cannot bypass either earned predecessor");
        }
        private static void Physical(Check c, bool direct, bool half, bool mixed, bool cancel)
        {
            var factory = Factory(direct, half, mixed); var game = ConstructionPlanTests.NewPoints(factory); var idle = ConstructionPlanTests.NewPoints(factory); ulong idleHash = idle.Hash();
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "timed and machine work independent of business");
            var limits = new ReplayLimits(4 * 1024 * 1024, 4000); var recording = game.RecordReplay(limits);
            Submit(c, game, new ConfigureWorkPolicyCommand { Worker = Entity(game, "machine"), Settings = new WorkPolicySettings(true, 0,
                new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) });
            int order = checked((int)Submit(c, game, new PostOrderCommand { Plan = "timed.parallel", Target = Entity(game, "source") }).Arg0);
            Reach(game, null, () => Nodes(game, order).Count(x => x.Id != "join" && x.State == NodeState.Working && x.Progress.WholeTicks > 0) == 2, "parallel actual acquisition");
            Bound(c, game, order, mixed); var retained = Nodes(game, order).Single(x => x.Id == "beta");
            if (mixed) c.That(game.Seat(0).Snapshot.Entities.Where(x => x.Id == Nodes(game, order).Single(n => n.Id == "alpha").Worker)
                .All(x => ((PointSnapshot)x.Spatial).Position == Fix.Zero), "staffed row physically reaches source before earning work");
            ulong hash = game.Hash(); var foreign = new List<ActivityNodeView>(); c.That(!Work(game, 1).TryCopyNodes(order, foreign) && foreign.Count == 0, "held timed branch facts company-private");
            var diagnostics = new List<WorkRequirementView>(); c.Equal(Code.None, Work(game).PreviewOrderRequirements("timed.single", Entity(game, "source"), diagnostics), "same evaluator supplies typed stock diagnostics");
            c.That(diagnostics.Any(x => x.Result.Status != WorkRequirementStatus.Satisfied), "scarce held tools block an additional complete set"); c.Equal(hash, game.Hash(), "projections reserve nothing");
            int blocked = checked((int)Submit(c, game, new PostOrderCommand { Plan = "timed.single", Target = Entity(game, "source") }).Arg0);
            var waiting = Nodes(game, blocked).Single(); c.That(waiting.Resources.Count == 0 && waiting.Choices.Count == 0 && waiting.Progress.Equals(WorkProgress.Zero) && !waiting.Worker.IsValid && !waiting.Blocker.IsNone, "contending timed row acquires no prefix");
            c.Equal(diagnostics.Single(x => x.Path.Count == 1).Result.Blocker, waiting.Blocker, "pre-command shortage equals actual admitted waiting blocker");
            TestKit.AssertUnclaimedRequirementParity(c, game, 0, "timed.single", Entity(game, "source"), blocked, waiting.Id);
            Bound(c, game, order, mixed);
            game.Clock.Pause(); int cancellation = Queue(c, game, new CancelOrderCommand { Order = blocked }); var twin = factory.Load(game.Save()); twin.Clock.Pause(); hash = game.Hash(); long at = game.Clock.StepIndex;
            TestKit.AssertUnclaimedRequirementParity(c, twin, 0, "timed.single", Entity(twin, "source"), blocked, waiting.Id);
            Step(game, twin); c.That(game.Hash() == hash && game.Clock.StepIndex == at, "saved pending cancellation waits on the sole paused clock"); Bound(c, game, order, mixed);
            game.Clock.Resume(); twin.Clock.Resume(); Step(game, twin); c.That(Result(game, cancellation).Accepted, "saved blocked cancellation accepted"); Bound(c, game, order, mixed);
            if (cancel)
            {
                game.Clock.Pause(); cancellation = Queue(c, game, new CancelOrderCommand { Order = order }); twin = factory.Load(game.Save()); twin.Clock.Pause(); hash = game.Hash(); Step(game, twin);
                c.Equal(hash, game.Hash(), "acquired cancellation cannot bypass pause"); game.Clock.Resume(); twin.Clock.Resume(); Step(game, twin);
                c.That(Result(game, cancellation).Accepted && !Complete(game, "timed.parallel"), "actual acquired cancellation never invents completion");
            }
            else Reach(game, twin, () => Complete(game, "timed.parallel"), "timed/mixed predecessors and join");
            c.That(Supply(game).Stock == Fix.FromInt(2) && Supply(game).OutboundPromised == Fix.Zero, "completion/cancellation releases tools, does not refund earned material");
            int later = checked((int)Submit(c, game, new PostOrderCommand { Plan = "timed.single", Target = Entity(game, "source") }).Arg0);
            Reach(game, null, () => Nodes(game, later).Any(x => x.Progress.WholeTicks > 0), "released tools support new timed work");
            c.That(Nodes(game, later).Single().Choices.Single().Entry == "z.feasible", "later task uses only actually affordable branch");
            twin = factory.Load(game.Save()); Reach(game, twin, () => Complete(game, "timed.single"), "later committed timer finishes");
            c.That(Supply(game).Stock == Fix.One && Supply(game).OutboundPromised == Fix.Zero, "exactly three material units spent and no surviving promise");
            c.That(retained.Progress.WholeTicks > 0 && retained.Progress.WholeTicks < retained.RequiredTicks && retained.Resources.Count == 2, "retained timed facts immutable after release");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "terminal actual graph/job/branch/resources save"); c.Equal(idleHash, idle.Hash(), "idle interleaved session isolated");
            c.That(new ReplayVerifier().Verify(factory, recording.Finish(), limits).Success, "public policy/frontier/blocked/cancel/later inputs replay exactly");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current actual identity");
            foreach (bool direct in new[] { false, true }) foreach (bool half in new[] { false, true })
                foreach (bool mixed in new[] { false, true }) foreach (bool cancel in new[] { false, true }) Physical(c, direct, half, mixed, cancel);
        }
    }
}
