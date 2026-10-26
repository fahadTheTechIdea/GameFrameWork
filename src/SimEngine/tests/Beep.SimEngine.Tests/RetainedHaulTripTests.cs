using System;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Testing;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class HaulTripPolicyTests
    {
        private static Job AcquireTrip(Check c, SimGame game, bool tile = false)
        {
            HaulPreview preview = Preview(game, tile);
            int id = Haul(game).AcquireHaul(new PlayerId(0), preview.Source, preview.Destination, preview.Resource, preview.Amount, 100);
            c.That(id > 0, "retained trip begins through actual atomic admission");
            return game.World.Services.Get<WorkBoard>().Job(id);
        }
        private static void TripPolicy(Check c, SimGame game, Entity actor, bool enabled, SimGame twin = null)
        {
            c.That(TestKit.Order(game, 0, new ConfigureHaulTripPolicyCommand { Worker = actor.Id,
                Settings = new HaulTripSettings("trip.funded", enabled, true) }).Accepted, "ordinary trip command changes retained job rule");
            if (twin != null) c.That(TestKit.Order(twin, 0, new ConfigureHaulTripPolicyCommand { Worker = actor.Id,
                Settings = new HaulTripSettings("trip.funded", enabled, true) }).Accepted, "restored twin applies same actual command");
        }
        private static void RetainedAdmission(Check c)
        {
            var game = Start(Factory()); var actor = Find(game, "reserve.truck"); var view = game.Seat(0).View.Feature<LogisticsView>();
            c.Equal(HaulTripPolicyCodes.NotHauling, view.ReadHaulTripBudget(actor.Id, out var absent), "idle carrier has no invented active trip budget");
            c.That(absent == null, "idle trip projection carries no fabricated journey");
            var initial = Preview(game); var job = AcquireTrip(c, game);
            Reach(game, () => job.ToilIndex >= 2 && actor.Get<IOperatingInputs>(new Code("propulsion")).ReadExposure().ElapsedTicks >= 3, "partial physical trip");
            ulong hash = game.Hash(); long time = game.Clock.StepIndex;
            c.Equal(Code.None, view.ReadHaulTripBudget(actor.Id, out var remaining), "same coordinator publishes private remaining funding");
            c.That(remaining.Blocker.IsNone && remaining.Travel.RemainingTicks < initial.Travel.Value.RemainingTicks, "remaining travel uses actual advanced position");
            c.That(remaining.ReturnCommitment.IsEmpty, "retained cargo is protected by actual receipt, never double counted");
            c.Equal(hash, game.Hash(), "remaining-route query does not alter actual intent, stock, receipts or job");
            c.Equal(time, game.Clock.StepIndex, "pure trip read never advances the sole game clock");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<LogisticsView>().ReadHaulTripBudget(actor.Id, out var foreign), "foreign remaining trip is private");
            c.That(foreign == null, "foreign caller receives no operational funding facts");
            c.Equal(Code.None, game.Seat(0).View.Feature<MotionView>().ReadExecution(actor.Id, out var motion), "existing motion view carries trip gate");
            c.Equal(remaining.Travel.RemainingTicks, motion.TripBudget.Travel.RemainingTicks, "movement and management use same selected remaining journey");
            c.Equal(StateCodecTests.ExpectedWorkBoardSchema, game.World.Services.Get<WorkBoard>().StateSchemaVersion, "same board owns direct trip-hold and classified cost codec revision");
            c.That(remaining.Operating.RequiredInputs.AmountOf(game.World.Content.ResourceId("fuel")) < initial.TripBudget.Operating.RequiredInputs.AmountOf(game.World.Content.ResourceId("fuel")),
                "remaining bill reuses native exposure credit instead of the full initial trip");
        }
        private static void RetainedPhysical(Check c)
        {
            foreach (bool tile in new[] { false, true })
            {
                var factory = Factory(tile); var game = Start(factory, tile); var actor = Actor(c, game, tile); var job = AcquireTrip(c, game, tile);
                Reach(game, () => job.ToilIndex >= 2 && actor.Get<IOperatingInputs>(new Code("propulsion")).ReadExposure().ElapsedTicks >= 3, "retained loaded movement");
                var fuel = game.World.Content.ResourceId("fuel"); var stock = actor.Get<IInventory>().Amount(fuel);
                var cargo = job.Loaded; var handling = job.HandlingProgress; var inbound = job.InReservation; var outbound = job.CargoOutReservation;
                int phase = job.ToilIndex, failures = job.Failures;
                TripPolicy(c, game, actor, false);
                c.That(job.TripHeld && job.State == JobState.Paused && job.Blocker == HaulTripPolicyCodes.Disabled, "disabled authored policy holds existing loaded job");
                c.Equal(stock, actor.Get<IInventory>().Amount(fuel), "pre-movement gate prevents extra fuel consumption on command update");
                c.That(actor.Get<IMotion>().Status == MoveStatus.Idle && actor.Get<IWorker>().Job == job.Id, "held recipient is physically stationary without losing machine assignment");
                c.That(job.Loaded == cargo && job.HandlingProgress.Equals(handling) && job.ToilIndex == phase, "hold preserves actual loaded quantity, phase and earned handling");
                c.That(job.InReservation.Equals(inbound) && job.CargoOutReservation.Equals(outbound), "hold never replaces native destination/cargo promises");
                var saved = factory.Load(game.Save()); var restoredJob = saved.World.Services.Get<WorkBoard>().Job(job.Id);
                c.That(restoredJob.TripHeld && restoredJob.InReservation.Equals(inbound) && restoredJob.CargoOutReservation.Equals(outbound), "same board restores retained trip hold and actual claims");
                for (int i = 0; i < 4; i++) { Tick(game); Tick(saved); }
                c.That(job.Failures == failures && job.RetryAt == 0 && actor.Get<IInventory>().Amount(fuel) == stock, "policy hold creates no repeated failure, deadline or fuel spending");
                c.Equal(game.Hash(), saved.Hash(), "held native/public checkpoint twins remain identical");
                game.Clock.Pause(); saved.Clock.Pause(); HashPause(c, game, saved); game.Clock.Resume(); saved.Clock.Resume();
                TripPolicy(c, game, actor, true, saved);
                Reach(game, () => !job.IsLive, "retained delivery and physical departure", saved);
                c.That(job.State == JobState.Done && !job.TripHeld, "same job resumes and ends without a new haul ledger");
                c.Equal(Fix.FromInt(4), game.World.Entities.Get(job.To).Get<IInventory>().Amount(job.Resource), "retained delivery arrives exactly once");
                c.Equal(game.Hash(), saved.Hash(), "resumed native/public loaded twins continue identically");
            }
            foreach (int phase in new[] { 1, 3 })
            {
                var factory = Factory(); var game = Start(factory); var actor = Find(game, "reserve.truck"); var job = AcquireTrip(c, game);
                Reach(game, () => job.ToilIndex == phase && !job.HandlingProgress.Equals(WorkProgress.Zero), "partial handling phase " + phase);
                var progress = job.HandlingProgress; var loaded = job.Loaded; var outbound = job.CargoOutReservation; var inbound = job.InReservation;
                TripPolicy(c, game, actor, false);
                c.That(job.TripHeld && job.HandlingProgress.Equals(progress) && job.Loaded == loaded && job.CargoOutReservation.Equals(outbound) && job.InReservation.Equals(inbound),
                    "trip hold preserves earned partial loading/unloading and exact physical promises");
                var twin = factory.Load(game.Save()); TripPolicy(c, game, actor, true, twin);
                Reach(game, () => !job.IsLive, "partial handling resumed", twin);
                c.Equal(Fix.FromInt(4), game.World.Entities.Get(job.To).Get<IInventory>().Amount(job.Resource), "saved partial handling neither duplicates nor loses cargo");
                c.Equal(game.Hash(), twin.Hash(), "saved earned handling continues identically");
            }
            RetainedReturn(c);
            PhysicalRefuel(c);
            ChangedNativeRoute(c);
            ProtectedEndpointCancellation(c);
            HeldControls(c);
        }
        private static void HashPause(Check c, SimGame game, SimGame twin)
        {
            ulong before = game.Hash(); long update = game.Clock.StepIndex; game.Update(); twin.Update();
            c.That(game.Hash() == before && game.Clock.StepIndex == update && game.Hash() == twin.Hash(), "sole game clock pause freezes retained jobs and inputs");
        }
        private static void RetainedReturn(Check c)
        {
            var factory = Factory(returning: true); var game = Start(factory); var actor = Find(game, "reserve.truck");
            Tick(game); var job = game.World.Services.Get<WorkBoard>().Job(actor.Get<IWorker>().Job);
            var budget = Haul(game).ReadHaulTripBudget(actor.Id);
            c.That(budget != null && budget.Blocker.IsNone && budget.ReturnCommitment.IsEmpty && job.CargoOutReservation.IsValid,
                "acquired same-resource return excludes promised cargo once, not twice");
            var fuel = game.World.Content.ResourceId("fuel"); Fix stock = actor.Get<IInventory>().Amount(fuel); var cargoClaim = job.CargoOutReservation;
            Control(c, game, actor, Fix.FromRatio(1, 4));
            c.That(job.TripHeld && !job.Blocker.IsNone && job.CargoOutReservation.Equals(cargoClaim), "unfunded slower return holds actual cargo rather than borrowing it");
            c.Equal(stock, actor.Get<IInventory>().Amount(fuel), "control slowdown gates movement before physical consumption");
            var twin = factory.Load(game.Save()); Control(c, game, actor, Fix.One); Control(c, twin, twin.World.Entities.Get(actor.Id), Fix.One);
            Reach(game, () => !job.IsLive, "retained return after restored rate", twin);
            c.Equal(Fix.FromInt(2), actor.Get<IInventory>().Amount(fuel), "restored return unloads real promised cargo and preserves only actual burn floor");
            c.Equal(game.Hash(), twin.Hash(), "return hold and controlled rate save/continue identically");
        }
        private static void PhysicalRefuel(Check c)
        {
            var factory = Factory(); var game = Start(factory); var actor = Find(game, "reserve.truck"); var job = AcquireTrip(c, game);
            Reach(game, () => job.ToilIndex >= 2 && actor.Get<IOperatingInputs>(new Code("propulsion")).ReadExposure().ElapsedTicks == 1, "loaded vehicle away from source");
            Fix stock = actor.Get<IInventory>().Amount(game.World.Content.ResourceId("fuel"));
            Control(c, game, actor, Fix.FromRatio(1, 4));
            c.That(job.TripHeld && actor.Get<IMotion>().Status == MoveStatus.Idle && actor.Get<IInventory>().Amount(game.World.Content.ResourceId("fuel")) == stock,
                "remaining unfunded travel holds before fuel expense, ready for actual service");
            var fuel = game.World.Content.ResourceId("fuel"); var cargoClaim = job.CargoOutReservation; var destinationClaim = job.InReservation;
            c.That(TestKit.Order(game, 0, new ConfigureStockPolicyCommand { Store = actor.Id, Resource = fuel,
                Change = LogisticsDemandChange.Replace(new LogisticsDemandSettings(true, Fix.FromInt(14), Fix.FromInt(24), Fix.FromInt(24), 200, LogisticsReserveAccess.Protected)) }).Accepted,
                "player requests real refueling through existing stock bands");
            var courier = Find(game, "reserve.courier");
            c.That(TestKit.Order(game, 0, new ConfigureWorkPolicyCommand { Worker = courier.Id,
                Settings = new WorkPolicySettings(true, 0, new[] { new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) }).Accepted,
                "ordinary policy enables a physical courier, not a stock grant");
            Reach(game, () => courier.Get<IWorker>().Job != 0, "refueling dispatch");
            var refill = game.World.Services.Get<WorkBoard>().Job(courier.Get<IWorker>().Job);
            c.That(refill.IsHaul && refill.To == actor.Id && refill.Resource == fuel && refill.InReservation.IsValid,
                "existing job coordinator supplies the held recipient through actual inbound capacity");
            var twin = factory.Load(game.Save());
            Reach(game, () => !job.IsLive, "physically refueled retained delivery", twin);
            c.That(job.State == JobState.Done && !job.TripHeld && !job.CargoOutReservation.IsValid && !job.InReservation.IsValid,
                "physically funded same job delivers and releases its own original commitments");
            c.That(cargoClaim.IsValid && destinationClaim.IsValid && refill.To == actor.Id, "refill does not replace the original cargo/destination intention");
            c.Equal(Fix.FromInt(4), game.World.Entities.Get(job.To).Get<IInventory>().Amount(job.Resource), "physical refueling permits exactly one original shipment");
            c.That(Find(game, "reserve.source").Get<IInventory>().Amount(fuel) < Fix.FromInt(50), "refuel consumes actual supplied fuel, no fabricated recovery");
            c.Equal(game.Hash(), twin.Hash(), "in-flight refueling, retained job and native stock promises continue identically");
        }
        private static void ChangedNativeRoute(Check c)
        {
            var factory = Factory(true); var game = Start(factory, true); var actor = Actor(c, game, true); var job = AcquireTrip(c, game, true);
            Reach(game, () => job.ToilIndex == 2 && actor.Get<IOperatingInputs>(new Code("propulsion")).ReadExposure().ElapsedTicks >= 3, "native loaded route");
            var fuel = game.World.Content.ResourceId("fuel"); var before = Haul(game).ReadHaulTripBudget(actor.Id);
            Fix floor = actor.Get<IInventory>().Available(fuel) - before.Operating.RequiredInputs.AmountOf(fuel);
            c.That(TestKit.Order(game, 0, new ConfigureOutletPolicyCommand { Source = actor.Id,
                Change = OutletPolicyChange.Reserve(fuel, floor, true) }).Accepted, "native route is exactly funded against real configured stock floor");
            before = Haul(game).ReadHaulTripBudget(actor.Id);
            c.That(before.Blocker.IsNone, "current native journey remains funded at exact floor");
            var obstacle = Spawn(game, "facility.destination", 19, 25);
            var detour = Haul(game).ReadHaulTripBudget(actor.Id);
            c.That(detour.Travel.RemainingTicks > before.Travel.RemainingTicks && detour.Blocker == HaulTripPolicyCodes.Reserve,
                "new actual static obstacle lengthens native detour and refuses its increased physical cost");
            var position = actor.Get<Beep.SimEngine.Maps.ITilePresence>().Position;
            var stock = actor.Get<IInventory>().Amount(fuel); var inbound = job.InReservation; var cargo = job.CargoOutReservation;
            Tick(game);
            c.That(job.TripHeld && actor.Get<Beep.SimEngine.Maps.ITilePresence>().Position == position && actor.Get<IInventory>().Amount(fuel) == stock,
                "changed native route holds before any movement/input expenditure");
            c.That(job.InReservation.Equals(inbound) && job.CargoOutReservation.Equals(cargo), "dynamic route hold preserves actual cargo and destination claims");
            var twin = factory.Load(game.Save());
            game.World.Despawn(obstacle); game.World.Settle(); twin.World.Despawn(twin.World.Entities.Get(obstacle.Id)); twin.World.Settle();
            Reach(game, () => !job.IsLive, "remaining native route after obstacle removed", twin);
            c.That(job.State == JobState.Done && actor.Get<IInventory>().Amount(fuel) >= floor, "same native job physically completes after actual route repair without floor breach");
            c.Equal(Fix.FromInt(4), game.World.Entities.Get(job.To).Get<IInventory>().Amount(job.Resource), "native dynamic route resumes exactly one loaded shipment");
            c.Equal(game.Hash(), twin.Hash(), "saved changed topology and retained funding resume identically");
        }
        private static void ProtectedEndpointCancellation(Check c)
        {
            var factory = Factory(true); var game = Start(factory, true); var actor = Actor(c, game, true); var job = AcquireTrip(c, game, true);
            Reach(game, () => job.ToilIndex == 2, "loaded endpoint-loss trip"); TripPolicy(c, game, actor, false);
            var destination = game.World.Entities.Get(job.To);
            c.Equal(ResourceTransactions.ActivePromises, game.World.CheckDespawn(destination), "live retained destination claims prevent unsafe endpoint removal");
            c.That(TestKit.Order(game, 0, new MoveVehicleCommand { Vehicle = actor.Id,
                To = actor.Get<Beep.SimEngine.Maps.ITilePresence>().Tile }).Accepted, "normal player interruption ends retained haul before removal");
            game.World.Despawn(destination); game.World.Settle(); Tick(game);
            c.That(!job.IsLive && !job.TripHeld && !job.InReservation.IsValid && !job.CargoOutReservation.IsValid,
                "interruption releases native claims and trip hold before endpoint removal");
            c.Equal(Fix.FromInt(4), actor.Get<ITransporter>().Cargo.Amount(job.Resource), "endpoint loss leaves physical cargo for existing recovery, never destroys it");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "cancelled retained trip and residual cargo remain durable");
        }
        private static void HeldControls(Check c)
        {
            foreach (bool enabled in new[] { false, true })
            {
                var factory = Factory(); var game = Start(factory); var actor = Find(game, "reserve.truck"); var job = AcquireTrip(c, game);
                Reach(game, () => job.ToilIndex == 2 && actor.Get<IOperatingInputs>(new Code("propulsion")).ReadExposure().ElapsedTicks == 1, "active control checkpoint");
                var fuel = game.World.Content.ResourceId("fuel"); var stock = actor.Get<IInventory>().Amount(fuel); var cargo = job.CargoOutReservation;
                game.Seat(0).Submit(new ConfigureOperationCommand { Entity = actor.Id, Operation = new Code("travel"),
                    Enabled = enabled, Target = enabled ? Fix.Zero : Fix.One });
                var twin = factory.Load(game.Save()); Tick(game); Tick(twin);
                var budget = Haul(game).ReadHaulTripBudget(actor.Id);
                c.That(job.TripHeld && budget != null && !budget.Blocker.IsNone && !budget.Travel.RemainingTicks.HasValue,
                    "disabled/zero-rate operation retains an explicitly unknown blocked trip, never a zero-duration funded prefix");
                c.That(actor.Get<IInventory>().Amount(fuel) == stock && job.CargoOutReservation.Equals(cargo) && job.RetryAt == 0,
                    "pending actual control gates movement before inputs and keeps original cargo promise");
                var jobs = new System.Collections.Generic.List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs);
                c.That(jobs.Exists(row => row.Id == job.Id && row.TripHeld && row.Blocker == job.Blocker), "immutable work view projects actual job hold without host-side policy");
                c.Equal(game.Hash(), twin.Hash(), "pending loaded operation command restores through the one normal command stream");
                Control(c, game, actor, Fix.One); Control(c, twin, twin.World.Entities.Get(actor.Id), Fix.One);
                Reach(game, () => !job.IsLive, "restored active operation", twin);
                c.That(job.State == JobState.Done && !job.TripHeld, "same physical shipment completes after operation restored");
                c.Equal(game.Hash(), twin.Hash(), "blocked-control saved twins resume identically");
            }
        }
    }
}
