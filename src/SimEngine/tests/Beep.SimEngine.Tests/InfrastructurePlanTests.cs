using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static partial class LineSegmentTests
    {
        private static GameFactory PlannedFactory(Action<JsonValue> edit = null)
        {
            var plans = Json.Parse(FrameworkProofContent.Read("infrastructure-plans.json")); edit?.Invoke(plans);
            return Colony.Factory(FrameworkProofContent.Read("resource-recovery.json"), FrameworkProofContent.Read("endpoints-flows.json"),
                additionalDocuments: new Dictionary<string, string> { ["line-segments.json"] = FrameworkProofContent.Read("line-segments.json"), ["infrastructure-plans.json"] = Text(plans) });
        }
        private static WorkView PlanView(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<WorkView>();
        private static List<PlanView> Plans(SimGame game, int seat = 0) { var list = new List<PlanView>(); PlanView(game, seat).CopyPlans(list); return list; }
        private static PlanView ReadPlan(SimGame game, long plan) => Plans(game).Find(p => p.Id == plan);
        private static ILineDefinition Road(SimGame game) => game.World.Content.Get<ILineDefinition>("segment.road");
        private static JsonValue Anchor(SimGame game, int x = 20, int y = 20) => TilePlacement.Arguments(game.World.Space.Id, new TileCoord(x, y));
        private static void PlanContracts(Check c)
        {
            var factory = PlannedFactory(); var game = New(factory); var view = PlanView(game); ILineDefinition line = Road(game);
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, Board(game).StateSchemaVersion, "same board owns explicit target-bound graph reconstruction");
            c.That(ReferenceEquals(line, Board(game).ResolvePlan("line:segment.road")) && Board(game).ResolvePlan("lay:1") == null,
                "stable line prototype aliases exact master content; numeric marker-key reader removed");
            var path = new List<TileCoord>(Path()); PlanIntent intent = line.CreateIntent(TilePlacement.PathArguments(line.Layer.Space.Id, path), "corridor", 70, true); path[0] = new TileCoord(2, 2);
            c.Equal("20", intent.Arguments.Get("path")[0].Get("x").Text, "intent freezes ordered path, never caller's mutable list");
            c.Throws<InvalidOperationException>(() => intent.Arguments.Get("path")[0].Set("x", JsonValue.Number("3")), "nested geometry frozen");
            ulong hash = game.Hash(); c.Equal(Code.None, view.PreviewPlan(intent), "persistent corridor admits before any cursor exists");
            foreach (Action<JsonValue> edit in new Action<JsonValue>[] {
                j => j.Remove("space"), j => j.Remove("path"), j => j.Set("path", JsonValue.NewArray()),
                j => j.Set("space", JsonValue.Of("other")), j => j.Set("target", JsonValue.Number("1")),
                j => j.Get("path")[1].Remove("x"), j => j.Get("path")[1].Set("order", JsonValue.Number("1"))
            }) { var bad = intent.Arguments.Clone(); edit(bad); c.Equal(Codes.InvalidArgument, view.PreviewPlan(new PlanIntent(line.PlanKey, bad, null, 1, true)), "strict corridor schema refuses missing/extra/live-state input"); }
            c.Equal(LineCodes.NotContiguous, view.PreviewPlan(line.CreateIntent(TilePlacement.PathArguments(line.Layer.Space.Id, new[] { Path()[0], Path()[0] }), null, 1, true)), "repeated path refuses");
            c.Equal(LineCodes.NotContiguous, view.PreviewPlan(line.CreateIntent(TilePlacement.PathArguments(line.Layer.Space.Id, new[] { Path()[0], new TileCoord(28, 20) }), null, 1, true)), "noncontiguous path refuses");
            c.Equal(Codes.OutOfBounds, view.PreviewPlan(line.CreateIntent(TilePlacement.PathArguments(line.Layer.Space.Id, new[] { new TileCoord(-1, 20) }), null, 1, true)), "actual authored bounds refuse without allocation");
            c.Equal(hash, game.Hash(), "all infrastructure plan previews are pure");
            var accepted = TestKit.Order(game, 0, new SubmitPlanCommand { Intent = intent }); c.That(accepted.Accepted, "player posts pending native corridor intent");
            c.That(ReadPlan(game, accepted.Arg0).State == PlanState.Pending && Board(game).Orders.Count == 0, "paused intent creates no native cursor/order");
            c.Equal(PlanCodes.Duplicate, view.PreviewPlan(intent), "whole ordered path has stable pre-allocation duplicate identity");
            c.That(Plans(game, 1).Count == 0, "private company plan view reveals no corridor");
            c.Equal(Codes.NotOwned, PlanView(game, 1).PreviewPlanCancel(new PlanCancellationSelection(false, new[] { accepted.Arg0 })).Blocker, "foreign control refuses same board owner");
            c.That(view.CapturePlanTemplate("corridor", Anchor(game, 18), out PlanTemplate captured).Ready, "existing capture reads original corridor intent");
            c.Equal("2", captured.Entries[0].Arguments.Get("path")[0].Get("dx").Text, "spatial provider owns relative offset");
            c.That(!captured.ToJson().ToString().Contains("target") && !captured.ToJson().ToString().Contains("order"), "portable corridor has no live identities/graph/progress");
            c.That(TestKit.Order(game, 0, new PlacePlanTemplateCommand { Selection = PlanTemplateSelection.Snapshot(captured), Anchor = Anchor(game, 30), Group = "translated" }).Accepted,
                "generic command translates corridor through same provider");
            c.Equal("32", Plans(game).Find(p => p.Group == "translated").Arguments.Get("path")[0].Get("x").Text, "actual translated path is engine-owned");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "pending path plans roundtrip without runtime graph serialization");
            var mixed = view.AuthoredPlanTemplate("infrastructure.field"); c.That(mixed != null && mixed.Entries.Count == 2 && game.World.Content.Get<PlanTemplateDef>("infrastructure.field").AuthoringPath.Contains("infrastructure-plans.json"),
                "mixed structure/corridor template is explicitly master-listed");
            var another = New(factory); hash = another.Hash();
            c.That(PlanView(another).PreviewPlanTemplate(mixed, Anchor(another), "mixed").Ready, "one whole admission supports heterogeneous providers");
            c.Equal(hash, another.Hash(), "mixed expansion pure");
            c.That(TestKit.Order(another, 0, new PlacePlanTemplateCommand { Selection = PlanTemplateSelection.Snapshot(mixed), Anchor = Anchor(another), Group = "mixed" }).Accepted && Plans(another).Count == 2 && Board(another).Orders.Count == 0,
                "mixed paused placement remains intent only");
            c.That(PlanView(another).CapturePlanTemplate("mixed", Anchor(another), out PlanTemplate mixedCapture).Ready && mixedCapture.Entries[0].Definition == line.PlanKey,
                "same capture supports mixed groups without infrastructure branches in board");
            foreach (Action<JsonValue> edit in new Action<JsonValue>[] {
                j => j.Get("plan_templates")[0].Get("entries")[0].Get("arguments").Set("target", JsonValue.Number("1")),
                j => j.Get("plan_templates")[0].Get("entries")[0].Get("arguments").Get("path")[0].Set("progress", JsonValue.Number("1")),
                j => j.Get("plan_templates")[0].Get("entries")[0].Get("arguments").Get("path")[0].Set("space", JsonValue.Of("other"))
            }) c.Throws<ContentException>(() => New(PlannedFactory(edit)), "malformed portable line content refuses composition");
            var duplicate = new PlanTemplate(mixed.Space, new[] { mixed.Entries[0], new PlanTemplateEntry("again", mixed.Entries[0].Definition, mixed.Entries[0].Arguments, 1, true) });
            var untouched = New(factory); c.Equal(PlanCodes.Duplicate, PlanView(untouched).PreviewPlanTemplate(duplicate, Anchor(untouched), "duplicate").Blocker, "late template duplicate refuses entire corridor group");
            c.Equal(PlanCodes.Duplicate, TestKit.Order(untouched, 0, new PlacePlanTemplateCommand { Selection = PlanTemplateSelection.Snapshot(duplicate), Anchor = Anchor(untouched), Group = "duplicate" }).Refusal, "whole command shares late refusal");
            c.Equal(0, Plans(untouched).Count, "failed group allocates no accepted prefix");
        }
        private static void PlanExecution(Check c)
        {
            var factory = PlannedFactory(); var game = New(factory); Workers(game);
            var submitted = TestKit.Order(game, 0, new SubmitPlanCommand { Intent = Road(game).CreateIntent(TilePlacement.PathArguments(Road(game).Layer.Space.Id, Path()), "physical", 70, true) });
            c.That(submitted.Accepted && ReadPlan(game, submitted.Arg0).State == PlanState.Pending, "player stages corridor before physical activation");
            c.That(TestKit.Order(game, 0, new ConfigurePlanCommand { Plan = submitted.Arg0, Group = "physical", Priority = 70, Paused = false }).Accepted, "player starts staged corridor");
            var order = Board(game).Order(ReadPlan(game, submitted.Arg0).Order);
            c.That(order != null && order.Nodes.Count == 4 && ReferenceEquals(order.Plan, Road(game).BindExecution(game.World, order.Target)) && !ReferenceEquals(order.Plan, Road(game)),
                "one actual cursor binds path-sized graph, not representative one-node authoring shape");
            Reach(game, () => order.NodesDone == 1, "planned first physical segment");
            c.That(TestKit.Order(game, 0, new ConfigurePlanCommand { Plan = submitted.Arg0, Group = "edited", Priority = 80, Paused = true }).Accepted, "ordinary plan pause/regroup retains earned prefix");
            var twin = factory.Load(game.Save()); var progress = order.Nodes[1].Progress;
            for (int i = 0; i < 5; i++) { Tick(game); Tick(twin); }
            c.That(order.NodesDone == 1 && order.Nodes[1].Progress.Equals(progress), "paused corridor cannot advance/stamp future tiles");
            c.Equal(game.Hash(), twin.Hash(), "saved bound graph/prefix/plan settings agree");
            long time = game.Clock.StepIndex; ulong hash = game.Hash(); game.Clock.Pause(); c.Equal(0, game.Update(), "sole clock pause stops corridor gameplay");
            c.That(game.Clock.StepIndex == time && game.Hash() == hash, "no line clock or independent update loop"); game.Clock.Resume();
            c.That(TestKit.Order(game, 0, new ConfigurePlanCommand { Plan = submitted.Arg0, Group = "edited", Priority = 80, Paused = false }).Accepted &&
                TestKit.Order(twin, 0, new ConfigurePlanCommand { Plan = submitted.Arg0, Group = "edited", Priority = 80, Paused = false }).Accepted, "resume native planned corridor");
            Reach(game, () => ReadPlan(game, submitted.Arg0).State == PlanState.Done, "planned corridor completion", twin);
            c.Equal(new System.Numerics.BigInteger(Fix.FromInt(4).Raw), Consumed(game), "real four-stage material bill paid once");
            c.Equal(Fix.FromInt(9980), game.World.Services.Get<IEconomy>().Cash(new PlayerId(0)), "real per-stage costs remain engine-owned");
            c.Equal(game.Hash(), twin.Hash(), "planned corridor saves/continues through cursor retirement and graph pruning");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "terminal intent is durable without retired target graph");
            var mixed = New(factory); Workers(mixed);
            c.That(TestKit.Order(mixed, 0, new PlacePlanTemplateCommand { Selection = PlanTemplateSelection.Snapshot(PlanView(mixed).AuthoredPlanTemplate("infrastructure.field")), Anchor = Anchor(mixed), Group = "field" }).Accepted &&
                TestKit.Order(mixed, 0, new ConfigurePlanGroupCommand { Group = "field", Priority = 60, Paused = false }).Accepted, "same group controls start real structure and line providers");
            Reach(mixed, () => Plans(mixed).TrueForAll(p => p.State == PlanState.Done), "mixed field construction");
            c.That(Colony.Mine(mixed, 0, "tower").Count == 1 && Map(mixed).Layer<ByteLayer>("road")[Path()[3]] == 1, "mixed template finishes actual tower and physical corridor");
            c.Equal(new System.Numerics.BigInteger(Fix.FromInt(34).Raw), Consumed(mixed), "mixed providers consume independent actual thirty-plus-four material bills");
            c.Equal(mixed.Hash(), factory.Load(mixed.Save()).Hash(), "mixed completed plans retain only original intentions");
            var cancelled = New(factory); Workers(cancelled);
            var posted = TestKit.Order(cancelled, 0, new LayLineCommand { Line = "segment.road", Tiles = new List<TileCoord>(Path()) });
            c.That(posted.Accepted && ReadPlan(cancelled, posted.Arg0).State == PlanState.Active, "normal line command returns actual persistent plan ID");
            var interrupted = Board(cancelled).Order(ReadPlan(cancelled, posted.Arg0).Order);
            Reach(cancelled, () => interrupted.NodesDone == 1, "earned prefix before planned cancellation");
            var cancelledTwin = factory.Load(cancelled.Save());
            c.That(TestKit.Order(cancelled, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { posted.Arg0 }) }).Accepted &&
                TestKit.Order(cancelledTwin, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { posted.Arg0 }) }).Accepted, "player cancels active corridor through ordinary plan command");
            Reach(cancelled, () => cancelled.World.Entities.Get(interrupted.Target) == null, "cancelled corridor cursor/material cleanup", cancelledTwin);
            c.That(ReadPlan(cancelled, posted.Arg0).State == PlanState.Cancelled && Map(cancelled).Layer<ByteLayer>("road")[Path()[0]] == 1 &&
                Map(cancelled).Layer<ByteLayer>("road")[Path()[1]] == 0, "cancellation preserves earned prefix, never stamps remaining path");
            c.Equal(new System.Numerics.BigInteger(Fix.One.Raw), Consumed(cancelled), "cancelled prefix spends exactly one real segment bill");
            c.Equal(cancelled.Hash(), cancelledTwin.Hash(), "saved cancellation/physical recovery remains deterministic");
            c.Equal(cancelled.Hash(), factory.Load(cancelled.Save()).Hash(), "cancelled intent survives after cursor graph is retired");
            foreach (bool provider in new[] { false, true })
            {
                var recurring = New(factory); Workers(recurring);
                // Initial authored fixture entity, not player instant construction or host behavior.
                var controller = recurring.World.Spawn(recurring.World.Content.Get<EntityDef>("infrastructure.controller"), new PlayerId(0)); recurring.World.Settle();
                var source = new Code(provider ? "public.corridor" : "native.corridor");
                c.That(TestKit.Order(recurring, 0, new ConfigureRecurringPlanCommand { Entity = controller.Id, Source = source,
                    Settings = new RecurringPlanSettings(true, 60, GameDuration.Minutes(1), Code.None) }).Accepted, "native/public source posts authored corridor group");
                var members = Plans(recurring); long first = members[0].Id, second = members[1].Id;
                var saved = factory.Load(recurring.Save()); Reach(recurring, () => ReadPlan(recurring, first).State == PlanState.Done, "recurring first corridor", saved);
                PlanView(recurring).ReadRecurringPlan(controller.Id, source, out var observation);
                c.That(Plans(recurring).Count == 2 && ReadPlan(recurring, second).State == PlanState.Pending && observation.Schedule.LastPlan == first && observation.Blocker == RecurringPlanCodes.Outstanding,
                    "completed first corridor cannot repost while later paused path remains live");
                c.Equal(recurring.Hash(), saved.Hash(), "saved recurring physical corridor binds exact graph in each session");
                c.That(TestKit.Order(recurring, 0, new ConfigureRecurringPlanCommand { Entity = controller.Id, Source = source,
                    Settings = new RecurringPlanSettings(false, 60, GameDuration.Minutes(1), Code.None) }).Accepted &&
                    TestKit.Order(recurring, 0, new CancelPlanCommand { Selection = new PlanCancellationSelection(false, new[] { second }) }).Accepted, "normal source disable/pending cancellation uses same board");
                c.Equal(recurring.Hash(), factory.Load(recurring.Save()).Hash(), "source cycle and terminal corridor intents save without extra graph store");
            }
        }
    }
}
