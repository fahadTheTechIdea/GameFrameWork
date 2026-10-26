using System;
using System.Collections.Generic;
using System.Diagnostics;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    internal static partial class WorkerExecutionTests
    {
        // Primitive contract publication measurement. Gameplay controls remain normal engine commands;
        // no test-owned work, material, rate, clock or balance implementation.
        private static void Publication(Check c)
        {
            var factory = Factory(extension: true, condition: true);
            var game = New(factory, condition: true);
            var machine = Find(game, "machine");
            Func<IWorkerExecution, ExecutionGateSnapshot> read = provider => provider.ReadExecution(game.World).Gate;
            ExecutionGateEvaluation ReadActual() => ExecutionGateContracts.Read(game.World, machine, read);
            ulong before = game.Hash();
            var first = ReadActual();
            c.Equal(2, first.Gates.Count, "actual native and independent named gates share one evaluation");
            c.Equal("availability", first.Gates[0].Capability.Id, "publication preserves canonical capability order");
            c.Equal("job-gate", first.Gates[1].Capability.Id, "independent gate retains its actual named identity");
            c.Equal(Code.None, first.Blocker, "actual intact native condition opens aggregate");
            c.Equal(1, first.Gates[0].Conditions.Count, "actual signal condition remains complete");
            c.Throws<NotSupportedException>(() => ((IList<ExecutionGateSnapshot>)first.Gates).Clear(), "published gate collection stays read-only");
            c.Throws<NotSupportedException>(() => ((IList<ExecutionGateConditionSnapshot>)first.Gates[0].Conditions).Clear(), "published condition collection stays read-only");

            // A public constructor cannot adopt arbitrary caller-owned input.
            var input = new List<ExecutionGateConditionSnapshot>(first.Gates[0].Conditions);
            var copied = new ExecutionGateSnapshot(first.Gates[0].Capability, first.Gates[0].Applies, first.Gates[0].Blocker, input);
            input.Clear();
            c.Equal(1, copied.Conditions.Count, "public snapshots still defensively copy external input");
            c.Equal(before, game.Hash(), "actual gate publication and external input mutation preserve world state");

            var source = Find(game, "source");
            var empty = ExecutionGateContracts.Read(game.World, source, read);
            c.Equal(0, empty.Gates.Count, "entity without the selected capability retains empty readiness");
            c.Equal(Code.None, empty.Blocker, "no provider is not an invented refusal");
            c.Throws<NotSupportedException>(() => ((IList<ExecutionGateSnapshot>)empty.Gates).Clear(), "empty publication is immutable too");

            var warming = Stopwatch.StartNew();
            do { _ = ReadActual(); } while (warming.ElapsedMilliseconds < 300);
            const int count = 2048;
            long allocated = GC.GetAllocatedBytesForCurrentThread(), start = Stopwatch.GetTimestamp();
            int seen = 0;
            for (int i = 0; i < count; i++) seen += ReadActual().Gates.Count;
            long bytes = GC.GetAllocatedBytesForCurrentThread() - allocated, ticks = Stopwatch.GetTimestamp() - start;
            c.Equal(count * 2, seen, "every measured read traverses both actual providers");
            c.Equal(before, game.Hash(), "repeated actual observations never mutate their world");
            Console.WriteLine(FormattableString.Invariant($"execution gate publication;build={EngineInfo.Build};reads={count};providers=2;elapsed-ms={(double)ticks * 1000 / Stopwatch.Frequency:F3};allocated={bytes};hash={before:X16}"));

            Control(c, game, false, Fix.One);
            var stopped = ReadActual();
            c.Equal(OperationCodes.Disabled, stopped.Blocker, "ordinary control is reflected by a fresh evaluation");
            c.Equal(Code.None, first.Blocker, "retained earlier aggregate is detached after actual control");
            c.Equal(Code.None, first.Gates[0].Blocker, "retained earlier gate stays detached after actual control");
            c.That(!ReferenceEquals(first.Gates, stopped.Gates), "nonempty evaluations never share a mutable publication buffer");
            var twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "published facts do not create saved state");
            Control(c, game, true, Fix.One); Control(c, twin, true, Fix.One);
            c.Equal(Code.None, ReadActual().Blocker, "ordinary re-enable reads actual current state");
            c.Equal(OperationCodes.Disabled, stopped.Blocker, "retained stopped evaluation does not change on re-enable");
            c.Equal(game.Hash(), twin.Hash(), "saved twin follows the same original operation commands");
            c.That(game.Fault == null && twin.Fault == null, "healthy publication keeps both sessions healthy");
        }
    }
}
