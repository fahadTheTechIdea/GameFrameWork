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
    // Extends the two existing directly affected suites; no independent test registration or gameplay loop.
    internal static class NetworkSignalTests
    {
        private const string Network = "arbitration.main";
        private static readonly Code PoolId = new Code("pool");
        private static string Text(JsonValue value) { var text = new StringBuilder(); value.WriteCanonical(text); return text.ToString(); }
        internal static GameFactory Factory(Action<JsonValue> edit = null, bool publicNeutral = false, bool neutralPrivate = false)
        {
            JsonValue network = Json.Parse(FrameworkProofContent.Read("network-arbitration.json"));
            var net = network.Get("networks")[0]; net.Set("capacity_policy", JsonValue.Of("network.shared"));
            net.Get("rates")[0].Set("quantity", JsonValue.Number("4"));
            net.Get("rates")[0].Get("period").Set("numerator", JsonValue.Number("1"));
            if (publicNeutral || neutralPrivate) { net.Set("line", JsonValue.Of("network.public-pipe")); net.Set("public", JsonValue.Of(publicNeutral)); }
            JsonValue signals = Json.Parse(FrameworkProofContent.Read("network-signals.json")); edit?.Invoke(signals);
            return PhysicalRecoveryTestContent.Factory(infrastructure: FrameworkProofContent.Read("network-transfers.json"),
                edit: root => root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(AutomationModule.ModuleId)),
                additionalDocuments: new Dictionary<string, string>
                { ["network-arbitration.json"] = Text(network), ["network-signals.json"] = Text(signals),
                  ["input-recovery.json"] = FrameworkProofContent.Read("input-recovery.json") },
                additionalModules: new GameModule[] { new AutomationModule() },
                moduleDeclarations: new[] { "{\"id\":\"sim.automation\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[]}" });
        }
        internal static SimGame Setup(GameFactory factory, out Entity source, out Entity watcher, out Entity independent, bool publicNeutral = false)
        {
            SimGame game = ResourceDispositionTests.New(factory);
            source = ResourceDispositionTests.Spawn(game, "pool.source", 9, 28);
            watcher = ResourceDispositionTests.Spawn(game, "pool.watcher", 11, 28);
            independent = ResourceDispositionTests.Spawn(game, "pool.independent", 10, 27);
            string line = publicNeutral ? "network.public-pipe" : "network.pipe";
            if (!((Beep.SimEngine.Maps.TileLineGeometry)Lines(game).World.Services.Get<ILineGeometry>()).Stamp(game.World.Content.Get<ILineDefinition>(line), new TileCoord(10, 28), new PlayerId(0)))
                throw new InvalidOperationException("authored signal fixture line refused");
            return game;
        }
        private static NetworkService Lines(SimGame game) => game.World.Services.Providers<NetworkService>().Single();
        private static INetworkDiagnostics Diagnostics(SimGame game) => game.World.Services.Get<INetworkDiagnostics>();
        private static ResourceId Steel(SimGame game) => game.World.Content.ResourceId("steel");
        private static OperationsView View(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<OperationsView>();
        private static AutomationSignalSnapshot Read(SimGame game, EntityId entity, string id = "pool.stored")
        {
            Code refusal = View(game).ReadSignal(entity, PoolId, id, out var facts);
            if (!refusal.IsNone) throw new InvalidOperationException("network signal view refused " + refusal); return facts;
        }
        private static Fix Amount(SimGame game, EntityId entity, string signal = "pool.stored")
        {
            var facts = Read(game, entity, signal);
            if (!facts.Available) throw new InvalidOperationException("network quantity unavailable " + facts.Blocker); return facts.Value.Value;
        }
        private static void Update(SimGame game) { game.Update(); if (game.Fault != null) throw new InvalidOperationException("network signal game update " + game.Tick + ": " + game.Fault); }
        private static void Advance(SimGame game, int count, SimGame twin = null)
        { for (int i = 0; i < count; i++) { Update(game); if (twin != null) Update(twin); } }
        private static AutomationRuleSnapshot Rule(SimGame game)
        { if (!View(game).TryGetAutomationRule(new PlayerId(0), "fill", out var rule)) throw new InvalidOperationException("missing fill rule"); return rule; }
        private static bool Enabled(SimGame game, EntityId source)
        { if (!View(game).TryGetControl(source, new Code("valve"), out var operation)) throw new InvalidOperationException("missing actual valve"); return operation.Enabled; }

        internal static void Contracts(Check c)
        {
            GameFactory factory = Factory(); SimGame game = Setup(factory, out Entity source, out Entity watcher, out Entity independent);
            c.That(ReferenceEquals(Lines(game), Diagnostics(game)), "read contract aliases sole network owner");
            c.That(independent.Get<INetworkPorts>() is ProofNetworkPorts, "independent public-only port participates without concrete lookup");
            ulong hash; long currentTime = game.Tick;
            Entity remote = ResourceDispositionTests.Spawn(game, "pool.watcher", 35, 28);
            var pending = Read(game, watcher.Id);
            c.That(!pending.Available && !pending.Value.HasValue && pending.Blocker == NetworkService.TopologyPending,
                "dirty topology never substitutes an old pool or fake zero");
            c.Equal(currentTime, game.Tick, "dirty connection read never advances game time");
            c.That(Read(game, watcher.Id).Blocker == NetworkService.TopologyPending && Lines(game).ComponentCount(Network) == 0,
                "pure observation never rebuilds pending topology");
            Update(game);
            foreach (Entity entity in new[] { source, watcher, independent })
            {
                c.Equal(Fix.FromInt(4), Amount(game, entity.Id), "actual connected pool, not local storage " + entity.Def.Id);
                c.Equal(Fix.FromInt(2), Amount(game, entity.Id, "pool.pairs"), "same exact scaled output from actual connected pool");
                c.Equal(Fix.Zero, Amount(game, entity.Id, "pool.inbound"), "known zero inbound is an available fact");
                c.Equal(Fix.FromInt(6), Amount(game, entity.Id, "pool.room"), "actual connected receiving capacity " + entity.Def.Id);
                c.Equal(Fix.FromInt(3), Amount(game, entity.Id, "pool.room-pairs"), "exact scaled connected receiving capacity " + entity.Def.Id);
                var incompatible = Read(game, entity.Id, "pool.water");
                c.That(!incompatible.Available && incompatible.Blocker == ResourceSignalCodes.Incompatible,
                    "non-carried resource cannot report fabricated zero");
            }
            var snapshots = new List<NetworkPoolView>();
            c.Equal(Code.None, game.Seat(0).View.Feature<InfrastructureView>().CopyPools(Network, snapshots), "view delegates to network diagnostic contract");
            c.Equal(1, snapshots.Count, "own company reads exactly its connected pool"); var retained = snapshots[0];
            c.Equal(Fix.FromInt(4), retained.Stock[0].Amount, "detached pool contains actual transfer");
            c.Throws<NotSupportedException>(() => ((IList<ResourceAmount>)retained.Stock).Clear(), "pool snapshot collections are immutable");
            c.Equal(Codes.NotOwned, View(game, 1).ReadSignal(watcher.Id, PoolId, "pool.stored", out var hidden), "foreign company's signal is withheld");
            c.That(!hidden.Value.HasValue, "private refusal contains no quantity");
            c.Equal(Code.None, game.Seat(1).View.Feature<InfrastructureView>().CopyPools(Network, snapshots), "known network has private pool filtering");
            c.Equal(0, snapshots.Count, "foreign company sees no owned pool facts");
            c.Equal(Codes.NotFound, Diagnostics(game).ReadQuantity(watcher.Id, Network, new PlayerId(1), Steel(game), out _), "engine-side diagnostics enforce connection ownership");
            var disconnected = Read(game, remote.Id);
            c.That(!disconnected.Available && disconnected.Blocker == NetworkService.Disconnected, "declared distant connection is unavailable, not zero");
            c.Equal(Codes.UnknownDef, Diagnostics(game).ReadQuantity(watcher.Id, "missing", new PlayerId(0), Steel(game), out _), "missing graph is not substituted");
            c.Equal(Codes.UnknownDef, Diagnostics(game).ReadQuantity(watcher.Id, Network, new PlayerId(0), ResourceId.None, out _), "invalid resource has no fabricated quantity");
            c.Equal(Codes.InvalidArgument, Diagnostics(game).ReadQuantity(watcher.Id, "network.main", new PlayerId(0), Steel(game), out _), "no undeclared network fallback");

            var pool = Lines(game).Component(Network, 0).Inventory; var holder = new ReservationOwner("proof.pool-facts", 1);
            ReservationToken outgoing = pool.ReserveOut(holder, Steel(game), Fix.FromInt(3));
            ReservationToken incoming = pool.ReserveIn(holder, Steel(game), Fix.FromInt(2));
            c.That(outgoing.IsValid && incoming.IsValid, "real pool receipt owners protect stock and capacity");
            c.Equal(Fix.One, Amount(game, independent.Id, "pool.available"), "actual pool outbound receipts reduce available quantity");
            c.Equal(Fix.FromInt(3), Amount(game, source.Id, "pool.outbound"), "actual pool outbound promises observed");
            c.Equal(Fix.FromInt(2), Amount(game, watcher.Id, "pool.inbound"), "actual pool inbound promises observed");
            c.Equal(Fix.FromInt(4), Amount(game, independent.Id, "pool.room"), "inbound promises reduce connected room; outbound never frees it");
            c.Equal(Fix.FromInt(4), retained.Stock[0].Amount, "retained snapshot does not follow later promises");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "no signal-owned pool or receipt codec");
            c.Equal(Fix.One, Amount(twin, independent.Id, "pool.available"), "restored actual promise owner derives same facts");
            c.Equal(Fix.FromInt(4), Amount(twin, independent.Id, "pool.room"), "saved pool promises derive exact receiving room");
            pool.ReleaseOut(holder, outgoing); pool.ReleaseIn(holder, incoming);
            c.Equal(Fix.FromInt(4), Amount(game, independent.Id, "pool.available"), "release returns availability, not duplicated stock");
            c.Equal(Fix.FromInt(6), Amount(game, independent.Id, "pool.room"), "release restores real connected capacity");
            c.Equal(Fix.One, Amount(twin, independent.Id, "pool.available"), "session-local receipt changes cannot affect saved twin");
            hash = game.Hash(); for (int i = 0; i < 8; i++) Read(game, independent.Id);
            c.Equal(hash, game.Hash(), "repeated facts are pure, without signal state or extra time");
            var foreignDefinition = game.World.Content.Get<AutomationSignalDef>("pool.stored");
            c.Throws<ArgumentException>(() => independent.Get<NetworkSignal>().ReadSignal(twin.World, foreignDefinition), "cross-session signal handles refuse");

            foreach (string invalid in new[] { "network", "unknown", "unsupported", "id", "schema", "metric", "dimension", "extra" })
                c.Throws<ContentException>(() => ResourceDispositionTests.New(Factory(root =>
                {
                    var sourceDef = root.Get("entities")[0].Get("components").Get("network_signal");
                    var quantity = root.Get("resource_signals")[0];
                    switch (invalid)
                    {
                        case "network": sourceDef.Remove("network"); break;
                        case "unknown": sourceDef.Set("network", JsonValue.Of("absent")); break;
                        case "unsupported": sourceDef.Set("network", JsonValue.Of("network.main")); break;
                        case "id": sourceDef.Set("id", JsonValue.Of("")); break;
                        case "schema": quantity.Get("providers")[0].Set("schema", JsonValue.Number("1")); break;
                        case "metric": quantity.Set("metric", JsonValue.Of("network.fake")); break;
                        case "dimension": quantity.Set("unit", JsonValue.Of("unit.water")); break;
                        case "extra": sourceDef.Set("pool_id", JsonValue.Number("1")); break;
                    }
                })), "invalid network signal authoring refuses before play " + invalid);

            var hostile = independent.Get<ProofNetworkPorts>(); hostile.ReadProbe = () => independent.Touch();
            c.Throws<InvalidOperationException>(() => Read(game, independent.Id), "mutating connection provider cannot publish facts"); hostile.ReadProbe = null;
            hostile.ReadProbe = () => ((Beep.SimEngine.Maps.TileLineGeometry)Lines(game).World.Services.Get<ILineGeometry>()).Stamp(game.World.Content.Get<ILineDefinition>("network.pipe"), new TileCoord(10, 29), new PlayerId(0));
            c.Throws<InvalidOperationException>(() => Read(game, independent.Id), "connection callback cannot silently dirty another owner"); hostile.ReadProbe = null;

            factory = Factory(publicNeutral: true); game = Setup(factory, out source, out watcher, out independent, true); Update(game);
            Entity other = ResourceDispositionTests.Spawn(game, "pool.watcher", 10, 29, 1);
            c.Equal(Code.None, View(game, 1).ReadSignal(other.Id, PoolId, "pool.stored", out var publicFact), "public neutral pool serves another company's own connection");
            c.Equal(Fix.FromInt(4), publicFact.Value.Value, "public network exposes the actual neutral pool, not local zero");
            c.Equal(Code.None, game.Seat(1).View.Feature<InfrastructureView>().CopyPools(Network, snapshots), "public pool diagnostic admitted");
            c.Equal(1, snapshots.Count, "public neutral pool has same existing ownership policy");
            c.Throws<ArgumentException>(() => new ResourceQuantitySnapshot(ResourceId.None, Fix.Zero, Fix.Zero, Fix.Zero, Fix.Zero, new CapacityOffer(CapacityStatus.Fits)),
                "public diagnostic providers must identify their actual resource");
            c.Throws<ArgumentException>(() => new ResourceQuantitySnapshot(Steel(game), Fix.One, Fix.One, Fix.Zero, Fix.One, new CapacityOffer(CapacityStatus.Fits)),
                "public providers cannot construct incoherent stock and promises");
            foreach (CapacityOffer invalid in new[] { new CapacityOffer((CapacityStatus)255), new CapacityOffer(CapacityStatus.Fits, -Fix.One),
                new CapacityOffer(CapacityStatus.LimitExceeded, Fix.One) })
                c.Throws<ArgumentException>(() => new ResourceQuantitySnapshot(Steel(game), Fix.Zero, Fix.Zero, Fix.Zero, Fix.Zero, invalid),
                    "public providers cannot publish malformed receiving capacity");
            var mutable = new List<ResourceAmount> { new ResourceAmount(Steel(game), Fix.One) };
            var detached = new NetworkPoolView(snapshots[0].Endpoint, PlayerId.None, 0, 1, false, mutable,
                Array.Empty<ResourceAmount>(), Array.Empty<ResourceAmount>(), snapshots[0].Capacity);
            mutable.Clear(); c.Equal(1, detached.Stock.Count, "public snapshot constructor defensively detaches provider collections");
            long before = game.Tick;
            c.That(((Beep.SimEngine.Maps.TileLineGeometry)Lines(game).World.Services.Get<ILineGeometry>()).Stamp(game.World.Content.Get<ILineDefinition>("network.public-pipe"), new TileCoord(10, 29), new PlayerId(0)),
                "ordinary geometry change marks connected graph dirty");
            c.That(Read(game, independent.Id).Blocker == NetworkService.TopologyPending, "changed graph cannot reuse previous connected pool fact");
            c.Equal(before, game.Tick, "topology diagnostics never pump another clock");
            Update(game); c.That(Read(game, independent.Id).Available, "normal engine update alone settles changed connection");
            factory = Factory(neutralPrivate: true); game = Setup(factory, out source, out watcher, out independent, true); Update(game);
            var privateNeutral = Read(game, watcher.Id);
            c.That(!privateNeutral.Available && privateNeutral.Blocker == Codes.NotOwned,
                "neutral ownership alone never makes a private network's pool public");
            c.Equal(Code.None, game.Seat(0).View.Feature<InfrastructureView>().CopyPools(Network, snapshots), "known private neutral network is filtered");
            c.Equal(0, snapshots.Count, "private neutral pool stock is withheld from company snapshots");
        }

        internal static void Execution(Check c)
        {
            GameFactory factory = Factory(); SimGame game = Setup(factory, out Entity source, out Entity watcher, out Entity independent);
            var settings = new AutomationSettings("pool.fill", true, new ControlBinding(source.Id, new Code("valve")),
                new Dictionary<string, ControlBinding> { ["stock"] = new ControlBinding(independent.Id, PoolId) }, ControlThresholds.Initial(game.World.Content.Get<IControlRule>("pool.fill")),
                game.World.Content.Get<IControlRule>("pool.fill").WhenTrue, game.World.Content.Get<IControlRule>("pool.fill").WhenFalse);
            c.That(TestKit.Order(game, 0, new ConfigureAutomationCommand { Rule = "fill", Settings = settings }).Accepted,
                "ordinary authored rule binds independent connected signal to actual valve");
            c.Equal(Fix.FromInt(4), Amount(game, watcher.Id), "flow occurs in existing declared network phase");
            c.That(Rule(game).Blocker == NetworkService.TopologyPending && !Rule(game).PendingDecision.HasValue,
                "unsettled connection suspends existing rule with a typed blocker, never a default stock action");
            c.That(TestKit.Order(game, 0, new ConfigureAutomationCommand { Rule = "fill", Settings = settings }).Accepted,
                "ordinary player reconfiguration resumes suspended rule after topology settles");
            SimGame twin = factory.Load(game.Save()); Advance(game, 1, twin);
            c.That(Rule(game).PendingDecision.HasValue && Rule(game).PendingDecision.Value, "high actual pool stock queues existing rule's true action");
            c.That(Enabled(game, source.Id), "queued decision cannot change valve in same game step");
            c.Equal(Fix.FromInt(10), Amount(game, watcher.Id), "last physical inlet admission respects actual capacity");
            Advance(game, 1, twin);
            c.That(!Enabled(game, source.Id), "next game update applies normal operation command and closes physical gate");
            c.Equal(Fix.FromInt(20), game.World.Entities.Get(source.Id).Get<IInventory>().Amount(Steel(game)), "only ten source units entered actual pool");
            c.Equal(game.Hash(), twin.Hash(), "saved pending control and pool state continue identically");
            var gate = game.Seat(0).View.Feature<InfrastructureView>(); gate.ReadGates(source.Id, Network, out var gateFacts);
            c.That(!gateFacts.Blocker.IsNone, "normal valve state blocks actual network gate");
            long time = game.Tick; ulong hash = game.Hash(); game.Clock.Pause(); game.Update(); game.Update();
            c.Equal(time, game.Tick, "single paused clock freezes network signal/control execution");
            c.Equal(hash, game.Hash(), "paused observations create no independent elapsed time"); game.Clock.Resume();
            Advance(game, 2, twin); c.Equal(Fix.FromInt(10), Amount(game, watcher.Id), "closed valve transfers no more stock");

            // Actual transfer protocol, not test-owned stock subtraction or a synthetic signal override.
            var endpoint = Lines(game).Component(Network, 0).Endpoint;
            var movement = new ResourceMovementIntent(new ReservationOwner("proof.pool-withdraw", 1), new PlayerId(0), endpoint,
                ResourceEndpointId.ForEntity(watcher.Id), new Bill(new[] { new ResourceAmount(Steel(game), Fix.FromInt(6)) }), Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain);
            c.That(game.World.Services.Get<IResourceTransaction>().CommitMovement(movement).Committed, "normal existing receipt protocol physically withdraws six pool units");
            c.Equal(Fix.FromInt(4), Amount(game, independent.Id), "actual lower pool stock crosses authored hysteresis threshold");
            Update(game); twin = factory.Load(game.Save());
            c.That(!Enabled(game, source.Id) && Rule(game).PendingDecision.HasValue && !Rule(game).PendingDecision.Value, "restart is deferred, not same-step feedback");
            Advance(game, 1, twin); c.That(Enabled(game, source.Id), "existing false action reopens physical valve");
            c.Equal(Fix.FromInt(8), Amount(game, watcher.Id), "reopened valve earns only its next normal physical allowance");
            Fix sourceStock = game.World.Entities.Get(source.Id).Get<IInventory>().Amount(Steel(game));
            Fix delivered = game.World.Entities.Get(watcher.Id).Get<IInventory>().Amount(Steel(game));
            c.Equal(Fix.FromInt(30), sourceStock + delivered + Amount(game, watcher.Id), "pool control never creates or duplicates any resource");
            c.Equal(game.Hash(), twin.Hash(), "restored physical withdrawal/restart uses existing single time and stock owners");
            Backpressure(c); DeferredBackpressure(c);
        }
        private static void Backpressure(Check c)
        {
            GameFactory factory = Factory(root => root.Get("entities")[0].Get("components").Get("signal_network_gate").Get("conditions").Add(Json.Parse(
                "{\"id\":\"receiving\",\"source\":\"pool\",\"signal\":\"pool.room\",\"minimum\":4,\"maximum\":10,\"unit\":\"unit.steel\",\"blocker\":\"network.backpressure\"}")));
            SimGame game = Setup(factory, out Entity source, out Entity watcher, out Entity independent);
            Advance(game, 2);
            c.Equal(Fix.FromInt(8), Amount(game, watcher.Id), "authored room gate admits two real rate-limited deliveries");
            SimGame twin = factory.Load(game.Save()); Advance(game, 3, twin);
            c.Equal(Fix.FromInt(8), Amount(game, independent.Id), "backpressure stops real flow before a partial remaining slot");
            c.Equal(game.Hash(), twin.Hash(), "saved backpressure wait has no separate controller or progress owner");
            game.Seat(0).View.Feature<InfrastructureView>().ReadGates(source.Id, Network, out var facts);
            c.Equal(new Code("network.backpressure"), facts.Blocker, "engine view publishes actual authored physical blocker");
            long time = game.Clock.StepIndex; ulong hash = game.Hash(); game.Clock.Pause(); game.Update(); game.Update();
            c.Equal(time, game.Clock.StepIndex, "one paused game clock freezes backpressure execution");
            c.Equal(hash, game.Hash(), "paused room gates acquire no independent time/state"); game.Clock.Resume();
            var holder = new ReservationOwner("proof.backpressure", 1);
            foreach (SimGame session in new[] { game, twin })
            {
                var pool = Lines(session).Component(Network, 0).Inventory;
                c.That(pool.ReserveIn(holder, Steel(session), Fix.FromInt(2)).IsValid, "real inbound commitment fills remaining room");
                c.Equal(Fix.Zero, Amount(session, independent.Id, "pool.room"), "reserved full capacity is known zero");
                var movement = new ResourceMovementIntent(holder, new PlayerId(0), pool.Endpoint, ResourceEndpointId.ForEntity(watcher.Id),
                    new Bill(new[] { new ResourceAmount(Steel(session), Fix.FromInt(4)) }), Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain);
                c.That(session.World.Services.Get<IResourceTransaction>().CommitMovement(movement).Committed, "normal receipts physically drain pool");
                c.Equal(Fix.FromInt(4), Amount(session, independent.Id, "pool.room"), "withdrawal retains inbound commitment in real room");
            }
            Advance(game, 1, twin);
            c.Equal(Fix.FromInt(8), Amount(game, watcher.Id), "reopened gate earns only one update allowance, never accumulated blocked work");
            c.Equal(Fix.Zero, Amount(game, independent.Id, "pool.room"), "new physical delivery plus existing commitment exhausts exact room");
            c.Equal(Fix.FromInt(30), source.Get<IInventory>().Amount(Steel(game)) + watcher.Get<IInventory>().Amount(Steel(game)) + Amount(game, watcher.Id),
                "backpressure never creates or destroys material");
            c.Equal(game.Hash(), twin.Hash(), "interleaved saved sessions retain sole clock/receipt/flow owners");
        }
        private static void DeferredBackpressure(Check c)
        {
            GameFactory factory = Factory(root =>
            {
                var rule = root.Get("control_rules")[0]; rule.Get("inputs")[0].Set("signal", JsonValue.Of("pool.room"));
                var condition = rule.Get("conditions")[0]; condition.Set("kind", JsonValue.Of("below"));
                condition.Set("on", JsonValue.Number("3")); condition.Set("off", JsonValue.Number("6"));
            });
            SimGame game = Setup(factory, out Entity source, out Entity watcher, out Entity independent); Update(game);
            IControlRule profile = game.World.Content.Get<IControlRule>("pool.fill");
            var settings = new AutomationSettings(profile.Id, true, new ControlBinding(source.Id, new Code("valve")),
                new Dictionary<string, ControlBinding> { ["stock"] = new ControlBinding(independent.Id, PoolId) },
                ControlThresholds.Initial(profile), profile.WhenTrue, profile.WhenFalse);
            c.That(TestKit.Order(game, 0, new ConfigureAutomationCommand { Rule = "fill", Settings = settings }).Accepted,
                "ordinary automation binds actual receiving-room metric without a new controller");
            c.Equal(Fix.FromInt(8), Amount(game, watcher.Id), "flow still uses actual rate after normal configuration");
            Update(game);
            c.That(Rule(game).PendingDecision.HasValue && Rule(game).PendingDecision.Value, "low actual room queues deferred shutdown");
            c.That(Enabled(game, source.Id), "room decision never commits in its observation update");
            SimGame twin = factory.Load(game.Save()); Advance(game, 1, twin);
            c.That(!Enabled(game, source.Id), "next update applies normal valve shutdown");
            c.Equal(Fix.Zero, Amount(game, independent.Id, "pool.room"), "full actual pool exposes known zero after shutdown");
            c.Equal(game.Hash(), twin.Hash(), "saved pending room decision applies identically");
            foreach (SimGame session in new[] { game, twin })
            {
                var pool = Lines(session).Component(Network, 0).Inventory;
                var movement = new ResourceMovementIntent(new ReservationOwner("proof.room-restart", 1), new PlayerId(0), pool.Endpoint,
                    ResourceEndpointId.ForEntity(watcher.Id), new Bill(new[] { new ResourceAmount(Steel(session), Fix.FromInt(6)) }),
                    Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain);
                c.That(session.World.Services.Get<IResourceTransaction>().CommitMovement(movement).Committed, "real physical withdrawal earns room, not a signal override");
            }
            Advance(game, 1, twin);
            c.That(!Enabled(game, source.Id) && Rule(game).PendingDecision.HasValue && !Rule(game).PendingDecision.Value,
                "hysteresis restart at six room is also deferred");
            Advance(game, 1, twin);
            c.That(Enabled(game, source.Id), "normal command reopens actual operation after receiving room returns");
            c.Equal(Fix.FromInt(8), Amount(game, watcher.Id), "reopened operation transfers only its earned update allowance");
            c.Equal(Fix.FromInt(30), source.Get<IInventory>().Amount(Steel(game)) + watcher.Get<IInventory>().Amount(Steel(game)) + Amount(game, watcher.Id),
                "deferred room rules conserve source, pool and delivered stock");
            c.Equal(game.Hash(), twin.Hash(), "saved hysteresis/receipts/control continue with the one game clock");
        }
    }
}
