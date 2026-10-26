using System;
using System.Collections.Generic;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Testing;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class HaulTripPolicyTests
    {
        internal static void Register()
        {
            TestRunner.Add("Haul trip policy: authored contracts, budgets and atomic private admission", c => { Admission(c); RetainedAdmission(c); });
            TestRunner.Add("Haul trip policy: physical funding, return commitments and saved continuation", c => { Physical(c); RetainedPhysical(c); });
        }
        private static string Text(JsonValue root) { var text = new StringBuilder(); root.WriteCanonical(text); return text.ToString(); }
        internal static GameFactory Factory(bool tile = false, string fault = null, bool returning = false,
            Action<JsonValue> edit = null, string rules = null, Action<JsonValue> editPointScenario = null,
            IReadOnlyList<GameModule> pointModules = null, string pointDeclarations = null,
            IReadOnlyDictionary<string, string> additionalDocuments = null)
        {
            var powered = Content.Json.Parse(FrameworkProofContent.Read("powered-hauling.json"));
            var policy = powered.Get("logistics_policies")[0];
            policy.Get("stock").Get("fuel").Set("minimum", JsonValue.Number("0"));
            policy.Get("stock").Get("fuel").Set("desired", JsonValue.Number(returning ? "4" : "12"));
            policy.Get("stock").Get("fuel").Set("maximum", JsonValue.Number("12"));
            policy.Get("outlet").Get("reserve").Set("fuel", JsonValue.Number("2"));
            var parts = powered.Get("entities")[1].Get("components");
            parts.Get("storage").Set("capacity_policy", JsonValue.Of("fixture.count.40"));
            parts.Get("storage").Get("initial_stock").Set("fuel", JsonValue.Number("12"));
            parts.Set("haul_trip_policy", Content.Json.Parse("{\"profile\":\"trip.funded\"}"));
            if (fault != null) parts.Get("point_motion").Set("area_fault", JsonValue.Of(fault));
            if (returning) StartupContentAuthoring.Position(powered, "reserve.truck", JsonValue.Number("4"));
            powered.Get("work_policies")[1].Set("enabled", JsonValue.Of(false));
            var documents = new Dictionary<string, string> { ["haul-trip-policy.json"] = rules ?? FrameworkProofContent.Read("haul-trip-policy.json") };
            if (additionalDocuments != null) foreach (var document in additionalDocuments) documents.Add(document.Key, document.Value);
            if (!tile)
            {
                edit?.Invoke(powered);
                var root = Content.Json.Parse(PointGame.Definitions); root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
                root.Get("entity_setups")[2].Set("company", JsonValue.Number("1"));
                editPointScenario?.Invoke(root.Get("scenarios")[0]);
                var modules = new List<GameModule>(); var declarations = JsonValue.NewArray();
                if (returning)
                {
                    modules.Add(new LogisticsRecoveryPolicyModule());
                    declarations.Add(Content.Json.Parse("{\"id\":\"sim.logistics-recovery\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[\"logistics-recovery.json\"]}"));
                    root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(LogisticsRecoveryPolicyModule.ModuleId));
                    root.Get("scenarios")[0].Get("settings").Set(LogisticsRecoveryPolicyModule.ModuleId, Content.Json.Parse("{\"profile\":\"recovery.high\"}"));
                }
                if (pointModules != null) foreach (var module in pointModules) modules.Add(module);
                if (pointDeclarations != null) { var extra = Content.Json.Parse(pointDeclarations); for (int i = 0; i < extra.Count; i++) declarations.Add(extra[i]); }
                documents.Add("trip-powered.json", Text(powered));
                var workRules = Content.Json.Parse(FrameworkProofContent.Read("work-retry.json"));
                workRules.Set("work_retry_profiles", Content.Json.Parse(FrameworkProofContent.Read("haul-retry.json")).Get("work_retry_profiles").Clone());
                return PointGame.Factory(Text(root), retryProfiles: Text(workRules),
                    flowDefinitions: FrameworkProofContent.Read("endpoints-flows.json"), additionalDocuments: documents,
                    extraModules: modules, extraDeclarations: declarations.ToString(),
                    extraDocuments: returning ? new Dictionary<string, string> { ["logistics-recovery.json"] = FrameworkProofContent.Read("logistics-recovery.json") } : null, startupProfiles: new[] { "powered-hauling.start" });
            }
            if (editPointScenario != null || pointModules != null || pointDeclarations != null || additionalDocuments != null)
                throw new ArgumentException("point fixture declarations cannot be applied to a tile fixture");
            var native = Content.Json.Parse(FrameworkProofContent.Read("tile-service-docks.json"));
            var nativeParts = native.Get("entities")[2].Get("components");
            nativeParts.Set("storage", parts.Get("storage").Clone()); nativeParts.Set("haul_trip_policy", parts.Get("haul_trip_policy").Clone());
            nativeParts.Set("logistics_policy", parts.Get("logistics_policy").Clone());
            var operating = parts.Get("operating_inputs").Clone(); operating.Get("states").Set("disabled", JsonValue.Of("stopped"));
            nativeParts.Set("operating_inputs", operating);
            native.Set("units", powered.Get("units").Clone()); native.Set("resources", powered.Get("resources").Clone());
            policy.Get("states").Set("disabled", JsonValue.Of("stopped"));
            var policies = Content.Json.Parse("[]"); policies.Add(policy); native.Set("logistics_policies", policies);
            edit?.Invoke(native);
            var definitions = Content.Json.Parse(Colony.Defs); definitions.Get("tile_spaces")[0].Set("height", JsonValue.Number("48"));
            return Colony.Factory(definitions: Text(definitions), additionalDefinitions: Text(native), additionalDocuments: documents);
        }
        private static SimGame Start(GameFactory factory, bool tile = false)
        {
            var request = new NewGameRequest { Scenario = tile ? "colony" : "points", Seed = 733 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); request.Mods.Add(ResourceFlowsModule.ModuleId);
            return factory.NewGame(request);
        }
        private static Entity Find(SimGame game, string id)
        { foreach (var entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == id) return entity; throw new InvalidOperationException(id); }
        private static Entity Actor(Check c, SimGame game, bool tile)
        {
            if (!tile) return Find(game, "reserve.truck");
            var workers = new List<Entity>(); foreach (var entity in game.World.Entities.All) if (entity.Has<IWorker>()) workers.Add(entity);
            foreach (var entity in workers) c.That(TestKit.Order(game, entity.Owner.Value, new MoveVehicleCommand { Vehicle = entity.Id, To = entity.Get<ITilePresence>().Tile }).Accepted,
                "ordinary player orders hold unrelated native workers");
            Spawn(game, "facility.source", 20, 20); Spawn(game, "facility.destination", 20, 30);
            var actor = Spawn(game, "facility.worker", 19, 18); Tick(game); return actor;
        }
        private static HaulPreview Preview(SimGame game, bool tile = false) => game.Seat(0).View.Feature<LogisticsView>().PreviewHaul(
            Find(game, tile ? "facility.source" : "source").Id, Find(game, tile ? "facility.destination" : "destination").Id,
            game.World.Content.ResourceId(tile ? "steel" : "supply"), Fix.FromInt(4));
        private static IHaulExecution Haul(SimGame game) => game.World.Services.Get<IHaulExecution>();
        private static void Control(Check c, SimGame game, Entity actor, Fix rate) => c.That(TestKit.Order(game, 0,
            new ConfigureOperationCommand { Entity = actor.Id, Operation = new Code("travel"), Enabled = true, Target = rate }).Accepted, "actual motion command changes trip cost");
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 300 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done()) throw new InvalidOperationException(stage + " timeout at game time " + game.Clock.TotalGameTime);
        }
        private static void Admission(Check c)
        {
            c.Throws<ContentException>(() => Start(Factory(rules: "{\"haul_trip_profiles\":[{\"id\":\"trip.funded\",\"enabled\":true}]}")), "missing JSON reserve intent has no fabricated boolean");
            c.Throws<ContentException>(() => Start(Factory(edit: root => root.Get("entities")[1].Get("components").Get("haul_trip_policy").Set("profile", JsonValue.Of("missing")))), "missing trip profile fails master content resolution");
            c.Throws<ContentException>(() => Start(Factory(edit: root => root.Get("entities")[1].Get("components").Remove("point_motion"))), "trip capability requires actual motion metadata");
            c.Throws<ContentException>(() => Start(Factory(edit: root => root.Get("entities")[1].Get("components").Remove("logistics_policy"))), "reserve preservation requires actual selected outlet policy");
            var factory = Factory(); var game = Start(factory); var actor = Actor(c, game, false); var fuel = game.World.Content.ResourceId("fuel");
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "authored trip command/composition has current identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "one current trip policy codec, no old reader");
            var view = game.Seat(0).View.Feature<LogisticsView>(); ulong before = game.Hash(); var preview = Preview(game);
            c.That(preview.Status == HaulStatus.Ready && preview.Carrier == actor.Id, "funded selected carrier admits through same haul selector");
            c.Equal(10L, preview.Travel.Value.RemainingTicks.Value, "selected duration is full physical trip");
            c.Equal(Fix.FromInt(5), preview.TripBudget.Operating.RequiredInputs.AmountOf(fuel), "existing cumulative native operating calculation funds full travel");
            c.Equal(Fix.FromInt(2), preview.TripBudget.ReserveFloor.AmountOf(fuel), "actual outlet floor is read, never copied to trip settings");
            c.Equal(Fix.FromInt(7), preview.TripBudget.RequiredStock.AmountOf(fuel), "trip plus existing floor shares actual inventory evaluator");
            c.Equal(before, game.Hash(), "trip budget creates no jobs/stock/claims/time changes");
            c.Equal(Code.None, view.ReadHaulTripPolicy(actor.Id, out var settings), "owned policy immutable projection");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<LogisticsView>().ReadHaulTripPolicy(actor.Id, out var foreign), "foreign policy is private");
            c.That(foreign == null, "foreign read has no policy facts");
            var disabled = new HaulTripSettings(settings.Profile, false, true);
            c.Equal(Code.None, view.PreviewHaulTripPolicy(actor.Id, disabled), "management preview admits same explicit settings");
            var command = TestKit.Order(game, 0, new ConfigureHaulTripPolicyCommand { Worker = actor.Id, Settings = disabled });
            c.That(command.Accepted && Preview(game).Blocker == HaulTripPolicyCodes.Disabled, "normal command disables fresh hauling, not the clock or unrelated work");
            c.Equal(settings.Enabled, true, "retained settings remain detached after configuration");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "changed policy has one durable codec");
            c.That(TestKit.Order(game, 1, new ConfigureHaulTripPolicyCommand { Worker = actor.Id, Settings = settings }).Refusal == Codes.NotFound, "foreign configure shares private refusal");
            c.That(TestKit.Order(game, 0, new ConfigureHaulTripPolicyCommand { Worker = actor.Id, Settings = settings }).Accepted, "normal player re-enables trip policy");
            Control(c, game, actor, Fix.FromRatio(1, 4)); var unfunded = Preview(game); before = game.Hash();
            c.That(unfunded.Status == HaulStatus.PolicyBlocked && unfunded.Blocker == OperatingInputCodes.Inputs && unfunded.TripBudget == null,
                "slower controlled travel refuses unfunded candidate without private incomplete budget");
            c.Equal(0, Haul(game).AcquireHaul(new PlayerId(0), Find(game, "source").Id, Find(game, "destination").Id,
                game.World.Content.ResourceId("supply"), Fix.FromInt(4), 100), "fresh acquisition cannot use older funded preview");
            c.Equal(before, game.Hash(), "unfunded acquire allocates no job or native promises");
            Control(c, game, actor, Fix.One);
            c.That(TestKit.Order(game, 0, new ConfigureOutletPolicyCommand { Source = actor.Id,
                Change = OutletPolicyChange.Reserve(fuel, Fix.FromInt(8), true) }).Accepted, "existing reserve command changes authoritative floor");
            c.Equal(HaulTripPolicyCodes.Reserve, Preview(game).Blocker, "trip does not deplete changed actual outlet floor");
            var noFloor = new HaulTripSettings("trip.no-floor", true, false);
            c.That(TestKit.Order(game, 0, new ConfigureHaulTripPolicyCommand { Worker = actor.Id, Settings = noFloor }).Accepted, "explicit rule can omit floor preservation, without removing stock policy");
            var available = Preview(game); c.That(available.Status == HaulStatus.Ready && available.TripBudget.ReserveFloor.IsEmpty, "explicit no-floor profile still funds physical movement");
            var inventory = actor.Get<IInventory>(); var owner = new ReservationOwner("test.trip.claim", 1);
            var token = inventory.ReserveOut(owner, fuel, Fix.FromInt(8)); c.That(token.IsValid, "real existing reservation removes funding stock");
            c.Equal(OperatingInputCodes.Inputs, HaulTripPolicyContracts.CheckTrip(game.World, actor, available.Travel.Value).Blocker,
                "existing horizon excludes real promised stock from future movement funding");
            c.That(Preview(game).Status != HaulStatus.Ready, "existing committed-cargo admission also refuses fresh haul without borrowing claims");
            inventory.ReleaseOut(owner, token);
            var unsupported = Start(Factory(fault: "route-duration-unsupported"));
            c.Equal(MotionArrivalCodes.Unsupported, Preview(unsupported).Blocker, "required unknown duration refuses rather than guesses fuel");
            var range = HaulTripPolicyContracts.CheckTrip(game.World, actor, new MotionTravelEstimate(long.MaxValue, Code.None));
            c.That(range.Blocker == OperatingInputCodes.Range && range.RequiredStock.IsEmpty, "overflow returns no partial funding bill or narrowed duration");
            c.Throws<ArgumentException>(() => HaulTripPolicyContracts.CheckTrip(game.World, actor, default), "malformed unknown duration is not silently replaced");
            var zero = HaulTripPolicyContracts.CheckTrip(game.World, actor, new MotionTravelEstimate(0, Code.None));
            c.That(zero.Blocker.IsNone && zero.Operating.RequiredInputs.IsEmpty, "proven zero movement requires no guessed minimum fuel interval");
            var emergencyFactory = Factory(); var emergencyGame = Start(emergencyFactory); var emergencyActor = Find(emergencyGame, "reserve.truck");
            var floor = new Bill(new[] { new ResourceAmount(emergencyGame.World.Content.ResourceId("fuel"), Fix.FromInt(8)) });
            c.That(TestKit.Order(emergencyGame, 0, new ConfigureOutletPolicyCommand { Source = emergencyActor.Id,
                Change = OutletPolicyChange.Replace(new OutletPolicySettings(true, floor, 0, null, LogisticsReserveAccess.Protected)) }).Accepted,
                "normal stock command owns emergency reserve access");
            c.Equal(HaulTripPolicyCodes.Reserve, HaulTripPolicyContracts.CheckTrip(emergencyGame.World, emergencyActor,
                new MotionTravelEstimate(10, Code.None), emergency: true).Blocker, "emergency cannot bypass protected operating floors");
            c.That(TestKit.Order(emergencyGame, 0, new ConfigureOutletPolicyCommand { Source = emergencyActor.Id,
                Change = OutletPolicyChange.Replace(new OutletPolicySettings(true, floor, 0, null, LogisticsReserveAccess.EmergencyOnly)) }).Accepted,
                "explicit existing reserve rule permits emergency use");
            c.Equal(Code.None, HaulTripPolicyContracts.CheckTrip(emergencyGame.World, emergencyActor,
                new MotionTravelEstimate(10, Code.None), emergency: true).Blocker, "authorized emergency releases only existing reserve floor");
            c.Equal(OperatingInputCodes.Inputs, HaulTripPolicyContracts.CheckTrip(emergencyGame.World, emergencyActor,
                new MotionTravelEstimate(40, Code.None), emergency: true).Blocker, "authorized emergency never creates fuel");
            var choices = Start(Factory(edit: root =>
            {
                root.Get("work_policies")[1].Set("enabled", JsonValue.Of(true));
                root.Get("entities")[2].Get("components").Set("haul_trip_policy", Content.Json.Parse("{\"profile\":\"trip.funded\"}"));
                root.Get("logistics_policies")[1].Get("outlet").Get("reserve").Remove("fuel");
            }));
            Control(c, choices, Find(choices, "reserve.truck"), Fix.FromRatio(1, 4)); var choice = Preview(choices);
            c.That(choice.Status == HaulStatus.Ready && choice.Carrier == Find(choices, "reserve.courier").Id && choice.TripBudget.Blocker.IsNone,
                "same selector chooses actual affordable alternative instead of rejecting or fabricating fuel for first carrier");
        }
        private static void Physical(Check c)
        {
            foreach (bool tile in new[] { false, true })
            {
                var factory = Factory(tile); var game = Start(factory, tile); var actor = Actor(c, game, tile); var fuel = game.World.Content.ResourceId("fuel");
                var preview = Preview(game, tile); c.That(preview.Status == HaulStatus.Ready && preview.TripBudget.Blocker.IsNone, "real native/public trip is funded before native claims");
                var inputs = preview.TripBudget.Operating.RequiredInputs.AmountOf(fuel); var initial = actor.Get<IInventory>().Amount(fuel);
                int id = Haul(game).AcquireHaul(new PlayerId(0), preview.Source, preview.Destination, preview.Resource, preview.Amount, 100);
                c.That(id > 0, "same acquisition physically starts admitted trip"); var job = game.World.Services.Get<WorkBoard>().Job(id);
                Reach(game, () => job.ToilIndex >= 2, "physical pickup");
                Reach(game, () => actor.Get<IOperatingInputs>(new Code("propulsion")).ReadExposure().ElapsedTicks % 2 == 1, "real fractional native input credit");
                var credit = HaulTripPolicyContracts.CheckTrip(game.World, actor, new MotionTravelEstimate(1, Code.None));
                c.That(credit.Operating.RequiredInputs.IsEmpty && credit.Blocker.IsNone,
                    "next interval reuses actually earned native quantization credit, not rounded next-demand multiplication");
                var twin = factory.Load(game.Save());
                game.Clock.Pause(); twin.Clock.Pause(); ulong paused = game.Hash(); long time = game.Clock.StepIndex;
                game.Update(); twin.Update(); c.That(game.Hash() == paused && game.Clock.StepIndex == time, "single game clock freezes funded loaded trip");
                game.Clock.Resume(); twin.Clock.Resume(); Reach(game, () => !job.IsLive, "funded delivery and departure", twin);
                c.Equal(inputs, initial - actor.Get<IInventory>().Amount(fuel), "actual accepted native/public consumption equals prior existing trip budget");
                c.That(actor.Get<IInventory>().Amount(fuel) >= preview.TripBudget.ReserveFloor.AmountOf(fuel), "actual completed trip preserves configured floor");
                c.Equal(Fix.FromInt(4), game.World.Entities.Get(preview.Destination).Get<IInventory>().Amount(preview.Resource), "real timed shipment arrives exactly once");
                c.Equal(game.Hash(), twin.Hash(), "loaded policy/resource/job state continues identically after save");
                c.Equal(inputs, preview.TripBudget.Operating.RequiredInputs.AmountOf(fuel), "retained budget does not follow live fuel expenditure");
            }
            var returnFactory = Factory(returning: true); var returning = Start(returnFactory); var machine = Find(returning, "reserve.truck");
            returning.World.Services.Get<LogisticsService>().PrepareDispatch();
            var resource = returning.World.Content.ResourceId("fuel"); var proposal = Haul(returning).PreviewReturnCargo(machine.Id, resource);
            c.That(proposal.Status == HaulStatus.Ready && proposal.TripBudget.ReturnCommitment.AmountOf(resource) == proposal.Amount,
                "same-resource return claim is included before promises, never borrowed as fuel");
            c.Equal(Fix.FromInt(12), proposal.TripBudget.RequiredStock.AmountOf(resource), "four-interval trip plus eight cargo plus two floor is exactly funded");
            var blocked = HaulTripPolicyContracts.CheckTrip(returning.World, machine, new MotionTravelEstimate(6, Code.None), resource, proposal.Amount);
            c.Equal(HaulTripPolicyCodes.Reserve, blocked.Blocker, "extra movement cannot borrow the future returned cargo");
            Tick(returning); var restored = returnFactory.Load(returning.Save());
            Reach(returning, () => machine.Get<IInventory>().Amount(resource) == Fix.FromInt(2), "actual timed same-resource cargo return", restored);
            c.Equal(returning.Hash(), restored.Hash(), "same-resource cargo claim and propulsion continue identically");
            var commandsFactory = Factory(); var commandsGame = Start(commandsFactory); var commanded = Find(commandsGame, "reserve.truck");
            commandsGame.Seat(0).Submit(new ConfigureHaulTripPolicyCommand { Worker = commanded.Id, Settings = new HaulTripSettings("trip.no-floor", false, false) });
            var commandsTwin = commandsFactory.Load(commandsGame.Save()); Tick(commandsGame); Tick(commandsTwin);
            c.Equal(commandsGame.Hash(), commandsTwin.Hash(), "pending player command and native settings use same saved command stream");
            c.Equal(HaulTripPolicyCodes.Disabled, Preview(commandsTwin).Blocker, "restored pending command is applied normally, not substituted on load");
        }
    }
}
