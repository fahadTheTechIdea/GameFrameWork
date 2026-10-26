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

namespace Beep.SimEngine.Tests
{
    internal static class SensorEntityObservationTests
    {
        internal const string Case = "Sensor entity observation: physical non-tile sight guards complete provider state and original errors";
        internal const string TargetCase = "Sensor targets: invalid non-tile captures preserve first errors and full physical replay";
        internal static void Register()
        {
            TestRunner.Add(Case, c => Run(c));
            TestRunner.Add(TargetCase, c => Run(c, targetCaptures: true));
        }
        private static GameFactory Factory()
        {
            var document = Json.Parse(FrameworkProofContent.Read("point-sensor.json"));
            var parts = document.Get("entities")[0].Get("components");
            parts.Set("sensor_sibling_probe", Json.Parse("{\"id\":\"sensor.observation-probe\"}"));
            document.Get("entities")[1].Get("components").Set("sensor_sibling_probe", Json.Parse("{\"id\":\"sensor.observation-probe\"}"));
            parts.Get("point_sensor").Get("operation").Set("target", JsonValue.Number("20"));
            var probe = new SensorObservationProofModule();
            return PointSensorTests.Factory(document.ToString(), additionalModules: new[] { probe },
                additionalDeclarations: new[] { Json.Parse("{\"id\":\"proof.sensor-observation\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"proof.point-sensors\",\"version\":\"1.0.0\"}],\"documents\":[]}") },
                definitionEdit: root => {
                    root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(probe.Id));
                    // A mobile sensor occupies its point. Delivery must stop next to it, not overlap it.
                    root.Get("point_spaces")[0].Set("interaction_reach", JsonValue.Number("1"));
                });
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest {
            Scenario = "points", Seed = 734, Seats = { Controller.Human, Controller.Human } });
        private static Entity Find(SimGame game, string id) => game.World.Entities.All.Single(e => e.Def.Id == id);
        private static void Step(SimGame game)
        { game.Update(); if (game.Fault != null) throw new InvalidOperationException("sensor proof update fault: " + game.Fault); }
        private static void Run(Check c, bool targetCaptures = false)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current engine identity");
            var factory = Factory(); var game = New(factory); var isolated = New(factory);
            var limits = new ReplayLimits(4 * 1024 * 1024, 2000);
            using var recording = game.RecordReplay(limits);
            var untouched = isolated.Hash(); var machine = Find(game, "sensor.machine"); var contact = Find(game, "sensor.contact").Id;
            var supply = game.World.Content.ResourceId("supply");
            var stock = machine.Get<IInventory>();
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "physical sensor needs no business module");
            c.That(!game.Seat(0).View.TryGetEntity(contact, out _), "unsupplied sensor cannot reveal foreign contact");
            var route = new AddRouteCommand { From = Find(game, "source").Id, To = machine.Id, Resource = supply,
                Settings = new LogisticsRouteSettings(true, Fix.One, 70) };
            c.Equal(Code.None, game.Seat(0).Preview(route), "normal physical supply preview");
            var answer = TestKit.Order(game, 0, route); c.That(answer.Accepted, "normal physical supply command");
            c.Equal(Fix.Zero, stock.Amount(supply), "posting route is not delivery");
            int steps = 0;
            while (stock.Amount(supply) != Fix.One && steps++ < 1200) Step(game);
            if (stock.Amount(supply) != Fix.One)
            {
                var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                throw new InvalidOperationException("sensor physical supply stalled at " + game.Clock.StepIndex + " stock=" + stock.Amount(supply) + " jobs=" +
                    string.Join(";", jobs.Select(j => j.Id + "/" + j.State + "/" + j.Blocker)) + " entities=" +
                    string.Join(";", game.World.Entities.All.Select(e => e.Def.Id + "/" + e.State + "/" + (e.Get<IMotion>()?.Status.ToString() ?? "-") +
                        "/" + ((e.Get<ISpatialPresence>()?.Capture() as PointSnapshot)?.Position.ToString() ?? "-"))));
            }
            c.That(steps > 0, "actual movement/loading/delivery took time");
            c.That(TestKit.Order(game, 0, new RemoveRouteCommand { Route = checked((int)answer.Arg0) }).Accepted, "ordinary route removal");
            c.That(game.Seat(0).View.TryGetEntity(contact, out _), "actual delivery opens foreign entity sight");
            var values = new List<Fix>(); var region = new PointRegion(Fix.FromInt(10), Fix.One, 1);
            c.Equal(SpatialLayerReadStatus.Success, game.Seat(0).View.Feature<PointLayerView>().ReadLayer("point.terrain", region, values), "same supplied sensor opens layer samples");
            var stable = game.Hash(); var bytes = game.Save(); var twin = factory.Load(bytes);
            c.Equal(stable, twin.Hash(), "complete supplied entity/sibling state saves identically");
            for (int i = 0; i < 8; i++) { Step(game); Step(twin); c.Equal(game.Hash(), twin.Hash(), "interleaved saved physical continuation"); }
            c.Equal(untouched, isolated.Hash(), "unrelated session borrows no stock or sight");
            c.That(!isolated.Seat(0).View.TryGetEntity(Find(isolated, "sensor.contact").Id, out _), "unrelated session stays unsupplied");
            c.Equal(Fix.One, stock.Amount(supply), "observation consumes or fabricates no supplied material");
            var replay = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
            c.That(replay.Success && replay.ActualHash == game.Hash(), "complete initial physical sensor supply and saved continuation replay to final hash");
            Console.WriteLine("sensor target physical replay: initial=" + recording.InitialStep + ";final=" + game.Tick
                + ";commands=" + recording.CommandCount + ";checkpoints=" + recording.CheckpointCount
                + ";expected-hash=" + game.Hash().ToString("X16") + ";actual-hash=" + replay.ActualHash.ToString("X16"));

            foreach (var stage in new[] { SensorObservationStage.Coverage, SensorObservationStage.Sight })
                foreach (var fault in new[] { SensorObservationFault.Sibling, SensorObservationFault.Throw, SensorObservationFault.Clock, SensorObservationFault.SiblingThrow })
                    foreach (string path in new[] { "entity", "sample", "keys" })
                    {
                        if (stage == SensorObservationStage.Sight && path == "keys") continue;
                        Fault(c, factory, bytes, stage, fault, path);
                    }
            if (targetCaptures)
                foreach (var saved in new[] { bytes, game.Save() })
                    foreach (SensorTargetCaptureFault fault in Enum.GetValues(typeof(SensorTargetCaptureFault)))
                        foreach (string path in new[] { "lookup", "count", "list", "inspect", "rule" })
                            TargetCapture(c, factory, saved, isolated, untouched, fault, path);
        }
        private static void TargetCapture(Check c, GameFactory factory, byte[] saved, SimGame isolated, ulong untouched,
            SensorTargetCaptureFault fault, string path)
        {
            var game = factory.Load(saved); var target = Find(game, "sensor.contact");
            var presence = (PointPresence)target.Get<ISpatialPresence>();
            var view = game.Seat(0).View;
            c.That(view.TryGetEntity(target.Id, out var retained), "healthy supplied saved sensor reveals actual target before negative injection");
            var rows = new List<EntityView> { retained }; var facets = new List<Facet>();
            long at = game.Tick; int version = target.Version;
            // Negative protocol diagnostic: arming is outside the persisted provider codec and never normal gameplay.
            presence.ArmVisibilityCapture(fault);
            string label = game.Tick + "/" + fault + "/" + path;
            Exception caught = null;
            try
            {
                if (path == "lookup") view.TryGetEntity(target.Id, out _);
                else if (path == "count") _ = view.EntityCount;
                else if (path == "list") view.CopyEntities(rows);
                else if (path == "inspect") view.Inspect(target.Id, facets);
                else game.World.Content.Get<IVisibilityEntityTarget>("sensor.contact.sight").AllowsEntity(game.World, new PlayerId(0), target);
            }
            catch (Exception error) { caught = error; }
            c.That(caught != null, label + " invalid or impure target capture cannot become an ordinary invisible result");
            c.That(game.Fault != null, label + " actual target capture violation latches the original session fault");
            c.Equal(1, presence.VisibilityCaptureCalls, label + " target callback runs once, not again for projection or recovery");
            if (caught == null || game.Fault == null) return; // Baseline records each missing boundary without inventing terminal facts.
            if (fault == SensorTargetCaptureFault.Throw || fault == SensorTargetCaptureFault.TouchThrow || fault == SensorTargetCaptureFault.SiblingThrow)
                c.That(caught.ToString().Contains("proof.sensor-target-capture-first-error") && game.Fault.ToString().Contains("proof.sensor-target-capture-first-error"),
                    label + " first callback error survives mutation verification");
            if (fault == SensorTargetCaptureFault.Sibling || fault == SensorTargetCaptureFault.SiblingThrow)
                c.Equal(version, target.Version, label + " private sibling probe does not rely on Entity.Touch");
            if (path == "list") c.Equal(0, rows.Count, label + " failed visibility publishes no owned or private entity prefix");
            if (path == "inspect") c.Equal(0, facets.Count, label + " failed visibility publishes no inspect prefix");
            var first = game.Fault;
            c.Throws<InvalidOperationException>(() => game.Save(), label + " terminal target cannot checkpoint");
            c.Throws<InvalidOperationException>(() => game.Hash(), label + " terminal target cannot publish a healthy hash");
            c.Throws<InvalidOperationException>(() => view.TryGetEntity(target.Id, out _), label + " retained query stops before repeating capture");
            c.Equal(0, game.Update(), label + " terminal target cannot advance");
            c.That(ReferenceEquals(first, game.Fault) && game.Tick == at && presence.VisibilityCaptureCalls == 1,
                label + " original fault and sole game clock stay authoritative");
            c.Equal(target.Id, retained.Id, label + " earlier detached entity snapshot remains stable");
            c.Equal(untouched, isolated.Hash(), label + " target fault cannot leak into interleaved session");
        }
        private static void Fault(Check c, GameFactory factory, byte[] bytes, SensorObservationStage stage, SensorObservationFault fault, string path)
        {
            var game = factory.Load(bytes); var machine = Find(game, "sensor.machine");
            var sensor = (PointSensor)machine.Get<ISpatialSensor>(new Code("sensor.sight"));
            var contact = Find(game, "sensor.contact").Id; int revision = machine.Version;
            sensor.ArmObservation(stage, fault); string label = stage + "/" + fault + "/" + path;
            Exception caught = null; var keys = new List<string>(); var samples = new List<Fix>();
            try
            {
                if (path == "entity") game.Seat(0).View.TryGetEntity(contact, out _);
                else if (path == "sample") game.Seat(0).View.Feature<PointLayerView>().ReadLayer("point.terrain", new PointRegion(Fix.FromInt(10), Fix.One, 1), samples);
                else SpatialLayerAccess.CopyObservationKeys(game.World, ((ISpatialLayerProvider)game.World.Space).SpatialLayers.Single(l => l.Id == "point.terrain"), new PlayerId(0), keys);
            }
            catch (Exception error) { caught = error; }
            c.That(caught != null, label + " rejects sensor side effects before returning visibility");
            if (caught == null) throw new InvalidOperationException("sensor observer returned mutable facts: " + label);
            c.That(game.Fault != null && sensor.ObservationCalls == 1, label + " latches first fault without repeated callback");
            c.Equal(revision, machine.Version, label + " private probe never relies on entity revision changes");
            if (fault == SensorObservationFault.Throw || fault == SensorObservationFault.SiblingThrow)
                c.That(caught.ToString().Contains("proof.sensor-first-error") && game.Fault.ToString().Contains("proof.sensor-first-error"), label + " original provider error remains authoritative");
            c.Equal(0, samples.Count + keys.Count, label + " failed observation publishes no sample/key prefix");
            c.Throws<InvalidOperationException>(() => game.Save(), label + " faulted state is not resumable");
            c.Throws<InvalidOperationException>(() => game.Seat(0).View.TryGetEntity(contact, out _), label + " subsequent query rejects terminal session");
        }
    }
}
