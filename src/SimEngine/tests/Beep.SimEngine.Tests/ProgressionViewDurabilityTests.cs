using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.PlanetXProductionTests;

namespace Beep.SimEngine.Tests
{
    internal static class ProgressionViewDurabilityTests
    {
        internal const string Case = "Progression views: detached private facts and original first-fault rejection without healthy prefixes";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Fixture(out ProgressionViewProbeModule probe)
        { probe = new ProgressionViewProbeModule(); return ProgressionDeclarationTests.Factory(new InitializeProbe(), out _, viewProbe: probe); }
        private static SimGame Session(GameFactory factory) => New(factory, "progression-view-probe");
        private static List<TechnologySnapshot> Technologies(SimGame game)
        { var rows = new List<TechnologySnapshot>(); game.Seat(0).View.Feature<TechnologyView>().CopyTechnologies(rows); return rows; }
        private static List<ObjectiveSnapshot> Objectives(SimGame game)
        { var rows = new List<ObjectiveSnapshot>(); game.Seat(0).View.Feature<ObjectiveView>().CopyObjectives(rows); return rows; }
        private static void Rejected(Check c, SimGame game, TechnologyView tech, ObjectiveView objective,
            List<TechnologySnapshot> technologies, List<ObjectiveSnapshot> objectives, WorldFault first)
        {
            var oldTech = technologies.ToArray(); var oldObjectives = objectives.ToArray();
            c.Throws<InvalidOperationException>(() => tech.CopyTechnologies(technologies), "retained technology view rejects first fault");
            c.Throws<InvalidOperationException>(() => objective.CopyObjectives(objectives), "retained objective view rejects first fault");
            c.Throws<InvalidOperationException>(() => objective.Observe("missing"), "missing objective cannot masquerade as healthy absence");
            c.Throws<InvalidOperationException>(() => objective.Observe("decl.native-earned"), "hidden objective cannot bypass first fault");
            c.Throws<InvalidOperationException>(() => tech.PreviewResearch("missing", EntityId.None, 1), "missing research preview cannot bypass first fault");
            c.Throws<InvalidOperationException>(() => game.Observer.Feature<ObjectiveView>().Observe("missing"), "unseated objective path rejects first fault");
            c.Throws<InvalidOperationException>(() => game.Observer.Feature<TechnologyView>().CopyTechnologies(new List<TechnologySnapshot>()), "empty observer technology facts reject first fault");
            c.Throws<InvalidOperationException>(() => game.Observer.Feature<ObjectiveView>().CopyObjectives(new List<ObjectiveSnapshot>()), "empty observer objective facts reject first fault");
            c.Throws<InvalidOperationException>(() => game.Save(), "faulted observation cannot checkpoint");
            c.That(technologies.SequenceEqual(oldTech) && objectives.SequenceEqual(oldObjectives), "fault refusal preserves caller-owned retained lists");
            c.That(ReferenceEquals(first, game.Fault), "secondary queries retain original first fault object");
        }
        private static void Run(Check c)
        {
            var factory = Fixture(out var probe); var game = Session(factory);
            var retainedTech = Technologies(game); var retainedObjectives = Objectives(game);
            ulong initial = game.Hash();
            c.That(retainedTech.Count == 2 && retainedObjectives.Count == 3 && retainedTech.All(x => !x.Known), "actual declared private progression starts unearned");
            probe.Hidden = "decl.native-earned";
            c.That(Objectives(game).All(x => x.Objective != probe.Hidden), "hidden objective omitted from original menu");
            c.Equal(Codes.NotFound, game.Seat(0).View.Feature<ObjectiveView>().Observe(probe.Hidden).Blocker, "direct hidden objective gives no predicate facts");
            probe.Hidden = "probe.earned"; probe.ArmPredicate("throw");
            c.Equal(Codes.NotFound, game.Seat(0).View.Feature<ObjectiveView>().Observe(probe.Hidden).Blocker, "hidden entry never evaluates its armed predicate");
            c.Equal(0, probe.Reads, "hidden observation executes no predicate callback");
            probe.ArmPredicate("healthy"); probe.Hidden = null;
            c.Equal(initial, game.Hash(), "healthy observations and transient probes preserve authoritative state/RNG");
            var limits = new ReplayLimits(4 * 1024 * 1024, 4000); var recording = game.RecordReplay(limits);
            var target = Actor(game, "receiver");
            var command = new PostOrderCommand { Plan = "decl.external", Target = target, Priority = 70 };
            c.Equal(Code.None, game.Seat(0).Preview(command), "normal authored research preview admits physical work");
            game.Seat(0).Submit(command); var twin = factory.Load(game.Save());
            int updates = 0;
            while (!Technologies(game).Single(x => x.Technology == "decl.external").Known && updates++ < 256) Step(game, twin);
            c.That(updates <= 256 && Technologies(game).Single(x => x.Technology == "decl.external").Known, "native physical supply/travel/elapsed research completes within256Updates");
            Step(game, twin);
            c.Equal(Fix.One, Stock(game, "receiver", "crucible"), "real tool remains held at research owner");
            c.That(retainedTech.All(x => !x.Known) && retainedObjectives.All(x => x.State == ObjectiveState.Locked), "retained pre-update facts remain detached");
            var foreign = new List<TechnologySnapshot>(); game.Seat(1).View.Feature<TechnologyView>().CopyTechnologies(foreign);
            c.That(foreign.All(x => !x.Known), "foreign company does not inherit earned technology");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "original progression and physical chronology restore");
            c.That(new ReplayVerifier().Verify(factory, recording.Finish(), limits).Success, "ordinary player research replay uses original owners");

            foreach (string family in new[] { "technology", "objective" })
                foreach (bool pause in new[] { false, true })
                {
                    var negativeFactory = Fixture(out var negative); var failed = Session(negativeFactory);
                    var tech = failed.Seat(0).View.Feature<TechnologyView>(); var objective = failed.Seat(0).View.Feature<ObjectiveView>();
                    var technologies = Technologies(failed); var objectives = Objectives(failed);
                    var originalTech = technologies.ToArray(); var originalObjectives = objectives.ToArray();
                    negative.FailingFamily = family; negative.Pause = pause;
                    if (family == "technology") c.Throws<InvalidOperationException>(() => tech.CopyTechnologies(technologies), "technology collection rejects later visibility callback");
                    else c.Throws<InvalidOperationException>(() => objective.CopyObjectives(objectives), "objective collection rejects later visibility callback");
                    c.That(negative.Reads == 2 && failed.State == GameState.Faulted, "failure occurs after earlier local fact and latches original session fault");
                    c.That(technologies.SequenceEqual(originalTech) && objectives.SequenceEqual(originalObjectives), "failing collection publishes no prefix or caller clear");
                    c.That(pause || failed.Fault.ToString().Contains("proof.progression.visibility.first"), "first thrown callback error preserved");
                    Rejected(c, failed, tech, objective, technologies, objectives, failed.Fault);
                    Step(game); c.That(game.Fault == null, "interleaved healthy session remains independent");
                }
            foreach (string mode in new[] { "throw", "clock", "malformed" })
            {
                var negativeFactory = Fixture(out var negative); var failed = Session(negativeFactory);
                var tech = failed.Seat(0).View.Feature<TechnologyView>(); var objective = failed.Seat(0).View.Feature<ObjectiveView>();
                var technologies = Technologies(failed); var objectives = Objectives(failed); negative.ArmPredicate(mode);
                c.Throws<InvalidOperationException>(() => objective.Observe("probe.earned"), "selected predicate refuses " + mode);
                c.That(negative.Reads == 1 && failed.State == GameState.Faulted, "direct predicate query latches original first fault " + mode);
                c.That(mode != "throw" || failed.Fault.ToString().Contains("proof.progression.predicate.first"), "original predicate exception is not masked");
                Rejected(c, failed, tech, objective, technologies, objectives, failed.Fault);
            }
        }
    }
}
