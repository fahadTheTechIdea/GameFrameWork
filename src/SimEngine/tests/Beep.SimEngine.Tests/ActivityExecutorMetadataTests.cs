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
using static Beep.SimEngine.Tests.ActivityExecutorContractTests;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Replaces the retired executor-bool callback proof; JSON roles are the sole classification source.
    internal static class ActivityExecutorMetadataTests
    {
        internal const string Case = "Activity roles: authored nodes solely classify work and validate complete metadata";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current sole-role protocol");
            c.Equal(154, EngineInfo.SaveFormat, "original jobs/nodes/codecs unchanged");
            foreach (Type type in new[] { typeof(IActivityExecutor), typeof(StaffedActivityExecutorDef),
                typeof(FixedActivityExecutorDef), typeof(TimedActivityExecutorDef), typeof(IndependentStaffedExecutorDef),
                typeof(IndependentFixedExecutorDef), typeof(IndependentTimedExecutorDef) })
                c.That(type.GetProperty("RequiresWorker") == null, "no executor classification callback or compatibility property " + type.Name);
            foreach (bool independent in new[] { false, true }) Physical(c, independent);
            Staffing(c); Invalid(c);
        }
        private static void Physical(Check c, bool independent)
        {
            var factory = Create(out var proof, independent, edit: data => {
                var node = data.Get("activities")[0].Get("nodes")[1];
                node.Set("effect", JsonValue.Of("survey.observe"));
                if (independent) data.Get("independent_staffed_executors")[0].Set("metadata_fault", JsonValue.Of("definition-inputs"));
            });
            var game = New(factory); var isolated = New(factory); ulong separate = isolated.Hash();
            var graph = proof.AuthoredGraph;
            c.That(ActivityExecutorContracts.RequiresWorker(graph.Find("operate")) &&
                ActivityExecutorContracts.RequiresWorker(graph.Find("finish")) &&
                !ActivityExecutorContracts.RequiresWorker(graph.Find("settle")), "sole authored role classification before and after world creation");
            c.That(graph.HasMachineWork(null) && graph.HasMachineWork("finish") &&
                !graph.HasMachineWork("settle"), "DAG machine ancestry derives from captured roles");
            c.That(graph.Find("operate").Requirement != null && graph.Find("operate").Effect == "survey.observe" &&
                graph.Find("finish").Dependencies.SequenceEqual(new[] { "operate", "settle" }), "validator received complete canonical metadata");
            c.Throws<NotSupportedException>(() => ((IList<WorkerRoleId>)graph.Find("operate").Roles).Clear(), "captured roles immutable");
            c.Throws<NotSupportedException>(() => ((IList<string>)graph.Find("finish").Dependencies).Clear(), "captured dependencies immutable");
            var limits = new ReplayLimits(4 * 1024 * 1024, 5000); var recorder = game.RecordReplay(limits); int id = Begin(c, game);
            Reach(game, null, () => Nodes(game, id).Single(n => n.Id == "operate").Progress.WholeTicks >= 2, "real machine work");
            c.That(Nodes(game, id).Single(n => n.Id == "operate").Worker.IsValid &&
                !Nodes(game, id).Single(n => n.Id == "settle").Worker.IsValid, "machine assigned, parallel timer invents no machine");
            c.Equal(Fix.FromInt(12), Steel(game), "physical resource consumption exactly once");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "role-based itineraries restore");
            var offers = new List<WorkerDispatchOffer>(); ulong hash = game.Hash(); Work(game).CopyAssignments(offers);
            c.Equal(hash, game.Hash(), "derived assignment facts pure");
            c.That(offers.All(o => o.Company == game.Seat(0).View.Viewer), "company-private assignment facts");
            game.Clock.Pause(); twin.Clock.Pause(); hash = game.Hash(); long at = game.Clock.StepIndex; Step(game, twin);
            c.That(hash == game.Hash() && at == game.Clock.StepIndex, "sole paused clock advances no role work"); game.Clock.Resume(); twin.Clock.Resume();
            Reach(game, twin, () => Plans(game).Single().State == PlanState.Done, "role-based physical/timed dependency completion");
            c.Equal(Fix.FromInt(12), Steel(game), "completion does not double-consume");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed original checkpoint");
            c.Equal(separate, isolated.Hash(), "interleaved session unchanged");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "normal commands replay with role data");
        }
        private static void Staffing(Check c)
        {
            var factory = StaffedTransformationGame.Factory(); var game = ProductionStaffingTests.New(factory);
            ProductionStaffingTests.Enqueue(c, game, "staff.batch"); var target = ProductionStaffingTests.Find(game, "staff.factory");
            c.Equal(StaffedTransformationGate.Shortage, ProductionStaffingTests.State(game).Blocker, "unassigned role declarations do not authorize production");
            ProductionStaffingTests.Order(c, game, new PostOrderCommand { Plan = "staff.operate", Target = target, Priority = 60 });
            ProductionStaffingTests.Until(game, () => ProductionStaffingTests.Staffing(game).Blocker.IsNone, "actual authored-role team");
            var members = ProductionStaffingTests.Staffing(game).Requirements.SelectMany(n => n.Assignments).ToArray();
            c.That(members.Length == 2 && members.Select(a => a.Worker).Distinct().Count() == 2, "two actual machines, no duplicated staffing");
            c.Equal(new WorkerRoleId("staff.technician"), members.Single(a => a.Node == "primary").Role, "any-compatible authored role chooses complete team");
            var twin = factory.Load(game.Save());
            ProductionStaffingTests.Until(game, () => ProductionStaffingTests.State(game).Queue.Count == 0, "native manufacturing completes", twin);
            c.Equal(Fix.FromInt(19), ProductionStaffingTests.Stock(game), "two inputs become one output; role data grants no free resources");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "staffing uses original work/manufacturing codecs");
        }
        private static void Invalid(Check c)
        {
            foreach (bool independent in new[] { false, true })
                foreach (string fault in new[] { "machine-empty", "timer-roles", "duplicate-role", "missing-role", "missing-edge", "cycle" })
                {
                    var factory = Create(out var proof, independent, edit: data => {
                        var nodes = data.Get("activities")[0].Get("nodes");
                        if (fault == "machine-empty") nodes[1].Set("roles", Json.Parse("[]"));
                        if (fault == "timer-roles") nodes[2].Set("roles", Json.Parse("[\"survey\"]"));
                        if (fault == "duplicate-role") nodes[1].Set("roles", Json.Parse("[\"survey\",\"survey\"]"));
                        if (fault == "missing-role") nodes[1].Set("roles", Json.Parse("[\"missing.role\"]"));
                        if (fault == "missing-edge") nodes[0].Set("after", Json.Parse("[\"absent\"]"));
                        if (fault == "cycle") nodes[1].Set("after", Json.Parse("[\"finish\"]"));
                    });
                    c.Throws<ContentException>(() => New(factory), "strict authored shape " + independent + "/" + fault);
                    c.Equal(0, proof.Initialize.Initializations, "before Initialize " + fault);
                }
            var thrown = Create(out var throwing, edit: data => data.Get("independent_staffed_executors")[0].Set("metadata_fault", JsonValue.Of("definition-throw")));
            c.Throws<ContentException>(() => New(thrown), "typed pre-world definition validator error");
            c.Equal(0, throwing.Initialize.Initializations, "thrown validator never initializes");
            foreach (bool process in new[] { false, true })
            {
                Action<JsonValue> edit = content => {
                    var node = content.Get("activities")[0].Get("nodes")[0];
                    node.Set("executor", JsonValue.Of("work.timed")); node.Set("roles", Json.Parse("[]"));
                };
                var factory = process ? ProcessStaffingGame.Factory(edit) : StaffedTransformationGame.Factory(edit);
                c.Throws<ContentException>(() => ProductionStaffingTests.New(factory), "timed node cannot satisfy machine staffing " + process);
            }
        }
    }
}
