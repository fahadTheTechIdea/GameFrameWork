using System;
using System.Collections.Generic;
using System.Reflection;
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
    internal static partial class RecipeObservationTests
    {
        private const string ParticipantsCase = "Recipe participants: pure projections protect sibling controls and selected space";
        private static void Participants(Check c)
        {
            var enabled = typeof(Transformer).GetField("_enabled", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("missing actual native enabled field");
            foreach (bool loaded in new[] { false, true })
            foreach (string reader in new[] { "rates", "ports", "staffing", "catalog", "snapshot", "entity-output" })
            foreach (string mode in new[] { "sibling", "space", "sibling-throw", "space-throw" })
            {
                // A definition-only query has no bound entity: assert selected-space purity,
                // not a fabricated global private-state sandbox for arbitrary captured entities.
                if (reader == "entity-output" && mode.StartsWith("sibling", StringComparison.Ordinal)) continue;
                var module = new RecipeObservationModule();
                var factory = Create(module, management: true, edit: (setup, document) => {
                    var parts = setup.Get("entities")[1].Get("components");
                    var siblingPart = parts.Get("observed_processor").Clone();
                    siblingPart.Get("operation").Set("id", JsonValue.Of("sibling.process"));
                    parts.Set("transformer", siblingPart);
                });
                var game = ConstructionPlanTests.NewPoints(factory);
                var id = Entity(game, "contract.factory");
                PermanentRemovalTests.Submit(c, game, new EnqueueTransformationCommand {
                    Transformer = id, Capability = Capability, Transformation = "contract.make" });
                if (loaded) game = factory.Load(game.Save());
                var independent = factory.Load(game.Save()); ulong isolated = independent.Hash();
                var entity = game.World.Entities.Get(id);
                var source = entity.Get<ITransformer>(Capability);
                var sibling = (Transformer)entity.Get<ITransformer>(new Code("sibling.process"));
                ulong Codec(Component part) { var hash = new StateHasher(); part.Sync(hash); return hash.Value; }
                ulong ownBefore = Codec((Component)source), siblingBefore = Codec(sibling);
                int version = entity.Version; long at = game.Tick; var before = game.Seat(0).Snapshot;
                var point = (PointSpace)game.World.Space;
                var view = game.Seat(0).View.Feature<TransformationManagementView>();
                c.That(source != sibling && sibling.ReadControl().Enabled && point.AccessOpen,
                    "two actual named native controls and open selected space precede injection");
                c.That(view.TryGetTransformation(id, Capability, out var originalFacts) && originalFacts.QueueCount == 1,
                    "ordinary command establishes actual selected recipe queue before its pure projection");
                string phase = reader == "ports" ? "ports" : reader == "catalog" || reader == "staffing" ? "support"
                    : reader == "snapshot" ? "visibility" : reader == "entity-output" ? "output" : "recipe";
                var original = new InvalidOperationException("proof.original-recipe-participant");
                bool changed = false;
                module.ArmProbe(phase, () => {
                    if (mode.StartsWith("space", StringComparison.Ordinal)) { point.SetAccess(false); changed = !point.AccessOpen; }
                    else { enabled.SetValue(sibling, false); changed = Codec(sibling) != siblingBefore; }
                    if (mode.EndsWith("-throw", StringComparison.Ordinal)) throw original;
                });
                var context = reader + "/" + mode + "/loaded=" + loaded;
                IReadOnlyList<ResourceRateInfo> rates = null; IReadOnlyList<ResourcePortSnapshot> ports = null;
                IReadOnlyList<TransformationStaffingSnapshot> staffing = null; IReadOnlyList<string> catalog = null;
                TransformationSnapshot snapshot = default; bool? found = null; Code? refusal = null; string output = null;
                Exception caught = null;
                try
                {
                    if (reader != "entity-output")
                    {
                        c.Equal(Codes.NotFound, game.Seat(1).View.Feature<TransformationManagementView>()
                            .ReadToolConsumptionRates(id, Capability, "contract.make", out var foreign), "private refusal before recipe callback: " + context);
                        c.That(foreign.Count == 0 && module.Reads == 0, "foreign refusal exposes neither facts nor callback: " + context);
                    }
                    if (reader == "rates") refusal = view.ReadToolConsumptionRates(id, Capability, "contract.make", out rates);
                    else if (reader == "ports") refusal = view.ReadInputPorts(id, Capability, "contract.make", out ports);
                    else if (reader == "staffing") refusal = view.ReadStaffing(id, Capability, "contract.make", out staffing);
                    else if (reader == "catalog") catalog = view.TransformationsOf(id, Capability);
                    else if (reader == "snapshot") found = view.TryGetTransformation(id, Capability, out snapshot);
                    else output = view.TransformationEntityOutput("contract.make");
                }
                catch (Exception error) { caught = error; }
                finally { module.Disarm(); }
                c.Equal(1, module.Reads, "actual selected metadata callback executes once: " + context);
                c.That(changed && entity.Version == version, "actual saved control or space changed without entity Touch: " + context);
                c.That(caught is InvalidOperationException && game.Fault != null, "pure recipe projection cannot leave mutated participants healthy: " + context);
                if (mode.EndsWith("-throw", StringComparison.Ordinal)) c.That(ReferenceEquals(original, caught), "original callback error retained: " + context);
                c.That(refusal == null && found == null && catalog == null && output == null && snapshot.Queue == null
                    && (rates == null || rates.Count == 0) && (ports == null || ports.Count == 0) && (staffing == null || staffing.Count == 0),
                    "failed projection returns no facts, bool or success/refusal prefix: " + context);
                c.Equal(ownBefore, Codec((Component)source), "selected processor codec unchanged; sibling/space guard is independently necessary: " + context);
                if (mode.StartsWith("sibling", StringComparison.Ordinal)) c.That(!sibling.ReadControl().Enabled, "fault is cutoff, not fabricated control rollback: " + context);
                else c.That(!point.AccessOpen, "fault is cutoff, not fabricated selected-space rollback: " + context);
                var fault = game.Fault;
                c.Throws<InvalidOperationException>(() => game.Save(), "failed recipe cannot save healthy state: " + context);
                c.Throws<InvalidOperationException>(() => game.Hash(), "failed recipe cannot publish healthy hash: " + context);
                c.Equal(0, game.Update(), "failed recipe advances no game update: " + context);
                c.Equal(at, game.Tick, "sole clock retains query coordinate: " + context);
                if (fault != null)
                {
                    c.Throws<InvalidOperationException>(() => view.TransformationsOf(default, Capability), "cached missing shortcut refuses after first fault: " + context);
                    c.That(ReferenceEquals(fault, game.Fault) && game.Seat(0).Snapshot.State == GameState.Faulted
                        && game.Seat(0).Snapshot.Tick == before.Tick, "first fault and last healthy publication retained: " + context);
                }
                c.Equal(isolated, independent.Hash(), "negative callback does not change unarmed saved session: " + context);
                independent.Update(); c.That(independent.Fault == null && independent.Tick > at, "independent physical queue still advances: " + context);
            }
        }
    }
}
