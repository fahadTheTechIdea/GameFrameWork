using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class OilAuthoredInfrastructureTests
    {
        public const string CaseName = "Oil infrastructure: canonical contracts physical road and connectivity-only power";
        public static void Register() => TestRunner.Add(CaseName, Run);
        // Explicit fixture projection: default infrastructure work includes only live line activities.
        internal static void SelectLineCosts(JsonValue root)
        {
            var selected = JsonValue.NewArray(); var bindings = root.Get("activity_cost_bindings");
            for (int i = 0; i < bindings.Count; i++)
                if (bindings[i].Get("activity").Text.StartsWith("oil.line.", StringComparison.Ordinal)) selected.Add(bindings[i]);
            root.Set("activity_cost_bindings", selected);
        }
        internal static GameFactory Factory(Action<JsonValue> editNavigation = null, int financialMode = 0, Action<JsonValue> editBindings = null,
            string authoredScenario = null, Action<JsonValue> operatingCosts = null, Action<JsonValue> editFixture = null,
            Action<PreloadedFileSource, JsonValue> editSources = null)
        {
            var definition = new GameDefinition("oil-infrastructure-proof", new GameModule[]
            { new ContentAccessModule(), new InventoryModule(), new MotionModule(), new TileNavigationModule(),
              new TileConditionsModule(), new WorkModule(), new InfrastructureModule(), new LogisticsModule(),
              new WorldSetupModule(), new TycoonModule(), new OperationsModule(), new TransformationModule(), new ConstructionModule(), new OilInfrastructureProofModule() });
            // Construction is available to explicitly authored integration fixtures, never activated implicitly.
            var master = Json.Parse(OilFieldDaysProofContent.Read("oil-infrastructure-master.json"));
            if (operatingCosts != null)
                for (int i = 0; i < master.Get("modules").Count; i++)
                    if (master.Get("modules")[i].Get("id").Text == TycoonModule.ModuleId)
                        master.Get("modules")[i].Get("documents").Add(JsonValue.Of("oil-business-operating-costs.json"));
            var files = new PreloadedFileSource().Add("manifest.json", master.ToString());
            if (operatingCosts != null)
            {
                var fees = Json.Parse(OilFieldDaysProofContent.Read("oil-business-operating-costs.json"));
                operatingCosts(fees); files.Add("oil-business-operating-costs.json", fees.ToString());
            }
            foreach (string file in new[] { "oil-infrastructure-fixture.json", "oil-items.json", "oil-spatial-world.json",
                "oil-navigation.json", "oil-networks.json", "oil-worker-roles.json", "oil-states.json",
                "oil-game-modes.json", "oil-business-accounts.json", "oil-business-costs.json", "oil-infrastructure-accounts.json" })
            {
                string text = OilFieldDaysProofContent.Read(file);
                if (file == "oil-navigation.json" && editNavigation != null)
                { var root = Json.Parse(text); editNavigation(root); text = root.ToString(); }
                if (file == "oil-business-costs.json" && editBindings != null)
                { var root = Json.Parse(text); editBindings(root); text = root.ToString(); }
                if (file == "oil-infrastructure-fixture.json" && financialMode != 0)
                {
                    var root = Json.Parse(text);
                    if (financialMode == 2) root.Get("game_modes")[0].Set("properties",
                        Json.Parse("{\"business.money.enabled\":true,\"business.construction_costs.enabled\":true}"));
                    for (int i = 0; i < root.Get("scenarios").Count; i++)
                    {
                        var scenario = root.Get("scenarios")[i];
                        scenario.Get("modules").Add(JsonValue.Of(TycoonModule.ModuleId));
                        scenario.Get("settings").Set(TycoonModule.ModuleId,
                            Json.Parse("{\"account_policy\":\"proof.infrastructure.accounts\"}"));
                    }
                    text = root.ToString();
                }
                if (file == "oil-infrastructure-fixture.json" && authoredScenario != null)
                {
                    var root = Json.Parse(text);
                    var modes = Json.Parse(OilFieldDaysProofContent.Read("oil-game-modes.json"));
                    JsonValue selected = null;
                    for (int i = 0; i < modes.Get("game_modes").Count; i++)
                        if (modes.Get("game_modes")[i].Get("id").Text == "oil.mode." + authoredScenario) selected = modes.Get("game_modes")[i];
                    if (selected == null) throw new InvalidOperationException("missing authored game mode");
                    bool money = selected.Get("properties").Get("business.money.enabled").Bool;
                    for (int i = 0; i < root.Get("scenarios").Count; i++)
                    {
                        var scenario = root.Get("scenarios")[i];
                        scenario.Set("game_mode", JsonValue.Of("oil.mode." + authoredScenario));
                        scenario.Set("time", JsonValue.Of("oil.time." + authoredScenario));
                        if (money)
                        {
                            scenario.Get("modules").Add(JsonValue.Of(TycoonModule.ModuleId));
                            scenario.Get("settings").Set(TycoonModule.ModuleId,
                                Json.Parse("{\"account_policy\":\"oil.accounts." + authoredScenario + "\"}"));
                        }
                    }
                    text = root.ToString();
                }
                if (file == "oil-infrastructure-fixture.json" && operatingCosts != null)
                {
                    var root = Json.Parse(text);
                    root.Get("time_definitions")[0].Set("quantum", Json.Parse("{\"numerator\":10,\"denominator\":1,\"unit\":\"minute\"}"));
                    if (financialMode == 2)
                        root.Get("game_modes")[0].Get("properties").Set("business.running_costs.enabled", JsonValue.Of(true));
                    text = root.ToString();
                }
                if (file == "oil-infrastructure-fixture.json" && editFixture != null)
                { var root = Json.Parse(text); editFixture(root); text = root.ToString(); }
                files.Add(file, text);
            }
            editSources?.Invoke(files, master);
            files.Add("manifest.json", master.ToString());
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        internal static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest
        { Scenario = "frontier", Seed = 17, Seats = new List<Controller> { Controller.Human } });
        private static int Road(SimGame game)
        {
            var values = new List<int>();
            game.Seat(0).View.Feature<TileMapView>().ReadLayer("road", new TileRect(8, 5, 1, 1), values);
            return values.Single();
        }
        private static Fix SourceStock(SimGame game)
        {
            var source = game.Seat(0).Snapshot.Entities.Single(e => e.Def == "proof.infrastructure.source");
            if (!game.Seat(0).View.TryFindResource("cement", out ResourceId resource)
                || !game.Seat(0).View.Feature<InventoryView>().ReadResourcePort(source.Id,
                    new Code("proof.source.stock"), resource, out var stock).IsNone)
                throw new InvalidOperationException("missing public source stock facts");
            return stock.Stock.Lines.Single(r => r.Resource == resource).Amount;
        }
        private static List<PlanView> Plans(SimGame game)
        { var values = new List<PlanView>(); game.Seat(0).View.Feature<WorkView>().CopyPlans(values); return values; }
        private static void Run(Check check)
        {
            var factory = Factory(); var game = New(factory); var idle = New(factory); ulong idleHash = idle.Hash();
            var declarations = game.Seat(0).View.Feature<OilInfrastructureProofView>(); ulong before = game.Hash();
            check.Equal(8, declarations.LineCount, "all canonical line definitions admitted");
            check.Equal(0, declarations.CarriedResourceCount("power"), "no invented power resource");
            check.Equal(2, declarations.CarriedResourceCount("oil"), "derived native oil selection");
            check.Equal(3, declarations.CarriedResourceCount("gas"), "derived native gas selection");
            foreach (string line in new[] { "road", "road_paved", "pipe_oil", "pipe_gas", "pipe_water", "power_line", "pipe_subsea_oil", "pipe_subsea_gas" })
            {
                check.That(game.SimulationTime.Ticks(declarations.Duration(line)) > 0, "authored exact duration retained without financial dependencies");
            }
            check.Equal(60L, game.SimulationTime.Ticks(declarations.Duration("road")), "original one-hour road duration");
            check.Equal(Code.None, declarations.Placement("road", new TileCoord(5, 1)), "road permits wet terrain");
            check.Equal(new Code("oil.on-mountain"), declarations.Placement("road", new TileCoord(3, 1)), "road refuses dry mountain");
            check.Equal(Code.None, declarations.Placement("road", new TileCoord(4, 1)), "nested any allows wet mountain");
            check.Equal(new Code("oil.line-land-blocked"), declarations.Placement("pipe_oil", new TileCoord(5, 1)), "land pipe refuses water");
            check.Equal(new Code("oil.line-land-blocked"), declarations.Placement("pipe_oil", new TileCoord(3, 1)), "land pipe refuses mountain");
            check.Equal(Code.None, declarations.Placement("pipe_subsea_oil", new TileCoord(4, 1)), "water pipe permits wet mountain");
            check.Equal(new Code("oil.on-land"), declarations.Placement("pipe_subsea_oil", new TileCoord(2, 1)), "water pipe refuses land");
            var rate = declarations.Rate("oil", "crude");
            check.Equal(Fix.FromInt(40), rate.Quantity, "original hourly native quantity");
            check.Equal(1L, rate.PeriodNumerator, "explicit hourly numerator");
            check.Equal(1L, rate.PeriodDenominator, "explicit hourly denominator");
            check.Equal(DurationUnit.Hour, rate.PeriodUnit, "hourly period, not per update");
            check.Equal(before, game.Hash(), "declaration and policy reads pure");
            check.That(!game.Modules.Contains(TycoonModule.ModuleId), "cost metadata does not activate business or a ledger");
            game.Update();
            var infrastructure = game.Seat(0).View.Feature<InfrastructureView>();
            var a = game.Seat(0).Snapshot.Entities.Single(e => e.Def == "proof.infrastructure.power-a").Id;
            var b = game.Seat(0).Snapshot.Entities.Single(e => e.Def == "proof.infrastructure.power-b").Id;
            check.Equal(Code.None, infrastructure.ReadConnection(a, "power", out var pa), "native connectivity-only endpoint A");
            check.Equal(Code.None, infrastructure.ReadConnection(b, "power", out var pb), "native connectivity-only endpoint B");
            check.Equal(pa.Endpoint, pb.Endpoint, "same actual power topology");
            var pools = new List<NetworkPoolView>();
            check.Equal(Code.None, infrastructure.CopyPools("power", pools), "native topology pool facts");
            check.Equal(1, pools.Count, "one native power component");
            check.Equal(0, pools.Single().Stock.Count, "connectivity creates no goods");
            var command = new LayLineCommand { Line = "road", Tiles = new List<TileCoord> { new TileCoord(8, 5) } };
            check.Equal(Code.None, game.Seat(0).Preview(command), "same native command admission");
            game.Seat(0).Submit(command); game.Update();
            check.Equal(0, Road(game), "command creates a project, not an instant road");
            long start = game.Clock.StepIndex;
            var twin = factory.Load(game.Save());
            bool finished = false;
            for (int step = 0; step < 800; step++)
            {
                game.Update(); twin.Update();
                check.Equal(game.Hash(), twin.Hash(), "saved native supplied road continuation");
                if (Road(game) != 0) { finished = true; break; }
            }
            if (!finished) throw new InvalidOperationException("bounded road stage stalled: step=" + game.Clock.StepIndex
                + ";plans=" + string.Join(",", Plans(game).Select(p => p.State.ToString())));
            check.That(game.Clock.StepIndex - start >= 60, "builder earns original full hour plus physical supply/travel");
            check.Equal(1, Road(game), "original line effect stamps the road");
            check.Equal(Fix.FromRatio(19, 2), SourceStock(game), "actual half-unit cement consumed");
            check.Equal(0, game.Seat(0).Snapshot.Entities.Count(e => e.Def == "oil.line-marker"), "native finished cursor removed");
            check.Equal(idleHash, idle.Hash(), "physical work isolated from independent session");
            check.That(game.Fault == null && twin.Fault == null, "healthy saved physical sessions");
            check.Throws<ContentException>(() => New(Factory(root => root.Get("tile_condition_groups")[0]
                .Set("conditions", Json.Parse("[\"oil.terrain.not-mountain\"]")))), "condition cycle refused before initialization");
            check.Throws<ContentException>(() => New(Factory(root => root.Get("tile_condition_groups")[0]
                .Set("conditions", Json.Parse("[\"missing.condition\"]")))), "missing condition refused before initialization");
            FinancialBindings(check, 1); FinancialBindings(check, 2);
            check.Throws<ContentException>(() => New(Factory(null, 1, root => root.Get("activity_cost_bindings")[0]
                .Set("node", JsonValue.Of("missing.node")))), "cost binding refuses missing node");
            check.Throws<ContentException>(() => New(Factory(null, 1, root => root.Get("activity_cost_bindings")[0]
                .Set("activity", JsonValue.Of("missing.activity")))), "cost binding refuses missing activity");
            check.Throws<ContentException>(() => New(Factory(null, 1, root => root.Get("activity_cost_bindings")[0]
                .Set("cost", JsonValue.Of("missing.cost")))), "cost binding refuses missing requirement");
            check.Throws<ContentException>(() => New(Factory(null, 1, root => {
                var duplicate = Json.Parse(root.Get("activity_cost_bindings")[0].ToString());
                duplicate.Set("id", JsonValue.Of("proof.duplicate.binding")); root.Get("activity_cost_bindings").Add(duplicate);
            })), "cost binding refuses duplicate activity/node attachment");
        }
        private static void FinancialBindings(Check check, int mode)
        {
            var factory = Factory(null, mode); var game = New(factory);
            var business = game.Seat(0).View.Feature<BusinessView>();
            check.Equal(mode == 2, business.HasAccounts, "accounts only when money selected");
            check.Equal(mode == 2, business.HasBudgets, "existing budget capability only when money selected");
            var declarations = game.Seat(0).View.Feature<OilInfrastructureProofView>();
            var amounts = new Dictionary<string, int> { ["road"] = 1500, ["road_paved"] = 4000,
                ["pipe_oil"] = 2200, ["pipe_gas"] = 2600, ["pipe_water"] = 1400,
                ["power_line"] = 1800, ["pipe_subsea_oil"] = 6500, ["pipe_subsea_gas"] = 7500 };
            foreach (var amount in amounts) check.Equal(Fix.FromInt(amount.Value), declarations.BoundCost(amount.Key), "original selected cost amount");
            game.Seat(0).Submit(new LayLineCommand { Line = "road", Tiles = new List<TileCoord> { new TileCoord(8, 5) } });
            game.Update(); var twin = factory.Load(game.Save()); bool finished = false, savedReserved = false, savedSpent = false;
            for (int i = 0; i < 800; i++)
            {
                game.Update(); twin.Update(); check.Equal(game.Hash(), twin.Hash(), "selected binding saved physical continuation");
                if (mode == 2)
                {
                    check.Equal(Code.None, business.ReadAccount(new PlayerId(0), out var current), "live original account facts");
                    if (!savedReserved && current.BudgetReservations > 0)
                    {
                        check.Equal(Fix.FromInt(1500), current.Reserved, "original amount reserved atomically");
                        check.Equal(Fix.FromInt(5000), current.Balance, "reservation is not a second debit");
                        twin = factory.Load(game.Save()); check.Equal(game.Hash(), twin.Hash(), "bound reserved occurrence restored"); savedReserved = true;
                    }
                    if (!savedSpent && current.Balance == Fix.FromInt(3500))
                    {
                        twin = factory.Load(game.Save()); check.Equal(game.Hash(), twin.Hash(), "bound spent occurrence restored"); savedSpent = true;
                    }
                }
                if (Road(game) == 1) { finished = true; break; }
            }
            check.That(finished, "selected binding physical road completes within bound");
            check.Equal(Fix.FromRatio(19, 2), SourceStock(game), "same actual physical requirement with optional cost");
            if (mode == 2)
            {
                check.That(savedReserved && savedSpent, "save/load covers original reservation and spent lifetimes");
                check.Equal(Code.None, business.ReadAccount(new PlayerId(0), out var account), "original account view");
                check.Equal(Fix.FromInt(3500), account.Balance, "original road amount charged exactly once");
                var budgets = new List<MonetaryBudgetReceipt>();
                check.Equal(Code.None, business.CopyBudgets(new PlayerId(0), budgets), "original budget view");
                check.Equal(0, budgets.Count, "spent receipt retired by original owner");
            }
            check.That(game.Fault == null && twin.Fault == null, "optional cost continuation healthy");
        }
    }
}
