using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using Sensor = Beep.SimEngine.Tests.PoweredPointSensorTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class NetworkedModDurabilityTests
    {
        internal const string ExplorationCase = "Networked powered exploration: physical supply, private shared discoveries and saved interrupted exposure";
        private static List<WorkJobView> SensorJobs(SimGame game)
        {
            var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs); return jobs;
        }
        private static bool SensorNoHaul(SimGame game) => !SensorJobs(game).Any(job => job.IsHaul &&
            (job.State == JobState.Open || job.State == JobState.Assigned || job.State == JobState.Paused));
        private static bool SensorDisabled(SimGame game)
        {
            if (!game.Seat(0).View.Feature<OperationsView>().TryGetControl(Sensor.Find(game, "sensor.machine"), Sensor.Power, out var state))
                throw new InvalidOperationException("authored power operation missing");
            return !state.Enabled;
        }
        private static Fix SensorOtherStock(SimGame game, string name)
        {
            Code refusal = game.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(Sensor.Find(game, name), Sensor.Supply(game), out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("physical survey stock/" + name + "/" + refusal);
            return value.Stock;
        }
        private static void SensorObserve(SimGame game)
        {
            BigInteger stock = new BigInteger(Sensor.Stock(game).Raw);
            foreach (string name in new[] { "source", "destination", "machine" }) stock += SensorOtherStock(game, name).Raw;
            if (stock + Sensor.Consumed(game) != new BigInteger(Fix.FromInt(20).Raw))
                throw new InvalidOperationException("survey supply/utility was lost or duplicated/" + SensorDiagnostics(game));
        }
        private static string SensorDiagnostics(SimGame game) => "at=" + game.Clock.StepIndex + "/stock=" + Sensor.Stock(game) +
            "/consumed=" + Sensor.Consumed(game) + "/process=" + Sensor.Process(game).Blocker + "/scan=" +
            string.Join(";", Sensor.Scan(game).Entries.Select(row => row.Id + "/" + row.Progress + "/" + row.Known + "/" + row.Blocker));
        private static Pair SensorPair(SimGame a, SimGame b) => new Pair(a, b, SensorObserve, SensorDiagnostics, 0);
        private static KnowledgeObservation SensorRecipient(SimGame game)
        {
            if (!game.Seat(1).View.Feature<KnowledgeView>().TryRead(new PlayerId(1), "sim.knowledge.facts", "powered.reset-known", out var value))
                throw new InvalidOperationException("actual shared recipient observation missing");
            return value;
        }
        private static void ExplorationRun(Check c)
        {
            var factory = Sensor.ExplorationFactory(); var pair = SensorPair(Sensor.New(factory), Sensor.New(factory));
            Console.WriteLine("MODULES exploration=" + string.Join(",", pair.A.Modules.OrderBy(id => id, StringComparer.Ordinal)));
            c.That(!pair.A.Modules.Contains(TycoonModule.ModuleId) && pair.A.Net.InputDelay == 0 && pair.B.Net.InputDelay == 0,
                "authored physical surveying uses explicit native zero-delay transport, not business or a tick bypass");
            c.That(Sensor.Scan(pair.A).Entries.All(row => !row.Known && row.Blocker == ExplorationCodes.NoSensor), "unpowered region starts undiscovered");
            c.That(pair.A.Seat(1).View.Feature<ExplorationView>().Read(Sensor.Find(pair.A, "sensor.machine"), Sensor.Explorer, out var hidden) == Codes.NotFound && hidden == null,
                "foreign partial exploration facts are withheld");
            var limits = new ReplayLimits(8 * 1024 * 1024, 3000);
            var left = pair.A.RecordReplay(limits); var right = pair.B.RecordReplay(limits);
            int foreign = Submit(c, pair, Sensor.Control(pair.B, false), Codes.NotFound, seat: 1);
            int supply = Submit(c, pair, Sensor.SupplyRouteCommand(pair.A, 8));
            Reach(pair, game => Result(game, 1, foreign)?.Refusal == Codes.NotFound && SensorJobs(game).Any(job => job.IsHaul &&
                job.Loaded.IsPositive && job.Loaded < Fix.FromInt(8)), "real partial carrier loading and foreign refusal");
            int route = checked((int)Result(pair.A, 0, supply).Value.Arg0);
            c.That(route > 0 && Sensor.Stock(pair.A).IsZero, "actual route is not remote utility delivery");
            var restored = SensorPair(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()));
            var haulLeft = restored.A.RecordReplay(limits); var haulRight = restored.B.RecordReplay(limits);
            c.Equal(pair.A.Hash(), restored.A.Hash(), "partial loading and original cargo commitments restore exactly");
            Both(pair, restored, game => Sensor.Stock(game) == Fix.FromInt(8), "original physical carrier supplies the remote sensor");
            foreach (var current in new[] { pair, restored }) Submit(c, current, new RemoveRouteCommand { Route = route });
            Both(pair, restored, SensorNoHaul, "carrier physically departs before power dispatch");
            foreach (var current in new[] { pair, restored }) Submit(c, current, Sensor.SightCommand(current.A, Fix.Parse("1.5")));
            Both(pair, restored, game => Result(game, 0, 3)?.Accepted == true, "normal narrow-sight intent admitted before power");
            foreach (var current in new[] { pair, restored }) Submit(c, current, new EnqueueTransformationCommand {
                Transformer = Sensor.Find(current.A, "sensor.machine"), Capability = Sensor.Power, Transformation = "sensor.power-cycle" });
            Both(pair, restored, game => Sensor.Process(game).QueueCount == 1, "single continuous native power intent");
            long execution = Sensor.Process(pair.A).Queue.Single().Execution;
            foreach (var current in new[] { pair, restored }) Submit(c, current, Sensor.Control(current.A, true));
            Both(pair, restored, game => Sensor.Utility(game).Drawing, "first actual paid narrow-sight interval");
            c.That(Sensor.Scan(pair.A).Entries.All(row => row.Progress.Equals(WorkProgress.Zero) && !row.Known && row.Blocker == ExplorationCodes.NoSensor),
                "center-only powered sight cannot earn whole-region discovery");
            foreach (var current in new[] { pair, restored }) Submit(c, current, Sensor.SightCommand(current.A, Fix.Parse("2.5")));
            Both(pair, restored, game => Sensor.Scan(game).Entries.All(row => row.Progress.WholeTicks == 1 && !row.Known),
                "first full powered region earns actual partial dwell");
            var partial = Sensor.Scan(pair.A);
            var layer = pair.A.Seat(0).View.Feature<PointLayerView>(); var region = new PointRegion(Fix.FromInt(5), Fix.One, 1); var samples = new List<Fix>();
            var page = layer.ReadLayerPage("point.terrain", region, new SpatialLayerPageRequest(0, 1), samples);
            c.Equal(SpatialLayerReadStatus.Success, page.Status, "actual powered private non-tile layer publication");
            foreach (var current in new[] { pair, restored }) Submit(c, current, Sensor.Control(current.A, false));
            Both(pair, restored, SensorDisabled, "normal power interruption before completed discovery");
            c.That(Sensor.Entry(pair.A, "a.reset").Progress.Equals(WorkProgress.Zero) && Sensor.Entry(pair.A, "b.retain").Progress.WholeTicks == 1,
                "same original reset and retain policies survive actual interruption");
            c.Equal(SpatialLayerReadStatus.Unavailable, layer.ReadLayerPage("point.terrain", region, new SpatialLayerPageRequest(0, 1, page.Cursor), samples).Status,
                "shutdown withholds old private layer cursor before admission");
            ReplayPair(c, factory, haulLeft, haulRight, restored, limits, 6, "partial-haul to disabled-partial-exposure replay");
            pair.A.Net.RequestPause(); Reach(pair, game => game.Clock.Paused, "sole-clock pause of interrupted original exposure");
            long paused = pair.A.Clock.StepIndex; ulong frozen = pair.A.Hash(); Fix held = Sensor.Stock(pair.A);
            restored = SensorPair(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()));
            var powerLeft = restored.A.RecordReplay(limits); var powerRight = restored.B.RecordReplay(limits);
            c.Equal(frozen, restored.A.Hash(), "original disabled dwell and elapsed utility restore together");
            foreach (var current in new[] { pair, restored }) Submit(c, current, Sensor.Control(current.A, true));
            Reach(restored, game => Sensor.Stock(game) == Fix.FromInt(2) && Sensor.Entry(game, "b.retain").Progress.WholeTicks == 2,
                "other actual saved session advances paid partial observation while original is paused");
            pair.Round();
            c.That(pair.A.Clock.StepIndex == paused && pair.A.Hash() == frozen && Sensor.Stock(pair.A) == held &&
                Sensor.Entry(pair.A, "b.retain").Progress.WholeTicks == 1, "paused original borrows neither fuel, utility time nor exposure");
            c.That(restored.A.Clock.StepIndex > paused && Sensor.Stock(restored.A) < held && !Sensor.Known(restored.A, "retain"),
                "independent session genuinely spends fuel and earns dwell without an early fact");
            ReplayPair(c, factory, powerLeft, powerRight, restored, limits, 1, "disabled-exposure to paid-running-partial replay");
            restored = SensorPair(factory.Load(restored.A.Save()), factory.Load(restored.B.Save()));
            var shortageLeft = restored.A.RecordReplay(limits); var shortageRight = restored.B.RecordReplay(limits);
            pair.A.Net.RequestResume();
            Both(pair, restored, game => Sensor.Stock(game).IsZero && !Sensor.Process(game).Blocker.IsNone, "native utility exhaustion after saved paid exposure");
            c.That(!Sensor.Known(pair.A, "reset") && !Sensor.Known(pair.A, "retain") &&
                Sensor.Entry(pair.A, "a.reset").Progress.Equals(WorkProgress.Zero) && Sensor.Entry(pair.A, "b.retain").Progress.WholeTicks == 2,
                "unpaid final sensor interval reveals no region and preserves only actual retained work");
            c.Equal(new BigInteger(Fix.FromInt(8).Raw), Sensor.Consumed(pair.A), "first finite delivery is consumed exactly once through native feed and utility owners");
            ReplayPair(c, factory, shortageLeft, shortageRight, restored, limits, 0, "paid-partial to actual-shortage replay");
            restored = SensorPair(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()));
            var recoveryLeft = restored.A.RecordReplay(limits); var recoveryRight = restored.B.RecordReplay(limits);
            foreach (var current in new[] { pair, restored }) Submit(c, current, Sensor.Control(current.A, false));
            Both(pair, restored, SensorDisabled, "disable exhausted process before physical refuel");
            int replenishment = Submit(c, pair, Sensor.SupplyRouteCommand(pair.A, 8));
            c.Equal(replenishment, Submit(c, restored, Sensor.SupplyRouteCommand(restored.A, 8)), "saved replenishment keeps normal seat command identity");
            Both(pair, restored, game => Sensor.Stock(game) == Fix.FromInt(8), "second actual physical utility delivery");
            int secondRoute = checked((int)Result(pair.A, 0, replenishment).Value.Arg0);
            foreach (var current in new[] { pair, restored }) Submit(c, current, new RemoveRouteCommand { Route = secondRoute });
            Both(pair, restored, SensorNoHaul, "second carrier finishes departure without overlapping service");
            foreach (var current in new[] { pair, restored }) Submit(c, current, Sensor.Control(current.A, true));
            Both(pair, restored, game => Sensor.Known(game, "retain") && !Sensor.Known(game, "reset"), "retained exposure earns its fact before reset branch");
            c.Equal(SpatialLayerReadStatus.Invalidated, layer.ReadLayerPage("point.terrain", region, new SpatialLayerPageRequest(0, 1, page.Cursor), samples).Status,
                "renewed live sight cannot revive pre-interruption private cursor");
            Both(pair, restored, game => Sensor.Known(game, "reset"), "reset exposure separately earns its full declared dwell");
            foreach (var current in new[] { pair, restored }) Submit(c, current, Sensor.Control(current.A, false));
            Both(pair, restored, SensorDisabled, "freeze earned discoveries through ordinary power policy");
            c.That(Sensor.Scan(pair.A).Entries.All(row => row.Known && row.Progress.Equals(WorkProgress.Zero)) &&
                Sensor.Process(pair.A).Queue.Single().Execution == execution && !Sensor.Known(pair.A, "reset", 1),
                "original ledger owns both private completed facts and same continuous execution survives recovery");
            ReplayPair(c, factory, recoveryLeft, recoveryRight, restored, limits, 5, "shortage to independently-earned-private-discoveries replay");
            restored = SensorPair(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()));
            var sharingLeft = restored.A.RecordReplay(limits); var sharingRight = restored.B.RecordReplay(limits);
            foreach (var current in new[] { pair, restored }) Submit(c, current, new ShareKnowledgeCommand { Recipient = new PlayerId(1), Policy = "powered.share" });
            Both(pair, restored, game => Sensor.Known(game, "reset", 1) && Sensor.Known(game, "retain", 1), "explicit directional sharing exports actual earned observations");
            var recipient = SensorRecipient(pair.A);
            foreach (var current in new[] { pair, restored }) Submit(c, current, new ForgetKnowledgeCommand { Policy = "powered.forget" });
            Both(pair, restored, game => !Sensor.Known(game, "reset") && !Sensor.Known(game, "retain"), "ordinary own forgetting removes both native facts");
            c.That(Sensor.Known(pair.A, "reset", 1) && Sensor.Scan(pair.A).Entries.All(row => !row.Known && row.Progress.Equals(WorkProgress.Zero)),
                "forgetting is not recipient revocation or powered-off rediscovery");
            ReplayPair(c, factory, sharingLeft, sharingRight, restored, limits, 2, "private-discoveries to shared-recipient-and-own-forgetting replay");
            restored = SensorPair(factory.Load(pair.A.Save()), factory.Load(pair.B.Save()));
            var lateLeft = restored.A.RecordReplay(limits); var lateRight = restored.B.RecordReplay(limits);
            int lastSupply = Submit(c, pair, Sensor.SupplyRouteCommand(pair.A, 4));
            c.Equal(lastSupply, Submit(c, restored, Sensor.SupplyRouteCommand(restored.A, 4)), "late saved delivery preserves normal command sequence");
            Both(pair, restored, game => Sensor.Stock(game) == Fix.FromInt(6), "last four finite source units are physically delivered for rediscovery");
            int lastRoute = checked((int)Result(pair.A, 0, lastSupply).Value.Arg0);
            foreach (var current in new[] { pair, restored }) Submit(c, current, new RemoveRouteCommand { Route = lastRoute });
            Both(pair, restored, SensorNoHaul, "last finite delivery and departure complete");
            foreach (var current in new[] { pair, restored }) { var control = Sensor.Control(current.A, true); control.Target = Fix.Half; Submit(c, current, control); }
            Both(pair, restored, game => Sensor.Known(game, "reset") && Sensor.Known(game, "retain"), "low-rate actual utility permits earned rediscovery after forgetting");
            foreach (var current in new[] { pair, restored }) Submit(c, current, new CancelTransformationCommand {
                Transformer = Sensor.Find(current.A, "sensor.machine"), Capability = Sensor.Power, Index = 0 });
            Both(pair, restored, game => Sensor.Process(game).QueueCount == 0, "normal cancellation retires original continuous intent without refunding consumed utility");
            var retainedRecipient = SensorRecipient(pair.A);
            c.That(recipient.LearnedTick == retainedRecipient.LearnedTick && recipient.ObservedTick == retainedRecipient.ObservedTick &&
                Sensor.Known(pair.A, "reset", 1) && Sensor.Known(pair.A, "retain", 1), "own rediscovery does not rewrite recipient's independent original observation");
            c.That(partial.Entries.All(row => !row.Known && row.Progress.WholeTicks == 1), "retained partial publication stays detached through sharing, forgetting and rediscovery");
            c.That(SensorOtherStock(pair.A, "source").IsZero && SensorOtherStock(pair.A, "machine").IsZero &&
                pair.Stalls > 0 && restored.Stalls > 0 && pair.ReversedBatches > 0 && restored.ReversedBatches > 0,
                "no free source/cargo remains and real original/restored native transports stalled and reordered");
            c.Equal(pair.A.Hash(), factory.Load(pair.A.Save()).Hash(), "settled original utility, knowledge and spatial observations restore exactly");
            ReplayPair(c, factory, lateLeft, lateRight, restored, limits, 4, "forgotten-own-facts to finite-low-rate-rediscovery replay");
            ReplayPair(c, factory, left, right, pair, limits, 20, "full physical powered exploration sharing and forgetting replay");
            c.That(pair.A.Hash() == pair.B.Hash() && pair.A.Hash() == restored.A.Hash() && restored.A.Hash() == restored.B.Hash(),
                "all four genuinely advanced powered exploration sessions converge");
        }
    }
}
