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
    // Master JSON, ordinary player actions and immutable observations; no connection/transfer decisions.
    internal static class NetworkPortsBindingTests
    {
        internal const string Case = "Network ports: independent JSON binds original timed push-pull, gates and physical hauling";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Create(InitializeProbe initialize, out NetworkPortsBindingsModule proof,
            string mode = "reusable", Action<JsonValue> edit = null)
        {
            proof = new NetworkPortsBindingsModule();
            var document = Json.Parse(FrameworkProofContent.Read("network-ports-bindings.json"));
            if (mode != "reusable")
                for (int i = 0; i < document.Get("entities").Count; i++)
                {
                    var parts = document.Get("entities")[i].Get("components"); var binding = parts.Get("independent_network_ports");
                    parts.Remove("independent_network_ports");
                    if (mode == "native") { binding.Remove("metadata_fault"); parts.Set("ports", binding); }
                    else parts.Set("independent_custom_ports", binding);
                }
            edit?.Invoke(document);
            return NetworkSignalBindingTests.Create(initialize, out _, edit: root => root.Set("entities", JsonValue.NewArray()),
                extraModules: new GameModule[] { proof },
                extraDeclarations: "[{\"id\":\"proof.network-ports-bindings\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.infrastructure\",\"version\":\"1.0.0\"}],\"documents\":[\"network-ports-bindings.json\"]}]",
                extraDocuments: new Dictionary<string, string> { { "network-ports-bindings.json", document.ToString() } });
        }
        private static SimGame New(GameFactory factory)
        { var request = new NewGameRequest { Scenario = "colony", Seed = 969 }; request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request); }
        private static Fix Stock(SimGame game, string id)
        { var rows = new List<Facet>(); game.Seat(0).View.Inspect(Entity(game, id), rows); return rows.Where(f => f.Key == Storage.FacetStock && f.Subject == "steel").Aggregate(Fix.Zero, (sum, row) => sum + row.Value); }
        private static Fix Pool(SimGame game)
        {
            var refusal = game.Seat(0).View.Feature<OperationsView>().ReadSignal(Entity(game, "binding.watcher"), new Code("pool"), "binding.stored", out var value);
            if (!refusal.IsNone || !value.Available) throw new InvalidOperationException("pool " + refusal + "/" + value.Blocker);
            return value.Value.Value;
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current ports binding identity"); c.Equal(154, EngineInfo.SaveFormat, "same original empty ports/stock/network/work codecs");
            c.That(typeof(IndependentNetworkPortsDef).BaseType == typeof(ComponentDef<Ports>), "independent authoring binds original runtime without native base");
            c.That(typeof(IndependentCustomPortsDef).BaseType == typeof(ComponentDef<IndependentCustomPorts>), "custom provider needs no native metadata base");
            long native = Physical(c, "native"); c.Equal(native, Physical(c, "reusable"), "reusable original runtime physical parity");
            c.Equal(native, Physical(c, "custom"), "custom contract provider physical parity");
            c.Equal(Pull(c, "native"), Pull(c, "reusable"), "same timed source/pool/destination chronology");
            Invalid(c); Faults(c);
        }
        private static long Physical(Check c, string mode)
        {
            var initialize = new InitializeProbe(); var factory = Create(initialize, out var proof, mode);
            var game = New(factory); var isolated = New(factory); ulong untouched = isolated.Hash();
            c.Equal(mode == "reusable", proof.ActualBinding, "actual metadata/original runtime/network aliases");
            c.Equal(mode == "custom", proof.CustomBinding, "actual custom runtime contract");
            c.Equal(2, initialize.Initializations, "one Initialize per session"); c.That(!game.Modules.Contains(TycoonModule.ModuleId), "networks need no business module");
            c.Equal(Fix.Zero, Pool(game), "authored startup corridor grants no stock");
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000); var recorder = game.RecordReplay(limits); var twin = factory.Load(game.Save());
            Step(game, twin); c.Equal(Fix.FromInt(26), Stock(game, "binding.source"), "first minute physically withdraws four"); c.Equal(Fix.FromInt(4), Pool(game), "first earned pool injection");
            Step(game, twin); c.Equal(Fix.FromInt(22), Stock(game, "binding.source"), "second real withdrawal"); c.Equal(Fix.FromInt(8), Pool(game), "backpressure reached");
            var view = game.Seat(0).View.Feature<InfrastructureView>();
            c.Equal(Code.None, view.ReadGates(Entity(game, "binding.source"), "binding.main", out var gate), "gate uses actual original connection provider");
            c.Equal(new Code("binding.backpressure"), gate.Blocker, "same typed reserve/backpressure facts");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<InfrastructureView>().ReadGates(Entity(game, "binding.source"), "binding.main", out _), "foreign company cannot read gate facts");
            c.Equal(Codes.NotOwned, game.Seat(1).View.Feature<OperationsView>().ReadSignal(Entity(game, "binding.source"), new Code("pool"), "binding.stored", out var foreign), "foreign connection quantity refusal");
            c.That(!foreign.Available, "no private value on refusal");
            ulong hash = game.Hash(); Pool(game); view.ReadGates(Entity(game, "binding.source"), "binding.main", out _); c.Equal(hash, game.Hash(), "actual connection facts pure");
            twin = factory.Load(game.Save()); for (int i = 0; i < 4; i++) Step(game, twin); c.Equal(Fix.FromInt(8), Pool(game), "blocked time creates no catch-up credit");
            long at = game.Clock.StepIndex; hash = game.Hash(); game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
            c.That(at == game.Clock.StepIndex && hash == game.Hash(), "sole paused clock freezes flow"); game.Clock.Resume(); twin.Clock.Resume();
            game.Seat(0).View.TryFindResource("steel", out var steel);
            Order(c, game, new HaulResourcesCommand { Source = Entity(game, "depot"), Destination = Entity(game, "binding.watcher"),
                Worker = Entity(game, "truck"), Resource = steel, Ceiling = Fix.FromInt(5), Priority = 90 });
            c.Equal(Fix.Zero, Stock(game, "binding.watcher"), "normal player order cannot deliver instantly"); twin = factory.Load(game.Save());
            int updates = 0; for (; updates < 240 && Stock(game, "binding.watcher") < Fix.FromInt(5); updates++) Step(game, twin);
            if (Stock(game, "binding.watcher") != Fix.FromInt(5)) throw new InvalidOperationException("hauling stalled step=" + game.Clock.StepIndex + " cargo=" + Stock(game, "truck"));
            c.That(updates > 1, "physical move/load/delivery takes game time"); c.Equal(Fix.FromInt(45), Stock(game, "depot"), "real five-unit supply");
            c.Equal(Fix.FromInt(8), Pool(game), "local delivery cannot fabricate pool stock");
            var pools = new List<NetworkPoolView>(); c.Equal(Code.None, view.CopyPools("binding.main", pools), "original pool diagnostics");
            c.That(pools.Count == 1 && pools[0].MemberCount == 1, "one actual topology/pool owner");
            var flows = new List<NetworkFlowSnapshot>(); c.Equal(Code.None, view.CopyFlow("binding.main", flows), "original earned-time diagnostics");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed original codecs restore"); c.Equal(untouched, isolated.Hash(), "shared-factory session isolation");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "normal input/timed transfers/hauling replay");
            return game.Clock.StepIndex;
        }
        private static long Pull(Check c, string mode)
        {
            string key = mode == "native" ? "ports" : "independent_network_ports";
            var factory = Create(new InitializeProbe(), out _, mode, edit: document =>
                document.Get("entities")[1].Get("components").Get(key).Get("list")[0].Set("pull", Json.Parse("[\"steel\"]")));
            var game = New(factory); var limits = new ReplayLimits(4 * 1024 * 1024, 200); var recorder = game.RecordReplay(limits); var twin = factory.Load(game.Save());
            Step(game, twin); c.Equal(Fix.FromInt(4), Pool(game), "first push earns pool stock"); c.Equal(Fix.Zero, Stock(game, "binding.watcher"), "newly pushed stock cannot earn same-step pull time");
            Step(game, twin); c.Equal(Fix.FromInt(4), Stock(game, "binding.watcher"), "next minute earns actual pull"); c.Equal(Fix.FromInt(4), Pool(game), "simultaneous next push/pull conserves pool");
            twin = factory.Load(game.Save()); Step(game, twin); c.Equal(Fix.FromInt(8), Stock(game, "binding.watcher"), "third minute continues original pull cursor");
            c.Equal(Fix.FromInt(18), Stock(game, "binding.source"), "three actual source withdrawals");
            c.Equal(Fix.FromInt(30), Stock(game, "binding.source") + Pool(game) + Stock(game, "binding.watcher"), "source/pool/destination conserve stock");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "earned push/pull original codecs restore");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "push/pull command-free elapsed-time replay");
            return game.Clock.StepIndex;
        }
        private static void Invalid(Check c)
        {
            var faults = new List<string> { "connections-null", "connection-default", "duplicate-network", "foreign-network", "resource-invalid", "resource-unsupported", "resource-duplicate", "metadata-throw",
                "missing-list", "missing-network", "empty-network", "unknown-network", "raw-duplicate-network", "raw-duplicate-push", "raw-duplicate-pull", "empty-push", "empty-pull", "unknown-push", "unknown-pull",
                "unsupported-push", "unsupported-pull", "extra", "unbound-original", "invalid-runtime", "missing-inlet", "missing-presence", "wrong-space" };
            foreach (string fault in faults)
            {
                var initialize = new InitializeProbe();
                c.Throws<ContentException>(() => New(Create(initialize, out _, edit: document =>
                {
                    var unused = document.Get("entities")[0].Clone(); unused.Set("id", JsonValue.Of("ports.unused")); document.Get("entities").Add(unused);
                    var parts = unused.Get("components"); var binding = parts.Get("independent_network_ports"); var row = binding.Get("list")[0];
                    binding.Set("metadata_fault", JsonValue.Of(fault));
                    if (fault == "missing-list") binding.Remove("list");
                    if (fault == "missing-network") row.Remove("network");
                    if (fault == "empty-network") row.Set("network", JsonValue.Of(""));
                    if (fault == "unknown-network") row.Set("network", JsonValue.Of("missing"));
                    if (fault == "raw-duplicate-network") binding.Get("list").Add(row.Clone());
                    if (fault == "raw-duplicate-push") row.Set("push", Json.Parse("[\"steel\",\"steel\"]"));
                    if (fault == "raw-duplicate-pull") row.Set("pull", Json.Parse("[\"steel\",\"steel\"]"));
                    foreach (string direction in new[] { "push", "pull" })
                    {
                        if (fault == "empty-" + direction) row.Set(direction, Json.Parse("[\"\"]"));
                        if (fault == "unknown-" + direction) row.Set(direction, Json.Parse("[\"missing\"]"));
                        if (fault == "unsupported-" + direction) row.Set(direction, Json.Parse("[\"water\"]"));
                    }
                    if (fault == "extra") binding.Set("instant_transfer", JsonValue.Of(true));
                    if (fault == "unbound-original" || fault == "invalid-runtime")
                    { binding.Remove("metadata_fault"); parts.Remove("independent_network_ports"); parts.Set(fault == "unbound-original" ? "unbound_original_ports" : "invalid_network_ports_runtime", binding); }
                    if (fault == "missing-inlet") parts.Remove("storage");
                    if (fault == "missing-presence") parts.Remove("tile_presence");
                    if (fault == "wrong-space") parts.Get("tile_presence").Set("space", JsonValue.Of("ports.other"));
                })), "unused malformed connection refuses " + fault);
                c.Equal(0, initialize.Initializations, "all loaded metadata admitted before Initialize " + fault);
            }
        }
        private static void Faults(Check c)
        {
            foreach (string mode in new[] { "reusable", "custom" })
                foreach (string fault in mode == "custom" ? new[] { "live-throw", "live-pause", "live-touch", "live-private", "live-copy" } : new[] { "live-throw", "live-pause", "live-touch", "live-private" })
                {
                    string key = mode == "custom" ? "independent_custom_ports" : "independent_network_ports";
                    var game = New(Create(new InitializeProbe(), out _, mode, edit: document => document.Get("entities")[0].Get("components").Get(key).Set("metadata_fault", JsonValue.Of(fault))));
                    game.Update(); c.That(game.Fault == null, "healthy first step before connection injection " + mode + "/" + fault);
                    c.Throws<Exception>(() => game.Seat(0).View.Feature<InfrastructureView>().ReadGates(Entity(game, "binding.source"), "binding.main", out _), "actual connection observation refuses " + mode + "/" + fault);
                    c.Throws<InvalidOperationException>(() => game.Save(), "faulted save refuses " + mode + "/" + fault);
                    c.Throws<InvalidOperationException>(() => game.Hash(), "faulted hash refuses " + mode + "/" + fault);
                }
        }
    }
}
