using System;
using System.Collections.Generic;
using System.IO;
using Beep.SimEngine.App;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class WideTimeTests
    {
        public static void Register()
        {
            TestRunner.Add("Wide time: commands, schedules, snapshots and queued save continuation", Commands);
            TestRunner.Add("Wide time: physical movement ages and construction preserve saved twins", Physical);
            TestRunner.Add("Wide time: lockstep hello, shuffled frames, checksums and refusal boundaries", Peers);
            TestRunner.Add("Wide time: calendar, script waits and checked deadlines", Deadlines);
        }

        // Boundary instrumentation is confined to the framework test assembly, never a host or extension.
        internal static void SetTick(SimGame game, long tick)
        {
            typeof(GameClock).GetProperty(nameof(GameClock.StepIndex)).SetValue(game.Clock, tick);
            game.Update(0); // Publish the ordinary engine projection, without executing gameplay.
        }

        private static void Commands(Check c)
        {
            foreach (long start in new[] { (long)int.MaxValue - 2, 1L << 45 })
            {
                GameFactory factory = TestGame.Factory();
                SimGame game = factory.NewGame(new NewGameRequest { Scenario = "flat", Seed = 67 });
                SetTick(game, start);
                int sequence = game.Seat(0).Submit(new PlaceBin { At = new TileCoord(20, 10) });
                byte[] queued = game.Save();
                c.Equal(start, SaveGame.Describe(queued).Tick, "save header retains full-width tick");
                c.Equal(start, factory.Describe(queued).Tick, "load-menu metadata retains full-width tick");
                SimGame restored = factory.Load(queued);
                c.Equal(start, restored.Tick, "authoritative tick restores exactly");
                c.Equal(game.Hash(), restored.Hash(), "queued command timestamp uses the same save/hash codec");
                c.Equal(1, game.Update(), "normal Update advances one tick at a wide age");
                restored.Update();
                var results = new List<CommandResult>(); long cursor = 0;
                game.Seat(0).ReadResults(ref cursor, results);
                c.That(results.Count == 1 && results[0].Sequence == sequence && results[0].Accepted, "saved queued command executes once");
                c.Equal(start, results[0].Tick, "answer timestamp is the executed tick, not truncated age");
                c.That(game.Seat(0).View.TryGetEntity(results[0].Entity, out EntityView entity), "created entity is visible through the normal view");
                c.Equal(start, entity.StateSince, "new entity birth/state timestamp is wide");
                var notices = new List<Notification>(); cursor = 0;
                game.Seat(0).ReadNotifications(ref cursor, notices);
                c.That(notices.Count == 1 && notices[0].Tick == start, "notification timestamp remains exact");
                c.Equal(start, game.Time.StartTick, "Update records wide start time");
                c.Equal(start + 1, game.Time.Tick, "Update records wide completion time");
                c.Equal(1, game.Time.TicksRun, "work count remains a bounded Update count");
                c.Equal(game.Tick, game.Snapshot.Tick, "published snapshot shares authoritative time");
                c.Equal(game.Tick, game.Seat(0).Snapshot.Tick, "private snapshot shares authoritative time");
                c.Equal(game.Tick, game.Seat(0).View.Tick, "live view shares authoritative time");
                c.That(Schedule.Every(7, 3).IsDue(start) == (start % 7 == 3), "periodic schedule uses wide modulo");
                Counter counter = game.World.Entities.Get(results[0].Entity).Get<Counter>();
                Fix before = counter.Value;
                game.RunTicks(20); restored.RunTicks(20);
                c.Equal(before + Fix.FromInt(10), counter.Value, "spread bucket covers twenty ticks exactly across old limit");
                c.Equal(game.Hash(), restored.Hash(), "interleaved saved twins continue identically");
                byte[] save = game.Save(); SimGame again = factory.Load(save);
                c.Equal(game.Hash(), again.Hash(), "wide new entity state survives another save");
                ulong hash = again.Hash(); SetTick(again, again.Tick + (1L << 32));
                c.That(hash != again.Hash(), "upper timestamp bits participate in the authoritative hash");
                SaveHeader header = SaveGame.ReadHeader(save, out StateReader body); header.Tick++;
                World empty = TestGame.Definition().Build(TestGame.Content(), header.Settings, false, out _);
                c.Throws<CorruptStateException>(() => SaveGame.ReadBody(empty, header, body), "header/world timestamp disagreement refuses explicitly");
            }
        }

        private static void Physical(Check c)
        {
            const long start = 1L << 40;
            GameFactory factory = Colony.Factory();
            SimGame game = factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 71 });
            SetTick(game, start);
            Entity crane = game.World.Entities.Get(Colony.Mine(game, 0, "crane")[0].Id);
            CommandResult move = TestKit.Order(game, 0, new MoveVehicleCommand { Vehicle = crane.Id, To = new TileCoord(15, 5) });
            c.That(move.Accepted && crane.Get<Mover>().IsMoving, "wide age does not make travel instantaneous");
            c.Equal(start, crane.StateSince, "moving state settles at the actual wide tick");
            Mover mover = crane.Get<Mover>();
            typeof(Mover).GetProperty(nameof(Mover.TicksMoving)).SetValue(mover, (long)int.MaxValue); // Test-only codec instrumentation; runtime setters are private.
            SimGame restored = factory.Load(game.Save());
            c.Equal((long)int.MaxValue, restored.World.Entities.Get(crane.Id).Get<Mover>().TicksMoving, "movement elapsed age uses its sole wide codec");
            game.Update(); restored.Update();
            c.Equal((long)int.MaxValue + 1, mover.TicksMoving, "moving age crosses the old limit without wrapping");
            int travelled = TestKit.RunUntil(game, () => !mover.IsMoving, 200, 1);
            restored.RunTicks(travelled);
            c.That(mover.Status == MoveStatus.Arrived && travelled > 0, "normal physical movement completes over time");
            c.That(crane.StateSince > start, "arrival commits a later wide state timestamp");
            c.Equal(game.Hash(), restored.Hash(), "in-motion saved twin arrives identically");
            c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = crane.Id, Held = false }).Accepted, "player releases the deliberately held manual-move machine for automatic work");
            CommandResult build = TestKit.Order(game, 0, new ConstructCommand { Def = "tower", Placement = Beep.SimEngine.Maps.TilePlacement.Arguments(game.World.Space.Id, new TileCoord(10, 16)), Priority = 50 });
            c.That(build.Accepted, "construction starts through a normal engine command");
            var orders = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
            c.That(orders.Count == 1 && orders[0].CreatedTick > start, "work projection retains the authoritative wide creation timestamp");
            WorkBoard board = game.World.Services.Get<WorkBoard>();
            c.That(board.Jobs.Count > 0 && board.Jobs[0].CreatedTick > start, "physical jobs retain full-width creation timestamps");
            Job job = board.Jobs[0]; job.RetryAt = game.Tick + 5;
            restored = factory.Load(game.Save());
            c.Equal(job.RetryAt, restored.World.Services.Get<WorkBoard>().Job(job.Id).RetryAt, "wide retry deadline restores without an alternate timer");
            c.Equal(game.Hash(), restored.Hash(), "mid-build jobs, retry and reservations restore together");
            int worked = TestKit.RunUntil(game, () => Colony.Mine(game, 0, "tower").Count == 1, 1500, 1);
            restored.RunTicks(worked);
            c.That(Colony.Mine(game, 0, "tower").Count == 1 && worked >= checked((int)TestGame.Time.Hours(5)), "hauling and machine construction still consume physical time: ran=" + worked + ", tick=" + game.Tick + ", fault=" + game.Fault + ", orders=" + board.Orders.Count + ", blocker=" + (board.Orders.Count > 0 ? board.Orders[0].Blocker.ToString() : "none"));
            c.Equal(game.Hash(), restored.Hash(), "wide-age construction saved twin completes identically");
            var news = new List<Notification>(); long cursor = 0; game.Seat(0).ReadNotifications(ref cursor, news);
            c.That(news.Exists(n => n.Kind == ConstructionService.NoteBuilt && n.Tick > start), "completion reports its actual wide timestamp");
        }

        private static void Exchange(SimGame from, SimGame to)
        {
            var messages = new List<byte[]>(); from.Net.DrainOutgoing(messages);
            for (int i = messages.Count - 1; i >= 0; i--) to.Net.Receive(messages[i]);
        }

        private static void Peers(Check c)
        {
            foreach (long start in new[] { (long)int.MaxValue - 2, 1L << 45 })
            {
                GameFactory factory = TestGame.Factory();
                SimGame a = factory.NewGame(new NewGameRequest { Scenario = "flat", Seed = 73, Seats = { Controller.Human, Controller.Human } });
                SetTick(a, start); SimGame b = factory.Load(a.Save());
                a.StartLockstep(new LockstepOptions { InputDelay = 2, ChecksumEvery = 1, LocalSeats = { 0 } });
                b.StartLockstep(new LockstepOptions { InputDelay = 2, ChecksumEvery = 1, LocalSeats = { 1 } });
                c.Equal(Code.None, a.Net.CheckHello(b.Net.Hello()), "hello carries full-width tick and matching state");
                a.Seat(0).Submit(new PlaceBin { At = new TileCoord(20, 10) });
                c.Equal(0, a.RunTicks(1), "wide-age peer stalls instead of running without remote input");
                c.Equal(start, a.Net.StalledOn, "stall diagnostic carries exact wide tick");
                c.Equal(0, b.RunTicks(1), "both peers await the same missing inputs");
                Exchange(a, b); Exchange(b, a);
                for (int i = 0; i < 20; i++) { a.RunTicks(1); b.RunTicks(1); Exchange(a, b); Exchange(b, a); }
                c.Equal(start + 20, a.Tick, "shuffled wide frames advance exactly twenty ticks");
                c.Equal(a.Tick, b.Tick, "peers reach identical wide time");
                c.Equal(a.Hash(), b.Hash(), "wide checksum keys preserve twin identity");
                c.That(a.Net.State != NetState.Broken && a.Net.State != NetState.Desynced, "valid wide frame and checksum messages decode normally");
                var results = new List<CommandResult>(); long cursor = 0; a.Seat(0).ReadResults(ref cursor, results);
                c.That(results.Count == 1 && results[0].Accepted && results[0].Tick == start + 2, "input delay stamps command at its physical application tick");
                // Capture a not-yet-compared checkpoint, then corrupt only the checksum payload.
                a.RunTicks(1); b.RunTicks(1);
                var packets = new List<byte[]>(); b.Net.DrainOutgoing(packets);
                byte[] bad = packets.Find(p => p[0] == 2); bad[bad.Length - 1] ^= 1; a.Net.Receive(bad);
                c.Equal(NetState.Desynced, a.Net.State, "a changed wide checksum causes desync");
                c.Equal(a.Tick, a.Net.DesyncTick, "desync diagnostic retains exact wide tick");
            }
            SimGame edge = TestGame.Factory().NewGame(new NewGameRequest { Scenario = "flat", Seats = { Controller.Human, Controller.Human } });
            SetTick(edge, long.MaxValue - 4);
            edge.StartLockstep(new LockstepOptions { InputDelay = 4, LocalSeats = { 0 } });
            using (var stream = new MemoryStream())
            {
                stream.WriteByte(1); var writer = new StateWriter(stream);
                long tick = long.MaxValue; writer.Long(ref tick); int seat = 1; writer.Int(ref seat);
                byte control = 0; writer.Byte(ref control); int speed = 0; writer.Int(ref speed); writer.Count(0); writer.Flush();
                edge.Net.Receive(stream.ToArray());
            }
            c.That(edge.Net.State != NetState.Broken, "ahead window uses safe subtraction near long maximum");
            ulong before = edge.Hash();
            c.Throws<OverflowException>(() => edge.Net.Seal(long.MaxValue - 1), "unrepresentable sealing target refuses before frames are committed");
            c.Equal(before, edge.Hash(), "refused protocol arithmetic does not mutate simulation state");
            foreach (bool trailing in new[] { false, true })
            {
                SimGame invalid = TestGame.New(); invalid.StartLockstep(new LockstepOptions { LocalSeats = { 0 } });
                using (var stream = new MemoryStream())
                {
                    stream.WriteByte(2); var writer = new StateWriter(stream);
                    long tick = trailing ? 0 : -1; writer.Long(ref tick); ulong hash = 0; writer.ULong(ref hash); writer.Flush();
                    if (trailing) stream.WriteByte(7);
                    ulong prior = invalid.Hash(); invalid.Net.Receive(stream.ToArray());
                    c.Equal(NetState.Broken, invalid.Net.State, "negative or trailing wide checksum refuses before comparison");
                    c.Equal(prior, invalid.Hash(), "invalid checksum changes no authoritative engine state");
                }
            }
        }

        private static void Deadlines(Check c)
        {
            var date = TestGame.Time.Calendar(long.MaxValue);
            c.Equal(long.MaxValue / checked((int)TestGame.Time.Days(1)), date["day"].Total, "calendar elapsed day count is wide");
            c.Equal(long.MaxValue / checked((int)TestGame.Time.Hours(1)), date["hour"].Total, "calendar elapsed hour count is wide");
            c.That(date["year"].Value > int.MaxValue && date["day"].Value >= 1 && date["day"].Value <= 365, "wide year and bounded calendar fields remain distinct");
            c.That(date["hour"].Value >= 0 && date["hour"].Value < 24 && date["minute"].Value >= 0 && date["minute"].Value < 60, "calendar moduli stay bounded");
            c.Throws<ArgumentOutOfRangeException>(() => TestGame.Time.Calendar(-1), "negative authoritative calendar timestamps refuse");
            c.Throws<ArgumentOutOfRangeException>(() => Wait.Ticks(-1), "negative scripted wait refuses");
            c.Throws<ArgumentOutOfRangeException>(() => Wait.Until(_ => false, -1), "negative scripted timeout refuses");
            SimGame game = TestGame.New(79); SetTick(game, 1L << 45);
            var decision = new PlayerDecisionContext(game.World, new PlayerId(0), game.Seat(0).View,
                game.Seat(0).Submit, () => Array.Empty<CommandResult>(), null);
            Wait wait = Wait.Ticks(2), timeout = Wait.Until(_ => false, 3);
            c.That(!wait.Done(decision) && !timeout.Done(decision), "waits begin at actual wide session time");
            game.RunTicks(2); c.That(wait.Done(decision) && !timeout.Done(decision), "relative wait expires without narrowing its deadline");
            game.Update(); c.That(timeout.Done(decision), "conditional wait deadline expires at its exact tick");
            SetTick(game, long.MaxValue - 1);
            c.Throws<OverflowException>(() => Wait.Ticks(2).Done(decision), "wait deadline overflow refuses instead of instant completion");
            c.Throws<OverflowException>(() => Wait.Until(_ => false, 2).Done(decision), "timeout deadline overflow refuses instead of a negative sentinel");
            c.Equal(1, game.Update(), "last representable tick completes");
            c.Equal(long.MaxValue, game.Tick, "last completed tick remains nonnegative and exact");
            c.Equal(0, game.Update(), "next tick faults before work instead of wrapping");
            c.That(game.Fault != null && game.Fault.Tick == long.MaxValue && game.Fault.Phase == "begin", "fault diagnostics share wide authoritative timestamp");
        }
    }
}
