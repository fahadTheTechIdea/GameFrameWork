using System;
using System.Collections.Generic;
using System.IO;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ResourceDispositionTests;
using static Beep.SimEngine.Tests.PhysicalRecoveryTestContent;

namespace Beep.SimEngine.Tests
{
    public static class WorkPolicyTests
    {
        public static void Register()
        {
            TestRunner.Add("Work policies: strict content, private commands and shared destination admission", Admission);
            TestRunner.Add("Work policies: physical pause, protected cargo, incident eligibility and saved controls", Physical);
        }
        private static string PointJson => FrameworkProofContent.Read("point-work-policies.json");
        private static string TileJson => FrameworkProofContent.Read("tile-work-policies.json");
        private static GameFactory Points(string extra = null)
            => PointGame.Factory(PointGame.Definitions.Replace("\"worker\": { \"roles\":", "\"work_policy\": {\"profile\":\"policy.initial\"}, \"worker\": { \"roles\":"), extra ?? PointJson);
        private static SimGame NewPoint(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 71 });
        private static Entity Find(SimGame game, string id)
        { foreach (Entity entity in game.World.Entities.All) if (entity.Def.Id == id && entity.Alive) return entity; throw new InvalidOperationException("missing policy entity " + id); }
        private static WorkPolicySettings Settings(bool enabled = true, int priority = 4, string area = null, bool interruptible = true, bool emergency = true, params string[] roles)
        {
            var allowed = new List<WorkerRoleId>(); foreach (string role in roles.Length == 0 ? new[] { "haul", "inspect" } : roles) allowed.Add(new WorkerRoleId(role));
            return new WorkPolicySettings(enabled, priority, allowed, area, interruptible, emergency, WorkAssignmentMode.All);
        }
        private static void Configure(Check c, SimGame game, EntityId worker, WorkPolicySettings settings)
        {
            var command = new ConfigureWorkPolicyCommand { Worker = worker, Settings = settings };
            c.Equal(Code.None, game.Seat(0).View.Feature<WorkView>().PreviewPolicy(worker, settings), "engine view validates current settings");
            c.Equal(Code.None, game.Seat(0).Preview(command), "command uses same policy admission");
            c.That(TestKit.Order(game, 0, command).Accepted, "player configures through the existing command boundary");
        }
        private static void Reach(SimGame game, Func<bool> ready, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 200 && !ready(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!ready())
            {
                var facts = new List<string>(); foreach (var job in game.World.Services.Get<WorkBoard>().Jobs) facts.Add(job.Id + "/" + job.State + "/toil=" + job.ToilIndex + "/" + job.Blocker);
                throw new InvalidOperationException(stage + " timed out at " + game.Tick + ": " + string.Join("; ", facts));
            }
        }
        private static void Admission(Check c)
        {
            GameFactory factory = Points(); SimGame game = NewPoint(factory); Entity machine = Find(game, "machine"), source = Find(game, "source"), target = Find(game, "destination");
            IWorkPolicy policy = machine.Get<IWorkPolicy>(); WorkView view = game.Seat(0).View.Feature<WorkView>();
            c.That(policy != null && ReferenceEquals(policy, machine.Get<WorkPolicyComponent>()), "one component owns the policy capability");
            c.Equal("haul", policy.ReadPolicy().AllowedRoles[0].Value, "authored role set canonicalizes ordinal identities");
            c.Throws<NotSupportedException>(() => ((IList<WorkerRoleId>)policy.ReadPolicy().AllowedRoles).Clear(), "policy roles are immutable");
            c.Equal(Code.None, view.TryReadPolicy(machine.Id, out var retained), "private view exposes detached settings");
            c.Equal(Codes.NotFound, PlayerView.Create(game.World, new PlayerId(1)).Feature<WorkView>().TryReadPolicy(machine.Id, out var hidden), "foreign policy reads preserve private admission");
            c.That(hidden.Settings == null, "refused policy read clears facts");
            ulong before = game.Hash();
            c.Equal(WorkPolicyCodes.InvalidSettings, view.PreviewPolicy(machine.Id, Settings(area: "missing")), "unknown region refuses");
            c.Equal(WorkPolicyCodes.InvalidSettings, view.PreviewPolicy(machine.Id, Settings(roles: new[] { "unknown" })), "unknown role refuses");
            c.Equal(WorkPolicyCodes.InvalidSettings, game.Seat(0).Preview(new ConfigureWorkPolicyCommand { Worker = machine.Id }), "missing settings refuse rather than fabricate a profile");
            c.Equal(WorkPolicyCodes.NotPolicy, view.PreviewPolicy(source.Id, Settings()), "unconfigured endpoint has no implicit policy");
            c.Equal(before, game.Hash(), "policy previews acquire no claims and mutate no state");
            Configure(c, game, machine.Id, Settings(area: "policy.site", roles: new[] { "inspect" }));
            c.That(retained.Settings.Area == null && retained.Settings.AllowedRoles.Count == 2, "retained view does not follow configured state");
            var offers = new List<WorkWorkerCandidate>();
            c.Equal(Code.None, view.PreviewWorkerCandidates("policy.work", "work", target.Id, offers), "shared selector projects current policy");
            c.Equal("inspect", offers[0].Role.Value, "policy allows another compatible role rather than rejecting first authored role");
            c.Equal(WorkRequirementStatus.Satisfied, offers[0].Result.Status, "worker may start outside destination area and travel physically");
            var dispatch = game.World.Services.Get<IWorkerDispatch>();
            c.Equal(WorkPolicyCodes.OutsideArea, dispatch.CheckAssignment(game.World, machine, new PlayerId(0), new WorkerRoleId("inspect"), new[] { source }, false), "destination-aware contract shares area refusal");
            c.Equal(Codes.InvalidArgument, dispatch.CheckAssignment(game.World, machine, new PlayerId(0), new WorkerRoleId("inspect"), Array.Empty<Entity>(), false), "destination-blind admission is removed");
            SimGame other = NewPoint(factory);
            c.Equal(Codes.NotFound, dispatch.CheckAssignment(game.World, machine, new PlayerId(0), new WorkerRoleId("inspect"), new[] { Find(other, "destination") }, false), "cross-session destination refuses");
            Configure(c, game, machine.Id, Settings(area: "policy.site"));
            ResourceId supply = game.World.Content.ResourceId("supply"); IHaulExecution haul = game.World.Services.Get<IHaulExecution>(); before = game.Hash();
            HaulPreview refused = game.Seat(0).View.Feature<LogisticsView>().PreviewHaul(source.Id, target.Id, supply, Fix.FromInt(4));
            c.Equal(HaulStatus.PolicyBlocked, refused.Status, "haul refuses when pickup is outside, even though delivery fits");
            c.Equal(WorkPolicyCodes.OutsideArea, refused.Blocker, "haul preview exposes the exact policy blocker");
            c.Equal(0, haul.AcquireHaul(new PlayerId(0), source.Id, target.Id, supply, Fix.FromInt(4), 50), "policy refusal precedes all native promises");
            c.Equal(before, game.Hash(), "haul refusal consumes no job ID, receipt sequence or carrier");
            Configure(c, game, machine.Id, Settings(area: "policy.both"));
            c.Equal(HaulStatus.Ready, haul.PreviewHaul(new PlayerId(0), source.Id, target.Id, supply, Fix.FromInt(4)).Status, "both permitted stops produce the ordinary native haul preview");
            Serialize(c, game, machine.Id);
            c.Throws<ContentException>(() => NewPoint(Points(PointJson.Replace("\"enabled\":true,", ""))), "missing authored enabled refuses");
            c.Throws<ContentException>(() => NewPoint(Points(PointJson.Replace("\"area\":null,", ""))), "missing area refuses; null must be explicit");
            c.Throws<ContentException>(() => NewPoint(Points(PointJson.Replace("\"priority\":4", "\"priority\":-1"))), "negative preference refuses");
            c.Throws<ContentException>(() => NewPoint(Points(PointJson.Replace("[\"inspect\",\"haul\"]", "[\"inspect\",\"inspect\"]"))), "duplicate authored roles refuse");
            c.Throws<ContentException>(() => NewPoint(Points(PointJson.Replace("\"area\":null", "\"area\":\"missing\""))), "unknown authored region refuses");
            c.Throws<ArgumentException>(() => new WorkPolicySettings(true, 0, null, null, true, true, WorkAssignmentMode.All), "settings require explicit roles");
            Configure(c, game, machine.Id, new WorkPolicySettings(true, 0, Array.Empty<WorkerRoleId>(), null, true, true, WorkAssignmentMode.All));
            c.Equal(WorkPolicyCodes.Role, dispatch.CheckAssignment(game.World, machine, new PlayerId(0), new WorkerRoleId("inspect"), new[] { target }, false), "explicit empty role set authorizes no work");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "saved job blocker and policy layout breaks with save27");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, game.World.Services.Get<WorkBoard>().StateSchemaVersion, "one work codec owns job blockers");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "empty role policy restores without inferred permissions");
            Priority(c);
        }
        private static void Serialize(Check c, SimGame game, EntityId worker)
        {
            var command = new ConfigureWorkPolicyCommand { Worker = worker, Settings = Settings(priority: int.MaxValue, area: "policy.site", interruptible: false, emergency: false, roles: new[] { "inspect" }) };
            using (var stream = new MemoryStream())
            {
                var writer = new StateWriter(stream); command.Sync(writer); writer.Flush(); stream.Position = 0;
                byte[] bytes = stream.ToArray(); var copy = new ConfigureWorkPolicyCommand(); copy.Sync(new StateReader(bytes, 0, bytes.Length));
                c.Equal(command.Worker, copy.Worker, "command stream retains actual worker identity");
                c.Equal(int.MaxValue, copy.Settings.Priority, "preference uses exact comparisons, not overflowing score");
                c.That(!copy.Settings.Interruptible && !copy.Settings.EmergencyEligible && copy.Settings.Area == "policy.site", "command codec retains every explicit policy field");
            }
        }
        private static void Priority(Check c)
        {
            SimGame game = NewPoint(Points()); Entity nearest = Find(game, "machine"), far = game.World.Spawn(game.World.Content.Get<EntityDef>("machine"), new PlayerId(0)); game.World.Settle();
            far.Get<PointPresence>().Move(game.World, Fix.FromInt(30));
            Configure(c, game, far.Id, Settings(priority: int.MaxValue));
            var offers = new List<WorkWorkerCandidate>(); game.Seat(0).View.Feature<WorkView>().PreviewWorkerCandidates("policy.work", "work", Find(game, "destination").Id, offers);
            c.Equal(far.Id, offers[0].Worker, "explicit preference precedes real travel cost");
            c.Equal(int.MaxValue, offers[0].Priority, "offer projects authoritative worker preference");
            HaulPreview haul = game.Seat(0).View.Feature<LogisticsView>().PreviewHaul(Find(game, "source").Id, Find(game, "destination").Id,
                game.World.Content.ResourceId("supply"), Fix.FromInt(4));
            c.Equal(far.Id, haul.Carrier, "hauling uses the same preference before cost and stable identity");
            Configure(c, game, far.Id, Settings(enabled: false, priority: int.MaxValue));
            game.Seat(0).View.Feature<WorkView>().PreviewWorkerCandidates("policy.work", "work", Find(game, "destination").Id, offers);
            c.Equal(nearest.Id, offers[0].Worker, "priority never admits a disabled machine");
            c.That(offers.Exists(x => x.Worker == far.Id && x.Result.Blocker == WorkPolicyCodes.Disabled), "disabled candidate has typed private diagnostics");
        }
        private static void Physical(Check c) { Hauling(c); Working(c); Responses(c); Home(c); }
        private static void Hauling(Check c)
        {
            GameFactory factory = Points(); SimGame game = NewPoint(factory); Entity machine = Find(game, "machine"), source = Find(game, "source"), target = Find(game, "destination");
            ResourceId supply = game.World.Content.ResourceId("supply"); IHaulExecution haul = game.World.Services.Get<IHaulExecution>();
            int id = haul.AcquireHaul(new PlayerId(0), source.Id, target.Id, supply, Fix.FromInt(4), 50); Job job = game.World.Services.Get<WorkBoard>().Job(id);
            c.That(id > 0, "ordinary haul acquires actual carrier and complete receipts");
            Reach(game, () => job.Loaded.IsPositive, "policy haul pickup");
            Configure(c, game, machine.Id, Settings(enabled: false));
            Fix position = machine.Get<PointPresence>().Position; ReservationToken cargo = job.CargoOutReservation, inlet = job.InReservation;
            c.Equal(JobState.Paused, job.State, "restrictive policy pauses actual loaded haul");
            c.Equal(WorkPolicyCodes.Disabled, job.Blocker, "actual job has typed saved blocker");
            c.Equal(id, machine.Get<IWorker>().Job, "loaded pause preserves exclusive carrier claim");
            c.Equal(new Code("point.blocked"), machine.State, "entity Update commits the authored disabled-policy state");
            c.Equal(Fix.FromInt(4), machine.Get<ITransporter>().Cargo.Amount(supply), "loaded cargo remains real stock");
            for (int i = 0; i < 3; i++) Tick(game);
            c.Equal(position, machine.Get<PointPresence>().Position, "policy pause stops motion without teleporting");
            c.Equal(cargo, job.CargoOutReservation, "pause preserves actual cargo token"); c.Equal(inlet, job.InReservation, "pause preserves actual destination token");
            var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
            c.That(jobs.Exists(x => x.Id == id && x.IsHaul && x.Blocker == WorkPolicyCodes.Disabled && x.Loaded == Fix.FromInt(4)), "private job view reports stranded loaded work truthfully");
            PlayerView.Create(game.World, new PlayerId(1)).Feature<WorkView>().CopyJobs(jobs); c.Equal(0, jobs.Count, "foreign job facts clear");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "paused loaded policy/receipts/position restore exactly");
            Code reason = job.Blocker; job.Blocker = Code.None;
            c.Throws<CorruptStateException>(() => factory.Load(game.Save()), "fabricated paused haul without its policy reason refuses restore"); job.Blocker = reason;
            Configure(c, game, machine.Id, Settings(area: "policy.site")); Configure(c, twin, machine.Id, Settings(area: "policy.site"));
            Reach(game, () => target.Get<IInletPort>().Amount(supply) == Fix.FromInt(4), "policy haul resume", twin);
            c.Equal(Fix.Zero, machine.Get<ITransporter>().Cargo.Amount(supply), "real delivery empties protected cargo");
            c.Equal(Fix.FromInt(16), source.Get<IInletPort>().Amount(supply), "policy resume never duplicates pickup");
            c.Equal(game.Hash(), twin.Hash(), "saved loaded twins deliver identically");
        }
        private static void Working(Check c)
        {
            GameFactory factory = Points(); SimGame game = NewPoint(factory); Entity machine = Find(game, "machine"), target = Find(game, "destination");
            Configure(c, game, machine.Id, Settings(area: "policy.site", roles: new[] { "inspect" }));
            CommandResult result = TestKit.Order(game, 0, new PostOrderCommand { Target = target.Id, Plan = "policy.work" }); c.That(result.Accepted, "player posts policy-controlled work");
            Order order = game.World.Services.Get<WorkBoard>().Order((int)result.Arg0);
            Reach(game, () => order.Node("work").Progress.WholeTicks > 0, "policy worker arrival"); WorkProgress progress = order.Node("work").Progress;
            int job = machine.Get<IWorker>().Job;
            Configure(c, game, machine.Id, Settings(area: "policy.source"));
            c.Equal(WorkPolicyCodes.OutsideArea, order.Node("work").Blocker, "remaining target refusal pauses current node");
            for (int i = 0; i < 3; i++) Tick(game);
            c.Equal(progress, order.Node("work").Progress, "policy change preserves exact work progress"); c.Equal(job, machine.Get<IWorker>().Job, "policy wait keeps actual worker identity");
            SimGame twin = factory.Load(game.Save());
            Configure(c, game, machine.Id, Settings(area: "policy.site", roles: new[] { "inspect" })); Configure(c, twin, machine.Id, Settings(area: "policy.site", roles: new[] { "inspect" }));
            Reach(game, () => order.State == OrderState.Done, "policy work resume", twin); c.Equal(game.Hash(), twin.Hash(), "policy work saved continuation is deterministic");
        }
        private static SimGame Tiles(Check c, GameFactory factory)
        {
            SimGame game = New(factory);
            ComponentList<IWorker> workers = game.World.Entities.Components<IWorker>();
            for (int i = 0; i < workers.Count; i++)
            {
                IWorker worker = workers[i];
                c.That(TestKit.Order(game, worker.Entity.Owner.Value, new MoveVehicleCommand { Vehicle = worker.Entity.Id, To = worker.Entity.Get<ITilePresence>().Tile }).Accepted, "player holds existing fixture workers before assigning a policy fleet");
            }
            return game;
        }
        private static void Responses(Check c)
        {
            GameFactory factory = Factory(TileJson); SimGame game = Tiles(c, factory);
            Entity target = Spawn(game, "policy.target", 20, 20), machine = Spawn(game, "policy.external", 16, 20); Tick(game);
            Configure(c, game, machine.Id, Settings(area: "policy.field", interruptible: false, emergency: false, roles: new[] { "crane" }));
            c.That(TestKit.Order(game, 0, new RaiseIncidentCommand { Target = target.Id, Source = new Code("damage"), Kind = new Code("proof.failure") }).Accepted, "engine incident posts ordinary response work");
            Order response = game.World.Services.Get<WorkBoard>().ActiveResponseOn(target.Id, new Code("damage"));
            for (int i = 0; i < 3; i++) Tick(game);
            c.Equal(0, machine.Get<IWorker>().Job, "emergency-ineligible machine is not assigned incident work");
            var candidates = new List<WorkWorkerCandidate>();
            c.That(game.Seat(0).View.Feature<WorkView>().TryCopyWorkerCandidates(response.Id, "work", candidates), "response offer diagnostics are private engine facts");
            c.That(candidates.Exists(x => x.Worker == machine.Id && x.Result.Blocker == WorkPolicyCodes.Emergency), "incident candidate has explicit policy reason even when another worker is merely held");
            Configure(c, game, machine.Id, Settings(area: "policy.field", interruptible: false, roles: new[] { "crane" }));
            Reach(game, () => machine.Get<IWorker>().Job != 0, "response eligible assignment"); int job = machine.Get<IWorker>().Job;
            ulong before = game.Hash();
            c.Equal(WorkPolicyCodes.NotInterruptible, game.Seat(0).Preview(new MoveVehicleCommand { Vehicle = machine.Id, To = new TileCoord(18, 16) }), "manual commandeering respects actual policy");
            c.Equal(before, game.Hash(), "interruption preview is pure");
            c.Equal(WorkPolicyCodes.NotInterruptible, game.World.System<WorkSystem>().Interrupt(game.World, game.World.Services.Get<WorkBoard>().Job(job)), "direct engine interruption shares refusal");
            c.Equal(job, machine.Get<IWorker>().Job, "refused interruption retains actual claim");
            SimGame twin = factory.Load(game.Save()); Reach(game, () => !target.Get<ContractIncident>().ReadIncident().Active, "physical policy response", twin);
            c.Equal(game.Hash(), twin.Hash(), "external worker policy response restores and completes without scheduler edits");
            c.Equal(0, machine.Get<IWorker>().Job, "completed incident releases actual machine");
            Configure(c, game, machine.Id, Settings(interruptible: false, emergency: false, roles: new[] { "crane" }));
            CommandResult ordinary = TestKit.Order(game, 0, new PostOrderCommand { Target = target.Id, Plan = "policy.tile-work" });
            c.That(ordinary.Accepted, "ordinary high-priority-capable work is not falsely classified as an emergency");
            Order work = game.World.Services.Get<WorkBoard>().Order((int)ordinary.Arg0);
            Reach(game, () => work.Node("work").Progress.WholeTicks > 0, "ordinary work with emergency disabled");
            c.Equal(Fix.FromInt(8), target.Get<IInventory>().Amount(game.World.Content.ResourceId("steel")), "policy-controlled reached work consumes actual startup once");
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = work.Id }).Accepted, "explicit cancellation remains distinct from manual commandeering");
            c.Equal(Fix.FromInt(8), target.Get<IInventory>().Amount(game.World.Content.ResourceId("steel")), "cancellation does not refund actual prior input");
        }
        private static void Home(Check c)
        {
            SimGame game = NewPoint(Points()); Entity machine = Find(game, "machine"), home = Find(game, "source");
            machine.Get<PointPresence>().Move(game.World, Fix.FromInt(10)); Configure(c, game, machine.Id, Settings(area: "policy.site"));
            c.That(TestKit.Order(game, 0, new SetHomeCommand { Worker = machine.Id, Home = home.Id }).Accepted, "player selects actual home");
            for (int i = 0; i < 3; i++) Tick(game);
            c.Equal(Fix.FromInt(10), machine.Get<PointPresence>().Position, "automatic home return cannot bypass permitted destinations");
            Configure(c, game, machine.Id, Settings(area: "policy.both"));
            c.That(machine.Get<PointPresence>().Position < Fix.FromInt(10) && machine.Get<PointPresence>().Position > Fix.Zero, "permitted home starts physical travel");
            Configure(c, game, machine.Id, Settings(area: "policy.site")); Fix stopped = machine.Get<PointPresence>().Position;
            for (int i = 0; i < 2; i++) Tick(game);
            c.Equal(stopped, machine.Get<PointPresence>().Position, "editing destination policy stops an already moving home return");
            Configure(c, game, machine.Id, Settings(area: "policy.both")); Reach(game, () => machine.Get<PointPresence>().Position == Fix.Zero, "permitted home return");
            c.Equal(Fix.Zero, machine.Get<PointPresence>().Position, "home return remains physical engine motion");
        }
    }
}
