using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class ProjectionTests
    {
        private static GameFactory Factory(bool fog = true)
        {
            var definition = new GameDefinition("projection", new GameModule[] { new ProjectionModule(fog) });
            var files = new PreloadedFileSource().Add("manifest.json", "{ \"schema\": 1, \"modules\": [{ \"id\": \"projection\", \"version\": \"1.0.0\", \"documents\": [\"world.json\"] }] }")
                .Add("world.json", "{ " + TestGame.TimeDefinitions + ProjectionModule.SpatialDefinitions + @"
                  ""entity_states"": [{ ""id"": ""idle"", ""state"": ""state.idle"", ""priority"": 0, ""default"": true }],
                  ""entities"": [{ ""id"": ""marker"", ""kind"": ""marker"", ""tags"": [""original""], ""components"": {
                    ""tile_presence"": { ""space"": ""projection.tiles"", ""width"": 1, ""height"": 1, ""blocks"": false, ""movable"": false }
                  }}],
                  ""game_modes"": [{ ""id"": ""projection.resource-only"", ""name"": ""Physical diagnostic game"", ""properties"": {} }],
                  ""scenarios"": [{ ""id"": ""projection"", ""game_mode"": ""projection.resource-only"", ""mode_overrides"": {}, ""time"": ""test.minute"", ""space"": ""projection.tiles"", ""modules"": [""projection""], ""seats"": 2, ""default_seats"": 2 }]
                }");
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "projection" });
        public static void Register()
        {
            TestRunner.Add("Projections: retained immutable frames and visibility-scoped deltas", Retained);
            TestRunner.Add("Projections: private layers cannot leak through cursors or snapshots", PrivateMap);
            TestRunner.Add("Projections: engine-owned collections cannot be replaced through casts", Collections);
            TestRunner.Add("Projections: private recipe queries and detached catalogue facts", Recipes);
        }

        private static void Retained(Check c)
        {
            SimGame game = New(Factory());
            GameSnapshot observer = game.Snapshot, seat = game.Seat(0).Snapshot;
            c.Equal(2, observer.Entities.Count, "authorized observer sees both entities");
            c.Equal(1, seat.Entities.Count, "seat snapshot uses the engine's visibility contract");
            c.Equal(1, game.Seat(0).View.EntityCount, "count uses exactly the same visibility rule");
            c.Equal(2, game.Observer.EntityCount, "observer count retains authorized facts");
            c.Throws<NotSupportedException>(() => ((IList<EntityView>)seat.Entities).Clear(), "published entity list cannot be mutated");
            c.Throws<NotSupportedException>(() => ((IList<EntityId>)seat.Spawned).Clear(), "published spawn list cannot be mutated");
            c.Throws<NotSupportedException>(() => ((IList<EntityId>)seat.Removed).Clear(), "published removal list cannot be mutated");
            foreach (var property in typeof(GameSnapshot).GetProperties()) c.That(property.SetMethod == null, "snapshot metadata is immutable: " + property.Name);
            game.World.Despawn(game.World.Entities.Get(seat.Entities[0].Id)); game.World.Settle(); game.RunTicks(5);
            c.Equal(0, game.Seat(0).Snapshot.Entities.Count, "removed entity leaves only its owner's scoped projection");
            c.Equal(0, game.Seat(0).View.EntityCount, "count excludes removed entity");
            c.Equal(1, seat.Entities.Count, "retained seat snapshot survives more than two later updates");
            c.Equal(2, observer.Entities.Count, "retained observer frame is not reused either");
            c.Equal(0, seat.Tick, "retained metadata does not change after later ticks");
            c.Equal(1, seat.Spawned.Count, "retained delta does not change after later frames");
            ulong hash = game.Hash(); var latest = game.Snapshot; var repeated = game.Snapshot; c.Equal(hash, game.Hash(), "snapshot reads never change authoritative simulation state");
        }

        private static void PrivateMap(Check c)
        {
            foreach (bool fog in new[] { false, true })
            {
                GameFactory factory = Factory(fog); SimGame game = New(factory);
                TileMap map = (TileMap)game.World.Space;
                TileMapView mine = game.Seat(0).View.Feature<TileMapView>(), other = game.Seat(1).View.Feature<TileMapView>();
                c.That(mine.LayerIds.Count == 2 && !new List<string>(mine.LayerIds).Contains("knowledge.1"), "private layer identity is not disclosed, fog=" + fog);
                c.Equal(3, game.Observer.Feature<TileMapView>().LayerIds.Count, "observer sees all declared layers");
                SpatialRegionChanges<TileRect> delta = mine.ReadChanges();
                var mineBaseline = delta.Cursor; int mineCursor = mineBaseline.Version, otherCursor = other.Version;
                c.That(delta.FullInvalidation && delta.ChangedRegions.Count == 0, "world generation signals full invalidation without enumerating the map");
                map.Layer<ByteLayer>("knowledge.1").Set(new TileCoord(20, 20), 8);
                c.Equal(mineCursor, mine.Version, "another company's private write changes no visible cursor");
                delta = mine.ReadChanges(mineBaseline);
                c.Equal(mineCursor, delta.Cursor.Version, "private delta returns unchanged cursor");
                c.Equal(0, delta.ChangedRegions.Count, "another company's private chunk never appears in deltas");
                c.That(other.Version > otherCursor, "owner observes its private change");
                game.Update(0); c.Equal(mineCursor, game.Seat(0).Snapshot.Space.Version, "seat snapshot exposes no private global version even without entity fog");
                var values = new List<int>();
                c.Equal(SpatialLayerReadStatus.Unavailable, mine.ReadLayer("knowledge.1", new TileRect(20, 20, 1, 1), values), "private layer pixels stay unavailable");
                c.Equal(0, values.Count, "private data is not substituted with fabricated zero pixels");
                map.Layer<ByteLayer>("public").Set(new TileCoord(20, 1), 3);
                delta = mine.ReadChanges(mineBaseline);
                var nextBaseline = delta.Cursor; int next = nextBaseline.Version;
                c.Equal(mineCursor + 1, next, "visible sequence contains no gap from invisible writes");
                c.Equal(1, delta.ChangedRegions.Count, "only the public changed chunk is returned");
                c.That(delta.ChangedRegions[0].Contains(new TileCoord(20, 1)) && !delta.ChangedRegions[0].Contains(new TileCoord(20, 20)), "visible delta does not include foreign private location");
                map.Layer<ByteLayer>("knowledge.0").Set(new TileCoord(1, 20), 5);
                delta = mine.ReadChanges(nextBaseline);
                c.Equal(1, delta.Cursor.Version - next, "owner private writes join its visible sequence");
                c.Equal(1, delta.ChangedRegions.Count, "owner receives its private chunk");
                ulong before = game.Hash(); mine.ReadChanges(); var current = game.Snapshot; c.Equal(before, game.Hash(), "visibility versions and projection reads are not another saved state ledger");
                SimGame restored = factory.Load(game.Save());
                c.Equal(game.Hash(), restored.Hash(), "private layer values restore through their existing sole Sync");
                TileMapView restoredView = restored.Seat(0).View.Feature<TileMapView>();
                c.Equal(2, restoredView.LayerIds.Count, "restored layer metadata retains privacy");
                delta = restoredView.ReadChanges();
                var restoredBaseline = delta.Cursor; int restoredCursor = restoredBaseline.Version;
                c.That(delta.FullInvalidation && delta.ChangedRegions.Count == 0, "fresh restored view signals full invalidation");
                ((TileMap)restored.World.Space).Layer<ByteLayer>("knowledge.1").Set(new TileCoord(1, 1), 2);
                delta = restoredView.ReadChanges(restoredBaseline);
                c.Equal(restoredCursor, delta.Cursor.Version, "restored foreign private writes remain invisible");
                c.Equal(0, delta.ChangedRegions.Count, "restored private delta cannot leak");
            }
        }

        private static void Collections(Check c)
        {
            SimGame game = New(Factory()); EntityDef def = game.World.Content.Get<EntityDef>("marker");
            c.Throws<NotSupportedException>(() => ((IList<string>)game.Modules)[0] = "other", "session modules cannot mutate world settings");
            c.Throws<NotSupportedException>(() => ((IList<string>)game.World.Content.Modules).Clear(), "resolved module identity list is frozen");
            c.Throws<NotSupportedException>(() => ((IList<string>)def.Tags)[0] = "other", "shared entity tags are frozen");
            c.Throws<NotSupportedException>(() => ((IList<ComponentDef>)def.Components).Clear(), "shared component slots are frozen");
            c.Throws<NotSupportedException>(() => ((IList<Component>)game.World.Entities.All[0].Components).Clear(), "runtime component slots cannot be replaced");
            c.Throws<NotSupportedException>(() => ((IList<Entity>)game.World.Entities.All).Clear(), "entity iteration cannot bypass its store");
            c.Throws<NotSupportedException>(() => ((IList<MapLayer>)((TileMap)game.World.Space).Layers).Clear(), "map iteration cannot bypass layer identity ownership");
            c.Throws<NotSupportedException>(() => ((IList<TileCoord>)TileSpatialGeometry.Neighbours)[0] = new TileCoord(8, 8), "shared navigation order cannot be edited globally");
            var tags = new[] { "captured" }; var parts = new ComponentDef[] { ReadSchemaProbe() };
            EntityDef generated = ContentAttachmentProbe.Attach("captured", "projection", EntityKind.Marker, "test", "test", tags, parts);
            tags[0] = "changed"; parts[0] = null;
            c.That(generated.HasTag("captured") && generated.Components[0] != null, "authored definitions capture both caller-owned arrays");
        }

        private static SchemaProbeDef ReadSchemaProbe()
        {
            var part = new SchemaProbeDef();
            part.Read(new JsonReader(Json.Parse("{\"operation\":" + SchemaProbeSystem.Controls + "}"), "negative array-capture probe"));
            return part;
        }
        private static void Recipes(Check c)
        {
            SimGame game = Colony.Factory().NewGame(new NewGameRequest { Scenario = "colony" });
            EntityId owned = Colony.Mine(game, 0, "factory")[0].Id, foreign = Colony.Mine(game, 1, "factory")[0].Id;
            TransformationManagementView view = game.Seat(0).View.Feature<TransformationManagementView>();
            c.That(view.TransformationsOf(owned, new Code("transformation")).Count > 0, "owner reads its transformation options");
            c.Equal(0, view.TransformationsOf(foreign, new Code("transformation")).Count, "foreign transformation options are not exposed");
            c.That(game.Observer.Feature<TransformationManagementView>().TransformationsOf(foreign, new Code("transformation")).Count > 0, "observer retains authorized transformation access");
            var catalogue = new List<CatalogueEntry>(); game.Observer.CopyCatalogue(catalogue);
            foreach (CatalogueEntry entry in catalogue) if (entry.Tags.Length > 0)
            {
                string authored = entry.Tags[0]; entry.Tags[0] = "changed";
                c.That(game.World.Content.Get<EntityDef>(entry.Id).HasTag(authored), "catalogue returns detached tags for " + entry.Id);
            }
        }
    }
}
