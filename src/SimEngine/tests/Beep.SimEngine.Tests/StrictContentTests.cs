using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Maps;

namespace Beep.SimEngine.Tests
{
    public static class StrictContentTests
    {
        public static void Register()
        {
            TestRunner.Add("Strict content: removed overlays, collisions and owner diagnostics", Refusals);
            TestRunner.Add("Strict content: explicit files, selected definitions and saved player sessions", Selection);
        }

        private static ContentSet Load(string extra, string manifest = null, string resources = "{}")
        {
            var files = new PreloadedFileSource().Add("manifest.json", manifest ?? TestGame.Manifest)
                .Add("test/defs.json", TestGame.Defs).Add("extra/defs.json", extra)
                .Add("extra/resources.json", resources)
                .Add("extra/unlisted.json", "{\"not_a_registered_section\": []}");
            return ContentSet.Load(files, TestGame.Definition().ContentTypes);
        }

        private static string Refused(Check c, ContentSet content, string label)
        {
            try { content.Resolve(new[] { "test", "extra" }); c.That(false, label); return ""; }
            catch (ContentException error) { c.That(true, label); return error.Message; }
        }

        private static void Refusals(Check c)
        {
            const string overlay = "{\"patches\": [{\"section\": \"entities\", \"id\": \"bin\", \"merge\": {\"kind\": \"marker\"}}]}";
            string message = Refused(c, Load(overlay), "old overlay has no loader path");
            c.That(message.Contains("unknown section 'patches'") && message.Contains("extra@1.0.0 extra/defs.json"), "old schema failure names owner/version/file");
            Refused(c, Load("{\"patches\": []}"), "even an empty old section is not accepted");
            Refused(c, Load("{\"entities\": [{\"id\": \"extra.bin\", \"kind\": \"structure\", \"merge\": {}}]}"), "embedded overlay fields do not become aliases");
            c.That(typeof(JsonValue).GetMethod("MergeFrom") == null, "public recursive overlay API is removed, not wrapped");

            ContentSet collision = Load(TestGame.ExtraDefs.Replace("extra.bin", "bin"));
            ResolvedContent original = collision.Resolve(new[] { "test" });
            c.Equal(Fix.Half, original.Get<EntityDef>("bin").Component<CounterDef>().PerTick, "inactive extension cannot mutate original");
            message = Refused(c, collision, "cross-owner duplicate refuses instead of replacing");
            foreach (string detail in new[] { "extra@1.0.0", "extra/defs.json:entities[0](bin)", "declared twice", "'test'", "'extra'" })
                c.That(message.Contains(detail), "collision diagnostic contains " + detail);
            c.That(ReferenceEquals(original, collision.Resolve(new[] { "test" })), "failed resolution does not replace cached selected content");
            c.Equal(Fix.Half, original.Get<EntityDef>("bin").Component<CounterDef>().PerTick, "failed extension leaves selected definition unchanged");
            c.Equal(message, Refused(c, collision, "repeated invalid resolution refuses again"), "failure is deterministic, not a poisoned partial cache");

            Refused(c, Load("{\"entities\": [{\"id\": \"extra.bin\"}, {\"id\": \"extra.bin\"}]}"), "same-file duplicate also refuses");
            string multiple = TestGame.Manifest.Replace("[\"extra/defs.json\"]", "[\"extra/defs.json\", \"extra/resources.json\"]");
            message = Refused(c, Load(TestGame.ExtraDefs, multiple, TestGame.ExtraDefs), "cross-file duplicate also refuses");
            c.That(message.Contains("extra/resources.json:entities[0](extra.bin)"), "cross-file collision identifies the competing entry");
            c.Throws<ContentException>(() => Load(TestGame.ExtraDefs, multiple.Replace("extra/resources.json", "test/defs.json")), "a manifest cannot assign one document to two owners");
        }

        private static void Selection(Check c)
        {
            const string resources = "{\"units\": [{\"id\":\"unit.crystal\",\"dimension\":\"count\",\"scale\":{\"numerator\":1,\"denominator\":1},\"discrete\":true}], \"resources\": [{\"id\":\"extra.crystal\",\"unit\":\"unit.crystal\",\"category\":\"general\",\"phase\":\"solid\",\"traits\":[],\"capacity\":{\"unit.crystal\":1}}]}";
            string multiple = TestGame.Manifest.Replace("[\"extra/defs.json\"]", "[\"extra/resources.json\", \"extra/defs.json\"]");
            ContentSet set = Load(TestGame.ExtraDefs, multiple, resources);
            ResolvedContent basic = set.Resolve(new[] { "test" });
            ResolvedContent extended = set.Resolve(new[] { "test", "extra" });
            c.That(basic.Find<EntityDef>("extra.bin") == null && basic.Find<ResourceDef>("extra.crystal") == null, "inactive definitions are absent from the selected catalog");
            c.Throws<ContentException>(() => basic.ResourceId("extra.crystal"), "inactive resource lookup refuses explicitly rather than fabricating an ID");
            c.Equal("extra", extended.Get<EntityDef>("extra.bin").Module, "new entity retains its declared owner");
            c.Equal("extra", extended.Get<ResourceDef>("extra.crystal").Module, "another listed file contributes to the same catalog");
            c.Equal(Fix.FromInt(2), extended.Get<EntityDef>("extra.bin").Component<CounterDef>().PerTick, "new prototype uses an existing provider contract");
            c.Equal(Fix.Half, extended.Get<EntityDef>("bin").Component<CounterDef>().PerTick, "base configuration is not overwritten");
            c.That(ReferenceEquals(extended, set.Resolve(new[] { "extra", "test" })), "selected module order aliases the same immutable cached catalog");
            c.That(basic.Fingerprint != extended.Fingerprint, "selected file ownership identifies distinct content");
            c.Equal(3, extended.ResourceCount, "listed resource file extends existing resource IDs without scanning unlisted files");
            c.Throws<NotSupportedException>(() => ((IList<string>)extended.Modules).Clear(), "selected module identity is immutable");
            c.Throws<NotSupportedException>(() => ((IList<ComponentDef>)extended.Get<EntityDef>("extra.bin").Components).Clear(), "shared component declarations are immutable");
            ResolvedContent reversed = Load(TestGame.ExtraDefs, multiple.Replace("[\"extra/resources.json\", \"extra/defs.json\"]", "[\"extra/defs.json\", \"extra/resources.json\"]"), resources).Resolve(new[] { "test", "extra" });
            c.Equal(extended.Fingerprint, reversed.Fingerprint, "canonical document order is independent of manifest enumeration");
            c.Equal(extended.ResourceId("extra.crystal"), reversed.ResourceId("extra.crystal"), "resource indices use the same canonical order");

            var factory = new GameFactory(TestGame.Definition(), set);
            SimGame a = factory.NewGame(new NewGameRequest { Scenario = "flat", Mods = { "extra" }, Seed = 9 });
            SimGame b = factory.NewGame(new NewGameRequest { Scenario = "flat", Mods = { "extra" }, Seed = 9 });
            a.Seat(0).Submit(new PlaceBin { At = new TileCoord(20, 10) });
            b.Seat(0).Submit(new PlaceBin { At = new TileCoord(20, 10) });
            a.RunTicks(13); b.RunTicks(5); b.RunTicks(8);
            c.Equal(a.Hash(), b.Hash(), "extension selection preserves ordinary player-command determinism");
            SimGame saved = factory.Load(a.Save());
            c.Equal(a.Hash(), saved.Hash(), "selected extension identity restores without an overlay replay");
            a.RunTicks(27); saved.RunTicks(11); b.RunTicks(27); saved.RunTicks(16);
            c.Equal(a.Hash(), saved.Hash(), "restored engine continues the same update path");
            c.Equal(a.Hash(), b.Hash(), "interleaved sessions keep independent live state");
            var views = new List<EntityView>(); a.Observer.CopyEntities(views);
            c.Equal(3, views.Count, "only physical player placement adds an entity; catalog entries are not auto-spawned");
            var changed = new GameFactory(TestGame.Definition(), Load(TestGame.ExtraDefs.Replace("\"per_tick\": 2", "\"per_tick\": 3"), multiple, resources));
            c.That(!changed.Describe(a.Save()).Compatible, "changed active authored content fails exact save identity");
            c.Throws<CorruptStateException>(() => changed.Load(a.Save()), "save load refuses a different active definition set");
            SimGame unextended = factory.NewGame(new NewGameRequest { Scenario = "flat", Seed = 9 });
            c.That(changed.Describe(unextended.Save()).Compatible, "changing inactive content leaves selected base save identity intact");
        }
    }
}
