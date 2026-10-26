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
    internal static class WorkerlessFrontierTests
    {
        internal const string Case = "Timed frontier acquisition: complete native commitments and physical saved continuation";
        private static readonly PlayerId Company = new PlayerId(0);
        private static readonly ReplayLimits Limits = new ReplayLimits(16 * 1024 * 1024, 12000);
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory() => OptionalWorkTests.Factory(new LocalWorkProofModule(),
            documents: new[] { "workerless-frontier.json" });
        private static SimGame New(GameFactory factory, string scenario = "local-work")
        {
            var request = new NewGameRequest { Scenario = scenario, Seed = 285 };
            request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static WorkView Work(SimGame game) => game.Seat(0).View.Feature<WorkView>();
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static EntityId Target(SimGame game, string prototype = "local")
            => game.Seat(0).Snapshot.Entities.Single(e => e.Owner == Company && e.Def == prototype).Id;
        private static Fix Stock(SimGame game, EntityId target, string resource = "kit")
        {
            var facts = new List<Facet>();
            if (!game.Seat(0).View.Inspect(target, facts)) throw new InvalidOperationException("missing owned stock");
            return facts.Where(f => f.Key == Storage.FacetStock && f.Subject == resource).Aggregate(Fix.Zero, (sum, f) => sum + f.Value);
        }
        private static List<ActivityNodeView> Nodes(SimGame game, int order)
        {
            var nodes = new List<ActivityNodeView>();
            if (!Work(game).TryCopyNodes(order, nodes)) throw new InvalidOperationException("missing live timed order");
            return nodes;
        }
        private static List<WorkJobView> Jobs(SimGame game)
        { var jobs = new List<WorkJobView>(); Work(game).CopyJobs(jobs); return jobs; }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update();
            if (game.Fault != null) throw new InvalidOperationException("timed fault at " + game.Tick + "/" + game.Fault);
            if (twin == null) return;
            twin.Update();
            if (twin.Fault != null || twin.Hash() != game.Hash()) throw new InvalidOperationException("timed saved divergence at " + game.Tick + "/" + twin.Fault);
        }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 2000 && !done(); i++) Step(game, twin);
            if (!done())
            {
                var orders = new List<OrderView>(); Work(game).CopyOrders(orders);
                throw new InvalidOperationException(stage + "/time=" + game.Clock.TotalGameTime + "/orders=" +
                    string.Join(";", orders.Select(o => o.Id + "/" + o.State + "/" + o.Blocker)));
            }
        }
        private static bool Completed(SimGame game, OrderView order)
            => Work(game).TryReadActivityCompletion(order.Plan, order.Target, out var fact) && fact.Order == order.Id;
        private static CommandResult Command(Check c, SimGame game, Command command)
        {
            ulong hash = game.Hash();
            c.Equal(Code.None, game.Seat(0).Preview(command), "engine previews ordinary player input");
            c.Equal(hash, game.Hash(), "preview reserves and advances nothing");
            var result = TestKit.Order(game, 0, command);
            c.That(result.Accepted, "player intent accepted: " + result.Refusal);
            if (!result.Accepted) throw new InvalidOperationException("timed player refused " + result.Refusal);
            return result;
        }
        private static OrderView Post(Check c, SimGame game, string plan, EntityId target)
        {
            Command(c, game, new PostOrderCommand { Plan = plan, Target = target, Priority = 30 });
            var orders = new List<OrderView>(); Work(game).CopyOrders(orders);
            return orders.Single(o => o.Target == target && o.State == OrderState.Active);
        }
        private static void Refuse(Check c, SimGame game, Order order, string[] ids, Code expected, string stage)
        {
            ulong hash = game.Hash();
            c.That(!Board(game).StartWorkerless(order, ids, out var blocker), stage + " complete timed set refuses");
            c.Equal(expected, blocker, stage + " original typed blocker");
            c.Equal(hash, game.Hash(), stage + " no node/stock/book/budget/version prefix");
        }
        private static void Protocol(Check c)
        {
            // Engine-side acquisition instrumentation only. Original Post owns real graph execution;
            // no invented nodes, progress, jobs, stock or claim books.
            var factory = Factory(); var game = New(factory); var target = Target(game);
            var board = Board(game); var plan = game.World.Content.Find<ActivityDef>("timed.frontier");
            Order order = board.Post(plan, Company, target, 30);
            Refuse(c, game, order, new[] { "a", "a" }, Codes.InvalidArgument, "duplicate node");
            Refuse(c, game, order, new[] { "a", "missing" }, Codes.InvalidArgument, "missing later node");
            Refuse(c, game, order, new[] { "a", "finish" }, WorkSystem.BlockerDependencies, "dependent later node");
            var endpoint = game.World.Entities.Get(target).Get<IOutletPort>();
            ResourceId kit = game.World.Content.ResourceId("kit");
            var owner = new ReservationOwner("proof.timed-competitor", 1);
            ReservationToken competitor = endpoint.ReserveOut(owner, kit, Fix.FromInt(3));
            Refuse(c, game, order, new[] { "a", "b" }, WorkResourceCodes.Stock, "individually fitting but aggregate shortage");
            c.That(order.Nodes.All(n => n.State == NodeState.Waiting && n.Resources.Count == 0),
                "ordinary group refusal preserves every original node and binding");
            c.That(endpoint.Reservations.Count == 1 && endpoint.Reservations[0].Token.Equals(competitor),
                "competitor remains untouched without acquisition prefix");
            endpoint.ReleaseOut(owner, competitor);
            var twin = factory.Load(game.Save());
            c.That(board.StartWorkerless(order, new[] { "a", "b" }, out var blocker) && blocker.IsNone, "complete actual timed set commits");
            c.That(Board(twin).StartWorkerless(Board(twin).Order(order.Id), new[] { "b", "a" }, out blocker) && blocker.IsNone,
                "reversed request uses same canonical timed acquisition owner");
            c.Equal(game.Hash(), twin.Hash(), "reversed complete acquisition has identical actual tokens and state");
            c.That(order.Nodes.Take(2).All(n => n.State == NodeState.Working && n.Progress.Equals(WorkProgress.Zero) && n.Resources.Count == 1),
                "both actual nodes bind commitments without productive work");
            c.Equal(Fix.FromInt(8), endpoint.ReservedOut(kit), "eight actual native units reserved together");
            c.Equal(Fix.FromInt(10), endpoint.Amount(kit), "no startup consumption before productive Update");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "existing nodes/books alone save complete timed starts");
            Refuse(c, game, order, new[] { "a", "b" }, Codes.InvalidArgument, "retained already-started nodes");
            Command(c, game, new CancelOrderCommand { Order = order.Id });
            c.Equal(Fix.FromInt(10), Stock(game, target), "cancel unproductive complete start preserves real stock");
            c.Equal(Fix.Zero, endpoint.ReservedOut(kit), "cancel releases every original timed input hold");
        }
        private static void Physical(Check c)
        {
            var factory = Factory(); var game = New(factory); var target = Target(game);
            var other = New(factory); ulong otherHash = other.Hash();
            c.That(!game.World.Services.Has<IEconomy>() && !game.World.Services.Has<IMonetaryBudgets>(), "authored resource-only timed game needs no business module");
            using (var recorder = game.RecordReplay(Limits))
            {
                var order = Post(c, game, "timed.frontier", target);
                var starts = Nodes(game, order.Id).Where(n => n.Id == "a" || n.Id == "b").ToArray();
                c.That(starts.Length == 2 && starts.All(n => n.State == NodeState.Working && n.Progress.Equals(WorkProgress.Zero)),
                    "public Update starts full timed frontier without same-loop advancement");
                c.That(Jobs(game).Count == 0 && starts.All(n => !n.Worker.IsValid), "no fabricated machines/jobs for authored workerless stages");
                c.Equal(Fix.FromInt(10), Stock(game, target), "complete start only reserves actual materials");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "saved both freshly started timed nodes");
                ulong hash = game.Hash(); long time = game.Tick; game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
                c.That(time == game.Tick && hash == game.Hash(), "one paused game clock freezes the complete timed frontier");
                game.Clock.Resume(); twin.Clock.Resume(); Step(game, twin);
                var work = Nodes(game, order.Id).Where(n => n.Id == "a" || n.Id == "b").ToArray();
                c.That(work.All(n => n.Progress.WholeTicks == 1), "both parallel nodes first earn exactly one same authoritative Update");
                c.Equal(Fix.FromInt(2), Stock(game, target), "whole parallel startup inputs consumed exactly once after time advances");
                twin = factory.Load(game.Save());
                Until(game, () => Completed(game, order), "timed parallel/dependent physical completion", twin);
                c.Equal(Fix.FromInt(2), Stock(game, target), "dependent completion never duplicates startup spending");
                var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "timed frontier canonical replay: " + replay.Failure + "/" + replay.Detail);
            }
            c.Equal(otherHash, other.Hash(), "independent interleaved timed session unchanged");
        }
        private static void ScarceTools(Check c)
        {
            var factory = Factory(); var game = New(factory); var target = Target(game);
            using (var recorder = game.RecordReplay(Limits))
            {
                var order = Post(c, game, "timed.tools", target); var nodes = Nodes(game, order.Id);
                c.That(nodes.Single(n => n.Id == "a").State == NodeState.Working &&
                    nodes.Single(n => n.Id == "b").State != NodeState.Working, "scarce six-unit tools allow canonical sequential start, not artificial group deadlock");
                c.Equal(Fix.FromInt(10), Stock(game, target), "tool holds never consume or fabricate stock");
                var twin = factory.Load(game.Save());
                Until(game, () => Nodes(game, order.Id).Single(n => n.Id == "b").State == NodeState.Working, "physical release makes second timed tool stage eligible", twin);
                c.That(Nodes(game, order.Id).Single(n => n.Id == "a").State == NodeState.Done &&
                    Nodes(game, order.Id).Single(n => n.Id == "b").Progress.Equals(WorkProgress.Zero), "first timed stage completes before second starts without extra same-loop work");
                twin = factory.Load(game.Save());
                Until(game, () => Completed(game, order), "sequential held-tool frontier completion", twin);
                c.Equal(Fix.FromInt(10), Stock(game, target), "held tool conservation after both real timed stages");
                var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "scarce timed tool sequential replay: " + replay.Failure + "/" + replay.Detail);
            }
        }
        private static void OptionalBudgets(Check c)
        {
            foreach (bool survival in new[] { false, true })
            {
                var factory = WorkBudgetTests.Factory(160, survival: survival, physicalDocuments: new[] { "workerless-frontier-costs.json" });
                var game = New(factory, "purchase-haul"); var target = Target(game, "budget.site-a");
                using (var recorder = game.RecordReplay(Limits))
                {
                    var order = Post(c, game, "timed.funded", target);
                    c.That(Nodes(game, order.Id).Take(2).All(n => n.Progress.Equals(WorkProgress.Zero)), "optional policies cannot grant timed nodes extra initial work");
                    var business = game.Seat(0).View.Feature<BusinessView>();
                    c.Equal(survival ? new Code("business.no-account") : Code.None, business.ReadAccount(Company, out var account),
                        "optional account is explicitly selected or genuinely absent by mode");
                    if (survival) c.That(!business.HasAccounts && !business.HasBudgets, "resource-only mode invents neither account nor budget owner");
                    else c.Equal(Fix.FromInt(160), account.Reserved, "enabled mode holds complete actual authored cost set");
                    c.Equal(Fix.FromInt(8), Stock(game, target, "supply"), "optional account cannot purchase or consume timed inputs at start");
                    var twin = factory.Load(game.Save());
                    Until(game, () => Completed(game, order), "timed optional funded/disabled-cost completion", twin);
                    c.Equal(survival ? new Code("business.no-account") : Code.None, business.ReadAccount(Company, out account),
                        "disabled mode retains no account after real timed work");
                    if (survival) c.That(!business.HasAccounts && !business.HasBudgets, "physical completion cannot silently enable optional business");
                    else c.That(account.Balance == Fix.Zero && account.Reserved == Fix.Zero,
                        "enabled timed work spends actual authored holds once");
                    c.Equal(Fix.FromInt(4), Stock(game, target, "supply"), "both timed nodes consume exact original physical bill regardless of monetary mode");
                    var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
                    c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "optional timed account replay: " + replay.Failure + "/" + replay.Detail);
                }
            }
        }
        private static void Fault(Check c)
        {
            var factory = Factory(); var game = New(factory); var target = Target(game);
            int acquired = 0;
            game.World.Entities.Get(target).Get<SingleResourcePort>().AfterReserveOut =
                () => { if (++acquired == 2) throw new InvalidOperationException("proof.timed.second-acquisition"); };
            game.Seat(0).Submit(new PostOrderCommand { Plan = "timed.frontier", Target = target });
            game.Update();
            c.That(game.Fault != null && acquired == 2, "real second timed acquisition fault reaches original Update boundary");
            c.That(Board(game).Orders.Single().Nodes.All(n => n.State != NodeState.Working && n.Resources.Count == 0),
                "no timed node prefix bound before complete real resource verification");
            c.Throws<InvalidOperationException>(() => game.Save(), "faulted partial provider cannot checkpoint");
            c.Throws<InvalidOperationException>(() => game.Hash(), "faulted partial provider cannot claim a settled hash");
        }
        private static void Hauling(Check c)
        {
            var factory = Colony.Factory(); var game = Colony.New(285);
            Command(c, game, new ConstructCommand { Def = "tower", Priority = 50,
                Placement = TilePlacement.Arguments("colony.tiles", new TileCoord(10, 16)) });
            Until(game, () => Jobs(game).Any(j => j.IsHaul && j.Loaded > Fix.Zero), "real loaded staged construction delivery");
            var twin = factory.Load(game.Save());
            Until(game, () => game.Seat(0).Snapshot.Entities.Any(e => e.Def == "tower"), "real delivered machine construction", twin);
            Fix steel = Fix.Zero;
            foreach (var entity in game.Seat(0).Snapshot.Entities.Where(e => e.Owner == Company)) steel += Stock(game, entity.Id, "steel");
            c.Equal(Fix.FromInt(20), steel, "unchanged staffed/haul path conserves50 minus30 actual steel");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "complete timed acquisition identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no new state layout or compatibility codec");
            Protocol(c); Physical(c); ScarceTools(c); OptionalBudgets(c); Fault(c); Hauling(c);
        }
    }
}
