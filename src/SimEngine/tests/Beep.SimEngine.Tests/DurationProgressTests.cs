using System.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class DurationProgressTests
    {
        public static void Register()
        {
            TestRunner.Add("Durations: exact JSON scaling and full-width fractional progress", Numeric);
            TestRunner.Add("Durations: physical work, transformation and lines retain saved progress", Physical);
        }

        private static long Duration(string text)
            => TestGame.Time.Ticks(new JsonReader(Json.Parse("{\"hours\":" + text + "}"), "duration").Duration("hours", DurationUnit.Hour, 0));

        private static void Numeric(Check c)
        {
            foreach (var example in new (string Text, long Ticks)[] {
                ("0", 0), ("-0e999999999999999999999", 0), ("0.05", 3), ("0.1", 6),
                ("1.25", 75), ("1e2", 6000), ("1e-2", 1), ("2147483647", 128849018820L),
                ("0.0166666666666666666666666666666666666", 1),
                ("0.0166666666666666666666666666666666667", 2),
                ("1e-999999999999999999999", 1), ("1e-9223372036854775808", 1),
                ("153722867280912930.1", long.MaxValue - 1), ("153722867280912930.11", long.MaxValue) })
                c.Equal(example.Ticks, Duration(example.Text), "exact authored hours " + example.Text);
            // Independent rational oracle: no Q32 rounding before the final tick ceiling.
            var random = new Random(216);
            for (int i = 0; i < 32; i++)
            {
                long numerator = random.NextInt64(1, 1L << 48); int exponent = random.Next(1, 10);
                BigInteger divisor = BigInteger.Pow(10, exponent);
                long expected = (long)(((BigInteger)numerator * 60 + divisor - 1) / divisor);
                c.Equal(expected, Duration(numerator.ToString(CultureInfo.InvariantCulture) + "e-" + exponent), "duration matches independent rational ceiling " + i);
            }
            foreach (string overflow in new[] { "153722867280912930.12", "1e99", "1e999999999999999999999" })
                c.Throws<OverflowException>(() => Duration(overflow), "unrepresentable duration refuses explicitly: " + overflow);
            c.Throws<ContentException>(() => Duration("-0.00000000000000000001"), "negative duration cannot vanish through fixed-point rounding");
            foreach (string invalid in new[] { "01", ".5", "1.", "+1", "1e", "--1", "", null })
                c.Throws<FormatException>(() => JsonValue.Number(invalid), "all numeric JSON construction uses the same lexical validation");
            c.Throws<ContentException>(() => Json.Parse("{\"x\":01}"), "JSON parsing shares the strict number grammar with contextual diagnostics");
            c.Equal(JsonKind.Number, Json.Parse("1e99").Kind, "JSON lexical validity is independent of numeric representation");
            c.Throws<ContentException>(() => new JsonReader(Json.Parse("{\"x\":1e99}"), "quantity").Fix("x"), "quantity reader still enforces its own fixed-point range");
            c.Equal(60L, TestGame.Time.Ticks(new JsonReader(Json.Parse("{}"), "duration").Duration("hours", DurationUnit.Hour, 1)), "whole-hour fallback does not format through approximate Fix text");

            var progress = new WorkProgress(int.MaxValue, 0xc0000000);
            progress = progress.Advance(Fix.FromRaw(0x80000000), (long)int.MaxValue + 10);
            c.Equal((long)int.MaxValue + 1, progress.WholeTicks, "fractional carry crosses the former whole-tick boundary");
            c.Equal(0x40000000u, progress.Fraction, "fractional work remains exact after carry");
            c.That(!progress.TryAsFix(out _), "wide elapsed work is not silently narrowed to a quantity");
            c.That(new WorkProgress(int.MaxValue, uint.MaxValue).TryAsFix(out Fix scalar) && scalar.Raw == long.MaxValue, "last representable scalar retains every fractional bit");
            c.Equal(new WorkProgress(long.MaxValue), new WorkProgress(long.MaxValue - 1, uint.MaxValue).Advance(Fix.One, long.MaxValue), "completion clamps real overshoot without overflowing");
            c.Equal(WorkProgress.Zero, new WorkProgress(5).Advance(Fix.One, 0), "zero-duration completion has no invented work");
            c.Throws<ArgumentOutOfRangeException>(() => new WorkProgress(-1), "negative stored progress refuses");
            c.Throws<ArgumentOutOfRangeException>(() => progress.Advance(-Fix.One, long.MaxValue), "negative work cannot reverse elapsed progress");
            c.Throws<ArgumentOutOfRangeException>(() => progress.Complete(-1), "negative work requirements refuse");
            c.Equal(Fix.FromRaw((long)((((BigInteger)progress.WholeTicks << 32) + progress.Fraction) / long.MaxValue)), progress.FractionOf(long.MaxValue), "presentation fraction does not narrow the elapsed ticks");
            using (var stream = new MemoryStream())
            {
                var writer = new StateWriter(stream); writer.Work(ref progress); writer.Flush();
                c.Equal(12L, stream.Length, "one progress codec stores long whole ticks and unsigned Q32 fraction");
                var reader = new StateReader(stream.ToArray(), 0, (int)stream.Length); WorkProgress restored = default; reader.Work(ref restored);
                c.Equal(progress, restored, "same codec restores wide whole ticks and high fraction bits");
                c.That(reader.AtEnd, "progress codec consumes exactly its layout");
                var a = new StateHasher(); a.Work(ref progress); var b = new StateHasher(); b.Work(ref restored);
                c.Equal(a.Value, b.Value, "saved and restored progress hashes identically");
                restored = new WorkProgress(restored.WholeTicks, restored.Fraction + 1); b = new StateHasher(); b.Work(ref restored);
                c.That(a.Value != b.Value, "hash includes fractional work, not just its whole ticks");
            }
            using (var stream = new MemoryStream())
            {
                var writer = new StateWriter(stream); long whole = -1; int fraction = 0;
                writer.Long(ref whole); writer.Int(ref fraction); writer.Flush();
                var reader = new StateReader(stream.ToArray(), 0, (int)stream.Length);
                c.Throws<CorruptStateException>(() => { WorkProgress invalid = default; reader.Work(ref invalid); }, "malformed saved negative elapsed work refuses");
            }
        }

        private static string Definitions()
            => Colony.Defs
                .Replace("\"hours\": 2", "\"hours\": 40000000")
                .Replace("\"hours\": 3, \"after\"", "\"hours\": 0.05, \"after\"")
                .Replace("\"hours\": 1, \"salvage\"", "\"hours\": 40000000, \"salvage\"")
                .Replace("\"duration\": { \"numerator\": 3, \"denominator\": 1, \"unit\": \"hour\" }, \"entity_output\"", "\"duration\": { \"numerator\": 40000000, \"denominator\": 1, \"unit\": \"hour\" }, \"entity_output\"")
                .Replace("\"duration\":{\"numerator\":900,\"denominator\":1,\"unit\":\"second\"}", "\"duration\":{\"numerator\":80000000,\"denominator\":1,\"unit\":\"hour\"}")
                .Replace("\"worker_roles\":", @"
  ""signals"": [{ ""id"": ""elapsed-work"", ""kind"": ""number"", ""metric"": ""transformation.progress"", ""unit"": ""unit.work-ticks"", ""providers"": [{ ""component"": ""transformer"", ""schema"": 1 }] }],
  ""worker_roles"":");

        private static void Reach(SimGame game, Func<bool> condition, int budget, string stage)
        {
            TestKit.RunUntil(game, condition, budget, 1);
            if (!condition()) throw new InvalidOperationException(stage + " stalled at tick " + game.Tick + "; fault=" + game.Fault);
        }
        private static bool Working(WorkBoard board, Order order, int step = 0)
        {
            Job job = board.Job(order.Nodes[step].JobId);
            return job != null && job.State == JobState.Assigned && job.ToilIndex < job.Toils.Count && job.Toils[job.ToilIndex].Executor == WorkPhases.Productive;
        }
        private static OrderView ViewOrder(SimGame game, int id)
        {
            var orders = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
            foreach (OrderView view in orders) if (view.Id == id) return view;
            throw new InvalidOperationException("missing order facts " + id);
        }

        private static void Physical(Check c)
        {
            GameFactory factory = Colony.Factory(Definitions()); SimGame game = factory.NewGame(new NewGameRequest { Scenario = "colony" });
            WorkBoard board = game.World.Services.Get<WorkBoard>();
            Entity crane = game.World.Entities.Get(Colony.Mine(game, 0, "crane")[0].Id); FixVec2 start = crane.Get<global::Beep.SimEngine.Maps.ITilePresence>().Position;
            CommandResult build = TestKit.Order(game, 0, new ConstructCommand { Def = "tower", Placement = Beep.SimEngine.Maps.TilePlacement.Arguments(game.World.Space.Id, new TileCoord(10, 16)), Priority = 50 });
            c.That(build.Accepted, "normal public construction accepts a wide authored duration");
            Entity site = game.World.Entities.Get(build.Entity); Order order = board.Order(site.Get<Site>().Order);
            c.Equal(2400000000L, game.SimulationTime.Ticks(order.Plan.Graph.Nodes[0].Duration), "authored construction goal is wider than int ticks");
            Reach(game, () => Working(board, order), 200, "construction travel");
            c.That(crane.Get<global::Beep.SimEngine.Maps.ITilePresence>().Position != start && order.IsActive, "wide work still waits for physical machine arrival");
            // Boundary instrumentation is only in framework tests, after normal dispatch and arrival.
            order.Nodes[0].Progress = new WorkProgress(game.SimulationTime.Ticks(order.Plan.Graph.Nodes[0].Duration) - 2, 0x80000000);
            OrderView facts = ViewOrder(game, order.Id);
            c.Equal(order.Nodes[0].Progress, facts.CurrentProgress, "immutable work projection retains wide fractional progress");
            c.Equal(2400000000L, facts.CurrentNeeded, "work projection retains the actual wide goal");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "wide physical work restores through the one work-board codec");
            game.RunTicks(1); twin.RunTicks(1);
            c.That(order.Nodes[0].State != NodeState.Done, "fractional elapsed work cannot finish early");
            game.RunTicks(1); twin.RunTicks(1);
            c.Equal(NodeState.Done, order.Nodes[0].State, "remaining physical work reaches its exact goal");
            c.Equal(new WorkProgress(2400000000L), order.Nodes[0].Progress, "completion records the real goal, not overshot work");
            c.Equal(game.Hash(), twin.Hash(), "wide boundary completion has identical saved continuation");
            int elapsed = TestKit.RunUntil(game, () => Colony.Mine(game, 0, "tower").Count == 1, 400, 1); twin.RunTicks(elapsed);
            c.Equal(1, Colony.Mine(game, 0, "tower").Count, "later material delivery and construction still finish physically");
            c.Equal(game.Hash(), twin.Hash(), "full construction, material bill and worker release match saved twin");

            Entity tower = game.World.Entities.Get(Colony.Mine(game, 0, "tower")[0].Id);
            CommandResult demolish = TestKit.Order(game, 0, new DemolishCommand { Target = tower.Id });
            c.That(demolish.Accepted, "demolition uses the existing physical command");
            var removalPlans = new List<PlanView>(); game.Seat(0).View.Feature<WorkView>().CopyPlans(removalPlans);
            Order removal = board.Order(removalPlans.Find(p => p.Id == demolish.Arg0).Order);
            c.Equal(2400000000L, game.SimulationTime.Ticks(removal.Plan.Graph.Nodes[0].Duration), "generated demolition retains wide authored duration");
            Reach(game, () => Working(board, removal), 200, "demolition arrival");
            removal.Nodes[0].Progress = new WorkProgress(game.SimulationTime.Ticks(removal.Plan.Graph.Nodes[0].Duration) - 1, 0x80000000);
            twin = factory.Load(game.Save()); game.RunTicks(3); twin.RunTicks(3);
            c.That(game.World.Entities.Get(tower.Id) == null, "physical demolition completes and removes the structure");
            c.Equal(game.Hash(), twin.Hash(), "saved demolition rebuilds the same wide generated plan");

            Entity producerEntity = game.World.Entities.Get(Colony.Mine(game, 0, "factory")[0].Id); Transformer transformer = producerEntity.Get<Transformer>();
            c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Capability = new Code("transformation"), Transformer = producerEntity.Id, Transformation = "build_truck" }).Accepted, "public queue accepts a wide transformation");
            Reach(game, () => transformer.Phase == TransformationPhase.Working, 400, "transformation supply");
            c.That(producerEntity.Get<IInventory>().Amount(game.World.Content.ResourceId("steel")) == Fix.Zero, "transformation consumes physically supplied inputs once");
            transformer.Progress = new WorkProgress(2400000000L - 2, 0x80000000);
            TransformationManagementView work = game.Seat(0).View.Feature<TransformationManagementView>(); work.TryGetTransformation(producerEntity.Id, new Code("transformation"), out TransformationSnapshot transformation);
            c.Equal(transformer.Progress, transformation.Progress, "transformation view does not narrow elapsed work");
            c.Equal(2400000000L, transformation.Required, "transformation view exposes the exact transformation goal");
            var facets = new List<Facet>(); game.Seat(0).View.Inspect(producerEntity.Id, facets);
            Facet gauge = facets.Find(f => f.Key == Transformer.FacetProgress);
            c.Equal(transformer.Progress.FractionOf(transformation.Required), gauge.Value, "engine projects wide progress into a normalized gauge");
            c.Equal(Fix.One, gauge.Max, "normalized progress gauge has a representable maximum");
            OperationsView operations = game.Seat(0).View.Feature<OperationsView>(); ulong before = game.Hash();
            c.Equal(Code.None, operations.ReadSignal(producerEntity.Id, new Code("transformation"), "elapsed-work", out AutomationSignalSnapshot signal), "authored scalar signal resolves through the existing provider");
            c.That(!signal.Available && signal.Blocker == OperationCodes.SignalNumericRange, "scalar signal reports unrepresentable work instead of a fake value");
            c.Equal(before, game.Hash(), "wide diagnostics mutate no authoritative state");
            twin = factory.Load(game.Save()); game.RunTicks(1); twin.RunTicks(1);
            c.Equal(1, transformer.Queue.Count, "wide transformation does not finish one tick early");
            game.RunTicks(1); twin.RunTicks(1);
            c.Equal(0, transformer.Queue.Count, "wide transformation completes through normal transformation");
            c.Equal(3, Colony.Mine(game, 0, "truck").Count, "physical rollout creates exactly one vehicle");
            c.Equal(game.Hash(), twin.Hash(), "transformation progress, input bill and rollout restore identically");

            var path = new List<TileCoord> { new TileCoord(12, 17), new TileCoord(13, 17) };
            c.That(TestKit.Order(game, 0, new LayLineCommand { Line = "pipe", Tiles = path }).Accepted, "line construction uses its normal command");
            LineWork lineWork = game.World.Entities.Components<LineWork>()[0]; Order lineOrder = board.Order(lineWork.Order);
            c.Equal(4800000000L, game.SimulationTime.Ticks(lineOrder.Plan.Graph.Nodes[0].Duration), "each segment keeps the full tick representation");
            c.Equal(2, lineOrder.Nodes.Count, "two physical stages do not merge elapsed work");
            Reach(game, () => Working(board, lineOrder), 200, "line arrival");
            lineOrder.Nodes[0].Progress = new WorkProgress(4800000000L - 2, 0x80000000);
            twin = factory.Load(game.Save()); game.RunTicks(1); twin.RunTicks(1);
            ByteLayer layer = ((global::Beep.SimEngine.Maps.TileMap)game.World.Space).Layer<ByteLayer>("pipe");
            c.Equal((byte)0, layer[path[0]], "fractional progress cannot stamp a tile before its whole requirement");
            game.RunTicks(2); twin.RunTicks(2);
            c.Equal((byte)1, layer[path[0]], "wide progress stamps the first completed tile");
            c.Equal((byte)0, layer[path[1]], "unfinished later tiles remain absent");
            c.Equal(game.Hash(), twin.Hash(), "partial line work and wide progress restore identically");
            lineOrder.Nodes[1].Progress = new WorkProgress(game.SimulationTime.Ticks(lineOrder.Plan.Graph.Nodes[1].Duration) - 1);
            twin = factory.Load(game.Save());
            int lineElapsed = TestKit.RunUntil(game, () => lineOrder.State == OrderState.Done, 60, 1); twin.RunTicks(lineElapsed);
            c.That(lineElapsed > 1, "machine follows the moved line marker physically before completing the final tile");
            c.Equal((byte)1, layer[path[1]], "wide line completion lays the final tile");
            c.Equal(game.Hash(), twin.Hash(), "line completion uses the same generated plan and progress codec");

            SimGame overflow = Colony.Factory(Definitions().Replace("\"duration\":{\"numerator\":80000000,\"denominator\":1,\"unit\":\"hour\"}", "\"duration\":{\"numerator\":9223372036854775807,\"denominator\":1,\"unit\":\"minute\"}")).NewGame(new NewGameRequest { Scenario = "colony" });
            ILineConstruction lines = overflow.World.Services.Get<ILineConstruction>(); ILineDefinition definition = overflow.World.Content.Get<ILineDefinition>("pipe");
            ulong hash = overflow.Hash();
            c.Equal(LineCodes.DurationRange, lines.Check(new PlayerId(0), definition, LineDeclaration.ReadPath(overflow.World, definition.Layer.Space.Id, TilePlacement.PathArguments(definition.Layer.Space.Id, path))), "duration overflow refuses in the same check used by command and preview");
            c.Throws<ArgumentException>(() => lines.Begin(new PlayerId(0), definition, LineDeclaration.ReadPath(overflow.World, definition.Layer.Space.Id, TilePlacement.PathArguments(definition.Layer.Space.Id, path)), 0), "direct provider entry cannot create a partial marker on invalid duration");
            c.Equal(hash, overflow.Hash(), "refused line duration allocates no identities or work state");
            c.Equal(LineCodes.DurationRange, TestKit.Order(overflow, 0, new LayLineCommand { Line = "pipe", Tiles = path }).Refusal, "public command returns the same typed duration refusal");
            c.Equal(0, overflow.World.Entities.Components<LineWork>().Count, "refused line command creates no work marker");
        }
    }
}
