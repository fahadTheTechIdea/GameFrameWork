using System;
using Beep.SimEngine.Commands;
using Beep.SimEngine.Content;
using Beep.SimEngine.ExtensionProof;
using Beep.SimEngine.Hosting;
using Beep.SimEngine.Kernel;
using Beep.SimEngine.Standard;
using Beep.SimEngine.Views;

namespace Beep.SimEngine.Tests
{
    // Original master-listed JSON, physical owners and shared player-command helper; negative hooks only in the provider.
    internal static class LoadPolicyObservationTests
    {
        internal const string Case = "Load policy observation: original errors whole workers and callback cycles preserve saved physical batching";
        internal static void Register() => TestRunner.Add(Case, Run);
        internal static GameFactory Factory(int fault, FleetTripProofModule module, string mutationResource = null,
            Action<JsonValue> authorRules = null, string loadProfile = "load.batch")
        {
            var rules = Json.Parse(FrameworkProofContent.Read("haul-trip-policy.json"));
            rules.Set("haul_load_profiles", Json.Parse(FrameworkProofContent.Read("haul-load-policies.json")).Get("haul_load_profiles").Clone());
            authorRules?.Invoke(rules);
            return TripAdmissionObservationTests.Factory(0, module, authorCarrier: parts => {
                var policy = Json.Parse("{\"profile\":\"load.batch\",\"fault\":" + fault + "}");
                policy.Set("profile", JsonValue.Of(loadProfile));
                if (mutationResource != null) policy.Set("mutation_resource", JsonValue.Of(mutationResource));
                parts.Set("contract_haul_load_policy", policy);
            }, rules: rules.ToString());
        }
        private static void Fault(Check c, SimGame game, FleetTripProofModule module, EntityId worker, int fault, int path, bool loaded)
        {
            string label = "load=" + fault + "/path=" + path + "/loaded=" + loaded;
            if (loaded) c.Equal(Fix.FromInt(4), PermanentRemovalTests.Facts(game, worker).Stock, label + " starts with actual loaded stock");
            var view = game.Seat(0).View.Feature<LogisticsView>(); var requested = new HaulLoadSettings("load.batch", false,
                new Bill(new[] { new ResourceAmount(PermanentRemovalTests.Supply(game), Fix.FromInt(4)) }), GameDuration.Minutes(100), false);
            long at = game.Clock.StepIndex; module.ArmLoadObservationFault(worker);
            Exception caught = null; bool published = false;
            try
            {
                if (fault <= 7 || fault == 11)
                {
                    if (path == 0) view.ReadHaulLoadPolicy(worker, out _);
                    else if (path == 1) view.PreviewHaulLoadPolicy(worker, requested);
                    else game.Seat(0).Preview(new ConfigureHaulLoadPolicyGroupCommand { Workers = new[] { worker }, Settings = requested });
                }
                else if (path == 0) view.PreviewHaulLoadPolicy(worker, requested);
                else if (path == 1) view.PreviewHaulLoadPolicyGroup(new[] { worker }, requested);
                else game.Seat(0).Preview(new ConfigureHaulLoadPolicyCommand { Worker = worker, Settings = requested });
                published = true;
            }
            catch (Exception error) { caught = error; }
            c.That(!published && caught is InvalidOperationException, label + " no healthy policy/admission result after invalid callback");
            c.That(game.Fault != null && game.State == GameState.Faulted, label + " original session latches callback failure");
            if (fault == 5 || fault == 7 || fault == 8 || fault == 10)
            {
                string original = fault <= 7 ? "proof.load-read-first-error" : "proof.load-check-first-error";
                c.Equal(original, caught?.Message, label + " original callback exception is not masked by finally");
                c.Equal("InvalidOperationException: " + original, game.Fault?.Message, label + " first-fault record retains original cause");
            }
            if (fault == 11 || fault == 12)
                c.Equal("logistics.load-policy." + (fault == 11 ? "read" : "check"), game.Fault?.Phase, label + " repeated actual provider operation owns cycle refusal");
            if (game.Fault == null) return; // Collect bounded baseline failures without advancing corrupt diagnostics.
            var first = game.Fault;
            c.Throws<InvalidOperationException>(() => game.Save(), label + " no healthy checkpoint after callback failure");
            c.Throws<InvalidOperationException>(() => view.ReadHaulLoadPolicy(worker, out _), label + " retained management view rejects faulted state");
            c.That(ReferenceEquals(first, game.Fault), label + " subsequent refusal preserves first cause");
            c.Equal(at, game.Clock.StepIndex, label + " observation advances no game time");
        }
        private static void Run(Check c)
        {
            var validFactory = Factory(13, new FleetTripProofModule());
            var valid = TripAdmissionObservationTests.New(validFactory);
            var selected = TransformationContentContractTests.Entity(valid, "reserve.truck");
            var limits = new ReplayLimits(4 * 1024 * 1024, 2000); using (var recording = valid.RecordReplay(limits))
            {
                // A real load-policy command must invoke CheckLoadPolicy; hauling alone reads
                // the current policy/rule but does not prove this configuration callback runs.
                var settings = new HaulLoadSettings("load.batch", true,
                    new Bill(new[] { new ResourceAmount(PermanentRemovalTests.Supply(valid), Fix.FromInt(4)) }), GameDuration.Minutes(50), false);
                PermanentRemovalTests.Submit(c, valid, new ConfigureHaulLoadPolicyGroupCommand { Workers = new[] { selected }, Settings = settings });
                c.Equal(Code.None, valid.Seat(0).View.Feature<LogisticsView>().ReadHaulLoadPolicy(selected, out var actual), "valid check-to-current-read commits normal load settings");
                c.Equal(GameDuration.Minutes(50), actual.Settings.MaximumWait, "authored exact player wait edit persists through actual owner");
                c.Equal(valid.Hash(), validFactory.Load(valid.Save()).Hash(), "valid load admission and settings survive save/load");
                var replay = new ReplayVerifier().Verify(validFactory, recording.Finish(), limits);
                c.That(replay.Success && replay.ActualHash == valid.Hash(), "actual valid nested load admission command replays");
                Console.WriteLine("load-observation/current-read/full/" + recording.InitialStep + "->" + valid.Clock.StepIndex + "/inputs=" + recording.CommandCount
                    + "/checkpoints=" + recording.CheckpointCount + "/hash=" + valid.Hash().ToString("X16"));
            }
            TripAdmissionObservationTests.Physical(c, validFactory, evidence: "load-observation/valid-check-read");
            foreach (int fault in new[] { 5, 6, 7, 8, 9, 10, 11, 12 })
            {
                var module = new FleetTripProofModule(); var factory = Factory(fault, module);
                TripAdmissionObservationTests.Physical(c, factory, (sameFactory, loaded, worker) => {
                    byte[] save = loaded.Save();
                    for (int path = 0; path < 3; path++)
                    {
                        var restored = sameFactory.Load(save); Fault(c, restored, module, worker, fault, path, true);
                        var fresh = TripAdmissionObservationTests.New(sameFactory);
                        Fault(c, fresh, module, TransformationContentContractTests.Entity(fresh, "reserve.truck"), fault, path, false);
                    }
                }, "load-observation/" + fault);
            }
        }
    }
}
