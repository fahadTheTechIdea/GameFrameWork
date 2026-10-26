using System;
using System.Collections.Generic;
using Beep.SimEngine.Content;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    public static class SchemaBindingTests
    {
        private static readonly Code Probe = new Code("probe");
        private const string Signals = @"""signals"": [
            { ""id"": ""value"", ""kind"": ""number"", ""metric"": ""probe.value"", ""unit"": ""unit.count"", ""providers"": [{ ""component"": ""schema_probe"", ""schema"": 1 }] },
            { ""id"": ""enabled"", ""kind"": ""boolean"", ""metric"": ""probe.enabled"", ""unit"": ""unit.boolean"", ""providers"": [{ ""component"": ""schema_probe"", ""schema"": 1 }] }
        ],";
        private static string Definitions(bool signals = true, bool authored = true)
            => "{ " + TestGame.TimeDefinitions + (signals ? Signals : "")
                + "\"entity_states\": [{ \"id\": \"idle\", \"state\": \"state.idle\", \"priority\": 0, \"default\": true }],"
                + (authored ? "\"entities\": [{ \"id\": \"machine\", \"kind\": \"marker\", \"components\": { \"schema_probe\": { \"operation\": " + SchemaProbeSystem.Controls + " } } }]," : "")
                + "\"game_modes\":[{\"id\":\"schema-binding.resource-only\",\"name\":\"Physical diagnostic game\",\"properties\":{}}],\"scenarios\": [{ \"id\": \"probe\",\"game_mode\":\"schema-binding.resource-only\",\"mode_overrides\":{}, \"time\": \"test.minute\", \"modules\": [\"probe\"], \"seats\": 2, \"default_seats\": 2 }] }";

        private static GameFactory Factory(SchemaProbeSystem system, string json = null, Action<ContentTypes> extra = null)
        {
            var module = new ProbeModule("probe", new ModuleVersion(1, 0, 0),
                new[] { new ModuleRequirement(WorkModule.ModuleId, new ModuleVersion(1, 0, 0)), new ModuleRequirement(TransformationModule.ModuleId, new ModuleVersion(1, 0, 0)) },
                types => { types.Component<SchemaProbeDef>("schema_probe", 1); extra?.Invoke(types); },
                composer => composer.AddSystem(system, 10));
            var definition = new GameDefinition("schema-test", new GameModule[] { new InventoryModule(), new OperationsModule(), new ContentAccessModule(), new TransformationModule(), new MotionModule(), new TileNavigationModule(), new WorkModule(), new LogisticsModule(), new ConstructionModule(), new InfrastructureModule(), new IncidentResponseModule(), module });
            const string master = "{ \"schema\": 1, \"modules\": [{\"id\":\"sim.access\",\"version\":\"1.0.0\",\"documents\":[\"framework/access.json\"]}, {\"id\":\"sim.inventory\",\"version\":\"1.0.0\",\"documents\":[]}, {\"id\":\"sim.operations\",\"version\":\"1.0.0\",\"documents\":[]}, {\"id\":\"sim.transformations\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.access\",\"version\":\"1.0.0\"},{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[]}, {\"id\":\"sim.motion\",\"version\":\"1.0.0\",\"documents\":[]}, {\"id\":\"sim.tile-navigation\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.motion\",\"version\":\"1.0.0\"}],\"documents\":[]}, {\"id\":\"sim.work\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.access\",\"version\":\"1.0.0\"},{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"},{\"id\":\"sim.motion\",\"version\":\"1.0.0\"}],\"documents\":[]}, {\"id\":\"sim.logistics\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}, {\"id\":\"sim.construction\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}, {\"id\":\"sim.infrastructure\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}, {\"id\":\"sim.incidents\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[]}, { \"id\": \"probe\", \"version\": \"1.0.0\", \"requires\": [{\"id\":\"sim.work\",\"version\":\"1.0.0\"},{\"id\":\"sim.transformations\",\"version\":\"1.0.0\"}], \"documents\": [\"probe.json\"] }] }";
            var files = new PreloadedFileSource().Add("framework/access.json", FrameworkProofContent.Access).Add("manifest.json", master).Add("probe.json", json ?? Definitions());
            return new GameFactory(definition, ContentSet.Load(files, definition.ContentTypes));
        }
        private static SimGame New(GameFactory factory) => factory.NewGame(new NewGameRequest { Scenario = "probe", Seed = 4 });
        private static SchemaProbe Part(SimGame game, int seat = 0)
        {
            foreach (Entity entity in game.World.Entities.All)
                if (entity.Owner == new PlayerId(seat)) return entity.Get<SchemaProbe>();
            throw new InvalidOperationException("no schema probe entity");
        }
        public static void Register()
        {
            TestRunner.Add("Binding schemas: captured immutable declarations and engine-owned views", Captured);
            TestRunner.Add("Binding schemas: malformed provider declarations refuse before initialization", Invalid);
            TestRunner.Add("Binding schemas: authored bindings resolve without live providers", References);
            TestRunner.Add("Binding schemas: foreign sessions cannot invoke signal providers", Ownership);
            TestRunner.Add("Binding schemas: authored components use the declared contracts", Authored);
            TestRunner.Add("Binding schemas: saved declaration drift refuses before callbacks", Drift);
            TestRunner.Add("Binding schemas: engine commands and saved sessions share provider state", Durability);
        }

        private static void Captured(Check c)
        {
            SchemaProbeDeclaration.Reset();
            var metrics = (List<AutomationMetricSchema>)SchemaProbeDeclaration.Metrics;
            var system = new SchemaProbeSystem();
            GameFactory factory = Factory(system);
            c.Equal(1, SchemaProbeDeclaration.OperationReads, "operation version captured once during declaration");
            c.Equal(1, SchemaProbeDeclaration.SignalReads, "signal version captured once during declaration");
            c.Equal(1, SchemaProbeDeclaration.BindingReads, "metric list captured once during declaration");
            metrics.Clear(); SchemaProbeDeclaration.OperationVersion = SchemaProbeDeclaration.SignalVersion = 9;
            SimGame game = New(factory);
            SchemaProbe part = Part(game);
            OperationsView view = game.Seat(0).View.Feature<OperationsView>();
            c.That(view.TryGetOperationSchema(part.Entity.Id, Probe, out OperationProviderSchema operation), "owner receives declared operation schema");
            c.That(view.TryGetSignalSchema(part.Entity.Id, Probe, out AutomationSignalProviderSchema signal), "owner receives declared signal schema");
            c.Equal(1, operation.Version, "mutable module metadata cannot change captured operation version");
            c.Equal(1, signal.Version, "mutable module metadata cannot change captured signal version");
            c.Equal(2, signal.Bindings.Count, "provider's mutable list was copied");
            c.Equal("probe.enabled", signal.Bindings[0].Metric.Id, "metric bindings have canonical identity order");
            c.Throws<NotSupportedException>(() => ((IList<AutomationMetricSchema>)signal.Bindings).Clear(), "published binding list is immutable");
            c.That(ReferenceEquals(part.Def.StateCodec, operation.StateCodec) && ReferenceEquals(operation.StateCodec, signal.StateCodec), "one component declaration owns state and capability schemas");
            c.That(!view.TryGetSignalSchema(Part(game, 1).Entity.Id, Probe, out _), "schemas respect company visibility");
            c.Equal(1, system.Initializations, "new game initializes its system once");
            game.Hash(); game.Save(); game.RunTicks(3);
            c.Equal(1, SchemaProbeDeclaration.OperationReads, "execution never recaptures operation declaration");
            c.Equal(1, SchemaProbeDeclaration.BindingReads, "execution never recaptures provider metrics");
            c.That(typeof(SchemaProbe).Assembly != typeof(World).Assembly, "proof implements public extension contracts in unrelated assembly");
        }

        private static void Invalid(Check c)
        {
            SchemaProbeDeclaration.Reset();
            c.Throws<ArgumentException>(() => new GameDefinition("bad", new[] { new ProbeModule("probe", new ModuleVersion(1, 0, 0),
                content: types => types.Component<UndeclaredSchemaDef>("bad", 1)) }), "runtime capabilities require matching definition contracts");
            c.Throws<ArgumentException>(() => new GameDefinition("bad", new[] { new ProbeModule("probe", new ModuleVersion(1, 0, 0),
                content: types => types.Component<WrongRuntimeSchemaDef>("bad", 1)) }), "definition cannot claim an absent runtime capability");
            for (int fault = 0; fault < 6; fault++)
            {
                SchemaProbeDeclaration.Reset(); var system = new SchemaProbeSystem();
                if (fault == 0) SchemaProbeDeclaration.OperationVersion = 0;
                if (fault == 1) SchemaProbeDeclaration.SignalVersion = -1;
                if (fault == 2) SchemaProbeDeclaration.Metrics = null;
                if (fault == 3) SchemaProbeDeclaration.Metrics = Array.Empty<AutomationMetricSchema>();
                if (fault == 4) SchemaProbeDeclaration.Metrics = new[] { default(AutomationMetricSchema) };
                if (fault == 5) SchemaProbeDeclaration.Metrics = new[] { SchemaProbeDeclaration.Metrics[0], SchemaProbeDeclaration.Metrics[0] };
                c.Throws<ArgumentException>(() => Factory(system), "malformed schema rejected: " + fault);
                c.Equal(0, system.Initializations, "invalid declaration invokes no initialization: " + fault);
            }
            c.Throws<ArgumentException>(() => new AutomationMetricSchema(new Code("boolean"), AutomationSignalKind.Boolean, new Code("unit.count")), "boolean units are explicit and fixed");
            c.Throws<ArgumentOutOfRangeException>(() => new AutomationMetricSchema(Probe, (AutomationSignalKind)99, new Code("unit.count")), "unknown metric kinds are not substituted");
        }

        private static void References(Check c)
        {
            SchemaProbeDeclaration.Reset();
            var bad = new[] {
                Definitions().Replace("\"schema_probe\", \"schema\": 1", "\"missing\", \"schema\": 1"),
                Definitions().Replace("\"schema\": 1", "\"schema\": 2"),
                Definitions().Replace("\"probe.value\"", "\"probe.missing\""),
                Definitions().Replace("\"unit.count\"", "\"unit.wrong\""),
                Definitions().Replace("\"metric\": \"probe.enabled\"", "\"metric\": \"probe.value\""),
                Definitions().Replace("\"providers\": [{ \"component\": \"schema_probe\", \"schema\": 1 }]", "\"providers\": []")
            };
            foreach (string json in bad)
            {
                var system = new SchemaProbeSystem();
                c.Throws<ContentException>(() => New(Factory(system, json)), "invalid authored binding rejected before a world runs");
                c.Equal(0, system.Initializations, "binding validation precedes initialization");
            }
            SimGame empty = New(Factory(new SchemaProbeSystem(), Definitions(authored: false)));
            c.Equal(0, empty.World.Entities.Count, "bindings resolve against declarations even without instantiated providers");
            c.Equal(bad.Length + 1, SchemaProbeDeclaration.SignalReads, "provider declarations are captured once per game definition");
        }

        private static void Ownership(Check c)
        {
            SchemaProbeDeclaration.Reset();
            string definitions = Definitions().Replace("\"signals\": [", "\"signals\": [{ \"id\": \"unbound\", \"kind\": \"number\", \"metric\": \"dial.accumulated\", \"unit\": \"unit.units\", \"providers\": [{ \"component\": \"dial_operation\", \"schema\": 1 }] },");
            GameFactory factory = Factory(new SchemaProbeSystem(), definitions, types => types.Component<DialOperationDef>("dial_operation", 1));
            SimGame a = New(factory), b = New(factory);
            SchemaProbe source = Part(a), foreign = Part(b);
            AutomationSignalDef signal = a.World.Content.Get<AutomationSignalDef>("value");
            c.That(ReferenceEquals(a.World.Content, b.World.Content), "test shares content to isolate world ownership from content ownership");
            c.Throws<ArgumentException>(() => signal.ReadFrom(a.World, foreign), "foreign session provider rejected before read");
            c.Equal(0, foreign.SignalReads, "foreign callback never invoked");
            c.Equal(0, source.SignalReads, "rejected request never invokes local provider either");
            c.That(!signal.ReadFrom(a.World, null).Available, "missing provider returns typed unavailable without a fake value");
            c.That(!a.World.Content.Get<AutomationSignalDef>("unbound").ReadFrom(a.World, source).Available, "a live but undeclared source cannot satisfy another provider's binding");
            c.Equal(0, source.SignalReads, "unbound request never invokes the provider");
            c.Equal(Codes.NotOwned, a.Seat(0).View.Feature<OperationsView>().ReadSignal(Part(a, 1).Entity.Id, Probe, "value", out _), "player signal privacy is enforced before provider reads");
            c.Equal(0, Part(a, 1).SignalReads, "private provider was never invoked");
            c.That(signal.ReadFrom(a.World, source).Available, "local declared provider supplies actual value");
            a.World.Despawn(source.Entity);
            c.Throws<ArgumentException>(() => signal.ReadFrom(a.World, source), "removed provider cannot supply stale session state");
            c.Equal(1, source.SignalReads, "removed provider read was not invoked");
        }

        private static void Authored(Check c)
        {
            SchemaProbeDeclaration.Reset();
            GameFactory factory = Factory(new SchemaProbeSystem(), Definitions());
            SimGame game = New(factory); SchemaProbe part = Part(game);
            c.Equal("schema_probe", part.Def.StateCodec.Id, "authored component uses the existing registered codec");
            c.Equal("probe", part.Def.SignalSchema.StateCodec.Owner, "authored capabilities retain the declared module owner");
            c.That(game.Seat(0).View.Feature<OperationsView>().TryGetControl(part.Entity.Id, Probe, out var control) && control.Target == Fix.FromInt(2), "authored controls are authored, not fabricated");
            c.Equal(Code.None, game.Seat(0).View.Feature<OperationsView>().ReadSignal(part.Entity.Id, Probe, "value", out var value), "authored signal uses the ordinary engine view");
            c.That(value.Available, "authored binding is accepted");
            SchemaProbeDeclaration.OnAuthoredRead = part => {
                var reader = new JsonReader(Json.Parse(SchemaProbeSystem.Controls.Replace("\"probe\"", "\"different\"")), "bad control");
                part.ControlOverride = OperationSettings.Read(reader);
            };
            try { c.Throws<ContentException>(() => New(Factory(new SchemaProbeSystem(), Definitions())), "authored controls cannot name a different capability"); }
            finally { SchemaProbeDeclaration.OnAuthoredRead = null; }
        }

        private static void Drift(Check c)
        {
            SchemaProbeDeclaration.Reset();
            SimGame original = New(Factory(new SchemaProbeSystem(), Definitions(signals: false))); original.RunTicks(5);
            byte[] saved = original.Save();
            for (int fault = 0; fault < 4; fault++)
            {
                SchemaProbeDeclaration.Reset(); var system = new SchemaProbeSystem();
                if (fault == 0) SchemaProbeDeclaration.OperationVersion = 2;
                if (fault == 1) SchemaProbeDeclaration.SignalVersion = 2;
                if (fault == 2) SchemaProbeDeclaration.Metrics = new[] { SchemaProbeDeclaration.Metrics[0] };
                if (fault == 3) SchemaProbeDeclaration.Metrics = new[] { SchemaProbeDeclaration.Metrics[1], new AutomationMetricSchema(new Code("probe.value"), AutomationSignalKind.Number, new Code("unit.other")) };
                GameFactory changed = Factory(system, Definitions(signals: false));
                c.Throws<CorruptStateException>(() => changed.Load(saved), "saved capability schema drift refuses: " + fault);
                c.Equal(0, system.Initializations, "declaration drift invokes no initialization: " + fault);
                c.Equal(0, system.Reads, "declaration drift invokes no system payload codec: " + fault);
                c.Equal(0, system.Loaded, "declaration drift invokes no loaded callbacks: " + fault);
            }
            SchemaProbeDeclaration.Reset(); var mismatch = new SchemaProbeSystem();
            c.Throws<CorruptStateException>(() => Factory(mismatch, Definitions(signals: false).Replace("\"target\": 2", "\"target\": 3")).Load(saved), "content mismatch refuses before initialization");
            c.Equal(0, mismatch.Initializations, "content drift invokes no initialization");
        }

        private static void Durability(Check c)
        {
            SchemaProbeDeclaration.Reset(); var originalSystem = new SchemaProbeSystem();
            GameFactory factory = Factory(originalSystem); SimGame a = New(factory); SchemaProbe part = Part(a);
            OperationsView view = a.Seat(0).View.Feature<OperationsView>();
            ulong hash = a.Hash();
            c.Equal(OperationCodes.TargetBounds, view.PreviewConfigure(part.Entity.Id, Probe, true, Fix.FromInt(6)), "engine preview uses authored bounds");
            c.Equal(hash, a.Hash(), "schema and control previews are pure");
            a.Seat(0).Submit(new Beep.SimEngine.Commands.ConfigureOperationCommand { Entity = part.Entity.Id, Operation = Probe, Enabled = true, Target = Fix.FromInt(4) });
            a.RunTicks(3);
            c.That(view.TryGetControl(part.Entity.Id, Probe, out var controlled) && controlled.Target == Fix.FromInt(4), "ordinary engine command controls external provider");
            view.ReadSignal(part.Entity.Id, Probe, "value", out var snapshot);
            var restoredSystem = new SchemaProbeSystem(); SimGame b = Factory(restoredSystem).Load(a.Save());
            c.Equal(a.Hash(), b.Hash(), "save restores actual provider state through its sole Sync");
            c.Equal(1, restoredSystem.Initializations, "valid load initializes exactly once");
            c.Equal(1, restoredSystem.Reads, "valid load reads system state exactly once");
            c.Equal(1, restoredSystem.Loaded, "valid load rebuilds derived state exactly once");
            c.Equal(1, Part(b).ReadCalls, "provider component restores exactly once");
            for (int i = 0; i < 12; i++) { a.Update(); b.Update(); }
            c.Equal(a.Hash(), b.Hash(), "interleaved restored sessions remain identical");
            view.ReadSignal(part.Entity.Id, Probe, "value", out var later);
            c.That(later.Value > snapshot.Value, "engine updates perform real provider work");
            c.Equal(Fix.FromInt(12), snapshot.Value.Value, "captured signal value never changes after later updates");
            part.BadControl = true;
            c.Throws<InvalidOperationException>(() => view.TryGetControl(part.Entity.Id, Probe, out _), "provider facts cannot override captured units or bounds");
        }
    }
}
