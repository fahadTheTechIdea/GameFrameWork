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
using static Beep.SimEngine.Tests.EmergencyPreemptionTests;

namespace Beep.SimEngine.Tests
{
    internal static class FrontierTeamFeasibilityTests
    {
        internal const string Case = "Response frontier teams: feasible alternatives, dispatch parity and saved physical work";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static readonly Code Source = new Code("damage");
        private static GameFactory Fixture(bool alternate = true, int stock = 8)
            => IncidentPreparednessTests.Fixture(fleet: alternate ? new[] { 1, 2, 2 } : new[] { 1, 2 },
                documentName: "prepared-frontier-resources.json", edit: root =>
                {
                    var parts = root.Get("entities")[1].Get("components");
                    parts.Get("point_stock").Set("initial", JsonValue.Number(stock.ToString()));
                    var dock = Json.Parse(FrameworkProofContent.Read("point-docks.json")).Get("entities")[1].Get("components").Get("point_dock").Clone();
                    dock.Set("work_roles", Json.Parse("[\"haul\",\"inspect\"]"));
                    dock.Get("slots").Add(Json.Parse("{\"id\":\"unload.bay.b\",\"kind\":\"service\",\"offset\":3}"));
                    parts.Set("point_dock", dock);
                    root.Get("point_interval_regions").Add(Json.Parse("{\"id\":\"teams.near\",\"space\":\"field.points\",\"minimum\":0,\"maximum\":12}"));
                    var policy = Json.Parse(FrameworkProofContent.Read("worker-standby.json")).Get("work_policies")[0].Clone();
                    policy.Set("id", JsonValue.Of("teams.near")); policy.Set("area", JsonValue.Of("teams.near"));
                    var policies = Json.Parse("[]"); policies.Add(policy); root.Set("work_policies", policies);
                    for (int i = 2; i < 4; i++) root.Get("entities")[i].Get("components").Get("work_policy").Set("profile", JsonValue.Of("teams.near"));
                });
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 617 }; request.Mods.Add("proof.shipments");
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static IncidentPreparednessSnapshot Read(SimGame game)
        {
            Code code = game.Seat(0).View.Feature<IncidentsView>().ReadPreparedness(Find(game, "destination").Id, Source, out var facts);
            if (!code.IsNone) throw new InvalidOperationException("team preparedness refused: " + code); return facts;
        }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("team Update: " + game.Fault + "/" + twin?.Fault);
        }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 300 && !done(); i++) Step(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " jobs=" +
                string.Join(";", Board(game).Jobs.Select(j => j.Id + "/" + j.State + "/" + j.Worker + "/" + j.ToilIndex + "/" + j.DestinationDockSlot + "/" + j.Blocker)) +
                " machines=" + string.Join(";", game.World.Entities.All.Where(e => e.Has<IWorker>()).Select(e => e.Def.Id + "/" + e.Get<PointPresence>().Position + "/" + e.Get<IMotion>().Status + "/" + e.Get<IWorker>().Job)) +
                " nodes=" + string.Join(";", Board(game).Orders.SelectMany(o => o.Nodes.Select(n => n.Id + "/" + n.State + "/" + n.Progress + "/" + n.Blocker))));
        }
        private static void Matching(Check c)
        {
            var a = new EntityId(1); var b = new EntityId(2); var d = new EntityId(3); var e = new EntityId(4);
            var edges = new IReadOnlyList<EntityId>[] { new[] { a, b, d }, new[] { e } };
            var original = DistinctWorkerMatching.Solve(new[] { 2, 1 }, edges); int calls = 0;
            var selected = DistinctWorkerMatching.Solve(new[] { 2, 1 }, edges, matches =>
            {
                calls++; c.Equal(2, matches.Count(m => m.Demand == 0), "every evaluated team preserves first actual demand coverage");
                c.Equal(1, matches.Count(m => m.Demand == 1), "complete sets preserve later coverage");
                return matches.Any(m => m.Worker == d);
            });
            c.That(calls > 1 && selected.Select(m => m.Worker).SequenceEqual(new[] { a, d, e }), "iterative set search finds actual alternate worker without redundant demand permutations");
            c.That(original.Select(m => m.Worker).SequenceEqual(new[] { a, b, e }), "retained original assignment remains detached");
            c.Throws<NotSupportedException>(() => ((IList<DistinctWorkerMatch>)selected).Clear(), "feasibility callback/results cannot mutate assignment lists");
            var refused = DistinctWorkerMatching.Solve(new[] { 2, 1 }, edges, matches => false);
            c.That(refused.Select(m => m.Worker).SequenceEqual(original.Select(m => m.Worker)), "no feasible set preserves original real coverage, not fake readiness or dropped work");
            var scarce = DistinctWorkerMatching.Solve(new[] { int.MaxValue, 1 }, new IReadOnlyList<EntityId>[] { new[] { a, b }, new[] { a } }, matches => false);
            c.That(scarce.Count == 2 && scarce.All(m => m.Demand == 0), "huge requested capacity uses actual workers only and preserves higher-priority coverage");
            c.Throws<InvalidOperationException>(() => DistinctWorkerMatching.Solve(new[] { 1 }, new IReadOnlyList<EntityId>[] { new[] { a } },
                matches => throw new InvalidOperationException("provider failure")), "predicate fault never becomes substituted admission");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "joint team simulation identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "matching introduces no persistent authority"); Matching(c);
            var factory = Fixture(); var game = New(factory); var hash = game.Hash(); var facts = Read(game);
            Entity first = Find(game, "machine"), near = Find(game, "prepared.worker.1"), alternate = Find(game, "prepared.worker.2");
            c.That(game.World.Space is PointSpace && !game.World.Services.Has<IEconomy>(), "independent engine space and optional money absent");
            c.That(facts.Prospective && Board(game).Orders.Count == 0 && Board(game).Jobs.Count == 0, "team preview posts no gameplay");
            c.That(facts.ReadyForFrontier && facts.FrontierWorkersCovered == 2, "actual alternate team resolves shared productive service conflict");
            c.Equal(first.Id, facts.Nodes.Single(n => n.Id == "a.flex").CoveredWorker, "preferred first demand retains actual machine");
            c.Equal(alternate.Id, facts.Nodes.Single(n => n.Id == "b.inspect").CoveredWorker, "alternate inspector selected instead of individually-ready conflicting near inspector");
            c.That(facts.Nodes.Single(n => n.Id == "b.inspect").Candidates.Any(n => n.Worker == near.Id && n.Result.Status == WorkRequirementStatus.Satisfied), "rejected team member was genuinely individually admissible");
            c.That(facts.FrontierResources.Value.Ready && facts.FrontierCostBlocker.Value.IsNone && facts.FrontierDockBlocker.Value.IsNone, "same joint material, optional cost and dock owners admit selected contexts");
            c.Equal(new Code("unload.bay.b"), facts.FrontierDocks.Single(d => d.Worker == alternate.Id).Slot, "alternate uses actual disjoint authored service bay");
            c.Equal(hash, game.Hash(), "search does not acquire worker, material or slot");
            c.Equal(hash, factory.Load(game.Save()).Hash(), "prospective search adds no saved state");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<IncidentsView>().ReadPreparedness(Find(game, "destination").Id, Source, out var hidden), "private company team facts stay private");
            c.That(hidden == null && game.Hash() == hash, "foreign refusal has no partial team or mutation");
            c.Throws<NotSupportedException>(() => ((IList<IncidentPreparednessNode>)facts.Nodes).Clear(), "retained team facts immutable");

            var disabled = New(Fixture()); var worker = Find(disabled, "prepared.worker.2"); var policy = WorkPolicyRules.Read(worker);
            c.That(TestKit.Order(disabled, 0, new ConfigureWorkPolicyCommand { Worker = worker.Id, Settings = new WorkPolicySettings(false,
                policy.Priority, policy.AllowedRoles, policy.Area, policy.Interruptible, policy.EmergencyEligible, policy.AssignmentMode) }).Accepted, "player disables alternate through existing policy command");
            var blocked = Read(disabled);
            c.That(!blocked.ReadyForFrontier && blocked.FrontierWorkersCovered == 2 && blocked.FrontierDockBlocker == WorkDockCodes.Full, "no feasible alternate never drops work or fabricates service");
            c.That(facts.ReadyForFrontier && facts.Nodes.Single(n => n.Id == "b.inspect").CoveredWorker == alternate.Id, "retained successful team unaffected by later separate session policy");
            var shortStock = Read(New(Fixture(stock: 4)));
            c.That(!shortStock.ReadyForFrontier && !shortStock.FrontierResources.Value.Ready, "changing workers never bypasses actual combined material shortage");

            Order order = Raise(c, game);
            Until(game, () => order.Nodes.Take(2).All(n => Board(game).Job(n.JobId)?.Worker.IsValid == true), "selected native team acquired");
            Job jobA = Board(game).Job(order.Node("a.flex").JobId), jobB = Board(game).Job(order.Node("b.inspect").JobId);
            c.Equal(first.Id, jobA.Worker, "normal dispatch agrees with first preview member");
            c.Equal(alternate.Id, jobB.Worker, "fresh dispatch uses feasible alternate after first real acquisition");
            c.That(near.Get<IWorker>().Idle && !Board(game).IsDockWaiting(jobB), "no engine-invented assignment to conflicting idle inspector or waiting berth");
            c.That(Read(game).ReadyForFrontier, "live incumbents remain fixed and exclude only their own real service claims");
            c.Equal(Fix.FromInt(8), Find(game, "destination").Get<IOutletPort>().Amount(Supply(game)), "physical travel does not instantly consume work materials");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual acquired jobs save through original owners");
            game.Clock.Pause(); twin.Clock.Pause(); hash = game.Hash(); long time = game.Tick; Step(game, twin); Read(game);
            c.That(game.Hash() == hash && game.Tick == time, "sole paused clock freezes selected team's physical work");
            game.Clock.Resume(); twin.Clock.Resume();
            Until(game, () => order.Node("a.flex").Progress.WholeTicks > 0 && order.Node("b.inspect").Progress.WholeTicks > 0, "both real service jobs productive", twin);
            c.Equal(game.Hash(), twin.Hash(), "saved chosen team advances identically");
            twin = factory.Load(game.Save());
            Until(game, () => order.State == OrderState.Done, "physical team work, departure and restart", twin);
            c.Equal(game.Hash(), twin.Hash(), "saved partial progress finishes identically");
            c.That(!Find(game, "destination").Get<IIncidentResponse>(Source).ReadIncident().Active, "only physical authored completion resolves incident");
            c.Equal(Fix.Zero, Find(game, "destination").Get<IOutletPort>().Amount(Supply(game)), "native resource owner consumes eight once");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed team requires no separate matching codec");

            factory = Fixture(alternate: false); game = New(factory); c.That(!Read(game).ReadyForFrontier, "simultaneous infeasibility remains honest");
            order = Raise(c, game);
            Until(game, () => order.Nodes.Take(2).All(n => Board(game).Job(n.JobId)?.Worker.IsValid == true), "ordinary physical queue still admitted");
            c.That(order.Nodes.Take(2).Any(n => Board(game).IsDockWaiting(Board(game).Job(n.JobId))), "no feasible simultaneous team uses real waiting, not starvation");
            twin = factory.Load(game.Save());
            Until(game, () => order.Node("a.flex").State == NodeState.Done &&
                Board(game).Job(order.Node("b.inspect").JobId)?.Blocker == WorkDockCodes.Occupied, "actual occupied departure reports blockage", twin);
            c.That(order.Node("b.inspect").Progress.WholeTicks == 12 && order.Node("b.inspect").State != NodeState.Done,
                "earned work alone never bypasses occupied physical exit");
            EntityId homeWorker = Find(game, "machine").Id, home = Find(game, "source").Id;
            game.Seat(0).Submit(new SetHomeCommand { Worker = homeWorker, Home = home });
            twin.Seat(0).Submit(new SetHomeCommand { Worker = homeWorker, Home = home }); Step(game, twin);
            c.Equal(home, Find(game, "machine").Get<IWorker>().Home, "normal player home intent lets the engine clear exit over time");
            Until(game, () => order.State == OrderState.Done, "queued original team physically completes", twin);
            c.Equal(game.Hash(), twin.Hash(), "fallback sequencing remains deterministic through saved actual queue");
            c.Equal(Fix.Zero, Find(game, "destination").Get<IOutletPort>().Amount(Supply(game)), "queued original team also conserves actual materials");
        }
    }
}
