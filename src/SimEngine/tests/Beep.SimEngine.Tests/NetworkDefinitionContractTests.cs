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
    // Master-listed authoring, normal player actions and observations; no network/stock decisions.
    internal static class NetworkDefinitionContractTests
    {
        internal const string Case = "Network definitions: independent master JSON drives original pools, gates and physical hauling";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Create(InitializeProbe initialize, out NetworkDefinitionContractsModule proof,
            bool independent = true, Action<JsonValue> edit = null)
        {
            proof = new NetworkDefinitionContractsModule();
            var document = Json.Parse(FrameworkProofContent.Read("network-definition-contracts.json"));
            if (!independent) document.Set("independent_networks", Json.Parse("[]"));
            edit?.Invoke(document);
            return NetworkSignalBindingTests.Create(initialize, out _, edit: root => { if (independent) root.Remove("networks"); },
                extraModules: new GameModule[] { proof },
                extraDeclarations: "[{\"id\":\"proof.network-definition-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.infrastructure\",\"version\":\"1.0.0\"}],\"documents\":[\"network-definition-contracts.json\"]}]",
                extraDocuments: new Dictionary<string, string> { { "network-definition-contracts.json", document.ToString() } });
        }
        private static SimGame New(GameFactory factory)
        { var request = new NewGameRequest { Scenario = "colony", Seed = 929 }; request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request); }
        private static Fix Stock(SimGame game, string owner)
        { var rows = new List<Facet>(); game.Seat(0).View.Inspect(Entity(game, owner), rows); return rows.Where(f => f.Key == Storage.FacetStock && f.Subject == "steel").Aggregate(Fix.Zero, (sum, row) => sum + row.Value); }
        private static Fix Pool(SimGame game)
        {
            var refusal = game.Seat(0).View.Feature<OperationsView>().ReadSignal(Entity(game, "binding.watcher"), new Code("pool"), "binding.stored", out var value);
            if (!refusal.IsNone || !value.Available) throw new InvalidOperationException("pool unavailable " + refusal + "/" + value.Blocker);
            return value.Value.Value;
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current definition contract identity");
            c.Equal(154, EngineInfo.SaveFormat, "original network/resource/flow codecs");
            c.That(typeof(IndependentNetworkDef).BaseType == typeof(Def), "independent network needs no native network inheritance");
            c.Equal(Physical(c, false), Physical(c, true), "same native/direct physical chronology");
            Invalid(c); Faults(c);
        }
        private static long Physical(Check c, bool independent)
        {
            var init = new InitializeProbe(); var factory = Create(init, out var proof, independent);
            var game = New(factory); var isolated = New(factory); ulong idleHash = isolated.Hash();
            c.Equal(independent, proof.ActualContract, "actual interface network/capacity aliases"); c.Equal(2, init.Initializations, "one Initialize per authored session");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "networks/hauling are resource-only");
            c.Equal(Fix.Zero, Pool(game), "authored startup pipe never fabricates initial stock");
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000); var recorder = game.RecordReplay(limits); var twin = factory.Load(game.Save());
            Step(game, twin); c.Equal(Fix.FromInt(4), Pool(game), "first minute earns four units"); c.Equal(Fix.FromInt(26), Stock(game, "binding.source"), "actual source withdrawal");
            Step(game, twin); c.Equal(Fix.FromInt(8), Pool(game), "second minute earns four more"); c.Equal(Fix.FromInt(22), Stock(game, "binding.source"), "real remaining stock");
            var infrastructure = game.Seat(0).View.Feature<InfrastructureView>();
            c.Equal(Code.None, infrastructure.ReadGates(Entity(game, "binding.source"), "binding.main", out var gate), "original gate reads independent network alias");
            c.Equal(new Code("binding.backpressure"), gate.Blocker, "same original signal-driven backpressure"); twin = factory.Load(game.Save());
            for (int i = 0; i < 4; i++) Step(game, twin); c.Equal(Fix.FromInt(8), Pool(game), "blocked time does not accrue catch-up credit");
            long at = game.Clock.StepIndex; ulong hash = game.Hash(); game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
            c.That(at == game.Clock.StepIndex && hash == game.Hash(), "sole paused clock freezes actual state"); game.Clock.Resume(); twin.Clock.Resume();
            game.Seat(0).View.TryFindResource("steel", out var steel);
            Order(c, game, new HaulResourcesCommand { Source = Entity(game, "depot"), Destination = Entity(game, "binding.watcher"),
                Worker = Entity(game, "truck"), Resource = steel, Ceiling = Fix.FromInt(5), Priority = 90 });
            c.Equal(Fix.Zero, Stock(game, "binding.watcher"), "command cannot deliver instantly"); twin = factory.Load(game.Save());
            int updates = 0; for (; updates < 240 && Stock(game, "binding.watcher") < Fix.FromInt(5); updates++) Step(game, twin);
            if (Stock(game, "binding.watcher") != Fix.FromInt(5)) throw new InvalidOperationException("physical hauling stalled step=" + game.Clock.StepIndex + " depot=" + Stock(game, "depot") + " cargo=" + Stock(game, "truck"));
            c.That(updates > 1, "truck physically travels/loads/delivers"); c.Equal(Fix.FromInt(45), Stock(game, "depot"), "actual supplied stock consumed");
            c.Equal(Fix.FromInt(8), Pool(game), "local hauling cannot grant connected stock"); c.Equal(Fix.FromInt(30), Pool(game) + Stock(game, "binding.source"), "actual source/pool conservation");
            var pools = new List<NetworkPoolView>(); c.Equal(Code.None, infrastructure.CopyPools("binding.main", pools), "original pool diagnostic uses independent content");
            c.That(pools.Count == 1 && pools[0].MemberCount == 1, "one original connected component, no duplicate graph");
            var flows = new List<NetworkFlowSnapshot>(); c.Equal(Code.None, infrastructure.CopyFlow("binding.main", flows), "original earned-time diagnostic");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed original codecs restore"); c.Equal(idleHash, isolated.Hash(), "interleaved session remains isolated");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "ordinary input/physical network and haul replay");
            return game.Clock.StepIndex;
        }
        private static void Invalid(Check c)
        {
            var faults = new List<string> { "line-null", "foreign-line", "capacity-null", "rates-null", "rates-empty", "rates-duplicate",
                "resources-null", "resources-empty", "resources-duplicate", "flow-null", "solver-null", "demand-null", "carries-extra", "rate-extra", "rate-copy", "public-throw",
                "missing-rates", "empty-rates", "duplicate-resource", "native-fraction", "zero-period", "extra" };
            foreach (string key in new[] { "line", "capacity_policy", "flow_policy", "solver", "demand_policy" })
            { faults.Add("missing-" + key); faults.Add("empty-" + key); faults.Add("unknown-" + key); }
            foreach (string fault in faults)
            {
                var init = new InitializeProbe();
                c.Throws<ContentException>(() => New(Create(init, out _, edit: document =>
                {
                    var row = document.Get("independent_networks")[0].Clone(); row.Set("id", JsonValue.Of("binding.unused")); document.Get("independent_networks").Add(row);
                    row.Set("metadata_fault", JsonValue.Of(fault));
                    if (fault == "missing-rates") row.Remove("rates");
                    if (fault == "empty-rates") row.Set("rates", Json.Parse("[]"));
                    if (fault == "duplicate-resource") row.Get("rates").Add(row.Get("rates")[0].Clone());
                    if (fault == "native-fraction") row.Get("rates")[0].Set("quantity", JsonValue.Number("0.5"));
                    if (fault == "zero-period") row.Get("rates")[0].Get("period").Set("numerator", JsonValue.Number("0"));
                    if (fault == "extra") row.Set("instant_flow", JsonValue.Of(true));
                    foreach (string key in new[] { "line", "capacity_policy", "flow_policy", "solver", "demand_policy" })
                    { if (fault == "missing-" + key) row.Remove(key); if (fault == "empty-" + key) row.Set(key, JsonValue.Of("")); if (fault == "unknown-" + key) row.Set(key, JsonValue.Of("missing.reference")); }
                })), "unused invalid definition refuses " + fault);
                c.Equal(0, init.Initializations, "unused content rejected before Initialize " + fault);
            }
        }
        private static void Faults(Check c)
        {
            foreach (string fault in new[] { "live-throw", "live-pause", "live-touch", "live-private" })
            {
                var game = New(Create(new InitializeProbe(), out _, edit: document => document.Get("independent_networks")[0].Set("metadata_fault", JsonValue.Of(fault))));
                game.Update(); c.That(game.Fault == null, "first earned step healthy before live selector injection " + fault);
                game.Update(); c.That(game.Fault != null, "actual Rate callback refuses mutation/failure " + fault);
                c.Throws<InvalidOperationException>(() => Pool(game), "faulted facts refuse " + fault);
                c.Throws<InvalidOperationException>(() => game.Save(), "faulted save refuses " + fault);
                c.Throws<InvalidOperationException>(() => game.Hash(), "faulted hash refuses " + fault);
            }
        }
    }
}
