using System;
using System.Collections.Generic;
using System.IO;
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
        private static PlanDependencyChanges Changes(params PlanDependencyChange[] entries) => new PlanDependencyChanges(entries);
        private static PlanDependencyChange Links(long plan, params long[] dependencies) => new PlanDependencyChange(plan, dependencies);
        private static CommandResult ChangeLinks(SimGame game, PlanDependencyChanges changes, int seat = 0)
            => TestKit.Order(game, seat, new ConfigurePlanDependenciesCommand { Changes = changes });
        private static void EditingReach(SimGame game, Func<bool> ready, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 720 && !ready(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!ready()) throw new InvalidOperationException(stage + "; game update=" + game.Clock.StepIndex + "; plans=" +
                string.Join(";", TemplatePlans(game).ConvertAll(p => p.Id + "/" + p.State + "/" + p.Blocker)));
        }
        private static void DependencyEditingContracts(Check c)
        {
            var input = new List<long> { 4, 2 }; var change = new PlanDependencyChange(1, input); input.Clear();
            c.That(change.Dependencies.Count == 2 && change.Dependencies[0] == 2 && change.Dependencies[1] == 4,
                "existing prerequisite capture owns immutable canonical edit links");
            c.Throws<NotSupportedException>(() => ((IList<long>)change.Dependencies).Add(7), "edit links cannot be changed through retained list");
            var edits = new List<PlanDependencyChange> { Links(3), change }; var captured = new PlanDependencyChanges(edits); edits.Clear();
            c.That(captured.Entries.Count == 2 && captured.Entries[0].Plan == 1, "atomic changes capture canonical membership without caller aliases");
            c.Throws<NotSupportedException>(() => ((IList<PlanDependencyChange>)captured.Entries).Clear(), "atomic change membership is immutable");
            c.Throws<ArgumentException>(() => Links(0), "edit requires a real positive plan ID");
            c.Throws<ArgumentNullException>(() => new PlanDependencyChange(1, null), "dependency set is explicit, not fabricated");
            c.Throws<ArgumentException>(() => Links(1, 0), "nonpositive prerequisite refuses");
            c.Throws<ArgumentException>(() => Links(1, 2, 2), "duplicate prerequisite refuses");
            c.Throws<ArgumentException>(() => Links(1, 1), "self dependency refuses");
            c.Throws<ArgumentException>(() => Changes(), "empty changes do not invent an edit");
            c.Throws<ArgumentException>(() => Changes(Links(1), Links(1, 2)), "duplicate edits cannot define conflicting authority");
            c.Throws<ArgumentException>(() => Changes((PlanDependencyChange)null), "null entry refuses");
            string first = Convert.ToBase64String(DependencyWire(new ConfigurePlanDependenciesCommand { Changes = captured }));
            string second = Convert.ToBase64String(DependencyWire(new ConfigurePlanDependenciesCommand { Changes = Changes(Links(3), Links(1, 4, 2)) }));
            c.Equal(first, second, "shuffled input changes and links have identical canonical command bytes");
            var decoded = new ConfigurePlanDependenciesCommand(); decoded.Sync(new StateReader(Convert.FromBase64String(first), 0, Convert.FromBase64String(first).Length));
            c.Equal(first, Convert.ToBase64String(DependencyWire(decoded)), "one canonical edit command codec roundtrips");
            BadEditWire(c, Array.Empty<long[]>(), "empty wire set refuses");
            BadEditWire(c, new[] { new long[] { 0 } }, "nonpositive wire plan refuses");
            BadEditWire(c, new[] { new long[] { 2 }, new long[] { 1 } }, "unsorted wire membership refuses");
            BadEditWire(c, new[] { new long[] { 1 }, new long[] { 1 } }, "duplicate wire membership refuses");
            BadEditWire(c, new[] { new long[] { 1, 1 } }, "wire self reference refuses");
            BadEditWire(c, new[] { new long[] { 1, 4, 2 } }, "unsorted wire links refuse");
            BadEditWire(c, new[] { new long[] { 1, 2, 2 } }, "duplicate wire links refuse");
            GameFactory factory = Dependencies(); SimGame game = ConstructionPlanTests.NewPoints(factory);
            long a = DependencySubmit(c, game, DependencyBuild(game, 12, "editing"));
            long b = DependencySubmit(c, game, DependencyBuild(game, 16, "editing", a));
            long d = DependencySubmit(c, game, DependencyBuild(game, 20, "editing", b));
            long foreign = DependencySubmit(c, game, DependencyBuild(game, 60, "foreign"), 1);
            ulong before = game.Hash();
            c.Equal(Codes.InvalidArgument, View(game).PreviewPlanDependencies(null).Blocker, "missing edit value has typed refusal");
            c.Equal(Codes.NotFound, View(game).PreviewPlanDependencies(Changes(Links(long.MaxValue))).Blocker, "missing edit plan refuses");
            c.Equal(Codes.NotFound, View(game).PreviewPlanDependencies(Changes(Links(a, long.MaxValue))).Blocker, "missing prerequisite refuses");
            c.Equal(Codes.NotOwned, View(game).PreviewPlanDependencies(Changes(Links(a, foreign))).Blocker, "foreign prerequisite refuses");
            c.Equal(Codes.NotOwned, View(game, 1).PreviewPlanDependencies(Changes(Links(a))).Blocker, "another seat cannot edit owned graph");
            c.Equal(before, game.Hash(), "dependency management previews advance no stock, plan identity or clock");
            var cycle = Changes(Links(a, d));
            c.Equal(PlanCodes.DependencyCycle, View(game).PreviewPlanDependencies(cycle).Blocker,
                "proposed edit includes unchanged indirect ancestry in sole DAG validation");
            c.Equal(before, game.Hash(), "cyclic proposal leaves native graph unchanged");
            c.Equal(PlanCodes.DependencyCycle, ChangeLinks(game, cycle).Refusal, "cycle command shares engine preview refusal");
            var mixed = Changes(Links(a, b), Links(foreign)); var checkedSet = View(game).PreviewPlanDependencies(mixed);
            c.That(checkedSet.Entries.Count == 2 && checkedSet.Entries[0].Blocker.IsNone && checkedSet.Entries[1].Blocker == Codes.NotOwned,
                "whole refusal preserves per-plan diagnostics, without leaking other graph membership");
            c.Equal(checkedSet.Blocker, ChangeLinks(game, mixed).Refusal, "late member refusal matches command");
            c.Equal(0, TemplatePlan(game, a).Dependencies.Count, "late refusal applies no otherwise valid prefix");
            c.Equal(a, TemplatePlan(game, b).Dependencies[0], "late refusal preserves actual existing ancestry");
            var reversed = Changes(Links(b), Links(a, b)); before = game.Hash();
            c.That(View(game).PreviewPlanDependencies(reversed).Ready, "simultaneous pending relationship reversal has no cyclic intermediate");
            c.Equal(before, game.Hash(), "valid atomic proposal is pure");
            PlanView retained = TemplatePlan(game, b); CommandResult changed = ChangeLinks(game, reversed);
            c.That(changed.Accepted && changed.Arg0 == a && changed.Arg1 == 2, "one command commits canonical complete member set");
            c.That(TemplatePlan(game, a).Dependencies[0] == b && TemplatePlan(game, b).Dependencies.Count == 0,
                "sole actual records now own the reversed relationship");
            c.Equal(a, retained.Dependencies[0], "retained immutable view does not mutate after edit");
            c.That(TemplatePlan(game, a).Order == 0 && TemplatePlan(game, b).Order == 0, "editing paused intent creates no native work");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "edited topology checkpoints through existing board codec");
            var inbox = Changes(Links(a), Links(d, a)); game.Seat(0).Submit(new ConfigurePlanDependenciesCommand { Changes = inbox });
            SimGame twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.That(TemplatePlan(game, a).Dependencies.Count == 0 && TemplatePlan(game, d).Dependencies[0] == a,
                "saved command inbox commits actual graph changes at normal update");
            c.Equal(game.Hash(), twin.Hash(), "saved edit command and native graph are deterministic");
            c.That(TestKit.Order(game, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { b }) }).Accepted, "player retires owned pending intent");
            c.Equal(PlanCodes.NotPending, View(game).PreviewPlanDependencies(Changes(Links(b))).Blocker,
                "terminal history cannot be rewritten as another pending graph");
            SourceOwnedEdits(c);
        }
        private static void BadEditWire(Check c, long[][] rows, string reason)
        {
            byte[] bytes;
            using (var stream = new MemoryStream())
            {
                var writer = new StateWriter(stream); bool present = true; writer.Bool(ref present); writer.Count(rows.Length);
                foreach (long[] row in rows)
                {
                    long id = row[0]; writer.Long(ref id); writer.Count(row.Length - 1);
                    for (int i = 1; i < row.Length; i++) { long link = row[i]; writer.Long(ref link); }
                }
                writer.Flush(); bytes = stream.ToArray();
            }
            c.Throws<CorruptStateException>(() => new ConfigurePlanDependenciesCommand().Sync(new StateReader(bytes, 0, bytes.Length)), reason);
        }
        private static void SourceOwnedEdits(Check c)
        {
            foreach (bool provider in new[] { false, true })
            {
                GameFactory factory = Repeating(root => {
                    JsonValue rows = root.Get("plan_templates")[0].Get("entries");
                    rows[0].Set("paused", JsonValue.Of(true)); rows[1].Set("after", Content.Json.Parse("[\"first\"]"));
                });
                SimGame game = ConstructionPlanTests.NewPoints(factory);
                c.That(ConfigureTemplate(game, provider, true).Accepted, "player enables actual native/public recurring source");
                Tick(game); long first = TemplateSource(game, provider).Schedule.LastPlan;
                c.That(first > 0 && TemplatePlan(game, first).State == PlanState.Pending, "authored recurring cycle owns paused real intent");
                var changes = Changes(Links(first + 1)); ulong before = game.Hash();
                c.Equal(PlanCodes.SourceOwnedDependencies, View(game).PreviewPlanDependencies(changes).Blocker,
                    "recurring source provenance refuses silent authored topology revision");
                c.Equal(before, game.Hash(), "source-owned graph preview is pure");
                c.Equal(PlanCodes.SourceOwnedDependencies, ChangeLinks(game, changes).Refusal, "source-owned refusal matches command");
                c.Equal(first, TemplatePlan(game, first + 1).Dependencies[0], "refused edit retains exact authored cycle links");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "source-owned cycle remains valid and durable after refusal");
            }
        }
        private static void DependencyEditingPhysical(Check c)
        {
            GameFactory factory = Dependencies(); SimGame game = ConstructionPlanTests.NewPoints(factory); EmergencyPreemptionTests.Configure(c, game);
            long a = DependencySubmit(c, game, DependencyBuild(game, 12, "editing"));
            long b = DependencySubmit(c, game, DependencyBuild(game, 16, "editing", a));
            long d = DependencySubmit(c, game, DependencyBuild(game, 20, "editing", b));
            c.That(ChangeLinks(game, Changes(Links(a, b), Links(b), Links(d, a))).Accepted, "player atomically changes real construction sequence");
            DependencyEdit(c, game, "editing", false); Tick(game);
            c.That(TemplatePlan(game, b).State == PlanState.Active && TemplatePlan(game, a).Order == 0 && TemplatePlan(game, d).Order == 0,
                "normal promotion starts edited root only, not former root or dependents");
            var activeMixed = Changes(Links(a), Links(b, long.MaxValue)); ulong before = game.Hash();
            c.Equal(Codes.NotFound, View(game).PreviewPlanDependencies(activeMixed).Blocker, "active dependency edit still requires actual owned references");
            c.Equal(before, game.Hash(), "invalid active proposal preserves actual work and claims");
            c.Equal(Codes.NotFound, ChangeLinks(game, activeMixed).Refusal, "invalid active edit refuses the entire set");
            c.Equal(b, TemplatePlan(game, a).Dependencies[0], "invalid active mixed refusal commits no pending prefix");
            c.That(View(game).CapturePlanTemplate("editing", At(game, 12), out PlanTemplate template).Ready, "capture reads edited actual graph, never old authoring");
            c.Equal("entry.2", template.Entries[0].Dependencies[0], "portable capture translates edited root ID to stable member ID");
            c.Equal("entry.1", template.Entries[2].Dependencies[0], "portable capture translates edited successor ancestry");
            SimGame twin = factory.Load(game.Save()); before = game.Hash(); long time = game.Clock.StepIndex; game.Clock.Pause();
            for (int i = 0; i < 5; i++) game.Update();
            c.Equal(time, game.Clock.StepIndex, "one clock pause freezes edited physical sequence");
            c.Equal(before, game.Hash(), "pause advances no plan, material, machine or graph state");
            game.Clock.Resume();
            EditingReach(game, () => TemplatePlan(game, b).State == PlanState.Done, "edited physical root", twin);
            c.That(TemplateCount(game, "point.tower") == 1 && TemplatePlan(game, a).State != PlanState.Done,
                "former root waits for actual new-root completion");
            EditingReach(game, () => TemplatePlan(game, d).State == PlanState.Done, "edited physical successor chain", twin);
            c.That(TemplatePlan(game, a).State == PlanState.Done && TemplateCount(game, "point.tower") == 3,
                "all edited dependencies finish through real physical construction");
            c.Equal(Fix.FromInt(8), FindPoint(game, "source").Get<IInventory>().Amount(game.World.Content.ResourceId("supply")),
                "three physical builds consume twelve finite units, not editing-time resources");
            c.Equal(game.Hash(), twin.Hash(), "edited physical graph saved twins agree");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed edited graph retains native durable history");
            CommandResult repeated = TestKit.Order(game, 0, new RepeatPlanGroupCommand { Group = "editing", NewGroup = "repeated.edit" });
            c.That(repeated.Accepted && repeated.Arg1 == 3, "normal repeat uses current edited graph");
            var rows = Group(game, "repeated.edit");
            c.That(rows[0].Dependencies[0] == rows[1].Id && rows[1].Dependencies.Count == 0 && rows[2].Dependencies[0] == rows[0].Id,
                "repeated graph remaps edited links, without old IDs or copied completion");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "repeated edited topology saves through sole board");
            CancelledPrerequisiteEdit(c);
        }
        private static Entity FindPoint(SimGame game, string definition)
        {
            foreach (Entity entity in game.World.Entities.All) if (entity.Alive && entity.Def.Id == definition) return entity;
            throw new InvalidOperationException("missing actual endpoint " + definition);
        }
        private static void CancelledPrerequisiteEdit(Check c)
        {
            GameFactory factory = Dependencies(); SimGame game = ConstructionPlanTests.NewPoints(factory); EmergencyPreemptionTests.Configure(c, game);
            long a = DependencySubmit(c, game, DependencyBuild(game, 12, "cancelled"));
            long b = DependencySubmit(c, game, DependencyBuild(game, 16, "survivor", a));
            c.That(TestKit.Order(game, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { a }) }).Accepted, "player cancels actual predecessor");
            DependencyEdit(c, game, "survivor", false);
            c.Equal(PlanCodes.DependencyCancelled, TemplatePlan(game, b).Blocker, "cancellation remains a real blocker until explicit editing");
            c.That(ChangeLinks(game, Changes(Links(b))).Accepted, "player explicitly removes cancelled pending prerequisite");
            PlanView current = TemplatePlan(game, b);
            c.That(current.State == PlanState.Active && game.World.Services.Get<WorkBoard>().Order(current.Order).NodesDone == 0,
                "command driver performs normal promotion, never instant construction completion");
            SimGame twin = factory.Load(game.Save());
            EditingReach(game, () => TemplatePlan(game, b).State == PlanState.Done, "physically unblocked edited successor", twin);
            c.That(TemplatePlan(game, a).State == PlanState.Cancelled && TemplateCount(game, "point.tower") == 1,
                "edited successor completes physically without rewriting cancelled predecessor history");
            c.Equal(Fix.FromInt(16), FindPoint(game, "source").Get<IInventory>().Amount(game.World.Content.ResourceId("supply")),
                "only the surviving physical construction consumes material");
            c.Equal(game.Hash(), twin.Hash(), "cancelled-history edit and physical continuation remain deterministic");
        }
    }
}
