using System;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static partial class ResourceDispositionTests
    {
        public static void Register()
        {
            TestRunner.Add(PortCase, Ports);
            TestRunner.Add(PortBindingCase, PortBindings);
            TestRunner.Add(IncidentPortCase, IncidentPorts);
            TestRunner.Add(IncidentContentionNativeCase, c => IncidentContention(c, false, false, false));
            TestRunner.Add(IncidentContentionScopedCase, c => IncidentContention(c, true, true, true));
            TestRunner.Add(MixedCargoCase, PublicMixedCargo);
            TestRunner.Add("Resource disposition: named episodes, authored native effects and saved physical response", Responses);
            TestRunner.Add("Cargo disposition: mixed stock, private diagnostics and interrupted saved return", Cargo);
        }
        private static readonly Code Damage = new Code("damage"), Alarm = new Code("alarm");
        internal static string Json => FrameworkProofContent.Read("resource-disposition.json");
        internal static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "colony", Seed = 131 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human);
            request.Mods.Add(ResourceFlowsModule.ModuleId);
            return factory.NewGame(request);
        }
        internal static Entity Spawn(SimGame game, string definition, int x, int y, int seat = 0)
        {
            var map = (TileMap)game.World.Space; EntityDef def = game.World.Content.Get<EntityDef>(definition);
            Entity result = def.Kind == EntityKind.Vehicle
                ? map.SpawnMobile(game.World, def, new PlayerId(seat), FixVec2.Centre(new TileCoord(x, y)))
                : map.SpawnAt(game.World, def, new PlayerId(seat), new TileCoord(x, y));
            game.World.Settle(); return result;
        }
        internal static void Tick(SimGame game)
        {
            game.Update();
            if (game.Fault != null) throw new InvalidOperationException("disposition tick " + game.Tick + ": " + game.Fault);
        }
        private static int Reach(SimGame game, SimGame twin, Func<bool> complete, int maximum, string stage, bool awaitingFault = false)
        {
            var elapsed = System.Diagnostics.Stopwatch.StartNew(); int updates = 0;
            while (!complete() && updates < maximum && elapsed.Elapsed < TimeSpan.FromSeconds(20))
            {
                if (awaitingFault) game.Update(); else Tick(game);
                if (twin != null) Tick(twin);
                updates++;
            }
            if (!complete())
            {
                var diagnostic = new System.Text.StringBuilder(stage + " stopped at update=" + game.Tick + ", elapsed-ms=" + elapsed.ElapsedMilliseconds);
                foreach (Entity entity in game.World.Entities.All)
                {
                    IWorker worker = entity.Get<IWorker>(); if (worker == null) continue;
                    Job job = game.World.Services.Get<WorkBoard>().Job(worker.Job);
                    diagnostic.Append("; ").Append(entity.Id).Append('/').Append(entity.Def.Id).Append(" state=").Append(entity.State)
                        .Append(" job=").Append(worker.Job).Append(" status=").Append(job?.State).Append(" motion=").Append(entity.Get<IMotion>()?.Status);
                }
                throw new InvalidOperationException(diagnostic.ToString());
            }
            return updates;
        }
        private static ResourceFlowPage Read(SimGame game, long cursor = 0)
            => game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), cursor);
        private static CommandResult Raise(SimGame game, Entity target, Code source, string kind)
            => TestKit.Order(game, 0, new RaiseIncidentCommand { Target = target.Id, Source = source, Kind = new Code(kind) });
        private static void Responses(Check c)
        {
            GameFactory factory = PhysicalRecoveryTestContent.Factory(Json); SimGame game = New(factory);
            Entity target = Spawn(game, "response_site", 16, 18), foreign = Spawn(game, "response_site", 30, 18, 1);
            Tick(game); long cursor = Read(game).Latest;
            IInventory stock = target.Get<IInventory>(); ResourceId steel = game.World.Content.ResourceId("steel"), water = game.World.Content.ResourceId("water");
            WorkBoard board = game.World.Services.Get<WorkBoard>(); WorkView work = game.Seat(0).View.Feature<WorkView>();
            IncidentsView incidents = game.Seat(0).View.Feature<IncidentsView>();
            c.That(Raise(game, target, Damage, "proof.failure").Accepted, "player command raises the first JSON-authored incident");
            Order old = board.ActiveResponseOn(target.Id, Damage);
            c.That(old != null && old.Priority == 85, "generic engine phase posts ordinary work using authored priority");
            c.That(Raise(game, target, Alarm, "proof.alarm").Accepted, "independent named provider raises the second incident");
            Order alarm = board.ActiveResponseOn(target.Id, Alarm);
            c.That(alarm != null && alarm.Id != old.Id && ReferenceEquals(old.Plan, alarm.Plan), "two named providers share one plan, not one execution");
            c.Equal(old.Id, board.ActiveResponseOn(target.Id, Damage).Id, "second incident does not cancel the first");
            c.That(incidents.TryGetResponseOrder(target.Id, Damage, out int damageId) && damageId == old.Id, "view selects the first named source");
            c.That(incidents.TryGetResponseOrder(target.Id, Alarm, out int alarmId) && alarmId == alarm.Id, "view selects the second named source");
            c.Equal(2, work.ActiveOrders(target.Id), "one authoritative order per active named incident");
            c.Equal(Fix.FromInt(40), stock.Amount(steel), "posting reserves intent but consumes no repair material before productive work");
            c.Equal(Fix.FromInt(8), stock.Amount(water), "destruction waits for timed machine work");
            ulong before = game.Hash();
            c.Equal(Code.None, work.PreviewNodeEffect(old.Id, "damage"), "effect preview uses the same native transaction evaluator");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<WorkView>().PreviewNodeEffect(old.Id, "damage"), "foreign effect diagnostics disclose no order identity");
            c.Equal(Codes.InvalidArgument, work.PreviewNodeEffect(old.Id, "missing"), "unknown stage has typed refusal");
            c.Equal(IncidentCodes.StaleResponse, target.Get<ContractIncident>().CompleteResponse(game.World, old), "unfinished response cannot instantly restart operation");
            for (int i = 0; i < 3; i++) ResponseWork.EnsurePosted(game.World, target, target.Get<ContractIncident>());
            c.Equal(before, game.Hash(), "pure previews and idempotent posting change no stock, jobs or IDs");

            // Engine-provider episode probe, not a host-owned incident rule or execution loop.
            target.Get<ContractIncident>().Renew(game.World); before = game.Hash();
            IInventoryWorkEffectDefinition effect = game.World.Content.Get<IInventoryWorkEffectDefinition>("proof.damage");
            c.Equal(IncidentCodes.StaleResponse, work.PreviewNodeEffect(old.Id, "damage"), "old episode is blocked before step effects");
            c.Equal(IncidentCodes.StaleResponse, effect.Apply(game.World, old, old.Plan.Graph.Nodes[1]).Blocker, "current binding is rechecked at execution");
            c.Equal(before, game.Hash(), "stale effect refusal consumes no inventory or emits facts");
            c.That(!incidents.TryGetResponseOrder(target.Id, Damage, out _), "stale order is not presented as current response work");
            Tick(game);
            Order current = board.ActiveResponseOn(target.Id, Damage);
            c.That(current != null && current.Id != old.Id && current.ResponseEpisode == 2, "engine cancels and reposts only the renewed episode");
            c.Equal(OrderState.Cancelled, old.State, "obsolete episode leaves no live job commitment");
            c.Equal(alarm.Id, board.ActiveResponseOn(target.Id, Alarm).Id, "renewal preserves the independent response");
            c.Equal(Fix.FromInt(40), stock.Amount(steel), "renewal before productive work consumes neither cancelled nor pending repair input");
            SimGame restored = factory.Load(game.Save());
            c.Equal(game.Hash(), restored.Hash(), "save preserves named providers and the same board-owned execution");
            c.Equal(current.Id, restored.World.Services.Get<WorkBoard>().ActiveResponseOn(target.Id, Damage).Id, "restore creates no duplicate response order");
            int ticks = Reach(game, restored, () => !ResponseWork.AnyActive(target), 600, "named response completion");
            c.That(!ResponseWork.AnyActive(target), "bounded physical machines finish both incidents");
            c.That(ticks > 12, "travel and three stages take real game time");
            c.Equal(1, target.Get<ContractIncident>().Completed, "renewed damage episode completes once");
            c.Equal(1, target.Get<AlarmIncident>().Completed, "bound completion selects the correct second provider");
            c.Equal(Fix.FromInt(34), stock.Amount(steel), "two earned repair inputs and two destructive bills account exactly; unstarted cancellation consumes nothing");
            c.Equal(Fix.FromInt(4), stock.Amount(water), "fractional destruction and loss use native water units");
            c.Equal(game.Hash(), restored.Hash(), "saved interleaved response continues deterministically");
            c.That(!ResponseWork.AnyActive(foreign), "private foreign incident state never changes");
            c.Equal(target.Def.InitialState.State, target.State, "entity Update settles state after both actual recoveries");
            ResourceFlowPage page = Read(game, cursor); int destroyed = 0, lost = 0;
            foreach (var entry in page.Entries)
            {
                ResourceFlowFact fact = entry.Item;
                if (fact.Kind != ResourceFlowKind.Destroyed && fact.Kind != ResourceFlowKind.Lost) continue;
                if (fact.Kind == ResourceFlowKind.Destroyed) destroyed++; else lost++;
                c.Equal(ResourceEndpointId.ForEntity(target.Id), fact.Endpoint, "negative lifecycle fact names the actual inventory owner");
                c.Equal(fact.Kind == ResourceFlowKind.Destroyed ? new Code("proof.damage") : new Code("proof.leak"), fact.Cause, "authored cause survives private history");
                c.That(!fact.IsTransfer && fact.Source.IsValid && fact.Source.Endpoint == fact.Endpoint && !fact.Destination.IsValid,
                    "destruction retains actual spending receipt provenance without fabricating a receiving transfer");
                c.Equal(fact.Amount, fact.SourceBefore.Value - fact.SourceAfter.Value, "native negative stock delta is verified exactly");
            }
            c.Equal(4, destroyed, "two mixed destructive bills publish four native facts"); c.Equal(2, lost, "two loss effects publish only real loss");
            c.Equal(new BigInteger(Fix.FromInt(4).Raw), page.Totals(steel).DestroyedRaw, "destroyed steel accounting remains resource-scoped");
            c.Equal(new BigInteger(Fix.FromInt(2).Raw), page.Totals(steel).ConsumedRaw, "only the two physically executed repairs publish consumption");
            c.Equal(new BigInteger(Fix.One.Raw), page.Totals(water).LostRaw, "fractional loss accumulates exactly");
            c.Equal(BigInteger.Zero, page.Totals(steel).CreatedRaw, "response fabricates no compensating production");
            c.Equal(ResourceFlowReadStatus.NotOwned, game.Seat(1).View.Feature<ResourceFlowView>().Read(new PlayerId(0), cursor).Status, "incident lifecycle history is company-private");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed disposition and history share the existing codecs");
            Refusals(c, factory); InvalidContent(c); Fault(c);
        }
        private static void Refusals(Check c, GameFactory factory)
        {
            SimGame game = New(factory); Entity target = Spawn(game, "response_site", 16, 18); Tick(game);
            Order order = game.World.Services.Get<WorkBoard>().Post(game.World.Content.Get<ActivityDef>("dispose"), target.Owner, target.Id, 50);
            IInventory stock = target.Get<IInventory>(); ResourceId water = game.World.Content.ResourceId("water");
            ReservationOwner owner = new ReservationOwner("proof.disposition-claim", 1);
            ReservationToken token = stock.ReserveOut(owner, water, Fix.FromInt(7)); ulong before = game.Hash();
            var view = game.Seat(0).View.Feature<WorkView>();
            c.Equal(WorkEffectCodes.NoStock, view.PreviewNodeEffect(order.Id, "damage"), "whole destructive bill protects an existing water promise");
            c.Equal(WorkEffectCodes.NoStock, game.World.Content.Get<IInventoryWorkEffectDefinition>("proof.damage").Apply(game.World, order, order.Plan.Graph.Nodes[0]).Blocker, "current execution refuses before a compatible steel prefix");
            c.Equal(before, game.Hash(), "refused whole bill preserves stock, claim identity and journal");
            c.That(stock.CheckReservedOut(owner, token).Ready, "another owner retains its exact pickup receipt");
            stock.ReleaseOut(owner, token);
            c.Equal(Code.None, view.PreviewNodeEffect(order.Id, "damage"), "same owner reports ready after the real claim is released");
            c.That(ReferenceEquals(game.World.Content.Get<IWorkEffect>("proof.damage"), game.World.Content.Get<IInventoryWorkEffectDefinition>("proof.damage")), "one content instance owns the effect, no parallel registry");
            game.World.System<WorkSystem>().Cancel(game.World, order);
            WorkBoard board = game.World.Services.Get<WorkBoard>();
            Order inspect = board.Post(game.World.Content.Get<ActivityDef>("inspect"), target.Owner, target.Id, 50);
            before = game.Hash();
            c.Equal(WorkEffectCodes.NoPreview, view.PreviewNodeEffect(inspect.Id, "mark"), "provider without preview contract yields explicit unsupported diagnostics");
            c.Equal(before, game.Hash(), "unsupported preview executes no effect");
            game.World.System<WorkSystem>().Cancel(game.World, inspect);
            long cursor = Read(game).Latest;
            c.That(TestKit.Order(game, 0, new PostOrderCommand { Plan = "consume", Target = target.Id }).Accepted, "player uses ordinary engine command for authored consumption work");
            Order consume = board.ActiveOn(target.Id, "activity:consume");
            c.Equal(Fix.FromInt(40), stock.Amount(game.World.Content.ResourceId("steel")), "posting alone never instantly consumes the effect bill");
            Reach(game, null, () => !consume.IsActive, 300, "authored consumption completion");
            c.Equal(OrderState.Done, consume.State, "machine physically completes authored consumption stage");
            c.Equal(Fix.FromInt(39), stock.Amount(game.World.Content.ResourceId("steel")), "consumed effect removes the exact native stock once");
            ResourceFlowPage page = Read(game, cursor);
            c.Equal(1, page.Entries.Count, "one real consumption fact, no fake transfer");
            c.Equal(ResourceFlowKind.Consumed, page.Entries[0].Item.Kind, "consumed effect uses the existing lifecycle payload");
            c.Equal(new Code("proof.consume"), page.Entries[0].Item.Cause, "explicit JSON consumption cause is retained");
        }
        private static void InvalidContent(Check c)
        {
            foreach (string invalid in new[] { "kind", "empty", "fraction", "resource", "bound-source", "effect" })
            {
                var authored = Content.Json.Parse(Json); var effect = authored.Get("inventory_effects")[0];
                if (invalid == "kind") effect.Set("kind", JsonValue.Of("produced"));
                else if (invalid == "empty") effect.Set("quantities", Content.Json.Parse("{}"));
                else if (invalid == "fraction") effect.Get("quantities").Set("steel", JsonValue.Number("0.5"));
                else if (invalid == "resource") { effect.Get("quantities").Remove("water"); effect.Get("quantities").Set("unknown", JsonValue.Number("1.5")); }
                else if (invalid == "bound-source") authored.Get("response_effects")[0].Set("source", JsonValue.Of("damage"));
                else authored.Get("activities")[1].Get("nodes")[1].Set("effect", JsonValue.Of("missing"));
                c.Throws<ContentException>(() => New(PhysicalRecoveryTestContent.Factory(authored.ToString())), "invalid effect/native quantity/reference refuses before play: " + invalid);
            }
        }
        private static void Fault(Check c)
        {
            GameFactory factory = PhysicalRecoveryTestContent.Factory(Json); SimGame game = New(factory); Entity target = Spawn(game, "fault_site", 16, 18); Tick(game);
            c.That(Raise(game, target, Damage, "proof.failure").Accepted, "host submits normal response command to faulty engine-side provider");
            Reach(game, null, () => game.Fault != null, 300, "negative provider removal", awaitingFault: true);
            c.That(game.Fault != null, "partial provider removal faults inside timed engine execution");
            c.That(game.Tick > 6, "fault does not bypass travel and authored stage time");
            c.Throws<InvalidOperationException>(() => game.Save(), "failed disposition cannot checkpoint partially changed stock");
            c.Throws<InvalidOperationException>(() => game.Hash(), "failed disposition cannot present a healthy hash");
            c.Equal(0, target.Get<ContractIncident>().Completed, "fault never fabricates recovery");
        }
        private static void Cargo(Check c)
        {
            GameFactory factory = PhysicalRecoveryTestContent.Factory(Json, cargo: true); SimGame game = New(factory);
            Entity machine = Spawn(game, "loaded_truck", 20, 20), destination = Spawn(game, "water_store", 30, 20);
            IWorker worker = machine.Get<IWorker>(); worker.Held = true; Tick(game);
            IInventory cargo = machine.Get<ITransporter>().Cargo; IHaulExecution hauling = game.World.Services.Get<IHaulExecution>();
            LogisticsView view = game.Seat(0).View.Feature<LogisticsView>(); var rows = new List<CargoReturnView>();
            ResourceId steel = game.World.Content.ResourceId("steel"), water = game.World.Content.ResourceId("water");
            c.Equal(Code.None, view.CopyCargoReturns(machine.Id, rows), "engine owns detached per-line cargo diagnostics");
            c.Equal(2, rows.Count, "mixed hold exposes both native resource lines");
            c.Equal(steel, rows[0].Resource, "first canonical stock line is the unavailable destination resource");
            c.Equal(HaulStatus.NoDestination, rows[0].Return.Status, "no compatible public depot is an explicit blocker");
            c.Equal(HaulStatus.NoCarrier, rows[1].Return.Status, "held machine cannot acquire a physical return");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<LogisticsView>().CopyCargoReturns(machine.Id, new List<CargoReturnView>()), "foreign cargo diagnostics disclose no worker identity");
            worker.Held = false;
            var source = game.World.Services.Get<IWorkerDispatchSource>(); var offers = new List<WorkerDispatchOffer>();
            source.CopyOffers(new PlayerId(0), WorkCandidateMode.IdleOnly, offers);
            var readyReturns = offers.FindAll(offer => offer.Ready && offer.Target == machine.Id && offer.Worker == machine.Id);
            c.Equal(1, readyReturns.Count, "only the actually returnable water line has a ready native dispatch offer");
            if (readyReturns.Count != 1) throw new InvalidOperationException("expected one actual water return offer");
            WorkerDispatchOffer returnOffer = readyReturns[0];
            var otherOwner = new ReservationOwner("proof.cargo-claim", 1);
            ReservationToken otherClaim = cargo.ReserveOut(otherOwner, steel, Fix.One);
            IInventory receiving = destination.Get<IInventory>();
            var capacityOwner = new ReservationOwner("proof.destination-claim", 1);
            ReservationToken capacityClaim = receiving.ReserveIn(capacityOwner, water, Fix.FromInt(100));
            ulong before = game.Hash();
            c.Equal(HaulStatus.NoCapacity, hauling.PreviewReturnCargo(machine.Id, water).Status, "compatible but fully promised destination reports actual capacity blocker");
            game.World.Services.Get<LogisticsService>().PrepareDispatch();
            before = game.Hash();
            c.That(!source.TryAcquire(returnOffer.Identity, returnOffer.Entry, machine.Id, WorkCandidateMode.IdleOnly), "actual earlier offer is revalidated against the later full-capacity promise and acquires no partial job");
            c.Equal(before, game.Hash(), "fully blocked return preserves job IDs, cargo and third-party receipts");
            receiving.ReleaseIn(capacityOwner, capacityClaim); before = game.Hash();
            c.Equal(Code.None, view.CopyCargoReturns(machine.Id, rows), "released worker has current diagnostics");
            CargoReturnView captured = rows[1];
            c.Equal(HaulStatus.Ready, captured.Return.Status, "later compatible line is returnable even when Stock[0] is blocked");
            c.Equal(destination.Id, captured.Return.Destination, "sole engine selector picks actual compatible destination");
            c.Equal(Fix.FromRatio(11, 2), captured.Return.Amount, "native fractional load remains exact");
            c.Equal(HaulStatus.InvalidRequest, hauling.PreviewReturnCargo(machine.Id, new ResourceId(int.MaxValue)).Status, "unknown cargo resource is typed");
            c.Equal(before, game.Hash(), "diagnostic reads never allocate jobs, receipts or stock");
            long cursor = Read(game).Latest; SimGame initial = factory.Load(game.Save());
            c.That(source.TryAcquire(returnOffer.Identity, returnOffer.Entry, machine.Id, WorkCandidateMode.IdleOnly), "shared source acquires the later deliverable line through its actual authored offer identity");
            Entity initialMachine = initial.World.Entities.Get(machine.Id);
            c.That(initial.World.Services.Get<IWorkerDispatchSource>().TryAcquire(returnOffer.Identity, returnOffer.Entry, initialMachine.Id, WorkCandidateMode.IdleOnly), "saved shared source acquires the same real return");
            Job job = game.World.Services.Get<WorkBoard>().Job(worker.Job);
            c.Equal(water, job.Resource, "actual haul chooses water, never rewrites steel as returnable");
            c.That(job.CargoOutReservation.IsValid && job.InReservation.IsValid && !job.OutReservation.IsValid && !job.CargoInReservation.IsValid, "already-loaded return owns exactly cargo-out and destination-in receipts");
            c.Equal(Fix.FromRatio(11, 2), cargo.ReservedOut(water), "actual cargo is protected by the one job owner");
            c.Equal(Fix.FromInt(3), cargo.Amount(steel), "unreturnable cargo is not silently lost");
            c.Equal(Fix.Zero, destination.Get<IInventory>().Amount(water), "acquisition is not instant delivery");
            c.That(!source.TryAcquire(returnOffer.Identity, returnOffer.Entry, machine.Id, WorkCandidateMode.IdleOnly), "busy carrier cannot acquire a duplicate shared return");
            for (int i = 0; i < 4; i++) { Tick(game); Tick(initial); }
            c.That(machine.Get<IMotion>().Status == MoveStatus.Moving, "loaded machine travels over game time");
            c.Equal(game.Hash(), initial.Hash(), "saved pre-acquisition twin uses identical physical execution");
            SimGame mid = factory.Load(game.Save()); c.Equal(game.Hash(), mid.Hash(), "moving loaded return saves its actual receipts and path");
            worker.Held = true; game.World.System<WorkSystem>().Interrupt(game.World, job);
            Entity midMachine = mid.World.Entities.Get(machine.Id); midMachine.Get<IWorker>().Held = true;
            mid.World.System<WorkSystem>().Interrupt(mid.World, mid.World.Services.Get<WorkBoard>().Job(midMachine.Get<IWorker>().Job));
            Tick(game); Tick(mid);
            c.Equal(JobState.Failed, job.State, "interrupted haul closes its existing commitment");
            c.Equal(1, cargo.Reservations.Count, "interruption releases only the current cargo receipt");
            c.That(cargo.CheckReservedOut(otherOwner, otherClaim).Ready, "interruption cannot clear an unrelated cargo promise");
            c.Equal(0, destination.Get<IInventory>().Reservations.Count, "interruption releases the same destination receipt");
            c.Equal(Fix.FromRatio(11, 2), cargo.Amount(water), "interruption retains the real loaded stock");
            c.Equal(Fix.Zero, destination.Get<IInventory>().Amount(water), "interruption cannot invent an unload");
            c.Equal(0, Read(game, cursor).Entries.Count, "travel and interruption publish no fictional transfer prefix");
            c.Equal(game.Hash(), mid.Hash(), "saved moving twin interrupts deterministically");
            SimGame restored = factory.Load(game.Save()); c.Equal(game.Hash(), restored.Hash(), "interrupted cargo saves without orphan promises");
            worker.Held = false; restored.World.Entities.Get(machine.Id).Get<IWorker>().Held = false;
            Reach(game, restored, () => cargo.Amount(water) < Fix.FromRatio(11, 2), 300, "first earned partial unload");
            c.Equal(Fix.FromRatio(9, 2), cargo.Amount(water), "one minute of productive unloading transfers only the authored one-unit quantum");
            c.Equal(Fix.One, receiving.Amount(water), "partial unload deposits actual water before completion");
            Job resumed = game.World.Services.Get<WorkBoard>().Job(worker.Job);
            ResourceReceiptCheck residualOut = cargo.CheckReservedOut(resumed.ReservationOwner, resumed.CargoOutReservation);
            ResourceReceiptCheck residualIn = receiving.CheckReservedIn(game.World.Content, resumed.ReservationOwner, resumed.InReservation);
            c.That(residualOut.Ready && residualIn.Ready && residualOut.Record.Amount == Fix.FromRatio(9, 2) && residualIn.Record.Amount == residualOut.Record.Amount,
                "the same resumed job owns exactly the residual cargo and destination receipts");
            c.Equal(game.Hash(), restored.Hash(), "interleaved interruption twins earn the same first unload quantum");
            restored = factory.Load(game.Save());
            c.Equal(game.Hash(), restored.Hash(), "partial unloading saves stock, original residual receipts and productive timing together");
            Job savedResumed = restored.World.Services.Get<WorkBoard>().Job(resumed.Id);
            c.That(savedResumed.CargoOutReservation.Equals(resumed.CargoOutReservation) && savedResumed.InReservation.Equals(resumed.InReservation),
                "partial-load restore retains exact original token identities, not replacement promises");
            Reach(game, restored, () => !cargo.Amount(water).IsPositive, 300, "interrupted loaded cargo return");
            c.That(!cargo.Amount(water).IsPositive, "normal engine idle behavior replans and finishes the remaining return");
            c.Equal(Fix.FromRatio(11, 2), destination.Get<IInventory>().Amount(water), "the timed unload sequence transfers exact real water stock");
            c.Equal(Fix.FromInt(3), cargo.Amount(steel), "blocked native steel survives every interrupted stage");
            c.Equal(game.Hash(), restored.Hash(), "interrupted saved workflow continues identically");
            c.Equal(Fix.FromRatio(11, 2), captured.Stock, "captured cargo view stays immutable after delivery");
            c.Equal(HaulStatus.Ready, captured.Return.Status, "captured preview is not a live mutable job");
            ResourceFlowPage page = Read(game, cursor);
            c.Equal(6, page.Entries.Count, "five whole quanta and one native fractional remainder publish only actual unloads");
            Fix remaining = Fix.FromRatio(11, 2), received = Fix.Zero;
            foreach (var entry in page.Entries)
            {
                ResourceFlowFact fact = entry.Item;
                c.Equal(ResourceFlowKind.StockTransfer, fact.Kind, "cargo recovery is actual transfer, not fabricated production/loss");
                c.Equal(water, fact.Resource, "every physical unload remains the original water resource");
                c.Equal(ResourceEndpointId.ForEntity(machine.Id), fact.Source.Endpoint, "real cargo endpoint is the source");
                c.Equal(ResourceEndpointId.ForEntity(destination.Id), fact.Destination.Endpoint, "real compatible store is the destination");
                c.Equal(Fix.Min(Fix.One, remaining), fact.Amount, "each quantum respects authored handling and the exact native residual");
                c.Equal(remaining, fact.SourceBefore.Value, "saved partial history continues the original source stock without resetting it");
                c.Equal(received, fact.DestinationBefore, "saved partial history continues the original receiver stock without duplication");
                remaining -= fact.Amount; received += fact.Amount;
                c.Equal(remaining, fact.SourceAfter.Value, "each actual source delta is exact");
                c.Equal(received, fact.DestinationAfter, "each actual receiver delta is exact");
            }
            c.Equal(new BigInteger(Fix.FromRatio(11, 2).Raw), page.Totals(water).MovedRaw, "native fractional transfer facts sum to the complete physical cargo exactly once");
            c.Equal(BigInteger.Zero, page.Totals(steel).LostRaw, "no silent blocked-cargo loss accounting");
            c.Equal(1, cargo.Reservations.Count, "completed return retains only the unrelated owner's live claim");
            c.That(cargo.CheckReservedOut(otherOwner, otherClaim).Ready, "saved return does not steal an unrelated stock promise");
            cargo.ReleaseOut(otherOwner, otherClaim);
            restored.World.Entities.Get(machine.Id).Get<ITransporter>().Cargo.ReleaseOut(otherOwner, otherClaim);
            c.Equal(0, cargo.Reservations.Count, "only the actual owner releases its final claim");
            c.Equal(Code.None, view.CopyCargoReturns(machine.Id, rows), "remaining cargo remains inspectable");
            c.That(rows.Count == 1 && rows[0].Return.Status == HaulStatus.NoDestination, "engine exposes remaining unmet disposition rather than discarding it");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "blocked remainder and settled history restore together");
        }
    }
}
