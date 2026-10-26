using System;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class WorkBudgetLifecycleTests
    {
        public static void Register() => TestRunner.Add("Tycoon budgets: route interruption, physical funding and provider integrity", Run);
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "purchase-haul", Seed = 67 });
        private static Entity Find(SimGame game, string id)
        { foreach (var entity in game.World.Entities.All) if (entity.Def.Id == id) return entity; throw new InvalidOperationException("missing " + id); }
        private static BusinessAccountSnapshot Account(SimGame game)
        {
            Code code = game.Seat(0).View.Feature<BusinessView>().ReadAccount(new PlayerId(0), out var result);
            if (!code.IsNone) throw new InvalidOperationException("account refused " + code); return result;
        }
        private static Fix Stock(SimGame game, string entity, string resource = "supply")
            => Find(game, entity).Get<IInventory>().Amount(game.World.Content.ResourceId(resource));
        private static List<MonetaryBudgetReceipt> Holds(SimGame game)
        { var items = new List<MonetaryBudgetReceipt>(); game.Seat(0).View.Feature<BusinessView>().CopyBudgets(new PlayerId(0), items); return items; }
        private static ActivityNodeView Node(SimGame game)
        {
            var orders = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
            foreach (var order in orders) if (order.Target == Find(game, "budget.site-a").Id)
            { var nodes = new List<ActivityNodeView>(); game.Seat(0).View.Feature<WorkView>().TryCopyNodes(order.Id, nodes); return nodes[0]; }
            throw new InvalidOperationException("missing live build order");
        }
        private static void Update(SimGame game, SimGame twin = null, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                if (game.RunTicks(1) != 1) throw new InvalidOperationException("lifecycle update fault: " + game.Fault);
                if (twin != null && twin.RunTicks(1) != 1) throw new InvalidOperationException("lifecycle twin fault: " + twin.Fault);
            }
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 120 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " timed out at " + game.Tick + ", balance=" + Account(game).Balance + ", reserved=" + Account(game).Reserved);
        }
        private static void Post(SimGame game)
        { game.Seat(0).Submit(new PostOrderCommand { Plan = "budget.build", Target = Find(game, "budget.site-a").Id }); Update(game); }
        private static void Access(SimGame game, bool open, SimGame twin = null)
        {
            game.Seat(0).Submit(new SetPointAccessCommand { Open = open });
            if (twin != null) twin.Seat(0).Submit(new SetPointAccessCommand { Open = open }); Update(game, twin);
        }
        private static bool Done(SimGame game) => game.Seat(0).View.Feature<WorkView>().ActiveOrders(Find(game, "budget.site-a").Id) == 0;
        private static void Run(Check c) { Route(c); Funding(c); Corruption(c); ProviderIntegrity(c); }

        // Normal gameplay: commands and bounded time only. No fixture-owned money, holds or progress.
        private static void Route(Check c)
        {
            var factory = WorkBudgetTests.Factory(lifecycle: true); var game = New(factory); Post(game);
            Reach(game, () => Holds(game).Count == 2, "machine assigned before route interruption");
            long last = Holds(game)[1].Id;
            c.Equal(WorkProgress.Zero, Node(game).Progress, "machine is physically traveling before work");
            Access(game, false);
            Reach(game, () => Holds(game).Count == 0, "broken route releases unpaid holds");
            c.Equal(Fix.FromInt(100), Account(game).Balance, "route failure releases promises, never credits cash");
            c.Equal(0, Node(game).Costs.Count, "reopened work has no detached unpaid budget bindings");
            c.That(!Node(game).Worker.IsValid, "inaccessible work frees its actual assigned worker");
            c.Equal(Fix.FromInt(8), Stock(game, "budget.site-a"), "interrupted travel consumes no material");
            c.Equal(WorkProgress.Zero, Node(game).Progress, "route failure invents no productive work");
            var twin = factory.Load(game.Save()); Update(game, twin, 3);
            c.Equal(game.Hash(), twin.Hash(), "blocked checkpoint continues without fabricated holds");
            c.Equal(0, Holds(game).Count, "closed route cannot reacquire money while waiting");
            Access(game, true, twin);
            Reach(game, () => Holds(game).Count == 2, "reopened route reassigns a real machine", twin);
            c.That(Holds(game)[0].Id > last, "route retry acquires fresh account receipt identities");
            c.Equal(Fix.FromInt(100), Account(game).Balance, "reassignment remains a promise rather than payment");
            twin = factory.Load(game.Save());
            Reach(game, () => Node(game).Progress.WholeTicks > 0, "machine reaches actual work site", twin);
            c.Equal(Fix.FromInt(20), Account(game).Balance, "productive construction pays its exact whole budget once");
            c.Equal(Fix.Zero, Account(game).Reserved, "productive settlement removes actual holds");
            c.Equal(Fix.FromInt(6), Stock(game, "budget.site-a"), "only productive work consumes construction materials");
            c.Equal(game.Hash(), twin.Hash(), "reassigned travel and payment continue identically");
            Reach(game, () => Done(game), "interrupted physical build completes", twin);
            Update(game, twin, 3);
            c.Equal(Fix.FromInt(20), Account(game).Balance, "completion never repeats the construction payment");
            c.Equal(game.Hash(), twin.Hash(), "post-completion native state remains identical");
        }
        private static void Funding(Check c)
        {
            var factory = WorkBudgetTests.Factory(lifecycle: true); var game = New(factory); Post(game);
            Reach(game, () => Holds(game).Count == 2, "construction budget held before authored debt");
            c.That(TestKit.Order(game, 0, new PostOrderCommand { Plan = "budget.upkeep", Target = Find(game, "processor").Id }).Accepted,
                "player starts authored timed running-cost work through the engine");
            Reach(game, () => Account(game).Balance == Fix.FromInt(-50), "actual running work charges its authored debt");
            c.Equal(Fix.FromInt(80), Account(game).Reserved, "underfunded hold remains owned by the same account");
            c.Equal(new BigInteger(Fix.FromInt(-130).Raw), Account(game).AvailableRaw, "negative availability is actual debt less real promises");
            c.Equal(WorkProgress.Zero, Node(game).Progress, "underfunded construction cannot progress");
            c.Equal(Fix.FromInt(8), Stock(game, "budget.site-a"), "underfunded construction consumes no materials");
            var twin = factory.Load(game.Save()); Update(game, twin, 4);
            c.Equal(Fix.FromInt(-50), Account(game).Balance, "authored upkeep is spent once, not every update");
            c.Equal(2, Holds(game).Count, "underfunded checkpoint retains exact unpaid receipts");
            c.Equal(game.Hash(), twin.Hash(), "saved debt and held physical intent continue identically");
            game.Seat(0).Submit(new EnqueueTransformationCommand { Transformer = Find(game, "processor").Id, Capability = new Code("transformation"), Transformation = "budget.fund", Count = 1 });
            twin.Seat(0).Submit(new EnqueueTransformationCommand { Transformer = Find(twin, "processor").Id, Capability = new Code("transformation"), Transformation = "budget.fund", Count = 1 });
            Update(game, twin); twin = factory.Load(game.Save());
            Reach(game, () => Account(game).GenerationSettlements == 1, "timed native resource generation funds the account", twin);
            c.Equal(Fix.FromInt(150), Account(game).Balance, "authored produced resource value credits exactly200 without a buyer");
            c.Equal(Fix.FromInt(40), Stock(game, "processor", "support"), "income leaves actual generated resources in inventory");
            c.Equal(Fix.FromInt(80), Account(game).Reserved, "generation funds but does not consume the construction hold");
            c.Equal(game.Hash(), twin.Hash(), "saved in-progress production settles once");
            Reach(game, () => Done(game), "funded machine physically finishes construction", twin);
            c.Equal(Fix.FromInt(70), Account(game).Balance, "funded construction pays80 once after real production income");
            c.Equal(Fix.Zero, Account(game).Reserved, "funded completed work leaves no live monetary promise");
            c.Equal(Fix.FromInt(6), Stock(game, "budget.site-a"), "funded work consumes actual building materials once");
            c.Equal(1L, Account(game).GenerationSettlements, "no duplicate production settlement after saved continuation");
            Update(game, twin, 4); c.Equal(game.Hash(), twin.Hash(), "debt, funding, travel and completion use one deterministic engine loop");
        }

        // Named corruption instrumentation; never used to manufacture normal gameplay state.
        private static void Corruption(Check c)
        {
            var factory = WorkBudgetTests.Factory(); var game = New(factory); Post(game);
            Reach(game, () => Holds(game).Count == 2, "native holds before checkpoint corruption probes");
            var board = game.World.Services.Get<WorkBoard>(); ActivityNodeExecution node = null;
            foreach (var order in board.Orders) if (order.Target == Find(game, "budget.site-a").Id) node = order.Nodes[0];
            var original = node.Costs; ulong hash = game.Hash();
            node.Costs = Array.Empty<WorkCostBinding>();
            c.Throws<CorruptStateException>(() => factory.Load(game.Save()), "orphan actual account hold refuses checkpoint restore");
            node.Costs = original;
            var receipt = original[0].Receipt.Value;
            node.Costs = Array.AsReadOnly(new[] { new WorkCostBinding(original[0].Choice, false,
                new MonetaryBudgetReceipt(receipt.Id + 1000, receipt.Company, receipt.Owner, receipt.Amount)), original[1] });
            c.Throws<CorruptStateException>(() => factory.Load(game.Save()), "matching amount with nonexistent receipt identity refuses restore");
            node.Costs = Array.AsReadOnly(new[] { new WorkCostBinding(original[0].Choice, true, receipt), original[1] });
            c.Throws<CorruptStateException>(() => factory.Load(game.Save()), "fabricated spent fact cannot retain an actual live hold");
            node.Costs = original;
            c.Equal(hash, game.Hash(), "restored probe instrumentation leaves native accounts and work unchanged");
            c.Equal(hash, factory.Load(game.Save()).Hash(), "unaltered native held checkpoint is valid");
        }

        // Fault proxies forward the actual native service; no test ledger or financial policy exists.
        private enum Fault { None, CheckMutates, CheckCommitted, ReserveFalse, SpendFalse, SpendWrongCash, ReleaseFalse, ReleaseWrongCash, NoInspection, TwiceInspection }
        private sealed class Proxy : IMonetaryBudgets
        {
            private readonly IMonetaryBudgets _actual;
            private readonly IEconomy _account;
            private readonly Fault _fault;
            internal Proxy(SimGame game, Fault fault)
            { _actual = game.World.Services.Get<IMonetaryBudgets>(); _account = game.World.Services.Get<IEconomy>(); _fault = fault; }
            public long Revision => _actual.Revision;
            public MonetaryBudgetStatus EvaluateReservations(PlayerId company, IReadOnlyList<MonetaryBudgetRequest> requests)
                => _fault == Fault.CheckCommitted ? MonetaryBudgetStatus.Committed : _actual.EvaluateReservations(company, requests);
            public MonetaryBudgetStatus Reserve(PlayerId company, IReadOnlyList<MonetaryBudgetRequest> requests, out IReadOnlyList<MonetaryBudgetReceipt> receipts)
            {
                if (_fault != Fault.ReserveFalse) return _actual.Reserve(company, requests, out receipts);
                var fabricated = new List<MonetaryBudgetReceipt>(); long id = 1000;
                foreach (var request in requests) fabricated.Add(new MonetaryBudgetReceipt(id++, company, request.Owner, request.Amount));
                receipts = fabricated.AsReadOnly(); return MonetaryBudgetStatus.Committed;
            }
            public MonetaryBudgetStatus EvaluateReceipts(PlayerId company, IReadOnlyList<MonetaryBudgetReceipt> receipts) => _actual.EvaluateReceipts(company, receipts);
            public MonetaryBudgetStatus EvaluateSpend(PlayerId company, IReadOnlyList<MonetaryBudgetReceipt> receipts)
            {
                if (_fault != Fault.CheckMutates) return _actual.EvaluateSpend(company, receipts);
                _actual.Release(company, receipts); return MonetaryBudgetStatus.Ready;
            }
            public MonetaryBudgetStatus Spend(PlayerId company, IReadOnlyList<MonetaryBudgetReceipt> receipts, Code reason)
            {
                if (_fault == Fault.SpendFalse) return MonetaryBudgetStatus.Committed;
                var result = _actual.Spend(company, receipts, reason);
                if (_fault == Fault.SpendWrongCash) _account.Credit(company, Fix.One, new Code("proof.budget-fault")); return result;
            }
            public MonetaryBudgetStatus Release(PlayerId company, IReadOnlyList<MonetaryBudgetReceipt> receipts)
            {
                if (_fault == Fault.ReleaseFalse) return MonetaryBudgetStatus.Committed;
                var result = _actual.Release(company, receipts);
                if (_fault == Fault.ReleaseWrongCash) _account.Credit(company, Fix.One, new Code("proof.budget-fault")); return result;
            }
            public void InspectBudgets(Action<IReadOnlyList<MonetaryBudgetReceipt>> inspect)
            {
                if (_fault == Fault.NoInspection) return;
                _actual.InspectBudgets(inspect); if (_fault == Fault.TwiceInspection) _actual.InspectBudgets(inspect);
            }
        }
        private static void ProviderIntegrity(Check c)
        {
            var game = New(WorkBudgetTests.Factory()); Post(game);
            Reach(game, () => Holds(game).Count == 2, "actual holds for named provider checks");
            ulong hash = game.Hash(); var company = new PlayerId(0); var held = Holds(game);
            c.Equal(MonetaryBudgetStatus.Ready, MonetaryBudgetExecution.CheckSpend(game.World, new Proxy(game, Fault.None), company, held), "native whole spend preflight passes exact validation");
            c.Equal(2, MonetaryBudgetExecution.Inspect(game.World, new Proxy(game, Fault.None)).Count, "native inspection reports exact detached receipts");
            c.Equal(hash, game.Hash(), "validated native checks remain pure");
            c.Throws<InvalidOperationException>(() => MonetaryBudgetExecution.CheckReservations(game.World, new Proxy(game, Fault.CheckCommitted), company,
                new[] { new MonetaryBudgetRequest(new ReservationOwner("proof.budget.new", 1), Fix.One) }), "preflight cannot report a mutation status");
            c.Throws<InvalidOperationException>(() => MonetaryBudgetExecution.Reserve(game.World, new Proxy(game, Fault.ReserveFalse), company,
                new[] { new MonetaryBudgetRequest(new ReservationOwner("proof.budget.new", 1), Fix.One) }), "false reserve success cannot invent a detached receipt");
            c.Throws<InvalidOperationException>(() => MonetaryBudgetExecution.Spend(game.World, new Proxy(game, Fault.SpendFalse), company, held, new Code("proof.budget-fault")), "false spend success cannot retain promises and cash");
            c.Throws<InvalidOperationException>(() => MonetaryBudgetExecution.Release(game.World, new Proxy(game, Fault.ReleaseFalse), company, held), "false release success cannot retain promises");
            c.Throws<InvalidOperationException>(() => MonetaryBudgetExecution.Inspect(game.World, new Proxy(game, Fault.NoInspection)), "inspection must publish one set");
            c.Throws<InvalidOperationException>(() => MonetaryBudgetExecution.Inspect(game.World, new Proxy(game, Fault.TwiceInspection)), "inspection cannot publish multiple sets");
            c.Equal(hash, game.Hash(), "non-mutating false provider facts never alter actual gameplay state");
            foreach (var fault in new[] { Fault.CheckMutates, Fault.SpendWrongCash, Fault.ReleaseWrongCash })
            {
                var probe = New(WorkBudgetTests.Factory()); Post(probe); Reach(probe, () => Holds(probe).Count == 2, "actual native hold for mutating fault probe");
                var proxy = new Proxy(probe, fault); var receipts = Holds(probe);
                c.Throws<InvalidOperationException>(() =>
                {
                    if (fault == Fault.CheckMutates) MonetaryBudgetExecution.CheckSpend(probe.World, proxy, company, receipts);
                    else if (fault == Fault.SpendWrongCash) MonetaryBudgetExecution.Spend(probe.World, proxy, company, receipts, new Code("proof.budget-fault"));
                    else MonetaryBudgetExecution.Release(probe.World, proxy, company, receipts);
                }, "direct provider boundary refuses " + fault + "; no rollback or normal Update recovery is claimed");
            }
        }
    }
}
