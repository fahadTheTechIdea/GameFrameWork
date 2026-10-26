using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    internal static class TransporterReserveTests
    {
        internal static void Register()
        {
            TestRunner.Add("Transporter reserves: cargo disposition and canonical admission", Admission);
            TestRunner.Add("Transporter reserves: powered hauling, physical refill and saved cargo", Physical);
        }
        private static string Text(JsonValue value) { var text = new StringBuilder(); value.WriteCanonical(text); return text.ToString(); }
        private static GameFactory Factory(bool extension = false, int fault = 0, Action<JsonValue> edit = null)
        {
            var root = Content.Json.Parse(PointGame.Definitions); root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            root.Get("entity_setups")[2].Set("company", JsonValue.Number("1"));
            var extra = Content.Json.Parse(FrameworkProofContent.Read("powered-hauling.json"));
            if (extension)
                foreach (int index in new[] { 1, 2 })
                {
                    var parts = extra.Get("entities")[index].Get("components"); parts.Remove("transporter");
                    parts.Set("reserve_transporter", Content.Json.Parse("{\"handling\":\"reserve.handling\",\"fault\":" + (index == 1 ? fault : 0) + "}"));
                }
            edit?.Invoke(extra);
            return PointGame.Factory(Text(root), retryProfiles: FrameworkProofContent.Read("haul-retry.json"), flowDefinitions: FrameworkProofContent.Read("endpoints-flows.json"),
                additionalDocuments: new Dictionary<string, string> { ["powered-hauling.json"] = Text(extra) }, startupProfiles: new[] { "powered-hauling.start" });
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 659 }; request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human);
            request.Mods.Add(ResourceFlowsModule.ModuleId); return factory.NewGame(request);
        }
        private static Entity Find(SimGame game, string id)
        { foreach (var entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == id) return entity; throw new InvalidOperationException(id); }
        private static ResourceId Resource(SimGame game, string name) => game.World.Content.ResourceId(name);
        private static CargoDispositionSnapshot Read(SimGame game, string worker = "reserve.truck")
        {
            Code code = game.Seat(0).View.Feature<LogisticsView>().ReadCargoDisposition(Find(game, worker).Id, out var value);
            if (!code.IsNone) throw new InvalidOperationException(code.ToString()); return value;
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 200 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done())
            {
                var truck = Find(game, "reserve.truck"); var board = game.World.Services.Get<WorkBoard>(); var job = board.Job(truck.Get<IWorker>().Job);
                throw new InvalidOperationException(stage + " timeout tick=" + game.Tick + " position=" + truck.Get<PointPresence>().Position +
                    " fuel=" + truck.Get<IInventory>().Amount(Resource(game, "fuel")) + " job=" + job?.State + "/" + job?.Blocker);
            }
        }
        private static CommandResult Stock(SimGame game, LogisticsDemandSettings settings, int seat = 0)
            => TestKit.Order(game, seat, new ConfigureStockPolicyCommand { Store = Find(game, "reserve.truck").Id, Resource = Resource(game, "fuel"), Change = LogisticsDemandChange.Replace(settings) });
        private static CommandResult Outlet(SimGame game, bool enabled, Bill reserve)
            => TestKit.Order(game, 0, new ConfigureOutletPolicyCommand { Source = Find(game, "reserve.truck").Id,
                Change = OutletPolicyChange.Replace(new OutletPolicySettings(enabled, reserve, 0, null, LogisticsReserveAccess.Protected)) });
        private static HaulPreview Preview(SimGame game)
            => game.Seat(0).View.Feature<LogisticsView>().PreviewHaul(Find(game, "source").Id, Find(game, "destination").Id, Resource(game, "supply"), Fix.FromInt(8));
        private static void Admission(Check c)
        {
            var factory = Factory(); var game = New(factory); var truck = Find(game, "reserve.truck"); var fuel = Resource(game, "fuel");
            var view = game.Seat(0).View.Feature<LogisticsView>(); ulong before = game.Hash(); var initial = Read(game);
            c.Equal(Fix.FromInt(4), initial.Retained.AmountOf(fuel), "actual authored desired/reserve protects onboard operating stock");
            c.That(initial.Committed.IsEmpty && initial.Unassigned.IsEmpty && !initial.HasResidualCargo, "fuelled idle vehicle has no fabricated leftover shipment");
            c.Equal(ResourceEndpointId.ForEntity(truck.Id), initial.Endpoint, "snapshot reports actual inventory endpoint");
            c.Equal(truck.Id, initial.Worker, "snapshot identifies actual carrier");
            var preview = Preview(game); c.Equal(HaulStatus.Ready, preview.Status, "fuelled carrier is eligible for fresh hauling");
            c.Equal(truck.Id, preview.Carrier, "existing player worker preference still selects the fuelled truck");
            c.Equal(Fix.FromInt(4), preview.Amount, "real reserve occupies four units of cargo capacity");
            c.Equal(before, game.Hash(), "cargo/read/admission projections change no balances/jobs or identities");
            c.Throws<NotSupportedException>(() => ((IList<ResourceAmount>)initial.Retained.Lines).Clear(), "retained quantities are immutable");
            c.Equal(Codes.NotOwned, game.Seat(1).View.Feature<LogisticsView>().ReadCargoDisposition(truck.Id, out var hidden), "foreign private disposition refuses");
            c.That(hidden == null, "foreign observer receives no stock/policy facts");
            c.Equal(Codes.NotAllowed, view.ReadCargoDisposition(Find(game, "source").Id, out _), "non-transporter has no fabricated cargo");
            c.Equal(Codes.NotFound, view.ReadCargoDisposition(EntityId.None, out _), "unknown carrier refuses");
            c.Equal(Fix.FromInt(4), game.World.Services.Get<IHaulExecution>().ReadCargoDisposition(truck.Id).Retained.AmountOf(fuel), "public hauling contract exposes same detached disposition");
            var owner = new ReservationOwner("proof.shipment", 1); var cargo = truck.Get<IInventory>(); var token = cargo.ReserveOut(owner, fuel, Fix.One);
            var promised = Read(game);
            c.Equal(Fix.One, promised.Committed.AmountOf(fuel), "outbound promised stock remains cargo despite operating policy");
            c.Equal(Fix.FromInt(3), promised.Retained.AmountOf(fuel), "retention never borrows committed quantity");
            c.That(promised.HasResidualCargo, "promised shipment excludes fresh carrier use");
            c.That(Preview(game).Carrier != truck.Id, "canonical admission excludes committed inventory");
            c.That(cargo.CheckReservedOut(owner, token).Ready, "disposition read leaves native receipt intact"); cargo.ReleaseOut(owner, token);
            var changed = new LogisticsDemandSettings(false, Fix.Zero, Fix.Zero, Fix.FromInt(4), 200, LogisticsReserveAccess.Protected);
            c.Equal(Code.None, view.PreviewStockPolicy(truck.Id, fuel, LogisticsDemandChange.Replace(changed)), "private mobile inventory policy uses canonical engine preview");
            c.That(Stock(game, changed).Accepted, "normal player command changes private mobile stock policy");
            c.That(Outlet(game, false, Bill.Empty).Accepted, "player explicitly removes operating retention while disabling export");
            c.Equal(Fix.FromInt(4), Read(game).Unassigned.AmountOf(fuel), "disabled exports do not invent a reserve for old surplus");
            c.That(Read(game).HasResidualCargo, "unassigned surplus still prevents new shipment use");
            c.That(Preview(game).Carrier != truck.Id, "surplus cannot bypass current carrier admission");
            c.Equal(Fix.FromInt(4), initial.Retained.AmountOf(fuel), "captured disposition does not follow policy changes");
            c.Equal(Codes.NotOwned, game.Seat(1).View.Feature<LogisticsView>().PreviewStockPolicy(truck.Id, fuel, LogisticsDemandChange.Replace(changed)), "foreign stock preview refuses identically");
            c.That(!Stock(game, changed, 1).Accepted, "foreign stock command cannot configure another company's fuel reserve");
            var invalid = new LogisticsDemandSettings(true, Fix.Zero, Fix.FromInt(9), Fix.FromInt(9), 1, LogisticsReserveAccess.Protected);
            Code refusal = view.PreviewStockPolicy(truck.Id, fuel, LogisticsDemandChange.Replace(invalid)); c.That(!refusal.IsNone, "invalid mobile stock maximum refuses without capacity fallback");
            var result = Stock(game, invalid); c.Equal(refusal, result.Refusal, "stock command and management preview use same refusal");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "current policies/disposition restore without seeding profile defaults again");
            c.Equal(StateCodecTests.ExpectedLogisticsSchema, game.World.Services.Get<LogisticsService>().StateSchemaVersion, "logistics semantics are versioned");
            c.Equal(StateCodecTests.ExpectedWorkSystemSchema, game.World.System<WorkSystem>().StateSchemaVersion, "work interruption uses current disposition");
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "engine change has explicit identity");
            PublicProviders(c); Surplus(c);
        }
        private static void PublicProviders(Check c)
        {
            var factory = Factory(extension: true); var game = New(factory);
            c.Equal(Fix.FromInt(4), Read(game).Retained.AmountOf(Resource(game, "fuel")), "independent public transporter uses same inventory and retention contracts");
            c.Equal(Find(game, "reserve.truck").Id, Preview(game).Carrier, "public transporter is admitted without concrete type lookup");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "stateless public transporter needs no extra stock/codec");
            foreach (int fault in new[] { 1, 2 })
                c.Throws<InvalidOperationException>(() => Read(New(Factory(extension: true, fault: fault))), "mutating/missing public cargo fails explicitly " + fault);
            var external = New(Factory(edit: root =>
            {
                root.Get("logistics_policies")[0].Set("stock", JsonValue.NewObject()); var parts = root.Get("entities")[1].Get("components");
                parts.Remove("logistics_policy"); parts.Set("contract_outlet_policy", Content.Json.Parse("{\"profile\":\"reserve.truck.policy\"}"));
            }));
            c.Equal(Fix.FromInt(4), Read(external).Retained.AmountOf(Resource(external, "fuel")), "public-only outlet reserve protects operating stock with no native demand seed");
            c.Equal(Find(external, "reserve.truck").Id, Preview(external).Carrier, "no native outlet cast is needed for carrier selection");
        }
        private static void Surplus(Check c)
        {
            var game = New(Factory(edit: root => root.Get("entities")[1].Get("components").Get("storage").Set("initial_stock", Content.Json.Parse("{\"fuel\":4,\"supply\":1}"))));
            c.Equal(Fix.One, Read(game).Unassigned.AmountOf(Resource(game, "supply")), "non-retained onboard material remains ordinary residual cargo");
            c.That(Preview(game).Carrier != Find(game, "reserve.truck").Id, "reserve exemption is not an all-stock exemption");
        }
        private static void Physical(Check c)
        { Powered(c, false); Powered(c, true); }
        private static void Powered(Check c, bool extension)
        {
            var factory = Factory(extension: extension); var game = New(factory); var isolated = New(factory);
            var truck = Find(game, "reserve.truck"); var fuel = Resource(game, "fuel"); var supply = Resource(game, "supply"); var board = game.World.Services.Get<WorkBoard>();
            var result = TestKit.Order(game, 0, new AddRouteCommand { From = Find(game, "source").Id, To = Find(game, "destination").Id,
                Resource = supply, Settings = new LogisticsRouteSettings(true, Fix.FromInt(4), 100) });
            c.That(result.Accepted, "player authors ordinary route; engine chooses fuelled carrier");
            Job shipment = board.Job(truck.Get<IWorker>().Job); c.That(shipment?.IsHaul == true, "existing native job owns powered haul");
            if (shipment == null) throw new InvalidOperationException("powered shipment was not admitted");
            c.That(TestKit.Order(game, 0, new ConfigureRouteCommand { Route = checked((int)result.Arg0), Settings = new LogisticsRouteSettings(false, Fix.FromInt(4), 100) }).Accepted, "player disables future dispatch without cancelling acquired shipment");
            Reach(game, () => shipment.ToilIndex == 2 && shipment.Loaded == Fix.FromInt(4), "funded load");
            c.Equal(Fix.FromInt(4), Read(game).Committed.AmountOf(supply), "loaded payload is protected native cargo, not operating reserve");
            var receipt = shipment.CargoOutReservation; var inlet = shipment.InReservation; var worker = shipment.Worker; long age = shipment.CreatedTick;
            Reach(game, () => shipment.State == JobState.Paused && shipment.Blocker == WorkSystem.BlockerWorkerUnavailable, "loaded fuel shortage");
            var position = truck.Get<PointPresence>().Position;
            c.That(position > Fix.Zero && position < Fix.FromInt(10), "worker physically strands between endpoints");
            c.Equal(Fix.Zero, truck.Get<IInventory>().Amount(fuel), "motion spends only real unreserved propulsion stock");
            c.Equal(Fix.FromInt(4), shipment.Loaded, "fuel shortage cannot spend protected payload");
            c.Equal(receipt, shipment.CargoOutReservation, "cargo receipt survives powered wait");
            c.Equal(inlet, shipment.InReservation, "destination capacity remains reserved");
            c.Equal(worker, shipment.Worker, "original worker stays assigned");
            c.Equal(age, shipment.CreatedTick, "fuel wait preserves original shipment age");
            c.Equal(Fix.Zero, Find(game, "destination").Get<IInletPort>().Amount(supply), "no remote delivery while stranded");
            var courier = Find(game, "reserve.courier"); var refuel = board.Job(courier.Get<IWorker>().Job);
            c.That(refuel?.IsHaul == true && refuel.Resource == fuel && refuel.To == truck.Id, "existing stock demand dispatches ordinary standby courier to mobile worker");
            c.Equal(LogisticsService.StockDemandScope, refuel.HaulDemand.Scope, "refueling keeps existing native stock provenance");
            c.Equal((long)truck.Id.Value, refuel.HaulDemand.Identity, "native target demand owns refuel identity");
            c.That(refuel.Loaded.IsPositive, "fuel exists as physical courier cargo");
            var courierFacts = Read(game, "reserve.courier");
            c.Equal(refuel.Loaded, courierFacts.Committed.AmountOf(fuel), "same-resource refuel cargo and courier's own fuel stay distinguished by receipts");
            c.That(courierFacts.Retained.AmountOf(fuel).IsPositive, "courier retains its own unreserved operating supply while carrying that resource");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "loaded shortage/refuel checkpoint restores exactly");
            var demands = new List<LogisticsDemandView>(); game.Seat(0).View.Feature<LogisticsView>().CopyDemands(demands);
            bool refill = false; foreach (var demand in demands) if (demand.Destination == truck.Id && demand.Resource == fuel && demand.Replenishing) refill = true;
            c.That(refill, "authoritative stock hysteresis reports active mobile refill");
            Reach(game, () => !shipment.IsLive, "saved powered delivery", twin);
            c.Equal(JobState.Done, shipment.State, "original native job resumes and completes");
            c.Equal(Fix.FromInt(4), Find(game, "destination").Get<IInletPort>().Amount(supply), "same four payload units arrive and unload once");
            c.Equal(Fix.FromInt(16), Find(game, "source").Get<IOutletPort>().Amount(supply), "actual source supplied payload");
            c.Equal(10L, MotionExecutionContracts.Read(game.World, truck).Operating.Providers[0].Exposure.ElapsedTicks, "fuel wait never resets earned active motion exposure");
            c.Equal(game.Hash(), twin.Hash(), "powered loaded wait/refill/resume hashes match");
            Reach(game, () => truck.Get<IInventory>().Amount(fuel) == Fix.FromInt(4) && truck.Get<IWorker>().Job == 0 && courier.Get<IWorker>().Job == 0, "idle fuel reserve restoration", twin);
            c.Equal(Fix.FromInt(4), Read(game).Retained.AmountOf(fuel), "finished vehicle keeps actual requested reserve");
            c.That(!Read(game).HasResidualCargo, "restored fuel is not leftover haul cargo");
            c.Equal(Fix.FromInt(10), truck.Get<PointPresence>().Position, "idle fuel never triggers an invented return-cargo trip");
            var heldFuel = truck.Get<IInventory>().Amount(fuel); long exposure = MotionExecutionContracts.Read(game.World, truck).Operating.Providers[0].Exposure.ElapsedTicks;
            for (int i = 0; i < 4; i++) { Tick(game); Tick(twin); Tick(isolated); }
            c.Equal(heldFuel, truck.Get<IInventory>().Amount(fuel), "idle reserve spends no fuel");
            c.Equal(exposure, MotionExecutionContracts.Read(game.World, truck).Operating.Providers[0].Exposure.ElapsedTicks, "idle reserve earns no exposure");
            c.Equal(game.Hash(), twin.Hash(), "saved stock latch/route/cargo/exposure continuation matches");
            c.Equal(Fix.FromInt(20), Find(game, "source").Get<IOutletPort>().Amount(supply) + truck.Get<IInventory>().Amount(supply) + courier.Get<IInventory>().Amount(supply) + Find(game, "destination").Get<IInletPort>().Amount(supply), "native payload quantities conserve through physical replenishment");
            BigInteger burned = 0; foreach (var fact in game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).Aggregates)
                if (fact.Resource == fuel && fact.Kind == ResourceFlowKind.Consumed && fact.Cause == OperatingInputCodes.Cause) burned += fact.AmountRaw;
            Fix remaining = Find(game, "reserve.source").Get<IInventory>().Amount(fuel) + truck.Get<IInventory>().Amount(fuel) + courier.Get<IInventory>().Amount(fuel);
            c.Equal(new BigInteger(Fix.FromInt(84).Raw - remaining.Raw), burned, "existing resource journal accounts for every burned unit without a fuel ledger");
            c.Equal(Fix.FromInt(4), Find(isolated, "reserve.truck").Get<IInventory>().Amount(Resource(isolated, "fuel")), "interleaved session receives no refuel or consumption");
            c.Equal(Fix.Zero, Find(isolated, "destination").Get<IInletPort>().Amount(Resource(isolated, "supply")), "interleaved session receives no shipment");
        }
    }
}
