using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    public static partial class PlanTemplateTests
    {
        private static GameFactory Dependencies(Action<JsonValue> edit = null, bool failed = false, Action<JsonValue> constructionEdit = null, string recovery = "point.recovery",
            IReadOnlyList<GameModule> extraModules = null, string extraDeclarations = null, IReadOnlyDictionary<string, string> extraDocuments = null, Action<JsonValue> scenarioEdit = null)
        {
            JsonValue root = Content.Json.Parse(FrameworkProofContent.Read("plan-dependencies.json")); edit?.Invoke(root);
            return ConstructionPlanTests.Points(authored => {
                authored.Get("target_resource_requirements")[0].Get("quantities").Set("supply", JsonValue.Number("4"));
                JsonValue nodes = authored.Get("activities")[0].Get("nodes");
                nodes[0].Set("duration", Content.Json.Parse("{\"numerator\":1,\"denominator\":5,\"unit\":\"hour\"}")); nodes[1].Set("duration", Content.Json.Parse("{\"numerator\":3,\"denominator\":10,\"unit\":\"hour\"}"));
                if (failed) nodes[1].Set("effect", JsonValue.Of("dependency.fail"));
                constructionEdit?.Invoke(authored);
            }, recovery: recovery, additionalModule: new CompletionEffectModule(),
                moduleDeclaration: "{\"id\":\"proof.completion\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"plan-dependencies.json\"]}",
                moduleDocument: root.ToString(), extraModules: extraModules, extraDeclarations: extraDeclarations, extraDocuments: extraDocuments,
                scenarioEdit: scenario => { scenario.Get("modules").Add(JsonValue.Of("proof.completion")); scenarioEdit?.Invoke(scenario); });
        }
        private static PlanIntent DependencyBuild(SimGame game, int x, string group, params long[] prerequisites)
            => DependencyBuild(At(game, x), group, prerequisites);
        internal static PlanIntent DependencyBuild(JsonValue placement, string group, params long[] prerequisites)
        {
            JsonValue arguments = JsonValue.NewObject(); arguments.Set("placement", placement);
            return new PlanIntent("build:point.tower", arguments, group, 60, true, prerequisites);
        }
        private static long DependencySubmit(Check c, SimGame game, PlanIntent intent, int seat = 0)
        { var result = TestKit.Order(game, seat, new SubmitPlanCommand { Intent = intent }); c.That(result.Accepted, "ordinary linked plan submission"); return result.Arg0; }
        private static void DependencyEdit(Check c, SimGame game, string group, bool paused)
        { c.That(TestKit.Order(game, 0, new ConfigurePlanGroupCommand { Group = group, Priority = 70, Paused = paused }).Accepted, "ordinary graph group control"); }
        private static void CorruptDependencies(Check c, SimGame game, long id, IReadOnlyList<long> dependencies, string message)
        {
            var entries = (IEnumerable)typeof(WorkBoard).GetField("_plans", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(game.World.Services.Get<WorkBoard>());
            foreach (object entry in entries)
                if ((long)entry.GetType().GetField("Id", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(entry) == id)
                {
                    FieldInfo field = entry.GetType().GetField("Dependencies", BindingFlags.NonPublic | BindingFlags.Instance); object old = field.GetValue(entry);
                    field.SetValue(entry, dependencies); try { c.Throws<CorruptStateException>(() => game.Save(), message); } finally { field.SetValue(entry, old); }
                    return;
                }
            throw new InvalidOperationException("missing instrumented plan");
        }
        private static byte[] DependencyWire(Command command)
        {
            using (var stream = new MemoryStream())
            { var writer = new StateWriter(stream); command.Sync(writer); writer.Flush(); return stream.ToArray(); }
        }
        private static void PlanDependencyContracts(Check c)
        {
            GameFactory factory = Dependencies(); SimGame game = ConstructionPlanTests.NewPoints(factory);
            PlanTemplate template = View(game).AuthoredPlanTemplate("dependency.diamond");
            c.That(game.World.Content.Get<PlanTemplateDef>("dependency.diamond").AuthoringPath.Contains("plan-dependencies.json"), "graph is explicitly master-listed JSON, not harness scheduling");
            c.Equal(2, template.Entries[0].Dependencies.Count, "forward fan-in uses explicit stable entry IDs");
            c.Equal("prepare", template.Entries[1].Dependencies[0], "forward prerequisite is not inferred from list order");
            c.Equal(template.ToJson().ToString(), Read(Content.Json.Parse(template.ToJson().ToString())).ToJson().ToString(), "one strict dependency JSON round trip");
            c.Throws<NotSupportedException>(() => ((IList<string>)template.Entries[0].Dependencies).Add("invented"), "portable dependency list is immutable");
            foreach (Action<JsonValue> bad in new Action<JsonValue>[] {
                root => root.Get("plan_templates")[0].Get("entries")[0].Remove("after"),
                root => root.Get("plan_templates")[0].Get("entries")[0].Set("after", JsonValue.Null()),
                root => root.Get("plan_templates")[0].Get("entries")[0].Set("after", Content.Json.Parse("[1]")),
                root => root.Get("plan_templates")[0].Get("entries")[0].Set("after", Content.Json.Parse("[\"missing\"]")),
                root => root.Get("plan_templates")[0].Get("entries")[0].Set("after", Content.Json.Parse("[\"finish\"]")),
                root => root.Get("plan_templates")[0].Get("entries")[0].Set("after", Content.Json.Parse("[\"left\",\"left\"]")),
                root => root.Get("plan_templates")[0].Get("entries")[3].Set("after", Content.Json.Parse("[\"finish\"]")) })
                c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(Dependencies(bad)), "invalid authored topology fails before Initialize");
            PlanIntent first = DependencyBuild(game, 60, "authoring"), second = DependencyBuild(game, 64, "authoring");
            c.Throws<ArgumentException>(() => DependencyBuild(game, 60, "authoring", 0), "zero is not an actual prerequisite ID");
            c.Throws<ArgumentException>(() => DependencyBuild(game, 60, "authoring", 1, 1), "duplicate actual IDs refuse");
            var sorted = DependencyBuild(game, 60, "authoring", 7, 2); c.That(sorted.Dependencies[0] == 2 && sorted.Dependencies[1] == 7, "actual prerequisite encoding is canonical");
            foreach (PlanGroupDependency[] bad in new[] {
                new[] { new PlanGroupDependency(0, 1), new PlanGroupDependency(1, 0) },
                new[] { new PlanGroupDependency(0, 1), new PlanGroupDependency(0, 1) },
                new[] { new PlanGroupDependency(2, 0) }, new[] { default(PlanGroupDependency) } })
                c.Throws<ArgumentException>(() => new PlanGroupIntent("authoring", new[] { first, second }, bad), "group topology uses sole compiler before any allocation");
            var wireGroup = new SubmitPlanGroupCommand { Intent = new PlanGroupIntent("authoring", new[] { first, second }, new[] { new PlanGroupDependency(0, 1) }) };
            byte[] corrupt = DependencyWire(wireGroup); Array.Clear(corrupt, corrupt.Length - sizeof(int), sizeof(int));
            c.Throws<CorruptStateException>(() => new SubmitPlanGroupCommand().Sync(new StateReader(corrupt, 0, corrupt.Length)), "self-cycle queued group is typed corruption, not normalized input");
            corrupt = DependencyWire(new SubmitPlanCommand { Intent = sorted }); Array.Copy(corrupt, corrupt.Length - 2 * sizeof(long), corrupt, corrupt.Length - sizeof(long), sizeof(long));
            c.Throws<CorruptStateException>(() => new SubmitPlanCommand().Sync(new StateReader(corrupt, 0, corrupt.Length)), "duplicate queued actual IDs cannot normalize into a valid command");
            var third = DependencyBuild(game, 68, "authoring");
            var canonicalA = new SubmitPlanGroupCommand { Intent = new PlanGroupIntent("authoring", new[] { first, second, third }, new[] { new PlanGroupDependency(1, 2), new PlanGroupDependency(0, 2) }) };
            var canonicalB = new SubmitPlanGroupCommand { Intent = new PlanGroupIntent("authoring", new[] { first, second, third }, new[] { new PlanGroupDependency(0, 2), new PlanGroupDependency(1, 2) }) };
            c.Equal(Convert.ToBase64String(DependencyWire(canonicalA)), Convert.ToBase64String(DependencyWire(canonicalB)), "shuffled authoring links produce one canonical command payload");
            var missing = DependencyBuild(game, 60, "missing", long.MaxValue); ulong hash = game.Hash();
            c.Equal(Codes.NotFound, View(game).PreviewPlan(missing), "missing real ID is not an invented future plan"); c.Equal(hash, game.Hash(), "dependency preview is pure");
            c.Equal(Codes.NotFound, TestKit.Order(game, 0, new SubmitPlanCommand { Intent = missing }).Refusal, "missing prerequisite command matches preview");
            long foreign = DependencySubmit(c, game, DependencyBuild(game, 80, "foreign"), 1);
            var privateIntent = DependencyBuild(game, 60, "private", foreign);
            c.Equal(Codes.NotOwned, View(game).PreviewPlan(privateIntent), "foreign actual prerequisite cannot control this company");
            c.Equal(Codes.NotOwned, TestKit.Order(game, 0, new SubmitPlanCommand { Intent = privateIntent }).Refusal, "foreign prerequisite command parity");
            var privateRows = new List<PlanView>(); View(game).CopyPlans(privateRows); c.Equal(0, privateRows.Count, "foreign graph facts are not disclosed by own plan list");
            long predecessor = DependencySubmit(c, game, DependencyBuild(game, 60, "predecessor"));
            var links = new List<long> { predecessor }; var linked = new PlanIntent(first.Definition, second.Arguments, "successor", 90, false, links); links.Clear();
            long successor = DependencySubmit(c, game, linked); c.Equal(predecessor, TemplatePlan(game, successor).Dependencies[0], "input links are captured, not aliased to caller list");
            c.That(TemplatePlan(game, successor).Order == 0 && TemplatePlan(game, successor).Blocker == PlanCodes.DependencyPending, "blocked successor has no native order or target");
            c.Equal(PlanCodes.ExternalDependencies, View(game).CapturePlanTemplate("successor", At(game, 0), out _).Blocker, "portable capture never silently drops external prerequisite IDs");
            CorruptDependencies(c, game, successor, new[] { foreign }, "foreign saved link refuses");
            CorruptDependencies(c, game, successor, new[] { long.MaxValue }, "missing saved link refuses");
            CorruptDependencies(c, game, successor, new[] { successor }, "self-cycle saved link refuses");
            CorruptDependencies(c, game, predecessor, new[] { successor }, "multi-plan cycle uses sole dependency compiler");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "actual stable dependency IDs save through existing board");
            c.That(TestKit.Order(game, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { predecessor }) }).Accepted, "cancel actual predecessor");
            c.That(TemplatePlan(game, successor).State == PlanState.Pending && TemplatePlan(game, successor).Blocker == PlanCodes.DependencyCancelled, "cancellation is not prerequisite completion or implicit descendant cancellation");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled-prerequisite blocker derives from saved actual history");
            c.That(TestKit.Order(game, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { successor }) }).Accepted, "explicit player can retire blocked descendant");
            var repeated = TestKit.Order(game, 0, new RepeatPlanCommand { Plan = successor }); c.That(repeated.Accepted, "single repeat retains explicit prerequisites");
            c.Equal(predecessor, TemplatePlan(game, repeated.Arg0).Dependencies[0], "single repeat does not fabricate successful predecessor");
            c.Equal(PlanCodes.DependencyCancelled, TemplatePlan(game, repeated.Arg0).Blocker, "repeating intent never clears cancelled dependency");
            var intent = new PlanGroupIntent("inbox", new[] { DependencyBuild(game, 30, "inbox"), DependencyBuild(game, 34, "inbox") }, new[] { new PlanGroupDependency(0, 1) });
            game.Seat(0).Submit(new SubmitPlanGroupCommand { Intent = intent }); SimGame twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            var inbox = Group(game, "inbox"); c.That(inbox.Count == 2 && inbox[0].Dependencies[0] == inbox[1].Id, "saved whole group resolves forward positional link to actual future member ID");
            c.Equal(game.Hash(), twin.Hash(), "group command codec owns links and saved continuation");
            c.That(TestKit.Order(game, 0, new ConfigurePlanCommand { Plan = inbox[1].Id, Group = "outside", Priority = 1, Paused = true }).Accepted, "regroup prerequisite without changing dependent edge");
            c.That(TestKit.Order(game, 0, new CancelPlanGroupCommand { Group = "inbox" }).Accepted, "retire only current one-member group");
            var subset = TestKit.Order(game, 0, new RepeatPlanGroupCommand { Group = "inbox", NewGroup = "subset" });
            c.That(subset.Accepted && subset.Arg1 == 1, "repeating current membership does not invent missing external members");
            c.That(Group(game, "subset")[0].Dependencies[0] == inbox[1].Id && Group(game, "subset")[0].Blocker == PlanCodes.DependencyPending,
                "subset repeat preserves real external prerequisite rather than treating it as a new local member");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "regroup/subset repetition retains one actual graph codec");
        }
        private static void PlanDependencyPhysical(Check c)
        {
            GameFactory factory = Dependencies(); SimGame game = ConstructionPlanTests.NewPoints(factory); EmergencyPreemptionTests.Configure(c, game);
            PlanTemplate template = View(game).AuthoredPlanTemplate("dependency.diamond");
            game.Seat(0).Submit(new PlacePlanTemplateCommand { Selection = PlanTemplateSelection.Snapshot(template), Anchor = At(game, 12), Group = "diamond" });
            SimGame twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            var rows = Group(game, "diamond"); long finish = rows[0].Id, left = rows[1].Id, right = rows[2].Id, root = rows[3].Id;
            c.That(rows.TrueForAll(p => p.Order == 0 && p.Target == EntityId.None), "paused root blocks all higher-priority physical site creation");
            c.That(rows[0].Blocker == PlanCodes.DependencyPending && rows[1].Dependencies[0] == root && rows[2].Dependencies[0] == root, "fan-out and fan-in resolve real forward IDs");
            c.Equal(game.Hash(), twin.Hash(), "saved portable graph command remains identical");
            c.That(View(game).CapturePlanTemplate("diamond", At(game, 12), out PlanTemplate captured).Ready, "whole DAG capture keeps relative geometry and internal dependencies");
            c.Equal(2, captured.Entries[0].Dependencies.Count, "capture keeps every fan-in edge");
            c.That(Place(game, captured, 60, "portable").Accepted, "portable graph maps to fresh actual IDs, not original workers/parents");
            var portable = Group(game, "portable"); c.That(portable[0].Dependencies[0] == portable[1].Id && portable[1].Dependencies[0] == portable[3].Id, "template translated graph does not reference original cycle");
            c.That(TestKit.Order(game, 0, new CancelPlanGroupCommand { Group = "portable" }).Accepted, "extra captured intent explicitly retired without free work");
            // Restore both from this checkpoint so later comparisons include the same actual history.
            twin = factory.Load(game.Save()); DependencyEdit(c, game, "diamond", false); DependencyEdit(c, twin, "diamond", false);
            c.That(TemplatePlan(game, root).State == PlanState.Active && TemplatePlan(game, left).Order == 0 && TemplatePlan(game, right).Order == 0 && TemplatePlan(game, finish).Order == 0, "only genuine ready frontier admits despite priorities/list order");
            c.That(TestKit.Order(game, 0, new MovePlanCommand { Plan = finish, Before = left }).Accepted, "pending successor can reorder independently of actual prerequisite links");
            c.That(TestKit.Order(twin, 0, new MovePlanCommand { Plan = finish, Before = left }).Accepted, "twin uses same ordinary reorder command");
            Until(game, () => game.World.Services.Get<WorkBoard>().Order(TemplatePlan(game, root).Order)?.Node("build").Progress.WholeTicks > 0, "diamond", twin);
            DependencyEdit(c, game, "diamond", true); twin = factory.Load(game.Save());
            WorkProgress progress = game.World.Services.Get<WorkBoard>().Order(TemplatePlan(game, root).Order).Node("build").Progress;
            TemplateAdvance(game, 5, twin); c.Equal(game.Hash(), twin.Hash(), "saved partial frontier preserves actual resources/machine state");
            c.That(game.World.Services.Get<WorkBoard>().Order(TemplatePlan(game, root).Order).Node("build").Progress.Equals(progress), "paused graph earns no work through another timer");
            DependencyEdit(c, game, "diamond", false); DependencyEdit(c, twin, "diamond", false);
            Until(game, () => TemplatePlan(game, root).State == PlanState.Done, "diamond", twin);
            c.That(TemplatePlan(game, left).Order == 0 && TemplatePlan(game, right).Order == 0, "successors wait until completion has actually settled");
            Tick(game); Tick(twin);
            c.That(TemplatePlan(game, left).State == PlanState.Active && TemplatePlan(game, right).State == PlanState.Active && TemplatePlan(game, finish).Order == 0, "independent branches admit together while join remains blocked");
            c.That(TestKit.Order(game, 0, new ConfigurePlanCommand { Plan = finish, Group = "detached", Priority = int.MaxValue, Paused = false }).Accepted, "display regroup does not sever topology");
            c.Equal(PlanCodes.ExternalDependencies, View(game).CapturePlanTemplate("detached", At(game, 12), out _).Blocker, "regrouped subset capture cannot erase outside ancestry");
            c.That(TestKit.Order(game, 0, new ConfigurePlanCommand { Plan = finish, Group = "diamond", Priority = int.MaxValue, Paused = false }).Accepted, "restore display grouping only");
            twin = factory.Load(game.Save()); Until(game, () => Group(game, "diamond").TrueForAll(p => p.State == PlanState.Done), "diamond", twin);
            c.Equal(game.Hash(), twin.Hash(), "real fan-in constructions finish identically from partial saved state");
            c.Equal(4, TemplateCount(game, "point.tower"), "only physically completed projects create facilities");
            c.Equal(Fix.FromInt(4), TemplateStock(game, game.World.Content.ResourceId("supply")), "four authored four-unit builds consume finite actual stock once");
            CorruptDependencies(c, game, root, new[] { root }, "completed history topology cannot be corrupted");
            var repeated = TestKit.Order(game, 0, new RepeatPlanGroupCommand { Group = "diamond", NewGroup = "repeated" });
            c.That(repeated.Accepted && repeated.Arg1 == 4, "entire terminal graph repeats through existing whole board");
            var again = Group(game, "repeated"); var byPosition = new Dictionary<string, PlanView>(); foreach (var p in again) byPosition.Add(p.Arguments.Get("placement").Get("position").Text, p);
            c.That(byPosition["16"].Dependencies[0] == byPosition["12"].Id && byPosition["20"].Dependencies[0] == byPosition["12"].Id, "repetition remaps root edges to fresh actual member IDs");
            c.That(byPosition["24"].Dependencies.Contains(byPosition["16"].Id) && byPosition["24"].Dependencies.Contains(byPosition["20"].Id), "reordered member repeat preserves both actual branch links");
            c.That(byPosition["12"].State == PlanState.Active && byPosition["24"].Order == 0, "old Done history cannot grant premature successor execution");
            CorruptDependencies(c, game, root, new[] { portable[0].Id }, "completed earned history cannot acquire an unfinished prerequisite");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "repeated graph and old terminal history use one codec");
            c.That(TestKit.Order(game, 0, new CancelPlanGroupCommand { Group = "repeated" }).Accepted, "explicit graph cancel retires only new work");
            game.Clock.Pause(); ulong paused = game.Hash(); c.Equal(0, game.Update(), "single clock pause stops the plan DAG"); c.Equal(paused, game.Hash(), "no plan-specific chronology advances"); game.Clock.Resume();
            DependencyBranchCancellation(c); DependencyFailure(c); DependencyRecurrence(c);
        }
        private static void DependencyBranchCancellation(Check c)
        {
            GameFactory factory = Dependencies(); SimGame game = ConstructionPlanTests.NewPoints(factory); EmergencyPreemptionTests.Configure(c, game);
            c.That(Place(game, View(game).AuthoredPlanTemplate("dependency.diamond"), 12, "branches").Accepted, "ordinary branched workflow intent");
            DependencyEdit(c, game, "branches", false); var rows = Group(game, "branches");
            Until(game, () => TemplatePlan(game, rows[3].Id).State == PlanState.Done, "branches"); Tick(game);
            c.That(TestKit.Order(game, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { rows[1].Id }) }).Accepted, "player cancels one admitted branch, not another activity or whole graph");
            SimGame twin = factory.Load(game.Save()); Until(game, () => TemplatePlan(game, rows[2].Id).State == PlanState.Done, "branches", twin);
            c.That(TemplatePlan(game, rows[3].Id).State == PlanState.Done && TemplatePlan(game, rows[1].Id).State == PlanState.Cancelled,
                "branch cancellation never rewinds earned predecessor completion");
            c.That(TemplatePlan(game, rows[0].Id).Order == 0 && TemplatePlan(game, rows[0].Id).Blocker == PlanCodes.DependencyCancelled,
                "fan-in refuses cancelled branch while independent sibling completes physically");
            c.Equal(game.Hash(), twin.Hash(), "interrupted physical branch and surviving sibling continue identically after save");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled branch retains actual saved topology and physical disposition");
        }
        private static void DependencyFailure(Check c)
        {
            GameFactory factory = Dependencies(failed: true); SimGame game = ConstructionPlanTests.NewPoints(factory); EmergencyPreemptionTests.Configure(c, game);
            c.That(Place(game, View(game).AuthoredPlanTemplate("dependency.diamond"), 12, "failed").Accepted, "authored failure workflow admitted normally");
            DependencyEdit(c, game, "failed", false); Until(game, () => Group(game, "failed")[3].State == PlanState.Failed, "failed");
            var rows = Group(game, "failed"); c.That(rows[3].Blocker == CompletionEffectDef.Rejected, "actual timed provider failure retained on predecessor");
            c.That(rows[1].Order == 0 && rows[2].Order == 0 && rows[1].Blocker == PlanCodes.DependencyFailed && rows[2].Blocker == PlanCodes.DependencyFailed, "failed physical predecessor never grants successors or fabricates Done");
            c.Equal(Fix.FromInt(16), TemplateStock(game, game.World.Content.ResourceId("supply")), "failed physical work does not refund already consumed inputs");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "failure and waiting ancestry save honestly");
        }
        private static void DependencyRecurrence(Check c)
        {
            foreach (bool provider in new[] { false, true })
            {
                GameFactory factory = Repeating(root => root.Get("plan_templates")[0].Get("entries")[1].Set("after", Content.Json.Parse("[\"first\"]")));
                SimGame game = ConstructionPlanTests.NewPoints(factory); EmergencyPreemptionTests.Configure(c, game);
                c.That(ConfigureTemplate(game, provider, true).Accepted, "native/public recurring provider uses authored graph links");
                Until(game, () => TemplateSource(game, provider).Schedule.LastPlan > 0, "recurring");
                long first = TemplateSource(game, provider).Schedule.LastPlan; PlanView second = TemplatePlan(game, first + 1);
                c.Equal(first, second.Dependencies[0], "recurring priority transformation preserves whole-group links");
                CorruptDependencies(c, game, second.Id, Array.Empty<long>(), "source history detects erased authored edge without re-expanding targets");
                SimGame twin = factory.Load(game.Save()); Until(game, () => TemplatePlan(game, first).State == PlanState.Done, "recurring", twin);
                c.That(TemplatePlan(game, second.Id).Order == 0 && TemplateSource(game, provider).Blocker == RecurringPlanCodes.Outstanding, "completed root with paused linked member never starts another cycle");
                c.Equal(game.Hash(), twin.Hash(), "native/public recurring graph partial continuation identical");
                c.That(ConfigureTemplate(game, provider, false).Accepted, "disable future cycles only");
                c.That(TestKit.Order(game, 0, new ConfigurePlanCommand { Plan = second.Id, Group = second.Group, Priority = 70, Paused = false }).Accepted, "release actual dependent through ordinary policy");
                Until(game, () => TemplatePlan(game, second.Id).State == PlanState.Done, "recurring");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "terminal recurring graph retains source/IDs/edges");
            }
        }
    }
}
