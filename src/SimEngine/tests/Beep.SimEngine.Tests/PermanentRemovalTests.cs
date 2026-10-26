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
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Pure master/JSON authoring and public player intent. Original engine owners execute all work.
    internal static class PermanentRemovalTests
    {
        internal const string Case = "Permanent removal: authored physical worker loss and partial-haul endpoint retirement preserve state";
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static WorkView Work(SimGame g, int seat = 0) => g.Seat(seat).View.Feature<WorkView>();
        internal static List<WorkJobView> Jobs(SimGame g)
        { var rows = new List<WorkJobView>(); Work(g).CopyJobs(rows); return rows; }
        internal static WorkJobView Job(SimGame g, int id) => Jobs(g).Single(x => x.Id == id);
        internal static PlanView Plan(SimGame g, long id)
        { var rows = new List<PlanView>(); Work(g).CopyPlans(rows); return rows.Single(x => x.Id == id); }
        internal static ActivityNodeView Node(SimGame g, int order, string name)
        { var rows = new List<ActivityNodeView>(); Work(g).TryCopyNodes(order, rows); return rows.Single(x => x.Id == name); }
        internal static bool Exists(SimGame g, EntityId id) => g.Seat(0).View.TryGetEntity(id, out _);
        internal static Fix At(SimGame g, EntityId id)
        { if (!g.Seat(0).View.TryGetEntity(id, out var value)) throw new InvalidOperationException("missing physical entity " + id); return ((PointSnapshot)value.Spatial).Position; }
        internal static ResourceId Supply(SimGame g)
        { if (!g.Seat(0).View.TryFindResource("supply", out var value)) throw new InvalidOperationException("missing authored supply"); return value; }
        internal static LogisticsOperationalSnapshot Facts(SimGame g, EntityId id)
        {
            var refusal = g.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(id, Supply(g), out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("actual endpoint facts " + id + "/" + refusal); return value;
        }
        internal static Fix Total(SimGame g)
        {
            Fix sum = Fix.Zero;
            foreach (var entity in g.Seat(0).Snapshot.Entities)
                if (g.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(entity.Id, Supply(g), out var value).IsNone) sum += value.Stock;
            return sum;
        }
        internal static void Step(SimGame g, SimGame twin = null)
        {
            g.Update(); twin?.Update();
            if (g.Fault != null || twin?.Fault != null || twin != null && g.Hash() != twin.Hash())
                throw new InvalidOperationException("removal update " + g.Tick + "/" + g.Fault + "/" + twin?.Fault);
        }
        internal static void Reach(SimGame g, SimGame twin, Func<bool> done, string stage)
        {
            var trail = new Queue<string>();
            for (int i = 0; i < 512 && !done(); i++)
            {
                trail.Enqueue(g.Tick + ":stock=" + Total(g) + ":jobs=" + string.Join(";", Jobs(g).Select(j => j.Id + "/" + j.State + "/" + j.Worker + "/" + j.ToilIndex + "/" + j.Loaded + "/" + j.Blocker)) +
                    ":entities=" + string.Join(";", g.Seat(0).Snapshot.Entities.Select(e => e.Id + "/" + e.Def + "/" + ((PointSnapshot)e.Spatial).Position + "/" + e.State)));
                if (trail.Count > 12) trail.Dequeue(); Step(g, twin);
            }
            if (!done()) throw new InvalidOperationException(stage + " exceeded512Updates " + string.Join(" | ", trail));
        }
        internal static CommandResult Submit(Check c, SimGame g, Command command)
        {
            ulong before = g.Hash(); c.Equal(Code.None, g.Seat(0).Preview(command), "normal command preview");
            c.Equal(before, g.Hash(), "preview is pure"); int sequence = g.Seat(0).Submit(command); Step(g);
            long cursor = 0; var results = new List<CommandResult>(); g.Seat(0).ReadResults(ref cursor, results);
            var result = results.Single(x => x.Sequence == sequence); c.That(result.Accepted, "ordinary command accepted " + result.Refusal);
            if (!result.Accepted) throw new InvalidOperationException("removal command " + result.Refusal); return result;
        }
        internal static SimGame Save(Check c, GameFactory f, SimGame g)
        { var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "actual removal/graph/stock/clock checkpoint"); return twin; }
        internal static void Policy(Check c, SimGame g, string name, bool enabled, bool builder = false)
            => Submit(c, g, new ConfigureWorkPolicyCommand { Worker = Entity(g, name), Settings = new WorkPolicySettings(enabled, 0,
                new[] { new WorkerRoleId(builder ? "extension.builder" : "haul") }, null, true, true, WorkAssignmentMode.All) });
        private static void Pause(Check c, SimGame g, long plan, bool paused)
            => Submit(c, g, new ConfigurePlanCommand { Plan = plan, Group = "retire", Priority = 100, Paused = paused });
        private static long Retire(Check c, SimGame g, string name, EntityId target, bool pack)
        {
            int actor = Entity(g, "lifecycle.b").Value;
            string bindings = "{\"node\":\"retire\",\"role\":\"extension.builder\",\"worker\":" + actor + "}";
            if (pack) bindings = "{\"node\":\"pack\",\"role\":\"extension.builder\",\"worker\":" + actor + "}," + bindings;
            var intent = new PlanIntent("activity:retire." + name + "-plan", Json.Parse("{\"target\":" + target.Value + ",\"workers\":[" + bindings + "]}"), "retire", 100, false);
            return Submit(c, g, new SubmitPlanCommand { Intent = intent }).Arg0;
        }
        internal static GameFactory Factory(bool half, Action<JsonValue> invalid = null, WorkflowExtensionModule module = null, bool endpoints = false,
            IReadOnlyList<GameModule> modules = null, string declarations = "[]", IReadOnlyDictionary<string, string> documents = null,
            IReadOnlyList<string> startup = null, Action<JsonValue> authorWorld = null, Action<JsonValue> authorLifecycle = null)
        {
            var add = Json.Parse(FrameworkProofContent.Read("permanent-removal.json")); invalid?.Invoke(add);
            var lifecycle = Json.Parse(FrameworkProofContent.Read("workflow-interruption.json"));
            string dock = "{\"work_roles\":[\"extension.builder\"],\"resources\":[\"supply\"],\"slots\":[{\"id\":\"retire.service.a\",\"kind\":\"service\",\"offset\":1},{\"id\":\"retire.service.b\",\"kind\":\"service\",\"offset\":2},{\"id\":\"retire.exit\",\"kind\":\"departure\",\"offset\":3},{\"id\":\"retire.wait\",\"kind\":\"waiting\",\"offset\":4}]}";
            var actorDock = Json.Parse(dock); actorDock.Set("resources", JsonValue.NewArray());
            // On this one-dimensional map a paused loader must not occupy the outer service
            // bay and physically trap the retirement crew behind it. Existing slot filters
            // author separate inner hauling and outer work approaches; no traffic bypass.
            var endpointDock = Json.Parse(dock);
            endpointDock.Get("slots")[0].Set("resource", JsonValue.Of("supply"));
            endpointDock.Get("slots")[1].Set("work_role", JsonValue.Of("extension.builder"));
            lifecycle.Get("entities")[0].Get("components").Set("point_dock", actorDock);
            authorLifecycle?.Invoke(lifecycle);
            if (endpoints)
            {
                StartupContentAuthoring.Position(lifecycle, "lifecycle.a", JsonValue.Number("95"));
                StartupContentAuthoring.Position(lifecycle, "lifecycle.home", JsonValue.Number("70"));
            }
            var retry = Json.Parse(FrameworkProofContent.Read("work-retry.json")); var profile = retry.Get("work_retry_profiles")[0];
            profile.Set("initial_delay", Json.Parse("{\"numerator\":1,\"denominator\":1,\"unit\":\"minute\"}"));
            profile.Set("maximum_delay", Json.Parse("{\"numerator\":5,\"denominator\":1,\"unit\":\"minute\"}"));
            profile.Get("retry_blockers").Add(JsonValue.Of(ResourceTransactions.ActivePromises.Id));
            profile.Get("retry_blockers").Add(JsonValue.Of(ResourceTransactions.UndisposedStock.Id));
            var docs = new Dictionary<string, string> {
                {"workflow-extension.json",FrameworkProofContent.Read("workflow-extension.json")},
                {"workflow-extension-inputs.json",FrameworkProofContent.Read("workflow-extension-inputs.json")},
                {"workflow-interruption.json",lifecycle.ToString()} };
            var selected = new List<GameModule> { module ?? new WorkflowExtensionModule(), new LogisticsRecoveryPolicyModule() };
            if (modules != null) selected.AddRange(modules);
            var manifest = Json.Parse("[{\"id\":\"proof.workflow-extension\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.construction\",\"version\":\"1.0.0\"},{\"id\":\"points\",\"version\":\"1.0.0\"},{\"id\":\"sim.world-setup\",\"version\":\"1.0.0\"}],\"documents\":[\"workflow-extension.json\",\"workflow-extension-inputs.json\",\"workflow-interruption.json\"]},{\"id\":\"sim.logistics-recovery\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[]}]");
            var extensions = Json.Parse(declarations); for (int i=0;i<extensions.Count;i++) manifest.Add(extensions[i]);
            var authored = new Dictionary<string, string> { { "permanent-removal.json", add.ToString() } };
            if (documents != null) foreach (var document in documents) authored.Add(document.Key, document.Value);
            var profiles = new List<string> { "lifecycle.start", "retire.start" }; if (startup != null) profiles.AddRange(startup);
            return ConstructionPlanTests.Points(extraModules: selected, extraDeclarations:manifest.ToString(),
                extraDocuments: docs, additionalDocuments: authored,
                retryProfiles: retry.ToString(), startupProfiles: profiles,
                scenarioEdit: scenario => {
                    scenario.Get("modules").Add(JsonValue.Of(WorkflowExtensionModule.ModuleId));
                    scenario.Get("modules").Add(JsonValue.Of(LogisticsRecoveryPolicyModule.ModuleId));
                    if (modules != null) foreach (var added in modules) scenario.Get("modules").Add(JsonValue.Of(added.Id));
                    scenario.Get("settings").Set(LogisticsRecoveryPolicyModule.ModuleId, Json.Parse("{\"profile\":\"lifecycle.recovery\"}"));
                }, worldEdit: world => {
                    StartupContentAuthoring.Position(world, "machine", JsonValue.Number("4"));
                    if (endpoints) for (int i=0;i<2;i++) world.Get("entities")[i].Get("components").Set("point_dock", endpointDock.Clone());
                    if (half) world.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
                    authorWorld?.Invoke(world);
                });
        }
        private static void Admission(Check c)
        {
            Action<JsonValue>[] invalid = {
                x => x.Get("entity_removal_effects")[0].Set("source", JsonValue.Of("missing")),
                x => x.Get("entity_removal_effects")[0].Set("source", JsonValue.Of("lifecycle.b")),
                x => x.Get("entity_activities")[0].Get("nodes")[0].Set("effect", JsonValue.Of("retire.worker")),
                x => { var n=x.Get("entity_activities")[0].Get("nodes")[0]; n.Set("executor",JsonValue.Of("extension.timer"));n.Remove("roles"); },
                x => x.Get("entity_activities")[0].Get("nodes")[0].Get("duration").Set("numerator",JsonValue.Number("0"))
            };
            foreach (var change in invalid)
            {
                var module = new WorkflowExtensionModule();
                c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(Factory(false, change, module)), "invalid removal content refuses before initialization");
                c.Equal(0, module.Initialization.Initializations, "invalid removal never initializes module systems");
            }
        }
        private static void Worker(Check c, bool half)
        {
            var f = Factory(half); var g = ConstructionPlanTests.NewPoints(f); var idle = ConstructionPlanTests.NewPoints(f); ulong isolated = idle.Hash();
            var limits = new ReplayLimits(4 * 1024 * 1024, 6000); using var recorder = g.RecordReplay(limits);
            c.That(!g.Modules.Contains(TycoonModule.ModuleId), "physical retirement is resource-only without business");
            Policy(c, g, "machine", true); Policy(c, g, "lifecycle.a", true, true);
            Submit(c, g, new SetHomeCommand { Worker = Entity(g, "machine"), Home = Entity(g, "source") });
            long build = Submit(c, g, new SubmitPlanCommand { Intent = new PlanIntent("build:extension.facility", Json.Parse("{\"placement\":{\"space\":\"field.points\",\"position\":30}}"), "retire", 70, false) }).Arg0;
            var twin = Save(c, f, g); Reach(g, twin, () => Plan(g, build).Order > 0, "physical build intent"); int order = Plan(g, build).Order;
            Reach(g, twin, () => Node(g, order, "materials").State == NodeState.Done, "actual eight hauled and spent");
            Submit(c, g, new PostOrderCommand { Plan = "extension.calibrate", Target = Entity(g, "lifecycle.a") }); twin = Save(c, f, g);
            Reach(g, twin, () => Node(g, order, "assemble").Progress.WholeTicks >= (half ? 4 : 2), "actual earned first actor prefix");
            Pause(c, g, build, true); var prefix = Node(g, order, "assemble"); EntityId actor = Entity(g, "lifecycle.a");
            var retainedEntity = g.Seat(0).Snapshot.Entities.Single(x => x.Id == actor);
            Policy(c, g, "lifecycle.b", true, true); long retirement = Retire(c, g, "worker", actor, false); twin = Save(c, f, g);
            Reach(g, twin, () => Plan(g, retirement).Order > 0 && Node(g, Plan(g, retirement).Order, "retire").Progress.WholeTicks > 0, "other selected actor travels and earns retirement");
            Fix serviceAt = At(g, Entity(g, "lifecycle.b")), targetAt = ((PointSnapshot)retainedEntity.Spatial).Position;
            c.That(Exists(g, actor) && (serviceAt == targetAt + Fix.One || serviceAt == targetAt + Fix.FromInt(2)),
                "worker remains live until actual retirement finishes at one of its two authored service bays; observed=" + serviceAt);
            twin = Save(c, f, g); Reach(g, twin, () => Plan(g, retirement).State == PlanState.Done, "actual permanent worker removal");
            c.That(!Exists(g, actor) && Node(g, order, "assemble").Progress.Equals(prefix.Progress), "permanent loss retains original assembly prefix");
            c.Equal(prefix.JobId, Node(g, order, "assemble").JobId, "same original job survives actual missing worker");
            c.Equal(Fix.FromInt(12), Total(g), "worker loss never refunds/repeats spent eight");
            c.That(retainedEntity.Id == actor && retainedEntity.Def == "lifecycle.a", "retained removed worker facts remain detached");
            c.Equal(Codes.NotFound, Work(g).PreviewPlanRepeat(retirement), "retired stable target never substitutes another worker");
            twin = Save(c, f, g); for (int i = 0; i < 3; i++) Step(g, twin);
            c.Equal(prefix.Progress, Node(g, order, "assemble").Progress, "paused post-loss checkpoint earns no work");
            Pause(c, g, build, false); twin = Save(c, f, g);
            Reach(g, twin, () => Plan(g, build).State == PlanState.Done, "backup resumes original graph after permanent loss");
            for (int i = 0; i < 4; i++) Step(g, twin);
            c.Equal(1, g.Seat(0).Snapshot.Entities.Count(x => x.Def == "extension.facility"), "retained graph commits exactly one physical facility");
            c.Equal(Fix.FromInt(12), Total(g), "once-only completion conserves actual remaining stock");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "terminal retirement and construction history save");
            c.Equal(isolated, idle.Hash(), "permanent loss isolates another session");
            c.That(new ReplayVerifier().Verify(f, recorder.Finish(), limits).Success, "public physical retirement and reselection replay");
        }
        private static void Endpoint(Check c, bool half, bool delivery)
        {
            var f = Factory(half, endpoints:true); var g = ConstructionPlanTests.NewPoints(f); var idle = ConstructionPlanTests.NewPoints(f); ulong isolated = idle.Hash();
            var limits = new ReplayLimits(4 * 1024 * 1024, 6000); using var recorder = g.RecordReplay(limits);
            Policy(c, g, "machine", true); Policy(c, g, "lifecycle.b", true, true);
            Submit(c, g, new SetHomeCommand { Worker = Entity(g, "lifecycle.b"), Home = Entity(g, "lifecycle.home") });
            int haul = checked((int)Submit(c, g, new HaulResourcesCommand { Source = Entity(g, "source"), Destination = Entity(g, "destination"),
                Worker = Entity(g, "machine"), Resource = Supply(g), Ceiling = Fix.FromInt(8), Priority = 100 }).Arg0);
            var twin = Save(c, f, g); Reach(g, twin, () => Job(g, haul).Loaded.IsPositive && Job(g, haul).Loaded < Job(g, haul).Amount &&
                Job(g, haul).ToilIndex == (delivery ? 3 : 1), "real partial pickup/delivery before removal");
            Policy(c, g, "machine", false); var prefix = Job(g, haul); EntityId carrier = Entity(g, "machine");
            Fix cargo = Facts(g, carrier).Stock, position = At(g, carrier);
            EntityId target = Entity(g, delivery ? "destination" : "source"); Fix targetStock = Facts(g, target).Stock, targetPosition = At(g, target);
            long retirement = Retire(c, g, delivery ? "destination" : "source", target, true); twin = Save(c, f, g);
            Reach(g, twin, () => Plan(g, retirement).Order > 0 && Node(g, Plan(g, retirement).Order, "pack").Blocker == ResourceTransactions.ActivePromises,
                "actual live haul promise blocks physical evacuation");
            c.That(Exists(g, target) && Facts(g, target).Stock == targetStock && Facts(g, carrier).Stock == cargo && At(g, carrier) == position,
                "guarded endpoint cannot discard stock/cargo or move paused carrier");
            c.Equal(prefix.HandlingProgress, Job(g, haul).HandlingProgress, "live promise wait preserves actual partial handling");
            c.Equal(Fix.FromInt(20), Total(g), "preparation wait conserves all original twenty");
            twin = Save(c, f, g); g.Clock.Pause(); twin.Clock.Pause(); ulong paused = g.Hash();
            c.Equal(0, g.Update(1000000), "sole paused clock stops recovery/removal"); c.Equal(0, twin.Update(1000000), "saved sole clock also stops");
            c.That(g.Hash() == paused && g.Hash() == twin.Hash(), "paused preparation checkpoint is stable"); g.Clock.Resume(); twin.Clock.Resume();
            Submit(c, g, new SetWorkerHeldCommand { Worker = carrier, Held = true }); twin = Save(c, f, g);
            c.That(!Jobs(g).Any(x => x.Id == haul) && Facts(g, carrier).OutboundPromised.IsZero && Facts(g, carrier).Stock == cargo && At(g, carrier) == position,
                "actual player interruption releases old commitments without losing cargo/position");
            Reach(g, twin, () => Plan(g, retirement).State == PlanState.Done, "explicit timed stock packing and endpoint removal");
            c.That(!Exists(g, target) && Facts(g, carrier).Stock == cargo && At(g, carrier) == position, "removed endpoint cannot teleport retained carrier");
            c.Equal(Fix.FromInt(20), Total(g), "physical packed stock plus retained cargo conserves all twenty");
            var packed = new List<EntityView>();
            foreach (var entity in g.Seat(0).Snapshot.Entities)
                if (entity.Id != carrier && entity.Def != "source" && entity.Def != "destination" && entity.Def != "retire.receiver" &&
                    g.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(entity.Id, Supply(g), out var value).IsNone && value.Stock.IsPositive) packed.Add(entity);
            c.That(packed.Count > 0 && packed.All(x => ((PointSnapshot)x.Spatial).Position == targetPosition), "original evacuation owner packs only at exact removed placement");
            c.That(prefix.Loaded == cargo && prefix.HandlingProgress.WholeTicks > 0, "retained old partial shipment stays detached from cancellation/removal");
            Reach(g, twin, () => At(g, Entity(g, "lifecycle.b")) == Fix.FromInt(70), "retirement worker physically clears carrier route to its authored home");
            Submit(c, g, new ConfigureStockPolicyCommand { Store = Entity(g, "retire.receiver"), Resource = Supply(g),
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, cargo, cargo, cargo, 90, LogisticsReserveAccess.Protected)) });
            Policy(c, g, "machine", true); Submit(c, g, new SetWorkerHeldCommand { Worker = carrier, Held = false }); twin = Save(c, f, g);
            Reach(g, twin, () => Stock(g, "retire.receiver") == cargo && Facts(g, carrier).Stock.IsZero, "actual residual cargo travels and unloads to authored demand");
            c.Equal(Fix.FromInt(20), Total(g), "removed endpoint recovery has no duplication or loss");
            c.Equal(Fix.FromInt(20), At(g, carrier), "cargo recovery reaches actual receiver over game time");
            var hidden = new List<PlanView>(); Work(g, 1).CopyPlans(hidden); c.Equal(0, hidden.Count, "endpoint retirement intent/history remains private");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "terminal removal/accounting/recovery durable");
            c.Equal(isolated, idle.Hash(), "partial endpoint retirement isolates another session");
            c.That(new ReplayVerifier().Verify(f, recorder.Finish(), limits).Success, "partial pickup/delivery removal is public command replay");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current sole engine build identity"); Admission(c);
            foreach (bool half in new[] { false, true }) { Worker(c, half); Endpoint(c, half, false); Endpoint(c, half, true); }
        }
    }
}
