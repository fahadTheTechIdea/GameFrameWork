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
    // Fictional recipes are JSON. This driver issues player inputs and verifies engine-owned facts only.
    internal static class PlanetXProductionTests
    {
        internal const string Case = "Planet X production: authored smelting and count assembly with physical saved queues and whole outputs";
        private static readonly Code Operation = new Code("space.operation");
        private static readonly string[] Actors = { "source", "smelter", "assembler", "receiver", "carrier" };
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(string recipes = null, string omit = null, Action<JsonValue> worldEdit = null, PriorActivityProofModule proof = null,
            BidirectionalPortModule ports = null, Action<JsonValue> portEdit = null,
            StartupCommandBindingsModule bindings = null, Action<JsonValue> bindingEdit = null, Action<JsonValue> masterEdit = null)
        {
            var modules = new List<GameModule> {
                new ContentAccessModule(), new InventoryModule(), new OperationsModule(), new MotionModule(),
                new WorkModule(), new LogisticsModule(), new TransformationModule(), new WorldSetupModule(), new PointModule() };
            if (proof != null) modules.Add(proof);
            if (ports != null) modules.Add(ports);
            if (bindings != null) modules.Add(bindings);
            if ((proof == null ? 0 : 1) + (ports == null ? 0 : 1) + (bindings == null ? 0 : 1) > 1) throw new ArgumentException("select one explicit proof master");
            var definition = new GameDefinition("planet-x", modules);
            var master = Json.Parse(FrameworkProofContent.Read(bindings != null ? "startup-command-bindings-master.json" : ports != null ? "bidirectional-ports-master.json" : proof == null ? "planet-x-production-master.json" : "production-preview-fault-master.json"));
            masterEdit?.Invoke(master);
            var files = new PreloadedFileSource().Add("manifest.json", master.ToString())
                .Add("open-access.json", FrameworkProofContent.Access).Add("work-retry.json", FrameworkProofContent.Read("work-retry.json"));
            if (proof != null) files.Add("production-preview-fault.json", FrameworkProofContent.Read("production-preview-fault.json"));
            if (bindings != null)
            {
                var authoredBindings = Json.Parse(FrameworkProofContent.Read("startup-command-bindings.json")); bindingEdit?.Invoke(authoredBindings);
                files.Add("startup-command-bindings.json", authoredBindings.ToString());
            }
            if (ports != null)
            {
                var authoredPorts = Json.Parse(FrameworkProofContent.Read("bidirectional-ports.json")); portEdit?.Invoke(authoredPorts);
                files.Add("bidirectional-ports.json", authoredPorts.ToString());
            }
            foreach (string name in new[] { "resources", "recipes", "world", "work", "logistics" })
            {
                string path = "planet-x-" + name + ".json";
                if (path == omit) continue;
                string document = name == "recipes" && recipes != null ? recipes : FrameworkProofContent.Read(path);
                if (name == "world" && (worldEdit != null || ports != null || bindings != null))
                {
                    var authored = Json.Parse(document);
                    if (ports != null) authored.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(ports.Id));
                    if (bindings != null) authored.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(bindings.Id));
                    worldEdit?.Invoke(authored); document = authored.ToString();
                }
                files.Add(path, document);
            }
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        internal static SimGame New(GameFactory factory, string scenario = "planet-x")
        {
            var request = new NewGameRequest { Scenario = scenario, Seed = 916 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        internal static EntityId Actor(SimGame game, string name)
            => game.Seat(0).Snapshot.Entities.Single(e => e.Def == "space." + name).Id;
        internal static ResourceId Resource(SimGame game, string name)
        {
            if (!game.Seat(0).View.TryFindResource("space." + name, out var id)) throw new InvalidOperationException("missing visible resource " + name);
            return id;
        }
        internal static Fix Stock(SimGame game, string entity, string resource)
        {
            var rows = new List<Facet>(); game.Seat(0).View.Inspect(Actor(game, entity), rows);
            return rows.Where(f => f.Key == Storage.FacetStock && f.Subject == "space." + resource).Aggregate(Fix.Zero, (sum, f) => sum + f.Value);
        }
        private static TransformationManagementView View(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<TransformationManagementView>();
        internal static TransformationSnapshot Process(SimGame game, string actor)
        {
            if (!View(game).TryGetTransformation(Actor(game, actor), Operation, out var facts))
                throw new InvalidOperationException("missing named processor " + actor);
            return facts;
        }
        private static void Integrity(SimGame game)
        {
            Fix mass = Fix.Zero, blanks = Fix.Zero, tools = Fix.Zero;
            foreach (string actor in Actors)
            {
                foreach (string resource in new[] { "planetx.ore", "alloy", "slag" }) mass += Stock(game, actor, resource);
                blanks += Stock(game, actor, "blank") + Stock(game, actor, "finished") * Fix.FromInt(3);
                tools += Stock(game, actor, "crucible") + Stock(game, actor, "fixture");
            }
            mass += Process(game, "smelter").Wip.Stock.AmountOf(Resource(game, "planetx.ore"));
            blanks += Process(game, "assembler").Wip.Stock.AmountOf(Resource(game, "blank"));
            if (mass != Fix.FromInt(30) || blanks != Fix.FromInt(12) || tools != Fix.FromInt(2))
                throw new InvalidOperationException("actual physical stock/WIP accounting at " + game.Clock.StepIndex + ": mass=" + mass + "/blanks=" + blanks + "/tools=" + tools);
        }
        internal static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null || twin != null && game.Hash() != twin.Hash())
                throw new InvalidOperationException("Planet X step=" + game.Clock.StepIndex + " fault=" + game.Fault + "/" + twin?.Fault);
            Integrity(game);
        }
        private static void Reach(SimGame game, SimGame twin, Func<bool> ready, string stage)
        {
            for (int i = 0; i < 512 && !ready(); i++) Step(game, twin);
            if (!ready()) throw new InvalidOperationException(stage + " step=" + game.Clock.StepIndex
                + " smelt=" + Process(game, "smelter").Phase + "/" + Process(game, "smelter").Blocker + "/" + Process(game, "smelter").Progress.WholeTicks
                + " assembly=" + Process(game, "assembler").Phase + "/" + Process(game, "assembler").Blocker + "/" + Process(game, "assembler").Progress.WholeTicks
                + " ore=" + Stock(game, "source", "planetx.ore") + " blanks=" + Stock(game, "source", "blank"));
        }
        internal static EnqueueTransformationCommand Queue(SimGame game, string actor, string recipe, int count = 1)
            => new EnqueueTransformationCommand { Transformer = Actor(game, actor), Capability = Operation, Transformation = recipe, Count = count };
        private static void Input(Check c, SimGame game, Command command)
        {
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "normal command preview");
            c.Equal(hash, game.Hash(), "preview changes no state or RNG");
            c.That(TestKit.Order(game, 0, command).Accepted, "normal player command accepted"); Integrity(game);
        }
        private static void Run(Check c)
        {
            c.Throws<ContentException>(() => New(Factory(omit: "planet-x-recipes.json")), "listed recipe file has no implicit substitute");
            var invalid = Json.Parse(FrameworkProofContent.Read("planet-x-recipes.json"));
            invalid.Get("transformations")[0].Get("input_ports").Set("space.planetx.ore", JsonValue.Of("space.missing"));
            c.Throws<ContentException>(() => New(Factory(invalid.ToString())), "unknown named port refuses before initialization");
            invalid = Json.Parse(FrameworkProofContent.Read("planet-x-recipes.json"));
            invalid.Get("transformations")[1].Get("inputs").Set("space.blank", JsonValue.Number("1.5"));
            c.Throws<ContentException>(() => New(Factory(invalid.ToString())), "fractional discrete recipe input refuses");
            invalid = Json.Parse(FrameworkProofContent.Read("planet-x-recipes.json"));
            invalid.Get("transformations")[0].Get("duration").Set("numerator", JsonValue.Number("-1"));
            c.Throws<ContentException>(() => New(Factory(invalid.ToString())), "negative authored duration refuses");
            var factory = Factory(); var game = New(factory);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "actual production engine identity");
            c.That(game.GameMode.Properties.Count == 0 && game.Modules.Count == 9
                && game.Modules.All(id => id == "points" || id.StartsWith("sim.", StringComparison.Ordinal)), "explicit resource-only mode with no money progression or controllers");
            c.Equal(5, game.Seat(0).Snapshot.Entities.Count, "only five authored original actors");
            var resources = new List<ResourceInfo>(); game.Seat(0).View.CopyResources(resources);
            c.That(resources.Single(r => r.Id == "space.planetx.ore").QuantityUnit.Unit == new UnitId("space.kg")
                && resources.Single(r => r.Id == "space.blank").QuantityUnit.Unit == new UnitId("space.count"), "distinct mass/count units from resource file");
            c.That(View(game).TransformationsOf(Actor(game, "smelter"), Operation).SequenceEqual(new[] { "space.smelt.planetx" })
                && View(game).TransformationsOf(Actor(game, "assembler"), Operation).SequenceEqual(new[] { "space.assemble.parts" }), "both new recipes bind actual named processor contracts");
            c.Equal(Code.None, View(game).ReadOutputPorts(Actor(game, "smelter"), Operation, "space.smelt.planetx", out var ports), "whole named output metadata");
            c.That(ports.Count == 2 && ports.Select(p => p.Endpoint).Distinct().Count() == 1
                && ports.Select(p => p.Port).Distinct().Count() == 2, "alloy/waste ports share one actual stock/capacity endpoint, not duplicate buffers");
            c.Equal(Code.None, View(game).ReadMonetaryCost(Actor(game, "assembler"), Operation, "space.assemble.parts", out var cost), "optional monetary facts");
            c.That(cost.Applicability == MonetaryCostApplicability.NotAuthored && !cost.EffectiveAmount.HasValue, "no budget or account prerequisite");
            var limits = new ReplayLimits(4 * 1024 * 1024, 4000); var replay = game.RecordReplay(limits);
            var smelt = Queue(game, "smelter", "space.smelt.planetx", 3);
            var assembly = Queue(game, "assembler", "space.assemble.parts", 2);
            foreach (var command in new[] { smelt, assembly })
            { c.Equal(Code.None, game.Seat(0).Preview(command), "simultaneous ordinary queue admission"); game.Seat(0).Submit(command); }
            c.That(Process(game, "smelter").QueueCount == 0 && Process(game, "assembler").QueueCount == 0, "saved pending input is not instant production");
            var twin = factory.Load(game.Save()); Step(game, twin);
            c.That(Process(game, "smelter").QueueCount == 3 && Process(game, "assembler").QueueCount == 2
                && Stock(game, "smelter", "alloy") == Fix.Zero && Stock(game, "assembler", "finished") == Fix.Zero, "normal Update owns queues and physical shortage");
            Reach(game, twin, () => Process(game, "smelter").Phase == TransformationPhase.Working
                && Process(game, "smelter").Progress.WholeTicks > 0, "first physically supplied smelt");
            var retained = Process(game, "smelter");
            c.That(retained.Required == 6 && retained.Wip.Stock.AmountOf(Resource(game, "planetx.ore")) == Fix.FromInt(10)
                && retained.Tools.Held.AmountOf(Resource(game, "crucible")) == Fix.One, "thirty seconds, ten original ore WIP, one tool held at actual source");
            c.That(!View(game, 1).TryGetTransformation(Actor(game, "smelter"), Operation, out _), "other company sees no private process/WIP");
            c.Equal(Codes.NotFound, game.Seat(1).Preview(smelt), "foreign queue command shares private preview refusal");
            Input(c, game, new ConfigureOperationCommand { Entity = Actor(game, "smelter"), Operation = Operation, Enabled = false, Target = Fix.One });
            twin = factory.Load(game.Save()); var paused = Process(game, "smelter").Progress;
            for (int i = 0; i < 4; i++) Step(game, twin);
            c.That(Process(game, "smelter").Progress.Equals(paused) && Stock(game, "smelter", "alloy") == Fix.Zero, "disabled operation preserves WIP/productive progress");
            long at = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); ulong pausedHash = game.Hash(); Step(game, twin);
            c.That(game.Clock.StepIndex == at && game.Hash() == pausedHash, "one clock pauses actual travel and both processors");
            game.Clock.Resume(); twin.Clock.Resume();
            Input(c, game, new ConfigureOperationCommand { Entity = Actor(game, "smelter"), Operation = Operation, Enabled = true, Target = Fix.One });
            twin = factory.Load(game.Save());
            Reach(game, twin, () => Stock(game, "smelter", "alloy") == Fix.FromInt(12)
                && Process(game, "smelter").QueueCount == 1 && Process(game, "assembler").QueueCount == 0, "shared-store backpressure and two count batches");
            var blocked = Process(game, "smelter");
            c.That(blocked.QueueCount == 1 && blocked.Progress.WholeTicks == 0 && blocked.Wip.Stock.IsEmpty
                && Stock(game, "source", "planetx.ore") == Fix.FromInt(10)
                && Stock(game, "smelter", "alloy") == Fix.FromInt(12) && Stock(game, "smelter", "slag") == Fix.FromInt(8), "shared store cannot accept third raw batch; original supply is retained, no partial third result");
            c.That(Stock(game, "assembler", "finished") == Fix.FromInt(2) && Stock(game, "assembler", "fixture") == Fix.One, "two twelve-step count assemblies reuse fixture and earn exact two items");
            twin = factory.Load(game.Save()); for (int i = 0; i < 5; i++) Step(game, twin);
            c.That(Process(game, "smelter").Progress.Equals(blocked.Progress)
                && Process(game, "smelter").Wip.Stock.IsEmpty && Stock(game, "source", "planetx.ore") == Fix.FromInt(10), "saved capacity wait retains original queue and upstream ore");
            Input(c, game, new HaulResourcesCommand { Source = Actor(game, "smelter"), SourcePort = new Code("space.product"),
                Destination = Actor(game, "receiver"), Worker = Actor(game, "carrier"), Resource = Resource(game, "alloy"), Ceiling = Fix.FromInt(10), Priority = 80 });
            twin = factory.Load(game.Save());
            Reach(game, twin, () => Process(game, "smelter").QueueCount == 0 && Stock(game, "receiver", "alloy") == Fix.FromInt(10), "ordinary physical alloy export frees whole third conversion");
            c.That(Stock(game, "smelter", "alloy") == Fix.FromInt(8) && Stock(game, "smelter", "slag") == Fix.FromInt(12)
                && Stock(game, "source", "planetx.ore") == Fix.Zero && Process(game, "smelter").Wip.Stock.IsEmpty, "three exact mass batches, byproduct retained and one real export");
            c.That(retained.QueueCount == 3 && retained.Wip.Stock.AmountOf(Resource(game, "planetx.ore")) == Fix.FromInt(10)
                && blocked.QueueCount == 1 && blocked.Progress.WholeTicks == 0 && blocked.Wip.Stock.IsEmpty, "retained process/capacity-wait facts are detached");
            Input(c, game, Queue(game, "assembler", "space.assemble.parts")); twin = factory.Load(game.Save());
            Reach(game, twin, () => Process(game, "assembler").Phase == TransformationPhase.Working
                && Process(game, "assembler").Progress.WholeTicks > 0, "partial third assembly");
            c.That(Process(game, "assembler").Required == 12 && Process(game, "assembler").Wip.Stock.AmountOf(Resource(game, "blank")) == Fix.FromInt(3), "sixty seconds, discrete three-item WIP");
            game.Seat(0).Submit(new CancelTransformationCommand { Transformer = Actor(game, "assembler"), Capability = Operation, Index = 0 });
            twin = factory.Load(game.Save()); Step(game, twin);
            Reach(game, twin, () => Process(game, "assembler").QueueCount == 0 && Process(game, "assembler").Wip.Stock.IsEmpty, "saved return_inputs cancellation");
            c.That(Stock(game, "assembler", "finished") == Fix.FromInt(2) && Stock(game, "assembler", "blank") == Fix.FromInt(3)
                && Stock(game, "assembler", "fixture") == Fix.One, "cancel returns actual original three inputs, no output or repeated tool cost");
            Input(c, game, Queue(game, "assembler", "space.assemble.parts")); twin = factory.Load(game.Save());
            Reach(game, twin, () => Process(game, "assembler").QueueCount == 0, "reuse returned inputs");
            c.That(Stock(game, "assembler", "finished") == Fix.FromInt(3) && Stock(game, "source", "blank") == Fix.FromInt(3)
                && Stock(game, "assembler", "blank") == Fix.Zero && Process(game, "assembler").Tools.Held.IsEmpty, "three successful count batches use exactly nine blanks and release tools");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "final whole stock/queue checkpoint");
            c.That(new ReplayVerifier().Verify(factory, replay.Finish(), limits).Success, "normal queues, operations, export and cancellation replay");
            var isolated = New(factory); for (int i = 0; i < 3; i++) { Step(game, twin); Step(isolated); }
            c.That(Process(isolated, "smelter").QueueCount == 0 && Stock(isolated, "source", "planetx.ore") == Fix.FromInt(30)
                && Stock(isolated, "source", "blank") == Fix.FromInt(12), "interleaved content pack has no shared process or stock state");
        }
    }
}
