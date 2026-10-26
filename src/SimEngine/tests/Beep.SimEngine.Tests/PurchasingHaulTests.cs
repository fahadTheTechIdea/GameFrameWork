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
    /// <summary>Player commands, immutable observations and bounded Update drives only.
    /// Native modules own payment, income, stock matching, assignment, travel, handling and retry.</summary>
    public static partial class PurchasingHaulTests
    {
        public static void Register()
        {
            TestRunner.Add("Tycoon hauling: timed purchases, payment waits and physical continuation", Physical);
            TestRunner.Add(CommerceCase, c => ResourceCommerce(c, false));
            TestRunner.Add(NetworkCommerceCase, c => ResourceCommerce(c, true));
        }
        private static GameFactory Factory(string mode = "haul-economic", int opening = 100, bool finite = false, bool commerce = false)
        {
            var version = new ModuleVersion(1, 0, 0);
            var modules = new List<GameModule>
            {
                new ContentAccessModule(), new InventoryModule(), new OperationsModule(), new MotionModule(),
                new TransformationModule(), new WorkModule(), new LogisticsModule(), new WorldSetupModule(), new PointModule(),
                new TycoonModule(), new ProbeModule("proof.modes", version),
                new ProbeModule("proof.purchase-finite", version, content: types => types.Component<FiniteSupplierDef>("haul_finite", 4))
            };
            if (commerce) { modules.Add(new ResourceFlowsModule()); modules.Add(new InventorySaleEligibilityModule()); }
            var definition = new GameDefinition("purchase-haul-proof", modules);
            JsonValue content = Json.Parse(FrameworkProofContent.Read("purchasing-haul.json"));
            JsonValue scenario = content.Get("scenarios")[0]; scenario.Set("game_mode", JsonValue.Of(mode));
            JsonValue manifest = Json.Parse(FrameworkProofContent.Read("purchasing-haul-manifest.json"));
            manifest.Get("modules").Add(Json.Parse("{\"id\":\"proof.purchase-finite\",\"version\":\"1.0.0\",\"documents\":[]}"));
            if (commerce)
            {
                scenario.Set("seats", JsonValue.Number("2"));
                scenario.Get("modules").Add(JsonValue.Of("proof.resource-commerce"));
                manifest.Get("modules").Add(Json.Parse("{\"id\":\"sim.resource-flows\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}"));
                manifest.Get("modules").Add(Json.Parse("{\"id\":\"proof.resource-commerce\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.tycoon\",\"version\":\"1.0.0\"},{\"id\":\"sim.resource-flows\",\"version\":\"1.0.0\"}],\"documents\":[\"purchasing-resource-accounting.json\"]}"));
            }
            if (finite)
            {
                scenario.Get("modules").Add(JsonValue.Of("proof.purchase-finite")); scenario.Set("seats", Json.Parse("2"));
                content.Get("entity_setups")[0].Set("company", Json.Parse("1"));
                content.Get("entities")[0].Set("components", Json.Parse("{\"point_presence\":{\"space\":\"purchase.points\",\"position\":10},\"point_stock\":{\"resource\":\"supply\",\"capacity_policy\":\"purchase.store\",\"initial\":20},\"haul_finite\":{\"resource\":\"supply\",\"price\":5}}"));
            }
            if (mode == "haul-survival")
            {
                scenario.Set("modules", Json.Parse("[\"points\",\"proof.modes\",\"sim.world-setup\"]"));
                scenario.Set("settings", Json.Parse("{\"sim.work\":{\"retry\":\"proof.retry\"},\"sim.world-setup\":{\"profile\":\"purchasing-haul.start\"}}"));
                content.Get("entities")[0].Set("components", Json.Parse("{\"point_presence\":{\"space\":\"purchase.points\",\"position\":10},\"point_stock\":{\"resource\":\"supply\",\"capacity_policy\":\"purchase.store\",\"initial\":20}}"));
            }
            JsonValue economic = Json.Parse(FrameworkProofContent.Read("purchasing-haul-economics.json"));
            economic.Get("business_accounts")[0].Set("opening_cash", Json.Parse(opening.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            var files = new PreloadedFileSource().Add("manifest.json", manifest.ToString()).Add("purchasing-haul.json", content.ToString())
                .Add("purchasing-haul-economics.json", economic.ToString()).Add("purchasing-haul-modes.json", FrameworkProofContent.Read("purchasing-haul-modes.json"))
                .Add("game-mode-properties.json", FrameworkProofContent.Read("game-mode-properties.json"))
                .Add("framework/access.json", FrameworkProofContent.Access).Add("haul-retry.json", FrameworkProofContent.Read("haul-retry.json"))
                .Add("point-handling.json", FrameworkProofContent.Read("point-handling.json"));
            if (commerce) files.Add("purchasing-resource-accounting.json", FrameworkProofContent.Read("purchasing-resource-accounting.json"));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory, bool finite = false)
        {
            var request = new NewGameRequest { Scenario = "purchase-haul", Seed = 59 };
            request.Seats.Add(Controller.Human); if (finite) request.Seats.Add(Controller.Human);
            return factory.NewGame(request);
        }
        private static Entity Find(SimGame game, string prototype)
        { foreach (Entity entity in game.World.Entities.All) if (entity.Def.Id == prototype) return entity; throw new InvalidOperationException("missing " + prototype); }
        private static Fix Stock(SimGame game, string prototype, string resource = "supply")
            => Find(game, prototype).Get<IInventory>().Amount(game.World.Content.ResourceId(resource));
        private static Fix Position(SimGame game) => Find(game, "machine").Get<PointPresence>().Position;
        private static BusinessAccountSnapshot Account(SimGame game)
        {
            Code refusal = game.Seat(0).View.Feature<BusinessView>().ReadAccount(new PlayerId(0), out var account);
            if (!refusal.IsNone) throw new InvalidOperationException("account read refused: " + refusal); return account;
        }
        private static WorkJobView? Read(SimGame game)
        {
            var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
            foreach (WorkJobView job in jobs) if (job.IsHaul) return job; return null;
        }
        private static void Update(SimGame game, SimGame twin = null, int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                if (game.RunTicks(1) != 1) throw new InvalidOperationException("engine Update fault: " + game.Fault);
                if (twin != null && twin.RunTicks(1) != 1) throw new InvalidOperationException("saved twin Update fault: " + twin.Fault);
            }
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null, int limit = 240)
        {
            for (int i = 0; i < limit && !done(); i++) Update(game, twin);
            if (!done())
            {
                WorkJobView? job = Read(game);
                throw new InvalidOperationException(stage + " timed out at " + game.Tick + ", position=" + Position(game) + ", cargo=" + Stock(game, "machine") +
                    ", destination=" + Stock(game, "destination") + ", job=" + job?.Id + "/" + job?.State + "/" + job?.ToilIndex + "/" + job?.Blocker + "/retry=" + job?.RetryAt);
            }
        }
        private static void Demand(Check c, SimGame game)
            => c.That(TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = Find(game, "destination").Id, Resource = game.World.Content.ResourceId("supply"),
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(8), Fix.FromInt(8), Fix.FromInt(8), 100, LogisticsReserveAccess.Protected)) }).Accepted,
                "public player stock policy delegates matching and physical dispatch to engine");
        private static void Produce(Check c, SimGame game, string recipe)
            => c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Transformer = Find(game, "processor").Id, Capability = new Code("transformation"), Transformation = recipe, Count = 1 }).Accepted,
                "public player production command changes resources/money through existing engine execution");
        private static void Physical(Check c)
        {
            Baseline(c, false); Baseline(c, true); PaymentDuringTravel(c); BlockedAdmission(c); RoadInterruption(c); Survival(c);
        }
        private static void Baseline(Check c, bool finite)
        {
            var factory = Factory(finite: finite); var game = New(factory, finite);
            ulong before = game.Hash(); long time = game.Tick;
            HaulPreview preview = game.Seat(0).View.Feature<LogisticsView>().PreviewHaul(Find(game, "source").Id, Find(game, "destination").Id, game.World.Content.ResourceId("supply"), Fix.FromInt(8), purchase: true);
            c.Equal(HaulStatus.Ready, preview.Status, "native purchase preview includes actual carrier and paid source");
            c.Equal(Fix.FromInt(8), preview.Amount, "engine calculates native load size");
            c.That(preview.Travel?.RemainingTicks > 0, "engine forecasts real movement, separate from handling duration");
            c.Equal(before, game.Hash(), "movement/payment preview reserves or charges nothing"); c.Equal(time, game.Tick, "preview advances no game time");
            Demand(c, game);
            Reach(game, () => Read(game).HasValue && Position(game).IsPositive && Position(game) < Fix.FromInt(10), "travel to supplier");
            c.Equal(Fix.Zero, Stock(game, "machine"), "traveling carrier cannot receive goods before reaching supplier");
            c.Equal(Fix.FromInt(100), Account(game).Balance, "dispatch and travel do not charge purchase money");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "travel state and original promises restore exactly");
            Reach(game, () => Stock(game, "machine") == Fix.FromInt(2), "partial progressive loading", twin);
            c.Equal(Fix.FromInt(90), Account(game).Balance, "two actual loaded units charge10 exactly");
            c.Equal(Fix.Zero, Stock(game, "destination"), "paid loading is not instant destination delivery");
            c.That(Read(game)?.HandlingProgress.WholeTicks > 0, "engine owns elapsed loading progress");
            c.Equal(game.Hash(), twin.Hash(), "saved travel continues through native pricing and partial loading identically");
            twin = factory.Load(game.Save());
            Reach(game, () => Position(game) > Fix.FromInt(10) && Position(game) < Fix.FromInt(20), "loaded travel to destination", twin);
            c.Equal(Fix.FromInt(8), Stock(game, "machine"), "real cargo stays on moving carrier"); c.Equal(Fix.FromInt(60), Account(game).Balance, "entire loaded purchase costs40");
            c.Equal(Fix.Zero, Stock(game, "destination"), "travel never teleports cargo");
            twin = factory.Load(game.Save()); Reach(game, () => Stock(game, "destination") == Fix.FromInt(2), "partial destination unloading", twin);
            c.Equal(Fix.FromInt(6), Stock(game, "machine"), "progressive unloading transfers only earned quantity");
            c.Equal(Fix.FromInt(60), Account(game).Balance, "destination stock transfer does not pay twice");
            twin = factory.Load(game.Save()); Reach(game, () => Stock(game, "destination") == Fix.FromInt(8) && !Read(game).HasValue, "complete purchase delivery", twin);
            c.Equal(Fix.Zero, Stock(game, "machine"), "delivery empties physical cargo"); c.Equal(Fix.FromInt(60), Account(game).Balance, "one purchase settlement per actual load, none per travel/unload/restore");
            c.Equal(0L, Account(game).GenerationSettlements, "buying goods does not count as primary generation income");
            if (finite) c.Equal(Fix.FromInt(12), Stock(game, "source"), "finite supplier loses only physically loaded8");
            c.Equal(game.Hash(), twin.Hash(), "save/load during travel/loading/unloading all continue identically");
            Update(game, twin, 10); c.Equal(Fix.FromInt(8), Stock(game, "destination"), "satisfied stock policy does not overbuy");
            c.Equal(Fix.FromInt(60), Account(game).Balance, "idle continuation creates no purchase charge"); c.Equal(game.Hash(), twin.Hash(), "completed delivery stays deterministic");
        }
        private static void PaymentDuringTravel(Check c)
        {
            var factory = Factory(); var game = New(factory); Demand(c, game);
            Reach(game, () => Read(game).HasValue && Position(game).IsPositive && Position(game) < Fix.FromInt(10), "funded purchase departs");
            Produce(c, game, "purchase.spend");
            c.Equal(Fix.FromInt(10), Account(game).Balance, "real manufacturing spends90 while purchase carrier travels");
            c.Equal(Fix.Zero, Stock(game, "machine"), "manufacturing does not grant transporter cargo");
            Reach(game, () => Read(game)?.Blocker == HaulHandlingCodes.Payment, "current funds block unpaid load remainder");
            WorkJobView wait = Read(game).Value;
            c.Equal(Fix.FromInt(2), wait.Loaded, "already paid2 remain owned cargo"); c.Equal(Fix.Zero, Account(game).Balance, "unfunded remainder creates no debt");
            c.Equal(Fix.FromInt(10), Position(game), "payment wait remains physically at supplier");
            c.That(wait.FailureCount > 0 && wait.RetryRemainingTicks > 0, "existing authored retry policy owns payment backoff");
            c.Equal(Fix.One, Stock(game, "processor", "support"), "actual paid manufacturing completed over time");
            c.Equal(0L, Account(game).GenerationSettlements, "noneligible manufacturing output earns no funding reward");
            var twin = factory.Load(game.Save()); Update(game, twin, 4);
            c.Equal(wait.Loaded, Read(game).Value.Loaded, "payment retries retain actual paid cargo");
            c.Equal(wait.HandlingProgress, Read(game).Value.HandlingProgress, "blocked retries earn no extra handling time");
            c.Equal(Fix.Zero, Account(game).Balance, "waiting never fabricates money"); c.Equal(game.Hash(), twin.Hash(), "saved physical payment wait is identical");
            Produce(c, game, "purchase.fund"); Produce(c, twin, "purchase.fund");
            c.Equal(Fix.Zero, Account(game).Balance, "queueing funding recipe is not instant income");
            Reach(game, () => Account(game).GenerationSettlements == 1, "physical funding production", twin);
            c.Equal(Fix.FromInt(7), Stock(game, "processor", "support"), "funding creates actual6 plus earlier manufactured1");
            Reach(game, () => Stock(game, "destination") == Fix.FromInt(8) && !Read(game).HasValue, "funded purchase resumes and delivers", twin);
            c.Equal(Fix.Zero, Account(game).Balance, "real production income30 pays the remaining6 once");
            c.Equal(Fix.Zero, Stock(game, "machine"), "funded continuation delivers rather than duplicating cargo");
            c.Equal(1L, Account(game).GenerationSettlements, "load/delivery never creates another generation reward");
            c.Equal(game.Hash(), twin.Hash(), "physical manufacture, income, retry and delivery use one saved engine state");
        }
        private static void BlockedAdmission(Check c)
        {
            var game = New(Factory(opening: 20)); ulong before = game.Hash();
            var preview = game.Seat(0).View.Feature<LogisticsView>().PreviewHaul(Find(game, "source").Id, Find(game, "destination").Id, game.World.Content.ResourceId("supply"), Fix.FromInt(8), purchase: true);
            c.Equal(HaulStatus.PolicyBlocked, preview.Status, "unaffordable full load is not advertised ready");
            c.Equal(HaulHandlingCodes.Payment, preview.Blocker, "purchase preview and live wait share the same typed payment blocker");
            c.Equal(before, game.Hash(), "shortage preview changes no account/worker/promise");
            Demand(c, game); Update(game, count: 20);
            c.That(!Read(game).HasValue, "engine creates no unaffordable physical job"); c.Equal(Fix.Zero, Stock(game, "machine"), "unfunded dispatch creates no goods");
            c.Equal(Fix.FromInt(20), Account(game).Balance, "unfunded stock policy never charges or grants funds");
            var offers = new List<LogisticsDispatchOffer>(); game.Seat(0).View.Feature<LogisticsView>().CopyQueue(offers);
            c.That(offers.Count > 0 && offers[0].Haul.Blocker == HaulHandlingCodes.Payment, "authoritative dispatch diagnostics retain economic shortage");
            var disabled = New(Factory("haul-disabled")); before = disabled.Hash();
            preview = disabled.Seat(0).View.Feature<LogisticsView>().PreviewHaul(Find(disabled, "source").Id, Find(disabled, "destination").Id, disabled.World.Content.ResourceId("supply"), Fix.FromInt(8), purchase: true);
            c.Equal(ResourceTransactions.TradingDisabled, preview.Blocker, "accounts alone cannot enable purchasing");
            c.Equal(before, disabled.Hash(), "disabled trading keeps actual account and physical state unchanged");
        }
        private static void RoadInterruption(Check c)
        {
            var factory = Factory(); var game = New(factory); Demand(c, game);
            Reach(game, () => Stock(game, "machine") == Fix.FromInt(8) && Position(game) > Fix.FromInt(10), "paid cargo enters route");
            c.That(TestKit.Order(game, 0, new SetPointAccessCommand { Open = false }).Accepted, "player closes actual route through spatial engine contract");
            Reach(game, () => Read(game)?.Blocker == WorkSystem.BlockerUnreachable, "broken route enters authored retry");
            Fix stopped = Position(game); var twin = factory.Load(game.Save()); Update(game, twin, 6);
            c.Equal(stopped, Position(game), "broken route stops actual vehicle"); c.Equal(Fix.FromInt(8), Stock(game, "machine"), "paid cargo survives broken route and retry");
            c.Equal(Fix.FromInt(60), Account(game).Balance, "route retries do not repurchase cargo");
            c.Equal(game.Hash(), twin.Hash(), "broken route, cargo promises and wait save consistently");
            c.That(TestKit.Order(game, 0, new SetPointAccessCommand { Open = true }).Accepted, "player restores route access");
            c.That(TestKit.Order(twin, 0, new SetPointAccessCommand { Open = true }).Accepted, "saved twin receives identical player intent");
            Reach(game, () => Stock(game, "destination") == Fix.FromInt(8) && !Read(game).HasValue, "reopened route delivery", twin);
            c.Equal(Fix.FromInt(60), Account(game).Balance, "recovered delivery incurs no second purchase"); c.Equal(game.Hash(), twin.Hash(), "restored route continues deterministically");
        }
        private static void Survival(Check c)
        {
            var factory = Factory("haul-survival"); var game = New(factory);
            c.That(!game.World.Services.Has<IEconomy>() && !game.World.Services.Has<IResourceTradingRules>() && !game.World.Services.Has<IResourcePurchaseSettlement>(), "resource-only hauling omits all economics without fake accounts");
            Demand(c, game); Reach(game, () => Stock(game, "machine") == Fix.FromInt(2), "resource-only physical loading");
            var twin = factory.Load(game.Save()); Reach(game, () => Stock(game, "destination") == Fix.FromInt(8) && !Read(game).HasValue, "resource-only physical delivery", twin);
            c.Equal(Fix.FromInt(12), Stock(game, "source"), "unpriced stock transport consumes actual source stock");
            c.Equal(Fix.Zero, Stock(game, "machine"), "unpriced transport delivers real cargo"); c.Equal(game.Hash(), twin.Hash(), "resource-only transport saves and continues with no economic services");
        }
    }
}
