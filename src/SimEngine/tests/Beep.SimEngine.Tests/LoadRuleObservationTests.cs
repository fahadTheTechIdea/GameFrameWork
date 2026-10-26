using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Player inputs execute gameplay. Scoped provider hooks are explicit negative framework diagnostics only.
    internal static class LoadRuleObservationTests
    {
        internal const string Case = "Load rules: direct content contracts preserve whole workers first errors cycles and physical odd batching";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory(int fault, FleetTripProofModule module)
            => LoadPolicyObservationTests.Factory(0, module, authorRules: rules => {
                var profiles = Json.Parse(FrameworkProofContent.Read("haul-load-policies.json")).Get("contract_haul_load_profiles").Clone();
                profiles[0].Set("rule_fault", JsonValue.Number(fault.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                rules.Set("contract_haul_load_profiles", profiles);
            }, loadProfile: "load.odd");
        private static LogisticsView View(SimGame game) => game.Seat(0).View.Feature<LogisticsView>();
        private static int Route(Check c, SimGame game, EntityId source, EntityId target, ResourceId supply)
        {
            PermanentRemovalTests.Submit(c, game, new AddRouteCommand { From = source, To = target, Resource = supply,
                Settings = new LogisticsRouteSettings(true, Fix.FromInt(2), 1) });
            var rows = new List<RouteView>(); View(game).CopyRoutes(rows); return rows.Single().Id;
        }
        private static void Fault(Check c, GameFactory factory, FleetTripProofModule module, int fault, int path, bool restored)
        {
            var game = TripAdmissionObservationTests.New(factory);
            var worker = TransformationContentContractTests.Entity(game, "reserve.truck");
            var source = TransformationContentContractTests.Entity(game, "source"); var target = TransformationContentContractTests.Entity(game, "destination");
            var supply = PermanentRemovalTests.Supply(game); Route(c, game, source, target, supply);
            if (restored) game = factory.Load(game.Save());
            string label = "rule=" + fault + "/path=" + path + "/restored=" + restored;
            var view = View(game); var rows = new List<LogisticsDispatchOffer> { default };
            Action query = path == 1 ? () => view.ReadDiagnostics(target, supply, out _) : path == 0
                ? () => view.CopyQueue(rows) : () => game.Observer.Feature<LogisticsView>().CopyQueue(rows);
            Exception caught = null; bool published = false; long at = game.Clock.StepIndex;
            try { module.ObserveLoadRule(worker, query); published = true; }
            catch (Exception error) { caught = error; }
            c.That(!published && caught is InvalidOperationException, label + " rule returns no healthy observation after invalid callback");
            c.That(game.Fault != null && game.State == GameState.Faulted, label + " original session owns terminal rule fault");
            c.Equal("logistics.load-rule.check", game.Fault?.Phase, label + " actual load-rule boundary owns first error");
            if (fault == 3 || fault == 4)
            {
                c.Equal("proof.load-rule-first-error", caught?.Message, label + " original throw is not masked by verification");
                c.Equal("InvalidOperationException: proof.load-rule-first-error", game.Fault?.Message, label + " first-fault cause retained");
            }
            if (path != 1) c.Equal(0, rows.Count, label + " caller queue has no partial facts");
            c.Equal(at, game.Clock.StepIndex, label + " pure observation never advances game time");
            if (game.Fault == null) return; // Record bounded baseline failures without advancing damaged diagnostics.
            var first = game.Fault;
            c.Throws<InvalidOperationException>(() => game.Save(), label + " invalid rule cannot create healthy save");
            c.Throws<InvalidOperationException>(() => view.ReadDiagnostics(target, supply, out _), label + " retained diagnostics reject faulted session");
            c.That(ReferenceEquals(first, game.Fault), label + " later refusal preserves original cause");
        }
        private static void Run(Check c)
        {
            var module = new FleetTripProofModule(); var factory = Factory(6, module);
            var idle = TripAdmissionObservationTests.New(factory); ulong isolated = idle.Hash();
            var game = TripAdmissionObservationTests.New(factory); var source = TransformationContentContractTests.Entity(game, "source");
            var target = TransformationContentContractTests.Entity(game, "destination"); var worker = TransformationContentContractTests.Entity(game, "reserve.truck");
            var supply = PermanentRemovalTests.Supply(game); var limits = new ReplayLimits(4 * 1024 * 1024, 2000);
            using (var recording = game.RecordReplay(limits))
            {
                int route = Route(c, game, source, target, supply); ulong before = game.Hash();
                var offers = new List<LogisticsDispatchOffer>(); module.ObserveLoadRule(worker, () => View(game).CopyQueue(offers));
                c.That(offers.Count == 1 && offers[0].Haul.Status == HaulStatus.PolicyBlocked, "valid independent rule can read current settings and refuse an even short batch");
                c.Equal(before, game.Hash(), "legitimate cross-operation read is pure");
                PermanentRemovalTests.Submit(c, game, new ConfigureRouteCommand { Route = route, Settings = new LogisticsRouteSettings(true, Fix.FromInt(3), 1) });
                var twin = factory.Load(game.Save());
                // The existing dispatch schedule need not admit on the command's own update.
                int admission = 0; var admissionWatch = Stopwatch.StartNew();
                while (PermanentRemovalTests.Jobs(game).Count == 0 && admission < 32 && admissionWatch.ElapsedMilliseconds < 12000)
                { PermanentRemovalTests.Step(game, twin); admission++; }
                c.That(PermanentRemovalTests.Jobs(game).Count > 0 && admission < 32 && admissionWatch.ElapsedMilliseconds < 12000,
                    "ordinary scheduled matching admits the custom odd batch before player disables future trips");
                game.Seat(0).Submit(new ConfigureRouteCommand { Route = route, Settings = new LogisticsRouteSettings(false, Fix.FromInt(3), 1) });
                twin.Seat(0).Submit(new ConfigureRouteCommand { Route = route, Settings = new LogisticsRouteSettings(false, Fix.FromInt(3), 1) });
                PermanentRemovalTests.Step(game, twin);
                bool loaded = false; int updates = 0; var watch = Stopwatch.StartNew();
                while (updates < 128 && watch.ElapsedMilliseconds < 25000)
                {
                    Fix cargo = LoadPolicyDisclosureTests.Stock(game, worker, supply), delivered = LoadPolicyDisclosureTests.Stock(game, target, supply);
                    loaded |= cargo == Fix.FromInt(3);
                    c.Equal(Fix.FromInt(20), LoadPolicyDisclosureTests.Stock(game, source, supply) + delivered + cargo, "original odd batching conserves finite supply at every update");
                    if (delivered == Fix.FromInt(3) && cargo.IsZero && PermanentRemovalTests.Jobs(game).Count == 0) break;
                    PermanentRemovalTests.Step(game, twin); updates++;
                }
                c.That(loaded && updates < 128 && watch.ElapsedMilliseconds < 25000, "original dispatcher loads moves and delivers one real custom odd batch");
                c.Equal(Fix.FromInt(17), LoadPolicyDisclosureTests.Stock(game, source, supply), "exact three units leave original source");
                c.Equal(Fix.FromInt(3), LoadPolicyDisclosureTests.Stock(game, target, supply), "exact three units arrive over game time");
                c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "direct-rule settings and physical result restore");
                c.Equal(isolated, idle.Hash(), "scoped probe leaves other session untouched");
                var replay = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
                c.That(replay.Success && replay.ActualHash == game.Hash(), "actual custom rule commands and physical continuation replay");
                Console.WriteLine("load-rule/valid/full/0->" + game.Clock.StepIndex + "/inputs=" + recording.CommandCount + "/checkpoints=" + recording.CheckpointCount + "/hash=" + game.Hash().ToString("X16"));
            }
            foreach (int fault in new[] { 1, 2, 3, 4, 5 })
            {
                var faults = new FleetTripProofModule(); var sameFactory = Factory(fault, faults);
                for (int path = 0; path < 3; path++)
                    foreach (bool restored in new[] { false, true }) Fault(c, sameFactory, faults, fault, path, restored);
            }
        }
    }
}
