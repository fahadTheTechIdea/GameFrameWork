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
    public static class TransformationOutputPortTests
    {
        public const string Case = "P8 output ports: authored mixed products, atomic waits and saved batch/flow continuation";
        private static readonly Code Process = new Code("process");
        public static void Register() => TestRunner.Add(Case, Run);
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest {
            Scenario = "ports", Seed = 937, Seats = { Controller.Human, Controller.Human } });
        private static EntityId Find(SimGame game, string prototype)
        {
            foreach (var entity in game.Seat(0).Snapshot.Entities) if (entity.Def == prototype) return entity.Id;
            throw new InvalidOperationException("missing authored entity " + prototype);
        }
        private static ResourceId Resource(SimGame game, string name)
        {
            if (!game.Seat(0).View.TryFindResource(name, out var resource)) throw new InvalidOperationException("missing public resource " + name);
            return resource;
        }
        private static Fix Stock(SimGame game, string resource)
        {
            var facets = new List<Facet>();
            if (!game.Seat(0).View.Inspect(Find(game, "ports.factory"), facets)) throw new InvalidOperationException("missing stock facts");
            foreach (var facet in facets) if (facet.Key == Storage.FacetStock && facet.Subject == resource) return facet.Value;
            return Fix.Zero;
        }
        private static TransformationSnapshot State(SimGame game)
        {
            if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(Find(game, "ports.factory"), Process, out var state))
                throw new InvalidOperationException("missing process facts");
            return state;
        }
        private static IReadOnlyList<ResourcePortSnapshot> Ports(SimGame game, string recipe)
        {
            var result = game.Seat(0).View.Feature<TransformationManagementView>().ReadOutputPorts(Find(game, "ports.factory"), Process, recipe, out var ports);
            if (!result.IsNone) throw new InvalidOperationException("missing port facts " + result);
            return ports;
        }
        private static ResourcePortSnapshot Port(SimGame game, string resource, string recipe = "ports.batch")
        {
            foreach (var port in Ports(game, recipe)) if (port.Resource == Resource(game, resource)) return port;
            throw new InvalidOperationException("missing authored route");
        }
        private static Fix Product(SimGame game, string resource, string recipe = "ports.batch")
            => Port(game, resource, recipe).Stock.AmountOf(Resource(game, resource));
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("engine fault at " + game.Tick + ": " + game.Fault);
        }
        private static void Enqueue(Check c, SimGame game, string recipe, int count = 1)
        {
            var command = new EnqueueTransformationCommand { Transformer = Find(game, "ports.factory"), Capability = Process, Transformation = recipe, Count = count };
            ulong before = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "normal process preview accepts");
            c.Equal(before, game.Hash(), "preview is pure");
            c.That(TestKit.Order(game, 0, command).Accepted, "ordinary engine command enqueues physical work");
        }
        private static void Drain(Check c, SimGame game, string resource, Fix amount, SimGame twin = null)
        {
            TransferProbeCommand Command(SimGame value) => new TransferProbeCommand { Move = true,
                Movements = new[] { new ResourceMovementIntent(new ReservationOwner("proof.player-movement", value.Tick + 1), new PlayerId(0),
                    Port(value, resource).Endpoint, ResourceEndpointId.ForEntity(Find(value, "ports.sink-entity")),
                    new Bill(new[] { new ResourceAmount(Resource(value, resource), amount) }), Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain) } };
            c.That(TestKit.Order(game, 0, Command(game)).Accepted, "existing native movement commits to authored sink");
            if (twin != null) c.That(TestKit.Order(twin, 0, Command(twin)).Accepted, "same player movement accepted in restored session");
        }
        private static void Run(Check c)
        {
            var factory = OutputPortGame.Factory(); var game = New(factory);
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build, "breaking engine semantics identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat, "no new persistent balance or queue codec");
            c.That(!game.Modules.Contains("sim.tycoon") && game.Seat(0).Snapshot.Space == null, "no business or fabricated spatial world");
            var retained = Ports(game, "ports.batch");
            c.Equal(2, retained.Count, "explicit routes for solid product and liquid waste");
            c.That(retained[0].Endpoint != retained[1].Endpoint, "actual distinct provider-owned vessels");
            c.That(!game.Seat(0).View.Feature<InventoryView>().StoreAccepts("ports.factory", Resource(game, "ports.water"), Code.None), "feed inventory cannot accept liquid product");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<TransformationManagementView>().ReadOutputPorts(Find(game, "ports.factory"), Process, "ports.batch", out var foreign), "foreign port read refused");
            c.Equal(0, foreign.Count, "foreign read leaks no route/stock metadata");
            c.Throws<NotSupportedException>(() => ((IList<ResourcePortSnapshot>)retained).Clear(), "retained routes immutable");
            Enqueue(c, game, "ports.batch", 2);
            c.Equal(Fix.FromInt(16), Stock(game, "ports.ore"), "first batch transfers actual inputs to WIP once");
            c.Equal(1L, State(game).Progress.WholeTicks, "first batch earns one authored second");
            c.Equal(Fix.Zero, Product(game, "ports.alloy"), "no instant solid result");
            c.Equal(Fix.Zero, Product(game, "ports.water"), "no instant liquid result");
            var twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "save restores running inputs/tools and port owners");
            Step(game, twin); Step(game, twin);
            c.Equal(Fix.FromInt(2), Product(game, "ports.alloy"), "batch produces into solid vessel");
            c.Equal(Fix.One, Product(game, "ports.water"), "same commit produces liquid byproduct");
            c.Equal(Fix.Zero, Stock(game, "ports.alloy"), "product never silently enters feed store");
            for (int i = 0; i < 3; i++) Step(game, twin);
            c.Equal(TransformationPhase.Blocked, State(game).Phase, "completed next batch physically waits for full ports");
            c.Equal(3L, State(game).Progress.WholeTicks, "full outputs retain completed progress");
            c.Equal(Fix.FromInt(4), State(game).Wip.Stock.AmountOf(Resource(game, "ports.ore")), "blocked batch retains actual protected inputs");
            c.Equal(Fix.One, State(game).Tools.Held.AmountOf(Resource(game, "ports.jig")), "blocked batch retains actual tool claim");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "save restores full-output wait");
            Drain(c, game, "ports.alloy", Fix.FromInt(2), twin);
            c.Equal(Fix.Zero, Product(game, "ports.alloy"), "one freed port receives no partial second result");
            c.Equal(Fix.One, Product(game, "ports.water"), "still-full byproduct retains its previous amount");
            c.Equal(TransformationPhase.Blocked, State(game).Phase, "both outputs must fit together");
            Drain(c, game, "ports.water", Fix.One, twin);
            c.Equal(0, State(game).QueueCount, "whole result commits when every port fits");
            c.Equal(Fix.FromInt(2), Product(game, "ports.alloy"), "second solid result committed exactly once");
            c.Equal(Fix.One, Product(game, "ports.water"), "second liquid result committed exactly once");
            c.That(State(game).Wip.Stock.IsEmpty && State(game).Tools.Held.IsEmpty, "completion consumes WIP and releases tools");
            c.Equal(game.Hash(), twin.Hash(), "saved waits and ordinary movements continue identically");
            c.That(retained.All(port => port.Stock.IsEmpty), "earlier route/stock publication remains detached");
            Enqueue(c, game, "ports.batch"); twin = factory.Load(game.Save());
            CancelTransformationCommand Cancel(SimGame value) => new CancelTransformationCommand { Transformer = Find(value, "ports.factory"), Capability = Process, Index = 0 };
            c.That(TestKit.Order(game, 0, Cancel(game)).Accepted && TestKit.Order(twin, 0, Cancel(twin)).Accepted, "running cancellation uses existing command and WIP return");
            c.Equal(Fix.FromInt(12), Stock(game, "ports.ore"), "cancel returns only this batch's actual raw inputs");
            c.Equal(Fix.FromInt(2), Product(game, "ports.alloy"), "cancel manufactures no extra solid result");
            c.Equal(Fix.One, Product(game, "ports.water"), "cancel manufactures no extra liquid result");
            c.Equal(game.Hash(), twin.Hash(), "saved cancellation remains identical");

            var flow = New(factory); Enqueue(c, flow, "ports.flow");
            c.Equal(Fix.FromInt(18), Stock(flow, "ports.ore"), "continuous step consumes actual feed");
            c.Equal(Fix.One, Product(flow, "ports.alloy"), "continuous solid output enters its own vessel");
            c.Equal(Fix.Parse("0.5"), Product(flow, "ports.water"), "continuous liquid uses its own packing policy");
            var flowTwin = factory.Load(flow.Save()); Step(flow, flowTwin);
            var pausedProgress = State(flow).Progress; var pausedInputs = Stock(flow, "ports.ore");
            Step(flow, flowTwin); Step(flow, flowTwin);
            c.Equal(pausedProgress, State(flow).Progress, "full continuous ports earn no work");
            c.Equal(pausedInputs, Stock(flow, "ports.ore"), "full continuous ports burn no feed");
            c.Equal(Fix.FromInt(2), Product(flow, "ports.alloy"), "no uncommitted solid prefix");
            c.Equal(Fix.One, Product(flow, "ports.water"), "no uncommitted liquid prefix");
            Drain(c, flow, "ports.alloy", Fix.FromInt(2), flowTwin);
            c.Equal(Fix.Zero, Product(flow, "ports.alloy"), "other full flow port prevents partial output");
            c.Equal(pausedInputs, Stock(flow, "ports.ore"), "other full flow port prevents partial input burn");
            Drain(c, flow, "ports.water", Fix.One, flowTwin);
            c.Equal(Fix.One, Product(flow, "ports.alloy"), "freed flow ports restart at retained progress");
            c.Equal(Fix.Parse("0.5"), Product(flow, "ports.water"), "saved liquid yield is neither lost nor doubled");
            c.Equal(flow.Hash(), flowTwin.Hash(), "mixed-rate continuation identical");
            c.Equal(2, State(flow).Flow.OutputPorts.Count, "flow publishes per-port private endpoint versions");
            c.That(TestKit.Order(flow, 0, Cancel(flow)).Accepted, "continuous stop accepted");
            c.That(State(flow).Tools.Held.IsEmpty && State(flow).QueueCount == 0, "stop releases reusable tools without clearing actual products");

            var sharedFactory = OutputPortGame.Factory(json => {
                var parts = json.Get("entities")[0].Get("components").Get("pool_port");
                for (int i = 0; i < parts.Count; i++) parts[i].Set("pool", JsonValue.Of("ports.shared-pool"));
            });
            var shared = New(sharedFactory); Enqueue(c, shared, "ports.batch"); Step(shared); Step(shared);
            c.Equal(Port(shared, "ports.alloy").Endpoint, Port(shared, "ports.water").Endpoint, "explicit aliases use one actual stock authority");
            c.Equal(Fix.FromInt(2), Product(shared, "ports.alloy"), "alias outputs grouped into one native production intent");
            c.Equal(Fix.One, Product(shared, "ports.water"), "same endpoint receives both resources without duplicate receipt");
            foreach (string invalid in new[] { "missing", "extra", "unknown-port", "incompatible", "empty" })
                c.Throws<ContentException>(() => New(OutputPortGame.Factory(json => {
                    var routes = json.Get("transformations")[0].Get("output_ports");
                    if (invalid == "missing") json.Get("transformations")[0].Remove("output_ports");
                    if (invalid == "empty") routes.Remove("ports.water");
                    if (invalid == "extra") routes.Set("ports.ore", JsonValue.Of("product"));
                    if (invalid == "unknown-port") routes.Set("ports.water", JsonValue.Of("missing"));
                    if (invalid == "incompatible") routes.Set("ports.water", JsonValue.Of("product"));
                })), "invalid authored routing refuses before engine Update/" + invalid);
            c.That(game.Fault == null && flow.Fault == null && shared.Fault == null, "all physical workflows end without engine fault");
        }
    }
}
