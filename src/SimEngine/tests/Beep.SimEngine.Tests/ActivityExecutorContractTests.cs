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
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // File inputs, ordinary player intent and detached engine facts only, never executor/gameplay logic.
    internal static class ActivityExecutorContractTests
    {
        internal const string Case = "Activity executors: direct JSON strategies share physical work and observed state";
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Create(out ActivityExecutorContractsModule proof, bool independent = true, Action<JsonValue> edit = null, Action<JsonValue> worldEdit = null)
        {
            proof = new ActivityExecutorContractsModule();
            var data = Json.Parse(FrameworkProofContent.Read("activity-executor-contracts.json")); edit?.Invoke(data);
            var work = Json.Parse(FrameworkProofContent.Read("native-survey-work.json"));
            if (!independent)
                foreach (string kind in new[] { "staffed", "fixed", "timed" })
                {
                    work.Set(kind + "_activity_executors", data.Get("independent_" + kind + "_executors").Clone());
                    data.Set("independent_" + kind + "_executors", JsonValue.NewArray());
                }
            var modules = new GameModule[] { new ContentAccessModule(), new InventoryModule(), new MotionModule(),
                new TileNavigationModule(), new WorkModule(), new CompanyKnowledgeModule(), new WorldSetupModule(), proof };
            var definition = new GameDefinition("activity-executor-contracts", modules);
            var master = Json.Parse(FrameworkProofContent.Read("native-survey-master.json"));
            master.Get("modules").Add(Json.Parse("{\"id\":\"" + proof.Id + "\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"activity-executor-contracts.json\"]}"));
            var world = Json.Parse(FrameworkProofContent.Read("native-survey-world.json")); world.Remove("entities"); worldEdit?.Invoke(world);
            world.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(proof.Id));
            var files = new PreloadedFileSource().Add("manifest.json", master.ToString()).Add("native-survey-world.json", world.ToString())
                .Add("native-survey-work.json", work.ToString()).Add("activity-executor-contracts.json", data.ToString())
                .Add("open-access.json", FrameworkProofContent.Access);
            foreach (string path in new[] { "native-survey-startup.json", "native-survey-navigation.json", "native-survey-knowledge.json", "work-retry.json" })
                files.Add(path, FrameworkProofContent.Read(path));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        internal static SimGame New(GameFactory factory)
        { var request = new NewGameRequest { Scenario = "native-survey", Seed = 755 }; request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request); }
        internal static WorkView Work(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<WorkView>();
        internal static List<OrderView> Orders(SimGame game) { var rows = new List<OrderView>(); Work(game).CopyOrders(rows); return rows; }
        internal static List<PlanView> Plans(SimGame game) { var rows = new List<PlanView>(); Work(game).CopyPlans(rows); return rows; }
        internal static List<ActivityNodeView> Nodes(SimGame game, int id)
        { var rows = new List<ActivityNodeView>(); if (!Work(game).TryCopyNodes(id, rows)) throw new InvalidOperationException("missing order graph " + id); return rows; }
        internal static Fix Steel(SimGame game)
        { var rows = new List<Facet>(); game.Seat(0).View.Inspect(Entity(game, "survey.site"), rows); return rows.Single(r => r.Key == Storage.FacetStock && r.Subject == "steel").Value; }
        internal static SubmitPlanCommand Intent(SimGame game)
            => new SubmitPlanCommand { Intent = new PlanIntent("activity:executor.chain", ActivityWorkerSelection.CreateArguments(Entity(game, "survey.site"), ActivityWorkerSelection.Unbound), "executors", 60, false) };
        internal static int Begin(Check c, SimGame game)
        { Order(c, game, Intent(game)); Step(game); return Orders(game).Single().Id; }
        internal static void Reach(SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 900 && !ready(); i++) Step(game, twin);
            if (!ready()) throw new InvalidOperationException(stage + " clock=" + game.Clock.StepIndex + " orders=" +
                string.Join(";", Orders(game).Select(o => o.Id + "/" + o.State + "/" + o.Blocker + "/" + o.NodesDone)));
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current exact protocol"); c.Equal(154, EngineInfo.SaveFormat, "original jobs/nodes/resources codec");
            c.That(typeof(IActivityExecutor).Assembly.GetType("Beep.SimEngine.Standard.ActivityExecutorDef") == null, "obsolete executor base deleted");
            foreach (Type type in new[] { typeof(StaffedActivityExecutorDef), typeof(FixedActivityExecutorDef), typeof(TimedActivityExecutorDef),
                typeof(IndependentStaffedExecutorDef), typeof(IndependentFixedExecutorDef), typeof(IndependentTimedExecutorDef) })
                c.That(type.BaseType == typeof(Def), "direct existing executor contract " + type.Name);
            c.Equal(Physical(c, false), Physical(c, true), "native/direct mixed execution chronology");
            foreach (bool independent in new[] { false, true })
                foreach (string fault in new[] { "missing-travel", "empty-travel", "empty-work", "empty-departure", "duplicate-phases", "unknown-travel", "wrong-work-purpose",
                    "fixed-missing-work", "fixed-empty-work", "fixed-empty-blocker", "fixed-unknown-work", "fixed-wrong-purpose", "timed-missing-rate", "timed-zero-rate", "timed-negative-rate", "instant-field", "role-mismatch" })
                {
                    ActivityExecutorContractsModule proof = null;
                    c.Throws<ContentException>(() => New(Create(out proof, independent, data =>
                    {
                        string kind = fault.StartsWith("fixed-", StringComparison.Ordinal) ? "fixed" : fault.StartsWith("timed-", StringComparison.Ordinal) ? "timed" : "staffed";
                        var row = data.Get("independent_" + kind + "_executors")[0].Clone(); row.Set("id", JsonValue.Of("executor.unused"));
                        data.Get("independent_" + kind + "_executors").Add(row);
                        if (fault == "missing-travel") row.Remove("travel");
                        if (fault == "empty-travel") row.Set("travel", JsonValue.Of(""));
                        if (fault == "empty-work" || fault == "fixed-empty-work") row.Set("work", JsonValue.Of(""));
                        if (fault == "empty-departure") row.Set("departure", JsonValue.Of(""));
                        if (fault == "duplicate-phases") row.Set("work", row.Get("travel").Clone());
                        if (fault == "unknown-travel") row.Set("travel", JsonValue.Of("unknown.phase"));
                        if (fault == "wrong-work-purpose" || fault == "fixed-wrong-purpose") row.Set("work", JsonValue.Of("sim.work.travel"));
                        if (fault == "fixed-missing-work") row.Remove("work");
                        if (fault == "fixed-empty-blocker") row.Set("access_blocker", JsonValue.Of(""));
                        if (fault == "fixed-unknown-work") row.Set("work", JsonValue.Of("unknown.phase"));
                        if (fault == "timed-missing-rate") row.Remove("rate");
                        if (fault == "timed-zero-rate") row.Set("rate", JsonValue.Number("0"));
                        if (fault == "timed-negative-rate") row.Set("rate", JsonValue.Number("-1"));
                        if (fault == "instant-field") row.Set("instant", JsonValue.Of(true));
                        if (fault == "role-mismatch") data.Get("activities")[0].Get("nodes")[2].Set("roles", Json.Parse("[\"survey\"]"));
                    })), "unused/native-direct authoring refusal " + independent + "/" + fault);
                    c.Equal(0, proof.Initialize.Initializations, "refusal before Initialize " + independent + "/" + fault);
                }
            foreach (string fault in new[] { "id-alias", "module-alias", "composition-pause" })
            {
                ActivityExecutorContractsModule proof = null;
                c.Throws<Exception>(() => New(Create(out proof, edit: data =>
                {
                    var row = data.Get("independent_staffed_executors")[0].Clone(); row.Set("id", JsonValue.Of("executor.unused")); row.Set("metadata_fault", JsonValue.Of(fault));
                    data.Get("independent_staffed_executors").Add(row);
                })), "unused actual alias/composition refusal " + fault);
                c.Equal(0, proof.Initialize.Initializations, "actual composition before Initialize " + fault);
            }
            foreach (string fault in new[] { "create-target-private", "create-pause", "match-target-private", "check-worker-private", "check-throw", "rate-worker-private", "rate-pause" })
            {
                var game = New(Create(out var proof, edit: data => data.Get("independent_staffed_executors")[0].Set("metadata_fault", JsonValue.Of(fault))));
                if (fault.StartsWith("rate-", StringComparison.Ordinal))
                {
                    int id = Begin(c, game); Reach(game, null, () => Nodes(game, id).Single(n => n.Id == "operate").Progress.WholeTicks >= 2, "healthy rate before armed fault");
                    proof.ArmObservationFault(); c.Throws<Exception>(() => Step(game), "selected worker/clock rate observation " + fault);
                }
                else if (fault.StartsWith("check-", StringComparison.Ordinal))
                {
                    proof.ArmObservationFault();
                    c.Throws<Exception>(() => Work(game).PreviewWorkerCandidates("executor.chain", "operate", Entity(game, "survey.site"), new List<WorkWorkerCandidate>()), "selected worker preview observation " + fault);
                }
                else
                {
                    var command = Intent(game);
                    c.Equal(Code.None, game.Seat(0).Preview(command), "healthy pending plan before fault " + fault);
                    proof.ArmObservationFault(); game.Seat(0).Submit(command);
                    c.Throws<Exception>(() => Step(game), "actual itinerary creation/matching observation " + fault);
                }
                c.Throws<InvalidOperationException>(() => game.Save(), "faulted session save refuses " + fault);
                c.Throws<InvalidOperationException>(() => game.Hash(), "faulted session hash refuses " + fault);
            }
        }
        private static long Physical(Check c, bool independent)
        {
            var factory = Create(out var proof, independent); var game = New(factory); var isolated = New(factory); ulong untouched = isolated.Hash();
            c.That(proof.ActualAliases, "actual selected executor catalog ownership"); c.Equal(independent, proof.Independent, "native/direct strategy selection");
            c.Equal(2, proof.Initialize.Initializations, "one Initialize per actual session"); c.That(!game.Modules.Contains(TycoonModule.ModuleId), "work/manufacturing do not require money");
            var origin = ((TileSpatialSnapshot)game.Seat(0).Snapshot.Entities.Single(e => e.Def == "survey.machine").Spatial).Position;
            var limits = new ReplayLimits(4 * 1024 * 1024, 5000); var recorder = game.RecordReplay(limits);
            int id = Begin(c, game); var nodes = Nodes(game, id);
            c.Equal("operate|settle|finish", string.Join("|", nodes.Select(n => n.Id)), "sole graph compiles authored reverse-order join");
            c.Equal("operate|settle", string.Join("|", nodes.Single(n => n.Id == "finish").Dependencies), "fixed finish waits on physical and timed prerequisites");
            c.That(nodes.Single(n => n.Id == "operate").Progress.WholeTicks == 0 &&
                nodes.Single(n => n.Id == "finish").Progress.WholeTicks == 0, "travel and dependency wait earn no instant physical work");
            c.That(nodes.Single(n => n.Id == "settle").Progress.WholeTicks <= game.Clock.StepIndex,
                "timed progress is bounded by actual game time advanced for command receipt");
            c.That(!Work(game, 1).TryCopyNodes(id, new List<ActivityNodeView>()), "foreign company cannot read execution");
            c.Equal(Fix.FromInt(20), Steel(game), "travel assignment spends no materials");
            Reach(game, null, () => Nodes(game, id).Single(n => n.Id == "operate").Progress.WholeTicks >= 2, "actual physical operation");
            nodes = Nodes(game, id); var timed = nodes.Single(n => n.Id == "settle");
            c.That(timed.JobId == 0 && !timed.Worker.IsValid && timed.Progress.WholeTicks > 0, "timed work has earned time but no fabricated worker/job");
            c.Equal(0L, nodes.Single(n => n.Id == "finish").Progress.WholeTicks, "fixed successor cannot bypass productive predecessor");
            c.That(((TileSpatialSnapshot)game.Seat(0).Snapshot.Entities.Single(e => e.Def == "survey.machine").Spatial).Position != origin, "original motion physically moved machine");
            c.Equal(Fix.FromInt(12), Steel(game), "actual eight-unit input consumed once"); var retained = nodes.Single(n => n.Id == "operate"); var progress = retained.Progress;
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual partial jobs/itineraries/node state restore");
            ulong hash = game.Hash(); long at = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
            c.That(hash == game.Hash() && at == game.Clock.StepIndex, "sole paused clock freezes every strategy"); game.Clock.Resume(); twin.Clock.Resume();
            Reach(game, twin, () => Nodes(game, id).Single(n => n.Id == "finish").Progress.WholeTicks >= 2, "fixed productive successor");
            var fixedNode = Nodes(game, id).Single(n => n.Id == "finish");
            c.That(fixedNode.Worker.IsValid && fixedNode.JobId > 0 && fixedNode.Executor == "executor.fixed", "original fixed job owns real worker and earned progress");
            c.Equal(Fix.FromInt(12), Steel(game), "fixed and timed strategies invent no extra resources");
            c.Equal(progress, retained.Progress, "detached progress remains immutable"); twin = factory.Load(game.Save());
            Reach(game, twin, () => Plans(game).Single().State == PlanState.Done, "whole mixed workflow");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed original payload restores"); long completed = game.Clock.StepIndex;
            int cancelled = Begin(c, game); Reach(game, null, () => Nodes(game, cancelled).Single(n => n.Id == "operate").Progress.WholeTicks >= 2, "second physical execution");
            c.Equal(Fix.FromInt(4), Steel(game), "second actual input, no duplicate charge");
            Order(c, game, new CancelOrderCommand { Order = cancelled });
            Reach(game, null, () => Plans(game).Last().State == PlanState.Cancelled, "normal mixed cancellation"); Step(game);
            c.Equal(Fix.FromInt(4), Steel(game), "cancellation never fabricates refunded consumed input");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled actual claims/jobs restore");
            c.Equal(untouched, isolated.Hash(), "interleaved sessions isolated");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "normal plans/work/clock/cancel replay");
            return completed;
        }
    }
}
