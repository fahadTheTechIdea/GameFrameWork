using System;
using System.Collections.Generic;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Normal physical gameplay uses the original shared player helper; direct calls are negative diagnostics only.
    internal static class TripRuleObservationTests
    {
        internal const string Case = "Trip rules: direct JSON contracts observe whole carriers first errors and callback cycles through saved physical trips";
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(int fault, FleetTripProofModule module, out HaulContractModule contracts, int sourcesFault = 0,
            Action<JsonValue> authorRules = null, Action<JsonValue> authorCarrier = null,
            IReadOnlyDictionary<string, string> additionalDocuments = null, IReadOnlyList<GameModule> extraModules = null,
            JsonValue extraDeclarations = null, Action<JsonValue> authorScenario = null, Action<JsonValue> authorPowered = null)
        {
            contracts = new HaulContractModule(new InitializeProbe());
            var rules = Json.Parse(FrameworkProofContent.Read("haul-trip-policy.json"));
            rules.Set("independent_haul_load_profiles", Json.Parse("[{\"id\":\"contract.load\",\"enabled\":true,\"minimum_load\":{\"supply\":4},\"maximum_wait\":{\"numerator\":0,\"denominator\":1,\"unit\":\"minute\"},\"emergency_bypass\":false}]"));
            var trips = Json.Parse("[{\"id\":\"contract.trip\",\"enabled\":true,\"preserve_outlet_reserve\":false,\"service_sources\":[]}]");
            trips[0].Set("rule_fault", JsonValue.Number(fault.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            if (sourcesFault != 0) trips[0].Set("sources_fault", JsonValue.Number(sourcesFault.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            rules.Set("independent_haul_trip_profiles", trips);
            authorRules?.Invoke(rules);
            var modules = new List<GameModule> { contracts };
            var declarations = Json.Parse("[{\"id\":\"proof.haul-contracts\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[]}]");
            if ((extraModules == null) != (extraDeclarations == null)) throw new ArgumentException("extra rule modules and declarations must be authored together");
            if (extraModules != null) modules.AddRange(extraModules);
            if (extraDeclarations != null) for (int i = 0; i < extraDeclarations.Count; i++) declarations.Add(extraDeclarations[i].Clone());
            return TripAdmissionObservationTests.Factory(0, module, authorCarrier: parts => {
                parts.Get("proof_fleet_trip").Set("profile", JsonValue.Of("contract.trip")); authorCarrier?.Invoke(parts);
            }, rules: rules.ToString(), additionalModules: modules, additionalDeclarations: declarations,
                additionalDocuments: additionalDocuments, authorScenario: authorScenario, authorPowered: authorPowered);
        }
        private static Action Query(Check c, SimGame game, FleetTripProofModule module, EntityId worker, int path, bool loaded)
        {
            var view = game.Seat(0).View.Feature<LogisticsView>();
            MotionTravelEstimate travel;
            if (loaded)
            {
                c.Equal(Code.None, view.ReadHaulTripBudget(worker, out var actual), "actual loaded native trip budget is readable before probe");
                if (actual == null) throw new InvalidOperationException("loaded trip budget required"); travel = actual.Travel;
            }
            else
            {
                var preview = view.PreviewHaul(TransformationContentContractTests.Entity(game, "source"),
                    TransformationContentContractTests.Entity(game, "destination"), PermanentRemovalTests.Supply(game), Fix.FromInt(4), worker: worker);
                if (preview.Status != HaulStatus.Ready || !preview.Travel.HasValue) throw new InvalidOperationException("actual funded fresh trip estimate required");
                travel = preview.Travel.Value;
            }
            if (path == 2) return () => module.ProbeTripRuleBudget(worker, travel);
            if (loaded) return path == 0 ? () => view.ReadHaulTripBudget(worker, out _)
                : () => game.Observer.Feature<LogisticsView>().ReadHaulTripBudget(worker, out _);
            var source = TransformationContentContractTests.Entity(game, "source"); var target = TransformationContentContractTests.Entity(game, "destination");
            var supply = PermanentRemovalTests.Supply(game);
            return path == 0 ? () => view.PreviewHaul(source, target, supply, Fix.FromInt(4), worker: worker)
                : () => game.Seat(0).Preview(new HaulResourcesCommand { Worker = worker, Source = source, Destination = target, Resource = supply, Ceiling = Fix.FromInt(4) });
        }
        private static void Observe(Check c, GameFactory factory, SimGame game, FleetTripProofModule module, EntityId worker, int fault, int path, bool loaded)
        {
            string label = "trip-rule=" + fault + "/path=" + path + "/loaded=" + loaded;
            if (loaded) c.Equal(Fix.FromInt(4), LoadPolicyDisclosureTests.Stock(game, worker, PermanentRemovalTests.Supply(game)), label + " actual saved four-unit cargo");
            var query = Query(c, game, module, worker, path, loaded); ulong hash = game.Hash(); long at = game.Clock.StepIndex;
            Exception caught = null; bool published = false;
            try { module.ObserveTripRule(worker, query); published = true; }
            catch (Exception error) { caught = error; }
            if (fault == 6)
            {
                c.That(published && caught == null && game.Fault == null, label + " legitimate rule-to-current-settings read remains allowed");
                c.Equal(hash, game.Hash(), label + " valid cross-operation rule read is pure");
                c.Equal(hash, factory.Load(game.Save()).Hash(), label + " healthy rule result can checkpoint");
                return;
            }
            c.That(!published && caught is InvalidOperationException, label + " invalid rule returns no healthy facts");
            c.That(game.Fault != null && game.State == GameState.Faulted, label + " original session terminates at first fault");
            c.Equal("logistics.trip-rule.check", game.Fault?.Phase, label + " actual trip-rule interval owns first error");
            if (fault == 3 || fault == 4)
            {
                c.Equal("proof.trip-rule-first-error", caught?.Message, label + " original callback throw survives verification");
                c.Equal("InvalidOperationException: proof.trip-rule-first-error", game.Fault?.Message, label + " original cause recorded");
            }
            c.Equal(at, game.Clock.StepIndex, label + " observation advances no game time");
            if (game.Fault == null) return; // Preserve baseline failures without stepping damaged diagnostics again.
            var first = game.Fault;
            c.Throws<InvalidOperationException>(() => game.Save(), label + " no healthy checkpoint after bad rule");
            c.Throws<InvalidOperationException>(() => game.Seat(0).View.Feature<LogisticsView>().ReadHaulTripBudget(worker, out _), label + " retained view rejects faulted session");
            c.That(ReferenceEquals(first, game.Fault), label + " later refusals preserve original cause");
        }
        private static void Run(Check c)
        {
            foreach (int fault in new[] { 6, 1, 2, 3, 4, 5 })
            {
                var module = new FleetTripProofModule(); var factory = Factory(fault, module, out var contracts);
                TripAdmissionObservationTests.Physical(c, factory, (sameFactory, loaded, worker) => {
                    c.That(contracts.IndependentProfiles, "actual frozen direct load/trip definitions have independent identities");
                    byte[] save = loaded.Save();
                    for (int path = 0; path < 3; path++)
                    {
                        Observe(c, sameFactory, sameFactory.Load(save), module, worker, fault, path, true);
                        var fresh = TripAdmissionObservationTests.New(sameFactory);
                        Observe(c, sameFactory, fresh, module, TransformationContentContractTests.Entity(fresh, "reserve.truck"), fault, path, false);
                    }
                }, "trip-rule/" + fault, initialSettings: new HaulTripSettings("contract.trip", true, false));
            }
        }
    }
}
