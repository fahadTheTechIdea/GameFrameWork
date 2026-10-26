using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static class ContactExplorationTests
    {
        internal static void Register() => TestRunner.Add("Contact exploration: physical regional discovery and saved sensor dwell", Run);
        private static readonly Code Source = new Code("contact.source"), Sensor = new Code("survey.sensor");
        private static readonly PlayerId Company = new PlayerId(0);
        internal static JsonValue WorldDocument(bool exploration = true)
        {
            var world = Content.Json.Parse(FrameworkProofContent.Read("native-sensor-world.json"));
            var scenario = world.Get("scenarios")[0]; scenario.Get("settings").Get(KnowledgeVisibilityModule.ModuleId).Set("policy", JsonValue.Of("contact.sight"));
            if (exploration)
            {
                scenario.Get("modules").Add(JsonValue.Of(ExplorationModule.ModuleId));
                world.Get("entities")[1].Get("components").Set("authored_exploration", Content.Json.Parse("{\"id\":\"contact.source\",\"sensor\":\"survey.sensor\",\"profile\":\"contact.profile\"}"));
            }
            return world;
        }
        internal static GameFactory Factory(JsonValue profiles = null, JsonValue world = null, bool exploration = true,
            JsonValue sharing = null, JsonValue reading = null, JsonValue forgetting = null, JsonValue masterDocument = null,
            IDictionary<string, string> additionalDocuments = null, IReadOnlyList<GameModule> additionalModules = null)
        {
            var master = masterDocument ?? Content.Json.Parse(FrameworkProofContent.Read("native-sensor-master.json")); var modules = master.Get("modules");
            for (int i = 0; i < modules.Count; i++)
            {
                string id = modules[i].Get("id").Text;
                if (id == CompanyKnowledgeModule.ModuleId) modules[i].Get("documents").Add(JsonValue.Of("native-contact-exploration-knowledge.json"));
                if (id == CompanyKnowledgeModule.ModuleId && sharing != null) modules[i].Get("documents").Add(JsonValue.Of("native-knowledge-sharing.json"));
                if (id == CompanyKnowledgeModule.ModuleId && forgetting != null) modules[i].Get("documents").Add(JsonValue.Of("native-knowledge-forgetting.json"));
                if (id == WorldSetupModule.ModuleId) modules[i].Get("documents").Add(JsonValue.Of("native-contact-exploration-regions.json"));
                if (id == KnowledgeVisibilityModule.ModuleId) modules[i].Get("documents").Add(JsonValue.Of("native-contact-exploration-visibility.json"));
            }
            if (exploration) modules.Add(Content.Json.Parse("{\"id\":\"sim.exploration\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.knowledge\",\"version\":\"1.0.0\"}],\"documents\":[\"native-contact-exploration-profiles.json\"]}"));
            var documents = new Dictionary<string, string> {
                    ["native-contact-exploration-profiles.json"] = (profiles ?? Content.Json.Parse(FrameworkProofContent.Read("native-contact-exploration-profiles.json"))).ToString(),
                    ["native-contact-exploration-regions.json"] = FrameworkProofContent.Read("native-contact-exploration-regions.json"),
                    ["native-contact-exploration-knowledge.json"] = FrameworkProofContent.Read("native-contact-exploration-knowledge.json"),
                    ["native-contact-exploration-visibility.json"] = FrameworkProofContent.Read("native-contact-exploration-visibility.json") };
            if (sharing != null) documents.Add("native-knowledge-sharing.json", sharing.ToString());
            if (reading != null) documents["native-survey-knowledge.json"] = reading.ToString();
            if (forgetting != null) documents.Add("native-knowledge-forgetting.json", forgetting.ToString());
            if (additionalDocuments != null) foreach (var doc in additionalDocuments) documents[doc.Key] = doc.Value;
            var selected = new List<GameModule>(); if (exploration) selected.Add(new ExplorationModule());
            if (additionalModules != null) selected.AddRange(additionalModules);
            return LiveSensorTests.Factory(world: (world ?? WorldDocument(exploration)).ToString(), master: master.ToString(),
                additionalModules: selected, additionalDocuments: documents);
        }
        private static Entity Machine(SimGame game) => game.World.Entities.All.Single(e => e.Def.Id == "survey.machine");
        private static ExplorationSnapshot Read(SimGame game)
        {
            Code code = game.Seat(0).View.Feature<ExplorationView>().Read(Machine(game).Id, Source, out var snapshot);
            if (!code.IsNone) throw new InvalidOperationException("exploration read " + code); return snapshot;
        }
        private static ExplorationEntrySnapshot Entry(SimGame game, string id) => Read(game).Entries.Single(e => e.Id == id);
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("contact time=" + game.Tick + " " + game.Fault);
            if (twin != null) Update(twin);
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 1000 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " position=" + Machine(game).Get<ITilePresence>().Tile
                + " entries=" + string.Join(";", Read(game).Entries.Select(e => e.Id + "/" + e.Progress + "/" + e.Blocker)));
        }
        private static void Control(Check c, SimGame game, bool enabled, SimGame twin = null)
        {
            var command = new ConfigureOperationCommand { Entity = Machine(game).Id, Operation = Sensor, Enabled = enabled, Target = Fix.FromInt(2) };
            ulong hash = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "normal actual sensor control preview");
            c.Equal(hash, game.Hash(), "sensor preview leaves discovery/progress unchanged");
            c.That(TestKit.Order(game, 0, command).Accepted, "normal operation command controls real sensor");
            if (twin != null) c.That(TestKit.Order(twin, 0, command).Accepted, "same ordinary sensor command in restored session");
        }
        private static void Move(Check c, SimGame game, TileCoord destination)
        {
            var command = new MoveVehicleCommand { Vehicle = Machine(game).Id, To = destination };
            c.Equal(Code.None, game.Seat(0).Preview(command), "normal movement admission");
            c.That(TestKit.Order(game, 0, command).Accepted, "normal player movement, no exploration movement owner");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "new exploration simulation identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "new optional owners do not change existing layouts");
            var factory = Factory(); var game = LiveSensorTests.New(factory); var machine = Machine(game);
            var view = game.Seat(0).View.Feature<ExplorationView>(); var map = game.Seat(0).View.Feature<TileMapView>();
            var firstCell = new TileRect(8, 10, 1, 1); var secondCell = new TileRect(14, 10, 1, 1); var values = new List<int>();
            c.That(machine.Get<IExplorationSource>(Source) is AuthoredExploration, "native source owns unfinished observations");
            c.That(game.World.Content.Get<AuthoredExplorationProfileDef>("contact.profile").AuthoringPath.Contains("native-contact-exploration-profiles.json"), "actual master-selected shared profile");
            c.Equal(2, Read(game).Entries.Count, "only explicit authored entries");
            c.Equal("a.reset", Read(game).Entries[0].Id, "canonical stable entry order");
            c.Equal(ExplorationCodes.Outside, Entry(game, "a.reset").Blocker, "initial physical source is outside contact region");
            c.Equal(0L, Entry(game, "a.reset").Progress.WholeTicks, "no free initial progress or reveal");
            c.Equal(SpatialLayerReadStatus.Unavailable, map.ReadLayer("survey.terrain", firstCell, values), "unvisited region samples are unavailable");
            c.That(!game.World.Services.Has<IEconomy>(), "exploration has no business requirement");
            c.Throws<InvalidOperationException>(() => machine.Get<IExplorationSource>(Source).AdvanceExploration(default), "forged/default update context cannot advance source");
            var snapshots = new List<ExplorationSnapshot>(); view.CopySources(snapshots); c.Equal(1, snapshots.Count, "owned provider list");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<ExplorationView>().Read(machine.Id, Source, out var hidden), "foreign diagnostics refuse");
            c.That(hidden == null, "no foreign policy/progress/subject facts");
            game.Seat(1).View.Feature<ExplorationView>().CopySources(snapshots); c.Equal(0, snapshots.Count, "foreign copy clears prior owned facts");
            c.Equal(ExplorationCodes.Unsupported, view.Read(machine.Id, new Code("unknown.source"), out hidden), "missing actual capability is typed unsupported");
            var frozen = Read(game); c.Throws<NotSupportedException>(() => ((IList<ExplorationEntrySnapshot>)frozen.Entries).Clear(), "detached entry list is immutable");
            ulong hash = game.Hash(); Read(game); view.CopySources(snapshots); map.ReadLayer("survey.terrain", firstCell, values);
            c.Equal(hash, game.Hash(), "diagnostics/sample reads do not advance or learn");
            c.That(TestKit.Order(game, 0, new SetWorkerHeldCommand { Worker = machine.Id, Held = true }).Accepted, "normal worker hold prevents automatic home orders");
            Control(c, game, false); Move(c, game, new TileCoord(8, 10));
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "ordinary travelling source checkpoint");
            Reach(game, () => machine.Get<IMotion>().Status == MoveStatus.Arrived, "actual contact travel", twin);
            c.Equal(new TileCoord(8, 10), machine.Get<ITilePresence>().Tile, "actual machine reached first region");
            c.Equal(game.Hash(), twin.Hash(), "saved physical movement continues identically");
            c.That(!Entry(game, "a.reset").Known && Entry(game, "a.reset").Progress.Equals(WorkProgress.Zero), "disabled actual sensor grants no observation during travel");
            c.Equal(ExplorationCodes.NoSensor, Entry(game, "a.reset").Blocker, "inside region still requires current actual sensor coverage");
            int unseenVersion = map.Version; Control(c, game, true);
            var partial = Entry(game, "a.reset");
            c.Equal(3L, partial.RequiredSteps, "150 seconds rounds upward through existing actual minute time");
            c.Equal(1L, partial.Progress.WholeTicks, "one actual interval of observation");
            c.Equal(new Code("exploring"), machine.State, "Entity.Update settles authored actual sampling state");
            c.Equal(unseenVersion, map.Version, "unfinished private progress does not reveal region through map cursor");
            c.Equal(SpatialLayerReadStatus.Unavailable, map.ReadLayer("survey.terrain", firstCell, values), "partial contact still unseen");
            twin = factory.Load(game.Save()); game.Clock.Pause(); twin.Clock.Pause(); long clock = game.Tick;
            for (int i = 0; i < 3; i++) Update(game, twin);
            c.Equal(clock, game.Tick, "pause advances no alternate clock");
            c.Equal(1L, Entry(game, "a.reset").Progress.WholeTicks, "paused partial observation remains exact");
            c.Equal(game.Hash(), twin.Hash(), "paused partial restored sessions identical");
            game.Clock.Resume(); twin.Clock.Resume(); Control(c, game, false, twin);
            c.Equal(0L, Entry(game, "a.reset").Progress.WholeTicks, "explicit reset interruption clears unfinished dwell");
            c.That(!Entry(game, "a.reset").Known, "reset does not grant knowledge");
            c.Equal(1L, partial.Progress.WholeTicks, "held partial facts remain detached");
            c.Equal(game.Hash(), twin.Hash(), "reset policy saved continuation identical");
            Control(c, game, true, twin); Update(game, twin);
            c.That(!Entry(game, "a.reset").Known && Entry(game, "a.reset").Progress.WholeTicks == 2, "last unfinished discovery interval");
            twin = factory.Load(game.Save()); Update(game, twin);
            c.That(Entry(game, "a.reset").Known && Entry(game, "a.reset").Progress.Equals(WorkProgress.Zero), "completion records only in existing knowledge ledger");
            c.Equal(game.Hash(), twin.Hash(), "saved discovery completion identical");
            c.That(map.Version > unseenVersion, "original regional visibility scope invalidates when known");
            c.Equal(SpatialLayerReadStatus.Success, map.ReadLayer("survey.terrain", firstCell, values), "learned region exposes actual native sample");
            c.Equal(0, values[0], "actual authored/default native terrain sample, not an invented observation value");
            var page = map.ReadLayerPage("survey.terrain", firstCell, new SpatialLayerPageRequest(0, 1), values);
            c.Equal(SpatialLayerReadStatus.Unavailable, map.ReadLayer("survey.terrain", new TileRect(6, 10, 3, 1), values), "mixed visited/unvisited region refuses atomically");
            c.Equal(0, values.Count, "mixed region emits no prefix");
            c.Equal(SpatialLayerReadStatus.Unavailable, game.Seat(1).View.Feature<TileMapView>().ReadLayer("survey.terrain", firstCell, values), "other company does not borrow discovered regions");
            var knowledge = game.Seat(0).View.Feature<KnowledgeView>();
            c.Equal(Code.None, knowledge.ReadFreshness(Company, "sim.knowledge.facts", "contact.north.known", out var remembered), "earned native permanent freshness metadata");
            c.That(remembered.IsFresh && !remembered.FreshFor.HasValue, "explicit-null discovery really is permanent after earning");
            c.That(!knowledge.TryRead(Company, "sim.knowledge.facts", "survey.reading", out _), "contact geography never substitutes seismic/geological survey");
            c.Equal(SpatialLayerReadStatus.Unavailable, map.ReadLayer("survey.deposit", new TileRect(16, 10, 1, 1), new List<Fix>()), "region discovery does not bypass geological reading rule");

            Control(c, game, false); Move(c, game, new TileCoord(14, 10)); twin = factory.Load(game.Save());
            Reach(game, () => machine.Get<IMotion>().Status == MoveStatus.Arrived, "physical painted region travel", twin);
            c.Equal(new TileCoord(14, 10), machine.Get<ITilePresence>().Tile, "actual source inside painted region");
            c.Equal(game.Hash(), twin.Hash(), "known history and second travel restore identically");
            c.Equal(SpatialLayerReadStatus.Success, map.ReadLayer("survey.terrain", firstCell, values), "departed discovery persists, not current sensor sight");
            c.Equal(SpatialLayerReadStatus.Unavailable, map.ReadLayer("survey.terrain", secondCell, values), "second region is independently unearned");
            Control(c, game, true); Update(game);
            c.Equal(2L, Entry(game, "b.retain").Progress.WholeTicks, "independent second positive observation duration");
            Control(c, game, false);
            c.Equal(2L, Entry(game, "b.retain").Progress.WholeTicks, "explicit retain interruption preserves unfinished dwell");
            c.Equal(ExplorationCodes.NoSensor, Entry(game, "b.retain").Blocker, "retained progress is not current readiness");
            twin = factory.Load(game.Save()); for (int i = 0; i < 3; i++) Update(game, twin);
            c.That(!Entry(game, "b.retain").Known && Entry(game, "b.retain").Progress.WholeTicks == 2, "blocked dwell cannot finish by elapsed wall/game time alone");
            c.Equal(game.Hash(), twin.Hash(), "retained interruption saves exact original component state");
            Control(c, game, true, twin); Update(game, twin);
            c.Equal(4L, Entry(game, "b.retain").Progress.WholeTicks, "resumed actual observation retains earlier intervals");
            twin = factory.Load(game.Save()); Update(game, twin);
            c.That(Entry(game, "b.retain").Known && Entry(game, "b.retain").Progress.Equals(WorkProgress.Zero), "second discovery completes through same ledger");
            c.Equal(game.Hash(), twin.Hash(), "second saved completion identical");
            c.Equal(SpatialLayerReadStatus.Success, map.ReadLayer("survey.terrain", new TileRect(14, 10, 1, 2), values), "painted declared cells known together");
            c.Equal(2, values.Count, "exact painted membership, not bounding rectangle expansion");
            c.Equal(SpatialLayerReadStatus.Invalidated, map.ReadLayerPage("survey.terrain", firstCell, new SpatialLayerPageRequest(0, 1, page.Cursor), values).Status, "second knowledge membership invalidates retained cursor");
            c.Equal(Code.None, knowledge.ReadFreshness(Company, "sim.knowledge.facts", "contact.north.known", out var older), "first discovery still readable after long physical travel");
            c.That(older.IsFresh && older.AgeSteps > remembered.AgeSteps && older.Observation.ObservedTick == remembered.Observation.ObservedTick, "permanent native fact ages without expiry or repeated capture");
            c.Equal(Fix.FromInt(20), game.World.Entities.All.Single(e => e.Def.Id == "survey.site").Get<IInventory>().Amount(game.World.Content.ResourceId("steel")), "passive sensor contact neither consumes nor fabricates survey stock");
            c.Equal(0, game.World.Services.Get<WorkBoard>().Orders.Count, "contact collector does not create a duplicate work scheduler/queue");
            var rows = new List<KnowledgeObservation>(); knowledge.CopyKnown(rows); c.Equal(2, rows.Count, "one original row per learned subject");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed contact history checkpoint");
            var isolated = LiveSensorTests.New(factory); c.That(!Entry(isolated, "a.reset").Known && Entry(isolated, "a.reset").Progress.Equals(WorkProgress.Zero), "interleaved session borrows neither history nor dwell");
            var absentModule = LiveSensorTests.New(Factory(exploration: false));
            c.That(absentModule.Seat(0).View.Feature<ExplorationView>() == null && Machine(absentModule).Get<IExplorationSource>(Source) == null, "whole exploration mechanism remains optional");
            Invalid(c);
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "missing-duration", "zero", "negative", "interruption", "empty", "duplicate", "region", "subject", "latest", "overflow", "state" })
            {
                var profiles = Content.Json.Parse(FrameworkProofContent.Read("native-contact-exploration-profiles.json")); var profile = profiles.Get("exploration_profiles")[0]; var entry = profile.Get("entries")[0];
                if (mode == "missing-duration") entry.Remove("duration");
                if (mode == "zero") entry.Get("duration").Set("numerator", JsonValue.Number("0"));
                if (mode == "negative") entry.Get("duration").Set("numerator", JsonValue.Number("-1"));
                if (mode == "interruption") entry.Set("interruption", JsonValue.Of("guess"));
                if (mode == "empty") profile.Set("entries", JsonValue.NewArray());
                if (mode == "duplicate") profile.Get("entries").Add(entry.Clone());
                if (mode == "region") entry.Set("region", JsonValue.Of("missing"));
                if (mode == "subject") entry.Set("subject", JsonValue.Of("missing"));
                if (mode == "latest") entry.Set("subject", JsonValue.Of("survey.reading"));
                if (mode == "overflow") { entry.Get("duration").Set("numerator", JsonValue.Number("9223372036854775807")); entry.Get("duration").Set("unit", JsonValue.Of("day")); }
                if (mode == "state") profile.Get("states").Set("sampling", JsonValue.Of("missing"));
                c.Throws<ContentException>(() => LiveSensorTests.New(Factory(profiles)), "invalid authored contact policy " + mode);
            }
            foreach (string mode in new[] { "sensor", "profile", "presence" })
            {
                var world = WorldDocument(); var components = world.Get("entities")[1].Get("components");
                if (mode == "sensor") components.Get("authored_exploration").Set("sensor", JsonValue.Of("missing.sensor"));
                if (mode == "profile") components.Get("authored_exploration").Set("profile", JsonValue.Of("missing.profile"));
                if (mode == "presence") components.Get("tile_sensor").Set("space", JsonValue.Of("missing.space"));
                c.Throws<ContentException>(() => LiveSensorTests.New(Factory(world: world)), "invalid actual source binding " + mode);
            }
        }
    }
}
