using System;
using System.Collections.Generic;
using System.IO;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    public static class GameModeConfigurationTests
    {
        public static void Register() => TestRunner.Add("Game modes: authored profiles, typed overrides and deterministic resolution", Configuration);

        internal static GameFactory Factory(string properties = null, string profiles = null, string scenarios = null,
            bool reverse = false, bool extra = false, Action<WorldComposer> compose = null, string proofProperties = null)
        {
            var definition = new GameDefinition("game-modes", new GameModule[]
            { new ProbeModule("proof.modes", new ModuleVersion(1, 0, 0), compose: compose), new TycoonModule() });
            JsonValue manifest = Json.Parse(FrameworkProofContent.Read("game-mode-manifest.json"));
            JsonValue documents = manifest.Get("modules")[0].Get("documents");
            if (reverse)
            {
                var reversed = JsonValue.NewArray();
                for (int i = documents.Count - 1; i >= 0; i--) reversed.Add(documents[i]);
                manifest.Get("modules")[0].Set("documents", reversed);
                profiles = ReverseAssignments(profiles ?? FrameworkProofContent.Read("game-modes.json"));
            }
            if (extra) manifest.Get("modules")[0].Get("documents").Add(JsonValue.Of("game-mode-extra.json"));
            var files = new PreloadedFileSource().Add("manifest.json", manifest.ToString())
                .Add("game-mode-properties.json", properties ?? FrameworkProofContent.Read("game-mode-properties.json"))
                .Add("game-mode-proof-properties.json", proofProperties ?? FrameworkProofContent.Read("game-mode-proof-properties.json"))
                .Add("game-modes.json", profiles ?? FrameworkProofContent.Read("game-modes.json"))
                .Add("game-mode-scenarios.json", scenarios ?? FrameworkProofContent.Read("game-mode-scenarios.json"))
                .Add("game-mode-extra.json", FrameworkProofContent.Read("game-mode-extra.json"));
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory, string scenario = "survival")
            => factory.NewGame(new NewGameRequest { Scenario = scenario, Seed = 31 });
        private static GameModeValue Value(IGameModeConfiguration configuration, string id)
        {
            if (!configuration.TryGet(new Code(id), out var value)) throw new InvalidOperationException("missing proof property " + id);
            return value;
        }
        private static GameModeValueSource Source(IGameModeConfiguration configuration, string id)
        {
            foreach (var property in configuration.Properties) if (property.Property.Id == id) return property.Source;
            throw new InvalidOperationException("missing proof source " + id);
        }
        private static string Change(string file, Action<JsonValue> change)
        { JsonValue root = Json.Parse(FrameworkProofContent.Read(file)); change(root); return root.ToString(); }
        private static string ReverseAssignments(string text)
        {
            JsonValue root = Json.Parse(text);
            foreach (int index in new[] { 0, 1, 2 })
            {
                JsonValue original = root.Get("game_modes")[index].Get("properties"), reversed = JsonValue.NewObject();
                for (int i = original.Count - 1; i >= 0; i--) reversed.Set(original.KeyAt(i), original.ValueAt(i));
                root.Get("game_modes")[index].Set("properties", reversed);
            }
            return root.ToString();
        }

        private static void Configuration(Check c)
        {
            IGameModeConfiguration duringComposition = null;
            var factory = Factory(compose: composer => duringComposition = composer.World.GameMode);
            var game = New(factory); var configuration = game.GameMode;
            c.That(configuration is GameModeConfiguration, "the engine owns one immutable configuration class");
            c.That(ReferenceEquals(configuration, duringComposition), "the same configuration exists before composing gameplay providers");
            c.That(ReferenceEquals(configuration, factory.ScenarioGameMode("survival")), "factory preview aliases frozen scenario configuration, not a second resolver store");
            c.Equal("survival", configuration.ModeId, "scenario selects an authored mode identity");
            c.Equal("survival", factory.Scenarios()[0].GameMode, "menu metadata declares mode without a host rule");
            c.Equal(11, configuration.Properties.Count, "native properties share the selected content registry");
            foreach (string property in new[] { "business.values.enabled", "business.money.enabled", "business.production_income.enabled",
                "business.construction_costs.enabled", "business.manufacturing_costs.enabled", "business.running_costs.enabled", "business.trading.enabled", "business.tycoon.enabled" })
            {
                c.Equal(false, Value(configuration, property).Boolean, property + " is explicitly off in survival JSON");
                c.Equal(GameModeValueSource.Profile, Source(configuration, property), property + " keeps profile provenance");
            }
            c.That(!game.World.Services.Has<IEconomy>() && !game.World.Services.Has<IBusinessAccounts>() &&
                !game.World.Services.Has<IResourceValuation>(), "omitted business module binds no fake economy/value service");
            c.Equal(0, game.World.Services.Persistent().Count, "mode configuration adds no mutable service codec");
            c.Equal(0, game.World.Systems.Count, "modes install no loop or scheduler");
            c.That(ReferenceEquals(game.Clock, game.World.Clock), "modes use the existing sole clock");
            c.Equal(Fix.One, Value(configuration, "proof.pressure").Number, "omitted numeric profile value uses explicit property JSON default");
            c.Equal("careful", Value(configuration, "proof.strategy").Text, "text values have an authored domain/default");
            c.Equal(GameModeValueSource.PropertyDefault, Source(configuration, "proof.pressure"), "property-default provenance is distinct");
            c.That(!configuration.TryGet(new Code("unknown"), out var unavailable), "unknown properties are unavailable, not zero/false");
            c.Equal(GameModeValueKind.Unavailable, unavailable.Kind, "default value has explicit unavailable kind");
            c.Throws<InvalidOperationException>(() => { _ = unavailable.Boolean; }, "unavailable cannot masquerade as disabled");
            c.Throws<InvalidOperationException>(() => { _ = Value(configuration, "proof.pressure").Boolean; }, "wrong-kind access refuses");
            c.Throws<NotSupportedException>(() => ((IList<GameModePropertyView>)configuration.Properties).Clear(), "retained properties cannot mutate");

            var expedition = New(factory, "expedition"); var custom = New(factory, "custom");
            c.Equal(Fix.FromInt(3), Value(expedition.GameMode, "proof.pressure").Number, "profile replaces authored default");
            c.Equal(GameModeValueSource.Profile, Source(expedition.GameMode, "proof.pressure"), "profile override provenance");
            c.Equal(Fix.FromInt(2), Value(custom.GameMode, "proof.pressure").Number, "scenario permitted override replaces profile");
            c.Equal(GameModeValueSource.Scenario, Source(custom.GameMode, "proof.pressure"), "scenario override provenance");
            c.Equal("careful", Value(custom.GameMode, "proof.strategy").Text, "scenario text override is constrained and typed");
            c.Equal(Fix.FromInt(3), Value(expedition.GameMode, "proof.pressure").Number, "one scenario cannot mutate another's shared profile");
            c.Equal(Fix.One, Value(game.GameMode, "proof.pressure").Number, "interleaved configuration stays isolated");
            c.That(game.Hash() != expedition.Hash(), "authoritative hash includes different mode configuration");
            var twin = factory.Load(custom.Save());
            c.Equal(custom.Hash(), twin.Hash(), "mode identity and typed values use current save/hash protocol");
            c.Equal(GameModeValueSource.Scenario, Source(twin.GameMode, "proof.pressure"), "saved configuration retains authored override provenance");
            custom.RunTicks(4); twin.RunTicks(1); game.RunTicks(2); twin.RunTicks(3);
            c.Equal(custom.Hash(), twin.Hash(), "saved mode sessions continue on the ordinary loop");
            c.Equal(Fix.One, Value(game.GameMode, "proof.pressure").Number, "updates do not mutate mode settings");
            c.That(New(factory, "defaults").GameMode != null, "every scenario selects an authored configuration");
            c.Equal(GameModeValueSource.PropertyDefault, Source(New(factory, "defaults").GameMode, "business.money.enabled"), "empty authored preset uses file defaults");
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "source identity is current");
            c.Equal(StateCodecTests.ExpectedSaveFormat, EngineInfo.SaveFormat, "derived mode identity has a direct breaking checkpoint layout");

            var reordered = New(Factory(reverse: true), "custom");
            c.Equal(custom.GameMode.Properties.Count, reordered.GameMode.Properties.Count, "reverse document/key order resolves same number of settings");
            for (int i = 0; i < custom.GameMode.Properties.Count; i++)
            {
                c.Equal(custom.GameMode.Properties[i].Property, reordered.GameMode.Properties[i].Property, "property order is stable ordinal identity");
                c.Equal(custom.GameMode.Properties[i].Value, reordered.GameMode.Properties[i].Value, "assignment order does not alter effective value");
            }
            c.Equal(Value(New(Factory(extra: true), "community").GameMode, "proof.new-rule").Boolean, true, "an extra master-listed file enrolls a property without scheduler edits");
            c.Equal("community-mode", New(Factory(extra: true), "community").GameMode.ModeId, "mode IDs are open content, not a hard-coded game enum");

            // Fault instrumentation exercises the same identity protocol, not a parallel serializer.
            using (var stream = new MemoryStream())
            {
                GameModeConfiguration.SyncIdentity(new StateWriter(stream), expedition.GameMode); byte[] bytes = stream.ToArray();
                c.Throws<CorruptStateException>(() => GameModeConfiguration.SyncIdentity(new StateReader(bytes, 0, bytes.Length), configuration), "different saved profile refuses rather than overwriting live rules");
            }
            using (var stream = new MemoryStream())
            {
                GameModeConfiguration.SyncIdentity(new StateWriter(stream), custom.GameMode); byte[] bytes = stream.ToArray();
                var different = Factory(scenarios: Change("game-mode-scenarios.json", root =>
                    root.Get("scenarios")[2].Get("mode_overrides").Set("proof.pressure", JsonValue.Number("4")))).ScenarioGameMode("custom");
                c.Throws<CorruptStateException>(() => GameModeConfiguration.SyncIdentity(new StateReader(bytes, 0, bytes.Length), different), "same profile/provenance with different override values refuses");
            }

            RejectProperties(c, r => r.Get("game_mode_properties")[0].Remove("default"), "missing explicit default refuses");
            RejectProperties(c, r => r.Get("game_mode_properties")[0].Set("default", JsonValue.Null()), "null is not a Boolean default");
            RejectProperties(c, r => r.Get("game_mode_properties")[0].Set("capability", JsonValue.Of("missing.module")), "unknown registered capability refuses even if disabled");
            RejectProperties(c, r => r.Get("game_mode_properties")[0].Set("id", JsonValue.Of("BAD ID")), "invalid property identities refuse");
            RejectProperties(c, r => r.Get("game_mode_properties")[0].Set("kind", JsonValue.Of("object")), "arbitrary JSON objects cannot become gameplay settings");
            RejectProofProperties(c, r => r.Get("game_mode_properties")[0].Set("minimum", JsonValue.Number("11")), "invalid numeric domain refuses");
            RejectProofProperties(c, r => r.Get("game_mode_properties")[0].Set("default", JsonValue.Number("-1")), "numeric default outside bounds refuses");
            RejectProofProperties(c, r => r.Get("game_mode_properties")[1].Set("default", JsonValue.Of("unknown")), "unknown constrained text default refuses");
            RejectProofProperties(c, r => r.Get("game_mode_properties")[1].Set("allowed", JsonValue.NewArray()), "empty text domain refuses");
            RejectProperties(c, r => r.Get("game_mode_properties")[2].Get("requires_when_true").Add(JsonValue.Of("missing")), "missing prerequisite reference refuses");
            RejectProperties(c, r => r.Get("game_mode_properties")[2].Get("requires_when_true").Add(JsonValue.Of("proof.pressure")), "non-Boolean prerequisite refuses");
            RejectProperties(c, r => r.Get("game_mode_properties")[2].Get("requires_when_true").Add(JsonValue.Of("business.money.enabled")), "duplicate prerequisite refuses");
            RejectProfiles(c, r => r.Get("game_modes")[0].Get("properties").Set("business.production_income.enabled", JsonValue.Of(true)), "income-on never auto-enables missing money/values");
            RejectProfiles(c, r => r.Get("game_modes")[0].Get("properties").Set("unknown", JsonValue.Of(false)), "unknown profile assignment refuses");
            RejectProfiles(c, r => r.Get("game_modes")[1].Get("properties").Set("proof.pressure", JsonValue.Of("3")), "profile value cannot change schema kind");
            RejectScenarios(c, r => r.Get("scenarios")[0].Set("game_mode", JsonValue.Of("missing")), "unknown selected mode refuses");
            RejectScenarios(c, r => r.Get("scenarios")[0].Get("mode_overrides").Set("proof.locked", JsonValue.Of(false)), "locked property rejects even an equal-value override");
            RejectScenarios(c, r => r.Get("scenarios")[0].Get("mode_overrides").Set("proof.pressure", JsonValue.Number("11")), "scenario number outside range refuses");
            RejectScenarios(c, r => r.Get("scenarios")[0].Get("mode_overrides").Set("proof.pressure", JsonValue.Number("1000000000000")), "fixed-point overflow refuses");
            RejectScenarios(c, r => r.Get("scenarios")[0].Get("mode_overrides").Set("business.trading.enabled", JsonValue.Of(true)), "trade-on with money-off refuses coherently");
            RejectScenarios(c, r => r.Get("scenarios")[0].Remove("game_mode"), "overrides without a profile refuse");
            c.Throws<ContentException>(() => Json.Parse("{\"game_modes\":[],\"game_modes\":[]}"), "duplicate JSON keys refuse before resolution");
        }
        private static void RejectProperties(Check c, Action<JsonValue> change, string why)
            => c.Throws<ContentException>(() => New(Factory(properties: Change("game-mode-properties.json", change))), why);
        private static void RejectProofProperties(Check c, Action<JsonValue> change, string why)
            => c.Throws<ContentException>(() => New(Factory(proofProperties: Change("game-mode-proof-properties.json", change))), why);
        private static void RejectProfiles(Check c, Action<JsonValue> change, string why)
            => c.Throws<ContentException>(() => New(Factory(profiles: Change("game-modes.json", change))), why);
        private static void RejectScenarios(Check c, Action<JsonValue> change, string why)
            => c.Throws<ContentException>(() => New(Factory(scenarios: Change("game-mode-scenarios.json", change))), why);
    }
}
