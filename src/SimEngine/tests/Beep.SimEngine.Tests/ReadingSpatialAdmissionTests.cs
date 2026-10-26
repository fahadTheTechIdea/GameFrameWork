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
    // Pure JSON composition, ordinary player inputs, bounded Update, immutable facts/save/replay.
    internal static partial class ReadingSpatialAdmissionTests
    {
        internal const string Case = "Spatial reading admission: direct contracts require actual layers and preserve physical knowledge";
        private const string Provider = "sim.knowledge.facts";
        internal static void Register()
        {
            TestRunner.Add(Case, Run);
            TestRunner.Add(FreshnessCase, FinitePointReadings);
            TestRunner.Add(CoverageCase, CoverageCallbacks);
        }
        private static GameFactory Admission(ReadingSpatialAdmissionModule proof, string mode)
        {
            JsonValue root = Json.Parse(FrameworkProofContent.Read("reading-spatial-admission.json"));
            if (mode == "foreign") root.Get("scenarios")[0].Set("space", JsonValue.Null());
            if (mode == "missing-provider" || mode == "missing-layer" || mode == "wrong-readable-layer")
            {
                var spaces = root.Get("tile_spaces");
                spaces[0].Set("mode", JsonValue.Of(mode == "missing-layer" ? "layer-missing" : mode == "wrong-readable-layer" ? "without-overlap" : mode));
                root.Set("unavailable_tile_spaces", spaces); root.Remove("tile_spaces");
            }
            if (mode == "throw" || mode == "clock" || mode == "clock-throw")
                root.Set("reading_admission_probes", Json.Parse("[{\"id\":\"reading.probe\",\"layer\":\"reading.layer\",\"retention\":\"latest\",\"fresh_for\":null,\"mode\":\"" + mode + "\"}]"));
            var definition = new GameDefinition("reading-admission", new GameModule[] {
                new MotionModule(), new TileNavigationModule(), new CompanyKnowledgeModule(), new WorldSetupModule(), proof });
            var files = new PreloadedFileSource().Add("manifest.json", FrameworkProofContent.Read("reading-spatial-admission-master.json"))
                .Add("reading-spatial-admission.json", root.ToString());
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory f, string scenario, int seats = 1)
        {
            var request = new NewGameRequest { Scenario = scenario, Seed = 629 };
            for (int i = 0; i < seats; i++) request.Seats.Add(Controller.Human); return f.NewGame(request);
        }
        private static GameFactory PhysicalFactory(bool tile, bool half, bool finite = false, bool regional = false)
        {
            if (tile)
            {
                var world = Json.Parse(FrameworkProofContent.Read("native-sensor-world.json"));
                if (half) world.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
                return LiveSensorTests.Factory(world: world.ToString());
            }
            var root = Json.Parse(PointGame.Definitions);
            var scenario = root.Get("scenarios")[0]; scenario.Set("seats", JsonValue.Number("2"));
            scenario.Get("modules").Add(JsonValue.Of(CompanyKnowledgeModule.ModuleId));
            scenario.Get("modules").Add(JsonValue.Of("proof.reading-spatial-admission"));
            root.Get("entities")[2].Get("components").Get("point_presence").Set("position", JsonValue.Number("4"));
            StartupContentAuthoring.Position(root, "machine", JsonValue.Number("4"));
            if (half) root.Get("time_definitions")[0].Get("quantum").Set("numerator", JsonValue.Number("30"));
            var reading = Json.Parse(FrameworkProofContent.Read("reading-spatial-point.json"));
            var declaration = Json.Parse(FrameworkProofContent.Read("reading-spatial-point-module.json"));
            if (finite)
            {
                // Select one subject declaration, not a runtime override/duplicate catalog entry.
                // The existing physical activities/effects/material requirements remain unchanged.
                reading.Remove("point_readings");
                declaration.Get("documents").Add(JsonValue.Of("reading-spatial-freshness.json"));
                scenario.Get("modules").Add(JsonValue.Of(KnowledgeVisibilityModule.ModuleId));
                scenario.Get("settings").Set(KnowledgeVisibilityModule.ModuleId, Json.Parse(regional
                    ? "{\"policy\":\"reading.point-region-sight\"}" : "{\"policy\":\"reading.point-sight\"}"));
            }
            return PointGame.Factory(root.ToString(), layerDefinitions: FrameworkProofContent.Read("point-layers.json"),
                additionalModule: new ReadingSpatialAdmissionModule(),
                moduleDeclaration: declaration.ToString(), moduleDocument: reading.ToString(),
                extraModules: finite ? new GameModule[] { new KnowledgeVisibilityModule() } : null,
                extraDeclarations: finite ? "[{\"id\":\"sim.knowledge-visibility\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.knowledge\",\"version\":\"1.0.0\"}],\"documents\":[]}]" : null,
                extraDocuments: finite ? new Dictionary<string, string> { ["reading-spatial-freshness.json"] = FrameworkProofContent.Read("reading-spatial-freshness.json") } : null);
        }
        private static EntityId Entity(SimGame g, string name) => g.Seat(0).Snapshot.Entities.Single(x => x.Def == name).Id;
        private static KnowledgeView Knowledge(SimGame g, int seat = 0) => g.Seat(seat).View.Feature<KnowledgeView>();
        private static bool Known(SimGame g, string subject) => Knowledge(g).TryRead(new PlayerId(0), Provider, subject, out _);
        private static KnowledgeObservation Read(SimGame g, string subject)
        {
            if (!Knowledge(g).TryRead(new PlayerId(0), Provider, subject, out var observation)) throw new InvalidOperationException("missing actual earned reading");
            return observation;
        }
        private static Fix Stock(SimGame g, EntityId id, string resource)
        {
            var facts = new List<Facet>(); g.Seat(0).View.Inspect(id, facts);
            return facts.Where(x => x.Key == Storage.FacetStock && x.Subject == resource).Aggregate(Fix.Zero, (sum, x) => sum + x.Value);
        }
        private static List<ActivityNodeView> Nodes(SimGame g, int order)
        { var rows = new List<ActivityNodeView>(); g.Seat(0).View.Feature<WorkView>().TryCopyNodes(order, rows); return rows; }
        private static void Step(Check c, SimGame g, SimGame twin = null)
        {
            g.Update(); twin?.Update();
            if (g.Fault != null || twin?.Fault != null) throw new InvalidOperationException("spatial reading fault " + g.Fault + "/" + twin?.Fault);
            if (twin != null) c.Equal(g.Hash(), twin.Hash(), "actual saved reading continuation matches every Update");
        }
        private static void Until(Check c, SimGame g, SimGame twin, int order, Func<bool> done, string stage)
        {
            var trail = new Queue<string>();
            for (int i = 0; i < 512 && !done(); i++)
            {
                trail.Enqueue(g.Clock.StepIndex + ":nodes=" + string.Join(";", Nodes(g, order).Select(x => x.Id + "/" + x.State + "/" + x.Progress.WholeTicks + "/" + x.Blocker)));
                if (trail.Count > 8) trail.Dequeue(); Step(c, g, twin);
            }
            if (!done()) throw new InvalidOperationException(stage + " exceeds512Updates " + string.Join(" | ", trail));
            Console.WriteLine("reading stage: " + stage + " at=" + g.Clock.StepIndex);
        }
        private static void Physical(Check c, bool tile, bool half)
        {
            Console.WriteLine("reading physical: tile=" + tile + " half=" + half);
            var f = PhysicalFactory(tile, half); var g = tile ? LiveSensorTests.New(f) : New(f, "points", 2);
            var idle = tile ? LiveSensorTests.New(f) : New(f, "points", 2); ulong isolation = idle.Hash();
            string subject = tile ? "survey.reading" : "reading.point", activity = tile ? "survey.sample" : "reading.point-sample",
                refresh = tile ? "survey.reobserve" : "reading.point-refresh", resource = tile ? "steel" : "supply";
            EntityId target = Entity(g, tile ? "survey.site" : "source"), machine = Entity(g, tile ? "survey.machine" : "machine");
            var before = g.Seat(0).Snapshot.Entities.Single(x => x.Id == machine).Spatial;
            c.Equal(Fix.FromInt(20), Stock(g, target, resource), "finite authored observation inputs");
            c.That(!Known(g, subject) && !g.Modules.Contains(TycoonModule.ModuleId), "no free learned sample or mandatory business");
            var limits = new ReplayLimits(4 * 1024 * 1024, 6000); using var recording = g.RecordReplay(limits);
            int order = checked((int)PermanentRemovalTests.Submit(c, g, new PostOrderCommand { Plan = activity, Target = target, Priority = 60 }).Arg0);
            c.That(!Known(g, subject), "normal accepted observation requires physical work and time");
            Until(c, g, null, order, () => Nodes(g, order).Any(x => x.Progress.WholeTicks > 0 && x.Progress.WholeTicks < x.RequiredTicks), "partial sample work");
            c.That(!Known(g, subject) && !before.Matches(g.Seat(0).Snapshot.Entities.Single(x => x.Id == machine).Spatial), "machine physically approaches and partially works before learning");
            var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "actual partial sample checkpoint");
            g.Clock.Pause(); twin.Clock.Pause(); ulong paused = g.Hash(); long time = g.Clock.StepIndex;
            Step(c, g, twin); c.That(g.Hash() == paused && g.Clock.StepIndex == time && !Known(g, subject), "sole paused clock preserves unfinished physical observation");
            g.Clock.Resume(); twin.Clock.Resume();
            Until(c, g, twin, order, () => Known(g, subject), "earned sample");
            var retained = Read(g, subject); string payload = retained.Data.ToString();
            c.That(retained.Company == new PlayerId(0), "actual learned observation is company-owned");
            c.Throws<InvalidOperationException>(() => retained.Data.Set("value_raw", JsonValue.Number("0")), "actual detached learned payload rejects mutation");
            c.Equal(Fix.FromInt(tile ? 31 : 0).Raw, new JsonReader(retained.Data, "earned sample").Long("value_raw"), "captured actual typed layer sample, never invented data");
            c.Equal(Fix.FromInt(12), Stock(g, target, resource), "exactly eight actual materials consumed once for physical sample");
            c.That(!Knowledge(g, 1).TryRead(new PlayerId(0), Provider, subject, out _), "another company cannot read private learned sample");
            c.Equal(Code.None, Knowledge(g).ReadFreshness(new PlayerId(0), Provider, subject, out var freshness), "freshness derives from sole clock");
            c.That(freshness.IsFresh, "authored permanent reading remains fresh");
            f.Load(g.Save());
            int next = checked((int)PermanentRemovalTests.Submit(c, g, new PostOrderCommand { Plan = refresh, Target = target, Priority = 60 }).Arg0);
            twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "second normal observation saved while pending");
            Until(c, g, twin, next, () => Read(g, subject).ObservedTick > retained.ObservedTick, "earned refresh");
            c.Equal(payload, retained.Data.ToString(), "retained old fact is detached from later observations");
            c.Equal(Fix.FromInt(12), Stock(g, target, resource), "explicit no-input refresh does not repeat first consumption");
            c.Equal(isolation, idle.Hash(), "interleaved session has no shared layer/knowledge state");
            var replay = new ReplayVerifier().Verify(f, recording.Finish(), limits);
            c.That(replay.Success && replay.Game != null && replay.Game.Hash() == g.Hash(), "physical sample/refresh replay " + replay.Failure + "/" + replay.Detail);
        }
        private static void Run(Check c)
        {
            c.That(typeof(TileReadingDef).Assembly.GetType("Beep.SimEngine.Standard.SpatialReadingDef") == null, "obsolete reading base removed, no compatibility alias");
            c.That(typeof(TileReadingDef).BaseType == typeof(Def) && typeof(PointReadingDef).BaseType == typeof(Def), "native/independent readings implement direct contracts");
            foreach (string mode in new[] { "missing-provider", "missing-layer", "wrong-readable-layer", "throw", "clock", "clock-throw" })
            {
                var probe = new ReadingSpatialAdmissionModule(); Exception error = null;
                try { New(Admission(probe, mode), "reading-admission"); } catch (Exception actual) { error = actual; }
                c.That(error != null && (mode == "throw" || mode == "clock" || mode == "clock-throw" ? error is InvalidOperationException : error is ContentException), "invalid unused reading refuses " + mode + "/" + error);
                if (mode == "wrong-readable-layer") c.That(error?.Message.Contains("readable tile layer") == true, "metadata-only actual layer cannot substitute native reading geometry");
                if (mode == "throw" || mode == "clock-throw") c.Equal("proof.reading-admission-first-error", error?.Message, "first actual callback error preserved instead of finally masking");
                c.Equal(0, probe.Initialize.Initializations, "invalid unused reading rejects before Initialize");
            }
            foreach (string mode in new[] { "valid", "foreign" })
            {
                var probe = new ReadingSpatialAdmissionModule(); var f = Admission(probe, mode); var g = New(f, "reading-admission");
                c.Equal(1, probe.Initialize.Initializations, "valid selected/foreign reading initializes");
                c.That(g.Seat(0).Snapshot.Entities.Count == 0 && (g.Observer.Space == null) == (mode == "foreign"), "unused definitions fabricate neither entities nor unselected map");
                var twin = f.Load(g.Save()); c.Equal(g.Hash(), twin.Hash(), "valid empty selected/nonspatial reading checkpoint"); Step(c, g, twin);
            }
            foreach (bool tile in new[] { true, false }) foreach (bool half in new[] { false, true }) Physical(c, tile, half);
        }
    }
}
