using System;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ResourceDispositionTests;
using static Beep.SimEngine.Tests.PhysicalRecoveryTestContent;

namespace Beep.SimEngine.Tests
{
    public static class CargoRecoveryTests
    {
        public static void Register()
        {
            TestRunner.Add("Cargo recovery: timed evacuation, native receipts and saved physical salvage", Physical);
            TestRunner.Add("Cargo recovery: unsafe sources, owned claims and authored completion admission", Guards);
        }
        private static ResourceFlowPage Read(SimGame game, long cursor = 0)
            => game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), cursor);
        private static Entity Hold(Check c, SimGame game)
        {
            Entity target = Spawn(game, "loaded_truck", 20, 20);
            c.That(TestKit.Order(game, 0, new MoveVehicleCommand { Vehicle = target.Id, To = new TileCoord(20, 20) }).Accepted,
                "player holds a carrying machine through the ordinary engine command");
            c.That(target.Get<IWorker>().Held && target.Get<IWorker>().Idle, "held machine is unavailable for automatic work");
            return target;
        }
        private static Order Post(Check c, SimGame game, Entity target, string plan = "recover.cargo")
        {
            CommandResult result = TestKit.Order(game, 0, new PostOrderCommand { Target = target.Id, Plan = plan });
            c.That(result.Accepted, "player posts JSON-authored recovery through normal work command");
            return game.World.Services.Get<WorkBoard>().Order(checked((int)result.Arg0));
        }
        private static Entity Pile(SimGame game)
        {
            foreach (Entity entity in game.World.Entities.All)
                if (entity.Alive && entity.Def.HasTag("pile") && entity.Owner == new PlayerId(0)) return entity;
            return null;
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Factory(ResourceDispositionTests.Json, cargo: true); SimGame game = New(factory); Entity target = Hold(c, game);
            IInventory cargo = target.Get<ITransporter>().Cargo; ResourceId steel = game.World.Content.ResourceId("steel"), water = game.World.Content.ResourceId("water");
            long cursor = Read(game).Latest; Order order = Post(c, game, target);
            c.That(order != null && order.IsActive && order.Priority == 90, "normal board owns the same priority/execution identity");
            c.Equal(Fix.FromInt(2), cargo.Amount(steel), "one real preparation input is consumed from the target");
            c.Equal(Fix.FromRatio(11, 2), cargo.Amount(water), "posting cannot instantly move loaded fluid");
            c.That(Pile(game) == null, "no duplicate salvage stock is created at posting");
            WorkView view = game.Seat(0).View.Feature<WorkView>(); ulong before = game.Hash();
            c.Equal(Code.None, view.PreviewNodeEffect(order.Id, "recover"), "preview delegates current packing/claims to existing recovery owner");
            c.Equal(Codes.NotOwned, game.Seat(1).View.Feature<WorkView>().PreviewNodeEffect(order.Id, "recover"), "foreign recovery diagnostics remain private");
            ResourceRecoveryEffectDef effect = game.World.Content.Get<ResourceRecoveryEffectDef>("proof.recover");
            c.Equal(WorkEffectCodes.NotComplete, effect.Apply(game.World, order, order.Plan.Graph.Nodes[0]).Blocker, "public effect refuses unfinished work instead of instant evacuation");
            c.Equal(before, game.Hash(), "pure preflight and early completion refusal allocate no pile/receipt/stock");
            Entity crane = game.World.Entities.Get(Colony.Mine(game, 0, "crane")[0].Id);
            FixVec2 origin = crane.Get<ITilePresence>().Position;
            SimGame start = factory.Load(game.Save()); c.Equal(game.Hash(), start.Hash(), "posted held-cargo recovery restores without grants");
            int ticks = 0;
            while (order.IsActive && order.Nodes[0].Progress.WholeTicks < 3 && ticks++ < 300) { Tick(game); Tick(start); }
            c.That(order.IsActive && order.Nodes[0].Progress.WholeTicks >= 3, "crew reaches the target and does partial positive-time work");
            c.That(crane.Get<ITilePresence>().Position != origin, "different machine physically travels to the held carrier");
            c.Equal(Fix.FromInt(2), cargo.Amount(steel), "partial recovery leaves native cargo in its original endpoint");
            c.That(Pile(game) == null, "partial work creates no completed recovery endpoint");
            c.Equal(game.Hash(), start.Hash(), "saved posted twin executes identical travel and partial progress");
            SimGame mid = factory.Load(game.Save()); c.Equal(game.Hash(), mid.Hash(), "partial machine work saves its exact progress/job");
            while (order.IsActive && ticks++ < 400) { Tick(game); Tick(mid); }
            c.Equal(OrderState.Done, order.State, "bounded engine workflow completes timed cargo evacuation");
            c.Equal(game.Hash(), mid.Hash(), "saved partial recovery continues identically");
            c.Equal(Fix.Zero, cargo.Amount(steel), "verified actual source count stock is empty");
            c.Equal(Fix.Zero, cargo.Amount(water), "verified actual source fluid stock is empty");
            c.Equal(0, cargo.Reservations.Count, "evacuation leaves no orphan source promises");
            c.That(target.Alive && target.Get<IWorker>().Held, "recovering cargo never silently deletes or releases the held machine");
            Entity pile = Pile(game); c.That(pile != null, "same recovery coordinator creates a real local receiving endpoint");
            c.Equal(target.Get<ITilePresence>().Tile, pile.Get<ITilePresence>().Tile, "recovery stays at the actual source, no distant teleport");
            c.Equal(Fix.FromInt(2), pile.Get<IInventory>().Amount(steel), "existing stock arrives, not a fabricated structural yield");
            c.Equal(Fix.FromRatio(11, 2), pile.Get<IInventory>().Amount(water), "local stock keeps exact continuous units");
            c.Equal(0, pile.Get<IInventory>().Reservations.Count, "receiving receipts are consumed exactly");
            ResourceFlowPage page = Read(game, cursor); int transfers = 0;
            foreach (var entry in page.Entries)
            {
                ResourceFlowFact fact = entry.Item; if (!fact.IsTransfer) continue; transfers++;
                c.Equal(ResourceFlowKind.StockTransfer, fact.Kind, "cargo evacuation publishes real movement, not lost/produced compensation");
                c.Equal(ResourceEndpointId.ForEntity(target.Id), fact.Source.Endpoint, "real cargo endpoint owns outgoing receipt");
                c.Equal(ResourceEndpointId.ForEntity(pile.Id), fact.Destination.Endpoint, "real locally registered endpoint owns receiving receipt");
                c.Equal(new ReservationOwner("sim.resource.recovery", target.Id.Value), fact.Owner, "existing recovery coordinator owns both receipts");
                c.Equal(fact.Amount, fact.SourceBefore.Value - fact.SourceAfter.Value, "native source delta is verified");
                c.Equal(fact.Amount, fact.DestinationAfter - fact.DestinationBefore, "native receiving delta is verified");
            }
            c.Equal(2, transfers, "mixed cargo has two real recovery transfers");
            c.Equal(new BigInteger(Fix.One.Raw), page.Totals(steel).ConsumedRaw, "repair input remains a real spent material");
            c.Equal(BigInteger.Zero, page.Totals(steel).CreatedRaw, "recovery fabricates no production");
            c.Equal(BigInteger.Zero, page.Totals(water).LostRaw, "recovery invents no fluid loss");
            c.Equal(ResourceFlowReadStatus.NotOwned, game.Seat(1).View.Feature<ResourceFlowView>().Read(new PlayerId(0), cursor).Status, "recovery facts are company-private");
            SimGame recovered = factory.Load(game.Save()); c.Equal(game.Hash(), recovered.Hash(), "recovered endpoint/journal persist through existing codecs");

            Entity steelStore = Spawn(game, "steel_store", 26, 20), waterStore = Spawn(game, "water_store", 30, 20);
            Spawn(recovered, "steel_store", 26, 20); Spawn(recovered, "water_store", 30, 20);
            long deliveryCursor = Read(game).Latest; ticks = 0;
            while ((steelStore.Get<IInventory>().Amount(steel) != Fix.FromInt(2) ||
                waterStore.Get<IInventory>().Amount(water) != Fix.FromRatio(11, 2)) && ticks++ < 500)
            { Tick(game); Tick(recovered); }
            c.That(ticks > 5 && ticks < 500, "normal logistics physically hauls recovered stock to compatible destinations");
            c.Equal(Fix.FromInt(2), steelStore.Get<IInventory>().Amount(steel), "all real recovered count stock is delivered");
            c.Equal(Fix.FromRatio(11, 2), waterStore.Get<IInventory>().Amount(water), "all real recovered fluid stock is delivered");
            c.Equal(game.Hash(), recovered.Hash(), "saved recovered stock continues into identical real hauls");
            c.Equal(4, Read(game, deliveryCursor).Entries.Count, "two physical pickup/unload pairs are recorded without duplicates");
            c.Equal(Fix.Zero, cargo.Amount(water), "continued logistics cannot re-grant original cargo");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed recovery/delivery remains a coherent checkpoint");
            Finalizers(c, factory);
            NegativeEffects(c, factory);
        }
        private static void NegativeEffects(Check c, GameFactory factory)
        {
            SimGame game = New(factory); Entity target = Spawn(game, "response_site", 16, 18); Tick(game);
            ResourceId steel = game.World.Content.ResourceId("steel"), water = game.World.Content.ResourceId("water");
            long cursor = Read(game).Latest;
            foreach (string plan in new[] { "consume", "dispose", "leak.cargo" })
            {
                Fix steelBefore = target.Get<IInventory>().Amount(steel), waterBefore = target.Get<IInventory>().Amount(water);
                Order order = Post(c, game, target, plan); SimGame twin = factory.Load(game.Save());
                c.Equal(steelBefore, target.Get<IInventory>().Amount(steel), "physical negative effect never spends ready stock at posting");
                c.Equal(waterBefore, target.Get<IInventory>().Amount(water), "physical negative effect keeps native fluid until completion");
                int ticks = 0; while (order.IsActive && ticks++ < 300) { Tick(game); Tick(twin); }
                c.Equal(OrderState.Done, order.State, "same physical completion owner executes timed " + plan);
                c.Equal(game.Hash(), twin.Hash(), "saved native " + plan + " continues identically");
            }
            c.Equal(Fix.FromInt(37), target.Get<IInventory>().Amount(steel), "exact consumed and destroyed native counts remain distinct");
            c.Equal(Fix.FromInt(6), target.Get<IInventory>().Amount(water), "exact destroyed and lost native fluid deltas remain distinct");
            ResourceFlowPage page = Read(game, cursor);
            c.Equal(4, page.Entries.Count, "completed negative effects publish only four actual native lifecycle facts");
            c.Equal(new BigInteger(Fix.One.Raw), page.Totals(steel).ConsumedRaw, "consumption is verified through the sole lifecycle owner");
            c.Equal(new BigInteger(Fix.FromInt(2).Raw), page.Totals(steel).DestroyedRaw, "destruction is not silently reclassified as consumption");
            c.Equal(new BigInteger(Fix.Half.Raw), page.Totals(water).LostRaw, "loss remains exact, explicit and resource-scoped");
            c.Equal(BigInteger.Zero, page.Totals(water).CreatedRaw, "physical effects create no compensating resource stock");
        }
        private static void Finalizers(Check c, GameFactory factory)
        {
            foreach (string plan in new[] { "recover.final", "recover.complete" })
            {
                SimGame game = New(factory); Entity target = Hold(c, game); Order order = Post(c, game, target, plan);
                long cursor = Read(game).Latest; SimGame twin = factory.Load(game.Save()); int ticks = 0;
                while (order.IsActive && ticks++ < 300) { Tick(game); Tick(twin); }
                c.Equal(OrderState.Done, order.State, "authored " + plan + " runs only after real machine work");
                c.Equal(0, target.Get<IInventory>().Stock.Count, "dependency/final effect empties actual cargo once");
                c.Equal(2, Read(game, cursor).Entries.Count, "dependency/final effect creates two exact transfers, no duplication");
                c.Equal(game.Hash(), twin.Hash(), "saved dependency/final execution remains deterministic");
            }
        }
        private static void Guards(Check c)
        {
            GameFactory factory = Factory(ResourceDispositionTests.Json, cargo: true); SimGame game = New(factory); Entity target = Hold(c, game);
            Order order = Post(c, game, target); IInventory cargo = target.Get<IInventory>();
            ResourceId water = game.World.Content.ResourceId("water"); var owner = new ReservationOwner("proof.recovery-claim", 1);
            ReservationToken claim = cargo.ReserveOut(owner, water, Fix.One); ulong before = game.Hash();
            ResourceRecoveryEffectDef effect = game.World.Content.Get<ResourceRecoveryEffectDef>("proof.recover");
            c.Equal(ResourceTransactions.ActivePromises, effect.Check(game.World, order, order.Plan.Graph.Nodes[0]), "recovery cannot evacuate another job's promised stock");
            c.Equal(ResourceTransactions.ActivePromises, effect.Apply(game.World, order, order.Plan.Graph.Nodes[0]).Blocker, "execution repeats current owned-claim preflight");
            c.Equal(before, game.Hash(), "claim refusal creates no pile/receipt/stock mutation");
            c.That(cargo.CheckReservedOut(owner, claim).Ready, "exact original pickup promise survives refusal");
            cargo.ReleaseOut(owner, claim);
            Order dispose = game.World.Services.Get<WorkBoard>().Post(game.World.Content.Get<ActivityDef>("dispose"), target.Owner, target.Id, 50);
            IInventoryWorkEffectDefinition negative = game.World.Content.Get<IInventoryWorkEffectDefinition>("proof.damage"); before = game.Hash();
            c.Equal(Code.None, negative.Check(game.World, dispose, dispose.Plan.Graph.Nodes[0]), "negative effect reports actual bill readiness before work");
            c.Equal(WorkEffectCodes.NotComplete, negative.Apply(game.World, dispose, dispose.Plan.Graph.Nodes[0]).Blocker, "same completion owner blocks early destruction as well as recovery");
            c.Equal(before, game.Hash(), "early negative effect cannot consume a ready bill");
            game.World.System<WorkSystem>().Cancel(game.World, dispose);
            c.That(TestKit.Order(game, 0, new MoveVehicleCommand { Vehicle = target.Id, To = new TileCoord(40, 20) }).Accepted, "player changes the source's real movement intent");
            before = game.Hash();
            c.Equal(WorkEffectCodes.MovingSource, game.Seat(0).View.Feature<WorkView>().PreviewNodeEffect(order.Id, "recover"), "moving cargo has a typed unsafe-source blocker");
            c.Equal(before, game.Hash(), "moving source preview neither stops nor relocates it");
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = order.Id }).Accepted, "normal player cancellation stops the recovery workflow");
            c.Equal(OrderState.Cancelled, order.State, "one board order records cancellation");
            c.Equal(Fix.FromInt(2), cargo.Amount(game.World.Content.ResourceId("steel")), "already spent input is never refunded or duplicated");
            c.Equal(Fix.FromRatio(11, 2), cargo.Amount(water), "cancelled recovery keeps real loaded stock");
            c.That(Pile(game) == null, "cancellation never creates completed salvage");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "moving-source cancellation saves without orphan execution");
            Busy(c, factory); LateClaim(c, factory); Admission(c);
        }
        private static void Busy(Check c, GameFactory factory)
        {
            SimGame game = New(factory); Entity target = Hold(c, game); Spawn(game, "water_store", 30, 20);
            Order order = Post(c, game, target); target.Get<IWorker>().Held = false;
            game.World.Services.Get<LogisticsService>().PrepareDispatch();
            c.That(game.World.Services.Get<IWorkerDispatchSource>().TryAcquire(target.Id.Value, "return.resource-" + target.Get<IInventory>().Stock[0].Resource.Value, target.Id, WorkCandidateMode.IdleOnly), "shared return source acquires actual loaded cargo");
            ulong before = game.Hash();
            c.Equal(WorkEffectCodes.BusySource, game.Seat(0).View.Feature<WorkView>().PreviewNodeEffect(order.Id, "recover"), "another current job prevents source recovery");
            c.Equal(before, game.Hash(), "busy-source diagnostic does not steal the current worker/receipt");
        }
        private static void LateClaim(Check c, GameFactory factory)
        {
            SimGame game = New(factory); Entity target = Hold(c, game); Order order = Post(c, game, target);
            long cursor = Read(game).Latest; long needed = game.World.Time.Ticks(order.Plan.Graph.Nodes[0].Duration); int ticks = 0;
            while (order.IsActive && order.Nodes[0].Progress.WholeTicks < needed - 1 && ticks++ < 300) Tick(game);
            c.That(order.IsActive && order.Nodes[0].Progress.WholeTicks == needed - 1, "real crew reaches the last unfinished work tick");
            IInventory cargo = target.Get<IInventory>(); ResourceId water = game.World.Content.ResourceId("water");
            var owner = new ReservationOwner("proof.late-recovery", 1); ReservationToken claim = cargo.ReserveOut(owner, water, Fix.One);
            SimGame restored = factory.Load(game.Save()); Tick(game); Tick(restored);
            c.Equal(OrderState.Failed, order.State, "late receipt prevents current completion rather than using an old preview");
            c.Equal(ResourceTransactions.ActivePromises, order.Blocker, "failed physical recovery retains exact owned-claim blocker");
            c.That(Pile(game) == null && cargo.CheckReservedOut(owner, claim).Ready, "failed completion preserves original stock and unrelated claim");
            c.Equal(0, Read(game, cursor).Entries.Count, "failed recovery emits no fictitious transfer/loss facts");
            c.Equal(game.Hash(), restored.Hash(), "saved last-tick refusal is deterministic");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "ordinary failed workflow remains checkpointable");
            cargo.ReleaseOut(owner, claim); Order retry = Post(c, game, target); ticks = 0;
            while (retry.IsActive && ticks++ < 300) Tick(game);
            c.Equal(OrderState.Done, retry.State, "new explicit player order recovers after the real blocker clears");
            c.Equal(0, cargo.Stock.Count, "retry uses actual remaining stock, no compensation or opening grant");
        }
        private static void Admission(Check c)
        {
            foreach (string bad in new[] {
                ResourceDispositionTests.Json.Replace("\"id\":\"recover\",\"executor\":\"work.staffed\",\"roles\":[\"crane\"],\"duration\":{\"numerator\":1,\"denominator\":5,\"unit\":\"hour\"}", "\"id\":\"recover\",\"hours\":0.2"),
                ResourceDispositionTests.Json.Replace("\"id\":\"recover\",\"roles\":[\"crane\"],\"hours\":0.2", "\"id\":\"recover\",\"roles\":[\"crane\"],\"hours\":0"),
                ResourceDispositionTests.Json.Replace("\"after\":[\"prepare\"],\"effect\":\"proof.recover\"", "\"effect\":\"proof.recover\""),
                ResourceDispositionTests.Json.Replace("\"id\":\"damage\",\"roles\":[\"crane\"],\"hours\":0.1,\"effect\":\"proof.single.damage\"", "\"id\":\"damage\",\"hours\":0.1,\"effect\":\"proof.single.damage\""),
                ResourceDispositionTests.Json.Replace("\"id\":\"damage\",\"roles\":[\"crane\"],\"hours\":0.1,\"effect\":\"proof.damage\"", "\"id\":\"damage\",\"hours\":0.1,\"effect\":\"proof.damage\""),
                ResourceDispositionTests.Json.Replace("\"id\":\"recover\",\"roles\":[\"crane\"],\"hours\":0.1}", "\"id\":\"recover\",\"hours\":0.1}") })
                c.Throws<ContentException>(() => New(Factory(bad, cargo: true)), "instant/timer-only/unrelated-machine physical effect refuses admission");
            string noRecovery = PointGame.Definitions.Replace("\"activities\": [", "\"resource_recovery_effects\": [{\"id\":\"no.recovery\"}], \"activities\": [{\"id\":\"recover\",\"automatic_supply\":false,\"nodes\":[{\"id\":\"work\",\"executor\":\"work.staffed\",\"roles\":[\"inspect\"],\"duration\":{\"numerator\":1,\"denominator\":10,\"unit\":\"hour\"},\"effect\":\"no.recovery\"}]},");
            c.Throws<ContentException>(() => PointGame.Factory(noRecovery).NewGame(new NewGameRequest { Scenario = "points" }), "selected recovery work requires an actual optional IResourceRecovery provider");
            try { PointGame.Factory(noRecovery).NewGame(new NewGameRequest { Scenario = "points" }); c.That(false, "missing recovery provider must refuse"); }
            catch (ContentException ex) { c.That(ex.Message.Contains("requires IResourceRecovery"), "refusal names the missing contract, not an unrelated content error: " + ex.Message); }
            c.That(typeof(ResourceRecoveryEffectDef).BaseType == typeof(Def) && typeof(InventoryEffectDef).BaseType == typeof(Def) && typeof(IPhysicalWorkEffect).IsAssignableFrom(typeof(ResourceRecoveryEffectDef)) && typeof(IPhysicalWorkEffect).IsAssignableFrom(typeof(InventoryEffectDef)), "direct physical contracts use one shared execution/admission owner");
        }
    }
}
