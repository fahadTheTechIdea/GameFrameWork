using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    /// <summary>Authored configurations and named contract probes, not a test-owned trading implementation.</summary>
    public static class PurchasingTests
    {
        public static void Register() => TestRunner.Add("Tycoon purchasing: sole settlement and aggregate physical admission", Purchasing);
        internal static GameFactory Factory(string mode = "purchasing", int opening = 100, bool fault = false)
        {
            var version = new ModuleVersion(1, 0, 0);
            var definition = new GameDefinition("purchasing-proof", new GameModule[]
            {
                new ContentAccessModule(), new InventoryModule(), new MotionModule(), new WorkModule(), new WorldSetupModule(), new LogisticsModule(),
                new LocalWorkProofModule(), new TycoonModule(), new ProbeModule("proof.modes", version),
                new ProbeModule("proof.purchases", version, content: types => types.Component<FiniteSupplierDef>("purchase_finite", 4))
            });
            JsonValue content = Json.Parse(FrameworkProofContent.Read("local-work.json"));
            JsonValue scenario = content.Get("scenarios")[0];
            scenario.Set("game_mode", JsonValue.Of(mode)); scenario.Set("seats", Json.Parse("2"));
            scenario.Set("modules", Json.Parse("[\"proof.local-work\",\"proof.modes\",\"proof.purchases\",\"sim.tycoon\",\"sim.logistics\",\"sim.world-setup\"]"));
            scenario.Get("settings").Get(WorldSetupModule.ModuleId).Set("profile", JsonValue.Of("purchasing.start"));
            scenario.Get("settings").Set("sim.tycoon", Json.Parse("{\"account_policy\":\"purchase.accounts\"}"));
            JsonValue economic = Json.Parse(FrameworkProofContent.Read("purchasing.json"));
            economic.Get("business_accounts")[0].Set("opening_cash", Json.Parse(opening.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            if (fault) economic.Get("entities")[2].Get("components").Get("purchase_finite").Set("receipt_fault", JsonValue.Of("no_stock_delta"));
            JsonValue manifest = Json.Parse(FrameworkProofContent.Read("local-work-manifest.json"));
            manifest.Get("modules").Add(Json.Parse("{\"id\":\"proof.modes\",\"version\":\"1.0.0\",\"documents\":[\"game-mode-properties.json\",\"purchasing-modes.json\"]}"));
            manifest.Get("modules").Add(Json.Parse("{\"id\":\"proof.purchases\",\"version\":\"1.0.0\",\"documents\":[]}"));
            manifest.Get("modules").Add(Json.Parse("{\"id\":\"sim.tycoon\",\"version\":\"1.0.0\",\"documents\":[\"purchasing.json\"]}"));
            manifest.Get("modules").Add(Json.Parse("{\"id\":\"sim.logistics\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}"));
            var files = new PreloadedFileSource().Add("manifest.json", manifest.ToString()).Add("local-work.json", content.ToString())
                .Add("framework/access.json", FrameworkProofContent.Access).Add("work-retry.json", FrameworkProofContent.Read("work-retry.json"))
                .Add("game-mode-properties.json", FrameworkProofContent.Read("game-mode-properties.json"))
                .Add("purchasing-modes.json", FrameworkProofContent.Read("purchasing-modes.json")).Add("purchasing.json", economic.ToString());
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        internal static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "local-work", Seed = 53 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human);
            return factory.NewGame(request);
        }
        private static Entity Find(SimGame game, string prototype)
        { foreach (Entity entity in game.World.Entities.All) if (entity.Def.Id == prototype) return entity; throw new InvalidOperationException("missing " + prototype); }
        private static IResourceTransaction Transactions(SimGame game) => game.World.Services.Get<IResourceTransaction>();
        private static Fix Cash(SimGame game, int seat = 0) => game.World.Services.Get<IEconomy>().Cash(new PlayerId(seat));
        private static Fix Stock(SimGame game, string prototype) => Find(game, prototype).Get<IInventory>().Amount(game.World.Content.ResourceId("kit"));
        // Named receipt-contract instrumentation. Reservation books and all purchase execution remain in the engine.
        private static ResourceTransferIntent Supply(SimGame game, string vendor, string destination, int quantity, long identity, int seat = 0, int? reserve = null)
        {
            var owner = new ReservationOwner("proof.purchase", identity);
            ResourceId resource = game.World.Content.ResourceId("kit"); Fix amount = Fix.FromInt(reserve ?? quantity);
            ISupplier supplier = Find(game, vendor).Get<ISupplier>(); IInventory inlet = Find(game, destination).Get<IInventory>();
            return new ResourceTransferIntent(owner, new PlayerId(seat), supplier.ReserveSupply(owner, resource, amount),
                inlet.ReserveIn(owner, resource, amount), resource, Fix.FromInt(quantity), ResourceTransferKind.Supply);
        }
        private static ResourceTransferIntent Remainder(ResourceTransferIntent intent, int quantity)
            => new ResourceTransferIntent(intent.Owner, intent.Company, intent.Source, intent.Destination, intent.Resource, Fix.FromInt(quantity), intent.Kind);
        private static void Purchasing(Check c)
        {
            var factory = Factory(); var game = New(factory); World world = game.World;
            var accounts = world.Services.Get<IBusinessAccounts>(); var settlement = world.Services.Get<IResourcePurchaseSettlement>();
            c.That(ReferenceEquals(accounts, settlement) && ReferenceEquals(accounts, world.Services.Get<IEconomy>()), "one account owner is also the sole purchase settlement");
            c.Equal(TycoonModule.ModuleId, world.Services.OwnerOf(settlement), "purchasing belongs to the optional Tycoon module");
            c.That(ReferenceEquals(world.Services.Get<IBusinessModeRules>(), world.Services.Get<IResourceTradingRules>()), "immutable rules have one owner, not copied gates");
            c.That(game.Seat(0).View.Feature<BusinessView>().HasPurchasing, "immutable view reports installed purchase capability");
            c.That(!world.Services.Get<IBusinessModeRules>().TycoonEnabled && !world.Services.Has<IResourceValuation>(), "fixed supplier purchasing requires no advanced Tycoon or resource valuation");
            ResourceTransferIntent a = Supply(game, "vendor-a", "local", 4, 1), b = Supply(game, "vendor-b", "receiver", 3, 2);
            ulong before = game.Hash(); long time = game.Tick;
            c.That(Transactions(game).EvaluateTransfers(new[] { b, a }).Ready, "complete aggregate purchase preview is ready");
            c.Equal(before, game.Hash(), "pricing and aggregate affordability previews mutate no stock, receipts or money");
            c.Equal(time, game.Tick, "contract preview advances no game clock");
            c.Throws<InvalidOperationException>(() => Find(game, "vendor-a").Get<ISupplier>().SupplyReserved(world, new PlayerId(0), a.Owner, a.Source, a.Amount, null), "raw supplier callback is not an alternative purchase API");
            c.Equal(before, game.Hash(), "missing execution scope creates neither free goods nor a charge");
            var twin = factory.Load(game.Save());
            c.That(Transactions(game).CommitTransfers(new[] { b, a }).Committed && Transactions(twin).CommitTransfers(new[] { a, b }).Committed, "shuffled supply legs use the same complete settlement");
            c.Equal(Fix.FromInt(76), Cash(game), "exact combined24 is charged once");
            var spending = new List<BusinessExpenseSnapshot>();
            c.Equal(Code.None, game.Seat(0).View.Feature<BusinessView>().CopySpending(new PlayerId(0), spending), "committed physical transfers publish native account spending");
            c.Equal((System.Numerics.BigInteger)Fix.FromInt(24).Raw, spending.Single().AmountRaw, "aggregate physical purchase records24 once");
            c.Equal(BusinessModeRules.LedgerPurchase, spending.Single().Reason, "purchase uses original ledger reason");
            c.Equal(Fix.FromInt(14), Stock(game, "local"), "first actual destination receives4");
            c.Equal(Fix.FromInt(3), Stock(game, "receiver"), "second actual destination receives3");
            c.Equal(game.Hash(), twin.Hash(), "caller ordering does not change saved physical/account state");
            before = game.Hash(); c.Equal(ResourceTransferStatus.InvalidReceipt, Transactions(game).Commit(a).Status, "spent receipt cannot repurchase or charge again");
            c.Equal(before, game.Hash(), "duplicate commit leaves all authoritative state unchanged");
            var restored = factory.Load(game.Save()); game.RunTicks(2); restored.RunTicks(2);
            c.Equal(game.Hash(), restored.Hash(), "restoring/idle continuation never repeats purchase settlement");
            c.Equal(Fix.FromInt(76), Cash(restored), "restored purchase balance remains exact");
            Shortage(c); PartialAndFinite(c); Modes(c); AccountBatches(c); Physical(c); PricingRange(c); ProviderFault(c);
        }
        private static void Shortage(Check c)
        {
            var game = New(Factory(opening: 20)); var a = Supply(game, "vendor-a", "local", 4, 1); var b = Supply(game, "vendor-b", "receiver", 3, 2);
            c.That(Transactions(game).Evaluate(a).Ready && Transactions(game).Evaluate(b).Ready, "each individual12 is affordable");
            ulong before = game.Hash();
            c.Equal(ResourceTransferStatus.NoPayment, Transactions(game).EvaluateTransfers(new[] { a, b }).Status, "combined24 refuses against20 before mutation");
            c.Equal(ResourceTransferStatus.NoPayment, Transactions(game).CommitTransfers(new[] { b, a }).Status, "actual commit repeats whole-set affordability");
            c.Equal(before, game.Hash(), "unaffordable aggregate preserves every receipt, resource and balance");
            c.Equal(Fix.FromInt(10), Stock(game, "local"), "first leg cannot partially create cargo");
            c.Equal(Fix.Zero, Stock(game, "receiver"), "second leg cannot receive fabricated cargo");
            c.Equal(Fix.FromInt(20), Cash(game), "refused aggregate charges nothing");
        }
        private static void PartialAndFinite(Check c)
        {
            var factory = Factory(); var game = New(factory);
            var partial = Supply(game, "vendor-a", "receiver", 2, 3, reserve: 5); var twin = factory.Load(game.Save());
            c.That(Transactions(game).Commit(partial).Committed && Transactions(twin).Commit(partial).Committed, "bounded fulfillment spends only actual2");
            c.Equal(Fix.FromInt(94), Cash(game), "partial purchase charges6, not the outstanding15");
            c.Equal(Fix.FromInt(3), Find(game, "vendor-a").Get<ISupplier>().Reservations[0].Amount, "same source token retains3");
            c.Equal(Fix.FromInt(3), Find(game, "receiver").Get<IInventory>().Reservations[0].Amount, "same inlet token retains3");
            c.That(Transactions(game).Commit(Remainder(partial, 3)).Committed && Transactions(twin).Commit(Remainder(partial, 3)).Committed, "unspent remainder is purchased through the same owner");
            c.Equal(Fix.FromInt(85), Cash(game), "remainder charges exact9");
            c.Equal(game.Hash(), twin.Hash(), "partial saved continuation stays identical");
            var finite = Supply(game, "finite-vendor", "receiver", 4, 4);
            c.That(Transactions(game).Commit(finite).Committed, "public-only finite supplier uses the native account settlement contract");
            c.Equal(Fix.FromInt(6), Stock(game, "finite-vendor"), "finite purchase consumes real source stock");
            c.Equal(Fix.FromInt(9), Stock(game, "receiver"), "destination holds actual partial and finite receipts");
            c.Equal(Fix.FromInt(77), Cash(game), "finite provider does not contain its own ledger or double charge");
        }
        private static void Modes(Check c)
        {
            foreach (string mode in new[] { "purchase-disabled", "purchase-dormant" })
            {
                var factory = Factory(mode); var game = New(factory); var intent = Supply(game, "vendor-a", "receiver", 1, 5);
                ulong before = game.Hash();
                c.That(!game.World.Services.Has<IResourcePurchaseSettlement>(), "off modes install no payment participant");
                c.Equal(ResourceTransferStatus.TradingDisabled, Transactions(game).Evaluate(intent).Status, "same optional rules refuse receipt admission");
                c.Equal(ResourceTransferStatus.TradingDisabled, Transactions(game).Commit(intent).Status, "off mode cannot create free vendor goods");
                var preview = game.World.Services.Get<IHaulExecution>().PreviewHaul(new PlayerId(0), Find(game, "vendor-a").Id, Find(game, "receiver").Id, intent.Resource, Fix.One, purchase: true);
                c.Equal(HaulStatus.PolicyBlocked, preview.Status, "haul requests consult the same optional admission contract");
                c.Equal(ResourceTransactions.TradingDisabled, preview.Blocker, "haul and direct transaction agree on disabled trading");
                c.Equal(before, game.Hash(), "all refused entry points preserve engine state");
                c.That((mode == "purchase-disabled") == game.World.Services.Has<IEconomy>(), "dormant vendors require no fake accounts");
                var saved = factory.Load(game.Save()); game.RunTicks(1); saved.RunTicks(1);
                c.Equal(game.Hash(), saved.Hash(), "disabled purchase promises save without creating obligations or payments");
            }
        }
        private static void AccountBatches(Check c)
        {
            var game = New(Factory(opening: 20)); var a = Supply(game, "vendor-a", "receiver", 4, 6); var b = Supply(game, "vendor-b", "other-receiver", 3, 7, seat: 1);
            c.That(Transactions(game).CommitTransfers(new[] { b, a }).Committed, "purchase aggregation is per actual buyer account");
            c.Equal(Fix.FromInt(8), Cash(game), "first account pays only its own12"); c.Equal(Fix.FromInt(8), Cash(game, 1), "second account pays only its own12");
            game.World.Services.Get<IEconomy>().Charge(new PlayerId(1), Fix.FromInt(9), new Code("proof.debt"));
            c.That(Transactions(game).Commit(Supply(game, "vendor-a", "receiver", 1, 8)).Committed, "unrelated indebted account cannot block an affordable purchase");
            c.Equal(Fix.FromInt(5), Cash(game), "active account pays3"); c.Equal(Fix.FromInt(-1), Cash(game, 1), "unrelated debt is untouched");
        }
        private static void Physical(Check c)
        {
            var factory = ResourceOnlyGameModeTests.Factory(false); var game = factory.NewGame(new NewGameRequest { Scenario = "local-work", Seed = 53 });
            c.That(!game.World.Services.Has<IResourceTradingRules>() && !game.World.Services.Has<IResourcePurchaseSettlement>() && !game.World.Services.Has<IEconomy>(), "resource-only engine has no economic gate, settlement or fake ledger");
            c.That(TestKit.Order(game, 0, new PostOrderCommand { Plan = "settle", Target = Find(game, "local").Id }).Accepted, "same public physical work still runs without Tycoon");
            var twin = factory.Load(game.Save()); game.RunTicks(6); twin.RunTicks(6);
            c.Equal(Fix.FromInt(8), Stock(game, "local"), "resource-only work consumes real materials through game updates");
            c.Equal(game.Hash(), twin.Hash(), "economic contract changes preserve resource-only saved continuation");
        }
        private static void PricingRange(Check c)
        {
            var game = New(Factory()); IResourcePurchaseSettlement settlement = game.World.Services.Get<IResourcePurchaseSettlement>();
            var price = (IResourceSupplyPrice)Find(game, "vendor-a").Get<ISupplier>(); ulong before = game.Hash();
            c.Equal(ResourceTransferStatus.InvalidRequest, settlement.CheckPurchases(null), "missing purchase vector refuses explicitly");
            c.Equal(ResourceTransferStatus.InvalidRequest, settlement.CheckPurchases(new[] { new ResourcePurchaseIntent(PlayerId.None, game.World.Content.ResourceId("kit"), Fix.One, price) }), "neutral provider does not create a fictitious buyer account");
            c.Equal(ResourceTransferStatus.SettlementRefused, settlement.CheckPurchases(new[] { new ResourcePurchaseIntent(new PlayerId(0), game.World.Content.ResourceId("kit"), Fix.FromInt(1000000000), price) }), "unrepresentable whole purchase cost refuses before physical execution");
            c.Equal(before, game.Hash(), "invalid and range admissions mutate no account state");
            var intent = new ResourcePurchaseIntent(new PlayerId(0), game.World.Content.ResourceId("kit"), Fix.One, price);
            c.Equal(ResourceTransferStatus.Ready, settlement.PreparePurchases(new[] { intent }, out var commit), "named account-commit contract probe prepares exact3");
            game.World.Services.Get<IEconomy>().Charge(new PlayerId(1), Fix.One, new Code("proof.interleave")); before = game.Hash();
            c.Throws<InvalidOperationException>(() => commit.Commit(), "account mutation makes synchronous prepared settlement stale");
            c.Equal(before, game.Hash(), "stale settlement checks the complete account vector before charging anyone");
        }
        private static void ProviderFault(Check c)
        {
            var game = New(Factory(fault: true)); var intent = Supply(game, "finite-vendor", "receiver", 1, 9);
            c.Throws<InvalidOperationException>(() => Transactions(game).Commit(intent), "provider lying about stock consumption faults exact physical commit");
            c.Equal(Fix.FromInt(100), Cash(game), "unverified physical fulfillment never settles payment");
            c.Equal(Fix.Zero, Stock(game, "receiver"), "failure occurs before destination receives goods");
            // Direct framework instrumentation, not a normal Update/save claim or recovery acceptance.
        }
    }
}
