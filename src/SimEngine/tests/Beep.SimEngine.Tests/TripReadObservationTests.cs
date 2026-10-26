using System;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // The existing provider's explicit negative hook is armed only on isolated diagnostic sessions.
    internal static class TripReadObservationTests
    {
        internal const string Case = "Trip read observation: whole saved workers and original getter errors withhold policy and service facts";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static void Fault(Check c, SimGame game, FleetTripProofModule module, EntityId worker, int fault, int path, bool loaded)
        {
            var view = game.Seat(0).View.Feature<LogisticsView>(); long at = game.Clock.StepIndex;
            var known = new HaulTripSettings("trip.funded", true, false); string label = "read=" + fault + "/path=" + path + "/loaded=" + loaded;
            if (loaded) c.That(PermanentRemovalTests.Facts(game, worker).Stock == Fix.FromInt(4), label + " starts with physically loaded cargo");
            module.ArmObservationFault(worker); Exception caught = null; bool published = false; object payload = null;
            try
            {
                if (path == 0) { view.ReadHaulTripPolicy(worker, out var facts); payload = facts; }
                else if (path == 1) { view.ReadHaulTripServiceSources(worker, out var facts); payload = facts; }
                else if (path == 2) { view.ReadHaulTripServiceWindow(worker, out var facts); payload = facts; }
                else if (path == 3) view.PreviewHaulTripPolicy(worker, known);
                else if (path == 4) payload = view.PreviewHaulTripPolicyGroup(new[] { worker }, known);
                else if (path == 5) game.Seat(0).Preview(new ConfigureHaulTripPolicyCommand { Worker = worker, Settings = known });
                else game.Seat(0).Preview(new ConfigureHaulTripPolicyGroupCommand { Workers = new[] { worker }, Settings = known });
                published = true;
            }
            catch (Exception error) { caught = error; }
            c.That(!published && payload == null, label + " callback failure publishes no policy/service/admission prefix");
            c.That(game.Fault != null && game.State == GameState.Faulted, label + " actual getter failure faults the session");
            if (fault != 11)
            {
                c.That(caught?.Message == "proof.trip-read-first-error", label + " original getter error is not masked by finally verification");
                c.That(game.Fault?.Message == "InvalidOperationException: proof.trip-read-first-error", label + " first failure keeps the original getter cause");
            }
            else c.That(caught is InvalidOperationException, label + " silent sibling Held mutation cannot produce healthy facts");
            if (game.Fault == null) return; // Collect baseline failures without stepping corrupt diagnostic sessions.
            var first = game.Fault;
            c.Throws<InvalidOperationException>(() => game.Save(), label + " no healthy saved prefix after observation failure");
            c.Throws<InvalidOperationException>(() => view.ReadHaulTripServiceWindow(worker, out _), label + " retained service view rejects after fault");
            c.Throws<InvalidOperationException>(() => game.Seat(0).Preview(new ConfigureHaulTripPolicyCommand { Worker = worker, Settings = known }), label + " later preview cannot recover a faulted session");
            c.That(ReferenceEquals(first, game.Fault), label + " subsequent refusals keep the first error");
            c.Equal(at, game.Clock.StepIndex, label + " query does not advance the sole clock");
        }
        private static void Run(Check c)
        {
            foreach (int fault in new[] { 8, 9, 10, 11, 12 })
            {
                var module = new FleetTripProofModule(); var factory = TripAdmissionObservationTests.Factory(fault, module);
                TripAdmissionObservationTests.Physical(c, factory, (sameFactory, loaded, worker) => {
                    byte[] save = loaded.Save();
                    for (int path = 0; path < 7; path++)
                    {
                        var restored = sameFactory.Load(save); Fault(c, restored, module, worker, fault, path, true);
                        var fresh = TripAdmissionObservationTests.New(sameFactory);
                        var actual = TransformationContentContractTests.Entity(fresh, "reserve.truck"); Fault(c, fresh, module, actual, fault, path, false);
                    }
                }, "trip-read-observation/" + fault);
            }
        }
    }
}
