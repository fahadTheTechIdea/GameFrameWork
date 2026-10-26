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
    // Player inputs, clock control and detached facts only; the engine owns retries and cargo.
    internal static class RetainedRetryAcceptanceTests
    {
        internal const string Case = "Retained haul retry: public partial loading travel and delivery survive authored interruptions";
        private static readonly ReplayLimits Limits = new ReplayLimits(8 * 1024 * 1024, 5000);
        internal static void Register() => TestRunner.Add(Case, Run);
        private static bool Live(WorkJobView job) => job.State == JobState.Open || job.State == JobState.Assigned || job.State == JobState.Paused;
        private static WorkJobView Job(SimGame game, int id) => Jobs(game).Single(j => j.Id == id);
        private static Fix Position(SimGame game) => ((PointSnapshot)game.Seat(0).Snapshot.Entities.Single(e => e.Id == Id(game, "machine")).Spatial).Position;
        private static LogisticsOperationalSnapshot Facts(SimGame game, string name)
        {
            Code refusal = View(game).ReadDiagnostics(Id(game, name), Supply(game), out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("actual retained haul diagnostics " + name + "/" + refusal);
            return value;
        }
        private static ConfigureOperationCommand Available(SimGame game, bool enabled) => new ConfigureOperationCommand
        { Entity = Id(game, "machine"), Operation = new Code("availability"), Enabled = enabled, Target = Fix.One };
        private static ConfigureWorkPolicyCommand Enabled(SimGame game, bool enabled) => new ConfigureWorkPolicyCommand
        { Worker = Id(game, "machine"), Settings = new WorkPolicySettings(enabled, 0, new[] { new WorkerRoleId("inspect"), new WorkerRoleId("haul") }, null, true, true, WorkAssignmentMode.All) };
        private static void Conserved(Check c, SimGame game)
            => c.Equal(Fix.FromInt(20), Facts(game, "source").Stock + Facts(game, "destination").Stock + Facts(game, "machine").Stock, "finite original material conserved");
        private static void Held(Check c, SimGame game, WorkJobView retained, Fix position, LogisticsOperationalSnapshot source,
            LogisticsOperationalSnapshot destination, LogisticsOperationalSnapshot cargo)
        {
            var current = Job(game, retained.Id);
            c.Equal(retained.Worker, current.Worker, "same carrier claim");
            c.Equal(retained.SourceEndpoint, current.SourceEndpoint, "same original source endpoint");
            c.Equal(retained.DestinationEndpoint, current.DestinationEndpoint, "same original destination endpoint");
            c.Equal(retained.Loaded, current.Loaded, "same actually loaded cargo");
            c.Equal(retained.HandlingProgress, current.HandlingProgress, "unavailable time earns no handling work");
            c.Equal(retained.UnloadingBaseline, current.UnloadingBaseline, "committed delivery prefix never rewound");
            c.Equal(position, Position(game), "unavailable time grants no travel");
            c.Equal(1, Jobs(game).Count(j => Live(j) && j.IsHaul), "one retained job, no replacement queue");
            foreach (var pair in new[] { ("source", source), ("destination", destination), ("machine", cargo) }) {
                var facts = Facts(game, pair.Item1);
                c.Equal(pair.Item2.Stock, facts.Stock, "saved actual " + pair.Item1 + " stock retained");
                c.Equal(pair.Item2.InboundPromised, facts.InboundPromised, "original " + pair.Item1 + " receiving claim retained");
                c.Equal(pair.Item2.OutboundPromised, facts.OutboundPromised, "original " + pair.Item1 + " cargo/export claim retained");
            }
            Conserved(c, game);
        }
        private static void Run(Check c)
        {
            foreach (bool direct in new[] { false, true })
                foreach (bool finer in new[] { false, true })
                    for (int phase = 0; phase < 3; phase++) Scenario(c, direct, finer, phase);
        }
        private static void Scenario(Check c, bool direct, bool finer, int phase)
        {
            var factory = HaulRetryTests.Factory(selected: direct ? "retry.constant" : "proof.retry", edit: root => {
                if (finer) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
            });
            SimGame NewSession() => factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 915,
                Mods = { "proof.retry-module", ResourceFlowsModule.ModuleId }, Seats = { Controller.Human, Controller.Human } });
            var game = NewSession(); var isolated = NewSession(); ulong untouched = isolated.Hash();
            using var recording = game.RecordReplay(Limits);
            c.That(!game.Modules.Contains("sim.tycoon"), "retry is resource-only game logic");
            var haul = new HaulResourcesCommand { Source = Id(game, "source"), Destination = Id(game, "destination"),
                Worker = Id(game, "machine"), Resource = Supply(game), Ceiling = Fix.FromInt(8), Priority = 100 };
            ulong before = game.Hash(); c.Equal(Codes.NotFound, game.Seat(1).Preview(haul), "foreign haul refused");
            c.Equal(before, game.Hash(), "foreign preview pure");
            var twin = Input(c, factory, game, haul);
            int id = Jobs(game).Single(j => Live(j) && j.IsHaul).Id;
            Until(c, game, twin, () => {
                var job = Job(game, id);
                return phase == 0 ? job.Loaded.IsPositive && job.Loaded < job.Amount && job.ToilIndex == 1
                    : phase == 1 ? job.Loaded == job.Amount && job.ToilIndex == 2 && Position(game) > Fix.Zero && Position(game) < Fix.FromInt(10)
                    : Stock(game, "destination").IsPositive && job.Loaded.IsPositive && job.ToilIndex == 3;
            }, "real physical interruption phase=" + phase + "/direct=" + direct + "/finer=" + finer);
            twin = Partial(c, factory, game);
            var retained = Job(game, id); Fix position = Position(game);
            var source = Facts(game, "source"); var destination = Facts(game, "destination"); var cargo = Facts(game, "machine");
            c.Equal(retained.Loaded, cargo.Stock, "loaded fact equals actual cargo");
            c.Equal(Fix.FromInt(8), destination.InboundPromised + destination.Stock, "one exact original remaining inlet claim");
            twin = Input(c, factory, game, phase == 1 ? new SetPointAccessCommand { Open = false } : Available(game, false));
            Until(c, game, twin, () => Job(game, id).State == JobState.Paused, "typed retained retry pause");
            var paused = Job(game, id); Code blocker = phase == 1 ? WorkSystem.BlockerUnreachable : WorkSystem.BlockerWorkerUnavailable;
            c.Equal(blocker, paused.Blocker, "actual route/operation retry reason");
            c.Equal(1, paused.FailureCount, "first consecutive interruption");
            Held(c, game, retained, position, source, destination, cargo);
            twin = Partial(c, factory, game);
            var privateJobs = new List<WorkJobView>(); game.Seat(1).View.Feature<WorkView>().CopyJobs(privateJobs);
            c.Equal(0, privateJobs.Count, "foreign retries and reservations withheld");
            c.Equal(Codes.NotOwned, game.Seat(1).View.Feature<LogisticsView>().ReadDiagnostics(Id(game, "machine"), Supply(game), out var hidden), "foreign cargo withheld");
            c.That(hidden == null, "no foreign cargo prefix");
            Until(c, game, twin, () => Job(game, id).FailureCount == 3, "actual authored repeated retry");
            var third = Job(game, id); int factor = finer ? 2 : 1;
            c.Equal((direct ? 3L : 8L) * factor, third.RetryAt - (game.Clock.StepIndex - 1), "native capped/independent constant delay uses sole game clock");
            Held(c, game, retained, position, source, destination, cargo);
            c.Equal(1, paused.FailureCount, "retained immutable earlier view not live");
            twin = Input(c, factory, game, Enabled(game, false));
            long due = Job(game, id).RetryAt;
            for (int i = 0; i < 20; i++) { Step(c, game, twin); Held(c, game, retained, position, source, destination, cargo); }
            c.Equal(due, Job(game, id).RetryAt, "policy pause preserves expired deadline");
            c.Equal(3, Job(game, id).FailureCount, "policy pause makes no new retries");
            twin = Partial(c, factory, game);
            twin = Input(c, factory, game, phase == 1 ? new SetPointAccessCommand { Open = true } : Available(game, true));
            Held(c, game, retained, position, source, destination, cargo);
            c.Equal(due, Job(game, id).RetryAt, "restoration cannot bypass disabled work policy");
            twin = Input(c, factory, game, Enabled(game, true));
            c.That(!Jobs(game).Any(j => Live(j) && j.IsHaul && j.Id != id), "restored input creates no replacement job");
            c.That(Stock(game, "destination") < Fix.FromInt(8), "restoration cannot instantly deliver remaining physical cargo");
            Until(c, game, twin, () => Stock(game, "destination") == Fix.FromInt(8) && !Jobs(game).Any(j => Live(j) && j.IsHaul), "same saved physical haul completes once");
            c.Equal(Fix.FromInt(12), Stock(game, "source"), "exact eight units exported once");
            c.Equal(Fix.Zero, Stock(game, "machine"), "no stranded cargo after delivery");
            Conserved(c, game);
            foreach (string name in new[] { "source", "destination", "machine" }) {
                var facts = Facts(game, name); c.Equal(Fix.Zero, facts.InboundPromised, "no inlet claim leak " + name);
                c.Equal(Fix.Zero, facts.OutboundPromised, "no outlet claim leak " + name);
            }
            for (int i = 0; i < 12; i++) { Step(c, game, twin); Conserved(c, game); }
            c.Equal(Fix.FromInt(8), Stock(game, "destination"), "later updates never duplicate delivery");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "terminal original state restored");
            c.Equal(untouched, isolated.Hash(), "other session gets no clock/claims/retries");
            var replay = new ReplayVerifier().Verify(factory, recording.Finish(), Limits);
            c.That(replay.Success && replay.Game != null && replay.Game.Hash() == game.Hash(), "public interruption replay " + replay.Failure + "/" + replay.Detail);
        }
    }
}
