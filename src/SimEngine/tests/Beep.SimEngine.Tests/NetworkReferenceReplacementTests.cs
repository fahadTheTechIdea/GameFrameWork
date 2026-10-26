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
    internal static class NetworkReferenceReplacementTests
    {
        internal const string Case = "Network replacement references: slow connected push pull timing and saved physical upgrades";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(bool half, bool direct, bool pull, string refusal = null)
        {
            var upgrade = Json.Parse(FrameworkProofContent.Read("network-replacement.json"));
            if (pull) upgrade.Get("entity_replacement_effects")[0].Set("source", JsonValue.Of("destination"));
            return SpatialNetworkGraphTests.Points(half, new PointLineGeometryProofModule(),
                extraModules: new GameModule[] { new ReplacementReferencesModule() },
                extraDeclarations: "[{\"id\":\"proof.references\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[\"network-replacement.json\"]}]",
                moduleDocuments: new Dictionary<string, string> { ["network-replacement.json"] = upgrade.ToString() },
                editNetwork: root => root.Get("networks")[0].Get("rates")[0].Get("period").Set("numerator", JsonValue.Number("17")),
                afterPorts: root =>
                {
                    root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.references"));
                    var actor = root.Get("entities")[pull ? 1 : 0];
                    if (direct)
                    {
                        var parts = actor.Get("components"); var ports = parts.Get("ports").Clone();
                        parts.Remove("ports"); parts.Set("reference_network_ports", ports);
                    }
                    var next = actor.Clone(); next.Set("id", JsonValue.Of("source.next"));
                    var partsNext = next.Get("components"); string key = direct ? "reference_network_ports" : "ports";
                    if (refusal == "missing") partsNext.Remove(key);
                    if (refusal == "direction")
                    {
                        var connection = partsNext.Get(key).Get("list")[0];
                        connection.Set(pull ? "pull" : "push", JsonValue.NewArray());
                        connection.Set(pull ? "push" : "pull", Json.Parse("[\"supply\"]"));
                    }
                    if (refusal == "resource") partsNext.Get(key).Get("list")[0].Set(pull ? "pull" : "push", JsonValue.NewArray());
                    root.Get("entities").Add(next);
                });
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest
        { Scenario = "points", Seed = 31, Seats = new List<Controller> { Controller.Human, Controller.Human }, Mods = { ResourceFlowsModule.ModuleId } });
        private static EntityId Find(SimGame game, string prototype) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == prototype).Id;
        private static List<NetworkFlowSnapshot> Flows(SimGame game, int seat = 0)
        {
            var facts = new List<NetworkFlowSnapshot>(); Code result = game.Seat(seat).View.Feature<InfrastructureView>().CopyFlow("point.network", facts);
            if (!result.IsNone) throw new InvalidOperationException("network flow view: " + result); return facts;
        }
        private static NetworkFlowSnapshot Flow(SimGame game, EntityId actor, bool pull)
            => Flows(game).Single(f => f.Entity == actor && f.Pull == pull);
        private static Fix Total(SimGame game)
        {
            var pools = new List<NetworkPoolView>();
            Code result = game.Seat(0).View.Feature<InfrastructureView>().CopyPools("point.network", pools);
            if (!result.IsNone) throw new InvalidOperationException("network pool view: " + result);
            return SpatialCorridorExecutionTests.Total(game, true) + pools.SelectMany(p => p.Stock).Aggregate(Fix.Zero, (sum, q) => sum + q.Amount);
        }
        private static Fix Amount(SimGame game, EntityId actor)
        {
            var facts = new List<Facet>(); game.Seat(0).View.Inspect(actor, facts);
            return facts.Single(f => f.Key == Storage.FacetStock && f.Subject == "supply").Value;
        }
        private static void Step(SimGame game, SimGame twin = null)
        { game.Update(); twin?.Update(); if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("network replacement fault: " + game.Fault + "/" + twin?.Fault); }
        private static CommandResult Send(Check c, SimGame game, Command command)
        {
            c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary public admission");
            int sequence = game.Seat(0).Submit(command); Step(game); long cursor = 0;
            var answers = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, answers);
            var answer = answers.Single(a => a.Sequence == sequence); c.That(answer.Accepted, "engine inbox commits player intent"); return answer;
        }
        private static void Build(Check c, SimGame game, bool half, bool pull)
        {
            long plan = Send(c, game, new SubmitPlanCommand { Intent = SpatialCorridorExecutionTests.Intent(true, false) }).Arg0;
            bool done = false;
            for (int i = 0; i < 750 && !done; i++)
            {
                var plans = new List<PlanView>(); game.Seat(0).View.Feature<WorkView>().CopyPlans(plans);
                done = plans.Single(p => p.Id == plan).State == PlanState.Done;
                if (!done) Step(game);
            }
            if (!done) throw new InvalidOperationException("physical corridor timeout; clock=" + game.Tick + "; flow=" + Flows(game).Count);
            c.Equal(Fix.FromInt(14), Total(game), "corridor consumes exactly six supplied materials");
            var actor = Find(game, pull ? "destination" : "source"); int period = half ? 34 : 17; bool ready = false;
            for (int i = 0; i < 100 && !ready; i++)
            { ready = Flows(game).Any(f => f.Entity == actor && f.Pull == pull && f.ElapsedTicks % period == 1); if (!ready) Step(game); }
            if (!ready) throw new InvalidOperationException("slow flow warmup timeout; clock=" + game.Tick + "; actor=" + actor);
        }
        private static void Run(Check c)
        {
            foreach (bool half in new[] { false, true }) foreach (bool direct in new[] { false, true }) foreach (bool pull in new[] { false, true })
            {
                var factory = Factory(half, direct, pull); var game = New(factory); var isolated = New(factory); ulong isolatedHash = isolated.Hash();
                Build(c, game, half, pull); var old = Find(game, pull ? "destination" : "source");
                var before = Flow(game, old, pull); var poolEndpoint = pull ? before.Source : before.Destination;
                var command = new PostOrderCommand { Plan = "network.upgrade", Target = old }; ulong hash = game.Hash();
                c.Equal(Code.None, game.Seat(0).Preview(command), "compatible retained network flow admits replacement");
                c.Equal(hash, game.Hash(), "network reference admission is pure");
                Send(c, game, command); var twin = factory.Load(game.Save());
                for (int i = 0; i < 30 && !game.Seat(0).Snapshot.Entities.Any(e => e.Def == "source.next"); i++) Step(game, twin);
                var made = Find(game, "source.next"); var after = Flow(game, made, pull);
                c.That(!game.Seat(0).Snapshot.Entities.Any(e => e.Id == old), "physical work retires original actor");
                c.Equal(before.ElapsedTicks + game.Tick - before.LastTick, after.ElapsedTicks, "earned flow time survives actor replacement without restart");
                c.Equal(game.Tick, after.LastTick, "flow advances only through the sole clock");
                c.Equal(poolEndpoint, pull ? after.Source : after.Destination, "actual connected pool identity stays unchanged");
                c.Equal(ResourceEndpointId.ForEntity(made), pull ? after.Destination : after.Source, "actual entity-owned endpoint rebinds");
                c.Equal(old, before.Entity, "detached historical flow facts stay unchanged");
                c.Equal(Fix.FromInt(14), Total(game), "upgrade and network flows conserve all remaining material");
                c.Equal(game.Hash(), twin.Hash(), "saved pending upgrade continues identically");
                game = factory.Load(game.Save()); twin = factory.Load(twin.Save());
                long nextTransfer = 17 * (half ? 2 : 1) - after.ElapsedTicks % (17 * (half ? 2 : 1));
                Fix stockBefore = Amount(game, made);
                for (int i = 0; i <= nextTransfer; i++)
                {
                    Step(game, twin); c.Equal(Fix.FromInt(14), Total(game), "continued quantized transfers conserve real stock");
                    c.Equal(stockBefore + (i + 1 >= nextTransfer ? pull ? Fix.One : -Fix.One : Fix.Zero), Amount(game, made),
                        "one real quantized transfer occurs at retained deadline, never early or restarted");
                }
                var later = Flow(game, made, pull);
                c.Equal(after.ElapsedTicks + nextTransfer + 1, later.ElapsedTicks, "normal subsequent allowance uses retained elapsed phase");
                c.Equal(game.Hash(), twin.Hash(), "saved rebound flow continues deterministically");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "retained flow/pool state reloads coherently");
                c.Equal(0, Flows(game, 1).Count, "another company sees no private actor timing");
                c.Equal(isolatedHash, isolated.Hash(), "interleaved independent session remains isolated");
            }
            foreach (bool direct in new[] { false, true }) foreach (string refusal in new[] { "missing", "direction", "resource" })
            {
                var factory = Factory(false, direct, false, refusal); var game = New(factory); Build(c, game, false, false);
                var old = Find(game, "source"); var command = new PostOrderCommand { Plan = "network.upgrade", Target = old }; ulong hash = game.Hash();
                c.Equal(NetworkService.ReplacementBinding, game.Seat(0).Preview(command), "incompatible connection metadata refuses");
                c.Equal(hash, game.Hash(), "refusal changes no elapsed flow or resource state");
                int sequence = game.Seat(0).Submit(command); Step(game); long cursor = 0; var answers = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, answers);
                c.That(answers.Any(a => a.Sequence == sequence && a.Refusal == NetworkService.ReplacementBinding), "preview and inbox use same reference owner refusal");
                c.That(game.Seat(0).Snapshot.Entities.Any(e => e.Id == old), "refused replacement leaves actual actor alive");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "refused flow remains saveable");
            }
        }
    }
}
