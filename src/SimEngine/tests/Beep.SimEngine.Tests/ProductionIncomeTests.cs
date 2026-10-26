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
    /// <summary>Public player commands/views; separately labeled resource/settlement contract probes.</summary>
    public static class ProductionIncomeTests
    {
        private const string PolicyContractCase = "Business policies: direct JSON valuation entries and income contracts retain native timed settlement";
        public static void Register()
        {
            TestRunner.Add("Tycoon income: committed primary production and independent physical generation", Production);
            TestRunner.Add(PolicyContractCase, PolicyContracts);
        }
        private static GameFactory Factory(string mode = "production-income", Action<JsonValue> edit = null, Action<JsonValue> economics = null,
            Action<ContentTypes> policies = null)
        {
            var definition = new GameDefinition("production-income-proof", new GameModule[]
            { new ContentAccessModule(), new InventoryModule(), new OperationsModule(), new TransformationModule(), new WorldSetupModule(), new ProcessProofModule(),
              new ProbeModule("proof.modes", new ModuleVersion(1, 0, 0), content: policies), new TycoonModule() });
            JsonValue content = Json.Parse(FrameworkProofContent.Read("production-income.json"));
            JsonValue economicContent = Json.Parse(FrameworkProofContent.Read("production-income-economics.json"));
            var scenario = content.Get("scenarios")[0]; scenario.Set("game_mode", JsonValue.Of(mode));
            if (mode == "production-survival")
            { scenario.Set("modules", Json.Parse("[\"proof.process\",\"proof.modes\",\"sim.world-setup\"]")); scenario.Get("settings").Remove(TycoonModule.ModuleId); }
            edit?.Invoke(content); economics?.Invoke(economicContent);
            var files = new PreloadedFileSource().Add("manifest.json", FrameworkProofContent.Read("production-income-manifest.json"))
                .Add("framework/access.json", FrameworkProofContent.Access).Add("production-income.json", content.ToString())
                .Add("production-income-economics.json", economicContent.ToString())
                .Add("game-mode-properties.json", FrameworkProofContent.Read("game-mode-properties.json"))
                .Add("production-income-modes.json", FrameworkProofContent.Read("production-income-modes.json"));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "production", Seed = 47 });
        private static Entity Machine(SimGame game, string prototype = "generator")
        { foreach (Entity entity in game.World.Entities.All) if (entity.Def.Id == prototype) return entity; throw new InvalidOperationException("missing " + prototype); }
        private static Fix Stock(SimGame game, string resource, string prototype = "generator")
            => Machine(game, prototype).Get<IInventory>().Amount(game.World.Content.ResourceId(resource));
        private static BusinessAccountSnapshot Account(SimGame game)
        {
            Code refusal = game.Seat(0).View.Feature<BusinessView>().ReadAccount(new PlayerId(0), out var account);
            if (!refusal.IsNone) throw new InvalidOperationException("missing actual account " + refusal);
            return account;
        }
        private static TransformationSnapshot Process(SimGame game)
        { game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(Machine(game).Id, new Code("transformation"), out var state); return state; }
        private static void Queue(Check c, SimGame game, string recipe = "gold.generate", int count = 1)
            => c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Transformer = Machine(game).Id, Capability = new Code("transformation"), Transformation = recipe, Count = count }).Accepted,
                "normal public player intent queues native timed production");
        private static void Complete(SimGame game, SimGame twin = null)
        {
            // Bounded acceptance stage, never an unbounded wait for a physical workflow.
            for (int i = 0; i < 12 && Process(game).Queue.Count > 0 && game.Fault == null && (twin == null || twin.Fault == null); i++)
            { game.Update(); twin?.Update(); }
            if (Process(game).Queue.Count != 0 || game.Fault != null || twin?.Fault != null)
                throw new InvalidOperationException("production stalled at game time " + game.Tick + "; blocker=" + Process(game).Blocker + "; fault=" + game.Fault);
        }
        private static void PolicyContracts(Check c)
        {
            GameFactory Direct(string mode = "production-income", Action<JsonValue> change = null)
                => Factory(mode, economics: root => {
                    foreach (string key in new[] { "fixed_resource_values", "resource_values", "primary_production_income" })
                    { var rows = root.Get(key); root.Remove(key); root.Set("contract_" + key, rows); }
                    change?.Invoke(root);
                }, policies: types => {
                    types.Section<IndependentBusinessValueDef>("contract_fixed_resource_values");
                    types.Section<IndependentResourceValueDef>("contract_resource_values");
                    types.Section<IndependentProductionIncomeDef>("contract_primary_production_income");
                });
            foreach (Type type in new[] { typeof(IndependentBusinessValueDef), typeof(IndependentResourceValueDef), typeof(IndependentProductionIncomeDef) })
                c.Equal(typeof(Def), type.BaseType, "independent metadata requires only the common content lifecycle: " + type.Name);
            var factory = Direct(); var game = New(factory); var view = game.Seat(0).View.Feature<BusinessView>();
            c.That(view.HasValuation && view.HasAccounts && view.HasGenerationIncome && !view.Rules.TycoonEnabled,
                "direct contracts select only authored optional fixed-default capabilities");
            c.Equal("production.values", view.Policy, "actual selected valuation identity");
            c.Equal("production.primary", Account(game).IncomePolicy, "actual selected income identity");
            game.Seat(0).View.TryFindResource("gold", out var gold); game.Seat(0).View.TryFindResource("water", out var water);
            ulong pure = game.Hash();
            c.Equal(Fix.FromInt(100000), view.Quote(gold, Fix.One).Value, "original authored gold price through independent value entry");
            c.Equal(ResourceValueStatus.Ready, view.Quote(water, Fix.One).Status, "explicit zero-valued water is valid, not a missing-price fallback");
            c.Equal(Fix.Zero, view.Quote(water, Fix.One).Value, "original explicit zero water value");
            c.Equal(ResourceValueStatus.InvalidQuantity, view.Quote(gold, Fix.Half).Status, "native discrete-unit validation remains");
            c.Equal(ResourceValueStatus.OutOfRange, view.Quote(gold, Fix.FromInt(100000)).Status, "exact original quote range refusal");
            c.Equal(ResourceValueStatus.UnknownResource, view.Quote(new ResourceId(999), Fix.One).Status, "no fabricated quote for an unknown resource");
            c.Equal(pure, game.Hash(), "quotes change no actual stock, income or clock");
            var idle = factory.Load(game.Save()); ulong isolated = idle.Hash();
            var limits = new ReplayLimits(8 * 1024 * 1024, 1000); ulong initialHash = game.Hash();
            using var recording = game.RecordReplay(limits);
            c.Equal(Fix.Zero, Account(game).Balance, "opening stock creates no generation income");
            Queue(c, game); c.Equal(Fix.Zero, Account(game).Balance, "uncommitted timed production is not paid");
            c.Equal(Fix.FromInt(2), Stock(game, "gold"), "no early physical output");
            var twin = factory.Load(game.Save()); Complete(game, twin);
            c.Equal(game.Hash(), twin.Hash(), "saved productive continuation preserves actual policies and settlement");
            c.Equal(Fix.FromInt(3), Stock(game, "gold"), "one actual native timed gold output");
            c.Equal(Fix.FromInt(100000), Account(game).Balance, "single original whole-result payout");
            c.Equal(1L, Account(game).GenerationSettlements, "one settlement, not a credit for each resource event");
            Queue(c, game, count: 2); Complete(game);
            c.Equal(Fix.FromInt(5), Stock(game, "gold"), "two sequential native results");
            c.Equal(Fix.FromInt(300000), Account(game).Balance, "original price credits only three actual primary results");
            c.Equal(3L, Account(game).GenerationSettlements, "native once-only settlement sequence");
            Queue(c, game, "gold.recycle"); Complete(game);
            c.Equal(Fix.FromInt(5), Stock(game, "gold"), "original consumed-input conversion completes physically");
            c.Equal(Fix.FromInt(300000), Account(game).Balance, "primary-only rule refuses recycled-input rewards even for listed cause");
            Queue(c, game, "gold.recycle");
            c.That(TestKit.Order(game, 0, new CancelTransformationCommand { Transformer = Machine(game).Id,
                Capability = new Code("transformation"), Index = 0 }).Accepted, "ordinary cancellation uses original WIP return");
            Complete(game); c.Equal(Fix.FromInt(5), Stock(game, "gold"), "cancellation returns real input without creation");
            c.Equal(3L, Account(game).GenerationSettlements, "cancellation/load never recredits an earned result");
            var saved = factory.Load(game.Save()); game.Update(); saved.Update();
            c.Equal(game.Hash(), saved.Hash(), "settled contracts save and continue on the original game clock");
            c.Equal(isolated, idle.Hash(), "shared definitions never advance another account session");
            byte[] bytes = recording.Finish(); var description = ReplayVerifier.Describe(bytes, limits);
            c.That(description.InitialStep == recording.InitialStep && description.InitialHash == initialHash,
                "direct-policy replay begins before all production inputs");
            c.That(description.FinalStep == game.Clock.StepIndex && description.FinalHash == game.Hash()
                && description.Commands == recording.CommandCount && description.Checkpoints == recording.CheckpointCount,
                "recording reaches the actual final material/account state");
            c.Equal(5, recording.CommandCount, "all original primary, sequential, recycled and cancellation intents recorded");
            c.Equal(game.Clock.StepIndex - recording.InitialStep, (long)recording.CheckpointCount, "every earned update is recorded");
            var replay = new ReplayVerifier().Verify(factory, bytes, limits);
            c.That(replay.Success && replay.Step == game.Clock.StepIndex && replay.ActualHash == game.Hash()
                && replay.ExpectedHash == game.Hash(), "direct-policy complete stream reproduces native final state: " + replay.Detail);
            Console.WriteLine("direct business policy replay: initial=" + description.InitialStep + ";final=" + description.FinalStep
                + ";commands=" + description.Commands + ";checkpoints=" + description.Checkpoints + ";live-hash=" + game.Hash().ToString("X16")
                + ";verified-hash=" + replay.ActualHash.ToString("X16"));
            foreach (string mode in new[] { "production-no-income", "production-survival" })
            {
                var plainFactory = Direct(mode); var plain = New(plainFactory); Queue(c, plain); Complete(plain);
                c.Equal(Fix.FromInt(3), Stock(plain, "gold"), "same physical generation with income off: " + mode);
                var optional = plain.Seat(0).View.Feature<BusinessView>();
                c.That(optional == null || !optional.HasGenerationIncome, "metadata never enables income by itself: " + mode);
                if (optional != null) c.Equal(Fix.Zero, Account(plain).Balance, "income-off balance remains unchanged");
            }
            c.Throws<ContentException>(() => New(Direct(change: root => root.Get("contract_resource_values")[0].Set("value", JsonValue.Number("-1")))), "shared entry reader rejects negative values");
            c.Throws<ContentException>(() => New(Direct(change: root => root.Get("contract_resource_values")[0].Set("unit", JsonValue.Of("unit.water")))), "shared entry linker rejects a mismatched native resource unit");
            c.Throws<ContentException>(() => New(Direct(change: root => {
                var values = JsonValue.NewArray(); values.Add(root.Get("contract_resource_values")[1].Clone()); root.Set("contract_resource_values", values);
            })), "shared fixed catalog rejects missing explicit resource value");
            c.Throws<ContentException>(() => New(Direct(change: root => {
                var duplicate = root.Get("contract_resource_values")[0].Clone(); duplicate.Set("id", JsonValue.Of("production.gold.duplicate")); root.Get("contract_resource_values").Add(duplicate);
            })), "shared fixed catalog rejects duplicate resource values");
            c.Throws<ContentException>(() => New(Direct(change: root => root.Get("contract_primary_production_income")[0].Set("input_rule", JsonValue.Of("recycled")))), "original explicit primary-only income rule remains required");
            c.Throws<ContentException>(() => New(Direct(change: root => root.Get("contract_primary_production_income")[0].Get("causes").Add(JsonValue.Of("gold.generate")))), "shared income authoring rejects duplicate causes");
            c.Throws<ContentException>(() => New(Direct(change: root => root.Get("contract_primary_production_income")[0].Remove("causes"))), "income cause selection remains explicit");
            c.That(game.Fault == null && saved.Fault == null, "direct policy composition and native settlement remain healthy");
        }
        private static void Production(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current committed-production runtime"); c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "direct status/behavior migration");
            var factory = Factory(); var game = New(factory); World world = game.World;
            var settlement = world.Services.Get<IResourceProductionSettlement>(); var economy = world.Services.Get<IEconomy>();
            c.That(ReferenceEquals(settlement, economy) && ReferenceEquals(settlement, world.Services.Get<IBusinessAccounts>()), "income and account bindings alias the sole account owner");
            c.Equal(TycoonModule.ModuleId, world.Services.OwnerOf(settlement), "Tycoon owns actual settlement");
            c.Equal(1, world.Services.Persistent().Count, "income adds no second ledger/codec");
            c.That(game.Seat(0).View.Feature<BusinessView>().HasGenerationIncome, "immutable view reports actual installed income capability");
            c.Equal("production.primary", Account(game).IncomePolicy, "account identifies its actual selected JSON income policy");
            c.Equal(Fix.Zero, Account(game).Balance, "starting gold/water never earn production income");
            c.Equal(0L, Account(game).GenerationSettlements, "initialization never records a generation settlement");
            c.Equal(Fix.FromInt(2), Stock(game, "gold"), "initial physical gold remains real stock");
            ulong before = game.Hash();
            c.Equal(Fix.FromInt(100000), game.Seat(0).View.Feature<BusinessView>().Quote(world.Content.ResourceId("gold"), Fix.One).Value, "one native gold unit has the authored100000 quote");
            var command = new EnqueueTransformationCommand { Transformer = Machine(game).Id, Capability = new Code("transformation"), Transformation = "gold.generate", Count = 1 };
            c.Equal(Code.None, game.Seat(0).Preview(command), "production command preview is available");
            c.Equal(before, game.Hash(), "quotes/player previews never credit money or mutate game state");
            Queue(c, game);
            c.Equal(Fix.Zero, Account(game).Balance, "queue/startup never rewards uncommitted production");
            c.Equal(Fix.FromInt(2), Stock(game, "gold"), "resource output is delayed by actual machine duration");
            c.Equal(1L, Process(game).Progress.WholeTicks, "existing update loop advances real production time");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "save during production restores income identity without payout");
            Complete(game, twin);
            c.Equal(Fix.FromInt(3), Stock(game, "gold"), "native engine produces one gold unit physically");
            c.Equal(Fix.FromInt(7), Stock(game, "water"), "the same whole conversion commits the zero-valued co-product");
            c.Equal(Fix.FromInt(100000), Account(game).Balance, "committed gold pays its default value without a buyer/seller");
            c.Equal(1L, Account(game).GenerationSettlements, "whole multi-resource output settles once, not per resource event");
            c.Equal(new BigInteger(Fix.FromInt(100000).Raw), Account(game).GenerationIncomeRaw, "income diagnostics preserve exact accumulated raw currency");
            c.Equal(game.Hash(), twin.Hash(), "saved productive continuation is identical");
            Queue(c, game, count: 2); Complete(game);
            c.Equal(Fix.FromInt(5), Stock(game, "gold"), "sequential real production makes two further units");
            c.Equal(Fix.FromInt(300000), Account(game).Balance, "another two gold units earn exactly200000");
            c.Equal(3L, Account(game).GenerationSettlements, "native sequential executions have distinct completed settlements");
            var continued = factory.Load(game.Save()); game.RunTicks(3); continued.RunTicks(3);
            c.Equal(game.Hash(), continued.Hash(), "restore/idle continuation never recredits old output");
            c.Equal(Fix.FromInt(300000), Account(continued).Balance, "restored balance is not recomputed from gold inventory");

            Queue(c, game, "gold.recycle"); c.Equal(Fix.FromInt(4), Stock(game, "gold"), "recycled recipe moves its actual input into WIP");
            Complete(game); c.Equal(Fix.FromInt(5), Stock(game, "gold"), "recycle physically returns its output after time");
            c.Equal(Fix.FromInt(300000), Account(game).Balance, "primary-only policy does not reward consumed-input loops even when cause is listed");
            c.Equal(3L, Account(game).GenerationSettlements, "unrewarded conversion does not manufacture a positive income settlement");
            Queue(c, game, "gold.recycle");
            c.That(TestKit.Order(game, 0, new CancelTransformationCommand { Transformer = Machine(game).Id, Capability = new Code("transformation"), Index = 0 }).Accepted,
                "player cancellation uses native WIP return rules");
            Complete(game); c.Equal(Fix.FromInt(5), Stock(game, "gold"), "cancellation returns inputs rather than generating new gold");
            c.Equal(Fix.FromInt(300000), Account(game).Balance, "returned/cancelled material earns no generation income");

            ContractTransactions(c, game);
            foreach (string mode in new[] { "production-no-income", "production-survival" })
            {
                var plainFactory = Factory(mode); var plain = New(plainFactory);
                c.That(!plain.World.Services.Has<IResourceProductionSettlement>(), "income-off/omitted mode binds no settlement capability");
                c.That((mode == "production-no-income") == plain.World.Services.Has<IEconomy>(), "accounts and resource-only production remain independent");
                Queue(c, plain); var saved = plainFactory.Load(plain.Save()); Complete(plain, saved);
                c.Equal(Fix.FromInt(3), Stock(plain, "gold"), "the same timed engine generates physical gold with income off");
                c.Equal(plain.Hash(), saved.Hash(), "income-off physical production saves and continues identically");
                if (mode == "production-no-income") c.Equal(Fix.Zero, Account(plain).Balance, "accounts-only production earns no automatic money");
                c.That(plain.Fault == null, "unpriced physical generation never requires nonexistent currency/accounts");
            }
            Blocked(c, full: true); Blocked(c, full: false);
            Continuous(c);
            c.Throws<ContentException>(() => New(Factory(edit: root => root.Get("scenarios")[0].Get("settings").Get("sim.tycoon").Remove("income_policy"))), "income-on requires its explicit policy, no default provider");
            c.Throws<ContentException>(() => New(Factory(edit: root => root.Get("scenarios")[0].Get("settings").Get("sim.tycoon").Set("income_policy", JsonValue.Of("missing")))), "missing selected income reference refuses before gameplay");
            c.That(game.Fault == null, "production/transfer/cancellation flow stays in the engine without faults");
        }
        private static void ContractTransactions(Check c, SimGame game)
        {
            // Explicit direct transaction probes, not host-side gameplay logic.
            World world = game.World; PlayerId company = new PlayerId(0); ResourceId gold = world.Content.ResourceId("gold");
            IInventory source = Machine(game).Get<IInventory>(), destination = Machine(game, "receiver").Get<IInventory>();
            IResourceTransaction transaction = world.Services.Get<IResourceTransaction>(); Fix balance = Account(game).Balance;
            var owner = new ReservationOwner("proof.income-transfer", 1);
            var outgoing = source.ReserveOut(owner, gold, Fix.One); var incoming = destination.ReserveIn(owner, gold, Fix.One);
            var move = new ResourceTransferIntent(owner, company, outgoing, incoming, gold, Fix.One);
            c.That(transaction.Commit(move).Committed, "ordinary stock transfer uses the existing transaction owner");
            c.Equal(balance, Account(game).Balance, "moving credited physical gold never credits it again");
            ulong before = game.Hash(); c.That(!transaction.Commit(move).Committed, "consumed transfer receipts cannot replay"); c.Equal(before, game.Hash(), "refused repeat transfer changes neither stock nor income");
            var generation = new ResourceInventoryIntent(new ReservationOwner("proof.primary", 2), company, Machine(game).Id,
                new Bill(new[] { new ResourceAmount(gold, Fix.One) }), ResourceFlowKind.Produced, new Code("proof.primary"));
            before = game.Hash(); c.Equal(ResourceTransferStatus.Ready, transaction.EvaluateInventory(generation).Status, "direct eligible production is admitted through the same settlement rule");
            c.Equal(before, game.Hash(), "generation preview never pays");
            c.That(transaction.CommitInventory(generation).Committed, "verified Produced inventory batch uses the same account settlement");
            c.Equal(balance + Fix.FromInt(100000), Account(game).Balance, "direct committed production earns exactly its physical one-unit value");
            balance = Account(game).Balance;
            var conversion = new ResourceConversionIntent(new ReservationOwner("proof.primary", 3), company, Array.Empty<ResourceConversionInput>(),
                new[] { new ResourceProductionIntent(source.Endpoint, new Bill(new[] { new ResourceAmount(gold, Fix.One) })),
                        new ResourceProductionIntent(destination.Endpoint, new Bill(new[] { new ResourceAmount(gold, Fix.One) })) }, new Code("proof.primary"));
            before = game.Hash(); c.That(transaction.EvaluateConversion(conversion, null).Ready, "multi-endpoint generation has whole pure admission");
            c.Equal(before, game.Hash(), "multi-endpoint preview does not consume capacity or money");
            long count = Account(game).GenerationSettlements;
            c.That(transaction.CommitConversion(conversion, null).Committed, "same conversion owner verifies every generated endpoint");
            c.Equal(balance + Fix.FromInt(200000), Account(game).Balance, "whole multi-endpoint output rewards only its aggregate actual quantity");
            c.Equal(count + 1, Account(game).GenerationSettlements, "multi-endpoint output is one settlement");
            c.Equal(Code.None, world.Services.Get<IResourceProductionSettlement>().CheckGeneration(new ResourceGenerationContext(
                new ReservationOwner("proof.neutral", 1), PlayerId.None, new Code("proof.primary"), Bill.Empty, generation.Quantities)), "neutral physical production has no invented recipient account");
            before = game.Hash();
            c.Equal(ProductionIncomeCodes.Invalid, world.Services.Get<IResourceProductionSettlement>().PrepareGeneration(default, out var invalid), "invalid generation cannot produce a payable admission");
            c.That(invalid == null, "refusal returns no commit"); c.Equal(before, game.Hash(), "invalid settlement admission is pure");
        }
        private static void Blocked(Check c, bool full)
        {
            var factory = full ? Factory(edit: root => root.Get("entities")[0].Get("components").Get("storage").Get("initial_stock").Set("gold", Json.Parse("15")))
                : Factory(economics: root => root.Get("business_accounts")[0].Set("opening_cash", Json.Parse("2147400000")));
            var game = New(factory); Fix balance = Account(game).Balance; Fix gold = Stock(game, "gold");
            Queue(c, game); game.RunTicks(4);
            c.Equal(full ? TransformerManager.BlockerOutput : ProductionIncomeCodes.Range, Process(game).Blocker, "full output/account arithmetic refusal is an engine-calculated blocker");
            c.Equal(gold, Stock(game, "gold"), "refused output creates no partial physical gold");
            c.Equal(Fix.FromInt(5), Stock(game, "water"), "whole output refusal also preserves the co-product");
            c.Equal(balance, Account(game).Balance, "refused generation cannot credit money");
            c.Equal(0L, Account(game).GenerationSettlements, "no committed output means no settlement identity");
            var twin = factory.Load(game.Save()); game.RunTicks(2); twin.RunTicks(2);
            c.Equal(game.Hash(), twin.Hash(), "blocked physical generation saves with no payout");
            c.That(game.Fault == null && twin.Fault == null, "ordinary shortage/arithmetic refusal does not fault the session");
        }
        private static void Continuous(Check c)
        {
            var factory = Factory(); var game = New(factory); Queue(c, game, "gold.flow");
            c.Equal(Fix.FromInt(3), Stock(game, "gold"), "native continuous rate commits its actual one-second output");
            c.Equal(Fix.FromInt(100000), Account(game).Balance, "continuous primary output uses the same settlement owner");
            var twin = factory.Load(game.Save()); game.RunTicks(3); twin.RunTicks(3);
            c.Equal(Fix.FromInt(6), Stock(game, "gold"), "continuous native flow progresses on the sole game clock");
            c.Equal(Fix.FromInt(400000), Account(game).Balance, "continuous physical units earn exactly once per committed quantum");
            c.Equal(4L, Account(game).GenerationSettlements, "continuous output settles only completed resource conversions");
            c.Equal(game.Hash(), twin.Hash(), "continuous income and physical progress save together");
            factory = Factory(economics: root => root.Get("business_accounts")[0].Set("opening_cash", Json.Parse("2147400000")));
            game = New(factory); Fix opening = Account(game).Balance; Queue(c, game, "gold.flow"); game.RunTicks(2);
            c.Equal(ProductionIncomeCodes.Range, Process(game).Blocker, "continuous income range is a preflight blocker, not a post-acquisition fault");
            c.Equal(Fix.FromInt(2), Stock(game, "gold"), "blocked continuous generation produces no partial stock");
            c.Equal(opening, Account(game).Balance, "blocked continuous income changes no balance");
            c.Equal(0L, Account(game).GenerationSettlements, "blocked continuous output records no settlement");
            c.That(game.Fault == null, "ordinary continuous income arithmetic refusal does not fault the engine");
        }
    }
}
