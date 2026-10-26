using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Commands;

namespace Beep.SimEngine.Tests
{
    public static class ContentOwnershipTests
    {
        public static void Register()
        {
            TestRunner.Add("Content ownership: work plans, transformations and ports expose frozen collections", Collections);
            TestRunner.Add("Content ownership: generated dependencies and connections capture caller data", Generated);
            TestRunner.Add("Content ownership: nested authored JSON and settings are sealed across sessions", JsonOwnership);
            TestRunner.Add("Content ownership: interleaved physical construction and saved twins", Sessions);
        }

        private static void Frozen<T>(Check c, IReadOnlyList<T> values, string label)
        {
            c.That(!(values is T[]), label + " is not an exposed array");
            c.Throws<NotSupportedException>(() => ((IList<T>)values).Clear(), label + " rejects removal");
            if (values.Count > 0)
                c.Throws<NotSupportedException>(() => ((IList<T>)values)[0] = default, label + " rejects replacement");
        }

        private static void Collections(Check c)
        {
            SimGame game = Colony.New(); ResolvedContent content = game.World.Content;
            c.That(!content.Owns(null), "absent definition is not an owned catalog alias");
            c.That(!content.Owns(new BasicResourceDef()), "unread resource refuses ownership without evaluating unresolved resource properties");
            c.That(!content.Owns(new SpatialLayerDef()), "unread layer refuses ownership without a null-key lookup");
            c.That(content.Owns(content.Get<IResourceContentDefinition>("steel")), "actual frozen resource remains the original owned alias");
            ScenarioDef scenario = content.Get<ScenarioDef>("colony");
            Frozen(c, scenario.Modules, "scenario modules");
            c.That(scenario.HasModule("sim.work") && !scenario.HasModule("absent"), "module lookup uses sealed declaration");
            TransformerDef transformer = content.Get<EntityDef>("factory").Component<TransformerDef>();
            Frozen(c, transformer.Transformations, "transformer transformations");
            c.That(new List<ITransformationDefinition>(transformer.Transformations).Exists(r => r.Id == "build_truck")
                && !new List<ITransformationDefinition>(transformer.Transformations).Exists(r => r.Id == "absent"), "transformation lookup retains authored selection");
            ActivityDef order = content.Get<ActivityDef>("inspect");
            Frozen(c, order.TargetTags, "order target tags"); Frozen(c, order.Graph.Nodes, "order steps");
            c.That(order.Graph.Nodes[0].Requirement == null, "unguarded activity has no fabricated requirement");
            ConstructibleDef build = content.Get<EntityDef>("tower").Component<ConstructibleDef>();
            Frozen(c, build.Graph.Nodes, "construction steps"); Frozen(c, build.Demolition.Graph.Nodes, "demolition steps");
            Frozen(c, build.Graph.Nodes[1].Dependencies, "dependency IDs"); Frozen(c, build.Graph.Nodes[1].Dependencies, "resolved dependency indices");
            c.Equal("prep", build.Graph.Find("erect").Dependencies[0], "erect still requires preparation");
            INetworkDefinition network = content.Get<INetworkDefinition>("waternet");
            Frozen(c, network.Resources, "network resources"); c.That(network.Carries(content.ResourceId("water")), "network resource lookup remains correct");
            foreach (string id in new[] { "tank", "sink" })
            {
                INetworkPortsDefinition ports = content.Get<EntityDef>(id).Component<INetworkPortsDefinition>();
                Frozen(c, ports.Connections, id + " port connections");
                Frozen(c, ports.Connections[0].Push, id + " push resources"); Frozen(c, ports.Connections[0].Pull, id + " pull resources");
            }
        }

        private static void Generated(Check c)
        {
            var after = new[] { "prepare" };
            SimGame game = Colony.New();
            IActivityNodeDefinition step = ActivityNodeDef.Make("build", game.World.Content.Get<IActivityExecutor>("work.timed"), Array.Empty<WorkerRoleId>(), GameDuration.Minutes(5), after, null);
            after[0] = "changed";
            c.Equal("prepare", step.Dependencies[0], "generated step owns dependency values, not caller array");
            Frozen(c, step.Dependencies, "generated dependencies");
            INetworkDefinition network = game.World.Content.Get<INetworkDefinition>("waternet");
            ResourceId water = game.World.Content.ResourceId("water"), steel = game.World.Content.ResourceId("steel");
            var push = new[] { water }; var pull = new List<ResourceId> { water };
            var connection = new PortSpec(network, push, pull);
            push[0] = steel; pull.Clear();
            c.Equal(water, connection.Push[0], "port captures caller push array");
            c.Equal(water, connection.Pull[0], "port captures caller pull list");
            Frozen(c, connection.Push, "generated push"); Frozen(c, connection.Pull, "generated pull");
            foreach (var property in typeof(PortSpec).GetProperties()) c.That(property.SetMethod == null, "connection identity immutable: " + property.Name);
        }

        private static void JsonOwnership(Check c)
        {
            const string definitions = "{" + TestGame.TimeDefinitions + ProjectionModule.SpatialDefinitions + @"
              ""entity_states"": [{ ""id"": ""idle"", ""state"": ""state.idle"", ""priority"": 0, ""default"": true }],
              ""entities"": [{ ""id"": ""marker"", ""kind"": ""marker"", ""components"": {
                ""tile_presence"": { ""space"": ""projection.tiles"", ""width"": 1, ""height"": 1, ""blocks"": false, ""movable"": false }
              }}],
              ""raw_probes"": [{ ""id"": ""probe"", ""payload"": { ""nested"": { ""value"": 7 }, ""resources"": [{ ""value"": 9 }] } }],
              ""game_modes"": [{ ""id"": ""content-ownership.resource-only"", ""name"": ""Physical diagnostic game"", ""properties"": {} }],
              ""scenarios"": [{ ""id"": ""projection"", ""game_mode"": ""content-ownership.resource-only"", ""mode_overrides"": {}, ""time"": ""test.minute"", ""space"": ""projection.tiles"", ""modules"": [""projection""], ""seats"": 2, ""default_seats"": 2,
                ""settings"": { ""projection"": { ""nested"": { ""value"": 7 }, ""resources"": [{ ""value"": 9 }] } } }]
            }";
            var definition = new GameDefinition("ownership", new GameModule[] { new ProjectionModule(false) });
            var files = new PreloadedFileSource().Add("framework/access.json", FrameworkProofContent.Access).Add("manifest.json", "{ \"schema\": 1, \"modules\": [{ \"id\": \"projection\", \"version\": \"1.0.0\", \"documents\": [\"defs.json\"] }] }")
                .Add("defs.json", definitions);
            ContentSet set = ContentSet.Load(files, definition.ContentTypes);
            ResolvedContent content = set.Resolve(new[] { "projection" });
            JsonValue payload = content.Get<RawContentProbeDef>("probe").Payload;
            AssertJsonFrozen(c, payload, "external provider");
            ScenarioDef scenario = content.Get<ScenarioDef>("projection"); JsonReader settings = scenario.SettingsFor("projection");
            JsonValue nested = settings.Object("nested"), resources = settings.Array("resources"); settings.End();
            c.Throws<InvalidOperationException>(() => nested.Set("value", JsonValue.Number("0")), "nested settings cannot change shared scenario");
            c.Throws<InvalidOperationException>(() => resources.Add(JsonValue.Null()), "settings arrays are frozen");
            c.Throws<InvalidOperationException>(() => resources[0].Remove("value"), "settings array children are frozen");
            JsonValue clone = payload.Clone(); clone.Set("local", JsonValue.Of(true)); clone.Get("nested").Remove("value");
            c.That(!payload.Has("local") && payload.Get("nested").Has("value"), "explicit detached JSON copy remains editable without touching definition");
            // A direct Read captures its own settings, too: retaining the author's input is not an escape.
            JsonValue raw = Json.Parse("{ \"time\": \"test.minute\", \"modules\": [\"projection\"], \"settings\": { \"projection\": { \"values\": [7] } } }");
            var standalone = new ScenarioDef(); var reader = new JsonReader(raw, "scenario"); standalone.Read(reader); reader.End();
            raw.Get("settings").Get("projection").Get("values").Add(JsonValue.Number("8"));
            c.Equal(1, standalone.SettingsFor("projection").Array("values").Count, "scenario captures caller's nested settings");
            var factory = new GameFactory(definition, set); SimGame a = factory.NewGame(new NewGameRequest { Scenario = "projection" });
            SimGame b = factory.NewGame(new NewGameRequest { Scenario = "projection" });
            a.RunTicks(10); b.RunTicks(10);
            c.Equal(a.Hash(), b.Hash(), "retained provider JSON does not diverge shared-content sessions");
            c.Equal(a.Hash(), factory.Load(a.Save()).Hash(), "frozen settings restore with unchanged content identity");
        }

        private static void AssertJsonFrozen(Check c, JsonValue payload, string label)
        {
            c.Throws<InvalidOperationException>(() => payload.Set("extra", JsonValue.Null()), label + " cannot set root key");
            c.Throws<InvalidOperationException>(() => payload.Remove("nested"), label + " cannot remove root key");
            c.Throws<InvalidOperationException>(() => payload.Get("nested").Set("value", JsonValue.Number("0")), label + " cannot change nested object");
            c.Throws<InvalidOperationException>(() => payload.Get("resources").Add(JsonValue.Null()), label + " cannot change nested array");
            c.Throws<InvalidOperationException>(() => payload.Get("resources")[0].Remove("value"), label + " cannot change array's nested object");
        }

        private static void Sessions(Check c)
        {
            GameFactory factory = Colony.Factory();
            SimGame a = factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 7 });
            SimGame b = factory.NewGame(new NewGameRequest { Scenario = "colony", Seed = 7 });
            c.That(ReferenceEquals(a.World.Content, b.World.Content), "sessions intentionally share frozen definitions, not duplicate content stores");
            c.That(TestKit.Order(a, 0, new ConstructCommand { Def = "tower", Placement = Beep.SimEngine.Maps.TilePlacement.Arguments(a.World.Space.Id, new TileCoord(10, 16)), Priority = 50 }).Accepted, "first construction accepted");
            c.That(TestKit.Order(b, 0, new ConstructCommand { Def = "tower", Placement = Beep.SimEngine.Maps.TilePlacement.Arguments(b.World.Space.Id, new TileCoord(10, 16)), Priority = 50 }).Accepted, "second construction accepted");
            a.RunTicks(31); b.RunTicks(31); SimGame restored = factory.Load(a.Save());
            for (int block = 0; block < 90; block++)
            {
                a.RunTicks(20); restored.RunTicks(7); b.RunTicks(20); restored.RunTicks(13);
            }
            c.Equal(a.Hash(), b.Hash(), "interleaved sessions retain identical work dependencies");
            c.Equal(a.Hash(), restored.Hash(), "mid-project restore retains identical physical progress");
            c.Equal(1, Colony.Mine(a, 0, "tower").Count, "work still physically completes with sealed plans");
        }
    }
}
