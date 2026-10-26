using System;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Actual master-listed files; public player commands/views/time/saves only.
    internal static class RemovalAdmissionObservationTests
    {
        internal const string Case = "Removal admission observation: physical packing refusal saved removal and callback first-fault cutoff";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(string mode, bool half) => PermanentRemovalTests.Factory(half, endpoints: true,
            authorWorld: root => {
                root.Get("entities")[0].Get("components").Set("removal_admission_probe",
                    Json.Parse("{\"mode\":\"" + mode + "\"}"));
                StartupContentAuthoring.Position(root, "machine", JsonValue.Number("90"));
            });
        private static SimGame New(GameFactory f) => ConstructionPlanTests.NewPoints(f);
        private static ConstructionView View(SimGame g, int seat = 0) => g.Seat(seat).View.Feature<ConstructionView>();
        private static PlanView Plan(SimGame g, long id) => PermanentRemovalTests.Plan(g, id);
        private static void Healthy(Check c, string mode, bool half)
        {
            var f = Factory(mode, half); var g = New(f); var idle = New(f); ulong isolated = idle.Hash();
            EntityId source = Entity(g, "source"), actor = Entity(g, "lifecycle.b");
            var limits = new ReplayLimits(4 * 1024 * 1024, 6000); using var recording = g.RecordReplay(limits);
            PermanentRemovalTests.Policy(c, g, "lifecycle.b", true, true);
            ulong pure = g.Hash(); c.Equal(Code.None, View(g).CheckRecovery(source), "actual recovery preflight ready before work");
            c.Equal(pure, g.Hash(), "normal recovery preflight pure"); c.Equal(Fix.FromInt(20), PermanentRemovalTests.Total(g), "authored actual initial stock");
            c.Equal(Codes.NotOwned, View(g, 1).CheckRecovery(source), "foreign source recovery withheld");
            g.Clock.Pause(); long stopped = g.Clock.StepIndex; pure = g.Hash();
            c.Equal(mode == "refuse" ? RemovalAdmissionProbe.Refused : Code.None, View(g).CheckRecovery(source), "original guard typed refusal preserved");
            c.Equal(pure, g.Hash(), "paused guard preview pure"); c.That(g.Fault == null, "legal guard refusal remains healthy");
            var twin = f.Load(g.Save()); twin.Clock.Pause(); Step(g, twin); c.Equal(stopped, g.Clock.StepIndex, "paused recovery earns no time"); g.Clock.Resume();
            var args = Json.Parse("{\"target\":" + source.Value + ",\"workers\":[{\"node\":\"pack\",\"role\":\"extension.builder\",\"worker\":" + actor.Value +
                "},{\"node\":\"retire\",\"role\":\"extension.builder\",\"worker\":" + actor.Value + "}]}");
            long id = PermanentRemovalTests.Submit(c, g, new SubmitPlanCommand { Intent = new PlanIntent("activity:retire.source-plan", args, "retire", 100, false) }).Arg0;
            twin = f.Load(g.Save()); int updates = 0; bool packedCheckpoint = false, travelCheckpoint = false;
            for (; updates < 512 && PermanentRemovalTests.Exists(g, source); updates++) {
                var current = Plan(g, id);
                if (!travelCheckpoint && current.Order > 0) { twin = f.Load(g.Save()); travelCheckpoint = true; }
                if (!packedCheckpoint && PermanentRemovalTests.Facts(g, source).Stock.IsZero) {
                    twin = f.Load(g.Save()); packedCheckpoint = true;
                    c.Equal(Fix.FromInt(20), PermanentRemovalTests.Total(g), "packed checkpoint conserves source material");
                    c.That(g.Seat(0).Snapshot.Entities.Count(x => x.Def == View(g).RecoveryContainer) == 5, "real20 stock occupies five authored4-unit containers");
                }
                Step(g, twin); c.Equal(Fix.FromInt(20), PermanentRemovalTests.Total(g), "each public update conserves physical recovery stock");
            }
            if (PermanentRemovalTests.Exists(g, source))
                throw new InvalidOperationException("removal exceeds512Updates clock=" + g.Clock.StepIndex + "/plan=" + Plan(g, id).State +
                    "/jobs=" + string.Join(";", PermanentRemovalTests.Jobs(g).Select(x => x.Id + "/" + x.State + "/" + x.Blocker)));
            c.That(updates > 5 && travelCheckpoint && packedCheckpoint, "travel packing and retirement take actual time with distinct saved stages");
            c.That(!PermanentRemovalTests.Exists(twin, source), "saved twin retires same source once");
            c.Equal(Fix.FromInt(20), PermanentRemovalTests.Total(g), "retirement preserves packed original stock");
            c.Equal(Codes.NotFound, View(g).CheckRecovery(source), "removed source returns typed not found");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "completed recovery/removal durable"); c.Equal(isolated, idle.Hash(), "interleaved healthy removal isolated");
            c.That(new ReplayVerifier().Verify(f, recording.Finish(), limits).Success, "public pause recovery timed removal replay");
        }
        private static void Negative(Check c, string mode, bool half)
        {
            var f = Factory(mode, half); var g = New(f); var idle = New(f); ulong isolated = idle.Hash();
            var source = Entity(g, "source"); var before = g.Seat(0).Snapshot; var foreign = g.Seat(1).Snapshot; long time = g.Clock.StepIndex;
            c.Equal(Code.None, View(g).CheckRecovery(source), "negative guard dormant before public clock pause");
            g.Clock.Pause(); Exception thrown = null; try { View(g).CheckRecovery(source); } catch (Exception error) { thrown = error; }
            c.That(thrown != null && g.Fault != null, "illegal removal callback cannot pass admission " + mode);
            var fault = g.Fault;
            if (mode == "throw") c.That(fault.Message.Contains("proof.original-removal-guard-error"), "original guard exception retained");
            g.Update(); var after = g.Seat(0).Snapshot;
            c.Equal(time, g.Clock.StepIndex, "faulted guard cannot earn time"); c.Equal(GameState.Faulted, after.State, "guard failure publishes explicit fault");
            c.Equal(before.Tick, after.Tick, "last healthy removal time retained");
            c.Equal(0, after.Changed.Count + after.Spawned.Count + after.Removed.Count, "no invalid packing/removal delta prefix");
            c.That(after.Entities.Any(x => x.Id == source), "faulted preview publishes no deletion");
            c.That(ReferenceEquals(before.Space, after.Space) && ReferenceEquals(foreign.Space, g.Seat(1).Snapshot.Space), "each viewer retains own healthy spatial state");
            foreach (var old in before.Entities) c.That(ReferenceEquals(old.Spatial, after.Entities.Single(x => x.Id == old.Id).Spatial), "healthy payload retained");
            c.Throws<InvalidOperationException>(() => View(g).CheckRecovery(source), "later live recovery refuses");
            c.Throws<InvalidOperationException>(() => g.Hash(), "guard fault cannot hash healthy"); c.Throws<InvalidOperationException>(() => g.Save(), "guard fault cannot save healthy");
            g.Update(); c.Equal(time, g.Clock.StepIndex, "repeat Update respects guard cutoff"); c.That(ReferenceEquals(fault, g.Fault), "first guard fault retained");
            c.Equal(isolated, idle.Hash(), "guard fault session isolated");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current removal observation engine identity"); c.Equal(154, EngineInfo.SaveFormat, "no duplicate production lifecycle state");
            foreach (bool half in new[] { false, true }) { Healthy(c, "normal", half); Healthy(c, "refuse", half); }
            foreach (string mode in new[] { "state", "sibling", "clock", "space", "throw" })
                foreach (bool half in new[] { false, true }) Negative(c, mode, half);
        }
    }
}
