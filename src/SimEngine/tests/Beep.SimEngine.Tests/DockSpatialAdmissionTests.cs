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
    // JSON authoring, ordinary player commands, bounded Update and immutable facts only.
    internal static class DockSpatialAdmissionTests
    {
        internal const string Case = "Dock spatial admission: required overlap rejects before initialization and preserves physical hauling";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(DockSpatialAdmissionModule probe, string mode)
        {
            JsonValue root = Json.Parse(FrameworkProofContent.Read("dock-spatial-admission.json"));
            var scenario = root.Get("scenarios")[0];
            if (mode == "tile") scenario.Set("space", JsonValue.Of("dock.tiles"));
            if (mode == "foreign" || mode == "no-presence") scenario.Set("space", JsonValue.Null());
            if (mode == "no-presence") root.Get("entities")[0].Get("components").Remove("dock_admission_presence");
            if (mode == "missing-provider" || mode == "wrong-provider-id")
                root.Get("unavailable_tile_spaces")[0].Set("mode", JsonValue.Of(mode));
            if (mode == "duplicate-slots") root.Get("entities")[0].Get("components").Get("dock_admission").Get("slots")[1].Set("id", JsonValue.Of("service"));
            var definition = new GameDefinition("dock-admission", new GameModule[] {
                new InventoryModule(), new ContentAccessModule(), new MotionModule(), new TileNavigationModule(),
                new WorkModule(), new WorldSetupModule(), probe });
            var files = new PreloadedFileSource().Add("manifest.json", FrameworkProofContent.Read("dock-spatial-admission-master.json"))
                .Add("dock-spatial-admission.json", root.ToString());
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory f, string scenario, int seats = 1)
        {
            var request = new NewGameRequest { Scenario = scenario, Seed = 628 };
            for (int i = 0; i < seats; i++) request.Seats.Add(Controller.Human);
            return f.NewGame(request);
        }
        private static EntityId Entity(SimGame g, string name) => g.Seat(0).Snapshot.Entities.Single(x => x.Def == name).Id;
        private static List<WorkJobView> Jobs(SimGame g)
        { var rows = new List<WorkJobView>(); g.Seat(0).View.Feature<WorkView>().CopyJobs(rows); return rows; }
        private static Fix Stock(SimGame g, EntityId id, ResourceId resource)
        {
            Code code = g.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(id, resource, out var facts);
            if (!code.IsNone) throw new InvalidOperationException("actual stock facts refused " + code); return facts.Stock;
        }
        private static Fix Total(SimGame g, ResourceId resource)
        {
            Fix total = Fix.Zero;
            foreach (var entity in g.Seat(0).Snapshot.Entities)
                if (g.Seat(0).View.Feature<LogisticsView>().ReadDiagnostics(entity.Id, resource, out var facts).IsNone) total += facts.Stock;
            return total;
        }
        private static void Step(Check c, SimGame g, SimGame twin, ResourceId resource, int opening)
        {
            g.Update(); twin?.Update();
            if (g.Fault != null || twin?.Fault != null) throw new InvalidOperationException("dock physical fault " + g.Fault + "/" + twin?.Fault);
            if (twin != null) c.Equal(g.Hash(), twin.Hash(), "saved physical dock twin matches after every ordinary Update");
            c.Equal(Fix.FromInt(opening), Total(g, resource), "all actual stock/cargo conserved through dock service and motion");
        }
        private static void Until(Check c, SimGame g, SimGame twin, ResourceId resource, int opening, Func<bool> done, string stage)
        {
            var trail = new Queue<string>();
            for (int i = 0; i < 512 && !done(); i++)
            {
                trail.Enqueue(g.Clock.StepIndex + ":stock=" + Total(g, resource) + ":jobs=" + string.Join(";", Jobs(g).Select(x => x.Id + "/" + x.State + "/" + x.ToilIndex + "/" + x.Loaded + "/" + x.Blocker)));
                if (trail.Count > 8) trail.Dequeue(); Step(c, g, twin, resource, opening);
            }
            if (!done()) throw new InvalidOperationException(stage + " exceeds512Updates " + string.Join(" | ", trail));
            Console.WriteLine("dock stage: " + stage + " at=" + g.Clock.StepIndex);
        }
        private static void Physical(Check c, bool tile, bool half = false)
        {
            Console.WriteLine("dock physical: tile=" + tile + " half=" + half);
            GameFactory f = tile ? SharedHaulAdmissionTests.Factory() : TransporterRetirementTests.Compose(half, false);
            SimGame g = tile ? New(f, "shared-haul", 2) : ConstructionPlanTests.NewPoints(f);
            SimGame idle = tile ? New(f, "shared-haul", 2) : ConstructionPlanTests.NewPoints(f); ulong isolated = idle.Hash();
            if (!tile) PermanentRemovalTests.Policy(c, g, "machine", true);
            string prefix = tile ? "queue." : "";
            EntityId source = Entity(g, prefix + "source"), destination = Entity(g, prefix + "destination"), worker = Entity(g, tile ? "queue.machine.a" : "machine");
            if (!g.Seat(0).View.TryFindResource(tile ? "process.input" : "supply", out var resource)) throw new InvalidOperationException("missing actual authored resource");
            int opening = tile ? 32 : 20;
            c.That(!g.Modules.Contains(TycoonModule.ModuleId), "dock work is resource-only engine simulation");
            var limits = new ReplayLimits(4 * 1024 * 1024, 6000); using var recording = g.RecordReplay(limits);
            SpatialSnapshot initial = g.Seat(0).Snapshot.Entities.Single(x => x.Id == worker).Spatial;
            long start = g.Clock.StepIndex;
            CommandResult accepted = PermanentRemovalTests.Submit(c, g, new HaulResourcesCommand { Source = source, Destination = destination,
                Worker = worker, Resource = resource, Ceiling = Fix.FromInt(8), Priority = 100 });
            int id = checked((int)accepted.Arg0);
            c.That(Jobs(g).Any(x => x.Id == id) && Stock(g, destination, resource).IsZero, "normal accepted job does not instantly deliver");
            Until(c, g, null, resource, opening, () => Jobs(g).Any(x => x.Id == id && x.Loaded.IsPositive && x.Loaded < x.Amount), "partial loading");
            var retained = Jobs(g).Single(x => x.Id == id);
            c.That(!retained.SourceDockSlot.IsNone && retained.HandlingProgress.WholeTicks > 0, "actual source dock commitment and elapsed partial handling");
            SimGame twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "saved actual partial dock handling");
            g.Clock.Pause(); twin.Clock.Pause(); ulong paused = g.Hash(); long clock = g.Clock.StepIndex;
            Step(c, g, twin, resource, opening);
            c.That(g.Hash() == paused && g.Clock.StepIndex == clock, "sole paused clock retains physical service");
            g.Clock.Resume(); twin.Clock.Resume();
            Until(c, g, twin, resource, opening, () => Jobs(g).Any(x => x.Id == id && x.ToilIndex == 3 && x.Loaded.IsPositive && x.Loaded < x.Amount), "partial delivery");
            SpatialSnapshot deliveredPosition = g.Seat(0).Snapshot.Entities.Single(x => x.Id == worker).Spatial;
            c.That(!initial.Matches(deliveredPosition) && g.Clock.StepIndex > start, "carrier physically moves to the receiving dock over game time");
            twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "saved actual partial destination handling");
            Until(c, g, twin, resource, opening, () => !Jobs(g).Any(x => x.Id == id), "delivery and departure complete");
            c.Equal(Fix.FromInt(opening - 8), Stock(g, source, resource), "source debited exactly once");
            c.Equal(Fix.FromInt(8), Stock(g, destination, resource), "destination receives exactly eight once");
            c.Equal(Fix.Zero, Stock(g, worker, resource), "carrier settles actual cargo");
            c.Equal(isolated, idle.Hash(), "interleaved untouched session has no shared spatial state");
            c.Equal(Codes.NotOwned, g.Seat(1).View.Feature<LogisticsView>().ReadDiagnostics(source, resource, out _),
                "visible geometry does not expose another company's owned operational stock");
            var replay = new ReplayVerifier().Verify(f, recording.Finish(), limits);
            c.That(replay.Success && replay.Game != null && replay.Game.Hash() == g.Hash(), "ordinary physical dock replay verifies " + replay.Failure + "/" + replay.Detail);
        }
        private static void Run(Check c)
        {
            foreach (string mode in new[] { "without-overlap", "missing-provider", "wrong-provider-id", "no-presence", "duplicate-slots" })
            {
                var probe = new DockSpatialAdmissionModule(); Exception refusal = null;
                try { New(Factory(probe, mode), "dock-admission"); } catch (Exception error) { refusal = error; }
                c.That(refusal is ContentException, "unused authored dock refuses invalid composition " + mode + "/" + refusal);
                if (mode == "without-overlap") c.That(refusal?.Message.Contains("spatial overlap provider") == true, "matching presence alone cannot substitute required overlap geometry");
                if (mode == "no-presence") c.That(refusal?.Message.Contains("spatial presence") == true, "dock itself requires actual spatial presence");
                c.Equal(0, probe.Initialize.Initializations, "all invalid unused dock content rejects before Initialize");
            }
            foreach (string mode in new[] { "tile", "foreign" })
            {
                var probe = new DockSpatialAdmissionModule(); var f = Factory(probe, mode); var g = New(f, "dock-admission");
                c.Equal(1, probe.Initialize.Initializations, "valid unused selected/foreign dock content initializes normally");
                c.Equal(0, g.Seat(0).Snapshot.Entities.Count, "empty authored setup does not fabricate instances");
                c.That((g.Observer.Space == null) == (mode == "foreign"), "actual selected/native or deliberately absent space, never fallback geometry");
                var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "valid empty spatial/nonspatial session saves");
                g.Update(); twin.Update(); c.Equal(g.Hash(), twin.Hash(), "valid empty spatial/nonspatial continuation is deterministic");
            }
            Physical(c, true); Physical(c, false); Physical(c, false, true);
        }
    }
}
