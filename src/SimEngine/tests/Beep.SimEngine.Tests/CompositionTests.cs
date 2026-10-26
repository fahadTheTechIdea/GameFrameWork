using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;

namespace Beep.SimEngine.Tests
{
    public static class CompositionTests
    {
        private static readonly ModuleVersion V1 = new ModuleVersion(1, 0, 0);
        public static void Register()
        {
            TestRunner.Add("Composition: exact versions, references, cycles and canonical module order", Dependencies);
            TestRunner.Add("Composition: manifest ownership, inactive providers and frozen content", Manifest);
            TestRunner.Add("Composition: one service owner and pre-initialize requirements", Services);
            TestRunner.Add("Composition: registry collisions fail before initialization", Registries);
            TestRunner.Add("Composition: service aliases persist once; versions identify saves and peers", Durability);
            TestRunner.Add("Composition: immutable module metadata survives external mutation", CapturedMetadata);
            TestRunner.Add("Composition: invalid module graphs run no declaration callbacks", DeclarationAtomicity);
        }
        private static ModuleRequirement Requires(string id) => new ModuleRequirement(id, V1);
        private static void Dependencies(Check c)
        {
            c.That(ModuleVersion.TryParse("12.3.45", out ModuleVersion parsed) && parsed.ToString() == "12.3.45", "canonical exact version round trips");
            foreach (string invalid in new[] { "1", "1.0", "1.0.0.0", "01.0.0", "1.-1.0", "1.0.0-beta", "1. 0.0", "2147483648.0.0" })
                c.That(!ModuleVersion.TryParse(invalid, out _), "refuse malformed version " + invalid);
            c.Throws<InvalidOperationException>(() => new GameDefinition("bad", new[] { new ProbeModule("a", V1, new[] { Requires("missing") }) }), "missing dependency refuses definition");
            c.Throws<InvalidOperationException>(() => new GameDefinition("bad", new[] { new ProbeModule("a", V1, new[] { Requires("b") }), new ProbeModule("b", new ModuleVersion(2, 0, 0)) }), "dependency version is exact, not a compatibility range");
            c.Throws<InvalidOperationException>(() => new GameDefinition("bad", new[] { new ProbeModule("a", V1, new[] { Requires("b") }), new ProbeModule("b", V1, new[] { Requires("a") }) }), "dependency cycle refuses definition");
            c.Throws<InvalidOperationException>(() => new GameDefinition("bad", new[] { new ProbeModule("a", V1, new[] { Requires("a") }) }), "self dependency refuses definition");
            c.Throws<InvalidOperationException>(() => new GameDefinition("bad", new[] { new ProbeModule("a", V1), new ProbeModule("a", V1) }), "duplicate module identity refuses definition");
            c.Throws<InvalidOperationException>(() => new GameDefinition("bad", new[] { new ProbeModule("a", V1, new[] { Requires("b"), Requires("b") }), new ProbeModule("b", V1) }), "duplicate requirement refuses definition");
            var first = new GameDefinition("testgame", new GameModule[] { new TestModule(), new ExtraModule() });
            var second = new GameDefinition("testgame", new GameModule[] { new ExtraModule(), new TestModule() });
            GameFactory fa = Factory(first, TestGame.Manifest), fb = Factory(second, TestGame.Manifest);
            SimGame a = fa.NewGame(new NewGameRequest { Scenario = "flat", Mods = { "extra" }, Seed = 7 });
            SimGame b = fb.NewGame(new NewGameRequest { Scenario = "flat", Mods = { "extra" }, Seed = 7 });
            a.RunTicks(80); b.RunTicks(80);
            c.Equal(a.Hash(), b.Hash(), "input module enumeration does not change composition or execution");
            c.Equal("test", second.Modules[0].Id, "dependency composes before dependent despite reverse input");
            string reversed = "{ \"schema\": 1, \"modules\": [{ \"id\": \"extra\", \"version\": \"1.0.0\", \"requires\": [{ \"id\": \"test\", \"version\": \"1.0.0\" }], \"documents\": [\"extra/defs.json\"] }, { \"id\": \"test\", \"version\": \"1.0.0\", \"documents\": [\"test/defs.json\"] }] }";
            SimGame reordered = Factory(second, reversed).NewGame(new NewGameRequest { Scenario = "flat", Mods = { "extra" }, Seed = 7 });
            reordered.RunTicks(80);
            c.Equal(a.Hash(), reordered.Hash(), "manifest module order also has no deterministic effect");
            var independent = new GameDefinition("ordered", new[] { new ProbeModule("z", V1), new ProbeModule("a", V1) });
            c.Equal("a", independent.Modules[0].Id, "independent ready modules use stable ordinal IDs");
        }
        private static GameFactory Factory(GameDefinition definition, string manifest)
        {
            var files = new PreloadedFileSource().Add("framework/access.json", FrameworkProofContent.Access).Add("manifest.json", manifest).Add("test/defs.json", TestGame.Defs).Add("extra/defs.json", TestGame.ExtraDefs);
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static void Manifest(Check c)
        {
            c.Throws<ContentException>(() => Factory(TestGame.Definition(), TestGame.Manifest.Replace("\"schema\": 1, ", "")), "unversioned legacy manifest is not accepted");
            c.Throws<ContentException>(() => Factory(TestGame.Definition(), TestGame.Manifest.Replace("\"schema\": 1", "\"schema\": 2")), "unknown manifest schema fails explicitly");
            c.Throws<ContentException>(() => Factory(TestGame.Definition(), TestGame.Manifest.Replace("\"version\": \"1.0.0\"", "\"version\": \"2.0.0\"")), "manifest cannot substitute a different code version");
            c.Throws<ContentException>(() => Factory(TestGame.Definition(), TestGame.Manifest.Replace("\"documents\": [\"extra/defs.json\"]", "\"documents\": [\"test/defs.json\"]")), "one document cannot be owned by two modules");
            foreach (string invalid in new[] { "../defs.json", "/defs.json", "test/../defs.json", "test//defs.json", "test\\defs.json", "C:/defs.json" })
                c.Throws<ContentException>(() => Factory(TestGame.Definition(), TestGame.Manifest.Replace("test/defs.json", invalid.Replace("\\", "\\\\"))), "refuse escaping or noncanonical document path " + invalid);
            var definition = TestGame.Definition();
            c.Throws<InvalidOperationException>(() => definition.ContentTypes.Component<CounterDef>("late", 1), "content declaration is frozen after definition construction");
            c.Throws<InvalidOperationException>(() => new GameDefinition("bad", new[] { new ProbeModule("probe", V1, content: types => { types.Component<CounterDef>("a", 1); types.Component<CounterDef>("b", 1); }) }), "a component codec cannot acquire another key/owner alias");
            var owners = new GameDefinition("inactive", new GameModule[] { new TestModule(), new ExtraModule(), new ProbeModule("provider", V1, content: types =>
            {
                types.Component<SingleResourcePortDef>("custom", 1);
            }) });
            string manifest = TestGame.Manifest.Replace(" ] }", ", { \"id\": \"provider\", \"version\": \"1.0.0\", \"documents\": [] } ] }");
            string defs = TestGame.Defs.Replace("\"counter\": { \"per_tick\": 0.5 }", "\"custom\": { \"resource\": \"rock\", \"capacity_policy\": \"fixture.count.10\", \"initial\": 0 }");
            var content = ContentSet.Load(new PreloadedFileSource().Add("framework/access.json", FrameworkProofContent.Access).Add("manifest.json", manifest).Add("test/defs.json", defs).Add("extra/defs.json", TestGame.ExtraDefs), owners.ContentTypes);
            c.Throws<ContentException>(() => content.Resolve(new[] { "test" }), "known but inactive component provider is not silently used");
            c.That(content.Resolve(new[] { "test", "provider" }).Get<EntityDef>("bin").HasCapability<IOutletPort>(), "explicitly composed provider resolves its capability");
            var valid = ContentSet.Load(new PreloadedFileSource().Add("framework/access.json", FrameworkProofContent.Access).Add("manifest.json", manifest).Add("test/defs.json", TestGame.Defs).Add("extra/defs.json", TestGame.ExtraDefs), owners.ContentTypes);
            valid.Resolve(new[] { "test" });
            c.That(typeof(ContentTypes).GetMethod("Generator") == null, "inactive modules have no generator admission path");
            valid.Resolve(new[] { "test", "provider" });
            c.That(valid.Resolve(new[] { "test", "provider" }).HasModule("provider"), "explicit active provider admits its authored contracts");
        }
        private static void Services(Check c)
        {
            var spy = new InitializeProbe();
            c.Throws<InvalidOperationException>(() => CompositionProbe.New(new ProbeModule("probe", V1, compose: composer => { composer.AddSystem(spy, 10); composer.Services.Require<IProbeFirst>(); })), "missing declared service refuses composition");
            c.Equal(0, spy.Initializations, "missing provider fails before any system Initialize");
            c.Throws<InvalidOperationException>(() => CompositionProbe.New(new ProbeModule("probe", V1, compose: composer => { composer.AddSystem(spy, 10); composer.Services.Bind<IProbeFirst>(new ProbeState("one")); composer.Services.Bind<IProbeFirst>(new ProbeState("two")); })), "duplicate service contract cannot replace its provider");
            c.Equal(0, spy.Initializations, "ambiguous provider also fails before Initialize");
            c.Throws<InvalidOperationException>(() => CompositionProbe.New(new ProbeModule("probe", V1, compose: composer => { composer.AddSystem(spy, 10); composer.Services.Bind<IProbeFirst>(new ProbeState("same")); composer.Services.Bind<IProbeSecond>(new ProbeState("same")); })), "distinct persistent providers cannot share codec identity even when Equals says equal");
            c.Equal(0, spy.Initializations, "codec collision is detected before Initialize rather than first save");
            var provider = new ProbeModule("probe", V1, compose: composer => { composer.AddSystem(spy, 10); composer.Services.Bind<IProbeFirst>(new ProbeState("first")); });
            c.Throws<InvalidOperationException>(() => CodeOnlyFactory(provider, V1, composer => composer.Services.Bind<IProbeFirst>(new ProbeState("shadow"))).NewGame(new NewGameRequest { Scenario = "probe" }), "another module cannot shadow a service provider");
            c.Equal(0, spy.Initializations, "cross-module shadowing fails before initialization");
            var standard = new GameDefinition("no-policy", new GameModule[] { new InventoryModule(), new OperationsModule(), new ContentAccessModule(), new TransformationModule(), new MotionModule(), new TileNavigationModule(), new WorkModule(), new LogisticsModule(), new ConstructionModule(), new InfrastructureModule(), new IncidentResponseModule() });
            var manifest = "{ \"schema\": 1, \"modules\": [{\"id\":\"sim.access\",\"version\":\"1.0.0\",\"documents\":[\"framework/access.json\"]}, {\"id\":\"sim.inventory\",\"version\":\"1.0.0\",\"documents\":[]}, {\"id\":\"sim.operations\",\"version\":\"1.0.0\",\"documents\":[]}, {\"id\":\"sim.transformations\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.access\",\"version\":\"1.0.0\"},{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[]}, {\"id\":\"sim.motion\",\"version\":\"1.0.0\",\"documents\":[]}, {\"id\":\"sim.tile-navigation\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.motion\",\"version\":\"1.0.0\"}],\"documents\":[]}, {\"id\":\"sim.work\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.access\",\"version\":\"1.0.0\"},{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.motion\",\"version\":\"1.0.0\"}],\"documents\":[\"sim.json\"]}, {\"id\":\"sim.logistics\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}, {\"id\":\"sim.construction\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}, {\"id\":\"sim.infrastructure\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}, {\"id\":\"sim.incidents\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[]}] }";
            string definitions = CompositionProbe.Definitions.Replace("\"probe\"],", "\"sim.work\"],");
            var factory = new GameFactory(standard, ContentSet.Load(new PreloadedFileSource().Add("framework/access.json", FrameworkProofContent.Access).Add("manifest.json", manifest).Add("sim.json", definitions), standard.ContentTypes));
            c.That(!factory.NewGame(new NewGameRequest { Scenario = "probe" }).World.Services.Has<IEconomy>(), "unpriced work does not fabricate or require an economy");
        }
        private static void Registries(Check c)
        {
            var spy = new InitializeProbe();
            c.Throws<InvalidOperationException>(() => CompositionProbe.New(new ProbeModule("probe", V1, compose: composer => { composer.AddSystem(spy, 10); composer.Commands.Register("one", new RemoveHandler()); composer.Commands.Register("two", new RemoveHandler()); })), "one command codec has one registration owner");
            c.Throws<InvalidOperationException>(() => CompositionProbe.New(new ProbeModule("probe", V1, compose: composer => { composer.AddSystem(spy, 10); composer.AddWorldGen(new StateStart(), 1); composer.AddWorldGen(new StateStart(), 2); })), "world-generation IDs are unique even at different orders");
            c.Throws<InvalidOperationException>(() => CompositionProbe.New(new ProbeModule("probe", V1, compose: composer => { composer.AddSystem(spy, 10); composer.AddSystem(new InitializeProbe(), 11); })), "system IDs are unique even at different orders");
            c.Equal(0, spy.Initializations, "registration collisions run no initialization callbacks");
            SimGame game = TestGame.New();
            c.That(game.World.Commands.TryGet("test.remove", out CommandRegistry.Entry entry) && entry.Owner == "test", "command metadata records its composing module");
            c.Throws<InvalidOperationException>(() => game.World.Commands.Register("late", new RemoveHandler()), "commands cannot register after freeze");
            c.Throws<InvalidOperationException>(() => game.World.Services.Bind<IProbeFirst>(new ProbeState("late")), "providers cannot register after freeze");
        }
        private static void Durability(Check c)
        {
            var module = new ProbeModule("probe", V1, compose: composer =>
            {
                var state = new ProbeState("probe.state") { Value = Fix.FromInt(17) };
                composer.Services.Bind<IProbeFirst>(state); composer.Services.Bind<IProbeSecond>(state);
            });
            GameFactory factory = CodeOnlyFactory(module, V1);
            SimGame game = factory.NewGame(new NewGameRequest { Scenario = "probe" });
            var state = (ProbeState)game.World.Services.Get<IProbeFirst>();
            int calls = state.SyncCalls;
            byte[] save = game.Save();
            c.Equal(calls + 1, state.SyncCalls, "service aliases serialize through their one state owner exactly once");
            c.Equal(V1, SaveGame.Describe(save).Settings.ModuleVersions[0], "save header names the exact module version");
            c.Equal(V1, SaveGame.Describe(save).Settings.ModuleVersions[1], "code-only module version is saved even with no documents");
            c.Equal(game.Hash(), factory.Load(save).Hash(), "versioned session and aliased provider restore identically");
            c.That(factory.Describe(save).Compatible, "matching module/content identity is loadable");
            GameFactory changed = CodeOnlyFactory(module, new ModuleVersion(1, 0, 1));
            c.That(!changed.Describe(save).Compatible, "load-menu compatibility includes module versions, not only engine build");
            c.Throws<CorruptStateException>(() => changed.Load(save), "changed code-only module refuses old session before execution");
            SimGame other = changed.NewGame(new NewGameRequest { Scenario = "probe" });
            game.StartLockstep(new LockstepOptions { LocalSeats = { 0 } });
            other.StartLockstep(new LockstepOptions { LocalSeats = { 0 } });
            c.That(!game.Net.CheckHello(other.Net.Hello()).IsNone, "peer handshake refuses a different code-only module version");
        }
        private static void CapturedMetadata(Check c)
        {
            var suppliedRequirements = new List<ModuleRequirement> { Requires("probe") };
            var feature = new MutableModule { SuppliedId = "feature", SuppliedVersion = V1, SuppliedRequirements = suppliedRequirements };
            feature.Build = composer => composer.Services.Bind<IProbeFirst>(new ProbeState("feature.state") { Value = Fix.FromInt(17) });
            var probe = new ProbeModule("probe", V1);
            var definition = new GameDefinition("probe", new GameModule[] { feature, probe });
            ModuleRegistration captured = definition.Find("feature");
            feature.SuppliedId = "renamed";
            feature.SuppliedVersion = new ModuleVersion(99, 0, 0);
            suppliedRequirements[0] = Requires("missing");
            suppliedRequirements.Add(Requires("other"));
            feature.SuppliedRequirements = null;
            c.That(definition.Find("feature") == captured && definition.Find("renamed") == null, "lookup uses captured module identity, not live getter");
            c.Equal(V1, captured.Version, "external version mutation cannot change registration");
            c.Equal(1, captured.Requires.Count, "external list additions do not alter captured requirements");
            c.Equal("probe", captured.Requires[0].Id, "external list replacement does not alter dependency identity");
            c.Throws<NotSupportedException>(() => ((IList<ModuleRequirement>)captured.Requires)[0] = Requires("other"), "captured requirements cannot be mutated through collection casts");
            c.Throws<NotSupportedException>(() => ((IList<ModuleRegistration>)definition.Modules).Clear(), "registered module list cannot be mutated through collection casts");
            IReadOnlyList<ModuleRegistration> selected = definition.Select(new[] { "feature" });
            c.Equal("probe", selected[0].Id, "selection still includes the original dependency first");
            c.Equal("feature", selected[1].Id, "selection uses original dependent identity");
            c.Throws<NotSupportedException>(() => ((IList<ModuleRegistration>)selected).Clear(), "selected registrations are read-only");
            string manifest = "{ \"schema\": 1, \"modules\": [{ \"id\": \"feature\", \"version\": \"1.0.0\", \"requires\": [{ \"id\": \"probe\", \"version\": \"1.0.0\" }], \"documents\": [] }, { \"id\": \"probe\", \"version\": \"1.0.0\", \"documents\": [\"probe.json\"] }] }";
            string defs = CompositionProbe.Definitions.Replace("[\"probe\"]", "[\"feature\"]");
            var content = ContentSet.Load(new PreloadedFileSource().Add("framework/access.json", FrameworkProofContent.Access).Add("manifest.json", manifest).Add("probe.json", defs), definition.ContentTypes);
            var factory = new GameFactory(definition, content);
            SimGame game = factory.NewGame(new NewGameRequest { Scenario = "probe", Seed = 7 });
            c.Equal(Fix.FromInt(17), ((ProbeState)game.World.Services.Get<IProbeFirst>()).Value, "captured registration still executes its original module implementation");
            byte[] save = game.Save();
            c.That(factory.Describe(save).Compatible, "save-menu identity uses frozen metadata after module mutation");
            c.Equal(game.Hash(), factory.Load(save).Hash(), "saved version/dependency identity survives metadata mutation");
            c.Equal("feature", SaveGame.Describe(save).Settings.Modules[1], "save retains captured module ID");
            c.Equal(V1, SaveGame.Describe(save).Settings.ModuleVersions[1], "save retains captured module version");
            var freshFeature = new ProbeModule("feature", V1, new[] { Requires("probe") }, compose: feature.Build);
            var freshDefinition = new GameDefinition("probe", new GameModule[] { probe, freshFeature });
            var freshContent = ContentSet.Load(new PreloadedFileSource().Add("framework/access.json", FrameworkProofContent.Access).Add("manifest.json", manifest).Add("probe.json", defs), freshDefinition.ContentTypes);
            var freshFactory = new GameFactory(freshDefinition, freshContent);
            SimGame fresh = freshFactory.NewGame(new NewGameRequest { Scenario = "probe", Seed = 7 });
            c.Equal(game.Hash(), fresh.Hash(), "mutated source module and fresh equivalent declarations yield identical sessions");
            c.Equal(game.Hash(), freshFactory.Load(save).Hash(), "independent equivalent registrations restore the same save");
            game.StartLockstep(new LockstepOptions { LocalSeats = { 0 } });
            fresh.StartLockstep(new LockstepOptions { LocalSeats = { 0 } });
            c.That(game.Net.CheckHello(fresh.Net.Hello()).IsNone, "peer identity is unchanged by source metadata mutation");
            c.Equal(1, feature.IdReads, "module ID getter is captured exactly once");
            c.Equal(1, feature.VersionReads, "module version getter is captured exactly once");
            c.Equal(1, feature.RequirementReads, "module dependency getter is captured exactly once");
        }

        private static void DeclarationAtomicity(Check c)
        {
            int calls = 0;
            var good = new ProbeModule("a", V1, content: _ => calls++);
            c.Throws<InvalidOperationException>(() => new GameDefinition("bad", new GameModule[] { good,
                new ProbeModule("b", V1, new[] { Requires("c") }), new ProbeModule("c", V1, new[] { Requires("b") }) }), "later cycle refuses complete graph");
            c.Equal(0, calls, "even an earlier independent module declares nothing when graph has a cycle");
            c.Throws<InvalidOperationException>(() => new GameDefinition("bad", new GameModule[] { good,
                new MutableModule { SuppliedId = "b", SuppliedVersion = V1, SuppliedRequirements = null } }), "null dependency metadata is explicit invalid content");
            c.Equal(0, calls, "null requirement list invokes no declaration callback");
            c.Throws<ArgumentException>(() => new GameDefinition("bad", new GameModule[] { good,
                new MutableModule { SuppliedId = "b", SuppliedVersion = V1, SuppliedRequirements = new ModuleRequirement[] { default } } }), "default dependency value with missing ID refuses registration");
            c.Equal(0, calls, "invalid default dependency runs no declarations");

            var requirements = new List<ModuleRequirement> { Requires("probe") };
            var mutable = new MutableModule { SuppliedId = "feature", SuppliedVersion = V1, SuppliedRequirements = requirements };
            var provider = new ProbeModule("probe", V1, content: _ =>
            {
                mutable.SuppliedId = "invalid identity"; mutable.SuppliedVersion = new ModuleVersion(2, 0, 0);
                requirements.Clear();
            });
            var definition = new GameDefinition("captured", new GameModule[] { mutable, provider });
            c.Equal("feature", definition.Modules[1].Id, "all module identities are captured before the first declaration callback");
            c.Equal(V1, definition.Modules[1].Version, "declaration callbacks cannot change another module's version");
            c.Equal(1, definition.Modules[1].Requires.Count, "declaration callbacks cannot erase another module's captured dependencies");
        }

        private static GameFactory CodeOnlyFactory(ProbeModule probe, ModuleVersion featureVersion, Action<WorldComposer> composeFeature = null)
        {
            var feature = new ProbeModule("feature", featureVersion, new[] { Requires("probe") }, compose: composeFeature);
            var definition = new GameDefinition("probe", new[] { feature, probe });
            string manifest = "{ \"schema\": 1, \"modules\": [{ \"id\": \"probe\", \"version\": \"1.0.0\", \"documents\": [\"probe.json\"] }, { \"id\": \"feature\", \"version\": \"" + featureVersion + "\", \"requires\": [{ \"id\": \"probe\", \"version\": \"1.0.0\" }], \"documents\": [] }] }";
            string defs = CompositionProbe.Definitions.Replace("[\"probe\"]", "[\"probe\",\"feature\"]");
            return new GameFactory(definition, ContentSet.Load(new PreloadedFileSource().Add("framework/access.json", FrameworkProofContent.Access).Add("manifest.json", manifest).Add("probe.json", defs), definition.ContentTypes));
        }
    }
}
