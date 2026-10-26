using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    internal static class SpatialLayerAccessGuardTests
    {
        internal const string Case = "Spatial layer access: guarded extension visibility and atomic observation keys";
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(bool half = false, string stage = "none", string fault = "none")
        {
            var root = Json.Parse(PointGame.Definitions); root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.layer-access"));
            if (half) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
            var document = Json.Parse(FrameworkProofContent.Read("layer-access.json"));
            var config = document.Get("layer_access_probes")[0]; config.Set("stage", JsonValue.Of(stage)); config.Set("fault", JsonValue.Of(fault));
            if (fault != "none") config.Set("hidden_resource", JsonValue.Null());
            return PointGame.Factory(definitions: root.ToString(), layerDefinitions: FrameworkProofContent.Read("point-layers.json"),
                additionalModule: new LayerAccessProofModule(),
                moduleDeclaration: "{\"id\":\"proof.layer-access\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"points\",\"version\":\"1.0.0\"}],\"documents\":[\"layer-access.json\"]}",
                moduleDocument: document.ToString());
        }
        private static SimGame New(GameFactory factory, string scenario = "points") => factory.NewGame(new NewGameRequest
        { Scenario = scenario, Seed = 1223, Seats = { Controller.Human, Controller.Human } });
        private static void Submit(Check c, SimGame g, int player, Command command)
        {
            ulong before = g.Hash(); c.Equal(Code.None, g.Seat(player).Preview(command), "public layer/worker preview admitted");
            c.Equal(before, g.Hash(), "preview has no policy or spatial mutation");
            int sequence = g.Seat(player).Submit(command); g.Update();
            if (g.Fault != null) throw new InvalidOperationException("layer proof command fault " + g.Fault.Message);
            long cursor = 0; var results = new List<CommandResult>(); g.Seat(player).ReadResults(ref cursor, results);
            c.That(results.Single(x => x.Sequence == sequence).Accepted, "ordinary engine command executes actual layer/worker operation");
        }
        private static void Work(Check c, SimGame g, GameFactory factory)
        {
            var target = Entity(g, "destination");
            Submit(c, g, 0, new PostOrderCommand { Plan = "inspect", Target = target, Priority = 30 });
            var twin = factory.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "physical activity saves before worker completion");
            bool completed = false;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 1000 && watch.ElapsedMilliseconds < 20000; i++)
            {
                if (g.Seat(0).View.Feature<WorkView>().TryReadActivityCompletion("activity:inspect", target, out _)) { completed = true; break; }
                Step(g, twin);
            }
            if (!completed) throw new InvalidOperationException("layer proof exceeds1000Updates/20s clock=" + g.Clock.StepIndex
                + " fault=" + g.Fault + " entities=" + string.Join(";", g.Seat(0).Snapshot.Entities.Select(e => e.Id + "/" + e.State)));
            c.That(((PointSnapshot)g.Seat(0).Snapshot.Entities.Single(x => x.Id == Entity(g, "machine")).Spatial).Position > Fix.Zero,
                "actual machine physically travels before engine completes activity");
        }
        private static void ReadOwned(Check c, SimGame g)
        {
            var mine = g.Seat(0).View.Feature<PointLayerView>(); var other = g.Seat(1).View.Feature<PointLayerView>();
            var values = new List<Fix> { Fix.One }; var first = new PointRegion(Fix.FromInt(3), Fix.One, 1);
            c.Equal(SpatialLayerReadStatus.Success, mine.ReadLayer("point.terrain", first, values), "observed terrain sample readable");
            c.Equal(Fix.FromInt(7), values.Single(), "real engine write appears through authorized sample");
            c.Equal(SpatialLayerReadStatus.Unavailable, other.ReadLayer("point.terrain", first, values), "foreign sample scope refuses same public layer");
            c.Equal(0, values.Count, "foreign sample refusal clears destination");
            c.Equal(SpatialLayerReadStatus.Success, mine.ReadLayer("point.deposit", first, values), "owned private resource sample readable");
            c.Equal(Fix.FromInt(9), values.Single(), "private resource quantity is actual authored engine value");
            c.Equal(SpatialLayerReadStatus.Unavailable, other.ReadLayer("point.deposit", first, values), "private ownership refusal has no sample result");
            c.Equal(0, values.Count, "private sample result cleared");
            c.That(mine.Layers.All(x => x.Id != "point.water") && other.Layers.All(x => x.Id != "point.water"),
                "hidden linked resource hides otherwise-public layer metadata");
            c.That(g.Observer.Feature<PointLayerView>().Layers.Any(x => x.Id == "point.water"), "explicit observer may see hidden linked-resource metadata");
            c.Equal(SpatialLayerReadStatus.Unavailable, mine.ReadLayer("point.water", first, values), "hidden metadata and hidden linked resource use same sample refusal");
            var region = new PointRegion(Fix.FromInt(3), Fix.One, 5);
            var firstPage = mine.ReadLayerPage("point.terrain", region, new SpatialLayerPageRequest(0, 1), values);
            c.Equal(SpatialLayerReadStatus.Success, firstPage.Status, "owned page reads through shared access contract");
            var refused = mine.ReadLayerPage("point.terrain", region, new SpatialLayerPageRequest(4, 1, firstPage.Cursor), values);
            c.Equal(SpatialLayerReadStatus.Unavailable, refused.Status, "continuation outside coverage refuses before value publication");
            c.That(refused.Count == 0 && refused.TotalCount == 0 && refused.Cursor == null && values.Count == 0, "refused page exposes no values/count/cursor");
            c.Equal(SpatialLayerReadStatus.Unavailable, mine.ReadLayer("point.terrain", region, values), "mixed visible/hidden region has no partial value prefix");
            c.Equal(0, values.Count, "mixed refusal clears destination");
            var keys = new List<string> { "retained" }; g.Seat(0).View.Feature<LayerAccessReadModel>().AppendKeys("point.terrain", keys);
            c.That(keys.Count == 3 && keys[0] == "retained" && keys[2].Contains("coverage:0"), "valid keys append atomically to existing caller output");
            var expected = keys.ToArray(); keys.Clear(); keys.Add("retained"); g.Seat(0).View.Feature<LayerAccessReadModel>().AppendKeys("point.terrain", keys);
            c.That(expected.SequenceEqual(keys), "pure authored observation identities stable");
            keys.Clear(); keys.Add("retained"); g.Seat(1).View.Feature<LayerAccessReadModel>().AppendKeys("point.deposit", keys);
            c.That(keys.SequenceEqual(new[] { "retained" }), "private key refusal preserves caller prefix without disclosing layer identity");
            c.That(!g.Seat(0).View.Feature<LayerAccessReadModel>().ObserveUnowned("point.terrain"), "unowned runtime layer refuses without calling hostile CanRead");
        }
        private static void Healthy(Check c, bool half)
        {
            var f = Factory(half); var g = New(f); var idle = New(f); ulong idleHash = idle.Hash();
            var limits = new ReplayLimits(4 * 1024 * 1024, 6000); using var recording = g.RecordReplay(limits);
            Work(c, g, f);
            Submit(c, g, 0, new AssignPointLayerCommand { Layer = "point.deposit", Owner = new PlayerId(0) });
            Submit(c, g, 0, new WritePointLayerCommand { Layer = "point.terrain", Position = Fix.FromInt(3), Value = Fix.FromInt(7) });
            Submit(c, g, 0, new WritePointLayerCommand { Layer = "point.deposit", Position = Fix.FromInt(3), Value = Fix.FromInt(9) });
            int mine = g.Seat(0).View.Feature<PointLayerView>().Version, foreign = g.Seat(1).View.Feature<PointLayerView>().Version;
            Submit(c, g, 1, new WritePointLayerCommand { Layer = "point.terrain", Position = Fix.FromInt(7), Value = Fix.FromInt(11) });
            c.Equal(mine, g.Seat(0).View.Feature<PointLayerView>().Version, "hidden foreign sample mutation does not disclose through own revision");
            c.That(g.Seat(1).View.Feature<PointLayerView>().Version > foreign, "actual foreign observer revision advances for own sample");
            ulong before = g.Hash(); long time = g.Clock.StepIndex; ReadOwned(c, g);
            c.Equal(before, g.Hash(), "metadata/samples/pages/keys never mutate actual state"); c.Equal(time, g.Clock.StepIndex, "visibility reads never drive time");
            var heldLayers = g.Seat(0).View.Feature<PointLayerView>().Layers;
            var heldDeposit = heldLayers.Single(x => x.Id == "point.deposit");
            var heldTerrain = heldLayers.Single(x => x.Id == "point.terrain");
            c.That(!(heldDeposit.Resource is IContentDefinition) && !(heldDeposit.Unit is IContentDefinition)
                && !(heldDeposit.VerticalUnit is IContentDefinition), "retained metadata exposes detached resource/unit facts, never live content capabilities");
            c.Throws<InvalidOperationException>(() => heldTerrain.Metadata.Get("generator").Set("id", JsonValue.Of("client.changed")),
                "retained nested layer metadata is immutable");
            c.Equal(before, g.Hash(), "refused metadata mutation leaves authoritative engine unchanged");
            Submit(c, g, 0, new AssignPointLayerCommand { Layer = "point.deposit", Owner = new PlayerId(1) });
            c.That(g.Seat(0).View.Feature<PointLayerView>().Layers.All(x => x.Id != "point.deposit"), "former owner loses current layer metadata access");
            c.Equal(new PlayerId(1), g.Seat(1).View.Feature<PointLayerView>().Layers.Single(x => x.Id == "point.deposit").Owner,
                "new owner sees actual current ownership");
            var refusedValues = new List<Fix> { Fix.One };
            c.Equal(SpatialLayerReadStatus.Unavailable, g.Seat(0).View.Feature<PointLayerView>().ReadLayer("point.deposit",
                new PointRegion(Fix.FromInt(3), Fix.One, 1), refusedValues), "held historical metadata cannot authorize current samples");
            c.Equal(0, refusedValues.Count, "ownership loss publishes no sample prefix");
            c.Equal(new PlayerId(0), heldDeposit.Owner, "retained layer owner remains historical after actual reassignment");
            c.Equal("supply", heldDeposit.Resource.Id, "retained resource identity remains detached");
            c.Equal("point.authored", heldTerrain.Metadata.Get("generator").Get("id").Text, "retained nested metadata preserves authored value");
            Submit(c, g, 1, new AssignPointLayerCommand { Layer = "point.deposit", Owner = new PlayerId(0) });
            var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "same visibility owner and original point samples restore");
            ReadOwned(c, twin); for (int i = 0; i < 4; i++) Step(g, twin);
            c.Equal(idleHash, idle.Hash(), "interleaved policy sessions remain isolated");
            c.Equal(7, recording.CommandCount, "recording includes physical work and every actual layer command from both seats");
            c.Equal(g.Clock.StepIndex - recording.InitialStep, (long)recording.CheckpointCount,
                "recording includes every committed update through saved layer continuation");
            var verified = new ReplayVerifier().Verify(f, recording.Finish(), limits);
            c.That(verified.Success && verified.ActualHash == g.Hash(),
                "physical worker and scoped layer operations replay to final live state, not only a valid prefix");
            Console.WriteLine("layer access replay: half=" + half + ";initial=" + recording.InitialStep + ";final=" + g.Clock.StepIndex
                + ";commands=" + recording.CommandCount + ";checkpoints=" + recording.CheckpointCount
                + ";expected-hash=" + g.Hash().ToString("X16") + ";actual-hash=" + verified.ActualHash.ToString("X16"));
        }
        private static void Refuse(Check c, string stage, string mode, int viewer = 0)
        {
            var f = Factory(stage: stage, fault: mode); var g = New(f); var idle = New(f); ulong idleHash = idle.Hash();
            Work(c, g, f); var before = g.Seat(0).Snapshot; var foreign = g.Seat(1).Snapshot;
            g.Clock.Pause(); var values = new List<Fix> { Fix.One }; var keys = new List<string> { "retained" };
            Action read = stage == "keys" ? () => g.Seat(viewer).View.Feature<LayerAccessReadModel>().AppendKeys("point.water", keys)
                : () => g.Seat(viewer).View.Feature<PointLayerView>().ReadLayer("point.water",
                    new PointRegion(Fix.FromInt(viewer == 0 ? 3 : 7), Fix.One, 1), values);
            c.Throws<InvalidOperationException>(read, "unguarded extension callback refuses through original shared engine access");
            var fault = g.Fault; c.That(fault != null, "extension access failure records first engine fault");
            if (fault == null) throw new InvalidOperationException("missing layer access fault " + stage + "/" + mode);
            if (mode == "throw") c.That(fault.Message.Contains("proof.layer-access-original"), "original callback exception retained");
            if (mode == "invalid-key") c.That(fault.Message.Contains("invalid observation identity"), "all key identities admitted inside guard");
            if (stage == "keys") c.That(keys.SequenceEqual(new[] { "retained" }), "failed key callback exposes no layer/sample prefix");
            else c.Equal(0, values.Count, "failed access exposes no sample prefix");
            g.Update();
            for (int seat = 0; seat < 2; seat++)
            {
                var old = seat == 0 ? before : foreign; var current = g.Seat(seat).Snapshot;
                c.Equal(GameState.Faulted, current.State, "retained private publication marks faulted state");
                c.Equal(old.Tick, current.Tick, "retained viewer time remains last healthy epoch");
                c.That(ReferenceEquals(old.Space, current.Space), "viewer retains own last healthy spatial projection");
                c.Equal(0, current.Changed.Count + current.Spawned.Count + current.Removed.Count, "callback fault invents no private deltas");
                c.That(old.Entities.Select(x => x.Id).SequenceEqual(current.Entities.Select(x => x.Id)), "no false private entity prefix");
            }
            long stopped = g.Clock.StepIndex; g.Update(); c.Equal(stopped, g.Clock.StepIndex, "faulted engine cannot advance sole clock");
            c.Throws<InvalidOperationException>(() => g.Hash(), "faulted access session cannot hash healthy");
            c.Throws<InvalidOperationException>(() => g.Save(), "faulted access session cannot save healthy");
            c.Throws<InvalidOperationException>(read, "faulted live query refuses instead of returning missing facts");
            c.That(ReferenceEquals(fault, g.Fault), "first visibility callback fault remains authoritative"); c.Equal(idleHash, idle.Hash(), "faulted extension does not affect another session");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current engine layer observation identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "current authored protocol, no historical layer save pin");
            Healthy(c, false); Healthy(c, true);
            var native = New(SpatialLayerTests.Factory(), "layers"); var map = native.Seat(0).View.Feature<TileMapView>();
            var values = new List<Fix>(); c.Equal(SpatialLayerReadStatus.Success, map.ReadLayer("elevation", new TileRect(0, 0, 1, 1), values), "native layer uses same actual shared admission");
            c.Equal(Fix.Zero, values.Single(), "native empty authored cell stays canonical zero");
            foreach (string stage in new[] { "content-layer", "content-unit", "content-resource", "content-vertical", "sample", "keys" })
                foreach (string mode in new[] { "throw", "state", "clock", "space" }) Refuse(c, stage, mode);
            Refuse(c, "keys", "invalid-key"); Refuse(c, "keys", "state", 1); Refuse(c, "sample", "space", 1);
        }
    }
}
