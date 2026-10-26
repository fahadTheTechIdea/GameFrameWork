using System;
using System.Collections.Generic;
using System.Reflection;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;

namespace Beep.SimEngine.Tests
{
    public static class ViewOwnershipTests
    {
        private static readonly ModuleVersion V1 = new ModuleVersion(1, 0, 0);
        public static void Register()
        {
            TestRunner.Add("View ownership: invalid declarations refuse before initialization", InvalidDeclarations);
            TestRunner.Add("View ownership: exact requirements and frozen composition", Requirements);
            TestRunner.Add("View ownership: immutable metadata, viewer isolation and saved continuation", Views);
        }

        private static ProbeModule Module(Action<WorldComposer> compose)
            => new ProbeModule("probe", V1, compose: compose);

        private static void InvalidDeclarations(Check c)
        {
            var spy = new InitializeProbe();
            foreach (string id in new[] { "", " ", "Upper", "path/view", "a:b" })
                c.Throws<ArgumentException>(() => CompositionProbe.New(Module(composer =>
                {
                    composer.AddSystem(spy, 10);
                    composer.AddViewFeature(id, 1, (world, viewer) => new ProbeView(viewer));
                })), "invalid view identity refuses composition: " + id);
            foreach (int version in new[] { 0, -1 })
                c.Throws<ArgumentOutOfRangeException>(() => CompositionProbe.New(Module(composer =>
                {
                    composer.AddSystem(spy, 10);
                    composer.AddViewFeature("probe.view", version, (world, viewer) => new ProbeView(viewer));
                })), "nonpositive view schema refuses composition");
            c.Throws<ArgumentNullException>(() => CompositionProbe.New(Module(composer =>
            {
                composer.AddSystem(spy, 10);
                composer.AddViewFeature<ProbeView>("probe.view", 1, null);
            })), "null factory is not an absent provider");
            c.Throws<InvalidOperationException>(() => CompositionProbe.New(Module(composer =>
            {
                composer.AddSystem(spy, 10);
                composer.AddViewFeature("probe.view", 1, (world, viewer) => new ProbeView(viewer));
                composer.AddViewFeature("probe.view", 1, (world, viewer) => new OtherProbeView());
            })), "different contracts cannot reuse one view ID");
            c.Throws<InvalidOperationException>(() => CompositionProbe.New(Module(composer =>
            {
                composer.AddSystem(spy, 10);
                composer.AddViewFeature("probe.first", 1, (world, viewer) => new ProbeView(viewer));
                composer.AddViewFeature("probe.second", 2, (world, viewer) => new ProbeView(viewer));
            })), "one contract cannot acquire another identity/schema");
            c.Equal(0, spy.Initializations, "all invalid registrations refuse before any initialization");
            SimGame nullProvider = CompositionProbe.New(Module(composer =>
                composer.AddViewFeature<ProbeView>("probe.null", 1, (world, viewer) => null)));
            c.Throws<InvalidOperationException>(() => nullProvider.Seat(0).View.Feature<ProbeView>(), "registered factory returning null fails explicitly on lazy read");
            c.That(nullProvider.Seat(0).View.Feature<OtherProbeView>() == null, "genuinely unregistered optional feature remains absent");
            var methods = typeof(WorldComposer).GetMethods(BindingFlags.Public | BindingFlags.Instance);
            int registrations = 0;
            foreach (var method in methods)
                if (method.Name == "AddViewFeature")
                {
                    registrations++;
                    c.Equal(3, method.GetParameters().Length, "there is no unowned registration overload");
                }
            c.Equal(1, registrations, "one view registration API");
        }

        private static void Requirements(Check c)
        {
            var spy = new InitializeProbe();
            foreach (int mismatch in new[] { 0, 1, 2, 3 })
                c.Throws<InvalidOperationException>(() => CompositionProbe.New(Module(composer =>
                {
                    composer.AddSystem(spy, 10);
                    composer.RequireViewFeature<ProbeView>("probe.view", 2);
                    if (mismatch == 1) composer.AddViewFeature("probe.other", 2, (world, viewer) => new ProbeView(viewer));
                    if (mismatch == 2) composer.AddViewFeature("probe.view", 1, (world, viewer) => new ProbeView(viewer));
                    if (mismatch == 3) composer.AddViewFeature("probe.view", 2, (world, viewer) => new OtherProbeView());
                })), "missing or different identity/schema/contract refuses required view");
            c.Equal(0, spy.Initializations, "view requirements validate before all system initialization");
            WorldComposer captured = null;
            SimGame valid = CompositionProbe.New(Module(composer =>
            {
                captured = composer;
                composer.AddSystem(spy, 10);
                composer.RequireViewFeature<ProbeView>("probe.view", 2);
                composer.AddViewFeature("probe.view", 2, (world, viewer) => new ProbeView(viewer));
            }));
            c.Equal(1, spy.Initializations, "a requirement declared before its provider resolves after composition");
            c.Equal(2, valid.Seat(0).View.FeatureRegistrations[0].SchemaVersion, "exact positive schema is preserved");
            c.Throws<InvalidOperationException>(() => captured.AddViewFeature("probe.late", 1, (world, viewer) => new OtherProbeView()), "retained composer cannot add a view after freeze");
            c.Throws<InvalidOperationException>(() => captured.RequireViewFeature<OtherProbeView>("probe.late", 1), "retained composer cannot add a requirement after freeze");

            var owner = Module(composer => composer.AddViewFeature("probe.view", 1, (world, viewer) => new ProbeView(viewer)));
            var consumer = new ProbeModule("consumer", V1, compose: composer => composer.RequireViewFeature<ProbeView>("probe.view", 1));
            var definition = new GameDefinition("probe", new GameModule[] { owner, consumer });
            string manifest = "{ \"schema\": 1, \"modules\": [{ \"id\": \"probe\", \"version\": \"1.0.0\", \"documents\": [\"probe.json\"] }, { \"id\": \"consumer\", \"version\": \"1.0.0\", \"documents\": [] }] }";
            var files = new PreloadedFileSource().Add("manifest.json", manifest)
                .Add("probe.json", CompositionProbe.Definitions.Replace("[\"probe\"]", "[\"probe\",\"consumer\"]"));
            var factory = new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
            var game = factory.NewGame(new NewGameRequest { Scenario = "probe" });
            c.Equal("probe", game.Seat(0).View.FeatureRegistrations[0].Owner, "consumer composing first cannot become the feature owner");

            var shadow = new ProbeModule("consumer", V1, compose: composer => composer.AddViewFeature("consumer.view", 1, (world, viewer) => new ProbeView(viewer)));
            var shadowDefinition = new GameDefinition("probe", new GameModule[] { owner, shadow });
            var shadowFactory = new GameFactory(shadowDefinition, ContentSet.Load(files, shadowDefinition.ContentTypes));
            c.Throws<InvalidOperationException>(() => shadowFactory.NewGame(new NewGameRequest { Scenario = "probe" }), "a different module cannot shadow the same view contract");
        }

        private static void Views(Check c)
        {
            int calls = 0;
            var module = Module(composer =>
            {
                composer.AddViewFeature("probe.z", 3, (world, viewer) => { calls++; return new ProbeView(viewer); });
                composer.AddViewFeature("probe.a", 1, (world, viewer) => new OtherProbeView());
            });
            GameFactory factory = CompositionProbe.Factory(module);
            SimGame game = factory.NewGame(new NewGameRequest { Scenario = "probe" });
            PlayerView view = game.Seat(0).View;
            ulong before = game.Hash();
            IReadOnlyList<ViewFeatureRegistration> entries = view.FeatureRegistrations;
            c.Equal(2, entries.Count, "all active view identities are described");
            c.Equal("probe.a", entries[0].Id, "metadata is ordinal-ID ordered, not insertion ordered");
            c.Equal("probe.z", entries[1].Id, "stable ordering includes the second entry");
            c.Equal("probe", entries[1].Owner, "metadata captures the declaring module");
            c.Equal(3, entries[1].SchemaVersion, "metadata names the authored schema");
            c.Equal(typeof(ProbeView), entries[1].Contract, "metadata names its public contract");
            c.Equal(0, calls, "metadata inspection never creates a view");
            c.Throws<NotSupportedException>(() => ((IList<ViewFeatureRegistration>)entries).Clear(), "metadata collection cannot be edited");
            foreach (PropertyInfo property in typeof(ViewFeatureRegistration).GetProperties())
                c.That(property.SetMethod == null, "registration property is immutable: " + property.Name);
            c.That(typeof(ViewFeatureRegistration).GetMethod("Create") == null, "hosts cannot invoke factory with another world/viewer");
            ProbeView first = view.Feature<ProbeView>();
            c.Equal(new PlayerId(0), first.Viewer, "factory receives the actual scoped viewer");
            c.That(ReferenceEquals(first, view.Feature<ProbeView>()), "one cached provider per viewer");
            c.Equal(1, calls, "repeated feature reads do not construct another provider");
            ProbeView observer = game.Observer.Feature<ProbeView>();
            c.That(!ReferenceEquals(first, observer) && observer.Viewer == PlayerId.None, "observer gets a separate scoped provider");
            c.Equal(before, game.Hash(), "all metadata and projection reads leave simulation unchanged");
            SimGame other = factory.NewGame(new NewGameRequest { Scenario = "probe" });
            c.That(!ReferenceEquals(first, other.Seat(0).View.Feature<ProbeView>()), "interleaved sessions never share a view instance");
            game.RunTicks(17);
            SimGame restored = factory.Load(game.Save());
            c.Equal(game.Hash(), restored.Hash(), "view creation is not a second persisted state authority");
            c.Equal(entries[1].Id, restored.Seat(0).View.FeatureRegistrations[1].Id, "saved composition rebuilds stable view identity");
            c.Equal(entries[1].SchemaVersion, restored.Seat(0).View.FeatureRegistrations[1].SchemaVersion, "saved composition rebuilds exact schema");
            c.That(!ReferenceEquals(first, restored.Seat(0).View.Feature<ProbeView>()), "restoration constructs a new scoped view");
            game.RunTicks(20); restored.RunTicks(20);
            c.Equal(game.Hash(), restored.Hash(), "saved twin continuation remains identical");
        }
    }
}
