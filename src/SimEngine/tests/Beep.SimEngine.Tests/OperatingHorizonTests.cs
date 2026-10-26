using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class OperatingInputsTests
    {
        private static OperatingInputEvaluation Horizon(SimGame game, long intervals)
        {
            Code refusal = game.Seat(0).View.Feature<MotionView>().ReadOperatingInputs(Find(game, "machine").Id, intervals, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("operating horizon refused: " + refusal); return value;
        }
        private static void HorizonAdmission(Check c)
        {
            var factory = Factory(); var game = New(factory); var entity = Find(game, "machine");
            ResourceId supply = game.World.Content.ResourceId("supply"); ulong hash = game.Hash(); long time = game.Clock.StepIndex;
            var zero = Horizon(game, 0); var one = Horizon(game, 1); var five = Horizon(game, 5);
            c.That(zero.Intervals == 0 && zero.RequiredInputs.IsEmpty && zero.Blocker.IsNone, "zero exposure horizon creates no fabricated input");
            c.That(one.Intervals == 1 && one.RequiredInputs.AmountOf(supply) == Fix.One, "existing one-interval admission is the same horizon calculator");
            c.That(five.Intervals == 5 && five.RequiredInputs.AmountOf(supply) == Fix.FromInt(3) && five.Blocker == OperatingInputCodes.Inputs,
                "five authored game-time intervals require three whole quanta, not five rounded next draws");
            c.That(one.Providers[0].Demand.NextInputs.AmountOf(supply) == Fix.One &&
                five.Providers[0].Demand.NextInputs.AmountOf(supply) == Fix.One, "utility next demand stays one real interval, distinct from whole-horizon requirement");
            c.That(game.Hash() == hash && game.Clock.StepIndex == time && entity.Get<IInventory>().Reservations.Count == 0,
                "forecast neither earns time/exposure nor burns/reserves stock");
            c.Equal(Codes.InvalidArgument, game.Seat(0).View.Feature<MotionView>().ReadOperatingInputs(entity.Id, -1, out _), "negative public horizon has typed refusal");
            c.Throws<ArgumentOutOfRangeException>(() => OperatingInputsContracts.Read(game.World, entity, OperatingPhase.Movement, -1), "invalid direct horizon is not clamped");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<MotionView>().ReadOperatingInputs(entity.Id, 5, out var hidden), "foreign forecast reveals no input rates or stock");
            c.That(hidden == null, "foreign horizon has no partial facts");
            c.Throws<NotSupportedException>(() => ((IList<OperatingUtilitySnapshot>)five.Providers).Clear(), "horizon provider facts are immutable");
            var range = Horizon(game, long.MaxValue);
            c.That(range.Intervals == long.MaxValue && range.Blocker == OperatingInputCodes.Range && range.RequiredInputs.IsEmpty,
                "unrepresentable whole horizon yields range, never a partial affordable prefix");
            var combined = New(Factory(stock: 1, extension: true)); var both = Horizon(combined, 4);
            c.That(both.Providers.Count == 2 && both.RequiredInputs.AmountOf(combined.World.Content.ResourceId("supply")) == Fix.FromInt(4) &&
                both.Blocker == OperatingInputCodes.Inputs, "native/public overlapping horizons aggregate before one inventory admission");
            var promised = New(Factory(stock: 4)); var promisedEntity = Find(promised, "machine");
            var promisedStore = promisedEntity.Get<IInventory>(); var promisedSupply = promised.World.Content.ResourceId("supply");
            var owner = new ReservationOwner("proof.horizon", 1); var receipt = promisedStore.ReserveOut(owner, promisedSupply, Fix.FromInt(2));
            ulong promisedHash = promised.Hash(); var unavailable = Horizon(promised, 5);
            c.That(unavailable.RequiredInputs.AmountOf(promisedSupply) == Fix.FromInt(3) && unavailable.Blocker == OperatingInputCodes.Inputs &&
                Horizon(promised, 1).Blocker.IsNone, "whole horizon excludes actual promised stock even when its next interval is affordable");
            c.That(promised.Hash() == promisedHash && promisedStore.CheckReservedOut(owner, receipt).Ready,
                "whole-horizon shortage neither consumes nor releases another owner's promise");
            promisedStore.ReleaseOut(owner, receipt);
            c.That(Horizon(promised, 5).Blocker.IsNone, "fresh funding sees native release, never retained preview authorization");
            var wide = New(Factory(edit: root => root.Get("entities")[2].Get("components").Get("operating_inputs").Get("inputs")[0]
                .Get("period").Set("numerator", JsonValue.Number(long.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture)))));
            var wideFacts = Horizon(wide, long.MaxValue);
            c.That(wideFacts.Intervals == long.MaxValue && wideFacts.RequiredInputs.AmountOf(wide.World.Content.ResourceId("supply")) == Fix.One &&
                wideFacts.Blocker.IsNone, "exact slow authored rates support signed64-bit horizons without a platform/engine cap");
            var workers = New(WorkerFactory()); var worker = Find(workers, "machine.a");
            c.That(workers.Seat(0).View.Feature<WorkView>().ReadOperatingInputs(worker.Id, OperatingPhase.Idle, out var idle, 5).IsNone &&
                idle.RequiredInputs.AmountOf(workers.World.Content.ResourceId("supply")) == Fix.FromInt(3), "idle horizons use their actual authored two-minute phase");
            c.That(workers.Seat(0).View.Feature<WorkView>().ReadOperatingInputs(worker.Id, OperatingPhase.Standby, out var standby, 5).IsNone &&
                standby.RequiredInputs.AmountOf(workers.World.Content.ResourceId("supply")) == Fix.FromInt(2), "standby horizons exclude idle and movement providers");
            c.Equal(Codes.InvalidArgument, workers.Seat(0).View.Feature<WorkView>().ReadOperatingInputs(worker.Id, OperatingPhase.Idle, out _, -1), "work-view horizon uses the same negative-intent refusal");
            c.Equal(hash, game.Hash(), "all retained phase-horizon reads remain pure");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "horizons add no saved forecast/clock authority");
        }
        private static MotionArrivalView BudgetArrival(SimGame game)
        {
            Code refusal = game.Seat(0).View.Feature<MotionView>().ReadArrival(Find(game, "machine").Id, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("route budget refused: " + refusal); return value;
        }
        private static void BudgetHome(Check c, SimGame game)
            => c.That(TestKit.Order(game, 0, new SetHomeCommand { Worker = Find(game, "machine").Id,
                Home = Find(game, "destination").Id }).Accepted, "player starts real engine-owned home travel");
        private static void HorizonPhysical(Check c)
        {
            foreach (bool extension in new[] { false, true })
            {
                // Independent providers both consume their own authored rates from one real inventory.
                var factory = Factory(stock: extension ? 8 : 6, extension: extension, worker: true,
                    edit: root =>
                    {
                        root.Get("entities")[2].Get("components").Remove("transporter");
                        if (extension) StartupContentAuthoring.Position(root, "destination", JsonValue.Number("4"));
                    });
                var game = New(factory); var entity = Find(game, "machine"); ResourceId supply = game.World.Content.ResourceId("supply");
                c.That(BudgetArrival(game).OperatingBudget == null, "idle route has no invented distance/funding");
                BudgetHome(c, game); var arrival = BudgetArrival(game); var forecast = arrival.OperatingBudget;
                if (forecast == null) throw new InvalidOperationException("missing actual route budget: status=" + arrival.Status +
                    " blocker=" + arrival.Blocker + " remaining=" + arrival.RemainingTicks + " execution=" + Read(game).Blocker +
                    " stock=" + entity.Get<IInventory>().Amount(supply) + " job=" + entity.Get<IWorker>().Job + " home=" + entity.Get<IWorker>().Home);
                c.That(forecast != null && forecast.Intervals == arrival.RemainingTicks && forecast.Blocker.IsNone,
                    "known actual remaining route attaches the same funded native phase horizon");
                Fix stock = entity.Get<IInventory>().Amount(supply); Fix required = forecast.RequiredInputs.AmountOf(supply);
                ulong hash = game.Hash(); var restored = factory.Load(game.Save());
                c.That(BudgetArrival(restored).OperatingBudget.RequiredInputs.AmountOf(supply) == required && restored.Hash() == hash,
                    "remaining-route funding reconstructs from actual saved exposure/geometry, not a copied estimate");
                long time = game.Clock.StepIndex; game.Clock.Pause(); restored.Clock.Pause(); game.Update(); restored.Update();
                c.That(game.Clock.StepIndex == time && game.Hash() == hash, "sole-clock pause freezes physical route and exposure");
                game.Clock.Resume(); restored.Clock.Resume();
                Reach(game, () => entity.Get<IMotion>().Status == MoveStatus.Arrived, "funded horizon physical arrival", restored);
                c.Equal(stock - required, entity.Get<IInventory>().Amount(supply), "actual route consumes exactly the prior cumulative remaining horizon");
                c.That(forecast.Providers[0].Exposure.ElapsedTicks == 1 && forecast.RequiredInputs.AmountOf(supply) == required,
                    "retained forecast does not follow subsequent physical state");
                c.That(BudgetArrival(game).OperatingBudget.Intervals == 0 && BudgetArrival(game).OperatingBudget.RequiredInputs.IsEmpty,
                    "actual arrival has zero future movement input, not another fuel draw");
                c.Equal(game.Hash(), restored.Hash(), "native/public physical horizon and saved route continue identically");
            }
            var scarceFactory = Factory(worker: true, edit: root => root.Get("entities")[2].Get("components").Remove("transporter"));
            var scarce = New(scarceFactory); BudgetHome(c, scarce);
            var budget = BudgetArrival(scarce);
            c.That(budget.ArrivalTick.HasValue && budget.OperatingBudget.Blocker == OperatingInputCodes.Inputs &&
                Read(scarce).Blocker.IsNone, "currently payable movement exposes underfunded route without inventing a guaranteed arrival or fleet gate");
            var held = scarceFactory.Load(scarce.Save()); var machine = Find(scarce, "machine");
            Reach(scarce, () => Read(scarce).Blocker == OperatingInputCodes.Inputs, "actual unfunded-route depletion", held);
            Fix position = machine.Get<PointPresence>().Position; long elapsed = Read(scarce).Operating.Providers[0].Exposure.ElapsedTicks;
            for (int i = 0; i < 3; i++) { Tick(scarce); Tick(held); }
            c.That(machine.Get<PointPresence>().Position == position && Read(scarce).Operating.Providers[0].Exposure.ElapsedTicks == elapsed,
                "forecast grants no free fuel/distance once actual next-input gate stops travel");
            c.That(BudgetArrival(scarce).OperatingBudget == null && BudgetArrival(scarce).RemainingTicks == null,
                "blocked route never substitutes a last known affordable forecast");
            c.Equal(scarce.Hash(), held.Hash(), "underfunded native route and physical shortage remain deterministic");
        }
    }
}

