using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class MixedProductiveSchedulingTests
    {
        internal const string Case = "Mixed productive scheduling: ranked machine-timer quanta, shared inputs and saved physical work";
        private static readonly PlayerId Company = new PlayerId(0);
        private static readonly ReplayLimits Limits = new ReplayLimits(16 * 1024 * 1024, 12000);
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(int stock = 20) => IncidentPreparednessTests.Fixture(
            documentName: "mixed-productive-scheduling.json",
            edit: root => root.Get("entities")[0].Get("components").Get("point_stock").Set("initial", JsonValue.Number(stock.ToString())));
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest
            { Scenario = "points", Seed = 291, Mods = { "proof.shipments" }, Seats = { Controller.Human, Controller.Human } });
        private static EntityId Visible(SimGame game, string prototype)
            => game.Seat(0).Snapshot.Entities.Single(e => e.Owner == Company && e.Def == prototype).Id;
        private static Entity Actual(SimGame game, string prototype)
            => game.World.Entities.All.Single(e => e.Alive && e.Def.Id == prototype);
        private static WorkView Work(SimGame game) => game.Seat(0).View.Feature<WorkView>();
        private static List<ActivityNodeView> Nodes(SimGame game, int id)
        { var nodes = new List<ActivityNodeView>(); if (!Work(game).TryCopyNodes(id, nodes)) throw new InvalidOperationException("missing mixed nodes"); return nodes; }
        private static ActivityNodeView Node(SimGame game, int id, string node) => Nodes(game, id).Single(n => n.Id == node);
        private static List<WorkJobView> Jobs(SimGame game)
        { var jobs = new List<WorkJobView>(); Work(game).CopyJobs(jobs); return jobs; }
        private static bool Completed(SimGame game, string plan, int id, string target = "source")
            => Work(game).TryReadActivityCompletion(plan, Visible(game, target), out var completion) && completion.Order == id;
        private static Fix Stock(SimGame game, EntityId target)
        {
            var facets = new List<Facet>(); game.Seat(0).View.Inspect(target, facets);
            return facets.Where(f => f.Key == Storage.FacetStock && f.Subject == "supply").Aggregate(Fix.Zero, (sum, f) => sum + f.Value);
        }
        private static Fix Total(SimGame game) => game.Seat(0).Snapshot.Entities.Where(e => e.Owner == Company)
            .Aggregate(Fix.Zero, (sum, e) => sum + Stock(game, e.Id));
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.RunTicks(1); twin?.RunTicks(1);
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("mixed time=" + game.Tick + "/fault=" + game.Fault + "/twin=" + twin?.Fault);
        }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 2000 && !done(); i++) Step(game, twin);
            if (!done()) throw new InvalidOperationException(stage + "/time=" + game.Tick + "/jobs=" +
                string.Join(";", Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.Blocker + "/" + j.Worker)));
        }
        private static CommandResult Command(Check c, SimGame game, Command command)
        {
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "player command previews through engine");
            c.Equal(hash, game.Hash(), "preview grants no work/claims/resources");
            var result = TestKit.Order(game, 0, command);
            c.That(result.Accepted, "player command accepted: " + result.Refusal);
            if (!result.Accepted) throw new InvalidOperationException("mixed command refused: " + result.Refusal);
            return result;
        }
        private static void Ordinary(Check c, SimGame game)
        {
            foreach (string prototype in new[] { "machine", "prepared.worker.1" })
                Command(c, game, new ConfigureWorkPolicyCommand { Worker = Visible(game, prototype),
                    Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") },
                        null, true, true, WorkAssignmentMode.All) });
            Command(c, game, new ConfigureCompanyAlertCommand { Company = Company,
                Settings = new CompanyAlertSettings("alerts.neutral", CompanyAlertLevel.Normal, Array.Empty<WorkerReserveSetting>()) });
        }
        private static int Post(Check c, SimGame game, string plan, int priority = 30)
            => checked((int)Command(c, game, new PostOrderCommand { Plan = plan, Target = Visible(game, "source"), Priority = priority }).Arg0);
        private static void StartPair(Check c, SimGame game, int id)
        {
            var pair = Nodes(game, id).Where(n => n.Id != "finish").ToArray();
            c.That(pair.Length == 2 && pair.All(n => n.State == NodeState.Working && n.Progress.Equals(WorkProgress.Zero)),
                "mixed acquisition starts real nodes with zero same-update work");
            c.That(pair.Single(n => n.Id == "machine").Worker.IsValid && !pair.Single(n => n.Id == "timer").Worker.IsValid,
                "machine has actual worker; timer has no fabricated worker");
        }
        private static void Together(Check c)
        {
            var factory = Factory(); var game = New(factory); Ordinary(c, game);
            using (var recorder = game.RecordReplay(Limits))
            {
                int id = Post(c, game, "mixed.pair"); StartPair(c, game, id);
                var twin = factory.Load(game.Save()); Step(game, twin);
                c.That(Node(game, id, "machine").Progress.WholeTicks == 1 && Node(game, id, "timer").Progress.WholeTicks == 1 &&
                    Node(game, id, "machine").ProductiveSteps == 1 && Node(game, id, "timer").ProductiveSteps == 1,
                    "real machine and timer earn exactly one jointly funded quantum");
                c.Equal(Fix.FromInt(18), Stock(game, Visible(game, "source")), "complete mixed quantum consumes exactly two shared units");
                c.That(Node(game, id, "finish").Progress.Equals(WorkProgress.Zero), "dependency cannot borrow earlier productive work");
                twin = factory.Load(game.Save());
                Until(game, () => Node(game, id, "machine").Progress.WholeTicks == 4 && Node(game, id, "timer").Progress.WholeTicks == 4,
                    "mixed productive completion before physical departure", twin);
                c.That(Node(game, id, "machine").ProductiveSteps == 4 && Node(game, id, "timer").ProductiveSteps == 4 &&
                    Node(game, id, "finish").ProductiveSteps == 0, "node accounting records actual exposure and gives no dependent same-update quantum");
                long beforeDeparture = game.Tick;
                Until(game, () => Completed(game, "activity:mixed.pair", id), "mixed departure/dependent timer/history", twin);
                c.That(game.Tick > beforeDeparture, "real departure and dependent work precede durable completion history");
                c.Equal(Fix.FromInt(12), Stock(game, Visible(game, "source")), "complete mixed activity consumes eight units once");
                var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(),
                    "joint machine/timer physical work and saved stages replay: " + replay.Failure + "/" + replay.Detail);
            }
            var cancelled = New(factory); Ordinary(c, cancelled); int cancelId = Post(c, cancelled, "mixed.pair"); Step(cancelled);
            Command(c, cancelled, new CancelOrderCommand { Order = cancelId });
            var active = new List<OrderView>(); Work(cancelled).CopyOrders(active);
            c.That(active.All(o => o.Id != cancelId) && !Completed(cancelled, "activity:mixed.pair", cancelId) &&
                Stock(cancelled, Visible(cancelled, "source")) == Fix.FromInt(18),
                "cancellation stops next quantum and preserves already consumed work");
            var saved = factory.Load(cancelled.Save()); Step(cancelled, saved);
            c.That(Jobs(cancelled).All(j => j.State != JobState.Assigned && j.State != JobState.Paused),
                "cancelled mixed activity has no retained physical job");
        }
        private static void Priorities(Check c)
        {
            foreach (bool timerWins in new[] { false, true })
            foreach (bool editPriority in new[] { false, true })
            {
                var factory = Factory(3); var game = New(factory); Ordinary(c, game);
                game.Seat(0).Submit(new PostOrderCommand { Plan = "mixed.timer", Target = Visible(game, "source"), Priority = timerWins ? 100 : 1 });
                game.Seat(0).Submit(new PostOrderCommand { Plan = "mixed.machine", Target = Visible(game, "source"), Priority = timerWins ? 1 : 100 });
                Step(game);
                var orders = new List<OrderView>(); Work(game).CopyOrders(orders);
                int timer = orders.Single(o => o.Plan == "activity:mixed.timer").Id, machine = orders.Single(o => o.Plan == "activity:mixed.machine").Id;
                Step(game); c.That(Node(game, timer, "timer").Progress.WholeTicks == 1 && Node(game, machine, "machine").Progress.WholeTicks == 1,
                    "separate orders initially share one complete quantum");
                c.Equal(Fix.One, Stock(game, Visible(game, "source")), "one competing running unit remains");
                var twin = factory.Load(game.Save());
                if (editPriority)
                {
                    int changed = timerWins ? machine : timer;
                    game.Seat(0).Submit(new SetOrderPriorityCommand { Order = changed, Priority = 200 });
                    twin.Seat(0).Submit(new SetOrderPriorityCommand { Order = changed, Priority = 200 });
                }
                Step(game, twin);
                bool actualTimerWins = timerWins != editPriority;
                var winner = actualTimerWins ? Node(game, timer, "timer") : Node(game, machine, "machine");
                var loser = actualTimerWins ? Node(game, machine, "machine") : Node(game, timer, "timer");
                c.That(winner.Progress.WholeTicks == 2 && loser.Progress.WholeTicks == 1 &&
                    loser.State == NodeState.Paused && !loser.Blocker.IsNone, "current player priority, not machine/order enumeration, wins scarce input");
                c.Equal(Fix.Zero, Stock(game, Visible(game, "source")), "scarce fallback neither duplicates nor borrows running stock");
                c.That(Node(game, timer, "timer").ProductiveSteps + Node(game, machine, "machine").ProductiveSteps == 3,
                    "aggregate productive accounting equals actual three consumed units");
            }
        }
        private static void Replenish(Check c)
        {
            var factory = Factory(2); var game = New(factory); Ordinary(c, game);
            using (var recorder = game.RecordReplay(Limits))
            {
                int id = Post(c, game, "mixed.pair"); Step(game);
                Step(game);
                c.That(Node(game, id, "machine").Progress.WholeTicks == 1 && Node(game, id, "timer").Progress.WholeTicks == 1 &&
                    Nodes(game, id).Where(n => n.Id != "finish").All(n => n.State == NodeState.Paused),
                    "real exhausted mixed work pauses without earning unfunded progress");
                var twin = factory.Load(game.Save()); game.Clock.Pause(); twin.Clock.Pause(); ulong hash = game.Hash(); long time = game.Tick;
                c.Equal(0, game.Update(), "sole clock pause stops all productive kinds"); twin.Update();
                c.That(game.Tick == time && game.Hash() == hash && game.Hash() == twin.Hash(), "paused saved mixed stages earn no time or input");
                game.Clock.Resume(); twin.Clock.Resume();
                if (!game.Seat(0).View.TryFindResource("supply", out var supply)) throw new InvalidOperationException("missing supply");
                Command(c, game, new ConfigureStockPolicyCommand { Store = Visible(game, "source"), Resource = supply,
                    Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.One, Fix.FromInt(8),
                        Fix.FromInt(40), 80, LogisticsReserveAccess.Protected)) });
                twin = factory.Load(game.Save());
                Until(game, () => Jobs(game).Any(j => j.IsHaul && j.Loaded > Fix.Zero), "actual replenishment pickup", twin);
                c.That(Nodes(game, id).Where(n => n.Id != "finish").All(n => n.Progress.WholeTicks == 1),
                    "loaded but undelivered cargo cannot fund productive work");
                twin = factory.Load(game.Save());
                Until(game, () => Completed(game, "activity:mixed.pair", id), "physical delivery resumes machine and timer", twin);
                c.Equal(Fix.FromInt(2), Total(game), "ten initial units minus eight actual productive inputs conserved across stores/cargo");
                var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(),
                    "paused mixed stages and physically delivered replenishment replay: " + replay.Failure + "/" + replay.Detail);
            }
        }
        private static void Fault(Check c)
        {
            var factory = Factory(); var game = New(factory); Ordinary(c, game); int id = Post(c, game, "mixed.pair");
            int claims = 0;
            Actual(game, "source").Get<SingleResourcePort>().AfterReserveOut =
                () => { if (++claims == 2) throw new InvalidOperationException("proof.mixed.second-running-claim"); };
            c.Equal(0, game.RunTicks(1), "second mixed claim failure faults current update before completed time");
            c.That(game.Fault != null && game.Fault.Message.Contains("proof.mixed.second-running-claim") && claims == 2,
                "common group actually acquires both obligations before advancing either node");
            var actual = game.World.Services.Get<WorkBoard>().Order(id);
            c.That(actual.Nodes.Where(n => n.Id != "finish").All(n => n.Progress.Equals(WorkProgress.Zero) && n.ProductiveSteps == 0),
                "machine does not keep a successful prefix when later timer acquisition faults");
            c.Equal(Fix.FromInt(20), Actual(game, "source").Get<IInletPort>().Amount(game.World.Content.ResourceId("supply")),
                "failed grouped acquisition consumed no resource prefix");
            c.Throws<InvalidOperationException>(() => game.Save(), "partial mixed acquisition cannot save");
            c.Throws<InvalidOperationException>(() => game.Hash(), "partial mixed acquisition cannot report valid hash");
            var first = game.Fault; c.Equal(0, game.RunTicks(1), "faulted group is never retried");
            c.That(ReferenceEquals(first, game.Fault) && claims == 2, "first group cause remains stable");
        }
        private static void CustomPhase(Check c)
        {
            // Pure authored fixture; real module/provider and engine loop, not an old runtime case.
            var authored = Json.Parse(FrameworkProofContent.Read("authored-executor-work.json"));
            var timer = Json.Parse(FrameworkProofContent.Read("mixed-productive-scheduling.json")).Get("activities")[0].Get("nodes")[1].Clone();
            timer.Remove("requirement"); timer.Set("id", JsonValue.Of("parallel")); timer.Set("duration", Json.Parse("{\"numerator\":100,\"denominator\":1,\"unit\":\"minute\"}"));
            authored.Get("activities")[0].Get("nodes").Add(timer);
            var factory = AuthoredActivityExecutorTests.Factory(authored.ToString());
            var game = factory.NewGame(new NewGameRequest { Scenario = "native-survey", Seed = 291, Seats = { Controller.Human, Controller.Human } });
            int id = checked((int)Command(c, game, new PostOrderCommand { Plan = "proof.authored-executor", Target = Visible(game, "survey.site"), Priority = 60 }).Arg0);
            Until(game, () => Node(game, id, "operate").Blocker == new Code("proof.phase-wait"), "physical approach to declared custom phase");
            c.That(Node(game, id, "parallel").ProductiveSteps > 0 && Node(game, id, "parallel").ProductiveSteps < 90 &&
                Node(game, id, "operate").ProductiveSteps == 0 &&
                Node(game, id, "operate").Blocker == new Code("proof.phase-wait"),
                "mixed scheduler does not bypass a mod's productive phase delay");
            var twin = factory.Load(game.Save());
            Until(game, () => Node(game, id, "operate").Progress.Complete(Node(game, id, "operate").RequiredTicks) &&
                Node(game, id, "parallel").ProductiveSteps == 100, "authored delayed phase and parallel timer", twin);
            c.That(Node(game, id, "operate").ProductiveSteps > 0 && Node(game, id, "parallel").ProductiveSteps == 100,
                "mod phase proposals join native productive ownership after actual readiness");
            Until(game, () => Completed(game, "activity:proof.authored-executor", id, "survey.site"), "authored completion history", twin);
        }
        private static void Construction(Check c)
        {
            var factory = Colony.Factory(); var game = Colony.New(291);
            Command(c, game, new ConstructCommand { Def = "tower", Priority = 50,
                Placement = TilePlacement.Arguments("colony.tiles", new TileCoord(10, 16)) });
            Until(game, () => Jobs(game).Any(j => j.IsHaul && j.Loaded > Fix.Zero), "physical colony pickup");
            c.That(!game.Seat(0).Snapshot.Entities.Any(e => e.Def == "tower"), "mixed scheduling adds no instant construction path");
            var twin = factory.Load(game.Save());
            Until(game, () => game.Seat(0).Snapshot.Entities.Any(e => e.Def == "tower"), "real delivered machine construction", twin);
            Fix steel = Fix.Zero;
            foreach (var entity in game.Seat(0).Snapshot.Entities.Where(e => e.Owner == Company))
            {
                var facets = new List<Facet>(); game.Seat(0).View.Inspect(entity.Id, facets);
                steel += facets.Where(f => f.Key == Storage.FacetStock && f.Subject == "steel").Aggregate(Fix.Zero, (sum, f) => sum + f.Value);
            }
            c.Equal(Fix.FromInt(20), steel, "real construction retains fifty-minus-thirty material accounting");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "mixed productive scheduling identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "existing node/job/receipt persistence unchanged");
            Together(c); Priorities(c); Replenish(c); Fault(c); CustomPhase(c); Construction(c);
        }
    }
}

