using System;
using System.Collections.Generic;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Player commands, detached observations and bounded time. No test-owned work, holds or payment.
    public static class WorkBudgetTests
    {
        public static void Register() => TestRunner.Add("Tycoon budgets: competing physical work, release and saved payment", Physical);
        internal static GameFactory Factory(int opening = 100, bool survival = false, bool lifecycle = false, bool controls = false, string recovery = null,
            Action<JsonValue> recoveryEdit = null, Action<JsonValue> physicalEdit = null, string[] physicalDocuments = null, bool incidents = false, IReadOnlyList<string> startupProfiles = null, Action<JsonValue> workEdit = null)
        {
            var modules = new List<GameModule>
            {
                new ContentAccessModule(), new InventoryModule(), new OperationsModule(), new MotionModule(),
                new TransformationModule(), new WorkModule(), new LogisticsModule(), new WorldSetupModule(), new PointModule(),
                new TycoonModule(), new ProbeModule("proof.modes", new ModuleVersion(1, 0, 0))
            };
            if (recovery != null)
            {
                modules.Add(new LogisticsRecoveryPolicyModule());
                modules.Add(new ProbeModule("proof.recovery-destinations", new ModuleVersion(1, 0, 0),
                    content: types => types.Section<MinimumPriorityRecoveryDestinationDef>("proof_recovery_destinations")));
            }
            if (incidents) modules.Add(new IncidentResponseModule());
            var definition = new GameDefinition("work-budget-proof", modules.ToArray());
            JsonValue content = Json.Parse(FrameworkProofContent.Read("purchasing-haul.json"));
            physicalEdit?.Invoke(content);
            StartupContentAuthoring.Include(content, "work-budgets.start");
            if (recovery != null) StartupContentAuthoring.Include(content, "recovery-destination-extension.start");
            if (controls) StartupContentAuthoring.Include(content, "worker-player-control.start");
            if (startupProfiles != null) foreach (string profile in startupProfiles) StartupContentAuthoring.Include(content, profile);
            if (incidents) content.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(IncidentResponseModule.ModuleId));
            content.Get("scenarios")[0].Set("game_mode", JsonValue.Of(lifecycle ? "budget-recovery" : survival ? "budget-survival" : "budget-economic"));
            content.Get("scenarios")[0].Get("settings").Set("sim.tycoon", Json.Parse(lifecycle
                ? "{\"account_policy\":\"haul.accounts\",\"valuation_policy\":\"haul.values\",\"income_policy\":\"haul.primary\"}"
                : "{\"account_policy\":\"haul.accounts\"}"));
            if (recovery != null)
            {
                content.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(LogisticsRecoveryPolicyModule.ModuleId));
                content.Get("scenarios")[0].Get("modules").Add(JsonValue.Of("proof.recovery-destinations"));
                content.Get("scenarios")[0].Get("settings").Set(LogisticsRecoveryPolicyModule.ModuleId,
                    Json.Parse("{\"profile\":\"" + recovery + "\"}"));
            }
            if (lifecycle) content.Get("entities")[3].Get("components").Get("transformer").Get("transformations").Add(JsonValue.Of("budget.fund"));
            JsonValue work = Json.Parse(FrameworkProofContent.Read("work-budgets.json"));
            work.Set("staffed_activity_executors", Json.Parse(FrameworkProofContent.Read("work-retry.json")).Get("staffed_activity_executors"));
            workEdit?.Invoke(work);
            if (controls) for (int i = 0; i < work.Get("entities").Count; i++)
                if (work.Get("entities")[i].Get("id").Text.StartsWith("budget.worker-", StringComparison.Ordinal))
                    work.Get("entities")[i].Get("components").Set("work_policy", Json.Parse("{\"profile\":\"budget.player-control\"}"));
            var manifest = Json.Parse(FrameworkProofContent.Read("purchasing-haul-manifest.json"));
            if (incidents) manifest.Get("modules").Add(Json.Parse("{\"id\":\"sim.incidents\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[]}"));
            if (recovery != null)
            {
                manifest.Get("modules").Add(Json.Parse("{\"id\":\"sim.logistics-recovery\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[\"recovery-destinations.json\"]}"));
                manifest.Get("modules").Add(Json.Parse("{\"id\":\"proof.recovery-destinations\",\"version\":\"1.0.0\",\"documents\":[\"recovery-destination-extension.json\"]}"));
            }
            for (int i = 0; i < manifest.Get("modules").Count; i++)
            {
                JsonValue module = manifest.Get("modules")[i];
                if (module.Get("id").Text == "points") module.Get("documents").Add(JsonValue.Of("work-budgets.json"));
                if (module.Get("id").Text == "proof.modes") module.Set("documents", Json.Parse("[\"game-mode-properties.json\",\"work-budget-modes.json\"]"));
                if (lifecycle && module.Get("id").Text == "points") module.Get("documents").Add(JsonValue.Of("work-budget-lifecycle.json"));
                if (lifecycle && module.Get("id").Text == "proof.modes") module.Get("documents").Add(JsonValue.Of("work-budget-lifecycle-modes.json"));
                if (controls && module.Get("id").Text == "points") module.Get("documents").Add(JsonValue.Of("worker-player-control.json"));
                if (physicalDocuments != null && module.Get("id").Text == "points")
                    foreach (string document in physicalDocuments) module.Get("documents").Add(JsonValue.Of(document));
            }
            var economics = Json.Parse(FrameworkProofContent.Read("purchasing-haul-economics.json"));
            economics.Get("business_accounts")[0].Set("opening_cash", Json.Parse(opening.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            if (lifecycle) economics.Get("primary_production_income")[0].Get("causes").Add(JsonValue.Of("budget.fund"));
            var files = new PreloadedFileSource().Add("manifest.json", manifest.ToString()).Add("purchasing-haul.json", content.ToString())
                .Add("work-budgets.json", work.ToString()).Add("work-budget-modes.json", FrameworkProofContent.Read("work-budget-modes.json"))
                .Add("purchasing-haul-economics.json", economics.ToString()).Add("game-mode-properties.json", FrameworkProofContent.Read("game-mode-properties.json"))
                .Add("framework/access.json", FrameworkProofContent.Access).Add("haul-retry.json", FrameworkProofContent.Read("haul-retry.json"))
                .Add("point-handling.json", FrameworkProofContent.Read("point-handling.json"));
            if (lifecycle) files.Add("work-budget-lifecycle.json", FrameworkProofContent.Read("work-budget-lifecycle.json"))
                .Add("work-budget-lifecycle-modes.json", FrameworkProofContent.Read("work-budget-lifecycle-modes.json"));
            if (controls) files.Add("worker-player-control.json", FrameworkProofContent.Read("worker-player-control.json"));
            if (physicalDocuments != null) foreach (string document in physicalDocuments) files.Add(document, FrameworkProofContent.Read(document));
            if (recovery != null)
            {
                JsonValue recoveryContent = Json.Parse(FrameworkProofContent.Read("recovery-destinations.json"));
                recoveryEdit?.Invoke(recoveryContent);
                files.Add("recovery-destinations.json", recoveryContent.ToString())
                    .Add("recovery-destination-extension.json", FrameworkProofContent.Read("recovery-destination-extension.json"));
            }
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "purchase-haul", Seed = 61 });
        private static Entity Find(SimGame game, string id)
        { foreach (var entity in game.World.Entities.All) if (entity.Def.Id == id) return entity; throw new InvalidOperationException("missing " + id); }
        private static Fix Stock(SimGame game, string id) => Find(game, id).Get<IInventory>().Amount(game.World.Content.ResourceId("supply"));
        private static BusinessAccountSnapshot Account(SimGame game)
        { Code code = game.Seat(0).View.Feature<BusinessView>().ReadAccount(new PlayerId(0), out var result); if (!code.IsNone) throw new InvalidOperationException("account refused " + code); return result; }
        private static List<MonetaryBudgetReceipt> Holds(SimGame game)
        { var result = new List<MonetaryBudgetReceipt>(); game.Seat(0).View.Feature<BusinessView>().CopyBudgets(new PlayerId(0), result); return result; }
        private static List<OrderView> Orders(SimGame game)
        { var result = new List<OrderView>(); game.Seat(0).View.Feature<WorkView>().CopyOrders(result); return result; }
        private static OrderView? Order(SimGame game, string target)
        { EntityId id = Find(game, target).Id; foreach (var order in Orders(game)) if (order.Target == id) return order; return null; }
        private static ActivityNodeView Node(SimGame game, string target)
        { var nodes = new List<ActivityNodeView>(); game.Seat(0).View.Feature<WorkView>().TryCopyNodes(Order(game, target).Value.Id, nodes); return nodes[0]; }
        private static void Update(SimGame game, SimGame twin = null, int steps = 1)
        {
            for (int i = 0; i < steps; i++)
            {
                if (game.RunTicks(1) != 1) throw new InvalidOperationException("work budget update fault " + game.Fault);
                if (twin != null && twin.RunTicks(1) != 1) throw new InvalidOperationException("work budget twin fault " + twin.Fault);
            }
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 120 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " timed out at " + game.Tick + ", balance=" + Account(game).Balance + ", reserved=" + Account(game).Reserved +
                ", orders=" + string.Join(";", Orders(game).ConvertAll(o => o.Id + "/" + o.State + "/" + o.Blocker + "/" + o.CurrentProgress.WholeTicks)));
        }
        private static void Post(SimGame game, bool both = true)
        {
            game.Seat(0).Submit(new PostOrderCommand { Plan = "budget.build", Target = Find(game, "budget.site-a").Id });
            if (both) game.Seat(0).Submit(new PostOrderCommand { Plan = "budget.build", Target = Find(game, "budget.site-b").Id });
            Update(game);
        }
        private static void Cancel(Check c, SimGame game, string target, SimGame twin = null)
        {
            int id = Order(game, target).Value.Id;
            game.Seat(0).Submit(new CancelOrderCommand { Order = id });
            if (twin != null) twin.Seat(0).Submit(new CancelOrderCommand { Order = id });
            Update(game, twin); c.That(Order(game, target) == null || Order(game, target).Value.State == OrderState.Cancelled, "player cancellation goes through existing work lifecycle");
        }
        private static void Physical(Check c)
        {
            Competition(c); Complete(c); Shortage(c); Survival(c); ReceiptContractRefusals(c);
        }
        private static void Competition(Check c)
        {
            var factory = Factory(); var game = New(factory); Post(game);
            Reach(game, () => Account(game).Reserved == Fix.FromInt(80), "first real assignment");
            c.Equal(Fix.FromInt(100), Account(game).Balance, "assignment holds money but does not spend it");
            c.Equal(new BigInteger(Fix.FromInt(20).Raw), Account(game).AvailableRaw, "only unreserved money is available");
            c.Equal(2, Account(game).BudgetReservations, "the selected two-leaf set is held by one account owner");
            c.Equal(2, Node(game, "budget.site-a").Costs.Count, "existing work bindings reference both real holds");
            c.Equal(WorkProgress.Zero, Node(game, "budget.site-a").Progress, "assigned machine must travel before productive work");
            c.Equal(Fix.FromInt(8), Stock(game, "budget.site-a"), "travel does not consume construction materials");
            c.Equal(WorkSystem.BlockerCash, Node(game, "budget.site-b").Blocker, "competing work cannot promise held money");
            c.Equal(0, Node(game, "budget.site-b").Costs.Count, "cash-blocked work creates no partial hold");
            c.That(ReferenceEquals(game.World.Services.Get<IEconomy>(), game.World.Services.Get<IMonetaryBudgets>()) &&
                ReferenceEquals(game.World.Services.Get<IMonetaryBudgets>(), game.World.Services.Get<IResourcePurchaseSettlement>()), "budget, payment and purchasing alias the same optional Tycoon owner");
            ulong hash = game.Hash();
            // This is the same public logistics admission used by physical purchase dispatch.
            var preview = game.Seat(0).View.Feature<LogisticsView>().PreviewHaul(Find(game, "source").Id, Find(game, "destination").Id,
                game.World.Content.ResourceId("supply"), Fix.FromInt(8), purchase: true);
            c.Equal(HaulHandlingCodes.Payment, preview.Blocker, "purchasing cannot use money held by construction");
            c.Equal(hash, game.Hash(), "purchase preview and detached budget diagnostics are pure");
            var privateRows = new List<MonetaryBudgetReceipt>();
            c.Equal(Codes.NotOwned, game.Seat(0).View.Feature<BusinessView>().CopyBudgets(new PlayerId(1), privateRows), "another company's budget details stay private");
            c.Equal(0, privateRows.Count, "refused private read exposes no receipts");
            long last = Holds(game)[1].Id;
            c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Transformer = Find(game, "processor").Id,
                Capability = new Code("transformation"), Transformation = "purchase.spend", Count = 1 }).Accepted, "player queues ordinary manufacturing, engine owns payment decision");
            game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(Find(game, "processor").Id, new Code("transformation"), out var process);
            c.Equal(WorkSystem.BlockerCash, process.Blocker, "manufacturing cannot consume assigned construction's budget");
            c.Equal(Fix.FromInt(100), Account(game).Balance, "blocked manufacturing spends nothing");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "mid-travel saves preserve exact account holds and work references");
            Update(game, twin, 3); c.Equal(game.Hash(), twin.Hash(), "saved traveling holds continue identically");
            game.Seat(0).Submit(new CancelTransformationCommand { Transformer = Find(game, "processor").Id, Capability = new Code("transformation"), Index = 0 });
            twin.Seat(0).Submit(new CancelTransformationCommand { Transformer = Find(twin, "processor").Id, Capability = new Code("transformation"), Index = 0 });
            Update(game, twin);
            Cancel(c, game, "budget.site-a", twin);
            Reach(game, () => Node(game, "budget.site-b").Costs.Count == 2, "released money reassigned", twin);
            c.Equal(Fix.FromInt(100), Account(game).Balance, "unpaid cancellation releases, never credits cash");
            c.Equal(Fix.FromInt(80), Account(game).Reserved, "another physical activity acquires the released whole budget");
            c.That(Holds(game)[0].Id > last, "released receipt IDs are never reused");
            c.Equal(Fix.FromInt(8), Stock(game, "budget.site-a"), "cancelled travel consumed no materials");
            Reach(game, () => Node(game, "budget.site-b").Progress.WholeTicks > 0, "real construction starts", twin);
            c.Equal(Fix.FromInt(20), Account(game).Balance, "first actual productive work spends exactly80");
            c.Equal(Fix.Zero, Account(game).Reserved, "spent money is no longer held");
            c.Equal(Fix.FromInt(6), Stock(game, "budget.site-b"), "physical first work consumes its real materials");
            foreach (var cost in Node(game, "budget.site-b").Costs) c.That(cost.Spent && cost.Receipt.HasValue, "paid binding retains provenance, not a live hold");
            c.Equal(game.Hash(), twin.Hash(), "receipt settlement is identical after saved travel");
            Cancel(c, game, "budget.site-b", twin);
            Update(game, twin, 4); c.Equal(Fix.FromInt(20), Account(game).Balance, "cancelling paid work does not refund or charge again");
            c.Equal(game.Hash(), twin.Hash(), "cancelled continuation has no orphan account promises");
        }
        private static void Complete(Check c)
        {
            var factory = Factory(160); var game = New(factory); Post(game);
            Reach(game, () => Account(game).Reserved == Fix.FromInt(160), "two whole assignments");
            c.Equal(4, Holds(game).Count, "both funded assignments hold their exact occurrences");
            var twin = factory.Load(game.Save());
            Reach(game, () => Node(game, "budget.site-a").Progress.WholeTicks > 0, "first work payment", twin);
            c.Equal(Fix.FromInt(80), Account(game).Balance, "one site pays while the other is still traveling");
            c.Equal(Fix.FromInt(80), Account(game).Reserved, "the other site's budget is retained");
            twin = factory.Load(game.Save());
            Reach(game, () => game.Seat(0).View.Feature<WorkView>().ActiveOrders(Find(game, "budget.site-a").Id) == 0 &&
                game.Seat(0).View.Feature<WorkView>().ActiveOrders(Find(game, "budget.site-b").Id) == 0, "both real builds finish", twin);
            c.Equal(Fix.Zero, Account(game).Balance, "each site's budget spends once, not per update");
            c.Equal(Fix.Zero, Account(game).Reserved, "completed work retains no live holds");
            c.Equal(0, Holds(game).Count, "whole account receipt collection is empty after payment");
            c.Equal(Fix.FromInt(6), Stock(game, "budget.site-a"), "first work consumes its authored material once");
            c.Equal(Fix.FromInt(6), Stock(game, "budget.site-b"), "second work consumes its authored material once");
            c.Equal(game.Hash(), twin.Hash(), "partial-payment checkpoint continues deterministically");
            Update(game, twin, 5); c.Equal(game.Hash(), twin.Hash(), "idle updates invent no extra charge or reservation");
        }
        private static void Shortage(Check c)
        {
            var factory = Factory(79); var game = New(factory); Post(game, false); Update(game, steps: 4);
            c.Equal(Fix.FromInt(79), Account(game).Balance, "whole-set shortage leaves money untouched");
            c.Equal(Fix.Zero, Account(game).Reserved, "two affordable individual leaves cannot partially acquire an unaffordable set");
            c.Equal(0, Holds(game).Count, "no partial receipt survives refusal");
            c.Equal(WorkProgress.Zero, Node(game, "budget.site-a").Progress, "cash shortage prevents construction progress");
            c.Equal(Fix.FromInt(8), Stock(game, "budget.site-a"), "shortage consumes no materials");
            c.Equal(WorkSystem.BlockerCash, Node(game, "budget.site-a").Blocker, "whole set reports the same engine cash blocker");
            var twin = factory.Load(game.Save()); Update(game, twin, 4); c.Equal(game.Hash(), twin.Hash(), "unfunded intent survives save without a fabricated hold");
        }
        private static void Survival(Check c)
        {
            var factory = Factory(survival: true); var game = New(factory); Post(game);
            c.That(!game.World.Services.Has<IEconomy>() && !game.World.Services.Has<IMonetaryBudgets>(), "disabled money composes neither account nor budget provider");
            c.That(!game.Seat(0).View.Feature<BusinessView>().HasBudgets, "dormant native monetary authoring does not activate budgets");
            Reach(game, () => Node(game, "budget.site-a").Worker.IsValid && Node(game, "budget.site-b").Worker.IsValid, "survival machines assigned");
            c.Equal(0, Node(game, "budget.site-a").Costs.Count, "disabled native costs create no monetary binding");
            var twin = factory.Load(game.Save());
            // No account diagnostic is needed in the successful bounded physical path.
            for (int i = 0; i < 80 && (game.Seat(0).View.Feature<WorkView>().ActiveOrders(Find(game, "budget.site-a").Id) > 0 ||
                game.Seat(0).View.Feature<WorkView>().ActiveOrders(Find(game, "budget.site-b").Id) > 0); i++) Update(game, twin);
            c.Equal(0, game.Seat(0).View.Feature<WorkView>().ActiveOrders(Find(game, "budget.site-a").Id), "survival first physical build completes");
            c.Equal(0, game.Seat(0).View.Feature<WorkView>().ActiveOrders(Find(game, "budget.site-b").Id), "survival second physical build completes");
            c.Equal(Fix.FromInt(6), Stock(game, "budget.site-a"), "money-off still consumes actual construction materials");
            c.Equal(game.Hash(), twin.Hash(), "survival saved travel and construction use the same engine loop");
            c.That(game.Fault == null && twin.Fault == null, "optional budgets never leak a required business dependency into physical work");
        }
        // Named receipt/commit boundary probes, not simulated player payment or resource production.
        private static void ReceiptContractRefusals(Check c)
        {
            var game = New(Factory());
            var settlement = game.World.Services.Get<IResourcePurchaseSettlement>();
            var intent = new ResourcePurchaseIntent(new PlayerId(0), game.World.Content.ResourceId("supply"), Fix.FromInt(8), (IResourceSupplyPrice)Find(game, "source").Get<ISupplier>());
            c.Equal(ResourceTransferStatus.Ready, settlement.PreparePurchases(new[] { intent }, out var commit), "contract probe prepares an affordable transient purchase before assignment");
            Post(game, false); Reach(game, () => Holds(game).Count == 2, "contract probe uses real assigned holds");
            var budgets = game.World.Services.Get<IMonetaryBudgets>(); var held = Holds(game); ulong hash = game.Hash();
            c.Throws<InvalidOperationException>(() => commit.Commit(), "contract probe refuses a stale purchase commit after holds change without changing balance");
            var altered = new MonetaryBudgetReceipt(held[0].Id, held[0].Company, held[0].Owner, held[0].Amount + Fix.One);
            c.Equal(MonetaryBudgetStatus.InvalidReceipt, budgets.Release(new PlayerId(0), new[] { altered }), "contract probe cannot release a mismatched amount");
            c.Equal(MonetaryBudgetStatus.InvalidReceipt, budgets.Spend(new PlayerId(0), new[] { held[0], held[0] }, new Code("proof.invalid")), "contract probe cannot spend a duplicate receipt set");
            c.Equal(MonetaryBudgetStatus.InvalidRequest, budgets.Spend(new PlayerId(1), held, new Code("proof.invalid")), "contract probe cannot debit another or nonexistent company");
            c.Equal(hash, game.Hash(), "all named contract refusals preserve actual money/holds/work/stock");
            c.Equal(Fix.FromInt(100), Account(game).Balance, "stale commit never spends physical purchase money");
        }
    }
}
