using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class OilFinancialOwnershipTests
    {
        public const string CaseName = "Oil finance: canonical modes and sole optional account ownership";
        public static void Register() => TestRunner.Add(CaseName, Run);
        private static void Run(Check check)
        {
            var openings = new Dictionary<string, int?> { ["frontier"] = 6000000, ["adventure"] = 3000000,
                ["continent"] = 6000000, ["showcase"] = 8000000, ["sandbox"] = null, ["gulf"] = null };
            foreach (var entry in openings)
            {
                var factory = OilAuthoredInfrastructureTests.Factory(authoredScenario: entry.Key,
                    editBindings: OilAuthoredInfrastructureTests.SelectLineCosts);
                var game = OilAuthoredInfrastructureTests.New(factory);
                var idle = OilAuthoredInfrastructureTests.New(factory); ulong idleHash = idle.Hash();
                check.Equal(entry.Value.HasValue, game.Modules.Contains(TycoonModule.ModuleId), "actual selected optional module");
                check.Equal(60L, game.SimulationTime.ExactTicks(GameDuration.Hours(1)), "canonical minute simulation quantum");
                check.Equal(10, game.Clock.TicksPerFrame, "original ten-minute game frame");
                BusinessView business = entry.Value.HasValue ? game.Seat(0).View.Feature<BusinessView>() : null;
                if (business != null)
                {
                    check.That(business.HasAccounts && business.HasBudgets, "same existing account/budget provider");
                    check.That(business.Rules.MoneyEnabled && business.Rules.ConstructionCostsEnabled
                        && business.Rules.ManufacturingCostsEnabled && business.Rules.RunningCostsEnabled
                        && business.Rules.TradingEnabled && !business.Rules.TycoonEnabled, "authored fixed-default capabilities, no advanced tycoon substitution");
                    check.Equal(Code.None, business.ReadAccount(new PlayerId(0), out var account), "public native account query");
                    check.Equal(Fix.FromInt(entry.Value.Value), account.Balance, "original opening amount preserved");
                    check.Equal("oil.accounts." + entry.Key, account.Policy, "actual master-listed policy");
                    check.Equal(new UnitId("unit.oil.currency"), account.Currency, "authored currency identity");
                }
                game.Seat(0).Submit(new LayLineCommand { Line = "road", Tiles = new List<TileCoord> { new TileCoord(8, 5) } });
                game.Update(); var twin = factory.Load(game.Save()); bool completed = false;
                for (int i = 0; i < 80; i++)
                {
                    game.Update(); twin.Update(); check.Equal(game.Hash(), twin.Hash(), "canonical mode/accounts saved physical continuation");
                    var cells = new List<int>();
                    game.Seat(0).View.Feature<TileMapView>().ReadLayer("road", new TileRect(8, 5, 1, 1), cells);
                    if (cells.Single() == 1) { completed = true; break; }
                }
                check.That(completed, "physical line completes in bounded native fixture");
                if (business != null)
                {
                    check.Equal(Code.None, business.ReadAccount(new PlayerId(0), out var account), "actual final account");
                    check.Equal(Fix.FromInt(entry.Value.Value - 1500), account.Balance, "same canonical road cost charged once");
                    check.Equal(0, account.BudgetReservations, "original held obligation retired");
                }
                check.Equal(idleHash, idle.Hash(), "independent account session remains untouched");
                check.That(game.Fault == null && twin.Fault == null, "healthy optional-money native sessions");
            }
        }
    }
}
