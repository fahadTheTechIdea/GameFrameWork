using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    // Pure file composition and normal player inputs/observations; no acquisition or stock decisions here.
    internal static class ParallelAcquisitionTests
    {
        internal const string Case = "Parallel acquisition: authored all-any roles tools and materials reserve whole contended sets";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static WorkView Work(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<WorkView>();
        private static List<ActivityNodeView> Nodes(SimGame game, int order)
        { var rows = new List<ActivityNodeView>(); if (!Work(game).TryCopyNodes(order, rows)) throw new InvalidOperationException("missing active order " + order); return rows; }
        private static ResourceId Resource(SimGame game, string name)
        { if (!game.Seat(0).View.TryFindResource(name, out var value)) throw new InvalidOperationException("missing visible resource " + name); return value; }
        private static LogisticsOperationalSnapshot Supply(SimGame game)
        {
            var code = game.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(Entity(game, "source"), Resource(game, "supply"), out var value);
            if (!code.IsNone || value == null) throw new InvalidOperationException("source diagnostics " + code); return value;
        }
        private static Fix Wrench(SimGame game, string entity)
        { var rows = new List<Facet>(); game.Seat(0).View.Inspect(Entity(game, entity), rows); return rows.Where(x => x.Key == Storage.FacetStock && x.Subject == "acquisition.wrench").Aggregate(Fix.Zero, (sum, x) => sum + x.Value); }
        private static CommandResult Submit(Check c, SimGame game, Command command)
        {
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary player command admission"); c.Equal(hash, game.Hash(), "command preview pure");
            int sequence = game.Seat(0).Submit(command); Step(game); long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
            var result = results.Single(x => x.Sequence == sequence); c.That(result.Accepted, "ordinary command accepted"); return result;
        }
        private static void Reach(SimGame game, SimGame twin, Func<bool> done, string stage)
        {
            var watchdog = Stopwatch.StartNew();
            var trail = new Queue<string>();
            bool Finished()
            {
                var orders = new List<OrderView>(); Work(game).CopyOrders(orders);
                var facts = new List<string>();
                foreach (var order in orders)
                {
                    var nodes = new List<ActivityNodeView>(); Work(game).TryCopyNodes(order.Id, nodes);
                    facts.Add(order.Id + ":" + string.Join(",", nodes.Select(x => x.Id + "/" + x.State + "/" + x.Progress.WholeTicks + "/" + x.Worker + "/" + x.Blocker + "/" + string.Join("+", x.Choices.Select(y => y.Entry)))));
                }
                var jobs = new List<WorkJobView>(); Work(game).CopyJobs(jobs);
                facts.Add("jobs=" + string.Join(",", jobs.Select(x => x.Id + "/" + x.Worker + "/" + x.DestinationDockSlot)));
                facts.Add("positions=" + string.Join(",", game.Seat(0).Snapshot.Entities.Select(x => x.Id + "/" + ((PointSnapshot)x.Spatial).Position)));
                trail.Enqueue(game.Clock.StepIndex + "=" + string.Join(";", facts)); if (trail.Count > 16) trail.Dequeue();
                try { return done(); }
                catch (InvalidOperationException ex) { throw new InvalidOperationException(stage + " " + string.Join(" | ", trail), ex); }
            }
            for (int i = 0; i < 512 && watchdog.Elapsed.TotalSeconds < 20 && !Finished(); i++) Step(game, twin);
            if (!done())
            { var rows = new List<OrderView>(); Work(game).CopyOrders(rows); throw new InvalidOperationException(stage + " exceeded512Updates/20s step=" + game.Clock.StepIndex + " orders=" + string.Join(";", rows.Select(x => x.Id + "/" + x.State + "/" + x.Blocker)) + " stock=" + Supply(game).Stock + " trail=" + string.Join(" | ", trail)); }
        }
        private static bool Complete(SimGame game, string activity)
            => Work(game).TryReadActivityCompletion("activity:" + activity, Entity(game, "source"), out _);
        private static GameFactory Factory(bool direct, bool half)
        {
            var module = new RequirementAcquisitionModule(); var data = Json.Parse(FrameworkProofContent.Read("requirement-acquisition.json"));
            if (!direct) foreach (var kinds in new[] { ("independent_worker_roles", "worker_roles"), ("independent_staffed_executors", "staffed_activity_executors"),
                ("independent_requirement_groups", "work_requirement_groups"), ("independent_target_resources", "target_resource_requirements") })
            { data.Set(kinds.Item2, data.Get(kinds.Item1)); data.Remove(kinds.Item1); }
            StartupContentAuthoring.Position(data, "acquisition.second", JsonValue.Number("96"));
            StartupContentAuthoring.Position(data, "acquisition.third", JsonValue.Number("98"));
            data.Set("point_interval_regions", Json.Parse("[{\"id\":\"acquisition.left\",\"space\":\"field.points\",\"minimum\":0,\"maximum\":50},{\"id\":\"acquisition.right\",\"space\":\"field.points\",\"minimum\":50,\"maximum\":100}]"));
            return ConstructionPlanTests.Points(extraModules: new[] { module },
                extraDeclarations: "[{\"id\":\"proof.requirement-acquisition\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"requirement-acquisition.json\"]}]",
                extraDocuments: new Dictionary<string, string> { { "requirement-acquisition.json", data.ToString() } }, startupProfiles: new[] { "acquisition.start" },
                scenarioEdit: s => s.Get("modules").Add(JsonValue.Of(module.Id)), worldEdit: root =>
                {
                    root.Get("entities")[0].Get("components").Get("point_stock").Set("initial", JsonValue.Number("4"));
                    StartupContentAuthoring.Position(root, "source", JsonValue.Number("50"));
                    root.Get("entities")[0].Get("components").Get("point_presence").Set("position", JsonValue.Number("50"));
                    root.Get("entities")[0].Get("components").Set("point_dock", Json.Parse("{\"resources\":[],\"work_roles\":[\"acquisition.inspect\"],\"slots\":[{\"id\":\"left\",\"kind\":\"service\",\"offset\":-1},{\"id\":\"right\",\"kind\":\"service\",\"offset\":1},{\"id\":\"exit.left\",\"kind\":\"departure\",\"offset\":-2},{\"id\":\"exit.right\",\"kind\":\"departure\",\"offset\":2},{\"id\":\"exit.far\",\"kind\":\"departure\",\"offset\":4}]}"));
                    root.Get("entities")[2].Get("components").Get("worker").Get("roles").Add(JsonValue.Of("acquisition.inspect"));
                    root.Get("entities")[2].Get("components").Get("storage").Set("initial_stock", Json.Parse("{\"acquisition.wrench\":1}"));
                    StartupContentAuthoring.Position(root, "machine", JsonValue.Number("4"));
                    if (half) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
                });
        }
        private static void Commitments(Check c, SimGame game, int order)
        {
            var rows = Nodes(game, order).Where(x => x.Id != "join").ToArray(); ResourceId supply = Resource(game, "supply"), wrench = Resource(game, "acquisition.wrench");
            c.That(rows.Length == 2 && rows.All(x => x.Worker.IsValid) && rows.Select(x => x.Worker).Distinct().Count() == 2, "two distinct actual workers own the parallel set");
            c.That(rows.All(x => x.SelectedRole == new WorkerRoleId("acquisition.inspect")), "role alternatives select compatible typed guard, never an incompatible inspect fallback");
            c.That(rows.All(x => x.Choices.Any(y => y.Entry == "z.feasible")), "joint all-any selection uses feasible pair rather than individual expensive branch");
            c.That(rows.All(x => x.Resources.Count == 3 && x.Resources.Count(y => y.Choice.Use == WorkResourceUse.Hold && y.Receipts.Count == 1 && !y.Spent) == 2), "each complete native set holds target and real worker tools");
            c.That(rows.All(x => x.Resources.Single(y => y.Choice.Use == WorkResourceUse.Consume).Spent
                && x.Resources.Single(y => y.Choice.Use == WorkResourceUse.Consume).Choice.Quantities.AmountOf(supply) == Fix.One), "both original material occurrences consume exactly one once");
            c.That(rows.All(x => x.Resources.Single(y => y.Choice.Requirement == "acquisition.worker-tool").Choice.Quantities.AmountOf(wrench) == Fix.One), "worker tools have their own actual catalog resource and endpoint");
            c.That(rows.SelectMany(x => x.Resources).Where(x => x.Choice.Requirement == "acquisition.small-tool").Select(x => x.Choice.Endpoint).Distinct().Count() == 1,
                "target tools share the one scarce physical source");
            var diagnostics = Supply(game); c.That(diagnostics.Stock == Fix.FromInt(2) && diagnostics.OutboundPromised == Fix.FromInt(2), "two material units consumed, both remaining physical supply protected as tools");
            c.That(Nodes(game, order).Single(x => x.Id == "join").Progress.WholeTicks == 0, "join cannot bypass active prerequisites");
        }
        private static void Physical(Check c, bool direct, bool half)
        {
            var factory = Factory(direct, half); var game = ConstructionPlanTests.NewPoints(factory); var idle = ConstructionPlanTests.NewPoints(factory); ulong idleHash = idle.Hash();
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "role/material/tool manufacturing needs no business module");
            var limits = new ReplayLimits(4 * 1024 * 1024, 4000); var recording = game.RecordReplay(limits);
            foreach (string worker in new[] { "machine", "acquisition.second", "acquisition.third" })
                Submit(c, game, new ConfigureWorkPolicyCommand { Worker = Entity(game, worker), Settings = new WorkPolicySettings(true, 0,
                    new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect"), new WorkerRoleId("acquisition.inspect") }, worker == "machine" ? "acquisition.left" : "acquisition.right", true, true, WorkAssignmentMode.All) });
            int parallel = checked((int)Submit(c, game, new PostOrderCommand { Plan = "acquisition.parallel", Target = Entity(game, "source") }).Arg0);
            Reach(game, null, () => Nodes(game, parallel).Count(x => x.Id != "join" && x.State == NodeState.Working && x.Progress.WholeTicks > 0) == 2, "parallel physically earned acquisition");
            Commitments(c, game, parallel); var retained = Nodes(game, parallel).First(x => x.Id == "alpha");
            c.That(game.Seat(0).Snapshot.Entities.Where(x => x.Id == retained.Worker).All(x => ((PointSnapshot)x.Spatial).Position == Fix.FromInt(49) || ((PointSnapshot)x.Spatial).Position == Fix.FromInt(51)), "assigned machine physically travelled to its source service bay before earning work");
            var twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "restored active parallel set has identical authoritative state");
            Commitments(c, twin, parallel);
            c.That(Nodes(game, parallel).SelectMany(x => x.Resources).SelectMany(x => x.Receipts).SequenceEqual(
                Nodes(twin, parallel).SelectMany(x => x.Resources).SelectMany(x => x.Receipts)),
                "restored claims preserve actual endpoint tokens, owners, quantities and directions");
            game.Clock.Pause(); twin.Clock.Pause(); ulong paused = game.Hash(); long at = game.Clock.StepIndex; Step(game, twin);
            c.That(game.Hash() == paused && game.Clock.StepIndex == at, "sole paused clock retains complete physical commitments"); game.Clock.Resume(); twin.Clock.Resume();
            int blocked = checked((int)Submit(c, game, new PostOrderCommand { Plan = "acquisition.single", Target = Entity(game, "source") }).Arg0);
            var waiting = Nodes(game, blocked).Single();
            c.That(!waiting.Worker.IsValid && waiting.Resources.Count == 0 && waiting.Choices.Count == 0 && !waiting.Blocker.IsNone && waiting.Progress.WholeTicks == 0,
                "contending task commits no worker, material, tool or branch prefix");
            Commitments(c, game, parallel); ulong hash = game.Hash(); var proposed = new List<WorkRequirementView>();
            c.Equal(Code.None, Work(game).PreviewOrderRequirements("acquisition.single", Entity(game, "source"), proposed), "same evaluator diagnoses actual shortage");
            c.That(proposed.Any(x => x.Result.Status != WorkRequirementStatus.Satisfied), "typed non-readiness reported without inventing stock");
            TestKit.AssertUnclaimedRequirementParity(c, game, 0, "acquisition.single", Entity(game, "source"), blocked, waiting.Id);
            var foreign = new List<ActivityNodeView>(); c.That(!Work(game, 1).TryCopyNodes(parallel, foreign) && foreign.Count == 0, "parallel tool/resource commitments company-private");
            c.Equal(hash, game.Hash(), "contention/private projections remain pure");
            twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "actual blocked contention checkpoint before cancellation");
            Commitments(c, twin, parallel);
            var loadedWaiting = Nodes(twin, blocked).Single();
            c.That(!loadedWaiting.Worker.IsValid && loadedWaiting.Resources.Count == 0 && loadedWaiting.Choices.Count == 0 &&
                loadedWaiting.Progress.Equals(waiting.Progress) && loadedWaiting.Blocker == waiting.Blocker,
                "restored contending node gains no worker, branch, resource or productive prefix");
            TestKit.AssertUnclaimedRequirementParity(c, twin, 0, "acquisition.single", Entity(twin, "source"), blocked, loadedWaiting.Id);
            int cancel = game.Seat(0).Submit(new CancelOrderCommand { Order = blocked }); twin = factory.Load(game.Save()); Step(game, twin);
            long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
            c.That(results.Single(x => x.Sequence == cancel).Accepted, "saved pending cancellation of blocked work accepted"); Commitments(c, game, parallel);
            Reach(game, twin, () => Complete(game, "acquisition.parallel"), "parallel predecessors and earned join");
            c.That(Supply(game).Stock == Fix.FromInt(2) && Supply(game).OutboundPromised == Fix.Zero, "full completion releases original tools, never refunds consumed material");
            int later = checked((int)Submit(c, game, new PostOrderCommand { Plan = "acquisition.single", Target = Entity(game, "source") }).Arg0);
            Reach(game, null, () => Nodes(game, later).Single().Progress.WholeTicks > 0, "released stock supports later physical all-any work");
            c.That(Nodes(game, later).Single().Choices.Any(x => x.Entry == "z.feasible"), "only actual remaining feasible branch selected after prior consumption");
            twin = factory.Load(game.Save()); Reach(game, twin, () => Complete(game, "acquisition.single"), "later original committed work completes");
            c.That(Supply(game).Stock == Fix.One && Supply(game).OutboundPromised == Fix.Zero, "three total consumptions conserved and final held tools released");
            c.That(new[] { "machine", "acquisition.second", "acquisition.third" }.All(x => Wrench(game, x) == Fix.One), "every physical worker wrench remains exactly once");
            c.That(retained.Progress.WholeTicks > 0 && retained.Progress.WholeTicks < retained.RequiredTicks && retained.Resources.Count == 3, "retained partial node facts detached from later completion");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "terminal original graph/job/resource state saves"); c.Equal(idleHash, idle.Hash(), "interleaved idle session isolated");
            c.That(new ReplayVerifier().Verify(factory, recording.Finish(), limits).Success, "public role/parallel/blocked/cancel/later-work inputs replay identically");
        }
        private static void Run(Check c)
        { c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current engine identity unchanged"); foreach (bool direct in new[] { false, true }) foreach (bool half in new[] { false, true }) Physical(c, direct, half); }
    }
}
