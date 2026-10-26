using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Testing;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Authored fixture content, ordinary player commands, bounded updates and engine observations.
    internal static class CompletionServiceTests
    {
        private static readonly Code Service = new Code("service.interval"), Integrity = new Code("integrity");
        internal static void Register()
        {
            TestRunner.Add("Completion service: authored cadence, private admission and retained success", Contracts);
            TestRunner.Add("Completion service: timed work, materials, restoration and saved continuation", Physical);
        }
        private static JsonValue Parts(JsonValue root, bool external) => root.Get("entities")[external ? 3 : 2].Get("components");
        private static JsonValue Source(JsonValue root, bool external) => Parts(root, external).Get(external ? "proof_service_recurring" : "recurring_activity");
        private static GameFactory Factory(bool external = false, Action<JsonValue> edit = null, bool failedService = false)
            => WorkerServiceTests.Factory(external, root =>
            {
                var source = Json.Parse("{\"id\":\"service.interval\",\"plan\":\"activity:completion.service\",\"template\":null,\"cadence\":\"successful_completion\",\"trigger\":\"schedule\",\"dispatch_mode\":\"compete\",\"target\":\"worker_home\",\"arguments\":null,\"facility_policy\":null,\"reserve_use\":\"none\",\"worker_nodes\":[\"service\"],\"enabled\":false,\"priority\":60,\"group\":\"fleet.intervals\",\"operation_policy\":null,\"interval\":{\"numerator\":40,\"denominator\":1,\"unit\":\"minute\"},\"conditions\":[]}");
                if (external)
                { source.Remove("conditions"); source.Set("fault", JsonValue.Number("0")); source.Set("idle_only", JsonValue.Of(false)); }
                if (failedService) source.Set("plan", JsonValue.Of("activity:completion.failed-service"));
                Parts(root, external).Set(external ? "proof_service_recurring" : "recurring_activity", source); edit?.Invoke(root);
            }, activityDocument: "completion-service.json", completionDocument: failedService ? "completion-failed-service.json" : null);
        private static GameFactory ReadinessFactory(bool external, string trigger, bool wear = false, int initial = 10, int spares = 32, bool serviceFirst = false, int destinationStock = 20)
            => Factory(external, root =>
            {
                var parts = Parts(root, external); var source = Source(root, external);
                source.Set("trigger", JsonValue.Of(trigger)); source.Set("operation_policy", JsonValue.Of("safety"));
                source.Set("dispatch_mode", JsonValue.Of(serviceFirst ? "service_first" : "compete"));
                parts.Remove("proof_service_condition");
                var condition = Json.Parse("{\"id\":\"integrity\",\"unit\":\"unit.condition\",\"maximum\":10,\"initial\":10,\"wear\":null,\"states\":{\"damaged\":\"blocked\",\"failed\":\"blocked\"}}");
                condition.Set("initial", JsonValue.Number(initial.ToString()));
                if (wear) condition.Set("wear", JsonValue.Of("completion.wear"));
                parts.Set("equipment_condition", condition);
                parts.Set("proof_service_operation", Json.Parse("{\"operation\":{\"id\":\"process\",\"enabled\":true,\"target\":1,\"minimum\":0,\"maximum\":2,\"unit\":\"unit.work-rate\"},\"level\":1}"));
                var policy = Json.Parse("{\"id\":\"safety\",\"operation\":\"process\",\"condition\":\"integrity\",\"unit\":\"unit.condition\",\"criticality\":20,\"maintenance_threshold\":4,\"shutdown_threshold\":2,\"restart_threshold\":8,\"restart_policy\":\"automatic\"}");
                if (external) policy.Set("fault", JsonValue.Number("0"));
                parts.Set(external ? "proof_service_safety" : "operation_policy", policy);
                root.Get("entities")[0].Get("components").Get("point_stock").Set("initial", JsonValue.Number(spares.ToString()));
                root.Get("entities")[1].Get("components").Get("point_stock").Set("initial", JsonValue.Number(destinationStock.ToString()));
            });
        private static OperationPolicyView Safety(SimGame game, bool external)
            => OperationPolicyContracts.Read(game.World, Actor(game, external).Get<IOperationPolicy>(new Code("safety")));
        private static void SetMaintenance(Check c, SimGame game, bool external, int threshold, SimGame twin = null)
        {
            foreach (var current in twin == null ? new[] { game } : new[] { game, twin })
                Command(c, current, new ConfigureOperationPolicyCommand { Entity = Actor(current, external).Id, Policy = new Code("safety"),
                    Settings = new OperationPolicySettings(20, Fix.FromInt(threshold), Fix.FromInt(2), Fix.FromInt(10), OperationRestartPolicy.Automatic) },
                    "ordinary policy command changes the existing maintenance threshold");
        }
        private static SimGame Start(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 744 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static Entity Find(SimGame game, string definition)
        { foreach (var entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == definition) return entity; throw new InvalidOperationException(definition); }
        private static Entity Actor(SimGame game, bool external) => Find(game, external ? "machine.b" : "machine.a");
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static RecurringPlanView Read(SimGame game, bool external)
        {
            Code code = game.Seat(0).View.Feature<WorkView>().ReadRecurringPlan(Actor(game, external).Id, Service, out var value);
            if (!code.IsNone) throw new InvalidOperationException("completion service read: " + code); return value;
        }
        private static PlanView Plan(SimGame game, long id)
        {
            var plans = new List<PlanView>(); game.Seat(0).View.Feature<WorkView>().CopyPlans(plans);
            foreach (var plan in plans) if (plan.Id == id) return plan; throw new InvalidOperationException("missing service plan " + id);
        }
        private static Fix Condition(SimGame game, bool external) => ConditionContracts.Read(game.World, Actor(game, external).Get<IConditionProvider>(Integrity)).Value;
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("service update fault: " + game.Fault + "/" + twin?.Fault);
        }
        private static void Reach(SimGame game, bool external, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 400 && !done(); i++) Step(game, twin);
            if (!done())
            {
                var snapshot = Read(game, external);
                throw new InvalidOperationException(stage + " timeout at game time " + game.Clock.TotalGameTime + ": " + snapshot.Blocker +
                    "/latest=" + snapshot.Schedule.LastPlan + "/completed=" + snapshot.Schedule.LastCompletedPlan);
            }
        }
        private static void Command(Check c, SimGame game, Command command, string message)
        {
            c.That(TestKit.Order(game, 0, command).Accepted, message);
            if (game.Fault != null) throw new InvalidOperationException("service command fault: " + game.Fault);
        }
        private static void Configure(Check c, SimGame game, bool external, bool enabled, int minutes = 40)
            => Command(c, game, new ConfigureRecurringPlanCommand { Entity = Actor(game, external).Id, Source = Service,
                Settings = new RecurringPlanSettings(enabled, 60, GameDuration.Minutes(minutes), Code.None) }, "ordinary player controls service interval/enabled state");
        private static void Begin(Check c, SimGame game, bool external)
        {
            Command(c, game, new SetHomeCommand { Worker = Actor(game, external).Id, Home = Find(game, "source").Id }, "ordinary home command selects actual service facility");
            Configure(c, game, external, true);
        }
        private static void Contracts(Check c)
        {
            MultiSourceContracts(c);
            EarnedWindowContracts(c);
            MovementWindowContracts(c);
            DispatchContracts(c);
            TriggerContracts(c);
            PlanTemplateTests.CompletionCadence(c);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current completion service identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "one direct current source completion codec");
            foreach (bool external in new[] { false, true })
            {
                c.Throws<ContentException>(() => Start(Factory(external, root => Source(root, external).Remove("cadence"))), "missing cadence never substitutes legacy/default submission");
                c.Throws<ContentException>(() => Start(Factory(external, root => Source(root, external).Set("cadence", JsonValue.Of("wall_clock")))), "undefined cadence fails before gameplay");
                var factory = Factory(external); var game = Start(factory); var actor = Actor(game, external); var source = actor.Get<IRecurringPlanSource>(Service);
                var before = Read(game, external); ulong hash = game.Hash();
                c.That(before.Cadence == RecurringCadenceBasis.SuccessfulCompletion && before.LastCompletedTick == null && before.Schedule.LastCompletedPlan == 0,
                    "unserviced source carries explicit cadence and no fabricated successful completion");
                c.Equal(Codes.NotFound, game.Seat(1).View.Feature<WorkView>().ReadRecurringPlan(actor.Id, Service, out _), "service timing remains company private");
                c.Equal(hash, game.Hash(), "cadence/private reads create no jobs, history, stock or game time");
                var condition = actor.Get<IConditionProvider>(Integrity); var facts = ConditionContracts.Read(game.World, condition);
                c.Equal(ConditionCodes.Bounds, ConditionContracts.Check(game.World, condition,
                    new ConditionChange(ConditionChangeKind.Restore, new UnitQuantity(facts.Unit, Fix.FromInt(11)))), "restore target cannot exceed real provider maximum");
                c.Equal(Fix.FromInt(10), ConditionContracts.ValueAfter(facts,
                    new ConditionChange(ConditionChangeKind.Restore, new UnitQuantity(facts.Unit, Fix.FromInt(10)))), "same bounded native restoration calculator reaches authored target");
                Begin(c, game, external); long first = Read(game, external).Schedule.LastPlan;
                c.That(first > 0 && Read(game, external).Blocker == RecurringPlanCodes.Outstanding && Read(game, external).DueTick == null,
                    "pending physical service has unknown finish, never posting-plus-interval as completion deadline");
                Reach(game, external, () => Read(game, external).Schedule.LastCompletedPlan == first, "first actual whole service");
                var completed = Read(game, external); var firstPlan = Plan(game, first);
                c.That(completed.LastCompletedTick == firstPlan.EndedTick && completed.LastCompletedTick > completed.Schedule.LastPostedTick,
                    "success timestamp is actual board completion, not a copied source age");
                c.Equal(firstPlan.EndedTick + game.World.Time.ExactTicks(completed.Schedule.Interval), completed.DueTick.Value,
                    "service interval begins only after whole physical cycle completes");
                Configure(c, game, external, false, 1); Configure(c, game, external, true, 1);
                long second = Read(game, external).Schedule.LastPlan;
                c.That(second > first && Read(game, external).Schedule.LastCompletedPlan == first, "new attempt retains actual previous successful cycle");
                c.Equal(PlanCodes.HistorySource, Board(game).PreviewRetirePlanHistory(new PlayerId(0), new PlanHistorySelection(new[] { first })).Blocker,
                    "previous successful cycle stays protected while latest attempt is different");
                Configure(c, game, external, false, 40);
                Command(c, game, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { second }) }, "normal cancellation stops actual current service plan");
                var cancelled = Read(game, external);
                c.That(cancelled.Schedule.LastCompletedPlan == first && cancelled.LastCompletedTick == firstPlan.EndedTick,
                    "cancelled service cannot advance or rebase successful history");
                c.Equal(PlanState.Cancelled, Plan(game, second).State, "cancelled attempt remains actual board history");
                c.That(cancelled.DueTick >= firstPlan.EndedTick + game.World.Time.ExactTicks(cancelled.Schedule.Interval), "failed-attempt retry cadence does not make successful service younger");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled/latest and successful/older source references restore identically");
                // Provider protocol fault instrumentation: a cancelled cycle cannot masquerade as success.
                source.RecordCompletion(game.World, second);
                c.Throws<CorruptStateException>(() => game.Save(), "checkpoint refuses fake successful acknowledgement of a cancelled cycle");
            }
            foreach (int fault in new[] { 16, 17, 18 })
            {
                var game = Start(Factory(true, root => Source(root, true).Set("fault", JsonValue.Number(fault.ToString()))));
                Begin(c, game, true); long cycle = Read(game, true).Schedule.LastPlan;
                Reach(game, true, () => Plan(game, cycle).State == PlanState.Done, "real completion before dishonest acknowledgement");
                long time = game.Clock.StepIndex; game.Update();
                c.That(game.Fault != null && game.Clock.StepIndex == time,
                    "ignored, settings-changing or forged completion acknowledgement faults before the sole game clock advances");
                c.Throws<InvalidOperationException>(() => game.Save(), "faulted completion protocol cannot publish a resumable checkpoint");
            }
        }
        private static void Physical(Check c)
        {
            MultiSourcePhysical(c);
            EarnedWindowPhysical(c);
            MovementWindowPhysical(c);
            DispatchPhysical(c);
            TriggerPhysical(c);
            foreach (bool external in new[] { false, true })
            {
                var factory = Factory(external); var game = Start(factory); Begin(c, game, external); long first = Read(game, external).Schedule.LastPlan;
                Reach(game, external, () => Plan(game, first).Order != 0 && !Board(game).Order(Plan(game, first).Order).Node("service").Progress.Equals(WorkProgress.Zero), "actual selected machine service");
                var order = Board(game).Order(Plan(game, first).Order); var node = order.Node("service");
                c.That(order.WorkerSelection.WorkerFor("service") == Actor(game, external).Id && node.Progress.WholeTicks < 12,
                    "service uses actual authored selected machine and positive timed progress");
                c.That(Condition(game, external) == Fix.FromInt(4) && Read(game, external).Schedule.LastCompletedPlan == 0,
                    "travelling/partial service does not repair or acknowledge completion instantly");
                var twin = factory.Load(game.Save());
                Reach(game, external, () => Condition(game, external) == Fix.FromInt(10), "earned physical restoration", twin);
                c.That(Plan(game, first).State != PlanState.Done && Read(game, external).Schedule.LastCompletedPlan == 0,
                    "completed repair effect alone cannot skip physical exit or remaining DAG stages");
                Reach(game, external, () => Plan(game, first).State == PlanState.Done, "whole physical service completion", twin);
                var pending = Read(game, external); ulong pendingHash = game.Hash();
                c.That(pending.Blocker == RecurringPlanCodes.CompletionPending && pending.Schedule.LastCompletedPlan == 0 && pending.LastCompletedTick == null,
                    "settled physical success waits for engine acknowledgement, never a false ready preview");
                c.Equal(Plan(game, first).EndedTick + 40, pending.DueTick.Value,
                    "pending success already derives its actual deadline from the board, not submission time");
                c.Equal(pendingHash, game.Hash(), "pending success preview never mutates source history or game time");
                twin = factory.Load(game.Save());
                c.Equal(pendingHash, twin.Hash(), "successful but unacknowledged cycle saves without fabricating acknowledgement");
                c.Equal(RecurringPlanCodes.CompletionPending, Read(twin, external).Blocker, "restored pending completion preserves refusal semantics");
                Reach(game, external, () => Read(game, external).Schedule.LastCompletedPlan == first, "whole service acknowledgement", twin);
                var completed = Read(game, external); long ended = Plan(game, first).EndedTick;
                c.That(node.Progress.Complete(12) && completed.LastCompletedTick == ended && completed.DueTick == ended + 40,
                    "actual full machine work, departure and later stage determine interval anchor");
                var stock = Find(game, "source").Get<IInventory>(); var resource = game.World.Content.ResourceId("supply");
                c.Equal(Fix.FromInt(29), stock.Amount(resource), "service consumes three real spares, not the two held tools");
                c.Equal(Fix.Zero, stock.ReservedOut(resource), "whole service releases real tool promises");
                c.Equal(game.Hash(), twin.Hash(), "mid-service saved native/public source and condition providers continue identically");
                game.Clock.Pause(); twin.Clock.Pause(); ulong frozen = game.Hash(); long update = game.Clock.StepIndex;
                game.Update(); twin.Update(); c.That(game.Hash() == frozen && game.Clock.StepIndex == update, "the sole game clock freezes due service and resources");
                game.Clock.Resume(); twin.Clock.Resume();
                while (game.Clock.StepIndex < completed.DueTick.Value) { Step(game, twin); c.Equal(first, Read(game, external).Schedule.LastPlan, "no early posting before actual successful interval"); }
                Step(game, twin); long second = Read(game, external).Schedule.LastPlan;
                c.That(second > first && Read(game, external).Schedule.LastCompletedPlan == first, "due healthy inspection is ordinary same-source physical work");
                var secondTwin = factory.Load(game.Save());
                Reach(game, external, () => Read(game, external).Schedule.LastCompletedPlan == second, "periodic healthy service", secondTwin);
                c.Equal(Fix.FromInt(10), Condition(game, external), "restore target neither over-repairs nor damages already healthy condition");
                c.Equal(Fix.FromInt(26), stock.Amount(resource), "healthy periodic inspection still earns work and consumes authored physical spares");
                c.Equal(game.Hash(), secondTwin.Hash(), "second in-flight cycle and successful history checkpoint continue identically");
                c.Equal(Code.None, Board(game).PreviewRetirePlanHistory(new PlayerId(0), new PlanHistorySelection(new[] { first })).Blocker,
                    "superseded successful history is not pinned forever by a duplicate ledger");
            }
        }
        private static void TriggerContracts(Check c)
        {
            foreach (bool external in new[] { false, true })
            {
                c.Throws<ContentException>(() => Start(Factory(external, root => Source(root, external).Remove("trigger"))), "trigger is explicit, never inferred from legacy linkage");
                c.Throws<ContentException>(() => Start(Factory(external, root => Source(root, external).Set("trigger", JsonValue.Of("automatic")))), "undefined trigger fails composition");
                c.Throws<ContentException>(() => Start(Factory(external, root => Source(root, external).Set("trigger", JsonValue.Of("maintenance")))), "condition trigger requires actual policy, not an invented threshold");
                c.Throws<ContentException>(() => Start(Factory(external, root => Source(root, external).Remove("operation_policy"))), "native/public policy binding is explicit even when null");
                var disabled = Start(ReadinessFactory(external, "maintenance", initial: 4));
                c.That(Read(disabled, external).MaintenanceDue && Read(disabled, external).Blocker == RecurringPlanCodes.Disabled &&
                    Read(disabled, external).Schedule.LastPlan == 0, "condition demand never bypasses disabled source intent");
                foreach (string mode in new[] { "schedule", "maintenance", "schedule_or_maintenance" })
                {
                    var game = Start(ReadinessFactory(external, mode)); Begin(c, game, external);
                    var view = Read(game, external); ulong hash = game.Hash();
                    c.That(!view.MaintenanceDue && view.TriggerMode == (mode == "schedule" ? RecurringTriggerMode.Schedule : mode == "maintenance" ? RecurringTriggerMode.Maintenance : RecurringTriggerMode.ScheduleOrMaintenance),
                        "explicit trigger projects actual policy readiness without copying condition bands");
                    c.That(mode == "maintenance" ? view.Schedule.LastPlan == 0 && view.Blocker == OperationPolicyCodes.MaintenanceNotDue : view.Schedule.LastPlan > 0,
                        "healthy scheduled inspections run, healthy condition-only maintenance does not");
                    c.Equal(Codes.NotFound, game.Seat(1).View.Feature<WorkView>().ReadRecurringPlan(Actor(game, external).Id, Service, out _), "policy-linked service readiness remains private");
                    c.Equal(hash, game.Hash(), "trigger and policy views mutate neither source nor condition nor clock");
                }
            }
        }
        private static void TriggerPhysical(Check c)
        {
            foreach (bool external in new[] { false, true })
            {
                foreach (string mode in new[] { "maintenance", "schedule_or_maintenance" })
                {
                    var factory = ReadinessFactory(external, mode, true, 4);
                    var game = Start(factory); Begin(c, game, external); long first = Read(game, external).Schedule.LastPlan;
                    Reach(game, external, () => Read(game, external).Schedule.LastCompletedPlan == first, "initial actual service before readiness proof");
                    var completed = Read(game, external); long scheduled = completed.DueTick.Value;
                    c.That(Condition(game, external) > Fix.FromInt(9) && Safety(game, external).Control.Enabled, "physical service restored real equipment while process remains safe");
                    var twin = factory.Load(game.Save()); SetMaintenance(c, game, external, 9, twin);
                    Reach(game, external, () => Read(game, external).Schedule.LastPlan > first, "wear-driven early service", twin);
                    long second = Read(game, external).Schedule.LastPlan;
                    c.That(game.Clock.StepIndex < scheduled && Read(game, external).MaintenanceDue && Read(game, external).Schedule.LastCompletedPlan == first,
                        "actual wear crosses configured policy band and requests service before scheduled cadence");
                    c.Equal(game.Hash(), twin.Hash(), "native/public policy and recurring sources continue early demand identically");
                    foreach (var current in new[] { game, twin }) Command(c, current, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { second }) }, "cancel actual early attempt through normal command");
                    long retryDue = Read(game, external).DueTick.Value;
                    c.That(Read(game, external).Blocker == RecurringPlanCodes.Cadence && retryDue > game.Clock.StepIndex,
                        "persistent low condition cannot repost cancelled work every update");
                    for (int i = 0; i < 3; i++) Step(game, twin);
                    c.That(Read(game, external).Schedule.LastPlan == second && Read(game, external).Schedule.LastCompletedPlan == first,
                        "cancellation retry wait preserves success history and no duplicate native plans");
                    c.Equal(game.Hash(), twin.Hash(), "early cancellation retains actual cadence on saved continuation");
                }

                var scheduledFactory = ReadinessFactory(external, "schedule", true, 4);
                var scheduledGame = Start(scheduledFactory); Begin(c, scheduledGame, external);
                long scheduledCycle = Read(scheduledGame, external).Schedule.LastPlan;
                Reach(scheduledGame, external, () => Read(scheduledGame, external).Schedule.LastCompletedPlan == scheduledCycle, "schedule-only physical cycle");
                long regularDue = Read(scheduledGame, external).DueTick.Value;
                SetMaintenance(c, scheduledGame, external, 9);
                Reach(scheduledGame, external, () => Read(scheduledGame, external).MaintenanceDue, "same wear band for schedule-only service");
                c.That(scheduledGame.Clock.StepIndex < regularDue && Read(scheduledGame, external).Blocker == RecurringPlanCodes.Cadence &&
                    Read(scheduledGame, external).Schedule.LastPlan == scheduledCycle,
                    "explicit schedule-only mode cannot silently become condition-driven service");

                var shortFactory = ReadinessFactory(external, "maintenance", true, 1, 2);
                var shortage = Start(shortFactory); Begin(c, shortage, external); long repair = Read(shortage, external).Schedule.LastPlan;
                Reach(shortage, external, () => Safety(shortage, external).State.ShutdownLatched, "unsafe operation physically isolated");
                c.That(!Safety(shortage, external).Control.Enabled && repair > 0 && Read(shortage, external).Schedule.LastCompletedPlan == 0,
                    "shutdown and physical repair request share existing policy without instant restoration");
                var stock = Find(shortage, "source").Get<IInventory>(); var resource = shortage.World.Content.ResourceId("supply");
                for (int i = 0; i < 6; i++) Step(shortage);
                c.That(Condition(shortage, external) <= Fix.One && stock.Amount(resource) == Fix.FromInt(2) && Read(shortage, external).Schedule.LastPlan == repair,
                    "missing spares leave the same service blocked with real inventory, no free repair or repeated jobs");
                c.Equal(OperationPolicyCodes.Unsafe, TestKit.Order(shortage, 0, new ConfigureOperationCommand { Entity = Actor(shortage, external).Id,
                    Operation = new Code("process"), Enabled = true, Target = Fix.One }).Refusal, "normal control cannot restart unsafe equipment before physical repair");
                var shortTwin = shortFactory.Load(shortage.Save());
                foreach (var current in new[] { shortage, shortTwin })
                    Command(c, current, new ConfigureStockPolicyCommand { Store = Find(current, "source").Id, Resource = resource,
                        Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(5), Fix.FromInt(10), Fix.FromInt(12), 200, LogisticsReserveAccess.Protected)) },
                        "player requests real spares delivery through existing stock bands");
                c.That(Read(shortage, external).Schedule.LastCompletedPlan == 0 && Condition(shortage, external) <= Fix.One,
                    "stock request and supplier promises cannot complete repair instantly");
                Reach(shortage, external, () => Read(shortage, external).Schedule.LastCompletedPlan == repair && Safety(shortage, external).Control.Enabled,
                    "physical delivery, service work and automatic safe restart", shortTwin);
                c.That(Condition(shortage, external) >= Fix.FromInt(8) && !Safety(shortage, external).State.ShutdownLatched,
                    "only earned physical restoration crosses actual restart hysteresis");
                c.That(Find(shortage, "destination").Get<IInventory>().Amount(resource) < Fix.FromInt(20), "repair consumes supplied spares delivered by real couriers");
                c.Equal(shortage.Hash(), shortTwin.Hash(), "shutdown shortage, actual refill, restoration and restart remain deterministic after save");
            }
        }
        private static Code Idle(SimGame game, bool external, bool emergency = false)
            => game.World.Services.Get<IWorkerDispatch>().CheckIdleWorker(game.World, Actor(game, external), new PlayerId(0), new WorkerRoleId("inspect"), emergency);
        private static GameFactory MovementWindowFactory(bool external = false, Action<JsonValue> edit = null, bool failedService = false)
            => Factory(external, root =>
            {
                Parts(root, external).Set("haul_trip_policy", Json.Parse("{\"profile\":\"completion.trip\"}"));
                edit?.Invoke(root);
            }, failedService);
        private static readonly Code Inspection = new Code("service.inspect");
        private static GameFactory MultiSourceFactory(bool external, bool reverse = false)
            => MovementWindowFactory(external, root =>
            {
                var primary = Source(root, external);
                primary.Set("interval", Json.Parse("{\"numerator\":200,\"denominator\":1,\"unit\":\"minute\"}"));
                primary.Set("dispatch_mode", JsonValue.Of("service_first"));
                var secondary = primary.Clone(); secondary.Set("id", JsonValue.Of(Inspection.Id));
                secondary.Set("plan", JsonValue.Of("activity:completion.inspection"));
                root.Get("activities").Add(Json.Parse("{\"id\":\"completion.inspection\",\"automatic_supply\":false,\"player_orderable\":true,\"target_tags\":[],\"nodes\":[{\"id\":\"service\",\"executor\":\"work.staffed\",\"roles\":[\"inspect\"],\"duration\":{\"numerator\":1,\"denominator\":10,\"unit\":\"hour\"},\"requirement\":\"completion.ready\"}]}"));
                secondary.Set("group", JsonValue.Of("fleet.inspections"));
                secondary.Set("interval", Json.Parse("{\"numerator\":300,\"denominator\":1,\"unit\":\"minute\"}"));
                if (external)
                { secondary.Remove("fault"); secondary.Remove("idle_only"); secondary.Set("conditions", JsonValue.NewArray()); }
                else
                { secondary.Remove("conditions"); secondary.Set("fault", JsonValue.Number("0")); secondary.Set("idle_only", JsonValue.Of(false)); }
                Parts(root, external).Set(external ? "recurring_activity" : "proof_service_recurring", secondary);
                root.Set("haul_trip_profiles", Json.Parse("[{\"id\":\"completion.multi-trip\",\"enabled\":true,\"preserve_outlet_reserve\":false,\"service_sources\":" +
                    (reverse ? "[\"service.inspect\",\"service.interval\"]" : "[\"service.interval\",\"service.inspect\"]") + "}]"));
                Parts(root, external).Get("haul_trip_policy").Set("profile", JsonValue.Of("completion.multi-trip"));
            });
        private static RecurringPlanView InspectionView(SimGame game, bool external)
        {
            Code code = game.Seat(0).View.Feature<WorkView>().ReadRecurringPlan(Actor(game, external).Id, Inspection, out var value);
            if (!code.IsNone) throw new InvalidOperationException("inspection source read: " + code); return value;
        }
        private static void ConfigureInspection(Check c, SimGame game, bool external, bool enabled, GameDuration interval)
            => Command(c, game, new ConfigureRecurringPlanCommand { Entity = Actor(game, external).Id, Source = Inspection,
                Settings = new RecurringPlanSettings(enabled, 60, interval, Code.None) }, "normal player command configures second actual source");
        private static ServiceMovementWindow Window(SimGame game, bool external)
        {
            Code code = game.Seat(0).View.Feature<LogisticsView>().ReadHaulTripServiceWindow(Actor(game, external).Id, out var value);
            if (!code.IsNone) throw new InvalidOperationException("private service window read: " + code); return value;
        }
        private static void BeginBoth(Check c, SimGame game, bool external)
        {
            Command(c, game, new SetHomeCommand { Worker = Actor(game, external).Id, Home = Find(game, "source").Id }, "player selects actual shared service facility");
            Configure(c, game, external, true, 200);
            ConfigureInspection(c, game, external, true, GameDuration.Minutes(300));
        }
        private static void MultiSourceContracts(Check c)
        {
            foreach (bool external in new[] { false, true }) foreach (bool reverse in new[] { false, true })
            {
                var factory = MultiSourceFactory(external, reverse); var game = Start(factory); var worker = Actor(game, external);
                var disabledSources = Window(game, external);
                c.That(disabledSources.Unrestricted && disabledSources.Blocker.IsNone && disabledSources.DueGameTime == null && disabledSources.RemainingGameTime == null,
                    "all explicitly disabled sources are unrestricted, not a fabricated zero deadline");
                BeginBoth(c, game, external); var pending = Window(game, external); ulong hash = game.Hash();
                c.That(!pending.Unrestricted && pending.Source == Inspection && pending.Blocker == RecurringPlanCodes.ServicePending && pending.RemainingUpdateSteps == null,
                    "multiple pending native/public obligations use stable source identity independent of authored array order");
                c.Equal(pending.Blocker, HaulTripPolicyContracts.CheckDispatch(game.World, worker, new MotionTravelEstimate(1, Code.None), out _),
                    "fresh dispatch and private aggregate share exact readiness calculation");
                c.Equal(Codes.NotOwned, game.Seat(1).View.Feature<LogisticsView>().ReadHaulTripServiceWindow(worker.Id, out var hidden), "multi-source readiness stays company private");
                c.That(hidden == null, "foreign view leaks neither limiting source nor deadline");
                c.Throws<ArgumentOutOfRangeException>(() => pending.CheckMovement(-1), "negative prospective time cannot enter shared allowance");
                c.Equal(hash, game.Hash(), "multi-source diagnostics create no history, claims, plans or game time");
                var loaded = factory.Load(game.Save()); var restored = Window(loaded, external);
                c.That(restored.Source == pending.Source && restored.Blocker == pending.Blocker && restored.RemainingGameTime == null,
                    "saved actual sources derive same pending diagnostic without a saved window");
                c.Equal(hash, loaded.Hash(), "derived window adds no authoritative saved state");
                Command(c, game, new ConfigureHaulTripPolicyCommand { Worker = worker.Id, Settings = new HaulTripSettings("completion.multi-trip", false, false) },
                    "normal player command disables trip policy");
                var stopped = Window(game, external);
                c.That(!stopped.Unrestricted && stopped.Source.IsNone && stopped.Blocker == HaulTripPolicyCodes.Disabled && stopped.RemainingGameTime == null,
                    "disabled hauling is explicitly blocked, not unrestricted or a source timer");
                c.That(pending.Blocker == RecurringPlanCodes.ServicePending && disabledSources.Unrestricted, "retained detached diagnostic snapshots do not follow later commands");
            }
        }
        private static void MultiSourcePhysical(Check c)
        {
            foreach (bool external in new[] { false, true }) foreach (bool reverse in new[] { false, true })
            {
                var factory = MultiSourceFactory(external, reverse); var game = Start(factory); BeginBoth(c, game, external);
                long primary = Read(game, external).Schedule.LastPlan, secondary = InspectionView(game, external).Schedule.LastPlan;
                c.That(primary > 0 && secondary > 0 && primary != secondary, "each actual service source owns its own ordinary physical plan");
                var twin = factory.Load(game.Save());
                Reach(game, external, () => Read(game, external).Schedule.LastCompletedPlan == primary && InspectionView(game, external).Schedule.LastCompletedPlan == secondary,
                    "two-source physical service without self-service dispatch deadlock", twin);
                c.That(Plan(game, primary).State == PlanState.Done && Plan(game, secondary).State == PlanState.Done && Condition(game, external) == Fix.FromInt(10),
                    "both actual workflows earn material/tool/time/repair and whole completion through the native board");
                c.Equal(game.Hash(), twin.Hash(), "pending two-source service resumes deterministically from existing source and board codecs");
                var window = Window(game, external); long remaining = window.RemainingUpdateSteps.Value;
                c.That(window.Source == Service && window.DueUpdateStep == Read(game, external).ServiceDueTick && !window.Unrestricted && window.Blocker.IsNone,
                    "engine aggregate selects earliest actual earned service, not first authored/public provider");
                c.Equal(remaining, game.World.Time.ExactTicks(window.RemainingGameTime.Value), "remaining game duration uses the sole clock's quantum");
                c.Equal(Code.None, HaulTripPolicyContracts.CheckDispatch(game.World, Actor(game, external), new MotionTravelEstimate(remaining, Code.None), out _), "movement fitting earliest obligation is admitted");
                c.Equal(RecurringPlanCodes.MovementWindow, HaulTripPolicyContracts.CheckDispatch(game.World, Actor(game, external), new MotionTravelEstimate(remaining + 1, Code.None), out _), "movement crossing earliest obligation is refused");
                ulong before = game.Hash(); Window(game, external); c.Equal(before, game.Hash(), "ready multi-source aggregation is pure");
                ConfigureInspection(c, game, external, true, GameDuration.Minutes(100));
                var sooner = Window(game, external);
                c.That(sooner.Source == Inspection && sooner.DueUpdateStep < window.DueUpdateStep && sooner.Blocker.IsNone,
                    "second native/public service can become the strict earliest deadline independent of profile order");
                c.Equal(RecurringPlanCodes.MovementWindow, HaulTripPolicyContracts.CheckDispatch(game.World, Actor(game, external),
                    new MotionTravelEstimate(sooner.RemainingUpdateSteps.Value + 1, Code.None), out _), "same fresh admission uses the newly limiting second source");
                // The player requests an equal deadline; engine resolves that duration in the same session time.
                var equalInterval = GameDuration.FromSteps(window.DueUpdateStep.Value - InspectionView(game, external).LastCompletedTick.Value, game.World.Time);
                ConfigureInspection(c, game, external, true, equalInterval);
                var equal = Window(game, external);
                c.That(equal.Source == Inspection && equal.DueUpdateStep == window.DueUpdateStep, "equal earned deadlines use stable source ID rather than content enumeration");
                c.That(window.Source == Service && window.RemainingUpdateSteps == remaining, "old allowance remains detached after actual source reconfiguration");
                ConfigureInspection(c, game, external, false, equalInterval);
                c.Equal(Service, Window(game, external).Source, "disabled future source no longer limits fresh movement");
                Configure(c, game, external, false, 200);
                c.That(Window(game, external).Unrestricted && Window(game, external).RemainingGameTime == null, "all disabled future obligations have explicit unbounded service allowance");
                var saved = factory.Load(game.Save());
                c.That(Window(saved, external).Unrestricted && saved.Hash() == game.Hash(), "reconfigured source set saves without a second window codec");
            }
        }
        private static void DisableOtherMachines(JsonValue root)
        {
            for (int i = 3; i < root.Get("entities").Count; i++)
                root.Get("entities")[i].Get("components").Get("work_policy").Set("profile", JsonValue.Of("docks.off"));
            var off = root.Get("work_policies")[0].Clone();
            off.Set("id", JsonValue.Of("docks.off")); off.Set("enabled", JsonValue.Of(false));
            root.Get("work_policies").Add(off);
        }
        private static void MovementWindowContracts(Check c)
        {
            foreach (bool external in new[] { false, true })
            {
                foreach (string sources in new[] { null, "[\"service.interval\",\"service.interval\"]", "[\"missing.service\"]" })
                    c.Throws<ContentException>(() => Start(MovementWindowFactory(external, root =>
                    {
                        root.Set("haul_trip_profiles", Json.Parse("[{\"id\":\"completion.invalid-trip\",\"enabled\":true,\"preserve_outlet_reserve\":false" +
                            (sources == null ? "" : ",\"service_sources\":" + sources) + "}]"));
                        Parts(root, external).Get("haul_trip_policy").Set("profile", JsonValue.Of("completion.invalid-trip"));
                    })), "missing/duplicate/unknown service-window references fail before gameplay");
                c.Throws<ContentException>(() => Start(MovementWindowFactory(external, root => Source(root, external).Set("worker_nodes", JsonValue.NewArray()))),
                    "trip window requires an actual selected-machine service binding");
                var factory = MovementWindowFactory(external); var game = Start(factory); Begin(c, game, external);
                long cycle = Read(game, external).Schedule.LastPlan;
                c.Equal(RecurringPlanCodes.ServicePending, HaulTripPolicyContracts.CheckDispatch(game.World, Actor(game, external), new MotionTravelEstimate(1, Code.None), out _),
                    "unfinished service has no fabricated completion deadline");
                Reach(game, external, () => Read(game, external).Schedule.LastCompletedPlan == cycle, "window begins after actual complete service");
                long remaining = Read(game, external).DueTick.Value - game.Clock.StepIndex; ulong hash = game.Hash();
                c.That(remaining > 1, "actual successful service creates a future game-time window");
                foreach (long duration in new[] { 0L, remaining - 1, remaining })
                    c.Equal(Code.None, HaulTripPolicyContracts.CheckDispatch(game.World, Actor(game, external), new MotionTravelEstimate(duration, Code.None), out _),
                        "known selected movement fits actual remaining cadence including equal boundary");
                c.Equal(RecurringPlanCodes.MovementWindow, HaulTripPolicyContracts.CheckDispatch(game.World, Actor(game, external), new MotionTravelEstimate(remaining + 1, Code.None), out _),
                    "fresh movement longer than actual service window refuses without copying deadlines");
                c.Equal(RecurringPlanCodes.MovementWindow, HaulTripPolicyContracts.CheckDispatch(game.World, Actor(game, external), new MotionTravelEstimate(long.MaxValue, Code.None), out _),
                    "large prospective movement never overflows current time or narrows into fixed point");
                c.Equal(Code.None, HaulTripPolicyContracts.CheckDispatch(game.World, Actor(game, external), new MotionTravelEstimate(remaining + 1, Code.None), out _, emergency: true),
                    "eligible emergency can bypass prospective cadence only");
                c.Equal(MotionArrivalCodes.Unsupported, HaulTripPolicyContracts.CheckDispatch(game.World, Actor(game, external), new MotionTravelEstimate(null, MotionArrivalCodes.Unsupported), out _),
                    "unknown movement cannot become an invented zero window");
                c.Throws<ArgumentOutOfRangeException>(() => RecurringPlanContracts.CheckMovementWindow(game.World, Actor(game, external).Get<IRecurringPlanSource>(Service), -1),
                    "negative prospective game time refuses");
                c.Equal(Codes.NotOwned, game.Seat(1).View.Feature<LogisticsView>().ReadHaulTripPolicy(Actor(game, external).Id, out _),
                    "service-linked trip settings remain private");
                c.Equal(Code.None, game.Seat(0).View.Feature<LogisticsView>().ReadHaulTripServiceSources(Actor(game, external).Id, out var linkedSources),
                    "private immutable trip view exposes actual authored service-source references");
                c.That(linkedSources.Count == 1 && linkedSources[0] == Service, "view names this machine's real service source, not a host-owned schedule");
                c.Equal(Codes.NotOwned, game.Seat(1).View.Feature<LogisticsView>().ReadHaulTripServiceSources(Actor(game, external).Id, out var hiddenSources),
                    "foreign source-reference view shares existing ownership refusal");
                c.That(hiddenSources == null, "foreign view leaks no operational service references");
                c.Equal(hash, game.Hash(), "window and private reads create no claims, plans, effects or extra game time");
                var twin = factory.Load(game.Save());
                c.Equal(RecurringPlanCodes.MovementWindow, HaulTripPolicyContracts.CheckDispatch(twin.World, Actor(twin, external), new MotionTravelEstimate(remaining + 1, Code.None), out _),
                    "saved source and whole-cycle history derive exactly the same window");
                Command(c, game, new ConfigureHaulTripPolicyCommand { Worker = Actor(game, external).Id,
                    Settings = new HaulTripSettings("completion.unlinked-trip", true, false) }, "ordinary trip command explicitly selects profile with no service sources");
                c.Equal(Code.None, HaulTripPolicyContracts.CheckDispatch(game.World, Actor(game, external), new MotionTravelEstimate(long.MaxValue, Code.None), out _),
                    "explicit empty profile imposes no window even while the actual service source remains enabled");
                c.Equal(Code.None, game.Seat(0).View.Feature<LogisticsView>().ReadHaulTripServiceSources(Actor(game, external).Id, out var unlinkedSources),
                    "same private view reads reconfigured actual profile references");
                c.That(unlinkedSources.Count == 0 && linkedSources.Count == 1 && linkedSources[0] == Service,
                    "retained service-reference snapshot stays detached across player reconfiguration");
                Command(c, game, new ConfigureHaulTripPolicyCommand { Worker = Actor(game, external).Id,
                    Settings = new HaulTripSettings("completion.trip", true, false) }, "ordinary trip command restores explicit valid service linkage");
                Configure(c, game, external, false);
                c.Equal(Code.None, HaulTripPolicyContracts.CheckDispatch(game.World, Actor(game, external), new MotionTravelEstimate(long.MaxValue, Code.None), out _),
                    "disabled future service schedule imposes no fictitious future deadline");
            }
        }
        private static void EarnedWindowContracts(Check c)
        {
            foreach (bool external in new[] { false, true })
            {
                c.Throws<ContentException>(() => Start(MovementWindowFactory(external, root => Source(root, external).Set("cadence", JsonValue.Of("submission")))),
                    "trip service references cannot substitute mere submission for performed maintenance");
                var generic = Start(Factory(external, root => Source(root, external).Set("cadence", JsonValue.Of("submission"))));
                var genericView = Read(generic, external);
                c.That(genericView.Cadence == RecurringCadenceBasis.Submission && genericView.ServiceDueTick == null && genericView.ServiceDueGameTime == null,
                    "explicit generic submission recurrence remains valid without inventing completed-service timing");
                c.Throws<ArgumentException>(() => RecurringPlanContracts.CheckMovementWindow(generic.World, Actor(generic, external).Get<IRecurringPlanSource>(Service), 1),
                    "public movement-window contract cannot treat generic submission as performed service");
                var factory = MovementWindowFactory(external); var game = Start(factory); Begin(c, game, external);
                long attempt = Read(game, external).Schedule.LastPlan;
                Command(c, game, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { attempt }) }, "normal player cancels first unfinished physical service");
                var cancelled = Read(game, external);
                c.That(cancelled.Schedule.LastCompletedPlan == 0 && cancelled.DueTick > game.Clock.StepIndex,
                    "actual cancelled service waits for authored retry without having completed maintenance");
                c.Equal(RecurringPlanCodes.MovementWindow, HaulTripPolicyContracts.CheckDispatch(game.World, Actor(game, external), new MotionTravelEstimate(1, Code.None), out _),
                    "cancelled unserviced machine cannot borrow its future retry deadline as maintenance credit");
                c.That(cancelled.ServiceDueTick == 0 && cancelled.ServiceDueGameTime.HasValue && cancelled.ServiceDueGameTime.Value.IsZero,
                    "private service deadline remains unearned initial game time while retry timing moves forward");
                var expired = Window(game, external);
                c.That(!expired.Unrestricted && expired.Source == Service && expired.Blocker == RecurringPlanCodes.MovementWindow &&
                    expired.DueGameTime.HasValue && expired.DueGameTime.Value.IsZero && expired.RemainingGameTime == null && expired.RemainingUpdateSteps == null,
                    "private expired diagnostic does not substitute retry time or a usable zero allowance");
                c.Equal(RecurringPlanCodes.MovementWindow, expired.CheckMovement(0), "expired service refuses even zero movement through the same shared rule");
                ulong hash = game.Hash();
                c.Equal(Codes.NotFound, game.Seat(1).View.Feature<WorkView>().ReadRecurringPlan(Actor(game, external).Id, Service, out _),
                    "foreign company cannot read cancelled service chronology");
                c.Equal(hash, game.Hash(), "earned-service/private retry reads do not alter native history, stock or time");
                var twin = factory.Load(game.Save());
                c.That(Read(twin, external).ServiceDueTick == 0 && Read(twin, external).DueTick == cancelled.DueTick && game.Hash() == twin.Hash(),
                    "save restores the same actual cancelled attempt and no fabricated completed-service deadline");
                c.Equal(RecurringPlanCodes.MovementWindow, HaulTripPolicyContracts.CheckDispatch(twin.World, Actor(twin, external), new MotionTravelEstimate(1, Code.None), out _),
                    "saved retry delay still cannot fund fresh movement");
            }
        }
        private static void EarnedWindowPhysical(Check c)
        {
            foreach (bool external in new[] { false, true })
            {
                var factory = MovementWindowFactory(external); var game = Start(factory); Begin(c, game, external);
                long initial = Read(game, external).Schedule.LastPlan;
                Command(c, game, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { initial }) }, "player cancels a real initial service attempt");
                var retained = Read(game, external); var twin = factory.Load(game.Save());
                Reach(game, external, () => Read(game, external).Schedule.LastCompletedPlan > initial, "actual retry earns first full service", twin);
                var first = Read(game, external); long success = first.Schedule.LastCompletedPlan;
                c.That(first.ServiceDueTick == Plan(game, success).EndedTick + game.World.Time.ExactTicks(first.Schedule.Interval) && Condition(game, external) == Fix.FromInt(10),
                    "only real spares, machine time, repair and whole workflow earn a new service window");
                c.That(retained.ServiceDueTick == 0 && retained.Schedule.LastCompletedPlan == 0 && retained.DueTick < first.ServiceDueTick,
                    "retained cancelled view never mutates into later successful service state");
                c.Equal(Code.None, HaulTripPolicyContracts.CheckDispatch(game.World, Actor(game, external), new MotionTravelEstimate(1, Code.None), out _),
                    "actual full retry completion restores normal movement readiness");
                c.Equal(game.Hash(), twin.Hash(), "saved cancelled retry progresses through real service with identical native state");
                Reach(game, external, () => Read(game, external).Schedule.LastPlan > success, "next actual due service after prior success", twin);
                long secondAttempt = Read(game, external).Schedule.LastPlan;
                foreach (var current in new[] { game, twin })
                    Command(c, current, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { secondAttempt }) }, "player cancels later attempt without rewriting prior success");
                var cancelled = Read(game, external);
                c.That(cancelled.ServiceDueTick == first.ServiceDueTick && cancelled.LastCompletedTick == first.LastCompletedTick &&
                    cancelled.ServiceDueTick < game.Clock.StepIndex && cancelled.DueTick > game.Clock.StepIndex,
                    "overdue earned service stays overdue despite delayed future retry after cancellation");
                c.Equal(RecurringPlanCodes.MovementWindow, HaulTripPolicyContracts.CheckDispatch(game.World, Actor(game, external), new MotionTravelEstimate(1, Code.None), out _),
                    "cancelled later service cannot refresh a previously earned but exhausted window");
                var restored = factory.Load(game.Save());
                c.Equal(RecurringPlanCodes.MovementWindow, HaulTripPolicyContracts.CheckDispatch(restored.World, Actor(restored, external), new MotionTravelEstimate(1, Code.None), out _),
                    "checkpoint after overdue cancellation preserves fresh movement refusal");
                Reach(game, external, () => Read(game, external).Schedule.LastCompletedPlan > secondAttempt, "later real retry restores exhausted service readiness", twin);
                c.Equal(Code.None, HaulTripPolicyContracts.CheckDispatch(game.World, Actor(game, external), new MotionTravelEstimate(1, Code.None), out _),
                    "new whole service, not cancellation or retry spacing, earns renewed readiness");
                c.Equal(game.Hash(), twin.Hash(), "past success, cancelled attempt and later physical retry continue identically");

                var failedFactory = MovementWindowFactory(external, failedService: true); var failed = Start(failedFactory); Begin(c, failed, external);
                long failure = Read(failed, external).Schedule.LastPlan;
                Reach(failed, external, () => Plan(failed, failure).State == PlanState.Failed, "authored engine-side completion refuses whole service");
                var refusal = Read(failed, external);
                c.That(Condition(failed, external) == Fix.FromInt(10) && refusal.Schedule.LastCompletedPlan == 0 && refusal.ServiceDueTick == 0 &&
                    refusal.DueTick > failed.Clock.StepIndex && Plan(failed, failure).Blocker == Beep.SimEngine.ExtensionProof.CompletionEffectDef.Rejected,
                    "actual repair plus typed whole-workflow failure cannot fabricate successful service or maintenance credit");
                c.Equal(RecurringPlanCodes.MovementWindow, HaulTripPolicyContracts.CheckDispatch(failed.World, Actor(failed, external), new MotionTravelEstimate(1, Code.None), out _),
                    "real failed workflow cannot borrow retry time as earned service readiness");
                var failedTwin = failedFactory.Load(failed.Save());
                c.Equal(failed.Hash(), failedTwin.Hash(), "actual failed order, spent resources and retry cadence have one durable native state");
                Reach(failed, external, () => Read(failed, external).Schedule.LastPlan > failure, "normal authored failed-service retry", failedTwin);
                long retry = Read(failed, external).Schedule.LastPlan;
                Reach(failed, external, () => Plan(failed, retry).State == PlanState.Failed, "repeated physical failure never manufactures success", failedTwin);
                c.That(Read(failed, external).Schedule.LastCompletedPlan == 0 && Read(failed, external).ServiceDueTick == 0 && failed.Hash() == failedTwin.Hash(),
                    "repeated real retries preserve failed service chronology and deterministic saved continuation");
            }
        }
        private static void MovementWindowPhysical(Check c)
        {
            var factory = MovementWindowFactory(edit: DisableOtherMachines); var game = Start(factory); Begin(c, game, false);
            long first = Read(game, false).Schedule.LastPlan;
            Reach(game, false, () => Read(game, false).Schedule.LastCompletedPlan == first, "real service before fresh window selection");
            Configure(c, game, false, true, 12);
            c.That(Actor(game, false).Get<IWorker>().Job == 0 && Read(game, false).Schedule.LastPlan == first,
                "prospective window tests idle machine before any next service exists, not an ordinary busy-worker refusal");
            var hauling = game.World.Services.Get<IHaulExecution>(); var resource = game.World.Content.ResourceId("supply");
            ulong hash = game.Hash();
            var selected = hauling.PreviewHaul(new PlayerId(0), Find(game, "source").Id, Find(game, "destination").Id, resource, Fix.FromInt(4));
            c.That(selected.Status == HaulStatus.PolicyBlocked && selected.Blocker == RecurringPlanCodes.MovementWindow,
                "fresh private haul selector refuses the actual idle machine's movement window, not a fabricated service plan");
            c.Equal(0, hauling.AcquireHaul(new PlayerId(0), Find(game, "source").Id, Find(game, "destination").Id, resource, Fix.FromInt(4), 100),
                "actual acquisition repeats fresh window refusal before cargo or destination claims");
            c.Equal(hash, game.Hash(), "refused preview/acquisition allocates no physical promises or extra game time");
            Command(c, game, new ConfigureWorkPolicyCommand { Worker = Find(game, "machine.c").Id,
                Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("haul"), new WorkerRoleId("inspect") }, null, true, true, WorkAssignmentMode.All) },
                "ordinary player enables another real carrier rather than bypassing service policy");
            selected = hauling.PreviewHaul(new PlayerId(0), Find(game, "source").Id, Find(game, "destination").Id, resource, Fix.FromInt(4));
            c.That(selected.Status == HaulStatus.Ready && selected.Carrier == Find(game, "machine.c").Id,
                "same fresh selector chooses actual permitted alternative carrier under the unchanged service window");

            var loadedFactory = MovementWindowFactory(edit: root =>
            {
                root.Get("entities")[1].Get("components").Get("point_stock").Set("initial", JsonValue.Number("0"));
                DisableOtherMachines(root);
            });
            var loaded = Start(loadedFactory); Begin(c, loaded, false); long initialService = Read(loaded, false).Schedule.LastPlan;
            Reach(loaded, false, () => Read(loaded, false).Schedule.LastCompletedPlan == initialService, "loaded case begins after earned service");
            var loadPreview = loaded.World.Services.Get<IHaulExecution>().PreviewHaul(new PlayerId(0), Find(loaded, "source").Id,
                Find(loaded, "destination").Id, resource, Fix.FromInt(4));
            c.That(loadPreview.Status == HaulStatus.Ready && loadPreview.Carrier == Actor(loaded, false).Id,
                "actual selected complete movement initially fits this machine's earned service window");
            Command(c, loaded, new ConfigureStockPolicyCommand { Store = Find(loaded, "destination").Id, Resource = resource,
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(4), Fix.FromInt(4), Fix.FromInt(4), 100, LogisticsReserveAccess.Protected)) },
                "normal stock policy starts the service-window-admitted real trip");
            Reach(loaded, false, () => Actor(loaded, false).Get<IWorker>().Job != 0 && Board(loaded).Job(Actor(loaded, false).Get<IWorker>().Job).CargoOutReservation.IsValid,
                "window-admitted selected machine earns actual cargo");
            int delivery = Actor(loaded, false).Get<IWorker>().Job;
            Configure(c, loaded, false, true, 1); long dueService = Read(loaded, false).Schedule.LastPlan;
            c.That(dueService > initialService && Board(loaded).Job(delivery).IsLive,
                "shortened real schedule admits service without cancelling incumbent cargo");
            var twin = loadedFactory.Load(loaded.Save());
            Reach(loaded, false, () => Find(loaded, "destination").Get<IInventory>().Amount(resource) == Fix.FromInt(4), "retained cargo crosses now-due service window", twin);
            Reach(loaded, false, () => Read(loaded, false).Schedule.LastCompletedPlan == dueService, "due service follows retained physical delivery", twin);
            c.Equal(loaded.Hash(), twin.Hash(), "real loaded delivery and later service continue from one saved clock/source/board state");
        }
        private static void DispatchContracts(Check c)
        {
            foreach (bool external in new[] { false, true })
            {
                c.Throws<ContentException>(() => Start(Factory(external, root => Source(root, external).Remove("dispatch_mode"))), "dispatch mode is explicit, never a default service reservation");
                c.Throws<ContentException>(() => Start(Factory(external, root => Source(root, external).Set("dispatch_mode", JsonValue.Of("automatic")))), "unknown dispatch mode refuses before gameplay");
                c.Throws<ContentException>(() => Start(Factory(external, root => {
                    Source(root, external).Set("dispatch_mode", JsonValue.Of("service_first")); Source(root, external).Set("worker_nodes", JsonValue.NewArray());
                })), "service-first requires actual worker-bound nodes, not an arbitrary recurring plan");
                var game = Start(ReadinessFactory(external, "maintenance", initial: 4, spares: 2, serviceFirst: true));
                c.Equal(Code.None, Idle(game, external), "unadmitted future service does not fabricate a worker reservation");
                Begin(c, game, external); var source = Read(game, external); long plan = source.Schedule.LastPlan;
                c.That(source.WorkerDispatch == RecurringWorkerDispatchMode.ServiceFirst && Actor(game, external).Get<IWorker>().Job == 0 && plan > 0,
                    "actual admitted service with missing spares owns the authored reservation, not a worker flag");
                ulong hash = game.Hash();
                c.Equal(RecurringPlanCodes.ServicePending, Idle(game, external), "fresh ordinary idle admission respects service-first intent");
                c.Equal(Code.None, Idle(game, external, true), "eligible emergency work remains available through existing policy");
                c.Equal(RecurringPlanCodes.ServicePending, game.World.Services.Get<IWorkerDispatch>().CheckAssignment(game.World, Actor(game, external),
                    new PlayerId(0), new WorkerRoleId("inspect"), new[] { Find(game, "source") }, false), "full assignment shares idle refusal semantics");
                var candidates = new List<WorkWorkerCandidate>();
                c.Equal(Code.None, game.Seat(0).View.Feature<WorkView>().PreviewWorkerCandidates("completion.service", "service", Find(game, "source").Id, candidates),
                    "manual activity preview uses existing private candidate path");
                bool selectedCandidate = false;
                foreach (var candidate in candidates) if (candidate.Worker == Actor(game, external).Id)
                {
                    selectedCandidate = true;
                    c.Equal(RecurringPlanCodes.ServicePending, candidate.Result.Blocker, "matching manual service activity cannot impersonate native recurring provenance");
                }
                c.That(selectedCandidate, "private preview includes the actual service-bound machine, not an empty candidate proof");
                c.Equal(hash, game.Hash(), "idle/full/manual candidate reads do not reserve stock, create jobs or advance time");
                Configure(c, game, external, false);
                c.Equal(RecurringPlanCodes.ServicePending, Idle(game, external), "disabling future recurrence does not abandon admitted service");
                Command(c, game, new ConfigurePlanCommand { Plan = plan, Group = "player.service", Priority = 80, Paused = true }, "pause actual service plan");
                c.Equal(Code.None, Idle(game, external), "deliberately paused plan releases fresh ordinary machine eligibility");
                Command(c, game, new ConfigurePlanCommand { Plan = plan, Group = "player.service", Priority = 80, Paused = false }, "resume actual service plan");
                c.Equal(RecurringPlanCodes.ServicePending, Idle(game, external), "resume derives reservation from same actual live plan");
                Command(c, game, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { plan }) }, "cancel actual service intent");
                c.Equal(Code.None, Idle(game, external), "cancelled service does not leave a stale reservation flag");
                var competing = Start(ReadinessFactory(external, "maintenance", initial: 4, spares: 2)); Begin(c, competing, external);
                c.That(Read(competing, external).WorkerDispatch == RecurringWorkerDispatchMode.Compete && Idle(competing, external).IsNone,
                    "explicit compete mode preserves ordinary scheduling under the same shortage");
            }
        }
        private static void DispatchPhysical(Check c)
        {
            foreach (bool external in new[] { false, true })
            {
                var factory = ReadinessFactory(external, "maintenance", initial: 4, spares: 2, serviceFirst: true);
                var game = Start(factory); Begin(c, game, external); long plan = Read(game, external).Schedule.LastPlan;
                c.Equal(RecurringPlanCodes.ServicePending, Idle(game, external), "actual service shortage prevents unrelated fresh machine work");
                var twin = factory.Load(game.Save());
                c.Equal(RecurringPlanCodes.ServicePending, Idle(twin, external), "checkpoint derives service-first hold from actual source/plan relation");
                var resource = game.World.Content.ResourceId("supply");
                foreach (var current in new[] { game, twin })
                    Command(c, current, new ConfigureStockPolicyCommand { Store = Find(current, "source").Id, Resource = resource,
                        Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(5), Fix.FromInt(10), Fix.FromInt(12), 200, LogisticsReserveAccess.Protected)) },
                        "normal stock command dispatches another real carrier for service spares");
                Reach(game, external, () => Read(game, external).Schedule.LastCompletedPlan == plan, "service-first physical refill and whole service", twin);
                c.That(Condition(game, external) == Fix.FromInt(10) && Find(game, "destination").Get<IInventory>().Amount(resource) < Fix.FromInt(20),
                    "authentic own service remains assignable and consumes actually delivered spares");
                c.Equal(game.Hash(), twin.Hash(), "saved service-first shortage/refill/service completes without a second reservation ledger");
                c.Equal(Code.None, Idle(game, external), "whole service completion releases ordinary machine admission");
            }
            var loadedFactory = ReadinessFactory(false, "maintenance", initial: 4, serviceFirst: true, destinationStock: 0);
            var loaded = Start(loadedFactory); var supply = loaded.World.Content.ResourceId("supply");
            Command(c, loaded, new ConfigureStockPolicyCommand { Store = Find(loaded, "destination").Id, Resource = supply,
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(4), Fix.FromInt(4), Fix.FromInt(4), 100, LogisticsReserveAccess.Protected)) },
                "normal player stock request creates ordinary physical hauling");
            Reach(loaded, false, () => Actor(loaded, false).Get<IWorker>().Job != 0 && Board(loaded).Job(Actor(loaded, false).Get<IWorker>().Job).CargoOutReservation.IsValid,
                "ordinary machine acquires real cargo before service is requested");
            var job = Board(loaded).Job(Actor(loaded, false).Get<IWorker>().Job); int originalJob = job.Id;
            var cargo = job.CargoOutReservation; var destinationClaim = job.InReservation;
            Begin(c, loaded, false); long service = Read(loaded, false).Schedule.LastPlan;
            c.That(service > 0 && job.IsLive && job.CargoOutReservation.Equals(cargo) && job.InReservation.Equals(destinationClaim),
                "admitted service cannot abandon or duplicate an incumbent loaded shipment");
            c.Equal(Code.None, loaded.World.Services.Get<IWorkerDispatch>().CheckJob(loaded.World, Actor(loaded, false), job),
                "actual incumbent continuation remains eligible while its own service waits");
            var loadedTwin = loadedFactory.Load(loaded.Save());
            Reach(loaded, false, () => Read(loaded, false).Schedule.LastCompletedPlan == service, "loaded delivery then whole physical service", loadedTwin);
            c.That(Board(loaded).Job(originalJob) == null || job.State == JobState.Done, "original shipment finishes through existing job coordinator");
            c.Equal(Fix.FromInt(4), Find(loaded, "destination").Get<IInventory>().Amount(supply), "service-first continuation delivers original cargo exactly once");
            c.Equal(loaded.Hash(), loadedTwin.Hash(), "loaded cargo claims and later service retain deterministic saved continuation");
        }
    }
}
