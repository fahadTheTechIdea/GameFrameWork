using System;
using System.IO;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Tests
{
    // Called inside the two existing complete lifecycle/save suites; no additional suite or update owner.
    internal static class SingleClockTests
    {
        internal static void Lifecycle(Check c)
        {
            SimGame game = TestGame.New(421);
            IGameClock clock = ((IGameCycle)game).Clock;
            c.That(ReferenceEquals(clock, game.Clock) && ReferenceEquals(clock, game.World.Clock), "world and lifecycle share exactly one clock instance");
            c.That(ReferenceEquals(clock.Time, game.SimulationTime) && ReferenceEquals(clock.Time, game.World.Time), "time conversion is the sole clock's immutable metadata");
            c.That(typeof(World).GetProperty(nameof(World.Tick)).GetSetMethod(true) == null, "world current time has no writable storage");
            c.That(typeof(GameClock).GetConstructors().Length == 0, "hosts cannot construct an unrelated session clock");
            c.That(typeof(IGameClock).GetMethod("CompleteStep") == null && typeof(IGameClock).GetProperty("StepIndex").CanWrite == false, "module clock contract cannot advance time");
            c.That(clock.TotalGameTime.IsZero && game.Time.ElapsedGameTime.IsZero, "Initialize runs no game time");
            c.Equal(1L, clock.Time.ExactTicks(clock.StepDuration), "authored duration of one update is exact");
            game.Clock.TicksPerSecond = 2;
            c.Equal(0, game.Update(250000), "partial platform update runs no game time");
            c.That(clock.TotalGameTime.IsZero && game.Time.ElapsedGameTime.IsZero, "pacing budget never becomes authoritative time");
            c.Equal(1, game.Update(250000), "the same clock converts earned platform input");
            c.Equal(1L, clock.StepIndex, "completed update advances the clock once");
            c.Equal(1L, clock.Time.ExactTicks(clock.TotalGameTime), "current total duration projects actual completed time");
            c.Equal(1L, clock.Time.ExactTicks(game.Time.ElapsedGameTime), "Update report has game duration, not platform duration");
            GameUpdate previous = game.Time;
            game.Clock.Pause();
            ulong paused = game.Hash();
            c.Equal(0, game.Update(1000000), "paused sole clock updates no systems or entities");
            c.That(game.Time.ElapsedGameTime.IsZero && clock.StepIndex == 1, "pause earns no simulated duration");
            c.Equal(paused, game.Hash(), "pause leaves actual simulation state unchanged");
            game.Clock.Resume(); game.Clock.Speed = 2;
            c.Equal(2, game.Update(), "speed changes completed updates through the same clock");
            c.Equal(3L, clock.Time.ExactTicks(clock.TotalGameTime), "total game duration includes all completed updates");
            c.Equal(2L, clock.Time.ExactTicks(game.Time.ElapsedGameTime), "speed reports the actual simulated duration");
            c.Equal(1L, clock.Time.ExactTicks(previous.ElapsedGameTime), "retained Update timing is immutable");
            c.Equal(game.Tick, game.Snapshot.Tick, "snapshot reads sole authoritative clock time");
            c.Equal(game.Tick, game.Seat(0).Snapshot.Tick, "private snapshot reads the same time");
            c.Equal(game.Tick, clock.Date.Tick, "calendar projects the same clock coordinate");

            SimGame state = StateGame.Factory().NewGame(new NewGameRequest { Scenario = "state-test", Seed = 421 });
            SignalA source = state.World.Entities.All[0].Get<SignalA>();
            state.Update();
            c.That(ReferenceEquals(source.ObservedClock, state.Clock), "entity update receives the world's actual clock contract");
            c.Equal(0L, source.ObservedTime, "entity runs at current update time before completion commits");
            c.Equal(1L, state.Clock.StepIndex, "all entity state updates settle before clock advancement");
            source.Throw = true;
            c.Equal(0, state.Update(), "failed entity phase reports no completed time");
            c.Equal(1L, state.Clock.StepIndex, "failed gameplay cannot advance the clock");
            c.That(state.Time.ElapsedGameTime.IsZero && state.Fault.Phase == "entity.state:1:signal_a[1]", "specific source fault preserves the original update coordinate");
            c.Equal(0, state.Update(), "faulted game never retries partial work");

            SimGame exhausted = TestGame.New(422);
            WideTimeTests.SetTick(exhausted, long.MaxValue);
            c.Equal(long.MaxValue, exhausted.Clock.Time.ExactTicks(exhausted.Clock.TotalGameTime), "total duration supports full-width time times authored quantum");
            c.Equal(0, exhausted.Update(), "time exhaustion refuses before gameplay");
            c.That(exhausted.Fault.Phase == "begin" && exhausted.Clock.StepIndex == long.MaxValue, "clock preflight never wraps time");
        }

        internal static void Persistence(Check c)
        {
            GameFactory factory = TestGame.Factory();
            SimGame a = TestGame.New(423);
            long start = (1L << 45) + 37;
            WideTimeTests.SetTick(a, start);
            a.Seat(0).Submit(new PlaceBin { At = new TileCoord(20, 10) });
            byte[] bytes = a.Save();
            SimGame b = factory.Load(bytes);
            c.That(!ReferenceEquals(a.Clock, b.Clock) && ReferenceEquals(b.Clock, b.World.Clock), "restored session has one independent clock, not a second wrapper clock");
            c.Equal(start, b.Clock.StepIndex, "save restores time into actual clock authority");
            c.Equal(start, b.Clock.Time.ExactTicks(b.Clock.TotalGameTime), "restored wide total duration remains exact");
            c.Equal(start, factory.Describe(bytes).Tick, "save metadata matches saved game-clock time");
            c.Equal(a.Hash(), b.Hash(), "current time uses the normal embedded save/hash codec");
            a.Update(); b.Update();
            c.Equal(a.Hash(), b.Hash(), "saved queued commands continue against the same time coordinate");
            c.Equal(start + 1, b.Clock.StepIndex, "restored clock advances once after normal Update");
            long otherTime = b.Clock.StepIndex;
            a.Update();
            c.Equal(otherTime, b.Clock.StepIndex, "interleaved sessions never share clock storage");
            b.Update();
            c.Equal(a.Hash(), b.Hash(), "interleaved sessions converge after the same completed updates");

            using (var memory = new MemoryStream())
            {
                var writer = new StateWriter(memory);
                a.Clock.Sync(writer); writer.Flush();
                c.Equal(8L, memory.Length, "clock has one full-width time field, not a duplicate timer codec");
                c.Equal(start + 2, BitConverter.ToInt64(memory.ToArray(), 0), "clock serializes its actual current time");
                c.Equal(start + 2, a.Clock.StepIndex, "writing the clock never changes time");
            }
            var bad = BitConverter.GetBytes(-1L);
            c.Throws<CorruptStateException>(() => a.Clock.Sync(new StateReader(bad, 0, bad.Length)), "negative decoded clock time rejects");
            c.Equal(start + 2, a.Clock.StepIndex, "invalid clock decode preserves original time");
            c.Throws<CorruptStateException>(() => a.Clock.Sync(new StateReader(new byte[7], 0, 7)), "truncated decoded clock time rejects");
            c.Equal(start + 2, a.Clock.StepIndex, "truncated decode publishes no partial current time");
            c.Throws<InvalidOperationException>(() => a.Clock.CompleteStep(start + 4), "clock cannot skip completed update identities");
            c.Equal(start + 2, a.Clock.StepIndex, "refused completion changes no current time");

            JsonValue root = Json.Parse(TestGame.Defs);
            JsonValue authored = root.Get("time_definitions")[0];
            authored.Set("quantum", Json.Parse("{\"numerator\":1,\"denominator\":3,\"unit\":\"second\"}"));
            authored.Set("calendar", JsonValue.Null());
            var fractionalFactory = new GameFactory(TestGame.Definition(), TestGame.Content(root.ToString()));
            SimGame fractional = fractionalFactory.NewGame(new NewGameRequest { Scenario = "flat", Seed = 424 });
            fractional.Update();
            c.Equal(1L, fractional.Clock.Time.ExactTicks(fractional.Clock.TotalGameTime), "nondecimal rational total time is not approximated");
            c.Equal(1L, fractional.Clock.Time.ExactTicks(fractional.Time.ElapsedGameTime), "nondecimal rational elapsed Update time is exact");
            c.That(fractional.Clock.Date == null, "clock invents no calendar when JSON explicitly selects none");
            SimGame restored = fractionalFactory.Load(fractional.Save());
            fractional.Update(); restored.Update();
            c.Equal(fractional.Hash(), restored.Hash(), "fractional-quantum clock save continuation remains deterministic");
            c.Equal(2L, restored.Clock.Time.ExactTicks(restored.Clock.TotalGameTime), "fractional restored current time is exact");
        }
    }
}
