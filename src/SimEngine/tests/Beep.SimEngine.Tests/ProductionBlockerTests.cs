using System;
using System.Collections.Generic;
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
    // Only authored composition, ordinary player input, views, Update, saves and replay.
    internal static class ProductionBlockerTests
    {
        internal const string Case = "Production blockers: physical supply workers fuel and full vessels preserve saved work and whole WIP";
        private static readonly Code Operation = new Code("space.operation");
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(string manifest = "production-blockers-master.json", string addition = null,
            Action<JsonValue> manifestEdit = null, IReadOnlyDictionary<string, string> documents = null, GameModule additionalModule = null)
        {
            var modules = new List<GameModule> {
                new ContentAccessModule(), new InventoryModule(), new OperationsModule(), new MotionModule(),
                new WorkModule(), new LogisticsModule(), new TransformationModule(), new WorldSetupModule(),
                new PointModule(), new TileNavigationModule(), new ResourceFlowsModule(),
                new EndpointProofModule(), new OutputPortProofModule() };
            if (additionalModule != null) modules.Add(additionalModule);
            var definition = new GameDefinition("production-blockers", modules);
            var master = Json.Parse(FrameworkProofContent.Read(manifest)); manifestEdit?.Invoke(master);
            string Read(string name) => documents != null && documents.TryGetValue(name, out var value) ? value : FrameworkProofContent.Read(name);
            var files = new PreloadedFileSource().Add("manifest.json", master.ToString())
                .Add("open-access.json", FrameworkProofContent.Access);
            foreach (string name in new[] { "planet-x-resources.json", "planet-x-recipes.json", "planet-x-world.json",
                "planet-x-work.json", "planet-x-logistics.json", "work-retry.json", "endpoints-flows.json", "production-blockers.json" })
                files.Add(name, Read(name));
            if (addition != null) files.Add(addition, Read(addition));
            if (documents != null) foreach (var pair in documents)
                if (pair.Key != addition && !new[] { "planet-x-resources.json", "planet-x-recipes.json", "planet-x-world.json",
                    "planet-x-work.json", "planet-x-logistics.json", "work-retry.json", "endpoints-flows.json", "production-blockers.json" }.Contains(pair.Key)) files.Add(pair.Key, pair.Value);
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        internal static SimGame New(GameFactory factory, string scenario = "production-blockers") => factory.NewGame(new NewGameRequest {
            Scenario = scenario, Seed = 918, Seats = { Controller.Human, Controller.Human } });
        internal static EntityId Actor(SimGame game, string name)
            => game.Seat(0).Snapshot.Entities.Single(e => e.Def == "block." + name || name == "factory" && e.Def == "block.return-factory").Id;
        internal static ResourceId Resource(SimGame game, string name)
        { if (!game.Seat(0).View.TryFindResource(name, out var id)) throw new InvalidOperationException("missing resource " + name); return id; }
        internal static TransformationManagementView View(SimGame game, int seat = 0)
            => game.Seat(seat).View.Feature<TransformationManagementView>();
        internal static TransformationSnapshot State(SimGame game)
        { if (!View(game).TryGetTransformation(Actor(game, "factory"), Operation, out var state)) throw new InvalidOperationException("missing actual process"); return state; }
        internal static Fix Stock(SimGame game, string actor, string resource)
        {
            var facts = new List<Facet>(); game.Seat(0).View.Inspect(Actor(game, actor), facts);
            return facts.Where(f => f.Key == Storage.FacetStock && f.Subject == resource).Aggregate(Fix.Zero, (sum, f) => sum + f.Value);
        }
        internal static Fix Output(SimGame game, string resource)
        {
            var result = View(game).ReadOutputPorts(Actor(game, "factory"), Operation, "block.smelt", out var ports);
            if (!result.IsNone) throw new InvalidOperationException("actual vessel read refused " + result);
            return ports.Single(p => p.Resource == Resource(game, resource)).Stock.AmountOf(Resource(game, resource));
        }
        internal static List<WorkJobView> Jobs(SimGame game)
        { var rows = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(rows); return rows; }
        internal static bool CarrierFree(SimGame game)
            => !Jobs(game).Any(j => j.Worker == Actor(game, "carrier") && (j.State == JobState.Assigned || j.State == JobState.Paused));
        internal static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("production blocker fault/divergence step=" + game.Clock.StepIndex + " " + game.Fault + "/" + twin?.Fault);
            Fix mass = State(game).Wip.Stock.AmountOf(Resource(game, "space.planetx.ore"))
                + Output(game, "space.alloy") + Output(game, "space.slag");
            Fix tools = Fix.Zero;
            foreach (string actor in new[] { "source", "factory", "receiver", "carrier" })
            {
                foreach (string resource in new[] { "space.planetx.ore", "space.alloy", "space.slag" }) mass += Stock(game, actor, resource);
                tools += Stock(game, actor, "space.crucible");
            }
            if (mass != Fix.FromInt(20) || tools != Fix.One)
                throw new InvalidOperationException("actual conserved stock/WIP step=" + game.Clock.StepIndex + " mass=" + mass + " tool=" + tools);
        }
        internal static void Reach(SimGame game, SimGame twin, Func<bool> done, string stage)
        {
            for (int i = 0; i < 256 && !done(); i++) Step(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " step=" + game.Clock.StepIndex + " phase=" + State(game).Phase
                + " blocker=" + State(game).Blocker + " work=" + State(game).Progress.WholeTicks + " jobs="
                + string.Join(";", Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.Blocker + "/" + j.ToilIndex)));
        }
        internal static void Input(Check c, SimGame game, Command input)
        {
            ulong before = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(input), "normal command preview");
            c.Equal(before, game.Hash(), "preview does not schedule supply or mutate resources");
            var result = TestKit.Order(game, 0, input);
            c.That(result.Accepted, "normal player input accepted " + result.Refusal);
            if (!result.Accepted) throw new InvalidOperationException("player input refused " + result.Refusal);
        }
        internal static void Haul(Check c, GameFactory factory, SimGame game, string source, string destination, string resource, int quantity, string port = null)
        {
            Reach(game, null, () => CarrierFree(game), "previous carrier departs");
            var command = new HaulResourcesCommand { Source = Actor(game, source), Destination = Actor(game, destination),
                Worker = Actor(game, "carrier"), Resource = Resource(game, resource), Ceiling = Fix.FromInt(quantity), Priority = 80,
                SourcePort = port == null ? Code.None : new Code(port) };
            Input(c, game, command);
            var twin = factory.Load(game.Save());
            Reach(game, twin, () => CarrierFree(game), "physical " + resource + " delivery");
            c.Equal(game.Hash(), twin.Hash(), "saved carrier moves loads delivers and departs without an instant transfer");
        }
        private static void Preserved(Check c, GameFactory factory, SimGame game, Code blocker, string stage)
        {
            var twin = factory.Load(game.Save());
            Reach(game, twin, () => State(game).Blocker == blocker, stage + " actual blocker");
            var before = State(game);
            for (int i = 0; i < 5; i++) Step(game, twin);
            var after = State(game);
            c.That(before.Progress.Equals(after.Progress) && before.Wip.Execution == after.Wip.Execution
                && before.Wip.Stock.AmountOf(Resource(game, "space.planetx.ore")) == after.Wip.Stock.AmountOf(Resource(game, "space.planetx.ore"))
                && before.Tools.Held.AmountOf(Resource(game, "space.crucible")) == after.Tools.Held.AmountOf(Resource(game, "space.crucible"))
                && before.Operating.ElapsedTicks == after.Operating.ElapsedTicks, stage + " retains exact earned work WIP tool and productive fuel exposure");
            c.Equal(game.Hash(), twin.Hash(), stage + " saved continuation identity");
        }
        private static void Run(Check c)
        {
            var factory = Factory(); var game = New(factory);
            var limits = new ReplayLimits(4 * 1024 * 1024, 4000); var recording = game.RecordReplay(limits);
            Input(c, game, new EnqueueTransformationCommand { Transformer = Actor(game, "factory"), Capability = Operation, Transformation = "block.smelt", Count = 2 });
            Preserved(c, factory, game, TransformerManager.BlockerTools, "startup tool shortage");
            Haul(c, factory, game, "source", "factory", "space.crucible", 1);
            Reach(game, null, () => State(game).Blocker == WorkSystem.BlockerMaterials, "authored staffing arrives but raw supply absent");
            Preserved(c, factory, game, WorkSystem.BlockerMaterials, "raw supply shortage");
            c.That(State(game).Wip.Stock.IsEmpty && State(game).Progress.WholeTicks == 0 && Stock(game, "source", "space.planetx.ore") == Fix.FromInt(20),
                "missing supply does not acquire startup WIP or promise completed products");
            Haul(c, factory, game, "source", "factory", "space.planetx.ore", 10);
            Preserved(c, factory, game, TransformerManager.BlockerOperatingInputs, "startup fuel shortage");
            c.That(State(game).Wip.Stock.IsEmpty && Stock(game, "factory", "space.planetx.ore") == Fix.FromInt(10), "unfunded first fuel draw cannot acquire raw WIP");
            Haul(c, factory, game, "source", "factory", "block.fuel", 4);
            Reach(game, null, () => State(game).Progress.WholeTicks > 0, "real productive work");
            var partial = State(game);
            c.That(partial.Required == 12 && partial.Wip.Stock.AmountOf(Resource(game, "space.planetx.ore")) == Fix.FromInt(10)
                && partial.Tools.Held.AmountOf(Resource(game, "space.crucible")) == Fix.One, "one minute of engine time owns actual raw WIP and reusable tool");
            Input(c, game, new ConfigureOperationCommand { Entity = Actor(game, "factory"), Operation = Operation, Enabled = false, Target = Fix.One });
            Preserved(c, factory, game, OperationCodes.Disabled, "disabled operation");
            Input(c, game, new SetWorkerHeldCommand { Worker = Actor(game, "operator"), Held = true });
            Input(c, game, new ConfigureOperationCommand { Entity = Actor(game, "factory"), Operation = Operation, Enabled = true, Target = Fix.One });
            Preserved(c, factory, game, StaffedTransformationGate.Shortage, "missing bound worker");
            c.That(State(game).Progress.Equals(partial.Progress), "operation and machine policies never erase partial work");
            Input(c, game, new SetWorkerHeldCommand { Worker = Actor(game, "operator"), Held = false });
            Preserved(c, factory, game, TransformerManager.BlockerOperatingInputs, "running fuel shortage");
            c.That(State(game).Progress.WholeTicks == 4 && State(game).Operating.Consumed.AmountOf(Resource(game, "block.fuel")) == Fix.FromInt(4),
                "four actual fuel units earn exactly four productive updates; later waits burn none");
            Haul(c, factory, game, "source", "factory", "space.planetx.ore", 10);
            Haul(c, factory, game, "source", "factory", "block.fuel", 10);
            Haul(c, factory, game, "source", "factory", "block.fuel", 10);
            Reach(game, null, () => State(game).Blocker == TransformerManager.BlockerOutput, "whole second result awaits full vessels");
            var full = State(game);
            c.That(full.Progress.Complete(full.Required) && full.Wip.Stock.AmountOf(Resource(game, "space.planetx.ore")) == Fix.FromInt(10)
                && Output(game, "space.alloy") == Fix.FromInt(6) && Output(game, "space.slag") == Fix.FromInt(4)
                && full.Operating.Consumed.AmountOf(Resource(game, "block.fuel")) == Fix.FromInt(12), "first result remains stocked; completed second work still owns raw WIP and exact fuel");
            Preserved(c, factory, game, TransformerManager.BlockerOutput, "both output vessels full");
            Haul(c, factory, game, "factory", "receiver", "space.alloy", 6, "space.product");
            Preserved(c, factory, game, TransformerManager.BlockerOutput, "waste vessel alone still full");
            c.That(Output(game, "space.alloy") == Fix.Zero && Output(game, "space.slag") == Fix.FromInt(4)
                && Stock(game, "receiver", "space.alloy") == Fix.FromInt(6), "free product vessel cannot publish a partial conversion while waste remains blocked");
            Haul(c, factory, game, "factory", "receiver", "space.slag", 4, "space.waste");
            Reach(game, null, () => State(game).QueueCount == 0, "physical waste delivery releases whole result");
            c.That(State(game).Wip.Stock.IsEmpty && State(game).Tools.Held.IsEmpty && Stock(game, "factory", "space.crucible") == Fix.One
                && Output(game, "space.alloy") == Fix.FromInt(6) && Output(game, "space.slag") == Fix.FromInt(4), "second conversion publishes all products and releases actual tool once");
            c.That(Stock(game, "source", "block.fuel") == Fix.Zero && Stock(game, "factory", "block.fuel") == Fix.Zero
                && Stock(game, "carrier", "block.fuel") == Fix.Zero, "two successful batches consume all and only twenty-four physically supplied fuel units");
            c.That(!View(game, 1).TryGetTransformation(Actor(game, "factory"), Operation, out _), "other company cannot read WIP");
            c.That(View(game, 1).ReadOutputPorts(Actor(game, "factory"), Operation, "block.smelt", out var hidden) == Codes.NotFound && hidden.Count == 0,
                "private internal output vessels remain hidden");
            c.That(partial.Wip.Stock.AmountOf(Resource(game, "space.planetx.ore")) == Fix.FromInt(10)
                && full.QueueCount == 1 && full.Progress.Complete(full.Required), "retained facts never alias completed mutable runtime");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "final public checkpoint");
            c.That(new ReplayVerifier().Verify(factory, recording.Finish(), limits).Success, "all policies and physical deliveries replay through ordinary engine inputs");
        }
    }
}
