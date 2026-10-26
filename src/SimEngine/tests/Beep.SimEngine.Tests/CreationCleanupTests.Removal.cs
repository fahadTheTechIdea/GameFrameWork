using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    // Explicit engine lifecycle/protocol instrumentation, not a player acceptance driver.
    internal static partial class CreationCleanupTests
    {
        internal const string RemovalFaultCase = "Removal lifecycle: direct geometry failures and swallowed nested faults cannot return healthy";
        internal const string RemovalAdmissionCase = "Removal lifecycle admission: nested callbacks cannot checkpoint or advance the sole clock";

        internal static void RemovalFaults(Check c)
        {
            var factory = Factory();
            foreach (bool loaded in new[] { false, true })
            {
                var healthy = New(factory); Entity removable = Create(healthy, true); healthy.World.Settle();
                if (loaded) { healthy = factory.Load(healthy.Save()); removable = healthy.World.Entities.Get(removable.Id); }
                var world = healthy.World; ulong before = healthy.Hash();
                Entity stocked = null; foreach (Entity entity in world.Entities.All) if (entity.Get<IInventory>()?.Stock.Count > 0) { stocked = entity; break; }
                if (stocked == null) throw new InvalidOperationException("removal fixture requires its real finite inventory");
                c.Equal(ResourceTransactions.UndisposedStock, world.CheckDespawn(stocked), "actual finite stock remains a healthy removal refusal");
                c.Throws<InvalidOperationException>(() => world.Despawn(stocked), "normal stock refusal never bypasses the actual guard");
                c.That(world.Fault == null && healthy.Hash() == before, "normal preflight refusal preserves healthy world and stock");
                int calls = 0; ((PointSpace)world.Space).GeometryObservationFault = (phase, entity) => { if (phase == "despawn" && entity.Id == removable.Id) calls++; };
                world.Despawn(removable); world.Settle();
                c.That(calls == 1 && !removable.Alive && world.Entities.Get(removable.Id) == null, "healthy removal invokes original geometry exactly once");
                var twin = factory.Load(healthy.Save()); c.Equal(healthy.Hash(), twin.Hash(), "normal removed state saves without resurrecting the entity");
                Step(healthy); Step(twin); c.Equal(healthy.Hash(), twin.Hash(), "healthy removal permits ordinary saved continuation");

                foreach (bool swallowed in new[] { false, true })
                {
                    var game = New(factory); Entity target = Create(game, true); game.World.Settle();
                    if (loaded) { game = factory.Load(game.Save()); target = game.World.Entities.Get(target.Id); }
                    var current = game.World; var isolated = factory.Load(game.Save()); ulong isolatedHash = isolated.Hash();
                    var published = game.Seat(0).Snapshot; long time = game.Clock.StepIndex; int updates = 0, callbacks = 0;
                    // Updated also reports a zero-step fault observation; it is not productive work.
                    game.Updated += result => { if (result.TicksRun != 0) updates++; };
                    var primary = new InvalidOperationException("proof.removal-primary"); Exception caught = null, nested = null;
                    ((PointSpace)current.Space).GeometryObservationFault = (phase, entity) => {
                        if (phase != "despawn" || entity.Id != target.Id) return;
                        callbacks++;
                        if (!swallowed) throw primary;
                        try { current.Spawn(current.Content.Get<EntityDef>("cleanup.failure"), Company); }
                        catch (InvalidOperationException error) { nested = error; }
                    };
                    try { current.Despawn(target); } catch (Exception error) { caught = error; }
                    string context = "loaded=" + loaded + "/swallowed=" + swallowed;
                    c.That(caught != null && current.Fault != null, "direct removal cannot return healthy after provider failure: " + context);
                    if (caught == null || current.Fault == null) throw new InvalidOperationException("removal failure escaped its original fault owner: " + context);
                    c.Equal(1, callbacks, "failed removal is not retried or hidden: " + context);
                    c.That(!target.Alive && current.Entities.Get(target.Id) == null, "actual terminal removal prefix is retained without fake rollback: " + context);
                    c.Equal(swallowed ? "entity.initialize" : "entity.remove", current.Fault.Phase, "first actual failure retains the owning lifecycle phase: " + context);
                    c.That(swallowed ? nested != null && current.Fault.Message.Contains(nested.Message) : ReferenceEquals(primary, caught), "original exception/cause is preserved: " + context);
                    c.Throws<InvalidOperationException>(() => game.Save(), "failed removal cannot publish a resumable save: " + context);
                    c.Throws<InvalidOperationException>(() => game.Hash(), "failed removal cannot certify a healthy hash: " + context);
                    c.Throws<InvalidOperationException>(() => game.HashBreakdown(), "failed removal cannot publish healthy diagnostic hashes: " + context);
                    var fault = current.Fault; game.Update();
                    c.That(ReferenceEquals(fault, current.Fault) && game.Clock.StepIndex == time && updates == 0, "faulted removal stops clock and success publication: " + context);
                    c.Equal(published.Tick, game.Seat(0).Snapshot.Tick, "last healthy snapshot remains the published observation: " + context);
                    c.Equal(isolatedHash, isolated.Hash(), "another saved session remains unchanged: " + context);
                    Step(isolated); c.That(isolated.Clock.StepIndex > time, "another session genuinely continues: " + context);
                }
            }
        }

        internal static void RemovalAdmission(Check c)
        {
            var factory = Factory();
            foreach (bool loaded in new[] { false, true })
            foreach (string stage in new[] { "outer-before", "nested", "outer-after" })
            foreach (string operation in new[] { "save", "hash", "breakdown", "sync", "load", "world", "update", "elapsed", "batch" })
            foreach (bool handled in new[] { false, true })
            {
                var game = New(factory); Entity parent = Create(game, true), child = Create(game, false); game.World.Settle();
                if (loaded) { game = factory.Load(game.Save()); parent = game.World.Entities.Get(parent.Id); child = game.World.Entities.Get(child.Id); }
                var world = game.World; var isolated = factory.Load(game.Save()); ulong isolatedHash = isolated.Hash(); byte[] saved = isolated.Save();
                long before = game.Clock.StepIndex, budget = game.Clock.PendingTicks; Fix alpha = game.Clock.Alpha;
                var published = game.Seat(0).Snapshot; int updated = 0, attempts = 0, callbacks = 0;
                Exception admissionError = null, removalError = null; bool leaked = false; Entity created = null;
                game.Updated += _ => updated++;
                string context = stage + "/" + operation + "/handled=" + handled + "/loaded=" + loaded;
                Action attempt = () => {
                    attempts++;
                    try {
                        if (operation == "save") { game.Save(); leaked = true; }
                        else if (operation == "hash") { game.Hash(); leaked = true; }
                        else if (operation == "breakdown") { game.HashBreakdown(); leaked = true; }
                        else if (operation == "sync") { world.Sync(new StateHasher()); leaked = true; }
                        else if (operation == "load") {
                            var reader = new StateReader(saved, 0, saved.Length); int position = reader.Position;
                            try { world.Sync(reader); leaked = true; }
                            finally { c.Equal(position, reader.Position, "unfinished removal rejects load before reading any world payload: " + context); }
                        }
                        else if (operation == "world") { world.Step(); leaked = true; }
                        else if (operation == "update") { game.Update(); leaked = true; }
                        else if (operation == "elapsed") { game.Update(1500000L); leaked = true; }
                        else { game.RunTicks(1); leaked = true; }
                    }
                    catch (Exception error) { admissionError = error; if (!handled) throw; }
                };
                ((PointSpace)world.Space).GeometryObservationFault = (phase, entity) => {
                    if (phase != "despawn") return;
                    if (entity.Id == child.Id) { callbacks++; if (stage == "nested") attempt(); return; }
                    if (entity.Id != parent.Id) return;
                    callbacks++;
                    if (stage == "outer-before") attempt();
                    world.Despawn(child);
                    if (stage == "outer-after") attempt();
                    // Legitimate nested creation remains permitted; no global ban or new allocator.
                    created = Child(game, true);
                };
                try { world.Despawn(parent); } catch (Exception error) { removalError = error; }
                c.That(attempts == 1 && !leaked && admissionError is InvalidOperationException && admissionError.Message.Contains("removal"),
                    "unfinished removal refuses the actual entry before payload/clock work: " + context);
                if (attempts != 1 || leaked || !(admissionError is InvalidOperationException) || !admissionError.Message.Contains("removal"))
                    throw new InvalidOperationException("unfinished removal admission leaked or used the wrong boundary: " + context);
                c.That(game.Clock.StepIndex == before && game.Clock.PendingTicks == budget && game.Clock.Alpha == alpha && updated == 0,
                    "refused lifecycle advancement changes no sole-clock budget or success event: " + context);
                c.Equal(published.Tick, game.Seat(0).Snapshot.Tick, "incomplete removal cannot publish a newer player snapshot: " + context);
                if (handled) {
                    c.That(removalError == null && world.Fault == null && callbacks == 2 && created != null && created.Alive,
                        "handled pure refusal permits nested removals and ordinary creation to finish once: " + context);
                    c.That(!parent.Alive && !child.Alive && world.Entities.Get(parent.Id) == null && world.Entities.Get(child.Id) == null,
                        "both intended original removals finish without resurrecting either identity: " + context);
                    world.Settle(); var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "completed outer removal releases guard for original save/load: " + context);
                    Step(game); Step(twin); c.Equal(game.Hash(), twin.Hash(), "completed nested removal continues deterministically: " + context);
                    c.That(game.Clock.StepIndex > before && updated == 1, "normal update and publication resume after removal completes: " + context);
                }
                else {
                    c.That(ReferenceEquals(removalError, admissionError) && world.Fault?.Phase == "entity.remove" && created == null,
                        "propagated refusal faults original removal and cuts off later callback work: " + context);
                    c.Throws<InvalidOperationException>(() => game.Save(), "propagated removal fault cannot save: " + context);
                    c.Throws<InvalidOperationException>(() => game.Hash(), "propagated removal fault cannot hash as healthy: " + context);
                }
                c.Equal(isolatedHash, isolated.Hash(), "another session is isolated from removal callback phases: " + context);
                Step(isolated); c.That(isolated.Clock.StepIndex > before, "isolated session can really advance: " + context);
            }
        }
    }
}
