using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class TycoonOperatingCostTests
    {
        public const string CaseName = "Tycoon upkeep: authored periodic costs debt pause saves and disabled money";
        public static void Register() => TestRunner.Add(CaseName, Run);
        // Fixture authoring projection only: actual Oil rates/times are bound to existing native test prototypes.
        internal static void Project(JsonValue root)
        {
            var selected = JsonValue.NewArray();
            var targets = new Dictionary<string, string> { ["company_base"] = "proof.infrastructure.source",
                ["bulldozer"] = "proof.infrastructure.builder", ["haul_truck"] = "proof.infrastructure.truck" };
            var rows = root.Get("entity_operating_costs");
            for (int i = 0; i < rows.Count; i++)
                if (targets.TryGetValue(rows[i].Get("target").Text, out string target))
                { rows[i].Set("target", JsonValue.Of(target)); selected.Add(rows[i]); }
            root.Set("entity_operating_costs", selected);
        }
        private static void Run(Check check)
        {
            var authored = Json.Parse(OilFieldDaysProofContent.Read("oil-business-operating-costs.json"));
            check.Equal(38, Enumerable.Range(0, authored.Get("entity_operating_costs").Count)
                .Count(i => authored.Get("entity_operating_costs")[i].Get("id").Text.StartsWith("oil.upkeep.")), "all canonical upkeep policies migrated");
            foreach (int mode in new[] { 0, 1, 2 })
            {
                var factory = OilAuthoredInfrastructureTests.Factory(financialMode: mode,
                    editBindings: OilAuthoredInfrastructureTests.SelectLineCosts, operatingCosts: Project);
                var game = OilAuthoredInfrastructureTests.New(factory); var twin = factory.Load(game.Save());
                var idle = OilAuthoredInfrastructureTests.New(factory); ulong idleHash = idle.Hash();
                BusinessView business = mode == 0 ? null : game.Seat(0).View.Feature<BusinessView>();
                check.Equal(mode != 0, game.Modules.Contains(TycoonModule.ModuleId), "optional module selected only when authored");
                check.Equal(144L, game.SimulationTime.ExactTicks(GameDuration.Days(1)), "one exact ten-minute game clock");
                if (business != null)
                {
                    var missing = new List<EntityOperatingCostView> { default };
                    check.Equal(Codes.NotFound, business.CopyOperatingCosts(new EntityId(int.MaxValue), missing), "unknown target has no disclosed fee facts");
                    check.Equal(0, missing.Count, "refused query clears destination");
                    foreach (var entity in game.Seat(0).Snapshot.Entities.Where(e => e.Def == "proof.infrastructure.source"
                        || e.Def == "proof.infrastructure.builder" || e.Def == "proof.infrastructure.truck"))
                    {
                        var views = new List<EntityOperatingCostView>(); ulong before = game.Hash();
                        check.Equal(Code.None, business.CopyOperatingCosts(entity.Id, views), "engine-calculated fee view");
                        check.Equal(1, views.Count, "one actual authored cost per selected prototype");
                        check.Equal(mode == 2 ? MonetaryCostApplicability.Enabled : MonetaryCostApplicability.DisabledByMode,
                            views.Single().Cost.Applicability, "same existing monetary mode decision");
                        check.Equal(before, game.Hash(), "fee view is pure");
                    }
                }
                bool savedBefore = false, savedAfter = false, savedDebt = false;
                for (int i = 0; i < 450; i++)
                {
                    game.Update(); twin.Update(); check.Equal(game.Hash(), twin.Hash(), "periodic costs saved twin identity");
                    long step = game.Clock.StepIndex;
                    if (mode == 2)
                    {
                        check.Equal(Code.None, business.ReadAccount(new PlayerId(0), out var account), "actual native balance");
                        // TickInfo is the interval's start coordinate; StepIndex counts completed intervals.
                        long payments = step <= 3 ? 0 : 1 + (step - 4) / 144;
                        check.Equal(Fix.FromInt(checked((int)(5000 - payments * 1650))), account.Balance, "original rates charged only at offset and daily boundaries");
                        if (step == 3) { twin = factory.Load(game.Save()); savedBefore = true; }
                        if (step == 4) { twin = factory.Load(game.Save()); savedAfter = true; }
                        if (step == 436) { twin = factory.Load(game.Save()); savedDebt = true; }
                    }
                    if (step == 40)
                    {
                        game.Clock.Pause(); twin.Clock.Pause(); ulong paused = game.Hash();
                        for (int wait = 0; wait < 5; wait++) { game.Update(); twin.Update(); }
                        check.Equal(40L, game.Clock.StepIndex, "pause stops the sole clock");
                        check.Equal(paused, game.Hash(), "pause incurs no charges or simulation changes");
                        game.Clock.Resume(); twin.Clock.Resume();
                    }
                }
                if (mode == 2)
                {
                    check.That(savedBefore && savedAfter && savedDebt, "save/load covers due boundary and negative balance without a second timer");
                    check.Equal(Code.None, business.ReadAccount(new PlayerId(0), out var account), "final native account");
                    check.Equal(Fix.FromInt(-1600), account.Balance, "original into-debt upkeep behavior retained");
                    var spending = new List<BusinessExpenseSnapshot>();
                    check.Equal(Code.None, business.CopySpending(new PlayerId(0), spending), "scheduled costs publish original account history");
                    check.Equal(1, spending.Count, "one cumulative actual upkeep reason");
                    check.Equal(new Code("ledger.upkeep"), spending.Single().Reason, "original authored upkeep ledger reason retained");
                    check.Equal((System.Numerics.BigInteger)Fix.FromInt(6600).Raw, spending.Single().AmountRaw, "four actual scheduled payments recorded once");
                }
                if (mode == 1) check.That(!business.HasAccounts && !business.HasBudgets, "disabled fees require no account or budget fallback");
                check.Equal(idleHash, idle.Hash(), "isolated session does not receive another session's charges");
                check.That(game.Fault == null && twin.Fault == null, "periodic cost sessions remain healthy");
            }
            check.Throws<ContentException>(() => OilAuthoredInfrastructureTests.New(OilAuthoredInfrastructureTests.Factory(financialMode: 2,
                editBindings: OilAuthoredInfrastructureTests.SelectLineCosts,
                operatingCosts: root => { Project(root); root.Get("entity_operating_costs")[0].Set("target", JsonValue.Of("missing.target")); })), "missing cost target refused before initialization");
            check.Throws<ContentException>(() => OilAuthoredInfrastructureTests.New(OilAuthoredInfrastructureTests.Factory(financialMode: 2,
                editBindings: OilAuthoredInfrastructureTests.SelectLineCosts,
                operatingCosts: root => { Project(root); root.Get("entity_operating_costs")[0].Set("offset", Json.Parse("{\"numerator\":1,\"denominator\":1,\"unit\":\"day\"}")); })), "offset outside period refused before initialization");
        }
    }
}
