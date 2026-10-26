using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Standard;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    public static partial class PlanTemplateTests
    {
        private static GameFactory ClosureFactory(int minutes)
            => ReferenceFactory(true, historyEdit: history => history.Get("public_plan_history_policies")[0].Get("minimum_age").Set("numerator", JsonValue.Number(minutes.ToString(System.Globalization.CultureInfo.InvariantCulture))));
        private static void HistoryClosureChain(Check c)
        {
            const int count = 128; GameFactory factory = ClosureFactory(512);
            SimGame game = ConstructionPlanTests.NewPoints(factory); var ids = new List<long>();
            for (int i = 0; i < count; i++)
            {
                string group = i == count - 1 ? "reference.service" : "closure." + i;
                long id = DependencySubmit(c, game, DependencyBuild(game, 12, group, i == 0 ? Array.Empty<long>() : new[] { ids[i - 1] }));
                ids.Add(id); CancelHistory(c, game, group);
            }
            // Real game updates, never injected timestamps/history or a second aging timer.
            TemplateAdvance(game, checked((int)game.World.Time.ExactTicks(GameDuration.Minutes(512))));
            c.Equal(count, TemplatePlans(game).Count, "provider-held tail retains every actual command-authored aged ancestor");
            var service = game.World.Services.Get<ProofPlanReferenceService>(); int reads = 0;
            service.References.ReadFault = () => reads++;
            ulong hash = game.Hash(); long time = game.Clock.StepIndex;
            var rows = HistoryRows(game);
            c.Equal(count, reads, "one initial native admission observes each plan once, not one full provider scan per rejected ancestor");
            c.Equal(count, rows.Count, "closure preserves actual distinct native cohorts, not compressed completion tokens");
            bool exact = true;
            for (int i = 0; i < count; i++)
                exact &= rows[i].Head == ids[i] && rows[i].Blocker == (i == count - 1 ? PlanCodes.HistoryProvider : PlanCodes.HistoryDependency);
            c.That(exact, "direct tail blocker remains primary and native ancestor closure reports dependency blockers in stable ID order");
            c.That(game.Clock.StepIndex == time && game.Hash() == hash, "counted closure diagnostics are pure and use only the sole game clock");
            c.Equal(0, HistoryRows(game, 1).Count, "another company cannot read this retained graph");
            c.Equal(count, reads, "foreign empty diagnostics do not inspect private reference providers");
            service.References.ReadFault = null; SimGame twin = factory.Load(game.Save());
            c.Equal(hash, twin.Hash(), "aged graph saves only its existing board/provider/cursor state");
            int twinReads = 0; twin.World.Services.Get<ProofPlanReferenceService>().References.ReadFault = () => twinReads++;
            var restored = HistoryRows(twin); c.That(twinReads == count && restored.Count == rows.Count, "loaded native graph computes the same one-admission closure without a saved shadow index");
            for (int i = 0; i < rows.Count; i++)
                c.That(rows[i].Head == restored[i].Head && rows[i].Blocker == restored[i].Blocker, "restored closure entry " + i + " preserves actual identity/blocker");
            twin.World.Services.Get<ProofPlanReferenceService>().References.ReadFault = null;
            game.Clock.Pause(); twin.Clock.Pause(); game.Update(); twin.Update();
            c.That(game.Clock.StepIndex == time && TemplatePlans(game).Count == count, "single-clock pause never spends history scan work or erases retained ancestry");
            game.Clock.Resume(); twin.Clock.Resume(); TemplateAdvance(game, 3, twin);
            c.Equal(game.Hash(), twin.Hash(), "native retained graph continues identically after saved closure and pause");
        }
        private static void HistoryClosureGroups(Check c)
        {
            GameFactory factory = ClosureFactory(1); SimGame game = ConstructionPlanTests.NewPoints(factory);
            long first = DependencySubmit(c, game, DependencyBuild(game, 12, "reference.service"));
            long middle = DependencySubmit(c, game, DependencyBuild(game, 16, "closure.middle", first));
            long last = DependencySubmit(c, game, DependencyBuild(game, 20, "reference.service", middle));
            CancelHistory(c, game, "reference.service"); CancelHistory(c, game, "closure.middle");
            // Native first -> middle -> last is acyclic; grouping contracts it to A -> B -> A.
            long child = DependencySubmit(c, game, DependencyBuild(game, 24, "closure.free-child", middle));
            CancelHistory(c, game, "closure.free-child");
            var service = game.World.Services.Get<ProofPlanReferenceService>(); int reads = 0; service.References.ReadFault = () => reads++;
            ulong hash = game.Hash(); long time = game.Clock.StepIndex; var rows = HistoryRows(game);
            c.Equal(4, reads, "group-contracted cycle and free descendant use one actual admission rather than repeated wave scans");
            c.Equal(3, rows.Count, "manual native group remains one whole two-member cohort");
            var held = rows.Find(row => row.Head == first); var ancestor = rows.Find(row => row.Head == middle); var free = rows.Find(row => row.Head == child);
            c.That(held.Blocker == PlanCodes.HistoryProvider && held.Plans.Count == 2 && held.Plans[1] == last,
                "first direct blocker retains both actual members of the authored group");
            c.Equal(PlanCodes.HistoryDependency, ancestor.Blocker, "rejected whole group propagates its trailing member's real dependency and terminates at already retained cohort");
            c.That(free.Eligible && free.Plans.Count == 1, "retained ancestry does not falsely retain a disposable descendant that only depends on it");
            c.That(game.Hash() == hash && game.Clock.StepIndex == time, "group closure never changes native graph/cursor/time");
            service.References.ReadFault = null; SimGame twin = factory.Load(game.Save());
            c.That(HistoryRows(twin).Find(row => row.Head == child).Eligible && twin.Hash() == hash, "pure grouped closure survives save/load with no cached graph authority");
            TemplateAdvance(game, 3, twin);
            c.That(TemplatePlan(game, child).Id == 0 && TemplatePlans(game).Count == 3,
                "ordinary bounded automatic retirement removes only safe descendant and leaves retained native group ancestry intact");
            c.Equal(game.Hash(), twin.Hash(), "whole retained cohort and safe descendant retirement continue identically on saved twin");
        }
    }
}
