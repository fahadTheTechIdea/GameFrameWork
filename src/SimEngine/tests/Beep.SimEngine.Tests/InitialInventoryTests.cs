using System;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class InitialInventoryTests
    {
        public static void Register()
            => TestRunner.Add("Initial inventory: authored creation, private checkpoints and replacement", Creation);
        private static NewGameRequest Request(bool flows = true)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 73 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human);
            if (flows) request.Mods.Add(ResourceFlowsModule.ModuleId); return request;
        }
        private static Entity Bin(SimGame game, int owner)
        {
            foreach (Entity entity in game.World.Entities.All)
                if (entity.Alive && entity.Owner == new PlayerId(owner) && entity.Def.Id == "bin") return entity;
            throw new InvalidOperationException("missing initial bin");
        }
        private static ResourceFlowPage Read(SimGame game, int owner = 0, long cursor = 0)
            => game.Seat(owner).View.Feature<ResourceFlowView>().Read(new PlayerId(owner), cursor);
        private static void Tick(SimGame game)
        { game.Update(); if (game.Fault != null) throw new InvalidOperationException("initial inventory tick " + game.Tick + ": " + game.Fault); }
        private static void Creation(Check c)
        {
            string json = FrameworkProofContent.Read("initial-inventory.json"); GameFactory factory = PointGame.Factory(json);
            SimGame game = factory.NewGame(Request()); World world = game.World; Entity bin = Bin(game, 0);
            ResourceId alpha = world.Content.ResourceId("alpha"), beta = world.Content.ResourceId("beta"); IInventory stock = bin.Get<IInventory>();
            c.That(world.Space == null && !world.Services.Has<IEconomy>(), "creation requires no game-specific world or economy");
            c.Equal(0L, game.Tick, "authored stock is ready before the first update");
            c.Equal(Fix.FromInt(10), stock.Amount(alpha), "whole initial count bill reaches its actual owner");
            c.Equal(Fix.FromInt(20), stock.Amount(beta), "whole initial mass bill preserves native quantities");
            c.That(stock is IEntityInitialization, "built-in creation follows the public capability lifecycle");
            c.That(!world.Events.HasPending, "startup drains initial facts before checkpoint");
            c.Equal(0, stock.Reservations.Count, "initialization fabricates no physical transfer promises");
            ResourceFlowPage page = Read(game); c.Equal(2, page.Entries.Count, "each verified initial line reaches the existing journal");
            foreach (var entry in page.Entries)
            {
                ResourceFlowFact fact = entry.Item;
                c.Equal(ResourceFlowKind.Initialized, fact.Kind, "opening fact is not production or movement");
                c.Equal(ResourceEndpointId.ForEntity(bin.Id), fact.Endpoint, "opening fact references the real registered entity");
                c.Equal(new Code("bin"), fact.Cause, "master-listed entity identity is the cause");
                c.Equal(new ReservationOwner("sim.inventory.initial", bin.Id.Value), fact.Owner, "initialization uses the engine requester");
                c.Equal(Fix.Zero, fact.DestinationBefore, "creation starts from actual empty stock");
                c.Equal(fact.Amount, fact.DestinationAfter, "fact contains exact stock delta");
                c.That(fact.DestinationVersionAfter > fact.DestinationVersionBefore, "actual owner version advances");
                c.That(!fact.IsTransfer && !fact.Source.IsValid && !fact.Destination.IsValid, "no invented transfer receipt");
            }
            c.Equal(new BigInteger(Fix.FromInt(10).Raw), page.Totals(alpha).InitializedRaw, "initial totals derive from native facts");
            c.Equal(BigInteger.Zero, page.Totals(alpha).CreatedRaw, "initial stock is not reported as production");
            c.Equal(ResourceFlowReadStatus.NotOwned, game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(1), 0).Status, "company initial history stays private");
            c.Equal(2L, Read(game, 1).Latest, "companies have independent cursors");
            SimGame restored = factory.Load(game.Save()), independent = factory.NewGame(Request());
            c.Equal(game.Hash(), restored.Hash(), "load preserves stock/history without grants");
            c.Equal(game.Hash(), independent.Hash(), "independent sessions create identical initial state");
            c.Equal(2L, Read(restored).Latest, "load does not append fresh opening facts");
            long cursor = page.Latest;
            c.That(Upgrades.TryReplace(world, bin, world.Content.Get<EntityDef>("replacement"), out Entity replaced).IsNone, "existing replacement accepts inventory");
            world.Settle(); Tick(game);
            c.Equal(Fix.FromInt(10), replaced.Get<IInventory>().Amount(alpha), "replacement ignores target starting grant");
            c.Equal(Fix.FromInt(20), replaced.Get<IInventory>().Amount(beta), "replacement preserves actual mass");
            c.That(!bin.Alive && world.Entities.Get(bin.Id) == null, "old entity stops being authoritative");
            c.Equal(cursor + 2, Read(game).Latest, "replacement records physical relocation, not false initialization");
            cursor = Read(game).Latest;
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "replacement uses the existing durable codec");
            Entity external = world.Spawn(world.Content.Get<EntityDef>("external"), new PlayerId(0)); world.Settle();
            c.Equal(Fix.FromInt(7), external.Get<IInventory>().Amount(alpha), "external provider uses the same owner");
            c.That(world.Events.HasPending, "runtime initialization uses deferred event phase");
            c.Throws<InvalidOperationException>(() => game.Save(), "runtime initial facts must drain before checkpoint");
            Tick(game); ResourceFlowPage created = Read(game, 0, cursor);
            c.Equal(1, created.Entries.Count, "external creation records exactly one effect");
            c.Equal(new Code("external"), created.Entries[0].Item.Cause, "external content supplies its cause");
            c.Equal(ResourceEndpointId.ForEntity(external.Id), created.Entries[0].Item.Endpoint, "external inventory keeps its endpoint identity");
            c.Equal(2, page.Entries.Count, "retained startup snapshot remains detached");
            c.Equal(ResourceFlowReadStatus.Expired, Read(game).Status, "bounded history is not complete opening accounting");
            cursor = Read(game).Latest;
            world.Spawn(world.Content.Get<EntityDef>("empty"), new PlayerId(0)); world.Settle();
            c.That(!world.Events.HasPending, "empty initial bill creates no fake fact");
            c.Equal(cursor, Read(game).Latest, "empty creation leaves history sequence unchanged");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "empty inventory is immediately checkpoint-safe");
            Tick(game); Tick(restored); Tick(independent);
            c.Equal(restored.Hash(), independent.Hash(), "interleaved session mutations do not leak state");
            SimGame noJournal = factory.NewGame(Request(false));
            c.Equal(Fix.FromInt(10), Bin(noJournal, 0).Get<IInventory>().Amount(alpha), "initialization does not require optional journal");
            c.Equal(noJournal.Hash(), factory.Load(noJournal.Save()).Hash(), "unsubscribed events leave no phantom payload");
            Validation(c, json); Fault(c, json);
        }
        private static void Validation(Check c, string json)
        {
            Action<string> reject = invalid => c.Throws<ContentException>(() => PointGame.Factory(invalid).NewGame(Request()), "invalid authored initial stock fails before play");
            reject(json.Replace("\"alpha\":10", "\"missing\":10"));
            reject(json.Replace("\"alpha\":10", "\"alpha\":-1"));
            reject(json.Replace("\"alpha\":10", "\"alpha\":0.5"));
            reject(json.Replace("\"beta\":20", "\"beta\":20.5"));
            reject(json.Replace("\"beta\":20", "\"beta\":31"));
            reject(json.Replace("\"initial_stock\":{\"alpha\":10", "\"accepts\":[\"beta\"],\"initial_stock\":{\"alpha\":10"));
            reject(json.Replace("\"initial_stock\":{\"alpha\":10", "\"phases\":[\"liquid\"],\"initial_stock\":{\"alpha\":10"));
            reject(json.Replace("\"initial\":7", "\"initial\":0.5"));
            var initial = PointGame.Factory(json).NewGame(Request());
            var declaration = initial.World.Content.Get<EntityDef>("bin").Component<IInventoryDefinition>();
            c.Throws<NotSupportedException>(() => ((IList<ResourceAmount>)declaration.InitialStock.Lines)[0] = default, "authored initial bill cannot mutate across sessions");
        }
        private static void Fault(Check c, string json)
        {
            string hostile = json.Replace("\"initial\":7", "\"initial\":7,\"receipt_fault\":\"partial_add\"");
            GameFactory factory = PointGame.Factory(hostile); SimGame game = factory.NewGame(Request()); long latest = Read(game).Latest;
            c.Throws<InvalidOperationException>(() => game.World.Spawn(game.World.Content.Get<EntityDef>("external"), new PlayerId(0)), "initial provider must report exact quantity");
            c.Equal("entity.initialize", game.Fault?.Phase, "creation failure identifies engine phase");
            c.That(game.Fault?.Message.Contains("violated exact lifecycle batch") == true, "failure reports real provider violation");
            c.Equal(latest, Read(game).Latest, "unverified creation appends no successful fact");
            c.That(!game.World.Events.HasPending, "failed batch publishes no deferred success");
            c.Throws<InvalidOperationException>(() => game.Save(), "partial initialization cannot be checkpointed");
            c.Throws<InvalidOperationException>(() => game.World.Spawn(game.World.Content.Get<EntityDef>("empty"), new PlayerId(0)), "faulted creation cannot keep spawning");
            c.Equal(0, game.RunTicks(1), "faulted creation cannot keep simulating");
            c.Throws<InvalidOperationException>(() => PointGame.Factory(hostile.Replace("\"entity\":\"bin\",\"owner\":0", "\"entity\":\"external\",\"owner\":0")).NewGame(Request()), "startup failure does not return a partial session");
        }
    }
}
