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
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Actual authored master/files and ordinary public player surfaces. Faults belong to the engine-side provider.
    internal static class DockProviderObservationTests
    {
        internal const string Case = "Dock provider observation: pure previews and live motion intent preserve original authority and first faults";
        internal static void Register() => TestRunner.Add(Case, Run);

        private static GameFactory Points(bool half = false, string fault = null, int at = 0, bool closed = false)
        {
            var root = Json.Parse(PointGame.Definitions);
            root.Get("scenarios")[0].Set("seats", JsonValue.Number("2"));
            var entities = JsonValue.NewArray(); entities.Add(root.Get("entities")[0]); entities.Add(root.Get("entities")[2]); root.Set("entities", entities);
            if (half) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
            if (closed) root.Get("point_spaces")[0].Set("access_open", JsonValue.Of(false));
            var document = Json.Parse(FrameworkProofContent.Read("dock-cost-points.json"));
            var dock = document.Get("entities")[0].Get("components").Get("point_dock");
            if (fault != null) { dock.Set("fault", JsonValue.Of(fault)); dock.Set("fault_at", JsonValue.Number(at.ToString())); }
            return PointGame.Factory(definitions: root.ToString(), additionalDocuments: new Dictionary<string, string> { { "dock-cost-points.json", document.ToString() } });
        }
        private static SimGame New(GameFactory f, bool tile = false)
            => f.NewGame(new NewGameRequest { Scenario = tile ? "shared-haul" : "points", Seed = 2879, Seats = { Controller.Human, Controller.Human } });
        private static HaulResourcesCommand Haul(SimGame g, bool tile = false)
        {
            g.Seat(0).View.TryFindResource(tile ? "process.input" : "supply", out var resource);
            return new HaulResourcesCommand { Source = Entity(g, tile ? "queue.source" : "source"), Destination = Entity(g, tile ? "queue.destination" : "destination"),
                Worker = Entity(g, tile ? "queue.machine.a" : "machine"), Resource = resource, Ceiling = Fix.FromInt(8), Priority = 100 };
        }
        private static bool Live(WorkJobView row) => row.State == JobState.Open || row.State == JobState.Assigned || row.State == JobState.Paused;
        private static Fix Amount(SimGame game, string entity, bool tile)
        {
            var rows = new List<Facet>(); game.Seat(0).View.Inspect(Entity(game, entity), rows);
            return rows.Where(x => x.Key == Storage.FacetStock && x.Subject == (tile ? "process.input" : "supply"))
                .Aggregate(Fix.Zero, (sum, x) => sum + x.Value);
        }
        private static void Healthy(Check c, bool half, bool tile = false)
        {
            var f = tile ? SharedHaulAdmissionTests.Factory() : Points(half); var g = New(f, tile); var idle = New(f, tile); ulong isolated = idle.Hash();
            var limits = new ReplayLimits(4 * 1024 * 1024, 4000); using var replay = g.RecordReplay(limits);
            var cmd = Haul(g, tile); ulong pure = g.Hash(); c.Equal(Code.None, g.Seat(0).Preview(cmd), "actual observed dock journey is ready");
            c.Equal(pure, g.Hash(), "whole-participant/space checks do not mutate healthy readiness");
            int id = checked((int)PermanentRemovalTests.Submit(c, g, cmd).Arg0);
            string source = tile ? "queue.source" : "source", destination = tile ? "queue.destination" : "destination", machine = tile ? "queue.machine.a" : "machine";
            c.Equal(Fix.Zero, Amount(g, destination, tile), "observed approach requests movement, not instant delivery");
            var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "actual accepted dock receipt/intent survives save");
            bool moved = false; var before = g.Seat(0).Snapshot.Entities.Single(x => x.Id == cmd.Worker).Spatial;
            int updates = 0; bool savedCargo = false;
            while (updates++ < 400 && PermanentRemovalTests.Jobs(g).Any(x => x.Id == id && Live(x))) {
                Step(g, twin);
                var current = g.Seat(0).Snapshot.Entities.Single(x => x.Id == cmd.Worker).Spatial;
                moved |= !before.Matches(current);
                if (!savedCargo && PermanentRemovalTests.Jobs(g).Any(x => x.Id == id && x.Loaded.IsPositive)) {
                    twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "original loaded cargo/dock geometry restore"); savedCargo = true;
                    g.Clock.Pause(); twin.Clock.Pause(); ulong paused = g.Hash(); long clock = g.Clock.StepIndex;
                    Step(g, twin); c.Equal(clock, g.Clock.StepIndex, "sole paused game clock prevents physical progress"); c.Equal(paused, g.Hash(), "pause preserves actual cargo/claims");
                    g.Clock.Resume(); twin.Clock.Resume();
                }
            }
            if (PermanentRemovalTests.Jobs(g).Any(x => x.Id == id && Live(x)))
                throw new InvalidOperationException("observed physical haul exceeds400Updates clock=" + g.Clock.StepIndex + " jobs=" +
                    string.Join(";", PermanentRemovalTests.Jobs(g).Select(x => x.Id + "/" + x.State + "/" + x.ToilIndex + "/" + x.Blocker + "/" + x.DestinationDockSlot)));
            c.That(moved && updates > 1 && savedCargo, "legal native/independent motion invalidation remains timed and physical");
            c.Equal(Fix.FromInt((tile ? 32 : 20) - 8), Amount(g, source, tile), "eight actual supplies leave original source once");
            c.Equal(Fix.FromInt(8), Amount(g, destination, tile), "eight supplies reach original destination once");
            c.Equal(Fix.Zero, Amount(g, machine, tile), "original cargo settles without duplication");
            var privateJobs = new List<WorkJobView>(); g.Seat(1).View.Feature<WorkView>().CopyJobs(privateJobs); c.Equal(0, privateJobs.Count, "private dock work withheld");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "settled healthy state restores");
            c.Equal(isolated, idle.Hash(), "interleaved session remains isolated");
            c.That(new ReplayVerifier().Verify(f, replay.Finish(), limits).Success, "ordinary player replay keeps legal observed approach deterministic");
        }
        private static void Faulted(Check c, string mode, bool live)
        {
            var f = Points(fault: mode, at: live ? 3 : 0); var g = New(f); var idle = New(f); ulong isolated = idle.Hash();
            var seat = g.Seat(0); var cached = seat.View.Feature<WorkView>(); var command = Haul(g); var retained = seat.Snapshot;
            SimGame twin = null;
            if (live) {
                PermanentRemovalTests.Submit(c, g, command); twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "pending fault composition saves original healthy intent " + mode);
                for (int i = 0; i < 100 && g.Fault == null; i++) { g.Update(); twin.Update(); }
                c.That(g.Fault != null && twin.Fault != null, "actual live callback faults within100Updates " + mode);
                if (g.Fault == null) throw new InvalidOperationException("fault probe never reached " + mode + " clock=" + g.Clock.StepIndex);
                c.Equal(g.Clock.StepIndex, twin.Clock.StepIndex, "saved negative twins stop on identical game-time coordinate " + mode);
                c.Equal(g.Fault.Message, twin.Fault.Message, "saved negative twins preserve identical first error " + mode);
            } else c.Throws<InvalidOperationException>(() => seat.Preview(command), "pure provider violation cannot return readiness " + mode);
            var fault = g.Fault;
            c.That(fault != null && g.State == GameState.Faulted, "original first fault is latched, not merely thrown " + mode);
            if (mode.EndsWith("-throw", StringComparison.Ordinal)) c.That(fault.Message.Contains("dock.probe."), "callback's original exception is retained " + mode);
            c.Throws<InvalidOperationException>(() => g.Save(), "faulted dock cannot produce healthy save " + mode);
            c.Throws<InvalidOperationException>(() => g.Hash(), "faulted dock cannot produce healthy hash " + mode);
            c.Throws<InvalidOperationException>(() => seat.Preview(command), "faulted dock is not retried by another preview " + mode);
            var jobs = new List<WorkJobView>(); c.Throws<InvalidOperationException>(() => cached.CopyJobs(jobs), "cached private work view rejects faulted query " + mode);
            c.Throws<InvalidOperationException>(() => cached.ReadWorkerExecution(EntityId.None, out _), "faulted missing entity cannot bypass healthy gate " + mode);
            c.Equal(0, jobs.Count, "no partial physical-job facts published " + mode);
            long clock = g.Clock.StepIndex; g.Update(); c.Equal(clock, g.Clock.StepIndex, "faulted session completes no further game update " + mode);
            c.That(ReferenceEquals(fault, g.Fault), "all subsequent refusals retain exact first fault " + mode);
            c.That(retained.Entities.Count > 0, "previous immutable snapshot remains safe " + mode);
            c.Equal(isolated, idle.Hash(), "negative callback cannot fault another interleaved session " + mode);
        }
        private static void Refusal(Check c)
        {
            var f = Points(closed: true); var g = New(f); ulong hash = g.Hash();
            c.That(!g.Seat(0).Preview(Haul(g)).IsNone, "expected closed physical route remains typed refusal");
            c.Equal(hash, g.Hash(), "ordinary route refusal allocates no stock/dock/native claims");
            c.That(g.Fault == null, "ordinary route refusal is not a provider contract fault");
            c.Equal(g.Hash(), f.Load(g.Save()).Hash(), "expected refusal remains healthy checkpoint");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "actual current observed dock identity");
            Healthy(c, false); Healthy(c, true); Healthy(c, false, tile: true); Refusal(c);
            foreach (string phase in new[] { "request", "vacancy", "cost", "geometry" })
                foreach (string action in new[] { "touch", "payload", "space", "clock", "throw" })
                    Faulted(c, phase + "-" + action, false);
            foreach (string phase in new[] { "request", "vacancy", "cost" })
                foreach (string action in new[] { "carrier-touch", "carrier-payload" }) Faulted(c, phase + "-" + action, false);
            foreach (string mode in new[] { "slots-touch", "slots-foreign", "geometry-null", "geometry-space", "negative-cost" }) Faulted(c, mode, false);
            foreach (string phase in new[] { "reach", "approach" })
                foreach (string action in new[] { "touch", "payload", "carrier-payload", "space", "clock", "throw" }) Faulted(c, phase + "-" + action, true);
            foreach (string mode in new[] { "invalid-reach", "endpoint-approach-touch", "approach-invalid", "approach-position" }) Faulted(c, mode, true);
        }
    }
}

