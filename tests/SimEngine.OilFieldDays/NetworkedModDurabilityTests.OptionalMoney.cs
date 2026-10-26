using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using Physical = Beep.SimEngine.Tests.PermanentRemovalTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class NetworkedModDurabilityTests
    {
        internal const string NoMoneyRoadCase = "Networked resource-only work: physical road and saved worker holds omit optional money entirely";
        internal const string DormantMoneyRoadCase = "Networked optional money: disabled authored cost bindings retain physical road without accounts";
        internal const string PaidRoadCase = "Networked optional money: native road budget reservation spending and saved physical work remain once-only";
        internal const string UpkeepDurabilityCase = "Networked optional upkeep: original daily fees pause saved boundaries private accounts and debt remain native";
        private static GameFactory OptionalMoneyFactory(int mode, bool upkeep = false)
            => OilAuthoredInfrastructureTests.Factory(financialMode: mode, editBindings: OilAuthoredInfrastructureTests.SelectLineCosts,
                operatingCosts: upkeep ? TycoonOperatingCostTests.Project : (Action<JsonValue>)null, editFixture: root => {
                    var scenarios = root.Get("scenarios");
                    for (int i = 0; i < scenarios.Count; i++) {
                        scenarios[i].Set("seats", JsonValue.Number("2")); scenarios[i].Set("default_seats", JsonValue.Number("2"));
                    }
                });
        private static SimGame OptionalMoneyNew(GameFactory factory)
            => factory.NewGame(new NewGameRequest { Scenario = "frontier", Seed = 17, Seats = { Controller.Human, Controller.Human } });
        private static BusinessView MoneyView(SimGame game) => game.Seat(0).View.Feature<BusinessView>();
        private static BusinessAccountSnapshot MoneyAccount(SimGame game)
        {
            Code code = MoneyView(game).ReadAccount(new PlayerId(0), out var account);
            if (!code.IsNone) throw new InvalidOperationException("actual optional account refused/" + code); return account;
        }
        private static int OptionalRoad(SimGame game)
        {
            var values = new List<int>();
            if (game.Seat(0).View.Feature<TileMapView>().ReadLayer("road", new TileRect(8, 5, 1, 1), values) != SpatialLayerReadStatus.Success)
                throw new InvalidOperationException("original physical road sample missing"); return values.Single();
        }
        private static Fix OptionalCement(SimGame game)
        {
            Fix amount = Fix.Zero;
            foreach (var entity in game.Seat(0).Snapshot.Entities.Where(e => e.Owner == new PlayerId(0))) {
                var facts = new List<Facet>(); game.Seat(0).View.Inspect(entity.Id, facts);
                foreach (var fact in facts.Where(f => f.Key == Storage.FacetStock && f.Subject == "cement")) amount += fact.Value;
            }
            return amount;
        }
        private static string MoneyDiagnostics(SimGame game, int mode) => "at=" + game.Clock.StepIndex + "/road=" + OptionalRoad(game) + "/cement=" + OptionalCement(game) +
            "/money=" + (mode == 2 ? MoneyAccount(game).Balance + "/" + MoneyAccount(game).Reserved : "disabled") +
            "/jobs=" + string.Join(";", Physical.Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.Loaded + "/" + j.Blocker));
        private static Pair MoneyPair(SimGame a, SimGame b, int mode, bool upkeep = false)
        {
            void Observe(SimGame game)
            {
                ulong before = game.Hash();
                if (game.Modules.Contains(TycoonModule.ModuleId) != (mode != 0)) throw new InvalidOperationException("optional money module selected implicitly");
                if (OptionalCement(game) < Fix.FromRatio(19, 2) || OptionalCement(game) > Fix.FromInt(10))
                    throw new InvalidOperationException("money workflow granted or discarded original physical cement");
                if (mode != 0) {
                    var view = MoneyView(game);
                    if (view.HasAccounts != (mode == 2) || view.HasBudgets != (mode == 2) || view.HasValuation || view.HasPurchasing || view.HasGenerationIncome || view.Rules.TycoonEnabled)
                        throw new InvalidOperationException("selected optional capabilities fabricated trading valuation income or advanced Tycoon");
                    if (mode == 2) {
                        var account = MoneyAccount(game); var budgets = new List<MonetaryBudgetReceipt>(); var spending = new List<BusinessExpenseSnapshot>();
                        if (!view.CopyBudgets(new PlayerId(0), budgets).IsNone || !view.CopySpending(new PlayerId(0), spending).IsNone)
                            throw new InvalidOperationException("actual private account records unavailable");
                        BigInteger held = budgets.Aggregate(BigInteger.Zero, (sum, row) => sum + row.Amount.Raw), spent = spending.Aggregate(BigInteger.Zero, (sum, row) => sum + row.AmountRaw);
                        if (held != account.Reserved.Raw || budgets.Count != account.BudgetReservations || account.AvailableRaw != (BigInteger)account.Balance.Raw - held ||
                            account.Balance.Raw + spent != new BigInteger(Fix.FromInt(5000).Raw) || account.GenerationSettlements != 0 || account.GenerationIncomeRaw != 0)
                            throw new InvalidOperationException("sole account budget and spending owners disagree");
                        var foreign = game.Seat(1).View.Feature<BusinessView>();
                        if (foreign.ReadAccount(new PlayerId(0), out var hidden) != Codes.NotOwned || hidden.Policy != null || hidden.Balance != Fix.Zero)
                            throw new InvalidOperationException("foreign account leaked");
                        var denied = new List<MonetaryBudgetReceipt> { default };
                        if (foreign.CopyBudgets(new PlayerId(0), denied) != Codes.NotOwned || denied.Count != 0)
                            throw new InvalidOperationException("foreign budget receipt leaked");
                        if (upkeep) {
                            long step = game.Clock.StepIndex, payments = step <= 3 ? 0 : 1 + (step - 4) / 144;
                            if (account.Balance != Fix.FromInt(checked((int)(5000 - payments * 1650))))
                                throw new InvalidOperationException("original daily fee schedule charged early twice or from another session");
                        }
                    } else if (view.ReadAccount(new PlayerId(0), out var absent) != new Code("business.no-account") || absent.Policy != null)
                        throw new InvalidOperationException("disabled mode fabricated a free account");
                }
                if (before != game.Hash()) throw new InvalidOperationException("optional account observations changed native authority");
            }
            Observe(a); Observe(b); return new Pair(a, b, Observe, g => MoneyDiagnostics(g, mode), 0);
        }
        private static Pair MoneyRestore(Check c, GameFactory factory, Pair source, int mode, bool upkeep = false)
        {
            var restored = MoneyPair(factory.Load(source.A.Save()), factory.Load(source.B.Save()), mode, upkeep);
            c.Equal(source.A.Hash(), restored.A.Hash(), "original mode budget receipts spending material work and clock restore exactly");
            c.Equal(source.B.Hash(), restored.B.Hash(), "saved peer retains sole account and command identities"); return restored;
        }
        private static void OptionalMoneyRoadRun(Check c, int mode)
        {
            var factory = OptionalMoneyFactory(mode); var pair = MoneyPair(OptionalMoneyNew(factory), OptionalMoneyNew(factory), mode);
            Console.WriteLine("MODULES optional-money-" + mode + "=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            c.Equal(60L, pair.A.SimulationTime.ExactTicks(GameDuration.Hours(1)), "original road has a full hour on native minute quantum");
            if (mode != 0) c.Equal(Fix.FromInt(1500), pair.A.Seat(0).View.Feature<Beep.SimEngine.ExtensionProof.OilInfrastructureProofView>().BoundCost("road"), "original JSON road cost stays1500");
            var limits = new ReplayLimits(8 * 1024 * 1024, 5000);
            var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            var path = Json.Parse("{\"space\":\"oil.tiles\",\"path\":[{\"space\":\"oil.tiles\",\"x\":8,\"y\":5}]}");
            int planned = Submit(c, pair, new SubmitPlanCommand { Intent = new PlanIntent("line:road", path, "optional.money", 70, true) });
            Reach(pair, g => Result(g, 0, planned)?.Accepted == true, "paused ordinary road intent admitted without money or completed geometry");
            long plan = Result(pair.A, 0, planned).Value.Arg0;
            c.That(Physical.Plan(pair.A, plan).Order == 0 && OptionalRoad(pair.A) == 0 && OptionalCement(pair.A) == Fix.FromInt(10), "paused native intent grants no work or material consumption");
            var restored = MoneyRestore(c, factory, pair, mode);
            var pendingLeft = restored.A.RecordReplay(limits); var pendingRight = restored.B.RecordReplay(limits);
            foreach (var current in new[] { pair, restored }) Submit(c, current, new ConfigurePlanCommand { Plan = plan, Group = "optional.money", Priority = 70, Paused = false });
            bool Started(SimGame game) => mode == 2 ? MoneyAccount(game).BudgetReservations > 0 && MoneyAccount(game).Balance == Fix.FromInt(5000) : Physical.Jobs(game).Any(j => j.IsHaul && j.Loaded.IsPositive);
            Both(pair, restored, Started, "actual native budget reservation or resource-only loaded supply");
            c.That(OptionalRoad(pair.A) == 0 && Physical.Plan(pair.A, plan).Order > 0, "activated line remains an unfinished physical project");
            BusinessAccountSnapshot reserved = default;
            if (mode == 2) { reserved = MoneyAccount(pair.A); c.That(reserved.Reserved == Fix.FromInt(1500) && reserved.BudgetReservations == 1 && reserved.Balance == Fix.FromInt(5000), "original cost reserved once without a debit"); }
            ReplayPair(c, factory, pendingLeft, pendingRight, restored, limits, 1, "saved paused intent to actual physical reservation or loading replay");
            restored = MoneyRestore(c, factory, pair, mode);
            var startLeft = restored.A.RecordReplay(limits); var startRight = restored.B.RecordReplay(limits);
            int order = Physical.Plan(pair.A, plan).Order;
            pair.A.Net.RequestPause(); Reach(pair, g => g.Clock.Paused, "native sole clock pause during actual material or money reservation");
            long at = pair.A.Clock.StepIndex; ulong frozen = pair.A.Hash();
            Reach(restored, g => g.Clock.StepIndex >= at + 4, "saved peers independently advance original physical supplies and budget lifecycle"); pair.Round();
            c.That(pair.A.Clock.StepIndex == at && pair.A.Hash() == frozen, "paused account and physical world borrow neither time nor state");
            pair.A.Net.RequestResume(); Both(pair, restored, g => InfrastructurePartial(g, order), "original builder arrives supplies and earns positive full-hour road work");
            var partial = LoadNodes(pair.A, order).Single();
            c.That(partial.RequiredTicks == 60 && OptionalRoad(pair.A) == 0, "positive work does not stamp a full-hour road early");
            if (mode == 2) c.That(MoneyAccount(pair.A).Balance == Fix.FromInt(3500) && partial.Costs.Single().Spent && reserved.Balance == Fix.FromInt(5000) && reserved.Reserved == Fix.FromInt(1500),
                "same obligation spends1500 once while retained original reservation remains detached");
            else c.Equal(0, partial.Costs.Count, "resource-only or disabled cost creates no fake monetary obligation");
            ReplayPair(c, factory, startLeft, startRight, restored, limits, 0, "saved physical start through native pause isolated advancement and productive road replay");
            restored = MoneyRestore(c, factory, pair, mode);
            var workLeft = restored.A.RecordReplay(limits); var workRight = restored.B.RecordReplay(limits);
            foreach (var current in new[] { pair, restored }) Submit(c, current, new SetWorkerHeldCommand { Worker = FleetEntity(current.A, "proof.infrastructure.builder"), Held = true });
            Both(pair, restored, g => Result(g, 0, 3)?.Accepted == true, "ordinary hold preserves original builder order and spent binding");
            var held = LoadNodes(pair.A, order).Single().Progress;
            ReplayPair(c, factory, workLeft, workRight, restored, limits, 1, "saved earned road work to normal worker hold replay");
            restored = MoneyRestore(c, factory, pair, mode);
            var heldLeft = restored.A.RecordReplay(limits); var heldRight = restored.B.RecordReplay(limits);
            long until = pair.A.Clock.StepIndex + 5; Both(pair, restored, g => g.Clock.StepIndex >= until, "saved held road leaves native time advancing without work");
            c.That(LoadNodes(pair.A, order).Single().Progress.Equals(held) && OptionalRoad(pair.A) == 0, "held worker earns no remote road work or repeated charges");
            foreach (var current in new[] { pair, restored }) Submit(c, current, new SetWorkerHeldCommand { Worker = FleetEntity(current.A, "proof.infrastructure.builder"), Held = false });
            Both(pair, restored, g => Physical.Plan(g, plan).State == PlanState.Done, "original builder resumes and completes earned physical road");
            c.That(OptionalRoad(pair.A) == 1 && OptionalCement(pair.A) == Fix.FromRatio(19, 2) && pair.A.Seat(0).Snapshot.Entities.All(e => e.Def != "oil.line-marker"), "one real road spends half-unit physical cement and retires native cursor");
            if (mode == 2) c.That(MoneyAccount(pair.A).Balance == Fix.FromInt(3500) && MoneyAccount(pair.A).BudgetReservations == 0 && MoneyAccount(pair.A).Reserved == Fix.Zero,
                "final original account has one1500 debit and no surviving budget receipt");
            ReplayPair(c, factory, heldLeft, heldRight, restored, limits, 1, "saved held road through normal resume full work and once-only spending replay");
            ReplayPair(c, factory, left, right, pair, limits, 4, "full optional-money or resource-only physical road player stream replay");
            c.That(pair.Stalls > 0 && pair.ReversedBatches > 0 && pair.A.Hash() == restored.B.Hash(), "four actually advanced physical account worlds converge through native shuffled transport");
            c.Equal(pair.A.Hash(), factory.Load(pair.A.Save()).Hash(), "completed native road mode and optional account checkpoint");
        }
        private static void UpkeepDurabilityRun(Check c)
        {
            var factory = OptionalMoneyFactory(2, upkeep: true); var pair = MoneyPair(OptionalMoneyNew(factory), OptionalMoneyNew(factory), 2, true);
            Console.WriteLine("MODULES optional-upkeep=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            c.Equal(144L, pair.A.SimulationTime.ExactTicks(GameDuration.Days(1)), "original upkeep uses one ten-minute game quantum");
            Fix daily = Fix.Zero;
            foreach (var name in new[] { "proof.infrastructure.source", "proof.infrastructure.builder", "proof.infrastructure.truck" }) {
                var rows = new List<EntityOperatingCostView>(); c.Equal(Code.None, MoneyView(pair.A).CopyOperatingCosts(FleetEntity(pair.A, name), rows), "actual original cost metadata exposed");
                c.That(rows.Count == 1 && rows[0].Cost.Applicability == MonetaryCostApplicability.Enabled && rows[0].Period.Equals(GameDuration.Days(1)) && rows[0].Offset.Equals(GameDuration.Minutes(30)),
                    "actual daily period and half-hour offset are JSON contracts not a host timer"); daily += rows[0].Cost.AuthoredAmount;
            }
            c.Equal(Fix.FromInt(1650), daily, "original1500 plus native vehicle upkeep amounts stay unchanged");
            var limits = new ReplayLimits(8 * 1024 * 1024, 5000); var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            Reach(pair, g => g.Clock.StepIndex == 2, "actual pre-due schedule prefix");
            var restored = MoneyRestore(c, factory, pair, 2, true); var dueLeft = restored.A.RecordReplay(limits); var dueRight = restored.B.RecordReplay(limits);
            pair.A.Net.RequestPause(); Reach(pair, g => g.Clock.Paused, "native pause before first due interval completes");
            long at = pair.A.Clock.StepIndex; ulong frozen = pair.A.Hash();
            Reach(restored, g => g.Clock.StepIndex >= 4, "separate saved peers earn actual first daily fee"); pair.Round();
            c.That(at == 3 && pair.A.Hash() == frozen && MoneyAccount(pair.A).Balance == Fix.FromInt(5000) && MoneyAccount(restored.A).Balance == Fix.FromInt(3350),
                "paused native world owes no unearned interval while separate saved schedule charges once");
            pair.A.Net.RequestResume(); Both(pair, restored, g => g.Clock.StepIndex >= 4, "native resumed schedule catches up to same original first payment");
            ReplayPair(c, factory, dueLeft, dueRight, restored, limits, 0, "saved pre-due clock through native pause isolation and once-only first charge replay");
            restored = MoneyRestore(c, factory, pair, 2, true); var firstLeft = restored.A.RecordReplay(limits); var firstRight = restored.B.RecordReplay(limits);
            Both(pair, restored, g => g.Clock.StepIndex >= 147, "original daily recurrence reaches next pre-due boundary");
            c.Equal(Fix.FromInt(3350), MoneyAccount(pair.A).Balance, "no repeated charge between original daily boundaries");
            ReplayPair(c, factory, firstLeft, firstRight, restored, limits, 0, "saved paid schedule to next actual pre-due boundary replay");
            restored = MoneyRestore(c, factory, pair, 2, true); var debtLeft = restored.A.RecordReplay(limits); var debtRight = restored.B.RecordReplay(limits);
            Both(pair, restored, g => g.Clock.StepIndex >= 436, "full original recurring costs continue across exact due boundaries into authored debt");
            c.That(MoneyAccount(pair.A).Balance == Fix.FromInt(-1600) && MoneyAccount(pair.A).BudgetReservations == 0 && OptionalRoad(pair.A) == 0 && OptionalCement(pair.A) == Fix.FromInt(10),
                "four native scheduled fees preserve existing into-debt behavior without work resources or a fabricated purchase");
            var spending = new List<BusinessExpenseSnapshot>(); MoneyView(pair.A).CopySpending(new PlayerId(0), spending);
            c.That(spending.Count == 1 && spending.Single().Reason == new Code("ledger.upkeep") && spending.Single().AmountRaw == new BigInteger(Fix.FromInt(6600).Raw),
                "original account owner records exactly four actual1650 scheduled payments");
            ReplayPair(c, factory, debtLeft, debtRight, restored, limits, 0, "saved recurring schedule through actual debt boundary replay");
            ReplayPair(c, factory, left, right, pair, limits, 0, "full authored upkeep sole-clock stream replay");
            c.That(pair.Stalls > 0 && pair.ReversedBatches > 0 && pair.A.Hash() == restored.B.Hash(), "four actually advanced schedule/account sessions converge");
            c.Equal(pair.A.Hash(), factory.Load(pair.A.Save()).Hash(), "negative native balance and paid schedule checkpoint");
        }
    }
}
