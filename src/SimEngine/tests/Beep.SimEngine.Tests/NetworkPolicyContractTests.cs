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
    // Authored input, public player actions and observations only. No gameplay decisions or state repairs.
    internal static class NetworkPolicyContractTests
    {
        internal const string Case = "Network policy contracts: independent JSON retains timed flow, physical hauling and bounded callbacks";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Create(InitializeProbe initialize, out NetworkPolicyContractsModule proof, bool independent = true,
            string kind = null, string fault = null, bool unused = false)
        {
            proof = new NetworkPolicyContractsModule();
            var document = Json.Parse(FrameworkProofContent.Read("network-policy-contracts.json"));
            if (kind != null)
            {
                var row = document.Get("independent_network_" + kind)[0];
                if (unused) { row = row.Clone(); row.Set("id", JsonValue.Of("policy.unused")); document.Get("independent_network_" + kind).Add(row); }
                row.Set("fault", JsonValue.Of(fault));
            }
            return NetworkSignalBindingTests.Create(initialize, out _, edit: root =>
            {
                if (independent)
                {
                    var network = root.Get("networks")[0];
                    network.Set("flow_policy", JsonValue.Of("policy.round-robin"));
                    network.Set("solver", JsonValue.Of("policy.transfer"));
                    network.Set("demand_policy", JsonValue.Of("policy.capacity"));
                }
                if (fault == "peer-private")
                {
                    var parts = root.Get("entities")[1].Get("components");
                    parts.Get("ports").Get("list")[0].Set("pull", Json.Parse("[\"steel\"]"));
                    parts.Set("network_signal_private_probe", Json.Parse("{}"));
                }
            }, extraModules: new GameModule[] { proof },
                extraDeclarations: "[{\"id\":\"proof.network-policy-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.infrastructure\",\"version\":\"1.0.0\"}],\"documents\":[\"network-policy-contracts.json\"]}]",
                extraDocuments: new Dictionary<string, string> { { "network-policy-contracts.json", document.ToString() } });
        }
        private static SimGame New(GameFactory factory)
        { var request = new NewGameRequest { Scenario = "colony", Seed = 919 }; request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request); }
        private static Fix Stock(SimGame game, string owner)
        {
            var rows = new List<Facet>(); game.Seat(0).View.Inspect(Entity(game, owner), rows);
            return rows.Where(f => f.Key == Storage.FacetStock && f.Subject == "steel").Aggregate(Fix.Zero, (sum, row) => sum + row.Value);
        }
        private static Fix Pool(SimGame game)
        {
            var code = game.Seat(0).View.Feature<OperationsView>().ReadSignal(Entity(game, "binding.watcher"),
                new Code("pool"), "binding.stored", out var value);
            if (!code.IsNone || !value.Available) throw new InvalidOperationException("network pool unavailable " + code + "/" + value.Blocker);
            return value.Value.Value;
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current contract build identity");
            c.Equal(154, EngineInfo.SaveFormat, "original network and resource codecs retained");
            c.That(typeof(IndependentNetworkRankingDef).BaseType == typeof(Def)
                && typeof(IndependentNetworkDemandDef).BaseType == typeof(Def)
                && typeof(IndependentNetworkTransferDef).BaseType == typeof(Def), "no native policy base required");
            c.Equal(Physical(c, false), Physical(c, true), "native and independent contracts have equal physical chronology");
            foreach (string fault in new[] { "invalid-time", "validate-pause" })
            {
                var init = new InitializeProbe();
                if (fault == "invalid-time") c.Throws<ContentException>(() => New(Create(init, out _, kind: "transfers", fault: fault, unused: true)), "unused solver validates actual time");
                else c.Throws<InvalidOperationException>(() => New(Create(init, out _, kind: "transfers", fault: fault, unused: true)), "unused solver cannot mutate clock during admission");
                c.Equal(0, init.Initializations, "unused invalid solver refuses before Initialize " + fault);
            }
            foreach (string kind in new[] { "rankings", "demands", "transfers" })
                foreach (string fault in new[] { "throw", "pause", "touch", "private", "peer-private", "pool" })
                {
                    var factory = Create(new InitializeProbe(), out _, kind: kind, fault: fault);
                    var game = New(factory); game.Update();
                    c.That(game.Fault == null, "first earned step completes before installed live fault " + kind + "/" + fault);
                    game.Update();
                    c.That(game.Fault != null, "actual callback/participating owner violation faults " + kind + "/" + fault);
                    c.Throws<InvalidOperationException>(() => Pool(game), "faulted pool facts refuse " + kind + "/" + fault);
                    c.Throws<InvalidOperationException>(() => game.Save(), "faulted save refuses " + kind + "/" + fault);
                    c.Throws<InvalidOperationException>(() => game.Hash(), "faulted hash refuses " + kind + "/" + fault);
                }
        }
        private static long Physical(Check c, bool independent)
        {
            var init = new InitializeProbe(); var factory = Create(init, out var proof, independent);
            var game = New(factory); var isolated = New(factory); ulong isolatedHash = isolated.Hash();
            c.Equal(independent, proof.ActualContracts, "actual content interface aliases bind all three independent policies");
            c.Equal(2, init.Initializations, "sessions initialize once"); c.That(!game.Modules.Contains(TycoonModule.ModuleId), "networks and hauling need no business module");
            c.Equal(Fix.Zero, Pool(game), "startup geometry gives no instantaneous resources");
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000); var recorder = game.RecordReplay(limits);
            var twin = factory.Load(game.Save()); Step(game, twin);
            c.Equal(Fix.FromInt(4), Pool(game), "one game minute earns authored four-unit allowance");
            c.Equal(Fix.FromInt(26), Stock(game, "binding.source"), "stock leaves actual source");
            Step(game, twin); c.Equal(Fix.FromInt(8), Pool(game), "second minute earns next actual transfer");
            c.Equal(Fix.FromInt(22), Stock(game, "binding.source"), "actual remaining source stock");
            if (independent)
            {
                c.That(proof.DemandContext != null && proof.ExecutionContext != null, "actual bounded callbacks observed");
                c.Throws<InvalidOperationException>(() => { var value = proof.DemandContext.Readiness; }, "demand context expires");
                var request = proof.ExecutionContext.Candidates[0];
                c.Throws<InvalidOperationException>(() => proof.ExecutionContext.Transfer(request, request.Candidate.Requested), "solver context cannot transfer after its step");
            }
            twin = factory.Load(game.Save()); for (int i = 0; i < 4; i++) Step(game, twin);
            c.Equal(Fix.FromInt(8), Pool(game), "gate backpressure earns no blocked catch-up credit");
            long at = game.Clock.StepIndex; ulong hash = game.Hash(); game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
            c.That(at == game.Clock.StepIndex && hash == game.Hash(), "one paused clock freezes physical owners"); game.Clock.Resume(); twin.Clock.Resume();
            game.Seat(0).View.TryFindResource("steel", out var steel);
            Order(c, game, new HaulResourcesCommand { Source = Entity(game, "depot"), Destination = Entity(game, "binding.watcher"),
                Worker = Entity(game, "truck"), Resource = steel, Ceiling = Fix.FromInt(5), Priority = 90 });
            c.Equal(Fix.Zero, Stock(game, "binding.watcher"), "normal command does not deliver instantly"); twin = factory.Load(game.Save());
            int updates = 0; for (; updates < 240 && Stock(game, "binding.watcher") < Fix.FromInt(5); updates++) Step(game, twin);
            if (Stock(game, "binding.watcher") != Fix.FromInt(5)) throw new InvalidOperationException("physical haul stalled: step=" + game.Clock.StepIndex + " depot=" + Stock(game, "depot") + " cargo=" + Stock(game, "truck"));
            c.That(updates > 1, "travel/loading/delivery require game time"); c.Equal(Fix.FromInt(45), Stock(game, "depot"), "delivered units came from real depot");
            c.Equal(Fix.FromInt(8), Pool(game), "local delivery is not connected pool stock");
            c.Equal(Fix.FromInt(30), Pool(game) + Stock(game, "binding.source"), "network source/pool conservation");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed original codecs restore");
            c.Equal(isolatedHash, isolated.Hash(), "interleaved idle session isolated");
            c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "ordinary inputs and physical execution replay");
            return game.Clock.StepIndex;
        }
    }
}
