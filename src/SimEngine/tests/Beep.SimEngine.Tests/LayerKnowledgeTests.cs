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
using static Beep.SimEngine.Tests.EmergencyPreemptionTests;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    internal static class LayerKnowledgeTests
    {
        private static string Facts => FrameworkProofContent.Read("layer-discovery-facts.json");
        private static GameFactory Points(string visibility = null)
            => TechnologyTests.DiscoveryFactory(facts: Facts,
                visibility: visibility ?? FrameworkProofContent.Read("layer-knowledge.json"),
                additionalDocuments: new Dictionary<string, string> { ["discovery-layers.json"] = FrameworkProofContent.Read("point-layers.json") });
        private static GameFactory Tiles(string visibility = null)
        {
            JsonValue root = Content.Json.Parse(FrameworkProofContent.Read("spatial-observer.json"));
            JsonValue scenario = root.Get("scenarios")[0];
            scenario.Get("modules").Add(JsonValue.Of(CompanyKnowledgeModule.ModuleId));
            scenario.Get("modules").Add(JsonValue.Of(KnowledgeVisibilityModule.ModuleId));
            scenario.Set("settings", Content.Json.Parse("{\"sim.knowledge-visibility\":{\"policy\":\"tile.discovery\"}}"));
            return SpatialLayerTests.Factory(root.ToString(), Facts, visibility ?? FrameworkProofContent.Read("tile-layer-knowledge.json"));
        }
        private static void Reach(SimGame game, Func<bool> done, SimGame twin, string stage)
        {
            for (int i = 0; i < 900 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done()) throw new InvalidOperationException(stage + " time=" + game.Tick + " orders=" +
                string.Join(";", Board(game).Orders.Select(o => o.Id + "/" + o.State + "/" + o.Blocker)));
        }
        internal static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "layer discovery changes authoritative visibility semantics");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "exact identity only; no legacy save reader");
            c.That(!typeof(IPersistentService).IsAssignableFrom(typeof(KnowledgeVisibility)), "visibility retains no second learned state codec");
            PointDiscovery(c);
            TileProtocol(c);
            JsonValue invalid = Content.Json.Parse(FrameworkProofContent.Read("layer-knowledge.json"));
            invalid.Get("layer_knowledge_visibility")[0].Set("definition", JsonValue.Of("missing"));
            c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(Points(invalid.ToString())), "unknown authored layer refuses composition");
            invalid = Content.Json.Parse(FrameworkProofContent.Read("layer-knowledge.json"));
            invalid.Get("layer_knowledge_visibility")[0].Set("facts", Content.Json.Parse("[\"missing\"]"));
            c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(Points(invalid.ToString())), "unknown authored fact refuses composition");
        }
        private static void PointDiscovery(Check c)
        {
            GameFactory factory = Points(); SimGame game = ConstructionPlanTests.NewPoints(factory);
            var space = (PointSpace)game.World.Space;
            var mine = game.Seat(0).View.Feature<PointLayerView>(); var other = game.Seat(1).View.Feature<PointLayerView>();
            var observer = game.Observer.Feature<PointLayerView>();
            c.That(game.World.Content.Get<LayerKnowledgeVisibilityDef>("visibility.terrain").AuthoringPath.Contains("visibility.json"), "authored layer rules use selected master policy document");
            c.That(game.World.Content.Get<SpatialLayerDef>("point.terrain").AuthoringPath.Contains("discovery-layers.json"), "layers use explicit nonduplicated master-listed document");
            c.That(mine.Layers.Count == 0 && other.Layers.Count == 0 && observer.Layers.Count == 3, "hidden public/private layers expose no metadata to companies");
            // Engine-side provider fixture writes are explicitly observer setup, not player discovery grants.
            space.AssignOwner(game.World, "point.deposit", new PlayerId(0));
            c.Equal(Code.None, space.WriteLayer(game.World, PlayerId.None, "point.terrain", Fix.One, Fix.FromInt(3)), "actual hidden public sample setup");
            c.Equal(Code.None, space.WriteLayer(game.World, PlayerId.None, "point.deposit", Fix.One, Fix.FromInt(5)), "actual hidden private sample setup");
            var region = new PointRegion(Fix.One, Fix.One, 2); var values = new List<Fix> { Fix.One };
            int before = mine.Version, foreign = other.Version;
            ulong hash = game.Hash();
            c.Equal(SpatialLayerReadStatus.Unavailable, mine.ReadLayer("point.terrain", region, values), "public does not mean discovered");
            c.Equal(0, values.Count, "refused regional read clears retained output");
            SpatialLayerPageResult page = mine.ReadLayerPage("point.deposit", new PointRegion(Fix.FromInt(-1), Fix.Zero, -1), new SpatialLayerPageRequest(-1, 0), values);
            c.That(page.Status == SpatialLayerReadStatus.Unavailable && page.Cursor == null && page.TotalCount == 0, "hidden page refuses before geometry/type/version disclosure");
            var write = new WritePointLayerCommand { Layer = "point.terrain", Position = Fix.One, Value = Fix.FromInt(9) };
            c.Equal(Codes.NotFound, game.Seat(0).Preview(write), "unknown public write preview refuses");
            c.Equal(Codes.NotFound, game.Seat(0).Preview(new AssignPointLayerCommand { Layer = "point.deposit", Owner = new PlayerId(0) }), "ownership alone is not learned-layer admission");
            c.Equal(Codes.NotFound, game.Seat(0).Preview(new AssignPointLayerCommand { Layer = "missing", Owner = new PlayerId(0) }), "missing and hidden assignment have identical refusal");
            c.Equal(hash, game.Hash(), "all hidden previews/queries are pure");
            c.Equal(Codes.NotFound, TestKit.Order(game, 0, write).Refusal, "ordinary player command uses same hidden refusal");
            c.That(mine.Version == before && other.Version == foreign, "hidden writes/ownership/refused commands do not change company cursors");
            c.Equal(SpatialLayerReadStatus.Success, observer.ReadLayer("point.terrain", region, values), "observer reads actual setup sample");
            c.Equal(Fix.FromInt(3), values[0], "refused player write changed no sample");
            var retained = mine.Layers;
            Configure(c, game);
            CommandResult result = TestKit.Order(game, 0, new PostOrderCommand { Plan = "field.discover", Target = Find(game, "source").Id, Priority = 60 });
            c.That(result.Accepted, "player orders existing engine physical discovery activity");
            Order order = Board(game).Order((int)result.Arg0);
            Reach(game, () => order.Node("sample").Progress.WholeTicks > 0, null, "partial sample");
            c.That(mine.Layers.Count == 0 && mine.Version == before, "partial machine work does not reveal a layer");
            SimGame twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "partially worked discovery and layer values restore");
            var twinView = twin.Seat(0).View.Feature<PointLayerView>();
            Reach(game, () => mine.Layers.Count == 2, twin, "physical whole-layer discovery");
            c.Equal(game.Hash(), twin.Hash(), "earned visibility and real work continuation remain identical");
            c.That(mine.Layers.Count == 2 && twinView.Layers.Count == 2 && other.Layers.Count == 0 && retained.Count == 0, "whole-layer learning is private and retained facts detached");
            c.Equal(before + 1, mine.Version, "new readable membership invalidates exactly once");
            c.Equal(foreign, other.Version, "other company's cursor does not reveal learning");
            c.Equal(SpatialLayerReadStatus.Success, mine.ReadLayer("point.deposit", region, values), "knowledge plus ownership admits actual deposit");
            c.Equal(Fix.FromInt(5), values[0], "discovery returns stored quantity, not a fabricated grant");
            c.Equal(Fix.FromInt(12), Find(game, "source").Get<IInventory>().Amount(Supply(game)), "physical discovery consumed eight stored materials");
            page = mine.ReadLayerPage("point.terrain", region, new SpatialLayerPageRequest(0, 1), values);
            c.Equal(SpatialLayerReadStatus.Success, page.Status, "known layer has bounded page cursor");
            before = mine.Version;
            c.Equal(Code.None, space.WriteLayer(game.World, PlayerId.None, "point.water", Fix.One, Fix.FromInt(8)), "other undiscovered layer changes real stored value");
            c.That(mine.Version == before && other.Version == foreign, "hidden public layer still changes no company cursor after discovery");
            c.Equal(SpatialLayerReadStatus.Success, mine.ReadLayerPage("point.terrain", region, new SpatialLayerPageRequest(1, 1, page.Cursor), values).Status, "hidden change does not invalidate readable page");
            int sequence = game.Seat(0).Submit(new WritePointLayerCommand { Layer = "point.terrain", Position = Fix.One, Value = Fix.FromInt(7) });
            twin = factory.Load(game.Save()); Tick(game); Tick(twin);
            c.That(sequence > 0 && game.Hash() == twin.Hash(), "known ordinary queued sample command restores and applies identically");
            c.Equal(before + 1, mine.Version, "known sample write changes its company cursor");
            c.Equal(foreign, other.Version, "unknown other company sees no known public write invalidation");
            c.Equal(SpatialLayerReadStatus.Invalidated, mine.ReadLayerPage("point.terrain", region, new SpatialLayerPageRequest(1, 1, page.Cursor), values).Status, "known change invalidates an old continuation");
            before = mine.Version; Tick(game);
            c.Equal(before, mine.Version, "stable visibility refresh does not continually invalidate");
            SimGame restored = factory.Load(game.Save());
            c.That(restored.Seat(0).View.Feature<PointLayerView>().Layers.Count == 2 && restored.Seat(1).View.Feature<PointLayerView>().Layers.Count == 0, "post-load refresh happens after learned ledger restore");
            c.Equal(game.Hash(), restored.Hash(), "derived cursors add no saved/hash state");
            c.Equal(0, ConstructionPlanTests.NewPoints(factory).Seat(0).View.Feature<PointLayerView>().Layers.Count, "another session remains undiscovered");
            var invalid = new PointLayerView(space, new PlayerId(9));
            c.That(invalid.Layers.Count == 0 && invalid.Version == 0, "positive nonseat direct provider view receives no metadata or cursor");
            c.Equal(SpatialLayerReadStatus.Unavailable, invalid.ReadLayer("point.terrain", region, values), "nonseat cannot read discovered public sample");
        }
        private static void TileProtocol(Check c)
        {
            GameFactory factory = Tiles(); SimGame game = factory.NewGame(new NewGameRequest { Scenario = "layers", Seed = 19 });
            var map = (TileMap)game.World.Space; var mine = game.Seat(0).View.Feature<TileMapView>();
            var other = game.Seat(1).View.Feature<TileMapView>(); var observer = game.Observer.Feature<TileMapView>();
            var tile = new TileCoord(69999, 69999); var area = new TileRect(tile, 1, 1); var integers = new List<int>();
            // Native layer/knowledge protocol instrumentation only; physical earning is the point workflow above.
            map.AssignOwner(game.World, "observer.private", new PlayerId(0));
            int before = mine.Version, foreign = other.Version;
            var baseline = mine.ReadChanges().Cursor;
            map.Layer<IntLayer>("observer.public").Set(tile, 17);
            map.Layer<FixedLayer>("observer.private").Set(tile, Fix.FromInt(4));
            c.That(mine.Layers.Count == 0 && mine.LayerIds.Count == 0 && other.Layers.Count == 0 && observer.Layers.Count == 2, "native hidden metadata respects authored visibility");
            c.That(mine.Version == before && other.Version == foreign && map.ChunkVersionFor(new PlayerId(0), 4374, 4374) == 0, "hidden native public/private writes expose no cursor/chunk");
            SpatialRegionChanges<TileRect> delta = mine.ReadChanges(baseline);
            c.That(!delta.FullInvalidation && delta.ChangedRegions.Count == 0, "hidden native changes do not disclose chunk activity");
            c.Equal(SpatialLayerReadStatus.Unavailable, mine.ReadLayer("observer.public", area, integers), "native public sample remains undiscovered");
            ulong hash = game.Hash(); mine.ReadChanges(baseline); _ = mine.Layers;
            c.Equal(hash, game.Hash(), "native visibility/delta queries remain pure");
            Learn(c, game, 0, "field.second"); Tick(game);
            c.That(mine.Layers.Count == 1 && mine.LayerIds[0] == "observer.public", "any-fact native rule reveals only matching layer");
            c.Equal(before + 1, mine.Version, "native reveal increments viewer-local cursor once");
            delta = mine.ReadChanges(baseline);
            c.That(delta.FullInvalidation && delta.ChangedRegions.Count == 0, "reveal requests re-read without enumerating hidden chunks");
            c.Equal(SpatialLayerReadStatus.Success, mine.ReadLayer("observer.public", area, integers), "actual public tile revealed");
            c.Equal(17, integers[0], "actual wide sparse tile value retained");
            SpatialLayerPageResult page = mine.ReadLayerPage("observer.public", area, new SpatialLayerPageRequest(0, 1), integers);
            before = mine.Version;
            map.Layer<FixedLayer>("observer.private").Set(tile, Fix.FromInt(5));
            c.That(mine.Version == before && other.Version == foreign, "owned but unknown private tile does not invalidate public cursor");
            c.Equal(SpatialLayerReadStatus.Success, mine.ReadLayerPage("observer.public", area, new SpatialLayerPageRequest(0, 1, page.Cursor), integers).Status, "native hidden write preserves public page");
            Learn(c, game, 0, "field.observed"); Tick(game);
            c.Equal(before + 1, mine.Version, "second native layer reveal invalidates once");
            c.Equal(SpatialLayerReadStatus.Invalidated, mine.ReadLayerPage("observer.public", area, new SpatialLayerPageRequest(0, 1, page.Cursor), integers).Status, "new visible membership invalidates earlier page");
            before = mine.Version; baseline = mine.ReadChanges().Cursor; map.Layer<IntLayer>("observer.public").Set(tile, 18);
            c.That(mine.Version == before + 1 && other.Version == foreign, "native visible public write invalidates only known company");
            delta = mine.ReadChanges(baseline);
            c.That(!delta.FullInvalidation && delta.ChangedRegions.Count == 1 && delta.ChangedRegions[0].X == 69984, "known write produces actual bounded native chunk");
            c.That(map.VersionFor(new PlayerId(9)) == 0 && map.ChunkVersionFor(new PlayerId(9), 4374, 4374) == 0, "native positive nonseat cursors deny");
            SimGame restored = factory.Load(game.Save());
            c.Equal(game.Hash(), restored.Hash(), "native sparse layer and sole ledger restore without saved cursors");
            c.That(restored.Seat(0).View.Feature<TileMapView>().Layers.Count == 2 && restored.Seat(1).View.Feature<TileMapView>().Layers.Count == 0, "native load visibility refresh reads restored company facts");
            before = mine.Version; Tick(game); Tick(restored);
            c.That(game.Hash() == restored.Hash() && mine.Version == before, "native saved continuation and stable refresh remain identical");
            JsonValue policy = Content.Json.Parse(FrameworkProofContent.Read("tile-layer-knowledge.json"));
            policy.Get("knowledge_visibility_policies")[0].Set("unlisted_content", JsonValue.Of(false));
            SimGame deniedLinks = Tiles(policy.ToString()).NewGame(new NewGameRequest { Scenario = "layers" });
            Learn(c, deniedLinks, 0, "field.observed"); Tick(deniedLinks);
            c.Equal(0, deniedLinks.Seat(0).View.Feature<TileMapView>().Layers.Count, "visible layer rule cannot leak independently hidden linked unit/resource metadata");
        }
        private static void Learn(Check c, SimGame game, int seat, string id)
        {
            KnowledgeFactDef fact = game.World.Content.Get<KnowledgeFactDef>(id);
            c.Equal(Code.None, game.World.Services.Get<ICompanyKnowledge>().Record(new PlayerId(seat), "sim.knowledge.facts", id, fact.Value), "explicit native knowledge protocol fixture records actual authored fact");
        }
    }
}
