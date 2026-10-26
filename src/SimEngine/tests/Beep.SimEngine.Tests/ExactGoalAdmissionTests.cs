using System;
using System.Collections.Generic;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Testing;

namespace Beep.SimEngine.Tests
{
    internal static class ExactGoalAdmissionTests
    {
        internal const string Case = "Navigation exact goals: blocked admission preserves guarded costs budgets and region alternatives";
        internal static void Run(Check c)
        {
            var factory = SpatialLayerTests.Factory(); var game = factory.NewGame(new NewGameRequest { Scenario = "layers", Seed = 19 });
            var goal = new TileCoord(8, 8); var reads = new Dictionary<TileCoord, int>();
            int Cost(TileCoord tile) { reads[tile] = reads.TryGetValue(tile, out var value) ? value + 1 : 1; return tile == goal ? 0 : 10; }
            var grid = new NavGrid(game.World, new PathSearch(), "exact.proof", 10, Cost, null);
            ulong hash = game.Hash();
            c.Equal(PathSearchResult.Unreachable, grid.CanReach(new TileCoord(0, 0), new TileRect(goal, 1, 1), 64), "zero exact-goal entry proves unreachable without exhausting budget");
            c.Equal(2, reads.Count, "only original source and exact target need guarded cost reads");
            c.That(reads.ContainsKey(goal) && reads[goal] == 1, "exact target is actually read once, not guessed");
            reads.Clear();
            c.Equal(PathSearchResult.BudgetExceeded, grid.CanReach(new TileCoord(0, 0), new TileRect(goal, 1, 1), 0), "zero expansion budget retains original precedence");
            c.That(reads.Count == 1 && !reads.ContainsKey(goal), "zero budget does not invoke a new target callback");
            var path = new List<TileCoord> { new TileCoord(99, 99) };
            c.Equal(PathSearchResult.Unreachable, grid.FindPath(new TileCoord(0, 0), new TileRect(goal, 1, 1), path, 64), "blocked exact destination returns no route");
            c.Equal(0, path.Count, "rejected search clears any retained path");
            reads.Clear();
            c.Equal(PathSearchResult.Unreachable, grid.CanReach(goal, new TileRect(goal, 1, 1), 64), "blocked source retains original refusal before expansion");
            c.Equal(1, reads.Count, "same source/goal uses one actual cost");
            reads.Clear();
            c.Equal(PathSearchResult.Found, grid.FindPath(new TileCoord(6, 6), new TileRect(8, 8, 2, 1), path, 64), "multi-tile goal can reach its other passable member");
            c.That(path.Count > 0 && path[path.Count - 1] == new TileCoord(9, 8), "region goal does not incorrectly reject its blocked first tile");
            c.That(System.Linq.Enumerable.All(reads.Values, value => value == 1), "one original query-local cache, no repeated tile costs");
            reads.Clear();
            c.Equal(PathSearchResult.Found, grid.FindPath(new TileCoord(0, 0), new TileRect(3, 3, 1, 1), path, 64), "ordinary passable exact route is unchanged");
            c.That(path.Count == 3 && path[0] == new TileCoord(1, 1) && path[2] == new TileCoord(3, 3), "normal octile route and stable ordering remain");
            c.Equal(1, reads[new TileCoord(3, 3)], "early positive target cost is reused in the same search");
            c.Equal(hash, game.Hash(), "pure cost admission mutates no authoritative state");
            c.Equal(0L, game.Tick, "search never advances the game clock");
            c.Equal(hash, factory.Load(game.Save()).Hash(), "search scratch adds no saved model");

            foreach (string fault in new[] { "negative", "clock", "throw" })
            {
                var broken = factory.NewGame(new NewGameRequest { Scenario = "layers", Seed = 19 }); var original = new ApplicationException("exact goal original");
                int Bad(TileCoord tile) { if (tile != goal) return 10; if (fault == "clock") broken.Clock.Pause(); if (fault == "throw") throw original; return fault == "negative" ? -1 : 0; }
                var bad = new NavGrid(broken.World, new PathSearch(), "exact.fault", 10, Bad, null); Exception caught = null;
                try { bad.CanReach(new TileCoord(0, 0), new TileRect(goal, 1, 1), 64); } catch (Exception error) { caught = error; }
                c.That(caught != null && broken.Fault != null, "exact goal still uses full native observation: " + fault);
                if (fault == "throw") c.That(ReferenceEquals(original, caught), "original target callback error is retained");
                c.Throws<InvalidOperationException>(() => broken.Save(), "failed goal callback never creates a resumable prefix: " + fault);
            }
        }
    }
}
