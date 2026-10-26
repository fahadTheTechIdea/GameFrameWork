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
    // Explicit authored modules/JSON and normal public players only.
    internal static class UpgradeOperationAccessTests
    {
        internal const string Case = "Upgrade operation access: hidden accepted replacement obeys real ordered restrictions and saved physical retry";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(bool half, ContentAccessContractModule rules)
            => RecipeDisclosureTests.Factory(false, "result", half, knowledgeExtraDocument: "upgrade-operation-access.json",
                knowledgeEdit: root => root.Get("knowledge_visibility_policies")[0].Set("rules", Json.Parse("[\"operating.hide-upgrade\"]")),
                extraModules: new GameModule[] { rules },
                extraDeclarations: "[{\"id\":\"proof.access-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.access\",\"version\":\"1.0.0\"}],\"documents\":[\"upgrade-operation-rules.json\"]}]",
                extraDocuments: new Dictionary<string, string> { ["upgrade-operation-rules.json"] = FrameworkProofContent.Read("upgrade-operation-rules.json") },
                scenarioEdit: scenario => scenario.Get("settings").Get(WorkModule.ModuleId).Set("retry", JsonValue.Of("operating.retry")));
        private static bool Exists(SimGame g, string prototype) => g.Snapshot.Entities.Any(x => x.Def == prototype);
        private static EntityId Entity(SimGame g, string prototype) => TransformationContentContractTests.Entity(g, prototype);
        private static WorkView View(SimGame g, int seat = 0) => g.Seat(seat).View.Feature<WorkView>();
        private static bool Known(SimGame g) => g.Seat(0).View.Feature<KnowledgeView>().TryRead(new PlayerId(0), "sim.knowledge.facts", "recipe.discovered", out _);
        private static OrderView Order(SimGame g, int id)
        { var rows = new List<OrderView>(); g.Observer.Feature<WorkView>().CopyOrders(rows); return rows.Single(x => x.Id == id); }
        private static Fix Stock(SimGame g, EntityId id, ResourceId r)
        { var code = g.Observer.Feature<LogisticsView>().ReadDiagnostics(id, r, out var value); if (!code.IsNone) throw new InvalidOperationException("actual upgrade stock/" + code); return value.Stock; }
        private static void Reach(SimGame g, SimGame twin, Func<bool> done, string stage)
        {
            for (int i = 0; i < 512 && !done(); i++) TransformationContentContractTests.Step(g, twin);
            if (!done())
            {
                var rows = new List<OrderView>(); g.Observer.Feature<WorkView>().CopyOrders(rows);
                var jobs = new List<WorkJobView>(); g.Observer.Feature<WorkView>().CopyJobs(jobs);
                throw new InvalidOperationException(stage + "/clock=" + g.Clock.StepIndex + "/orders=" + string.Join(";", rows.Select(x => x.Id + "/" + x.Plan + "/" + x.State + "/" + x.Blocker)) +
                    "/jobs=" + string.Join(";", jobs.Select(x => x.Id + "/" + x.State + "/" + x.Blocker)));
            }
        }
        private static ConfigureAccessRuleCommand Rule(string id, bool enabled) => new ConfigureAccessRuleCommand { Rule = id, Enabled = enabled };
        private static void Hidden(Check c, SimGame g, EntityId tower, int order)
        {
            ulong pure = g.Hash();
            c.Equal(Codes.UnknownDef, g.Seat(0).Preview(new PostOrderCommand { Plan = "operating.upgrade", Target = tower }), "new upgrade intent remains metadata restricted");
            c.That(!View(g).TryReadProgress(order, out _) && !View(g, 1).TryReadProgress(order, out _), "hidden and foreign progress remains private");
            c.Equal(pure, g.Hash(), "private upgrade observation pure");
        }
        private static void Healthy(Check c, bool half, bool restricted)
        {
            var rules = new ContentAccessContractModule(); var f = Factory(half, rules);
            var g = ConstructionPlanTests.NewPoints(f); var idle = ConstructionPlanTests.NewPoints(f); ulong isolated = idle.Hash();
            c.That(rules.ActualAlias && rules.RuleAliases && rules.DirectDefinition, "one actual authored access basis and two independent saved rule aliases");
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current operating replacement identity");
            c.That(!g.Modules.Contains(TycoonModule.ModuleId), "real construction and upgrade need no business module");
            var source = Entity(g, "source"); var machine = Entity(g, "machine");
            c.That(g.Seat(0).View.TryFindResource("supply", out var resource), "actual shared resource");
            var limits = new ReplayLimits(6 * 1024 * 1024, 8000); using var recording = g.RecordReplay(limits);
            PermanentRemovalTests.Submit(c, g, new ConfigureWorkPolicyCommand { Worker = machine,
                Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) });
            PermanentRemovalTests.Submit(c, g, new ConstructCommand { Def = "point.tower", Placement = Json.Parse("{\"space\":\"field.points\",\"position\":12}"), Priority = 70 });
            Reach(g, null, () => Exists(g, "point.tower"), "ordinary material-supplied initial construction");
            var tower = Entity(g, "point.tower"); c.Equal(Fix.FromInt(12), Stock(g, source, resource), "construction consumed eight real units");
            PermanentRemovalTests.Submit(c, g, new PostOrderCommand { Plan = "recipe.discover", Target = source });
            Reach(g, null, () => Known(g), "physical upgrade knowledge discovery");
            PermanentRemovalTests.Submit(c, g, new PostOrderCommand { Plan = "operating.upgrade", Target = tower });
            var orders = new List<OrderView>(); g.Observer.Feature<WorkView>().CopyOrders(orders);
            int order = orders.Single(x => x.Plan == "activity:operating.upgrade").Id;
            Reach(g, null, () => {
                var nodes = new List<ActivityNodeView>(); g.Observer.Feature<WorkView>().TryCopyNodes(order, nodes);
                return nodes.Any(x => x.Id == "upgrade" && x.State == NodeState.Working && !x.Progress.Equals(WorkProgress.Zero));
            }, "physically supplied positive-time upgrade");
            c.That(View(g).TryReadProgress(order, out var retained), "known actual progress visible");
            c.That(!Exists(g, "operating.enhanced") && Exists(g, "point.tower"), "accepted work does not replace prematurely");
            if (restricted)
            {
                PermanentRemovalTests.Submit(c, g, Rule("proof.access.b", true));
                PermanentRemovalTests.Submit(c, g, Rule("proof.access.a", true));
            }
            PermanentRemovalTests.Submit(c, g, new ForgetKnowledgeCommand { Policy = "recipe.forget" });
            c.That(!Known(g), "normal forgetting revokes prototype metadata"); Hidden(c, g, tower, order);
            var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "hidden physical work and actual rules saved");
            if (restricted)
            {
                var a = new Code("proof.access.a.denied"); var b = new Code("proof.access.b.denied");
                Reach(g, twin, () => Order(g, order).State == OrderState.Completing && Order(g, order).Blocker == a, "real ordered access rule A blocks earned replacement");
                var waiting = Order(g, order);
                c.That(waiting.CompletionFailures > 0 && !waiting.CompletionEffectApplied && waiting.CompletionRetryRemainingTicks > 0, "original authored retry records physical refusal");
                c.That(Exists(g, "point.tower") && !Exists(g, "operating.enhanced"), "actual restriction prevents replacement despite earned work");
                c.Equal(Fix.FromInt(10), Stock(g, source, resource), "upgrade consumed two actual materials once before blocked completion");
                c.Equal(Fix.Zero, Stock(g, tower, resource), "blocked replacement did not copy consumed materials");
                PermanentRemovalTests.Submit(c, g, Rule("proof.access.a", false)); twin = f.Load(g.Save());
                Reach(g, twin, () => Order(g, order).Blocker == b, "remaining real access rule B still blocks");
                c.That(!Order(g, order).CompletionEffectApplied && !Exists(g, "operating.enhanced"), "removing first restriction is not blanket operating access");
                c.Equal(Fix.FromInt(10), Stock(g, source, resource), "retries never charge materials again");
                PermanentRemovalTests.Submit(c, g, Rule("proof.access.b", false)); twin = f.Load(g.Save());
            }
            Reach(g, twin, () => Exists(g, "operating.enhanced"), "accepted hidden replacement completes after real eligibility");
            var made = Entity(g, "operating.enhanced");
            c.That(made != tower && !Exists(g, "point.tower") && g.Snapshot.Entities.Count(x => x.Def == "operating.enhanced") == 1, "one original coordinator replaces actual identity");
            var actual = g.Snapshot.Entities.Single(x => x.Id == made);
            c.That(actual.Owner == new PlayerId(0) && ((PointSnapshot)actual.Spatial).Position == Fix.FromInt(12), "owner and real position preserved, not prototype position99");
            c.Equal(Fix.FromInt(10), Stock(g, source, resource), "build eight plus upgrade two consumed exactly once");
            c.Equal(Fix.Zero, Stock(g, made, resource), "replacement owns no fabricated stock");
            c.Equal(Fix.Zero, Stock(g, machine, resource), "actual timed cargo delivered exactly once");
            c.That(g.Observer.Feature<WorkView>().TryReadCompanyActivityCompletion(new PlayerId(0), "activity:operating.upgrade", EntityId.None, out var completion) &&
                completion.Order == order && completion.CompletedTick > completion.CreatedTick, "actual earned history confirms replacement, not order disappearance");
            c.That(!View(g).TryReadCompanyActivityCompletion(new PlayerId(0), "activity:operating.upgrade", EntityId.None, out _), "hidden completion history remains private");
            c.That(retained.UnfinishedNodes > 0, "retained immutable partial progress not rewritten");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "completed replacement/rules/history durable"); c.Equal(isolated, idle.Hash(), "interleaved session isolated");
            c.That(new ReplayVerifier().Verify(f, recording.Finish(), limits).Success, "normal build/discovery/upgrade/revocation/rule retry stream replays");
        }
        private static void Run(Check c) { foreach (bool half in new[] { false, true }) foreach (bool restricted in new[] { false, true }) Healthy(c, half, restricted); }
    }
}

