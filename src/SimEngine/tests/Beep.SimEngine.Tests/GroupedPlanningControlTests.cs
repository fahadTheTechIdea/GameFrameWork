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
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Ordinary player input and detached facts; no construction, scheduling or provider decisions here.
    internal static class GroupedPlanningControlTests
    {
        internal const string Case = "Grouped planning: truthful entry admission and saved physical pause reorder repeat cancel";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(out ConstructionContentContractsProofModule proof, bool direct = true)
            => ConstructionContentContractTests.Factory(new InitializeProbe(), out proof, direct, templates: FrameworkProofContent.Read("grouped-planning.json"));
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static WorkView View(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<WorkView>();
        private static PlanTemplate Template(SimGame game) => View(game).AuthoredPlanTemplate("planning.linked");
        private static JsonValue At(int position) => Json.Parse("{\"space\":\"field.points\",\"position\":" + position + "}");
        private static List<PlanView> Group(SimGame game, string name, int seat = 0)
        { var rows = new List<PlanView>(); View(game, seat).CopyPlanGroup(name, rows); return rows; }
        private static List<PlanView> All(SimGame game)
        { var rows = new List<PlanView>(); View(game).CopyPlans(rows); return rows; }
        private static List<ActivityNodeView> Nodes(SimGame game, string group)
        { var rows = new List<ActivityNodeView>(); var entry = Group(game, group).First(); View(game).TryCopyNodes(entry.Order, rows); return rows; }
        private static int Towers(SimGame game) => game.Seat(0).Snapshot.Entities.Count(e => e.Def == "point.tower");
        private static int Sites(SimGame game) => game.Seat(0).Snapshot.Entities.Count(e => e.Def == "point.tower-site");
        private static void Place(Check c, SimGame game, PlanTemplate value, int at, string group)
            => Order(c, game, new PlacePlanTemplateCommand { Selection = PlanTemplateSelection.Snapshot(value), Anchor = At(at), Group = group });
        private static void Reach(SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 256 && !ready(); i++) Step(game, twin);
            if (!ready()) throw new InvalidOperationException(stage + " update=" + game.Clock.StepIndex + " source=" + Stock(game, "source")
                + " cargo=" + Stock(game, "machine") + " plans=" + string.Join(",", All(game).Select(p => p.Id + ":" + p.State + ":" + p.Blocker)));
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "actual grouped planning protocol");
            c.Equal(154, EngineInfo.SaveFormat, "original plan and physical work state codecs");
            c.That(Physical(c, false).SequenceEqual(Physical(c, true)), "native and independent contract construction earn identical group chronology");
            Faults(c);
        }
        private static long[] Physical(Check c, bool direct)
        {
            var factory = Factory(out var proof, direct); var game = New(factory); var view = View(game); var template = Template(game);
            c.That(proof.OneOwner && proof.ActualBindings && template.Entries.Count == 2
                && template.Entries[1].Dependencies.SequenceEqual(new[] { "first" }), "master file and actual contract graph retain stable authored dependency identity");
            c.That(!game.Modules.Contains("sim.tycoon"), "plans and work require no business module");
            Enable(c, game); var limits = new ReplayLimits(4 * 1024 * 1024, 3000); var recorder = game.RecordReplay(limits);
            var invalid = new PlanTemplate(template.Space, new[] { template.Entries[0],
                new PlanTemplateEntry("unknown", "build:missing", template.Entries[1].Arguments, 60, true) });
            ulong hash = game.Hash(); var failure = view.ExpandPlanTemplate(invalid, At(12), "invalid", out var refused);
            c.That(failure.Blocker == Codes.UnknownDef && failure.Entries.Count == 2 && failure.Entries[0].Blocker.IsNone
                && failure.Entries[1].Blocker == Codes.UnknownDef && refused == null, "late invalid entry returns truthful complete diagnostics, no candidate intent");
            c.Equal(hash, game.Hash(), "failed expansion is pure");
            c.Equal(failure.Blocker, TestKit.Order(game, 0, new PlacePlanTemplateCommand {
                Selection = PlanTemplateSelection.Snapshot(invalid), Anchor = At(12), Group = "invalid" }).Refusal, "whole failed command matches preview");
            c.That(All(game).Count == 0 && Sites(game) == 0 && Towers(game) == 0 && Stock(game, "source") == Fix.FromInt(20), "failed group creates no prefix or spending");
            var duplicate = new PlanTemplate(template.Space, new[] { template.Entries[0],
                new PlanTemplateEntry("duplicate", template.Entries[0].Definition, template.Entries[0].Arguments, 1, true) });
            var duplicates = view.ExpandPlanTemplate(duplicate, At(12), "duplicate", out refused);
            c.That(duplicates.Blocker == PlanCodes.Duplicate && duplicates.Entries.Count == 2
                && duplicates.Entries[0].Blocker.IsNone && duplicates.Entries[1].Blocker == PlanCodes.Duplicate && refused == null, "whole group admission failure clears already-expanded candidate");
            var outside = view.PreviewPlanTemplate(template, At(98), "outside");
            c.That(outside.Blocker == Codes.OutOfBounds && outside.Entries.Count == 2 && outside.Entries[0].Blocker.IsNone
                && outside.Entries[1].Blocker == Codes.OutOfBounds, "native geometry returns distinct per-entry bounds facts");
            hash = game.Hash(); var accepted = view.ExpandPlanTemplate(template, At(12), "seed", out var expanded);
            c.That(accepted.Ready && accepted.Entries.Count == 2 && expanded != null && expanded.Entries[1].Arguments.Get("placement").Get("position").Text == "18", "engine expands relative coordinates, host never calculates geometry");
            c.Equal(hash, game.Hash(), "successful expansion is not execution or resource reservation");
            Place(c, game, template, 12, "seed"); var seed = Group(game, "seed");
            c.That(seed.Select(p => p.Id).SequenceEqual(new long[] { 1, 2 }) && seed[1].Dependencies.SequenceEqual(new[] { seed[0].Id })
                && seed.All(p => p.State == PlanState.Pending && p.Paused && p.Order == 0), "only whole accepted intent allocates stable IDs and remaps links, no ghost sites");
            Place(c, game, template, 40, "other");
            c.That(view.PreviewPlanGroupMove("seed", 0).Ready, "pending group can reorder as a whole");
            Order(c, game, new MovePlanGroupCommand { Group = "seed", Before = 0 });
            c.That(All(game).Select(p => p.Group).SequenceEqual(new[] { "other", "other", "seed", "seed" })
                && Group(game, "seed")[1].Dependencies.Single() == seed[0].Id, "reorder preserves member order and actual dependency identities");
            c.That(Group(game, "seed", 1).Count == 0 && View(game, 1).PreviewPlanGroupCancel("seed").Blocker == Codes.NotFound, "foreign group admission publishes no entry identities");
            var cancel = view.PreviewPlanGroupCancel("seed");
            c.That(cancel.Ready && cancel.Entries.Select(e => e.Plan).SequenceEqual(seed.Select(p => p.Id)), "cancel previews exactly selected live members");
            Order(c, game, new CancelPlanGroupCommand { Group = "seed" });
            c.That(Group(game, "seed").All(p => p.State == PlanState.Cancelled && p.EndedTick >= 0)
                && view.PreviewPlanGroupCancel("seed").Blocker == Codes.AlreadyDone && Sites(game) == 0 && Stock(game, "source") == Fix.FromInt(20), "cancelled pending group performs no physical work and truthful terminal refusal");
            c.That(view.PreviewPlanGroupRepeat("seed", "field").Ready, "terminal group admits fresh whole repeated intent");
            Order(c, game, new RepeatPlanGroupCommand { Group = "seed", NewGroup = "field" });
            var field = Group(game, "field");
            c.That(field[0].Id > seed[1].Id && field[1].Dependencies.SequenceEqual(new[] { field[0].Id })
                && field[0].State == PlanState.Active && field[1].State == PlanState.Pending && Sites(game) == 1 && Towers(game) == 0, "repeat remaps links and starts only eligible physical predecessor, never copies completion");
            var liveRepeat = view.PreviewPlanGroupRepeat("field", "too-soon");
            c.That(liveRepeat.Blocker == PlanCodes.NotEnded && liveRepeat.Entries.Count == 2
                && liveRepeat.Entries.All(e => e.Blocker == PlanCodes.NotEnded), "live repeat returns blockers for both actual members");
            Reach(game, null, () => Nodes(game, "field").Any(n => n.Id == "build" && n.ProductiveSteps > 0), "real supplied partial first construction");
            var earned = Nodes(game, "field");
            c.That(Stock(game, "source") == Fix.FromInt(12) && Towers(game) == 0, "actual eight inputs arrive and consume before timed completion");
            Order(c, game, new ConfigurePlanGroupCommand { Group = "field", Priority = 90, Paused = true });
            var paused = Nodes(game, "field").Select(n => n.Progress).ToArray(); var twin = factory.Load(game.Save());
            for (int i = 0; i < 4; i++) Step(game, twin);
            c.That(Nodes(game, "field").Select(n => n.Progress).SequenceEqual(paused) && Group(game, "field").All(p => p.Paused && p.Priority == 90), "whole pause retains earned work and sets actual priorities, including dependent pending entry");
            var cannotMove = view.PreviewPlanGroupMove("field", 0);
            c.That(cannotMove.Blocker == PlanCodes.NotPending && cannotMove.Entries.Count == 2
                && cannotMove.Entries[0].Blocker == PlanCodes.NotPending && cannotMove.Entries[1].Blocker.IsNone, "active/pending move reports truthful distinct member status");
            hash = game.Hash(); long at = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
            c.That(game.Clock.StepIndex == at && game.Hash() == hash, "one game clock freezes both physical and pending group members");
            game.Clock.Resume(); twin.Clock.Resume();
            c.That(view.CapturePlanTemplate("field", At(12), out var captured).Ready
                && captured.Entries[1].Dependencies.SequenceEqual(new[] { "entry.1" })
                && !captured.ToJson().ToString().Contains("progress") && !captured.ToJson().ToString().Contains("order"), "active capture contains authored relative intent/dependencies only");
            Order(c, game, new ConfigurePlanGroupCommand { Group = "field", Priority = 90, Paused = false }); twin = factory.Load(game.Save());
            Reach(game, twin, () => Group(game, "field").All(p => p.State == PlanState.Done), "saved linked group finishes real sequential construction");
            var done = Group(game, "field");
            c.That(done[0].EndedTick < done[1].EndedTick && Towers(game) == 2 && Sites(game) == 0
                && Stock(game, "source") == Fix.FromInt(4) && Stock(game, "machine") == Fix.Zero, "dependency orders physical completion and exactly sixteen actual inputs consumed");
            c.That(earned.Any(n => n.ProductiveSteps > 0) && seed.All(p => p.State == PlanState.Pending), "retained node and intent views remain detached");
            var collision = view.PreviewPlanTemplateActivation(PlanTemplateSelection.Snapshot(template), At(12), "occupied");
            c.That(collision.Blocker == Codes.Blocked && collision.Entries.Count == 2 && collision.Entries.All(e => e.Blocker == Codes.Blocked), "completed real structures yield per-entry current activation blockers");
            Order(c, game, new RepeatPlanGroupCommand { Group = "field", NewGroup = "occupied" });
            c.That(Group(game, "occupied").All(p => p.State == PlanState.Pending) && Sites(game) == 0 && Towers(game) == 2
                && Stock(game, "source") == Fix.FromInt(4), "repeat records intent, actual occupied world cannot instantiate completed sites or spend materials");
            Order(c, game, new CancelPlanGroupCommand { Group = "occupied" });
            Order(c, game, new CancelPlanGroupCommand { Group = "other" });
            c.That(Group(game, "occupied").All(p => p.State == PlanState.Cancelled) && Towers(game) == 2 && Sites(game) == 0, "cancel pending repeats never deletes completed structures or resurrects work");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed grouped history and actual states durable");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "ordinary grouped control and physical work replay");
            var fresh = New(factory); c.That(All(fresh).Count == 0 && Towers(fresh) == 0 && Stock(fresh, "source") == Fix.FromInt(20), "interleaved session keeps original planning/material isolation");
            return new[] { done[0].EndedTick, done[1].EndedTick, game.Clock.StepIndex, ActiveCancellation(c, direct) };
        }
        private static long ActiveCancellation(Check c, bool direct)
        {
            var factory = Factory(out _, direct); var game = New(factory); var view = View(game);
            Enable(c, game); var limits = new ReplayLimits(4 * 1024 * 1024, 3000); var recorder = game.RecordReplay(limits);
            Place(c, game, Template(game), 12, "cancel-active");
            Order(c, game, new ConfigurePlanGroupCommand { Group = "cancel-active", Priority = 70, Paused = false });
            Reach(game, null, () => Nodes(game, "cancel-active").Any(n => n.Id == "build" && n.ProductiveSteps > 0), "active group earns partial material-consuming construction");
            var entries = Group(game, "cancel-active"); var earned = Nodes(game, "cancel-active");
            c.That(entries[0].State == PlanState.Active && entries[1].State == PlanState.Pending
                && Stock(game, "source") == Fix.FromInt(12) && Towers(game) == 0, "active predecessor consumes actual inputs, successor remains dependency-blocked");
            var preview = view.PreviewPlanGroupCancel("cancel-active");
            c.That(preview.Ready && preview.Entries.Count == 2 && preview.Entries.All(e => e.Blocker.IsNone), "whole cancellation admits both active and pending actual members");
            var command = new CancelPlanGroupCommand { Group = "cancel-active" }; ulong hash = game.Hash();
            c.Equal(Code.None, game.Seat(0).Preview(command), "active whole command shares the same admission");
            c.Equal(hash, game.Hash(), "active cancellation preview leaves progress/material/claims untouched");
            game.Seat(0).Submit(command); var twin = factory.Load(game.Save()); Step(game, twin);
            c.That(Group(game, "cancel-active").All(p => p.State == PlanState.Cancelled)
                && Sites(game) == 0 && Towers(game) == 0 && Stock(game, "source") == Fix.FromInt(12) && Stock(game, "machine") == Fix.Zero,
                "queued saved cancellation retires actual work, never refunds consumed eight or completes either structure");
            for (int i = 0; i < 4; i++) Step(game, twin);
            c.That(earned.Any(n => n.ProductiveSteps > 0) && Sites(game) == 0 && Towers(game) == 0
                && Group(game, "cancel-active").All(p => p.State == PlanState.Cancelled), "detached earned facts remain and no cancelled dependent work resurrects");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "ordinary partial group cancellation and saved inbox replay");
            return game.Clock.StepIndex;
        }
        private static void Faults(Check c)
        {
            foreach (string operation in new[] { "validate", "anchor", "capture", "place", "activation" })
                foreach (string fault in new[] { "throw", "pause", "identity", "reenter", "swallowed" })
                {
                    var factory = Factory(out var proof); var game = New(factory); var view = View(game); var template = Template(game);
                    Place(c, game, template, 12, "negative");
                    proof.ArmTemplateFault(operation, fault, template, At(30));
                    c.Throws<InvalidOperationException>(() => {
                        if (operation == "validate") view.PreviewPlanTemplate(template, At(30), "probe");
                        if (operation == "anchor") view.PreviewPlanTemplateStructure(template, At(30));
                        if (operation == "capture") view.CapturePlanTemplate("negative", At(12), out _);
                        if (operation == "place") view.ExpandPlanTemplate(template, At(30), "probe", out _);
                        if (operation == "activation") view.PreviewPlanTemplateActivation(PlanTemplateSelection.Snapshot(template), At(30), "probe");
                    }, "actual template observation refuses " + operation + "/" + fault);
                    c.That(game.Fault != null, "template fault latched " + operation + "/" + fault);
                    if (fault == "throw") c.That(game.Fault?.Message.Contains("proof.template.first") == true, "original callback error preserved " + operation);
                    c.Throws<InvalidOperationException>(() => game.Save(), "faulted template cannot publish save " + operation + "/" + fault);
                    c.Throws<InvalidOperationException>(() => game.Hash(), "faulted template cannot publish hash " + operation + "/" + fault);
                    c.Throws<InvalidOperationException>(() => view.PreviewPlanTemplate(null, null, "invalid"), "faulted even invalid early-return query refuses " + operation + "/" + fault);
                    c.Throws<InvalidOperationException>(() => view.PreviewPlanTemplateSelection(null, null, "invalid"), "faulted selection early-return refuses " + operation + "/" + fault);
                    c.Throws<InvalidOperationException>(() => view.ReadStoredPlanTemplate(0, out _), "faulted stored lookup early-return refuses " + operation + "/" + fault);
                    c.Throws<InvalidOperationException>(() => view.AuthoredPlanTemplate("missing"), "faulted authored lookup early-return refuses " + operation + "/" + fault);
                }
            var healthyFactory = Factory(out var healthyProof); var healthy = New(healthyFactory); var healthyView = View(healthy); var healthyTemplate = Template(healthy);
            ulong hash = healthy.Hash(); healthyProof.ArmTemplateFault("validate", "content", healthyTemplate, At(12));
            var refusal = healthyView.PreviewPlanTemplate(healthyTemplate, At(12), "invalid");
            c.That(refusal.Blocker == Codes.InvalidArgument && refusal.Entries.Count == 2 && healthy.Fault == null, "ordinary authoring exception remains typed per-entry, not a session fault");
            healthyProof.ClearTemplateFault(); c.Equal(hash, healthy.Hash(), "typed refusal leaves all authoritative state untouched");
        }
    }
}
