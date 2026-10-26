using System.Linq;
using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class NetworkTopologyTests
    {
        public static void Register()
        {
            TestRunner.Add("Network topology: native splits, merges, capacity and removal refusals", c => Native(c, false));
            TestRunner.Add("Network topology: saved multi-network ownership and current-state execution", c => Durable(c, false));
            TestRunner.Add("Network topology: independent tile splits, merges, capacity and removal refusals", c => Native(c, true));
            TestRunner.Add("Network topology: independent tile saved ownership and current-state execution", c => Durable(c, true));
            TestRunner.Add("Topology lifecycle: repeated saved splits merges preserve complete stock and receipt authority", Lifecycle);
        }
        private static GameFactory Factory(string networks = null, bool independent = false, bool half = false)
        {
            var modules = new List<GameModule> {
                new ContentAccessModule(), new InventoryModule(), new MotionModule(), new TileNavigationModule(), new WorkModule(), new InfrastructureModule() };
            var master = Json.Parse(FrameworkProofContent.Read("network-topology-master.json"));
            var world = Json.Parse(FrameworkProofContent.Read("network-topology-world.json"));
            if (half) world.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
            if (independent) {
                modules.Add(new IndependentTileSensorModule());
                master.Get("modules").Add(Json.Parse("{\"id\":\"proof.tile-sensor-space\",\"version\":\"1.0.0\",\"documents\":[]}"));
                world.Set("sensor_tile_spaces", world.Get("tile_spaces")); world.Remove("tile_spaces");
                world.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.tile-sensor-space"));
            }
            var definition = new GameDefinition("network-topology", modules);
            var files = new PreloadedFileSource().Add("manifest.json", master.ToString())
                .Add("network-topology-world.json", world.ToString())
                .Add("network-topology-navigation.json", FrameworkProofContent.Read("network-topology-navigation.json"))
                .Add("network-topology.json", networks ?? FrameworkProofContent.Read("network-topology.json"))
                .Add("work-retry.json", FrameworkProofContent.Read("work-retry.json"))
                .Add("open-access.json", FrameworkProofContent.Access);
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "colony", Seed = 97 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human);
            return factory.NewGame(request);
        }
        private static void Tick(SimGame game)
        { game.Update(); if (game.Fault != null) throw new InvalidOperationException("topology tick " + game.Tick + ": " + game.Fault); }
        private static List<TileCoord> Tiles(int first = 10, int last = 14)
        { var result = new List<TileCoord>(); for (int x = first; x <= last; x++) result.Add(new TileCoord(x, 28)); return result; }
        private static NetworkService Setup(SimGame game)
        {
            NetworkService lines = game.World.Services.Providers<NetworkService>().Single(); ILineDefinition line = game.World.Content.Get<ILineDefinition>("network.pipe");
            foreach (TileCoord tile in Tiles()) if (!Stamp(game, line, tile)) throw new InvalidOperationException("fixture stamping refused");
            Tick(game); return lines;
        }
        // Explicit protocol stock setup; journal-backed finite transfers and physical players have separate proofs.
        private static void Stock(SimGame game, string network, string resource, Fix quantity)
            => game.World.Services.Providers<NetworkService>().Single().Component(network, 0).Inventory.Add(game.World.Content.ResourceId(resource), quantity);
        private static Fix Total(NetworkService lines, string network, ResourceId resource)
        { Fix result = Fix.Zero; for (int i = 0; i < lines.ComponentCount(network); i++) result += lines.Component(network, i).Amount(resource); return result; }
        private static bool Stamp(SimGame game, ILineDefinition line, TileCoord tile)
            => game.World.Services.Get<ILineGeometry>().Stamp(game.World, new PlayerId(0), line,
                ((ISpatialPlacementIntent)game.World.Space).ReadPlacement(TilePlacement.Arguments(line.Layer.Space.Id, tile))).IsNone;
        private static ITileWritableSampleLayer Layer(SimGame game, string id)
            => TileSpatialGeometry.RequireLayer<ITileWritableSampleLayer>(game.World, (ITileSpatialSpace)game.World.Space,
                game.World.Content.Get<ISpatialLayerDefinition>(id), false);
        private static IReadOnlyList<SpatialPlacement> Path(SimGame game, ILineDefinition line, IReadOnlyList<TileCoord> tiles)
            => LineDeclaration.ReadPath(game.World, line.Layer.Space.Id, TilePlacement.PathArguments(line.Layer.Space.Id, tiles));
        private static void AssertHeld(Check c, GameFactory factory, SimGame game, NetworkService lines, ILineDefinition line,
            IReadOnlyList<SpatialPlacement> path, string network, int component, ResourceId resource, bool incoming, bool adding, int cycle)
        {
            var pool = lines.Component(network, component).Inventory;
            var owner = new ReservationOwner("proof.topology-lifecycle", cycle + 1);
            Fix amount = Fix.Min(Fix.One, pool.Amount(resource));
            ReservationToken token = incoming ? pool.ReserveIn(owner, resource, amount) : pool.ReserveOut(owner, resource, amount);
            ulong before = game.Hash(); string context = network + "/part=" + component + "/resource=" + resource.Value + "/in=" + incoming + "/adding=" + adding;
            c.Equal(ResourceTransactions.ActivePromises, adding ? game.World.Services.Get<INetworkTopology>().CheckAddition(new PlayerId(0), line, path)
                : game.World.Services.Get<INetworkTopology>().CheckRemoval(new PlayerId(0), line, path), "actual changed participant refuses held receipt: " + context);
            if (!adding) {
                c.Equal(ResourceTransactions.ActivePromises, game.Seat(0).View.Feature<InfrastructureView>().PreviewRemoval(line.Id, path), "player preview shares live-receipt refusal: " + context);
                c.Equal(ResourceTransactions.ActivePromises, game.World.Services.Get<INetworkTopology>().Remove(new PlayerId(0), line, path), "execution preserves complete original topology: " + context);
            }
            c.Equal(before, game.Hash(), "refusal changes no stock/receipt/identity/geometry: " + context);
            var twin = factory.Load(game.Save()); c.Equal(before, twin.Hash(), "complete claimed topology checkpoint exact: " + context);
            var savedPool = (IResourceOutlet)twin.World.Services.Providers<NetworkService>().Single().Resolve(pool.Endpoint);
            var check = incoming ? savedPool.CheckReservedIn(twin.World.Content, owner, token) : savedPool.CheckReservedOut(owner, token);
            c.That(check.Ready && check.Record.Token.Equals(token) && check.Record.Owner.Equals(owner) && check.Record.Amount == amount && check.Record.Resource == resource,
                "original saved receipt retains exact authority: " + context);
            if (incoming) pool.ReleaseIn(owner, token); else pool.ReleaseOut(owner, token);
            c.That((incoming ? pool.CheckReservedIn(game.World.Content, owner, token) : pool.CheckReservedOut(owner, token)).Status == ResourceTransferStatus.InvalidReceipt,
                "only explicit release retires selected authority: " + context);
        }
        private static void Lifecycle(Check c)
        {
            foreach (bool independent in new[] { false, true }) foreach (bool half in new[] { false, true })
            {
                var factory = Factory(independent: independent, half: half); var game = New(factory); var lines = Setup(game);
                var idle = New(factory); ulong isolated = idle.Hash(); var line = game.World.Content.Get<ILineDefinition>("network.pipe");
                var steel = game.World.Content.ResourceId("steel"); var water = game.World.Content.ResourceId("water");
                string[] networks = { "network.main", "network.secondary" }; ResourceId[] resources = { steel, water };
                Stock(game, networks[0], "steel", Fix.FromInt(11)); Stock(game, networks[0], "water", Fix.FromRatio(11, 2));
                Stock(game, networks[1], "steel", Fix.FromInt(2)); Stock(game, networks[1], "water", Fix.FromRatio(3, 2));
                var expected = networks.ToDictionary(x => x, x => resources.ToDictionary(r => r, r => Total(lines, x, r)));
                var seen = new HashSet<ResourceEndpointId>(); foreach (string network in networks) seen.Add(lines.Component(network, 0).Endpoint);
                var middle = Path(game, line, new[] { new TileCoord(12, 28) });
                for (int cycle = 0; cycle < 8; cycle++) {
                    foreach (string network in networks) foreach (ResourceId resource in resources) foreach (bool incoming in new[] { false, true })
                        AssertHeld(c, factory, game, lines, line, middle, network, 0, resource, incoming, false, cycle);
                    var retired = networks.Select(x => lines.Component(x, 0)).ToArray();
                    c.Equal(Code.None, game.World.Services.Get<INetworkTopology>().Remove(new PlayerId(0), line, middle), "released whole set permits actual split"); Tick(game);
                    foreach (var prior in retired) c.That(prior.Pool.Count == 0 && prior.Inventory.Reservations.Count == 0 && lines.Resolve(prior.Endpoint) == null,
                        "retired actual endpoint drains and no longer resolves");
                    foreach (string network in networks) {
                        c.Equal(2, lines.ComponentCount(network), "each actual network splits into two");
                        for (int part = 0; part < 2; part++) {
                            c.That(seen.Add(lines.Component(network, part).Endpoint), "new split endpoint never recycles an earlier identity");
                            foreach (ResourceId resource in resources) foreach (bool incoming in new[] { false, true })
                                AssertHeld(c, factory, game, lines, line, middle, network, part, resource, incoming, true, cycle);
                        }
                        foreach (ResourceId resource in resources) c.Equal(expected[network][resource], Total(lines, network, resource), "complete native stock vector conserved through split");
                    }
                    var twin = factory.Load(game.Save()); long time = game.Clock.StepIndex;
                    game.Clock.Pause(); twin.Clock.Pause(); Tick(game); Tick(twin); c.Equal(time, game.Clock.StepIndex, "sole clock pause does not advance topology");
                    game.Clock.Resume(); twin.Clock.Resume();
                    c.Equal(Code.None, game.World.Services.Get<INetworkTopology>().CheckAddition(new PlayerId(0), line, middle), "released split components allow real reconnect");
                    c.That(Stamp(game, line, new TileCoord(12, 28)), "original geometry provider reconnects");
                    c.That(Stamp(twin, twin.World.Content.Get<ILineDefinition>(line.Id), new TileCoord(12, 28)), "saved original provider reconnects identically");
                    Tick(game); Tick(twin); c.Equal(game.Hash(), twin.Hash(), "saved split merges and continues identically");
                    foreach (string network in networks) {
                        c.Equal(1, lines.ComponentCount(network), "actual merge uses one original stock owner");
                        c.That(seen.Add(lines.Component(network, 0).Endpoint), "merged endpoint identity never recycles");
                        c.Equal(0, lines.Component(network, 0).Inventory.Reservations.Count, "every explicitly released receipt remains retired");
                        foreach (ResourceId resource in resources) c.Equal(expected[network][resource], Total(lines, network, resource), "complete stock vector conserved through saved merge");
                    }
                }
                var all = Path(game, line, Tiles()); ulong before = game.Hash();
                c.Equal(NetworkService.OccupiedNetwork, game.World.Services.Get<INetworkTopology>().Remove(new PlayerId(0), line, all), "nonempty final disappearance refuses without explicit disposition");
                c.Equal(before, game.Hash(), "occupied final refusal pure");
                var inputs = new List<ResourceConversionInput>(); var disposer = new ReservationOwner("proof.topology-disposition", 1);
                foreach (string network in networks) {
                    var pool = lines.Component(network, 0).Inventory;
                    foreach (ResourceId resource in resources) {
                        Fix amount = pool.Amount(resource); var token = pool.ReserveOut(disposer, resource, amount);
                        inputs.Add(new ResourceConversionInput(ResourceFlowKind.Consumed, pool.CheckReservedOut(disposer, token).Record, amount, false));
                    }
                }
                var disposition = new ResourceConversionIntent(disposer, new PlayerId(0), inputs, Array.Empty<ResourceProductionIntent>(), new Code("proof.topology-drain"));
                c.That(game.World.Services.Get<IResourceTransaction>().CommitConversion(disposition, null).Committed, "original whole conversion explicitly consumes all stored native quantities"); Tick(game);
                foreach (string network in networks) foreach (ResourceId resource in resources) c.Equal(Fix.Zero, Total(lines, network, resource), "disposition drains actual owners, not a copied balance");
                c.Equal(Code.None, game.World.Services.Get<INetworkTopology>().Remove(new PlayerId(0), line, all), "empty fully released topology may disappear"); Tick(game);
                foreach (string network in networks) c.Equal(0, lines.ComponentCount(network), "no hidden stocked or claimed component survives final removal");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "final empty state and allocator sequence durable");
                c.Equal(isolated, idle.Hash(), "all lifecycle operations isolated from another session");
            }
        }
        private static void Native(Check c, bool independent)
        {
            GameFactory factory = Factory(independent: independent); SimGame game = New(factory); NetworkService lines = Setup(game);
            c.That(!game.World.Services.Has<IEconomy>() && (independent ? game.World.Space is SensorTileSpace : game.World.Space is TileMap), "explicit resource-only actual topology provider");
            INetworkTopology topology = game.World.Services.Get<INetworkTopology>(); ILineDefinition line = game.World.Content.Get<ILineDefinition>("network.pipe");
            c.That(ReferenceEquals(lines, topology), "topology contract aliases the sole graph and pool owner");
            ResourceId steel = game.World.Content.ResourceId("steel"), water = game.World.Content.ResourceId("water");
            Stock(game, "network.main", "steel", Fix.FromInt(11)); Stock(game, "network.main", "water", Fix.FromRatio(11, 2));
            var middle = new[] { new TileCoord(12, 28) }; ulong before = game.Hash();
            c.That(topology.CheckRemoval(new PlayerId(0), line, LineDeclaration.ReadPath(game.World,(line).Layer.Space.Id,Beep.SimEngine.Maps.TilePlacement.PathArguments((line).Layer.Space.Id,middle))).IsNone, "split fits surviving authored capacity");
            c.That(game.Seat(0).View.Feature<InfrastructureView>().PreviewRemoval(line.Id, LineDeclaration.ReadPath(game.World,(game.World.Content.Get<ILineDefinition>(line.Id)).Layer.Space.Id,Beep.SimEngine.Maps.TilePlacement.PathArguments((game.World.Content.Get<ILineDefinition>(line.Id)).Layer.Space.Id,middle))).IsNone, "immutable view uses identical engine preflight");
            c.Equal(before, game.Hash(), "preview creates no map mutation, pool, ID or history");
            c.That(topology.Remove(new PlayerId(0), line, LineDeclaration.ReadPath(game.World,(line).Layer.Space.Id,Beep.SimEngine.Maps.TilePlacement.PathArguments((line).Layer.Space.Id,middle))).IsNone, "physical provider can execute preflighted topology removal");
            c.Equal(2, lines.ComponentCount("network.main"), "middle removal splits the graph");
            c.Equal(Fix.FromInt(6), lines.Component("network.main", 0).Amount(steel), "stable largest-overlap tie gets the whole discrete remainder");
            c.Equal(Fix.FromInt(5), lines.Component("network.main", 1).Amount(steel), "other share remains a native whole quantity");
            c.Equal(Fix.FromRatio(11, 4), lines.Component("network.main", 0).Amount(water), "continuous quantity keeps its fractional share");
            c.Equal(Fix.FromInt(11), Total(lines, "network.main", steel), "split conserves steel exactly");
            c.Equal(Fix.FromRatio(11, 2), Total(lines, "network.main", water), "split conserves water exactly");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "split native pools survive checkpoint reconstruction");
            c.That(Stamp(game, line, middle[0]), "restored connecting tile is ordinary graph execution"); Tick(game);
            c.Equal(1, lines.ComponentCount("network.main"), "reconnection merges components");
            c.Equal(Fix.FromInt(11), Total(lines, "network.main", steel), "merge conserves discrete remainder");
            c.Equal(Fix.FromRatio(11, 2), Total(lines, "network.main", water), "merge conserves fractional stock");
            before = game.Hash();
            c.Equal(NetworkService.OccupiedNetwork, topology.CheckRemoval(new PlayerId(0), line, LineDeclaration.ReadPath(game.World,(line).Layer.Space.Id,Beep.SimEngine.Maps.TilePlacement.PathArguments((line).Layer.Space.Id,Tiles()))), "last-tile removal requires explicit material disposition");
            c.Equal(NetworkService.OccupiedNetwork, topology.Remove(new PlayerId(0), line, LineDeclaration.ReadPath(game.World,(line).Layer.Space.Id,Beep.SimEngine.Maps.TilePlacement.PathArguments((line).Layer.Space.Id,Tiles()))), "execution refuses the same total pool disappearance");
            c.Equal(before, game.Hash(), "occupied refusal preserves all tiles, owners and stock");

            SimGame capacity = New(factory); NetworkService constrained = Setup(capacity);
            Stock(capacity, "network.main", "steel", Fix.FromInt(45)); before = capacity.Hash();
            c.Equal(NetworkService.NetworkCapacity, capacity.World.Services.Get<INetworkTopology>().Remove(new PlayerId(0), capacity.World.Content.Get<ILineDefinition>(line.Id), LineDeclaration.ReadPath(capacity.World,(capacity.World.Content.Get<ILineDefinition>(line.Id)).Layer.Space.Id,Beep.SimEngine.Maps.TilePlacement.PathArguments((capacity.World.Content.Get<ILineDefinition>(line.Id)).Layer.Space.Id,middle))),
                "five-to-four tile shrink cannot overfill reduced forty-unit capacity");
            c.Equal(before, capacity.Hash(), "capacity refusal commits no candidate changes");
            c.Equal(Fix.FromInt(45), Total(constrained, "network.main", steel), "capacity failure preserves actual prior pool");
            c.Equal(1, constrained.ComponentCount("network.main"), "capacity failure preserves the prior graph");
            ILineDefinition capacityLine = capacity.World.Content.Get<ILineDefinition>(line.Id);
            Stamp(capacity, capacityLine, new TileCoord(15, 28)); before = capacity.Hash();
            c.That(capacity.World.Services.Get<INetworkTopology>().CheckRemoval(new PlayerId(0), capacityLine, LineDeclaration.ReadPath(capacity.World,(capacityLine).Layer.Space.Id,Beep.SimEngine.Maps.TilePlacement.PathArguments((capacityLine).Layer.Space.Id,middle))).IsNone,
                "pending added segment supplies real capacity before the next scheduled rebuild");
            c.Equal(before, capacity.Hash(), "dirty-graph preview never commits its private current or future candidate");
            c.That(capacity.World.Services.Get<INetworkTopology>().Remove(new PlayerId(0), capacityLine, LineDeclaration.ReadPath(capacity.World,(capacityLine).Layer.Space.Id,Beep.SimEngine.Maps.TilePlacement.PathArguments((capacityLine).Layer.Space.Id,middle))).IsNone,
                "execution commits current pending geometry and valid removal together");
            c.Equal(Fix.FromInt(45), Total(constrained, "network.main", steel), "pending-addition removal conserves exact real stock");
            Large(c, independent);
        }
        private static void Large(Check c, bool independent)
        {
            var authored = Json.Parse(FrameworkProofContent.Read("network-topology.json"));
            authored.Get("capacity_policies")[0].Get("limits").Set("unit.storage", JsonValue.Number("300000000"));
            GameFactory factory = Factory(authored.ToString(), independent);
            SimGame game = New(factory); NetworkService lines = Setup(game); Fix amount = Fix.FromRaw(long.MaxValue / 2 + 15);
            Stock(game, "network.main", "water", amount);
            c.Equal(amount, Total(lines, "network.main", game.World.Content.ResourceId("water")), "authored large-capacity fixture admits the exact original quantity");
            c.That(game.World.Services.Get<INetworkTopology>().Remove(new PlayerId(0), game.World.Content.Get<ILineDefinition>("network.pipe"), LineDeclaration.ReadPath(game.World,(game.World.Content.Get<ILineDefinition>("network.pipe")).Layer.Space.Id,Beep.SimEngine.Maps.TilePlacement.PathArguments((game.World.Content.Get<ILineDefinition>("network.pipe")).Layer.Space.Id,new[] { new TileCoord(12, 28) }))).IsNone,
                "wide raw proportional products never overflow a narrow intermediate");
            c.Equal(amount, Total(lines, "network.main", game.World.Content.ResourceId("water")), "wide native quantity conserves every raw unit");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "wide split pools have identical checkpoint identity");
        }
        private static void Durable(Check c, bool independent)
        {
            GameFactory factory = Factory(independent: independent); SimGame game = New(factory); NetworkService lines = Setup(game);
            c.That(!game.World.Services.Has<IEconomy>() && (independent ? game.World.Space is SensorTileSpace : game.World.Space is TileMap), "saved ownership uses actual resource-only provider");
            ILineDefinition line = game.World.Content.Get<ILineDefinition>("network.pipe"); INetworkTopology topology = game.World.Services.Get<INetworkTopology>();
            ResourceId steel = game.World.Content.ResourceId("steel"); var middle = new[] { new TileCoord(12, 28) };
            Stock(game, "network.main", "steel", Fix.FromInt(5)); ulong before = game.Hash();
            c.Equal(LineCodes.ForeignLine, topology.CheckRemoval(new PlayerId(1), line, LineDeclaration.ReadPath(game.World,(line).Layer.Space.Id,Beep.SimEngine.Maps.TilePlacement.PathArguments((line).Layer.Space.Id,middle))), "another company cannot remove owned segments");
            c.Equal(before, game.Hash(), "foreign preflight reveals no pool mutation");
            c.Equal(Codes.OutOfBounds, topology.CheckRemoval(new PlayerId(0), line, LineDeclaration.ReadPath(game.World,(line).Layer.Space.Id,Beep.SimEngine.Maps.TilePlacement.PathArguments((line).Layer.Space.Id,new[] { new TileCoord(-1, 28) }))), "out-of-world removal refuses explicitly");
            c.Equal(Codes.InvalidArgument, topology.CheckRemoval(new PlayerId(0), line, LineDeclaration.ReadPath(game.World,(line).Layer.Space.Id,Beep.SimEngine.Maps.TilePlacement.PathArguments((line).Layer.Space.Id,new[] { middle[0], middle[0] }))), "duplicate removal references refuse");
            c.Equal(Codes.NotFound, topology.CheckRemoval(new PlayerId(0), line, LineDeclaration.ReadPath(game.World,(line).Layer.Space.Id,Beep.SimEngine.Maps.TilePlacement.PathArguments((line).Layer.Space.Id,new[] { new TileCoord(30, 28) }))), "missing segment never silently succeeds");
            c.That(topology.CheckRemoval(new PlayerId(0), line, LineDeclaration.ReadPath(game.World,(line).Layer.Space.Id,Beep.SimEngine.Maps.TilePlacement.PathArguments((line).Layer.Space.Id,middle))).IsNone, "initial multi-network removal would fit");
            Stock(game, "network.secondary", "steel", Fix.FromInt(10)); before = game.Hash();
            c.Equal(NetworkService.NetworkCapacity, topology.Remove(new PlayerId(0), line, LineDeclaration.ReadPath(game.World,(line).Layer.Space.Id,Beep.SimEngine.Maps.TilePlacement.PathArguments((line).Layer.Space.Id,middle))), "current-state execution rechecks changed secondary-network stock");
            c.Equal(before, game.Hash(), "one refusing network prevents all graph and map commits");
            c.Equal(Fix.FromInt(5), Total(lines, "network.main", steel), "earlier successfully evaluated network is not modified");
            c.Equal(1, lines.ComponentCount("network.main"), "primary graph remains whole after secondary refusal");
            // Simulate prior physical draining; no scheduling or flow rule is reproduced by the fixture.
            lines.Component("network.secondary", 0).Inventory.Remove(steel, Fix.FromInt(2));
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "multi-network state restores before execution");
            c.That(topology.Remove(new PlayerId(0), line, LineDeclaration.ReadPath(game.World,(line).Layer.Space.Id,Beep.SimEngine.Maps.TilePlacement.PathArguments((line).Layer.Space.Id,middle))).IsNone, "drained pools now fit exactly");
            c.That(twin.World.Services.Get<INetworkTopology>().Remove(new PlayerId(0), twin.World.Content.Get<ILineDefinition>(line.Id), LineDeclaration.ReadPath(twin.World,(twin.World.Content.Get<ILineDefinition>(line.Id)).Layer.Space.Id,Beep.SimEngine.Maps.TilePlacement.PathArguments((twin.World.Content.Get<ILineDefinition>(line.Id)).Layer.Space.Id,middle))).IsNone,
                "saved session follows the identical topology contract");
            Tick(game); Tick(twin); c.Equal(game.Hash(), twin.Hash(), "multi-network split continues identically");
            c.Equal(Fix.FromInt(8), Total(lines, "network.secondary", steel), "secondary quantity conserves at exact reduced capacity");
            c.Equal(Fix.Zero, Layer(game,line.OwnerLayer.Id).GetSample(middle[0]), "removed ownership clears with geometry");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "committed multi-network split checkpoint has no stale graph");
            Empty(c, factory); Bypass(c, factory); Invalid(c, factory);
        }
        private static void Empty(Check c, GameFactory factory)
        {
            SimGame game = New(factory); NetworkService lines = Setup(game);
            c.That(game.World.Services.Get<INetworkTopology>().Remove(new PlayerId(0), game.World.Content.Get<ILineDefinition>("network.pipe"), LineDeclaration.ReadPath(game.World,(game.World.Content.Get<ILineDefinition>("network.pipe")).Layer.Space.Id,Beep.SimEngine.Maps.TilePlacement.PathArguments((game.World.Content.Get<ILineDefinition>("network.pipe")).Layer.Space.Id,Tiles()))).IsNone,
                "empty network can remove all segments without invented recovery");
            c.Equal(0, lines.ComponentCount("network.main"), "empty primary has no remaining components");
            c.Equal(0, lines.ComponentCount("network.secondary"), "empty secondary has no remaining components");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "empty topology checkpoint reconstructs identically");
        }
        private static void Bypass(Check c, GameFactory factory)
        {
            SimGame game = New(factory); NetworkService lines = Setup(game); Stock(game, "network.secondary", "steel", Fix.One);
            NetworkService.NetComponent original = lines.Component("network.secondary", 0), primary = lines.Component("network.main", 0);
            ILineDefinition line = game.World.Content.Get<ILineDefinition>("network.pipe");
            var layer = Layer(game, line.Layer.Id);
            foreach (TileCoord tile in Tiles()) layer.SetSample(tile, Fix.Zero);
            Stamp(game, line, new TileCoord(30, 28)); game.Update();
            c.That(game.Fault != null && game.Fault.Message.Contains(NetworkService.OccupiedNetwork.Id), "bypassing the execution contract faults instead of silently losing stock");
            c.That(ReferenceEquals(original, lines.State("network.secondary").Components[0]), "failed scheduled rebuild retains prior pool owner");
            c.That(ReferenceEquals(primary, lines.State("network.main").Components[0]), "later scheduled refusal prevents earlier empty-network graph commit");
            c.Equal(Fix.One, original.Amount(game.World.Content.ResourceId("steel")), "failed rebuild retains the actual undisposed material");
            c.Throws<InvalidOperationException>(() => game.Save(), "faulted direct mutation cannot produce a healthy checkpoint");
        }
        private static void Invalid(Check c, GameFactory factory)
        {
            SimGame game = New(factory); NetworkService lines = Setup(game); ResourceId steel = game.World.Content.ResourceId("steel");
            c.Equal(Fix.Zero, lines.Component("network.main", 0).Inventory.Add(steel, Fix.FromRatio(3, 2)), "shared owner refuses fractional discrete pool stock");
            c.Equal(Fix.FromInt(10), lines.Component("network.secondary", 0).Inventory.Add(steel, Fix.FromInt(11)), "shared owner bounds admission to actual component capacity");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "refused invalid native/capacity attempts leave a healthy checkpoint");
        }
    }
}
