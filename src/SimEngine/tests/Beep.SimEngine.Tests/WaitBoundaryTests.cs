using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Tests
{
    internal static class WaitBoundaryTests
    {
        internal const string Case = "P6 wait boundary: explicit unmet predicates and bounded CLI acceptance";
        internal static void Register() => TestRunner.Add(Case, Run);

        private sealed class Limits
        {
            public readonly int Steps, Batch, Physical, ChildTimeout, ObserverTimeout, Grace, Output;
            public Limits()
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("tests.wait-boundary-profile.json")
                    ?? throw new InvalidOperationException("explicit wait verification profile missing");
                using var text = new StreamReader(stream);
                var reader = new JsonReader(Json.Parse(text.ReadToEnd()), "wait verification profile");
                Steps = reader.Int("step_budget"); Batch = reader.Int("batch_size"); Physical = reader.Int("physical_budget");
                ChildTimeout = reader.Int("child_timeout_ms"); ObserverTimeout = reader.Int("observation_timeout_ms");
                Grace = reader.Int("termination_timeout_ms"); Output = reader.Int("output_chars"); reader.End();
                if (Steps <= 0 || Batch <= 0 || Physical <= 0 || ChildTimeout <= 0 || Grace <= 0 || Output <= 0 ||
                    ObserverTimeout <= ChildTimeout + Grace) throw new InvalidOperationException("explicit positive nested verification limits required");
            }
        }
        private static GameFactory Factory() => PlanetX.Factory(FrameworkProofContent.Read("update-watchdog-master.json"),
            new GameModule[] { new UpdateWatchdogProbeModule() },
            new Dictionary<string, string> { ["update-watchdog.json"] = FrameworkProofContent.Read("update-watchdog.json") });
        private static SimGame New(GameFactory factory, string behavior) => factory.NewGame(new NewGameRequest
            { Scenario = "watchdog-" + behavior, Seed = 853, Seats = { Controller.Human, Controller.Human } });
        private static void Run(Check c)
        {
            var limits = new Limits();
            if (TestRunner.IsExactWorker)
            {
                if (TestRunner.CaseInput == null || TestRunner.CaseInput == "healthy") { Waits(c, limits); return; }
                Child(TestRunner.CaseInput, limits); return;
            }
            Waits(c, limits);
            c.Throws<ArgumentException>(() => BoundedTestProcess.RunInvocation(new[] { "--all" },
                limits.ObserverTimeout, limits.Grace, limits.Output), "observer refuses broad/default invocation");
            string[] Arguments(string input = null, string name = Case, string timeout = null)
            {
                var args = new List<string> { "--bounded-exact", name, "--timeout-ms",
                    timeout ?? limits.ChildTimeout.ToString(CultureInfo.InvariantCulture),
                    "--termination-timeout-ms", limits.Grace.ToString(CultureInfo.InvariantCulture),
                    "--output-chars", limits.Output.ToString(CultureInfo.InvariantCulture) };
                if (input != null) { args.Add("--case-input"); args.Add(input); }
                return args.ToArray();
            }
            BoundedCaseResult Execute(string label, string[] args)
            {
                var result = BoundedTestProcess.RunInvocation(args, limits.ObserverTimeout, limits.Grace, limits.Output);
                Console.WriteLine("CLI_RESULT " + label + "/" + result.Status + "/pid=" + result.ProcessId +
                    "/exit=" + result.ExitCode + "/ms=" + result.ElapsedMilliseconds + "/observer=" + result.Failure);
                c.That(result.Status == BoundedCaseStatus.Exited && result.ProcessId.HasValue && result.ExitCode.HasValue &&
                    result.Failure == null, "actual CLI parent exit observed/" + label);
                c.That(result.Output.Length <= limits.Output && result.Error.Length <= limits.Output, "CLI output bounded/" + label);
                return result;
            }
            var ordinary = Execute("default", Arguments());
            c.That(ordinary.Success && ordinary.Output.Contains("bounded case: Exited") &&
                ordinary.Output.Contains("1 suites"), "eight-argument default CLI runs exactly one healthy worker without recursion");
            var healthy = Execute("explicit-healthy", Arguments("healthy"));
            c.That(healthy.Success && healthy.Output.Contains("bounded case: Exited"), "optional child input remains exact");
            var unmet = Execute("unmet", Arguments("unmet"));
            c.That(!unmet.Success && unmet.ExitCode == 1 && unmet.Output.Contains("SimulationWaitException") &&
                unmet.Output.Contains("StepLimitReached") && unmet.Output.Contains("bounded case: Exited"),
                "unmet wait makes CLI fail, never silent elapsed-count success");
            var fault = Execute("engine-fault", Arguments("engine-fault"));
            c.That(!fault.Success && fault.ExitCode == 1 && fault.Output.Contains("EngineStopped") &&
                fault.Output.Contains("intentional_engine_update_fault"), "native fault diagnostic reaches bounded CLI failure");
            var block = Execute("block", Arguments("block"));
            c.That(!block.Success && block.ExitCode == 1 && block.Output.Contains("bounded case: TimedOut") &&
                block.Output.Contains("WAIT_BOUNDARY") && block.Output.Contains("PROVIDER_UPDATE_ENTER"),
                "CLI watcher terminates real blocked helper update and retains actual last emitted boundary");
            var missing = Execute("missing", Arguments("healthy", Case + " missing"));
            c.That(!missing.Success && missing.Error.Contains("matched=0") && missing.Output.Contains("exit=2"),
                "unknown exact case remains a failing CLI result");
            var invalid = Execute("invalid-timeout", Arguments("healthy", Case, "0"));
            c.That(invalid.ExitCode == 2 && invalid.Error.Contains("invalid explicit bounded-exact") &&
                !invalid.Output.Contains("bounded case:"), "invalid bounds refuse before worker launch");
            var empty = Execute("empty-name", Arguments("healthy", ""));
            c.That(empty.ExitCode == 2 && empty.Error.Contains("invalid explicit bounded-exact"), "empty name refuses explicitly");
            var incomplete = Execute("incomplete", new[] { "--bounded-exact" });
            c.That(incomplete.ExitCode == 2 && incomplete.Error.Contains("invalid explicit bounded-exact"),
                "short argv refuses without indexing past input or broad execution");
        }

        private static void Waits(Check c, Limits limits)
        {
            var factory = Factory(); var game = New(factory, "healthy"); var isolated = New(factory, "healthy");
            ulong initial = game.Hash(), untouched = isolated.Hash();
            c.Equal(0, TestKit.RunUntil(game, () => true, 0, 1), "already satisfied zero-budget wait advances nothing");
            c.Equal(initial, game.Hash(), "immediate success preserves all state");
            c.Throws<ArgumentNullException>(() => TestKit.RunUntil(null, () => true, 1, 1), "null game rejected");
            c.Throws<ArgumentNullException>(() => TestKit.RunUntil(game, null, 1, 1), "null predicate rejected");
            c.Throws<ArgumentOutOfRangeException>(() => TestKit.RunUntil(game, () => true, -1, 1), "negative budget rejected");
            c.Throws<ArgumentOutOfRangeException>(() => TestKit.RunUntil(game, () => true, 1, 0), "nonpositive batch rejected");
            c.Throws<InvalidOperationException>(() => TestKit.RunUntil(game,
                () => throw new InvalidOperationException("actual_predicate_failure"), 1, 1), "predicate exception remains a real failure");
            c.Equal(initial, game.Hash(), "invalid inputs/predicate do not advance or grant anything");
            SimulationWaitException Zero()
            {
                try { TestKit.RunUntil(game, () => false, 0, 1); }
                catch (SimulationWaitException error) { return error; }
                throw new InvalidOperationException("unmet zero wait silently returned");
            }
            var zero = Zero();
            c.Equal(SimulationWaitFailure.StepLimitReached, zero.Reason, "false zero wait is explicit step limit");
            c.That(zero.CompletedSteps == 0 && zero.AllowedSteps == 0 && zero.StateHash == initial && zero.HashUnavailable == null,
                "zero failure records actual unchanged healthy hash");
            c.Equal(initial, game.Hash(), "diagnostic capture is read-only");
            int observations = 0;
            SimulationWaitException clipped = null;
            try { TestKit.RunUntil(game, () => { observations++; return false; }, limits.Steps, limits.Batch); }
            catch (SimulationWaitException error) { clipped = error; }
            if (clipped == null) throw new InvalidOperationException("unmet bounded wait silently returned");
            c.Equal(SimulationWaitFailure.StepLimitReached, clipped.Reason, "unmet bounded condition throws typed failure");
            c.That(clipped.CompletedSteps == limits.Steps && clipped.AllowedSteps == limits.Steps &&
                clipped.ClockStep == limits.Steps && game.Clock.StepIndex == limits.Steps, "last batch clips exactly to budget");
            c.Equal((limits.Steps + limits.Batch - 1) / limits.Batch + 1, observations, "one predicate evaluation per observed boundary");
            c.That(clipped.Scenario == game.Scenario && clipped.Seed == game.Seed &&
                clipped.EngineBuild == EngineInfo.Build && clipped.SaveFormat == EngineInfo.SaveFormat &&
                clipped.Modules.SequenceEqual(game.Modules) && clipped.Outcome == game.Outcome && clipped.Fault == null,
                "typed metadata is actual engine identity/state");
            c.That(clipped.StateHash == game.Hash() && clipped.Snapshot.Tick == game.Clock.StepIndex,
                "healthy failure captures current native hash and detached published epoch");
            var retained = clipped.Snapshot; long snapshotStep = retained.Tick; game.Update();
            c.That(retained.Tick == snapshotStep && game.Clock.StepIndex > snapshotStep, "failure snapshot retained across later engine update");
            var exact = New(factory, "healthy");
            c.Equal(limits.Steps, TestKit.RunUntil(exact, () => exact.Clock.StepIndex == limits.Steps,
                limits.Steps, 1), "condition satisfied at final step succeeds");
            c.Equal(0, TestKit.RunUntil(exact, () => exact.Clock.StepIndex == limits.Steps, 0, 1),
                "no extra engine step after reached condition");

            var physical = New(factory, "healthy");
            Fix Deposit()
            {
                var values = new List<Fix>();
                var status = physical.Seat(0).View.Feature<PointLayerView>().ReadLayer("planet.deposit",
                    new PointRegion(Fix.FromInt(20), Fix.One, 1), values);
                if (status != SpatialLayerReadStatus.Success || values.Count != 1) throw new InvalidOperationException("actual private deposit unavailable");
                return values[0];
            }
            var mine = physical.Seat(0).Snapshot.Entities.Single(e => e.Def == "planet.mine").Id;
            c.Equal(Fix.FromInt(12), Deposit(), "explicit real private ore layer");
            physical.Seat(0).Submit(new PostOrderCommand { Plan = "planet.extract", Target = mine });
            int elapsed = TestKit.RunUntil(physical, () => Deposit() == Fix.FromInt(10), limits.Physical, 1);
            c.That(elapsed > 1 && physical.Clock.StepIndex == elapsed, "physical travel/work consume actual sole-clock time");
            c.Equal(Fix.FromInt(10), Deposit(), "wait never fabricates extraction or supply");
            c.Equal(physical.Hash(), factory.Load(physical.Save()).Hash(), "reached physical work checkpoint survives native load");
            c.Equal(untouched, isolated.Hash(), "waits leave other session unchanged");

            var faulted = New(factory, "throw"); SimulationWaitException stopped = null;
            try { TestKit.RunUntil(faulted, () => false, limits.Physical, 1); }
            catch (SimulationWaitException error) { stopped = error; }
            if (stopped == null) throw new InvalidOperationException("stopped engine silently returned");
            c.That(stopped.Reason == SimulationWaitFailure.EngineStopped && stopped.State == GameState.Faulted &&
                stopped.Fault == faulted.Fault && stopped.Fault.Message.Contains("intentional_engine_update_fault"),
                "native fault stops wait with actual typed state/cause");
            c.That(stopped.StateHash == null && stopped.HashUnavailable.Contains("faulted partial update") &&
                stopped.Snapshot.State == GameState.Faulted && stopped.Snapshot.Tick <= stopped.ClockStep,
                "faulted diagnostics retain last safe snapshot and never fabricate a partial hash");
            c.Equal(0, TestKit.RunUntil(faulted, () => faulted.Fault != null, 0, 1),
                "explicit observation of already latched fault can succeed without resuming engine");
            c.Throws<InvalidOperationException>(() => faulted.Hash(), "diagnostics did not make faulted hash legal");
            c.Equal(untouched, isolated.Hash(), "fault diagnostics do not mutate another session");
            Console.WriteLine("WAIT_PROOF physical-steps=" + elapsed + " budget=" + limits.Steps + " clipped=" +
                clipped.CompletedSteps + " fault=" + stopped.Reason + " retained-epoch=" + stopped.Snapshot.Tick);
        }

        private static void Child(string input, Limits limits)
        {
            string behavior = input == "unmet" ? "healthy" : input == "engine-fault" ? "throw" :
                input == "block" ? "block" : throw new ArgumentException("unknown explicit wait probe input");
            var game = New(Factory(), behavior);
            bool Observe()
            {
                Console.WriteLine("WAIT_BOUNDARY scenario=" + game.Scenario + " clock=" + game.Clock.StepIndex +
                    " snapshot=" + game.Snapshot.Tick + " state=" + game.State +
                    " fault=" + game.Fault + " hash=" + (game.Fault == null ? game.Hash().ToString() : "unavailable-faulted"));
                Console.Out.Flush(); return false;
            }
            TestKit.RunUntil(game, Observe, limits.Steps, 1); // Must fail or externally time out, never silently succeed.
            throw new InvalidOperationException("unmet child wait returned");
        }
    }
}

