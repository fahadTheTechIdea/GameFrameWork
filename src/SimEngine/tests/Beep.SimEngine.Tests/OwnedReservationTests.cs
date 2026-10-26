using System;
using System.Collections.Generic;
using System.IO;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class OwnedReservationTests
    {
        public static void Register()
        {
            TestRunner.Add("Owned reservations: endpoint ownership, exact consume, immutable views and saved receipts", Ownership);
            TestRunner.Add("Owned reservations: physical pickup, delivery, cancellation and saved continuation", Physical);
        }
        private static readonly ReservationOwner Alice = new ReservationOwner("proof.request", 1);
        private static readonly ReservationOwner Bob = new ReservationOwner("proof.request", 2);
        private static void Ownership(Check c)
        {
            VerifyEndpoint(c, "truck");
            VerifyEndpoint(c, "module_port");
            c.That(typeof(IInventory).GetMethod("ClearReservations") == null &&
                typeof(IInletPort).GetMethod("ReserveIn", new[] { typeof(ResourceId), typeof(Fix) }) == null,
                "anonymous mutation/clear APIs have no compatibility overload");
            c.That(typeof(Job).GetField("InHeld") == null && typeof(Job).GetField("OutHeld") == null,
                "job receipts are tokens, not duplicated held booleans");
            c.Throws<CorruptStateException>(() => ReadRecord(new EntityId(2), 1, Alice, Fix.One, ReservationDirection.Inbound),
                "saved receipt rejects foreign endpoint identity");
            c.Throws<CorruptStateException>(() => ReadRecord(new EntityId(1), 5, Alice, Fix.One, ReservationDirection.Inbound),
                "saved receipt cannot reach or exceed the next sequence");
            c.Throws<CorruptStateException>(() => ReadRecord(new EntityId(1), 1, default, Fix.One, ReservationDirection.Inbound),
                "saved receipt rejects missing requester identity");
            c.Throws<CorruptStateException>(() => ReadRecord(new EntityId(1), 1, Alice, -Fix.One, ReservationDirection.Inbound),
                "saved receipt rejects negative physical promise");
            c.Throws<CorruptStateException>(() => ReadRecord(new EntityId(1), 1, Alice, Fix.One, (ReservationDirection)2),
                "saved receipt rejects unknown commitment direction");
        }
        private static void ReadRecord(EntityId endpoint, long sequence, ReservationOwner owner, Fix amount, ReservationDirection direction)
        {
            using (var stream = new MemoryStream())
            {
                var writer = new StateWriter(stream); long next = 5; writer.Long(ref next); writer.Count(1);
                ResourceEndpointId location = ResourceEndpointId.ForEntity(endpoint); ResourceEndpointId.Sync(writer, ref location); writer.Long(ref sequence);
                string scope = owner.Scope; long identity = owner.Identity; ResourceId resource = new ResourceId(1);
                writer.String(ref scope); writer.Long(ref identity); writer.Resource(ref resource); writer.Fix(ref amount); writer.Enum(ref direction);
                writer.Flush(); byte[] payload = stream.ToArray();
                new ResourceReservationBook().Sync(new StateReader(payload, 0, payload.Length), ResourceEndpointId.ForEntity(new EntityId(1)));
            }
        }
        private static void VerifyEndpoint(Check c, string definition)
        {
            GameFactory factory = Colony.Factory(); SimGame game = factory.NewGame(new NewGameRequest { Scenario = "colony" });
            World world = game.World; TileMap map = (TileMap)world.Space;
            Entity endpoint = map.SpawnAt(world, world.Content.Get<EntityDef>(definition), new PlayerId(0), new TileCoord(9, 27));
            IInventory stock = endpoint.Get<IInventory>(); ResourceId steel = world.Content.ResourceId("steel");
            if (stock.Amount(steel).IsPositive) stock.Remove(steel, stock.Amount(steel));
            c.Equal(Fix.FromInt(10), stock.Add(steel, Fix.FromInt(10)), "actual stock belongs to the selected provider");
            ulong before = game.Hash();
            c.That(!stock.CanReserveIn(default, steel, Fix.One), "an absent requester cannot acquire");
            c.Throws<InvalidOperationException>(() => stock.ReserveIn(default, steel, Fix.One), "invalid owner refuses mutation");
            c.Equal(before, game.Hash(), "refused acquisition consumes no sequence or stock");
            c.That(stock.CanReserveIn(Alice, steel, Fix.FromInt(4)), "whole owned intention preflights without acquiring");
            c.Equal(before, game.Hash(), "preflight leaves hash unchanged");
            ReservationToken incomingA = stock.ReserveIn(Alice, steel, Fix.FromInt(4));
            ReservationToken incomingB = stock.ReserveIn(Bob, steel, Fix.FromInt(3));
            ReservationToken outgoing = stock.ReserveOut(Alice, steel, Fix.FromInt(6));
            c.That(incomingA.Endpoint == ResourceEndpointId.ForEntity(endpoint.Id) && incomingB.Sequence > incomingA.Sequence && outgoing.Sequence > incomingB.Sequence,
                "each endpoint issues stable monotonic receipts");
            c.Equal(Fix.FromInt(7), stock.ReservedIn(steel), "inbound aggregate derives from two owned records");
            c.Equal(Fix.FromInt(6), stock.ReservedOut(steel), "outbound aggregate derives from the same record authority");
            c.That(!stock.CanReserveIn(Alice, steel, Fix.One), "duplicate requester/resource/direction refuses");
            c.Throws<InvalidOperationException>(() => stock.ReserveIn(Alice, steel, Fix.One), "a replayed acquire cannot create another active promise");
            before = game.Hash();
            c.Throws<InvalidOperationException>(() => stock.ReleaseIn(Bob, incomingA), "foreign requester cannot release Alice's room");
            c.Throws<InvalidOperationException>(() => stock.ReleaseOut(Alice, incomingA), "inbound token cannot release outbound stock");
            c.Throws<InvalidOperationException>(() => stock.TakeReserved(Bob, outgoing, Fix.FromInt(6)), "foreign requester cannot take protected stock");
            c.Throws<InvalidOperationException>(() => stock.ReleaseIn(Alice, default), "default token is not a receipt");
            c.Throws<InvalidOperationException>(() => stock.ReleaseIn(Alice, new ReservationToken(ResourceEndpointId.ForEntity(new EntityId(endpoint.Id.Value + 1)), incomingA.Sequence)),
                "another endpoint identity cannot redeem the same sequence");
            c.Equal(before, game.Hash(), "all foreign/direction/stale checks leave authoritative state unchanged");
            c.Equal(Fix.Zero, stock.Add(steel, Fix.Half), "owned reservations do not bypass native discrete rules");
            c.Equal(Upgrades.InventoryReserved, Upgrades.Check(world, endpoint, endpoint.Def), "replacement cannot clear/copy another owner's live commitments");
            c.That(game.Seat(0).View.Feature<InventoryView>().TryGetReservations(endpoint.Id, Code.None, out IReadOnlyList<ResourceReservation> retained),
                "owner can inspect immutable reservation facts");
            c.Equal(3, retained.Count, "projection contains records, not an independent aggregate balance");
            c.Throws<NotSupportedException>(() => ((IList<ResourceReservation>)retained).Clear(), "retained facts cannot clear live commitments");
            c.That(!game.Seat(1).View.Feature<InventoryView>().TryGetReservations(endpoint.Id, Code.None, out _), "foreign company cannot read private promises");
            SimGame twin = factory.Load(game.Save()); IInventory restored = twin.World.Entities.Get(endpoint.Id).Get<IInventory>();
            c.Equal(EngineInfo.SaveFormat, SaveGame.Describe(game.Save()).Format, "owned endpoint/job payload uses the current breaking layout");
            c.Equal(game.Hash(), twin.Hash(), "stock, records, requester IDs and next receipt sequence restore identically");
            c.Equal(Fix.FromInt(4), stock.ReceiveReserved(world.Content, Alice, incomingA, Fix.FromInt(4)), "delivery consumes its own room without release/add window");
            restored.ReceiveReserved(twin.World.Content, Alice, incomingA, Fix.FromInt(4));
            c.Equal(Fix.FromInt(3), stock.ReservedIn(steel), "another requester's room remains protected");
            c.Equal(Fix.FromInt(14), stock.Amount(steel), "received physical quantity enters actual stock once");
            c.Equal(Fix.FromInt(6), stock.TakeReserved(Alice, outgoing, Fix.FromInt(6)), "pickup consumes its owned stock receipt exactly");
            restored.TakeReserved(Alice, outgoing, Fix.FromInt(6));
            c.Equal(Fix.FromInt(8), stock.Amount(steel), "pickup quantity leaves the only stock authority once");
            c.Equal(Fix.Zero, stock.ReservedOut(steel), "consumed pickup no longer promises stock");
            before = game.Hash();
            c.Throws<InvalidOperationException>(() => stock.ReceiveReserved(world.Content, Alice, incomingA, Fix.FromInt(4)), "consumed delivery cannot be repeated");
            c.Throws<InvalidOperationException>(() => stock.TakeReserved(Alice, outgoing, Fix.FromInt(6)), "consumed pickup cannot be repeated");
            c.Equal(before, game.Hash(), "repeated consume changes no accounting");
            stock.ReleaseIn(Bob, incomingB); restored.ReleaseIn(Bob, incomingB);
            c.Throws<InvalidOperationException>(() => stock.ReleaseIn(Bob, incomingB), "double release cannot conceal lost accounting");
            ReservationToken next = stock.ReserveIn(Alice, steel, Fix.One);
            ReservationToken nextTwin = restored.ReserveIn(Alice, steel, Fix.One);
            c.That(next.Sequence > outgoing.Sequence && next.Equals(nextTwin), "released tokens are never reused, including after load");
            c.Equal(game.Hash(), twin.Hash(), "exact consume/release/reacquire continues identically");
            c.Equal(3, retained.Count, "retained facts remain detached after the records change");
        }
        private static Job FindHaul(World world, EntityId destination)
        {
            foreach (Job job in world.Services.Get<WorkBoard>().Jobs)
                if (job.IsLive && job.IsHaul && job.To == destination) return job;
            return null;
        }
        private static void Until(SimGame game, SimGame twin, Func<bool> done, int limit, string stage)
        {
            for (int i = 0; i < limit && !done() && game.Fault == null && (twin == null || twin.Fault == null); i++)
            { game.Update(); twin?.Update(); }
            if (!done() || game.Fault != null || twin?.Fault != null)
                throw new InvalidOperationException(stage + " stalled at tick " + game.Tick + "; fault=" + game.Fault + "; twin=" + twin?.Fault);
        }
        private static void Physical(Check c)
        {
            GameFactory factory = Colony.Factory(); SimGame game = factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 41 });
            World world = game.World; TileMap map = (TileMap)world.Space; ResourceId steel = world.Content.ResourceId("steel");
            Entity destination = map.SpawnAt(world, world.Content.Get<EntityDef>("module_port"), new PlayerId(0), new TileCoord(12, 18));
            IInventory dest = destination.Get<IInventory>(); dest.Remove(steel, dest.Amount(steel));
            c.That(TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = destination.Id, Resource = steel, Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(20), Fix.FromInt(20), Fix.FromInt(20), 45, LogisticsReserveAccess.Protected)) }).Accepted,
                "a normal player stock policy requests physical delivery");
            Until(game, null, () => FindHaul(world, destination.Id) != null, 30, "plan owned haul");
            Job job = FindHaul(world, destination.Id); IInventory source = world.Entities.Get(job.From).Get<IInventory>();
            c.That(!job.Purchase && job.OutReservation.IsValid && job.InReservation.IsValid, "existing job owns pickup/admission receipts");
            c.Equal(Fix.Zero, dest.Amount(steel), "reservation and travel do not teleport resources");
            c.That(source.Reservations[0].Owner.Equals(job.ReservationOwner), "source record names the existing job, not a second coordinator balance");
            ulong before = game.Hash();
            c.Throws<InvalidOperationException>(() => dest.ReleaseIn(new ReservationOwner("sim.haul", job.Id + 1), job.InReservation),
                "another job cannot release this delivery's capacity");
            c.Equal(before, game.Hash(), "foreign job release is a refusal, not a partial transfer");
            ReservationToken actualReceipt = job.InReservation;
            job.InReservation = new ReservationToken(actualReceipt.Endpoint, actualReceipt.Sequence + 1);
            c.Throws<CorruptStateException>(() => factory.Load(game.Save()), "saved job cannot reference a missing or different endpoint receipt");
            job.InReservation = actualReceipt;
            c.Equal(before, game.Hash(), "restoring the fault-injected reference leaves the real commitment intact");
            c.Throws<InvalidOperationException>(() => world.Services.Get<IHaulExecution>().Load(world, job,
                world.Entities.Get(job.Worker), world.Entities.Get(job.From), world.Content.ResourceId("water"), job.Amount),
                "physical pickup cannot substitute another resource for a valid owned token");
            c.Equal(before, game.Hash(), "mismatched physical pickup refuses before stock or receipt consumption");
            SimGame twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "travel and both owned receipts restore identically");
            Until(game, twin, () => job.Loaded.IsPositive, 2880, "owned physical pickup");
            c.That(!job.OutReservation.IsValid && job.InReservation.IsValid, "pickup consumes only source token; destination remains reserved");
            c.Equal(Fix.FromInt(30), source.Amount(steel), "exact load removes 20 from actual source stock");
            Entity carrier = world.Entities.Get(job.Worker); IInventory cargo = carrier.Get<ITransporter>().Cargo;
            c.Equal(Fix.FromInt(20), cargo.Amount(steel), "load is in the actual carrier before delivery");
            c.Equal(Fix.Zero, dest.Amount(steel), "destination remains empty until physical arrival");
            c.Equal(game.Hash(), twin.Hash(), "pickup state and token consumption continue identically");
            twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "loaded cargo and unspent admission receipt save together");
            Until(game, twin, () => dest.Amount(steel) == Fix.FromInt(20), 2880, "owned physical delivery");
            c.Equal(game.Hash(), twin.Hash(), "saved loaded delivery commits identically");
            c.Equal(Fix.Zero, cargo.Amount(steel), "committed resource leaves the actual carrier once");
            c.Equal(Fix.Zero, dest.ReservedIn(steel), "delivery consumes its own capacity receipt");
            c.That(!job.OutReservation.IsValid && !job.InReservation.IsValid, "completed job carries no live receipt");
            c.Equal(Fix.FromInt(50), source.Amount(steel) + cargo.Amount(steel) + dest.Amount(steel), "physical transfer conserves the authored resource");

            Entity other = map.SpawnAt(world, world.Content.Get<EntityDef>("module_port"), new PlayerId(0), new TileCoord(20, 28));
            other.Get<IInventory>().Remove(steel, Fix.FromInt(30));
            c.That(TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = other.Id, Resource = steel, Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(10), Fix.FromInt(10), Fix.FromInt(10), 45, LogisticsReserveAccess.Protected)) }).Accepted,
                "a later normal demand acquires new owned receipts");
            Until(game, null, () => FindHaul(world, other.Id) != null, 30, "plan removed endpoint haul");
            Job cancelled = FindHaul(world, other.Id); Entity removedSource = world.Entities.Get(cancelled.From);
            world.Despawn(removedSource); // Engine fault-injection fixture: endpoint disappearance during physical travel.
            game.Update(); twin = factory.Load(game.Save());
            Until(game, twin, () => !cancelled.IsLive, 2880, "removed pickup endpoint cancellation");
            c.Equal(Fix.Zero, other.Get<IInventory>().ReservedIn(steel), "missing source releases only the surviving owned destination receipt");
            c.That(!cancelled.InReservation.IsValid && !cancelled.OutReservation.IsValid, "cancelled job clears its own receipt references");
            c.Equal(game.Hash(), twin.Hash(), "cancellation cleanup after endpoint disappearance is saved-deterministic");
            c.That(game.Fault == null && twin.Fault == null, "bounded physical ownership stages complete without faults");
            CapabilityTests.ExternalProduction(c); // Directly affected independent inventory/production promise path.
        }
    }
}
