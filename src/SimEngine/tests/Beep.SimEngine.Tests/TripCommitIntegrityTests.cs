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
    // Ordinary player inputs run positive gameplay; direct calls are explicit negative contract diagnostics.
    internal static class TripCommitIntegrityTests
    {
        internal const string Case = "Trip commit integrity: settings-only changes preserve saved physical cargo clock and sibling state";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static void Fault(Check c, SimGame game, FleetTripProofModule module, EntityId worker, int fault, int path, bool loaded)
        {
            string label = "commit=" + fault + "/path=" + path + "/loaded=" + loaded;
            var view = game.Seat(0).View.Feature<LogisticsView>(); var requested = new HaulTripSettings("trip.funded", false, false);
            if (loaded) c.Equal(Fix.FromInt(4), PermanentRemovalTests.Facts(game, worker).Stock, label + " actual loaded cargo before negative commit");
            ulong before = game.Hash(); c.Equal(Code.None, view.PreviewHaulTripPolicy(worker, requested), label + " pure fresh admission accepts intended settings");
            c.Equal(before, game.Hash(), label + " admission changes no original state");
            Exception caught = null; bool committed = false; int sequence = 0;
            try
            {
                if (path < 2) { module.ProbeConfigure(worker, requested, path == 1); committed = true; }
                else
                {
                    Command command = path == 2 ? new ConfigureHaulTripPolicyCommand { Worker = worker, Settings = requested }
                        : new ConfigureHaulTripPolicyGroupCommand { Workers = new[] { worker }, Settings = requested };
                    sequence = game.Seat(0).Submit(command); game.Update();
                }
            }
            catch (Exception error) { caught = error; }
            c.That(!committed, label + " direct contract never returns success after an invalid commit");
            c.That(game.Fault != null && game.State == GameState.Faulted, label + " original session faults before publishing success");
            if (fault == 16 || fault == 18)
            {
                c.That(path >= 2 || caught?.Message == "proof.trip-commit-first-error", label + " thrown callback keeps original cause");
                c.That(game.Fault?.Message == "InvalidOperationException: proof.trip-commit-first-error", label + " first-fault record keeps original callback error");
            }
            else c.That(path >= 2 || caught is InvalidOperationException, label + " successful-return mutation is rejected");
            if (sequence != 0)
            {
                long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
                c.That(!results.Any(row => row.Sequence == sequence && row.Accepted), label + " no accepted command result for invalid commit");
            }
            if (game.Fault == null) return; // Observe baseline failures without stepping corrupt sessions again.
            var first = game.Fault;
            c.Throws<InvalidOperationException>(() => game.Save(), label + " no healthy checkpoint after a bad commit");
            c.Throws<InvalidOperationException>(() => view.ReadHaulTripPolicy(worker, out _), label + " retained view cannot publish a partial policy");
            c.Throws<InvalidOperationException>(() => game.Seat(0).Preview(new ConfigureHaulTripPolicyCommand { Worker = worker, Settings = requested }), label + " faulted admission cannot retry");
            c.That(ReferenceEquals(first, game.Fault), label + " later refusals cannot mask the original error");
        }
        private static void Run(Check c)
        {
            foreach (int fault in new[] { 13, 14, 15, 16, 17, 18 })
            {
                var module = new FleetTripProofModule(); var factory = TripAdmissionObservationTests.Factory(fault, module, fault >= 17 ? "fuel" : null);
                TripAdmissionObservationTests.Physical(c, factory, (sameFactory, loaded, worker) => {
                    byte[] save = loaded.Save();
                    for (int path = 0; path < 4; path++)
                    {
                        var restored = sameFactory.Load(save); Fault(c, restored, module, worker, fault, path, true);
                        var fresh = TripAdmissionObservationTests.New(sameFactory);
                        var actual = TransformationContentContractTests.Entity(fresh, "reserve.truck"); Fault(c, fresh, module, actual, fault, path, false);
                    }
                }, "trip-commit-integrity/" + fault);
            }
        }
    }
}
