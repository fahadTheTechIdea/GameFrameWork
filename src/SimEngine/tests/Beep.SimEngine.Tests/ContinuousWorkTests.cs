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
    // Same two directly affected suites. Setup/instrumentation only; all work/supply decisions stay engine-side.
    internal static class ContinuousWorkTests
    {
        private static readonly PlayerId Company = new PlayerId(0);
        private static string Json => WorkerExecutionTestContent.Author(FrameworkProofContent.Read("continuous-work.json"));
        private static GameFactory Build(string json = null, string elapsedJson = null, string worldJson = null) => Colony.Factory(worldJson ?? FrameworkProofContent.Read("resource-recovery.json"),
            FrameworkProofContent.Read("endpoints-flows.json"), additionalDefinitions: FrameworkProofContent.Read("resource-requirements.json"),
            additionalDocuments: new Dictionary<string, string> { ["work-start.json"] = FrameworkProofContent.Read("work-start.json"), ["continuous-work.json"] = json ?? Json,
                ["elapsed-work.json"] = elapsedJson ?? ElapsedJson });
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static ResourceId Resource(SimGame game, string id) => game.World.Content.ResourceId(id);
        private static Order Post(Check c, SimGame game, Entity target, string plan)
        {
            var result = TestKit.Order(game, 0, new PostOrderCommand { Target = target.Id, Plan = plan });
            c.That(result.Accepted, "public engine command posts the authored continuous activity"); return Board(game).Order(checked((int)result.Arg0));
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 500 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (done()) return;
            var facts = new List<string>(); foreach (var order in Board(game).Orders) foreach (var node in order.Nodes)
                facts.Add(order.Id + "/" + node.Id + ":" + node.State + ",job=" + node.JobId + ",work=" + node.Progress + "," + node.Blocker);
            throw new InvalidOperationException(stage + " timeout: " + string.Join(";", facts));
        }
        private static Fix Amount(Bill bill, ResourceId resource)
        { foreach (var line in bill.Lines) if (line.Resource == resource) return line.Amount; return Fix.Zero; }
        private static ResourceFlowPage History(SimGame game) => game.Seat(0).View.Feature<ResourceFlowView>().Read(Company, 0);
        internal static void Admission(Check c)
        {
            ElapsedAdmission(c);
            var factory = Build(); var game = New(factory); var target = Spawn(game, "continuous.target", 20, 20);
            var worker = Spawn(game, "continuous.slow", 6, 20); Tick(game); ulong before = game.Hash();
            var offers = new List<WorkWorkerCandidate>();
            c.Equal(Code.None, game.Seat(0).View.Feature<WorkView>().PreviewWorkerCandidates("continuous.work", "work", target.Id, offers), "continuous offers use the sole worker/source selector");
            c.Equal(WorkRequirementStatus.Satisfied, offers[0].Result.Status, "a fraction of the full budget is enough for the next actual slow increment");
            var water = Resource(game, "water"); var steel = Resource(game, "steel");
            foreach (var source in offers[0].Result.ResourceCandidates) if (source.Choice.Requirement == "continuous.fluid")
            {
                c.Equal(Fix.FromRatio(1, 4), source.Availability[0].Required, "half-rate work needs one quarter native water, not the full six-unit budget");
                c.Equal(Fix.FromInt(6), Amount(source.Choice.Quantities, water), "authored total budget remains immutable intent");
            }
            c.Equal(before, game.Hash(), "continuous preview acquires/spends nothing");
            c.Equal(0, worker.Get<IWorker>().Job, "pure preview does not claim the actual machine");
            offers.Clear(); game.Seat(0).View.Feature<WorkView>().PreviewWorkerCandidates("continuous.competing-work", "work", target.Id, offers);
            c.Equal(WorkResourceCodes.Stock, offers[0].Result.Blocker, "two individually ready two-unit next deltas refuse jointly against two stock units");
            c.Equal(before, game.Hash(), "whole-set continuous refusal remains pure");
            foreach (string invalid in new[] {
                Json.Replace("\"use\":\"continuous\"", "\"use\":\"per_tick\""),
                Json.Replace("\"quantities\":{\"steel\":2}", "\"quantities\":{\"steel\":0.5}"),
                Json.Replace("\"hours\":0.2", "\"hours\":0"),
                Json.Replace("\"quantities\":{\"water\":6}", "\"quantities\":{}")
            }) c.Throws<ContentException>(() => New(Build(invalid)), "invalid use/native/instant continuous budgets refuse before Update");
            var privateTarget = Spawn(game, "requirements.target", 32, 20);
            var intent = new[] { new ResourceClaimIntent(ReservationDirection.Outbound, new ReservationOwner("proof.probe", 1), privateTarget.Get<IInventory>().Endpoint, Bill.Empty) };
            var transaction = game.World.Services.Get<IResourceTransaction>();
            c.That(transaction.EvaluateClaims(Company, intent, System.Array.Empty<ResourceReservation>()).Ready, "zero next demand still probes actual endpoint ownership without stock");
            c.Equal(ResourceTransferStatus.InvalidRequest, transaction.AcquireClaims(Company, intent, System.Array.Empty<ResourceReservation>()).Status, "a pure zero-demand probe cannot acquire a fake empty promise");
            c.Equal(ResourceTransferStatus.InvalidRequest, transaction.EvaluateClaims(new PlayerId(1), intent, System.Array.Empty<ResourceReservation>()).Status, "empty probes cannot expose a private foreign endpoint");
            var total = new Bill(new[] { new ResourceAmount(steel, Fix.FromInt(2)), new ResourceAmount(water, Fix.FromRaw(7)) });
            var first = WorkBoard.WorkConsumed(total, new WorkProgress(0, 1), long.MaxValue, game.World.Content);
            c.Equal(Fix.One, Amount(first, steel), "even a tiny positive work quantum funds the first discrete input unit");
            c.Equal(Fix.FromRaw(1), Amount(first, water), "tiny continuous work uses one native raw quantum, not zero or a fabricated unit");
            c.Equal(Fix.One, Amount(WorkBoard.WorkConsumed(total, new WorkProgress(long.MaxValue / 2), long.MaxValue, game.World.Content), steel), "wide work duration is never narrowed into Fix");
            c.Equal(Fix.FromInt(2), Amount(WorkBoard.WorkConsumed(total, new WorkProgress(long.MaxValue), long.MaxValue, game.World.Content), steel), "completion settles exactly the authored discrete total");
            c.Equal(Fix.FromRaw(7), Amount(WorkBoard.WorkConsumed(total, new WorkProgress(long.MaxValue), long.MaxValue, game.World.Content), water), "native fractional tail is neither lost nor overcharged");
        }
        internal static void Physical(Check c)
        {
            ElapsedPhysical(c);
            var factory = Build(); var game = New(factory); var target = Spawn(game, "continuous.target", 20, 20);
            var worker = Spawn(game, "continuous.slow", 6, 20); var order = Post(c, game, target, "continuous.work"); var node = order.Node("work");
            var water = Resource(game, "water"); var steel = Resource(game, "steel"); Tick(game);
            c.Equal(Fix.FromInt(2), target.Get<IInventory>().Amount(water), "travelling machines consume no continuous input");
            foreach (var binding in node.Resources) if (binding.Choice.Use == WorkResourceUse.Continuous)
                c.That(!binding.Spent && binding.Receipts.Count == 0, "assigned continuous source retains no long-lived promise or spent flag");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "travelling source-only bindings restore identically");
            Reach(game, () => node.Progress.Fraction > 0, "first fractional physical increment");
            c.Equal(new WorkProgress(0, 2147483648), node.Progress, "authored half-rate machine advances exactly half a work tick");
            c.Equal(Fix.FromRatio(7, 4), target.Get<IInventory>().Amount(water), "first physical increment spends one quarter water");
            c.Equal(Fix.FromInt(7), target.Get<IInventory>().Amount(steel), "startup two plus one continuous native discrete quantum spend only at actual work");
            c.Equal(Fix.FromInt(9995), game.World.Services.Get<IEconomy>().Cash(Company), "startup money pays once alongside first physical work");
            var snapshot = new List<ActivityNodeView>(); var view = game.Seat(0).View.Feature<WorkView>(); ulong hash = game.Hash();
            c.That(view.TryCopyNodes(order.Id, snapshot), "owner reads detached continuous budget facts");
            c.Equal(2, snapshot[0].RunningInputs.Count, "both authored continuous occurrences use the same node view");
            foreach (var budget in snapshot[0].RunningInputs) if (budget.Requirement == "continuous.fluid")
            { c.Equal(Fix.FromRatio(1, 4), Amount(budget.Consumed, water), "consumed budget is derived from actual fractional progress"); c.Equal(Fix.FromRatio(23, 4), Amount(budget.RemainingBudget, water), "remaining budget is exact, not another stock balance"); }
            c.Equal(hash, game.Hash(), "continuous budget projection is pure");
            c.Throws<NotSupportedException>(() => ((IList<WorkResourceUsage>)snapshot[0].RunningInputs).Clear(), "continuous budget snapshots are immutable");
            var privateView = new List<ActivityNodeView>(); c.That(!game.Seat(1).View.Feature<WorkView>().TryCopyNodes(order.Id, privateView) && privateView.Count == 0, "foreign viewer receives no budget/source facts");
            var twin = factory.Load(game.Save()); Reach(game, () => node.State == NodeState.Paused, "actual fluid exhaustion", twin);
            c.Equal(game.Hash(), twin.Hash(), "fractional charges continue identically after save/load");
            c.Equal(WorkResourceCodes.Stock, node.Blocker, "exhaustion is a typed stock shortage, not lost receipt or fake completion");
            c.Equal(new WorkProgress(4), node.Progress, "only physically supplied eight half increments advance");
            c.Equal(Fix.Zero, target.Get<IInventory>().Amount(water), "exhausted real fluid stock is not substituted");
            WorkProgress paused = node.Progress; for (int i = 0; i < 4; i++) { Tick(game); Tick(twin); }
            c.Equal(paused, node.Progress, "shortage waits advance and consume nothing");
            c.Equal("a.chosen", node.Choices[0].Entry, "shortage cannot silently change the committed recipe to a cheaper branch");
            c.Equal(Fix.FromInt(9995), game.World.Services.Get<IEconomy>().Cash(Company), "paused continuous steps never repeat startup costs");
            Spawn(game, "depot", 4, 20); Spawn(game, "truck", 8, 24);
            c.That(TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = target.Id, Resource = water, Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(4), Fix.FromInt(4), Fix.FromInt(4), 45, LogisticsReserveAccess.Protected)) }).Accepted, "player replenishment uses the existing stock policy and physical hauls");
            twin = factory.Load(game.Save()); Reach(game, () => order.State == OrderState.Done, "physical replenishment and resumed work", twin);
            c.Equal(game.Hash(), twin.Hash(), "paused/native supply/fractional work continues identically");
            c.Equal(new BigInteger(Fix.FromInt(6).Raw), History(game).Totals(water).ConsumedRaw, "continuous fluid sum is exactly six, never per-step rounded overcharge");
            c.Equal(new BigInteger(Fix.FromInt(4).Raw), History(game).Totals(steel).ConsumedRaw, "startup two plus continuous two consume exact authored totals");
            c.Equal(Fix.FromInt(9995), game.World.Services.Get<IEconomy>().Cash(Company), "all resumed work uses the original once-only payment");
            c.That(new List<ResourceReservation>(target.Get<IInventory>().Reservations).TrueForAll(r => !r.Owner.Scope.StartsWith("sim.work.resources.", StringComparison.Ordinal)),
                "completed continuous work leaves no phantom receipt or clears a separate live player-supply claim");
            c.Equal(Fix.FromRatio(1, 4), Amount(snapshot[0].RunningInputs[0].Requirement == "continuous.fluid" ? snapshot[0].RunningInputs[0].Consumed : snapshot[0].RunningInputs[1].Consumed, water), "retained earlier budgets do not mutate during continuation");
            SavedIntegrity(c, factory); Automatic(c, factory); TimedAndWorker(c, factory); ZeroRate(c, factory); Connected(c, factory); ProviderFault(c, factory); Parallel(c, factory); Local(c); ScalingAndOvershoot(c, factory);
        }
        private static void SavedIntegrity(Check c, GameFactory factory)
        {
            var game = New(factory); var target = Spawn(game, "continuous.target", 20, 20); Spawn(game, "continuous.slow", 6, 20);
            var order = Post(c, game, target, "continuous.work"); var node = order.Node("work"); Reach(game, () => node.Progress.Fraction > 0, "corruption setup");
            var original = node.Resources; var changed = new List<WorkResourceBinding>(original); int index = changed.FindIndex(b => b.Choice.Use == WorkResourceUse.Continuous);
            var actual = changed[index]; changed[index] = new WorkResourceBinding(actual.Choice, actual.Receipts, true); node.Resources = changed.AsReadOnly();
            c.Throws<CorruptStateException>(() => factory.Load(game.Save()), "continuous source cannot restore fabricated one-time spent status");
            changed[index] = new WorkResourceBinding(actual.Choice, new[] { original[original.Count - 1].Receipts[0] }, false); node.Resources = changed.AsReadOnly();
            c.Throws<CorruptStateException>(() => factory.Load(game.Save()), "continuous source cannot restore a fabricated persistent receipt");
            node.Resources = original; c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "valid continuous source/progress restores after corruption probes");
            var before = History(game).Totals(Resource(game, "water")).ConsumedRaw;
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = order.Id }).Accepted, "public cancellation ends physical continuous work");
            for (int i = 0; i < 3; i++) Tick(game);
            c.Equal(before, History(game).Totals(Resource(game, "water")).ConsumedRaw, "cancellation refunds/spends no consumed continuous input");
            c.Equal(0, target.Get<IInventory>().Reservations.Count, "cancel releases actual startup/tool claims, not another owner's promises");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled fractional progress/committed branch has one durable codec");
        }
        private static void Automatic(Check c, GameFactory factory)
        {
            var game = New(factory); var target = Spawn(game, "continuous.target", 20, 20); Spawn(game, "continuous.slow", 6, 20);
            var order = Post(c, game, target, "continuous.supplied"); var node = order.Node("work");
            Reach(game, () => node.State == NodeState.Paused, "automatic shortage before transport");
            var water = Resource(game, "water"); var logistics = game.World.Services.Get<LogisticsService>();
            c.Equal(Fix.FromRatio(1, 4), logistics.DemandOf(target.Id, water), "assigned paused node requests the actual next quarter, not its full budget or zero");
            Spawn(game, "depot", 14, 20); Spawn(game, "truck", 16, 24); Tick(game); var twin = factory.Load(game.Save());
            Reach(game, () => order.State == OrderState.Done, "automatic just-in-time physical deliveries", twin);
            c.Equal(game.Hash(), twin.Hash(), "automatic next-increment supply is saved through the existing demand/haul owners");
            c.Equal(new BigInteger(Fix.FromInt(6).Raw), History(game).Totals(water).ConsumedRaw, "automatic supplied continuous work conserves exact authored total");
            c.Equal(Fix.Zero, logistics.DemandOf(target.Id, water), "completed next-increment demand is withdrawn");
        }
        private static void TimedAndWorker(Check c, GameFactory factory)
        {
            var game = New(factory); var target = Spawn(game, "requirements.target", 20, 20); var order = Post(c, game, target, "continuous.timed");
            Reach(game, () => order.Node("work").State == NodeState.Paused, "timed continuous stock exhaustion");
            c.Equal(new WorkProgress(10), order.Node("work").Progress, "timed nodes share the same native consumption/advance protocol");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "paused timed continuous inputs save identically without a machine");
            game = New(factory); target = Spawn(game, "continuous.target", 20, 20); var worker = Spawn(game, "continuous.slow", 6, 20);
            order = Post(c, game, target, "continuous.worker-work"); var twin = factory.Load(game.Save());
            Reach(game, () => order.State == OrderState.Done, "worker-local discrete consumption", twin);
            c.Equal(Fix.Zero, worker.Get<IInventory>().Amount(Resource(game, "wrench")), "the first funded discrete quantum permits later zero-demand increments with empty stock");
            c.Equal(new BigInteger(Fix.One.Raw), History(game).Totals(Resource(game, "wrench")).ConsumedRaw, "a discrete worker input is consumed once across twenty-four half increments");
            c.Equal(game.Hash(), twin.Hash(), "worker-local zero-demand probes preserve saved continuation");
        }
        private static void ZeroRate(Check c, GameFactory factory)
        {
            var game = New(factory); var target = Spawn(game, "continuous.target", 20, 20); Spawn(game, "continuous.slow", 6, 20);
            var zeroWorker = game.World.Entities.Get(Colony.Mine(game, 0, "continuous.slow")[0].Id);
            WorkerExecutionTestContent.Control(game, zeroWorker.Id, true, Fix.Zero);
            var order = Post(c, game, target, "continuous.work"); for (int i = 0; i < 12; i++) Tick(game);
            c.Equal(WorkProgress.Zero, order.Node("work").Progress, "zero rate advances no work");
            c.Equal(Fix.FromInt(2), target.Get<IInventory>().Amount(Resource(game, "water")), "zero rate spends no continuous fluid");
            c.Equal(Fix.FromInt(10), target.Get<IInventory>().Amount(Resource(game, "steel")), "zero rate spends no discrete or startup input");
            c.Equal(Fix.FromInt(10000), game.World.Services.Get<IEconomy>().Cash(Company), "zero rate does not pay a work-start obligation");
            WorkerExecutionTestContent.Control(game, zeroWorker.Id, true, Fix.One); c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "zero-work source bindings save without fabricated progress");
        }
        private static void Connected(Check c, GameFactory factory)
        {
            var game = New(factory); var target = Spawn(game, "requirements.target", 20, 20); var worker = Spawn(game, "continuous.slow", 6, 20);
            var water = Resource(game, "water"); var pools = game.World.Services.Get<EndpointPools>();
            var source = target.Get<IInventory>(); var transaction = game.World.Services.Get<IResourceTransaction>();
            foreach (long identity in new[] { 1L, 5000000000L })
            {
                var pool = pools.Get(identity); var owner = new ReservationOwner("proof.seed.continuous", identity);
                var outgoing = source.ReserveOut(owner, water, Fix.One); var incoming = pool.ReserveIn(owner, water, Fix.One);
                c.That(transaction.Commit(new ResourceTransferIntent(owner, Company, outgoing, incoming, water, Fix.One)).Committed, "setup moves actual native stock into physically connected provider endpoints");
            }
            var order = Post(c, game, target, "continuous.connected-work"); Reach(game, () => order.Node("work").State == NodeState.Paused, "connected source exhaustion");
            var node = order.Node("work"); c.Equal(WorkResourceCodes.Stock, node.Blocker, "connected provider shortage is typed, not teleported to another source");
            c.Equal(Fix.Zero, pools.Get(1).Amount(water), "selected connected provider supplies actual work-time consumption");
            c.Equal(Fix.One, pools.Get(5000000000L).Amount(water), "retained source commitment does not silently switch to available unselected stock");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "scoped continuous source/progress and actual native history save identically");
            pools.ClaimFault = ClaimFaultMode.ShortTake; pools.FaultEndpoint = 5000000000L;
            // This node remains on source 1; unselected provider faults cannot grant stock or affect it.
            WorkProgress before = node.Progress; Tick(game); c.Equal(before, node.Progress, "unselected faulty provider cannot bypass a selected-source shortage");
            pools.ClaimFault = ClaimFaultMode.None;
            Spawn(game, "continuous.slow", 6, 24);
            c.That(TestKit.Order(game, 0, new MoveVehicleCommand { Vehicle = worker.Id, To = new TileCoord(8, 8) }).Accepted,
                "player interruption releases the existing job/source relation through engine commands");
            var twin = factory.Load(game.Save()); Reach(game, () => node.Progress.WholeTicks == 8 && node.State == NodeState.Paused, "alternative source exhaustion after interruption", twin);
            c.Equal(game.Hash(), twin.Hash(), "interrupted continuous source reselection and subsequent shortage save identically");
            c.Equal(WorkResourceCodes.Stock, node.Blocker, "shortage uses the actual committed second source, never the first unselected candidate's lost-source result");
            c.Equal(new ResourceEndpointId("colony.pools", 5000000000L), node.Resources[0].Choice.Endpoint, "second-source shortage retains its own real endpoint binding");
            var refillOwner = new ReservationOwner("proof.refill.continuous", 1); var destination = pools.Get(5000000000L);
            c.That(transaction.Commit(new ResourceTransferIntent(refillOwner, Company, source.ReserveOut(refillOwner, water, Fix.One),
                destination.ReserveIn(refillOwner, water, Fix.One), water, Fix.One)).Committed, "setup replenishes the alternative with real transferred stock, never a grant");
            Tick(game); twin = factory.Load(game.Save()); Reach(game, () => order.State == OrderState.Done, "explicit source reselection and replenished completion", twin);
            c.Equal(game.Hash(), twin.Hash(), "reselected continuous source continues from exact prior fractional progress");
            c.Equal(new BigInteger(Fix.FromInt(3).Raw), History(game).Totals(water).ConsumedRaw, "source reselection spends remaining work only, never repeats its original budget");
            c.Equal(Fix.Zero, destination.Amount(water), "reselection uses the alternative's actual remaining native stock");
        }
        private static void ProviderFault(Check c, GameFactory factory)
        {
            var game = New(factory); var target = Spawn(game, "requirements.target", 20, 20); Spawn(game, "continuous.slow", 6, 20);
            var pools = game.World.Services.Get<EndpointPools>(); var water = Resource(game, "water"); var source = target.Get<IInventory>();
            var owner = new ReservationOwner("proof.continuous.fault", 1); var endpoint = pools.Get(1);
            c.That(game.World.Services.Get<IResourceTransaction>().Commit(new ResourceTransferIntent(owner, Company,
                source.ReserveOut(owner, water, Fix.One), endpoint.ReserveIn(owner, water, Fix.One), water, Fix.One)).Committed, "fault setup transfers actual stock");
            var order = Post(c, game, target, "continuous.connected-work"); long before = History(game).Latest;
            pools.FaultEndpoint = 1; pools.ClaimFault = ClaimFaultMode.ShortTake;
            for (int i = 0; i < 500 && game.Fault == null; i++) game.Update();
            c.That(game.Fault != null && game.Fault.Message.Contains("exact owned consumption"), "short native continuous consumption faults normal Update for its protocol violation");
            c.Equal(WorkProgress.Zero, order.Node("work").Progress, "faulted consumption cannot grant completed work");
            c.Equal(before, History(game).Latest, "faulted continuous set emits no fake partial-success history");
            c.Throws<InvalidOperationException>(() => game.Save(), "faulted continuous provider cannot checkpoint partial mutation");
        }
        private static void Parallel(Check c, GameFactory factory)
        {
            var game = New(factory); var target = Spawn(game, "continuous.target", 20, 20); Spawn(game, "continuous.slow", 16, 20); Spawn(game, "continuous.slow", 16, 24);
            var order = Post(c, game, target, "continuous.parallel"); var a = order.Node("a"); var b = order.Node("b");
            Reach(game, () => a.Progress.Fraction > 0 || b.Progress.Fraction > 0, "parallel next-input contention");
            a.Progress.TryAsFix(out Fix first); b.Progress.TryAsFix(out Fix second);
            c.Equal(Fix.FromRatio(1, 2), first + second, "only one of two competing physical half increments can use the actual two-unit stock");
            c.Equal(Fix.Zero, target.Get<IInventory>().Amount(Resource(game, "water")), "parallel next-step admission cannot duplicate the scarce native input");
            c.Equal(new BigInteger(Fix.FromInt(2).Raw), History(game).Totals(Resource(game, "water")).ConsumedRaw, "parallel actual consumption is conserved and published once");
            var twin = factory.Load(game.Save()); for (int i = 0; i < 5; i++) { Tick(game); Tick(twin); }
            c.Equal(game.Hash(), twin.Hash(), "competing source-only bindings and typed resource waits save/continue identically");
            c.That(a.Blocker == WorkResourceCodes.Stock && b.Blocker == WorkResourceCodes.Stock, "both waiting continuations expose stock pressure through existing nodes");
        }
        private static void Local(Check c)
        {
            var module = new LocalWorkProofModule(); var factory = OptionalWorkTests.Factory(module,
                FrameworkProofContent.Read("local-work.json").Replace("\"use\": \"consume\"", "\"use\": \"continuous\""));
            var game = OptionalWorkTests.New(factory); var entities = new List<EntityView>(); game.Seat(0).View.CopyEntities(Company, entities);
            var target = game.World.Entities.Get(entities.Find(e => e.Def == "local").Id);
            c.That(game.World.Space == null && !game.World.Services.Has<IEconomy>(), "continuous native work composes without map, money, hauling or game-specific fuel services");
            var order = Post(c, game, target, "settle"); var twin = factory.Load(game.Save());
            Reach(game, () => order.State == OrderState.Done, "mapless native continuous completion", twin);
            c.Equal(game.Hash(), twin.Hash(), "mapless unpriced continuous inputs save/continue through the same progress owner");
            c.Equal(0, target.Get<IInventory>().Reservations.Count, "mapless continuous inputs leave no persistent promise");
        }
        private static string ElapsedJson => FrameworkProofContent.Read("elapsed-work.json");
        private static void ElapsedAdmission(Check c)
        {
            GameFactory factory = Build(); SimGame game = New(factory);
            Entity target = Spawn(game, "continuous.target", 20, 20); Spawn(game, "continuous.slow", 6, 20); Tick(game);
            ulong before = game.Hash(); var offers = new List<WorkWorkerCandidate>();
            c.Equal(Code.None, game.Seat(0).View.Feature<WorkView>().PreviewWorkerCandidates("elapsed.work", "work", target.Id, offers), "elapsed inputs use normal candidate preview");
            c.Equal(WorkRequirementStatus.Satisfied, offers[0].Result.Status, "only next productive elapsed quantum needs funding");
            foreach (var candidate in offers[0].Result.ResourceCandidates)
                if (candidate.Choice.Requirement == "elapsed.fluid")
                    c.Equal(Fix.FromRatio(1, 2), candidate.Availability[0].Required, "elapsed demand is game-time based, not half worker progress");
            c.Equal(before, game.Hash(), "elapsed candidate/source/quantum views stay pure");
            c.Equal(WorkResourceUse.Elapsed, game.World.Content.Get<TargetResourceRequirementDef>("elapsed.fluid").Use, "master-listed JSON selects explicit elapsed mode");
            var water = Resource(game, "water"); var steel = Resource(game, "steel");
            var rates = new List<IResourceRate>(game.World.Content.Get<TargetResourceRequirementDef>("elapsed.fluid").Rates);
            rates.AddRange(game.World.Content.Get<TargetResourceRequirementDef>("elapsed.discrete").Rates);
            Bill spent = WorkBoard.ElapsedConsumed(rates.AsReadOnly(), new WorkProgress(24), game.SimulationTime, game.World.Content);
            c.Equal(Fix.FromInt(12), Amount(spent, water), "elapsed cumulative usage may exceed reference quantity for a slow machine");
            c.Equal(Fix.FromInt(4), Amount(spent, steel), "discrete elapsed charges round cumulative quantities, not every update");
            foreach (string invalid in new[] { "per_tick", "wall_time", "Elapsed" })
                c.Throws<ContentException>(() => New(Build(elapsedJson: ElapsedJson.Replace("\"use\":\"elapsed\"", "\"use\":\"" + invalid + "\""))), "unknown elapsed aliases are rejected: " + invalid);
            RateAdmission(c, game);
        }
        private static void ElapsedPhysical(Check c)
        {
            RatePhysical(c);
            GameFactory factory = Build(); SimGame game = New(factory);
            Entity target = Spawn(game, "continuous.target", 20, 20); Entity worker = Spawn(game, "continuous.slow", 6, 20);
            Order order = Post(c, game, target, "elapsed.work"); ActivityNodeExecution node = order.Node("work");
            var water = Resource(game, "water"); Tick(game);
            c.Equal(0L, node.ProductiveSteps, "physical travel earns no productive exposure");
            c.Equal(Fix.FromInt(2), target.Get<IInventory>().Amount(water), "travel burns no elapsed activity fuel");
            Reach(game, () => node.ProductiveSteps > 0, "first elapsed productive update");
            c.Equal(1L, node.ProductiveSteps, "one funded productive update earns exactly one sole-clock quantum");
            c.Equal(new WorkProgress(0, 2147483648), node.Progress, "half-rate work remains separate from productive duration");
            c.Equal(Fix.FromRatio(3, 2), target.Get<IInventory>().Amount(water), "half-rate machine uses full productive elapsed charge");
            var facts = new List<ActivityNodeView>(); game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order.Id, facts);
            WorkResourceUsage fuel = null; foreach (var input in facts[0].RunningInputs) if (input.Requirement == "elapsed.fluid") fuel = input;
            c.That(fuel.Use == WorkResourceUse.Elapsed && fuel.RemainingBudget == null, "elapsed usage never fabricates a bounded remaining fuel budget");
            c.Equal(Fix.FromRatio(1, 2), Amount(fuel.Consumed, water), "usage derives consumed input from same actual node exposure");
            c.Equal(1L, facts[0].ProductiveSteps, "immutable node facts expose engine-owned productive duration");
            c.Equal(1L, game.SimulationTime.ExactTicks(facts[0].ProductiveGameTime), "node game duration projects the sole clock quantum, not a local clock");
            var privateFacts = new List<ActivityNodeView>();
            c.That(!game.Seat(1).View.Feature<WorkView>().TryCopyNodes(order.Id, privateFacts), "foreign company receives no elapsed usage facts");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "productive exposure uses the existing node codec");
            game.Clock.Pause(); ulong paused = game.Hash(); c.Equal(0, game.Update(), "sole-clock pause funds no productive elapsed interval");
            c.Equal(paused, game.Hash(), "sole-clock pause changes neither usage nor work"); game.Clock.Resume();
            Reach(game, () => node.State == NodeState.Paused, "elapsed native stock shortage", twin);
            c.Equal(4L, node.ProductiveSteps, "four funded time quanta exhaust two fluid units");
            c.Equal(new WorkProgress(2), node.Progress, "slow work reaches only two work quanta at four productive time quanta");
            c.Equal(game.Hash(), twin.Hash(), "saved elapsed exhaustion matches actual source and progress");
            for (int i = 0; i < 5; i++) { Tick(game); Tick(twin); }
            c.Equal(4L, node.ProductiveSteps, "blocked resource wait earns no elapsed usage");
            c.Equal(Fix.FromRatio(1, 2), Amount(fuel.Consumed, water), "retained elapsed facts remain detached during future work");
            long valid = node.ProductiveSteps; node.ProductiveSteps = 0;
            c.Throws<CorruptStateException>(() => factory.Load(game.Save()), "positive saved work requires actual productive exposure"); node.ProductiveSteps = valid;
            node.ProductiveSteps = checked(game.Tick + 1);
            c.Throws<CorruptStateException>(() => factory.Load(game.Save()), "future productive exposure cannot exceed actual completed game time"); node.ProductiveSteps = valid;
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "restoring valid node exposure restores the same sole codec");
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = order.Id }).Accepted, "normal player cancellation stops elapsed work");
            for (int i = 0; i < 3; i++) Tick(game);
            c.Equal(valid, node.ProductiveSteps, "cancelled node cannot accrue idle fuel exposure");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled elapsed history remains durable without refunding fuel");

            game = New(factory); target = Spawn(game, "continuous.target", 20, 20); worker = Spawn(game, "continuous.slow", 6, 20);
            WorkerExecutionTestContent.Control(game, worker.Id, true, Fix.Zero);
            order = Post(c, game, target, "elapsed.work"); for (int i = 0; i < 10; i++) Tick(game);
            c.Equal(0L, order.Node("work").ProductiveSteps, "zero effective work rate earns no elapsed activity consumption");
            c.Equal(Fix.FromInt(2), target.Get<IInventory>().Amount(water), "zero-rate assigned machine preserves all native fuel");

            game = New(factory); target = Spawn(game, "continuous.target", 20, 20); Spawn(game, "continuous.slow", 6, 20);
            order = Post(c, game, target, "elapsed.supplied"); node = order.Node("work");
            Reach(game, () => node.State == NodeState.Paused, "elapsed automatic demand before delivery");
            c.Equal(Fix.FromRatio(1, 2), game.World.Services.Get<LogisticsService>().DemandOf(target.Id, water), "existing logistics requests next actual elapsed input, not work-normalized quarter");
            Spawn(game, "depot", 14, 20); Spawn(game, "truck", 16, 24); Tick(game); twin = factory.Load(game.Save());
            Reach(game, () => order.State == OrderState.Done, "elapsed physical fuel delivery and completion", twin);
            c.Equal(24L, node.ProductiveSteps, "half-rate machine consumes twenty-four real productive game updates");
            c.Equal(ResourceFlowReadStatus.Expired, History(game).Status, "long physical supply run may evict the bounded diagnostic page");
            BigInteger sessionConsumed = 0;
            foreach (var aggregate in game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(Company).Aggregates)
                if (aggregate.Resource == water && aggregate.Kind == ResourceFlowKind.Consumed) sessionConsumed += aggregate.AmountRaw;
            c.Equal(new BigInteger(Fix.FromInt(12).Raw), sessionConsumed, "actual whole-session journal accounts twelve units despite page eviction");
            c.Equal(game.Hash(), twin.Hash(), "elapsed supply, progress and consumption continue identically after save");
            c.Equal(Fix.Zero, game.World.Services.Get<LogisticsService>().DemandOf(target.Id, water), "completed elapsed activity withdraws demand");

            GameFactory fast = Build(Json.Replace("\"work_rate\":0.5", "\"work_rate\":2"));
            game = New(fast); target = Spawn(game, "depot", 20, 20); Spawn(game, "continuous.slow", 6, 20);
            order = Post(c, game, target, "elapsed.work"); Reach(game, () => order.State == OrderState.Done, "fast elapsed activity completion");
            c.Equal(6L, order.Node("work").ProductiveSteps, "faster compatible machine uses fewer productive time intervals");
            c.Equal(new BigInteger(Fix.FromInt(3).Raw), History(game).Totals(water).ConsumedRaw, "fast elapsed machine consumes three units, not full reference budget");

            game = New(factory); target = Spawn(game, "depot", 20, 20);
            order = Post(c, game, target, "elapsed.timed"); twin = factory.Load(game.Save());
            Reach(game, () => order.State == OrderState.Done, "elapsed timer without worker", twin);
            c.Equal(12L, order.Node("work").ProductiveSteps, "role-free node uses its actual funded game duration");
            c.Equal(new BigInteger(Fix.FromInt(6).Raw), History(game).Totals(water).ConsumedRaw, "normal-rate timer uses the reference interval quantity");
            c.Equal(game.Hash(), twin.Hash(), "elapsed timer restores without a machine or another timer owner");

            game = New(factory); target = Spawn(game, "continuous.target", 20, 20); worker = Spawn(game, "continuous.slow", 6, 20);
            order = Post(c, game, target, "elapsed.worker-work"); twin = factory.Load(game.Save());
            Reach(game, () => order.Node("work").State == NodeState.Paused, "worker-local discrete elapsed shortage", twin);
            c.Equal(12L, order.Node("work").ProductiveSteps, "one prepaid discrete native unit funds twelve elapsed quanta");
            c.Equal(new WorkProgress(6), order.Node("work").Progress, "next native elapsed charge pauses actual slower work");
            c.Equal(Fix.Zero, worker.Get<IInventory>().Amount(Resource(game, "wrench")), "worker-local elapsed source really consumes its tool material");
            c.Equal(game.Hash(), twin.Hash(), "worker-local elapsed lifetime saves through ordinary source/step bindings");
        }
        private static void RateAdmission(Check c, SimGame game)
        {
            var definition = game.World.Content.Get<TargetResourceRequirementDef>("elapsed.fluid");
            c.Equal(12L, definition.Rates[0].PeriodNumerator, "authored rate has its own explicit period");
            c.Throws<NotSupportedException>(() => ((IList<IResourceRate>)definition.Rates).Clear(), "resolved rates are immutable");
            string fluid = "\"rates\":[{\"resource\":\"water\",\"quantity\":6,\"period\":{\"numerator\":12,\"denominator\":1,\"unit\":\"minute\"}}]";
            foreach (string invalid in new[] {
                ElapsedJson.Replace(fluid, "\"quantities\":{\"water\":6}"),
                ElapsedJson.Replace(fluid, fluid + ",\"quantities\":{\"water\":6}"),
                ElapsedJson.Replace(fluid, "\"rates\":[]"),
                ElapsedJson.Replace(fluid, "\"rates\":null"),
                ElapsedJson.Replace("\"numerator\":12", "\"numerator\":0"),
                ElapsedJson.Replace("\"denominator\":1", "\"denominator\":0"),
                ElapsedJson.Replace("\"unit\":\"minute\"", "\"unit\":\"update\""),
                ElapsedJson.Replace("\"quantity\":6", "\"quantity\":0"),
                ElapsedJson.Replace("\"quantity\":6", "\"quantity\":-1"),
                ElapsedJson.Replace("\"resource\":\"water\"", "\"resource\":\"undeclared\""),
                ElapsedJson.Replace("\"resource\":\"steel\",\"quantity\":2", "\"resource\":\"steel\",\"quantity\":0.5"),
                ElapsedJson.Replace(fluid, fluid.Substring(0, fluid.Length - 1) + "," + fluid.Substring(9))
            }) c.Throws<ContentException>(() => New(Build(elapsedJson: invalid)), "invalid/old/ambiguous elapsed rate intent refuses before play");

            var longer = New(Build(elapsedJson: ElapsedJson.Replace("\"hours\":0.2", "\"hours\":0.4")));
            foreach (var candidateGame in new[] { game, longer })
            {
                var target = Spawn(candidateGame, "depot", 32, 20); Spawn(candidateGame, "continuous.slow", 6, 24); Tick(candidateGame);
                long time = candidateGame.Clock.StepIndex; ulong before = candidateGame.Hash();
                var candidates = new List<WorkWorkerCandidate>();
                c.Equal(Code.None, candidateGame.Seat(0).View.Feature<WorkView>().PreviewWorkerCandidates("elapsed.work", "work", target.Id, candidates), "normal engine preview admits exact rates");
                foreach (var candidate in candidates[0].Result.ResourceCandidates)
                    if (candidate.Choice.Requirement == "elapsed.fluid")
                    {
                        c.Equal(Fix.FromRatio(1, 2), candidate.Availability[0].Required, "changing activity duration never changes fuel per game minute");
                        c.Equal(12L, candidate.Choice.Rates[0].PeriodNumerator, "candidate exposes the same exact authored period");
                        c.Throws<NotSupportedException>(() => ((IList<IResourceRate>)candidate.Choice.Rates).Clear(), "candidate rate list is immutable");
                    }
                c.Equal(before, candidateGame.Hash(), "rate preview does not mutate resources/progress");
                c.Equal(time, candidateGame.Clock.StepIndex, "rate preview does not advance the sole game clock");
            }
            var water = Resource(game, "water");
            var fractional = game.World.Content.Get<TargetResourceRequirementDef>("elapsed.fractional").Rates;
            c.Equal(Fix.FromRaw((Fix.One.Raw * 2 + 2) / 3), Amount(WorkBoard.ElapsedConsumed(fractional, new WorkProgress(1), game.SimulationTime, game.World.Content), water),
                "ninety-second rate is not rounded to a two-minute update period");
            c.Equal(Fix.FromInt(8), Amount(WorkBoard.ElapsedConsumed(fractional, new WorkProgress(12), game.SimulationTime, game.World.Content), water),
                "fractional-period cumulative rounding has no per-update drift");
            c.Throws<OverflowException>(() => WorkBoard.ElapsedConsumed(definition.Rates, new WorkProgress(long.MaxValue), game.SimulationTime, game.World.Content),
                "wide elapsed accumulation refuses unrepresentable stock without wrapping");
            var steel = Resource(game, "steel");
            c.Throws<ArgumentException>(() => new WorkResourceChoice(new[] { "proof" }, "proof", new ResourceEndpointId("proof", 1),
                new Bill(new[] { new ResourceAmount(steel, Fix.One) }), WorkResourceUse.Elapsed, definition.Rates), "public rate choice refuses bill/rate disagreement");
        }
        private static void RatePhysical(Check c)
        {
            var factory = Build(); var game = New(factory); var target = Spawn(game, "depot", 20, 20);
            var order = Post(c, game, target, "elapsed.mixed-work"); var twin = factory.Load(game.Save());
            var facts = new List<ActivityNodeView>(); ulong before = game.Hash();
            c.That(game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order.Id, facts), "owner reads exact running rate facts while order is retained");
            c.Equal(2, facts[0].RunningInputs[0].Rates.Count, "one requirement exposes all independent resource periods");
            c.Throws<NotSupportedException>(() => ((IList<IResourceRate>)facts[0].RunningInputs[0].Rates).Clear(), "running usage has a detached immutable rate list");
            c.Equal(before, game.Hash(), "usage rates remain pure observations");
            var retainedRates = facts[0].RunningInputs[0].Rates;
            Reach(game, () => order.State == OrderState.Done, "independent mixed rate periods", twin);
            var water = Resource(game, "water"); var steel = Resource(game, "steel");
            c.Equal(new BigInteger(Fix.FromInt(6).Raw), History(game).Totals(water).ConsumedRaw, "water rate uses its six-minute period");
            c.Equal(new BigInteger(Fix.FromInt(3).Raw), History(game).Totals(steel).ConsumedRaw, "discrete steel uses its own five-minute period and cumulative prepayment");
            c.Equal(game.Hash(), twin.Hash(), "multiple periods continue through the existing saved node/receipts");
            c.Equal(2, retainedRates.Count, "earlier detached rates survive normal terminal order pruning");

            game = New(factory); target = Spawn(game, "depot", 20, 20); order = Post(c, game, target, "elapsed.fractional-work");
            Tick(game); twin = factory.Load(game.Save());
            Reach(game, () => order.State == OrderState.Done, "fractional rate native physical consumption", twin);
            c.Equal(new BigInteger(Fix.FromInt(8).Raw), History(game).Totals(water).ConsumedRaw, "actual ninety-second input totals eight, not six or per-update overcharge");
            c.Equal(game.Hash(), twin.Hash(), "fractional rate consumption saves without a remainder ledger");

            game = New(factory); target = Spawn(game, "depot", 20, 20); order = Post(c, game, target, "elapsed.scaled");
            Reach(game, () => order.Node("work").State == NodeState.Paused, "scaled exact operating rate shortage");
            c.Equal(10L, order.Node("work").ProductiveSteps, "multiplier doubles the native rate, not the rate period or game clock");
            game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order.Id, facts);
            c.Equal(Fix.FromInt(12), facts[0].RunningInputs[0].Rates[0].Quantity, "scaled immutable rate matches the same native reference bill");
            c.Equal(12L, facts[0].RunningInputs[0].Rates[0].PeriodNumerator, "scaling leaves independent rate time intact");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "scaled rate stock wait restores identically");

            factory = Build(elapsedJson: ElapsedJson.Replace("\"hours\":0.2", "\"hours\":0.4"));
            game = New(factory); target = Spawn(game, "depot", 20, 20); order = Post(c, game, target, "elapsed.timed");
            Reach(game, () => order.Node("work").State == NodeState.Paused, "longer activity at unchanged operating rate");
            c.Equal(20L, order.Node("work").ProductiveSteps, "longer node exhausts ten water at the same half-unit rate");
            c.Equal(new WorkProgress(20), order.Node("work").Progress, "fuel shortage does not falsely complete the longer twenty-four-minute activity");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "duration-independent rate shortage saves through the same codec");

            factory = Build(worldJson: FrameworkProofContent.Read("resource-recovery.json").Replace("\"numerator\":60,\"denominator\":1,\"unit\":\"second\"", "\"numerator\":30,\"denominator\":1,\"unit\":\"second\""));
            game = New(factory); target = Spawn(game, "depot", 20, 20); order = Post(c, game, target, "elapsed.fractional-work");
            twin = factory.Load(game.Save()); Reach(game, () => order.State == OrderState.Done, "alternate single-clock quantum with exact rate", twin);
            c.Equal(24L, order.Node("work").ProductiveSteps, "same twelve game minutes uses twenty-four half-minute updates");
            c.Equal(new BigInteger(Fix.FromInt(8).Raw), History(game).Totals(water).ConsumedRaw, "changing game update quantum does not change rational input rate");
            c.Equal(game.Hash(), twin.Hash(), "alternate quantum and input period restore together without a second clock");

            factory = Build(); game = New(factory); target = Spawn(game, "requirements.target", 20, 20); Spawn(game, "continuous.slow", 6, 20);
            var pools = game.World.Services.Get<EndpointPools>(); var source = target.Get<IInventory>();
            var transaction = game.World.Services.Get<IResourceTransaction>();
            foreach (long identity in new[] { 1L, 5000000000L })
            {
                var pool = pools.Get(identity); var owner = new ReservationOwner("proof.seed.elapsed", identity);
                c.That(transaction.Commit(new ResourceTransferIntent(owner, Company, source.ReserveOut(owner, water, Fix.One),
                    pool.ReserveIn(owner, water, Fix.One), water, Fix.One)).Committed, "external source setup transfers real stock through normal endpoint contracts");
            }
            order = Post(c, game, target, "elapsed.connected-work"); twin = factory.Load(game.Save());
            Reach(game, () => order.Node("work").State == NodeState.Paused, "public-only scoped source rate shortage", twin);
            c.Equal(6L, order.Node("work").ProductiveSteps, "public source's six-minute rate funds exactly six productive game updates");
            c.Equal(new WorkProgress(3), order.Node("work").Progress, "public-only compatible worker advances at its real half rate");
            c.Equal(Fix.Zero, pools.Get(1).Amount(water), "rate execution consumes actual selected scoped endpoint stock");
            c.Equal(Fix.One, pools.Get(5000000000L).Amount(water), "stock waits cannot silently change a committed source");
            c.Equal(game.Hash(), twin.Hash(), "public-only provider/source/native usage continue through existing save ownership");
        }
        private static void ScalingAndOvershoot(Check c, GameFactory factory)
        {
            var game = New(factory); var target = Spawn(game, "continuous.target", 20, 20); var order = Post(c, game, target, "continuous.scaled");
            Reach(game, () => order.Node("work").State == NodeState.Paused, "scaled continuous budget depletion");
            c.Equal(new WorkProgress(2), order.Node("work").Progress, "multiplier doubles native usage per actual increment, not elapsed progress");
            var nodes = new List<ActivityNodeView>(); game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order.Id, nodes);
            var water = Resource(game, "water");
            c.Equal(Fix.FromInt(12), Amount(nodes[0].RunningInputs[0].ReferenceQuantity, water), "continuous total budget aliases the same exact authored multiplier");
            c.Equal(Fix.FromInt(2), Amount(nodes[0].RunningInputs[0].Consumed, water), "scaled consumed amount follows actual progress");
            c.Equal(Fix.FromInt(10), Amount(nodes[0].RunningInputs[0].RemainingBudget, water), "scaled remaining amount is derived without another accumulator");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "scaled pending native work restores exactly");
            var fastFactory = Build(Json.Replace("\"work_rate\":0.5", "\"work_rate\":100"));
            game = New(fastFactory); target = Spawn(game, "depot", 20, 20); Spawn(game, "continuous.slow", 6, 20);
            order = Post(c, game, target, "continuous.work"); var twin = fastFactory.Load(game.Save());
            Reach(game, () => order.State == OrderState.Done, "overshooting effective worker rate", twin);
            c.Equal(new WorkProgress(12), order.Node("work").Progress, "high effective work rate clamps at authored work, not the overshooting rate");
            c.Equal(new BigInteger(Fix.FromInt(6).Raw), History(game).Totals(Resource(game, "water")).ConsumedRaw, "overshoot consumes exactly the native authored total");
            c.Equal(new BigInteger(Fix.FromInt(4).Raw), History(game).Totals(Resource(game, "steel")).ConsumedRaw, "startup and discrete continuous inputs share one bounded physical commit");
            c.Equal(game.Hash(), twin.Hash(), "one-step completed budget/history and held-tool release save/continue identically");
        }
    }
}
