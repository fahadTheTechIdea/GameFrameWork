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
    internal static class FleetLoadTests
    {
        internal const string Case = "Fleet batching: whole controls, original wait history and protected saved cargo";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static Entity Find(SimGame game, string id) => game.World.Entities.All.Single(e => e.Def.Id == id);
        internal static FleetView View(SimGame game, int company = 0) => game.Seat(company).View.Feature<FleetView>();
        private static Job[] Shipments(SimGame game) => game.World.Services.Get<WorkBoard>().Jobs.Where(j => j.IsHaul).OrderBy(j => j.Id).ToArray();
        private static HaulLoadSettings Settings(SimGame game, bool enabled, int minimum, GameDuration wait, bool bypass = false, string profile = "fleet.batch")
            => Settings(game.World.Content.ResourceId("steel"), enabled, minimum, wait, bypass, profile);
        internal static HaulLoadSettings Settings(ResourceId steel, bool enabled, int minimum, GameDuration wait, bool bypass = false, string profile = "fleet.batch")
            => new HaulLoadSettings(profile, enabled, new Bill(new[] { new ResourceAmount(steel, Fix.FromInt(minimum)) }), wait, bypass);
        internal static ConfigureFleetLoadPolicyCommand Control(HaulLoadSettings settings) => new ConfigureFleetLoadPolicyCommand { Fleet = "delivery", Settings = settings };
        internal static ConfigureFleetCommand MembershipCommand(IReadOnlyList<EntityId> ids)
            => new ConfigureFleetCommand { Fleet = "delivery", Settings = new FleetSettings("fleet.haul", ids) };
        private static void Membership(Check c, SimGame game, IReadOnlyList<EntityId> ids)
            => c.That(TestKit.Order(game, 0, MembershipCommand(ids)).Accepted, "normal current fleet membership");
        private static void Held(Check c, SimGame game, EntityId actor, bool held, SimGame twin = null)
        {
            c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = actor, Held = held }).Accepted, "normal player-held control");
            if (twin != null) c.That(TestKit.Order(twin, 0, new SetWorkerHeldCommand { Worker = actor, Held = held }).Accepted, "same twin held control");
        }
        internal static LogisticsDispatchOffer Offer(SimGame game, int route)
        { var offers = new List<LogisticsDispatchOffer>(); game.Seat(0).View.Feature<LogisticsView>().CopyQueue(offers); return offers.Single(o => o.Route == route); }
        internal static RouteView Route(SimGame game, int id)
        { var routes = new List<RouteView>(); game.Seat(0).View.Feature<LogisticsView>().CopyRoutes(routes); return routes.Single(r => r.Id == id); }
        private static Fix Total(SimGame game)
        {
            var steel = game.World.Content.ResourceId("steel"); Fix total = Fix.Zero;
            foreach (var entity in game.World.Entities.All) if (entity.Get<IInventory>() is IInventory inventory) total += inventory.Amount(steel);
            return total;
        }
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("fleet batching clock=" + game.Clock.StepIndex + " " + game.Fault);
            if (Total(game) != Fix.FromInt(20)) throw new InvalidOperationException("fleet batching conservation clock=" + game.Clock.StepIndex);
            if (twin != null) Update(twin);
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 1500 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " clock=" + game.Clock.StepIndex + " jobs=" + string.Join(";",
                Shipments(game).Select(j => j.Id + "/" + j.State + "/" + j.Worker + "/" + j.Loaded + "/" + j.Blocker)) + " positions=" +
                string.Join(";", game.World.Entities.All.Where(e => e.Get<IWorker>() != null).Select(e => e.Id + "/" + e.Get<ITilePresence>().Position)));
        }
        private static void Apply(Check c, SimGame game, HaulLoadSettings settings, SimGame twin = null)
        {
            c.Equal(Code.None, View(game).PreviewLoadPolicy("delivery", settings).Blocker, "pure named whole admission");
            c.That(TestKit.Order(game, 0, Control(settings)).Accepted, "normal named batching control");
            if (twin != null)
            { c.That(TestKit.Order(twin, 0, Control(settings)).Accepted, "same batching intent on saved twin"); c.Equal(game.Hash(), twin.Hash(), "same original settings/physical owners"); }
        }
        private static void ConfigureRoute(Check c, SimGame game, int route, bool enabled, int desired, SimGame twin = null)
        {
            c.That(TestKit.Order(game, 0, new ConfigureRouteCommand { Route = route, Settings = new LogisticsRouteSettings(enabled, Fix.FromInt(desired), 100) }).Accepted, "normal route intent");
            if (twin != null) c.That(TestKit.Order(twin, 0, new ConfigureRouteCommand { Route = route, Settings = new LogisticsRouteSettings(enabled, Fix.FromInt(desired), 100) }).Accepted, "same saved route intent");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current fleet batching identity"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no fleet batching overlay payload");
            var factory = FleetNavigationTests.Factory(named: true, loadControls: true); var game = LiveSensorTests.New(factory);
            var native = Find(game, "fleet.load-native"); var external = Find(game, "fleet.load-public"); var source = Find(game, "survey.site"); var target = Find(game, "fleet.destination");
            var foreign = Find(game, "survey.foreign-sensor"); var unsupported = Find(game, "fleet.restricted"); var steel = game.World.Content.ResourceId("steel");
            var actors = new[] { native, external }.OrderBy(e => e.Id).ToArray(); var ids = actors.Select(e => e.Id).ToArray();
            foreach (var actor in game.World.Entities.All.Where(e => e.Owner == new PlayerId(0) && e.Get<IWorker>() != null)) Held(c, game, actor.Id, true);
            c.That(native.Get<IHaulLoadPolicy>() is HaulLoadPolicy && external.Get<IHaulLoadPolicy>() is ContractHaulLoad, "actual native and independent public batching owners");
            c.That(native.Def.AuthoringPath.Contains("native-fleet-load-policies.json") && external.Def.AuthoringPath.Contains("fleet-load-proof.json"), "actual selected master JSON machine definitions");
            c.That(!game.World.Services.Has<IEconomy>(), "batching/physical work require no money module");
            c.Equal(7, game.Seat(0).View.FeatureRegistrations.Single(r => r.Id == MachineFleetsModule.ModuleId).SchemaVersion, "one revised private fleet view");
            c.Equal(1, game.Seat(0).View.StateCodecs.Single(r => r.Kind == StateCodecKind.Service && r.Id == MachineFleetsModule.ModuleId).SchemaVersion, "membership codec unchanged");
            var requested = Settings(game, false, 3, GameDuration.Decimal("12.5", DurationUnit.Minute), true);
            Membership(c, game, new[] { native.Id, unsupported.Id }); ulong hash = game.Hash();
            var refused = View(game).PreviewLoadPolicy("delivery", requested);
            c.Equal(HaulLoadPolicyCodes.NotPolicy, refused.Blocker, "one no-policy member refuses entire set");
            c.That(refused.Entries.Single(e => e.Entity == unsupported.Id).Settings == null, "no fabricated provider settings");
            c.Equal(hash, game.Hash(), "whole refusal preview pure");
            c.Equal(HaulLoadPolicyCodes.NotPolicy, TestKit.Order(game, 0, Control(requested)).Refusal, "normal command shares refusal");
            c.That(HaulLoadPolicyContracts.Read(game.World, native).Enabled, "refused native prefix unchanged");
            Membership(c, game, ids.Reverse().ToArray()); hash = game.Hash(); var before = View(game).PreviewLoadPolicy("delivery", requested);
            c.That(before.Entries.Select(e => e.Entity).SequenceEqual(ids), "canonical actual member facts");
            c.That(before.Entries.All(e => e.Settings.Enabled && e.Settings.MinimumLoad.AmountOf(steel) == Fix.FromInt(8)), "actual authored initial settings");
            c.Throws<NotSupportedException>(() => ((IList<HaulLoadSelectionEntry>)before.Entries).Clear(), "selection immutable");
            c.Throws<NotSupportedException>(() => ((IList<ResourceAmount>)before.Entries[0].Settings.MinimumLoad.Lines).Clear(), "native minimum bill immutable");
            foreach (var malformed in new[] { null, Array.Empty<EntityId>(), new[] { ids[0], ids[0] }, new[] { ids[0], EntityId.None } })
                c.Equal(Codes.InvalidArgument, HaulLoadPolicyContracts.PreviewGroup(game.World, new PlayerId(0), malformed, requested).Blocker, "malformed selection refused");
            c.Equal(Codes.NotSeated, HaulLoadPolicyContracts.PreviewGroup(game.World, PlayerId.None, ids, requested).Blocker, "observer has no group authority");
            var hidden = HaulLoadPolicyContracts.PreviewGroup(game.World, new PlayerId(0), new[] { foreign.Id }, requested);
            c.That(hidden.Blocker == Codes.NotFound && hidden.Entries.Single().Settings == null, "foreign settings omitted");
            c.Equal(Codes.NotFound, View(game, 1).PreviewLoadPolicy("delivery", requested).Blocker, "foreign named metadata private");
            c.Equal(Codes.NotFound, game.Seat(1).Preview(Control(requested)), "sender-stamped control sees only own fleet");
            foreach (var invalid in new[] { null, Settings(game, true, 9, GameDuration.Zero), Settings(game, true, 2, GameDuration.Days(long.MaxValue)), Settings(game, true, 2, GameDuration.Zero, profile: "missing") })
                c.Equal(HaulLoadPolicyCodes.Invalid, View(game).PreviewLoadPolicy("delivery", invalid).Blocker, "missing/rule/capacity/time settings refuse");
            c.Equal(hash, game.Hash(), "all checks preserve settings/clock/claims");
            c.That(TestKit.Order(game, 0, new ConfigureFleetCommand { Fleet = "empty", Settings = new FleetSettings("fleet.haul", Array.Empty<EntityId>()) }).Accepted, "explicit empty fleet");
            c.Equal(FleetCodes.Empty, View(game).PreviewLoadPolicy("empty", requested).Blocker, "empty set invents no carrier");
            var pending = Control(requested); int sequence = game.Seat(0).Submit(pending); hash = game.Hash(); pending.Fleet = "changed"; pending.Settings = null;
            c.Equal(hash, game.Hash(), "pending intent copied away from caller"); var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending complete batching settings save");
            Update(game, twin); c.Equal(game.Hash(), twin.Hash(), "saved pending control continues identically");
            var results = new List<CommandResult>(); long cursor = 0; game.Seat(0).ReadResults(ref cursor, results);
            c.That(results.Single(r => r.Sequence == sequence).Accepted && results.Single(r => r.Sequence == sequence).Arg1 == 2, "two actual changed policy owners");
            foreach (var actor in actors)
            {
                var actual = HaulLoadPolicyContracts.Read(game.World, actor);
                c.That(!actual.Enabled && actual.Profile == "fleet.batch" && actual.MinimumLoad.AmountOf(steel) == Fix.FromInt(3) && actual.MaximumWait.Equals(requested.MaximumWait) && actual.EmergencyBypass, "all exact settings on actual native/public owner");
                c.That(actor.Get<IWorker>().Held && actor.Get<IWorker>().Job == 0 && WorkerHomeContracts.ReadHome(game.World, actor) == source.Id, "batch edit preserves original held/home/job state");
            }
            c.That(before.Entries.All(e => e.Settings.Enabled), "previous detached snapshots stay frozen");
            var versions = actors.Select(e => e.Version).ToArray(); var same = TestKit.Order(game, 0, Control(requested));
            c.That(same.Accepted && same.Arg0 == 2 && same.Arg1 == 0, "identical settings no changed owners"); c.That(actors.Select(e => e.Version).SequenceEqual(versions), "identical settings no Touch");
            var routeResult = TestKit.Order(game, 0, new AddRouteCommand { From = source.Id, To = target.Id, Resource = steel, Settings = new LogisticsRouteSettings(true, Fix.FromInt(3), 100) });
            c.That(routeResult.Accepted, "normal three-unit physical request"); int route = checked((int)routeResult.Arg0); long created = Route(game, route).CreatedTick;
            Held(c, game, external.Id, false); c.Equal(HaulLoadPolicyCodes.Disabled, Offer(game, route).Haul.Blocker, "disabled current intake blocks real dispatch");
            for (int i = 0; i < 3; i++) Update(game); c.That(Shipments(game).Length == 0, "disabled policy creates no fictional cargo job");
            Apply(c, game, Settings(game, true, 8, GameDuration.Minutes(10), true));
            c.Equal(HaulLoadPolicyCodes.Minimum, Offer(game, route).Haul.Blocker, "short real offer waits for original age threshold");
            c.Equal(created, Route(game, route).CreatedTick, "edit did not reset request origin"); c.Equal(created, Route(game, route).LastDispatchTick, "waiting retains original request dispatch coordinate");
            c.Equal(Fix.FromInt(20), source.Get<IInventory>().Amount(steel), "waiting consumes no source cargo");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "waiting request age and native/public policies saved");
            game.Clock.Pause(); twin.Clock.Pause(); long paused = game.Clock.StepIndex; Update(game, twin); c.Equal(paused, game.Clock.StepIndex, "one clock pause freezes deadline"); game.Clock.Resume(); twin.Clock.Resume();
            Reach(game, () => Shipments(game).Length == 1 && Shipments(game)[0].Worker.IsValid, "actual max-wait dispatch", twin);
            var first = Shipments(game)[0]; long admitted = Route(game, route).LastDispatchTick;
            c.That(admitted - created >= 10 && first.Amount == Fix.FromInt(3) && first.Worker == external.Id, "original deadline admits short physical public shipment");
            c.Equal(created, Route(game, route).CreatedTick, "successful dispatch retains original request identity/age");
            ConfigureRoute(c, game, route, false, 3, twin);
            Reach(game, () => first.Loaded.IsPositive && first.Loaded < first.Amount, "public partial timed load", twin);
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial public cargo/claims/policy saved");
            var destinationClaim = first.InReservation; var worker = first.Worker; int job = first.Id;
            Apply(c, game, Settings(game, false, 8, GameDuration.Minutes(100)), twin);
            c.That(first.Id == job && first.Worker == worker && first.InReservation.Equals(destinationClaim) && first.IsLive, "disabled future intake retains original acquired job/worker/claim");
            c.Equal(admitted, Route(game, route).LastDispatchTick, "fleet control does not reset successful dispatch history");
            c.That(TestKit.Order(game, 0, new RemoveFleetCommand { Fleet = "delivery" }).Accepted, "remove metadata while loading real cargo");
            c.That(TestKit.Order(twin, 0, new RemoveFleetCommand { Fleet = "delivery" }).Accepted, "same twin metadata removal");
            Reach(game, () => first.Loaded == first.Amount && first.Toils[first.ToilIndex].Executor == WorkPhases.Travel && first.Toils[first.ToilIndex].At == target.Id, "loaded public cargo must travel", twin);
            c.Equal(Fix.Zero, target.Get<IInventory>().Amount(steel), "policy edit/metadata removal no instant delivery");
            Reach(game, () => first.State == JobState.Done, "actual public delivery/departure despite disabled intake", twin);
            c.Equal(Fix.FromInt(3), target.Get<IInventory>().Amount(steel), "real short public delivery"); c.Equal(game.Hash(), twin.Hash(), "saved public delivery identity");
            Reach(game, () => external.Get<ITilePresence>().Tile.X < 29, "public physical return clears destination exit", twin);
            Held(c, game, external.Id, true, twin); Membership(c, game, new[] { native.Id }); Membership(c, twin, new[] { native.Id });
            var odd = game.World.Content.Get<IHaulLoadProfile>("fleet.odd").Initial; Apply(c, game, odd, twin);
            c.That(HaulLoadPolicyContracts.Read(game.World, native).Profile == "fleet.odd" && !HaulLoadPolicyContracts.Read(game.World, external).Enabled, "current membership edits native only, no fleet override");
            ConfigureRoute(c, game, route, true, 2, twin); Held(c, game, native.Id, false, twin);
            c.Equal(new Code("proof.odd-load"), Offer(game, route).Haul.Blocker, "public authored rule affects original dispatcher with no scheduler edit");
            c.That(!Shipments(game).Any(j => j.IsLive), "alternate even-load refusal creates no second live job");
            c.Equal(admitted, Route(game, route).LastDispatchTick, "rule/profile edit keeps prior successful origin");
            ConfigureRoute(c, game, route, true, 3, twin);
            Reach(game, () => Shipments(game).Any(j => j.Id != first.Id && j.IsLive && j.Worker.IsValid), "odd native short shipment", twin);
            var second = Shipments(game).Single(j => j.Id != first.Id && j.IsLive); c.That(second.Worker == native.Id && second.Amount == Fix.FromInt(3), "native actual policy runs independent authored rule");
            ConfigureRoute(c, game, route, false, 3, twin);
            Reach(game, () => second.Loaded == second.Amount && second.Toils[second.ToilIndex].Executor == WorkPhases.Travel && second.Toils[second.ToilIndex].At == target.Id, "native full physical trip", twin);
            var secondClaim = second.InReservation; Apply(c, game, Settings(game, false, 8, GameDuration.Minutes(100)), twin);
            c.That(second.InReservation.Equals(secondClaim) && second.Worker == native.Id && second.IsLive, "native busy intake edit retains actual commitment");
            c.Equal(game.World.Content.Get<IEntityStateDefinition>("survey.moving").State, native.State, "disabled batching no longer mislabels real native movement as stopped");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "native loaded disabled intake/moving state saved");
            Reach(game, () => second.State == JobState.Done, "native retained delivery/departure", twin);
            c.Equal(Fix.FromInt(6), target.Get<IInventory>().Amount(steel), "both actual native/public timed deliveries"); c.Equal(Fix.FromInt(14), source.Get<IInventory>().Amount(steel), "physical source remainder preserved");
            c.Equal(Fix.FromInt(20), Total(game), "batching adds no cargo/accounting store"); c.That(actors.All(e => e.Get<IInventory>().Amount(steel).IsZero), "actual completed unloading empties cargo");
            c.Equal(game.Hash(), twin.Hash(), "saved original physical/policy owners finish identically"); c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "final roundtrip");
            var isolated = LiveSensorTests.New(factory); c.That(!View(isolated).TryRead("delivery", out _), "interleaved sessions share no fleet");
            c.That(HaulLoadPolicyContracts.Read(isolated.World, Find(isolated, "fleet.load-public")).Enabled, "interleaved public provider owns JSON initial settings");
            c.That(!LiveSensorTests.New(FleetNavigationTests.Factory()).World.Services.Has<IMachineFleets>(), "omitted named module grants no hidden batching control");
            Invalid(c);
        }
        private static void Invalid(Check c)
        {
            foreach (var mode in new[] { "missing-wait", "missing-minimum", "extra", "over-capacity", "missing-profile" })
            {
                var doc = Content.Json.Parse(FrameworkProofContent.Read("native-fleet-load-policies.json")); var profile = doc.Get("haul_load_profiles")[0];
                if (mode == "missing-wait") profile.Remove("maximum_wait"); if (mode == "missing-minimum") profile.Remove("minimum_load");
                if (mode == "extra") profile.Set("instant_delivery", JsonValue.Of(true)); if (mode == "over-capacity") profile.Set("minimum_load", Content.Json.Parse("{\"steel\":9}"));
                if (mode == "missing-profile") doc.Get("entities")[0].Get("components").Get("haul_load_policy").Set("profile", JsonValue.Of("missing"));
                c.Throws<ContentException>(() => LiveSensorTests.New(FleetNavigationTests.Factory(named: true, loadControls: true, loadPolicies: doc.ToString())), "strict original master-selected JSON refuses " + mode);
            }
            var defect = Content.Json.Parse(FrameworkProofContent.Read("fleet-load-proof.json"));
            defect.Get("entities")[0].Get("components").Get("proof_fleet_load").Set("fault", Content.Json.Parse("4"));
            var game = LiveSensorTests.New(FleetNavigationTests.Factory(named: true, loadControls: true, proofLoad: defect.ToString()));
            c.Throws<InvalidOperationException>(() => HaulLoadPolicyContracts.PreviewGroup(game.World, new PlayerId(0), new[] { Find(game, "fleet.load-public").Id }, Settings(game, false, 2, GameDuration.Zero)), "public mutating CheckLoadPolicy faults pure whole admission");
            c.That(game.Fault != null, "actual scoped provider observation fail-stops session"); c.Throws<InvalidOperationException>(() => game.Save(), "mutated provider cannot checkpoint healthy state");
        }
    }
}
