using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    internal static partial class TopologyCapacityAdmissionTests
    {
        internal const string RefusalCase = "Topology capacity continuation: saved legal refusal and physical withdrawal distinguish malformed provider faults";
        private static void Refusals(Check c)
        {
            foreach (bool half in new[] { false, true }) {
                LegalRefusal(c, half);
                foreach (string mode in new[] { "unknown-quantum", "failure-raw-quantum", "inconsistent-offer" })
                    Negative(c, mode, half, 6);
            }
        }
        // Actual JSON capacity6/member: three members hold14, but a split has only12 room.
        // No World, geometry, stock, receipt, capacity or work-progress writes during play.
        private static void LegalRefusal(Check c, bool half)
        {
            var factory = Factory(true, half, pointLimit: 6); var game = ConstructionPlanTests.NewPoints(factory);
            var limits = new ReplayLimits(6 * 1024 * 1024, 6000);
            using var recording = game.RecordReplay(limits);
            TopologyErasePostconditionTests.Ready(c, factory, game, true);
            var twin = factory.Load(game.Save()); var idle = factory.Load(game.Save()); ulong isolated = idle.Hash();
            var source = Entity(game, "source"); var destination = Entity(game, "destination"); var worker = Entity(game, "machine");
            if (!game.Seat(0).View.TryFindResource("supply", out var resource)) throw new InvalidOperationException("authored capacity resource missing");
            const string line = "corridor.point-line";
            List<NetworkPoolView> Pools() => TopologyErasePostconditionTests.Pools(game, true);
            Fix Pooled() => Pools().SelectMany(row => row.Stock).Where(row => row.Resource == resource).Aggregate(Fix.Zero, (sum, row) => sum + row.Amount);
            Fix Total() => SpatialCorridorExecutionTests.Total(game, true) + Pooled();
            Fix Segment() => SpatialCorridorExecutionTests.Segment(game, true, 1);
            PlanView Plan(long id) => PermanentRemovalTests.Plan(game, id);
            void Observe() {
                if (game.Fault != null || twin.Fault != null) throw new InvalidOperationException("capacity refusal fault half=" + half + "/" + game.Fault + "/" + twin.Fault);
                c.Equal(game.Hash(), twin.Hash(), "saved valid capacity policy and original work match every Update");
                c.Equal(Fix.FromInt(14), Total(), "pending refusal haul and earned split preserve all finite remaining material");
            }
            void Step() { game.Update(); twin.Update(); Observe(); }
            void Wait(Func<bool> ready, string stage) {
                var elapsed = Stopwatch.StartNew(); int updates = 0;
                while (updates < 512 && elapsed.ElapsedMilliseconds < 20000 && !ready()) { Step(); updates++; }
                if (!ready()) throw new InvalidOperationException(stage + " exceeds512Updates/20000ms clock=" + game.Clock.StepIndex + "/half=" + half + "/elapsed=" + elapsed.ElapsedMilliseconds +
                    "/pools=" + Pools().Count + "/stock=" + Pooled() + "/jobs=" + string.Join(";", PermanentRemovalTests.Jobs(game).Select(row => row.Id + "/" + row.State + "/" + row.Blocker)));
            }
            CommandResult Send(Command command) { var result = PermanentRemovalTests.Submit(c, game, command); twin = factory.Load(game.Save()); Observe(); return result; }
            void Save() { c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "healthy capacity refusal/current promises restore canonically"); twin = factory.Load(game.Save()); }
            var old = Pools().Single(); c.That(old.MemberCount == 3 && Pooled() == Fix.FromInt(14), "actual earned stock fits18 original capacity, not hypothetical split12");
            var path = new SpatialPlacement[] { new PointPlacement("field.points", Fix.FromInt(5)) };
            ulong pure = game.Hash();
            c.Equal(NetworkService.NetworkCapacity, game.Seat(0).View.Feature<InfrastructureView>().PreviewRemoval(line, path), "valid bounded zero offer is a typed capacity refusal, not a provider fault");
            c.Equal(pure, game.Hash(), "legal capacity preview changes no geometry stock or allocator");
            long cut = Send(new SubmitPlanCommand { Intent = new PlanIntent("activity:corridor.remove", ActivityWorkerSelection.CreateArguments(Entity(game, "corridor.removal-site"), ActivityWorkerSelection.Unbound), "capacity-wait", 100, false) }).Arg0;
            c.That(Plan(cut).State == PlanState.Pending && Plan(cut).Order == 0 && Plan(cut).Blocker == NetworkService.NetworkCapacity, "player intent persists without phantom cut when actual split lacks capacity");
            Save(); for (int i = 0; i < 4; i++) Step();
            c.That(game.Fault == null && Plan(cut).Order == 0 && Pools().Single().Endpoint == old.Endpoint && Segment().IsPositive, "saved valid refusal stays healthy and does not replace original topology");
            game.Clock.Pause(); twin.Clock.Pause(); pure = game.Hash(); Step(); c.Equal(pure, game.Hash(), "one paused clock freezes pending capacity intent"); game.Clock.Resume(); twin.Clock.Resume();
            int haul = (int)Send(new HaulResourcesCommand { Source = source, SourcePort = new Code("graph"), Destination = destination, Worker = worker, Resource = resource, Ceiling = Fix.FromInt(2), Priority = 100 }).Arg0;
            Wait(() => PermanentRemovalTests.Jobs(game).Any(row => row.Id == haul && row.Loaded == Fix.FromInt(2) && row.ToilIndex == 2), "physical pickup frees two actual pool units without deleting or refunding them");
            c.That(Pooled() == Fix.FromInt(12) && Stock(game, "destination").IsZero, "original pool falls to actual split capacity while two units remain in transit");
            Save();
            Wait(() => Plan(cut).State == PlanState.Done, "same persisted cut earns physical work after capacity becomes sufficient");
            var split = Pools();
            c.That(split.Count == 2 && split.All(row => row.Endpoint != old.Endpoint && row.MemberCount == 1 && row.Stock.Single().Amount == Fix.FromInt(6)), "two new original owners fit exactly their authored six-unit limits");
            c.Equal(Fix.FromInt(2), Stock(game, "destination"), "freed material was delivered physically before sole machine could cut");
            c.That(Segment().IsZero && Total() == Fix.FromInt(14), "earned deletion conserves six plus six plus two delivered units");
            Save(); c.Equal(isolated, idle.Hash(), "shared healthy policy content isolates saved sessions");
            var hidden = new List<PlanView>(); game.Seat(1).View.Feature<WorkView>().CopyPlans(hidden); c.Equal(0, hidden.Count, "capacity-blocked and completed player intent remains private");
            c.That(new ReplayVerifier().Verify(factory, recording.Finish(), limits).Success, "actual legal refusal wait saved load physical withdrawal and cut replay");
        }
    }
}
