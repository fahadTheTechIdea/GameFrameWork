using System;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Tests
{
    public static class TimeBoundaryTests
    {
        public static void Register()
        {
            TestRunner.Add("Time boundaries: exact fixed-point parsing and checked limits", Numbers);
            TestRunner.Add("Time boundaries: fixed-point operators agree with integer oracle", Arithmetic);
            TestRunner.Add("Time boundaries: earned elapsed backlog is retained and interpolation bounded", Backlog);
            TestRunner.Add("Time boundaries: pause, pace changes, stalls and terminal cutoffs", Lifecycle);
            TestRunner.Add("Time boundaries: duration and state-reader overflow fail explicitly", ContentAndReaders);
            TestRunner.Add("Time boundaries: tick, identity and coordinate exhaustion never wrap", Exhaustion);
        }

        private static void Numbers(Check c)
        {
            c.Equal(Fix.MinValue, Fix.Parse("-2147483648"), "minimum signed fixed point is representable");
            c.Equal(Fix.MaxValue, Fix.Parse("2147483647.99999999976716935634613037109375"), "all 32 fraction bits survive parsing");
            c.Equal(Fix.FromRaw(1), Fix.Parse("0.00000000023283064365386962890625"), "one raw bit survives exact decimal parsing");
            c.Equal(Fix.One, Fix.Parse("0.0000000001e10"), "exponent is applied before fractional quantization");
            c.Equal(Fix.FromInt(2), Fix.Parse("20000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000e-100"), "large mantissa is parsed with bounded intermediate precision");
            c.Equal(Fix.MaxValue, Fix.Parse("2147483647.999999999999999999999999999999999"), "sub-raw-bit excess truncates under Q32.32 precision, not arithmetic saturation");
            foreach (string invalid in new[] { "2147483648", "-2147483648.1", "1e4294967296", "1e2147483647", ".", "1e", "1e-", "1.2x", "+" })
                c.That(!Fix.TryParse(invalid, out _), "invalid or overflowing decimal does not wrap: " + invalid);
            c.That(Fix.TryParse("1e-2147483647", out Fix tiny) && tiny.IsZero, "sub-resolution value truncates to zero under the declared Q32.32 precision");
            c.Equal(Fix.One, Fix.FromRatio(long.MaxValue, long.MaxValue), "full-width rational inputs do not require fabricated 32-bit bounds");
            c.Equal(Fix.One, Fix.FromRatio(long.MinValue, long.MinValue), "negative full-width rational inputs cancel exactly");
            c.Throws<OverflowException>(() => Fix.FromRatio(long.MaxValue, 1), "unrepresentable rational fails");
            c.Throws<DivideByZeroException>(() => Fix.FromRatio(1, 0), "zero denominator fails");
            c.Throws<OverflowException>(() => { var ignored = Fix.MaxValue + Fix.FromRaw(1); }, "sum cannot silently become negative");
            c.Throws<OverflowException>(() => { var ignored = Fix.MinValue - Fix.FromRaw(1); }, "difference cannot silently become positive");
            c.Throws<OverflowException>(() => { var ignored = -Fix.MinValue; }, "unrepresentable negation fails");
            c.Throws<OverflowException>(() => Fix.Abs(Fix.MinValue), "unrepresentable absolute value fails");
            c.Equal(int.MinValue, Fix.MinValue.ToIntCeiling(), "ceiling minimum never wraps");
            c.Equal(int.MinValue, Fix.MinValue.ToIntRound(), "rounding minimum never wraps");
            c.Throws<OverflowException>(() => Fix.MaxValue.ToIntCeiling(), "ceiling beyond int range fails");
            c.Throws<OverflowException>(() => Fix.MaxValue.ToIntRound(), "rounding beyond int range fails");
            c.Equal(-1, Fix.Parse("-1.5").ToIntRound(), "integer rounding keeps ties toward positive infinity");
            c.Equal("-2147483648.000", Fix.MinValue.ToString(), "diagnostics handle the minimum without negation overflow");
        }

        private static void Expected(Check c, BigInteger expected, Func<Fix> operation, string label)
        {
            if (expected < long.MinValue || expected > long.MaxValue)
                c.Throws<OverflowException>(() => operation(), label + " overflow");
            else c.Equal((long)expected, operation().Raw, label + " raw result");
        }

        private static void Pair(Check c, long ar, long br)
        {
            Fix a = Fix.FromRaw(ar), b = Fix.FromRaw(br);
            BigInteger product = (BigInteger)ar * br;
            Expected(c, product >> 32, () => a * b, "multiply " + ar + "/" + br);
            if (br == 0) { c.Throws<DivideByZeroException>(() => { var ignored = a / b; }, "division by zero"); return; }
            BigInteger numerator = BigInteger.Abs((BigInteger)ar) << 32, denominator = BigInteger.Abs((BigInteger)br);
            BigInteger quotient = BigInteger.DivRem(numerator, denominator, out BigInteger remainder);
            if (remainder * 2 >= denominator) quotient++;
            if ((ar ^ br) < 0) quotient = -quotient;
            Expected(c, quotient, () => a / b, "divide " + ar + "/" + br);
        }

        private static void Arithmetic(Check c)
        {
            foreach (long a in new[] { long.MinValue, long.MinValue + 1, long.MaxValue, -4294967296L, 4294967296L, -1L, 0L, 1L })
                foreach (long b in new[] { long.MinValue, long.MaxValue, -4294967296L, 4294967296L, -1L, 0L, 1L }) Pair(c, a, b);
            var rng = new DetRandom(1241);
            for (int i = 0; i < 256; i++) Pair(c, unchecked((long)rng.NextULong()), unchecked((long)rng.NextULong()));
            for (int i = 0; i < 64; i++) Pair(c, unchecked((long)rng.NextULong()), rng.NextInt(1, 100) * (1L << 32));
        }

        private static void Drain(SimGame game)
        {
            int attempts = 0;
            while (game.Clock.PendingTicks > 0 && attempts++ < 1000) game.Update(0);
            if (game.Clock.PendingTicks != 0) throw new InvalidOperationException("clock proof did not drain its earned backlog");
        }

        private static void Backlog(Check c)
        {
            SimGame game = TestGame.New(51), twin = TestGame.New(51);
            game.Clock.TicksPerSecond = 10; game.Clock.CatchUpCap = 3;
            c.Equal(3, game.Update(2050000), "2.05 seconds is not silently clipped to one second or the budget");
            c.Equal(17L, game.Clock.PendingTicks, "whole backlog is retained");
            c.Equal(Fix.Half, game.Clock.Alpha, "interpolation excludes whole pending ticks");
            c.That(game.Clock.IsRunningSlowly, "exceeded work budget is reported");
            c.That(game.Snapshot.Alpha >= Fix.Zero && game.Snapshot.Alpha < Fix.One, "snapshot fraction remains within one tick");
            Drain(game); twin.RunTicks(20);
            c.Equal(twin.Hash(), game.Hash(), "zero-elapsed updates drain exactly the previously earned work");
            c.Equal(Fix.Half, game.Clock.Alpha, "fraction is retained through all backlog drains");
            c.Equal(1, game.Update(50000), "the second half earns one exact tick"); twin.RunTicks(1);
            c.Equal(twin.Hash(), game.Hash(), "no ticks duplicated or dropped");
            SimGame frame = TestGame.New(52), frameTwin = TestGame.New(52);
            frame.Clock.TicksPerFrame = 100000; frame.Clock.CatchUpCap = 2;
            c.Equal(2, frame.Update(), "engine-time Update uses the same explicit work budget");
            c.Equal(99998L, frame.Clock.PendingTicks, "engine-time updates do not discard earned ticks");
            frame.Clock.CatchUpCap = 100000; c.Equal(99998, frame.Update(0), "zero elapsed drains an engine-time backlog without adding a frame");
            frameTwin.RunTicks(100000); c.Equal(frameTwin.Hash(), frame.Hash(), "one kernel owns engine-time and elapsed-time work");
        }

        private static void Lifecycle(Check c)
        {
            SimGame game = TestGame.New(53); game.Clock.TicksPerSecond = 10; game.Clock.CatchUpCap = 2;
            game.Update(550000); ulong beforePause = game.Hash();
            game.Clock.Pause(); c.Equal(0, game.Update(long.MaxValue), "paused time cannot accrue or overflow");
            c.Equal(beforePause, game.Hash(), "paused update leaves simulation untouched");
            c.Equal(3L, game.Clock.PendingTicks, "pause retains already earned work");
            c.Equal(Fix.Zero, game.Clock.Alpha, "paused presentation reports no partial progress");
            game.Clock.Speed = 2; game.Clock.TicksPerSecond = 20; game.Clock.Resume();
            c.Equal(2, game.Update(0), "resume and pace changes do not discard previously earned work");
            c.Equal(Fix.Half, game.Clock.Alpha, "pace changes preserve earned fractional work");
            game.Update(0); c.Equal(5, game.Tick, "all pre-pause whole ticks finish");
            c.Equal(1, game.Update(12500), "new elapsed time uses the new rate and speed only");
            SimGame ended = TestGame.Factory().NewGame(new NewGameRequest { Scenario = "flat", Horizon = GameDuration.Days(1) });
            ended.Clock.TicksPerSecond = checked((int)TestGame.Time.Days(1)); ended.Clock.CatchUpCap = checked((int)TestGame.Time.Days(1));
            c.Equal(checked((int)TestGame.Time.Days(1)), ended.Update(2000000), "horizon stops an earned batch at the exact game tick");
            c.Equal(0L, ended.Clock.PendingTicks, "terminal cutoff cancels unapplied host time");
            c.Equal(0, ended.Update(long.MaxValue), "ended updates never accumulate more time or overflow");
            c.Equal(0L, ended.Clock.PendingTicks, "terminal update remains empty");
            SimGame stalled = TestGame.Factory().NewGame(new NewGameRequest { Scenario = "flat", Seats = { Controller.Human, Controller.Human } });
            stalled.StartLockstep(new LockstepOptions { LocalSeats = { 0 }, InputDelay = 0 });
            stalled.Clock.TicksPerSecond = 10; stalled.Clock.CatchUpCap = 3;
            c.Equal(0, stalled.Update(450000), "missing remote input cannot consume a due tick");
            c.Equal(4L, stalled.Clock.PendingTicks, "a stalled peer retains every earned tick");
            c.Equal(Fix.Half, stalled.Clock.Alpha, "stall cannot inflate interpolation beyond one");
            ulong before = game.Hash(); long pending = game.Clock.PendingTicks;
            c.Throws<ArgumentOutOfRangeException>(() => game.Update(-1), "negative elapsed input refuses explicitly");
            c.Throws<OverflowException>(() => game.Update(long.MaxValue), "unrepresentable accumulator input refuses before ticking");
            c.Equal(before, game.Hash(), "invalid elapsed inputs cannot advance authoritative time");
            c.Equal(pending, game.Clock.PendingTicks, "invalid elapsed inputs cannot replace a pending budget");
            game.Clock.TicksPerSecond = int.MaxValue; game.Clock.TicksPerFrame = int.MaxValue;
            c.Equal(int.MaxValue, game.Clock.TicksPerSecond, "no arbitrary maximum embedding tick rate remains");
            c.Equal(int.MaxValue, game.Clock.TicksPerFrame, "no calendar-day maximum update step remains");
        }

        private static void ContentAndReaders(Check c)
        {
            var hours = new JsonReader(Json.Parse("{\"hours\": 2147483647}"), "duration");
            c.Equal(128849018820L, TestGame.Time.Ticks(hours.Duration("hours", DurationUnit.Hour, 0)), "authored hours are not constrained by Q32.32 ticks");
            var overflow = new JsonReader(Json.Parse("{\"hours\":1e99}"), "duration");
            c.Throws<OverflowException>(() => TestGame.Time.Ticks(overflow.Duration("hours", DurationUnit.Hour, 0)), "content gets a typed duration-overflow diagnostic");
            c.Equal(3092376451680L, TestGame.Time.Days(int.MaxValue), "session duration conversion retains wide ticks");
            c.Throws<ArgumentOutOfRangeException>(() => new StateReader(new byte[8], 4, int.MaxValue), "reader bounds cannot wrap the offset sum");
            c.Throws<ArgumentOutOfRangeException>(() => new StateReader(new byte[8], int.MaxValue, 1), "reader offset is validated before indexing");
        }

        private static void Exhaustion(Check c)
        {
            c.Equal(2000000000, TileCoord.Steps(new TileCoord(-1000000000, 0), new TileCoord(1000000000, 0)), "large representable distance stays exact");
            c.Throws<OverflowException>(() => TileCoord.Steps(new TileCoord(int.MinValue, 0), new TileCoord(int.MaxValue, 0)), "coordinate difference cannot wrap before absolute value");
            c.Throws<OverflowException>(() => TileCoord.Octile10(new TileCoord(0, 0), new TileCoord(300000000, 0)), "unrepresentable path cost fails explicitly");
            c.Throws<OverflowException>(() => { int ignored = new TileRect(int.MaxValue, 0, 1, 1).Right; }, "rectangle endpoint cannot wrap");
            c.Throws<OverflowException>(() => { int ignored = new TileRect(0, 0, int.MaxValue, 2).Area; }, "rectangle allocation area cannot wrap");
            c.Throws<OverflowException>(() => new TileRect(0, 0, int.MaxValue, 1).Inflate(1), "inflation cannot wrap dimensions");
            // Test-only instrumentation is permitted; no public mutator or external module reflection is added.
            SimGame game = TestGame.New(55);
            typeof(GameClock).GetProperty(nameof(GameClock.StepIndex)).SetValue(game.Clock, long.MaxValue);
            Fix before = game.World.Entities.All[0].Get<Counter>().Value;
            c.Equal(0, game.Update(), "exhausted tick representation stops before commands or systems");
            c.That(game.Fault != null && game.Fault.Phase == "begin" && game.Tick == long.MaxValue, "exhaustion is a latched diagnostic, never a negative tick");
            c.Equal(before, game.World.Entities.All[0].Get<Counter>().Value, "numeric tick preflight executes no gameplay");
            c.Equal(0L, game.Clock.PendingTicks, "numeric fault cancels host backlog");
            SimGame identities = TestGame.New(56); Entity entity = identities.World.Entities.All[0];
            typeof(Entity).GetProperty("Version").SetValue(entity, int.MaxValue);
            c.Throws<OverflowException>(() => entity.Touch(), "entity version exhaustion cannot wrap");
            c.Equal(int.MaxValue, entity.Version, "refused version increment leaves original identity fact");
            int count = identities.World.Entities.All.Count;
            typeof(EntityStore).GetField("_nextId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(identities.World.Entities, int.MaxValue);
            c.Throws<OverflowException>(() => ((global::Beep.SimEngine.Maps.TileMap)identities.World.Space).SpawnAt(identities.World, identities.World.Content.Get<EntityDef>("bin"), new PlayerId(0), new TileCoord(20, 10)), "entity ID preflight refuses exhaustion before creation");
            identities.World.Settle(); c.Equal(count, identities.World.Entities.All.Count, "identity exhaustion spawns no partial entity");
        }
    }
}
