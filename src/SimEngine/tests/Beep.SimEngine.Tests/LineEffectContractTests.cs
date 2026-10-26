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
    // Thin player/content driver: simulation, requirements and completion exist only in the engine.
    internal static class LineEffectContractTests
    {
        internal const string Case = "Line effects: direct master JSON earns physical segments and whole-order completion";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Create(out LineEffectContractsModule proof, bool independent = true,
            Action<JsonValue> edit = null, Action<JsonValue> workEdit = null, Action<JsonValue> lineEdit = null, bool provideCorruption = true)
        {
            proof = provideCorruption ? new LineEffectContractsModule(LineCursorCorruption.Order) : new LineEffectContractsModule();
            var data = Json.Parse(FrameworkProofContent.Read("line-effect-contracts.json"));
            var infrastructure = Json.Parse(FrameworkProofContent.Read("authored-segment-infrastructure.json"));
            var work = Json.Parse(FrameworkProofContent.Read("authored-segment-work.json"));
            if (independent) infrastructure.Remove("line_effects");
            else data.Set("independent_line_effects", JsonValue.NewArray());
            edit?.Invoke(data); workEdit?.Invoke(work); lineEdit?.Invoke(infrastructure);
            return AuthoredSegmentWorkflowTests.Factory(work.ToString(), infrastructure.ToString(), proof,
                "line-effect-contracts.json", data.ToString());
        }
        private static SimGame New(GameFactory factory)
        { var request = new NewGameRequest { Scenario = "native-survey", Seed = 754 }; request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request); }
        private static WorkView Work(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<WorkView>();
        private static List<OrderView> Orders(SimGame game) { var result = new List<OrderView>(); Work(game).CopyOrders(result); return result; }
        private static List<ActivityNodeView> Nodes(SimGame game, int id)
        { var result = new List<ActivityNodeView>(); if (!Work(game).TryCopyNodes(id, result)) throw new InvalidOperationException("missing live graph " + id); return result; }
        private static List<PlanView> Plans(SimGame game) { var result = new List<PlanView>(); Work(game).CopyPlans(result); return result; }
        private static int Tile(SimGame game, string layer, int x, int y)
        {
            var rows = new List<int>();
            if (game.Seat(0).View.Feature<TileMapView>().ReadLayer(layer, new TileRect(x, y, 1, 1), rows) != SpatialLayerReadStatus.Success || rows.Count != 1)
                throw new InvalidOperationException("missing layer tile");
            return rows[0];
        }
        private static Fix Steel(SimGame game)
        {
            Fix sum = Fix.Zero;
            foreach (var entity in game.Seat(0).Snapshot.Entities)
            { var rows = new List<Facet>(); game.Seat(0).View.Inspect(entity.Id, rows); foreach (var row in rows) if (row.Key == Storage.FacetStock && row.Subject == "steel") sum += row.Value; }
            return sum;
        }
        private static LayLineCommand Lay(string line = "segment.road-line", int y = 10)
        { var value = new LayLineCommand { Line = line }; value.Tiles.AddRange(new[] { new TileCoord(16, y), new TileCoord(17, y), new TileCoord(18, y) }); return value; }
        private static void Reach(SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 900 && !ready(); i++) Step(game, twin);
            if (!ready()) throw new InvalidOperationException(stage + " clock=" + game.Clock.StepIndex + " orders=" +
                string.Join(";", Orders(game).Select(o => o.Id + "/" + o.State + "/" + o.Blocker + "/" + o.NodesDone)));
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current exact build");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "current independent protocol expectation, no duplicate completion state");
            c.That(typeof(IndependentLineEffectDef).BaseType == typeof(Def), "direct contract without native inheritance");
            LineEffectContractsModule missing = null;
            c.Throws<ContentException>(() => New(Create(out missing,
                edit: data => data.Get("independent_line_effects")[0].Set("metadata_fault", JsonValue.Of("live-cursor")),
                provideCorruption: false)), "selected privileged negative probe requires explicit test dependency");
            c.Equal(0, missing.Initialize.Initializations, "missing negative dependency refuses before Initialize");
            c.Equal(Physical(c, false), Physical(c, true), "native/direct physical completion chronology");
            foreach (string fault in new[] { "missing-operation", "empty-operation", "unknown-operation", "instant-field", "invalid-enum", "id-alias", "module-alias", "getter-throw" })
            {
                LineEffectContractsModule proof = null;
                c.Throws<ContentException>(() => New(Create(out proof, edit: data =>
                {
                    var row = data.Get("independent_line_effects")[0].Clone(); row.Set("id", JsonValue.Of("segment.unused"));
                    data.Get("independent_line_effects").Add(row);
                    if (fault == "missing-operation") row.Remove("operation");
                    else if (fault == "empty-operation") row.Set("operation", JsonValue.Of(""));
                    else if (fault == "unknown-operation") row.Set("operation", JsonValue.Of("teleport"));
                    else if (fault == "instant-field") row.Set("instant", JsonValue.Of(true));
                    else row.Set("metadata_fault", JsonValue.Of(fault));
                })), "unused effect refusal " + fault);
                c.Equal(0, proof.Initialize.Initializations, "refusal before Initialize " + fault);
            }
            foreach (string fault in new[] { "early-segment", "terminal-finish", "final-segment", "unknown-final", "unused-node-finish", "unused-final-segment" })
            {
                LineEffectContractsModule proof = null;
                c.Throws<ContentException>(() => New(Create(out proof, workEdit: work =>
                {
                    var activity = work.Get("activities")[0];
                    if (fault == "early-segment") activity.Get("nodes")[1].Set("effect", JsonValue.Of("segment.stamp"));
                    if (fault == "terminal-finish") activity.Get("nodes")[0].Set("effect", JsonValue.Of("segment.finish"));
                    if (fault.StartsWith("unused-", StringComparison.Ordinal))
                    {
                        var unused = activity.Clone(); unused.Set("id", JsonValue.Of("segment.unused-activity")); work.Get("activities").Add(unused);
                        if (fault == "unused-node-finish") unused.Get("nodes")[0].Set("effect", JsonValue.Of("segment.finish"));
                        else unused.Set("on_complete", JsonValue.Of("segment.stamp"));
                    }
                }, lineEdit: lines =>
                {
                    if (fault == "final-segment") lines.Get("lines")[0].Set("finish_effect", JsonValue.Of("segment.stamp"));
                    if (fault == "unknown-final") lines.Get("lines")[0].Set("finish_effect", JsonValue.Of("missing.effect"));
                })), "effect position refusal " + fault);
                c.Equal(0, proof.Initialize.Initializations, "position admission before Initialize " + fault);
            }
            foreach (string fault in new[] { "live-throw", "live-pause", "live-touch", "live-cursor" })
            {
                var game = New(Create(out var proof, edit: data => data.Get("independent_line_effects")[0].Set("metadata_fault", JsonValue.Of(fault))));
                Order(c, game, Lay()); int id = Orders(game).Single().Id; Step(game);
                c.Equal(0, Tile(game, "segment.road", 16, 10), "healthy pre-fault physical work " + fault);
                proof.ArmObservationFault();
                c.Throws<Exception>(() => Work(game).PreviewNodeEffect(id, "segment.0.install"), "selected callback fault " + fault);
                c.Throws<InvalidOperationException>(() => game.Save(), "faulted save refuses " + fault);
                c.Throws<InvalidOperationException>(() => game.Hash(), "faulted hash refuses " + fault);
            }
        }
        private static long Physical(Check c, bool independent)
        {
            var factory = Create(out var proof, independent); var game = New(factory); var isolated = New(factory); ulong untouched = isolated.Hash();
            c.That(proof.ActualAliases, "actual line/effect/work aliases"); c.Equal(independent, proof.Independent, "selected normal implementation");
            c.Equal(2, proof.Initialize.Initializations, "same engine initialization per session");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "physical work independent of business");
            var origins = game.Seat(0).Snapshot.Entities.Where(e => e.Def == "segment.machine").Select(e => ((TileSpatialSnapshot)e.Spatial).Position).ToArray();
            var limits = new ReplayLimits(4 * 1024 * 1024, 5000); var recorder = game.RecordReplay(limits);
            Order(c, game, Lay()); int id = Orders(game).Single().Id;
            c.Equal(9, Orders(game).Single().NodeCount, "authored three-stage graph per tile");
            ulong hash = game.Hash();
            c.Equal(WorkEffectCodes.NotComplete, Work(game).PreviewNodeEffect(id, "segment.1.install"), "future segment refuses");
            c.Equal(WorkEffectCodes.NotComplete, Work(game).PreviewOrderEffect(id), "whole-order finish refuses early");
            c.Equal(Codes.NotFound, Work(game, 1).PreviewNodeEffect(id, "segment.0.install"), "private effect readiness discloses no foreign order existence");
            c.Equal(hash, game.Hash(), "refused previews leave actual state unchanged");
            c.Equal(0, Tile(game, "segment.road", 16, 10), "command and previews stamp no geometry");
            Reach(game, null, () => Nodes(game, id).Where(n => n.Id == "segment.0.prepare" || n.Id == "segment.0.inspect").All(n => n.Progress.WholeTicks > 0), "parallel physical stages");
            c.Equal(Fix.FromInt(40), Steel(game), "held tools remain actual stock");
            c.That(game.Seat(0).Snapshot.Entities.Where(e => e.Def == "segment.machine").Select((e, i) => ((TileSpatialSnapshot)e.Spatial).Position != origins[i]).Any(x => x), "actual travel precedes construction");
            var retained = Nodes(game, id).Single(n => n.Id == "segment.0.prepare"); var progress = retained.Progress;
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial original claims/jobs restore");
            hash = game.Hash(); long at = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
            c.That(hash == game.Hash() && at == game.Clock.StepIndex, "one paused clock freezes work"); game.Clock.Resume(); twin.Clock.Resume();
            Reach(game, twin, () => Tile(game, "segment.road", 16, 10) == 1, "earned segment terminal");
            c.Equal(0, Tile(game, "segment.road", 17, 10), "future tile remains unearned"); c.Equal(Fix.FromInt(38), Steel(game), "one original material consumption");
            c.Equal(progress, retained.Progress, "detached progress immutable"); twin = factory.Load(game.Save());
            Reach(game, twin, () => Plans(game).Single(p => p.Order == id).State == PlanState.Done, "whole-order physical finish");
            for (int x = 16; x <= 18; x++) c.Equal(1, Tile(game, "segment.road", x, 10), "earned completed geometry");
            c.That(game.Seat(0).Snapshot.Entities.All(e => e.Def != "sim.line-work.segment.road-line"), "final effect retires original cursor");
            c.Equal(Fix.FromInt(34), Steel(game), "three exact material bills, no duplicates");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed original payload restores"); long completed = game.Clock.StepIndex;
            Order(c, game, Lay("segment.pipe-line", 14)); int cancelled = Orders(game).Last().Id;
            Reach(game, null, () => Tile(game, "segment.pipe", 16, 14) == 1 && Nodes(game, cancelled).Any(n => n.Id == "segment.1.prepare" && n.Progress.WholeTicks > 0), "cancel after earned prefix");
            Order(c, game, new CancelOrderCommand { Order = cancelled });
            Reach(game, null, () => Plans(game).Single(p => p.Order == cancelled).State == PlanState.Cancelled, "normal cancellation"); Step(game);
            c.Equal(1, Tile(game, "segment.pipe", 16, 14), "cancel retains earned geometry"); c.Equal(0, Tile(game, "segment.pipe", 17, 14), "cancel never earns future tile");
            c.Equal(Fix.FromInt(32), Steel(game), "cancel releases unused tools/materials");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled original graph saves");
            c.Equal(untouched, isolated.Hash(), "interleaved session isolation");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "actual command/clock/completion/cancellation replay");
            return completed;
        }
    }
}
