using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.LogisticsPolicyContentTests;

namespace Beep.SimEngine.Tests
{
    // Authored initial state and player input only. All admission and physical work remain in the engine.
    internal static class RouteRecoveryPolicyAcceptanceTests
    {
        internal const string Case = "Dispatch policies: physical routes evacuations and cargo returns obey original policy owners";
        private static readonly ReplayLimits Limits = new ReplayLimits(8 * 1024 * 1024, 5000);
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static ConfigureOutletPolicyCommand Outlet(SimGame game, string origin, int minimum, bool enabled = true, string destination = "destination")
            => new ConfigureOutletPolicyCommand { Source = Id(game, origin), Change = OutletPolicyChange.Replace(new OutletPolicySettings(enabled,
                minimum == 0 ? Bill.Empty : new Bill(new[] { new ResourceAmount(Supply(game), Fix.FromInt(minimum)) }), 100, new[] { destination }, LogisticsReserveAccess.Protected)) };
        internal static ConfigureStockPolicyCommand Ceiling(SimGame game, int maximum)
            => new ConfigureStockPolicyCommand { Store = Id(game, "destination"), Resource = Supply(game), Change = LogisticsDemandChange.Replace(
                new LogisticsDemandSettings(true, Fix.Zero, Fix.Zero, Fix.FromInt(maximum), 100, LogisticsReserveAccess.Protected)) };
        internal static List<LogisticsDispatchOffer> Queue(SimGame game)
        { var facts = new List<LogisticsDispatchOffer>(); View(game).CopyQueue(facts); return facts; }
        internal static HaulPreview Preview(SimGame game, string origin, bool cargo)
        {
            if (!cargo) return View(game).PreviewHaul(Id(game, origin), Id(game, "destination"), Supply(game), Fix.FromInt(8));
            var facts = new List<CargoReturnView>();
            if (!View(game).CopyCargoReturns(Id(game, "machine"), facts).IsNone) throw new InvalidOperationException("actual carrier cargo unavailable");
            return facts.Single(f => f.Resource == Supply(game)).Return;
        }
        private static void Run(Check c)
        {
            foreach (var kind in new[] { LogisticsDispatchKind.Route, LogisticsDispatchKind.Evacuation, LogisticsDispatchKind.CargoReturn })
                foreach (bool direct in new[] { false, true }) foreach (bool external in new[] { false, true }) Physical(c, kind, direct, external);
        }
        private static void Physical(Check c, LogisticsDispatchKind kind, bool direct, bool external)
        {
            bool cargo = kind == LogisticsDispatchKind.CargoReturn;
            string origin = cargo ? "machine" : "source";
            int firstAmount = cargo ? 2 : 4, firstReserve = cargo ? 2 : 16, finalReserve = cargo ? 0 : 12;
            var factory = DispatchPolicyScenario.Create(cargo ? PolicyDispatchKind.CargoReturn : kind == LogisticsDispatchKind.Evacuation ? PolicyDispatchKind.Evacuation : PolicyDispatchKind.Route, direct, external);
            var game = New(factory); var isolated = New(factory); ulong untouched = isolated.Hash();
            using var recorder = game.RecordReplay(Limits);
            c.Equal(Fix.FromInt(20), Stock(game, "source") + Stock(game, "destination") + Stock(game, "machine"), "explicit finite starting balance");
            ulong before = game.Hash(); c.Equal(Code.None, View(game).ReadOutletPolicy(Id(game, origin), out var held), "original actual export owner");
            c.Equal(Fix.FromInt(firstReserve), held.Reserve.AmountOf(Supply(game)), "authored actual origin reserve");
            c.Equal(Code.None, View(game).ReadStockPolicy(Id(game, "destination"), Supply(game), out var band), "original actual receiving owner");
            c.Equal(Fix.Zero, band.Desired, "no competing automatic stock-demand dispatch"); c.Equal(Fix.FromInt(firstAmount), band.Maximum, "authored receiving ceiling");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<LogisticsView>().ReadOutletPolicy(Id(game, origin), out var foreign), "foreign export facts withheld");
            c.That(foreign == null, "no foreign private prefix"); c.Equal(Codes.NotFound, game.Seat(1).Preview(Outlet(game, origin, finalReserve)), "foreign export command refused");
            c.Equal(Codes.NotFound, game.Seat(1).Preview(Ceiling(game, firstAmount * 2)), "foreign stock command refused"); c.Equal(before, game.Hash(), "all policy observations/refusals pure");
            var start = new List<Command> { Worker(game, "policy.short") };
            if (kind == LogisticsDispatchKind.Route) start.Add(new AddRouteCommand { From = Id(game, "source"), To = Id(game, "destination"), Resource = Supply(game), Settings = new LogisticsRouteSettings(true, Fix.FromInt(8), 100) });
            var twin = Input(c, factory, game, start.ToArray()); Wait(c, game, twin, kind + " operating area");
            c.That(Queue(game).Any(q => q.Kind == kind), "original selected dispatch category is visible while blocked");
            c.That(!Queue(game).Any(q => q.Kind == LogisticsDispatchKind.Demand), "stock ceiling does not create competing replenishment");
            c.That(Preview(game, origin, cargo).Status != HaulStatus.Ready, "area cannot authorize forbidden pickup/delivery");
            twin = Input(c, factory, game, Worker(game, "policy.all"), Reserve(1)); Wait(c, game, twin, kind + " ready-worker floor");
            twin = Input(c, factory, game, Reserve(0), Outlet(game, origin, firstReserve, destination: "machine")); Wait(c, game, twin, kind + " destination permission");
            c.Equal(LogisticsPolicyCodes.Destination, Preview(game, origin, cargo).Blocker, "original typed destination refusal");
            twin = Input(c, factory, game, Outlet(game, origin, cargo ? 4 : 20)); Wait(c, game, twin, kind + " entire physical stock reserve");
            twin = Input(c, factory, game, Outlet(game, origin, firstReserve));
            Until(c, game, twin, () => Jobs(game).Any(j => j.IsHaul && j.Worker.IsValid), kind + " original physical acquisition");
            var first = Jobs(game).Single(j => j.IsHaul && j.Worker.IsValid);
            c.Equal(Fix.FromInt(firstAmount), first.Amount, "reserve and actual receiving ceiling limit dispatch");
            c.Equal(Id(game, "destination"), first.Destination, "actual destination binding"); c.That(!first.Purchase, "recovery/routes do not purchase resources");
            if (kind == LogisticsDispatchKind.Route) c.That(first.Route > 0 && first.RecoveryKind == null, "actual route identity, not demand/recovery fallback");
            else c.Equal(cargo ? LogisticsRecoveryKind.CargoReturn : LogisticsRecoveryKind.Evacuation, first.RecoveryKind.Value, "actual original recovery kind");
            c.Equal(Fix.Zero, Stock(game, "destination"), "acquisition cannot instantly deliver");
            if (!cargo) c.Equal(Fix.Zero, first.Loaded, "assignment cannot instantly load");
            else { c.Equal(Fix.FromInt(4), Stock(game, "machine"), "acquired return leaves actual cargo unmoved"); c.Equal(Fix.FromInt(2), first.Loaded, "original return claims only unassigned cargo"); }
            twin = Partial(c, factory, game);
            if (!cargo) { Until(c, game, twin, () => Jobs(game).Any(j => j.IsHaul && j.Loaded.IsPositive), kind + " timed original loading"); twin = Partial(c, factory, game); }
            Until(c, game, twin, () => Stock(game, "destination") == Fix.FromInt(firstAmount) && !Jobs(game).Any(j => j.IsHaul && j.Worker.IsValid), kind + " first actual delivery");
            c.Equal(Fix.FromInt(16), Stock(game, "source"), "protected warehouse reserve remains");
            c.Equal(Fix.FromInt(cargo ? 2 : 0), Stock(game, "machine"), "retained cargo stays in actual carrier");
            twin = Input(c, factory, game, Outlet(game, origin, finalReserve)); Wait(c, game, twin, kind + " full receiving maximum");
            c.Equal(cargo ? LogisticsPolicyCodes.Destination : LogisticsPolicyCodes.Maximum, Preview(game, origin, cargo).Blocker,
                "original first infeasible-destination diagnostic; physical wait proves permitted destination maximum");
            c.Equal(Code.None, View(game).ReadStockPolicy(Id(game, "destination"), Supply(game), out var full), "actual full receiving policy still present");
            c.Equal(Fix.FromInt(firstAmount), full.Maximum, "permitted destination remains full at its actual maximum");
            twin = Input(c, factory, game, Outlet(game, origin, finalReserve, false), Ceiling(game, firstAmount * 2)); Wait(c, game, twin, kind + " disabled outlet");
            c.Equal(LogisticsPolicyCodes.Disabled, Preview(game, origin, cargo).Blocker, "disabled export remains deterministic");
            twin = Input(c, factory, game, Outlet(game, origin, finalReserve));
            Until(c, game, twin, () => Jobs(game).Any(j => j.IsHaul && j.Worker.IsValid), kind + " normal physical restart");
            c.Equal(Fix.FromInt(firstAmount), Jobs(game).Single(j => j.IsHaul && j.Worker.IsValid).Amount, "new original trip respects actual current gap");
            twin = Partial(c, factory, game);
            Until(c, game, twin, () => Stock(game, "destination") == Fix.FromInt(firstAmount * 2) && !Jobs(game).Any(j => j.IsHaul && j.Worker.IsValid), kind + " second saved physical delivery");
            Wait(c, game, twin, kind + " final stock/reserve ceiling");
            c.Equal(Fix.FromInt(cargo ? 16 : 12), Stock(game, "source"), "terminal finite source balance"); c.Equal(Fix.Zero, Stock(game, "machine"), "terminal actual cargo empty");
            c.Equal(Fix.FromInt(firstReserve), held.Reserve.AmountOf(Supply(game)), "retained outlet snapshot detached");
            c.Equal(Fix.FromInt(firstAmount), band.Maximum, "retained original maximum detached");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "terminal original policies/receipts/route/recovery restore"); c.Equal(untouched, isolated.Hash(), "interleaved sessions isolated");
            var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), Limits);
            c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "physical route/recovery policy replay " + replay.Failure + "/" + replay.Detail);
        }
    }
}
