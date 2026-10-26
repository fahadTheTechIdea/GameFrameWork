using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.TransformationContentContractTests;
namespace Beep.SimEngine.Tests
{
    // Content authoring, ordinary player commands and detached facts only; no line/work simulation.
    internal static partial class LineDefinitionContractTests
    {
        internal const string Case = "Line definitions: independent master JSON drives physical DAG corridors and plans";
        internal static void Register() { TestRunner.Add(Case, Run); TestRunner.Add(MaterialCase, TopologyNotMaterial); }
        private static GameFactory Create(out LineDefinitionContractsModule proof, bool independent = true, Action<JsonValue> edit = null,
            Action<JsonValue> infrastructureEdit = null)
        {
            proof = new LineDefinitionContractsModule();
            var data = Json.Parse(FrameworkProofContent.Read("line-definition-contracts.json"));
            if (!independent) data.Set("independent_lines", JsonValue.NewArray());
            edit?.Invoke(data);
            var infrastructure = Json.Parse(FrameworkProofContent.Read("authored-segment-infrastructure.json"));
            if (independent) infrastructure.Remove("lines");
            infrastructureEdit?.Invoke(infrastructure);
            return AuthoredSegmentWorkflowTests.Factory(infrastructure: infrastructure.ToString(), extraModule: proof,
                extraDocument: "line-definition-contracts.json", extraJson: data.ToString());
        }
        internal static SimGame New(GameFactory factory)
        { var request = new NewGameRequest { Scenario = "native-survey", Seed = 754 }; request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request); }
        internal static WorkView Work(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<WorkView>();
        internal static List<OrderView> Orders(SimGame game) { var rows = new List<OrderView>(); Work(game).CopyOrders(rows); return rows; }
        internal static List<ActivityNodeView> Nodes(SimGame game, int order)
        { var rows = new List<ActivityNodeView>(); if (!Work(game).TryCopyNodes(order, rows)) throw new InvalidOperationException("missing order " + order); return rows; }
        internal static List<PlanView> Plans(SimGame game, int seat = 0) { var rows = new List<PlanView>(); Work(game, seat).CopyPlans(rows); return rows; }
        internal static Fix Steel(SimGame game)
        {
            Fix sum = Fix.Zero;
            foreach (var entity in game.Seat(0).Snapshot.Entities)
            { var rows = new List<Facet>(); game.Seat(0).View.Inspect(entity.Id, rows); foreach (var row in rows) if (row.Key == Storage.FacetStock && row.Subject == "steel") sum += row.Value; }
            return sum;
        }
        internal static int Tile(SimGame game, string layer, int x, int y)
        {
            var rows = new List<int>();
            var status = game.Seat(0).View.Feature<TileMapView>().ReadLayer(layer, new TileRect(x, y, 1, 1), rows);
            if (status != SpatialLayerReadStatus.Success || rows.Count != 1) throw new InvalidOperationException("tile " + status);
            return rows[0];
        }
        internal static void Reach(SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 900 && !ready(); i++) Step(game, twin);
            if (!ready()) throw new InvalidOperationException(stage + " clock=" + game.Clock.StepIndex + " orders=" +
                string.Join(";", Orders(game).Select(o => o.Id + "/" + o.State + "/" + o.Blocker + "/" + o.NodesDone)));
        }
        private static LayLineCommand Lay(string id = "segment.road-line", int y = 10)
        { var command = new LayLineCommand { Line = id }; command.Tiles.AddRange(new[] { new TileCoord(16, y), new TileCoord(17, y), new TileCoord(18, y) }); return command; }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current contract build"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "current original line/graph/job payloads");
            c.That(typeof(IndependentLineDef).BaseType == typeof(Def), "no native line base");
            c.Equal(Physical(c, false), Physical(c, true), "native/direct same physical chronology");
            foreach (string fault in new[] { "layer-null", "foreign-layer", "value-zero", "placement-null", "plan-wrong", "graph-null", "graph-source", "supply-mode", "supply-null", "supply-extra", "segment-wrong",
                "missing-activity", "empty-activity", "unknown-activity", "missing-segment_node", "empty-segment_node", "unknown-segment_node", "missing-layer", "empty-layer", "unknown-layer",
                "missing-work_marker", "empty-work_marker", "unknown-work_marker", "missing-finish_effect", "empty-finish_effect", "unknown-finish_effect", "legacy-flat" })
            {
                LineDefinitionContractsModule proof = null;
                c.Throws<ContentException>(() => New(Create(out proof, edit: data =>
                {
                    var row = data.Get("independent_lines")[0].Clone(); row.Set("id", JsonValue.Of("segment.unused")); data.Get("independent_lines").Add(row);
                    row.Set("metadata_fault", JsonValue.Of(fault));
                    foreach (string key in new[] { "activity", "segment_node", "layer", "work_marker", "finish_effect" })
                    { if (fault == "missing-" + key) row.Remove(key); if (fault == "empty-" + key) row.Set(key, JsonValue.Of("")); if (fault == "unknown-" + key) row.Set(key, JsonValue.Of("missing.reference")); }
                    if (fault == "legacy-flat") row.Set("duration_per_tile", JsonValue.Number("1"));
                })), "unused malformed line refused " + fault);
                c.Equal(0, proof.Initialize.Initializations, "admission precedes Initialize " + fault);
            }
            // Negative command-protocol instrumentation; positive physical scenarios above use players only.
            foreach (string fault in new[] { "live-throw", "live-pause", "live-intent-throw", "live-intent-pause" })
            foreach (bool apply in new[] { false, true })
            {
                var factory = Create(out _, edit: data => data.Get("independent_lines")[0].Set("metadata_fault", JsonValue.Of(fault)));
                var game = New(factory); Step(game); var independent = New(Create(out _)); ulong isolated = independent.Hash();
                string context = fault + "/apply=" + apply; Exception caught = null;
                try {
                    if (!apply) game.Seat(0).Preview(Lay());
                    else {
                        var command = Lay(); command.Player = new PlayerId(0);
                        if (!game.World.Commands.TryGet(typeof(LayLineCommand), out var registration)) throw new InvalidOperationException("actual line command missing");
                        registration.Handler.Apply(game.World, command, new CommandContext());
                    }
                } catch (Exception error) { caught = error; }
                c.That(caught is InvalidOperationException, "live metadata read refuses before returning intent: " + context);
                c.That(game.World.Fault?.Phase == "observation.provider", "original line observation owns first fault: " + context);
                c.That(caught != null && caught.Message.Contains(fault.EndsWith("throw", StringComparison.Ordinal) ? "proof.line-definition." : "game-clock"), "original callback/scalar diagnostic retained: " + context);
                c.Throws<InvalidOperationException>(() => game.Save(), "faulted save refuses " + context);
                c.Throws<InvalidOperationException>(() => game.Hash(), "faulted hash refuses " + context);
                var first = game.World.Fault; long time = game.Clock.StepIndex; game.Update();
                c.Equal(time, game.Clock.StepIndex, "failed metadata cannot advance time: " + context);
                c.That(ReferenceEquals(first, game.World.Fault), "first fault retained: " + context);
                c.Equal(isolated, independent.Hash(), "independent content/session not mutated: " + context); Step(independent);
            }
        }
        private static long Physical(Check c, bool independent)
        {
            var factory = Create(out var proof, independent); var game = New(factory); var isolated = New(factory); ulong untouched = isolated.Hash();
            c.Equal(independent, proof.ActualContract, "actual owned line alias"); c.Equal(2, proof.Initialize.Initializations, "one Initialize per session");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "physical construction has no business dependency");
            var origins = game.Seat(0).Snapshot.Entities.Where(e => e.Def == "segment.machine").Select(e => ((TileSpatialSnapshot)e.Spatial).Position).ToArray();
            var limits = new ReplayLimits(4 * 1024 * 1024, 5000); var recorder = game.RecordReplay(limits);
            Order(c, game, Lay()); var order = Orders(game).Single();
            c.That(order.NodeCount == 9 && order.NodesDone == 0, "three authored stages bound to each path tile with no earned work");
            var nodes = Nodes(game, order.Id);
            c.Equal("segment.0.install", nodes.Single(n => n.Id == "segment.1.prepare").Dependencies.Single(), "next tile waits on prior terminal");
            c.Equal("segment.0.inspect|segment.0.prepare", string.Join("|", nodes.Single(n => n.Id == "segment.0.install").Dependencies.OrderBy(x => x)), "parallel prerequisites retained");
            c.Equal(0, Tile(game, "segment.road", 16, 10), "command cannot stamp completed topology");
            c.That(!Work(game, 1).TryCopyNodes(order.Id, new List<ActivityNodeView>()), "foreign company cannot read private workflow");
            Reach(game, null, () => Nodes(game, order.Id).Where(n => n.Id == "segment.0.prepare" || n.Id == "segment.0.inspect").All(n => n.Progress.WholeTicks > 0), "parallel physical preparation");
            var retained = Nodes(game, order.Id).Single(n => n.Id == "segment.0.prepare"); var retainedProgress = retained.Progress;
            c.Equal(Fix.FromInt(40), Steel(game), "held tools not consumed");
            c.That(game.Seat(0).Snapshot.Entities.Where(e => e.Def == "segment.machine").Select((e, i) => ((TileSpatialSnapshot)e.Spatial).Position != origins[i]).Any(x => x), "machines physically moved");
            c.Equal(0L, Nodes(game, order.Id).Single(n => n.Id == "segment.0.install").Progress.WholeTicks, "terminal waits on both branches");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial DAG saves original jobs and claims");
            long at = game.Clock.StepIndex; ulong hash = game.Hash(); game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
            c.That(at == game.Clock.StepIndex && hash == game.Hash(), "one paused game clock freezes work"); game.Clock.Resume(); twin.Clock.Resume();
            Reach(game, twin, () => Tile(game, "segment.road", 16, 10) == 1, "first earned physical segment");
            c.Equal(0, Tile(game, "segment.road", 17, 10), "future geometry remains absent"); c.Equal(Fix.FromInt(38), Steel(game), "one segment consumes actual two-unit bill");
            c.Equal(retainedProgress, retained.Progress, "retained facts immutable"); twin = factory.Load(game.Save());
            Reach(game, twin, () => Plans(game).Single(p => p.Order == order.Id).State == PlanState.Done, "full corridor");
            c.Equal(Fix.FromInt(34), Steel(game), "six units consumed exactly");
            c.That(game.Seat(0).Snapshot.Entities.All(e => e.Def != "sim.line-work.segment.road-line"), "completed cursor removed by original final effect");
            for (int x = 16; x <= 18; x++) c.Equal(1, Tile(game, "segment.road", x, 10), "earned corridor retained");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed original payload restores");
            c.Equal(untouched, isolated.Hash(), "interleaved sessions isolated");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "ordinary plan/physical work/clock input replay");
            long completed = game.Clock.StepIndex;
            // Public plan/template input is player intent, not a duplicate placement algorithm.
            var arguments = Json.Parse("{\"space\":\"survey.tiles\",\"path\":[{\"space\":\"survey.tiles\",\"x\":16,\"y\":20},{\"space\":\"survey.tiles\",\"x\":17,\"y\":20}]}");
            Order(c, game, new SubmitPlanCommand { Intent = new PlanIntent("line:segment.pipe-line", arguments, "corridor", 60, true) });
            c.That(Plans(game).Last().Paused && Plans(game).Last().State == PlanState.Pending, "paused plan owns intent only");
            c.That(Plans(game, 1).Count == 0, "plan board private");
            c.That(Work(game).CapturePlanTemplate("corridor", TilePlacement.Arguments("survey.tiles", new TileCoord(14, 20)), out var template).Ready, "engine captures portable corridor");
            c.That(template.Entries[0].Arguments.Get("path")[0].Get("dx").Text == "2", "original spatial provider owns relative intent");
            Order(c, game, new PlacePlanTemplateCommand { Selection = PlanTemplateSelection.Snapshot(template), Anchor = TilePlacement.Arguments("survey.tiles", new TileCoord(24, 20)), Group = "copy" });
            c.Equal(2, Plans(game).Count(p => p.State == PlanState.Pending), "same plan board accepts translated intent");
            c.Equal(0, Tile(game, "segment.pipe", 26, 20), "template placement never builds");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "pending template plans restore");
            Order(c, game, Lay("segment.pipe-line", 14)); int pipeOrder = Orders(game).Last().Id;
            Reach(game, null, () => Tile(game, "segment.pipe", 16, 14) == 1 && Nodes(game, pipeOrder).Any(n => n.Id == "segment.1.prepare" && n.Progress.WholeTicks > 0), "partial pipe stage");
            Order(c, game, new CancelOrderCommand { Order = pipeOrder });
            Reach(game, null, () => Plans(game).Single(p => p.Order == pipeOrder).State == PlanState.Cancelled, "normal cancellation");
            Step(game);
            c.Equal(1, Tile(game, "segment.pipe", 16, 14), "cancel preserves earned prefix"); c.Equal(0, Tile(game, "segment.pipe", 17, 14), "cancel earns no future segment");
            c.Equal(Fix.FromInt(32), Steel(game), "cancel retains unused materials"); c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled original execution restores");
            return completed;
        }
    }
}
