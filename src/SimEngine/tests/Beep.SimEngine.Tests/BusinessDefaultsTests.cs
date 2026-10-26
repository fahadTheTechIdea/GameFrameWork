using System;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class BusinessDefaultsTests
    {
        public static void Register() => TestRunner.Add("Business defaults: optional JSON valuation, account ownership and saved sessions", Defaults);
        internal static GameFactory Factory(string core = null, string values = null, bool extra = false, bool missingExtraValue = false, ProbeModule proof = null, string accounts = null, string properties = null)
        {
            var definition = new GameDefinition("business-defaults", new GameModule[]
            { proof ?? new ProbeModule("proof.business", new ModuleVersion(1, 0, 0)), new TycoonModule(),
              new ProbeModule("proof.modes", new ModuleVersion(1, 0, 0)),
              new ProbeModule("proof.business-values", new ModuleVersion(1, 0, 0), new[] { new ModuleRequirement("sim.tycoon", new ModuleVersion(1, 0, 0)) }) });
            JsonValue manifest = Json.Parse(FrameworkProofContent.Read("business-manifest.json"));
            if (extra)
            {
                manifest.Get("modules")[0].Get("documents").Add(JsonValue.Of("business-extra-resource.json"));
                if (!missingExtraValue) manifest.Get("modules")[2].Get("documents").Add(JsonValue.Of("business-extra-value.json"));
            }
            var files = new PreloadedFileSource().Add("manifest.json", manifest.ToString())
                .Add("business-core.json", core ?? FrameworkProofContent.Read("business-core.json"))
                .Add("business-accounts.json", accounts ?? FrameworkProofContent.Read("business-accounts.json"))
                .Add("business-modes.json", FrameworkProofContent.Read("business-modes.json"))
                .Add("game-modes.json", FrameworkProofContent.Read("game-modes.json"))
                .Add("game-mode-properties.json", properties ?? FrameworkProofContent.Read("game-mode-properties.json"))
                .Add("game-mode-proof-properties.json", FrameworkProofContent.Read("game-mode-proof-properties.json"))
                .Add("business-values.json", values ?? FrameworkProofContent.Read("business-values.json"))
                .Add("business-extra-resource.json", FrameworkProofContent.Read("business-extra-resource.json"))
                .Add("business-extra-value.json", FrameworkProofContent.Read("business-extra-value.json"));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory, string scenario = "default-business")
        {
            var request = new NewGameRequest { Scenario = scenario, Seed = 23 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human);
            return factory.NewGame(request);
        }
        private static void Defaults(Check c)
        {
            var factory = Factory(); var survival = New(factory, "survival");
            c.That(!survival.World.Services.Has<IEconomy>() && !survival.World.Services.Has<IResourceValuation>() &&
                !survival.World.Services.Has<IBusinessAccounts>(), "module absence means no money, valuation or fake account");
            c.That(survival.Seat(0).View.Feature<BusinessView>() == null, "survival has no business feature");
            c.Equal(0, survival.World.Content.All<FixedResourceValuesDef>().Count, "unselected value files are not loaded into survival");
            c.Equal(2, survival.World.Content.ResourceCount, "physical resources exist without prices or a buyer/seller");
            var survivalTwin = factory.Load(survival.Save());
            survival.RunTicks(3); survivalTwin.RunTicks(1); survivalTwin.RunTicks(2);
            c.Equal(survival.Hash(), survivalTwin.Hash(), "resource-only session saves and advances on its sole clock");

            var game = New(factory); var seat = new PlayerId(0); var other = new PlayerId(1);
            var accounts = game.World.Services.Get<IBusinessAccounts>(); var money = game.World.Services.Get<IEconomy>();
            var view = game.Seat(0).View.Feature<BusinessView>(); var gold = game.World.Content.ResourceId("gold");
            var water = game.World.Content.ResourceId("water");
            c.That(ReferenceEquals(accounts, money), "account and economy bindings are aliases of one owner");
            c.Equal(1, game.World.Services.Persistent().Count, "aliases register exactly one account codec");
            c.Equal(3, game.World.Services.Persistent()[0].Metadata.SchemaVersion, "business account schema is explicit");
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current runtime identity");
            c.Equal(BusinessMode.FixedDefaults, view.Mode, "authored default mode is not advanced Tycoon mode");
            c.Equal("default-values", view.Policy, "the manifest-selected policy is visible without engine handles");
            c.Equal(new UnitId("unit.money"), view.Currency, "currency is a resolved unit identity");
            c.Equal(0, game.World.Systems.Count, "business defaults install no market or second update system");
            ulong before = game.Hash(); long time = game.World.Tick;
            var quote = view.Quote(gold, Fix.One);
            c.Equal(ResourceValueStatus.Ready, quote.Status, "gold value has an explicit successful status");
            c.Equal(Fix.FromInt(100000), quote.Value, "one gold unit has authored100000 value");
            c.Equal(Fix.FromInt(100000), quote.UnitValue, "quote carries the unit price rather than guessing host data");
            c.Equal(new UnitId("unit.gold"), quote.QuantityUnit, "quote preserves native quantity unit");
            c.Equal(Fix.FromInt(300000), view.Quote(gold, Fix.FromInt(3)).Value, "value scales by actual native quantity");
            c.Equal(ResourceValueStatus.InvalidQuantity, view.Quote(gold, Fix.Half).Status, "discrete gold rejects fractional units");
            c.Equal(ResourceValueStatus.InvalidQuantity, view.Quote(water, -Fix.One).Status, "negative quantity refuses");
            c.Equal(ResourceValueStatus.UnknownResource, view.Quote(new ResourceId(999), Fix.One).Status, "unknown resource does not fabricate zero value");
            c.Equal(ResourceValueStatus.OutOfRange, view.Quote(gold, Fix.FromInt(100000)).Status, "representational overflow is explicit");
            c.Equal(ResourceValueStatus.Unavailable, default(ResourceValueQuote).Status, "default quote never claims success");
            c.Equal(ResourceValueStatus.Ready, view.Quote(water, Fix.Half).Status, "explicit zero-valued continuous resource is valid");
            c.Equal(Fix.Zero, view.Quote(water, Fix.Half).Value, "zero is authored, not a fallback");
            c.Equal(Code.None, view.ReadAccount(seat, out var retained), "player reads own detached account");
            c.Equal(Fix.FromInt(500000), retained.Balance, "opening budget is scenario JSON");
            c.Equal(Codes.NotOwned, view.ReadAccount(other, out var hidden), "private balance refuses rival access");
            c.Equal(default(BusinessAccountSnapshot), hidden, "refusal leaks no rival account fact");
            c.Equal(Codes.InvalidArgument, game.Observer.Feature<BusinessView>().ReadAccount(new PlayerId(2), out _), "observer cannot read nonexistent seats");
            c.That(!money.CanAfford(new PlayerId(2), Fix.One) && !money.TryCharge(PlayerId.None, Fix.One, new Code("proof.debit")), "invalid accounts never spend");
            c.That(!money.TryCharge(seat, Fix.FromInt(500001), new Code("proof.debit")), "insufficient budget refuses without mutation");
            c.Equal(before, game.Hash(), "quotes, affordability, refused debit and private views are pure");
            c.Equal(time, game.World.Tick, "economic queries advance no clock");

            // Contract instrumentation only. H2 owns real gameplay settlement and monetary construction.
            c.That(money.TryCharge(seat, Fix.FromInt(400000), new Code("proof.debit")), "actual account debit consumes its one balance");
            c.Equal(Fix.FromInt(100000), money.Cash(seat), "successful debit is visible through same account owner");
            c.Equal(Fix.FromInt(500000), retained.Balance, "retained account snapshot stays detached");
            money.Credit(seat, Fix.FromInt(7), new Code("proof.credit"));
            c.Equal(Fix.FromInt(100007), money.Cash(seat), "contract credit uses the same balance");
            money.Charge(other, Fix.FromInt(500001), new Code("proof.running-cost"));
            c.Equal(-Fix.One, money.Cash(other), "explicit running-charge contract can enter debt");
            c.Throws<ArgumentOutOfRangeException>(() => money.Credit(seat, -Fix.One, new Code("proof.credit")), "negative credit cannot become debit");
            c.Throws<ArgumentOutOfRangeException>(() => money.TryCharge(seat, -Fix.One, new Code("proof.debit")), "negative charge cannot create money");
            c.Throws<ArgumentOutOfRangeException>(() => money.Cash(PlayerId.None), "invalid account cannot pose as zero balance");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "account balances, debt and mode identity save together");
            var isolated = New(factory); ulong isolatedHash = isolated.Hash();
            game.RunTicks(3); twin.RunTicks(3); survival.RunTicks(2); survivalTwin.RunTicks(2);
            c.Equal(game.Hash(), twin.Hash(), "saved selected business sessions use the ordinary update loop");
            c.Equal(survival.Hash(), survivalTwin.Hash(), "business updates never enable economics in another survival session");
            c.Equal(isolatedHash, isolated.Hash(), "interleaved sessions share immutable content, not account balances");
            c.That(factory.Describe(game.Save()).Compatible, "save manifest describes selected business module");
            c.Equal(Fix.FromInt(100000), twin.Seat(0).View.Feature<BusinessView>().Quote(gold, Fix.One).Value, "restored quotes still use frozen authored values");
            money.Credit(seat, Fix.MaxValue - money.Cash(seat), new Code("proof.limit"));
            ulong limitHash = game.Hash();
            c.Throws<OverflowException>(() => money.Credit(seat, Fix.One, new Code("proof.limit")), "credit overflow is checked, never wrapped");
            c.Equal(limitHash, game.Hash(), "overflow refuses before changing account");

            string core = FrameworkProofContent.Read("business-core.json"), values = FrameworkProofContent.Read("business-values.json");
            foreach (string invalidCore in new[]
            {
                core.Replace("\"account_policy\": \"colony.accounts\"", "\"account_policy\": \"missing\""),
                core.Replace("\"valuation_policy\": \"default-values\"", "\"valuation_policy\": \"default-values\", \"mode\": \"unknown\""),
                core.Replace("\"valuation_policy\": \"default-values\"", "\"valuation_policy\": \"default-values\", \"mode\": \"tycoon\""),
                core.Replace("\"valuation_policy\": \"default-values\"", "\"valuation_policy\": \"missing\""),
                core.Replace("\"account_policy\": \"colony.accounts\"", "\"account_policy\": \"colony.accounts\", \"hidden_default\": true")
            }) c.Throws<ContentException>(() => New(Factory(core: invalidCore)), "invalid setting/missing selected Tycoon policy refuses, never falls back");
            c.Throws<ContentException>(() => New(Factory(values: values.Replace("\"value\": 100000", "\"value\": -1"))), "negative default value refuses");
            c.Throws<ContentException>(() => New(Factory(values: values.Replace("\"unit\": \"unit.gold\"", "\"unit\": \"unit.water\""))), "unit mismatch refuses before gameplay");
            c.Throws<ContentException>(() => New(Factory(values: values.Replace("\"currency\": \"unit.money\"", "\"currency\": \"unit.water\""))), "physical units cannot silently become money units");
            c.Throws<ContentException>(() => New(Factory(values: values.Replace("\"catalog\": \"defaults\", \"resource\": \"water\"", "\"catalog\": \"unselected\", \"resource\": \"water\""))), "every selected resource needs an explicit value");
            JsonValue duplicate = Json.Parse(values); duplicate.Get("resource_values").Add(duplicate.Get("resource_values")[0].Clone());
            duplicate.Get("resource_values")[2].Set("id", JsonValue.Of("duplicate.gold"));
            c.Throws<ContentException>(() => New(Factory(values: duplicate.ToString())), "different definitions cannot price one catalog resource twice");
            JsonValue missing = Json.Parse(core); missing.Get("scenarios")[1].Get("settings").Set("sim.tycoon", JsonValue.NewObject());
            c.Throws<ContentException>(() => New(Factory(core: missing.ToString())), "selected module has no fabricated settings");
            var duplicateOwner = new ProbeModule("proof.business", new ModuleVersion(1, 0, 0),
                compose: composer => composer.Services.Bind<IEconomy>(new TestLedger(composer.World.Players.Count, Fix.One)));
            c.Throws<InvalidOperationException>(() => New(Factory(proof: duplicateOwner)), "module cannot install a second money authority");

            var extraFactory = Factory(extra: true); var expanded = New(extraFactory);
            c.Equal(Fix.FromInt(14), expanded.Seat(0).View.Feature<BusinessView>().Quote(expanded.World.Content.ResourceId("stone"), Fix.FromInt(2)).Value,
                "adding master-listed resource/value files extends the same catalog without scheduler edits");
            c.That(!extraFactory.Describe(twin.Save()).Compatible, "changed selected content identity refuses old save compatibility");
            c.Throws<ContentException>(() => New(Factory(extra: true, missingExtraValue: true)), "missing newly enrolled resource value refuses monetized composition");
            var newSurvival = New(Factory(extra: true, missingExtraValue: true), "survival");
            c.Equal(3, newSurvival.World.Content.ResourceCount, "survival can add resource files without values");
            string smallValue = values.Replace("\"value\": 0", "\"value\": 0.00000000023283064365386962890625");
            var fine = New(Factory(values: smallValue));
            c.Equal(ResourceValueStatus.Unrepresentable, fine.Seat(0).View.Feature<BusinessView>().Quote(fine.World.Content.ResourceId("water"), Fix.Half).Status,
                "sub-quantum currency result refuses instead of rounding into fabricated zero");

            // A real resource-only process uses public commands and the existing engine, not a test economy.
            var processDefinition = new GameDefinition("business-survival-process", new GameModule[]
            { new InventoryModule(), new OperationsModule(), new ContentAccessModule(), new TransformationModule(), new WorldSetupModule(), new ProcessProofModule() });
            var processFiles = new PreloadedFileSource().Add("manifest.json", FrameworkProofContent.Read("process-manifest.json"))
                .Add("framework/access.json", FrameworkProofContent.Access).Add("process.json", FrameworkProofContent.Read("process.json"));
            var processFactory = new GameFactory(processDefinition, ContentSet.Load(processFiles, processDefinition.ContentTypes));
            var process = processFactory.NewGame(new NewGameRequest { Scenario = "process", Seed = 4 });
            Entity assembler = null; foreach (Entity entity in process.World.Entities.All) if (entity.Def.Id == "assembler") assembler = entity;
            c.That(!process.World.Services.Has<IEconomy>(), "timed survival processing has no dummy money dependency");
            c.That(TestKit.Order(process, 0, new EnqueueTransformationCommand { Transformer = assembler.Id, Capability = new Code("transformation"), Transformation = "build", Count = 1 }).Accepted,
                "ordinary public player intent starts real resource-only construction/manufacture");
            process.RunTicks(5);
            c.Equal(Fix.FromInt(18), assembler.Get<IInventory>().Amount(process.World.Content.ResourceId("supply")), "resource-only timed inputs/outputs complete without a buyer or seller");
            c.That(process.Fault == null, "survival completion never accesses an absent business module");
            string priced = FrameworkProofContent.Read("process.json").Replace("\"mode\":\"batch\"", "\"cash\":2,\"mode\":\"batch\"");
            var pricedFiles = new PreloadedFileSource().Add("manifest.json", FrameworkProofContent.Read("process-manifest.json"))
                .Add("framework/access.json", FrameworkProofContent.Access).Add("process.json", priced);
            var pricedFactory = new GameFactory(processDefinition, ContentSet.Load(pricedFiles, processDefinition.ContentTypes));
            c.Throws<ContentException>(() => pricedFactory.NewGame(new NewGameRequest { Scenario = "process", Seed = 4 }), "authored monetary work without economy refuses composition");
        }
    }
}
