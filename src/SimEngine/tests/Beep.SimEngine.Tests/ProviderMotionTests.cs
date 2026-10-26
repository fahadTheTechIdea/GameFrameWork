using System;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    public static class ProviderMotionTests
    {
        public static void Register()
        {
            TestRunner.Add("Provider motion: non-tile work, home travel and saved lifecycle", Work);
            TestRunner.Add("Provider motion: physical cargo survives interrupted non-tile access", Cargo);
        }
        private static Entity Find(SimGame game, string definition)
        {
            foreach (Entity entity in game.World.Entities.All) if (entity.Def.Id == definition) return entity;
            throw new InvalidOperationException("missing proof entity " + definition);
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 7 });
        private static void Reach(SimGame game, Func<bool> condition, int ticks, string stage, SimGame twin = null)
        {
            for (int i = 0; i < ticks && !condition(); i++) { game.Update(); twin?.Update(); }
            if (!condition()) throw new InvalidOperationException(stage + " stalled at tick " + game.Tick + "; fault=" + game.Fault
                + "; position=" + Find(game, "machine").Get<PointPresence>().Position);
        }
        private static Fix Stock(SimGame game, string entity)
            => Find(game, entity).Get<IInletPort>().Amount(game.World.Content.ResourceId("supply"));
        private static Fix Total(SimGame game) => Stock(game, "source") + Stock(game, "destination") + Stock(game, "machine");

        private static void Work(Check c)
        {
            GameFactory factory = PointGame.Factory(); SimGame game = New(factory);
            Entity machine = Find(game, "machine"), target = Find(game, "destination"), home = Find(game, "source");
            PointMotion motion = machine.Get<PointMotion>(); PointPresence presence = machine.Get<PointPresence>();
            c.That(game.World.Space is PointSpace && game.Snapshot.Space is PointWorldSnapshot, "scenario selects an ordinary external spatial provider");
            c.That(game.World.Content.HasModule(MotionModule.ModuleId), "non-tile work explicitly selects the coordinate-neutral motion owner");
            c.That(!game.World.Content.HasModule(TileNavigationModule.ModuleId), "point movement does not select the tile-navigation module");
            c.That(!game.World.Services.TryGet<TileMap>(out _) && !game.World.Services.TryGet<NavService>(out _), "no tile map or navigation service is composed");
            c.That(machine.Get<IMobility>() == null && machine.Get<ITilePresence>() == null, "non-tile machine declares no tile contract");
            c.That(ReferenceEquals(motion, machine.Get<IMotion>()), "one capability owns generic physical motion");
            c.Equal(new Code("point.ready"), machine.State, "Initialize settles the authored initial state");
            c.That(TestKit.Order(game, 0, new SetHomeCommand { Worker = machine.Id, Home = home.Id }).Accepted, "player bases machine through ordinary engine command");
            c.Equal(Code.None, game.Seat(0).Preview(new PostOrderCommand { Plan = "inspect", Target = target.Id }), "engine preview accepts compatible work");
            c.That(TestKit.Order(game, 0, new PostOrderCommand { Plan = "inspect", Target = target.Id }).Accepted, "command executes the same work rule");
            Order order = game.World.Services.Get<WorkBoard>().Orders[0];
            Reach(game, () => motion.Status == MoveStatus.Moving, 5, "motion admission");
            c.That(presence.Position > Fix.Zero && presence.Position < Fix.FromInt(10), "provider approaches physically rather than teleporting");
            c.Equal(0L, order.Nodes[0].Progress.WholeTicks, "distant work has no progress before arrival");
            c.Equal(1L, motion.TicksMoving, "existing movement phase advances a non-tile owner exactly once");
            c.Equal(new Code("point.travel"), machine.State, "entity Update commits the provider's authored travel state");
            PointSnapshot retained = null;
            foreach (EntityView view in game.Snapshot.Entities) if (view.Id == machine.Id) retained = (PointSnapshot)view.Spatial;
            c.That(retained != null && retained.Previous == Fix.Zero && retained.Position == presence.Position, "snapshot contains provider-owned current and previous coordinates");
            Fix retainedPosition = retained.Position;
            SimGame twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "non-tile motion/presence/jobs restore through their existing owners");
            Reach(game, () => order.Nodes[0].Progress.WholeTicks > 0, 20, "physical arrival", twin);
            c.Equal(Fix.FromInt(10), presence.Position, "work begins only at the target's actual position");
            c.That(order.IsActive && order.Nodes[0].Progress.WholeTicks < 6, "arrival starts timed work instead of completing it instantly");
            c.Equal(new Code("point.processing"), machine.State, "ordinary worker state settles after motion stops");
            c.Equal(retainedPosition, retained.Position, "retained immutable coordinates do not follow live motion");
            c.Equal(game.Hash(), twin.Hash(), "saved moving twins reach and work identically");
            Reach(game, () => order.State == OrderState.Done, 12, "work completion", twin);
            c.Equal(6L, order.Nodes[0].Progress.WholeTicks, "all authored work ticks are executed");
            c.That(motion.Status == MoveStatus.Moving && presence.Position < Fix.FromInt(10), "idle behavior returns home through provider motion");
            Reach(game, () => presence.Position == Fix.Zero && motion.Status == MoveStatus.Arrived, 20, "home return", twin);
            c.Equal(new Code("point.ready"), machine.State, "home arrival releases active motion state");
            c.Equal(game.Hash(), twin.Hash(), "completed work and home travel remain deterministic after restore");

            SimGame other = New(factory);
            c.Equal(SpatialInteractionStatus.Unsupported, SpatialInteraction.Approach(game.World, machine, Find(other, "destination")), "foreign session endpoints refuse before provider invocation");
            c.Equal(Fix.Zero, Find(other, "machine").Get<PointPresence>().Position, "interleaved sessions share no physical state");
            c.Equal(Codes.UnknownCommand, game.Seat(0).Preview(new MoveVehicleCommand { Vehicle = machine.Id, To = new TileCoord(1, 1) }),
                "unselected tile-navigation command refuses explicitly without a missing-service exception");

            SimGame tiles = Colony.New(); Entity crane = tiles.World.Entities.Get(Colony.Mine(tiles, 0, "crane")[0].Id);
            c.That(ReferenceEquals(crane.Get<Mover>(), crane.Get<IMotion>()) && ReferenceEquals(crane.Get<IMobility>(), crane.Get<IMotion>()),
                "tile navigation inherits generic motion with one component owner");
            c.That(TestKit.Order(tiles, 0, new MoveVehicleCommand { Vehicle = crane.Id, To = new TileCoord(15, 5) }).Accepted, "tile movement still uses its provider-specific command");
            c.Equal(1L, crane.Get<Mover>().TicksMoving, "inherited tile motion is not advanced twice");
            c.That(crane.Get<IMotion>().Status == MoveStatus.Moving && crane.State == crane.Get<Mover>().Def.States["moving"].State,
                "tile motion and entity-owned state remain on the same lifecycle");
        }

        private static void Cargo(Check c)
        {
            GameFactory factory = PointGame.Factory(); SimGame game = New(factory);
            Entity machine = Find(game, "machine"), destination = Find(game, "destination"), source = Find(game, "source");
            ResourceId resource = game.World.Content.ResourceId("supply"); PointPresence presence = machine.Get<PointPresence>();
            c.That(machine.Get<ITransporter>().Cargo is IOutletPort && machine.Get<ITransporter>().Cargo is IInletPort,
                "non-tile carrier uses the same cargo authority and outlet/inlet invariant");
            c.Equal(Fix.FromInt(20), Total(game), "initial supply is authored in JSON");
            c.That(TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = destination.Id, Resource = resource, Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(16), Fix.FromInt(16), Fix.FromInt(16), 45, LogisticsReserveAccess.Protected)) }).Accepted,
                "standard stock policy plans physical non-tile hauling");
            Reach(game, () => Stock(game, "machine") > Fix.Zero && presence.Position > Fix.Zero, 16, "cargo pickup and departure");
            c.Equal(Fix.FromInt(8), Stock(game, "machine"), "carrier capacity limits the physical load");
            c.Equal(Fix.Zero, Stock(game, "destination"), "cargo is not delivered before travel");
            c.That(source.Get<IOutletPort>().ReservedOut(resource) == Fix.Zero && destination.Get<IInletPort>().ReservedIn(resource) == Fix.FromInt(8),
                "only the selected carrier's active physical load holds endpoint commitments");
            c.Equal(Fix.FromInt(20), Total(game), "loading moves stock without creating resources");
            Fix midRoute = presence.Position;
            c.That(TestKit.Order(game, 0, new SetPointAccessCommand { Open = false }).Accepted, "engine module changes actual route access");
            c.Equal(midRoute, presence.Position, "closing a route freezes position instead of jumping to an endpoint");
            c.Equal(MoveStatus.Unreachable, machine.Get<IMotion>().Status, "motion provider reports blocked physical travel");
            c.Equal(new Code("point.blocked"), machine.State, "blocked provider state settles through the entity resolver");
            c.Equal(Fix.FromInt(8), Stock(game, "machine"), "interrupted loaded work retains stranded cargo");
            SimGame twin = factory.Load(game.Save());
            c.That(!((PointWorldSnapshot)twin.Snapshot.Space).AccessOpen, "route access restores in the selected space's sole codec");
            c.Equal(game.Hash(), twin.Hash(), "in-flight interruption saves with ordinary jobs, reservations and cargo");
            for (int i = 0; i < 5; i++) { game.Update(); twin.Update(); }
            c.Equal(midRoute, presence.Position, "blocked updates cannot move or deliver cargo remotely");
            c.Equal(Fix.Zero, Stock(game, "destination"), "blocked destination receives no invented supply");
            c.Equal(Fix.FromInt(20), Total(game), "cancelled/reopened jobs neither discard nor duplicate cargo");
            c.That(TestKit.Order(game, 0, new SetPointAccessCommand { Open = true }).Accepted, "player restores access through a real engine command");
            TestKit.Order(twin, 0, new SetPointAccessCommand { Open = true });
            Reach(game, () => Stock(game, "destination") == Fix.FromInt(16) && Stock(game, "machine") == Fix.Zero, 160, "cargo recovery and delivery", twin);
            c.Equal(Fix.FromInt(4), Stock(game, "source"), "ordinary recovery/replanning supplies the target without duplication");
            c.Equal(Fix.FromInt(20), Total(game), "all authored stock is conserved after recovery");
            c.Equal(game.Hash(), twin.Hash(), "restored non-tile logistics continues identically across route recovery");
            c.That(game.Fault == null && twin.Fault == null, "physical dispatch finishes within its bounded scenario without an engine fault");
        }
    }
}
