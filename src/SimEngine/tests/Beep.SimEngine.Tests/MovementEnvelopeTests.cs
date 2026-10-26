using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Player-only requests/observations; all admission, stock, promises and mutations stay in the engine.
    public static class MovementEnvelopeTests
    {
        public const string Case = "P8 movement envelope: mixed ports, aggregate refusals and saved convergence/split";
        private static readonly PlayerId Company = new PlayerId(0);
        private static readonly Code Process = new Code("process");
        public static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(bool smallSink = false) => OutputPortGame.Factory(json => {
            var storage = json.Get("entities")[0].Get("components").Get("storage");
            storage.Get("initial_stock").Set("ports.alloy", JsonValue.Number("20"));
            storage.Get("initial_stock").Set("ports.water", JsonValue.Number("5"));
            storage.Get("accepts").Add(JsonValue.Of("ports.alloy"));
            storage.Get("accepts").Add(JsonValue.Of("ports.water"));
            storage.Get("phases").Add(JsonValue.Of("liquid"));
        }, editShared: json => {
            json.Get("capacity_policies")[0].Get("limits").Set("ports.kg", JsonValue.Number("100"));
            if (smallSink)
            {
                json.Get("capacity_policies")[4].Get("limits").Set("ports.kg", JsonValue.Number("3"));
                json.Get("capacity_policies")[4].Get("limits").Set("ports.litre", JsonValue.Number("3"));
            }
        });
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest {
            Scenario = "ports", Seed = 811, Seats = { Controller.Human, Controller.Human } });
        private static EntityId Find(SimGame game, string name) => game.Seat(0).Snapshot.Entities.Single(entity => entity.Def == name).Id;
        private static ResourceId Resource(SimGame game, string name)
        {
            if (!game.Seat(0).View.TryFindResource(name, out var resource)) throw new InvalidOperationException("missing authored resource");
            return resource;
        }
        private static ResourceEndpointId Source(SimGame game) => ResourceEndpointId.ForEntity(Find(game, "ports.factory"));
        private static ResourceEndpointId Sink(SimGame game) => ResourceEndpointId.ForEntity(Find(game, "ports.sink-entity"));
        private static ResourceEndpointId Port(SimGame game, string resource)
        {
            var result = game.Seat(0).View.Feature<TransformationManagementView>().ReadOutputPorts(Find(game, "ports.factory"), Process, "ports.batch", out var ports);
            if (!result.IsNone) throw new InvalidOperationException("missing port facts");
            return ports.Single(port => port.Resource == Resource(game, resource)).Endpoint;
        }
        private static Fix Stock(SimGame game, string entity, string resource)
        {
            var facets = new List<Facet>();
            if (!game.Seat(0).View.Inspect(Find(game, entity), facets)) throw new InvalidOperationException("missing stock facts");
            return facets.Where(facet => facet.Key == Storage.FacetStock && facet.Subject == resource).Select(facet => facet.Value).DefaultIfEmpty(Fix.Zero).Single();
        }
        private static ResourceFlowPage History(SimGame game) => game.Seat(0).View.Feature<ResourceFlowView>().Read(Company, 0);
        private static ResourceMovementIntent Move(SimGame game, long owner, ResourceEndpointId source, ResourceEndpointId destination,
            string resource, int amount, IReadOnlyList<ResourceReservation> receipts = null)
            => new ResourceMovementIntent(new ReservationOwner("proof.movement-envelope", owner), Company, source, destination,
                new Bill(new[] { new ResourceAmount(Resource(game, resource), Fix.FromInt(amount)) }), receipts ?? Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain);
        private static TransferProbeCommand Command(params ResourceMovementIntent[] movements) => new TransferProbeCommand { Move = true, Movements = movements };
        private static void Order(Check c, SimGame game, TransferProbeCommand command, bool accepted = true)
        {
            ulong hash = game.Hash();
            c.Equal(accepted ? Code.None : Codes.Blocked, game.Seat(0).Preview(command), "whole-envelope preview outcome");
            c.Equal(hash, game.Hash(), "preview changes no endpoint, receipt, sequence or clock");
            c.Equal(accepted, TestKit.Order(game, 0, command).Accepted, "normal command uses the same current-state admission");
            if (game.Fault != null) throw new InvalidOperationException("movement Update fault: " + game.Fault);
        }
        private static void Reservations(Check c, SimGame game, string entity, int count)
        {
            c.That(game.Seat(0).View.Feature<InventoryView>().TryGetReservations(Find(game, entity), Code.None, out var receipts), "owned reservation facts readable");
            c.Equal(count, receipts.Count, "only actual surviving endpoint-owned receipts remain");
        }
        private static void Run(Check c)
        {
            var factory = Factory(); var game = New(factory);
            var limits = new ReplayLimits(1024 * 1024, 200);
            var recorder = game.RecordReplay(limits);
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build, "breaking complete movement semantics identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat, "no new stock, promise or clock codec");
            c.That(!game.Modules.Contains("sim.tycoon") && game.Seat(0).Snapshot.Space == null, "resource-only internal vessels need no business or map");
            var product = Port(game, "ports.alloy"); var waste = Port(game, "ports.water");
            var shared = new ResourceEndpointId("proof.pools", 3); // Explicit identity in the selected authored pool definition.
            Order(c, game, Command(Move(game, 1, Source(game), product, "ports.alloy", 2), Move(game, 1, Source(game), waste, "ports.water", 1)));
            c.Equal(Fix.FromInt(18), Stock(game, "ports.factory", "ports.alloy"), "complete mixed output movement uses actual solid source");
            c.Equal(Fix.FromInt(4), Stock(game, "ports.factory", "ports.water"), "same complete movement preserves exact liquid units");
            Reservations(c, game, "ports.factory", 0);
            long latest = History(game).Latest;
            Order(c, game, Command(Move(game, 2, Source(game), Sink(game), "ports.alloy", 1), Move(game, 2, Source(game), product, "ports.alloy", 1)), false);
            c.Equal(Fix.Zero, Stock(game, "ports.sink-entity", "ports.alloy"), "full later port refuses earlier ready prefix");
            c.Equal(Fix.FromInt(18), Stock(game, "ports.factory", "ports.alloy"), "full later port consumes no earlier source");
            c.Equal(latest, History(game).Latest, "refusal publishes no partial movement fact");
            Reservations(c, game, "ports.factory", 0); Reservations(c, game, "ports.sink-entity", 0);
            Order(c, game, Command(Move(game, 3, Source(game), shared, "ports.alloy", 2)));
            var converge = Command(Move(game, 4, product, Sink(game), "ports.alloy", 2), Move(game, 4, shared, Sink(game), "ports.alloy", 2));
            game.Seat(0).Submit(converge); var twin = factory.Load(game.Save());
            game.Update(); twin.Update(); c.That(game.Fault == null && twin.Fault == null, "saved converging envelope executes through normal Update");
            c.Equal(game.Hash(), twin.Hash(), "pending mixed endpoint command restores identically");
            c.Equal(Fix.FromInt(4), Stock(game, "ports.sink-entity", "ports.alloy"), "converging sources share one aggregate inlet receipt");
            var split = new[] { Move(game, 5, Sink(game), product, "ports.alloy", 2), Move(game, 5, Sink(game), shared, "ports.alloy", 2) };
            twin = factory.Load(game.Save());
            Order(c, game, Command(split)); Array.Reverse(split); Order(c, twin, Command(split));
            c.Equal(game.Hash(), twin.Hash(), "shuffled split uses canonical actual source claims and transfer history");
            c.Equal(Fix.Zero, Stock(game, "ports.sink-entity", "ports.alloy"), "split spends one complete source receipt across distinct destinations");
            Reservations(c, game, "ports.sink-entity", 0);
            c.Equal(ResourceFlowReadStatus.NotOwned, game.Seat(1).View.Feature<ResourceFlowView>().Read(Company, 0).Status, "movement diagnostics remain company-private");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "mixed movement state uses existing sole stock codecs");
            var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
            c.That(replay.Success, "canonical movement-array replay verifies: " + replay.Detail);
            c.Equal(game.Hash(), replay.ActualHash, "whole-envelope replay restores exact final native owners/history");

            var bounded = New(Factory(true));
            Order(c, bounded, Command(Move(bounded, 1, Source(bounded), Port(bounded, "ports.alloy"), "ports.alloy", 2), Move(bounded, 1, Source(bounded), shared, "ports.alloy", 2)));
            latest = History(bounded).Latest;
            Order(c, bounded, Command(Move(bounded, 2, Port(bounded, "ports.alloy"), Sink(bounded), "ports.alloy", 2), Move(bounded, 2, shared, Sink(bounded), "ports.alloy", 2)), false);
            c.Equal(Fix.Zero, Stock(bounded, "ports.sink-entity", "ports.alloy"), "individually fitting convergent legs cannot overbook aggregate capacity");
            c.Equal(latest, History(bounded).Latest, "aggregate inlet refusal creates no success facts");
            Reservations(c, bounded, "ports.sink-entity", 0);

            var heldGame = New(factory); var owner = new ReservationOwner("proof.movement-envelope", 11);
            c.That(TestKit.Order(heldGame, 0, new ClaimProbeCommand { Intents = new[] { new ResourceClaimIntent(ReservationDirection.Outbound,
                owner, Source(heldGame), new Bill(new[] { new ResourceAmount(Resource(heldGame, "ports.alloy"), Fix.FromInt(6)) })) } }).Accepted, "ordinary command acquires one actual source receipt");
            c.That(heldGame.Seat(0).View.Feature<InventoryView>().TryGetReservations(Find(heldGame, "ports.factory"), Code.None, out var held), "existing protected stock is publicly inspectable by owner");
            latest = History(heldGame).Latest;
            Order(c, heldGame, Command(Move(heldGame, 11, Source(heldGame), Sink(heldGame), "ports.alloy", 4, held), Move(heldGame, 11, Source(heldGame), shared, "ports.alloy", 3, held)), false);
            c.Equal(latest, History(heldGame).Latest, "aggregate existing-receipt overspend refuses without any prefix");
            Reservations(c, heldGame, "ports.factory", 1);
            Order(c, heldGame, Command(Move(heldGame, 11, Source(heldGame), Sink(heldGame), "ports.alloy", 3, held), Move(heldGame, 11, Source(heldGame), product, "ports.alloy", 2, held)));
            c.That(heldGame.Seat(0).View.Feature<InventoryView>().TryGetReservations(Find(heldGame, "ports.factory"), Code.None, out var remaining), "same endpoint owns partial surviving receipt");
            c.Equal(1, remaining.Count, "one source token remains, never copied per destination");
            c.Equal(held[0].Token, remaining[0].Token, "bounded spending retains original native token");
            c.Equal(Fix.One, remaining[0].Amount, "whole-set spending leaves exact unspent native amount");
            c.Equal(Fix.FromInt(6), held[0].Amount, "previous private facts remain detached");
            latest = History(heldGame).Latest;
            Order(c, heldGame, Command(Move(heldGame, 11, Source(heldGame), shared, "ports.alloy", 1, held)), false);
            c.Equal(latest, History(heldGame).Latest, "stale retained receipt is never commit authority");
            twin = factory.Load(heldGame.Save());
            Order(c, heldGame, Command(Move(heldGame, 11, Source(heldGame), shared, "ports.alloy", 1, remaining)));
            Order(c, twin, Command(Move(twin, 11, Source(twin), shared, "ports.alloy", 1, remaining)));
            c.Equal(heldGame.Hash(), twin.Hash(), "saved partial receipt continuation spends once and matches");
            Reservations(c, heldGame, "ports.factory", 0);

            var invalid = New(factory);
            foreach (var request in new[] {
                Command(Array.Empty<ResourceMovementIntent>()),
                Command(Move(invalid, 1, Source(invalid), Sink(invalid), "ports.alloy", 1), default),
                Command(Move(invalid, 1, Source(invalid), Sink(invalid), "ports.alloy", 1), Move(invalid, 1, Source(invalid), Sink(invalid), "ports.alloy", 1)),
                Command(Move(invalid, 2, Source(invalid), Sink(invalid), "ports.alloy", 20), Move(invalid, 2, Source(invalid), Port(invalid, "ports.alloy"), "ports.alloy", 1)),
                Command(Move(invalid, 3, Source(invalid), Sink(invalid), "ports.alloy", 1), Move(invalid, 3, Source(invalid), new ResourceEndpointId("proof.pools", 99), "ports.alloy", 1))
            }) Order(c, invalid, request, false);
            c.Equal(Fix.FromInt(20), Stock(invalid, "ports.factory", "ports.alloy"), "malformed/missing/aggregate source refusal never moves valid prefix");
            Reservations(c, invalid, "ports.factory", 0); Reservations(c, invalid, "ports.sink-entity", 0);
            var maximum = new Bill(new[] { new ResourceAmount(Resource(invalid, "ports.alloy"), Fix.FromRaw(long.MaxValue)) });
            Order(c, invalid, Command(
                new ResourceMovementIntent(new ReservationOwner("proof.movement-envelope", 20), Company, Source(invalid), Sink(invalid), maximum, Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain),
                new ResourceMovementIntent(new ReservationOwner("proof.movement-envelope", 20), Company, Source(invalid), shared, maximum, Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain)), false);
            var mixed = new Bill(new[] { new ResourceAmount(Resource(invalid, "ports.ore"), Fix.FromInt(2)), new ResourceAmount(Resource(invalid, "ports.jig"), Fix.One) });
            Order(c, invalid, Command(new ResourceMovementIntent(new ReservationOwner("proof.movement-envelope", 21), Company, Source(invalid), Sink(invalid), mixed, Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), System.Array.Empty<ResourceReservation>(), ResourceMovementParticipantDisposition.Retain)));
            c.Equal(Fix.FromInt(2), Stock(invalid, "ports.sink-entity", "ports.ore"), "one directed bill transfers native material count exactly");
            c.Equal(Fix.One, Stock(invalid, "ports.sink-entity", "ports.jig"), "same bill transfers discrete tool without mixed-unit total");
            Reservations(c, invalid, "ports.factory", 0); Reservations(c, invalid, "ports.sink-entity", 0);
            var nullCommand = new TransferProbeCommand { Move = true, Movements = null };
            ulong hash = invalid.Hash();
            c.Equal(Codes.InvalidArgument, invalid.Seat(0).Preview(nullCommand), "absent movement set explicitly refuses");
            c.Equal(hash, invalid.Hash(), "absent-set preview is pure");
            invalid.Seat(0).Submit(nullCommand); twin = factory.Load(invalid.Save());
            invalid.Update(); twin.Update();
            c.That(invalid.Fault == null && twin.Fault == null, "saved absent set refuses without fault or fabricated default");
            c.Equal(invalid.Hash(), twin.Hash(), "absent movement array remains absent through command/save codec");
            c.Equal(Fix.FromInt(20), Stock(invalid, "ports.factory", "ports.alloy"), "overflow and absent sets preserve actual unrelated stock");
        }
    }
}
