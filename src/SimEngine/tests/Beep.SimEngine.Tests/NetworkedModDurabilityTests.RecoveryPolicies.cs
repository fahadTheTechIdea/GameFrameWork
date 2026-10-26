using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using Policy = Beep.SimEngine.Tests.LogisticsPolicyContentTests;
using Recovery = Beep.SimEngine.Tests.RouteRecoveryPolicyAcceptanceTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class NetworkedModDurabilityTests
    {
        internal const string PolicyRouteCase = "Networked dispatch policy: saved native route, physical capacity and export reserves";
        internal const string PolicyEvacuationCase = "Networked dispatch policy: saved evacuation, physical capacity and export reserves";
        internal const string PolicyCargoCase = "Networked dispatch policy: saved cargo return, physical capacity and export reserves";
        internal const string PolicyContractCargoCase = "Networked dispatch policy contracts: saved independent cargo return, physical capacity and export reserves";
        private static string RecoveryPolicyDiagnostics(SimGame game) => "at=" + game.Clock.StepIndex + "/stock=" + Policy.Stock(game, "source") +
            "/" + Policy.Stock(game, "destination") + "/" + Policy.Stock(game, "machine") + "/queue=" + string.Join(";", Recovery.Queue(game).Select(q =>
                q.Kind + "/" + q.Route + "/" + q.AgeTicks + "/" + q.Haul.Blocker)) + "/jobs=" + string.Join(";", Policy.Jobs(game).Select(j =>
                j.Id + "/" + j.State + "/" + j.Loaded + "/" + j.Blocker));
        private static Pair RecoveryPolicyPair(SimGame a, SimGame b)
        {
            var observed = new Dictionary<SimGame, long>();
            void Observe(SimGame game)
            {
                if (observed.TryGetValue(game, out long previous) && previous == game.Clock.StepIndex) return;
                if (Policy.Stock(game, "source") + Policy.Stock(game, "destination") + Policy.Stock(game, "machine") != Fix.FromInt(20))
                    throw new InvalidOperationException("finite transfer/recovery cargo lost or duplicated/" + RecoveryPolicyDiagnostics(game));
                var foreign = new List<LogisticsDispatchOffer>(); game.Seat(1).View.Feature<LogisticsView>().CopyQueue(foreign);
                if (foreign.Count != 0) throw new InvalidOperationException("foreign dispatch intent leaked");
                observed[game] = game.Clock.StepIndex;
            }
            Observe(a); Observe(b); return new Pair(a, b, Observe, RecoveryPolicyDiagnostics, 2);
        }
        private static bool RecoveryPolicyFree(SimGame game) => Policy.Jobs(game).All(job => !job.Worker.IsValid);
        private static Pair RecoveryPolicyRestore(Check c, GameFactory factory, Pair original)
        {
            var pair = RecoveryPolicyPair(factory.Load(original.A.Save()), factory.Load(original.B.Save()));
            c.Equal(original.A.Hash(), pair.A.Hash(), "original policy/age/receipt/cargo/motion owners reattach exactly");
            c.Equal(original.B.Hash(), pair.B.Hash(), "original peer restores exact committed policy state"); return pair;
        }
        private static void RecoveryPolicyWait(Check c, Pair pair, Pair restored, string stage)
        {
            long through = pair.A.Clock.StepIndex + 8;
            if (restored == null) Reach(pair, game => game.Clock.StepIndex >= through, stage);
            else Both(pair, restored, game => game.Clock.StepIndex >= through, stage);
            c.That(RecoveryPolicyFree(pair.A) && (restored == null || RecoveryPolicyFree(restored.A)), stage + " assigns no physical worker or fake delivery");
        }
        private static void PolicyRecoveryRun(Check c, PolicyDispatchKind kind, bool independent)
        {
            bool cargo = kind == PolicyDispatchKind.CargoReturn, route = kind == PolicyDispatchKind.Route;
            string origin = cargo ? "machine" : "source";
            int amount = cargo ? 2 : 4, reserve = cargo ? 2 : 16, finalReserve = cargo ? 0 : 12;
            var category = route ? LogisticsDispatchKind.Route : cargo ? LogisticsDispatchKind.CargoReturn : LogisticsDispatchKind.Evacuation;
            var factory = DispatchPolicyScenario.Create(kind, direct: independent, external: independent);
            var pair = RecoveryPolicyPair(Policy.New(factory), Policy.New(factory));
            Console.WriteLine("MODULES recovery-policy=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            c.That(!pair.A.Modules.Contains(TycoonModule.ModuleId) && pair.A.Modules.Contains(DispatchPolicyModule.ModuleId), "physical policy dispatch has its native scheduler, not business");
            c.Equal(!route, pair.A.Modules.Contains(LogisticsRecoveryPolicyModule.ModuleId), "recovery is explicitly selected only for actual recovery scenarios");
            c.Equal(Code.None, Policy.View(pair.A).ReadOutletPolicy(Policy.Id(pair.A, origin), out var held), "original source/carrier policy owner");
            c.Equal(Fix.FromInt(reserve), held.Reserve.AmountOf(Policy.Supply(pair.A)), "unchanged authored source or carried-cargo reserve");
            c.Equal(Code.None, Policy.View(pair.A).ReadStockPolicy(Policy.Id(pair.A, "destination"), Policy.Supply(pair.A), out var band), "actual receiver policy owner");
            c.That(band.Desired.IsZero && band.Maximum == Fix.FromInt(amount), "existing receiver ceiling creates no competing replenishment demand");
            c.Equal(Codes.NotFound, pair.A.Seat(1).View.Feature<LogisticsView>().ReadOutletPolicy(Policy.Id(pair.A, origin), out var hidden), "foreign policy reads are private");
            c.That(hidden == null, "foreign source reserve/destination permissions publish no prefix");
            var limits = new ReplayLimits(8 * 1024 * 1024, 5000);
            var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            int area = Submit(c, pair, Policy.Worker(pair.A, "policy.short"));
            int foreign = Submit(c, pair, Recovery.Outlet(pair.B, origin, finalReserve), Codes.NotFound, seat: 1);
            int routeTicket = 0;
            if (route) routeTicket = Submit(c, pair, new AddRouteCommand { From = Policy.Id(pair.A, "source"), To = Policy.Id(pair.A, "destination"),
                Resource = Policy.Supply(pair.A), Settings = new LogisticsRouteSettings(true, Fix.FromInt(8), 100) });
            Reach(pair, game => Result(game, 0, area)?.Accepted == true && Result(game, 1, foreign)?.Refusal == Codes.NotFound &&
                Recovery.Queue(game).Any(q => q.Kind == category), "actual policy-blocked route/recovery intent and foreign refusal");
            RecoveryPolicyWait(c, pair, null, "operating area wait");
            c.That(Recovery.Preview(pair.A, origin, cargo).Status != HaulStatus.Ready, "the original area prevents forbidden pickup/delivery");
            var queued = Recovery.Queue(pair.A).Single(q => q.Kind == category);
            c.That(queued.AgeTicks > 0 && queued.AgeTicks == pair.A.Clock.StepIndex - queued.CreatedTick,
                "original intent ages only by the sole committed game clock");
            int routeId = route ? checked((int)Result(pair.A, 0, routeTicket).Value.Arg0) : 0;
            var restored = RecoveryPolicyRestore(c, factory, pair);
            var ageLeft = restored.A.RecordReplay(limits); var ageRight = restored.B.RecordReplay(limits);
            c.Equal(queued.CreatedTick, Recovery.Queue(restored.A).Single(q => q.Kind == category).CreatedTick, "save preserves native blocked intent chronology");
            foreach (var current in new[] { pair, restored }) { Submit(c, current, Policy.Worker(current.A, "policy.all")); Submit(c, current, Policy.Reserve(1)); }
            RecoveryPolicyWait(c, pair, restored, "actual ready-worker reserve floor");
            foreach (var current in new[] { pair, restored }) { Submit(c, current, Policy.Reserve(0)); Submit(c, current, Recovery.Outlet(current.A, origin, reserve, destination: "machine")); }
            RecoveryPolicyWait(c, pair, restored, "actual destination permission wait");
            c.Equal(LogisticsPolicyCodes.Destination, Recovery.Preview(pair.A, origin, cargo).Blocker, "same original destination refusal, not a host policy evaluator");
            foreach (var current in new[] { pair, restored }) Submit(c, current, Recovery.Outlet(current.A, origin, cargo ? 4 : 20));
            RecoveryPolicyWait(c, pair, restored, "entire actual supply/cargo protected by player reserve");
            if (cargo) {
                c.That(!Recovery.Queue(pair.A).Any(q => q.Kind == category) && Policy.Stock(pair.A, "machine") == Fix.FromInt(4),
                    "reserving all actual cargo removes return eligibility without deleting its stock");
                c.Equal(HaulStatus.NoSupply, Recovery.Preview(pair.A, origin, true).Status, "operating reserves are not fabricated return cargo");
            } else c.Equal(queued.CreatedTick, Recovery.Queue(pair.A).Single(q => q.Kind == category).CreatedTick, "existing route/evacuation intent keeps its age through policy waits");
            ReplayPair(c, factory, ageLeft, ageRight, restored, limits, 5, "area-blocked intent through worker floor, destination and full stock reserve replay");
            restored = RecoveryPolicyRestore(c, factory, pair);
            var releaseLeft = restored.A.RecordReplay(limits); var releaseRight = restored.B.RecordReplay(limits);
            foreach (var current in new[] { pair, restored }) Submit(c, current, Recovery.Outlet(current.A, origin, reserve));
            Both(pair, restored, game => Policy.Jobs(game).Any(j => j.IsHaul && j.Worker.IsValid), "native constrained physical dispatch acquisition");
            var first = Policy.Jobs(pair.A).Single(j => j.IsHaul && j.Worker.IsValid);
            c.That(first.Amount == Fix.FromInt(amount) && first.Destination == Policy.Id(pair.A, "destination") && !first.Purchase,
                "real source reserve and receiver gap bound the original nonpurchase job");
            c.Equal(Fix.Zero, Policy.Stock(pair.A, "destination"), "acquisition cannot teleport actual inventory");
            c.Equal(cargo ? Fix.FromInt(amount) : Fix.Zero, first.Loaded, "only actual preexisting cargo is loaded at acquisition");
            if (route) c.That(first.Route == routeId && first.RecoveryKind == null, "same original standing route, no recovery/demand fallback");
            else c.Equal(cargo ? LogisticsRecoveryKind.CargoReturn : LogisticsRecoveryKind.Evacuation, first.RecoveryKind.Value, "original typed physical recovery kind");
            if (cargo) c.That(Recovery.Queue(pair.A).Single(q => q.Kind == category).CreatedTick > queued.CreatedTick,
                "explicitly releasing operating reserve earns a fresh real return intent, not a resurrected stale age");
            if (!cargo) Both(pair, restored, game => Policy.Jobs(game).Any(j => j.IsHaul && j.Loaded > Fix.Zero && j.Loaded < Fix.FromInt(amount)), "real partially timed loading");
            ReplayPair(c, factory, releaseLeft, releaseRight, restored, limits, 1, "full stock reserve through actual partial cargo acquisition replay");
            restored = RecoveryPolicyRestore(c, factory, pair);
            var cargoLeft = restored.A.RecordReplay(limits); var cargoRight = restored.B.RecordReplay(limits);
            c.Equal(first.Id, Policy.Jobs(restored.A).Single(j => j.IsHaul && j.Worker.IsValid).Id, "partial cargo keeps its original job/receipt identity");
            pair.A.Net.RequestPause(); Reach(pair, game => game.Clock.Paused, "native network pause during unfinished physical transfer");
            long paused = pair.A.Clock.StepIndex; ulong frozen = pair.A.Hash();
            Reach(restored, game => game.Clock.StepIndex >= paused + 8, "other saved peers advance actual transfer while original is paused");
            pair.Round(); c.That(pair.A.Clock.StepIndex == paused && pair.A.Hash() == frozen, "paused original borrows no cargo, movement, claims or intent age");
            pair.A.Net.RequestResume(); Both(pair, restored, game => game.Clock.StepIndex >= paused + 8, "native resumption converges at one actual epoch");
            Both(pair, restored, game => Policy.Stock(game, "destination") == Fix.FromInt(amount) && RecoveryPolicyFree(game), "first actual timed delivery and physical worker departure");
            c.That(Policy.Stock(pair.A, "source") == Fix.FromInt(16) && Policy.Stock(pair.A, "machine") == Fix.FromInt(cargo ? 2 : 0), "actual protected stock and residual cargo remain finite");
            ReplayPair(c, factory, cargoLeft, cargoRight, restored, limits, 0, "partial cargo through paused-isolated physical delivery replay");
            restored = RecoveryPolicyRestore(c, factory, pair);
            var roomLeft = restored.A.RecordReplay(limits); var roomRight = restored.B.RecordReplay(limits);
            foreach (var current in new[] { pair, restored }) Submit(c, current, Recovery.Outlet(current.A, origin, finalReserve));
            RecoveryPolicyWait(c, pair, restored, "full original receiving maximum");
            c.Equal(cargo ? LogisticsPolicyCodes.Destination : LogisticsPolicyCodes.Maximum, Recovery.Preview(pair.A, origin, cargo).Blocker,
                "retain the actual first infeasible destination diagnostic rather than inventing a stronger summary");
            foreach (var current in new[] { pair, restored }) {
                Submit(c, current, Recovery.Outlet(current.A, origin, finalReserve, false)); Submit(c, current, Recovery.Ceiling(current.A, amount * 2));
            }
            RecoveryPolicyWait(c, pair, restored, "disabled export despite newly available receiver capacity");
            c.Equal(LogisticsPolicyCodes.Disabled, Recovery.Preview(pair.A, origin, cargo).Blocker, "current disabled source/carrier remains authoritative");
            c.Equal(Code.None, Policy.View(pair.A).ReadStockPolicy(Policy.Id(pair.A, "destination"), Policy.Supply(pair.A), out var expanded), "actual widened receiver policy");
            c.Equal(Fix.FromInt(amount * 2), expanded.Maximum, "only explicit player command changes actual capacity policy");
            ReplayPair(c, factory, roomLeft, roomRight, restored, limits, 3, "first delivery through full receiver and disabled-capacity-release replay");
            restored = RecoveryPolicyRestore(c, factory, pair);
            var enableLeft = restored.A.RecordReplay(limits); var enableRight = restored.B.RecordReplay(limits);
            foreach (var current in new[] { pair, restored }) Submit(c, current, Recovery.Outlet(current.A, origin, finalReserve));
            Both(pair, restored, game => Policy.Jobs(game).Any(j => j.IsHaul && j.Worker.IsValid), "ordinary export restart creates the remaining actual trip");
            var second = Policy.Jobs(pair.A).Single(j => j.IsHaul && j.Worker.IsValid);
            c.That(second.Id != first.Id && second.Amount == Fix.FromInt(amount) && (route ? second.Route == routeId : second.RecoveryKind == first.RecoveryKind),
                "fresh real job retains intended route/recovery identity and exact remaining gap");
            ReplayPair(c, factory, enableLeft, enableRight, restored, limits, 1, "saved disabled export through original new-trip admission replay");
            restored = RecoveryPolicyRestore(c, factory, pair);
            var finalLeft = restored.A.RecordReplay(limits); var finalRight = restored.B.RecordReplay(limits);
            Both(pair, restored, game => Policy.Stock(game, "destination") == Fix.FromInt(amount * 2) && RecoveryPolicyFree(game), "saved second physical trip completes without another player order");
            RecoveryPolicyWait(c, pair, restored, "terminal actual capacity/protected-stock wait");
            c.That(Policy.Stock(pair.A, "source") == Fix.FromInt(cargo ? 16 : 12) && Policy.Stock(pair.A, "machine").IsZero,
                "terminal finite source and original cargo empty exactly match both real deliveries");
            c.That(held.Reserve.AmountOf(Policy.Supply(pair.A)) == Fix.FromInt(reserve) && band.Maximum == Fix.FromInt(amount) && queued.AgeTicks > 0,
                "retained original policies/age facts remain detached through later commands and physical work");
            foreach (string entity in new[] { "source", "destination", "machine" }) {
                c.Equal(Code.None, Policy.View(pair.A).ReadDiagnostics(Policy.Id(pair.A, entity), Policy.Supply(pair.A), out var facts), "actual terminal port diagnostics");
                c.Equal(Fix.Zero, facts.InboundPromised + facts.OutboundPromised, "actual transfer and recovery promises fully settle");
            }
            c.That(pair.Stalls > 0 && restored.Stalls > 0 && pair.ReversedBatches > 0 && restored.ReversedBatches > 0, "original and restored native transports genuinely stall and reorder original bytes");
            ReplayPair(c, factory, finalLeft, finalRight, restored, limits, 0, "saved second trip to settled finite physical delivery replay");
            ReplayPair(c, factory, left, right, pair, limits, route ? 13 : 12, "full authored route/recovery policy sequence replay");
            c.That(pair.A.Hash() == pair.B.Hash() && pair.A.Hash() == restored.A.Hash() && restored.A.Hash() == restored.B.Hash(), "all four genuinely advanced policy sessions converge");
            RecoveryPolicyRestore(c, factory, pair);
        }
    }
}
