using System;
using System.Collections.Generic;
using System.Linq;
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
    internal static class FleetTripTests
    {
        internal const string Case = "Fleet trips: whole reserve control, physical loaded holds and saved delivery";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static Entity Find(SimGame game, string id) => game.World.Entities.All.Single(e => e.Alive && e.Def.Id == id);
        private static FleetView View(SimGame game, int company = 0) => game.Seat(company).View.Feature<FleetView>();
        private static Job[] Jobs(SimGame game) => game.World.Services.Get<WorkBoard>().Jobs.Where(j => j.IsHaul).OrderBy(j => j.Id).ToArray();
        internal static HaulTripSettings Settings(bool enabled = true, bool reserve = false, string profile = "fleet.trip-unrestricted")
            => new HaulTripSettings(profile, enabled, reserve);
        internal static ConfigureFleetTripPolicyCommand Control(HaulTripSettings settings)
            => new ConfigureFleetTripPolicyCommand { Fleet = "delivery", Settings = settings };
        private static void Members(Check c, SimGame game, params EntityId[] ids)
            => c.That(TestKit.Order(game, 0, new ConfigureFleetCommand { Fleet = "delivery", Settings = new FleetSettings("fleet.haul", ids) }).Accepted, "normal current membership");
        private static void Held(Check c, SimGame game, EntityId worker, bool held, SimGame twin = null)
        {
            c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = worker, Held = held }).Accepted, "normal player held control");
            if (twin != null) c.That(TestKit.Order(twin, 0, new SetWorkerHeldCommand { Worker = worker, Held = held }).Accepted, "same twin held control");
        }
        private static Fix Steel(SimGame game)
        {
            Fix sum = Fix.Zero; var resource = game.World.Content.ResourceId("steel");
            foreach (var entity in game.World.Entities.All) if (entity.Get<IInventory>() is IInventory store) sum += store.Amount(resource);
            return sum;
        }
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("fleet trip step=" + game.Clock.StepIndex + " " + game.Fault);
            if (Steel(game) != Fix.FromInt(20)) throw new InvalidOperationException("fleet trip steel accounting step=" + game.Clock.StepIndex);
            if (twin != null) Update(twin);
        }
        private static void Reach(SimGame game, Func<bool> ready, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 1500 && !ready(); i++) Update(game, twin);
            if (!ready()) throw new InvalidOperationException(stage + " clock=" + game.Clock.StepIndex + " jobs=" + string.Join(";",
                Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.Worker + "/" + j.Loaded + "/" + j.Blocker)) + " machines=" +
                string.Join(";", game.World.Entities.All.Where(e => e.Get<IWorker>() != null).Select(e => e.Id + "/" + e.Get<ITilePresence>().Position)));
        }
        private static void Apply(Check c, SimGame game, HaulTripSettings settings, SimGame twin = null)
        {
            c.Equal(Code.None, View(game).PreviewTripPolicy("delivery", settings).Blocker, "private whole admission");
            c.That(TestKit.Order(game, 0, Control(settings)).Accepted, "normal named trip control");
            if (twin != null)
            { c.That(TestKit.Order(twin, 0, Control(settings)).Accepted, "same saved twin trip control"); c.Equal(game.Hash(), twin.Hash(), "actual policy and physical owners match"); }
        }
        private static void Route(Check c, SimGame game, int id, bool enabled, SimGame twin = null)
        {
            c.That(TestKit.Order(game, 0, new ConfigureRouteCommand { Route = id, Settings = new LogisticsRouteSettings(enabled, Fix.FromInt(3), 100) }).Accepted, "normal route batch intent");
            if (twin != null) c.That(TestKit.Order(twin, 0, new ConfigureRouteCommand { Route = id, Settings = new LogisticsRouteSettings(enabled, Fix.FromInt(3), 100) }).Accepted, "same twin route intent");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current trip controls identity"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no trip overlay payload");
            var factory = FleetNavigationTests.Factory(named: true, tripControls: true); var game = LiveSensorTests.New(factory);
            var native = Find(game, "fleet.trip-native"); var external = Find(game, "fleet.trip-public");
            var source = Find(game, "survey.site"); var target = Find(game, "fleet.destination"); var unsupported = Find(game, "fleet.restricted");
            var foreign = Find(game, "survey.foreign-sensor"); var steel = game.World.Content.ResourceId("steel"); var fuel = game.World.Content.ResourceId("fleet.fuel");
            var actors = new[] { native, external }.OrderBy(e => e.Id).ToArray(); var ids = actors.Select(e => e.Id).ToArray();
            foreach (var actor in game.World.Entities.All.Where(e => e.Owner == new PlayerId(0) && e.Get<IWorker>() != null)) Held(c, game, actor.Id, true);
            c.That(native.Get<IHaulTripPolicy>() is HaulTripPolicy && external.Get<IHaulTripPolicy>() is ContractHaulTrip, "actual native and independent public owners");
            c.That(native.Def.AuthoringPath.Contains("native-fleet-trip-policies.json") && external.Def.AuthoringPath.Contains("fleet-trip-proof.json"), "selected master JSON prototypes");
            c.That(!game.World.Services.Has<IEconomy>(), "physical work and fuel do not require money");
            c.Equal(7, game.Seat(0).View.FeatureRegistrations.Single(r => r.Id == MachineFleetsModule.ModuleId).SchemaVersion, "private trip view revision");
            c.Equal(1, game.Seat(0).View.StateCodecs.Single(r => r.Kind == StateCodecKind.Service && r.Id == MachineFleetsModule.ModuleId).SchemaVersion, "membership-only codec unchanged");
            Members(c, game, native.Id, unsupported.Id); var requested = Settings(false);
            ulong hash = game.Hash(); var refuse = View(game).PreviewTripPolicy("delivery", requested);
            c.Equal(HaulTripPolicyCodes.NotPolicy, refuse.Blocker, "absent actual provider refuses whole fleet");
            c.That(refuse.Entries.Single(e => e.Entity == unsupported.Id).Settings == null, "no fabricated trip settings"); c.Equal(hash, game.Hash(), "refusal pure");
            c.Equal(HaulTripPolicyCodes.NotPolicy, TestKit.Order(game, 0, Control(requested)).Refusal, "ordinary refusal parity");
            c.That(HaulTripPolicyContracts.Read(game.World, native).Enabled, "valid native prefix not changed");
            Members(c, game, ids.Reverse().ToArray()); hash = game.Hash(); var before = View(game).PreviewTripPolicy("delivery", requested);
            c.That(before.Entries.Select(e => e.Entity).SequenceEqual(ids), "canonical stable entity order");
            c.That(before.Entries.All(e => e.Settings.Profile == "fleet.trip-funded" && e.Settings.Enabled && e.Settings.PreserveOutletReserve), "actual authored initial settings");
            c.Throws<NotSupportedException>(() => ((IList<HaulTripSelectionEntry>)before.Entries).Clear(), "immutable detached preview");
            foreach (var malformed in new[] { null, Array.Empty<EntityId>(), new[] { ids[0], ids[0] }, new[] { ids[0], EntityId.None } })
                c.Equal(Codes.InvalidArgument, HaulTripPolicyContracts.PreviewGroup(game.World, new PlayerId(0), malformed, requested).Blocker, "malformed selection refuses");
            c.Equal(Codes.NotSeated, HaulTripPolicyContracts.PreviewGroup(game.World, PlayerId.None, ids, requested).Blocker, "observer no mutation authority");
            var hidden = HaulTripPolicyContracts.PreviewGroup(game.World, new PlayerId(0), new[] { foreign.Id }, requested);
            c.That(hidden.Blocker == Codes.NotFound && hidden.Entries[0].Settings == null, "foreign policy never read");
            c.Equal(Codes.NotFound, View(game, 1).PreviewTripPolicy("delivery", requested).Blocker, "company-private named controls");
            c.Equal(Codes.NotFound, View(game).PreviewTripPolicy("missing", requested).Blocker, "missing name refuses");
            c.Equal(HaulTripPolicyCodes.Invalid, View(game).PreviewTripPolicy("delivery", null).Blocker, "null settings no defaults");
            c.Equal(HaulTripPolicyCodes.Invalid, View(game).PreviewTripPolicy("delivery", Settings(profile: "missing")).Blocker, "unknown selected profile refuses");
            var veto = Settings(profile: "fleet.trip-veto"); c.Equal(new Code("proof.trip-refused"), View(game).PreviewTripPolicy("delivery", veto).Blocker, "independent public veto is whole refusal");
            c.Equal(hash, game.Hash(), "all reads/admission pure");
            var versions = actors.Select(e => e.Version).ToArray(); c.Equal(new Code("proof.trip-refused"), TestKit.Order(game, 0, Control(veto)).Refusal, "normal public veto refusal");
            c.That(actors.Select(e => e.Version).SequenceEqual(versions) && actors.All(e => HaulTripPolicyContracts.Read(game.World, e).Profile == "fleet.trip-funded"), "late public veto commits no native prefix");
            game.Seat(0).Submit(Control(requested)); var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending trip intent persisted");
            Update(game, twin); c.Equal(game.Hash(), twin.Hash(), "pending trip command same original settings owners");
            c.That(actors.All(e => !HaulTripPolicyContracts.Read(game.World, e).Enabled && !HaulTripPolicyContracts.Read(game.World, e).PreserveOutletReserve), "all exact requested fields");
            c.That(before.Entries.All(e => e.Settings.Enabled && e.Settings.PreserveOutletReserve), "old detached facts remain frozen");
            c.That(actors.All(e => e.Get<IWorker>().Held && e.Get<IWorker>().Job == 0 && WorkerHomeContracts.ReadHome(game.World, e) == source.Id), "trip edit preserves held/job/home");
            versions = actors.Select(e => e.Version).ToArray(); var same = TestKit.Order(game, 0, Control(requested)); TestKit.Order(twin, 0, Control(requested));
            c.That(same.Accepted && same.Arg0 == 2 && same.Arg1 == 0 && actors.Select(e => e.Version).SequenceEqual(versions), "identical settings no provider Touch");
            Apply(c, game, Settings(true, true, "fleet.trip-funded"), twin);
            foreach (var actor in actors)
            {
                var beforeBudget = actor.Get<IInventory>().Amount(fuel);
                var budget = HaulTripPolicyContracts.CheckTrip(game.World, actor, new MotionTravelEstimate(60, Code.None));
                c.That(budget.Blocker == HaulTripPolicyCodes.Reserve && budget.ReserveFloor.AmountOf(fuel) == Fix.FromInt(12) && budget.RequiredStock.AmountOf(fuel) > Fix.FromInt(12), "original movement inputs plus actual outlet reserve refuse unfunded trip");
                c.Equal(beforeBudget, actor.Get<IInventory>().Amount(fuel), "pure budget burns no fuel");
            }
            var result = TestKit.Order(game, 0, new AddRouteCommand { From = source.Id, To = target.Id, Resource = steel, Settings = new LogisticsRouteSettings(true, Fix.FromInt(3), 100) });
            var other = TestKit.Order(twin, 0, new AddRouteCommand { From = source.Id, To = target.Id, Resource = steel, Settings = new LogisticsRouteSettings(true, Fix.FromInt(3), 100) });
            c.That(result.Accepted && other.Accepted && result.Arg0 == other.Arg0, "normal original physical route"); int route = checked((int)result.Arg0);
            Held(c, game, external.Id, false, twin);
            for (int i = 0; i < 4; i++) Update(game, twin);
            c.That(!Jobs(game).Any(j => j.IsLive) && source.Get<IInventory>().Amount(steel) == Fix.FromInt(20), "real reserve refusal creates no shipment or stock grant");
            var offers = new List<LogisticsDispatchOffer>(); game.Seat(0).View.Feature<LogisticsView>().CopyQueue(offers);
            c.Equal(HaulTripPolicyCodes.Reserve, offers.Single(o => o.Route == route).Haul.Blocker, "original dispatcher reports actual reserve blocker");
            Apply(c, game, Settings(), twin);
            c.That(actors.All(e => LogisticsPolicyRules.Read(game.World, e).Reserve.AmountOf(fuel) == Fix.FromInt(12)), "trip switch does not rewrite outlet reserves");
            Reach(game, () => Jobs(game).Any(j => j.IsLive && j.Worker == external.Id), "public funded dispatch", twin); var first = Jobs(game).Single(j => j.IsLive);
            Route(c, game, route, false, twin);
            Reach(game, () => first.Loaded == first.Amount && first.Toils[first.ToilIndex].Executor == WorkPhases.Travel && first.Toils[first.ToilIndex].At == target.Id && external.Get<IOperatingInputs>(new Code("propulsion")).ReadExposure().ElapsedTicks >= 3, "public loaded movement", twin);
            c.Equal(Fix.Zero, target.Get<IInventory>().Amount(steel), "loaded travel does not instantly deliver");
            var cargo = first.Loaded; var receipt = first.InReservation; var cargoReceipt = first.CargoOutReservation; var handling = first.HandlingProgress;
            var stock = external.Get<IInventory>().Amount(fuel); var position = external.Get<ITilePresence>().Position;
            Apply(c, game, Settings(false), twin);
            c.That(first.TripHeld && first.State == JobState.Paused && first.Blocker == HaulTripPolicyCodes.Disabled && external.Get<IMotion>().Status == MoveStatus.Idle, "original budget physically holds loaded public carrier");
            c.That(first.Loaded == cargo && first.InReservation.Equals(receipt) && first.CargoOutReservation.Equals(cargoReceipt) && first.HandlingProgress.Equals(handling) && external.Get<IWorker>().Job == first.Id, "original work and protected claims retained");
            c.Equal(stock, external.Get<IInventory>().Amount(fuel), "command update burns no held fuel");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "loaded trip hold saved through original codecs");
            for (int i = 0; i < 5; i++) Update(game, twin);
            c.That(external.Get<ITilePresence>().Position.Equals(position) && external.Get<IInventory>().Amount(fuel) == stock && first.Loaded == cargo, "held time no movement/cargo/fuel loss");
            game.Clock.Pause(); twin.Clock.Pause(); long paused = game.Clock.StepIndex; Update(game, twin); c.Equal(paused, game.Clock.StepIndex, "sole pause freezes trip"); game.Clock.Resume(); twin.Clock.Resume();
            Apply(c, game, Settings(), twin);
            c.That(TestKit.Order(game, 0, new RemoveFleetCommand { Fleet = "delivery" }).Accepted, "remove membership metadata during physical trip");
            c.That(TestKit.Order(twin, 0, new RemoveFleetCommand { Fleet = "delivery" }).Accepted, "same twin metadata removal");
            Reach(game, () => first.State == JobState.Done, "public delivery and physical departure", twin);
            c.Equal(Fix.FromInt(3), target.Get<IInventory>().Amount(steel), "actual public delivery once"); c.Equal(game.Hash(), twin.Hash(), "saved public delivery identical");
            c.That(external.Get<IInventory>().Amount(fuel) < Fix.FromInt(12), "original movement consumes actual fuel over game time");
            Reach(game, () => external.Get<ITilePresence>().Tile.X < 29, "public return clears original destination exit", twin);
            Held(c, game, external.Id, true, twin); Members(c, game, native.Id); Members(c, twin, native.Id);
            Apply(c, game, Settings(false, true, "fleet.trip-funded"), twin);
            c.That(!HaulTripPolicyContracts.Read(game.World, native).Enabled && HaulTripPolicyContracts.Read(game.World, external).Enabled, "current membership only, no overlay changes removed public provider");
            Held(c, game, native.Id, false, twin); Route(c, game, route, true, twin);
            c.That(!Jobs(game).Any(j => j.IsLive), "disabled native policy prevents new physical intake"); Apply(c, game, Settings(), twin);
            Reach(game, () => Jobs(game).Any(j => j.IsLive && j.Worker == native.Id), "native funded dispatch", twin); var second = Jobs(game).Single(j => j.IsLive);
            Route(c, game, route, false, twin);
            Reach(game, () => second.Loaded == second.Amount && second.Toils[second.ToilIndex].Executor == WorkPhases.Travel && second.Toils[second.ToilIndex].At == target.Id, "native loaded trip", twin);
            Apply(c, game, Settings(true, true, "fleet.trip-funded"), twin);
            c.That(second.TripHeld && second.Blocker == HaulTripPolicyCodes.Reserve && second.Worker == native.Id, "actual reserve switch holds acquired native cargo without cancellation");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "native reserve-held physical checkpoint");
            Apply(c, game, Settings(), twin); Reach(game, () => second.State == JobState.Done, "native resume and physical delivery", twin);
            c.That(target.Get<IInventory>().Amount(steel) == Fix.FromInt(6) && source.Get<IInventory>().Amount(steel) == Fix.FromInt(14) && Steel(game) == Fix.FromInt(20), "both native/public deliveries conserve real steel");
            c.That(actors.All(e => e.Get<IInventory>().Amount(steel).IsZero), "completed physical unloading empties cargo");
            c.Equal(game.Hash(), twin.Hash(), "saved native fuel/claims/physical continuation identity"); c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "settled final save roundtrip");
            var isolated = LiveSensorTests.New(factory); c.That(!View(isolated).TryRead("delivery", out _) && HaulTripPolicyContracts.Read(isolated.World, Find(isolated, "fleet.trip-public")).PreserveOutletReserve, "interleaved sessions separate membership/settings");
            c.That(!LiveSensorTests.New(FleetNavigationTests.Factory()).World.Services.Has<IMachineFleets>(), "omitted fleet module grants no hidden controls");
            Members(c, isolated); c.Equal(FleetCodes.Empty, View(isolated).PreviewTripPolicy("delivery", requested).Blocker, "empty explicit fleet refuses trip control");
            Invalid(c);
        }
        private static void Invalid(Check c)
        {
            foreach (var mode in new[] { "missing-reserve", "missing-sources", "extra", "unknown-profile", "unknown-service" })
            {
                var doc = Content.Json.Parse(FrameworkProofContent.Read("native-fleet-trip-policies.json")); var profile = doc.Get("haul_trip_profiles")[0];
                if (mode == "missing-reserve") profile.Remove("preserve_outlet_reserve"); if (mode == "missing-sources") profile.Remove("service_sources");
                if (mode == "extra") profile.Set("free_fuel", JsonValue.Of(true));
                if (mode == "unknown-profile") doc.Get("entities")[0].Get("components").Get("haul_trip_policy").Set("profile", JsonValue.Of("missing"));
                if (mode == "unknown-service") profile.Set("service_sources", Content.Json.Parse("[\"unknown.service\"]"));
                c.Throws<ContentException>(() => LiveSensorTests.New(FleetNavigationTests.Factory(named: true, tripControls: true, tripPolicies: doc.ToString())), "strict master JSON refuses " + mode);
            }
            foreach (int fault in new[] { 1, 2 })
            {
                var doc = Content.Json.Parse(FrameworkProofContent.Read("fleet-trip-proof.json"));
                doc.Get("entities")[0].Get("components").Get("proof_fleet_trip").Set("fault", JsonValue.Number(fault.ToString()));
                var game = LiveSensorTests.New(FleetNavigationTests.Factory(named: true, tripControls: true, proofTrip: doc.ToString())); var actor = Find(game, "fleet.trip-public");
                if (fault == 1) c.Throws<InvalidOperationException>(() => HaulTripPolicyContracts.PreviewGroup(game.World, new PlayerId(0), new[] { actor.Id }, Settings(false)), "silent public Check mutation detected by actual codec");
                else c.Throws<InvalidOperationException>(() => HaulTripPolicyContracts.ConfigureGroup(game.World, new PlayerId(0), new[] { actor.Id }, Settings(false)), "ignored public commit fail-stops original selection");
                c.That(game.Fault != null, "actual provider violation faults session"); c.Throws<InvalidOperationException>(() => game.Save(), "faulted trip provider cannot healthy checkpoint");
            }
        }
    }
}
