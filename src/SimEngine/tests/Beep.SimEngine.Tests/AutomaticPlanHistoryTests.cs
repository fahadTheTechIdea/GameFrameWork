using System;
using System.Collections.Generic;
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
        private static string HistoryDeclarations()
        { var rows = JsonValue.NewArray(); rows.Add(Content.Json.Parse(FrameworkProofContent.Read("plan-history-manifest.json"))); return rows.ToString(); }
        private static Action<JsonValue> HistoryScenario(string policy, bool selected = true)
            => scenario => {
                if (selected) scenario.Get("modules").Add(JsonValue.Of(PlanHistoryModule.ModuleId));
                var settings = JsonValue.NewObject(); settings.Set("policy", policy == null ? JsonValue.Null() : JsonValue.Of(policy));
                scenario.Get("settings").Set(PlanHistoryModule.ModuleId, settings);
            };
        internal static GameFactory AutomaticHistoryFactory(string policy = "history.proof", Action<JsonValue> edit = null, bool selected = true)
        {
            JsonValue content = Content.Json.Parse(FrameworkProofContent.Read("plan-history.json")); edit?.Invoke(content);
            return Dependencies(extraModules: new GameModule[] { new PlanHistoryPolicyProofModule() }, extraDeclarations: HistoryDeclarations(),
                extraDocuments: new Dictionary<string, string> { ["plan-history.json"] = content.ToString() }, scenarioEdit: HistoryScenario(policy, selected));
        }
        internal static List<PlanHistoryCohortView> HistoryRows(SimGame game, int seat = 0)
        { var rows = new List<PlanHistoryCohortView>(); View(game, seat).CopyPlanHistory(rows); return rows; }
        private static void CancelHistory(Check c, SimGame game, string group, int seat = 0)
            => c.That(TestKit.Order(game, seat, new CancelPlanGroupCommand { Group = group }).Accepted, "ordinary player cancellation establishes terminal history");
        private static void AutomaticHistoryContracts(Check c)
        {
            foreach (string field in new[] { "minimum_age", "minimum_retained_cohorts", "cohorts_per_update" })
                c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(AutomaticHistoryFactory(edit: root => root.Get("plan_history_policies")[0].Remove(field))), "history JSON requires explicit " + field);
            foreach (Action<JsonValue> bad in new Action<JsonValue>[] {
                root => root.Get("plan_history_policies")[0].Set("minimum_retained_cohorts", JsonValue.Number("-1")),
                root => root.Get("plan_history_policies")[0].Set("cohorts_per_update", JsonValue.Number("0")),
                root => root.Get("plan_history_policies")[0].Get("minimum_age").Set("numerator", JsonValue.Number("-1")),
                root => root.Get("plan_history_policies")[0].Set("unknown", JsonValue.Of(true)) })
                c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(AutomaticHistoryFactory(edit: bad)), "invalid/extra retention authoring refuses before Update");
            c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(AutomaticHistoryFactory("missing")), "selected unknown policy never falls back");
            c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(AutomaticHistoryFactory(null)), "selected null policy never enables a fabricated default");
            GameFactory absentFactory = AutomaticHistoryFactory(selected: false); SimGame absent = ConstructionPlanTests.NewPoints(absentFactory);
            long absentId = DependencySubmit(c, absent, DependencyBuild(absent, 12, "unselected"));
            CancelHistory(c, absent, "unselected"); for (int i = 0; i < 12; i++) Tick(absent);
            c.That(!View(absent).ReadPlanHistoryPolicy().HasValue && Group(absent, "unselected").Count == 1, "registered but unselected history module does not silently prune");
            c.Equal(PlanCodes.HistoryUnselected, HistoryRows(absent)[0].Blocker, "diagnostics explicitly report absent automatic policy");
            c.That(Retire(absent, History(absentId)).Accepted, "explicit safe retirement stays available without automatic policy");

            GameFactory factory = AutomaticHistoryFactory(edit: root => root.Get("plan_history_policies")[0].Set("minimum_retained_cohorts", JsonValue.Number("0")));
            SimGame game = ConstructionPlanTests.NewPoints(factory);
            c.That(game.World.Content.Get<PlanHistoryPolicyDef>("history.proof").AuthoringPath.Contains("plan-history.json"), "policy is resolved from separate explicitly master-listed JSON");
            var policy = View(game).ReadPlanHistoryPolicy().Value;
            c.That(policy.Policy == "history.proof" && policy.MinimumRetainedCohorts == 0 && policy.CohortsPerUpdate == 2 && policy.Cursor == 0, "selected immutable policy facts expose no live provider");
            long id = DependencySubmit(c, game, DependencyBuild(game, 12, "age"));
            for (int i = 0; i < 20; i++) Tick(game);
            c.Equal(PlanCodes.NotEnded, HistoryRows(game)[0].Blocker, "arbitrarily old live intent is not disposable terminal history");
            CancelHistory(c, game, "age"); PlanView terminal = TemplatePlan(game, id);
            c.That(terminal.EndedTick >= terminal.CreatedTick + 20 && terminal.EndedTick < game.Clock.StepIndex, "actual end timestamp comes only from the sole game clock");
            c.Equal(PlanCodes.HistoryYoung, HistoryRows(game)[0].Blocker, "history age begins at cancellation rather than creation");
            ulong hash = game.Hash(); long time = game.Clock.StepIndex; var retainedView = HistoryRows(game);
            c.Equal(hash, game.Hash(), "cohort policy/reference diagnostics are pure");
            c.Equal(time, game.Clock.StepIndex, "reading cleanup diagnostics drives no independent timer");
            c.Equal(0, HistoryRows(game, 1).Count, "other company cannot read private cohorts");
            c.Throws<NotSupportedException>(() => ((IList<long>)retainedView[0].Plans).Clear(), "cohort membership projection is immutable");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "terminal chronology and native-ID cursor save in one board codec");
            game.Clock.Pause(); twin.Clock.Pause(); long cursor = View(game).ReadPlanHistoryPolicy().Value.Cursor;
            for (int i = 0; i < 8; i++) { game.Update(); twin.Update(); }
            c.That(game.Clock.StepIndex == time && View(game).ReadPlanHistoryPolicy().Value.Cursor == cursor && Group(game, "age").Count == 1, "one-clock pause freezes automatic cleanup and its fairness cursor");
            c.Equal(game.Hash(), twin.Hash(), "paused saved histories remain identical"); game.Clock.Resume(); twin.Clock.Resume();
            for (int i = 0; i < 12; i++) { Tick(game); Tick(twin); }
            c.That(TemplatePlans(game).Count == 0 && retainedView[0].Plans[0] == id, "normal Update retires aged history without mutating retained views");
            c.Equal(game.Hash(), twin.Hash(), "aged automatic retirement preserves deterministic saved continuation");
            long later = DependencySubmit(c, game, DependencyBuild(game, 16, "age")); c.That(later > id, "automatic retirement never recycles native IDs");
            HistoryPolicyCorruption(c, game, factory);
            AutomaticHistoryMinimum(c);
            AutomaticHistoryBudget(c);
            AutomaticHistoryProvider(c);
        }
        private static void HistoryPolicyCorruption(Check c, SimGame game, GameFactory factory)
        {
            var board = game.World.Services.Get<WorkBoard>(); var cursor = typeof(WorkBoard).GetField("_historyCursor", BindingFlags.Instance | BindingFlags.NonPublic);
            object original = cursor.GetValue(board); cursor.SetValue(board, long.MaxValue);
            try { c.Throws<CorruptStateException>(() => game.Save(), "future forged scan cursor cannot checkpoint"); } finally { cursor.SetValue(board, original); }
            CancelHistory(c, game, "age");
            var entries = (System.Collections.IEnumerable)typeof(WorkBoard).GetField("_plans", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(board);
            foreach (var entry in entries)
            {
                var field = entry.GetType().GetField("EndedTick", BindingFlags.Instance | BindingFlags.NonPublic);
                object old = field.GetValue(entry); field.SetValue(entry, game.Clock.StepIndex + 1);
                try { c.Throws<CorruptStateException>(() => game.Save(), "future terminal timestamp cannot impersonate completed game time"); } finally { field.SetValue(entry, old); }
                break;
            }
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "restored genuine timestamp/cursor remains valid after corruption probes");
        }
        private static void AutomaticHistoryMinimum(Check c)
        {
            var factory = AutomaticHistoryFactory(); SimGame game = ConstructionPlanTests.NewPoints(factory);
            long parent = DependencySubmit(c, game, DependencyBuild(game, 12, "parent"));
            long child = DependencySubmit(c, game, DependencyBuild(game, 16, "child", parent));
            long foreign = DependencySubmit(c, game, DependencyBuild(game, 40, "foreign"), 1);
            CancelHistory(c, game, "parent"); CancelHistory(c, game, "child"); CancelHistory(c, game, "foreign", 1);
            for (int i = 0; i < 15; i++) Tick(game);
            var rows = HistoryRows(game);
            c.That(rows.Count == 2 && rows.Find(row => row.Head == parent).Blocker == PlanCodes.HistoryDependency
                && rows.Find(row => row.Head == child).Blocker == PlanCodes.HistoryMinimum, "excluded newest cohort keeps the real older prerequisite through safe-set fixed point");
            c.That(HistoryRows(game, 1).Count == 1 && HistoryRows(game, 1)[0].Head == foreign && HistoryRows(game, 1)[0].Blocker == PlanCodes.HistoryMinimum, "minimum retained cohorts is per company, not global");
            c.That(Retire(game, History(child)).Accepted, "player may explicitly retire safe history protected only by automatic policy minimum");
            // Parent is now this company's newest terminal cohort, so minimum policy still retains it.
            c.Equal(PlanCodes.HistoryMinimum, HistoryRows(game)[0].Blocker, "newest policy recomputes from actual remaining history, not a copied retention ledger");
            long newest = DependencySubmit(c, game, DependencyBuild(game, 20, "newest")); CancelHistory(c, game, "newest");
            for (int i = 0; i < 12; i++) Tick(game);
            c.That(TemplatePlans(game).Count == 1 && TemplatePlan(game, newest).State == PlanState.Cancelled && TemplatePlan(game, parent).Id == 0,
                "unreferenced older cohort retires once a genuine newer terminal cohort exists");
        }
        private static void AutomaticHistoryBudget(Check c)
        {
            var factory = AutomaticHistoryFactory("history.immediate"); SimGame game = ConstructionPlanTests.NewPoints(factory);
            long live = DependencySubmit(c, game, DependencyBuild(game, 12, "always.live"));
            for (int i = 0; i < 6; i++) DependencySubmit(c, game, DependencyBuild(game, 20 + 4 * i, "budget." + i));
            for (int i = 0; i < 6; i++) game.Seat(0).Submit(new CancelPlanGroupCommand { Group = "budget." + i });
            int before = TemplatePlans(game).Count; Tick(game); int after = TemplatePlans(game).Count;
            c.That(before - after >= 0 && before - after <= 1, "authored one-cohort budget permits at most one retirement on a normal Update, including a protected cohort scan");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partially scanned queue cursor is durable in the same board");
            for (int i = 0; i < 20 && TemplatePlans(game).Count > 1; i++)
            {
                before = TemplatePlans(game).Count; Tick(game); Tick(twin);
                c.That(before - TemplatePlans(game).Count <= 1, "cleanup obeys authored cohort budget while round-robin revisits blocked head");
            }
            c.That(TemplatePlans(game).Count == 1 && TemplatePlan(game, live).State == PlanState.Pending, "old permanently live head cannot starve eligible younger terminal cohorts");
            c.Equal(game.Hash(), twin.Hash(), "saved scan fairness and actual retired identities remain identical");
        }
        private static void AutomaticHistoryPhysical(Check c)
        {
            GameFactory factory = AutomaticHistoryFactory(edit: root => root.Get("plan_history_policies")[0].Set("minimum_retained_cohorts", JsonValue.Number("0")));
            SimGame game = ConstructionPlanTests.NewPoints(factory); EmergencyPreemptionTests.Configure(c, game);
            long first = DependencySubmit(c, game, DependencyBuild(game, 12, "automatic.physical"));
            long second = DependencySubmit(c, game, DependencyBuild(game, 16, "automatic.physical", first));
            DependencyEdit(c, game, "automatic.physical", false);
            EditingReach(game, () => TemplatePlans(game).Count == 2 && Group(game, "automatic.physical").TrueForAll(p => p.State == PlanState.Done),
                "physical actual end chronology");
            var completed = Group(game, "automatic.physical");
            c.That(completed[1].EndedTick >= completed[0].EndedTick && completed[1].EndedTick > completed[0].CreatedTick, "cohort age uses latest real physical member completion");
            c.Equal(PlanCodes.HistoryYoung, HistoryRows(game)[0].Blocker, "newly completed physical graph retains its full age window");
            SimGame twin = factory.Load(game.Save());
            for (int i = 0; i < 20; i++) { Tick(game); Tick(twin); }
            c.That(TemplatePlans(game).Count == 0 && TemplateCount(game, "point.tower") == 2, "automatic cleanup retires whole actual graph without destroying its finished structures");
            c.Equal(Fix.FromInt(12), FindPoint(game, "source").Get<IInventory>().Amount(game.World.Content.ResourceId("supply")), "automatic cleanup neither refunds nor consumes finite construction material");
            c.Equal(game.Hash(), twin.Hash(), "physical automatic lifecycle restores and continues deterministically");
            foreach (bool provider in new[] { false, true }) { AutomaticHistoryRecurring(c, provider); AutomaticHistoryTemplate(c, provider); }
        }
        private static void AutomaticHistoryRecurring(Check c, bool provider)
        {
            var factory = Repeating(root => {
                root.Get("entities")[0].Get("components").Set("point_presence", Content.Json.Parse("{\"space\":\"field.points\",\"position\":12}"));
                root.Get("entities")[0].Get("components").Set("work_permit", Content.Json.Parse("{\"enabled\":true}"));
                root.Set("entity_states", Content.Json.Parse("[{\"id\":\"stopped\",\"state\":\"point.permit-stopped\",\"priority\":61}]"));
                var row = TemplateRow(root, provider); row.Set("template", JsonValue.Null()); row.Set("plan", JsonValue.Of("activity:inspect"));
                row.Set("target", JsonValue.Of("self")); row.Set("arguments", JsonValue.Null()); row.Set("group", JsonValue.Null());
            }, extraModules: new GameModule[] { new PlanHistoryPolicyProofModule() }, extraDeclarations: HistoryDeclarations(),
                extraDocuments: new Dictionary<string, string> { ["plan-history.json"] = FrameworkProofContent.Read("plan-history.json") },
                scenarioEdit: HistoryScenario(provider ? "history.public" : "history.immediate"));
            SimGame game = ConstructionPlanTests.NewPoints(factory); EmergencyPreemptionTests.Configure(c, game);
            c.Equal(provider ? "history.public" : "history.immediate", View(game).ReadPlanHistoryPolicy().Value.Policy,
                "native/public authored policy resolves through the same engine module/board contract");
            c.That(ConfigureTemplate(game, provider, true).Accepted, "native/public authored actual machine inspection source starts normally");
            TemplateReach(game, () => TemplateSource(game, provider).Schedule.LastPlan >= 10, "repeated actual inspection cleanup");
            c.That(TemplatePlans(game).Count <= 2 && game.Clock.StepIndex > 10, "unreferenced completed source cycles are bounded by authored fair-scan budget and real physical time");
            long current = TemplateSource(game, provider).Schedule.LastPlan;
            c.That(TemplatePlan(game, current).Origin.CycleHead == current && !TemplatePlan(game, current).Origin.TemplateCycle,
                "same cleanup supports actual single-plan recurring provenance, not a template-only workaround");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "native/public bounded recurring history and cursor save together");
            long time = game.Clock.StepIndex, cursor = View(game).ReadPlanHistoryPolicy().Value.Cursor;
            game.Clock.Pause(); for (int i = 0; i < 5; i++) game.Update();
            c.That(game.Clock.StepIndex == time && View(game).ReadPlanHistoryPolicy().Value.Cursor == cursor, "recurring cleanup pauses with the one game clock");
            game.Clock.Resume();
            TemplateReach(game, () => TemplateSource(game, provider).Schedule.LastPlan >= 20, "saved bounded source continuation", twin);
            c.That(TemplatePlans(game).Count <= 2 && TemplateSource(game, provider).Schedule.LastPlan > current, "twenty actual cycles retain at most one awaiting fair-scan terminal cohort and never reuse identities");
            c.Equal(game.Hash(), twin.Hash(), "public/native saved physical recurrence stays deterministic after repeated automatic retirement");
            SimGame isolated = ConstructionPlanTests.NewPoints(factory);
            c.That(TemplatePlans(isolated).Count == 0 && View(isolated).ReadPlanHistoryPolicy().Value.Cursor == 0, "interleaved fresh session has no shared cleanup cursor/history");
        }
        private static void AutomaticHistoryTemplate(Check c, bool provider)
        {
            var factory = Repeating(root => {
                var rows = root.Get("plan_templates")[0].Get("entries");
                rows[0].Set("paused", JsonValue.Of(true)); rows[1].Set("paused", JsonValue.Of(true));
                rows[1].Set("after", Content.Json.Parse("[\"first\"]"));
            }, extraModules: new GameModule[] { new PlanHistoryPolicyProofModule() }, extraDeclarations: HistoryDeclarations(),
                extraDocuments: new Dictionary<string, string> { ["plan-history.json"] = FrameworkProofContent.Read("plan-history.json") },
                scenarioEdit: HistoryScenario(provider ? "history.public" : "history.immediate"));
            SimGame game = ConstructionPlanTests.NewPoints(factory);
            c.That(ConfigureTemplate(game, provider, true).Accepted && ConfigureTemplate(game, provider, false).Accepted, "ordinary native/public source posts one paused authored template cycle");
            long old = TemplateSource(game, provider).Schedule.LastPlan; string group = TemplatePlan(game, old).Group;
            CancelHistory(c, game, group);
            c.That(HistoryRows(game).Count == 1 && HistoryRows(game)[0].Plans.Count == 2 && HistoryRows(game)[0].Blocker == PlanCodes.HistorySource,
                "automatic diagnostics treat a whole frozen cycle as one source-retained cohort despite zero age");
            var next = new RecurringTemplateSettings(PlanTemplateSelection.Snapshot(View(game).AuthoredPlanTemplate("repeat.pair")), At(game, 40), "auto.next");
            c.That(SelectRecurring(game, provider, next).Accepted && ConfigureTemplate(game, provider, true).Accepted && ConfigureTemplate(game, provider, false).Accepted,
                "next actual cycle releases old source reference through the normal engine command path");
            for (int i = 0; i < 4; i++) Tick(game);
            long head = TemplateSource(game, provider).Schedule.LastPlan;
            c.That(TemplatePlans(game).Count == 2 && TemplatePlan(game, old).Id == 0 && TemplatePlan(game, old + 1).Id == 0
                && TemplatePlan(game, head).Origin.CycleHead == head && TemplatePlan(game, head + 1).Dependencies[0] == head,
                "automatic retirement removes the entire old template while preserving exact new native/public ancestry");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "frozen template cohort cleanup saves without orphan cycle heads");
        }
        private static void AutomaticHistoryProvider(Check c)
        {
            var factory = Repeating(root => root.Get("entities")[0].Get("components").Set("proof_plan_history", Content.Json.Parse("{\"group\":\"automatic.protected\",\"enabled\":true}")),
                extraModules: new GameModule[] { new PlanHistoryPolicyProofModule() }, extraDeclarations: HistoryDeclarations(),
                extraDocuments: new Dictionary<string, string> { ["plan-history.json"] = FrameworkProofContent.Read("plan-history.json") },
                scenarioEdit: HistoryScenario("history.public"));
            SimGame game = ConstructionPlanTests.NewPoints(factory);
            long id = DependencySubmit(c, game, DependencyBuild(game, 12, "automatic.protected")); CancelHistory(c, game, "automatic.protected");
            for (int i = 0; i < 6; i++) Tick(game);
            c.That(TemplatePlan(game, id).State == PlanState.Cancelled && HistoryRows(game)[0].Blocker == PlanCodes.HistoryProvider,
                "expired automatic history remains while a real public provider reference exists");
            ulong hash = game.Hash(); var component = TemplateOwner(game).Get<ProofPlanHistoryRetention>();
            component.ReadFault = () => HistoryRows(game);
            c.Throws<InvalidOperationException>(() => HistoryRows(game), "recursive reference diagnostics fail explicitly instead of overflowing or silently pruning");
            component.ReadFault = null;
            c.Equal(hash, game.Hash(), "rejected recursive read leaves history, clock and scan cursor unchanged");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "reference-held expired cohorts remain durable");
        }
    }
}
