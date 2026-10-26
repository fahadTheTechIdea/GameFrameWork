using System;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Tests
{
    internal static partial class NavigationObservationAllocationTests
    {
        internal const string Case = "Navigation cost reads: typed context guard parity and concurrent budgets";
        internal static void Register()
        {
            TestRunner.Add(Case, Run);
            TestRunner.Add("Navigation entry contexts: measured native and independent guarded callback allocation", EntryContexts);
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest
        { Scenario = "points", Seed = 9621, Seats = { Controller.Human, Controller.Human } });
        internal static void Guard(Check c, bool raw, string fault)
        {
            var factory = CapacityLogisticsStressTests.ObservationFactory(fault, raw);
            var game = New(factory); var idle = New(factory); ulong idleHash = idle.Hash();
            game.Update(); c.That(game.Fault == null, "explicit probe initializes and makes a healthy engine Update");
            long time = game.Clock.StepIndex; game.Clock.Pause();
            var before = game.Seat(0).Snapshot; var other = game.Seat(1).Snapshot;
            var view = game.Seat(0).View.Feature<NavigationObservationView>(); var tile = new TileCoord(3, 3);
            if (fault == "none" || fault == "rng-hash" || fault == "rng-save")
            {
                ulong hash = game.Hash();
                c.Equal(10, view.EntryCost(tile), "actual native/raw registration returns authored cost");
                c.Equal(0, view.EntryCost(new TileCoord(-1, 3)), "off-map is still a blocked entry");
                c.Equal(hash, game.Hash(), "value-context/raw callback reads preserve saved state");
                var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "healthy context-backed read saves identically");
                c.Equal(10, twin.Seat(0).View.Feature<NavigationObservationView>().EntryCost(tile), "restored session uses its own actual provider");
                c.Equal(game.Hash(), twin.Hash(), "interleaved provider query does not alter state");
                c.That(game.Fault == null, "valid guarded cost stays healthy");
            }
            else
            {
                Exception thrown = null;
                try { view.EntryCost(tile); } catch (Exception error) { thrown = error; }
                c.That(thrown != null, "native/raw callback violation exposes no cost result");
                c.That(game.Fault != null, "typed/shared observation boundary latches original fault");
                if (fault == "throw") c.That(thrown?.Message.Contains("proof.navigation-cost-original") == true, "actual callback exception survives invocation adapter");
                var original = game.Fault;
                game.Update(); c.That(ReferenceEquals(original, game.Fault), "next Update preserves first fault, never resumes compromised work");
                var after = game.Seat(0).Snapshot; var foreign = game.Seat(1).Snapshot;
                c.Equal(GameState.Faulted, after.State, "fault is published through normal engine view");
                c.Equal(before.Tick, after.Tick, "own publication retains last healthy time");
                c.Equal(other.Tick, foreign.Tick, "foreign publication retains its own healthy time");
                c.That(ReferenceEquals(before.Space, after.Space) && ReferenceEquals(other.Space, foreign.Space), "failed read publishes no mutated spatial prefix");
                c.Equal(0, after.Spawned.Count + after.Removed.Count + after.Changed.Count, "failed query invents no entity delta");
            }
            c.Equal(time, game.Clock.StepIndex, "provider reads never advance sole game clock");
            c.Equal(idleHash, idle.Hash(), "fault/read context never binds another session");
        }
        private static void Run(Check c)
        {
            foreach (bool raw in new[] { false, true }) foreach (string fault in new[] { "none", "throw", "clock", "space", "negative", "below-minimum" })
                Guard(c, raw, fault);
            Guard(c, true, "provider");
            CapacityLogisticsStressTests.Run(c); // Same public physical workload, no old registry case invocation.
        }
    }
}
