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
    internal static class ProductiveFrontierTests
    {
        internal const string Case = "Productive frontier: complete native quanta, paused continuation and saved physical work";
        private static readonly PlayerId Company = new PlayerId(0);
        private static readonly ReplayLimits Limits = new ReplayLimits(16 * 1024 * 1024, 12000);
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(int stock = 8) => IncidentPreparednessTests.Fixture(
            documentName: "productive-frontier.json",
            edit: root => root.Get("entities")[1].Get("components").Get("point_stock").Set("initial", Content.JsonValue.Number(stock.ToString())));
        private static SimGame New(GameFactory factory, string scenario = "points")
        {
            var request = new NewGameRequest { Scenario = scenario, Seed = 287 };
            if (scenario == "points") request.Mods.Add("proof.shipments");
            request.Seats.Add(Controller.Human); if (scenario == "points") request.Seats.Add(Controller.Human);
            return factory.NewGame(request);
        }
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static Entity Find(SimGame game, string prototype) => game.World.Entities.All.Single(e => e.Alive && e.Def.Id == prototype);
        private static EntityId Visible(SimGame game, string prototype) => game.Seat(0).Snapshot.Entities.Single(e => e.Owner == Company && e.Def == prototype).Id;
        private static WorkView Work(SimGame game) => game.Seat(0).View.Feature<WorkView>();
        private static List<ActivityNodeView> Nodes(SimGame game, int id)
        { var rows = new List<ActivityNodeView>(); if (!Work(game).TryCopyNodes(id, rows)) throw new InvalidOperationException("missing productive nodes"); return rows; }
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
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("productive fault at " + game.Tick + "/" + game.Fault);
            if (twin == null) return;
            twin.Update(); if (twin.Fault != null || twin.Hash() != game.Hash()) throw new InvalidOperationException("productive saved divergence at " + game.Tick + "/" + twin.Fault);
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
            c.Equal(before, game.Hash(), "productive preview reserves and advances nothing");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "player intent accepted: " + result.Refusal);
            if (!result.Accepted) throw new InvalidOperationException("productive player refused " + result.Refusal); return result;
        }
        private static OrderView Post(Check c, SimGame game)
        {
            int id = checked((int)Command(c, game, new PostOrderCommand { Plan = "productive.parallel", Target = Visible(game, "destination"), Priority = 30 }).Arg0);
            return ReadOrder(game, id);
        }
        private static void Protocol(Check c)
        {
            // Engine-side complete-set protocol, using real player-created nodes and real stock books.
            var factory = Factory(); var game = New(factory); var order = Post(c, game); var board = Board(game);
            var pair = new[] { (order.Id, "a"), (order.Id, "b") };
            var port = Find(game, "destination").Get<IOutletPort>(); var supply = game.World.Content.ResourceId("supply");
            c.That(Nodes(game, order.Id).Take(2).All(n => n.State == NodeState.Working && n.Progress.Equals(WorkProgress.Zero)), "actual productive frontier starts without same-Update work");
            c.Equal(Fix.FromInt(2), port.ReservedOut(supply), "original startup holds exist before productive admission");
            Action<IReadOnlyList<(int Order, string Node)>, Code, string> refuse = (nodes, expected, reason) =>
            {
                ulong hash = game.Hash(); c.That(!board.AdvanceWork(nodes, Company, out Code blocker), reason + " complete set refuses");
                c.Equal(expected, blocker, reason + " original typed blocker"); c.Equal(hash, game.Hash(), reason + " zero mutation of actual state/books/progress");
            };
            refuse(new[] { (order.Id, "a"), (order.Id, "a") }, Codes.InvalidArgument, "duplicate actual node");
            refuse(new[] { (order.Id, "a"), (int.MaxValue, "b") }, Codes.InvalidArgument, "missing later order");
            refuse(new[] { (order.Id, "a"), (order.Id, "finish") }, Codes.InvalidArgument, "unstarted dependent node");
            ulong hash = game.Hash(); c.That(!board.AdvanceWork(pair, new PlayerId(1), out _), "foreign productive set refuses before providers");
            c.Equal(hash, game.Hash(), "foreign refusal discloses no mutable prefix");
            var owner = new ReservationOwner("proof.productive-competitor", 1); var competitor = port.ReserveOut(owner, supply, Fix.FromInt(5));
            refuse(pair, WorkResourceCodes.Stock, "individually fitting running deltas exceed aggregate free stock");
            c.That(port.Reservations.Count == 3 && port.Reservations.Any(r => r.Token.Equals(competitor)), "aggregate refusal retains competitor and both actual startup receipts");
            port.ReleaseOut(owner, competitor);
            var twin = factory.Load(game.Save());
            c.That(board.AdvanceWork(pair, Company, out var blocker) && blocker.IsNone, "complete running inputs and startup receipts commit together");
            c.That(Board(twin).AdvanceWork(pair.Reverse().ToArray(), Company, out blocker) && blocker.IsNone, "reversed references use same canonical productive owner");
            c.Equal(game.Hash(), twin.Hash(), "reversed complete quanta retain identical native tokens/progress/history state");
            var nodes = Nodes(game, order.Id).Where(n => n.Id != "finish").ToArray();
            c.That(nodes.All(n => n.Progress.WholeTicks == 1 && n.ProductiveSteps == 1 && n.Resources.Any(r => r.Spent)), "both nodes bind exactly one productive step after complete verified spending");
            c.Equal(Fix.FromInt(4), Stock(game, Visible(game, "destination")), "two startup units and two running units consumed once");
            c.Equal(Fix.Zero, port.ReservedOut(supply), "running quanta leave no retained running promises");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "existing productive bindings/codecs save complete quantum");
            game.Seat(0).Submit(new CancelOrderCommand { Order = order.Id }); Step(game);
            c.Equal(Fix.FromInt(4), Stock(game, Visible(game, "destination")), "cancel spent productive work creates no refund");
        }
        private static void Physical(Check c)
        {
            var factory = Factory(4); var game = New(factory); var isolated = New(factory); ulong untouched = isolated.Hash();
            // Explicit player fleet/reserve settings, never harness scheduling decisions.
            foreach (string actor in new[] { "machine", "prepared.worker.1" })
                Command(c, game, new ConfigureWorkPolicyCommand { Worker = Visible(game, actor),
                    Settings = new WorkPolicySettings(true, 0, actor == "machine" ? new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") } :
                        new[] { new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) });
            Command(c, game, new ConfigureCompanyAlertCommand { Company = Company,
                Settings = new CompanyAlertSettings("alerts.neutral", CompanyAlertLevel.Normal, Array.Empty<WorkerReserveSetting>()) });
            using (var recorder = game.RecordReplay(Limits))
            {
                var order = Post(c, game); Step(game);
                c.That(Nodes(game, order.Id).Take(2).All(n => n.Progress.WholeTicks == 1 && n.ProductiveSteps == 1), "public loop advances complete feasible timed quantum once");
                c.Equal(Fix.Zero, Stock(game, Visible(game, "destination")), "first full quantum spends physical startup and running inputs");
                Step(game); var paused = Nodes(game, order.Id).Take(2).ToArray();
                c.That(paused.All(n => n.State == NodeState.Paused && n.Progress.WholeTicks == 1 && n.Blocker == WorkResourceCodes.Stock), "actual empty running supply pauses both original nodes without progress reset");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "both paused productive nodes save only original state");
                ulong before = game.Hash(); long step = game.Tick; game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
                c.That(game.Hash() == before && game.Tick == step, "one clock pause freezes complete paused frontier");
                game.Clock.Resume(); twin.Clock.Resume();
                if (!game.Seat(0).View.TryFindResource("supply", out var resource)) throw new InvalidOperationException("missing visible authored supply");
                var stock = new ConfigureStockPolicyCommand { Store = Visible(game, "destination"), Resource = resource,
                    Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.One, Fix.FromInt(4), Fix.FromInt(40), 80, LogisticsReserveAccess.Protected)) };
                Command(c, game, stock);
                // Saved twin resumes from the exact player command settled by the engine.
                twin = factory.Load(game.Save());
                Until(game, () => Jobs(game).Any(j => j.IsHaul && j.Loaded > Fix.Zero), "real physical replenishment pickup", twin);
                c.That(Nodes(game, order.Id).Take(2).All(n => n.Progress.WholeTicks == 1), "loaded materials cannot fund work before real delivery");
                twin = factory.Load(game.Save());
                Until(game, () => Nodes(game, order.Id).Where(n => n.Id != "finish").All(n => n.Progress.WholeTicks >= 2), "delivered supply resumes productive frontier", twin);
                c.That(Nodes(game, order.Id).Where(n => n.Id != "finish").All(n => n.ProductiveSteps == n.Progress.WholeTicks), "waited game time never becomes productive input or progress");
                twin = factory.Load(game.Save()); Until(game, () => Completed(game, order), "physical resumed work and dependent completion", twin);
                Fix total = Fix.Zero; foreach (var entity in game.Seat(0).Snapshot.Entities.Where(e => e.Owner == Company)) total += Stock(game, entity.Id);
                c.Equal(Fix.FromInt(16), total, "twenty source plus four target minus eight actual consumption conserved through hauling");
                c.That(!game.World.Services.Has<IEconomy>() && !game.World.Services.Has<IMonetaryBudgets>(), "resource-only productive work invents no business owner");
                var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "paused supplied productive frontier canonical replay: " + replay.Failure + "/" + replay.Detail);
            }
            c.Equal(untouched, isolated.Hash(), "interleaved independent productive session unchanged");
        }
        private static void Scarcity(Check c)
        {
            var factory = Factory(5); var game = New(factory); var order = Post(c, game); Step(game);
            c.Equal(Fix.One, Stock(game, Visible(game, "destination")), "first feasible full productive set conserves one remaining unit");
            Step(game); var nodes = Nodes(game, order.Id).Where(n => n.Id != "finish").ToArray();
            c.That(nodes.Single(n => n.Id == "a").Progress.WholeTicks == 2 && nodes.Single(n => n.Id == "b").Progress.WholeTicks == 1,
                "nonfeasible full quantum retains canonical sequential physical progress instead of deadlock");
            c.Equal(Fix.Zero, Stock(game, Visible(game, "destination")), "scarce next quantum spends only one real unit");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "unequal real partial progress saves without duplicate frontier state");
            Command(c, game, new CancelOrderCommand { Order = order.Id });
            c.Equal(Fix.Zero, Stock(game, Visible(game, "destination")), "cancel scarce spent inputs cannot fabricate stock");
        }
        private static void Budgets(Check c)
        {
            foreach (bool survival in new[] { false, true })
            {
                var factory = WorkBudgetTests.Factory(160, survival: survival,
                    physicalDocuments: new[] { "workerless-frontier-costs.json", "productive-frontier-costs.json" });
                var game = New(factory, "purchase-haul"); var target = Visible(game, "budget.site-a");
                using (var recorder = game.RecordReplay(Limits))
                {
                    int id = checked((int)Command(c, game, new PostOrderCommand { Plan = "productive.funded", Target = target, Priority = 30 }).Arg0);
                    var order = ReadOrder(game, id); var business = game.Seat(0).View.Feature<BusinessView>();
                    c.Equal(survival ? new Code("business.no-account") : Code.None, business.ReadAccount(Company, out var account), "authored mode selects actual optional account or none");
                    if (survival) c.That(!business.HasAccounts && !business.HasBudgets, "survival has no synthetic balance or budget owner");
                    else c.Equal(Fix.FromInt(160), account.Reserved, "actual whole authored costs held before productive commitment");
                    var twin = factory.Load(game.Save()); Step(game, twin);
                    c.That(Nodes(game, id).Take(2).All(n => n.Progress.WholeTicks == 1 && n.ProductiveSteps == 1), "whole productive budget frontier earns one physical step");
                    c.Equal(Fix.FromInt(2), Stock(game, target), "four startup and two exact rounded elapsed units consumed together");
                    c.Equal(survival ? new Code("business.no-account") : Code.None, business.ReadAccount(Company, out account), "productive work retains mode-selected monetary ownership");
                    if (!survival) c.That(account.Balance == Fix.Zero && account.Reserved == Fix.Zero, "whole original budget obligations spent once before progress binds");
                    twin = factory.Load(game.Save()); Until(game, () => Completed(game, order), "funded or resource-only exact elapsed completion", twin);
                    c.Equal(Fix.FromInt(2), Stock(game, target), "zero later rounded deltas never consume fabricated full units");
                    var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                    c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "productive optional-budget replay: " + replay.Failure + "/" + replay.Detail);
                }
            }
        }
        private static void Fault(Check c)
        {
            var factory = Factory(); var game = New(factory); var order = Post(c, game); int acquired = 0;
            Find(game, "destination").Get<SingleResourcePort>().AfterReserveOut =
                () => { if (++acquired == 2) throw new InvalidOperationException("proof.productive.second-running-acquisition"); };
            game.Update();
            c.That(game.Fault != null && acquired == 2, "later real running acquisition faults normal Update");
            c.That(Board(game).Order(order.Id).Nodes.Take(2).All(n => n.Progress.Equals(WorkProgress.Zero) && n.ProductiveSteps == 0 &&
                n.Resources.All(r => !r.Spent)), "no earlier node gains productive progress or spent flags after partial provider failure");
            c.Equal(Fix.FromInt(8), Find(game, "destination").Get<IInletPort>().Amount(game.World.Content.ResourceId("supply")), "acquisition fault consumes no startup stock");
            c.Throws<InvalidOperationException>(() => game.Save(), "faulted productive prefix cannot checkpoint");
            c.Throws<InvalidOperationException>(() => game.Hash(), "faulted productive prefix cannot report settled hash");
        }
        private static void Native(Check c)
        {
            var factory = Factory(); var game = New(factory);
            using (var recorder = game.RecordReplay(Limits))
            {
                var target = Visible(game, "destination");
                Command(c, game, new RaiseIncidentCommand { Target = target, Source = new Code("damage"), Kind = new Code("proof.failure") });
                if (!game.Seat(0).View.Feature<IncidentsView>().TryGetResponseOrder(target, new Code("damage"), out int id)) throw new InvalidOperationException("missing response");
                var order = ReadOrder(game, id); var twin = factory.Load(game.Save());
                Until(game, () => Nodes(game, id).Single(n => n.Id == "b.inspect").Progress.WholeTicks > 0, "actual travelling inspector becomes productive", twin);
                c.That(Nodes(game, id).Single(n => n.Id == "b.inspect").Worker.IsValid, "same productive owner requires actual physically working machine");
                twin = factory.Load(game.Save()); Until(game, () => Completed(game, order), "native work departure and dependent restart", twin);
                c.Equal(Fix.Zero, Stock(game, target), "machine and timed native productive startup consume original eight units once");
                var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "native productive owner replay: " + replay.Failure + "/" + replay.Detail);
            }
            var colony = Colony.New(287); var colonyFactory = Colony.Factory();
            Command(c, colony, new ConstructCommand { Def = "tower", Priority = 50, Placement = TilePlacement.Arguments("colony.tiles", new TileCoord(10, 16)) });
            Until(colony, () => Jobs(colony).Any(j => j.IsHaul && j.Loaded > Fix.Zero), "real staged construction pickup");
            var saved = colonyFactory.Load(colony.Save());
            Until(colony, () => colony.Seat(0).Snapshot.Entities.Any(e => e.Def == "tower"), "actual delivered machine construction", saved);
            Fix steel = Fix.Zero; foreach (var entity in colony.Seat(0).Snapshot.Entities.Where(e => e.Owner == Company)) steel += Stock(colony, entity.Id, "steel");
            c.Equal(Fix.FromInt(20), steel, "machine productive migration conserves fifty minus thirty steel");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "productive frontier identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "original persistent progress and receipt layout");
            Protocol(c); Physical(c); Scarcity(c); Budgets(c); Fault(c); Native(c);
        }
    }
}
