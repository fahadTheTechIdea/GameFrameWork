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
    public static class TransformationCancellationTests
    {
        private static readonly PlayerId Company = new PlayerId(0);
        private static readonly Code Pulse = new Code("pulse");
        internal const string NoOpCase = "Transformation cancellation boundary: independent no-op commit cannot publish successful cancellation";
        internal const string TailCase = "Transformation cancellation boundary: independent tail commit cannot erase earned head work";
        internal const string IndependentCase = "Transformation cancellation contract: independent saved edits and replay under minute and half-minute clocks";
        internal const string CommitCase = "Transformation cancellation boundary: entry control and interrupted commit violations cannot report success";
        internal const string AdmissionCase = "Transformation cancellation boundary: admission mutation faults before preview success";
        internal const string SiblingCase = "Transformation cancellation boundary: sibling operation mutation cannot report success";
        internal const string DirectCase = "Transformation cancellation boundary: direct coordinator preserves interrupted commit fault";
        public static void Register()
        {
            TestRunner.Add(NoOpCase, c => Violation(c, TransformerTests.Factory(), 17));
            TestRunner.Add(AdmissionCase, AdmissionFault);
            TestRunner.Add(SiblingCase, SiblingFault);
            TestRunner.Add(DirectCase, DirectFault);
            TestRunner.Add(TailCase, c => {
                foreach (bool half in new[] { false, true }) Violation(c, CancellationFactory(half), 23);
            });
            TestRunner.Add(IndependentCase, c => { Independent(c); Independent(c, true); });
            TestRunner.Add(CommitCase, c => {
                foreach (int mode in new[] { 18, 19, 20, 21 }) Violation(c, TransformerTests.Factory(), mode);
            });
        }
        // Explicit pre-game clock authoring; original module/provider/queue and two-minute work remain.
        private static GameFactory CancellationFactory(bool half)
            => TransformerTests.Factory(root => {
                if (half) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
            });
        private static Entity Find(SimGame game, string id) => TransformerTests.Find(game, id);
        private static TransformationManagementView View(SimGame game, int seat = 0)
            => game.Seat(seat).View.Feature<TransformationManagementView>();
        private static TransformationSnapshot Read(SimGame game, Entity entity, Code capability)
        {
            if (!View(game).TryGetTransformation(entity.Id, capability, out var result)) throw new InvalidOperationException("missing actual queue facts");
            return result;
        }
        private static void Step(SimGame game)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("cancellation Update: " + game.Fault);
        }
        private static void Stop(Check c, SimGame game, Entity entity, Code capability)
            => c.That(TestKit.Order(game, 0, new ConfigureOperationCommand { Entity = entity.Id, Operation = capability,
                Enabled = false, Target = Fix.One }).Accepted, "ordinary operation control stops manufacturing");
        private static CommandResult Cancel(SimGame game, Entity entity, Code capability, int index, int seat = 0)
            => TestKit.Order(game, seat, new CancelTransformationCommand { Transformer = entity.Id, Capability = capability, Index = index });
        private static void Ready(Check c, SimGame game, Entity entity, Code capability, string transformation)
        {
            c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Transformer = entity.Id, Capability = capability,
                Transformation = transformation, Count = 3 }).Accepted, "ordinary queue admission creates three real requests");
            c.That(Read(game, entity, capability).Progress.WholeTicks > 0, "active head earns actual game time before cancellation");
            Stop(c, game, entity, capability);
        }
        private static void Cutoff(Check c, SimGame game, string reason)
        {
            c.That(game.Fault != null, reason + " latches actual fault"); long time = game.Tick;
            game.Update(); c.Equal(time, game.Tick, reason + " cannot advance clock");
            c.Throws<InvalidOperationException>(() => game.Save(), reason + " cannot save partial commit");
            c.Throws<InvalidOperationException>(() => game.Hash(), reason + " cannot checkpoint hash");
        }
        private static void Independent(Check c, bool half = false)
        {
            var factory = CancellationFactory(half); var game = TransformerTests.New(factory); var entity = Find(game, "emitter");
            var limits = new ReplayLimits(4 * 1024 * 1024, 1000); using var recording = game.RecordReplay(limits);
            c.That(entity.Get<Transformer>() == null, "independent provider is not native transformer");
            Ready(c, game, entity, Pulse, "pulse"); var before = Read(game, entity, Pulse);
            ulong pure = game.Hash();
            c.Equal(Code.None, View(game).PreviewCancel(entity.Id, Pulse, 1), "ordinary independent tail preview accepts");
            c.Equal(Codes.InvalidArgument, View(game).PreviewCancel(entity.Id, Pulse, 3), "missing independent index has typed refusal");
            c.Equal(Codes.NotFound, View(game, 1).PreviewCancel(entity.Id, Pulse, 1), "another company cannot cancel private queue");
            c.That(!View(game, 1).TryGetTransformation(entity.Id, Pulse, out _), "another company receives no private head progress");
            c.Equal(pure, game.Hash(), "valid invalid and private cancellation previews are pure");
            c.That(Cancel(game, entity, Pulse, 1).Accepted, "independent provider tail uses same contract");
            var tail = Read(game, entity, Pulse);
            c.That(tail.QueueCount == 2 && tail.Progress.Equals(before.Progress) && tail.Phase == before.Phase && tail.Blocker == before.Blocker,
                "independent tail preserves actual head work");
            c.That(tail.Queue[0].Execution == before.Queue[0].Execution && tail.Queue[1].Execution == before.Queue[2].Execution,
                "only selected tail identity retires, not head or surviving request");
            c.That(before.QueueCount == 3 && before.Progress.Equals(tail.Progress), "retained pre-cancel snapshot stays detached");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "tail-edited active progress survives save/load");
            long time = game.Clock.StepIndex; pure = game.Hash(); game.Clock.Pause(); twin.Clock.Pause(); Step(game); Step(twin);
            c.That(game.Clock.StepIndex == time && game.Hash() == pure && twin.Hash() == pure,
                "sole paused clock preserves saved tail-edited queue and earned progress");
            game.Clock.Resume(); twin.Clock.Resume();
            c.That(Cancel(game, entity, Pulse, 0).Accepted, "head without physical WIP removes exactly one entry");
            var head = Read(game, entity, Pulse);
            c.That(head.QueueCount == 1 && head.Progress.Equals(WorkProgress.Zero) && head.Phase == TransformationPhase.Idle,
                "independent remaining request has no prior-head work");
            c.Equal(before.Queue[2].Execution, head.Queue[0].Execution, "head cancellation preserves sole surviving request identity");
            twin = factory.Load(game.Save()); Step(game); Step(twin);
            c.Equal(game.Hash(), twin.Hash(), "independent cancelled queue/control state persists");
            c.Equal(WorkProgress.Zero, Read(game, entity, Pulse).Progress, "disabled survivor cannot inherit cancelled work");
            c.That(Cancel(game, entity, Pulse, 0).Accepted && Read(game, entity, Pulse).QueueCount == 0, "final independent request retires");
            c.Equal(Codes.InvalidArgument, Cancel(game, entity, Pulse, 0).Refusal, "duplicate final cancellation is refused normally");
            c.Equal(0, game.Seat(0).Snapshot.Entities.Count(row => row.Def == "token"), "cancelled independent queue creates no result");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "fully cancelled independent queue is durably empty");
            c.Equal(6, recording.CommandCount, "enqueue stop three accepted cancellations and duplicate refusal are all canonical inputs");
            var verified = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
            c.That(verified.Success, "ordinary independent cancellation and clock controls replay");
            c.Equal(game.Hash(), verified.ActualHash, "independent cancellation replay reaches exact final hash");
            c.That(game.Fault == null, "valid mod cancellation remains healthy");
        }
        // Negative protocol probes only; ordinary material pressure belongs to the real-truck flow.
        private static void AdmissionFault(Check c)
        {
            var factory = TransformerTests.Factory(); var game = TransformerTests.New(factory); var entity = Find(game, "emitter");
            Ready(c, game, entity, Pulse, "pulse"); entity.Get<PulseTransformer>().FaultMode = 16;
            c.Throws<InvalidOperationException>(() => View(game).PreviewCancel(entity.Id, Pulse, 0), "hidden persisted cancel-check mutation refuses");
            var first = game.Fault; Cutoff(c, game, "hidden cancellation admission");
            c.That(first != null && ReferenceEquals(first, game.Fault), "pure admission mutation preserves original first fault");
        }
        private static void SiblingFault(Check c)
        {
            var factory = TransformerTests.Factory(root => root.Get("entities")[0].Get("components").Set("pulse_transformer",
                Json.Parse(FrameworkProofContent.Read("transformer-proof.json")).Get("entities")[0].Get("components").Get("pulse_transformer")));
            var game = TransformerTests.New(factory); var entity = Find(game, "assembler");
            c.That(TestKit.Order(game, 0, new EnqueueTransformationCommand { Transformer = entity.Id, Capability = Pulse, Transformation = "pulse", Count = 3 }).Accepted,
                "real sibling fixture begins valid independent queue");
            entity.Get<PulseTransformer>().FaultMode = 22;
            int sequence = game.Seat(0).Submit(new CancelTransformationCommand { Transformer = entity.Id, Capability = Pulse, Index = 1 });
            game.Update(); var first = game.Fault; Cutoff(c, game, "actual sibling operation changed by cancellation");
            NoAcceptedAnswer(c, game, sequence);
            c.That(first != null && ReferenceEquals(first, game.Fault), "sibling mutation retains first cancellation fault");
        }
        private static void DirectFault(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "strict cancellation coordinator identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "actual queue and return codecs");
            var factory = TransformerTests.Factory(); var game = TransformerTests.New(factory); var entity = Find(game, "emitter");
            c.That(ReferenceEquals(game.World.Services.Get<ITransformationQueue>(), game.World.Services.Get<TransformerManager>()),
                "same actual queue coordinator owns direct and command cancellation");
            c.That(game.World.Space == null && !game.World.Services.Has<IEconomy>(), "map and money remain optional in original contract fixture");
            Ready(c, game, entity, Pulse, "pulse"); entity.Get<PulseTransformer>().FaultMode = 21;
            c.Throws<InvalidOperationException>(() => game.World.Services.Get<ITransformationQueue>().Cancel(Company, entity.Id, Pulse, 0),
                "direct coordinator interrupted commit throws");
            var first = game.Fault; Cutoff(c, game, "direct cancellation");
            c.That(first != null && first.Phase == "transformations.cancel" && first.Message.Contains("negative interrupted committed cancellation") &&
                ReferenceEquals(first, game.Fault), "direct coordinator preserves original interrupted commit cause");
            var healthy = TransformerTests.New(factory); long time = healthy.Clock.StepIndex; Step(healthy);
            c.That(healthy.Fault == null && healthy.Clock.StepIndex == time + 1, "faulted mod session cannot stop another session");
            c.Equal(healthy.Hash(), factory.Load(healthy.Save()).Hash(), "other session remains durably healthy after direct provider fault");
        }
        private static void NoAcceptedAnswer(Check c, SimGame game, int sequence)
        {
            long cursor = 0; var answers = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, answers);
            c.That(!answers.Any(answer => answer.Stream == CommandStreams.Player && answer.Sequence == sequence && answer.Accepted),
                "faulting cancellation cannot publish an accepted command answer");
        }
        // Explicit white-box provider protocol probe, never the normal physical player driver.
        private static void Violation(Check c, GameFactory factory, int mode)
        {
            var game = TransformerTests.New(factory); var entity = Find(game, "emitter"); Ready(c, game, entity, Pulse, "pulse");
            var management = mode == 23 ? View(game) : null;
            var idle = mode == 23 ? TransformerTests.New(factory) : null;
            ulong idleHash = idle?.Hash() ?? 0;
            if (mode == 23) {
                var ready = Read(game, entity, Pulse);
                c.That(ready.QueueCount == 3 && ready.Progress.WholeTicks > 0,
                    "tail corruption probe begins with real earned head and two pending requests");
            }
            entity.Get<PulseTransformer>().FaultMode = mode;
            int sequence = game.Seat(0).Submit(new CancelTransformationCommand { Transformer = entity.Id, Capability = Pulse, Index = mode == 23 ? 1 : 0 });
            game.Update(); var first = game.Fault; Cutoff(c, game, "malformed cancellation mode" + mode);
            NoAcceptedAnswer(c, game, sequence);
            string expectedCause = mode == 18 ? "cancellation did not retain its returning head or remove exactly its selected entry"
                : mode == 19 ? "cancellation changed an unrelated queue entry"
                : mode == 20 ? "transformer changed controls or sibling components while cancelling its queue"
                : mode == 21 ? "negative interrupted committed cancellation" : null;
            if (expectedCause != null)
                c.That(first != null && first.Phase == "transformations.cancel" && first.Message.Contains(expectedCause),
                    "original cancellation owner preserves the specific entry/control/interrupted-commit cause " + mode);
            if (mode == 23) {
                c.That(first != null && first.Phase == "transformations.cancel" && first.Message.Contains("tail cancellation changed active head execution"),
                    "original queue owner detects actual earned-head mutation, not an unrelated provider error");
                c.Throws<InvalidOperationException>(() => management.PreviewCancel(entity.Id, Pulse, 0), "retained cancellation preview refuses faulted session");
                c.Throws<InvalidOperationException>(() => management.PreviewCancel(EntityId.None, Pulse, 0), "missing target cannot mask original cancellation fault");
                c.Throws<InvalidOperationException>(() => management.PreviewCancel(entity.Id, Code.None, 0), "missing capability cannot mask original cancellation fault");
                c.Throws<InvalidOperationException>(() => management.PreviewCancel(entity.Id, Pulse, -1), "invalid index cannot mask original cancellation fault");
                c.Equal(idleHash, idle.Hash(), "provider fault leaves independent session untouched");
                long idleStep = idle.Clock.StepIndex; Step(idle);
                c.That(idle.Fault == null && idle.Clock.StepIndex == idleStep + 1, "independent session continues through actual native Update");
                c.Equal(idle.Hash(), factory.Load(idle.Save()).Hash(), "independent session remains saveable after provider fault");
            }
            c.That(ReferenceEquals(first, game.Fault), "refused faulted checks preserve original cancellation fault");
            c.Equal(0, game.World.Entities.All.Count(e => e.Def.Id == "token"), "failed cancellation cannot create productive output");
        }
    }
}
