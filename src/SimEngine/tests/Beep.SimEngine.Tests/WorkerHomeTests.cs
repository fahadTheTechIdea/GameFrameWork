using System;
using System.Collections.Generic;
using System.IO;
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
    internal static class WorkerHomeTests
    {
        internal static void Register()
        {
            TestRunner.Add("Worker home: private intent, provider guards and canonical command admission", Admission);
            TestRunner.Add("Worker home: physical retargeting, protected work and saved continuation", Physical);
        }
        private static string Text(JsonValue value) { var text = new StringBuilder(); value.WriteCanonical(text); return text.ToString(); }
        private static GameFactory Factory(bool external = false, string fault = null, bool powered = false, bool ignoreStop = false, bool stationary = false, bool residualCargo = false)
        {
            var root = Content.Json.Parse(PointGame.Definitions); root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            var parts = root.Get("entities")[2].Get("components");
            StartupContentAuthoring.Position(root, "machine", JsonValue.Number("5"));
            if (ignoreStop) parts.Get("point_motion").Set("ignore_stop", JsonValue.Of(true));
            if (stationary) parts.Remove("point_motion");
            parts.Get("worker").Set("work_rate", JsonValue.Number("1"));
            parts.Set("work_policy", Content.Json.Parse("{\"profile\":\"policy.initial\"}"));
            if (external)
            {
                parts.Remove("worker");
                var worker = Content.Json.Parse("{\"role\":\"haul\",\"rate\":1,\"states\":{\"working\":\"processing\",\"stopped\":\"blocked\"}}");
                if (fault != null) worker.Set("home_fault", JsonValue.Of(fault));
                parts.Set("contract_worker", worker);
            }
            if (powered)
            {
                parts.Set("operating_inputs", Content.Json.Parse(FrameworkProofContent.Read("motion-operating-inputs.json")));
                parts.Get("storage").Set("initial_stock", Content.Json.Parse("{\"supply\":2}"));
            }
            if (residualCargo) parts.Get("storage").Set("initial_stock", Content.Json.Parse("{\"supply\":2}"));
            if (residualCargo)
            {
                var scenario = root.Get("scenarios")[0];
                scenario.Get("modules").Add(JsonValue.Of(LogisticsRecoveryPolicyModule.ModuleId));
                scenario.Get("settings").Set(LogisticsRecoveryPolicyModule.ModuleId, Content.Json.Parse("{\"profile\":\"recovery.high\"}"));
            }
            foreach (var entry in new[] { ("other_home", 20, 0), ("foreign_home", 15, 1), ("plain", -1, 0) })
            {
                var entity = Content.Json.Parse("{\"id\":\"" + entry.Item1 + "\",\"kind\":\"entity\",\"components\":{}}");
                if (entry.Item2 >= 0) entity.Get("components").Set("point_presence", Content.Json.Parse("{\"space\":\"field.points\",\"position\":" + entry.Item2 + "}"));
                root.Get("entities").Add(entity);
                StartupContentAuthoring.Add(root, entry.Item1, entry.Item1, entry.Item3, entity.Get("components").Get("point_presence"));
            }
            var retry = Content.Json.Parse(FrameworkProofContent.Read("haul-retry.json"));
            var executors = Content.Json.Parse(FrameworkProofContent.Read("work-retry.json"));
            retry.Set("staffed_activity_executors", executors.Get("staffed_activity_executors").Clone());
            retry.Set("timed_activity_executors", executors.Get("timed_activity_executors").Clone());
            return PointGame.Factory(Text(root), retryProfiles: Text(retry),
                additionalModule: residualCargo ? new LogisticsRecoveryPolicyModule() : null,
                moduleDeclaration: residualCargo ? "{\"id\":\"sim.logistics-recovery\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[\"logistics-recovery.json\"]}" : null,
                moduleDocument: residualCargo ? FrameworkProofContent.Read("logistics-recovery.json") : null,
                additionalDocuments: new Dictionary<string, string> { ["worker-home-policies.json"] = FrameworkProofContent.Read("point-work-policies.json") });
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 733 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static Entity Find(SimGame game, string id)
        { foreach (Entity e in game.World.Entities.All) if (e.Alive && e.Def.Id == id && (id == "foreign_home" || e.Owner == new PlayerId(0))) return e; throw new InvalidOperationException(id); }
        private static WorkerHomeView Read(SimGame game)
        { Code refusal = game.Seat(0).View.Feature<WorkView>().ReadHome(Find(game, "machine").Id, out var value); if (!refusal.IsNone) throw new InvalidOperationException(refusal.ToString()); return value; }
        private static CommandResult Set(SimGame game, EntityId home, int seat = 0)
            => TestKit.Order(game, seat, new SetHomeCommand { Worker = Find(game, "machine").Id, Home = home });
        private static Fix Position(SimGame game) => Find(game, "machine").Get<PointPresence>().Position;
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 250 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done())
            {
                var machine = Find(game, "machine"); var view = Read(game);
                throw new InvalidOperationException(stage + " timeout tick=" + game.Tick + " position=" + Position(game) + " home=" + view.Home +
                    " return=" + view.State + "/" + view.Blocker + " motion=" + machine.Get<IMotion>().Status + " job=" + view.Job);
            }
        }
        private static byte[] WorkerBytes(Component worker)
        { using (var stream = new MemoryStream()) { var writer = new StateWriter(stream); worker.Sync(writer); writer.Flush(); return stream.ToArray(); } }
        private static void Admission(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current home semantics have exact build identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "existing worker payload remains sole layout");
            foreach (bool external in new[] { false, true })
            {
                GameFactory factory = Factory(external); SimGame game = New(factory); Entity worker = Find(game, "machine"); var view = game.Seat(0).View.Feature<WorkView>();
                ulong hash = game.Hash(); var empty = Read(game);
                c.That(empty.Home == EntityId.None && empty.State == WorkerHomeState.Unassigned, "no authored/command home invents no default depot");
                c.Equal(hash, game.Hash(), "detached home read is pure");
                c.Equal(Codes.NotFound, game.Seat(1).View.Feature<WorkView>().ReadHome(worker.Id, out var hidden), "foreign home facts are private");
                c.That(hidden == null, "foreign refusal leaks no intent/state");
                c.Equal(StandardCodes.NotWorker, view.ReadHome(Find(game, "plain").Id, out _), "ordinary entity cannot fabricate worker home");
                c.Equal(Code.None, game.Observer.Feature<WorkView>().ReadHome(worker.Id, out _), "explicit observer can inspect owned home");
                foreach (var pair in new[] { (worker.Id, Codes.InvalidArgument), (Find(game, "foreign_home").Id, Codes.NotFound),
                    (Find(game, "plain").Id, WorkerHomeContracts.Unsupported), (new EntityId(99999), Codes.NotFound), (new EntityId(-1), Codes.InvalidArgument) })
                {
                    c.Equal(pair.Item2, view.PreviewHome(worker.Id, pair.Item1), "canonical structural refusal " + pair.Item1);
                    c.Equal(pair.Item2, Set(game, pair.Item1).Refusal, "command/refusal parity " + pair.Item1);
                }
                c.Equal(Codes.NotFound, game.Seat(1).View.Feature<WorkView>().PreviewHome(worker.Id, Find(game, "source").Id), "foreign preview cannot change home");
                c.Equal(Codes.NotFound, Set(game, Find(game, "source").Id, 1).Refusal, "foreign command refuses identically");
                c.Equal(Code.None, view.PreviewHome(worker.Id, Find(game, "other_home").Id), "actual compatible endpoint accepts");
                c.That(Set(game, Find(game, "other_home").Id).Accepted, "normal command records home through IWorker");
                var selected = Read(game);
                c.That(selected.Home == Find(game, "other_home").Id && selected.State == WorkerHomeState.Returning, "current immutable view derives physical return");
                c.That(selected.EntityVersion > empty.EntityVersion, "actual home edit touches its entity");
                c.That(empty.Home == EntityId.None && empty.State == WorkerHomeState.Unassigned, "retained snapshot never follows mutable intent");
                var policy = new WorkPolicySettings(true, 1, new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, "policy.source", true, true, WorkAssignmentMode.All);
                c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = worker.Id, Settings = policy }).Accepted, "normal player sets operating area");
                c.Equal(WorkPolicyCodes.OutsideArea, Read(game).Blocker, "home outside operating area remains blocked, not silently replaced");
                c.Equal(Code.None, view.PreviewHome(worker.Id, Find(game, "destination").Id), "operational wait does not forbid recording future home intent");
                c.That(Set(game, Find(game, "destination").Id).Accepted, "blocked intent command matches preview");
                c.Equal(WorkPolicyCodes.OutsideArea, Read(game).Blocker, "selected home still obeys real operating area");
                Fix stopped = Position(game); c.That(Set(game, EntityId.None).Accepted, "explicit None clears current home");
                c.Equal(stopped, Position(game), "clear never spends movement"); c.Equal(WorkerHomeState.Unassigned, Read(game).State, "clear has no inferred replacement");
                worker.Get<IWorker>().Home = Find(game, "source").Id;
                Component state = external ? (Component)worker.Get<ContractWorker>() : worker.Get<Worker>(); byte[] original = WorkerBytes(state);
                byte[] bad = (byte[])original.Clone(); Array.Copy(BitConverter.GetBytes(-1), 0, bad, 4, 4);
                c.Throws<CorruptStateException>(() => state.Sync(new StateReader(bad, 0, bad.Length)), "native/public codec refuses negative home identity");
                c.Equal(Convert.ToBase64String(original), Convert.ToBase64String(WorkerBytes(state)), "bad home decode replaces no prior worker fields");
                c.Throws<CorruptStateException>(() => state.Sync(new StateReader(original, 0, original.Length - 1)), "truncated worker payload refuses");
                c.Equal(Convert.ToBase64String(original), Convert.ToBase64String(WorkerBytes(state)), "truncated decode retains job/home/manual flags together");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "current home and policies survive sole owner save");
                var queued = New(factory); queued.Seat(0).Submit(new SetHomeCommand { Worker = Find(queued, "machine").Id, Home = Find(queued, "other_home").Id });
                var twin = factory.Load(queued.Save()); Tick(queued); Tick(twin);
                c.Equal(queued.Hash(), twin.Hash(), "pending ordinary home command is self-describing and replayed identically after load");
                var independent = New(factory); c.Equal(WorkerHomeState.Unassigned, Read(independent).State, "shared JSON never shares runtime home/goal state");
            }
            foreach (string fault in new[] { "read-touch", "commit-ignore", "commit-no-touch", "commit-job" })
            {
                GameFactory factory = Factory(true, fault); SimGame game = New(factory);
                if (fault == "read-touch") c.Throws<InvalidOperationException>(() => Read(game), "public mutating home getter faults pure read");
                else
                {
                    game.Seat(0).Submit(new SetHomeCommand { Worker = Find(game, "machine").Id, Home = Find(game, "other_home").Id }); game.Update();
                    c.That(game.World.Fault != null, "public lying home setter faults normal Update: " + fault);
                    c.Throws<InvalidOperationException>(() => game.Save(), "faulted partial home commit cannot checkpoint: " + fault);
                }
            }
            var stopping = New(Factory(ignoreStop: true));
            c.That(Set(stopping, Find(stopping, "other_home").Id).Accepted, "independent mover starts before stop fault");
            stopping.Seat(0).Submit(new SetHomeCommand { Worker = Find(stopping, "machine").Id, Home = EntityId.None }); stopping.Update();
            c.That(stopping.World.Fault != null, "ignored actual Stop faults normal clear instead of moving toward stale home");
            c.Throws<InvalidOperationException>(() => stopping.Save(), "ignored Stop cannot checkpoint a resumable false clear");
            var acquiring = New(Factory(ignoreStop: true));
            c.That(Set(acquiring, Find(acquiring, "other_home").Id).Accepted, "independent mover starts before assignment fault");
            acquiring.Seat(0).Submit(new PostOrderCommand { Target = Find(acquiring, "destination").Id, Plan = "policy.work" }); acquiring.Update();
            c.That(acquiring.World.Fault != null, "ignored Stop faults actual work assignment rather than advancing stale home travel");
            c.Throws<InvalidOperationException>(() => acquiring.Save(), "faulted motion ownership acquisition cannot checkpoint");
            var stationary = New(Factory(stationary: true));
            c.Equal(Code.None, stationary.Seat(0).View.Feature<WorkView>().PreviewHome(Find(stationary, "machine").Id, EntityId.None), "stationary worker can explicitly clear home without invented motion");
            c.That(Set(stationary, EntityId.None).Accepted, "no-motion clear matches normal command admission");
            c.Equal(WorkerHomeContracts.Unsupported, Set(stationary, Find(stationary, "source").Id).Refusal, "distant stationary worker home requires a real motion provider");
        }
        private static void Physical(Check c)
        {
            foreach (bool external in new[] { false, true })
            {
                var factory = Factory(external); var game = New(factory); var machine = Find(game, "machine");
                c.That(Set(game, Find(game, "other_home").Id).Accepted, "start real home return");
                c.That(Position(game) > Fix.FromInt(5) && Position(game) < Fix.FromInt(20), "engine Update moves partially, never teleports");
                c.Equal(new Code("point.travel"), machine.State, "Entity.Update settles authored travel state");
                long exposure = machine.Get<PointMotion>().TicksMoving; Fix position = Position(game);
                c.That(Set(game, Find(game, "other_home").Id).Accepted, "same home command is accepted without resetting route");
                c.Equal(exposure + 1, machine.Get<PointMotion>().TicksMoving, "unchanged home retains actual motion age");
                c.Equal(position + Fix.One, Position(game), "unchanged home advances exactly one physical tick");
                position = Position(game); c.That(Set(game, Find(game, "source").Id).Accepted, "moving home can be reassigned");
                c.That(Position(game) < position && Position(game) > Fix.Zero, "new home changes direction before movement without teleportation");
                c.Equal(Fix.Zero, machine.Get<PointMotion>().Goal, "actual provider owns the retargeted goal");
                var twin = factory.Load(game.Save()); Reach(game, () => Read(game).State == WorkerHomeState.AtHome, "saved home return", twin);
                c.Equal(game.Hash(), twin.Hash(), "physical home twins continue identically"); c.Equal(Fix.Zero, Position(game), "actual return reaches current home");
                c.That(Set(game, Find(game, "other_home").Id).Accepted, "start another return"); position = Position(game);
                c.That(Set(game, EntityId.None).Accepted, "clear an already moving return"); c.Equal(position, Position(game), "cleared return stops before next physical step");
                for (int i = 0; i < 3; i++) Tick(game); c.Equal(position, Position(game), "unassigned worker never resumes stale goal");
                c.That(Set(game, Find(game, "other_home").Id).Accepted, "select moving endpoint");
                // Engine-provider fixture input: actual home geometry changes, never host-side movement rules.
                Find(game, "other_home").Get<PointPresence>().Move(game.World, Fix.FromInt(25)); Tick(game);
                c.Equal(Fix.FromInt(25), machine.Get<PointMotion>().Goal, "current mobile home replans through selected provider");
                position = Position(game); EntityId removed = Find(game, "other_home").Id; game.World.Despawn(Find(game, "other_home")); Tick(game);
                c.Equal(position, Position(game), "removed home stops old motion before advance");
                c.That(Read(game).Home == removed && Read(game).Blocker == Codes.NotFound, "removed home retains original saved intent and typed blocker");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "lost home reference remains lawful durable intent");
                c.That(Set(game, EntityId.None).Accepted, "lost home can be explicitly cleared");

                var ownership = New(factory); Entity ownedHome = Find(ownership, "other_home");
                c.That(Set(ownership, ownedHome.Id).Accepted, "owned home starts before ownership change"); position = Position(ownership);
                // Authoritative framework fixture input, not a free gameplay ownership command.
                ownedHome.Owner = new PlayerId(1); Tick(ownership);
                c.Equal(position, Position(ownership), "changed ownership stops old home motion before advance");
                c.Equal(Codes.NotFound, ownership.Seat(0).View.Feature<WorkView>().ReadHome(Find(ownership, "machine").Id, out var foreign), "foreign current home is withheld by the public company-private view");
                c.That(foreign == null, "withheld home details never leak through a refusal");
                var ownedIntent = WorkerHomeContracts.Read(ownership.World, Find(ownership, "machine"));
                c.Equal(Codes.NotOwned, ownedIntent.Blocker, "actual engine-owned intent retains the ownership-loss blocker");
                c.Equal(ownedHome.Id, ownedIntent.Home, "ownership loss preserves actual intent identity without exposing the foreign home");
                var routes = New(factory); c.That(Set(routes, Find(routes, "other_home").Id).Accepted, "home starts through open route"); position = Position(routes);
                c.That(TestKit.Order(routes, 0, new SetPointAccessCommand { Open = false }).Accepted, "engine-side spatial command changes actual route access");
                c.Equal(position, Position(routes), "closed route cannot earn distance");
                c.Equal(WorkSystem.BlockerUnreachable, Read(routes).Blocker, "route loss has an engine-calculated home blocker");
                c.That(TestKit.Order(routes, 0, new SetPointAccessCommand { Open = true }).Accepted, "actual spatial access can reopen");
                Reach(routes, () => Read(routes).State == WorkerHomeState.AtHome, "home route retry");
                c.Equal(Fix.FromInt(20), Position(routes), "reopened return continues physically to original target");

                var workGame = New(factory); machine = Find(workGame, "machine");
                var posted = TestKit.Order(workGame, 0, new PostOrderCommand { Target = Find(workGame, "destination").Id, Plan = "policy.work" });
                c.That(posted.Accepted, "ordinary JSON activity begins through same work owner");
                Reach(workGame, () => machine.Get<IWorker>().Job != 0, "work assigned"); int job = machine.Get<IWorker>().Job;
                c.That(Set(workGame, Find(workGame, "other_home").Id).Accepted, "home edit records future intent during active job");
                c.Equal(job, machine.Get<IWorker>().Job, "home edit preserves actual acquired worker claim");
                c.Equal(Fix.FromInt(10), machine.Get<PointMotion>().Goal, "active job retains its physical destination");
                c.Equal(WorkerHomeState.Working, Read(workGame).State, "view distinguishes acquired work from autonomous home return");
                var workTwin = factory.Load(workGame.Save()); Reach(workGame, () => Read(workGame).State == WorkerHomeState.AtHome, "work then home", workTwin);
                c.Equal(workGame.Hash(), workTwin.Hash(), "saved acquired work finishes before native home travel");
                c.Equal(Fix.FromInt(20), Position(workGame), "engine returns only after actual work releases machine");
                var returning = New(factory); machine = Find(returning, "machine");
                c.That(Set(returning, Find(returning, "other_home").Id).Accepted, "idle worker starts home travel before new work");
                position = Position(returning);
                c.That(TestKit.Order(returning, 0, new PostOrderCommand { Target = Find(returning, "source").Id, Plan = "policy.work" }).Accepted,
                    "ordinary work can acquire a physically returning idle worker");
                c.That(machine.Get<IWorker>().Job != 0, "returning worker has one real assigned job");
                c.Equal(position, Position(returning), "assignment stops idle goal before that tick's movement phase");
                c.Equal(MoveStatus.Idle, machine.Get<IMotion>().Status, "only new job owns the next physical goal");
                c.Equal(WorkerHomeState.Working, Read(returning).State, "current job overrides still-recorded home intent");
                var returningTwin = factory.Load(returning.Save()); Reach(returning, () => Read(returning).State == WorkerHomeState.AtHome, "new work interrupts home travel", returningTwin);
                c.Equal(returning.Hash(), returningTwin.Hash(), "saved motion ownership transfer continues identically");
                c.Equal(Fix.FromInt(20), Position(returning), "new work releases into original physical home intent");
                Powered(c, external); Cargo(c, external); ResidualCargo(c, external);
            }
            ManualTile(c);
        }
        private static void Powered(Check c, bool external)
        {
            var factory = Factory(external, powered: true); var game = New(factory); var machine = Find(game, "machine"); var supply = game.World.Content.ResourceId("supply");
            c.That(TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = machine.Id, Resource = supply,
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.Zero, Fix.FromInt(2), Fix.FromInt(8), 1, LogisticsReserveAccess.Protected)) }).Accepted, "ordinary stock policy protects actual operating reserve");
            c.That(Set(game, Find(game, "other_home").Id).Accepted, "powered home return starts with actual fuel");
            Reach(game, () => Read(game).State == WorkerHomeState.Blocked, "home fuel depletion");
            Fix stopped = Position(game); var intent = Read(game); var motion = machine.Get<PointMotion>();
            c.That(stopped > Fix.FromInt(5) && stopped < Fix.FromInt(20) && !intent.Blocker.IsNone, "fuel exhaustion blocks unfinished real travel");
            c.Equal(0, machine.Get<IWorker>().Job, "retained fuel is not silently dispatched as residual cargo");
            c.Equal(Fix.Zero, machine.Get<IInventory>().Amount(supply), "home travel consumes real inventory, not a virtual fuel ledger");
            c.Equal(MoveStatus.Moving, motion.Status, "fuel wait retains actual provider goal");
            var twin = factory.Load(game.Save()); for (int i = 0; i < 3; i++) { Tick(game); Tick(twin); }
            c.Equal(stopped, Position(game), "fuel waits earn no physical movement"); c.Equal(game.Hash(), twin.Hash(), "saved fuel waits continue identically");
            c.That(Set(game, EntityId.None).Accepted, "blocked powered home can be cleared"); c.Equal(stopped, Position(game), "clear grants no stock or distance");
        }
        private static void Cargo(Check c, bool external)
        {
            var factory = Factory(external); var game = New(factory); var machine = Find(game, "machine"); var resource = game.World.Content.ResourceId("supply");
            var route = TestKit.Order(game, 0, new AddRouteCommand { From = Find(game, "source").Id, To = Find(game, "destination").Id,
                Resource = resource, Settings = new LogisticsRouteSettings(true, Fix.FromInt(2), 1) });
            c.That(route.Accepted, "standing route enters ordinary physical hauling");
            Reach(game, () => game.World.Services.Get<WorkBoard>().Job(machine.Get<IWorker>().Job)?.Loaded > Fix.Zero, "loaded haul");
            int job = machine.Get<IWorker>().Job;
            c.That(Set(game, Find(game, "other_home").Id).Accepted, "home can be set during loaded haul");
            c.Equal(job, machine.Get<IWorker>().Job, "home edit never revokes acquired cargo job");
            c.Equal(WorkerHomeState.Working, Read(game).State, "loaded haul remains authoritative over home intent");
            c.That(TestKit.Order(game, 0, new ConfigureRouteCommand { Route = checked((int)route.Arg0),
                Settings = new LogisticsRouteSettings(false, Fix.FromInt(2), 1) }).Accepted, "disable only future route intent");
            var twin = factory.Load(game.Save()); Reach(game, () => Read(game).State == WorkerHomeState.AtHome, "loaded delivery then home", twin);
            c.Equal(game.Hash(), twin.Hash(), "loaded home intent twins retain native claims and finish normally");
            c.Equal(Fix.FromInt(2), Find(game, "destination").Get<IInventory>().Amount(resource), "promised cargo physically arrives before home return");
            c.Equal(Fix.Zero, machine.Get<IInventory>().Amount(resource), "home return never abandons acquired cargo");
            c.Equal(Fix.FromInt(20), Position(game), "empty released worker physically reaches new home");
            c.Equal(Fix.FromInt(20), Find(game, "source").Get<IInventory>().Amount(resource) + Find(game, "destination").Get<IInventory>().Amount(resource), "no stock fabricated or discarded by home management");
        }
        private static void ResidualCargo(Check c, bool external)
        {
            var factory = Factory(external, residualCargo: true); var game = New(factory); var machine = Find(game, "machine");
            var resource = game.World.Content.ResourceId("supply"); Fix position = Position(game);
            c.That(Set(game, Find(game, "other_home").Id).Accepted, "home selection with real residual cargo is valid future intent");
            Job returning = game.World.Services.Get<WorkBoard>().Job(machine.Get<IWorker>().Job);
            c.That(returning != null && returning.From == EntityId.None && returning.Loaded == Fix.FromInt(2),
                "existing haul execution acquires residual cargo before home travel");
            c.Equal(position, Position(game), "new cargo return cannot advance the superseded home goal on acquisition");
            c.Equal(WorkerHomeState.Working, Read(game).State, "unassigned cargo becomes an authoritative physical job, not a home shortcut");
            var twin = factory.Load(game.Save()); Reach(game, () => Read(game).State == WorkerHomeState.AtHome, "residual cargo then home", twin);
            c.Equal(game.Hash(), twin.Hash(), "saved residual disposition finishes before home travel identically");
            c.Equal(Fix.Zero, machine.Get<IInventory>().Amount(resource), "residual cargo is actually unloaded before home arrival");
            c.Equal(Fix.FromInt(22), Find(game, "source").Get<IInventory>().Amount(resource) + Find(game, "destination").Get<IInventory>().Amount(resource),
                "authored residual cargo and original stock are conserved through actual endpoints");
            c.Equal(Fix.FromInt(20), Position(game), "empty released carrier reaches its selected physical home");
        }
        private static void ManualTile(Check c)
        {
            var definitions = Content.Json.Parse(Colony.Defs);
            var entities = definitions.Get("entities");
            for (int i = 0; i < entities.Count; i++)
                if (entities[i].Get("id").Text == "truck") entities[i].Get("components").Set("work_policy", Content.Json.Parse("{\"profile\":\"home.manual\"}"));
            var factory = Colony.Factory(Text(definitions), additionalDefinitions:
                "{\"work_policies\":[{\"id\":\"home.manual\",\"enabled\":true,\"priority\":0,\"allowed_roles\":[\"haul\"],\"area\":null,\"interruptible\":true,\"emergency_eligible\":true,\"assignment_mode\":\"all\",\"states\":{\"disabled\":\"stopped\",\"standby\":\"stopped\"}}]}");
            var request = new NewGameRequest { Scenario = "colony", Seed = 739 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); var game = factory.NewGame(request);
            Entity truck = Find(game, "truck"), factoryHome = Find(game, "factory");
            c.That(TestKit.Order(game, 0, new MoveVehicleCommand { Vehicle = truck.Id, To = new TileCoord(20, 20) }).Accepted, "normal player command acquires manual tile control");
            TileRect directGoal = truck.Get<IMobility>().Goal;
            c.That(TestKit.Order(game, 0, new SetHomeCommand { Worker = truck.Id, Home = factoryHome.Id }).Accepted, "home edit during direct control is future intent only");
            c.That(truck.Get<IWorker>().Held && truck.Get<IMobility>().Goal == directGoal, "home edit cannot redirect manually held goal");
            c.Equal(Code.None, game.Seat(0).View.Feature<WorkView>().ReadHome(truck.Id, out var held), "held home facts read through engine");
            c.Equal(WorkerHomeState.Held, held.State, "immutable view reports manual control");
            c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = truck.Id,
                Settings = new WorkPolicySettings(false, 0, new[] { new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) }).Accepted,
                "player can disable automatic work while retaining direct control");
            c.That(truck.Get<IWorker>().Held && truck.Get<IMobility>().Goal == directGoal && truck.Get<IMotion>().Status == MoveStatus.Moving,
                "work policy refresh cannot stop or retarget manually held movement");
            c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = truck.Id,
                Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) }).Accepted,
                "normal policy re-enables autonomous work before release");
            c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = truck.Id, Held = false }).Accepted, "normal release ends manual motion ownership");
            c.That(!truck.Get<IWorker>().Held && truck.Get<IMobility>().Goal != directGoal, "release uses current physical home rather than stale manual goal");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "tile control/home transition remains durable");
        }
    }
}
