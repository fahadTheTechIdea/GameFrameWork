using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;
using static Beep.SimEngine.Tests.TransformationContentContractTests;

namespace Beep.SimEngine.Tests
{
    // Ordinary player authoring, previews/commands, detached views, Update and persistence.
    internal static class ProcessManagementTests
    {
        internal const string Case = "Process management: guarded metadata gates and command preview parity";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static readonly Code Capability = new Code("contract.process");
        private static SimGame New(RecipeObservationModule module, out GameFactory factory)
        {
            factory = RecipeObservationTests.Create(module, management: true);
            return ConstructionPlanTests.NewPoints(factory);
        }
        private static EnqueueTransformationCommand Enqueue(SimGame game, string recipe = "contract.make", int count = 1)
            => new EnqueueTransformationCommand { Transformer = Entity(game, "contract.factory"), Capability = Capability, Transformation = recipe, Count = count };
        private static void Run(Check c)
        {
            c.Equal(StateCodecTests.ExpectedBuild, EngineInfo.Build, "current management boundary build");
            Healthy(c);
            foreach (string reader in new[] { "staffing", "gates", "cost", "preview", "catalog", "command" })
                foreach (ResourceSnapshotFault mode in Enum.GetValues(typeof(ResourceSnapshotFault))) Fault(c, reader, "support", mode);
            foreach (ResourceSnapshotFault mode in Enum.GetValues(typeof(ResourceSnapshotFault)))
            {
                Fault(c, "cost", "cost", mode);
                foreach (string reader in new[] { "output", "preview", "command" }) Fault(c, reader, "output", mode);
                Fault(c, "gates", "gate", mode); Fault(c, "staffing", "staffing", mode);
            }
            foreach (string reader in new[] { "gates", "staffing" })
            {
                var module = new RecipeObservationModule(); var game = New(module, out _); var probe = Probe(game, reader);
                probe.Read(); module.ArmHiddenGate(reader == "gates" ? "gate" : "staffing");
                Poisoned(c, game, module, probe, reader + " hidden codec mutation", false);
                for (int mode = 1; mode <= 3; mode++)
                {
                    module = new RecipeObservationModule(); game = New(module, out _); probe = Probe(game, reader);
                    probe.Read(); module.ArmMalformed(mode);
                    Poisoned(c, game, module, probe, reader + " malformed facts " + mode, false);
                }
            }
        }
        private static void Healthy(Check c)
        {
            var module = new RecipeObservationModule(); var game = New(module, out var factory);
            var view = game.Seat(0).View.Feature<TransformationManagementView>(); var id = Entity(game, "contract.factory");
            ulong hash = game.Hash();
            c.That(view.TransformationsOf(id, Capability).SequenceEqual(new[] { "contract.make" }), "actual named supported recipe enumeration");
            c.Equal("contract.result", view.TransformationEntityOutput("contract.make"), "detached authored entity output ID");
            c.That(view.TransformationEntityOutput("absent") == null && view.TransformationEntityOutput("observation.hidden") == null, "unknown/hidden output IDs absent");
            c.Equal(Code.None, view.ReadStaffing(id, Capability, "contract.make", out var staffing), "actual staffing view");
            c.That(staffing.Count == 1 && staffing[0].Capability == new Code("management.staff") && staffing[0].Applies && staffing[0].Blocker.IsNone, "original immutable staffing facts");
            c.Equal(Code.None, view.ReadGates(id, Capability, "contract.make", out var gates), "actual gate evaluation");
            c.That(gates.Blocker.IsNone && gates.Gates.Any(g => g.Capability == new Code("management.staff")), "same actual gate owner");
            c.Equal(Code.None, view.ReadMonetaryCost(id, Capability, "contract.make", out var cost), "optional cost facts without Tycoon");
            c.That(cost.Applicability == MonetaryCostApplicability.NotAuthored && !game.Modules.Contains("sim.tycoon"), "no mandatory money or business module");
            var output = view.PreviewEntityOutput(id, Capability, "contract.make");
            c.That(output.Refusal.IsNone && output.Output == "contract.result", "engine-calculated output eligibility");
            foreach (string recipe in new[] { "contract.make", "absent", "observation.hidden", "observation.unused" })
                foreach (int count in new[] { 0, 1 })
                {
                    var command = Enqueue(game, recipe, count);
                    c.Equal(game.Seat(0).Preview(command), view.PreviewEnqueue(id, Capability, recipe, count, false, EntityId.None), "management/normal command refusal parity " + recipe + "/" + count);
                }
            var other = game.Seat(1).View.Feature<TransformationManagementView>();
            c.That(other.TransformationsOf(id, Capability).Count == 0 && other.PreviewEntityOutput(id, Capability, "contract.make").Refusal == Codes.NotFound, "foreign company metadata private");
            c.Equal(Codes.NotFound, other.ReadStaffing(id, Capability, "contract.make", out var privateStaff), "foreign staffing refused");
            c.Equal(Codes.NotFound, other.ReadGates(id, Capability, "contract.make", out var privateGates), "foreign gates refused");
            c.That(privateStaff.Count == 0 && privateGates == null, "foreign view no facts");
            var queue = new List<TransformationQueueEntry>();
            c.That(view.CopyQueue(id, Capability, queue) && queue.Count == 0, "existing detached empty queue read");
            c.That(view.TryGetTransformation(id, Capability, out var state) && state.QueueCount == 0, "existing guarded process snapshot read");
            var cancel = new CancelTransformationCommand { Transformer = id, Capability = Capability, Index = 0 };
            c.Equal(game.Seat(0).Preview(cancel), view.PreviewCancel(id, Capability, 0), "empty cancellation preview parity");
            foreach (string recipe in new[] { "absent", "observation.hidden", "observation.unused" })
            {
                Code expected = recipe == "observation.unused" ? EnqueueTransformationHandler.NotMadeHere : Codes.UnknownDef;
                c.Equal(expected, view.ReadStaffing(id, Capability, recipe, out var refusedStaff), "staffing recipe refusal");
                c.Equal(expected, view.ReadGates(id, Capability, recipe, out var refusedGate), "gate recipe refusal");
                c.Equal(expected, view.ReadMonetaryCost(id, Capability, recipe, out var refusedCost), "cost recipe refusal");
                c.That(refusedStaff.Count == 0 && refusedGate == null && refusedCost.Category == MonetaryCostCategory.Unavailable
                    && view.PreviewEntityOutput(id, Capability, recipe).Refusal == expected, "refused recipe no partial facts");
            }
            c.Throws<NotSupportedException>(() => ((IList<TransformationStaffingSnapshot>)staffing).Clear(), "staffing list readonly");
            c.Throws<NotSupportedException>(() => ((IList<ExecutionGateSnapshot>)gates.Gates).Clear(), "gate list readonly");
            c.Equal(hash, game.Hash(), "all management reads/normal previews pure");
            Enable(c, game); var limits = new ReplayLimits(4 * 1024 * 1024, 3000);
            using (var recorder = game.RecordReplay(limits))
            {
                Order(c, game, Enqueue(game));
                c.That(game.Seat(0).Snapshot.Entities.All(e => e.Def != "contract.result"), "preview never creates result");
                Until(game, null, () => Process(game).Phase == TransformationPhase.Working, "actual physically supplied authored manufacturing");
                c.That(view.CopyQueue(id, Capability, queue) && queue.Count == 1 && queue[0].Transformation == "contract.make", "same actual queued physical recipe");
                c.Equal(game.Seat(0).Preview(cancel), view.PreviewCancel(id, Capability, 0), "working cancellation preview parity");
                c.That(!other.CopyQueue(id, Capability, queue) && queue.Count == 0
                    && !other.TryGetTransformation(id, Capability, out _), "foreign queue and progress remain private");
                var twin = factory.Load(game.Save()); long at = game.Clock.StepIndex; hash = game.Hash();
                game.Clock.Pause(); twin.Clock.Pause(); Step(game, twin);
                c.That(at == game.Clock.StepIndex && hash == game.Hash(), "single clock pauses actual manufacturing");
                game.Clock.Resume(); twin.Clock.Resume();
                Until(game, twin, () => Process(game).QueueCount == 0, "same native timed output completion");
                c.That(game.Seat(0).Snapshot.Entities.Count(e => e.Def == "contract.result") == 1 && Stock(game, "source") == Fix.FromInt(16)
                    && Stock(game, "contract.factory") == Fix.One && Stock(game, "machine") == Fix.Zero, "original physical resources/tools/result, no free output");
                c.That(staffing[0].Capability == new Code("management.staff") && output.Output == "contract.result", "retained facts unchanged after actual production");
                c.That(new ReplayVerifier().Verify(factory, recorder.Finish(), limits).Success, "same player inputs replay with authored observation providers");
            }
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed saved native state with original gate codec");
        }
        private sealed class ReadProbe
        {
            internal Action Read;
            internal Func<bool> Empty;
        }
        private static ReadProbe Probe(SimGame game, string reader)
        {
            var view = game.Seat(0).View.Feature<TransformationManagementView>(); var id = Entity(game, "contract.factory");
            IReadOnlyList<TransformationStaffingSnapshot> staffing = null; ExecutionGateEvaluation gates = null;
            IReadOnlyList<string> catalog = null; MonetaryCostDecision cost = default; EntityOutputPreview preview = default;
            string output = null;
            if (reader == "staffing") return new ReadProbe { Read = () => view.ReadStaffing(id, Capability, "contract.make", out staffing), Empty = () => staffing.Count == 0 };
            if (reader == "gates") return new ReadProbe { Read = () => view.ReadGates(id, Capability, "contract.make", out gates), Empty = () => gates == null };
            if (reader == "cost") return new ReadProbe { Read = () => view.ReadMonetaryCost(id, Capability, "contract.make", out cost), Empty = () => cost.Category == MonetaryCostCategory.Unavailable };
            // Return-value methods leave the caller's prior value intact when they throw; they must
            // not return new/partial facts. Out-value methods explicitly clear before callbacks.
            if (reader == "preview") return new ReadProbe { Read = () => preview = view.PreviewEntityOutput(id, Capability, "contract.make"), Empty = () => preview.Output == "contract.result" };
            if (reader == "catalog") return new ReadProbe { Read = () => catalog = view.TransformationsOf(id, Capability), Empty = () => catalog.Count == 1 && catalog[0] == "contract.make" };
            if (reader == "output") return new ReadProbe { Read = () => output = view.TransformationEntityOutput("contract.make"), Empty = () => output == "contract.result" };
            return new ReadProbe { Read = () => game.Seat(0).Preview(Enqueue(game)), Empty = () => true };
        }
        private static void Fault(Check c, string reader, string site, ResourceSnapshotFault mode)
        {
            var module = new RecipeObservationModule(); var game = New(module, out _); var probe = Probe(game, reader);
            probe.Read(); module.Arm(site, mode);
            Poisoned(c, game, module, probe, reader + "/" + site + "/" + mode, true);
        }
        private static void Poisoned(Check c, SimGame game, RecipeObservationModule module, ReadProbe probe, string label, bool callback)
        {
            c.Throws<InvalidOperationException>(probe.Read, "actual guarded refusal " + label);
            c.That(probe.Empty(), "no partial output publication " + label);
            c.That(game.Fault != null && game.Fault.Phase == "observation.provider", "original observation fault " + label);
            int reads = module.Reads; var first = game.Fault;
            if (callback) c.Equal(1, reads, "one armed callback " + label);
            c.Equal(0, game.Update(), "poisoned Update stopped");
            c.Throws<InvalidOperationException>(() => game.Save(), "poisoned save stopped");
            c.Throws<InvalidOperationException>(() => game.Hash(), "poisoned hash stopped");
            c.Throws<InvalidOperationException>(probe.Read, "poisoned read cannot retry");
            c.That(module.Reads == reads && ReferenceEquals(first, game.Fault), "first fault retained and callback not retried");
        }
    }
}
