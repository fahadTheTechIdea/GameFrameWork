using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ResourceDispositionTests;
using static Beep.SimEngine.Tests.PhysicalRecoveryTestContent;

namespace Beep.SimEngine.Tests
{
    public static class ActivityGraphTests
    {
        private static string Definitions => FrameworkProofContent.Read("activity-dag.json");
        public static void Register()
        {
            TestRunner.Add("Activity DAG: canonical graph validation and strict authored admission", Admission);
            TestRunner.Add("Activity DAG: named parallel physical work, private views and saved execution", Physical);
        }
        private static ActivityNodeDef Node(string id, params string[] dependencies)
            => ActivityNodeDef.Make(id, Timed.Value, Array.Empty<WorkerRoleId>(), GameDuration.Minutes(1), dependencies, null);
        private static readonly Lazy<IActivityExecutor> Timed = new Lazy<IActivityExecutor>(() => Colony.New().World.Content.Get<IActivityExecutor>("work.timed"));
        private static string Sequence(IActivityGraph graph)
        {
            var ids = new List<string>();
            foreach (IActivityNodeDefinition node in graph.Nodes) ids.Add(node.Id);
            return string.Join(",", ids);
        }
        private static string Cycle(params IActivityNodeDefinition[] nodes)
        {
            try { new ActivityGraph(nodes, "proof"); return "accepted"; }
            catch (ContentException exception) { return exception.Message; }
        }
        internal static void Admission(Check c)
        {
            var graph = new ActivityGraph(new[] { Node("join", "b", "a"), Node("b"), Node("a") }, "proof");
            var shuffled = new ActivityGraph(new[] { Node("a"), Node("join", "a", "b"), Node("b") }, "proof");
            c.Equal("a,b,join", Sequence(graph), "forward references compile in canonical identity order");
            c.Equal(Sequence(graph), Sequence(shuffled), "node and edge declaration order do not change topology");
            c.That(ReferenceEquals(graph.Find("join"), graph.Nodes[2]), "named lookup aliases the sole compiled definition");
            c.Equal("a", graph.Find("join").Dependencies[0], "dependency lists are canonical too");
            c.That(graph.Find("missing") == null && graph.Find(null) == null, "unknown identity never substitutes a node");
            c.Throws<NotSupportedException>(() => ((IList<IActivityNodeDefinition>)graph.Nodes).Clear(), "graph nodes are frozen");
            c.Throws<NotSupportedException>(() => ((IList<string>)graph.Find("join").Dependencies).Clear(), "graph edges are frozen");
            var source = new List<IActivityNodeDefinition> { Node("a") };
            var captured = new ActivityGraph(source, "proof"); source.Clear();
            c.Equal("a", Sequence(captured), "graph captures caller collection once");
            c.Throws<ContentException>(() => new ActivityGraph(new[] { Node(""), Node("a") }, "proof"), "empty node identity refused");
            c.Throws<ContentException>(() => new ActivityGraph(new[] { Node("a"), Node("a") }, "proof"), "duplicate identities refused");
            c.Throws<ContentException>(() => new ActivityGraph(new[] { Node("a", "missing") }, "proof"), "missing named edge refused");
            c.Throws<ContentException>(() => new ActivityGraph(new[] { Node("a", "a") }, "proof"), "self edge refused");
            c.Throws<ContentException>(() => new ActivityGraph(new[] { Node("a"), Node("b", "a", "a") }, "proof"), "duplicate edge refused");
            c.Throws<ContentException>(() => new ActivityGraph(new[] { Node("a", "") }, "proof"), "empty edge refused");
            string cycle = Cycle(Node("b", "a"), Node("a", "b"));
            c.That(cycle.Contains("a -> b -> a"), "cycle refusal identifies stable named path");
            c.Equal(cycle, Cycle(Node("a", "b"), Node("b", "a")), "cycle diagnosis is independent of declaration order");
            var deep = new List<IActivityNodeDefinition>();
            for (int i = 4095; i >= 0; i--) deep.Add(Node("n" + i, i == 0 ? Array.Empty<string>() : new[] { "n" + (i - 1) }));
            c.Equal(4096, new ActivityGraph(deep, "deep").Nodes.Count, "iterative compilation accepts a deep graph without a content size cap");
            deep[4095] = Node("n0", "n4095");
            c.That(Cycle(deep.ToArray()).Contains("dependency cycle"), "deep cycle validation does not use recursive stack depth");

            GameFactory factory = Factory(Definitions); SimGame game = New(factory);
            IActivityGraph authored = game.World.Content.Get<ActivityDef>("dag.parallel").Graph;
            c.Equal("electrical,foundation,finish", Sequence(authored), "master-listed JSON resolves the same graph");
            c.That(authored.HasMachineWork("finish") && authored.HasMachineWork(null), "physical effect ancestry follows both forward dependencies");
            c.That(!authored.HasMachineWork("missing"), "unknown ancestry never grants machine execution");
            string unrelated = Definitions.Replace("\"foundation\",\"electrical\"", "\"wait\"")
                .Replace("{\"id\":\"finish\",\"after\":[\"wait\"],\"effect\":\"dag.consume\"}",
                    "{\"id\":\"finish\",\"after\":[\"wait\"],\"effect\":\"dag.consume\"},{\"id\":\"wait\",\"hours\":0.1}");
            c.Throws<ContentException>(() => New(Factory(unrelated)), "unrelated machine branch does not authorize a physical completion effect");
            c.Throws<ContentException>(() => New(Factory(Definitions.Replace("\"activities\"", "\"orders\""))), "removed orders section is not accepted as compatibility content");
            c.Throws<ContentException>(() => New(Factory(Definitions.Replace("\"nodes\"", "\"steps\""))), "removed steps property is not accepted as compatibility content");
            c.Throws<ContentException>(() => New(Factory(Definitions.Replace("\"foundation\",\"electrical\"", "\"absent\",\"electrical\""))), "authored missing reference fails before execution");
            c.Throws<ContentException>(() => New(Factory(Definitions.Replace("\"electrical\"", "\"foundation\""))), "authored duplicate references/identities fail before execution");
            var h = new StateHasher(); game.World.Services.Get<WorkBoard>().Sync(h);
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, game.World.Services.Get<WorkBoard>().StateSchemaVersion, "named execution has one breaking codec version");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "save layout is explicitly broken rather than shimmed");
            ActivityDef definition = game.World.Content.Get<ActivityDef>("dag.parallel");
            ulong before = game.Hash();
            c.Throws<ContentException>(() => definition.Read(new JsonReader(Content.Json.Parse("{}"), "proof")), "shared activity cannot be reauthored");
            c.Throws<ContentException>(() => game.World.Content.Get<EntityDef>("tower").Component<ConstructibleDef>()
                .Read(new JsonReader(Content.Json.Parse("{}"), "proof")), "shared construction cannot be reauthored");
            c.Equal(before, game.Hash(), "refused reauthoring leaves shared definitions and session state unchanged");
        }

        private static Order Post(Check c, SimGame game, Entity target, string activity = "dag.parallel")
        {
            CommandResult result = TestKit.Order(game, 0, new PostOrderCommand { Target = target.Id, Plan = activity });
            c.That(result.Accepted, "player posts master-authored activity through the existing command");
            return game.World.Services.Get<WorkBoard>().Order(checked((int)result.Arg0));
        }
        private static void Reach(SimGame game, Func<bool> predicate, string stage, params SimGame[] twins)
        {
            for (int tick = 0; !predicate() && tick < 500; tick++)
            { Tick(game); foreach (SimGame twin in twins) Tick(twin); }
            if (!predicate())
            {
                var nodes = new List<string>();
                foreach (Order order in game.World.Services.Get<WorkBoard>().Orders)
                    foreach (ActivityNodeExecution node in order.Nodes) nodes.Add(order.Id + ":" + node.Id + "=" + node.State + "/" + node.Progress + "/" + node.Blocker);
                throw new InvalidOperationException(stage + " stalled at " + game.Tick + ": " + string.Join("; ", nodes));
            }
        }
        private static void Permit(Check c, SimGame game, EntityId target, bool enabled)
            => c.That(TestKit.Order(game, 0, new SetWorkPermitCommand { Target = target, Enabled = enabled }).Accepted, "player changes an engine-side requirement");
        private static ulong BoardHash(SimGame game)
        { var h = new StateHasher(); game.World.Services.Get<WorkBoard>().Sync(h); return h.Value; }

        internal static void Physical(Check c)
        {
            GameFactory factory = Factory(Definitions); SimGame game = New(factory);
            Entity target = Spawn(game, "dag.target", 20, 20);
            Entity secondCrane = Spawn(game, "crane", 10, 5);
            Order order = Post(c, game, target); WorkBoard board = game.World.Services.Get<WorkBoard>();
            ResourceId steel = game.World.Content.ResourceId("steel"), water = game.World.Content.ResourceId("water");
            c.Equal(3, order.Nodes.Count, "one runtime record per stable graph identity");
            c.That(ReferenceEquals(order.Node("foundation"), order.Nodes[1]), "named runtime lookup aliases its sole record");
            c.Equal(NodeState.Waiting, order.Node("finish").State, "join waits for both dependencies");
            c.Equal(new Code("work.dependencies"), order.Node("finish").Blocker, "dependency blocker is a named saved engine fact");
            c.Equal(NodeState.Waiting, order.Node("electrical").State, "first branch waits for actual materials");
            c.Equal(NodeState.Waiting, order.Node("foundation").State, "second branch waits for actual materials");
            int requests = 0;
            foreach (LogisticsService.Demand demand in game.World.Services.Get<LogisticsService>().Demands)
                if (demand.Source.Equals(new LogisticsDemandId(WorkModule.ModuleId, order.Id, Code.None)) && demand.Resource == steel)
                { requests++; c.Equal(Fix.FromInt(5), demand.Settings.Desired, "parallel branches aggregate their bills, not overwrite the same demand identity"); }
            c.Equal(1, requests, "one native order demand owns combined resource input");
            c.Equal(Fix.Zero, target.Get<IInventory>().Amount(steel), "request creates no delivered stock");
            SimGame supply = factory.Load(game.Save()); c.Equal(game.Hash(), supply.Hash(), "dependency/supply state restores named records and receipts");
            Reach(game, () => order.Node("foundation").Progress.WholeTicks >= 3 && order.Node("electrical").Progress.WholeTicks >= 3,
                "parallel physical arrival", supply);
            c.Equal(game.Hash(), supply.Hash(), "saved supply twin performs identical hauling and parallel execution");
            c.That(order.Node("foundation").JobId != 0 && order.Node("electrical").JobId != 0 &&
                order.Node("foundation").JobId != order.Node("electrical").JobId, "two real jobs own separate branches");
            c.That(board.Job(order.Node("foundation").JobId).Worker != board.Job(order.Node("electrical").JobId).Worker,
                "parallel execution requires two actual machines");
            c.That(secondCrane.Get<ITilePresence>().Position != FixVec2.Centre(new TileCoord(10, 5)), "machine travels over simulation time");
            c.Equal(NodeState.Waiting, order.Node("finish").State, "join cannot finish after only partial work");
            WorkView view = game.Seat(0).View.Feature<WorkView>(); var facts = new List<ActivityNodeView>();
            ulong before = game.Hash();
            c.That(view.TryCopyNodes(order.Id, facts), "owner reads detached per-node facts");
            c.Equal("electrical", facts[0].Id, "view follows canonical graph order");
            c.Equal(order.Node("electrical").Progress, facts[0].Progress, "view retains exact named progress");
            c.Throws<NotSupportedException>(() => ((IList<string>)facts[2].Dependencies).Clear(), "view dependencies are immutable");
            var foreign = new List<ActivityNodeView> { facts[0] };
            c.That(!game.Seat(1).View.Feature<WorkView>().TryCopyNodes(order.Id, foreign) && foreign.Count == 0, "foreign view reveals no node state");
            c.Equal(before, game.Hash(), "node diagnostics allocate no authoritative state or materials");
            WorkProgress detached = facts[0].Progress;
            Permit(c, game, target.Id, false);
            c.Equal(NodeState.Paused, order.Node("foundation").State, "typed guard pauses real node execution");
            c.Equal(new Code("dag.disabled"), order.Node("foundation").Blocker, "paused node records provider blocker");
            WorkProgress paused = order.Node("foundation").Progress;
            SimGame pause = factory.Load(game.Save()); c.Equal(game.Hash(), pause.Hash(), "paused job/requirement identity restores exactly");
            for (int i = 0; i < 4; i++) { Tick(game); Tick(pause); }
            c.Equal(paused, order.Node("foundation").Progress, "paused machines do not accrue hidden progress");
            c.Equal(game.Hash(), pause.Hash(), "paused saved continuation is deterministic");
            Permit(c, game, target.Id, true); Permit(c, pause, target.Id, true);
            Reach(game, () => !order.IsActive, "joined physical completion", pause);
            c.Equal(OrderState.Done, order.State, "both branches complete before their dependent effect");
            c.Equal(game.Hash(), pause.Hash(), "mid-work saved continuation has identical final resources and identities");
            c.Equal(Fix.FromRatio(3, 2), target.Get<IInventory>().Amount(water), "physical ancestry effect consumes native resource exactly once");
            foreach (ActivityNodeExecution node in order.Nodes)
            { c.Equal(NodeState.Done, node.State, "completed named state: " + node.Id); c.Equal(0, node.JobId, "finished job link cleared: " + node.Id); }
            c.Equal(detached, facts[0].Progress, "old immutable view does not follow later progress");
            SimGame complete = factory.Load(game.Save()); c.Equal(game.Hash(), complete.Hash(), "retained completed graph saves without stale pruned jobs");
            for (int i = 0; i < 4; i++) { Tick(game); Tick(complete); }
            c.Equal(Fix.FromRatio(3, 2), target.Get<IInventory>().Amount(water), "saved completion does not replay its effect");
            c.Equal(game.Hash(), complete.Hash(), "completed saved continuation remains identical");

            TimersAndCancellation(c, factory);
            SingleMachine(c, factory);
            CorruptBindings(c, factory);
            DeclarationOrder(c);
        }
        private static void TimersAndCancellation(Check c, GameFactory factory)
        {
            SimGame game = New(factory); Entity target = Spawn(game, "dag.target", 20, 20);
            Order timer = Post(c, game, target, "dag.timer");
            Reach(game, () => timer.Node("wait").Progress.WholeTicks >= 2, "timed node");
            Permit(c, game, target.Id, false); WorkProgress progress = timer.Node("wait").Progress;
            SimGame saved = factory.Load(game.Save());
            for (int i = 0; i < 3; i++) { Tick(game); Tick(saved); }
            c.Equal(progress, timer.Node("wait").Progress, "non-machine timer also respects typed pause");
            c.Equal(game.Hash(), saved.Hash(), "paused timer is saved by the same node codec");
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = timer.Id }).Accepted, "player cancels the ordinary graph execution");
            c.Equal(OrderState.Cancelled, timer.State, "cancel terminates the aggregate execution");
            foreach (ActivityNodeExecution node in timer.Nodes)
            { c.Equal(NodeState.Cancelled, node.State, "unfinished node is explicitly cancelled"); c.Equal(0, node.JobId, "cancelled node has no live job link"); }
            c.Equal(progress, timer.Node("wait").Progress, "cancellation retains real historical progress");
            SimGame cancelled = factory.Load(game.Save());
            c.Equal(progress, cancelled.World.Services.Get<WorkBoard>().Order(timer.Id).Node("wait").Progress, "retained cancelled graph restores exact named progress");
            c.Equal(game.Hash(), cancelled.Hash(), "cancelled saved state is identical");
            for (int i = 0; i < 4; i++) { Tick(game); Tick(cancelled); }
            c.Equal(progress, timer.Node("wait").Progress, "cancelled execution cannot resume silently");
            c.Equal(game.Hash(), cancelled.Hash(), "cancelled saved continuation is deterministic");

            SimGame running = New(factory); Entity machineTarget = Spawn(running, "dag.target", 20, 20);
            Order machine = Post(c, running, machineTarget);
            Reach(running, () => machine.Node("electrical").Progress.WholeTicks >= 2, "machine cancellation");
            WorkProgress machineProgress = machine.Node("electrical").Progress;
            c.That(TestKit.Order(running, 0, new CancelOrderCommand { Order = machine.Id }).Accepted, "cancelling an assigned physical graph frees its jobs");
            foreach (ActivityNodeExecution node in machine.Nodes) c.Equal(0, node.JobId, "physical cancellation clears owned job link");
            foreach (Job job in running.World.Services.Get<WorkBoard>().Jobs) c.That(job.OrderId != machine.Id, "cancelled jobs are pruned by the sole board");
            foreach (LogisticsService.Demand demand in running.World.Services.Get<LogisticsService>().Demands)
                c.That(!demand.Source.Equals(new LogisticsDemandId(WorkModule.ModuleId, machine.Id, Code.None)), "cancellation withdraws graph input requests");
            c.Equal(machineProgress, machine.Node("electrical").Progress, "physical cancellation retains attained work");
            c.Equal(running.Hash(), factory.Load(running.Save()).Hash(), "physical cancellation leaves no dangling saved commitments");
        }
        private static void CorruptBindings(Check c, GameFactory factory)
        {
            SimGame game = New(factory); Entity target = Spawn(game, "dag.target", 20, 20);
            Order order = Post(c, game, target);
            Reach(game, () => order.Node("electrical").JobId != 0, "corrupt binding fixture");
            WorkBoard board = game.World.Services.Get<WorkBoard>(); Job job = board.Job(order.Node("electrical").JobId);
            string id = job.NodeId; job.NodeId = "missing";
            c.Throws<CorruptStateException>(() => factory.Load(game.Save()), "live saved job cannot substitute an unknown graph node");
            job.NodeId = id; int link = order.Node("electrical").JobId; order.Node("electrical").JobId = 0;
            c.Throws<CorruptStateException>(() => factory.Load(game.Save()), "orphan live saved job is rejected");
            order.Node("electrical").JobId = link;
            WorkProgress progress = order.Node("electrical").Progress; order.Node("electrical").Progress = new WorkProgress(31);
            c.Throws<CorruptStateException>(() => factory.Load(game.Save()), "saved node cannot exceed its real required duration");
            order.Node("electrical").Progress = progress;
            var stream = new MemoryStream(); var writer = new StateWriter(stream); board.Sync(writer); writer.Flush();
            byte[] raw = stream.ToArray(), name = Encoding.UTF8.GetBytes("electrical");
            int position = -1;
            for (int i = 0; i <= raw.Length - name.Length; i++)
            {
                bool match = true; for (int k = 0; k < name.Length; k++) match &= raw[i + k] == name[k];
                if (match) { position = i; break; }
            }
            c.That(position >= 0, "sole codec serializes stable node identity");
            foreach (string replacement in new[] { "foundation", "missing___" })
            {
                byte[] corrupt = (byte[])raw.Clone(); Array.Copy(Encoding.UTF8.GetBytes(replacement), 0, corrupt, position, 10);
                c.Throws<CorruptStateException>(() => new WorkBoard(game.World, board.RetryPolicy).Sync(new StateReader(corrupt, 0, corrupt.Length)),
                    "sole named codec rejects duplicate or missing node identity: " + replacement);
            }
            byte[] missing = (byte[])raw.Clone(); Array.Copy(BitConverter.GetBytes(2), 0, missing, position - 8, 4);
            c.Throws<CorruptStateException>(() => new WorkBoard(game.World, board.RetryPolicy).Sync(new StateReader(missing, 0, missing.Length)), "sole named codec rejects missing record count");
        }
        private static void SingleMachine(Check c, GameFactory factory)
        {
            SimGame game = New(factory); Entity target = Spawn(game, "dag.target", 20, 20);
            Order order = Post(c, game, target);
            Reach(game, () => order.Node("electrical").Progress.WholeTicks > 0 || order.Node("foundation").Progress.WholeTicks > 0, "single-worker contention");
            ActivityNodeExecution waiting = order.Node("electrical").Progress.WholeTicks > 0 ? order.Node("foundation") : order.Node("electrical");
            c.Equal(WorkProgress.Zero, waiting.Progress, "ready parallel branch cannot work without a second physical machine");
            c.That(waiting.JobId != 0 && game.World.Services.Get<WorkBoard>().Job(waiting.JobId).State == JobState.Open,
                "unassigned branch waits in the existing job board");
            c.Equal(WorkSystem.BlockerWorkerBusy, waiting.Blocker, "unassigned named node reports the actual existing machine is busy");
            SimGame saved = factory.Load(game.Save());
            Reach(game, () => !order.IsActive, "sequential provider reuse", saved);
            c.Equal(OrderState.Done, order.State, "one actual machine can execute parallel intent sequentially");
            c.Equal(game.Hash(), saved.Hash(), "saved contention resumes identical provider assignment and timing");
            c.Equal(Fix.FromRatio(3, 2), target.Get<IInventory>().Amount(game.World.Content.ResourceId("water")), "joined effect still commits once after sequential execution");
        }
        private static void DeclarationOrder(Check c)
        {
            string json = Definitions;
            JsonValue reordered = Content.Json.Parse(json); JsonValue nodes = reordered.Get("activities")[0].Get("nodes");
            var reverse = new StringBuilder("[");
            for (int i = nodes.Count - 1; i >= 0; i--) { if (i != nodes.Count - 1) reverse.Append(','); nodes[i].WriteCanonical(reverse); }
            reverse.Append(']'); reordered.Get("activities")[0].Set("nodes", Content.Json.Parse(reverse.ToString()));
            var authored = new StringBuilder(); reordered.WriteCanonical(authored);
            GameFactory first = Factory(json), second = Factory(authored.ToString());
            SimGame a = New(first), b = New(second);
            Entity ta = Spawn(a, "dag.target", 20, 20), tb = Spawn(b, "dag.target", 20, 20);
            Spawn(a, "crane", 10, 5); Spawn(b, "crane", 10, 5);
            Order oa = Post(c, a, ta), ob = Post(c, b, tb);
            c.Equal(Sequence(oa.Plan.Graph), Sequence(ob.Plan.Graph), "reordered JSON yields identical topology");
            c.Equal(BoardHash(a), BoardHash(b), "named board state ignores declaration order, not raw content fingerprint");
            Reach(a, () => !oa.IsActive, "reordered physical execution", b);
            c.Equal(OrderState.Done, ob.State, "reordered authored workflow also completes");
            c.Equal(BoardHash(a), BoardHash(b), "finished named execution state is identical across reordered declarations");
            c.Equal(ta.Get<IInventory>().Amount(a.World.Content.ResourceId("water")), tb.Get<IInventory>().Amount(b.World.Content.ResourceId("water")),
                "reordered declarations preserve native effect outcome");
        }
    }
}
