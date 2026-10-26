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
    internal static class FleetOutletTests
    {
        internal const string Case = "Fleet outlets: whole native reserves, retained cargo and physical saved delivery";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static Entity Find(SimGame game, string id) => game.World.Entities.All.Single(e => e.Alive && e.Def.Id == id);
        private static FleetView View(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<FleetView>();
        private static OutletPolicySettings Read(SimGame game, EntityId id) => LogisticsPolicyRules.Read(game.World, game.World.Entities.Get(id));
        private static Job[] Jobs(SimGame game) => game.World.Services.Get<WorkBoard>().Jobs.Where(j => j.IsHaul).ToArray();
        private static ConfigureFleetOutletPolicyCommand Control(OutletPolicyChange change) => new ConfigureFleetOutletPolicyCommand { Fleet = "delivery", Change = change };
        private static Fix Total(SimGame game)
        {
            var resource = game.World.Content.ResourceId("steel"); Fix total = Fix.Zero;
            foreach (var entity in game.World.Entities.All) if (entity.Get<IInventory>() is IInventory stock) total += stock.Amount(resource);
            return total;
        }
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update();
            if (game.Fault != null || Total(game) != Fix.FromInt(20)) throw new InvalidOperationException("outlet step=" + game.Clock.StepIndex + " fault=" + game.Fault + " steel=" + Total(game));
            if (twin != null) Update(twin);
        }
        private static void Reach(SimGame game, Func<bool> ready, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 1500 && !ready(); i++) Update(game, twin);
            if (!ready()) throw new InvalidOperationException(stage + " clock=" + game.Clock.StepIndex + " jobs=" +
                string.Join(";", Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.Worker + "/" + j.Loaded + "/" + j.Blocker)) +
                " machines=" + string.Join(";", game.World.Entities.All.Where(e => e.Get<IWorker>() != null).Select(e =>
                    e.Id + "/" + e.Get<ITilePresence>().Position + "/" + e.Get<IMotion>().Status + "/fuel=" + e.Get<IInventory>()?.Amount(game.World.Content.ResourceId("fleet.fuel")))));
        }
        private static void Members(Check c, SimGame game, params EntityId[] ids)
            => c.That(TestKit.Order(game, 0, new ConfigureFleetCommand { Fleet = "delivery", Settings = new FleetSettings("fleet.haul", ids) }).Accepted, "normal current membership");
        private static void Held(Check c, SimGame game, EntityId id, bool held, SimGame twin = null)
        {
            c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = id, Held = held }).Accepted, "normal actual worker control");
            if (twin != null) c.That(TestKit.Order(twin, 0, new SetWorkerHeldCommand { Worker = id, Held = held }).Accepted, "same twin worker control");
        }
        private static void Apply(Check c, SimGame game, OutletPolicyChange change, SimGame twin = null)
        {
            c.Equal(Code.None, View(game).PreviewOutletPolicy("delivery", change).Blocker, "same management admission");
            c.That(TestKit.Order(game, 0, Control(change)).Accepted, "normal fleet outlet command");
            if (twin != null)
            { c.That(TestKit.Order(twin, 0, Control(change)).Accepted, "same saved twin outlet command"); c.Equal(game.Hash(), twin.Hash(), "original physical and policy owners identical"); }
        }
        private static OutletPolicyChange Full(SimGame game, int reserve = 30, bool enabled = true, int priority = 100,
            IEnumerable<string> destinations = null, LogisticsReserveAccess access = LogisticsReserveAccess.Protected)
            => OutletPolicyChange.Replace(new OutletPolicySettings(enabled, new Bill(new[] { new ResourceAmount(game.World.Content.ResourceId("fleet.fuel"), Fix.FromInt(reserve)) }), priority, destinations, access));
        private static void Trip(Check c, SimGame game, bool protect, SimGame twin = null)
        {
            var command = new ConfigureFleetTripPolicyCommand { Fleet = "delivery", Settings = new HaulTripSettings(protect ? "fleet.trip-funded" : "fleet.trip-unrestricted", true, protect) };
            c.That(TestKit.Order(game, 0, command).Accepted, "normal existing trip budget selection");
            if (twin != null) c.That(TestKit.Order(twin, 0, new ConfigureFleetTripPolicyCommand { Fleet = "delivery", Settings = command.Settings }).Accepted, "same existing twin trip policy");
        }
        private static void Route(Check c, SimGame game, int route, bool enabled, SimGame twin = null)
        {
            var settings = new LogisticsRouteSettings(enabled, Fix.FromInt(3), 100);
            c.That(TestKit.Order(game, 0, new ConfigureRouteCommand { Route = route, Settings = settings }).Accepted, "normal future route control");
            if (twin != null) c.That(TestKit.Order(twin, 0, new ConfigureRouteCommand { Route = route, Settings = settings }).Accepted, "same twin route control");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current outlet control identity"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no fleet balance payload");
            var factory = FleetNavigationTests.Factory(named: true, outletControls: true); var game = LiveSensorTests.New(factory);
            var native = Find(game, "fleet.trip-native"); var external = Find(game, "fleet.trip-public");
            var unsupported = Find(game, "fleet.restricted"); var foreign = Find(game, "survey.foreign-sensor");
            var source = Find(game, "survey.site"); var target = Find(game, "fleet.destination");
            var fuel = game.World.Content.ResourceId("fleet.fuel"); var steel = game.World.Content.ResourceId("steel");
            var actors = new[] { native, external }.OrderBy(e => e.Id).ToArray(); var ids = actors.Select(e => e.Id).ToArray();
            foreach (var actor in game.World.Entities.All.Where(e => e.Owner == new PlayerId(0) && e.Get<IWorker>() != null)) Held(c, game, actor.Id, true);
            c.That(native.Get<IOutletPolicy>() is LogisticsPolicyComponent && external.Get<IOutletPolicy>() is ContractOutletPolicy, "actual native and existing independent mod outlet owners");
            c.That(native.Get<IOutletPort>() is IInletPort && external.Get<IOutletPort>() is IInletPort, "all outlets also have inlet contract");
            c.That(native.Def.AuthoringPath.Contains("native-fleet-outlet-policies.json") && external.Def.AuthoringPath.Contains("fleet-outlet-machines.json"), "strict master-authored physical prototypes");
            c.That(!game.World.Services.Has<IEconomy>(), "outlet and physical work independent of business");
            c.Equal(7, game.Seat(0).View.FeatureRegistrations.Single(r => r.Id == MachineFleetsModule.ModuleId).SchemaVersion, "private outlet view");
            c.Equal(1, game.Seat(0).View.StateCodecs.Single(r => r.Kind == StateCodecKind.Service && r.Id == MachineFleetsModule.ModuleId).SchemaVersion, "membership only codec");
            var change = OutletPolicyChange.Reserve(fuel, Fix.FromInt(28), true);
            Members(c, game, native.Id, unsupported.Id); ulong hash = game.Hash(); var refusal = View(game).PreviewOutletPolicy("delivery", change);
            c.Equal(LogisticsPolicyCodes.NoPolicy, refusal.Blocker, "missing provider whole refusal"); c.That(refusal.Entries.Single(e => e.Entity == unsupported.Id).Current == null, "no fabricated policy");
            c.Equal(hash, game.Hash(), "whole refusal pure"); c.Equal(LogisticsPolicyCodes.NoPolicy, TestKit.Order(game, 0, Control(change)).Refusal, "normal refusal parity");
            c.Equal(Fix.FromInt(30), Read(game, native.Id).Reserve.AmountOf(fuel), "native prefix unchanged");
            Members(c, game, ids.Reverse().ToArray()); hash = game.Hash(); var before = View(game).PreviewOutletPolicy("delivery", change);
            c.That(before.Entries.Select(e => e.Entity).SequenceEqual(ids) && before.Entries.All(e => e.Current.Reserve.AmountOf(fuel) == Fix.FromInt(30) && e.Next.Reserve.AmountOf(fuel) == Fix.FromInt(28)), "canonical current/proposed detached facts");
            c.That(before.Entries.Single(e => e.Entity == native.Id).Next.Reserve.AmountOf(steel) == Fix.FromInt(2) &&
                before.Entries.Single(e => e.Entity == external.Id).Next.Reserve.AmountOf(steel) == Fix.FromInt(3), "one-resource edit preserves distinct other reserve lines");
            c.That(before.Entries.All(e => e.Next.ExportPriority == e.Current.ExportPriority && e.Next.ReserveAccess == e.Current.ReserveAccess &&
                (e.Next.AllowedDestinations == null) == (e.Current.AllowedDestinations == null)), "per-member priority access and filters retained");
            c.Throws<NotSupportedException>(() => ((IList<OutletPolicySelectionEntry>)before.Entries).Clear(), "immutable selection");
            foreach (var malformed in new[] { null, Array.Empty<EntityId>(), new[] { native.Id, native.Id }, new[] { native.Id, EntityId.None } })
                c.Equal(Codes.InvalidArgument, LogisticsPolicyRules.PreviewGroup(game.World, new PlayerId(0), malformed, change).Blocker, "malformed targets refuse");
            c.Equal(Codes.NotSeated, LogisticsPolicyRules.PreviewGroup(game.World, PlayerId.None, ids, change).Blocker, "observer no control");
            c.Equal(Codes.InvalidArgument, View(game).PreviewOutletPolicy("delivery", null).Blocker, "no default change");
            c.Equal(Codes.InvalidArgument, View(game).PreviewOutletPolicy("delivery", OutletPolicyChange.Reserve(fuel, Fix.Parse("0.5"), true)).Blocker, "native discrete quantum");
            c.Equal(Codes.InvalidArgument, View(game).PreviewOutletPolicy("delivery", OutletPolicyChange.Reserve(fuel, Fix.FromInt(64), true)).Blocker, "aggregate existing reserve capacity");
            c.Equal(Codes.InvalidArgument, View(game).PreviewOutletPolicy("delivery", Full(game, destinations: new[] { "missing" })).Blocker, "unknown destination content");
            c.Equal(Codes.NotFound, View(game, 1).PreviewOutletPolicy("delivery", change).Blocker, "company-private named control");
            var hidden = LogisticsPolicyRules.PreviewGroup(game.World, new PlayerId(0), new[] { foreign.Id }, change);
            c.That(hidden.Blocker == Codes.NotFound && hidden.Entries[0].Current == null && hidden.Entries[0].Next == null, "foreign provider never disclosed or read");
            c.Equal(hash, game.Hash(), "all pure reads preserve authoritative state");
            var versions = actors.Select(e => e.Version).ToArray(); var same = TestKit.Order(game, 0, Control(OutletPolicyChange.Reserve(fuel, Fix.FromInt(30), true)));
            c.That(same.Accepted && same.Arg0 == 2 && same.Arg1 == 0 && actors.Select(e => e.Version).SequenceEqual(versions), "identical native/public settings no Touch");
            game.Seat(0).Submit(Control(change)); var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending original change codec saved");
            Update(game, twin); c.Equal(game.Hash(), twin.Hash(), "same pending command commits original owners");
            c.That(before.Entries.All(e => e.Current.Reserve.AmountOf(fuel) == Fix.FromInt(30)), "previous detached current facts frozen");
            c.That(actors.All(e => e.Get<IWorker>().Held && e.Get<IWorker>().Job == 0 && WorkerHomeContracts.ReadHome(game.World, e) == source.Id), "policy preserves held/job/home");
            Apply(c, game, Full(game, enabled: false, priority: 251, destinations: Array.Empty<string>(), access: LogisticsReserveAccess.EmergencyOnly), twin);
            c.That(actors.All(e => !Read(game, e.Id).Enabled && Read(game, e.Id).ExportPriority == 251 && Read(game, e.Id).ReserveAccess == LogisticsReserveAccess.EmergencyOnly && Read(game, e.Id).AllowedDestinations.Count == 0), "all original replace fields");
            Apply(c, game, Full(game), twin);
            Members(c, game, external.Id); Members(c, twin, external.Id);
            Apply(c, game, OutletPolicyChange.Reserve(fuel, Fix.FromInt(29), true), twin);
            c.That(Read(game, native.Id).Reserve.AmountOf(fuel) == Fix.FromInt(30) && Read(game, external.Id).Reserve.AmountOf(fuel) == Fix.FromInt(29), "current members only, no overlay");
            Apply(c, game, OutletPolicyChange.Reserve(fuel, Fix.FromInt(30), true), twin);
            // Single entity command uses the SAME exact owner; no duplicate commit path.
            var single = new ConfigureOutletPolicyCommand { Source = native.Id, Change = Full(game) };
            int oldVersion = native.Version; c.That(TestKit.Order(game, 0, single).Accepted && native.Version == oldVersion, "single idempotent path");
            c.That(TestKit.Order(twin, 0, new ConfigureOutletPolicyCommand { Source = native.Id, Change = Full(twin) }).Accepted, "same twin single owner");
            var routeResult = TestKit.Order(game, 0, new AddRouteCommand { From = source.Id, To = target.Id, Resource = steel, Settings = new LogisticsRouteSettings(true, Fix.FromInt(3), 100) });
            var twinRoute = TestKit.Order(twin, 0, new AddRouteCommand { From = source.Id, To = target.Id, Resource = steel, Settings = new LogisticsRouteSettings(true, Fix.FromInt(3), 100) });
            c.That(routeResult.Accepted && twinRoute.Accepted && routeResult.Arg0 == twinRoute.Arg0, "normal physical route"); int route = checked((int)routeResult.Arg0);
            foreach (var actor in new[] { external, native })
            {
                Members(c, game, actor.Id); Members(c, twin, actor.Id); Held(c, game, actor.Id, false, twin); Route(c, game, route, true, twin);
                Reach(game, () => Jobs(game).Any(j => j.IsLive && j.Worker == actor.Id), "real haul assignment", twin); var job = Jobs(game).Single(j => j.IsLive && j.Worker == actor.Id);
                Route(c, game, route, false, twin);
                Reach(game, () => job.Loaded == job.Amount && job.Toils[job.ToilIndex].Executor == WorkPhases.Travel && job.Toils[job.ToilIndex].At == target.Id, "actual loaded journey", twin);
                c.That(actor.Get<IInventory>().Amount(steel) == Fix.FromInt(3), "physical loading, no instant delivery");
                Apply(c, game, OutletPolicyChange.Reserve(fuel, Fix.FromInt(4), true), twin); Trip(c, game, true, twin);
                var destinationReceipt = job.InReservation; var cargoReceipt = job.CargoOutReservation; var loaded = job.Loaded;
                Apply(c, game, OutletPolicyChange.Reserve(fuel, Fix.FromInt(30), true), twin);
                Reach(game, () => job.TripHeld, "actual reserve floor holds loaded trip", twin);
                c.That(job.Blocker == HaulTripPolicyCodes.Reserve && job.Worker == actor.Id && actor.Get<IWorker>().Job == job.Id &&
                    job.InReservation.Equals(destinationReceipt) && job.CargoOutReservation.Equals(cargoReceipt) && job.Loaded == loaded, "same acquired cargo and claims retained");
                var position = actor.Get<ITilePresence>().Position; Fix actualFuel = actor.Get<IInventory>().Amount(fuel);
                for (int i = 0; i < 5; i++) Update(game, twin);
                c.That(actor.Get<ITilePresence>().Position.Equals(position) && actor.Get<IInventory>().Amount(fuel) == actualFuel && job.Loaded == loaded, "held elapsed time burns no physical fuel/cargo");
                twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "loaded reserve-held checkpoint");
                game.Clock.Pause(); twin.Clock.Pause(); long time = game.Clock.StepIndex; Update(game, twin); c.Equal(time, game.Clock.StepIndex, "sole clock pause"); game.Clock.Resume(); twin.Clock.Resume();
                Apply(c, game, OutletPolicyChange.Reserve(fuel, Fix.FromInt(4), true), twin);
                c.That(TestKit.Order(game, 0, new RemoveFleetCommand { Fleet = "delivery" }).Accepted, "remove metadata during physical delivery");
                c.That(TestKit.Order(twin, 0, new RemoveFleetCommand { Fleet = "delivery" }).Accepted, "same twin metadata removal");
                Reach(game, () => job.State == JobState.Done, "real delivery and physical departure", twin);
                c.That(actor.Get<IInventory>().Amount(steel).IsZero && actor.Get<IInventory>().Amount(fuel) < actualFuel, "actual unloading and positive fuel consumption");
                c.Equal(game.Hash(), twin.Hash(), "saved physical native-public continuation");
                Reach(game, () => actor.Get<ITilePresence>().Tile.X < 29, "physical return clears destination exit", twin);
                Held(c, game, actor.Id, true, twin);
            }
            c.That(target.Get<IInventory>().Amount(steel) == Fix.FromInt(6) && source.Get<IInventory>().Amount(steel) == Fix.FromInt(14) && Total(game) == Fix.FromInt(20), "two physical deliveries conserve20 steel");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "final original owner roundtrip");
            var isolated = LiveSensorTests.New(factory); c.That(!View(isolated).TryRead("delivery", out _) && Read(isolated, Find(isolated, "fleet.trip-public").Id).Reserve.AmountOf(fuel) == Fix.FromInt(30), "isolated settings and membership");
            Members(c, isolated); c.Equal(FleetCodes.Empty, View(isolated).PreviewOutletPolicy("delivery", change).Blocker, "empty explicit fleet refuses");
            c.That(!LiveSensorTests.New(FleetNavigationTests.Factory()).World.Services.Has<IMachineFleets>(), "optional fleet omission");
            Invalid(c);
        }
        private static void Invalid(Check c)
        {
            foreach (var mode in new[] { "missing-reserve", "unknown-resource", "extra", "overflow", "unknown-destination" })
            {
                var doc = Json.Parse(FrameworkProofContent.Read("native-fleet-outlet-policies.json")); var outlet = doc.Get("logistics_policies")[0].Get("outlet");
                if (mode == "missing-reserve") outlet.Remove("reserve");
                if (mode == "unknown-resource") outlet.Get("reserve").Set("missing.resource", JsonValue.Number("1"));
                if (mode == "extra") outlet.Set("free_fuel", JsonValue.Of(true));
                if (mode == "overflow") outlet.Get("reserve").Set("fleet.fuel", JsonValue.Number("64"));
                if (mode == "unknown-destination") outlet.Set("allowed_destinations", Json.Parse("[\"missing\"]"));
                c.Throws<ContentException>(() => LiveSensorTests.New(FleetNavigationTests.Factory(named: true, outletControls: true, outletPolicies: doc.ToString())), "strict master refuses " + mode);
            }
            foreach (int fault in new[] { 1, 2 })
            {
                var doc = Json.Parse(FrameworkProofContent.Read("fleet-outlet-machines.json")); doc.Get("entities")[0].Get("components").Get("proof_fleet_outlet").Set("fault", JsonValue.Number(fault.ToString()));
                var game = LiveSensorTests.New(FleetNavigationTests.Factory(named: true, outletControls: true, proofOutlet: doc.ToString())); var actor = Find(game, "fleet.trip-public");
                var change = OutletPolicyChange.Reserve(game.World.Content.ResourceId("fleet.fuel"), Fix.FromInt(28), true);
                if (fault == 1) c.Throws<InvalidOperationException>(() => LogisticsPolicyRules.PreviewGroup(game.World, new PlayerId(0), new[] { actor.Id }, change), "silent public pure-check mutation guarded by original codec");
                else c.Throws<InvalidOperationException>(() => LogisticsPolicyRules.ConfigureGroup(game.World, new PlayerId(0), new[] { actor.Id }, change), "ignored exact public commit faults");
                c.That(game.Fault != null, "actual violation faults session"); c.Throws<InvalidOperationException>(() => game.Save(), "no healthy partial checkpoint");
            }
        }
    }
}
