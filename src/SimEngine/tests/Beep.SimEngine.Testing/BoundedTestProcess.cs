using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Beep.SimEngine.Testing
{
    public enum BoundedCaseStatus { Exited, TimedOut, StartFailed, TerminationFailed }

    /// <summary>Process observations only; no simulated time, gameplay state or inferred completion.</summary>
    public sealed class BoundedCaseResult
    {
        internal BoundedCaseResult(BoundedCaseStatus status, int? processId, int? exitCode,
            long elapsed, string output, string error, bool outputTruncated, bool errorTruncated, string failure)
        {
            Status = status; ProcessId = processId; ExitCode = exitCode; ElapsedMilliseconds = elapsed;
            Output = output; Error = error; OutputTruncated = outputTruncated; ErrorTruncated = errorTruncated; Failure = failure;
        }
        public BoundedCaseStatus Status { get; }
        public int? ProcessId { get; }
        public int? ExitCode { get; }
        public long ElapsedMilliseconds { get; }
        public string Output { get; }
        public string Error { get; }
        public bool OutputTruncated { get; }
        public bool ErrorTruncated { get; }
        public string Failure { get; }
        public bool Success => Status == BoundedCaseStatus.Exited && ExitCode == 0 && Failure == null;
    }

    /// <summary>Owns only one started child, its streams and deadline. Never attaches to an unrelated PID.</summary>
    public static class BoundedTestProcess
    {
        public static BoundedCaseResult Run(string exactCase, int timeoutMilliseconds,
            int terminationTimeoutMilliseconds, int outputCharacters, string caseInput = null)
        {
            if (string.IsNullOrWhiteSpace(exactCase)) throw new ArgumentException("exact case name required", nameof(exactCase));
            var arguments = new System.Collections.Generic.List<string> { "--worker-exact", exactCase };
            if (caseInput != null) { arguments.Add("--case-input"); arguments.Add(caseInput); }
            return RunInvocation(arguments, timeoutMilliseconds, terminationTimeoutMilliseconds, outputCharacters);
        }

        /// <summary>Observes only explicit exact protocols of this same entry assembly, never a broad/default suite command.</summary>
        public static BoundedCaseResult RunInvocation(System.Collections.Generic.IReadOnlyList<string> arguments,
            int timeoutMilliseconds, int terminationTimeoutMilliseconds, int outputCharacters)
        {
            if (arguments == null || arguments.Count == 0 ||
                arguments[0] != "--bounded-exact" && arguments[0] != "--worker-exact")
                throw new ArgumentException("explicit exact process protocol required", nameof(arguments));
            if (timeoutMilliseconds <= 0 || terminationTimeoutMilliseconds <= 0 || outputCharacters <= 0)
                throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds), "all execution bounds must be positive");
            var copy = new string[arguments.Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = arguments[i] ?? throw new ArgumentException("null process argument", nameof(arguments));
            return RunAsync(copy, timeoutMilliseconds, terminationTimeoutMilliseconds, outputCharacters)
                .GetAwaiter().GetResult();
        }

        private static async Task<BoundedCaseResult> RunAsync(string[] arguments, int timeoutMilliseconds,
            int terminationTimeoutMilliseconds, int outputCharacters)
        {
            var elapsed = Stopwatch.StartNew(); var output = new Tail(outputCharacters); var error = new Tail(outputCharacters);
            string entry = Assembly.GetEntryAssembly()?.Location;
            string executable = Environment.ProcessPath;
            if (string.IsNullOrEmpty(entry) || string.IsNullOrEmpty(executable))
                return new BoundedCaseResult(BoundedCaseStatus.StartFailed, null, null, elapsed.ElapsedMilliseconds,
                    "", "", false, false, "current executable/entry assembly unavailable");
            var start = new ProcessStartInfo(executable)
            {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
            };
            if (string.Equals(Path.GetFileNameWithoutExtension(executable), "dotnet", StringComparison.OrdinalIgnoreCase))
                start.ArgumentList.Add(entry);
            foreach (string argument in arguments) start.ArgumentList.Add(argument);
            using var process = new Process { StartInfo = start };
            int? id = null, exitCode = null; string failure = null;
            BoundedCaseStatus status = BoundedCaseStatus.StartFailed;
            using var streams = new CancellationTokenSource();
            using var deadline = new CancellationTokenSource();
            Task stdout = Task.CompletedTask, stderr = Task.CompletedTask;
            try
            {
                if (!process.Start()) throw new InvalidOperationException("process did not start");
                id = process.Id;
                stdout = Pump(process.StandardOutput, output, streams.Token);
                stderr = Pump(process.StandardError, error, streams.Token);
                Task exited = process.WaitForExitAsync();
                // This deadline is wall-time only. Output cannot renew it.
                int remaining = Math.Max(1, timeoutMilliseconds - checked((int)Math.Min(elapsed.ElapsedMilliseconds, int.MaxValue)));
                if (await Task.WhenAny(exited, Task.Delay(remaining, deadline.Token)).ConfigureAwait(false) == exited)
                {
                    await exited.ConfigureAwait(false); exitCode = process.ExitCode; status = BoundedCaseStatus.Exited;
                }
                else
                {
                    status = BoundedCaseStatus.TimedOut;
                    try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
                    catch (Exception problem) { failure = "termination request: " + problem.GetType().Name + ": " + problem.Message; }
                    if (await Task.WhenAny(exited, Task.Delay(terminationTimeoutMilliseconds)).ConfigureAwait(false) == exited)
                    { await exited.ConfigureAwait(false); exitCode = process.ExitCode; }
                    else { status = BoundedCaseStatus.TerminationFailed; failure = "own child termination not observed within grace"; }
                }
                deadline.Cancel();
                var drained = Task.WhenAll(stdout, stderr);
                if (await Task.WhenAny(drained, Task.Delay(terminationTimeoutMilliseconds)).ConfigureAwait(false) != drained)
                { failure = (failure == null ? "" : failure + "; ") + "output drain exceeded grace"; }
                else await drained.ConfigureAwait(false);
            }
            catch (Exception problem)
            {
                failure = problem.GetType().Name + ": " + problem.Message;
                if (id.HasValue)
                {
                    status = BoundedCaseStatus.TerminationFailed;
                    try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
                    catch (Exception termination) { failure += "; termination: " + termination.Message; }
                    try
                    {
                        Task exited = process.WaitForExitAsync();
                        if (await Task.WhenAny(exited, Task.Delay(terminationTimeoutMilliseconds)).ConfigureAwait(false) == exited)
                        { await exited.ConfigureAwait(false); exitCode = process.ExitCode; }
                    }
                    catch (Exception observation) { failure += "; exit observation: " + observation.Message; }
                }
            }
            finally
            {
                deadline.Cancel(); streams.Cancel();
                // Cancel/close only this process's readers; never wait indefinitely for inherited pipes.
                if (id.HasValue) { process.StandardOutput.Dispose(); process.StandardError.Dispose(); }
            }
            return new BoundedCaseResult(status, id, exitCode, elapsed.ElapsedMilliseconds,
                output.Read(), error.Read(), output.Truncated, error.Truncated, failure);
        }

        private static async Task Pump(StreamReader reader, Tail tail, CancellationToken cancellation)
        {
            var buffer = new char[1024];
            try
            {
                int count;
                while ((count = await reader.ReadAsync(buffer.AsMemory(), cancellation).ConfigureAwait(false)) != 0)
                    tail.Append(buffer, count);
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (ObjectDisposedException) when (cancellation.IsCancellationRequested) { }
        }

        private sealed class Tail
        {
            private readonly int _limit;
            private readonly StringBuilder _text = new StringBuilder();
            private readonly object _gate = new object();
            private bool _truncated;
            public Tail(int limit) { _limit = limit; }
            public bool Truncated { get { lock (_gate) return _truncated; } }
            public void Append(char[] buffer, int count)
            {
                lock (_gate)
                {
                    _text.Append(buffer, 0, count);
                    if (_text.Length > _limit) { _text.Remove(0, _text.Length - _limit); _truncated = true; }
                }
            }
            public string Read() { lock (_gate) return _text.ToString(); }
        }
    }
}

