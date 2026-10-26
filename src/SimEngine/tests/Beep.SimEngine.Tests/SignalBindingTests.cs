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
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Input authoring and normal public player APIs only; no raw world/resource/progress writes.
    internal static class SignalBindingTests
    {
        internal const string Case = "Signal bindings: authored named metadata drives gates automation and physical production";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static readonly Code Source = new Code("metadata.sensor"), Processor = new Code("contract.process");
        internal static GameFactory Create(SignalBindingModule module, InitializeProbe init = null, string fault = null, Action<JsonValue> editSource = null, Action<JsonValue> editSetup = null)
        {
            var data = Json.Parse(FrameworkProofContent.Read("automation-rules.json"));
            data.Set("signals", Json.Parse("[{\"id\":\"binding.level\",\"kind\":\"number\",\"metric\":\"probe.level\",\"unit\":\"unit.level\",\"providers\":[{\"component\":\"metadata_source\",\"schema\":1}]},{\"id\":\"binding.enabled\",\"kind\":\"boolean\",\"metric\":\"operation.enabled\",\"unit\":\"unit.boolean\",\"providers\":[{\"component\":\"metadata_source\",\"schema\":1}]}]"));
            var rule = data.Get("control_rules")[0].Clone(); rule.Set("id", JsonValue.Of("binding.rule"));
            rule.Get("inputs")[0].Set("signal", JsonValue.Of("binding.level"));
            rule.Get("when_true").Set("enabled", JsonValue.Of(true)); rule.Get("when_false").Set("enabled", JsonValue.Of(false));
            data.Set("control_rules", JsonValue.NewArray()); data.Get("control_rules").Add(rule);
            if (fault == "signal-provider") data.Get("signals")[0].Get("providers")[0].Set("schema", JsonValue.Number("7"));
            return RecipeObservationTests.Create(new RecipeObservationModule(), management: true, initialize: init,
                extraModules: new GameModule[] { new AutomationModule(), module },
                extraDeclarations: "[{\"id\":\"sim.automation\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[]},{\"id\":\"proof.signal-bindings\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.operations\",\"version\":\"1.0.0\"}],\"documents\":[\"signal-bindings.json\"]}]",
                extraDocuments: new Dictionary<string, string> { { "signal-bindings.json", data.ToString() } },
                edit: (setup, recipes) =>
                {
                    var components = setup.Get("entities")[1].Get("components");
                    var source = Json.Parse("{\"schema_fault\":\"none\",\"operation\":{\"id\":\"metadata.sensor\",\"enabled\":true,\"target\":6,\"minimum\":0,\"maximum\":10,\"unit\":\"unit.level\"}}");
                    editSource?.Invoke(source); components.Set("metadata_source", source);
                    components.Get("observed_processor").Get("operation").Set("enabled", JsonValue.Of(false));
                    components.Set("signal_transformation_gate", Json.Parse("{\"id\":\"binding.gate\",\"transformations\":[\"contract.make\"],\"conditions\":[{\"id\":\"power\",\"source\":\"metadata.sensor\",\"signal\":\"binding.enabled\",\"expected\":true,\"blocker\":\"binding.no-power\"}]}"));
                    if (fault == "null" || fault == "foreign" || fault == "identity") source.Set("schema_fault", JsonValue.Of(fault));
                    if (fault == "unused")
                    {
                        var extra = Json.Parse("{\"id\":\"binding.unused\",\"kind\":\"entity\",\"components\":{}}");
                        var bad = source.Clone(); bad.Set("schema_fault", JsonValue.Of("foreign"));
                        extra.Get("components").Set("metadata_source", bad); setup.Get("entities").Add(extra);
                    }
                    if (fault == "duplicate")
                    {
                        var both = JsonValue.NewArray(); both.Add(source.Clone()); both.Add(source.Clone()); components.Set("metadata_source", both);
                    }
                    if (fault == "missing") components.Remove("metadata_source");
                    if (fault == "incompatible") components.Get("signal_transformation_gate").Get("conditions")[0].Set("source", JsonValue.Of("contract.process"));
                    editSetup?.Invoke(setup);
                });
        }
        private static OperationsView View(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<OperationsView>();
        private static EntityId Factory(SimGame game) => Entity(game, "contract.factory");
        internal static AutomationSettings Settings(SimGame game, Code target = default)
            => new AutomationSettings("binding.rule", true, new ControlBinding(Factory(game), target.IsNone ? Processor : target),
                new Dictionary<string, ControlBinding> { { "level", new ControlBinding(Factory(game), Source) } },
                new Dictionary<string, ControlThresholdBand> { { "high", new ControlThresholdBand(new Code("unit.level"), Fix.FromInt(5), Fix.FromInt(3)) } },
                new ControlAction(true, Fix.One, new Code("unit.work-rate")), new ControlAction(false, Fix.One, new Code("unit.work-rate")));
        private static AutomationRuleSnapshot Rule(SimGame game)
        {
            if (!View(game).TryGetAutomationRule(new PlayerId(0), "binding.live", out var value)) throw new InvalidOperationException("missing authored automation facts");
            return value;
        }
        private static bool Enabled(SimGame game)
        {
            if (!View(game).TryGetControl(Factory(game), Processor, out var value)) throw new InvalidOperationException("missing original processor control");
            return value.Enabled;
        }
        private static void Set(Check c, SimGame game, bool enabled, int target)
            => Order(c, game, new ConfigureOperationCommand { Entity = Factory(game), Operation = Source, Enabled = enabled, Target = Fix.FromInt(target) });
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current named signal binding build");
            Healthy(c); Faults(c);
            foreach (string fault in new[] { "null", "foreign", "identity", "unused", "duplicate", "missing", "incompatible", "signal-provider" })
            {
                var init = new InitializeProbe();
                c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(Create(new SignalBindingModule(), init, fault)), "authored pre-Initialize refusal " + fault);
                c.Equal(0, init.Initializations, "no partial Initialize " + fault);
            }
        }
        private static void Healthy(Check c)
        {
            var module = new SignalBindingModule(); var factory = Create(module); var game = ConstructionPlanTests.NewPoints(factory);
            var view = View(game); var id = Factory(game); var management = game.Seat(0).View.Feature<TransformationManagementView>(); ulong hash = game.Hash();
            c.That(module.PureNamedLookup(), "runtime/definition/type queries share original authored slot without identity getter execution");
            int before = module.SchemaReads;
            c.Equal(Code.None, view.ReadSignal(id, Source, "binding.level", out var retained), "contract-based numeric source read");
            c.That(module.SchemaReads > before && retained.Value == Fix.FromInt(6), "actual schema contract getter is used, never runtime Def cast");
            before = module.SchemaReads;
            c.That(view.TryGetSignalSchema(id, Source, out var schema) && module.SchemaReads > before
                && schema.StateCodec.Id == "metadata_source" && schema.Version == 1, "same registered actual immutable schema through contract");
            c.Equal(Code.None, management.ReadGates(id, Processor, "contract.make", out var gate), "native signal gate reads same contract metadata");
            c.That(gate.Blocker.IsNone && gate.Gates.Any(g => g.Capability == new Code("binding.gate") && g.Conditions.Single().Signal.Value == Fix.One), "native gate typed Boolean agrees");
            c.Equal(Code.None, view.ReadSignal(id, new Code("absent"), "binding.level", out var missing), "missing source projection typed");
            c.That(!missing.Available && missing.Blocker == OperationCodes.SignalUnavailable, "missing source no substitute");
            c.Equal(Code.None, view.ReadSignal(id, Processor, "binding.level", out var incompatible), "incompatible source projection typed");
            c.That(!incompatible.Available && incompatible.Blocker == OperationCodes.SignalUnavailable, "incompatible actual source no substitute");
            c.That(View(game, 1).ReadSignal(id, Source, "binding.level", out _) != Code.None
                && !View(game, 1).TryGetSignalSchema(id, Source, out _), "foreign source/schema private");
            c.Equal(hash, game.Hash(), "all named signal/schema/gate reads pure");
            Enable(c, game); var limits = new ReplayLimits(4 * 1024 * 1024, 3000);
            using (var recorder = game.RecordReplay(limits))
            {
                Order(c, game, new EnqueueTransformationCommand { Transformer = id, Capability = Processor, Transformation = "contract.make" });
                c.That(!Enabled(game) && Stock(game, "source") == Fix.FromInt(20), "disabled processor consumes no supply");
                Order(c, game, new ConfigureAutomationCommand { Rule = "binding.live", Settings = Settings(game) });
                Until(game, null, () => Enabled(game) && Rule(game).HasApplied && Process(game).Phase == TransformationPhase.Working, "metadata-bound deferred automation enables physical manufacturing");
                c.That(Stock(game, "source") == Fix.FromInt(16), "automatic activation still requires real four-unit hauling");
                Set(c, game, false, 6);
                Until(game, null, () => Process(game).Blocker == new Code("binding.no-power"), "authored Boolean gate physically pauses actual work");
                var held = Process(game).Progress; var twin = factory.Load(game.Save());
                for (int i = 0; i < 3; i++) Step(game, twin);
                c.That(Process(game).Progress.Equals(held), "unsafe signal gate earns no work over saved continuation");
                Set(c, game, true, 2);
                Until(game, null, () => !Enabled(game) && Rule(game).HasApplied && !Rule(game).Latch, "same native automation honors low signal and deferred shutdown");
                held = Process(game).Progress; twin = factory.Load(game.Save());
                for (int i = 0; i < 3; i++) Step(game, twin);
                c.That(Process(game).Progress.Equals(held), "actual operation disabled by rule remains physically paused");
                Set(c, game, true, 6);
                Until(game, null, () => Enabled(game) && Rule(game).Latch, "native dwell/cooldown restores actual process");
                twin = factory.Load(game.Save()); long at = game.Clock.StepIndex; hash = game.Hash();
                game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
                c.That(at == game.Clock.StepIndex && hash == game.Hash(), "sole clock pauses source, automation and production together");
                game.Clock.Resume(); twin.Clock.Resume(); Until(game, twin, () => Process(game).QueueCount == 0, "contract-bound physical completion");
                c.That(game.Seat(0).Snapshot.Entities.Count(e => e.Def == "contract.result") == 1 && Stock(game, "contract.factory") == Fix.One && Stock(game, "machine") == Fix.Zero,
                    "one native result and conserved reusable tool, no host gameplay");
                c.That(retained.Value == Fix.FromInt(6) && schema.StateCodec.Id == "metadata_source", "retained signal/schema facts detached");
                c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "normal player controls/automation/physical flow replay");
            }
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "settled saved native providers");
        }
        private static void Faults(Check c)
        {
            foreach (string reader in new[] { "signal", "schema", "gates", "automation" })
                foreach (ResourceSnapshotFault mode in Enum.GetValues(typeof(ResourceSnapshotFault)))
                {
                    var module = new SignalBindingModule(); var game = ConstructionPlanTests.NewPoints(Create(module)); var view = View(game); var id = Factory(game);
                    AutomationSignalSnapshot signal = default; AutomationSignalProviderSchema schema = null; ExecutionGateEvaluation gates = null;
                    Action read = () =>
                    {
                        if (reader == "signal") view.ReadSignal(id, Source, "binding.level", out signal);
                        else if (reader == "schema") view.TryGetSignalSchema(id, Source, out schema);
                        else if (reader == "gates") game.Seat(0).View.Feature<TransformationManagementView>().ReadGates(id, Processor, "contract.make", out gates);
                        else game.Seat(0).Preview(new ConfigureAutomationCommand { Rule = "binding.live", Settings = Settings(game) });
                    };
                    read(); module.Arm(mode);
                    c.Throws<InvalidOperationException>(read, "actual schema metadata fault " + reader + "/" + mode);
                    c.That(reader == "signal" ? signal.Equals(default(AutomationSignalSnapshot)) : reader == "schema" ? schema == null : reader == "gates" ? gates == null : true, "no partial source/schema/gate facts");
                    c.That(game.Fault != null && game.Fault.Phase == "observation.provider" && module.Reads == 1, "original scoped observation fault");
                    int reads = module.Reads; var fault = game.Fault;
                    c.Equal(0, game.Update(), "faulted Update stopped");
                    c.Throws<InvalidOperationException>(() => game.Save(), "faulted save stopped");
                    c.Throws<InvalidOperationException>(() => game.Hash(), "faulted hash stopped");
                    c.Throws<InvalidOperationException>(read, "faulted read cannot retry");
                    c.That(module.Reads == reads && ReferenceEquals(fault, game.Fault), "original fault and no callback retry");
                }
        }
    }
}
