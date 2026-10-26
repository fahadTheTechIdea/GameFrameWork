using System;
using System.Collections.Generic;
using Beep.SimEngine;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    public static class LifecycleTests
    {
        public static void Register()
        {
            TestRunner.Add("Lifecycle: initialize, update, snapshots and events", Phases);
            TestRunner.Add("Lifecycle: no nested updates or engine drawing phase", Reentry);
            TestRunner.Add("Lifecycle: entity-owned states, arrival and restored continuation", PhysicalStates);
            TestRunner.Add("Lifecycle: mod state sources and deterministic priorities", ModStates);
            TestRunner.Add("Lifecycle: state content is mandatory and validated", StateContent);
            TestRunner.Add("Lifecycle: entity state faults latch before snapshot publication", StateFault);
        }

        private static void Phases(Check c)
        {
            SingleClockTests.Lifecycle(c);
            GameFactory factory = TestGame.Factory();
            SimGame g = factory.NewGame(new NewGameRequest { Scenario = "flat", Seed = 41 });
            IGameCycle cycle = g;
            c.That(cycle.Initialized, "the factory initializes its composed game");
            ulong start = g.Hash();
            g.Initialize();
            c.Equal(start, g.Hash(), "idempotent Initialize does not generate another world");
            c.Equal(0, g.Time.TicksRun, "initialization advances no game time");
            var order = new List<string>();
            cycle.Updated += time => order.Add("update:" + time.Tick);
            GameSnapshot before = g.Snapshot;
            c.Equal(2, before.Entities.Count, "Initialize exposes settled initial simulation facts");
            c.Equal(1, g.Update(), "Update advances a fixed tick");
            c.That(!ReferenceEquals(before, g.Snapshot) && g.Snapshot.Revision == before.Revision + 1, "Update publishes new simulation facts, not a Draw phase");
            c.Equal(1, g.Time.TicksRun, "the update event describes its fixed ticks");
            ulong settled = g.Hash();
            c.Equal("update:1", string.Join("|", order), "there is one engine Update event and no drawing event");
            c.That(g.Snapshot.Entities.Count == 2 && g.Snapshot.Tick == g.Tick, "Update publishes current simulation entities");
            g.Clock.Pause();
            c.Equal(0, g.Update(), "paused Update advances no entities or game time");
            c.Equal(0, g.Time.TicksRun, "paused event reports zero fixed ticks");
            c.Equal(settled, g.Hash(), "paused Update publishes facts without changing simulation state");
            SimGame loaded = factory.Load(g.Save());
            c.That(loaded.Initialized && loaded.Time.Tick == g.Tick, "loaded games are initialized at their restored tick");
            ulong loadedHash = loaded.Hash();
            loaded.Initialize();
            c.Equal(loadedHash, loaded.Hash(), "Initialize never overwrites loaded state");

            SimGame a = TestGame.New(8), b = TestGame.New(8);
            for (int i = 0; i < 50; i++) { a.Update(); b.RunTicks(1); }
            c.Equal(a.Hash(), b.Hash(), "normal Update and a diagnostic tick batch use the same kernel");
            SimGame p = TestGame.New(8);
            p.Clock.TicksPerSecond = 10;
            c.Equal(0, p.Update(50000), "a half fixed tick only accumulates time");
            c.Equal(2, p.Snapshot.Entities.Count, "a partial update retains initialized entity facts without any drawing phase");
            c.Equal(1, p.Update(50000), "the second half advances exactly one tick");
            c.Equal(50000L, p.Time.ElapsedMicroseconds, "the timing event records the supplied platform delta");
            c.Equal(p.Tick, p.Snapshot.Tick, "platform-timed Update publishes its fixed update result");
            SimGame created = TestGame.New();
            created.RunTicks(7);
            created.Seat(0).Submit(new PlaceBin { At = new TileCoord(20, 10) });
            created.Update();
            Entity newborn = created.World.Entities.All[created.World.Entities.Count - 1];
            c.Equal(7, newborn.StateSince, "a newly created idle entity records its actual birth tick, not tick zero");
            SimGame newbornLoaded = factory.Load(created.Save());
            c.Equal(7, newbornLoaded.World.Entities.Get(newborn.Id).StateSince, "the initial state's timestamp survives load");
        }

        private static void Reentry(Check c)
        {
            SimGame g = TestGame.New();
            int updated = 0;
            g.Updated += _ =>
            {
                updated++;
                c.Throws<InvalidOperationException>(() => g.Update(), "an update callback cannot update recursively");
                c.Throws<InvalidOperationException>(() => g.RunTicks(1), "an update callback cannot bypass phase guards with a batch");
                c.Equal(g.Tick, g.Snapshot.Tick, "Update observers already have the settled snapshot");
            };
            g.Update();
            c.That(updated == 1 && g.Tick == 1, "guards preserve exactly one engine update");
            c.That(typeof(IGameCycle).GetMethod("Draw") == null && typeof(SimGame).GetMethod("Draw") == null, "drawing is absent from the engine lifecycle");
            c.That(typeof(IGameCycle).GetEvent("Drawn") == null && typeof(SimGame).GetMethod("RunFrame") == null, "no drawing event or obsolete Update/Draw wrapper remains");
            SimGame throws = TestGame.New();
            Action<GameUpdate> callback = _ => throw new InvalidOperationException("observer failed");
            throws.Updated += callback;
            c.Throws<InvalidOperationException>(() => throws.Update(), "an observer failure is reported");
            throws.Updated -= callback;
            c.Equal(1, throws.Update(), "observer failure does not leave the lifecycle guard latched");
        }

        private static void PhysicalStates(Check c)
        {
            GameFactory factory = Colony.Factory();
            SimGame g = factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 2 });
            Entity crane = g.World.Entities.Get(Colony.Mine(g, 0, "crane")[0].Id);
            Code idle = g.World.Content.DefaultEntityState.State;
            Code moving = g.World.Content.Get<IEntityStateDefinition>("moving").State;
            c.That(crane is IEntity && crane.State == idle, "every entity starts with an authored state and update contract");
            g.Seat(0).Submit(new Beep.SimEngine.Commands.MoveVehicleCommand { Vehicle = crane.Id, To = new TileCoord(15, 5) });
            g.Update();
            c.Equal(moving, crane.State, "Game.Update invokes the entity's moving state transition");
            c.Equal(0, crane.StateSince, "a transition records its fixed tick");
            int version = crane.Version;
            c.That(g.Snapshot.Entities.Exists(crane.Id, moving), "Update publishes the entity's settled state");
            SimGame loaded = factory.Load(g.Save());
            Entity copy = loaded.World.Entities.Get(crane.Id);
            c.That(copy.State == crane.State && copy.StateSince == crane.StateSince && copy.Version == version,
                "state, transition tick and version survive an in-motion save");
            int ticks = TestKit.RunUntil(g, () => !crane.Get<Mover>().IsMoving, checked((int)TestGame.Time.Hours(2)));
            loaded.RunTicks(ticks);
            c.That(ticks > 0 && crane.Get<Mover>().Status == MoveStatus.Arrived, "arrival takes physical game time");
            c.Equal(idle, crane.State, "arrival clears the moving state instead of leaving it stale");
            c.Equal(g.Hash(), loaded.Hash(), "a loaded moving entity continues identically");
            long since = crane.StateSince;
            g.RunTicks(2);
            c.Equal(since, crane.StateSince, "unchanged states do not reset the transition timestamp");
            g.Seat(0).Submit(new Beep.SimEngine.Commands.ConstructCommand { Def = "tower", Placement = Beep.SimEngine.Maps.TilePlacement.Arguments(g.World.Space.Id, new TileCoord(12, 16)), Priority = 50 });
            g.Update();
            Entity site = null;
            foreach (Entity e in g.World.Entities.All) if (e.Get<Site>() != null) site = e;
            c.That(site != null && site.State == g.World.Content.Get<IEntityStateDefinition>("under-construction").State,
                "newly admitted sites receive their construction state in the same fixed update");
        }

        private static void ModStates(Check c)
        {
            GameFactory factory = StateGame.Factory();
            SimGame g = factory.NewGame(new NewGameRequest { Scenario = "state-test", Seed = 7 });
            Entity e = g.World.Entities.All[0];
            g.Update();
            c.Equal(new Code("mod.running-a"), e.State, "equal source priorities use ordinal state id, not component order");
            c.Equal(1, e.Get<SignalA>().Reads, "entity Update visits a mod state source exactly once per fixed tick");
            c.Equal(1, e.Get<SignalB>().Reads, "the other source is also invoked exactly once");
            var snapshot = g.Snapshot; var same = g.Snapshot;
            c.Equal(1, e.Get<SignalA>().Reads, "repeated snapshot reads never update entities");
            e.Get<SignalA>().Active = false;
            g.Update();
            c.Equal(new Code("mod.running-b"), e.State, "removing the winning source reveals the other state");
            e.Get<SignalA>().Active = true;
            e.Get<SignalA>().Critical = true;
            g.Update();
            c.Equal(new Code("mod.failed"), e.State, "an authored emergency priority overrides other capabilities");
            SimGame loaded = factory.Load(g.Save());
            g.Update(); loaded.Update();
            c.Equal(g.Hash(), loaded.Hash(), "source state and entity transitions share the save/hash path");
            e.Get<SignalA>().Active = false; e.Get<SignalB>().Active = false;
            g.Update();
            c.Equal(new Code("mod.idle"), e.State, "an inactive object uses the explicit JSON default");
            EntityId id = e.Id;
            g.Seat(0).Submit(new RemoveEntity { Target = id });
            int reads = e.Get<SignalA>().Reads;
            g.Update();
            c.That(!e.Alive && e.Get<SignalA>().Reads == reads, "removed entities are never updated again");
            SimGame reordered = StateGame.Factory(StateGame.Defs.Replace("\"signal_b\": {}, \"signal_a\": {}", "\"signal_a\": {}, \"signal_b\": {}")).NewGame(
                new NewGameRequest { Scenario = "state-test", Seed = 7 });
            reordered.Update();
            c.Equal(new Code("mod.running-a"), reordered.World.Entities.All[0].State, "reordering components does not change state arbitration");
            SimGame highDefault = StateGame.Factory(StateGame.Defs.Replace("\"state\": \"mod.idle\", \"priority\": 0", "\"state\": \"mod.idle\", \"priority\": 1000")).NewGame(
                new NewGameRequest { Scenario = "state-test", Seed = 7 });
            highDefault.Update();
            c.Equal(new Code("mod.running-a"), highDefault.World.Entities.All[0].State, "the explicit default is a fallback, never an active competitor");
        }

        private static void StateContent(Check c)
        {
            c.Throws<ContentException>(() => TestGame.Content(TestGame.Defs.Replace("\"default\": true", "\"default\": false")).Resolve(new[] { "test" }), "no fabricated default state");
            c.Throws<ContentException>(() => StateGame.Factory(StateGame.Defs.Replace("\"id\": \"a\",", "\"id\": \"a\", \"default\": true,")), "multiple defaults fail before tick one");
            c.Throws<ContentException>(() => StateGame.Factory(StateGame.Defs.Replace("mod.running-b", "mod.running-a")), "duplicate published state codes fail load");
            c.Throws<ContentException>(() => StateGame.Factory(StateGame.Defs.Replace("\"id\": \"a\"", "\"id\": \"missing-a\"")), "a missing capability state reference fails load");
        }

        private static void StateFault(Check c)
        {
            FaultBoundaryTests.Run(c);
            SimGame g = StateGame.Factory().NewGame(new NewGameRequest { Scenario = "state-test", Seed = 7 });
            g.World.Entities.All[0].Get<SignalA>().Throw = true;
            c.Equal(0, g.Update(), "a failed entity update does not claim a completed tick");
            c.That(g.Fault != null && g.Fault.Phase == "entity.state:1:signal_a[1]" && g.State == GameState.Faulted, "the entity source boundary latches its specific diagnostic fault");
            c.Equal(0, g.Update(), "a faulted game never retries partially applied gameplay");
            c.Throws<InvalidOperationException>(() => g.Save(), "partial faulted state is never written as a healthy resumable game");
            c.Throws<InvalidOperationException>(() => g.Hash(), "partial faulted state cannot be certified by a healthy checkpoint hash");
            c.Equal(GameState.Faulted, g.Snapshot.State, "Update exposes the faulted diagnostic snapshot without progressing it");
        }
    }

    public sealed class SignalADef : ComponentDef<SignalA>
    {
        public IEntityStateDefinition Normal, Critical;
        protected override void ReadCore(JsonReader r) { }
        protected override void ResolveCore(ContentResolver c) { Normal = c.Def<IEntityStateDefinition>("a"); Critical = c.Def<IEntityStateDefinition>("critical"); }
    }
    public sealed class SignalA : Component<SignalADef>, IEntityStateSource
    {
        public bool Active = true, Critical, Throw;
        public int Reads;
        public IGameClock ObservedClock;
        public long ObservedTime;
        public IEntityStateDefinition GetState(EntityUpdateContext context)
        {
            if (Throw) throw new InvalidOperationException("mod state failure");
            ObservedClock = context.Clock;
            ObservedTime = context.Clock.StepIndex;
            Reads++;
            return !Active ? null : Critical ? Def.Critical : Def.Normal;
        }
        public override void Sync(IStateSync s) { s.Bool(ref Active); s.Bool(ref Critical); s.Bool(ref Throw); }
    }
    public sealed class SignalBDef : ComponentDef<SignalB>
    {
        public IEntityStateDefinition Normal;
        protected override void ReadCore(JsonReader r) { }
        protected override void ResolveCore(ContentResolver c) => Normal = c.Def<IEntityStateDefinition>("b");
    }
    public sealed class SignalB : Component<SignalBDef>, IEntityStateSource
    {
        public bool Active = true;
        public int Reads;
        public IEntityStateDefinition GetState(EntityUpdateContext context) { Reads++; return Active ? Def.Normal : null; }
        public override void Sync(IStateSync s) { s.Bool(ref Active); }
    }
    public sealed class StateModule : GameModule
    {
        public override string Id => "states";
        public override ModuleVersion Version => new ModuleVersion(1, 0, 0);
        public override void DeclareContent(ContentTypes types)
        {
            types.Component<SignalADef>("signal_a", 2); types.Component<SignalBDef>("signal_b", 2);
            types.Component<FaultInitializationDef>("fault_initialization", 1);
        }
        public override void Compose(WorldComposer c)
        {
            c.AddWorldGen(new StateStart(), 10); c.Commands.Register("test.remove", new RemoveHandler());
            c.Commands.Register("test.fault-probe", new FaultProbeHandler());
        }
    }
    public sealed class StateStart : WorldGenStage
    {
        public override string Id => "states.start";
        public override void Generate(World world, ScenarioDef scenario) => world.Spawn(world.Content.Get<EntityDef>("machine"), new PlayerId(0));
    }
    public static class StateGame
    {
        public const string Defs = "{ " + TestGame.TimeDefinitions + " \"entity_states\": [ { \"id\": \"idle\", \"state\": \"mod.idle\", \"priority\": 0, \"default\": true }, { \"id\": \"a\", \"state\": \"mod.running-a\", \"priority\": 10 }, { \"id\": \"b\", \"state\": \"mod.running-b\", \"priority\": 10 }, { \"id\": \"critical\", \"state\": \"mod.failed\", \"priority\": 100 } ], \"entities\": [ { \"id\": \"machine\", \"kind\": \"structure\", \"components\": { \"signal_b\": {}, \"signal_a\": {} } } ], \"game_modes\":[{\"id\":\"lifecycle.resource-only\",\"name\":\"Physical diagnostic game\",\"properties\":{}}],\"scenarios\": [ { \"id\": \"state-test\",\"game_mode\":\"lifecycle.resource-only\",\"mode_overrides\":{}, \"time\": \"test.minute\", \"modules\": [\"states\"], \"seats\": 1, \"default_seats\": 1 } ] }";
        public static GameFactory Factory(string json = Defs)
        {
            var definition = new GameDefinition("states", new GameModule[] { new StateModule() });
            var files = new PreloadedFileSource().Add("manifest.json", "{ \"schema\": 1, \"modules\": [ { \"id\": \"states\", \"version\": \"1.0.0\", \"documents\": [\"states.json\"] } ] }").Add("states.json", json);
            var content = ContentSet.Load(files, definition.ContentTypes);
            content.Resolve(new[] { "states" });
            return new GameFactory(definition, content);
        }
    }

    internal static class RenderAssertions
    {
        public static bool Exists(this IReadOnlyList<EntityView> entities, EntityId id, Code state)
        {
            for (int i = 0; i < entities.Count; i++) if (entities[i].Id == id && entities[i].State == state) return true;
            return false;
        }
    }
}
