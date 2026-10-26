using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class UnifiedNativeAdmissionTests
    {
        internal const string Case = "Unified native admission: mixed priorities, complete commitments and physical saved continuation";
        private static readonly PlayerId Company = new PlayerId(0);
        private static readonly ReplayLimits Limits = new ReplayLimits(16 * 1024 * 1024, 12000);
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(int source = 20) => IncidentPreparednessTests.Fixture(
            documentName: "unified-native-admission.json",
            edit: root => root.Get("entities")[0].Get("components").Get("point_stock").Set("initial", Content.JsonValue.Number(source.ToString())));
        private static SimGame New(GameFactory factory, string scenario = "points")
        {
            var request = new NewGameRequest { Scenario = scenario, Seed = 286 };
            if (scenario == "points") request.Mods.Add("proof.shipments");
            request.Seats.Add(Controller.Human); if (scenario == "points") request.Seats.Add(Controller.Human);
            return factory.NewGame(request);
        }
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static Entity Find(SimGame game, string prototype) => game.World.Entities.All.Single(e => e.Alive && e.Def.Id == prototype);
        private static EntityId Visible(SimGame game, string prototype) => game.Seat(0).Snapshot.Entities.Single(e => e.Owner == Company && e.Def == prototype).Id;
        private static WorkView Work(SimGame game) => game.Seat(0).View.Feature<WorkView>();
        private static List<ActivityNodeView> Nodes(SimGame game, int id)
        { var rows = new List<ActivityNodeView>(); if (!Work(game).TryCopyNodes(id, rows)) throw new InvalidOperationException("missing native nodes"); return rows; }
        private static List<WorkJobView> Jobs(SimGame game)
        { var rows = new List<WorkJobView>(); Work(game).CopyJobs(rows); return rows; }
        private static OrderView ReadOrder(SimGame game, int id)
        { var rows = new List<OrderView>(); Work(game).CopyOrders(rows); return rows.Single(o => o.Id == id); }
        private static bool Completed(SimGame game, OrderView order)
            => Work(game).TryReadActivityCompletion(order.Plan, order.Target, out var fact) && fact.Order == order.Id;
        private static Fix Stock(SimGame game, EntityId target, string resource = "supply")
        {
            var rows = new List<Facet>(); if (!game.Seat(0).View.Inspect(target, rows)) throw new InvalidOperationException("missing owned stock");
            return rows.Where(f => f.Key == Storage.FacetStock && f.Subject == resource).Aggregate(Fix.Zero, (sum, f) => sum + f.Value);
        }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("mixed fault at " + game.Tick + "/" + game.Fault);
            if (twin == null) return;
            twin.Update(); if (twin.Fault != null || twin.Hash() != game.Hash()) throw new InvalidOperationException("mixed saved divergence at " + game.Tick + "/" + twin.Fault);
        }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 2000 && !done(); i++) Step(game, twin);
            if (!done()) throw new InvalidOperationException(stage + "/time=" + game.Clock.TotalGameTime + "/jobs=" +
                string.Join(";", Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.Blocker + "/" + j.Worker)));
        }
        private static CommandResult Command(Check c, SimGame game, Command command)
        {
            ulong before = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "engine previews player intent");
            c.Equal(before, game.Hash(), "preview reserves and advances nothing");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "player intent accepted: " + result.Refusal);
            if (!result.Accepted) throw new InvalidOperationException("mixed player refused " + result.Refusal); return result;
        }
        private static void Ordinary(Check c, SimGame game)
        {
            Command(c, game, new ConfigureWorkPolicyCommand { Worker = Visible(game, "machine"),
                Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") },
                    null, true, true, WorkAssignmentMode.All) });
            Command(c, game, new ConfigureCompanyAlertCommand { Company = Company,
                Settings = new CompanyAlertSettings("alerts.neutral", CompanyAlertLevel.Normal, Array.Empty<WorkerReserveSetting>()) });
        }
        private static WorkWorkerCandidate Candidate(SimGame game, Job job)
        {
            var order = Board(game).Order(job.OrderId); var execution = order.Node(job.NodeId); var node = order.Plan.Graph.Find(job.NodeId);
            var rows = new List<WorkWorkerCandidate>();
            game.World.Services.Get<IWorkerDispatch>().CopyCandidates(game.World, Company, node.Roles, Find(game, "source"),
                node, job.Id, execution.Choices, execution, WorkCandidateMode.IdleOnly, rows, null);
            return rows.Single(c => c.Worker == Visible(game, "machine") && c.Result.Status == WorkRequirementStatus.Satisfied);
        }
        private static void Protocol(Check c)
        {
            // Engine-side protocol instrumentation only; public commands create actual machine jobs.
            var factory = Factory(); var game = New(factory); Ordinary(c, game);
            Command(c, game, new SetWorkerHeldCommand { Worker = Visible(game, "machine"), Held = true });
            int nativeId = checked((int)Command(c, game, new PostOrderCommand {
                Plan = "unified.staffed", Target = Visible(game, "source"), Priority = 30 }).Arg0);
            var board = Board(game); var staffed = board.Order(nativeId); var job = board.Job(staffed.Node("staffed").JobId);
            var timed = board.Post(game.World.Content.Find<ActivityDef>("unified.timer"), Company, Visible(game, "source"), 30);
            game.World.Services.Get<IWorkerControl>().SetHeld(game.World, Company, Visible(game, "machine"), false);
            var candidate = Candidate(game, job); var port = Find(game, "source").Get<IOutletPort>(); var supply = game.World.Content.ResourceId("supply");
            var owner = new ReservationOwner("proof.unified-competitor", 1); var competitor = port.ReserveOut(owner, supply, Fix.FromInt(16));
            Action<IReadOnlyList<(int Order, string Node)>, string> refuse = (nodes, reason) =>
            {
                ulong hash = game.Hash();
                c.That(!board.Assign(new[] { job.Id }, new[] { candidate }, new Code?[] { null }, nodes), reason + " complete mixed set refuses");
                c.Equal(hash, game.Hash(), reason + " leaves all original nodes/jobs/books/workers unchanged");
            };
            refuse(new[] { (timed.Id, "timer") }, "individually fitting aggregate shortage");
            refuse(new[] { (timed.Id, "timer"), (timed.Id, "timer") }, "duplicate timed identity");
            refuse(new[] { (timed.Id, "timer"), (int.MaxValue, "timer") }, "missing later actual order");
            c.That(job.State == JobState.Open && timed.Node("timer").Resources.Count == 0 && port.Reservations.Count == 1 &&
                port.Reservations[0].Token.Equals(competitor), "refused complete set preserves competitor and has no retained prefix");
            port.ReleaseOut(owner, competitor);
            var offers = new List<WorkerDispatchOffer>(); ulong pure = game.Hash(); Work(game).CopyAssignments(offers);
            var timer = offers.Single(o => !o.RequiresWorker && o.Identity == timed.Id);
            c.That(timer.Ready && !timer.Worker.IsValid && timer.TravelCost == 0 && timer.Entry == "node.timer", "real timer is explicit intent, not missing machine");
            c.Equal(pure, game.Hash(), "mixed queue projection has no acquisition side effects");
            c.Throws<InvalidOperationException>(() => timer.WithCandidate(Visible(game, "machine"), 1), "timed nature cannot become a fake machine candidate");
            c.Throws<ArgumentException>(() => new WorkerDispatchOffer(WorkModule.ModuleId, 1, "job", Company,
                Visible(game, "source"), EntityId.None, true, Code.None, false, 0, game.Tick, game.Tick, 0, 1, 0, true),
                "missing required worker cannot advertise ready timer semantics");
            var hidden = new List<WorkerDispatchOffer>(); game.Seat(1).View.Feature<WorkView>().CopyAssignments(hidden);
            c.That(hidden.Count == 0, "foreign company sees neither machine nor timed intents");
            c.That(board.Assign(new[] { job.Id }, new[] { candidate }, new Code?[] { null }, new[] { (timed.Id, "timer") }), "whole mixed actual set acquires together");
            c.That(job.State == JobState.Assigned && job.Worker.IsValid && timed.Node("timer").State == NodeState.Working &&
                timed.Node("timer").Progress.Equals(WorkProgress.Zero), "complete acquisition binds original machine and timer without progress");
            c.Equal(Fix.FromInt(8), port.ReservedOut(supply), "one real resource owner holds eight combined units");
            c.Equal(Fix.FromInt(20), port.Amount(supply), "acquisition cannot consume or grant productive work");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "existing node/job/receipt codecs save mixed commitments");
            game.Seat(0).Submit(new CancelOrderCommand { Order = staffed.Id });
            game.Seat(0).Submit(new CancelOrderCommand { Order = timed.Id }); Step(game);
            c.Equal(Fix.Zero, port.ReservedOut(supply), "cancellation releases complete original mixed holds");
            c.Equal(Fix.FromInt(20), Stock(game, Visible(game, "source")), "unproductive cancellation conserves actual held tools");
        }
        private static void Priorities(Check c)
        {
            foreach (bool timerWins in new[] { false, true })
            {
                var factory = Factory(4); var game = New(factory); Ordinary(c, game); var target = Visible(game, "source");
                using (var recorder = game.RecordReplay(Limits))
                {
                    var commands = new[] {
                        new PostOrderCommand { Plan = timerWins ? "unified.staffed" : "unified.timer", Target = target, Priority = 1 },
                        new PostOrderCommand { Plan = timerWins ? "unified.timer" : "unified.staffed", Target = target, Priority = 100 } };
                    foreach (var command in commands)
                    {
                        ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "same-pass priorities preview");
                        c.Equal(hash, game.Hash(), "pending priority preview stays pure"); game.Seat(0).Submit(command);
                    }
                    Step(game); var orders = new List<OrderView>(); Work(game).CopyOrders(orders);
                    var high = orders.Single(o => o.Priority == 100); var low = orders.Single(o => o.Priority == 1);
                    var hn = Nodes(game, high.Id).Single(); var ln = Nodes(game, low.Id).Single();
                    c.That(hn.State == NodeState.Working && (timerWins
                        ? !ln.Worker.IsValid && Jobs(game).Single(j => j.Id == ln.JobId).State == JobState.Open
                        : ln.State != NodeState.Working), "higher player priority wins scarce tool across executor natures");
                    c.That(hn.Progress.Equals(WorkProgress.Zero) && hn.Worker.IsValid != timerWins, "winner starts zero-progress with only its real machine requirement");
                    c.Equal(Fix.FromInt(4), Stock(game, target), "ranked admission holds tools, not consumption");
                    var twin = factory.Load(game.Save()); ulong before = game.Hash(); long step = game.Tick;
                    game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
                    c.That(game.Hash() == before && game.Tick == step, "one paused clock freezes both competing natures");
                    game.Clock.Resume(); twin.Clock.Resume();
                    Until(game, () => Completed(game, high), "high-priority physical completion", twin);
                    c.That(!Completed(game, low), "blocked competitor never gains instant completion");
                    twin = factory.Load(game.Save());
                    Until(game, () => Completed(game, low), "released tools permit lower-priority physical work", twin);
                    c.Equal(Fix.FromInt(4), Stock(game, target), "sequential mixed tools conserved exactly");
                    var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                    c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "mixed priority canonical replay: " + replay.Failure + "/" + replay.Detail);
                }
            }
        }
        private static void Response(Check c)
        {
            var factory = Factory(); var game = New(factory); var isolated = New(factory); ulong untouched = isolated.Hash();
            c.That(!game.World.Services.Has<IEconomy>() && !game.World.Services.Has<IMonetaryBudgets>(), "resource-only mixed response needs no business");
            using (var recorder = game.RecordReplay(Limits))
            {
                var target = Visible(game, "destination");
                Command(c, game, new RaiseIncidentCommand { Target = target, Source = new Code("damage"), Kind = new Code("proof.failure") });
                if (!game.Seat(0).View.Feature<IncidentsView>().TryGetResponseOrder(target, new Code("damage"), out int id))
                    throw new InvalidOperationException("missing actual mixed response");
                var order = ReadOrder(game, id); var first = Nodes(game, id).Where(n => n.Id != "restart").ToArray();
                c.That(first.Length == 2 && first.All(n => n.State == NodeState.Working && n.Progress.Equals(WorkProgress.Zero)), "public Update acquires complete timed and travelling machine response");
                c.That(first.Count(n => n.Worker.IsValid) == 1, "response uses exactly one real machine and no timer worker");
                c.Equal(Fix.FromInt(8), Stock(game, target), "full startup commitments leave input stock physically unspent");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "fresh complete mixed response saved");
                Step(game, twin);
                c.That(Nodes(game, id).Single(n => n.Id == "a.timer").Progress.WholeTicks == 1, "timer first earns one productive Update after admission");
                c.Equal(Fix.FromInt(4), Stock(game, target), "only productive timer startup consumes its four units while machine travels");
                twin = factory.Load(game.Save()); Until(game, () => Completed(game, order), "parallel response and dependent physical restart", twin);
                c.Equal(Fix.Zero, Stock(game, target), "timed and machine response consume eight actual units once");
                var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "complete mixed response replay: " + replay.Failure + "/" + replay.Detail);
            }
            c.Equal(untouched, isolated.Hash(), "interleaved independent engine session remains unchanged");
        }
        private static void Budgets(Check c)
        {
            foreach (bool survival in new[] { false, true })
            {
                var factory = WorkBudgetTests.Factory(160, survival: survival,
                    physicalDocuments: new[] { "workerless-frontier-costs.json", "unified-native-costs.json" });
                var game = New(factory, "purchase-haul"); var target = Visible(game, "budget.site-a");
                using (var recorder = game.RecordReplay(Limits))
                {
                    int id = checked((int)Command(c, game, new PostOrderCommand { Plan = "unified.funded", Target = target, Priority = 30 }).Arg0);
                    var order = ReadOrder(game, id); var first = Nodes(game, id).Where(n => n.Id != "finish").ToArray();
                    c.That(first.All(n => n.State == NodeState.Working && n.Progress.Equals(WorkProgress.Zero)) &&
                        first.Count(n => n.Worker.IsValid) == 1, "optional money cannot alter real mixed executor startup");
                    var business = game.Seat(0).View.Feature<BusinessView>();
                    c.Equal(survival ? new Code("business.no-account") : Code.None, business.ReadAccount(Company, out var account), "game mode explicitly selects or omits account");
                    if (survival) c.That(!business.HasAccounts && !business.HasBudgets, "survival has no fabricated zero account or budget authority");
                    else c.Equal(Fix.FromInt(160), account.Reserved, "whole mixed actual cost occurrences held before productive time");
                    c.Equal(Fix.FromInt(8), Stock(game, target), "monetary admission does not buy or consume work inputs");
                    var twin = factory.Load(game.Save()); Until(game, () => Completed(game, order), "mixed funded or resource-only physical completion", twin);
                    c.Equal(survival ? new Code("business.no-account") : Code.None, business.ReadAccount(Company, out account), "physical work cannot silently enable accounts");
                    if (!survival) c.That(account.Balance == Fix.Zero && account.Reserved == Fix.Zero, "original budget owner spends exact mixed holds once");
                    c.Equal(Fix.FromInt(4), Stock(game, target), "mixed work consumes same physical bills with or without optional money");
                    var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                    c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "mixed optional-budget canonical replay: " + replay.Failure + "/" + replay.Detail);
                }
            }
        }
        private static void Fault(Check c)
        {
            var factory = Factory(); var game = New(factory); int acquired = 0;
            Find(game, "destination").Get<SingleResourcePort>().AfterReserveOut =
                () => { if (++acquired == 2) throw new InvalidOperationException("proof.unified.second-acquisition"); };
            game.Seat(0).Submit(new RaiseIncidentCommand { Target = Visible(game, "destination"), Source = new Code("damage"), Kind = new Code("proof.failure") });
            game.Update();
            c.That(game.Fault != null && acquired == 2, "real later mixed resource mutation faults ordinary Update");
            c.That(Find(game, "machine").Get<IWorker>().Idle && Board(game).Orders.Single().Node("a.timer").State != NodeState.Working &&
                Board(game).Orders.Single().Nodes.All(n => n.Resources.Count == 0) &&
                Board(game).Jobs.All(j => j.State != JobState.Assigned), "no machine or timer prefix bound before complete provider verification");
            c.Throws<InvalidOperationException>(() => game.Save(), "faulted provider prefix cannot checkpoint");
            c.Throws<InvalidOperationException>(() => game.Hash(), "faulted provider prefix cannot report a settled hash");
        }
        private static void Hauling(Check c)
        {
            var factory = Colony.Factory(); var game = Colony.New(286);
            Command(c, game, new ConstructCommand { Def = "tower", Priority = 50, Placement = TilePlacement.Arguments("colony.tiles", new TileCoord(10, 16)) });
            Until(game, () => Jobs(game).Any(j => j.IsHaul && j.Loaded > Fix.Zero), "physical construction loaded hauling");
            var twin = factory.Load(game.Save());
            Until(game, () => game.Seat(0).Snapshot.Entities.Any(e => e.Def == "tower"), "physical delivery and machine construction", twin);
            Fix steel = Fix.Zero; foreach (var entity in game.Seat(0).Snapshot.Entities.Where(e => e.Owner == Company)) steel += Stock(game, entity.Id, "steel");
            c.Equal(Fix.FromInt(20), steel, "native hauling preserves fifty minus thirty construction materials");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "unified native admission identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no duplicate state codec or compatibility reader");
            Protocol(c); Priorities(c); Response(c); Budgets(c); Fault(c); Hauling(c);
        }
    }
}
