using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Testing
{
    /// <summary>What every suite needs to play a game the way a client does: send an order, read its answer, run until something is true.</summary>
    public static class TestKit
    {
        /// <summary>Assertion-only comparison of engine facts for an unclaimed blocked node.
        /// Committed work has receipt/choice context and must not be compared to a new intent.</summary>
        public static void AssertUnclaimedRequirementParity(Check check, SimGame game, int seat,
            string activity, EntityId target, int order, string node)
        {
            ulong before = game.Hash(); long time = game.Clock.StepIndex;
            var view = game.Seat(seat).View.Feature<WorkView>();
            var proposed = new List<WorkRequirementView>(); var current = new List<WorkRequirementView>();
            var nodes = new List<ActivityNodeView>();
            check.Equal(Code.None, view.PreviewOrderRequirements(activity, target, proposed), "valid blocked intent retains requirement diagnostics");
            check.That(view.TryCopyOrderRequirements(order, current), "actual blocked order has private requirement diagnostics");
            check.That(view.TryCopyNodes(order, nodes), "actual blocked node has private execution facts");
            var actual = nodes.Single(value => value.Id == node);
            check.That(!actual.Worker.IsValid && actual.Resources.Count == 0 && actual.Choices.Count == 0
                && actual.Progress.Equals(WorkProgress.Zero), "parity comparison uses unclaimed execution context only");
            var a = proposed.Single(value => value.Node == node && value.Path.Count == 1);
            var b = current.Single(value => value.Node == node && value.Path.Count == 1);
            check.Equal(a.Requirement, b.Requirement, "proposed and active facts identify the same authored root");
            check.That(a.Path.SequenceEqual(b.Path), "proposed and active root paths remain identical");
            check.Equal(a.Result.Status, b.Result.Status, "same unclaimed context has the same typed readiness status");
            check.That(!actual.Blocker.IsNone && a.Result.Status != WorkRequirementStatus.Satisfied, "actual blocked node has typed non-readiness");
            check.Equal(actual.Blocker, a.Result.Blocker, "proposed blocker equals actual scheduled blocker");
            check.Equal(actual.Blocker, b.Result.Blocker, "active requirement blocker equals actual scheduled blocker");
            check.Equal(before, game.Hash(), "readiness comparison reserves or consumes nothing");
            check.Equal(time, game.Clock.StepIndex, "readiness comparison drives no game time");
        }

        /// <summary>Submits an order through a seat, runs one tick and returns the engine's answer to it.</summary>
        public static CommandResult Order(SimGame g, int seat, Command cmd)
        {
            int seq = g.Seat(seat).Submit(cmd);
            g.RunTicks(1);
            long cursor = 0;
            var results = new List<CommandResult>();
            g.Seat(seat).ReadResults(ref cursor, results);
            for (int i = results.Count - 1; i >= 0; i--) if (results[i].Stream == CommandStreams.Player && results[i].Sequence == seq) return results[i];
            throw new InvalidOperationException("no answer");
        }

        /// <summary>Returns actual engine steps only when the observation is satisfied; unmet waits fail explicitly.
        /// Uses engine RunTicks fast-forward, not another clock. A blocked single update still needs external isolation.</summary>
        public static int RunUntil(SimGame g, Func<bool> done, int maxTicks, int step = 10)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            if (done == null) throw new ArgumentNullException(nameof(done));
            if (maxTicks < 0) throw new ArgumentOutOfRangeException(nameof(maxTicks));
            if (step <= 0) throw new ArgumentOutOfRangeException(nameof(step));
            return Await(g, done, maxTicks, remaining => g.RunTicks(System.Math.Min(step, remaining)), SimulationWaitBudgetUnit.SimulationSteps);
        }

        /// <summary>Calls the normal game lifecycle only. Pause/lockstep waits consume the observer's
        /// explicit Update-call budget, never invented simulation steps. A blocked call needs process isolation.</summary>
        public static int UpdateUntil(SimGame game, Func<bool> done, int maxUpdates)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            if (done == null) throw new ArgumentNullException(nameof(done));
            if (maxUpdates < 0) throw new ArgumentOutOfRangeException(nameof(maxUpdates));
            return Await(game, done, maxUpdates, remaining => { game.Update(); return 1; }, SimulationWaitBudgetUnit.LifecycleUpdates);
        }

        private static int Await(SimGame g, Func<bool> done, int budget, Func<int, int> advance, SimulationWaitBudgetUnit unit)
        {
            int t = 0;
            bool stopped = false;
            while (true)
            {
                if (done()) return t;
                if (stopped) throw new SimulationWaitException(g, SimulationWaitFailure.EngineStopped, t, budget, unit);
                if (t == budget) throw new SimulationWaitException(g, SimulationWaitFailure.StepLimitReached, t, budget, unit);
                int ran = advance(budget - t);
                t += ran;
                stopped = ran == 0 || g.Fault != null || !g.Outcome.IsNone;
            }
        }
    }
}
