using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Boundary infrastructure observes public engine facts; no resource/gameplay/state writes.
    internal static class PhysicalFailureDiagnosticsTests
    {
        internal const string Case = "Failure diagnostics: normal update starvation paused corruption and blocked process boundaries";
        internal static void Register() => TestRunner.Add(Case, Run);
        private sealed class Limits
        {
            public int Updates, Timeout, Grace, Output;
            public Limits()
            {
                using var stream = typeof(Program).Assembly.GetManifestResourceStream("tests.physical-failure-profile.json")
                    ?? throw new InvalidOperationException("explicit physical failure profile missing");
                using var text = new StreamReader(stream); var r = new JsonReader(Json.Parse(text.ReadToEnd()), "physical failure limits");
                Updates = r.Int("update_budget"); Timeout = r.Int("child_timeout_ms"); Grace = r.Int("termination_timeout_ms"); Output = r.Int("output_chars"); r.End();
                if (Updates <= 0 || Timeout <= 0 || Grace <= 0 || Output <= 0) throw new InvalidOperationException("positive explicit bounds required");
            }
        }
        private static GameFactory Factory()
            => ResourceContentContractTests.Factory(false, new DirectResourceContentModule());
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest
        { Scenario = "points", Seed = 10019, Seats = { Controller.Human, Controller.Human } });
        private static void Publish(SimGame g, string label)
        {
            string hash = g.Fault == null ? g.Hash().ToString(System.Globalization.CultureInfo.InvariantCulture) : "unavailable-faulted";
            var work = g.Seat(0).View.Feature<WorkView>(); var jobs = new List<WorkJobView>();
            if (g.Fault == null && work != null) work.CopyJobs(jobs);
            var id = g.Seat(0).Snapshot.Entities.FirstOrDefault(x => x.Def == "destination").Id;
            TransformationSnapshot state = default;
            bool hasQueue = g.Fault == null && id.IsValid && g.Seat(0).View.Feature<TransformationManagementView>().TryGetTransformation(id, new Code("process"), out state);
            Console.WriteLine("BOUNDARY label=" + label + " scenario=" + g.Scenario + " seed=" + g.Seed + " build=" + EngineInfo.Build +
                " format=" + EngineInfo.SaveFormat + " step=" + g.Clock.StepIndex + " snapshot=" + g.Seat(0).Snapshot.Tick +
                " hash=" + hash + " state=" + g.State + " fault=" + g.Fault + " modules=" + string.Join(",", g.Modules) +
                " queue=" + (hasQueue ? state.QueueCount.ToString() : "unavailable") + " phase=" + (hasQueue ? state.Phase.ToString() : "unavailable") +
                " blocker=" + (hasQueue ? state.Blocker.ToString() : "unavailable") +
                " jobs=" + string.Join(";", jobs.Select(x => x.Id + "/" + x.State + "/" + x.Blocker)));
            Console.Out.Flush();
        }
        private static void FaultChild(Limits limits, string input)
        {
            string behavior = input == "engine-fault" ? "throw" : input == "blocked" ? "block" : "noisy-block";
            var f = PlanetX.Factory(FrameworkProofContent.Read("update-watchdog-master.json"), new GameModule[] { new UpdateWatchdogProbeModule() },
                new Dictionary<string, string> { ["update-watchdog.json"] = FrameworkProofContent.Read("update-watchdog.json") });
            var g = f.NewGame(new NewGameRequest { Scenario = "watchdog-" + behavior, Seed = 10019, Seats = { Controller.Human, Controller.Human } });
            try { TestKit.UpdateUntil(g, () => { Publish(g, input); return false; }, limits.Updates); }
            catch (SimulationWaitException failure)
            {
                Console.WriteLine("WAIT_FAILURE reason=" + failure.Reason + " unit=" + failure.BudgetUnit + " fault=" + failure.Fault +
                    " hash=" + failure.StateHash + " unavailable=" + failure.HashUnavailable + " snapshot-state=" + failure.Snapshot.State);
                Console.Out.Flush(); throw;
            }
        }
        private static void Child(Check c, Limits limits, string input)
        {
            if (input == "engine-fault" || input == "blocked" || input == "noisy-block") { FaultChild(limits, input); return; }
            var f = Factory(); var idle = New(f); ulong untouched = idle.Hash(); var g = New(f);
            EntityId target = g.Seat(0).Snapshot.Entities.Single(x => x.Def == "destination").Id;
            var view = g.Seat(0).View.Feature<TransformationManagementView>();
            if (input == "corrupt")
            {
                var saved = g.Save(); saved[24] ^= 1; ulong before = g.Hash(); Publish(g, input);
                c.Throws<CorruptStateException>(() => f.Load(saved), "real damaged save refuses before world load");
                c.Equal(before, g.Hash(), "corrupt save attempt changes no healthy game");
                c.Equal(untouched, idle.Hash(), "corrupt save attempt changes no other session");
                Console.WriteLine("CORRUPT_REFUSED scenario=" + g.Scenario + " step=" + g.Clock.StepIndex + " healthy-hash=" + g.Hash());
                return;
            }
            if (input != "starved" && input != "paused" && input != "healthy") throw new ArgumentException(nameof(input));
            PermanentRemovalTests.Submit(c, g, new EnqueueTransformationCommand
            { Transformer = target, Capability = new Code("process"), Transformation = "direct.batch" });
            if (input == "paused") g.Clock.Pause();
            try
            {
                int calls = TestKit.UpdateUntil(g, () =>
                {
                    Publish(g, input);
                    return input == "healthy" ? g.Clock.StepIndex >= 3 : view.TryGetTransformation(target, new Code("process"), out var state) && state.QueueCount == 0;
                }, limits.Updates);
                c.That(input == "healthy" && calls > 0, "explicit healthy public observation reaches normal update condition");
                c.Equal(untouched, idle.Hash(), "bounded normal lifecycle observations are isolated");
            }
            catch (SimulationWaitException failure)
            {
                Publish(g, input + "-failure");
                Console.WriteLine("WAIT_FAILURE reason=" + failure.Reason + " unit=" + failure.BudgetUnit + " completed=" +
                    failure.CompletedSteps + "/" + failure.AllowedSteps + " clock=" + failure.ClockStep + " hash=" + failure.StateHash +
                    " snapshot=" + failure.Snapshot.Tick + " modules=" + string.Join(",", failure.Modules));
                c.That(failure.BudgetUnit == SimulationWaitBudgetUnit.LifecycleUpdates && failure.CompletedSteps == limits.Updates,
                    "unmet normal wait counts host lifecycle calls, never invented game age");
                c.That(failure.StateHash == g.Hash() && failure.Snapshot.Tick == g.Clock.StepIndex, "failure captures actual healthy detached diagnostics");
                if (input == "paused") c.Equal(1L, failure.ClockStep, "paused wait never fast-forwards physical work");
                var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "stalled real queue saves without resource/progress fallback");
                c.Equal(untouched, idle.Hash(), "failure capture does not change other session");
                Console.Out.Flush(); throw;
            }
        }
        private static void Run(Check c)
        {
            var limits = new Limits();
            if (TestRunner.CaseInput != null) { Child(c, limits, TestRunner.CaseInput); return; }
            foreach (string input in new[] { "healthy", "starved", "paused", "corrupt", "engine-fault", "blocked", "noisy-block" })
            {
                var result = BoundedTestProcess.Run(Case, limits.Timeout, limits.Grace, limits.Output, input);
                Console.WriteLine("CHILD_RESULT input=" + input + " status=" + result.Status + " pid=" + result.ProcessId +
                    " exit=" + result.ExitCode + " elapsed-ms=" + result.ElapsedMilliseconds + " observer=" + result.Failure);
                c.That(result.ProcessId.HasValue && result.ExitCode.HasValue && result.Failure == null, "owned child reaches observed terminal state/" + input);
                c.That(result.Output.Length <= limits.Output && result.Error.Length <= limits.Output, "both diagnostic streams stay bounded/" + input);
                if (input != "noisy-block") c.That(result.Output.Contains("BOUNDARY") && result.Output.Contains(EngineInfo.Build), "actual engine identity/last public boundary retained/" + input);
                if (input == "healthy" || input == "corrupt")
                    c.That(result.Success && result.Status == BoundedCaseStatus.Exited, "healthy/current-corruption refusal child succeeds/" + input);
                else if (input == "blocked" || input == "noisy-block")
                    c.That(!result.Success && result.Status == BoundedCaseStatus.TimedOut && result.Output.Contains("PROVIDER_UPDATE_ENTER"),
                        "real synchronous provider blocked update is terminated with original diagnostics/" + input);
                else
                    c.That(!result.Success && result.Status == BoundedCaseStatus.Exited && result.ExitCode == 1 &&
                        result.Output.Contains("WAIT_FAILURE") && result.Output.Contains("LifecycleUpdates"), "deliberate unmet/fault workflow is explicit failure/" + input);
                if (input == "starved") c.That(result.Output.Contains("queue=1") && result.Output.Contains("phase=Supplying") &&
                    result.Output.Contains("blocker=blocker.transformation-tools"), "actual undelivered tool/material phase andblocker reach diagnostics");
                if (input == "paused") c.That(result.Output.Contains("clock=1") && result.Output.Contains("completed=" + limits.Updates), "paused lifecycle budget remains distinct from engine age");
                if (input == "corrupt") c.That(result.Output.Contains("CORRUPT_REFUSED"), "actual save corruption refusal reported");
                if (input == "engine-fault") c.That(result.Output.Contains("intentional_engine_update_fault") && result.Output.Contains("unavailable-faulted") ||
                    result.Output.Contains("faulted partial update"), "latched fault reports unavailable hash without fabricated healthy state");
                if (input == "noisy-block") c.That(result.OutputTruncated && result.ErrorTruncated, "chatty blocked provider tails are bounded");
            }
            ResourceContentContractTests.Healthy(c, false, false);
        }
    }
}
