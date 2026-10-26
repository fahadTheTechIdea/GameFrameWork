using System;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Explicit provider/lifecycle instrumentation. No host-owned resource rule or transaction model.
    internal static partial class ResourceLifecycleCommitTests
    {
        internal const string AdmissionCase = "Resource mutation admission: unfinished original commits cannot checkpoint or advance the sole clock";
        private static void MutationAdmission(Check c)
        {
            var factory = Factory("none", flows: true);
            foreach (bool restored in new[] { false, true })
            foreach (bool adding in new[] { false, true })
            foreach (bool after in new[] { false, true })
            foreach (string operation in new[] { "save", "hash", "breakdown", "sync", "load", "world", "update", "elapsed", "batch" })
            foreach (bool handled in new[] { false, true })
            {
                var game = ConstructionPlanTests.NewPoints(factory); MakeRoom(game);
                if (restored) game = factory.Load(game.Save());
                var world = game.World; var isolated = factory.Load(game.Save()); ulong isolatedHash = isolated.Hash(); byte[] saved = isolated.Save();
                var endpoint = Endpoint(game); var inventory = endpoint.Get<AtomicInventory>(); var intent = Intent(game, adding);
                var resource = intent.Quantities[0].Resource; Fix original = inventory.Amount(resource);
                long time = game.Clock.StepIndex, budget = game.Clock.PendingTicks; Fix alpha = game.Clock.Alpha;
                long cursor = game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), 0).Latest;
                var published = game.Seat(0).Snapshot; int updated = 0, calls = 0;
                Exception admissionError = null, commitError = null; bool escaped = false;
                game.Updated += _ => updated++;
                string context = operation + "/after=" + after + "/adding=" + adding + "/handled=" + handled + "/restored=" + restored;
                Action attempt = () => {
                    calls++;
                    var transactions = world.Services.Get<IResourceTransaction>();
                    c.That(transactions.OperationInProgress, "original transaction declares its active execution permission: " + context);
                    // A real nested coordinator path takes and releases two more scopes.
                    // Its healthy invalid-intent refusal must not release the outer boundary.
                    var nested = transactions.CommitMovement(default);
                    c.That(!nested.Committed && world.Fault == null && transactions.OperationInProgress,
                        "nested normal refusal preserves the original outer transaction boundary: " + context);
                    c.Equal(after ? original + (adding ? Fix.One : -Fix.One) : original, inventory.Amount(resource),
                        "probe surrounds the actual original inventory mutation, not a fake prefix: " + context);
                    try {
                        if (operation == "save") { game.Save(); escaped = true; }
                        else if (operation == "hash") { game.Hash(); escaped = true; }
                        else if (operation == "breakdown") { game.HashBreakdown(); escaped = true; }
                        else if (operation == "sync") { world.Sync(new StateHasher()); escaped = true; }
                        else if (operation == "load") {
                            var reader = new StateReader(saved, 0, saved.Length); int position = reader.Position;
                            try { world.Sync(reader); escaped = true; }
                            finally { c.Equal(position, reader.Position, "unfinished transaction refuses decode before any world payload: " + context); }
                        }
                        else if (operation == "world") { world.Step(); escaped = true; }
                        else if (operation == "update") { game.Update(); escaped = true; }
                        else if (operation == "elapsed") { game.Update(1500000L); escaped = true; }
                        else { game.RunTicks(1); escaped = true; }
                    }
                    catch (Exception error) { admissionError = error; if (!handled) throw; }
                };
                if (after) inventory.AfterLifecycleCommit = attempt; else inventory.BeforeLifecycleCommit = attempt;
                ResourceInventoryResult result = default;
                try { result = world.Services.Get<IResourceTransaction>().CommitInventory(intent); }
                catch (Exception error) { commitError = error; }
                inventory.BeforeLifecycleCommit = null; inventory.AfterLifecycleCommit = null;
                c.That(calls == 1 && !escaped && admissionError is InvalidOperationException && admissionError.Message.Contains("operation"),
                    "unfinished resource operation refuses checkpoint/clock work at original entry: " + context);
                if (calls != 1 || escaped || !(admissionError is InvalidOperationException) || !admissionError.Message.Contains("operation"))
                    throw new InvalidOperationException("unfinished resource transaction escaped its execution boundary: " + context);
                c.That(game.Clock.StepIndex == time && game.Clock.PendingTicks == budget && game.Clock.Alpha == alpha && updated == 0,
                    "refusal changes no sole-clock budget or Update publication: " + context);
                c.Equal(published.Tick, game.Seat(0).Snapshot.Tick, "unfinished transaction cannot publish a newer snapshot: " + context);
                if (handled) {
                    c.That(!world.Services.Get<IResourceTransaction>().OperationInProgress, "completed outer transaction releases its own permission exactly once: " + context);
                    c.That(commitError == null && result.Committed && game.Fault == null, "handled pure refusal permits original verified commit: " + context);
                    c.Equal(original + (adding ? Fix.One : -Fix.One), inventory.Amount(resource), "completed native delta occurs once: " + context);
                    var twin = factory.Load(saved);
                    c.That(twin.World.Services.Get<IResourceTransaction>().CommitInventory(intent).Committed, "saved original owner commits without any diagnostic callback: " + context);
                    game.Update(); twin.Update();
                    c.That(game.Fault == null && twin.Fault == null, "completed resource operation releases normal advancement: " + context);
                    c.Equal(game.Hash(), twin.Hash(), "handled refusal changes no saved deterministic stock or journal result: " + context);
                    c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed operation releases original checkpoint admission: " + context);
                    var page = game.Seat(0).View.Feature<ResourceFlowView>().Read(new PlayerId(0), cursor);
                    c.That(page.Entries.Count == 1 && page.Entries[0].Item.Resource == resource && page.Entries[0].Item.Amount == Fix.One &&
                        page.Entries[0].Item.Kind == (adding ? ResourceFlowKind.Produced : ResourceFlowKind.Consumed), "one verified actual lifecycle fact, no leaked partial journal: " + context);
                }
                else {
                    c.That(ReferenceEquals(admissionError, commitError) && game.Fault != null && !result.Committed, "propagated callback refusal retains the original fault and withholds success: " + context);
                    c.Equal(after ? original + (adding ? Fix.One : -Fix.One) : original, inventory.Amount(resource), "actual terminal prefix remains truthful without rollback: " + context);
                    c.That(!world.Events.HasPending, "unverified lifecycle publishes no success fact: " + context);
                    c.Throws<InvalidOperationException>(() => game.Save(), "failed operation cannot publish a resumable save: " + context);
                    c.Throws<InvalidOperationException>(() => game.Hash(), "failed operation cannot certify healthy state: " + context);
                }
                c.Equal(isolatedHash, isolated.Hash(), "another loaded session is unaffected by execution permissions: " + context);
                isolated.Update(); c.That(isolated.Fault == null && isolated.Clock.StepIndex > time, "another session genuinely advances: " + context);
            }
        }
    }
}
