using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    public static partial class PlanTemplateTests
    {
        private static Code TemplateCode(bool provider) => provider ? PublicTemplate : NativeTemplate;
        private static CommandResult SelectRecurring(SimGame game, bool provider, RecurringTemplateSettings settings, int seat = 0)
            => TestKit.Order(game, seat, new ConfigureRecurringTemplateCommand { Entity = TemplateOwner(game).Id, Source = TemplateCode(provider), Settings = settings });
        private static RecurringTemplateSettings FutureTemplate(SimGame game, long id, long revision, int at, string group)
            => new RecurringTemplateSettings(PlanTemplateSelection.Stored(id, revision), At(game, at), group);
        private static GameFactory LibraryRepeating(Action<JsonValue> edit = null)
            => Repeating(edit, authored => {
                authored.Get("target_resource_requirements")[0].Get("quantities").Set("supply", JsonValue.Number("4"));
                var nodes = authored.Get("activities")[0].Get("nodes");
                nodes[0].Set("duration", Content.Json.Parse("{\"numerator\":1,\"denominator\":5,\"unit\":\"hour\"}")); nodes[1].Set("duration", Content.Json.Parse("{\"numerator\":3,\"denominator\":10,\"unit\":\"hour\"}"));
            });
        private static void RecurringLibraryContracts(Check c)
        {
            foreach (bool provider in new[] { false, true })
            {
                GameFactory factory = LibraryRepeating(); SimGame game = ConstructionPlanTests.NewPoints(factory);
                PlanTemplate design = View(game).AuthoredPlanTemplate("repeat.pair"); long id = StoreTemplate(game, design, "repeat").Arg0;
                var future = FutureTemplate(game, id, 1, 40, "future.field");
                c.Equal(Code.None, View(game).ReadRecurringTemplate(TemplateOwner(game).Id, TemplateCode(provider), out var initial), "native/public initial settings come from explicit master template/anchor/group");
                c.That(initial.Selection.Kind == PlanTemplateSelectionKind.Snapshot && initial.Group == "field.repeat", "initial authoring is explicit snapshot, never fallback library content");
                ulong before = game.Hash();
                c.That(View(game).PreviewRecurringTemplate(TemplateOwner(game).Id, TemplateCode(provider), future).Ready, "future source preview shares board structural provider checks");
                c.Equal(before, game.Hash(), "future source preview has no job/plan/library/clock mutation");
                c.Equal(Codes.NotOwned, SelectRecurring(game, provider, future, 1).Refusal, "other company cannot configure this actual recurring capability");
                c.Equal(Codes.NotFound, View(game, 1).ReadRecurringTemplate(TemplateOwner(game).Id, TemplateCode(provider), out var hidden), "foreign source future settings remain private");
                c.That(hidden == null, "foreign future selection exposes no private payload");
                long foreign = StoreTemplate(game, design, "foreign", seat: 1).Arg0;
                c.Equal(Codes.NotFound, SelectRecurring(game, provider, FutureTemplate(game, foreign, 1, 40, "foreign")).Refusal, "source cannot resolve another company's blueprint");
                c.Equal(PlanCodes.TemplateRevisionConflict, SelectRecurring(game, provider, FutureTemplate(game, id, 2, 40, "stale")).Refusal, "future selection requires exact actual private revision");
                c.Equal(Codes.InvalidArgument, SelectRecurring(game, provider, null).Refusal, "no fabricated future settings");
                c.That(SelectRecurring(game, provider, future).Accepted, "native/public source uses same future selection command contract");
                c.That(TemplateSource(game, provider).TemplateSettings.Selection.Id == id && TemplateSource(game, provider).TemplateSettings.Group == "future.field",
                    "ordinary recurring projection includes immutable future selection");
                c.That(initial.Selection.Kind == PlanTemplateSelectionKind.Snapshot && initial.Anchor.Get("position").Text == "12", "retained future settings are detached, not overwritten");
                c.Equal(0, TemplatePlans(game).Count, "disabled future selection config creates no work");
                JsonValue anchor = At(game, 50); var captured = new RecurringTemplateSettings(PlanTemplateSelection.Stored(id, 1), anchor, "captured"); anchor.Set("position", JsonValue.Number("99"));
                c.Equal("50", captured.Anchor.Get("position").Text, "future anchor clones caller values");
                c.Throws<InvalidOperationException>(() => captured.Anchor.Set("position", JsonValue.Number("80")), "future anchor remains frozen");
                c.Throws<ArgumentNullException>(() => new RecurringTemplateSettings(null, At(game, 12), "bad"), "future source requires explicit selection");
                c.Throws<ArgumentException>(() => new RecurringTemplateSettings(future.Selection, null, "bad"), "future source requires explicit anchor");
                c.Throws<ArgumentException>(() => new RecurringTemplateSettings(future.Selection, At(game, 12), "bad name"), "future source requires stable group");
                var invalid = new RecurringTemplateSettings(future.Selection, JsonValue.NewObject(), "invalid");
                var preview = View(game).PreviewRecurringTemplate(TemplateOwner(game).Id, TemplateCode(provider), invalid);
                c.That(preview.Blocker == Codes.InvalidArgument && preview.Entries.Count == 2, "malformed anchor reports all structural entry checks");
                c.Equal(preview.Blocker, SelectRecurring(game, provider, invalid).Refusal, "future structural refusal preview/command parity");
                var huge = new PlanTemplate(design.Space, new[] { new PlanTemplateEntry("huge", design.Entries[0].Definition, design.Entries[0].Arguments, int.MaxValue, false) });
                c.Equal(OperationPolicyCodes.PriorityRange, SelectRecurring(game, provider, new RecurringTemplateSettings(PlanTemplateSelection.Snapshot(huge), At(game, 60), "huge")).Refusal,
                    "current source priority plus selected entry priority refuses without overflow/fault");
                c.That(StoreTemplate(game, design, "repeat", id, 1).Accepted, "player revises selected private design");
                var source = TemplateSource(game, provider);
                c.That(source.Blocker == RecurringPlanCodes.Disabled && source.RequestBlocker == PlanCodes.TemplateRevisionConflict, "disabled precedence does not hide typed broken future request");
                c.That(ConfigureTemplate(game, provider, false).Accepted, "broken future revision does not prevent common disable policy");
                c.That(SelectRecurring(game, provider, FutureTemplate(game, id, 2, 60, "repaired")).Accepted, "player explicitly repairs selected revision/anchor/group");
                byte[] wire = DependencyWire(new ConfigureRecurringTemplateCommand { Entity = TemplateOwner(game).Id, Source = TemplateCode(provider), Settings = captured });
                var command = new ConfigureRecurringTemplateCommand(); command.Sync(new StateReader(wire, 0, wire.Length));
                c.Equal(Convert.ToBase64String(wire), Convert.ToBase64String(DependencyWire(command)), "future settings wire reuses selection/JSON codec");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "native/public future private selection saves without requiring a valid old reference");
                var singleFactory = LibraryRepeating(root => {
                    JsonValue row = TemplateRow(root, provider); row.Set("template", JsonValue.Null()); row.Set("plan", JsonValue.Of("build:point.tower"));
                    row.Set("target", JsonValue.Of("authored_intent")); row.Set("arguments", Content.Json.Parse("{\"placement\":{\"space\":\"field.points\",\"position\":12}}"));
                });
                SimGame singleSource = ConstructionPlanTests.NewPoints(singleFactory);
                c.Equal(RecurringPlanCodes.NotTemplate, View(singleSource).ReadRecurringTemplate(TemplateOwner(singleSource).Id, TemplateCode(provider), out _), "single-plan source does not fabricate template settings");
                var singleSettings = new RecurringTemplateSettings(PlanTemplateSelection.Snapshot(design), At(singleSource, 12), "single");
                c.Equal(RecurringPlanCodes.NotTemplate, SelectRecurring(singleSource, provider, singleSettings).Refusal, "single-plan source explicitly refuses future template control");
            }
            c.Throws<ArgumentException>(() => new PlanOrigin(new EntityId(1), NativeTemplate, 0, true), "recurring provenance requires actual cycle-head plan ID");
            RecurringLibraryInbox(c); RecurringLibraryFaults(c);
        }
        private static void RecurringLibraryInbox(Check c)
        {
            GameFactory factory = LibraryRepeating(); SimGame game = ConstructionPlanTests.NewPoints(factory); var design = View(game).AuthoredPlanTemplate("repeat.pair");
            long id = StoreTemplate(game, design, "inbox").Arg0;
            game.Seat(0).Submit(new StorePlanTemplateCommand { Template = id, ExpectedRevision = 1, Name = "inbox", Value = design });
            game.Seat(0).Submit(new ConfigureRecurringTemplateCommand { Entity = TemplateOwner(game).Id, Source = NativeTemplate, Settings = FutureTemplate(game, id, 1, 40, "stale.inbox") });
            SimGame twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.That(TemplateSource(game, false).TemplateSettings.Selection.Kind == PlanTemplateSelectionKind.Snapshot && Library(game)[0].Revision == 2,
                "saved ordered stale configuration refuses after earlier library revision with no partial future change");
            c.Equal(game.Hash(), twin.Hash(), "saved future-settings inbox has deterministic refusal semantics");
        }
        private static void RecurringLibraryFaults(Check c)
        {
            GameFactory factory = LibraryRepeating(root => TemplateRow(root, true).Set("fault", JsonValue.Number("14")));
            SimGame game = ConstructionPlanTests.NewPoints(factory); long id = StoreTemplate(game, View(game).AuthoredPlanTemplate("repeat.pair"), "fault").Arg0;
            long time = game.Clock.StepIndex; game.Seat(0).Submit(new ConfigureRecurringTemplateCommand { Entity = TemplateOwner(game).Id, Source = PublicTemplate, Settings = FutureTemplate(game, id, 1, 40, "fault") });
            game.Update(); c.That(game.Fault != null && game.Clock.StepIndex == time, "public provider ignoring future configuration faults before sole clock completion");
            c.Throws<InvalidOperationException>(() => game.Save(), "faulted future configuration cannot checkpoint partial authority");
            factory = LibraryRepeating(root => {
                TemplateRow(root, true).Set("fault", JsonValue.Number("15"));
                root.Get("plan_templates")[0].Get("entries")[1].Set("after", Content.Json.Parse("[\"first\"]"));
            });
            game = ConstructionPlanTests.NewPoints(factory); time = game.Clock.StepIndex; game.Update();
            c.That(game.Fault != null && game.Clock.StepIndex == time, "public source forging missing dependency links faults in normal Update before clock completion");
            c.Throws<InvalidOperationException>(() => game.Save(), "forged dependency request cannot checkpoint partial authority");
        }
        private static void RecurringLibraryPhysical(Check c)
        {
            foreach (bool provider in new[] { false, true })
            {
                GameFactory factory = LibraryRepeating(root => root.Get("plan_templates")[0].Get("entries")[1].Set("after", Content.Json.Parse("[\"first\"]")));
                SimGame game = ConstructionPlanTests.NewPoints(factory); PlanTemplate original = View(game).AuthoredPlanTemplate("repeat.pair");
                long id = StoreTemplate(game, original, "physical.repeat").Arg0;
                c.That(SelectRecurring(game, provider, FutureTemplate(game, id, 1, 12, "old.field")).Accepted, "player selects private recurring blueprint");
                EmergencyPreemptionTests.Configure(c, game); c.That(ConfigureTemplate(game, provider, true).Accepted, "ordinary enable admits genuine first physical cycle");
                long first = TemplateSource(game, provider).Schedule.LastPlan; string group = TemplatePlan(game, first).Group;
                c.That(first > 0 && TemplatePlan(game, first + 1).Dependencies[0] == first, "stored recurring DAG binds actual native member identities");
                c.That(TemplatePlan(game, first).Origin.CycleHead == first && TemplatePlan(game, first + 1).Origin.CycleHead == first && TemplatePlan(game, first).Origin.TemplateCycle,
                    "all actual members retain existing provenance with the same native head identity, never a new cycle counter");
                TemplateReach(game, () => game.World.Services.Get<WorkBoard>().Order(TemplatePlan(game, first).Order)?.Node("build").Progress.WholeTicks > 0, "stored recurring partial physical work");
                var single = new PlanTemplate(original.Space, new[] { original.Entries[0] }); SimGame twin = factory.Load(game.Save());
                foreach (SimGame current in new[] { game, twin })
                {
                    c.That(StoreTemplate(current, single, "physical.repeat", id, 1).Accepted, "future design revision changes membership without rewriting active two-member cycle");
                    var source = TemplateSource(current, provider);
                    c.That(source.Blocker == RecurringPlanCodes.Outstanding && source.RequestBlocker == PlanCodes.TemplateRevisionConflict, "outstanding physical work precedes stale future source blocker");
                    c.That(SelectRecurring(current, provider, FutureTemplate(current, id, 2, 40, "next.field")).Accepted,
                        "future anchor/group/single-member blueprint can change while old cycle remains live");
                    c.That(RemoveTemplate(current, id, 2).Accepted, "future library deletion cannot erase admitted native cycle");
                    c.That(TemplateSource(current, provider).Blocker == RecurringPlanCodes.Outstanding && TemplateSource(current, provider).RequestBlocker == Codes.NotFound,
                        "deleted future selection never faults or replaces outstanding history");
                    DependencyEdit(c, current, group, false);
                }
                c.That(Group(game, group).Count == 2 && TemplatePlan(game, first + 1).Dependencies[0] == first, "old cycle keeps exact frozen membership and ancestry after future edits");
                c.Equal(game.Hash(), twin.Hash(), "native/public revised-source physical saved twins agree");
                twin = factory.Load(game.Save()); ulong before = game.Hash(); long time = game.Clock.StepIndex; game.Clock.Pause();
                for (int i = 0; i < 4; i++) game.Update();
                c.Equal(before, game.Hash(), "one clock pause freezes old cycle despite unavailable future library");
                c.Equal(time, game.Clock.StepIndex, "recurrence has no independent clock"); game.Clock.Resume();
                TemplateReach(game, () => TemplatePlan(game, first + 1).State == PlanState.Done, "old cycle completion after future deletion", twin);
                c.Equal(2, TemplateCount(game, "point.tower"), "deleted future source still allows both original machines' physical projects to finish");
                c.Equal(Fix.FromInt(12), FindPoint(game, "source").Get<IInventory>().Amount(game.World.Content.ResourceId("supply")), "old cycle consumes eight finite units, not blueprint revision resources");
                c.That(TemplateSource(game, provider).Blocker == Codes.NotFound && TemplateSource(game, provider).Schedule.LastPlan == first,
                    "completed old cycle cannot auto-fallback to authored or same-name replacement design");
                foreach (SimGame current in new[] { game, twin })
                {
                    var replacement = StoreTemplate(current, single, "physical.repeat");
                    c.That(replacement.Arg0 > id && TemplateSource(current, provider).Blocker == Codes.NotFound, "same-name recreation cannot silently redirect recurrence");
                    c.That(SelectRecurring(current, provider, FutureTemplate(current, replacement.Arg0, 1, 40, "new.field")).Accepted, "explicit new source identity admits next ordinary cycle at chosen anchor");
                    c.That(ConfigureTemplate(current, provider, false).Accepted, "player disables future repetition without cancelling current actual work");
                }
                long next = TemplateSource(game, provider).Schedule.LastPlan;
                c.That(next > first + 1 && TemplatePlans(game).Count == 3 && TemplatePlan(game, next).Arguments.Get("placement").Get("position").Text == "40",
                    "new cycle uses selected single-member shape and provider anchor, never old membership");
                TemplateReach(game, () => TemplatePlan(game, next).State == PlanState.Done, "new selected cycle completion", twin);
                c.Equal(Fix.FromInt(8), FindPoint(game, "source").Get<IInventory>().Amount(game.World.Content.ResourceId("supply")), "next cycle consumes four additional physical units");
                c.Equal(game.Hash(), twin.Hash(), "different future cycle shapes preserve saved physical deterministic continuation");
                CorruptDependencies(c, game, first + 1, Array.Empty<long>(), "older cycle lost ancestry is rejected even after a different latest cycle");
                CorruptCycleTemplate(c, game, first, null, "older cycle cannot lose frozen template after source advances");
                CorruptCycleTemplate(c, game, first + 1, original, "trailing member cannot impersonate frozen cycle head");
                CorruptCycleTemplate(c, game, next, null, "single-entry template cycle cannot impersonate an ordinary single-plan cycle");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "complete historical cycles and future control have one durable board authority");
                RecurringLibraryCancellation(c, provider);
            }
        }
        private static void CorruptCycleTemplate(Check c, SimGame game, long id, PlanTemplate replacement, string message)
        {
            var entries = (IEnumerable)typeof(WorkBoard).GetField("_plans", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(game.World.Services.Get<WorkBoard>());
            foreach (object entry in entries)
                if ((long)entry.GetType().GetField("Id", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(entry) == id)
                {
                    FieldInfo field = entry.GetType().GetField("CycleTemplate", BindingFlags.Instance | BindingFlags.NonPublic); object old = field.GetValue(entry);
                    field.SetValue(entry, replacement);
                    try { c.Throws<CorruptStateException>(() => game.Save(), message); } finally { field.SetValue(entry, old); }
                    return;
                }
            throw new InvalidOperationException("missing actual historical cycle member");
        }
        private static void RecurringLibraryCancellation(Check c, bool provider)
        {
            GameFactory factory = LibraryRepeating(root => {
                var rows = root.Get("plan_templates")[0].Get("entries");
                rows[0].Set("paused", JsonValue.Of(true)); rows[1].Set("after", Content.Json.Parse("[\"first\"]"));
            });
            SimGame game = ConstructionPlanTests.NewPoints(factory); var original = View(game).AuthoredPlanTemplate("repeat.pair");
            long id = StoreTemplate(game, original, "cancel.repeat").Arg0;
            c.That(SelectRecurring(game, provider, FutureTemplate(game, id, 1, 12, "cancel.old")).Accepted && ConfigureTemplate(game, provider, true).Accepted,
                "ordinary stored source admits paused cancellable graph");
            long first = TemplateSource(game, provider).Schedule.LastPlan;
            c.That(TestKit.Order(game, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { first }) }).Accepted, "player cancels actual stored recurring prerequisite");
            DependencyEdit(c, game, TemplatePlan(game, first).Group, false);
            var single = new PlanTemplate(original.Space, new[] { original.Entries[0] });
            c.That(StoreTemplate(game, single, "cancel.repeat", id, 1).Accepted && SelectRecurring(game, provider, FutureTemplate(game, id, 2, 40, "cancel.next")).Accepted,
                "future single-member selection does not reinterpret cancelled two-member ancestry");
            var source = TemplateSource(game, provider);
            c.That(source.Blocker == RecurringPlanCodes.Outstanding && TemplatePlan(game, first + 1).Blocker == PlanCodes.DependencyCancelled && TemplatePlans(game).Count == 2,
                "cancelled predecessor is never treated as completion or erased by future shape change");
            c.Equal(0, TemplateCount(game, "point.tower-site"), "cancelled paused ancestry creates no free site");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled old cycle and revised future shape share strict saved history");
        }
    }
}
