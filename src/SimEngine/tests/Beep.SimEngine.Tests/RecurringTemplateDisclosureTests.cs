using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public static partial class PlanTemplateTests
    {
        internal const string TemplateDisclosureCase = "Recurring template disclosure: known revision blockers and replacements share player admission and saved physical construction";
        internal static void RegisterTemplateDisclosure() => TestRunner.Add(TemplateDisclosureCase, TemplateDisclosure);
        private static GameFactory DisclosureRepeating(bool provider, bool entitySight = false)
            => Repeating(root => {
                var rows = JsonValue.NewArray(); rows.Add(root.Get("plan_templates")[0].Get("entries")[0].Clone());
                root.Get("plan_templates")[0].Set("entries", rows);
                TemplateRow(root, provider).Set("cadence", JsonValue.Of("successful_completion"));
                TemplateRow(root, provider).Set("interval", Json.Parse("{\"numerator\":120,\"denominator\":1,\"unit\":\"minute\"}"));
            }, extraModules: new GameModule[] { new InventorySightModule() },
                extraDeclarations: "[{\"id\":\"proof.inventory-sight\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}]",
                scenarioEdit: scenario => {
                    scenario.Get("modules").Add(JsonValue.Of(InventorySightModule.ModuleId));
                    scenario.Get("settings").Set(InventorySightModule.ModuleId, Json.Parse(entitySight
                        ? "{\"target_kind\":\"entity\",\"target\":\"template.controller\",\"initial_hidden\":false}"
                        : "{\"target_kind\":\"activity\",\"target\":\"point.tower.build-work\",\"initial_hidden\":false}"));
                }, additionalDocuments: new Dictionary<string, string> {
                    ["recurring-template-visibility.json"] = FrameworkProofContent.Read("recurring-template-visibility.json") });
        private static RecurringTemplateSettings DisclosureSettings(PlanTemplateSelection selection)
            => new RecurringTemplateSettings(selection, Json.Parse("{\"space\":\"field.points\",\"position\":40}"), "visible.replacement");
        private static ConfigureRecurringTemplateCommand DisclosureCommand(EntityId entity, Code source, RecurringTemplateSettings settings)
            => new ConfigureRecurringTemplateCommand { Entity = entity, Source = source, Settings = settings };
        private static CommandResult DisclosureSubmit(Check c, SimGame game, Command command, SimGame twin = null, Func<Command> twinCommand = null)
        {
            c.Equal(Code.None, game.Seat(0).Preview(command), "normal template input preview is admitted");
            int sequence = game.Seat(0).Submit(command);
            if (twin != null) twin.Seat(0).Submit(twinCommand());
            PermanentRemovalTests.Step(game, twin);
            long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
            var result = results.Single(row => row.Sequence == sequence);
            c.That(result.Accepted, "normal template input executes through engine"); return result;
        }
        private static void TemplateEntityDisclosure(Check c, bool provider)
        {
            var factory = DisclosureRepeating(provider, true); var game = ConstructionPlanTests.NewPoints(factory);
            var entity = TransformationContentContractTests.Entity(game, "template.controller"); var source = TemplateCode(provider);
            var work = game.Seat(0).View.Feature<WorkView>(); var replacement = DisclosureSettings(PlanTemplateSelection.Snapshot(work.AuthoredPlanTemplate("repeat.visible")));
            var limits = new ReplayLimits(4 * 1024 * 1024, 2000); using var recording = game.RecordReplay(limits);
            DisclosureSubmit(c, game, new SetInventorySightCommand { Hidden = true });
            c.Equal(Codes.NotFound, work.PreviewRecurringTemplate(entity, source, replacement).Blocker, "hidden owned entity blocks typed future-template control");
            c.Equal(Codes.NotFound, game.Seat(0).Preview(DisclosureCommand(entity, source, replacement)), "ordinary future-template preview uses same sight gate");
            int sequence = game.Seat(0).Submit(DisclosureCommand(entity, source, replacement)); game.Update();
            long cursor = 0; var rows = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, rows);
            c.That(!rows.Single(row => row.Sequence == sequence).Accepted && rows.Single(row => row.Sequence == sequence).Refusal == Codes.NotFound,
                "executed template replacement cannot bypass entity sight");
            c.Equal(Code.None, game.Observer.Feature<WorkView>().ReadRecurringTemplate(entity, source, out var original), "observer reads actual unchanged future intent");
            c.That(original.Group != replacement.Group, "denied replacement has no settings prefix");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "hidden-entity refusal checkpoints");
            var replay = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
            c.That(replay.Success && replay.ActualHash == game.Hash(), "actual hidden-entity template refusal replays");
        }
        private static void TemplateDisclosure(Check c)
        {
            foreach (bool provider in new[] { false, true })
            {
                var factory = DisclosureRepeating(provider); var game = ConstructionPlanTests.NewPoints(factory);
                var idle = ConstructionPlanTests.NewPoints(factory); ulong isolated = idle.Hash();
                var entity = TransformationContentContractTests.Entity(game, "template.controller"); var source = TemplateCode(provider);
                var work = game.Seat(0).View.Feature<WorkView>(); var design = work.AuthoredPlanTemplate("repeat.pair");
                var replacement = work.AuthoredPlanTemplate("repeat.visible");
                var limits = new ReplayLimits(4 * 1024 * 1024, 2000); using var recording = game.RecordReplay(limits);
                var created = DisclosureSubmit(c, game, new StorePlanTemplateCommand { Name = "revision.proof", Value = design });
                long id = created.Arg0;
                DisclosureSubmit(c, game, DisclosureCommand(entity, source, DisclosureSettings(PlanTemplateSelection.Stored(id, 1))));
                DisclosureSubmit(c, game, new StorePlanTemplateCommand { Template = id, ExpectedRevision = 1, Name = "revision.proof", Value = design });
                c.Equal(Code.None, work.ReadRecurringPlan(entity, source, out var stale), "known revision conflict remains inspectable, not hidden content");
                c.Equal(PlanCodes.TemplateRevisionConflict, stale.RequestBlocker, "actual request reports precise stale revision");
                c.Equal(Code.None, work.ReadRecurringTemplate(entity, source, out var future), "known stale future selection remains inspectable");
                c.That(future != null && future.Selection.Revision == 1, "known stale source preserves actual requested revision");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "known stale selection saves without invented repair");
                DisclosureSubmit(c, game, new SetInventorySightCommand { Hidden = true });
                c.Equal(Codes.NotFound, work.ReadRecurringPlan(entity, source, out _), "hidden current library graph is not a stale-revision oracle");
                c.Equal(Codes.NotFound, work.ReadRecurringTemplate(entity, source, out _), "private future graph hides its settings");
                var known = DisclosureSettings(PlanTemplateSelection.Snapshot(replacement));
                c.That(work.PreviewRecurringTemplate(entity, source, known).Ready, "known replacement does not require hidden old template");
                DisclosureSubmit(c, game, DisclosureCommand(entity, source, known));
                c.Equal(Code.None, work.ReadRecurringTemplate(entity, source, out var selected), "current known snapshot is visible despite hidden authored default");
                c.That(selected.Selection.Kind == PlanTemplateSelectionKind.Snapshot && selected.Selection.Template.Entries[0].Definition == "build:point.visible-tower",
                    "disclosure follows actual current snapshot, not old authored template");
                DisclosureSubmit(c, game, new StorePlanTemplateCommand { Template = id, ExpectedRevision = 2, Name = "revision.proof", Value = replacement });
                DisclosureSubmit(c, game, DisclosureCommand(entity, source, DisclosureSettings(PlanTemplateSelection.Stored(id, 3))));
                c.Equal(Code.None, work.ReadRecurringPlan(entity, source, out var repaired), "current known private-library replacement is visible");
                c.That(repaired.Schedule.LastPlan == 0 && repaired.TemplateSettings.Selection.Revision == 3, "future repair creates no completed or active project");
                var worker = TransformationContentContractTests.Entity(game, "machine");
                DisclosureSubmit(c, game, EmergencyPreemptionTests.ConfigureCommand(worker));
                DisclosureSubmit(c, game, new ConfigureRecurringPlanCommand { Entity = entity, Source = source,
                    Settings = new RecurringPlanSettings(true, 60, GameDuration.Minutes(120), Code.None) });
                long planId = 0; SimGame twin = null; bool partial = false; int updates = 0; var watch = Stopwatch.StartNew();
                while (updates < 192 && watch.ElapsedMilliseconds < 25000)
                {
                    c.Equal(Code.None, work.ReadRecurringPlan(entity, source, out var current), "known current physical cycle remains inspectable");
                    planId = current.Schedule.LastPlan;
                    var plans = new List<PlanView>(); work.CopyPlans(plans); var plan = plans.SingleOrDefault(row => row.Id == planId);
                    if (planId > 0 && plan.State == PlanState.Done && current.Schedule.LastCompletedPlan == planId) break;
                    if (planId > 0 && !partial)
                    {
                        var nodes = new List<ActivityNodeView>(); work.TryCopyNodes(plan.Order, nodes);
                        if (nodes.Any(row => row.ProductiveSteps > 0 && row.ProductiveSteps < row.RequiredTicks))
                        {
                            partial = true; twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual replacement construction saves mid-work");
                            Func<Command> disable = () => new ConfigureRecurringPlanCommand { Entity = entity, Source = source,
                                Settings = new RecurringPlanSettings(false, 60, GameDuration.Minutes(120), Code.None) };
                            DisclosureSubmit(c, game, disable(), twin, disable);
                        }
                    }
                    PermanentRemovalTests.Step(game, twin); updates++;
                }
                c.That(partial && updates < 192 && watch.ElapsedMilliseconds < 25000, "admitted replacement physically completes after future cycles disabled");
                var completed = new List<PlanView>(); work.CopyPlans(completed);
                c.That(planId > 0 && completed.Count == 1 && completed[0].State == PlanState.Done && completed[0].EndedTick - completed[0].CreatedTick >= 9,
                    "one real project earns every authored construction stage over game time");
                c.That(game.Seat(0).Snapshot.Entities.Any(row => row.Def == "point.visible-tower"), "engine creates actual replacement structure, not just a completed-plan flag");
                var supply = PermanentRemovalTests.Supply(game); var stock = TransformationContentContractTests.Entity(game, "source");
                c.Equal(Fix.FromInt(12), LoadPolicyDisclosureTests.Stock(game, stock, supply), "real construction consumes eight finite supply once");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed replacement and retained chronology restore");
                c.Equal(isolated, idle.Hash(), "interleaved idle template session remains isolated");
                var replay = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
                c.That(replay.Success && replay.ActualHash == game.Hash(), "actual revision hide replace supply construct and disable inputs replay");
                TemplateEntityDisclosure(c, provider);
            }
        }
    }
}
