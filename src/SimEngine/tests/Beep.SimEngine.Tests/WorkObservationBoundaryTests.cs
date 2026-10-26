using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Actual master/JSON authoring and ordinary public player inputs only.
    internal static class WorkObservationBoundaryTests
    {
        internal const string Case = "Work observations: physical execution and terminal candidate plan query boundaries";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static readonly WorkerRoleId Inspect = new WorkerRoleId("inspect");
        private static GameFactory Factory(bool half, WorkObservationProbeModule probe, Action<JsonValue> edit = null)
        {
            var json = Json.Parse(PointGame.Definitions);
            var scenario = json.Get("scenarios")[0]; scenario.Set("seats", JsonValue.Number("2"));
            scenario.Get("modules").Add(JsonValue.Of(probe.Id));
            if (half) json.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
            var late = json.Get("entities")[2].Clone(); late.Set("id", JsonValue.Of("late.worker"));
            var parts = late.Get("components"); parts.Remove("worker"); parts.Remove("transporter"); parts.Remove("storage");
            parts.Set("contract_worker", Json.Parse("{\"role\":\"inspect\",\"rate\":1,\"states\":{\"working\":\"processing\",\"stopped\":\"blocked\"}}"));
            parts.Get("point_presence").Set("position", JsonValue.Number("40")); json.Get("entities").Add(late);
            json.Get("entity_setups").Add(Json.Parse("{\"id\":\"late.worker\",\"after\":[],\"entity\":\"late.worker\",\"company\":0,\"placement\":{\"space\":\"field.points\",\"position\":40},\"home\":null}"));
            json.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("late.worker"));
            edit?.Invoke(json);
            return PointGame.Factory(json.ToString(), extraModules: new GameModule[] { probe },
                extraDeclarations: "[{\"id\":\"proof.work-observation\",\"version\":\"1.0.0\",\"documents\":[]}]");
        }
        private static SimGame New(GameFactory f) => f.NewGame(new NewGameRequest {
            Scenario = "points", Seed = 945, Seats = { Controller.Human, Controller.Human } });
        private static EntityId Entity(SimGame g, string name) => g.Seat(0).Snapshot.Entities.Single(x => x.Def == name).Id;
        private static WorkView Work(SimGame g, int seat = 0) => g.Seat(seat).View.Feature<WorkView>();
        private static PlanView Plan(SimGame g, long id)
        { var rows = new List<PlanView>(); Work(g).CopyPlans(rows); return rows.Single(x => x.Id == id); }
        private static long Start(Check c, SimGame g, bool paused)
            => PermanentRemovalTests.Submit(c, g, new SubmitPlanCommand { Intent = new PlanIntent("activity:inspect",
                Json.Parse("{\"target\":" + Entity(g, "destination").Value + "}"), "observe", 50, paused) }).Arg0;
        private static void Reach(SimGame g, SimGame twin, Func<bool> done, string stage)
        {
            for (int i = 0; i < 160 && !done(); i++) PermanentRemovalTests.Step(g, twin);
            if (!done()) throw new InvalidOperationException(stage + " exceeded160Updates clock=" + g.Clock.StepIndex + " fault=" + g.Fault);
        }
        private static void EmptyRefusal<T>(Check c, Action<IList<T>> read, string name)
        {
            var rows = new List<T> { default };
            c.Throws<InvalidOperationException>(() => read(rows), name + " terminal refusal");
            c.Equal(0, rows.Count, name + " no stale or partial output");
        }
        private static void Refuses(Check c, WorkView view, EntityId worker, int order, long plan)
        {
            foreach (EntityId id in new[] { EntityId.None, worker, new EntityId(int.MaxValue) })
            {
                WorkerHomeView home = null; WorkerExecutionView execution = null; OperatingInputEvaluation inputs = null;
                WorkerPolicyView policy = default; RecurringPlanView recurring = default; RecurringTemplateSettings template = null;
                ActivityFacilitySelectionView facilities = null;
                c.Throws<InvalidOperationException>(() => view.ReadHome(id, out home), "home missing/own/private refuses");
                c.Throws<InvalidOperationException>(() => view.ReadWorkerExecution(id, out execution), "execution shortcut refuses");
                c.Throws<InvalidOperationException>(() => view.ReadOperatingInputs(id, OperatingPhase.Idle, out inputs), "inputs shortcut refuses");
                c.Throws<InvalidOperationException>(() => view.TryReadPolicy(id, out policy), "policy shortcut refuses");
                c.Throws<InvalidOperationException>(() => view.ReadRecurringPlan(id, Code.None, out recurring), "recurrence shortcut refuses");
                c.Throws<InvalidOperationException>(() => view.ReadRecurringFacilities(id, Code.None, out facilities), "facilities shortcut refuses");
                c.Throws<InvalidOperationException>(() => view.ReadRecurringTemplate(id, Code.None, out template), "template shortcut refuses");
                c.That(home == null && execution == null && inputs == null && template == null && facilities == null,
                    "reference outputs cleared before first-fault refusal");
                c.Throws<InvalidOperationException>(() => view.PreviewHome(id, EntityId.None), "home preview refuses");
                c.Throws<InvalidOperationException>(() => view.PreviewWorkerHeld(id, true), "hold preview refuses");
                c.Throws<InvalidOperationException>(() => view.PreviewPolicy(id, null), "policy preview refuses");
                c.Throws<InvalidOperationException>(() => view.PreviewRecurringPlan(id, Code.None, null), "recurring preview refuses");
                c.Throws<InvalidOperationException>(() => view.PreviewRecurringTemplate(id, Code.None, null), "recurring template preview refuses");
            }
            foreach (PlayerId company in new[] { new PlayerId(0), new PlayerId(1), new PlayerId(99) })
            {
                WorkerCapacitySnapshot capacity = default; WorkLoadSnapshot load = default;
                c.Throws<InvalidOperationException>(() => view.ReadWorkerCapacity(company, Inspect, worker, out capacity), "private capacity refuses");
                c.Throws<InvalidOperationException>(() => view.ReadWorkLoad(company, WorkLoadScope.Company, EntityId.None, out load), "private load refuses");
            }
            c.Throws<InvalidOperationException>(() => view.IdleWorkers(new WorkerRoleId("missing")), "empty idle role refuses");
            foreach (int id in new[] { 0, order, int.MaxValue })
            {
                c.Throws<InvalidOperationException>(() => view.PreviewOrderEffect(id), "missing/private/actual final effect refuses");
                EmptyRefusal<WorkWorkerCandidate>(c, rows => view.TryCopyWorkerCandidates(id, "check", rows), "active candidate read");
            }
            EmptyRefusal<WorkWorkerCandidate>(c, rows => view.PreviewWorkerCandidates("inspect", "check", worker, rows), "prospective candidates");
            foreach (long id in new[] { 0L, plan, long.MaxValue })
            {
                c.Throws<InvalidOperationException>(() => view.PreviewPlanEdit(id, null, 0, false), "plan edit refuses");
                c.Throws<InvalidOperationException>(() => view.PreviewPlanMove(id, 0), "plan reorder refuses");
                c.Throws<InvalidOperationException>(() => view.PreviewPlanRepeat(id), "plan repeat refuses");
            }
            c.Throws<InvalidOperationException>(() => view.PreviewPlanCancel(null), "null cancel refuses before shortcut");
            c.Throws<InvalidOperationException>(() => view.PreviewPlanDependencies(null), "null dependencies refuse before shortcut");
            c.Throws<InvalidOperationException>(() => view.PreviewPlanGroup(null), "null group refuses before shortcut");
            foreach (string group in new[] { null, "missing", "observe" })
            {
                c.Throws<InvalidOperationException>(() => view.PreviewPlanGroupEdit(group, 0, false), "empty/private group edit refuses");
                c.Throws<InvalidOperationException>(() => view.PreviewPlanGroupCancel(group), "empty/private group cancel refuses");
                c.Throws<InvalidOperationException>(() => view.PreviewPlanGroupMove(group, 0), "empty/private group move refuses");
                c.Throws<InvalidOperationException>(() => view.PreviewPlanGroupRepeat(group, "again"), "empty/private group repeat refuses");
            }
        }
        private static void Physical(Check c, bool half)
        {
            var probe = new WorkObservationProbeModule(); var f = Factory(half, probe); var g = New(f); var idle = New(f); ulong isolation = idle.Hash();
            var work = Work(g); var other = Work(g, 1); var observer = g.Observer.Feature<WorkView>();
            c.That(!g.Modules.Contains(TycoonModule.ModuleId), "work game has no business dependency");
            c.Equal(2, work.IdleWorkers(Inspect), "actual independent/native role facts"); c.Equal(0, other.IdleWorkers(Inspect), "other company sees no machines");
            var limits = new ReplayLimits(2 * 1024 * 1024, 2200); using var recorder = g.RecordReplay(limits);
            long plan = Start(c, g, true); c.Equal(PlanState.Pending, Plan(g, plan).State, "paused intent creates no instant work");
            var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "paused intent saves exactly");
            g.Clock.Pause(); twin.Clock.Pause(); ulong paused = g.Hash();
            c.Equal(0, g.Update(50000), "single paused game clock stops actual work"); c.Equal(0, twin.Update(50000), "saved clock also pauses");
            c.That(g.Hash() == paused && g.Hash() == twin.Hash(), "paused work state stays authoritative"); g.Clock.Resume(); twin.Clock.Resume();
            PermanentRemovalTests.Submit(c, g, new ConfigurePlanCommand { Plan = plan, Group = "observe", Priority = 50, Paused = false });
            twin = f.Load(g.Save()); Reach(g, twin, () => Plan(g, plan).Order > 0, "plan activation"); int order = Plan(g, plan).Order;
            var candidates = new List<WorkWorkerCandidate>(); ulong pure = g.Hash();
            c.That(work.TryCopyWorkerCandidates(order, "check", candidates) && candidates.Count > 0, "actual selector provides detached live candidates");
            c.Equal(pure, g.Hash(), "candidate observation mutates no work or claims"); var retained = candidates.ToArray();
            c.That(!other.TryCopyWorkerCandidates(order, "check", candidates) && candidates.Count == 0, "private candidates publish nothing");
            var nodes = new List<ActivityNodeView>();
            Reach(g, twin, () => work.TryCopyNodes(order, nodes) && nodes.Count == 1 && nodes[0].Progress.WholeTicks > 0,
                "actual physical arrival and productive prefix");
            c.That(nodes[0].Progress.WholeTicks > 0 && nodes[0].Progress.WholeTicks < (half ? 12 : 6), "partial work is earned, not instantly completed");
            twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "partial productive work saves exactly");
            Reach(g, twin, () => Plan(g, plan).State == PlanState.Done, "physical travel and six minutes of earned inspection");
            c.That(Plan(g, plan).State == PlanState.Done && retained.Length > 0, "earned terminal plan and retained detached candidates survive native order pruning");
            c.That(g.Seat(0).View.TryFindResource("supply", out var resource), "actual file-authored resource");
            PermanentRemovalTests.Submit(c, g, new HaulResourcesCommand { Source = Entity(g, "source"), Destination = Entity(g, "destination"),
                Worker = Entity(g, "machine"), Resource = resource, Ceiling = Fix.FromInt(4), Priority = 100 });
            twin = f.Load(g.Save()); Reach(g, twin, () => PermanentRemovalTests.Facts(g, Entity(g, "destination")).Stock == Fix.FromInt(4), "physical pickup/travel/delivery");
            c.Equal(Fix.FromInt(20), PermanentRemovalTests.Total(g), "physical work and stock conservation use original owners");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "terminal execution checkpoint");
            c.That(new ReplayVerifier().Verify(f, recorder.Finish(), limits).Success, "public policy/plan/work/haul replay");
            c.Equal(isolation, idle.Hash(), "interleaved independent session stays untouched");
            var worker = Entity(g, "machine"); var retainedSnapshot = g.Seat(0).Snapshot;
            probe.FailFactory = true;
            c.Throws<InvalidOperationException>(() => g.Seat(0).View.Feature<ProbeView>(), "actual module factory produces terminal first fault");
            var fault = g.Fault; Refuses(c, work, worker, order, plan); Refuses(c, other, worker, order, plan); Refuses(c, observer, worker, order, plan);
            c.That(ReferenceEquals(fault, g.Fault) && probe.FactoryCalls == 1 && probe.WorkerCalls == 0, "all shortcuts preserve first fault without callback retries");
            c.Equal(0, g.Update(), "terminal work loop cannot advance"); c.Throws<InvalidOperationException>(() => g.Save(), "terminal work cannot save");
            c.That(retainedSnapshot.Entities.Count == 4 && retainedSnapshot.State != GameState.Faulted, "retained game facts remain detached");
            c.Equal(isolation, idle.Hash(), "terminal query failure remains session-local");
        }
        private static void Late(Check c, string kind)
        {
            var probe = new WorkObservationProbeModule(); var f = Factory(false, probe); var g = New(f); var work = Work(g);
            long plan = Start(c, g, false); Reach(g, null, () => Plan(g, plan).Order > 0, "negative probe's ordinary active plan");
            int order = Plan(g, plan).Order; var candidates = new List<WorkWorkerCandidate>();
            c.Equal(Code.None, work.PreviewWorkerCandidates("inspect", "check", Entity(g, "destination"), candidates), "healthy full native/independent candidate observation");
            c.Equal(2, candidates.Count, "later independent candidate follows native worker");
            probe.ArmWorker = true; g.Seat(0).View.Feature<ProbeView>();
            if (kind == "preview") EmptyRefusal<WorkWorkerCandidate>(c, rows => work.PreviewWorkerCandidates("inspect", "check", Entity(g, "destination"), rows), "late preview worker fault");
            else if (kind == "active") EmptyRefusal<WorkWorkerCandidate>(c, rows => work.TryCopyWorkerCandidates(order, "check", rows), "late active worker fault");
            else c.Throws<InvalidOperationException>(() => work.IdleWorkers(Inspect), "late idle worker failure cannot publish count");
            c.That(g.Fault != null && probe.WorkerCalls == 1, "actual worker callback failure reaches original fault owner");
            var fault = g.Fault; c.Throws<InvalidOperationException>(() => work.IdleWorkers(new WorkerRoleId("missing")), "subsequent empty role refuses");
            c.That(ReferenceEquals(fault, g.Fault) && probe.WorkerCalls == 1, "subsequent query does not retry actual bad worker");
            c.Equal(0, g.Update(), "late candidate fault stops update"); c.Throws<InvalidOperationException>(() => g.Save(), "late candidate fault cannot save");
        }
        private static void RequirementVisibility(Check c, bool hidden)
        {
            // Explicit visibility-provider instrumentation, not player/host gameplay.
            var probe = new WorkObservationProbeModule { ConfigureVisibility = visibility => {
                if (hidden) visibility.EntityAllowed = (world, viewer, entity) => entity.Owner == viewer && entity.Def.Id != "destination";
                else visibility.OnEntity = (world, viewer, entity) => {
                    if (world.Clock.Paused && entity.Def.Id == "destination") throw new InvalidOperationException("proof.requirement-visibility.original");
                };
            } };
            var factory = Factory(false, probe, json => {
                json.Set("target_resource_requirements", Json.Parse("[{\"id\":\"visibility.stock\",\"port\":null,\"use\":\"consume\",\"quantities\":{\"supply\":1}}]"));
                json.Get("activities")[0].Get("nodes")[0].Set("requirement", JsonValue.Of("visibility.stock"));
            });
            var game = New(factory); var idle = New(factory); ulong isolated = idle.Hash();
            EntityId target = game.Snapshot.Entities.Single(x => x.Def == "destination").Id;
            var rows = new List<WorkRequirementView> { default }; ulong before = game.Hash();
            if (hidden)
            {
                c.That(!game.Seat(0).View.TryGetEntity(target, out _), "selected visibility actually hides owned target");
                c.Equal(Codes.NotFound, Work(game).PreviewOrderRequirements("inspect", target, rows), "hidden owned target has no proposed requirement diagnostics");
                c.Equal(0, rows.Count, "hidden target leaks no stock requirement prefix");
                c.Equal(Code.None, game.Observer.Feature<WorkView>().PreviewOrderRequirements("inspect", target, rows), "explicit observer may diagnose actual target");
                c.That(rows.Any(x => x.Requirement == "visibility.stock" && x.Result.Blocker == WorkResourceCodes.Stock), "actual stock shortage criteria exist behind private target");
                c.Equal(before, game.Hash(), "hidden/observer diagnostic reads neither reserve nor advance work");
                var restored = factory.Load(game.Save());
                c.Equal(game.Hash(), restored.Hash(), "hidden-target provider and authored stock restore through original owners");
                c.Equal(Codes.NotFound, Work(restored).PreviewOrderRequirements("inspect", target, rows), "saved target remains private to proposed diagnostics");
                c.Equal(0, rows.Count, "saved private diagnostic has no result prefix");
            }
            else
            {
                c.Equal(Code.None, Work(game).PreviewOrderRequirements("inspect", target, rows), "healthy visible target retains original diagnostics");
                c.That(rows.Any(x => x.Requirement == "visibility.stock" && x.Result.Blocker == WorkResourceCodes.Stock), "healthy criteria use actual authored stock-shortage provider");
                game.Clock.Pause(); var retained = game.Seat(0).Snapshot; rows.Add(default);
                c.Throws<InvalidOperationException>(() => Work(game).PreviewOrderRequirements("inspect", target, rows), "target visibility callback participates in guarded proposed diagnostics");
                c.Equal(0, rows.Count, "visibility fault publishes no diagnostic prefix");
                var fault = game.Fault; c.That(fault != null && fault.Message.Contains("proof.requirement-visibility.original"), "original visibility exception is authoritative");
                if (fault == null) return; // Failed assertion above is the reproduction, not a healthy fallback.
                game.Update();
                c.That(game.Seat(0).Snapshot.Tick == retained.Tick && game.Seat(0).Snapshot.State == GameState.Faulted
                    && ReferenceEquals(retained.Space, game.Seat(0).Snapshot.Space), "failed proposed read retains own healthy spatial epoch");
                c.Throws<InvalidOperationException>(() => game.Save(), "faulted diagnostic cannot save healthy");
                c.That(ReferenceEquals(fault, game.Fault), "diagnostic fault is never replaced");
            }
            c.Equal(isolated, idle.Hash(), "diagnostic visibility does not affect another session");
        }
        private static GameFactory IntentFactory(string mode, string stage)
            => Factory(false, new WorkObservationProbeModule(), json => {
                var activity = json.Get("activities"); json.Remove("activities");
                activity[0].Set("intent_fault", JsonValue.Of(mode)); activity[0].Set("intent_stage", JsonValue.Of(stage));
                json.Set("intent_probe_activities", activity);
            });
        private static void IntentObservation(Check c, string mode, string surface, string stage = "intent")
        {
            // Explicit negative authored contract probes; normal physical driver remains public-only.
            var factory = IntentFactory(mode, stage);
            var game = New(factory); var independent = New(factory); ulong isolated = independent.Hash();
            var target = Entity(game, "destination"); var view = Work(game);
            var rows = new List<WorkRequirementView> { default };
            var intent = new PlanIntent("activity:inspect", ActivityWorkerSelection.CreateArguments(target, ActivityWorkerSelection.Unbound), "intent-probe", 50, false);
            c.Equal(Code.None, game.Seat(0).Preview(new PostOrderCommand { Plan = "inspect", Target = target }), "healthy independent intent admits before negative activation");
            var retained = game.Seat(0).Snapshot; game.Clock.Pause(); long at = game.Clock.StepIndex;
            Action read = surface == "requirements" ? () => view.PreviewOrderRequirements("inspect", target, rows)
                : surface == "command" ? () => game.Seat(0).Preview(new PostOrderCommand { Plan = "inspect", Target = target })
                : () => view.PreviewPlan(intent);
            c.Throws<InvalidOperationException>(read, "impure/throwing activity intent refuses " + stage + "/" + mode + "/" + surface);
            var fault = game.Fault; c.That(fault != null, "activity intent invocation latches original fault " + stage + "/" + mode + "/" + surface);
            if (fault == null) return; // Reproduction is failed assertions, never a healthy fallback.
            if (surface == "requirements") c.Equal(0, rows.Count, "bad activity intent exposes no requirement prefix");
            if (mode == "throw") c.That(fault.Message.Contains("proof.activity-intent.original"), "original activity intent exception retained");
            c.Equal(0, game.Update(), "bad activity intent stops native update");
            c.Equal(at, game.Clock.StepIndex, "bad intent cannot earn game time even after attempted clock resume");
            var current = game.Seat(0).Snapshot;
            c.That(current.State == GameState.Faulted && current.Tick == retained.Tick && ReferenceEquals(current.Space, retained.Space)
                && current.Changed.Count + current.Spawned.Count + current.Removed.Count == 0, "fault retains own previous private publication, never callback mutations");
            c.Throws<InvalidOperationException>(read, "faulted activity read cannot retry callback");
            c.That(ReferenceEquals(fault, game.Fault), "first activity intent fault remains authoritative");
            c.Throws<InvalidOperationException>(() => game.Save(), "bad activity intent cannot persist healthy");
            c.Throws<InvalidOperationException>(() => game.Hash(), "bad activity intent cannot hash healthy");
            c.Equal(isolated, independent.Hash(), "immutable negative intent content does not contaminate another session");
        }
        private static void MalformedPlanObservation(Check c, string stage)
        {
            var factory = IntentFactory("content", stage);
            var game = New(factory); var target = Entity(game, "destination"); game.Clock.Pause(); ulong before = game.Hash();
            var intent = new PlanIntent("activity:inspect", ActivityWorkerSelection.CreateArguments(target, ActivityWorkerSelection.Unbound), "malformed", 20, false);
            c.Equal(Codes.InvalidArgument, Work(game).PreviewPlan(intent), "pure provider ContentException remains a typed argument refusal");
            c.That(game.Fault == null && game.Hash() == before, "expected argument error does not poison or mutate session");
            var malformed = new PlanIntent("activity:inspect", Json.Parse("{}"), "malformed", 20, false);
            c.Equal(Codes.InvalidArgument, Work(game).PreviewPlan(malformed), "missing declared references refuse as malformed arguments");
            c.That(game.Fault == null && game.Hash() == before, "pure reference parser error remains healthy");
        }
        private static void ExecutionObservation(Check c, string stage, string mode)
        {
            var factory = IntentFactory(mode, stage); var game = New(factory); var independent = New(factory);
            game.Update(); // Arm after a real completed epoch; promotion runs before its own epoch commits.
            ulong isolated = independent.Hash(); var target = Entity(game, "destination"); var retained = game.Seat(0).Snapshot;
            var command = new SubmitPlanCommand { Intent = new PlanIntent("activity:inspect",
                ActivityWorkerSelection.CreateArguments(target, ActivityWorkerSelection.Unbound), "execution-probe", 50, false) };
            ulong before = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "unarmed plan preview remains admitted " + stage + "/" + mode);
            c.Equal(before, game.Hash(), "unarmed plan preview creates no execution state");
            game.Seat(0).Submit(command);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 12 && watch.ElapsedMilliseconds < 10000 && game.Fault == null; i++)
            {
                game.Update();
            }
            var fault = game.Fault;
            long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
            c.That(fault != null, "execution callback cannot mutate actual admission " + stage + "/" + mode
                + " clock=" + game.Clock.StepIndex + " results=" + string.Join(";", results.Select(x => x.Accepted + "/" + x.Refusal)));
            if (fault == null) return; // Actual negative reproduction, not a successful fallback.
            c.That(fault.Phase.StartsWith("observation.", StringComparison.Ordinal), "callback fails at its original observed boundary " + stage + "/" + mode);
            if (mode == "throw") c.That(fault.Message.Contains("proof.activity-intent.original"), "execution callback keeps original exception");
            var current = game.Seat(0).Snapshot;
            c.That(current.State == GameState.Faulted && current.Tick == retained.Tick && ReferenceEquals(current.Space, retained.Space)
                && current.Entities.Select(x => (x.Id, x.Version)).SequenceEqual(retained.Entities.Select(x => (x.Id, x.Version)))
                && current.Changed.Count + current.Spawned.Count + current.Removed.Count == 0, "execution mutation never reaches private publication");
            long at = game.Clock.StepIndex; c.Equal(0, game.Update(), "failed binding/identity stops later updates");
            c.Equal(at, game.Clock.StepIndex, "failed execution callback cannot earn additional time");
            c.Throws<InvalidOperationException>(() => game.Save(), "failed execution callback refuses healthy save");
            c.Throws<InvalidOperationException>(() => game.Hash(), "failed execution callback refuses healthy hash");
            c.That(ReferenceEquals(fault, game.Fault), "execution first fault retained");
            c.Equal(isolated, independent.Hash(), "execution callback does not affect another session");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current work observation build identity");
            foreach (bool half in new[] { false, true }) Physical(c, half);
            foreach (string kind in new[] { "preview", "active", "idle" }) Late(c, kind);
            RequirementVisibility(c, true); RequirementVisibility(c, false);
            foreach (string mode in new[] { "target", "clock", "space", "throw" })
                foreach (string surface in new[] { "requirements", "command", "plan" }) IntentObservation(c, mode, surface);
            foreach (string stage in new[] { "identity", "submission", "activation" })
                foreach (string mode in new[] { "target", "clock", "space", "throw", "target-content" })
                    IntentObservation(c, mode, stage == "activation" ? "command" : "plan", stage);
            MalformedPlanObservation(c, "identity"); MalformedPlanObservation(c, "submission");
            foreach (string mode in new[] { "target", "clock", "space", "throw" })
            { ExecutionObservation(c, "binding", mode); ExecutionObservation(c, "execution", mode); }
            ExecutionObservation(c, "execution", "order");
        }
    }
}
