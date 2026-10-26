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
    internal static class TileConditionDefinitionTests
    {
        internal const string Case = "Authored tile conditions: frozen shared contracts and physical saved colony work";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static JsonValue Read(string file) => Json.Parse(FrameworkProofContent.Read(file));
        private static GameFactory Factory(JsonValue bands = null, JsonValue placement = null, JsonValue navigation = null,
            JsonValue world = null, JsonValue columns = null)
        {
            var files = new Dictionary<string, string>();
            if (bands != null) files.Add("colony-conditions.json", bands.ToString());
            if (placement != null) files.Add("colony-placement.json", placement.ToString());
            if (navigation != null) files.Add("colony-navigation.json", navigation.ToString());
            if (columns == null) return Colony.Factory(definitions: world?.ToString(), moduleDocuments: files);
            world = world ?? Read("colony.json");
            world.Get("scenarios")[0].Get("modules").Add(JsonValue.Of(TileConditionProofModule.ModuleId));
            files.Add("tile-condition-proof.json", columns.ToString());
            return Colony.Factory(definitions: world.ToString(), moduleDocuments: files,
                additionalModules: new[] { new TileConditionProofModule() },
                moduleDeclarations: new[] { "{\"id\":\"proof.tile-conditions\",\"version\":\"1.0.0\",\"documents\":[\"tile-condition-proof.json\"]}" });
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 877 });
        private static IEnumerable<EntityView> Mine(SimGame game, string prototype = null)
            => game.Seat(0).Snapshot.Entities.Where(e => e.Owner == new PlayerId(0) && (prototype == null || e.Def == prototype));
        private static ConstructCommand Build(int x, int y)
            => new ConstructCommand { Def = "tower", Priority = 50, Placement = TilePlacement.Arguments("colony.tiles", new TileCoord(x, y)) };
        private static Code MovePreview(SimGame game, int x, int y)
            => game.Seat(0).Preview(new MoveVehicleCommand { Vehicle = Mine(game, "truck").First().Id, To = new TileCoord(x, y) });
        private static List<WorkJobView> Jobs(SimGame game)
        {
            var jobs = new List<WorkJobView>(); game.Seat(0).View.Feature<WorkView>().CopyJobs(jobs); return jobs;
        }
        private static bool Productive(SimGame game)
        {
            var work = game.Seat(0).View.Feature<WorkView>(); var orders = new List<OrderView>(); work.CopyOrders(orders);
            foreach (var order in orders)
            {
                var nodes = new List<ActivityNodeView>();
                if (work.TryCopyNodes(order.Id, nodes) && nodes.Any(n => n.ProductiveSteps > 0 && !n.Progress.Equals(WorkProgress.Zero))) return true;
            }
            return false;
        }
        private static Fix Steel(SimGame game)
        {
            Fix amount = Fix.Zero;
            foreach (var entity in Mine(game))
            {
                var facts = new List<Facet>();
                if (!game.Seat(0).View.Inspect(entity.Id, facts)) throw new InvalidOperationException("missing owned inventory facts");
                foreach (var fact in facts) if (fact.Key == Storage.FacetStock && fact.Subject == "steel") amount += fact.Value;
            }
            return amount;
        }
        private sealed class Observation
        {
            internal bool Moving, Loaded;
            internal void Read(SimGame game)
            {
                Moving |= Mine(game).Any(e => e.State == new Code("state.moving"));
                Loaded |= Jobs(game).Any(j => j.IsHaul && j.Loaded > Fix.Zero);
            }
        }
        private static void Update(SimGame game, Observation observation, SimGame twin = null)
        {
            game.Update(); observation.Read(game);
            if (game.Fault != null) throw new InvalidOperationException("condition Update fault " + game.Fault);
            if (twin == null) return;
            twin.Update();
            if (twin.Fault != null || game.Hash() != twin.Hash())
                throw new InvalidOperationException("condition saved twins diverged at " + game.Clock.StepIndex + "/" + twin.Fault);
        }
        private static void Reach(SimGame game, Observation observation, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 2000 && !done(); i++) Update(game, observation, twin);
            if (!done()) throw new InvalidOperationException(stage + "/time=" + game.Clock.StepIndex + "/entities=" +
                string.Join(";", Mine(game).Select(e => e.Id + "/" + e.Def + "/" + e.State)) + "/jobs=" +
                string.Join(";", Jobs(game).Select(j => j.Id + "/" + j.State + "/" + j.Loaded + "/" + j.Blocker)));
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current shared condition engine identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no new runtime/save owner");
            var factory = Factory(); var game = New(factory); var idle = New(factory); ulong idleHash = idle.Hash();
            c.That(game.Modules.Contains(TileConditionsModule.ModuleId), "explicit master/scenario selects native condition content");
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "physical condition game needs no optional business implementation");
            c.Equal(0L, game.Clock.StepIndex, "condition composition does not advance sole clock");

            // Definition/lifecycle diagnostics, not player-owned gameplay or terrain mutation.
            var content = game.World.Content;
            var band = content.Get<TileLayerBandConditionDef>("colony.condition.wet");
            var navigation = content.Get<LayeredTileNavigationDef>("land");
            var placement = content.Get<TileConstructionPlacementDef>("colony.placement");
            var map = (TileMap)game.World.Space; var resolver = new ContentResolver(content, band);
            c.That(band.IsFrozen && navigation.IsFrozen && placement.IsFrozen, "existing catalog publishes only frozen definitions");
            c.That(ReferenceEquals(band, navigation.Rules[0].Conditions[0]) &&
                ReferenceEquals(band, placement.Rules[0].Conditions[0]), "both consumers alias the SAME actual stable-ID contract definition");
            c.That(ReferenceEquals(band, idle.World.Content.Get<ITileConditionDefinition>(band.Id)), "interleaved sessions share metadata, not live state");
            c.That(band.AuthoringPath.Contains("colony-conditions.json") && band.Module == TileConditionsModule.ModuleId,
                "master file ownership and native provider identity retained");
            c.Throws<ContentException>(() => band.Resolve(resolver), "published condition cannot rebind");
            c.Throws<ContentException>(() => band.ValidateResolved(resolver), "published condition cannot revalidate");
            c.Throws<ContentException>(() => band.Read(new JsonReader(Json.Parse("{}"), "late")), "published condition cannot reread");
            c.Throws<NotSupportedException>(() => ((IList<ITileConditionDefinition>)navigation.Rules[0].Conditions).Clear(),
                "consumer contract list is immutable");
            c.That(band.Matches(game.World, map, new TileCoord(35, 5)) && !band.Matches(game.World, map, new TileCoord(10, 16)),
                "rejected lifecycle attempts preserve exact wet/dry metadata");
            c.That(!band.Matches(game.World, map, new TileCoord(-1, 5)), "off-map condition never invents a sample");
            c.Throws<InvalidOperationException>(() => band.Matches(game.World, (TileMap)idle.World.Space, new TileCoord(35, 5)),
                "cross-session actual-map substitution refuses");
            var fresh = new TileLayerBandConditionDef();
            fresh.Read(new JsonReader(Json.Parse("{\"space\":\"colony.tiles\",\"layer\":\"water\",\"minimum\":1,\"maximum\":null}"), "fresh"));
            c.Throws<InvalidOperationException>(() => fresh.Matches(game.World, map, new TileCoord(35, 5)), "unpublished condition cannot evaluate");
            fresh.Resolve(resolver);
            c.Throws<ContentException>(() => fresh.Resolve(resolver), "resolution is one-shot even before publication");

            ulong before = game.Hash();
            c.Equal(new Code("colony.on-water"), game.Seat(0).Preview(Build(33, 4)), "any matching footprint cell excludes real build");
            c.Equal(new Code("core.unreachable"), MovePreview(game, 35, 5), "same authored wet definition excludes routing");
            c.Equal(before, game.Hash(), "condition previews and lifecycle diagnostics remain pure");
            var refusal = TestKit.Order(game, 0, Build(33, 4));
            c.That(!refusal.Accepted && refusal.Refusal == new Code("colony.on-water") && !Mine(game, "site:tower").Any(),
                "normal command refusal matches preview and creates no project");

            var bands = Read("colony-conditions.json"); bands.Get("tile_layer_band_conditions")[0].Set("minimum", JsonValue.Number("2"));
            var changed = New(Factory(bands: bands));
            c.Equal(Code.None, changed.Seat(0).Preview(Build(35, 5)), "one JSON band edit changes actual construction admission");
            c.Equal(Code.None, MovePreview(changed, 35, 5), "SAME JSON definition edit changes actual navigation admission");
            c.That(changed.World.Content.Fingerprint != game.World.Content.Fingerprint, "authored condition edit changes actual content identity");
            c.Throws<InvalidOperationException>(() => band.Matches(changed.World, (TileMap)changed.World.Space, new TileCoord(35, 5)),
                "definition from different content set cannot evaluate actual session");
            c.Equal(new Code("colony.on-water"), game.Seat(0).Preview(Build(35, 5)), "JSON edit never mutates earlier shared catalog");
            Extension(c); NativeScalars(c); Invalid(c);

            var build = Build(10, 16); before = game.Hash();
            c.Equal(Code.None, game.Seat(0).Preview(build), "dry authored conditions admit actual physical project");
            c.Equal(before, game.Hash(), "accepted build preview is pure");
            var result = TestKit.Order(game, 0, build);
            c.That(result.Accepted && Mine(game, "site:tower").Any(e => e.Id == result.Entity) && !Mine(game, "tower").Any(),
                "normal build creates staged project, never instant finished structure");
            var observation = new Observation();
            Reach(game, observation, () => observation.Loaded && Productive(game) && !Mine(game, "tower").Any(), "loaded productive partial construction");
            c.That(observation.Moving && observation.Loaded && game.Clock.StepIndex > 1, "real machine travel and loaded delivery consume game time");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "partial physical work uses original saved owners");
            long step = game.Clock.StepIndex; game.Clock.Pause(); twin.Clock.Pause(); Update(game, observation, twin);
            c.Equal(step, game.Clock.StepIndex, "sole paused clock stops physical work and hauling");
            game.Clock.Resume(); twin.Clock.Resume();
            Reach(game, observation, () => Mine(game, "tower").Count() == 1, "actual construction completion", twin);
            c.Equal(Fix.FromInt(20), Steel(game), "50 opening minus30 actual tower input, no cargo duplication");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed build roundtrips with same condition metadata");
            c.Equal(idleHash, idle.Hash(), "interleaved live sessions remain isolated");
        }
        private static void Extension(Check c)
        {
            var placement = Read("colony-placement.json"); var navigation = Read("colony-navigation.json");
            placement.Get("tile_construction_rules")[0].Get("rules")[0].Set("conditions", Json.Parse("[\"proof.closed-columns\"]"));
            navigation.Get("layered_tile_navigation")[0].Get("rules")[0].Set("conditions", Json.Parse("[\"proof.closed-columns\"]"));
            var factory = Factory(placement: placement, navigation: navigation, columns: Read("tile-condition-proof.json"));
            var game = New(factory); ulong hash = game.Hash();
            c.That(game.Modules.Contains(TileConditionProofModule.ModuleId), "external content module explicitly selected");
            c.That(game.World.Content.Get<TileColumnsConditionDef>("proof.closed-columns") is ITileConditionDefinition,
                "external predicate implements original consumer contract");
            c.Equal(new Code("colony.on-water"), game.Seat(0).Preview(Build(35, 16)), "external JSON columns exclude DRY construction without changing consumer");
            c.Equal(new Code("core.unreachable"), MovePreview(game, 35, 16), "same external condition excludes dry navigation without scheduler edits");
            c.Equal(Code.None, game.Seat(0).Preview(Build(10, 16)), "external predicate retains admitted operating space");
            c.Equal(hash, game.Hash(), "external authored predicate previews remain pure");
            c.Equal(hash, factory.Load(game.Save()).Hash(), "external definition uses same content/save identity");
        }
        private static void NativeScalars(Check c)
        {
            var factory = LayeredNavigationTests.Factory(); var game = LiveSensorTests.New(factory);
            var content = game.World.Content; var map = (TileMap)game.World.Space; var road = new TileCoord(8, 8);
            var roadBand = content.Get<TileLayerBandConditionDef>("route.condition.road");
            var shallow = content.Get<TileLayerBandConditionDef>("route.condition.shallow");
            c.That(roadBand.Layer.ValueType == SpatialLayerValueType.Int32 && roadBand.Matches(game.World, map, road),
                "native signed-int samples obey frozen authored band");
            c.That(shallow.Layer.ValueType == SpatialLayerValueType.Fixed && shallow.Matches(game.World, map, road) &&
                shallow.Maximum == Fix.Half, "fractional native fixed band remains exact");
            c.Equal(1, content.Get<LayeredTileNavigationDef>("terrain.route").Cost(game.World, road),
                "original stable rule ordering and AND composition use shared native references");
            c.Equal(0, content.Get<LayeredTileNavigationDef>("terrain.vessel").Cost(game.World, road),
                "unmatched fixed-water rule retains explicit blocked base");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "native int/fixed referenced route metadata saves through existing identity");
            var bad = Read("native-route-conditions.json"); bad.Get("tile_layer_band_conditions")[0].Set("minimum", JsonValue.Number("0.5"));
            c.Throws<ContentException>(() => LiveSensorTests.New(LayeredNavigationTests.Factory(conditions: bad)),
                "native int layer cannot accept fractional bound");
            var additional = Read("colony-conditions.json");
            var another = additional.Get("tile_layer_band_conditions")[0].Clone(); another.Set("id", JsonValue.Of("colony.condition.wet-again"));
            additional.Get("tile_layer_band_conditions").Add(another);
            var nav = Read("colony-navigation.json"); nav.Get("layered_tile_navigation")[0].Get("rules")[0].Get("conditions").Add(JsonValue.Of("colony.condition.wet-again"));
            var equivalent = New(Factory(bands: additional, navigation: nav));
            c.Equal(new Code("core.unreachable"), MovePreview(equivalent, 35, 5),
                "distinct condition identities may constrain SAME actual layer without concrete-layer restriction");
        }
        private static void Invalid(Check c)
        {
            foreach (string mode in new[] { "missing-reference", "unknown-reference", "inline-object", "duplicate-reference", "empty-reference",
                "empty-conditions", "missing-bound", "empty-band", "fractional-byte", "missing-layer", "private-layer", "foreign-space", "unselected-provider" })
            {
                var bands = Read("colony-conditions.json"); var band = bands.Get("tile_layer_band_conditions")[0];
                var placement = Read("colony-placement.json"); var rule = placement.Get("tile_construction_rules")[0].Get("rules")[0];
                var world = Read("colony.json");
                if (mode == "missing-reference") rule.Remove("conditions");
                if (mode == "unknown-reference") rule.Set("conditions", Json.Parse("[\"missing.condition\"]"));
                if (mode == "inline-object") rule.Set("conditions", Json.Parse("[{\"layer\":\"water\",\"minimum\":1,\"maximum\":null}]"));
                if (mode == "duplicate-reference") rule.Get("conditions").Add(rule.Get("conditions")[0].Clone());
                if (mode == "empty-reference") rule.Set("conditions", Json.Parse("[\"\"]"));
                if (mode == "empty-conditions") rule.Set("conditions", JsonValue.NewArray());
                if (mode == "missing-bound") band.Remove("maximum");
                if (mode == "empty-band") band.Set("maximum", JsonValue.Number("1"));
                if (mode == "fractional-byte") band.Set("minimum", JsonValue.Number("0.5"));
                if (mode == "missing-layer") band.Set("layer", JsonValue.Of("missing.layer"));
                if (mode == "private-layer") world.Get("spatial_layers")[0].Set("visibility", JsonValue.Of("company_private"));
                if (mode == "foreign-space") band.Set("space", JsonValue.Of("missing.space"));
                if (mode == "unselected-provider")
                {
                    var selected = world.Get("scenarios")[0].Get("modules"); var modules = JsonValue.NewArray();
                    for (int i = 0; i < selected.Count; i++) if (selected[i].Text != TileConditionsModule.ModuleId) modules.Add(selected[i].Clone());
                    world.Get("scenarios")[0].Set("modules", modules);
                }
                c.Throws<ContentException>(() => New(Factory(bands: bands, placement: placement, world: world)), "before Initialize: " + mode);
            }
            var foreign = Read("tile-condition-proof.json"); foreign.Get("proof_tile_columns")[0].Set("space", JsonValue.Of("other.tiles"));
            var worldOther = Read("colony.json"); worldOther.Get("tile_spaces").Add(Json.Parse("{\"id\":\"other.tiles\",\"width\":60,\"height\":40}"));
            var placementOther = Read("colony-placement.json");
            placementOther.Get("tile_construction_rules")[0].Get("rules")[0].Set("conditions", Json.Parse("[\"proof.closed-columns\"]"));
            c.Throws<ContentException>(() => New(Factory(world: worldOther, placement: placementOther, columns: foreign)),
                "valid external condition in another actual space cannot authorize selected placement");
        }
    }
}
