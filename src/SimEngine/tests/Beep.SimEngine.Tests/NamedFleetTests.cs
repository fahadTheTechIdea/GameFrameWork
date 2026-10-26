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
    internal static class NamedFleetTests
    {
        internal const string Case = "Named fleets: authored membership, whole controls and physical saved hauling";
        internal const string FaultCase = "Named fleet views: retained faults reject empty private and missing controls";
        internal static void Register()
        {
            TestRunner.Add(Case, Run);
            TestRunner.Add(FaultCase, c => { Run(c); RetainedFaults(c); });
        }
        private static FleetView View(SimGame game, int company = 0) => game.Seat(company).View.Feature<FleetView>();
        private static Entity Find(SimGame game, string id) => game.World.Entities.All.Single(e => e.Alive && e.Def.Id == id);
        private static Entity[] Actors(SimGame game) => game.World.Entities.All.Where(e => e.Alive && e.Def.Id == "survey.machine").OrderBy(e => e.Id).ToArray();
        private static Job[] Shipments(SimGame game) => game.World.Services.Get<WorkBoard>().Jobs.Where(j => j.IsHaul).OrderBy(j => j.Id).ToArray();
        private static ConfigureFleetCommand Configure(string id, string policy, IReadOnlyList<EntityId> members)
            => new ConfigureFleetCommand { Fleet = id, Settings = new FleetSettings(policy, members) };
        private static ConfigureFleetNavigationCommand Navigate(string profile) => new ConfigureFleetNavigationCommand { Fleet = "delivery", Profile = profile };
        private static string Profiles(Entity[] actors) => string.Join(",", actors.Select(e => e.Get<IMotionNavigation>().ReadNavigation()));
        private static Fix Total(SimGame game)
        {
            var resource = game.World.Content.ResourceId("steel"); Fix total = Fix.Zero;
            foreach (var entity in game.World.Entities.All) if (entity.Get<IInventory>() is IInventory inventory) total += inventory.Amount(resource);
            return total;
        }
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("named fleet clock=" + game.Clock.StepIndex + " " + game.Fault);
            if (Total(game) != Fix.FromInt(20)) throw new InvalidOperationException("named fleet stock conservation clock=" + game.Clock.StepIndex);
            if (twin != null) Update(twin);
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 1500 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " clock=" + game.Clock.StepIndex + " machines=" + string.Join(";", Actors(game).Select(e =>
                e.Id + "/" + e.Get<IMotion>().Status + "/" + e.Get<ITilePresence>().Position)) + " jobs=" +
                string.Join(";", Shipments(game).Select(j => j.Id + "/" + j.State + "/" + j.ToilIndex + "/" + j.Loaded + "/" + j.Blocker)));
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "exact native named-fleet identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "optional self-described service needs no legacy save reader");
            var factory = FleetNavigationTests.Factory(named: true); var game = LiveSensorTests.New(factory);
            var limits = new ReplayLimits(4 * 1024 * 1024, 2000);
            using var recording = game.RecordReplay(limits);
            var actors = Actors(game); var ids = actors.Select(e => e.Id).ToArray(); var reversed = ids.Reverse().ToArray();
            var limited = Find(game, "fleet.restricted"); var foreign = Find(game, "survey.foreign-sensor");
            var source = Find(game, "survey.site"); var target = Find(game, "fleet.destination"); var view = View(game);
            var fleets = game.World.Services.Get<IMachineFleets>(); var steel = game.World.Content.ResourceId("steel");
            c.That(ReferenceEquals(game.World, fleets.World), "one session-owned fleet service");
            c.Equal(1, game.Seat(0).View.StateCodecs.Count(s => s.Kind == StateCodecKind.Service && s.Id == MachineFleetsModule.ModuleId && s.SchemaVersion == 1), "one self-described membership codec");
            c.That(!game.World.Services.Has<IEconomy>(), "physical machine fleets do not require business");
            foreach (var actor in actors.Concat(new[] { limited })) c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = actor.Id, Held = true }).Accepted, "normal hold while authoring membership");
            c.Throws<ArgumentException>(() => new FleetSettings("fleet.haul", new[] { ids[0], ids[0] }), "duplicate member is never silently deduplicated");
            c.Throws<ArgumentException>(() => new FleetSettings("fleet.haul", new[] { EntityId.None }), "invalid member is never substituted");
            var settings = new FleetSettings("fleet.haul", reversed); reversed[0] = limited.Id;
            c.That(settings.Members.SequenceEqual(ids), "settings own canonical detached membership");
            c.Throws<NotSupportedException>(() => ((IList<EntityId>)settings.Members).Clear(), "membership immutable");
            var policies = new List<FleetPolicySnapshot>(); view.CopyPolicies(policies);
            c.That(policies.Select(p => p.Id).SequenceEqual(new[] { "fleet.haul", "fleet.proof", "fleet.road", "fleet.survey" }), "private deterministic native/mod policy discovery");
            c.That(policies.Single(p => p.Id == "fleet.haul").ContentDependencies.SequenceEqual(new[] { "haul" }), "roles remain actual catalog dependencies");
            c.Throws<NotSupportedException>(() => ((IList<string>)policies[0].NavigationClasses).Clear(), "policy navigation facts immutable");
            c.That(game.World.Content.Get<IFleetPolicy>("fleet.proof") is RequiredNavigationFleetDef, "public external policy selected from original master");
            ulong hash = game.Hash();
            c.Equal(Code.None, view.PreviewConfigure("delivery", settings, out var prepared), "whole native management admission");
            c.Equal(Code.None, game.Seat(0).Preview(new ConfigureFleetCommand { Fleet = "delivery", Settings = settings }), "normal membership preview parity");
            c.Equal(Codes.InvalidArgument, game.Seat(0).Preview(new ConfigureFleetCommand { Fleet = "delivery" }), "absent settings are explicit refusal");
            c.Equal(Codes.InvalidArgument, view.PreviewConfigure("invalid id", settings, out _), "invalid stable fleet identity refused");
            c.Equal(Codes.UnknownDef, view.PreviewConfigure("delivery", new FleetSettings("missing", ids), out _), "no invented policy default");
            c.Equal(Codes.NotFound, view.PreviewConfigure("delivery", new FleetSettings("fleet.haul", new[] { ids[0], foreign.Id }), out var hidden), "private admission conceals foreign member");
            c.That(hidden.Members.Single(m => m.Entity == foreign.Id).Navigation == null, "foreign navigation never disclosed");
            c.Equal(Codes.NotOwned, game.Seat(0).Preview(Configure("delivery", "fleet.haul", new[] { ids[0], foreign.Id })), "normal sender ownership refusal");
            c.Equal(Codes.NotFound, view.PreviewConfigure("delivery", new FleetSettings("fleet.haul", new[] { ids[0], new EntityId(9999) }), out _), "missing member typed refusal");
            c.Equal(MotionNavigationCodes.Unsupported, view.PreviewConfigure("delivery", new FleetSettings("fleet.haul", new[] { source.Id }), out _), "actual non-motion entity cannot become fleet vehicle");
            c.Equal(FleetCodes.MemberNotAllowed, view.PreviewConfigure("delivery", new FleetSettings("fleet.survey", ids), out _), "native compatible role is authored, not a coordinator switch");
            c.Equal(Codes.NotSeated, fleets.PreviewConfigure(new PlayerId(8), "delivery", settings, out _), "actual seat required");
            c.Equal(hash, game.Hash(), "policy/member discovery and rejected previews pure");
            var left = new StateHasher(); Configure("delivery", "fleet.haul", ids).Sync(left); var right = new StateHasher(); Configure("delivery", "fleet.haul", ids.Reverse().ToArray()).Sync(right);
            c.Equal(left.Value, right.Value, "opposite caller order hashes to same command");
            var command = new ConfigureFleetCommand { Fleet = "delivery", Settings = settings }; int sequence = game.Seat(0).Submit(command);
            hash = game.Hash(); command.Fleet = "changed"; command.Settings = null; c.Equal(hash, game.Hash(), "pending command ignores caller mutation");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending creation save hash"); Update(game, twin);
            var results = new List<CommandResult>(); long cursor = 0; game.Seat(0).ReadResults(ref cursor, results);
            c.That(results.Single(r => r.Sequence == sequence).Accepted, "one normal command creates full fleet");
            c.That(view.TryRead("delivery", out var stored) && stored.Settings.Members.SequenceEqual(ids), "engine retains stable name and canonical actual members");
            c.Equal(game.Hash(), twin.Hash(), "pending fleet creation saved twins");
            var list = new List<FleetSnapshot>(); View(game, 1).CopyFleets(list); c.Equal(0, list.Count, "foreign company fleet list private");
            c.That(!View(game, 1).TryRead("delivery", out _), "same name does not cross company boundary");
            c.Equal(Codes.NotFound, game.Seat(1).Preview(Navigate("terrain.route")), "normal named target resolves actual sender fleet only");
            c.Equal(FleetCodes.MemberConflict, view.PreviewConfigure("another", new FleetSettings("fleet.haul", new[] { ids[0], limited.Id }), out _), "existing actual member prevents complete second fleet");
            var failed = TestKit.Order(game, 0, Configure("another", "fleet.haul", new[] { ids[0], limited.Id }));
            c.That(!failed.Accepted && failed.Refusal == FleetCodes.MemberConflict && !view.TryRead("another", out _), "no partial new fleet on conflict");
            c.That(TestKit.Order(game, 0, Configure("delivery", "fleet.haul", ids.Concat(new[] { limited.Id }).ToArray())).Accepted, "whole membership edit can include restricted mover");
            c.Equal(MotionNavigationCodes.NotAllowed, view.PreviewNavigation("delivery", "terrain.route").Blocker, "one actual restricted mover refuses complete named control");
            failed = TestKit.Order(game, 0, Navigate("terrain.route"));
            c.That(!failed.Accepted && failed.Refusal == MotionNavigationCodes.NotAllowed, "normal whole named navigation refusal");
            c.Equal("survey.route,survey.route", Profiles(actors), "no valid navigation prefix committed");
            failed = TestKit.Order(game, 0, Configure("delivery", "fleet.proof", ids.Concat(new[] { limited.Id }).ToArray()));
            c.That(!failed.Accepted && failed.Refusal == RequiredNavigationFleetDef.Incompatible, "independent public policy rejects actual unsupported member");
            c.That(view.TryRead("delivery", out var retained) && retained.Settings.Profile == "fleet.haul" && retained.Members.Count == 3, "rejected policy replacement preserves whole original membership");
            c.That(TestKit.Order(game, 0, Configure("delivery", "fleet.road", ids)).Accepted, "native profile and membership edit together");
            c.Equal(MotionNavigationCodes.NotAllowed, view.PreviewNavigation("delivery", "survey.route").Blocker, "fleet policy narrows actual compatible navigation");
            c.That(TestKit.Order(game, 0, Navigate("terrain.route")).Accepted, "named command delegates to original whole motion selection");
            c.Equal("terrain.route,terrain.route", Profiles(actors), "native mover alone owns selected values");
            c.That(TestKit.Order(game, 0, Configure("delivery", "fleet.proof", new[] { ids[0] })).Accepted, "public policy admits compatible actual member");
            c.That(TestKit.Order(game, 0, Navigate("survey.route")).Accepted, "normal named control resolves edited current set");
            c.Equal("survey.route,terrain.route", Profiles(actors), "removed member never silently retained as control target");
            c.That(TestKit.Order(game, 0, Configure("delivery", "fleet.proof", ids)).Accepted, "same original coordinator supports external policy and complete set");
            c.That(TestKit.Order(game, 0, Navigate("terrain.route")).Accepted, "normal complete mod fleet navigation");
            c.That(prepared.Members.All(m => m.Navigation.Profile == "survey.route"), "retained earlier member diagnostics stay frozen");
            c.That(TestKit.Order(game, 0, Configure("empty", "fleet.haul", Array.Empty<EntityId>())).Accepted, "explicit named empty fleet permitted");
            c.Equal(FleetCodes.Empty, view.PreviewNavigation("empty", "terrain.route").Blocker, "empty fleet cannot invent workers");
            c.That(TestKit.Order(game, 0, new RemoveFleetCommand { Fleet = "empty" }).Accepted, "normal metadata removal");
            c.Equal(Codes.NotFound, view.PreviewRemove("empty"), "removed identity does not remain alias");
            var idleVersions = actors.Select(e => e.Version).ToArray();
            c.That(TestKit.Order(game, 0, Configure("delivery", "fleet.proof", ids.Reverse().ToArray())).Accepted, "same-value membership idempotent");
            c.That(actors.Select(e => e.Version).SequenceEqual(idleVersions), "membership never touches actual entity execution");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "edited membership saves with original current motion values");

            var route = TestKit.Order(game, 0, new AddRouteCommand { From = source.Id, To = target.Id, Resource = steel, Settings = new LogisticsRouteSettings(true, Fix.FromInt(8), 100) });
            c.That(route.Accepted, "normal route posts physical original cargo work");
            foreach (var actor in actors) c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = actor.Id, Held = false }).Accepted, "player releases real native machines");
            Reach(game, () => Shipments(game).Length == 2, "native two-carrier acquisition");
            c.That(TestKit.Order(game, 0, new ConfigureRouteCommand { Route = checked((int)route.Arg0), Settings = new LogisticsRouteSettings(false, Fix.FromInt(8), 100) }).Accepted, "stop future dispatch without cancelling acquired work");
            Reach(game, () => Shipments(game).Any(j => j.Loaded.IsPositive && j.Loaded < j.Amount), "native partial dock load");
            var shipment = Shipments(game).First(j => j.Loaded.IsPositive); var carrier = game.World.Entities.Get(shipment.Worker);
            c.That(view.TryRead("delivery", out stored) && stored.Settings.Members.SequenceEqual(ids), "physical jobs do not replace stable fleet membership");
            c.Equal(Fix.Zero, target.Get<IInventory>().Amount(steel), "fleet metadata cannot deliver stock instantly");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial physical loading retains membership and original claims");
            game.Clock.Pause(); twin.Clock.Pause(); long paused = game.Clock.StepIndex; Update(game, twin);
            c.Equal(paused, game.Clock.StepIndex, "sole clock pause stops work and fleet command application");
            c.Equal(game.Hash(), twin.Hash(), "equal explicit local pacing preserves saved twin"); game.Clock.Resume(); twin.Clock.Resume();
            Reach(game, () => shipment.Loaded == shipment.Amount && carrier.Get<IMotion>().Status == MoveStatus.Moving &&
                shipment.Toils[shipment.ToilIndex].Executor == WorkPhases.Travel && shipment.Toils[shipment.ToilIndex].At == target.Id, "loaded physical departure", twin);
            var mover = carrier.Get<Mover>(); var goal = mover.Goal; long moving = mover.TicksMoving; var incoming = shipment.InReservation; var cargo = shipment.CargoOutReservation;
            c.That(TestKit.Order(game, 0, Navigate("survey.route")).Accepted, "normal named route change while carrying actual stock");
            c.That(TestKit.Order(twin, 0, Navigate("survey.route")).Accepted, "saved twin receives same named route change");
            c.Equal(goal, mover.Goal, "route change preserves physical destination"); c.Equal(moving + 1, mover.TicksMoving, "physical travel age advances normally without reset");
            c.Equal(incoming, shipment.InReservation, "named control retains actual receiving receipt"); c.Equal(cargo, shipment.CargoOutReservation, "named control retains actual loaded receipt");
            c.Equal(game.Hash(), twin.Hash(), "loaded navigation continues identical saved state");
            int job = shipment.Id; var membersBefore = stored.Settings.Members;
            c.That(TestKit.Order(game, 0, new RemoveFleetCommand { Fleet = "delivery" }).Accepted, "remove metadata during live physical work");
            c.That(TestKit.Order(twin, 0, new RemoveFleetCommand { Fleet = "delivery" }).Accepted, "saved twin removes metadata only");
            c.That(!view.TryRead("delivery", out _) && shipment.Id == job && shipment.Worker == carrier.Id, "removal does not cancel/reassign physical shipment");
            c.Equal(incoming, shipment.InReservation, "removal does not release actual receiving commitment"); c.Equal(cargo, shipment.CargoOutReservation, "removal does not release loaded cargo commitment");
            c.Equal(2, membersBefore.Count, "retained snapshot survives actual removal");
            twin = factory.Load(game.Save()); Reach(game, () => Shipments(game).All(j => j.State == JobState.Done), "saved physical deliveries after fleet removal", twin);
            c.Equal(Fix.FromInt(16), target.Get<IInventory>().Amount(steel), "two actual deliveries consume physical time and stock");
            c.Equal(Fix.FromInt(4), source.Get<IInventory>().Amount(steel), "actual source retains remaining stock"); c.Equal(Fix.FromInt(20), Total(game), "no fleet cargo copy/loss");
            c.That(actors.All(e => e.Get<IInventory>().Amount(steel).IsZero), "original unload clears real cargo"); c.Equal(game.Hash(), twin.Hash(), "physical saved twins complete with sole original owners");
            var isolated = LiveSensorTests.New(factory); c.That(!View(isolated).TryRead("delivery", out _), "interleaved session borrows no named membership");
            var omitted = LiveSensorTests.New(FleetNavigationTests.Factory()); c.That(!omitted.World.Services.Has<IMachineFleets>() && View(omitted) == null, "module omission has no hidden fleet defaults");
            var replay = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
            c.That(replay.Success && replay.ActualHash == game.Hash(), "complete initial named-fleet commands and physical deliveries replay to final hash");
            Console.WriteLine("named fleet physical replay: initial=" + recording.InitialStep + ";final=" + game.Tick
                + ";commands=" + recording.CommandCount + ";checkpoints=" + recording.CheckpointCount
                + ";expected-hash=" + game.Hash().ToString("X16") + ";actual-hash=" + replay.ActualHash.ToString("X16"));
            Invalid(c);
        }
        private static void RetainedFaults(Check c)
        {
            const string marker = "proof.fleet.cached-view.failure";
            int calls = 0;
            var probe = new ProbeModule("proof.fleet-view-fault", new ModuleVersion(1, 0, 0), compose: composer =>
                composer.AddViewFeature<ProbeView>("proof.fleet-view-fault", 1, (world, viewer) =>
                { calls++; throw new InvalidOperationException(marker); }));
            var declarations = Json.Parse("[{\"id\":\"proof.fleet-view-fault\",\"version\":\"1.0.0\",\"documents\":[]}]");
            var factory = FleetNavigationTests.Factory(named: true, additionalModules: new[] { probe },
                additionalDeclarations: declarations,
                worldEdit: world => world.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(probe.Id)));
            var idle = LiveSensorTests.New(factory); ulong untouched = idle.Hash();
            foreach (bool populated in new[] { false, true })
            foreach (bool restored in new[] { false, true })
            {
                var game = LiveSensorTests.New(factory);
                var settings = new FleetSettings("fleet.haul", Actors(game).Select(e => e.Id).ToArray());
                if (populated) c.That(TestKit.Order(game, 0, Configure("delivery", settings.Profile, settings.Members)).Accepted,
                    "ordinary player command creates actual membership before fault");
                if (restored) game = factory.Load(game.Save());
                var own = View(game); var foreign = View(game, 1); var observer = game.Observer.Feature<FleetView>();
                var policies = new List<FleetPolicySnapshot>(); own.CopyPolicies(policies); var retainedPolicy = policies[0];
                c.Equal(Code.None, own.PreviewConfigure("delivery", settings, out var retained), "healthy native fleet preview remains usable");
                c.Equal(populated, own.TryRead("delivery", out _), "healthy membership reflects actual empty or configured service");
                var rows = new List<FleetSnapshot> { retained }; foreign.CopyFleets(rows);
                c.Equal(0, rows.Count, "healthy foreign enumeration clears caller's private rows");
                ulong before = game.Hash(); own.CopyPolicies(policies);
                c.Equal(before, game.Hash(), "healthy fleet policy observation remains pure");
                long at = game.Tick; int previousCalls = calls;
                // Negative extension protocol only: the original registered view callback owns fault latching.
                c.Throws<InvalidOperationException>(() => game.Seat(0).View.Feature<ProbeView>(), "real declared provider causes terminal session fault");
                var first = game.Fault;
                c.That(first != null && first.Phase == "observation.provider" && first.Message.Contains(marker), "original provider cause and phase observable");
                foreach (var view in new[] { own, foreign, observer, new FleetView(game.World, new PlayerId(-2)), new FleetView(game.World, new PlayerId(99)) })
                {
                    rows.Clear(); rows.Add(retained);
                    c.Throws<InvalidOperationException>(() => view.CopyFleets(rows), "terminal fleet list rejects owned private observer or invalid viewer");
                    c.Equal(0, rows.Count, "terminal fleet copy clears retained rows");
                    policies.Clear(); policies.Add(retainedPolicy);
                    c.Throws<InvalidOperationException>(() => view.CopyPolicies(policies), "terminal policy list rejects before empty or private shortcut");
                    c.Equal(0, policies.Count, "terminal policy copy clears retained rows");
                    foreach (string id in new[] { "delivery", "missing", null })
                    {
                        c.Throws<InvalidOperationException>(() => view.TryRead(id, out _), "terminal fleet read rejects before missing identity");
                        c.Throws<InvalidOperationException>(() => view.PreviewConfigure(id, settings, out _), "terminal membership preview rejects before admission");
                        c.Throws<InvalidOperationException>(() => view.PreviewRemove(id), "terminal removal preview rejects before missing or private shortcut");
                        c.Throws<InvalidOperationException>(() => view.PreviewNavigation(id, "missing"), "terminal navigation rejects before missing profile");
                        c.Throws<InvalidOperationException>(() => view.PreviewHome(id, EntityId.None), "terminal home rejects before unsupported member");
                        c.Throws<InvalidOperationException>(() => view.PreviewOutletPolicy(id, null), "terminal outlet rejects before absent policy");
                        c.Throws<InvalidOperationException>(() => view.PreviewRecurringPlan(id, Code.None, null), "terminal service rejects before absent settings");
                        c.Throws<InvalidOperationException>(() => view.PreviewTripPolicy(id, null), "terminal trip rejects before absent settings");
                        c.Throws<InvalidOperationException>(() => view.PreviewLoadPolicy(id, null), "terminal load rejects before absent settings");
                        c.Throws<InvalidOperationException>(() => view.PreviewWorkPolicy(id, null), "terminal work rejects before absent settings");
                    }
                }
                // Direct original service checks are framework diagnostics, never a host gameplay path.
                var source = game.World.Services.Get<IMachineFleets>();
                c.Throws<InvalidOperationException>(() => source.CheckRemove(new PlayerId(99), null), "original service cutoff precedes company refusal");
                c.Throws<InvalidOperationException>(() => source.TryRead(PlayerId.None, null, out _), "original source cutoff precedes empty lookup");
                c.Throws<InvalidOperationException>(() => source.PreviewConfigure(PlayerId.None, null, null, out _), "original source cutoff precedes invalid configure");
                c.Throws<InvalidOperationException>(() => game.Save(), "terminal fleet session cannot checkpoint");
                c.Throws<InvalidOperationException>(() => game.Hash(), "terminal fleet session cannot publish healthy hash");
                c.Throws<InvalidOperationException>(() => game.Seat(0).View.Feature<ProbeView>(), "first fault prevents reentering failed callback");
                c.Equal(0, game.Update(), "terminal fleet session does not advance");
                c.That(ReferenceEquals(first, game.Fault) && game.Tick == at && calls == previousCalls + 1,
                    "retained fleet queries preserve first error and sole clock without invoking callback again");
                c.That(retained.Settings.Members.SequenceEqual(settings.Members), "earlier detached membership remains immutable after terminal reads");
                c.Equal(untouched, idle.Hash(), "faulted fleet observations leave independent idle session unchanged");
            }
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "roles", "empty-roles", "duplicate-role", "unknown-role", "bad-role", "classes", "empty-classes", "duplicate-class", "bad-class", "number-class", "extra" })
            {
                var doc = Content.Json.Parse(FrameworkProofContent.Read("native-named-fleets.json")); var policy = doc.Get("fleet_policies")[0];
                if (mode == "roles") policy.Remove("allowed_roles"); if (mode == "empty-roles") policy.Set("allowed_roles", JsonValue.NewArray());
                if (mode == "duplicate-role") policy.Get("allowed_roles").Add(JsonValue.Of("haul")); if (mode == "unknown-role") policy.Set("allowed_roles", Content.Json.Parse("[\"missing\"]"));
                if (mode == "bad-role") policy.Set("allowed_roles", Content.Json.Parse("[\"invalid role\"]")); if (mode == "classes") policy.Remove("navigation_classes");
                if (mode == "empty-classes") policy.Set("navigation_classes", JsonValue.NewArray()); if (mode == "duplicate-class") policy.Get("navigation_classes").Add(JsonValue.Of("terrain.route"));
                if (mode == "bad-class") policy.Set("navigation_classes", Content.Json.Parse("[\"invalid class\"]")); if (mode == "number-class") policy.Set("navigation_classes", Content.Json.Parse("[1]"));
                if (mode == "extra") policy.Set("instant_move", JsonValue.Of(true));
                c.Throws<ContentException>(() => LiveSensorTests.New(FleetNavigationTests.Factory(named: true, policies: doc.ToString())), "strict native fleet policy " + mode);
            }
            var proof = Content.Json.Parse(FrameworkProofContent.Read("fleet-policy-proof.json")); proof.Get("required_navigation_fleet_policies")[0].Set("required_navigation", JsonValue.Of("missing"));
            c.Throws<ContentException>(() => LiveSensorTests.New(FleetNavigationTests.Factory(named: true, proofPolicies: proof.ToString())), "public policy validation runs before initial Update");
        }
    }
}
