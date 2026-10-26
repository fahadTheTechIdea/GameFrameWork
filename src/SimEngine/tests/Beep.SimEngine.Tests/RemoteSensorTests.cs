using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class RemoteSensorTests
    {
        internal const string Case = "Remote utility sensors: non-tile physical supply saved bindings and private live gates";
        private static readonly Code Remote = new Code("sensor.remote");
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(string document = null, Action<JsonValue> edit = null, bool independent = false, bool lifecycle = false, bool production = false, bool recordFlows = false)
        {
            var sensor = Json.Parse(FrameworkProofContent.Read("point-sensor.json"));
            var parts = sensor.Get("entities")[0].Get("components");
            parts.Get("point_presence").Set("position", JsonValue.Number("80"));
            sensor.Get("entity_setups")[0].Get("placement").Set("position", JsonValue.Number("80"));
            parts.Set("remote_utility_signal", Json.Parse("{\"id\":\"sensor.remote\",\"providers\":[\"utility.remote-provider\",\"utility.remote-alternative\"],\"source\":\"utility\",\"target\":null}"));
            edit?.Invoke(parts.Get("remote_utility_signal"));
            parts.Get("point_sensor").Set("conditions", Json.Parse("[{\"id\":\"remote.ready\",\"source\":\"sensor.remote\",\"signal\":\"sensor.utility.ready\",\"expected\":true,\"blocker\":\"remote.utility-shortage\"}]"));
            sensor.Get("world_setup_profiles")[0].Get("includes").Add(JsonValue.Of("remote.utility.start"));
            var modules = new List<GameModule> { new UtilitySignalsModule(), new UtilityConsumerProofModule(), new RemoteSignalsModule() };
            var declarations = new List<JsonValue> {
                    Json.Parse("{\"id\":\"sim.utility-signals\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[]}"),
                    Json.Parse("{\"id\":\"proof.utility-consumer\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.utility-signals\",\"version\":\"1.0.0\"}],\"documents\":[]}"),
                    Json.Parse("{\"id\":\"sim.remote-signals\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[\"point-remote-utility.json\"]}") };
            var authored = Json.Parse(document ?? FrameworkProofContent.Read("point-remote-utility.json"));
            var documents = new Dictionary<string,string>();
            if (production)
            {
                parts.Get("remote_utility_signal").Set("providers", Json.Parse("[\"remote.production-provider\"]"));
                sensor.Get("world_setup_profiles")[0].Set("includes", Json.Parse("[\"remote.production.start\"]"));
                declarations[2].Get("documents").Add(JsonValue.Of("remote-signal-production.json"));
                documents.Add("remote-signal-production.json", FrameworkProofContent.Read("remote-signal-production.json"));
            }
            if (lifecycle)
            {
                declarations[2].Get("documents").Add(JsonValue.Of("remote-signal-lifecycle.json"));
                documents.Add("remote-signal-lifecycle.json", FrameworkProofContent.Read("remote-signal-lifecycle.json"));
            }
            if (independent)
            {
                var binding = parts.Get("remote_utility_signal"); parts.Remove("remote_utility_signal"); parts.Set("proof_remote_utility_signal", binding);
                modules.Add(new IndependentRemoteSignalsModule());
                declarations.Add(Json.Parse("{\"id\":\"proof.remote-utility\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.remote-signals\",\"version\":\"1.0.0\"}],\"documents\":[]}"));
                foreach (string section in new[] { "signals", "resource_signals" })
                    for (int i = 0; i < authored.Get(section).Count; i++) authored.Get(section)[i].Get("providers").Add(Json.Parse("{\"component\":\"proof_remote_utility_signal\",\"schema\":1}"));
            }
            documents.Add("point-remote-utility.json", authored.ToString());
            return PointSensorTests.Factory(sensor.ToString(), additionalModules: modules, additionalDeclarations: declarations,
                extraDocuments: documents,
                definitionEdit: root => {
                    foreach (var module in modules) root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(module.Id));
                    if (production || recordFlows) root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(ResourceFlowsModule.ModuleId));
                });
        }
        private static EntityId Find(SimGame game, string definition)
        { var entities = new List<EntityView>(); game.Seat(0).View.CopyEntities(entities); return entities.Single(e => e.Def == definition).Id; }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("remote sensor update fault: " + game.Fault + "/" + twin?.Fault);
        }
        private static AutomationSignalSnapshot Read(SimGame game, EntityId sensor)
        {
            Code refusal = game.Seat(0).View.Feature<OperationsView>().ReadSignal(sensor, Remote, "sensor.utility.ready", out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("remote signal query: " + refusal); return value;
        }
        private static void Run(Check c)
        {
            foreach (bool independent in new[] { false, true }) RunVariant(c, independent);
        }
        private static void RunVariant(Check c, bool independent)
        {
            var factory = Factory(independent: independent); var game = factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 734, Seats = { Controller.Human, Controller.Human } });
            var sensor = Find(game, "sensor.machine"); var provider = Find(game, "utility.remote-provider");
            var remote = game.Seat(0).View.Feature<RemoteSignalsView>();
            c.That(game.Modules.Contains(RemoteSignalsModule.ModuleId), "explicit master selects optional remote owner");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "no business module needed");
            c.Equal(RemoteSignalCodes.Unbound, Read(game, sensor).Blocker, "unbound is explicit unavailable, no guessed provider");
            c.Equal(Code.None, remote.ReadBinding(sensor, Remote, out var unbound), "own immutable binding readable");
            c.Equal(EntityId.None, unbound.Provider, "no startup identity fabricated");
            ulong before = game.Hash();
            var wrong = new BindRemoteSignalCommand { Entity = sensor, SignalSource = Remote, Provider = Find(game, "source") };
            c.Equal(RemoteSignalCodes.Incompatible, game.Seat(0).Preview(wrong), "wrong prototype refused");
            c.Equal(Codes.NotFound, game.Seat(1).Preview(new BindRemoteSignalCommand { Entity = sensor, SignalSource = Remote, Provider = provider }), "foreign binding cannot be controlled");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<RemoteSignalsView>().ReadBinding(sensor, Remote, out var withheld), "foreign binding facts withheld");
            c.Equal(EntityId.None, withheld.Provider, "no foreign reference leaked");
            c.Equal(before, game.Hash(), "preview/private reads do not change state");
            var limits = new ReplayLimits(4 * 1024 * 1024, 2000); var recording = game.RecordReplay(limits);
            game.Seat(0).Submit(new BindRemoteSignalCommand { Entity = sensor, SignalSource = Remote, Provider = provider });
            var twin = factory.Load(game.Save()); Step(game, twin);
            c.Equal(game.Hash(), twin.Hash(), "pending ordinary link command restores");
            c.That(Read(game, sensor).Available && Read(game, sensor).Value == Fix.Zero, "bound actual utility shortage is false, not fabricated power");
            var operations = game.Seat(0).View.Feature<OperationsView>();
            c.Equal(Code.None, operations.ReadSignal(sensor, Remote, "remote.requested", out var requested), "same typed quantity signal forwarded");
            c.That(requested.Available && requested.Value == Fix.One && requested.Unit == new Code("unit.supply"), "native utility demand retains exact resource/unit");
            c.Equal(Code.None, operations.ReadSignal(sensor, Remote, "remote.available", out var unavailable), "actual empty contribution forwarded");
            c.That(unavailable.Available && unavailable.Value == Fix.Zero, "remote quantity does not fabricate stock");
            var origin = new PointRegion(Fix.FromInt(80), Fix.One, 1); var samples = new List<Fix>();
            c.Equal(SpatialLayerReadStatus.Unavailable, game.Seat(0).View.Feature<PointLayerView>().ReadLayer("point.terrain", origin, samples), "remote shortage closes current point sight");
            game.Seat(0).View.TryFindResource("supply", out var supply);
            var route = new Beep.SimEngine.Commands.AddRouteCommand { From = Find(game, "source"), To = provider, Resource = supply,
                Settings = new LogisticsRouteSettings(true, Fix.One, 70) };
            c.Equal(Code.None, game.Seat(0).Preview(route), "actual remote utility provider can receive ordinary physical supply");
            game.Seat(0).Submit(route); twin = factory.Load(game.Save()); Step(game, twin);
            c.That(Read(game, sensor).Value == Fix.Zero, "route admission does not instantly deliver supply");
            int steps = 0;
            while (Read(game, sensor).Value != Fix.One && steps++ < 400) { Step(game, twin); if (steps % 12 == 0) c.Equal(game.Hash(), twin.Hash(), "physical remote supply saved checkpoint"); }
            c.That(steps <= 400 && Read(game, sensor).Value == Fix.One, "physical delivery changes original remote utility fact");
            if (Read(game, sensor).Value != Fix.One)
            {
                var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                throw new InvalidOperationException("remote supply stalled at" + game.Tick + ":" + string.Join(";", jobs.Select(j => j.Id + "/" + j.State + "/" + j.Blocker)));
            }
            c.Equal(game.Hash(), twin.Hash(), "saved physical delivery identical");
            c.Equal(Code.None, operations.ReadSignal(sensor, Remote, "remote.available", out var available), "physically changed native contribution forwarded");
            c.That(available.Available && available.Value == Fix.One, "exact native contribution after actual delivery");
            c.Equal(SpatialLayerReadStatus.Success, game.Seat(0).View.Feature<PointLayerView>().ReadLayer("point.terrain", origin, samples), "actual remote utility readiness opens point sensor gate");
            c.Equal(Code.None, remote.ReadBinding(sensor, Remote, out var bound), "bound immutable facts available");
            c.Equal(provider, bound.Provider, "binding keeps selected provider, not another entity");
            var retained = bound;
            var alternative = Find(game, "utility.remote-alternative");
            var switchProvider = new BindRemoteSignalCommand { Entity = sensor, SignalSource = Remote, Provider = alternative };
            c.Equal(Code.None, game.Seat(0).Preview(switchProvider), "another explicitly compatible prototype admitted");
            game.Seat(0).Submit(switchProvider); twin = factory.Load(game.Save()); Step(game, twin);
            c.Equal(game.Hash(), twin.Hash(), "alternative provider selection survives pending-command save");
            c.That(Read(game, sensor).Available && Read(game, sensor).Value == Fix.Zero, "selected empty alternative never falls back to supplied first provider");
            c.Equal(Code.None, remote.ReadBinding(sensor, Remote, out var alternativeBinding), "alternative binding facts available");
            c.Equal(alternative, alternativeBinding.Provider, "sole reference owner retains exact chosen alternative");
            game.Seat(0).Submit(new BindRemoteSignalCommand { Entity = sensor, SignalSource = Remote, Provider = EntityId.None });
            twin = factory.Load(game.Save()); Step(game, twin);
            c.Equal(game.Hash(), twin.Hash(), "unbind command restored through same codec");
            c.Equal(RemoteSignalCodes.Unbound, Read(game, sensor).Blocker, "unbinding withholds live utility instead of cached ready value");
            c.Equal(provider, retained.Provider, "retained binding is detached");
            c.Equal(SpatialLayerReadStatus.Unavailable, game.Seat(0).View.Feature<PointLayerView>().ReadLayer("point.terrain", origin, samples), "unbind immediately revokes current sight");
            c.That(game.Fault == null, "entire remote workflow healthy");
            c.That(new ReplayVerifier().Verify(factory, recording.Finish(), limits).Success, "ordinary remote commands and physical supply replay identically");
            var isolated = factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 734, Seats = { Controller.Human, Controller.Human } });
            c.Equal(RemoteSignalCodes.Unbound, Read(isolated, Find(isolated, "sensor.machine")).Blocker, "interleaved session borrows no remote binding/ready state");
            foreach (string invalid in new[] { "missing-provider", "missing-source", "live-target", "empty-providers", "duplicate-providers", "wrong-metrics", "missing-providers" })
                c.Throws<ContentException>(() => Factory(edit: row => {
                    if (invalid == "missing-provider") row.Set("providers", Json.Parse("[\"missing\"]"));
                    else if (invalid == "missing-source") row.Set("source", JsonValue.Of("missing"));
                    else if (invalid == "empty-providers") row.Set("providers", Json.Parse("[]"));
                    else if (invalid == "duplicate-providers") row.Set("providers", Json.Parse("[\"utility.remote-provider\",\"utility.remote-provider\"]"));
                    else if (invalid == "wrong-metrics") row.Set("providers", Json.Parse("[\"sensor.machine\"]"));
                    else if (invalid == "missing-providers") row.Remove("providers");
                    else row.Set("target", JsonValue.Number("1"));
                }, independent: independent).NewGame(new NewGameRequest { Scenario = "points", Seed = 734, Seats = { Controller.Human, Controller.Human } }), "invalid remote authoring rejected before play: " + invalid);
        }
    }
}
