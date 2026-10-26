using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    public static partial class PlanTemplateTests
    {
        private static PlanHistorySelection History(params long[] plans) => new PlanHistorySelection(plans);
        private static CommandResult Retire(SimGame game, PlanHistorySelection selection, int seat = 0)
            => TestKit.Order(game, seat, new RetirePlanHistoryCommand { Selection = selection });
        private static void HistoryContracts(Check c)
        {
            var ids = new List<long> { 9, 2 }; var value = new PlanHistorySelection(ids); ids.Clear();
            c.That(value.Plans.Count == 2 && value.Plans[0] == 2 && value.Plans[1] == 9, "retirement captures canonical immutable actual IDs");
            c.Throws<NotSupportedException>(() => ((IList<long>)value.Plans).Clear(), "history selection cannot be mutated through retained list");
            c.Throws<ArgumentNullException>(() => new PlanHistorySelection(null), "selection is explicit");
            c.Throws<ArgumentException>(() => History(), "empty retirement cannot fabricate success");
            c.Throws<ArgumentException>(() => History(1, 1), "duplicate retirement IDs refuse");
            c.Throws<ArgumentException>(() => History(0), "zero is not a history identity");
            byte[] wire = DependencyWire(new RetirePlanHistoryCommand { Selection = value });
            var decoded = new RetirePlanHistoryCommand(); decoded.Sync(new StateReader(wire, 0, wire.Length));
            c.Equal(Convert.ToBase64String(wire), Convert.ToBase64String(DependencyWire(decoded)), "retirement wire uses one canonical identity codec");
            c.Equal(Convert.ToBase64String(wire), Convert.ToBase64String(DependencyWire(new RetirePlanHistoryCommand { Selection = History(2, 9) })), "arrival selection order cannot alter command bytes");
            Array.Copy(wire, wire.Length - 2 * sizeof(long), wire, wire.Length - sizeof(long), sizeof(long));
            c.Throws<CorruptStateException>(() => new RetirePlanHistoryCommand().Sync(new StateReader(wire, 0, wire.Length)), "wire cannot normalize duplicate IDs into a valid selection");

            GameFactory factory = Dependencies(); SimGame game = ConstructionPlanTests.NewPoints(factory);
            long first = DependencySubmit(c, game, DependencyBuild(game, 12, "history"));
            long second = DependencySubmit(c, game, DependencyBuild(game, 16, "history", first));
            c.Equal(Codes.InvalidArgument, Retire(game, null).Refusal, "missing retirement selection refuses");
            c.Equal(PlanCodes.NotEnded, View(game).PreviewRetirePlanHistory(History(first)).Blocker, "pending intent is not disposable history");
            c.Equal(Codes.NotOwned, Retire(game, History(first), 1).Refusal, "other company cannot retire this actual plan");
            c.That(TestKit.Order(game, 0, new CancelPlanGroupCommand { Group = "history" }).Accepted, "player cancels actual paused graph");
            ulong hash = game.Hash(); long time = game.Clock.StepIndex;
            var blocked = View(game).PreviewRetirePlanHistory(History(first));
            c.Equal(PlanCodes.HistoryDependency, blocked.Blocker, "even terminal surviving descendant retains its actual prerequisite");
            c.That(blocked.Entries.Count == 1 && blocked.Entries[0].Plan == first, "retirement diagnostics report requested stable identity");
            c.Equal(hash, game.Hash(), "retirement preview mutates neither history nor current game time");
            c.Equal(time, game.Clock.StepIndex, "retirement preview does not drive a second clock");
            c.Equal(blocked.Blocker, Retire(game, History(first)).Refusal, "dependency refusal command/preview parity");
            var mixed = View(game).PreviewRetirePlanHistory(History(first, 999));
            c.That(mixed.Blocker == Codes.NotFound && mixed.Entries.Count == 2 && mixed.Entries[0].Blocker.IsNone, "whole missing identity admission is checked before reference observation");
            c.Equal(Codes.NotFound, Retire(game, History(first, 999)).Refusal, "late invalid member creates no retirement prefix");
            c.Equal(2, Group(game, "history").Count, "atomic refusal retains whole actual graph");
            c.That(View(game).PreviewRetirePlanHistory(History(first, second)).Ready, "terminal internal dependency retires only with its complete selected ancestry");
            c.That(Retire(game, History(first, second)).Accepted, "owned whole terminal graph retires through engine command");
            c.Equal(0, Group(game, "history").Count, "sole actual list has no shadow history");
            c.Equal(Codes.NotFound, TestKit.Order(game, 0, new RepeatPlanCommand { Plan = first }).Refusal, "retired stable ID never silently repeats different intent");
            long later = DependencySubmit(c, game, DependencyBuild(game, 12, "history"));
            c.That(later > second, "same label and placement cannot recycle retired IDs");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "one board codec accepts genuine identity gaps");
            HistoryRecurring(c);
            HistoryPublicProvider(c);
            HistoryController(c);
        }
        private static void HistoryRecurring(Check c)
        {
            foreach (bool provider in new[] { false, true })
            {
                var factory = LibraryRepeating(root => {
                    var rows = root.Get("plan_templates")[0].Get("entries");
                    rows[0].Set("paused", JsonValue.Of(true)); rows[1].Set("paused", JsonValue.Of(true));
                    rows[1].Set("after", Content.Json.Parse("[\"first\"]"));
                });
                SimGame game = ConstructionPlanTests.NewPoints(factory);
                c.That(ConfigureTemplate(game, provider, true).Accepted && ConfigureTemplate(game, provider, false).Accepted, "native/public source posts one real paused cycle then disables future work");
                long head = TemplateSource(game, provider).Schedule.LastPlan; string group = TemplatePlan(game, head).Group;
                c.That(TestKit.Order(game, 0, new CancelPlanGroupCommand { Group = group }).Accepted, "actual whole recurring cycle ends before retirement");
                c.Equal(PlanCodes.HistoryDependency, Retire(game, History(head)).Refusal, "incomplete recurring head also has real retained dependency");
                c.Equal(PlanCodes.HistoryCycle, Retire(game, History(head + 1)).Refusal, "trailing member cannot retire away from frozen cycle head");
                c.Equal(PlanCodes.HistorySource, Retire(game, History(head, head + 1)).Refusal, "disabled actual source still requires latest cycle chronology");
                var next = new RecurringTemplateSettings(PlanTemplateSelection.Snapshot(View(game).AuthoredPlanTemplate("repeat.pair")), At(game, 40), "history.future");
                c.That(SelectRecurring(game, provider, next).Accepted && ConfigureTemplate(game, provider, true).Accepted && ConfigureTemplate(game, provider, false).Accepted, "source explicitly posts next paused cycle using normal engine controls");
                long latest = TemplateSource(game, provider).Schedule.LastPlan;
                c.That(latest > head + 1 && View(game).PreviewRetirePlanHistory(History(head, head + 1)).Ready, "prior complete cycle becomes unreferenced only after actual next admission");
                c.That(Retire(game, History(head, head + 1)).Accepted, "whole old native/public cycle retires atomically");
                c.That(TemplatePlans(game).Count == 2 && TemplatePlan(game, latest).Origin.CycleHead == latest, "latest live cycle retains exact frozen shape with no history surrogate");
                c.Equal(0, TemplateCount(game, "point.tower-site"), "retiring paused cancelled work creates no project");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "source and frozen latest cycle restore after honest retired ID gaps");
            }
        }
        private static void HistoryPublicProvider(Check c)
        {
            var factory = LibraryRepeating(root => root.Get("entities")[0].Get("components").Set("proof_plan_history", Content.Json.Parse("{\"group\":\"protected.history\",\"enabled\":true}")));
            SimGame game = ConstructionPlanTests.NewPoints(factory);
            long id = DependencySubmit(c, game, DependencyBuild(game, 12, "protected.history"));
            c.That(TestKit.Order(game, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { id }) }).Accepted, "public retained terminal plan has no hidden execution");
            c.Equal(PlanCodes.HistoryProvider, View(game).PreviewRetirePlanHistory(History(id)).Blocker, "public-only authored component reference is discovered without scheduler modifications");
            c.Equal(PlanCodes.HistoryProvider, Retire(game, History(id)).Refusal, "module reference uses same retirement command refusal");
            c.That(TemplateOwner(game).Def.AuthoringPath.Contains("recurring-templates.json"), "retention provider comes from explicitly master-listed JSON");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "authored reference and actual terminal history save in existing owners");
            var component = TemplateOwner(game).Get<ProofPlanHistoryRetention>();
            component.ReadFault = () => component.Entity.Touch();
            c.Throws<InvalidOperationException>(() => View(game).PreviewRetirePlanHistory(History(id)), "impure component reference observation faults instead of deleting history");
            component.ReadFault = null;
            c.Equal(1, Group(game, "protected.history").Count, "impure observation cannot retire a prefix");
        }
        private static void HistoryController(Check c)
        {
            GameFactory factory = PlayerControllerTests.Factory(edit: root => {
                var profile = root.Get("plan_playbooks")[0];
                profile.Set("cadence", Content.Json.Parse("{\"numerator\":100,\"denominator\":1,\"unit\":\"minute\"}"));
            });
            SimGame game = ConstructionPlanTests.NewPoints(factory);
            c.That(TestKit.Order(game, 0, new SetControllerCommand { Controller = Controller.Autopilot }).Accepted, "normal engine strategy starts selected authored playbook");
            Tick(game); var plans = TemplatePlans(game);
            c.Equal(2, plans.Count, "actual template reply exists before slow playbook consumes it");
            c.That(TestKit.Order(game, 0, new SetControllerCommand { Controller = Controller.Human }).Accepted, "human takeover preserves dormant strategy references");
            c.That(TestKit.Order(game, 0, new CancelPlanGroupCommand { Group = "autopilot.field" }).Accepted, "player ends the strategy's actual native plans");
            var selection = new PlanHistorySelection(plans.ConvertAll(p => p.Id));
            c.Equal(PlanCodes.HistoryProvider, View(game).PreviewRetirePlanHistory(selection).Blocker, "unconsumed accepted controller reply retains its actual identities while dormant");
            c.Equal(PlanCodes.HistoryProvider, Retire(game, selection).Refusal, "dormant controller retirement refusal matches preview");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "unconsumed native reply and terminal histories remain durable");
            // Isolated provider codec proof: exact cursor is the already authored controller state.
            var controller = game.World.Content.Get<PlanPlaybookDef>("field.playbook").Create();
            using (var stream = new System.IO.MemoryStream())
            {
                var writer = new StateWriter(stream); int step = 0, pending = 0; long head = plans[0].Id;
                writer.Int(ref step); writer.Int(ref pending); writer.Long(ref head); writer.Flush();
                byte[] bytes = stream.ToArray(); ((IPlayerControllerState)controller).Sync(new StateReader(bytes, 0, bytes.Length));
            }
            var reference = (IPlanHistoryRetention)controller;
            c.That(reference.RetainsPlan(game.World, new PlayerId(0), plans[0].Id) && reference.RetainsPlan(game.World, new PlayerId(0), plans[1].Id)
                && !reference.RetainsPlan(game.World, new PlayerId(0), plans[1].Id + 1), "authored playbook cursor retains exactly its actual contiguous admitted range");
        }
        private static void HistoryPhysical(Check c)
        {
            GameFactory factory = Dependencies(); SimGame game = ConstructionPlanTests.NewPoints(factory);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current physical history identity");
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000); using var recording = game.RecordReplay(limits);
            EmergencyPreemptionTests.Configure(c, game);
            long first = DependencySubmit(c, game, DependencyBuild(game, 12, "physical.history"));
            long second = DependencySubmit(c, game, DependencyBuild(game, 16, "physical.history", first));
            DependencyEdit(c, game, "physical.history", false);
            c.Equal(PlanCodes.NotEnded, Retire(game, History(first, second)).Refusal, "active physical work cannot be retired as terminal history");
            c.That(Group(game, "physical.history").Exists(p => p.State == PlanState.Active), "refused retirement preserves real native execution");
            EditingReach(game, () => Group(game, "physical.history").TrueForAll(p => p.State == PlanState.Done), "physical history completion");
            c.Equal(2, TemplateCount(game, "point.tower"), "machines physically finish both dependent structures before retirement");
            var supply = game.World.Content.ResourceId("supply");
            c.Equal(Fix.FromInt(12), FindPoint(game, "source").Get<IInventory>().Amount(supply), "physical history consumes eight finite units");
            c.Equal(PlanCodes.HistoryDependency, View(game).PreviewRetirePlanHistory(History(first)).Blocker, "completed successor retains real completed predecessor");
            var both = History(second, first); c.That(View(game).PreviewRetirePlanHistory(both).Ready, "unreferenced completed whole physical graph is eligible");
            var beforeViews = Group(game, "physical.history"); ulong before = game.Hash(); long at = game.Clock.StepIndex;
            game.Clock.Pause(); game.Seat(0).Submit(new RetirePlanHistoryCommand { Selection = both });
            game.Seat(0).Submit(new RepeatPlanCommand { Plan = first });
            game.Seat(0).Submit(new SubmitPlanCommand { Intent = DependencyBuild(game, 24, "physical.history") });
            SimGame twin = factory.Load(game.Save());
            game.Update(); c.Equal(at, game.Clock.StepIndex, "paused single clock cannot execute pending retirement");
            c.Equal(2, Group(game, "physical.history").Count, "pause retains actual terminal history until ordinary Update");
            game.Clock.Resume(); twin.Clock.Resume(); Tick(game); Tick(twin);
            var results = game.World.LastTickResults;
            c.That(results.Count == 3 && results[0].Accepted && results[0].Arg0 == first && results[0].Arg1 == 2
                && results[1].Refusal == Codes.NotFound && results[2].Accepted && results[2].Arg0 > second, "saved command order retires real IDs then refuses stale repeat and admits fresh monotonic ID");
            c.Equal(game.Hash(), twin.Hash(), "saved pending retirement/repeat/new-plan twins agree");
            c.Equal(1, Group(game, "physical.history").Count, "same label names only genuinely new history after retirement");
            c.Equal(2, beforeViews.Count, "retained immutable host view does not lose its old records");
            c.Equal(2, TemplateCount(game, "point.tower"), "history retirement does not erase completed structures");
            c.Equal(Fix.FromInt(12), FindPoint(game, "source").Get<IInventory>().Amount(supply), "retirement refunds or consumes no physical materials");
            c.That(game.Hash() != before, "only actual lifecycle/commands change authoritative state");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "physical world and actual retired ID gaps reload without compatibility reader");
            for (int i = 0; i < 4; i++) { Tick(game); Tick(twin); }
            c.Equal(game.Hash(), twin.Hash(), "continued sessions remain deterministic after retirement");
            var replay = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
            c.That(replay.Success && replay.ActualHash == game.Hash(), "complete physical construction and saved retirement replay to final live hash");
            Console.WriteLine("physical history retirement replay: initial=" + recording.InitialStep + ";final=" + game.Tick
                + ";commands=" + recording.CommandCount + ";checkpoints=" + recording.CheckpointCount
                + ";expected-hash=" + game.Hash().ToString("X16") + ";actual-hash=" + replay.ActualHash.ToString("X16"));
        }
        private static void HistoryFailedExecution(Check c)
        {
            GameFactory factory = Dependencies(failed: true, constructionEdit: authored =>
                authored.Get("target_resource_requirements")[0].Set("use", JsonValue.Of("hold")), recovery: null);
            SimGame game = ConstructionPlanTests.NewPoints(factory);
            EmergencyPreemptionTests.Configure(c, game);
            long id = DependencySubmit(c, game, DependencyBuild(game, 12, "failed.history"));
            DependencyEdit(c, game, "failed.history", false);
            EditingReach(game, () => TemplatePlan(game, id).State == PlanState.Failed, "retained failed physical site");
            c.Equal(PlanCodes.HistoryExecution, View(game).PreviewRetirePlanHistory(History(id)).Blocker, "real failed site retains native disposition and its plan history");
            c.Equal(PlanCodes.HistoryExecution, Retire(game, History(id)).Refusal, "unresolved physical disposition cannot be erased through history cleanup");
            c.Equal(1, TemplateCount(game, "point.tower-site"), "refused history cleanup preserves the actual failed site");
            c.Equal(Fix.FromInt(16), FindPoint(game, "source").Get<IInventory>().Amount(game.World.Content.ResourceId("supply")), "failed work's physically delivered inputs are never refunded by retirement");
            c.Equal(Fix.FromInt(4), game.World.Entities.Get(TemplatePlan(game, id).Target).Get<IInventory>().Amount(game.World.Content.ResourceId("supply")),
                "unresolved disposition is actual site stock, not a copied history balance");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "failed native disposition and history retain their existing codecs");
        }
        private static PlayerControllerFacts HistoryControllerFacts(SimGame game)
        {
            var rows = new List<PlayerControllerFacts>(); game.Seat(0).View.Feature<PlayerControllersView>().CopyControllers(rows);
            return rows[0];
        }
        private static void HistoryControllerPhysical(Check c)
        {
            GameFactory factory = PlayerControllerTests.Factory(constructionEdit: authored => {
                authored.Get("target_resource_requirements")[0].Get("quantities").Set("supply", JsonValue.Number("4"));
                var nodes = authored.Get("activities")[0].Get("nodes");
                nodes[0].Set("duration", Content.Json.Parse("{\"numerator\":1,\"denominator\":5,\"unit\":\"hour\"}")); nodes[1].Set("duration", Content.Json.Parse("{\"numerator\":3,\"denominator\":10,\"unit\":\"hour\"}"));
            });
            SimGame game = ConstructionPlanTests.NewPoints(factory); EmergencyPreemptionTests.Configure(c, game);
            c.That(TestKit.Order(game, 0, new SetControllerCommand { Controller = Controller.Autopilot }).Accepted, "physical playbook uses normal engine controller");
            EditingReach(game, () => HistoryControllerFacts(game).Decisions >= 2 && HistoryControllerFacts(game).PendingAnswers == 0,
                "playbook consumes actual accepted plan IDs");
            c.That(Group(game, "autopilot.field").Count == 2 && Group(game, "autopilot.field").Exists(p => p.State != PlanState.Done),
                "strategy cursor is bound before its actual machines finish");
            c.That(TestKit.Order(game, 0, new SetControllerCommand { Controller = Controller.Human }).Accepted, "player suspends strategy while native physical work continues");
            EditingReach(game, () => Group(game, "autopilot.field").TrueForAll(p => p.State == PlanState.Done), "dormant playbook's real physical completion");
            var selection = new PlanHistorySelection(Group(game, "autopilot.field").ConvertAll(p => p.Id));
            c.Equal(PlanCodes.HistoryProvider, View(game).PreviewRetirePlanHistory(selection).Blocker, "completed actual plans remain referenced by dormant real playbook cursor");
            c.Equal(Fix.FromInt(12), FindPoint(game, "source").Get<IInventory>().Amount(game.World.Content.ResourceId("supply")),
                "playbook uses real finite construction supply");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "dormant strategy cursor restores with actual completed histories");
            foreach (var current in new[] { game, twin })
                c.That(TestKit.Order(current, 0, new SetControllerCommand { Controller = Controller.Autopilot }).Accepted,
                    "resumed ordinary strategy observes actual Done and releases its own cursor");
            EditingReach(game, () => View(game).PreviewRetirePlanHistory(selection).Ready, "completed playbook releases history", twin);
            foreach (var current in new[] { game, twin }) c.That(Retire(current, selection).Accepted, "retirement succeeds only after actual strategy releases reference");
            c.Equal(game.Hash(), twin.Hash(), "physical strategy completion and retirement stay identical after saved takeover");
            c.That(TemplatePlans(game).Count == 0 && TemplateCount(game, "point.tower") == 2,
                "retiring released strategy history neither resubmits work nor destroys physical structures");
        }
    }
}
