using System;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    /// <summary>Observes actual engine composition/quotes/accounts; no test-owned economics or gameplay.</summary>
    public static class BusinessModeCompositionTests
    {
        private const string AccountContractCase = "Business accounts: independent JSON metadata uses the account contract without a native base";
        public static void Register()
        {
            TestRunner.Add("Game modes: dormant business, values-only and independent JSON accounts", Composition);
            TestRunner.Add(AccountContractCase, AccountContracts);
        }
        private static SimGame New(GameFactory factory, string scenario)
            => factory.NewGame(new NewGameRequest { Scenario = scenario, Seed = 23 });
        private static string Edit(string file, Action<JsonValue> edit)
        { JsonValue root = Json.Parse(FrameworkProofContent.Read(file)); edit(root); return root.ToString(); }
        private static void AccountContracts(Check c)
        {
            GameFactory Factory(Action<JsonValue> edit = null)
            {
                var root = Json.Parse(FrameworkProofContent.Read("business-accounts.json"));
                var rows = root.Get("business_accounts"); root.Remove("business_accounts");
                root.Set("independent_business_accounts", rows); edit?.Invoke(rows[0]);
                return BusinessDefaultsTests.Factory(accounts: root.ToString(), proof: new ProbeModule("proof.business", new ModuleVersion(1, 0, 0),
                    content: types => types.Section<IndependentBusinessAccountDef>("independent_business_accounts")));
            }
            c.Equal(typeof(Def), typeof(IndependentBusinessAccountDef).BaseType, "non-friend metadata uses only the common content lifecycle");
            var factory = Factory(); var game = New(factory, "accounts-only");
            var view = game.Seat(0).View.Feature<BusinessView>();
            c.That(view.HasAccounts && !view.HasValuation && !view.HasGenerationIncome && !view.Rules.TycoonEnabled,
                "direct account metadata does not enable unrelated valuation, generation income or advanced economics");
            c.Equal(Code.None, view.ReadAccount(new PlayerId(0), out var initial), "ordinary public account observation");
            c.Equal("colony.accounts", initial.Policy, "same explicit master-listed account identity");
            c.Equal(Fix.FromInt(500000), initial.Balance, "original authored opening balance, no new default or grant");
            c.Equal(new UnitId("unit.money"), initial.Currency, "actual linked currency definition");
            c.Equal(0, initial.BudgetReservations, "new session creates no phantom obligation");
            c.Equal(Codes.NotOwned, game.Seat(1).View.Feature<BusinessView>().ReadAccount(new PlayerId(0), out var hidden), "ordinary foreign account refusal");
            c.Equal(default(BusinessAccountSnapshot), hidden, "refusal publishes no private account prefix");
            ulong before = game.Hash(); view.ReadAccount(new PlayerId(0), out _); c.Equal(before, game.Hash(), "metadata-backed account reads stay pure");
            var twin = factory.Load(game.Save()); var idle = New(factory, "accounts-only"); ulong untouched = idle.Hash();
            game.RunTicks(3); twin.RunTicks(1); twin.RunTicks(2);
            c.Equal(game.Hash(), twin.Hash(), "direct contract definition restores through the original account codec and one clock");
            c.Equal(untouched, idle.Hash(), "another account session remains independent");
            c.That(factory.Describe(game.Save()).Compatible, "normal save identity accepts the same explicit content contract");
            c.Equal(Code.None, view.ReadAccount(new PlayerId(0), out var retained), "native owner remains usable after ordinary time");
            c.Equal(initial.Balance, retained.Balance, "ordinary updates add no income or charges to account-only content");
            foreach (var scenario in new[] { "survival", "dormant-business", "values-only" })
            {
                var absent = New(factory, scenario); var optional = absent.Seat(0).View.Feature<BusinessView>();
                c.That(optional == null || !optional.HasAccounts, "direct definition never enables optional money by itself: " + scenario);
            }
            c.Throws<ContentException>(() => New(Factory(row => row.Set("opening_cash", JsonValue.Number("-1"))), "accounts-only"), "selected contract cannot bypass nonnegative opening-cash admission");
            c.Throws<ContentException>(() => New(Factory(row => row.Set("currency", JsonValue.Of("unit.gold"))), "accounts-only"), "selected contract cannot substitute a discrete noncurrency unit");
            c.Throws<ContentException>(() => New(Factory(row => row.Set("currency", JsonValue.Of("missing.currency"))), "accounts-only"), "missing currency remains an explicit content reference failure");
            c.That(game.Fault == null && twin.Fault == null, "original native account sessions remain healthy");
        }
        private static void Composition(Check c)
        {
            var factory = BusinessDefaultsTests.Factory();
            var survival = New(factory, "survival"); var dormant = New(factory, "dormant-business");
            var values = New(factory, "values-only"); var accounts = New(factory, "accounts-only"); var both = New(factory, "default-business");
            c.Equal("survival", survival.GameMode.ModeId, "resource-only game explicitly selects file-authored profile");
            c.That(!survival.World.Services.Has<IBusinessModeRules>() && !survival.World.Services.Has<IEconomy>() &&
                !survival.World.Services.Has<IResourceValuation>(), "business module omission composes no business rule/account/value provider");
            c.That(survival.Seat(0).View.Feature<BusinessView>() == null, "omitted capability has no fictional business view");
            var dormantView = dormant.Seat(0).View.Feature<BusinessView>();
            c.That(!dormantView.HasAccounts && !dormantView.HasValuation && !dormantView.HasGenerationIncome, "explicit dormant module installs no optional providers");
            c.Equal(0, dormant.World.Services.Persistent().Count, "immutable rules and dormant view introduce no mutable codec");
            c.Equal(0, dormant.World.Systems.Count, "configuration adds no business game loop");
            c.Equal(BusinessMode.Unavailable, dormantView.Mode, "no valuation is unavailable, not implicit fixed-default/Tycoon mode");
            c.Equal(null, dormantView.Policy, "no selected valuation means no fabricated policy identity");
            c.That(!dormantView.Currency.IsValid, "dormant capability has no fictional currency");
            c.Equal(new Code("business.no-account"), dormantView.ReadAccount(new PlayerId(0), out var noAccount), "missing account is typed absence, not zero balance");
            c.Equal(default(BusinessAccountSnapshot), noAccount, "absence exposes no account state");
            c.Equal(ResourceValueStatus.Unavailable, dormantView.Quote(dormant.World.Content.ResourceId("gold"), Fix.One).Status, "disabled values cannot pose as a free successful quote");
            var dormantRules = dormant.World.Services.Get<IBusinessModeRules>();
            c.That(ReferenceEquals(dormantRules, dormantView.Rules), "view uses the same immutable rules contract, not another interpreter");
            c.That(!dormantRules.ValuesEnabled && !dormantRules.MoneyEnabled && !dormantRules.ProductionIncomeEnabled &&
                !dormantRules.ConstructionCostsEnabled && !dormantRules.ManufacturingCostsEnabled && !dormantRules.RunningCostsEnabled &&
                !dormantRules.TradingEnabled && !dormantRules.TycoonEnabled, "all optional monetary flags remain explicitly off");

            var valueView = values.Seat(0).View.Feature<BusinessView>();
            c.That(valueView.HasValuation && !valueView.HasAccounts && !valueView.HasGenerationIncome, "values-only has quotes but no ledger/income");
            c.That(!values.World.Services.Has<IEconomy>() && !values.World.Services.Has<IBusinessAccounts>(), "valuation does not bind dummy economy aliases");
            c.Equal(0, values.World.Services.Persistent().Count, "fixed valuation has no mutable account codec");
            ulong valueHash = values.Hash(); long valueTime = values.Tick;
            var quote = valueView.Quote(values.World.Content.ResourceId("gold"), Fix.One);
            c.Equal(ResourceValueStatus.Ready, quote.Status, "authored fixed quote remains usable without money");
            c.Equal(Fix.FromInt(100000), quote.Value, "gold value comes from master-listed catalog, not new defaults");
            c.Equal(valueHash, values.Hash(), "quote is pure"); c.Equal(valueTime, values.Tick, "quote advances no game time");
            c.Equal(new Code("business.no-account"), valueView.ReadAccount(new PlayerId(0), out _), "values-only account lookup is unavailable");

            var accountView = accounts.Seat(0).View.Feature<BusinessView>();
            var money = accounts.World.Services.Get<IEconomy>(); var ledger = accounts.World.Services.Get<IBusinessAccounts>();
            c.That(ReferenceEquals(money, ledger), "independent account aliases share the existing sole owner");
            c.That(accountView.HasAccounts && !accountView.HasValuation && !accountView.HasGenerationIncome, "money-only has real balances without value policy/income");
            c.Equal(0, accounts.World.Content.Count<IBusinessValuePolicyDefinition>(), "accounts-only does not load unrelated valuation documents");
            c.Equal(1, accounts.World.Content.Count<IBusinessAccountDefinition>(), "account definition lives in the existing content registry");
            c.Equal("colony.accounts", ledger.Policy, "account provenance names its own authored definition, not a valuation policy");
            c.Equal(new UnitId("unit.money"), ledger.Currency, "account currency comes from its independent JSON definition");
            c.Equal(BusinessMode.Unavailable, accountView.Mode, "money does not imply valuation/Tycoon mode");
            c.Equal(ResourceValueStatus.Unavailable, accountView.Quote(accounts.World.Content.ResourceId("gold"), Fix.One).Status, "money-only has no fabricated value catalog");
            c.Equal(1, accounts.World.Services.Persistent().Count, "one existing account codec, not a second ledger");
            c.Equal(3, accounts.World.Services.Persistent()[0].Metadata.SchemaVersion, "account identity directly migrates codec schema");
            c.Equal(Code.None, accountView.ReadAccount(new PlayerId(0), out var retained), "ordinary account view reads real owner state");
            c.Equal(Fix.FromInt(500000), retained.Balance, "opening cash is explicitly authored in account definition");
            c.Equal(Codes.NotOwned, accountView.ReadAccount(new PlayerId(1), out var hidden), "account-only view preserves company privacy");
            c.Equal(default(BusinessAccountSnapshot), hidden, "privacy refusal leaks no account snapshot");
            ulong accountHash = accounts.Hash();
            c.That(!money.TryCharge(new PlayerId(0), Fix.FromInt(500001), new Code("proof.debit")), "real insufficient balance refuses");
            c.Equal(accountHash, accounts.Hash(), "refused account mutation is pure");
            c.That(money.TryCharge(new PlayerId(0), Fix.FromInt(2), new Code("proof.debit")), "actual account debit uses existing economy contract");
            c.Equal(Fix.FromInt(499998), money.Cash(new PlayerId(0)), "no duplicate balance handles debit");
            c.Equal(Fix.FromInt(500000), retained.Balance, "retained account snapshot remains detached");
            c.That(!accounts.World.Services.Has<IResourceProductionSettlement>(), "money-only binds no unwired production-income provider");
            var bothView = both.Seat(0).View.Feature<BusinessView>();
            c.That(bothView.HasAccounts && bothView.HasValuation && !bothView.HasGenerationIncome, "explicit dual selection composes both capabilities, not automatic income");
            c.Equal("default-values", bothView.Policy, "valuation identity stays separate from accounts");
            c.Equal("colony.accounts", both.World.Services.Get<IBusinessAccounts>().Policy, "both providers retain distinct authored identities");
            c.That(!bothView.Rules.TycoonEnabled, "default money/values require no advanced Tycoon activation");

            var interleaved = New(factory, "survival");
            foreach (var session in new[] { survival, dormant, values, accounts, both })
            {
                var twin = factory.Load(session.Save()); c.Equal(session.Hash(), twin.Hash(), "selected capability matrix saves with actual owner identity: " + session.Scenario);
                session.RunTicks(2); twin.RunTicks(1); interleaved.RunTicks(1); twin.RunTicks(1);
                c.Equal(session.Hash(), twin.Hash(), "other interleaved session cannot alter saved twin: " + session.Scenario);
                c.That(factory.Describe(session.Save()).Compatible, "load-menu checks current mode/account intent: " + session.Scenario);
                c.That(ReferenceEquals(session.Clock, session.World.Clock), "each session uses one existing clock: " + session.Scenario);
            }
            c.Equal(Fix.FromInt(499998), money.Cash(new PlayerId(0)), "ordinary updates generate no hidden income or charges in account-only content");
            Refusals(c);
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current rules/account identity build");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "account identity directly changes current checkpoint layout");
        }
        private static void Refusals(Check c)
        {
            foreach (string key in new[] { "valuation_policy", "account_policy" })
            {
                string missing = Edit("business-core.json", root => root.Get("scenarios")[1].Get("settings").Get("sim.tycoon").Remove(key));
                c.Throws<ContentException>(() => New(BusinessDefaultsTests.Factory(core: missing), "default-business"), "enabled provider requires explicit parameters: " + key);
            }
            foreach (string key in new[] { "mode", "accounts", "money_enabled" })
            {
                string old = Edit("business-core.json", root => root.Get("scenarios")[2].Set("settings",
                    Json.Parse("{\"sim.tycoon\":{\"" + key + "\":true}}")));
                c.Throws<ContentException>(() => New(BusinessDefaultsTests.Factory(core: old), "dormant-business"), "deleted/duplicate module switches refuse rather than override false flags: " + key);
            }
            string inactive = Edit("business-core.json", root => root.Get("scenarios")[2].Set("settings",
                Json.Parse("{\"sim.tycoon\":{\"valuation_policy\":\"missing.values\",\"account_policy\":\"missing.accounts\",\"income_policy\":\"missing.income\"}}")));
            var dormant = New(BusinessDefaultsTests.Factory(core: inactive), "dormant-business");
            c.That(!dormant.World.Services.Has<IEconomy>() && !dormant.World.Services.Has<IResourceValuation>(), "dormant parameters never secretly select/resolve providers");
            string malformed = Edit("business-core.json", root => root.Get("scenarios")[2].Set("settings", Json.Parse("{\"sim.tycoon\":{\"account_policy\":7}}")));
            c.Throws<ContentException>(() => New(BusinessDefaultsTests.Factory(core: malformed), "dormant-business"), "even dormant authored parameter shapes are validated");
            foreach (string property in new[] { "business.production_income.enabled", "business.trading.enabled", "business.tycoon.enabled" })
            {
                var request = new NewGameRequest { Scenario = "default-business", ModeOverrides = { new GameModeAssignment(new Code(property), GameModeValue.FromBoolean(true)) } };
                c.Throws<ContentException>(() => BusinessDefaultsTests.Factory().NewGame(request), "uninstalled transaction/advanced capability cannot advertise an enabled runtime: " + property);
            }
            foreach (string field in new[] { "currency", "opening_cash" })
                c.Throws<ContentException>(() => New(BusinessDefaultsTests.Factory(accounts: Edit("business-accounts.json", root => root.Get("business_accounts")[0].Remove(field))), "accounts-only"), "no hidden account parameter default: " + field);
            c.Throws<ContentException>(() => New(BusinessDefaultsTests.Factory(accounts: Edit("business-accounts.json", root => root.Get("business_accounts")[0].Set("opening_cash", JsonValue.Number("-1")))), "accounts-only"), "negative authored starting money refuses");
            c.Throws<ContentException>(() => New(BusinessDefaultsTests.Factory(accounts: Edit("business-accounts.json", root => root.Get("business_accounts")[0].Set("currency", JsonValue.Of("unit.water")))), "accounts-only"), "physical water is not account currency");
            string noProfile = Edit("business-core.json", root => root.Get("scenarios")[2].Remove("game_mode"));
            c.Throws<ContentException>(() => New(BusinessDefaultsTests.Factory(core: noProfile), "dormant-business"), "selected business requires explicit shared configuration, not old settings inference");
            string wrongOwner = Edit("game-mode-properties.json", root => root.Get("game_mode_properties")[1].Set("capability", JsonValue.Of("proof.business")));
            c.Throws<ContentException>(() => New(BusinessDefaultsTests.Factory(properties: wrongOwner), "accounts-only"), "business consumes only Boolean properties owned by its own capability");
            int composed = 0;
            string omitted = Edit("business-core.json", root => root.Get("scenarios")[0].Set("game_mode", JsonValue.Of("business-money")));
            var missingModule = BusinessDefaultsTests.Factory(core: omitted, proof: new ProbeModule("proof.business", new ModuleVersion(1, 0, 0), compose: composer => composed++));
            var invalid = new NewGameRequest { Scenario = "survival" };
            c.Throws<ContentException>(() => missingModule.PreviewGameMode(invalid), "pure setup refuses enabled flag with unselected capability");
            c.Throws<ContentException>(() => missingModule.NewGame(invalid), "creation refuses same unselected capability before composition");
            c.Equal(0, composed, "missing selected capability cannot enter any composition callback");
        }
    }
}
