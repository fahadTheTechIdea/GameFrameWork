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
    // JSON/module authoring and ordinary public player inputs/views only.
    internal static class ActivityContentDependenciesTests
    {
        internal const string Case = "Activity content dependencies: native and direct provider references protect metadata without stopping accepted physical work";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static WorkView Work(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<WorkView>();
        private static bool Knows(SimGame game) => game.Seat(0).View.Feature<KnowledgeView>()
            .TryRead(new PlayerId(0), "sim.knowledge.facts", "recipe.discovered", out _);
        private static GameFactory Factory(bool half, string hidden, ActivityContentDependenciesModule module, string bad = null)
        {
            var document = Json.Parse(FrameworkProofContent.Read("activity-content-dependencies-module.json"));
            if (bad != null) document.Get("dependency_invalid_effects").Add(Json.Parse("{\"id\":\"deps.invalid\",\"mode\":\"" + bad + "\",\"resource\":\"supply\"}"));
            return RecipeDisclosureTests.Factory(false, "result", half, knowledgeExtraDocument: "activity-content-dependencies.json",
                knowledgeEdit: root => {
                    root.Get("knowledge_visibility_policies")[0].Set("rules", Json.Parse("[\"" + (hidden == "subject" ? "deps.hide-subject" : "recipe.hide-" + hidden) + "\"]"));
                    if (bad != null) root.Get("activities").Add(Json.Parse("{\"id\":\"deps.invalid\",\"player_orderable\":true,\"automatic_supply\":false,\"on_complete\":\"deps.invalid\",\"nodes\":[{\"id\":\"work\",\"executor\":\"work.staffed\",\"roles\":[\"inspect\"],\"duration\":{\"numerator\":1,\"denominator\":1,\"unit\":\"minute\"}}]}"));
                }, extraModules: new GameModule[] { module },
                extraDeclarations: "[{\"id\":\"proof.activity-content\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"activity-content-dependencies-module.json\"]}]",
                extraDocuments: new Dictionary<string, string> { ["activity-content-dependencies-module.json"] = document.ToString() });
        }
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
        private static Fix Stock(SimGame game, EntityId entity, ResourceId resource)
        {
            var code = game.Observer.Feature<LogisticsView>().ReadDiagnostics(entity, resource, out var value);
            if (!code.IsNone) throw new InvalidOperationException("actual activity stock/" + code);
            return value.Stock;
        }
        private static void Hidden(Check c, SimGame game, string plan, EntityId target, int order)
        {
            ulong hash = game.Hash(); var requirements = new List<WorkRequirementView>();
            c.Equal(Codes.UnknownDef, Work(game).PreviewOrderRequirements(plan, target, requirements), "transitive provider metadata withheld");
            c.That(requirements.Count == 0, "no partial provider requirement details");
            c.Equal(Codes.UnknownDef, game.Seat(0).Preview(new PostOrderCommand { Plan = plan, Target = target }), "normal intent uses same transitive closure");
            if (order != 0) c.That(!Work(game).TryReadProgress(order, out _) && !Work(game, 1).TryReadProgress(order, out _), "hidden and foreign progress withheld");
            c.Equal(hash, game.Hash(), "metadata traversal is nonmutating");
        }
        private static void Healthy(Check c, bool half, string kind, string hidden)
        {
            var module = new ActivityContentDependenciesModule(); var factory = Factory(half, hidden, module);
            var game = ConstructionPlanTests.NewPoints(factory); var idle = ConstructionPlanTests.NewPoints(factory); ulong idleHash = idle.Hash();
            c.That(module.ActualDirectAliases, "independent requirement and effect expose real selected aliases without native requirement inheritance");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "work and resource effects need no business module");
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current dependency-contract identity");
            string plan = "deps." + kind; var target = TransformationContentContractTests.Entity(game, "source");
            var worker = TransformationContentContractTests.Entity(game, "machine");
            c.That(game.Observer.TryFindResource("supply", out var resource), "actual observer resource identity");
            var limits = new ReplayLimits(6 * 1024 * 1024, 8000); using var recording = game.RecordReplay(limits);
            Hidden(c, game, plan, target, 0);
            int sequence = game.Seat(0).Submit(new PostOrderCommand { Plan = plan, Target = target }); TransformationContentContractTests.Step(game);
            long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
            c.Equal(Codes.UnknownDef, results.Single(x => x.Sequence == sequence).Refusal, "hidden command/preview parity");
            PermanentRemovalTests.Submit(c, game, new ConfigureWorkPolicyCommand { Worker = worker,
                Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) });
            PermanentRemovalTests.Submit(c, game, new PostOrderCommand { Plan = "recipe.discover", Target = target });
            Reach(game, null, () => Knows(game), "timed discovery of provider dependencies");
            var criteria = new List<WorkRequirementView>(); c.Equal(Code.None, Work(game).PreviewOrderRequirements(plan, target, criteria), "known actual references admit normal requirements preview");
            PermanentRemovalTests.Submit(c, game, new PostOrderCommand { Plan = plan, Target = target });
            var orders = new List<OrderView>(); game.Observer.Feature<WorkView>().CopyOrders(orders);
            int order = orders.Single(x => x.Plan == "activity:" + plan).Id;
            Reach(game, null, () => {
                var nodes = new List<ActivityNodeView>(); game.Observer.Feature<WorkView>().TryCopyNodes(order, nodes);
                return nodes.Any(x => x.State == NodeState.Working && !x.Progress.Equals(WorkProgress.Zero));
            }, "real positive-time activity execution");
            c.That(Work(game).TryReadProgress(order, out var retained), "known partial view detached");
            PermanentRemovalTests.Submit(c, game, new ForgetKnowledgeCommand { Policy = "deps.forget" });
            c.That(!Knows(game), "normal forgetting revokes inner dependency metadata"); Hidden(c, game, plan, target, order);
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "accepted hidden work and native commitments restored");
            game.Clock.Pause(); twin.Clock.Pause(); long at = game.Clock.StepIndex; ulong paused = game.Hash(); TransformationContentContractTests.Step(game, twin);
            c.That(game.Clock.StepIndex == at && game.Hash() == paused, "one clock pauses accepted hidden physical work"); game.Clock.Resume(); twin.Clock.Resume();
            var observer = game.Observer.Feature<WorkView>();
            Reach(game, twin, () => observer.TryReadCompanyActivityCompletion(new PlayerId(0), "activity:" + plan, target, out _), "hidden earned original completion");
            c.That(observer.TryReadCompanyActivityCompletion(new PlayerId(0), "activity:" + plan, target, out var fact) && fact.Order == order && fact.CompletedTick > fact.CreatedTick, "original earned history confirms same order and duration");
            c.That(!Work(game).TryReadActivityCompletion("activity:" + plan, target, out _), "private transitive history withheld");
            int expected = kind == "requirement" ? 18 : kind == "subject" ? 20 : 19;
            c.Equal(Fix.FromInt(expected), Stock(game, target, resource), "original resource transaction consumes destroys or produces exact authored quantities");
            c.Equal(Fix.Zero, Stock(game, worker, resource), "no duplicated material in worker cargo");
            c.That(retained.UnfinishedNodes > 0, "retained partial progress not rewritten");
            PermanentRemovalTests.Submit(c, game, new PostOrderCommand { Plan = "recipe.discover", Target = target });
            twin = factory.Load(game.Save()); Reach(game, twin, () => Knows(game), "physical re-discovery");
            c.That(Work(game).TryReadActivityCompletion("activity:" + plan, target, out var revealed) && revealed.Order == order, "re-discovery reveals the same earned result");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "settled dependency visibility and history durable");
            c.Equal(idleHash, idle.Hash(), "interleaved session stays isolated");
            c.That(new ReplayVerifier().Verify(factory, recording.Finish(), limits).Success, "ordinary input stream replays dependency disclosure and hidden physical execution");
        }
        private static void Invalid(Check c, string mode)
        {
            var module = new ActivityContentDependenciesModule(); Exception error = null;
            try { ConstructionPlanTests.NewPoints(Factory(false, "resource", module, mode)); } catch (Exception caught) { error = caught; }
            c.That(error is ContentException && module.Initialize.Initializations == 0, "invalid explicit dependencies rejected before initialization/" + mode);
            if (error == null) throw new InvalidOperationException("missing dependency metadata refusal/" + mode);
        }
        private static void Run(Check c)
        {
            foreach (bool half in new[] { false, true })
            {
                foreach (string kind in new[] { "requirement", "effect", "external" })
                    foreach (string hidden in new[] { "resource", "quantity", "capacity" }) Healthy(c, half, kind, hidden);
                Healthy(c, half, "subject", "subject");
            }
            foreach (string mode in new[] { "null", "duplicate", "foreign" }) Invalid(c, mode);
        }
    }
}
