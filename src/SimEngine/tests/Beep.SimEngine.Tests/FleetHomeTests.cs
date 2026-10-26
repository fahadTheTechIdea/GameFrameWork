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
    internal static class FleetHomeTests
    {
        internal const string Case = "Fleet homes: whole intent, protected physical cargo and saved return";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static Entity Find(SimGame game, string id) => game.World.Entities.All.Single(e => e.Def.Id == id);
        private static Entity Native(SimGame game) => game.World.Entities.All.Where(e => e.Def.Id == "survey.machine").OrderBy(e => e.Id).First();
        private static Job[] Shipments(SimGame game) => game.World.Services.Get<WorkBoard>().Jobs.Where(j => j.IsHaul).OrderBy(j => j.Id).ToArray();
        private static FleetView View(SimGame game, int company = 0) => game.Seat(company).View.Feature<FleetView>();
        internal static ConfigureFleetHomeCommand Home(EntityId id) => new ConfigureFleetHomeCommand { Fleet = "delivery", Home = id };
        private static void Membership(Check c, SimGame game, IReadOnlyList<EntityId> ids)
            => c.That(TestKit.Order(game, 0, FleetLoadTests.MembershipCommand(ids)).Accepted, "normal current membership");
        private static WorkerHomeView Read(SimGame game, Entity actor)
        {
            var code = game.Seat(0).View.Feature<WorkView>().ReadHome(actor.Id, out var value);
            if (!code.IsNone) throw new InvalidOperationException("home diagnostics " + code); return value;
        }
        private static Fix Total(SimGame game)
        {
            var steel = game.World.Content.ResourceId("steel"); Fix total = Fix.Zero;
            foreach (var entity in game.World.Entities.All) if (entity.Get<IInventory>() is IInventory inventory) total += inventory.Amount(steel);
            return total;
        }
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("fleet homes clock=" + game.Clock.StepIndex + " " + game.Fault);
            if (Total(game) != Fix.FromInt(20)) throw new InvalidOperationException("fleet homes stock conservation clock=" + game.Clock.StepIndex);
            if (twin != null) Update(twin);
        }
        private static void Reach(SimGame game, Entity[] actors, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 1500 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " clock=" + game.Clock.StepIndex + " jobs=" + string.Join(";",
                Shipments(game).Select(j => j.Id + "/" + j.State + "/" + j.Worker + "/" + j.Loaded + "/" + j.Blocker)) + " homes=" +
                string.Join(";", actors.Select(e => e.Id + "/" + Read(game, e).State + "/" + Read(game, e).Blocker + "/" + e.Get<ITilePresence>().Position)));
        }
        private static void Apply(Check c, SimGame game, EntityId home, SimGame twin = null)
        {
            c.Equal(Code.None, View(game).PreviewHome("delivery", home).Blocker, "whole private intent preview");
            c.That(TestKit.Order(game, 0, Home(home)).Accepted, "normal named home intent");
            if (twin != null)
            { c.That(TestKit.Order(twin, 0, Home(home)).Accepted, "same intent on saved twin"); c.Equal(game.Hash(), twin.Hash(), "same actual owner state after intent"); }
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current home selection identity"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no fleet home payload");
            var factory = FleetNavigationTests.Factory(named: true, homeControls: true); var game = LiveSensorTests.New(factory);
            var native = Native(game); var external = Find(game, "fleet.public-carrier"); var source = Find(game, "survey.site");
            var target = Find(game, "fleet.destination"); var foreign = Find(game, "survey.foreign-sensor"); var steel = game.World.Content.ResourceId("steel");
            var actors = new[] { native, external }.OrderBy(e => e.Id).ToArray(); var ids = actors.Select(e => e.Id).ToArray();
            foreach (var actor in game.World.Entities.All.Where(e => e.Owner == new PlayerId(0) && e.Get<IWorker>() != null))
                c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = actor.Id, Held = true }).Accepted, "player holds actual candidates while authoring");
            c.That(native.Get<IWorker>() is Worker && external.Get<IWorker>() is ContractWorker, "native and existing independent public worker owners");
            c.That(external.Def.AuthoringPath.Contains("fleet-home-proof.json"), "selected master JSON authors public machine");
            c.That(!game.World.Services.Has<IEconomy>(), "home/work do not require business");
            c.Equal(7, game.Seat(0).View.FeatureRegistrations.Single(r => r.Id == MachineFleetsModule.ModuleId).SchemaVersion, "fleet view revised");
            c.Equal(1, game.Seat(0).View.StateCodecs.Single(r => r.Kind == StateCodecKind.Service && r.Id == MachineFleetsModule.ModuleId).SchemaVersion, "membership-only codec unchanged");
            Membership(c, game, ids.Reverse().ToArray()); ulong hash = game.Hash();
            var before = View(game).PreviewHome("delivery", EntityId.None);
            c.That(before.Entries.Select(e => e.Entity).SequenceEqual(ids), "canonical actual member facts");
            c.That(before.Entries.All(e => e.Current.Home == source.Id && e.Current.State == WorkerHomeState.Held), "actual authored homes and held states");
            c.Throws<NotSupportedException>(() => ((IList<WorkerHomeSelectionEntry>)before.Entries).Clear(), "selection immutable");
            var refused = View(game).PreviewHome("delivery", external.Id);
            c.Equal(Codes.InvalidArgument, refused.Blocker, "later self-home refuses complete set");
            c.Equal(Code.None, refused.Entries.Single(e => e.Entity == native.Id).Blocker, "valid prefix is only admission");
            c.Equal(Codes.NotFound, View(game).PreviewHome("delivery", foreign.Id).Blocker, "private target refusal conceals foreign existence");
            c.Equal(Codes.NotFound, game.Seat(0).Preview(Home(foreign.Id)), "command preserves private home admission");
            c.Equal(Codes.NotFound, View(game, 1).PreviewHome("delivery", source.Id).Blocker, "foreign named metadata private");
            foreach (var malformed in new[] { null, Array.Empty<EntityId>(), new[] { ids[0], ids[0] }, new[] { ids[0], EntityId.None } })
                c.Equal(Codes.InvalidArgument, WorkerHomeContracts.PreviewGroup(game.World, new PlayerId(0), malformed, source.Id).Blocker, "malformed selection refuses");
            c.Equal(Codes.NotSeated, WorkerHomeContracts.PreviewGroup(game.World, PlayerId.None, ids, source.Id).Blocker, "observer has no whole control authority");
            c.Equal(StandardCodes.NotWorker, WorkerHomeContracts.PreviewGroup(game.World, new PlayerId(0), new[] { target.Id }, source.Id).Blocker, "non-worker cannot fabricate home");
            var privateMember = WorkerHomeContracts.PreviewGroup(game.World, new PlayerId(0), new[] { foreign.Id }, source.Id);
            c.That(privateMember.Blocker == Codes.NotFound && privateMember.Entries.Single().Current == null, "foreign home facts omitted");
            c.Equal(Codes.NotFound, View(game).PreviewHome("delivery", new EntityId(999999)).Blocker, "missing target no default home");
            c.Equal(Codes.InvalidArgument, View(game).PreviewHome("delivery", new EntityId(-1)).Blocker, "negative home not implicit clear");
            c.Equal(hash, game.Hash(), "all management checks preserve state/time");
            c.Equal(Codes.InvalidArgument, TestKit.Order(game, 0, Home(external.Id)).Refusal, "normal whole refusal");
            c.That(actors.All(e => Read(game, e).Home == source.Id), "refused command changes no native prefix");
            c.That(TestKit.Order(game, 0, new ConfigureFleetCommand { Fleet = "empty", Settings = new FleetSettings("fleet.haul", Array.Empty<EntityId>()) }).Accepted, "explicit empty membership");
            c.Equal(FleetCodes.Empty, View(game).PreviewHome("empty", source.Id).Blocker, "empty fleet invents no worker");
            var pending = Home(EntityId.None); int sequence = game.Seat(0).Submit(pending); hash = game.Hash(); pending.Home = foreign.Id; pending.Fleet = "changed";
            c.Equal(hash, game.Hash(), "inbox owns pending intent copy"); var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending named clear saved");
            Update(game, twin); c.Equal(game.Hash(), twin.Hash(), "pending clear continues identically");
            var results = new List<CommandResult>(); long cursor = 0; game.Seat(0).ReadResults(ref cursor, results);
            c.That(results.Single(r => r.Sequence == sequence).Accepted && results.Single(r => r.Sequence == sequence).Arg1 == 2, "actual two changed owner intents");
            c.That(actors.All(e => Read(game, e).Home == EntityId.None && e.Get<IWorker>().Held), "clear does not release manual held control");
            c.That(before.Entries.All(e => e.Current.Home == source.Id), "old detached home facts stay frozen");
            var versions = actors.Select(e => e.Version).ToArray(); var same = TestKit.Order(game, 0, Home(EntityId.None));
            c.That(same.Accepted && same.Arg0 == 2 && same.Arg1 == 0, "idempotent clear zero changes"); c.That(actors.Select(e => e.Version).SequenceEqual(versions), "idempotent clear no owner touch");
            Membership(c, game, new[] { native.Id }); Apply(c, game, target.Id);
            c.That(Read(game, native).Home == target.Id && Read(game, external).Home == EntityId.None, "current membership only; removed public worker intent retained");
            Membership(c, game, ids); Apply(c, game, target.Id);
            var limited = new WorkPolicySettings(true, 50, new[] { new WorkerRoleId("haul") }, "fleet.source-only", true, false, WorkAssignmentMode.All);
            c.That(TestKit.Order(game, 0, new ConfigureFleetWorkPolicyCommand { Fleet = "delivery", Settings = limited }).Accepted, "original area policy gates real movement");
            var route = TestKit.Order(game, 0, new AddRouteCommand { From = source.Id, To = target.Id, Resource = steel, Settings = new LogisticsRouteSettings(true, Fix.FromInt(8), 100) });
            c.That(route.Accepted, "normal physical demand"); var positions = actors.Select(e => e.Get<ITilePresence>().Position).ToArray();
            foreach (var actor in actors) c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = actor.Id, Held = false }).Accepted, "normal release to engine");
            for (int i = 0; i < 4; i++) Update(game);
            c.That(actors.All(e => Read(game, e).Home == target.Id && Read(game, e).State == WorkerHomeState.Blocked && Read(game, e).Blocker == WorkPolicyCodes.OutsideArea), "future home intent retained but current area refuses travel");
            c.That(actors.Select(e => e.Get<ITilePresence>().Position).SequenceEqual(positions), "blocked return no teleport or movement");
            var allowed = new WorkPolicySettings(true, 50, new[] { new WorkerRoleId("haul") }, "fleet.all", true, false, WorkAssignmentMode.All);
            c.That(TestKit.Order(game, 0, new ConfigureFleetWorkPolicyCommand { Fleet = "delivery", Settings = allowed }).Accepted, "normal policy enables physical dispatch");
            Reach(game, actors, () => Shipments(game).Length == 2 && Shipments(game).All(j => j.Worker.IsValid), "native/public actual cargo acquisition");
            c.That(Shipments(game).Select(j => j.Worker).OrderBy(i => i).SequenceEqual(ids), "actual mixed selected carrier owners");
            c.That(TestKit.Order(game, 0, new ConfigureRouteCommand { Route = checked((int)route.Arg0), Settings = new LogisticsRouteSettings(false, Fix.FromInt(8), 100) }).Accepted, "disable future jobs without cancelling acquired cargo");
            Reach(game, actors, () => Shipments(game).Any(j => j.Loaded.IsPositive && j.Loaded < j.Amount), "partial timed loading");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial handling/home intent saves");
            Reach(game, actors, () => Shipments(game).All(j => j.Loaded == j.Amount && j.Toils[j.ToilIndex].Executor == WorkPhases.Travel && j.Toils[j.ToilIndex].At == target.Id), "full physical cargo travelling", twin);
            var jobs = Shipments(game); var jobIds = jobs.Select(j => j.Id).ToArray(); var assigned = jobs.Select(j => j.Worker).ToArray(); var claims = jobs.Select(j => j.InReservation).ToArray();
            long changedAt = game.Clock.StepIndex; Apply(c, game, source.Id, twin);
            c.That(Shipments(game).Select(j => j.Id).SequenceEqual(jobIds) && jobs.Select(j => j.Worker).SequenceEqual(assigned), "busy home edit never replaces acquired job/worker");
            c.That(jobs.Select(j => j.InReservation).SequenceEqual(claims) && jobs.All(j => j.Loaded == j.Amount), "busy edit retains real receiving claims/full cargo");
            c.That(actors.All(e => Read(game, e).Home == source.Id && Read(game, e).State == WorkerHomeState.Working), "new home is future intent while loaded work continues");
            c.Equal(Fix.Zero, target.Get<IInventory>().Amount(steel), "home edit does not deliver instantly");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "changed busy intents/claims saved");
            game.Clock.Pause(); twin.Clock.Pause(); long paused = game.Clock.StepIndex; var pausedHash = game.Hash(); Update(game, twin);
            c.Equal(paused, game.Clock.StepIndex, "sole clock pause stops cargo/return"); c.Equal(pausedHash, game.Hash(), "pause changes no authoritative state"); game.Clock.Resume(); twin.Clock.Resume();
            c.That(TestKit.Order(game, 0, new RemoveFleetCommand { Fleet = "delivery" }).Accepted, "remove metadata during loaded trip");
            c.That(TestKit.Order(twin, 0, new RemoveFleetCommand { Fleet = "delivery" }).Accepted, "same saved metadata removal");
            Reach(game, actors, () => Shipments(game).All(j => j.State == JobState.Done), "original timed unload/departure", twin);
            c.Equal(Fix.FromInt(16), target.Get<IInventory>().Amount(steel), "two actual eight-unit physical deliveries"); c.Equal(Fix.FromInt(4), source.Get<IInventory>().Amount(steel), "source remainder preserved");
            c.That(game.Clock.StepIndex > changedAt && actors.All(e => e.Get<IInventory>().Amount(steel).IsZero), "positive game time and actual empty cargo");
            Reach(game, actors, () => actors.Any(e => Read(game, e).State == WorkerHomeState.Returning), "original idle physical home return", twin);
            c.That(actors.Any(e => e.State == game.World.Content.Get<IEntityStateDefinition>("survey.moving").State), "Game.Update settles real moving entity state");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partway returning owner state saved without fleet metadata"); long returningAt = game.Clock.StepIndex;
            Reach(game, actors, () => actors.All(e => Read(game, e).State == WorkerHomeState.AtHome), "saved real return arrival", twin);
            c.That(game.Clock.StepIndex > returningAt, "return cannot complete instantly"); c.Equal(game.Hash(), twin.Hash(), "saved physical return twins equal");
            c.Equal(Fix.FromInt(20), Total(game), "delivery/return conserve actual stock"); c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "final worker-owned home state restores");
            var isolated = LiveSensorTests.New(factory); c.That(!View(isolated).TryRead("delivery", out _), "interleaved session borrows no fleet");
            c.Equal(Find(isolated, "survey.site").Id, Read(isolated, Find(isolated, "fleet.public-carrier")).Home, "independent public worker owns authored home");
            c.That(!LiveSensorTests.New(FleetNavigationTests.Factory()).World.Services.Has<IMachineFleets>(), "omitted module grants no hidden named home control");
            Fault(c);
        }
        private static void Fault(Check c)
        {
            var doc = Content.Json.Parse(FrameworkProofContent.Read("fleet-home-proof.json"));
            doc.Get("entities")[0].Get("components").Get("proof_fleet_home_worker").Set("home_fault", JsonValue.Of("commit-ignore"));
            var game = LiveSensorTests.New(FleetNavigationTests.Factory(named: true, homeControls: true, proofWork: doc.ToString()));
            var external = Find(game, "fleet.public-carrier");
            c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = external.Id, Held = true }).Accepted, "hold actual fault probe through engine");
            Membership(c, game, new[] { external.Id }); var destination = Find(game, "fleet.destination");
            c.Equal(Code.None, View(game).PreviewHome("delivery", destination.Id).Blocker, "pure admission cannot promise hostile provider commit");
            game.Seat(0).Submit(Home(destination.Id)); game.Update(); c.That(game.Fault != null, "lying actual Home setter faults original session");
            c.Throws<InvalidOperationException>(() => game.Save(), "failed home commit cannot checkpoint partial state");
        }
    }
}
