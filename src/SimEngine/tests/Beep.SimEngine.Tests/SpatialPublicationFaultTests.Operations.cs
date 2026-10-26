using System;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Tests
{
    internal static partial class SpatialPublicationFaultTests
    {
        private const string OperationCase = "Spatial operation admission: unfinished quantity and ownership publication cannot checkpoint or advance time";

        // Actual low-level publication callbacks; no host-owned simulation rule or material model.
        private static void OperationAdmission(Check c)
        {
            foreach (string representation in new[] { "native", "independent", "points" })
            foreach (string mutation in representation == "native" ? new[] { "withdraw", "owner", "batch", "fill" }
                : representation == "independent" ? new[] { "withdraw", "owner", "batch" } : new[] { "withdraw", "owner" })
            foreach (bool restored in new[] { false, true })
            foreach (bool handled in new[] { true, false })
            foreach (string entry in new[] { "hash", "save", "breakdown", "sync", "load", "world", "update", "elapsed", "batch" })
            {
                var probe = OwnershipProbe(representation, true); var game = probe.Game;
                if (restored) {
                    game = probe.Factory.Load(game.Save());
                    var cell = game.World.Content.Get<Beep.SimEngine.Standard.ISpatialResourceCellDefinition>(representation == "points" ? "point.extract.cell" : "sensor.cell");
                    var endpoint = game.World.Services.Get<Beep.SimEngine.Standard.ISpatialResourceEndpoints>().ResolveCell(cell);
                    var forward = new NotificationGuard((ISpatialResourceCellGuard)endpoint);
                    var cells = (System.Collections.Generic.SortedDictionary<long, ISpatialResourceCellGuard>)Field(Field(forward.Layer, "_resourceCells"), "_cells");
                    cells[representation == "points" ? Fix.FromInt(10).Raw : ((long)20 << 32) | 14] = forward;
                    probe = (probe.Factory, game, probe.Idle, (ISpatialLayerProvider)game.World.Space, forward, probe.LayerId);
                }
                var guard = probe.Guard; byte[] saved = game.Save(); ulong isolated = probe.Idle.Hash();
                Fix before = guard.Quantity; int version = guard.Version;
                long time = game.Clock.StepIndex, pending = game.Clock.PendingTicks; Fix alpha = game.Clock.Alpha;
                int calls = 0, updated = 0; bool escaped = false; Exception admission = null, failure = null;
                game.Updated += _ => updated++;
                string context = representation + "/" + mutation + "/restored=" + restored + "/handled=" + handled + "/" + entry;
                Action callback = () => {
                    calls++;
                    c.Equal(mutation == "owner" ? before : before - Fix.One, guard.Quantity, "callback observes the actual published quantity prefix: " + context);
                    c.Equal(new PlayerId(mutation == "owner" ? 1 : 0), guard.Company, "callback observes the actual published owner: " + context);
                    var publication = game.World.Services.Get<ISpatialMutation>();
                    c.That(publication.OperationInProgress, "the declared original publication owner reports its actual active scope: " + context);
                    publication.Commit(() => { });
                    c.That(publication.OperationInProgress, "a completed nested publication cannot release its original enclosing scope: " + context);
                    try {
                        if (entry == "hash") game.Hash();
                        else if (entry == "save") game.Save();
                        else if (entry == "breakdown") game.HashBreakdown();
                        else if (entry == "sync") game.World.Sync(new StateHasher());
                        else if (entry == "load") {
                            var reader = new StateReader(saved, 0, saved.Length); int position = reader.Position;
                            try { game.World.Sync(reader); }
                            finally { c.Equal(position, reader.Position, "unfinished publication refuses before reading a payload: " + context); }
                        }
                        else if (entry == "world") game.World.Step();
                        else if (entry == "update") game.Update();
                        else if (entry == "elapsed") game.Update(1500000L);
                        else game.RunTicks(1);
                        escaped = true;
                    }
                    catch (Exception error) { admission = error; if (!handled) throw; }
                };
                if (mutation == "owner") guard.OwnerNotification = callback; else guard.Notification = callback;
                try { Publish(game, guard.Layer, probe.LayerId, representation, mutation); }
                catch (Exception error) { failure = error; }
                guard.Notification = () => { }; guard.OwnerNotification = null;
                c.That(calls == 1 && !escaped && admission is InvalidOperationException && admission.Message.Contains("operation"),
                    "unfinished spatial publication rejects checkpoint/time admission: " + context);
                if (calls != 1 || escaped || !(admission is InvalidOperationException) || !admission.Message.Contains("operation"))
                    throw new InvalidOperationException("unfinished spatial publication escaped: " + context + ", escaped=" + escaped + ", error=" + admission?.Message);
                c.That(game.Clock.StepIndex == time && game.Clock.PendingTicks == pending && game.Clock.Alpha == alpha && updated == 0,
                    "denied publication reentry changes no sole-clock budget or update event: " + context);
                c.Equal(version + 1, guard.Version, "actual endpoint revision commits exactly once: " + context);
                if (handled) {
                    c.That(failure == null && game.Fault == null, "handled pure refusal permits original publication: " + context);
                    c.That(!game.World.Services.Get<ISpatialMutation>().OperationInProgress, "completed publication releases its own transient scope: " + context);
                    var twin = probe.Factory.Load(saved); var layers = (ISpatialLayerProvider)twin.World.Space;
                    ISpatialLayer layer = null; foreach (var candidate in layers.SpatialLayers) if (candidate.Id == probe.LayerId) layer = candidate;
                    Publish(twin, layer, probe.LayerId, representation, mutation);
                    c.Equal(game.Hash(), twin.Hash(), "handled reentry changes no layer values, revisions or private cursors: " + context);
                    c.Equal(game.Hash(), probe.Factory.Load(game.Save()).Hash(), "completed original publication admits a real durable checkpoint: " + context);
                    game.Update(); twin.Update();
                    c.That(game.Fault == null && twin.Fault == null, "normal game update resumes after completed publication: " + context);
                    c.Equal(game.Hash(), twin.Hash(), "saved original spatial owners continue identically: " + context);
                }
                else {
                    c.That(ReferenceEquals(admission, failure) && game.Fault != null && game.Fault.Phase == "spatial.mutation", "propagated notification preserves original first fault: " + context);
                    c.Equal(mutation == "owner" ? before : before - Fix.One, guard.Quantity, "failed publication retains its truthful material prefix: " + context);
                    c.Throws<InvalidOperationException>(() => game.Save(), "failed publication cannot certify resumable state: " + context);
                }
                c.Equal(isolated, probe.Idle.Hash(), "publication permission and fault remain session-local: " + context);
            }
        }

        private static void Publish(SimGame game, ISpatialLayer layer, string id, string representation, string mutation)
        {
            if (mutation == "owner") { ((ISpatialLayerProvider)game.World.Space).AssignOwner(game.World, id, new PlayerId(1)); return; }
            if (representation == "points") { ((ISpatialResourceQuantity<Fix>)layer).Withdraw(Fix.FromInt(10), Fix.One); return; }
            var location = new TileCoord(14, 20); var quantity = (ISpatialResourceQuantity<TileCoord>)layer;
            if (mutation == "withdraw") { quantity.Withdraw(location, Fix.One); return; }
            Fix after = quantity.CheckWithdrawal(location, Fix.One).StoredAfter;
            if (mutation == "batch") ((ITileSampleMutationSpace)game.World.Space).ApplySamples(new[] { new TileSampleMutation(game.World.Content.Get<Beep.SimEngine.Content.ISpatialLayerDefinition>(id), location, after) });
            else ((TileMap)game.World.Space).Layer<ByteLayer>(id).Fill((x, y) => x == location.X && y == location.Y ? checked((byte)(after.Raw / Fix.One.Raw)) : (byte)0);
        }
    }
}
