using System;
using System.Collections.Generic;
using System.IO;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Tests
{
    public static class ProtocolBoundaryTests
    {
        public static void Register()
        {
            TestRunner.Add("Protocol boundary: schedule and random arguments fail before arithmetic", Arguments);
            TestRunner.Add("Protocol boundary: options are captured and invalid sessions leave no state", Options);
            TestRunner.Add("Protocol boundary: tick and seat keys remain distinct above seat 1023", CompoundKeys);
            TestRunner.Add("Protocol boundary: invalid frames never commit partial input", Frames);
        }

        private static void Arguments(Check c)
        {
            foreach (int period in new[] { 0, -1, int.MinValue })
            {
                c.Throws<ArgumentOutOfRangeException>(() => Schedule.Every(period), "period rejected before modulo: " + period);
                c.Throws<ArgumentOutOfRangeException>(() => Schedule.Spread(period), "spread period rejected: " + period);
            }
            c.Throws<ArgumentOutOfRangeException>(() => Schedule.Every(10, -1), "negative phase cannot produce a never-due schedule");
            c.Equal(3L, Schedule.Every(10, 23).Phase, "nonnegative phases retain canonical normalization");
            c.That(Schedule.Every(int.MaxValue, int.MaxValue).IsDue(0), "large valid schedule does not overflow");
            var rng = new DetRandom(71); ulong state = rng.State;
            c.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(0), "zero random bound is not a fabricated zero");
            c.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(-1), "negative random bound rejected");
            c.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(8, 8), "empty integer range rejected");
            c.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(9, 8), "reversed integer range rejected");
            c.Throws<ArgumentOutOfRangeException>(() => rng.NextFix(Fix.One, Fix.One), "empty fixed-point range rejected");
            c.Throws<ArgumentOutOfRangeException>(() => rng.NextFix(Fix.One, Fix.Zero), "reversed fixed-point range rejected");
            c.Throws<OverflowException>(() => rng.NextFix(Fix.MinValue, Fix.MaxValue), "unrepresentable fixed-point width rejected before random consumption");
            c.Throws<ArgumentOutOfRangeException>(() => rng.Chance(Fix.FromInt(-1)), "negative probability rejected");
            c.Throws<ArgumentOutOfRangeException>(() => rng.Chance(Fix.FromInt(2)), "probability above one rejected");
            c.Equal(state, rng.State, "all argument refusals leave session random state untouched");
            var twin = new DetRandom(71); bool same = true, bounded = true;
            for (int i = 0; i < 100; i++)
            {
                int value = rng.NextInt(int.MinValue, int.MaxValue);
                same &= value == twin.NextInt(int.MinValue, int.MaxValue); bounded &= value >= int.MinValue && value < int.MaxValue;
            }
            c.That(same && bounded, "full signed integer width uses checked long arithmetic and remains deterministic");
            c.That(!rng.Chance(Fix.Zero) && rng.Chance(Fix.One), "probability endpoints remain valid");
        }

        private static void Options(Check c)
        {
            foreach (LockstepOptions bad in new[] {
                new LockstepOptions { InputDelay = -1 },
                new LockstepOptions { ChecksumEvery = -1 }, new LockstepOptions { LocalSeats = null },
                new LockstepOptions { LocalSeats = { 0, 0 } }, new LockstepOptions { LocalSeats = { -1 } },
                new LockstepOptions { LocalSeats = { 2 } } })
            {
                SimGame game = TestGame.New(); ulong before = game.Hash();
                c.Throws<ArgumentException>(() => game.StartLockstep(bad), "invalid options refuse at session creation");
                c.That(game.Net == null && !game.Clock.Sequenced && before == game.Hash(), "failed session leaves lifecycle and authoritative state untouched");
            }
            var request = new NewGameRequest { Scenario = "flat", Seed = 11, Seats = { Controller.Human, Controller.Human } };
            SimGame a = TestGame.Factory().NewGame(request), b = TestGame.Factory().NewGame(request);
            var options = new LockstepOptions { LocalSeats = { 0 }, InputDelay = 2, ChecksumEvery = 1 };
            a.StartLockstep(options); b.StartLockstep(new LockstepOptions { LocalSeats = { 1 }, InputDelay = 2, ChecksumEvery = 1 });
            byte[] hello = a.Net.Hello(); options.InputDelay = -10; options.ChecksumEvery = -1; options.LocalSeats.Clear();
            c.Equal(2, a.Net.InputDelay, "input delay is captured, not retained by reference");
            c.That(a.Net.IsLocal(new PlayerId(0)), "caller cannot revoke captured local seat ownership");
            c.Equal(Code.None, b.Net.CheckHello(a.Net.Hello()), "option mutation cannot change hello semantics");
            c.Equal(Code.None, b.Net.CheckHello(hello), "captured peers agree on protocol before input");
            SimGame different = TestGame.Factory().NewGame(request);
            different.StartLockstep(new LockstepOptions { InputDelay = 3, ChecksumEvery = 1 });
            c.Equal(new Code("net.protocol-differs"), different.Net.CheckHello(hello), "different delay refuses handshake, not later divergent ticks");
            SimGame interval = TestGame.Factory().NewGame(request);
            interval.StartLockstep(new LockstepOptions { InputDelay = 2, ChecksumEvery = 0 });
            c.Equal(new Code("net.protocol-differs"), interval.Net.CheckHello(hello), "checksum policy is explicit peer metadata");
            c.Throws<ArgumentOutOfRangeException>(() => a.Net.RequestSpeed(0), "invalid requested speed refuses before enqueue");
            c.Throws<ArgumentOutOfRangeException>(() => a.Net.RequestSpeed(65), "invalid fast speed refuses before enqueue");
            for (int i = 0; i < 20; i++) { a.RunTicks(1); b.RunTicks(1); Exchange(a, b); Exchange(b, a); }
            c.That(a.Tick > 0 && a.Net.State != NetState.Broken && b.Net.State != NetState.Broken, "captured session survives hostile mutation of supplied options");
            c.Equal(a.Hash(), b.Hash(), "captured interval/delay retain peer determinism");
        }

        private static void Exchange(SimGame from, SimGame to, bool reverse = false)
        {
            var messages = new List<byte[]>(); from.Net.DrainOutgoing(messages); if (reverse) messages.Reverse();
            foreach (byte[] message in messages) to.Net.Receive(message);
        }

        private static GameFactory WideFactory()
        {
            var definition = new GameDefinition("wide", new[] { new ProbeModule("wide", new ModuleVersion(1, 0, 0)) });
            var files = new PreloadedFileSource().Add("manifest.json", "{ \"schema\": 1, \"modules\": [{ \"id\": \"wide\", \"version\": \"1.0.0\", \"documents\": [\"wide.json\"] }] }")
                .Add("wide.json", "{ " + TestGame.TimeDefinitions + " \"entity_states\": [{ \"id\": \"idle\", \"state\": \"state.idle\", \"priority\": 0, \"default\": true }], \"game_modes\":[{\"id\":\"protocol-boundary.resource-only\",\"name\":\"Physical diagnostic game\",\"properties\":{}}],\"scenarios\": [{ \"id\": \"wide\",\"game_mode\":\"protocol-boundary.resource-only\",\"mode_overrides\":{}, \"time\": \"test.minute\", \"modules\": [\"wide\"], \"seats\": 1025 }] }");
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }

        private static void CompoundKeys(Check c)
        {
            var request = new NewGameRequest { Scenario = "wide" };
            for (int i = 0; i < 1025; i++) request.Seats.Add(i == 0 || i == 1024 ? Controller.Human : Controller.Autopilot);
            GameFactory factory = WideFactory(); SimGame a = factory.NewGame(request), b = factory.NewGame(request);
            a.StartLockstep(new LockstepOptions { LocalSeats = { 0 }, ChecksumEvery = 1 });
            b.StartLockstep(new LockstepOptions { LocalSeats = { 1024 }, ChecksumEvery = 1 });
            for (int i = 0; i < 12; i++) { a.RunTicks(1); b.RunTicks(1); Exchange(a, b, true); Exchange(b, a, true); }
            c.Equal(1025, a.SeatCount, "protocol does not introduce a platform seat cap");
            c.That(a.Tick > 0 && a.Tick == b.Tick, "shuffled high-seat frames progress identically");
            c.That(a.Net.State != NetState.Broken && b.Net.State != NetState.Broken, "tick 0/seat 1024 never aliases tick 1/seat 0");
            c.Equal(a.Hash(), b.Hash(), "compound identities preserve all session facts");
        }

        private static byte[] Frame(long tick, byte control, int speed, bool trailing = false, int count = 0,
            CommandRegistry.Entry entry = null, int[] sequences = null)
        {
            using (var stream = new MemoryStream())
            {
                stream.WriteByte(1); var writer = new StateWriter(stream);
                writer.Long(ref tick); int seat = 1; writer.Int(ref seat); writer.Byte(ref control); writer.Int(ref speed); writer.Count(sequences == null ? count : sequences.Length);
                if (sequences != null) foreach (int value in sequences) {
                    int tag = entry.Tag, sequence = value; writer.Int(ref tag); writer.Int(ref sequence); entry.Create().Sync(writer);
                }
                writer.Flush(); if (trailing) stream.WriteByte(7); return stream.ToArray();
            }
        }

        private static void Frames(Check c)
        {
            foreach (byte[] bad in new[] { Frame(0, 9, 0), Frame(0, 3, 0), Frame(0, 3, 65), Frame(0, 0, 1),
                Frame(0, 0, 0, true), Frame(-1, 0, 0), Frame(0, 0, 0, count: 257) })
            {
                SimGame game = TestGame.Factory().NewGame(new NewGameRequest { Scenario = "flat", Seats = { Controller.Human, Controller.Human } });
                game.StartLockstep(new LockstepOptions { LocalSeats = { 0 } }); ulong before = game.Hash();
                game.Net.Receive(bad); c.Equal(NetState.Broken, game.Net.State, "invalid frame is an explicit protocol failure");
                c.Equal(before, game.Hash(), "invalid frame commits no authoritative command state");
                c.Equal(0, game.RunTicks(1), "broken session cannot apply a partially decoded frame");
            }
            foreach (int[] sequences in new[] { new[] { 0 }, new[] { -1 }, new[] { 1, 1 }, new[] { 2, 1 } }) {
                var game = TestGame.Factory().NewGame(new NewGameRequest { Scenario = "flat", Seats = { Controller.Human, Controller.Human } });
                game.World.Commands.TryGet(typeof(Commands.SetControllerCommand), out var entry);
                game.StartLockstep(new LockstepOptions { LocalSeats = { 0 }, InputDelay = 0 }); ulong before = game.Hash();
                game.Net.Receive(Frame(0, 0, 0, entry: entry, sequences: sequences));
                c.That(game.Net.State == NetState.Broken && game.Hash() == before && game.Update() == 0,
                    "nonpositive/duplicate/regressing within-frame identity commits no input");
            }
            {
                var game = TestGame.Factory().NewGame(new NewGameRequest { Scenario = "flat", Seats = { Controller.Human, Controller.Human } });
                game.World.Commands.TryGet(typeof(Commands.SetControllerCommand), out var entry);
                game.StartLockstep(new LockstepOptions { LocalSeats = { 0 }, InputDelay = 0 });
                game.Net.Receive(Frame(1, 0, 0, entry: entry, sequences: new[] { 1 }));
                game.Net.Receive(Frame(0, 0, 0, entry: entry, sequences: new[] { 2 }));
                c.Equal(1, game.Update(), "reordered earlier epoch admits its positive fresh ticket");
                game.Seat(0).Submit(new Commands.SetControllerCommand { Controller = Controller.Human });
                ulong before = game.Hash();
                c.That(game.Update() == 0 && game.Net.State == NetState.Broken && game.Fault == null, "cross-frame producer regression is typed transport failure, not an escaping partial admission");
                c.That(game.Hash() == before && game.World.LastCommandSequence(new PlayerId(0), CommandStreams.Player) == 0,
                    "bad later seat never commits the earlier local seat's valid command");
            }
        }
    }
}
