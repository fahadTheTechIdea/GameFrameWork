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
    /// <summary>Player intent/observations plus explicitly named ownership and receipt-contract probes.</summary>
    public static class TycoonOwnershipTests
    {
        public static void Register() => TestRunner.Add("Tycoon module: one economic owner and independent physical games", Ownership);
        private static SimGame New(GameFactory factory, string scenario)
            => factory.NewGame(new NewGameRequest { Scenario = scenario, Seed = 41 });
        private static Entity Find(SimGame game, string prototype)
        {
            foreach (Entity entity in game.World.Entities.All) if (entity.Def.Id == prototype) return entity;
            throw new InvalidOperationException("missing authored entity " + prototype);
        }
        private static bool Selected(SimGame game, string id)
        { foreach (string module in game.Modules) if (module == id) return true; return false; }
        private static void Ownership(Check c)
        {
            var definition = new GameDefinition("tycoon-owner", new GameModule[]
            { new ContentAccessModule(), new InventoryModule(), new MotionModule(), new WorkModule(), new LogisticsModule(), new TycoonModule() });
            foreach (string key in new[] { "fixed_resource_values", "resource_values", "business_accounts", "primary_production_income", "work_cost_requirements" })
            {
                int found = 0;
                foreach (var section in definition.ContentTypes.Sections)
                    if (section.Name == key) { found++; c.Equal(TycoonModule.ModuleId, section.Owner, key + " has one Tycoon owner"); }
                c.Equal(1, found, key + " has no duplicate declaration");
            }
            c.That(definition.ContentTypes.TryGetComponent("supplier", out var supplierType), "native supplier has one declared component identity");
            c.Equal(TycoonModule.ModuleId, supplierType.Owner, "native vendor pricing/receipts belong to Tycoon, not Logistics");
            foreach (var requirement in new WorkModule().Requires) c.That(requirement.Id != TycoonModule.ModuleId, "Work does not require economics");
            foreach (var requirement in new LogisticsModule().Requires) c.That(requirement.Id != TycoonModule.ModuleId, "Logistics does not require economics");
            var assembly = typeof(TycoonModule).Assembly;
            c.That(assembly.GetType("Beep.SimEngine.Standard.BusinessModule") == null, "old module class is removed, not an alias");
            c.That(assembly.GetType("Beep.SimEngine.Standard.FreeEconomy") == null, "fake money-free account provider is removed");
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current runtime identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "direct module/status migration has no old save reader");

            var factory = BusinessDefaultsTests.Factory(); var valued = New(factory, "default-business");
            var accounts = valued.World.Services.Get<IBusinessAccounts>();
            c.That(ReferenceEquals(accounts, valued.World.Services.Get<IEconomy>()), "account aliases share the existing sole mutable owner");
            c.Equal(TycoonModule.ModuleId, valued.World.Services.OwnerOf(accounts), "Tycoon owns the actual account service");
            c.Equal(1, valued.World.Services.Persistent().Count, "basic economy creates one account codec only");
            c.Equal("sim.tycoon-accounts", valued.World.Services.Persistent()[0].Metadata.Id, "account codec uses the new native identity");
            c.Equal(TycoonModule.ModuleId, valued.World.Services.Persistent()[0].Metadata.Owner, "codec ownership matches the economic module");
            c.Equal(Fix.FromInt(100000), valued.Seat(0).View.Feature<BusinessView>().Quote(valued.World.Content.ResourceId("gold"), Fix.One).Value,
                "basic authored gold value remains100000 without advanced Tycoon activation");
            c.That(!valued.World.Services.Get<IBusinessModeRules>().TycoonEnabled, "selecting TycoonModule does not activate advanced rules");
            c.Equal(0, valued.World.Systems.Count, "fixed values/accounts install no second loop or market system");
            var valuedTwin = factory.Load(valued.Save()); valued.RunTicks(2); valuedTwin.RunTicks(2);
            c.Equal(valued.Hash(), valuedTwin.Hash(), "new module/account identity saves and continues deterministically");
            var valuesOnly = New(factory, "values-only");
            c.That(valuesOnly.World.Services.Has<IResourceValuation>() && !valuesOnly.World.Services.Has<IEconomy>(), "values alone require no money");
            var accountsOnly = New(factory, "accounts-only");
            c.That(accountsOnly.World.Services.Has<IEconomy>() && !accountsOnly.World.Services.Has<IResourceValuation>(), "money alone requires no valuation");
            var dormant = New(factory, "dormant-business");
            c.That(Selected(dormant, TycoonModule.ModuleId) && !dormant.World.Services.Has<IEconomy>() && !dormant.World.Services.Has<IResourceValuation>(),
                "selected all-off Tycoon is dormant, not unlimited accounts");
            c.Equal(0, dormant.World.Services.Persistent().Count, "dormant rules create no account state");
            c.Equal(ResourceTransferStatus.TradingDisabled, dormant.World.Services.Get<IResourceTradingRules>().CheckTrading(), "dormant trading is explicitly disabled");
            var survival = New(factory, "survival");
            c.That(!Selected(survival, TycoonModule.ModuleId) && !survival.World.Services.Has<IBusinessModeRules>(), "survival omits the economic module entirely");
            c.That(!survival.World.Services.Has<IResourceTradingRules>(), "module absence cannot grant free supplier purchases");

            Physical(c, false); Physical(c, true);
            DormantPurchases(c, false); DormantPurchases(c, true);
            c.Throws<ContentException>(() => New(ResourceOnlyGameModeTests.Factory(false, MonetaryCostCategory.Construction,
                edit: root => { var scenario = root.Get("scenarios")[0]; var selected = JsonValue.NewArray(); var authored = scenario.Get("modules"); for (int i = 0; i < authored.Count; i++) if (authored[i].Text != TycoonModule.ModuleId) selected.Add(authored[i].Clone()); scenario.Set("modules", selected); }), "local-work"),
                "native monetary content requires its declared owner even when payment is disabled");
        }
        private static void Physical(Check c, bool manufacture)
        {
            var factory = ResourceOnlyGameModeTests.Factory(manufacture); string scenario = manufacture ? "process" : "local-work";
            var game = New(factory, scenario); Entity machine = Find(game, manufacture ? "assembler" : "local");
            c.That(!Selected(game, TycoonModule.ModuleId) && !game.World.Services.Has<IEconomy>() && !game.World.Services.Has<IBusinessModeRules>(),
                "unpriced physical game has neither Tycoon nor fake accounts");
            if (manufacture)
            {
                c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Transformer = machine.Id, Capability = new Code("transformation"), Transformation = "build", Count = 1 }).Accepted,
                    "ordinary manufacturing command works without Tycoon");
                c.Equal(Fix.FromInt(17), machine.Get<IInventory>().Amount(game.World.Content.ResourceId("supply")), "real recipe inputs enter WIP");
            }
            else
            {
                c.That(TestKit.Order(game, 0, new PostOrderCommand { Plan = "settle", Target = machine.Id }).Accepted, "ordinary work command works without Tycoon");
                c.Equal(Fix.FromInt(10), machine.Get<IInventory>().Amount(game.World.Content.ResourceId("kit")), "queueing does not bypass productive material timing");
            }
            game.RunTicks(2);
            if (manufacture)
            {
                int count = 0; foreach (Entity entity in game.World.Entities.All) if (entity.Def.Id == "token") count++;
                c.Equal(0, count, "manufacturing output waits for real elapsed game time");
            }
            else
            {
                var orders = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(orders);
                c.Equal(2L, orders[0].CurrentProgress.WholeTicks, "existing engine update advances timed work");
                c.Equal(Fix.FromInt(8), machine.Get<IInventory>().Amount(game.World.Content.ResourceId("kit")), "physical work consumes actual materials");
            }
            var twin = factory.Load(game.Save()); game.RunTicks(manufacture ? 3 : 4); twin.RunTicks(manufacture ? 3 : 4);
            c.Equal(game.Hash(), twin.Hash(), "resource-only physical workflow saves and continues identically");
            if (manufacture)
            {
                int count = 0; foreach (Entity entity in game.World.Entities.All) if (entity.Def.Id == "token") count++;
                c.Equal(1, count, "existing engine produces the machine output after its duration");
                c.Equal(Fix.FromInt(18), machine.Get<IInventory>().Amount(game.World.Content.ResourceId("supply")), "physical recipe output conserves authored quantities");
            }
            else c.Equal(0, game.Seat(0).View.Feature<WorkView>().ActiveOrders(machine.Id), "timed work completes without monetary capabilities");
            c.That(game.Fault == null && twin.Fault == null, "neither physical session consults absent accounts");
        }
        private static void DormantPurchases(Check c, bool money)
        {
            var factory = ResourceOnlyGameModeTests.Factory(false, MonetaryCostCategory.Construction, money: money, openingCash: 20, logistics: true,
                edit: root =>
                {
                    root.Get("entities").Add(Json.Parse("{\"id\":\"vendor\",\"kind\":\"entity\",\"components\":{\"supplier\":{\"sells\":{\"kit\":3}},\"local_stock\":{\"resource\":\"kit\",\"capacity_policy\":\"fixture.count.20\",\"initial\":0}}}"));
                    root.Get("entity_setups").Add(Json.Parse("{\"id\":\"vendor-start\",\"after\":[],\"entity\":\"vendor\",\"company\":0,\"placement\":null,\"home\":null}"));
                    root.Get("world_setup_profiles")[0].Get("entries").Add(JsonValue.Of("vendor-start"));
                });
            var game = New(factory, "local-work"); World world = game.World;
            Entity vendor = Find(game, "vendor"), target = Find(game, "local"); ResourceId kit = world.Content.ResourceId("kit");
            ISupplier supply = vendor.Get<ISupplier>(); IInventory destination = target.Get<IInventory>();
            ulong before = game.Hash(); long time = game.Tick;
            var haul = world.Services.Get<IHaulExecution>().PreviewHaul(new PlayerId(0), vendor.Id, target.Id, kit, Fix.One, purchase: true);
            c.Equal(HaulStatus.PolicyBlocked, haul.Status, "purchase haul refuses even when the buyer has accounts");
            c.Equal(ResourceTransactions.TradingDisabled, haul.Blocker, "haul reports the shared typed trading blocker");
            c.Equal(before, game.Hash(), "refused haul reserves no transporter, endpoint or cargo");
            c.Equal(time, game.Tick, "purchase preview advances no game time");

            // Explicit receipt-contract instrumentation: a direct caller cannot bypass haul admission.
            var owner = new ReservationOwner("proof.tycoon-receipt", 1);
            ReservationToken outgoing = supply.ReserveSupply(owner, kit, Fix.One), incoming = destination.ReserveIn(owner, kit, Fix.One);
            var intent = new ResourceTransferIntent(owner, new PlayerId(0), outgoing, incoming, kit, Fix.One, ResourceTransferKind.Supply);
            IResourceTransaction transaction = world.Services.Get<IResourceTransaction>(); before = game.Hash();
            c.That(supply.CheckSupply(world, new PlayerId(0), owner, outgoing, Fix.One).Ready, "physical endpoint does not own economic admission");
            c.Equal(ResourceTransferStatus.TradingDisabled, transaction.Evaluate(intent).Status, "direct transaction preflight cannot bypass disabled trading");
            c.Equal(ResourceTransferStatus.TradingDisabled, transaction.Commit(intent).Status, "direct commit cannot create free vendor goods");
            c.Throws<InvalidOperationException>(() => supply.SupplyReserved(world, new PlayerId(0), owner, outgoing, Fix.One, null), "direct vendor fulfillment requires the transaction owner");
            c.Equal(before, game.Hash(), "refusals preserve stock, receipts, journal and money");
            c.Equal(Fix.FromInt(10), destination.Amount(kit), "disabled vendor purchase creates no resources");
            c.That(money == world.Services.Has<IEconomy>(), "dormant priced vendors do not require or fabricate accounts");
            if (money) c.Equal(Fix.FromInt(20), world.Services.Get<IEconomy>().Cash(new PlayerId(0)), "accounts-only mode makes no purchase payment");
            supply.ReleaseSupply(owner, outgoing); destination.ReleaseIn(owner, incoming);
            var twin = factory.Load(game.Save()); game.RunTicks(2); twin.RunTicks(2);
            c.Equal(game.Hash(), twin.Hash(), "dormant native monetary/supplier content saves under its actual owner");
            c.That(game.Fault == null && twin.Fault == null, "dormant supplier content updates without absent-ledger access");
        }
    }
}
