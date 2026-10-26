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
    // Authored input and ordinary public player controls only; all gameplay remains in engine.
    internal static class OperationBindingTests
    {
        private const string Case = "Operation bindings: actual metadata owns controls status and physical command flow";
        private static readonly Code Source = new Code("metadata.sensor"), Processor = new Code("contract.process");
        internal static void Register() => TestRunner.Add(Case, Run);
        private static OperationsView View(SimGame game, int seat = 0) => game.Seat(seat).View.Feature<OperationsView>();
        private static EntityId Factory(SimGame game) => Entity(game, "contract.factory");
        private static ConfigureOperationCommand Command(SimGame game, bool enabled = true, int target = 6)
            => new ConfigureOperationCommand { Entity = Factory(game), Operation = Source, Enabled = enabled, Target = Fix.FromInt(target) };
        private static void Refusal(Check c, SimGame game, ConfigureOperationCommand command, Code expected)
        {
            ulong before = game.Hash(); c.Equal(expected, game.Seat(0).Preview(command), "normal refused preview");
            c.Equal(before, game.Hash(), "refused preview is pure");
            var result = TestKit.Order(game, 0, command); c.Equal(expected, result.Refusal, "same ordinary command refusal");
        }
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current operation contract build");
            Healthy(c); Faults(c); Malformed(c);
            foreach (string fault in new[] { "schema-null", "schema-foreign", "controls-null", "controls-foreign" })
                foreach (bool unused in new[] { false, true })
                {
                    var initialize = new InitializeProbe();
                    var factory = unused
                        ? SignalBindingTests.Create(new SignalBindingModule(), initialize, editSetup: setup =>
                        {
                            var source = setup.Get("entities")[1].Get("components").Get("metadata_source").Clone();
                            source.Set("operation_fault", JsonValue.Of(fault));
                            var extra = Json.Parse("{\"id\":\"binding.unused-operation\",\"kind\":\"entity\",\"components\":{}}");
                            extra.Get("components").Set("metadata_source", source); setup.Get("entities").Add(extra);
                        })
                        : SignalBindingTests.Create(new SignalBindingModule(), initialize, editSource: source =>
                            source.Set("operation_fault", JsonValue.Of(fault)));
                    c.Throws<ContentException>(() => ConstructionPlanTests.NewPoints(factory), "pre-Initialize actual metadata refusal " + fault + "/" + unused);
                    c.Equal(0, initialize.Initializations, "no partial initialization");
                }
        }
        private static void Healthy(Check c)
        {
            var module = new SignalBindingModule(); var factory = SignalBindingTests.Create(module); var game = ConstructionPlanTests.NewPoints(factory);
            var id = Factory(game); var view = View(game); var ids = new List<Code>(); ulong before = game.Hash();
            c.That(view.CopyOperationIds(id, ids) && ids.Contains(Source) && ids.Contains(Processor), "authored operations discovered through actual provider");
            int reads = module.OperationSchemaReads;
            c.That(view.TryGetOperationSchema(id, Source, out var retainedSchema) && module.OperationSchemaReads > reads
                && retainedSchema.StateCodec.Id == "metadata_source" && retainedSchema.StateCodec.SchemaVersion == 2 && retainedSchema.Controllable,
                "actual schema contract, not runtime Component Def");
            c.That(view.TryGetControl(id, Source, out var retainedControl) && retainedControl.Target == Fix.FromInt(6)
                && retainedControl.Minimum == Fix.Zero && retainedControl.Maximum == Fix.FromInt(10), "actual authored controls bounds");
            c.That(view.TryGetOperation(id, Source, out var retainedStatus) && retainedStatus.Status == OperationStatus.Running, "actual guarded status");
            c.Equal(before, game.Hash(), "operation ID/schema/control/status views pure");
            foreach (Code absent in new[] { Code.None, new Code("absent") })
                c.That(!view.TryGetOperationSchema(id, absent, out var schema) && schema == null
                    && !view.TryGetControl(id, absent, out var control) && control.Equals(default(OperationControlSnapshot))
                    && !view.TryGetOperation(id, absent, out var status) && status.Equals(default(OperationSnapshot)), "typed absent operation facts");
            c.That(!View(game, 1).CopyOperationIds(id, ids) && ids.Count == 0
                && !View(game, 1).TryGetControl(id, Source, out _) && !View(game, 1).TryGetOperationSchema(id, Source, out _)
                && !View(game, 1).TryGetOperation(id, Source, out _), "company private operation facts");
            Refusal(c, game, Command(game, target: 11), new Code("operation.target-bounds"));
            var missing = Command(game); missing.Operation = new Code("absent"); Refusal(c, game, missing, new Code("operation.not-controllable"));
            var none = Command(game); none.Operation = Code.None; Refusal(c, game, none, Codes.InvalidArgument);
            var foreign = Command(game); foreign.Entity = Entity(game, "destination"); Refusal(c, game, foreign, new Code("operation.not-controllable"));
            Enable(c, game); var limits = new ReplayLimits(4 * 1024 * 1024, 3000);
            using (var recorder = game.RecordReplay(limits))
            {
                Order(c, game, new EnqueueTransformationCommand { Transformer = id, Capability = Processor, Transformation = "contract.make" });
                c.That(Stock(game, "source") == Fix.FromInt(20), "disabled native processor does not consume");
                Order(c, game, new ConfigureAutomationCommand { Rule = "binding.live", Settings = SignalBindingTests.Settings(game) });
                Until(game, null, () => Process(game).Phase == TransformationPhase.Working, "actual control contracts enable physical manufacturing");
                c.That(Stock(game, "source") == Fix.FromInt(16), "actual provider requires timed hauling");
                Order(c, game, Command(game, false, 6));
                Until(game, null, () => Process(game).Blocker == new Code("binding.no-power"), "operation command pauses native signal gate");
                var held = Process(game).Progress; var twin = factory.Load(game.Save());
                for (int i = 0; i < 3; i++) Step(game, twin);
                c.That(Process(game).Progress.Equals(held), "disabled gate cannot earn saved work");
                c.That(view.TryGetOperation(id, Source, out var off) && off.Status == OperationStatus.Disabled
                    && view.TryGetControl(id, Source, out var controls) && !controls.Enabled, "authoritative disabled status and controls agree");
                Order(c, game, Command(game));
                twin = factory.Load(game.Save()); long at = game.Clock.StepIndex; before = game.Hash();
                game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
                c.That(at == game.Clock.StepIndex && before == game.Hash(), "one game clock pauses all operation decisions");
                game.Clock.Resume(); twin.Clock.Resume(); Until(game, twin, () => Process(game).QueueCount == 0, "native control physical completion");
                c.That(game.Clock.StepIndex > at && game.Seat(0).Snapshot.Entities.Count(e => e.Def == "contract.result") == 1
                    && Stock(game, "contract.factory") == Fix.One && Stock(game, "machine") == Fix.Zero, "one timed result and conserved tool");
                c.That(retainedControl.Enabled && retainedControl.Target == Fix.FromInt(6) && retainedStatus.Status == OperationStatus.Running
                    && retainedSchema.StateCodec.Id == "metadata_source", "retained facts detached");
                c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "same ordinary controls replay");
            }
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "settled operation save identity");
        }
        private static void Faults(Check c)
        {
            var reads = new[] {
                ("ids", "identity"), ("schema", "schema"), ("schema", "identity"),
                ("status", "status"), ("status", "identity"), ("control", "control"),
                ("control", "settings"), ("control", "schema"), ("preview", "check"),
                ("preview", "settings"), ("automation", "control"), ("automation", "schema") };
            foreach (var entry in reads) foreach (OperationBindingFault mode in Enum.GetValues(typeof(OperationBindingFault)))
            {
                var module = new SignalBindingModule();
                var factory = SignalBindingTests.Create(module, editSource: source =>
                { if (entry.Item1 == "automation") source.Get("operation").Set("unit", JsonValue.Of("unit.work-rate")); });
                var game = ConstructionPlanTests.NewPoints(factory); var view = View(game); var id = Factory(game);
                var ids = new List<Code>(); OperationProviderSchema schema = null; OperationSnapshot status = default; OperationControlSnapshot control = default; Code refusal = Code.None;
                Action read = () => {
                    if (entry.Item1 == "ids") view.CopyOperationIds(id, ids);
                    else if (entry.Item1 == "schema") view.TryGetOperationSchema(id, Source, out schema);
                    else if (entry.Item1 == "status") view.TryGetOperation(id, Source, out status);
                    else if (entry.Item1 == "control") view.TryGetControl(id, Source, out control);
                    else if (entry.Item1 == "preview") refusal = game.Seat(0).Preview(Command(game));
                    else refusal = game.Seat(0).Preview(new ConfigureAutomationCommand { Rule = "binding.live", Settings = SignalBindingTests.Settings(game, Source) });
                };
                read(); c.Equal(Code.None, refusal, "fault probe reaches an admitted actual target"); module.ArmOperation(entry.Item2, mode);
                c.Throws<InvalidOperationException>(read, "actual operation observation " + entry + "/" + mode);
                c.That(entry.Item1 == "ids" ? ids.Count == 0 : entry.Item1 == "schema" ? schema == null :
                    entry.Item1 == "status" ? status.Equals(default(OperationSnapshot)) : entry.Item1 == "control" ? control.Equals(default(OperationControlSnapshot)) : true,
                    "no partially published operation facts");
                AssertFault(c, game, module, read);
            }
        }
        private static void AssertFault(Check c, SimGame game, SignalBindingModule module, Action read)
        {
            c.That(game.Fault != null && game.Fault.Phase == "observation.provider" && module.Reads == 1, "original codec observation owns first fault");
            int reads = module.Reads; var fault = game.Fault;
            c.Equal(0, game.Update(), "faulted Update stopped");
            c.Throws<InvalidOperationException>(() => game.Hash(), "faulted hash stopped");
            c.Throws<InvalidOperationException>(() => game.Save(), "faulted save stopped");
            c.Throws<InvalidOperationException>(read, "faulted read stopped");
            c.That(module.Reads == reads && ReferenceEquals(fault, game.Fault), "no callbacks after original fault");
        }
        private static void Malformed(Check c)
        {
            foreach (string projection in new[] { "status", "control", "identity" })
            {
                var module = new SignalBindingModule(); var game = ConstructionPlanTests.NewPoints(SignalBindingTests.Create(module)); var view = View(game);
                OperationSnapshot status = default; OperationControlSnapshot control = default; var ids = new List<Code>();
                Action read = () => { if (projection == "status") view.TryGetOperation(Factory(game), Source, out status); else if (projection == "identity") view.CopyOperationIds(Factory(game), ids); else view.TryGetControl(Factory(game), Source, out control); };
                read(); module.ArmMalformed(projection);
                c.Throws<InvalidOperationException>(read, "malformed operation facts refused");
                c.That(ids.Count == 0 && status.Equals(default(OperationSnapshot)) && control.Equals(default(OperationControlSnapshot))
                    && game.Fault?.Phase == "observation.provider", "malformed facts never publish");
            }
        }
    }
}
