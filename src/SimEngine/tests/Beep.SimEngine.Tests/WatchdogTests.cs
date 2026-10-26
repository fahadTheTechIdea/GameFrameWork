using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class WatchdogTests
    {
        internal const string Case = "P6 watchdog: exact isolated acceptance, bounded output and blocked engine update diagnostics";
        internal static void Register(string[] args)
        {
            TestRunner.Add(Case, Run);
            // Explicit negative registration fixture; exact worker must reject ambiguity before execution.
            if (args.Length == 4 && args[0] == "--worker-exact" && args[1] == Case &&
                args[2] == "--case-input" && args[3] == "duplicate")
                TestRunner.Add(Case, _ => throw new InvalidOperationException("duplicate case must never execute"));
        }

        private static void Run(Check c)
        {
            if (TestRunner.CaseInput != null) { Child(c, TestRunner.CaseInput); return; }
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("tests.watchdog-profile.json")
                ?? throw new InvalidOperationException("missing explicit watchdog verification profile");
            using var reader = new StreamReader(stream);
            var profile = new JsonReader(Json.Parse(reader.ReadToEnd()), "watchdog verification profile");
            int timeout = profile.Int("timeout_ms"), grace = profile.Int("termination_timeout_ms"),
                capacity = profile.Int("output_chars"); profile.End();
            BoundedCaseResult Execute(string input, string name = Case)
            {
                var result = BoundedTestProcess.Run(name, timeout, grace, capacity, input);
                Console.WriteLine("PROBE_RESULT input=" + input + " status=" + result.Status + " pid=" + result.ProcessId +
                    " exit=" + result.ExitCode + " elapsed-ms=" + result.ElapsedMilliseconds +
                    " output-chars=" + result.Output.Length + " error-chars=" + result.Error.Length + " observer=" + result.Failure);
                c.That(result.ProcessId.HasValue && result.ExitCode.HasValue, "own child start and terminal exit observed/" + input);
                c.That(result.Output.Length <= capacity && result.Error.Length <= capacity, "both retained tails bounded/" + input);
                c.That(result.ElapsedMilliseconds < timeout + grace * 3L, "entire observed lifecycle bounded/" + input);
                return result;
            }
            c.Throws<ArgumentOutOfRangeException>(() => BoundedTestProcess.Run(Case, 0, grace, capacity), "zero timeout refuses without launch");
            c.Throws<ArgumentException>(() => BoundedTestProcess.Run("", timeout, grace, capacity), "empty exact name refuses");
            var healthy = Execute("healthy");
            c.That(healthy.Success && healthy.Status == BoundedCaseStatus.Exited && healthy.Failure == null, "healthy exact child succeeds");
            c.That(healthy.Output.Contains("PUBLIC_SNAPSHOT") && healthy.Output.Contains("recent-command=") &&
                healthy.Output.Contains("1 suites"), "actual public snapshots, player commands and one suite retained");
            var assertion = Execute("assertion");
            c.That(!assertion.Success && assertion.Status == BoundedCaseStatus.Exited && assertion.ExitCode == 1 &&
                assertion.Output.Contains("intentional_assertion_failure"), "ordinary assertion failure is not timeout or success");
            var driver = Execute("driver-throw");
            c.That(!driver.Success && driver.Status == BoundedCaseStatus.Exited && driver.Output.Contains("intentional_driver_exception"),
                "driver exception captured with ordinary failing exit");
            var engine = Execute("engine-throw");
            c.That(!engine.Success && engine.Status == BoundedCaseStatus.Exited && engine.Output.Contains("ENGINE_FAULT") &&
                engine.Output.Contains("proof.update-watchdog.execute") && engine.Output.Contains("intentional_engine_update_fault"),
                "native engine fault identity/phase/message retained");
            var blocked = Execute("block");
            c.That(!blocked.Success && blocked.Status == BoundedCaseStatus.TimedOut && blocked.Failure == null,
                "synchronous blocked Update times out with confirmed termination");
            c.That(blocked.Output.Contains("PUBLIC_SNAPSHOT") && blocked.Output.Contains("PROVIDER_UPDATE_ENTER") &&
                blocked.Error.Contains("PROVIDER_UPDATE_ENTER"), "last complete public diagnostics and actual entered provider retained");
            var noisy = Execute("noisy-block");
            c.That(!noisy.Success && noisy.Status == BoundedCaseStatus.TimedOut && noisy.OutputTruncated && noisy.ErrorTruncated,
                "chatty blocked provider cannot renew deadline or evade bounded tails");
            c.That(noisy.Output.Contains("still-blocked"), "tail retains actual latest provider output rather than invented progress");
            var unknown = Execute("healthy", Case + " missing");
            c.That(unknown.Status == BoundedCaseStatus.Exited && unknown.ExitCode == 2 &&
                unknown.Error.Contains("matched=0") && !unknown.Output.Contains("PUBLIC_SNAPSHOT"), "unknown exact name executes nothing");
            var partial = Execute("healthy", "P6 watchdog:");
            c.That(partial.ExitCode == 2 && partial.Error.Contains("matched=0"), "substring cannot broaden exact process gate");
            var duplicate = Execute("duplicate");
            c.That(duplicate.ExitCode == 2 && duplicate.Error.Contains("matched=2") &&
                !duplicate.Output.Contains("PUBLIC_SNAPSHOT"), "duplicate exact registrations refuse before scenario execution");
        }

        private static void Child(Check c, string input)
        {
            string behavior = input == "engine-throw" ? "throw" : input == "healthy" || input == "assertion" ||
                input == "driver-throw" ? "healthy" : input == "block" || input == "noisy-block" ? input :
                throw new ArgumentException("unknown explicit watchdog probe input");
            string master = FrameworkProofContent.Read("update-watchdog-master.json"),
                content = FrameworkProofContent.Read("update-watchdog.json");
            var factory = PlanetX.Factory(master, new GameModule[] { new UpdateWatchdogProbeModule() },
                new Dictionary<string, string> { ["update-watchdog.json"] = content });
            var game = factory.NewGame(new NewGameRequest { Scenario = "watchdog-" + behavior, Seed = 839,
                Seats = { Controller.Human, Controller.Human } });
            var isolated = factory.NewGame(new NewGameRequest { Scenario = "watchdog-healthy", Seed = 839,
                Seats = { Controller.Human, Controller.Human } });
            ulong isolatedHash = isolated.Hash();
            var mine = game.Seat(0).Snapshot.Entities.Single(e => e.Def == "planet.mine").Id;
            int sequence = game.Seat(0).Submit(new PostOrderCommand { Plan = "planet.extract", Target = mine, Priority = 60 });
            string contentDigest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(master + content +
                FrameworkProofContent.Read("planet-x.json"))));
            void Publish()
            {
                var work = game.Seat(0).View.Feature<WorkView>(); var orders = new List<OrderView>(); var jobs = new List<WorkJobView>();
                work.CopyOrders(orders); work.CopyJobs(jobs);
                var nodes = new List<string>();
                foreach (var order in orders)
                {
                    var facts = new List<ActivityNodeView>();
                    if (work.TryCopyNodes(order.Id, facts)) nodes.AddRange(facts.Select(n => order.Id + "/" + n.Id + "/" + n.State + "/" + n.Progress + "/" + n.Blocker));
                }
                Console.WriteLine("PUBLIC_SNAPSHOT scenario=" + game.Scenario + " seed=" + game.Seed + " build=" + EngineInfo.Build +
                    " save-format=" + EngineInfo.SaveFormat + " content=" + contentDigest + " step=" + game.Clock.StepIndex +
                    " hash=" + game.Hash() + " state=" + game.State + " modules=" + string.Join(",", game.Modules) +
                    " recent-command=" + sequence + "/planet.extract entities=" + game.Seat(0).Snapshot.Entities.Count +
                    " orders=" + string.Join(";", orders.Select(o => o.Id + "/" + o.State + "/" + o.Blocker)) +
                    " nodes=" + string.Join(";", nodes) + " jobs=" + string.Join(";", jobs.Select(j => j.Id + "/" + j.State + "/" + j.Blocker)));
                Console.Out.Flush();
            }
            long cursor = 0;
            for (int i = 0; i < 4; i++)
            {
                Publish(); // Last completed boundary, never claims to observe an unfinished Update.
                if (input == "driver-throw" && i == 1) throw new InvalidOperationException("intentional_driver_exception");
                game.Update();
                if (game.Fault != null)
                {
                    Console.WriteLine("ENGINE_FAULT " + game.Fault); Console.Out.Flush();
                    c.That(false, "actual native engine fault prevents successful acceptance"); return;
                }
                var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
                foreach (var result in results)
                {
                    Console.WriteLine("COMMAND_RESULT " + result.Sequence + "/" + result.Accepted + "/" + result.Refusal);
                    c.That(result.Sequence == sequence && result.Accepted, "normal player activity accepted");
                }
            }
            Publish();
            c.Equal(4L, game.Clock.StepIndex, "healthy public updates advance the sole clock");
            c.Equal(isolatedHash, isolated.Hash(), "child execution leaves other engine session unchanged");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "healthy physical work survives native checkpoint");
            if (input == "assertion") c.That(false, "intentional_assertion_failure");
        }
    }
}

