using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static partial class OilCanonicalManufacturingTests
    {
        private const string NetworkCase = "Oil canonical manufacturing: shuffled lockstep physical truck saved continuation and canonical replay";
        private static void Exchange(SimGame from, SimGame to)
        {
            var packets = new List<byte[]>(); from.Net.DrainOutgoing(packets); packets.Reverse();
            foreach (var packet in packets) to.Net.Receive(packet);
        }
        private static void Network(Check c)
        {
            var factory = Factory(out var oil);
            var a = factory.NewGame(new NewGameRequest { Scenario = "frontier", Seed = 17, Seats = { Controller.Human, Controller.Human } });
            var hq = Found(c, a, oil); var b = factory.Load(a.Save());
            var isolated = factory.Load(a.Save()); var isolatedHash = isolated.Hash();
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current actual engine identity");
            c.Equal(a.Hash(), b.Hash(), "actual domain peers start from identical admitted state");
            var initial = new Dictionary<string, Fix>();
            foreach (string resource in new[] { "structural_steel", "machinery", "electrical_parts" }) initial.Add(resource, Stock(a, hq, resource));
            int trucks = Trucks(a).Count; c.Equal(2, trucks, "original authored initial haul trucks");
            var limits = new ReplayLimits(16 * 1024 * 1024, 20000);
            ulong replayInitialHash = a.Hash();
            using var left = a.RecordReplay(limits); using var right = b.RecordReplay(limits);
            a.StartLockstep(new LockstepOptions { LocalSeats = { 0 }, InputDelay = 2, ChecksumEvery = 60 });
            b.StartLockstep(new LockstepOptions { LocalSeats = { 1 }, InputDelay = 2, ChecksumEvery = 60 });
            c.Equal(Code.None, a.Net.CheckHello(b.Net.Hello()), "actual canonical peers accept protocol/modules/content");
            int rounds = 0;
            void Round()
            {
                a.RunTicks(1); b.RunTicks(1); Exchange(a, b); Exchange(b, a); rounds++;
                if (a.Fault != null || b.Fault != null || a.Net.State == NetState.Broken || b.Net.State == NetState.Broken ||
                    a.Net.State == NetState.Desynced || b.Net.State == NetState.Desynced)
                    throw new InvalidOperationException("actual Oil network stopped: " + Diagnostic(a, hq) + "/" + Diagnostic(b, hq) + ";" + a.Fault + "/" + b.Fault);
            }
            void Until(Func<bool> ready, string stage, int maximumRounds = 5000)
            {
                var watch = Stopwatch.StartNew(); int before = rounds;
                while (!ready() && rounds - before < maximumRounds && watch.ElapsedMilliseconds < 60000) Round();
                if (!ready()) throw new InvalidOperationException("bounded actual Oil network " + stage + ";elapsed-ms=" + watch.ElapsedMilliseconds + ";" + Diagnostic(a, hq) + "/" + Diagnostic(b, hq));
                Console.WriteLine("manufacturing-net-stage " + stage + ";rounds=" + (rounds - before) + ";elapsed-ms=" + watch.ElapsedMilliseconds + ";" + Diagnostic(a, hq));
            }
            void Aligned(string stage)
            {
                for (int i = 0; a.Tick != b.Tick && i < 20; i++)
                { if (a.Tick < b.Tick) a.RunTicks(1); else b.RunTicks(1); Exchange(a, b); Exchange(b, a); }
                c.Equal(a.Tick, b.Tick, stage + " peer times align"); c.Equal(a.Hash(), b.Hash(), stage + " complete peer state agrees");
            }
            var queue = new EnqueueTransformationCommand { Transformer = hq, Capability = Production, Transformation = "build_haul_truck", Count = 2 };
            c.Equal(Code.None, a.Seat(0).Preview(queue), "ordinary actual manufacturing preview"); a.Seat(0).Submit(queue);
            var foreign = new ConfigureOperationCommand { Entity = hq, Operation = Production, Enabled = false, Target = Fix.One };
            c.Equal(Codes.NotFound, b.Seat(1).Preview(foreign), "foreign operation preview withholds private company"); b.Seat(1).Submit(foreign);
            Until(() => State(a, hq).QueueCount == 2 && State(b, hq).QueueCount == 2 && State(a, hq).Progress.WholeTicks >= 120, "actual two-hour work prefix");
            Aligned("working queue"); var active = State(a, hq); var head = active.Queue[0].Execution;
            c.Equal(2160L, active.Required, "canonical36h duration, no replacement recipe");
            c.Equal(trucks, Trucks(a).Count, "two hours produce no premature truck");
            var cancel = new CancelTransformationCommand { Transformer = hq, Capability = Production, Index = 1 };
            c.Equal(Code.None, a.Seat(0).Preview(cancel), "ordinary tail cancellation preview"); a.Seat(0).Submit(cancel);
            a.Seat(0).Submit(new ConfigureOperationCommand { Entity = hq, Operation = Production, Enabled = false, Target = Fix.One });
            Until(() => State(a, hq).QueueCount == 1 && State(b, hq).QueueCount == 1 && State(a, hq).Blocker == OperationCodes.Disabled && State(b, hq).Blocker == OperationCodes.Disabled, "actual cancellation and shutdown");
            Aligned("disabled active head"); var stopped = State(a, hq);
            c.Equal(head, stopped.Queue[0].Execution, "tail cancellation preserves actual head execution identity");
            c.Equal(stopped.Progress, State(b, hq).Progress, "same actual earned productive time");
            var disabled = factory.Load(a.Save()); c.Equal(a.Hash(), disabled.Hash(), "disabled partial production save preserves admitted state");
            for (int i = 0; i < 8; i++) Round(); Aligned("disabled wait");
            c.Equal(stopped.Progress, State(a, hq).Progress, "disabled network time earns no work");
            c.Equal(trucks, Trucks(a).Count, "disabled network time creates no result");
            a.Seat(0).Submit(new ConfigureOperationCommand { Entity = hq, Operation = Production, Enabled = true, Target = Fix.One });
            Until(() => State(a, hq).Progress.WholeTicks > stopped.Progress.WholeTicks && State(b, hq).Progress.WholeTicks > stopped.Progress.WholeTicks, "ordinary restart");
            Aligned("restarted saved work"); var saved = factory.Load(a.Save()); var checkpoint = State(a, hq);
            c.Equal(a.Hash(), saved.Hash(), "active actual WIP/queue/source/budget checkpoint");
            c.Equal(Codes.NotFound, b.Seat(1).Preview(foreign), "private refusal remains identical after saved continuation");
            Until(() => State(a, hq).Progress.WholeTicks == 2159 && State(b, hq).Progress.WholeTicks == 2159, "last incomplete canonical minute");
            Aligned("last incomplete minute"); c.Equal(trucks, Trucks(a).Count, "neither peer fabricates early physical output");
            Until(() => Trucks(a).Count == trucks + 1 && Trucks(b).Count == trucks + 1 && State(a, hq).QueueCount == 0 && State(b, hq).QueueCount == 0, "physical canonical truck completion", 100);
            Aligned("complete result");
            var output = Trucks(a).Last(); var footprint = ((TileSpatialSnapshot)output.Spatial).Footprint;
            c.That(Trucks(a).Take(trucks).All(e => !((TileSpatialSnapshot)e.Spatial).Footprint.Overlaps(footprint)), "actual network-created truck has non-overlapping rollout");
            c.That(State(a, hq).Wip.Stock.IsEmpty && State(b, hq).Wip.Stock.IsEmpty, "both original WIP stores retire");
            foreach (var material in new[] { ("structural_steel", 10), ("machinery", 3), ("electrical_parts", 2) })
                c.Equal(initial[material.Item1] - Fix.FromInt(material.Item2), Stock(a, hq, material.Item1), "one actual truck, cancelled tail charges no physical input " + material.Item1);
            Advance(saved, hq, a.Tick - saved.Tick, "saved independent remaining canonical work");
            c.That(checkpoint.Progress.WholeTicks > 0 && checkpoint.Progress.WholeTicks < 2160, "saved comparison begins during actual partial work");
            c.Equal(a.Hash(), saved.Hash(), "independent saved physical continuation equals completed network peers");
            byte[] l = left.Finish(), r = right.Finish();
            c.Equal(5, left.CommandCount, "only actual queue, foreign refusal, tail cancel, stop and restart admissions are recorded");
            c.Equal(left.CommandCount, right.CommandCount, "both peers record identical logical admissions");
            c.That(l.SequenceEqual(r), "reversed packet arrival yields the same canonical actual command replay");
            var description = ReplayVerifier.Describe(l, limits);
            c.That(description.InitialStep == left.InitialStep && description.InitialHash == replayInitialHash,
                "actual manufacture replay starts at the same admitted company before network production");
            c.That(description.FinalStep == a.Clock.StepIndex && description.FinalHash == a.Hash()
                && description.Commands == left.CommandCount && description.Checkpoints == left.CheckpointCount,
                "actual manufacture replay covers every command through the completed live truck");
            c.Equal(a.Clock.StepIndex - left.InitialStep, (long)left.CheckpointCount,
                "actual manufacture records every earned game minute, not transport stalls");
            c.Equal(left.CheckpointCount, right.CheckpointCount, "shuffled peers record the same complete update stream");
            var verification = new ReplayVerifier().Verify(factory, l, limits);
            c.That(verification.Success, "canonical actual manufacture replays: " + verification.Failure + "/" + verification.Detail);
            c.Equal(a.Clock.StepIndex, verification.Step, "verification reaches the actual completed live minute");
            c.That(verification.ExpectedHash == a.Hash() && verification.ActualHash == a.Hash(),
                "verification reproduces the completed live state, not merely a self-consistent prefix");
            c.That(verification.Game != null && verification.Game.Hash() == a.Hash(), "verified actual game retains the complete final state");
            Console.WriteLine("actual manufacturing replay: initial=" + description.InitialStep + ";final=" + description.FinalStep
                + ";commands=" + description.Commands + ";checkpoints=" + description.Checkpoints
                + ";live-hash=" + a.Hash().ToString("X16") + ";verified-hash=" + verification.ActualHash.ToString("X16"));
            c.Equal(isolatedHash, isolated.Hash(), "network production never advances or supplies an unrelated session");
        }
    }
}
