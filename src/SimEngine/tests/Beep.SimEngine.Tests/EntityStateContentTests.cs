using System;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using static Beep.SimEngine.Tests.PlanetXProductionTests;

namespace Beep.SimEngine.Tests
{
    // Normal file composition and player inputs/views/time/saves only. State settlement stays in Entity.Update.
    internal static class EntityStateContentTests
    {
        internal const string Case = "Entity state content: independent JSON aliases settle native physical entities through one state contract";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(EntityStateContentModule module, Action<JsonValue> edit = null)
        {
            var definition = new GameDefinition("state-contract", new GameModule[] {
                new ContentAccessModule(), new InventoryModule(), new OperationsModule(), new MotionModule(),
                new WorkModule(), new LogisticsModule(), new TransformationModule(), new WorldSetupModule(), new PointModule(), module });
            var authored = Json.Parse(FrameworkProofContent.Read("entity-state-contract-world.json")); edit?.Invoke(authored);
            var files = new PreloadedFileSource().Add("manifest.json", FrameworkProofContent.Read("entity-state-contract-master.json"))
                .Add("open-access.json", FrameworkProofContent.Access).Add("work-retry.json", FrameworkProofContent.Read("work-retry.json"))
                .Add("entity-state-contract-world.json", authored.ToString());
            foreach (string name in new[] { "resources", "recipes", "work", "logistics" })
                files.Add("planet-x-" + name + ".json", FrameworkProofContent.Read("planet-x-" + name + ".json"));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static Code EntityState(SimGame game, string name)
            => game.Seat(0).Snapshot.Entities.Single(e => e.Id == Actor(game, name)).State;
        private static void Invalid(Check c, Action<JsonValue> edit, string label)
        {
            var module = new EntityStateContentModule();
            c.Throws<ContentException>(() => New(Factory(module, edit)), label);
            c.Equal(0, module.Compositions, label + " refuses before Compose/Initialize, unused included");
        }
        private static void Run(Check c)
        {
            Invalid(c, x => x.Get("independent_entity_states")[4].Set("fault", JsonValue.Of("empty-state")), "unused invalid published state code");
            Invalid(c, x => x.Get("independent_entity_states")[4].Set("fault", JsonValue.Of("throw-priority")), "unused throwing priority metadata");
            Invalid(c, x => x.Get("independent_entity_states")[4].Set("state", JsonValue.Of("mod.idle")), "duplicate published state code across aliases");
            Invalid(c, x => x.Get("independent_entity_states")[1].Set("default", JsonValue.Of(true)), "multiple default state providers");
            Invalid(c, x => x.Get("independent_entity_states")[0].Set("default", JsonValue.Of(false)), "missing explicit default state provider");
            Invalid(c, x => x.Get("entities")[1].Get("components").Get("transformer").Get("states").Set("working", JsonValue.Of("missing.state")), "missing native processor state reference");
            Invalid(c, x => x.Get("independent_entity_states")[1].Set("extra", JsonValue.Of("unknown")), "shared reader keeps strict JSON keys");
            var module = new EntityStateContentModule(); var factory = Factory(module); var game = New(factory);
            c.That(module.Compositions == 1 && game.Modules.Count == 10, "explicit module composes independent state definitions once");
            c.Equal(new Code("mod.idle"), EntityState(game, "smelter"), "initial native entity state resolves independent default alias");
            c.Equal(new Code("mod.idle"), EntityState(game, "carrier"), "native worker initial state is the same owned catalog default");
            var initial = game.Seat(0).Snapshot;
            var limits = new ReplayLimits(4 * 1024 * 1024, 4000); var record = game.RecordReplay(limits);
            var queue = Queue(game, "smelter", "space.smelt.planetx");
            c.Equal(Code.None, game.Seat(0).Preview(queue), "normal player production preview");
            game.Seat(0).Submit(queue); var twin = factory.Load(game.Save());
            bool moving = false, loading = false, working = false, stopped = false;
            for (int i = 0; i < 256 && Process(game, "smelter").QueueCount == 0 && Stock(game, "smelter", "alloy") == Fix.Zero; i++)
                Step(game, twin);
            for (int i = 0; i < 256 && Process(game, "smelter").QueueCount != 0; i++)
            {
                Step(game, twin);
                moving |= EntityState(game, "carrier") == new Code("mod.moving");
                loading |= EntityState(game, "carrier") == new Code("mod.loading");
                working |= EntityState(game, "smelter") == new Code("mod.working");
                stopped |= EntityState(game, "smelter") == new Code("mod.stopped");
            }
            if (Process(game, "smelter").QueueCount != 0) throw new InvalidOperationException("state-contract physical stage exceeded256Updates");
            c.That(moving && loading && working && stopped, "native hauling and manufacturing settle independently authored moving/loading/working/stopped states");
            c.That(Stock(game, "smelter", "alloy") == Fix.FromInt(6) && Stock(game, "smelter", "slag") == Fix.FromInt(4), "actual native whole mass conversion remains unchanged");
            var operation = new Code("space.operation");
            c.That(TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = Actor(game, "smelter"), Operation = operation, Enabled = false, Target = Fix.One }).Accepted, "normal disabled operation");
            twin = factory.Load(game.Save()); Step(game, twin);
            c.Equal(new Code("mod.stopped"), EntityState(game, "smelter"), "native operation state binding resolves the independent state interface");
            c.That(initial.Entities.Single(e => e.Id == Actor(game, "smelter")).State == new Code("mod.idle"), "retained initial snapshot never aliases entity state transitions");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "state code/since and original physical ownership restore without new state codec");
            c.That(new ReplayVerifier().Verify(factory, record.Finish(), limits).Success, "independent state content and native physical transitions replay");

            var brokenModule = new EntityStateContentModule { PauseOnStoppedMetadata = true };
            var broken = New(Factory(brokenModule)); var retained = broken.Seat(0).Snapshot;
            broken.Seat(0).Submit(Queue(broken, "smelter", "space.smelt.planetx")); broken.Update();
            c.That(broken.Fault != null && broken.State == GameState.Faulted, "state metadata callback cannot mutate the actual clock after source observation");
            c.That(broken.Seat(0).Snapshot.Entities.Single(e => e.Id == Actor(broken, "smelter")).State
                == retained.Entities.Single(e => e.Id == Actor(broken, "smelter")).State, "failed metadata publishes no healthy state transition prefix");
            c.Throws<InvalidOperationException>(() => broken.Save(), "faulted state metadata cannot checkpoint poisoned state");
            var isolated = New(factory); Step(isolated);
            c.That(isolated.Fault == null, "separately composed state content remains isolated from metadata fault");
        }
    }
}
