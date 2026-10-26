using System;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Positive reads use public contracts. The one explicit live reference below observes fault-probe counters only.
    internal static class InventoryQuantityProviderTests
    {
        internal const string Case = "Inventory quantities: local named original endpoints and illegal getters obey first-fault isolation";
        internal const string PortCase = "Inventory port quantities: final publication validates native units and rejects late invalid getters";
        private static readonly Code Shared = new Code("shared");
        internal static void Register() { TestRunner.Add(Case, Run); TestRunner.Add(PortCase, PortQuantities); }
        private static GameFactory Factory(bool half, string mode)
            => ConnectedResourcePortTests.Factory(half, false, true, new ConnectedResourcePortModule(),
                definitions: FrameworkProofContent.Read("atomic-inventory.json"),
                edit: root => {
                    var part = root.Get("entities")[1].Get("components").Get("atomic_inventory");
                    part.Set("lifecycle_read", JsonValue.Of(mode == "none" ? "none" : "amount"));
                    part.Set("lifecycle_fault", JsonValue.Of(mode));
                }, editWorld: root => root.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("atomic.factory-start")));
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static EntityId Site(SimGame game) => game.Seat(0).Snapshot.Entities.Single(e => e.Def == "atomic.factory").Id;
        private static ResourceId Supply(SimGame game)
            => game.Seat(0).View.TryFindResource("supply", out var resource) ? resource : throw new InvalidOperationException("authored supply unavailable");
        private static ResourceEndpointId Endpoint(SimGame game)
        {
            Code code = game.Seat(0).View.Feature<InventoryView>().ReadResourcePort(Site(game), Shared, Supply(game), out var fact);
            if (!code.IsNone || !fact.Ready) throw new InvalidOperationException("actual original endpoint unavailable " + code); return fact.Endpoint;
        }
        private static void Healthy(Check c, bool half)
        {
            var factory = Factory(half, "none"); var game = New(factory); var idle = New(factory); ulong isolated = idle.Hash();
            var view = game.Seat(0).View.Feature<InventoryView>(); var site = Site(game); var supply = Supply(game); var original = Endpoint(game);
            ulong pure = game.Hash(); long at = game.Clock.StepIndex;
            foreach (Code port in new[] { Code.None, Shared })
            {
                c.That(view.TryGetAmount(site, supply, out var quantity, port) && quantity == Fix.FromInt(12), "local/named read returns actual stock");
                c.That(view.TryGetAmount(site, supply, out quantity, port, original) && quantity == Fix.FromInt(12), "explicit original reads that same stock owner");
                c.That(!view.TryGetAmount(site, supply, out quantity, port, ResourceEndpointId.ForEntity(new EntityId(int.MaxValue))) && quantity.IsZero,
                    "stale original never substitutes the selected pool");
            }
            c.That(!view.TryGetAmount(site, supply, out var absent, new Code("missing.port")) && absent.IsZero, "unknown named endpoint is unavailable");
            c.That(!game.Seat(1).View.Feature<InventoryView>().TryGetAmount(site, supply, out absent, Shared) && absent.IsZero, "foreign private quantity unavailable");
            c.Equal(pure, game.Hash(), "successful/refused quantity queries are pure"); c.Equal(at, game.Clock.StepIndex, "query earns no game time");
            var twin = factory.Load(game.Save()); game.Clock.Pause(); twin.Clock.Pause(); game.Update(); twin.Update();
            c.That(view.TryGetAmount(site, supply, out var retained, Shared, original) && retained == Fix.FromInt(12), "paused same-owner quantity remains observable");
            c.Equal(game.Hash(), twin.Hash(), "saved paused quantity state identical"); game.Clock.Resume(); twin.Clock.Resume();
            for (int i = 0; i < 3; i++) { game.Update(); twin.Update(); c.Equal(game.Hash(), twin.Hash(), "normal continuation unaffected by reads"); }
            c.That(retained == Fix.FromInt(12) && view.TryGetAmount(site, supply, out var current) && current == retained, "retained scalar stays detached");
            c.Equal(isolated, idle.Hash(), "other session has no borrowed quantity or clock state");
        }
        private static void Negative(Check c, bool half, Code port, string mode)
        {
            var factory = Factory(half, mode); var game = New(factory); var idle = New(factory); ulong isolated = idle.Hash();
            var site = Site(game); var supply = Supply(game); var original = Endpoint(game);
            var views = new[] { game.Seat(0).View.Feature<InventoryView>(), game.Seat(1).View.Feature<InventoryView>(), game.Observer.Feature<InventoryView>() };
            // Explicit negative instrumentation only: no stock, entity version, work or clock writes from this driver.
            var endpoint = game.World.Entities.Get(site).Get<AtomicInventory>();
            c.That(views[0].TryGetAmount(site, supply, out var healthy, port, original) && healthy == Fix.FromInt(12), "endpoint healthy before the paused fault probe");
            game.Clock.Pause(); ulong before = game.Hash(); long at = game.Clock.StepIndex;
            c.That(!views[0].TryGetAmount(site, supply, out var unavailable, port, ResourceEndpointId.ForEntity(new EntityId(int.MaxValue))) && unavailable.IsZero,
                "unavailable original refuses before illegal quantity callback");
            c.That(!views[1].TryGetAmount(site, supply, out unavailable, port) && unavailable.IsZero, "foreign private read refuses before callback");
            c.That(!views[0].TryGetAmount(site, new ResourceId(int.MaxValue), out unavailable, port) && unavailable.IsZero, "unknown resource refuses before callback");
            c.Equal(0, endpoint.LifecycleCalls, "ordinary unavailability does not execute getter probe"); c.Equal(before, game.Hash(), "refused reads do not mutate owner");
            Fix amount = Fix.FromInt(99);
            c.Throws<InvalidOperationException>(() => views[0].TryGetAmount(site, supply, out amount, port, original), "illegal getter never returns successful quantity: " + mode);
            c.That(amount.IsZero && game.Fault != null && game.Fault.Phase == "observation.provider", "getter faults once and publishes no stale/partial output");
            c.Equal(1, endpoint.LifecycleCalls, "fault comes from actual selected scalar getter");
            if (mode == "throw") c.That(game.Fault.Message.Contains("proof.original-lifecycle-read-error"), "first callback exception retained");
            if (mode == "negative-amount") c.That(game.Fault.Message.Contains("inventory quantity violates"), "invalid native quantity has explicit first cause");
            var first = game.Fault; int calls = endpoint.LifecycleCalls;
            foreach (var cached in views)
            {
                Fix stale = Fix.FromInt(99);
                c.Throws<InvalidOperationException>(() => cached.TryGetAmount(site, supply, out stale, port, original), "cached own/foreign/observer read rejects first-fault session");
                c.That(stale.IsZero, "faulted cached read clears caller value before callback");
                c.Throws<InvalidOperationException>(() => cached.TryGetAmount(new EntityId(int.MaxValue), new ResourceId(int.MaxValue), out stale), "fault gates even missing/unknown early returns");
                c.That(stale.IsZero, "faulted unavailable read cannot retain a prior scalar");
            }
            c.Equal(calls, endpoint.LifecycleCalls, "failed getter is never retried"); c.That(ReferenceEquals(first, game.Fault), "first fault identity never replaced");
            c.Equal(0, game.Update(), "faulted session cannot earn an update"); c.Equal(at, game.Clock.StepIndex, "failure does not earn game time");
            c.Throws<InvalidOperationException>(() => game.Save(), "bad callback cannot checkpoint partial state");
            c.Throws<InvalidOperationException>(() => game.Hash(), "bad callback cannot advertise healthy hash");
            c.Equal(isolated, idle.Hash(), "bad endpoint cannot poison another interleaved world");
        }
        private static void PortQuantities(Check c)
        {
            foreach (bool half in new[] { false, true })
            {
                var positive = Factory(half, "none"); var healthy = New(positive); var site = Site(healthy); var supply = Supply(healthy);
                var view = healthy.Seat(0).View.Feature<InventoryView>();
                c.Equal(Code.None, view.ReadResourcePort(site, Shared, supply, out var held), "ordinary actual port remains available");
                c.Equal(Fix.FromInt(12), held.Stock.AmountOf(supply), "ordinary port publishes exact original stock");
                ulong before = healthy.Hash(); view.ReadResourcePort(site, Shared, supply, out _); c.Equal(before, healthy.Hash(), "port projection is pure");
                healthy.Clock.Pause(); var twin = positive.Load(healthy.Save());
                c.Equal(Code.None, twin.Seat(0).View.Feature<InventoryView>().ReadResourcePort(site, Shared, supply, out var saved), "saved paused actual port remains available");
                c.Equal(held.Stock.AmountOf(supply), saved.Stock.AmountOf(supply), "saved port retains same real quantity");
                c.Equal(healthy.Hash(), twin.Hash(), "saved port queries retain original owner identity");
                foreach (string mode in new[] { "late-negative-amount", "late-fractional-amount" })
                {
                    var factory = Factory(half, mode); var game = New(factory); var idle = New(factory); ulong isolated = idle.Hash();
                    site = Site(game); supply = Supply(game); view = game.Seat(0).View.Feature<InventoryView>();
                    var endpoint = game.World.Entities.Get(site).Get<AtomicInventory>(); // Negative counter instrumentation only.
                    c.Equal(Code.None, view.ReadResourcePort(site, Shared, supply, out var original), "endpoint is valid before the explicit paused probe");
                    game = factory.Load(game.Save()); view = game.Seat(0).View.Feature<InventoryView>();
                    endpoint = game.World.Entities.Get(site).Get<AtomicInventory>();
                    var cachedViews = new[] { view, game.Seat(1).View.Feature<InventoryView>(), game.Observer.Feature<InventoryView>() };
                    game.Clock.Pause(); long at = game.Clock.StepIndex;
                    ResourcePortSnapshot facts = default; Exception thrown = null;
                    try { view.ReadResourcePort(site, Shared, supply, out facts); } catch (Exception error) { thrown = error; }
                    c.That(thrown != null && facts.Stock == null, "late illegal getter publishes no completed port facts: " + mode);
                    c.Equal(2, endpoint.LifecycleCalls, "actual binding reads valid quantity before final projection reads the illegal value");
                    var fault = game.Fault; c.That(fault != null && fault.Phase == "observation.provider", "late port quantity latches original provider fault");
                    if (fault == null) continue; // Avoid consequential assertions when the old engine admits malformed facts.
                    foreach (var cached in cachedViews)
                    {
                        c.Throws<InvalidOperationException>(() => cached.ReadResourcePort(site, Shared, supply, out facts), "cached views reject the original first-fault session");
                        c.That(facts.Stock == null, "faulted port read clears prior facts");
                    }
                    c.Equal(2, endpoint.LifecycleCalls, "faulted provider getter is not retried");
                    c.That(ReferenceEquals(fault, game.Fault), "late quantity fault keeps original identity");
                    c.Equal(0, game.Update(), "faulted port cannot earn an update"); c.Equal(at, game.Clock.StepIndex, "failed projection earns no game time");
                    c.Throws<InvalidOperationException>(() => game.Hash(), "invalid port projection cannot hash healthy");
                    c.Throws<InvalidOperationException>(() => game.Save(), "invalid port projection cannot checkpoint healthy");
                    c.Equal(Fix.FromInt(12), original.Stock.AmountOf(supply), "held valid port facts remain detached");
                    c.Equal(isolated, idle.Hash(), "late port failure does not affect another session");
                }
            }
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "existing native image remains authoritative");
            foreach (bool half in new[] { false, true })
            {
                Healthy(c, half);
                foreach (Code port in new[] { Code.None, Shared }) foreach (string mode in new[] { "private", "clock", "throw", "negative-amount" })
                    Negative(c, half, port, mode);
            }
        }
    }
}
