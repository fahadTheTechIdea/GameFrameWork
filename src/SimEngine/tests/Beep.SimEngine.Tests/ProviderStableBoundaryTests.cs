using System;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Callback checkpoint/hash/update attempts are explicit engine-side negatives, not driver gameplay.
    internal static class ProviderStableBoundaryTests
    {
        internal const string Case = "Provider stable boundaries: no nested checkpoint hash or game update even when callback suppresses refusal";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static void Fault(Check c, GameFactory factory, SimGame game, FleetTripProofModule module, EntityId worker, int fault, int path, bool loaded)
        {
            string label = "boundary=" + fault + "/path=" + path + "/loaded=" + loaded;
            if (loaded) c.Equal(Fix.FromInt(4), PermanentRemovalTests.Facts(game, worker).Stock, label + " actual saved finite loaded cargo");
            var view = game.Seat(0).View.Feature<LogisticsView>(); long at = game.Clock.StepIndex;
            module.ArmObservationFault(worker, factory.GameName); Exception caught = null; bool published = false;
            try
            {
                if (path == 0) view.ReadHaulTripPolicy(worker, out _);
                else if (path == 1) view.ReadHaulTripServiceWindow(worker, out _);
                else game.Seat(0).Preview(new ConfigureHaulTripPolicyCommand { Worker = worker, Settings = new HaulTripSettings("trip.funded", false, false) });
                published = true;
            }
            catch (Exception error) { caught = error; }
            var probe = module.ReadNestedOperationProbe(worker);
            c.That(!published && caught is InvalidOperationException, label + " no successful read after callback crosses stable-state boundary");
            c.That(game.Fault != null && game.State == GameState.Faulted, label + " rejected or suppressed nested operation faults session");
            c.Equal("provider.operation", game.Fault?.Phase, label + " stable-state owner latches the first boundary refusal");
            c.That(!probe.Captured, label + " no checkpoint/hash result or nested update return escapes callback");
            c.Equal(at, game.Clock.StepIndex, label + " nested update never starts the sole game clock");
            if (fault == 25 || fault == 27 || fault == 29) c.That(probe.Swallowed, label + " negative callback actually suppresses the refused operation");
            if (game.Fault == null) return; // Preserve baseline evidence, never continue an invalid diagnostic session.
            var first = game.Fault;
            c.Throws<InvalidOperationException>(() => game.Save(), label + " no later healthy checkpoint");
            c.Throws<InvalidOperationException>(() => game.Hash(), label + " no later healthy complete-world hash");
            c.Throws<InvalidOperationException>(() => view.ReadHaulTripPolicy(worker, out _), label + " retained query cannot retry callback");
            c.That(ReferenceEquals(first, game.Fault), label + " later refusals preserve original boundary fault");
        }
        private static void Run(Check c)
        {
            foreach (int fault in new[] { 24, 25, 26, 27, 28, 29 })
            {
                var module = new FleetTripProofModule(); var factory = TripAdmissionObservationTests.Factory(fault, module);
                TripAdmissionObservationTests.Physical(c, factory, (sameFactory, loaded, worker) => {
                    byte[] save = loaded.Save();
                    for (int path = 0; path < 3; path++)
                    {
                        var restored = sameFactory.Load(save); Fault(c, sameFactory, restored, module, worker, fault, path, true);
                        var fresh = TripAdmissionObservationTests.New(sameFactory);
                        Fault(c, sameFactory, fresh, module, TransformationContentContractTests.Entity(fresh, "reserve.truck"), fault, path, false);
                    }
                }, "provider-stable-boundary/" + fault);
            }
        }
    }
}
