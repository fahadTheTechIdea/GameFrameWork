using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Authored contracts/JSON and ordinary public player actions only.
    internal static class ActivityGraphDisclosureTests
    {
        internal const string Case = "Activity graph disclosure: nested requirement and node final effects stay private while earned work continues";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static WorkView Work(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<WorkView>();
        private static bool Knows(SimGame game, string subject) => game.Seat(0).View.Feature<KnowledgeView>()
            .TryRead(new PlayerId(0), "sim.knowledge.facts", subject, out _);
        private static GameFactory Factory(bool half, string reference)
            => RecipeDisclosureTests.Factory(false, "result", half, knowledgeExtraDocument: "activity-graph-disclosure.json",
                knowledgeEdit: root => root.Get("knowledge_visibility_policies")[0].Set("rules", Json.Parse("[\"graph.hide-" + reference + "\"]")));
        private static void Reach(SimGame game, SimGame twin, Func<bool> done, string stage)
        {
            for (int i = 0; i < 384 && !done(); i++) TransformationContentContractTests.Step(game, twin);
            if (!done())
            {
                var orders = new List<OrderView>(); game.Observer.Feature<WorkView>().CopyOrders(orders);
                throw new InvalidOperationException(stage + "/clock=" + game.Clock.StepIndex + "/orders=" +
                    string.Join(";", orders.Select(x => x.Id + "/" + x.Plan + "/" + x.State + "/" + x.Blocker)));
            }
        }
        private static void Hidden(Check c, SimGame game, EntityId target, int order)
        {
            ulong hash = game.Hash(); var rows = new List<WorkRequirementView>();
            c.Equal(Codes.UnknownDef, Work(game).PreviewOrderRequirements("graph.perform", target, rows), "hidden graph diagnostics use whole catalog closure");
            c.That(rows.Count == 0, "no requirement metadata prefix");
            c.Equal(Codes.UnknownDef, game.Seat(0).Preview(new PostOrderCommand { Plan = "graph.perform", Target = target }), "hidden graph player preview refused");
            if (order != 0)
            {
                c.That(!Work(game).TryReadProgress(order, out _) && !Work(game, 1).TryReadProgress(order, out _), "private and foreign graph progress withheld");
                var visible = new List<OrderView>(); Work(game).CopyOrders(visible);
                c.That(visible.All(x => x.Id != order), "private order metadata not partially disclosed");
            }
            c.Equal(hash, game.Hash(), "disclosure does not evaluate or advance physical work");
        }
        private static void Healthy(Check c, bool half, string reference)
        {
            var factory = Factory(half, reference); var game = ConstructionPlanTests.NewPoints(factory);
            var idle = ConstructionPlanTests.NewPoints(factory); ulong idleHash = idle.Hash();
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "graph activity requires no business module");
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current graph disclosure identity");
            var target = TransformationContentContractTests.Entity(game, "source");
            var worker = TransformationContentContractTests.Entity(game, "machine");
            var limits = new ReplayLimits(6 * 1024 * 1024, 8000); using var recording = game.RecordReplay(limits);
            Hidden(c, game, target, 0);
            int sequence = game.Seat(0).Submit(new PostOrderCommand { Plan = "graph.perform", Target = target });
            TransformationContentContractTests.Step(game); long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
            c.Equal(Codes.UnknownDef, results.Single(x => x.Sequence == sequence).Refusal, "command matches hidden graph preview");
            PermanentRemovalTests.Submit(c, game, new ConfigureWorkPolicyCommand { Worker = worker,
                Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) });
            PermanentRemovalTests.Submit(c, game, new PostOrderCommand { Plan = "recipe.discover", Target = target });
            Reach(game, null, () => Knows(game, "recipe.discovered"), "real timed graph metadata discovery");
            var criteria = new List<WorkRequirementView>();
            c.Equal(Code.None, Work(game).PreviewOrderRequirements("graph.perform", target, criteria), "all original graph aliases now known");
            c.That(criteria.Count > 0, "nested actual requirement diagnostics available");
            PermanentRemovalTests.Submit(c, game, new PostOrderCommand { Plan = "graph.perform", Target = target });
            var orders = new List<OrderView>(); game.Observer.Feature<WorkView>().CopyOrders(orders);
            int order = orders.Single(x => x.Plan == "activity:graph.perform").Id;
            Reach(game, null, () => Knows(game, "graph.node-earned") && !Knows(game, "graph.completed"), "real node effect before final effect");
            c.That(Work(game).TryReadProgress(order, out var retained), "partial graph observation detached");
            PermanentRemovalTests.Submit(c, game, new ForgetKnowledgeCommand { Policy = "graph.forget" });
            c.That(!Knows(game, "recipe.discovered"), "authored forgetting hides selected graph reference"); Hidden(c, game, target, order);
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "hidden accepted graph restores original state");
            game.Clock.Pause(); twin.Clock.Pause(); long at = game.Clock.StepIndex; ulong paused = game.Hash(); TransformationContentContractTests.Step(game, twin);
            c.That(game.Clock.StepIndex == at && game.Hash() == paused, "sole clock pauses hidden physical work"); game.Clock.Resume(); twin.Clock.Resume();
            Reach(game, twin, () => Knows(game, "graph.completed"), "hidden earned final effect completes");
            c.That(game.Observer.Feature<WorkView>().TryReadCompanyActivityCompletion(new PlayerId(0), "activity:graph.perform", target, out var fact) &&
                fact.Order == order && fact.CompletedTick > fact.CreatedTick, "original history confirms positive-time physical completion");
            c.That(!Work(game).TryReadActivityCompletion("activity:graph.perform", target, out _), "hidden final history cannot leak through metadata");
            c.That(retained.UnfinishedNodes > 0, "old partial result remains detached after completion");
            PermanentRemovalTests.Submit(c, game, new PostOrderCommand { Plan = "recipe.discover", Target = target });
            twin = factory.Load(game.Save()); Reach(game, twin, () => Knows(game, "recipe.discovered"), "physical re-discovery");
            c.That(Work(game).TryReadActivityCompletion("activity:graph.perform", target, out var revealed) && revealed.Order == order, "re-discovery reveals same earned order");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completion and graph metadata durable");
            c.Equal(idleHash, idle.Hash(), "interleaved session does not learn graph state");
            c.That(new ReplayVerifier().Verify(factory, recording.Finish(), limits).Success, "normal discovery graph forgetting completion stream replays");
        }
        private static void Run(Check c)
        { foreach (bool half in new[] { false, true }) foreach (string reference in new[] { "requirement", "node", "final" }) Healthy(c, half, reference); }
    }
}
