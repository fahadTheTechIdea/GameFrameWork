using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Beep.SimEngine;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Tests
{
    public static class CommandInboxTests
    {
        public static void Register()
        {
            CommandStreamTests.Register();
            TestRunner.Add("Framework: concurrent command input preserves every order and its checkpoint", ConcurrentInput);
            TestRunner.Add("Framework: input arriving during a tick waits for the next command phase", PhaseBoundary);
            TestRunner.Add("Framework: submitted commands own their captured payload", PayloadOwnership);
            TestRunner.Add("Net: concurrent local input is sealed and answered once on both peers", LockstepInput);
        }

        private sealed class UnregisteredInput : Command { }

        private static List<CommandResult> Answers(SimGame game, int seat)
        {
            long cursor = 0;
            var results = new List<CommandResult>();
            game.Seat(seat).ReadResults(ref cursor, results);
            return results;
        }

        private static void ConcurrentInput(Check c)
        {
            SimGame game = TestGame.New(71);
            const int producers = 8, perProducer = 64;
            var tasks = new Task[producers];
            var sequences = new int[producers][];
            for (int p = 0; p < producers; p++)
            {
                int producer = p;
                sequences[p] = new int[perProducer];
                tasks[p] = Task.Run(() =>
                {
                    for (int i = 0; i < perProducer; i++)
                        sequences[producer][i] = game.Seat(producer % 2).Submit(
                            new PlaceBin { At = new TileCoord(-1, producer * perProducer + i) });
                });
            }
            c.That(Task.WaitAll(tasks, 10000), "every producer finished within the failure timeout");
            bool unique = true;
            for (int seat = 0; seat < 2; seat++)
            {
                var seen = new HashSet<int>();
                for (int p = seat; p < producers; p += 2)
                    for (int i = 0; i < perProducer; i++) unique &= seen.Add(sequences[p][i]);
                unique &= seen.Count == producers / 2 * perProducer
                    && seen.Contains(1) && seen.Contains(producers / 2 * perProducer);
            }
            c.That(unique, "each seat has contiguous, unique sequences across competing producers");
            byte[] saved = game.Save();
            SimGame restored = TestGame.Factory().Load(saved);
            c.Equal(game.Hash(), restored.Hash(), "pending input and sequence counters round-trip together");
            game.RunTicks(1); restored.RunTicks(1);
            for (int seat = 0; seat < 2; seat++)
            {
                List<CommandResult> results = Answers(game, seat);
                bool ordered = results.Count == producers / 2 * perProducer;
                for (int i = 0; i < results.Count; i++)
                    ordered &= results[i].Sequence == i + 1 && results[i].Tick == 0
                        && !results[i].Accepted && results[i].Refusal == Codes.OutOfBounds;
                c.That(ordered, "seat " + seat + " receives every refusal once, in sequence order");
            }
            c.Equal(game.Hash(), restored.Hash(), "the saved pending batch continues identically");
            c.Equal(producers / 2 * perProducer + 1,
                restored.Seat(0).Submit(new PlaceBin { At = new TileCoord(20, 10) }),
                "the resumed inbox allocates the next unused sequence");
        }

        private static void PhaseBoundary(Check c)
        {
            using (var insideTick = new ManualResetEventSlim())
            using (var continueTick = new ManualResetEventSlim())
            {
                bool first = true;
                var boundary = new ProbeModule("boundary", new ModuleVersion(1, 0, 0),
                    requires: new[] { new ModuleRequirement("test", new ModuleVersion(1, 0, 0)) },
                    compose: composer => composer.Events.Subscribe<BinPlaced>("boundary.input", _ =>
                    {
                        if (!first) return;
                        first = false;
                        insideTick.Set();
                        if (!continueTick.Wait(10000)) throw new InvalidOperationException("input boundary test timed out");
                    }));
                var definition = new GameDefinition("testgame", new GameModule[] { new TestModule(), new ExtraModule(), boundary });
                string manifest = TestGame.Manifest.Replace(" ] }", ", { \"id\": \"boundary\", \"version\": \"1.0.0\", \"requires\": [{ \"id\": \"test\", \"version\": \"1.0.0\" }], \"documents\": [] } ] }");
                var files = new PreloadedFileSource().Add("manifest.json", manifest)
                    .Add("test/defs.json", TestGame.Defs).Add("extra/defs.json", TestGame.ExtraDefs);
                var factory = new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
                SimGame game = factory.NewGame(new NewGameRequest { Scenario = "flat", Seed = 72, Mods = { "boundary" } });
                game.Seat(0).Submit(new PlaceBin { At = new TileCoord(20, 10) });
                Task<int> tick = Task.Run(() => game.RunTicks(1));
                bool reached = insideTick.Wait(10000);
                int sequence = 0;
                try
                {
                    c.That(reached, "the engine reached its fixed event phase");
                    if (reached) sequence = game.Seat(0).Submit(new PlaceBin { At = new TileCoord(24, 10) });
                }
                finally { continueTick.Set(); }
                c.That(tick.Wait(10000) && tick.Result == 1, "input does not block or fault the running tick");
                c.Equal(1, Answers(game, 0).Count, "input arriving after batch capture is not applied mid-tick");
                SimGame restored = factory.Load(game.Save());
                game.RunTicks(1); restored.RunTicks(1);
                List<CommandResult> results = Answers(game, 0);
                c.That(results.Count == 2 && results[1].Sequence == sequence && results[1].Tick == 1
                    && results[1].Accepted, "the next command phase answers the late order exactly once");
                c.Equal(game.Hash(), restored.Hash(), "a checkpoint retains input received during the previous tick");
            }
        }

        private static void PayloadOwnership(Check c)
        {
            SimGame game = TestGame.New(73);
            var command = new PlaceBin { At = new TileCoord(20, 10) };
            int first = game.Seat(0).Submit(command);
            command.At = new TileCoord(24, 10);
            int second = game.Seat(0).Submit(command);
            command.At = new TileCoord(-1, -1);
            SimGame restored = TestGame.Factory().Load(game.Save());
            game.RunTicks(1); restored.RunTicks(1);
            List<CommandResult> results = Answers(game, 0);
            c.That(first == 1 && second == 2 && results.Count == 2
                && results[0].Sequence == first && results[1].Sequence == second
                && results[0].Accepted && results[1].Accepted,
                "reusing a command object cannot overwrite either queued order or sequence");
            game.Observer.TryGetEntity(results[0].Entity, out EntityView a);
            game.Observer.TryGetEntity(results[1].Entity, out EntityView b);
            c.That(((global::Beep.SimEngine.Maps.TileSpatialSnapshot)a.Spatial).Tile == new TileCoord(20, 10)
                && ((global::Beep.SimEngine.Maps.TileSpatialSnapshot)b.Spatial).Tile == new TileCoord(24, 10),
                "each order uses the payload captured at its own submission");
            c.Equal(game.Hash(), restored.Hash(), "captured payloads survive a pending-input save");
            ulong before = game.Hash();
            c.Throws<ArgumentException>(() => game.Seat(0).Submit(new UnregisteredInput()),
                "unregistered input is rejected before entering persistent state");
            c.Equal(before, game.Hash(), "rejected input neither advances sequences nor changes state");

            SimGame colony = Colony.New(73);
            var line = new LayLineCommand { Line = "road", Tiles = new List<TileCoord> { new TileCoord(10, 10) } };
            var copy = (LayLineCommand)colony.World.Commands.Copy(line);
            line.Tiles[0] = new TileCoord(11, 11);
            line.Tiles.Add(new TileCoord(12, 12));
            c.That(copy.Tiles.Count == 1 && copy.Tiles[0] == new TileCoord(10, 10),
                "command Sync also captures nested mutable collections without shared references");
        }

        private static void Exchange(SimGame from, SimGame to)
        {
            var messages = new List<byte[]>();
            from.Net.DrainOutgoing(messages);
            for (int i = 0; i < messages.Count; i++) to.Net.Receive(messages[i]);
        }

        private static void LockstepInput(Check c)
        {
            var request = new NewGameRequest { Scenario = "flat", Seed = 74,
                Seats = { Controller.Human, Controller.Human } };
            SimGame a = TestGame.Factory().NewGame(request), b = TestGame.Factory().NewGame(request);
            a.StartLockstep(new LockstepOptions { LocalSeats = { 0 } });
            b.StartLockstep(new LockstepOptions { LocalSeats = { 1 } });
            const int total = 768;
            var submitted = new int[total];
            Task producer = Task.Run(() =>
            {
                var reusable = new PlaceBin();
                for (int i = 0; i < total; i++)
                {
                    reusable.At = new TileCoord(-1, i);
                    submitted[i] = a.Seat(0).Submit(reusable);
                }
            });
            int rounds = 0;
            do
            {
                a.RunTicks(1); b.RunTicks(1);
                Exchange(a, b); Exchange(b, a);
                rounds++;
            } while ((!producer.IsCompleted || Answers(a, 0).Count < total) && rounds < 10000);
            c.That(producer.Wait(10000), "the local input producer finishes while frames are being sealed");
            for (int i = 0; i < 32; i++) { a.RunTicks(1); b.RunTicks(1); Exchange(a, b); Exchange(b, a); }
            List<CommandResult> left = Answers(a, 0), right = Answers(b, 0);
            bool same = left.Count == total && right.Count == total;
            for (int i = 0; i < total && i < left.Count && i < right.Count; i++)
                same &= submitted[i] == i + 1 && left[i].Sequence == right[i].Sequence
                    && left[i].Sequence == i + 1 && left[i].Tick == right[i].Tick
                    && left[i].Refusal == right[i].Refusal && left[i].Refusal == Codes.OutOfBounds;
            c.That(same, "both peers answer every captured local order once with matching tick and sequence");
            c.That(a.State != GameState.Faulted && b.State != GameState.Faulted
                && a.State != GameState.Desynced && b.State != GameState.Desynced,
                "concurrent input causes neither a fault nor a desync");
            for (int i = 0; i < 64 && a.Tick != b.Tick; i++)
            {
                if (a.Tick < b.Tick) a.RunTicks(1);
                else b.RunTicks(1);
                Exchange(a, b); Exchange(b, a);
            }
            c.Equal(a.Tick, b.Tick, "both peers settle at the same tick within the bounded wait");
            c.Equal(a.Hash(), b.Hash(), "both peers reach the same authoritative state");
        }
    }
}
