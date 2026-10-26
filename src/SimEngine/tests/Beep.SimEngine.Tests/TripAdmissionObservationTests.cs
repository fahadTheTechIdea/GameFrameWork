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
    // Player commands/views drive gameplay; only the engine-side provider injects negative faults.
    internal static class TripAdmissionObservationTests
    {
        internal const string Case = "Trip admission observation: original callback errors and whole worker state protect saved physical hauling";
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(int fault, FleetTripProofModule module = null, string mutationResource = null,
            Action<JsonValue> authorCarrier = null, string rules = null,
            IReadOnlyList<GameModule> additionalModules = null, JsonValue additionalDeclarations = null,
            IReadOnlyDictionary<string, string> additionalDocuments = null, Action<JsonValue> authorScenario = null,
            Action<JsonValue> authorPowered = null)
        {
            if ((additionalModules == null) != (additionalDeclarations == null)) throw new ArgumentException("extra modules and declarations must be authored together");
            var modules = new List<GameModule> { new MachineFleetsModule(), module ?? new FleetTripProofModule() };
            var declarations = Json.Parse("[{\"id\":\"sim.machine-fleets\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.motion\",\"version\":\"1.0.0\"},{\"id\":\"sim.work\",\"version\":\"1.0.0\"}],\"documents\":[]},{\"id\":\"proof.fleet-trip\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.machine-fleets\",\"version\":\"1.0.0\"},{\"id\":\"sim.logistics\",\"version\":\"1.0.0\"}],\"documents\":[]}]");
            if (additionalModules != null) modules.AddRange(additionalModules);
            if (additionalDeclarations != null) for (int i = 0; i < additionalDeclarations.Count; i++) declarations.Add(additionalDeclarations[i].Clone());
            return HaulTripPolicyTests.Factory(edit: document => {
                var parts = document.Get("entities")[1].Get("components"); parts.Remove("haul_trip_policy");
                parts.Set("proof_fleet_trip", Json.Parse("{\"profile\":\"trip.funded\",\"refuse_profile\":\"trip.no-floor\",\"fault\":" + fault + "}"));
                if (mutationResource != null) parts.Get("proof_fleet_trip").Set("mutation_resource", JsonValue.Of(mutationResource));
                authorCarrier?.Invoke(parts);
                authorPowered?.Invoke(document);
            }, editPointScenario: scenario => {
                scenario.Get("modules").Add(JsonValue.Of(MachineFleetsModule.ModuleId));
                scenario.Get("modules").Add(JsonValue.Of("proof.fleet-trip"));
                if (additionalModules != null) foreach (var extra in additionalModules) scenario.Get("modules").Add(JsonValue.Of(extra.Id));
                authorScenario?.Invoke(scenario);
            }, rules: rules, pointModules: modules, pointDeclarations: declarations.ToString(), additionalDocuments: additionalDocuments);
        }
        internal static SimGame New(GameFactory factory)
            => factory.NewGame(new NewGameRequest { Scenario = "points", Seed = 11723, Seats = { Controller.Human, Controller.Human } });
        private static LogisticsView View(SimGame game) => game.Seat(0).View.Feature<LogisticsView>();
        private static Fix Stock(SimGame game, EntityId entity, ResourceId resource)
        {
            var refusal = View(game).ReadDiagnostics(entity, resource, out var facts);
            if (!refusal.IsNone) throw new InvalidOperationException("trip observation stock/" + refusal); return facts.Stock;
        }
        private static Command Command(EntityId worker, HaulTripSettings settings, bool group)
            => group ? new ConfigureHaulTripPolicyGroupCommand { Workers = new[] { worker }, Settings = settings }
                : new ConfigureHaulTripPolicyCommand { Worker = worker, Settings = settings };
        private static void Fault(Check c, GameFactory factory, int fault, int path, bool restored)
        {
            var game = New(factory); if (restored) game = factory.Load(game.Save());
            var worker = TransformationContentContractTests.Entity(game, "reserve.truck");
            var settings = new HaulTripSettings("trip.no-floor", false, false); long at = game.Clock.StepIndex;
            Exception caught = null; bool published = false;
            try
            {
                if (path == 0) { View(game).PreviewHaulTripPolicy(worker, settings); published = true; }
                else if (path == 1) { View(game).PreviewHaulTripPolicyGroup(new[] { worker }, settings); published = true; }
                else if (path == 2 || path == 3) { game.Seat(0).Preview(Command(worker, settings, path == 3)); published = true; }
                else { game.Seat(0).Submit(Command(worker, settings, path == 5)); game.Update(); }
            }
            catch (Exception error) { caught = error; }
            string label = "fault=" + fault + "/path=" + path + "/restored=" + restored;
            c.That(!published, label + " never publishes admission facts after a provider failure");
            c.That(game.Fault != null && game.State == GameState.Faulted, label + " latches the original terminal session fault");
            if (fault != 6)
            {
                c.That(path >= 4 || caught?.Message == "proof.trip-admission-first-error", label + " preserves the thrown callback error, not verification in finally");
                c.That(game.Fault?.Message == "InvalidOperationException: proof.trip-admission-first-error", label + " first-fault record preserves the callback cause");
            }
            else c.That(path >= 4 || caught is InvalidOperationException, label + " silent sibling worker mutation is rejected");
            if (game.Fault == null) return; // Keep collecting the reproduced failures, never step corrupted baseline state.
            var first = game.Fault;
            c.Throws<InvalidOperationException>(() => game.Save(), label + " no healthy checkpoint after admission failure");
            c.Throws<InvalidOperationException>(() => View(game).ReadHaulTripPolicy(worker, out _), label + " retained management view cannot publish after failure");
            c.Throws<InvalidOperationException>(() => game.Seat(0).Preview(Command(worker, settings, false)), label + " later command preview cannot retry a faulted session");
            c.That(ReferenceEquals(first, game.Fault), label + " later refusals never replace the first failure");
            if (path < 4) c.Equal(at, game.Clock.StepIndex, label + " observation does not advance the sole clock");
            long cursor = 0; var results = new List<CommandResult>(); game.Seat(0).ReadResults(ref cursor, results);
            c.That(!results.Any(result => result.Accepted), label + " no successful command result prefix");
        }
        internal static SimGame Physical(Check c, GameFactory factory = null,
            Action<GameFactory, SimGame, EntityId> onLoaded = null, string evidence = "trip-admission-observation",
            HaulTripSettings initialSettings = null)
        {
            factory ??= Factory(0); var game = New(factory); var idle = New(factory); ulong isolated = idle.Hash();
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "independent physical trip policy requires no business module");
            var worker = TransformationContentContractTests.Entity(game, "reserve.truck");
            var source = TransformationContentContractTests.Entity(game, "source");
            var target = TransformationContentContractTests.Entity(game, "destination"); var supply = PermanentRemovalTests.Supply(game);
            var limits = new ReplayLimits(4 * 1024 * 1024, 2000); using var recording = game.RecordReplay(limits);
            var requested = initialSettings ?? new HaulTripSettings("trip.funded", true, false);
            PermanentRemovalTests.Submit(c, game, Command(worker, requested, false));
            ulong before = game.Hash(); var veto = new HaulTripSettings("trip.no-floor", true, false);
            c.Equal(new Code("proof.trip-refused"), View(game).PreviewHaulTripPolicy(worker, veto), "pure independent provider veto remains an ordinary refusal");
            c.Equal(before, game.Hash(), "ordinary veto is pure and does not fault"); c.That(game.Fault == null, "valid provider remains healthy");
            PermanentRemovalTests.Submit(c, game, new HaulResourcesCommand {
                Worker = worker, Source = source, Destination = target, Resource = supply, Ceiling = Fix.FromInt(4), Priority = 100 });
            var timer = Stopwatch.StartNew(); long began = game.Clock.StepIndex;
            void Wait(Func<bool> ready, string phase, SimGame twin = null)
            {
                while (!ready() && game.Clock.StepIndex - began < 128 && timer.Elapsed.TotalSeconds < 25)
                {
                    PermanentRemovalTests.Step(game, twin);
                    c.Equal(Fix.FromInt(20), Stock(game, source, supply) + Stock(game, target, supply) + Stock(game, worker, supply), "finite supply conserved during " + phase);
                }
                if (!ready()) throw new InvalidOperationException("trip admission physical/128updates25seconds/" + phase + "/" + game.Clock.StepIndex);
            }
            Wait(() => Stock(game, worker, supply) == Fix.FromInt(4), "real loading");
            var twin = factory.Load(game.Save()); c.Equal(game.Hash(), twin.Hash(), "actual loaded independent worker checkpoint");
            onLoaded?.Invoke(factory, game, worker);
            Wait(() => Stock(game, target, supply) == Fix.FromInt(4) && !PermanentRemovalTests.Jobs(game).Any(job => job.IsHaul && job.Worker == worker), "saved delivery", twin);
            c.That(Stock(game, source, supply) == Fix.FromInt(16) && Stock(game, worker, supply) == Fix.Zero, "physical supply delivered once without instant fallback");
            c.Equal(game.Hash(), twin.Hash(), "saved independent worker continuation agrees");
            c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed stock/claims/policy restore exactly");
            c.Equal(game.Clock.StepIndex - recording.InitialStep, (long)recording.CheckpointCount, "recording includes every advancing update");
            var verified = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
            c.That(verified.Success && verified.ActualHash == game.Hash(), "complete independent physical trip replay");
            Console.WriteLine(evidence + "/full/" + recording.InitialStep + "->" + game.Clock.StepIndex + "/inputs=" + recording.CommandCount
                + "/checkpoints=" + recording.CheckpointCount + "/hash=" + game.Hash().ToString("X16"));
            c.Equal(isolated, idle.Hash(), "interleaved physical and loaded diagnostic sessions preserve an idle session");
            return idle;
        }
        private static void Run(Check c)
        {
            var idle = Physical(c); ulong isolated = idle.Hash();
            foreach (int fault in new[] { 3, 4, 5, 6, 7 })
            {
                var broken = Factory(fault);
                for (int path = 0; path < 6; path++) { Fault(c, broken, fault, path, false); Fault(c, broken, fault, path, true); }
            }
            c.Equal(isolated, idle.Hash(), "interleaved faults and physical work never affect an idle session");
        }
    }
}
