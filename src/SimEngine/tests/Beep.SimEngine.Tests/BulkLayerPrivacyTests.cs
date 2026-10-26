using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    public static class BulkLayerPrivacyTests
    {
        private static readonly TileCoord First = new TileCoord(0, 0), Next = new TileCoord(1, 0), Gap = new TileCoord(2, 0), Edge = new TileCoord(17, 16);
        public static void Register()
        {
            TestRunner.Add("Bulk layers: exact sample-private generation and durable cursors", Run);
            TestRunner.Add("Bulk layers: engine generation settles initial scopes before runtime privacy", Generation);
        }
        private static GameFactory Factory(bool visibility = true, Action<WorldComposer> compose = null)
        {
            JsonValue root = Content.Json.Parse(FrameworkProofContent.Read("bulk-layer-world.json"));
            if (!visibility)
            {
                root.Get("scenarios")[0].Set("modules", Content.Json.Parse("[\"layers\"]"));
                root.Get("scenarios")[0].Remove("settings");
            }
            return SpatialLayerTests.Factory(root.ToString(), visibility ? FrameworkProofContent.Read("layer-discovery-facts.json") : null,
                visibility ? FrameworkProofContent.Read("bulk-layer-visibility.json") : null, compose);
        }
        private sealed class InitialLayers : WorldGenStage
        {
            public bool ObservedGeneration;
            public override string Id => "proof.bulk.initial-layers";
            public override void Generate(World world, ScenarioDef scenario)
            {
                ObservedGeneration = world.Generating;
                var map = (TileMap)world.Space;
                map.Layer<ByteLayer>("bulk.byte").Fill((x, y) => (byte)(x == 0 && y == 0 ? 7 : x == 17 && y == 16 ? 11 : 0));
                map.AssignOwner(world, "bulk.private", new PlayerId(0));
                map.Layer<FixedLayer>("bulk.private").Fill((x, y) => x == 0 && y == 0 ? Fix.FromInt(2) : Fix.Zero);
            }
        }
        private static void Generation(Check c)
        {
            var stage = new InitialLayers();
            GameFactory factory = Factory(compose: composer => composer.AddWorldGen(stage, 10));
            SimGame game = New(factory);
            c.That(stage.ObservedGeneration && !game.World.Generating, "only the actual engine generation window defers viewer invalidation");
            c.Equal(0L, game.Tick, "initial generation consumes no game time");
            c.Equal((byte)7, Map(game).Layer<ByteLayer>("bulk.byte")[First], "native first value generated");
            c.Equal((byte)11, Map(game).Layer<ByteLayer>("bulk.byte")[Edge], "native partial-edge chunk generated");
            c.Equal(Fix.FromInt(2), Map(game).Layer<FixedLayer>("bulk.private")[First], "actual private value generated");
            c.Equal(View(game, 0).Version, game.Seat(0).Snapshot.Space.Version, "first published snapshot has the settled viewer baseline");
            var values = new List<int>();
            var hidden = View(game, 0).ReadLayerPage("bulk.byte", new TileRect(First, 1, 1), new SpatialLayerPageRequest(0, 1), values);
            c.That(hidden.Status != SpatialLayerReadStatus.Success || values.Count == 0, "initial generation does not reveal undiscovered sample");
            Learn(c, game, 0, "field.observed");
            var known = View(game, 0).ReadLayerPage("bulk.byte", new TileRect(First, 1, 1), new SpatialLayerPageRequest(0, 1), values);
            c.That(known.Status == SpatialLayerReadStatus.Success && values.Count == 1 && values[0] == 7, "ordinary scope discovery reveals original generated value");
            hidden = View(game, 1).ReadLayerPage("bulk.byte", new TileRect(First, 1, 1), new SpatialLayerPageRequest(0, 1), values);
            c.That(hidden.Status != SpatialLayerReadStatus.Success || values.Count == 0, "another company remains undiscovered");
            int version = View(game, 0).Version;
            var cursor = View(game, 0).ReadChanges().Cursor;
            Map(game).Layer<ByteLayer>("bulk.byte").Set(Gap, 9);
            c.That(View(game, 0).Version == version && View(game, 0).ReadChanges(cursor).ChangedRegions.Count == 0, "runtime hidden write retains exact sample-private invalidation");
            SimGame twin = factory.Load(game.Save());
            c.Equal(game.Hash(), twin.Hash(), "generated native layers and discovered scopes restore exactly");
            Tick(game); Tick(twin); c.Equal(game.Hash(), twin.Hash(), "saved continuation remains deterministic");
            c.That(!game.World.Generating && !twin.World.Generating, "generation phase is not serialized as an active runtime mode");
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "layers", Seed = 31 });
        private static TileMap Map(SimGame game) => (TileMap)game.World.Space;
        private static TileMapView View(SimGame game, int seat) => game.Seat(seat).View.Feature<TileMapView>();
        private static void Tick(SimGame game)
        { game.Update(); if (game.Fault != null) throw new InvalidOperationException("native bulk continuation time=" + game.Tick + " fault=" + game.Fault); }
        private static void Learn(Check c, SimGame game, int seat, string factId)
        {
            // Native protocol instrumentation, NOT a normal player discovery grant.
            KnowledgeFactDef fact = game.World.Content.Get<KnowledgeFactDef>(factId);
            c.Equal(Code.None, game.World.Services.Get<ICompanyKnowledge>().Record(new PlayerId(seat), "sim.knowledge.facts", factId, fact.Value), "actual authored regional protocol fact");
            Tick(game);
        }
        private static SimGame Prepared(Check c, GameFactory factory)
        {
            SimGame game = New(factory); Map(game).AssignOwner(game.World, "bulk.private", new PlayerId(0));
            Learn(c, game, 0, "field.observed"); Learn(c, game, 1, "field.second"); return game;
        }
        private static string Cursors(SimGame game, MapLayer layer)
        {
            TileMap map = Map(game);
            return layer.Version + ":" + map.Version + ":" + View(game, 0).Version + ":" + View(game, 1).Version + ":" +
                map.ChunkVersion(0, 0) + ":" + map.ChunkVersion(1, 1) + ":" +
                map.ChunkVersionFor(new PlayerId(0), 0, 0) + ":" + map.ChunkVersionFor(new PlayerId(1), 1, 1);
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "native bulk semantics have exact identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no legacy save reader");
            GameFactory factory = Factory(); SimGame game = Prepared(c, factory);
            c.That(game.World.Content.Get<LayerRegionKnowledgeVisibilityDef>("bulk.byte.regions").AuthoringPath.Contains("tile-layer-knowledge.json"), "actual master-listed visibility document");
            c.That(game.World.Content.Get<SpatialLayerDef>("bulk.byte").AuthoringPath.Contains("layers.json"), "actual master-listed generation world");
            c.That(!game.World.Services.Has<WorkBoard>() && !game.World.Services.Has<ITechnologyKnowledge>(), "native generation needs no worker, technology or business subsystem");
            Exercise(c, factory, game, Map(game).Layer<ByteLayer>("bulk.byte"), n => (byte)n);
            Exercise(c, factory, game, Map(game).Layer<IntLayer>("bulk.int"), n => n);
            Exercise(c, factory, game, Map(game).Layer<FixedLayer>("bulk.fixed"), n => Fix.FromInt(n));
            PrivateOwnership(c, factory, game); Refusals(c, factory); WithoutVisibility(c);
            SimGame fresh = New(factory);
            c.That(View(fresh, 0).Layers.Count == 0 && Map(fresh).Layer<IntLayer>("bulk.int")[First] == 0, "new interleaved session borrows no values/knowledge/cursors");
        }
        private static void Exercise<T>(Check c, GameFactory factory, SimGame game, MapLayer<T> layer, Func<int, T> value) where T : struct
        {
            TileMap map = Map(game); var mine = View(game, 0); var other = View(game, 1); string id = layer.Id;
            layer.Fill((x, y) => x == First.X && y == First.Y ? value(1) : x == Edge.X && y == Edge.Y ? value(2) : x == Gap.X && y == Gap.Y ? value(3) : default);
            c.Equal(value(1), layer[First], id + " actual generated first value"); c.Equal(value(2), layer[Edge], id + " actual partial-edge chunk value");
            int a = mine.Version, b = other.Version, global = map.Version, version = layer.Version;
            var baselineA = mine.ReadChanges().Cursor; var baselineB = other.ReadChanges().Cursor;
            var prefix = new List<int>(); var fixedPrefix = new List<Fix>();
            SpatialLayerPageResult held = layer is FixedLayer
                ? mine.ReadLayerPage(id, new TileRect(First, 1, 1), new SpatialLayerPageRequest(0, 1), fixedPrefix)
                : mine.ReadLayerPage(id, new TileRect(First, 1, 1), new SpatialLayerPageRequest(0, 1), prefix);
            c.Equal(SpatialLayerReadStatus.Success, held.Status, id + " known page captured");
            // Regenerate unchanged visible values while changing ONLY an unknown cell in same chunk.
            layer.Fill((x, y) => x == Gap.X && y == Gap.Y ? value(4) : layer[x, y]);
            c.That(map.Version == global + 1 && layer.Version == version + 1 && mine.Version == a && other.Version == b, id + " hidden-only replacement changes only actual observer/layer cursors");
            c.Equal(value(4), layer[Gap], id + " hidden value actually changed");
            c.That(mine.ReadChanges(baselineA).ChangedRegions.Count == 0 && !mine.ReadChanges(baselineA).FullInvalidation, id + " hidden same-chunk bulk change leaks no delta");
            SpatialLayerPageResult resumed = layer is FixedLayer
                ? mine.ReadLayerPage(id, new TileRect(First, 1, 1), new SpatialLayerPageRequest(0, 1, held.Cursor), fixedPrefix)
                : mine.ReadLayerPage(id, new TileRect(First, 1, 1), new SpatialLayerPageRequest(0, 1, held.Cursor), prefix);
            c.Equal(SpatialLayerReadStatus.Success, resumed.Status, id + " hidden regeneration preserves retained known page");
            ulong hash = game.Hash(); string cursors = Cursors(game, layer);
            layer.Fill((x, y) => layer[x, y]);
            c.Equal(cursors, Cursors(game, layer), id + " identical replacement invalidates nothing");
            c.Equal(hash, game.Hash(), id + " identical regeneration changes no authoritative value");
            global = map.Version; version = layer.Version;
            layer.Fill((x, y) => x == First.X && y == First.Y ? value(5) : x == Next.X && y == Next.Y ? value(6) : x == Edge.X && y == Edge.Y ? value(7) : x == Gap.X && y == Gap.Y ? value(8) : layer[x, y]);
            c.That(mine.Version == a + 1 && other.Version == b + 1 && map.Version == global + 1 && layer.Version == version + 1, id + " mixed bulk commit increments EACH affected viewer and actual owner once");
            var deltaA = mine.ReadChanges(baselineA); var deltaB = other.ReadChanges(baselineB);
            c.That(!deltaA.FullInvalidation && deltaA.ChangedRegions.Count == 1 && deltaA.ChangedRegions[0].X == 0 && deltaA.ChangedRegions[0].Y == 0, id + " first company sees only changed known chunk");
            c.That(!deltaB.FullInvalidation && deltaB.ChangedRegions.Count == 1 && deltaB.ChangedRegions[0].X == 16 && deltaB.ChangedRegions[0].Y == 16, id + " second company sees only its partial-edge known chunk");
            resumed = layer is FixedLayer
                ? mine.ReadLayerPage(id, new TileRect(First, 1, 1), new SpatialLayerPageRequest(0, 1, held.Cursor), fixedPrefix)
                : mine.ReadLayerPage(id, new TileRect(First, 1, 1), new SpatialLayerPageRequest(0, 1, held.Cursor), prefix);
            c.Equal(SpatialLayerReadStatus.Invalidated, resumed.Status, id + " visible replacement invalidates old page");
            a = mine.Version; b = other.Version;
            layer.Fill((x, y) => x == Gap.X && y == Gap.Y ? default : layer[x, y]);
            c.That(mine.Version == a && other.Version == b && EqualityComparer<T>.Default.Equals(layer[Gap], default), id + " hidden erasure removes actual value without disclosure");
            layer.Fill((x, y) => x == First.X && y == First.Y ? default : layer[x, y]);
            c.That(mine.Version == a + 1 && other.Version == b && EqualityComparer<T>.Default.Equals(layer[First], default), id + " known erasure invalidates only actual observer");
            a = mine.Version; b = other.Version;
            layer.Set(Next, value(9));
            c.That(mine.Version == a + 1 && other.Version == b && layer[Next].Equals(value(9)), id + " Set uses same exact preparation/publication owner");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), id + " sparse replacements and learned scopes restore");
            MapLayer<T> twinLayer = Map(twin).Layer<MapLayer<T>>(id);
            c.That(twinLayer[Next].Equals(value(9)) && twinLayer[Edge].Equals(value(7)) && twinLayer[Gap].Equals(default(T)), id + " restored generated/erased/edge values are exact");
            layer.Fill((x, y) => x == Edge.X && y == Edge.Y ? value(10) : layer[x, y]);
            twinLayer.Fill((x, y) => x == Edge.X && y == Edge.Y ? value(10) : twinLayer[x, y]);
            Tick(game); Tick(twin); c.Equal(game.Hash(), twin.Hash(), id + " saved future regeneration and Update continue identically");
        }
        private static void PrivateOwnership(Check c, GameFactory factory, SimGame game)
        {
            var map = Map(game); var layer = map.Layer<FixedLayer>("bulk.private"); int a = View(game, 0).Version, b = View(game, 1).Version;
            layer.Fill((x, y) => x == Edge.X && y == Edge.Y ? Fix.FromInt(8) : Fix.Zero);
            c.That(View(game, 0).Version == a && View(game, 1).Version == b, "private hidden edge regeneration belongs to observer only, despite foreign edge knowledge");
            layer.Fill((x, y) => x == First.X && y == First.Y ? Fix.FromInt(5) : layer[x, y]);
            c.That(View(game, 0).Version == a + 1 && View(game, 1).Version == b, "private known regeneration invalidates only actual owner");
            map.AssignOwner(game.World, "bulk.private", new PlayerId(1));
            a = View(game, 0).Version; b = View(game, 1).Version;
            layer.Fill((x, y) => x == First.X && y == First.Y ? Fix.FromInt(6) : layer[x, y]);
            c.That(View(game, 0).Version == a && View(game, 1).Version == b, "former-owner known area is hidden to new owner's different coverage");
            layer.Fill((x, y) => x == Edge.X && y == Edge.Y ? Fix.FromInt(9) : layer[x, y]);
            c.That(View(game, 0).Version == a && View(game, 1).Version == b + 1, "current owner and actual known region govern private bulk invalidation");
            var list = new List<Fix> { Fix.One };
            c.Equal(SpatialLayerReadStatus.Unavailable, View(game, 0).ReadLayer("bulk.private", new TileRect(First, 1, 1), list), "private former owner cannot read regenerated value");
            c.Equal(0, list.Count, "private refusal clears retained values");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "private ownership/regeneration restores actual material and coverage");
        }
        private static void Counter(TileMap map, string field, int value, int? company = null)
        {
            // Explicit framework counter fault injection. Not a gameplay/provider/host capability.
            object target = map;
            if (company.HasValue) target = ((IDictionary)typeof(TileMap).GetField("_privateChanges", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(map))[new PlayerId(company.Value)];
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).SetValue(target, value);
        }
        private static void Refusals(Check c, GameFactory factory)
        {
            SimGame game = Prepared(c, factory); var map = Map(game); var layer = map.Layer<IntLayer>("bulk.int"); layer.Set(First, 5);
            ulong hash = game.Hash(); string before = Cursors(game, layer);
            c.Throws<InvalidOperationException>(() => layer.Fill((x, y) => x == 2 && y == 1 ? throw new InvalidOperationException("generator refusal") : 7), "partial generator refuses before candidate publication");
            c.Throws<InvalidOperationException>(() => layer.Fill((x, y) => { layer.Set(First, 7); return 7; }), "same-layer Set from generator refuses");
            c.Throws<InvalidOperationException>(() => layer.Fill((x, y) => { layer.Fill((a, b) => 7); return 7; }), "recursive generator refuses");
            c.Throws<InvalidOperationException>(() => layer.Fill((x, y) => { layer.Sync(new StateHasher()); return 7; }), "generation cannot checkpoint partial values");
            c.Equal(hash, game.Hash(), "generator failures preserve actual authoritative values"); c.Equal(before, Cursors(game, layer), "generator failures preserve every cursor");
            var quantity = map.Layer<FixedLayer>("bulk.fixed");
            c.Throws<ArgumentException>(() => quantity.Fill((x, y) => x == 2 && y == 1 ? Fix.FromInt(-1) : Fix.FromInt(7)), "invalid generated material refuses before quantity commit");
            c.Equal(hash, game.Hash(), "invalid material preserves all original layers");
            foreach (string fault in new[] { "layer", "map", "public", "affected-company" })
            {
                game = Prepared(c, factory); map = Map(game); layer = map.Layer<IntLayer>("bulk.int"); layer.Set(First, 5);
                if (fault == "layer") layer.Version = int.MaxValue;
                else if (fault == "map") Counter(map, "_version", int.MaxValue);
                else if (fault == "public") Counter(map, "_publicVersion", int.MaxValue);
                else Counter(map, "Version", int.MaxValue, 0);
                before = Cursors(game, layer); hash = game.Hash();
                layer.Fill((x, y) => layer[x, y]);
                c.Equal(before, Cursors(game, layer), fault + " exhausted counter does not reject or invalidate same-value generation");
                c.Throws<OverflowException>(() => layer.Fill((x, y) => x == First.X && y == First.Y ? 6 : layer[x, y]), fault + " affected current counter refuses before native value publication");
                c.Equal(5, layer[First], fault + " prior native value retained"); c.Equal(before, Cursors(game, layer), fault + " failed preflight retains all cursors");
                c.Equal(hash, game.Hash(), fault + " failed preflight retains actual hash"); c.Equal(hash, factory.Load(game.Save()).Hash(), fault + " transient counter exhaustion does not corrupt durable values");
            }
            game = Prepared(c, factory); map = Map(game); layer = map.Layer<IntLayer>("bulk.int"); layer.Set(First, 5);
            Counter(map, "Version", int.MaxValue, 0); int foreign = View(game, 1).Version;
            layer.Fill((x, y) => x == Gap.X && y == Gap.Y ? 7 : layer[x, y]);
            c.That(View(game, 0).Version == int.MaxValue && View(game, 1).Version == foreign && layer[Gap] == 7, "unaffected exhausted company cannot refuse hidden bulk generation");
            game = Prepared(c, factory); map = Map(game); layer = map.Layer<IntLayer>("bulk.int"); layer.Set(First, 5);
            var privateLayer = map.Layer<FixedLayer>("bulk.private"); Counter(map, "_publicVersion", int.MaxValue);
            privateLayer.Fill((x, y) => x == First.X && y == First.Y ? Fix.One : Fix.Zero);
            c.Equal(Fix.One, privateLayer[First], "unaffected exhausted public cursor cannot refuse private generation");
            // Independently committed other-layer generator callback is NOT fabricated rollback.
            game = Prepared(c, factory); map = Map(game); layer = map.Layer<IntLayer>("bulk.int"); layer.Set(First, 5);
            Counter(map, "_version", int.MaxValue - 1); int version = layer.Version;
            c.Throws<OverflowException>(() => layer.Fill((x, y) => { if (x == 0 && y == 0) map.Layer<ByteLayer>("bulk.byte").Set(First, 9); return 7; }), "bulk preflight checks actual counters AFTER generator callbacks");
            c.That(layer[First] == 5 && layer.Version == version && map.Version == int.MaxValue && map.Layer<ByteLayer>("bulk.byte")[First] == 9, "failed main replacement retains prior data but preserves real independent callback effect");
            game = Prepared(c, factory); map = Map(game); layer = map.Layer<IntLayer>("bulk.int");
            TileMap.ValueWrite stale = map.PrepareValueWrite(layer, new[] { First }); layer.Set(First, 3); before = Cursors(game, layer);
            bool published=false;
            c.Throws<InvalidOperationException>(() => map.CommitValueWrite(stale,()=>published=true,()=>published=true), "stale internal preparation never authorizes later cursor publication");
            c.That(!published,"stale preparation invokes neither actual value publication nor notification");
            c.Equal(before, Cursors(game, layer), "refused stale cursor preparation changes nothing");
        }
        private static void WithoutVisibility(Check c)
        {
            GameFactory factory = Factory(false); SimGame game = New(factory); var map = Map(game); var layer = map.Layer<IntLayer>("bulk.int");
            int a = View(game, 0).Version, b = View(game, 1).Version, global = map.Version;
            var baseline = View(game, 0).ReadChanges().Cursor;
            layer.Fill((x, y) => x == 0 && y == 0 ? 4 : x == 17 && y == 16 ? 6 : 0);
            c.That(map.Version == global + 1 && View(game, 0).Version == a + 1 && View(game, 1).Version == b + 1, "explicit no-policy composition still increments every actual public observer exactly once");
            var delta = View(game, 0).ReadChanges(baseline);
            c.That(!delta.FullInvalidation && delta.ChangedRegions.Count == 2, "no-policy bulk diff reports exact actual changed chunks, not full-map fallback");
            map.AssignOwner(game.World, "bulk.private", new PlayerId(0)); a = View(game, 0).Version; b = View(game, 1).Version;
            map.Layer<FixedLayer>("bulk.private").Fill((x, y) => x == 0 && y == 0 ? Fix.FromInt(3) : Fix.Zero);
            c.That(View(game, 0).Version == a + 1 && View(game, 1).Version == b, "no-policy private layer still protects actual ownership");
            SimGame twin = factory.Load(game.Save()); Tick(game); Tick(twin); c.Equal(game.Hash(), twin.Hash(), "no-policy bulk generation restores and continues without duplicate clock/state");
        }
    }
}
