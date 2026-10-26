using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.RecoveryDestinationTests;

namespace Beep.SimEngine.Tests
{
    // Reuses the existing thin player workflow. Native engine owns quantities, claims and handling.
    public static class RecoveryQuantityTests
    {
        public static void Register() => TestRunner.Add("Recovery quantities: common quantum, paid returns and physical evacuation", Run);
        private static GameFactory Factory(string policy, bool evacuation = false)
            => WorkBudgetTests.Factory(controls: true, recovery: policy, physicalEdit: root =>
            {
                var machine = root.Get("entities")[2].Get("components");
                machine.Get("storage").Set("capacity_policy", JsonValue.Of("recovery.pair-cargo"));
                machine.Get("transporter").Set("handling", JsonValue.Of("recovery.pair-handling"));
            }, physicalDocuments: evacuation
                ? new[] { "recovery-quantities.json", "recovery-quantum-evacuation.json" }
                : new[] { "recovery-quantities.json" }, startupProfiles: evacuation ? new[] { "recovery-quantum-evacuation.start" } : null);
        private static HaulPreview OrdinaryPreview(SimGame game, string source, string destination, int amount, bool purchase = false)
            => game.Seat(0).View.Feature<LogisticsView>().PreviewHaul(Find(game, source).Id, Find(game, destination).Id,
                game.World.Content.ResourceId("supply"), Fix.FromInt(amount), purchase);
        private static void Run(Check c)
        {
            Facts(c);
            Returns(c, "return.demand-only.profile", RecoveryDestinationDeclaration.QuantumGap);
            Returns(c, "return.custom.profile", new Code("proof.recovery-quantum-gap"));
            PartialUnload(c); DemandFirst(c); Evacuation(c);
        }
        private static void Facts(Check c)
        {
            var factory = Factory("return.demand-only.profile"); var game = New(factory); ulong hash = game.Hash();
            HaulPreview preview = OrdinaryPreview(game, "source", "destination", 7, true);
            c.Equal(HaulStatus.Ready, preview.Status, "actual physical purchase admits a pair-compatible prefix");
            c.Equal(Fix.FromInt(6), preview.Amount, "native common quantum floors ordinary ceiling7 to6");
            c.Equal(CapacityStatus.Fits, preview.QuantityQuantum.Status, "ready view carries admitted common quantum");
            c.Equal(Fix.FromInt(2).Raw, preview.QuantityQuantum.Raw, "cargo pairs determine actual common quantity step");
            c.Equal(hash, game.Hash(), "ready quantity observation acquires no stock, receipts, jobs or payment");
            HaulPreview refused = OrdinaryPreview(game, "source", "destination", 1, true);
            c.That(refused.Status != HaulStatus.Ready && refused.QuantityQuantum.Raw == 0, "refused view fabricates no valid quantum");
            ResourceId resource = game.World.Content.ResourceId("supply");
            RecoveryDestinationFacts FactsFor(CapacityQuantum quantum, int feasible)
                => new RecoveryDestinationFacts(new PlayerId(0), Find(game, "machine").Id, Find(game, "destination").Id,
                    Find(game, "machine").Id, resource, Fix.FromInt(feasible), quantum, Fix.FromInt(3), 200, 20,
                    ResourceEndpointId.ForEntity(Find(game, "machine").Id), Code.None, ResourceEndpointId.ForEntity(Find(game, "destination").Id), Fix.Zero);
            c.Throws<ArgumentException>(() => FactsFor(default, 8), "default quantum cannot become feasible recovery facts");
            c.Throws<ArgumentException>(() => FactsFor(new CapacityQuantum(CapacityStatus.OutOfRange), 8), "failed quantum cannot become feasible recovery facts");
            c.Throws<ArgumentException>(() => FactsFor(preview.QuantityQuantum, 7), "nonmultiple feasible quantity is not an admitted contract fact");
            c.Equal(Fix.FromInt(2), game.World.Content.Get<IRecoveryDestinationPolicy>("return.demand-only").Evaluate(FactsFor(preview.QuantityQuantum, 8)).Ceiling,
                "native detached policy explicitly consumes admitted common quantum");
            c.Equal(Fix.FromInt(2), game.World.Content.Get<IRecoveryDestinationPolicy>("return.priority-required").Evaluate(FactsFor(preview.QuantityQuantum, 8)).Ceiling,
                "public-only policy consumes same quantity fact without copied capacity logic");
            c.Equal(hash, game.Hash(), "detached policy and invalid-facts probes mutate no authoritative game state");
        }
        private static void Returns(Check c, string policy, Code insufficient)
        {
            var factory = Factory(policy); var game = New(factory); BuyAndHold(game, true);
            Demand(game, "destination", 8, 40, 100, false); Demand(game, "return.depot", 3, 40, 200);
            long created = Recovery(game).Value.CreatedTick; var twin = factory.Load(game.Save()); Held(game, false, twin);
            Reach(game, () => Job(game).HasValue, "pair-compatible residual assignment", twin);
            c.Equal(Fix.FromInt(2), Job(game).Value.Amount, policy + " gap3 reserves only transferable2");
            c.That(!Job(game).Value.Source.IsValid && !Job(game).Value.Purchase, "recovery job binds paid cargo rather than repurchasing a pair");
            Reach(game, () => Stock(game, "return.depot") == Fix.FromInt(2) && Stock(game, "machine") == Fix.FromInt(6), "pair-compatible timed delivery", twin);
            Update(game, twin, 3); ulong hash = game.Hash();
            c.Equal(insufficient, Preview(game).Blocker, "remaining positive gap1 is a typed quantity wait, not a fault or oversupply");
            c.Equal(hash, game.Hash(), "insufficient-gap preview is pure");
            c.Equal(created, Recovery(game).Value.CreatedTick, "quantum wait retains existing recovery chronology");
            c.Equal(Fix.FromInt(60), Account(game).Balance, "actual eight-unit pickup is still the only payment");
            twin = factory.Load(game.Save()); Update(game, twin, 3);
            c.Equal(Fix.FromInt(6), Stock(game, "machine"), "saved quantum wait retains all six actual residual units");
            c.Equal(game.Hash(), twin.Hash(), "saved incompatible-gap wait remains deterministic");
            Demand(game, "destination", 6, 40, 250, true, twin);
            Reach(game, () => Stock(game, "destination") == Fix.FromInt(6) && Stock(game, "machine").IsZero, "compatible demand resumes same physical cargo", twin);
            c.Equal(Fix.FromInt(8), Stock(game, "destination") + Stock(game, "return.depot"), "quantized split deliveries conserve paid eight");
            c.Equal(Fix.FromInt(60), Account(game).Balance, "changing compatible demand neither spends nor credits additional money");
            Update(game, twin, 3); c.Equal(game.Hash(), twin.Hash(), "public/native quantity decisions and actual completion save identically");
        }
        private static void PartialUnload(Check c)
        {
            var factory = Factory("return.demand-only.profile"); var game = New(factory); BuyAndHold(game, true);
            long created = Recovery(game).Value.CreatedTick; var twin = factory.Load(game.Save()); Held(game, false, twin);
            Reach(game, () => Stock(game, "destination") == Fix.FromInt(2) && Stock(game, "machine") == Fix.FromInt(6), "first earned pair during larger unloading", twin);
            c.Equal(Fix.FromInt(8), Job(game).Value.Amount, "partial unloading belongs to the admitted actual eight-unit shipment");
            Held(game, true, twin); Demand(game, "destination", 3, 40, 200, true, twin);
            twin = factory.Load(game.Save()); Held(game, false, twin); Update(game, twin, 3);
            c.Equal(RecoveryDestinationDeclaration.QuantumGap, Preview(game).Blocker, "interrupted residual does not transfer odd gap1");
            c.Equal(Find(game, "destination").Id, Preview(game).Destination, "diagnostic names the actual requested endpoint, not unrelated no-demand depot");
            c.Equal(Fix.FromInt(2), Stock(game, "destination"), "interrupting unloading preserves its delivered pair prefix");
            c.Equal(Fix.FromInt(6), Stock(game, "machine"), "same saved actual cargo retains remaining three pairs");
            c.Equal(created, Recovery(game).Value.CreatedTick, "held partial unload and quantum wait never reset recovery age");
            Demand(game, "destination", 8, 8, 200, true, twin);
            Reach(game, () => Stock(game, "destination") == Fix.FromInt(8) && Stock(game, "machine").IsZero, "saved pair unloading finishes after compatible demand", twin);
            c.Equal(Fix.FromInt(60), Account(game).Balance, "partial-unload hold/resume never repeats the actual purchase");
            Update(game, twin, 3); c.Equal(game.Hash(), twin.Hash(), "saved quantized interruption completes identically");
        }
        private static void DemandFirst(Check c)
        {
            var factory = Factory("return.demand-first.profile"); var game = New(factory); BuyAndHold(game, true);
            Demand(game, "return.depot", 1, 40, 250); Demand(game, "destination", 6, 6, 100);
            var twin = factory.Load(game.Save()); Held(game, false, twin);
            Reach(game, () => Job(game).HasValue, "feasible lower-priority compatible destination", twin);
            c.Equal(Find(game, "destination").Id, Job(game).Value.Destination, "incompatible nearby high-priority gap cannot displace feasible farther demand");
            c.Equal(Fix.FromInt(6), Job(game).Value.Amount, "lower-priority demand still admits only its actual compatible gap");
            Reach(game, () => Stock(game, "destination") == Fix.FromInt(6) && Stock(game, "machine") == Fix.FromInt(2), "demand-first six-unit physical completion", twin);
            Update(game, twin, 3);
            c.Equal(RecoveryDestinationDeclaration.QuantumGap, Preview(game).Blocker, "maximum protects fulfilled store while remaining pair waits for gap1");
            Demand(game, "return.depot", 2, 40, 250, true, twin);
            Reach(game, () => Stock(game, "return.depot") == Fix.FromInt(2) && Stock(game, "machine").IsZero, "compatible nearby demand completes residual pair", twin);
            c.Equal(Fix.FromInt(60), Account(game).Balance, "demand-first compatible retry has no extra financial effect");
            Update(game, twin, 3); c.Equal(game.Hash(), twin.Hash(), "saved demand-first feasibility/ranking sequence remains equal");
        }
        private static LogisticsDispatchOffer? EvacuationOffer(SimGame game, EntityId source)
        {
            var offers = new List<LogisticsDispatchOffer>(); game.Seat(0).View.Feature<LogisticsView>().CopyQueue(offers);
            foreach (var offer in offers) if (offer.Kind == LogisticsDispatchKind.Evacuation && offer.RecoveryOrigin == source) return offer;
            return null;
        }
        private static void Evacuation(Check c)
        {
            var factory = Factory("recovery.quantum-evacuation", true); var game = New(factory);
            Entity source = Find(game, "recovery.triple-source"); IInventory inventory = source.Get<IInventory>();
            ulong hash = game.Hash(); HaulPreview preview = OrdinaryPreview(game, "recovery.triple-source", "return.depot", 8);
            c.Equal(HaulStatus.Ready, preview.Status, "authored triple-source/pair-carrier has a real feasible transfer");
            c.Equal(Fix.FromInt(6).Raw, preview.QuantityQuantum.Raw, "common step is LCM3/2=6, not destination quantum1 or carrier quantum2");
            c.Equal(Fix.FromInt(6), preview.Amount, "eight-unit carrier admits one actual six-unit compatible shipment");
            c.Equal(hash, game.Hash(), "common-quantum feasibility preview consumes no evacuated stock");
            Demand(game, "return.depot", 7, 40, 200);
            Reach(game, () => Job(game).HasValue, "actual common-quantum evacuation assignment");
            c.Equal(source.Id, Job(game).Value.Source, "evacuation uses actual source rather than a cargo-return shortcut");
            c.Equal(Fix.FromInt(6), Job(game).Value.Amount, "destination gap7 admits source/cargo common6");
            c.That(!Job(game).Value.Purchase, "moving authored evacuation stock requires no economic purchase");
            long created = EvacuationOffer(game, source.Id).Value.CreatedTick; var twin = factory.Load(game.Save());
            Reach(game, () => Stock(game, "return.depot") == Fix.FromInt(6) && Stock(game, "machine").IsZero, "real common-quantum load/travel/unload", twin);
            Update(game, twin, 3);
            c.Equal(Fix.FromInt(6), inventory.Amount(game.World.Content.ResourceId("supply")), "remaining real source stock is not split below its transferable common step");
            c.Equal(RecoveryDestinationDeclaration.QuantumGap, EvacuationOffer(game, source.Id).Value.Haul.Blocker, "positive gap1 reports native evacuation quantity wait");
            c.Equal(created, EvacuationOffer(game, source.Id).Value.CreatedTick, "evacuation wait keeps original intent chronology");
            c.Equal(Fix.FromInt(100), Account(game).Balance, "actual physical stock evacuation neither buys nor sells resources");
            twin = factory.Load(game.Save()); Update(game, twin, 3); c.Equal(game.Hash(), twin.Hash(), "saved common-quantum evacuation wait remains equal");
            Demand(game, "destination", 6, 40, 100, true, twin);
            Reach(game, () => Stock(game, "destination") == Fix.FromInt(6) && Stock(game, "machine").IsZero, "remaining actual source6 delivered to compatible demand", twin);
            Update(game, twin, 3);
            c.That(!source.Alive && inventory.Amount(game.World.Content.ResourceId("supply")).IsZero, "normal engine cleanup retires only actually emptied evacuation source");
            c.Equal(Fix.FromInt(12), Stock(game, "destination") + Stock(game, "return.depot"), "all authored evacuation12 physically conserve across destinations");
            c.Equal(Fix.FromInt(100), Account(game).Balance, "completion changes no money or generation income");
            c.Equal(game.Hash(), twin.Hash(), "saved multi-endpoint common-quantum evacuation finishes identically");
        }
    }
}
