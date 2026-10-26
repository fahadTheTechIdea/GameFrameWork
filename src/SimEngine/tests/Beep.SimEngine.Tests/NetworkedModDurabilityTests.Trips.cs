using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using Trips = Beep.SimEngine.Tests.FleetTripTests;
using Batching = Beep.SimEngine.Tests.FleetLoadTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class NetworkedModDurabilityTests
    {
        internal const string FleetTripCase = "Networked fleet trips: saved loaded holds, finite propulsion fuel and native/contract reserve decisions";
        private static ResourceId TripFuel(SimGame game)
        { if (!game.Seat(0).View.TryFindResource("fleet.fuel", out var value)) throw new InvalidOperationException("authored propulsion fuel absent"); return value; }
        private static HaulTripSettings TripSettings(SimGame game, EntityId worker)
        {
            Code refusal = game.Seat(0).View.Feature<LogisticsView>().ReadHaulTripPolicy(worker, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("actual trip settings/" + refusal); return value;
        }
        private static MotionExecutionView TripMovement(SimGame game, EntityId worker)
        {
            Code refusal = game.Seat(0).View.Feature<MotionView>().ReadExecution(worker, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("actual propulsion execution/" + refusal); return value;
        }
        private static FixVec2 TripPosition(SimGame game, EntityId worker)
            => ((TileSpatialSnapshot)game.Seat(0).Snapshot.Entities.Single(e => e.Id == worker).Spatial).Position;
        private static string TripDiagnostics(SimGame game) => FleetDiagnostics(game) + "/fuel=" +
            FleetStock(game, FleetEntity(game, "fleet.trip-native"), TripFuel(game)).Stock + "/" +
            FleetStock(game, FleetEntity(game, "fleet.trip-public"), TripFuel(game)).Stock;
        private static void TripObserve(SimGame game)
            => TripObserve(game, 24);
        private static void TripObserve(SimGame game, int initialFuel)
        {
            var fuel = TripFuel(game); BigInteger stock = 0, consumed = 0;
            foreach (var entity in game.Seat(0).Snapshot.Entities.Where(e => e.Owner == new PlayerId(0)))
                if (game.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(entity.Id, fuel, out var value).IsNone) stock += value.Stock.Raw;
            foreach (var row in game.Seat(0).View.Feature<ResourceFlowView>().ReadSummary(new PlayerId(0)).Aggregates) {
                if (row.Kind == ResourceFlowKind.AuthoredCreation || row.Kind == ResourceFlowKind.StockPurchase)
                    throw new InvalidOperationException("trip workflow fabricated material or fuel");
                if (row.Resource != fuel) continue;
                if (row.Kind == ResourceFlowKind.Consumed) consumed += row.AmountRaw;
                if (row.Kind == ResourceFlowKind.Produced || row.Kind == ResourceFlowKind.Lost || row.Kind == ResourceFlowKind.Destroyed)
                    throw new InvalidOperationException("trip fuel must be original finite stock and actual propulsion consumption");
            }
            if (stock + consumed != new BigInteger(Fix.FromInt(initialFuel).Raw)) throw new InvalidOperationException("propulsion fuel/journal conservation failed/" + TripDiagnostics(game));
        }
        private static Pair TripRestore(Check c, GameFactory factory, Pair source)
            => FleetRestore(c, factory, source, TripObserve, TripDiagnostics);
        private static void FleetTripRun(Check c)
        {
            var factory = FleetNavigationTests.Factory(named: true, tripControls: true, recordFlows: true);
            var pair = FleetPair(LiveSensorTests.New(factory), LiveSensorTests.New(factory), TripObserve, TripDiagnostics);
            Console.WriteLine("MODULES fleet-trips=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            c.That(!pair.A.Modules.Contains(TycoonModule.ModuleId), "finite propulsion and trip control require no business or platform");
            var native = FleetEntity(pair.A, "fleet.trip-native"); var external = FleetEntity(pair.A, "fleet.trip-public");
            var limited = FleetEntity(pair.A, "fleet.restricted"); var source = FleetEntity(pair.A, "survey.site"); var target = FleetEntity(pair.A, "fleet.destination");
            var fuel = TripFuel(pair.A); var steel = FleetSteel(pair.A); var limits = new ReplayLimits(8 * 1024 * 1024, 5000);
            var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits); Pair restored = null; int issued = 0;
            int Input(Command command, Code expected = default, int seat = 0) {
                int ticket = Submit(c, pair, command, expected, seat); issued++;
                if (restored != null) c.Equal(ticket, Submit(c, restored, command, expected, seat), "actual saved trip command identity");
                bool Answered(SimGame game) => Result(game, seat, ticket)?.Refusal == expected;
                if (restored == null) Reach(pair, Answered, "ordinary trip player intent"); else Both(pair, restored, Answered, "same trip player intent on saved peers");
                return ticket;
            }
            void Route(int id, bool enabled) => Input(new ConfigureRouteCommand { Route = id, Settings = new LogisticsRouteSettings(enabled, Fix.FromInt(3), 100) });
            WorkJobView Job(SimGame game, int id) => FleetJobs(game).Single(j => j.Id == id);
            bool LoadedMoving(SimGame game, EntityId worker) => FleetJobs(game).Any(j => j.Worker == worker && j.Loaded == Fix.FromInt(3) && !j.TripHeld) &&
                FleetPosition(game, worker).X > 10 && FleetPosition(game, worker).X < 29 && TripMovement(game, worker).Operating.Providers.Single().Exposure.ElapsedTicks >= 3;
            var workers = pair.A.Seat(0).Snapshot.Entities.Where(e => e.Owner == new PlayerId(0) &&
                pair.A.Seat(0).View.Feature<WorkView>().ReadWorkerExecution(e.Id, out _).IsNone).Select(e => e.Id).ToArray();
            foreach (var worker in workers) Input(new SetWorkerHeldCommand { Worker = worker, Held = true });
            Input(Batching.MembershipCommand(new[] { native, limited }));
            Input(Trips.Control(Trips.Settings(false)), HaulTripPolicyCodes.NotPolicy);
            c.That(TripSettings(pair.A, native).Enabled && TripSettings(pair.A, native).PreserveOutletReserve, "unsupported member cannot alter valid native trip prefix");
            Input(Batching.MembershipCommand(new[] { external, native }));
            var before = Batching.View(pair.A).PreviewTripPolicy("delivery", Trips.Settings(false));
            Input(Trips.Control(Trips.Settings(profile: "fleet.trip-veto")), new Code("proof.trip-refused"));
            c.That(TripSettings(pair.A, native).Profile == "fleet.trip-funded" && TripSettings(pair.A, external).Profile == "fleet.trip-funded", "late independent veto commits no earlier valid native prefix");
            Input(Trips.Control(Trips.Settings()), Codes.NotFound, 1);
            Input(Trips.Control(Trips.Settings(false)));
            foreach (var worker in new[] { native, external }) c.That(!TripSettings(pair.A, worker).Enabled && FleetHome(pair.A, worker).State == WorkerHomeState.Held &&
                FleetHome(pair.A, worker).Home == source, "whole trip edits retain actual held/home state");
            int same = Input(Trips.Control(Trips.Settings(false)));
            c.That(Result(pair.A, 0, same).Value.Arg0 == 2 && Result(pair.A, 0, same).Value.Arg1 == 0, "identical whole trip policy changes no actual owner");
            Input(Trips.Control(Trips.Settings(true, true, "fleet.trip-funded")));
            int ticketRoute = Input(new AddRouteCommand { From = source, To = target, Resource = steel, Settings = new LogisticsRouteSettings(true, Fix.FromInt(3), 100) });
            int route = checked((int)Result(pair.A, 0, ticketRoute).Value.Arg0);
            Input(new SetWorkerHeldCommand { Worker = external, Held = false });
            long blockedAt = pair.A.Clock.StepIndex;
            Reach(pair, game => game.Clock.StepIndex >= blockedAt + 4, "real reserve-blocked request remains pending over time");
            c.That(Batching.Offer(pair.A, route).Haul.Blocker == HaulTripPolicyCodes.Reserve && FleetJobs(pair.A).Count == 0 && FleetStock(pair.A, source).Stock == Fix.FromInt(20),
                "actual outlet reserve prevents unfunded physical dispatch without inventing cargo or fuel");
            restored = TripRestore(c, factory, pair); int releaseStart = issued;
            var releaseLeft = restored.A.RecordReplay(limits); var releaseRight = restored.B.RecordReplay(limits);
            Input(Trips.Control(Trips.Settings()));
            foreach (var worker in new[] { native, external }) {
                c.Equal(Code.None, pair.A.Seat(0).View.Feature<LogisticsView>().ReadOutletPolicy(worker, out var outlet), "actual unchanged outlet policy readable");
                c.Equal(Fix.FromInt(12), outlet.Reserve.AmountOf(fuel), "trip setting never rewrites original reserve floor");
            }
            Both(pair, restored, game => FleetJobs(game).Any(j => j.Worker == external), "real independent carrier assigned after ordinary trip policy release");
            var first = FleetJobs(pair.A).Single(j => j.Worker == external); Route(route, false);
            Both(pair, restored, game => LoadedMoving(game, external), "independent carrier acquires full actual cargo and burns propulsion fuel in loaded travel");
            c.Equal(Fix.Zero, FleetStock(pair.A, target).Stock, "loaded physical travel is not instant delivery");
            ReplayPair(c, factory, releaseLeft, releaseRight, restored, limits, issued - releaseStart, "saved reserve refusal through actual fueled loaded independent travel replay");
            restored = TripRestore(c, factory, pair);
            var holdLeft = restored.A.RecordReplay(limits); var holdRight = restored.B.RecordReplay(limits);
            var firstPosition = TripPosition(pair.A, external); var firstFuel = FleetStock(pair.A, external, fuel).Stock;
            var exposure = TripMovement(pair.A, external).Operating.Providers.Single().Exposure;
            var handling = Job(pair.A, first.Id).HandlingProgress;
            Input(Trips.Control(Trips.Settings(false)));
            c.That(Job(pair.A, first.Id).TripHeld && Job(pair.A, first.Id).State == JobState.Paused && Job(pair.A, first.Id).Blocker == HaulTripPolicyCodes.Disabled &&
                Job(pair.A, first.Id).Loaded == Fix.FromInt(3) && FleetHome(pair.A, external).Job == first.Id &&
                FleetStock(pair.A, target).InboundPromised == Fix.FromInt(3), "trip disable physically holds same acquired job/worker/cargo and destination promise");
            c.That(TripPosition(pair.A, external).Equals(firstPosition) && FleetStock(pair.A, external, fuel).Stock == firstFuel &&
                TripMovement(pair.A, external).Operating.Providers.Single().Exposure.ElapsedTicks == exposure.ElapsedTicks &&
                Job(pair.A, first.Id).HandlingProgress.Equals(handling), "trip command step burns no held fuel or hidden fractional movement/work");
            ReplayPair(c, factory, holdLeft, holdRight, restored, limits, 1, "saved loaded independent journey through actual trip hold replay");
            restored = TripRestore(c, factory, pair); int heldStart = issued;
            var heldLeft = restored.A.RecordReplay(limits); var heldRight = restored.B.RecordReplay(limits);
            long heldAt = pair.A.Clock.StepIndex;
            Both(pair, restored, game => game.Clock.StepIndex >= heldAt + 8, "real held game time accrues without propulsion work");
            c.That(TripPosition(pair.A, external).Equals(firstPosition) && FleetStock(pair.A, external, fuel).Stock == firstFuel &&
                TripMovement(pair.A, external).Operating.Providers.Single().Exposure.ElapsedTicks == exposure.ElapsedTicks, "eight actual held steps consume no movement fuel/exposure");
            pair.A.Net.RequestPause(); Reach(pair, game => game.Clock.Paused, "native sole-clock pause on saved trip hold");
            long paused = pair.A.Clock.StepIndex; ulong frozen = pair.A.Hash();
            Reach(restored, game => game.Clock.StepIndex >= paused + 8, "other saved held peers advance independently on their sole clock");
            pair.Round(); c.That(pair.A.Hash() == frozen && pair.A.Clock.StepIndex == paused, "paused original borrows no clock or fuel consumption");
            pair.A.Net.RequestResume(); Both(pair, restored, game => game.Clock.StepIndex >= paused + 8, "native resume restores common held physical epoch");
            Input(Trips.Control(Trips.Settings())); Input(new RemoveFleetCommand { Fleet = "delivery" });
            Both(pair, restored, game => Job(game, first.Id).Loaded == Fix.FromInt(3) && !Job(game, first.Id).TripHeld && !TripPosition(game, external).Equals(firstPosition),
                "ordinary release resumes same loaded vehicle after fleet metadata removal");
            c.That(!Batching.View(pair.A).TryRead("delivery", out _) && TripSettings(pair.A, external).Enabled, "metadata deletion retains current component-owned policy");
            ReplayPair(c, factory, heldLeft, heldRight, restored, limits, issued - heldStart, "saved loaded hold through quiet time/isolation/resumption and metadata deletion replay");
            restored = TripRestore(c, factory, pair);
            var deliveryLeft = restored.A.RecordReplay(limits); var deliveryRight = restored.B.RecordReplay(limits);
            Both(pair, restored, game => FleetStock(game, target).Stock == Fix.FromInt(3) && FleetHome(game, external).Job == 0 && FleetPosition(game, external).X < 29,
                "same independent trip physically delivers and departs despite removed fleet metadata");
            c.That(FleetStock(pair.A, external, fuel).Stock < firstFuel && FleetStock(pair.A, external).Stock.IsZero, "resumed real movement consumes fuel but unloads steel once");
            ReplayPair(c, factory, deliveryLeft, deliveryRight, restored, limits, 0, "saved resumed metadata-free trip through first actual delivery replay");
            restored = TripRestore(c, factory, pair); int nativeStart = issued;
            var nativeLeft = restored.A.RecordReplay(limits); var nativeRight = restored.B.RecordReplay(limits);
            Input(new SetWorkerHeldCommand { Worker = external, Held = true }); Input(Batching.MembershipCommand(new[] { native }));
            Input(Trips.Control(Trips.Settings(false, true, "fleet.trip-funded")));
            c.That(!TripSettings(pair.A, native).Enabled && TripSettings(pair.A, external).Enabled, "new membership changes actual current native owner only");
            Input(new SetWorkerHeldCommand { Worker = native, Held = false }); Route(route, true);
            c.That(FleetJobs(pair.A).Count == 0 && Batching.Offer(pair.A, route).Haul.Blocker == HaulTripPolicyCodes.Disabled, "disabled native policy prevents new physical intake");
            ReplayPair(c, factory, nativeLeft, nativeRight, restored, limits, issued - nativeStart, "saved first trip through native-only membership and blocked second intake replay");
            restored = TripRestore(c, factory, pair); int nativeReleaseStart = issued;
            var nativeReleaseLeft = restored.A.RecordReplay(limits); var nativeReleaseRight = restored.B.RecordReplay(limits);
            Input(Trips.Control(Trips.Settings()));
            Both(pair, restored, game => FleetJobs(game).Any(j => j.Worker == native), "native actual fueled dispatch after ordinary policy release");
            var second = FleetJobs(pair.A).Single(j => j.Worker == native); Route(route, false);
            Both(pair, restored, game => LoadedMoving(game, native), "native acquired loaded physical travel");
            c.That(second.Id != first.Id && FleetStock(pair.A, target).Stock == Fix.FromInt(3), "native second trip is a distinct execution, not repeated first delivery");
            ReplayPair(c, factory, nativeReleaseLeft, nativeReleaseRight, restored, limits, issued - nativeReleaseStart, "saved disabled native intake through loaded second trip replay");
            restored = TripRestore(c, factory, pair);
            var reserveLeft = restored.A.RecordReplay(limits); var reserveRight = restored.B.RecordReplay(limits);
            Input(Trips.Control(Trips.Settings(true, true, "fleet.trip-funded")));
            c.Equal(Code.None, pair.A.Seat(0).View.Feature<LogisticsView>().ReadHaulTripBudget(native, out var budget), "actual acquired trip budget public read");
            c.That(Job(pair.A, second.Id).TripHeld && Job(pair.A, second.Id).Blocker == HaulTripPolicyCodes.Reserve &&
                budget.Blocker == HaulTripPolicyCodes.Reserve && budget.ReserveFloor.AmountOf(fuel) == Fix.FromInt(12) && budget.RequiredStock.AmountOf(fuel) > Fix.FromInt(12),
                "original remaining movement and unchanged outlet reserve hold same acquired native cargo");
            ReplayPair(c, factory, reserveLeft, reserveRight, restored, limits, 1, "saved native loaded journey through actual unfunded reserve hold replay");
            restored = TripRestore(c, factory, pair);
            var finalLeft = restored.A.RecordReplay(limits); var finalRight = restored.B.RecordReplay(limits);
            var nativePosition = TripPosition(pair.A, native); var nativeFuel = FleetStock(pair.A, native, fuel).Stock; long reserveAt = pair.A.Clock.StepIndex;
            Both(pair, restored, game => game.Clock.StepIndex >= reserveAt + 4, "saved native reserve hold accrues actual waiting time");
            c.That(TripPosition(pair.A, native).Equals(nativePosition) && FleetStock(pair.A, native, fuel).Stock == nativeFuel && Job(pair.A, second.Id).Loaded == Fix.FromInt(3),
                "reserve-held native state retains exact fractional position/fuel/cargo");
            Input(Trips.Control(Trips.Settings()));
            Both(pair, restored, game => FleetStock(game, target).Stock == Fix.FromInt(6) && FleetHome(game, native).Job == 0 && FleetPosition(game, native).X < 29,
                "same native trip resumes actual fueled movement, second delivery and departure");
            c.That(FleetStock(pair.A, source).Stock == Fix.FromInt(14) && FleetStock(pair.A, native).Stock.IsZero && FleetStock(pair.A, external).Stock.IsZero &&
                FleetStock(pair.A, source).OutboundPromised.IsZero && FleetStock(pair.A, target).InboundPromised.IsZero &&
                FleetStock(pair.A, native, fuel).Stock < nativeFuel, "both steel deliveries and finite propulsion consumption settle once without grants");
            c.That(before.Entries.All(e => e.Settings.Enabled && e.Settings.PreserveOutletReserve) && budget.Blocker == HaulTripPolicyCodes.Reserve,
                "old public policy/budget facts remain detached after real release and completion");
            c.That(pair.Stalls > 0 && restored.Stalls > 0 && pair.ReversedBatches > 0 && restored.ReversedBatches > 0, "original/restored native trip transports actually stall and reverse bytes");
            ReplayPair(c, factory, finalLeft, finalRight, restored, limits, 1, "saved native reserve hold through quiet time and actual second delivery replay");
            ReplayPair(c, factory, left, right, pair, limits, issued, "full native/independent fueled trip sequence replay");
            c.That(pair.A.Hash() == pair.B.Hash() && pair.A.Hash() == restored.A.Hash() && restored.A.Hash() == restored.B.Hash(), "all four genuinely advanced fueled trip worlds converge");
            TripRestore(c, factory, pair); Console.WriteLine("TRIP inputs=" + issued);
        }
    }
}
