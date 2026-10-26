using System;
using System.Collections.Generic;
using System.Reflection;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Tests
{
    public static class EventOwnershipTests
    {
        private static readonly ModuleVersion V1 = new ModuleVersion(1, 0, 0);
        public static void Register()
        {
            TestRunner.Add("Event ownership: explicit declarations and early refusals", Invalid);
            TestRunner.Add("Event ownership: forward subscriptions, exact requirements and freeze", Freeze);
            TestRunner.Add("Event ownership: cross-module providers and inactive refusal", CrossModule);
            TestRunner.Add("Event ownership: external dispatch order, metadata and saved state", External);
            TestRunner.Add("Event ownership: handler failure clears every pending queue", FailureCleanup);
            TestRunner.Add("Event ownership: nested same-type publications drain in order", Nested);
            TestRunner.Add("Wide time: external event payloads and persisted listener timestamps", WidePayloads);
        }
        private static ProbeModule Module(Action<WorldComposer> compose)
            => new ProbeModule("probe", V1, compose: compose);

        private static void Invalid(Check c)
        {
            var spy = new InitializeProbe();
            foreach (string id in new[] { "", " ", "Upper", "path/event" })
                c.Throws<ArgumentException>(() => CompositionProbe.New(Module(composer =>
                {
                    composer.AddSystem(spy, 10);
                    composer.Events.Register<PulseEvent>(id, 1);
                })), "invalid stable event identity refuses: " + id);
            foreach (int version in new[] { 0, -1 })
                c.Throws<ArgumentOutOfRangeException>(() => CompositionProbe.New(Module(composer =>
                    composer.Events.Register<PulseEvent>("probe.pulse", version))), "event schema must be positive");
            c.Throws<InvalidOperationException>(() => CompositionProbe.New(Module(composer =>
            {
                composer.AddSystem(spy, 10);
                composer.Events.Register<PulseEvent>("probe.pulse", 1);
                composer.Events.Register<EchoEvent>("probe.pulse", 2);
            })), "one event ID cannot name different payloads");
            c.Throws<InvalidOperationException>(() => CompositionProbe.New(Module(composer =>
            {
                composer.AddSystem(spy, 10);
                composer.Events.Register<PulseEvent>("probe.pulse", 1);
                composer.Events.Register<PulseEvent>("probe.other", 1);
            })), "one payload cannot acquire a second identity");
            c.Throws<InvalidOperationException>(() => CompositionProbe.New(Module(composer =>
            {
                composer.AddSystem(spy, 10);
                composer.Events.Subscribe<PulseEvent>("probe.listener", _ => { });
            })), "a subscription does not invent an event declaration");
            c.Throws<InvalidOperationException>(() => CompositionProbe.New(Module(composer =>
            {
                composer.AddSystem(spy, 10);
                composer.Events.Require<PulseEvent>("probe.pulse", 1);
            })), "missing required event refuses before initialization");
            c.Throws<InvalidOperationException>(() => CompositionProbe.New(Module(composer =>
            {
                composer.AddSystem(spy, 10);
                composer.Events.Register<PulseEvent>("probe.pulse", 1);
                composer.Events.Subscribe<PulseEvent>("probe.listener", _ => { });
                composer.Events.Subscribe<EchoEvent>("probe.listener", _ => { });
            })), "subscription IDs have one owner across payload types");
            c.Throws<ArgumentNullException>(() => CompositionProbe.New(Module(composer =>
                composer.Events.Subscribe<PulseEvent>("probe.null", null))), "null handler refuses explicitly");
            c.Throws<ArgumentException>(() => CompositionProbe.New(Module(composer =>
                composer.Events.Subscribe<PulseEvent>("Invalid", _ => { }))), "invalid handler identity refuses explicitly");
            c.Equal(0, spy.Initializations, "all undeclared/colliding contracts refuse before any system Initialize");
            c.Throws<InvalidOperationException>(() => CompositionProbe.New(Module(composer =>
                composer.Events.Publish(new PulseEvent(1)))), "composition cannot publish gameplay before freeze");
        }

        private static void Freeze(Check c)
        {
            foreach (int mismatch in new[] { 0, 1, 2 })
                c.Throws<InvalidOperationException>(() => CompositionProbe.New(Module(composer =>
                {
                    composer.Events.Require<PulseEvent>("probe.pulse", 2);
                    if (mismatch == 0) composer.Events.Register<PulseEvent>("probe.other", 2);
                    if (mismatch == 1) composer.Events.Register<PulseEvent>("probe.pulse", 1);
                    if (mismatch == 2) composer.Events.Register<EchoEvent>("probe.pulse", 2);
                })), "required event ID/schema/type must match exactly");
            EventBus captured = null;
            var seen = new List<long>();
            SimGame game = CompositionProbe.New(Module(composer =>
            {
                captured = composer.Events;
                composer.Events.Require<PulseEvent>("probe.pulse", 2);
                composer.Events.Subscribe<PulseEvent>("probe.listener", e => seen.Add(e.Value));
                composer.Events.Register<PulseEvent>("probe.pulse", 2);
                composer.Events.Register<EchoEvent>("probe.echo", 1);
            }));
            c.Equal(2, game.Seat(0).View.EventRegistrations.Count, "forward declarations settle the original queue entries");
            captured.Publish(new PulseEvent(7)); captured.Dispatch();
            c.Equal(7, seen[0], "a subscription declared before its provider runs normally");
            c.Throws<InvalidOperationException>(() => captured.Register<PulseEvent>("probe.late", 3), "late contract declaration refuses");
            c.Throws<InvalidOperationException>(() => captured.Require<PulseEvent>("probe.pulse", 2), "late requirement refuses");
            c.Throws<InvalidOperationException>(() => captured.Subscribe<PulseEvent>("probe.late", _ => { }), "late subscriptions cannot alter future execution");
            c.Throws<InvalidOperationException>(() => captured.Publish(new BinPlaced()), "unknown runtime payload is not silently created or dropped");
            captured.Publish(new EchoEvent(8)); captured.Dispatch();
            c.Equal(1, seen.Count, "explicitly declared events with no listeners are valid no-ops");
            int subscribeApis = 0;
            foreach (MethodInfo method in typeof(EventBus).GetMethods())
                if (method.Name == "Subscribe")
                {
                    subscribeApis++;
                    c.Equal(2, method.GetParameters().Length, "no anonymous/legacy subscription overload remains");
                }
            c.Equal(1, subscribeApis, "one subscription authority");
        }

        private static GameFactory Factory(ProbeModule consumer = null)
        {
            var modules = new List<GameModule> { Module(_ => { }), new EventProbeModule() };
            if (consumer != null) modules.Add(consumer);
            var definition = new GameDefinition("event-proof", modules);
            string manifest = "{ \"schema\": 1, \"modules\": [{ \"id\": \"probe\", \"version\": \"1.0.0\", \"documents\": [\"probe.json\"] }, { \"id\": \"eventprobe\", \"version\": \"1.0.0\", \"requires\": [{ \"id\": \"probe\", \"version\": \"1.0.0\" }], \"documents\": [] }] }";
            if (consumer != null) manifest = manifest.Substring(0, manifest.LastIndexOf("] }", StringComparison.Ordinal))
                + ", { \"id\": \"consumer\", \"version\": \"1.0.0\", \"requires\": [{ \"id\": \"probe\", \"version\": \"1.0.0\" }], \"documents\": [] }] }";
            var files = new PreloadedFileSource().Add("manifest.json", manifest).Add("probe.json", CompositionProbe.Definitions);
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "probe", Mods = { "eventprobe" } });

        private static void CrossModule(Check c)
        {
            var seen = new List<long>();
            var consumer = new ProbeModule("consumer", V1, requires: new[] { new ModuleRequirement("probe", V1) }, compose: composer =>
            {
                composer.Events.Require<PulseEvent>("eventprobe.pulse", 2);
                composer.Events.Subscribe<PulseEvent>("consumer.pulse", e => seen.Add(e.Value));
            });
            GameFactory factory = Factory(consumer);
            SimGame game = factory.NewGame(new NewGameRequest { Scenario = "probe", Mods = { "consumer", "eventprobe" } });
            c.Equal("eventprobe", game.Observer.EventRegistrations[1].Owner, "forward consumer never acquires provider ownership");
            c.Equal("consumer", game.Observer.EventSubscriptions[0].Owner, "consumer owns its own handler, not the payload contract");
            c.Equal("eventprobe.pulse", game.Observer.EventSubscriptions[0].EventId, "forward subscription binds the final provider identity");
            game.RunTicks(1);
            c.That(seen.Count == 1 && seen[0] == 1, "cross-module consumer receives the actual engine publication once");
            c.Throws<InvalidOperationException>(() => factory.NewGame(new NewGameRequest { Scenario = "probe", Mods = { "consumer" } }), "inactive event module cannot supply a required contract implicitly");
            var spy = new InitializeProbe();
            var shadow = new ProbeModule("consumer", V1, requires: new[] { new ModuleRequirement("probe", V1) }, compose: composer =>
            {
                composer.AddSystem(spy, 10);
                composer.Events.Register<PulseEvent>("consumer.stolen", 1);
            });
            c.Throws<InvalidOperationException>(() => Factory(shadow).NewGame(new NewGameRequest { Scenario = "probe", Mods = { "consumer", "eventprobe" } }), "another module cannot shadow a payload declaration");
            c.Equal(0, spy.Initializations, "cross-module ownership collision runs no initialization");
        }

        private static void External(Check c)
        {
            GameFactory factory = Factory();
            SimGame game = New(factory);
            IReadOnlyList<EventRegistration> entries = game.Seat(0).View.EventRegistrations;
            c.Equal("eventprobe.echo", entries[0].Id, "declaration diagnostics sort by ID, not creation order");
            c.Equal("eventprobe", entries[0].Owner, "event declaration records its external module owner");
            c.Equal(3, entries[0].SchemaVersion, "event schema is explicit and exact");
            c.Equal(typeof(EchoEvent), entries[0].Contract, "metadata identifies the payload contract");
            c.Throws<NotSupportedException>(() => ((IList<EventRegistration>)entries).Clear(), "declaration metadata cannot be edited");
            foreach (PropertyInfo property in typeof(EventRegistration).GetProperties())
                c.That(property.SetMethod == null, "event registration is immutable: " + property.Name);
            var subscriptions = game.Seat(0).View.EventSubscriptions;
            c.Equal(3, subscriptions.Count, "all composed subscriptions are described");
            c.Equal("eventprobe.a-second", subscriptions[0].Id, "subscription metadata sorts independently of dispatch order");
            c.Equal("eventprobe", subscriptions[0].Owner, "subscription ownership is captured");
            c.Equal("eventprobe.pulse", subscriptions[0].EventId, "subscription names the declared event identity");
            c.Throws<NotSupportedException>(() => ((IList<EventSubscription>)subscriptions).Clear(), "subscription facts cannot change the bus");
            ulong before = game.Hash();
            game.Seat(0).View.EventRegistrations.ToString(); game.Observer.EventSubscriptions.ToString();
            c.Equal(before, game.Hash(), "event diagnostics run no gameplay");
            game.RunTicks(1);
            EventProbeState state = game.World.Services.Get<EventProbeState>();
            c.Equal("1,100,10000,-1", string.Join(",", state.Values), "listeners retain composition order; nested events append after already queued types");
            c.Throws<NotSupportedException>(() => ((IList<long>)state.Values).Clear(), "external state inspection cannot alter listener state");
            game.RunTicks(11);
            SimGame restored = factory.Load(game.Save());
            c.Equal(game.Hash(), restored.Hash(), "listener state saves through its sole service codec");
            c.Equal(3, restored.Observer.EventSubscriptions.Count, "load rebuilds declarations/subscriptions once");
            SimGame other = New(factory);
            other.RunTicks(3);
            c.Equal(48, state.Values.Count, "interleaved session events cannot reach the first session's listeners");
            game.RunTicks(20); restored.RunTicks(20);
            c.Equal(game.Hash(), restored.Hash(), "saved twin event processing remains identical");
            c.Equal(128, state.Values.Count, "load does not duplicate subscriptions");
            bool friend = false;
            foreach (var attribute in typeof(World).Assembly.GetCustomAttributes<System.Runtime.CompilerServices.InternalsVisibleToAttribute>())
                friend |= attribute.AssemblyName == typeof(EventProbeModule).Assembly.GetName().Name;
            c.That(!friend, "external event proof receives no framework friend access");
        }

        private static void WidePayloads(Check c)
        {
            const long start = 1L << 40;
            GameFactory factory = Factory(); SimGame game = New(factory);
            WideTimeTests.SetTick(game, start); game.Update();
            IReadOnlyList<long> values = game.World.Services.Get<EventProbeState>().Values;
            c.Equal(4, values.Count, "one wide publication dispatches the original deterministic listener order");
            c.Equal(start + 1, values[0], "external pulse contains full-width TickInfo timestamp");
            c.Equal((start + 1) * 100, values[1], "external listener processes a wide value without narrowing");
            c.Equal(start + 10000, values[2], "second event contains full-width time");
            c.Equal(-(start + 1), values[3], "nested signed payload preserves exact wide identity");
            SimGame restored = factory.Load(game.Save());
            c.Equal(values[0], restored.World.Services.Get<EventProbeState>().Values[0], "ordinary service codec restores wide event facts");
            c.Equal(game.Hash(), restored.Hash(), "wide listener state has one save/hash authority");
            game.RunTicks(7); restored.RunTicks(7);
            c.Equal(game.Hash(), restored.Hash(), "interleaved external-event saved twins remain identical");
        }

        private static void FailureCleanup(Check c)
        {
            EventFaultBoundaryTests.Run(c);
            var seen = new List<long>();
            SimGame game = CompositionProbe.New(Module(composer =>
            {
                composer.Events.Register<PulseEvent>("probe.pulse", 1);
                composer.Events.Register<EchoEvent>("probe.echo", 1);
                composer.Events.Subscribe<PulseEvent>("probe.pulse-listener", e =>
                {
                    throw new InvalidOperationException("injected listener failure");
                });
                composer.Events.Subscribe<EchoEvent>("probe.echo-listener", e => seen.Add(e.Value));
            }));
            EventBus bus = game.World.Events;
            // White-box dispatcher fault injection, not a host gameplay path.
            bus.Publish(new PulseEvent(1)); bus.Publish(new EchoEvent(2));
            c.Throws<InvalidOperationException>(() => bus.Dispatch(), "handler fault is surfaced rather than swallowed");
            c.Equal(0, seen.Count, "the interrupted pass delivers no later event");
            c.That(!bus.HasPending && game.Fault.Phase == "events.probe.pulse-listener", "failed dispatcher clears all queues and identifies exact subscription");
            WorldFault fault = game.Fault;
            c.Throws<InvalidOperationException>(() => bus.Publish(new EchoEvent(3)), "faulted event owner cannot earn new publications");
            c.Throws<InvalidOperationException>(() => bus.Dispatch(), "direct dispatcher access cannot restart a terminal session");
            c.Equal(0, game.Update(), "normal lifecycle also refuses a terminal dispatcher failure");
            c.That(ReferenceEquals(fault, game.Fault) && !bus.HasPending, "original fault and empty queues remain stable");
        }

        private static void Nested(Check c)
        {
            var seen = new List<long>();
            SimGame game = CompositionProbe.New(Module(composer =>
            {
                composer.Events.Register<PulseEvent>("probe.pulse", 1);
                composer.Events.Subscribe<PulseEvent>("probe.listener", e =>
                {
                    seen.Add(e.Value);
                    if (e.Value < 3) composer.World.Events.Publish(new PulseEvent(e.Value + 1));
                });
            }));
            game.World.Events.Publish(new PulseEvent(1)); game.World.Events.Dispatch();
            c.Equal("1,2,3", string.Join(",", seen), "same-type nested events drain exactly once in global order");
            game.World.Events.Publish(new PulseEvent(9)); game.World.Events.Dispatch();
            c.Equal("1,2,3,9", string.Join(",", seen), "reused queue starts empty on its next pass");

            GameFactory factory = Factory();
            SimGame physical = New(factory), isolated = New(factory);
            ulong untouched = isolated.Hash();
            c.Equal(1, physical.Update(), "normal healthy event phase completes exactly one game update");
            c.Equal("1,100,10000,-1", string.Join(",", physical.World.Services.Get<EventProbeState>().Values),
                "external handlers retain subscription/global/nested order through the bound bus");
            c.That(physical.Fault == null && !physical.World.Events.HasPending, "healthy dispatch drains every queue without fault");
            SimGame restored = factory.Load(physical.Save());
            c.That(!ReferenceEquals(physical.World.Events, restored.World.Events), "saved session owns its own single dispatcher");
            c.Equal(physical.Hash(), restored.Hash(), "bound dispatcher introduces no duplicate persistent state");
            for (int i = 0; i < 5; i++)
            {
                physical.Update(); restored.Update();
                c.Equal(physical.Hash(), restored.Hash(), "saved healthy listeners continue identically: " + i);
                c.Equal(string.Join(",", physical.World.Services.Get<EventProbeState>().Values),
                    string.Join(",", restored.World.Services.Get<EventProbeState>().Values), "ordinary listener codec retains exact publication order: " + i);
                c.That(!physical.World.Events.HasPending && !restored.World.Events.HasPending, "both healthy dispatchers settle each update: " + i);
            }
            ulong paused = physical.Hash(); physical.Clock.Pause();
            c.Equal(0, physical.Update(), "paused one clock invokes no event-producing systems");
            c.Equal(paused, physical.Hash(), "paused events earn no listener state or time");
            c.Equal(6L, physical.Clock.StepIndex, "healthy event timing derives only from actual completed updates");
            c.Equal(untouched, isolated.Hash(), "interleaved saved dispatch never changes another session");
            c.That(isolated.Clock.StepIndex == 0 && isolated.Fault == null && !isolated.World.Events.HasPending,
                "unrelated session retains its own healthy clock and empty dispatcher");
        }
    }
}
