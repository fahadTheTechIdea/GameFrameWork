using System;
using System.Collections.Generic;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Original player helper runs physical work; only scoped negative provider instrumentation writes state.
    internal static class TripSourceObservationTests
    {
        internal const string Case = "Trip service metadata: one guarded source read protects direct admission views and saved physical carriers";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static void Observe(Check c, GameFactory factory, SimGame game, FleetTripProofModule module, EntityId worker, int fault, int path, bool loaded)
        {
            string label = "trip-sources=" + fault + "/path=" + path + "/loaded=" + loaded;
            var view = game.Seat(0).View.Feature<LogisticsView>();
            c.Equal(Code.None, view.ReadHaulTripPolicy(worker, out var settings), label + " actual current settings before scoped metadata probe");
            if (loaded) c.Equal(Fix.FromInt(4), LoadPolicyDisclosureTests.Stock(game, worker, PermanentRemovalTests.Supply(game)), label + " actual loaded saved carrier");
            IReadOnlyList<Code> sources = null; ServiceMovementWindow window = null; Code result = Codes.NotFound;
            Action query = path == 0 ? () => result = module.ProbeTripSourceCheck(worker, settings) : path == 1
                ? () => result = view.ReadHaulTripServiceSources(worker, out sources)
                : () => result = view.ReadHaulTripServiceWindow(worker, out window);
            ulong before = game.Hash(); long at = game.Clock.StepIndex;
            Exception caught = null; bool published = false; int reads = 0;
            try { reads = module.ObserveTripSources(worker, query); published = true; }
            catch (Exception error) { caught = error; }
            if (fault == 6)
            {
                c.That(published && caught == null && game.Fault == null && result.IsNone, label + " actual pure getter remains usable");
                c.Equal(path + 1, reads, label + " each settings/list/window consumer reads actual metadata once");
                if (path == 1)
                {
                    c.That(sources != null && sources.Count == 0, label + " actual explicit empty source list, not fallback data");
                    c.Throws<NotSupportedException>(() => ((IList<Code>)sources).Add(new Code("fake")), label + " published metadata detached and immutable");
                }
                if (path == 2) c.That(window != null && window.Unrestricted, label + " actual empty source set has unrestricted native window");
                c.Equal(before, game.Hash(), label + " pure source reads change no original state");
                c.Equal(before, factory.Load(game.Save()).Hash(), label + " pure metadata read can checkpoint");
                return;
            }
            c.That(!published && caught is InvalidOperationException, label + " bad getter publishes no healthy result");
            c.That(game.Fault != null && game.State == GameState.Faulted, label + " original session faults at invalid metadata callback");
            string phase = fault == 5 && path != 0 ? "logistics.trip-policy.read" : "logistics.trip-sources.read";
            c.Equal(phase, game.Fault?.Phase, label + " earliest repeated operation or source owner keeps first cause");
            if (fault == 3 || fault == 4)
            {
                c.Equal("proof.trip-sources-first-error", caught?.Message, label + " first callback error is not replaced by verification");
                c.Equal("InvalidOperationException: proof.trip-sources-first-error", game.Fault?.Message, label + " original throw is recorded");
            }
            c.That(sources == null && window == null, label + " management returns no metadata/window prefix");
            c.Equal(at, game.Clock.StepIndex, label + " source observation advances no time");
            if (game.Fault == null) return; // Inspect baseline failures without stepping damaged diagnostic sessions.
            var first = game.Fault;
            c.Throws<InvalidOperationException>(() => game.Save(), label + " no healthy save after metadata violation");
            c.Throws<InvalidOperationException>(() => view.ReadHaulTripServiceSources(worker, out _), label + " retained view rejects faulted source query");
            c.That(ReferenceEquals(first, game.Fault), label + " later refusal cannot replace first cause");
        }
        private static void Run(Check c)
        {
            foreach (int fault in new[] { 6, 1, 2, 3, 4, 5 })
            {
                var module = new FleetTripProofModule(); var factory = TripRuleObservationTests.Factory(0, module, out _, sourcesFault: fault);
                TripAdmissionObservationTests.Physical(c, factory, (sameFactory, loaded, worker) => {
                    byte[] save = loaded.Save();
                    for (int path = 0; path < 3; path++)
                    {
                        Observe(c, sameFactory, sameFactory.Load(save), module, worker, fault, path, true);
                        var fresh = TripAdmissionObservationTests.New(sameFactory);
                        Observe(c, sameFactory, fresh, module, TransformationContentContractTests.Entity(fresh, "reserve.truck"), fault, path, false);
                    }
                }, "trip-sources/" + fault, initialSettings: new HaulTripSettings("contract.trip", true, false));
            }
        }
    }
}
