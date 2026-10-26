using System;
using System.Reflection;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    internal static partial class TransformationProviderCutoffTests
    {
        private const string ReadCase = "Transformation pure participants: snapshots and admissions cannot mutate sibling processing state with held WIP";
        private static void PureParticipants(Check c)
        {
            // Existing public queue commands establish actual native WIP. Reflection below is
            // explicit negative framework instrumentation, never host gameplay or a stock grant.
            var enabled = typeof(Transformer).GetField("_enabled", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("missing actual native control backing field");
            foreach (bool loaded in new[] { false, true })
            foreach (string entry in new[] { "snapshot", "execution", "enqueue", "cancel", "removal" })
            foreach (string mode in new[] { "silent", "payload-throw", "clock-throw" })
            {
                var factory = Factory(); var game = Prepared(c, factory, loaded);
                var independent = factory.Load(game.Save()); ulong isolated = independent.Hash();
                var provider = Source(game); var native = Native(game); var entity = provider.Entity;
                var own = game.Seat(0).View.Feature<TransformationManagementView>(); var foreign = game.Seat(1).View.Feature<TransformationManagementView>();
                // Establish the original ordinary context before arming its transient read callback.
                c.That(own.TryGetTransformation(entity.Id, provider.CapabilityId, out _), "actual named provider read admitted before injection");
                var published = game.Seat(0).Snapshot; long time = game.Tick;
                ulong nativeBefore = Codec(native), providerBefore = Codec(provider); int version = entity.Version;
                bool controlBefore = native.ReadControl().Enabled; var original = new InvalidOperationException("proof.original-transformer-pure-read");
                int calls = 0; bool actualChange = false;
                string phase = entry == "enqueue" || entry == "cancel" ? entry : "snapshot";
                provider.PureReadProbe = (world, callback) => {
                    if (callback != phase) return;
                    calls++; if (calls != 1) return;
                    if (mode == "clock-throw") game.Clock.Pause();
                    else { enabled.SetValue(native, !controlBefore); actualChange = Codec(native) != nativeBefore; }
                    if (mode != "silent") throw original;
                };
                var context = entry + "/" + mode + "/loaded=" + loaded;
                Exception caught = null; bool? answer = null; Code? refusal = null;
                TransformationSnapshot snapshot = default; TransformationExecutionPreview preview = null;
                try
                {
                    c.That(!foreign.TryGetTransformation(entity.Id, provider.CapabilityId, out _), "foreign provider read refuses before callback: " + context);
                    c.Equal(0, calls, "private refusal executes no provider callback: " + context);
                    if (entry == "snapshot") answer = own.TryGetTransformation(entity.Id, provider.CapabilityId, out snapshot);
                    else if (entry == "execution") refusal = own.ReadExecutionPreview(entity.Id, provider.CapabilityId, "pulse", out preview);
                    else if (entry == "enqueue") refusal = own.PreviewEnqueue(entity.Id, provider.CapabilityId, "pulse", 1, false, default);
                    else if (entry == "cancel") refusal = own.PreviewCancel(entity.Id, provider.CapabilityId, 0);
                    else refusal = TransformationContracts.CheckRemoval(game.World, provider);
                }
                catch (Exception error) { caught = error; }
                finally { provider.PureReadProbe = null; }
                c.Equal(1, calls, "selected actual pure callback executes once: " + context);
                c.That(caught is InvalidOperationException && game.Fault != null, "pure provider cannot change sibling state and remain healthy: " + context);
                c.That(answer == null && refusal == null && snapshot.Queue == null && preview == null, "failed read/admission publishes no snapshot or success/refusal prefix: " + context);
                if (mode != "silent") c.That(ReferenceEquals(original, caught), "original callback failure retained without verification masking: " + context);
                if (mode != "clock-throw") c.That(actualChange && !native.ReadControl().Enabled && entity.Version == version,
                    "actual sibling codec changes silently without Touch, not fabricated rollback: " + context);
                c.Equal(providerBefore, Codec(provider), "original queried provider codec unchanged while sibling mutation is independently detected: " + context);
                c.That(Wip(game) == Fix.FromInt(3) && Stock(game) == Fix.FromInt(17), "read fault retains actual protected WIP and original source stock: " + context);
                var fault = game.Fault;
                c.Throws<InvalidOperationException>(() => game.Save(), "failed pure read cannot checkpoint healthy state: " + context);
                c.Throws<InvalidOperationException>(() => game.Hash(), "failed pure read cannot publish healthy hash: " + context);
                c.Equal(0, game.Update(), "failed pure read advances no further game step: " + context);
                c.Equal(time, game.Tick, "sole clock retains failed query coordinate: " + context);
                if (fault != null)
                {
                    c.Throws<InvalidOperationException>(() => own.TryGetTransformation(default, provider.CapabilityId, out _), "cached missing read refuses after fault: " + context);
                    c.Throws<InvalidOperationException>(() => foreign.PreviewCancel(entity.Id, provider.CapabilityId, 0), "cached private admission refuses after fault: " + context);
                    c.That(ReferenceEquals(fault, game.Fault), "first query fault retained: " + context);
                    c.That(game.Seat(0).Snapshot.State == GameState.Faulted && game.Seat(0).Snapshot.Tick == published.Tick,
                        "fault publication retains original healthy epoch: " + context);
                }
                c.Equal(isolated, independent.Hash(), "pure provider fault does not change independent saved materials or processing: " + context);
                independent.Update(); c.That(independent.Fault == null && independent.Tick > time, "independent mixed processing still advances: " + context);
            }
        }
    }
}
