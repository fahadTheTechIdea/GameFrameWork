using System.Linq;
using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class LiveCapacityTests
    {
        public static void Register()
        {
            TestRunner.Add("Live capacity: measured stock, promises, exact offers and saved admission", Inventory);
            TestRunner.Add("Live capacity: whole production, physical build, hauling and network continuation", Physical);
        }
        private static readonly ResourceAmount[] Empty = Array.Empty<ResourceAmount>();
        private static Fix Projected(CapacityCheck check, string unit)
        {
            foreach (CapacityUsage usage in check.Usage) if (usage.Unit == new UnitId(unit)) return usage.Projected;
            throw new InvalidOperationException("missing usage " + unit + ": " + check.Status);
        }
        private static void Inventory(Check c)
        {
            string workflow = FrameworkProofContent.Read("resource-workflow.json").Replace("\"entities\": [", @"""entities"": [
              { ""id"": ""mixed.bin"", ""kind"": ""entity"", ""components"": { ""storage"": { ""capacity_policy"": ""cargo.combined"" } } },
              { ""id"": ""closed.bin"", ""kind"": ""entity"", ""components"": { ""storage"": { ""capacity_policy"": ""cargo.closed"" } } },
              { ""id"": ""slot.bin"", ""kind"": ""entity"", ""components"": { ""storage"": { ""capacity_policy"": ""cargo.slots"" } } },");
            GameFactory factory = CapacityPolicyTests.Factory(new ResourceProofModule(), workflow: workflow);
            SimGame game = factory.NewGame(new NewGameRequest { Scenario = "resource-work", Seed = 23 });
            World world = game.World; ResourceId ore = world.Content.ResourceId("planetx.ore"), crate = world.Content.ResourceId("crate");
            ResourceId coolant = world.Content.ResourceId("measured.coolant");
            Entity entity = world.Spawn(world.Content.Get<EntityDef>("mixed.bin"), new PlayerId(0));
            IInventory stock = entity.Get<IInventory>();
            c.That(stock is Storage && ReferenceEquals(stock, entity.Get<IInletPort>()) && ReferenceEquals(stock, entity.Get<IOutletPort>()), "all port and stock contracts share one authority");
            c.Equal(Fix.FromInt(2), stock.Add(ore, Fix.FromInt(2)), "native ore quantity is admitted against two measured dimensions");
            c.Equal(Fix.One, stock.Receive(world.Content, coolant, Fix.One), "common measured inventory can hold authored liquid without a binary solid/fluid rule");
            ReservationToken inbound = stock.ReserveIn(new ReservationOwner("proof.capacity", 1), crate, Fix.One);
            c.Equal(Fix.One, stock.ReservedIn(crate), "inbound commitment stays in the existing provider");
            c.Equal(Fix.FromInt(3), Projected(stock.CheckIncoming(Empty), "unit.litre"), "promised crate volume is counted beside actual bulk fluids/ore");
            c.Equal(Fix.FromInt(2), stock.Offer(ore, Fix.FromInt(100)), "offer returns native ore constrained by remaining volume, not summed amounts");
            c.Equal(Fix.FromInt(2), stock.Add(ore, Fix.FromInt(100)), "Add cannot consume capacity promised to another resource");
            c.Equal(Fix.Zero, stock.Receive(world.Content, crate, Fix.One), "a reserved delivery must release its own promise before physical admission");
            stock.ReleaseIn(new ReservationOwner("proof.capacity", 1), inbound);
            c.Equal(Fix.One, stock.Receive(world.Content, crate, Fix.One), "normal release/admit sequence fills the preserved capacity once");
            c.Equal(Fix.FromInt(4), Projected(stock.CheckIncoming(Empty), "unit.litre"), "volume boundary is exact after delivery");
            c.Equal(Fix.FromInt(6) + Fix.Half, Projected(stock.CheckIncoming(Empty), "unit.kg"), "mass remains an independent measure of mixed native units");
            c.Equal(Fix.Zero, stock.Add(ore, Fix.One), "full volume prevents insertion despite free mass capacity");
            c.Equal(Fix.Zero, stock.Add(crate, Fix.Half), "an exact add of fractional discrete native quantity refuses");
            c.Equal(CapacityStatus.FractionalDiscrete, stock.CheckIncoming(new[] { new ResourceAmount(crate, Fix.Half) }).Status, "whole-intention check exposes typed fractional count refusal");
            c.Throws<InvalidOperationException>(() => stock.ReserveIn(new ReservationOwner("proof.capacity", 2), crate, Fix.One), "capacity cannot be promised beyond any independent limit");
            ReservationToken outbound = stock.ReserveOut(new ReservationOwner("proof.capacity", 1), ore, Fix.FromInt(4));
            c.Equal(Fix.Zero, stock.Remove(ore, Fix.One), "direct Remove cannot steal promised pickup stock");
            var bill = new Bill(new[] { new ResourceAmount(ore, Fix.One), new ResourceAmount(crate, Fix.One) });
            ulong protectedHash = game.Hash();
            var consume = new ResourceInventoryIntent(new ReservationOwner("proof.capacity", entity.Id.Value), entity.Owner,
                entity.Id, bill, ResourceFlowKind.Consumed, new Code("proof.consume"));
            c.That(!world.Services.Get<IResourceTransaction>().CommitInventory(consume).Committed, "whole bill cannot consume another job's promised stock");
            c.Equal(protectedHash, game.Hash(), "refused whole consumption changes no stock or promises");
            c.Equal(Fix.One, stock.Amount(crate), "compatible unpromised line is not partially consumed");
            stock.ReleaseOut(new ReservationOwner("proof.capacity", 1), outbound);
            c.That(world.Services.Get<IResourceTransaction>().CommitInventory(consume).Committed, "released stock permits the entire native-unit bill");
            c.Equal(Fix.FromInt(3), stock.Amount(ore), "consumption removes the declared resource quantity once");
            c.Equal(Fix.Zero, stock.Amount(crate), "all bill lines commit together after validation");
            c.Throws<InvalidOperationException>(() => stock.ReleaseOut(new ReservationOwner("proof.capacity", 1), outbound), "invalid release no longer clamps lost accounting");
            c.Throws<InvalidOperationException>(() => stock.ReserveOut(new ReservationOwner("proof.capacity", 2), ore, Fix.FromInt(4)), "source stock cannot be overpromised");
            inbound = stock.ReserveIn(new ReservationOwner("proof.capacity", 1), crate, Fix.One); stock.ReserveOut(new ReservationOwner("proof.capacity", 1), ore, Fix.One);
            c.Throws<NotSupportedException>(() => ((IList<ResourceAmount>)stock.InboundPromises).Clear(), "promise facts cannot mutate the authoritative list");
            CapacityCheck prior = stock.CheckIncoming(Empty); ulong previewHash = game.Hash();
            c.Equal(CapacityStatus.LimitExceeded, stock.CheckIncoming(new[] { new ResourceAmount(crate, Fix.FromInt(2)) }).Status, "whole future input refuses without inserting a compatible prefix");
            c.Equal(previewHash, game.Hash(), "capacity preview does not acquire or consume");
            c.That(game.Seat(0).View.Feature<InventoryView>().TryGetCapacity(entity.Id, Code.None, out CapacityCheck ownerFacts), "owner gets live detached dimension facts");
            c.That(!PlayerView.Create(world, new PlayerId(1)).Feature<InventoryView>().TryGetCapacity(entity.Id, Code.None, out _), "another company cannot inspect stock or promise occupancy");
            c.Equal(Projected(prior, "unit.kg"), Projected(ownerFacts, "unit.kg"), "view and execution share one evaluator");
            c.Equal(Upgrades.InventoryReserved, Upgrades.Check(world, entity, world.Content.Get<EntityDef>("closed.bin")), "replacement cannot copy owned commitments onto a new identity");
            SimGame twin = factory.Load(game.Save());
            c.Equal(EngineInfo.SaveFormat, SaveGame.Describe(game.Save()).Format, "changed resource admission source rejects the old save schema");
            c.Equal(game.Hash(), twin.Hash(), "mixed quantities and both existing promise lists restore identically");
            IInventory restored = twin.World.Entities.Get(entity.Id).Get<IInventory>();
            stock.ReleaseIn(new ReservationOwner("proof.capacity", 1), inbound); restored.ReleaseIn(new ReservationOwner("proof.capacity", 1), inbound);
            stock.Receive(world.Content, crate, Fix.One); restored.Receive(twin.World.Content, crate, Fix.One);
            game.RunTicks(4); twin.RunTicks(4);
            c.Equal(game.Hash(), twin.Hash(), "physical admission and lifecycle continuation match after load");
            c.Equal(Projected(prior, "unit.kg"), Projected(ownerFacts, "unit.kg"), "retained capacity projection does not become a live balance");

            Entity empty = world.Spawn(world.Content.Get<EntityDef>("mixed.bin"), new PlayerId(0)); IInventory exact = empty.Get<IInventory>();
            c.Equal(Fix.FromRaw(2), exact.Offer(ore, Fix.FromRaw(3)), "offer chooses the greatest exact representable capacity quantum");
            c.Equal(Fix.Zero, exact.Add(ore, Fix.FromRaw(1)), "unsupported measured precision inserts nothing");
            c.Equal(Fix.FromRaw(2), exact.Add(ore, Fix.FromRaw(2)), "a representable small measured stock is admitted");
            c.Equal(Fix.Zero, exact.Remove(ore, Fix.FromRaw(1)), "pickup cannot leave an unrepresentable measured remainder");
            world.Settle();
            c.That(!world.Services.Get<IResourceTransaction>().CommitInventory(new ResourceInventoryIntent(
                new ReservationOwner("proof.capacity", empty.Id.Value), empty.Owner, empty.Id,
                new Bill(new[] { new ResourceAmount(ore, Fix.FromRaw(1)) }), ResourceFlowKind.Consumed, new Code("proof.consume"))).Committed,
                "whole consumption rejects unsupported measured precision without partial removal");
            c.Throws<InvalidOperationException>(() => exact.ReserveOut(new ReservationOwner("proof.capacity", 3), ore, Fix.FromRaw(1)), "outbound promise must preserve the provider's exact native quantum");
            c.Equal(Fix.FromRaw(2), exact.Take(ore, Fix.FromRaw(3)), "pickup ceiling yields the largest representable actual stock quantity");
            Entity slotEntity = world.Spawn(world.Content.Get<EntityDef>("slot.bin"), new PlayerId(0)); IInventory slots = slotEntity.Get<IInventory>();
            c.Equal(Fix.One, slots.Offer(crate, Fix.One + Fix.Half), "offer ceiling may be floored to whole native pieces");
            c.Equal(Fix.Zero, slots.Add(crate, Fix.One + Fix.Half), "an exact native insertion cannot silently accept a fractional count intention");
            c.Equal(Fix.FromInt(3), slots.Add(crate, Fix.FromInt(5)), "actual admission inserts the exact whole count that fits");
            c.Equal(Fix.Zero, slots.Offer(crate, Fix.One), "filled count policy has no native offer");
            ICapacityPolicy policy = world.Content.Get<CapacityPolicyDef>("cargo.volume");
            c.Equal(Fix.FromInt(12), policy.Offer(Empty, Empty, coolant, Fix.FromInt(100), 3).Amount, "instance count scales authored equipment, not a duplicated pool algorithm");
            c.Equal(CapacityStatus.InvalidRequest, policy.Check(Empty, Empty, Empty, 0).Status, "zero capacity instances are not fabricated equipment");
            c.Equal(CapacityStatus.OutOfRange, policy.Check(Empty, Empty, Empty, long.MaxValue).Status, "instance scaling refuses arithmetic range overflow");
            c.That(typeof(IInventory).GetProperty("Total") == null && typeof(IInletPort).GetProperty("Capacity") == null &&
                typeof(IInletPort).GetProperty("FreeSpace") == null && typeof(Bill).GetProperty("Total") == null,
                "obsolete mixed-resource scalar APIs have no aliases");
            c.That(typeof(StorageDef).GetProperty("Fluid") == null && typeof(NetworkDef).GetProperty("CapacityPerTile") == null, "old binary and untyped capacity definition paths are removed");
        }

        private static void Reach(SimGame game, SimGame twin, Func<bool> done, int limit, string stage)
        {
            for (int i = 0; i < limit && !done() && game.Fault == null && twin.Fault == null; i++) { game.Update(); twin.Update(); }
            if (!done() || game.Fault != null || twin.Fault != null)
                throw new InvalidOperationException(stage + " stalled at tick " + game.Tick + "; fault=" + game.Fault + "; twin=" + twin.Fault);
        }
        private static void Physical(Check c)
        {
            CapabilityTests.ExternalProduction(c);
            CapabilityTests.IncompatibleProduction(c);
            OptionalWorkTests.Physical(c);
            GameFactory factory = Colony.Factory(); SimGame game = factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 29 });
            World world = game.World; TileMap map = (TileMap)world.Space; ResourceId water = world.Content.ResourceId("water");
            Entity source = map.SpawnAt(world, world.Content.Get<EntityDef>("tank"), new PlayerId(0), new TileCoord(20, 24));
            Entity destination = map.SpawnAt(world, world.Content.Get<EntityDef>("sink"), new PlayerId(0), new TileCoord(26, 24));
            c.Equal(Fix.FromInt(100), source.Get<IInventory>().Add(water, Fix.FromInt(100)), "network source uses authored volume capacity");
            var path = new List<TileCoord>(); for (int x = 22; x <= 25; x++) path.Add(new TileCoord(x, 25));
            c.That(TestKit.Order(game, 0, new LayLineCommand { Line = "pipe", Tiles = path }).Accepted, "pipe construction uses the generated per-line authored work buffer");
            game.RunTicks(17); SimGame twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "physical line and measured stores save during execution");
            Reach(game, twin, () => destination.Get<IInventory>().Amount(water) == Fix.FromInt(100), 2880, "measured network transfer");
            c.Equal(game.Hash(), twin.Hash(), "per-tile pool capacity and physical network flow continue identically");
            c.Equal(Fix.Zero, source.Get<IInventory>().Amount(water), "source balance is not duplicated by network admission");
            c.Equal(Fix.Zero, world.Services.Providers<NetworkService>().Single().Component("waternet", 0).Amount(water), "all actual water eventually leaves the sole network pool");
            c.That(destination.Get<IInventory>().CheckIncoming(Empty).Fits, "final destination respects its authored dimensional policy");
            world.Services.Get<IResourceRecovery>().ReleaseYield(world, new PlayerId(0), new TilePlacement(world.Space.Id, new TileCoord(8, 28), FixVec2.Centre(new TileCoord(8, 28))), new Bill(new[] {
                new ResourceAmount(world.Content.ResourceId("steel"), Fix.FromInt(1500)) }),
                new ReservationOwner("proof.capacity-yield", 1), new Code("proof.authored-yield"));
            game.Update(); // Publish queued entities through the actual lifecycle, not an internal Flush.
            List<EntityView> piles = Colony.Mine(game, 0, game.Seat(0).View.Feature<Beep.SimEngine.Views.ConstructionView>().RecoveryContainer);
            c.Equal(2, piles.Count, "oversized salvage splits into physical authored-policy-sized piles");
            Fix salvaged = Fix.Zero;
            foreach (EntityView pile in piles)
            {
                IInventory inventory = world.Entities.Get(pile.Id).Get<IInventory>();
                salvaged += inventory.Amount(world.Content.ResourceId("steel"));
                c.That(inventory.CheckIncoming(Empty).Fits, "each salvage buffer respects its file-authored limit");
            }
            c.Equal(Fix.FromInt(1500), salvaged, "salvage overflow never silently clamps away resources");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "split salvage uses the existing stock/placement save owners");
            var smallContent=Json.Parse(Colony.Defs);
            for(int i=0;i<smallContent.Get("entities").Count;i++) {
                var entry=smallContent.Get("entities")[i];
                if(entry.Get("id").Text=="sim.line-work.road") entry.Get("components").Get("storage").Set("capacity_policy",JsonValue.Of("fixture.count.1"));
            }
            GameFactory smallFactory = Colony.Factory(smallContent.ToString());
            SimGame small = smallFactory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 3 });
            var oversized = new List<TileCoord>(); for (int x = 5; x < 7; x++) oversized.Add(new TileCoord(x, 26));
            var tooLarge = new LayLineCommand { Line = "road", Tiles = oversized }; ulong before = small.Hash();
            c.Equal(Codes.InvalidArgument, small.Seat(0).Preview(tooLarge), "oversized whole line bill refuses before posting an impossible physical order");
            c.Equal(before, small.Hash(), "authored work-buffer preview mutates no game state");
            c.That(!TestKit.Order(small, 0, tooLarge).Accepted, "command shares the same measured work-buffer refusal");
            c.That(game.Fault == null && twin.Fault == null, "all bounded production/construction/line/network stages are fault free");
        }
    }
}
