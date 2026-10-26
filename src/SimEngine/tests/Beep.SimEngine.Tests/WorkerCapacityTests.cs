using System;
using System.Collections.Generic;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Authored files, player commands and bounded engine updates; no fleet or automation decisions here.
    internal static class WorkerCapacityTests
    {
        private static readonly PlayerId Company = new PlayerId(0);
        private static readonly WorkerRoleId Inspect = new WorkerRoleId("inspect"), Haul = new WorkerRoleId("haul");
        internal static void Register()
        {
            TestRunner.Add("Worker capacity: role filters, current eligibility and private fleet signals", Contracts);
            TestRunner.Add("Worker capacity: physical dispatch, deferred controls and saved continuation", c => Physical(c, false));
            TestRunner.Add("Worker capacity: independent JSON metadata drives physical fleet dispatch saved controls and replay", c => Physical(c, true));
        }
        private static string Text(JsonValue value) { var output = new StringBuilder(); value.WriteCanonical(output); return output.ToString(); }
        private static GameFactory Factory(bool publicPolicy = false, Action<JsonValue> edit = null, bool independent = false, Action<JsonValue> editStartup = null)
        {
            var document = Json.Parse(FrameworkProofContent.Read("worker-capacity.json"));
            if (publicPolicy) for (int i = 2; i < document.Get("entities").Count; i++)
            {
                var parts = document.Get("entities")[i].Get("components"); parts.Remove("work_policy");
                parts.Set("capacity_work_policy", Json.Parse("{\"profile\":\"docks.worker\",\"fault\":0}"));
            }
            edit?.Invoke(document);
            if (independent) WorkSignalMetadataProof.SelectIndependent(document);
            var alerts = Json.Parse(FrameworkProofContent.Read("company-alerts.json"));
            var neutral = JsonValue.NewArray(); neutral.Add(alerts.Get("company_alert_profiles")[1]); alerts.Set("company_alert_profiles", neutral);
            return ServiceDockTests.Factory(root =>
            {
                // This explicit master document owns the entities/policies; no duplicate catalog entries.
                root.Remove("entities"); root.Remove("work_policies");
                editStartup?.Invoke(root);
                root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(WorkSignalsModule.ModuleId));
                root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(AutomationModule.ModuleId));
                root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(CompanyAlertModule.ModuleId));
                root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.worker-capacity"));
                root.Get("scenarios")[0].Get("settings").Set(CompanyAlertModule.ModuleId,
                    Json.Parse("{\"companies\":[{\"company\":0,\"profile\":\"alerts.neutral\",\"level\":\"normal\",\"worker_reserves\":[]},{\"company\":1,\"profile\":\"alerts.neutral\",\"level\":\"normal\",\"worker_reserves\":[]}]}"));
            }, extraModules: new GameModule[] { new WorkSignalsModule(), new AutomationModule(), new WorkerCapacityProofModule() },
                extraDeclarations: "[{\"id\":\"sim.work-signals\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[\"worker-capacity.json\"]},{\"id\":\"sim.automation\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[]},{\"id\":\"proof.worker-capacity\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[]}]",
                extraDocuments: new Dictionary<string, string> { ["worker-capacity.json"] = Text(document) }, alertProfiles: Text(alerts));
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 9169 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static Entity Find(SimGame game, string id)
        { foreach (var entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == id) return entity; throw new InvalidOperationException("missing capacity entity " + id); }
        private static WorkView View(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<WorkView>();
        private static WorkerCapacitySnapshot Read(SimGame game, WorkerRoleId? role = null, string worker = null)
        {
            Code refusal = View(game).ReadWorkerCapacity(Company, role ?? WorkerRoleId.None,
                worker == null ? EntityId.None : Find(game, worker).Id, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("worker capacity refused: " + refusal); return value;
        }
        private static AutomationSignalSnapshot Signal(SimGame game, string metric, string entity = "source", string feed = "fleet")
        {
            Code refusal = game.Seat(0).View.Feature<OperationsView>().ReadSignal(Find(game, entity).Id, new Code(feed), "fleet." + metric, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("fleet signal binding refused: " + refusal); return value;
        }
        private static void Step(SimGame game, SimGame twin = null)
        { game.Update(); twin?.Update(); if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("fleet update fault: " + game.Fault + "/" + twin?.Fault); }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 400 && !done(); i++) Step(game, twin);
            if (!done()) { var value = Read(game); throw new InvalidOperationException(stage + " timeout at " + game.Clock.StepIndex + "; total=" + value.TotalWorkers + "; busy=" + value.BusyWorkers + "; idle=" + value.IdleWorkers + "; ready=" + value.OrdinaryReady + WorkLoadSignalTests.DescribeJobs(game)); }
        }
        private static void Policy(Check c, SimGame game, string id, WorkAssignmentMode mode, bool enabled = true, bool emergency = true)
        {
            var actor = Find(game, id); var current = WorkPolicyRules.Read(actor);
            var settings = new WorkPolicySettings(enabled, current.Priority, current.AllowedRoles, current.Area, current.Interruptible, emergency, mode);
            c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = actor.Id, Settings = settings }).Accepted, "player changes actual worker policy");
        }
        private static void Reserves(Check c, SimGame game, int minimum)
        {
            var settings = new CompanyAlertSettings("alerts.neutral", CompanyAlertLevel.Normal, new[] { new WorkerReserveSetting(Inspect, minimum) });
            c.That(TestKit.Order(game, 0, new ConfigureCompanyAlertCommand { Company = Company, Settings = settings }).Accepted, "normal company command changes existing floors");
        }
        private static void Partition(Check c, WorkerCapacitySnapshot value)
        {
            c.Equal(value.TotalWorkers, value.BusyWorkers + value.HeldWorkers + value.IdleWorkers, "actual worker identities partition once");
            c.That(value.OrdinaryReady <= value.IdleWorkers && value.EmergencyReady <= value.IdleWorkers, "readiness never includes busy or held machines");
        }
        private static void Contracts(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "one new engine identity"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no extra capacity persistence owner");
            foreach (bool publicPolicy in new[] { false, true })
            {
                var factory = Factory(publicPolicy); var game = New(factory); var query = game.World.Services.Get<IWorkLoad>();
                c.That(ReferenceEquals(query, game.World.Services.Get<WorkBoard>()), "fleet queries alias the sole board");
                c.That(Find(game, "machine.a").Get<IWorker>() is Worker && Find(game, "machine.b").Get<IWorker>() is ContractWorker, "native and unrelated public worker participate");
                ulong before = game.Hash(); var retained = Read(game); Partition(c, retained);
                c.Equal(4, retained.TotalWorkers, "multi-role machine counted once"); c.Equal(4, retained.OrdinaryReady, "all current workers eligible");
                c.Equal(4, retained.EmergencyReady, "all current emergency workers eligible");
                c.Equal(3, Read(game, Haul).TotalWorkers, "role filter excludes actual public inspect-only worker"); c.Equal(4, Read(game, Inspect).TotalWorkers, "inspect is independently authored");
                c.Equal(1, Read(game, worker: "machine.b").TotalWorkers, "individual scope retains actual identity"); c.Equal(0, Read(game, Haul, "machine.b").TotalWorkers, "owned worker without queried role is genuine zero");
                c.Equal(Fix.FromInt(4), Signal(game, "total").Value.Value, "JSON signal reads exact fleet count"); c.Equal(Fix.FromInt(3), Signal(game, "total", feed: "haulers").Value.Value, "JSON role filter reads same query");
                c.Equal(Fix.One, Signal(game, "total", "machine.b").Value.Value, "individual public worker signal reads same query");
                c.That(game.Seat(0).View.Feature<OperationsView>().TryGetSignalSchema(Find(game, "source").Id, new Code("fleet"), out var schema) && schema.Version == 2 && schema.Bindings.Count == 14,
                    "capacity extends existing declared workload schema, not another signal registry");
                c.That(!Signal(game, "jobs").Available && !Signal(game, "total", feed: "backlog").Available, "unsupported metric/scope withholds facts, not fake zero");
                c.Equal(before, game.Hash(), "all observations preserve clock and authoritative state");
                c.Equal(Codes.NotFound, View(game, 1).ReadWorkerCapacity(Company, WorkerRoleId.None, EntityId.None, out var hidden), "company capacity remains private"); c.Equal(0, hidden.TotalWorkers, "refusal returns no partial fleet");
                c.Equal(Codes.NotFound, PlayerView.Create(game.World, new PlayerId(9)).Feature<WorkView>().ReadWorkerCapacity(Company, WorkerRoleId.None, EntityId.None, out _), "unseated viewer is not observer");
                c.Equal(Codes.NotSeated, query.ReadWorkerCapacity(new PlayerId(9), WorkerRoleId.None, EntityId.None, out _), "unknown company refuses");
                c.Equal(Codes.InvalidArgument, query.ReadWorkerCapacity(Company, new WorkerRoleId("missing"), EntityId.None, out _), "missing role refuses");
                c.Equal(Codes.NotFound, query.ReadWorkerCapacity(Company, Inspect, new EntityId(99999), out _), "missing worker is not substituted");
                c.Equal(StandardCodes.NotWorker, query.ReadWorkerCapacity(Company, Inspect, Find(game, "source").Id, out _), "nonworker subject refuses");
                c.Equal(Codes.NotOwned, query.ReadWorkerCapacity(new PlayerId(1), Inspect, Find(game, "machine.a").Id, out _), "foreign individual refuses");
                c.Equal(Code.None, View(game, 1).ReadWorkerCapacity(new PlayerId(1), WorkerRoleId.None, EntityId.None, out var empty), "other company has actual empty fleet"); c.Equal(0, empty.TotalWorkers, "empty fleet is known zero");
                Policy(c, game, "machine.a", WorkAssignmentMode.EmergencyOnly);
                c.Equal(3, Read(game).OrdinaryReady, "standby removes ordinary eligibility"); c.Equal(4, Read(game).EmergencyReady, "standby retains response eligibility");
                Policy(c, game, "machine.b", WorkAssignmentMode.All, emergency: false);
                c.Equal(3, Read(game).EmergencyReady, "non-emergency public worker is excluded only there");
                c.That(WorkerExecutionTestContent.Control(game, Find(game, "machine.c").Id, true, Fix.Zero).Accepted, "normal control authors zero work rate");
                c.Equal(2, Read(game).OrdinaryReady, "zero effective rate is idle but unavailable"); c.Equal(2, Read(game).EmergencyReady, "zero rate unavailable for emergencies too");
                c.That(WorkerExecutionTestContent.Control(game, Find(game, "machine.c").Id, false, Fix.One).Accepted, "normal control disables actual execution");
                c.Equal(4, Read(game).IdleWorkers, "disabled is still physically idle, not busy");
                var actor = Find(game, "machine.d"); actor.Get<IWorker>().Held = true; actor.Touch(); // Kernel permission instrumentation, not a synthetic job.
                c.Equal(1, Read(game).HeldWorkers, "actual manual hold is distinct from idle"); c.Equal(1, Read(game).OrdinaryReady, "held actor unavailable"); Partition(c, Read(game));
                actor.Get<IWorker>().Held = false; actor.Touch();
                Policy(c, game, "machine.a", WorkAssignmentMode.All); Policy(c, game, "machine.b", WorkAssignmentMode.All);
                c.That(WorkerExecutionTestContent.Control(game, Find(game, "machine.c").Id, true, Fix.One).Accepted, "restore existing execution through normal control");
                Reserves(c, game, 4); c.Equal(0, Read(game).OrdinaryReady, "company floor prevents every fresh ordinary candidate"); c.Equal(4, Read(game).EmergencyReady, "emergency capacity bypasses ordinary floor only in observation");
                Reserves(c, game, 3); c.Equal(4, Read(game).OrdinaryReady, "individual eligibility is not simultaneous four-worker acquisition");
                c.Equal(4, retained.IdleWorkers, "retained snapshot remains detached"); c.Equal(4, retained.OrdinaryReady, "later policy changes never rewrite retained facts");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "capacity introduces no new persistent state"); Step(game, twin); c.Equal(game.Hash(), twin.Hash(), "current readiness restores from actual owners");
            }
            foreach (string field in new[] { "id", "scope", "role" })
                c.Throws<ContentException>(() => New(Factory(edit: file => file.Get("entities")[0].Get("components").Get("work_load_signal")[0].Remove(field))), "required fleet authoring refuses " + field);
            c.Throws<ContentException>(() => New(Factory(edit: file => file.Get("entities")[0].Get("components").Get("work_load_signal")[0].Set("role", JsonValue.Of("missing")))), "unresolved role refuses composition");
            c.Throws<ContentException>(() => New(Factory(edit: file => file.Get("entities")[0].Get("components").Get("work_load_signal")[0].Set("role", JsonValue.Number("1")))), "numeric role is not a guessed ID");
            c.Throws<ContentException>(() => New(Factory(edit: file => file.Get("entities")[0].Get("components").Get("work_load_signal")[0].Set("scope", JsonValue.Of("fleet_worker")))), "nonworker individual source refuses composition");
            c.Throws<ContentException>(() => New(Factory(edit: file => file.Get("signals")[0].Get("providers")[0].Set("schema", JsonValue.Number("1")))), "old signal schema is not accepted");
            c.Throws<ContentException>(() => New(Factory(edit: file => file.Get("signals")[0].Set("unit", JsonValue.Of("unit.wrong")))), "fleet counts have exact authored units");
            c.Throws<InvalidOperationException>(() => { var impure = New(Factory(true, file => file.Get("entities")[2].Get("components").Get("capacity_work_policy").Set("fault", JsonValue.Number("3"))));
                Read(impure); }, "scoped impure policy cannot publish usable fleet readiness");
        }
        private static bool Enabled(SimGame game)
        { if (!game.Seat(0).View.Feature<OperationsView>().TryGetControl(Find(game, "machine.d").Id, new Code("availability"), out var value)) throw new InvalidOperationException("missing existing execution control"); return value.Enabled; }
        private static AutomationRuleSnapshot Rule(SimGame game)
        { if (!game.Seat(0).View.Feature<OperationsView>().TryGetAutomationRule(Company, "fleet", out var value)) throw new InvalidOperationException("missing fleet rule"); return value; }
        private static void Physical(Check c, bool independent)
        {
            foreach (bool publicPolicy in new[] { false, true })
            {
                var factory = Factory(publicPolicy, edit: document => {
                    var entities = document.Get("entities");
                    var slots = entities[0].Get("components").Get("point_dock").Get("slots");
                    for (int i = 0; i < slots.Count; i++)
                        if (slots[i].Get("id").Text == "load.exit") slots[i].Set("offset", JsonValue.Number("0"));
                    // A real exit behind the bay avoids crossing the next worker's waiting position.
                }, independent: independent, editStartup: root => {
                    var entries = root.Get("entity_setups");
                    for (int i = 4; i < entries.Count; i++)
                        entries[i].Get("placement").Set("position", JsonValue.Number((24 + i).ToString(System.Globalization.CultureInfo.InvariantCulture)));
                }); var game = New(factory); var isolated = New(factory); ulong isolatedHash = isolated.Hash();
                var limits = new ReplayLimits(4 * 1024 * 1024, 2000); using var recorder = game.RecordReplay(limits);
                var definition = game.World.Content.Get<IControlRule>("fleet.balance");
                var settings = new AutomationSettings("fleet.balance", true, new ControlBinding(Find(game, "machine.d").Id, new Code("availability")),
                    new Dictionary<string, ControlBinding> { ["idle"] = new ControlBinding(Find(game, "source").Id, new Code("fleet")) }, ControlThresholds.Initial(definition), definition.WhenTrue, definition.WhenFalse);
                c.That(TestKit.Order(game, 0, new ConfigureAutomationCommand { Rule = "fleet", Settings = settings }).Accepted, "ordinary JSON rule configured through engine command");
                Until(game, () => Rule(game).HasApplied, "initial deferred fleet observation");
                var result = TestKit.Order(game, 0, new PostOrderCommand { Plan = "inspect", Target = Find(game, "source").Id });
                c.That(result.Accepted, "normal player starts physical parallel inspection");
                var order = game.World.Services.Get<WorkBoard>().Order(checked((int)result.Arg0));
                Until(game, () => Read(game).BusyWorkers == 2, "two actual machine claims");
                c.Equal(2, Read(game).IdleWorkers, "actual dispatch changes fleet capacity"); Partition(c, Read(game));
                c.Equal(Fix.FromInt(2), Signal(game, "busy").Value.Value, "automation sees real busy identities");
                c.Equal(Fix.FromInt(2), Signal(game, "idle").Value.Value, "automation sees real spare identities");
                var busySnapshot = Read(game);
                c.That(Enabled(game), "dispatch cannot cause same-update spare-worker control feedback");
                Step(game);
                c.That(Rule(game).PendingDecision == false && Enabled(game), "capacity observation schedules an ordinary disable for the next update");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "pending fleet decision and active jobs save through existing owners");
                game.Clock.Pause(); twin.Clock.Pause(); long update = game.Clock.StepIndex; Step(game, twin);
                c.Equal(update, game.Clock.StepIndex, "single clock pause stops work and automation"); c.Equal(game.Hash(), twin.Hash(), "paused twins agree");
                c.That(Enabled(game) && Rule(game).PendingDecision == false, "pause cannot commit the pending decision");
                game.Clock.Resume(); twin.Clock.Resume();
                Until(game, () => !Enabled(game), "deferred spare-worker control", twin);
                c.Equal(2, Read(game).BusyWorkers, "capacity automation never cancels active workers");
                c.Equal(1, Read(game).OrdinaryReady, "disabled idle machine is not advertised ready");
                c.That(order.State != OrderState.Done, "real travel and inspection still take game time");
                c.Equal(game.Hash(), twin.Hash(), "restored pending decision commits identically through normal control");
                Until(game, () => order.State == OrderState.Done && Read(game).BusyWorkers == 0 && Enabled(game), "physical work, dock departure and deferred restart", twin);
                c.Equal(OrderState.Done, order.State, "whole authored parallel workflow completes physically");
                c.Equal(game.Hash(), twin.Hash(), "saved twins continue identically through physical completion");
                c.Equal(4, Read(game).IdleWorkers, "completed/departed machines return to actual idle capacity"); c.Equal(4, Read(game).OrdinaryReady, "normal deferred restart restores current eligibility");
                c.Equal(2, busySnapshot.BusyWorkers, "retained active snapshot does not follow completion");
                c.Equal(isolatedHash, isolated.Hash(), "interleaved session has no shared runtime capacity state");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed checkpoint restores all existing owners");
                Reserves(c, game, 3);
                c.Equal(4, Read(game).OrdinaryReady, "individual eligibility before acquisition does not reserve four workers");
                var next = TestKit.Order(game, 0, new PostOrderCommand { Plan = "inspect", Target = Find(game, "destination").Id }); c.That(next.Accepted, "next physical workflow admitted normally");
                Until(game, () => Read(game).BusyWorkers > 0, "next actual dispatch");
                c.Equal(1, Read(game).BusyWorkers, "actual acquisition rechecks floor and cannot assign the second parallel worker");
                c.Equal(0, Read(game).OrdinaryReady, "remaining protected idle machines refuse fresh ordinary acquisition");
                c.Equal(3, Read(game).EmergencyReady, "remaining protected machines retain target-independent emergency eligibility");
                c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = checked((int)next.Arg0) }).Accepted, "player cancellation releases only native worker claims");
                c.Equal(0, Read(game).BusyWorkers, "cancelled machines no longer count busy"); Partition(c, Read(game));
                var verified = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
                c.That(verified.Success, "fleet controls and physical dispatch replay through original owners");
                c.Equal(game.Hash(), verified.ActualHash, "fleet replay reaches the actual final live state");
            }
        }
    }
}
