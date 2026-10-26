using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static partial class NetworkedModDurabilityTests
    {
        internal const string InventoryControlCase = "Networked inventory control: native stock, saved numeric hysteresis and earned manufacturing restart";
        internal const string IndependentInventoryControlCase = "Networked inventory control contracts: independent stock, saved numeric hysteresis and earned manufacturing restart";
        private static readonly Code InventoryFeed = new Code("feed"), InventoryProcess = new Code("transformation");
        private static OperationsView InventoryOperations(SimGame game) => game.Seat(0).View.Feature<OperationsView>();
        private static EntityId InventoryEntity(SimGame game, string name, int seat = 0)
            => game.Seat(seat).Snapshot.Entities.Single(e => e.Owner == new PlayerId(seat) && e.Def == name).Id;
        private static AutomationSignalSnapshot InventorySignal(SimGame game, string name, string signal)
        {
            Code refusal = InventoryOperations(game).ReadSignal(InventoryEntity(game, name), InventoryFeed, signal, out var value);
            if (!refusal.IsNone || !value.Available || !value.Value.HasValue)
                throw new InvalidOperationException("actual inventory signal/" + name + "/" + signal + "/" + refusal + "/" + value.Blocker);
            return value;
        }
        private static TransformationSnapshot InventoryState(SimGame game, string name)
        {
            if (!game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(InventoryEntity(game, name), InventoryProcess, out var state))
                throw new InvalidOperationException("authored inventory process missing");
            return state;
        }
        private static bool InventoryEnabled(SimGame game, string name)
        {
            if (!InventoryOperations(game).TryGetControl(InventoryEntity(game, name), InventoryProcess, out var control))
                throw new InvalidOperationException("authored inventory operation missing");
            return control.Enabled;
        }
        private static AutomationRuleSnapshot InventoryRule(SimGame game)
            => InventoryOperations(game).TryGetAutomationRule(new PlayerId(0), "feed", out var rule) ? rule : null;
        private static int InventoryTokens(SimGame game) => game.Seat(0).Snapshot.Entities.Count(e => e.Owner == new PlayerId(0) && e.Def == "token");
        private static ResourceId InventorySupply(SimGame game)
        {
            if (!game.Seat(0).View.TryFindResource("supply", out var resource)) throw new InvalidOperationException("authored supply missing");
            return resource;
        }
        // Player policy intent is copied from the public immutable authored profile.
        // All comparisons, dwell, hysteresis and deferred actions remain in AutomationSystem.
        private static ConfigureAutomationCommand InventoryConfigure(SimGame game, string name, string profile, bool enabled = true)
        {
            if (!InventoryOperations(game).TryGetAutomationProfile(profile, out var authored)) throw new InvalidOperationException("authored inventory policy missing");
            return new ConfigureAutomationCommand { Rule = "feed", Settings = new AutomationSettings(profile, enabled,
                new ControlBinding(InventoryEntity(game, name), InventoryProcess),
                new Dictionary<string, ControlBinding> { ["stock"] = new ControlBinding(InventoryEntity(game, name), InventoryFeed) },
                authored.Thresholds.ToDictionary(row => row.Key, row => row.Value.Initial, StringComparer.Ordinal), authored.WhenTrue, authored.WhenFalse) };
        }
        private static string InventoryDiagnostics(SimGame game, string name)
        {
            var state = InventoryState(game, name); var rule = InventoryRule(game);
            return "at=" + game.Clock.StepIndex + "/stock=" + InventorySignal(game, name, "available.amount").Value +
                "/queue=" + state.QueueCount + "/progress=" + state.Progress.WholeTicks + "/required=" + state.Required +
                "/wip=" + state.Wip.Execution + "/enabled=" + InventoryEnabled(game, name) +
                "/candidate=" + rule?.Candidate + "/pending=" + rule?.PendingDecision + "/tokens=" + InventoryTokens(game);
        }
        private static Pair InventoryPair(SimGame a, SimGame b, string name)
        {
            var observed = new Dictionary<SimGame, long>();
            void Observe(SimGame game)
            {
                if (observed.TryGetValue(game, out long prior) && prior == game.Clock.StepIndex) return;
                BigInteger stock = 0, consumed = 0, produced = 0;
                foreach (var entity in game.Seat(0).Snapshot.Entities.Where(e => e.Owner == new PlayerId(0))) {
                    var facets = new List<Facet>(); game.Seat(0).View.Inspect(entity.Id, facets);
                    foreach (var facet in facets.Where(f => f.Key == Storage.FacetStock && f.Subject == "supply")) stock += facet.Value.Raw;
                    if (game.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(entity.Id, InventoryProcess, out var process))
                        stock += process.Wip.Stock.AmountOf(InventorySupply(game)).Raw;
                }
                foreach (var row in game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).Aggregates) {
                    if (row.Kind == ResourceFlowKind.AuthoredCreation || row.Kind == ResourceFlowKind.StockPurchase ||
                        row.Kind == ResourceFlowKind.Lost || row.Kind == ResourceFlowKind.Destroyed)
                        throw new InvalidOperationException("inventory policy fabricated or discarded resources");
                    if (row.Resource != InventorySupply(game)) continue;
                    if (row.Kind == ResourceFlowKind.Consumed) consumed += row.AmountRaw;
                    if (row.Kind == ResourceFlowKind.Produced) produced += row.AmountRaw;
                }
                if (stock + consumed != new BigInteger(Fix.FromInt(60).Raw) + produced || InventoryTokens(game) > 9)
                    throw new InvalidOperationException("finite stock/WIP/manufacturing balance/" + InventoryDiagnostics(game, name));
                var foreign = game.Seat(1).View.Feature<OperationsView>();
                if (foreign.ReadSignal(InventoryEntity(game, name), InventoryFeed, "available.amount", out var hidden) != Codes.NotOwned || hidden.Value.HasValue ||
                    foreign.TryGetAutomationRule(new PlayerId(0), "feed", out _) ||
                    foreign.ReadSignal(InventoryEntity(game, "stock.assembler", 1), InventoryFeed, "stock.amount", out var own) != Code.None || own.Value != Fix.FromInt(20))
                    throw new InvalidOperationException("private inventory/control state leaked or foreign stock changed");
                observed[game] = game.Clock.StepIndex;
            }
            Observe(a); Observe(b); return new Pair(a, b, Observe, g => InventoryDiagnostics(g, name), 0);
        }
        private static Pair InventoryRestore(Check c, GameFactory factory, Pair source, string name)
        {
            var restored = InventoryPair(factory.Load(source.A.Save()), factory.Load(source.B.Save()), name);
            c.Equal(source.A.Hash(), restored.A.Hash(), "actual inventory/WIP/rule/inbox owners restore exact hash");
            c.Equal(source.B.Hash(), restored.B.Hash(), "saved peer restores exact same state"); return restored;
        }
        private static void InventoryControlRun(Check c, bool independent)
        {
            string name = independent ? "stock.independent" : "stock.assembler";
            var factory = InventorySignalTests.Factory();
            SimGame New() => factory.NewGame(new NewGameRequest { Scenario = "process", Seed = 331, Seats = { Controller.Human, Controller.Human } });
            var pair = InventoryPair(New(), New(), name);
            Console.WriteLine("MODULES inventory-control=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            c.That(pair.A.Snapshot.Space == null && !pair.A.Modules.Contains(TycoonModule.ModuleId), "authored stock control/manufacturing needs neither map nor business");
            var limits = new ReplayLimits(4 * 1024 * 1024, 2000);
            var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            var early = InventorySignal(pair.A, name, "available.amount");
            c.Equal(Fix.FromInt(20), early.Value.Value, "original finite stock begins at twenty");
            c.Equal(Fix.FromInt(10), InventorySignal(pair.A, name, "pair.amount").Value.Value, "scaled numeric signal derives exact original units");
            c.Equal(Fix.Zero, InventorySignal(pair.A, name, "inbound.amount").Value.Value, "known zero promise is available, not missing");
            int queued = Submit(c, pair, new EnqueueTransformationCommand { Transformer = InventoryEntity(pair.A, name), Capability = InventoryProcess, Transformation = "build", Count = 10 });
            int configured = Submit(c, pair, InventoryConfigure(pair.A, name, "stock.numeric"));
            int foreign = Submit(c, pair, InventoryConfigure(pair.B, name, "stock.numeric"), Codes.NotOwned, seat: 1);
            Reach(pair, g => InventoryRule(g)?.PendingDecision == false, "real finite spending crosses original lower threshold and dwell");
            c.That(Result(pair.A, 0, queued)?.Accepted == true && Result(pair.A, 0, configured)?.Accepted == true &&
                Result(pair.A, 1, foreign)?.Refusal == Codes.NotOwned, "ordinary owner commands and foreign refusal are committed");
            var pending = InventoryState(pair.A, name);
            c.That(InventoryEnabled(pair.A, name) && InventorySignal(pair.A, name, "available.amount").Value < Fix.FromInt(8) &&
                pending.Progress.WholeTicks > 0 && pending.Progress.WholeTicks < pending.Required, "low-stock decision retains actual unfinished work until next engine update");
            c.Equal(6L, pending.Required, "original one-tenth-hour batch earns six full game minutes");
            var restored = InventoryRestore(c, factory, pair, name);
            var pendingLeft = restored.A.RecordReplay(limits); var pendingRight = restored.B.RecordReplay(limits);
            Both(pair, restored, g => !InventoryEnabled(g, name), "saved pending numeric shutdown applies through original operation handler");
            c.Equal(pending.Wip.Execution, InventoryState(pair.A, name).Wip.Execution, "shutdown preserves original WIP execution identity");
            ReplayPair(c, factory, pendingLeft, pendingRight, restored, limits, 0, "pending stock shutdown replay");
            restored = InventoryRestore(c, factory, pair, name);
            var stoppedLeft = restored.A.RecordReplay(limits); var stoppedRight = restored.B.RecordReplay(limits);
            var stopped = InventoryState(pair.A, name); var stoppedSignal = InventorySignal(pair.A, name, "available.amount");
            long target = pair.A.Clock.StepIndex + 4;
            Both(pair, restored, g => g.Clock.StepIndex >= target, "actual quiet disabled work");
            c.Equal(stopped.Progress, InventoryState(pair.A, name).Progress, "disabled rule earns no work");
            c.Equal(stoppedSignal.Value, InventorySignal(pair.A, name, "available.amount").Value, "disabled work spends no further material");
            pair.A.Net.RequestPause(); Reach(pair, g => g.Clock.Paused, "native clock pause with real held WIP");
            long paused = pair.A.Clock.StepIndex; ulong held = pair.A.Hash();
            Reach(restored, g => g.Clock.StepIndex >= paused + 8, "separate restored clocks advance while original is paused"); pair.Round();
            c.That(pair.A.Clock.StepIndex == paused && pair.A.Hash() == held && restored.A.Clock.StepIndex > paused, "actual sessions isolate clocks/WIP/rule state");
            pair.A.Net.RequestResume(); target = restored.A.Clock.StepIndex;
            Both(pair, restored, g => g.Clock.StepIndex >= target, "original clock catches up without another shutdown or resource debit");
            foreach (var current in new[] { pair, restored }) Submit(c, current, new CancelTransformationCommand { Transformer = InventoryEntity(current.A, name), Capability = InventoryProcess, Index = 0 });
            Both(pair, restored, g => InventoryState(g, name).Wip.Stock.IsEmpty && InventoryState(g, name).QueueCount == stopped.QueueCount - 1,
                "ordinary cancellation returns only actual unfinished inputs");
            c.Equal(stoppedSignal.Value.Value + Fix.FromInt(3), InventorySignal(pair.A, name, "available.amount").Value.Value, "three original inputs return once, completed outputs are not refunded");
            c.That(InventorySignal(pair.A, name, "available.amount").Value > Fix.FromInt(8) && InventorySignal(pair.A, name, "available.amount").Value <= Fix.FromInt(12),
                "actual returned stock lies inside original hysteresis band");
            ReplayPair(c, factory, stoppedLeft, stoppedRight, restored, limits, 1, "disabled physical work to returned-input hysteresis replay");
            restored = InventoryRestore(c, factory, pair, name);
            var bandLeft = restored.A.RecordReplay(limits); var bandRight = restored.B.RecordReplay(limits);
            target = pair.A.Clock.StepIndex + 4;
            Both(pair, restored, g => g.Clock.StepIndex >= target, "saved stock in hysteresis band remains off");
            c.That(!InventoryEnabled(pair.A, name) && !InventoryRule(pair.A).Latch && InventoryRule(pair.A).PendingDecision == null,
                "returned stock does not bypass authored upper restart threshold");
            foreach (var current in new[] { pair, restored }) Submit(c, current, InventoryConfigure(current.A, name, "stock.restart"));
            Both(pair, restored, g => InventoryRule(g)?.PendingDecision == true, "explicit authored lower-threshold policy earns a deferred restart");
            c.That(!InventoryEnabled(pair.A, name) && InventoryState(pair.A, name).Wip.Stock.IsEmpty, "captured restart is not same-update production");
            ReplayPair(c, factory, bandLeft, bandRight, restored, limits, 1, "saved hysteresis to authored restart intent replay");
            restored = InventoryRestore(c, factory, pair, name);
            var restartLeft = restored.A.RecordReplay(limits); var restartRight = restored.B.RecordReplay(limits);
            Both(pair, restored, g => InventoryEnabled(g, name) && InventoryState(g, name).Progress.WholeTicks > 0, "saved deferred restart creates only real paid timed WIP");
            c.That(InventoryState(pair.A, name).Wip.Execution != stopped.Wip.Execution && InventoryState(pair.A, name).Wip.Stock.AmountOf(InventorySupply(pair.A)) == Fix.FromInt(3),
                "fresh actual execution pays its inputs rather than resurrecting cancelled work");
            ReplayPair(c, factory, restartLeft, restartRight, restored, limits, 0, "saved pending restart to productive work replay");
            restored = InventoryRestore(c, factory, pair, name);
            var runningLeft = restored.A.RecordReplay(limits); var runningRight = restored.B.RecordReplay(limits);
            foreach (var current in new[] { pair, restored }) {
                Submit(c, current, InventoryConfigure(current.A, name, "stock.restart", false));
                Submit(c, current, new ConfigureOperationCommand { Entity = InventoryEntity(current.A, name), Operation = InventoryProcess, Enabled = true, Target = Fix.One });
            }
            bool LastMinute(SimGame game) { var state = InventoryState(game, name); return state.QueueCount == 1 && state.Progress.WholeTicks == state.Required - 1; }
            Both(pair, restored, LastMinute, "original remaining batches earn work until actual final minute");
            c.Equal(8, InventoryTokens(pair.A), "last unfinished batch is not an early ninth output");
            ReplayPair(c, factory, runningLeft, runningRight, restored, limits, 2, "saved productive restart to final-minute work replay");
            restored = InventoryRestore(c, factory, pair, name);
            var finalLeft = restored.A.RecordReplay(limits); var finalRight = restored.B.RecordReplay(limits);
            Both(pair, restored, g => InventoryState(g, name).QueueCount == 0, "saved final minute earns last whole result");
            target = pair.A.Clock.StepIndex + 4; Both(pair, restored, g => g.Clock.StepIndex >= target, "completed manufacturing remains settled");
            c.That(InventoryTokens(pair.A) == 9 && InventorySignal(pair.A, name, "stock.amount").Value == Fix.FromInt(2) &&
                InventoryState(pair.A, name).Wip.Stock.IsEmpty && InventoryRule(pair.A).PendingDecision == null, "ten ordered minus one cancelled yields nine earned outputs and two finite supplies");
            c.Equal(Fix.FromInt(20), early.Value.Value, "retained initial signal stays detached from later spending");
            c.That(pair.Stalls > 0 && pair.ReversedBatches > 0 && restored.Stalls > 0 && restored.ReversedBatches > 0, "real original/restored transports stall and shuffle");
            ReplayPair(c, factory, finalLeft, finalRight, restored, limits, 0, "saved final-minute manufacturing replay");
            ReplayPair(c, factory, left, right, pair, limits, 7, "full finite inventory control replay");
            c.That(pair.A.Hash() == pair.B.Hash() && pair.A.Hash() == restored.A.Hash() && restored.A.Hash() == restored.B.Hash(), "four actually advanced worlds converge");
            c.Equal(pair.A.Hash(), factory.Load(pair.A.Save()).Hash(), "settled public workflow survives terminal native save");
        }
    }
}
