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
        private static GameFactory RemovalTemplates(bool point, Action<JsonValue> edit = null)
        {
            JsonValue templates = Content.Json.Parse(FrameworkProofContent.Read("removal-templates.json")); edit?.Invoke(templates);
            if (point) return ConstructionPlanTests.Points(additionalDocuments: new Dictionary<string, string> { ["removal-templates.json"] = templates.ToString() });
            return Colony.Factory(FrameworkProofContent.Read("resource-recovery.json"), FrameworkProofContent.Read("endpoints-flows.json"),
                additionalDocuments: new Dictionary<string, string> {
                    ["removal-workflows.json"] = FrameworkProofContent.Read("removal-workflows.json"), ["removal-templates.json"] = templates.ToString() });
        }
        private static Entity TemplateTarget(SimGame game, string prototype)
        { foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == prototype) return entity; return null; }
        private static Fix TemplateStock(SimGame game, ResourceId resource)
        {
            Fix total = Fix.Zero; foreach (Entity entity in game.World.Entities.All)
                if (entity.Owner == new PlayerId(0) || entity.Def.Id == "recovery.container")
                { IInletPort port = entity.Get<IInletPort>(); if (port != null) total += port.Amount(resource); }
            return total;
        }
        private static Entity PointFixture(SimGame game, string prototype, int owner, int position)
            => game.World.Spawn(game.World.Content.Get<EntityDef>(prototype), new PlayerId(owner),
                entity => ((ISpatialPlacement)game.World.Space).ConfigurePlacement(entity, new PointPlacement(game.World.Space.Id, Fix.FromInt(position))));
        private static PlanIntent RemovalIntent(SimGame game, Entity target, string group, IActivityWorkerSelection workers = null)
        {
            PlanIntent intent = target.Def.Component<IConstructionDefinition>().Demolition.CreateTargetIntent(target.Id, workers ?? ActivityWorkerSelection.Unbound, group, 70);
            return new PlanIntent(intent.Definition, intent.Arguments, group, intent.Priority, true);
        }
        private static void BuildTemplateTarget(Check c, SimGame game, string prototype, int at, string group)
        {
            var arguments = JsonValue.NewObject(); arguments.Set("placement", At(game, at));
            c.That(TestKit.Order(game, 0, new SubmitPlanCommand { Intent = new PlanIntent("build:" + prototype, arguments, group, 70, false) }).Accepted, "player orders real facility, not an instant template target");
            Until(game, () => Group(game, group).TrueForAll(p => p.State == PlanState.Done), group);
        }
        private static void RemovalTemplateContracts(Check c)
        {
            GameFactory factory = RemovalTemplates(true); SimGame game = ConstructionPlanTests.NewPoints(factory);
            Entity target = PointFixture(game, "point.tower", 0, 12);
            Entity translated = PointFixture(game, "point.tower", 0, 30);
            Entity foreign = PointFixture(game, "point.tower", 1, 40); game.World.Settle();
            PlanTemplate authored = View(game).AuthoredPlanTemplate("removal.point"); ulong hash = game.Hash();
            c.That(game.World.Content.Get<PlanTemplateDef>("removal.point").AuthoringPath.Contains("removal-templates.json"), "removal library comes through the existing explicit master document");
            c.That(View(game).PreviewPlanTemplate(authored, At(game, 12), "original").Ready, "mixed removal/construction template admits entirely through existing board");
            PlanGroupCheck missing = View(game).PreviewPlanTemplate(authored, At(game, 60), "missing");
            c.That(missing.Blocker == Codes.NotFound && missing.Entries.Count == 2 && missing.Entries[0].Blocker == Codes.NotFound && missing.Entries[1].Blocker.IsNone, "missing target has truthful per-entry refusal, not generic malformed geometry");
            c.Equal(Codes.NotOwned, View(game).PreviewPlanTemplate(authored, At(game, 40), "foreign").Blocker, "foreign translated prototype never becomes a default target");
            c.Equal(hash, game.Hash(), "missing/private previews allocate no IDs or mutate clock/stock");
            c.That(Place(game, authored, 60, "missing").Refusal == Codes.NotFound && Group(game, "missing").Count == 0, "late valid entry cannot create any prefix after target refusal");
            c.That(TestKit.Order(game, 0, new SubmitPlanCommand { Intent = RemovalIntent(game, target, "capture") }).Accepted, "ordinary paused removal intent can be captured");
            c.That(View(game).CapturePlanTemplate("capture", At(game, 10), out PlanTemplate portable).Ready, "removal provider captures relative target geometry");
            c.Equal("2", portable.Entries[0].Arguments.Get("placement").Get("offset").Text, "provider owns exact relative placement");
            c.That(!portable.ToJson().ToString().Contains("target") && !portable.ToJson().ToString().Contains("worker") && !portable.ToJson().ToString().Contains("order"), "portable removal has no live target, machine or execution identity");
            c.That(Place(game, portable, 28, "translated").Accepted, "captured prototype resolves at translated placement");
            c.Equal(translated.Id.Value.ToString(), Group(game, "translated")[0].Arguments.Get("target").Text, "normal intent uses actual translated entity, never the original target ID");
            c.That(target.Id != translated.Id, "two distinct real facilities prove portable resolution");
            Entity duplicate = PointFixture(game, "point.tower", 0, 30); game.World.Settle();
            c.Equal(EntityPlacementTargets.Ambiguous, View(game).PreviewPlanTemplate(portable, At(game, 28), "ambiguous").Blocker, "coincident prototype ambiguity refuses without ID tie-break guessing");
            game.World.Despawn(duplicate); game.World.Settle();
            Entity machine = TemplateTarget(game, "machine");
            var selection = new ActivityWorkerSelection(new[] { new ActivityWorkerBinding("demolish", machine.Id) });
            c.That(TestKit.Order(game, 0, new SubmitPlanCommand { Intent = RemovalIntent(game, target, "bound", selection) }).Accepted, "explicit machine binding remains valid ordinary removal intent");
            c.Equal(PlanCodes.LiveBindings, View(game).CapturePlanTemplate("bound", At(game, 12), out _).Blocker, "capture cannot silently discard or copy a live machine selection");
            c.That(View(game, 1).CapturePlanTemplate("capture", At(game, 10), out PlanTemplate hidden).Blocker == Codes.NotFound && hidden == null, "foreign capture reveals no target geometry");
            foreach (Action<JsonValue> edit in new Action<JsonValue>[] {
                j => j.Set("target", JsonValue.Number("1")), j => j.Remove("placement"),
                j => j.Get("placement").Set("target", JsonValue.Number("1")), j => j.Get("placement").Set("space", JsonValue.Of("other")) })
            {
                JsonValue bad = portable.Entries[0].Arguments.Clone(); edit(bad);
                var invalid = new PlanTemplate(portable.Space, new[] { new PlanTemplateEntry("remove", portable.Entries[0].Definition, bad, 70, true) });
                c.Equal(Codes.InvalidArgument, View(game).PreviewPlanTemplate(invalid, At(game, 30), "invalid").Blocker, "strict portable schema accepts no live identity or foreign space");
            }
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "ordinary captured/translated intent saves with one board codec");
            var geometry = (ISpatialPlacementTemplates)game.World.Space;
            foreach (long raw in new[] { 0L, 1L, -1L, long.MinValue, long.MaxValue, Fix.Parse("12.123456").Raw })
            {
                var placement = new PointPlacement(game.World.Space.Id, Fix.FromRaw(raw));
                c.Equal(geometry.PlacementIdentity(placement), geometry.PlacementIdentity(geometry.ReadPlacement(geometry.WritePlacement(placement))), "provider serialization preserves all native bits without limits/rounding");
            }
            SimGame corrupt = ConstructionPlanTests.NewPoints(RemovalTemplates(true)); var space = (PointSpace)corrupt.World.Space;
            PointFixture(corrupt, "point.tower", 0, 12); corrupt.World.Settle();
            space.PlacementReadFault = () => corrupt.Clock.Pause();
            c.Throws<InvalidOperationException>(() => View(corrupt).PreviewPlanTemplate(View(corrupt).AuthoredPlanTemplate("removal.point"), At(corrupt, 12), "clock-fault"), "pure template lookup cannot mutate sole game clock");
            space.PlacementReadFault = null; corrupt.Clock.Resume();
            Entity observed = TemplateTarget(corrupt, "point.tower"); space.PlacementCaptureFault = () => observed.Touch();
            c.Throws<InvalidOperationException>(() => EntityPlacementTargets.Find(corrupt.World, new PlayerId(0), observed.Def, At(corrupt, 12), out _), "same scoped observation guard detects candidate mutation");
            space.PlacementCaptureFault = null;
            c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(RemovalTemplates(true, root => root.Get("plan_templates")[1].Get("entries")[0].Get("arguments").Set("target", JsonValue.Number("1")))), "bad master removal template refuses before Initialize");
        }
        private static void RemovalTemplatePhysical(Check c)
        {
            foreach (bool point in new[] { false, true })
            {
                GameFactory factory = RemovalTemplates(point); SimGame game = point ? ConstructionPlanTests.NewPoints(factory) : New(factory);
                if (point) EmergencyPreemptionTests.Configure(c, game);
                string prototype = point ? "point.tower" : "removal.tower"; BuildTemplateTarget(c, game, prototype, 12, "build.actual");
                Entity original = TemplateTarget(game, prototype); PlanTemplate template = View(game).AuthoredPlanTemplate(point ? "removal.point" : "removal.native");
                game.Seat(0).Submit(new PlacePlanTemplateCommand { Selection = PlanTemplateSelection.Snapshot(template), Anchor = At(game, 12), Group = "field.removal" });
                SimGame twin = factory.Load(game.Save()); Tick(game); Tick(twin);
                c.Equal(game.Hash(), twin.Hash(), "saved template command resolves identical actual target and grouped intent");
                c.That(Group(game, "field.removal").Count == 2 && Group(game, "field.removal").TrueForAll(p => p.State == PlanState.Pending), "paused mixed template creates no ghost structures or removal work");
                c.Equal(original.Id.Value.ToString(), Group(game, "field.removal")[0].Arguments.Get("target").Text, "real removal target resolves inside engine");
                foreach (SimGame current in new[] { game, twin }) c.That(TestKit.Order(current, 0, new ConfigurePlanGroupCommand { Group = "field.removal", Priority = 70, Paused = false }).Accepted, "player starts ordinary mixed group");
                c.That(Group(game, "field.removal")[1].Order == 0 && Group(game, "field.removal")[1].Blocker == PlanCodes.DependencyPending &&
                    Group(game, "field.removal")[1].Dependencies[0] == Group(game, "field.removal")[0].Id,
                    "native/point replacement explicitly waits for physically completed removal");
                Until(game, () => Group(game, "field.removal")[0].State == PlanState.Done, "field.removal", twin);
                c.That(!original.Alive, "template cannot remove original until physical authored work completes");
                c.Equal(game.Hash(), twin.Hash(), "physical removal, real salvage and later group work continue identically");
                Until(game, () => Group(game, "field.removal").TrueForAll(p => p.State == PlanState.Done), "field.removal", twin);
                c.Equal(game.Hash(), twin.Hash(), "mixed replacement/expansion finishes physically from saved state");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "removed-target history and new completed facility remain durable");
                c.Equal(Codes.NotFound, View(game).CapturePlanTemplate("field.removal", At(game, 12), out _).Blocker, "missing historical target is not silently substituted by its replacement");
                game.Clock.Pause(); ulong paused = game.Hash(); c.Equal(0, game.Update(), "sole game clock stops all template work"); c.Equal(paused, game.Hash(), "no template clock advances"); game.Clock.Resume();
                Entity replacement = TemplateTarget(game, point ? "point.tower" : "tower");
                c.That(replacement != null && replacement.Id != original.Id, "replacement/expansion is a newly built real facility, not copied state");
                c.Equal(Fix.FromInt(point ? 4 : 3), TemplateStock(game, game.World.Content.ResourceId(point ? "supply" : "steel")),
                    "finite real stock accounts for both builds, authored removal inputs and native salvage");
            }
        }
        private static GameFactory RemovalRepeating()
            => ConstructionPlanTests.Points(additionalModule: new PlanProofModule(),
                moduleDeclaration: "{\"id\":\"proof.plans\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"removal-recurring-templates.json\"]}",
                moduleDocument: FrameworkProofContent.Read("removal-recurring-templates.json"), scenarioEdit: scenario => scenario.Get("modules").Add(JsonValue.Of("proof.plans")), startupProfiles: new[] { "removal-recurring-templates.start" });
        private static void RemovalTemplateRecurrence(Check c)
        {
            foreach (bool provider in new[] { false, true })
            {
                GameFactory factory = RemovalRepeating(); SimGame game = ConstructionPlanTests.NewPoints(factory); EmergencyPreemptionTests.Configure(c, game);
                c.That(ConfigureTemplate(game, provider, true).Accepted, "native/public removal recurrence initializes without inventing a live target");
                TemplateAdvance(game, 3); c.That(TemplateSource(game, provider).Blocker == Codes.NotFound && TemplateSource(game, provider).Schedule.LastPlan == 0, "missing target remains a typed runtime prerequisite, not an authoring failure or free spawn");
                BuildTemplateTarget(c, game, "point.tower", 12, "first.facility");
                Until(game, () => TemplateSource(game, provider).Schedule.LastPlan > 0, "first.facility");
                long first = TemplateSource(game, provider).Schedule.LastPlan;
                c.Equal(first, TemplatePlan(game, first + 1).Dependencies[0], "native/public removal source preserves authored completion dependency");
                var all = new List<PlanView>(); View(game).CopyPlans(all); string group = all.Find(p => p.Id == first).Group;
                Until(game, () => Group(game, group)[0].State == PlanState.Done, group);
                c.Equal(2, Group(game, group).Count, "recurrence creates one whole removal/expansion group");
                c.That(Group(game, group)[1].State == PlanState.Pending && Group(game, group)[1].Paused, "later paused expansion remains ordinary outstanding work");
                c.Equal(RecurringPlanCodes.Outstanding, TemplateSource(game, provider).Blocker, "gone first target cannot hide the still-live admitted cycle");
                SimGame twin = factory.Load(game.Save()); TemplateAdvance(game, 3, twin); c.Equal(game.Hash(), twin.Hash(), "source history survives removed target without re-resolving old execution");
                c.Equal(first, TemplateSource(game, provider).Schedule.LastPlan, "live second member prevents another cycle or substituted target");
                foreach (SimGame current in new[] { game, twin })
                { c.That(ConfigureTemplate(current, provider, false).Accepted, "future recurrence can be disabled independently of admitted work"); c.That(TestKit.Order(current, 0, new ConfigurePlanGroupCommand { Group = group, Priority = 70, Paused = false }).Accepted, "player releases remaining admitted expansion"); }
                Until(game, () => Group(game, group).TrueForAll(p => p.State == PlanState.Done), group, twin);
                c.Equal(game.Hash(), twin.Hash(), "native/public source and completed cycle continue identically");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "terminal recurrence keeps its actual provenance without another history store");
            }
        }
    }
}
