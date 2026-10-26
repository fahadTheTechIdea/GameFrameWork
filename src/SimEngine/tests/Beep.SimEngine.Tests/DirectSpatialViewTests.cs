using System;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    internal static class DirectSpatialViewTests
    {
        internal const string Case = "Direct spatial views: shared capture admission and retained viewer faults";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(bool half = false, string fault = null)
        {
            var root = Json.Parse(PointGame.Definitions);
            root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            if (half) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
            if (fault != null) root.Get("point_spaces")[0].Set("snapshot_fault", JsonValue.Of(fault));
            return PointGame.Factory(definitions: root.ToString());
        }
        private static SimGame New(GameFactory factory, string scenario = "points") => factory.NewGame(new NewGameRequest
        { Scenario = scenario, Seed = 9441, Seats = { Controller.Human, Controller.Human } });
        private static void Read(Check c, PlayerView view, GameSnapshot published)
        {
            var direct = view.Space;
            c.Equal(published.Space?.Space, direct?.Space, "live and published projection use actual selected space");
            if (direct != null) c.Equal(published.Space.Version, direct.Version, "live viewer revision matches current publication");
            if (direct is PointWorldSnapshot point)
            {
                var publication = (PointWorldSnapshot)published.Space;
                c.Equal(publication.Extent, point.Extent, "actual detached extent agrees");
                c.Equal(publication.AccessOpen, point.AccessOpen, "actual detached access facts agree");
            }
        }
        private static void Healthy(Check c, bool half)
        {
            var f = Factory(half); var g = New(f); var idle = New(f); ulong idleHash = idle.Hash();
            var limits = new ReplayLimits(4 * 1024 * 1024, 6000); using var recording = g.RecordReplay(limits);
            var retained = (PointWorldSnapshot)g.Seat(0).View.Space;
            var target = Entity(g, "destination"); var worker = Entity(g, "machine");
            PermanentRemovalTests.Submit(c, g, new PostOrderCommand { Plan = "inspect", Target = target, Priority = 30 });
            var twin = f.Load(g.Save()); bool completed = false;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 1000 && watch.ElapsedMilliseconds < 20000; i++)
            {
                ulong before = g.Hash(); long time = g.Clock.StepIndex;
                Read(c, g.Seat(0).View, g.Seat(0).Snapshot); Read(c, g.Seat(1).View, g.Seat(1).Snapshot);
                c.Equal(before, g.Hash(), "direct spatial reads are pure"); c.Equal(time, g.Clock.StepIndex, "direct reads never drive game time");
                completed = g.Seat(0).View.Feature<WorkView>().TryReadActivityCompletion("activity:inspect", target, out _);
                if (completed) break;
                Step(g, twin);
            }
            if (!completed) throw new InvalidOperationException("direct spatial workflow exceeds1000Updates/20s clock=" + g.Clock.StepIndex
                + " fault=" + g.Fault + " entities=" + string.Join(";", g.Seat(0).Snapshot.Entities.Select(e => e.Id + "/" + e.State)));
            c.That(((PointSnapshot)g.Seat(0).Snapshot.Entities.Single(e => e.Id == worker).Spatial).Position > Fix.Zero,
                "engine physically moved actual worker before completing activity");
            PermanentRemovalTests.Submit(c, g, new SetPointAccessCommand { Open = false });
            Read(c, g.Seat(0).View, g.Seat(0).Snapshot); Read(c, g.Seat(1).View, g.Seat(1).Snapshot);
            c.That(!((PointWorldSnapshot)g.Seat(0).View.Space).AccessOpen, "real command changes original spatial authority");
            c.That(retained.AccessOpen, "retained direct payload is detached from later command");
            var restored = f.Load(g.Save()); c.Equal(g.Hash(), restored.Hash(), "live read workflow survives save/load");
            Read(c, restored.Seat(0).View, restored.Seat(0).Snapshot);
            c.Equal(idleHash, idle.Hash(), "interleaved idle session unchanged");
            c.That(new ReplayVerifier().Verify(f, recording.Finish(), limits).Success, "physical direct-view workflow replays");
        }
        private static void Refuse(Check c, string mode, int seat)
        {
            var f = Factory(fault: mode); var g = New(f); var idle = New(f); ulong idleHash = idle.Hash();
            var before = g.Seat(0).Snapshot; var foreignBefore = g.Seat(1).Snapshot;
            ulong hash = g.Hash(); var command = new DirectPointCaptureCommand();
            c.Equal(Code.None, g.Seat(seat).Preview(command), "negative probe is admitted through normal public command preview");
            c.Equal(hash, g.Hash(), "probe preview never closes access or captures malformed result");
            g.Seat(seat).Submit(command); g.Update(); var fault = g.Fault;
            c.That(fault != null, "direct read faults before publication");
            if (fault == null) throw new InvalidOperationException("missing direct capture fault " + mode);
            c.That(!fault.Message.Contains("proof.direct-space-read-returned"), "unvalidated direct result never escapes admission");
            string expected = mode == "null" || mode == "foreign" ? "actual selected space and viewer revision"
                : mode == "blank" ? "actual space identity" : mode == "negative" ? "nonnegative viewer revision"
                : mode == "throw" ? "proof.original-world-snapshot-error" : "mutat";
            c.That(fault.Message.Contains(expected), "actual direct capture refusal preserves original error");
            for (int viewer = 0; viewer < 2; viewer++)
            {
                var old = viewer == 0 ? before : foreignBefore; var current = g.Seat(viewer).Snapshot;
                c.Equal(GameState.Faulted, current.State, "retained viewer publication marks faulted session");
                c.Equal(old.Tick, current.Tick, "viewer retains last healthy publication time");
                c.That(ReferenceEquals(old.Space, current.Space), "each viewer retains its own healthy space projection");
                c.That(old.Entities.Select(e => e.Id).SequenceEqual(current.Entities.Select(e => e.Id)), "no partial entity prefix published");
                c.Equal(0, current.Changed.Count + current.Spawned.Count + current.Removed.Count, "failure invents no private deltas");
                c.Throws<InvalidOperationException>(() => { _ = g.Seat(viewer).View.Space; }, "faulted live read refuses instead of returning absent space");
            }
            long stopped = g.Clock.StepIndex; g.Update(); c.Equal(stopped, g.Clock.StepIndex, "faulted engine stops sole clock");
            c.Throws<InvalidOperationException>(() => g.Hash(), "faulted session cannot hash healthy");
            c.Throws<InvalidOperationException>(() => g.Save(), "faulted session cannot save healthy");
            c.That(ReferenceEquals(fault, g.Fault), "first observation fault retained"); c.Equal(idleHash, idle.Hash(), "faulted provider cannot affect other session");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current engine capture contract identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "current authored protocol, no historical spatial save pin");
            Healthy(c, false); Healthy(c, true);
            var native = New(ScopedHaulGame.Factory(restoredDockDelivery: true), "scoped-dock-restoration");
            Read(c, native.Seat(0).View, native.Seat(0).Snapshot);
            var plain = New(TransformerTests.Factory(), "process"); Read(c, plain.Seat(0).View, plain.Seat(0).Snapshot);
            c.That(plain.Seat(0).View.Space == null, "nonspatial healthy session keeps explicit absence");
            foreach (string mode in new[] { "null", "foreign", "blank", "negative", "throw", "mutate" })
                for (int seat = 0; seat < 2; seat++) Refuse(c, mode, seat);
        }
    }
}
