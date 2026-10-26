using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static partial class SpillResourceAccountingTests
    {
        internal const string InterruptedCargoCase = "Spill cargo interruption: native and independent saved load returns through authored recovery";
        // Public player intent/facts only. Original logistics owns cancellation, residual intent, claims and return.
        private static void InterruptedCargo(Check c, GameFactory factory, SimGame game, ref SimGame twin, EntityId worker, EntityId destination, int original)
        {
            var oil = Resource(game, "oil"); var view = game.Seat(0).View.Feature<LogisticsView>();
            long facts = game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).Latest;
            c.That(Jobs(game).Any(row => row.Id == original && row.Loaded == Fix.FromInt(3)), "original physical pickup owns the complete real load");
            c.Equal(Fix.Zero, Stock(game, destination, "oil"), "pickup has not instantly delivered oil to destination");
            Send(c, game, new SetWorkerHeldCommand { Worker = worker, Held = true }); twin = Save(c, factory, game);
            c.That(!Jobs(game).Any(row => row.Id == original), "ordinary hold retires original complete haul commitment");
            c.Equal(Code.None, view.ReadCargoDisposition(worker, out var held), "original cargo disposition is readable after player interruption");
            c.That(held.Retained.IsEmpty && held.Committed.IsEmpty && held.Unassigned.AmountOf(oil) == Fix.FromInt(3), "released old receipts leave real unassigned cargo, not loss or refund");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<LogisticsView>().ReadCargoDisposition(worker, out _), "foreign player cannot observe interrupted cargo");
            for (int i = 0; i < 4; i++) Step(c, game, twin);
            c.That(Stock(game, worker, "oil") == Fix.FromInt(3) && Stock(game, destination, "oil").IsZero, "held carrier physically retains every recovered unit");
            c.Equal(facts, game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).Latest, "holding real cargo publishes no fictitious unload/refund/loss prefix");
            Send(c, game, new SetWorkerHeldCommand { Worker = worker, Held = false }); twin = Save(c, factory, game);
            for (int i = 0; i < 4; i++) Step(c, game, twin);
            var offers = new List<LogisticsDispatchOffer>(); view.CopyQueue(offers);
            c.That(offers.Any(row => row.Kind == LogisticsDispatchKind.CargoReturn && row.Haul.Blocker == RecoveryDestinationDeclaration.NoDemand), "JSON demand-only recovery waits for real player stock policy, not an invented destination");
            c.Equal(Fix.FromInt(3), Stock(game, worker, "oil"), "unsatisfied recovery keeps original load in its actual carrier");
            twin = Save(c, factory, game); game.Clock.Pause(); twin.Clock.Pause(); ulong paused = game.Hash();
            Step(c, game, twin); c.Equal(paused, game.Hash(), "sole paused clock freezes loaded residual recovery"); game.Clock.Resume(); twin.Clock.Resume();
            Send(c, game, new ConfigureStockPolicyCommand { Store = destination, Port = new Code("material"), Resource = oil,
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(3), Fix.FromInt(3), Fix.FromInt(3), 99, LogisticsReserveAccess.Protected)) });
            twin = Save(c, factory, game);
            Until(c, game, twin, () => Jobs(game).Any(row => row.Worker == worker && row.RecoveryKind == LogisticsRecoveryKind.CargoReturn), "engine acquires residual return through authored policy and actual stock demand");
            var returning = Jobs(game).Single(row => row.Worker == worker && row.RecoveryKind == LogisticsRecoveryKind.CargoReturn);
            c.That(returning.Id > original && !returning.Source.IsValid && !returning.Purchase && returning.Destination == destination && returning.Loaded == Fix.FromInt(3), "engine return is a new actual loaded journey without a second pickup or purchase");
            c.That(returning.SourceEndpoint == held.Endpoint, "return receipts address the original cargo owner, not a substitute stock");
            c.Equal(Code.None, view.ReadCargoDisposition(worker, out var committed), "loaded return exposes original partition");
            c.That(committed.Committed.AmountOf(oil) == Fix.FromInt(3) && committed.Unassigned.IsEmpty, "one engine return protects exactly the real residual load");
            twin = Save(c, factory, game);
            Until(c, game, twin, () => Stock(game, destination, "oil") == Fix.One && Stock(game, worker, "oil") == Fix.FromInt(2), "authored productive unloading transfers exactly one unit per game minute");
            c.Equal(Fix.Zero, Spill(game), "cargo return cannot re-deposit recovered spill as a second source");
            twin = Save(c, factory, game);
            Until(c, game, twin, () => Stock(game, destination, "oil") == Fix.FromInt(3) && Stock(game, worker, "oil").IsZero && !Jobs(game).Any(row => row.Id == returning.Id), "saved partial unload completes actual delivery and departure");
            c.Equal(Code.None, view.ReadCargoDisposition(worker, out var empty), "completed carrier disposition remains truthful");
            c.That(empty.Retained.IsEmpty && empty.Committed.IsEmpty && empty.Unassigned.IsEmpty, "settled return retains no duplicate stock or reservation authority");
            c.Equal(Fix.FromInt(5), Total(game), "recovered site plus delivered oil still equals original finite liquid");
        }
    }
}
