using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.Text;
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
        private static readonly Code NativeTemplate = new Code("native.template"), PublicTemplate = new Code("public.template");
        private static JsonValue TemplateRow(JsonValue root, bool provider)
            => root.Get("entities")[0].Get("components").Get(provider ? "proof_plan_recurring" : "recurring_activity");
        private static GameFactory Repeating(Action<JsonValue> edit = null, Action<JsonValue> constructionEdit = null,
            IReadOnlyList<GameModule> extraModules = null, string extraDeclarations = null, IReadOnlyDictionary<string, string> extraDocuments = null, Action<JsonValue> scenarioEdit = null,
            IReadOnlyDictionary<string, string> additionalDocuments = null)
        {
            var root = Content.Json.Parse(FrameworkProofContent.Read("recurring-templates.json")); edit?.Invoke(root);
            var text = new StringBuilder(); root.WriteCanonical(text);
            return ConstructionPlanTests.Points(edit: constructionEdit, additionalModule: new PlanProofModule(),
                moduleDeclaration: "{\"id\":\"proof.plans\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"recurring-templates.json\"]}",
                moduleDocument: text.ToString(), extraModules: extraModules, extraDeclarations: extraDeclarations, extraDocuments: extraDocuments, additionalDocuments: additionalDocuments,
                scenarioEdit: scenario => { scenario.Get("modules").Add(JsonValue.Of("proof.plans")); scenarioEdit?.Invoke(scenario); }, startupProfiles: new[] { "recurring-templates.start" });
        }
        private static Entity TemplateOwner(SimGame game)
        {
            foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == "template.controller") return entity;
            throw new InvalidOperationException("missing authored template controller");
        }
        private static RecurringPlanView TemplateSource(SimGame game, bool provider)
        {
            Code code = View(game).ReadRecurringPlan(TemplateOwner(game).Id, provider ? PublicTemplate : NativeTemplate, out var value);
            if (!code.IsNone) throw new InvalidOperationException("template source refused: " + code);
            return value;
        }
        private static CommandResult ConfigureTemplate(SimGame game, bool provider, bool enabled, int priority = 60)
            => TestKit.Order(game, 0, new ConfigureRecurringPlanCommand { Entity = TemplateOwner(game).Id,
                Source = provider ? PublicTemplate : NativeTemplate,
                Settings = new RecurringPlanSettings(enabled, priority, GameDuration.Minutes(1), Code.None) });
        internal static List<PlanView> TemplatePlans(SimGame game)
        { var list = new List<PlanView>(); View(game).CopyPlans(list); return list; }
        internal static PlanView TemplatePlan(SimGame game, long id) => TemplatePlans(game).Find(p => p.Id == id);
        private static int TemplateCount(SimGame game, string definition)
        { int count = 0; foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == definition) count++; return count; }
        private static void TemplateReach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 480 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done()) throw new InvalidOperationException(stage + "; game update=" + game.Clock.StepIndex +
                "; native=" + TemplateSource(game, false).Blocker + "; public=" + TemplateSource(game, true).Blocker +
                "; plans=" + string.Join(";", TemplatePlans(game).ConvertAll(p => p.Id + "/" + p.State + "/" + p.Blocker)) +
                "; jobs=" + game.World.Services.Get<WorkBoard>().Jobs.Count);
        }
        private static void TemplateAdvance(SimGame game, int count, SimGame twin = null)
        { for (int i = 0; i < count; i++) { Tick(game); if (twin != null) Tick(twin); } }
        internal static void CompletionCadence(Check c)
        {
            foreach (bool provider in new[] { false, true })
            {
                var factory = Repeating(root =>
                {
                    TemplateRow(root, provider).Set("cadence", JsonValue.Of("successful_completion"));
                    TemplateRow(root, provider).Set("interval", Content.Json.Parse("{\"numerator\":40,\"denominator\":1,\"unit\":\"minute\"}"));
                });
                var game = ConstructionPlanTests.NewPoints(factory);
                Code source = provider ? PublicTemplate : NativeTemplate;
                c.That(TestKit.Order(game, 0, new ConfigureRecurringPlanCommand { Entity = TemplateOwner(game).Id, Source = source,
                    Settings = new RecurringPlanSettings(true, 60, GameDuration.Minutes(40), Code.None) }).Accepted,
                    "ordinary control admits authored successful-completion template cohort");
                long first = TemplateSource(game, provider).Schedule.LastPlan, second = first + 1;
                EmergencyPreemptionTests.Configure(c, game);
                TemplateReach(game, () => TemplatePlan(game, first).State == PlanState.Done, "first physical cohort member finishes");
                var partial = TemplateSource(game, provider);
                c.That(partial.Schedule.LastCompletedPlan == 0 && partial.LastCompletedTick == null && partial.DueTick == null &&
                    partial.Blocker == RecurringPlanCodes.Outstanding && TemplatePlan(game, second).State == PlanState.Pending,
                    "one completed member never acknowledges a whole cohort with paused work");
                var twin = factory.Load(game.Save());
                foreach (var current in new[] { game, twin })
                    c.That(TestKit.Order(current, 0, new ConfigurePlanCommand { Plan = second, Group = "player.service", Priority = 80, Paused = false }).Accepted,
                        "ordinary player command resumes actual remaining cohort member");
                TemplateReach(game, () => TemplatePlan(game, second).State == PlanState.Done, "whole physical cohort finishes", twin);
                var pending = TemplateSource(game, provider);
                long ended = Math.Max(TemplatePlan(game, first).EndedTick, TemplatePlan(game, second).EndedTick);
                c.That(pending.Blocker == RecurringPlanCodes.CompletionPending && pending.DueTick == ended + 40 && pending.LastCompletedTick == null,
                    "whole cohort pending view derives latest actual member completion");
                c.Equal(game.Hash(), twin.Hash(), "partial cohort saved continuation retains actual success relation");
                TemplateAdvance(game, 1, twin); var complete = TemplateSource(game, provider);
                c.That(complete.Schedule.LastCompletedPlan == first && complete.LastCompletedTick == ended && complete.DueTick == ended + 40,
                    "engine acknowledges cycle head only after every member physically succeeds");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "acknowledged cohort restores actual member history, never a copied timestamp");

                var mixed = ConstructionPlanTests.NewPoints(factory);
                c.That(TestKit.Order(mixed, 0, new ConfigureRecurringPlanCommand { Entity = TemplateOwner(mixed).Id, Source = source,
                    Settings = new RecurringPlanSettings(true, 60, GameDuration.Minutes(40), Code.None) }).Accepted, "start actual mixed-result cohort");
                long head = TemplateSource(mixed, provider).Schedule.LastPlan;
                EmergencyPreemptionTests.Configure(c, mixed);
                c.That(TestKit.Order(mixed, 0, new ConfigureRecurringPlanCommand { Entity = TemplateOwner(mixed).Id, Source = source,
                    Settings = new RecurringPlanSettings(false, 60, GameDuration.Minutes(40), Code.None) }).Accepted, "disable future cycles without cancelling admitted work");
                c.That(TestKit.Order(mixed, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { head }) }).Accepted, "cancel one actual cohort member");
                c.That(TestKit.Order(mixed, 0, new ConfigurePlanCommand { Plan = head + 1, Group = "player.service", Priority = 80, Paused = false }).Accepted,
                    "continue independent admitted cohort work");
                TemplateReach(mixed, () => TemplatePlan(mixed, head + 1).State == PlanState.Done, "uncancelled cohort work finishes");
                TemplateAdvance(mixed, 1);
                c.That(TemplateSource(mixed, provider).Schedule.LastCompletedPlan == 0 && TemplateSource(mixed, provider).LastCompletedTick == null,
                    "mixed cancelled/successful cohort never fabricates successful service");
                c.Equal(mixed.Hash(), factory.Load(mixed.Save()).Hash(), "mixed terminal cohort saves without successful acknowledgement");
            }
        }
        private static void RecurringContracts(Check c)
        {
            c.That(!default(RecurringPlanRequest).IsWellFormed, "default request cannot impersonate readiness");
            c.Throws<ArgumentNullException>(() => RecurringPlanRequest.Ready((PlanGroupIntent)null), "group readiness needs actual immutable intent");
            var anchor = Content.Json.Parse("{\"space\":\"field.points\",\"position\":12}");
            var binding = new RecurringPlanBinding(RecurringPlanTarget.Template, Array.Empty<string>(), RecurringReserveUse.None, anchor);
            anchor.Set("position", JsonValue.Number("90")); c.Equal("12", binding.Arguments.Get("position").Text, "template anchor detached from author");
            c.Throws<InvalidOperationException>(() => binding.Arguments.Set("position", JsonValue.Number("13")), "template anchor remains frozen");
            foreach (bool provider in new[] { false, true })
            {
                foreach (Action<JsonValue> edit in new Action<JsonValue>[] {
                    j => TemplateRow(j, provider).Remove("plan"),
                    j => TemplateRow(j, provider).Remove("template"),
                    j => TemplateRow(j, provider).Set("template", JsonValue.Null()),
                    j => TemplateRow(j, provider).Set("plan", JsonValue.Of("build:point.tower")),
                    j => TemplateRow(j, provider).Set("template", JsonValue.Of("missing.template")),
                    j => TemplateRow(j, provider).Set("template", JsonValue.Of("")),
                    j => TemplateRow(j, provider).Set("target", JsonValue.Of("authored_intent")),
                    j => TemplateRow(j, provider).Set("arguments", JsonValue.Null()),
                    j => TemplateRow(j, provider).Set("arguments", JsonValue.NewArray()),
                    j => TemplateRow(j, provider).Get("arguments").Set("extra", JsonValue.Of(true)),
                    j => TemplateRow(j, provider).Get("arguments").Set("space", JsonValue.Of("other")),
                    j => TemplateRow(j, provider).Set("group", JsonValue.Null()),
                    j => TemplateRow(j, provider).Set("worker_nodes", Content.Json.Parse("[\"build\"]")),
                    j => TemplateRow(j, provider).Set("reserve_use", JsonValue.Of("self_service")),
                    j => TemplateRow(j, provider).Set("facility_policy", JsonValue.Of("other.facility")),
                    j => TemplateRow(j, provider).Set("priority", JsonValue.Number(int.MaxValue.ToString())),
                    j => j.Get("plan_templates")[0].Set("space", JsonValue.Of("other")),
                    j => j.Get("plan_templates")[0].Get("entries")[1].Set("definition", JsonValue.Of("build:missing"))
                }) c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(Repeating(edit)), "native/public malformed template selection fails composition");
                var factory = Repeating(); var game = ConstructionPlanTests.NewPoints(factory); var owner = TemplateOwner(game);
                var source = TemplateSource(game, provider); var definition = owner.Def.Component<IRecurringPlanDefinition>(source.Source);
                c.That(definition.PlanKey == null && ReferenceEquals(definition.Template, game.World.Content.Get<PlanTemplateDef>("repeat.pair")), "source aliases the one master-listed template definition");
                c.That(owner.Def.AuthoringPath.Contains("recurring-templates.json") && game.World.Content.Get<PlanTemplateDef>("repeat.pair").AuthoringPath.Contains("recurring-templates.json"), "master lists content explicitly, no implicit file discovery");
                c.That(source.Intent == null && source.GroupIntent.Entries.Count == 2 && source.GroupAdmission.Ready && source.GroupAdmission.Entries.Count == 2 &&
                    source.Blocker == RecurringPlanCodes.Disabled && source.Schedule.LastPlan == 0, "disabled source exposes only pure grouped intent/admission");
                int priority = provider ? 40 : 60;
                c.That(source.GroupIntent.Entries[0].Priority == priority + 20 && source.GroupIntent.Entries[1].Priority == priority + 10 &&
                    !source.GroupIntent.Entries[0].Paused && source.GroupIntent.Entries[1].Paused, "relative authored priorities and pause survive expansion");
                c.Equal("16", source.GroupIntent.Entries[1].Arguments.Get("placement").Get("position").Text, "same provider expander translates second placement");
                ulong hash = game.Hash();
                c.Equal(OperationPolicyCodes.PriorityRange, View(game).PreviewRecurringPlan(owner.Id, source.Source,
                    new RecurringPlanSettings(true, int.MaxValue - 19, GameDuration.Minutes(1), Code.None)), "source preview includes every relative priority in range check");
                c.Equal(Code.None, View(game).PreviewRecurringPlan(owner.Id, source.Source,
                    new RecurringPlanSettings(true, int.MaxValue - 20, GameDuration.Minutes(1), Code.None)), "representable boundary accepted without arbitrary cap");
                c.Equal(Codes.NotFound, View(game, 1).ReadRecurringPlan(owner.Id, source.Source, out var hidden), "foreign company cannot inspect authored recurrence");
                c.That(hidden.GroupIntent == null && hidden.GroupAdmission == null && hidden.Schedule.Settings == null, "foreign result exposes no template members or schedule");
                c.Equal(hash, game.Hash(), "strict previews/private reads never allocate or advance time");
                c.That(ConfigureTemplate(game, provider, true).Accepted, "normal source control admits whole template");
                var after = TemplateSource(game, provider); var members = TemplatePlans(game);
                c.That(members.Count == 2 && after.Schedule.LastPlan == members[0].Id && members[1].Id == members[0].Id + 1, "one acknowledgement selects first of ordered native IDs");
                foreach (var member in members) c.That(member.Origin.Entity == owner.Id && member.Origin.Source == source.Source && member.CreatedTick == after.Schedule.LastPostedTick,
                    "every native member carries same actual provenance and sole-clock acceptance coordinate");
                c.That(members[0].State == PlanState.Active && members[1].State == PlanState.Pending && members[1].Paused && TemplateCount(game, "point.tower-site") == 1,
                    "paused member is intent only, unpaused member becomes actual timed site");
                c.That(source.Schedule.LastPlan == 0 && !source.Schedule.Enabled, "retained source view is detached");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "group membership uses existing board/source codecs");
            }
            var refused = ConstructionPlanTests.NewPoints(Repeating()); var offered = TemplateSource(refused, false).GroupIntent;
            c.That(TestKit.Order(refused, 0, new SubmitPlanCommand { Intent = new PlanIntent(offered.Entries[1].Definition, offered.Entries[1].Arguments, "manual", 90, true) }).Accepted,
                "player reserves only second geometry with ordinary paused plan");
            c.That(ConfigureTemplate(refused, false, true).Accepted, "source enabled even while whole group refuses");
            var refusal = TemplateSource(refused, false);
            c.That(refusal.Blocker == PlanCodes.Duplicate && refusal.GroupAdmission.Entries.Count == 2 && refusal.GroupAdmission.Entries[0].Blocker.IsNone &&
                refusal.GroupAdmission.Entries[1].Blocker == PlanCodes.Duplicate && refusal.Schedule.LastPlan == 0 && refusal.Schedule.LastPostedTick == -1,
                "late duplicate returns per-member diagnostics without accepted prefix or cadence stamp");
            c.That(TemplatePlans(refused).Count == 1 && TemplateCount(refused, "point.tower-site") == 0, "whole refusal allocates no recurring ID/site/order");
            var outside = ConstructionPlanTests.NewPoints(Repeating(j => TemplateRow(j, false).Get("arguments").Set("position", JsonValue.Number("99"))));
            c.That(ConfigureTemplate(outside, false, true).Accepted && TemplateSource(outside, false).Blocker == Codes.OutOfBounds && TemplatePlans(outside).Count == 0,
                "actual authored bounds refuse entire translated group, no invented engine size limit");
            var reorderedFactory = Repeating(j => j.Get("plan_templates")[0].Get("entries")[0].Set("paused", JsonValue.Of(true)));
            var reordered = ConstructionPlanTests.NewPoints(reorderedFactory); ConfigureTemplate(reordered, false, true);
            long head = TemplateSource(reordered, false).Schedule.LastPlan;
            c.That(TestKit.Order(reordered, 0, new MovePlanCommand { Plan = head + 1, Before = head }).Accepted && TemplatePlans(reordered)[0].Id == head + 1,
                "ordinary pending reorder changes display/order list, not accepted template entry identity");
            c.Equal(reordered.Hash(), reorderedFactory.Load(reordered.Save()).Hash(), "source checkpoint sorts real IDs instead of depending on mutable board order");
            TemplateAdvance(reordered, 4);
            c.That(TemplateSource(reordered, false).Blocker == RecurringPlanCodes.Outstanding && TemplateSource(reordered, false).Schedule.LastPlan == head,
                "reordering cannot detach still-live cycle members");
            var records = (IEnumerable)typeof(WorkBoard).GetField("_plans", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(reordered.World.Services.Get<WorkBoard>());
            foreach (object record in records)
                if ((long)record.GetType().GetField("Id", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(record) == head + 1)
                    record.GetType().GetField("Origin", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(record, default(PlanOrigin));
            c.Throws<CorruptStateException>(() => reordered.Hash(), "missing member provenance cannot be saved/hashed as a valid source cycle");
            foreach (int fault in new[] { 12, 13 })
            {
                var broken = ConstructionPlanTests.NewPoints(Repeating(j => { TemplateRow(j, true).Set("fault", JsonValue.Number(fault.ToString())); TemplateRow(j, true).Set("enabled", JsonValue.Of(true)); }));
                long time = broken.Clock.StepIndex; broken.Update();
                c.That(broken.Fault != null && broken.Clock.StepIndex == time, "forged partial group/wrong member acknowledgement faults before sole clock completion");
                c.Throws<InvalidOperationException>(() => broken.Save(), "faulted group cannot checkpoint partial authority");
            }
        }
        private static void RecurringPhysical(Check c)
        {
            foreach (bool provider in new[] { false, true })
            {
                var factory = Repeating(); var game = ConstructionPlanTests.NewPoints(factory);
                c.That(ConfigureTemplate(game, provider, true).Accepted, "player starts grouped recurrence"); EmergencyPreemptionTests.Configure(c, game);
                long first = TemplateSource(game, provider).Schedule.LastPlan, second = first + 1;
                string group = TemplatePlan(game, first).Group; long posted = TemplateSource(game, provider).Schedule.LastPostedTick;
                var stock = EmergencyPreemptionTests.Find(game, "source").Get<IInventory>(); var supply = game.World.Content.ResourceId("supply");
                c.Equal(Fix.FromInt(20), stock.Amount(supply), "posting cannot consume distant material instantly");
                var twin = factory.Load(game.Save());
                TemplateReach(game, () => TemplatePlan(game, first).State == PlanState.Done, "first member physical construction", twin);
                c.That(TemplatePlan(game, second).State == PlanState.Pending && TemplatePlan(game, second).Paused && TemplateCount(game, "point.tower") == 1,
                    "first finishes while another real member remains paused/live");
                TemplateAdvance(game, 5, twin);
                c.That(TemplateSource(game, provider).Blocker == RecurringPlanCodes.Outstanding && TemplateSource(game, provider).Schedule.LastPlan == first && TemplatePlans(game).Count == 2,
                    "first completion cannot authorize another group while any later member lives");
                c.Equal(game.Hash(), twin.Hash(), "saved physical first completion has exact cycle ownership");
                c.That(TestKit.Order(game, 0, new ConfigurePlanCommand { Plan = second, Group = "player.regrouped", Priority = 99, Paused = true }).Accepted &&
                    TestKit.Order(twin, 0, new ConfigurePlanCommand { Plan = second, Group = "player.regrouped", Priority = 99, Paused = true }).Accepted,
                    "player can regroup native member without changing source provenance");
                c.That(ConfigureTemplate(game, provider, true, 15).Accepted && ConfigureTemplate(twin, provider, true, 15).Accepted, "source future priority can change during outstanding group");
                TemplateAdvance(game, 8, twin);
                c.That(TemplatePlans(game).Count == 2 && TemplateSource(game, provider).Blocker == RecurringPlanCodes.Outstanding &&
                    TemplateSource(game, provider).Schedule.LastPostedTick == posted && TemplatePlan(game, first).Priority == 80 && TemplatePlan(game, second).Priority == 99,
                    "regroup/priority edit cannot end cycle, rewrite admitted work or reset cadence");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "regrouped cycle validates without mutable display group key");
                ulong pausedHash = game.Hash(); long now = game.Clock.StepIndex; game.Clock.Pause();
                c.Equal(0, game.Update(), "one game clock pause stops recurrence and physical work");
                c.That(game.Clock.StepIndex == now && game.Hash() == pausedHash, "no independent source/group chronology while paused"); game.Clock.Resume();
                c.That(ConfigureTemplate(game, provider, false, 15).Accepted && ConfigureTemplate(twin, provider, false, 15).Accepted,
                    "disable prevents future groups, not existing physical projects");
                c.That(TestKit.Order(game, 0, new ConfigurePlanCommand { Plan = second, Group = "player.regrouped", Priority = 99, Paused = false }).Accepted &&
                    TestKit.Order(twin, 0, new ConfigurePlanCommand { Plan = second, Group = "player.regrouped", Priority = 99, Paused = false }).Accepted, "resume second using ordinary plan control");
                twin = factory.Load(game.Save());
                TemplateReach(game, () => TemplatePlan(game, second).State == PlanState.Done, "second member physical construction", twin);
                c.That(TemplateCount(game, "point.tower") == 2 && TemplatePlans(game).Count == 2 && TemplateSource(game, provider).Schedule.LastPlan == first,
                    "all real constructions finish while disabled source retains first history pointer");
                c.Equal(Fix.FromInt(4), stock.Amount(supply), "group consumes two independent real eight-unit construction bills");
                c.Equal(game.Hash(), twin.Hash(), "second member save/load/continue is deterministic");
                c.That(ConfigureTemplate(game, provider, true, 15).Accepted && ConfigureTemplate(twin, provider, true, 15).Accepted, "all terminal members permit fresh group");
                long next = TemplateSource(game, provider).Schedule.LastPlan;
                c.That(next > second && TemplatePlans(game).Count == 4 && TemplatePlan(game, next).Group != group &&
                    TemplatePlan(game, next).Priority == 35 && TemplatePlan(game, next + 1).Priority == 25 && TemplatePlan(game, next + 1).Paused,
                    "next group derives identity from actual last ID and applies only future settings/relative priorities");
                TemplateAdvance(game, 70, twin);
                c.That(TemplatePlan(game, next).State == PlanState.Active && TemplateCount(game, "point.tower") == 2 && TemplateSource(game, provider).Schedule.LastPlan == next && TemplatePlans(game).Count == 4,
                    "finite material shortage keeps native physical work outstanding, no free construction/reposting");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "shortage receipts and complete grouped cycle checkpoint together");
                c.That(ConfigureTemplate(game, provider, false, 15).Accepted && ConfigureTemplate(twin, provider, false, 15).Accepted, "stop future repetition before cancellation");
                string nextGroup = TemplatePlan(game, next).Group;
                c.That(TestKit.Order(game, 0, new CancelPlanGroupCommand { Group = nextGroup }).Accepted && TestKit.Order(twin, 0, new CancelPlanGroupCommand { Group = nextGroup }).Accepted,
                    "native group cancellation disposes real site and paused intent");
                TemplateReach(game, () => TemplateCount(game, "point.tower-site") == 0, "cancelled grouped shortage material recovery", twin);
                c.That(TemplatePlan(game, next).State == PlanState.Cancelled && TemplatePlan(game, next + 1).State == PlanState.Cancelled && TemplatePlans(game).Count == 4,
                    "existing plan owner retains terminal group without resurrection");
                Fix remaining = Fix.Zero;
                foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Get<IInventory>() is IInventory inventory) remaining += inventory.Amount(supply);
                c.Equal(Fix.FromInt(4), remaining, "ordinary cancellation conserves all unconsumed stock across real endpoint inventories");
                c.Equal(game.Hash(), twin.Hash(), "interleaved native/public source sessions remain isolated through recovery");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "terminal groups survive durable checkpoint validation");
                long otherTime = twin.Clock.StepIndex; TemplateAdvance(game, 1);
                c.Equal(otherTime, twin.Clock.StepIndex, "another session never shares source state or game clock storage");
            }
            var cancelled = ConstructionPlanTests.NewPoints(Repeating());
            c.That(ConfigureTemplate(cancelled, false, true).Accepted, "start cancellation-cycle proof through normal control");
            long cancelledFirst = TemplateSource(cancelled, false).Schedule.LastPlan;
            c.That(TestKit.Order(cancelled, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { cancelledFirst }) }).Accepted, "cancel only first template member");
            TemplateAdvance(cancelled, 5);
            c.That(TemplatePlan(cancelled, cancelledFirst).State == PlanState.Cancelled && TemplatePlan(cancelled, cancelledFirst + 1).State == PlanState.Pending &&
                TemplateSource(cancelled, false).Blocker == RecurringPlanCodes.Outstanding && TemplatePlans(cancelled).Count == 2,
                "cancelling first cannot repost while later paused member is live");
        }
    }
}
