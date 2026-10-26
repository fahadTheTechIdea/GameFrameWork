using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Beep.SimEngine.Testing
{
    /// <summary>Records checks for one suite.</summary>
    public sealed class Check
    {
        public int Passed, Failed;
        public readonly List<string> Failures = new List<string>();
        private readonly bool _verbose;
        public Check(bool verbose) { _verbose = verbose; }

        public void That(bool condition, string what)
        {
            if (condition) { Passed++; if (_verbose) Console.WriteLine("  PASS  " + what); }
            else { Failed++; Failures.Add(what); Console.WriteLine("  FAIL  " + what); }
        }

        public void Equal<T>(T expected, T actual, string what)
            => That(EqualityComparer<T>.Default.Equals(expected, actual), what + (EqualityComparer<T>.Default.Equals(expected, actual) ? "" : " (expected " + expected + ", got " + actual + ")"));

        public void Throws<TException>(Action action, string what) where TException : Exception
        {
            try { action(); That(false, what + " (nothing was thrown)"); }
            catch (TException) { That(true, what); }
            catch (Exception ex) { That(false, what + " (threw " + ex.GetType().Name + ": " + ex.Message + ")"); }
        }
    }

    /// <summary>
    /// The test run: every registered suite, in order, with PASS/FAIL lines and a count. A suite that
    /// throws counts as one failure and the run goes on. No external test framework.
    /// </summary>
    public static class TestRunner
    {
        private static readonly List<KeyValuePair<string, Action<Check>>> Suites = new List<KeyValuePair<string, Action<Check>>>();

        /// <summary>Verification-only child input; never engine/game configuration.</summary>
        public static string CaseInput { get; private set; }
        public static bool IsExactWorker { get; private set; }

        public static void Add(string name, Action<Check> suite) => Suites.Add(new KeyValuePair<string, Action<Check>>(name, suite));

        /// <summary>Detached verification registry names; listing invokes no suite body.</summary>
        public static System.Collections.ObjectModel.ReadOnlyCollection<string> CaseNames
        {
            get
            {
                var names = new List<string>();
                foreach (var suite in Suites) names.Add(suite.Key);
                return names.AsReadOnly();
            }
        }
        private static int ProfileCommand(string[] args)
        {
            if (args.Length != 2) { Console.Error.WriteLine("explicit profile command requires one JSON file"); return 2; }
            ExplicitTestProfile profile;
            try { profile = ExplicitTestProfile.Parse(System.IO.File.ReadAllText(args[1]), CaseNames); }
            catch (Exception error) { Console.Error.WriteLine("invalid explicit profile: " + error.Message); return 2; }
            int included = 0, excluded = 0, longCases = 0;
            foreach (var entry in profile.Entries)
                if (entry.Included) { included++; if (entry.Name.Contains("(long)")) longCases++; } else excluded++;
            if (args[0] == "--profile-info")
            {
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { profile.Id, registered = profile.Entries.Count,
                    included, excluded, includedLongCases = longCases, runtimeSuitesExecuted = 0 }));
                return 0;
            }
            int passed = 0, failed = 0, aborted = 0;
            foreach (var entry in profile.Entries)
            {
                if (!entry.Included) { Console.WriteLine("PROFILE EXCLUDED: " + entry.Name + " -- " + entry.ExclusionReason); continue; }
                var result = BoundedTestProcess.Run(entry.Name, entry.TimeoutMilliseconds,
                    profile.TerminationMilliseconds, profile.OutputCharacters);
                Console.Write(result.Output); Console.Error.Write(result.Error);
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { profile = profile.Id, @case = entry.Name,
                    status = result.Status.ToString(), result.ProcessId, result.ExitCode, result.ElapsedMilliseconds,
                    result.OutputTruncated, result.ErrorTruncated, result.Failure }));
                if (result.Success) passed++; else { failed++; if (result.Status != BoundedCaseStatus.Exited) aborted++; }
            }
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { profile = profile.Id, included, excluded, passedCases = passed,
                failedCases = failed, abortedCases = aborted, complete = passed + failed == included }));
            return failed == 0 && aborted == 0 && passed == included ? 0 : 1;
        }

        public static int Run(string[] args)
        {
            if (args.Length == 1 && args[0] == "--list-cases")
            { Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(CaseNames)); return 0; }
            if (args.Length > 0 && (args[0] == "--profile-info" || args[0] == "--bounded-profile")) return ProfileCommand(args);
            bool exact = false;
            CaseInput = null;
            IsExactWorker = false;
            if (args.Length > 0 && args[0] == "--bounded-exact")
            {
                if ((args.Length != 8 && args.Length != 10) || string.IsNullOrWhiteSpace(args[1]) || args[2] != "--timeout-ms" ||
                    args[4] != "--termination-timeout-ms" || args[6] != "--output-chars" ||
                    args.Length == 10 && args[8] != "--case-input" ||
                    !int.TryParse(args[3], out int timeout) || timeout <= 0 ||
                    !int.TryParse(args[5], out int termination) || termination <= 0 ||
                    !int.TryParse(args[7], out int capacity) || capacity <= 0)
                { Console.Error.WriteLine("invalid explicit bounded-exact arguments"); return 2; }
                var result = BoundedTestProcess.Run(args[1], timeout, termination, capacity, args.Length == 10 ? args[9] : null);
                Console.Write(result.Output); Console.Error.Write(result.Error);
                Console.WriteLine("bounded case: " + result.Status + ", pid=" + result.ProcessId + ", exit=" + result.ExitCode +
                    ", elapsed-ms=" + result.ElapsedMilliseconds + ", stdout-truncated=" + result.OutputTruncated +
                    ", stderr-truncated=" + result.ErrorTruncated + ", observer=" + result.Failure);
                return result.Success ? 0 : 1;
            }
            if (args.Length > 0 && args[0] == "--worker-exact")
            {
                if ((args.Length != 2 && args.Length != 4) || args.Length == 4 && args[2] != "--case-input")
                { Console.Error.WriteLine("invalid exact worker arguments"); return 2; }
                int matches = 0;
                foreach (var suite in Suites) if (string.Equals(suite.Key, args[1], StringComparison.Ordinal)) matches++;
                if (matches != 1) { Console.Error.WriteLine("exact case requires one registration; matched=" + matches); return 2; }
                CaseInput = args.Length == 4 ? args[3] : null; args = new[] { args[1] }; exact = true; IsExactWorker = true;
            }
            else if (Array.Exists(args, a => a == "--bounded-exact" || a == "--worker-exact" || a == "--case-input"))
            { Console.Error.WriteLine("explicit process mode must be first and complete"); return 2; }
            string filter = null;
            bool verbose = false, all = false;
            int skipped = 0;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-v" || args[i] == "--verbose") verbose = true;
                else if (args[i] == "--all") all = true;
                else filter = args[i];
            }
            int passed = 0, failed = 0, ran = 0;
            var total = Stopwatch.StartNew();
            foreach (KeyValuePair<string, Action<Check>> suite in Suites)
            {
                if (filter != null && (exact ? !string.Equals(suite.Key, filter, StringComparison.Ordinal) :
                    suite.Key.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)) continue;
                // A long suite (minutes of game play) runs only when asked for by name or with --all.
                if (!all && filter == null && suite.Key.Contains("(long)")) { skipped++; continue; }
                ran++;
                Console.WriteLine("== " + suite.Key);
                var check = new Check(verbose);
                var sw = Stopwatch.StartNew();
                try { suite.Value(check); }
                catch (Exception ex)
                {
                    check.Failed++;
                    Console.WriteLine("  FAIL  suite threw " + ex.GetType().Name + ": " + ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
                Console.WriteLine("   " + check.Passed + " passed, " + check.Failed + " failed (" + sw.ElapsedMilliseconds + " ms)");
                passed += check.Passed;
                failed += check.Failed;
            }
            if (skipped > 0) Console.WriteLine(skipped + " long suites skipped (run with --all)");
            Console.WriteLine("=== " + passed + " passed, " + failed + " failed in " + ran + " suites (" + total.Elapsed.TotalSeconds.ToString("0.0") + " s) ===");
            return failed == 0 && ran > 0 ? 0 : 1;
        }

    }
}
