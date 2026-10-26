using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class ActivityWorkDiagnosticsTests
    {
        public const string Case = "Activity work diagnostics: exact critical path, private physical progress and saved continuation";
        public static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory()
        {
            var definition = new GameDefinition("activity-work-diagnostics", new GameModule[] {
                new ContentAccessModule(), new InventoryModule(), new MotionModule(), new TileNavigationModule(),
                new WorkModule(), new CompanyKnowledgeModule(), new WorldSetupModule(), new InfrastructureModule() });
            var files = new PreloadedFileSource().Add("manifest.json", FrameworkProofContent.Read("authored-segment-master.json"))
                .Add("open-access.json", FrameworkProofContent.Access);
            foreach (string name in new[] { "authored-segment-world.json", "authored-segment-startup.json",
                "native-survey-navigation.json", "native-survey-work.json", "native-survey-knowledge.json",
                "work-retry.json", "authored-segment-work.json", "authored-segment-infrastructure.json" })
                files.Add(name, FrameworkProofContent.Read(name));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "native-survey", Seed = 756 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static WorkBoard Board(SimGame game) => game.World.Services.Get<WorkBoard>();
        private static ActivityProgressView Read(SimGame game, int order)
        {
            if (!game.Seat(0).View.Feature<WorkView>().TryReadProgress(order, out var value))
                throw new InvalidOperationException("missing progress for actual order " + order + " at " + game.Tick);
            return value;
        }
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("work diagnostics time=" + game.Tick + " " + game.Fault);
            if (twin != null) Update(twin);
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 900 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " nodes=" +
                string.Join(";", Board(game).Orders.SelectMany(o => o.Nodes.Select(n => o.Id + "/" + n.Id + "/" + n.State + "/" + n.Progress + "/" + n.Blocker))));
        }
        private static void Held(Check c, SimGame game, EntityId worker, bool held)
            => c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = worker, Held = held }).Accepted, "player changes actual machine hold");
        private static Order Begin(Check c, SimGame game, int y)
        {
            var command = new LayLineCommand { Line = "segment.road-line" };
            command.Tiles.AddRange(new[] { new TileCoord(16, y), new TileCoord(17, y), new TileCoord(18, y) });
            c.That(TestKit.Order(game, 0, command).Accepted, "player requests authored physical corridor");
            return Board(game).Orders.Last();
        }
        private static BigInteger Raw(long ticks) => (BigInteger)ticks << Fix.FractionalBits;
        private sealed class Dependency : IDependencyNode
        {
            public string Id { get; }
            public IReadOnlyList<string> Dependencies { get; }
            public Dependency(string id, params string[] after) { Id = id; Dependencies = Array.AsReadOnly(after); }
        }
        private static void Numeric(Check c, SimGame game)
        {
            c.Equal(Raw(long.MaxValue) - 1, new WorkProgress(0, 1).RemainingRaw(long.MaxValue), "full-width duration minus exact Q32 fraction");
            c.Equal(new BigInteger(1), new WorkProgress(long.MaxValue - 1, uint.MaxValue).RemainingRaw(long.MaxValue), "last Q32 unit remains exact");
            c.Equal(BigInteger.Zero, new WorkProgress(long.MaxValue).RemainingRaw(long.MaxValue), "exact full-width completion");
            c.Throws<InvalidOperationException>(() => new WorkProgress(1, 1).RemainingRaw(1), "fractional overshoot refuses without clamp");
            c.Throws<ArgumentOutOfRangeException>(() => WorkProgress.Zero.RemainingRaw(-1), "negative requirement refuses");
            var graph = new DependencyGraph<Dependency>(new[] { new Dependency("z", "bridge"), new Dependency("b"),
                new Dependency("bridge", "b", "a"), new Dependency("a"), new Dependency("zero", "z") }, "diagnostics");
            var weights = new Dictionary<string, BigInteger>(StringComparer.Ordinal) {
                ["a"] = Raw(long.MaxValue), ["b"] = Raw(long.MaxValue), ["bridge"] = 0, ["z"] = Raw(long.MaxValue), ["zero"] = 0 };
            var path = graph.CriticalPath(weights);
            c.Equal(Raw(long.MaxValue) * 2, path.Weight, "aggregate path exceeds long and Fix without a cap");
            c.Equal("a|bridge|z", string.Join("|", path.Nodes), "ordinal predecessor tie, zero connector retained and zero suffix trimmed");
            c.Throws<NotSupportedException>(() => ((IList<string>)path.Nodes).Clear(), "path is immutable");
            weights["a"] = 0;
            c.Equal("b|bridge|z", string.Join("|", graph.CriticalPath(weights).Nodes), "new weights select actual longest branch");
            c.Equal("a|bridge|z", string.Join("|", path.Nodes), "detached result does not alias caller weights");
            weights["b"] = 0;
            c.Equal("z", string.Join("|", graph.CriticalPath(weights).Nodes), "zero work prefixes trimmed");
            weights["z"] = 0;
            c.Equal(0, graph.CriticalPath(weights).Nodes.Count, "all earned work yields empty work path");
            weights["a"] = -1;
            c.Throws<ArgumentException>(() => graph.CriticalPath(weights), "negative weight refuses");
            weights["a"] = 0; weights.Remove("b");
            c.Throws<ArgumentException>(() => graph.CriticalPath(weights), "missing weight refuses");
            weights["foreign"] = 0;
            c.Throws<ArgumentException>(() => graph.CriticalPath(weights), "same count with foreign identity refuses");
            c.Throws<ArgumentNullException>(() => graph.CriticalPath(null), "null weights refuse");
            var terminals = new DependencyGraph<Dependency>(new[] { new Dependency("b"), new Dependency("a") }, "terminals");
            c.Equal("a", string.Join("|", terminals.CriticalPath(new Dictionary<string, BigInteger> { ["b"] = 1, ["a"] = 1 }).Nodes), "ordinal terminal tie independent of authoring order");
            c.Throws<ArgumentException>(() => terminals.CriticalPath(new Dictionary<string, BigInteger>(StringComparer.OrdinalIgnoreCase) { ["A"] = 1, ["b"] = 1 }), "caller comparer cannot substitute stable identity");
            var deep = new List<Dependency>(); var deepWeights = new Dictionary<string, BigInteger>();
            for (int i = 4095; i >= 0; i--) { deep.Add(new Dependency("n" + i, i == 0 ? Array.Empty<string>() : new[] { "n" + (i - 1) })); deepWeights.Add("n" + i, 1); }
            c.Equal(4096, new DependencyGraph<Dependency>(deep, "deep").CriticalPath(deepWeights).Nodes.Count, "iterative path has no recursion/platform size cap");

            IActivityGraph authored = game.World.Content.Get<ActivityDef>("segment.shared").Graph;
            var progress = authored.Nodes.ToDictionary(n => n.Id, n => WorkProgress.Zero, StringComparer.Ordinal);
            var analysis = authored.AnalyzeWork(game.World.Time, progress);
            c.Equal(Raw(game.World.Time.Ticks(GameDuration.Minutes(13))), analysis.TotalRequiredRaw, "typed activity analysis uses actual authored durations");
            c.Equal(Raw(game.World.Time.Ticks(GameDuration.Minutes(10))), analysis.CriticalWork.Weight, "parallel branch maximum not both branches summed");
            progress["inspect"] = new WorkProgress(0, 1);
            c.Equal(analysis.RemainingRaw - 1, authored.AnalyzeWork(game.World.Time, progress).RemainingRaw, "actual fractional progress is not rounded away");
            c.Equal(analysis.CriticalWork.Weight - 1, authored.AnalyzeWork(game.World.Time, progress).CriticalWork.Weight, "critical fractional work exact");
            progress["inspect"] = new WorkProgress(game.World.Time.Ticks(authored.Find("inspect").Duration) + 1);
            c.Throws<InvalidOperationException>(() => authored.AnalyzeWork(game.World.Time, progress), "activity overshoot refuses");
            progress.Remove("inspect"); progress["foreign"] = WorkProgress.Zero;
            c.Throws<ArgumentException>(() => authored.AnalyzeWork(game.World.Time, progress), "activity identity mismatch refuses");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "strict diagnostics identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no parallel saved progress authority");
            var factory = Factory(); var game = New(factory); Numeric(c, game);
            EntityId[] machines = game.World.Entities.All.Where(e => e.Def.Id == "segment.machine").Select(e => e.Id).ToArray();
            foreach (EntityId worker in machines) Held(c, game, worker, true);
            Order order = Begin(c, game, 10); var initial = Read(game, order.Id);
            c.Equal(Raw(game.World.Time.Ticks(GameDuration.Minutes(39))), initial.Work.TotalRequiredRaw, "three authored segment work totals");
            c.Equal(initial.Work.TotalRequiredRaw, initial.Work.RemainingRaw, "travel and holding earn no work");
            c.Equal(Raw(game.World.Time.Ticks(GameDuration.Minutes(30))), initial.Work.CriticalWork.Weight, "three sequential segments with parallel prerequisite work");
            c.Equal("segment.0.inspect|segment.0.install|segment.1.inspect|segment.1.install|segment.2.inspect|segment.2.install", string.Join("|", initial.Work.CriticalWork.Nodes), "engine calculates unfinished critical path for host");
            c.Equal(9, initial.UnfinishedNodes, "node settlement is separately visible");
            c.Equal(0, initial.EarnedUnsettledNodes, "unworked physical nodes not settled");
            ulong hash = game.Hash();
            for (int i = 0; i < 20; i++) Read(game, order.Id);
            c.Equal(hash, game.Hash(), "repeated diagnostics are pure");
            c.That(!game.Seat(1).View.Feature<WorkView>().TryReadProgress(order.Id, out var hidden) && hidden == null, "foreign company gets no partial diagnostics");
            c.That(!game.Seat(0).View.Feature<WorkView>().TryReadProgress(int.MaxValue, out var missing) && missing == null, "missing order no fabricated progress");
            c.Throws<NotSupportedException>(() => ((IList<string>)initial.Work.CriticalWork.Nodes).Clear(), "host cannot edit critical work path");
            foreach (EntityId worker in machines) Held(c, game, worker, false);
            Reach(game, () => order.Node("segment.0.prepare").Progress.WholeTicks > 0 && order.Node("segment.0.inspect").Progress.WholeTicks > 0, "actual parallel machine work");
            var moving = Read(game, order.Id);
            c.That(moving.Work.RemainingRaw < initial.Work.RemainingRaw && moving.Work.CriticalWork.Weight < initial.Work.CriticalWork.Weight, "only earned physical work reduces work projection");
            c.Equal(initial.Work.TotalRequiredRaw, moving.Work.TotalRequiredRaw, "authored total remains fixed");
            c.Equal(initial.Work.TotalRequiredRaw, initial.Work.RemainingRaw, "old snapshot remains detached");
            foreach (EntityId worker in machines) Held(c, game, worker, true);
            var paused = Read(game, order.Id);
            for (int i = 0; i < 8; i++) Update(game);
            c.Equal(paused.Work.RemainingRaw, Read(game, order.Id).Work.RemainingRaw, "held machines advance game clock but earn no work");
            c.Equal(paused.Work.CriticalWork.Weight, Read(game, order.Id).Work.CriticalWork.Weight, "holding preserves critical unfinished work");
            var twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "partial actual jobs/progress restore exactly");
            c.Equal(Read(game, order.Id).Work.RemainingRaw, Read(twin, order.Id).Work.RemainingRaw, "diagnostics derive from restored records");
            c.Equal(string.Join("|", Read(game, order.Id).Work.CriticalWork.Nodes), string.Join("|", Read(twin, order.Id).Work.CriticalWork.Nodes), "restored critical path identical");
            foreach (EntityId worker in machines) { Held(c, game, worker, false); Held(c, twin, worker, false); }
            Reach(game, () => order.Node("segment.0.install").State == NodeState.Done, "earned first segment", twin);
            var prefix = Read(game, order.Id);
            c.That(prefix.CompletedNodes >= 3 && prefix.Work.CriticalWork.Nodes.All(id => !id.StartsWith("segment.0.", StringComparison.Ordinal)), "completed prefix contributes no unfinished work path");
            c.Equal(game.Hash(), twin.Hash(), "saved physical progression identical");
            Reach(game, () => order.Nodes.All(n => n.Progress.Complete(game.World.Time.Ticks(order.Plan.Graph.Find(n.Id).Duration))), "all required work earned", twin);
            // This no-dock workflow can settle and retire in the final productive Update.
            // Read the completed actual records through the same graph analysis, not a fabricated retained order.
            var earned = order.Plan.Graph.AnalyzeWork(game.World.Time,
                order.Nodes.ToDictionary(n => n.Id, n => n.Progress, StringComparer.Ordinal));
            c.Equal(BigInteger.Zero, earned.RemainingRaw, "all actual productive work earned");
            c.Equal(BigInteger.Zero, earned.CriticalWork.Weight, "no nominal unfinished work remains");
            c.Equal(0, earned.CriticalWork.Nodes.Count, "earned work path empty");
            Reach(game, () => order.State == OrderState.Done, "normal final physical settlement", twin);
            c.Equal(game.Hash(), twin.Hash(), "full actual saved execution matches");
            c.Equal(9, order.NodesDone, "all authored nodes actually settle");
            c.That(!game.Seat(0).View.Feature<WorkView>().TryReadProgress(order.Id, out var completed) && completed == null,
                "retired completed order has no fabricated live diagnostics");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "settled session remains checkpointable");
            c.Equal(initial.Work.TotalRequiredRaw, initial.Work.RemainingRaw, "snapshot never becomes live state");

            game = New(factory); order = Begin(c, game, 14);
            Reach(game, () => order.Node("segment.0.inspect").Progress.WholeTicks > 0, "cancel partial work");
            var cancelledSnapshot = Read(game, order.Id);
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = order.Id }).Accepted, "player cancels real physical order");
            Reach(game, () => Board(game).Order(order.Id) == null, "cancelled order retirement");
            c.That(!game.Seat(0).View.Feature<WorkView>().TryReadProgress(order.Id, out var retired) && retired == null, "retired cancellation cannot invent completed progress");
            c.That(cancelledSnapshot.Work.RemainingRaw > 0, "detached cancelled intent retains actual unearned work");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancel cleanup checkpoints through same owner");
            c.That(!game.World.Services.Has<IEconomy>(), "physical diagnostics need no business module");
        }
    }
}
