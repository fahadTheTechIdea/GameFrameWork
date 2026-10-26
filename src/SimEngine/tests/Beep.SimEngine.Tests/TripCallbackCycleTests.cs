using System;
using System.Collections.Generic;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Bounded negative callbacks are in the selected provider; normal gameplay uses original public commands.
    internal static class TripCallbackCycleTests
    {
        internal const string Case = "Trip callback cycles: reject recursive providers while valid current-setting reads retain saved hauling";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static void Fault(Check c, SimGame game, FleetTripProofModule module, EntityId worker, int fault, int path, bool loaded)
        {
            string label = "cycle=" + fault + "/path=" + path + "/loaded=" + loaded;
            if (loaded) c.Equal(Fix.FromInt(4), PermanentRemovalTests.Facts(game, worker).Stock, label + " actual saved loaded worker");
            var view = game.Seat(0).View.Feature<LogisticsView>(); var requested = new HaulTripSettings("trip.funded", false, false);
            long at = game.Clock.StepIndex; module.ArmObservationFault(worker);
            Exception caught = null; bool published = false; int sequence = 0;
            try
            {
                if (fault == 23)
                {
                    if (path < 2) { module.ProbeConfigure(worker, requested, path == 1); published = true; }
                    else
                    {
                        Command command = path == 2 ? new ConfigureHaulTripPolicyCommand { Worker = worker, Settings = requested }
                            : new ConfigureHaulTripPolicyGroupCommand { Workers = new[] { worker }, Settings = requested };
                        sequence = game.Seat(0).Submit(command); game.Update();
                    }
                }
                else
                {
                    if (fault == 21)
                    {
                        if (path == 0) view.PreviewHaulTripPolicy(worker, requested);
                        else game.Seat(0).Preview(new ConfigureHaulTripPolicyGroupCommand { Workers = new[] { worker }, Settings = requested });
                    }
                    else if (path == 0) view.ReadHaulTripPolicy(worker, out _);
                    else if (path == 1) view.ReadHaulTripServiceWindow(worker, out _);
                    else game.Seat(0).Preview(new ConfigureHaulTripPolicyCommand { Worker = worker, Settings = requested });
                    published = true;
                }
            }
            catch (Exception error) { caught = error; }
            // An ordinary preview already owns its check frame before it reads current
            // settings: read -> check then repeats CHECK first, not the still-active read.
            string operation = fault == 23 ? "commit" : fault == 21 || fault == 20 && path == 2 ? "check" : "read";
            c.That(!published, label + " recursive provider returns no successful read/admission/commit");
            c.That(game.Fault != null && game.State == GameState.Faulted, label + " callback cycle faults the original session");
            c.That(sequence != 0 || caught is InvalidOperationException && caught.Message.StartsWith("recursive provider callback:", StringComparison.Ordinal), label + " rejects the actual cycle before another callback body");
            c.Equal("logistics.trip-policy." + operation, game.Fault?.Phase, label + " original cycle operation owns the first fault");
            if (sequence != 0)
            {
                long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
                c.That(!results.Any(row => row.Sequence == sequence && row.Accepted), label + " no accepted command result prefix");
            }
            if (game.Fault == null) return; // Collect bounded baseline failures; never retry a corrupt session.
            var first = game.Fault;
            c.Throws<InvalidOperationException>(() => game.Save(), label + " no healthy checkpoint after cycle");
            c.Throws<InvalidOperationException>(() => view.ReadHaulTripPolicy(worker, out _), label + " retained view cannot restart callback");
            c.That(ReferenceEquals(first, game.Fault), label + " original cycle remains first failure");
            if (sequence == 0) c.Equal(at, game.Clock.StepIndex, label + " cycle query advances no game time");
        }
        private static void Run(Check c)
        {
            // A provider can legitimately query its current settings during admission.
            TripAdmissionObservationTests.Physical(c, TripAdmissionObservationTests.Factory(22), evidence: "trip-callback-cycle/valid-check-read");
            foreach (int fault in new[] { 19, 20, 21, 23 })
            {
                var module = new FleetTripProofModule(); var factory = TripAdmissionObservationTests.Factory(fault, module);
                TripAdmissionObservationTests.Physical(c, factory, (sameFactory, loaded, worker) => {
                    byte[] save = loaded.Save(); int count = fault == 23 ? 4 : fault == 21 ? 2 : 3;
                    for (int path = 0; path < count; path++)
                    {
                        var restored = sameFactory.Load(save); Fault(c, restored, module, worker, fault, path, true);
                        var fresh = TripAdmissionObservationTests.New(sameFactory);
                        Fault(c, fresh, module, TransformationContentContractTests.Entity(fresh, "reserve.truck"), fault, path, false);
                    }
                }, "trip-callback-cycle/" + fault);
            }
        }
    }
}
