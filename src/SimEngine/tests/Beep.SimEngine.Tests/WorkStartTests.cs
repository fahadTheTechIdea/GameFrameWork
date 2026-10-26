using System;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    // Additional directly affected checks in the same two exact resource suites, not a broad family run.
    internal static class WorkStartTests
    {
        private static string Json => FrameworkProofContent.Read("work-start.json");
        private static readonly PlayerId Company = new PlayerId(0);
        private static GameFactory StartFactory(string definitions = null) => Colony.Factory(
            FrameworkProofContent.Read("resource-recovery.json"), FrameworkProofContent.Read("endpoints-flows.json"),
            additionalDefinitions: FrameworkProofContent.Read("resource-requirements.json"),
            additionalDocuments: new Dictionary<string, string> { ["work-start.json"] = definitions ?? Json });
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static ResourceId Steel(SimGame game) => game.World.Content.ResourceId("steel");
        private static IEconomy Economy(SimGame game) => game.World.Services.Get<IEconomy>();
        private static void Cash(SimGame game, int remaining)
        { var economy = Economy(game); economy.Charge(Company, economy.Cash(Company) - Fix.FromInt(remaining), new Code("proof.setup")); }
        private static Order Post(Check c, SimGame game, Entity target, string activity)
        {
            var result = TestKit.Order(game, 0, new PostOrderCommand { Target = target.Id, Plan = activity });
            c.That(result.Accepted, "player posts the authored ordinary engine activity"); return Board(game).Order(checked((int)result.Arg0));
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int tick = 0; tick < 500 && !done(); tick++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done())
            {
                var snapshots = new List<string>(); foreach (var o in Board(game).Orders) foreach (var n in o.Nodes)
                    snapshots.Add(o.Id + "/" + n.Id + ":" + n.State + ",job=" + n.JobId + ",work=" + n.Progress.WholeTicks + "," + n.Blocker);
                throw new InvalidOperationException(stage + " timeout: " + string.Join(";", snapshots));
            }
        }
        internal static void Admission(Check c)
        {
            var factory = StartFactory(); var game = New(factory);
            var target = Spawn(game, "requirements.target", 20, 20); var worker = Spawn(game, "requirements.builder", 6, 20);
            Cash(game, 10); Tick(game); ulong before = game.Hash(); var offers = new List<WorkWorkerCandidate>();
            c.Equal(Code.None, game.Seat(0).View.Feature<WorkView>().PreviewWorkerCandidates("start.expensive-work", "work", target.Id, offers), "cost offers share the existing engine evaluator");
            c.Equal(WorkSystem.BlockerCash, offers[0].Result.Blocker, "two individually affordable costs refuse as one aggregate twelve-unit obligation");
            c.Equal(2, offers[0].Result.Costs.Count, "native selected monetary obligations remain separate facts");
            c.Equal(before, game.Hash(), "money/material preview is pure");
            c.Equal(0, worker.Get<IWorker>().Job, "aggregate cost shortage acquires no machine");
            var order = Post(c, game, target, "start.expensive-work"); Tick(game);
            c.Equal(0, order.Node("work").Resources.Count, "money shortage creates no endpoint promise links");
            c.Equal(Fix.FromInt(10), target.Get<IInventory>().Available(Steel(game)), "money refusal neither promises nor consumes materials");
            c.Equal(Fix.FromInt(10), Economy(game).Cash(Company), "aggregate refusal debits nothing");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "unassigned monetary blocker saves identically");
            foreach (var invalid in new[] {
                Json.Replace("\"requirement\":\"start.ready\"", "\"requirement\":\"start.ready\",\"needs\":{\"steel\":1}"),
                Json.Replace("\"requirement\":\"start.ready\"", "\"requirement\":\"start.ready\",\"cash\":1"),
                Json.Replace("\"requirement\":\"start.ready\"", "\"requirement\":\"start.ready\",\"on_credit\":true"),
                Json.Replace("\"requirement_multiplier\":2", "\"requirement_multiplier\":0"),
                Json.Replace("\"requirement_multiplier\":2", "\"requirement_multiplier\":0.25"),
                Json.Replace("\"amount\":5", "\"amount\":0"),
                Json.Replace("\"requirement\":\"start.line-ready\"", "\"requirement\":\"start.line-ready\",\"bill_per_tile\":{\"steel\":1}"),
                Json.Replace("\"requirement\":\"start.line-ready\"", "\"requirement\":\"start.line-ready\",\"cash_per_tile\":1"),
                Json.Replace(",\"credit\":false", "")
            }) c.Throws<ContentException>(() => New(StartFactory(invalid)), "old node cost/input fields or illegal scaled/native cost refuses, never translates");
            c.That(typeof(IActivityNodeDefinition).GetProperty("Needs") == null && typeof(IActivityNodeDefinition).GetProperty("Cash") == null,
                "activity contract has one requirement path, not the superseded inline input/payment path");
            c.That(!Enum.IsDefined(typeof(NodeState), "Supplying"), "no saved early-consumption phase remains");
            c.Throws<NotSupportedException>(() => ((IList<WorkCostChoice>)offers[0].Result.Costs).Clear(), "private cost facts are immutable");
            var local = new LocalWorkProofModule(); var localGame = OptionalWorkTests.New(OptionalWorkTests.Factory(local));
            c.That(!localGame.World.Services.Has<IEconomy>() && localGame.World.Space == null, "native unpriced requirements still admit without money or a map");
            string json = FrameworkProofContent.Read("local-work.json");
            string priced = json.Replace("\"requirement\": \"proof.local-work.settle.settling.inputs\"", "\"requirement\": \"local.paid\"")
                .Replace("\"activities\":", "\"work_cost_requirements\":[{\"id\":\"local.paid\",\"amount\":1,\"credit\":false, \"category\":\"construction\"}],\"activities\":");
            var absent = new LocalWorkProofModule();
            c.Throws<ContentException>(() => OptionalWorkTests.New(OptionalWorkTests.Factory(absent, priced)), "selected authored payment requires the actual economy, not FreeEconomy fallback");
            c.Equal(0, absent.Initializations, "missing monetary capability fails before first Initialize");
        }
        internal static void Physical(Check c)
        {
            var factory = StartFactory(); var game = New(factory);
            var target = Spawn(game, "requirements.target", 20, 20); var worker = Spawn(game, "requirements.builder", 6, 20);
            var order = Post(c, game, target, "start.work"); var node = order.Node("work"); Tick(game);
            c.That(worker.Get<IWorker>().Job != 0 && node.Costs.Count == 1 && !node.Costs[0].Spent, "real worker/native claims commit with an unspent monetary obligation");
            c.Equal(Fix.FromInt(10000), Economy(game).Cash(Company), "travel does not prepay work");
            c.Equal(Fix.FromInt(10), target.Get<IInventory>().Amount(Steel(game)), "travel does not consume inputs");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "travelling cost/source commitments restore through the sole work codec");
            var unpaid = node.Costs; node.Costs = new[] { new WorkCostBinding(unpaid[0].Choice, true, unpaid[0].Receipt) };
            c.Throws<CorruptStateException>(() => factory.Load(game.Save()), "unstarted saved work cannot fabricate a paid monetary obligation"); node.Costs = unpaid;
            Cash(game, 0); for (int i = 0; i < 4; i++) Tick(game);
            c.Equal(WorkSystem.BlockerCash, node.Blocker, "funds changed during travel are an explicit current-state blocker, not fabricated reserved cash");
            c.Equal(WorkProgress.Zero, node.Progress, "funds refusal advances no work");
            c.Equal(Fix.FromInt(10), target.Get<IInventory>().Amount(Steel(game)), "funds refusal preserves native inputs");
            Economy(game).Credit(Company, Fix.FromInt(10), new Code("proof.setup"));
            Reach(game, () => node.Progress.WholeTicks > 0, "first physical paid work");
            c.Equal(Fix.FromInt(5), Economy(game).Cash(Company), "physical startup charges once");
            c.Equal(Fix.FromInt(8), target.Get<IInventory>().Amount(Steel(game)), "same startup consumes actual claimed material once");
            c.That(node.Costs[0].Spent && node.Resources[0].Spent, "node facts retain actual payment and resource consumption");
            var views = new List<ActivityNodeView>(); c.That(game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order.Id, views), "private node view exposes actual cost commitment");
            c.That(views[0].Costs[0].Spent, "detached view distinguishes paid obligation from preview");
            c.That(!game.Seat(1).View.Feature<WorkView>().TryCopyNodes(order.Id, views) && views.Count == 0, "private monetary facts never leak to another company");
            var before = node.Costs; node.Costs = new[] { new WorkCostBinding(node.Costs[0].Choice, false, node.Costs[0].Receipt) };
            c.Throws<CorruptStateException>(() => factory.Load(game.Save()), "progressing saved work cannot erase its paid obligation"); node.Costs = before;
            var twin = factory.Load(game.Save()); Reach(game, () => order.State == OrderState.Done, "paid completion", twin);
            c.Equal(game.Hash(), twin.Hash(), "paid saved twins complete identically without repeat spending");
            c.Equal(Fix.FromInt(5), Economy(game).Cash(Company), "every subsequent Update retains one payment");
            Cancel(c, factory, false); Cancel(c, factory, true); ScaledAndCredit(c, factory); Supply(c, factory); Infrastructure(c, factory); Construction(c, factory); Faults(c, factory);
        }
        private static void Cancel(Check c, GameFactory factory, bool started)
        {
            var game = New(factory); var target = Spawn(game, "requirements.target", 20, 20); Spawn(game, "requirements.builder", 6, 20);
            var order = Post(c, game, target, "start.work"); var node = order.Node("work"); Tick(game);
            if (started) Reach(game, () => node.Progress.WholeTicks > 0, "cancel after physical start");
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = order.Id }).Accepted, "normal cancellation remains engine-owned");
            c.Equal(Fix.FromInt(started ? 9995 : 10000), Economy(game).Cash(Company), "cancel never prepays or refunds actual work");
            c.Equal(started ? 1 : 0, node.Costs.Count, "cancel retains spent facts only");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled monetary lifetime restores identically");
        }
        private static void ScaledAndCredit(Check c, GameFactory factory)
        {
            var game = New(factory); var target = Spawn(game, "requirements.target", 20, 20); Spawn(game, "requirements.builder", 6, 20);
            var order = Post(c, game, target, "start.scaled"); var twin = factory.Load(game.Save());
            Reach(game, () => order.State == OrderState.Done, "scaled authored physical work", twin);
            c.Equal(Fix.FromInt(6), target.Get<IInventory>().Amount(Steel(game)), "declared multiplier scales native inputs, not another generated bill");
            c.Equal(Fix.FromInt(9990), Economy(game).Cash(Company), "same multiplier scales actual authored cost");
            c.Equal(game.Hash(), twin.Hash(), "scaled receipt bills/obligations restore identically");
            game = New(factory); target = Spawn(game, "requirements.target", 20, 20); Spawn(game, "requirements.builder", 6, 20); Cash(game, 5);
            order = Post(c, game, target, "start.credit-work"); Reach(game, () => order.State == OrderState.Done, "explicit credit policy");
            c.Equal(Fix.FromInt(-7), Economy(game).Cash(Company), "only authored credit obligation enters debt; ordinary five-unit cost is paid first");
        }
        private static void Supply(Check c, GameFactory factory)
        {
            var game = New(factory); var target = Spawn(game, "start.empty", 20, 20); Spawn(game, "depot", 4, 20); Spawn(game, "truck", 8, 24);
            Spawn(game, "requirements.builder", 6, 16); Spawn(game, "requirements.builder", 10, 16);
            var order = Post(c, game, target, "start.supplied"); var logistics = game.World.Services.Get<LogisticsService>(); Tick(game);
            c.Equal(Fix.FromInt(5), logistics.DemandOf(target.Id, Steel(game)), "same evaluated parallel requirement leaves aggregate one order demand");
            c.Equal(Fix.Zero, target.Get<IInventory>().Amount(Steel(game)), "posting/supply planning does not teleport native resources");
            var twin = factory.Load(game.Save()); Reach(game, () => order.State == OrderState.Done, "physical authored supply", twin);
            c.Equal(game.Hash(), twin.Hash(), "pending supplies/real hauls/parallel work use existing saved owners");
            c.Equal(Fix.Zero, target.Get<IInventory>().Amount(Steel(game)), "delivered five native inputs were consumed by actual reached work only");
            c.Equal(Fix.Zero, logistics.DemandOf(target.Id, Steel(game)), "completed native requirement demand is withdrawn");
        }
        private static void Infrastructure(Check c, GameFactory factory)
        {
            var game = New(factory); Spawn(game, "depot", 4, 20); Spawn(game, "truck", 8, 24); Spawn(game, "crane", 6, 16);
            var path = new[] { new TileCoord(20, 20), new TileCoord(21, 20), new TileCoord(22, 20) };
            var command = new LayLineCommand { Line = "start.road" }; command.Tiles.AddRange(path);
            c.That(TestKit.Order(game, 0, command).Accepted, "ordinary line command binds authored per-tile requirement");
            var order = Board(game).Orders[0]; c.Equal(3, order.Nodes.Count, "path uses three physical stages"); c.Equal(Fix.One, order.Plan.Graph.Nodes[0].RequirementMultiplier, "each stage uses the same authored one-tile requirement");
            var steel = Steel(game); var flows = game.Seat(0).View.Feature<ResourceFlowView>(); long cursor = flows.Read(Company, 0).Latest;
            var twin = factory.Load(game.Save()); Reach(game, () => !order.IsActive, "scaled physical infrastructure", twin);
            c.Equal(OrderState.Done, order.State, "native authored line materials are hauled and laid over time");
            c.Equal(new BigInteger(Fix.FromInt(3).Raw), flows.Read(Company, cursor).Totals(steel).ConsumedRaw, "three actual per-tile inputs consume once through exact receipts");
            c.Equal(Fix.FromInt(9985), Economy(game).Cash(Company), "generated line charges each reached segment once at physical startup");
            c.Equal(game.Hash(), twin.Hash(), "scaled generated activity saves and continues through the same node codec");
        }
        private static void Faults(Check c, GameFactory factory)
        {
            foreach (bool admission in new[] { true, false })
            {
                var game = New(factory); var target = Spawn(game, "requirements.target", 20, 20); Spawn(game, "requirements.builder", 6, 20);
                var order = Post(c, game, target, "start.work"); Tick(game); var ledger = (TestLedger)Economy(game);
                ledger.MutateAdmission = admission; ledger.RefuseCharge = !admission;
                for (int i = 0; i < 500 && game.Fault == null; i++) game.Update();
                c.That(game.Fault != null, "mutating affordability or refused post-preflight charge faults the normal engine Update");
                c.Equal(Fix.FromInt(10), target.Get<IInventory>().Amount(Steel(game)), "fault does not fabricate material consumption before successful payment");
                c.Throws<InvalidOperationException>(() => game.Save(), "faulted payment/provider state has no resumable checkpoint");
            }
        }
        private static void Construction(Check c, GameFactory factory)
        {
            var game = New(factory); Spawn(game, "depot", 4, 20); Spawn(game, "truck", 8, 24); Spawn(game, "crane", 6, 16);
            var placed = TestKit.Order(game, 0, new ConstructCommand { Def = "tower", Placement = Beep.SimEngine.Maps.TilePlacement.Arguments(game.World.Space.Id, new TileCoord(20, 20)), Priority = 50 });
            c.That(placed.Accepted, "construction uses the migrated authored input requirement");
            var site = game.World.Entities.Get(placed.Entity); var order = Board(game).Order(site.Get<Site>().Order);
            var steel = Steel(game); var flows = game.Seat(0).View.Feature<ResourceFlowView>(); var twin = factory.Load(game.Save());
            Reach(game, () => !site.Alive, "native supplied construction", twin);
            c.Equal(new BigInteger(Fix.FromInt(30).Raw), flows.Read(Company, 0).Totals(steel).ConsumedRaw, "construction spends thirty actually delivered inputs through named native receipts");
            var structures = new List<EntityView>(); game.Seat(0).View.CopyEntities(Company, structures);
            var built = structures.Find(e => e.Def == "tower"); c.That(built.Id.IsValid, "actual completed prototype is created by the engine effect");
            c.Equal(game.Hash(), twin.Hash(), "construction supply/travel/startup/completion uses the same saved owners");
            c.That(TestKit.Order(game, 0, new DemolishCommand { Target = built.Id }).Accepted, "ordinary physical demolition uses explicit authored salvage");
            twin = factory.Load(game.Save()); Reach(game, () => game.World.Entities.Get(built.Id) == null, "explicit native salvage", twin);
            BigInteger yielded = BigInteger.Zero;
            foreach (var entry in flows.Read(Company, 0).Entries)
                if (entry.Item.Kind == ResourceFlowKind.Produced && entry.Item.Resource == steel && entry.Item.Cause == new Code("demolish:tower")) yielded += entry.Item.Amount.Raw;
            c.Equal(new BigInteger(Fix.FromInt(15).Raw), yielded, "salvage is a declared native yield, not inferred from an unchosen requirement branch");
            c.Equal(game.Hash(), twin.Hash(), "explicit salvage and downstream hauling restore identically");
        }
    }
}
