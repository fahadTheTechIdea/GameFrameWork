using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    // Explicit protocol/provider fault instrumentation, not player gameplay or a second lifecycle.
    internal static partial class CreationCleanupTests
    {
        internal const string Case = "Creation cleanup: primary exception identity candidate death and first fault survive secondary failures";
        internal const string AllocationCase = "Creation allocation: local reentrancy and checkpoints refuse while initialized children retain distinct identities";
        internal const string InitializationCase = "Creation initialization: nested capability phases refuse premature checkpoints and release only after completion";
        internal const string ClockAdmissionCase = "Creation clock admission: unfinished local and nested initialization cannot advance or publish game updates";
        private static readonly PlayerId Company = new PlayerId(0);
        private static GameFactory Factory() => PointGame.Factory(
            extraModules: new GameModule[] { new EndpointProofModule() },
            extraDeclarations: "[{\"id\":\"proof.pools\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.resource-flows\",\"version\":\"1.0.0\"}],\"documents\":[\"creation-cleanup.json\"]}]",
            extraDocuments: new Dictionary<string,string> { ["creation-cleanup.json"] = FrameworkProofContent.Read("creation-cleanup.json") });
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 881 };
            request.Mods.Add("proof.pools"); var game = factory.NewGame(request); Step(game); return game;
        }
        private static void Step(SimGame game)
        { game.Update(); if (game.Fault != null) throw new InvalidOperationException("cleanup proof unexpected Update fault " + game.Fault); }
        private static Entity Create(SimGame game, bool commit)
        {
            var world = game.World; var definition = world.Content.Get<EntityDef>("cleanup.result");
            var placement = new PointPlacement(world.Space.Id, Fix.FromInt(50));
            if (!commit) return world.Spawn(definition, Company, entity => ((ISpatialPlacement)world.Space).ConfigurePlacement(entity, placement));
            Code refusal = world.PrepareCreation(definition, Company, EntityId.None, placement, out var intent);
            if (!refusal.IsNone) throw new InvalidOperationException("cleanup proof preparation " + refusal);
            return world.CommitCreation(intent);
        }
        internal static void Run(Check c)
        {
            var factory = Factory();
            foreach (bool loaded in new[] { false, true })
            foreach (bool commit in new[] { false, true })
            {
                var healthy = New(factory); if (loaded) healthy = factory.Load(healthy.Save());
                Entity positive = Create(healthy, commit); healthy.World.Settle();
                c.That(positive.Get<CreationTailProbe>().Creates == 1 && positive.Get<CreationTailProbe>().Initializations == 1,
                    "ordinary actual point creation keeps original callback order");
                var saved = factory.Load(healthy.Save());
                c.Equal(healthy.Hash(), saved.Hash(), "healthy spatial creation retains native saved identity and geometry");
                c.That(saved.World.Entities.Get(positive.Id).Get<CreationTailProbe>().Creates == 0 && saved.World.Entities.Get(positive.Id).Get<CreationTailProbe>().Initializations == 0,
                    "load restores component state without rerunning local creation/grant callbacks");
                foreach (string stage in new[] { "configure", "spawn", "create" })
                foreach (string cleanup in new[] { "none", "throw", "nested-catch", "nested-throw" })
                {
                    var game = New(factory); if (loaded) game = factory.Load(game.Save());
                    var world = game.World; var space = (PointSpace)world.Space;
                    var independent = factory.Load(game.Save()); long independentAt = independent.Tick;
                    var published = game.Seat(0).Snapshot; EntityId next = world.Entities.NextIdentity;
                    Entity candidate = null, result = null; Exception caught = null;
                    var primary = new InvalidOperationException("proof.cleanup-primary");
                    var secondary = new InvalidOperationException("proof.cleanup-secondary");
                    int primaryCalls = 0, cleanupCalls = 0; WorldFault duringCleanup = null;
                    Action fail = () => { primaryCalls++; throw primary; };
                    space.GeometryObservationFault = (phase, entity) => {
                        if (entity?.Def.Id != "cleanup.result") return;
                        if (phase == "configure") {
                            candidate = entity;
                            if (stage == "configure") fail();
                            if (stage == "create") entity.Get<CreationProbe>().BeforeCreate = fail;
                        }
                        if (phase == "spawn" && stage == "spawn") fail();
                        if (phase != "despawn") return;
                        cleanupCalls++; duringCleanup = world.Fault;
                        if (cleanup == "throw") throw secondary;
                        if (cleanup == "nested-catch") {
                            try { world.Spawn(world.Content.Get<EntityDef>("cleanup.failure"), Company); }
                            catch (InvalidOperationException) { }
                        }
                        if (cleanup == "nested-throw") world.Spawn(world.Content.Get<EntityDef>("cleanup.failure"), Company);
                    };
                    try { result = Create(game, commit); } catch (Exception error) { caught = error; }
                    string context = stage + "/" + cleanup + "/" + (commit ? "commit" : "spawn") + "/" + (loaded ? "loaded" : "new");
                    c.That(ReferenceEquals(primary, caught), "cleanup cannot replace or wrap the original exception: " + context);
                    c.That(result == null && candidate != null, "failed creation withholds its result while retaining actual candidate diagnostics: " + context);
                    c.Equal(1, primaryCalls, "one actual primary callback, no retry: " + context);
                    c.Equal(1, cleanupCalls, "original spatial cleanup is attempted exactly once: " + context);
                    c.That(duringCleanup != null && ReferenceEquals(duringCleanup, world.Fault), "primary fault exists before cleanup and remains the first immutable record: " + context);
                    c.Equal("entity.create", world.Fault?.Phase, "original creation owner retains failure phase: " + context);
                    c.Equal("InvalidOperationException: proof.cleanup-primary", world.Fault?.Message, "secondary cleanup or nested failure cannot replace original cause: " + context);
                    c.That(candidate != null && !candidate.Alive, "failed unregistered candidate is dead even when cleanup throws: " + context);
                    c.That(candidate != null && !ReferenceEquals(world.Entities.Get(candidate.Id), candidate), "failed candidate is never registered as a result: " + context);
                    c.Equal(next, world.Entities.NextIdentity, "cleanup cannot allocate a child after the primary fault: " + context);
                    c.That(candidate?.Get<CreationTailProbe>().Creates == 0 && candidate.Get<CreationTailProbe>().Initializations == 0,
                        "later local/capability callbacks never execute after the primary failure: " + context);
                    c.That(!world.Events.HasPending, "failed cleanup publishes no success prefix: " + context);
                    c.Equal(published.Tick, game.Seat(0).Snapshot.Tick, "last healthy published snapshot remains retained: " + context);
                    c.Throws<InvalidOperationException>(() => game.Save(), "failed creation/cleanup cannot checkpoint healthy state: " + context);
                    c.Throws<InvalidOperationException>(() => game.Hash(), "failed creation/cleanup cannot certify resumability: " + context);
                    Step(independent);
                    c.That(independent.Fault == null && independent.Tick > independentAt, "another restored session advances through original owners: " + context);
                }
            }
        }
        private static Entity Child(SimGame game, bool commit)
        {
            var world = game.World; var definition = world.Content.Get<EntityDef>("cleanup.child");
            if (!commit) return world.Spawn(definition, Company);
            Code refusal = world.PrepareCreation(definition, Company, EntityId.None, null, out var intent);
            if (!refusal.IsNone) throw new InvalidOperationException("child creation preparation " + refusal);
            return world.CommitCreation(intent);
        }
        internal static void InitializationCheckpoints(Check c)
        {
            var factory = Factory();
            foreach (bool loaded in new[] { false, true })
            foreach (bool parentCommit in new[] { false, true })
            foreach (bool childCommit in new[] { false, true })
            foreach (string stage in new[] { "parent-before", "child", "parent-after" })
            foreach (string operation in new[] { "save", "hash", "breakdown", "sync", "load" })
            foreach (bool handled in new[] { false, true })
            {
                string context = stage + "/" + operation + "/handled=" + handled + "/parent=" + parentCommit + "/child=" + childCommit + "/loaded=" + loaded;
                var game = New(factory); if (loaded) game = factory.Load(game.Save());
                var world = game.World; var independent = factory.Load(game.Save()); ulong isolated = independent.Hash();
                byte[] previous = independent.Save(); bool leakedLoad = false;
                Entity parent = null, child = null, result = null; EntityId next = world.Entities.NextIdentity;
                Exception checkpointError = null, creationError = null; byte[] leakedSave = null; ulong? leakedHash = null;
                IReadOnlyList<KeyValuePair<string, ulong>> leakedBreakdown = null; int attempts = 0;
                Action attempt = () => {
                    attempts++; world.Settle();
                    c.That(parent != null && parent.Get<CreationTailProbe>().Initializations == 0,
                        "actual parent remains incompletely initialized at checkpoint attempt: " + context);
                    try {
                        if (operation == "save") leakedSave = game.Save();
                        else if (operation == "hash") leakedHash = game.Hash();
                        else if (operation == "breakdown") leakedBreakdown = game.HashBreakdown();
                        else if (operation == "load") {
                            SaveGame.ReadHeader(previous, out var reader); int position = reader.Position;
                            try { world.Sync(reader); leakedLoad = true; }
                            finally { c.Equal(position, reader.Position, "unfinished initialization rejects load before reading any world payload: " + context); }
                        }
                        else { var hash = new StateHasher(); world.Sync(hash); leakedHash = hash.Value; }
                    }
                    catch (Exception error) { checkpointError = error; if (!handled) throw; }
                };
                ((PointSpace)world.Space).GeometryObservationFault = (phase, entity) => {
                    if (phase == "configure" && entity?.Def.Id == "cleanup.result") {
                        parent = entity;
                        entity.Get<CreationProbe>().BeforeInitialize = () => {
                            if (stage == "parent-before") attempt();
                            child = Child(game, childCommit);
                            if (stage == "parent-after") attempt();
                            // Real authoritative state changes after the attempted premature checkpoint.
                            parent.Touch();
                        };
                    }
                    if (phase == "spawn" && entity?.Def.Id == "cleanup.child")
                        entity.Get<CreationTailProbe>().BeforeInitialize = () => { if (stage == "child") attempt(); entity.Touch(); };
                };
                try { result = Create(game, parentCommit); }
                catch (Exception error) { creationError = error; }
                c.Equal(1, attempts, "one actual initialization checkpoint attempt: " + context);
                c.That(checkpointError is InvalidOperationException && checkpointError.Message.Contains("initialization"), "unfinished initialization refuses at original boundary: " + context);
                c.That(leakedSave == null && !leakedHash.HasValue && leakedBreakdown == null && !leakedLoad, "no incomplete initialization checkpoint escapes: " + context);
                if (handled) {
                    c.That(creationError == null && world.Fault == null && result != null, "handled pure refusal permits complete creation: " + context);
                    c.That(result?.Id == next && child != null && child.Id.Value == next.Value + 1 && world.Entities.NextIdentity.Value == next.Value + 2,
                        "valid child allocation still uses original sequential identities: " + context);
                    c.That(parent.Get<CreationTailProbe>().Initializations == 1 && child != null && child.Get<CreationTailProbe>().Initializations == 1,
                        "parent and child initialization finish exactly once: " + context);
                    world.Settle(); var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "completed nesting releases persistence guard: " + context);
                    c.Equal(parent.Version, twin.World.Entities.Get(parent.Id).Version, "final authoritative parent version saved, not earlier prefix: " + context);
                    c.That(game.HashBreakdown().Count > 0, "completed initialization permits breakdown diagnostics: " + context);
                    Step(game); Step(twin); c.Equal(game.Hash(), twin.Hash(), "saved completed lifecycle continues identically: " + context);
                }
                else {
                    c.That(result == null && creationError != null && ReferenceEquals(creationError, checkpointError), "original propagated checkpoint exception retained: " + context);
                    c.That(world.Fault != null && world.Fault.Phase == "entity.initialize", "original initialization fault owner: " + context);
                    c.Equal(0, parent.Get<CreationTailProbe>().Initializations, "later parent initializer cut off: " + context);
                    c.That(!world.Events.HasPending, "no initialization success-event prefix: " + context);
                    c.Throws<InvalidOperationException>(() => game.Save(), "propagated initialization fault cannot checkpoint: " + context);
                    c.Throws<InvalidOperationException>(() => game.Hash(), "propagated initialization fault cannot certify state: " + context);
                    var fault = world.Fault; long time = game.Clock.StepIndex; game.Update();
                    c.Equal(time, game.Clock.StepIndex, "faulted initialization cannot advance time: " + context);
                    c.That(ReferenceEquals(fault, world.Fault), "fault remains first after continuation: " + context);
                }
                c.Equal(isolated, independent.Hash(), "separate saved session unaffected: " + context);
                long otherTime = independent.Clock.StepIndex; Step(independent);
                c.That(independent.Clock.StepIndex > otherTime, "separate session actually advances: " + context);
            }
        }
        internal static void ClockAdmission(Check c)
        {
            var factory = Factory();
            foreach (bool loaded in new[] { false, true })
            foreach (bool commit in new[] { false, true })
            foreach (string stage in new[] { "configure", "spawn", "create", "parent-before", "child", "parent-after" })
            foreach (string operation in new[] { "world", "frame", "elapsed", "batch" })
            foreach (bool handled in new[] { false, true })
            {
                string context = stage + "/" + operation + "/handled=" + handled + "/commit=" + commit + "/loaded=" + loaded;
                var game = New(factory); if (loaded) game = factory.Load(game.Save());
                var world = game.World; var independent = factory.Load(game.Save()); ulong isolated = independent.Hash();
                long before = game.Clock.StepIndex, budget = game.Clock.PendingTicks; Fix alpha = game.Clock.Alpha;
                var published = game.Seat(0).Snapshot; int updated = 0, attempts = 0;
                game.Updated += _ => updated++;
                Entity parent = null, child = null, result = null; Exception admissionError = null, creationError = null;
                Action attempt = () => {
                    attempts++;
                    c.That(parent != null && parent.Get<CreationTailProbe>().Initializations == 0,
                        "actual parent remains unfinished when advancement is attempted: " + context);
                    try {
                        if (operation == "world") world.Step();
                        else if (operation == "frame") game.Update();
                        else if (operation == "elapsed") game.Update(1500000L);
                        else game.RunTicks(1);
                    }
                    catch (Exception error) { admissionError = error; if (!handled) throw; }
                };
                ((PointSpace)world.Space).GeometryObservationFault = (phase, entity) => {
                    if (entity?.Def.Id == "cleanup.result") {
                        if (phase == "configure") {
                            parent = entity;
                            if (stage == "configure") attempt();
                            if (stage == "create") entity.Get<CreationProbe>().BeforeCreate = attempt;
                            if (stage == "parent-before" || stage == "child" || stage == "parent-after")
                                entity.Get<CreationProbe>().BeforeInitialize = () => {
                                    if (stage == "parent-before") attempt();
                                    child = Child(game, false);
                                    if (stage == "parent-after") attempt();
                                };
                        }
                        if (phase == "spawn" && stage == "spawn") attempt();
                    }
                    if (phase == "spawn" && entity?.Def.Id == "cleanup.child" && stage == "child")
                        entity.Get<CreationTailProbe>().BeforeInitialize = attempt;
                };
                try { result = Create(game, commit); } catch (Exception error) { creationError = error; }
                c.Equal(1, attempts, "exactly one actual advancement attempt: " + context);
                c.That(admissionError is InvalidOperationException, "unfinished lifecycle refuses advancement at its original owner: " + context);
                c.Equal(before, game.Clock.StepIndex, "refusal cannot advance the sole game clock: " + context);
                c.Equal(budget, game.Clock.PendingTicks, "refusal consumes no whole pacing budget: " + context);
                c.Equal(alpha, game.Clock.Alpha, "refusal consumes no fractional pacing budget: " + context);
                c.Equal(0, updated, "refusal emits no completed Game Updated callback: " + context);
                c.That(ReferenceEquals(published, game.Seat(0).Snapshot), "refusal publishes no unfinished entity snapshot: " + context);
                if (handled) {
                    c.That(creationError == null && world.Fault == null && result != null && result.Alive,
                        "handled pure admission refusal permits complete original creation: " + context);
                    c.That(parent.Get<CreationTailProbe>().Creates == 1 && parent.Get<CreationTailProbe>().Initializations == 1,
                        "original parent callbacks finish exactly once: " + context);
                    if (stage == "parent-before" || stage == "child" || stage == "parent-after")
                        c.That(child != null && child.Id != parent.Id && child.Get<CreationTailProbe>().Initializations == 1,
                            "valid nested child creation still completes once: " + context);
                    world.Settle(); var twin = factory.Load(game.Save());
                    c.Equal(game.Hash(), twin.Hash(), "completed creation admits original save/load: " + context);
                    Step(game); Step(twin);
                    c.Equal(game.Hash(), twin.Hash(), "admission guard releases for normal saved continuation: " + context);
                    c.That(game.Clock.StepIndex > before && updated == 1, "completed lifecycle permits actual update and publication: " + context);
                }
                else {
                    c.That(result == null && ReferenceEquals(creationError, admissionError), "propagated refusal preserves original exception without success result: " + context);
                    string phase = stage == "configure" || stage == "spawn" || stage == "create" ? "entity.create" : "entity.initialize";
                    c.Equal(phase, world.Fault?.Phase, "original lifecycle phase owns first fault: " + context);
                    c.Equal(0, parent.Get<CreationTailProbe>().Initializations, "later parent initialization is cut off: " + context);
                    c.Throws<InvalidOperationException>(() => game.Save(), "faulted lifecycle cannot save: " + context);
                    c.Throws<InvalidOperationException>(() => game.Hash(), "faulted lifecycle cannot hash as healthy: " + context);
                }
                c.Equal(isolated, independent.Hash(), "another restored session remains unchanged: " + context);
                long otherTime = independent.Tick; Step(independent);
                c.That(independent.Tick > otherTime, "another restored session actually advances: " + context);
            }
        }
        internal static void Allocation(Check c)
        {
            var factory = Factory();
            foreach (bool loaded in new[] { false, true })
            foreach (bool parentCommit in new[] { false, true })
            foreach (bool childCommit in new[] { false, true })
            {
                var initialized = New(factory); if (loaded) initialized = factory.Load(initialized.Save());
                var initializedSpace = (PointSpace)initialized.World.Space; Entity child = null;
                EntityId first = initialized.World.Entities.NextIdentity;
                initializedSpace.GeometryObservationFault = (phase, entity) => {
                    if (phase == "configure" && entity?.Def.Id == "cleanup.result")
                        entity.Get<CreationProbe>().BeforeInitialize = () => child = Child(initialized, childCommit);
                };
                Entity parent = Create(initialized, parentCommit); initialized.World.Settle();
                c.That(parent.Id == first && child != null && child.Id.Value == first.Value + 1 && initialized.World.Entities.NextIdentity.Value == first.Value + 2,
                    "registered parent initialization may create one real child using distinct sequential IDs");
                c.That(parent.Get<CreationTailProbe>().Creates == 1 && parent.Get<CreationTailProbe>().Initializations == 1 &&
                    child.Get<CreationTailProbe>().Creates == 1 && child.Get<CreationTailProbe>().Initializations == 1,
                    "valid child and parent use the original lifecycle exactly once");
                c.Equal(initialized.Hash(), factory.Load(initialized.Save()).Hash(), "post-registration parent/child identity and original state restore normally");
                foreach (string stage in new[] { "configure", "spawn", "create" })
                foreach (bool swallow in new[] { false, true })
                {
                    var game = New(factory); if (loaded) game = factory.Load(game.Save());
                    var world = game.World; var space = (PointSpace)world.Space; first = world.Entities.NextIdentity;
                    var independent = factory.Load(game.Save()); long independentAt = independent.Tick;
                    Entity candidate = null, nested = null, result = null; int attempts = 0;
                    Exception caught = null, nestedError = null;
                    Action allocate = () => {
                        attempts++;
                        try { nested = Child(game, childCommit); }
                        catch (Exception error) { nestedError = error; if (!swallow) throw; }
                    };
                    space.GeometryObservationFault = (phase, entity) => {
                        if (entity?.Def.Id != "cleanup.result") return;
                        if (phase == "configure") {
                            candidate = entity;
                            if (stage == "configure") allocate();
                            if (stage == "create") entity.Get<CreationProbe>().BeforeCreate = allocate;
                        }
                        if (phase == "spawn" && stage == "spawn") allocate();
                    };
                    try { result = Create(game, parentCommit); } catch (Exception error) { caught = error; }
                    string context = stage + "/" + (swallow ? "caught" : "propagated") + "/parent-" + parentCommit + "/child-" + childCommit + "/loaded-" + loaded;
                    c.That(nestedError is InvalidOperationException && nestedError.Message.Contains("already in progress"), "nested allocation refuses before duplicate-ID child creation: " + context);
                    c.That(caught is InvalidOperationException && result == null && nested == null, "neither parent nor nested child advertises successful creation: " + context);
                    c.Equal(1, attempts, "one actual nested allocation attempt without fallback: " + context);
                    c.That(world.Fault != null && world.Fault.Phase == "entity.create", "original World fault owner cuts off caught or propagated nested allocation: " + context);
                    c.Equal(first, world.Entities.NextIdentity, "failed nested creation consumes no identity prefix: " + context);
                    c.That(world.Entities.Get(first) == null && candidate != null && !candidate.Alive, "no child registration or live abandoned candidate remains: " + context);
                    c.That(candidate?.Get<CreationTailProbe>().Creates == 0 && candidate.Get<CreationTailProbe>().Initializations == 0, "later parent callbacks stop before registration: " + context);
                    c.That(!world.Events.HasPending, "no resource/success event prefix from nested allocation refusal: " + context);
                    c.Throws<InvalidOperationException>(() => game.Save(), "failed nested creation cannot checkpoint a healthy continuation: " + context);
                    c.Throws<InvalidOperationException>(() => game.Hash(), "failed nested creation cannot certify a healthy hash: " + context);
                    Step(independent); c.That(independent.Fault == null && independent.Tick > independentAt, "another saved session actually advances: " + context);
                }
            }
            foreach (bool loaded in new[] { false, true })
            foreach (bool commit in new[] { false, true })
            foreach (string stage in new[] { "configure", "spawn", "create" })
            {
                var game = New(factory); if (loaded) game = factory.Load(game.Save());
                var space = (PointSpace)game.World.Space; EntityId next = game.World.Entities.NextIdentity;
                byte[] leakedSave = null; ulong? leakedHash = null; Exception saveError = null, hashError = null;
                int attempts = 0;
                Action checkpoint = () => {
                    attempts++;
                    try { leakedSave = game.Save(); } catch (Exception error) { saveError = error; }
                    try { leakedHash = game.Hash(); } catch (Exception error) { hashError = error; }
                };
                space.GeometryObservationFault = (phase, entity) => {
                    if (entity?.Def.Id != "cleanup.result") return;
                    if (phase == "configure") {
                        if (stage == "configure") checkpoint();
                        if (stage == "create") entity.Get<CreationProbe>().BeforeCreate = checkpoint;
                    }
                    if (phase == "spawn" && stage == "spawn") checkpoint();
                };
                Entity made = Create(game, commit); game.World.Settle();
                string context = stage + "/commit-" + commit + "/loaded-" + loaded;
                c.That(saveError is InvalidOperationException && leakedSave == null, "unfinished local candidate cannot publish a resumable save: " + context);
                c.That(hashError is InvalidOperationException && !leakedHash.HasValue, "unfinished local candidate cannot publish a healthy checkpoint hash: " + context);
                c.That(game.Fault == null && attempts == 1, "handled pure checkpoint refusal does not poison healthy creation: " + context);
                c.That(made.Id == next && made.Alive && made.Get<CreationTailProbe>().Creates == 1 && made.Get<CreationTailProbe>().Initializations == 1,
                    "local creation continues once after handled diagnostic refusal: " + context);
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed creation releases the transient phase for original save/load");
            }
        }
    }
}
