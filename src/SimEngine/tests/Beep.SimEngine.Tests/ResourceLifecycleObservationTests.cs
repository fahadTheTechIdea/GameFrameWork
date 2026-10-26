using System;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    // Kernel contract proof: direct transaction calls exercise its own boundary, not host gameplay.
    internal static class ResourceLifecycleObservationTests
    {
        internal const string Case = "Resource lifecycle observation: direct preflights protect original state and first fault";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(string read, string fault)
            => ConnectedResourcePortTests.Factory(false, false, true, new ConnectedResourcePortModule(),
                definitions: FrameworkProofContent.Read("atomic-inventory.json"),
                edit: data => {
                    var parts = data.Get("entities")[1].Get("components").Get("atomic_inventory");
                    parts.Set("lifecycle_read", JsonValue.Of(read)); parts.Set("lifecycle_fault", JsonValue.Of(fault));
                }, editWorld: root => root.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("atomic.factory-start")));
        private static Entity Endpoint(SimGame game)
            => game.World.Entities.All.Single(e => e.Alive && e.Def.Id == "atomic.factory");
        private static ResourceInventoryIntent Intent(SimGame game, string read)
        {
            Entity entity = Endpoint(game);
            return new ResourceInventoryIntent(new ReservationOwner("proof.lifecycle-observation", entity.Id.Value),
                entity.Owner, entity.Id, new Bill(new[] { new ResourceAmount(game.World.Content.ResourceId("supply"), Fix.One) }),
                read == "incoming" ? ResourceFlowKind.Produced : ResourceFlowKind.Consumed, new Code("proof.lifecycle-observation"));
        }
        private static void Negative(Check c, string read, string mode, bool commit)
        {
            var factory = Factory(read, mode); var game = ConstructionPlanTests.NewPoints(factory);
            var other = ConstructionPlanTests.NewPoints(factory); ulong isolated = other.Hash();
            Entity endpoint = Endpoint(game); IInventory inventory = endpoint.Get<IInventory>();
            var probe = endpoint.Get<AtomicInventory>(); var intent = Intent(game, read);
            IResourceTransaction transactions = game.World.Services.Get<IResourceTransaction>();
            int version = endpoint.Version; Fix amount = inventory.Amount(intent.Quantities[0].Resource);
            long time = game.Clock.StepIndex; game.Clock.Pause();
            Exception thrown = null;
            try { if (commit) transactions.CommitInventory(intent); else transactions.EvaluateInventory(intent); }
            catch (Exception error) { thrown = error; }
            string label = read + "/" + mode + "/" + (commit ? "commit" : "preview");
            c.That(thrown is InvalidOperationException, "provider violation returns no healthy lifecycle result " + label);
            c.That(game.Fault != null, "direct lifecycle owner latches first fault " + label);
            if (mode == "throw") c.That(thrown?.Message == "proof.original-lifecycle-read-error", "original callback exception preserved " + label);
            c.Equal(version, endpoint.Version, "probe does not touch entity version " + label);
            c.Equal(amount, inventory.Amount(intent.Quantities[0].Resource), "no stock commit prefix " + label);
            c.That(!game.World.Events.HasPending, "no success event prefix " + label);
            var first = game.Fault; int calls = probe.LifecycleCalls;
            c.Throws<InvalidOperationException>(() => transactions.EvaluateInventory(default), "fault precedes even invalid preview " + label);
            c.Throws<InvalidOperationException>(() => transactions.CommitInventory(default), "fault precedes even invalid commit " + label);
            c.Equal(calls, probe.LifecycleCalls, "later calls do not retry provider " + label);
            c.That(ReferenceEquals(first, game.Fault), "first fault remains authoritative " + label);
            c.Throws<InvalidOperationException>(() => game.Save(), "fault cannot checkpoint " + label);
            c.Throws<InvalidOperationException>(() => game.Hash(), "fault cannot claim healthy identity " + label);
            game.Update(); c.Equal(time, game.Clock.StepIndex, "faulted Update earns no game time " + label);
            c.Equal(isolated, other.Hash(), "other session unaffected " + label);
        }
        private static void Healthy(Check c)
        {
            var factory = Factory("none", "none"); var game = ConstructionPlanTests.NewPoints(factory);
            var intent = Intent(game, "available"); var transactions = game.World.Services.Get<IResourceTransaction>();
            game.Clock.Pause(); ulong before = game.Hash();
            c.That(transactions.EvaluateInventory(intent).Ready, "healthy direct whole-bill preview ready");
            c.Equal(before, game.Hash(), "healthy preview preserves state while paused");
            var twin = factory.Load(game.Save());
            c.That(transactions.CommitInventory(intent).Committed, "healthy direct commit retained");
            c.That(twin.World.Services.Get<IResourceTransaction>().CommitInventory(intent).Committed, "saved twin uses same owner");
            game.Clock.Resume(); twin.Clock.Resume(); game.Update(); twin.Update();
            c.That(game.Fault == null && twin.Fault == null, "healthy physical session continues");
            c.Equal(game.Hash(), twin.Hash(), "original inventory and journal continue identically");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "healthy settled payload survives save/load");
        }
        private static void Run(Check c)
        {
            Healthy(c);
            foreach (string read in new[] { "accepts", "capacity", "available", "incoming" })
                foreach (string mode in new[] { "private", "clock", "throw" })
                    foreach (bool commit in new[] { false, true }) Negative(c, read, mode, commit);
        }
    }
}
