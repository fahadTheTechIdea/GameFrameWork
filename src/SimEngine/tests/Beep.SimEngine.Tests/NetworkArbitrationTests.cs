using System.Linq;
using System;
using System.Collections.Generic;
using System.Text;
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
    public static class NetworkArbitrationTests
    {
        private const string Network = "arbitration.main";
        public static void Register()
        {
            TestRunner.Add("Network arbitration: authored contracts, pure ranking and durable history", Contracts);
            TestRunner.Add("Network arbitration: competing physical legs and saved fair continuation", Physical);
        }
        private static GameFactory Factory(Action<JsonValue> edit = null)
        {
            JsonValue content = Json.Parse(FrameworkProofContent.Read("network-arbitration.json")); edit?.Invoke(content);
            var text = new StringBuilder(); content.WriteCanonical(text);
            return Colony.Factory(FrameworkProofContent.Read("resource-recovery.json"), FrameworkProofContent.Read("endpoints-flows.json"),
                FrameworkProofContent.Read("network-transfers.json"), additionalDocuments: new Dictionary<string, string> { ["network-arbitration.json"] = text.ToString() });
        }
        private static NetworkService Lines(SimGame game) => game.World.Services.Providers<NetworkService>().Single();
        private static ResourceId Steel(SimGame game) => game.World.Content.ResourceId("steel");
        private static NetworkService.NetComponent Pool(SimGame game) => Lines(game).Component(Network, 0);
        private static void Tick(SimGame game)
        { game.Update(); if (game.Fault != null) throw new InvalidOperationException("arbitration tick " + game.Tick + ": " + game.Fault); }
        private static void Advance(SimGame game, int ticks, SimGame twin = null)
        { for (int i = 0; i < ticks; i++) { Tick(game); if (twin != null) Tick(twin); } }
        private static Entity Spawn(SimGame game, string def, int x, int y = 28) => ResourceDispositionTests.Spawn(game, def, x, y);
        private static SimGame Setup(GameFactory factory, bool push, out Entity source, out Entity first, out Entity second)
        {
            SimGame game = ResourceDispositionTests.New(factory);
            source = Spawn(game, "arbitration.source", 9);
            first = Spawn(game, push ? "arbitration.proof-source" : "arbitration.receiver", 11);
            second = Spawn(game, push ? "arbitration.receiver" : "arbitration.proof-receiver", 10, 27);
            Spawn(game, "arbitration.inactive", 20); // Four ports make a four-tick supply cycle alias old tick rotation.
            if (!((Beep.SimEngine.Maps.TileLineGeometry)Lines(game).World.Services.Get<ILineGeometry>()).Stamp(game.World.Content.Get<ILineDefinition>("network.pipe"), new TileCoord(10, 28), new PlayerId(0)))
                throw new InvalidOperationException("fixture line refused");
            return game;
        }
        private static Fix Amount(SimGame game, EntityId entity) => game.World.Entities.Get(entity).Get<IInventory>().Amount(Steel(game));
        private static NetworkService.FlowTurn Turn(SimGame game, bool pull)
        { foreach (var turn in Pool(game).FlowTurns) if (turn.Pull == pull && turn.Resource == Steel(game)) return turn; return null; }
        private static void Conserved(Check c, SimGame game, EntityId source, EntityId first, EntityId second, bool push)
            => c.Equal(Fix.FromInt(push ? 60 : 30), Amount(game, source) + Amount(game, first) + Amount(game, second) + Pool(game).Amount(Steel(game)), "actual native steel conserved across contention");
        private static void Valve(Check c, SimGame game, EntityId source, bool enabled)
            => c.That(TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = source, Operation = new Code("valve"), Enabled = enabled, Target = Fix.One }).Accepted, "ordinary gate command accepted");
        private static void Contracts(Check c)
        {
            GameFactory factory = Factory(); SimGame game = Setup(factory, false, out Entity source, out Entity first, out Entity second);
            INetworkDefinition definition = game.World.Content.Get<INetworkDefinition>(Network);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "breaking current build"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "current save only");
            c.Equal(4, Lines(game).StateSchemaVersion, "one graph codec embeds successful service");
            c.That(definition.FlowPolicy is RoundRobinNetworkFlowPolicyDef, "explicit authored native policy resolves through the contract");
            Tick(game); c.Equal(0, Pool(game).FlowTurns.Count, "sub-quantum readiness does not fabricate a winner");
            Tick(game); c.Equal(source.Id, Turn(game, false).Entity, "only actual push records successful participant");
            c.Equal(2L, Turn(game, false).Tick, "successful history uses actual completed step");
            c.That(Turn(game, true) == null, "same-step pushed stock cannot create pull service history");
            ulong hash = game.Hash(); SimGame twin = factory.Load(game.Save()); c.Equal(hash, twin.Hash(), "successful history and elapsed eligibility roundtrip");
            var facts = new List<NetworkFlowSnapshot>(); game.Seat(0).View.Feature<InfrastructureView>().CopyFlow(Network, facts);
            c.Equal(hash, game.Hash(), "detached views never rank or rotate service");
            game.Seat(1).View.Feature<InfrastructureView>().CopyFlow(Network, facts); c.Equal(0, facts.Count, "private flow does not leak to another company");
            var history = Turn(game, false); long tick = history.Tick; EntityId entity = history.Entity;
            history.Tick = game.Tick + 1; c.Throws<CorruptStateException>(() => game.Save(), "future service time refuses"); history.Tick = tick;
            history.Entity = new EntityId(int.MaxValue); c.Throws<CorruptStateException>(() => game.Save(), "never-created historical participant refuses"); history.Entity = entity;
            Pool(game).FlowTurns.Add(history); c.Throws<CorruptStateException>(() => game.Save(), "duplicate resource/direction history refuses"); Pool(game).FlowTurns.RemoveAt(Pool(game).FlowTurns.Count - 1);
            history.Resource = Water(game); c.Throws<CorruptStateException>(() => game.Save(), "uncarried historical resource refuses"); history.Resource = Steel(game);
            c.Equal(hash, game.Hash(), "restored valid instrumentation changes no authoritative facts");
            foreach (Action<JsonValue> edit in new Action<JsonValue>[] {
                root => root.Get("networks")[0].Remove("flow_policy"),
                root => root.Get("networks")[0].Set("flow_policy", JsonValue.Of("missing")),
                root => root.Get("networks")[0].Set("flow_policy", JsonValue.Of("network.pipe")),
                root => root.Get("network_round_robin_policies")[0].Set("strategy", JsonValue.Of("old")) })
                c.Throws<ContentException>(() => ResourceDispositionTests.New(Factory(edit)), "missing/wrong/obsolete policy metadata refuses during content resolution");
            factory = Factory(root => root.Get("networks")[0].Set("flow_policy", JsonValue.Of("arbitration.highest")));
            game = Setup(factory, false, out source, out first, out second); Advance(game, 4);
            c.That(game.World.Content.Get<INetworkDefinition>(Network).FlowPolicy is ProofNetworkFlowPolicyDef, "public-only extension supplies policy without a system edit");
            c.Equal(Fix.One, Amount(game, second.Id), "custom rank changes actual winner"); c.Equal(Fix.Zero, Amount(game, first.Id), "custom ranking is not silently replaced by native policy");
            factory = Factory(root => root.Get("networks")[0].Set("flow_policy", JsonValue.Of("arbitration.equal")));
            game = Setup(factory, false, out source, out first, out second); Advance(game, 4);
            c.Equal(Fix.One, Amount(game, first.Id), "equal custom ranks tie-break by actual stable identity");
            factory = Factory(root => root.Get("networks")[0].Set("flow_policy", JsonValue.Of("arbitration.fault")));
            game = Setup(factory, false, out source, out first, out second);
            Advance(game, 3); // Both actual receiving legs participate before their due-service pass.
            game.World.Content.Get<FaultNetworkFlowPolicyDef>("arbitration.fault").ReadFault = () => first.Touch();
            game.Update(); c.That(game.Fault != null && game.Fault.Phase == "sim.lines", "mutation of another participating port faults pure arbitration");
            c.Equal(Fix.One, Pool(game).Amount(Steel(game)), "fault preserves actual stock before any transfer in the pass");
            c.Equal(Fix.Zero, Amount(game, first.Id), "fault cannot silently deliver to the changed participant");
            c.Throws<InvalidOperationException>(() => game.Save(), "faulted policy cannot become a healthy checkpoint");
        }
        private static ResourceId Water(SimGame game) => game.World.Content.ResourceId("water");
        private static void Physical(Check c)
        {
            GameFactory factory = Factory(); SimGame game = Setup(factory, false, out Entity source, out Entity first, out Entity second);
            EntityId s = source.Id, a = first.Id, b = second.Id;
            Advance(game, 3); c.That(Turn(game, true) == null, "receiving service waits actual own elapsed time");
            SimGame twin = factory.Load(game.Save()); Advance(game, 1, twin);
            c.Equal(Fix.One, Amount(game, a), "first sparse opportunity serves first stable candidate"); c.Equal(Fix.Zero, Amount(game, b), "capacity is real, not duplicated between consumers");
            c.Equal(a, Turn(game, true).Entity, "only committed pull advances history"); c.Equal(game.Hash(), twin.Hash(), "save before arbitration preserves winner");
            Advance(game, 4, twin); c.Equal(Fix.One, Amount(game, b), "next same-residue opportunity serves the other consumer");
            c.Equal(b, Turn(game, true).Entity, "successful round-robin defeats tick-period aliasing");
            Advance(game, 72, twin); c.Equal(Fix.FromInt(10), Amount(game, a), "first consumer receives ten finite opportunities");
            c.Equal(Fix.FromInt(10), Amount(game, b), "second consumer cannot starve at the same tick residue");
            c.Equal(game.Hash(), twin.Hash(), "interleaved saved fair sessions stay identical"); Conserved(c, game, s, a, b, false);
            long servedAt = Turn(game, true).Tick; EntityId served = Turn(game, true).Entity;
            Valve(c, game, s, false); Tick(game); Advance(game, 31);
            c.Equal(servedAt, Turn(game, true).Tick, "dry/gated idle time does not rotate successful history");
            c.Equal(served, Turn(game, true).Entity, "idle consumer order is not derived from elapsed tick");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "idle history survives a checkpoint");
            Valve(c, game, s, true); Valve(c, twin, s, true); Advance(game, 2, twin);
            c.Equal(Fix.FromInt(10), Amount(game, a), "resuming does not manufacture past receiving service"); Tick(game); Tick(twin);
            c.Equal(Fix.FromInt(11), Amount(game, a), "resumed opportunity starts after the last successful consumer");
            c.Equal(game.Hash(), twin.Hash(), "gate-resumed twins retain success-based order"); Conserved(c, game, s, a, b, false);
            long currentTime = game.Tick; ulong pausedHash = game.Hash(); servedAt = Turn(game, true).Tick;
            game.Clock.Pause(); game.Update(); game.Update();
            c.Equal(currentTime, game.Tick, "one paused game clock freezes all simulation advancement");
            c.Equal(servedAt, Turn(game, true).Tick, "last service is a timestamp, not another advancing clock");
            c.Equal(pausedHash, game.Hash(), "paused Update changes no network progress or arbitration"); game.Clock.Resume();

            game = Setup(factory, true, out source, out first, out second); s = source.Id; a = first.Id; b = second.Id;
            Advance(game, 2); c.Equal(Fix.FromInt(29), Amount(game, s), "first producer alone wins scarce pool capacity");
            c.Equal(Fix.FromInt(30), Amount(game, a), "losing producer does not consume or duplicate stock");
            var pushTurn = Turn(game, false); servedAt = pushTurn.Tick; Tick(game);
            c.Equal(servedAt, pushTurn.Tick, "full-capacity attempt does not advance winner");
            twin = factory.Load(game.Save()); Advance(game, 3, twin);
            c.Equal(Fix.FromInt(29), Amount(game, a), "next capacity opportunity serves independent producer");
            Advance(game, 74, twin); c.Equal(Fix.FromInt(20), Amount(game, s), "native producer gets ten successful turns");
            c.Equal(Fix.FromInt(20), Amount(game, a), "independent producer gets ten successful turns");
            c.Equal(Fix.FromInt(20), Amount(game, b), "receiver gets only actual twenty transfers");
            c.Equal(game.Hash(), twin.Hash(), "producer contention history survives saved continuation"); Conserved(c, game, s, a, b, true);

            // Unchanged topology identity retains fairness; changed membership starts new actual history.
            EntityId endpointWinner = Turn(game, false).Entity;
            c.That(((Beep.SimEngine.Maps.TileLineGeometry)Lines(game).World.Services.Get<ILineGeometry>()).Stamp(game.World.Content.Get<ILineDefinition>("network.pipe"), new TileCoord(30, 28), new PlayerId(0)), "unrelated physical component added");
            Tick(game); c.Equal(endpointWinner, Turn(game, false).Entity, "unrelated rebuild retains existing endpoint/history");
            c.That(((Beep.SimEngine.Maps.TileLineGeometry)Lines(game).World.Services.Get<ILineGeometry>()).Stamp(game.World.Content.Get<ILineDefinition>("network.pipe"), new TileCoord(10, 29), new PlayerId(0)), "current membership physically expanded");
            Tick(game); c.Equal(0, Pool(game).FlowTurns.Count, "changed component does not inherit another endpoint's arbitration history");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "new component with reset history roundtrips");

            factory = Factory(root =>
            {
                var components = root.Get("entities")[2].Get("components"); components.Remove("storage");
                components.Set("single_resource_port", Json.Parse("{\"resource\":\"steel\",\"capacity_policy\":\"recovery.mixed\",\"initial\":0,\"receipt_fault\":\"refuse_in\"}"));
            });
            game = Setup(factory, false, out source, out first, out second); Advance(game, 4);
            c.Equal(Fix.Zero, Amount(game, first.Id), "ordinary pure receipt refusal moves no stock");
            c.Equal(Fix.One, Amount(game, second.Id), "next due compatible candidate can still commit");
            c.Equal(second.Id, Turn(game, true).Entity, "failed receipt attempt cannot steal successful history");
            twin = factory.Load(game.Save()); Advance(game, 4, twin);
            c.Equal(Fix.FromInt(2), Amount(game, second.Id), "repeated refused attempts neither block nor fabricate another winner");
            c.Equal(game.Hash(), twin.Hash(), "failed-attempt continuation is deterministic and durable");
            Conserved(c, game, source.Id, first.Id, second.Id, false);

            factory = Factory(); game = Setup(factory, false, out source, out first, out second); Advance(game, 4);
            a = first.Id;
            var consume = new ResourceInventoryIntent(new ReservationOwner("proof.arbitration-fixture", a.Value), first.Owner, a,
                new Bill(new[] { new ResourceAmount(Steel(game), Fix.One) }), ResourceFlowKind.Consumed, new Code("proof.explicit-test-effect"));
            c.That(game.World.Services.Get<IResourceTransaction>().CommitInventory(consume).Committed, "actual cleanup consumes the removed receiver's stock");
            game.World.Despawn(first); Tick(game);
            c.That(game.World.Entities.Get(a) == null, "actual participant removal completes without stock loss");
            c.Equal(a, Turn(game, true).Entity, "historical successful identity is not replaced when participant disappears");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "legitimate removed historical winner still checkpoints");
            Advance(game, 3, twin); c.Equal(Fix.One, Amount(game, second.Id), "remaining compatible consumer receives the next physical opportunity");
            c.Equal(game.Hash(), twin.Hash(), "removed-winner continuation remains identical");
        }
    }
}
