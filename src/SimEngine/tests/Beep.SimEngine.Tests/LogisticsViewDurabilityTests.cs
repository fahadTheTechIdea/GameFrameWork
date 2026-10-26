using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Ordinary player hauling and immutable facts; fault injection is the existing negative engine extension.
    internal static class LogisticsViewDurabilityTests
    {
        internal const string Case = "Logistics view durability: retained stock cargo jobs and empty queues reject first update fault";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static List<WorkJobView> Jobs(WorkView view)
        { var rows = new List<WorkJobView>(); view.CopyJobs(rows); return rows; }
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "logistics-view-fault", Seed = 914 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human);
            return factory.NewGame(request);
        }
        private static EntityId Actor(SimGame game, string name)
            => game.Seat(0).Snapshot.Entities.Single(e => e.Def == "queue." + name).Id;
        private static void Run(Check c)
        {
            var proof = new PriorActivityProofModule();
            var projection = new LogisticsProjectionFaultModule();
            var factory = SharedHaulAdmissionTests.Factory(proof, projection);
            var game = New(factory); var isolated = New(factory);
            if (!game.Modules.Contains(proof.Id) || !game.Modules.Contains(projection.Id)) throw new InvalidOperationException("negative probe not selected");
            var inventory = game.Seat(0).View.Feature<InventoryView>();
            var logistics = game.Seat(0).View.Feature<LogisticsView>();
            var work = game.Seat(0).View.Feature<WorkView>();
            var privateInventory = game.Seat(1).View.Feature<InventoryView>();
            var privateLogistics = game.Seat(1).View.Feature<LogisticsView>();
            var privateWork = game.Seat(1).View.Feature<WorkView>();
            var source = Actor(game, "source"); var target = Actor(game, "destination");
            var worker = Actor(game, "machine.a");
            if (!game.Seat(0).View.TryFindResource("process.input", out var resource))
                throw new InvalidOperationException("missing visible resource");
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build, "actual engine identity");
            c.That(inventory.TryGetCapacity(source, Code.None, out _) && !privateInventory.TryGetCapacity(source, Code.None, out _), "healthy capacity ownership");
            c.Equal(Codes.NotFound, logistics.ReadDiagnostics(EntityId.None, resource, out _), "healthy missing diagnostics still refuse normally");
            c.Equal(Codes.NotFound, logistics.ReadStockTarget(EntityId.None, resource, out _), "healthy absent stock target is unavailable");
            c.That(!work.TryReadDispatchPolicy(out _), "optional dispatch policy remains absent without a fabricated default");
            ulong before = game.Hash();
            var queue = new List<LogisticsDispatchOffer>(); logistics.CopyQueue(queue);
            var demands = new List<LogisticsDemandView>(); logistics.CopyDemands(demands);
            var routes = new List<RouteView>(); logistics.CopyRoutes(routes);
            var assignments = new List<WorkerDispatchOffer>(); work.CopyAssignments(assignments);
            var workQueue = new List<WorkDispatchOffer>(); work.CopyDispatchQueue(workQueue);
            c.That(queue.Count == 0 && demands.Count == 0 && routes.Count == 0 && assignments.Count == 0
                && workQueue.Count == 0 && game.Hash() == before, "healthy empty projections leave original state unchanged");
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000); var replay = game.RecordReplay(limits);
            foreach (var item in new[] { ("machine.a", 4), ("machine.b", 8) })
            {
                var command = new HaulResourcesCommand { Source = source, Destination = target, Worker = Actor(game, item.Item1),
                    Resource = resource, Ceiling = Fix.FromInt(item.Item2), Priority = 60 };
                c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary simultaneous haul preview");
                game.Seat(0).Submit(command);
            }
            var twin = factory.Load(game.Save());
            for (int i = 0; i < 256; i++)
            {
                game.Update(); twin.Update();
                if (game.Fault != null || twin.Fault != null || game.Hash() != twin.Hash())
                    throw new InvalidOperationException("saved physical haul diverged at " + game.Clock.StepIndex + ": " + game.Fault + "/" + twin.Fault);
                if (Jobs(work).Any(j => j.Worker == worker && j.Loaded > Fix.Zero && j.Loaded < Fix.FromInt(4))) break;
            }
            var retainedJobs = Jobs(work);
            c.That(retainedJobs.Count == 2 && retainedJobs.Any(j => j.Loaded > Fix.Zero && j.Loaded < Fix.FromInt(4)), "physical partial handling reached through Update");
            c.That(inventory.TryGetReservations(target, Code.None, out var receipts) && receipts.Count == 2, "two actual original inbound claims");
            var cargo = new List<CargoReturnView>();
            c.Equal(Code.None, logistics.CopyCargoReturns(worker, cargo), "actual partially loaded cargo projection");
            c.That(cargo.Count == 1 && cargo[0].Stock > Fix.Zero && cargo[0].Stock < Fix.FromInt(4), "detached real cargo amount");
            c.That(Jobs(privateWork).Count == 0, "healthy private job projection");
            before = game.Hash(); logistics.CopyQueue(queue); logistics.CopyDemands(demands); logistics.CopyRoutes(routes);
            work.CopyAssignments(assignments); work.CopyDispatchQueue(workQueue);
            c.Equal(before, game.Hash(), "whole callback projections do not alter current claims or physical work");
            c.Equal(before, factory.Load(game.Save()).Hash(), "partial cargo checkpoint");
            c.That(new ReplayVerifier().Verify(factory, replay.Finish(), limits).Success, "normal physical input replay before negative injection");
            long at = game.Clock.StepIndex; game.Clock.Pause(); game.Update();
            c.Equal(at, game.Clock.StepIndex, "one authoritative game clock pause");
            game.Clock.Resume();
            proof.FaultNextUpdate = true; game.Update();
            c.That(game.Fault != null && game.Fault.Message.Contains("proof.prior.first"), "real update first fault latched");
            var firstFault = game.Fault;
            foreach (var pair in new[] { (inventory, logistics, work), (privateInventory, privateLogistics, privateWork) })
            {
                foreach (var id in new[] { source, worker, EntityId.None })
                {
                    var inv = pair.Item1; var log = pair.Item2;
                    c.Throws<InvalidOperationException>(() => inv.ReadResourcePort(id, Code.None, resource, out _), "faulted port read before invalid/private/missing refusal");
                    c.Throws<InvalidOperationException>(() => inv.TryGetCapacity(id, Code.None, out _), "faulted capacity read before private/missing refusal");
                    IReadOnlyList<ResourceReservation> failed = receipts;
                    c.Throws<InvalidOperationException>(() => inv.TryGetReservations(id, Code.None, out failed), "faulted reservation read");
                    c.That(failed.Count == 0, "failed reservation output cleared");
                    c.Throws<InvalidOperationException>(() => log.ReadDiagnostics(id, resource, out _), "faulted operational diagnostics");
                    c.Throws<InvalidOperationException>(() => log.ReadIncomingThroughput(id, resource, 0, at, out _), "faulted incoming throughput");
                    c.Throws<InvalidOperationException>(() => log.ReadOutletPolicy(id, out _), "faulted outlet policy");
                    c.Throws<InvalidOperationException>(() => log.PreviewOutletPolicy(id, null), "faulted outlet preview");
                    c.Throws<InvalidOperationException>(() => log.ReadStockPolicy(id, resource, out _), "faulted stock policy");
                    c.Throws<InvalidOperationException>(() => log.PreviewStockPolicy(id, resource, null), "faulted stock preview");
                    c.Throws<InvalidOperationException>(() => log.ReadHaulLoadPolicy(id, out _), "faulted load policy");
                    c.Throws<InvalidOperationException>(() => log.PreviewHaulLoadPolicy(id, null), "faulted load preview");
                    c.Throws<InvalidOperationException>(() => log.ReadHaulTripPolicy(id, out _), "faulted trip policy");
                    c.Throws<InvalidOperationException>(() => log.PreviewHaulTripPolicy(id, null), "faulted trip preview");
                    c.Throws<InvalidOperationException>(() => log.ReadHaulTripServiceSources(id, out _), "faulted service sources");
                    c.Throws<InvalidOperationException>(() => log.ReadHaulTripServiceWindow(id, out _), "faulted service movement window");
                    c.Throws<InvalidOperationException>(() => log.ReadHaulTripBudget(id, out _), "faulted trip budget");
                    c.Throws<InvalidOperationException>(() => log.ReadCargoDisposition(id, out _), "faulted cargo disposition");
                    c.Throws<InvalidOperationException>(() => log.PreviewHaul(source, id, resource, Fix.One), "faulted haul preview");
                    c.Throws<InvalidOperationException>(() => log.PreviewRoute(id, target, resource, null), "faulted route preview");
                    c.Throws<InvalidOperationException>(() => log.ReadStockTarget(id, resource, out _), "faulted target cannot look like zero");
                    var failedCargo = new List<CargoReturnView>(cargo);
                    c.Throws<InvalidOperationException>(() => log.CopyCargoReturns(id, failedCargo), "faulted cargo list refuses");
                    c.Equal(0, failedCargo.Count, "failed cargo list cleared");
                }
                var inv2 = pair.Item1; var log2 = pair.Item2; var jobs2 = pair.Item3;
                c.Throws<InvalidOperationException>(() => inv2.StoreAccepts("missing", resource, Code.None), "faulted content absence cannot bypass gate");
                var failedJobs = new List<WorkJobView>(retainedJobs);
                c.Throws<InvalidOperationException>(() => jobs2.CopyJobs(failedJobs), "faulted live/private/empty jobs refuse");
                c.Equal(0, failedJobs.Count, "failed jobs cleared");
                var dq = new List<LogisticsDispatchOffer> { default };
                c.Throws<InvalidOperationException>(() => log2.CopyQueue(dq), "faulted empty logistics queue refuses"); c.Equal(0, dq.Count, "queue output cleared");
                var ds = new List<LogisticsDemandView> { default };
                c.Throws<InvalidOperationException>(() => log2.CopyDemands(ds), "faulted empty demands refuse"); c.Equal(0, ds.Count, "demands output cleared");
                var rt = new List<RouteView> { default };
                c.Throws<InvalidOperationException>(() => log2.CopyRoutes(rt), "faulted empty routes refuse"); c.Equal(0, rt.Count, "routes output cleared");
                var wq = new List<WorkDispatchOffer> { default };
                c.Throws<InvalidOperationException>(() => jobs2.CopyDispatchQueue(wq), "faulted empty work queue refuses"); c.Equal(0, wq.Count, "work queue cleared");
                var wa = new List<WorkerDispatchOffer> { default };
                c.Throws<InvalidOperationException>(() => jobs2.CopyAssignments(wa), "faulted empty assignments refuse"); c.Equal(0, wa.Count, "assignments cleared");
                c.Throws<InvalidOperationException>(() => jobs2.TryReadDispatchPolicy(out _), "faulted absent dispatch policy refuses");
            }
            c.Throws<InvalidOperationException>(() => game.Save(), "faulted physical state cannot save");
            c.Throws<InvalidOperationException>(() => game.Hash(), "faulted physical state cannot hash healthy");
            c.That(ReferenceEquals(firstFault, game.Fault) && receipts.Count == 2 && cargo[0].Stock > Fix.Zero
                && retainedJobs.Any(j => j.Loaded > Fix.Zero), "first fault and previously detached claims cargo jobs remain unchanged");
            isolated.Update();
            var healthyInventory = isolated.Seat(0).View.Feature<InventoryView>();
            c.That(isolated.Fault == null && healthyInventory.TryGetReservations(Actor(isolated, "source"), Code.None, out var empty)
                && empty.Count == 0 && Jobs(isolated.Seat(0).View.Feature<WorkView>()).Count == 0, "independent session still healthy with real empty state");
            twin.Update(); c.That(twin.Fault == null, "saved physical twin unaffected by faulted session");
            var callbackGame = New(factory);
            var callbackWork = callbackGame.Seat(0).View.Feature<WorkView>();
            var callbackInventory = callbackGame.Seat(0).View.Feature<InventoryView>();
            var callbackRows = new List<WorkerDispatchOffer> { default };
            projection.ThrowNextProjection = true;
            c.Throws<InvalidOperationException>(() => callbackWork.CopyAssignments(callbackRows), "actual source callback throws after private prefix");
            c.That(callbackRows.Count == 0 && callbackGame.Fault != null
                && callbackGame.Fault.Message == "InvalidOperationException: proof.logistics.projection.first"
                && callbackGame.Fault.Phase == "observation.provider", "whole query latches original error and publishes no successful prefix");
            var callbackFault = callbackGame.Fault;
            c.Throws<InvalidOperationException>(() => callbackInventory.TryGetCapacity(EntityId.None, Code.None, out _), "callback-faulted missing stock query refuses");
            c.Throws<InvalidOperationException>(() => callbackWork.TryReadDispatchPolicy(out _), "callback-faulted absent policy query refuses");
            c.Throws<InvalidOperationException>(() => callbackGame.Save(), "callback-faulted state cannot save");
            c.Throws<InvalidOperationException>(() => callbackGame.Hash(), "callback-faulted state cannot hash healthy");
            c.That(ReferenceEquals(callbackFault, callbackGame.Fault), "secondary rejections preserve first callback error");
            isolated.Update(); c.That(isolated.Fault == null, "negative callback does not corrupt an interleaved healthy session");
        }
    }
}
