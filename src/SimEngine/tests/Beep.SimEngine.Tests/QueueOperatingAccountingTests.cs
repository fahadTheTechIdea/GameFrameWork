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

namespace Beep.SimEngine.Tests
{
    // JSON and ordinary player requests/views/Update/save/replay only. Bad callbacks belong to the mod.
    internal static class QueueOperatingAccountingTests
    {
        internal const string Case = "Queue operating accounting: append tail and physical return preserve earned consumption";
        private static readonly Code Capability = new Code("pulse");
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(string fault, bool half)
        {
            var document = Json.Parse(FrameworkProofContent.Read("transformer-proof.json"));
            document.Get("entities")[0].Get("components").Get("pulse_transformer").Set("queue_fault", JsonValue.Of(fault));
            document.Get("transformations")[0].Get("duration").Set("numerator", JsonValue.Number("12"));
            return TransformerTests.Factory(root => { if (half) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30")); },
                transformerDocument: document.ToString());
        }
        private static SimGame New(GameFactory f) => f.NewGame(new NewGameRequest
        { Scenario = "process", Seed = 551, Seats = { Controller.Human, Controller.Human } });
        private static EntityId Actor(SimGame g) => g.Seat(0).Snapshot.Entities.Single(x => x.Def == "emitter").Id;
        private static TransformationManagementView View(SimGame g) => g.Seat(0).View.Feature<TransformationManagementView>();
        private static TransformationSnapshot Read(SimGame g)
        {
            if (!View(g).TryGetTransformation(Actor(g), Capability, out var facts)) throw new InvalidOperationException("missing actual exposure facts");
            return facts;
        }
        private static void Step(SimGame g, SimGame twin = null)
        {
            g.Update(); twin?.Update();
            if (g.Fault != null || twin?.Fault != null || twin != null && g.Hash() != twin.Hash())
                throw new InvalidOperationException("operating accounting clock=" + g.Clock.StepIndex + " " + g.Fault + "/" + twin?.Fault);
        }
        private static void Input(Check c, SimGame g, Command command)
        {
            ulong hash = g.Hash(); c.Equal(Code.None, g.Seat(0).Preview(command), "original player evaluator");
            c.Equal(hash, g.Hash(), "queue intent preview changes no time costs or state");
            int sequence = g.Seat(0).Submit(command); Step(g); long cursor = 0; var results = new List<CommandResult>();
            g.Seat(0).ReadResults(ref cursor, results); c.That(results.Single(x => x.Sequence == sequence).Accepted, "ordinary player request applies in Update");
        }
        private static EnqueueTransformationCommand Enqueue(SimGame g, int count)
            => new EnqueueTransformationCommand { Transformer = Actor(g), Capability = Capability, Transformation = "pulse", Count = count };
        private static CancelTransformationCommand Cancel(SimGame g, int index)
            => new CancelTransformationCommand { Transformer = Actor(g), Capability = Capability, Index = index };
        private static ConfigureOperationCommand Control(SimGame g, bool enabled, Fix target)
            => new ConfigureOperationCommand { Entity = Actor(g), Operation = Capability, Enabled = enabled, Target = target };
        private static void Ready(Check c, SimGame g)
        {
            Input(c, g, Enqueue(g, 3)); Step(g); Step(g);
            var running = Read(g); c.That(running.Progress.WholeTicks == 3 && running.Operating.ElapsedTicks == 3,
                "actual independent processor earns three productive updates through sole game clock");
            Input(c, g, Control(g, false, Fix.One));
            c.That(Read(g).Progress.Equals(running.Progress) && Read(g).Operating.ElapsedTicks == 3,
                "disabled work preserves original productive exposure");
        }
        private static void Healthy(Check c, bool half)
        {
            var f = Factory("none", half); var g = New(f); var idle = New(f); ulong untouched = idle.Hash();
            var limits = new ReplayLimits(4 * 1024 * 1024, 3000); using var recording = g.RecordReplay(limits);
            Ready(c, g); var original = Read(g); var twin = f.Load(g.Save()); Step(g, twin);
            c.Equal(3L, Read(g).Operating.ElapsedTicks, "saved disabled exposure cannot grow");
            Input(c, g, Enqueue(g, 1)); var appended = Read(g);
            c.That(appended.QueueCount == 4 && appended.Progress.Equals(original.Progress)
                && appended.Operating.ElapsedTicks == original.Operating.ElapsedTicks && appended.Operating.Consumed.IsEmpty,
                "append changes intent only, no earned operating time or invented payment");
            Input(c, g, Cancel(g, 1)); var tail = Read(g);
            c.That(tail.QueueCount == 3 && tail.Queue[0].Execution == original.Queue[0].Execution
                && tail.Progress.Equals(original.Progress) && tail.Operating.ElapsedTicks == original.Operating.ElapsedTicks,
                "tail cancellation preserves actual head and earned costs");
            c.That(!g.Seat(1).View.Feature<TransformationManagementView>().TryGetTransformation(Actor(g), Capability, out _), "actual operating facts remain company private");
            twin = f.Load(g.Save()); g.Clock.Pause(); twin.Clock.Pause(); long at = g.Clock.StepIndex; ulong paused = g.Hash(); Step(g, twin);
            c.That(g.Clock.StepIndex == at && g.Hash() == paused && Read(g).Operating.ElapsedTicks == 3, "one paused clock changes no queue or productive costs");
            g.Clock.Resume(); twin.Clock.Resume();
            Input(c, g, Cancel(g, 0)); c.That(Read(g).Operating.ElapsedTicks == 0 && Read(g).Progress.Equals(WorkProgress.Zero), "unbuffered head retirement clears only its completed exposure");
            Input(c, g, Cancel(g, 1)); Input(c, g, Control(g, true, Fix.FromRatio(3, 2)));
            var next = Read(g); c.That(next.Operating.ElapsedTicks == 1 && next.Progress.Equals(new WorkProgress(1, 1u << 31)),
                "new head has one elapsed productive update at rate1.5, not a second clock or copied progress time");
            twin = f.Load(g.Save());
            for (int i = 0; i < 128 && Read(g).QueueCount > 0; i++) Step(g, twin);
            c.That(Read(g).QueueCount == 0 && Read(g).Operating.ElapsedTicks == 0 && g.Seat(0).Snapshot.Entities.Count(x => x.Def == "token") == 1,
                "only surviving physically timed request produces one result and retires its exposure");
            c.That(original.QueueCount == 3 && original.Operating.ElapsedTicks == 3 && original.Progress.WholeTicks == 3, "retained costs/queue/progress remain immutable");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "terminal exposure checkpoint identical"); c.Equal(untouched, idle.Hash(), "mod sessions isolated");
            c.That(new ReplayVerifier().Verify(f, recording.Finish(), limits).Success, "normal enqueue/cancel/control/clock inputs replay exposure once");
        }
        private static void Fault(Check c, string mode, bool half)
        {
            var f = Factory(mode, half); var g = New(f); var idle = New(f); ulong untouched = idle.Hash(); Ready(c, g);
            var retained = Read(g); var publication = g.Seat(0).Snapshot;
            Command command = mode == "append-exposure" ? Enqueue(g, 1) : Cancel(g, 1);
            ulong hash = g.Hash(); c.Equal(Code.None, g.Seat(0).Preview(command), "fault is an execution callback, not admission refusal");
            c.Equal(hash, g.Hash(), "negative pending intent still has pure preview"); g.Seat(0).Submit(command); var twin = f.Load(g.Save());
            g.Update(); twin.Update(); string phase = mode == "append-exposure" ? "transformations.enqueue" : "transformations.cancel";
            c.That(g.Fault?.Phase == phase && g.Fault.Message.Contains("earned operating exposure or consumption"), "original queue coordinator detects otherwise valid changed exposure");
            c.That(twin.Fault?.Phase == phase && twin.Fault.Message == g.Fault?.Message, "saved pending faulty callback refuses identically");
            c.That(g.Seat(0).Snapshot.Entities.Count(x => x.Def == "token") == 0 && publication.Entities.Count(x => x.Def == "token") == 0,
                "no result or successful publication prefix after invalid queue mutation");
            var first = g.Fault; long at = g.Clock.StepIndex; g.Update(); c.That(ReferenceEquals(first, g.Fault) && at == g.Clock.StepIndex, "first fault stops all gameplay and time");
            c.Throws<InvalidOperationException>(() => g.Save(), "mutated costs cannot become a resumable save");
            c.That(retained.Operating.ElapsedTicks == 3 && retained.QueueCount == 3, "detached pre-fault costs remain original");
            c.Equal(untouched, idle.Hash(), "faulty exposure cannot poison unrelated session");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "actual strengthened queue coordinator build");
            foreach (bool half in new[] { false, true }) Healthy(c, half);
            foreach (string mode in new[] { "append-exposure", "cancel-exposure", "cancel-reduce-exposure" })
                foreach (bool half in new[] { false, true }) Fault(c, mode, half);
            ProductionCancellationTests.Physical(c);
            foreach (bool point in new[] { false, true }) foreach (bool half in new[] { false, true }) ReturnExecutionPreviewTests.Physical(c, point, half);
        }
    }
}
