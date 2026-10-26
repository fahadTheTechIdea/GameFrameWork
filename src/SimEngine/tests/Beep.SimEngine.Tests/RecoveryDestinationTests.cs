using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Thin player simulation only: native recovery owns selection, jobs, receipts and physical movement.
    public static class RecoveryDestinationTests
    {
        public static void Register() => TestRunner.Add("Recovery destinations: authored demand policies and paid physical cargo", Run);
        private static GameFactory Factory(string profile, Action<JsonValue> edit = null)
            => WorkBudgetTests.Factory(controls: true, recovery: profile, recoveryEdit: edit);
        internal static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "purchase-haul", Seed = 73 });
        internal static Entity Find(SimGame game, string id)
        { foreach (Entity entity in game.World.Entities.All) if (entity.Def.Id == id) return entity; throw new InvalidOperationException("missing " + id); }
        internal static Fix Stock(SimGame game, string id) => Find(game, id).Get<IInventory>().Amount(game.World.Content.ResourceId("supply"));
        internal static BusinessAccountSnapshot Account(SimGame game)
        {
            Code refusal = game.Seat(0).View.Feature<BusinessView>().ReadAccount(new PlayerId(0), out var result);
            if (!refusal.IsNone) throw new InvalidOperationException("account refused " + refusal); return result;
        }
        internal static WorkJobView? Job(SimGame game)
        {
            var items = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(items);
            foreach (var job in items) if (job.IsHaul && job.Worker == Find(game, "machine").Id) return job; return null;
        }
        internal static LogisticsDispatchOffer? Recovery(SimGame game)
        {
            var items = new List<LogisticsDispatchOffer>(); game.Seat(0).View.Feature<LogisticsView>().CopyQueue(items);
            foreach (var item in items) if (item.Kind == LogisticsDispatchKind.CargoReturn && item.RecoveryOrigin == Find(game, "machine").Id) return item; return null;
        }
        internal static HaulPreview Preview(SimGame game)
        {
            var items = new List<CargoReturnView>();
            Code refusal = game.Seat(0).View.Feature<LogisticsView>().CopyCargoReturns(Find(game, "machine").Id, items);
            if (!refusal.IsNone || items.Count != 1) throw new InvalidOperationException("expected one current cargo return: " + refusal); return items[0].Return;
        }
        internal static void Update(SimGame game, SimGame twin = null, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                if (game.RunTicks(1) != 1) throw new InvalidOperationException("recovery Update fault " + game.Fault);
                if (twin != null && twin.RunTicks(1) != 1) throw new InvalidOperationException("recovery twin fault " + twin.Fault);
            }
        }
        internal static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 160 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " timed out at " + game.Tick + ", cargo=" + Stock(game, "machine") +
                ", destination=" + Stock(game, "destination") + ", depot=" + Stock(game, "return.depot") + ", cash=" + Account(game).Balance + ", job=" + Job(game)?.Id + "/" + Job(game)?.State + "/" + Job(game)?.Blocker);
        }
        internal static void Held(SimGame game, bool held, SimGame twin = null)
        {
            game.Seat(0).Submit(new SetWorkerHeldCommand { Worker = Find(game, "machine").Id, Held = held });
            if (twin != null) twin.Seat(0).Submit(new SetWorkerHeldCommand { Worker = Find(twin, "machine").Id, Held = held }); Update(game, twin);
        }
        internal static void Demand(SimGame game, string store, int desired, int maximum, int priority, bool enabled = true, SimGame twin = null)
        {
            var settings = new LogisticsDemandSettings(enabled, Fix.FromInt(desired), Fix.FromInt(desired), Fix.FromInt(maximum), priority, LogisticsReserveAccess.Protected);
            game.Seat(0).Submit(new ConfigureStockPolicyCommand { Store = Find(game, store).Id, Resource = game.World.Content.ResourceId("supply"), Change = LogisticsDemandChange.Replace(settings) });
            if (twin != null) twin.Seat(0).Submit(new ConfigureStockPolicyCommand { Store = Find(twin, store).Id, Resource = twin.World.Content.ResourceId("supply"), Change = LogisticsDemandChange.Replace(settings) }); Update(game, twin);
        }
        private static void Access(SimGame game, bool open, SimGame twin = null)
        {
            game.Seat(0).Submit(new SetPointAccessCommand { Open = open });
            if (twin != null) twin.Seat(0).Submit(new SetPointAccessCommand { Open = open }); Update(game, twin);
        }
        internal static Fix BuyAndHold(SimGame game, bool full)
        {
            Demand(game, "destination", 8, 8, 100);
            Reach(game, () => full ? Stock(game, "machine") == Fix.FromInt(8) : Stock(game, "machine").IsPositive, "actual paid cargo pickup");
            Fix cargo = Stock(game, "machine"); Held(game, true); return cargo;
        }
        private static void Run(Check c) { Content(c); DemandFirst(c); PartialUnload(c); DemandOnly(c); Nearest(c); Extension(c); }
        private static void Content(Check c)
        {
            c.Throws<ContentException>(() => New(Factory("return.nearest.profile", root => root.Get("logistics_recovery_profiles")[0].Get("cargo_return").Remove("destination_policy"))), "each recovery kind requires explicit policy authoring");
            c.Throws<ContentException>(() => New(Factory("return.nearest.profile", root => root.Get("logistics_recovery_profiles")[3].Get("cargo_return").Set("destination_policy", JsonValue.Of("missing")))), "even inactive authored recovery policy references resolve before gameplay");
            c.Throws<ContentException>(() => New(Factory("return.nearest.profile", root => root.Get("recovery_destination_policies")[0].Set("mode", JsonValue.Of("fallback")))), "unknown native destination mode is not translated or defaulted");
            var factory = Factory("return.custom.profile");
            c.That(New(factory).World.Content.Find<IRecoveryDestinationPolicy>("return.priority-required") is MinimumPriorityRecoveryDestinationDef, "public-only policy aliases the existing content contract");
        }
        private static void DemandFirst(Check c)
        {
            var factory = Factory("return.demand-first.profile"); var game = New(factory); Fix cargo = BuyAndHold(game, false);
            Fix balance = Account(game).Balance; c.That(cargo < Fix.FromInt(8), "interruption occurs after real partial pickup");
            Demand(game, "return.depot", 1, 40, 5); var twin = factory.Load(game.Save());
            Held(game, false, twin);
            Reach(game, () => Job(game).HasValue && !Job(game).Value.Source.IsValid, "same paid carrier obtains a real return shipment", twin);
            c.Equal(Find(game, "destination").Id, Job(game).Value.Destination, "higher active demand outranks a much closer feasible depot");
            c.Equal(cargo, Job(game).Value.Loaded, "recovery binds actual loaded cargo, not a second pickup");
            c.That(!Job(game).Value.Purchase, "returning paid cargo is a stock delivery, not another purchase");
            c.Equal(balance, Account(game).Balance, "recovery assignment spends no money");
            Access(game, false, twin); Update(game, twin, 3);
            c.Equal(cargo, Stock(game, "machine"), "closed route preserves actual paid cargo and its commitment");
            c.Equal(Fix.Zero, Stock(game, "destination"), "route wait does not teleport delivery");
            c.Equal(balance, Account(game).Balance, "route retries do not repurchase retained cargo");
            twin = factory.Load(game.Save()); Access(game, true, twin);
            Reach(game, () => Stock(game, "destination").IsPositive, "reopened route physically unloads retained cargo", twin);
            c.Equal(balance, Account(game).Balance, "earned return delivery never charges paid material again");
            c.Equal(game.Hash(), twin.Hash(), "saved route recovery and physical return settle identically");
            Demand(game, "return.depot", 1, 40, 5, false, twin);
            Reach(game, () => Stock(game, "destination") == Fix.FromInt(8) && Stock(game, "machine").IsZero, "native demand fills only the original8 units", twin);
            c.Equal(Fix.FromInt(60), Account(game).Balance, "partial interrupted pickup plus later remainder buys exactly8 at5/unit");
            c.Equal(Fix.Zero, Stock(game, "return.depot"), "disabled lower-priority demand creates no extra delivery");
            Update(game, twin, 4); c.Equal(game.Hash(), twin.Hash(), "fulfilled recovery and purchase continuation remain deterministic");
        }
        private static void PartialUnload(Check c)
        {
            var factory = Factory("return.demand-first.profile"); var game = New(factory); BuyAndHold(game, true);
            c.Equal(Fix.FromInt(60), Account(game).Balance, "full physical pickup pays once before interruption");
            long created = Recovery(game).Value.CreatedTick; var twin = factory.Load(game.Save()); Held(game, false, twin);
            Reach(game, () => Stock(game, "destination").IsPositive && Stock(game, "machine").IsPositive, "physical timed partial unloading", twin);
            c.Equal(Fix.FromInt(8), Stock(game, "destination") + Stock(game, "machine"), "partial unload conserves actual purchased quantities");
            twin = factory.Load(game.Save()); Held(game, true, twin);
            Fix delivered = Stock(game, "destination"), remaining = Stock(game, "machine");
            c.Equal(created, Recovery(game).Value.CreatedTick, "interrupted residual shipment retains its original recovery age");
            Update(game, twin, 3);
            c.Equal(delivered, Stock(game, "destination"), "held partial unloading advances no delivered prefix");
            c.Equal(remaining, Stock(game, "machine"), "held partial unloading loses no cargo");
            c.Equal(Fix.FromInt(60), Account(game).Balance, "partial unload interruption neither refunds nor repurchases");
            twin = factory.Load(game.Save()); Held(game, false, twin);
            Reach(game, () => Stock(game, "destination") == Fix.FromInt(8) && Stock(game, "machine").IsZero, "remaining return physically completes", twin);
            c.Equal(Fix.FromInt(60), Account(game).Balance, "reassigned residual stock is never another economic purchase");
            c.Equal(0L, Account(game).GenerationSettlements, "hauling/recovery generates no production-income reward");
            Update(game, twin, 4); c.That(!Recovery(game).HasValue, "normal engine preparation retires only the resolved recovery intent");
            c.Equal(game.Hash(), twin.Hash(), "saved partial unloading and reassignment complete identically");
        }
        private static void DemandOnly(Check c)
        {
            var factory = Factory("return.demand-only.profile"); var game = New(factory); BuyAndHold(game, true);
            Demand(game, "destination", 8, 8, 100, false); Held(game, false); Update(game, count: 3);
            ulong hash = game.Hash(); var preview = Preview(game);
            c.Equal(HaulStatus.PolicyBlocked, preview.Status, "demand-only deliberately refuses feasible stores without demand");
            c.Equal(RecoveryDestinationDeclaration.NoDemand, preview.Blocker, "no matching demand has a stable typed blocker");
            c.Equal(hash, game.Hash(), "destination-policy diagnostics neither allocate jobs nor mutate stock");
            var twin = factory.Load(game.Save()); Update(game, twin, 3);
            c.Equal(Fix.FromInt(8), Stock(game, "machine"), "no-demand save retains all actual purchased cargo");
            c.Equal(Fix.FromInt(60), Account(game).Balance, "no-demand wait creates neither refund nor purchase");
            c.Equal(game.Hash(), twin.Hash(), "saved no-demand wait continues identically");
            long created = Recovery(game).Value.CreatedTick;
            Demand(game, "destination", 3, 40, 100, true, twin);
            Reach(game, () => Job(game).HasValue, "new actual demand unlocks existing residual recovery", twin);
            c.Equal(Fix.FromInt(3), Job(game).Value.Amount, "policy caps an8-unit feasible return to the current3-unit demand gap");
            Reach(game, () => Stock(game, "destination") == Fix.FromInt(3) && Stock(game, "machine") == Fix.FromInt(5), "reduced demand receives only its real gap", twin);
            Update(game, twin, 3);
            c.Equal(RecoveryDestinationDeclaration.NoDemand, Preview(game).Blocker, "fulfilled demand does not force remaining cargo into spare capacity");
            c.Equal(created, Recovery(game).Value.CreatedTick, "remaining5 keep the same recovery chronology");
            Demand(game, "return.depot", 5, 40, 175, true, twin);
            Reach(game, () => Stock(game, "return.depot") == Fix.FromInt(5) && Stock(game, "machine").IsZero, "new depot demand physically receives the remaining5", twin);
            c.Equal(Fix.FromInt(8), Stock(game, "return.depot") + Stock(game, "destination"), "split demand deliveries conserve the original paid8");
            c.Equal(Fix.FromInt(60), Account(game).Balance, "changing destination demand never credits or repurchases stock");
            Update(game, twin, 4); c.Equal(game.Hash(), twin.Hash(), "saved reduced-gap/change-of-destination continuation is deterministic");
        }
        private static void Nearest(Check c)
        {
            var factory = Factory("return.nearest.profile"); var game = New(factory); BuyAndHold(game, true);
            Demand(game, "destination", 8, 8, 100, false); var twin = factory.Load(game.Save()); Held(game, false, twin);
            Reach(game, () => Job(game).HasValue, "explicit nearest policy obtains a real return", twin);
            c.Equal(Find(game, "return.depot").Id, Job(game).Value.Destination, "nearest behavior is explicitly authored, not a hidden demand-only fallback");
            Reach(game, () => Stock(game, "return.depot") == Fix.FromInt(8) && Stock(game, "machine").IsZero, "nearest actual depot unload completes", twin);
            c.Equal(Fix.Zero, Stock(game, "destination"), "no inventory is fabricated at the unselected store");
            c.Equal(Fix.FromInt(60), Account(game).Balance, "nearest return uses only already-paid physical goods");
            Update(game, twin, 3); c.Equal(game.Hash(), twin.Hash(), "explicit nearest saved continuation is deterministic");
        }
        private static void Extension(Check c)
        {
            var factory = Factory("return.custom.profile"); var game = New(factory); BuyAndHold(game, true);
            Demand(game, "return.depot", 2, 40, 200); var twin = factory.Load(game.Save()); Held(game, false, twin);
            Reach(game, () => Job(game).HasValue, "public-only content policy uses the same recovery scheduler", twin);
            c.Equal(Find(game, "return.depot").Id, Job(game).Value.Destination, "custom policy accepts demand200 and refuses existing demand100");
            c.Equal(Fix.FromInt(2), Job(game).Value.Amount, "custom policy uses actual bounded native demand facts");
            Reach(game, () => Stock(game, "return.depot") == Fix.FromInt(2) && Stock(game, "machine") == Fix.FromInt(6), "custom eligible demand physically receives2", twin);
            Update(game, twin, 3);
            c.Equal(new Code("proof.recovery-priority-required"), Preview(game).Blocker, "custom policy reports its own typed unmet requirement");
            Demand(game, "destination", 6, 40, 250, true, twin);
            Reach(game, () => Stock(game, "destination") == Fix.FromInt(6) && Stock(game, "machine").IsZero, "player policy change makes actual remaining cargo eligible", twin);
            c.Equal(Fix.FromInt(8), Stock(game, "destination") + Stock(game, "return.depot"), "public-only policy adds no stock, reservation or scheduler store");
            c.Equal(Fix.FromInt(60), Account(game).Balance, "mod policy selection never generates or spends extra money");
            Update(game, twin, 3); c.Equal(game.Hash(), twin.Hash(), "public-only policy and saved physical recovery remain identical");
        }
    }
}
