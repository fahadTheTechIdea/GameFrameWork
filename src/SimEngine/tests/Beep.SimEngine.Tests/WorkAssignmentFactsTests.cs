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
    internal static class WorkAssignmentFactsTests
    {
        internal const string Case = "Work assignment facts: actual authored phases, private detached holds and saved player continuation";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static SimGame New(GameFactory factory)
        {
            var request = new NewGameRequest { Scenario = "native-survey", Seed = 752 };
            request.Seats.Add(Controller.Human); request.Seats.Add(Controller.Human);
            return factory.NewGame(request);
        }
        private static EntityId Entity(SimGame game, string definition)
            => game.Seat(0).Snapshot.Entities.Single(e => e.Def == definition).Id;
        private static WorkView Work(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<WorkView>();
        private static WorkExecutionFacts? Read(Check c, SimGame game, EntityId worker)
        {
            ulong hash = game.Hash();
            c.Equal(Code.None, Work(game).ReadWorkerAssignment(worker, out var value), "actual owned worker read succeeds");
            c.Equal(hash, game.Hash(), "assignment read acquires no actor, dock, stock or progress");
            return value;
        }
        private static void Step(SimGame game, SimGame twin = null)
        {
            game.Update();
            if (game.Fault != null) throw new InvalidOperationException("assignment fault at " + game.Tick + ": " + game.Fault);
            if (twin == null) return;
            twin.Update();
            if (twin.Fault != null || twin.Hash() != game.Hash())
                throw new InvalidOperationException("assignment twin divergence at " + game.Tick + ": " + twin.Fault);
        }
        private static void Until(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 300 && !done(); i++) Step(game, twin);
            if (!done())
            {
                var jobs = new List<WorkJobView>(); Work(game).CopyJobs(jobs);
                throw new InvalidOperationException(stage + "/time=" + game.Tick + "/jobs=" +
                    string.Join(";", jobs.Select(j => j.Id + "/" + j.State + "/" + j.Blocker + "/" + j.ToilIndex)));
            }
        }
        private static int Start(Check c, SimGame game, EntityId target)
        {
            var command = new PostOrderCommand { Plan = "proof.authored-executor", Target = target, Priority = 60 };
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary engine intent preview");
            c.Equal(hash, game.Hash(), "preview does not reserve a machine");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "ordinary activity accepted");
            if (!result.Accepted) throw new InvalidOperationException("refused activity " + result.Refusal);
            return (int)result.Arg0;
        }
        private static bool Has(SimGame game, EntityId worker, Func<WorkExecutionFacts, bool> match)
            => Work(game).ReadWorkerAssignment(worker, out var value).IsNone && value.HasValue && match(value.Value);
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild,EngineInfo.Build, "new native facts identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat,EngineInfo.SaveFormat, "read projection changes no native state layout");
            c.That(typeof(WorkExecutionFacts).GetProperties().All(p => p.SetMethod == null), "facts have no mutable public property");
            c.That(typeof(WorkExecutionFacts).GetProperties().All(p => p.PropertyType != typeof(Job) &&
                p.PropertyType != typeof(Order) && p.PropertyType != typeof(World) && p.PropertyType != typeof(ActivityNodeExecution)),
                "facts expose no executable owner or job handle");
            var factory = AuthoredActivityExecutorTests.Factory(); var game = New(factory);
            var limits = new ReplayLimits(4 * 1024 * 1024, 600); var recorder = game.RecordReplay(limits);
            EntityId worker = Entity(game, "survey.machine"), site = Entity(game, "survey.site");
            c.That(!Read(c, game, worker).HasValue, "idle is explicit null, not fabricated readiness");
            ulong hash = game.Hash();
            c.Equal(Codes.NotFound, Work(game, 1).ReadWorkerAssignment(worker, out var hidden), "foreign assignment stays private");
            c.That(!hidden.HasValue, "foreign read has no facts");
            c.Equal(Codes.NotFound, Work(game).ReadWorkerAssignment(new EntityId(int.MaxValue), out hidden), "absent worker refuses");
            c.That(!hidden.HasValue, "absent worker does not inherit another actor");
            c.Equal(StandardCodes.NotWorker, Work(game).ReadWorkerAssignment(site, out hidden), "owned non-worker is explicitly rejected");
            c.That(!hidden.HasValue, "non-worker has no synthetic assignment");
            c.Equal(hash, game.Hash(), "all refusal reads are pure");

            int order = Start(c, game, site);
            Until(game, () => Has(game, worker, f => f.PhasePurpose == WorkPhasePurpose.Travel), "real machine approach");
            var approaching = Read(c, game, worker).Value;
            c.Equal(order, approaching.Order, "actual parent order"); c.Equal("operate", approaching.Node, "actual graph node");
            c.Equal("activity:proof.authored-executor", approaching.Plan, "authored stable plan identity");
            c.Equal(worker, approaching.Worker, "actual reserved actor"); c.Equal(site, approaching.Target, "actual job target");
            c.Equal(site, approaching.PhaseTarget, "actual approach destination"); c.Equal(new PlayerId(0), approaching.Company, "actual company");
            c.Equal(new WorkerRoleId("survey"), approaching.Role, "actual selected authored role");
            c.Equal(WorkPhases.Travel, approaching.Phase, "actual named travel executor");
            c.That(!approaching.IsHaul && !approaching.Emergency && approaching.Job > 0, "ordinary native staffed reservation");
            c.Equal(WorkProgress.Zero, approaching.Progress, "travel has not fabricated productive work");

            Until(game, () => Has(game, worker, f => f.State == JobState.Paused), "authored productive phase wait");
            var waiting = Read(c, game, worker).Value;
            c.Equal(approaching.Job, waiting.Job, "same native actor hold through travel and wait");
            c.Equal(new Code("proof.work.delayed"), waiting.Phase, "actual module-authored productive phase");
            c.Equal((WorkPhasePurpose?)WorkPhasePurpose.Productive, waiting.PhasePurpose, "phase purpose is composed, not hard-coded name");
            c.Equal(new Code("proof.phase-wait"), waiting.Blocker, "real authored wait blocker");
            c.Equal(WorkProgress.Zero, waiting.Progress, "waiting earns no work");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "saved actual phase hold restores");
            Step(game, twin); Step(game, twin);
            c.Equal(waiting.Job, Read(c, twin, worker).Value.Job, "same saved reciprocal reservation");
            Until(game, () => Has(game, worker, f => f.Progress.WholeTicks > 0), "funded native productive work", twin);
            var working = Read(c, game, worker).Value;
            c.Equal(waiting.Job, working.Job, "native work retains the same actor");
            c.Equal(JobState.Assigned, working.State, "wait resumes native assigned state");
            c.Equal(JobState.Paused, waiting.State, "retained facts do not become live working state");
            c.Equal(WorkProgress.Zero, waiting.Progress, "retained facts do not become live progress");
            c.Equal(WorkPhases.Travel, approaching.Phase, "retained approach remains detached");
            c.Equal(game.Hash(), twin.Hash(), "saved authored wait and physical progress stay identical");
            Until(game, () => Work(game).ReadWorkerAssignment(worker, out var value).IsNone && !value.HasValue,
                "completed physical node releases its actor", twin);
            c.That(!Read(c, game, worker).HasValue, "released worker is not a tool claim");
            c.Equal(game.Hash(), twin.Hash(), "completion leaves no duplicate assignment");
            var replay = new ReplayVerifier().Verify(factory, recorder.Finish(), limits);
            c.That(replay.Success, "ordinary player commands replay actual assignment lifecycle");

            var cancelled = New(factory); worker = Entity(cancelled, "survey.machine"); site = Entity(cancelled, "survey.site");
            order = Start(c, cancelled, site);
            Until(cancelled, () => Has(cancelled, worker, f => f.State == JobState.Paused), "cancel saved wait");
            var retained = Read(c, cancelled, worker).Value;
            cancelled.Seat(0).Submit(new CancelOrderCommand { Order = order });
            twin = factory.Load(cancelled.Save()); Step(cancelled, twin);
            c.That(!Read(c, cancelled, worker).HasValue, "pending saved ordinary cancellation releases actual hold");
            c.Equal(JobState.Paused, retained.State, "cancel does not mutate copied past facts");
            c.Equal(cancelled.Hash(), twin.Hash(), "pending cancellation restores through the same owner");
        }
    }
}
