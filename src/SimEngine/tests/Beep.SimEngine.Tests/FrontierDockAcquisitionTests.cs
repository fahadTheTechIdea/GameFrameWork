using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.EmergencyPreemptionTests;

namespace Beep.SimEngine.Tests
{
    internal static class FrontierDockAcquisitionTests
    {
        internal const string Case = "Response service selection: exact joint bays, fresh refusal and saved acquisition";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static readonly Code Source = new Code("damage"), Bay = new Code("unload.bay"), AlternateBay = new Code("unload.bay.b");
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "points", Seed = 619 }; request.Mods.Add("proof.shipments");
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human); return factory.NewGame(request);
        }
        private static IncidentPreparednessSnapshot Read(SimGame game)
        {
            var code = game.Seat(0).View.Feature<IncidentsView>().ReadPreparedness(Find(game, "destination").Id, Source, out var facts);
            if (!code.IsNone) throw new InvalidOperationException("service preparedness: " + code); return facts;
        }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update(); twin?.Update();
            if (game.Fault != null || twin?.Fault != null) throw new InvalidOperationException("service Update: " + game.Fault + "/" + twin?.Fault);
        }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 300 && !done(); i++) Step(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " jobs=" +
                string.Join(";", Board(game).Jobs.Select(j => j.Id + "/" + j.State + "/" + j.Worker + "/" + j.ToilIndex + "/" + j.DestinationDockSlot + "/" + j.Blocker)));
        }
        private static void Homes(Check c, SimGame game)
        {
            foreach (string id in new[] { "machine", "prepared.worker.1" })
                c.That(TestKit.Order(game, 0, new SetHomeCommand { Worker = Find(game, id).Id, Home = Find(game, "source").Id }).Accepted,
                    "player records actual future home so departure traffic clears physically");
        }
        private static Code Exact(SimGame game, Entity worker, Code slot, out Code selected)
            => Board(game).PreviewDock(Find(game, "destination"), worker, WorkDockRequest.Work(worker.Get<IWorker>().Can(new WorkerRoleId("haul"))
                ? new WorkerRoleId("haul") : new WorkerRoleId("inspect")), null, out selected, out _, out _, out _, productiveOnly: true, requiredSlot: slot);
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "exact service acquisition identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "existing job owns actual selected bay, no additional codec");
            var factory = FrontierDockFeasibilityTests.Fixture(two: true, constrained: true); var game = New(factory);
            Entity first = Find(game, "machine"), inspector = Find(game, "prepared.worker.1"); ulong hash = game.Hash(); var facts = Read(game);
            c.That(facts.ReadyForFrontier && facts.FrontierWorkersCovered == 2, "actual two machines jointly feasible");
            c.Equal(AlternateBay, facts.FrontierDocks.Single(d => d.Worker == first.Id).Slot, "same joint selector backtracks unrestricted first worker to later bay");
            c.Equal(Bay, facts.FrontierDocks.Single(d => d.Worker == inspector.Id).Slot, "area-constrained inspector retains sole compatible bay");
            c.Equal(Code.None, Exact(game, first, AlternateBay, out Code exact), "fresh exact bay admitted by original geometry/route/policy owner");
            c.Equal(AlternateBay, exact, "exact fresh selection never returns default bay");
            c.That(!Exact(game, inspector, AlternateBay, out exact).IsNone && exact.IsNone, "area-invalid exact request never substitutes otherwise available bay");
            c.That(!Exact(game, first, new Code("missing.bay"), out exact).IsNone && exact.IsNone, "missing slot identity never substitutes default");
            c.That(!Exact(game, first, new Code("unload.wait.a"), out exact).IsNone && exact.IsNone, "exact waiting bay cannot become productive admission");
            c.Equal(hash, game.Hash(), "exact checks and joint search acquire nothing");
            c.Equal(hash, factory.Load(game.Save()).Hash(), "pure exact selection adds no saved state");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<IncidentsView>().ReadPreparedness(Find(game, "destination").Id, Source, out var hidden), "private exact bay facts company-scoped");
            c.That(hidden == null, "foreign refusal has no selected identities");
            c.Throws<NotSupportedException>(() => ((IList<IncidentPreparednessDock>)facts.FrontierDocks).Clear(), "retained selected bay facts detached immutable");

            var refused = New(factory); var board = Board(refused); Entity refusedInspector = Find(refused, "prepared.worker.1");
            Job attempted = board.AddJob(new PlayerId(0), new WorkerRoleId("inspect"), 0, Find(refused, "destination").Id);
            hash = refused.Hash(); var candidate = new WorkWorkerCandidate(refusedInspector.Id, new WorkerRoleId("inspect"), 0, WorkRequirementResult.Satisfied(), 0, 0);
            c.That(!board.Assign(attempted, candidate, AlternateBay, null), "actual acquisition refuses exact policy-invalid bay before any worker/stock mutation");
            c.That(attempted.State == JobState.Open && !attempted.Worker.IsValid && attempted.DestinationDockSlot.IsNone && refusedInspector.Get<IWorker>().Idle,
                "refused acquisition has no substituted slot, worker or live claim");
            c.Equal(hash, refused.Hash(), "fresh acquisition refusal is exact and leaves saved state unchanged");
            var dockless = New(FrontierDockFeasibilityTests.Fixture(docked: false)); hash = dockless.Hash();
            c.Equal(WorkDockCodes.Missing, Exact(dockless, Find(dockless, "machine"), Bay, out exact), "dockless work cannot satisfy a required real bay");
            c.That(exact.IsNone && dockless.Hash() == hash, "no imaginary service slot or mutation");

            Homes(c, game); Order order = Raise(c, game);
            Until(game, () => order.Nodes.Take(2).All(n => Board(game).Job(n.JobId)?.Worker.IsValid == true), "actual selected bays acquired");
            Job a = Board(game).Job(order.Node("a.flex").JobId), b = Board(game).Job(order.Node("b.inspect").JobId);
            c.Equal(first.Id, a.Worker, "first actual machine not substituted"); c.Equal(inspector.Id, b.Worker, "constrained actual inspector not substituted");
            c.Equal(AlternateBay, a.DestinationDockSlot, "first native acquisition honors joint backtracked slot");
            c.Equal(Bay, b.DestinationDockSlot, "fresh rebuild preserves later inspector's usable bay");
            c.That(!Board(game).IsDockWaiting(a) && !Board(game).IsDockWaiting(b), "feasible service set does not turn into unnecessary physical waiting");
            c.That(Read(game).ReadyForFrontier, "actual acquired slots agree with fresh preparedness");
            c.That(!Exact(game, inspector, AlternateBay, out exact).IsNone && exact.IsNone, "actual first job's live claim is not bypassed by retained intention");
            c.Equal(Fix.FromInt(8), Find(game, "destination").Get<IOutletPort>().Amount(Supply(game)), "selection/acquisition does not instantly perform productive work");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "real native selected slots saved with existing jobs");
            game.Clock.Pause(); twin.Clock.Pause(); hash = game.Hash(); long time = game.Tick; Step(game, twin); Read(game);
            c.That(game.Hash() == hash && game.Tick == time, "sole paused clock freezes exact service acquisition's physical follow-through");
            game.Clock.Resume(); twin.Clock.Resume();
            Until(game, () => order.Node("a.flex").Progress.WholeTicks > 0 && order.Node("b.inspect").Progress.WholeTicks > 0, "parallel actual service work", twin);
            c.Equal(game.Hash(), twin.Hash(), "saved travelling selected bays produce identical physical work");
            twin = factory.Load(game.Save()); Until(game, () => order.State == OrderState.Done, "actual departures, home traffic and restart", twin);
            c.Equal(game.Hash(), twin.Hash(), "partial selected-bay work saves and completes identically");
            c.Equal(Fix.Zero, Find(game, "destination").Get<IOutletPort>().Amount(Supply(game)), "existing native owner consumes eight units once");
            c.That(!Find(game, "destination").Get<IIncidentResponse>(Source).ReadIncident().Active && a.DestinationDockSlot.IsNone && b.DestinationDockSlot.IsNone,
                "only physical completion settles incident and clears native slot intentions");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed selected service set needs no extra state");
            c.That(facts.ReadyForFrontier && facts.FrontierDocks.Single(d => d.Worker == first.Id).Slot == AlternateBay, "retained prospective choice remains frozen");

            factory = FrontierDockFeasibilityTests.Fixture(); game = New(factory); Homes(c, game);
            c.That(!Read(game).ReadyForFrontier, "one service bay remains an honest simultaneous shortage"); order = Raise(c, game);
            Until(game, () => order.Nodes.Take(2).All(n => Board(game).Job(n.JobId)?.Worker.IsValid == true), "ordinary queue acquired without a feasible joint selection");
            c.That(order.Nodes.Take(2).Any(n => Board(game).IsDockWaiting(Board(game).Job(n.JobId))), "joint refusal preserves real ordinary waiting rather than dropping work");
            twin = factory.Load(game.Save()); Until(game, () => order.State == OrderState.Done, "real ordinary queue completes physically", twin);
            c.Equal(game.Hash(), twin.Hash(), "ordinary queue save/load remains deterministic");
            c.Equal(Fix.Zero, Find(game, "destination").Get<IOutletPort>().Amount(Supply(game)), "queued work also conserves real materials");
        }
    }
}
