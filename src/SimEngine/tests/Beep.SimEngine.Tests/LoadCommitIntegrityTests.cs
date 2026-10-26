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
    // Positive gameplay uses player inputs; direct provider calls are negative contract diagnostics only.
    internal static class LoadCommitIntegrityTests
    {
        internal const string Case = "Load commit integrity: settings-only changes preserve physical cargo clock siblings and callback boundaries";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static void Fault(Check c, SimGame game, FleetTripProofModule module, EntityId worker, int fault, int path, bool loaded)
        {
            string label = "load-commit=" + fault + "/path=" + path + "/loaded=" + loaded;
            var view = game.Seat(0).View.Feature<LogisticsView>();
            c.Equal(Code.None, view.ReadHaulLoadPolicy(worker, out var current), label + " actual admitted policy");
            var requested = new HaulLoadSettings(current.Settings.Profile, false, current.Settings.MinimumLoad,
                current.Settings.MaximumWait, current.Settings.EmergencyBypass);
            if (loaded) c.Equal(Fix.FromInt(4), PermanentRemovalTests.Facts(game, worker).Stock, label + " actual loaded cargo");
            ulong before = game.Hash(); module.ArmLoadObservationFault(worker);
            c.Equal(Code.None, view.PreviewHaulLoadPolicy(worker, requested), label + " pure current admission permits edit");
            module.ProbeLoadConfigure(worker, current.Settings, path == 1 || path == 3);
            c.Equal(before, game.Hash(), label + " exact no-op does not invoke bad commit");
            Exception caught = null; bool committed = false; int sequence = 0;
            try
            {
                if (path < 2) { module.ProbeLoadConfigure(worker, requested, path == 1); committed = true; }
                else
                {
                    Command command = path == 2 ? new ConfigureHaulLoadPolicyCommand { Worker = worker, Settings = requested }
                        : new ConfigureHaulLoadPolicyGroupCommand { Workers = new[] { worker }, Settings = requested };
                    sequence = game.Seat(0).Submit(command); game.Update();
                }
            }
            catch (Exception error) { caught = error; }
            c.That(!committed, label + " invalid direct commit never returns success");
            c.That(game.Fault != null && game.State == GameState.Faulted, label + " first fault terminates original session");
            if (fault == 17 || fault == 19)
            {
                c.That(path >= 2 || caught?.Message == "proof.load-commit-first-error", label + " original callback exception survives");
                c.Equal("InvalidOperationException: proof.load-commit-first-error", game.Fault?.Message, label + " first error not replaced by verification");
            }
            else c.That(path >= 2 || caught is InvalidOperationException, label + " mutation or recursion cannot publish success");
            c.Equal(fault == 20 ? "logistics.load-policy.commit" : "logistics.configure-load", game.Fault?.Phase,
                label + " original committing boundary owns first fault");
            if (sequence != 0)
            {
                long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
                c.That(!results.Any(row => row.Sequence == sequence && row.Accepted), label + " invalid command has no accepted result");
            }
            if (game.Fault == null) return; // Collect baseline failures without advancing damaged diagnostics again.
            var first = game.Fault;
            c.Throws<InvalidOperationException>(() => game.Save(), label + " no healthy save after failed commit");
            c.Throws<InvalidOperationException>(() => view.ReadHaulLoadPolicy(worker, out _), label + " retained view publishes no partial policy");
            c.Throws<InvalidOperationException>(() => game.Seat(0).Preview(new ConfigureHaulLoadPolicyCommand { Worker = worker, Settings = requested }), label + " no retry on faulted session");
            c.That(ReferenceEquals(first, game.Fault), label + " later refusals preserve first cause");
        }
        private static void Run(Check c)
        {
            foreach (int fault in new[] { 14, 15, 16, 17, 18, 19, 20 })
            {
                var module = new FleetTripProofModule();
                var factory = LoadPolicyObservationTests.Factory(fault, module, fault == 18 || fault == 19 ? "fuel" : null);
                TripAdmissionObservationTests.Physical(c, factory, (sameFactory, loaded, worker) => {
                    byte[] save = loaded.Save();
                    for (int path = 0; path < 4; path++)
                    {
                        Fault(c, sameFactory.Load(save), module, worker, fault, path, true);
                        var fresh = TripAdmissionObservationTests.New(sameFactory);
                        Fault(c, fresh, module, TransformationContentContractTests.Entity(fresh, "reserve.truck"), fault, path, false);
                    }
                }, "load-commit-integrity/" + fault);
            }
        }
    }
}
