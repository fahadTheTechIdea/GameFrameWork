using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Tests
{
    internal static class EventFaultBoundaryTests
    {
        internal static void Run(Check c)
        {
            c.Equal(0, typeof(EventBus).GetConstructors().Length, "event dispatcher is created only by its engine world");
            for (int mode = 0; mode < 4; mode++)
            {
                var seen = new List<long>();
                var publisher = new FaultEventPublisher();
                var module = new ProbeModule("probe", new ModuleVersion(1, 0, 0),
                    content: types => types.Component<FaultInitializationDef>("fault_initialization", 1),
                    compose: composer =>
                    {
                        composer.AddSystem(publisher, 10);
                        composer.Events.Register<PulseEvent>("probe.pulse", 1);
                        composer.Events.Register<EchoEvent>("probe.echo", 1);
                        composer.Events.Subscribe<PulseEvent>("probe.first", e =>
                        {
                            seen.Add(e.Value);
                            composer.World.Events.Publish(new EchoEvent(99));
                            if (mode == 0) throw new InvalidOperationException("listener probe");
                            try { composer.World.Spawn(composer.World.Content.Get<EntityDef>("broken"), new PlayerId(0)); }
                            catch (InvalidOperationException error)
                            {
                                if (mode == 1) throw;
                                if (mode == 2) throw new InvalidOperationException("listener wrapper", error);
                            }
                        });
                        composer.Events.Subscribe<PulseEvent>("probe.later-same-payload", e => seen.Add(10 + e.Value));
                        composer.Events.Subscribe<EchoEvent>("probe.later-other-payload", e => seen.Add(e.Value));
                    });
                var definition = new GameDefinition("probe", new[] { module });
                JsonValue root = Json.Parse(CompositionProbe.Definitions);
                root.Set("entities", Json.Parse("[{\"id\":\"broken\",\"kind\":\"entity\",\"components\":{\"fault_initialization\":{}}}]"));
                var files = new PreloadedFileSource()
                    .Add("manifest.json", "{\"schema\":1,\"modules\":[{\"id\":\"probe\",\"version\":\"1.0.0\",\"documents\":[\"probe.json\"]}]}")
                    .Add("probe.json", root.ToString());
                var factory = new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
                SimGame game = factory.NewGame(new NewGameRequest { Scenario = "probe", Seed = 81 });
                GameSnapshot before = game.Snapshot;
                game.Clock.TicksPerFrame = 8;
                WorldFault published = null;
                game.Updated += _ => published = game.Fault;
                c.Equal(0, game.Update(), "faulted event phase completes no game time: " + mode);
                c.Equal("1", string.Join(",", seen), "later listeners/queued/nested events never run after failure: " + mode);
                c.Equal(1, publisher.Updates, "event publisher runs only its original update: " + mode);
                c.Equal(0, publisher.Completions, "post-event completion phase never runs: " + mode);
                c.That(game.Fault != null && ReferenceEquals(published, game.Fault), "Updated observes original immutable event fault: " + mode);
                c.Equal(mode == 0 ? "events.probe.first" : "entity.initialize", game.Fault.Phase, "most specific original owner survives dispatcher/outer catches: " + mode);
                c.Equal(mode == 0 ? "InvalidOperationException: listener probe" : "InvalidOperationException: initialization probe", game.Fault.Message, "original cause survives nested/swallowed exception: " + mode);
                c.That(!game.World.Events.HasPending, "all touched payload queues clear on terminal dispatch: " + mode);
                c.Equal(0L, game.Clock.StepIndex, "failure never advances the sole game clock: " + mode);
                c.That(game.Time.ElapsedGameTime.IsZero && game.Clock.PendingTicks == 0, "failed events earn no game time/backlog: " + mode);
                c.Equal(GameState.Faulted, game.Snapshot.State, "faulted facts are published by the engine: " + mode);
                c.Equal(GameState.Running, before.State, "previous healthy facts remain immutable: " + mode);
                c.Throws<InvalidOperationException>(() => game.Save(), "partial event update refuses save: " + mode);
                c.Throws<InvalidOperationException>(() => game.Hash(), "partial event update refuses checkpoint hash: " + mode);
                c.Throws<InvalidOperationException>(() => game.World.Events.Publish(new PulseEvent(8)), "faulted session rejects subsequent event publication: " + mode);
                c.Equal(0, game.Update(), "normal lifecycle cannot retry failed listeners: " + mode);
                c.Equal(0, game.RunTicks(8), "diagnostic batch cannot retry failed listeners: " + mode);
                c.Equal("1", string.Join(",", seen), "no deferred faulted listener resumes: " + mode);
                c.That(!game.World.Events.HasPending && publisher.Updates == 1, "repeated observations preserve cutoff: " + mode);
            }
        }
    }

    internal sealed class FaultEventPublisher : GameSystem
    {
        public override string Id => "probe.fault-events";
        public override int StateSchemaVersion => 1;
        // Instrumentation counts, not gameplay state or a second scheduler.
        internal int Updates, Completions;
        public override void Update(World world, TickInfo time)
        {
            Updates++;
            world.Events.Publish(new PulseEvent(1));
            world.Events.Publish(new EchoEvent(2));
            world.Events.Publish(new PulseEvent(3));
        }
        public override void CompleteUpdate(World world) => Completions++;
    }
}
