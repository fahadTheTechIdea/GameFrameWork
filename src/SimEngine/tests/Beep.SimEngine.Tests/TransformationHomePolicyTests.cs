using System;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class TransformationHomePolicyTests
    {
        internal const string AdmissionCase = "Output home policy: explicit JSON, pure admission and resolved physical homes";
        internal const string LossCase = "Output home policy: lost retained home blocks results and preserves physical WIP";
        internal const string ProviderCase = "Output home policy: independent processor uses retained homes and physical saved results";
        internal static void Register()
        { TestRunner.Add(AdmissionCase, Admission); TestRunner.Add(LossCase, Loss); TestRunner.Add(ProviderCase, Provider); }
        private static Entity Find(SimGame game, string id) => game.World.Entities.All.Single(e => e.Alive && e.Def.Id == id);
        private static TransformationManagementView View(SimGame game) => game.Seat(0).View.Feature<TransformationManagementView>();
        private static TransformationSnapshot State(SimGame game, string id = "berth.producer", string capability = "production")
        {
            if (!View(game).TryGetTransformation(Find(game, id).Id, new Code(capability), out var state)) throw new InvalidOperationException("missing actual process");
            return state;
        }
        private static void Step(Check c, SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("output-home fault/" + game.Fault + "/" + twin?.Fault);
            if (twin != null) c.Equal(game.Hash(), twin.Hash(), "every pending/creation/home update matches the original saved state");
        }
        private static EnqueueTransformationCommand Command(SimGame game, EntityId home = default)
            => new EnqueueTransformationCommand { Transformer = Find(game, "berth.producer").Id, Capability = new Code("production"), Transformation = "berth.worker", Count = 1, Home = home };
        private static void Admission(Check c)
        {
            foreach (string mode in new[] { "unassigned", "producer", "required", "override" })
            {
                var factory = ProducerHomeBerthTests.Factory(data => data.Get("transformations")[0].Set("output_home", JsonValue.Of(mode == "override" ? "unassigned" : mode)));
                var game = LiveSensorTests.New(factory); var producer = Find(game, "berth.producer");
                var command = Command(game); ulong hash = game.Hash();
                c.Equal(mode == "required" ? TransformationContracts.HomeRequired : Code.None, game.Seat(0).Preview(command), "authored mode controls missing-home admission/" + mode);
                c.Equal(hash, game.Hash(), "home resolution is pure/" + mode);
                c.Equal(Code.None, View(game).ReadExecutionPreview(producer.Id, new Code("production"), "berth.worker", out var preview), "immutable execution preview exposes actual recipe");
                c.Equal(mode == "producer" ? TransformationOutputHome.Producer : mode == "required" ? TransformationOutputHome.Required : TransformationOutputHome.Unassigned,
                    preview.OutputHome.Value, "preview uses actual authored contract, not a host decision");
                if (mode == "required" || mode == "override") command.Home = producer.Id;
                c.That(TestKit.Order(game, 0, command).Accepted, "ordinary policy-aware enqueue");
                EntityId resolved = mode == "unassigned" ? EntityId.None : producer.Id;
                c.Equal(resolved, State(game).Queue[0].Home, "original queue owns the resolved home identity/" + mode);
                var retained = State(game); var twin = factory.Load(game.Save());
                for (int i = 0; i < 25; i++) Step(c, game, twin);
                var worker = Find(game, "berth.observer");
                c.Equal(resolved, worker.Get<IWorker>().Home, "completion applies only the retained identity/" + mode);
                c.Equal(mode == "unassigned" ? WorkerHomeState.Unassigned : WorkerHomeState.AtHome, WorkerHomeContracts.Read(game.World, worker).State,
                    "physical worker state obeys selected policy/" + mode);
                c.Equal(resolved, retained.Queue[0].Home, "detached admitted queue facts do not change on completion");
                c.Equal(0, State(game).QueueCount, "full authored worker duration completed exactly once");
                var invalid = Command(game, new EntityId(-1)); hash = game.Hash();
                c.Equal(Codes.InvalidArgument, game.Seat(0).Preview(invalid), "negative home identity refuses without a fallback");
                c.Equal(hash, game.Hash(), "invalid home preview is pure");
            }
            foreach (string invalid in new[] { "missing", "unknown", "null", "numeric", "resource", "nonworker" })
                c.Throws<ContentException>(() => LiveSensorTests.New(ProducerHomeBerthTests.Factory(data => {
                    var row = data.Get("transformations")[0];
                    if (invalid == "missing") row.Remove("output_home");
                    if (invalid == "unknown") row.Set("output_home", JsonValue.Of("legacy"));
                    if (invalid == "null") row.Set("output_home", Json.Parse("null"));
                    if (invalid == "numeric") row.Set("output_home", JsonValue.Number("2"));
                    if (invalid == "resource") data.Get("transformations")[1].Set("output_home", JsonValue.Of("unassigned"));
                    if (invalid == "nonworker") row.Set("entity_output", JsonValue.Of("berth.source"));
                })), "invalid/inapplicable output-home metadata refuses before Initialize/" + invalid);
            c.Throws<ContentException>(() => LiveSensorTests.New(ProducerHomeBerthTests.Factory(data => {
                var unused = data.Get("transformations")[0].Clone(); unused.Set("id", JsonValue.Of("berth.unused")); unused.Remove("output_home"); data.Get("transformations").Add(unused);
            })), "unused recipes cannot hide a missing output-home declaration");
            AutomaticAndRepeat(c);
        }
        private static void AutomaticAndRepeat(Check c)
        {
            var factory = ProducerHomeBerthTests.Factory(data => {
                data.Get("transformations")[0].Set("output_home", JsonValue.Of("required"));
                data.Get("entities")[0].Get("components").Get("transformer").Set("idle_transformation", JsonValue.Of("berth.worker"));
            });
            var game = LiveSensorTests.New(factory);
            for (int i = 0; i < 3; i++) Step(c, game);
            c.Equal(TransformationContracts.HomeRequired, State(game).Blocker, "idle automatic recipe cannot invent a required home");
            c.Equal(0, State(game).QueueCount, "refused automatic home resolution appends no queue item");
            c.That(!game.World.Entities.All.Any(e => e.Def.Id == "berth.observer"), "refused automatic recipe creates no worker");
            c.That(TestKit.Order(game, 0, Command(game, Find(game, "berth.producer").Id)).Accepted, "normal explicit intent can satisfy an automatic recipe's required home");
            var twin = factory.Load(game.Save()); for (int i = 0; i < 20; i++) Step(c, game, twin);
            c.Equal(1, game.World.Entities.All.Count(e => e.Def.Id == "berth.observer"), "only admitted explicit execution creates a worker");
            c.Equal(TransformationContracts.HomeRequired, State(game).Blocker, "next automatic attempt retains the same explicit requirement");

            factory = ProducerHomeBerthTests.Factory(); game = LiveSensorTests.New(factory);
            var command = Command(game); command.Repeat = true;
            c.That(TestKit.Order(game, 0, command).Accepted, "repeat uses ordinary authored producer policy");
            for (int i = 0; i < 4; i++) Step(c, game);
            c.That(State(game).Queue.Count == 1 && State(game).Queue[0].Repeat && State(game).Queue[0].Home == Find(game, "berth.producer").Id,
                "repeat reuses the retained resolved home, not a new default request");
            twin = factory.Load(game.Save());
            foreach (var current in new[] { game, twin }) c.That(TestKit.Order(current, 0, new CancelTransformationCommand {
                Transformer = Find(current, "berth.producer").Id, Capability = new Code("production"), Index = 0 }).Accepted, "normal cancellation stops future repeat intent");
            for (int i = 0; i < 15; i++) Step(c, game, twin);
            c.Equal(1, game.World.Entities.All.Count(e => e.Def.Id == "berth.observer"), "cancelling repeat never fabricates another output");
            c.Equal(0, State(game).QueueCount, "cancelled repeat retires the original queue only");
        }
        private static void Loss(Check c)
        {
            foreach (bool removed in new[] { false, true })
            {
                var factory = ProducerHomeBerthTests.Factory(data => {
                    var row = data.Get("transformations")[0]; row.Set("output_home", JsonValue.Of("required"));
                    row.Set("inputs", Json.Parse("{\"steel\":4}")); row.Set("input_ports", Json.Parse("{\"steel\":\"stock\"}"));
                    data.Get("entities")[1].Get("components").Get("storage").Set("initial_stock", Json.Parse("{\"steel\":4}"));
                });
                var game = LiveSensorTests.New(factory); var home = Find(game, "berth.source");
                c.That(TestKit.Order(game, 0, Command(game, home.Id)).Accepted, "ordinary explicit home queued with real materials");
                for (int i = 0; i < 80 && State(game).Phase != TransformationPhase.Working; i++) Step(c, game);
                c.Equal(TransformationPhase.Working, State(game).Phase, "physical carrier supplied actual WIP before target loss");
                c.Equal(Fix.FromInt(4), State(game).Wip.Stock.AmountOf(game.World.Content.ResourceId("steel")), "active execution owns actual four-unit WIP");
                var twin = factory.Load(game.Save());
                // Explicit adverse-world fixture event. No host-side worker, resource or recovery logic.
                foreach (var current in new[] { game, twin })
                {
                    var target = Find(current, "berth.source");
                    if (removed) { current.World.Despawn(target); current.World.Settle(); }
                    else target.Owner = new PlayerId(1);
                }
                for (int i = 0; i < 12; i++) Step(c, game, twin);
                var state = State(game);
                c.Equal(removed ? Codes.NotFound : Codes.NotOwned, state.Blocker, "retained home loss is typed, never producer substitution");
                c.Equal(TransformationPhase.Blocked, state.Phase, "earned completion waits for actual selected home");
                c.Equal(home.Id, state.Queue[0].Home, "queue retains original lost identity");
                c.Equal(Fix.FromInt(4), state.Wip.Stock.AmountOf(game.World.Content.ResourceId("steel")), "refused result consumes no additional WIP");
                c.That(!game.World.Entities.All.Any(e => e.Def.Id == "berth.observer"), "no entity created before complete home/result admission");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "blocked missing/foreign-home completion remains durable");
                foreach (var current in new[] { game, twin }) c.That(TestKit.Order(current, 0, new CancelTransformationCommand {
                    Transformer = Find(current, "berth.producer").Id, Capability = new Code("production"), Index = 0 }).Accepted, "ordinary cancellation owns WIP disposition");
                for (int i = 0; i < 15 && (State(game).QueueCount != 0 || State(game).Phase != TransformationPhase.Idle); i++) Step(c, game, twin);
                c.Equal(0, State(game).QueueCount, "cancelled lost-home execution retires original queue");
                c.Equal(Fix.FromInt(4), Find(game, "berth.producer").Get<IInventory>().Amount(game.World.Content.ResourceId("steel")), "all physical inputs return to the original port, not a new home ledger");
            }
        }
        internal static GameFactory IndependentFactory(Action<JsonValue> editWorld = null, Action<JsonValue> editContent = null)
        {
            var root = Json.Parse(PointGame.Definitions); root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.transformer"));
            var output = root.Get("entities")[2].Clone(); output.Set("id", JsonValue.Of("home.worker")); root.Get("entities").Add(output);
            var data = Json.Parse(FrameworkProofContent.Read("transformer-proof.json"));
            data.Get("world_setup_profiles")[0].Set("entries", Json.Parse("[\"emitter-start\"]"));
            data.Get("transformations")[0].Set("entity_output", JsonValue.Of("home.worker")); data.Get("transformations")[0].Set("output_home", JsonValue.Of("producer"));
            data.Get("entities")[0].Get("components").Set("point_presence", Json.Parse("{\"space\":\"field.points\",\"position\":30}"));
            data.Get("entities")[0].Get("components").Set("point_dock", Json.Parse("{\"work_roles\":[],\"resources\":[],\"slots\":[{\"id\":\"launch\",\"kind\":\"rollout\",\"offset\":1},{\"id\":\"exit\",\"kind\":\"departure\",\"offset\":2}],\"rollouts\":[{\"id\":\"workers\",\"outputs\":[\"home.worker\"],\"starts\":[\"launch\"],\"exits\":[\"exit\"],\"max_expanded\":1024}]}"));
            data.Get("entities")[0].Get("components").Get("pulse_transformer").Get("states").Set("working", JsonValue.Of("processing"));
            data.Get("entity_setups")[0].Set("placement", Json.Parse("{\"space\":\"field.points\",\"position\":30}"));
            StartupContentAuthoring.Include(root, "transformer.start");
            editWorld?.Invoke(root); editContent?.Invoke(data);
            return PointGame.Factory(root.ToString(), additionalModule: new TransformerProofModule(),
                moduleDeclaration: "{\"id\":\"proof.transformer\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.transformations\",\"version\":\"1.0.0\"}],\"documents\":[\"transformer-home-proof.json\"]}", moduleDocument: data.ToString());
        }
        private static void Provider(Check c)
        {
            var factory = IndependentFactory();
            var game = factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 509 }); var producer = Find(game, "emitter");
            using var recorder = game.RecordReplay(new ReplayLimits(2 * 1024 * 1024, 1000));
            c.That(producer.Get<ITransformer>(new Code("pulse")) is PulseTransformer, "independent provider owns actual execution, not a native wrapper");
            c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Transformer = producer.Id, Capability = new Code("pulse"), Transformation = "pulse", Count = 2 }).Accepted,
                "same command queues independent physical outputs");
            c.That(State(game, "emitter", "pulse").Queue.All(entry => entry.Home == producer.Id), "independent queue receives resolved producer identities");
            var twin = factory.Load(game.Save());
            for (int i = 0; i < 20; i++) Step(c, game, twin);
            var workers = game.World.Entities.All.Where(e => e.Def.Id == "home.worker").ToArray();
            c.Equal(2, workers.Length, "independent timed executions create exactly two actual workers");
            c.That(workers.All(e => e.Get<IWorker>().Home == producer.Id), "independent completion uses shared worker-home binding owner");
            c.Equal(0, State(game, "emitter", "pulse").QueueCount, "independent queue drains without an unresolved fallback");
            var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), new ReplayLimits(2 * 1024 * 1024, 1000));
            c.That(replay.Success && replay.ActualHash == game.Hash(), "independent output homes replay through original command/creation/clock owners/" + replay.Failure);
        }
    }
}
