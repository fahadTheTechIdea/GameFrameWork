using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    /// <summary>Player intent and detached observations; production, payments and time stay in the engine.</summary>
    public static class MonetaryGameModeTests
    {
        public static void Register() => TestRunner.Add("Game modes: survival physical work and independent money rules", Physical);
        private static SimGame New(GameFactory factory, bool manufacturing)
            => factory.NewGame(new NewGameRequest { Scenario = manufacturing ? "process" : "local-work", Seed = 37 });
        private static EntityId Find(SimGame game, string prototype)
        {
            var entities = new List<EntityView>(); game.Seat(0).View.CopyEntities(new PlayerId(0), entities);
            foreach (EntityView entity in entities) if (entity.Def == prototype) return entity.Id;
            throw new InvalidOperationException("missing authored entity " + prototype);
        }
        private static Fix Stock(SimGame game, EntityId entity, string resource)
            => game.World.Entities.Get(entity).Get<IInventory>().Amount(game.World.Content.ResourceId(resource));
        private static OrderView Order(SimGame game)
        {
            var orders = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
            if (orders.Count != 1) throw new InvalidOperationException("expected one work order");
            return orders[0];
        }
        private static ActivityNodeView Node(SimGame game)
        {
            var nodes = new List<ActivityNodeView>(); game.Seat(0).View.Feature<WorkView>().TryCopyNodes(Order(game).Id, nodes);
            return nodes[0];
        }
        private static WorkCostChoice PreviewCost(SimGame game, EntityId target)
        {
            var facts = new List<WorkRequirementView>(); game.Seat(0).View.Feature<WorkView>().PreviewOrderRequirements("settle", target, facts);
            foreach (WorkRequirementView fact in facts) if (fact.Requirement == "local.money") return fact.Result.Costs[0];
            throw new InvalidOperationException("missing monetary diagnostic");
        }
        private static TransformationSnapshot Process(SimGame game, EntityId machine)
        {
            game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(machine, new Code("transformation"), out var state);
            return state;
        }
        private static void Physical(Check c)
        {
            foreach (MonetaryCostCategory category in new[] { MonetaryCostCategory.Construction, MonetaryCostCategory.Manufacturing, MonetaryCostCategory.Running })
            {
                var factory = ResourceOnlyGameModeTests.Factory(false, category); var game = New(factory, false); EntityId target = Find(game, "local");
                ulong before = game.Hash(); WorkCostChoice cost = PreviewCost(game, target);
                c.Equal(category, cost.Category, "work preserves its explicit category");
                c.Equal(Fix.FromInt(7), cost.Amount, "disabled work retains the positive authored cost");
                c.Equal(MonetaryCostApplicability.DisabledByMode, cost.Applicability, "cost is inapplicable, not paid or free credit");
                c.That(!cost.Decision.EffectiveAmount.HasValue, "disabled cost has no payable amount");
                c.Equal(before, game.Hash(), "monetary readiness is pure");
                c.That(!game.World.Services.Has<IEconomy>() && !game.World.Services.Get<IBusinessModeRules>().MoneyEnabled, "dormant Tycoon owns native monetary content without accounts");
                c.That(TestKit.Order(game, 0, new PostOrderCommand { Plan = "settle", Target = target }).Accepted, "physical work admits without Tycoon");
                game.RunTicks(2); ActivityNodeView node = Node(game);
                c.Equal(2L, node.Progress.WholeTicks, "work still advances over real game time");
                c.Equal(Fix.FromInt(8), Stock(game, target, "kit"), "physical material input is consumed once");
                c.Equal(0, node.Costs.Count, "disabled costs create no spent or reserved binding");
                var twin = factory.Load(game.Save()); game.RunTicks(4); twin.RunTicks(4);
                c.Equal(0, game.Seat(0).View.Feature<WorkView>().ActiveOrders(target), "physical work finishes after its authored duration");
                c.Equal(game.Hash(), twin.Hash(), "disabled-cost progress saves and continues deterministically");

                factory = ResourceOnlyGameModeTests.Factory(false, category, money: true, costs: true, openingCash: 6);
                game = New(factory, false); target = Find(game, "local");
                c.Equal(MonetaryCostApplicability.Enabled, PreviewCost(game, target).Applicability, "cost category can opt in without Tycoon");
                TestKit.Order(game, 0, new PostOrderCommand { Plan = "settle", Target = target }); game.RunTicks(3);
                c.Equal(WorkSystem.BlockerCash, Node(game).Blocker, "enabled insufficient money blocks existing execution");
                c.Equal(WorkProgress.Zero, Node(game).Progress, "cash shortage cannot advance work");
                c.Equal(Fix.FromInt(10), Stock(game, target, "kit"), "cash refusal consumes no physical stock");
                c.Equal(Fix.FromInt(6), game.World.Services.Get<IEconomy>().Cash(new PlayerId(0)), "cash refusal spends nothing");
                twin = factory.Load(game.Save()); game.RunTicks(2); twin.RunTicks(2);
                c.Equal(game.Hash(), twin.Hash(), "cash-blocked physical work restores without fictional payment");

                factory = ResourceOnlyGameModeTests.Factory(false, category, money: true, costs: true, openingCash: 20);
                game = New(factory, false); target = Find(game, "local");
                TestKit.Order(game, 0, new PostOrderCommand { Plan = "settle", Target = target }); game.RunTicks(2);
                c.Equal(Fix.FromInt(13), game.World.Services.Get<IEconomy>().Cash(new PlayerId(0)), "enabled cost charges once at physical startup");
                c.That(Node(game).Costs.Count == 1 && Node(game).Costs[0].Spent, "node retains only an actual paid obligation");
                twin = factory.Load(game.Save()); game.RunTicks(4); twin.RunTicks(4);
                c.Equal(Fix.FromInt(13), game.World.Services.Get<IEconomy>().Cash(new PlayerId(0)), "later updates and load do not charge again");
                c.Equal(game.Hash(), twin.Hash(), "paid work and cost category survive save/load");
            }
            // Account presence alone cannot turn on construction costs.
            var accountsFactory = ResourceOnlyGameModeTests.Factory(false, MonetaryCostCategory.Construction, money: true, openingCash: 20);
            var accountsGame = New(accountsFactory, false); EntityId accountTarget = Find(accountsGame, "local");
            TestKit.Order(accountsGame, 0, new PostOrderCommand { Plan = "settle", Target = accountTarget }); accountsGame.RunTicks(6);
            c.Equal(Fix.FromInt(20), accountsGame.World.Services.Get<IEconomy>().Cash(new PlayerId(0)), "accounts-only work has no automatic monetary cost");
            c.Equal(Fix.FromInt(8), Stock(accountsGame, accountTarget, "kit"), "accounts-only work still consumes materials");
            foreach (bool pay in new[] { false, true })
            {
                var factory = ResourceOnlyGameModeTests.Factory(true, MonetaryCostCategory.Manufacturing, money: pay, costs: pay, openingCash: 20);
                var game = New(factory, true); EntityId machine = Find(game, "assembler"); var view = game.Seat(0).View.Feature<TransformationManagementView>();
                ulong before = game.Hash(); c.Equal(Code.None, view.ReadMonetaryCost(machine, new Code("transformation"), "build", out var cost), "recipe cost diagnostic is available");
                c.Equal(Fix.FromInt(7), cost.AuthoredAmount, "recipe cost remains authored positive");
                c.Equal(pay ? MonetaryCostApplicability.Enabled : MonetaryCostApplicability.DisabledByMode, cost.Applicability, "recipe follows manufacturing costs independently");
                c.Equal(before, game.Hash(), "recipe cost view is pure");
                var command = new EnqueueTransformationCommand { Transformer = machine, Capability = new Code("transformation"), Transformation = "build", Count = 1 };
                c.Equal(Code.None, game.Seat(0).Preview(command), "normal manufacturing preview does not require Tycoon");
                c.That(TestKit.Order(game, 0, command).Accepted, "manufacturing command uses the same engine path");
                c.Equal(Fix.FromInt(17), Stock(game, machine, "supply"), "real recipe stock enters WIP");
                c.That(!FindToken(game), "manufacturing does not create output instantly");
                if (pay) c.Equal(Fix.FromInt(13), game.World.Services.Get<IEconomy>().Cash(new PlayerId(0)), "manufacturing charges once at startup");
                else c.That(!game.World.Services.Has<IEconomy>(), "resource-only manufacturing composes no account");
                game.RunTicks(2); c.That(!FindToken(game), "machine remains incomplete before its duration");
                var twin = factory.Load(game.Save()); game.RunTicks(3); twin.RunTicks(3);
                c.That(FindToken(game), "existing machine completes physical entity manufacture");
                c.Equal(Fix.FromInt(18), Stock(game, machine, "supply"), "recipe conserves consumed and produced material");
                c.Equal(game.Hash(), twin.Hash(), "manufacturing monetary decision saves with the same mode and WIP");
                if (pay) c.Equal(Fix.FromInt(13), game.World.Services.Get<IEconomy>().Cash(new PlayerId(0)), "recipe completion never repeats payment");
                c.That(game.Fault == null, "physical manufacturing remains healthy");
            }
            var blockedFactory = ResourceOnlyGameModeTests.Factory(true, MonetaryCostCategory.Manufacturing, money: true, costs: true, openingCash: 6);
            var blocked = New(blockedFactory, true); EntityId blockedMachine = Find(blocked, "assembler");
            TestKit.Order(blocked, 0, new EnqueueTransformationCommand { Transformer = blockedMachine, Capability = new Code("transformation"), Transformation = "build", Count = 1 }); blocked.RunTicks(3);
            c.Equal(WorkSystem.BlockerCash, Process(blocked, blockedMachine).Blocker, "enabled recipe waits for money");
            c.Equal(Fix.FromInt(20), Stock(blocked, blockedMachine, "supply"), "cash-blocked recipe acquires no WIP input");
            c.That(!FindToken(blocked), "cash shortage creates no machine output");
            var blockedTwin = blockedFactory.Load(blocked.Save()); blocked.RunTicks(2); blockedTwin.RunTicks(2);
            c.Equal(blocked.Hash(), blockedTwin.Hash(), "recipe shortage saves and waits consistently");
            var shortageFactory = ResourceOnlyGameModeTests.Factory(false, MonetaryCostCategory.Construction,
                edit: root => root.Get("entities")[0].Get("components").Get("local_stock").Set("initial", Json.Parse("1")));
            var shortage = New(shortageFactory, false); EntityId shortageTarget = Find(shortage, "local");
            TestKit.Order(shortage, 0, new PostOrderCommand { Plan = "settle", Target = shortageTarget }); shortage.RunTicks(3);
            c.Equal(WorkProgress.Zero, Node(shortage).Progress, "money-off cannot bypass an actual material shortage");
            c.Equal(Fix.One, Stock(shortage, shortageTarget, "kit"), "missing materials do not become synthetic stock");
            c.Throws<ContentException>(() => New(ResourceOnlyGameModeTests.Factory(false, MonetaryCostCategory.Construction,
                edit: root => root.Get("work_cost_requirements")[0].Remove("category")), false), "unclassified native cost refuses instead of guessing construction");
            c.Throws<ContentException>(() => New(ResourceOnlyGameModeTests.Factory(false, MonetaryCostCategory.Construction,
                edit: root => root.Get("work_cost_requirements")[0].Set("category", JsonValue.Of("tycoon"))), false), "unsupported category refuses");
        }
        private static bool FindToken(SimGame game)
        {
            var entities = new List<EntityView>(); game.Seat(0).View.CopyEntities(new PlayerId(0), entities);
            foreach (EntityView entity in entities) if (entity.Def == "token") return true;
            return false;
        }
    }
}
