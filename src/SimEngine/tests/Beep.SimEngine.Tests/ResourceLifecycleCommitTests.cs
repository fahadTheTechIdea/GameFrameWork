using System;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    // Direct engine contract proof, not a normal-player gameplay driver.
    internal static partial class ResourceLifecycleCommitTests
    {
        internal const string Case = "Resource lifecycle commit: terminal provider violations and verified publication";
        internal static void Register() { TestRunner.Add(Case, Run); TestRunner.Add(AdmissionCase, MutationAdmission); }
        internal static GameFactory Factory(string mode, bool flows = false)
            => ConnectedResourcePortTests.Factory(false, false, true, new ConnectedResourcePortModule(),
                definitions: FrameworkProofContent.Read("atomic-inventory.json"),
                edit: data => data.Get("entities")[1].Get("components").Get("atomic_inventory")
                    .Set("lifecycle_commit_fault", JsonValue.Of(mode)),
                editWorld: root => {
                    root.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("atomic.factory-start"));
                    if (flows) root.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(ResourceFlowsModule.ModuleId));
                });
        internal static Entity Endpoint(SimGame game) => game.World.Entities.All.Single(e => e.Alive && e.Def.Id == "atomic.factory");
        internal static ResourceInventoryIntent Intent(SimGame game, bool adding)
        {
            Entity endpoint = Endpoint(game);
            return new ResourceInventoryIntent(new ReservationOwner("proof.lifecycle-commit", endpoint.Id.Value), endpoint.Owner,
                endpoint.Id, new Bill(new[] { new ResourceAmount(game.World.Content.ResourceId("supply"), Fix.One) }),
                adding ? ResourceFlowKind.Produced : ResourceFlowKind.Consumed, new Code("proof.lifecycle-commit"));
        }
        internal static void MakeRoom(SimGame game)
        {
            if (!game.World.Services.Get<IResourceTransaction>().CommitInventory(Intent(game, false)).Committed)
                throw new InvalidOperationException("healthy original stock setup refused");
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("healthy stock setup faulted " + game.Fault);
        }
        private static void Negative(Check c, string mode, bool adding, bool restored = false)
        {
            var factory = Factory(mode); var game = ConstructionPlanTests.NewPoints(factory);
            var other = ConstructionPlanTests.NewPoints(factory); MakeRoom(game); MakeRoom(other);
            if (restored) game = factory.Load(game.Save());
            ulong isolated = other.Hash(); long time = game.Clock.StepIndex;
            IResourceTransaction transactions = game.World.Services.Get<IResourceTransaction>();
            var probe = Endpoint(game).Get<AtomicInventory>(); var intent = Intent(game, adding);
            game.Clock.Pause(); c.That(transactions.EvaluateInventory(intent).Ready, "commit fault does not falsify pure admission " + mode);
            Exception thrown = null;
            try { transactions.CommitInventory(intent); } catch (Exception error) { thrown = error; }
            string label = mode + "/" + (adding ? "add" : "remove") + (restored ? "/restored" : "/new");
            c.That(thrown is InvalidOperationException, "invalid commit returns no healthy success " + label);
            c.That(game.Fault != null, "direct commit latches terminal first fault " + label);
            if (mode == "throw-before" || mode == "throw-after")
                c.Equal("proof.original-lifecycle-commit-" + (mode == "throw-before" ? "before" : "after"), thrown?.Message, "original commit exception survives " + label);
            if (mode == "amount-throw") c.Equal("proof.original-lifecycle-amount", thrown?.Message, "original inspection exception survives " + label);
            if (mode == "reservations-throw") c.Equal("proof.original-lifecycle-reservations", thrown?.Message, "original promises exception survives " + label);
            c.That(!game.World.Events.HasPending, "no lifecycle success prefix before full verification " + label);
            var first = game.Fault; int calls = probe.LifecycleCommitCalls;
            c.Throws<InvalidOperationException>(() => transactions.CommitInventory(intent), "faulted commit is not retried " + label);
            c.Throws<InvalidOperationException>(() => transactions.EvaluateInventory(default), "faulted invalid preview also refuses " + label);
            c.Equal(calls, probe.LifecycleCommitCalls, "provider commit callback is never retried " + label);
            c.That(ReferenceEquals(first, game.Fault), "original fault remains authoritative " + label);
            c.Throws<InvalidOperationException>(() => game.Hash(), "partial commit cannot hash healthy " + label);
            c.Throws<InvalidOperationException>(() => game.Save(), "partial commit cannot save healthy " + label);
            game.Update(); c.Equal(time, game.Clock.StepIndex, "faulted Update earns no time " + label);
            c.Equal(isolated, other.Hash(), "other session remains unchanged " + label);
        }
        private static void Healthy(Check c)
        {
            var factory = Factory("none"); var game = ConstructionPlanTests.NewPoints(factory); MakeRoom(game);
            foreach (bool adding in new[] { true, false }) {
                var intent = Intent(game, adding); var transactions = game.World.Services.Get<IResourceTransaction>();
                game.Clock.Pause(); ulong before = game.Hash();
                c.That(transactions.EvaluateInventory(intent).Ready, "healthy admission remains ready");
                c.Equal(before, game.Hash(), "healthy admission is pure"); var twin = factory.Load(game.Save());
                c.That(transactions.CommitInventory(intent).Committed, "healthy actual native delta commits");
                c.That(twin.World.Services.Get<IResourceTransaction>().CommitInventory(intent).Committed, "saved twin commits same native delta");
                c.That(game.World.Events.HasPending, "verified result publishes existing lifecycle fact");
                game.Clock.Resume(); twin.Clock.Resume(); game.Update(); twin.Update();
                c.That(game.Fault == null && twin.Fault == null, "healthy original session continues");
                c.Equal(game.Hash(), twin.Hash(), "healthy stock and journal twin identity");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "settled result checkpoints through original codecs");
            }
        }
        private static void Publication(Check c, bool adding)
        {
            var factory = Factory("late-amount-throw"); var game = ConstructionPlanTests.NewPoints(factory); MakeRoom(game);
            var intent = Intent(game, adding); var transaction = game.World.Services.Get<IResourceTransaction>();
            game.Clock.Pause(); var twin = factory.Load(game.Save());
            // Pause is host control, not saved simulation progress; arm both sessions explicitly.
            twin.Clock.Pause();
            c.That(transaction.CommitInventory(intent).Committed, "publication uses verified values, not another Amount callback");
            c.That(twin.World.Services.Get<IResourceTransaction>().CommitInventory(intent).Committed, "saved provider publishes from same verified values");
            c.Equal(1, Endpoint(game).Get<AtomicInventory>().PostCommitAmountReads, "exactly one pure post-mutation Amount verification");
            c.Equal(1, Endpoint(twin).Get<AtomicInventory>().PostCommitAmountReads, "saved twin invokes no publication callback");
            c.That(game.World.Events.HasPending && game.Fault == null, "verified fact is pending without triggering hostile extra getter");
            game.Clock.Resume(); twin.Clock.Resume(); game.Update(); twin.Update();
            c.That(game.Fault == null && twin.Fault == null, "publication follows normal Update and journal delivery");
            c.Equal(game.Hash(), twin.Hash(), "verified facts and actual inventory continue identically");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "publication result checkpoints normally");
        }
        private static void Run(Check c)
        {
            Healthy(c);
            Publication(c, true); Publication(c, false);
            foreach (string mode in new[] { "clock", "noop", "short", "throw-before", "throw-after", "promises", "amount-throw", "stock-clock", "reservations-throw" })
                foreach (bool adding in new[] { true, false }) Negative(c, mode, adding);
            foreach (string mode in new[] { "clock", "throw-after" })
                foreach (bool adding in new[] { true, false }) Negative(c, mode, adding, true);
        }
    }
}
