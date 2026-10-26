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
    internal static class KnowledgeFreshnessTests
    {
        internal static void Register() => TestRunner.Add("Knowledge freshness: physical survey expiry and exact saved visibility", Run);
        private const string Provider = "sim.knowledge.facts", Subject = "survey.reading";
        private static readonly PlayerId Company = new PlayerId(0);
        private static JsonValue ReadingDocument()
        {
            var document = Content.Json.Parse(FrameworkProofContent.Read("native-survey-knowledge.json"));
            document.Get("tile_readings")[0].Set("fresh_for", Content.Json.Parse("{\"numerator\":150,\"denominator\":1,\"unit\":\"second\"}"));
            return document;
        }
        private static GameFactory Factory(JsonValue knowledge = null, JsonValue visibility = null)
        {
            var master = Content.Json.Parse(FrameworkProofContent.Read("native-sensor-master.json"));
            var modules = master.Get("modules");
            for (int i = 0; i < modules.Count; i++)
                if (modules[i].Get("id").Text == KnowledgeVisibilityModule.ModuleId)
                    modules[i].Get("documents").Add(JsonValue.Of("native-fresh-survey-visibility.json"));
            var policy = Content.Json.Parse(FrameworkProofContent.Read("native-sensor-visibility.json"));
            policy.Get("knowledge_visibility_policies")[0].Set("rules", Content.Json.Parse("[\"survey.deposit.fresh\"]"));
            return LiveSensorTests.Factory(visibility: policy.ToString(), master: master.ToString(), additionalDocuments: new Dictionary<string, string> {
                ["native-survey-knowledge.json"] = (knowledge ?? ReadingDocument()).ToString(),
                ["native-fresh-survey-visibility.json"] = (visibility ?? Content.Json.Parse(FrameworkProofContent.Read("native-fresh-survey-visibility.json"))).ToString() });
        }
        private static Entity Find(SimGame game, string id) => game.World.Entities.All.Single(e => e.Def.Id == id);
        private static KnowledgeView Knowledge(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<KnowledgeView>();
        private static TileMapView MapView(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<TileMapView>();
        private static KnowledgeFreshnessSnapshot Reading(SimGame game)
        {
            Code code = Knowledge(game).ReadFreshness(Company, Provider, Subject, out var snapshot);
            if (!code.IsNone) throw new InvalidOperationException("freshness refusal " + code); return snapshot;
        }
        private static void Update(SimGame game, SimGame twin = null)
        {
            game.Update(); if (game.Fault != null) throw new InvalidOperationException("freshness time=" + game.Tick + " " + game.Fault);
            if (twin != null) Update(twin);
        }
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 500 && !done(); i++) Update(game, twin);
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " position=" + Find(game, "survey.machine").Get<ITilePresence>().Tile
                + " orders=" + string.Join(";", game.World.Services.Get<WorkBoard>().Orders.Select(o => o.Id + "/" + o.State + "/" + o.Blocker)));
        }
        private static Order Start(Check c, SimGame game, string plan)
        {
            var command = new PostOrderCommand { Plan = plan, Target = Find(game, "survey.site").Id, Priority = 60 };
            ulong before = game.Hash(); c.Equal(Code.None, game.Seat(0).Preview(command), "ordinary work preview");
            c.Equal(before, game.Hash(), "preview changes no history/clock/state");
            var result = TestKit.Order(game, 0, command); c.That(result.Accepted, "normal player survey command");
            return game.World.Services.Get<WorkBoard>().Order((int)result.Arg0);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "new simulation identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no parallel persistence layout");
            var factory = Factory(); var game = LiveSensorTests.New(factory);
            var ledger = game.World.Services.Get<ICompanyKnowledge>();
            c.That(ReferenceEquals(ledger, game.World.Services.Get<IKnowledgeFreshness>()), "same sole learned ledger has freshness alias");
            c.Equal(3, ((CompanyKnowledgeService)ledger).StateSchemaVersion, "record payload unchanged, acquired old observation semantics explicit");
            c.That(!game.World.Services.Has<IEconomy>(), "physical knowledge does not require Tycoon");
            var view = MapView(game); var field = new TileRect(16, 10, 1, 1); var values = new List<Fix>();
            c.Equal(Codes.NotFound, Knowledge(game).ReadFreshness(Company, Provider, Subject, out var absent), "unobserved is not fresh by default");
            c.That(absent == null, "unobserved response has no facts");
            c.Equal(SpatialLayerReadStatus.Unavailable, view.ReadLayer("survey.deposit", field, values), "no unearned geological sample visibility");
            var first = Start(c, game, "survey.sample");
            Reach(game, () => first.Node("sample").Progress.WholeTicks > 0, "physical survey work");
            c.That(Find(game, "survey.machine").Get<ITilePresence>().Tile != new TileCoord(2, 10), "native machine actually travelled");
            c.Equal(Fix.FromInt(12), Find(game, "survey.site").Get<IInventory>().Amount(game.World.Content.ResourceId("steel")), "eight real materials consumed");
            c.That(!Knowledge(game).TryRead(Company, Provider, Subject, out _), "partial work grants no knowledge");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial physical checkpoint");
            Reach(game, () => first.State == OrderState.Done, "completed physical survey", twin);
            c.Equal(game.Hash(), twin.Hash(), "same physical work and capture after load");
            var fresh = Reading(game); var original = fresh.Observation;
            c.Equal(3, original.SchemaVersion, "native authored freshness semantics provider version");
            c.That(fresh.IsFresh && fresh.FreshFor.HasValue, "completed observation has authored freshness");
            c.Equal(3L, fresh.FreshSteps.Value, "150 seconds rounds upward to three native minute intervals");
            c.Equal(1L, fresh.AgeSteps, "existing interval-start capture includes completion interval");
            c.Equal(1L, game.World.Time.ExactTicks(fresh.Age), "age duration uses actual session quantum");
            c.Equal(SpatialLayerReadStatus.Success, view.ReadLayer("survey.deposit", field, values), "fresh exact sample authorized");
            c.Equal(Fix.FromInt(31), values[0], "actual current deposit sample");
            c.Equal(SpatialLayerReadStatus.Unavailable, view.ReadLayer("survey.deposit", new TileRect(15, 10, 2, 1), values), "fresh one cell never authorizes surrounding rectangle");
            c.Equal(0, values.Count, "mixed samples return no prefix");
            c.Equal(SpatialLayerReadStatus.Unavailable, MapView(game, 1).ReadLayer("survey.deposit", field, values), "foreign coverage does not bypass knowledge or owner");
            c.Equal(Codes.NotFound, Knowledge(game, 1).ReadFreshness(Company, Provider, Subject, out absent), "private freshness concealed");
            c.That(absent == null, "foreign response has no metadata");
            var wide = new TileSpatialSnapshot("survey.tiles", new TileCoord(16, 10), new TileRect(16, 10, 2, 1), default, default, default, false, false);
            c.That(!game.World.Content.Get<TileReadingDef>(Subject).CoversSample(wide), "contract refuses wide footprint containing survey point");
            var page = view.ReadLayerPage("survey.deposit", field, new SpatialLayerPageRequest(0, 1), values);
            c.Equal(SpatialLayerReadStatus.Success, page.Status, "fresh native page");
            page = FreshBulk(c, factory, game, field, page);
            ulong hash = game.Hash(); Reading(game); view.ReadLayer("survey.deposit", field, values);
            c.Equal(hash, game.Hash(), "freshness and sample reads are pure");
            c.Throws<InvalidOperationException>(() => original.Data.Set("value_raw", JsonValue.Number("99")), "historical payload immutable");

            twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "fresh observation checkpoint contains original ledger only");
            game.Clock.Pause(); twin.Clock.Pause(); long clock = game.Tick;
            for (int i = 0; i < 3; i++) Update(game, twin);
            c.Equal(clock, game.Tick, "paused sole clock does not age survey");
            c.Equal(fresh.AgeSteps, Reading(game).AgeSteps, "pause does not expire knowledge");
            c.That(Reading(game).IsFresh, "paused freshness retained");
            c.Equal(game.Hash(), twin.Hash(), "paused restored sessions identical");
            game.Clock.Resume(); twin.Clock.Resume(); Update(game, twin);
            c.That(Reading(game).IsFresh && Reading(game).AgeSteps == 2, "last fresh interval");
            c.Equal(SpatialLayerReadStatus.Invalidated, view.ReadLayerPage("survey.deposit", field, new SpatialLayerPageRequest(0, 1, page.Cursor), values).Status, "clock dependency invalidates old page");
            int beforeExpiry = view.Version; Update(game, twin);
            var stale = Reading(game);
            c.That(!stale.IsFresh && stale.AgeSteps == 3, "exact terminal expiry uses sole clock");
            c.That(view.Version > beforeExpiry, "same expiry Update invalidates presenter cursor");
            c.Equal(SpatialLayerReadStatus.Unavailable, view.ReadLayer("survey.deposit", field, values), "expired historical reading is not live authorization");
            c.Equal(0, values.Count, "expired sample output cleared");
            c.That(Knowledge(game).TryRead(Company, Provider, Subject, out var remembered), "expiry never deletes history");
            c.Equal(original.ObservedTick, remembered.ObservedTick, "expiry neither refreshes nor rewrites observation");
            c.That(fresh.IsFresh && fresh.AgeSteps == 1, "held freshness snapshot remains detached");
            c.Equal(game.Hash(), twin.Hash(), "expiry saved continuation identical");
            StaleBulk(c, game, twin, field, page);
            twin = factory.Load(game.Save()); c.That(!Reading(twin).IsFresh, "restore derives stale status from same clock");
            c.Equal(SpatialLayerReadStatus.Unavailable, MapView(twin).ReadLayer("survey.deposit", field, values), "restore does not resurrect expired sight");

            // Native layer mutation is explicit provider protocol instrumentation, not a player edit command.
            ((TileMap)game.World.Space).Layer<FixedLayer>("survey.deposit").Set(new TileCoord(16, 10), Fix.FromInt(7));
            c.Equal(Fix.FromInt(31).Raw, new JsonReader(Reading(game).Observation.Data, "remembered").Long("value_raw"), "actual environment change cannot rewrite historical reading");
            var refresh = Start(c, game, "survey.reobserve");
            Reach(game, () => refresh.Node("sample").Progress.WholeTicks > 0, "physical resurvey begins");
            c.Equal(original.ObservedTick, Reading(game).Observation.ObservedTick, "partial resurvey retains stale chronology");
            twin = factory.Load(game.Save()); Reach(game, () => refresh.State == OrderState.Done, "physical resurvey completes", twin);
            c.Equal(game.Hash(), twin.Hash(), "partial refresh saved continuation identical");
            var latest = Reading(game);
            c.That(latest.IsFresh && latest.Observation.ObservedTick > original.ObservedTick, "completed physical work alone refreshes sight");
            c.Equal(original.LearnedTick, latest.Observation.LearnedTick, "first learning retained");
            c.Equal(Fix.FromInt(7).Raw, new JsonReader(latest.Observation.Data, "latest").Long("value_raw"), "resurvey captures actual changed deposit");
            c.Equal(Fix.FromInt(31).Raw, new JsonReader(original.Data, "held").Long("value_raw"), "held original remains historical");
            ((TileMap)game.World.Space).Layer<FixedLayer>("survey.deposit").Set(new TileCoord(16, 10), Fix.FromInt(9));
            c.Equal(SpatialLayerReadStatus.Success, view.ReadLayer("survey.deposit", field, values), "fresh rule authorizes live sample, not cached value");
            c.Equal(Fix.FromInt(9), values[0], "authorized current sample differs from historical sample");
            c.Equal(Fix.FromInt(7).Raw, new JsonReader(Reading(game).Observation.Data, "latest").Long("value_raw"), "live read does not refresh history");
            var cancelled = Start(c, game, "survey.reobserve");
            Reach(game, () => cancelled.Node("sample").Progress.WholeTicks > 0, "cancelled physical resurvey");
            c.That(TestKit.Order(game, 0, new CancelOrderCommand { Order = cancelled.Id }).Accepted, "normal player cancellation");
            c.Equal(latest.Observation.ObservedTick, Reading(game).Observation.ObservedTick, "cancelled work grants no refreshed time");
            var rows = new List<KnowledgeObservation>(); Knowledge(game).CopyKnown(rows);
            c.Equal(1, rows.Count, "one original knowledge record, not another fog/expiry table");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "stale cancelled work preserves original checkpoint semantics");
            var isolated = LiveSensorTests.New(factory);
            c.Equal(Codes.NotFound, Knowledge(isolated).ReadFreshness(Company, Provider, Subject, out absent), "independent session borrows no record or lifetime");
            Invalid(c);
        }
        private static SpatialLayerPageResult FreshBulk(Check c, GameFactory factory, SimGame game,
            TileRect field, SpatialLayerPageResult retained)
        {
            // Explicit native mutation protocol, NOT player resource generation. Sight itself was
            // earned by the original supplied, travelling, timed survey above. No second workflow.
            var map = (TileMap)game.World.Space;
            var layer = map.Layer<FixedLayer>("survey.deposit");
            var mine = MapView(game); var other = MapView(game, 1);
            var observer = game.Observer.Feature<TileMapView>();
            var near = new TileCoord(17, 10); var edge = new TileCoord(47, 31);
            var mixed = new TileRect(16, 10, 2, 1);
            var values = new List<Fix> { Fix.One };
            int a = mine.Version, b = other.Version, global = map.Version, actual = layer.Version,
                observed = observer.Version;
            var baseline = mine.ReadChanges().Cursor; var foreignBaseline = other.ReadChanges().Cursor;
            var observerBaseline = observer.ReadChanges().Cursor;
            long at = game.Clock.StepIndex;
            layer.Fill((x, y) => x == near.X && y == near.Y ? Fix.FromInt(13)
                : x == edge.X && y == edge.Y ? Fix.FromInt(29) : layer[x, y]);
            c.That(layer[near] == Fix.FromInt(13) && layer[edge] == Fix.FromInt(29), "hidden bulk edits actually change near and distant native cells");
            c.That(map.Version == global + 1 && layer.Version == actual + 1 && observer.Version == observed + 1,
                "one hidden bulk commit updates original layer/map/exact observer once");
            c.That(mine.Version == a && other.Version == b, "fresh one-cell knowledge leaks neither adjacent nor remote bulk changes");
            var delta = mine.ReadChanges(baseline); var foreign = other.ReadChanges(foreignBaseline);
            c.That(!delta.FullInvalidation && delta.ChangedRegions.Count == 0 && !foreign.FullInvalidation && foreign.ChangedRegions.Count == 0,
                "hidden fresh bulk changes disclose no company chunk or full invalidation");
            var observerDelta = observer.ReadChanges(observerBaseline);
            c.That(!observerDelta.FullInvalidation && observerDelta.ChangedRegions.Count == 2,
                "exact observer sees the two genuinely modified chunks");
            c.Equal(SpatialLayerReadStatus.Success, mine.ReadLayerPage("survey.deposit", field,
                new SpatialLayerPageRequest(0, 1, retained.Cursor), values).Status, "hidden bulk edits preserve retained visible page");
            c.That(values.Count == 1 && values[0] == Fix.FromInt(31), "retained visible page returns original live sample");
            var slice = mine.ReadLayerPage("survey.deposit", mixed, new SpatialLayerPageRequest(0, 1), values);
            c.That(slice.Status == SpatialLayerReadStatus.Success && slice.TotalCount == 2 && values.Count == 1 && values[0] == Fix.FromInt(31),
                "bounded first page authorizes only the earned cell, not the entire requested rectangle");
            var refused = mine.ReadLayerPage("survey.deposit", mixed, new SpatialLayerPageRequest(1, 1, slice.Cursor), values);
            c.That(refused.Status == SpatialLayerReadStatus.Unavailable && refused.Count == 0 && refused.TotalCount == 0 && refused.Cursor == null && values.Count == 0,
                "hidden adjacent continuation publishes no value, count or cursor");

            layer.Fill((x, y) => x == field.X && y == field.Y ? Fix.FromInt(32)
                : x == near.X && y == near.Y ? Fix.FromInt(14)
                : x == edge.X && y == edge.Y ? Fix.FromInt(30) : layer[x, y]);
            c.That(mine.Version == a + 1 && other.Version == b && map.Version == global + 2 && layer.Version == actual + 2 && observer.Version == observed + 2,
                "mixed bulk edit invalidates actual owners once without foreign visibility");
            delta = mine.ReadChanges(baseline);
            c.That(!delta.FullInvalidation && delta.ChangedRegions.Count == 1 && delta.ChangedRegions[0].X == 16 && delta.ChangedRegions[0].Y == 0,
                "mixed bulk edit exposes only the genuinely observed chunk");
            c.Equal(SpatialLayerReadStatus.Invalidated, mine.ReadLayerPage("survey.deposit", field,
                new SpatialLayerPageRequest(0, 1, retained.Cursor), values).Status, "visible bulk edit invalidates retained page");
            c.Equal(0, values.Count, "invalidated visible bulk page publishes no prefix");
            refused = mine.ReadLayerPage("survey.deposit", mixed, new SpatialLayerPageRequest(1, 1, slice.Cursor), values);
            c.That(refused.Status == SpatialLayerReadStatus.Unavailable && refused.Cursor == null && refused.TotalCount == 0 && values.Count == 0,
                "hidden sample refusal precedes stale page identity after mixed bulk edit");
            c.Equal(Fix.FromInt(31).Raw, new JsonReader(Reading(game).Observation.Data, "bulk history").Long("value_raw"),
                "bulk edits never rewrite earned historical observation");
            c.Equal(at, game.Clock.StepIndex, "bulk protocol does not advance a second clock");
            var restored = factory.Load(game.Save());
            c.Equal(game.Hash(), restored.Hash(), "fresh bulk values and original earned ledger restore together");
            var restoredLayer = ((TileMap)restored.World.Space).Layer<FixedLayer>("survey.deposit");
            c.That(restoredLayer[new TileCoord(field.X, field.Y)] == Fix.FromInt(32) && restoredLayer[near] == Fix.FromInt(14) && restoredLayer[edge] == Fix.FromInt(30),
                "save keeps all actual bulk values, including company-hidden samples");
            c.Equal(SpatialLayerReadStatus.Success, MapView(restored).ReadLayer("survey.deposit", field, values), "saved fresh exact sample remains authorized");
            c.That(values.Count == 1 && values[0] == Fix.FromInt(32), "saved fresh query reads live bulk value, not historical reading");
            c.Equal(SpatialLayerReadStatus.Unavailable, MapView(restored).ReadLayer("survey.deposit", mixed, values), "restore cannot widen earned coverage to hidden bulk neighbour");
            c.Equal(0, values.Count, "restored mixed query publishes no prefix");
            var current = mine.ReadLayerPage("survey.deposit", field, new SpatialLayerPageRequest(0, 1), values);
            c.Equal(SpatialLayerReadStatus.Success, current.Status, "new page binds actual post-bulk viewer version");
            return current;
        }
        private static void StaleBulk(Check c, SimGame game, SimGame twin, TileRect field, SpatialLayerPageResult retained)
        {
            // Both independently saved sessions receive the same explicit provider mutation.
            // Their expiry is still determined only by the engine's original game clock/ledger.
            foreach (SimGame session in new[] { game, twin })
            {
                var map = (TileMap)session.World.Space; var layer = map.Layer<FixedLayer>("survey.deposit");
                var mine = MapView(session); var other = MapView(session, 1);
                int a = mine.Version, b = other.Version, global = map.Version, actual = layer.Version;
                var baseline = mine.ReadChanges().Cursor;
                layer.Fill((x, y) => x == field.X && y == field.Y ? Fix.FromInt(33)
                    : x == 17 && y == 10 ? Fix.FromInt(15) : x == 47 && y == 31 ? Fix.FromInt(31) : layer[x, y]);
                c.That(map.Version == global + 1 && layer.Version == actual + 1 && layer[new TileCoord(field.X, field.Y)] == Fix.FromInt(33),
                    "expired bulk update commits actual values through original layer owner");
                c.That(mine.Version == a && other.Version == b, "expired learned sample cannot leak later bulk edits through company versions");
                var delta = mine.ReadChanges(baseline);
                c.That(!delta.FullInvalidation && delta.ChangedRegions.Count == 0, "expired bulk edit publishes no company chunk changes");
                var values = new List<Fix> { Fix.One };
                var refused = mine.ReadLayerPage("survey.deposit", field, new SpatialLayerPageRequest(0, 1, retained.Cursor), values);
                c.That(refused.Status == SpatialLayerReadStatus.Unavailable && refused.Count == 0 && refused.TotalCount == 0 && refused.Cursor == null && values.Count == 0,
                    "expired sample refuses before stale cursor disclosure");
                refused = mine.ReadLayerPage("survey.deposit", new TileRect(-1, -1, -1, -1), new SpatialLayerPageRequest(-1, 0), values);
                c.That(refused.Status == SpatialLayerReadStatus.Unavailable && refused.TotalCount == 0 && refused.Cursor == null && values.Count == 0,
                    "expired hidden layer refuses before invalid geometry and page envelope disclosure");
                c.That(mine.Layers.All(x => x.Id != "survey.deposit"), "expired layer metadata remains undisclosed after bulk edits");
                c.Equal(Fix.FromInt(31).Raw, new JsonReader(Reading(session).Observation.Data, "expired bulk history").Long("value_raw"),
                    "expired bulk edits preserve original historical reading");
            }
            c.Equal(game.Hash(), twin.Hash(), "saved sessions agree after expired bulk mutation and scoped queries");
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "missing", "zero", "negative", "denominator", "unit", "first", "overflow" })
            {
                var knowledge = ReadingDocument(); var reading = knowledge.Get("tile_readings")[0]; var duration = reading.Get("fresh_for");
                if (mode == "missing") reading.Remove("fresh_for");
                if (mode == "zero") duration.Set("numerator", JsonValue.Number("0"));
                if (mode == "negative") duration.Set("numerator", JsonValue.Number("-1"));
                if (mode == "denominator") duration.Set("denominator", JsonValue.Number("0"));
                if (mode == "unit") duration.Set("unit", JsonValue.Of("frame"));
                if (mode == "first") reading.Set("retention", JsonValue.Of("first"));
                if (mode == "overflow") { duration.Set("numerator", JsonValue.Number(long.MaxValue.ToString())); duration.Set("unit", JsonValue.Of("day")); }
                c.Throws<ContentException>(() => LiveSensorTests.New(Factory(knowledge: knowledge)), "invalid explicit freshness " + mode);
            }
            foreach (string mode in new[] { "missing", "empty", "duplicate", "wrong-layer", "non-spatial" })
            {
                var visibility = Content.Json.Parse(FrameworkProofContent.Read("native-fresh-survey-visibility.json"));
                var rule = visibility.Get("layer_fresh_knowledge_visibility")[0]; var knowledge = ReadingDocument();
                if (mode == "missing") rule.Set("readings", Content.Json.Parse("[\"missing\"]"));
                if (mode == "empty") rule.Set("readings", Content.Json.Parse("[]"));
                if (mode == "duplicate") rule.Set("readings", Content.Json.Parse("[\"survey.reading\",\"survey.reading\"]"));
                if (mode == "wrong-layer") rule.Set("definition", JsonValue.Of("survey.terrain"));
                if (mode == "non-spatial") { knowledge.Set("knowledge_facts", Content.Json.Parse("[{\"id\":\"survey.fact\",\"fresh_for\":null,\"value\":{\"fact\":true}}]")); rule.Set("readings", Content.Json.Parse("[\"survey.fact\"]")); }
                c.Throws<ContentException>(() => LiveSensorTests.New(Factory(knowledge, visibility)), "invalid spatial freshness rule " + mode);
            }
            var permanent = ReadingDocument(); permanent.Get("tile_readings")[0].Set("fresh_for", JsonValue.Null());
            var game = LiveSensorTests.New(Factory(permanent));
            c.That(!game.World.Content.Get<TileReadingDef>(Subject).FreshFor.HasValue, "explicit null remains permanent authored intent");
            c.Equal(Codes.NotFound, Knowledge(game).ReadFreshness(Company, Provider, Subject, out _), "permanent intent is not unearned knowledge");
        }
    }
}
