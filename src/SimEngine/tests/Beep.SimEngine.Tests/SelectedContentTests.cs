using System;
using System.Collections.Generic;
using Beep.SimEngine.App;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Tests
{
    public static class SelectedContentTests
    {
        private const string Manifest = "{\"schema\":1,\"modules\":["
            + "{\"id\":\"test\",\"version\":\"1.0.0\",\"documents\":[\"test/defs.json\"]},"
            + "{\"id\":\"unused\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"test\",\"version\":\"1.0.0\"}],\"documents\":[\"unused/defs.json\"]}]}";
        private const string Scenario = "{\"id\":\"extension\",\"game_mode\":\"test.resource-only\",\"mode_overrides\":{},\"name\":\"Extension\",\"time\":\"test.minute\",\"space\":\"flat.tiles\",\"modules\":[\"unused\"],\"seats\":2}";
        private const string Valid = "{\"scenarios\": [" + Scenario + "]}";

        public static void Register()
        {
            TestRunner.Add("Selected content: inactive providers and engine-only discovery", Discovery);
            TestRunner.Add("Selected content: selected dependencies, refusal and saved session identity", Sessions);
        }

        private static GameFactory Factory(DiscoveryProbe probe, string unused = Valid, string basis = null, bool reverse = false)
        {
            GameModule module = probe.Module();
            var definition = new GameDefinition("selected", reverse
                ? new GameModule[] { module, new TestModule() } : new GameModule[] { new TestModule(), module });
            var files = new PreloadedFileSource().Add("manifest.json", Manifest)
                .Add("test/defs.json", basis ?? TestGame.Defs).Add("unused/defs.json", unused);
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }

        private static void Untouched(Check c, DiscoveryProbe probe, string label)
        {
            c.That(typeof(ContentTypes).GetMethod("Generator") == null, label + " has no catalog generator callback");
            c.Equal(0, probe.Compositions, label + " never composes unselected providers");
            c.Equal(0, probe.Initializations, label + " never initializes unselected systems");
        }

        private static void Discovery(Check c)
        {
            foreach (string trap in new[] {
                "\"unused_reads\": [{\"id\":\"trap\"}]",
                "\"unused_resolves\": [{\"id\":\"trap\"}]",
                "\"entities\": [{\"id\":\"trap\",\"components\": {\"unregistered\": {}}}]",
                "\"patches\": []" })
            {
                var probe = new DiscoveryProbe();
                string unused = Valid.Replace("{\"scenarios\"", "{" + trap + ",\"scenarios\"");
                GameFactory factory = Factory(probe, unused);
                IReadOnlyList<ScenarioInfo> menu = factory.Scenarios();
                c.Equal(2, menu.Count, "menu reads only scenario intent despite " + trap);
                c.Equal("flat.tiles", menu[1].Space, "unresolved authored space identity is projected without composing it");
                c.Equal("unused", menu[1].Modules[0], "menu preserves authored module request, not an invented dependency selection");
                c.That(factory.InputMap().TryMap("q", out InputAction quit) && quit.Action == "app.exit", "core clock/quit input does not resolve gameplay");
                c.Equal(TestGame.Time.Ticks(GameDuration.Minutes(1)), factory.ScenarioTime("flat").Ticks(GameDuration.Minutes(1)), "scenario clock resolves only declared active providers");
                SimGame game = factory.NewGame(new NewGameRequest { Scenario = "flat", Seed = 17 });
                game.RunTicks(7);
                c.Equal(7L, game.Tick, "valid selected game advances normally despite broken unused gameplay");
                Untouched(c, probe, "engine-only discovery/selected start");
                c.Throws<NotSupportedException>(() => ((IList<ScenarioInfo>)menu).Clear(), "menu collection is immutable");
                c.Throws<NotSupportedException>(() => ((IList<string>)menu[1].Modules).Clear(), "nested requested module list is immutable");
                foreach (var property in typeof(ScenarioInfo).GetProperties())
                    c.That(property.SetMethod == null, "metadata is detached read-only intent: " + property.Name);
                c.Throws<ContentException>(() => factory.NewGame(new NewGameRequest { Scenario = "extension" }), "selecting broken provider performs strict normal validation");
                c.Equal(0, probe.Compositions, "invalid selected content refuses before provider Compose");
                c.Equal(0, probe.Initializations, "invalid selected content refuses before Initialize");
            }

            var untouched = new DiscoveryProbe();
            GameFactory brokenMetadata = Factory(untouched, "{\"scenarios\":[{\"id\":\"broken\",\"modules\":[\"unused\"]}]}" );
            c.Equal(0L, brokenMetadata.NewGame(new NewGameRequest { Scenario = "flat" }).Tick, "unrelated bad scenario body does not prevent selecting a valid ID");
            c.Throws<ContentException>(() => brokenMetadata.NewGame(new NewGameRequest { Scenario = "broken" }), "selected malformed metadata refuses");
            c.Throws<ContentException>(() => brokenMetadata.Scenarios(), "listing bad menu metadata reports an error rather than inventing fields");
            Untouched(c, untouched, "malformed inactive scenario selection");

            string inputs = TestGame.Defs.Replace("\"scenarios\": [", "\"input_maps\": [{\"id\":\"default\",\"bindings\":{\"x\":\"app.exit\"}}], \"scenarios\": [");
            var inputProbe = new DiscoveryProbe();
            GameFactory inputFactory = Factory(inputProbe, Valid.Replace("{\"scenarios\"", "{\"unused_reads\":[{\"id\":\"trap\"}],\"scenarios\""), inputs);
            c.That(inputFactory.InputMap().TryMap("x", out InputAction mapped) && mapped.Action == "app.exit", "authored core input map uses its existing reader without provider resolution");
            Untouched(c, inputProbe, "authored input discovery");
        }

        private static void Sessions(Check c)
        {
            var probe = new DiscoveryProbe(); GameFactory factory = Factory(probe);
            factory.Scenarios(); factory.InputMap();
            Untouched(c, probe, "menu intent");
            c.That(factory.ScenarioTime("extension") != null, "clock selection includes captured transitive dependencies");
            c.That(typeof(ContentTypes).GetMethod("Generator") == null, "selected catalog admits only authored documents");
            c.Equal(0, probe.Compositions, "clock query creates no live world");
            SimGame a = factory.NewGame(new NewGameRequest { Scenario = "extension", Seed = 13 });
            SimGame b = factory.NewGame(new NewGameRequest { Scenario = "extension", Seed = 13 });
            c.That(ReferenceEquals(a.World.Content, b.World.Content), "both sessions reuse the sole immutable selected catalog");
            c.Equal(2, probe.Compositions, "each selected session composes its own provider");
            c.Equal(2, probe.Initializations, "each selected session initializes normally");
            c.Equal("test,unused", string.Join(",", a.World.Content.Modules), "selected dependency order is canonical");
            c.That(ReferenceEquals(a.World.Content, b.World.Content), "live sessions share only canonical immutable definitions");
            a.Seat(0).Submit(new PlaceBin { At = new TileCoord(20, 10) });
            b.Seat(0).Submit(new PlaceBin { At = new TileCoord(20, 10) });
            a.RunTicks(13); b.RunTicks(4); b.RunTicks(9);
            c.Equal(a.Hash(), b.Hash(), "same player stream is deterministic through selected composition");
            byte[] save = a.Save(); SimGame loaded = factory.Load(save);
            c.Equal(a.Hash(), loaded.Hash(), "saved selected modules restore the same facts");
            a.RunTicks(37); b.RunTicks(37); loaded.RunTicks(20); loaded.RunTicks(17);
            c.Equal(a.Hash(), b.Hash(), "interleaved sessions remain isolated");
            c.Equal(a.Hash(), loaded.Hash(), "saved continuation remains identical");
            c.That(factory.Describe(save).Compatible, "saved exact selected identities describe correctly");
            var reversedProbe = new DiscoveryProbe(); GameFactory reversed = Factory(reversedProbe, reverse: true);
            SimGame reversedLoad = reversed.Load(save);
            c.Equal(factory.Load(save).Hash(), reversedLoad.Hash(), "code module enumeration does not change saved selection order");

            var added = new DiscoveryProbe(); GameFactory extraFactory = Factory(added);
            extraFactory.NewGame(new NewGameRequest { Scenario = "flat", Mods = { "unused", "unused" } });
            c.Equal(1, added.Initializations, "explicit repeated mod requests initialize their provider once");
            c.Equal(1, added.Compositions, "one selected module has one composition responsibility");

            foreach (string invalid in new[] {
                "{\"scenarios\":[" + Scenario.Replace("flat.tiles", "missing.space") + "]}",
                "{\"scenarios\":[" + Scenario.Replace("test.minute", "missing.time") + "]}",
                "{\"scenarios\":[" + Scenario.Replace("\"unused\"]", "\"test\"]") + "]}" })
            {
                var refused = new DiscoveryProbe(); GameFactory invalidFactory = Factory(refused, invalid);
                c.Throws<ContentException>(() => invalidFactory.NewGame(new NewGameRequest { Scenario = "extension" }), "invalid selected references/owner refuse before initialization");
                c.Equal(0, refused.Compositions, "invalid selected intent does not compose");
                c.Equal(0, refused.Initializations, "invalid selected intent does not initialize");
            }
            var missing = new DiscoveryProbe();
            GameFactory missingModule = Factory(missing, Valid.Replace("\"unused\"]", "\"missing\"]"));
            c.Throws<InvalidOperationException>(() => missingModule.NewGame(new NewGameRequest { Scenario = "extension" }), "unknown selected module refuses through captured composition graph");
            Untouched(c, missing, "unknown module selection");
            var duplicate = new DiscoveryProbe(); GameFactory ambiguous = Factory(duplicate, Valid.Replace("\"extension\"", "\"flat\""));
            c.Throws<ContentException>(() => ambiguous.NewGame(new NewGameRequest { Scenario = "flat" }), "duplicate scenario identity refuses before any provider selection");
            c.Throws<ContentException>(() => ambiguous.Scenarios(), "menu never silently substitutes duplicate identity");
            Untouched(c, duplicate, "duplicate scenario identity");
            c.Throws<ArgumentException>(() => factory.NewGame(new NewGameRequest { Scenario = "absent" }), "unknown scenario retains explicit refusal");
        }
    }
}
