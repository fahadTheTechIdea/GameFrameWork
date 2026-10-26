using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    internal static class MotionCheckpointTests
    {
        internal static void Register()
        {
            TestRunner.Add("Motion checkpoints: owned route state and staged corruption admission", Admission);
            TestRunner.Add("Motion checkpoints: physical paths, interruption and saved continuation", Physical);
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 709 });
        private static GameFactory ReorderedFactory()
        {
            var root = Content.Json.Parse(Colony.Defs);
            var entities = root.Get("entities");
            for (int entry = 0; entry < entities.Count; entry++)
            {
                JsonValue entity = entities[entry];
                if (entity.Get("id").Text == "crane")
                {
                    var previous = entity.Get("components"); var parts = JsonValue.NewObject(); parts.Set("mover", previous.Get("mover").Clone());
                    for (int i = 0; i < previous.Count; i++) if (previous.KeyAt(i) != "mover") parts.Set(previous.KeyAt(i), previous.ValueAt(i).Clone());
                    entity.Set("components", parts);
                }
            }
            var text = new StringBuilder(); root.WriteCanonical(text); return Colony.Factory(text.ToString());
        }
        private static Entity Crane(SimGame game) => game.World.Entities.Get(Colony.Mine(game, 0, "crane")[0].Id);
        private static void Hold(Check c, SimGame game)
        {
            var actors = new List<Entity>(); foreach (Entity entity in game.World.Entities.All) if (entity.Has<IWorker>()) actors.Add(entity);
            foreach (Entity entity in actors) c.That(TestKit.Order(game, entity.Owner.Value, new MoveVehicleCommand { Vehicle = entity.Id, To = entity.Get<ITilePresence>().Tile }).Accepted, "ordinary commands hold unrelated workers");
            Tick(game);
        }
        private static void Start(Check c, SimGame game, TileCoord to)
        {
            var command = new MoveVehicleCommand { Vehicle = Crane(game).Id, To = to };
            c.Equal(Code.None, game.Seat(0).Preview(command), "normal move preview admits actual goal");
            c.That(TestKit.Order(game, 0, command).Accepted, "normal player command owns travel intent");
        }
        private sealed class Image
        {
            public string Navigation; public int Status, Index; public TileRect Goal; public long Age; public FixVec2 Origin; public List<TileCoord> Path;
            public Image(Mover mover) { Navigation = mover.ReadNavigation(); Status = (int)mover.Status; Index = mover.PathIndex; Goal = mover.Goal; Age = mover.TicksMoving; Origin = mover.RouteOrigin; Path = new List<TileCoord>(mover.Path); }
        }
        private static byte[] Bytes(Image value)
        {
            using (var stream = new MemoryStream())
            {
                var s = new StateWriter(stream); s.String(ref value.Navigation); s.Int(ref value.Status);
                int x = value.Goal.X, y = value.Goal.Y, w = value.Goal.W, h = value.Goal.H;
                s.Int(ref x); s.Int(ref y); s.Int(ref w); s.Int(ref h); s.Count(value.Path.Count);
                foreach (var point in value.Path) { var tile = point; s.Tile(ref tile); }
                s.Int(ref value.Index); s.Long(ref value.Age); s.Vec(ref value.Origin); s.Flush(); return stream.ToArray();
            }
        }
        private static void Read(Mover mover, byte[] bytes) => mover.Sync(new StateReader(bytes, 0, bytes.Length));
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 500 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done()) { var mover = Crane(game).Get<Mover>(); throw new InvalidOperationException(stage + " tick=" + game.Tick + " status=" + mover.Status + " index=" + mover.PathIndex + "/" + mover.Path.Count + " age=" + mover.TicksMoving); }
        }
        private static void Admission(Check c)
        {
            GameFactory factory = Colony.Factory(); SimGame game = New(factory); Hold(c, game);
            Entity crane = Crane(game); Mover mover = crane.Get<Mover>();
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current native motion identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "one current route-origin payload, no legacy reader");
            int codecs = 0;
            foreach (StateCodecRegistration codec in game.World.Content.ComponentCodecs)
                if (codec.Id == "mover") { codecs++; c.Equal(4, codec.SchemaVersion, "actual mover owns selected navigation and route-origin codec4"); }
            c.Equal(1, codecs, "one declared native motion codec, no parallel or legacy owner");
            c.That(game.World.Content.Owns(crane.Def.Component<MoverDef>().SpaceDefinition), "route bounds alias actual immutable authored space");
            foreach (string name in new[] { "Status", "Goal", "PathIndex", "TicksMoving", "RouteOrigin" })
                c.That(!typeof(Mover).GetProperty(name).GetSetMethod(true).IsPublic, "native " + name + " has no public setter");
            c.That(mover is IMotion && mover is IMobility && mover is IEntityCheckpointValidation, "one native provider owns lifecycle and checkpoint validation");
            ulong hash = game.Hash();
            foreach (TileRect bad in new[] { new TileRect(-1, 0, 1, 1), new TileRect(0, 0, 0, 1), new TileRect(int.MaxValue, 1, int.MaxValue, 1) })
            {
                c.Equal(PathSearchResult.InvalidRequest, mover.CanReach(game.World, bad), "invalid native reach request has a typed refusal");
                c.Throws<ArgumentException>(() => mover.GoTo(bad), "invalid direct engine goal refuses without an inferred route");
                c.Equal(hash, game.Hash(), "refused direct goal retains native route and presence");
            }
            Start(c, game, new TileCoord(19, 15)); Tick(game);
            SimGame foreign = New(factory); hash = game.Hash();
            c.Equal(PathSearchResult.InvalidRequest, mover.CanReach(foreign.World, mover.Goal), "foreign session cannot supply navigation for actual native motion");
            c.Throws<ArgumentException>(() => mover.Advance(foreign.World, new TickInfo(foreign.Tick, 1, 0, foreign.World.Time), Fix.One), "foreign execution session refuses before route mutation");
            c.Equal(hash, game.Hash(), "foreign navigation/advance retains actual native route");
            c.That(mover.Path.Count > 1 && mover.PathIndex >= 0, "actual native path exists for staged codec proof");
            c.Throws<NotSupportedException>(() => ((IList<TileCoord>)mover.Path).Add(new TileCoord(1, 1)), "read-only native route facts cannot mutate actual path");
            var invalid = new Action<Image>[] {
                image => image.Navigation = "missing.navigation",
                image => image.Status = -1, image => image.Status = 256, image => image.Status = 4,
                image => image.Age = -1, image => image.Index = -2, image => image.Index = image.Path.Count,
                image => image.Index = -1, image => image.Path.Clear(),
                image => image.Goal = new TileRect(0, 0, 0, 1),
                image => image.Goal = new TileRect(int.MaxValue, 1, int.MaxValue, 1),
                image => image.Goal = new TileRect(0, 0, 1, 1),
                image => image.Origin = FixVec2.Centre(new TileCoord(-1, 1)),
                image => image.Path[0] = new TileCoord(-1, 1),
                image => image.Path[1] = image.Path[0],
                image => image.Path[0] = new TileCoord(40, 35),
                image => image.Status = (int)MoveStatus.Idle,
                image => image.Status = (int)MoveStatus.Arrived,
                image => { image.Status = (int)MoveStatus.Idle; image.Path.Clear(); image.Index = 0; image.Origin = FixVec2.Zero; }
            };
            foreach (var change in invalid)
            {
                var image = new Image(mover); change(image); hash = game.Hash();
                c.Throws<CorruptStateException>(() => Read(mover, Bytes(image)), "malformed full route payload refuses before local publication");
                c.Equal(hash, game.Hash(), "malformed route decode retains all actual native fields");
            }
            byte[] complete = Bytes(new Image(mover));
            using (var actualPayload = new MemoryStream())
            {
                var writer = new StateWriter(actualPayload); mover.Sync(writer); writer.Flush();
                c.That(System.Linq.Enumerable.SequenceEqual(complete, actualPayload.ToArray()), "authored corruption image matches the complete actual native codec before any mutations");
            }
            for (int cut = 0; cut < complete.Length; cut++)
            {
                int length = cut; hash = game.Hash();
                c.Throws<CorruptStateException>(() => mover.Sync(new StateReader(complete, 0, length)), "every truncated route prefix refuses staged decode");
                c.Equal(hash, game.Hash(), "truncated route prefix retains actual native state");
            }
            Read(mover, complete); c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "valid staged native payload and origin roundtrip");
            var broken = factory.Load(game.Save()); Entity brokenCrane = Crane(broken); Mover brokenMover = brokenCrane.Get<Mover>();
            brokenCrane.Get<ITilePresence>().Move(broken.World, FixVec2.Centre(new TileCoord(23, 25)));
            c.Throws<CorruptStateException>(() => brokenMover.ValidateCheckpoint(), "actual position cannot be unrelated to current segment");
            c.Throws<CorruptStateException>(() => broken.Save(), "whole checkpoint invokes cross-capability validation");
            c.That(broken.Fault != null, "invalid whole checkpoint leaves its own session terminal");
            c.Equal(0, broken.Update(), "corrupt native geometry cannot be repaired by a later Update");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "actual provider geometry is checked after complete saved decode");
            var badIndex = factory.Load(game.Save());
            typeof(Mover).GetProperty(nameof(Mover.PathIndex)).SetValue(Crane(badIndex).Get<Mover>(), -2);
            c.Throws<CorruptStateException>(() => badIndex.Hash(), "corrupt live writer also refuses instead of serializing a bad phase");
            c.That(badIndex.Fault != null, "invalid phase hash latches its own session's fault");
            c.That(game.Seat(1).View.Feature<MotionView>().ReadArrival(crane.Id, out _) == Codes.NotFound, "native motion diagnostics remain company-private");
        }
        private static void Physical(Check c)
        {
            foreach (TileCoord destination in new[] { new TileCoord(15, 5), new TileCoord(19, 17) })
            {
                GameFactory factory = destination.Y == 5 ? Colony.Factory() : ReorderedFactory(); SimGame game = New(factory); Hold(c, game);
                Entity crane = Crane(game); Mover mover = crane.Get<Mover>();
                mover.GoTo(new TileRect(destination, 1, 1)); // Engine extension seam; pending state precedes the next engine Update.
                c.Equal(-1, mover.PathIndex, "accepted command awaits actual route planning");
                c.Equal(crane.Get<ITilePresence>().Position, mover.RouteOrigin, "pending origin is actual physical position");
                SimGame pending = factory.Load(game.Save()); c.Equal(game.Hash(), pending.Hash(), "pending route saves without inventing a path");
                Tick(game); Tick(pending); c.Equal(game.Hash(), pending.Hash(), "pending saved route plans through the same engine owner");
                Start(c, game, destination);
                c.That(mover.TicksMoving > 0 && mover.Path.Count > 0, "accepted route moves physically over game time");
                c.That(crane.Get<ITilePresence>().Position != FixVec2.Centre(crane.Get<ITilePresence>().Tile), "checkpoint captures actual fractional segment position");
                SimGame twin = factory.Load(game.Save());
                c.Equal(mover.RouteOrigin, Crane(twin).Get<Mover>().RouteOrigin, "actual route origin survives independent component decode");
                Reach(game, () => mover.Status == MoveStatus.Arrived, "saved straight/diagonal physical arrival", twin);
                c.Equal(game.Hash(), twin.Hash(), "saved fractional route continuation is deterministic");
                c.Equal(destination, crane.Get<ITilePresence>().Tile, "arrived status has real physical destination");
                c.Equal(0, mover.Path.Count, "arrival clears actual route");
                c.Equal(FixVec2.Zero, mover.RouteOrigin, "arrival retains no active origin");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "actual arrival checkpoint remains durable");

                Start(c, game, new TileCoord(8, 5)); Tick(game);
                c.That(mover.Path.Count > 0, "second route planned before interruption");
                TileCoord blocked = mover.Path[mover.PathIndex];
                var water = ((TileMap)game.World.Space).Layer<ByteLayer>("water"); water.Set(blocked, 1);
                twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "changed road/access is a legal saved route, not corrupt state");
                Tick(game); Tick(twin);
                c.Equal(-1, mover.PathIndex, "blocked live segment reopens existing path search");
                c.Equal(crane.Get<ITilePresence>().Position, mover.RouteOrigin, "replanning origin is exact current physical position");
                c.Equal(game.Hash(), twin.Hash(), "interrupted saved routes retain the same physical pending state");
                water.Set(blocked, 0); ((TileMap)twin.World.Space).Layer<ByteLayer>("water").Set(blocked, 0);
                Reach(game, () => mover.Status == MoveStatus.Arrived, "reopened physical route arrives", twin);
                c.Equal(game.Hash(), twin.Hash(), "route repair uses original provider and deterministic saved state");

                Start(c, game, destination); Tick(game); var stop = crane.Get<ITilePresence>().Position;
                mover.Stop(); game.World.Settle();
                c.Equal(stop, crane.Get<ITilePresence>().Position, "Stop clears only motion intent, never teleports presence");
                c.Equal(default(TileRect), mover.Goal, "stopped owner retains no stale movement goal");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "stopped state saves without an active path");
            }
            GameFactory wideFactory = Colony.Factory(); SimGame wide = New(wideFactory); Hold(c, wide); Start(c, wide, new TileCoord(15, 5));
            Mover age = Crane(wide).Get<Mover>(); typeof(Mover).GetProperty(nameof(Mover.TicksMoving)).SetValue(age, (long)int.MaxValue);
            SimGame wideTwin = wideFactory.Load(wide.Save()); Tick(wide); Tick(wideTwin);
            c.Equal((long)int.MaxValue + 1, age.TicksMoving, "native elapsed movement age remains checked 64-bit");
            c.Equal(wide.Hash(), wideTwin.Hash(), "wide elapsed route codec resumes identically");
            ulong hash = wide.Hash();
            c.Throws<OverflowException>(() => age.Advance(wide.World, new TickInfo(wide.Tick, 1, 0, wide.World.Time), Fix.FromInt(int.MaxValue)), "unrepresentable tick budget refuses before native age/route mutation");
            c.Equal(hash, wide.Hash(), "budget overflow preserves actual motion state");
            typeof(Mover).GetProperty(nameof(Mover.TicksMoving)).SetValue(age, long.MaxValue);
            hash = wide.Hash();
            c.Throws<OverflowException>(() => age.Advance(wide.World, new TickInfo(wide.Tick, 1, 0, wide.World.Time), Fix.One), "exhausted age refuses before native motion earns travel");
            c.Equal(hash, wide.Hash(), "age overflow preserves actual route/presence/age");
        }
    }
}

