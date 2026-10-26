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
    // Thin player: requests, immutable views and engine Update only. All materials/timing/rules belong to the engine.
    public static class TransformationInputPortTests
    {
        public const string Case = "P8 input ports: authored mixed feed, original-source returns and saved batch/flow execution";
        private static readonly Code Process = new Code("process");
        public static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(Action<JsonValue> edit = null)
            => OutputPortGame.Factory(edit, "input-ports.json", "input-ports-master.json");
        internal static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest {
            Scenario = "ports", Seed = 923, Seats = { Controller.Human, Controller.Human } });
        internal static EntityId Find(SimGame game, string name) => game.Seat(0).Snapshot.Entities.Single(entity => entity.Def == name).Id;
        internal static ResourceId Resource(SimGame game, string name)
        {
            if (!game.Seat(0).View.TryFindResource(name, out var id)) throw new InvalidOperationException("missing authored resource " + name);
            return id;
        }
        internal static TransformationSnapshot State(SimGame game, string entity = "ports.factory")
        {
            if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(Find(game, entity), Process, out var state))
                throw new InvalidOperationException("missing private process facts");
            return state;
        }
        private static IReadOnlyList<ResourcePortSnapshot> Ports(SimGame game, bool input, string recipe = "ports.batch")
        {
            var view = game.Seat(0).View.Feature<TransformationManagementView>();
            IReadOnlyList<ResourcePortSnapshot> ports;
            var result = input ? view.ReadInputPorts(Find(game, "ports.factory"), Process, recipe, out ports)
                : view.ReadOutputPorts(Find(game, "ports.factory"), Process, recipe, out ports);
            if (!result.IsNone) throw new InvalidOperationException("missing port facts " + result);
            return ports;
        }
        internal static ResourcePortSnapshot Port(SimGame game, string resource, bool input)
            => Ports(game, input).Single(port => port.Resource == Resource(game, resource));
        internal static Fix PortStock(SimGame game, string resource, bool input)
            => Port(game, resource, input).Stock.AmountOf(Resource(game, resource));
        internal static Fix Stock(SimGame game, string resource, string entity = "ports.factory")
        {
            var facets = new List<Facet>();
            if (!game.Seat(0).View.Inspect(Find(game, entity), facets)) throw new InvalidOperationException("missing stock facts");
            return facets.Where(facet => facet.Key == Storage.FacetStock && facet.Subject == resource).Select(facet => facet.Value).DefaultIfEmpty(Fix.Zero).Single();
        }
        internal static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("native input Update fault at " + game.Tick + ": " + game.Fault + "/" + twin?.Fault);
        }
        internal static void Order(Check c, SimGame game, Command command)
        {
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary player command accepts");
            c.Equal(hash, game.Hash(), "preview is pure");
            c.That(TestKit.Order(game, 0, command).Accepted, "ordinary engine command committed through Update");
            if (game.Fault != null) throw new InvalidOperationException("native command fault " + game.Fault);
        }
        internal static void Enqueue(Check c, SimGame game, string recipe, int count = 1) => Order(c, game,
            new EnqueueTransformationCommand { Transformer = Find(game, "ports.factory"), Capability = Process, Transformation = recipe, Count = count });
        internal static void Control(Check c, SimGame game, bool enabled) => Order(c, game,
            new ConfigureOperationCommand { Entity = Find(game, "ports.factory"), Operation = Process, Enabled = enabled, Target = Fix.One });
        internal static void Cancel(Check c, SimGame game) => Order(c, game,
            new CancelTransformationCommand { Transformer = Find(game, "ports.factory"), Capability = Process, Index = 0 });
        internal static void Select(Check c, SimGame game, long pool) => Order(c, game,
            new SelectResourcePortCommand { Entity = Find(game, "ports.factory"), Port = new Code("raw"), Endpoint = new ResourceEndpointId("proof.pools", pool) });
        internal static void Transfer(Check c, SimGame game, ResourceEndpointId source, ResourceEndpointId destination, string resource, Fix amount, SimGame twin = null)
        {
            TransferProbeCommand Request(SimGame session) => new TransferProbeCommand { Move = true, Movements = new[] {
                new ResourceMovementIntent(new ReservationOwner("proof.input-ports-transfer", session.Tick + 100), new PlayerId(0), source, destination,
                    new Bill(new[] { new ResourceAmount(Resource(session, resource), amount) }), Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain) } };
            Order(c, game, Request(game)); if (twin != null) Order(c, twin, Request(twin));
        }
        internal static void Prime(Check c, SimGame game, Fix ore, Fix water, SimGame twin = null)
        {
            TransferProbeCommand Request(SimGame session)
            {
                var rows = new List<ResourceMovementIntent>(); var source = ResourceEndpointId.ForEntity(Find(session, "ports.source"));
                foreach (var entry in new[] { (Resource: "ports.ore", Amount: ore), (Resource: "ports.water", Amount: water) })
                    if (entry.Amount.IsPositive) rows.Add(new ResourceMovementIntent(new ReservationOwner("proof.input-ports-transfer", session.Tick + 100), new PlayerId(0),
                        source, Port(session, entry.Resource, true).Endpoint,
                        new Bill(new[] { new ResourceAmount(Resource(session, entry.Resource), entry.Amount) }), Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain));
                return new TransferProbeCommand { Move = true, Movements = rows.ToArray() };
            }
            Order(c, game, Request(game)); if (twin != null) Order(c, twin, Request(twin));
        }
        private static void Run(Check c)
        {
            var factory = Factory(); var game = New(factory);
            var limits = new ReplayLimits(2 * 1024 * 1024, 500); var recorder = game.RecordReplay(limits);
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build, "breaking named-feed engine semantics identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat, "original input references join the existing execution codec");
            c.That(!game.Modules.Contains("sim.tycoon") && game.Seat(0).Snapshot.Space == null, "manufacturing requires no money, market, adapter or map");
            ulong hash = game.Hash(); var retained = Ports(game, true);
            c.Equal(hash, game.Hash(), "native per-input facts do not change stock, clock or RNG");
            c.Equal(2, retained.Count, "explicit independent solid/liquid input ports");
            var originalRaw = Port(game, "ports.ore", true).Endpoint; var originalWater = Port(game, "ports.water", true).Endpoint;
            c.That(originalRaw != originalWater, "actual provider-owned input vessels are distinct");
            c.Throws<NotSupportedException>(() => ((IList<ResourcePortSnapshot>)retained).Clear(), "published input collection is immutable");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<TransformationManagementView>().ReadInputPorts(Find(game, "ports.factory"), Process, "ports.batch", out var hidden), "foreign input facts refused");
            c.Equal(0, hidden.Count, "foreign refusal reveals no input endpoints or stock");
            Enqueue(c, game, "ports.batch");
            c.Equal(TransformationPhase.Supplying, State(game).Phase, "unfilled selected input ports wait even when local inventory has ore");
            c.Equal(WorkSystem.BlockerMaterials, State(game).Blocker, "real selected-source shortage is engine reported");
            c.That(State(game).Wip.Stock.IsEmpty && State(game).Tools.Held.IsEmpty, "startup shortage acquires no prefix inputs or tool");
            c.Equal(Fix.FromInt(20), Stock(game, "ports.ore"), "selected raw port never falls back to local ore stock");
            Prime(c, game, Fix.FromInt(4), Fix.Zero);
            c.Equal(Fix.FromInt(4), PortStock(game, "ports.ore", true), "solid ready prefix stays at source while liquid is absent");
            c.That(State(game).Wip.Stock.IsEmpty && State(game).Progress.Equals(WorkProgress.Zero), "missing liquid earns no work or input prefix");
            Prime(c, game, Fix.Zero, Fix.One);
            c.Equal(TransformationPhase.Working, State(game).Phase, "whole selected feed starts timed native batch");
            c.Equal(1L, State(game).Progress.WholeTicks, "startup does not instantly complete authored three-second process");
            c.Equal(Fix.FromInt(4), State(game).Wip.Stock.AmountOf(Resource(game, "ports.ore")), "actual raw material enters protected WIP");
            c.Equal(Fix.One, State(game).Wip.Stock.AmountOf(Resource(game, "ports.water")), "same atomic startup admits actual liquid to WIP");
            c.Equal(Fix.One, State(game).Tools.Held.AmountOf(Resource(game, "ports.jig")), "existing native tool owner holds its actual reusable tool");
            c.Equal(Fix.Zero, PortStock(game, "ports.alloy", false), "no result before earned duration");
            var sources = State(game).Wip.Sources; c.Equal(2, sources.Count, "sole execution persists original source references");
            c.Throws<NotSupportedException>(() => ((IList<TransformationPortLink>)sources).Clear(), "original-source publication cannot mutate execution");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "save restores mixed WIP, tool, source books and execution links");
            Control(c, game, false); Control(c, twin, false);
            Prime(c, game, Fix.FromInt(4), Fix.One, twin);
            Select(c, game, 6); Select(c, twin, 6);
            c.Equal(6L, Port(game, "ports.ore", true).Endpoint.Identity, "ordinary engine routing control changes current selected vessel");
            c.Equal(originalRaw, State(game).Wip.Sources.Single(source => source.Resource == Resource(game, "ports.ore")).Endpoint, "port reconfiguration cannot relabel active original source");
            Cancel(c, game); Cancel(c, twin);
            c.Equal(TransformationPhase.Returning, State(game).Phase, "full original return vessels retain cancelled head");
            c.Equal(TransformerManager.BlockerReturnFull, State(game).Blocker, "return capacity wait is engine reported");
            c.Equal(1L, State(game).Progress.WholeTicks, "cancel wait preserves earned work");
            c.Equal(2, State(game).Wip.Sources.Count, "full return retains original references");
            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "saved full return includes changed live port plus original source bindings");
            var sink = ResourceEndpointId.ForEntity(Find(game, "ports.sink-entity")); var wipEndpoint = State(game).Wip.Endpoint;
            Transfer(c, game, originalRaw, sink, "ports.ore", Fix.FromInt(4), twin);
            c.Equal(TransformationPhase.Returning, State(game).Phase, "one empty vessel cannot authorize half a return");
            c.Equal(Fix.FromInt(4), State(game).Wip.Stock.AmountOf(Resource(game, "ports.ore")), "solid WIP remains while liquid return still full");
            c.Equal(Fix.One, State(game).Wip.Stock.AmountOf(Resource(game, "ports.water")), "liquid WIP remains in same buffer");
            c.Equal(Fix.One, State(game).Tools.Held.AmountOf(Resource(game, "ports.jig")), "return wait retains actual tool promise");
            Transfer(c, game, originalWater, sink, "ports.water", Fix.One, twin);
            c.That(State(game).QueueCount == 0 && State(game).Wip.Stock.IsEmpty && State(game).Tools.Held.IsEmpty && State(game).Wip.Sources.Count == 0, "complete return releases head, actual WIP/tool and reference state once");
            c.Equal(Fix.Zero, PortStock(game, "ports.ore", true), "cancel never redirects original ore into newly selected vessel");
            var returned = game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0).Entries.Where(entry => entry.Item.Source.Endpoint == wipEndpoint).ToArray();
            c.Equal(2, returned.Length, "one verified return fact for each native input resource");
            c.That(returned.Any(entry => entry.Item.Destination.Endpoint == originalRaw && entry.Item.Resource == Resource(game, "ports.ore") && entry.Item.Amount == Fix.FromInt(4)), "ore returns to its original actual endpoint");
            c.That(returned.Any(entry => entry.Item.Destination.Endpoint == originalWater && entry.Item.Resource == Resource(game, "ports.water") && entry.Item.Amount == Fix.One), "liquid returns to its original actual endpoint");
            c.Equal(game.Hash(), twin.Hash(), "saved mixed returning wait and routing control continue identically");
            c.Equal(2, sources.Count, "earlier original-source facts remain detached after execution clears");
            c.That(retained.All(port => port.Stock.IsEmpty), "earlier stock facts remain detached after prime/consume/return");
            Select(c, game, 4); Control(c, game, true); Enqueue(c, game, "ports.batch", 2);
            twin = factory.Load(game.Save()); Step(game, twin); Step(game, twin);
            c.Equal(Fix.FromInt(2), PortStock(game, "ports.alloy", false), "first completed native mixed-input batch yields solid product");
            c.Equal(Fix.One, PortStock(game, "ports.water", false), "same complete result yields liquid byproduct");
            c.Equal(1, State(game).QueueCount, "second batch remains normal authoritative queue entry");
            Prime(c, game, Fix.FromInt(4), Fix.One, twin); Step(game, twin); Step(game, twin);
            c.Equal(TransformationPhase.Blocked, State(game).Phase, "full products retain completed second mixed batch");
            c.Equal(3L, State(game).Progress.WholeTicks, "output wait preserves complete earned duration");
            c.Equal(2, State(game).Wip.Sources.Count, "output wait retains both original input bindings");
            c.Equal(Fix.FromInt(4), State(game).Wip.Stock.AmountOf(Resource(game, "ports.ore")), "output wait does not destroy raw WIP");
            twin = factory.Load(game.Save()); Cancel(c, game); Cancel(c, twin);
            c.Equal(Fix.FromInt(4), PortStock(game, "ports.ore", true), "completed cancellation returns unconverted raw input exactly once");
            c.Equal(Fix.One, PortStock(game, "ports.water", true), "completed cancellation returns unconverted liquid exactly once");
            c.Equal(Fix.FromInt(2), PortStock(game, "ports.alloy", false), "cancel does not create extra product prefix");
            c.Equal(game.Hash(), twin.Hash(), "saved completed output wait cancellation remains identical");
            c.Equal(Fix.FromInt(20), Stock(game, "ports.ore"), "every native batch used selected ports, never local raw fallback");
            var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
            c.That(replay.Success, "native input/route/cancel player replay verifies: " + replay.Detail);
            c.Equal(game.Hash(), replay.ActualHash, "replay reproduces original-source bindings and actual stock/history");

            var flow = New(factory); Prime(c, flow, Fix.FromInt(2), Fix.Parse("0.5")); Enqueue(c, flow, "ports.flow");
            c.Equal(1L, State(flow).Progress.WholeTicks, "continuous process earns its first authored second");
            c.Equal(Fix.One, PortStock(flow, "ports.alloy", false), "continuous output follows positive native update time");
            c.Equal(Fix.Parse("0.5"), PortStock(flow, "ports.water", false), "fractional liquid product keeps selected outlet quantum");
            c.Equal(2, State(flow).Flow.InputPorts.Count, "flow publishes both actual named input sources");
            c.Equal(0, State(flow).Wip.Sources.Count, "consumed continuous feed creates no refundable source/reference ledger");
            var flowTwin = factory.Load(flow.Save()); var progress = State(flow).Progress;
            Step(flow, flowTwin); c.Equal(progress, State(flow).Progress, "feed shortage earns no progress");
            c.Equal(TransformerManager.BlockerFlowInputs, State(flow).Blocker, "real port feed shortage is reported");
            Prime(c, flow, Fix.FromInt(2), Fix.Zero, flowTwin);
            c.Equal(Fix.FromInt(2), PortStock(flow, "ports.ore", true), "one ready feed cannot burn before other feed arrives");
            c.Equal(progress, State(flow).Progress, "mixed source shortage preserves earned flow progress");
            Prime(c, flow, Fix.Zero, Fix.Parse("0.5"), flowTwin);
            c.Equal(Fix.FromInt(4), State(flow).Flow.Consumed.AmountOf(Resource(flow, "ports.ore")), "cumulative native raw consumption derives from exact progress");
            c.Equal(Fix.One, State(flow).Flow.Consumed.AmountOf(Resource(flow, "ports.water")), "same cumulative progress derives native liquid consumption");
            c.Equal(Fix.FromInt(2), PortStock(flow, "ports.alloy", false), "saved whole mixed draw produces complete next result");
            progress = State(flow).Progress; Prime(c, flow, Fix.FromInt(2), Fix.Parse("0.5"), flowTwin); Step(flow, flowTwin);
            c.Equal(progress, State(flow).Progress, "full output burns none of the new mixed feed");
            c.Equal(Fix.FromInt(2), PortStock(flow, "ports.ore", true), "full output retains raw source stock");
            c.Equal(Fix.Parse("0.5"), PortStock(flow, "ports.water", true), "full output retains liquid source stock");
            c.Equal(flow.Hash(), flowTwin.Hash(), "saved continuous shortages and backpressure continue identically");
            Cancel(c, flow); Cancel(c, flowTwin);
            c.That(State(flow).QueueCount == 0 && State(flow).Tools.Held.IsEmpty, "stop releases native tool without refunding consumed inputs");
            c.Equal(Fix.FromInt(2), PortStock(flow, "ports.ore", true), "stop preserves only actual remaining feed");
            c.Equal(Fix.FromInt(20), Stock(flow, "ports.ore"), "continuous feed also has no local inventory fallback");
            c.Equal(flow.Hash(), flowTwin.Hash(), "saved continuous stop remains identical");
            foreach (string invalid in new[] { "missing", "extra", "unknown", "incompatible", "empty" })
                c.Throws<ContentException>(() => New(Factory(json => {
                    var recipe = json.Get("transformations")[0]; var routes = recipe.Get("input_ports");
                    if (invalid == "missing") recipe.Remove("input_ports");
                    if (invalid == "extra") routes.Set("ports.jig", JsonValue.Of("raw"));
                    if (invalid == "unknown") routes.Set("ports.ore", JsonValue.Of("unknown-input"));
                    if (invalid == "incompatible") routes.Set("ports.water", JsonValue.Of("raw"));
                    if (invalid == "empty") routes.Remove("ports.water");
                })), "invalid authored input routes refuse before Update: " + invalid);
        }
    }
}
