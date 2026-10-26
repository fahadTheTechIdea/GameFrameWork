using System;
using System.Reflection;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.EmergencyPreemptionTests;

namespace Beep.SimEngine.Tests
{
    internal static class EntityBoundaryTests
    {
        internal static void Register()
        {
            TestRunner.Add("Entity boundaries: issued updates, scoped observations and first-fault cutoff", Contracts);
            TestRunner.Add("Entity boundaries: authored states, physical updates and saved continuation", Physical);
        }
        private static GameFactory Factory(string mode = "healthy", bool repeat = false)
        {
            var module = new ProbeModule("probe", new ModuleVersion(1, 0, 0), content: types =>
            {
                types.Component<EntityBoundaryProbeDef>("entity_probe", 2);
                types.Component<EntityBoundaryBrokenDef>("entity_broken", 1);
            }, compose: composer => composer.AddSystem(new EntityBoundarySystem(), 10));
            var definition = new GameDefinition("entity-boundaries", new[] { module });
            var root = Json.Parse(CompositionProbe.Definitions); root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            var authored = Json.Parse(FrameworkProofContent.Read("entity-boundaries.json"));
            authored.Get("entities")[0].Get("components").Get("entity_probe")[0].Set("mode", JsonValue.Of(mode));
            if (repeat) authored.Get("entities")[1].Get("components").Get("entity_probe")[0].Set("mode", JsonValue.Of("repeat-prior"));
            var files = new PreloadedFileSource().Add("manifest.json", "{\"schema\":1,\"modules\":[{\"id\":\"probe\",\"version\":\"1.0.0\",\"documents\":[\"probe.json\",\"entity-boundaries.json\"]}]}")
                .Add("probe.json", root.ToString()).Add("entity-boundaries.json", authored.ToString());
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory, bool physical = false)
        {
            var request = new NewGameRequest { Scenario = physical ? "points" : "probe", Seed = 607 };
            if (physical) request.Mods.Add("proof.shipments");
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static Entity Actor(SimGame game, int company = 0)
        {
            foreach (Entity entity in game.World.Entities.All)
                if (entity.Def.Id == "actor" && entity.Owner == new PlayerId(company)) return entity;
            throw new InvalidOperationException("no actual boundary actor");
        }
        private static EntityBoundaryProbe Probe(Entity entity, string id = "a") => entity.Get<EntityBoundaryProbe>(new Code(id));
        private static void Cutoff(Check c, SimGame game, EntityBoundarySystem system, string phase, string label)
        {
            Entity entity = Actor(game); var first = Probe(entity); var second = Probe(entity, "b"); var last = Probe(entity, "z");
            var other = Probe(Actor(game, 1)); var retained = game.Snapshot; Code state = entity.State;
            long age = game.Clock.StepIndex; WorldFault observed = null; game.Updated += _ => observed = game.Fault;
            game.Clock.TicksPerFrame = 8;
            c.Equal(0, game.Update(), "partial state update completes no time: " + label);
            c.That(game.Fault != null && ReferenceEquals(game.Fault, observed), "original immutable fault is published: " + label);
            c.Equal(phase, game.Fault.Phase, "most specific original boundary survives outer catches: " + label);
            c.Equal(age, game.Clock.StepIndex, "state failure cannot advance another game clock: " + label);
            c.Equal(age, game.Fault.Tick, "fault coordinate derives from sole clock: " + label);
            c.Equal(1, first.Reads, "offending callback ran only once: " + label);
            c.Equal(0, second.Reads, "later same-entity callback does not run: " + label);
            c.Equal(0, last.Reads, "last same-entity callback does not run: " + label);
            c.Equal(0, other.Reads, "next entity never runs after state failure: " + label);
            c.Equal(1, system.Updates, "preceding game system runs once: " + label);
            c.Equal(0, system.Completions, "post-entity completion phases do not run: " + label);
            c.That(!game.World.Stepping && !game.World.UpdatingEntities, "transient lifecycle permissions close on failure: " + label);
            c.Equal(GameState.Faulted, game.Snapshot.State, "faulted snapshot is engine-published: " + label);
            c.Equal(GameState.Running, retained.State, "retained prior snapshot remains immutable: " + label);
            c.Equal(0L, game.Clock.PendingTicks, "unapplied host backlog is cancelled after fault: " + label);
            c.Throws<InvalidOperationException>(() => game.Save(), "partial entity update cannot checkpoint: " + label);
            c.Throws<InvalidOperationException>(() => game.Hash(), "partial entity update cannot claim a healthy hash: " + label);
            c.Equal(0, game.Update(), "failed entity callbacks cannot retry: " + label);
            c.Equal(1, first.Reads, "terminal lifecycle never resumes callback: " + label);
            if (label != "state" && label != "since") c.Equal(state, entity.State, "engine never commits selected state after fault: " + label);
        }
        private static void Contracts(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "entity boundaries have explicit current identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "old save semantics do not translate");
            c.Equal(0, typeof(EntityUpdateContext).GetConstructors().Length, "modules cannot forge engine-issued contexts");
            foreach (string mode in new[] { "throw", "recursive", "swallow-recursive", "swallow-step", "swallow-initialize", "wrap-initialize", "cross-entity" })
            {
                var factory = Factory(mode); var game = New(factory); var system = game.World.System<EntityBoundarySystem>();
                var isolated = New(factory); ulong untouched = isolated.Hash();
                string phase = mode == "recursive" || mode == "swallow-recursive" ? "entity.update:1" : mode == "swallow-step" ? "game.update" :
                    mode == "swallow-initialize" || mode == "wrap-initialize" ? "entity.initialize" : "entity.state:1:entity_probe[0]";
                Cutoff(c, game, system, phase, mode);
                c.Equal(untouched, isolated.Hash(), "state fault remains session-isolated: " + mode);
                if (mode == "wrap-initialize" || mode == "swallow-initialize")
                    c.Equal("InvalidOperationException: entity boundary initialization probe", game.Fault.Message, "swallowed/wrapped nested initialization keeps original diagnostic");
            }
            foreach (string mutation in new[] { "touch", "owner", "alive", "spawn", "despawn", "state", "since", "pause", "speed" })
            {
                var game = New(Factory()); var system = game.World.System<EntityBoundarySystem>(); var entity = Actor(game);
                Probe(entity).OnRead = context =>
                {
                    switch (mutation)
                    {
                        case "touch": entity.Touch(); break;
                        case "owner": entity.Owner = new PlayerId(1); break;
                        case "alive": entity.Alive = false; break;
                        case "spawn": context.World.Spawn(context.World.Content.Get<EntityDef>("later"), entity.Owner); break;
                        case "despawn": context.World.Despawn(entity); break;
                        // Internal corruption instrumentation: intentionally bypass native version signals.
                        case "state": typeof(Entity).GetField("<State>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(entity, new Code("corrupt.state")); break;
                        case "since": typeof(Entity).GetField("<StateSince>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(entity, 42L); break;
                        case "pause": ((GameClock)context.Clock).Pause(); break;
                        case "speed": ((GameClock)context.Clock).Speed = 2; break;
                    }
                };
                Cutoff(c, game, system, "entity.state:1:entity_probe[0]", mutation);
            }
            var references = New(Factory()); var referenceSystem = references.World.System<EntityBoundarySystem>();
            Probe(Actor(references)).Alternate = New(Factory()).World.Content.Get<IEntityStateDefinition>("alpha");
            Cutoff(c, references, referenceSystem, "entity.state:1:entity_probe[0]", "foreign state");
            var healthy = New(Factory()); c.Equal(1, healthy.Update(), "healthy entity update runs before retained-context checks");
            var actor = Actor(healthy); var source = Probe(actor); EntityUpdateContext captured = source.LastContext;
            ulong hash = healthy.Hash(); int reads = source.Reads;
            c.Throws<InvalidOperationException>(() => actor.Update(default), "default context grants no update authority");
            c.Throws<InvalidOperationException>(() => actor.Update(captured), "retained context cannot update outside game phase");
            c.Throws<InvalidOperationException>(() => Actor(healthy, 1).Update(captured), "one entity cannot reuse another's context");
            var foreign = New(Factory());
            c.Throws<InvalidOperationException>(() => Actor(foreign).Update(captured), "same numeric entity identity in another session grants no authority");
            c.Equal(hash, healthy.Hash(), "invalid context refusals change no gameplay state"); c.Equal(reads, source.Reads, "refused contexts never call state sources");
            c.That(healthy.Fault == null && foreign.Fault == null, "out-of-phase refusal is not an invented partial-update fault");
            source.OnRead = _ => actor.Update(captured);
            c.Equal(0, healthy.Update(), "stale prior-update context cannot reenter next active update");
            c.Equal(1L, healthy.Clock.StepIndex, "stale-context failure leaves current sole-clock coordinate intact");
            c.Equal(reads + 1, source.Reads, "stale reentry never invokes nested state callbacks");
            var repeat = New(Factory(repeat: true)); var repeatSystem = repeat.World.System<EntityBoundarySystem>();
            c.Equal(0, repeat.Update(), "later entity cannot update already-completed entity again in same game update");
            c.Equal("entity.update:1", repeat.Fault.Phase, "repeat permission failure records original entity identity");
            c.Equal(1, Probe(Actor(repeat)).Reads, "repeated same-update call never duplicates earlier callback");
            c.Equal(0L, repeat.Clock.StepIndex, "repeat failure never completes game time");
            c.Equal(0, repeatSystem.Completions, "repeat cutoff precedes all completion systems");
        }
        private static void Physical(Check c)
        {
            var factory = Factory(); var game = New(factory); var system = game.World.System<EntityBoundarySystem>();
            for (int i = 0; i < 7; i++) c.Equal(1, game.Update(), "healthy authored source update completes once");
            var entity = Actor(game); var source = Probe(entity);
            c.Equal(new Code("phase.alpha"), entity.State, "stable content-ID tie-break wins over callback order");
            c.Equal(0L, entity.StateSince, "unchanged selected state retains its actual original game timestamp");
            foreach (string id in new[] { "a", "b", "z" }) c.Equal(7, Probe(entity, id).Reads, "each source is visited once per engine update");
            c.Equal(7, system.Completions, "healthy completion phase runs after entities");
            ulong hash = game.Hash(); var snapshot = game.Snapshot; int reads = source.Reads;
            for (int i = 0; i < 5; i++) { _ = game.Snapshot; _ = game.Seat(0).Snapshot; }
            c.Equal(reads, source.Reads, "snapshot observation never drives entity lifecycle"); c.Equal(hash, game.Hash(), "snapshot observation remains pure");
            var twin = factory.Load(game.Save()); c.Equal(hash, twin.Hash(), "transient permissions are not duplicate saved chronology");
            game.Clock.Pause(); twin.Clock.Pause(); c.Equal(0, game.Update(), "pause advances no entity updates"); twin.Update();
            c.Equal(reads, source.Reads, "pause cannot visit state callbacks independently"); c.Equal(hash, game.Hash(), "pause preserves authoritative state");
            game.Clock.Resume(); twin.Clock.Resume();
            for (int i = 0; i < 12; i++) { c.Equal(1, game.Update(), "healthy original continues"); c.Equal(1, twin.Update(), "restored entity permissions initialize normally"); }
            c.Equal(game.Hash(), twin.Hash(), "public-only state providers remain deterministic after restore");
            c.Equal(new Code("phase.alpha"), snapshot.Entities[0].State, "retained snapshot remains detached");
            foreach (bool native in new[] { false, true }) foreach (bool policy in new[] { false, true }) foreach (bool worker in new[] { false, true })
            {
                var physicalFactory = IncidentPreparednessTests.Fixture(native, policy, worker); var physical = New(physicalFactory, true);
                var isolated = New(physicalFactory, true); ulong untouched = isolated.Hash();
                Order order = Raise(c, physical); c.That(order.State != OrderState.Done, "state selection cannot instantly perform response work");
                var loaded = physicalFactory.Load(physical.Save()); c.Equal(physical.Hash(), loaded.Hash(), "active physical jobs and entity state restore");
                long time = physical.Clock.StepIndex; hash = physical.Hash();
                physical.Clock.Pause(); loaded.Clock.Pause(); physical.Update(); loaded.Update();
                c.Equal(time, physical.Clock.StepIndex, "paused physical state derives from sole game clock"); c.Equal(hash, physical.Hash(), "paused state resolution never mutates progress");
                physical.Clock.Resume(); loaded.Clock.Resume();
                for (int i = 0; i < 300 && order.State != OrderState.Done; i++)
                {
                    physical.Update(); loaded.Update();
                    if (physical.Fault != null || loaded.Fault != null) throw new InvalidOperationException("physical entity boundary fault: " + physical.Fault + "/" + loaded.Fault);
                }
                if (order.State != OrderState.Done) throw new InvalidOperationException("entity physical timeout at game update " + physical.Clock.StepIndex + ", order=" + order.State + ", blocker=" + order.Blocker);
                c.Equal(Fix.FromInt(4), Find(physical, "destination").Get<IInletPort>().Amount(Supply(physical)), "ordinary physical engine consumes actual response inputs once");
                c.Equal(physical.Hash(), loaded.Hash(), "native/public states and physical continuations remain identical");
                c.Equal(untouched, isolated.Hash(), "another session cannot share transient entity update permission");
                c.That(ReferenceEquals(physical.Clock, physical.World.Clock), "one clock feeds actual game and entity loop");
                hash = physical.Hash(); var facts = physical.Seat(0).View.Feature<WorkView>();
                c.Equal(Code.None, facts.TryReadPolicy(Find(physical, "machine").Id, out _), "private policy view uses migrated shared observation owner");
                c.Equal(hash, physical.Hash(), "migrated shared observation owner keeps healthy policy reads pure");
                c.Equal(physical.Hash(), physicalFactory.Load(physical.Save()).Hash(), "completed physical state checkpoints continue through current lifecycle");
            }
        }
    }
}
