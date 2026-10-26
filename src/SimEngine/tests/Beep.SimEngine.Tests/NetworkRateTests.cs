using System.Linq;
using System;
using System.Collections.Generic;
using System.Numerics;
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
    public static class NetworkRateTests
    {
        private const string Network = "rate.slow";
        private const string ObservationCase = "Network flow observation: committed transfer events expose earned end-of-update progress without advancing the clock";
        public static void Register()
        {
            TestRunner.Add("Network rates: authored native periods, contracts and private progress", Contracts);
            TestRunner.Add("Network rates: slow physical legs, blocked intervals and saved continuation", Physical);
            TestRunner.Add(ObservationCase, FlowObservation);
        }
        private static string Text(JsonValue value) { var result = new StringBuilder(); value.WriteCanonical(result); return result.ToString(); }
        private static GameFactory Factory(Action<JsonValue> edit = null, int quantumSeconds = 60,
            Action<World, ResourceFlowFact> observe = null)
        {
            JsonValue inputs = Json.Parse(FrameworkProofContent.Read("network-rates.json")); edit?.Invoke(inputs);
            JsonValue content = Json.Parse(FrameworkProofContent.Read("resource-recovery.json"));
            // This rate fixture is resource-only; the shared sample's unused supplier is not selected.
            for (int i = 0; i < content.Get("entities").Count; i++)
                if (content.Get("entities")[i].Get("id").Text == "market") content.Get("entities")[i].Get("components").Remove("supplier");
            content.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number(quantumSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            var navigation = Json.Parse(FrameworkProofContent.Read("colony-navigation.json"));
            var routes = navigation.Get("layered_tile_navigation");
            for (int i = 0; i < routes.Count; i++)
            {
                routes[i].Set("space", JsonValue.Of("recovery.tiles"));
                if (routes[i].Has("layer") && routes[i].Get("layer").Text == "colony.lanes") routes[i].Set("layer", JsonValue.Of("road"));
            }
            content.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(TileConditionsModule.ModuleId));
            if (observe != null) content.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.network-rate-observer"));
            var conditions = Json.Parse(FrameworkProofContent.Read("colony-conditions.json"));
            var bands = conditions.Get("tile_layer_band_conditions");
            for (int i = 0; i < bands.Count; i++)
            {
                bands[i].Set("space", JsonValue.Of("recovery.tiles"));
                if (bands[i].Has("layer") && bands[i].Get("layer").Text == "colony.lanes") bands[i].Set("layer", JsonValue.Of("road"));
            }
            return Colony.Factory(Text(content), FrameworkProofContent.Read("endpoints-flows.json"),
                FrameworkProofContent.Read("network-transfers.json"), additionalDocuments: new Dictionary<string, string> { ["network-rates.json"] = Text(inputs) },
                additionalModules: observe == null ? null : new[] { new ProbeModule("proof.network-rate-observer", new ModuleVersion(1, 0, 0),
                    requires: new[] { new ModuleRequirement(InventoryModule.ModuleId, new ModuleVersion(1, 0, 0)), new ModuleRequirement(InfrastructureModule.ModuleId, new ModuleVersion(1, 0, 0)) },
                    compose: composer => composer.Events.Subscribe<ResourceFlowFact>("proof.network-rate-observer.flow", fact => observe(composer.World, fact))) },
                moduleDeclarations: observe == null ? null : new[] { "{\"id\":\"proof.network-rate-observer\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.infrastructure\",\"version\":\"1.0.0\"}],\"documents\":[]}" },
                moduleDocuments: new Dictionary<string, string> { ["colony-navigation.json"] = Text(navigation), ["colony-conditions.json"] = Text(conditions) });
        }
        // Read-only engine event instrumentation. No resource, progress, job or clock mutation.
        private static void FlowObservation(Check c)
        {
            foreach (int quantum in new[] { 60, 30 })
            {
                int reads = 0;
                var factory = Factory(quantumSeconds: quantum, observe: (world, fact) => {
                    if (fact.Kind != ResourceFlowKind.StockTransfer) return;
                    var lines = world.Services.Providers<NetworkService>().Single();
                    if (fact.Source.Endpoint.Scope != lines.EndpointScope && fact.Destination.Endpoint.Scope != lines.EndpointScope) return;
                    long step = world.Clock.StepIndex;
                    var rows = new List<NetworkFlowSnapshot>();
                    c.That(world.Stepping, "event observation follows completed flow inside the same game Update");
                    c.Equal(Code.None, lines.CopyFlow(Network, PlayerId.None, rows), "post-flow event can read the original derived progress");
                    c.That(rows.Any(row => row.LastTick == checked(step + 1)), "actual earned interval is the end of this still-unsettled Update");
                    c.Equal(step, world.Clock.StepIndex, "read-only event observation advances no clock");
                    reads++;
                });
                var game = New(factory); var source = Spawn(game, "rate.source", 9); var receiver = Spawn(game, "rate.receiver", 15);
                Geometry(game); Advance(game, 7200 / quantum);
                c.That(reads >= 4, "both real resource legs publish observed committed transfers: quantum=" + quantum);
                Conservation(c, game, source.Id, receiver.Id);
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "settled flow and material state still save canonically: quantum=" + quantum);
            }
        }
        private static SimGame New(GameFactory factory) => ResourceDispositionTests.New(factory);
        private static NetworkService Lines(SimGame game) => game.World.Services.Providers<NetworkService>().Single();
        private static ResourceId Steel(SimGame game) => game.World.Content.ResourceId("steel");
        private static ResourceId Water(SimGame game) => game.World.Content.ResourceId("water");
        private static void Tick(SimGame game) { game.Update(); if (game.Fault != null) throw new InvalidOperationException("rate tick " + game.Tick + ": " + game.Fault); }
        private static void Advance(SimGame game, int ticks, SimGame twin = null)
        { for (int i = 0; i < ticks; i++) { Tick(game); if (twin != null) Tick(twin); } }
        private static Entity Spawn(SimGame game, string id, int x, int owner = 0) => ResourceDispositionTests.Spawn(game, id, x, 28, owner);
        private static Entity Find(SimGame game, EntityId id) => game.World.Entities.Get(id);
        private static NetworkService.NetComponent Pool(SimGame game) => Lines(game).Component(Network, 0);
        private static void Geometry(SimGame game, string line = "network.pipe")
        {
            for (int x = 10; x <= 14; x++)
                if (!((Beep.SimEngine.Maps.TileLineGeometry)Lines(game).World.Services.Get<ILineGeometry>()).Stamp(game.World.Content.Get<ILineDefinition>(line), new TileCoord(x, 28), new PlayerId(0))) throw new InvalidOperationException("fixture geometry refused");
        }
        private static List<NetworkFlowSnapshot> Facts(SimGame game, int viewer = 0)
        {
            var facts = new List<NetworkFlowSnapshot>();
            Code refusal = game.Seat(viewer).View.Feature<InfrastructureView>().CopyFlow(Network, facts);
            if (!refusal.IsNone) throw new InvalidOperationException("flow view refused " + refusal); return facts;
        }
        private static NetworkFlowSnapshot Fact(SimGame game, EntityId entity, ResourceId resource)
        { foreach (var fact in Facts(game)) if (fact.Entity == entity && fact.Resource == resource) return fact; return null; }
        private static void Conservation(Check c, SimGame game, EntityId source, EntityId receiver)
        {
            c.Equal(Fix.FromInt(30), Find(game, source).Get<IInventory>().Amount(Steel(game)) + Pool(game).Amount(Steel(game)) + Find(game, receiver).Get<IInventory>().Amount(Steel(game)), "real steel conserved through two legs");
            c.Equal(Fix.FromInt(6), Find(game, source).Get<IInventory>().Amount(Water(game)) + Pool(game).Amount(Water(game)) + Find(game, receiver).Get<IInventory>().Amount(Water(game)), "every native fluid bit conserved");
        }
        private static void SetStock(SimGame game, Entity entity, ResourceId resource, Fix amount, ResourceFlowKind kind)
        {
            var intent = new ResourceInventoryIntent(new ReservationOwner("proof.rate-fixture", entity.Id.Value), entity.Owner, entity.Id,
                new Bill(new[] { new ResourceAmount(resource, amount) }), kind, new Code("proof.authored-test-effect"));
            if (!game.World.Services.Get<IResourceTransaction>().CommitInventory(intent).Committed) throw new InvalidOperationException("fixture stock effect refused");
        }
        private static void Valve(Check c, SimGame game, EntityId source, bool enabled)
            => c.That(TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = source, Operation = new Code("valve"), Enabled = enabled, Target = Fix.One }).Accepted, "normal engine valve command accepted");
        private static void Contracts(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Entity source = Spawn(game, "rate.source", 9), receiver = Spawn(game, "rate.receiver", 15);
            Geometry(game); Tick(game); INetworkDefinition definition = game.World.Content.Get<INetworkDefinition>(Network);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current exact build"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no old save reader");
            c.Equal(6, Lines(game).StateSchemaVersion, "current network owner embeds elapsed cursors and service history");
            c.Equal(2, definition.Resources.Count, "carried resources derive from actual rates"); c.Equal(2, definition.Rates.Count, "one explicit native rate per resource");
            c.Equal(Fix.One, definition.Rate(Steel(game)).Quantity, "whole native count rate"); c.Equal(Fix.FromRatio(1, 2), definition.Rate(Water(game)).Quantity, "independent native fluid rate");
            c.That(receiver.Get<INetworkPorts>() is ProofNetworkPorts && receiver.Get<Ports>() == null, "independent ports require no concrete provider switch");
            c.Throws<NotSupportedException>(() => ((IList<IResourceRate>)definition.Rates).Clear(), "rate definitions immutable");
            c.Equal(1L, Fact(game, source.Id, Steel(game)).ElapsedTicks, "first actual Update earns one session quantum");
            c.Equal(0, Facts(game, 1).Count, "private elapsed port facts stay company-owned");
            ulong hash = game.Hash(); var retained = Facts(game)[0]; Facts(game); c.Equal(hash, game.Hash(), "views never advance time or transfer stock");
            SimGame twin = factory.Load(game.Save()); c.Equal(hash, twin.Hash(), "sub-quantum elapsed state roundtrips");
            byte[] healthy = game.Save();
            // Corruption is negative raw-state instrumentation, never a player operation. Each
            // malformed checkpoint is terminal; correcting fields must not revive that session.
            foreach (string corruption in new[] { "elapsed", "quantum", "duplicate", "future-cursor", "future-turn" })
            {
                var broken = factory.Load(healthy); var lines = Lines(broken); var state = lines.State(Network);
                var cursor = state.Flow[0]; long stopped = broken.Clock.StepIndex;
                if (corruption == "elapsed") cursor.Elapsed = stopped + 1;
                if (corruption == "quantum") cursor.Quantum = 0;
                if (corruption == "duplicate") state.Flow.Add(cursor);
                if (corruption == "future-cursor") cursor.LastTick = stopped + 1;
                if (corruption == "future-turn") state.Components[0].FlowTurns.Add(new NetworkService.FlowTurn {
                    Resource = Steel(broken), Entity = source.Id, Tick = stopped + 1 });
                c.Throws<CorruptStateException>(() => broken.Save(), "checkpoint rejects actual corrupt flow state: " + corruption);
                var first = broken.Fault;
                c.That(first != null && first.Phase == "checkpoint.encode", "failed checkpoint retains its original terminal owner: " + corruption);
                c.Throws<InvalidOperationException>(() => broken.Hash(), "invalid flow cannot become a healthy resumable hash: " + corruption);
                broken.Update(); c.Equal(stopped, broken.Clock.StepIndex, "corrupt flow earns no time: " + corruption);
                c.That(ReferenceEquals(first, broken.Fault), "first corruption failure is not replaced: " + corruption);
                c.Equal(hash, game.Hash(), "original healthy session remains unchanged: " + corruption);
            }
            Tick(game); c.Equal(1L, retained.ElapsedTicks, "retained snapshot detached from later progress");
            foreach (Action<JsonValue> edit in new Action<JsonValue>[] {
                root => root.Get("networks")[0].Remove("rates"),
                root => root.Get("networks")[0].Set("rates", Json.Parse("[]")),
                root => root.Get("networks")[0].Set("port_rate", JsonValue.Number("1")),
                root => root.Get("networks")[0].Set("resources", Json.Parse("[\"steel\"]")),
                root => root.Get("networks")[0].Get("rates")[0].Set("quantity", JsonValue.Number("0.5")),
                root => root.Get("networks")[0].Get("rates")[0].Get("period").Set("denominator", JsonValue.Number("0")),
                root => root.Get("networks")[0].Get("rates")[0].Set("resource", JsonValue.Of("missing")),
                root => root.Get("networks")[0].Get("rates").Add(root.Get("networks")[0].Get("rates")[0]),
                root => root.Get("entities")[0].Get("components").Get("ports").Get("list").Add(root.Get("entities")[0].Get("components").Get("ports").Get("list")[0]),
                root => root.Get("entities")[0].Get("components").Get("ports").Get("list")[0].Set("push", Json.Parse("[\"steel\",\"steel\"]")) })
                c.Throws<ContentException>(() => New(Factory(edit)), "invalid/old rate or duplicate connection refuses before tick one");
            // One common receiver quantum may be larger than the authored native quantity.
            c.Equal(new BigInteger(Fix.FromInt(2).Raw), QuantizedGameRates.AccumulatedRaw(Fix.One, 1, 1, DurationUnit.Hour,
                game.SimulationTime, new WorkProgress(120), Fix.FromInt(2).Raw, false), "sole quantizer earns a two-unit batch from a one-unit hourly rate");
            factory = Factory(root => root.Get("networks")[0].Get("rates")[0].Set("period", Json.Parse("{\"numerator\":1,\"denominator\":1000000000,\"unit\":\"second\"}")));
            game = New(factory); source = Spawn(game, "rate.source", 9); Geometry(game); Tick(game);
            c.Equal(NetworkService.FlowRange, Fact(game, source.Id, Steel(game)).Blocker, "unrepresentable one-step quantity has typed range blocker");
            c.Equal(Fix.Zero, Pool(game).Amount(Steel(game)), "range refusal fabricates no delivery");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "range-held elapsed cursor is durable");
            game = New(Factory()); source = Spawn(game, "rate.source", 9); Geometry(game); source.Get<AutomationProbe>().FaultMode = 2; game.Update();
            c.That(game.Fault != null && game.Fault.Phase == "observation.provider" &&
                game.Fault.Message.Contains("entity observation mutated scoped state"),
                "mutating signal gate retains the actual first observation fault: " + game.Fault);
            c.Throws<InvalidOperationException>(() => game.Save(), "faulted readiness cannot checkpoint as healthy");
            ISimulationTime nonDivisor = Factory(quantumSeconds: 90).ScenarioTime("colony");
            c.That(new LineSystem().GetSchedule(nonDivisor).IsDue(0), "network schedule accepts a ninety-second quantum without exact ten-minute conversion");
            factory = Factory(quantumSeconds: 300); game = New(factory); source = Spawn(game, "rate.source", 9); Geometry(game);
            Advance(game, 11); c.Equal(Fix.Zero, Pool(game).Amount(Steel(game)), "non-minute session quantum earns no early whole delivery");
            Tick(game); c.Equal(Fix.One, Pool(game).Amount(Steel(game)), "network flow earns the hour through actual five-minute session quanta");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "non-minute elapsed clock and resource state roundtrip");
            factory = Factory(root => { root.Get("networks")[0].Set("line", JsonValue.Of("network.public-pipe")); root.Get("networks")[0].Set("public", JsonValue.Of(true)); });
            game = New(factory); source = Spawn(game, "rate.source", 9); receiver = Spawn(game, "rate.receiver", 15, 1);
            Geometry(game, "network.public-pipe"); Advance(game, 61);
            c.Equal(2, Facts(game).Count, "public network does not expose another company's receiving progress");
            c.Equal(2, Facts(game, 1).Count, "receiving company sees only its actual private two-resource flow");
            var all = new List<NetworkFlowSnapshot>(); game.Observer.Feature<InfrastructureView>().CopyFlow(Network, all);
            c.Equal(4, all.Count, "explicit observer sees detached facts for both real companies");
            c.That(Facts(game)[0].Entity == source.Id && Facts(game, 1)[0].Entity == receiver.Id, "private progress never substitutes public topology visibility");
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); Entity source = Spawn(game, "rate.source", 9), receiver = Spawn(game, "rate.receiver", 15);
            Geometry(game); Advance(game, 59);
            c.Equal(Fix.Zero, Pool(game).Amount(Steel(game)), "sub-quantum intervals do not deliver a fractional whole resource");
            c.Equal(Fix.One, Fact(game, source.Id, Steel(game)).NextAllowance, "engine reports exact next earned whole allowance");
            SimGame twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.Equal(Fix.One, Pool(game).Amount(Steel(game)), "slow rate eventually pushes a real whole unit");
            c.Equal(Fix.Zero, receiver.Get<IInventory>().Amount(Steel(game)), "new push cannot satisfy past-time pull readiness");
            c.Equal(Fix.FromRatio(11, 2), source.Get<IInventory>().Amount(Water(game)), "fluid has its own authored half-unit hourly rate");
            Advance(game, 59, twin); c.Equal(Fix.Zero, receiver.Get<IInventory>().Amount(Steel(game)), "second leg must independently earn service time");
            Tick(game); Tick(twin); c.Equal(Fix.One, receiver.Get<IInventory>().Amount(Steel(game)), "first slow unit reaches destination through two earned legs");
            Conservation(c, game, source.Id, receiver.Id); c.Equal(game.Hash(), twin.Hash(), "saved slow legs continue identically");
            ResourceFlowPage history = game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0);
            c.Equal(BigInteger.Zero, history.Totals(Steel(game)).ConsumedRaw + history.Totals(Steel(game)).CreatedRaw, "network flow is transfer, not fake production or consumption");
            BigInteger received = BigInteger.Zero;
            foreach (var entry in history.Entries)
                if (entry.Item.Resource == Steel(game) && entry.Item.Destination.Endpoint == receiver.Get<IInventory>().Endpoint) received += entry.Item.Amount.Raw;
            c.Equal(new BigInteger(Fix.One.Raw), received, "journal reports only actual receiving leg");

            game = New(factory); source = Spawn(game, "rate.source", 9); receiver = Spawn(game, "rate.batch-receiver", 15); Geometry(game);
            Advance(game, 239); c.Equal(Fix.Zero, receiver.Get<IInventory>().Amount(Steel(game)), "larger receiving quantum earns its full service duration");
            twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.Equal(Fix.FromInt(2), receiver.Get<IInventory>().Amount(Steel(game)), "one-unit authored rate can deliver a two-unit native batch");
            Conservation(c, game, source.Id, receiver.Id); c.Equal(game.Hash(), twin.Hash(), "combined-quantum pending stage roundtrips");

            game = New(factory); source = Spawn(game, "rate.source", 9); receiver = Spawn(game, "rate.receiver", 15); Geometry(game); Advance(game, 30);
            Valve(c, game, source.Id, false); Advance(game, 120); c.That(Fact(game, source.Id, Steel(game)) == null, "closed intervals retain no elapsed service credit");
            c.Equal(Fix.Zero, Pool(game).Amount(Steel(game)), "closed valve moves no steel"); twin = factory.Load(game.Save());
            Valve(c, game, source.Id, true); Valve(c, twin, source.Id, true); Advance(game, 58, twin);
            c.Equal(Fix.Zero, Pool(game).Amount(Steel(game)), "reopened valve cannot spend old half-hour or blocked time");
            Tick(game); Tick(twin); c.Equal(Fix.One, Pool(game).Amount(Steel(game)), "reopened valve earns a new full hour"); c.Equal(game.Hash(), twin.Hash(), "gated continuation deterministic");
            game.Clock.Pause(); ulong paused = game.Hash(); game.Update(); c.Equal(paused, game.Hash(), "paused Update earns no flow time");
            game.Clock.Resume();

            game = New(factory); source = Spawn(game, "rate.source", 9); Geometry(game); Advance(game, 30);
            SetStock(game, source, Steel(game), source.Get<IInventory>().Amount(Steel(game)), ResourceFlowKind.Consumed); Tick(game); Advance(game, 120);
            c.That(Fact(game, source.Id, Steel(game)) == null, "empty supply gap drops elapsed credit");
            SetStock(game, source, Steel(game), Fix.FromInt(3), ResourceFlowKind.Produced); Advance(game, 59);
            c.Equal(Fix.Zero, Pool(game).Amount(Steel(game)), "new supply cannot catch up missing or prior time"); Tick(game);
            c.Equal(Fix.One, Pool(game).Amount(Steel(game)), "restocked leg earns its own new hour");

            game = New(factory); source = Spawn(game, "rate.source", 9); Geometry(game); Advance(game, 30);
            ((Beep.SimEngine.Maps.TileLineGeometry)Lines(game).World.Services.Get<ILineGeometry>()).Stamp(game.World.Content.Get<ILineDefinition>("network.pipe"), new TileCoord(15, 28), new PlayerId(0)); Tick(game);
            c.Equal(1L, Fact(game, source.Id, Steel(game)).ElapsedTicks, "changed actual pool identity resets its bound elapsed service");
            twin = factory.Load(game.Save()); Advance(game, 58, twin); c.Equal(Fix.Zero, Pool(game).Amount(Steel(game)), "topology rebind has no inherited transfer credit");
            Tick(game); Tick(twin); c.Equal(Fix.One, Pool(game).Amount(Steel(game)), "rebound real pool receives after a complete new interval"); c.Equal(game.Hash(), twin.Hash(), "rebound saved identities and time match");

            game = New(factory); source = Spawn(game, "rate.source", 9); receiver = Spawn(game, "rate.receiver", 15); Geometry(game); Advance(game, 90);
            IInventory destination = receiver.Get<IInventory>(); var holder = new ReservationOwner("proof.rate-capacity", 1);
            ReservationToken held = destination.ReserveIn(holder, Water(game), destination.Offer(Water(game), Fix.FromInt(1000)));
            Tick(game); Advance(game, 120); c.That(Fact(game, receiver.Id, Steel(game)) == null, "full promised capacity earns no receiving service time");
            twin = factory.Load(game.Save()); destination.ReleaseIn(holder, held); Find(twin, receiver.Id).Get<IInventory>().ReleaseIn(holder, held);
            Advance(game, 59, twin); c.Equal(Fix.Zero, destination.Amount(Steel(game)), "released capacity does not catch up blocked or prior elapsed service");
            Tick(game); Tick(twin); c.Equal(Fix.One, destination.Amount(Steel(game)), "unblocked receiver earns a new complete interval");
            Conservation(c, game, source.Id, receiver.Id); c.Equal(game.Hash(), twin.Hash(), "saved real capacity promise and release continue identically");

            game = New(factory); source = Spawn(game, "rate.source", 9); Geometry(game); Advance(game, 30);
            c.That(Lines(game).Remove(new PlayerId(0), game.World.Content.Get<ILineDefinition>("network.pipe"), LineDeclaration.ReadPath(game.World,(game.World.Content.Get<ILineDefinition>("network.pipe")).Layer.Space.Id,Beep.SimEngine.Maps.TilePlacement.PathArguments((game.World.Content.Get<ILineDefinition>("network.pipe")).Layer.Space.Id,new[] { new TileCoord(10, 28) }))).IsNone, "ordinary topology removal physically disconnects source");
            Tick(game); Advance(game, 120); c.That(Fact(game, source.Id, Steel(game)) == null, "disconnected source has no elapsed-service credit");
            c.Equal(Fix.FromInt(30), source.Get<IInventory>().Amount(Steel(game)), "disconnected time moves no whole source stock");
            Geometry(game); Advance(game, 59); c.Equal(Fix.Zero, Pool(game).Amount(Steel(game)), "reconnected source must earn a new interval");
            Tick(game); c.Equal(Fix.One, Pool(game).Amount(Steel(game)), "physical reconnection restores ordinary slow flow without catch-up");

            factory = Factory(root => root.Get("networks")[0].Get("rates")[0].Get("period").Set("denominator", JsonValue.Number("2")));
            game = New(factory); source = Spawn(game, "rate.source", 9); Geometry(game); Advance(game, 60);
            c.Equal(Fix.FromInt(2), Pool(game).Amount(Steel(game)), "explicit rational half-hour period gives two actual whole units per hour");
        }
    }
}
