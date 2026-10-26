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
    // JSON authoring, public simulated players and engine-owned observations only.
    internal static class WorkflowOperationAccessTests
    {
        internal const string Case = "Workflow operation access: hidden signal work obeys real conditions and earned private dispatch obeys real policy";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static readonly Code Capability = new Code("contract.process");
        private static EntityId Entity(SimGame g, string prototype) => TransformationContentContractTests.Entity(g, prototype);
        private static WorkView Work(SimGame g, int seat = 0) => g.Seat(seat).View.Feature<WorkView>();
        private static WorkView Observer(SimGame g) => g.Observer.Feature<WorkView>();
        private static bool Known(SimGame g) => g.Seat(0).View.Feature<KnowledgeView>()
            .TryRead(new PlayerId(0), "sim.knowledge.facts", "recipe.discovered", out _);
        private static GameFactory Factory(bool half, bool worker, bool number, string hidden, ContentAccessContractModule rules)
        {
            JsonValue processor = null;
            return RecipeDisclosureTests.Factory(false, "result", half,
                setupEdit: root => {
                    var actor = root.Get("entities")[1].Get("components").Get("transformer");
                    actor.Get("operation").Set("maximum", JsonValue.Number("2"));
                    actor.Get("operation").Set("target", JsonValue.Number(number ? "2" : "1"));
                    processor = actor.Clone(); processor.Set("automatic_supply", JsonValue.Of(false));
                    processor.Set("wip_capacity", JsonValue.Of("contract.carrier"));
                }, knowledgeExtraDocument: "workflow-operation-access.json",
                knowledgeEdit: root => root.Get("knowledge_visibility_policies")[0].Set("rules", Json.Parse("[\"" +
                    (hidden == "signal" ? number ? "wo.hide-target" : "wo.hide-enabled" : "recipe.hide-" + hidden) + "\"]")),
                extraModules: new GameModule[] { rules },
                extraDeclarations: "[{\"id\":\"proof.access-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.access\",\"version\":\"1.0.0\"}],\"documents\":[\"workflow-operation-rules.json\"]}]",
                extraDocuments: new Dictionary<string, string> { ["workflow-operation-rules.json"] = FrameworkProofContent.Read("workflow-operation-rules.json") },
                scenarioEdit: scenario => scenario.Get("settings").Get(WorkModule.ModuleId).Set("retry", JsonValue.Of("wo.retry")),
                worldEdit: root => { if (worker) {
                    var components = root.Get("entities")[2].Get("components");
                    components.Set("inventory_port", Json.Parse("[{\"id\":\"material.output\"},{\"id\":\"material.input\"},{\"id\":\"tool.input\"},{\"id\":\"operating.input\"}]"));
                    components.Set("transformer", processor.Clone());
                }});
        }
        private static void Reach(SimGame g, SimGame twin, Func<bool> done, string stage)
        {
            for (int i = 0; i < 512 && !done(); i++) TransformationContentContractTests.Step(g, twin);
            if (!done())
            {
                var orders = new List<OrderView>(); Observer(g).CopyOrders(orders);
                var jobs = new List<WorkJobView>(); Observer(g).CopyJobs(jobs);
                throw new InvalidOperationException(stage + "/clock=" + g.Clock.StepIndex + "/orders=" + string.Join(";", orders.Select(x => x.Id + "/" + x.Plan + "/" + x.State + "/" + x.Blocker)) +
                    "/jobs=" + string.Join(";", jobs.Select(x => x.Id + "/" + x.State + "/" + x.Blocker)));
            }
        }
        private static OrderView Order(SimGame g, int id)
        { var rows = new List<OrderView>(); Observer(g).CopyOrders(rows); return rows.Single(x => x.Id == id); }
        private static ActivityNodeView Node(SimGame g, int id)
        { var rows = new List<ActivityNodeView>(); Observer(g).TryCopyNodes(id, rows); return rows.Single(); }
        private static int Begin(Check c, SimGame g, string plan, EntityId target)
        {
            PermanentRemovalTests.Submit(c, g, new PostOrderCommand { Plan = plan, Target = target });
            var rows = new List<OrderView>(); Observer(g).CopyOrders(rows); int id = rows.Single(x => x.Plan == "activity:" + plan).Id;
            Reach(g, null, () => Node(g, id).State == NodeState.Working && !Node(g, id).Progress.Equals(WorkProgress.Zero), "positive-time accepted work"); return id;
        }
        private static void Discover(Check c, SimGame g)
        {
            PermanentRemovalTests.Submit(c, g, new PostOrderCommand { Plan = "recipe.discover", Target = Entity(g, "source") });
            Reach(g, null, () => Known(g), "ordinary physical discovery");
        }
        private static void Enable(Check c, SimGame g)
            => PermanentRemovalTests.Submit(c, g, new ConfigureWorkPolicyCommand { Worker = Entity(g, "machine"),
                Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) });
        private static void Hidden(Check c, SimGame g, string plan, EntityId target, int order)
        {
            ulong hash = g.Hash(); var criteria = new List<WorkRequirementView>();
            c.Equal(Codes.UnknownDef, Work(g).PreviewOrderRequirements(plan, target, criteria), "whole workflow metadata private");
            c.That(criteria.Count == 0, "no partial requirement prefix");
            c.Equal(Codes.UnknownDef, g.Seat(0).Preview(new PostOrderCommand { Plan = plan, Target = target }), "new hidden player intent still refused");
            if (order != 0) c.That(!Work(g).TryReadProgress(order, out _) && !Work(g, 1).TryReadProgress(order, out _), "hidden and foreign progress withheld");
            c.Equal(hash, g.Hash(), "private observation does not change physical state");
        }
        private static bool Completed(SimGame g, string plan, EntityId target)
            => Observer(g).TryReadCompanyActivityCompletion(new PlayerId(0), "activity:" + plan, target, out _);
        private static void Control(Check c, SimGame g, EntityId source, bool enabled, int target)
            => PermanentRemovalTests.Submit(c, g, new ConfigureOperationCommand { Entity = source, Operation = Capability, Enabled = enabled, Target = Fix.FromInt(target) });
        private static void Signal(Check c, bool half, bool worker, bool number)
        {
            var rules = new ContentAccessContractModule(); var f = Factory(half, worker, number, "signal", rules);
            var g = ConstructionPlanTests.NewPoints(f); var idle = ConstructionPlanTests.NewPoints(f); ulong idleHash = idle.Hash();
            var limits = new ReplayLimits(6 * 1024 * 1024, 9000); using var recording = g.RecordReplay(limits);
            string plan = "wo." + (worker ? "worker-" : "target-") + (number ? "number" : "bool");
            var target = Entity(g, "contract.factory"); var source = worker ? Entity(g, "machine") : target;
            c.That(!g.Modules.Contains(TycoonModule.ModuleId), "signal work requires no business module");
            Hidden(c, g, plan, target, 0); Enable(c, g); Discover(c, g);
            int order = Begin(c, g, plan, target); c.That(Work(g).TryReadProgress(order, out var retained), "known actual partial view");
            Control(c, g, source, number, 1);
            var stopped = Node(g, order).Progress;
            PermanentRemovalTests.Submit(c, g, new ForgetKnowledgeCommand { Policy = "wo.forget" }); Hidden(c, g, plan, target, order);
            var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "real stopped hidden work restored");
            for (int i = 0; i < 3; i++) TransformationContentContractTests.Step(g, twin);
            c.That(Node(g, order).Progress.Equals(stopped) && !Completed(g, plan, target), "hidden but genuinely unsatisfied condition earns no work");
            var jobs = new List<WorkJobView>(); Observer(g).CopyJobs(jobs);
            c.That(jobs.Any(x => x.Blocker == new Code("wo.signal-unready")) || Node(g, order).Blocker == new Code("wo.signal-unready"), "actual authored condition blocker, not metadata UnknownDef");
            c.Equal(Codes.UnknownDef, g.Seat(0).View.Feature<OperationsView>().ReadSignal(source, Capability, number ? "wo.target" : "wo.enabled", out _), "signal projection remains private");
            Control(c, g, source, true, number ? 2 : 1); twin = f.Load(g.Save());
            g.Clock.Pause(); twin.Clock.Pause(); long at = g.Clock.StepIndex; ulong paused = g.Hash(); TransformationContentContractTests.Step(g, twin);
            c.That(g.Clock.StepIndex == at && g.Hash() == paused, "sole clock pauses hidden signal work"); g.Clock.Resume(); twin.Clock.Resume();
            Reach(g, twin, () => Completed(g, plan, target), "real condition restoration resumes earned hidden work");
            c.That(!Work(g).TryReadActivityCompletion("activity:" + plan, target, out _), "hidden history stays private");
            c.That(retained.UnfinishedNodes > 0, "retained partial view stays detached");
            Discover(c, g); c.That(Work(g).TryReadActivityCompletion("activity:" + plan, target, out var fact) && fact.Order == order && fact.CompletedTick > fact.CreatedTick, "re-discovery reveals same positive-time completion");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "completed signal workflow durable"); c.Equal(idleHash, idle.Hash(), "signal sessions isolated");
            c.That(new ReplayVerifier().Verify(f, recording.Finish(), limits).Success, "normal hidden signal control and work replay");
        }
        private static TransformationSnapshot Process(SimGame g)
        {
            if (!g.Observer.Feature<TransformationManagementView>().TryGetTransformation(Entity(g, "contract.factory"), Capability, out var value))
                throw new InvalidOperationException("actual dispatch processor unavailable"); return value;
        }
        private static Fix Stock(SimGame g, string prototype)
        {
            if (!g.Observer.TryFindResource("supply", out var resource)) throw new InvalidOperationException("actual resource missing");
            var code = g.Observer.Feature<LogisticsView>().ReadDiagnostics(Entity(g, prototype), resource, out var value);
            if (!code.IsNone) throw new InvalidOperationException("actual stock/" + code); return value.Stock;
        }
        private static void Rule(Check c, SimGame g, string id, bool enabled)
            => PermanentRemovalTests.Submit(c, g, new ConfigureAccessRuleCommand { Rule = id, Enabled = enabled });
        private static void Dispatch(Check c, bool half, string hidden, bool restricted)
        {
            var rules = new ContentAccessContractModule(); var f = Factory(half, false, false, hidden, rules);
            var g = ConstructionPlanTests.NewPoints(f); var idle = ConstructionPlanTests.NewPoints(f); ulong idleHash = idle.Hash();
            var limits = new ReplayLimits(6 * 1024 * 1024, 9000); using var recording = g.RecordReplay(limits);
            string plan = "wo.manufacture"; var target = Entity(g, "contract.factory");
            c.That(rules.ActualAlias && rules.RuleAliases && !g.Modules.Contains(TycoonModule.ModuleId), "ordinary actual access rules without business module");
            Hidden(c, g, plan, target, 0); Enable(c, g); Discover(c, g); int order = Begin(c, g, plan, target);
            c.That(Work(g).TryReadProgress(order, out var retained) && Process(g).QueueCount == 0, "accepted work never manufactures or queues prematurely");
            if (restricted) { Rule(c, g, "proof.access.b", true); Rule(c, g, "proof.access.a", true); }
            PermanentRemovalTests.Submit(c, g, new ForgetKnowledgeCommand { Policy = "wo.forget" }); Hidden(c, g, plan, target, order);
            var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "private accepted dispatch restores");
            if (restricted)
            {
                var a = new Code("proof.access.a.denied"); var b = new Code("proof.access.b.denied");
                Reach(g, twin, () => Order(g, order).State == OrderState.Completing && Order(g, order).Blocker == a, "earned dispatch waits for genuine rule A");
                c.That(Order(g, order).CompletionFailures > 0 && Process(g).QueueCount == 0 && Stock(g, "source") == Fix.FromInt(20), "real rule refusal preserves queue/materials and durable retry");
                Rule(c, g, "proof.access.a", false); twin = f.Load(g.Save()); Reach(g, twin, () => Order(g, order).Blocker == b, "real rule B still blocks private dispatch");
                c.That(Process(g).QueueCount == 0, "operational admission does not waive remaining policy"); Rule(c, g, "proof.access.b", false); twin = f.Load(g.Save());
            }
            Reach(g, twin, () => Completed(g, plan, target), "earned hidden dispatch enqueues physical manufacturing");
            c.That(Process(g).QueueCount == 1 && Process(g).Progress.Equals(WorkProgress.Zero) && !g.Snapshot.Entities.Any(x => x.Def == "contract.result"), "earned enqueue creates no instantaneous result or same-update processing");
            c.That(!Work(g).TryReadActivityCompletion("activity:" + plan, target, out _) && retained.UnfinishedNodes > 0, "hidden history private and old progress detached");
            Reach(g, twin, () => g.Snapshot.Entities.Any(x => x.Def == "contract.result"), "physical supplied timed production after hidden dispatch");
            c.That(Process(g).QueueCount == 0 && g.Snapshot.Entities.Count(x => x.Def == "contract.result") == 1, "one earned dispatch and manufactured result");
            c.That(Stock(g, "source") == Fix.FromInt(16) && Stock(g, "contract.factory") == Fix.One && Stock(g, "machine") == Fix.Zero, "real hauled four units consume three retain tool one and no cargo duplication");
            Discover(c, g); c.That(Work(g).TryReadActivityCompletion("activity:" + plan, target, out var fact) && fact.Order == order && fact.CompletedTick > fact.CreatedTick, "re-discovery reveals original dispatch completion");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "manufacturing and earned dispatch durable"); c.Equal(idleHash, idle.Hash(), "private dispatch sessions isolated");
            c.That(new ReplayVerifier().Verify(f, recording.Finish(), limits).Success, "normal private dispatch real rule retries and physical manufacture replay");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current operational workflow identity");
            foreach (bool half in new[] { false, true })
            {
                foreach (bool worker in new[] { false, true }) foreach (bool number in new[] { false, true }) Signal(c, half, worker, number);
                foreach (string hidden in new[] { "result", "resource", "quantity" }) foreach (bool restricted in new[] { false, true }) Dispatch(c, half, hidden, restricted);
            }
        }
    }
}
