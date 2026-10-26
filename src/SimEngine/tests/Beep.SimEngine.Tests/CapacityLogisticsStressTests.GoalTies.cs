using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    internal static partial class CapacityLogisticsStressTests
    {
        private static void GoalDirectedTies(Check c)
        {
            using var stream = typeof(Program).Assembly.GetManifestResourceStream("tests.navigation-goal-ties.json")
                ?? throw new InvalidOperationException("missing explicit goal-tie proof");
            using var text = new StreamReader(stream);
            var reader = new JsonReader(Json.Parse(text.ReadToEnd()), "navigation.goal-ties");
            if (reader.Int("schema") != 1 || reader.String("id") != "navigation.goal-ties") throw reader.Error("proof identity");
            int width = reader.Int("width"), height = reader.Int("height"), cost = reader.Int("cost"),
                maximumReads = reader.Int("maximum_reads"), budget = reader.Int("maximum_expanded"),
                steps = reader.Int("expected_steps"), expected = reader.Int("expected_cost");
            var journeys = reader.Array("journeys"); reader.End();
            if (width < 1 || height < 1 || cost < 1 || budget < 1 || maximumReads < 1 || journeys.Count == 0)
                throw new InvalidOperationException("positive authored search proof bounds required");
            var map = new TileMap("navigation.goal-ties", width, height);
            var search = new PathSearch();
            for (int i = 0; i < journeys.Count; i++)
            {
                var entry = new JsonReader(journeys[i], "navigation.goal-ties.journeys[" + i + "]");
                var from = new TileCoord(entry.Int("from_x"), entry.Int("from_y"));
                var to = new TileCoord(entry.Int("to_x"), entry.Int("to_y")); entry.End();
                var goal = new TileRect(to, 1, 1); var reads = new Dictionary<TileCoord, int>();
                int Cost(TileCoord tile)
                {
                    if (!map.InBounds(tile)) throw new InvalidOperationException("searched off-map tile");
                    reads[tile] = reads.TryGetValue(tile, out int previous) ? previous + 1 : 1;
                    return cost;
                }
                var path = new List<TileCoord>(); long started = Stopwatch.GetTimestamp();
                var result = search.Find(map, from, goal, Cost, cost, true, checked(width * height), path);
                double milliseconds = (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;
                c.Equal(PathSearchResult.Found, result, "actual original search finds authored optimal plateau");
                c.Equal(steps, path.Count, "minimum physical step count retained");
                long total = 0; bool legal = true; TileCoord at = from;
                foreach (var next in path)
                {
                    int dx = Math.Abs(next.X - at.X), dy = Math.Abs(next.Y - at.Y);
                    legal &= dx <= 1 && dy <= 1 && dx + dy > 0 && map.InBounds(next);
                    total += dx > 0 && dy > 0 ? (long)cost * 14 / 10 : cost; at = next;
                }
                c.That(legal && at == to, "every original path edge reaches the actual destination");
                c.Equal((long)expected, total, "identical optimal rounded path cost in every direction");
                c.That(reads.Values.All(count => count == 1), "query-local original cost memoization remains once per tile");
                c.That(reads.Count <= maximumReads, "equal-cost tie expansion stays within authored read budget");
                Console.WriteLine(FormattableString.Invariant($"  goal-tie build={EngineInfo.Build} journey={i} reads={reads.Count} route-cost={total} steps={path.Count} elapsed-ms={milliseconds:0.000}"));
                var repeat = new List<TileCoord>();
                c.Equal(PathSearchResult.Found, search.Find(map, from, goal, _ => cost, cost, true, budget, repeat),
                    "same authored80-expansion bound reaches the goal without increasing any engine cap");
                c.That(path.SequenceEqual(repeat), "search order and path repeat under a sufficient smaller budget");
                repeat.Add(from);
                c.Equal(PathSearchResult.BudgetExceeded, search.Find(map, from, goal, _ => cost, cost, true, 0, repeat), "zero expansion budget remains pending");
                c.Equal(0, repeat.Count, "budget refusal exposes no old route prefix");
            }
            var heap = new PathHeap();
            heap.Push(new PathHeap.Entry(new TileCoord(2, 2), 100, 9));
            heap.Push(new PathHeap.Entry(new TileCoord(1, 1), 100, 8));
            heap.Push(new PathHeap.Entry(new TileCoord(3, 3), 90, 1));
            c.Equal(new TileCoord(3, 3), heap.Pop().Tile, "lower total estimate ALWAYS precedes progress tie-break");
            c.Equal(new TileCoord(2, 2), heap.Pop().Tile, "equal total estimate prefers greater progress");
            c.Equal(new TileCoord(1, 1), heap.Pop().Tile, "less advanced equal-cost candidate remains queued");
            heap.Push(new PathHeap.Entry(new TileCoord(4, 2), 100, 9));
            heap.Push(new PathHeap.Entry(new TileCoord(3, 2), 100, 9));
            heap.Push(new PathHeap.Entry(new TileCoord(5, 1), 100, 9));
            c.Equal(new TileCoord(5, 1), heap.Pop().Tile, "equal cost/progress still prefers stable lower row");
            c.Equal(new TileCoord(3, 2), heap.Pop().Tile, "equal row still prefers stable lower column");
            c.Equal(new TileCoord(4, 2), heap.Pop().Tile, "heap loses no final tied candidate");
        }
    }
}
