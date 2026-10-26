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
    internal static class FleetWorkPolicyTests
    {
        internal const string Case = "Fleet work policies: whole admission, physical cargo pause and saved mixed-provider delivery";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static Entity Find(SimGame game, string id) => game.World.Entities.All.Single(e => e.Def.Id == id);
        private static Entity Native(SimGame game) => game.World.Entities.All.Where(e => e.Def.Id == "survey.machine").OrderBy(e => e.Id).First();
        private static Job[] Shipments(SimGame game) => game.World.Services.Get<WorkBoard>().Jobs.Where(j => j.IsHaul).OrderBy(j => j.Id).ToArray();
        private static FleetView View(SimGame game, int company = 0) => game.Seat(company).View.Feature<FleetView>();
        internal static WorkPolicySettings Settings(bool enabled, int priority, string area, bool interruptible, bool emergency, WorkAssignmentMode mode)
            => new WorkPolicySettings(enabled, priority, new[] { new WorkerRoleId("haul") }, area, interruptible, emergency, mode);
        internal static ConfigureFleetWorkPolicyCommand Control(WorkPolicySettings settings) => new ConfigureFleetWorkPolicyCommand { Fleet = "delivery", Settings = settings };
        private static Fix Total(SimGame game)
        {
            var steel = game.World.Content.ResourceId("steel"); Fix total = Fix.Zero;
            foreach (var entity in game.World.Entities.All) if (entity.Get<IInventory>() is IInventory inventory) total += inventory.Amount(steel);
            return total;
        }
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("fleet policies clock=" + game.Clock.StepIndex + " " + game.Fault);
            if (Total(game) != Fix.FromInt(20)) throw new InvalidOperationException("fleet policy stock conservation clock=" + game.Clock.StepIndex);
            if (twin != null) Update(twin);
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 1500 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " clock=" + game.Clock.StepIndex + " jobs=" + string.Join(";",
                Shipments(game).Select(j => j.Id + "/" + j.State + "/" + j.Worker + "/" + j.Loaded + "/" + j.Blocker)) + " positions=" +
                string.Join(";", game.World.Entities.All.Where(e => e.Get<IWorker>() != null).Select(e => e.Id + "/" + e.Get<ITilePresence>()?.Position)));
        }
        private static void Membership(Check c, SimGame game, IReadOnlyList<EntityId> ids)
            => c.That(TestKit.Order(game, 0, new ConfigureFleetCommand { Fleet = "delivery", Settings = new FleetSettings("fleet.haul", ids) }).Accepted, "normal current fleet membership");
        private static void Apply(Check c, SimGame game, WorkPolicySettings settings, SimGame twin = null)
        {
            c.Equal(Code.None, View(game).PreviewWorkPolicy("delivery", settings).Blocker, "private preview shares original whole admission");
            var result = TestKit.Order(game, 0, Control(settings)); c.That(result.Accepted, "normal named policy control");
            if (twin != null)
            {
                var other = TestKit.Order(twin, 0, Control(settings)); c.That(other.Accepted, "saved twin receives same named intent");
                c.Equal(game.Hash(), twin.Hash(), "normal control continues identical saved owner state");
            }
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current whole work-policy identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no fleet policy overlay payload");
            var factory = FleetNavigationTests.Factory(named: true, workControls: true); var game = LiveSensorTests.New(factory);
            var native = Native(game); var external = Find(game, "fleet.public-carrier"); var unsupported = Find(game, "fleet.no-policy");
            var source = Find(game, "survey.site"); var target = Find(game, "fleet.destination"); var steel = game.World.Content.ResourceId("steel");
            var actors = new[] { native, external }.OrderBy(e => e.Id).ToArray(); var ids = actors.Select(e => e.Id).ToArray();
            foreach (var actor in game.World.Entities.All.Where(e => e.Owner == new PlayerId(0) && e.Get<IWorker>() != null))
                c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = actor.Id, Held = true }).Accepted, "ordinary player holds candidates while authoring intent");
            c.That(native.Get<IWorkPolicy>() is WorkPolicyComponent && external.Get<IWorkPolicy>() is ProofWorkPolicy, "actual mixed independent provider owners");
            c.That(external.Def.AuthoringPath.Contains("fleet-work-policy-proof.json"), "actual selected master JSON provides external worker");
            c.That(!game.World.Services.Has<IEconomy>(), "machine work controls have no business dependency");
            c.Equal(7, game.Seat(0).View.FeatureRegistrations.Single(r => r.Id == MachineFleetsModule.ModuleId).SchemaVersion, "one revised fleet view declaration");
            c.Equal(1, game.Seat(0).View.StateCodecs.Single(r => r.Kind == StateCodecKind.Service && r.Id == MachineFleetsModule.ModuleId).SchemaVersion, "membership codec unchanged");
            var authored = game.World.Content.Get<WorkPolicyDef>("fleet.controls"); var requested = authored.Initial;
            c.That(authored.AuthoringPath.Contains("native-fleet-work-policies.json"), "original strict JSON work settings, no fleet defaults model");
            Membership(c, game, new[] { native.Id, unsupported.Id }); ulong hash = game.Hash();
            var refused = View(game).PreviewWorkPolicy("delivery", requested);
            c.Equal(WorkPolicyCodes.NotPolicy, refused.Blocker, "one policy-less actual member refuses complete named set");
            c.Equal(Code.None, refused.Entries.Single(e => e.Entity == native.Id).Blocker, "valid member has truthful noncommitted admission");
            c.That(refused.Entries.Single(e => e.Entity == unsupported.Id).Settings == null, "no fabricated policy settings");
            c.Equal(hash, game.Hash(), "whole native preview is pure");
            var failed = TestKit.Order(game, 0, Control(requested));
            c.That(!failed.Accepted && failed.Refusal == WorkPolicyCodes.NotPolicy, "ordinary complete refusal");
            c.Equal(100, WorkPolicyRules.Read(native).Priority, "refused command changes no admitted prefix");
            Membership(c, game, ids.Reverse().ToArray()); hash = game.Hash();
            var before = View(game).PreviewWorkPolicy("delivery", requested);
            c.That(before.Entries.Select(e => e.Entity).SequenceEqual(ids), "canonical actual native/public member ordering");
            c.That(before.Entries.All(e => e.Settings.Priority == 100), "snapshots read original settings owners");
            c.Throws<NotSupportedException>(() => ((IList<WorkPolicySelectionEntry>)before.Entries).Clear(), "whole policy facts immutable");
            c.Throws<NotSupportedException>(() => ((IList<WorkerRoleId>)before.Entries[0].Settings.AllowedRoles).Clear(), "settings role facts immutable");
            foreach (var malformed in new[] { null, Array.Empty<EntityId>(), new[] { ids[0], ids[0] }, new[] { ids[0], EntityId.None } })
                c.Equal(Codes.InvalidArgument, WorkPolicyRules.PreviewGroup(game.World, new PlayerId(0), malformed, requested).Blocker, "malformed selection never substitutes members");
            c.Equal(Codes.NotSeated, WorkPolicyRules.PreviewGroup(game.World, new PlayerId(-2), ids, requested).Blocker, "invalid viewer gains no group authority");
            var foreign = Find(game, "survey.foreign-sensor");
            var privateFacts = WorkPolicyRules.PreviewGroup(game.World, new PlayerId(0), new[] { foreign.Id }, requested);
            c.That(privateFacts.Blocker == Codes.NotFound && privateFacts.Entries.Single().Settings == null, "foreign provider facts omitted");
            c.Equal(Codes.NotFound, View(game, 1).PreviewWorkPolicy("delivery", requested).Blocker, "named policy management company-private");
            c.Equal(Codes.NotFound, game.Seat(1).Preview(Control(requested)), "command uses actual sender membership");
            c.Equal(WorkPolicyCodes.InvalidSettings, View(game).PreviewWorkPolicy("delivery", null).Blocker, "null policy not default settings");
            c.Equal(WorkPolicyCodes.InvalidSettings, View(game).PreviewWorkPolicy("delivery", Settings(true, 1, "missing", true, true, WorkAssignmentMode.All)).Blocker, "unknown actual area refuses whole set");
            var unknownRole = new WorkPolicySettings(true, 1, new[] { new WorkerRoleId("missing") }, null, true, true, WorkAssignmentMode.All);
            c.Equal(WorkPolicyCodes.InvalidSettings, View(game).PreviewWorkPolicy("delivery", unknownRole).Blocker, "unknown selected role refuses whole set");
            c.Equal(hash, game.Hash(), "all preview/control diagnostics preserve state and time");
            c.That(TestKit.Order(game, 0, new ConfigureFleetCommand { Fleet = "empty", Settings = new FleetSettings("fleet.haul", Array.Empty<EntityId>()) }).Accepted, "explicit empty membership");
            c.Equal(FleetCodes.Empty, View(game).PreviewWorkPolicy("empty", requested).Blocker, "empty fleet cannot invent a machine");
            var pending = Control(requested); int sequence = game.Seat(0).Submit(pending); hash = game.Hash(); pending.Fleet = "mutated"; pending.Settings = null;
            c.Equal(hash, game.Hash(), "pending command copied away from caller");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending full policy command saved with original membership/owners");
            Update(game, twin); c.Equal(game.Hash(), twin.Hash(), "pending named policy command continues identically");
            var results = new List<CommandResult>(); long cursor = 0; game.Seat(0).ReadResults(ref cursor, results);
            c.That(results.Single(r => r.Sequence == sequence).Accepted && results.Single(r => r.Sequence == sequence).Arg1 == 2, "both actual owners report changed settings");
            foreach (var actor in actors)
            {
                var actual = WorkPolicyRules.Read(actor);
                c.That(actual.Enabled && actual.Priority == 77 && actual.Area == "fleet.source-only" && !actual.Interruptible && actual.EmergencyEligible && actual.AssignmentMode == WorkAssignmentMode.All && actual.Allows(new WorkerRoleId("haul")), "all requested fields retained by actual native/public policy owner");
                c.Equal(WorkPolicyCodes.OutsideArea, WorkPolicyRules.CheckDestination(game.World, actor, target), "actual original dispatch destination gate respects area");
            }
            c.That(before.Entries.All(e => e.Settings.Priority == 100 && e.Settings.Area == null), "held snapshots frozen after real controls");
            var versions = actors.Select(e => e.Version).ToArray(); var unchanged = TestKit.Order(game, 0, Control(requested));
            c.That(unchanged.Accepted && unchanged.Arg0 == 2 && unchanged.Arg1 == 0, "same settings return exact zero changed count");
            c.That(actors.Select(e => e.Version).SequenceEqual(versions), "same settings do not touch actual policy providers");
            c.That(TestKit.Order(twin, 0, Control(requested)).Accepted, "same idempotent player command on twin");
            c.Equal(game.Hash(), twin.Hash(), "idempotent command saved identity");
            var route = TestKit.Order(game, 0, new AddRouteCommand { From = source.Id, To = target.Id, Resource = steel, Settings = new LogisticsRouteSettings(true, Fix.FromInt(8), 100) });
            c.That(route.Accepted, "ordinary explicit physical route");
            foreach (var actor in actors) c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = actor.Id, Held = false }).Accepted, "release actual selected actors to original dispatch");
            for (int i = 0; i < 6; i++) Update(game);
            c.That(actors.All(e => e.Get<IWorker>().Job == 0), "area-limited policy does not acquire invalid automatic work");
            c.Equal(Fix.FromInt(20), source.Get<IInventory>().Amount(steel), "blocked policy dispatch spends no source stock");
            c.Equal(Fix.Zero, target.Get<IInventory>().Amount(steel), "blocked fleet cannot transfer instantly");
            Apply(c, game, Settings(true, 77, "fleet.all", false, true, WorkAssignmentMode.All));
            Reach(game, () => Shipments(game).Length == 2 && Shipments(game).All(j => j.Worker.IsValid), "native/public original carrier acquisition");
            c.That(Shipments(game).Select(j => j.Worker).OrderBy(i => i).SequenceEqual(ids), "only current selected released providers receive actual cargo work");
            c.That(TestKit.Order(game, 0, new ConfigureRouteCommand { Route = checked((int)route.Arg0), Settings = new LogisticsRouteSettings(false, Fix.FromInt(8), 100) }).Accepted, "stop future demand without cancelling acquired shipments");
            Reach(game, () => Shipments(game).Any(j => j.Loaded.IsPositive && j.Loaded < j.Amount), "partial physical loading");
            var loading = Shipments(game).First(j => j.Loaded.IsPositive); var loaded = loading.Loaded; var inReceipt = loading.InReservation; var outReceipt = loading.OutReservation;
            var cargoReceipt = loading.CargoOutReservation; var worker = loading.Worker;
            Apply(c, game, Settings(false, 13, "fleet.all", false, true, WorkAssignmentMode.All));
            c.That(Shipments(game).All(j => j.State == JobState.Paused && j.Blocker == WorkPolicyCodes.Disabled), "whole policy disabled through original live job gates");
            c.Equal(loaded, loading.Loaded, "policy pause adds no phantom handling"); c.Equal(inReceipt, loading.InReservation, "receiving claim retained");
            c.Equal(outReceipt, loading.OutReservation, "source claim retained"); c.Equal(cargoReceipt, loading.CargoOutReservation, "partial cargo claim retained");
            c.Equal(worker, loading.Worker, "policy pause retains actual assigned worker identity");
            c.That(actors.All(e => !e.Get<IWorker>().Held && e.State == game.World.Content.Get<IEntityStateDefinition>("survey.stopped").State), "policy is not held player control; Game.Update settles authored disabled entity state");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial cargo and policy-disabled jobs saved");
            game.Clock.Pause(); twin.Clock.Pause(); long paused = game.Clock.StepIndex; Update(game, twin);
            c.Equal(paused, game.Clock.StepIndex, "one actual clock pause stops physical work"); game.Clock.Resume(); twin.Clock.Resume();
            for (int i = 0; i < 6; i++) Update(game, twin);
            c.Equal(loaded, loading.Loaded, "disabled loading remains physically paused while game time advances");
            c.Equal(game.Hash(), twin.Hash(), "paused material/receipt/policy state continues identically");
            Apply(c, game, Settings(true, 21, "fleet.all", true, false, WorkAssignmentMode.EmergencyOnly), twin);
            Reach(game, () => loading.Loaded == loading.Amount && game.World.Entities.Get(worker).Get<IMotion>().Status == MoveStatus.Moving &&
                loading.Toils[loading.ToilIndex].Executor == WorkPhases.Travel && loading.Toils[loading.ToilIndex].At == target.Id, "retained ordinary cargo continues in emergency-only policy", twin);
            c.Equal(inReceipt, loading.InReservation, "same original destination claim survives policy continuation");
            c.Equal(worker, loading.Worker, "same original worker carries loaded cargo");
            c.Equal(Fix.Zero, target.Get<IInventory>().Amount(steel), "loaded machine still must travel before delivery");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "loaded travelling mixed-provider state saved");
            Membership(c, game, new[] { native.Id }); Membership(c, twin, new[] { native.Id });
            Apply(c, game, Settings(true, 99, "fleet.all", true, false, WorkAssignmentMode.All), twin);
            c.Equal(99, WorkPolicyRules.Read(native).Priority, "named command resolves edited current set");
            c.Equal(21, WorkPolicyRules.Read(external).Priority, "removed member keeps own prior settings, no fleet overlay");
            c.Equal(worker, loading.Worker, "membership edit never reassigns existing cargo");
            c.That(TestKit.Order(game, 0, new RemoveFleetCommand { Fleet = "delivery" }).Accepted, "remove membership metadata during physical delivery");
            c.That(TestKit.Order(twin, 0, new RemoveFleetCommand { Fleet = "delivery" }).Accepted, "same saved metadata removal");
            Reach(game, () => Shipments(game).All(j => j.State == JobState.Done), "actual saved mixed-provider unload and departure after metadata removal", twin);
            c.Equal(Fix.FromInt(16), target.Get<IInventory>().Amount(steel), "two real eight-unit shipments delivered over time");
            c.Equal(Fix.FromInt(4), source.Get<IInventory>().Amount(steel), "remaining source stock preserved");
            c.Equal(Fix.FromInt(20), Total(game), "no second fleet cargo accounting");
            c.That(actors.All(e => e.Get<IInventory>().Amount(steel).IsZero), "actual unloading empties original cargo providers");
            c.Equal(game.Hash(), twin.Hash(), "original work/policies/claims finish identically");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "final independent policy state survives no-membership save");
            var isolated = LiveSensorTests.New(factory); c.That(!View(isolated).TryRead("delivery", out _), "interleaved session borrows no membership");
            c.Equal(100, WorkPolicyRules.Read(Find(isolated, "fleet.public-carrier")).Priority, "interleaved public provider gets own authored defaults");
            c.That(!LiveSensorTests.New(FleetNavigationTests.Factory()).World.Services.Has<IMachineFleets>(), "omitted fleet module provides no hidden controls");
            Invalid(c);
        }
        private static void Invalid(Check c)
        {
            foreach (var mode in new[] { "missing-mode", "invalid-mode", "extra", "unknown-area", "unknown-role" })
            {
                var doc = Content.Json.Parse(FrameworkProofContent.Read("native-fleet-work-policies.json")); var value = doc.Get("work_policies")[0];
                if (mode == "missing-mode") value.Remove("assignment_mode"); if (mode == "invalid-mode") value.Set("assignment_mode", JsonValue.Of("instant"));
                if (mode == "extra") value.Set("free_materials", JsonValue.Of(true)); if (mode == "unknown-area") value.Set("area", JsonValue.Of("missing"));
                if (mode == "unknown-role") value.Set("allowed_roles", Content.Json.Parse("[\"missing\"]"));
                c.Throws<ContentException>(() => LiveSensorTests.New(FleetNavigationTests.Factory(named: true, workControls: true, workPolicies: doc.ToString())), "original strict authored work policy refuses " + mode);
            }
            var defect = Content.Json.Parse(FrameworkProofContent.Read("fleet-work-policy-proof.json"));
            defect.Get("entities")[0].Get("components").Get("proof_fleet_work_policy").Set("fault", Content.Json.Parse("2"));
            var game = LiveSensorTests.New(FleetNavigationTests.Factory(named: true, workControls: true, proofWork: defect.ToString()));
            c.Throws<InvalidOperationException>(() => WorkPolicyRules.PreviewGroup(game.World, new PlayerId(0), new[] { Find(game, "fleet.public-carrier").Id }, Settings(false, 1, null, true, false, WorkAssignmentMode.All)), "actual public policy mutation during pure admission faults");
            c.That(game.Fault != null, "provider observation violation fail-stops original session");
            c.Throws<InvalidOperationException>(() => game.Save(), "faulted provider state cannot produce healthy checkpoint");
        }
    }
}
