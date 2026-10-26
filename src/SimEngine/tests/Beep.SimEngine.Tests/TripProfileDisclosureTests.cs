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
    // Original JSON/commands/policies only; no driver-side policy state, atomicity or physical execution.
    internal static class TripProfileDisclosureTests
    {
        internal const string Case = "Trip profile disclosure: private management and grouped replacement preserve physical saved hauling";
        internal static void Register() => TestRunner.Add(Case, Run);
        private static GameFactory Factory()
            => HaulTripPolicyTests.Factory(edit: document => {
                document.Get("entities")[2].Get("components").Set("haul_trip_policy", Json.Parse("{\"profile\":\"trip.no-floor\"}"));
                document.Get("entity_setups")[1].Get("after").Add(JsonValue.Of("reserve.courier"));
            }, editPointScenario: scenario => {
                scenario.Get("modules").Add(JsonValue.Of(InventorySightModule.ModuleId));
                scenario.Get("settings").Set(InventorySightModule.ModuleId,
                    Json.Parse("{\"target_kind\":\"trip-profile\",\"target\":\"trip.funded\",\"initial_hidden\":false}"));
            }, pointModules: new GameModule[] { new InventorySightModule() },
                pointDeclarations: "[{\"id\":\"proof.inventory-sight\",\"version\":\"1.0.0\",\"requires\":[{\"id\":\"sim.inventory\",\"version\":\"1.0.0\"}],\"documents\":[]}]");
        private static LogisticsView View(SimGame game) => game.Seat(0).View.Feature<LogisticsView>();
        private static HaulTripSettings Actual(SimGame game, EntityId entity)
        {
            var refusal = game.Observer.Feature<LogisticsView>().ReadHaulTripPolicy(entity, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("actual trip policy/" + refusal); return value;
        }
        private static Fix Stock(SimGame game, EntityId entity, ResourceId resource)
        {
            var refusal = game.Observer.Feature<LogisticsView>().ReadDiagnostics(entity, resource, out var value);
            if (!refusal.IsNone) throw new InvalidOperationException("actual trip-policy stock/" + refusal); return value.Stock;
        }
        private static ConfigureHaulTripPolicyGroupCommand Group(EntityId[] entities, HaulTripSettings settings)
            => new ConfigureHaulTripPolicyGroupCommand { Workers = entities, Settings = settings };
        private static void Hidden(Check c, SimGame game, EntityId worker, EntityId visible, EntityId source, EntityId target, ResourceId supply)
        {
            ulong hash = game.Hash(); long at = game.Clock.StepIndex; var view = View(game);
            c.That(game.Seat(0).View.TryGetEntity(worker, out _) && game.Seat(0).View.TryFindResource("supply", out _) && game.Seat(0).View.TryFindResource("fuel", out _),
                "only actual trip profile metadata is hidden, not workers, cargo or fuel");
            c.Equal(HaulStatus.NotFound, view.PreviewHaul(source, target, supply, Fix.FromInt(4), worker: worker).Status,
                "existing budget closure already respects the hidden selected profile");
            c.Equal(Codes.NotFound, view.ReadHaulTripPolicy(worker, out var policy), "management does not disclose a hidden selected profile");
            c.That(policy == null, "no hidden policy payload");
            c.Equal(Codes.NotFound, view.ReadHaulTripServiceSources(worker, out var sources), "hidden profile does not disclose even an empty service-source declaration");
            c.That(sources == null, "no substituted service source list");
            c.Equal(Codes.NotFound, view.ReadHaulTripServiceWindow(worker, out var window), "hidden profile cannot disclose a derived service window");
            c.That(window == null, "no hidden unrestricted-window claim");
            c.Equal(Code.None, view.ReadHaulTripPolicy(visible, out var known), "unrelated known policy remains observable");
            c.That(known.Profile == "trip.no-floor" && known.Enabled, "visible member keeps actual unchanged settings");
            var request = new HaulTripSettings("trip.funded", false, true); var ids = new[] { worker, visible };
            foreach (var id in ids)
            {
                c.Equal(Codes.NotFound, view.PreviewHaulTripPolicy(id, request), "private requested profile refuses typed configuration");
                c.Equal(Codes.NotFound, game.Seat(0).Preview(new ConfigureHaulTripPolicyCommand { Worker = id, Settings = request }), "ordinary single preview agrees");
            }
            var denied = view.PreviewHaulTripPolicyGroup(ids, request);
            c.Equal(Codes.NotFound, denied.Blocker, "whole grouped request refuses a hidden target profile");
            c.That(denied.Entries.All(entry => entry.Settings == null), "private-request preview publishes no current-setting prefix");
            c.Equal(Codes.NotFound, game.Seat(0).Preview(Group(ids, request)), "ordinary grouped preview agrees");
            var replacement = new HaulTripSettings("trip.no-floor", true, false);
            var allowed = view.PreviewHaulTripPolicyGroup(ids, replacement);
            c.Equal(Code.None, allowed.Blocker, "owned known replacement does not require disclosure of old private intent");
            c.That(allowed.Entries[0].Entity == visible && allowed.Entries[0].Settings?.Profile == "trip.no-floor"
                && allowed.Entries[1].Entity == worker && allowed.Entries[1].Settings == null, "stable visible prefix is real; hidden current profile is withheld even on allowed replacement");
            c.Equal(Code.None, game.Seat(0).Preview(Group(ids, replacement)), "known replacement preview remains usable");
            c.Equal(Codes.NotFound, game.Seat(1).View.Feature<LogisticsView>().ReadHaulTripPolicy(worker, out _), "foreign profile remains private");
            c.Equal(Code.None, game.Seat(0).View.Feature<WorkView>().ReadHome(worker, out _), "unrelated own worker facts are not blanket-denied");
            c.Equal(hash, game.Hash(), "all management/service/group previews are pure"); c.Equal(at, game.Clock.StepIndex, "policy disclosure never advances game time");
        }
        private static void Run(Check c)
        {
            var factory = Factory(); var request = new NewGameRequest { Scenario = "points", Seed = 11623, Seats = { Controller.Human, Controller.Human } };
            var game = factory.NewGame(request); var idle = factory.NewGame(request); ulong isolated = idle.Hash();
            c.That(!game.Modules.Contains(TycoonModule.ModuleId), "policy replacement and physical hauling require no business module");
            EntityId worker = TransformationContentContractTests.Entity(game, "reserve.truck"), visible = TransformationContentContractTests.Entity(game, "reserve.courier"),
                source = TransformationContentContractTests.Entity(game, "source"), target = TransformationContentContractTests.Entity(game, "destination");
            c.That(visible.Value < worker.Value, "authored startup dependency puts the visible member before the private member");
            var supply = PermanentRemovalTests.Supply(game); var oldWorker = Actual(game, worker); var oldVisible = Actual(game, visible);
            var limits = new ReplayLimits(4 * 1024 * 1024, 2000); using var recording = game.RecordReplay(limits);
            PermanentRemovalTests.Submit(c, game, new SetInventorySightCommand { Hidden = true });
            Hidden(c, game, worker, visible, source, target, supply);
            var denied = factory.Load(game.Save()); var deniedTwin = factory.Load(game.Save()); using var deniedRecording = denied.RecordReplay(limits);
            var privateSettings = new HaulTripSettings("trip.funded", false, true); var ids = new[] { worker, visible };
            var sequences = new[] { denied.Seat(0).Submit(Group(ids, privateSettings)),
                denied.Seat(0).Submit(new ConfigureHaulTripPolicyCommand { Worker = visible, Settings = privateSettings }) };
            c.Equal(sequences[0], deniedTwin.Seat(0).Submit(Group(ids, privateSettings)), "saved group command sequence matches");
            c.Equal(sequences[1], deniedTwin.Seat(0).Submit(new ConfigureHaulTripPolicyCommand { Worker = visible, Settings = privateSettings }), "saved single command sequence matches");
            PermanentRemovalTests.Step(denied, deniedTwin);
            foreach (var current in new[] { denied, deniedTwin })
            {
                long cursor = 0; var results = new List<CommandResult>(); current.Seat(0).ReadResults(ref cursor, results);
                foreach (int sequence in sequences) c.That(results.Single(row => row.Sequence == sequence).Refusal == Codes.NotFound
                    && !results.Single(row => row.Sequence == sequence).Accepted, "executed private group/single configure refuses");
                c.That(Actual(current, worker).Profile == oldWorker.Profile && Actual(current, worker).Enabled == oldWorker.Enabled
                    && Actual(current, visible).Profile == oldVisible.Profile && Actual(current, visible).Enabled == oldVisible.Enabled,
                    "refused grouped control commits no visible or hidden member prefix");
            }
            c.Equal(denied.Hash(), deniedTwin.Hash(), "saved private refusals agree");
            c.That(new ReplayVerifier().Verify(factory, deniedRecording.Finish(), limits).Success, "actual private grouped/single prefix replays");
            var twin = factory.Load(game.Save()); Hidden(c, twin, worker, visible, source, target, supply);
            game.Clock.Pause(); twin.Clock.Pause(); long stopped = game.Clock.StepIndex; PermanentRemovalTests.Step(game, twin);
            c.Equal(stopped, game.Clock.StepIndex, "paused private policies and saved twins keep one stopped clock"); game.Clock.Resume(); twin.Clock.Resume();
            var replacement = new HaulTripSettings("trip.no-floor", true, false);
            var result = PermanentRemovalTests.Submit(c, game, Group(ids, replacement));
            var twinResult = PermanentRemovalTests.Submit(c, twin, Group(ids, replacement));
            c.That(result.Arg0 == 2 && twinResult.Arg0 == 2 && result.Arg1 == 1 && twinResult.Arg1 == 1,
                "two selected members and exactly one actual change, not a count derived from redacted preview fields");
            foreach (var current in new[] { game, twin })
            {
                c.Equal(Code.None, View(current).ReadHaulTripPolicy(worker, out var known), "known replacement becomes observable without revealing the old profile");
                c.That(known.Profile == replacement.Profile && known.Enabled && !known.PreserveOutletReserve, "exact approved replacement committed");
                c.Equal(Code.None, View(current).ReadHaulTripServiceSources(worker, out var sources), "known service declarations readable"); c.Equal(0, sources.Count, "authored empty source list unchanged");
                c.Equal(Code.None, View(current).ReadHaulTripServiceWindow(worker, out var window), "known actual service window readable"); c.That(window.Unrestricted, "explicit no-service profile remains unrestricted");
            }
            c.Equal(game.Hash(), twin.Hash(), "hidden-to-known grouped policy change survives saved continuation");
            twin = factory.Load(game.Save());
            foreach (var current in new[] { game, twin }) PermanentRemovalTests.Submit(c, current, new HaulResourcesCommand {
                Worker = worker, Source = source, Destination = target, Resource = supply, Ceiling = Fix.FromInt(4), Priority = 100 });
            var timer = Stopwatch.StartNew(); long began = game.Clock.StepIndex;
            bool Done() => Stock(game, target, supply) == Fix.FromInt(4) && !PermanentRemovalTests.Jobs(game).Any(job => job.IsHaul && job.Worker == worker);
            while (!Done() && game.Clock.StepIndex - began < 128 && timer.Elapsed.TotalSeconds < 25)
            {
                PermanentRemovalTests.Step(game, twin);
                c.Equal(Fix.FromInt(20), Stock(game, source, supply) + Stock(game, target, supply) + Stock(game, worker, supply), "actual finite cargo conserved through selected known policy");
            }
            if (!Done()) throw new InvalidOperationException("known replacement physical haul/128updates25seconds/" + game.Clock.StepIndex);
            c.That(Stock(game, source, supply) == Fix.FromInt(16) && Stock(game, worker, supply) == Fix.Zero, "replacement policy funds real movement and four-unit delivery once");
            c.That(oldWorker.Profile == "trip.funded" && oldWorker.Enabled && oldVisible.Profile == "trip.no-floor", "old authorized settings remain detached");
            c.Equal(game.Hash(), twin.Hash(), "saved known replacement and physical hauling converge"); c.Equal(game.Hash(), factory.Load(game.Save()).Hash(), "completed policy/stock/claims checkpoint restores exactly");
            c.Equal(isolated, idle.Hash(), "interleaved profile visibility and settings remain isolated");
            c.Equal(game.Clock.StepIndex - recording.InitialStep, (long)recording.CheckpointCount, "full main recording includes every advancing update");
            var verified = new ReplayVerifier().Verify(factory, recording.Finish(), limits);
            c.That(verified.Success && verified.ActualHash == game.Hash(), "complete profile privacy/replacement/physical haul replay");
            Console.WriteLine("trip-profile-disclosure/full/" + recording.InitialStep + "->" + game.Clock.StepIndex + "/inputs=" + recording.CommandCount
                + "/checkpoints=" + recording.CheckpointCount + "/hash=" + game.Hash().ToString("X16"));
        }
    }
}
