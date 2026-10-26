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
using static Beep.SimEngine.Tests.EmergencyPreemptionTests;
using static Beep.SimEngine.Tests.ResourceDispositionTests;

namespace Beep.SimEngine.Tests
{
    public static partial class KnowledgeVisibilityTests
    {
        public static void Register()
        {
            TestRunner.Add("Layer knowledge: authored discovery, private cursors and saved continuation", LayerKnowledgeTests.Run);
            TestRunner.Add("Visibility: authored knowledge contracts and hidden definition admission", Contracts);
            TestRunner.Add("Visibility: physical discovery, scoped snapshots and saved continuation", Physical);
            TestRunner.Add(ProviderIdentityCase, ProviderIdentityObservation);
        }
        private static string Json => FrameworkProofContent.Read("knowledge-visibility.json");
        private static GameFactory Factory(string json = null, Action<JsonValue> edit = null)
            => TechnologyTests.DiscoveryFactory(visibility: json ?? Json, scenarioEdit: edit);
        private static GameFactory PhysicalFactory(string facts = null, string visibility = null)
        {
            // Authored inputs for the SAME master-selected documents and existing source store.
            // Known consumables/tools must not depend on the unknown observation they help earn.
            JsonValue inputs = Content.Json.Parse(FrameworkProofContent.Read("knowledge-visibility-physical.json"));
            JsonValue work = Content.Json.Parse(FrameworkProofContent.Read("knowledge-work.json"));
            work.Get("activities")[0].Get("nodes")[0].Set("requirement", JsonValue.Of("discovery.ready"));
            return TechnologyTests.DiscoveryFactory(facts: facts, work: work.ToString(), visibility: visibility ?? Json,
                constructionEdit: document => {
                    foreach (string section in new[] { "target_resource_requirements", "work_requirement_groups" }) {
                        JsonValue rows = document.Get(section);
                        if (rows == null) { rows = JsonValue.NewArray(); document.Set(section, rows); }
                        for (int i = 0; i < inputs.Get(section).Count; i++) rows.Add(inputs.Get(section)[i].Clone());
                    }
                }, worldEdit: document => {
                    document.Get("resources").Add(inputs.Get("input_resource").Clone());
                    JsonValue entities = document.Get("entities");
                    for (int i = 0; i < entities.Count; i++) if (entities[i].Get("id").Text == "source") {
                        JsonValue components = entities[i].Get("components"); components.Remove("point_stock");
                        components.Set("storage", inputs.Get("source_inventory").Clone());
                    }
                });
        }
        private static SimGame New(GameFactory factory) => ConstructionPlanTests.NewPoints(factory);
        private static List<CatalogueEntry> Catalogue(SimGame game, int seat = 0)
        { var list = new List<CatalogueEntry>(); game.Seat(seat).View.CopyCatalogue(list); return list; }
        private static List<TechnologySnapshot> Technology(SimGame game, int seat = 0)
        { var list = new List<TechnologySnapshot>(); game.Seat(seat).View.Feature<TechnologyView>().CopyTechnologies(list); return list; }
        private static Code Preview(SimGame game, string activity)
            => game.Seat(0).Preview(new PostOrderCommand { Plan = activity, Target = Find(game, "source").Id, Priority = 10 });
        private static CommandResult Post(SimGame game, string activity)
            => TestKit.Order(game, 0, new PostOrderCommand { Plan = activity, Target = Find(game, "source").Id, Priority = 10 });
        private static void Reach(SimGame game, Func<bool> done, string stage, SimGame twin = null)
        {
            for (int i = 0; i < 900 && !done(); i++) { Tick(game); if (twin != null) Tick(twin); }
            if (!done()) throw new InvalidOperationException(stage + " tick=" + game.Tick + " orders=" + string.Join(";", Board(game).Orders.Select(o => o.Id + "/" + o.State + "/" + o.Blocker)));
        }
        private static void Contracts(Check c)
        {
            GameFactory factory = Factory(); SimGame game = New(factory); IVisibility policy = game.World.Services.Get<IVisibility>();
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "visibility semantics have current breaking identity");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "no duplicate visibility state/codec");
            c.That(policy is KnowledgeVisibility, "actual engine-owned policy composes through the sole IVisibility");
            c.That(!typeof(IPersistentService).IsAssignableFrom(typeof(KnowledgeVisibility)), "policy derives learned facts, never another saved knowledge table");
            c.That(game.World.Content.Get<KnowledgeVisibilityPolicyDef>("discovery.policy").AuthoringPath.Contains("visibility.json"), "selected master document is policy origin");
            c.Equal(3, game.Seat(0).View.EntityCount, "own-entity visibility is explicitly authored");
            c.Equal(0, game.Seat(1).View.EntityCount, "unlisted foreign entities are explicitly hidden");
            c.Equal(3, game.Observer.EntityCount, "explicit observer retains entity facts");
            ulong hash = game.Hash();
            c.That(!Catalogue(game).Any(x => x.Id == "point.tower"), "unknown prototype absent from catalogue");
            c.That(!Catalogue(game, 1).Any(x => x.Id == "point.tower"), "other company catalogue independently unknown");
            var all = new List<CatalogueEntry>(); game.Observer.CopyCatalogue(all); c.That(all.Any(x => x.Id == "point.tower"), "observer sees authored prototype");
            c.Equal(1, Technology(game).Count, "hidden research absent from technology menu");
            c.Equal("research.foundation", Technology(game)[0].Technology, "visible root research retained");
            c.Equal(Codes.UnknownDef, Preview(game, "research.advanced"), "hidden research preview is unknown");
            c.Equal(Codes.UnknownDef, Preview(game, "missing"), "unknown and hidden research share refusal");
            c.Equal(Code.None, Preview(game, "point.tower"), "same-name activity is distinct from hidden entity prototype");
            var placement = Content.Json.Parse("{\"space\":\"field.points\",\"position\":-100}");
            c.Equal(Codes.UnknownDef, game.Seat(0).View.Feature<ConstructionView>().CheckPlacement("point.tower", placement), "hidden prototype discloses no out-of-bounds placement detail");
            c.Equal(Codes.UnknownDef, game.World.Services.Get<IConstructionProjects>().CheckIntent(game.World, new PlayerId(0), game.World.Content.Get<EntityDef>("point.tower"), placement), "normal pending construction intent shares discovery gate");
            var resources = new List<ResourceInfo>(); game.Seat(0).View.CopyResources(resources); c.Equal(0, resources.Count, "public extension predicate scopes resource catalogue");
            c.That(!game.Seat(0).View.TryFindResource("supply", out ResourceId resource) && resource == default, "hidden resource lookup returns no identity");
            c.Equal<string>(null, game.Seat(0).View.ResourceName(Supply(game)), "hidden resource name lookup discloses no label");
            c.That(game.Observer.TryFindResource("supply", out _), "observer keeps resource lookup");
            c.That(!game.Seat(0).View.Feature<InventoryView>().StoreAccepts("point.tower", Supply(game), Code.None), "capability lookup cannot identify hidden prototype");
            var requirements = new List<WorkRequirementView>();
            c.Equal(Codes.UnknownDef, game.Seat(0).View.Feature<WorkView>().PreviewOrderRequirements("research.advanced", Find(game, "source").Id, requirements), "hidden requirement graph remains unknown");
            c.Equal(0, requirements.Count, "no hidden requirement details");
            var facets = new List<Facet>(); c.That(game.Seat(0).View.Inspect(Find(game, "source").Id, facets), "owned source remains inspectable");
            c.That(!facets.Any(f => f.Key == Storage.FacetStock), "resource inspector does not leak an undiscovered resource label/balance");
            c.Equal(hash, game.Hash(), "visibility/catalogue/preview queries are pure");
            c.Equal(Codes.UnknownDef, Post(game, "research.advanced").Refusal, "ordinary command shares hidden preview refusal");
            c.Equal(0, Board(game).Orders.Count, "hidden refused work allocates no execution");
            SimGame foreign = New(Factory());
            c.That(!policy.CanSeeContent(foreign.World, new PlayerId(0), game.World.Content.Get<EntityDef>("source")), "foreign session cannot use visibility provider");
            c.That(!policy.CanSeeContent(game.World, PlayerId.None, foreign.World.Content.Get<EntityDef>("source")), "observer still requires actual content identity");
            c.That(!policy.CanSeeContent(game.World, new PlayerId(-2), game.World.Content.Get<EntityDef>("source")), "invalid negative identity is no observer");
            c.That(!policy.CanSee(game.World, new PlayerId(-2), Find(game, "source")), "invalid viewer cannot see own/world entities");
            foreach (string mode in new[] { "settings", "policy", "flag", "rules", "duplicate", "mode", "fact", "target", "empty-facts" })
            {
                JsonValue root = Content.Json.Parse(Json); var selected = root.Get("knowledge_visibility_policies")[0]; var target = root.Get("entity_knowledge_visibility")[0];
                if (mode == "flag") selected.Remove("own_entities");
                if (mode == "rules") selected.Remove("rules");
                if (mode == "duplicate") selected.Set("rules", Content.Json.Parse("[\"visibility.tower\",\"visibility.tower\"]"));
                if (mode == "mode") target.Set("mode", JsonValue.Of("guessed"));
                if (mode == "fact") target.Set("facts", Content.Json.Parse("[\"missing\"]"));
                if (mode == "empty-facts") target.Set("facts", JsonValue.NewArray());
                if (mode == "target") target.Set("definition", JsonValue.Of("missing"));
                c.Throws<ContentException>(() => New(Factory(root.ToString(), scenario => {
                    if (mode == "settings") scenario.Get("settings").Remove(KnowledgeVisibilityModule.ModuleId);
                    if (mode == "policy") scenario.Get("settings").Get(KnowledgeVisibilityModule.ModuleId).Set("policy", JsonValue.Of("missing"));
                })), "invalid authored visibility refuses before Initialize: " + mode);
            }
            SimGame omitted = New(TechnologyTests.DiscoveryFactory());
            c.That(!omitted.World.Services.Has<IVisibility>() && Catalogue(omitted).Any(x => x.Id == "point.tower"), "optional omission installs no hidden fog/knowledge policy");
            NoWork(c);
            Recipes(c);
        }
        private static void Recipes(Check c)
        {
            JsonValue rules = Content.Json.Parse(Json);
            rules.Get("knowledge_visibility_policies")[0].Get("rules").Add(JsonValue.Of("visibility.recipe"));
            rules.Set("transformation_knowledge_visibility", Content.Json.Parse("[{\"id\":\"visibility.recipe\",\"definition\":\"point.make\",\"mode\":\"all\",\"facts\":[\"field.observed\"]}]"));
            GameFactory factory = TechnologyTests.DiscoveryFactory(visibility: rules.ToString(), constructionEdit: content => {
                content.Set("transformations", Content.Json.Parse("[{\"id\":\"point.make\",\"mode\":\"batch\",\"cancel_policy\":\"return_inputs\",\"operating_inputs\":[],\"tools\":{},\"inputs\":{},\"outputs\":{},\"entity_output\":\"point.tower\",\"output_home\":\"unassigned\",\"duration\":{\"numerator\":1,\"denominator\":10,\"unit\":\"hour\"}}]"));
                content.Get("entities").Add(Content.Json.Parse("{\"id\":\"assembler\",\"kind\":\"entity\",\"components\":{\"point_presence\":{\"space\":\"field.points\",\"position\":50},\"storage\":{\"capacity_policy\":\"fixture.count.40\",\"public\":false},\"transformer\":{\"automatic_supply\":false,\"wip_capacity\":\"fixture.count.20\",\"transformations\":[\"point.make\"],\"states\":{\"working\":\"processing\",\"stopped\":\"blocked\"},\"operation\":{\"id\":\"transformation\",\"enabled\":true,\"target\":1,\"minimum\":1,\"maximum\":1,\"unit\":\"unit.work-rate\"}}}}"));
            });
            SimGame game = New(factory); Entity assembler = game.World.Spawn(game.World.Content.Get<EntityDef>("assembler"), new PlayerId(0)); game.World.Settle();
            var view = game.Seat(0).View.Feature<TransformationManagementView>(); var capability = new Code("transformation");
            ulong hash = game.Hash();
            c.Equal(0, view.TransformationsOf(assembler.Id, capability).Count, "hidden recipe absent from actual transformer's menu");
            c.Equal<string>(null, view.TransformationEntityOutput("point.make"), "hidden recipe lookup returns no output prototype");
            c.Equal(Codes.UnknownDef, view.ReadGates(assembler.Id, capability, "point.make", out ExecutionGateEvaluation gates), "hidden recipe gates disclose no compatibility details");
            c.That(gates == null, "hidden recipe returns no gate payload");
            EntityOutputPreview output = view.PreviewEntityOutput(assembler.Id, capability, "point.make");
            c.Equal(Codes.UnknownDef, output.Refusal, "hidden entity-output preview is unknown");
            c.That(output.Output == null && output.Placement == null, "hidden recipe output and rollout location stay undisclosed");
            c.Equal(Codes.UnknownDef, view.PreviewEnqueue(assembler.Id, capability, "point.make", 1, false, EntityId.None), "hidden recipe queue preview shares admission");
            c.Equal(hash, game.Hash(), "hidden recipe metadata/gate reads change no state");
            c.Equal(Codes.UnknownDef, TestKit.Order(game, 0, new EnqueueTransformationCommand { Transformer = assembler.Id, Capability = capability, Transformation = "point.make", Count = 1 }).Refusal, "normal recipe command receives same unknown refusal");
            c.That(view.TryGetTransformation(assembler.Id, capability, out TransformationSnapshot snapshot) && snapshot.Queue.Count == 0, "refused hidden recipe allocates no queue execution");
            Configure(c, game); c.That(Post(game, "field.discover").Accepted, "recipe knowledge is earned by normal physical discovery");
            Reach(game, () => view.TransformationsOf(assembler.Id, capability).Count == 1, "physical recipe visibility");
            c.Equal("point.make", view.TransformationsOf(assembler.Id, capability)[0], "actual recipe appears after earned fact");
            c.Equal("point.tower", view.TransformationEntityOutput("point.make"), "known output prototype revealed without fabrication");
            c.Equal(TechnologyCodes.Locked, view.PreviewEnqueue(assembler.Id, capability, "point.make", 1, false, EntityId.None), "known recipe still obeys independent technology access");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "earned recipe visibility restores from the same learned ledger");
        }
        private static void NoWork(Check c)
        {
            var definition = new GameDefinition("visibility-only", new GameModule[] { new CompanyKnowledgeModule(), new KnowledgeVisibilityModule() });
            var files = new PreloadedFileSource().Add("manifest.json", "{\"schema\":1,\"modules\":[{\"id\":\"sim.knowledge\",\"version\":\"1.0.0\",\"documents\":[\"knowledge.json\"]},{\"id\":\"sim.knowledge-visibility\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.knowledge\",\"version\":\"1.0.0\"}],\"documents\":[\"visibility.json\"]}]}")
                .Add("knowledge.json", "{" + TestGame.TimeDefinitions + "\"entity_states\":[{\"id\":\"idle\",\"state\":\"idle\",\"priority\":0,\"default\":true}],\"entities\":[{\"id\":\"marker\",\"kind\":\"marker\",\"components\":{}}],\"game_modes\":[{\"id\":\"knowledge-visibility.resource-only\",\"name\":\"Physical diagnostic game\",\"properties\":{}}],\"scenarios\":[{\"id\":\"visibility-only\",\"game_mode\":\"knowledge-visibility.resource-only\",\"mode_overrides\":{},\"time\":\"test.minute\",\"space\":null,\"modules\":[\"sim.knowledge-visibility\"],\"seats\":2,\"default_seats\":2,\"settings\":{\"sim.knowledge-visibility\":{\"policy\":\"private\"}}}]}")
                .Add("visibility.json", "{\"knowledge_visibility_policies\":[{\"id\":\"private\",\"own_entities\":true,\"unlisted_entities\":false,\"unlisted_content\":true,\"rules\":[]}]}");
            var factory = new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
            SimGame game = factory.NewGame(new NewGameRequest { Scenario = "visibility-only" });
            c.That(game.World.Space == null && !game.World.Services.Has<WorkBoard>(), "visibility module requires neither work nor spatial game rules");
            game.World.Spawn(game.World.Content.Get<EntityDef>("marker"), new PlayerId(1)); game.World.Settle(); game.Update(0);
            c.Equal(0, game.Seat(0).Snapshot.Entities.Count, "no-space private snapshot remains scoped");
            c.Equal(1, game.Seat(1).Snapshot.Entities.Count, "no-space owner sees its actual entity");
            c.Equal(1, game.Snapshot.Entities.Count, "no-space explicit observer remains intentional");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "no-work knowledge/visibility composition saves normally");
        }
        private static void Physical(Check c)
        {
            GameFactory factory = PhysicalFactory(); SimGame game = New(factory);
            // Explicit visibility-protocol setup. Do not place a blocking foreign structure at
            // the default source point and prevent the subsequent real discovery worker reaching it.
            var geometry = (ISpatialPlacement)game.World.Space;
            EntityDef hiddenDefinition = game.World.Content.Get<EntityDef>("point.tower");
            var hiddenPlacement = new PointPlacement(game.World.Space.Id, Fix.FromInt(80));
            c.Equal(Code.None, geometry.CheckPlacement(game.World, hiddenDefinition, hiddenPlacement, null), "hidden foreign fixture has a valid separate physical placement");
            Entity hidden = game.World.Spawn(hiddenDefinition, new PlayerId(1), created => geometry.ConfigurePlacement(created, hiddenPlacement)); game.World.Settle(); game.Update(0);
            c.That(!game.Seat(0).View.TryGetEntity(hidden.Id, out _), "actual foreign prototype is hidden before learning");
            c.Equal(3, game.Seat(0).Snapshot.Entities.Count, "hidden spawn changes no seat entity list");
            c.Equal(0, game.Seat(0).Snapshot.Spawned.Count, "hidden spawn produces no private delta");
            c.Equal(4, game.Observer.EntityCount, "observer sees actual hidden startup entity");
            c.Equal(1, game.Seat(1).View.EntityCount, "authored own visibility does not require company discovery");
            c.That(!Catalogue(game, 1).Any(x => x.Id == "point.tower"), "seeing one's owned entity is not implicit catalogue knowledge");
            GameSnapshot retained = game.Seat(0).Snapshot; var oldCatalogue = Catalogue(game);
            c.That(game.Seat(0).View.TryFindResource("discovery.inputs", out var knownInput) && !game.Seat(0).View.TryFindResource("supply", out _), "work consumables are known while observed supply remains genuinely unknown");
            var initialStock = new List<Facet>(); game.Seat(0).View.Inspect(Find(game, "source").Id, initialStock);
            c.That(initialStock.Any(f => f.Key == Storage.FacetStock && f.Subject == "discovery.inputs") && !initialStock.Any(f => f.Key == Storage.FacetStock && f.Subject == "supply"), "initial inspection reveals only known work inputs, not protected observed stock");
            Configure(c, game); CommandResult start = Post(game, "field.discover"); c.That(start.Accepted, "player starts normal physical discovery: " + start.Refusal);
            if (!start.Accepted) throw new InvalidOperationException("discovery admission refused: " + start.Refusal + ";fault=" + game.Fault);
            Order order = Board(game).Order((int)start.Arg0);
            Reach(game, () => order.Node("sample").Progress.WholeTicks > 0, "partial machine discovery");
            c.That(!Catalogue(game).Any(x => x.Id == "point.tower"), "partial work reveals no prototype");
            SimGame twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "visibility is derived identically from saved partial work/knowledge");
            Reach(game, () => Catalogue(game).Any(x => x.Id == "point.tower"), "earned catalogue and entity reveal", twin);
            c.Equal(game.Hash(), twin.Hash(), "physical discovery and derived visibility are twin-identical");
            c.That(game.Seat(0).View.TryGetEntity(hidden.Id, out EntityView visible) && visible.Owner == new PlayerId(1), "earned knowledge reveals the real existing entity identity");
            c.Equal(4, game.Seat(0).View.EntityCount, "count uses actual earned visibility");
            c.Equal(4, game.Seat(0).Snapshot.Entities.Count, "native snapshot uses same visibility gate");
            c.That(game.Seat(0).Snapshot.Spawned.Contains(hidden.Id), "first reveal yields existing native entered-sight delta exactly once");
            c.Equal(3, retained.Entities.Count, "retained hidden snapshot never changes");
            c.That(!oldCatalogue.Any(x => x.Id == "point.tower"), "retained catalogue facts stay detached");
            c.That(!Catalogue(game, 1).Any(x => x.Id == "point.tower"), "company-private discovery does not unlock another company's catalogue");
            c.Equal(2, Technology(game).Count, "earned visibility reveals research metadata");
            c.Equal(1, Technology(game, 1).Count, "foreign company retains hidden research");
            c.Equal(TechnologyCodes.MissingPrerequisite, Preview(game, "research.advanced"), "visible technology still obeys actual prerequisite admission");
            c.Equal(TechnologyCodes.Locked, game.Seat(0).View.Feature<ConstructionView>().CheckPlacement("point.tower", Content.Json.Parse("{\"space\":\"field.points\",\"position\":12}")), "discovery never bypasses separate technology unlock");
            c.That(game.Seat(0).View.TryFindResource("supply", out ResourceId resource) && resource == Supply(game), "public provider's earned observation reveals actual resource identity");
            c.Equal("supply", game.Seat(0).View.ResourceName(resource), "earned resource name is no substituted default");
            var stockFacets = new List<Facet>(); game.Seat(0).View.Inspect(Find(game, "source").Id, stockFacets);
            c.That(stockFacets.Any(f => f.Key == Storage.FacetStock && f.Subject == "supply"), "earned resource inspector facts become visible through the same policy");
            var resources = new List<ResourceInfo>(); game.Seat(0).View.CopyResources(resources); c.Equal(2, resources.Count, "earned public predicate reveals supply alongside the already known work input");
            c.That(!game.Seat(1).View.TryFindResource("supply", out _), "other company has no borrowed stock observation");
            c.Equal(Fix.FromInt(12), Find(game, "source").Get<IInventory>().Amount(knownInput), "discovery consumed eight actual known materials, not a visibility bonus");
            c.Equal(Fix.FromInt(20), Find(game, "source").Get<IInventory>().Amount(Supply(game)), "earning visibility does not consume or create the observed unknown resource");
            var facets = new List<Facet>(); c.That(game.Seat(0).View.Inspect(hidden.Id, facets), "inspect admits the genuinely revealed entity");
            game.Update(0); c.Equal(0, game.Seat(0).Snapshot.Spawned.Count, "repeated capture does not duplicate entered-sight delta");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "learned visibility survives full save/load without a second codec");
            SimGame restored = factory.Load(game.Save()); c.That(restored.Seat(0).View.TryGetEntity(hidden.Id, out _), "restored private learned visibility derives from the sole ledger");
            c.That(!Catalogue(New(factory)).Any(x => x.Id == "point.tower"), "another session remains independently unknown");
            game.World.Despawn(hidden); game.World.Settle(); game.Update(0);
            c.That(game.Seat(0).Snapshot.Removed.Contains(hidden.Id), "known actual removal returns normal private delta");
            c.Equal(3, game.Seat(0).View.EntityCount, "removal never leaves phantom visible entity");
            Alternatives(c);
        }
        private static void Alternatives(Check c)
        {
            JsonValue facts = Content.Json.Parse(FrameworkProofContent.Read("knowledge.json"));
            facts.Get("knowledge_facts").Add(Content.Json.Parse("{\"id\":\"field.second\",\"fresh_for\":null,\"value\":{\"classification\":\"extra\"}}"));
            JsonValue rules = Content.Json.Parse(Json);
            rules.Get("entity_knowledge_visibility")[0].Set("facts", Content.Json.Parse("[\"field.observed\",\"field.second\"]"));
            rules.Get("activity_knowledge_visibility")[0].Set("facts", Content.Json.Parse("[\"field.observed\",\"field.second\"]"));
            GameFactory factory = PhysicalFactory(facts: facts.ToString(), visibility: rules.ToString());
            SimGame game = New(factory); Configure(c, game); c.That(Post(game, "field.discover").Accepted, "alternative policy still uses normal native discovery");
            Reach(game, () => Technology(game).Count == 2, "any-fact visibility");
            c.That(!Catalogue(game).Any(x => x.Id == "point.tower"), "all-fact rule waits for every declared observation");
            c.Equal(2, Technology(game).Count, "any-fact rule admits one actual observation");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "all/any derived differences checkpoint without another policy table");
        }
    }
}
