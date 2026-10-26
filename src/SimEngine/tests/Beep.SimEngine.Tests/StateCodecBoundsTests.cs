using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.PermanentRemovalTests;

namespace Beep.SimEngine.Tests
{
    // Codec probes are serialization-only; positive play uses authored content and public inputs/facts.
    internal static class StateCodecBoundsTests
    {
        internal const string Case = "State codec: format-width counts and strings preserve physical planned workflows";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static EntityId Entity(SimGame game, string name) => TransformationContentContractTests.Entity(game, name);
        private static byte[] Prefix(int value)
        {
            using var stream = new MemoryStream(); var writer = new StateWriter(stream);
            writer.Int(ref value); writer.Flush(); return stream.ToArray();
        }
        private static void Primitives(Check c)
        {
            foreach (int count in new[] { 0, 1, (1 << 24) + 1, int.MaxValue })
            {
                using var stream = new MemoryStream(); var writer = new StateWriter(stream);
                c.Equal(count, writer.Count(count), "format-width count writer"); writer.Flush();
                byte[] bytes = stream.ToArray(); c.Equal(4, bytes.Length, "count encodes one native integer, not implied element allocations");
                var reader = new StateReader(bytes, 0, bytes.Length);
                int decoded = reader.Count(0); c.Equal(count, decoded, "reader has no hidden run-count ceiling"); c.That(reader.AtEnd, "exact count slice exhausted");
                var a = new StateHasher(); var b = new StateHasher(); a.Count(count); b.Count(decoded);
                c.Equal(a.Value, b.Value, "same count participates in original hash pass");
            }
            foreach (int invalid in new[] { -1, int.MinValue })
            {
                using var stream = new MemoryStream(); var writer = new StateWriter(stream); var hash = new StateHasher(); ulong before = hash.Value;
                c.Throws<CorruptStateException>(() => writer.Count(invalid), "negative writer count is not resumable state");
                c.Equal(0L, stream.Length, "invalid count writes no prefix");
                c.Throws<CorruptStateException>(() => hash.Count(invalid), "negative hash count refused by same validator");
                c.Equal(before, hash.Value, "invalid count cannot mutate hash");
                byte[] bytes = Prefix(invalid);
                c.Throws<CorruptStateException>(() => new StateReader(bytes, 0, bytes.Length).Count(0), "negative serialized count refused");
            }
            string large = new string('\u03c0', 600000) + "\u6c34\ud83d\ude80";
            foreach (string value in new[] { null, string.Empty, large })
            {
                using var stream = new MemoryStream(); stream.WriteByte(77); stream.WriteByte(88);
                var writer = new StateWriter(stream); string original = value; writer.String(ref original); writer.Flush();
                int end = checked((int)stream.Length); stream.WriteByte(99); byte[] bytes = stream.ToArray();
                var reader = new StateReader(bytes, 2, end - 2); string restored = "sentinel"; reader.String(ref restored);
                c.That(value == restored && reader.AtEnd, "null/empty/multibyte large string restores exact original slice");
                if (value != null && value.Length > 0) c.That(end - 6 > 1 << 20, "actual UTF8 payload exceeds former reader ceiling");
                var a = new StateHasher(); var b = new StateHasher(); a.String(ref original); b.String(ref restored);
                c.Equal(a.Value, b.Value, "UTF8 decode preserves original string hash");
                c.Throws<CorruptStateException>(() => { byte v = 0; reader.Byte(ref v); }, "outer backing bytes cannot extend supplied slice");
                if (value == large)
                    c.Throws<CorruptStateException>(() => { string text = null; new StateReader(bytes, 2, end - 3).String(ref text); }, "large missing final payload byte refuses");
            }
            foreach (int invalid in new[] { -2, int.MinValue, int.MaxValue })
            {
                byte[] bytes = Prefix(invalid);
                c.Throws<CorruptStateException>(() => { string value = null; new StateReader(bytes, 0, bytes.Length).String(ref value); }, "invalid/truncated length refuses without fabricated string");
            }
            for (int bytes = 0; bytes < 4; bytes++)
            {
                var fragment = new byte[bytes];
                c.Throws<CorruptStateException>(() => new StateReader(fragment, 0, fragment.Length).Count(0), "truncated count prefix refuses");
                c.Throws<CorruptStateException>(() => { string value = null; new StateReader(fragment, 0, fragment.Length).String(ref value); }, "truncated string prefix refuses");
            }
        }
        private static ActivityNodeView Node(SimGame g, int order, string name)
        { var rows = new List<ActivityNodeView>(); if (!Work(g).TryCopyNodes(order, rows)) throw new InvalidOperationException("missing actual order " + order); return rows.Single(x => x.Id == name); }
        private static void Physical(Check c, bool half, bool independent)
        {
            var f = WorkLifecycleAcceptanceTests.Compose(half, independent); var g = ConstructionPlanTests.NewPoints(f);
            var isolated = ConstructionPlanTests.NewPoints(f); ulong idle = isolated.Hash();
            c.That(!g.Modules.Contains(TycoonModule.ModuleId), "large state does not impose business/platform gameplay");
            var limits = new ReplayLimits(32 * 1024 * 1024, 6000); using var recording = g.RecordReplay(limits);
            Policy(c, g, "machine", true); Policy(c, g, "lifecycle.a", true, true);
            Submit(c, g, new SetHomeCommand { Worker = Entity(g, "machine"), Home = Entity(g, "source") });
            string group = new string('g', (1 << 20) + 1);
            var command = new SubmitPlanCommand { Intent = new PlanIntent("build:extension.facility",
                Json.Parse("{\"placement\":{\"space\":\"field.points\",\"position\":30}}"), group, 70, true) };
            ulong before = g.Hash(); c.Equal(Code.None, g.Seat(0).Preview(command), "valid large ordinary group has no gameplay-size ceiling");
            c.Equal(before, g.Hash(), "wide input preview pure"); int sequence = g.Seat(0).Submit(command);
            var twin = Save(c, f, g); Step(g, twin);
            long cursor = 0; var results = new List<CommandResult>(); g.Seat(0).ReadResults(ref cursor, results);
            var result = results.Single(x => x.Sequence == sequence); c.That(result.Accepted, "actual saved wide inbox command executes normally");
            long plan = result.Arg0; var retained = Plan(g, plan);
            c.That(retained.Group == group && retained.Order == 0 && retained.Paused, "real paused intent retains exact large identity without building");
            var foreign = new List<PlanView>(); Work(g, 1).CopyPlans(foreign); c.Equal(0, foreign.Count, "wide values remain company private");
            twin = Save(c, f, g); g.Clock.Pause(); twin.Clock.Pause(); before = g.Hash(); long time = g.Clock.StepIndex; Step(g, twin);
            c.That(g.Hash() == before && g.Clock.StepIndex == time, "large-state saved twins still use one paused game clock");
            g.Clock.Resume(); twin.Clock.Resume();
            Submit(c, g, new ConfigurePlanCommand { Plan = plan, Group = "codec.field", Priority = 70, Paused = false });
            Submit(c, g, new PostOrderCommand { Plan = "extension.calibrate", Target = Entity(g, "lifecycle.a") }); twin = Save(c, f, g);
            Reach(g, twin, () => Plan(g, plan).Order > 0, "original physical site from restored wide intent"); int order = Plan(g, plan).Order;
            Reach(g, twin, () => Node(g, order, "assemble").Progress.WholeTicks >= (half ? 4 : 2), "real material-consuming travel and assembly prefix");
            var prefix = Node(g, order, "assemble");
            c.That(prefix.Worker == Entity(g, "lifecycle.a") && At(g, prefix.Worker) == Fix.FromInt(30), "selected native/independent machine actually reaches site");
            c.That(Node(g, order, "materials").Resources.Single().Spent && Total(g) == Fix.FromInt(12), "actual eight materials consumed once, not decoded gifts");
            Submit(c, g, new ConfigurePlanCommand { Plan = plan, Group = group, Priority = 70, Paused = true });
            var held = Node(g, order, "assemble").Progress; twin = Save(c, f, g);
            for (int i = 0; i < 3; i++) Step(g, twin);
            c.That(Plan(g, plan).Group == group && Node(g, order, "assemble").Progress.Equals(held), "large active graph restores without loss/extra work");
            var hidden = new List<ActivityNodeView>(); c.That(!Work(g, 1).TryCopyNodes(order, hidden) && hidden.Count == 0, "saved active large-group workflow still private");
            Submit(c, g, new ConfigurePlanCommand { Plan = plan, Group = "codec.field", Priority = 70, Paused = false }); twin = Save(c, f, g);
            Reach(g, twin, () => Plan(g, plan).State == PlanState.Done, "original physical construction completes after wide-state restore");
            c.That(g.Seat(0).Snapshot.Entities.Count(x => x.Def == "extension.facility") == 1 &&
                !g.Seat(0).Snapshot.Entities.Any(x => x.Def == "extension.site") && Total(g) == Fix.FromInt(12), "one earned effect/retired site/conserved material");
            c.That(Work(g).TryReadActivityCompletion("build:extension.facility", EntityId.None, out _), "same original completion owner");
            c.That(retained.Group == group && retained.Paused && retained.Order == 0, "retained large view immutable after actual rename/build");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "terminal original save/hash identity"); c.Equal(idle, isolated.Hash(), "interleaved wide inputs do not mutate another session");
            var replay = new ReplayVerifier().Verify(f, recording.Finish(), limits);
            c.That(replay.Success && replay.Game.Hash() == g.Hash(), "wide ordinary input replay: " + replay.Failure + "/" + replay.Detail);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current literal codec build");
            Primitives(c);
            foreach (bool half in new[] { false, true }) foreach (bool independent in new[] { false, true }) Physical(c, half, independent);
        }
    }
}
