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
using static Beep.SimEngine.Tests.PlanetXProductionTests;
using static Beep.SimEngine.Tests.ProgressionDisclosureTests;

namespace Beep.SimEngine.Tests
{
    // Only explicit JSON composition, player commands, immutable observations, clock/saves and assertions.
    internal static class PlanDisclosureTests
    {
        internal const string Case = "Plan disclosure: whole authored and stored blueprints, groups and history retain original identities through revocation";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static List<StoredPlanTemplateView> Library(WorkView view)
        { var rows = new List<StoredPlanTemplateView>(); view.CopyStoredPlanTemplates(rows); return rows; }
        private static List<PlanView> Group(WorkView view)
        { var rows = new List<PlanView>(); view.CopyPlanGroup("inspection", rows); return rows; }
        private static List<PlanHistoryCohortView> History(WorkView view)
        { var rows = new List<PlanHistoryCohortView>(); view.CopyPlanHistory(rows); return rows; }
        private static void Run(Check c)
        {
            var initialize = new InitializeProbe();
            var factory = Fixture(initialize, activity: new ActivityDisclosureModule(), planDocument: "plan-disclosure.json");
            var game = New(factory, "plan-disclosure"); c.Equal(16, game.Modules.Count, "explicit master/scenario reuse sixteen original modules without a new coordinator");
            var work = game.Seat(0).View.Feature<WorkView>(); var observer = game.Observer.Feature<WorkView>();
            var anchor = Json.Parse("{\"space\":\"space.points\",\"position\":0}");
            PlanTemplate original = observer.AuthoredPlanTemplate("plans.disclosure.pair");
            c.That(original != null && original.Entries.Count == 2, "observer sees original whole file-authored blueprint");
            ulong hash = game.Hash();
            c.That(work.AuthoredPlanTemplate("plans.disclosure.pair") == null, "visible outer blueprint cannot expose hidden activity/provider references");
            c.Equal(Codes.UnknownDef, work.PreviewPlanTemplateStructure(original, anchor).Blocker, "structural preview refuses hidden content before provider schema callbacks");
            c.Equal(Codes.UnknownDef, work.PreviewStorePlanTemplate(0, 0, "inspection.design", original).Blocker, "private library admission uses same whole content disclosure");
            c.Equal(Codes.UnknownDef, game.Seat(0).Preview(new StorePlanTemplateCommand { Name = "inspection.design", Value = original }), "ordinary store command matches preview");
            c.Equal(Codes.UnknownDef, work.PreviewPlanTemplateSelection(PlanTemplateSelection.Snapshot(original), anchor, "inspection").Blocker, "held snapshot cannot bypass visibility");
            c.That(!work.ExpandPlanTemplate(original, anchor, "inspection", out var hiddenIntent).Ready && hiddenIntent == null, "hidden expansion yields no partial intent");
            c.Equal(hash, game.Hash(), "hidden blueprint observations and refusals are pure");
            var limits = new ReplayLimits(4 * 1024 * 1024, 4000); var recording = game.RecordReplay(limits);
            var twin = Reveal(c, factory, game, "tech");
            c.That(work.AuthoredPlanTemplate("plans.disclosure.pair") == null, "native entity activity declares its still-hidden exact target prototype too");
            twin = Reveal(c, factory, game, "predicate");
            var design = work.AuthoredPlanTemplate("plans.disclosure.pair");
            c.That(design != null && design.ToJson().ToString() == original.ToJson().ToString(), "whole discovery exposes the same truthful immutable authored intent");
            c.That(work.PreviewPlanTemplateStructure(design, anchor).Ready, "authorized structure uses original provider validation");
            twin = Input(c, factory, game, new StorePlanTemplateCommand { Name = "inspection.design", Value = design }); Step(game, twin);
            var retained = Library(work).Single(); long id = retained.Id;
            c.That(id > 0 && retained.Revision == 1 && retained.Template.ToJson().ToString() == design.ToJson().ToString(), "original library owns stable identity/revision and same immutable design");
            twin = Input(c, factory, game, new PlacePlanTemplateCommand { Selection = PlanTemplateSelection.Stored(id, 1), Anchor = anchor, Group = "inspection" }); Step(game, twin);
            var pending = Group(work);
            c.That(pending.Count == 2 && pending.All(x => x.State == PlanState.Pending), "ordinary stored blueprint creates only original paused physical plans");
            long[] ids = pending.Select(x => x.Id).ToArray();
            c.That(pending[1].Dependencies.Single() == ids[0] && ids[1] == ids[0] + 1, "original grouped plan prerequisite and stable allocation remain authoritative");
            c.That(work.CapturePlanTemplate("inspection", anchor, out var captured).Ready
                && captured.Entries.Select(x => x.Definition).SequenceEqual(design.Entries.Select(x => x.Definition))
                && captured.Entries[1].Dependencies.Single() == captured.Entries[0].Id
                && captured.Entries.Select(x => x.Arguments.ToString()).SequenceEqual(design.Entries.Select(x => x.Arguments.ToString())),
                "original owner captures whole geometry/dependencies with portable entry identities, not live IDs");
            c.That(History(work).Count == 1 && Group(game.Seat(1).View.Feature<WorkView>()).Count == 0 && Library(game.Seat(1).View.Feature<WorkView>()).Count == 0, "owned cohort visible without foreign company leakage");
            twin = Input(c, factory, game, new ConfigurePlanGroupCommand { Group = "inspection", Priority = 70, Paused = false }); Step(game, twin);
            Until(game, twin, () => Group(observer).Any(x => {
                var nodes = new List<ActivityNodeView>();
                return x.State == PlanState.Active && observer.TryCopyNodes(x.Order, nodes)
                    && nodes.Any(node => node.State == NodeState.Working && !node.Progress.Equals(WorkProgress.Zero));
            }), "native physical inspection starts");
            twin = Input(c, factory, game, new ForgetKnowledgeCommand { Policy = "disclose.forget" }); Step(game, twin);
            hash = game.Hash();
            c.That(work.AuthoredPlanTemplate("plans.disclosure.pair") == null && Library(work).Count == 0, "revocation withholds authored and stored blueprint metadata");
            c.Equal(Codes.NotFound, work.ReadStoredPlanTemplate(id, out var absent), "known stored ID has same refusal as absent after revocation"); c.That(absent == null, "no stored name/revision/payload leak");
            c.Equal(Codes.NotFound, work.ReadStoredPlanTemplate(long.MaxValue, out _), "absent private stored ID matches hidden");
            c.Equal(Codes.NotFound, work.PreviewPlanTemplateSelection(PlanTemplateSelection.Stored(id, 1), anchor, "again").Blocker, "stored selector cannot expose hidden revision/design");
            c.Equal(Codes.UnknownDef, work.PreviewPlanTemplateSelection(PlanTemplateSelection.Snapshot(design), anchor, "again").Blocker, "retained snapshot rechecks current provider authorization");
            c.Equal(Codes.UnknownDef, work.PreviewPlanTemplateStructure(design, anchor).Blocker, "retained structural preview rechecks authorization");
            c.Equal(Codes.UnknownDef, work.PreviewStorePlanTemplate(0, 0, "again", captured).Blocker, "captured design cannot republish hidden activity metadata");
            c.That(!work.ExpandPlanTemplate(design, anchor, "again", out hiddenIntent).Ready && hiddenIntent == null, "revoked expansion emits no partial entries");
            c.That(Group(work).Count == 0 && Group(observer).Count == 2, "whole group withheld instead of revealing the otherwise visible dependent");
            c.That(History(work).Count == 0 && History(observer).Count == 1, "whole history cohort withheld without hidden plan IDs or age diagnostics");
            c.Equal(Codes.NotFound, work.CapturePlanTemplate("inspection", anchor, out captured).Blocker, "hidden group capture has no member diagnostic leak"); c.That(captured == null, "hidden capture emits no blueprint");
            c.Equal(hash, game.Hash(), "revoked reads/refusals neither mutate design nor authoritative work");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "valid hidden saved designs remain structural and restore during active work");
            Until(game, twin, () => Group(observer).Count == 2 && Group(observer).All(x => x.State == PlanState.Done), "already admitted hidden group physically finishes in original dependency order");
            c.That(Group(work).Count == 0 && History(work).Count == 0 && Library(work).Count == 0, "completion does not implicitly disclose private blueprint or retained history");
            c.That(pending.All(x => x.State == PlanState.Pending) && retained.Revision == 1 && retained.Template.Entries.Count == 2, "retained immutable facts do not mutate during hidden execution");
            twin = Reveal(c, factory, game, "tech"); c.That(work.AuthoredPlanTemplate("plans.disclosure.pair") == null, "prototype authorization remains independently required after re-discovery");
            twin = Reveal(c, factory, game, "predicate");
            c.That(Group(work).Select(x => x.Id).SequenceEqual(ids) && Group(work).All(x => x.State == PlanState.Done), "re-discovery exposes same physically earned plan identities and states");
            c.That(History(work).Count == 1 && Library(work).Single().Id == id && Library(work).Single().Revision == 1, "original cohort/design revision retained without replacement records");
            c.Equal(design.ToJson().ToString(), work.AuthoredPlanTemplate("plans.disclosure.pair").ToJson().ToString(), "authored blueprint remains original file intent");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "revealed terminal plans and stored design preserve hash");
            c.That(new ReplayVerifier().Verify(factory, recording.Finish(), limits).Success, "ordinary saved inputs replay discovery/design/physical group/revocation/re-discovery");
            var isolated = New(factory, "plan-disclosure"); Step(isolated);
            c.That(isolated.Seat(0).View.Feature<WorkView>().AuthoredPlanTemplate("plans.disclosure.pair") == null && game.Fault == null, "interleaved session has no leaked design discovery or execution");
        }
    }
}
